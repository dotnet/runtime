// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using Xunit;

namespace System.Runtime.InteropServices.JavaScript.Tests;

public partial class AppSettingsTests
{
    [Fact]
    public void GetAppSettingsFilesSelectsApplicationEnvironment()
    {
        string[] expected = ["appsettings.json", "appsettings.AppSettingsTests.json"];
        string[] files = GetAppSettingsFiles();
        Assert.Equal(expected, files);
        Assert.False(File.Exists("appsettings.Development.json"));
        Assert.Null(GetAppSettingsBytes("appsettings.Development.json"));

        Assert.True(HasIndependentFileLists());
    }

    [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsMonoRuntime))]
    [InlineData("duplicates", "appsettings.json,appsettings.AppSettingsTests.json")]
    [InlineData("withoutBase", "appsettings.AppSettingsTests.json")]
    [InlineData("empty", "")]
    [InlineData("absent", "")]
    public void GetAppSettingsFilesUsesCurrentMonoConfiguration(string variation, string expected)
    {
        Assert.Equal(expected, string.Join(",", GetFilesWithChangedConfig(variation)));
    }

    [Theory]
    [InlineData("appsettings.json", "{ \"value\": \"base\" }")]
    [InlineData("appsettings.AppSettingsTests.json", "")]
    public void GetAppSettingsBytesReturnsDownloadedContents(string fileName, string expected)
    {
        byte[]? bytes = GetAppSettingsBytes(fileName);
        Assert.NotNull(bytes);
        Assert.Equal(expected, Encoding.UTF8.GetString(bytes).Trim());
        Assert.Equal(File.ReadAllBytes(fileName), bytes);
        Assert.Equal(PlatformDetection.IsCoreCLR, HasSharedBytes(fileName));
    }

    [Fact]
    public void GetAppSettingsBytesForMissingAndVfsOnlyFiles()
    {
        Assert.Null(GetAppSettingsBytes("appsettings.Missing.json"));

        const string FileName = "appsettings.VfsOnly.json";
        File.WriteAllBytes(FileName, []);
        try
        {
            if (PlatformDetection.IsMonoRuntime)
            {
                Assert.Empty(GetAppSettingsBytes(FileName)!);
            }
            else
            {
                Assert.Null(GetAppSettingsBytes(FileName));
            }
        }
        finally
        {
            File.Delete(FileName);
        }
    }

    [Fact]
    public void GetAppSettingsBytesMutation()
    {
        Assert.Equal(PlatformDetection.IsCoreCLR, MutationChangesStoredBytes("appsettings.json"));
    }

    [Fact]
    public void GetAppSettingsBytesForDirectory()
    {
        if (PlatformDetection.IsMonoRuntime)
        {
            Assert.Throws<JSException>(() => GetAppSettingsBytes("/"));
        }
        else
        {
            Assert.Null(GetAppSettingsBytes("/"));
        }
    }

    [Fact]
    public void GetAppSettingsBytesAfterVfsUpdate()
    {
        const string FileName = "appsettings.json";
        byte[] original = File.ReadAllBytes(FileName);
        byte[] updated = [1, 2, 3];
        try
        {
            File.WriteAllBytes(FileName, updated);
            Assert.Equal(PlatformDetection.IsMonoRuntime ? updated : original, GetAppSettingsBytes(FileName));
        }
        finally
        {
            File.WriteAllBytes(FileName, original);
        }
    }

    [JSImport("INTERNAL.getAppSettingsFiles")]
    private static partial string[] GetAppSettingsFiles();

    [JSImport("INTERNAL.getAppSettingsBytes")]
    private static partial byte[]? GetAppSettingsBytes(string fileName);

    [JSImport("globalThis.appSettingsTests.hasSharedBytes")]
    private static partial bool HasSharedBytes(string fileName);

    [JSImport("globalThis.appSettingsTests.mutationChangesStoredBytes")]
    private static partial bool MutationChangesStoredBytes(string fileName);

    [JSImport("globalThis.appSettingsTests.hasIndependentFileLists")]
    private static partial bool HasIndependentFileLists();

    [JSImport("globalThis.appSettingsTests.getFilesWithChangedConfig")]
    private static partial string[] GetFilesWithChangedConfig(string variation);
}
