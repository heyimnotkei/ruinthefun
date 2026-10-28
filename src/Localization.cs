using System;
using System.Collections.Generic;
using System.Linq;

namespace RuinTheFun;

internal static class Localization
{
    public const string DefaultLanguage = "en";

    private static readonly Dictionary<string, Dictionary<string, string>> Translations =
        new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["help"]                          = "Help",
            ["settingsTitle"]                 = "Ruin the Fun",
            ["settingsSectionGeneral"]        = "General",
            ["settingsLanguage"]              = "Language",
            ["settingsOpenHelp"]              = "Open help",
            ["mainSectionGeneral"]            = "General features",
            ["mainHomeNewLabel"]              = "Name new home",
            ["mainHomeSetButton"]             = "Set here",
            ["mainHomeSavedLabel"]            = "Saved homes",
            ["mainHomeNone"]                  = "No homes saved yet. Enter a name above and click Set here.",
            ["mainHomeTeleportButton"]        = "Teleport",
            ["mainHomeDeleteButton"]          = "Delete",
            ["mainFlightHeading"]             = "Flight mode",
            ["mainFlightToggle"]              = "Enable flight",
            ["mainFlightHint"]                = "Jump/Crouch to ascend/descend",
            ["hotkeyFlight"]                  = "Toggle flight mode",
            ["hotkeyFastPlace"]               = "Toggle fast place",
            ["mainFastPlaceHeading"]          = "Fast place",
            ["mainFastPlaceToggle"]           = "Enable fast place",
            ["mainFastPlaceHint"]             = "No delay between block placements",
            ["tabFeatures"]                   = "Features",
            ["tabHotkeys"]                    = "Hotkeys",
            ["hotkeyTabTitle"]                = "Rebind Ruin the Fun hotkeys",
            ["hotkeyTabHint"]                 = "Type a key name and click Set. Use GlKeys names: A–Z, F1–F12, Space, Return, D0–D9 ...",
            ["hotkeyColAction"]               = "Action",
            ["hotkeyColKey"]                  = "Key",
            ["hotkeySetBtn"]                  = "Set",
            ["hotkeyDefaultBtn"]              = "Default",
            ["mainMovementHeading"]           = "Movement speed modifier",
            ["mainMovementValue"]             = "Speed: {0}×",
            ["mainMovementHint"]              = "Adjusts how fast your character moves.",
            ["mainSectionUtility"]            = "Utility",
            ["mainOneHitHeading"]             = "Block one-hit",
            ["mainOneHitToggle"]              = "Enable block one-hit",
            ["mainOneHitHint"]                = "Breaks targeted blocks instantly while active.",
            ["mainNoFallHeading"]             = "Fall damage immunity",
            ["mainNoFallToggle"]              = "Disable fall damage",
            ["settingsSectionHotkeys"]        = "Registered hotkeys",
            ["settingsHotkeysHint"]           = "Rebind keys from the Vintage Story controls menu.",
            ["settingsNoHotkeys"]             = "No hotkeys registered yet.",
            ["settingsSectionExample"]        = "Sample components",
            ["settingsExampleGreetingLabel"]  = "Enable welcome greeting",
            ["settingsExampleGreetingHint"]   = "Shows how localization, GUI inputs, and config storage work together.",
            ["settingsExampleGreetingTextLabel"] = "Greeting text",
            ["settingsExampleGreetingTextHint"]  = "This value is saved to the mod config file.",
            ["settingsExampleGreetingPreview"]   = "Preview greeting",
            ["hotkeySettings"]                = "Open settings menu",
            ["hotkeyOverlay"]                 = "Toggle hotkey overlay",
            ["hotkeyOneHitBlock"]             = "Toggle block one-hit",
            ["hotkeyHomeTeleport"]            = "Teleport to home position",
            ["overlayEmpty"]                  = "No hotkeys are registered yet.",
            ["helpIntroHeading"]              = "Overview",
            ["helpIntro"]                     = "Ruin the Fun keeps custom features in one place. Use the Vintage Story controls menu to rebind hotkeys whenever you add new functionality.",
            ["helpHotkeysHeading"]            = "Registered hotkeys",
            ["helpColumnHotkey"]              = "Hotkey",
            ["helpColumnDescription"]         = "Action",
            ["helpCategoryGeneral"]           = "General",
            ["helpNoHotkeys"]                 = "No hotkeys are currently registered.",
            ["exampleGreetingChat"]           = "Ruin the Fun: {0}",
            ["exampleGreetingDefault"]        = "Ruin the Fun ready!"
        },
        ["de"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["help"]                          = "Hilfe",
            ["settingsTitle"]                 = "Ruin the Fun",
            ["settingsSectionGeneral"]        = "Allgemein",
            ["settingsLanguage"]              = "Sprache",
            ["settingsOpenHelp"]              = "Hilfe öffnen",
            ["mainSectionGeneral"]            = "Allgemeine Funktionen",
            ["mainHomeNewLabel"]              = "Neuen Home benennen",
            ["mainHomeSetButton"]             = "Hier setzen",
            ["mainHomeSavedLabel"]            = "Gespeicherte Homes",
            ["mainHomeNone"]                  = "Noch keine Homes gespeichert. Gib einen Namen ein und klicke Hier setzen.",
            ["mainHomeTeleportButton"]        = "Teleport",
            ["mainHomeDeleteButton"]          = "Löschen",
            ["mainFlightHeading"]             = "Flugmodus",
            ["mainFlightToggle"]              = "Fliegen aktivieren",
            ["mainFlightHint"]                = "Springen/Ducken zum Steigen/Sinken",
            ["hotkeyFlight"]                  = "Flugmodus umschalten",
            ["hotkeyFastPlace"]               = "Schnellplatzierung umschalten",
            ["mainFastPlaceHeading"]          = "Schnellplatzierung",
            ["mainFastPlaceToggle"]           = "Aktivieren",
            ["mainFastPlaceHint"]             = "Kein Delay zwischen Blockplatzierungen",
            ["tabFeatures"]                   = "Funktionen",
            ["tabHotkeys"]                    = "Tasten",
            ["hotkeyTabTitle"]                = "Ruin the Fun-Tasten belegen",
            ["hotkeyTabHint"]                 = "Tastenname eingeben und Setzen klicken. GlKeys-Namen: A–Z, F1–F12, Space, Return ...",
            ["hotkeyColAction"]               = "Aktion",
            ["hotkeyColKey"]                  = "Taste",
            ["hotkeySetBtn"]                  = "Setzen",
            ["hotkeyDefaultBtn"]              = "Standard",
            ["mainMovementHeading"]           = "Bewegungsgeschwindigkeit",
            ["mainMovementValue"]             = "Geschwindigkeit: {0}×",
            ["mainMovementHint"]              = "Passt an, wie schnell sich dein Spieler bewegt.",
            ["mainSectionUtility"]            = "Hilfsfunktionen",
            ["mainOneHitHeading"]             = "Block-One-Hit",
            ["mainOneHitToggle"]              = "One-Hit aktivieren",
            ["mainOneHitHint"]                = "Zerstört anvisierte Blöcke sofort, solange aktiv.",
            ["mainNoFallHeading"]             = "Kein Fallschaden",
            ["mainNoFallToggle"]              = "Fallschaden ausschalten",
            ["settingsSectionHotkeys"]        = "Registrierte Tastenkürzel",
            ["settingsHotkeysHint"]           = "Tasten im Vintage Story-Steuerungsmenü anpassen.",
            ["settingsNoHotkeys"]             = "Noch keine Tastenkürzel registriert.",
            ["settingsSectionExample"]        = "Beispielkomponenten",
            ["settingsExampleGreetingLabel"]  = "Begrüßung aktivieren",
            ["settingsExampleGreetingHint"]   = "Zeigt, wie Übersetzungen, GUI-Eingaben und Konfigurationsspeicher zusammenspielen.",
            ["settingsExampleGreetingTextLabel"] = "Begrüßungstext",
            ["settingsExampleGreetingTextHint"]  = "Dieser Wert wird in der Mod-Konfiguration gespeichert.",
            ["settingsExampleGreetingPreview"]   = "Begrüßung testen",
            ["hotkeySettings"]                = "Einstellungsmenü öffnen",
            ["hotkeyOverlay"]                 = "Hotkey-Overlay umschalten",
            ["hotkeyOneHitBlock"]             = "Block-One-Hit umschalten",
            ["hotkeyHomeTeleport"]            = "Zur Home-Position teleportieren",
            ["overlayEmpty"]                  = "Es sind noch keine Tastenkürzel registriert.",
            ["helpIntroHeading"]              = "Überblick",
            ["helpIntro"]                     = "Ruin the Fun bündelt eigene Funktionen an einem Ort. Nutze das Vintage Story-Steuerungsmenü, um Hotkeys anzupassen, sobald neue Funktionen hinzukommen.",
            ["helpHotkeysHeading"]            = "Registrierte Tastenkürzel",
            ["helpColumnHotkey"]              = "Taste",
            ["helpColumnDescription"]         = "Aktion",
            ["helpCategoryGeneral"]           = "Allgemein",
            ["helpNoHotkeys"]                 = "Derzeit sind keine Tastenkürzel registriert.",
            ["exampleGreetingChat"]           = "Ruin the Fun: {0}",
            ["exampleGreetingDefault"]        = "Ruin the Fun bereit!"
        },
        ["pt-br"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["help"]                          = "Ajuda",
            ["settingsTitle"]                 = "Ruin the Fun",
            ["settingsSectionGeneral"]        = "Geral",
            ["settingsLanguage"]              = "Idioma",
            ["settingsOpenHelp"]              = "Abrir ajuda",
            ["mainSectionGeneral"]            = "Recursos gerais",
            ["mainHomeNewLabel"]              = "Nome do novo home",
            ["mainHomeSetButton"]             = "Salvar aqui",
            ["mainHomeSavedLabel"]            = "Homes salvos",
            ["mainHomeNone"]                  = "Nenhum home salvo ainda. Digite um nome e clique em Salvar aqui.",
            ["mainHomeTeleportButton"]        = "Teleportar",
            ["mainHomeDeleteButton"]          = "Excluir",
            ["mainFlightHeading"]             = "Modo de voo",
            ["mainFlightToggle"]              = "Ativar voo",
            ["mainFlightHint"]                = "Pular/Agachar para subir/descer",
            ["hotkeyFlight"]                  = "Alternar modo de voo",
            ["hotkeyFastPlace"]               = "Alternar colocação rápida",
            ["mainFastPlaceHeading"]          = "Colocação rápida",
            ["mainFastPlaceToggle"]           = "Ativar",
            ["mainFastPlaceHint"]             = "Sem atraso entre colocações de blocos",
            ["tabFeatures"]                   = "Funções",
            ["tabHotkeys"]                    = "Teclas",
            ["hotkeyTabTitle"]                = "Reconfigurar teclas Ruin the Fun",
            ["hotkeyTabHint"]                 = "Digite o nome da tecla e clique em Definir. Nomes GlKeys: A–Z, F1–F12, Space, Return ...",
            ["hotkeyColAction"]               = "Ação",
            ["hotkeyColKey"]                  = "Tecla",
            ["hotkeySetBtn"]                  = "Definir",
            ["hotkeyDefaultBtn"]              = "Padrão",
            ["mainMovementHeading"]           = "Velocidade de movimento",
            ["mainMovementValue"]             = "Velocidade: {0}×",
            ["mainMovementHint"]              = "Ajusta a velocidade de movimento do jogador.",
            ["mainSectionUtility"]            = "Utilidades",
            ["mainOneHitHeading"]             = "Quebra em um golpe",
            ["mainOneHitToggle"]              = "Ativar quebra instantânea",
            ["mainOneHitHint"]                = "Quebra blocos instantaneamente enquanto ativo.",
            ["mainNoFallHeading"]             = "Sem dano de queda",
            ["mainNoFallToggle"]              = "Desativar dano de queda",
            ["settingsSectionHotkeys"]        = "Atalhos registrados",
            ["settingsHotkeysHint"]           = "Redefina as teclas no menu de controles do Vintage Story.",
            ["settingsNoHotkeys"]             = "Nenhum atalho registrado ainda.",
            ["settingsSectionExample"]        = "Componentes de exemplo",
            ["settingsExampleGreetingLabel"]  = "Ativar saudação de boas-vindas",
            ["settingsExampleGreetingHint"]   = "Mostra como localização, GUI e configuração funcionam juntas.",
            ["settingsExampleGreetingTextLabel"] = "Texto da saudação",
            ["settingsExampleGreetingTextHint"]  = "Esse valor é salvo no arquivo de configuração da mod.",
            ["settingsExampleGreetingPreview"]   = "Visualizar saudação",
            ["hotkeySettings"]                = "Abrir menu de configurações",
            ["hotkeyOverlay"]                 = "Alternar overlay de atalhos",
            ["hotkeyOneHitBlock"]             = "Alternar quebra em um golpe",
            ["hotkeyHomeTeleport"]            = "Teleportar para a home",
            ["overlayEmpty"]                  = "Ainda não há atalhos registrados.",
            ["helpIntroHeading"]              = "Visão geral",
            ["helpIntro"]                     = "O Ruin the Fun reúne recursos personalizados em um só lugar. Use o menu de controles do Vintage Story para redefinir atalhos sempre que adicionar novas funções.",
            ["helpHotkeysHeading"]            = "Atalhos registrados",
            ["helpColumnHotkey"]              = "Atalho",
            ["helpColumnDescription"]         = "Ação",
            ["helpCategoryGeneral"]           = "Geral",
            ["helpNoHotkeys"]                 = "Nenhum atalho está registrado no momento.",
            ["exampleGreetingChat"]           = "Ruin the Fun: {0}",
            ["exampleGreetingDefault"]        = "Ruin the Fun pronto!"
        }
    };

    private static readonly Dictionary<string, string> LanguageDisplayNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["en"]    = "English",
        ["de"]    = "Deutsch",
        ["pt-br"] = "Português (Brasil)"
    };

    public static string NormalizeLanguage(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return DefaultLanguage;

        if (Translations.ContainsKey(language))
            return language;

        return Translations.Keys.FirstOrDefault(code =>
            string.Equals(code, language, StringComparison.OrdinalIgnoreCase)) ?? DefaultLanguage;
    }

    public static bool HasLanguage(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return false;

        return Translations.Keys.Any(code =>
            string.Equals(code, language, StringComparison.OrdinalIgnoreCase));
    }

    public static IEnumerable<string> GetSupportedLanguageCodes() => Translations.Keys;

    public static string GetLanguageDisplayName(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return DefaultLanguage;

        foreach (var kvp in LanguageDisplayNames)
        {
            if (string.Equals(kvp.Key, language, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }

        return language;
    }

    public static string Get(string key, string language)
    {
        string lang = NormalizeLanguage(language);

        if (Translations.TryGetValue(lang, out var table) && table.TryGetValue(key, out var value))
            return value;

        if (!string.Equals(lang, DefaultLanguage, StringComparison.OrdinalIgnoreCase) &&
            Translations.TryGetValue(DefaultLanguage, out var fallback) &&
            fallback.TryGetValue(key, out value))
            return value;

        return key;
    }
}
