// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Globalization;
using System.IO;
using System.Text;

namespace System.Formats.Tar
{
    // Static class containing a variety of helper methods.
    internal static class TarHelpers
    {
        internal const short RecordSize = 512;
        internal const int MaxBufferLength = 4096;

        internal const int ZeroChar = 0x30;
        internal const byte SpaceChar = 0x20;
        internal const byte EqualsChar = 0x3d;
        internal const byte NewLineChar = 0xa;

        internal const TarFileMode DefaultMode = // 644 in octal
            TarFileMode.UserRead | TarFileMode.UserWrite | TarFileMode.GroupRead | TarFileMode.OtherRead;

        // Helps advance the stream a total number of bytes larger than int.MaxValue.
        internal static void AdvanceStream(Stream archiveStream, long bytesToDiscard)
        {
            if (archiveStream.CanSeek)
            {
                archiveStream.Position += bytesToDiscard;
            }
            else if (bytesToDiscard > 0)
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(minimumLength: MaxBufferLength);
                while (bytesToDiscard > 0)
                {
                    int currentLengthToRead = (int)Math.Min(MaxBufferLength, bytesToDiscard);
                    if (archiveStream.Read(buffer.AsSpan(0, currentLengthToRead)) != currentLengthToRead)
                    {
                        throw new EndOfStreamException();
                    }
                    bytesToDiscard -= currentLengthToRead;
                }
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        // Helps copy a specific number of bytes from one stream into another.
        internal static void CopyBytes(Stream origin, Stream destination, long bytesToCopy)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(minimumLength: MaxBufferLength);
            while (bytesToCopy > 0)
            {
                int currentLengthToRead = (int)Math.Min(MaxBufferLength, bytesToCopy);
                if (origin.Read(buffer.AsSpan(0, currentLengthToRead)) != currentLengthToRead)
                {
                    throw new EndOfStreamException();
                }
                destination.Write(buffer.AsSpan(0, currentLengthToRead));
                bytesToCopy -= currentLengthToRead;
            }
            ArrayPool<byte>.Shared.Return(buffer);
        }

        // Returns the number of bytes until the next multiple of the record size.
        internal static int CalculatePadding(long size)
        {
            long ceilingMultipleOfRecordSize = ((RecordSize - 1) | (size - 1)) + 1;
            int padding = (int)(ceilingMultipleOfRecordSize - size);
            return padding;
        }

        // Returns the specified 8-base number as a 10-base number.
        internal static int ConvertDecimalToOctal(int value)
        {
            int multiplier = 1;
            int accum = value;
            int actual = 0;
            while (accum != 0)
            {
                actual += (accum % 8) * multiplier;
                accum /= 8;
                multiplier *= 10;
            }
            return actual;
        }

        // Returns the specified 10-base number as an 8-base number.
        internal static long ConvertDecimalToOctal(long value)
        {
            long multiplier = 1;
            long accum = value;
            long actual = 0;
            while (accum != 0)
            {
                actual += (accum % 8) * multiplier;
                accum /= 8;
                multiplier *= 10;
            }
            return actual;
        }

