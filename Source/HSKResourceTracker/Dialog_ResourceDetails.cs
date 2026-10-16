using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace HSKResourceTracker;

public class Dialog_ResourceDetails : Window
{
    private readonly CategoryCount data;
    private Vector2 scrollPosition;

    private static readonly Color GreenText = new Color(0.4f, 0.95f, 0.4f);
    private static readonly Color YellowText = new Color(0.95f, 0.95f, 0.3f);
    private static readonly Color RedText = new Color(0.95f, 0.4f, 0.4f);
    private static readonly Color DimText = new Color(1f, 1f, 1f, 0.5f);
    private static readonly Color RowBg = new Color(0.15f, 0.15f, 0.15f, 0.4f);

    public override Vector2 InitialSize => new Vector2(400f, 450f);

    public Dialog_ResourceDetails(CategoryCount data)
    {
        this.data = data;
        doCloseButton = true;
        doCloseX = true;
        draggable = true;
        absorbInputAroundWindow = false;
    }

    public override void DoWindowContents(Rect inRect)
    {
        // Title
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), data.name);
        Text.Font = GameFont.Small;

        float y = 38f;

        // Summary bar
        bool belowMin = data.count < data.minimum;
        float ratio = data.minimum > 0 ? (float)data.count / data.minimum : 1f;

        Rect summaryRect = new Rect(0f, y, inRect.width, 40f);
        Widgets.DrawBoxSolid(summaryRect, new Color(0.15f, 0.15f, 0.15f, 0.8f));

        Text.Anchor = TextAnchor.MiddleCenter;
        GUI.color = belowMin ? (ratio < 0.3f ? RedText : YellowText) : GreenText;
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(0f, y + 2f, inRect.width, 36f),
            data.count + " / " + data.minimum);
        Text.Font = GameFont.Small;
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
        y += 46f;

        // Progress bar
        float barX = 20f;
        float barW = inRect.width - 40f;
        float barH = 14f;
        Widgets.DrawBoxSolid(new Rect(barX, y, barW, barH), new Color(0.1f, 0.1f, 0.1f, 0.8f));
        float fillW = Mathf.Clamp01(ratio) * barW;
        Color barColor = ratio < 0.3f ? RedText : ratio < 0.7f ? YellowText : GreenText;
        if (fillW > 0f)
            Widgets.DrawBoxSolid(new Rect(barX, y, fillW, barH), new Color(barColor.r, barColor.g, barColor.b, 0.6f));
        Widgets.DrawBox(new Rect(barX, y, barW, barH), 1);
        y += barH + 10f;

        // Items list
        var sorted = data.items.OrderByDescending(i => i.count).ToList();
        float listH = sorted.Count * 28f;
        Rect outRect = new Rect(0f, y, inRect.width, inRect.height - y - 50f);
        Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, listH);

        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
        float rowY = 0f;

        for (int i = 0; i < sorted.Count; i++)
        {
            var item = sorted[i];
            Rect rowRect = new Rect(0f, rowY, viewRect.width, 26f);

            if (i % 2 == 0)
                Widgets.DrawBoxSolid(rowRect, RowBg);

            // Icon
            var def = DefDatabase<ThingDef>.GetNamedSilentFail(item.defName);
            if (def?.uiIcon != null && def.uiIcon != BaseContent.BadTex)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(4f, rowY + 1f, 22f, 22f), def.uiIcon, ScaleMode.ScaleToFit);
            }

            // Name
            GUI.color = item.count > 0 ? Color.white : DimText;
            Widgets.Label(new Rect(30f, rowY, viewRect.width * 0.55f, 26f), item.label);

            // Count
            Text.Anchor = TextAnchor.MiddleRight;
            GUI.color = item.count > 0 ? GreenText : DimText;
            Widgets.Label(new Rect(viewRect.width - 60f, rowY, 55f, 26f), "x" + item.count);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            rowY += 28f;
        }

        Widgets.EndScrollView();
    }
}
