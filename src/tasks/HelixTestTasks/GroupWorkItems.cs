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
/// Items exceeding <see cref="LargeThreshold"/> are placed into solo batches with
/// negative batch IDs (-1, -2, …). Remaining items are distributed across
/// <see cref="BatchSize"/> batches, always assigning the next item to the
/// lightest batch.
/// </summary>
public class GroupWorkItems : Task
{
    [Required]
    public ITaskItem[] Items { get; set; } = Array.Empty<ITaskItem>();

    public int BatchSize { get; set; } = 10;

    public long LargeThreshold { get; set; } = 52428800L; // 50 MB

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

        var result = new List<ITaskItem>();
        int negativeBatchId = -1;

        // Separate large items (each gets its own batch)
        var smallItems = new List<(ITaskItem item, long size)>();
        foreach (var entry in itemsWithSize)
        {
            if (entry.size > LargeThreshold)
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
