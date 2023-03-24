// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Formats.Tar
{
    // Static class containing a variety of helper methods.
    internal static partial class TarHelpers
    {
        internal const short RecordSize = 512;
        internal const int MaxBufferLength = 4096;
        internal const long MaxSizeLength = (1L << 33) - 1; // Max value of 11 octal digits = 2^33 - 1 or 8 Gb.

        internal const UnixFileMode ValidUnixFileModes =
            UnixFileMode.UserRead |
            UnixFileMode.UserWrite |
            UnixFileMode.UserExecute |
            UnixFileMode.GroupRead |
            UnixFileMode.GroupWrite |
            UnixFileMode.GroupExecute |
            UnixFileMode.OtherRead |
            UnixFileMode.OtherWrite |
            UnixFileMode.OtherExecute |
            UnixFileMode.StickyBit |
            UnixFileMode.SetGroup |
            UnixFileMode.SetUser;

        // Default mode for TarEntry created for a file-type.
        private const UnixFileMode DefaultFileMode =
            UnixFileMode.UserRead | UnixFileMode.UserWrite |
            UnixFileMode.GroupRead |
            UnixFileMode.OtherRead;

        // Default mode for TarEntry created for a directory-type.
        private const UnixFileMode DefaultDirectoryMode =
            DefaultFileMode |
            UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;

        internal static int GetDefaultMode(TarEntryType type)
            => type is TarEntryType.Directory or TarEntryType.DirectoryList ? (int)DefaultDirectoryMode : (int)DefaultFileMode;

        // Helps advance the stream a total number of bytes larger than int.MaxValue.
        internal static void AdvanceStream(Stream archiveStream, long bytesToDiscard)
        {
            if (archiveStream.CanSeek)
            {
                archiveStream.Position += bytesToDiscard;
            }
            else if (bytesToDiscard > 0)
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(minimumLength: (int)Math.Min(MaxBufferLength, bytesToDiscard));
                while (bytesToDiscard > 0)
                {
                    int currentLengthToRead = (int)Math.Min(MaxBufferLength, bytesToDiscard);
                    archiveStream.ReadExactly(buffer.AsSpan(0, currentLengthToRead));
                    bytesToDiscard -= currentLengthToRead;
                }
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        // Asynchronously helps advance the stream a total number of bytes larger than int.MaxValue.
        internal static async ValueTask AdvanceStreamAsync(Stream archiveStream, long bytesToDiscard, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (archiveStream.CanSeek)
            {
                archiveStream.Position += bytesToDiscard;
            }
            else if (bytesToDiscard > 0)
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(minimumLength: (int)Math.Min(MaxBufferLength, bytesToDiscard));
                while (bytesToDiscard > 0)
                {
                    int currentLengthToRead = (int)Math.Min(MaxBufferLength, bytesToDiscard);
                    await archiveStream.ReadExactlyAsync(buffer, 0, currentLengthToRead, cancellationToken).ConfigureAwait(false);
                    bytesToDiscard -= currentLengthToRead;
                }
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        // Helps copy a specific number of bytes from one stream into another.
        internal static void CopyBytes(Stream origin, Stream destination, long bytesToCopy)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(minimumLength: (int)Math.Min(MaxBufferLength, bytesToCopy));
            while (bytesToCopy > 0)
            {
                int currentLengthToRead = (int)Math.Min(MaxBufferLength, bytesToCopy);
                origin.ReadExactly(buffer.AsSpan(0, currentLengthToRead));
                destination.Write(buffer.AsSpan(0, currentLengthToRead));
                bytesToCopy -= currentLengthToRead;
            }
            ArrayPool<byte>.Shared.Return(buffer);
        }

        // Asynchronously helps copy a specific number of bytes from one stream into another.
        internal static async ValueTask CopyBytesAsync(Stream origin, Stream destination, long bytesToCopy, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            byte[] buffer = ArrayPool<byte>.Shared.Rent(minimumLength: (int)Math.Min(MaxBufferLength, bytesToCopy));
            while (bytesToCopy > 0)
            {
                int currentLengthToRead = (int)Math.Min(MaxBufferLength, bytesToCopy);
                Memory<byte> memory = buffer.AsMemory(0, currentLengthToRead);
                await origin.ReadExactlyAsync(buffer, 0, currentLengthToRead, cancellationToken).ConfigureAwait(false);
                await destination.WriteAsync(memory, cancellationToken).ConfigureAwait(false);
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

        // Returns true if all the bytes in the specified array are nulls, false otherwise.
        internal static bool IsAllNullBytes(Span<byte> buffer) =>
            buffer.IndexOfAnyExcept((byte)0) < 0;

        // Converts the specified number of seconds that have passed since the Unix Epoch to a DateTimeOffset.
        internal static DateTimeOffset GetDateTimeOffsetFromSecondsSinceEpoch(long secondsSinceUnixEpoch) =>
            new DateTimeOffset((secondsSinceUnixEpoch * TimeSpan.TicksPerSecond) + DateTime.UnixEpoch.Ticks, TimeSpan.Zero);

        // Converts the specified number of seconds that have passed since the Unix Epoch to a DateTimeOffset.
        private static DateTimeOffset GetDateTimeOffsetFromSecondsSinceEpoch(decimal secondsSinceUnixEpoch) =>
            new DateTimeOffset((long)(secondsSinceUnixEpoch * TimeSpan.TicksPerSecond) + DateTime.UnixEpoch.Ticks, TimeSpan.Zero);

        // Converts the specified DateTimeOffset to the number of seconds that have passed since the Unix Epoch.
        private static decimal GetSecondsSinceEpochFromDateTimeOffset(DateTimeOffset dateTimeOffset) =>
            ((decimal)(dateTimeOffset.UtcDateTime - DateTime.UnixEpoch).Ticks) / TimeSpan.TicksPerSecond;

        // If the specified fieldName is found in the provided dictionary and it is a valid decimal number, returns true and sets the value in 'dateTimeOffset'.
        internal static bool TryGetDateTimeOffsetFromTimestampString(Dictionary<string, string>? dict, string fieldName, out DateTimeOffset dateTimeOffset)
        {
            dateTimeOffset = default;
            if (dict != null &&
                dict.TryGetValue(fieldName, out string? value) &&
                decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal secondsSinceEpoch))
            {
                dateTimeOffset = GetDateTimeOffsetFromSecondsSinceEpoch(secondsSinceEpoch);
                return true;
            }
            return false;
        }

        // Converts the specified DateTimeOffset to the string representation of seconds since the Unix Epoch.
        internal static string GetTimestampStringFromDateTimeOffset(DateTimeOffset timestamp)
        {
            decimal secondsSinceEpoch = GetSecondsSinceEpochFromDateTimeOffset(timestamp);

            // Use 'G' to ensure the decimals get preserved (avoid losing precision).
            return secondsSinceEpoch.ToString("G", CultureInfo.InvariantCulture);
        }

        // If the specified fieldName is found in the provided dictionary and is a valid string representation of a number, returns true and sets the value in 'baseTenInteger'.
        internal static bool TryGetStringAsBaseTenInteger(IReadOnlyDictionary<string, string> dict, string fieldName, out int baseTenInteger)
        {
            if (dict.TryGetValue(fieldName, out string? strNumber) && !string.IsNullOrEmpty(strNumber))
            {
                baseTenInteger = int.Parse(strNumber, CultureInfo.InvariantCulture);
                return true;
            }

            baseTenInteger = 0;
            return false;
        }

        // If the specified fieldName is found in the provided dictionary and is a valid string representation of a number, returns true and sets the value in 'baseTenLong'.
        internal static bool TryGetStringAsBaseTenLong(IReadOnlyDictionary<string, string> dict, string fieldName, out long baseTenLong)
        {
            if (dict.TryGetValue(fieldName, out string? strNumber) && !string.IsNullOrEmpty(strNumber))
            {
                baseTenLong = long.Parse(strNumber, CultureInfo.InvariantCulture);
                return true;
            }

            baseTenLong = 0;
            return false;
        }

        // When writing an entry that came from an archive of a different format, if its entry type happens to
        // be an incompatible regular file entry type, convert it to the compatible one.
        // No change for all other entry types.
        internal static TarEntryType GetCorrectTypeFlagForFormat(TarEntryFormat format, TarEntryType entryType)
        {
            if (format is TarEntryFormat.V7)
            {
                if (entryType is TarEntryType.RegularFile)
                {
                    return TarEntryType.V7RegularFile;
                }
            }
            else if (entryType is TarEntryType.V7RegularFile)
            {
                return TarEntryType.RegularFile;
            }

            return entryType;
        }

        /// <summary>Parses a byte span that represents an ASCII string containing a number in octal base.</summary>
        internal static T ParseOctal<T>(ReadOnlySpan<byte> buffer) where T : struct, INumber<T>
        {
            buffer = TrimEndingNullsAndSpaces(buffer);
            buffer = TrimLeadingNullsAndSpaces(buffer);

            if (buffer.Length == 0)
            {
                return T.Zero;
            }

            T octalFactor = T.CreateTruncating(8u);
            T value = T.Zero;
            foreach (byte b in buffer)
            {
                uint digit = (uint)(b - '0');
                if (digit >= 8)
                {
                    ThrowInvalidNumber();
                }

                value = checked((value * octalFactor) + T.CreateTruncating(digit));
            }

            return value;
        }

        [DoesNotReturn]
        private static void ThrowInvalidNumber() =>
            throw new InvalidDataException(SR.Format(SR.TarInvalidNumber));

        // Returns the string contained in the specified buffer of bytes,
        // in the specified encoding, removing the trailing null or space chars.
        private static string GetTrimmedString(ReadOnlySpan<byte> buffer, Encoding encoding)
        {
            buffer = TrimEndingNullsAndSpaces(buffer);
            return buffer.IsEmpty ? string.Empty : encoding.GetString(buffer);
        }

        internal static ReadOnlySpan<byte> TrimEndingNullsAndSpaces(ReadOnlySpan<byte> buffer)
        {
            int trimmedLength = buffer.Length;
            while (trimmedLength > 0 && buffer[trimmedLength - 1] is 0 or 32)
            {
                trimmedLength--;
            }

            return buffer.Slice(0, trimmedLength);
        }

        private static ReadOnlySpan<byte> TrimLeadingNullsAndSpaces(ReadOnlySpan<byte> buffer)
        {
            int newStart = 0;
            while (newStart < buffer.Length && buffer[newStart] is 0 or 32)
            {
                newStart++;
            }

            return buffer.Slice(newStart);
        }

        // Returns the ASCII string contained in the specified buffer of bytes,
        // removing the trailing null or space chars.
        internal static string GetTrimmedAsciiString(ReadOnlySpan<byte> buffer) => GetTrimmedString(buffer, Encoding.ASCII);

        // Returns the UTF8 string contained in the specified buffer of bytes,
        // removing the trailing null or space chars.
        internal static string GetTrimmedUtf8String(ReadOnlySpan<byte> buffer) => GetTrimmedString(buffer, Encoding.UTF8);

        // After the file contents, there may be zero or more null characters,
        // which exist to ensure the data is aligned to the record size. Skip them and
        // set the stream position to the first byte of the next entry.
        internal static int SkipBlockAlignmentPadding(Stream archiveStream, long size)
        {
            int bytesToSkip = CalculatePadding(size);
            AdvanceStream(archiveStream, bytesToSkip);
            return bytesToSkip;
        }

        // After the file contents, there may be zero or more null characters,
        // which exist to ensure the data is aligned to the record size.
        // Asynchronously skip them and set the stream position to the first byte of the next entry.
        internal static async ValueTask<int> SkipBlockAlignmentPaddingAsync(Stream archiveStream, long size, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int bytesToSkip = CalculatePadding(size);
            await AdvanceStreamAsync(archiveStream, bytesToSkip, cancellationToken).ConfigureAwait(false);
            return bytesToSkip;
        }

        // Throws if the specified entry type is not supported for the specified format.
        internal static void ThrowIfEntryTypeNotSupported(TarEntryType entryType, TarEntryFormat archiveFormat, [CallerArgumentExpression(nameof(entryType))] string? paramName = null)
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
                        // GlobalExtendedAttributes is handled via PaxGlobalExtendedAttributesEntry

                        // Not supported for writing - internally autogenerated:
                        // - ExtendedAttributes
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
                    break;

                case TarEntryFormat.Unknown:
                default:
                    throw new InvalidDataException(SR.Format(SR.TarInvalidFormat, archiveFormat));
            }

            throw new ArgumentException(SR.Format(SR.TarEntryTypeNotSupportedInFormat, entryType, archiveFormat), paramName);
        }
    }
}
