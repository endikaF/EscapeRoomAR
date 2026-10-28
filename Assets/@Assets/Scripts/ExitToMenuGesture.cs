using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Salida al menú inicial: mantener grip izquierdo + grip derecho + A + X durante holdSeconds.
// Solo actúa en modo Jugar. Por separado, esos botones no hacen nada en el juego.
// Ponlo en un objeto vacío de la escena Gafas y asigna el provider.
// En el editor, mantener la tecla F9 también cuenta como gesto completo (para probar sin casco).
public class ExitToMenuGesture : MonoBehaviour
{
    [SerializeField] private ARTapSpawnPointProvider provider;
    [SerializeField] private string menuSceneName = "Inicio";
    [SerializeField] private float holdSeconds = 3f;

    // 0..1, por si quieres mostrar una barra o texto de progreso en el HUD
    public float Progress { get; private set; }

    private InputAction gripL, gripR, aBtn, xBtn;
    private float held;
    private bool leaving;

    private void Awake()
    {
        gripL = Make("<XRController>{LeftHand}/{GripButton}", "<XRController>{LeftHand}/gripPressed");
        gripR = Make("<XRController>{RightHand}/{GripButton}", "<XRController>{RightHand}/gripPressed");
        aBtn = Make("<XRController>{RightHand}/{PrimaryButton}", "<XRController>{RightHand}/primaryButton");
        xBtn = Make("<XRController>{LeftHand}/{PrimaryButton}", "<XRController>{LeftHand}/primaryButton");
    }

    private static InputAction Make(params string[] bindings)
    {
        var a = new InputAction(type: InputActionType.Button);
        foreach (var b in bindings) a.AddBinding(b);
        return a;
    }

    private void OnEnable() { gripL.Enable(); gripR.Enable(); aBtn.Enable(); xBtn.Enable(); }
    private void OnDisable() { gripL.Disable(); gripR.Disable(); aBtn.Disable(); xBtn.Disable(); }
    private void OnDestroy() { gripL.Dispose(); gripR.Dispose(); aBtn.Dispose(); xBtn.Dispose(); }

    private void Update()
    {
        if (leaving || provider == null) return;

        bool playing = provider.CurrentMode == ARTapSpawnPointProvider.Mode.Play;
        bool all = playing && gripL.IsPressed() && gripR.IsPressed() && aBtn.IsPressed() && xBtn.IsPressed();

#if UNITY_EDITOR
        var kb = Keyboard.current;
        if (playing && kb != null && kb.f9Key.isPressed) all = true;
#endif

        held = all ? held + Time.unscaledDeltaTime : 0f;
        Progress = Mathf.Clamp01(held / holdSeconds);

        if (held >= holdSeconds) GoToMenu();
    }

    // También lo puede llamar EndOfGame
    public void GoToMenu()
    {
        if (leaving) return;
        leaving = true;

        // Al volver a entrar en la escena de juego se recargan las anclas y el JSON: los puntos se conservan
        GameSession.StartMode = ARTapSpawnPointProvider.Mode.Play;
        GameSession.IsAdmin = false;
        GameSession.KidsMode = false;
        SceneManager.LoadScene(menuSceneName);
    }
}
