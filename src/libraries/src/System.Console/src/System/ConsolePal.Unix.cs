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
            return new UnixConsoleStream(SafeFileHandleHelper.Open(() => Interop.Sys.Dup(Interop.Sys.FileDescriptors.STDIN_FILENO)), FileAccess.Read);
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
                            new StdInReader(
                                encoding: new ConsoleEncoding(Console.InputEncoding), // This ensures no prefix is written to the stream.
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
                        encoding: new ConsoleEncoding(Console.InputEncoding), // This ensures no prefix is written to the stream.
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

            bool previouslyProcessed;
            ConsoleKeyInfo keyInfo = StdInReader.ReadKey(out previouslyProcessed);
            if (!intercept && !previouslyProcessed) Console.Write(keyInfo.KeyChar);
            return keyInfo;
        }

        public static bool TreatControlCAsInput
        {
            get
            {
                if (Console.IsInputRedirected)
                    return false;

                EnsureInitialized();
                return !Interop.Sys.GetSignalForBreak();
            }
            set
            {
                if (!Console.IsInputRedirected)
                {
                    EnsureInitialized();
                    if (!Interop.Sys.SetSignalForBreak(signalForBreak: !value))
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

                string titleFormat = TerminalFormatStrings.Instance.Title;
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
                WriteStdoutAnsiString(TerminalFormatStrings.Instance.Bell);
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

            string cursorAddressFormat = TerminalFormatStrings.Instance.CursorAddress;
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

        public static void SetBufferSize(int width, int height)
        {
            throw new PlatformNotSupportedException();
        }

        public static int LargestWindowWidth
        {
            get { return WindowWidth; }
            set { throw new PlatformNotSupportedException(); }
        }

        public static int LargestWindowHeight
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
                    TerminalFormatStrings.Instance.Columns;
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
                    TerminalFormatStrings.Instance.Lines;
            }
            set { throw new PlatformNotSupportedException(); }
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
            Debug.Assert(!string.IsNullOrEmpty(TerminalFormatStrings.CursorPositionReport));

            // Synchronize with all other stdin readers.  We need to do this in case multiple threads are
            // trying to read/write concurrently, and to minimize the chances of resulting conflicts.
            // This does mean that Console.get_CursorLeft/Top can't be used concurrently Console.Read*, etc.;
            // attempting to do so will block one of them until the other completes, but in doing so we prevent
            // one thread's get_CursorLeft/Top from providing input to the other's Console.Read*.
            lock (StdInReader) 
            {
                Interop.Sys.InitializeConsoleBeforeRead(minChars: 0, decisecondsTimeout: 10);
                try
                {
                    // Write out the cursor position report request.
                    WriteStdoutAnsiString(TerminalFormatStrings.CursorPositionReport);

                    // Read the cursor position report reponse, of the form \ESC[row;colR. There's a race
                    // condition here if the user is typing, or if other threads are accessing the console;
                    // to try to avoid losing such data, we push unexpected inputs into the stdin buffer, but
                    // even with that, there's a a potential that we could misinterpret data from the user as
                    // being part of the cursor position response.  This is inherent to the nature of the protocol.
                    StdInReader r = StdInReader.Inner;
                    byte b;

                    while (true) // \ESC
                    {
                        if (r.ReadStdin(&b, 1) != 1) return;
                        if (b == 0x1B) break;
                        r.AppendExtraBuffer(&b, 1);
                    }

                    while (true) // [
                    {
                        if (r.ReadStdin(&b, 1) != 1) return;
                        if (b == '[') break;
                        r.AppendExtraBuffer(&b, 1);
                    }

                    try
                    {
                        int row = 0;
                        while (true) // row until ';'
                        {
                            if (r.ReadStdin(&b, 1) != 1) return;
                            if (b == ';') break;
                            if (IsDigit((char)b))
                            {
                                row = checked((row * 10) + (b - '0'));
                            }
                            else
                            {
                                r.AppendExtraBuffer(&b, 1);
                            }
                        }
                        if (row >= 1) top = row - 1;

                        int col = 0;
                        while (true) // col until 'R'
                        {
                            if (r.ReadStdin(&b, 1) == 0) return;
                            if (b == 'R') break;
                            if (IsDigit((char)b))
                            {
                                col = checked((col * 10) + (b - '0'));
                            }
                            else
                            {
                                r.AppendExtraBuffer(&b, 1);
                            }
                        }
                        if (col >= 1) left = col - 1;
                    }
                    catch (OverflowException) { return; }
                }
                finally
                {
                    Interop.Sys.UninitializeConsoleAfterRead();
                }
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
        private static bool IsDigit(char c) { return c >= '0' && c <= '9'; }

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
            Encoding enc = EncodingHelper.GetEncodingFromCharset();
            return enc ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
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
            string formatString = foreground ? TerminalFormatStrings.Instance.Foreground : TerminalFormatStrings.Instance.Background;
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
                    var currentString = new StringOrCharArray(givenChars, startIndex, i);

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
        private static void EnsureInitialized()
        {
            if (!s_initialized)
            {
                EnsureInitializedCore(); // factored out for inlinability
            }
        }

        /// <summary>Ensures that the console has been initialized for use.</summary>
        private static void EnsureInitializedCore()
        {
            lock (Console.Out) // ensure that writing the ANSI string and setting initialized to true are done atomically
            {
                if (!s_initialized)
                {
                    // Ensure the console is configured appropriately.  This will start
                    // signal handlers, etc.
                    if (!Interop.Sys.InitializeConsole())
                    {
                        throw Interop.GetExceptionForIoErrno(Interop.Sys.GetLastErrorInfo());
                    }

                    // Provide the native lib with the correct code from the terminfo to transition us into
                    // "application mode".  This will both transition it immediately, as well as allow
                    // the native lib later to handle signals that require re-entering the mode.
                    if (!Console.IsOutputRedirected)
                    {
                        string keypadXmit = TerminalFormatStrings.Instance.KeypadXmit;
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
        internal class TerminalFormatStrings
        {
            /// <summary>Gets the lazily-initialized terminal information for the terminal.</summary>
            public static TerminalFormatStrings Instance { get { return s_instance.Value; } }
            private readonly static Lazy<TerminalFormatStrings> s_instance = new Lazy<TerminalFormatStrings>(() => new TerminalFormatStrings(TermInfo.Database.ReadActiveDatabase()));

            /// <summary>The format string to use to change the foreground color.</summary>
            public readonly string Foreground;
            /// <summary>The format string to use to change the background color.</summary>
            public readonly string Background;
            /// <summary>The format string to use to reset the foreground and background colors.</summary>
            public readonly string Reset;
            /// <summary>The maximum number of colors supported by the terminal.</summary>
            public readonly int MaxColors;
            /// <summary>The number of columns in a format.</summary>
            public readonly int Columns;
            /// <summary>The number of lines in a format.</summary>
            public readonly int Lines;
            /// <summary>The format string to use to make cursor visible.</summary>
            public readonly string CursorVisible;
            /// <summary>The format string to use to make cursor invisible</summary>
            public readonly string CursorInvisible;
            /// <summary>The format string to use to set the window title.</summary>
            public readonly string Title;
            /// <summary>The format string to use for an audible bell.</summary>
            public readonly string Bell;
            /// <summary>The format string to use to clear the terminal.</summary>
            public readonly string Clear;
            /// <summary>The format string to use to set the position of the cursor.</summary>
            public readonly string CursorAddress;
            /// <summary>The format string to use to move the cursor to the left.</summary>
            public readonly string CursorLeft;
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
            public readonly Dictionary<StringOrCharArray, ConsoleKeyInfo> KeyFormatToConsoleKey = new Dictionary<StringOrCharArray, ConsoleKeyInfo>();
            /// <summary> Max key length </summary>
            public readonly int MaxKeyFormatLength;
            /// <summary> Min key length </summary>
            public readonly int MinKeyFormatLength;
            /// <summary>The ANSI string used to enter "application" / "keypad transmit" mode.</summary>
            public readonly string KeypadXmit;

            public TerminalFormatStrings(TermInfo.Database db)
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

                    foreach (KeyValuePair<StringOrCharArray, ConsoleKeyInfo> entry in KeyFormatToConsoleKey)
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
                string tsl = db.GetString(TermInfo.WellKnownStrings.ToStatusLine);
                string fsl = db.GetString(TermInfo.WellKnownStrings.FromStatusLine);
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
        }

        /// <summary>Reads data from the file descriptor into the buffer.</summary>
        /// <param name="fd">The file descriptor.</param>
        /// <param name="buffer">The buffer to read into.</param>
        /// <param name="offset">The offset at which to start writing into the buffer.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <returns>The number of bytes read, or a negative value if there's an error.</returns>
        internal static unsafe int Read(SafeFileHandle fd, byte[] buffer, int offset, int count)
        {
            fixed (byte* bufPtr = buffer)
            {
                int result = Interop.CheckIo(Interop.Sys.Read(fd, (byte*)bufPtr + offset, count));
                Debug.Assert(result <= count);
                return result;
            }
        }

        /// <summary>Writes data from the buffer into the file descriptor.</summary>
        /// <param name="fd">The file descriptor.</param>
        /// <param name="buffer">The buffer from which to write data.</param>
        /// <param name="offset">The offset at which the data to write starts in the buffer.</param>
        /// <param name="count">The number of bytes to write.</param>
        private static unsafe void Write(SafeFileHandle fd, byte[] buffer, int offset, int count)
        {
            fixed (byte* bufPtr = buffer)
            {
                Write(fd, bufPtr + offset, count);
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
                Interop.Sys.FileStatus buf;
                _handleType =
                Interop.Sys.FStat(_handle, out buf) == 0 ?
                        (buf.Mode & Interop.Sys.FileTypes.S_IFMT) :
                        Interop.Sys.FileTypes.S_IFREG; // if something goes wrong, don't fail, just say it's a regular file
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

                return ConsolePal.Read(_handle, buffer, offset, count);
                }

            public override void Write(byte[] buffer, int offset, int count)
            {
                ValidateWrite(buffer, offset, count);

                ConsolePal.Write(_handle, buffer, offset, count);
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
                EnsureInitialized();

                Debug.Assert(!_handlerRegistered);
                Interop.Sys.RegisterForCtrl(_handler);
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
