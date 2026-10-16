using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace HSKResourceTracker;

public class Dialog_ResourceSettings : Window
{
    private Vector2 scrollPosition;
    private int editingIndex = -1;
    private ThingFilterUI.UIState filterState = new ThingFilterUI.UIState();

    // Drag-and-drop
    private int dragIndex = -1;
    private bool isDragging;
    private float dragOffsetY;

    // Parent filter — allows everything, used as reference for ThingFilterUI
    private static ThingFilter parentFilter;
    private static ThingFilter ParentFilter
    {
        get
        {
            if (parentFilter == null)
            {
                parentFilter = new ThingFilter();
                parentFilter.SetAllow(ThingCategoryDefOf.Root, true);
            }
            return parentFilter;
        }
    }

    public override Vector2 InitialSize => new Vector2(560f, 600f);

    public Dialog_ResourceSettings()
    {
        doCloseButton = true;
        doCloseX = true;
        draggable = true;
        absorbInputAroundWindow = false;
    }

    public override void DoWindowContents(Rect inRect)
    {
        var settings = HSKResourceTrackerMod.Settings;
        if (settings == null) return;

        if (editingIndex >= 0 && editingIndex < settings.categories.Count)
        {
            doCloseButton = false;
            DrawCategoryEditor(inRect, settings.categories[editingIndex]);
            return;
        }
        doCloseButton = true;

        // Title
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "RT_SettingsCategory".Translate());
        Text.Font = GameFont.Small;

        float y = 40f;

        // Hide resource readout checkbox
        bool hideReadout = settings.hideResourceReadout;
        Widgets.CheckboxLabeled(new Rect(0f, y, inRect.width, 24f), "RT_HideReadout".Translate(), ref hideReadout);
        if (hideReadout != settings.hideResourceReadout)
        {
            settings.hideResourceReadout = hideReadout;
            Patch_ResourceReadout.hideReadout = hideReadout;
        }
        y += 30f;

        // Add category button
        if (Widgets.ButtonText(new Rect(0f, y, 200f, 28f), "RT_AddCategory".Translate()))
        {
            settings.categories.Add(new ResourceCategory { name = "RT_NewCategory".Translate() });
            editingIndex = settings.categories.Count - 1;
        }
        y += 36f;

