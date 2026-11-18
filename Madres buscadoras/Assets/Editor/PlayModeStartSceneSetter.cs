#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Configura automaticamente a Unity para que al presionar el boton Play,
/// siempre inicie en la escena del Menu de Inicio (Assets/Scenes/MenuInicio.unity)
/// y al hacer clic en START pase a Nivel_1.
/// </summary>
[InitializeOnLoad]
public static class PlayModeStartSceneSetter
{
    static PlayModeStartSceneSetter()
    {
        EditorApplication.delayCall += EnsureStartSceneConfigured;
    }

    [MenuItem("Madres Buscadoras/Configuracion/Asegurar Inicio en Menu al dar Play")]
    public static void EnsureStartSceneConfigured()
    {
        // Forzar importacion de los nuevos sprites de inicio y del personaje
        AssetDatabase.ImportAsset("Assets/Sprites/UI/pantalla_inicio_decorada.png", ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/Sprites/UI/btn_jugar_decorado.png", ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/Sprites/Props/prop_pico_brazos.png", ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/Sprites/Personajes/esperanza_sheet.png", ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/Sprites/Personajes/enemigo_presidenta.png", ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/Resources/enemigo_presidenta.png", ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/Audio/sonido_desaparecidos_mexico.wav", ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/Resources/Audio/sonido_desaparecidos_mexico.wav", ImportAssetOptions.ForceUpdate);

        SceneAsset menuScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/MenuInicio.unity");
        if (menuScene != null)
        {
            if (EditorSceneManager.playModeStartScene != menuScene)
            {
                EditorSceneManager.playModeStartScene = menuScene;
                Debug.Log("[PlayModeStartSceneSetter] Unity ahora iniciara siempre desde: Assets/Scenes/MenuInicio.unity");
            }
        }

        // Asegurar que el enemigo Presidenta esté presente en Nivel_1
        string activeSceneName = EditorSceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(activeSceneName) || activeSceneName == "Nivel_1")
        {
            SceneSetupTool.SetupPresidentEnemy();
        }
    }
}
#endif
