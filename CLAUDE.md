# Normas del proyecto EscapeRoomAR (Halloween AR)

Este archivo se carga al iniciar cada sesión. **Léelo y síguelo antes de modificar nada.**
Las normas salen de lo que ya hay en el proyecto (scripts, ajustes, historial de git), no de preferencias genéricas. Si una norma choca con lo que pide el usuario, manda el usuario; si una norma ha quedado obsoleta, díselo y propón actualizar este archivo.

## 1. Flujo de trabajo (siempre)

1. Lee este archivo y el `README.md` (describe controles, tipos de punto y requisitos de cada prefab).
2. Lee los scripts que vas a tocar **y** los que los llaman o dependen de ellos (mira el mapa de la sección 4) antes de editar.
3. Imita el estilo del código vecino. No refactorices, renombres ni "limpies" nada que no se haya pedido.
4. Tras tocar scripts: espera a que Unity compile y revisa `read_console` (errores y warnings). No des la tarea por hecha con errores de compilación.
5. Si cambias comportamiento, controles o requisitos de un prefab, **actualiza el `README.md`** (el historial muestra que se mantiene al día).
6. No hagas commit ni push salvo que el usuario lo pida. No guardes escenas ni prefabs por tu cuenta: di qué has modificado en el editor y deja que el usuario decida.
7. Cambios por MCP en escena/prefabs/assets: comprueba antes `mcpforunity://editor/state` (que no esté compilando ni en Play Mode) y no cambies de escena activa sin avisar.

## 2. Entorno y ajustes fijos

- Unity **6000.3.8f1**, URP 17.3, plataforma objetivo **Android** (Meta Quest 3 y móvil/tablet ARCore). IL2CPP, ARM64, Min SDK 32, Target SDK 35, paquete `com.endika.escaperoom`.
- **Active Input Handling = Both** (Input System + Input antiguo). Ambos se usan a propósito: `Input.GetMouseButtonDown`/`Input.touch` conviven con `InputAction`.
- Paquetes clave: AR Foundation 6.3.5, ARCore, OpenXR + Meta OpenXR, XR Interaction Toolkit 3.3.2, Input System 1.18, `unity-mcp` (CoplayDev).
- Escenas en Build: **Inicio** (menú, primera) y **Gafas** (juego, segunda). `Prueba.unity` es de pruebas y no está en el build.
- Capa física **`RealWorld`** = planos/paredes reales detectados. Los monstruos la usan para visión y colisión (`RealWorld.CanSee`, `RealWorld.PathBlocked`). No cambies su nombre ni su índice.
- No hay `.asmdef` propios: todo el código propio va a `Assembly-CSharp`.

## 3. Qué NO tocar

- `Assets/Samples/`, `Assets/TextMesh Pro/`, `Assets/TutorialInfo/`, `Assets/XRI/`, `Assets/XR/`, `Assets/Oculus/`, `Assets/Plugins/`, `Assets/Settings/`, `Assets/UnityXRContent/`, `Assets/CompositionLayers/`: paquetes y plantillas de terceros.
- `Assets/@Assets/Esqueleto/Skeleton/Scripts/` (`SimpleCameraController`, `AnimationReplayer`): vienen con el asset del esqueleto, no son código del juego.
- `Library/`, `Temp/`, `obj/`, `Logs/`, `UserSettings/`, `.vs/`, `.utmp/`, `APK/` y los `*.csproj` / `.sln`: generados o locales.
- Los `.cs` sueltos de la raíz (`ComputeCommandBuffer.cs`, `IBaseCommandBuffer.cs`, `IComputeCommandBuffer.cs`, `IRasterCommandBuffer.cs`, `IUnsafeCommandBuffer.cs`, `RasterCommandBuffer.cs`, `UnsafeCommandBuffer.cs`): no están en `Assets/`, no son del juego.
- Cambios en `Packages/manifest.json` y `packages-lock.json` solo si el usuario lo pide.
- Nunca borres ni muevas archivos de `Assets/` por el sistema de archivos: rompe los `.meta` y los GUID. Usa `manage_asset` (o el editor) y conserva siempre el `.meta`.

## 4. Mapa de scripts (`Assets/@Assets/Scripts/`)

