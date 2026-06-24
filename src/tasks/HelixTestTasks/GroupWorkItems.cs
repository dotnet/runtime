// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.DotNet.HelixTestTasks;

/// <summary>
/// Groups work items into balanced batches using greedy bin-packing by file size.
/// Items exceeding <see cref="LargeThreshold"/>, or whose file name (without
/// extension) is listed in <see cref="SoloItems"/>, are placed into solo batches
/// with negative batch IDs (-1, -2, …). Remaining items are distributed across
/// <see cref="BatchSize"/> batches, always assigning the next item to the
/// lightest batch.
/// </summary>
public class GroupWorkItems : Task
{
    [Required]
    public ITaskItem[] Items { get; set; } = Array.Empty<ITaskItem>();

    public int BatchSize { get; set; } = 10;

    public long LargeThreshold { get; set; } = 52428800L; // 50 MB

    /// <summary>
    /// File names (without extension) of work items that must each run in their own
    /// solo batch regardless of size. Used to isolate slow suites so a single slow
    /// suite does not inflate the runtime of a shared batch.
    /// </summary>
    public ITaskItem[] SoloItems { get; set; } = Array.Empty<ITaskItem>();

    [Output]
    public ITaskItem[] GroupedItems { get; set; } = Array.Empty<ITaskItem>();

    public override bool Execute()
    {
        if (BatchSize <= 0)
            BatchSize = 10;
        if (LargeThreshold <= 0)
            LargeThreshold = 52428800L;

        var itemsWithSize = new List<(ITaskItem item, long size)>();
        foreach (var item in Items)
        {
            long size = 0;
            if (File.Exists(item.ItemSpec))
                size = new FileInfo(item.ItemSpec).Length;

            itemsWithSize.Add((item, size));
        }

        // Sort largest first for greedy bin-packing
        itemsWithSize.Sort((a, b) => b.size.CompareTo(a.size));

        // Match on the file name only, so callers may pass either a bare suite name or a path.
        // Do not strip the extension here: suite names contain dots (e.g. "System.Text.Json.Tests")
        // and Path.GetFileNameWithoutExtension would drop the trailing ".Tests" segment, while the
        // candidate side below strips only the real archive extension (".zip").
        var soloNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var solo in SoloItems)
            soloNames.Add(Path.GetFileName(solo.ItemSpec));

        var result = new List<ITaskItem>();
        int negativeBatchId = -1;

        // Separate items that must run alone (too large, or explicitly listed as solo)
        var smallItems = new List<(ITaskItem item, long size)>();
        foreach (var entry in itemsWithSize)
        {
            if (entry.size > LargeThreshold || soloNames.Contains(Path.GetFileNameWithoutExtension(entry.item.ItemSpec)))
            {
                var newItem = new TaskItem(entry.item);
                newItem.SetMetadata("BatchId", negativeBatchId.ToString());
                negativeBatchId--;
                result.Add(newItem);
            }
            else
            {
                smallItems.Add(entry);
            }
        }

        // Greedy bin-packing for small items
        if (smallItems.Count > 0)
        {
            int numBatches = Math.Min(BatchSize, smallItems.Count);
            var batchSizes = new long[numBatches];
            var batchAssignments = new List<ITaskItem>[numBatches];
            for (int i = 0; i < numBatches; i++)
                batchAssignments[i] = new List<ITaskItem>();

            foreach (var entry in smallItems)
            {
                // Find batch with smallest total size
                int minIdx = 0;
                for (int i = 1; i < numBatches; i++)
                {
                    if (batchSizes[i] < batchSizes[minIdx])
                        minIdx = i;
                }
                batchSizes[minIdx] += entry.size;
                var newItem = new TaskItem(entry.item);
                newItem.SetMetadata("BatchId", minIdx.ToString());
                batchAssignments[minIdx].Add(newItem);
            }

            for (int i = 0; i < numBatches; i++)
                result.AddRange(batchAssignments[i]);
        }

        GroupedItems = result.ToArray();
        return !Log.HasLoggedErrors;
    }
}
