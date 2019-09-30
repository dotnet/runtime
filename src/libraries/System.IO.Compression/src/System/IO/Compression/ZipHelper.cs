// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.IO.Compression
{
    internal static class ZipHelper
    {
        internal const uint Mask32Bit = 0xFFFFFFFF;
        internal const ushort Mask16Bit = 0xFFFF;

        private const int BackwardsSeekingBufferSize = 32;

        internal const int ValidZipDate_YearMin = 1980;
        internal const int ValidZipDate_YearMax = 2107;

        private static readonly DateTime s_invalidDateIndicator = new DateTime(ValidZipDate_YearMin, 1, 1, 0, 0, 0);

        internal static bool RequiresUnicode(string test)
        {
            Debug.Assert(test != null);

            foreach (char c in test)
            {
                // The Zip Format uses code page 437 when the Unicode bit is not set. This format
                // is the same as ASCII for characters 32-126 but differs otherwise. If we can fit
                // the string into CP437 then we treat ASCII as acceptable.
                if (c > 126 || c < 32)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Reads exactly bytesToRead out of stream, unless it is out of bytes
        /// </summary>
        internal static void ReadBytes(Stream stream, byte[] buffer, int bytesToRead)
        {
            int bytesLeftToRead = bytesToRead;

            int totalBytesRead = 0;

            while (bytesLeftToRead > 0)
            {
                int bytesRead = stream.Read(buffer, totalBytesRead, bytesLeftToRead);
                if (bytesRead == 0) throw new IOException(SR.UnexpectedEndOfStream);

                totalBytesRead += bytesRead;
                bytesLeftToRead -= bytesRead;
            }
        }

        // will silently return InvalidDateIndicator if the uint is not a valid Dos DateTime
        internal static DateTime DosTimeToDateTime(uint dateTime)
        {
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

        // assumes all bytes of signatureToFind are non zero, looks backwards from current position in stream,
        // if the signature is found then returns true and positions stream at first byte of signature
        // if the signature is not found, returns false
        internal static bool SeekBackwardsToSignature(Stream stream, uint signatureToFind)
        {
            int bufferPointer = 0;
            uint currentSignature = 0;
            byte[] buffer = new byte[BackwardsSeekingBufferSize];

            bool outOfBytes = false;
            bool signatureFound = false;

            while (!signatureFound && !outOfBytes)
            {
                outOfBytes = SeekBackwardsAndRead(stream, buffer, out bufferPointer);

                Debug.Assert(bufferPointer < buffer.Length);

                while (bufferPointer >= 0 && !signatureFound)
                {
                    currentSignature = (currentSignature << 8) | ((uint)buffer[bufferPointer]);
                    if (currentSignature == signatureToFind)
                    {
                        signatureFound = true;
                    }
                    else
                    {
                        bufferPointer--;
                    }
                }
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

        // Skip to a further position downstream (without relying on the stream being seekable)
        internal static void AdvanceToPosition(this Stream stream, long position)
        {
            long numBytesLeft = position - stream.Position;
            Debug.Assert(numBytesLeft >= 0);
            while (numBytesLeft != 0)
            {
                const int throwAwayBufferSize = 64;
                int numBytesToSkip = (numBytesLeft > throwAwayBufferSize) ? throwAwayBufferSize : (int)numBytesLeft;
                int numBytesActuallySkipped = stream.Read(new byte[throwAwayBufferSize], 0, numBytesToSkip);
                if (numBytesActuallySkipped == 0)
                    throw new IOException(SR.UnexpectedEndOfStream);
                numBytesLeft -= numBytesActuallySkipped;
            }
        }

        // Returns true if we are out of bytes
        private static bool SeekBackwardsAndRead(Stream stream, byte[] buffer, out int bufferPointer)
        {
            if (stream.Position >= buffer.Length)
            {
                stream.Seek(-buffer.Length, SeekOrigin.Current);
                ReadBytes(stream, buffer, buffer.Length);
                stream.Seek(-buffer.Length, SeekOrigin.Current);
                bufferPointer = buffer.Length - 1;
                return false;
            }
            else
            {
                int bytesToRead = (int)stream.Position;
                stream.Seek(0, SeekOrigin.Begin);
                ReadBytes(stream, buffer, bytesToRead);
                stream.Seek(0, SeekOrigin.Begin);
                bufferPointer = bytesToRead - 1;
                return true;
            }
        }
    }
}
