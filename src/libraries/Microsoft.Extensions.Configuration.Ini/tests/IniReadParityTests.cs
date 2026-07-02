// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace SharpNinja.Extensions.Configuration.Ini.Tests;

using System;
using System.IO;
using System.Linq;
using global::Microsoft.Extensions.Configuration;
using Xunit;

/// <summary>
/// The writable provider's read path must be byte-for-byte equivalent to the
/// built-in INI provider. The built-in <c>AddIniFile</c> here is the verbatim
/// Microsoft source carried by this package, so parity with it is parity with
/// <c>Microsoft.Extensions.Configuration.Ini</c>.
/// </summary>
public sealed class IniReadParityTests
{
    private const string Sample =
        "[Version]\n" +
        "ConfigVersion=3.8\n" +
        "\n" +
        "[C64SC]\n" +
        "SaveResourcesOnExit=1\n" +
        "VICIIModel=3\n" +
        "WIC64MACAddress=\"08:d1:f9:0a:0c:0e\"\n" +
        "; a comment line\n" +
        "Drive8Type=1541\n";

    private static string WriteTemp(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"sncfg-{Guid.NewGuid():N}.ini");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void WritableProvider_ReadsSameKeysAsBuiltInIniProvider()
    {
        var path = WriteTemp(Sample);
        try
        {
            var builtin = new ConfigurationBuilder().AddIniFile(path, optional: false).Build();
            var writable = new ConfigurationBuilder().AddWritableIniFile(path, optional: false).Build();

            var expected = builtin.AsEnumerable()
                .Where(k => k.Value != null)
                .ToDictionary(k => k.Key, k => k.Value, StringComparer.OrdinalIgnoreCase);
            var actual = writable.AsEnumerable()
                .Where(k => k.Value != null)
                .ToDictionary(k => k.Key, k => k.Value, StringComparer.OrdinalIgnoreCase);

            Assert.Equal(expected, actual);
            Assert.Equal("3", actual["C64SC:VICIIModel"]);
            Assert.Equal("08:d1:f9:0a:0c:0e", actual["C64SC:WIC64MACAddress"]); // quotes stripped
            Assert.DoesNotContain(actual.Keys, k => k.Contains("comment", StringComparison.OrdinalIgnoreCase));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void WritableProvider_OptionalMissingFile_YieldsEmpty()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"sncfg-missing-{Guid.NewGuid():N}.ini");
        var config = new ConfigurationBuilder().AddWritableIniFile(missing, optional: true).Build();
        Assert.DoesNotContain(config.AsEnumerable(), k => k.Value != null);
    }
}
