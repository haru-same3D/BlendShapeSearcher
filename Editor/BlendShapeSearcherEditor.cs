using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(SkinnedMeshRenderer))]
[CanEditMultipleObjects]
public class BlendShapeSearchSliderEditor : Editor
{
    private Editor defaultInspector;
    private string searchKeyword = "";
    private bool showBlendShapes = true;
    private bool showFavoritesOnly = false;
    private HashSet<string> favorites = new HashSet<string>();
    private BlendShapePreset selectedPreset;

    void OnEnable()
    {
        var inspectorType = typeof(Editor).Assembly.GetType("UnityEditor.SkinnedMeshRendererEditor");
        defaultInspector = CreateEditor(targets, inspectorType);
    }

    void OnDisable()
    {
        if (defaultInspector)
            DestroyImmediate(defaultInspector);
    }

    public override void OnInspectorGUI()
    {
        if (defaultInspector != null)
            defaultInspector.OnInspectorGUI();

        EditorGUILayout.Space();
        showBlendShapes = EditorGUILayout.Foldout(showBlendShapes, "🔍 ブレンドシェイプ検索・操作", true, EditorStyles.foldoutHeader);
        if (!showBlendShapes) return;

        EditorGUILayout.BeginHorizontal();
        searchKeyword = EditorGUILayout.TextField("キーワード", searchKeyword);
        showFavoritesOnly = GUILayout.Toggle(showFavoritesOnly, "★お気に入りのみ", "Button", GUILayout.Width(110));
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("🔄 すべてのブレンドシェイプをリセット"))
        {
            foreach (var obj in targets)
            {
                var smr = (SkinnedMeshRenderer)obj;
                Undo.RecordObject(smr, "Reset All BlendShapes");
                var mesh = smr.sharedMesh;
                for (int i = 0; i < mesh.blendShapeCount; i++)
                    smr.SetBlendShapeWeight(i, 0f);
                EditorUtility.SetDirty(smr);
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("💾 プリセット", EditorStyles.boldLabel);
        selectedPreset = (BlendShapePreset)EditorGUILayout.ObjectField("プリセットを選択", selectedPreset, typeof(BlendShapePreset), false);
        if (selectedPreset != null)
        {
            if (GUILayout.Button("📤 プリセットを読み込む"))
            {
                if (EditorUtility.DisplayDialog("確認", "現在の設定は上書きされます。プリセットを読み込みますか？", "はい", "キャンセル"))
                {
                    ApplyPreset((SkinnedMeshRenderer)target, selectedPreset);
                }
            }
        }
        if (GUILayout.Button("📥 現在の状態をプリセットに保存（新規作成）"))
            CreatePresetFromCurrentState((SkinnedMeshRenderer)target);

        var smrTarget = (SkinnedMeshRenderer)target;
        var meshTarget = smrTarget.sharedMesh;
        if (meshTarget == null)
        {
            EditorGUILayout.HelpBox("SkinnedMeshRenderer に Mesh が設定されていません。", MessageType.Warning);
            return;
        }

        int count = meshTarget.blendShapeCount;
        bool foundAny = false;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("🎛️ ブレンドシェイプ", EditorStyles.boldLabel);

        for (int i = 0; i < count; i++)
        {
            string name = meshTarget.GetBlendShapeName(i);
            bool isFav = favorites.Contains(name);

            if (!string.IsNullOrEmpty(searchKeyword))
            {
                var keys = searchKeyword.ToLower().Split(' ');
                if (!keys.All(k => name.ToLower().Contains(k)))
                    continue;
            }

            if (showFavoritesOnly && !isFav) continue;
            foundAny = true;

            EditorGUILayout.BeginHorizontal();
            float cur = smrTarget.GetBlendShapeWeight(i);
            float newVal = EditorGUILayout.Slider(name, cur, 0f, 100f);

            var origColor = GUI.color;
            if (isFav) GUI.color = new Color(1f, 0.4f, 0.7f);
            if (GUILayout.Button(isFav ? "★" : "☆", GUILayout.Width(30)))
            {
                if (isFav) favorites.Remove(name);
                else favorites.Add(name);
            }
            GUI.color = origColor;
            EditorGUILayout.EndHorizontal();

            if (!Mathf.Approximately(cur, newVal))
            {
                Undo.RecordObject(smrTarget, "Change BlendShape Weight");
                smrTarget.SetBlendShapeWeight(i, newVal);
                EditorUtility.SetDirty(smrTarget);
            }
        }

        if (!foundAny)
            EditorGUILayout.LabelField("一致するブレンドシェイプが見つかりませんでした。");
    }

    private void CreatePresetFromCurrentState(SkinnedMeshRenderer smr)
    {
        var mesh = smr.sharedMesh;
        int count = mesh.blendShapeCount;
        var preset = ScriptableObject.CreateInstance<BlendShapePreset>();
        preset.entries = new BlendShapePreset.BlendShapeEntry[count];
        for (int i = 0; i < count; i++)
        {
            preset.entries[i] = new BlendShapePreset.BlendShapeEntry
            {
                name = mesh.GetBlendShapeName(i),
                weight = smr.GetBlendShapeWeight(i)
            };
        }
        string path = EditorUtility.SaveFilePanelInProject("プリセット保存", "NewBlendShapePreset", "asset", "保存先を選択してください");
        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(preset, path);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = preset;
        }
    }

    private void ApplyPreset(SkinnedMeshRenderer smr, BlendShapePreset preset)
    {
        var mesh = smr.sharedMesh;
        foreach (var entry in preset.entries)
        {
            int idx = GetBlendShapeIndexByName(mesh, entry.name);
            if (idx >= 0)
                smr.SetBlendShapeWeight(idx, entry.weight);
        }
        EditorUtility.SetDirty(smr);
    }

    private int GetBlendShapeIndexByName(Mesh mesh, string name)
    {
        for (int i = 0; i < mesh.blendShapeCount; i++)
            if (mesh.GetBlendShapeName(i) == name) return i;
        return -1;
    }
}
