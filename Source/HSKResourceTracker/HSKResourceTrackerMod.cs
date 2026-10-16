using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace HSKResourceTracker;

public class HSKResourceTrackerSettings : ModSettings
{
    public float widgetX = -1f;
    public float widgetY = -1f;
    public bool hideResourceReadout;
    public List<ResourceCategory> categories = new List<ResourceCategory>();

    public override void ExposeData()
    {
        Scribe_Values.Look(ref widgetX, "widgetX", -1f);
        Scribe_Values.Look(ref widgetY, "widgetY", -1f);
        Scribe_Values.Look(ref hideResourceReadout, "hideResourceReadout");
        Scribe_Collections.Look(ref categories, "categories", LookMode.Deep);
        if (categories == null)
            categories = new List<ResourceCategory>();
        base.ExposeData();
    }
}

public class HSKResourceTrackerMod : Mod
{
    public static HSKResourceTrackerSettings Settings;

    public HSKResourceTrackerMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<HSKResourceTrackerSettings>();
    }

    public override string SettingsCategory() => null;
}
