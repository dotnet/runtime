// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    /// <summary>
    /// RFC-7468 PEM (Privacy-Enhanced Mail) parsing and encoding.
    /// </summary>
    public static class PemEncoding
    {
        private const string Preeb = "-----BEGIN ";
        private const string Posteb = "-----END ";
        private const string Ending = "-----";
        private const int EncodedLineLength = 64;

        /// <summary>
        /// Finds the first PEM-encoded data.
        /// </summary>
        /// <param name="pemData">
        /// A span containing the PEM encoded data.
        /// </param>
        /// <exception cref="ArgumentException">
        /// <paramref name="pemData"/> does not contain a well-formed PEM encoded value.
        /// </exception>
        /// <returns>
        /// A <see cref="PemFields"/> structure that contains the location, label, and
        /// data location of the encoded data.
        /// </returns>
        public static PemFields Find(ReadOnlySpan<char> pemData)
        {
            if (!TryFind(pemData, out PemFields fields))
            {
                throw new ArgumentException(SR.Argument_PemEncoding_NoPemFound, nameof(pemData));
            }
            return fields;
        }

        /// <summary>
        /// Finds the first PEM-encoded data.
        /// </summary>
        /// <param name="pemData">
        /// A span containing the PEM encoded data.
        /// </param>
        /// <param name="fields">
        /// When this method returns <c>true</c>, the found <see cref="PemFields"/> structure
        /// that contains the location, label, and data location of the encoded data.
        /// </param>
        /// <returns>
        /// <c>true</c> if PEM encoded data was found; otherwise <c>false</c>.
        /// </returns>
        public static bool TryFind(ReadOnlySpan<char> pemData, out PemFields fields)
        {
            // Check for the minimum possible encoded length of a PEM structure
            // and exit early if there is no way the input could contain a well-formed
            // PEM.
            if (pemData.Length < Preeb.Length + Ending.Length * 2 + Posteb.Length)
            {
                fields = default;
                return false;
            }

            int preebLinePosition = 0;
            while (TryReadNextLine(pemData, ref preebLinePosition, out Range lineRange))
            {
                ReadOnlySpan<char> line = pemData[lineRange];
                int preebIndex = line.IndexOf(Preeb);

                if (preebIndex == -1 ||
                    (preebIndex > 0 && !line[..preebIndex].IsWhiteSpace())) // can only be preceeded by whitespace
                {
                    continue;
                }

                int preebEndingIndex = line[(preebIndex + Preeb.Length)..].IndexOf(Ending);

                if (preebEndingIndex == -1)
                {
                    continue;
                }

                (int preebOffset, _) = lineRange.GetOffsetAndLength(pemData.Length);
                int preebStartIndex = preebOffset + preebIndex;
                int startLabelIndex = preebStartIndex + Preeb.Length;
                int endLabelIndex = startLabelIndex + preebEndingIndex;
                Range labelRange = startLabelIndex..endLabelIndex;
                ReadOnlySpan<char> label = pemData[labelRange];

                if (!IsValidLabel(label))
                {
                    continue;
                }

                int postebLength = Posteb.Length + label.Length + Ending.Length;
                Span<char> postebBuffer = postebLength > 256 ? new char[postebLength] : stackalloc char[256];
                Posteb.AsSpan().CopyTo(postebBuffer);
                label.CopyTo(postebBuffer[Posteb.Length..]);
                Ending.AsSpan().CopyTo(postebBuffer[(Posteb.Length + label.Length)..]);
                ReadOnlySpan<char> posteb = postebBuffer[..postebLength];

                Range postebLineRange = lineRange;
                int postebLinePosition = preebLinePosition;

                // in lax decoding a posteb may appear on the same line as the preeb.
                // start on the current line. We do not need to check that this posteb
                // comes after the preeb because the preeb's prior content has already
                // been validated to be whitespace.
                do
                {
                    ReadOnlySpan<char> postebLine = pemData[postebLineRange];
                    int postebIndex = postebLine.IndexOf(posteb);

                    if (postebIndex == -1)
                    {
                        continue;
                    }

                    (int postebOffset, _) = postebLineRange.GetOffsetAndLength(pemData.Length);
                    int postebEndIndex = postebOffset + postebIndex + posteb.Length;
                    Range location = preebStartIndex..postebEndIndex;
                    Range content = (endLabelIndex + Ending.Length)..(postebOffset + postebIndex);

                    if (!postebLine[(postebIndex + posteb.Length)..].IsWhiteSpace())
                    {
                        break;
                    }

                    if (IsValidBase64(pemData, content, out Range base64range, out int decodedBase64Size))
                    {
                        fields = new PemFields(labelRange, base64range, location, decodedBase64Size);
                        return true;
                    }
                    break;
                }
                while (TryReadNextLine(pemData, ref postebLinePosition, out postebLineRange));
            }

            fields = default;
            return false;
        }

        private static bool IsValidLabel(ReadOnlySpan<char> data)
        {
            static bool IsLabelChar(char c) => c >= 0x21 && c <= 0x7e && c != '-';

            if (data.Length == 0)
                return true;

            // First character of label must be a labelchar
            if (!IsLabelChar(data[0]))
                return false;

            for (int index = 1; index < data.Length; index++)
            {
                char c = data[index];
                if (!IsLabelChar(c) && c != ' ' && c != '-')
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsValidBase64(
            ReadOnlySpan<char> data,
            Range content,
            out Range base64range,
            out int decodedSize)
        {
            static bool IsBase64Char(char c) =>
                (c >= '0' && c <= '9') ||
                (c >= 'A' && c <= 'Z') ||
                (c >= 'a' && c <= 'z') ||
                c == '+' || c == '/' || c == '=';

            int base64chars = 0;
            int precedingWhiteSpace = 0;
            int trailingWhiteSpace = 0;
            (int offset, int length) = content.GetOffsetAndLength(data.Length);
            for (int index = offset; index < offset + length; index++)
            {
                char c = data[index];

                if (IsBase64Char(c))
                {
                    trailingWhiteSpace = 0;
                    base64chars++;
                }
                else if (c == ' ' || c == '\n' || c == '\r' ||
                         c == '\t' || c == '\v')
                {
                    if (base64chars == 0)
                    {
                        precedingWhiteSpace++;
                    }
                    else
                    {
                        trailingWhiteSpace++;
                    }
                }
                else
                {
                    base64range = default;
                    decodedSize = 0;
                    return false;
                }
            }

            base64range = (offset + precedingWhiteSpace)..(offset + (length - trailingWhiteSpace));
            decodedSize = (base64chars * 3) / 4;
            return true;
        }

        private static bool TryReadNextLine(ReadOnlySpan<char> data, ref int position, out Range nextLineContent)
        {
            if (position < 0)
            {
                nextLineContent = default;
                return false;
            }

            int newLineIndex = data[position..].IndexOfAny('\n', '\r');

            if (newLineIndex == -1)
            {
                nextLineContent = position..;
                position = -1;
                return true;
            }
            else if (data[newLineIndex] == '\r' &&
                     newLineIndex < data.Length - 1 &&
                     data[newLineIndex + 1] == '\n')
            {
                // We landed at a Windows new line, we should consume both the \r and \n.
                nextLineContent = position..(position + newLineIndex);
                position += newLineIndex + 2;
                return true;
            }
            else
            {
                nextLineContent = position..(position + newLineIndex);
                position += newLineIndex + 1;
                return true;
            }
        }

        /// <summary>
        /// Given the length of a label and binary data, determines the
        /// length of an encoded PEM in characters.
        /// </summary>
        /// <param name="labelLength">
        /// The length of the label, in characters.
        /// </param>
        /// <param name="dataLength">
        /// The length of the data, in bytes.
        /// </param>
        /// <returns>
        /// The number of characters in the encoded PEM.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="labelLength"/> is a negative value.
        ///   <para>
        ///       -or-
        ///   </para>
        ///   <paramref name="dataLength"/> is a negative value.
        ///   <para>
        ///       -or-
        ///   </para>
        ///   <paramref name="labelLength"/> exceeds the maximum possible label length.
        ///   <para>
        ///       -or-
        ///   </para>
        ///   <paramref name="dataLength"/> exceeds the maximum possible encoded data length.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The PEM is too large to encode in a signed 32-bit integer.
        /// </exception>
        public static int GetEncodedSize(int labelLength, int dataLength)
        {
            // The largest possible label is MaxLabelSize - when included in the posteb
            // and preeb lines new lines, assuming the base64 content is empty.
            //     -----BEGIN {char * MaxLabelSize}-----\n
            //     -----END {char * MaxLabelSize}-----
            const int MaxLabelSize = 1_073_741_808;

            // The largest possible binary value to fit in a padded base64 string
            // is 1,610,612,733 bytes. RFC 7468 states:
            //   Generators MUST wrap the base64-encoded lines so that each line
            //   consists of exactly 64 characters except for the final line
            // We need to account for new line characters, every 64 characters.
            // This works out to 1,585,834,053 maximum bytes in data when wrapping
            // is accounted for assuming an empty label.
            const int MaxDataLength = 1_585_834_053;

            if (labelLength < 0)
                throw new ArgumentOutOfRangeException(nameof(labelLength), SR.ArgumentOutOfRange_NeedPositiveNumber);
            if (dataLength < 0)
                throw new ArgumentOutOfRangeException(nameof(dataLength), SR.ArgumentOutOfRange_NeedPositiveNumber);
            if (labelLength > MaxLabelSize)
                throw new ArgumentOutOfRangeException(nameof(labelLength), SR.Argument_PemEncoding_EncodedSizeTooLarge);
            if (dataLength > MaxDataLength)
                throw new ArgumentOutOfRangeException(nameof(dataLength), SR.Argument_PemEncoding_EncodedSizeTooLarge);

            int preebLength = Preeb.Length + labelLength + Ending.Length;
            int postebLength = Posteb.Length + labelLength + Ending.Length;
            int totalEncapLength = preebLength + postebLength + 1; //Add one for newline after preeb

            // dataLength is already known to not overflow here
            int encodedDataLength = ((dataLength + 2) / 3) << 2;
            int lineCount = Math.DivRem(encodedDataLength, EncodedLineLength, out int remainder);
            lineCount += ((remainder >> 31) - (-remainder >> 31)); //Increment lineCount if remainder is positive.
            int encodedDataLengthWithBreaks = encodedDataLength + lineCount;

            if (int.MaxValue - encodedDataLengthWithBreaks < totalEncapLength)
                throw new ArgumentException(SR.Argument_PemEncoding_EncodedSizeTooLarge);

            return encodedDataLengthWithBreaks + totalEncapLength;
        }

        /// <summary>
        /// Tries to write PEM encoded data to <paramref name="destination" />.
        /// </summary>
        /// <param name="label">
        /// The label to encode.
        /// </param>
        /// <param name="data">
        /// The data to encode.
        /// </param>
        /// <param name="destination">
        /// The destination to write the PEM encoded data to.
        /// </param>
        /// <param name="charsWritten">
        /// When this method returns <c>true</c>, this parameter contains the number of characters
        /// written to the buffer.
        /// </param>
        /// <returns>
        /// <c>true</c> if the <paramref name="charsWritten"/> buffer is large enough to contain
        /// the encoded PEM, otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method always wraps the base-64 encoded text to 64 characters, per the
        /// recommended wrapping of RFC-7468. Unix-style line endings are used for line breaks.
        /// </remarks>
        public static bool TryWrite(ReadOnlySpan<char> label, ReadOnlySpan<byte> data, Span<char> destination, out int charsWritten)
        {
            static void Write(ReadOnlySpan<char> str, Span<char> dest, ref int offset)
            {
                str.CopyTo(dest[offset..]);
                offset += str.Length;
            }

            static void WriteBase64(ReadOnlySpan<byte> bytes, Span<char> dest, ref int offset)
            {
                bool success = Convert.TryToBase64Chars(bytes, dest[offset..], out int base64Written);

                if (!success)
                    throw new CryptographicException();

                offset += base64Written;
            }

            const string NewLine = "\n";
            int encodedSize = GetEncodedSize(label.Length, data.Length);

            if (destination.Length < encodedSize)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = 0;
            Write(Preeb, destination, ref charsWritten);
            Write(label, destination, ref charsWritten);
            Write(Ending, destination, ref charsWritten);
            Write(NewLine, destination, ref charsWritten);

            ReadOnlySpan<byte> remainingData = data;
            while (remainingData.Length >= 48)
            {
                WriteBase64(remainingData[..48], destination, ref charsWritten);
                remainingData = remainingData[48..];
                Write(NewLine, destination, ref charsWritten);
            }

            Debug.Assert(remainingData.Length < 48);

            if (remainingData.Length > 0)
            {
                WriteBase64(remainingData, destination, ref charsWritten);
                Write(NewLine, destination, ref charsWritten);
                remainingData = default;
            }

            Write(Posteb, destination, ref charsWritten);
            Write(label, destination, ref charsWritten);
            Write(Ending, destination, ref charsWritten);

            return true;
        }

        /// <summary>
        /// Creates an encoded PEM with the given label and data.
        /// </summary>
        /// <param name="label">
        /// The label to encode.
        /// </param>
        /// <param name="data">
        /// The data to encode.
        /// </param>
        /// <returns>
        /// A character array of the encoded PEM.
        /// </returns>
        /// <remarks>
        /// This method always wraps the base-64 encoded text to 64 characters, per the
        /// recommended wrapping of RFC-7468. Unix-style line endings are used for line breaks.
        /// </remarks>
        public static char[] Write(ReadOnlySpan<char> label, ReadOnlySpan<byte> data)
        {
            int encodedSize = GetEncodedSize(label.Length, data.Length);
            char[] buffer = new char[encodedSize];

            if (!TryWrite(label, data, buffer, out int charsWritten))
            {
                throw new CryptographicException();
            }

            Debug.Assert(charsWritten == encodedSize);
            return buffer;
        }
    }
}
