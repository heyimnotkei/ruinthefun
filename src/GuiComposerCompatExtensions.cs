using System;
using System.Reflection;
using Vintagestory.API.Client;

namespace RuinTheFun;

/// <summary>
/// Adds a title bar to a <see cref="GuiComposer"/> in a forward-compatible way.
/// Uses reflection so the call still compiles if the VS API renames the helper method.
/// </summary>
internal static class GuiComposerCompatExtensions
{
    public static GuiComposer AddRuinTheFunTitleBar(
        this GuiComposer composer,
        string text,
        Action? onClose = null,
        CairoFont? font = null,
        ElementBounds? bounds = null)
    {
        if (composer is null)
            throw new ArgumentNullException(nameof(composer));

        // Try to call GuiComposerHelpers.AddDialogTitleBar via reflection so the
        // code stays compatible if Vintagestory renames or reorganises the helper.
        var helpersType = typeof(GuiComposer).Assembly
            .GetType("Vintagestory.API.Client.GuiComposerHelpers");

        if (helpersType is not null)
        {
            foreach (var method in helpersType.GetMethods(BindingFlags.Static | BindingFlags.Public))
            {
                if (method.Name != "AddDialogTitleBar")
                    continue;

                var parameters = method.GetParameters();
                if (parameters.Length == 0 || !parameters[0].ParameterType.IsAssignableFrom(typeof(GuiComposer)))
                    continue;

                var args = new object?[parameters.Length];
                bool gotText = false, gotClose = false, gotFont = false, gotBounds = false;

                for (int i = 0; i < parameters.Length; i++)
                {
                    var p = parameters[i];
                    if (i == 0)
                    {
                        args[i] = composer;
                    }
                    else if (!gotText && p.ParameterType == typeof(string) &&
                             (string.Equals(p.Name, "text", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(p.Name, "title", StringComparison.OrdinalIgnoreCase) ||
                              !p.HasDefaultValue))
                    {
                        args[i] = text; gotText = true;
                    }
                    else if (!gotClose && p.ParameterType == typeof(Action) &&
                             (string.Equals(p.Name, "onClose", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(p.Name, "closeHandler", StringComparison.OrdinalIgnoreCase) ||
                              !p.HasDefaultValue))
                    {
                        args[i] = onClose; gotClose = true;
                    }
                    else if (!gotFont && p.ParameterType == typeof(CairoFont))
                    {
                        args[i] = font; gotFont = true;
                    }
                    else if (!gotBounds && p.ParameterType == typeof(ElementBounds))
                    {
                        args[i] = bounds; gotBounds = true;
                    }
                    else
                    {
                        args[i] = p.HasDefaultValue
                            ? p.DefaultValue
                            : (p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null);
                    }
                }

                try
                {
                    method.Invoke(null, args);
                    return composer;
                }
                catch { /* fall through to direct construction */ }
            }
        }

        // Fallback: construct the title bar element directly.
        var api = composer.Api
            ?? throw new InvalidOperationException("GuiComposer API is not a client API instance.");

        font ??= CairoFont.WhiteSmallText();
        bounds ??= ElementStdBounds.TitleBar();

        if (!composer.Composed)
            composer.AddInteractiveElement(new GuiElementDialogTitleBar(api, text, composer, onClose, font, bounds));

        return composer;
    }
}
