using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace RuinTheFun;

internal class RuinTheFunSystem : ModSystem
{
    // ── Hotkey record ─────────────────────────────────────────────────────────

    internal class RegisteredHotkey
    {
        public string Code             { get; }
        public string TranslationKey   { get; }
        public GlKeys DefaultKey       { get; }
        public bool   VisibleInOverlay { get; }
        public string HelpCategoryKey  { get; }

        internal RegisteredHotkey(string code, string translationKey, GlKeys defaultKey,
            bool visibleInOverlay, string helpCategoryKey)
        {
            Code = code; TranslationKey = translationKey; DefaultKey = defaultKey;
            VisibleInOverlay = visibleInOverlay; HelpCategoryKey = helpCategoryKey;
        }
    }

    // ── State ─────────────────────────────────────────────────────────────────

    private float movementSpeedMultiplier = 1f;
    private long  movementSpeedListenerId;

    private bool  noFallActive;
    private long  noFallLoopId;
    private readonly Vec3d lastGroundedPos = new Vec3d();
    private bool  hasLastGroundedPos;
    private const float NoFallVelLimit   = -0.08f;
    private const float NoFallTriggerVel = -0.75f;

    private bool       oneHitActive;
    private long       oneHitLoopId;
    private float      prevMiningMod = 1f;
    private MethodInfo destroyBlockMethod;

    private bool         flightActive;
    private long         flightLoopId;

    private bool              fastPlaceActive;
    private long              fastPlaceLoopId;


