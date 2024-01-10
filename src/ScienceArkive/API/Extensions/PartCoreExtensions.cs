using KSP.Sim.Definitions;

namespace ScienceArkive.API.Extensions;

public static class PartCoreExtensions
{
    /// <summary>
    /// Returns the ModuleData of the given type for the given part, or null if not found.
    /// Used to get the ScienceExperiment module from a part (Data_ScienceExperiment).   
    /// </summary>
    /// <param name="part"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T? GetModuleData<T>(this PartCore part) where T : ModuleData
    {
        if (part.modules == null) return null;

        foreach (var moduleData in part.modules)
            if (moduleData is T data)
                return data;

        return null;
    }

    /// <summary>
    /// Returns the _first_ serialized ModuleData of the given type for the given part, or null if not found.
    /// TODO: Should we return a list of all serialized ModuleData of the given type?
    /// </summary>
    /// <param name="part"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T? GetSerializedModuleData<T>(this PartCore part) where T : ModuleData
    {
        if (part.data.serializedPartModules == null) return null;

        foreach (var serializePartModule in part.data.serializedPartModules)
        foreach (var serializedModuleData in serializePartModule.ModuleData)
            if (serializedModuleData.DataObject is T data)
                return data;

        return null;
    }
}