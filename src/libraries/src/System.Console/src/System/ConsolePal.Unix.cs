// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

namespace System
{
    // Provides Unix-based support for System.Console.
    // - OpenStandardInput, OpenStandardOutput, OpenStandardError
    // - InputEncoding, OutputEncoding
    // - ForegroundColor, BackgroundColor, ResetColor
    //
    // This implementation relies only on POSIX-compliant/POSIX-standard APIs,
    // e.g. for reading/writing /dev/stdin, /dev/stdout, and /dev/stderr,
    // for getting environment variables for accessing charset information for encodings, 
    // and terminfo databases / strings for manipulating the terminal.
    // NOTE: The test class reflects over this class to run the tests due to limitations in
    //       the test infrastructure that prevent OS-specific builds of test binaries. If you
    //       change any of the class / struct / function names, parameters, etc then you need
    //       to also change the test class.
    internal static class ConsolePal
    {
        public static Stream OpenStandardInput()
        {
            return new UnixConsoleStream(Interop.Devices.stdin, FileAccess.Read);
        }

        public static Stream OpenStandardOutput()
        {
            return new UnixConsoleStream(Interop.Devices.stdout, FileAccess.Write);
        }

        public static Stream OpenStandardError()
        {
            return new UnixConsoleStream(Interop.Devices.stderr, FileAccess.Write);
        }

        public static Encoding InputEncoding
        {
            get { return GetConsoleEncoding(); }
        }

        public static Encoding OutputEncoding
        {
            get { return GetConsoleEncoding(); }
        }

        public static ConsoleColor ForegroundColor
        {
            get { throw new PlatformNotSupportedException(SR.PlatformNotSupported_GettingColor); } // no general mechanism for getting the current color
            set { ChangeColor(foreground: true, color:  value); }
        }

        public static ConsoleColor BackgroundColor
        {
            get { throw new PlatformNotSupportedException(SR.PlatformNotSupported_GettingColor); } // no general mechanism for getting the current color
            set { ChangeColor(foreground: false, color: value); }
        }

        public static void ResetColor()
        {
            if (!ConsoleOutIsTerminal)
                return;

            string resetFormat = TerminalColorInfo.Instance.ResetFormat;
            if (resetFormat != null)
            {
                Console.Write(resetFormat);
            }
        }

