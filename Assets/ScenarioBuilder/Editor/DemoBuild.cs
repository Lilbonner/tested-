using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ScenarioBuilder.Editor
{
    /// <summary>Сборка Windows-билда демо. Из меню или из командной строки (-executeMethod).</summary>
    public static class DemoBuild
    {
        const string OutputPath = "Builds/ScenarioDemo_Windows/ScenarioDemo.exe";

        [MenuItem("Tools/Scenario/Собрать Windows-билд демо")]
        public static void BuildWindows()
        {
            if (!File.Exists(DemoSceneBuilder.ScenePath)) DemoSceneBuilder.Build();

            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { DemoSceneBuilder.ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
            Debug.Log($"[Scenario] Build {report.summary.result}: {report.summary.outputPath}, " +
                      $"{report.summary.totalSize / (1024 * 1024)} MB, errors: {report.summary.totalErrors}");
            if (Application.isBatchMode && report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        /// <summary>Командная строка: пересобрать сцену и билд.</summary>
        public static void RebuildSceneAndPlayer()
        {
            DemoSceneBuilder.Build();
            BuildWindows();
        }

        public static void RebuildSceneOnly() => DemoSceneBuilder.Build();
    }
}
