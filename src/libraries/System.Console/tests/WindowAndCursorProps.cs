// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.NetCore.Extensions;

public class WindowAndCursorProps : RemoteExecutorTestBase
{
    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]
    public static void BufferSize_SettingNotSupported()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.BufferWidth = 1);
        Assert.Throws<PlatformNotSupportedException>(() => Console.BufferHeight = 1);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]
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
    [PlatformSpecific(TestPlatforms.AnyUnix)]
    public static void WindowWidth()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.WindowWidth = 100);

        // Validate that Console.WindowWidth returns some value in a non-redirected o/p.
        Helpers.RunInNonRedirectedOutput((data) => Console.WriteLine(Console.WindowWidth));
        Helpers.RunInRedirectedOutput((data) => Console.WriteLine(Console.WindowWidth));
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]
    public static void WindowHeight()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.WindowHeight = 100);

        // Validate that Console.WindowHeight returns some value in a non-redirected o/p.
        Helpers.RunInNonRedirectedOutput((data) => Console.WriteLine(Console.WindowHeight));
        Helpers.RunInRedirectedOutput((data) => Console.WriteLine(Console.WindowHeight));
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]
    public static void WindowLeftTop_AnyUnix()
    {
        Assert.Equal(0, Console.WindowLeft);
        Assert.Equal(0, Console.WindowTop);
        Assert.Throws<PlatformNotSupportedException>(() => Console.WindowLeft = 0);
        Assert.Throws<PlatformNotSupportedException>(() => Console.WindowTop = 0);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
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
    [PlatformSpecific(TestPlatforms.AnyUnix)]
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
    [PlatformSpecific(TestPlatforms.AnyUnix)]
    public static void CursorVisible()
    {
        Assert.Throws<PlatformNotSupportedException>(() => { bool unused = Console.CursorVisible; });

        // Validate that the Console.CursorVisible does nothing in a redirected stream.
        Helpers.RunInRedirectedOutput((data) => { Console.CursorVisible = false; Assert.Equal(0, data.ToArray().Length); });
        Helpers.RunInRedirectedOutput((data) => { Console.CursorVisible = true; Assert.Equal(0, data.ToArray().Length); });
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]
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
    [PlatformSpecific(TestPlatforms.Windows)]
    public static void Title_Get_Windows()
    {
        Assert.NotNull(Console.Title);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(256)]
    [InlineData(1024)]
    [PlatformSpecific(TestPlatforms.Windows)]
    public static void Title_Set_Windows(int lengthOfTitle)
    {
        // Try to set the title to some other value.
        RemoteInvoke(() =>
        {
            string newTitle = new string('a', lengthOfTitle);
            Console.Title = newTitle;
            Assert.Equal(newTitle, Console.Title);
            return SuccessExitCode;
        }).Dispose();
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    public static void Title_Set_Windows_longlength()
    {
        RemoteInvoke(() =>
        {
            // Try setting a title greater than 24500 chars and check that it fails.
            string newTitle = new string('a', 24501);
            Assert.Throws<ArgumentOutOfRangeException>(() => { Console.Title = newTitle; });
            return SuccessExitCode;
        }).Dispose();
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
    public static void BeepWithFrequency()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Console.Beep(36, 200));
            Assert.Throws<ArgumentOutOfRangeException>(() => Console.Beep(32768, 200));
            Assert.Throws<ArgumentOutOfRangeException>(() => Console.Beep(800, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Console.Beep(800, -1));

            // Nothing to verify; just run the code.
            Console.Beep(800, 200);
        }
        else
        {
            Assert.Throws<PlatformNotSupportedException>(() => Console.Beep(800, 200));
        }
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
        else if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Equal(0, Console.CursorLeft);
            Assert.Equal(0, Console.CursorTop);
        }
    }

    [Fact]
    public static void CursorSize()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => { Console.CursorSize = 0; });
                Assert.Throws<ArgumentOutOfRangeException>(() => { Console.CursorSize = 101; });

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
        else
        {
            Assert.Equal(100, Console.CursorSize);
            Assert.Throws<PlatformNotSupportedException>(() => { Console.CursorSize = 50; });
        }
    }

    [Fact]
    public static void SetWindowPosition()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => Console.SetWindowPosition(-1, Console.WindowTop));
                Assert.Throws<ArgumentOutOfRangeException>(() => Console.SetWindowPosition(Console.WindowLeft, -1));
                Assert.Throws<ArgumentOutOfRangeException>(() => Console.SetWindowPosition(Console.BufferWidth - Console.WindowWidth + 2, Console.WindowTop));
                Assert.Throws<ArgumentOutOfRangeException>(() => Console.SetWindowPosition(Console.WindowHeight, Console.BufferHeight - Console.WindowHeight + 2));

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
        else
        {
            Assert.Throws<PlatformNotSupportedException>(() => Console.SetWindowPosition(50, 50));
        }
    }

    [Fact]
    public static void SetWindowSize()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (PlatformDetection.IsNotWindowsNanoServer && !Console.IsInputRedirected && !Console.IsOutputRedirected)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => Console.SetWindowSize(-1, Console.WindowHeight));
                Assert.Throws<ArgumentOutOfRangeException>(() => Console.SetWindowSize(Console.WindowWidth, -1));
                Assert.Throws<ArgumentOutOfRangeException>(() => Console.SetWindowSize(short.MaxValue - Console.WindowLeft, Console.WindowHeight));
                Assert.Throws<ArgumentOutOfRangeException>(() => Console.SetWindowSize(Console.WindowWidth, short.MaxValue - Console.WindowTop));
                Assert.Throws<ArgumentOutOfRangeException>(() => Console.SetWindowSize(Console.LargestWindowWidth + 1, Console.WindowHeight));
                Assert.Throws<ArgumentOutOfRangeException>(() => Console.SetWindowSize(Console.WindowWidth, Console.LargestWindowHeight + 1));

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
        else
        {
            Assert.Throws<PlatformNotSupportedException>(() => Console.SetWindowSize(50, 50));
        }
    }

    [Fact]
    public static void MoveBufferArea_DefaultChar()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => Console.MoveBufferArea(-1, 0, 0, 0, 0, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => Console.MoveBufferArea(0, -1, 0, 0, 0, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => Console.MoveBufferArea(0, 0, -1, 0, 0, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => Console.MoveBufferArea(0, 0, 0, -1, 0, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => Console.MoveBufferArea(0, 0, 0, 0, -1, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => Console.MoveBufferArea(0, 0, 0, 0, 0, -1));
                Assert.Throws<ArgumentOutOfRangeException>(() => Console.MoveBufferArea(Console.BufferWidth + 1, 0, 0, 0, 0, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => Console.MoveBufferArea(0, 0, 0, 0, Console.BufferWidth + 1, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => Console.MoveBufferArea(0, Console.BufferHeight + 1, 0, 0, 0, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => Console.MoveBufferArea(0, 0, 0, 0, 0, Console.BufferHeight + 1));
                Assert.Throws<ArgumentOutOfRangeException>(() => Console.MoveBufferArea(0, 1, 0, Console.BufferHeight, 0, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => Console.MoveBufferArea(1, 0, Console.BufferWidth, 0, 0, 0));
                // Nothing to verify; just run the code.
                Console.MoveBufferArea(0, 0, 1, 1, 2, 2);
            }
        }
        else
        {
            Assert.Throws<PlatformNotSupportedException>(() => Console.MoveBufferArea(0, 0, 0, 0, 0, 0));
        }
    }

    [Fact]
    public static void MoveBufferArea()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
        else
        {
            Assert.Throws<PlatformNotSupportedException>(() => Console.MoveBufferArea(0, 0, 0, 0, 0, 0));
        }
    }
}
