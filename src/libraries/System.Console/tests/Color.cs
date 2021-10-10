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
        // simple ensure that getting/setting doesn't throw.
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

    [Fact]
    [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "Not supported on Browser, iOS, MacCatalyst, or tvOS.")]
    public static void RedirectedOutputDoesNotUseAnsiSequences()
    {
        // Make sure that redirecting to a memory stream causes Console not to write out the ANSI sequences

        Helpers.RunInRedirectedOutput((data) =>
        {
            Console.Write('1');
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write('2');
            Console.BackgroundColor = ConsoleColor.Red;
            Console.Write('3');
            Console.ResetColor();
            Console.Write('4');

            Assert.Equal(0, Encoding.UTF8.GetString(data.ToArray()).ToCharArray().Count(c => c == Esc));
            Assert.Equal("1234", Encoding.UTF8.GetString(data.ToArray()));
        });
    }

    public static bool TermIsSet => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TERM"));

    [ConditionalTheory(nameof(TermIsSet))]
    [PlatformSpecific(TestPlatforms.AnyUnix)]
    [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "Not supported on Browser, iOS, MacCatalyst, or tvOS.")]
    [InlineData(null)]
    [InlineData("1")]
    [InlineData("true")]
    [InlineData("tRuE")]
    [InlineData("0")]
    [InlineData("false")]
    public static void RedirectedOutput_EnvVarSet_EmitsAnsiCodes(string envVar)
    {
        var psi = new ProcessStartInfo { RedirectStandardOutput = true };
        psi.Environment["DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION"] = envVar;

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

            bool expectedEscapes = envVar is not null && (envVar == "1" || envVar.Equals("true", StringComparison.OrdinalIgnoreCase));

            string stdout = remote.Process.StandardOutput.ReadToEnd();
            string[] parts = stdout.Split("SEPARATOR");
            Assert.Equal(3, parts.Length);

            Assert.Equal(expectedEscapes, parts[1].Contains(Esc));
        }
    }
}
