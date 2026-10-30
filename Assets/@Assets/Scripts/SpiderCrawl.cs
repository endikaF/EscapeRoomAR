using Unity.XR.CoreUtils;
using UnityEngine;
using System.Collections.Generic;

// La araña vive pegada a la superficie donde nació (suelo, pared o techo).
// La superficie se define por el punto de spawn y la normal = transform.up al aparecer.
//
// Estados:
//  - Wander : deambula por puntos al azar dentro de un radio alrededor de su punto, con pausas.
//  - Descend: (solo arañas de pared) cuando el jugador se acerca, baja por la pared en diagonal hacia
//             el suelo, hacia el punto que queda frente a él. Al llegar al suelo pasa a Chase.
//  - Chase  : va a por el jugador por su superficie (suelo, o techo por ahora).
//
// Suelo: altura del XR Origin si hay uno en la escena (escena final, modo Floor);
// si no lo hay (escena de pruebas del editor) usa fallbackFloorY.
// Animator esperado: bool "IsMoving", trigger "Attack" (opcional).
public class SpiderCrawl : MonoBehaviour
{
    private enum State { Wander, Descend, Chase, Drop, Dead }

    [Header("Persecución")]
    [SerializeField] private float speed = 1f;
    [SerializeField] private float stopDistance = 0.8f;
    [SerializeField] private float attackCooldown = 1.5f;
    [Tooltip("Distancia horizontal al jugador a la que deja de deambular y va a por él")]
    [SerializeField] private float aggroDistance = 3f;

    [Header("Deambular")]
    [SerializeField] private float wanderSpeed = 0.4f;
    [Tooltip("Radio alrededor de su punto dentro del cual elige destinos")]
    [SerializeField] private float wanderRadius = 1f;
    [Tooltip("Pausa mínima y máxima (s) al llegar a cada destino")]
    [SerializeField] private Vector2 pauseRange = new Vector2(0.5f, 2f);

    [Header("Cuerpo")]
    [SerializeField] private float turnSpeed = 8f;
    [Tooltip("Distancia del pivote (centro del cuerpo) a las patas")]
    [SerializeField] private float bodyOffset = 0.05f;

    [Header("Bajada de pared")]
    [Tooltip("Al llegar a esta altura sobre el suelo, deja la pared y pasa a ir por el suelo")]
    [SerializeField] private float landDistance = 0.2f;
    [Tooltip("Velocidad (m/s) con la que se acerca al suelo tras dejar la pared")]
    [SerializeField] private float settleSpeed = 1f;

    [Header("Bajada de techo (hilo)")]
    [Tooltip("Distancia horizontal al jugador a la que se descuelga del techo")]
    [SerializeField] private float dropDistance = 1f;
    [Tooltip("Velocidad (m/s) de bajada por el hilo")]
    [SerializeField] private float dropSpeed = 1.2f;
    [Tooltip("Rapidez con la que gira hasta quedar de pie mientras baja")]
    [SerializeField] private float dropTurnSpeed = 3f;
    [SerializeField] private float threadWidth = 0.004f;

    [Header("Suelo")]
    [Tooltip("Altura del suelo si no hay XR Origin en la escena (escena de pruebas del editor)")]
    [SerializeField] private float fallbackFloorY = 0f;

    [Header("Sonido")]
    [Tooltip("AudioSource con el sonido de deambular/perseguir (Loop activado)")]
    [SerializeField] private AudioSource moveSource;
    [Tooltip("AudioSource aparte para el ataque (sin Loop, Play On Awake desactivado)")]
    [SerializeField] private AudioSource attackSource;
    [SerializeField] private AudioClip attackClip;
    [Range(0f, 1f)][SerializeField] private float wanderVolume = 0.3f;
    [Range(0f, 1f)][SerializeField] private float chaseVolume = 1f;
    [Tooltip("Rapidez del cambio de volumen (unidades de volumen por segundo)")]
    [SerializeField] private float volumeFadeSpeed = 1.5f;
    [Tooltip("Segundos entre que empieza la animación de ataque y suena el golpe")]
    [SerializeField] private float attackSoundDelay = 0f;

    [Tooltip("Rapidez con la que se apaga el sonido de andar al parar")]
    [SerializeField] private float stopFadeSpeed = 6f;

    [Header("Separación")]
    [Tooltip("Distancia mínima que intentan mantener entre arañas")]
    [SerializeField] private float separationRadius = 0.5f;
    [Tooltip("Rapidez con la que se apartan (m/s)")]
    [SerializeField] private float separationSpeed = 1.5f;

    private static readonly List<SpiderCrawl> allSpiders = new List<SpiderCrawl>();

