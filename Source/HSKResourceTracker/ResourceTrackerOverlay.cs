using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace HSKResourceTracker;

public class ResourceTrackerOverlay : GameComponent
{
    private static readonly Color Green = new Color(0.3f, 0.9f, 0.3f);
    private static readonly Color Yellow = new Color(0.9f, 0.9f, 0.3f);
    private static readonly Color Red = new Color(0.9f, 0.3f, 0.3f);
    private static readonly Color BgColor = new Color(0.1f, 0.1f, 0.1f, 0.7f);
    private static readonly Color DimText = new Color(1f, 1f, 1f, 0.5f);

    public static bool visible = true;
    private static bool expanded;
    private static bool dragging;
    private static Vector2 dragOffset;
    private static Vector2 scrollPosition;
    private static HashSet<int> expandedCategories = new HashSet<int>();
    private const float Width = 180f;
    private const float MaxHeight = 400f;
    private const float LineH = 18f;
    private const float ItemLineH = 16f;
    private const float HeaderH = 22f;

    private static Vector2 WidgetPos
    {
        get
        {
            var s = HSKResourceTrackerMod.Settings;
            if (s == null) return new Vector2(4f, 70f);
            return new Vector2(s.widgetX, s.widgetY);
        }
        set
        {
            var s = HSKResourceTrackerMod.Settings;
            if (s == null) return;
            s.widgetX = value.x;
            s.widgetY = value.y;
        }
    }

    public ResourceTrackerOverlay(Game game) : base()
    {
    }

