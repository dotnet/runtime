// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace Microsoft.NET.Build.Tasks.Tests;

public class InvocationCacheTests
{
    private const string EnabledEnvironmentVariable = "INVOCATION_CACHE_TEST_ENABLED";
    private const string PathEnvironmentVariable = "INVOCATION_CACHE_TEST_PATH";

    [Theory]
    [InlineData(null, false, false)]
    [InlineData("", false, false)]
    [InlineData("false", false, false)]
    [InlineData("FALSE", false, false)]
    [InlineData("true", true, false)]
    [InlineData("TrUe", true, false)]
    [InlineData("1", false, true)]
    [InlineData("invalid", false, true)]
    public void CacheEnvironmentConfiguration(string? setting, bool enabled, bool invalid)
    {
        RemoteExecutor.Invoke(static (value, expectedEnabled, expectedInvalid) =>
        {
            Environment.SetEnvironmentVariable(EnabledEnvironmentVariable, value == "<unset>" ? null : value);
            string directory = Path.Combine(Path.GetTempPath(), "invocation-cache-" + Guid.NewGuid().ToString("N"));
            Environment.SetEnvironmentVariable(PathEnvironmentVariable, directory);
            var buildEngine = new CacheBuildEngine();
            var task = new CacheTask { BuildEngine = buildEngine };
            InvocationCache? cache = InvocationCache.TryCreateFromEnvironment(task.Log, "Test",
                EnabledEnvironmentVariable, PathEnvironmentVariable, "test-tool");

            Assert.Equal(bool.Parse(expectedEnabled), cache is not null);
            if (cache is not null)
                Assert.Equal(directory, cache.CacheDirectory);
            if (bool.Parse(expectedInvalid))
            {
                BuildMessageEventArgs message = Assert.Single(buildEngine.Messages);
                Assert.Equal($"Test caching is disabled because {EnabledEnvironmentVariable} must be 'true' or 'false'.", message.Message);
                Assert.Equal(MessageImportance.Low, message.Importance);
            }
            else
            {
                Assert.Empty(buildEngine.Messages);
            }
            Assert.False(Directory.Exists(directory));
        }, setting ?? "<unset>", enabled.ToString(), invalid.ToString()).Dispose();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("cache directory")]
    public void CacheEnvironmentDirectory(string? directory)
    {
        RemoteExecutor.Invoke(static value =>
        {
            string? path = value == "<unset>" ? null : value;
            Environment.SetEnvironmentVariable(EnabledEnvironmentVariable, "true");
            Environment.SetEnvironmentVariable(PathEnvironmentVariable, path);
            var task = new CacheTask { BuildEngine = new CacheBuildEngine() };

            InvocationCache? cache = InvocationCache.TryCreateFromEnvironment(task.Log, "Test",
                EnabledEnvironmentVariable, PathEnvironmentVariable, "test-tool");

            Assert.NotNull(cache);
            Assert.Equal(InvocationCache.TryCreate(path, task.Log, "Test", "test-tool")!.CacheDirectory, cache.CacheDirectory);
        }, directory ?? "<unset>").Dispose();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CacheUsesPlatformDefaultDirectory(string? cacheDirectory)
    {
        var task = new CacheTask { BuildEngine = new CacheBuildEngine() };
        string expectedDirectory;
        if (OperatingSystem.IsWindows())
        {
            expectedDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "test-tool");
        }
        else if (OperatingSystem.IsMacOS())
        {
            expectedDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Caches", "test-tool");
        }
        else
        {
            string? cacheHome = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
            expectedDirectory = !string.IsNullOrEmpty(cacheHome) && Path.IsPathRooted(cacheHome)
                ? Path.Combine(cacheHome, "test-tool")
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache", "test-tool");
        }

        InvocationCache? cache = InvocationCache.TryCreate(cacheDirectory, task.Log, "Test", "test-tool");

        Assert.NotNull(cache);
        Assert.Equal(Path.GetFullPath(expectedDirectory), cache.CacheDirectory);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CacheDirectoryOverrideDoesNotCreateDirectory(bool absolutePath)
    {
        string directoryName = "invocation-cache-" + Guid.NewGuid().ToString("N");
        string cacheDirectory = absolutePath ? Path.Combine(Path.GetTempPath(), directoryName) : directoryName;
        var task = new CacheTask { BuildEngine = new CacheBuildEngine() };

        InvocationCache? cache = InvocationCache.TryCreate(cacheDirectory, task.Log, "Test", "test-tool");

        Assert.NotNull(cache);
        Assert.Equal(Path.GetFullPath(cacheDirectory), cache.CacheDirectory);
        Assert.False(Directory.Exists(cache.CacheDirectory));
    }

    [Fact]
    public void InvalidCacheDirectoryBypassesCaching()
    {
        var buildEngine = new CacheBuildEngine();
        var task = new CacheTask { BuildEngine = buildEngine };

        Assert.Null(InvocationCache.TryCreate("invalid\0path", task.Log, "Test", "test-tool"));
        BuildMessageEventArgs message = Assert.Single(buildEngine.Messages);
        Assert.StartsWith("Test caching is disabled because the cache directory 'invalid\0path' could not be resolved:", message.Message);
        Assert.Equal(MessageImportance.Low, message.Importance);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("relative")]
    [InlineData("absolute")]
    public void CacheDefaultDirectoryHonorsPlatformAndXdgHome(string? setting)
    {
        RemoteExecutor.Invoke(static value =>
        {
            string? cacheHome = value == "<unset>" ? null
                : value == "absolute" ? Path.Combine(Path.GetTempPath(), "invocation-cache-" + Guid.NewGuid().ToString("N"))
                : value;
            Environment.SetEnvironmentVariable("XDG_CACHE_HOME", cacheHome);
            var task = new CacheTask { BuildEngine = new CacheBuildEngine() };
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string expectedDirectory = OperatingSystem.IsWindows()
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "test-tool")
                : OperatingSystem.IsMacOS()
                    ? Path.Combine(home, "Library", "Caches", "test-tool")
                    : Path.Combine(value == "absolute" ? cacheHome! : Path.Combine(home, ".cache"), "test-tool");

            InvocationCache? cache = InvocationCache.TryCreate(null, task.Log, "Test", "test-tool");

            Assert.NotNull(cache);
            Assert.Equal(Path.GetFullPath(expectedDirectory), cache.CacheDirectory);
        }, setting ?? "<unset>").Dispose();
    }

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
        Assert.Empty(Directory.GetDirectories(test.Entries, "*.tmp"));

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
        Assert.Contains(test.BuildEngine.Messages, message => message.Message?.Contains("last-used update failed") is true);
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
        Assert.Contains(test.BuildEngine.Messages, message => message.Message?.Contains("already exists") is true);
        Assert.Empty(Directory.GetDirectories(test.Entries, "*.tmp"));
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
        Assert.Contains(test.BuildEngine.Messages, message => message.Message?.Contains("restore failed") is true);
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
            Directory.CreateDirectory(test.Entries);
            File.WriteAllText(test.Entry, "blocks publication");
        }
        else
        {
            File.WriteAllText(test.Cache.CacheDirectory, "blocks cache creation");
        }

        test.Cache.Store(CacheTestDirectory.Key, test.Source);

        Assert.Equal("assembly", File.ReadAllText(Path.Combine(test.Source, "app.dll")));
        Assert.False(Directory.Exists(test.Entry));
        Assert.Contains(test.BuildEngine.Messages, message => message.Message?.Contains("store failed") is true);
        if (failPublication)
        {
            Assert.Equal("blocks publication", File.ReadAllText(test.Entry));
            Assert.Empty(Directory.GetDirectories(test.Entries, "*.tmp"));
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
            Assert.Contains(test.BuildEngine.Messages, message => message.Message?.Contains("another writer") is true);

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
        Assert.Single(Directory.GetDirectories(test.Entries));
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
        Assert.Contains(test.BuildEngine.Messages, message => message.Message?.Contains("abandoned") is true);
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

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void SharedExecutionBypassesWithoutSkippingTaskPreparation(bool configured, bool eligible)
    {
        using var test = new CacheTestDirectory();
        Directory.CreateDirectory(test.Output);
        string stale = Path.Combine(test.Output, "stale.dll");
        File.WriteAllText(stale, "stale");
        int keyComputations = 0;
        int executions = 0;

        int exitCode = InvocationCache.Execute(configured ? test.Cache : null, CacheTestDirectory.Timestamp,
            () =>
            {
                keyComputations++;
                return eligible ? new InvocationCacheRequest(CacheTestDirectory.Key, test.Output) : null;
            },
            outputDirectory =>
            {
                Assert.Null(outputDirectory);
                executions++;
                test.CleanOutput();
                return 42;
            },
            onRestored: () => Assert.Fail("Bypass must not report a hit."));

        Assert.Equal(42, exitCode);
        Assert.Equal(configured ? 1 : 0, keyComputations);
        Assert.Equal(1, executions);
        Assert.False(File.Exists(stale));
        Assert.False(Directory.Exists(test.Cache.CacheDirectory));
    }

    [Fact]
    public void SharedExecutionStoresSuccessfulResultsAndRestoresWithoutExecuting()
    {
        using var test = new CacheTestDirectory();
        string image = Path.Combine(test.Output, "image.dll");
        var events = new List<string>();

        Assert.Equal(0, Run());
        Assert.Equal(new[] { "key", "execute" }, events);
        Assert.True(Directory.Exists(test.Entry));
        events.Clear();
        File.WriteAllText(image, "outdated");
        string stale = Path.Combine(test.Output, "stale.dll");
        File.WriteAllText(stale, "stale");

        Assert.Equal(0, Run());

        Assert.Equal(new[] { "key", "restored" }, events);
        Assert.False(File.Exists(stale));

        int Run() => InvocationCache.Execute(test.Cache, CacheTestDirectory.Timestamp,
            () =>
            {
                events.Add("key");
                return new InvocationCacheRequest(CacheTestDirectory.Key, test.Output);
            },
            outputDirectory =>
            {
                Assert.Equal(test.Output, outputDirectory);
                events.Add("execute");
                test.CleanOutput();
                File.WriteAllText(image, "computed");
                return 0;
            },
            onRestored: () =>
            {
                events.Add("restored");
                Assert.Equal("computed", File.ReadAllText(image));
            });
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(-1, false)]
    [InlineData(1, true)]
    public void SharedExecutionDoesNotStoreFailedOrErrorLoggedResults(int exitCode, bool logError)
    {
        using var test = new CacheTestDirectory();
        int executions = 0;
        int result = InvocationCache.Execute(test.Cache, CacheTestDirectory.Timestamp,
            () => new InvocationCacheRequest(CacheTestDirectory.Key, test.Output),
            outputDirectory =>
            {
                Assert.Equal(test.Output, outputDirectory);
                executions++;
                test.CleanOutput();
                File.WriteAllText(Path.Combine(test.Output, "image.dll"), "produced");
                if (logError)
                    test.Log.LogError("Expected compiler error.");
                return exitCode;
            });

        Assert.Equal(exitCode, result);
        Assert.Equal(1, executions);
        Assert.False(Directory.Exists(test.Entry));
    }

    [Theory]
    [InlineData("key")]
    [InlineData("execute")]
    [InlineData("restored")]
    public void SharedExecutionPropagatesTaskExceptions(string phase)
    {
        using var test = new CacheTestDirectory();
        if (phase == "restored")
        {
            test.WriteSource("image.dll", "cached");
            test.Cache.Store(CacheTestDirectory.Key, test.Source);
        }

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            InvocationCache.Execute(test.Cache, CacheTestDirectory.Timestamp,
                () => phase == "key" ? throw new InvalidOperationException("key") : new InvocationCacheRequest(CacheTestDirectory.Key, test.Output),
                _ => throw new InvalidOperationException("execute"),
                onRestored: () => throw new InvalidOperationException("restored")));

        Assert.Equal(phase, exception.Message);
        Assert.Equal(phase == "restored", Directory.Exists(test.Entry));
    }

    [Fact]
    public void SharedExecutionFallsBackAfterRestoreFailure()
    {
        using var test = new CacheTestDirectory();
        test.WriteSource("image.dll", "cached");
        test.Cache.Store(CacheTestDirectory.Key, test.Source);
        File.WriteAllText(Path.Combine(test.Entry, "outputs", "image.dll"), "corrupt");
        Directory.CreateDirectory(test.Output);
        string stale = Path.Combine(test.Output, "stale.dll");
        File.WriteAllText(stale, "stale");
        int executions = 0;

        Assert.Equal(0, InvocationCache.Execute(test.Cache, CacheTestDirectory.Timestamp,
            () => new InvocationCacheRequest(CacheTestDirectory.Key, test.Output),
            outputDirectory =>
            {
                Assert.Equal(test.Output, outputDirectory);
                executions++;
                test.CleanOutput();
                File.WriteAllText(Path.Combine(test.Output, "image.dll"), "recomputed");
                return 0;
            },
            onRestored: () => Assert.Fail("Failed restoration must not report a hit.")));

        Assert.Equal(1, executions);
        Assert.False(File.Exists(stale));
        Assert.Equal("recomputed", File.ReadAllText(Path.Combine(test.Output, "image.dll")));
        Assert.Contains(test.BuildEngine.Messages, message => message.Message?.Contains("restore failed") is true);
    }

    private static void RunOnOtherThread(Action action)
    {
        ExceptionDispatchInfo? exception = null;
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
        internal string Entries => Path.Combine(Cache.CacheDirectory, InvocationCacheEntry.VersionDirectory);
        internal string Entry => Path.Combine(Entries, Key);
        internal CacheBuildEngine BuildEngine { get; } = new();
        internal TaskLoggingHelper Log { get; }
        internal InvocationCache Cache { get; }
        internal string MutexName => Assert.IsType<string>(typeof(InvocationCache).GetMethod("GetWriterMutexName", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(Cache, new object[] { Key }));

        internal CacheTestDirectory()
        {
            Directory.CreateDirectory(Source);
            var task = new CacheTask { BuildEngine = BuildEngine };
            Log = task.Log;
            Cache = new InvocationCache(Path.Combine(Root, "cache"), Log, "Test");
        }

        internal void WriteSource(string relativePath, string contents)
        {
            string path = Path.Combine(Source, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, contents);
        }

        internal void CleanOutput()
        {
            if (Directory.Exists(Output))
                Directory.Delete(Output, recursive: true);
            Directory.CreateDirectory(Output);
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
