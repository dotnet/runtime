// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

[SkipOnPlatform(TestPlatforms.Android | TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "Not supported on Android, Browser, iOS, MacCatalyst, or tvOS.")]
public class TermInfoTests
{
    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]  // Tests TermInfo
    public void VerifyInstalledTermInfosParse()
    {
        bool foundAtLeastOne = false;

        foreach (string location in TermInfo.DatabaseFactory.s_terminfoLocations)
        {
            if (!Directory.Exists(location))
                continue;

            foreach (string term in Directory.EnumerateFiles(location, "*", SearchOption.AllDirectories))
            {
                if (term.ToUpper().Contains("README")) continue;
                foundAtLeastOne = true;

                TerminalFormatStrings info = new(TermInfo.DatabaseFactory.ReadDatabase(Path.GetFileName(term)));

                if (!string.IsNullOrEmpty(info.Foreground))
                {
                    Assert.NotEmpty(TermInfo.ParameterizedStrings.Evaluate(info.Foreground, 0 /* irrelevant, just an integer to put into the formatting*/));
                }

                if (!string.IsNullOrEmpty(info.Background))
                {
                    Assert.NotEmpty(TermInfo.ParameterizedStrings.Evaluate(info.Background, 0 /* irrelevant, just an integer to put into the formatting*/));
                }
            }
        }

        Assert.True(foundAtLeastOne, "Didn't find any terminfo files");
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)] // Tests TermInfo
    public void VerifyTermInfoSupportsNewAndLegacyNcurses()
    {
        Assert.NotNull(TermInfo.DatabaseFactory.ReadDatabase("xterm", "ncursesFormats")); // This will throw InvalidOperationException in case we don't support the legacy format
        Assert.NotNull(TermInfo.DatabaseFactory.ReadDatabase("screen-256color", "ncursesFormats")); // This will throw InvalidOperationException if we can't parse the new format
    }

    [Theory]
    [PlatformSpecific(TestPlatforms.AnyUnix)]  // Tests TermInfo
    [InlineData("xterm-256color", "\u001B\u005B\u00330m", "\u001B\u005B\u00340m", 0)]
    [InlineData("xterm-256color", "\u001B\u005B\u00331m", "\u001B\u005B\u00341m", 1)]
    [InlineData("xterm-256color", "\u001B\u005B90m", "\u001B\u005B100m", 8)]
    [InlineData("screen", "\u001B\u005B\u00330m", "\u001B\u005B\u00340m", 0)]
    [InlineData("screen", "\u001B\u005B\u00332m", "\u001B\u005B\u00342m", 2)]
    [InlineData("screen", "\u001B\u005B\u00339m", "\u001B\u005B\u00349m", 9)]
    [InlineData("Eterm", "\u001B\u005B\u00330m", "\u001B\u005B\u00340m", 0)]
    [InlineData("Eterm", "\u001B\u005B\u00333m", "\u001B\u005B\u00343m", 3)]
    [InlineData("Eterm", "\u001B\u005B\u003310m", "\u001B\u005B\u003410m", 10)]
    [InlineData("wsvt25", "\u001B\u005B\u00330m", "\u001B\u005B\u00340m", 0)]
    [InlineData("wsvt25", "\u001B\u005B\u00334m", "\u001B\u005B\u00344m", 4)]
    [InlineData("wsvt25", "\u001B\u005B\u003311m", "\u001B\u005B\u003411m", 11)]
    [InlineData("mach-color", "\u001B\u005B\u00330m", "\u001B\u005B\u00340m", 0)]
    [InlineData("mach-color", "\u001B\u005B\u00335m", "\u001B\u005B\u00345m", 5)]
    [InlineData("mach-color", "\u001B\u005B\u003312m", "\u001B\u005B\u003412m", 12)]
    public void TermInfoVerification(string termToTest, string expectedForeground, string expectedBackground, int colorValue)
    {
        TermInfo.Database db = TermInfo.DatabaseFactory.ReadDatabase(termToTest);
        if (db != null)
        {
            TerminalFormatStrings info = new(db);
            Assert.Equal(expectedForeground, TermInfo.ParameterizedStrings.Evaluate(info.Foreground, colorValue));
            Assert.Equal(expectedBackground, TermInfo.ParameterizedStrings.Evaluate(info.Background, colorValue));
            Assert.InRange(info.MaxColors, 1, int.MaxValue);
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.OSX)]  // The file being tested is available by default only on OSX
    public void EmuTermInfoDoesntBreakParser()
    {
        // This file (available by default on OS X) is called out specifically since it contains a format where it has %i
        // but only one variable instead of two. Make sure we don't break in this case
        TermInfoVerification("emu", "\u001Br1;", "\u001Bs1;", 0);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]  // Tests TermInfo
    public void TryingToLoadTermThatDoesNotExistDoesNotThrow()
    {
        const string NonexistentTerm = "foobar____";
        TermInfo.Database db = TermInfo.DatabaseFactory.ReadDatabase(NonexistentTerm);
        TerminalFormatStrings info = new(db);
        Assert.Null(db);
        Assert.Null(info.Background);
        Assert.Null(info.Foreground);
        Assert.Equal(0, info.MaxColors);
        Assert.Null(info.Reset);
    }
}
