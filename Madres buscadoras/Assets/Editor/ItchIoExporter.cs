#if UNITY_EDITOR
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Herramienta de publicación directa para Itch.io (Web HTML5 / WebGL / Windows).
/// </summary>
public static class ItchIoExporter
{
    [MenuItem("Itch.io/1. Abrir Itch.io y Mostrar ZIP Web Listo para Subir", false, 10)]
    public static void OpenItchIoAndShowZip()
    {
        string rootDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
        string zipPath = Path.Combine(rootDir, "MadresBuscadoras_Web_ItchIo.zip");

        if (File.Exists(zipPath))
        {
            EditorUtility.RevealInFinder(zipPath);
        }

        Application.OpenURL("https://itch.io/game/new");
        Debug.Log("[Itch.io] Abriendo https://itch.io/game/new y mostrando MadresBuscadoras_Web_ItchIo.zip");
    }

    [MenuItem("Itch.io/2. Compilar Unity WebGL para Itch.io", false, 20)]
    public static void BuildUnityWebGLForItchIo()
    {
        string rootDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
        string buildDir = Path.Combine(rootDir, "Build_Unity_WebGL");
        string zipPath = Path.Combine(rootDir, "MadresBuscadoras_UnityWebGL_ItchIo.zip");

        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
        {
            EditorUtility.DisplayDialog(
                "Paquete Web Ya Listo",
                "Ya tienes el archivo 'MadresBuscadoras_Web_ItchIo.zip' listo en tu carpeta para subir a https://itch.io/game/new (Kind of project: HTML).\n\n(Si deseas compilar con el motor WebGL nativo de Unity, instala el módulo 'WebGL Build Support' en Unity Hub).",
                "Abrir ZIP e Itch.io"
            );
            OpenItchIoAndShowZip();
            return;
        }

        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        string[] scenes = new string[] { "Assets/Nivel_1.unity" };

        BuildReport report = BuildPipeline.BuildPlayer(scenes, buildDir, BuildTarget.WebGL, BuildOptions.None);
        if (report.summary.result == BuildResult.Succeeded)
        {
            if (File.Exists(zipPath)) File.Delete(zipPath);
            ZipFile.CreateFromDirectory(buildDir, zipPath);
            EditorUtility.RevealInFinder(zipPath);
            Application.OpenURL("https://itch.io/game/new");
        }
    }
}
#endif

