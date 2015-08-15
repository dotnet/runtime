// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

public class TermInfo
{
    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public void VerifyInstalledTermInfosParse()
    {
        string[] locations = GetFieldValueOnObject<string[]>("_terminfoLocations", null, typeof(Console).GetTypeInfo().Assembly.GetType("System.ConsolePal+TermInfo+Database"));
        foreach (string location in locations)
        {
            if (!Directory.Exists(location))
                continue;

            foreach (string term in Directory.EnumerateFiles(location, "*", SearchOption.AllDirectories))
            {
                if (term.ToUpper().Contains("README")) continue;
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
    }

    // Note that we cannot use a Theory here due using unprintable characters and a bug in xunit where the runner errors
    // out while trying to generate a report and printing the character (bug #380 on xunit)
    private void TermInfoVerification(string termToTest, string expectedForeground, string expectedBackground, int colorValue)
    {
        object info = CreateTermColorInfo(ReadTermInfoDatabase(termToTest));
        Assert.Equal(expectedForeground, EvaluateParameterizedStrings(GetForegroundFormat(info), colorValue));
        Assert.Equal(expectedBackground, EvaluateParameterizedStrings(GetBackgroundFormat(info), colorValue));
    }

    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public void Xterm256ColorDefault()
    {
        TermInfoVerification("xterm-256color", "\u001B\u005B\u00330m", "\u001B\u005B\u00340m", 0);
    }

    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public void Xterm256ColorLight()
    {
        TermInfoVerification("xterm-256color", "\u001B\u005B\u00331m", "\u001B\u005B\u00341m", 1);
    }

    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public void Xterm256ColorDark()
    {
        TermInfoVerification("xterm-256color", "\u001B\u005B90m", "\u001B\u005B100m", 8);
    }

    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public void ScreenTermDefault()
    {
        TermInfoVerification("screen", "\u001B\u005B\u00330m", "\u001B\u005B\u00340m", 0);
    }

    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public void ScreenTermLight()
    {
        TermInfoVerification("screen", "\u001B\u005B\u00332m", "\u001B\u005B\u00342m", 2);
    }

    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public void ScreenTermDark()
    {
        TermInfoVerification("screen", "\u001B\u005B\u00339m", "\u001B\u005B\u00349m", 9);
    }

    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public void EtermDefault()
    {
        TermInfoVerification("Eterm", "\u001B\u005B\u00330m", "\u001B\u005B\u00340m", 0);
    }

    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public void EtermLight()
    {
        TermInfoVerification("Eterm", "\u001B\u005B\u00333m", "\u001B\u005B\u00343m", 3);
    }

    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public void EtermDark()
    {
        TermInfoVerification("Eterm", "\u001B\u005B\u003310m", "\u001B\u005B\u003410m", 10);
    }

    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public void Wsvt25TermDefault()
    {
        TermInfoVerification("wsvt25", "\u001B\u005B\u00330m", "\u001B\u005B\u00340m", 0);
    }

    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public void Wsvt25TermLight()
    {
        TermInfoVerification("wsvt25", "\u001B\u005B\u00334m", "\u001B\u005B\u00344m", 4);
    }

    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public void Wsvt25TermDark()
    {
        TermInfoVerification("wsvt25", "\u001B\u005B\u003311m", "\u001B\u005B\u003411m", 11);
    }

    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public void MachColorTermDefault()
    {
        TermInfoVerification("mach-color", "\u001B\u005B\u00330m", "\u001B\u005B\u00340m", 0);
    }

    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public void MachColorTermLight()
    {
        TermInfoVerification("mach-color", "\u001B\u005B\u00335m", "\u001B\u005B\u00345m", 5);
    }

    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public void MachColorTermDark()
    {
        TermInfoVerification("mach-color", "\u001B\u005B\u003312m", "\u001B\u005B\u003412m", 12);
    }

    [Fact]
    [PlatformSpecific(PlatformID.OSX)]
    public void EmuTermInfoDoesntBreakParser()
    {
        // This file (available by default on OS X) is called out specifically since it contains a format where it has %i
        // but only one variable instead of two. Make sure we don't break in this case
        TermInfoVerification("emu", "\u001Br1", "\u001Bs1", 0);
    }

    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public void TryingToLoadTermThatDoesNotExistDoesNotThrow()
    {
        object db = ReadTermInfoDatabase("foobar____");
        object info = CreateTermColorInfo(db);
        Assert.Null(db);
        Assert.NotNull(GetBackgroundFormat(info));
        Assert.NotNull(GetForegroundFormat(info));
        Assert.NotNull(GetMaxColors(info));
        Assert.NotNull(GetResetFormat(info));
    }

    private object ReadTermInfoDatabase(string term)
    {
        MethodInfo readDbMethod = typeof(Console).GetTypeInfo().Assembly.GetType("System.ConsolePal+TermInfo+Database").GetTypeInfo().GetDeclaredMethods("ReadDatabase").Where(m => m.GetParameters().Count() == 1).Single();
        return readDbMethod.Invoke(null, new object[] { term });
    }

    private object CreateTermColorInfo(object db)
    {
        return typeof(Console).GetTypeInfo().Assembly.GetType("System.ConsolePal+TerminalColorInfo").GetTypeInfo().DeclaredConstructors
                              .Where(c => c.GetParameters().Count() == 1).Single().Invoke(new object[] { db });
    }

    private string GetForegroundFormat(object colorInfo)
    {
        return GetFieldValueOnObject<string>("ForegroundFormat", colorInfo, typeof(Console).GetTypeInfo().Assembly.GetType("System.ConsolePal+TerminalColorInfo"));
    }

    private string GetBackgroundFormat(object colorInfo)
    {
        return GetFieldValueOnObject<string>("BackgroundFormat", colorInfo, typeof(Console).GetTypeInfo().Assembly.GetType("System.ConsolePal+TerminalColorInfo"));
    }

    private int GetMaxColors(object colorInfo)
    {
        return GetFieldValueOnObject<int>("MaxColors", colorInfo, typeof(Console).GetTypeInfo().Assembly.GetType("System.ConsolePal+TerminalColorInfo"));
    }

    private string GetResetFormat(object colorInfo)
    {
        return GetFieldValueOnObject<string>("ResetFormat", colorInfo, typeof(Console).GetTypeInfo().Assembly.GetType("System.ConsolePal+TerminalColorInfo"));
    }

    private T GetFieldValueOnObject<T>(string name, object instance, Type baseType)
    {
        return (T)baseType.GetTypeInfo().GetDeclaredField(name).GetValue(instance);
    }

    private object CreateFormatParam(object o)
    {
        Assert.True((o.GetType() == typeof(Int32)) || (o.GetType() == typeof(string)));

        TypeInfo ti = typeof(Console).GetTypeInfo().Assembly.GetType("System.ConsolePal+TermInfo+ParameterizedStrings+FormatParam").GetTypeInfo();
        ConstructorInfo ci = null;

        foreach (ConstructorInfo c in ti.DeclaredConstructors)
        {
            Type paramType = c.GetParameters().ElementAt(0).ParameterType;
            if ((paramType == typeof(string)) && (o.GetType() == typeof(string)))
            {
                ci = c;
                break;
            }
            else if ((paramType == typeof(Int32)) && (o.GetType() == typeof(Int32)))
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
        Type formatArrayType = typeof(Console).GetTypeInfo().Assembly.GetType("System.ConsolePal+TermInfo+ParameterizedStrings+FormatParam").MakeArrayType();
        MethodInfo mi = typeof(Console).GetTypeInfo().Assembly.GetType("System.ConsolePal+TermInfo+ParameterizedStrings").GetTypeInfo().GetDeclaredMethod("Evaluate");

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
