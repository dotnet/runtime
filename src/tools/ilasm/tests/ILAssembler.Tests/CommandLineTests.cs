// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace ILAssembler.Tests;

public class CommandLineTests
{
    public static TheoryData<string, string> NativeBooleanOptions { get; } = new()
    {
        { "32B", "--32bitpreferred" },
        { "APP", "--appcontainer" },
        { "CLO", "--clock" },
        { "DET", "--deterministic" },
        { "DLL", "--dll" },
        { "ERR", "--error" },
        { "EXE", "--exe" },
        { "FOL", "--fold" },
        { "HIG", "--highentropyva" },
        { "NOA", "--noautoinherit" },
        { "NOC", "--nocorstub" },
        { "NOL", "--nologo" },
        { "OPT", "--optimize" },
        { "PDB", "--pdb" },
        { "PE6", "--pe64" },
        { "QUI", "--quiet" },
        { "STR", "--stripreloc" },
        { "X64", "--x64" },
    };

    public static TheoryData<string, string> NativeValueOptions { get; } = new()
    {
        { "ALI", "--alignment" },
        { "ANA", "--aname" },
        { "BAS", "--base" },
        { "FLA", "--flags" },
        { "INC", "--include" },
        { "KEY", "--key" },
        { "MDV", "--mdv" },
        { "OUT", "--output" },
        { "SSV", "--ssver" },
        { "STA", "--stack" },
        { "SUB", "--subsystem" },
    };

    [Theory]
    [MemberData(nameof(NativeValueOptions))]
    public void NativeValueOption_IsCaseInsensitiveAndUsesThreeCharacterPrefix(
        string prefix,
        string expectedOption)
    {
        string nativeOption = $"-{prefix.ToLowerInvariant()}Suffix";

        Assert.Equal(
            [expectedOption, "value:part"],
            NativeCommandLine.Normalize([$"{nativeOption}=value:part"], allowSlashOptions: false));
        Assert.Equal(
            [expectedOption, "value=part"],
            NativeCommandLine.Normalize([$"{nativeOption}:value=part"], allowSlashOptions: false));
    }

    [Theory]
    [InlineData("ALI")]
    [InlineData("ANA")]
    [InlineData("BAS")]
    [InlineData("FLA")]
    [InlineData("INC")]
    [InlineData("KEY")]
    [InlineData("MDV")]
    [InlineData("OUT")]
    [InlineData("SSV")]
    [InlineData("STA")]
    [InlineData("SUB")]
    public void NativeValueOption_RequiresSeparator(string prefix)
    {
        Assert.Throws<ArgumentException>(
            () => NativeCommandLine.Normalize(
                [$"-{prefix.ToLowerInvariant()}Suffix"],
                allowSlashOptions: false));
    }

    [Theory]
    [MemberData(nameof(NativeBooleanOptions))]
    public void NativeBooleanOption_IsCaseInsensitiveAndUsesThreeCharacterPrefix(
        string prefix,
        string expectedOption)
    {
        Assert.Equal(
            [expectedOption],
            NativeCommandLine.Normalize(
                [$"-{prefix.ToLowerInvariant()}Suffix"],
                allowSlashOptions: false));
    }

    [Fact]
    public void NativeBooleanOption_IgnoresValue()
    {
        Assert.Equal(
            ["--dll"],
            NativeCommandLine.Normalize(["-dll:false"], allowSlashOptions: false));
    }

    [Fact]
    public void NativeDebugOption_UsesThreeCharacterPrefix()
    {
        Assert.Equal(
            ["--debug"],
            NativeCommandLine.Normalize(["-dEbSuffix"], allowSlashOptions: false));
    }

    [Theory]
    [InlineData("-deb:implicit", "Impl")]
    [InlineData("-DEBUGWHATEVER=optimized", "Opt")]
    public void NativeDebugSuboption_UsesThreeCharacterPrefix(string argument, string expectedMode)
    {
        Assert.Equal(
            ["--debug", "--debug-mode", expectedMode],
            NativeCommandLine.Normalize([argument], allowSlashOptions: false));
    }

