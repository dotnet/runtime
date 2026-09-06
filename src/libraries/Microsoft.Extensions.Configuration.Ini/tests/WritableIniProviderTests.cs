// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace SharpNinja.Extensions.Configuration.Ini.Tests;

using System;
using System.IO;
using global::Microsoft.Extensions.Configuration;
using global::Microsoft.Extensions.Configuration.Ini;
using Xunit;

/// <summary>
/// Provider-level write behavior: SetValue + Save round-trips through the file,
/// preserving resources the caller never touched and their quoting, and the
/// IConfiguration indexer path (Set) is reconciled into the document on Save.
/// </summary>
public sealed class WritableIniProviderTests
{
    private const string Sample =
        "[Version]\n" +
        "ConfigVersion=3.8\n" +
        "\n" +
        "[C64SC]\n" +
        "VICIIModel=3\n" +
        "WIC64MACAddress=\"08:d1:f9:0a:0c:0e\"\n" +
        "Drive8Type=1541\n" +
        "\n";

    private static string WriteTemp(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"sncfg-{Guid.NewGuid():N}.ini");
        File.WriteAllText(path, content);
        return path;
    }

    private static WritableIniConfigurationProvider Provider(string path)
        => (WritableIniConfigurationProvider)new WritableIniConfigurationSource { Path = path }
            .Build(new ConfigurationBuilder());

    [Fact]
    public void SetValue_Save_PersistsAndPreservesUnknownAndQuoting()
    {
        var path = WriteTemp(Sample);
        try
        {
            var provider = Provider(path);
            provider.Load();
            provider.SetValue("C64SC", "VICIIModel", "1");
            provider.SetValue("C64SC", "WIC64IPAddress", "192.168.41.165", quote: true);
            provider.Save();

            var text = File.ReadAllText(path);
            Assert.Contains("VICIIModel=1", text);
            Assert.Contains("WIC64IPAddress=\"192.168.41.165\"", text);
            Assert.Contains("Drive8Type=1541", text);                       // unmanaged key survives
            Assert.Contains("WIC64MACAddress=\"08:d1:f9:0a:0c:0e\"", text);  // quoting survives

            var reloaded = new ConfigurationBuilder().AddWritableIniFile(path).Build();
            Assert.Equal("1", reloaded["C64SC:VICIIModel"]);
            Assert.Equal("192.168.41.165", reloaded["C64SC:WIC64IPAddress"]);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Save_NewFile_CreatesDirectoryAndWrites()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"sncfg-{Guid.NewGuid():N}");
        var path = Path.Combine(dir, "settings.ini");
        try
        {
            var provider = Provider(path);
            provider.Load();
            provider.SetValue("C128", "VDCRevision", "1");
            provider.Save();

            Assert.True(File.Exists(path));
            Assert.Contains("[C128]", File.ReadAllText(path));
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void Set_ViaConfigurationIndexer_ThenSave_RoundTrips()
    {
        var path = WriteTemp(Sample);
        try
        {
            var provider = Provider(path);
            provider.Load();
            provider.Set("C64SC:VICIIModel", "2");   // the IConfiguration write path
            provider.Save();

            Assert.Contains("VICIIModel=2", File.ReadAllText(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Load_MissingRequiredFile_Throws()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"sncfg-req-{Guid.NewGuid():N}.ini");
        var provider = (WritableIniConfigurationProvider)new WritableIniConfigurationSource
        {
            Path = missing,
            Optional = false,
        }.Build(new ConfigurationBuilder());

        Assert.Throws<FileNotFoundException>(() => provider.Load());
    }
}