    [Tooltip("0 = se queda esperando detrás. Más alto = rodea a las demás buscando un hueco")]
    [SerializeField] private float sidestepWeight = 0.6f;

    private float distToPlayer;

    private bool moving;

    private Animator anim;
    private bool hasAttackParam;
    private Vector3 surfaceOrigin;
    private Vector3 surfaceNormal;
    private Vector3 axisF, axisR; // ejes del plano para elegir destinos
    private bool isWall;
    private float floorY;
    private float heightOnSurface; // distancia del pivote a la superficie; baja hasta bodyOffset tras aterrizar
    private bool isCeiling;
    private LineRenderer thread;
    private Vector3 threadTop;
    private float fallSpeed;

    private State state = State.Wander;
    private Vector3 waypoint;
    private float pauseUntil;
    private float nextAttackTime;
    private float stunnedUntil;

    private void Start()
    {
        anim = GetComponentInChildren<Animator>();
        surfaceOrigin = transform.position;
        surfaceNormal = transform.up.normalized;
        heightOnSurface = bodyOffset;
        transform.position = surfaceOrigin + surfaceNormal * heightOnSurface;

        // Pared = superficie casi vertical (su normal es casi horizontal)
        isWall = Mathf.Abs(surfaceNormal.y) < 0.5f;
        isCeiling = surfaceNormal.y < -0.5f;

        var origin = FindFirstObjectByType<XROrigin>();
        floorY = origin != null ? origin.transform.position.y : fallbackFloorY;

        axisF = Vector3.ProjectOnPlane(transform.forward, surfaceNormal).normalized;
        if (axisF.sqrMagnitude < 0.001f) axisF = Vector3.ProjectOnPlane(Vector3.forward, surfaceNormal).normalized;
        axisR = Vector3.Cross(surfaceNormal, axisF);

        if (anim != null)
            foreach (var p in anim.parameters)
                if (p.name == "Attack") hasAttackParam = true;

        Debug.Log($"[Spider] anim={(anim != null)}, hasAttackParam={hasAttackParam}, animObj={(anim != null ? anim.gameObject.name : "-")}, spiderObj={gameObject.name}");

        PickWaypoint();
        pauseUntil = Time.time + Random.Range(pauseRange.x, pauseRange.y);

        if (moveSource != null)
        {
            moveSource.loop = true;
            moveSource.volume = 0f; // empieza en silencio; UpdateMoveVolume lo arranca al moverse
        }
    }

    private void Update()
    {
        if (state == State.Dead)
        {
            FallUpdate();
            return;
        }

        if (MonsterFreeze.Frozen) { SetMoving(false); UpdateMoveVolume(); return; }

        UpdateMoveVolume();

        var cam = Camera.main;
        if (cam == null) return;
        if (Time.time < stunnedUntil) return;

        Vector3 player = cam.transform.position;

        if (state == State.Wander && HorizontalDistance(player) <= aggroDistance
        && RealWorld.CanSee(transform.position, player))
            Aggro();

        // Tras aterrizar, acercarse suavemente al suelo
        heightOnSurface = Mathf.MoveTowards(heightOnSurface, bodyOffset, settleSpeed * Time.deltaTime);

        if (state == State.Chase && !RealWorld.CanSee(transform.position, player))
        {                                                                            
            SetMoving(false);                                                          
            return;                                                                    
        }

        switch (state)
        {
            case State.Wander: WanderUpdate(); break;
            case State.Descend: DescendUpdate(player); break;
            case State.Chase: ChaseUpdate(player); break;
            case State.Drop: DropUpdate(player); break;
        }

        // Mantener siempre pegada a la superficie a la altura actual (no mientras cuelga del hilo)
        if (state != State.Drop)
        {
            ApplySeparation(player);
            transform.position = ProjectOnSurface(transform.position) + surfaceNormal * heightOnSurface;
        }
    }

    private void Aggro()
    {
        state = isWall ? State.Descend : State.Chase;
    }

    // ---- Estados ----

    private void WanderUpdate()
    {
        if (Time.time < pauseUntil)
        {
            SetMoving(false);
            return;
        }

        Vector3 current = ProjectOnSurface(transform.position);
        Vector3 dir = waypoint - current;

        if (dir.magnitude < 0.05f)
        {
            pauseUntil = Time.time + Random.Range(pauseRange.x, pauseRange.y);
            PickWaypoint();
            SetMoving(false);
            return;
        }

        MoveAndFace(current, dir, wanderSpeed);
    }

    // Baja por la pared en línea recta hacia el punto de la pared frente al jugador, a la altura del suelo (diagonal)
    private void DescendUpdate(Vector3 playerPos)
    {
        Vector3 current = ProjectOnSurface(transform.position);

        if (current.y <= floorY + landDistance)
        {
            LandOnFloor(current);
            return;
        }

        Vector3 target = ProjectOnSurface(new Vector3(playerPos.x, floorY, playerPos.z));
        MoveAndFace(current, target - current, speed);
    }

