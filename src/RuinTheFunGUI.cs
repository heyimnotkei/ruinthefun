using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace RuinTheFun;

internal class RuinTheFunGUI : GuiDialog
{
    private const int SHPad = 16; private const int STPad = 14; private const int SBPad = 18;
    private const int SCBPad = 10; private const int SSpc = 16; private const int SDepth = 5;
    private const float SBright = 0.78f;
    private const int H = 28; private const int Gap = 12; private const int LW = 170;
    private const int SlMin = 100; private const int SlMax = 300; private const int SlStep = 5;

    private readonly RuinTheFunSystem system;
    private int activeTab = 0; // 0 = Features, 1 = Hotkeys

    // Feature tab element refs
    private GuiElementSlider?       movSlider;
    private GuiElementRichtext?     movVal;
    private GuiElementToggleButton? oneHitTgl;
    private GuiElementToggleButton? noFallTgl;
    private GuiElementToggleButton? flightTgl;
    private GuiElementToggleButton? fastPlaceTgl;

    private string[] langCodes = new string[0];
    private bool syncing;

    private CairoFont sectionFont; private CairoFont textFont; private CairoFont accentFont;

    public override string ToggleKeyCombinationCode => "ruinthefunsettings";

    public RuinTheFunGUI(ICoreClientAPI capi, RuinTheFunSystem system) : base(capi)
    { this.system = system; Compose(); }

    private void Compose()
    {
        SingleComposer?.Dispose();
        string lang = system.CurrentLanguage;

        sectionFont = CairoFont.WhiteDetailText().WithFontSize(18f).Clone().WithColor(new double[] { 0.97, 0.9, 0.68, 1.0 });
        textFont    = CairoFont.WhiteSmallText().WithFontSize(15.5f);
        accentFont  = textFont.Clone().WithColor(new double[] { 0.9, 0.86, 0.7, 1.0 });

        var dlg = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
        var bg  = ElementStdBounds.DialogBackground().WithFixedPadding(GuiStyle.ElementToDialogPadding);

        langCodes = BuildLangList();
        string[] dispNames = langCodes.Select(Localization.GetLanguageDisplayName).ToArray();
        int langIdx = 0;
        for (int i = 0; i < langCodes.Length; i++) if (langCodes[i] == system.CurrentLanguage) { langIdx = i; break; }

        int margin = 24; int width = 580;

        // ── Tab definitions ───────────────────────────────────────────────────
        var tabs = new GuiTab[]
        {
            new GuiTab { Name = Localization.Get("tabFeatures", lang), DataInt = 0 },
            new GuiTab { Name = Localization.Get("tabHotkeys",  lang), DataInt = 1 }
        };

        int titleH  = (int)GuiStyle.TitleBarHeight;
        int tabH     = 28;
        var tabBounds = ElementBounds.Fixed(margin, titleH + 10, width, tabH);
        int contentY  = titleH + tabH + 18;

        var composer = capi.Gui
            .CreateCompo("ruinthefunsettings", dlg)
            .AddRuinTheFunTitleBar(Localization.Get("settingsTitle", lang), () => TryClose())
            .AddDialogBG(bg)
            .BeginChildElements()
            .AddHorizontalTabs(tabs, tabBounds, OnTabSwitch, textFont, accentFont, "tabs");

        if (activeTab == 0)
            ComposeFeatures(composer, contentY, margin, width, lang, langIdx, langCodes, dispNames);
        else
            ComposeHotkeys(composer, contentY, margin, width, lang);

        SingleComposer = composer.EndChildElements().Compose();

        // Reflect active tab in the tab bar
        var tabEl = SingleComposer.GetHorizontalTabs("tabs");
        if (tabEl != null) tabEl.activeElement = activeTab;

        if (activeTab == 0) SyncFeatures();
        else                PopulateHotkeyInputs();
    }

    // ── Features tab ──────────────────────────────────────────────────────────

