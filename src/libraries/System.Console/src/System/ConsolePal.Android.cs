// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Runtime.InteropServices;

namespace System
{
    internal sealed unsafe class LogcatStream : ConsoleStream
    {
        public LogcatStream() : base(FileAccess.Write) {}

        public override int Read(Span<byte> buffer) => throw Error.GetReadNotSupported();

        public override unsafe void Write(ReadOnlySpan<byte> buffer)
        {
            string log = ConsolePal.OutputEncoding.GetString(buffer);
            Interop.Logcat.AndroidLogPrint(Interop.Logcat.LogLevel.Info, "DOTNET", log);
        }
    }

    internal static class ConsolePal
    {
        internal static void EnsureConsoleInitialized() { }

        public static Stream OpenStandardInput() => throw new PlatformNotSupportedException();

        public static Stream OpenStandardOutput() => new LogcatStream();

        public static Stream OpenStandardError() => new LogcatStream();

        public static Encoding InputEncoding => throw new PlatformNotSupportedException();

        public static void SetConsoleInputEncoding(Encoding enc) => throw new PlatformNotSupportedException();

        public static Encoding OutputEncoding => Encoding.Unicode;

        public static void SetConsoleOutputEncoding(Encoding enc) => throw new PlatformNotSupportedException();

        public static bool IsInputRedirectedCore() => false;

        public static bool IsOutputRedirectedCore() => false;

        public static bool IsErrorRedirectedCore() => false;

        internal static TextReader GetOrCreateReader() => throw new PlatformNotSupportedException();

        public static bool NumberLock => throw new PlatformNotSupportedException();

        public static bool CapsLock => throw new PlatformNotSupportedException();

        public static bool KeyAvailable => false;

        public static ConsoleKeyInfo ReadKey(bool intercept) => throw new PlatformNotSupportedException();

        public static bool TreatControlCAsInput
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public static ConsoleColor BackgroundColor
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public static ConsoleColor ForegroundColor
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public static void ResetColor() => throw new PlatformNotSupportedException();

        public static int CursorSize
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public static bool CursorVisible
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public static (int Left, int Top) GetCursorPosition() => throw new PlatformNotSupportedException();

        public static string Title
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public static void Beep() => throw new PlatformNotSupportedException();

        public static void Beep(int frequency, int duration) => throw new PlatformNotSupportedException();

        public static void MoveBufferArea(int sourceLeft, int sourceTop,
            int sourceWidth, int sourceHeight, int targetLeft, int targetTop,
            char sourceChar, ConsoleColor sourceForeColor,
            ConsoleColor sourceBackColor) => throw new PlatformNotSupportedException();

        public static void Clear() => throw new PlatformNotSupportedException();

        public static void SetCursorPosition(int left, int top) => throw new PlatformNotSupportedException();

        public static int BufferWidth
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public static int BufferHeight
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public static void SetBufferSize(int width, int height) => throw new PlatformNotSupportedException();

        public static int LargestWindowWidth => throw new PlatformNotSupportedException();

        public static int LargestWindowHeight => throw new PlatformNotSupportedException();

        public static int WindowLeft
        {
            get => 0;
            set => throw new PlatformNotSupportedException();
        }

        public static int WindowTop
        {
            get => 0;
            set => throw new PlatformNotSupportedException();
        }

        public static int WindowWidth
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public static int WindowHeight
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public static void SetWindowPosition(int left, int top) => throw new PlatformNotSupportedException();

        public static void SetWindowSize(int width, int height) => throw new PlatformNotSupportedException();
    }
}
