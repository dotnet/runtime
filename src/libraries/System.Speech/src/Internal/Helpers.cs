// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Globalization;

namespace System.Speech.Internal
{
    internal static class Helpers
    {
        //*******************************************************************
        //
        // Internal Methods
        //
        //*******************************************************************

        #region Internal Methods

        // Disable parameter validation check
#pragma warning disable 56507

        // Throws exception if the specified Rule does not have a valid Id.
        static internal void ThrowIfEmptyOrNull(string s, string paramName)
        {
            if (string.IsNullOrEmpty(s))
            {
                if (s == null)
                {
                    throw new ArgumentNullException(paramName);
                }
                else
                {
                    throw new ArgumentException(SR.Get(SRID.StringCanNotBeEmpty, paramName), paramName);
                }
            }
        }

#pragma warning restore 56507

        // Throws exception if the specified Rule does not have a valid Id.
        static internal void ThrowIfNull(object value, string paramName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        static internal bool CompareInvariantCulture(CultureInfo culture1, CultureInfo culture2)
        {
            // If perfect match easy
            if (culture1.Equals(culture2))
            {
                return true;
            }

            // Compare the Neutral culture
            while (!culture1.IsNeutralCulture)
            {
                culture1 = culture1.Parent;
            }
            while (!culture2.IsNeutralCulture)
            {
                culture2 = culture2.Parent;
            }
            return culture1.Equals(culture2);
        }

        // Copy the input cfg to the output.
        // Streams point to the start of the data on entry and to the end on exit
        static internal void CopyStream(Stream inputStream, Stream outputStream, int bytesToCopy)
        {
            // Copy using an intermediate buffer of a reasonable size.
            int bufferSize = bytesToCopy > 4096 ? 4096 : bytesToCopy;
            byte[] buffer = new byte[bufferSize];
            int bytesRead;
            while (bytesToCopy > 0)
            {
                bytesRead = inputStream.Read(buffer, 0, bufferSize);
                if (bytesRead <= 0)
                {
                    throw new EndOfStreamException(SR.Get(SRID.StreamEndedUnexpectedly));
                }
                outputStream.Write(buffer, 0, bytesRead);
                bytesToCopy -= bytesRead;
            }
        }

        // Copy the input cfg to the output.
        // inputStream points to the start of the data on entry and to the end on exit
        static internal byte[] ReadStreamToByteArray(Stream inputStream, int bytesToCopy)
        {
            byte[] outputArray = new byte[bytesToCopy];
            BlockingRead(inputStream, outputArray, 0, bytesToCopy);
            return outputArray;
        }

        static internal void BlockingRead(Stream stream, byte[] buffer, int offset, int count)
        {
            // Stream is not like IStream - it will block until some data is available but not necessarily all of it.
            while (count > 0)
            {
                int read = stream.Read(buffer, offset, count);
                if (read <= 0) // End of stream
                {
                    throw new EndOfStreamException();
                }
                count -= read;
                offset += read;
            }
        }


        #endregion

        //*******************************************************************
        //
        // Internal fields
        //
        //*******************************************************************

        #region Internal fields

        internal static readonly char[] _achTrimChars = new char[] { ' ', '\t', '\n', '\r' };

        // Size of a char (avoid to use the marshal class
        internal const int _sizeOfChar = 2;

        #endregion
    }
}