    private readonly Dictionary<string, RegisteredHotkey> registeredHotkeys =
        new Dictionary<string, RegisteredHotkey>(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, (GlKeys OldKey, GlKeys NewKey)> HotkeyMigrations =
        new Dictionary<string, (GlKeys, GlKeys)>(StringComparer.OrdinalIgnoreCase)
        {
            ["ruinthefunonehitblock"] = (GlKeys.O, GlKeys.K)
        };

    private ICoreClientAPI          api;
    private RuinTheFunGUI        settingsDialog;
    private RuinTheFunHelpDialog helpDialog;
    private RuinTheFunHotkeyOverlayHud        hotkeyOverlay;
    private RuinTheFunConfig     config = new RuinTheFunConfig();
    private const string ConfigFile = "RuinTheFunConfig.json";

    // ── Public properties ─────────────────────────────────────────────────────

    internal static RuinTheFunSystem Instance { get; private set; }
    public ICoreClientAPI Api => api;

    internal bool   ExampleGreetingEnabled => config?.ExampleGreetingEnabled ?? true;
    internal string ExampleGreetingText    => config?.ExampleGreetingText ?? string.Empty;

    public float MovementSpeedMultiplier => movementSpeedMultiplier;
    public bool  NoFallEnabled           => noFallActive;
    public bool  OneHitBlockEnabled      => oneHitActive;
    public bool  FlightEnabled           => flightActive;
    public bool  FastPlaceEnabled        => fastPlaceActive;

    public string CurrentLanguage => ResolveLanguage(config?.Language);

    // ── NotifyPlayer ──────────────────────────────────────────────────────────
    // Sends to the VS info log channel (separate from general chat).
    // Reflection keeps this compiling across VS versions.
    // Falls back to the game logger (file only) if the channel can't be found.

    private void NotifyPlayer(string message)
    {
        if (api is null) return;
        try
        {
            var field = typeof(GlobalConstants).GetField("InfoLogChatGroup",
                BindingFlags.Public | BindingFlags.Static);
            if (field != null)
            {
                int channel = (int)field.GetValue(null);
                var method = api.GetType().GetMethod("ShowChatMessage",
                    BindingFlags.Instance | BindingFlags.Public,
                    null, new Type[] { typeof(string), typeof(int) }, null);
                if (method != null) { method.Invoke(api, new object[] { message, channel }); return; }
            }
        }
        catch { }
        api.Logger.Notification(message);
    }

    // ── Multi-home ────────────────────────────────────────────────────────────

    public Dictionary<string, HomeEntry> GetAllHomes()
    {
        if (config?.HomePoses is null) return new Dictionary<string, HomeEntry>();
        return new Dictionary<string, HomeEntry>(config.HomePoses);
    }

    public bool TryAddOrUpdateHome(string name, out string err)
    {
        err = string.Empty;
        if (string.IsNullOrWhiteSpace(name)) { err = "Name cannot be empty."; return false; }
        var entity = api?.World?.Player?.Entity;
        if (entity is null) { err = "Player not ready."; return false; }
        var entry = new HomeEntry(entity.Pos.X, entity.Pos.Y, entity.Pos.Z);
        config.HomePoses ??= new Dictionary<string, HomeEntry>();
        config.HomePoses[name.Trim()] = entry;
        SaveConfig();
        NotifyPlayer(string.Format(CultureInfo.InvariantCulture,
            "Home '{0}' saved at {1:0.0}, {2:0.0}, {3:0.0}", name.Trim(), entry.X, entry.Y, entry.Z));
        settingsDialog?.RefreshFeatureStates();
        return true;
    }

    public bool TryDeleteHome(string name)
    {
        if (config?.HomePoses is null || !config.HomePoses.ContainsKey(name)) return false;
        config.HomePoses.Remove(name); SaveConfig();
        settingsDialog?.RefreshFeatureStates();
        return true;
    }

    public bool TryTeleportToHome(string name)
    {
        if (api is null) return false;
        if (config?.HomePoses is null || !config.HomePoses.TryGetValue(name, out HomeEntry entry))
        { NotifyPlayer("Home '" + name + "' not found."); return false; }

        // Use the /tp command rather than setting Pos directly.
        // SetPos only moves the client-side entity and snaps back at range;
        // the server command works at any distance and in any dimension.
        string player = api.World?.Player?.PlayerName ?? string.Empty;
        string cmd = string.Format(CultureInfo.InvariantCulture,
            "/tp {0} {1:0.##} {2:0.##} {3:0.##}", player, entry.X, entry.Y, entry.Z);
        api.SendChatMessage(cmd);

        NotifyPlayer(string.Format(CultureInfo.InvariantCulture,
            "Teleporting to '{0}' ({1:0.0}, {2:0.0}, {3:0.0})", name, entry.X, entry.Y, entry.Z));
        return true;
    }

    public bool TryTeleportHome()
    {
        if (config?.HomePoses is null || config.HomePoses.Count == 0)
        { NotifyPlayer("No home positions saved."); return false; }
        return TryTeleportToHome(config.HomePoses.Keys.First());
    }

    // ── Hotkey rebinding ──────────────────────────────────────────────────────

    public string GetCurrentKeyName(string code, GlKeys defaultKey)
    {
        var hk = api?.Input?.GetHotKeyByCode(code);
        if (hk?.CurrentMapping != null)
        {
            int kc = hk.CurrentMapping.KeyCode;
            if (Enum.IsDefined(typeof(GlKeys), kc)) return ((GlKeys)kc).ToString();
        }
        if (config?.Hotkeys != null && config.Hotkeys.TryGetValue(code, out string stored)
            && !string.IsNullOrWhiteSpace(stored)) return stored;
        return defaultKey.ToString();
    }

    public bool TryRebindHotkey(string code, string keyName)
    {
        if (!Enum.TryParse(keyName.Trim(), true, out GlKeys key)) return false;
        var hk = api?.Input?.GetHotKeyByCode(code);
        if (hk != null) hk.CurrentMapping = new KeyCombination { KeyCode = (int)key };
        config.Hotkeys ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        config.Hotkeys[code] = key.ToString();
        SaveConfig();
        RefreshHotkeyDisplays();
        return true;
    }

    public void ResetHotkeyToDefault(string code)
    {
        var hk = GetHotkey(code);
        if (hk != null) TryRebindHotkey(code, hk.DefaultKey.ToString());
    }

    // ── ModSystem lifecycle ───────────────────────────────────────────────────

    public override bool ShouldLoad(EnumAppSide forSide) => true;

    public override void StartClientSide(ICoreClientAPI api)
    {
        Instance = this; this.api = api;
        LoadConfig(); EnsureLanguage();
        RegisterBaseHotkeys();
        EnsureSettingsDialog(); EnsureHotkeyOverlay();
        InitializeFeatures();
        ShowExampleGreetingIfEnabled();
    }

    // ── Config ────────────────────────────────────────────────────────────────

    internal void SaveConfig()
    {
        if (api is null || config is null) return;
        try { api.StoreModConfig(config, ConfigFile); }
        catch (Exception ex) { api.Logger.Warning("Ruin the Fun: save failed: " + ex.Message); }
    }

    private void LoadConfig()
    {
        if (api is null) return;
        try
        {
            var loaded = api.LoadModConfig<RuinTheFunConfig>(ConfigFile);
            if (loaded is null) { SaveConfig(); return; }
            config = loaded;
            config.Hotkeys ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (config.Hotkeys.Comparer != StringComparer.OrdinalIgnoreCase)
                config.Hotkeys = new Dictionary<string, string>(config.Hotkeys, StringComparer.OrdinalIgnoreCase);
            config.HomePoses ??= new Dictionary<string, HomeEntry>();
            config.ExampleGreetingText ??= "Ruin the Fun ready!";
            bool dirty = UpgradeLegacyHotkeys(config.Hotkeys);
            if (float.IsNaN(config.MovementSpeedMultiplier) || float.IsInfinity(config.MovementSpeedMultiplier))
            { config.MovementSpeedMultiplier = 1f; dirty = true; }
            float cl = GameMath.Clamp(config.MovementSpeedMultiplier, 1f, 3f);
            if (Math.Abs(config.MovementSpeedMultiplier - cl) > 0.0001f) { config.MovementSpeedMultiplier = cl; dirty = true; }
            if (dirty) SaveConfig();
        }
        catch (Exception ex) { api?.Logger.Warning("Ruin the Fun: load failed: " + ex.Message); }
    }

    private bool UpgradeLegacyHotkeys(Dictionary<string, string> hotkeys)
    {
        bool changed = false;
        foreach (var m in HotkeyMigrations)
        {
            if (!hotkeys.TryGetValue(m.Key, out string s) || string.IsNullOrWhiteSpace(s)) continue;
            if ((Enum.TryParse(s, true, out GlKeys p) && p == m.Value.OldKey)
                || s.Equals(m.Value.OldKey.ToString(), StringComparison.OrdinalIgnoreCase))
            { hotkeys[m.Key] = m.Value.NewKey.ToString(); changed = true; }
        }
        return changed;
    }

    // ── Language ──────────────────────────────────────────────────────────────

    private string ResolveLanguage(string req)
    {
        if (!string.IsNullOrWhiteSpace(req) && Localization.HasLanguage(req))
            return Localization.NormalizeLanguage(req);
        string loc = Lang.CurrentLocale ?? Lang.DefaultLocale;
        return Localization.HasLanguage(loc) ? Localization.NormalizeLanguage(loc) : "en";
    }

    private void EnsureLanguage()
    {
        string lang = ResolveLanguage(config?.Language);
        if (!string.Equals(config?.Language, lang, StringComparison.OrdinalIgnoreCase))
        { config.Language = lang; SaveConfig(); }
    }

    public void SetLanguage(string language)
    {
        string lang = ResolveLanguage(language);
        if (string.Equals(config.Language, lang, StringComparison.OrdinalIgnoreCase)) return;
        config.Language = lang; SaveConfig(); RefreshLanguage();
    }

    // ── Dialogs ───────────────────────────────────────────────────────────────

    private void EnsureSettingsDialog()
    {
        if (api is null) return;
        if (settingsDialog is null) { settingsDialog = new RuinTheFunGUI(api, this); api.Gui.RegisterDialog(settingsDialog); }
        else settingsDialog.RefreshLanguage();
    }

    private void EnsureHotkeyOverlay()
    {
        if (api is null) return;
        if (hotkeyOverlay is null) { hotkeyOverlay = new RuinTheFunHotkeyOverlayHud(api, this); api.Gui.RegisterDialog(hotkeyOverlay); }
        else hotkeyOverlay.RefreshContent();
    }

    private void EnsureHelpDialog()
    {
        if (api is null) return;
        if (helpDialog is null) { helpDialog = new RuinTheFunHelpDialog(api, this); api.Gui.RegisterDialog(helpDialog); }
        else helpDialog.RefreshLanguage();
    }

    public void OpenHelpDialog()
    { EnsureHelpDialog(); if (!helpDialog.IsOpened()) helpDialog.TryOpen(); }

    // ── Hotkey registration ───────────────────────────────────────────────────

    private void RegisterBaseHotkeys()
    {
        RegisterHotkey("ruinthefunsettings",      "hotkeySettings",     GlKeys.R, ToggleSettings);
        RegisterHotkey("ruinthefunhotkeyoverlay", "hotkeyOverlay",      GlKeys.O, ToggleHotkeyOverlay);
        RegisterHotkey("ruinthefunonehitblock",   "hotkeyOneHitBlock",  GlKeys.K, OnOneHitKey);
        RegisterHotkey("ruinthefunhomeport",      "hotkeyHomeTeleport", GlKeys.L, OnPortKey);
        RegisterHotkey("ruinthefunflight",        "hotkeyFlight",       GlKeys.F, OnFlightKey);
        RegisterHotkey("ruinthefunfastplace",     "hotkeyFastPlace",    GlKeys.G, OnFastPlaceKey);
    }

    public void RegisterHotkey(string code, string translKey, GlKeys defaultKey,
        ActionConsumable<KeyCombination> handler, bool overlay = true, string cat = "helpCategoryGeneral")
    {
        if (api is null) return;
        GlKeys effectiveKey = defaultKey;
        if (config.Hotkeys != null && config.Hotkeys.TryGetValue(code, out string s)
            && Enum.TryParse(s, true, out GlKeys p)) effectiveKey = p;
        string name = Localization.Get(translKey, CurrentLanguage);
        api.Input.RegisterHotKey(code, name, effectiveKey, HotkeyType.CharacterControls);
        if (handler != null) api.Input.SetHotKeyHandler(code, handler);
        config.Hotkeys ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!config.Hotkeys.ContainsKey(code)) { config.Hotkeys[code] = effectiveKey.ToString(); SaveConfig(); }
        registeredHotkeys[code] = new RegisteredHotkey(code, translKey, defaultKey, overlay, cat);
        RefreshHotkeyDisplays();
    }

