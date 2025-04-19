using UnityEngine;

[CreateAssetMenu(fileName = "NewBlendShapePreset", menuName = "BlendShape Preset")]
public class BlendShapePreset : ScriptableObject
{
    public BlendShapeEntry[] entries;

    [System.Serializable]
    public class BlendShapeEntry
    {
        public string name;
        public float weight;
    }
}
