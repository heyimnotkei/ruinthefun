using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;

namespace RuinTheFun;

internal class RuinTheFunHelpDialog : GuiDialog
{
    private const int SectionFrameHorizontalPadding = 16;
    private const int SectionFrameTopPadding        = 14;
    private const int SectionFrameBottomPadding     = 18;
    private const int SectionContentBottomPadding   = 10;
    private const int SectionSpacing                = 16;
    private const int SectionFrameDepth             = 5;
    private const float SectionFrameBrightness      = 0.78f;
    private const int ControlHeight                 = 28;
    private const int ControlVerticalSpacing        = 12;
    private const int ControlHorizontalSpacing      = 12;

    private readonly RuinTheFunSystem system;

    private CairoFont headingFont        = null!;
    private CairoFont sectionHeadingFont = null!;
    private CairoFont textFont           = null!;
    private CairoFont accentFont         = null!;

    public override string? ToggleKeyCombinationCode => null;

    public RuinTheFunHelpDialog(ICoreClientAPI capi, RuinTheFunSystem system)
        : base(capi)
    {
        this.system = system;
        BuildDialog();
    }

    private void BuildDialog()
    {
        SingleComposer?.Dispose();

        string lang = system.CurrentLanguage;

        headingFont        = CairoFont.WhiteDetailText().WithFontSize(18f);
        sectionHeadingFont = headingFont.Clone().WithColor(new double[] { 0.97, 0.9, 0.68, 1.0 });
        textFont           = CairoFont.WhiteSmallText().WithFontSize(15.5f);
        accentFont         = textFont.Clone().WithColor(new double[] { 0.9, 0.86, 0.7, 1.0 });

        var dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
        var bgBounds     = ElementStdBounds.DialogBackground().WithFixedPadding(GuiStyle.ElementToDialogPadding);

        var composer = capi.Gui
            .CreateCompo("ruinthefunhelp", dialogBounds)
            .AddRuinTheFunTitleBar(Localization.Get("help", lang), () => TryClose())
            .AddDialogBG(bgBounds)
            .BeginChildElements();

        int margin = 24;
        int width  = 520;
        int y      = (int)GuiStyle.TitleBarHeight + 16;

        // --- Intro section ---
        int sectionStartY = y;
        var frame1 = CreateSectionFrame(composer, margin, width, sectionStartY);

        composer.AddStaticText(Localization.Get("helpIntroHeading", lang), sectionHeadingFont,
            ElementBounds.Fixed(margin, y, width, 26), "help-intro-heading");
        y += 32;

        int introHeight = 94;
        composer.AddRichtext(Localization.Get("helpIntro", lang), textFont,
            ElementBounds.Fixed(margin, y, width, introHeight), "help-intro");
        y += introHeight + 12;

        FinalizeSection(frame1, sectionStartY, ref y, addSpacingAfter: true);

        // --- Hotkeys section ---
        sectionStartY = y;
        var frame2 = CreateSectionFrame(composer, margin, width, sectionStartY);

        composer.AddStaticText(Localization.Get("helpHotkeysHeading", lang), sectionHeadingFont,
            ElementBounds.Fixed(margin, y, width, 26), "help-heading");
        y += 32;

        var hotkeyTable = BuildHotkeyTable();

        if (hotkeyTable.Count == 0)
        {
            composer.AddRichtext(Localization.Get("helpNoHotkeys", lang), textFont,
                ElementBounds.Fixed(margin, y, width, 40), "help-empty");
            y += 40;
        }
        else
        {
            int colKey  = 200;
            int colDesc = width - colKey - ControlHorizontalSpacing;

            composer.AddStaticText(Localization.Get("helpColumnHotkey", lang), accentFont,
                ElementBounds.Fixed(margin, y, colKey, ControlHeight), "col-hotkey");
            composer.AddStaticText(Localization.Get("helpColumnDescription", lang), accentFont,
                ElementBounds.Fixed(margin + colKey + ControlHorizontalSpacing, y, colDesc, ControlHeight), "col-desc");
            y += 34;

            foreach (var (categoryKey, hotkeys) in hotkeyTable)
            {
                composer.AddStaticText(Localization.Get(categoryKey, lang), sectionHeadingFont,
                    ElementBounds.Fixed(margin, y, width, ControlHeight), "cat-" + categoryKey);
                y += ControlHeight;

                foreach (var hk in hotkeys)
                {
                    string key  = system.FormatHotkey(hk.Code, hk.DefaultKey);
                    string desc = Localization.Get(hk.TranslationKey, lang);
                    if (string.IsNullOrWhiteSpace(key)) key = "-";

                    composer.AddStaticText(key, textFont,
                        ElementBounds.Fixed(margin, y, colKey, ControlHeight),
                        "row-" + hk.Code + "-key");
                    composer.AddStaticText(desc, textFont,
                        ElementBounds.Fixed(margin + colKey + ControlHorizontalSpacing, y, colDesc, ControlHeight),
                        "row-" + hk.Code + "-label");
                    y += ControlHeight;
                }

                y += 12;
            }
        }

        FinalizeSection(frame2, sectionStartY, ref y, addSpacingAfter: false);

        SingleComposer = composer.EndChildElements().Compose();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private ElementBounds CreateSectionFrame(GuiComposer composer, int margin, int width, int headingStartY)
    {
        var bounds = ElementBounds.Fixed(
            margin - SectionFrameHorizontalPadding,
            headingStartY - SectionFrameTopPadding,
            width + SectionFrameHorizontalPadding * 2,
            0);
        composer.AddInset(bounds, SectionFrameDepth, SectionFrameBrightness);
        return bounds;
    }

    private void FinalizeSection(ElementBounds frameBounds, int headingStartY, ref int y, bool addSpacingAfter)
    {
        y += SectionContentBottomPadding;
        frameBounds.fixedHeight = y - headingStartY + SectionFrameTopPadding + SectionFrameBottomPadding;
        double nextY = frameBounds.fixedY + frameBounds.fixedHeight;
        if (addSpacingAfter) nextY += SectionSpacing;
        y = (int)Math.Ceiling(nextY);
    }

    private Dictionary<string, List<RuinTheFunSystem.RegisteredHotkey>> BuildHotkeyTable()
    {
        var table = new Dictionary<string, List<RuinTheFunSystem.RegisteredHotkey>>(StringComparer.OrdinalIgnoreCase);

        foreach (string category in system.GetHelpCategories())
        {
            var hotkeys = system.GetHotkeysForCategory(category).ToList();
            if (hotkeys.Count > 0)
                table[category] = hotkeys;
        }

        return table;
    }

    // ── Public refresh API ───────────────────────────────────────────────────

    public void RefreshLanguage()
    {
        bool wasOpen = IsOpened();
        BuildDialog();
        if (wasOpen && !IsOpened()) TryOpen();
    }

    public void RefreshContent()
    {
        bool wasOpen = IsOpened();
        BuildDialog();
        if (wasOpen && !IsOpened()) TryOpen();
    }
}
