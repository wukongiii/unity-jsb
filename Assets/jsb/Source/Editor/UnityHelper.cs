using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace QuickJS.Editor
{
    using UnityEngine;
    using UnityEditor;

    [Serializable]
    public class TSConfig
    {
        [Serializable]
        public class CompilerOptions
        {
            public string module;
            public string target;
            public string sourceRoot;
            public string outDir;
            public string outFile;
            public string[] typeRoots;
            public string moduleResolution;
            public string[] types;
            public bool listEmittedFiles;
            public bool experimentalDecorators;
            public bool noImplicitAny;
            public bool allowJs;
            public bool inlineSourceMap;
            public bool sourceMap;
        }
        public CompilerOptions compilerOptions;
        public bool compileOnSave;
        public string[] include;
        public string[] exclude;
    }

    [InitializeOnLoad]
    public static class UnityHelper
    {
        #region All Menu Items
        [MenuItem("JS Bridge/Generate Bindings with d.ts")]
        public static void GenerateBindings()
        {
            var bm = new BindingManager(Prefs.Load());
            bm.Collect();
            bm.Generate(true);
            bm.Cleanup();
            bm.Report();
            AssetDatabase.Refresh();
        }

        [MenuItem("JS Bridge/Generate Bindings without d.ts")]
        public static void GenerateBindingsWithoutTSD()
        {
            var bm = new BindingManager(Prefs.Load());
            bm.Collect();
            bm.Generate(false);
            bm.Cleanup();
            bm.Report();
            AssetDatabase.Refresh();
        }

        // [MenuItem("JS Bridge/Compile TypeScript")]
        public static void CompileScripts()
        {
            Debug.Log("compiling typescript source...");
            EditorApplication.delayCall += () =>
            {
                EditorApplication.delayCall += () =>
                {
#if UNITY_EDITOR_WIN
                    string command = "tsc.cmd";
#else
                    string command = "tsc";
#endif
                    var exitCode = ShellHelper.Run(command, "", 30);
                    Debug.Log($"{command}: {exitCode}");
                };
            };
        }

        [MenuItem("JS Bridge/Clear")]
        public static void ClearBindings()
        {
            var prefs = Prefs.Load();
            var kv = new Dictionary<string, List<string>>();
            foreach (var dir in prefs.cleanupDir)
            {
                var pdir = Prefs.ReplacePathVars(dir);
                kv[pdir] = new List<string>();
            }
            BindingManager.Cleanup(kv, null);
            AssetDatabase.Refresh();
        }

        [MenuItem("JS Bridge/Prefs ...", false, 5001)]
        public static void OpenPrefsEditor()
        {
            EditorWindow.GetWindow<PrefsEditor>().Show();
        }

        [MenuItem("Assets/JS Bridge/Compile (bytecode)", true)]
        public static bool CompileBytecodeValidate()
        {
            var objects = Selection.objects;
            for (var i = 0; i < objects.Length; ++i)
            {
                var obj = objects[i];
                var assetPath = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".js"))
                {
                    return true;
                }
            }
            return false;
        }

        [MenuItem("Assets/JS Bridge/Compile (bytecode)")]
        public static void CompileBytecode()
        {
            using (var ctx = new ScriptCompiler())
            {
                var objects = Selection.objects;
                for (var i = 0; i < objects.Length; ++i)
                {
                    var obj = objects[i];
                    var assetPath = AssetDatabase.GetAssetPath(obj);
                    if (!string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".js"))
                    {
                        var outPath = assetPath + ".bytes";
                        var bytes = File.ReadAllBytes(assetPath);
                        var bytecode = ctx.Compile(assetPath, bytes);
                        if (bytecode != null)
                        {
                            File.WriteAllBytes(outPath, bytecode);
                            Debug.LogFormat("compile {0}({1}) => {2}({3})", assetPath, bytes.Length, outPath, bytecode.Length);
                        }
                        else
                        {
                            Debug.LogErrorFormat("compilation failed: {0}", assetPath);
                            if (File.Exists(outPath))
                            {
                                File.Delete(outPath);
                            }
                        }
                    }
                }
            }
            AssetDatabase.Refresh();
        }

        #endregion

        static UnityHelper()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange playModeStateChange)
        {
            if (playModeStateChange == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += () => ScriptEngine.Destroy();
            }
        }
    }
}