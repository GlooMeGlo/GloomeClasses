using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace GloomeClasses.src.Utils;

/// <summary>
/// tracks mesh generation and caching statistics to diagnose performance issues.
/// helps identify excessive mesh regeneration that could lead to GL buffer warnings.
/// </summary>
public static class MeshDiagnostics
{
    private static readonly Dictionary<string, MeshStats> componentStats = new();
    private static long totalMeshGenerations = 0;
    private static long totalCacheHits = 0;
    private static long totalCacheMisses = 0;
    private static ICoreAPI api;

    public class MeshStats
    {
        public string ComponentName { get; set; }
        public long Generations { get; set; }
        public long CacheHits { get; set; }
        public long CacheMisses { get; set; }
        public DateTime LastGeneration { get; set; }
        public long VertexCount { get; set; }

        public double HitRate => CacheHits + CacheMisses > 0
            ? (double)CacheHits / (CacheHits + CacheMisses) * 100.0
            : 0.0;
    }

    public static void Initialize(ICoreAPI coreApi)
    {
        api = coreApi;
        Log.Debug(api, "MeshDiagnostics", "Mesh diagnostics system initialized");
    }

    /// <summary>
    /// records a mesh generation event.
    /// </summary>
    /// <param name="componentName">name of the component generating the mesh (e.g., "BlockMetalBarrel")</param>
    /// <param name="vertexCount">number of vertices in the generated mesh</param>
    public static void RecordMeshGeneration(string componentName, int vertexCount = 0)
    {
        if (api == null) return;

        if (!componentStats.TryGetValue(componentName, out var stats))
        {
            stats = new MeshStats { ComponentName = componentName };
            componentStats[componentName] = stats;
        }

        stats.Generations++;
        stats.LastGeneration = DateTime.Now;
        stats.VertexCount += vertexCount;
        totalMeshGenerations++;

        Log.VerboseDebug(api, "MeshDiagnostics", "Mesh generated for {0} (vertices: {1}, total: {2})",
            componentName, vertexCount, stats.Generations);
    }

    /// <summary>
    /// records a cache hit event.
    /// </summary>
    /// <param name="componentName">name of the component</param>
    public static void RecordCacheHit(string componentName)
    {
        if (api == null) return;

        if (!componentStats.TryGetValue(componentName, out var stats))
        {
            stats = new MeshStats { ComponentName = componentName };
            componentStats[componentName] = stats;
        }

        stats.CacheHits++;
        totalCacheHits++;

        Log.VerboseDebug(api, "MeshDiagnostics", "Cache hit for {0} (total hits: {1})",
            componentName, stats.CacheHits);
    }

    /// <summary>
    /// records a cache miss event.
    /// </summary>
    /// <param name="componentName">name of the component</param>
    public static void RecordCacheMiss(string componentName)
    {
        if (api == null) return;

        if (!componentStats.TryGetValue(componentName, out var stats))
        {
            stats = new MeshStats { ComponentName = componentName };
            componentStats[componentName] = stats;
        }

        stats.CacheMisses++;
        totalCacheMisses++;

        Log.VerboseDebug(api, "MeshDiagnostics", "Cache miss for {0} (total misses: {1})",
            componentName, stats.CacheMisses);
    }

    /// <summary>
    /// generates a diagnostic report of mesh generation statistics.
    /// </summary>
    public static void GenerateReport()
    {
        if (api == null) return;

        Log.Notification(api, "MeshDiagnostics", "=== Mesh Generation Diagnostics Report ===");
        Log.Notification(api, "MeshDiagnostics", "Total mesh generations: {0}", totalMeshGenerations);
        Log.Notification(api, "MeshDiagnostics", "Total cache hits: {0}", totalCacheHits);
        Log.Notification(api, "MeshDiagnostics", "Total cache misses: {0}", totalCacheMisses);

        double overallHitRate = totalCacheHits + totalCacheMisses > 0
            ? (double)totalCacheHits / (totalCacheHits + totalCacheMisses) * 100.0
            : 0.0;

        Log.Notification(api, "MeshDiagnostics", "Overall cache hit rate: {0:F2}%", overallHitRate);

        if (componentStats.Any())
        {
            Log.Notification(api, "MeshDiagnostics", "");
            Log.Notification(api, "MeshDiagnostics", "Per-component statistics:");

            foreach (var kvp in componentStats.OrderByDescending(x => x.Value.Generations))
            {
                var stats = kvp.Value;
                Log.Notification(api, "MeshDiagnostics", "  {0}:", stats.ComponentName);
                Log.Notification(api, "MeshDiagnostics", "    Generations: {0}", stats.Generations);
                Log.Notification(api, "MeshDiagnostics", "    Cache hits: {0}", stats.CacheHits);
                Log.Notification(api, "MeshDiagnostics", "    Cache misses: {0}", stats.CacheMisses);
                Log.Notification(api, "MeshDiagnostics", "    Hit rate: {0:F2}%", stats.HitRate);
                Log.Notification(api, "MeshDiagnostics", "    Total vertices: {0}", stats.VertexCount);
                Log.Notification(api, "MeshDiagnostics", "    Last generation: {0}", stats.LastGeneration);
            }
        }

        // warn about components with poor cache performance
        var poorPerformers = componentStats.Values
            .Where(s => s.Generations > 100 && s.HitRate < 80.0)
            .ToList();

        if (poorPerformers.Any())
        {
            Log.Warning(api, "MeshDiagnostics", "");
            Log.Warning(api, "MeshDiagnostics", "Components with poor cache performance (>100 gens, <80% hit rate):");
            foreach (var stats in poorPerformers)
            {
                Log.Warning(api, "MeshDiagnostics", "  {0}: {1} generations, {2:F2}% hit rate",
                    stats.ComponentName, stats.Generations, stats.HitRate);
            }
        }

        Log.Notification(api, "MeshDiagnostics", "==========================================");
    }

    /// <summary>
    /// resets all statistics.
    /// </summary>
    public static void Reset()
    {
        componentStats.Clear();
        totalMeshGenerations = 0;
        totalCacheHits = 0;
        totalCacheMisses = 0;

        if (api != null)
        {
            Log.Debug(api, "MeshDiagnostics", "Statistics reset");
        }
    }

    /// <summary>
    /// gets statistics for a specific component.
    /// </summary>
    public static MeshStats GetStats(string componentName)
    {
        return componentStats.TryGetValue(componentName, out var stats) ? stats : null;
    }
}
