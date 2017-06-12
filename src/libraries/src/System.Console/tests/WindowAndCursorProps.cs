// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.NetCore.Extensions;

public class WindowAndCursorProps : RemoteExecutorTestBase
{
    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]  // Expected behavior specific to Unix
    public static void BufferSize_SettingNotSupported()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.BufferWidth = 1);
        Assert.Throws<PlatformNotSupportedException>(() => Console.BufferHeight = 1);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]  // Expected behavior specific to Unix
    public static void BufferSize_GettingSameAsWindowSize()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.BufferWidth = 1);
        Assert.Throws<PlatformNotSupportedException>(() => Console.BufferHeight = 1);

        Assert.Equal(Console.WindowWidth, Console.BufferWidth);
        Assert.Equal(Console.WindowHeight, Console.BufferHeight);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]  // Expected behavior specific to Windows
    public static void WindowWidth_WindowHeight_InvalidSize()
    {
        if (Console.IsOutputRedirected)
        {
            Assert.Throws<IOException>(() => Console.WindowWidth = 0);
            Assert.Throws<IOException>(() => Console.WindowHeight = 0);
        }
        else
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("width", () => Console.WindowWidth = 0);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("height", () => Console.WindowHeight = 0);
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]  // Expected behavior specific to Unix
    public static void WindowWidth()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.WindowWidth = 100);

        // Validate that Console.WindowWidth returns some value in a non-redirected o/p.
        Helpers.RunInNonRedirectedOutput((data) => Console.WriteLine(Console.WindowWidth));
        Helpers.RunInRedirectedOutput((data) => Console.WriteLine(Console.WindowWidth));
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]  // Expected behavior specific to Unix
    public static void WindowHeight()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.WindowHeight = 100);

        // Validate that Console.WindowHeight returns some value in a non-redirected o/p.
        Helpers.RunInNonRedirectedOutput((data) => Console.WriteLine(Console.WindowHeight));
        Helpers.RunInRedirectedOutput((data) => Console.WriteLine(Console.WindowHeight));
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]  // Expected behavior specific to Unix
    public static void WindowLeftTop_AnyUnix()
    {
        Assert.Equal(0, Console.WindowLeft);
        Assert.Equal(0, Console.WindowTop);
        Assert.Throws<PlatformNotSupportedException>(() => Console.WindowLeft = 0);
        Assert.Throws<PlatformNotSupportedException>(() => Console.WindowTop = 0);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]  // Expected behavior specific to Windows
    public static void WindowLeftTop_Windows()
    {
        if (Console.IsOutputRedirected)
        {
            Assert.Throws<IOException>(() => Console.WindowLeft);
            Assert.Throws<IOException>(() => Console.WindowTop);
        }
        else
        {
            Console.WriteLine(Console.WindowLeft);
            Console.WriteLine(Console.WindowTop);
        }
    }

    [Fact] 
    [PlatformSpecific(TestPlatforms.AnyUnix)]  // Expected behavior specific to Unix
    [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)] //CI system makes it difficult to run things in a non-redirected environments.
    public static void NonRedirectedCursorVisible()
    {
        if (!Console.IsOutputRedirected)
        {
            // Validate that Console.CursorVisible adds something to the stream when in a non-redirected environment.
            Helpers.RunInNonRedirectedOutput((data) => { Console.CursorVisible = false; Assert.True(data.ToArray().Length > 0); });
            Helpers.RunInNonRedirectedOutput((data) => { Console.CursorVisible = true; Assert.True(data.ToArray().Length > 0); });
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]  // Expected behavior specific to Unix
    public static void CursorVisible()
    {
        Assert.Throws<PlatformNotSupportedException>(() => { bool unused = Console.CursorVisible; });

        // Validate that the Console.CursorVisible does nothing in a redirected stream.
        Helpers.RunInRedirectedOutput((data) => { Console.CursorVisible = false; Assert.Equal(0, data.ToArray().Length); });
        Helpers.RunInRedirectedOutput((data) => { Console.CursorVisible = true; Assert.Equal(0, data.ToArray().Length); });
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]  // Expected behavior specific to Unix
    public static void Title_GetSet_Unix()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.Title);
        RemoteInvoke(() =>
        {
            Console.Title = "Title set by unit test";
            return SuccessExitCode;
        }).Dispose();
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]  // Expected behavior specific to Windows
    public static void Title_Get_Windows()
    {
        Assert.NotNull(Console.Title);
    }

    [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))] // Nano currently ignores set title
    [InlineData(10)]
    [InlineData(256)]
    [InlineData(1024)]
    [PlatformSpecific(TestPlatforms.Windows)]  // Expected behavior specific to Windows
    public static void Title_Set_Windows(int lengthOfTitle)
    {
        // Try to set the title to some other value.
        RemoteInvoke(lengthOfTitleString =>
        {
            string newTitle = new string('a', int.Parse(lengthOfTitleString));
            Console.Title = newTitle;
            Assert.Equal(newTitle, Console.Title);
            return SuccessExitCode;
        }, lengthOfTitle.ToString()).Dispose();
    }

    [Fact]
    public static void Title_Set_Windows_Null_ThrowsArgumentNullException()
    {
        AssertExtensions.Throws<ArgumentNullException>("value", () => Console.Title = null);
    }

    [Fact]
    public static void Title_Set_Windows_GreaterThan24500Chars_ThrowsArgumentOutOfRangeException()
    {
        string newTitle = new string('a', 24501);
        AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => Console.Title = newTitle);
    }

    [Fact]
    [OuterLoop] // makes noise, not very inner-loop friendly
    public static void Beep()
    {
        // Nothing to verify; just run the code.
        Console.Beep();
    }

    [Fact]
    [OuterLoop] // makes noise, not very inner-loop friendly
    [PlatformSpecific(TestPlatforms.Windows)]
    public static void BeepWithFrequency()
    {
        // Nothing to verify; just run the code.
        Console.Beep(800, 200);
    }

    [Theory]
    [PlatformSpecific(TestPlatforms.Windows)]
    [InlineData(36)]
    [InlineData(32768)]
    public void BeepWithFrequency_InvalidFrequency_ThrowsArgumentOutOfRangeException(int frequency)
    {
        AssertExtensions.Throws<ArgumentOutOfRangeException>("frequency", () => Console.Beep(frequency, 200));
    }

    [Theory]
    [PlatformSpecific(TestPlatforms.Windows)]
    [InlineData(0)]
    [InlineData(-1)]
    public void BeepWithFrequency_InvalidDuration_ThrowsArgumentOutOfRangeException(int duration)
    {
        AssertExtensions.Throws<ArgumentOutOfRangeException>("duration", () => Console.Beep(800, duration));
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]
    public void BeepWithFrequency_Unix_ThrowsPlatformNotSupportedException()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.Beep(800, 200));
    }

    [Fact]
    [OuterLoop] // clears the screen, not very inner-loop friendly
    public static void Clear()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || (!Console.IsInputRedirected && !Console.IsOutputRedirected))
        {
            // Nothing to verify; just run the code.
            Console.Clear();
        }
    }

    [Fact]
    public static void SetCursorPosition()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || (!Console.IsInputRedirected && !Console.IsOutputRedirected))
        {
            int origLeft = Console.CursorLeft;
            int origTop = Console.CursorTop;

            // Nothing to verify; just run the code.
            // On windows, we might end of throwing IOException, since the handles are redirected.
            Console.SetCursorPosition(0, 0);
            Console.SetCursorPosition(1, 2);

            Console.SetCursorPosition(origLeft, origTop);
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(short.MaxValue + 1)]
    public void SetCursorPosition_InvalidPosition_ThrowsArgumentOutOfRangeException(int value)
    {
        AssertExtensions.Throws<ArgumentOutOfRangeException>("left", () => Console.SetCursorPosition(value, 100));
        AssertExtensions.Throws<ArgumentOutOfRangeException>("top", () => Console.SetCursorPosition(100, value));
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
        else if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Equal(0, Console.CursorLeft);
            Assert.Equal(0, Console.CursorTop);
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void CursorSize_SetGet_ReturnsExpected()
    {
        if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
        {
            int orig = Console.CursorSize;
            try
            {
                Console.CursorSize = 50;
                Assert.Equal(50, Console.CursorSize);
            }
            finally
            {
                Console.CursorSize = orig;
            }
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void CursorSize_SetInvalidValue_ThrowsArgumentOutOfRangeException(int value)
    {
        AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => Console.CursorSize = value);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]
    public void CursorSize_SetUnix_ThrowsPlatformNotSupportedException()
    {
        Assert.Equal(100, Console.CursorSize);
        Assert.Throws<PlatformNotSupportedException>(() => Console.CursorSize = 1);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void SetWindowPosition_GetWindowPosition_ReturnsExpected()
    {
        if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("left", () => Console.SetWindowPosition(-1, Console.WindowTop));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("top", () => Console.SetWindowPosition(Console.WindowLeft, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("left", () => Console.SetWindowPosition(Console.BufferWidth - Console.WindowWidth + 2, Console.WindowTop));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("top", () => Console.SetWindowPosition(Console.WindowHeight, Console.BufferHeight - Console.WindowHeight + 2));

            int origTop = Console.WindowTop;
            int origLeft = Console.WindowLeft;
            try
            {
                Console.SetWindowPosition(0, 0);
                Assert.Equal(0, Console.WindowTop);
                Assert.Equal(0, Console.WindowLeft);
            }
            finally
            {
                Console.WindowTop = origTop;
                Console.WindowLeft = origLeft;
            }
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]
    public void SetWindowPosition_Unix_ThrowsPlatformNotSupportedException()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.SetWindowPosition(50, 50));
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void SetWindowSize_GetWindowSize_ReturnsExpected()
    {
        if (PlatformDetection.IsNotWindowsNanoServer && !Console.IsInputRedirected && !Console.IsOutputRedirected)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("width", () => Console.SetWindowSize(-1, Console.WindowHeight));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("height", () => Console.SetWindowSize(Console.WindowHeight, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("width", () => Console.SetWindowSize(short.MaxValue - Console.WindowLeft, Console.WindowHeight));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("height", () => Console.SetWindowSize(Console.WindowWidth, short.MaxValue - Console.WindowTop));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("width", () => Console.SetWindowSize(Console.LargestWindowWidth + 1, Console.WindowHeight));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("height", () => Console.SetWindowSize(Console.WindowWidth, Console.LargestWindowHeight + 1));

            int origWidth = Console.WindowWidth;
            int origHeight = Console.WindowHeight;
            try
            {
                Console.SetWindowSize(10, 10);
                Assert.Equal(10, Console.WindowWidth);
                Assert.Equal(10, Console.WindowHeight);
            }
            finally
            {
                Console.WindowWidth = origWidth;
                Console.WindowHeight = origHeight;
            }
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]
    public void SetWindowSize_Unix_ThrowsPlatformNotSupportedException()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.SetWindowSize(50, 50));
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void MoveBufferArea_DefaultChar()
    {
        if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("sourceLeft", () => Console.MoveBufferArea(-1, 0, 0, 0, 0, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("sourceTop", () => Console.MoveBufferArea(0, -1, 0, 0, 0, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("sourceWidth", () => Console.MoveBufferArea(0, 0, -1, 0, 0, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("sourceHeight", () => Console.MoveBufferArea(0, 0, 0, -1, 0, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("targetLeft", () => Console.MoveBufferArea(0, 0, 0, 0, -1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("targetTop", () => Console.MoveBufferArea(0, 0, 0, 0, 0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("sourceLeft", () => Console.MoveBufferArea(Console.BufferWidth + 1, 0, 0, 0, 0, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("targetLeft", () => Console.MoveBufferArea(0, 0, 0, 0, Console.BufferWidth + 1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("sourceTop", () => Console.MoveBufferArea(0, Console.BufferHeight + 1, 0, 0, 0, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("targetTop", () => Console.MoveBufferArea(0, 0, 0, 0, 0, Console.BufferHeight + 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("sourceHeight", () => Console.MoveBufferArea(0, 1, 0, Console.BufferHeight, 0, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("sourceWidth", () => Console.MoveBufferArea(1, 0, Console.BufferWidth, 0, 0, 0));

            // Nothing to verify; just run the code.
            Console.MoveBufferArea(0, 0, 1, 1, 2, 2);
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void MoveBufferArea()
    {
        if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Console.MoveBufferArea(-1, 0, 0, 0, 0, 0, '0', ConsoleColor.Black, ConsoleColor.White));
            Assert.Throws<ArgumentOutOfRangeException>(() => Console.MoveBufferArea(0, -1, 0, 0, 0, 0, '0', ConsoleColor.Black, ConsoleColor.White));
            Assert.Throws<ArgumentOutOfRangeException>(() => Console.MoveBufferArea(0, 0, -1, 0, 0, 0, '0', ConsoleColor.Black, ConsoleColor.White));
            Assert.Throws<ArgumentOutOfRangeException>(() => Console.MoveBufferArea(0, 0, 0, -1, 0, 0, '0', ConsoleColor.Black, ConsoleColor.White));
            Assert.Throws<ArgumentOutOfRangeException>(() => Console.MoveBufferArea(0, 0, 0, 0, -1, 0, '0', ConsoleColor.Black, ConsoleColor.White));
            Assert.Throws<ArgumentOutOfRangeException>(() => Console.MoveBufferArea(0, 0, 0, 0, 0, -1, '0', ConsoleColor.Black, ConsoleColor.White));
            Assert.Throws<ArgumentOutOfRangeException>(() => Console.MoveBufferArea(Console.BufferWidth + 1, 0, 0, 0, 0, 0, '0', ConsoleColor.Black, ConsoleColor.White));
            Assert.Throws<ArgumentOutOfRangeException>(() => Console.MoveBufferArea(0, 0, 0, 0, Console.BufferWidth + 1, 0, '0', ConsoleColor.Black, ConsoleColor.White));
            Assert.Throws<ArgumentOutOfRangeException>(() => Console.MoveBufferArea(0, Console.BufferHeight + 1, 0, 0, 0, 0, '0', ConsoleColor.Black, ConsoleColor.White));
            Assert.Throws<ArgumentOutOfRangeException>(() => Console.MoveBufferArea(0, 0, 0, 0, 0, Console.BufferHeight + 1, '0', ConsoleColor.Black, ConsoleColor.White));
            Assert.Throws<ArgumentOutOfRangeException>(() => Console.MoveBufferArea(0, 1, 0, Console.BufferHeight, 0, 0, '0', ConsoleColor.Black, ConsoleColor.White));
            Assert.Throws<ArgumentOutOfRangeException>(() => Console.MoveBufferArea(1, 0, Console.BufferWidth, 0, 0, 0, '0', ConsoleColor.Black, ConsoleColor.White));

            // Nothing to verify; just run the code.
            Console.MoveBufferArea(0, 0, 1, 1, 2, 2, 'a', ConsoleColor.Black, ConsoleColor.White);
        }
    }

    [Theory]
    [InlineData(ConsoleColor.Black - 1)]
    [InlineData(ConsoleColor.White + 1)]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void MoveBufferArea_InvalidColor_ThrowsException(ConsoleColor color)
    {
        AssertExtensions.Throws<ArgumentException>("sourceForeColor", () => Console.MoveBufferArea(0, 0, 0, 0, 0, 0, 'a', color, ConsoleColor.Black));
        AssertExtensions.Throws<ArgumentException>("sourceBackColor", () => Console.MoveBufferArea(0, 0, 0, 0, 0, 0, 'a', ConsoleColor.Black, color));
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]
    public void MoveBufferArea_Unix_ThrowsPlatformNotSupportedException()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.MoveBufferArea(0, 0, 0, 0, 0, 0));
        Assert.Throws<PlatformNotSupportedException>(() => Console.MoveBufferArea(0, 0, 0, 0, 0, 0, 'c', ConsoleColor.White, ConsoleColor.Black));
    }
}