    // La superficie pasa a ser el suelo. El giro de orientación lo hace FaceOnly poco a poco
    private void LandOnFloor(Vector3 current)
    {
        // Conserva la altura actual sobre el suelo; Update la irá bajando hasta bodyOffset
        heightOnSurface = Mathf.Max(bodyOffset, current.y - floorY);
        surfaceNormal = Vector3.up;
        surfaceOrigin = new Vector3(current.x, floorY, current.z);
        isWall = false;
        isCeiling = false;
        DestroyThread();
        state = State.Chase;
    }

    private void ChaseUpdate(Vector3 playerPos)
    {
        Vector3 current = ProjectOnSurface(transform.position);
        Vector3 dir = ProjectOnSurface(playerPos) - current;
        float dist = dir.magnitude;

        // Araña de techo: cuando está cerca de ti, se descuelga
        if (isCeiling && dist <= dropDistance)
        {
            StartDrop(current);
            return;
        }

        if (dist > stopDistance)
        {
            MoveAndFace(current, dir, speed);
        }
        else
        {
            FaceOnly(dir);
            SetMoving(false);
            if (Time.time >= nextAttackTime)
            {
                Debug.Log($"[Spider] ATAQUE: dist={dist:F2}, hasAttackParam={hasAttackParam}");
                nextAttackTime = Time.time + attackCooldown;
                if (anim != null && hasAttackParam) anim.SetTrigger("Attack");
            }
        }
    }

    // ---- Muerte ----

    // Lo llama MonsterHealth al morir (en lugar de desactivar el script).
    // Si está en el suelo se queda donde está; si está en pared, techo o colgando, cae al suelo.
    public void Die()
    {
        DestroyThread(); // el hilo se corta

        StopSounds();

        if (state == State.Dead) return;

        bool onFloor = !isWall && !isCeiling;
        if (onFloor)
        {
            enabled = false;
            return;
        }

        state = State.Dead;
        fallSpeed = 0f;
    }

    private void FallUpdate()
    {
        fallSpeed += 9.81f * Time.deltaTime;

        Vector3 p = transform.position;
        p.y -= fallSpeed * Time.deltaTime;

        // Va quedando de pie mientras cae
        Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.ProjectOnPlane(transform.up, Vector3.up);
        if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
        Quaternion upright = Quaternion.LookRotation(fwd.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, upright, dropTurnSpeed * Time.deltaTime);

        float rest = floorY + bodyOffset;
        if (p.y <= rest)
        {
            p.y = rest;
            transform.position = p;
            enabled = false; // ya en el suelo
            return;
        }

        transform.position = p;
    }

    // ---- Bajada del techo colgada de un hilo ----

    private void StartDrop(Vector3 ceilingPoint)
    {
        threadTop = ceilingPoint;
        state = State.Drop;
        CreateThread();
        SetMoving(false);
    }

    private void DropUpdate(Vector3 playerPos)
    {
        Vector3 p = transform.position;
        p.y -= dropSpeed * Time.deltaTime;
        transform.position = p;

        // Gira poco a poco hasta quedar de pie, mirando al jugador
        Vector3 toPlayer = playerPos - p;
        toPlayer.y = 0f;
        Vector3 fwd = toPlayer.sqrMagnitude > 0.001f
            ? toPlayer.normalized
            : Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (fwd.sqrMagnitude > 0.001f)
        {
            Quaternion target = Quaternion.LookRotation(fwd, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, dropTurnSpeed * Time.deltaTime);
        }

        if (p.y <= floorY + landDistance) LandOnFloor(p);
    }

    private void CreateThread()
    {
        if (thread != null) return;
        var go = new GameObject("SpiderThread");
        thread = go.AddComponent<LineRenderer>();
        thread.positionCount = 2;
        thread.startWidth = thread.endWidth = threadWidth;
        thread.material = new Material(Shader.Find("Sprites/Default"));
        thread.startColor = thread.endColor = new Color(1f, 1f, 1f, 0.8f);
    }

    private void DestroyThread()
    {
        if (thread == null) return;
        Destroy(thread.gameObject);
        thread = null;
    }

    private void LateUpdate()
    {
        if (thread == null) return;
        thread.SetPosition(0, threadTop);
        thread.SetPosition(1, transform.position + Vector3.up * bodyOffset);
    }

    private void OnEnable() { allSpiders.Add(this); }
    private void OnDisable() { allSpiders.Remove(this); DestroyThread(); }
    private void OnDestroy() { DestroyThread(); }

