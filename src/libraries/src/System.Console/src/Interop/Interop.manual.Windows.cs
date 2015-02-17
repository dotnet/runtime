// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal static IntPtr InvalidHandleValue = new IntPtr(-1);

    internal const int STD_INPUT_HANDLE = -10;
    internal const int STD_OUTPUT_HANDLE = -11;
    internal const int STD_ERROR_HANDLE = -12;

    internal const int ERROR_SUCCESS = 0x0;
    internal const int ERROR_NO_DATA = 0xE8;
    internal const int ERROR_BROKEN_PIPE = 0x6D;
    internal const int ERROR_FILE_NOT_FOUND = 0x2;
    internal const int ERROR_PATH_NOT_FOUND = 0x3;
    internal const int ERROR_ACCESS_DENIED = 0x5;
    internal const int ERROR_ALREADY_EXISTS = 0xB7;
    internal const int ERROR_FILENAME_EXCED_RANGE = 0xCE;  // filename too long.
    internal const int ERROR_SHARING_VIOLATION = 0x20;
    internal const int ERROR_INVALID_PARAMETER = 0x57;
    internal const int ERROR_FILE_EXISTS = 0x50;
    internal const int ERROR_OPERATION_ABORTED = 0x3E3;  // 995; For IO Cancellation
    internal const int ERROR_INVALID_HANDLE = 0x6;

    internal const int FILE_TYPE_PIPE = 0x0003;

    [StructLayoutAttribute(LayoutKind.Sequential)]
    internal struct CONSOLE_SCREEN_BUFFER_INFO
    {
        internal COORD dwSize;
        internal COORD dwCursorPosition;
        internal short wAttributes;
        internal SMALL_RECT srWindow;
        internal COORD dwMaximumWindowSize;
    }

    internal partial struct COORD
    {
        public short X;
        public short Y;
    }

    internal partial struct SMALL_RECT
    {
        public short Left;
        public short Top;
        public short Right;
        public short Bottom;
    }

    internal partial class mincore
    {
        internal enum Color : short
        {
            Black = 0,
            ForegroundBlue = 0x1,
            ForegroundGreen = 0x2,
            ForegroundRed = 0x4,
            ForegroundYellow = 0x6,
            ForegroundIntensity = 0x8,
            BackgroundBlue = 0x10,
            BackgroundGreen = 0x20,
            BackgroundRed = 0x40,
            BackgroundYellow = 0x60,
            BackgroundIntensity = 0x80,

            ForegroundMask = 0xf,
            BackgroundMask = 0xf0,
            ColorMask = 0xff
        }

        [DllImport("api-ms-win-core-processenvironment-l1-1-0.dll", SetLastError = true)]
        internal static extern IntPtr GetStdHandle(int nStdHandle);  // param is NOT a handle, but it returns one!

        [DllImport("api-ms-win-core-file-l1-1-0.dll", SetLastError = true)]
        internal static unsafe extern int WriteFile(
            IntPtr handle,
            byte* bytes,
            int numBytesToWrite,
            out int numBytesWritten,
            IntPtr mustBeZero);

        [DllImport("api-ms-win-core-file-l1-1-0.dll", SetLastError = true)]
        unsafe internal static extern int ReadFile(
            IntPtr handle,
            byte* bytes,
            int numBytesToRead,
            out int numBytesRead,
            IntPtr mustBeZero);

        [DllImport("api-ms-win-core-console-l1-1-0.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "ReadConsoleW")]
        internal static unsafe extern bool ReadConsole(
            IntPtr hConsoleInput,
            Byte* lpBuffer,
            Int32 nNumberOfCharsToRead,
            out Int32 lpNumberOfCharsRead,
            IntPtr pInputControl);

        [DllImport("api-ms-win-core-console-l1-1-0.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "WriteConsoleW")]
        internal static unsafe extern bool WriteConsole(
            IntPtr hConsoleOutput,
            Byte* lpBuffer,
            Int32 nNumberOfCharsToWrite,
            out Int32 lpNumberOfCharsWritten,
            IntPtr lpReservedMustBeNull);

        [DllImport("api-ms-win-core-console-l2-1-0.dll", SetLastError = true)]
        internal extern static int SetConsoleTextAttribute(
                    IntPtr hConsoleOutput,
                    short wAttributes);

        [DllImport("api-ms-win-core-console-l2-1-0.dll", SetLastError = true)]
        internal static extern bool GetConsoleScreenBufferInfo(
            IntPtr hConsoleOutput,
            out Interop.CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);

        [DllImport("api-ms-win-core-file-l1-1-0.dll", SetLastError = true)]
        internal extern static uint GetFileType(IntPtr hFile);

        [DllImport("api-ms-win-core-console-l1-1-0.dll", SetLastError = true)]
        internal extern static uint GetConsoleOutputCP();

        [DllImport("api-ms-win-core-console-l1-1-0.dll", SetLastError = true)]
        internal extern static uint GetConsoleCP();
    }
}
