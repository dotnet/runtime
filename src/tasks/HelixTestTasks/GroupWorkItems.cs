// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private static readonly char[] s_compatibilityMetadataKeySeparators = { ';' };

    [Required]
    public ITaskItem[] Items { get; set; } = Array.Empty<ITaskItem>();

    public int BatchSize { get; set; } = 10;

    public long LargeThreshold { get; set; } = 52428800L; // 50 MB

    public string CompatibilityMetadataKeys { get; set; } = string.Empty;

    public string BatchCompatibleMetadataKey { get; set; } = string.Empty;

    public bool StrictCompatibilityMetadata { get; set; }

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
        int nextBatchId = 0;
        string[] compatibilityKeys = CompatibilityMetadataKeys
            .Split(s_compatibilityMetadataKeySeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string batchCompatibleMetadataKey = BatchCompatibleMetadataKey.Trim();

        // Separate large items (each gets its own batch)
        var partitionedSmallItems = new Dictionary<string, List<(ITaskItem item, long size)>>(StringComparer.Ordinal);
        var soloSmallItems = new List<(ITaskItem item, long size)>();
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
                if (IsSolo(entry.item, batchCompatibleMetadataKey, compatibilityKeys, out string partitionKey))
                {
                    soloSmallItems.Add(entry);
                }
                else
                {
                    if (!partitionedSmallItems.TryGetValue(partitionKey, out List<(ITaskItem item, long size)>? partitionItems))
                    {
                        partitionItems = new List<(ITaskItem item, long size)>();
                        partitionedSmallItems.Add(partitionKey, partitionItems);
                    }

                    partitionItems.Add(entry);
                }
            }
        }

        // Greedy bin-packing for small items
        foreach (List<(ITaskItem item, long size)> smallItems in partitionedSmallItems.Values)
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
                newItem.SetMetadata("BatchId", (nextBatchId + minIdx).ToString());
                batchAssignments[minIdx].Add(newItem);
            }

            for (int i = 0; i < numBatches; i++)
                result.AddRange(batchAssignments[i]);

            nextBatchId += numBatches;
        }

        foreach (var entry in soloSmallItems)
        {
            var newItem = new TaskItem(entry.item);
            newItem.SetMetadata("BatchId", nextBatchId.ToString());
            nextBatchId++;
            result.Add(newItem);
        }

        GroupedItems = result.ToArray();
        return !Log.HasLoggedErrors;
    }

    private bool IsSolo(ITaskItem item, string batchCompatibleMetadataKey, string[] compatibilityKeys, out string partitionKey)
    {
        partitionKey = string.Empty;

        if (!string.IsNullOrEmpty(batchCompatibleMetadataKey))
        {
            string batchCompatible = item.GetMetadata(batchCompatibleMetadataKey);
            if (!string.IsNullOrEmpty(batchCompatible) &&
                !string.Equals(batchCompatible, "true", StringComparison.OrdinalIgnoreCase))
            {
                Log.LogMessage(
                    MessageImportance.Low,
                    "Keeping '{0}' in a solo batch because metadata '{1}' is '{2}'.",
                    item.ItemSpec,
                    batchCompatibleMetadataKey,
                    batchCompatible);
                return true;
            }
        }

        if (compatibilityKeys.Length == 0)
        {
            return false;
        }

        string[] values = new string[compatibilityKeys.Length];
        for (int i = 0; i < compatibilityKeys.Length; i++)
        {
            string key = compatibilityKeys[i];
            string value = item.GetMetadata(key);
            if (string.IsNullOrEmpty(value))
            {
                string message = $"Keeping '{item.ItemSpec}' in a solo batch because required compatibility metadata '{key}' is missing.";
                if (StrictCompatibilityMetadata)
                {
                    Log.LogError(message);
                }
                else
                {
                    Log.LogMessage(MessageImportance.High, message);
                }

                return true;
            }

            values[i] = value;
        }

        partitionKey = string.Join('\u001f', compatibilityKeys.Zip(values, static (key, value) => key + "=" + value));
        return false;
    }
}
