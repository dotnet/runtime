// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.DotNet.HelixTestTasks;

/// <summary>
/// Computes per-batch Helix work item metadata (name, staging directory, timeout).
/// Timeout is 20 minutes per suite with a 30-minute minimum to handle the
/// heaviest individual suites (e.g. Cryptography ~17 min).
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

    [Output]
    public ITaskItem[] TimedItems { get; set; } = Array.Empty<ITaskItem>();

    public override bool Execute()
    {
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
            // 20 minutes per suite to account for WASM startup overhead + test execution;
            // minimum 30 minutes to handle the heaviest individual suites (e.g. Cryptography ~17m)
            // Cap at 23:59 to prevent hh format wrapping at 24 hours
            int totalMinutes = Math.Min(1439, Math.Max(30, count * 20));
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
