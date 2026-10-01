// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.IO;
using ILLink.Tasks;
using Xunit;

namespace ILLink.CacheTool.Tests;

public class CachePurgeTests
{
    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    public void PurgeUsesLastUsedContentsAndExclusiveCutoff(int ticks, bool deleted)
    {
        using var cache = new TestCache();
        string entry = cache.AddEntry('a', TestCache.Cutoff.AddTicks(ticks));
        File.SetLastWriteTimeUtc(Path.Combine(entry, ILLinkCacheEntry.LastUsedFileName), DateTime.UtcNow);

        Assert.Equal(0, cache.Run());

        Assert.Equal(!deleted, Directory.Exists(entry));
        Assert.Equal($"ILLink cache purge: Deleted: {(deleted ? 1 : 0)}, Kept: {(deleted ? 0 : 1)}, Errors: 0{Environment.NewLine}",
            cache.Output.ToString());
        Assert.Empty(cache.Error.ToString());
    }

    [Fact]
    public void PurgeOnlyVisitsPublishedEntriesInCurrentLayout()
    {
        using var cache = new TestCache();
        string old = cache.AddEntry('a', TestCache.Cutoff.AddDays(-1));
        string recent = cache.AddEntry('b', TestCache.Cutoff.AddDays(1));
        foreach (string name in new[] { new string('a', 64) + ".unique.tmp", "unrelated", new string('A', 64), new string('c', 63) })
        {
            Directory.CreateDirectory(Path.Combine(cache.Entries, name));
        }
        string otherVersion = Path.Combine(cache.Root, "v2", new string('d', 64));
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
    [InlineData("missing")]
    [InlineData("malformed")]
    [InlineData("directory")]
    [InlineData("non-utc")]
    public void InvalidMarkerReportsErrorAndDoesNotDeleteEntry(string damage)
    {
        using var cache = new TestCache();
        string damagedEntry = cache.AddEntry('a', TestCache.Cutoff.AddDays(-1));
        string validEntry = cache.AddEntry('b', TestCache.Cutoff.AddDays(-1));
        string marker = Path.Combine(damagedEntry, ILLinkCacheEntry.LastUsedFileName);
        switch (damage)
        {
            case "missing":
                File.Delete(marker);
                break;
            case "malformed":
                File.WriteAllText(marker, "invalid timestamp");
                break;
            case "directory":
                File.Delete(marker);
                Directory.CreateDirectory(marker);
                break;
            case "non-utc":
                File.WriteAllText(marker, TestCache.Cutoff.ToOffset(TimeSpan.FromHours(1)).ToString("O", CultureInfo.InvariantCulture));
                break;
        }

        Assert.Equal(1, cache.Run());

        Assert.True(Directory.Exists(damagedEntry));
        Assert.False(Directory.Exists(validEntry));
        Assert.Contains(damagedEntry, cache.Error.ToString());
        Assert.Contains("Deleted: 1, Kept: 0, Errors: 1", cache.Output.ToString());
    }

    [Fact]
    public void MissingVersionDirectoryIsReported()
    {
        using var cache = new TestCache();
        Directory.Delete(cache.Entries);

        Assert.Equal(1, cache.Run());

        Assert.Contains(cache.Entries, cache.Error.ToString());
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

    [Theory]
    [InlineData("2025-01-02T03:04:05Z")]
    [InlineData("2025-01-02T03:04:05+00:00")]
    [InlineData("2025-01-02T03:04:05.0000000+00:00")]
    [InlineData("2025-01-02T03:04:05.0000000Z")]
    public void CommandAcceptsExplicitUtcCutoff(string cutoff)
    {
        using var cache = new TestCache();
        string entry = cache.AddEntry('a', TestCache.Cutoff.AddTicks(-1));

        Assert.Equal(0, Program.Run(new[] { "purge", "--cache-directory", cache.Root, "--before", cutoff }, cache.Output, cache.Error));

        Assert.False(Directory.Exists(entry));
        Assert.Contains("Deleted: 1", cache.Output.ToString());
        Assert.Empty(cache.Error.ToString());
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData("2025-01-02")]
    [InlineData("2025-01-02T03:04:05")]
    [InlineData("2025-01-02T03:04:05+01:00")]
    public void CommandRejectsInvalidCutoffWithoutDeleting(string cutoff)
    {
        using var cache = new TestCache();
        string entry = cache.AddEntry('a', TestCache.Cutoff.AddDays(-1));

        Assert.NotEqual(0, Program.Run(new[] { "purge", "--cache-directory", cache.Root, "--before", cutoff }, cache.Output, cache.Error));

        Assert.True(Directory.Exists(entry));
        Assert.Contains("--before", cache.Error.ToString());
        Assert.DoesNotContain("ILLink cache purge:", cache.Output.ToString());
    }

    [Theory]
    [InlineData("missing-directory")]
    [InlineData("missing-cutoff")]
    [InlineData("missing-value")]
    [InlineData("unknown-option")]
    [InlineData("unknown-command")]
    public void CommandRejectsIncompleteOrUnknownArguments(string scenario)
    {
        using var cache = new TestCache();
        string[] args = scenario switch
        {
            "missing-directory" => new[] { "purge", "--before", "2025-01-02T03:04:05Z" },
            "missing-cutoff" => new[] { "purge", "--cache-directory", cache.Root },
            "missing-value" => new[] { "purge", "--cache-directory", cache.Root, "--before" },
            "unknown-option" => new[] { "purge", "--cache-directory", cache.Root, "--before", "2025-01-02T03:04:05Z", "--force" },
            _ => new[] { "delete-all" }
        };
        string entry = cache.AddEntry('a', TestCache.Cutoff.AddDays(-1));

        Assert.NotEqual(0, Program.Run(args, cache.Output, cache.Error));

        Assert.True(Directory.Exists(entry));
        Assert.NotEmpty(cache.Error.ToString());
    }

    [Fact]
    public void CommandRejectsNonexistentDirectory()
    {
        using var cache = new TestCache();
        string missing = Path.Combine(cache.Root, "missing");

        Assert.NotEqual(0, Program.Run(new[] { "purge", "--cache-directory", missing, "--before", "2025-01-02T03:04:05Z" }, cache.Output, cache.Error));

        Assert.NotEmpty(cache.Error.ToString());
        Assert.False(Directory.Exists(missing));
    }

    [Fact]
    public void CommandPropagatesMaintenanceFailure()
    {
        using var cache = new TestCache();
        string entry = cache.AddEntry('a', TestCache.Cutoff.AddDays(-1));
        File.Delete(Path.Combine(entry, ILLinkCacheEntry.LastUsedFileName));

        Assert.Equal(1, Program.Run(new[] { "purge", "--cache-directory", cache.Root, "--before", "2025-01-02T03:04:05Z" }, cache.Output, cache.Error));

        Assert.True(Directory.Exists(entry));
        Assert.Contains("Errors: 1", cache.Output.ToString());
    }

    [Fact]
    public void HelpExplainsIdleCacheRequirement()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "purge", "--help" }, output, error));
        Assert.Contains("No builds may use this cache while purging.", output.ToString());
        Assert.Empty(error.ToString());
    }

    private sealed class TestCache : IDisposable
    {
        internal static readonly DateTimeOffset Cutoff = new(2025, 1, 2, 3, 4, 5, TimeSpan.Zero);
        internal string Root { get; } = Path.Combine(Path.GetTempPath(), "illink-purge-tests-" + Guid.NewGuid().ToString("N"));
        internal string Entries => Path.Combine(Root, ILLinkCacheEntry.VersionDirectory);
        internal StringWriter Output { get; } = new();
        internal StringWriter Error { get; } = new();

        internal TestCache() => Directory.CreateDirectory(Entries);

        internal string AddEntry(char key, DateTimeOffset lastUsed)
        {
            string entry = Path.Combine(Entries, new string(key, 64));
            Directory.CreateDirectory(Path.Combine(entry, "outputs"));
            File.WriteAllText(Path.Combine(entry, "outputs", "app.dll"), "cached output");
            File.WriteAllText(Path.Combine(entry, ILLinkCacheEntry.LastUsedFileName), lastUsed.ToString("O", CultureInfo.InvariantCulture));
            return entry;
        }

        internal int Run() => CachePurge.Run(Root, Cutoff, Output, Error);

        public void Dispose()
        {
            Output.Dispose();
            Error.Dispose();
            Directory.Delete(Root, recursive: true);
        }
    }
}
