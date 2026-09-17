using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using ValheimVehicles.BepInExConfig;

namespace ValheimVehicles.Helpers;

public static class LoopTracker
{
  public static bool Enabled => VehicleGuiMenuConfig.EnableLoopLogging?.Value ?? false;

  private class LoopStats
  {
    public int CallCount;
    public int ItemCount;
    public double TotalElapsedMs;
    public double MaxElapsedMs;
    public float LastReportTime;
  }

  private static readonly Dictionary<string, LoopStats> _stats = new();
  private static readonly object _lock = new();

  private static float GetCurrentTime()
  {
    try
    {
      return Time.time;
    }
    catch
    {
      return (float)(Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency);
    }
  }

  public struct LoopScope : IDisposable
  {
    private readonly string _loopId;
    private readonly Func<int> _itemCountFunc;
    private readonly int _fixedItemCount;
    private readonly Stopwatch _stopwatch;

    public LoopScope(string loopId, int fixedItemCount = -1, Func<int> itemCountFunc = null)
    {
      _loopId = loopId;
      _fixedItemCount = fixedItemCount;
      _itemCountFunc = itemCountFunc;
      _stopwatch = Stopwatch.StartNew();
    }

    public void Dispose()
    {
      if (!Enabled) return;
      _stopwatch.Stop();
      var items = _itemCountFunc != null ? _itemCountFunc() : (_fixedItemCount >= 0 ? _fixedItemCount : 0);
      Record(_loopId, items, _stopwatch.Elapsed.TotalMilliseconds);
    }
  }

  public static LoopScope Scope(string loopId, int itemCount = -1)
  {
    return new LoopScope(loopId, itemCount);
  }

  public static LoopScope Scope(string loopId, Func<int> itemCountFunc)
  {
    return new LoopScope(loopId, -1, itemCountFunc);
  }

  public static void Record(string loopId, int items, double elapsedMs)
  {
    if (!Enabled) return;

    var now = GetCurrentTime();
    LoopStats stats;

    lock (_lock)
    {
      if (!_stats.TryGetValue(loopId, out stats))
      {
        stats = new LoopStats { LastReportTime = now };
        _stats[loopId] = stats;
      }

      stats.CallCount++;
      stats.ItemCount += items;
      stats.TotalElapsedMs += elapsedMs;
      if (elapsedMs > stats.MaxElapsedMs)
      {
        stats.MaxElapsedMs = elapsedMs;
      }

      var timeSinceReport = now - stats.LastReportTime;
      if (timeSinceReport >= 2f || elapsedMs >= 10.0)
      {
        if (timeSinceReport < 0.001f) timeSinceReport = 2f;
        var avgMs = stats.TotalElapsedMs / Math.Max(1, stats.CallCount);
        var msg = $"[LoopPerf] {loopId}: {stats.CallCount} runs in {timeSinceReport:F1}s | items: {stats.ItemCount} | avg: {avgMs:F2}ms (max: {stats.MaxElapsedMs:F2}ms, total: {stats.TotalElapsedMs:F1}ms)";
        
        Jotunn.Logger.LogInfo(msg);

        stats.CallCount = 0;
        stats.ItemCount = 0;
        stats.TotalElapsedMs = 0;
        stats.MaxElapsedMs = 0;
        stats.LastReportTime = now;
      }
    }
  }
}