    public override void GameComponentOnGUI()
    {
        if (!visible || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null)
            return;

        var tracker = Find.CurrentMap.GetComponent<MapComponent_ResourceTracker>();
        if (tracker == null)
            return;

        var pos = WidgetPos;
        if (pos.x < 0f)
        {
            pos = new Vector2(4f, 70f);
            WidgetPos = pos;
        }

        // Calculate visible lines
        int visibleLines = 0;
        int expandedItems = 0;
        for (int i = 0; i < tracker.CachedCounts.Count; i++)
        {
            var cc = tracker.CachedCounts[i];
            if (expanded || cc.count < cc.minimum)
            {
                visibleLines++;
                if (expandedCategories.Contains(i) && cc.items != null)
                {
                    foreach (var item in cc.items)
                    {
                        if (cc.minVisible > 0 && item.count < cc.minVisible)
                            continue;
                        expandedItems++;
                    }
                }
            }
        }

        float contentH = HeaderH + visibleLines * LineH + expandedItems * ItemLineH + 4f;
        bool needsScroll = contentH > MaxHeight;
        float height = needsScroll ? MaxHeight : contentH;
        Rect widgetRect = new Rect(pos.x, pos.y, Width, height);

        // Dragging (right-click)
        if (Event.current.type == EventType.MouseDown && Mouse.IsOver(widgetRect) && Event.current.button == 1)
        {
            dragging = true;
            dragOffset = Event.current.mousePosition - pos;
            Event.current.Use();
        }
        if (dragging)
        {
            if (Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseMove)
            {
                pos = Event.current.mousePosition - dragOffset;
                pos.x = Mathf.Clamp(pos.x, 0f, UI.screenWidth - Width);
                pos.y = Mathf.Clamp(pos.y, 0f, UI.screenHeight - height);
                WidgetPos = pos;
                widgetRect = new Rect(pos.x, pos.y, Width, height);
            }
            if (Event.current.type == EventType.MouseUp && Event.current.button == 1)
            {
                dragging = false;
                HSKResourceTrackerMod.Settings?.Write();
                Event.current.Use();
            }
        }

        // Background
        Widgets.DrawBoxSolid(widgetRect, BgColor);
        Widgets.DrawBox(widgetRect, 1);

        // Header
        Text.Font = GameFont.Tiny;
        Text.Anchor = TextAnchor.MiddleCenter;
        GUI.color = DimText;
        string headerLabel = expanded ? "RT_WidgetTitle".Translate() + " ▼" : "RT_WidgetTitle".Translate() + " ▶";
        Rect headerRect = new Rect(widgetRect.x, widgetRect.y, widgetRect.width - 22f, HeaderH);
        Widgets.Label(headerRect, headerLabel);

        // Settings button
        Rect gearRect = new Rect(widgetRect.xMax - 22f, widgetRect.y + 2f, 20f, 18f);
        Text.Font = GameFont.Tiny;
        Text.Anchor = TextAnchor.MiddleCenter;
        GUI.color = Mouse.IsOver(gearRect) ? Color.white : new Color(1f, 1f, 1f, 0.5f);
        Widgets.DrawBoxSolid(gearRect, new Color(0.2f, 0.2f, 0.2f, 0.6f));
        Widgets.Label(gearRect, "...");
        if (Widgets.ButtonInvisible(gearRect))
            Find.WindowStack.Add(new Dialog_ResourceSettings());

        // Toggle expand on header click
        if (Widgets.ButtonInvisible(headerRect))
            expanded = !expanded;

        GUI.color = Color.white;

        // Scrollable content area
        Rect contentRect = new Rect(widgetRect.x, widgetRect.y + HeaderH, Width, height - HeaderH);
        if (needsScroll)
        {
            Rect viewRect = new Rect(0f, 0f, Width - 16f, contentH - HeaderH);
            Widgets.BeginScrollView(contentRect, ref scrollPosition, viewRect);
        }

        float lineY = needsScroll ? 0f : widgetRect.y + HeaderH;
        float lineX = needsScroll ? 0f : widgetRect.x;
        float lineW = needsScroll ? Width - 16f : Width;

        // Lines
        for (int i = 0; i < tracker.CachedCounts.Count; i++)
        {
            var cc = tracker.CachedCounts[i];
            bool belowMin = cc.count < cc.minimum;

            if (!expanded && !belowMin)
                continue;

            // Color: below min = red, 70-100% = yellow, above = green
            if (belowMin)
                GUI.color = Red;
            else
            {
                float ratio = cc.minimum > 0 ? (float)cc.count / cc.minimum : 2f;
                GUI.color = ratio < 1.5f ? Yellow : Green;
            }

            bool isCatExpanded = expandedCategories.Contains(i);
            string arrow = isCatExpanded ? "▼ " : "▶ ";

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(lineX + 4f, lineY, lineW * 0.6f, LineH), arrow + cc.name);

            Text.Anchor = TextAnchor.MiddleRight;
            string countStr = (!expanded && cc.minimum > 0) ? cc.count + " / " + cc.minimum : cc.count.ToString();
            Widgets.Label(new Rect(lineX, lineY, lineW - 4f, LineH), countStr);

            // Click to toggle expand
            Rect lineRect = new Rect(lineX, lineY, lineW, LineH);
            if (!needsScroll && Mouse.IsOver(lineRect))
                Widgets.DrawHighlight(lineRect);
            if (Widgets.ButtonInvisible(lineRect) && !dragging)
                { if (isCatExpanded) expandedCategories.Remove(i); else expandedCategories.Add(i); }

            lineY += LineH;

            // Expanded items
            if (isCatExpanded && cc.items != null)
            {
                GUI.color = new Color(0.75f, 0.75f, 0.75f);
                foreach (var item in cc.items)
                {
                    if (cc.minVisible > 0 && item.count < cc.minVisible)
                        continue;

                    // Item icon
                    var def = DefDatabase<ThingDef>.GetNamedSilentFail(item.defName);
                    if (def?.uiIcon != null && def.uiIcon != BaseContent.BadTex)
                        GUI.DrawTexture(new Rect(lineX + 6f, lineY, ItemLineH, ItemLineH), def.uiIcon, ScaleMode.ScaleToFit);

                    Text.WordWrap = false;
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(new Rect(lineX + 6f + ItemLineH + 2f, lineY, lineW * 0.5f, ItemLineH), item.label);
                    Text.Anchor = TextAnchor.MiddleRight;
                    Widgets.Label(new Rect(lineX, lineY, lineW - 4f, ItemLineH), "x" + item.count);
                    Text.WordWrap = true;
                    lineY += ItemLineH;
                }
                GUI.color = belowMin ? Red : (cc.minimum > 0 && (float)cc.count / cc.minimum < 1.5f ? Yellow : Green);
            }
        }

        if (needsScroll)
            Widgets.EndScrollView();

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;

        // Tooltip
        if (Mouse.IsOver(widgetRect))
        {
            TooltipHandler.TipRegion(widgetRect, "RT_WidgetTooltip".Translate() + "\n\n" + "RT_DragHint".Translate());
        }
    }
}