        // Returns true if all the bytes in the specified array are nulls, false otherwise.
        internal static bool IsAllNullBytes(Span<byte> buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] != 0)
                {
                    return false;
                }
            }
            return true;
        }

        // Returns a DateTimeOffset instance representing the number of seconds that have passed since the Unix Epoch.
        internal static DateTimeOffset GetDateTimeFromSecondsSinceEpoch(double secondsSinceUnixEpoch)
        {
            DateTimeOffset offset = new DateTimeOffset((long)(secondsSinceUnixEpoch * TimeSpan.TicksPerSecond) + DateTime.UnixEpoch.Ticks, TimeSpan.Zero);
            return offset;
        }

        // Receives a byte array that represents an ASCII string containing a number in octal base.
        // Converts the array to an octal base number, then transforms it to ten base and returns it.
        internal static int GetTenBaseNumberFromOctalAsciiChars(Span<byte> buffer)
        {
            string str = GetTrimmedAsciiString(buffer);
            return string.IsNullOrEmpty(str) ? 0 : Convert.ToInt32(str, fromBase: 8);
        }

        // Returns the string contained in the specified buffer of bytes,
        // in the specified encoding, removing the trailing null or space chars.
        private static string GetTrimmedString(ReadOnlySpan<byte> buffer, Encoding encoding)
        {
            int trimmedLength = buffer.Length;
            while (trimmedLength > 0 && IsByteNullOrSpace(buffer[trimmedLength - 1]))
            {
                trimmedLength--;
            }

            return trimmedLength == 0 ? string.Empty : encoding.GetString(buffer.Slice(0, trimmedLength));

            static bool IsByteNullOrSpace(byte c) => c is 0 or 32;
        }

        // Returns the ASCII string contained in the specified buffer of bytes,
        // removing the trailing null or space chars.
        internal static string GetTrimmedAsciiString(ReadOnlySpan<byte> buffer) => GetTrimmedString(buffer, Encoding.ASCII);

        // Returns the UTF8 string contained in the specified buffer of bytes,
        // removing the trailing null or space chars.
        internal static string GetTrimmedUtf8String(ReadOnlySpan<byte> buffer) => GetTrimmedString(buffer, Encoding.UTF8);

        // Returns true if it successfully converts the specified string to a DateTimeOffset, false otherwise.
        internal static bool TryConvertToDateTimeOffset(string value, out DateTimeOffset timestamp)
        {
            timestamp = default;
            if (!string.IsNullOrEmpty(value))
            {
                if (!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleTime))
                {
                    return false;
                }

                timestamp = GetDateTimeFromSecondsSinceEpoch(doubleTime);
            }
            return timestamp != default;
        }

        // After the file contents, there may be zero or more null characters,
        // which exist to ensure the data is aligned to the record size. Skip them and
        // set the stream position to the first byte of the next entry.
        internal static int SkipBlockAlignmentPadding(Stream archiveStream, long size)
        {
            int bytesToSkip = CalculatePadding(size);
            AdvanceStream(archiveStream, bytesToSkip);
            return bytesToSkip;
        }

        // Throws if the specified entry type is not supported for the specified format.
        // If 'forWriting' is true, an incompatible 'Regular File' entry type is allowed. It will be converted to the compatible version before writing.
        internal static void VerifyEntryTypeIsSupported(TarEntryType entryType, TarEntryFormat archiveFormat, bool forWriting)
        {
            switch (archiveFormat)
            {
                case TarEntryFormat.V7:
                    if (entryType is
                        TarEntryType.Directory or
                        TarEntryType.HardLink or
                        TarEntryType.V7RegularFile or
                        TarEntryType.SymbolicLink)
                    {
                        return;
                    }
                    if (forWriting && entryType is TarEntryType.RegularFile)
                    {
                        return;
                    }
                    break;

                case TarEntryFormat.Ustar:
                    if (entryType is
                        TarEntryType.BlockDevice or
                        TarEntryType.CharacterDevice or
                        TarEntryType.Directory or
                        TarEntryType.Fifo or
                        TarEntryType.HardLink or
                        TarEntryType.RegularFile or
                        TarEntryType.SymbolicLink)
                    {
                        return;
                    }
                    if (forWriting && entryType is TarEntryType.V7RegularFile)
                    {
                        return;
                    }
                    break;

                case TarEntryFormat.Pax:
                    if (entryType is
                        TarEntryType.BlockDevice or
                        TarEntryType.CharacterDevice or
                        TarEntryType.Directory or
                        TarEntryType.Fifo or
                        TarEntryType.HardLink or
                        TarEntryType.RegularFile or
                        TarEntryType.SymbolicLink)
                    {
                        // Not supported for writing - internally autogenerated:
                        // - ExtendedAttributes
                        // - GlobalExtendedAttributes
                        return;
                    }
                    if (forWriting && entryType is TarEntryType.V7RegularFile)
                    {
                        return;
                    }
                    break;

                case TarEntryFormat.Gnu:
                    if (entryType is
                        TarEntryType.BlockDevice or
                        TarEntryType.CharacterDevice or
                        TarEntryType.Directory or
                        TarEntryType.Fifo or
                        TarEntryType.HardLink or
                        TarEntryType.RegularFile or
                        TarEntryType.SymbolicLink)
                    {
                        // Not supported for writing:
                        // - ContiguousFile
                        // - DirectoryList
                        // - MultiVolume
                        // - RenamedOrSymlinked
                        // - SparseFile
                        // - TapeVolume

                        // Also not supported for writing - internally autogenerated:
                        // - LongLink
                        // - LongPath
                        return;
                    }
                    if (forWriting && entryType is TarEntryType.V7RegularFile)
                    {
                        return;
                    }
                    break;

                case TarEntryFormat.Unknown:
                default:
                    throw new FormatException(string.Format(SR.TarInvalidFormat, archiveFormat));
            }

            throw new InvalidOperationException(string.Format(SR.TarEntryTypeNotSupported, entryType, archiveFormat));
        }
    }
}