        /// <summary>Gets whether Console.Out is targeting a terminal display.</summary>
        private static bool ConsoleOutIsTerminal
        {
            get
            {
                // We only want to write out ANSI escape sequences if we're targeting a TTY device,
                // so we don't want to if the output stream is redirected, either redirected to another
                // stream via Console.SetOut, or redirected via Unix stdout redirection.
                // We make a best guess by unwrapping the TextWriter to get at the underlying
                // UnixConsoleStream, and checking the type of the underlying file descriptor
                // for that stream: we say it's a TTY if we can get the UnixConsoleStream and
                // if its type is CHR (a "character device").

                SyncTextWriter stw = Console.Out as SyncTextWriter;
                if (stw != null)
                {
                    StreamWriter sw = stw._out as StreamWriter;
                    if (sw != null)
                    {
                        UnixConsoleStream ucs = sw.BaseStream as UnixConsoleStream;
                        if (ucs != null)
                        {
                            return ucs._handleType == Interop.Sys.FileTypes.S_IFCHR;
                        }
                    }
                }
                return false;
            }
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
                catch (NotSupportedException) { }
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

        /// <summary>Outputs the format string evaluated and parameterized with the color.</summary>
        /// <param name="foreground">true for foreground; false for background.</param>
        /// <param name="color">The color to store into the field and to use as an argument to the format string.</param>
        private static void ChangeColor(bool foreground, ConsoleColor color)
        {
            // Get the numerical value of the color
            int ccValue = (int)color;
            if ((ccValue & ~0xF) != 0)
            {
                throw new ArgumentException(SR.Arg_InvalidConsoleColor);
            }

            // Changing the color involves writing an ANSI character sequence out to the output stream.
            // We only want to do this if we know that sequence will be interpreted by the output
            // rather than simply displayed visibly.  A reasonable approximation for this is whether
            // the underlying stream is our UnixConsoleStream and it's wrapping a character device.
            if (!ConsoleOutIsTerminal)
                return;

            // See if we've already cached a format string for this foreground/background
            // and specific color choice.  If we have, just output that format string again.
            int fgbgIndex = foreground ? 0 : 1;
            string evaluatedString = s_fgbgAndColorStrings[fgbgIndex, ccValue]; // benign race
            if (evaluatedString != null)
            {
                Console.Write(evaluatedString);
                return;
            }

            // We haven't yet computed a format string.  Compute it, use it, then cache it.
            string formatString = foreground ? TerminalColorInfo.Instance.ForegroundFormat : TerminalColorInfo.Instance.BackgroundFormat;
            if (formatString != null)
            {
                int maxColors = TerminalColorInfo.Instance.MaxColors; // often 8 or 16; 0 is invalid
                if (maxColors > 0)
                {
                    int ansiCode = _consoleColorToAnsiCode[ccValue] % maxColors;
                    evaluatedString = TermInfo.ParameterizedStrings.Evaluate(formatString, ansiCode);

                    Console.Write(evaluatedString);

                    s_fgbgAndColorStrings[fgbgIndex, ccValue] = evaluatedString; // benign race
                }
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
            public static TerminalColorInfo Instance { get { return _instance.Value; } }

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
            private static Lazy<TerminalColorInfo> _instance = new Lazy<TerminalColorInfo>(() =>
            {
                TermInfo.Database db = TermInfo.Database.Instance; // Could be null if TERM is set to a file that doesn't exist
                return new TerminalColorInfo(db);
            }, isThreadSafe: true);
        }

        /// <summary>Reads data from the file descriptor into the buffer.</summary>
        /// <param name="fd">The file descriptor.</param>
        /// <param name="buffer">The buffer to read into.</param>
        /// <param name="offset">The offset at which to start writing into the buffer.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <returns>The number of bytes read, or a negative value if there's an error.</returns>
        private static unsafe int Read(int fd, byte[] buffer, int offset, int count)
        {
            fixed (byte* bufPtr = buffer)
            {
                int result;
                while (Interop.CheckIo(result = Interop.Sys.Read(fd, (byte*)bufPtr + offset, count))) ;
                Debug.Assert(result <= count);
                return (int)result;
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
                while (count > 0)
                {
                    int bytesWritten;
                    while (Interop.CheckIo(bytesWritten = Interop.Sys.Write(fd, bufPtr + offset, count))) ;
                    count -= bytesWritten;
                    offset += bytesWritten;
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
            /// <param name="devPath">A path to a "/dev/std*" file.</param>
            /// <param name="access">FileAccess.Read or FileAccess.Write.</param>
            internal UnixConsoleStream(string devPath, FileAccess access)
                : base(access)
            {
                Debug.Assert(devPath != null && devPath.StartsWith("/dev/std"));
                Debug.Assert(access == FileAccess.Read || access == FileAccess.Write);
                
                // Open the file descriptor for this stream
                Interop.Sys.OpenFlags flags = 0;
                switch (access)
                {
                    case FileAccess.Read: flags = Interop.Sys.OpenFlags.O_RDONLY; break;
                    case FileAccess.Write: flags = Interop.Sys.OpenFlags.O_WRONLY; break;
                }
                _handle = SafeFileHandle.Open(devPath, flags, 0);

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
                    throw __Error.GetFileNotOpen();
                }
                base.Flush();
            }
        }

        /// <summary>Provides access to and processing of the terminfo database.</summary>
        internal static class TermInfo
        {
            /// <summary>Provides a terminfo database.</summary>
            internal sealed class Database
            {
                /// <summary>Lazily-initialized instance of the database.</summary>
                private static readonly Lazy<Database> _instance = new Lazy<Database>(() => ReadDatabase(), isThreadSafe: true);
                /// <summary>Raw data of the database instance.</summary>
                private readonly byte[] _data;
                /// <summary>The number of bytes in the names section of the database.</summary>
                private readonly int _nameSectionNumBytes;
                /// <summary>The number of bytes in the Booleans section of the database.</summary>
                private readonly int _boolSectionNumBytes;
                /// <summary>The number of shorts in the numbers section of the database.</summary>
                private readonly int _numberSectionNumShorts;
                /// <summary>The number of offsets in the strings section of the database.</summary>
                private readonly int _stringSectionNumOffsets;
                //private readonly int _stringTableNumBytes; // number of bytes in the strings table; this is in the header, but we don't actually use it

                /// <summary>Initializes the database instance.</summary>
                /// <param name="data">The data from the terminfo file.</param>
                private Database(byte[] data)
                {
                    _data = data;

                    // See "man term" for the file format.
                    if (ReadInt16(data, 0) != 0x11A) // magic number octal 0432
                    {
                        throw new InvalidOperationException(SR.IO_TermInfoInvalid);
                    }

                    _nameSectionNumBytes = ReadInt16(data, 2);
                    _boolSectionNumBytes = ReadInt16(data, 4);
                    _numberSectionNumShorts = ReadInt16(data, 6);
                    _stringSectionNumOffsets = ReadInt16(data, 8);
                    //_stringTableNumBytes = ReadInt16(data, 10); // in the header, but we don't currently need it
                }

                /// <summary>Gets the cached instance of the database.</summary>
                public static Database Instance { get { return _instance.Value; } }

                /// <summary>Read the database for the current terminal as specified by the "TERM" environment variable.</summary>
                /// <returns>The database, or null if it could not be found.</returns>
                private static Database ReadDatabase()
                {
                    string term = Environment.GetEnvironmentVariable("TERM");
                    return !string.IsNullOrEmpty(term) ? ReadDatabase(term) : null;
                }

                /// <summary>
                /// The default locations in which to search for terminfo databases.
                /// This is the ordering of well-known locations used by ncurses.
                /// </summary>
                private static readonly string[] _terminfoLocations = new string[] {
                    "/etc/terminfo",
                    "/lib/terminfo",
                    "/usr/share/terminfo",
                };

                /// <summary>Read the database for the specified terminal.</summary>
                /// <param name="term">The identifier for the terminal.</param>
                /// <returns>The database, or null if it could not be found.</returns>
                private static Database ReadDatabase(string term)
                {
                    // This follows the same search order as prescribed by ncurses.
                    Database db;

                    // First try a location specified in the TERMINFO environment variable.
                    string terminfo = Environment.GetEnvironmentVariable("TERMINFO");
                    if (!string.IsNullOrWhiteSpace(terminfo) && (db = ReadDatabase(term, terminfo)) != null)
                    {
                        return db;
                    }

                    // Then try in the user's home directory.
                    string home = PersistedFiles.GetHomeDirectory();
                    if (!string.IsNullOrWhiteSpace(home) && (db = ReadDatabase(term, home + "/.terminfo")) != null)
                    {
                        return db;
                    }

                    // Then try a set of well-known locations.
                    foreach (string terminfoLocation in _terminfoLocations)
                    {
                        if ((db = ReadDatabase(term, terminfoLocation)) != null)
                        {
                            return db;
                        }
                    }

                    // Couldn't find one
                    return null;
                }

                /// <summary>Attempt to open as readonly the specified file path.</summary>
                /// <param name="filePath">The path to the file to open.</param>
                /// <param name="fd">If successful, the opened file descriptor; otherwise, -1.</param>
                /// <returns>true if the file was successfully opened; otherwise, false.</returns>
                private static bool TryOpen(string filePath, out int fd)
                {
                    int tmpFd;
                    while ((tmpFd = Interop.Sys.Open(filePath, Interop.Sys.OpenFlags.O_RDONLY, 0)) < 0)
                    {
                        // Don't throw in this case, as we'll be polling multiple locations looking for the file.
                        // But we still want to retry if the open is interrupted by a signal.
                        if (Interop.Sys.GetLastError() != Interop.Error.EINTR)
                        {
                            fd = -1;
                            return false;
                        }
                    }
                    fd = tmpFd;
                    return true;
                }

                /// <summary>Read the database for the specified terminal from the specified directory.</summary>
                /// <param name="term">The identifier for the terminal.</param>
                /// <param name="directoryPath">The path to the directory containing terminfo database files.</param>
                /// <returns>The database, or null if it could not be found.</returns>
                private static Database ReadDatabase(string term, string directoryPath)
                {
                    if (string.IsNullOrEmpty(term) || string.IsNullOrEmpty(directoryPath))
                    {
                        return null;
                    }

                    int fd;
                    if (!TryOpen(directoryPath + "/" + term[0].ToString() + "/" + term, out fd) &&          // /directory/termFirstLetter/term      (Linux)
                        !TryOpen(directoryPath + "/" + ((int)term[0]).ToString("X") + "/" + term, out fd))  // /directory/termFirstLetterAsHex/term (Mac)
                    {
                        return null;
                    }

                    try
                    {
                        // Read in all of the terminfo data
                        long termInfoLength;
                        while (Interop.CheckIo(termInfoLength = Interop.Sys.LSeek(fd, 0, Interop.Sys.SeekWhence.SEEK_END))) ; // jump to the end to get the file length
                        while (Interop.CheckIo(Interop.Sys.LSeek(fd, 0, Interop.Sys.SeekWhence.SEEK_SET))) ; // reset back to beginning
                        const int MaxTermInfoLength = 4096; // according to the term and tic man pages, 4096 is the terminfo file size max
                        const int HeaderLength = 12;
                        if (termInfoLength <= HeaderLength || termInfoLength > MaxTermInfoLength)
                        {
                            throw new InvalidOperationException(SR.IO_TermInfoInvalid);
                        }
                        int fileLen = (int)termInfoLength;

                        byte[] data = new byte[fileLen];
                        if (Read(fd, data, 0, fileLen) != fileLen)
                        {
                            throw new InvalidOperationException(SR.IO_TermInfoInvalid);
                        }

                        // Create the database from the data
                        return new Database(data);
                    }
                    finally
                    {
                        Interop.CheckIo(Interop.Sys.Close(fd)); // Avoid retrying close on EINTR, e.g. https://lkml.org/lkml/2005/9/11/49
                    }
                }

                /// <summary>The offset into data where the names section begins.</summary>
                private const int NamesOffset = 12; // comes right after the header, which is always 12 bytes

                /// <summary>The offset into data where the Booleans section begins.</summary>
                private int BooleansOffset { get { return NamesOffset + _nameSectionNumBytes; } } // after the names section

                /// <summary>The offset into data where the numbers section begins.</summary>
                private int NumbersOffset // after the Booleans section, at an even position
                {
                    get
                    {
                        int offset = BooleansOffset + _boolSectionNumBytes;
                        return offset % 2 == 1 ? offset + 1 : offset;
                    }
                }

                /// <summary>
                /// The offset into data where the string offsets section begins.  We index into this section
                /// to find the location within the strings table where a string value exists.
                /// </summary>
                private int StringOffsetsOffset { get { return NumbersOffset + (_numberSectionNumShorts * 2); } }

                /// <summary>The offset into data where the string table exists.</summary>
                private int StringsTableOffset { get { return StringOffsetsOffset + (_stringSectionNumOffsets * 2); } }

                /// <summary>Gets a string from the strings section by the string's well-known index.</summary>
                /// <param name="stringTableIndex">The index of the string to find.</param>
                /// <returns>The string if it's in the database; otherwise, null.</returns>
                public string GetString(int stringTableIndex)
                {
                    Debug.Assert(stringTableIndex >= 0);

                    if (stringTableIndex >= _stringSectionNumOffsets)
                    {
                        // Some terminfo files may not contain enough entries to actually 
                        // have the requested one.
                        return null;
                    }

                    int tableIndex = ReadInt16(_data, StringOffsetsOffset + (stringTableIndex * 2));
                    if (tableIndex == -1)
                    {
                        // Some terminfo files may have enough entries, but may not actually
                        // have it filled in for this particular string.
                        return null;
                    }

                    return ReadString(_data, StringsTableOffset + tableIndex);
                }

                /// <summary>Gets a number from the numbers section by the number's well-known index.</summary>
                /// <param name="numberIndex">The index of the string to find.</param>
                /// <returns>The number if it's in the database; otherwise, -1.</returns>
                public int GetNumber(int numberIndex)
                {
                    Debug.Assert(numberIndex >= 0);

                    if (numberIndex >= _numberSectionNumShorts)
                    {
                        // Some terminfo files may not contain enough entries to actually
                        // have the requested one.
                        return -1;
                    }

                    return ReadInt16(_data, NumbersOffset + (numberIndex * 2));
                }

                /// <summary>The well-known index of the max_colors numbers entry.</summary>
                public const int MaxColorsIndex = 13;
                /// <summary>The well-known index of the orig_pairs string entry.</summary>
                public const int OrigPairsIndex = 297;
                /// <summary>The well-known index of the orig_colors string entry.</summary>
                public const int OrigColorsIndex = 298;
                /// <summary>The well-known index of the set_a_foreground string entry.</summary>
                public const int SetAnsiForegroundIndex = 359;
                /// <summary>The well-known index of the set_a_background string entry.</summary>
                public const int SetAnsiBackgroundIndex = 360;

                /// <summary>Read a 16-bit value from the buffer starting at the specified position.</summary>
                /// <param name="buffer">The buffer from which to read.</param>
                /// <param name="pos">The position at which to read.</param>
                /// <returns>The 16-bit value read.</returns>
                private static short ReadInt16(byte[] buffer, int pos)
                {
                    return (short)
                        ((((int)buffer[pos + 1]) << 8) |
                         ((int)buffer[pos] & 0xff));
                }

                /// <summary>Reads a string from the buffer starting at the specified position.</summary>
                /// <param name="buffer">The buffer from which to read.</param>
                /// <param name="pos">The position at which to read.</param>
                /// <returns>The string read from the specified position.</returns>
                private static string ReadString(byte[] buffer, int pos)
                {
                    // Strings are null-terminated in the data.  First find how long it is.
                    int findNullEnding = pos;
                    while (findNullEnding < buffer.Length && buffer[findNullEnding] != '\0')
                    {
                        findNullEnding++;
                    }
                    return Encoding.ASCII.GetString(buffer, pos, findNullEnding - pos);
                }
            }

            /// <summary>Provides support for evaluating parameterized terminfo database format strings.</summary>
            internal static class ParameterizedStrings
            {
                /// <summary>A cached stack to use to avoid allocating a new stack object for every evaluation.</summary>
                [ThreadStatic]
                private static LowLevelStack<FormatParam> _cachedStack;

                /// <summary>Evaluates a terminfo formatting string, using the supplied arguments.</summary>
                /// <param name="format">The format string.</param>
                /// <param name="args">The arguments to the format string.</param>
                /// <returns>The formatted string.</returns>
                public static string Evaluate(string format, params FormatParam[] args)
                {
                    if (format == null)
                    {
                        throw new ArgumentNullException("format");
                    }
                    if (args == null)
                    {
                        throw new ArgumentNullException("args");
                    }

                    // Initialize the stack to use for processing.
                    LowLevelStack<FormatParam> stack = _cachedStack;
                    if (stack == null)
                    {
                        _cachedStack = stack = new LowLevelStack<FormatParam>();
                    }
                    else
                    {
                        stack.Clear();
                    }

                    // "dynamic" and "static" variables are much less often used (the "dynamic" and "static"
                    // terminology appears to just refer to two different collections rather than to any semantic
                    // meaning).  As such, we'll only initialize them if we really need them.
                    FormatParam[] dynamicVars = null, staticVars = null;

                    int pos = 0;
                    return EvaluateInternal(format, ref pos, args, stack, ref dynamicVars, ref staticVars);

                    // EvaluateInternal may throw IndexOutOfRangeException and InvalidOperationException
                    // if the format string is malformed or if it's inconsistent with the parameters provided.
                }

                /// <summary>Evaluates a terminfo formatting string, using the supplied arguments and processing data structures.</summary>
                /// <param name="format">The format string.</param>
                /// <param name="pos">The position in <paramref name="format"/> to start processing.</param>
                /// <param name="args">The arguments to the format string.</param>
                /// <param name="stack">The stack to use as the format string is evaluated.</param>
                /// <param name="dynamicVars">A lazily-initialized collection of variables.</param>
                /// <param name="staticVars">A lazily-initialized collection of variables.</param>
                /// <returns>
                /// The formatted string; this may be empty if the evaluation didn't yield any output.
                /// The evaluation stack will have a 1 at the top if all processing was completed at invoked level
                /// of recursion, and a 0 at the top if we're still inside of a conditional that requires more processing.
                /// </returns>
                private static string EvaluateInternal(
                    string format, ref int pos, FormatParam[] args, LowLevelStack<FormatParam> stack,
                    ref FormatParam[] dynamicVars, ref FormatParam[] staticVars)
                {
                    // Create a StringBuilder to store the output of this processing.  We use the format's length as an 
                    // approximation of an upper-bound for how large the output will be, though with parameter processing,
                    // this is just an estimate, sometimes way over, sometimes under.
                    StringBuilder output = StringBuilderCache.Acquire(format.Length);

                    // Format strings support conditionals, including the equivalent of "if ... then ..." and
                    // "if ... then ... else ...", as well as "if ... then ... else ... then ..."
                    // and so on, where an else clause can not only be evaluated for string output but also
                    // as a conditional used to determine whether to evaluate a subsequent then clause.
                    // We use recursion to process these subsequent parts, and we track whether we're processing
                    // at the same level of the initial if clause (or whether we're nested).
                    bool sawIfConditional = false;

                    // Process each character in the format string, starting from the position passed in.
                    for (; pos < format.Length; pos++)
                    {
                        // '%' is the escape character for a special sequence to be evaluated.
                        // Anything else just gets pushed to output.
                        if (format[pos] != '%')
                        {
                            output.Append(format[pos]);
                            continue;
                        }

                        // We have a special parameter sequence to process.  Now we need
                        // to look at what comes after the '%'.
                        ++pos;
                        switch (format[pos])
                        {
                            // Output appending operations
                            case '%': // Output the escaped '%'
                                output.Append('%');
                                break;
                            case 'c': // Pop the stack and output it as a char
                                output.Append((char)stack.Pop().Int32);
                                break;
                            case 's': // Pop the stack and output it as a string
                                output.Append(stack.Pop().String);
                                break;
                            case 'd': // Pop the stack and output it as an integer
                                output.Append(stack.Pop().Int32);
                                break;
                            case 'o':
                            case 'X':
                            case 'x':
                            case ':':
                            case '0':
                            case '1':
                            case '2':
                            case '3':
                            case '4':
                            case '5':
                            case '6':
                            case '7':
                            case '8':
                            case '9':
                                // printf strings of the format "%[[:]flags][width[.precision]][doxXs]" are allowed
                                // (with a ':' used in front of flags to help differentiate from binary operations, as flags can
                                // include '-' and '+').  While above we've special-cased common usage (e.g. %d, %s),
                                // for more complicated expressions we delegate to printf.
                                int printfEnd = pos;
                                for (; printfEnd < format.Length; printfEnd++) // find the end of the printf format string
                                {
                                    char ec = format[printfEnd];
                                    if (ec == 'd' || ec == 'o' || ec == 'x' || ec == 'X' || ec == 's')
                                    {
                                        break;
                                    }
                                }
                                if (printfEnd >= format.Length)
                                {
                                    throw new InvalidOperationException(SR.IO_TermInfoInvalid);
                                }
                                string printfFormat = format.Substring(pos - 1, printfEnd - pos + 2); // extract the format string
                                if (printfFormat.Length > 1 && printfFormat[1] == ':')
                                {
                                    printfFormat = printfFormat.Remove(1, 1);
                                }
                                output.Append(FormatPrintF(printfFormat, stack.Pop().Object)); // do the printf formatting and append its output
                                break;

                            // Stack pushing operations
                            case 'p': // Push the specified parameter (1-based) onto the stack
                                pos++;
                                Debug.Assert(format[pos] >= '0' && format[pos] <= '9');
                                stack.Push(args[format[pos] - '1']);
                                break;
                            case 'l': // Pop a string and push its length
                                stack.Push(stack.Pop().String.Length);
                                break;
                            case '{': // Push integer literal, enclosed between braces
                                pos++;
                                int intLit = 0;
                                while (format[pos] != '}')
                                {
                                    Debug.Assert(format[pos] >= '0' && format[pos] <= '9');
                                    intLit = (intLit * 10) + (format[pos] - '0');
                                    pos++;
                                }
                                stack.Push(intLit);
                                break;
                            case '\'': // Push literal character, enclosed between single quotes
                                stack.Push((int)format[pos + 1]);
                                Debug.Assert(format[pos + 2] == '\'');
                                pos += 2;
                                break;

                            // Storing and retrieving "static" and "dynamic" variables
                            case 'P': // Pop a value and store it into either static or dynamic variables based on whether the a-z variable is capitalized
                                pos++;
                                int setIndex;
                                FormatParam[] targetVars = GetDynamicOrStaticVariables(format[pos], ref dynamicVars, ref staticVars, out setIndex);
                                targetVars[setIndex] = stack.Pop();
                                break;
                            case 'g': // Push a static or dynamic variable; which is based on whether the a-z variable is capitalized
                                pos++;
                                int getIndex;
                                FormatParam[] sourceVars = GetDynamicOrStaticVariables(format[pos], ref dynamicVars, ref staticVars, out getIndex);
                                stack.Push(sourceVars[getIndex]);
                                break;

                            // Binary operations
                            case '+':
                            case '-':
                            case '*':
                            case '/':
                            case 'm':
                            case '^': // arithmetic
                            case '&':
                            case '|':                                         // bitwise
                            case '=':
                            case '>':
                            case '<':                               // comparison
                            case 'A':
                            case 'O':                                         // logical
                                int second = stack.Pop().Int32; // it's a stack... the second value was pushed last
                                int first = stack.Pop().Int32;
                                char c = format[pos];
                                stack.Push(
                                    c == '+' ? (first + second) :
                                    c == '-' ? (first - second) :
                                    c == '*' ? (first * second) :
                                    c == '/' ? (first / second) :
                                    c == 'm' ? (first % second) :
                                    c == '^' ? (first ^ second) :
                                    c == '&' ? (first & second) :
                                    c == '|' ? (first | second) :
                                    c == '=' ? AsInt(first == second) :
                                    c == '>' ? AsInt(first > second) :
                                    c == '<' ? AsInt(first < second) :
                                    c == 'A' ? AsInt(AsBool(first) && AsBool(second)) :
                                    c == 'O' ? AsInt(AsBool(first) || AsBool(second)) :
                                    0); // not possible; we just validated above
                                break;

                            // Unary operations
                            case '!':
                            case '~':
                                int value = stack.Pop().Int32;
                                stack.Push(
                                    format[pos] == '!' ? AsInt(!AsBool(value)) :
                                    ~value);
                                break;

                                // Some terminfo files appear to have a fairly liberal interpretation of %i. The spec states that %i increments the first two arguments, 
                                // but some uses occur when there's only a single argument. To make sure we accomodate these files, we increment the values 
                                // of up to (but not requiring) two arguments.
                            case 'i':
                                if (args.Length > 0)
                                {
                                    args[0] = 1 + args[0].Int32;
                                    if (args.Length > 1)
                                        args[1] = 1 + args[1].Int32;
                                }
                                break;

                            // Conditional of the form %? if-part %t then-part %e else-part %;
                            // The "%e else-part" is optional.
                            case '?':
                                sawIfConditional = true;
                                break;
                            case 't':
                                // We hit the end of the if-part and are about to start the then-part.
                                // The if-part left its result on the stack; pop and evaluate.
                                bool conditionalResult = AsBool(stack.Pop().Int32);

                                // Regardless of whether it's true, run the then-part to get past it.
                                // If the conditional was true, output the then results.
                                pos++;
                                string thenResult = EvaluateInternal(format, ref pos, args, stack, ref dynamicVars, ref staticVars);
                                if (conditionalResult)
                                {
                                    output.Append(thenResult);
                                }
                                Debug.Assert(format[pos] == 'e' || format[pos] == ';');

                                // We're past the then; the top of the stack should now be a Boolean
                                // indicating whether this conditional has more to be processed (an else clause).
                                if (!AsBool(stack.Pop().Int32))
                                {
                                    // Process the else clause, and if the conditional was false, output the else results.
                                    pos++;
                                    string elseResult = EvaluateInternal(format, ref pos, args, stack, ref dynamicVars, ref staticVars);
                                    if (!conditionalResult)
                                    {
                                        output.Append(elseResult);
                                    }

                                    // Now we should be done (any subsequent elseif logic will have been handled in the recursive call).
                                    if (!AsBool(stack.Pop().Int32))
                                    {
                                        throw new InvalidOperationException(SR.IO_TermInfoInvalid);
                                    }
                                }

                                // If we're in a nested processing, return to our parent.
                                if (!sawIfConditional)
                                {
                                    stack.Push(1);
                                    return StringBuilderCache.GetStringAndRelease(output);
                                }

                                // Otherwise, we're done processing the conditional in its entirety.
                                sawIfConditional = false;
                                break;
                            case 'e':
                            case ';':
                                // Let our caller know why we're exiting, whether due to the end of the conditional or an else branch.
                                stack.Push(AsInt(format[pos] == ';'));
                                return StringBuilderCache.GetStringAndRelease(output);

                            // Anything else is an error
                            default:
                                throw new InvalidOperationException(SR.IO_TermInfoInvalid);
                        }
                    }

                    stack.Push(1);
                    return StringBuilderCache.GetStringAndRelease(output);
                }

                /// <summary>Converts an Int32 to a Boolean, with 0 meaning false and all non-zero values meaning true.</summary>
                /// <param name="i">The integer value to convert.</param>
                /// <returns>true if the integer was non-zero; otherwise, false.</returns>
                private static bool AsBool(Int32 i) { return i != 0; }

                /// <summary>Converts a Boolean to an Int32, with true meaning 1 and false meaning 0.</summary>
                /// <param name="b">The Boolean value to convert.</param>
                /// <returns>1 if the Boolean is true; otherwise, 0.</returns>
                private static int AsInt(bool b) { return b ? 1 : 0; }

                /// <summary>Formats an argument into a printf-style format string.</summary>
                /// <param name="format">The printf-style format string.</param>
                /// <param name="arg">The argument to format.  This must be an Int32 or a String.</param>
                /// <returns>The formatted string.</returns>
                private static unsafe string FormatPrintF(string format, object arg)
                {
                    Debug.Assert(arg is string || arg is Int32);

                    // Determine how much space is needed to store the formatted string.
                    string stringArg = arg as string;
                    int neededLength = stringArg != null ?
                        Interop.Sys.SNPrintF(null, 0, format, stringArg) :
                        Interop.Sys.SNPrintF(null, 0, format, (int)arg);
                    if (neededLength == 0)
                    {
                        return string.Empty;
                    }
                    if (neededLength < 0)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_PrintF);
                    }

                    // Allocate the needed space, format into it, and return the data as a string.
                    byte[] bytes = new byte[neededLength + 1]; // extra byte for the null terminator
                    fixed (byte* ptr = bytes)
                    {
                        int length = stringArg != null ?
                            Interop.Sys.SNPrintF(ptr, bytes.Length, format, stringArg) :
                            Interop.Sys.SNPrintF(ptr, bytes.Length, format, (int)arg);
                        if (length != neededLength)
                        {
                            throw new InvalidOperationException(SR.InvalidOperation_PrintF);
                        }
                    }
                    return Encoding.ASCII.GetString(bytes, 0, neededLength);
                }

                /// <summary>Gets the lazily-initialized dynamic or static variables collection, based on the supplied variable name.</summary>
                /// <param name="c">The name of the variable.</param>
                /// <param name="dynamicVars">The lazily-initialized dynamic variables collection.</param>
                /// <param name="staticVars">The lazily-initialized static variables collection.</param>
                /// <param name="index">The index to use to index into the variables.</param>
                /// <returns>The variables collection.</returns>
                private static FormatParam[] GetDynamicOrStaticVariables(
                    char c, ref FormatParam[] dynamicVars, ref FormatParam[] staticVars, out int index)
                {
                    if (c >= 'A' && c <= 'Z')
                    {
                        index = c - 'A';
                        return staticVars ?? (staticVars = new FormatParam[26]); // one slot for each letter of alphabet
                    }
                    else if (c >= 'a' && c <= 'z')
                    {
                        index = c - 'a';
                        return dynamicVars ?? (dynamicVars = new FormatParam[26]); // one slot for each letter of alphabet
                    }
                    else throw new InvalidOperationException(SR.IO_TermInfoInvalid);
                }

                /// <summary>
                /// Represents a parameter to a terminfo formatting string.
                /// It is a discriminated union of either an integer or a string, 
                /// with characters represented as integers.
                /// </summary>
                public struct FormatParam
                {
                    /// <summary>The integer stored in the parameter.</summary>
                    private readonly int _int32;
                    /// <summary>The string stored in the parameter.</summary>
                    private readonly string _string; // null means an Int32 is stored

                    /// <summary>Initializes the parameter with an integer value.</summary>
                    /// <param name="value">The value to be stored in the parameter.</param>
                    public FormatParam(Int32 value) : this(value, null) { }

                    /// <summary>Initializes the parameter with a string value.</summary>
                    /// <param name="value">The value to be stored in the parameter.</param>
                    public FormatParam(String value) : this(0, value ?? string.Empty) { }

                    /// <summary>Initializes the parameter.</summary>
                    /// <param name="intValue">The integer value.</param>
                    /// <param name="stringValue">The string value.</param>
                    private FormatParam(Int32 intValue, String stringValue)
                    {
                        _int32 = intValue;
                        _string = stringValue;
                    }

                    /// <summary>Implicit converts an integer into a parameter.</summary>
                    public static implicit operator FormatParam(int value)
                    {
                        return new FormatParam(value);
                    }

                    /// <summary>Implicit converts a string into a parameter.</summary>
                    public static implicit operator FormatParam(string value)
                    {
                        return new FormatParam(value);
                    }

                    /// <summary>Gets the integer value of the parameter. If a string was stored, 0 is returned.</summary>
                    public int Int32 { get { return _int32; } }

                    /// <summary>Gets the string value of the parameter.  If an Int32 or a null String were stored, an empty string is returned.</summary>
                    public string String { get { return _string ?? string.Empty; } }

                    /// <summary>Gets the string or the integer value as an object.</summary>
                    public object Object { get { return _string ?? (object)_int32; } }
                }

                /// <summary>Provides a basic stack data structure.</summary>
                /// <typeparam name="T">Specifies the type of data in the stack.</typeparam>
                private sealed class LowLevelStack<T> // System.Console.dll doesn't reference System.Collections.dll
                {
                    private const int DefaultSize = 4;
                    private T[] _arr;
                    private int _count;

                    public LowLevelStack() { _arr = new T[DefaultSize]; }

                    public T Pop()
                    {
                        if (_count == 0)
                        {
                            throw new InvalidOperationException(SR.InvalidOperation_EmptyStack);
                        }
                        T item = _arr[--_count];
                        _arr[_count] = default(T);
                        return item;
                    }

                    public void Push(T item)
                    {
                        if (_arr.Length == _count)
                        {
                            T[] newArr = new T[_arr.Length * 2];
                            Array.Copy(_arr, 0, newArr, 0, _arr.Length);
                            _arr = newArr;
                        }
                        _arr[_count++] = item;
                    }

                    public void Clear()
                    {
                        Array.Clear(_arr, 0, _count);
                        _count = 0;
                    }
                }
            }
        }

        internal sealed class ControlCHandlerRegistrar
        {
            private readonly Interop.libcoreclr.ConsoleCtrlHandlerRoutine _handler;
            private bool _handlerRegistered;

            internal ControlCHandlerRegistrar()
            {
                _handler = new Interop.libcoreclr.ConsoleCtrlHandlerRoutine(c =>
                    (c == Interop.libcoreclr.CTRL_C_EVENT || c == Interop.libcoreclr.CTRL_BREAK_EVENT) &&
                    Console.HandleBreakEvent(c == Interop.libcoreclr.CTRL_C_EVENT ? ConsoleSpecialKey.ControlC : ConsoleSpecialKey.ControlBreak));
            }

            internal void Register() { RegisterOrUnregister(true); }

            internal void Unregister() { RegisterOrUnregister(false); }

            private void RegisterOrUnregister(bool register)
            {
                Debug.Assert(register == !_handlerRegistered);
                if (!Interop.libcoreclr.SetConsoleCtrlHandler(_handler, register))
                {
                    int error = Marshal.GetLastWin32Error(); // Win32 error code from coreclr PAL, not a Unix errno value
                    throw Interop.GetExceptionForIoErrno(
                        error == Interop.libcoreclr.ERROR_INVALID_PARAMETER ? Interop.Error.EINVAL.Info() :
                        error == Interop.libcoreclr.ERROR_NOT_ENOUGH_MEMORY ? Interop.Error.ENOMEM.Info() :
                        Interop.Error.EIO.Info());
                }
                _handlerRegistered = register;
            }
        }

    }
}
