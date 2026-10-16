using System.Collections.Generic;
using Verse;

namespace HSKResourceTracker;

public struct CategoryCount
{
    public string name;
    public int count;
    public int minimum;
    public int minVisible;
    public List<ItemCount> items;
}

public struct ItemCount
{
    public string defName;
    public string label;
    public int count;
}

public class MapComponent_ResourceTracker : MapComponent
{
    private int updateCounter;
    private const int UpdateInterval = 2500; // every game hour
    public List<CategoryCount> CachedCounts = new List<CategoryCount>();

    private bool firstTick = true;

    public MapComponent_ResourceTracker(Map map) : base(map)
    {
    }

    public override void MapComponentTick()
    {
        if (firstTick)
        {
            firstTick = false;
            Recalculate();
        }

        updateCounter++;
        if (updateCounter < UpdateInterval)
            return;
        updateCounter = 0;
        Recalculate();
    }

    public void Recalculate()
    {
        CachedCounts.Clear();
        var settings = HSKResourceTrackerMod.Settings;
        if (settings == null)
            return;

        foreach (var cat in settings.categories)
        {
            if (cat.hidden)
                continue;
            var cc = new CategoryCount
            {
                name = cat.name,
                minimum = cat.useMinimum ? cat.minimum : 0,
                minVisible = cat.useMinVisible ? cat.minVisible : 0,
                items = new List<ItemCount>()
            };

            int total = 0;
            foreach (var def in cat.Filter.AllowedThingDefs)
            {
                int count = map.resourceCounter.GetCount(def);
                if (count > 0)
                {
                    cc.items.Add(new ItemCount
                    {
                        defName = def.defName,
                        label = def.LabelCap,
                        count = count
                    });
                }
                total += count;
            }

            cc.count = total;
            CachedCounts.Add(cc);
        }
    }
}
