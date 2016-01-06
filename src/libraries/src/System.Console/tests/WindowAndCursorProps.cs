// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;
using System.Runtime.InteropServices;

public class WindowAndCursorProps
{
    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public static void BufferSize_SettingNotSupported()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.BufferWidth = 1);
        Assert.Throws<PlatformNotSupportedException>(() => Console.BufferHeight = 1);
    }

    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public static void BufferSize_GettingSameAsWindowSize()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.BufferWidth = 1);
        Assert.Throws<PlatformNotSupportedException>(() => Console.BufferHeight = 1);

        Assert.Equal(Console.WindowWidth, Console.BufferWidth);
        Assert.Equal(Console.WindowHeight, Console.BufferHeight);
    }

    [Fact]
    public static void WindowWidth_WindowHeight_InvalidSize()
    {
        Assert.Throws<ArgumentOutOfRangeException>("value", () => Console.WindowWidth = 0);
        Assert.Throws<ArgumentOutOfRangeException>("value", () => Console.WindowHeight = 0);
    }

    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public static void WindowWidth()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.WindowWidth = 100);

        // Validate that Console.WindowWidth returns some value in a non-redirected o/p.
        Helpers.RunInNonRedirectedOutput((data) => Console.WriteLine(Console.WindowWidth));
        Helpers.RunInRedirectedOutput((data) => Console.WriteLine(Console.WindowWidth));
    }

    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public static void WindowHeight()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.WindowHeight = 100);

        // Validate that Console.WindowHeight returns some value in a non-redirected o/p.
        Helpers.RunInNonRedirectedOutput((data) => Console.WriteLine(Console.WindowHeight));
        Helpers.RunInRedirectedOutput((data) => Console.WriteLine(Console.WindowHeight));
    }

    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public static void WindowLeftTop()
    {
        Assert.Equal(0, Console.WindowLeft);
        Assert.Equal(0, Console.WindowTop);
        Assert.Throws<PlatformNotSupportedException>(() => Console.WindowLeft = 0);
        Assert.Throws<PlatformNotSupportedException>(() => Console.WindowTop = 0);
    }

    //[Fact] //CI system makes it difficult to run things in a non-redirected environments.
    [PlatformSpecific(PlatformID.AnyUnix)]
    public static void NonRedirectedCursorVisible()
    {
        // Validate that Console.CursorVisible adds something to the stream when in a non-redirected environment.
        Helpers.RunInNonRedirectedOutput((data) => { Console.CursorVisible = false; Assert.True(data.ToArray().Length > 0); });
        Helpers.RunInNonRedirectedOutput((data) => { Console.CursorVisible = true; Assert.True(data.ToArray().Length > 0); });
    }

    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public static void CursorVisible()
    {
        Assert.Throws<PlatformNotSupportedException>(() => { bool unused = Console.CursorVisible; });

        // Validate that the Console.CursorVisible does nothing in a redirected stream.
        Helpers.RunInRedirectedOutput((data) => { Console.CursorVisible = false; Assert.Equal(0, data.ToArray().Length); });
        Helpers.RunInRedirectedOutput((data) => { Console.CursorVisible = true; Assert.Equal(0, data.ToArray().Length); });
    }

    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public static void Title_Get_Unix()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.Title);
    }

    [ActiveIssue(4636, PlatformID.Windows)]
    [Fact]
    [OuterLoop] // changes the title and we can't change it back automatically in the test
    public static void Title_Set()
    {
        Console.Title = "Title set by unit test";
    }

    [Fact]
    [OuterLoop] // makes noise, not very inner-loop friendly
    public static void Beep()
    {
        // Nothing to verify; just run the code.
        Console.Beep();
    }

    [Fact]
    [OuterLoop] // clears the screen, not very inner-loop friendly
    public static void Clear()
    {
        // Nothing to verify; just run the code.
        Console.Clear();
    }

    [Fact]
    public static void SetCursorPosition()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || (!Console.IsInputRedirected && !Console.IsOutputRedirected))
        {
            // Nothing to verify; just run the code.
            // On windows, we might end of throwing IOException, since the handles are redirected.
            Console.SetCursorPosition(0, 0);
            Console.SetCursorPosition(1, 2);
        }
            Assert.Throws<ArgumentOutOfRangeException>("left", () => Console.SetCursorPosition(-1, 100));
            Assert.Throws<ArgumentOutOfRangeException>("top", () => Console.SetCursorPosition(100, -1));
    }

    [Fact]
    public static void GetCursorPosition()
    {
        if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
        {
            int origLeft = Console.CursorLeft, origTop = Console.CursorTop;

            Console.SetCursorPosition(10, 12);
            Assert.Equal(10, Console.CursorLeft);
            Assert.Equal(12, Console.CursorTop);

            Console.SetCursorPosition(origLeft, origTop);
            Assert.Equal(origLeft, Console.CursorLeft);
            Assert.Equal(origTop, Console.CursorTop);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Assert.Equal(0, Console.CursorLeft);
            Assert.Equal(0, Console.CursorTop);
        }
    }
}
