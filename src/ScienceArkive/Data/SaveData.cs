using JetBrains.Annotations;
using UnityEngine;

namespace ScienceArkive.Data;

public class SaveData
{
    public string SessionGuidString;
    public Vector3? WindowPosition;
    [CanBeNull] public string SelectedBody;

    public Dictionary<string, Dictionary<string, bool>> OpenedExperiments = new();
}