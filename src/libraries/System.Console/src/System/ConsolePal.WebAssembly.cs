// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System
{
    internal sealed class WasmConsoleStream : ConsoleStream
    {
        private readonly SafeFileHandle _handle;

        internal WasmConsoleStream(SafeFileHandle handle, FileAccess access)
            : base(access)
        {
            _handle = handle;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _handle.Dispose();
            }
            base.Dispose(disposing);
        }

        public override int Read(byte[] buffer, int offset, int count) => throw Error.GetReadNotSupported();

        public override unsafe void Write(byte[] buffer, int offset, int count)
        {
            ValidateWrite(buffer, offset, count);

            fixed (byte* bufPtr = buffer)
            {
                Write(_handle, bufPtr + offset, count);
            }
        }

        private static unsafe void Write(SafeFileHandle fd, byte* bufPtr, int count)
        {
            while (count > 0)
            {
                int bytesWritten = Interop.Sys.Write(fd, bufPtr, count);
                if (bytesWritten < 0)
                {
                    Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                    if (errorInfo.Error == Interop.Error.EPIPE)
                    {
                        return;
                    }
                    else
                    {
                        throw Interop.GetIOException(errorInfo);
                    }
                }

                count -= bytesWritten;
                bufPtr += bytesWritten;
            }
        }

        public override void Flush()
        {
            if (_handle.IsClosed)
            {
                throw Error.GetFileNotOpen();
            }
            base.Flush();
        }
    }

    internal static class ConsolePal
    {
        internal static void EnsureConsoleInitialized() { }

        public static Stream OpenStandardInput() => throw new PlatformNotSupportedException();

        public static Stream OpenStandardOutput()
        {
            return new WasmConsoleStream(Interop.Sys.Dup(Interop.Sys.FileDescriptors.STDOUT_FILENO), FileAccess.Write);
        }

        public static Stream OpenStandardError()
        {
            return new WasmConsoleStream(Interop.Sys.Dup(Interop.Sys.FileDescriptors.STDERR_FILENO), FileAccess.Write);
        }

        public static Encoding InputEncoding => throw new PlatformNotSupportedException();

        public static void SetConsoleInputEncoding(Encoding enc) => throw new PlatformNotSupportedException();

        public static Encoding OutputEncoding => Encoding.Unicode;

        public static void SetConsoleOutputEncoding(Encoding enc) => throw new PlatformNotSupportedException();

        public static bool IsInputRedirectedCore() => false;

        public static bool IsOutputRedirectedCore() => false;

        public static bool IsErrorRedirectedCore() => false;

        internal static TextReader GetOrCreateReader() => throw new PlatformNotSupportedException();

        public static bool NumberLock => false;

        public static bool CapsLock => false;

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

        public static int CursorLeft => throw new PlatformNotSupportedException();

        public static int CursorTop => throw new PlatformNotSupportedException();

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
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public static int WindowTop
        {
            get => throw new PlatformNotSupportedException();
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

        internal sealed class ControlCHandlerRegistrar
        {
            internal ControlCHandlerRegistrar() => throw new PlatformNotSupportedException();

            internal void Register() => throw new PlatformNotSupportedException();

            internal void Unregister() => throw new PlatformNotSupportedException();
        }
    }
}
