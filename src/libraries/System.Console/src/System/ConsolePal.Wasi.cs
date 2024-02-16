// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;
#pragma warning disable IDE0060

namespace System
{
    // Provides Unix-based support for System.Console.
    //
    // NOTE: The test class reflects over this class to run the tests due to limitations in
    //       the test infrastructure that prevent OS-specific builds of test binaries. If you
    //       change any of the class / struct / function names, parameters, etc then you need
    //       to also change the test class.
    internal static partial class ConsolePal
    {
        public static Stream OpenStandardInput()
        {
            return new UnixConsoleStream(Interop.Sys.FileDescriptors.STDIN_FILENO, FileAccess.Read,
                                         useReadLine: !Console.IsInputRedirected);
        }

        public static Stream OpenStandardOutput()
        {
            return new UnixConsoleStream(Interop.Sys.FileDescriptors.STDOUT_FILENO, FileAccess.Write);
        }

        public static Stream OpenStandardError()
        {
            return new UnixConsoleStream(Interop.Sys.FileDescriptors.STDERR_FILENO, FileAccess.Write);
        }

        public static Encoding InputEncoding
        {
            get { return GetConsoleEncoding(); }
        }

        public static Encoding OutputEncoding
        {
            get { return GetConsoleEncoding(); }
        }

        internal static TextReader GetOrCreateReader()
        {
            Stream inputStream = OpenStandardInput();
            return inputStream == Stream.Null ?
                StreamReader.Null :
                new StreamReader(
                    stream: inputStream,
                    encoding: Console.InputEncoding,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: Console.ReadBufferSize,
                    leaveOpen: true);
        }

        public static bool KeyAvailable => false;

        public static ConsoleKeyInfo ReadKey(bool intercept) => throw new PlatformNotSupportedException();

        public static bool TreatControlCAsInput
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public static ConsoleColor ForegroundColor
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }
        public static ConsoleColor BackgroundColor
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }
        public static void ResetColor() => throw new PlatformNotSupportedException();

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
            set => throw new PlatformNotSupportedException();
        }

        public static void Beep() => throw new PlatformNotSupportedException();

        public static void Clear() => throw new PlatformNotSupportedException();
        public static void SetCursorPosition(int left, int top) => throw new PlatformNotSupportedException();
        public static bool IsInputRedirectedCore() => true;
        public static bool IsOutputRedirectedCore() => true;
        public static bool IsErrorRedirectedCore() => true;

        public static int BufferWidth
        {
            get { return WindowWidth; }
            set { throw new PlatformNotSupportedException(); }
        }

        public static int BufferHeight
        {
            get { return WindowHeight; }
            set { throw new PlatformNotSupportedException(); }
        }

        public static int LargestWindowWidth
        {
            get { return WindowWidth; }
        }

        public static int LargestWindowHeight
        {
            get { return WindowHeight; }
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
            get
            {
                GetWindowSize(out int width, out _);
                return width;
            }
            set => SetWindowSize(value, WindowHeight);
        }

        public static int WindowHeight
        {
            get
            {
                GetWindowSize(out _, out int height);
                return height;
            }
            set => SetWindowSize(WindowWidth, value);
        }

        private static void GetWindowSize(out int width, out int height)
        {
            throw new PlatformNotSupportedException();
        }

        public static void SetWindowSize(int width, int height)
        {
            throw new PlatformNotSupportedException();
        }

        public static bool CursorVisible
        {
            get { throw new PlatformNotSupportedException(); }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public static (int Left, int Top) GetCursorPosition()
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>Creates an encoding from the current environment.</summary>
        /// <returns>The encoding.</returns>
        private static Encoding GetConsoleEncoding()
        {
            Encoding? enc = EncodingHelper.GetEncodingFromCharset();
            return enc != null ?
                enc.RemovePreamble() :
                Encoding.Default;
        }

#pragma warning disable IDE0060
        public static void Beep(int frequency, int duration)
        {
            throw new PlatformNotSupportedException();
        }

        public static void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop)
        {
            throw new PlatformNotSupportedException();
        }

        public static void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop, char sourceChar, ConsoleColor sourceForeColor, ConsoleColor sourceBackColor)
        {
            throw new PlatformNotSupportedException();
        }

        public static void SetBufferSize(int width, int height)
        {
            throw new PlatformNotSupportedException();
        }

        public static void SetConsoleInputEncoding(Encoding enc)
        {
            // No-op.
            // There is no good way to set the terminal console encoding.
        }

        public static void SetConsoleOutputEncoding(Encoding enc)
        {
            // No-op.
            // There is no good way to set the terminal console encoding.
        }

        public static void SetWindowPosition(int left, int top)
        {
            throw new PlatformNotSupportedException();
        }

#pragma warning restore IDE0060

        internal static void EnsureConsoleInitialized()
        {
        }

        /// <summary>Reads data from the file descriptor into the buffer.</summary>
        /// <param name="fd">The file descriptor.</param>
        /// <param name="buffer">The buffer to read into.</param>
        /// <returns>The number of bytes read, or an exception if there's an error.</returns>
        private static unsafe int Read(SafeFileHandle fd, Span<byte> buffer)
        {
            fixed (byte* bufPtr = buffer)
            {
                int result = Interop.CheckIo(Interop.Sys.Read(fd, bufPtr, buffer.Length));
                Debug.Assert(result <= buffer.Length);
                return result;
            }
        }

        internal static unsafe void WriteFromConsoleStream(SafeFileHandle fd, ReadOnlySpan<byte> buffer)
        {
            EnsureConsoleInitialized();

            lock (Console.Out) // synchronize with other writers
            {
                Write(fd, buffer);
            }
        }

        /// <summary>Writes data from the buffer into the file descriptor.</summary>
        /// <param name="fd">The file descriptor.</param>
        /// <param name="buffer">The buffer from which to write data.</param>
        private static unsafe void Write(SafeFileHandle fd, ReadOnlySpan<byte> buffer)
        {
            fixed (byte* p = buffer)
            {
                byte* bufPtr = p;
                int count = buffer.Length;
                while (count > 0)
                {
                    int bytesWritten = Interop.Sys.Write(fd, bufPtr, count);
                    if (bytesWritten < 0)
                    {
                        Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                        if (errorInfo.Error == Interop.Error.EPIPE)
                        {
                            // Broken pipe... likely due to being redirected to a program
                            // that ended, so simply pretend we were successful.
                            return;
                        }
                        else if (errorInfo.Error == Interop.Error.EAGAIN) // aka EWOULDBLOCK
                        {
                            // May happen if the file handle is configured as non-blocking.
                            // In that case, we need to wait to be able to write and then
                            // try again. We poll, but don't actually care about the result,
                            // only the blocking behavior, and thus ignore any poll errors
                            // and loop around to do another write (which may correctly fail
                            // if something else has gone wrong).
                            Interop.Sys.Poll(fd, Interop.PollEvents.POLLOUT, Timeout.Infinite, out Interop.PollEvents triggered);
                            continue;
                        }
                        else
                        {
                            // Something else... fail.
                            throw Interop.GetExceptionForIoErrno(errorInfo);
                        }
                    }

                    count -= bytesWritten;
                    bufPtr += bytesWritten;
                }
            }
        }
    }
}
