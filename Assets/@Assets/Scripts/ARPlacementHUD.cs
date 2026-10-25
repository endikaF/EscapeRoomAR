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
            $"Modo: {modeText}  |  Tipo: {provider.SelectedType}  |  Puntos: {provider.PointCount}\n{provider.LastMessage}",
            labelStyle);

        float y = Screen.height - stripH + pad;

        if (provider.CurrentMode == ARTapSpawnPointProvider.Mode.Place)
        {
            string[] names = { "Zombie", "Spider", "Bat", "Skeleton", "Deshacer", "Borrar todo", "JUGAR" };
            float w = (Screen.width - pad * (names.Length + 1)) / names.Length;

            for (int i = 0; i < names.Length; i++)
            {
                var r = new Rect(pad + i * (w + pad), y, w, h);

                Color old = GUI.backgroundColor;
                if ((i == 0 && provider.SelectedType == MonsterType.Zombie) ||
                    (i == 1 && provider.SelectedType == MonsterType.Spider) ||
                    (i == 2 && provider.SelectedType == MonsterType.Bat) ||
                    (i == 3 && provider.SelectedType == MonsterType.Skeleton))
                    GUI.backgroundColor = Color.green;

                bool pressed = GUI.Button(r, names[i], buttonStyle);
                GUI.backgroundColor = old;

                if (!pressed) continue;

                switch (i)
                {
                    case 0: provider.SetType(MonsterType.Zombie); break;
                    case 1: provider.SetType(MonsterType.Spider); break;
                    case 2: provider.SetType(MonsterType.Bat); break;
                    case 3: provider.SetType(MonsterType.Skeleton); break;
                    case 4: provider.UndoLast(); break;
                    case 5: provider.ClearAll(); break;
                    case 6: provider.SetMode(ARTapSpawnPointProvider.Mode.Play); break;
                }
            }
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