**Núcleo de puntos y aparición**
- `MonsterType` – enum de tipos de punto, `PointCategory` (Monster / Decoration / Audio) y extensiones (`GetCategory`, `IsSurfaceAligned`, `TypesOf`).
- `SpawnPointData` – struct (`id`, `position`, `rotation`, `type`) que usa todo el juego.
- `ISpawnPointProvider` – interfaz de la capa de detección. Implementaciones: `ARTapSpawnPointProvider` (móvil/Quest, con anclas y JSON) y `MockSpawnPointProvider` (editor, con `MockSpawnMarker`).
- `SpawnManager` – instancia el prefab del punto cuando el jugador se acerca. Solo conoce `ISpawnPointProvider` y `MonsterRegistry`.
- `MonsterRegistry` – `ScriptableObject` (`Assets/@Assets/Data/MonsterRegistry.asset`): tipo → prefab(s) y distancia de activación.
- `GameSession` – estado estático entre escenas (`StartMode`, `IsAdmin`, `KidsMode`, `GameSceneName`).

**Monstruos y combate**
- `MonsterHealth` – vida y muerte. Es el **punto central**: llama a `Stun`/`Die` de cada comportamiento.
- `ChasePlayer` (zombi), `SpiderCrawl`, `BatSwarm`, `SkeletonSequence`, `PayasoPerseguir`, `PayasoEmboscar`, `PayasoSecuencia` – comportamientos por tipo.
- `MonsterFreeze` + `FreezeAnimators` – congelan monstruos mientras dura una secuencia (contador `Push`/`Pop`).
- `RealWorld` – línea de visión y colisión contra la capa `RealWorld`.
- `AttackSoundState` – `StateMachineBehaviour` para el sonido de ataque de la araña.

**Entrada y control**
- `QuestControllerPlacement` – mandos Quest (colocar y disparar). `ClickToShoot`, `EditorPlayerController` – solo editor/pruebas.
- `ExitToMenuGesture` – salida al menú (Grip+Grip+A+X 3 s, F9 en editor). `EndOfGame` – fin de partida opcional.
- `QuestPermissions` – permiso de escena (`com.oculus.permission.USE_SCENE`).

**Menú y HUD**
- `MenuController`, `MenuControllerPointer`, `PlaceMenuInFront`, `ARPlacementHUD` (IMGUI, sin Canvas).

**Audio**
- `AudioPoint` (reloj / demogorgon), `ThemeMusicController`, `BatMurmur`. Se coordinan por eventos estáticos (`AudioPoint.Started/Finished`, `SkeletonSequence.SpeechFinished`, `BatSwarm.Spawned`).

## 5. Estilo de código C# (observado en los scripts)

**Formato**
- 4 espacios de indentación, llaves en línea propia (Allman). Finales de línea: respeta los del archivo (ver sección 7).
- Sin `namespace`. Un tipo público por archivo y el nombre del archivo = nombre de la clase, en PascalCase.
- `using` de `System.*` primero y después `UnityEngine.*`; solo los necesarios.
- `if`/`foreach` de una sola sentencia pueden ir sin llaves; guard clauses con `return` al principio de `Update`.
- Se permite `;` en una línea para pares triviales (`aAction.Enable(); bAction.Enable();`).

**Nombres y campos**
- Campos del inspector: `[SerializeField] private tipo camelCase = valor;` (sin `_` ni `m_`). Públicos solo si son API real (`public bool X { get; private set; }`).
- Agrupa con `[Header("...")]`, explica con `[Tooltip("...")]` y limita con `[Range]`. **Tooltips, cabeceras y comentarios en español.**
- Estados internos con `enum State` privado; estructuras auxiliares como `private class` anidadas.
- Constantes públicas con `const` (`GameSession.GameSceneName`).

**Comentarios**
- Cada clase lleva arriba un bloque `//` en español: qué hace, dónde se pone (qué objeto/prefab), qué asignar y qué `Animator` espera (`Animator esperado: bool "IsMoving", trigger "Attack"`).
- Los métodos que llama otro script se anotan: `// Lo llama MonsterHealth al recibir daño`.
- Comenta el porqué, no lo obvio.

