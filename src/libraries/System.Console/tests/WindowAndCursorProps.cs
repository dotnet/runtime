// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

public class WindowAndCursorProps
{
    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.Browser & ~TestPlatforms.iOS & ~TestPlatforms.MacCatalyst & ~TestPlatforms.tvOS)]  // Expected behavior specific to Unix
    public static void BufferWidth_GetUnix_ReturnsWindowWidth()
    {
        Assert.Equal(Console.WindowWidth, Console.BufferWidth);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]  // Expected behavior specific to Unix
    public static void BufferWidth_SetUnix_ThrowsPlatformNotSupportedException()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.BufferWidth = 1);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.Browser & ~TestPlatforms.iOS & ~TestPlatforms.MacCatalyst & ~TestPlatforms.tvOS)]  // Expected behavior specific to Unix
    public static void BufferHeight_GetUnix_ReturnsWindowHeight()
    {
        Assert.Equal(Console.WindowHeight, Console.BufferHeight);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]  // Expected behavior specific to Unix
    public static void BufferHeight_SetUnix_ThrowsPlatformNotSupportedException()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.BufferHeight = 1);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]  // Expected behavior specific to Unix
    public static void SetBufferSize_Unix_ThrowsPlatformNotSupportedException()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.SetBufferSize(0, 0));
    }

    [Theory]
    [PlatformSpecific((TestPlatforms.Windows) | (TestPlatforms.AnyUnix & ~TestPlatforms.Browser & ~TestPlatforms.iOS & ~TestPlatforms.MacCatalyst & ~TestPlatforms.tvOS))]
    [InlineData(0)]
    [InlineData(-1)]
    public static void WindowWidth_SetInvalid_ThrowsArgumentOutOfRangeException(int value)
    {
        if (Console.IsOutputRedirected)
        {
            Assert.Throws<IOException>(() => Console.WindowWidth = value);
        }
        else
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("WindowWidth", () => Console.WindowWidth = value);
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.Browser & ~TestPlatforms.iOS & ~TestPlatforms.MacCatalyst & ~TestPlatforms.tvOS)]  // Expected behavior specific to Unix
    public static void WindowWidth_GetUnix_Success()
    {
        // Validate that Console.WindowWidth returns some value in a non-redirected o/p.
        Helpers.RunInNonRedirectedOutput((data) => Console.WriteLine(Console.WindowWidth));
        Helpers.RunInRedirectedOutput((data) => Console.WriteLine(Console.WindowWidth));
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS)]  // Expected behavior specific to Unix
    public static void WindowWidth_SetUnix_ThrowsPlatformNotSupportedException()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.WindowWidth = 100);
    }

    [Theory]
    [PlatformSpecific((TestPlatforms.Windows) | (TestPlatforms.AnyUnix & ~TestPlatforms.Browser & ~TestPlatforms.iOS & ~TestPlatforms.MacCatalyst & ~TestPlatforms.tvOS))]
    [InlineData(0)]
    [InlineData(-1)]
    public static void WindowHeight_SetInvalid_ThrowsArgumentOutOfRangeException(int value)
    {
        if (Console.IsOutputRedirected)
        {
            Assert.Throws<IOException>(() => Console.WindowHeight = value);
        }
        else
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("WindowHeight", () => Console.WindowHeight = value);
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.Browser & ~TestPlatforms.iOS & ~TestPlatforms.MacCatalyst & ~TestPlatforms.tvOS)]  // Expected behavior specific to Unix
    public static void WindowHeight_GetUnix_Success()
    {
        // Validate that Console.WindowHeight returns some value in a non-redirected o/p.
        Helpers.RunInNonRedirectedOutput((data) => Console.WriteLine(Console.WindowHeight));
        Helpers.RunInRedirectedOutput((data) => Console.WriteLine(Console.WindowHeight));
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.Browser & ~TestPlatforms.iOS & ~TestPlatforms.MacCatalyst & ~TestPlatforms.tvOS)]  // Expected behavior specific to Unix
    public static void LargestWindowWidth_UnixGet_ReturnsExpected()
    {
        Helpers.RunInNonRedirectedOutput((data) => Assert.Equal(Console.WindowWidth, Console.LargestWindowWidth));
        Helpers.RunInRedirectedOutput((data) => Assert.Equal(Console.WindowWidth, Console.LargestWindowWidth));
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS)]  // Expected behavior specific to Unix
    public static void WindowHeight_SetUnix_ThrowsPlatformNotSupportedException()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.WindowHeight = 100);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.Browser & ~TestPlatforms.iOS & ~TestPlatforms.MacCatalyst & ~TestPlatforms.tvOS)]  // Expected behavior specific to Unix
    public static void LargestWindowHeight_UnixGet_ReturnsExpected()
    {
        Helpers.RunInNonRedirectedOutput((data) => Assert.Equal(Console.WindowHeight, Console.LargestWindowHeight));
        Helpers.RunInRedirectedOutput((data) => Assert.Equal(Console.WindowHeight, Console.LargestWindowHeight));
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Browser)]
    public static void WindowHeight_Getter_Throws_PlatformNotSupportedException()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.WindowHeight);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Browser)]
    public static void WindowWidth_Getter_Throws_PlatformNotSupportedException()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.WindowWidth);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]  // Expected behavior specific to Unix
    public static void WindowLeft_GetUnix_ReturnsZero()
    {
        Assert.Equal(0, Console.WindowLeft);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]  // Expected behavior specific to Unix
    public static void WindowLeft_SetUnix_ThrowsPlatformNotSupportedException()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.WindowLeft = 0);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]  // Expected behavior specific to Unix
    public static void WindowTop_GetUnix_ReturnsZero()
    {
        Assert.Equal(0, Console.WindowTop);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]  // Expected behavior specific to Unix
    public static void WindowTop_SetUnix_ThrowsPlatformNotSupportedException()
    {
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
    public static void CursorVisible_GetUnix_ThrowsPlatformNotSupportedException()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.CursorVisible);
    }

    [Theory]
    [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.Browser & ~TestPlatforms.iOS & ~TestPlatforms.MacCatalyst & ~TestPlatforms.tvOS )]  // Expected behavior specific to Unix
    [InlineData(true)]
    [InlineData(false)]
    public static void CursorVisible_SetUnixRedirected_Nop(bool value)
    {
        Helpers.RunInRedirectedOutput((data) => {
            Console.CursorVisible = value;
            Assert.Equal(0, data.ToArray().Length);
        });
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]  // Expected behavior specific to Unix
    public static void Title_GetUnix_ThrowPlatformNotSupportedException()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.Title);
    }

    [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
    [PlatformSpecific(TestPlatforms.AnyUnix)]  // Expected behavior specific to Unix
    public static void Title_SetUnix_Success()
    {
        RemoteExecutor.Invoke(() =>
        {
            Console.Title = "Title set by unit test";
        }).Dispose();
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    public static void Title_GetWindows_ReturnsNonNull()
    {
        Assert.NotNull(Console.Title);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoNorServerCore))]
    [PlatformSpecific(TestPlatforms.Windows)]
    public static void Title_Get_Windows_NoNulls()
    {
        string title = Console.Title;
        string trimmedTitle = title.TrimEnd('\0');
        Assert.Equal(trimmedTitle, title);
    }

    private static bool IsNotWindowsNanoServerAndNotServerCoreAndRemoteExecutorSupported => PlatformDetection.IsNotWindowsNanoNorServerCore && RemoteExecutor.IsSupported;

    [ConditionalTheory(nameof(IsNotWindowsNanoServerAndNotServerCoreAndRemoteExecutorSupported))] // Nano and Server Core currently ignore set title
    [ActiveIssue("https://github.com/dotnet/runtime/issues/34454", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(254)]
    [InlineData(255)]
    [InlineData(256)]
    [InlineData(257)]
    [InlineData(511)]
    [InlineData(512)]
    [InlineData(513)]
    [InlineData(1024)]
    [PlatformSpecific(TestPlatforms.Windows)]
    public static void Title_Set_Windows(int lengthOfTitle)
    {
        // Try to set the title to some other value.
        RemoteExecutor.Invoke(lengthOfTitleString =>
        {
            string newTitle = new string('a', int.Parse(lengthOfTitleString));
            Console.Title = newTitle;

            Assert.Equal(newTitle, Console.Title);
        }, lengthOfTitle.ToString()).Dispose();
    }

    [Fact]
    public static void Title_SetNull_ThrowsArgumentNullException()
    {
        AssertExtensions.Throws<ArgumentNullException>("value", () => Console.Title = null);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
    [OuterLoop] // makes noise, not very inner-loop friendly
    public static void Beep_Invoke_Success()
    {
        // Nothing to verify; just run the code.
        Console.Beep();
    }

    [Fact]
    [OuterLoop] // makes noise, not very inner-loop friendly
    [PlatformSpecific(TestPlatforms.Windows)]
    public static void BeepWithFrequency_Invoke_Success()
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
    public static void Clear_Invoke_Success()
    {
        if (!OperatingSystem.IsWindows() || (!Console.IsInputRedirected && !Console.IsOutputRedirected))
        {
            // Nothing to verify; just run the code.
            Console.Clear();
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Browser)]
    public static void SetCursorPosition_Throws_PlatformNotSupportedException()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.SetCursorPosition(0, 0));
    }

    [Fact]
    [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "Not supported on Browser, iOS, MacCatalyst, or tvOS.")]
    public static void SetCursorPosition_Invoke_Success()
    {
        if (!OperatingSystem.IsWindows() || (!Console.IsInputRedirected && !Console.IsOutputRedirected))
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
    [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "Not supported on Browser, iOS, MacCatalyst, or tvOS.")]
    public static void GetCursorPosition_Invoke_ReturnsExpected()
    {
        if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
        {
            int origLeft = Console.CursorLeft, origTop = Console.CursorTop;
            (int, int) origTuple = Console.GetCursorPosition();

            Console.SetCursorPosition(10, 12);
            Assert.Equal(10, Console.CursorLeft);
            Assert.Equal(12, Console.CursorTop);
            Assert.Equal((10, 12), Console.GetCursorPosition());

            Console.SetCursorPosition(origLeft, origTop);
            Assert.Equal(origLeft, Console.CursorLeft);
            Assert.Equal(origTop, Console.CursorTop);
            Assert.Equal(origTuple, Console.GetCursorPosition());
        }
        else if (!OperatingSystem.IsWindows())
        {
            Assert.Equal(0, Console.CursorLeft);
            Assert.Equal(0, Console.CursorTop);
            Assert.Equal((0, 0), Console.GetCursorPosition());
        }
    }

    [Fact]
    [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "Not supported on Browser, iOS, MacCatalyst, or tvOS.")]
    public void CursorLeft_Set_GetReturnsExpected()
    {
        if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
        {
            int origLeft = Console.CursorLeft;

            Console.CursorLeft = 10;
            Assert.Equal(10, Console.CursorLeft);

            Console.CursorLeft = origLeft;
            Assert.Equal(origLeft, Console.CursorLeft);
        }
        else if (!OperatingSystem.IsWindows())
        {
            Assert.Equal(0, Console.CursorLeft);
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(short.MaxValue + 1)]
    [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "Not supported on Browser, iOS, MacCatalyst, or tvOS.")]
    public void CursorLeft_SetInvalid_ThrowsArgumentOutOfRangeException(int value)
    {
        if (PlatformDetection.IsWindows && Console.IsOutputRedirected)
        {
            Assert.Throws<IOException>(() => Console.CursorLeft = value);
        }
        else
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("left", () => Console.CursorLeft = value);
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Browser)]
    public void CursorLeft_Setter_Throws_PlatformNotSupportedException()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.CursorLeft = 0);
    }

    [Fact]
    [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "Not supported on Browser, iOS, MacCatalyst, or tvOS.")]
    public void CursorTop_Set_GetReturnsExpected()
    {
        if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
        {
            int origTop = Console.CursorTop;

            Console.CursorTop = 10;
            Assert.Equal(10, Console.CursorTop);

            Console.CursorTop = origTop;
            Assert.Equal(origTop, Console.CursorTop);
        }
        else if (!OperatingSystem.IsWindows())
        {
            Assert.Equal(0, Console.CursorTop);
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(short.MaxValue + 1)]
    [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "Not supported on Browser, iOS, MacCatalyst, or tvOS.")]
    public void CursorTop_SetInvalid_ThrowsArgumentOutOfRangeException(int value)
    {
        if (PlatformDetection.IsWindows & Console.IsOutputRedirected)
        {
            Assert.Throws<IOException>(() => Console.CursorTop = value);
        }
        else
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("top", () => Console.CursorTop = value);
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Browser)]
    public void CursorTop_Setter_Throws_PlatformNotSupportedException()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.CursorTop = 0 );
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void CursorSize_Set_GetReturnsExpected()
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
    [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.Browser & ~TestPlatforms.iOS & ~TestPlatforms.MacCatalyst & ~TestPlatforms.tvOS)]
    public void CursorSize_GetUnix_ReturnsExpected()
    {
        Assert.Equal(100, Console.CursorSize);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Browser)]
    public void CursorSize_Getter_Throws_PlatformNotSupportedException()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.CursorSize);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]
    public void CursorSize_SetUnix_ThrowsPlatformNotSupportedException()
    {
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
            AssertExtensions.Throws<ArgumentOutOfRangeException>("left", () => Console.SetWindowPosition(Console.WindowHeight, Console.BufferHeight - Console.WindowHeight + 2));

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

    [PlatformSpecific(TestPlatforms.Windows)]
    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoNorServerCore))]
    public void SetWindowSize_GetWindowSize_ReturnsExpected()
    {
        if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("width", () => Console.SetWindowSize(-1, Console.WindowHeight));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("height", () => Console.SetWindowSize(Console.WindowHeight, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("width", () => Console.SetWindowSize(short.MaxValue - Console.WindowLeft, Console.WindowHeight));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("height", () => Console.SetWindowSize(Console.WindowWidth, short.MaxValue - Console.WindowTop));

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
    [PlatformSpecific(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS)]
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
