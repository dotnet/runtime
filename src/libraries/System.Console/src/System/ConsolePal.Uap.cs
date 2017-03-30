// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace System
{
    /// <summary>
    /// UWP does not have a concept of StandardOutput on which to print Console output.
    /// However, we don't want to throw an error for every instance of Console.WriteLine
    /// so we instead just do nothing. 
    /// </summary>
    internal sealed class NoOpStream : ConsoleStream
    {
        public NoOpStream() : base(FileAccess.Write) { }

        public override void Flush()
        {
            if (!CanWrite)
                throw Error.GetFileNotOpen(); // ObjectDisposedException so Flush fails after disposal
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateRead(buffer, offset, count); // will always throw since access = FileAccess.Write
            return -1;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateWrite(buffer, offset, count);
        }
    }

    /// <summary>
    /// The UWP ConsolePal stubs out all methods with PlatformNotSupportedExceptions. The exception to this is
    /// some methods return default values similar to those returned by Unix.
    /// 
    /// The end result of the stubs is that using the Console on UWP will fail unless input and/or output are
    /// redirected.
    /// </summary>
    internal static class ConsolePal
    {
        internal static TextReader GetOrCreateReader() { throw new PlatformNotSupportedException(); }

        internal sealed class ControlCHandlerRegistrar
        {
            internal void Register() { throw new PlatformNotSupportedException(); }

            internal void Unregister() { throw new PlatformNotSupportedException(); }
        }

        public static Stream OpenStandardInput() { throw new PlatformNotSupportedException(); }

        public static Stream OpenStandardOutput() { return new NoOpStream(); }

        public static Stream OpenStandardError() { return new NoOpStream(); }

        public static Encoding InputEncoding
        {
            get { return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false); }
        }

        public static Encoding OutputEncoding
        {
            get { return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false); }
        }

        public static bool KeyAvailable { get { throw new PlatformNotSupportedException(); } }

        public static ConsoleKeyInfo ReadKey(bool intercept) { throw new PlatformNotSupportedException(); }

        public static bool TreatControlCAsInput
        {
            get { throw new PlatformNotSupportedException(); }
            set { throw new PlatformNotSupportedException(); }
        }

        private static ConsoleColor s_trackedForegroundColor = Console.UnknownColor;
        private static ConsoleColor s_trackedBackgroundColor = Console.UnknownColor;

        public static ConsoleColor ForegroundColor
        {
            get
            {
                return s_trackedForegroundColor;
            }
            set
            {
                lock (Console.Out) // synchronize with other writers
                {
                    s_trackedForegroundColor = value;
                }
            }
        }

        public static ConsoleColor BackgroundColor
        {
            get
            {
                return s_trackedBackgroundColor;
            }
            set
            {
                lock (Console.Out) // synchronize with other writers
                {
                    s_trackedBackgroundColor = value;
                }
            }
        }

        public static void ResetColor()
        {
            lock (Console.Out) // synchronize with other writers
            {
                s_trackedForegroundColor = Console.UnknownColor;
                s_trackedBackgroundColor = Console.UnknownColor;
            }
        }

        public static bool NumberLock { get { throw new PlatformNotSupportedException(); } }

        public static bool CapsLock { get { throw new PlatformNotSupportedException(); } }

        public static int CursorSize
        {
            get { return 100; }
            set { throw new PlatformNotSupportedException(); }
        }

        public static string Title
        {
            get { throw new PlatformNotSupportedException(); }
            set { throw new PlatformNotSupportedException(); }
        }

        public static void Beep() { throw new PlatformNotSupportedException(); }

        public static void Beep(int frequency, int duration) { throw new PlatformNotSupportedException(); }

        public static void Clear() { throw new PlatformNotSupportedException(); }

        public static void SetCursorPosition(int left, int top) { throw new PlatformNotSupportedException(); }

        public static int BufferWidth
        {
            get { throw new PlatformNotSupportedException(); }
            set { throw new PlatformNotSupportedException(); }
        }

        public static int BufferHeight
        {
            get { throw new PlatformNotSupportedException(); }
            set { throw new PlatformNotSupportedException(); }
        }

        public static void SetBufferSize(int width, int height) { throw new PlatformNotSupportedException(); }

        public static int LargestWindowWidth
        {
            get { throw new PlatformNotSupportedException(); }
            set { throw new PlatformNotSupportedException(); }
        }

        public static int LargestWindowHeight
        {
            get { throw new PlatformNotSupportedException(); }
            set { throw new PlatformNotSupportedException(); }
        }

        public static int WindowLeft
        {
            get { return 0; }
            set { throw new PlatformNotSupportedException(); }
        }

        public static int WindowTop
        {
            get { return 0; }
            set { throw new PlatformNotSupportedException(); }
        }

        public static int WindowWidth
        {
            get { throw new PlatformNotSupportedException(); }
            set { throw new PlatformNotSupportedException(); }
        }

        public static int WindowHeight
        {
            get { throw new PlatformNotSupportedException(); }
            set { throw new PlatformNotSupportedException(); }
        }

        public static void SetWindowPosition(int left, int top) { throw new PlatformNotSupportedException(); }

        public static void SetWindowSize(int width, int height) { throw new PlatformNotSupportedException(); }

        public static bool CursorVisible
        {
            get { throw new PlatformNotSupportedException(); }
            set { throw new PlatformNotSupportedException(); }
        }

        public static int CursorLeft { get { throw new PlatformNotSupportedException(); } }

        public static int CursorTop { get { throw new PlatformNotSupportedException(); } }

        public static void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop) { throw new PlatformNotSupportedException(); }

        public static void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop, char sourceChar, ConsoleColor sourceForeColor, ConsoleColor sourceBackColor) { throw new PlatformNotSupportedException(); }

        public static bool IsInputRedirectedCore() { throw new PlatformNotSupportedException(); }

        public static bool IsOutputRedirectedCore() { throw new PlatformNotSupportedException(); }

        public static bool IsErrorRedirectedCore() { throw new PlatformNotSupportedException(); }

        public static void SetConsoleInputEncoding(Encoding enc) { }

        public static void SetConsoleOutputEncoding(Encoding enc) { }

        public static bool TryGetSpecialConsoleKey(char[] givenChars, int startIndex, int endIndex, out ConsoleKeyInfo key, out int keyLength) { throw new PlatformNotSupportedException(); }
    }
}
