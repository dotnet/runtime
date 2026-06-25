// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.DotNet.HelixTestTasks;

/// <summary>
/// Computes per-batch Helix work item metadata (name, staging directory, timeout).
/// Timeout is <see cref="MinutesPerSuite"/> minutes per suite (default 20) with a
/// <see cref="MinimumMinutes"/> floor (default 30) to handle the heaviest individual
/// suites (e.g. Cryptography ~17 min), capped at <see cref="MaximumMinutes"/>.
/// Slower test scopes (e.g. outerloop) can raise these knobs.
/// </summary>
public class ComputeBatchTimeout : Task
{
    [Required]
    public ITaskItem[] GroupedItems { get; set; } = Array.Empty<ITaskItem>();

    [Required]
    public ITaskItem[] BatchIds { get; set; } = Array.Empty<ITaskItem>();

    [Required]
    public string ItemPrefix { get; set; } = string.Empty;

    [Required]
    public string BatchOutputDir { get; set; } = string.Empty;

    /// <summary>Minutes budgeted per test suite in a batch. Default 20.</summary>
    public int MinutesPerSuite { get; set; } = 20;

    /// <summary>Minimum per-batch timeout in minutes. Default 30.</summary>
    public int MinimumMinutes { get; set; } = 30;

    /// <summary>
    /// Maximum per-batch timeout in minutes. Default 1439 (capped below 24h to
    /// prevent hh format wrapping). Keep this below the AzDO job timeout (minus
    /// build/send overhead) so the synchronous send-to-Helix wait cannot outlast
    /// the job.
    /// </summary>
    public int MaximumMinutes { get; set; } = 1439;

    [Output]
    public ITaskItem[] TimedItems { get; set; } = Array.Empty<ITaskItem>();

    public override bool Execute()
    {
        // Sanitize MSBuild-provided knobs: fall back to defaults on non-positive values, and cap
        // MaximumMinutes below 24h so the "hh" specifier in the formatted Timeout cannot wrap
        // (e.g. 1440 minutes would render as 00:00:00). Also ensure the floor never exceeds the cap.
        if (MinutesPerSuite <= 0)
            MinutesPerSuite = 20;
        if (MinimumMinutes <= 0)
            MinimumMinutes = 30;
        if (MaximumMinutes <= 0 || MaximumMinutes > 1439)
            MaximumMinutes = 1439;
        if (MinimumMinutes > MaximumMinutes)
            MinimumMinutes = MaximumMinutes;

        var counts = new Dictionary<string, int>();
        foreach (var item in GroupedItems)
        {
            string bid = item.GetMetadata("BatchId");
            counts.TryGetValue(bid, out int current);
            counts[bid] = current + 1;
        }

        var result = new List<ITaskItem>();
        foreach (var batchId in BatchIds)
        {
            string bid = batchId.ItemSpec;
            int count = counts.GetValueOrDefault(bid, 1);
            // MinutesPerSuite minutes per suite to account for WASM startup overhead + test
            // execution; MinimumMinutes floor handles the heaviest individual suites
            // (e.g. Cryptography ~17m); capped at MaximumMinutes (kept below 24h to prevent
            // hh format wrapping, and intended to stay under the AzDO job timeout).
            // Multiply in long so an outsized MinutesPerSuite override cannot overflow int; the
            // result is bounded by MaximumMinutes (< 24h), so casting back to int is safe.
            long perSuiteMinutes = (long)count * MinutesPerSuite;
            int totalMinutes = (int)Math.Min(MaximumMinutes, Math.Max(MinimumMinutes, perSuiteMinutes));
            var ts = TimeSpan.FromMinutes(totalMinutes);

            var helixItem = new TaskItem(ItemPrefix + "Batch-" + bid);
            helixItem.SetMetadata("BatchDir", BatchOutputDir + "batch-" + bid + "/");
            helixItem.SetMetadata("Timeout", ts.ToString(@"hh\:mm\:ss"));
            result.Add(helixItem);
        }

        TimedItems = result.ToArray();
        return !Log.HasLoggedErrors;
    }
}