        // Category list
        Rect listRect = new Rect(0f, y, inRect.width, inRect.height - y - 50f);
        float totalH = settings.categories.Count * 34f;
        Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, totalH);

        Widgets.BeginScrollView(listRect, ref scrollPosition, viewRect);
        float rowY = 0f;

        // Fixed column layout
        const float checkX = 4f;
        const float checkW = 24f;
        const float nameX = 32f;
        const float nameW = 150f;
        const float infoX = 186f;
        const float infoW = 80f;
        float editX = viewRect.width - (26f * 2 + 2f + 4f);
        const float btnW = 60f;
        const float btnH = 24f;
        const float arrowW = 26f;
        const float dragHandleW = 20f;
        const float pad = 4f;
        const float rowH = 30f;
        const float fullRowH = rowH + pad;

        // Handle drag events
        Event ev = Event.current;
        int dropTarget = -1;
        bool dropNow = false;
        if (isDragging && ev.type == EventType.MouseUp)
        {
            dropNow = true;
            ev.Use();
        }

        for (int i = 0; i < settings.categories.Count; i++)
        {
            // Skip drawing dragged item in its original position
            if (isDragging && i == dragIndex)
            {
                rowY += fullRowH;
                continue;
            }

            var cat = settings.categories[i];
            Rect row = new Rect(0f, rowY, viewRect.width, rowH);

            // Drop target highlight
            if (isDragging && i != dragIndex)
            {
                Vector2 mouseInView = ev.mousePosition;
                if (mouseInView.y >= rowY && mouseInView.y < rowY + fullRowH)
                {
                    dropTarget = i;
                    Widgets.DrawBoxSolid(new Rect(0f, rowY - 2f, viewRect.width, 3f), new Color(0.3f, 0.7f, 1f, 0.8f));
                }
            }

            if (i % 2 == 0)
                Widgets.DrawBoxSolid(row, new Color(0.15f, 0.15f, 0.15f, 0.4f));

            float btnY = rowY + 3f;

            // Drag handle (≡)
            Rect handleRect = new Rect(0f, rowY, dragHandleW, rowH);
            GUI.color = Mouse.IsOver(handleRect) ? Color.white : new Color(1f, 1f, 1f, 0.4f);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(handleRect, "≡");
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            if (!isDragging && ev.type == EventType.MouseDown && ev.button == 0 && Mouse.IsOver(handleRect))
            {
                isDragging = true;
                dragIndex = i;
                dragOffsetY = ev.mousePosition.y - rowY;
                ev.Use();
            }

            // Visibility toggle
            bool visible = !cat.hidden;
            Widgets.Checkbox(new Vector2(checkX + dragHandleW, btnY), ref visible, 18f);
            TooltipHandler.TipRegion(new Rect(checkX + dragHandleW, btnY, checkW, btnH), "RT_ToggleVisibility".Translate());
            if (visible == cat.hidden)
            {
                cat.hidden = !visible;
                Find.CurrentMap?.GetComponent<MapComponent_ResourceTracker>()?.Recalculate();
            }

            // Name
            if (cat.hidden)
                GUI.color = new Color(1f, 1f, 1f, 0.4f);
            Widgets.Label(new Rect(nameX + dragHandleW, rowY, nameW, rowH), cat.name);
            GUI.color = Color.white;

            // Info
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = new Color(1f, 1f, 1f, 0.5f);
            Widgets.Label(new Rect(infoX + dragHandleW, rowY, infoW, rowH),
                cat.allowedDefNames.Count + " " + "RT_Items".Translate());
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            // Preview icons (up to 4)
            float iconX = infoX + infoW + 4f + dragHandleW;
            const float iconSz = 18f;
            int shown = 0;
            foreach (var defName in cat.allowedDefNames)
            {
                if (shown >= 4) break;
                var def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                if (def?.uiIcon == null || def.uiIcon == BaseContent.BadTex) continue;
                Rect iconRect = new Rect(iconX, rowY + 3f, iconSz, iconSz);
                Widgets.ThingIcon(iconRect, def);
                if (Mouse.IsOver(iconRect))
                    TooltipHandler.TipRegion(iconRect, def.LabelCap);
                iconX += iconSz + 2f;
                shown++;
            }
            if (cat.allowedDefNames.Count > 4)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.5f);
                Widgets.Label(new Rect(iconX, rowY, 20f, rowH), "...");
                GUI.color = Color.white;
            }

            // Edit button
            float x = editX;
            if (Widgets.ButtonText(new Rect(x, btnY, arrowW, btnH), "..."))
                editingIndex = i;
            x += arrowW + 2f;

            // Delete button [x]
            if (Widgets.ButtonText(new Rect(x, btnY, arrowW, btnH), "✕"))
            {
                settings.categories.RemoveAt(i);
                if (dragIndex == i) { isDragging = false; dragIndex = -1; }
                i--;
                rowY += fullRowH;
                continue;
            }

            rowY += fullRowH;
        }

        // Draw dragged item ghost
        if (isDragging && dragIndex >= 0 && dragIndex < settings.categories.Count)
        {
            float ghostY = ev.mousePosition.y - dragOffsetY;
            var dragCat = settings.categories[dragIndex];
            Widgets.DrawBoxSolid(new Rect(0f, ghostY, viewRect.width, rowH), new Color(0.2f, 0.3f, 0.5f, 0.6f));
            GUI.color = Color.white;
            Widgets.Label(new Rect(dragHandleW + nameX, ghostY, nameW, rowH), dragCat.name);
        }

        // Apply drop
        if (dropNow && dragIndex >= 0)
        {
            if (dropTarget >= 0 && dropTarget != dragIndex)
            {
                var item = settings.categories[dragIndex];
                settings.categories.RemoveAt(dragIndex);
                if (dropTarget > dragIndex) dropTarget--;
                settings.categories.Insert(dropTarget, item);
            }
            isDragging = false;
            dragIndex = -1;
        }

        Widgets.EndScrollView();
    }

    private void DrawCategoryEditor(Rect inRect, ResourceCategory cat)
    {
        // Back button
        if (Widgets.ButtonText(new Rect(0f, 0f, 120f, 28f), "RT_Back".Translate()))
        {
            cat.CopyFromFilter(cat.Filter);
            editingIndex = -1;
            Find.CurrentMap?.GetComponent<MapComponent_ResourceTracker>()?.Recalculate();
            return;
        }

        float y = 36f;

        // Category name
        Widgets.Label(new Rect(0f, y, 80f, 24f), "RT_CategoryName".Translate());
        cat.name = Widgets.TextField(new Rect(85f, y, 200f, 24f), cat.name);
        y += 30f;

        // --- Shortage mode (collapsed) ---
        GUI.color = new Color(1f, 1f, 1f, 0.6f);
        Widgets.DrawLineHorizontal(0f, y + 2f, inRect.width);
        GUI.color = Color.white;
        y += 6f;
        Text.Font = GameFont.Tiny;
        GUI.color = new Color(1f, 1f, 0.7f);
        Widgets.Label(new Rect(0f, y, inRect.width, 20f), "RT_ShortageMode".Translate());
        GUI.color = Color.white;
        Text.Font = GameFont.Small;
        y += 22f;

        bool useMin = cat.useMinimum;
        Widgets.CheckboxLabeled(new Rect(0f, y, 200f, 24f), "RT_UseMinimum".Translate(), ref useMin);
        cat.useMinimum = useMin;

        if (cat.useMinimum)
        {
            string minBuf = cat.minimum.ToString();
            minBuf = Widgets.TextField(new Rect(210f, y, 80f, 24f), minBuf);
            if (int.TryParse(minBuf, out int newMin))
                cat.minimum = Mathf.Max(0, newMin);
        }
        y += 30f;

        // --- Storage mode (expanded) ---
        GUI.color = new Color(1f, 1f, 1f, 0.6f);
        Widgets.DrawLineHorizontal(0f, y + 2f, inRect.width);
        GUI.color = Color.white;
        y += 6f;
        Text.Font = GameFont.Tiny;
        GUI.color = new Color(0.7f, 1f, 0.7f);
        Widgets.Label(new Rect(0f, y, inRect.width, 20f), "RT_StorageMode".Translate());
        GUI.color = Color.white;
        Text.Font = GameFont.Small;
        y += 22f;

        bool useMinVis = cat.useMinVisible;
        Widgets.CheckboxLabeled(new Rect(0f, y, 200f, 24f), "RT_UseMinVisible".Translate(), ref useMinVis);
        cat.useMinVisible = useMinVis;

        if (cat.useMinVisible)
        {
            string visBuf = cat.minVisible.ToString();
            visBuf = Widgets.TextField(new Rect(210f, y, 80f, 24f), visBuf);
            if (int.TryParse(visBuf, out int newVis))
                cat.minVisible = Mathf.Max(0, newVis);
        }
        y += 30f;

        // ThingFilter tree
        Rect filterRect = new Rect(0f, y, inRect.width, inRect.height - y - 50f);
        ThingFilterUI.DoThingFilterConfigWindow(filterRect, filterState, cat.Filter, ParentFilter);

        // Apply button (bottom center)
        float applyW = 150f;
        float applyH = 40f;
        if (Widgets.ButtonText(new Rect((inRect.width - applyW) / 2f, inRect.height - applyH, applyW, applyH), "RT_Apply".Translate()))
        {
            cat.CopyFromFilter(cat.Filter);
            editingIndex = -1;
            Find.CurrentMap?.GetComponent<MapComponent_ResourceTracker>()?.Recalculate();
        }
    }

    public override void PreClose()
    {
        base.PreClose();
        // Sync filter selections back to string lists
        var settings = HSKResourceTrackerMod.Settings;
        if (settings != null)
        {
            foreach (var cat in settings.categories)
                cat.CopyFromFilter(cat.Filter);
            settings.Write();
        }
        Find.CurrentMap?.GetComponent<MapComponent_ResourceTracker>()?.Recalculate();
    }
}