    public string FormatHotkey(string code, GlKeys fallback)
    {
        if (api is null) return Norm(GlKeyNames.ToString(fallback) ?? fallback.ToString());
        var hk = api.Input.GetHotKeyByCode(code);
        if (hk?.CurrentMapping != null) { string t = hk.CurrentMapping.ToString(); if (!string.IsNullOrWhiteSpace(t)) return Norm(t); }
        return Norm(GlKeyNames.ToString(fallback) ?? fallback.ToString());
    }

    private static string Norm(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s ?? string.Empty;
        return s.StartsWith("Keypad", StringComparison.OrdinalIgnoreCase)
            ? "Numpad" + s.Substring("Keypad".Length) : s;
    }

    private bool ToggleSettings(KeyCombination k)
    { EnsureSettingsDialog(); if (settingsDialog.IsOpened()) settingsDialog.TryClose(); else settingsDialog.TryOpen(); return true; }

    private bool ToggleHotkeyOverlay(KeyCombination k)
    { EnsureHotkeyOverlay(); if (hotkeyOverlay.IsOpened()) hotkeyOverlay.TryClose(); else { hotkeyOverlay.RefreshContent(); hotkeyOverlay.TryOpen(); } return true; }

    // ── Hotkey queries ────────────────────────────────────────────────────────

