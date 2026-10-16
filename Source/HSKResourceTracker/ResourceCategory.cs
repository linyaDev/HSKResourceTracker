using System.Collections.Generic;
using System.Linq;
using Verse;

namespace HSKResourceTracker;

public class ResourceCategory : IExposable
{
    public string name = "";
    public int minimum = 500;
    public bool useMinimum = true;
    public int minVisible;
    public bool useMinVisible;
    public bool hidden;

    // Saved as strings, rebuilt into ThingFilter at runtime
    public List<string> allowedDefNames = new List<string>();

    // Runtime only — not saved
    private ThingFilter cachedFilter;
    private bool filterDirty = true;

    public ThingFilter Filter
    {
        get
        {
            if (cachedFilter == null || filterDirty)
                RebuildFilter();
            return cachedFilter;
        }
    }

    public void SetAllowed(ThingDef def, bool allow)
    {
        if (allow && !allowedDefNames.Contains(def.defName))
            allowedDefNames.Add(def.defName);
        else if (!allow)
            allowedDefNames.Remove(def.defName);
        filterDirty = true;
    }

    public bool Allows(ThingDef def)
    {
        return allowedDefNames.Contains(def.defName);
    }

    public void CopyFromFilter(ThingFilter source)
    {
        allowedDefNames.Clear();
        foreach (var def in source.AllowedThingDefs)
            allowedDefNames.Add(def.defName);
        filterDirty = true;
    }

    private void RebuildFilter()
    {
        cachedFilter = new ThingFilter();
        foreach (var defName in allowedDefNames)
        {
            var def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def != null)
                cachedFilter.SetAllow(def, true);
        }
        filterDirty = false;
    }

    public void MarkFilterDirty()
    {
        filterDirty = true;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref name, "name", "");
        Scribe_Values.Look(ref minimum, "minimum", 500);
        Scribe_Values.Look(ref useMinimum, "useMinimum", true);
        Scribe_Values.Look(ref minVisible, "minVisible", 0);
        Scribe_Values.Look(ref useMinVisible, "useMinVisible", false);
        Scribe_Values.Look(ref hidden, "hidden", false);
        Scribe_Collections.Look(ref allowedDefNames, "allowedDefNames", LookMode.Value);
        if (allowedDefNames == null)
            allowedDefNames = new List<string>();
        filterDirty = true;
    }
}
