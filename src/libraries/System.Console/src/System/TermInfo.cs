// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace System
{
    /// <summary>Provides access to and processing of a terminfo database.</summary>
    internal static class TermInfo
    {
        /// <summary>Provides a terminfo database.</summary>
        internal sealed class Database
        {
            /// <summary>Lazily-initialized instance of the database.</summary>
            private static readonly Lazy<Database> _instance = new Lazy<Database>(() => ReadDatabase(), isThreadSafe: true);

            /// <summary>The name of the terminfo file.</summary>
            private readonly string _term;
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
            /// <summary>The number of bytes in the strings table of the database.</summary>
            private readonly int _stringTableNumBytes;

            /// <summary>Extended / user-defined entries in the terminfo database.</summary>
            private readonly Dictionary<string, string> _extendedStrings;

            /// <summary>Initializes the database instance.</summary>
            /// <param name="term">The name of the terminal.</param>
            /// <param name="data">The data from the terminfo file.</param>
            private Database(string term, byte[] data)
            {
                _term = term;
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
                _stringTableNumBytes = ReadInt16(data, 10);
                if (_nameSectionNumBytes < 0 ||
                    _boolSectionNumBytes < 0 ||
                    _numberSectionNumShorts < 0 ||
                    _stringSectionNumOffsets < 0 ||
                    _stringTableNumBytes < 0)
                {
                    throw new InvalidOperationException(SR.IO_TermInfoInvalid);
                }

                // In addition to the main section of bools, numbers, and strings, there is also
                // an "extended" section.  This section contains additional entries that don't
                // have well-known indices, and are instead named mappings.  As such, we parse
                // all of this data now rather than on each request, as the mapping is fairly complicated.
                // This function relies on the data stored above, so it's the last thing we run.
                // (Note that the extended section also includes other Booleans and numbers, but we don't
                // have any need for those now, so we don't parse them.)
                int extendedBeginning = RoundUpToEven(StringsTableOffset + _stringTableNumBytes);
                _extendedStrings = ParseExtendedStrings(data, extendedBeginning) ?? new Dictionary<string, string>();
            }

            /// <summary>Gets the cached instance of the database.</summary>
            public static Database Instance { get { return _instance.Value; } }

            /// <summary>The name of the associated terminfo, if any.</summary>
            public string Term { get { return _term; } }

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
                    if (ConsolePal.Read(fd, data, 0, fileLen) != fileLen)
                    {
                        throw new InvalidOperationException(SR.IO_TermInfoInvalid);
                    }

                    // Create the database from the data
                    return new Database(term, data);
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
            private int NumbersOffset { get { return RoundUpToEven(BooleansOffset + _boolSectionNumBytes); } } // after the Booleans section, at an even position

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

            /// <summary>Gets a string from the extended strings section.</summary>
            /// <param name="name">The name of the string as contained in the extended names section.</param>
            /// <returns>The string if it's in the database; otherwise, null.</returns>
            public string GetExtendedString(string name)
            {
                Debug.Assert(name != null);

                string value;
                return _extendedStrings.TryGetValue(name, out value) ?
                    value :
                    null;
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

            /// <summary>Parses the extended string information from the terminfo data.</summary>
            /// <returns>
            /// A dictionary of the name to value mapping.  As this section of the terminfo isn't as well
            /// defined as the earlier portions, and may not even exist, the parsing is more lenient about
            /// errors, returning an empty collection rather than throwing.
            /// </returns>
            private static Dictionary<string, string> ParseExtendedStrings(byte[] data, int extendedBeginning)
            {
                const int ExtendedHeaderSize = 10;
                if (extendedBeginning + ExtendedHeaderSize >= data.Length)
                {
                    // Exit out as there's no extended information.
                    return null;
                }

                // Read in extended counts, and exit out if we got any incorrect info
                int extendedBoolCount = ReadInt16(data, extendedBeginning);
                int extendedNumberCount = ReadInt16(data, extendedBeginning + 2);
                int extendedStringCount = ReadInt16(data, extendedBeginning + 4);
                int extendedStringNumOffsets = ReadInt16(data, extendedBeginning + 6);
                int extendedStringTableByteSize = ReadInt16(data, extendedBeginning + 8);
                if (extendedBoolCount < 0 ||
                    extendedNumberCount < 0 ||
                    extendedStringCount < 0 ||
                    extendedStringNumOffsets < 0 ||
                    extendedStringTableByteSize < 0)
                {
                    // The extended header contained invalid data.  Bail.
                    return null;
                }

                // Skip over the extended bools.  We don't need them now and can add this in later 
                // if needed. Also skip over extended numbers, for the same reason.

                // Get the location where the extended string offsets begin.  These point into
                // the extended string table.
                int extendedOffsetsStart =
                    extendedBeginning + // go past the normal data
                    ExtendedHeaderSize + // and past the extended header
                    RoundUpToEven(extendedBoolCount) + // and past all of the extended Booleans
                    (extendedNumberCount * 2); // and past all of the extended numbers

                // Get the location where the extended string table begins.  This area contains
                // null-terminated strings.
                int extendedStringTableStart =
                    extendedOffsetsStart +
                    (extendedStringCount * 2) + // and past all of the string offsets
                    ((extendedBoolCount + extendedNumberCount + extendedStringCount) * 2); // and past all of the name offsets

                // Get the location where the extended string table ends.  We shouldn't read past this.
                int extendedStringTableEnd =
                    extendedStringTableStart +
                    extendedStringTableByteSize;

                if (extendedStringTableEnd > data.Length)
                {
                    // We don't have enough data to parse everything.  Bail.
                    return null;
                }

                // Now we need to parse all of the extended string values.  These aren't necessarily
                // "in order", meaning the offsets aren't guaranteed to be increasing.  Instead, we parse
                // the offsets in order, pulling out each string it references and storing them into our
                // results list in the order of the offsets.
                var values = new List<string>(extendedStringCount);
                int lastEnd = 0;
                for (int i = 0; i < extendedStringCount; i++)
                {
                    int offset = extendedStringTableStart + ReadInt16(data, extendedOffsetsStart + (i * 2));
                    if (offset < 0 || offset >= data.Length)
                    {
                        // If the offset is invalid, bail.
                        return null;
                    }

                    // Add the string
                    int end = FindNullTerminator(data, offset);
                    values.Add(Encoding.ASCII.GetString(data, offset, end - offset));

                    // Keep track of where the last string ends.  The name strings will come after that.
                    lastEnd = Math.Max(end, lastEnd);
                }

                // Now parse all of the names.
                var names = new List<string>(extendedBoolCount + extendedNumberCount + extendedStringCount);
                for (int pos = lastEnd + 1; pos < extendedStringTableEnd; pos++)
                {
                    int end = FindNullTerminator(data, pos);
                    names.Add(Encoding.ASCII.GetString(data, pos, end - pos));
                    pos = end;
                }

                // The names are in order for the Booleans, then the numbers, and then the strings.
                // Skip over the bools and numbers, and associate the names with the values.
                var extendedStrings = new Dictionary<string, string>(extendedStringCount);
                for (int iName = extendedBoolCount + extendedNumberCount, iValue = 0;
                     iName < names.Count && iValue < values.Count;
                     iName++, iValue++)
                {
                    extendedStrings.Add(names[iName], values[iValue]);
                }

                return extendedStrings;
            }

            private static int RoundUpToEven(int i) { return i % 2 == 1 ? i + 1 : i; }

            /// <summary>The well-known index of the audible bell entry.</summary>
            public const int BellIndex = 1;
            /// <summary>The well-known index of the clear screen entry.</summary>
            public const int ClearIndex = 5;
            /// <summary>The well-known index of the cursor address entry.</summary>
            public const int CursorAddressIndex = 10;
            /// <summary>The well-known index of the cursor left entry.</summary>
            public const int CursorLeftIndex = 14;
            /// <summary>The well-known index of "user string 7", which is used for cursor position requests.</summary>
            public const int CursorPositionRequest = 294;

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

            /// <summary>The well-known index of the columns numeric entry.</summary>
            public const int ColumnIndex = 0;
            /// <summary>The well-known index of the lines numeric entry.</summary>
            public const int LinesIndex = 2;
            /// <summary>The well-known index of the cursor_invisible string entry.</summary>
            public const int CursorInvisibleIndex = 13;
            /// <summary>The well-known index of the cursor_normal string entry.</summary>
            public const int CursorVisibleIndex = 16;
            /// <summary>The well-known index of the from_status_line string entry.</summary>
            public const int FromStatusLineIndex = 47;
            /// <summary>The well-known index of the from_status_line string entry.</summary>
            public const int ToStatusLineIndex = 135;

            /// <summary>The well-known index of key_backspace</summary>
            public const int KeyBackspace = 55;
            /// <summary>The well-known index of key_clear</summary>
            public const int KeyClear = 57;
            /// <summary>The well-known index of key_dc</summary>
            public const int KeyDelete = 59;
            /// <summary>The well-known index of key_down</summary>
            public const int KeyDown = 61;
            /// <summary>The well-known index of key_f1</summary>
            public const int KeyF1 = 66;
            /// <summary>The well-known index of key_f10</summary>
            public const int KeyF10 = 67;
            /// <summary>The well-known index of key_f2</summary>
            public const int KeyF2 = 68;
            /// <summary>The well-known index of key_f3</summary>
            public const int KeyF3 = 69;
            /// <summary>The well-known index of key_f4</summary>
            public const int KeyF4 = 70;
            /// <summary>The well-known index of key_f5</summary>
            public const int KeyF5 = 71;
            /// <summary>The well-known index of key_f6</summary>
            public const int KeyF6 = 72;
            /// <summary>The well-known index of key_f7</summary>
            public const int KeyF7 = 73;
            /// <summary>The well-known index of key_f8</summary>
            public const int KeyF8 = 74;
            /// <summary>The well-known index of key_f9</summary>
            public const int KeyF9 = 75;
            /// <summary>The well-known index of key_home</summary>
            public const int KeyHome = 76;
            /// <summary>The well-known index of key_ic</summary>
            public const int KeyInsert = 77;
            /// <summary>The well-known index of key_left</summary>
            public const int KeyLeft = 79;
            /// <summary>The well-known index of key_npage</summary>
            public const int KeyPageDown = 81;
            /// <summary>The well-known index of key_ppage</summary>
            public const int KeyPageUp = 82;
            /// <summary>The well-known index of key_right</summary>
            public const int KeyRight = 83;
            /// <summary>The well-known index of key_sf</summary>
            public const int KeyScrollForward = 84;
            /// <summary>The well-known index of key_sr</summary>
            public const int KeyScrollReverse = 85;
            /// <summary>The well-known index of key_up</summary>
            public const int KeyUp = 87;
            /// <summary>The well-known index of keypad_xmit</summary>
            public const int KeypadXmit = 89;
            /// <summary>The well-known index of key_btab</summary>
            public const int KeyBackTab = 148;
            /// <summary>The well-known index of key_beg</summary>
            public const int KeyBegin = 158;
            /// <summary>The well-known index of key_end</summary>
            public const int KeyEnd = 164;
            /// <summary>The well-known index of key_enter</summary>
            public const int KeyEnter = 165;
            /// <summary>The well-known index of key_help</summary>
            public const int KeyHelp = 168;
            /// <summary>The well-known index of key_print</summary>
            public const int KeyPrint = 176;
            /// <summary>The well-known index of key_sbeg</summary>
            public const int KeySBegin = 186;
            /// <summary>The well-known index of key_sdc</summary>
            public const int KeySDelete = 191;
            /// <summary>The well-known index of key_select</summary>
            public const int KeySelect = 193;
            /// <summary>The well-known index of key_shelp</summary>
            public const int KeySHelp = 198;
            /// <summary>The well-known index of key_shome</summary>
            public const int KeySHome = 199;
            /// <summary>The well-known index of key_sleft</summary>
            public const int KeySLeft = 201;
            /// <summary>The well-known index of key_sprint</summary>
            public const int KeySPrint = 207;
            /// <summary>The well-known index of key_sright</summary>
            public const int KeySRight = 210;
            /// <summary>The well-known index of key_f11</summary>
            public const int KeyF11 = 216;
            /// <summary>The well-known index of key_f12</summary>
            public const int KeyF12 = 217;
            /// <summary>The well-known index of key_f13</summary>
            public const int KeyF13 = 218;
            /// <summary>The well-known index of key_f14</summary>
            public const int KeyF14 = 219;
            /// <summary>The well-known index of key_f15</summary>
            public const int KeyF15 = 220;
            /// <summary>The well-known index of key_f16</summary>
            public const int KeyF16 = 221;
            /// <summary>The well-known index of key_f17</summary>
            public const int KeyF17 = 222;
            /// <summary>The well-known index of key_f18</summary>
            public const int KeyF18 = 223;
            /// <summary>The well-known index of key_f19</summary>
            public const int KeyF19 = 224;
            /// <summary>The well-known index of key_f20</summary>
            public const int KeyF20 = 225;
            /// <summary>The well-known index of key_f21</summary>
            public const int KeyF21 = 226;
            /// <summary>The well-known index of key_f22</summary>
            public const int KeyF22 = 227;
            /// <summary>The well-known index of key_f23</summary>
            public const int KeyF23 = 228;
            /// <summary>The well-known index of key_f24</summary>
            public const int KeyF24 = 229;

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
                int end = FindNullTerminator(buffer, pos);
                return Encoding.ASCII.GetString(buffer, pos, end - pos);
            }

            /// <summary>Finds the null-terminator for a string that begins at the specified position.</summary>
            private static int FindNullTerminator(byte[] buffer, int pos)
            {
                int termPos = pos;
                while (termPos < buffer.Length && buffer[termPos] != '\0') termPos++;
                return termPos;
            }
        }

        /// <summary>Provides support for evaluating parameterized terminfo database format strings.</summary>
        internal static class ParameterizedStrings
        {
            /// <summary>A cached stack to use to avoid allocating a new stack object for every evaluation.</summary>
            [ThreadStatic]
            private static LowLevelStack<FormatParam> t_cachedStack;

            /// <summary>A cached array of arguments to use to avoid allocating a new array object for every evaluation.</summary>
            [ThreadStatic]
            private static FormatParam[] t_cachedOneElementArgsArray;

            /// <summary>A cached array of arguments to use to avoid allocating a new array object for every evaluation.</summary>
            [ThreadStatic]
            private static FormatParam[] t_cachedTwoElementArgsArray;

            /// <summary>Evaluates a terminfo formatting string, using the supplied argument.</summary>
            /// <param name="format">The format string.</param>
            /// <param name="arg">The argument to the format string.</param>
            /// <returns>The formatted string.</returns>
            public static string Evaluate(string format, FormatParam arg)
            {
                FormatParam[] args = t_cachedOneElementArgsArray;
                if (args == null)
                {
                    t_cachedOneElementArgsArray = args = new FormatParam[1];
                }

                args[0] = arg;

                return Evaluate(format, args);
            }

            /// <summary>Evaluates a terminfo formatting string, using the supplied arguments.</summary>
            /// <param name="format">The format string.</param>
            /// <param name="arg1">The first argument to the format string.</param>
            /// <param name="arg2">The second argument to the format string.</param>
            /// <returns>The formatted string.</returns>
            public static string Evaluate(string format, FormatParam arg1, FormatParam arg2)
            {
                FormatParam[] args = t_cachedTwoElementArgsArray;
                if (args == null)
                {
                    t_cachedTwoElementArgsArray = args = new FormatParam[2];
                }

                args[0] = arg1;
                args[1] = arg2;

                return Evaluate(format, args);
            }

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
                LowLevelStack<FormatParam> stack = t_cachedStack;
                if (stack == null)
                {
                    t_cachedStack = stack = new LowLevelStack<FormatParam>();
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
}