    // ---- Movimiento sobre la superficie ----

    private void MoveAndFace(Vector3 current, Vector3 dir, float spd)
    {
        FaceOnly(dir);
        Vector3 next = current + dir.normalized * spd * Time.deltaTime;
        transform.position = next + surfaceNormal * heightOnSurface;
        SetMoving(true);
    }

    private void FaceOnly(Vector3 dir)
    {
        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion rot = Quaternion.LookRotation(dir, surfaceNormal);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, turnSpeed * Time.deltaTime);
        }
        else
        {
            Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, surfaceNormal);
            if (fwd.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(fwd, surfaceNormal);
        }
    }

    private void PickWaypoint()
    {
        Vector2 c = Random.insideUnitCircle * wanderRadius;
        waypoint = surfaceOrigin + axisR * c.x + axisF * c.y;
    }

    private Vector3 ProjectOnSurface(Vector3 p)
    {
        float d = Vector3.Dot(p - surfaceOrigin, surfaceNormal);
        return p - surfaceNormal * d;
    }

    private float HorizontalDistance(Vector3 playerPos)
    {
        Vector3 a = new Vector3(playerPos.x, 0f, playerPos.z);
        Vector3 b = new Vector3(transform.position.x, 0f, transform.position.z);
        return Vector3.Distance(a, b);
    }

    private void SetMoving(bool value)
    {
        moving = value;
        if (anim != null) anim.SetBool("IsMoving", value);
    }

    // Lo llama MonsterHealth al recibir daño: se queda quieta un momento y pasa a ir a por el jugador
    public void Stun(float seconds)
    {
        if (state == State.Wander) Aggro();
        if (Time.time < stunnedUntil) return;
        stunnedUntil = Time.time + seconds;
        SetMoving(false);
    }

    private void UpdateMoveVolume()
    {
        if (moveSource == null) return;

        // Cuelga del hilo (Drop) también cuenta como movimiento
        bool soundOn = moving || state == State.Drop;
        float target = !soundOn ? 0f : (state == State.Wander ? wanderVolume : chaseVolume);
        float fade = soundOn ? volumeFadeSpeed : stopFadeSpeed;

        moveSource.volume = Mathf.MoveTowards(moveSource.volume, target, fade * Time.deltaTime);

        if (soundOn && !moveSource.isPlaying) moveSource.Play();
        else if (!soundOn && moveSource.volume <= 0f && moveSource.isPlaying) moveSource.Stop();
    }

    // Lo llama un Animation Event en el clip de ataque, en el frame del golpe
    public void PlayAttackSound()
    {
        if (attackSource == null || attackClip == null) return;
        attackSource.Stop();
        attackSource.clip = attackClip;
        attackSource.Play();
        Debug.Log($"[Spider] PlayAttackSound t={Time.time:F2}");
    }

    private void StopSounds()
    {
        if (moveSource != null) moveSource.Stop();
    }

    private void ApplySeparation(Vector3 player)
    {
        distToPlayer = HorizontalDistance(player);
        Vector3 push = Vector3.zero;

        foreach (var o in allSpiders)
        {
            if (o == this || o.state == State.Dead) continue;

            // Solo me aparto de las que tienen prioridad (más cerca del jugador). Las de atrás no me empujan
            bool otherHasPriority = o.distToPlayer < distToPlayer - 0.05f
                || (Mathf.Abs(o.distToPlayer - distToPlayer) <= 0.05f && o.GetInstanceID() < GetInstanceID());
            if (!otherHasPriority) continue;

            Vector3 d = transform.position - o.transform.position;
            float dist = d.magnitude;
            if (dist >= separationRadius) continue;

            Vector3 away = Vector3.ProjectOnPlane(d, surfaceNormal);
            if (away.sqrMagnitude < 0.0001f) away = axisR;
            away.Normalize();
            float w = 1f - dist / separationRadius;

            Vector3 side = Vector3.Cross(surfaceNormal, away);
            // Elegir siempre el mismo sentido de giro alrededor del jugador, para que todas rodeen hacia el mismo lado y no se bloqueen
            Vector3 toPlayerDir = ProjectOnSurface(player) - ProjectOnSurface(transform.position);
            if (Vector3.Dot(Vector3.Cross(surfaceNormal, toPlayerDir), side) < 0f) side = -side;
            side = Vector3.Cross(surfaceNormal, away) * Mathf.Sign(Vector3.Dot(Vector3.Cross(surfaceNormal, away), Vector3.ProjectOnPlane(d, surfaceNormal)) + 0.0001f);

            push += (away + side * sidestepWeight) * w;
        }

        if (push != Vector3.zero)
            transform.position += push * separationSpeed * Time.deltaTime;
    }
}