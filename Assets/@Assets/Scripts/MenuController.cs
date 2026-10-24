using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Lógica del menú inicial.
//  - StartPlay(): lo llama el botón JUGAR. Carga la escena de juego en modo Jugar (cliente).
//  - Combinación de administrador con el mando derecho: A, A, B y Grip (en ese orden).
//    Carga la escena de juego en modo Colocar con permisos de administrador.
//    El gatillo NO forma parte de la combinación: solo sirve para hacer clic en el botón.
//  - Solo en el editor, atajos de teclado equivalentes: 1 = A, 2 = B, 3 = Grip (o sea 1, 1, 2, 3).
// Si te equivocas de botón, la secuencia se reinicia. También se reinicia si pasa mucho tiempo entre pulsaciones.
public class MenuController : MonoBehaviour
{
    private enum Btn { A, B, Grip }
    private static readonly Btn[] sequence = { Btn.A, Btn.A, Btn.B, Btn.Grip };

    [Tooltip("Tiempo máximo entre dos pulsaciones de la combinación")]
    [SerializeField] private float maxSecondsBetweenPresses = 3f;

    private InputAction aAction, bAction, gripAction;
    private int step;
    private float lastPressTime;
    private bool launching;

    private void Awake()
    {
        aAction = MakeButton("A", "{PrimaryButton}", "primaryButton");
        bAction = MakeButton("B", "{SecondaryButton}", "secondaryButton");
        gripAction = MakeButton("Grip", "{GripButton}", "gripPressed");
    }

    private static InputAction MakeButton(string name, params string[] controls)
    {
        var action = new InputAction(name, InputActionType.Button);
        foreach (var c in controls)
            action.AddBinding($"<XRController>{{RightHand}}/{c}");
        return action;
    }

    private void OnEnable()
    {
        aAction.Enable(); bAction.Enable(); gripAction.Enable();
    }

    private void OnDisable()
    {
        aAction.Disable(); bAction.Disable(); gripAction.Disable();
    }

    private void OnDestroy()
    {
        aAction.Dispose(); bAction.Dispose(); gripAction.Dispose();
    }

    // Se asigna en el OnClick del botón JUGAR
    public void StartPlay()
    {
        if (launching) return;
        Launch(ARTapSpawnPointProvider.Mode.Play, false);
    }

    private void Update()
    {
        if (launching) return;

        if (step > 0 && Time.time - lastPressTime > maxSecondsBetweenPresses)
            step = 0;

        if (aAction.WasPressedThisFrame()) Register(Btn.A);
        if (bAction.WasPressedThisFrame()) Register(Btn.B);
        if (gripAction.WasPressedThisFrame()) Register(Btn.Grip);

        EditorShortcuts();
    }

    private void EditorShortcuts()
    {
#if UNITY_EDITOR
        var kb = Keyboard.current;
        if (kb == null || launching) return;

        if (kb.digit1Key.wasPressedThisFrame) Register(Btn.A);
        if (kb.digit2Key.wasPressedThisFrame) Register(Btn.B);
        if (kb.digit3Key.wasPressedThisFrame) Register(Btn.Grip);
#endif
    }

    private void Register(Btn pressed)
    {
        if (launching) return;

        lastPressTime = Time.time;

        if (pressed == sequence[step]) step++;
        else step = pressed == sequence[0] ? 1 : 0;

        if (step == sequence.Length)
            Launch(ARTapSpawnPointProvider.Mode.Place, true);
    }

    private void Launch(ARTapSpawnPointProvider.Mode mode, bool admin)
    {
        launching = true;
        GameSession.StartMode = mode;
        GameSession.IsAdmin = admin;
        SceneManager.LoadScene(GameSession.GameSceneName);
    }
}