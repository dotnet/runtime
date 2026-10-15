// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace Microsoft.NET.Build.Tasks.Tests;

public class InvocationCacheTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CacheRoundTrip(bool includeSymbols)
    {
        using var test = new CacheTestDirectory();
        var files = new Dictionary<string, string>
        {
            ["app.dll"] = "assembly",
            ["app.dll.config"] = "configuration",
            [Path.Combine("fr", "app.resources.dll")] = "satellite",
            ["Link.semaphore"] = "ordinary file"
        };
        if (includeSymbols)
            files.Add("app.pdb", "symbols");

        foreach (var file in files)
            test.WriteSource(file.Key, file.Value);

        Assert.False(test.Cache.TryRestore(CacheTestDirectory.Key, test.Output, CacheTestDirectory.Timestamp));
        Assert.False(Directory.Exists(test.Cache.CacheDirectory));
        test.Cache.Store(CacheTestDirectory.Key, test.Source);

        Directory.CreateDirectory(test.Output);
        File.WriteAllText(Path.Combine(test.Output, "app.dll"), "old assembly");
        File.WriteAllText(Path.Combine(test.Output, "Link.semaphore"), "existing semaphore");
        Assert.True(test.Cache.TryRestore(CacheTestDirectory.Key, test.Output, CacheTestDirectory.Timestamp));

        foreach (var file in files)
        {
            string path = Path.Combine(test.Output, file.Key);
            Assert.Equal(file.Value, File.ReadAllText(path));
            Assert.Equal(CacheTestDirectory.Timestamp, File.GetLastWriteTimeUtc(path));
        }
        Assert.Equal("ordinary file", File.ReadAllText(Path.Combine(test.Output, "Link.semaphore")));
        Assert.Equal(files.Count, Directory.GetFiles(Path.Combine(test.Entry, "outputs"), "*", SearchOption.AllDirectories).Length);
        Assert.Empty(Directory.GetDirectories(Path.GetDirectoryName(test.Entry), "*.tmp"));

        File.WriteAllText(Path.Combine(test.Output, "app.dll"), "modified restored output");
        Assert.Equal("assembly", File.ReadAllText(Path.Combine(test.Entry, "outputs", "app.dll")));
    }

    [Fact]
    public void CacheSupportsEmptyOutput()
    {
        using var test = new CacheTestDirectory();
        test.Cache.Store(CacheTestDirectory.Key, test.Source);
        Assert.True(test.Cache.TryRestore(CacheTestDirectory.Key, test.Output, CacheTestDirectory.Timestamp));
        Assert.True(Directory.Exists(test.Output));
        Assert.Empty(Directory.GetFileSystemEntries(test.Output));
    }

    [Fact]
    public void CacheRecordsLastUsedOnStoreAndHit()
    {
        using var test = new CacheTestDirectory();
        test.WriteSource("app.dll", "assembly");
        DateTimeOffset beforeStore = DateTimeOffset.UtcNow;
        test.Cache.Store(CacheTestDirectory.Key, test.Source);
        string marker = Path.Combine(test.Entry, InvocationCacheEntry.LastUsedFileName);
        Assert.InRange(DateTimeOffset.ParseExact(File.ReadAllText(marker), "O", CultureInfo.InvariantCulture),
            beforeStore, DateTimeOffset.UtcNow);
        File.WriteAllText(marker, DateTimeOffset.MinValue.ToString("O", CultureInfo.InvariantCulture));

        DateTimeOffset beforeRestore = DateTimeOffset.UtcNow;
        Assert.True(test.Cache.TryRestore(CacheTestDirectory.Key, test.Output, CacheTestDirectory.Timestamp));

        Assert.InRange(DateTimeOffset.ParseExact(File.ReadAllText(marker), "O", CultureInfo.InvariantCulture),
            beforeRestore, DateTimeOffset.UtcNow);
        Assert.False(File.Exists(Path.Combine(test.Output, InvocationCacheEntry.LastUsedFileName)));
        Assert.Empty(Directory.GetFiles(test.Entry, "*.tmp"));
    }

    [Fact]
    public void CacheMarkerUpdateFailureDoesNotFailRestore()
    {
        using var test = new CacheTestDirectory();
        test.WriteSource("app.dll", "assembly");
        test.Cache.Store(CacheTestDirectory.Key, test.Source);
        string marker = Path.Combine(test.Entry, InvocationCacheEntry.LastUsedFileName);
        File.Delete(marker);
        Directory.CreateDirectory(marker);

        Assert.True(test.Cache.TryRestore(CacheTestDirectory.Key, test.Output, CacheTestDirectory.Timestamp));

        Assert.Equal("assembly", File.ReadAllText(Path.Combine(test.Output, "app.dll")));
        Assert.Contains(test.BuildEngine.Messages, message => message.Message.Contains("last-used update failed"));
        Assert.Empty(Directory.GetFiles(test.Entry, "*.tmp"));
        Assert.Empty(test.BuildEngine.Errors);
    }

    [Fact]
    public void FailedRestoreDoesNotRefreshUsage()
    {
        using var test = new CacheTestDirectory();
        test.WriteSource("app.dll", "assembly");
        test.Cache.Store(CacheTestDirectory.Key, test.Source);
        string marker = Path.Combine(test.Entry, InvocationCacheEntry.LastUsedFileName);
        string oldTimestamp = DateTimeOffset.MinValue.ToString("O", CultureInfo.InvariantCulture);
        File.WriteAllText(marker, oldTimestamp);
        File.WriteAllText(Path.Combine(test.Entry, "outputs", "app.dll"), "corrupt");

        Assert.False(test.Cache.TryRestore(CacheTestDirectory.Key, test.Output, CacheTestDirectory.Timestamp));

        Assert.Equal(oldTimestamp, File.ReadAllText(marker));
    }

    [Fact]
    public void ParallelProcessesRefreshUsageWithoutCorruptingMarker()
    {
        using var test = new CacheTestDirectory();
        test.WriteSource("app.dll", "assembly");
        test.Cache.Store(CacheTestDirectory.Key, test.Source);
        DateTimeOffset beforeRestore = DateTimeOffset.UtcNow;
        using (StartReader(test, 0))
        using (StartReader(test, 1))
        {
            Assert.True(SpinWait.SpinUntil(() => Directory.GetFiles(test.Root, "*.ready").Length == 2, TimeSpan.FromSeconds(30)));
            File.WriteAllText(Path.Combine(test.Root, "start"), "");
        }

        string marker = Path.Combine(test.Entry, InvocationCacheEntry.LastUsedFileName);
        Assert.InRange(DateTimeOffset.ParseExact(File.ReadAllText(marker), "O", CultureInfo.InvariantCulture),
            beforeRestore, DateTimeOffset.UtcNow);
        Assert.Empty(Directory.GetFiles(test.Entry, "*.tmp"));

        static RemoteInvokeHandle StartReader(CacheTestDirectory test, int index) =>
            RemoteExecutor.Invoke(static (cacheDirectory, output, start) =>
            {
                var engine = new CacheBuildEngine();
                var task = new CacheTask { BuildEngine = engine };
                var cache = new InvocationCache(cacheDirectory, task.Log, "Test");
                File.WriteAllText(output + ".ready", "");
                Assert.True(SpinWait.SpinUntil(() => File.Exists(start), TimeSpan.FromSeconds(30)));
                for (int attempt = 0; attempt < 20; attempt++)
                    Assert.True(cache.TryRestore(CacheTestDirectory.Key, output, CacheTestDirectory.Timestamp));
                Assert.Empty(engine.Errors);
            }, test.Cache.CacheDirectory, Path.Combine(test.Root, "reader-" + index), Path.Combine(test.Root, "start"));
    }

    [Fact]
    public void CacheDoesNotReplaceExistingEntry()
    {
        using var test = new CacheTestDirectory();
        test.WriteSource("app.dll", "first");
        test.Cache.Store(CacheTestDirectory.Key, test.Source);
        test.WriteSource("app.dll", "second");
        test.Cache.Store(CacheTestDirectory.Key, test.Source);
        Assert.True(test.Cache.TryRestore(CacheTestDirectory.Key, test.Output, CacheTestDirectory.Timestamp));
        Assert.Equal("first", File.ReadAllText(Path.Combine(test.Output, "app.dll")));
        Assert.Contains(test.BuildEngine.Messages, message => message.Message.Contains("already exists"));
        Assert.Empty(Directory.GetDirectories(Path.GetDirectoryName(test.Entry), "*.tmp"));
    }

    [Theory]
    [InlineData("missing-manifest")]
    [InlineData("version")]
    [InlineData("truncated")]
    [InlineData("missing-file")]
    [InlineData("corrupt-file")]
    [InlineData("escaping-path")]
    public void CacheRejectsInvalidEntryBeforeCopying(string damage)
    {
        using var test = new CacheTestDirectory();
        test.WriteSource("a.dll", "first");
        test.WriteSource("b.dll", "second");
        test.Cache.Store(CacheTestDirectory.Key, test.Source);
        string manifest = Path.Combine(test.Entry, "manifest");
        string cachedFile = Path.Combine(test.Entry, "outputs", "b.dll");
        switch (damage)
        {
            case "missing-manifest":
                File.Delete(manifest);
                break;
            case "version":
                using (var writer = new BinaryWriter(File.OpenWrite(manifest)))
                    writer.Write(-1);
                break;
            case "truncated":
                File.WriteAllBytes(manifest, new byte[] { 1 });
                break;
            case "missing-file":
                File.Delete(cachedFile);
                break;
            case "corrupt-file":
                File.WriteAllText(cachedFile, "broken");
                break;
            case "escaping-path":
                using (var writer = new BinaryWriter(File.Create(manifest)))
                {
                    writer.Write(1);
                    writer.Write(1);
                    writer.Write("../escaped.dll");
                }
                break;
        }

        Directory.CreateDirectory(test.Output);
        string existingOutput = Path.Combine(test.Output, "a.dll");
        File.WriteAllText(existingOutput, "unchanged");
        Assert.False(test.Cache.TryRestore(CacheTestDirectory.Key, test.Output, CacheTestDirectory.Timestamp));
        Assert.Equal("unchanged", File.ReadAllText(existingOutput));
        Assert.False(File.Exists(Path.Combine(test.Output, "b.dll")));
        Assert.False(File.Exists(Path.Combine(test.Root, "escaped.dll")));
        Assert.True(Directory.Exists(test.Entry));
        Assert.Contains(test.BuildEngine.Messages, message => message.Message.Contains("restore failed"));
    }

    [Fact]
    public void CacheRestoreReplacesExistingDirectory()
    {
        using var test = new CacheTestDirectory();
        test.WriteSource("a.dll", "new assembly");
        test.WriteSource("b.dll", "other assembly");
        test.Cache.Store(CacheTestDirectory.Key, test.Source);
        Directory.CreateDirectory(Path.Combine(test.Output, "b.dll"));
        File.WriteAllText(Path.Combine(test.Output, "a.dll"), "old assembly");

        Assert.True(test.Cache.TryRestore(CacheTestDirectory.Key, test.Output, CacheTestDirectory.Timestamp));
        Assert.Equal("new assembly", File.ReadAllText(Path.Combine(test.Output, "a.dll")));
        Assert.Equal("other assembly", File.ReadAllText(Path.Combine(test.Output, "b.dll")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CacheStoreFailureIsBestEffort(bool failPublication)
    {
        using var test = new CacheTestDirectory();
        test.WriteSource("app.dll", "assembly");
        if (failPublication)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(test.Entry));
            File.WriteAllText(test.Entry, "blocks publication");
        }
        else
        {
            File.WriteAllText(test.Cache.CacheDirectory, "blocks cache creation");
        }

        test.Cache.Store(CacheTestDirectory.Key, test.Source);

        Assert.Equal("assembly", File.ReadAllText(Path.Combine(test.Source, "app.dll")));
        Assert.False(Directory.Exists(test.Entry));
        Assert.Contains(test.BuildEngine.Messages, message => message.Message.Contains("store failed"));
        if (failPublication)
        {
            Assert.Equal("blocks publication", File.ReadAllText(test.Entry));
            Assert.Empty(Directory.GetDirectories(Path.GetDirectoryName(test.Entry), "*.tmp"));
        }
    }

    [Fact]
    public void CacheSkipsContendedEntryButNotOtherKeys()
    {
        using var test = new CacheTestDirectory();
        test.WriteSource("app.dll", "assembly");
        using var mutex = new Mutex(false, test.MutexName);
        Assert.True(mutex.WaitOne(0));
        try
        {
            RunOnOtherThread(() => test.Cache.Store(CacheTestDirectory.Key, test.Source));
            Assert.False(Directory.Exists(test.Cache.CacheDirectory));
            Assert.Contains(test.BuildEngine.Messages, message => message.Message.Contains("another writer"));

            string otherKey = new string('b', 64);
            RunOnOtherThread(() => test.Cache.Store(otherKey, test.Source));
            Assert.True(test.Cache.TryRestore(otherKey, test.Output, CacheTestDirectory.Timestamp));
        }
        finally
        {
            mutex.ReleaseMutex();
        }

        test.Cache.Store(CacheTestDirectory.Key, test.Source);
        Assert.True(Directory.Exists(test.Entry));
    }

    [Fact]
    public void CacheReadsPublishedEntryWithoutWriterLock()
    {
        using var test = new CacheTestDirectory();
        test.WriteSource("app.dll", "assembly");
        test.Cache.Store(CacheTestDirectory.Key, test.Source);
        using var mutex = new Mutex(false, test.MutexName);
        Assert.True(mutex.WaitOne(0));
        try
        {
            RunOnOtherThread(() =>
                Assert.True(test.Cache.TryRestore(CacheTestDirectory.Key, test.Output, CacheTestDirectory.Timestamp)));
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    [Fact]
    public void CacheSupportsConcurrentWritersWithDifferentOutputs()
    {
        using var test = new CacheTestDirectory();
        string contents = new string('a', 1024 * 1024);
        System.Threading.Tasks.Parallel.For(0, 8, index =>
        {
            string source = Path.Combine(test.Root, "source-" + index);
            Directory.CreateDirectory(source);
            File.WriteAllText(Path.Combine(source, "app.dll"), contents);
            File.WriteAllText(Path.Combine(source, "app.pdb"), "symbols");
            var task = new CacheTask { BuildEngine = new CacheBuildEngine() };
            var cache = new InvocationCache(test.Cache.CacheDirectory, task.Log, "Test");

            cache.Store(CacheTestDirectory.Key, source);
        });

        Assert.True(test.Cache.TryRestore(CacheTestDirectory.Key, test.Output, CacheTestDirectory.Timestamp));
        Assert.Equal(contents, File.ReadAllText(Path.Combine(test.Output, "app.dll")));
        Assert.Equal("symbols", File.ReadAllText(Path.Combine(test.Output, "app.pdb")));
        Assert.Single(Directory.GetDirectories(Path.GetDirectoryName(test.Entry)));
    }

    [Fact]
    public void CacheIgnoresOtherWritersStagingDirectories()
    {
        using var test = new CacheTestDirectory();
        string stagingDirectory = test.Entry + ".orphan.tmp";
        Directory.CreateDirectory(stagingDirectory);
        string marker = Path.Combine(stagingDirectory, "partial");
        File.WriteAllText(marker, "other attempt");

        Assert.False(test.Cache.TryRestore(CacheTestDirectory.Key, test.Output, CacheTestDirectory.Timestamp));
        test.Cache.Store(CacheTestDirectory.Key, test.Source);

        Assert.Equal("other attempt", File.ReadAllText(marker));
        Assert.True(test.Cache.TryRestore(CacheTestDirectory.Key, test.Output, CacheTestDirectory.Timestamp));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CacheRejectsOverlappingDirectories(bool cacheContainsOutput)
    {
        using var test = new CacheTestDirectory();
        string output = cacheContainsOutput ? Path.Combine(test.Cache.CacheDirectory, "output") : test.Root;

        Assert.Throws<ArgumentException>(() => test.Cache.Store(CacheTestDirectory.Key, output));
        Assert.Throws<ArgumentException>(() => test.Cache.TryRestore(CacheTestDirectory.Key, output, CacheTestDirectory.Timestamp));
        Assert.False(Directory.Exists(test.Cache.CacheDirectory));
    }

    [Fact]
    public void CacheRecoversAbandonedWriterLock()
    {
        using var test = new CacheTestDirectory();
        test.WriteSource("app.dll", "assembly");
        using var mutex = new Mutex(false, test.MutexName);
        var owner = new Thread(() => mutex.WaitOne());
        owner.Start();
        owner.Join();

        test.Cache.Store(CacheTestDirectory.Key, test.Source);

        Assert.True(test.Cache.TryRestore(CacheTestDirectory.Key, test.Output, CacheTestDirectory.Timestamp));
        Assert.Contains(test.BuildEngine.Messages, message => message.Message.Contains("abandoned"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("../entry")]
    [InlineData("not-a-hash")]
    public void CacheRejectsInvalidInputHash(string inputHash)
    {
        using var test = new CacheTestDirectory();
        Assert.Throws<ArgumentException>(() => test.Cache.Store(inputHash, test.Source));
        Assert.Throws<ArgumentException>(() => test.Cache.TryRestore(inputHash, test.Output, CacheTestDirectory.Timestamp));
        Assert.False(Directory.Exists(test.Cache.CacheDirectory));
    }

    private static void RunOnOtherThread(Action action)
    {
        ExceptionDispatchInfo exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ExceptionDispatchInfo.Capture(ex);
            }
        });
        thread.Start();
        thread.Join();
        exception?.Throw();
    }

    private sealed class CacheTestDirectory : IDisposable
    {
        internal static string Key { get; } = new string('a', 64);
        internal static DateTime Timestamp { get; } = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        internal string Root { get; } = Path.Combine(Path.GetTempPath(), "invocation-cache-tests-" + Guid.NewGuid().ToString("N"));
        internal string Source => Path.Combine(Root, "source");
        internal string Output => Path.Combine(Root, "output");
        internal string Entry => Path.Combine(Cache.CacheDirectory, InvocationCacheEntry.VersionDirectory, Key);
        internal CacheBuildEngine BuildEngine { get; } = new();
        internal InvocationCache Cache { get; }
        internal string MutexName => (string)typeof(InvocationCache).GetMethod("GetWriterMutexName", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(Cache, new object[] { Key });

        internal CacheTestDirectory()
        {
            Directory.CreateDirectory(Source);
            var task = new CacheTask { BuildEngine = BuildEngine };
            Cache = new InvocationCache(Path.Combine(Root, "cache"), task.Log, "Test");
        }

        internal void WriteSource(string relativePath, string contents)
        {
            string path = Path.Combine(Source, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, contents);
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }

    private sealed class CacheTask : Microsoft.Build.Utilities.Task
    {
        public override bool Execute() => true;
    }

    private sealed class CacheBuildEngine : IBuildEngine
    {
        internal List<BuildErrorEventArgs> Errors { get; } = new();
        internal List<BuildMessageEventArgs> Messages { get; } = new();

        public void LogErrorEvent(BuildErrorEventArgs e) => Errors.Add(e);
        public void LogWarningEvent(BuildWarningEventArgs e) { }
        public void LogMessageEvent(BuildMessageEventArgs e) => Messages.Add(e);
        public void LogCustomEvent(CustomBuildEventArgs e) { }
        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs) => false;
        public bool ContinueOnError => false;
        public int LineNumberOfTaskNode => 0;
        public int ColumnNumberOfTaskNode => 0;
        public string ProjectFileOfTaskNode => string.Empty;
    }
}