    private void ComposeFeatures(GuiComposer c, int y, int margin, int width, string lang,
        int langIdx, string[] codes, string[] names)
    {
        int tX = margin + LW + Gap;

        // General
        int secY = y; var f1 = MkFrame(c, margin, width, secY);
        c.AddStaticText(Localization.Get("settingsSectionGeneral", lang), sectionFont, EB(margin, y, width, 26), "sg");
        y += 30;
        c.AddStaticText(Localization.Get("settingsLanguage", lang), accentFont, EB(margin, y + 2, LW, H), "ll");
        c.AddDropDown(codes, names, langIdx, OnLangChanged, EB(tX, y, 200, H), "ldd");
        c.AddSmallButton(Localization.Get("settingsOpenHelp", lang), OnHelpBtn, EB(tX + 200 + Gap, y, 120, H), EnumButtonStyle.Normal, "hb");
        y += 42; FinSec(f1, secY, ref y, true);

        // Homes
        secY = y; var f2 = MkFrame(c, margin, width, secY);
        c.AddStaticText(Localization.Get("mainSectionGeneral", lang), sectionFont, EB(margin, y, width, 26), "sh");
        y += 30;
        int inW = 260; int setBW = 110;
        c.AddStaticText(Localization.Get("mainHomeNewLabel", lang), accentFont, EB(margin, y + 4, LW, H), "hnl");
        c.AddTextInput(EB(tX, y, inW, H), null, textFont, "home-name");
        c.AddSmallButton(Localization.Get("mainHomeSetButton", lang),
            () => { var inp = SingleComposer.GetTextInput("home-name"); string n = inp?.GetText()?.Trim() ?? ""; if (string.IsNullOrWhiteSpace(n)) n = "Home " + (system.GetAllHomes().Count + 1); system.TryAddOrUpdateHome(n, out _); Compose(); return true; },
            EB(tX + inW + Gap, y, setBW, H), EnumButtonStyle.Normal, "hset");
        y += 40;
        c.AddStaticText(Localization.Get("mainHomeSavedLabel", lang), accentFont, EB(margin, y, width, 22), "hsl");
        y += 26;
        var homes = system.GetAllHomes();
        if (homes.Count == 0) { c.AddStaticText(Localization.Get("mainHomeNone", lang), textFont, EB(margin, y, width, H), "hne"); y += H + 8; }
        else
        {
            int tpW = 88; int delW = 68; int cW = width - LW - Gap - tpW - Gap - delW - Gap;
            int ri = 0;
            foreach (var kv in homes)
            {
                string hn = kv.Key; var en = kv.Value;
                string co = string.Format(CultureInfo.InvariantCulture, "{0:0}, {1:0}, {2:0}", en.X, en.Y, en.Z);
                string rk = "hr" + ri; string cap = hn;
                c.AddStaticText(hn, accentFont, EB(margin, y + 3, LW, H), rk + "n");
                c.AddStaticText(co, textFont,   EB(margin + LW + Gap, y + 3, cW, H), rk + "c");
                c.AddSmallButton(Localization.Get("mainHomeTeleportButton", lang),
                    () => { system.TryTeleportToHome(cap); return true; },
                    EB(margin + LW + Gap + cW + Gap, y, tpW, H), EnumButtonStyle.Normal, rk + "tp");
                c.AddSmallButton(Localization.Get("mainHomeDeleteButton", lang),
                    () => { system.TryDeleteHome(cap); Compose(); return true; },
                    EB(margin + LW + Gap + cW + Gap + tpW + Gap, y, delW, H), EnumButtonStyle.Normal, rk + "del");
                y += H + 8; ri++;
            }
        }
        y += 4;
        int slW = 210;
        c.AddStaticText(Localization.Get("mainMovementHeading", lang), accentFont, EB(margin, y + 2, LW, H), "ml");
        c.AddSlider(OnMovSlider, EB(tX, y, slW, H), "mslider");
        c.AddRichtext(BuildMovTxt(system.MovementSpeedMultiplier, lang), accentFont, EB(tX + slW + Gap, y + 2, 90, H), "mval");
        y += 40; FinSec(f2, secY, ref y, true);

        // Utility
        secY = y; var f3 = MkFrame(c, margin, width, secY);
        c.AddStaticText(Localization.Get("mainSectionUtility", lang), sectionFont, EB(margin, y, width, 26), "su");
        y += 30;
        int tW = 180;
        c.AddStaticText(Localization.Get("mainOneHitHeading", lang), accentFont, EB(margin, y + 2, LW, H), "ohl");
        c.AddToggleButton(Localization.Get("mainOneHitToggle", lang), textFont, b => { if (!syncing) system.SetOneHitBlockEnabled(b); }, EB(tX, y, tW, H), "ohtgl");
        y += 40;
        c.AddStaticText(Localization.Get("mainNoFallHeading", lang), accentFont, EB(margin, y + 2, LW, H), "nfl");
        c.AddToggleButton(Localization.Get("mainNoFallToggle", lang), textFont, b => { if (!syncing) system.SetNoFallEnabled(b); }, EB(tX, y, tW, H), "nftgl");
        y += 40;
        c.AddStaticText(Localization.Get("mainFlightHeading", lang), accentFont, EB(margin, y + 2, LW, H), "fll");
        c.AddToggleButton(Localization.Get("mainFlightToggle", lang), textFont, b => { if (!syncing) system.SetFlightEnabled(b); }, EB(tX, y, tW, H), "fltgl");
        c.AddStaticText(Localization.Get("mainFlightHint", lang), textFont, EB(tX + tW + Gap, y + 4, width - tX - tW - Gap, H), "flhint");
        y += 40;
        c.AddStaticText(Localization.Get("mainFastPlaceHeading", lang), accentFont, EB(margin, y + 2, LW, H), "fpl");
        c.AddToggleButton(Localization.Get("mainFastPlaceToggle", lang), textFont, b => { if (!syncing) system.SetFastPlaceEnabled(b); }, EB(tX, y, tW, H), "fptgl");
        c.AddStaticText(Localization.Get("mainFastPlaceHint", lang), textFont, EB(tX + tW + Gap, y + 4, width - tX - tW - Gap, H), "fphint");
        y += 40;
        FinSec(f3, secY, ref y, false);

        movSlider  = null; movVal = null; oneHitTgl = null; noFallTgl = null; flightTgl = null; fastPlaceTgl = null;
    }

