// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

public class Color
{
    private const char Esc = (char)0x1B;

    [Fact]
    [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "Not supported on Browser, iOS, MacCatalyst, or tvOS.")]
    public static void InvalidColors()
    {
        AssertExtensions.Throws<ArgumentException>(null, () => Console.BackgroundColor = (ConsoleColor)42);
        AssertExtensions.Throws<ArgumentException>(null, () => Console.ForegroundColor = (ConsoleColor)42);
    }

    [Fact]
    [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "Not supported on Browser, iOS, MacCatalyst, or tvOS.")]
    public static void RoundtrippingColor()
    {
        Console.BackgroundColor = Console.BackgroundColor;
        Console.ForegroundColor = Console.ForegroundColor;

        // Changing color on Windows doesn't have effect in some testing environments
        // when there is no associated console, such as when run under a profiler like
        // our code coverage tools, so we don't assert that the change took place and
        // simply ensure that getting/setting doesn't throw.
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Browser)]
    public static void ForegroundColor_Throws_PlatformNotSupportedException()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.ForegroundColor);
        Assert.Throws<PlatformNotSupportedException>(() => Console.ForegroundColor = ConsoleColor.Red);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Browser)]
    public static void BackgroundColor_Throws_PlatformNotSupportedException()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.BackgroundColor);
        Assert.Throws<PlatformNotSupportedException>(() => Console.BackgroundColor = ConsoleColor.Red);
    }

    [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
    [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "Not supported on Browser, iOS, MacCatalyst, or tvOS.")]
    public static void RedirectedOutputDoesNotUseAnsiSequences()
    {
        // Run in a child process with redirected stdout so that no in-process
        // test framework output (e.g. xunit skip messages) can pollute the stream.
        var startInfo = new ProcessStartInfo { RedirectStandardOutput = true };
        using RemoteInvokeHandle handle = RemoteExecutor.Invoke(static () =>
        {
            Console.Write("1");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("2");
            Console.BackgroundColor = ConsoleColor.Red;
            Console.Write("3");
            Console.ResetColor();
            Console.Write("4");
        }, new RemoteInvokeOptions { StartInfo = startInfo });

        string capturedOutput = handle.Process.StandardOutput.ReadToEnd();
        Assert.DoesNotContain(Esc.ToString(), capturedOutput);
        Assert.Equal("1234", capturedOutput);
    }

    public static bool TermIsSetAndRemoteExecutorIsSupported
        => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TERM")) && RemoteExecutor.IsSupported;

    [ConditionalTheory(typeof(Color), nameof(TermIsSetAndRemoteExecutorIsSupported))]
    [PlatformSpecific(TestPlatforms.AnyUnix)]
    [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "Not supported on Browser, iOS, MacCatalyst, or tvOS.")]
    [InlineData("DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION", "1", null, null, true)]
    [InlineData("DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION", "true", null, null, true)]
    [InlineData("DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION", "tRuE", null, null, true)]
    [InlineData("DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION", "0", null, null, true)]
    [InlineData("DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION", "any-value", null, null, true)]
    [InlineData("DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION", "", null, null, false)]
    [InlineData(null, null, "FORCE_COLOR", "1", true)]
    [InlineData(null, null, "FORCE_COLOR", "true", true)]
    [InlineData(null, null, "FORCE_COLOR", "any-value", true)]
    [InlineData(null, null, "FORCE_COLOR", "", false)]
    [InlineData(null, null, "NO_COLOR", "1", false)]
    [InlineData(null, null, "NO_COLOR", "true", false)]
    [InlineData(null, null, "NO_COLOR", "any-value", false)]
    [InlineData("FORCE_COLOR", "1", "NO_COLOR", "1", true)]
    [InlineData("DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION", "1", "NO_COLOR", "1", true)]
    public static void RedirectedOutput_ColorEnvVars_RespectColorPreference(
        string? envVarName1, string? envVarValue1,
        string? envVarName2, string? envVarValue2,
        bool shouldEmitEscapes)
    {
        var psi = new ProcessStartInfo { RedirectStandardOutput = true };
        if (envVarName1 is not null)
        {
            psi.Environment[envVarName1] = envVarValue1;
        }
        if (envVarName2 is not null)
        {
            psi.Environment[envVarName2] = envVarValue2;
        }

        for (int i = 0; i < 3; i++)
        {
            Action<string> main = i =>
            {
                Console.Write("SEPARATOR");
                switch (i)
                {
                    case "0":
                        Console.ForegroundColor = ConsoleColor.Blue;
                        break;

                    case "1":
                        Console.BackgroundColor = ConsoleColor.Red;
                        break;

                    case "2":
                        Console.ResetColor();
                        break;
                }
                Console.Write("SEPARATOR");
            };

            using RemoteInvokeHandle remote = RemoteExecutor.Invoke(main, i.ToString(CultureInfo.InvariantCulture), new RemoteInvokeOptions() { StartInfo = psi });

            string stdout = remote.Process.StandardOutput.ReadToEnd();
            string[] parts = stdout.Split("SEPARATOR");
            Assert.Equal(3, parts.Length);

            Assert.Equal(shouldEmitEscapes, parts[1].Contains(Esc));
        }
    }
}
