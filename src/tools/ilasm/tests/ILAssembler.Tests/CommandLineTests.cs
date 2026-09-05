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

    public static TheoryData<string> NativeNonEmptyValueOptions { get; } = new()
    {
        "ALI",
        "BAS",
        "FLA",
        "INC",
        "KEY",
        "MDV",
        "OUT",
        "SSV",
        "STA",
        "SUB",
    };

    public static TheoryData<string> ModernValueOptions { get; } = new()
    {
        "--alignment",
        "--aname",
        "--base",
        "--debug-mode",
        "--flags",
        "--include",
        "--key",
        "--mdv",
        "--output",
        "--ssver",
        "--stack",
        "--subsystem",
        "-I",
        "-k",
        "-o",
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
        Assert.Equal(
            [expectedOption, "value"],
            NativeCommandLine.Normalize([$"/{nativeOption[1..]}=value"], allowSlashOptions: true));
    }

    [Theory]
    [MemberData(nameof(NativeValueOptions))]
    public void NativeValueOption_RequiresSeparator(string prefix, string _)
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
        Assert.Equal(
            [expectedOption],
            NativeCommandLine.Normalize(
                [$"/{prefix.ToLowerInvariant()}Suffix"],
                allowSlashOptions: true));
    }

    [Theory]
    [MemberData(nameof(NativeBooleanOptions))]
    public void NativeBooleanOption_IgnoresAttachedValue(string prefix, string expectedOption)
    {
        Assert.Equal(
            [expectedOption],
            NativeCommandLine.Normalize([$"-{prefix}:false"], allowSlashOptions: false));
        Assert.Equal(
            [expectedOption],
            NativeCommandLine.Normalize([$"-{prefix}="], allowSlashOptions: false));
        Assert.Equal(
            [expectedOption],
            NativeCommandLine.Normalize([$"/{prefix}:ignored"], allowSlashOptions: true));
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
    [InlineData("-OUT:x.exe", "--output", "x.exe")]
    [InlineData("-OUT:C:\\Temp\\test.exe", "--output", "C:\\Temp\\test.exe")]
    public void NativeValueOption_HandlesColonSeparatedValue(
        string argument,
        string expectedOption,
        string expectedValue)
    {
        Assert.Equal(
            [expectedOption, expectedValue],
            NativeCommandLine.Normalize([argument], allowSlashOptions: false));
    }

    [Theory]
    [MemberData(nameof(NativeNonEmptyValueOptions))]
    public void NativeValueOption_RejectsEmptyValue(string prefix)
    {
        Assert.Throws<ArgumentException>(
            () => NativeCommandLine.Normalize([$"-{prefix}:"], allowSlashOptions: false));
        Assert.Throws<ArgumentException>(
            () => NativeCommandLine.Normalize([$"-{prefix}=  "], allowSlashOptions: false));
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
        Assert.Equal(
            ["/unknown"],
            NativeCommandLine.Normalize(["/unknown"], allowSlashOptions: false));
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
    [InlineData("/arm", "--arm")]
    [InlineData("/ARM64Anything:value", "--arm64")]
    public void ArmOptions_UseNativeDisambiguation(string argument, string expectedOption)
    {
        Assert.Equal(
            [expectedOption],
            NativeCommandLine.Normalize([argument], allowSlashOptions: argument[0] == '/'));
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
    [InlineData("-res:file.res", false)]
    [InlineData("-RESOURCE=file.res", false)]
    [InlineData("/ReSAnything:file.res", true)]
    [InlineData("-msv=2.0", false)]
    [InlineData("-MSVersion:2.0", false)]
    [InlineData("/MSV=2.0", true)]
    public void UnsupportedNativeOption_Throws(string argument, bool allowSlashOptions)
    {
        Assert.Throws<ArgumentException>(
            () => NativeCommandLine.Normalize([argument], allowSlashOptions));
    }

    [Theory]
    [InlineData("-listing")]
    [InlineData("-LIS:anything")]
    [InlineData("/LiStInG")]
    public void NativeListingOption_IsAcceptedAsNoOp(string argument)
    {
        Assert.Empty(NativeCommandLine.Normalize([argument], allowSlashOptions: true));
    }

    [Fact]
    public void ModernAndShortOptions_ArePreserved()
    {
        string[] args = ["--output=test.exe", "-o:other.exe", "-O", "-h", "input.il"];
        Assert.Equal(args, NativeCommandLine.Normalize(args, allowSlashOptions: false));
    }

    [Theory]
    [MemberData(nameof(ModernValueOptions))]
    public void ModernValueOption_PreservesNextArgument(string option)
    {
        Assert.Equal(
            [option, "-DLL", "input.il"],
            NativeCommandLine.Normalize([option, "-DLL", "input.il"], allowSlashOptions: false));
    }

    [Theory]
    [InlineData("-I:include")]
    [InlineData("-k:key.snk")]
    [InlineData("-o:output.exe")]
    public void ModernShortValueOption_WithAttachedValueIsPreserved(string argument)
    {
        Assert.Equal(
            [argument],
            NativeCommandLine.Normalize([argument], allowSlashOptions: false));
    }

    [Theory]
    [InlineData("-g:false")]
    [InlineData("-O:false")]
    [InlineData("-q:true")]
    public void ModernShortBooleanOption_WithBooleanValueIsPreserved(string argument)
    {
        Assert.Equal(
            [argument],
            NativeCommandLine.Normalize([argument], allowSlashOptions: false));
    }

    [Theory]
    [InlineData("-g:opt", false)]
    [InlineData("-O=x.exe", false)]
    [InlineData("-O:x.exe", false)]
    [InlineData("-Ox.il", false)]
    [InlineData("-Oanything", false)]
    [InlineData("-gdebug", false)]
    [InlineData("-qvalue", false)]
    [InlineData("-Iinclude", false)]
    [InlineData("-kkey.snk", false)]
    [InlineData("-ooutput.exe", false)]
    [InlineData("-q:value", false)]
    [InlineData("-h:value", false)]
    [InlineData("/O:x.exe", true)]
    public void ShortOption_RejectsInvalidAttachedValue(string argument, bool allowSlashOptions)
    {
        Assert.Throws<ArgumentException>(
            () => NativeCommandLine.Normalize([argument], allowSlashOptions));
    }

    [Theory]
    [InlineData("-OU", false)]
    [InlineData("-unknown", false)]
    [InlineData("-x", false)]
    [InlineData("-i", false)]
    [InlineData("/unknown", true)]
    public void UnrecognizedNativeOption_Throws(string argument, bool allowSlashOptions)
    {
        Assert.Throws<ArgumentException>(
            () => NativeCommandLine.Normalize([argument], allowSlashOptions));
    }

    [Theory]
    [InlineData("input.il")]
    [InlineData("--unknown")]
    [InlineData("--")]
    [InlineData("-")]
    public void NonNativeArgument_IsPreserved(string argument)
    {
        Assert.Equal(
            [argument],
            NativeCommandLine.Normalize([argument], allowSlashOptions: false));
    }

    [Fact]
    public void EndOfOptions_PreservesAllRemainingArguments()
    {
        string[] args = ["--", "-DLL", "-unknown", "/OUT:test.dll"];
        Assert.Equal(args, NativeCommandLine.Normalize(args, allowSlashOptions: true));
    }

    [Fact]
    public void NativeOptions_AreNormalizedInPlaceAmongInputFiles()
    {
        Assert.Equal(
            ["first.il", "--dll", "--output", "test.dll", "second.il"],
            NativeCommandLine.Normalize(
                ["first.il", "-dllSuffix", "-outSuffix:test.dll", "second.il"],
                allowSlashOptions: false));
    }
}
