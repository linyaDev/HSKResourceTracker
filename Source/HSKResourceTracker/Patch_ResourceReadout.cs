using HarmonyLib;
using RimWorld;

namespace HSKResourceTracker;

[HarmonyPatch(typeof(ResourceReadout), nameof(ResourceReadout.ResourceReadoutOnGUI))]
public static class Patch_ResourceReadout
{
    public static bool hideReadout;

    public static bool Prefix()
    {
        return !hideReadout;
    }
}
