// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace System
{
    /// <summary>Provides access to and processing of a terminfo database.</summary>
    internal static class TermInfo
    {
        internal enum WellKnownNumbers
        {
            Columns = 0,
            Lines = 2,
            MaxColors = 13,
        }

        internal enum WellKnownStrings
        {
            Bell = 1,
            Clear = 5,
            ClrEol = 6,
            CursorAddress = 10,
            CursorLeft = 14,
            CursorPositionReport = 294,
            OrigPairs = 297,
            OrigColors = 298,
            SetAnsiForeground = 359,
            SetAnsiBackground = 360,
            CursorInvisible = 13,
            CursorVisible = 16,
            FromStatusLine = 47,
            ToStatusLine = 135,
            KeyBackspace = 55,
            KeyClear = 57,
            KeyDelete = 59,
            KeyDown = 61,
            KeyF1 = 66,
            KeyF10 = 67,
            KeyF2 = 68,
            KeyF3 = 69,
            KeyF4 = 70,
            KeyF5 = 71,
            KeyF6 = 72,
            KeyF7 = 73,
            KeyF8 = 74,
            KeyF9 = 75,
            KeyHome = 76,
            KeyInsert = 77,
            KeyLeft = 79,
            KeyPageDown = 81,
            KeyPageUp = 82,
            KeyRight = 83,
            KeyScrollForward = 84,
            KeyScrollReverse = 85,
            KeyUp = 87,
            KeypadXmit = 89,
            KeyBackTab = 148,
            KeyBegin = 158,
            KeyEnd = 164,
            KeyEnter = 165,
            KeyHelp = 168,
            KeyPrint = 176,
            KeySBegin = 186,
            KeySDelete = 191,
            KeySelect = 193,
            KeySHelp = 198,
            KeySHome = 199,
            KeySLeft = 201,
            KeySPrint = 207,
            KeySRight = 210,
            KeyF11 = 216,
            KeyF12 = 217,
            KeyF13 = 218,
            KeyF14 = 219,
            KeyF15 = 220,
            KeyF16 = 221,
            KeyF17 = 222,
            KeyF18 = 223,
            KeyF19 = 224,
            KeyF20 = 225,
            KeyF21 = 226,
            KeyF22 = 227,
            KeyF23 = 228,
            KeyF24 = 229,
        }

        /// <summary>Provides a terminfo database.</summary>
        internal sealed class Database
        {
            /// <summary>The name of the terminfo file.</summary>
            private readonly string _term;
            /// <summary>Raw data of the database instance.</summary>
            private readonly byte[] _data;

            /// <summary>The number of bytes in the names section of the database.</summary>
            private readonly int _nameSectionNumBytes;
            /// <summary>The number of bytes in the Booleans section of the database.</summary>
            private readonly int _boolSectionNumBytes;
            /// <summary>The number of integers in the numbers section of the database.</summary>
            private readonly int _numberSectionNumInts;
            /// <summary>The number of offsets in the strings section of the database.</summary>
            private readonly int _stringSectionNumOffsets;
            /// <summary>The number of bytes in the strings table of the database.</summary>
            private readonly int _stringTableNumBytes;
            /// <summary>Whether or not to read the number section as 32-bit integers.</summary>
            private readonly bool _readAs32Bit;
            /// <summary>The size of the integers on the number section.</summary>
            private readonly int _sizeOfInt;

            /// <summary>Extended / user-defined entries in the terminfo database.</summary>
            private readonly Dictionary<string, string> _extendedStrings;

            /// <summary>Initializes the database instance.</summary>
            /// <param name="term">The name of the terminal.</param>
            /// <param name="data">The data from the terminfo file.</param>
            private Database(string term, byte[] data)
            {
                _term = term;
                _data = data;

                const int MagicLegacyNumber = 0x11A; // magic number octal 0432 for legacy ncurses terminfo
                const int Magic32BitNumber = 0x21E; // magic number octal 01036 for new ncruses terminfo
                short magic = ReadInt16(data, 0);
                _readAs32Bit =
                    magic == MagicLegacyNumber ? false :
                    magic == Magic32BitNumber ? true :
                    throw new InvalidOperationException(SR.Format(SR.IO_TermInfoInvalidMagicNumber, string.Concat("O" + Convert.ToString(magic, 8)))); // magic number was not recognized. Printing the magic number in octal.
                _sizeOfInt = (_readAs32Bit) ? 4 : 2;

                _nameSectionNumBytes = ReadInt16(data, 2);
                _boolSectionNumBytes = ReadInt16(data, 4);
                _numberSectionNumInts = ReadInt16(data, 6);
                _stringSectionNumOffsets = ReadInt16(data, 8);
                _stringTableNumBytes = ReadInt16(data, 10);
                if (_nameSectionNumBytes < 0 ||
                    _boolSectionNumBytes < 0 ||
                    _numberSectionNumInts < 0 ||
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
                _extendedStrings = ParseExtendedStrings(data, extendedBeginning, _readAs32Bit) ?? new Dictionary<string, string>();
            }

            /// <summary>The name of the associated terminfo, if any.</summary>
            public string Term { get { return _term; } }

            /// <summary>Read the database for the current terminal as specified by the "TERM" environment variable.</summary>
            /// <returns>The database, or null if it could not be found.</returns>
            internal static Database ReadActiveDatabase()
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
                    "/usr/share/misc/terminfo"
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
            private static bool TryOpen(string filePath, out SafeFileHandle fd)
            {
                fd = Interop.Sys.Open(filePath, Interop.Sys.OpenFlags.O_RDONLY | Interop.Sys.OpenFlags.O_CLOEXEC, 0);
                if (fd.IsInvalid)
                {
                    // Don't throw in this case, as we'll be polling multiple locations looking for the file.
                    fd = null;
                    return false;
                }

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

                SafeFileHandle fd;
                if (!TryOpen(directoryPath + "/" + term[0].ToString() + "/" + term, out fd) &&          // /directory/termFirstLetter/term      (Linux)
                    !TryOpen(directoryPath + "/" + ((int)term[0]).ToString("X") + "/" + term, out fd))  // /directory/termFirstLetterAsHex/term (Mac)
                {
                    return null;
                }

                using (fd)
                {
                    // Read in all of the terminfo data
                    long termInfoLength = Interop.CheckIo(Interop.Sys.LSeek(fd, 0, Interop.Sys.SeekWhence.SEEK_END)); // jump to the end to get the file length
                    Interop.CheckIo(Interop.Sys.LSeek(fd, 0, Interop.Sys.SeekWhence.SEEK_SET)); // reset back to beginning
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
            private int StringOffsetsOffset { get { return NumbersOffset + (_numberSectionNumInts * _sizeOfInt); } }

            /// <summary>The offset into data where the string table exists.</summary>
            private int StringsTableOffset { get { return StringOffsetsOffset + (_stringSectionNumOffsets * 2); } }

            /// <summary>Gets a string from the strings section by the string's well-known index.</summary>
            /// <param name="stringTableIndex">The index of the string to find.</param>
            /// <returns>The string if it's in the database; otherwise, null.</returns>
            public string GetString(WellKnownStrings stringTableIndex)
            {
                int index = (int)stringTableIndex;
                Debug.Assert(index >= 0);

                if (index >= _stringSectionNumOffsets)
                {
                    // Some terminfo files may not contain enough entries to actually 
                    // have the requested one.
                    return null;
                }

                int tableIndex = ReadInt16(_data, StringOffsetsOffset + (index * 2));
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
            public int GetNumber(WellKnownNumbers numberIndex)
            {
                int index = (int)numberIndex;
                Debug.Assert(index >= 0);

                if (index >= _numberSectionNumInts)
                {
                    // Some terminfo files may not contain enough entries to actually
                    // have the requested one.
                    return -1;
                }

                return ReadInt(_data, NumbersOffset + (index * _sizeOfInt), _readAs32Bit);
            }

            /// <summary>Parses the extended string information from the terminfo data.</summary>
            /// <returns>
            /// A dictionary of the name to value mapping.  As this section of the terminfo isn't as well
            /// defined as the earlier portions, and may not even exist, the parsing is more lenient about
            /// errors, returning an empty collection rather than throwing.
            /// </returns>
            private static Dictionary<string, string> ParseExtendedStrings(byte[] data, int extendedBeginning, bool readAs32Bit)
            {
                const int ExtendedHeaderSize = 10;
                int sizeOfIntValuesInBytes = (readAs32Bit) ? 4 : 2;
                if (extendedBeginning + ExtendedHeaderSize >= data.Length)
                {
                    // Exit out as there's no extended information.
                    return null;
                }

                // Read in extended counts, and exit out if we got any incorrect info
                int extendedBoolCount = ReadInt16(data, extendedBeginning);
                int extendedNumberCount = ReadInt16(data, extendedBeginning + (2 * 1));
                int extendedStringCount = ReadInt16(data, extendedBeginning + (2 * 2));
                int extendedStringNumOffsets = ReadInt16(data, extendedBeginning + (2 * 3));
                int extendedStringTableByteSize = ReadInt16(data, extendedBeginning + (2 * 4));
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
                    (extendedNumberCount * sizeOfIntValuesInBytes); // and past all of the extended numbers

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

            /// <summary>Read a 16-bit or 32-bit value from the buffer starting at the specified position.</summary>
            /// <param name="buffer">The buffer from which to read.</param>
            /// <param name="pos">The position at which to read.</param>
            /// <param name="readAs32Bit">Whether or not to read value as 32-bit. Will read as 16-bit if set to false.</param>
            /// <returns>The value read.</returns>
            private static int ReadInt(byte[] buffer, int pos, bool readAs32Bit) =>
                readAs32Bit ? ReadInt32(buffer, pos) : ReadInt16(buffer, pos);

            /// <summary>Read a 16-bit value from the buffer starting at the specified position.</summary>
            /// <param name="buffer">The buffer from which to read.</param>
            /// <param name="pos">The position at which to read.</param>
            /// <returns>The 16-bit value read.</returns>
            private static short ReadInt16(byte[] buffer, int pos)
            {
                return unchecked((short)
                    ((((int)buffer[pos + 1]) << 8) |
                     ((int)buffer[pos] & 0xff)));
            }

            /// <summary>Read a 32-bit value from the buffer starting at the specified position.</summary>
            /// <param name="buffer">The buffer from which to read.</param>
            /// <param name="pos">The position at which to read.</param>
            /// <returns>The 32-bit value read.</returns>
            private static int ReadInt32(byte[] buffer, int pos)
            {
                return (int)((buffer[pos] & 0xff) | 
                             buffer[pos + 1] << 8 | 
                             buffer[pos + 2] << 16 | 
                             buffer[pos + 3] << 24);
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
            private static Stack<FormatParam> t_cachedStack;

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
                    throw new ArgumentNullException(nameof(format));
                }
                if (args == null)
                {
                    throw new ArgumentNullException(nameof(args));
                }

                // Initialize the stack to use for processing.
                Stack<FormatParam> stack = t_cachedStack;
                if (stack == null)
                {
                    t_cachedStack = stack = new Stack<FormatParam>();
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
                string format, ref int pos, FormatParam[] args, Stack<FormatParam> stack,
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
                        // but some uses occur when there's only a single argument. To make sure we accommodate these files, we increment the values 
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
            private static bool AsBool(int i) { return i != 0; }

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
                Debug.Assert(arg is string || arg is int);

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
                fixed (byte* ptr = &bytes[0])
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
            public readonly struct FormatParam
            {
                /// <summary>The integer stored in the parameter.</summary>
                private readonly int _int32;
                /// <summary>The string stored in the parameter.</summary>
                private readonly string _string; // null means an Int32 is stored

                /// <summary>Initializes the parameter with an integer value.</summary>
                /// <param name="value">The value to be stored in the parameter.</param>
                public FormatParam(int value) : this(value, null) { }

                /// <summary>Initializes the parameter with a string value.</summary>
                /// <param name="value">The value to be stored in the parameter.</param>
                public FormatParam(string value) : this(0, value ?? string.Empty) { }

                /// <summary>Initializes the parameter.</summary>
                /// <param name="intValue">The integer value.</param>
                /// <param name="stringValue">The string value.</param>
                private FormatParam(int intValue, string stringValue)
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
        }
    }
}
