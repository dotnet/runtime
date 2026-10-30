// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CommandLine;
using Xunit;

namespace ILDisassembler.Tests;

public class CommandLineTests
{
    public static TheoryData<string, string> NativeBooleanOptions { get; } = new()
    {
        { "ALL", "--all" },
        { "BYT", "--bytes" },
        { "CAV", "--caverbal" },
        { "CLA", "--classlist" },
        { "FOR", "--forward" },
        { "HEA", "--headers" },
        { "HTM", "--html" },
        { "LIN", "--linenum" },
        { "NOC", "--noca" },
        { "NOI", "--noil" },
        { "NOL", "--nologo" },
        { "NOT", "--raweh" },
        { "PUB", "--pubonly" },
        { "QUI", "--quiet" },
        { "QUO", "--quoteallnames" },
        { "R2R", "--r2rnativemetadata" },
        { "RAW", "--raweh" },
        { "RTF", "--rtf" },
        { "SOU", "--source" },
        { "STA", "--stats" },
        { "TOK", "--tokens" },
        { "TYP", "--typelist" },
        { "UNI", "--unicode" },
        { "UTF", "--utf8" },
    };

    public static TheoryData<string, string> NativeValueOptions { get; } = new()
    {
        { "ITE", "--item" },
        { "OUT", "--output" },
        { "VIS", "--visibility" },
    };

    public static TheoryData<string> ModernValueOptions
    {
        get
        {
            TheoryData<string> options = new();
            foreach (Option option in new IldasmRootCommand().Options)
            {
                if (option.Arity.MinimumNumberOfValues == 0)
                {
                    continue;
                }

                if (option.Name.StartsWith("-", StringComparison.Ordinal))
                {
                    options.Add(option.Name);
                }

                foreach (string alias in option.Aliases)
                {
                    if (alias.StartsWith("-", StringComparison.Ordinal))
                    {
                        options.Add(alias);
                    }
                }
            }

            return options;
        }
    }

    [Theory]
    [MemberData(nameof(NativeBooleanOptions))]
    public void NativeBooleanOption_IsCaseInsensitiveAndUsesThreeCharacterPrefix(
        string prefix,
        string expectedOption)
    {
        Assert.Equal(
            [expectedOption],
            NativeCommandLine.Normalize([$"-{prefix.ToLowerInvariant()}Suffix"], allowSlashOptions: false));
        Assert.Equal(
            [expectedOption],
            NativeCommandLine.Normalize([$"/{prefix.ToLowerInvariant()}Suffix"], allowSlashOptions: true));
    }

    [Theory]
    [MemberData(nameof(NativeBooleanOptions))]
    public void NativeBooleanOption_IgnoresAttachedValue(string prefix, string expectedOption)
    {
        Assert.Equal([expectedOption], NativeCommandLine.Normalize([$"-{prefix}:false"], allowSlashOptions: false));
        Assert.Equal([expectedOption], NativeCommandLine.Normalize([$"-{prefix}="], allowSlashOptions: false));
        Assert.Equal([expectedOption], NativeCommandLine.Normalize([$"/{prefix}:ignored"], allowSlashOptions: true));
    }

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
    public void NativeValueOption_RequiresNonEmptyValue(string prefix, string _)
    {
        Assert.Throws<ArgumentException>(
            () => NativeCommandLine.Normalize([$"-{prefix}Suffix"], allowSlashOptions: false));
        Assert.Throws<ArgumentException>(
            () => NativeCommandLine.Normalize([$"-{prefix}:"], allowSlashOptions: false));
        Assert.Throws<ArgumentException>(
            () => NativeCommandLine.Normalize([$"-{prefix}=  "], allowSlashOptions: false));
    }

    [Theory]
    [InlineData("-out:  test.il", "--output", "test.il")]
    [InlineData("-OUT:C:\\Temp\\test.il", "--output", "C:\\Temp\\test.il")]
    [InlineData("-item=Namespace.Type::Method(int32)", "--item", "Namespace.Type::Method(int32)")]
    [InlineData("-vis=PUB+FAM", "--visibility", "PUB+FAM")]
    public void NativeValueOption_PreservesValue(string argument, string expectedOption, string expectedValue)
    {
        Assert.Equal(
            [expectedOption, expectedValue],
            NativeCommandLine.Normalize([argument], allowSlashOptions: false));
    }

    [Theory]
    [InlineData("-out=con")]
    [InlineData("-OUTPUT:CON")]
    public void NativeOutputToConsole_IsDropped(string argument)
    {
        Assert.Empty(NativeCommandLine.Normalize([argument], allowSlashOptions: false));
    }

    [Theory]
    [InlineData("-metadata", new[] { "--metadata" })]
    [InlineData("-MET=RAW", new[] { "--metadata", "--metadata-filter", "RAW" })]
    [InlineData("-metadata:  heaps", new[] { "--metadata", "--metadata-filter", "heaps" })]
    public void NativeMetadataOption_HasOptionalFilter(string argument, string[] expected)
    {
        Assert.Equal(expected, NativeCommandLine.Normalize([argument], allowSlashOptions: false));
    }

    [Theory]
    [InlineData("-met:")]
    [InlineData("-metadata=  ")]
    public void NativeMetadataOption_RejectsEmptyFilter(string argument)
    {
        Assert.Throws<ArgumentException>(() => NativeCommandLine.Normalize([argument], allowSlashOptions: false));
    }

