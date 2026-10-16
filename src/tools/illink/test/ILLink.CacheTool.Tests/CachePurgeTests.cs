// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;
using TestCache = Microsoft.NET.Build.Tasks.Tests.InvocationCachePurgeTests.TestCache;

namespace ILLink.CacheTool.Tests;

public class CachePurgeTests
{
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
        Assert.Equal($"ILLink cache purge: Deleted: 1, Kept: 0, Errors: 0{Environment.NewLine}", cache.Output.ToString());
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
        Directory.Delete(cache.Entries);

        Assert.Equal(1, Program.Run(new[] { "purge", "--cache-directory", cache.Root, "--before", "2025-01-02T03:04:05Z" }, cache.Output, cache.Error));

        Assert.Contains("Errors: 1", cache.Output.ToString());
        Assert.StartsWith($"Could not enumerate ILLink cache '{cache.Entries}':", cache.Error.ToString());
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
}
