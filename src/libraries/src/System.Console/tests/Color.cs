// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

public class Color
{
    [Fact]
    public static void InvalidColors()
    {
        AssertExtensions.Throws<ArgumentException>(null, () => Console.BackgroundColor = (ConsoleColor)42);
        AssertExtensions.Throws<ArgumentException>(null, () => Console.ForegroundColor = (ConsoleColor)42);
    }

    [Fact]
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

            const char Esc = (char)0x1B;
            Assert.Equal(0, Encoding.UTF8.GetString(data.ToArray()).ToCharArray().Count(c => c == Esc));
            Assert.Equal("1234", Encoding.UTF8.GetString(data.ToArray()));
        });
    }

    //[Fact] // the CI system redirects stdout, so we can't easily test non-redirected behavior
    [PlatformSpecific(TestPlatforms.AnyUnix)]
    public static void NonRedirectedOutputDoesUseAnsiSequences()
    {
        // Make sure that when writing out to a UnixConsoleStream, the ANSI escape sequences are properly
        // written out.
        Helpers.RunInNonRedirectedOutput((data) =>
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ResetColor();
            const char Esc = (char)0x1B;
            Assert.Equal(3, Encoding.UTF8.GetString(data.ToArray()).ToCharArray().Count(c => c == Esc));
        });
    }
}
