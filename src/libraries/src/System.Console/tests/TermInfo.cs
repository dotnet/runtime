// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

public class TermInfo
{
    // Names of internal members accessed via reflection
    private const string TerminfoType = "System.TermInfo";
    private const string TerminfoDatabaseType = TerminfoType + "+Database";
    private const string ParameterizedStringsType = TerminfoType + "+ParameterizedStrings";
    private const string FormatParamType = ParameterizedStringsType + "+FormatParam";
    private const string TerminalFormatStringsType = "System.ConsolePal+TerminalFormatStrings";
    private const string ReadDatabaseMethod = "ReadDatabase";
    private const string EvaluateMethod = "Evaluate";
    private const string ForegroundFormatField = "Foreground";
    private const string BackgroundFormatField = "Background";
    private const string ResetFormatField = "Reset";
    private const string MaxColorsField = "MaxColors";
    private const string TerminfoLocationsField = "_terminfoLocations";

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]  // Tests TermInfo
    public void VerifyInstalledTermInfosParse()
    {
        bool foundAtLeastOne = false;

        string[] locations = GetFieldValueOnObject<string[]>(TerminfoLocationsField, null, typeof(Console).GetTypeInfo().Assembly.GetType(TerminfoDatabaseType));
        foreach (string location in locations)
        {
            if (!Directory.Exists(location))
                continue;

            foreach (string term in Directory.EnumerateFiles(location, "*", SearchOption.AllDirectories))
            {
                if (term.ToUpper().Contains("README")) continue;
                foundAtLeastOne = true;

                object info = CreateTermColorInfo(ReadTermInfoDatabase(Path.GetFileName(term)));

                if (!string.IsNullOrEmpty(GetForegroundFormat(info)))
                {
                    Assert.NotEmpty(EvaluateParameterizedStrings(GetForegroundFormat(info), 0 /* irrelevant, just an integer to put into the formatting*/));
                }

                if (!string.IsNullOrEmpty(GetBackgroundFormat(info)))
                {
                    Assert.NotEmpty(EvaluateParameterizedStrings(GetBackgroundFormat(info), 0 /* irrelevant, just an integer to put into the formatting*/));
                }
            }
        }

        Assert.True(foundAtLeastOne, "Didn't find any terminfo files");
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)] // Tests TermInfo
    public void VerifyTermInfoSupportsNewAndLegacyNcurses()
    {
        MethodInfo readDbMethod = typeof(Console).GetTypeInfo().Assembly.GetType(TerminfoDatabaseType).GetTypeInfo().GetDeclaredMethods(ReadDatabaseMethod).Where(m => m.GetParameters().Count() == 2).Single();
        readDbMethod.Invoke(null, new object[] { "xterm", "ncursesFormats" }); // This will throw InvalidOperationException in case we don't support the legacy format
        readDbMethod.Invoke(null, new object[] { "screen-256color", "ncursesFormats" }); // This will throw InvalidOperationException if we can't parse the new format
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
        object db = ReadTermInfoDatabase(termToTest);
        if (db != null)
        {
            object info = CreateTermColorInfo(db);
            Assert.Equal(expectedForeground, EvaluateParameterizedStrings(GetForegroundFormat(info), colorValue));
            Assert.Equal(expectedBackground, EvaluateParameterizedStrings(GetBackgroundFormat(info), colorValue));
            Assert.InRange(GetMaxColors(info), 1, int.MaxValue);
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
        object db = ReadTermInfoDatabase(NonexistentTerm);
        object info = CreateTermColorInfo(db);
        Assert.Null(db);
        Assert.Null(GetBackgroundFormat(info));
        Assert.Null(GetForegroundFormat(info));
        Assert.Equal(0, GetMaxColors(info));
        Assert.Null(GetResetFormat(info));
    }

    private object ReadTermInfoDatabase(string term)
    {
        MethodInfo readDbMethod = typeof(Console).GetTypeInfo().Assembly.GetType(TerminfoDatabaseType).GetTypeInfo().GetDeclaredMethods(ReadDatabaseMethod).Where(m => m.GetParameters().Count() == 1).Single();
        return readDbMethod.Invoke(null, new object[] { term });
    }

    private object CreateTermColorInfo(object db)
    {
        return typeof(Console).GetTypeInfo().Assembly.GetType(TerminalFormatStringsType).GetTypeInfo().DeclaredConstructors
                              .Where(c => c.GetParameters().Count() == 1).Single().Invoke(new object[] { db });
    }

    private string GetForegroundFormat(object colorInfo)
    {
        return GetFieldValueOnObject<string>(ForegroundFormatField, colorInfo, typeof(Console).GetTypeInfo().Assembly.GetType(TerminalFormatStringsType));
    }

    private string GetBackgroundFormat(object colorInfo)
    {
        return GetFieldValueOnObject<string>(BackgroundFormatField, colorInfo, typeof(Console).GetTypeInfo().Assembly.GetType(TerminalFormatStringsType));
    }

    private int GetMaxColors(object colorInfo)
    {
        return GetFieldValueOnObject<int>(MaxColorsField, colorInfo, typeof(Console).GetTypeInfo().Assembly.GetType(TerminalFormatStringsType));
    }

    private string GetResetFormat(object colorInfo)
    {
        return GetFieldValueOnObject<string>(ResetFormatField, colorInfo, typeof(Console).GetTypeInfo().Assembly.GetType(TerminalFormatStringsType));
    }

    private T GetFieldValueOnObject<T>(string name, object instance, Type baseType)
    {
        return (T)baseType.GetTypeInfo().GetDeclaredField(name).GetValue(instance);
    }

    private object CreateFormatParam(object o)
    {
        Assert.True((o.GetType() == typeof(int)) || (o.GetType() == typeof(string)));

        TypeInfo ti = typeof(Console).GetTypeInfo().Assembly.GetType(FormatParamType).GetTypeInfo();
        ConstructorInfo ci = null;

        foreach (ConstructorInfo c in ti.DeclaredConstructors)
        {
            Type paramType = c.GetParameters().ElementAt(0).ParameterType;
            if ((paramType == typeof(string)) && (o.GetType() == typeof(string)))
            {
                ci = c;
                break;
            }
            else if ((paramType == typeof(int)) && (o.GetType() == typeof(int)))
            {
                ci = c;
                break;
            }
        }

        Assert.True(ci != null);
        return ci.Invoke(new object[] { o });
    }

    private string EvaluateParameterizedStrings(string format, params object[] parameters)
    {
        Type formatArrayType = typeof(Console).GetTypeInfo().Assembly.GetType(FormatParamType).MakeArrayType();
        MethodInfo mi = typeof(Console).GetTypeInfo().Assembly.GetType(ParameterizedStringsType).GetTypeInfo()
            .GetDeclaredMethods(EvaluateMethod).First(m => m.GetParameters()[1].ParameterType.IsArray);

        // Create individual FormatParams
        object[] stringParams = new object[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
            stringParams[i] = CreateFormatParam(parameters[i]);

        // Create the array of format params and then put the individual params in their location
        Array typeArray = (Array)Activator.CreateInstance(formatArrayType, new object[] { stringParams.Length });
        for (int i = 0; i < parameters.Length; i++)
            typeArray.SetValue(stringParams[i], i);

        // Setup the params to evaluate
        object[] evalParams = new object[2];
        evalParams[0] = format;
        evalParams[1] = typeArray;

        return (string)mi.Invoke(null, evalParams);
    }
}