**Patrones a seguir**
- Comprobaciones de Unity con `if (x != null)` / `x == null` (no `?.` sobre objetos de Unity). Si falta una referencia, `return` en silencio o un único `Debug.LogWarning/LogError` con prefijo de clase: `Debug.LogError("SpawnManager: 'providerSource' no implementa ISpawnPointProvider.")`.
- Jugador = `Camera.main`, comprobando `null` en cada `Update`. Distancias "de aparición" son **horizontales** (se pone `y = 0`).
- Cooldowns y aturdimiento con `Time.time` (`nextAttackTime`, `stunnedUntil`). Gestos de entrada con `Time.unscaledDeltaTime`.
- Los monstruos cogen su `Animator` con `GetComponentInChildren<Animator>()` y comprueban `anim != null` antes de usarlo. Nombres de parámetros estándar: bool `IsMoving`, triggers `Attack`, `Hit`, `Die` (murciélago: bool `Fast`). Los nombres de estados del `Animator` son **exactos** y se exponen como `string` en el inspector.
- Suelo de los monstruos terrestres: `groundY = transform.position.y` en `Start`.
- Todo monstruo que se mueve debe: salir si `MonsterFreeze.Frozen`, respetar `stunnedUntil`, y usar `RealWorld.CanSee` / `RealWorld.PathBlocked` para no atravesar paredes.
- Eventos entre sistemas: `public static event System.Action<...>`; suscribir en `OnEnable` y desuscribir en `OnDisable`.
- Si un script hace `MonsterFreeze.Push()`, debe hacer `Pop()` al terminar **y** en `OnDestroy` (ver `SkeletonSequence`).
- `InputAction` creadas por código sobre `<XRController>{LeftHand|RightHand}/...`: `Enable` en `OnEnable`, `Disable` en `OnDisable`, `Dispose` en `OnDestroy`. Cada botón se enlaza con ambos controles (`{PrimaryButton}` y `primaryButton`, `{GripButton}` y `gripPressed`).
- Código solo para editor dentro de `#if UNITY_EDITOR` (atajos de teclado, ratón, `GameSession` por defecto). En el build, móvil usa `Input.touch`.
- Datos entre escenas solo en `GameSession` (estático). No uses `DontDestroyOnLoad` si basta con eso.
- Las corrutinas son el patrón habitual para secuencias (`IEnumerator`, `StartCoroutine`). Guarda el `Coroutine` si hay que cortarla.
- Persistencia de puntos: JSON en `Application.persistentDataPath` + anclas (`ARTapSpawnPointProvider`). No cambies el formato sin migración.

## 6. Reglas críticas (romperlas rompe datos guardados o prefabs)

1. **`MonsterType`: añadir SOLO al final del enum.** Sus valores se guardan como entero en `spawnpoints.json` (`SavedPoint.type`) y en `MonsterRegistry.asset`. Insertar en medio o reordenar corrompe los puntos ya colocados y el registro. El orden de ciclado en el juego sale del orden del enum.
2. Para añadir un tipo nuevo: (a) añadirlo al final de `MonsterType`; (b) darle categoría en `GetCategory` (y `IsSurfaceAligned` si procede); (c) asignar su prefab en `MonsterRegistry`; (d) si es un monstruo con vida, conectarlo en `MonsterHealth` (`Stun`/`Die`); (e) actualizar las tablas del `README.md`.
3. Si añades un comportamiento nuevo con `Stun`/`Die`, **hay que llamarlo desde `MonsterHealth`**, o el monstruo no reaccionará a los disparos.
4. Para recibir disparos un prefab necesita **Collider** + `MonsterHealth`. Los disparos buscan `GetComponentInParent<MonsterHealth>()`.
5. Nombres de escena (`"Inicio"`, `"Gafas"`) y de estados/parámetros del `Animator` están como `string`: si los cambias, hay que cambiarlos en código, inspector y Build Settings.
6. Zombis y payasos usan la altura de su punto como suelo (colocar en el suelo). Spider y telarañas se pegan a la superficie (`IsSurfaceAligned`).
7. Los controles del modo JUGAR son **solo los gatillos**; A/B/X/Y/Grip no deben hacer nada salvo el gesto de salida y la combinación de administrador del menú.
8. El `SpawnManager` no debe conocer anclas ni Meta: esa lógica va en un `ISpawnPointProvider`.