    // ── Hotkeys tab ───────────────────────────────────────────────────────────

    private void ComposeHotkeys(GuiComposer c, int y, int margin, int width, string lang)
    {
        int secY = y; var f1 = MkFrame(c, margin, width, secY);
        c.AddStaticText(Localization.Get("hotkeyTabTitle", lang), sectionFont, EB(margin, y, width, 26), "htt");
        y += 30;
        c.AddStaticText(Localization.Get("hotkeyTabHint", lang), textFont, EB(margin, y, width, 22), "hth");
        y += 28;

        int nameW  = 220;
        int inputW = 120;
        int setW   = 60;
        int defW   = 76;

        // Column headers
        c.AddStaticText(Localization.Get("hotkeyColAction", lang), accentFont, EB(margin, y, nameW, H), "hch-a");
        c.AddStaticText(Localization.Get("hotkeyColKey",    lang), accentFont, EB(margin + nameW + Gap, y, inputW, H), "hch-k");
        y += H + 4;

        int ri = 0;
        foreach (var hk in system.GetRegisteredHotkeys())
        {
            string code = hk.Code; string rk = "hkr" + ri;
            string actionName = Localization.Get(hk.TranslationKey, lang);

            c.AddStaticText(actionName, textFont, EB(margin, y + 3, nameW, H), rk + "n");
            c.AddTextInput(EB(margin + nameW + Gap, y, inputW, H), null, textFont, rk + "inp");

            c.AddSmallButton(Localization.Get("hotkeySetBtn", lang),
                () =>
                {
                    var inp = SingleComposer?.GetTextInput(rk + "inp");
                    string keyStr = inp?.GetText()?.Trim() ?? string.Empty;
                    if (!system.TryRebindHotkey(code, keyStr))
                        NotifyBadKey(keyStr);
                    return true;
                },
                EB(margin + nameW + Gap + inputW + Gap, y, setW, H), EnumButtonStyle.Normal, rk + "set");

            c.AddSmallButton(Localization.Get("hotkeyDefaultBtn", lang),
                () => { system.ResetHotkeyToDefault(code); Compose(); return true; },
                EB(margin + nameW + Gap + inputW + Gap + setW + Gap, y, defW, H), EnumButtonStyle.Normal, rk + "def");

            y += H + 8; ri++;
        }

        FinSec(f1, secY, ref y, false);
    }

    private void PopulateHotkeyInputs()
    {
        int ri = 0;
        foreach (var hk in system.GetRegisteredHotkeys())
        {
            string rk  = "hkr" + ri;
            var    inp = SingleComposer?.GetTextInput(rk + "inp");
            if (inp != null) inp.SetValue(system.GetCurrentKeyName(hk.Code, hk.DefaultKey));
            ri++;
        }
    }

