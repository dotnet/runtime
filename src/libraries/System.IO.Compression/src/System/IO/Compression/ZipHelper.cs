// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Text;

namespace System.IO.Compression
{
    internal static class ZipHelper
    {
        internal const uint Mask32Bit = 0xFFFFFFFF;
        internal const ushort Mask16Bit = 0xFFFF;

        private const int BackwardsSeekingBufferSize = 4096;

        internal const int ValidZipDate_YearMin = 1980;
        internal const int ValidZipDate_YearMax = 2107;

        private static readonly DateTime s_invalidDateIndicator = new DateTime(ValidZipDate_YearMin, 1, 1, 0, 0, 0);

        internal static Encoding GetEncoding(string text)
        {
            if (text.AsSpan().ContainsAnyExceptInRange((char)32, (char)126))
            {
                // The Zip Format uses code page 437 when the Unicode bit is not set. This format
                // is the same as ASCII for characters 32-126 but differs otherwise. If we can fit
                // the string into CP437 then we treat ASCII as acceptable.
                return Encoding.UTF8;
            }

            return Encoding.ASCII;
        }

        /// <summary>
        /// Reads exactly bytesToRead out of stream, unless it is out of bytes
        /// </summary>
        internal static void ReadBytes(Stream stream, Span<byte> buffer, int bytesToRead)
        {
            int bytesRead = stream.ReadAtLeast(buffer, bytesToRead, throwOnEndOfStream: false);
            if (bytesRead < bytesToRead)
            {
                throw new IOException(SR.UnexpectedEndOfStream);
            }
        }

        // will silently return InvalidDateIndicator if the uint is not a valid Dos DateTime
        internal static DateTime DosTimeToDateTime(uint dateTime)
        {
            if (dateTime == 0)
            {
                return s_invalidDateIndicator;
            }

            // DosTime format 32 bits
            // Year: 7 bits, 0 is ValidZipDate_YearMin, unsigned (ValidZipDate_YearMin = 1980)
            // Month: 4 bits
            // Day: 5 bits
            // Hour: 5
            // Minute: 6 bits
            // Second: 5 bits

            // do the bit shift as unsigned because the fields are unsigned, but
            // we can safely convert to int, because they won't be too big
            int year = (int)(ValidZipDate_YearMin + (dateTime >> 25));
            int month = (int)((dateTime >> 21) & 0xF);
            int day = (int)((dateTime >> 16) & 0x1F);
            int hour = (int)((dateTime >> 11) & 0x1F);
            int minute = (int)((dateTime >> 5) & 0x3F);
            int second = (int)((dateTime & 0x001F) * 2); // only 5 bits for second, so we only have a granularity of 2 sec.

            try
            {
                return new DateTime(year, month, day, hour, minute, second, 0);
            }
            catch (ArgumentOutOfRangeException)
            {
                return s_invalidDateIndicator;
            }
            catch (ArgumentException)
            {
                return s_invalidDateIndicator;
            }
        }

        // assume date time has passed IsConvertibleToDosTime
        internal static uint DateTimeToDosTime(DateTime dateTime)
        {
            // DateTime must be Convertible to DosTime:
            Debug.Assert(ValidZipDate_YearMin <= dateTime.Year && dateTime.Year <= ValidZipDate_YearMax);

            int ret = ((dateTime.Year - ValidZipDate_YearMin) & 0x7F);
            ret = (ret << 4) + dateTime.Month;
            ret = (ret << 5) + dateTime.Day;
            ret = (ret << 5) + dateTime.Hour;
            ret = (ret << 6) + dateTime.Minute;
            ret = (ret << 5) + (dateTime.Second / 2); // only 5 bits for second, so we only have a granularity of 2 sec.
            return (uint)ret;
        }

        // Assumes all bytes of signatureToFind are non zero, looks backwards from current position in stream,
        // assumes maxBytesToRead is positive, ensures to not read beyond the provided max number of bytes,
        // if the signature is found then returns true and positions stream at first byte of signature
        // if the signature is not found, returns false
        internal static bool SeekBackwardsToSignature(Stream stream, ReadOnlySpan<byte> signatureToFind, int maxBytesToRead)
        {
            Debug.Assert(signatureToFind.Length != 0);
            Debug.Assert(maxBytesToRead > 0);

            int bufferPointer = 0;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(BackwardsSeekingBufferSize);
            Span<byte> bufferSpan = buffer.AsSpan(0, BackwardsSeekingBufferSize);

            try
            {
                bool outOfBytes = false;
                bool signatureFound = false;

                int bytesRead = 0;
                int duplicateBytesRead = 0;

                while (!signatureFound && !outOfBytes && bytesRead <= maxBytesToRead)
                {
                    outOfBytes = SeekBackwardsAndRead(stream, bufferSpan, signatureToFind.Length);

                    Debug.Assert(bufferPointer < bufferSpan.Length);

                    bufferPointer = bufferSpan.LastIndexOf(signatureToFind);
                    bytesRead += (bufferSpan.Length - duplicateBytesRead);

                    if (bufferPointer != -1)
                    {
                        signatureFound = true;
                        break;
                    }

                    duplicateBytesRead = signatureToFind.Length;
                }

                if (!signatureFound)
                {
                    return false;
                }
                else
                {
                    stream.Seek(bufferPointer, SeekOrigin.Current);
                    return true;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        // Returns true if we are out of bytes
        // Allows successive buffers to overlap by a number of bytes (to handle cases where the
        // value being searched for straddles buffers.)
        private static bool SeekBackwardsAndRead(Stream stream, Span<byte> buffer, int overlap)
        {
            if (stream.Position >= buffer.Length)
            {
                stream.Seek(-(buffer.Length - overlap), SeekOrigin.Current);
                ReadBytes(stream, buffer, buffer.Length);
                stream.Seek(-buffer.Length, SeekOrigin.Current);
                return false;
            }
            else
            {
                int bytesToRead = (int)stream.Position;
                stream.Seek(0, SeekOrigin.Begin);
                ReadBytes(stream, buffer, bytesToRead);
                stream.Seek(0, SeekOrigin.Begin);
                return true;
            }
        }

        // Converts the specified string into bytes using the optional specified encoding.
        // If the encoding null, then the encoding is calculated from the string itself.
        // If maxBytes is greater than zero, the returned string will be truncated to a total
        // number of characters whose bytes do not add up to more than that number.
        internal static byte[] GetEncodedTruncatedBytesFromString(string? text, Encoding? encoding, int maxBytes, out bool isUTF8)
        {
            if (string.IsNullOrEmpty(text))
            {
                isUTF8 = false;
                return Array.Empty<byte>();
            }

            encoding ??= GetEncoding(text);
            isUTF8 = encoding.CodePage == 65001;

            if (maxBytes == 0) // No truncation
            {
                return encoding.GetBytes(text);
            }

            byte[] bytes;
            if (isUTF8 && encoding.GetMaxByteCount(text.Length) > maxBytes)
            {
                int totalCodePoints = 0;
                foreach (Rune rune in text.EnumerateRunes())
                {
                    if (totalCodePoints + rune.Utf8SequenceLength > maxBytes)
                    {
                        break;
                    }
                    totalCodePoints += rune.Utf8SequenceLength;
                }

                bytes = encoding.GetBytes(text);

                Debug.Assert(totalCodePoints > 0);
                Debug.Assert(totalCodePoints <= bytes.Length);

                return bytes[0..totalCodePoints];
            }

            bytes = encoding.GetBytes(text);
            return maxBytes < bytes.Length ? bytes[0..maxBytes] : bytes;
        }
    }
}
