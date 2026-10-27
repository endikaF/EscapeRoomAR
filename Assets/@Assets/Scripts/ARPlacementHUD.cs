using UnityEngine;

// Interfaz mínima con IMGUI (no necesita Canvas ni EventSystem). Franja de botones abajo y texto de estado arriba.
public class ARPlacementHUD : MonoBehaviour
{
    [SerializeField] private ARTapSpawnPointProvider provider;
    [SerializeField] private SpawnManager spawnManager;

    private GUIStyle buttonStyle;
    private GUIStyle labelStyle;

    private void OnGUI()
    {
        if (provider == null) return;

        float h = Screen.height * 0.10f;
        float pad = h * 0.08f;
        float stripH = h + pad * 2f;
        int font = Mathf.RoundToInt(h * 0.32f);

        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter };
            labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, wordWrap = true };
        }
        buttonStyle.fontSize = font;
        labelStyle.fontSize = Mathf.RoundToInt(font * 0.8f);

        // Los toques en esta franja no colocan puntos
        provider.ignoreBottomPixels = stripH;

        // Texto de estado (arriba)
        string modeText = provider.CurrentMode == ARTapSpawnPointProvider.Mode.Place ? "COLOCAR" : "JUGAR";
        GUI.Label(new Rect(pad * 2, pad, Screen.width - pad * 4, h * 1.6f),
                $"Modo: {modeText}  |  {provider.SelectedCategory}: {provider.SelectedType}  |  Puntos: {provider.PointCount}\n{provider.LastMessage}",
                labelStyle);

        float y = Screen.height - stripH + pad;

        if (provider.CurrentMode == ARTapSpawnPointProvider.Mode.Place)
        {
            var types = MonsterTypeExtensions.TypesOf(provider.SelectedCategory);
            int n = 1 + types.Count + 3;
            float w = (Screen.width - pad * (n + 1)) / n;
            int idx = 0;

            // Botón de categoría
            if (GUI.Button(new Rect(pad + idx++ * (w + pad), y, w, h), "Cat: " + provider.SelectedCategory, buttonStyle))
                provider.CycleCategory();

            // Tipos de la categoría activa
            foreach (var t in types)
            {
                Color old = GUI.backgroundColor;
                if (provider.SelectedType == t) GUI.backgroundColor = Color.green;
                bool pressed = GUI.Button(new Rect(pad + idx++ * (w + pad), y, w, h), t.ToString(), buttonStyle);
                GUI.backgroundColor = old;
                if (pressed) provider.SetType(t);
            }

            if (GUI.Button(new Rect(pad + idx++ * (w + pad), y, w, h), "Deshacer", buttonStyle)) provider.UndoLast();
            if (GUI.Button(new Rect(pad + idx++ * (w + pad), y, w, h), "Borrar todo", buttonStyle)) provider.ClearAll();
            if (GUI.Button(new Rect(pad + idx++ * (w + pad), y, w, h), "JUGAR", buttonStyle))
                provider.SetMode(ARTapSpawnPointProvider.Mode.Play);
        }
        else
        {
            var r = new Rect(pad, y, Screen.width - pad * 2, h);
            if (GUI.Button(r, "VOLVER A COLOCAR", buttonStyle))
            {
                // Limpiar los monstruos ya aparecidos para poder repetir la prueba
                if (spawnManager != null) spawnManager.ResetSpawns();
                provider.SetMode(ARTapSpawnPointProvider.Mode.Place);
            }
        }
    }
}
