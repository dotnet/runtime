// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Tests;
using Xunit;

[SkipOnPlatform(TestPlatforms.Android | TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "Not supported on Android, Browser, iOS, MacCatalyst, or tvOS.")]
public class TermInfoTests
{
    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]  // Tests TermInfo
    public void VerifyInstalledTermInfosParse()
    {
        bool foundAtLeastOne = false;
        List<Exception>? verifyExceptions = null;

        HashSet<string?> locations = new HashSet<string>(TermInfo.DatabaseFactory.SystemTermInfoLocations);
        locations.Add(TermInfo.DatabaseFactory.HomeTermInfoLocation);
        locations.Add(TermInfo.DatabaseFactory.EnvVarTermInfoLocation);

        foreach (string location in locations)
        {
            if (!Directory.Exists(location))
                continue;

            foreach (string termFile in Directory.EnumerateFiles(location, "*", SearchOption.AllDirectories))
            {
                try
                {
                    if (termFile.ToUpper().Contains("README")) continue;
                    foundAtLeastOne = true;

                    string term = Path.GetFileName(termFile);
                    TermInfo.Database db = TermInfo.DatabaseFactory.ReadDatabase(term, location);
                    Assert.NotNull(db);
                    TerminalFormatStrings info = new(db);

                    if (!string.IsNullOrEmpty(info.Foreground))
                    {
                        Assert.NotEmpty(TermInfo.ParameterizedStrings.Evaluate(info.Foreground, 0 /* irrelevant, just an integer to put into the formatting*/));
                    }

                    if (!string.IsNullOrEmpty(info.Background))
                    {
                        Assert.NotEmpty(TermInfo.ParameterizedStrings.Evaluate(info.Background, 0 /* irrelevant, just an integer to put into the formatting*/));
                    }
                }
                catch (Exception ex)
                {
                    verifyExceptions ??= new();
                    verifyExceptions.Add(new Exception($"Exception while verifying '{termFile}'", ex));
                }
            }
        }

        Assert.True(foundAtLeastOne, "Didn't find any terminfo files");

        if (verifyExceptions is not null)
        {
            throw new AggregateException(verifyExceptions);
        }
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
    [InlineData("xterm-256color", "\e\u005B\u00330m", "\e\u005B\u00340m", 0)]
    [InlineData("xterm-256color", "\e\u005B\u00331m", "\e\u005B\u00341m", 1)]
    [InlineData("xterm-256color", "\e\u005B90m", "\e\u005B100m", 8)]
    [InlineData("screen", "\e\u005B\u00330m", "\e\u005B\u00340m", 0)]
    [InlineData("screen", "\e\u005B\u00332m", "\e\u005B\u00342m", 2)]
    [InlineData("screen", "\e\u005B\u00339m", "\e\u005B\u00349m", 9)]
    [InlineData("Eterm", "\e\u005B\u00330m", "\e\u005B\u00340m", 0)]
    [InlineData("Eterm", "\e\u005B\u00333m", "\e\u005B\u00343m", 3)]
    [InlineData("Eterm", "\e\u005B\u003310m", "\e\u005B\u003410m", 10)]
    [InlineData("wsvt25", "\e\u005B\u00330m", "\e\u005B\u00340m", 0)]
    [InlineData("wsvt25", "\e\u005B\u00334m", "\e\u005B\u00344m", 4)]
    [InlineData("wsvt25", "\e\u005B\u003311m", "\e\u005B\u003411m", 11)]
    [InlineData("mach-color", "\e\u005B\u00330m", "\e\u005B\u00340m", 0)]
    [InlineData("mach-color", "\e\u005B\u00335m", "\e\u005B\u00345m", 5)]
    [InlineData("mach-color", "\e\u005B\u003312m", "\e\u005B\u003412m", 12)]
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
    [PlatformSpecific(TestPlatforms.AnyUnix)] // Tests TermInfo
    public void TermInfoClearIncludesE3WhenExpected()
    {
        // XTerm defines E3 for clearing scrollback buffer and tmux does not.
        // This can't be added to TermInfoVerification because xterm-256color sometimes has E3 defined (e.g. on Ubuntu but not macOS)
        Assert.Equal("\e[H\e[2J\e[3J", new XTermData().TerminalDb.Clear);
        Assert.Equal("\e[H\e[J", new TmuxData().TerminalDb.Clear);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.OSX)]  // The file being tested is available by default only on OSX
    public void EmuTermInfoDoesntBreakParser()
    {
        // This file (available by default on OS X) is called out specifically since it contains a format where it has %i
        // but only one variable instead of two. Make sure we don't break in this case
        TermInfoVerification("emu", "\er1;", "\es1;", 0);
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
