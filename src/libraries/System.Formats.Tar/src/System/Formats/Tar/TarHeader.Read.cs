// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Formats.Tar
{
    // Reads the header attributes from a tar archive entry.
    internal sealed partial class TarHeader
    {
        private const string UstarPrefixFormat = "{0}/{1}"; // "prefix/name"

        // Attempts to retrieve the next header from the specified tar archive stream.
        // Throws if end of stream is reached or if any data type conversion fails.
        // Returns a valid TarHeader object if the attributes were read successfully, null otherwise.
        internal static TarHeader? TryGetNextHeader(Stream archiveStream, bool copyData, TarEntryFormat initialFormat)
        {
            // The four supported formats have a header that fits in the default record size
            Span<byte> buffer = stackalloc byte[TarHelpers.RecordSize];

            archiveStream.ReadExactly(buffer);

            TarHeader? header = TryReadAttributes(initialFormat, buffer);
            if (header != null)
            {
                header.ProcessDataBlock(archiveStream, copyData);
            }

            return header;
        }

        // Asynchronously attempts read all the fields of the next header.
        // Throws if end of stream is reached or if any data type conversion fails.
        // Returns true if all the attributes were read successfully, false otherwise.
        internal static async ValueTask<TarHeader?> TryGetNextHeaderAsync(Stream archiveStream, bool copyData, TarEntryFormat initialFormat, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // The four supported formats have a header that fits in the default record size
            byte[] rented = ArrayPool<byte>.Shared.Rent(minimumLength: TarHelpers.RecordSize);
            Memory<byte> buffer = rented.AsMemory(0, TarHelpers.RecordSize); // minimumLength means the array could've been larger

            await archiveStream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);

            TarHeader? header = TryReadAttributes(initialFormat, buffer.Span);
            if (header != null)
            {
                await header.ProcessDataBlockAsync(archiveStream, copyData, cancellationToken).ConfigureAwait(false);
            }

            ArrayPool<byte>.Shared.Return(rented);

            return header;
        }

        private static TarHeader? TryReadAttributes(TarEntryFormat initialFormat, Span<byte> buffer)
        {
            // Confirms if v7 or pax, or tentatively selects ustar
            TarHeader? header = TryReadCommonAttributes(buffer, initialFormat);
            if (header != null)
            {
                // Confirms if gnu, or tentatively selects ustar
                header.ReadMagicAttribute(buffer);

                if (header._format != TarEntryFormat.V7)
                {
                    // Confirms if gnu
                    header.ReadVersionAttribute(buffer);

                    // Fields that ustar, pax and gnu share identically
                    header.ReadPosixAndGnuSharedAttributes(buffer);

                    Debug.Assert(header._format is TarEntryFormat.Ustar or TarEntryFormat.Pax or TarEntryFormat.Gnu);
                    if (header._format == TarEntryFormat.Ustar)
                    {
                        header.ReadUstarAttributes(buffer);
                    }
                    else if (header._format == TarEntryFormat.Gnu)
                    {
                        header.ReadGnuAttributes(buffer);
                    }
                    // In PAX, there is nothing to read in this section (empty space)
                }
            }
            return header;
        }

        // Reads the elements from the passed dictionary, which comes from the previous extended attributes entry,
        // and inserts or replaces those elements into the current header's dictionary.
        // If any of the dictionary entries use the name of a standard attribute, that attribute's value gets replaced with the one from the dictionary.
        // Unlike the historic header, numeric values in extended attributes are stored using decimal, not octal.
        // Throws if any conversion from string to the expected data type fails.
        internal void ReplaceNormalAttributesWithExtended(Dictionary<string, string>? dictionaryFromExtendedAttributesHeader)
        {
            if (dictionaryFromExtendedAttributesHeader == null || dictionaryFromExtendedAttributesHeader.Count == 0)
            {
                return;
            }

            InitializeExtendedAttributesWithExisting(dictionaryFromExtendedAttributesHeader);

            // Find all the extended attributes with known names and save them in the expected standard attribute.

            // The 'name' header field only fits 100 bytes, so we always store the full name text to the dictionary.
            if (ExtendedAttributes.TryGetValue(PaxEaName, out string? paxEaName))
            {
                _name = paxEaName;
            }

            // The 'linkName' header field only fits 100 bytes, so we always store the full linkName text to the dictionary.
            if (ExtendedAttributes.TryGetValue(PaxEaLinkName, out string? paxEaLinkName))
            {
                _linkName = paxEaLinkName;
            }

            // The 'mtime' header field only fits 12 bytes, so a more precise timestamp goes in the extended attributes
            if (TarHelpers.TryGetDateTimeOffsetFromTimestampString(ExtendedAttributes, PaxEaMTime, out DateTimeOffset mTime))
            {
                _mTime = mTime;
            }

            // The user could've stored an override in the extended attributes
            if (TarHelpers.TryGetStringAsBaseTenInteger(ExtendedAttributes, PaxEaMode, out int mode))
            {
                _mode = mode;
            }

            // The 'size' header field only fits 12 bytes, so the data section length that surpases that limit needs to be retrieved
            if (TarHelpers.TryGetStringAsBaseTenLong(ExtendedAttributes, PaxEaSize, out long size))
            {
                _size = size;
            }

            // The 'uid' header field only fits 8 bytes, or the user could've stored an override in the extended attributes
            if (TarHelpers.TryGetStringAsBaseTenInteger(ExtendedAttributes, PaxEaUid, out int uid))
            {
                _uid = uid;
            }

            // The 'gid' header field only fits 8 bytes, or the user could've stored an override in the extended attributes
            if (TarHelpers.TryGetStringAsBaseTenInteger(ExtendedAttributes, PaxEaGid, out int gid))
            {
                _gid = gid;
            }

            // The 'uname' header field only fits 32 bytes
            if (ExtendedAttributes.TryGetValue(PaxEaUName, out string? paxEaUName))
            {
                _uName = paxEaUName;
            }

            // The 'gname' header field only fits 32 bytes
            if (ExtendedAttributes.TryGetValue(PaxEaGName, out string? paxEaGName))
            {
                _gName = paxEaGName;
            }

            // The 'devmajor' header field only fits 8 bytes, or the user could've stored an override in the extended attributes
            if (TarHelpers.TryGetStringAsBaseTenInteger(ExtendedAttributes, PaxEaDevMajor, out int devMajor))
            {
                _devMajor = devMajor;
            }

            // The 'devminor' header field only fits 8 bytes, or the user could've stored an override in the extended attributes
            if (TarHelpers.TryGetStringAsBaseTenInteger(ExtendedAttributes, PaxEaDevMinor, out int devMinor))
            {
                _devMinor = devMinor;
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

        private async Task ProcessDataBlockAsync(Stream archiveStream, bool copyData, CancellationToken cancellationToken)
        {
            bool skipBlockAlignmentPadding = true;

            switch (_typeFlag)
            {
                case TarEntryType.ExtendedAttributes or TarEntryType.GlobalExtendedAttributes:
                    await ReadExtendedAttributesBlockAsync(archiveStream, cancellationToken).ConfigureAwait(false);
                    break;
                case TarEntryType.LongLink or TarEntryType.LongPath:
                    await ReadGnuLongPathDataBlockAsync(archiveStream, cancellationToken).ConfigureAwait(false);
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
                    _dataStream = await GetDataStreamAsync(archiveStream, copyData, _size, cancellationToken).ConfigureAwait(false);
                    if (_dataStream is SeekableSubReadStream)
                    {
                        await TarHelpers.AdvanceStreamAsync(archiveStream, _size, cancellationToken).ConfigureAwait(false);
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
                    await TarHelpers.SkipBlockAlignmentPaddingAsync(archiveStream, _size, cancellationToken).ConfigureAwait(false);
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

        // Asynchronously returns a stream that represents the data section of the current header.
        // If copyData is true, then a total number of _size bytes will be copied to a new MemoryStream, which is then returned.
        // Otherwise, if the archive stream is seekable, returns a seekable wrapper stream.
        // Otherwise, it returns an unseekable wrapper stream.
        private static async ValueTask<Stream?> GetDataStreamAsync(Stream archiveStream, bool copyData, long size, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (size == 0)
            {
                return null;
            }

            if (copyData)
            {
                MemoryStream copiedData = new MemoryStream();
                await TarHelpers.CopyBytesAsync(archiveStream, copiedData, size, cancellationToken).ConfigureAwait(false);
                return copiedData;
            }

            return archiveStream.CanSeek
                ? new SeekableSubReadStream(archiveStream, archiveStream.Position, size)
                : new SubReadStream(archiveStream, 0, size);
        }

        // Attempts to read the fields shared by all formats and stores them in their expected data type.
        // Throws if any data type conversion fails.
        // Returns true on success, false if checksum is zero.
        private static TarHeader? TryReadCommonAttributes(Span<byte> buffer, TarEntryFormat initialFormat)
        {
            // Start by collecting fields that need special checks that return early when data is wrong

            // Empty checksum means this is an invalid (all blank) entry, finish early
            Span<byte> spanChecksum = buffer.Slice(FieldLocations.Checksum, FieldLengths.Checksum);
            if (TarHelpers.IsAllNullBytes(spanChecksum))
            {
                return null;
            }
            int checksum = TarHelpers.GetTenBaseNumberFromOctalAsciiChars(spanChecksum);
            // Zero checksum means the whole header is empty
            if (checksum == 0)
            {
                return null;
            }

            long size = TarHelpers.GetTenBaseNumberFromOctalAsciiChars(buffer.Slice(FieldLocations.Size, FieldLengths.Size));
            if (size < 0)
            {
                throw new FormatException(string.Format(SR.TarSizeFieldNegative));
            }

            // Continue with the rest of the fields that require no special checks
            TarHeader header = new(initialFormat,
                name: TarHelpers.GetTrimmedUtf8String(buffer.Slice(FieldLocations.Name, FieldLengths.Name)),
                mode: TarHelpers.GetTenBaseNumberFromOctalAsciiChars(buffer.Slice(FieldLocations.Mode, FieldLengths.Mode)),
                mTime: TarHelpers.GetDateTimeOffsetFromSecondsSinceEpoch(TarHelpers.GetTenBaseLongFromOctalAsciiChars(buffer.Slice(FieldLocations.MTime, FieldLengths.MTime))),
                typeFlag: (TarEntryType)buffer[FieldLocations.TypeFlag])
            {
                _checksum = checksum,
                _size = size,
                _uid = TarHelpers.GetTenBaseNumberFromOctalAsciiChars(buffer.Slice(FieldLocations.Uid, FieldLengths.Uid)),
                _gid = TarHelpers.GetTenBaseNumberFromOctalAsciiChars(buffer.Slice(FieldLocations.Gid, FieldLengths.Gid)),
                _linkName = TarHelpers.GetTrimmedUtf8String(buffer.Slice(FieldLocations.LinkName, FieldLengths.LinkName))
            };

            if (header._format == TarEntryFormat.Unknown)
            {
                header._format = header._typeFlag switch
                {
                    TarEntryType.ExtendedAttributes or
                    TarEntryType.GlobalExtendedAttributes => TarEntryFormat.Pax,

                    TarEntryType.DirectoryList or
                    TarEntryType.LongLink or
                    TarEntryType.LongPath or
                    TarEntryType.MultiVolume or
                    TarEntryType.RenamedOrSymlinked or
                    TarEntryType.SparseFile or
                    TarEntryType.TapeVolume => TarEntryFormat.Gnu,

                    // V7 is the only one that uses 'V7RegularFile'.
                    TarEntryType.V7RegularFile => TarEntryFormat.V7,

                    // We can quickly determine the *minimum* possible format if the entry type
                    // is the POSIX 'RegularFile', although later we could upgrade it to PAX or GNU
                    _ => (header._typeFlag == TarEntryType.RegularFile) ? TarEntryFormat.Ustar : TarEntryFormat.V7
                };
            }

            return header;
        }

        // Reads fields only found in ustar format or above and converts them to their expected data type.
        // Throws if any conversion fails.
        private void ReadMagicAttribute(Span<byte> buffer)
        {
            Span<byte> magic = buffer.Slice(FieldLocations.Magic, FieldLengths.Magic);

            // If at this point the magic value is all nulls, we definitely have a V7
            if (TarHelpers.IsAllNullBytes(magic))
            {
                _format = TarEntryFormat.V7;
                return;
            }

            // When the magic field is set, the archive is newer than v7.
            _magic = Encoding.ASCII.GetString(magic);

            if (_magic == GnuMagic)
            {
                _format = TarEntryFormat.Gnu;
            }
            else if (_format == TarEntryFormat.V7 && _magic == UstarMagic)
            {
                // Important: Only change to ustar if we had not changed the format to pax already
                _format = TarEntryFormat.Ustar;
            }
        }

        // Reads the version string and determines the format depending on its value.
        // Throws if converting the bytes to string fails or if an unexpected version string is found.
        private void ReadVersionAttribute(Span<byte> buffer)
        {
            if (_format == TarEntryFormat.V7)
            {
                return;
            }

            Span<byte> version = buffer.Slice(FieldLocations.Version, FieldLengths.Version);

            _version = Encoding.ASCII.GetString(version);

            // The POSIX formats have a 6 byte Magic "ustar\0", followed by a 2 byte Version "00"
            if ((_format is TarEntryFormat.Ustar or TarEntryFormat.Pax) && _version != UstarVersion)
            {
                throw new FormatException(string.Format(SR.TarPosixFormatExpected, _name));
            }

            // The GNU format has a Magic+Version 8 byte string "ustar  \0"
            if (_format == TarEntryFormat.Gnu && _version != GnuVersion)
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
            long aTime = TarHelpers.GetTenBaseLongFromOctalAsciiChars(buffer.Slice(FieldLocations.ATime, FieldLengths.ATime));
            _aTime = TarHelpers.GetDateTimeOffsetFromSecondsSinceEpoch(aTime);

            long cTime = TarHelpers.GetTenBaseLongFromOctalAsciiChars(buffer.Slice(FieldLocations.CTime, FieldLengths.CTime));
            _cTime = TarHelpers.GetDateTimeOffsetFromSecondsSinceEpoch(cTime);

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
            byte[]? buffer = CreateExtendedAttributesBufferIfSizeIsValid();
            if (buffer != null)
            {
                archiveStream.ReadExactly(buffer);
                ReadExtendedAttributesFromBuffer(buffer, _name);
            }
        }

        // Asynchronously collects the extended attributes found in the data section of a PAX entry of type 'x' or 'g'.
        // Throws if end of stream is reached or if an attribute is malformed.
        private async ValueTask ReadExtendedAttributesBlockAsync(Stream archiveStream, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            byte[]? buffer = CreateExtendedAttributesBufferIfSizeIsValid();
            if (buffer != null)
            {
                await archiveStream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
                ReadExtendedAttributesFromBuffer(buffer, _name);
            }
        }

        // Return a byte array if the size field has a valid value for extended attributes. Otherwise, return null, or throw.
        private byte[]? CreateExtendedAttributesBufferIfSizeIsValid()
        {
            Debug.Assert(_typeFlag is TarEntryType.ExtendedAttributes or TarEntryType.GlobalExtendedAttributes);

            // It is not expected that the extended attributes data section will be longer than Array.MaxLength, considering
            // the size field is 12 bytes long, which fits a number with a value under int.MaxValue.
            if (_size > Array.MaxLength)
            {
                throw new InvalidOperationException(string.Format(SR.TarSizeFieldTooLargeForEntryType, _typeFlag.ToString()));
            }

            if (_size == 0)
            {
                return null;
            }

            return new byte[(int)_size];
        }

        // Returns a dictionary containing the extended attributes collected from the provided byte buffer.
        private void ReadExtendedAttributesFromBuffer(ReadOnlySpan<byte> buffer, string name)
        {
            string dataAsString = TarHelpers.GetTrimmedUtf8String(buffer);

            using StringReader reader = new(dataAsString);

            while (TryGetNextExtendedAttribute(reader, out string? key, out string? value))
            {
                if (ExtendedAttributes.ContainsKey(key))
                {
                    throw new FormatException(string.Format(SR.TarDuplicateExtendedAttribute, name));
                }
                ExtendedAttributes.Add(key, value);
            }
        }

        // Reads the long path found in the data section of a GNU entry of type 'K' or 'L'
        // and replaces Name or LinkName, respectively, with the found string.
        // Throws if end of stream is reached.
        private void ReadGnuLongPathDataBlock(Stream archiveStream)
        {
            byte[]? buffer = CreateGnuLongDataBufferIfSizeIsValid();
            if (buffer != null)
            {
                archiveStream.ReadExactly(buffer);
                ReadGnuLongPathDataFromBuffer(buffer);
            }
        }

        // Asynchronously reads the long path found in the data section of a GNU entry of type 'K' or 'L'
        // and replaces Name or LinkName, respectively, with the found string.
        // Throws if end of stream is reached.
        private async ValueTask ReadGnuLongPathDataBlockAsync(Stream archiveStream, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            byte[]? buffer = CreateGnuLongDataBufferIfSizeIsValid();
            if (buffer != null)
            {
                await archiveStream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
                ReadGnuLongPathDataFromBuffer(buffer);
            }
        }

        // Collects the GNU long path info from the buffer and sets it in the right field depending on the type flag.
        private void ReadGnuLongPathDataFromBuffer(ReadOnlySpan<byte> buffer)
        {
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

        // Return a byte array if the size field has a valid value for GNU long metadata entry data. Otherwise, return null, or throw.
        private byte[]? CreateGnuLongDataBufferIfSizeIsValid()
        {
            Debug.Assert(_typeFlag is TarEntryType.LongLink or TarEntryType.LongPath);

            if (_size > Array.MaxLength)
            {
                throw new InvalidOperationException(string.Format(SR.TarSizeFieldTooLargeForEntryType, _typeFlag.ToString()));
            }

            if (_size == 0)
            {
                return null;
            }

            return new byte[(int)_size];
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