    private void NotifyBadKey(string keyStr)
    {
        // Show a brief reminder — valid names are GlKeys enum members
        capi.ShowChatMessage("Ruin the Fun: '" + keyStr + "' is not a valid key name. Try: A, B, F1, R, Space ...");
    }

    // ── Tab switch ────────────────────────────────────────────────────────────

    private void OnTabSwitch(int tabDataInt) { activeTab = tabDataInt; Compose(); }

    // ── Layout helpers ────────────────────────────────────────────────────────

    private ElementBounds EB(int x, int y, int w, int h) => ElementBounds.Fixed(x, y, w, h);

    private ElementBounds MkFrame(GuiComposer c, int m, int w, int headY)
    {
        var b = ElementBounds.Fixed(m - SHPad, headY - STPad, w + SHPad * 2, 0);
        c.AddInset(b, SDepth, SBright);
        return b;
    }

    private void FinSec(ElementBounds f, int headY, ref int y, bool spacing)
    {
        y += SCBPad;
        f.fixedHeight = y - headY + STPad + SBPad;
        double next = f.fixedY + f.fixedHeight;
        if (spacing) next += SSpc;
        y = (int)Math.Ceiling(next);
    }

    // ── Events (Features tab) ─────────────────────────────────────────────────

    private bool OnHelpBtn() { system.OpenHelpDialog(); return true; }

    private void OnLangChanged(string code, bool sel)
    { if (sel) { system.SetLanguage(code); Compose(); } }

    private bool OnMovSlider(int value)
    {
        if (syncing) return true;
        float m = (float)GameMath.Clamp(value, SlMin, SlMax) / 100f;
        system.SetMovementSpeedMultiplier(m);
        movVal?.SetNewText(BuildMovTxt(m, system.CurrentLanguage), accentFont);
        return true;
    }

    // ── State sync (Features tab) ─────────────────────────────────────────────

    private void SyncFeatures()
    {
        // Re-grab refs after compose
        movSlider   = SingleComposer?.GetSlider("mslider");
        movVal      = SingleComposer?.GetRichtext("mval");
        oneHitTgl   = SingleComposer?.GetToggleButton("ohtgl");
        noFallTgl   = SingleComposer?.GetToggleButton("nftgl");
        flightTgl   = SingleComposer?.GetToggleButton("fltgl");
        fastPlaceTgl = SingleComposer?.GetToggleButton("fptgl");

        syncing = true;
        if (movSlider != null)
        {
            int sv = (int)Math.Round(GameMath.Clamp(system.MovementSpeedMultiplier, 1f, 3f) * 100f);
            movSlider.SetValues(sv, SlMin, SlMax, SlStep, "");
            movSlider.SetValue(sv);
        }
        movVal?.SetNewText(BuildMovTxt(system.MovementSpeedMultiplier, system.CurrentLanguage), accentFont);
        oneHitTgl?.SetValue(system.OneHitBlockEnabled);
        noFallTgl?.SetValue(system.NoFallEnabled);
        flightTgl?.SetValue(system.FlightEnabled);
        fastPlaceTgl?.SetValue(system.FastPlaceEnabled);
        syncing = false;
    }

    // ── Text builders ─────────────────────────────────────────────────────────

    private string BuildMovTxt(float m, string lang)
        => string.Format(Localization.Get("mainMovementValue", lang), m.ToString("0.00", CultureInfo.InvariantCulture));

    private string[] BuildLangList()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<string>();
        foreach (string c in Localization.GetSupportedLanguageCodes())
        { string n = Localization.NormalizeLanguage(c); if (seen.Add(n)) list.Add(n); }
        list.Sort((a, b) => string.Compare(Localization.GetLanguageDisplayName(a), Localization.GetLanguageDisplayName(b), StringComparison.OrdinalIgnoreCase));
        return list.ToArray();
    }

    // ── Public refresh ────────────────────────────────────────────────────────

    public void RefreshLanguage()      => Compose();
    public void RefreshHotkeys()       { if (activeTab == 1) PopulateHotkeyInputs(); }
    public void RefreshFeatureStates() { if (activeTab == 0) SyncFeatures(); else Compose(); }
}