    public IEnumerable<RegisteredHotkey> GetRegisteredHotkeys() =>
        registeredHotkeys.Values.OrderBy(h => Localization.Get(h.TranslationKey, CurrentLanguage));

    public RegisteredHotkey GetHotkey(string code)
    { registeredHotkeys.TryGetValue(code, out RegisteredHotkey h); return h; }

    public IReadOnlyCollection<string> GetHelpCategories() =>
        (from h in registeredHotkeys.Values select h.HelpCategoryKey into k group k by k into g select g.Key)
        .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(k => Localization.Get(k, CurrentLanguage)).ToArray();

    public IEnumerable<RegisteredHotkey> GetHotkeysForCategory(string cat) =>
        registeredHotkeys.Values.Where(h => string.Equals(h.HelpCategoryKey, cat, StringComparison.OrdinalIgnoreCase))
            .OrderBy(h => Localization.Get(h.TranslationKey, CurrentLanguage));

    public IEnumerable<RegisteredHotkey> GetOverlayHotkeys() =>
        registeredHotkeys.Values.Where(h => h.VisibleInOverlay)
            .OrderBy(h => Localization.Get(h.TranslationKey, CurrentLanguage));

    // ── Refresh ───────────────────────────────────────────────────────────────

    internal void RefreshHotkeyDisplays()
    { hotkeyOverlay?.RefreshContent(); settingsDialog?.RefreshHotkeys(); helpDialog?.RefreshContent(); }

