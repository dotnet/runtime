// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

public partial class ConsoleEncoding
{
    [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void InputEncoding_SetDefaultEncoding_Success()
    {
        RemoteExecutor.Invoke(() =>
        {
            Encoding encoding = Encoding.GetEncoding(0);
            Console.InputEncoding = encoding;
            Assert.Equal(encoding, Console.InputEncoding);
            Assert.Equal((uint)encoding.CodePage, GetConsoleCP());
        }).Dispose();
    }

    [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void InputEncoding_SetUnicodeEncoding_SilentlyIgnoredInternally()
    {
        RemoteExecutor.Invoke(() =>
        {
            Encoding unicodeEncoding = Encoding.Unicode;
            Encoding oldEncoding = Console.InputEncoding;
            Assert.NotEqual(unicodeEncoding.CodePage, oldEncoding.CodePage);

            Console.InputEncoding = unicodeEncoding;
            Assert.Equal(unicodeEncoding, Console.InputEncoding);
            Assert.Equal((uint)oldEncoding.CodePage, GetConsoleCP());
        }).Dispose();
    }

    [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void InputEncoding_SetEncodingWhenDetached_ErrorIsSilentlyIgnored()
    {
        RemoteExecutor.Invoke(() =>
        {
            Encoding encoding = Console.InputEncoding.CodePage != Encoding.ASCII.CodePage
                ? Encoding.ASCII
                : Encoding.Latin1;

            // use FreeConsole to detach the current console - simulating a process started with the "DETACHED_PROCESS" flag
            FreeConsole();

            // Setting the input encoding should not throw an exception
            Console.InputEncoding = encoding;
            // The internal state of Console should have updated, despite the failure to change the console's input encoding
            Assert.Equal(encoding, Console.InputEncoding);
            // Operations on the console are no longer valid - GetConsoleCP fails.
            Assert.Equal(0u, GetConsoleCP());
        }).Dispose();
    }

    [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void OutputEncoding_SetDefaultEncoding_Success()
    {
        RemoteExecutor.Invoke(() =>
        {
            Encoding encoding = Encoding.GetEncoding(0);
            Console.OutputEncoding = encoding;
            Assert.Equal(encoding, Console.OutputEncoding);
            Assert.Equal((uint)encoding.CodePage, GetConsoleOutputCP());
        }).Dispose();
    }

    [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void OutputEncoding_SetUnicodeEncoding_SilentlyIgnoredInternally()
    {
        RemoteExecutor.Invoke(() =>
        {
            Encoding unicodeEncoding = Encoding.Unicode;
            Encoding oldEncoding = Console.OutputEncoding;
            Assert.NotEqual(unicodeEncoding.CodePage, oldEncoding.CodePage);
            Console.OutputEncoding = unicodeEncoding;
            Assert.Equal(unicodeEncoding, Console.OutputEncoding);

            Assert.Equal((uint)oldEncoding.CodePage, GetConsoleOutputCP());
        }).Dispose();
    }

    [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void OutputEncoding_SetEncodingWhenDetached_ErrorIsSilentlyIgnored()
    {
        RemoteExecutor.Invoke(() =>
        {
            Encoding encoding = Console.OutputEncoding.CodePage != Encoding.ASCII.CodePage
                ? Encoding.ASCII
                : Encoding.Latin1;

            // use FreeConsole to detach the current console - simulating a process started with the "DETACHED_PROCESS" flag
            FreeConsole();

            // Setting the output encoding should not throw an exception
            Console.OutputEncoding = encoding;
            // The internal state of Console should have updated, despite the failure to change the console's output encoding
            Assert.Equal(encoding, Console.OutputEncoding);
            // Operations on the console are no longer valid - GetConsoleOutputCP fails.
            Assert.Equal(0u, GetConsoleOutputCP());
        }).Dispose();
    }

    private static bool IsNotWindowsNanoServerAndNotServerCoreAndRemoteExecutorSupported => PlatformDetection.IsNotWindowsNanoNorServerCore && RemoteExecutor.IsSupported;

    [ConditionalFact(typeof(ConsoleEncoding), nameof(IsNotWindowsNanoServerAndNotServerCoreAndRemoteExecutorSupported))]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void InputEncoding_SetUnicodeEncoding_StandardInputReadsAnyNumberOfBytes()
    {
        RemoteExecutor.Invoke(() =>
        {
            AllocatedConsole console = AllocateConsole();
            Console.InputEncoding = Encoding.Unicode;
            Assert.False(Console.IsInputRedirected);

            TypeIntoConsole(console.Input, "ab\r");
            byte[] expected = Encoding.Unicode.GetBytes("ab\r\n");

            using Stream stdin = Console.OpenStandardInput();
            var actual = new List<byte> { (byte)stdin.ReadByte() };

            // Odd and even buffer sizes, so a read starts both on and in the middle of a UTF-16 code unit.
            int[] bufferSizes = [3, 1, 2];
            byte[] buffer = new byte[3];
            for (int i = 0; actual.Count < expected.Length; i++)
            {
                int count = Math.Min(bufferSizes[i % bufferSizes.Length], expected.Length - actual.Count);
                int bytesRead = stdin.Read(buffer, 0, count);
                Assert.InRange(bytesRead, 1, count);
                actual.AddRange(buffer.AsSpan(0, bytesRead).ToArray());
            }

            Assert.Equal(expected, actual);
        }).Dispose();
    }

    [ConditionalFact(typeof(ConsoleEncoding), nameof(IsNotWindowsNanoServerAndNotServerCoreAndRemoteExecutorSupported))]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void OutputEncoding_SetUnicodeEncoding_StandardOutputWritesAnyNumberOfBytes()
    {
        RemoteExecutor.Invoke(() =>
        {
            AllocatedConsole console = AllocateConsole();
            Console.OutputEncoding = Encoding.Unicode;
            Assert.False(Console.IsOutputRedirected);

            Assert.NotEqual(0, GetConsoleScreenBufferInfo(console.Output, out CONSOLE_SCREEN_BUFFER_INFO info));
            byte[] bytes = Encoding.Unicode.GetBytes("abcd");

            using Stream stdout = Console.OpenStandardOutput();
            stdout.WriteByte(bytes[0]);
            stdout.WriteByte(bytes[1]);
            stdout.Write(bytes, 2, 3);
            stdout.Write(bytes, 5, 3);

            Assert.Equal("abcd", ReadConsoleOutput(console.Output, info.dwCursorPosition, 4));
        }).Dispose();
    }

    private readonly record struct AllocatedConsole(IntPtr Input, IntPtr Output);

    // Gives the process a console of its own, with the standard handles pointing at it instead of the redirected
    // handles it inherited, so the console streams use the console APIs.
    private static AllocatedConsole AllocateConsole()
    {
        FreeConsole();
        Assert.NotEqual(0, AllocConsole());

        var console = new AllocatedConsole(OpenConsoleDevice("CONIN$"), OpenConsoleDevice("CONOUT$"));
        Assert.NotEqual(0, SetStdHandle(STD_INPUT_HANDLE, console.Input));
        Assert.NotEqual(0, SetStdHandle(STD_OUTPUT_HANDLE, console.Output));
        return console;
    }

    private static IntPtr OpenConsoleDevice(string name)
    {
        IntPtr handle = CreateFileW(name, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
        Assert.NotEqual(INVALID_HANDLE_VALUE, handle);
        return handle;
    }

    private static unsafe void TypeIntoConsole(IntPtr input, string text)
    {
        var records = new INPUT_RECORD[text.Length * 2];
        for (int i = 0; i < text.Length; i++)
        {
            records[2 * i] = new INPUT_RECORD { EventType = KEY_EVENT, bKeyDown = 1, wRepeatCount = 1, UnicodeChar = text[i] };
            records[2 * i + 1] = records[2 * i] with { bKeyDown = 0 };
        }

        fixed (INPUT_RECORD* p = records)
        {
            Assert.NotEqual(0, WriteConsoleInputW(input, p, (uint)records.Length, out uint written));
            Assert.Equal((uint)records.Length, written);
        }
    }

    private static unsafe string ReadConsoleOutput(IntPtr output, COORD position, int length)
    {
        char* buffer = stackalloc char[length];
        Assert.NotEqual(0, ReadConsoleOutputCharacterW(output, buffer, (uint)length, position, out uint read));
        return new string(buffer, 0, (int)read);
    }

    private const int STD_INPUT_HANDLE = -10;
    private const int STD_OUTPUT_HANDLE = -11;
    private const uint GENERIC_READ = 0x80000000;
    private const uint GENERIC_WRITE = 0x40000000;
    private const uint FILE_SHARE_READ = 0x1;
    private const uint FILE_SHARE_WRITE = 0x2;
    private const uint OPEN_EXISTING = 3;
    private const ushort KEY_EVENT = 0x1;
    private static readonly IntPtr INVALID_HANDLE_VALUE = -1;

    [StructLayout(LayoutKind.Sequential)]
    private struct COORD
    {
        public short X;
        public short Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CONSOLE_SCREEN_BUFFER_INFO
    {
        public COORD dwSize;
        public COORD dwCursorPosition;
        public ushort wAttributes;
        public short srWindowLeft, srWindowTop, srWindowRight, srWindowBottom;
        public COORD dwMaximumWindowSize;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUT_RECORD
    {
        [FieldOffset(0)] public ushort EventType;
        [FieldOffset(4)] public int bKeyDown;
        [FieldOffset(8)] public ushort wRepeatCount;
        [FieldOffset(10)] public ushort wVirtualKeyCode;
        [FieldOffset(12)] public ushort wVirtualScanCode;
        [FieldOffset(14)] public char UnicodeChar;
        [FieldOffset(16)] public uint dwControlKeyState;
    }

    [LibraryImport("kernel32.dll")]
    private static partial int AllocConsole();

    [LibraryImport("kernel32.dll")]
    private static partial int SetStdHandle(int nStdHandle, IntPtr hHandle);

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr CreateFileW(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    [LibraryImport("kernel32.dll")]
    private static unsafe partial int WriteConsoleInputW(IntPtr hConsoleInput, INPUT_RECORD* lpBuffer, uint nLength, out uint lpNumberOfEventsWritten);

    [LibraryImport("kernel32.dll")]
    private static partial int GetConsoleScreenBufferInfo(IntPtr hConsoleOutput, out CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);

    [LibraryImport("kernel32.dll")]
    private static unsafe partial int ReadConsoleOutputCharacterW(IntPtr hConsoleOutput, char* lpCharacter, uint nLength, COORD dwReadCoord, out uint lpNumberOfCharsRead);

    [LibraryImport("kernel32.dll")]
    public static partial uint GetConsoleCP();

    [LibraryImport("kernel32.dll")]
    public static partial uint GetConsoleOutputCP();

    [LibraryImport("kernel32.dll")]
    public static partial int FreeConsole();
}
