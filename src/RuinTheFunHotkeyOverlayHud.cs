using System.Text;
using Vintagestory.API.Client;

namespace RuinTheFun;

internal class RuinTheFunHotkeyOverlayHud : HudElement
{
    private readonly RuinTheFunSystem system;
    private GuiElementRichtext? textElement;

    public override bool Focusable => false;
    public override bool PrefersUngrabbedMouse => false;
    public override double DrawOrder => 0.05;

    public override bool ShouldReceiveMouseEvents() => false;
    public override bool ShouldReceiveKeyboardEvents() => false;

    public RuinTheFunHotkeyOverlayHud(ICoreClientAPI capi, RuinTheFunSystem system)
        : base(capi)
    {
        this.system = system;
        Compose();
    }

    private void Compose()
    {
        var panelBounds   = ElementBounds.Fixed(20, 80, 360, 240);
        var insetBounds   = ElementBounds.Fixed(0, 0, 360, 240);
        var contentBounds = ElementBounds.Fixed(12, 12, 336, 216);

        var composer = capi.Gui
            .CreateCompo("ruinthefunhotkeyoverlay", panelBounds)
            .AddInset(insetBounds, 6, 0.35f)
            .BeginChildElements()
            .AddRichtext(BuildOverlayText(), CairoFont.WhiteSmallText(), contentBounds, "overlaytext")
            .EndChildElements();

        SingleComposer = composer.Compose();
        textElement = SingleComposer.GetRichtext("overlaytext");
    }

    public void RefreshContent()
    {
        if (textElement is null)
        {
            Compose();
            return;
        }
        textElement.SetNewText(BuildOverlayText(), CairoFont.WhiteSmallText());
    }

    public override void OnGuiOpened()
    {
        RefreshContent();
        base.OnGuiOpened();
    }

    private string BuildOverlayText()
    {
        var hotkeys = system.GetOverlayHotkeys();
        string lang  = system.CurrentLanguage;

        using var enumerator = hotkeys.GetEnumerator();
        if (!enumerator.MoveNext())
            return Localization.Get("overlayEmpty", lang);

        var sb = new StringBuilder();
        do
        {
            var hk = enumerator.Current;
            string key  = system.FormatHotkey(hk.Code, hk.DefaultKey);
            string desc = Localization.Get(hk.TranslationKey, lang);
            if (string.IsNullOrWhiteSpace(key)) key = "-";
            sb.Append(key).Append(" - ").AppendLine(desc);
        }
        while (enumerator.MoveNext());

        return sb.ToString().TrimEnd();
    }
}