    internal void RefreshLanguage()
    { settingsDialog?.RefreshLanguage(); hotkeyOverlay?.RefreshContent(); helpDialog?.RefreshLanguage(); }

    private void Notify() => hotkeyOverlay?.RefreshContent();

    // ── Feature initialisation ────────────────────────────────────────────────

    private void InitializeFeatures()
    {
        InitMovementSpeed(); InitOneHit(); InitNoFall(); InitFlight(); InitFastPlace();
        settingsDialog?.RefreshFeatureStates();
    }

    private void ShowExampleGreetingIfEnabled()
    {
        if (config?.ExampleGreetingEnabled != true) return;
        string txt = string.IsNullOrWhiteSpace(config.ExampleGreetingText)
            ? Localization.Get("exampleGreetingDefault", CurrentLanguage) : config.ExampleGreetingText;
        api?.ShowChatMessage(string.Format(Localization.Get("exampleGreetingChat", CurrentLanguage), txt));
    }

    private bool OnPortKey(KeyCombination k) { TryTeleportHome(); return true; }

    // ── Movement speed ────────────────────────────────────────────────────────

    private void InitMovementSpeed()
    {
        movementSpeedMultiplier = GameMath.Clamp(config?.MovementSpeedMultiplier ?? 1f, 1f, 3f);
        if (api != null && movementSpeedListenerId == 0L)
            movementSpeedListenerId = api.Event.RegisterGameTickListener(_ => ApplyMovementSpeed(), 50);
        ApplyMovementSpeed();
    }

    public void SetMovementSpeedMultiplier(float m)
    {
        float c = GameMath.Clamp(m, 1f, 3f);
        if (Math.Abs(movementSpeedMultiplier - c) < 0.0001f) return;
        movementSpeedMultiplier = c;
        if (config != null) { config.MovementSpeedMultiplier = c; SaveConfig(); }
        ApplyMovementSpeed(); settingsDialog?.RefreshFeatureStates();
    }

    private void ApplyMovementSpeed()
    {
        var p = api?.World?.Player; if (p?.Entity is null) return;
        if (Math.Abs(p.WorldData.MoveSpeedMultiplier - movementSpeedMultiplier) > 0.0001f)
            p.WorldData.MoveSpeedMultiplier = movementSpeedMultiplier;
        var ctrl = ((EntityAgent)p.Entity).Controls;
        if (ctrl != null && Math.Abs(ctrl.MovespeedMultiplier - movementSpeedMultiplier) > 0.0001f)
            ctrl.MovespeedMultiplier = movementSpeedMultiplier;
    }

    // ── No-fall ───────────────────────────────────────────────────────────────

    private void InitNoFall() => TogNoFall(config?.NoFallEnabled ?? false, false, false);
    public void SetNoFallEnabled(bool e) => TogNoFall(e, true, true);

    private void TogNoFall(bool enable, bool fb, bool persist)
    {
        if (api is null) { noFallActive = enable; return; }
        bool prev = noFallActive; noFallActive = enable;
        if (enable) { if (noFallLoopId == 0L) { noFallLoopId = api.Event.RegisterGameTickListener(NoFallTick, 10); hasLastGroundedPos = false; if (fb) NotifyPlayer("No-fall enabled."); } }
        else if (noFallLoopId != 0L) { api.Event.UnregisterGameTickListener(noFallLoopId); noFallLoopId = 0L; if (fb) NotifyPlayer("No-fall disabled."); }
        if (!enable) hasLastGroundedPos = false;
        if (persist && config != null && config.NoFallEnabled != enable) { config.NoFallEnabled = enable; SaveConfig(); }
        if (prev != enable || fb) Notify();
        settingsDialog?.RefreshFeatureStates();
    }

