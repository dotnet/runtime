// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace System.Formats.Tar
{
    // Reads the header attributes from a tar archive entry.
    internal partial struct TarHeader
    {
        private const string UstarPrefixFormat = "{0}/{1}"; // "prefix/name"

        // Attempts to read all the fields of the next header.
        // Throws if end of stream is reached or if any data type conversion fails.
        // Returns true if all the attributes were read successfully, false otherwise.
        internal bool TryGetNextHeader(Stream archiveStream, bool copyData)
        {
            // The four supported formats have a header that fits in the default record size
            byte[] rented = ArrayPool<byte>.Shared.Rent(minimumLength: TarHelpers.RecordSize);

            Span<byte> buffer = rented.AsSpan(0, TarHelpers.RecordSize); // minimumLength means the array could've been larger
            buffer.Clear(); // Rented arrays aren't clean

            TarHelpers.ReadOrThrow(archiveStream, buffer);

            try
            {
                // Confirms if v7 or pax, or tentatively selects ustar
                if (!TryReadCommonAttributes(buffer))
                {
                    return false;
                }

                // Confirms if gnu, or tentatively selects ustar
                ReadMagicAttribute(buffer);

                if (_format != TarFormat.V7)
                {
                    // Confirms if gnu
                    ReadVersionAttribute(buffer);

                    // Fields that ustar, pax and gnu share identically
                    ReadPosixAndGnuSharedAttributes(buffer);

                    Debug.Assert(_format is TarFormat.Ustar or TarFormat.Pax or TarFormat.Gnu);
                    if (_format == TarFormat.Ustar)
                    {
                        ReadUstarAttributes(buffer);
                    }
                    else if (_format == TarFormat.Gnu)
                    {
                        ReadGnuAttributes(buffer);
                    }
                    // In PAX, there is nothing to read in this section (empty space)
                }

                ProcessDataBlock(archiveStream, copyData);

                return true;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }

        // Reads the elements from the passed dictionary, which comes from the first global extended attributes entry,
        // and inserts or replaces those elements into the current header's dictionary.
        // If any of the dictionary entries use the name of a standard attribute (not all of them), that attribute's value gets replaced with the one from the dictionary.
        // Unlike the historic header, numeric values in extended attributes are stored using decimal, not octal.
        // Throws if any conversion from string to the expected data type fails.
        internal void ReplaceNormalAttributesWithGlobalExtended(IReadOnlyDictionary<string, string> gea)
        {
            // First step: Insert or replace all the elements in the passed dictionary into the current header's dictionary.
            foreach ((string key, string value) in gea)
            {
                _extendedAttributes ??= new Dictionary<string, string>();
                _extendedAttributes[key] = value;
            }

            // Second, find only the attributes that make sense to substitute, and replace them.
            if (gea.TryGetValue(PaxEaATime, out string? paxEaATime))
            {
                if (TarHelpers.TryConvertToDateTimeOffset(paxEaATime, out DateTimeOffset aTime))
                {
                    _aTime = aTime;
                }
            }
            if (gea.TryGetValue(PaxEaCTime, out string? paxEaCTime))
            {
                if (TarHelpers.TryConvertToDateTimeOffset(paxEaCTime, out DateTimeOffset cTime))
                {
                    _cTime = cTime;
                }
            }
            if (gea.TryGetValue(PaxEaMTime, out string? paxEaMTime))
            {
                if (TarHelpers.TryConvertToDateTimeOffset(paxEaMTime, out DateTimeOffset mTime))
                {
                    _mTime = mTime;
                }
            }
            if (gea.TryGetValue(PaxEaMode, out string? paxEaMode))
            {
                _mode = Convert.ToInt32(paxEaMode);
            }
            if (gea.TryGetValue(PaxEaUid, out string? paxEaUid))
            {
                _uid = Convert.ToInt32(paxEaUid);
            }
            if (gea.TryGetValue(PaxEaGid, out string? paxEaGid))
            {
                _gid = Convert.ToInt32(paxEaGid);
            }
            if (gea.TryGetValue(PaxEaUName, out string? paxEaUName))
            {
                _uName = paxEaUName;
            }
            if (gea.TryGetValue(PaxEaGName, out string? paxEaGName))
            {
                _gName = paxEaGName;
            }
        }

        // Reads the elements from the passed dictionary, which comes from the previous extended attributes entry,
        // and inserts or replaces those elements into the current header's dictionary.
        // If any of the dictionary entries use the name of a standard attribute, that attribute's value gets replaced with the one from the dictionary.
        // Unlike the historic header, numeric values in extended attributes are stored using decimal, not octal.
        // Throws if any conversion from string to the expected data type fails.
        internal void ReplaceNormalAttributesWithExtended(IEnumerable<KeyValuePair<string, string>> extendedAttributesEnumerable)
        {
            Dictionary<string, string> ea = new Dictionary<string, string>(extendedAttributesEnumerable);
            if (ea.Count == 0)
            {
                return;
            }
            _extendedAttributes ??= new Dictionary<string, string>();

            // First step: Insert or replace all the elements in the passed dictionary into the current header's dictionary.
            foreach ((string key, string value) in ea)
            {
                _extendedAttributes[key] = value;
            }

            // Second, find all the extended attributes with known names and save them in the expected standard attribute.
            if (ea.TryGetValue(PaxEaName, out string? paxEaName))
            {
                _name = paxEaName;
            }
            if (ea.TryGetValue(PaxEaLinkName, out string? paxEaLinkName))
            {
                _linkName = paxEaLinkName;
            }
            if (ea.TryGetValue(PaxEaATime, out string? paxEaATime))
            {
                if (TarHelpers.TryConvertToDateTimeOffset(paxEaATime, out DateTimeOffset aTime))
                {
                    _aTime = aTime;
                }
            }
            if (ea.TryGetValue(PaxEaCTime, out string? paxEaCTime))
            {
                if (TarHelpers.TryConvertToDateTimeOffset(paxEaCTime, out DateTimeOffset cTime))
                {
                    _cTime = cTime;
                }
            }
            if (ea.TryGetValue(PaxEaMTime, out string? paxEaMTime))
            {
                if (TarHelpers.TryConvertToDateTimeOffset(paxEaMTime, out DateTimeOffset mTime))
                {
                    _mTime = mTime;
                }
            }
            if (ea.TryGetValue(PaxEaMode, out string? paxEaMode))
            {
                _mode = Convert.ToInt32(paxEaMode);
            }
            if (ea.TryGetValue(PaxEaSize, out string? paxEaSize))
            {
                _size = Convert.ToInt32(paxEaSize);
            }
            if (ea.TryGetValue(PaxEaUid, out string? paxEaUid))
            {
                _uid = Convert.ToInt32(paxEaUid);
            }
            if (ea.TryGetValue(PaxEaGid, out string? paxEaGid))
            {
                _gid = Convert.ToInt32(paxEaGid);
            }
            if (ea.TryGetValue(PaxEaUName, out string? paxEaUName))
            {
                _uName = paxEaUName;
            }
            if (ea.TryGetValue(PaxEaGName, out string? paxEaGName))
            {
                _gName = paxEaGName;
            }
            if (ea.TryGetValue(PaxEaDevMajor, out string? paxEaDevMajor))
            {
                _devMajor = int.Parse(paxEaDevMajor);
            }
            if (ea.TryGetValue(PaxEaDevMinor, out string? paxEaDevMinor))
            {
                _devMinor = int.Parse(paxEaDevMinor);
            }
        }

        // Determines what kind of stream needs to be saved for the data section.
        // - Metadata typeflag entries (Extended Attributes and Global Extended Attributes in PAX, LongLink and LongPath in GNU)
        //   will get all the data section read and the stream pointer positioned at the beginning of the next header.
        // - Block, Character, Directory, Fifo, HardLink and SymbolicLink typeflag entries have no data section so the archive stream pointer will be positioned at the beginning of the next header.
        // - All other typeflag entries with a data section will generate a stream wrapping the data section: SeekableSubReadStream for seekable archive streams, and SubReadStream for unseekable archive streams.
        private void ProcessDataBlock(Stream archiveStream, bool copyData)
        {
            bool skipBlockAlignmentPadding = true;

            switch (_typeFlag)
            {
                case TarEntryType.ExtendedAttributes or TarEntryType.GlobalExtendedAttributes:
                    ReadExtendedAttributesBlock(archiveStream);
                    break;
                case TarEntryType.LongLink or TarEntryType.LongPath:
                    ReadGnuLongPathDataBlock(archiveStream);
                    break;
                case TarEntryType.BlockDevice:
                case TarEntryType.CharacterDevice:
                case TarEntryType.Directory:
                case TarEntryType.Fifo:
                case TarEntryType.HardLink:
                case TarEntryType.SymbolicLink:
                    // No data section
                    break;
                case TarEntryType.RegularFile:
                case TarEntryType.V7RegularFile: // Treated as regular file
                case TarEntryType.ContiguousFile: // Treated as regular file
                case TarEntryType.DirectoryList: // Contains the list of filesystem entries in the data section
                case TarEntryType.MultiVolume: // Contains portion of a file
                case TarEntryType.RenamedOrSymlinked: // Might contain data
                case TarEntryType.SparseFile: // Contains portion of a file
                case TarEntryType.TapeVolume: // Might contain data
                default: // Unrecognized entry types could potentially have a data section
                    _dataStream = GetDataStream(archiveStream, copyData);
                    if (_dataStream is SeekableSubReadStream)
                    {
                        TarHelpers.AdvanceStream(archiveStream, _size);
                    }
                    else if (_dataStream is SubReadStream)
                    {
                        // This stream gives the user the chance to optionally read the data section
                        // when the underlying archive stream is unseekable
                        skipBlockAlignmentPadding = false;
                    }

                    break;
            }

            if (skipBlockAlignmentPadding)
            {
                if (_size > 0)
                {
                    TarHelpers.SkipBlockAlignmentPadding(archiveStream, _size);
                }

                if (archiveStream.CanSeek)
                {
                    _endOfHeaderAndDataAndBlockAlignment = archiveStream.Position;
                }
            }
        }

        // Returns a stream that represents the data section of the current header.
        // If copyData is true, then a total number of _size bytes will be copied to a new MemoryStream, which is then returned.
        // Otherwise, if the archive stream is seekable, returns a seekable wrapper stream.
        // Otherwise, it returns an unseekable wrapper stream.
        private Stream? GetDataStream(Stream archiveStream, bool copyData)
        {
            if (_size == 0)
            {
                return null;
            }

            if (copyData)
            {
                MemoryStream copiedData = new MemoryStream();
                TarHelpers.CopyBytes(archiveStream, copiedData, _size);
                return copiedData;
            }

            return archiveStream.CanSeek
                ? new SeekableSubReadStream(archiveStream, archiveStream.Position, _size)
                : new SubReadStream(archiveStream, 0, _size);
        }

        // Attempts to read the fields shared by all formats and stores them in their expected data type.
        // Throws if any data type conversion fails.
        // Returns true on success, false if checksum is zero.
        private bool TryReadCommonAttributes(Span<byte> buffer)
        {
            // Start by collecting fields that need special checks that return early when data is wrong

            // Empty checksum means this is an invalid (all blank) entry, finish early
            Span<byte> spanChecksum = buffer.Slice(FieldLocations.Checksum, FieldLengths.Checksum);
            if (TarHelpers.IsAllNullBytes(spanChecksum))
            {
                return false;
            }
            _checksum = TarHelpers.GetTenBaseNumberFromOctalAsciiChars(spanChecksum);
            // Zero checksum means the whole header is empty
            if (_checksum == 0)
            {
                return false;
            }

            _size = TarHelpers.GetTenBaseNumberFromOctalAsciiChars(buffer.Slice(FieldLocations.Size, FieldLengths.Size));
            if (_size < 0)
            {
                throw new FormatException(string.Format(SR.TarSizeFieldNegative, _name));
            }

            // Continue with the rest of the fields that require no special checks

            _name = TarHelpers.GetTrimmedUtf8String(buffer.Slice(FieldLocations.Name, FieldLengths.Name));
            _mode = TarHelpers.GetTenBaseNumberFromOctalAsciiChars(buffer.Slice(FieldLocations.Mode, FieldLengths.Mode));
            _uid = TarHelpers.GetTenBaseNumberFromOctalAsciiChars(buffer.Slice(FieldLocations.Uid, FieldLengths.Uid));
            _gid = TarHelpers.GetTenBaseNumberFromOctalAsciiChars(buffer.Slice(FieldLocations.Gid, FieldLengths.Gid));
            int mTime = TarHelpers.GetTenBaseNumberFromOctalAsciiChars(buffer.Slice(FieldLocations.MTime, FieldLengths.MTime));
            _mTime = TarHelpers.GetDateTimeFromSecondsSinceEpoch(mTime);
            _typeFlag = (TarEntryType)buffer[FieldLocations.TypeFlag];
            _linkName = TarHelpers.GetTrimmedUtf8String(buffer.Slice(FieldLocations.LinkName, FieldLengths.LinkName));

            if (_format == TarFormat.Unknown)
            {
                _format = _typeFlag switch
                {
                    TarEntryType.ExtendedAttributes or
                    TarEntryType.GlobalExtendedAttributes => TarFormat.Pax,

                    TarEntryType.DirectoryList or
                    TarEntryType.LongLink or
                    TarEntryType.LongPath or
                    TarEntryType.MultiVolume or
                    TarEntryType.RenamedOrSymlinked or
                    TarEntryType.SparseFile or
                    TarEntryType.TapeVolume => TarFormat.Gnu,

                    // V7 is the only one that uses 'V7RegularFile'.
                    TarEntryType.V7RegularFile => TarFormat.V7,

                    // We can quickly determine the *minimum* possible format if the entry type
                    // is the POSIX 'RegularFile', although later we could upgrade it to PAX or GNU
                    _ => (_typeFlag == TarEntryType.RegularFile) ? TarFormat.Ustar : TarFormat.V7
                };
            }

            return true;
        }

        // Reads fields only found in ustar format or above and converts them to their expected data type.
        // Throws if any conversion fails.
        private void ReadMagicAttribute(Span<byte> buffer)
        {
            Span<byte> magic = buffer.Slice(FieldLocations.Magic, FieldLengths.Magic);

            // If at this point the magic value is all nulls, we definitely have a V7
            if (TarHelpers.IsAllNullBytes(magic))
            {
                _format = TarFormat.V7;
                return;
            }

            // When the magic field is set, the archive is newer than v7.
            _magic = Encoding.ASCII.GetString(magic);

            if (_magic == GnuMagic)
            {
                _format = TarFormat.Gnu;
            }
            else if (_format == TarFormat.V7 && _magic == UstarMagic)
            {
                // Important: Only change to ustar if we had not changed the format to pax already
                _format = TarFormat.Ustar;
            }
        }

        // Reads the version string and determines the format depending on its value.
        // Throws if converting the bytes to string fails or if an unexpected version string is found.
        private void ReadVersionAttribute(Span<byte> buffer)
        {
            if (_format == TarFormat.V7)
            {
                return;
            }

            Span<byte> version = buffer.Slice(FieldLocations.Version, FieldLengths.Version);

            _version = Encoding.ASCII.GetString(version);

            // The POSIX formats have a 6 byte Magic "ustar\0", followed by a 2 byte Version "00"
            if ((_format is TarFormat.Ustar or TarFormat.Pax) && _version != UstarVersion)
            {
                throw new FormatException(string.Format(SR.TarPosixFormatExpected, _name));
            }

            // The GNU format has a Magic+Version 8 byte string "ustar  \0"
            if (_format == TarFormat.Gnu && _version != GnuVersion)
            {
                throw new FormatException(string.Format(SR.TarGnuFormatExpected, _name));
            }
        }

        // Reads the attributes shared by the POSIX and GNU formats.
        // Throws if converting the bytes to their expected data type fails.
        private void ReadPosixAndGnuSharedAttributes(Span<byte> buffer)
        {
            // Convert the byte arrays
            _uName = TarHelpers.GetTrimmedAsciiString(buffer.Slice(FieldLocations.UName, FieldLengths.UName));
            _gName = TarHelpers.GetTrimmedAsciiString(buffer.Slice(FieldLocations.GName, FieldLengths.GName));

            // DevMajor and DevMinor only have values with character devices and block devices.
            // For all other typeflags, the values in these fields are irrelevant.
            if (_typeFlag is TarEntryType.CharacterDevice or TarEntryType.BlockDevice)
            {
                // Major number for a character device or block device entry.
                _devMajor = TarHelpers.GetTenBaseNumberFromOctalAsciiChars(buffer.Slice(FieldLocations.DevMajor, FieldLengths.DevMajor));

                // Minor number for a character device or block device entry.
                _devMinor = TarHelpers.GetTenBaseNumberFromOctalAsciiChars(buffer.Slice(FieldLocations.DevMinor, FieldLengths.DevMinor));
            }
        }

        // Reads attributes specific to the GNU format.
        // Throws if any conversion fails.
        private void ReadGnuAttributes(Span<byte> buffer)
        {
            // Convert byte arrays
            int aTime = TarHelpers.GetTenBaseNumberFromOctalAsciiChars(buffer.Slice(FieldLocations.ATime, FieldLengths.ATime));
            _aTime = TarHelpers.GetDateTimeFromSecondsSinceEpoch(aTime);

            int cTime = TarHelpers.GetTenBaseNumberFromOctalAsciiChars(buffer.Slice(FieldLocations.CTime, FieldLengths.CTime));
            _cTime = TarHelpers.GetDateTimeFromSecondsSinceEpoch(cTime);

            // TODO: Read the bytes of the currently unsupported GNU fields, in case user wants to write this entry into another GNU archive, they need to be preserved. https://github.com/dotnet/runtime/issues/68230
        }

        // Reads the ustar prefix attribute.
        // Throws if a conversion to an expected data type fails.
        private void ReadUstarAttributes(Span<byte> buffer)
        {
            _prefix = TarHelpers.GetTrimmedUtf8String(buffer.Slice(FieldLocations.Prefix, FieldLengths.Prefix));

            // In ustar, Prefix is used to store the *leading* path segments of
            // Name, if the full path did not fit in the Name byte array.
            if (!string.IsNullOrEmpty(_prefix))
            {
                // Prefix never has a leading separator, so we add it
                // it should always  be a forward slash for compatibility
                _name = string.Format(UstarPrefixFormat, _prefix, _name);
            }
        }

        // Collects the extended attributes found in the data section of a PAX entry of type 'x' or 'g'.
        // Throws if end of stream is reached or if an attribute is malformed.
        private void ReadExtendedAttributesBlock(Stream archiveStream)
        {
            Debug.Assert(_typeFlag is TarEntryType.ExtendedAttributes or TarEntryType.GlobalExtendedAttributes);

            // Regardless of the size, this entry should always have a valid dictionary object
            _extendedAttributes ??= new Dictionary<string, string>();

            if (_size == 0)
            {
                return;
            }

            // It is not expected that the extended attributes data section will be longer than int.MaxValue, considering
            // 4096 is a common max path length, and also the size field is 12 bytes long, which is under int.MaxValue.
            if (_size > int.MaxValue)
            {
                throw new InvalidOperationException(string.Format(SR.TarSizeFieldTooLargeForExtendedAttribute, _typeFlag.ToString()));
            }

            byte[] buffer = new byte[(int)_size];
            if (archiveStream.Read(buffer.AsSpan()) != _size)
            {
                throw new EndOfStreamException();
            }

            string dataAsString = TarHelpers.GetTrimmedUtf8String(buffer);

            using StringReader reader = new(dataAsString);

            while (TryGetNextExtendedAttribute(reader, out string? key, out string? value))
            {
                _extendedAttributes ??= new Dictionary<string, string>();

                if (_extendedAttributes.ContainsKey(key))
                {
                    throw new FormatException(string.Format(SR.TarDuplicateExtendedAttribute, _name));
                }
                _extendedAttributes.Add(key, value);
            }
        }

        // Reads the long path found in the data section of a GNU entry of type 'K' or 'L'
        // and replaces Name or LinkName, respectively, with the found string.
        // Throws if end of stream is reached.
        private void ReadGnuLongPathDataBlock(Stream archiveStream)
        {
            Debug.Assert(_typeFlag is TarEntryType.LongLink or TarEntryType.LongPath);

            if (_size == 0)
            {
                return;
            }

            byte[] buffer = new byte[(int)_size];

            if (archiveStream.Read(buffer.AsSpan()) != _size)
            {
                throw new EndOfStreamException();
            }

            string longPath = TarHelpers.GetTrimmedUtf8String(buffer);

            if (_typeFlag == TarEntryType.LongLink)
            {
                _linkName = longPath;
            }
            else if (_typeFlag == TarEntryType.LongPath)
            {
                _name = longPath;
            }
        }

        // Tries to collect the next extended attribute from the string wrapped by the specified reader.
        // Extended attributes are saved in the ISO/IEC 10646-1:2000 standard UTF-8 encoding format.
        // https://pubs.opengroup.org/onlinepubs/9699919799/utilities/pax.html
        // "LENGTH KEY=VALUE\n"
        // Where LENGTH is the total number of bytes of that line, from LENGTH itself to the endline, inclusive.
        // Throws if end of stream is reached or if an attribute is malformed.
        private static bool TryGetNextExtendedAttribute(
            StringReader reader,
            [NotNullWhen(returnValue: true)] out string? key,
            [NotNullWhen(returnValue: true)] out string? value)
        {
            key = null;
            value = null;

            string? nextLine = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(nextLine))
            {
                return false;
            }

            StringSplitOptions splitOptions = StringSplitOptions.RemoveEmptyEntries;

            string[] attributeArray = nextLine.Split(' ', 2, splitOptions);
            if (attributeArray.Length != 2)
            {
                return false;
            }

            string[] keyAndValueArray = attributeArray[1].Split('=', 2, splitOptions);
            if (keyAndValueArray.Length != 2)
            {
                return false;
            }

            key = keyAndValueArray[0];
            value = keyAndValueArray[1];

            return true;
        }
    }
}