    [Fact]
    public void NativeDebugSuboption_RejectsEmptyValue()
    {
        Assert.Throws<ArgumentException>(
            () => NativeCommandLine.Normalize(["-debug:  "], allowSlashOptions: false));
    }

    [Theory]
    [InlineData("-out:  test.exe", "--output", "test.exe")]
    [InlineData("-aname:  Test", "--aname", "  Test")]
    public void NativeValueOption_HandlesLeadingSpaces(
        string argument,
        string expectedOption,
        string expectedValue)
    {
        Assert.Equal(
            [expectedOption, expectedValue],
            NativeCommandLine.Normalize([argument], allowSlashOptions: false));
    }

    [Theory]
    [InlineData("-out:")]
    [InlineData("-key:  ")]
    [InlineData("-subsystem=")]
    public void NativeValueOption_RejectsEmptyValue(string argument)
    {
        Assert.Throws<ArgumentException>(
            () => NativeCommandLine.Normalize([argument], allowSlashOptions: false));
    }

    [Fact]
    public void NativeAssemblyNameOption_AllowsEmptyValue()
    {
        Assert.Equal(
            ["--aname", ""],
            NativeCommandLine.Normalize(["-aname:"], allowSlashOptions: false));
    }

    [Theory]
    [InlineData("/OuT:test.exe", "--output", "test.exe")]
    [InlineData("/dLlWhatever", "--dll", null)]
    [InlineData("/?", "--help", null)]
    public void SlashOption_IsNormalizedWhenEnabled(
        string argument,
        string expectedOption,
        string? expectedValue)
    {
        string[] expected = expectedValue is null
            ? [expectedOption]
            : [expectedOption, expectedValue];

        Assert.Equal(expected, NativeCommandLine.Normalize([argument], allowSlashOptions: true));
    }

    [Fact]
    public void SlashOption_IsPreservedWhenDisabled()
    {
        Assert.Equal(
            ["/output/source.il"],
            NativeCommandLine.Normalize(["/output/source.il"], allowSlashOptions: false));
    }

    [Fact]
    public void SlashOption_DefaultMatchesPlatform()
    {
        string[] expected = OperatingSystem.IsWindows()
            ? ["--dll"]
            : ["/dll"];

        Assert.Equal(expected, NativeCommandLine.Normalize(["/dll"]));
    }

    [Theory]
    [InlineData("-ARM", "--arm")]
    [InlineData("-arm64", "--arm64")]
    [InlineData("-ARM64Anything", "--arm64")]
    [InlineData("-ARM64=value", "--arm64")]
    public void ArmOptions_UseNativeDisambiguation(string argument, string expectedOption)
    {
        Assert.Equal(
            [expectedOption],
            NativeCommandLine.Normalize([argument], allowSlashOptions: false));
    }

    [Theory]
    [InlineData("-ARMAnything")]
    [InlineData("-ARM:value")]
    [InlineData("-ARM6")]
    public void InvalidArmOption_Throws(string argument)
    {
        Assert.Throws<ArgumentException>(
            () => NativeCommandLine.Normalize([argument], allowSlashOptions: false));
    }

    [Theory]
    [InlineData("-res:file.res")]
    [InlineData("-RESOURCE=file.res")]
    [InlineData("/ReSAnything:file.res")]
    public void UnsupportedResourceOption_Throws(string argument)
    {
        Assert.Throws<ArgumentException>(
            () => NativeCommandLine.Normalize([argument], allowSlashOptions: true));
    }

    [Fact]
    public void ModernAndShortOptions_ArePreserved()
    {
        string[] args = ["--output", "test.exe", "-o", "other.exe", "-O", "input.il"];
        Assert.Equal(args, NativeCommandLine.Normalize(args, allowSlashOptions: false));
    }

    [Theory]
    [InlineData("-OU")]
    [InlineData("-unknown")]
    [InlineData("input.il")]
    public void UnrecognizedArgument_IsPreserved(string argument)
    {
        Assert.Equal(
            [argument],
            NativeCommandLine.Normalize([argument], allowSlashOptions: false));
    }
}
