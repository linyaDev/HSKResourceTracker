using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace HSKResourceTracker;

[HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls))]
public static class Patch_PlaySettings
{
    private static Texture2D iconTexture;
    private static Texture2D Icon => iconTexture ??= ContentFinder<Texture2D>.Get("UI/Buttons/ResourceReadoutCategorized", true);

    public static void Postfix(WidgetRow row, bool worldView)
    {
        if (worldView)
            return;

        row.ToggleableIcon(ref ResourceTrackerOverlay.visible, Icon,
            "RT_ToggleWidget".Translate(), SoundDefOf.Mouseover_ButtonToggle);
    }
}
