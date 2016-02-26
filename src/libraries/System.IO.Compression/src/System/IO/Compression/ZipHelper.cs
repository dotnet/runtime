// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace System.IO.Compression
{
    internal static class ZipHelper
    {
        internal const UInt32 Mask32Bit = 0xFFFFFFFF;
        internal const UInt16 Mask16Bit = 0xFFFF;

        private const Int32 BackwardsSeekingBufferSize = 32;

        internal const Int32 ValidZipDate_YearMin = 1980;
        internal const Int32 ValidZipDate_YearMax = 2107;

        private static readonly DateTime s_invalidDateIndicator = new DateTime(ValidZipDate_YearMin, 1, 1, 0, 0, 0);

        internal static Boolean RequiresUnicode(String test)
        {
            Debug.Assert(test != null);

            foreach (Char c in test)
            {
                if (c > 127) return true;
            }

            return false;
        }

        //reads exactly bytesToRead out of stream, unless it is out of bytes
        internal static void ReadBytes(Stream stream, Byte[] buffer, Int32 bytesToRead)
        {
            Int32 bytesLeftToRead = bytesToRead;

            Int32 totalBytesRead = 0;

            while (bytesLeftToRead > 0)
            {
                Int32 bytesRead = stream.Read(buffer, totalBytesRead, bytesLeftToRead);
                if (bytesRead == 0) throw new IOException(SR.UnexpectedEndOfStream);

                totalBytesRead += bytesRead;
                bytesLeftToRead -= bytesRead;
            }
        }

        /*DosTime format 32 bits
        //Year: 7 bits, 0 is ValidZipDate_YearMin, unsigned (ValidZipDate_YearMin = 1980)
        //Month: 4 bits
        //Day: 5 bits
        //Hour: 5
        //Minute: 6 bits
        //Second: 5 bits
        */

        //will silently return InvalidDateIndicator if the UInt32 is not a valid Dos DateTime
        internal static DateTime DosTimeToDateTime(UInt32 dateTime)
        {
            // do the bit shift as unsigned because the fields are unsigned, but
            // we can safely convert to Int32, because they won't be too big
            Int32 year = (Int32)(ValidZipDate_YearMin + (dateTime >> 25));
            Int32 month = (Int32)((dateTime >> 21) & 0xF);
            Int32 day = (Int32)((dateTime >> 16) & 0x1F);
            Int32 hour = (Int32)((dateTime >> 11) & 0x1F);
            Int32 minute = (Int32)((dateTime >> 5) & 0x3F);
            Int32 second = (Int32)((dateTime & 0x001F) * 2);       // only 5 bits for second, so we only have a granularity of 2 sec. 

            try
            {
                return new System.DateTime(year, month, day, hour, minute, second, 0);
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


        //assume date time has passed IsConvertibleToDosTime
        internal static UInt32 DateTimeToDosTime(DateTime dateTime)
        {
            // DateTime must be Convertible to DosTime:
            Contract.Requires(ValidZipDate_YearMin <= dateTime.Year && dateTime.Year <= ValidZipDate_YearMax);

            int ret = ((dateTime.Year - ValidZipDate_YearMin) & 0x7F);
            ret = (ret << 4) + dateTime.Month;
            ret = (ret << 5) + dateTime.Day;
            ret = (ret << 5) + dateTime.Hour;
            ret = (ret << 6) + dateTime.Minute;
            ret = (ret << 5) + (dateTime.Second / 2);   // only 5 bits for second, so we only have a granularity of 2 sec. 
            return (uint)ret;
        }


        //assumes all bytes of signatureToFind are non zero, looks backwards from current position in stream,
        //if the signature is found then returns true and positions stream at first byte of signature
        //if the signature is not found, returns false
        internal static Boolean SeekBackwardsToSignature(Stream stream, UInt32 signatureToFind)
        {
            Int32 bufferPointer = 0;
            UInt32 currentSignature = 0;
            byte[] buffer = new byte[BackwardsSeekingBufferSize];

            Boolean outOfBytes = false;
            Boolean signatureFound = false;

            while (!signatureFound && !outOfBytes)
            {
                outOfBytes = SeekBackwardsAndRead(stream, buffer, out bufferPointer);

                Debug.Assert(bufferPointer < buffer.Length);

                while (bufferPointer >= 0 && !signatureFound)
                {
                    currentSignature = (currentSignature << 8) | ((UInt32)buffer[bufferPointer]);
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

        //Skip to a further position downstream (without relying on the stream being seekable)
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

        //Returns true if we are out of bytes
        private static Boolean SeekBackwardsAndRead(Stream stream, Byte[] buffer, out Int32 bufferPointer)
        {
            if (stream.Position >= buffer.Length)
            {
                stream.Seek(-buffer.Length, SeekOrigin.Current);
                ZipHelper.ReadBytes(stream, buffer, buffer.Length);
                stream.Seek(-buffer.Length, SeekOrigin.Current);
                bufferPointer = buffer.Length - 1;
                return false;
            }
            else
            {
                Int32 bytesToRead = (Int32)stream.Position;
                stream.Seek(0, SeekOrigin.Begin);
                ZipHelper.ReadBytes(stream, buffer, bytesToRead);
                stream.Seek(0, SeekOrigin.Begin);
                bufferPointer = bytesToRead - 1;
                return true;
            }
        }
    }
}