## 7. Codificación y finales de línea

- **Todos los scripts de `Assets/@Assets/Scripts/` son UTF-8 sin BOM.** (Siete que estaban en Windows-1252 se convirtieron el 2026-11-10: `AttackSoundState`, `PayasoEmboscar`, `PayasoPerseguir`, `PayasoSecuencia`, `PlaceMenuInFront`, `RealWorld`, `SkeletonSequence`.)
- Los archivos nuevos: **UTF-8 sin BOM**. Nunca guardes un `.cs` en Windows-1252/ANSI.
- Si algún archivo muestra `�` donde debería haber una tilde, comprueba su codificación con `iconv -f UTF-8 -t UTF-8 archivo.cs` antes de editarlo; si falla, está en cp1252 y hay que convertirlo con copia de seguridad previa y verificación de ida y vuelta, no editarlo a ciegas.
- **Finales de línea mezclados en el proyecto** (unos archivos CRLF, otros LF y otros con ambos). Git tiene `core.autocrlf=true` y normaliza a LF al hacer commit. **Respeta los finales de línea que ya tenga cada archivo** y no los normalices de paso: genera diffs enormes sin ningún cambio real. Los archivos nuevos pueden usar LF.

## 8. Recursos y convenciones del proyecto

- Todo el contenido propio vive en `Assets/@Assets/`, por tema y en español: `Animaciones`, `Ataud`, `Data`, `Esqueleto`, `Fantasma`, `Fotos`, `Material`, `Mesa`, `Murcielago`, `Payaso`, `Prefabs`, `Scenes`, `Scripts`, `Sonido`, `Spiders`, `Telarañas`, `Zombie`.
- Scripts nuevos: `Assets/@Assets/Scripts/`. Prefabs de juego: `Assets/@Assets/Prefabs/`. `ScriptableObject`: `Assets/@Assets/Data/`. Sonidos: `Assets/@Assets/Sonido/`.
- Nombres de prefabs/archivos en español, con `_` o espacios y a veces tildes/ñ (`telaraña_esquina`, `Payaso_Arma_Perseguir`). Los tipos de punto mezclan inglés y español (`Zombie`, `Payaso_perseguir`, `Audio_reloj`): sigue el patrón del tipo más cercano.
- Menú de creación de assets propios: `Halloween/...` (`[CreateAssetMenu(menuName = "Halloween/Monster Registry")]`).
- Shaders propios con prefijo `Halloween/` (ej. `Halloween/DepthOccluder`).
- No renombres archivos con referencias por nombre (escenas, clips de audio usados por estado) sin revisar quién los usa.

## 9. Git

- Rama principal: `main`. Mensajes de commit **en español, minúsculas, sin prefijos** tipo `feat:`, frase corta que dice el resultado: `zombie agrandado, y plano solucionado.`, `payaso emboscar listo a falta de pruebas`.
- `.gitignore` es el de plantilla Unity; no subas `Library/`, `Temp/`, `obj/`, `Logs/`, `UserSettings/`.
- Cambios en `.unity`, `.prefab` y `.asset` son YAML: no los edites a mano salvo que no haya alternativa; usa el editor/MCP.

## 10. Rendimiento y plataforma (Quest 3 / móvil)

- Evita crear garbage por frame en `Update` (`new List`, `new Vector3[]`, LINQ, concatenar strings). Reutiliza listas estáticas como hace `ARTapSpawnPointProvider` (`hits`, `empty`).
- No añadas `Find*`/`GetComponent` por frame cuando se pueda cachear en `Awake`/`Start` (ya hay algún `FindObjectsByType` en `EndOfGame`; no lo repliques en más sitios).
- Todo debe funcionar en editor, móvil y Quest: protege el código específico con `#if UNITY_EDITOR` o comprobando referencias `null` (`anchorManager == null` = sin persistencia).
- Fondo de cámara transparente (passthrough) y un solo proveedor XR por build (OpenXR para Quest, ARCore para móvil): no lo cambies sin pedirlo.

---
*Para ampliar o corregir estas normas, edita este archivo (`CLAUDE.md`). Para recordar algo nuevo, añádelo en la sección que corresponda.*
