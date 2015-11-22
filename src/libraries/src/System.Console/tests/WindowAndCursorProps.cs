// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

public class WindowAndCursorProps
{
    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public static void WindowWidth()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.WindowWidth = 100);

        // Validate that Console.WindowWidth returns some value in a non-redirected o/p.
        Helpers.RunInNonRedirectedOutput((data) => Console.WriteLine(Console.WindowWidth));
        Helpers.RunInRedirectedOutput((data) => Console.WriteLine(Console.WindowWidth));
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
    public static void Title_GetSet_Unix()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.Title);
        Console.Title = "Title set by unit test";
    }

    [Fact]
    public static void Beep()
    {
        // Nothing to verify; just run the code.
        Console.Beep();
        Console.Beep(100, 100);

        Assert.Throws<ArgumentOutOfRangeException>("frequency", () => Console.Beep(36, 100));
        Assert.Throws<ArgumentOutOfRangeException>("frequency", () => Console.Beep((int)short.MaxValue + 1, 100));

        Assert.Throws<ArgumentOutOfRangeException>("duration", () => Console.Beep(100, 0));
        Assert.Throws<ArgumentOutOfRangeException>("duration", () => Console.Beep(100, -1));
    }

    [Fact]
    public static void Clear()
    {
        // Nothing to verify; just run the code.
        Console.Clear();
    }

    [Fact]
    public static void SetCursorPosition()
    {
        // Nothing to verify; just run the code.
        Console.SetCursorPosition(0, 0);
        Console.SetCursorPosition(1, 2);
        Assert.Throws<ArgumentOutOfRangeException>("left", () => Console.SetCursorPosition(-1, 100));
        Assert.Throws<ArgumentOutOfRangeException>("top", () => Console.SetCursorPosition(100, -1));
    }
}
