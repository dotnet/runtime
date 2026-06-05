// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.DotNet.HelixTestTasks;
using Xunit;

namespace HelixTestTasks.Tests;

public sealed class GroupWorkItemsTests : IDisposable
{
    private readonly string _testDirectory = Path.Combine(AppContext.BaseDirectory, "GroupWorkItemsTests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [Fact]
    public void PreservesGreedyPackingWhenCompatibilityMetadataIsNotConfigured()
    {
        ITaskItem[] result = Execute(
            batchSize: 2,
            largeThreshold: 1_000,
            Item("a.zip", 100),
            Item("b.zip", 50),
            Item("c.zip", 25));

        Assert.Equal("0", BatchId(result, "a.zip"));
        Assert.Equal("1", BatchId(result, "b.zip"));
        Assert.Equal("1", BatchId(result, "c.zip"));
    }

    [Fact]
    public void PartitionsSmallItemsByCompatibilityMetadataBeforePacking()
    {
        ITaskItem normal1 = Item("normal1.zip", 100, ("Scenario", "normal"));
        ITaskItem normal2 = Item("normal2.zip", 50, ("Scenario", "normal"));
        ITaskItem stress1 = Item("stress1.zip", 90, ("Scenario", "stress"));
        ITaskItem stress2 = Item("stress2.zip", 40, ("Scenario", "stress"));

        ITaskItem[] result = Execute(
            batchSize: 2,
            largeThreshold: 1_000,
            compatibilityMetadataKeys: "Scenario",
            normal1,
            normal2,
            stress1,
            stress2);

        string[] normalBatchIds = { BatchId(result, "normal1.zip"), BatchId(result, "normal2.zip") };
        string[] stressBatchIds = { BatchId(result, "stress1.zip"), BatchId(result, "stress2.zip") };

        Assert.Empty(normalBatchIds.Intersect(stressBatchIds));
    }

    [Fact]
    public void BatchCompatibleFalseItemsBecomeSoloBatches()
    {
        ITaskItem[] result = Execute(
            batchSize: 2,
            largeThreshold: 1_000,
            compatibilityMetadataKeys: "RunnerType",
            batchCompatibleMetadataKey: "BatchCompatible",
            Item("one.zip", 100, ("RunnerType", "Desktop"), ("BatchCompatible", "true")),
            Item("two.zip", 90, ("RunnerType", "Desktop"), ("BatchCompatible", "true")),
            Item("stress.zip", 80, ("RunnerType", "Desktop"), ("BatchCompatible", "false")));

        Assert.NotEqual(BatchId(result, "stress.zip"), BatchId(result, "one.zip"));
        Assert.NotEqual(BatchId(result, "stress.zip"), BatchId(result, "two.zip"));
        Assert.Single(result.Where(item => item.GetMetadata("BatchId") == BatchId(result, "stress.zip")));
    }

    [Fact]
    public void LargeItemsKeepNegativeSoloBatchIds()
    {
        ITaskItem[] result = Execute(
            batchSize: 2,
            largeThreshold: 50,
            Item("large.zip", 100),
            Item("small.zip", 10));

        Assert.Equal("-1", BatchId(result, "large.zip"));
        Assert.Equal("0", BatchId(result, "small.zip"));
    }

    [Fact]
    public void MissingCompatibilityMetadataIsSoloBatchedWithoutFailingByDefault()
    {
        ITaskItem[] result = Execute(
            batchSize: 2,
            largeThreshold: 1_000,
            compatibilityMetadataKeys: "RunnerType",
            Item("has-metadata.zip", 100, ("RunnerType", "Desktop")),
            Item("missing-metadata.zip", 90));

        Assert.NotEqual(BatchId(result, "has-metadata.zip"), BatchId(result, "missing-metadata.zip"));
    }

    private ITaskItem[] Execute(
        int batchSize,
        long largeThreshold,
        params ITaskItem[] items) =>
        Execute(batchSize, largeThreshold, compatibilityMetadataKeys: string.Empty, batchCompatibleMetadataKey: string.Empty, items);

    private static ITaskItem[] Execute(
        int batchSize,
        long largeThreshold,
        string compatibilityMetadataKeys,
        params ITaskItem[] items) =>
        Execute(batchSize, largeThreshold, compatibilityMetadataKeys, batchCompatibleMetadataKey: string.Empty, items);

    private static ITaskItem[] Execute(
        int batchSize,
        long largeThreshold,
        string compatibilityMetadataKeys,
        string batchCompatibleMetadataKey,
        params ITaskItem[] items)
    {
        var task = new GroupWorkItems
        {
            BuildEngine = new MockBuildEngine(),
            Items = items,
            BatchSize = batchSize,
            LargeThreshold = largeThreshold,
            CompatibilityMetadataKeys = compatibilityMetadataKeys,
            BatchCompatibleMetadataKey = batchCompatibleMetadataKey,
        };

        Assert.True(task.Execute());
        return task.GroupedItems;
    }

    private ITaskItem Item(string name, int size, params (string key, string value)[] metadata)
    {
        Directory.CreateDirectory(_testDirectory);
        string path = Path.Combine(_testDirectory, name);
        File.WriteAllBytes(path, new byte[size]);

        var item = new TaskItem(path);
        foreach ((string key, string value) in metadata)
        {
            item.SetMetadata(key, value);
        }

        return item;
    }

    private static string BatchId(IEnumerable<ITaskItem> items, string fileName) =>
        items.Single(item => Path.GetFileName(item.ItemSpec) == fileName).GetMetadata("BatchId");

    private sealed class MockBuildEngine : IBuildEngine
    {
        public bool ContinueOnError => false;
        public int LineNumberOfTaskNode => 0;
        public int ColumnNumberOfTaskNode => 0;
        public string ProjectFileOfTaskNode => string.Empty;

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs) => true;
        public void LogCustomEvent(CustomBuildEventArgs e) { }
        public void LogErrorEvent(BuildErrorEventArgs e) { }
        public void LogMessageEvent(BuildMessageEventArgs e) { }
        public void LogWarningEvent(BuildWarningEventArgs e) { }
    }
}
