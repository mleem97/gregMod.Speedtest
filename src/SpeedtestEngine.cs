using System;
using System.Collections.Generic;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace GregModSpeedtest;

/// <summary>One throughput sample: bottleneck link speed + server egress.</summary>
internal struct SpeedSample
{
    public float DownLink;
    public float UpProc;
}

/// <summary>Finished test result (averages + jitter over samples).</summary>
internal sealed class SpeedResult
{
    public string TargetName = "";
    public float DownAvg;
    public float UpAvg;
    public float Jitter;
    public int Samples;
    public string Time = "";
}

/// <summary>
/// Measures REAL data streams: samples the aimed server's egress
/// (currentProcessingSpeed) and its connected links (connectionSpeed)
/// over a window. No invented metrics — DOWN is the link bottleneck,
/// UP is processing egress, JITTER is the down-sample deviation.
/// </summary>
internal static class SpeedtestEngine
{
    private const float MaxRayDistance = 48f;

    public static bool TryGetAimedServer(out Server server)
    {
        server = null;
        try
        {
            var cam = Camera.main;
            if (cam == null) return false;
            var ray = new Ray(cam.transform.position, cam.transform.forward);
            if (!Physics.Raycast(ray, out var hit, MaxRayDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
                return false;
            if (hit.collider == null) return false;
            server = hit.collider.GetComponentInParent<Server>();
            return server != null;
        }
        catch { server = null; return false; }
    }

    public static string ServerName(Server server)
    {
        try
        {
            var go = server?.gameObject;
            return go != null ? go.name ?? "Server" : "Server";
        }
        catch { return "Server"; }
    }

    public static List<SpeedSample> TakeSample(Server target, List<SpeedSample> into)
    {
        try
        {
            if (target == null || into == null) return into;
            float up = 0f;
            try { up = target.currentProcessingSpeed; } catch { }
            float down = 0f;
            bool any = false;
            try
            {
                foreach (var link in gregCore.Core.Networking.GregCables.FindByServer(target))
                {
                    if (link == null) continue;
                    float s = 0f;
                    try { s = link.connectionSpeed; } catch { continue; }
                    if (!any || s < down) down = s;
                    any = true;
                }
            }
            catch { }
            into.Add(new SpeedSample { DownLink = any ? down : 0f, UpProc = up });
        }
        catch { }
        return into;
    }

    public static SpeedResult Finish(string targetName, List<SpeedSample> samples)
    {
        var r = new SpeedResult { TargetName = targetName, Samples = samples != null ? samples.Count : 0 };
        try
        {
            r.Time = DateTime.Now.ToString("HH:mm:ss");
            if (samples == null || samples.Count == 0) return r;
            double downSum = 0, upSum = 0;
            foreach (var s in samples) { downSum += s.DownLink; upSum += s.UpProc; }
            r.DownAvg = (float)(downSum / samples.Count);
            r.UpAvg = (float)(upSum / samples.Count);
            double dev = 0;
            foreach (var s in samples) { var d = s.DownLink - r.DownAvg; dev += d * d; }
            r.Jitter = (float)Math.Sqrt(dev / samples.Count);
        }
        catch { }
        return r;
    }
}
