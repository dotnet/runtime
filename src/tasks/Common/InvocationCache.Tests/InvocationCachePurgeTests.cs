// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Globalization;
using System.IO;
using Xunit;

namespace Microsoft.NET.Build.Tasks.Tests;

public class InvocationCachePurgeTests
{
    [Theory]
    [InlineData(InvocationCacheEntry.VersionDirectory, -1, true)]
    [InlineData(InvocationCacheEntry.VersionDirectory, 0, false)]
    [InlineData(InvocationCacheEntry.VersionDirectory, 1, false)]
    [InlineData("v2", -1, true)]
    [InlineData("v2", 0, false)]
    [InlineData("v2", 1, false)]
    public void PurgeUsesLastUsedContentsAndExclusiveCutoff(string versionDirectory, int ticks, bool deleted)
    {
        using var cache = new TestCache(versionDirectory);
        string entry = cache.AddEntry('a', TestCache.Cutoff.AddTicks(ticks));
        File.SetLastWriteTimeUtc(Path.Combine(entry, InvocationCacheEntry.LastUsedFileName), DateTime.UtcNow);

        Assert.Equal(0, cache.Run());

        Assert.Equal(!deleted, Directory.Exists(entry));
        Assert.Equal($"Test cache purge: Deleted: {(deleted ? 1 : 0)}, Kept: {(deleted ? 0 : 1)}, Errors: 0{Environment.NewLine}",
            cache.Output.ToString());
        Assert.Empty(cache.Error.ToString());
    }

    [Theory]
    [InlineData(InvocationCacheEntry.VersionDirectory, "v2")]
    [InlineData("v2", InvocationCacheEntry.VersionDirectory)]
    [InlineData("other-layout", "v1")]
    public void PurgeOnlyVisitsPublishedEntriesInSelectedLayout(string versionDirectory, string otherVersionDirectory)
    {
        using var cache = new TestCache(versionDirectory);
        string old = cache.AddEntry('a', TestCache.Cutoff.AddDays(-1));
        string recent = cache.AddEntry('b', TestCache.Cutoff.AddDays(1));
        foreach (string name in new[] { new string('a', 64) + ".unique.tmp", "unrelated", new string('E', 64), new string('c', 63) })
        {
            Directory.CreateDirectory(Path.Combine(cache.Entries, name));
        }
        string otherVersion = Path.Combine(cache.Root, otherVersionDirectory, new string('d', 64));
        Directory.CreateDirectory(otherVersion);
        File.WriteAllText(Path.Combine(cache.Entries, "unrelated-file"), "keep");

        Assert.Equal(0, cache.Run());

        Assert.False(Directory.Exists(old));
        Assert.True(Directory.Exists(recent));
        Assert.Equal(5, Directory.GetDirectories(cache.Entries).Length);
        Assert.True(Directory.Exists(otherVersion));
        Assert.Equal("keep", File.ReadAllText(Path.Combine(cache.Entries, "unrelated-file")));
        Assert.Contains("Deleted: 1, Kept: 1, Errors: 0", cache.Output.ToString());
    }

    [Theory]
    [InlineData("missing", false)]
    [InlineData("malformed", false)]
    [InlineData("empty", false)]
    [InlineData("directory", false)]
    [InlineData("non-utc", false)]
    [InlineData("missing", true)]
    [InlineData("malformed", true)]
    [InlineData("empty", true)]
    [InlineData("directory", true)]
    [InlineData("non-utc", true)]
    public void InvalidMarkerUsesEntryCreationTime(string damage, bool delete)
    {
        using var cache = new TestCache();
        string damagedEntry = cache.AddEntry('a', TestCache.Cutoff.AddDays(-1));
        string validEntry = cache.AddEntry('b', TestCache.Cutoff.AddDays(-1));
        string marker = Path.Combine(damagedEntry, InvocationCacheEntry.LastUsedFileName);
        switch (damage)
        {
            case "missing":
                File.Delete(marker);
                break;
            case "malformed":
                File.WriteAllText(marker, "invalid timestamp");
                break;
            case "empty":
                File.WriteAllText(marker, "");
                break;
            case "directory":
                File.Delete(marker);
                Directory.CreateDirectory(marker);
                break;
            case "non-utc":
                File.WriteAllText(marker, TestCache.Cutoff.ToOffset(TimeSpan.FromHours(1)).ToString("O", CultureInfo.InvariantCulture));
                break;
        }

        Assert.Equal(0, cache.Run(delete ? DateTimeOffset.UtcNow.AddMinutes(1) : TestCache.Cutoff));

        Assert.Equal(!delete, Directory.Exists(damagedEntry));
        Assert.False(Directory.Exists(validEntry));
        Assert.Contains(damagedEntry, cache.Error.ToString());
        Assert.Contains("using directory creation time", cache.Error.ToString());
        Assert.Equal($"Test cache purge: Deleted: {(delete ? 2 : 1)}, Kept: {(delete ? 0 : 1)}, Errors: 0{Environment.NewLine}",
            cache.Output.ToString());
    }

    [Fact]
    public void MissingVersionDirectoryIsReported()
    {
        using var cache = new TestCache();
        Directory.Delete(cache.Entries);

        Assert.Equal(1, cache.Run());

        Assert.StartsWith($"Could not enumerate Test cache '{cache.Entries}':", cache.Error.ToString());
        Assert.Contains("Errors: 1", cache.Output.ToString());
        Assert.False(Directory.Exists(cache.Entries));
    }

    [Fact]
    public void EmptyCacheSucceeds()
    {
        using var cache = new TestCache();
        Assert.Equal(0, cache.Run());
        Assert.Contains("Deleted: 0, Kept: 0, Errors: 0", cache.Output.ToString());
    }

    internal sealed class TestCache : IDisposable
    {
        internal static readonly DateTimeOffset Cutoff = new(2025, 1, 2, 3, 4, 5, TimeSpan.Zero);
        internal string Root { get; } = Path.Combine(Path.GetTempPath(), "invocation-cache-purge-tests-" + Guid.NewGuid().ToString("N"));
        internal string VersionDirectory { get; }
        internal string Entries => Path.Combine(Root, VersionDirectory);
        internal StringWriter Output { get; } = new();
        internal StringWriter Error { get; } = new();

        internal TestCache(string versionDirectory = InvocationCacheEntry.VersionDirectory)
        {
            VersionDirectory = versionDirectory;
            Directory.CreateDirectory(Entries);
        }

        internal string AddEntry(char key, DateTimeOffset lastUsed)
        {
            string entry = Path.Combine(Entries, new string(key, 64));
            Directory.CreateDirectory(Path.Combine(entry, "outputs"));
            File.WriteAllText(Path.Combine(entry, "outputs", "app.dll"), "cached output");
            File.WriteAllText(Path.Combine(entry, InvocationCacheEntry.LastUsedFileName), lastUsed.ToString("O", CultureInfo.InvariantCulture));
            return entry;
        }

        internal int Run(DateTimeOffset? cutoff = null) =>
            InvocationCachePurge.Run(Root, VersionDirectory, "Test", cutoff ?? Cutoff, Output, Error);

        public void Dispose()
        {
            Output.Dispose();
            Error.Dispose();
            Directory.Delete(Root, recursive: true);
        }
    }
}
