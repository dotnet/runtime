// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Security.Cryptography;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.NET.Build.Tasks;
using Xunit;
using TestCache = Microsoft.NET.Build.Tasks.Tests.InvocationCachePurgeTests.TestCache;

namespace BuildCache.Tests;

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
        Assert.Equal($"Build cache purge: Deleted: 1, Kept: 0, Errors: 0{Environment.NewLine}", cache.Output.ToString());
        Assert.Empty(cache.Error.ToString());
    }

    [Theory]
    [InlineData("v2")]
    [InlineData("other-layout")]
    public void CommandOnlyPurgesCurrentLayout(string otherVersion)
    {
        using var cache = new TestCache();
        string entry = cache.AddEntry('a', TestCache.Cutoff.AddTicks(-1));
        string otherEntry = Path.Combine(cache.Root, otherVersion, new string('b', 64));
        Directory.CreateDirectory(otherEntry);
        File.WriteAllText(Path.Combine(otherEntry, InvocationCacheEntry.LastUsedFileName),
            "2020-01-01T00:00:00.0000000+00:00");

        Assert.Equal(0, Program.Run(new[] { "purge", "--cache-directory", cache.Root,
            "--before", "2025-01-02T03:04:05Z" }, cache.Output, cache.Error));

        Assert.False(Directory.Exists(entry));
        Assert.True(Directory.Exists(otherEntry));
        Assert.Equal($"Build cache purge: Deleted: 1, Kept: 0, Errors: 0{Environment.NewLine}", cache.Output.ToString());
        Assert.Empty(cache.Error.ToString());
    }

    [Fact]
    public void CommandPurgesLegacyV1Entries()
    {
        using var cache = new TestCache("v1");
        string entry = cache.AddEntry('a', TestCache.Cutoff.AddTicks(-1));
        byte[] image = File.ReadAllBytes(Path.Combine(entry, "outputs", "app.dll"));
        using (var manifest = new BinaryWriter(File.Create(Path.Combine(entry, "manifest"))))
        {
            manifest.Write(1);
            manifest.Write(1);
            manifest.Write("app.dll");
            manifest.Write((long)image.Length);
            manifest.Write(SHA256.HashData(image));
        }
        Assert.False(File.Exists(Path.Combine(entry, "manifest.sha256")));

        Assert.Equal(0, Program.Run(new[] { "purge", "--cache-directory", cache.Root,
            "--before", "2025-01-02T03:04:05Z" }, cache.Output, cache.Error));

        Assert.False(Directory.Exists(entry));
        Assert.Equal($"Build cache purge: Deleted: 1, Kept: 0, Errors: 0{Environment.NewLine}", cache.Output.ToString());
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
        Assert.DoesNotContain("Build cache purge:", cache.Output.ToString());
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
        Assert.StartsWith($"Could not enumerate Build cache '{cache.Entries}':", cache.Error.ToString());
    }

    [Theory]
    [InlineData("unknown", false)]
    [InlineData("unknown", true)]
    [InlineData("", true)]
    [InlineData("ILLink", true)]
    public void CommandRejectsUnknownCacheEvenWithDirectory(string name, bool explicitDirectory)
    {
        using var cache = new TestCache();
        string entry = cache.AddEntry('a', TestCache.Cutoff.AddDays(-1));
        string[] args = explicitDirectory
            ? new[] { "purge", "--cache", name, "--cache-directory", cache.Root, "--before", "2025-01-02T03:04:05Z" }
            : new[] { "purge", "--cache", name, "--before", "2025-01-02T03:04:05Z" };

        Assert.Equal(1, Program.Run(args, cache.Output, cache.Error));

        Assert.True(Directory.Exists(entry));
        Assert.Contains("--cache must be 'illink' or 'crossgen2'.", cache.Error.ToString());
        Assert.Empty(cache.Output.ToString());
    }

    [Theory]
    [InlineData("illink", "true")]
    [InlineData("illink", "false")]
    [InlineData("illink", "invalid")]
    [InlineData("illink", "<unset>")]
    [InlineData("crossgen2", "true")]
    [InlineData("crossgen2", "false")]
    [InlineData("crossgen2", "invalid")]
    [InlineData("crossgen2", "<unset>")]
    public void NamedCacheUsesPathEnvironmentRegardlessOfEnablement(string name, string enabled) =>
        RemoteExecutor.Invoke(static (cacheName, enabledValue) =>
        {
            using var cache = new TestCache();
            string entry = cache.AddEntry('a', TestCache.Cutoff.AddDays(-1));
            string prefix = cacheName.ToUpperInvariant() + "_EXPERIMENTAL_CACHE";
            Environment.SetEnvironmentVariable(prefix, enabledValue == "<unset>" ? null : enabledValue);
            Environment.SetEnvironmentVariable(prefix + "_PATH", cache.Root);
            Environment.SetEnvironmentVariable("XDG_CACHE_HOME", Path.Combine(cache.Root, "unused-default"));

            Assert.Equal(0, Program.Run(new[] { "purge", "--cache", cacheName,
                "--before", "2025-01-02T03:04:05Z" }, cache.Output, cache.Error));

            Assert.False(Directory.Exists(entry));
            string label = cacheName == "illink" ? "ILLink" : "Crossgen2";
            Assert.Equal($"{label} cache purge: Deleted: 1, Kept: 0, Errors: 0{Environment.NewLine}", cache.Output.ToString());
            Assert.Empty(cache.Error.ToString());
            Assert.False(Directory.Exists(Path.Combine(cache.Root, "unused-default")));
        }, name, enabled).Dispose();

    [Theory]
    [InlineData("illink", false)]
    [InlineData("illink", true)]
    [InlineData("crossgen2", false)]
    [InlineData("crossgen2", true)]
    public void ExplicitDirectoryOverridesNamedEnvironmentWithoutFallback(string name, bool missing) =>
        RemoteExecutor.Invoke(static (cacheName, missingValue) =>
        {
            using var cache = new TestCache();
            using var environmentCache = new TestCache();
            string entry = cache.AddEntry('a', TestCache.Cutoff.AddDays(-1));
            string environmentEntry = environmentCache.AddEntry('a', TestCache.Cutoff.AddDays(-1));
            Environment.SetEnvironmentVariable(cacheName.ToUpperInvariant() + "_EXPERIMENTAL_CACHE_PATH", environmentCache.Root);
            bool missingDirectory = bool.Parse(missingValue);
            string directory = missingDirectory ? Path.Combine(cache.Root, "missing") : cache.Root;

            int exitCode = Program.Run(new[] { "purge", "--cache", cacheName, "--cache-directory", directory,
                "--before", "2025-01-02T03:04:05Z" }, cache.Output, cache.Error);

            Assert.Equal(missingDirectory, exitCode != 0);
            Assert.Equal(missingDirectory, Directory.Exists(entry));
            Assert.True(Directory.Exists(environmentEntry));
            Assert.Equal(!missingDirectory, Directory.Exists(directory));
            if (missingDirectory)
                Assert.NotEmpty(cache.Error.ToString());
            else
                Assert.Empty(cache.Error.ToString());
        }, name, missing.ToString()).Dispose();

    [Theory]
    [InlineData("illink", false)]
    [InlineData("illink", true)]
    [InlineData("crossgen2", false)]
    [InlineData("crossgen2", true)]
    public void NamedCacheRejectsMissingOrFileDirectoryWithoutCreatingIt(string name, bool file) =>
        RemoteExecutor.Invoke(static (cacheName, fileValue) =>
        {
            using var cache = new TestCache();
            string invalid = Path.Combine(cache.Root, "invalid");
            bool isFile = bool.Parse(fileValue);
            if (isFile)
                File.WriteAllText(invalid, "not a directory");
            Environment.SetEnvironmentVariable(cacheName.ToUpperInvariant() + "_EXPERIMENTAL_CACHE_PATH", invalid);

            Assert.Equal(1, Program.Run(new[] { "purge", "--cache", cacheName,
                "--before", "2025-01-02T03:04:05Z" }, cache.Output, cache.Error));

            Assert.False(Directory.Exists(invalid));
            Assert.Equal(isFile, File.Exists(invalid));
            Assert.Contains(invalid, cache.Error.ToString());
            Assert.Empty(cache.Output.ToString());
        }, name, file.ToString()).Dispose();

    [ConditionalTheory(typeof(OperatingSystem), nameof(OperatingSystem.IsLinux))]
    [PlatformSpecific(TestPlatforms.Linux)]
    [InlineData("illink", "<unset>")]
    [InlineData("illink", "")]
    [InlineData("crossgen2", "<unset>")]
    [InlineData("crossgen2", "")]
    public void NamedCacheUsesPlatformDefaultWhenPathIsUnsetOrEmpty(string name, string path) =>
        RemoteExecutor.Invoke(static (cacheName, pathValue) =>
        {
            using var cache = new TestCache();
            string entry = cache.AddEntry('a', TestCache.Cutoff.AddDays(-1));
            string root = Path.Combine(cache.Root, cacheName);
            Directory.CreateDirectory(root);
            Directory.Move(cache.Entries, Path.Combine(root, InvocationCacheEntry.VersionDirectory));
            entry = Path.Combine(root, InvocationCacheEntry.VersionDirectory, Path.GetFileName(entry));
            Environment.SetEnvironmentVariable("XDG_CACHE_HOME", cache.Root);
            Environment.SetEnvironmentVariable(cacheName.ToUpperInvariant() + "_EXPERIMENTAL_CACHE_PATH",
                pathValue == "<unset>" ? null : pathValue);

            Assert.Equal(root, InvocationCacheConfiguration.GetDefaultCacheDirectory(cacheName));
            Assert.Equal(0, Program.Run(new[] { "purge", "--cache", cacheName,
                "--before", "2025-01-02T03:04:05Z" }, cache.Output, cache.Error));

            Assert.False(Directory.Exists(entry));
            Assert.Empty(cache.Error.ToString());
        }, name, path).Dispose();

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
