using System;
using System.Collections.Generic;
using Il2Cpp;
using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GregModSpeedtest;

/// <summary>
/// Panel + test state machine (Toolkit only, no IMGUI).
/// Toggle via hotkey or F1 hub; progress + results + history.
/// </summary>
internal static class SpeedtestFeature
{
    private const float TestDurationSeconds = 5f;
    private const float SampleIntervalSeconds = 0.1f;
    private const int MaxHistory = 5;

    private static bool _visible;
    private static Key _toggleKey = Key.F4;
    private static gregCore.UI.GregPanelBuilder _panel;

    private static bool _running;
    private static Server _target;
    private static string _targetName = "";
    private static readonly List<SpeedSample> _samples = new();
    private static float _nextSampleAt;
    private static float _testEndsAt;
    private static SpeedResult _lastResult;
    private static readonly List<SpeedResult> _history = new();

    public static string ToggleKeyLabel
    {
        get { try { return _toggleKey.ToString(); } catch { return "F4"; } }
    }

    public static bool IsVisible
    {
        get
        {
            try { if (_panel != null) return _panel.IsVisible; } catch { }
            return _visible;
        }
    }

    public static void ConfigureToggleKey(string raw)
    {
        try
        {
            if (Enum.TryParse<Key>(raw, true, out var k) && k != Key.None)
                _toggleKey = k;
            else
                MelonLogger.Warning($"[Speedtest] Unknown ToggleKey '{raw}', defaulting to F4.");
        }
        catch { }
    }

    public static void ToggleVisibility()
    {
        _visible = !_visible;
        if (_visible)
        {
            RebuildPanel();
            try { _panel?.Show(); } catch { }
        }
        else
        {
            try { _panel?.Hide(); } catch { }
        }
        try { ReportOpenState(); } catch { }
    }

    private static void ReportOpenState()
    {
        try { gregCore.UI.GregMenuRegistry.SetOpen("speedtest", IsVisible); } catch { }
    }

    public static void Shutdown()
    {
        try { _panel?.Hide(); } catch { }
        _panel = null;
        _running = false;
        _target = null;
    }

    public static void Update()
    {
        if (_running) PumpTest();

        var kb = Keyboard.current;
        if (kb == null) return;
        try
        {
            var ctrl = kb[_toggleKey];
            if (ctrl != null && ctrl.wasPressedThisFrame)
                ToggleVisibility();
        }
        catch { }
    }

    private static void RunTest()
    {
        try
        {
            if (_running) return;
            if (!SpeedtestEngine.TryGetAimedServer(out var server) || server == null)
            {
                try { gregCore.UI.GregNotificationManager.Show("Aim at a server, then run the test.", 4f); } catch { }
                return;
            }
            _target = server;
            _targetName = SpeedtestEngine.ServerName(server);
            _samples.Clear();
            _running = true;
            _testEndsAt = Time.unscaledTime + TestDurationSeconds;
            _nextSampleAt = 0f;
            RebuildPanel();
            try { _panel?.Show(); _visible = true; ReportOpenState(); } catch { }
        }
        catch { _running = false; }
    }

    private static void PumpTest()
    {
        try
        {
            if (_target == null) { _running = false; RebuildPanel(); return; }
            if (Time.unscaledTime >= _nextSampleAt)
            {
                _nextSampleAt = Time.unscaledTime + SampleIntervalSeconds;
                SpeedtestEngine.TakeSample(_target, _samples);
                if (Time.frameCount % 5 == 0) RebuildPanel();
            }
            if (Time.unscaledTime >= _testEndsAt || _samples.Count >= 60)
            {
                _running = false;
                _lastResult = SpeedtestEngine.Finish(_targetName, _samples);
                _history.Insert(0, _lastResult);
                while (_history.Count > MaxHistory) _history.RemoveAt(_history.Count - 1);
                _target = null;
                RebuildPanel();
                try
                {
                    gregCore.UI.GregNotificationManager.ShowRich("SPEEDTEST",
                        $"{_lastResult.DownAvg:F1} down · {_lastResult.UpAvg:F1} up",
                        $"{_lastResult.TargetName} · jitter {_lastResult.Jitter:F1} ({_lastResult.Samples} samples)", null, null, 6f);
                }
                catch { }
            }
        }
        catch { _running = false; RebuildPanel(); }
    }

    private static void RebuildPanel()
    {
        try
        {
            if (_panel == null)
            {
                try { _panel = gregCore.UI.GregPanelBuilder.Create("Speedtest").Build(); }
                catch { _panel = null; }
            }
            if (_panel == null) return;
            _panel.ClearContent();
            _panel.AddHeadline("Target");
            if (_running)
            {
                float pct = 0f;
                try
                {
                    pct = 1f - (_testEndsAt - Time.unscaledTime) / TestDurationSeconds;
                    pct = Mathf.Clamp01(pct);
                }
                catch { }
                _panel.AddLabel($"{_targetName} — testing… {(int)(pct * 100f)}% ({_samples.Count} samples)");
            }
            else
            {
                _panel.AddLabel("Aim at a server, then run.");
            }
            _panel.AddButton(_running ? "Testing…" : "Run test", RunTest);
            _panel.AddSecondaryButton("Close", ToggleVisibility);
            _panel.AddSeparator();
            if (_lastResult != null)
            {
                _panel.AddHeadline("Last result");
                _panel.AddLabel($"{_lastResult.TargetName} ({_lastResult.Time})");
                _panel.AddLabel($"DOWN {_lastResult.DownAvg:F1} (link) · UP {_lastResult.UpAvg:F1} (proc)");
                _panel.AddLabel($"JITTER {_lastResult.Jitter:F1} · {_lastResult.Samples} samples · game units");
                _panel.AddSeparator();
            }
            if (_history.Count > 0)
            {
                _panel.AddHeadline("History");
                foreach (var h in _history)
                {
                    if (h == null) continue;
                    _panel.AddLabel($"{h.Time} {h.TargetName}: {h.DownAvg:F1}/{h.UpAvg:F1}/{h.Jitter:F1}");
                }
            }
        }
        catch { }
    }
}