    private void NoFallTick(float dt)
    {
        if (!noFallActive) return;
        var player = api?.World?.Player?.Entity as EntityPlayer; if (player is null) return;
        EntityPos pos = ((Entity)player).Pos; if (pos is null) return;
        bool liq = ((Entity)player).Swimming || ((Entity)player).FeetInLiquid;
        if (((Entity)player).OnGround || liq || (((Entity)player).CollidedVertically && pos.Motion.Y >= -0.02))
        { lastGroundedPos.Set(((Entity)player).ServerPos.X, ((Entity)player).ServerPos.Y, ((Entity)player).ServerPos.Z); hasLastGroundedPos = true; ((Entity)player).PositionBeforeFalling.Set(((Entity)player).ServerPos.X, ((Entity)player).ServerPos.Y, ((Entity)player).ServerPos.Z); return; }
        if (pos.Motion.Y < -0.02) ((Entity)player).PositionBeforeFalling.Set(((Entity)player).ServerPos.X, ((Entity)player).ServerPos.Y, ((Entity)player).ServerPos.Z);
        if (pos.Motion.Y < NoFallVelLimit) { double cap = Math.Max(pos.Motion.Y, NoFallVelLimit); pos.Motion.Y = cap; ((Entity)player).ServerPos.Motion.Y = cap; }
        if (!hasLastGroundedPos || pos.Motion.Y >= NoFallTriggerVel) return;
        var ba = api?.World?.BlockAccessor; if (ba is null) return;
        var below = pos.AsBlockPos.Copy(); below.Y--;
        var block = ba.GetBlock(below);
        if (block != null && block.CollisionBoxes != null && block.CollisionBoxes.Length > 0)
        { ((Entity)player).ServerPos.X = lastGroundedPos.X; ((Entity)player).ServerPos.Y = lastGroundedPos.Y + 0.1; ((Entity)player).ServerPos.Z = lastGroundedPos.Z; ((Entity)player).ServerPos.Motion.Set(0,0,0); ((Entity)player).Pos.SetFrom(((Entity)player).ServerPos); ((Entity)player).Pos.Motion.Set(0,0,0); ((Entity)player).PositionBeforeFalling.Set(((Entity)player).ServerPos.X, ((Entity)player).ServerPos.Y, ((Entity)player).ServerPos.Z); ((Entity)player).OnGround = true; ((Entity)player).CollidedVertically = true; }
    }

    // ── One-hit block ─────────────────────────────────────────────────────────

    private void InitOneHit() => TogOneHit(config?.OneHitBlockEnabled ?? false, false, false);
    public void SetOneHitBlockEnabled(bool e) => TogOneHit(e, true, true);
    private bool OnOneHitKey(KeyCombination k) { TogOneHit(!oneHitActive, true, true); return true; }

    private void TogOneHit(bool enable, bool fb, bool persist)
    {
        if (api is null) { oneHitActive = enable; return; }
        bool prev = oneHitActive; oneHitActive = enable;
        if (enable)
        { if (oneHitLoopId == 0L) { destroyBlockMethod = api.World.GetType().GetMethod("OnPlayerTryDestroyBlock", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); oneHitLoopId = api.Event.RegisterGameTickListener(OneHitTick, 30); prevMiningMod = GlobalConstants.ToolMiningSpeedModifier; GlobalConstants.ToolMiningSpeedModifier = 50f; if (fb) NotifyPlayer("One-hit block enabled."); } }
        else if (oneHitLoopId != 0L) { api.Event.UnregisterGameTickListener(oneHitLoopId); oneHitLoopId = 0L; GlobalConstants.ToolMiningSpeedModifier = prevMiningMod; if (fb) NotifyPlayer("One-hit block disabled."); }
        if (persist && config != null && config.OneHitBlockEnabled != enable) { config.OneHitBlockEnabled = enable; SaveConfig(); }
        if (prev != enable || fb) Notify();
        settingsDialog?.RefreshFeatureStates();
    }

    private void OneHitTick(float dt)
    {
        if (!oneHitActive || api?.World is null || !api.Input.InWorldMouseButton.Left) return;
        var sel = api.World.Player?.CurrentBlockSelection; if (sel is null) return;
        var block = api.World.BlockAccessor.GetBlock(sel.Position);
        if (block is null || block.Id == 0) return;
        if (destroyBlockMethod != null)
            try { destroyBlockMethod.Invoke(api.World, new object[] { new BlockSelection { Position = sel.Position, Block = block, Face = sel.Face, HitPosition = sel.HitPosition } }); } catch { }
    }