    [Fact]
    public void MetadataFilters_DoNotConsumeInputFile()
    {
        IldasmRootCommand command = new();
        ParseResult result = command.Parse(
            NativeCommandLine.Normalize(["-MET=RAW", "-MET=HEX", "input.dll"], allowSlashOptions: false));

        Assert.Empty(result.Errors);
        Assert.True(result.GetValue(command.Metadata));
        Assert.Equal(["RAW", "HEX"], result.GetValue(command.MetadataFilters)!);
        Assert.Equal("input.dll", result.GetValue(command.InputFilePath));
    }

    [Theory]
    [InlineData("-adv")]
    [InlineData("/ADVANCED")]
    [InlineData("-pretty")]
    public void NativeNoOpOption_IsAccepted(string argument)
    {
        Assert.Empty(NativeCommandLine.Normalize([argument], allowSlashOptions: true));
    }

    [Theory]
    [InlineData("-decompile")]
    [InlineData("-OBJECTFILE=a.obj")]
    [InlineData("/project")]
    [InlineData("-refs")]
    [InlineData("-summary")]
    public void UnsupportedNativeOption_Throws(string argument)
    {
        Assert.Throws<ArgumentException>(() => NativeCommandLine.Normalize([argument], allowSlashOptions: true));
    }

    [Theory]
    [InlineData("-OU", false)]
    [InlineData("-unknown", false)]
    [InlineData("-x", false)]
    [InlineData("/unknown", true)]
    public void UnrecognizedNativeOption_Throws(string argument, bool allowSlashOptions)
    {
        Assert.Throws<ArgumentException>(() => NativeCommandLine.Normalize([argument], allowSlashOptions));
    }

    [Theory]
    [InlineData("/OuT:test.il", "--output", "test.il")]
    [InlineData("/tOkEnS", "--tokens", null)]
    [InlineData("/?", "--help", null)]
    public void SlashOption_IsNormalizedWhenEnabled(string argument, string expectedOption, string? expectedValue)
    {
        string[] expected = expectedValue is null ? [expectedOption] : [expectedOption, expectedValue];

        Assert.Equal(expected, NativeCommandLine.Normalize([argument], allowSlashOptions: true));
    }

    [Fact]
    public void SlashOption_IsPreservedWhenDisabled()
    {
        Assert.Equal(["/tmp/input.dll"], NativeCommandLine.Normalize(["/tmp/input.dll"], allowSlashOptions: false));
        Assert.Equal(["/unknown"], NativeCommandLine.Normalize(["/unknown"], allowSlashOptions: false));
    }

    [Fact]
    public void SlashOption_DefaultMatchesPlatform()
    {
        string[] expected = OperatingSystem.IsWindows() ? ["--tokens"] : ["/tok"];

        Assert.Equal(expected, NativeCommandLine.Normalize(["/tok"]));
    }

    [Fact]
    public void ModernAndShortOptions_ArePreserved()
    {
        string[] args = ["--output=test.il", "-o:other.il", "-q", "-h", "--metadata-filter", "RAW", "input.dll"];
        Assert.Equal(args, NativeCommandLine.Normalize(args, allowSlashOptions: false));
    }

    [Theory]
    [MemberData(nameof(ModernValueOptions))]
    public void ModernValueOption_PreservesNextArgument(string option)
    {
        Assert.Equal(
            [option, "-TOK", "input.dll"],
            NativeCommandLine.Normalize([option, "-TOK", "input.dll"], allowSlashOptions: false));
    }

    [Theory]
    [InlineData("-ooutput.il")]
    [InlineData("-qvalue")]
    [InlineData("-q:value")]
    [InlineData("-h:value")]
    public void ShortOption_RejectsInvalidAttachedValue(string argument)
    {
        Assert.Throws<ArgumentException>(() => NativeCommandLine.Normalize([argument], allowSlashOptions: false));
    }

    [Theory]
    [InlineData("input.dll")]
    [InlineData("--unknown")]
    [InlineData("--")]
    [InlineData("-")]
    public void NonNativeArgument_IsPreserved(string argument)
    {
        Assert.Equal([argument], NativeCommandLine.Normalize([argument], allowSlashOptions: false));
    }

    [Fact]
    public void ArgumentsAfterDoubleDash_ArePreserved()
    {
        Assert.Equal(
            ["--tokens", "--", "-BYT", "/OUT=x.il"],
            NativeCommandLine.Normalize(["-TOK", "--", "-BYT", "/OUT=x.il"], allowSlashOptions: true));
    }

    [Fact]
    public void NativeCommandLine_ParsesWithoutErrors()
    {
        IldasmRootCommand command = new();
        ParseResult result = command.Parse(
            NativeCommandLine.Normalize(
                ["-tok", "-byt", "input.dll", "-out=input.il", "-vis=PUB+FAM", "-ITEM=N.T::M"],
                allowSlashOptions: false));

        Assert.Empty(result.Errors);
        Assert.True(result.GetValue(command.ShowTokens));
        Assert.True(result.GetValue(command.ShowBytes));
        Assert.Equal("input.dll", result.GetValue(command.InputFilePath));
        Assert.Equal("input.il", result.GetValue(command.OutputFilePath));
        Assert.Equal("PUB+FAM", result.GetValue(command.Visibility));
        Assert.Equal("N.T::M", result.GetValue(command.Item));
    }
}
