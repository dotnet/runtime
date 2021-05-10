// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace System
{
    // Provides Unix-based support for System.Console.
    //
    // NOTE: The test class reflects over this class to run the tests due to limitations in
    //       the test infrastructure that prevent OS-specific builds of test binaries. If you
    //       change any of the class / struct / function names, parameters, etc then you need
    //       to also change the test class.
    internal static class ConsolePal
    {
        // StdInReader is only used when input isn't redirected and we're working
        // with an interactive terminal.  In that case, performance isn't critical
        // and we can use a smaller buffer to minimize working set.
        private const int InteractiveBufferSize = 255;

        // For performance we cache Cursor{Left,Top} and Window{Width,Height}.
        // These values must be read/written under lock (Console.Out).
        // We also need to invalidate these values when certain signals occur.
        // We don't want to take the lock in the signal handling thread for this.
        // Instead, we set a flag. Before reading a cached value, a call to CheckTerminalSettingsInvalidated
        // will invalidate the cached values if a signal has occured.
        private static int s_cursorVersion; // Gets incremented each time the cursor position changed.
                                            // Used to synchronize between lock (Console.Out) blocks.
        private static int s_cursorLeft;    // Cached CursorLeft, -1 when invalid.
        private static int s_cursorTop;     // Cached CursorTop, invalid when s_cursorLeft == -1.
        private static int s_windowWidth;   // Cached WindowWidth, -1 when invalid.
        private static int s_windowHeight;  // Cached WindowHeight, invalid when s_windowWidth == -1.
        private static int s_invalidateCachedSettings = 1; // Tracks whether we should invalidate the cached settings.

        public static Stream OpenStandardInput()
        {
            return new UnixConsoleStream(SafeFileHandleHelper.Open(() => Interop.Sys.Dup(Interop.Sys.FileDescriptors.STDIN_FILENO)), FileAccess.Read,
                                         useReadLine: !Console.IsInputRedirected);
        }

        public static Stream OpenStandardOutput()
        {
            return new UnixConsoleStream(SafeFileHandleHelper.Open(() => Interop.Sys.Dup(Interop.Sys.FileDescriptors.STDOUT_FILENO)), FileAccess.Write);
        }

        public static Stream OpenStandardError()
        {
            return new UnixConsoleStream(SafeFileHandleHelper.Open(() => Interop.Sys.Dup(Interop.Sys.FileDescriptors.STDERR_FILENO)), FileAccess.Write);
        }

        public static Encoding InputEncoding
        {
            get { return GetConsoleEncoding(); }
        }

        public static Encoding OutputEncoding
        {
            get { return GetConsoleEncoding(); }
        }

        private static SyncTextReader? s_stdInReader;

        internal static SyncTextReader StdInReader
        {
            get
            {
                return Volatile.Read(ref s_stdInReader) ?? EnsureInitialized();

                static SyncTextReader EnsureInitialized()
                {
                    EnsureConsoleInitialized();

                    SyncTextReader reader = SyncTextReader.GetSynchronizedTextReader(
                                                new StdInReader(
                                                    encoding: Console.InputEncoding,
                                                    bufferSize: InteractiveBufferSize));

                    // Don't overwrite a set reader.
                    // The reader doesn't own resources, so we don't need to dispose
                    // when it was already set.
                    Interlocked.CompareExchange(ref s_stdInReader, reader, null);

                    return s_stdInReader;
                }
            }
        }

        internal static TextReader GetOrCreateReader()
        {
            if (Console.IsInputRedirected)
            {
                Stream inputStream = OpenStandardInput();
                return SyncTextReader.GetSynchronizedTextReader(
                    inputStream == Stream.Null ?
                    StreamReader.Null :
                    new StreamReader(
                        stream: inputStream,
                        encoding: Console.InputEncoding,
                        detectEncodingFromByteOrderMarks: false,
                        bufferSize: Console.ReadBufferSize,
                        leaveOpen: true));
            }
            else
            {
                return StdInReader;
            }
        }

        public static bool KeyAvailable { get { return StdInReader.KeyAvailable; } }

        public static ConsoleKeyInfo ReadKey(bool intercept)
        {
            if (Console.IsInputRedirected)
            {
                // We could leverage Console.Read() here however
                // windows fails when stdin is redirected.
                throw new InvalidOperationException(SR.InvalidOperation_ConsoleReadKeyOnFile);
            }

            bool previouslyProcessed;
            ConsoleKeyInfo keyInfo = StdInReader.ReadKey(out previouslyProcessed);

            // Replace the '\n' char for Enter by '\r' to match Windows behavior.
            if (keyInfo.Key == ConsoleKey.Enter && keyInfo.KeyChar == '\n')
            {
                bool shift   = (keyInfo.Modifiers & ConsoleModifiers.Shift)   != 0;
                bool alt     = (keyInfo.Modifiers & ConsoleModifiers.Alt)     != 0;
                bool control = (keyInfo.Modifiers & ConsoleModifiers.Control) != 0;
                keyInfo = new ConsoleKeyInfo('\r', keyInfo.Key, shift, alt, control);
            }

            if (!intercept && !previouslyProcessed && keyInfo.KeyChar != '\0')
            {
                Console.Write(keyInfo.KeyChar);
            }
            return keyInfo;
        }

        public static bool TreatControlCAsInput
        {
            get
            {
                if (Console.IsInputRedirected)
                    return false;

                EnsureConsoleInitialized();
                return Interop.Sys.GetSignalForBreak() == 0;
            }
            set
            {
                if (!Console.IsInputRedirected)
                {
                    EnsureConsoleInitialized();
                    if (Interop.Sys.SetSignalForBreak(Convert.ToInt32(!value)) == 0)
                        throw Interop.GetExceptionForIoErrno(Interop.Sys.GetLastErrorInfo());
                }
            }
        }

        private static ConsoleColor s_trackedForegroundColor = Console.UnknownColor;
        private static ConsoleColor s_trackedBackgroundColor = Console.UnknownColor;

        public static ConsoleColor ForegroundColor
        {
            get { return s_trackedForegroundColor; }
            set { RefreshColors(ref s_trackedForegroundColor, value); }
        }

        public static ConsoleColor BackgroundColor
        {
            get { return s_trackedBackgroundColor; }
            set { RefreshColors(ref s_trackedBackgroundColor, value); }
        }

        public static void ResetColor()
        {
            lock (Console.Out) // synchronize with other writers
            {
                s_trackedForegroundColor = Console.UnknownColor;
                s_trackedBackgroundColor = Console.UnknownColor;
                WriteResetColorString();
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
            set
            {
                if (Console.IsOutputRedirected)
                    return;

                string? titleFormat = TerminalFormatStrings.Instance.Title;
                if (!string.IsNullOrEmpty(titleFormat))
                {
                    string ansiStr = TermInfo.ParameterizedStrings.Evaluate(titleFormat, value);
                    WriteStdoutAnsiString(ansiStr, mayChangeCursorPosition: false);
                }
            }
        }

        public static void Beep()
        {
            if (!Console.IsOutputRedirected)
            {
                WriteStdoutAnsiString(TerminalFormatStrings.Instance.Bell, mayChangeCursorPosition: false);
            }
        }

        public static void Beep(int frequency, int duration)
        {
            throw new PlatformNotSupportedException();
        }

        public static void Clear()
        {
            if (!Console.IsOutputRedirected)
            {
                WriteStdoutAnsiString(TerminalFormatStrings.Instance.Clear);
            }
        }

        public static void SetCursorPosition(int left, int top)
        {
            if (Console.IsOutputRedirected)
                return;

            lock (Console.Out)
            {
                if (TryGetCachedCursorPosition(out int leftCurrent, out int topCurrent) &&
                    left == leftCurrent &&
                    top == topCurrent)
                {
                    return;
                }

                string? cursorAddressFormat = TerminalFormatStrings.Instance.CursorAddress;
                if (!string.IsNullOrEmpty(cursorAddressFormat))
                {
                    string ansiStr = TermInfo.ParameterizedStrings.Evaluate(cursorAddressFormat, top, left);
                    WriteStdoutAnsiString(ansiStr);
                }

                SetCachedCursorPosition(left, top);
            }
        }

        private static void SetCachedCursorPosition(int left, int top, int? version = null)
        {
            Debug.Assert(left >= 0);

            bool setPosition = version == null || version == s_cursorVersion;

            if (setPosition)
            {
                s_cursorLeft = left;
                s_cursorTop = top;
                s_cursorVersion++;
            }
            else
            {
                InvalidateCachedCursorPosition();
            }
        }

        private static void InvalidateCachedCursorPosition()
        {
            s_cursorLeft = -1;
            s_cursorVersion++;
        }

        private static bool TryGetCachedCursorPosition(out int left, out int top)
        {
            // Invalidate before reading cached values.
            CheckTerminalSettingsInvalidated();

            bool hasCachedCursorPosition = s_cursorLeft >= 0;
            if (hasCachedCursorPosition)
            {
                left = s_cursorLeft;
                top = s_cursorTop;
            }
            else
            {
                left = 0;
                top = 0;
            }
            return hasCachedCursorPosition;
        }

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

        public static void SetBufferSize(int width, int height)
        {
            throw new PlatformNotSupportedException();
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
                GetWindowSize(out int width, out int height);
                return width;
            }
            set { throw new PlatformNotSupportedException(); }
        }

        public static int WindowHeight
        {
            get
            {
                GetWindowSize(out int width, out int height);
                return height;
            }
            set { throw new PlatformNotSupportedException(); }
        }

        private static void GetWindowSize(out int width, out int height)
        {
            lock (Console.Out)
            {
                // Invalidate before reading cached values.
                CheckTerminalSettingsInvalidated();

                if (s_windowWidth == -1)
                {
                    Interop.Sys.WinSize winsize;
                    if (Interop.Sys.GetWindowSize(out winsize) == 0)
                    {
                        s_windowWidth = winsize.Col;
                        s_windowHeight = winsize.Row;
                    }
                    else
                    {
                        s_windowWidth = TerminalFormatStrings.Instance.Columns;
                        s_windowHeight = TerminalFormatStrings.Instance.Lines;
                    }
                }
                width = s_windowWidth;
                height = s_windowHeight;
            }
        }

        public static void SetWindowPosition(int left, int top)
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
                if (!Console.IsOutputRedirected)
                {
                    WriteStdoutAnsiString(value ?
                        TerminalFormatStrings.Instance.CursorVisible :
                        TerminalFormatStrings.Instance.CursorInvisible);
                }
            }
        }

        public static (int Left, int Top) GetCursorPosition()
        {
            TryGetCursorPosition(out int left, out int top);
            return (left, top);
        }

        /// <summary>
        /// Tracks whether we've ever successfully received a response to a cursor position request (CPR).
        /// If we have, then we can be more aggressive about expecting a response to subsequent requests,
        /// e.g. using a longer timeout.
        /// </summary>
        private static bool s_everReceivedCursorPositionResponse;

        /// <summary>
        /// Tracks if this is out first attempt to send a cursor posotion request. If it is, we start the
        /// timer immediately (i.e. minChar = 0), but we use a slightly longer timeout to avoid the CPR response
        /// being written to the console.
        /// </summary>
        private static bool s_firstCursorPositionRequest = true;

        /// <summary>Gets the current cursor position.  This involves both writing to stdout and reading stdin.</summary>
        /// <param name="left">Cursor column.</param>
        /// <param name="top">Cursor row.</param>
        /// <param name="reinitializeForRead">Indicates whether this method is called as part of a on-going Read operation.</param>
        internal static bool TryGetCursorPosition(out int left, out int top, bool reinitializeForRead = false)
        {
            left = top = 0;

            // Getting the cursor position involves both writing out a request string and
            // parsing a response string from the terminal.  So if anything is redirected, bail.
            if (Console.IsInputRedirected || Console.IsOutputRedirected)
            {
                return false;
            }

            int cursorVersion;
            lock (Console.Out)
            {
                if (TryGetCachedCursorPosition(out left, out top))
                {
                    return true;
                }

                cursorVersion = s_cursorVersion;
            }

            // Create a buffer to read the response into.  We start with stack memory and grow
            // into the heap only if we need to, and we choose a limit that should be large
            // enough for the vast, vast majority of use cases, such that when we do grow, we
            // just allocate, rather than employing any complicated pooling strategy.
            int readBytesPos = 0;
            Span<byte> readBytes = stackalloc byte[256];

            // Synchronize with all other stdin readers.  We need to do this in case multiple threads are
            // trying to read/write concurrently, and to minimize the chances of resulting conflicts.
            // This does mean that Console.get_CursorLeft/Top can't be used concurrently with Console.Read*, etc.;
            // attempting to do so will block one of them until the other completes, but in doing so we prevent
            // one thread's get_CursorLeft/Top from providing input to the other's Console.Read*.
            lock (StdInReader)
            {
                // Because the CPR request/response protocol involves blocking until we get a certain
                // response from the terminal, we want to avoid doing so if we don't know the terminal
                // will definitely respond.  As such, we start with minChars == 0, which causes the
                // terminal's read timer to start immediately.  Once we've received a response for
                // a request such that we know the terminal supports the protocol, we then specify
                // minChars == 1.  With that, the timer won't start until the first character is
                // received.  This makes the mechanism more reliable when there are high latencies
                // involved in reading/writing, such as when accessing a remote system. We also extend
                // the timeout on the very first request to 15 seconds, to account for potential latency
                // before we know if we will receive a response.
                Interop.Sys.InitializeConsoleBeforeRead(minChars: (byte)(s_everReceivedCursorPositionResponse ? 1 : 0), decisecondsTimeout: (byte)(s_firstCursorPositionRequest ? 100 : 10));
                try
                {
                    // Write out the cursor position report request.
                    Debug.Assert(!string.IsNullOrEmpty(TerminalFormatStrings.CursorPositionReport));
                    WriteStdoutAnsiString(TerminalFormatStrings.CursorPositionReport, mayChangeCursorPosition: false);

                    // Read the cursor position report (CPR), of the form \ESC[row;colR. This is not
                    // as easy as it sounds.  Prior to the CPR having been supplied to stdin, other
                    // user input could have come in and be available to read first from stdin.  Plus,
                    // that user input could include escape sequences, and those escape sequences could
                    // have a prefix very similar to that of the CPR (e.g. other escape sequences start
                    // with \ESC + '['.  It's also possible that some terminal implementations may not
                    // write the CPR to stdin atomically, such that the CPR could have other user input
                    // in the middle of it, and that user input could have escape sequences!  Handling
                    // that last case is very challenging, and rare, so we don't try, but we do need to
                    // handle the rest.  The min bar here is doing something reasonable, which may include
                    // giving up and just returning default top and left values.

                    // Consume from stdin until we find all of the key markers for the CPR:
                    // \ESC, '[', ';', and 'R'.  For everything before the \ESC, it's definitely
                    // not part of the CPR sequence, so we just immediately move any such bytes
                    // over to the StdInReader's extra buffer.  From there until the end, we buffer
                    // everything into readBytes for subsequent parsing.
                    const byte Esc = 0x1B;
                    StdInReader r = StdInReader.Inner;
                    int escPos, bracketPos, semiPos, rPos;
                    if (!AppendToStdInReaderUntil(Esc, r, readBytes, ref readBytesPos, out escPos) ||
                        !BufferUntil((byte)'[', r, ref readBytes, ref readBytesPos, out bracketPos) ||
                        !BufferUntil((byte)';', r, ref readBytes, ref readBytesPos, out semiPos) ||
                        !BufferUntil((byte)'R', r, ref readBytes, ref readBytesPos, out rPos))
                    {
                        // We were unable to read everything from stdin, e.g. a timeout occurred.
                        // Since we couldn't get the complete CPR, transfer any bytes we did read
                        // back to the StdInReader's extra buffer, treating it all as user input,
                        // and exit having not computed a valid cursor position.
                        TransferBytes(readBytes.Slice(readBytesPos), r);
                        return false;
                    }

                    // At this point, readBytes starts with \ESC and ends with 'R'.
                    Debug.Assert(readBytesPos > 0 && readBytesPos <= readBytes.Length);
                    Debug.Assert(escPos == 0 && bracketPos > escPos && semiPos > bracketPos && rPos > semiPos);
                    Debug.Assert(readBytes[escPos] == Esc);
                    Debug.Assert(readBytes[bracketPos] == '[');
                    Debug.Assert(readBytes[semiPos] == ';');
                    Debug.Assert(readBytes[rPos] == 'R');

                    // There are other sequences that begin with \ESC + '[' and that might be in our sequence before
                    // the CPR, so we don't immediately trust escPos and bracketPos.  Instead, as a heuristic we trust
                    // semiPos (which we only tracked after seeing a '[' after seeing an \ESC) and search backwards from
                    // there looking for '[' and then \ESC.
                    bracketPos = readBytes.Slice(0, semiPos).LastIndexOf((byte)'[');
                    escPos = readBytes.Slice(0, bracketPos).LastIndexOf(Esc);

                    // Everything before the \ESC is transferred back to the StdInReader. As is everything
                    // between the \ESC and the '['; there really shouldn't be anything there, but we're
                    // defensive in case the CPR wasn't written atomically and something crept in.
                    TransferBytes(readBytes.Slice(0, escPos), r);
                    TransferBytes(readBytes.Slice(escPos + 1, bracketPos - (escPos + 1)), r);

                    // Now loop through all characters between the '[' and the ';' to compute the row,
                    // and then between the ';' and the 'R' to compute the column. We incorporate any
                    // digits we find, and while we shouldn't find anything else, we defensively put anything
                    // else back into the StdInReader.
                    ReadRowOrCol(bracketPos, semiPos, r, readBytes, ref top);
                    ReadRowOrCol(semiPos, rPos, r, readBytes, ref left);

                    // Mark that we've successfully received a CPR response at least once.
                    s_everReceivedCursorPositionResponse = true;
                }
                finally
                {
                    if (reinitializeForRead)
                    {
                        Interop.Sys.InitializeConsoleBeforeRead();
                    }
                    else
                    {
                        Interop.Sys.UninitializeConsoleAfterRead();
                    }
                    s_firstCursorPositionRequest = false;
                }

                static unsafe bool BufferUntil(byte toFind, StdInReader src, ref Span<byte> dst, ref int dstPos, out int foundPos)
                {
                    // Loop until we find the target byte.
                    while (true)
                    {
                        // Read the next byte from stdin.
                        byte b;
                        if (src.ReadStdin(&b, 1) != 1)
                        {
                            foundPos = -1;
                            return false;
                        }

                        // Make sure we have enough room to store the byte.
                        if (dstPos == dst.Length)
                        {
                            var tmpReadBytes = new byte[dst.Length * 2];
                            dst.CopyTo(tmpReadBytes);
                            dst = tmpReadBytes;
                        }

                        // Store the byte.
                        dst[dstPos++] = b;

                        // If this is the target, we're done.
                        if (b == toFind)
                        {
                            foundPos = dstPos - 1;
                            return true;
                        }
                    }
                }

                static unsafe bool AppendToStdInReaderUntil(byte toFind, StdInReader reader, Span<byte> foundByteDst, ref int foundByteDstPos, out int foundPos)
                {
                    // Loop until we find the target byte.
                    while (true)
                    {
                        // Read the next byte from stdin.
                        byte b;
                        if (reader.ReadStdin(&b, 1) != 1)
                        {
                            foundPos = -1;
                            return false;
                        }

                        // If it's the target byte, store it and exit.
                        if (b == toFind)
                        {
                            Debug.Assert(foundByteDstPos < foundByteDst.Length, "Should only be called when there's room for at least one byte.");
                            foundPos = foundByteDstPos;
                            foundByteDst[foundByteDstPos++] = b;
                            return true;
                        }

                        // Otherwise, push it back into the reader's extra buffer.
                        reader.AppendExtraBuffer(MemoryMarshal.CreateReadOnlySpan(ref b, 1));
                    }
                }

                static void ReadRowOrCol(int startExclusive, int endExclusive, StdInReader reader, ReadOnlySpan<byte> source, ref int result)
                {
                    int row = 0;

                    for (int i = startExclusive + 1; i < endExclusive; i++)
                    {
                        byte b = source[i];
                        if (IsDigit(b))
                        {
                            try
                            {
                                row = checked((row * 10) + (b - '0'));
                            }
                            catch (OverflowException) { }
                        }
                        else
                        {
                            reader.AppendExtraBuffer(MemoryMarshal.CreateReadOnlySpan(ref b, 1));
                        }
                    }

                    if (row >= 1)
                    {
                        result = row - 1;
                    }
                }
            }

            static void TransferBytes(ReadOnlySpan<byte> src, StdInReader dst)
            {
                for (int i = 0; i < src.Length; i++)
                {
                    dst.AppendExtraBuffer(src.Slice(i, 1));
                }
            }

            lock (Console.Out)
            {
                SetCachedCursorPosition(left, top, cursorVersion);
                return true;
            }
        }

        public static void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop)
        {
            throw new PlatformNotSupportedException();
        }

        public static void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop, char sourceChar, ConsoleColor sourceForeColor, ConsoleColor sourceBackColor)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>Gets whether the specified character is a digit 0-9.</summary>
        private static bool IsDigit(byte c) => c >= '0' && c <= '9';

        /// <summary>
        /// Gets whether the specified file descriptor was redirected.
        /// It's considered redirected if it doesn't refer to a terminal.
        /// </summary>
        private static bool IsHandleRedirected(SafeFileHandle fd)
        {
            return !Interop.Sys.IsATty(fd);
        }

        /// <summary>
        /// Gets whether Console.In is redirected.
        /// We approximate the behavior by checking whether the underlying stream is our UnixConsoleStream and it's wrapping a character device.
        /// </summary>
        public static bool IsInputRedirectedCore()
        {
            return IsHandleRedirected(Interop.Sys.FileDescriptors.STDIN_FILENO);
        }

        /// <summary>Gets whether Console.Out is redirected.
        /// We approximate the behavior by checking whether the underlying stream is our UnixConsoleStream and it's wrapping a character device.
        /// </summary>
        public static bool IsOutputRedirectedCore()
        {
            return IsHandleRedirected(Interop.Sys.FileDescriptors.STDOUT_FILENO);
        }

        /// <summary>Gets whether Console.Error is redirected.
        /// We approximate the behavior by checking whether the underlying stream is our UnixConsoleStream and it's wrapping a character device.
        /// </summary>
        public static bool IsErrorRedirectedCore()
        {
            return IsHandleRedirected(Interop.Sys.FileDescriptors.STDERR_FILENO);
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

        /// <summary>
        /// Refreshes the foreground and background colors in use by the terminal by resetting
        /// the colors and then reissuing commands for both foreground and background, if necessary.
        /// Before doing so, the <paramref name="toChange"/> ref is changed to <paramref name="value"/>
        /// if <paramref name="value"/> is valid.
        /// </summary>
        private static void RefreshColors(ref ConsoleColor toChange, ConsoleColor value)
        {
            if (((int)value & ~0xF) != 0 && value != Console.UnknownColor)
            {
                throw new ArgumentException(SR.Arg_InvalidConsoleColor);
            }

            lock (Console.Out)
            {
                toChange = value; // toChange is either s_trackedForegroundColor or s_trackedBackgroundColor

                WriteResetColorString();

                if (s_trackedForegroundColor != Console.UnknownColor)
                {
                    WriteSetColorString(foreground: true, color: s_trackedForegroundColor);
                }

                if (s_trackedBackgroundColor != Console.UnknownColor)
                {
                    WriteSetColorString(foreground: false, color: s_trackedBackgroundColor);
                }
            }
        }

        /// <summary>Outputs the format string evaluated and parameterized with the color.</summary>
        /// <param name="foreground">true for foreground; false for background.</param>
        /// <param name="color">The color to store into the field and to use as an argument to the format string.</param>
        private static void WriteSetColorString(bool foreground, ConsoleColor color)
        {
            // Changing the color involves writing an ANSI character sequence out to the output stream.
            // We only want to do this if we know that sequence will be interpreted by the output.
            // rather than simply displayed visibly.
            if (Console.IsOutputRedirected)
                return;

            // See if we've already cached a format string for this foreground/background
            // and specific color choice.  If we have, just output that format string again.
            int fgbgIndex = foreground ? 0 : 1;
            int ccValue = (int)color;
            string evaluatedString = s_fgbgAndColorStrings[fgbgIndex, ccValue]; // benign race
            if (evaluatedString != null)
            {
                WriteStdoutAnsiString(evaluatedString);
                return;
            }

            // We haven't yet computed a format string.  Compute it, use it, then cache it.
            string? formatString = foreground ? TerminalFormatStrings.Instance.Foreground : TerminalFormatStrings.Instance.Background;
            if (!string.IsNullOrEmpty(formatString))
            {
                int maxColors = TerminalFormatStrings.Instance.MaxColors; // often 8 or 16; 0 is invalid
                if (maxColors > 0)
                {
                    int ansiCode = _consoleColorToAnsiCode[ccValue] % maxColors;
                    evaluatedString = TermInfo.ParameterizedStrings.Evaluate(formatString, ansiCode);

                    WriteStdoutAnsiString(evaluatedString);

                    s_fgbgAndColorStrings[fgbgIndex, ccValue] = evaluatedString; // benign race
                }
            }
        }

        /// <summary>Writes out the ANSI string to reset colors.</summary>
        private static void WriteResetColorString()
        {
            // We only want to send the reset string if we're targeting a TTY device
            if (!Console.IsOutputRedirected)
            {
                WriteStdoutAnsiString(TerminalFormatStrings.Instance.Reset);
            }
        }

        /// <summary>
        /// The values of the ConsoleColor enums unfortunately don't map to the
        /// corresponding ANSI values.  We need to do the mapping manually.
        /// See http://en.wikipedia.org/wiki/ANSI_escape_code#Colors
        /// </summary>
        private static readonly int[] _consoleColorToAnsiCode = new int[]
        {
            // Dark/Normal colors
            0, // Black,
            4, // DarkBlue,
            2, // DarkGreen,
            6, // DarkCyan,
            1, // DarkRed,
            5, // DarkMagenta,
            3, // DarkYellow,
            7, // Gray,

            // Bright colors
            8,  // DarkGray,
            12, // Blue,
            10, // Green,
            14, // Cyan,
            9,  // Red,
            13, // Magenta,
            11, // Yellow,
            15  // White
        };

        /// <summary>Cache of the format strings for foreground/background and ConsoleColor.</summary>
        private static readonly string[,] s_fgbgAndColorStrings = new string[2, 16]; // 2 == fg vs bg, 16 == ConsoleColor values

        public static bool TryGetSpecialConsoleKey(char[] givenChars, int startIndex, int endIndex, out ConsoleKeyInfo key, out int keyLength)
        {
            int unprocessedCharCount = endIndex - startIndex;

            // First process special control character codes.  These override anything from terminfo.
            if (unprocessedCharCount > 0)
            {
                // Is this an erase / backspace?
                char c = givenChars[startIndex];
                if (c != s_posixDisableValue && c == s_veraseCharacter)
                {
                    key = new ConsoleKeyInfo(c, ConsoleKey.Backspace, shift: false, alt: false, control: false);
                    keyLength = 1;
                    return true;
                }
            }

            // Then process terminfo mappings.
            int minRange = TerminalFormatStrings.Instance.MinKeyFormatLength;
            if (unprocessedCharCount >= minRange)
            {
                int maxRange = Math.Min(unprocessedCharCount, TerminalFormatStrings.Instance.MaxKeyFormatLength);

                for (int i = maxRange; i >= minRange; i--)
                {
                    var currentString = new ReadOnlyMemory<char>(givenChars, startIndex, i);

                    // Check if the string prefix matches.
                    if (TerminalFormatStrings.Instance.KeyFormatToConsoleKey.TryGetValue(currentString, out key))
                    {
                        keyLength = currentString.Length;
                        return true;
                    }
                }
            }

            // Otherwise, not a known special console key.
            key = default(ConsoleKeyInfo);
            keyLength = 0;
            return false;
        }

        /// <summary>Whether keypad_xmit has already been written out to the terminal.</summary>
        private static volatile bool s_initialized;

        /// <summary>Value used to indicate that a special character code isn't available.</summary>
        internal static byte s_posixDisableValue;
        /// <summary>Special control character code used to represent an erase (backspace).</summary>
        private static byte s_veraseCharacter;
        /// <summary>Special control character that represents the end of a line.</summary>
        internal static byte s_veolCharacter;
        /// <summary>Special control character that represents the end of a line.</summary>
        internal static byte s_veol2Character;
        /// <summary>Special control character that represents the end of a file.</summary>
        internal static byte s_veofCharacter;

        /// <summary>Ensures that the console has been initialized for use.</summary>
        internal static void EnsureConsoleInitialized()
        {
            if (!s_initialized)
            {
                EnsureInitializedCore(); // factored out for inlinability
            }
        }

        /// <summary>Ensures that the console has been initialized for use.</summary>
        private static unsafe void EnsureInitializedCore()
        {
            // Initialization is only needed when input isn't redirected.
            if (Console.IsInputRedirected)
            {
                s_initialized = true;
                return;
            }

            lock (Console.Out) // ensure that writing the ANSI string and setting initialized to true are done atomically
            {
                if (!s_initialized)
                {
                    if (!Interop.Sys.InitializeTerminalAndSignalHandling())
                    {
                        throw new Win32Exception();
                    }

                    // Register a callback for signals that may invalidate our cached terminal settings.
                    // This includes: SIGCONT, SIGCHLD, SIGWINCH.
                    Interop.Sys.SetTerminalInvalidationHandler(&InvalidateTerminalSettings);

                    // Provide the native lib with the correct code from the terminfo to transition us into
                    // "application mode".  This will both transition it immediately, as well as allow
                    // the native lib later to handle signals that require re-entering the mode.
                    if (!Console.IsOutputRedirected)
                    {
                        string? keypadXmit = TerminalFormatStrings.Instance.KeypadXmit;
                        if (keypadXmit != null)
                        {
                            Interop.Sys.SetKeypadXmit(keypadXmit);
                        }
                    }

                    // Load special control character codes used for input processing
                    var controlCharacterNames = new Interop.Sys.ControlCharacterNames[4]
                    {
                        Interop.Sys.ControlCharacterNames.VERASE,
                        Interop.Sys.ControlCharacterNames.VEOL,
                        Interop.Sys.ControlCharacterNames.VEOL2,
                        Interop.Sys.ControlCharacterNames.VEOF
                    };
                    var controlCharacterValues = new byte[controlCharacterNames.Length];
                    Interop.Sys.GetControlCharacters(controlCharacterNames, controlCharacterValues, controlCharacterNames.Length, out s_posixDisableValue);
                    s_veraseCharacter = controlCharacterValues[0];
                    s_veolCharacter = controlCharacterValues[1];
                    s_veol2Character = controlCharacterValues[2];
                    s_veofCharacter = controlCharacterValues[3];

                    // Mark us as initialized
                    s_initialized = true;
                }
            }
        }

        /// <summary>Provides format strings and related information for use with the current terminal.</summary>
        internal sealed class TerminalFormatStrings
        {
            /// <summary>Gets the lazily-initialized terminal information for the terminal.</summary>
            public static TerminalFormatStrings Instance { get { return s_instance.Value; } }
            private static readonly Lazy<TerminalFormatStrings> s_instance = new Lazy<TerminalFormatStrings>(() => new TerminalFormatStrings(TermInfo.Database.ReadActiveDatabase()));

            /// <summary>The format string to use to change the foreground color.</summary>
            public readonly string? Foreground;
            /// <summary>The format string to use to change the background color.</summary>
            public readonly string? Background;
            /// <summary>The format string to use to reset the foreground and background colors.</summary>
            public readonly string? Reset;
            /// <summary>The maximum number of colors supported by the terminal.</summary>
            public readonly int MaxColors;
            /// <summary>The number of columns in a format.</summary>
            public readonly int Columns;
            /// <summary>The number of lines in a format.</summary>
            public readonly int Lines;
            /// <summary>The format string to use to make cursor visible.</summary>
            public readonly string? CursorVisible;
            /// <summary>The format string to use to make cursor invisible</summary>
            public readonly string? CursorInvisible;
            /// <summary>The format string to use to set the window title.</summary>
            public readonly string? Title;
            /// <summary>The format string to use for an audible bell.</summary>
            public readonly string? Bell;
            /// <summary>The format string to use to clear the terminal.</summary>
            public readonly string? Clear;
            /// <summary>The format string to use to set the position of the cursor.</summary>
            public readonly string? CursorAddress;
            /// <summary>The format string to use to move the cursor to the left.</summary>
            public readonly string? CursorLeft;
            /// <summary>The format string to use to clear to the end of line.</summary>
            public readonly string? ClrEol;
            /// <summary>The ANSI-compatible string for the Cursor Position report request.</summary>
            /// <remarks>
            /// This should really be in user string 7 in the terminfo file, but some terminfo databases
            /// are missing it.  As this is defined to be supported by any ANSI-compatible terminal,
            /// we assume it's available; doing so means CursorTop/Left will work even if the terminfo database
            /// doesn't contain it (as appears to be the case with e.g. screen and tmux on Ubuntu), at the risk
            /// of outputting the sequence on some terminal that's not compatible.
            /// </remarks>
            public const string CursorPositionReport = "\x1B[6n";
            /// <summary>
            /// The dictionary of keystring to ConsoleKeyInfo.
            /// Only some members of the ConsoleKeyInfo are used; in particular, the actual char is ignored.
            /// </summary>
            public readonly Dictionary<ReadOnlyMemory<char>, ConsoleKeyInfo> KeyFormatToConsoleKey =
                new Dictionary<ReadOnlyMemory<char>, ConsoleKeyInfo>(new ReadOnlyMemoryContentComparer());

            /// <summary> Max key length </summary>
            public readonly int MaxKeyFormatLength;
            /// <summary> Min key length </summary>
            public readonly int MinKeyFormatLength;
            /// <summary>The ANSI string used to enter "application" / "keypad transmit" mode.</summary>
            public readonly string? KeypadXmit;

            public TerminalFormatStrings(TermInfo.Database? db)
            {
                if (db == null)
                    return;

                KeypadXmit = db.GetString(TermInfo.WellKnownStrings.KeypadXmit);
                Foreground = db.GetString(TermInfo.WellKnownStrings.SetAnsiForeground);
                Background = db.GetString(TermInfo.WellKnownStrings.SetAnsiBackground);
                Reset = db.GetString(TermInfo.WellKnownStrings.OrigPairs) ?? db.GetString(TermInfo.WellKnownStrings.OrigColors);
                Bell = db.GetString(TermInfo.WellKnownStrings.Bell);
                Clear = db.GetString(TermInfo.WellKnownStrings.Clear);
                Columns = db.GetNumber(TermInfo.WellKnownNumbers.Columns);
                Lines = db.GetNumber(TermInfo.WellKnownNumbers.Lines);
                CursorVisible = db.GetString(TermInfo.WellKnownStrings.CursorVisible);
                CursorInvisible = db.GetString(TermInfo.WellKnownStrings.CursorInvisible);
                CursorAddress = db.GetString(TermInfo.WellKnownStrings.CursorAddress);
                CursorLeft = db.GetString(TermInfo.WellKnownStrings.CursorLeft);
                ClrEol = db.GetString(TermInfo.WellKnownStrings.ClrEol);

                Title = GetTitle(db);

                Debug.WriteLineIf(db.GetString(TermInfo.WellKnownStrings.CursorPositionReport) != CursorPositionReport,
                    "Getting the cursor position will only work if the terminal supports the CPR sequence," +
                    "but the terminfo database does not contain an entry for it.");

                int maxColors = db.GetNumber(TermInfo.WellKnownNumbers.MaxColors);
                MaxColors = // normalize to either the full range of all ANSI colors, just the dark ones, or none
                    maxColors >= 16 ? 16 :
                    maxColors >= 8 ? 8 :
                    0;

                AddKey(db, TermInfo.WellKnownStrings.KeyF1, ConsoleKey.F1);
                AddKey(db, TermInfo.WellKnownStrings.KeyF2, ConsoleKey.F2);
                AddKey(db, TermInfo.WellKnownStrings.KeyF3, ConsoleKey.F3);
                AddKey(db, TermInfo.WellKnownStrings.KeyF4, ConsoleKey.F4);
                AddKey(db, TermInfo.WellKnownStrings.KeyF5, ConsoleKey.F5);
                AddKey(db, TermInfo.WellKnownStrings.KeyF6, ConsoleKey.F6);
                AddKey(db, TermInfo.WellKnownStrings.KeyF7, ConsoleKey.F7);
                AddKey(db, TermInfo.WellKnownStrings.KeyF8, ConsoleKey.F8);
                AddKey(db, TermInfo.WellKnownStrings.KeyF9, ConsoleKey.F9);
                AddKey(db, TermInfo.WellKnownStrings.KeyF10, ConsoleKey.F10);
                AddKey(db, TermInfo.WellKnownStrings.KeyF11, ConsoleKey.F11);
                AddKey(db, TermInfo.WellKnownStrings.KeyF12, ConsoleKey.F12);
                AddKey(db, TermInfo.WellKnownStrings.KeyF13, ConsoleKey.F13);
                AddKey(db, TermInfo.WellKnownStrings.KeyF14, ConsoleKey.F14);
                AddKey(db, TermInfo.WellKnownStrings.KeyF15, ConsoleKey.F15);
                AddKey(db, TermInfo.WellKnownStrings.KeyF16, ConsoleKey.F16);
                AddKey(db, TermInfo.WellKnownStrings.KeyF17, ConsoleKey.F17);
                AddKey(db, TermInfo.WellKnownStrings.KeyF18, ConsoleKey.F18);
                AddKey(db, TermInfo.WellKnownStrings.KeyF19, ConsoleKey.F19);
                AddKey(db, TermInfo.WellKnownStrings.KeyF20, ConsoleKey.F20);
                AddKey(db, TermInfo.WellKnownStrings.KeyF21, ConsoleKey.F21);
                AddKey(db, TermInfo.WellKnownStrings.KeyF22, ConsoleKey.F22);
                AddKey(db, TermInfo.WellKnownStrings.KeyF23, ConsoleKey.F23);
                AddKey(db, TermInfo.WellKnownStrings.KeyF24, ConsoleKey.F24);
                AddKey(db, TermInfo.WellKnownStrings.KeyBackspace, ConsoleKey.Backspace);
                AddKey(db, TermInfo.WellKnownStrings.KeyBackTab, ConsoleKey.Tab, shift: true, alt: false, control: false);
                AddKey(db, TermInfo.WellKnownStrings.KeyBegin, ConsoleKey.Home);
                AddKey(db, TermInfo.WellKnownStrings.KeyClear, ConsoleKey.Clear);
                AddKey(db, TermInfo.WellKnownStrings.KeyDelete, ConsoleKey.Delete);
                AddKey(db, TermInfo.WellKnownStrings.KeyDown, ConsoleKey.DownArrow);
                AddKey(db, TermInfo.WellKnownStrings.KeyEnd, ConsoleKey.End);
                AddKey(db, TermInfo.WellKnownStrings.KeyEnter, ConsoleKey.Enter);
                AddKey(db, TermInfo.WellKnownStrings.KeyHelp, ConsoleKey.Help);
                AddKey(db, TermInfo.WellKnownStrings.KeyHome, ConsoleKey.Home);
                AddKey(db, TermInfo.WellKnownStrings.KeyInsert, ConsoleKey.Insert);
                AddKey(db, TermInfo.WellKnownStrings.KeyLeft, ConsoleKey.LeftArrow);
                AddKey(db, TermInfo.WellKnownStrings.KeyPageDown, ConsoleKey.PageDown);
                AddKey(db, TermInfo.WellKnownStrings.KeyPageUp, ConsoleKey.PageUp);
                AddKey(db, TermInfo.WellKnownStrings.KeyPrint, ConsoleKey.Print);
                AddKey(db, TermInfo.WellKnownStrings.KeyRight, ConsoleKey.RightArrow);
                AddKey(db, TermInfo.WellKnownStrings.KeyScrollForward, ConsoleKey.PageDown, shift: true, alt: false, control: false);
                AddKey(db, TermInfo.WellKnownStrings.KeyScrollReverse, ConsoleKey.PageUp, shift: true, alt: false, control: false);
                AddKey(db, TermInfo.WellKnownStrings.KeySBegin, ConsoleKey.Home, shift: true, alt: false, control: false);
                AddKey(db, TermInfo.WellKnownStrings.KeySDelete, ConsoleKey.Delete, shift: true, alt: false, control: false);
                AddKey(db, TermInfo.WellKnownStrings.KeySHome, ConsoleKey.Home, shift: true, alt: false, control: false);
                AddKey(db, TermInfo.WellKnownStrings.KeySelect, ConsoleKey.Select);
                AddKey(db, TermInfo.WellKnownStrings.KeySLeft, ConsoleKey.LeftArrow, shift: true, alt: false, control: false);
                AddKey(db, TermInfo.WellKnownStrings.KeySPrint, ConsoleKey.Print, shift: true, alt: false, control: false);
                AddKey(db, TermInfo.WellKnownStrings.KeySRight, ConsoleKey.RightArrow, shift: true, alt: false, control: false);
                AddKey(db, TermInfo.WellKnownStrings.KeyUp, ConsoleKey.UpArrow);
                AddPrefixKey(db, "kLFT", ConsoleKey.LeftArrow);
                AddPrefixKey(db, "kRIT", ConsoleKey.RightArrow);
                AddPrefixKey(db, "kUP", ConsoleKey.UpArrow);
                AddPrefixKey(db, "kDN", ConsoleKey.DownArrow);
                AddPrefixKey(db, "kDC", ConsoleKey.Delete);
                AddPrefixKey(db, "kEND", ConsoleKey.End);
                AddPrefixKey(db, "kHOM", ConsoleKey.Home);
                AddPrefixKey(db, "kNXT", ConsoleKey.PageDown);
                AddPrefixKey(db, "kPRV", ConsoleKey.PageUp);

                if (KeyFormatToConsoleKey.Count > 0)
                {
                    MaxKeyFormatLength = int.MinValue;
                    MinKeyFormatLength = int.MaxValue;

                    foreach (KeyValuePair<ReadOnlyMemory<char>, ConsoleKeyInfo> entry in KeyFormatToConsoleKey)
                    {
                        if (entry.Key.Length > MaxKeyFormatLength)
                        {
                            MaxKeyFormatLength = entry.Key.Length;
                        }
                        if (entry.Key.Length < MinKeyFormatLength)
                        {
                            MinKeyFormatLength = entry.Key.Length;
                        }
                    }
                }
            }

            private static string GetTitle(TermInfo.Database db)
            {
                // Try to get the format string from tsl/fsl and use it if they're available
                string? tsl = db.GetString(TermInfo.WellKnownStrings.ToStatusLine);
                string? fsl = db.GetString(TermInfo.WellKnownStrings.FromStatusLine);
                if (tsl != null && fsl != null)
                {
                    return tsl + "%p1%s" + fsl;
                }

                string term = db.Term;
                if (term == null)
                {
                    return string.Empty;
                }

                if (term.StartsWith("xterm", StringComparison.Ordinal)) // normalize all xterms to enable easier matching
                {
                    term = "xterm";
                }

                switch (term)
                {
                    case "aixterm":
                    case "dtterm":
                    case "linux":
                    case "rxvt":
                    case "xterm":
                        return "\x1B]0;%p1%s\x07";
                    case "cygwin":
                        return "\x1B];%p1%s\x07";
                    case "konsole":
                        return "\x1B]30;%p1%s\x07";
                    case "screen":
                        return "\x1Bk%p1%s\x1B";
                    default:
                        return string.Empty;
                }
            }

            private void AddKey(TermInfo.Database db, TermInfo.WellKnownStrings keyId, ConsoleKey key)
            {
                AddKey(db, keyId, key, shift: false, alt: false, control: false);
            }

            private void AddKey(TermInfo.Database db, TermInfo.WellKnownStrings keyId, ConsoleKey key, bool shift, bool alt, bool control)
            {
                ReadOnlyMemory<char> keyFormat = db.GetString(keyId).AsMemory();
                if (!keyFormat.IsEmpty)
                    KeyFormatToConsoleKey[keyFormat] = new ConsoleKeyInfo('\0', key, shift, alt, control);
            }

            private void AddPrefixKey(TermInfo.Database db, string extendedNamePrefix, ConsoleKey key)
            {
                AddKey(db, extendedNamePrefix + "3", key, shift: false, alt: true,  control: false);
                AddKey(db, extendedNamePrefix + "4", key, shift: true,  alt: true,  control: false);
                AddKey(db, extendedNamePrefix + "5", key, shift: false, alt: false, control: true);
                AddKey(db, extendedNamePrefix + "6", key, shift: true,  alt: false, control: true);
                AddKey(db, extendedNamePrefix + "7", key, shift: false, alt: false, control: true);
            }

            private void AddKey(TermInfo.Database db, string extendedName, ConsoleKey key, bool shift, bool alt, bool control)
            {
                ReadOnlyMemory<char> keyFormat = db.GetExtendedString(extendedName).AsMemory();
                if (!keyFormat.IsEmpty)
                    KeyFormatToConsoleKey[keyFormat] = new ConsoleKeyInfo('\0', key, shift, alt, control);
            }
        }

        /// <summary>Reads data from the file descriptor into the buffer.</summary>
        /// <param name="fd">The file descriptor.</param>
        /// <param name="buffer">The buffer to read into.</param>
        /// <returns>The number of bytes read, or a negative value if there's an error.</returns>
        internal static unsafe int Read(SafeFileHandle fd, Span<byte> buffer)
        {
            fixed (byte* bufPtr = buffer)
            {
                int result = Interop.CheckIo(Interop.Sys.Read(fd, bufPtr, buffer.Length));
                Debug.Assert(result <= buffer.Length);
                return result;
            }
        }

        /// <summary>Writes data from the buffer into the file descriptor.</summary>
        /// <param name="fd">The file descriptor.</param>
        /// <param name="buffer">The buffer from which to write data.</param>
        /// <param name="mayChangeCursorPosition">Writing this buffer may change the cursor position.</param>
        internal static unsafe void Write(SafeFileHandle fd, ReadOnlySpan<byte> buffer, bool mayChangeCursorPosition = true)
        {
            // Console initialization might emit data to stdout.
            // In order to avoid splitting user data we need to
            // complete it before any writes are performed.
            EnsureConsoleInitialized();

            fixed (byte* p = buffer)
            {
                byte* bufPtr = p;
                int count = buffer.Length;
                while (count > 0)
                {
                    int cursorVersion = mayChangeCursorPosition ? Volatile.Read(ref s_cursorVersion) : -1;

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
                    else
                    {
                        if (mayChangeCursorPosition)
                        {
                            UpdatedCachedCursorPosition(bufPtr, bytesWritten, cursorVersion);
                        }
                    }

                    count -= bytesWritten;
                    bufPtr += bytesWritten;
                }
            }
        }

        private static unsafe void UpdatedCachedCursorPosition(byte* bufPtr, int count, int cursorVersion)
        {
            lock (Console.Out)
            {
                int left, top;
                if (cursorVersion != s_cursorVersion               ||  // the cursor was changed during the write by another operation
                    !TryGetCachedCursorPosition(out left, out top) ||  // we don't have a cursor position
                    count > InteractiveBufferSize)                     // limit the amount of bytes we are willing to inspect
                {
                    InvalidateCachedCursorPosition();
                    return;
                }

                GetWindowSize(out int width, out int height);

                for (int i = 0; i < count; i++)
                {
                    byte c = bufPtr[i];
                    if (c < 127 && c >= 32) // ASCII/UTF-8 characters that take up a single position
                    {
                        IncrementX();
                    }
                    else if (c == (byte)'\r')
                    {
                        left = 0;
                    }
                    else if (c == (byte)'\n')
                    {
                        left = 0;
                        IncrementY();
                    }
                    else if (c == (byte)'\b')
                    {
                        if (left > 0)
                        {
                            left--;
                        }
                    }
                    else
                    {
                        InvalidateCachedCursorPosition();
                        return;
                    }
                }

                // We pass cursorVersion because it may have changed the earlier check by calling GetWindowSize.
                SetCachedCursorPosition(left, top, cursorVersion);

                void IncrementY()
                {
                    top++;
                    if (top >= height)
                    {
                        top = height - 1;
                    }
                }

                void IncrementX()
                {
                    left++;
                    if (left >= width)
                    {
                        left = 0;
                        IncrementY();
                    }
                }
            }
        }

        private static void CheckTerminalSettingsInvalidated()
        {
            // Register for signals that invalidate cached values.
            EnsureConsoleInitialized();

            bool invalidateSettings = Interlocked.CompareExchange(ref s_invalidateCachedSettings, 0, 1) == 1;
            if (invalidateSettings)
            {
                InvalidateCachedCursorPosition();
                s_windowWidth = -1;
            }
        }

        [UnmanagedCallersOnly]
        private static void InvalidateTerminalSettings()
        {
            Volatile.Write(ref s_invalidateCachedSettings, 1);
        }

        /// <summary>Writes a terminfo-based ANSI escape string to stdout.</summary>
        /// <param name="value">The string to write.</param>
        /// <param name="mayChangeCursorPosition">Writing this value may change the cursor position.</param>
        internal static void WriteStdoutAnsiString(string? value, bool mayChangeCursorPosition = true)
        {
            if (string.IsNullOrEmpty(value))
                return;

            Span<byte> data = stackalloc byte[0];
            if (value.Length <= 256) // except for extremely rare cases, ANSI escape strings are very short
            {
                data = stackalloc byte[Encoding.UTF8.GetMaxByteCount(value.Length)];
                int bytesToWrite = Encoding.UTF8.GetBytes(value, data);
                data = data.Slice(0, bytesToWrite);
            }
            else
            {
                data = Encoding.UTF8.GetBytes(value);
            }

            lock (Console.Out) // synchronize with other writers
            {
                Write(Interop.Sys.FileDescriptors.STDOUT_FILENO, data, mayChangeCursorPosition);
            }
        }

        /// <summary>Provides a stream to use for Unix console input or output.</summary>
        private sealed class UnixConsoleStream : ConsoleStream
        {
            /// <summary>The file descriptor for the opened file.</summary>
            private readonly SafeFileHandle _handle;

            private readonly bool _useReadLine;

            /// <summary>Initialize the stream.</summary>
            /// <param name="handle">The file handle wrapped by this stream.</param>
            /// <param name="access">FileAccess.Read or FileAccess.Write.</param>
            /// <param name="useReadLine">Use ReadLine API for reading.</param>
            internal UnixConsoleStream(SafeFileHandle handle, FileAccess access, bool useReadLine = false)
                : base(access)
            {
                Debug.Assert(handle != null, "Expected non-null console handle");
                Debug.Assert(!handle.IsInvalid, "Expected valid console handle");
                _handle = handle;
                _useReadLine = useReadLine;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _handle.Dispose();
                }
                base.Dispose(disposing);
            }

            public override int Read(Span<byte> buffer) =>
                _useReadLine ?
                    ConsolePal.StdInReader.ReadLine(buffer) :
                    ConsolePal.Read(_handle, buffer);

            public override void Write(ReadOnlySpan<byte> buffer) =>
                ConsolePal.Write(_handle, buffer);

            public override void Flush()
            {
                if (_handle.IsClosed)
                {
                    throw Error.GetFileNotOpen();
                }
                base.Flush();
            }
        }

        internal sealed class ControlCHandlerRegistrar
        {
            private bool _handlerRegistered;

            internal unsafe void Register()
            {
                EnsureConsoleInitialized();

                Debug.Assert(!_handlerRegistered);
                Interop.Sys.RegisterForCtrl(&OnBreakEvent);
                _handlerRegistered = true;
            }

            internal void Unregister()
            {
                Debug.Assert(_handlerRegistered);
                _handlerRegistered = false;
                Interop.Sys.UnregisterForCtrl();
            }

            [UnmanagedCallersOnly]
            private static void OnBreakEvent(Interop.Sys.CtrlCode ctrlCode)
            {
                // This is called on the native signal handling thread. We need to move to another thread so
                // signal handling is not blocked. Otherwise we may get deadlocked when the handler depends
                // on work triggered from the signal handling thread.
                // We use a new thread rather than queueing to the ThreadPool in order to prioritize handling
                // in case the ThreadPool is saturated.
                Thread handlerThread = new Thread(HandleBreakEvent)
                {
                    IsBackground = true,
                    Name = ".NET Console Break"
                };
                handlerThread.Start(ctrlCode);
            }

            private static void HandleBreakEvent(object? state)
            {
                Debug.Assert(state != null);
                var ctrlCode = (Interop.Sys.CtrlCode)state;
                ConsoleSpecialKey controlKey = (ctrlCode == Interop.Sys.CtrlCode.Break ? ConsoleSpecialKey.ControlBreak : ConsoleSpecialKey.ControlC);
                bool cancel = Console.HandleBreakEvent(controlKey);
                if (!cancel)
                {
                    Interop.Sys.RestoreAndHandleCtrl(ctrlCode);
                }
            }
        }

        private sealed class ReadOnlyMemoryContentComparer : IEqualityComparer<ReadOnlyMemory<char>>
        {
            public bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y) =>
                x.Span.SequenceEqual(y.Span);

            public int GetHashCode(ReadOnlyMemory<char> obj) =>
                string.GetHashCode(obj.Span);
        }
    }
}
