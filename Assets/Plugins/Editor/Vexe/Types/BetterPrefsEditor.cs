using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using System.Linq;
#endif

namespace Vexe.Runtime.Types
{
    public class BetterPrefsEditor {

#if UNITY_EDITOR
        static BetterPrefs instance;
        public static BetterPrefs GetEditorInstance()
        {
            if (instance == null || !AssetDatabase.Contains(instance))
            {
                var dirs = Directory.GetDirectories("Assets", "Vexe", SearchOption.AllDirectories);
                var editorDir = dirs.FirstOrDefault(x => Directory.GetParent(x).Name == "Editor");
                var prefsDir = Path.Combine(editorDir, "ScriptableAssets");
                if (editorDir == null || !Directory.Exists(prefsDir))
                {
                    Debug.LogError("Unable to create editor prefs asset at Editor/Vexe/ScriptableAssets (couldn't find folder). Please make sure that path exists 'somewhere' in your project");
                    return instance ?? (instance = ScriptableObject.CreateInstance<BetterPrefs>());
                }

                var path = Path.Combine(prefsDir, "BetterEditorPrefs.asset");
                instance = AssetDatabase.LoadAssetAtPath<BetterPrefs>(path);
                if (instance == null)
                {
                    instance = ScriptableObject.CreateInstance<BetterPrefs>();
                    AssetDatabase.CreateAsset(instance, path);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    AssetDatabase.Refresh();
                }
            }

            if (instance.Ints == null) instance.Ints = new BetterPrefs.LookupIntInt();
            if (instance.Strings == null) instance.Strings = new BetterPrefs.LookupIntString();
            if (instance.Floats == null) instance.Floats = new BetterPrefs.LookupIntFloat();
            if (instance.Bools == null) instance.Bools = new BetterPrefs.LookupIntBool();
            if (instance.Colors == null) instance.Colors = new BetterPrefs.LookupIntColor();
            if (instance.Vector3s == null) instance.Vector3s = new BetterPrefs.LookupIntVector3();

            return instance;
        }
#endif
    }
}
