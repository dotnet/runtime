﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        public static Stream OpenStandardInput()
        {
            return new UnixConsoleStream(SafeFileHandle.Open(() => Interop.Sys.Dup(Interop.Sys.FileDescriptors.STDIN_FILENO)), FileAccess.Read);
        }

        public static Stream OpenStandardOutput()
        {
            return new UnixConsoleStream(SafeFileHandle.Open(() => Interop.Sys.Dup(Interop.Sys.FileDescriptors.STDOUT_FILENO)), FileAccess.Write);
        }

        public static Stream OpenStandardError()
        {
            return new UnixConsoleStream(SafeFileHandle.Open(() => Interop.Sys.Dup(Interop.Sys.FileDescriptors.STDERR_FILENO)), FileAccess.Write);
        }

        public static Encoding InputEncoding
        {
            get { return GetConsoleEncoding(); }
        }

        public static Encoding OutputEncoding
        {
            get { return GetConsoleEncoding(); }
        }

        private static readonly object s_stdInReaderSyncObject = new object();
        private static SyncTextReader s_stdInReader;
        private const int DefaultBufferSize = 255;

        private static SyncTextReader StdInReader
        {
            get
            {
                EnsureInitialized();

                return Volatile.Read(ref s_stdInReader) ??
                    Console.EnsureInitialized(
                        ref s_stdInReader,
                        () => SyncTextReader.GetSynchronizedTextReader(
                            new StdInStreamReader(
                                stream: OpenStandardInput(),
                                encoding: InputEncoding,
                                bufferSize: DefaultBufferSize)));
            }
        }

        private const int DefaultConsoleBufferSize = 256; // default size of buffer used in stream readers/writers
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
                        encoding: ConsolePal.InputEncoding,
                        detectEncodingFromByteOrderMarks: false,
                        bufferSize: DefaultConsoleBufferSize,
                        leaveOpen: true)
                        );
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

            ConsoleKeyInfo keyInfo = StdInReader.ReadKey();
            if (!intercept) Console.Write(keyInfo.KeyChar);

            return keyInfo;
        }

        private const ConsoleColor UnknownColor = (ConsoleColor)(-1);
        private static ConsoleColor s_trackedForegroundColor = UnknownColor;
        private static ConsoleColor s_trackedBackgroundColor = UnknownColor;

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
                s_trackedForegroundColor = UnknownColor;
                s_trackedBackgroundColor = UnknownColor;
                WriteResetColorString();
            }
        }

        public static string Title
        {
            get { throw new PlatformNotSupportedException(); }
            set
            {
                if (Console.IsOutputRedirected)
                    return;

                string titleFormat = TerminalBasicInfo.Instance.TitleFormat;
                if (!string.IsNullOrEmpty(titleFormat))
                {
                    string ansiStr = TermInfo.ParameterizedStrings.Evaluate(titleFormat, value);
                    WriteStdoutAnsiString(ansiStr);
                }
            }
        }

        public static void Beep()
        {
            if (!Console.IsOutputRedirected)
            {
                WriteStdoutAnsiString(TerminalBasicInfo.Instance.BellFormat);
            }
        }

        public static void Clear()
        {
            if (!Console.IsOutputRedirected)
            {
                WriteStdoutAnsiString(TerminalBasicInfo.Instance.ClearFormat);
            }
        }

        public static void SetCursorPosition(int left, int top)
        {
            if (Console.IsOutputRedirected)
                return;

            string cursorAddressFormat = TerminalBasicInfo.Instance.CursorAddressFormat;
            if (!string.IsNullOrEmpty(cursorAddressFormat))
            {
                string ansiStr = TermInfo.ParameterizedStrings.Evaluate(cursorAddressFormat, top, left);
                WriteStdoutAnsiString(ansiStr);
            }
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
                Interop.Sys.WinSize winsize;
                return Interop.Sys.GetWindowSize(out winsize) == 0 ?
                    winsize.Col :
                    TerminalBasicInfo.Instance.ColumnFormat;
            }
            set { throw new PlatformNotSupportedException(); }
        }

        public static int WindowHeight
        {
            get
            {
                Interop.Sys.WinSize winsize;
                return Interop.Sys.GetWindowSize(out winsize) == 0 ?
                    winsize.Row :
                    TerminalBasicInfo.Instance.LinesFormat;
            }
            set { throw new PlatformNotSupportedException(); }
        }

        public static bool CursorVisible
        {
            get { throw new PlatformNotSupportedException(); }
            set
            {
                if (!Console.IsOutputRedirected)
                {
                    WriteStdoutAnsiString(value ?
                        TerminalBasicInfo.Instance.CursorVisibleFormat :
                        TerminalBasicInfo.Instance.CursorInvisibleFormat);
                }
            }
        }

        // TODO: It's quite expensive to use the request/response protocol each time CursorLeft/Top is accessed.
        // We should be able to (mostly) track the position of the cursor in locals, doing the request/response infrequently.

        public static int CursorLeft
        {
            get
            {
                int left, top;
                GetCursorPosition(out left, out top);
                return left;
            }
        }

        public static int CursorTop
        {
            get
            {
                int left, top;
                GetCursorPosition(out left, out top);
                return top;
            }
        }

        /// <summary>Gets the current cursor position.  This involves both writing to stdout and reading stdin.</summary>
        private static unsafe void GetCursorPosition(out int left, out int top)
        {
            left = top = 0;

            // Getting the cursor position involves both writing out a request string and
            // parsing a response string from the terminal.  So if anything is redirected, bail.
            if (Console.IsInputRedirected || Console.IsOutputRedirected)
                return;

            // Get the cursor position request format string.
            string cpr = TerminalBasicInfo.Instance.CursorPositionRequestFormat;
            if (string.IsNullOrEmpty(cpr))
                return;

            // Synchronize with all other stdin readers.  We need to do this in case multiple threads are
            // trying to read/write concurrently, and to minimize the chances of resulting conflicts.
            // This does mean that Console.get_CursorLeft/Top can't be used concurrently Console.Read*, etc.;
            // attempting to do so will block one of them until the other completes, but in doing so we prevent
            // one thread's get_CursorLeft/Top from providing input to the other's Console.Read*.
            lock (StdInReader) 
            {
                // Write out the cursor position request.
                WriteStdoutAnsiString(cpr);

                // Read the response.  There's a race condition here if the user is typing,
                // or if other threads are accessing the console; there's relatively little
                // we can do about that, but we try not to lose any data.
                StdInStreamReader r = StdInReader.Inner;
                const int BufferSize = 1024;
                byte* bytes = stackalloc byte[BufferSize];

                int bytesRead = 0, i = 0;

                // Response expected in the form "\ESC[row;colR".  However, user typing concurrently
                // with the request/response sequence can result in other characters, and potentially
                // other escape sequences (e.g. for an arrow key) being entered concurrently with
                // the response.  To avoid garbage showing up in the user's input, we are very liberal
                // with regards to eating all input from this point until all aspects of the sequence
                // have been consumed.  

                // Find the ESC as the start of the sequence.
                ReadStdinUnbufferedUntil(r, bytes, BufferSize, ref bytesRead, ref i, b => b == 0x1B);
                i++; // move past the ESC

                // Find the '['
                ReadStdinUnbufferedUntil(r, bytes, BufferSize, ref bytesRead, ref i, b => b == '[');

                // Find the first Int32 and parse it.
                ReadStdinUnbufferedUntil(r, bytes, BufferSize, ref bytesRead, ref i, b => IsDigit((char)b));
                int row = ParseInt32(bytes, bytesRead, ref i);
                if (row >= 1) top = row - 1;

                // Find the second Int32 and parse it.
                ReadStdinUnbufferedUntil(r, bytes, BufferSize, ref bytesRead, ref i, b => IsDigit((char)b));
                int col = ParseInt32(bytes, bytesRead, ref i);
                if (col >= 1) left = col - 1;

                // Find the ending 'R'
                ReadStdinUnbufferedUntil(r, bytes, BufferSize, ref bytesRead, ref i, b => b == 'R');
            }
        }

        /// <summary>Reads from the stdin reader, unbuffered, until the specified condition is met.</summary>
        private static unsafe void ReadStdinUnbufferedUntil(
            StdInStreamReader reader, 
            byte* buffer, int bufferSize, 
            ref int bytesRead, ref int pos, 
            Func<byte, bool> condition)
        {
            while (true)
            {
                for (; pos < bytesRead && !condition(buffer[pos]); pos++) ;
                if (pos < bytesRead) return;

                bytesRead = reader.ReadStdinUnbuffered(buffer, bufferSize);
                pos = 0;
            }
        }

        /// <summary>Parses the Int32 at the specified position in the buffer.</summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="bufferSize">The length of the buffer.</param>
        /// <param name="pos">The current position in the buffer.</param>
        /// <returns>The parsed result, or 0 if nothing could be parsed.</returns>
        private static unsafe int ParseInt32(byte* buffer, int bufferSize, ref int pos)
        {
            int result = 0;
            for (; pos < bufferSize; pos++)
            {
                char c = (char)buffer[pos];
                if (!IsDigit(c)) break;
                result = (result * 10) + (c - '0');
            }
            return result;
        }

        /// <summary>Gets whether the specified character is a digit 0-9.</summary>
        private static bool IsDigit(char c) { return c >= '0' && c <= '9'; }

        /// <summary>
        /// Gets whether the specified file descriptor was redirected.
        /// It's considered redirected if it doesn't refer to a terminal.
        /// </summary>
        private static bool IsHandleRedirected(int fd)
        {
            return !Interop.Sys.IsATty(fd);
        }

        /// <summary>
        /// Gets whether Console.In is redirected.
        /// We approximate the behaviorby checking whether the underlying stream is our UnixConsoleStream and it's wrapping a character device.
        /// </summary>
        public static bool IsInputRedirectedCore()
        {
            return IsHandleRedirected(Interop.Sys.FileDescriptors.STDIN_FILENO);
        }

        /// <summary>Gets whether Console.Out is redirected.
        /// We approximate the behaviorby checking whether the underlying stream is our UnixConsoleStream and it's wrapping a character device.
        /// </summary>
        public static bool IsOutputRedirectedCore()
        {
            return IsHandleRedirected(Interop.Sys.FileDescriptors.STDOUT_FILENO);
        }

        /// <summary>Gets whether Console.Error is redirected.
        /// We approximate the behaviorby checking whether the underlying stream is our UnixConsoleStream and it's wrapping a character device.
        /// </summary>
        public static bool IsErrorRedirectedCore()
        {
            return IsHandleRedirected(Interop.Sys.FileDescriptors.STDERR_FILENO);
        }

        /// <summary>Creates an encoding from the current environment.</summary>
        /// <returns>The encoding.</returns>
        private static Encoding GetConsoleEncoding()
        {
            string charset = GetCharset();
            if (charset != null)
            {
                // Try to use an encoding that matches the current charset
                try { return new ConsoleEncoding(Encoding.GetEncoding(charset)); }
                catch { } // unknown charset, or arbitrary exceptions thrown from providers
            }
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        }

        /// <summary>Environment variables that should be checked, in order, for locale.</summary>
        /// <remarks>
        /// One of these environment variables should contain a string of a form consistent with
        /// the X/Open Portability Guide syntax:
        ///     language[territory][.charset][@modifier]
        /// We're interested in the charset, as it specifies the encoding used
        /// for the console.
        /// </remarks>
        private static readonly string[] LocaleEnvVars = { "LC_ALL", "LC_MESSAGES", "LANG" }; // this ordering codifies the lookup rules prescribed by POSIX

        /// <summary>Gets the current charset name from the environment.</summary>
        /// <returns>The charset name if found; otherwise, null.</returns>
        private static string GetCharset()
        {
            // Find the first of the locale environment variables that's set.
            string locale = null;
            foreach (string envVar in LocaleEnvVars)
            {
                locale = Environment.GetEnvironmentVariable(envVar);
                if (!string.IsNullOrWhiteSpace(locale)) break;
            }

            // If we found one, try to parse it.
            // The locale string is expected to be of a form that matches the
            // X/Open Portability Guide syntax: language[_territory][.charset][@modifier]
            if (locale != null)
            {
                // Does it contain the optional charset?
                int dotPos = locale.IndexOf('.');
                if (dotPos >= 0)
                {
                    dotPos++;
                    int atPos = locale.IndexOf('@', dotPos + 1);

                    // return the charset from the locale, stripping off everything else
                    string charset = atPos < dotPos ?
                        locale.Substring(dotPos) :                // no modifier
                        locale.Substring(dotPos, atPos - dotPos); // has modifier
                    return charset.ToLowerInvariant();
                }
            }

            // no charset found; the default will be used
            return null;
        }

        /// <summary>
        /// Refreshes the foreground and background colors in use by the terminal by resetting
        /// the colors and then reissuing commands for both foreground and background, if necessary.
        /// Before doing so, the <paramref name="toChange"/> ref is changed to <paramref name="value"/>
        /// if <paramref name="value"/> is valid.
        /// </summary>
        private static void RefreshColors(ref ConsoleColor toChange, ConsoleColor value)
        {
            if (((int)value & ~0xF) != 0 && value != UnknownColor)
            {
                throw new ArgumentException(SR.Arg_InvalidConsoleColor);
            }

            lock (Console.Out)
            {
                toChange = value; // toChange is either s_trackedForegroundColor or s_trackedBackgroundColor

                WriteResetColorString();

                if (s_trackedForegroundColor != UnknownColor)
                {
                    WriteSetColorString(foreground: true, color: s_trackedForegroundColor);
                }

                if (s_trackedBackgroundColor != UnknownColor)
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
            string formatString = foreground ? TerminalColorInfo.Instance.ForegroundFormat : TerminalColorInfo.Instance.BackgroundFormat;
            if (!string.IsNullOrEmpty(formatString))
            {
                int maxColors = TerminalColorInfo.Instance.MaxColors; // often 8 or 16; 0 is invalid
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
                WriteStdoutAnsiString(TerminalColorInfo.Instance.ResetFormat);
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

            int minRange = TerminalKeyInfo.Instance.MinKeyLength;
            if (unprocessedCharCount >= minRange)
            {
                int maxRange = Math.Min(unprocessedCharCount, TerminalKeyInfo.Instance.MaxKeyLength);

                for (int i = maxRange; i >= minRange; i--)
                {
                    var currentString = new StringOrCharArray(givenChars, startIndex, i);

                    // Check if the string prefix matches.
                    if (TerminalKeyInfo.Instance.KeyFormatToConsoleKey.TryGetValue(currentString, out key))
                    {
                        keyLength = currentString.Length;
                        return true;
                    }
                }
            }

            key = default(ConsoleKeyInfo);
            keyLength = 0;
            return false;
        }

        /// <summary>Whether keypad_xmit has already been written out to the terminal.</summary>
        private static volatile bool s_initialized;

        /// <summary>Ensures that the console has been initialized for reading.</summary>
        private static void EnsureInitialized()
        {
            if (!s_initialized)
            {
                EnsureInitializedCore(); // factored out for inlinability
            }
        }

        /// <summary>Ensures that the console has been initialized for reading.</summary>
        private static void EnsureInitializedCore()
        {
            lock (Console.Out) // ensure that writing the ANSI string and setting initialized to true are done atomically
            {
                if (!s_initialized)
                {
                    // Ensure the console is configured appropriately
                    Interop.Sys.InitializeConsole();

                    // Make sure it's in application mode
                    if (!Console.IsOutputRedirected)
                    {
                        WriteStdoutAnsiString(TerminalKeyInfo.Instance.KeypadXmit);
                    }

                    s_initialized = true;
                }
            }
        }

        /// <summary>Provides a cache of color information sourced from terminfo.</summary>
        private struct TerminalColorInfo
        {
            /// <summary>The format string to use to change the foreground color.</summary>
            public string ForegroundFormat;
            /// <summary>The format string to use to change the background color.</summary>
            public string BackgroundFormat;
            /// <summary>The format string to use to reset the foreground and background colors.</summary>
            public string ResetFormat;
            /// <summary>The maximum number of colors supported by the terminal.</summary>
            public int MaxColors;

            /// <summary>The cached instance.</summary>
            public static TerminalColorInfo Instance { get { return s_instance.Value; } }

            private TerminalColorInfo(TermInfo.Database db)
            {
                ForegroundFormat = db != null ? db.GetString(TermInfo.Database.SetAnsiForegroundIndex) : string.Empty;
                BackgroundFormat = db != null ? db.GetString(TermInfo.Database.SetAnsiBackgroundIndex) : string.Empty;
                ResetFormat = db != null ?
                    db.GetString(TermInfo.Database.OrigPairsIndex) ??
                    db.GetString(TermInfo.Database.OrigColorsIndex)
                    : string.Empty;

                int maxColors = db != null ? db.GetNumber(TermInfo.Database.MaxColorsIndex) : 0;
                MaxColors = // normalize to either the full range of all ANSI colors, just the dark ones, or none
                    maxColors >= 16 ? 16 :
                    maxColors >= 8 ? 8 :
                    0;
            }

            /// <summary>Lazy initialization of the terminal color information.</summary>
            private static Lazy<TerminalColorInfo> s_instance = new Lazy<TerminalColorInfo>(() =>
            {
                TermInfo.Database db = TermInfo.Database.Instance; // Could be null if TERM is set to a file that doesn't exist
                return new TerminalColorInfo(db);
            }, isThreadSafe: true);
        }

        internal struct TerminalBasicInfo
        {
            /// <summary>The no. of columns in a format.</summary>
            public int ColumnFormat;
            /// <summary>The no. of lines in a format.</summary>
            public int LinesFormat;
            /// <summary>The format string to use to make cursor visible.</summary>
            public string CursorVisibleFormat;
            /// <summary>The format string to use to make cursor invisible</summary>
            public string CursorInvisibleFormat;
            /// <summary>The format string to use to set the window title.</summary>
            public string TitleFormat;
            /// <summary>The format string to use for an audible bell.</summary>
            public string BellFormat;
            /// <summary>The format string to use to clear the terminal.</summary>
            public string ClearFormat;
            /// <summary>The format string to use to set the position of the cursor.</summary>
            public string CursorAddressFormat;
            /// <summary>The format string to use to move the cursor to the left.</summary>
            public string CursorLeftFormat;
            /// <summary>The format string for "user string 7", interpreted to be a cursor position request.</summary>
            /// <remarks>
            /// This should be <see cref="KnownCursorPositionRequestFormat"/>, but we use the format string as a way to 
            /// guess whether the terminal will actually support the request/response protocol.
            /// </remarks>
            public string CursorPositionRequestFormat;
            /// <summary>Well-known CPR format.</summary>
            private const string KnownCursorPositionRequestFormat = "\x1B[6n";

            /// <summary>The cached instance.</summary>
            public static TerminalBasicInfo Instance { get { return s_instance.Value; } }

            private TerminalBasicInfo(TermInfo.Database db)
            {
                BellFormat = db != null ? db.GetString(TermInfo.Database.BellIndex) : string.Empty;
                ClearFormat = db != null ? db.GetString(TermInfo.Database.ClearIndex) : string.Empty;
                ColumnFormat = db != null ? db.GetNumber(TermInfo.Database.ColumnIndex) : 0;
                LinesFormat = db != null ? db.GetNumber(TermInfo.Database.LinesIndex) : 0;
                CursorVisibleFormat = db != null ? db.GetString(TermInfo.Database.CursorVisibleIndex) : string.Empty;
                CursorInvisibleFormat = db != null ? db.GetString(TermInfo.Database.CursorInvisibleIndex) : string.Empty;
                CursorAddressFormat = db != null ? db.GetString(TermInfo.Database.CursorAddressIndex) : string.Empty;
                CursorLeftFormat = db != null ? db.GetString(TermInfo.Database.CursorLeftIndex) : string.Empty;
                TitleFormat = GetTitleFormat(db);
                CursorPositionRequestFormat = db != null && db.GetString(TermInfo.Database.CursorPositionRequest) == KnownCursorPositionRequestFormat ?
                    KnownCursorPositionRequestFormat : 
                    string.Empty;
            }

            private static string GetTitleFormat(TermInfo.Database db)
            {
                if (db == null)
                {
                    return string.Empty;
                }

                // Try to get the format string from tsl/fsl and use it if they're available
                string tsl = db.GetString(TermInfo.Database.ToStatusLineIndex);
                string fsl = db.GetString(TermInfo.Database.FromStatusLineIndex);
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

            /// <summary>Lazy initialization of the terminal basic information.</summary>
            private static Lazy<TerminalBasicInfo> s_instance = new Lazy<TerminalBasicInfo>(() =>
            {
                TermInfo.Database db = TermInfo.Database.Instance; // Could be null if TERM is set to a file that doesn't exist
                return new TerminalBasicInfo(db);
            }, isThreadSafe: true);
        }

        /// <summary>Provides a cache of color information sourced from terminfo.</summary>
        private struct TerminalKeyInfo
        {
            /// <summary>
            /// The dictionary of keystring to ConsoleKeyInfo.
            /// Only some members of the ConsoleKeyInfo are used; in particular, the actual char is ignored.
            /// </summary>
            public Dictionary<StringOrCharArray, ConsoleKeyInfo> KeyFormatToConsoleKey;
            /// <summary> Max key length </summary>
            public int MaxKeyLength;
            /// <summary> Min key length </summary>
            public int MinKeyLength;
            /// <summary>The ANSI string used to enter "application" / "keypad transmit" mode.</summary>
            public string KeypadXmit;

            /// <summary>The cached instance.</summary>
            public static TerminalKeyInfo Instance { get { return s_instance.Value; } }

            private void AddKey(TermInfo.Database db, int keyId, ConsoleKey key)
            {
                AddKey(db, keyId, key, shift: false, alt: false, control: false);
            }

            private void AddKey(TermInfo.Database db, int keyId, ConsoleKey key, bool shift, bool alt, bool control)
            {
                string keyFormat = db.GetString(keyId);
                if (!string.IsNullOrEmpty(keyFormat))
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
                string keyFormat = db.GetExtendedString(extendedName);
                if (!string.IsNullOrEmpty(keyFormat))
                    KeyFormatToConsoleKey[keyFormat] = new ConsoleKeyInfo('\0', key, shift, alt, control);
            }

            private TerminalKeyInfo(TermInfo.Database db)
            {
                KeyFormatToConsoleKey = new Dictionary<StringOrCharArray, ConsoleKeyInfo>();
                MaxKeyLength = MinKeyLength = 0;
                KeypadXmit = string.Empty;

                if (db != null)
                {
                    KeypadXmit = db.GetString(TermInfo.Database.KeypadXmit);

                    AddKey(db, TermInfo.Database.KeyF1, ConsoleKey.F1);
                    AddKey(db, TermInfo.Database.KeyF2, ConsoleKey.F2);
                    AddKey(db, TermInfo.Database.KeyF3, ConsoleKey.F3);
                    AddKey(db, TermInfo.Database.KeyF4, ConsoleKey.F4);
                    AddKey(db, TermInfo.Database.KeyF5, ConsoleKey.F5);
                    AddKey(db, TermInfo.Database.KeyF6, ConsoleKey.F6);
                    AddKey(db, TermInfo.Database.KeyF7, ConsoleKey.F7);
                    AddKey(db, TermInfo.Database.KeyF8, ConsoleKey.F8);
                    AddKey(db, TermInfo.Database.KeyF9, ConsoleKey.F9);
                    AddKey(db, TermInfo.Database.KeyF10, ConsoleKey.F10);
                    AddKey(db, TermInfo.Database.KeyF11, ConsoleKey.F11);
                    AddKey(db, TermInfo.Database.KeyF12, ConsoleKey.F12);
                    AddKey(db, TermInfo.Database.KeyF13, ConsoleKey.F13);
                    AddKey(db, TermInfo.Database.KeyF14, ConsoleKey.F14);
                    AddKey(db, TermInfo.Database.KeyF15, ConsoleKey.F15);
                    AddKey(db, TermInfo.Database.KeyF16, ConsoleKey.F16);
                    AddKey(db, TermInfo.Database.KeyF17, ConsoleKey.F17);
                    AddKey(db, TermInfo.Database.KeyF18, ConsoleKey.F18);
                    AddKey(db, TermInfo.Database.KeyF19, ConsoleKey.F19);
                    AddKey(db, TermInfo.Database.KeyF20, ConsoleKey.F20);
                    AddKey(db, TermInfo.Database.KeyF21, ConsoleKey.F21);
                    AddKey(db, TermInfo.Database.KeyF22, ConsoleKey.F22);
                    AddKey(db, TermInfo.Database.KeyF23, ConsoleKey.F23);
                    AddKey(db, TermInfo.Database.KeyF24, ConsoleKey.F24);
                    AddKey(db, TermInfo.Database.KeyBackspace, ConsoleKey.Backspace);
                    AddKey(db, TermInfo.Database.KeyBackTab, ConsoleKey.Tab, shift: true, alt: false, control: false);
                    AddKey(db, TermInfo.Database.KeyBegin, ConsoleKey.Home);
                    AddKey(db, TermInfo.Database.KeyClear, ConsoleKey.Clear);
                    AddKey(db, TermInfo.Database.KeyDelete, ConsoleKey.Delete);
                    AddKey(db, TermInfo.Database.KeyDown, ConsoleKey.DownArrow);
                    AddKey(db, TermInfo.Database.KeyEnd, ConsoleKey.End);
                    AddKey(db, TermInfo.Database.KeyEnter, ConsoleKey.Enter);
                    AddKey(db, TermInfo.Database.KeyHelp, ConsoleKey.Help);
                    AddKey(db, TermInfo.Database.KeyHome, ConsoleKey.Home);
                    AddKey(db, TermInfo.Database.KeyInsert, ConsoleKey.Insert);
                    AddKey(db, TermInfo.Database.KeyLeft, ConsoleKey.LeftArrow);
                    AddKey(db, TermInfo.Database.KeyPageDown, ConsoleKey.PageDown);
                    AddKey(db, TermInfo.Database.KeyPageUp, ConsoleKey.PageUp);
                    AddKey(db, TermInfo.Database.KeyPrint, ConsoleKey.Print);
                    AddKey(db, TermInfo.Database.KeyRight, ConsoleKey.RightArrow);
                    AddKey(db, TermInfo.Database.KeyScrollForward, ConsoleKey.PageDown, shift: true, alt: false, control: false);
                    AddKey(db, TermInfo.Database.KeyScrollReverse, ConsoleKey.PageUp, shift: true, alt: false, control: false);
                    AddKey(db, TermInfo.Database.KeySBegin, ConsoleKey.Home, shift: true, alt: false, control: false);
                    AddKey(db, TermInfo.Database.KeySDelete, ConsoleKey.Delete, shift: true, alt: false, control: false);
                    AddKey(db, TermInfo.Database.KeySHome, ConsoleKey.Home, shift: true, alt: false, control: false);
                    AddKey(db, TermInfo.Database.KeySelect, ConsoleKey.Select);
                    AddKey(db, TermInfo.Database.KeySLeft, ConsoleKey.LeftArrow, shift: true, alt: false, control: false);
                    AddKey(db, TermInfo.Database.KeySPrint, ConsoleKey.Print, shift: true, alt: false, control: false);
                    AddKey(db, TermInfo.Database.KeySRight, ConsoleKey.RightArrow, shift: true, alt: false, control: false);
                    AddKey(db, TermInfo.Database.KeyUp, ConsoleKey.UpArrow);

                    AddPrefixKey(db, "kLFT", ConsoleKey.LeftArrow);
                    AddPrefixKey(db, "kRIT", ConsoleKey.RightArrow);
                    AddPrefixKey(db, "kUP", ConsoleKey.UpArrow);
                    AddPrefixKey(db, "kDN", ConsoleKey.DownArrow);
                    AddPrefixKey(db, "kDC", ConsoleKey.Delete);
                    AddPrefixKey(db, "kEND", ConsoleKey.End);
                    AddPrefixKey(db, "kHOM", ConsoleKey.Home);
                    AddPrefixKey(db, "kNXT", ConsoleKey.PageDown);
                    AddPrefixKey(db, "kPRV", ConsoleKey.PageUp);

                    MaxKeyLength = KeyFormatToConsoleKey.Keys.Max(key => key.Length);
                    MinKeyLength = KeyFormatToConsoleKey.Keys.Min(key => key.Length);
                }
            }

            /// <summary>Lazy initialization of the terminal key information.</summary>
            private static Lazy<TerminalKeyInfo> s_instance = new Lazy<TerminalKeyInfo>(() =>
            {
                TermInfo.Database db = TermInfo.Database.Instance; // Could be null if TERM is set to a file that doesn't exist
                return new TerminalKeyInfo(db);
            }, isThreadSafe: true);
        }

        /// <summary>Reads data from the file descriptor into the buffer.</summary>
        /// <param name="fd">The file descriptor.</param>
        /// <param name="buffer">The buffer to read into.</param>
        /// <param name="offset">The offset at which to start writing into the buffer.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <returns>The number of bytes read, or a negative value if there's an error.</returns>
        internal static unsafe int Read(int fd, byte[] buffer, int offset, int count)
        {
            fixed (byte* bufPtr = buffer)
            {
                int result;
                while (Interop.CheckIo(result = Interop.Sys.Read(fd, (byte*)bufPtr + offset, count))) ;
                Debug.Assert(result <= count);
                return result;
            }
        }

        /// <summary>Writes data from the buffer into the file descriptor.</summary>
        /// <param name="fd">The file descriptor.</param>
        /// <param name="buffer">The buffer from which to write data.</param>
        /// <param name="offset">The offset at which the data to write starts in the buffer.</param>
        /// <param name="count">The number of bytes to write.</param>
        private static unsafe void Write(int fd, byte[] buffer, int offset, int count)
        {
            fixed (byte* bufPtr = buffer)
            {
                Write(fd, bufPtr + offset, count);
            }
        }

        private static unsafe void Write(int fd, byte* bufPtr, int count)
        {
            while (count > 0)
            {
                int bytesWritten = Interop.Sys.Write(fd, bufPtr, count);
                if (bytesWritten < 0)
                {
                    Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                    if (errorInfo.Error == Interop.Error.EINTR)
                    {
                        // Interrupted... try again.
                        continue;
                    }
                    else if (errorInfo.Error == Interop.Error.EPIPE)
                    {
                        // Broken pipe... likely due to being redirected to a program
                        // that ended, so simply pretend we were successful.
                        return;
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

        /// <summary>Writes a terminfo-based ANSI escape string to stdout.</summary>
        /// <param name="value">The string to write.</param>
        private static unsafe void WriteStdoutAnsiString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            // Except for extremely rare cases, ANSI escape strings should be very short.
            const int StackAllocThreshold = 256;
            if (value.Length <= StackAllocThreshold)
            {
                int dataLen = Encoding.UTF8.GetMaxByteCount(value.Length);
                byte* data = stackalloc byte[dataLen];
                fixed (char* chars = value)
                {
                    int bytesToWrite = Encoding.UTF8.GetBytes(chars, value.Length, data, dataLen);
                    Debug.Assert(bytesToWrite <= dataLen);

                    lock (Console.Out) // synchronize with other writers
                    {
                        Write(Interop.Sys.FileDescriptors.STDOUT_FILENO, data, bytesToWrite);
                    }
                }
            }
            else
            {
                byte[] data = Encoding.UTF8.GetBytes(value);
                lock (Console.Out) // synchronize with other writers
                {
                    Write(Interop.Sys.FileDescriptors.STDOUT_FILENO, data, 0, data.Length);
                }
            }
        }

        /// <summary>Provides a stream to use for Unix console input or output.</summary>
        private sealed class UnixConsoleStream : ConsoleStream
        {
            /// <summary>The file descriptor for the opened file.</summary>
            private readonly SafeFileHandle _handle;
            /// <summary>The type of the underlying file descriptor.</summary>
            internal readonly int _handleType;

            /// <summary>Initialize the stream.</summary>
            /// <param name="handle">The file handle wrapped by this stream.</param>
            /// <param name="access">FileAccess.Read or FileAccess.Write.</param>
            internal UnixConsoleStream(SafeFileHandle handle, FileAccess access)
                : base(access)
            {
                Debug.Assert(handle != null, "Expected non-null console handle");
                Debug.Assert(!handle.IsInvalid, "Expected valid console handle");
                _handle = handle;

                // Determine the type of the descriptor (e.g. regular file, character file, pipe, etc.)
                bool gotFd = false;
                try
                {
                    _handle.DangerousAddRef(ref gotFd);
                    Interop.Sys.FileStatus buf;
                    _handleType =
                        Interop.Sys.FStat((int)_handle.DangerousGetHandle(), out buf) == 0 ?
                            (buf.Mode & Interop.Sys.FileTypes.S_IFMT) :
                            Interop.Sys.FileTypes.S_IFREG; // if something goes wrong, don't fail, just say it's a regular file
                }
                finally
                {
                    if (gotFd)
                        _handle.DangerousRelease();
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _handle.Dispose();
                }
                base.Dispose(disposing);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                ValidateRead(buffer, offset, count);
                bool gotFd = false;
                try
                {
                    _handle.DangerousAddRef(ref gotFd);
                    return ConsolePal.Read((int)_handle.DangerousGetHandle(), buffer, offset, count);
                }
                finally
                {
                    if (gotFd)
                    {
                        _handle.DangerousRelease();
                    }
                }
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                ValidateWrite(buffer, offset, count);
                bool gotFd = false;
                try
                {
                    _handle.DangerousAddRef(ref gotFd);
                    ConsolePal.Write((int)_handle.DangerousGetHandle(), buffer, offset, count);
                }
                finally
                {
                    if (gotFd)
                    {
                        _handle.DangerousRelease();
                    }
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

        internal sealed class ControlCHandlerRegistrar
        {
            private static readonly Interop.Sys.CtrlCallback _handler = 
                c => Console.HandleBreakEvent(c == Interop.Sys.CtrlCode.Break ? ConsoleSpecialKey.ControlBreak : ConsoleSpecialKey.ControlC);
            private bool _handlerRegistered;

            internal void Register()
            {
                Debug.Assert(!_handlerRegistered);
                if (!Interop.Sys.RegisterForCtrl(_handler))
                {
                    throw Interop.GetExceptionForIoErrno(Interop.Sys.GetLastErrorInfo());
                }
                _handlerRegistered = true;
            }

            internal void Unregister()
            {
                Debug.Assert(_handlerRegistered);
                _handlerRegistered = false;
                Interop.Sys.UnregisterForCtrl();
            }
        }

    }
}