    // ── Flight ────────────────────────────────────────────────────────────────

    private void InitFlight() => TogFlight(config?.FlightEnabled ?? false, false, false);
    public void SetFlightEnabled(bool e) => TogFlight(e, true, true);
    private bool OnFlightKey(KeyCombination k) { TogFlight(!flightActive, true, true); return true; }

    private void TogFlight(bool enable, bool fb, bool persist)
    {
        if (api is null) { flightActive = enable; return; }
        bool prev = flightActive; flightActive = enable;
        if (enable) { if (flightLoopId == 0L) { flightLoopId = api.Event.RegisterGameTickListener(_ => { if (flightActive) ApplyFlight(true); }, 20); ApplyFlight(true); if (fb) NotifyPlayer("Flight enabled. Jump/Crouch to ascend/descend."); } }
        else if (flightLoopId != 0L) { api.Event.UnregisterGameTickListener(flightLoopId); flightLoopId = 0L; ApplyFlight(false); if (fb) NotifyPlayer("Flight disabled."); }
        if (persist && config != null && config.FlightEnabled != enable) { config.FlightEnabled = enable; SaveConfig(); }
        if (prev != enable || fb) Notify();
        settingsDialog?.RefreshFeatureStates();
    }

    private void ApplyFlight(bool on)
    {
        var p = api?.World?.Player; if (p is null) return;
        if (p.WorldData.FreeMove != on) p.WorldData.FreeMove = on;
        if (!on) { var e = p.Entity; if (e?.Pos != null && e.Pos.Motion.Y > 0) e.Pos.Motion.Y = 0; }
    }

    // ── Fast block placement ──────────────────────────────────────────────────
    // Zeroes the game's internal block-placement cooldown fields every tick so
    // the player can place blocks back-to-back with no delay.
    // The field search uses reflection so it stays compatible across VS versions.

    private void InitFastPlace() => TogFastPlace(config?.FastPlaceEnabled ?? false, false, false);
    public void SetFastPlaceEnabled(bool e) => TogFastPlace(e, true, true);
    private bool OnFastPlaceKey(KeyCombination k) { TogFastPlace(!fastPlaceActive, true, true); return true; }

    private void TogFastPlace(bool enable, bool fb, bool persist)
    {
        if (api is null) { fastPlaceActive = enable; return; }
        bool prev = fastPlaceActive; fastPlaceActive = enable;

        if (enable)
        {
            if (fastPlaceLoopId == 0L)
            {
                fastPlaceLoopId = api.Event.RegisterGameTickListener(FastPlaceTick, 10);
                if (fb) NotifyPlayer("Fast place enabled.");
            }
        }
        else if (fastPlaceLoopId != 0L)
        {
            api.Event.UnregisterGameTickListener(fastPlaceLoopId);
            fastPlaceLoopId = 0L;
            if (fb) NotifyPlayer("Fast place disabled.");
        }

        if (persist && config != null && config.FastPlaceEnabled != enable)
        { config.FastPlaceEnabled = enable; SaveConfig(); }
        if (prev != enable || fb) Notify();
        settingsDialog?.RefreshFeatureStates();
    }

    private void FastPlaceTick(float dt)
    {
        if (!fastPlaceActive || api?.World is null) return;

        // VS stores per-item timing (repeat delays, cooldowns) as TempAttributes
        // on the held ItemStack. Clearing these attributes each tick removes the
        // placement delay without touching any camera or mouse fields.
        var player = api.World.Player?.Entity as EntityAgent;
        if (player is null) return;

        foreach (var slot in new ItemSlot[] { player.RightHandItemSlot, player.LeftHandItemSlot })
        {
            if (slot?.Itemstack?.TempAttributes is null) continue;
            try
            {
                var attrs = slot.Itemstack.TempAttributes;
                attrs.RemoveAttribute("repeatDelay");
                attrs.RemoveAttribute("cooldown");
                attrs.RemoveAttribute("interactCooldown");
                attrs.RemoveAttribute("aimTimeout");
                attrs.RemoveAttribute("useDelay");
                attrs.RemoveAttribute("blockPlaceDelay");
            }
            catch { }
        }
    }
}
