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
using System.Security.Cryptography;
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
    private static readonly InvocationCacheConfiguration s_cacheConfiguration =
        new("Test", EnabledEnvironmentVariable, PathEnvironmentVariable);

    [Fact]
    public void CacheConfigurationDoesNotCaptureEnvironmentValues() => RemoteExecutor.Invoke(static () =>
    {
        using var test = new CacheTestDirectory();
        var configuration = new InvocationCacheConfiguration("Test", EnabledEnvironmentVariable, PathEnvironmentVariable);
        Environment.SetEnvironmentVariable(EnabledEnvironmentVariable, "false");
        Environment.SetEnvironmentVariable(PathEnvironmentVariable, Path.Combine(test.Root, "first"));
        Assert.Null(InvocationCache.TryCreateFromEnvironment(test.Log, configuration));

        Environment.SetEnvironmentVariable(EnabledEnvironmentVariable, "true");
        Assert.Equal(Path.Combine(test.Root, "first"),
            InvocationCache.TryCreateFromEnvironment(test.Log, configuration)!.CacheDirectory);

        Environment.SetEnvironmentVariable(PathEnvironmentVariable, Path.Combine(test.Root, "second"));
        Assert.Equal(Path.Combine(test.Root, "second"),
            InvocationCache.TryCreateFromEnvironment(test.Log, configuration)!.CacheDirectory);
        Assert.False(Directory.Exists(Path.Combine(test.Root, "first")));
        Assert.False(Directory.Exists(Path.Combine(test.Root, "second")));
    }).Dispose();

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
            InvocationCache? cache = InvocationCache.TryCreateFromEnvironment(task.Log, s_cacheConfiguration);

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

            InvocationCache? cache = InvocationCache.TryCreateFromEnvironment(task.Log, s_cacheConfiguration);

            Assert.NotNull(cache);
            Assert.Equal(InvocationCache.TryCreate(path, task.Log, "Test")!.CacheDirectory, cache.CacheDirectory);
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
            expectedDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "test");
        }
        else if (OperatingSystem.IsMacOS())
        {
            expectedDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Caches", "test");
        }
        else
        {
            string? cacheHome = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
            expectedDirectory = !string.IsNullOrEmpty(cacheHome) && Path.IsPathRooted(cacheHome)
                ? Path.Combine(cacheHome, "test")
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache", "test");
        }

        InvocationCache? cache = InvocationCache.TryCreate(cacheDirectory, task.Log, "Test");

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

        InvocationCache? cache = InvocationCache.TryCreate(cacheDirectory, task.Log, "Test");

        Assert.NotNull(cache);
        Assert.Equal(Path.GetFullPath(cacheDirectory), cache.CacheDirectory);
        Assert.False(Directory.Exists(cache.CacheDirectory));
    }

    [Fact]
    public void InvalidCacheDirectoryBypassesCaching()
    {
        var buildEngine = new CacheBuildEngine();
        var task = new CacheTask { BuildEngine = buildEngine };

        Assert.Null(InvocationCache.TryCreate("invalid\0path", task.Log, "Test"));
        BuildMessageEventArgs message = Assert.Single(buildEngine.Messages);
        Assert.StartsWith("Test caching is disabled because the cache directory 'invalid\0path' could not be resolved:", message.Message);
        Assert.Equal(MessageImportance.Low, message.Importance);
    }

    [Fact]
    public void CacheDirectoryFileBypassesCaching()
    {
        using var test = new CacheTestDirectory();
        File.WriteAllText(test.Cache.CacheDirectory, "not a directory");

        Assert.Null(InvocationCache.TryCreate(test.Cache.CacheDirectory, test.Log, "Test"));

        BuildMessageEventArgs message = Assert.Single(test.BuildEngine.Messages);
        Assert.Contains($"Cache location is a file: '{test.Cache.CacheDirectory}'.", message.Message);
        Assert.Equal("not a directory", File.ReadAllText(test.Cache.CacheDirectory));
    }

    [Theory]
    [InlineData("ILLink", "illink")]
    [InlineData("Crossgen2", "crossgen2")]
    public void CacheDirectoryNameUsesInvariantToolName(string toolName, string directoryName)
        => RemoteExecutor.Invoke(static (name, expectedDirectory) =>
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            var task = new CacheTask { BuildEngine = new CacheBuildEngine() };

            InvocationCache? cache = InvocationCache.TryCreate(null, task.Log, name);

            Assert.NotNull(cache);
            Assert.Equal(expectedDirectory, Path.GetFileName(cache.CacheDirectory));
        }, toolName, directoryName).Dispose();

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
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "test")
                : OperatingSystem.IsMacOS()
                    ? Path.Combine(home, "Library", "Caches", "test")
                    : Path.Combine(value == "absolute" ? cacheHome! : Path.Combine(home, ".cache"), "test");

            InvocationCache? cache = InvocationCache.TryCreate(null, task.Log, "Test");

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

        Assert.False(test.Cache.TryRestore(CacheTestDirectory.Key, test.OutputOutputs, CacheTestDirectory.Timestamp));
        Assert.False(Directory.Exists(test.Cache.CacheDirectory));
        test.Cache.Store(CacheTestDirectory.Key, test.SourceOutputs);

        Directory.CreateDirectory(test.Output);
        File.WriteAllText(Path.Combine(test.Output, "app.dll"), "old assembly");
        File.WriteAllText(Path.Combine(test.Output, "Link.semaphore"), "existing semaphore");
        Assert.True(test.Cache.TryRestore(CacheTestDirectory.Key, test.OutputOutputs, CacheTestDirectory.Timestamp));

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
        test.Cache.Store(CacheTestDirectory.Key, test.SourceOutputs);
        Assert.True(test.Cache.TryRestore(CacheTestDirectory.Key, test.OutputOutputs, CacheTestDirectory.Timestamp));
        Assert.True(Directory.Exists(test.Output));
        Assert.Empty(Directory.GetFileSystemEntries(test.Output));
    }

    [Fact]
    public void CacheRecordsLastUsedOnStoreAndHit()
    {
        using var test = new CacheTestDirectory();
        test.WriteSource("app.dll", "assembly");
        DateTimeOffset beforeStore = DateTimeOffset.UtcNow;
        test.Cache.Store(CacheTestDirectory.Key, test.SourceOutputs);
        string marker = Path.Combine(test.Entry, InvocationCacheEntry.LastUsedFileName);
        Assert.InRange(DateTimeOffset.ParseExact(File.ReadAllText(marker), "O", CultureInfo.InvariantCulture),
            beforeStore, DateTimeOffset.UtcNow);
        File.WriteAllText(marker, DateTimeOffset.MinValue.ToString("O", CultureInfo.InvariantCulture));

        DateTimeOffset beforeRestore = DateTimeOffset.UtcNow;
        Assert.True(test.Cache.TryRestore(CacheTestDirectory.Key, test.OutputOutputs, CacheTestDirectory.Timestamp));

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
        test.Cache.Store(CacheTestDirectory.Key, test.SourceOutputs);
        string marker = Path.Combine(test.Entry, InvocationCacheEntry.LastUsedFileName);
        File.Delete(marker);
        Directory.CreateDirectory(marker);

        Assert.True(test.Cache.TryRestore(CacheTestDirectory.Key, test.OutputOutputs, CacheTestDirectory.Timestamp));

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
        test.Cache.Store(CacheTestDirectory.Key, test.SourceOutputs);
        string marker = Path.Combine(test.Entry, InvocationCacheEntry.LastUsedFileName);
        string oldTimestamp = DateTimeOffset.MinValue.ToString("O", CultureInfo.InvariantCulture);
        File.WriteAllText(marker, oldTimestamp);
        File.WriteAllText(Path.Combine(test.Entry, "outputs", "app.dll"), "corrupt");

        Assert.False(test.Cache.TryRestore(CacheTestDirectory.Key, test.OutputOutputs, CacheTestDirectory.Timestamp));

        Assert.Equal(oldTimestamp, File.ReadAllText(marker));
    }

    [Fact]
    public void ParallelProcessesRefreshUsageWithoutCorruptingMarker()
    {
        using var test = new CacheTestDirectory();
        test.WriteSource("app.dll", "assembly");
        test.Cache.Store(CacheTestDirectory.Key, test.SourceOutputs);
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
                    Assert.True(cache.TryRestore(CacheTestDirectory.Key, InvocationOutputs.FromDirectory(output), CacheTestDirectory.Timestamp));
                Assert.Empty(engine.Errors);
            }, test.Cache.CacheDirectory, Path.Combine(test.Root, "reader-" + index), Path.Combine(test.Root, "start"));
    }

    [Fact]
    public void CacheDoesNotReplaceExistingEntry()
    {
        using var test = new CacheTestDirectory();
        test.WriteSource("app.dll", "first");
        test.Cache.Store(CacheTestDirectory.Key, test.SourceOutputs);
        test.WriteSource("app.dll", "second");
        test.Cache.Store(CacheTestDirectory.Key, test.SourceOutputs);
        Assert.True(test.Cache.TryRestore(CacheTestDirectory.Key, test.OutputOutputs, CacheTestDirectory.Timestamp));
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
        test.Cache.Store(CacheTestDirectory.Key, test.SourceOutputs);
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
                    writer.Write(InvocationCacheEntry.FormatVersion);
                    writer.Write(false);
                    writer.Write(1);
                    writer.Write("../escaped.dll");
                }
                break;
        }
        if (File.Exists(manifest))
            File.WriteAllBytes(Path.Combine(test.Entry, "manifest.sha256"), SHA256.HashData(File.ReadAllBytes(manifest)));

        Directory.CreateDirectory(test.Output);
        string existingOutput = Path.Combine(test.Output, "a.dll");
        File.WriteAllText(existingOutput, "unchanged");
        Assert.False(test.Cache.TryRestore(CacheTestDirectory.Key, test.OutputOutputs, CacheTestDirectory.Timestamp));
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
        test.Cache.Store(CacheTestDirectory.Key, test.SourceOutputs);
        Directory.CreateDirectory(Path.Combine(test.Output, "b.dll"));
        File.WriteAllText(Path.Combine(test.Output, "a.dll"), "old assembly");

        Assert.True(test.Cache.TryRestore(CacheTestDirectory.Key, test.OutputOutputs, CacheTestDirectory.Timestamp));
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

        test.Cache.Store(CacheTestDirectory.Key, test.SourceOutputs);

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
            RunOnOtherThread(() => test.Cache.Store(CacheTestDirectory.Key, test.SourceOutputs));
            Assert.False(Directory.Exists(test.Cache.CacheDirectory));
            Assert.Contains(test.BuildEngine.Messages, message => message.Message?.Contains("another writer") is true);

            string otherKey = new string('b', 64);
            RunOnOtherThread(() => test.Cache.Store(otherKey, test.SourceOutputs));
            Assert.True(test.Cache.TryRestore(otherKey, test.OutputOutputs, CacheTestDirectory.Timestamp));
        }
        finally
        {
            mutex.ReleaseMutex();
        }

        test.Cache.Store(CacheTestDirectory.Key, test.SourceOutputs);
        Assert.True(Directory.Exists(test.Entry));
    }

    [Fact]
    public void CacheReadsPublishedEntryWithoutWriterLock()
    {
        using var test = new CacheTestDirectory();
        test.WriteSource("app.dll", "assembly");
        test.Cache.Store(CacheTestDirectory.Key, test.SourceOutputs);
        using var mutex = new Mutex(false, test.MutexName);
        Assert.True(mutex.WaitOne(0));
        try
        {
            RunOnOtherThread(() =>
                Assert.True(test.Cache.TryRestore(CacheTestDirectory.Key, test.OutputOutputs, CacheTestDirectory.Timestamp)));
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

            cache.Store(CacheTestDirectory.Key, InvocationOutputs.FromDirectory(source));
        });

        Assert.True(test.Cache.TryRestore(CacheTestDirectory.Key, test.OutputOutputs, CacheTestDirectory.Timestamp));
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

        Assert.False(test.Cache.TryRestore(CacheTestDirectory.Key, test.OutputOutputs, CacheTestDirectory.Timestamp));
        test.Cache.Store(CacheTestDirectory.Key, test.SourceOutputs);

        Assert.Equal("other attempt", File.ReadAllText(marker));
        Assert.True(test.Cache.TryRestore(CacheTestDirectory.Key, test.OutputOutputs, CacheTestDirectory.Timestamp));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CacheRejectsOverlappingDirectories(bool cacheContainsOutput)
    {
        using var test = new CacheTestDirectory();
        string output = cacheContainsOutput ? Path.Combine(test.Cache.CacheDirectory, "output") : test.Root;

        Assert.Throws<ArgumentException>(() => test.Cache.Store(CacheTestDirectory.Key, InvocationOutputs.FromDirectory(output)));
        Assert.Throws<ArgumentException>(() => test.Cache.TryRestore(CacheTestDirectory.Key, InvocationOutputs.FromDirectory(output), CacheTestDirectory.Timestamp));
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

        test.Cache.Store(CacheTestDirectory.Key, test.SourceOutputs);

        Assert.True(test.Cache.TryRestore(CacheTestDirectory.Key, test.OutputOutputs, CacheTestDirectory.Timestamp));
        Assert.Contains(test.BuildEngine.Messages, message => message.Message?.Contains("abandoned") is true);
    }

    [Theory]
    [InlineData("")]
    [InlineData("../entry")]
    [InlineData("not-a-hash")]
    public void CacheRejectsInvalidInputHash(string inputHash)
    {
        using var test = new CacheTestDirectory();
        Assert.Throws<ArgumentException>(() => test.Cache.Store(inputHash, test.SourceOutputs));
        Assert.Throws<ArgumentException>(() => test.Cache.TryRestore(inputHash, test.OutputOutputs, CacheTestDirectory.Timestamp));
        Assert.False(Directory.Exists(test.Cache.CacheDirectory));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void SharedTaskAdapterGatesDescriptionAndUsesCapturedArguments(bool configured, bool eligible)
        => RemoteExecutor.Invoke(static (enabled, supported) =>
    {
        bool isConfigured = bool.Parse(enabled);
        bool isEligible = bool.Parse(supported);
        using var test = new CacheTestDirectory();
        Environment.SetEnvironmentVariable(EnabledEnvironmentVariable, enabled);
        Environment.SetEnvironmentVariable(PathEnvironmentVariable, test.Cache.CacheDirectory);
        test.WriteSource("image.dll", "cached");
        var invocation = new InvocationDescription(InvocationInputs.FromFiles(),
            new[] { "compiler path", "response contents", "command contents" }, test.OutputOutputs);
        test.Cache.Store(InvocationCache.ComputeKey(invocation), test.SourceOutputs);
        var events = new List<string>();
        DateTime preparedAt = default;

        int result = InvocationCache.ExecuteCached(test.Log, s_cacheConfiguration,
            describeInvocation: () => DescribeInvocation("compiler path", "response contents", "command contents"),
            executeNormally: () => ExecuteNormally("compiler path", "response contents", "command contents"));
        DateTime completedAt = DateTime.UtcNow;

        Assert.Equal(isConfigured && isEligible ? 0 : 42, result);
        Assert.Equal(isConfigured
            ? isEligible ? new[] { "describe" } : new[] { "describe", "execute" }
            : new[] { "execute" }, events);
        if (isConfigured && isEligible)
        {
            Assert.InRange(File.GetLastWriteTimeUtc(Path.Combine(test.Output, "image.dll")), preparedAt, completedAt);
            Assert.Equal("cached", File.ReadAllText(Path.Combine(test.Output, "image.dll")));
        }

        InvocationDescription? DescribeInvocation(string path, string response, string command)
        {
            AssertToolArguments(path, response, command);
            events.Add("describe");
            preparedAt = DateTime.UtcNow;
            return isEligible ? invocation : null;
        }

        int ExecuteNormally(string path, string response, string command)
        {
            AssertToolArguments(path, response, command);
            events.Add("execute");
            return 42;
        }

        static void AssertToolArguments(string path, string response, string command)
        {
            Assert.Equal("compiler path", path);
            Assert.Equal("response contents", response);
            Assert.Equal("command contents", command);
        }
    }, configured.ToString(), eligible.ToString()).Dispose();

    [Fact]
    public void SharedTaskAdapterPropagatesPreparationExceptions() => RemoteExecutor.Invoke(static () =>
    {
        using var test = new CacheTestDirectory();
        Environment.SetEnvironmentVariable(EnabledEnvironmentVariable, "true");
        Environment.SetEnvironmentVariable(PathEnvironmentVariable, test.Cache.CacheDirectory);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            InvocationCache.ExecuteCached(test.Log, s_cacheConfiguration,
                describeInvocation: () => throw new InvalidOperationException("description"),
                executeNormally: () => throw new InvalidOperationException("unexpected execution")));

        Assert.Equal("description", exception.Message);
        Assert.False(Directory.Exists(test.Entry));
    }).Dispose();

    [Theory]
    [InlineData("equal-directory")]
    [InlineData("cache-in-directory")]
    [InlineData("directory-in-cache")]
    [InlineData("duplicate-files")]
    [InlineData("nested-files")]
    [InlineData("file-in-cache")]
    [InlineData("cache-in-file")]
    public void SharedTaskAdapterRejectsInvalidOutputsWithoutExecuting(string shape) => RemoteExecutor.Invoke(static value =>
    {
        using var test = new CacheTestDirectory();
        Environment.SetEnvironmentVariable(EnabledEnvironmentVariable, "true");
        Environment.SetEnvironmentVariable(PathEnvironmentVariable, test.Cache.CacheDirectory);
        test.WriteSource("image.dll", "cached");
        test.Cache.Store(CacheTestDirectory.Key, test.SourceOutputs);
        Directory.CreateDirectory(test.Output);
        string stale = Path.Combine(test.Output, "stale.dll");
        File.WriteAllText(stale, "untouched");
        string image = Path.Combine(test.Output, "image.dll");
        InvocationOutputs outputs = value switch
        {
            "equal-directory" => InvocationOutputs.FromDirectory(test.Cache.CacheDirectory),
            "cache-in-directory" => InvocationOutputs.FromDirectory(test.Root),
            "directory-in-cache" => InvocationOutputs.FromDirectory(Path.Combine(test.Cache.CacheDirectory, "output")),
            "duplicate-files" => InvocationOutputs.FromFiles(image, image),
            "nested-files" => InvocationOutputs.FromFiles(image, Path.Combine(image, "child")),
            "file-in-cache" => InvocationOutputs.FromFiles(Path.Combine(test.Cache.CacheDirectory, "image.dll")),
            "cache-in-file" => InvocationOutputs.FromFiles(test.Root),
            _ => throw new InvalidOperationException()
        };
        var invocation = new InvocationDescription(InvocationInputs.FromFiles(), Array.Empty<string>(), outputs);
        int executions = 0;

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            InvocationCache.ExecuteCached(test.Log, s_cacheConfiguration,
                describeInvocation: () => invocation,
                executeNormally: () =>
                {
                    executions++;
                    return 42;
                }));

        Assert.Equal(nameof(outputs), exception.ParamName);
        Assert.Equal(0, executions);
        Assert.Equal("untouched", File.ReadAllText(stale));
        Assert.Equal("cached", File.ReadAllText(Path.Combine(test.Entry, "outputs", "image.dll")));
    }, shape).Dispose();

    [Fact]
    public void SharedTaskAdapterBypassesUnreadableInputWithoutCacheCleanup() => RemoteExecutor.Invoke(static () =>
    {
        using var test = new CacheTestDirectory();
        Environment.SetEnvironmentVariable(EnabledEnvironmentVariable, "true");
        Environment.SetEnvironmentVariable(PathEnvironmentVariable, test.Cache.CacheDirectory);
        Directory.CreateDirectory(test.Output);
        string stale = Path.Combine(test.Output, "stale.dll");
        File.WriteAllText(stale, "untouched");
        var invocation = new InvocationDescription(InvocationInputs.FromFiles(test.Source), Array.Empty<string>(), test.OutputOutputs);
        int executions = 0;

        int result = InvocationCache.ExecuteCached(test.Log, s_cacheConfiguration,
            describeInvocation: () => invocation,
            executeNormally: () =>
            {
                executions++;
                Assert.Equal("untouched", File.ReadAllText(stale));
                return 42;
            });

        Assert.Equal(42, result);
        Assert.Equal(1, executions);
        Assert.False(Directory.Exists(test.Cache.CacheDirectory));
        Assert.Contains(test.BuildEngine.Messages, message => message.Message?.Contains("input identity could not be computed") is true);
    }).Dispose();

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void SharedCacheRestoresDeclaredOutputContract(bool ownedDirectory, bool includeOptional)
    {
        using var test = new CacheTestDirectory();
        InvocationCache cache = test.Cache;
        test.WriteSource("image.dll", "image");
        if (includeOptional)
            test.WriteSource(Path.Combine("symbols", "image.pdb"), "symbols");

        cache.Store(CacheTestDirectory.Key, Contract(test.Source));
        Directory.CreateDirectory(Path.Combine(test.Output, "symbols"));
        string stale = Path.Combine(test.Output, "unrelated.txt");
        File.WriteAllText(stale, "keep unless the whole directory is owned");
        File.WriteAllText(Path.Combine(test.Output, "symbols", "image.pdb"), "stale symbols");

        Assert.True(cache.TryRestore(CacheTestDirectory.Key, Contract(test.Output), CacheTestDirectory.Timestamp));
        Assert.Equal("image", File.ReadAllText(Path.Combine(test.Output, "image.dll")));
        Assert.Equal(includeOptional, File.Exists(Path.Combine(test.Output, "symbols", "image.pdb")));
        if (includeOptional)
            Assert.Equal("symbols", File.ReadAllText(Path.Combine(test.Output, "symbols", "image.pdb")));
        Assert.Equal(!ownedDirectory, File.Exists(stale));
        Assert.Equal(CacheTestDirectory.Timestamp, File.GetLastWriteTimeUtc(Path.Combine(test.Output, "image.dll")));

        InvocationOutputs Contract(string directory) => ownedDirectory
            ? InvocationOutputs.FromDirectory(directory)
            : InvocationOutputs.FromFiles(
                Path.Combine(directory, "image.dll"),
                Path.Combine(directory, "symbols", "image.pdb"));
    }

    [Fact]
    public void SharedCacheRecordsAbsentDeclaredFiles()
    {
        using var test = new CacheTestDirectory();
        InvocationCache cache = test.Cache;
        InvocationOutputs outputs = InvocationOutputs.FromFiles(
            Path.Combine(test.Output, "missing.dll"));
        cache.Store(CacheTestDirectory.Key, outputs);
        Assert.True(Directory.Exists(test.Entry));
        Directory.CreateDirectory(test.Output);
        File.WriteAllText(Path.Combine(test.Output, "missing.dll"), "stale");
        Assert.True(cache.TryRestore(CacheTestDirectory.Key, outputs, CacheTestDirectory.Timestamp));
        Assert.False(File.Exists(Path.Combine(test.Output, "missing.dll")));
        Assert.Empty(Directory.GetDirectories(Path.Combine(cache.CacheDirectory, InvocationCacheEntry.VersionDirectory), "*.tmp"));
    }

    [Theory]
    [InlineData("content")]
    [InlineData("absence")]
    [InlineData("context")]
    [InlineData("output")]
    [InlineData("input-order")]
    public void SharedFingerprintIncludesDeclaredInputsContextAndOutputs(string change)
    {
        using var test = new CacheTestDirectory();
        test.WriteSource("first.dll", "first");
        test.WriteSource("second.dll", "second");
        string first = Path.Combine(test.Source, "first.dll");
        string second = Path.Combine(test.Source, "second.dll");
        string missing = Path.Combine(test.Source, "missing.dll");
        string[] inputs = new[] { first, second, missing };
        string[] context = new[] { "arguments" };
        InvocationOutputs outputs = test.OutputOutputs;
        string before = InvocationCache.ComputeKey(new InvocationDescription(InvocationInputs.FromFiles(inputs), context, outputs));

        switch (change)
        {
            case "content": File.WriteAllText(first, "changed"); break;
            case "absence": File.WriteAllText(missing, "now present"); break;
            case "context": context = new[] { "changed arguments" }; break;
            case "output": outputs = InvocationOutputs.FromFiles(Path.Combine(test.Output, "other.dll")); break;
            case "input-order": inputs = new[] { second, first, missing }; break;
        }

        Assert.NotEqual(before, InvocationCache.ComputeKey(new InvocationDescription(InvocationInputs.FromFiles(inputs), context, outputs)));
    }

    [Fact]
    public void SharedFingerprintDoesNotTreatUnreadableInputsAsAbsent()
    {
        using var test = new CacheTestDirectory();
        var invocation = new InvocationDescription(InvocationInputs.FromFiles(test.Source), Array.Empty<string>(), test.OutputOutputs);
        Assert.False(InvocationCache.TryComputeKey(test.Log, "Test", invocation, out string key));
        Assert.Equal(string.Empty, key);
        Assert.Contains(test.BuildEngine.Messages, message => message.Message?.Contains("input identity could not be computed") is true);
    }

    [Fact]
    public void SharedExecutionRecordsAbsentOutputsAfterSuccess()
    {
        using var test = new CacheTestDirectory();
        Directory.CreateDirectory(test.Output);
        string image = Path.Combine(test.Output, "image.dll");
        string symbols = Path.Combine(test.Output, "image.pdb");
        File.WriteAllText(image, "stale image");
        File.WriteAllText(symbols, "stale symbols");
        var request = new InvocationCacheRequest(CacheTestDirectory.Key, InvocationOutputs.FromFiles(image, symbols));
        int executions = 0;

        int Run() => InvocationCache.ExecutePrepared(test.Cache, CacheTestDirectory.Timestamp, request, () =>
        {
            executions++;
            Assert.Equal("stale image", File.ReadAllText(image));
            Assert.Equal("stale symbols", File.ReadAllText(symbols));
            File.Delete(image);
            File.Delete(symbols);
            return 0;
        });

        Assert.Equal(0, Run());
        Assert.True(Directory.Exists(test.Entry));
        File.WriteAllText(image, "new stale image");
        File.WriteAllText(symbols, "new stale symbols");
        Assert.Equal(0, Run());
        Assert.Equal(1, executions);
        Assert.False(File.Exists(image));
        Assert.False(File.Exists(symbols));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SharedCacheUsesOneManifestFormat(bool ownedDirectory)
    {
        using var test = new CacheTestDirectory();
        test.WriteSource("image.dll", "image");
        InvocationOutputs source = ownedDirectory ? test.SourceOutputs
            : InvocationOutputs.FromFiles(Path.Combine(test.Source, "image.dll"));
        test.Cache.Store(CacheTestDirectory.Key, source);

        Assert.Equal("v1", InvocationCacheEntry.VersionDirectory);
        byte[] manifestBytes = File.ReadAllBytes(Path.Combine(test.Entry, "manifest"));
        Assert.Equal(SHA256.HashData(manifestBytes), File.ReadAllBytes(Path.Combine(test.Entry, "manifest.sha256")));
        using var manifest = new BinaryReader(new MemoryStream(manifestBytes));
        Assert.Equal(1, manifest.ReadInt32());
        Assert.Equal(!ownedDirectory, manifest.ReadBoolean());
        Assert.Equal(1, manifest.ReadInt32());
        Assert.Equal(ownedDirectory ? "image.dll" : "0", manifest.ReadString());
        Assert.True(manifest.ReadBoolean());
        byte[] image = File.ReadAllBytes(Path.Combine(test.Source, "image.dll"));
        Assert.Equal(image.Length, manifest.ReadInt64());
        Assert.Equal(SHA256.HashData(image), manifest.ReadBytes(32));
        Assert.Equal(manifest.BaseStream.Length, manifest.BaseStream.Position);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SharedCacheRejectsDifferentOutputContract(bool ownedDirectory)
    {
        using var test = new CacheTestDirectory();
        test.WriteSource("0", "cached");
        InvocationOutputs source = ownedDirectory ? test.SourceOutputs
            : InvocationOutputs.FromFiles(Path.Combine(test.Source, "0"));
        test.Cache.Store(CacheTestDirectory.Key, source);
        Directory.CreateDirectory(test.Output);
        string image = Path.Combine(test.Output, "0");
        File.WriteAllText(image, "untouched");
        InvocationOutputs destination = ownedDirectory
            ? InvocationOutputs.FromFiles(image)
            : test.OutputOutputs;

        Assert.False(test.Cache.TryRestore(CacheTestDirectory.Key, destination, CacheTestDirectory.Timestamp));

        Assert.Equal("untouched", File.ReadAllText(image));
    }

    [Theory]
    [InlineData(false, "missing-checksum")]
    [InlineData(false, "corrupt-checksum")]
    [InlineData(false, "corrupt-manifest")]
    [InlineData(true, "missing-checksum")]
    [InlineData(true, "corrupt-checksum")]
    [InlineData(true, "corrupt-manifest")]
    public void SharedCacheValidatesManifestIntegrityBeforeModifyingOutputs(bool ownedDirectory, string damage)
    {
        using var test = new CacheTestDirectory();
        test.WriteSource("image.dll", "cached");
        InvocationOutputs source = ownedDirectory ? test.SourceOutputs
            : InvocationOutputs.FromFiles(Path.Combine(test.Source, "image.dll"));
        test.Cache.Store(CacheTestDirectory.Key, source);
        string checksum = Path.Combine(test.Entry, "manifest.sha256");
        switch (damage)
        {
            case "missing-checksum":
                File.Delete(checksum);
                break;
            case "corrupt-checksum":
                File.WriteAllBytes(checksum, new byte[32]);
                break;
            case "corrupt-manifest":
                File.WriteAllText(Path.Combine(test.Entry, "manifest"), "corrupt");
                break;
        }
        Directory.CreateDirectory(test.Output);
        string image = Path.Combine(test.Output, "image.dll");
        File.WriteAllText(image, "untouched");
        InvocationOutputs destination = ownedDirectory ? test.OutputOutputs
            : InvocationOutputs.FromFiles(image);

        Assert.False(test.Cache.TryRestore(CacheTestDirectory.Key, destination, CacheTestDirectory.Timestamp));

        Assert.Equal("untouched", File.ReadAllText(image));
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("nested")]
    [InlineData("cache-child")]
    [InlineData("cache-parent")]
    public void SharedCacheRejectsOverlappingFileDeclarations(string overlap)
    {
        using var test = new CacheTestDirectory();
        InvocationCache cache = test.Cache;
        string first = Path.Combine(test.Output, "image.dll");
        string second = overlap switch
        {
            "duplicate" => first,
            "nested" => Path.Combine(first, "child"),
            "cache-child" => Path.Combine(cache.CacheDirectory, "output.dll"),
            "cache-parent" => test.Root,
            _ => throw new InvalidOperationException()
        };
        InvocationOutputs outputs = InvocationOutputs.FromFiles(
            first, second);
        Assert.Throws<ArgumentException>(() => cache.Store(CacheTestDirectory.Key, outputs));
        Assert.Throws<ArgumentException>(() => cache.TryRestore(CacheTestDirectory.Key, outputs, CacheTestDirectory.Timestamp));
        Assert.False(Directory.Exists(cache.CacheDirectory));
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
        int executions = 0;

        int exitCode = InvocationCache.ExecutePrepared(configured ? test.Cache : null, CacheTestDirectory.Timestamp,
            eligible ? new InvocationCacheRequest(CacheTestDirectory.Key, test.OutputOutputs) : null,
            () =>
            {
                executions++;
                Directory.Delete(test.Output, recursive: true);
                Directory.CreateDirectory(test.Output);
                return 42;
            });

        Assert.Equal(42, exitCode);
        Assert.Equal(1, executions);
        Assert.False(File.Exists(stale));
        Assert.False(Directory.Exists(test.Cache.CacheDirectory));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SharedExecutionStoresSuccessfulResultsAndRestoresWithoutExecuting(bool ownedDirectory)
    {
        using var test = new CacheTestDirectory();
        string image = Path.Combine(test.Output, "image.dll");
        string symbols = Path.Combine(test.Output, "image.pdb");
        InvocationOutputs outputs = ownedDirectory ? test.OutputOutputs
            : InvocationOutputs.FromFiles(image, symbols);
        var request = new InvocationCacheRequest(CacheTestDirectory.Key, outputs);
        var events = new List<string>();
        Directory.CreateDirectory(test.Output);
        File.WriteAllText(image, "stale image");
        File.WriteAllText(symbols, "stale symbols");

        int Run() => InvocationCache.ExecutePrepared(test.Cache, CacheTestDirectory.Timestamp, request,
            () =>
            {
                events.Add("execute");
                Assert.Equal("stale image", File.ReadAllText(image));
                Assert.Equal("stale symbols", File.ReadAllText(symbols));
                if (ownedDirectory)
                {
                    Directory.Delete(test.Output, recursive: true);
                }
                else
                {
                    File.Delete(image);
                    File.Delete(symbols);
                }
                Directory.CreateDirectory(test.Output);
                File.WriteAllText(image, "computed");
                return 0;
            });

        Assert.Equal(0, Run());
        Assert.Equal(new[] { "execute" }, events);
        Assert.True(Directory.Exists(test.Entry));
        events.Clear();
        File.WriteAllText(image, "outdated");
        File.WriteAllText(symbols, "stale symbols");
        string neighbor = Path.Combine(test.Output, "unrelated.txt");
        File.WriteAllText(neighbor, "neighbor");

        Assert.Equal(0, Run());

        Assert.Empty(events);
        Assert.Equal("computed", File.ReadAllText(image));
        Assert.False(File.Exists(symbols));
        Assert.Equal(!ownedDirectory, File.Exists(neighbor));
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
        int result = InvocationCache.ExecutePrepared(test.Cache, CacheTestDirectory.Timestamp,
            new InvocationCacheRequest(CacheTestDirectory.Key, test.OutputOutputs),
            () =>
            {
                executions++;
                Directory.CreateDirectory(test.Output);
                File.WriteAllText(Path.Combine(test.Output, "image.dll"), "produced");
                if (logError)
                    test.Log.LogError("Expected compiler error.");
                return exitCode;
            });

        Assert.Equal(exitCode, result);
        Assert.Equal(1, executions);
        Assert.False(Directory.Exists(test.Entry));
    }

    [Fact]
    public void SharedExecutionPropagatesTaskExceptions()
    {
        using var test = new CacheTestDirectory();
        int executions = 0;
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            InvocationCache.ExecutePrepared(test.Cache, CacheTestDirectory.Timestamp,
                new InvocationCacheRequest(CacheTestDirectory.Key, test.OutputOutputs),
                () =>
                {
                    executions++;
                    throw new InvalidOperationException("execute");
                }));

        Assert.Equal("execute", exception.Message);
        Assert.Equal(1, executions);
        Assert.False(Directory.Exists(test.Entry));
    }

    [Fact]
    public void SharedExecutionFallsBackAfterRestoreFailure()
    {
        using var test = new CacheTestDirectory();
        test.WriteSource("image.dll", "cached");
        test.Cache.Store(CacheTestDirectory.Key, test.SourceOutputs);
        File.WriteAllText(Path.Combine(test.Entry, "outputs", "image.dll"), "corrupt");
        Directory.CreateDirectory(test.Output);
        string stale = Path.Combine(test.Output, "stale.dll");
        File.WriteAllText(stale, "stale");
        int executions = 0;

        Assert.Equal(0, InvocationCache.ExecutePrepared(test.Cache, CacheTestDirectory.Timestamp,
            new InvocationCacheRequest(CacheTestDirectory.Key, test.OutputOutputs),
            () =>
            {
                executions++;
                Assert.Equal("stale", File.ReadAllText(stale));
                Directory.Delete(test.Output, recursive: true);
                Directory.CreateDirectory(test.Output);
                File.WriteAllText(Path.Combine(test.Output, "image.dll"), "recomputed");
                return 0;
            }));

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
        internal InvocationOutputs SourceOutputs { get; }
        internal InvocationOutputs OutputOutputs { get; }
        internal string Entries => Path.Combine(Cache.CacheDirectory, InvocationCacheEntry.VersionDirectory);
        internal string Entry => Path.Combine(Entries, Key);
        internal CacheBuildEngine BuildEngine { get; } = new();
        internal TaskLoggingHelper Log { get; }
        internal InvocationCache Cache { get; }
        internal string MutexName => Assert.IsType<string>(
            typeof(InvocationCache).GetMethod("GetWriterMutexName", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(Cache, new object[] { Key }));

        internal CacheTestDirectory()
        {
            Directory.CreateDirectory(Source);
            var task = new CacheTask { BuildEngine = BuildEngine };
            Log = task.Log;
            Cache = new InvocationCache(Path.Combine(Root, "cache"), Log, "Test");
            SourceOutputs = InvocationOutputs.FromDirectory(Source);
            OutputOutputs = InvocationOutputs.FromDirectory(Output);
        }

        internal void WriteSource(string relativePath, string contents)
        {
            string path = Path.Combine(Source, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
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
