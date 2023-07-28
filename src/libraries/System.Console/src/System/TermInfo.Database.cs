// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace System;

internal static partial class TermInfo
{
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
        private readonly Dictionary<string, string>? _extendedStrings;

        /// <summary>Initializes the database instance.</summary>
        /// <param name="term">The name of the terminal.</param>
        /// <param name="data">The data from the terminfo file.</param>
        internal Database(string term, byte[] data)
        {
            _term = term;
            _data = data;

            const int MagicLegacyNumber = 0x11A; // magic number octal 0432 for legacy ncurses terminfo
            const int Magic32BitNumber = 0x21E; // magic number octal 01036 for new ncruses terminfo
            short magic = ReadInt16(data, 0);
            _readAs32Bit =
                magic == MagicLegacyNumber ? false :
                magic == Magic32BitNumber ? true :
                throw new InvalidOperationException(SR.Format(SR.IO_TermInfoInvalidMagicNumber, "O" + Convert.ToString(magic, 8))); // magic number was not recognized. Printing the magic number in octal.
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
            _extendedStrings = ParseExtendedStrings(data, extendedBeginning, _readAs32Bit);
        }

        /// <summary>The name of the associated terminfo, if any.</summary>
        public string Term { get { return _term; } }

        internal bool HasExtendedStrings => _extendedStrings is not null;

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
        public string? GetString(WellKnownStrings stringTableIndex)
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
        public string? GetExtendedString(string name)
        {
            Debug.Assert(name != null);

            string? value;
            return _extendedStrings is not null && _extendedStrings.TryGetValue(name, out value) ? value : null;
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
        private static Dictionary<string, string>? ParseExtendedStrings(byte[] data, int extendedBeginning, bool readAs32Bit)
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
            => BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(pos));

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
            int i = buffer.AsSpan(pos).IndexOf((byte)'\0');
            return i >= 0 ? pos + i : buffer.Length;
        }
    }
}
