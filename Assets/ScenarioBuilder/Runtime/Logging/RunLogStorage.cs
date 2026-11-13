using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ScenarioBuilder
{
    /// <summary>Чтение/запись журналов прохождений в JSON.</summary>
    public static class RunLogStorage
    {
        public static string Folder => Path.Combine(Application.persistentDataPath, "ScenarioRuns");

        public static string Save(RunLog run)
        {
            Directory.CreateDirectory(Folder);
            var path = Path.Combine(Folder, $"{run.runId}.json");
            File.WriteAllText(path, ToJson(run));
            return path;
        }

        public static string ToJson(RunLog run) => JsonUtility.ToJson(run, true);

        /// <summary>Загружает журнал. Возвращает null и текст ошибки, если файл повреждён.</summary>
        public static RunLog Load(string path, out string error)
        {
            error = null;
            try
            {
                var run = JsonUtility.FromJson<RunLog>(File.ReadAllText(path));
                if (run == null || run.entries == null)
                {
                    error = $"Файл {Path.GetFileName(path)} не похож на журнал прохождения.";
                    return null;
                }
                return run;
            }
            catch (Exception ex)
            {
                error = $"Не удалось прочитать {Path.GetFileName(path)}: {ex.Message}";
                return null;
            }
        }

        /// <summary>Пути сохранённых журналов, новые первыми.</summary>
        public static List<string> List()
        {
            if (!Directory.Exists(Folder)) return new List<string>();
            return Directory.GetFiles(Folder, "*.json").OrderByDescending(p => p).ToList();
        }
    }
}
