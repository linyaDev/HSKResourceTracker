using HarmonyLib;
using Verse;

namespace HSKResourceTracker;

[StaticConstructorOnStartup]
public static class HSKResourceTrackerInit
{
    static HSKResourceTrackerInit()
    {
        var harmony = new Harmony("linya.hskresourcetracker");
        harmony.PatchAll();
        Patch_ResourceReadout.hideReadout = HSKResourceTrackerMod.Settings?.hideResourceReadout ?? false;
        Log.Message("[HSKResourceTracker] Loaded.");
    }
}
