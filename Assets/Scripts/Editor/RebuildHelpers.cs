using ChimeralTheater.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChimeralTheater.Editor
{
    /// <summary>
    /// Batch entry points for forcing prefab and scene regeneration from scripts
    /// (see Ignore/rebuild-prefab.sh). Runs the same code paths the delayCall
    /// builders use, but always rebuilds rather than only when missing.
    /// </summary>
    public static class RebuildHelpers
    {
        public static void RebuildAll()
        {
            // Batch sessions start with an untitled scene; open the real one first.
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);

            VRDashboardPrefabBuilder.CreatePrefab();
            RecordingHUDPrefabBuilder.CreatePrefab();
            RecordingHUDPrefabBuilder.SetupSceneInstance(
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/RecordingHUD.prefab"));
            VRDashboardPrefabBuilder.SetupSceneInstance(
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VRDashboard.prefab"));

            AssetDatabase.SaveAssets();
            EditorApplication.Exit(0);
        }
    }
}
