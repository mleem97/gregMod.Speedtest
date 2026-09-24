using MelonLoader;
using System;

[assembly: MelonInfo(typeof(GregModSpeedtest.SpeedtestMod), "gregMod.Speedtest", "0.1.0", "mleem97")]
[assembly: MelonGame("Waseku", "Data Center")]

namespace GregModSpeedtest;

// Hard dependency on gregCore (UI fully central). Fail fast without the DLL.
public sealed class SpeedtestMod : MelonMod
{
    private const string CoreProbeType = "gregCore.UI.GregNotificationManager, gregCore";
    private bool _disabled;

    public override void OnInitializeMelon()
    {
        bool hasCore = false;
        try { hasCore = Type.GetType(CoreProbeType) != null; } catch { }
        if (!hasCore)
        {
            LoggerInstance.Error("[Speedtest] gregCore not found — hard dependency, staying disabled. Put gregCore.dll in Mods/.");
            _disabled = true;
            return;
        }

        try
        {
            var cat = MelonPreferences.CreateCategory("Speedtest");
            var keyEntry = cat.CreateEntry("ToggleKey", "F4", "ToggleKey",
                "Hotkey to open/close the Speedtest panel.");
            SpeedtestFeature.ConfigureToggleKey(keyEntry.Value);
        }
        catch { }
        MelonLogger.Msg($"[Speedtest] v0.1.0 loaded (gregCore UI). {SpeedtestFeature.ToggleKeyLabel} = Speedtest panel.");
        try { RegisterCoreExtras(); } catch { }
    }

    private void RegisterCoreExtras()
    {
        try
        {
            gregCore.Core.Mods.GregModRegistry.Register(
                "gregMod.Speedtest", "Speedtest", "0.1.0",
                new string[] { "speedtest" });
            gregCore.UI.GregHudRegistry.Register("speedtest",
                SpeedtestFeature.ToggleKeyLabel, "Speed");
            gregCore.UI.GregMenuBinding.BindToggle("speedtest",
                SpeedtestFeature.ToggleVisibility, () => SpeedtestFeature.IsVisible);
        }
        catch (System.Exception ex)
        {
            MelonLogger.Warning("[Speedtest] Hub registration failed: " + ex.GetBaseException().Message);
        }
    }

    public override void OnUpdate()
    {
        if (_disabled) return;
        SpeedtestFeature.Update();
    }

    public override void OnDeinitializeMelon()
    {
        SpeedtestFeature.Shutdown();
    }
}
