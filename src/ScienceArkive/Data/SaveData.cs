#nullable enable
using JetBrains.Annotations;
using UnityEngine;

namespace ScienceArkive.Data;

public class SaveData
{
    public string? SessionGuidString;
    public Vector3? WindowPosition;
    public string? SelectedBody;

    /// <summary>
    /// Using TravelFirsts from KSP2 is not sufficient because some times a body will be skipped 
    /// </summary>
    public List<string>? DiscoveredBodies = [];

    public Dictionary<string, Dictionary<string, bool>> OpenedExperiments = new();
}