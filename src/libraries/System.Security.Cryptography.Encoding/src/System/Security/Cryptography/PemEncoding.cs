// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    /// <summary>
    /// Provides methods for reading and writing the IETF RFC 7468
    /// subset of PEM (Privacy-Enhanced Mail) textual encodings.
    /// This class cannot be inherited.
    /// </summary>
    public static class PemEncoding
    {
        private const string PreEBPrefix = "-----BEGIN ";
        private const string PostEBPrefix = "-----END ";
        private const string Ending = "-----";
        private const int EncodedLineLength = 64;

        /// <summary>
        /// Finds the first PEM-encoded data.
        /// </summary>
        /// <param name="pemData">
        /// The text containing the PEM-encoded data.
        /// </param>
        /// <exception cref="ArgumentException">
        /// <paramref name="pemData"/> does not contain a well-formed PEM-encoded value.
        /// </exception>
        /// <returns>
        /// A value that specifies the location, label, and data location of
        /// the encoded data.
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
        /// Attempts to find the first PEM-encoded data.
        /// </summary>
        /// <param name="pemData">
        /// The text containing the PEM-encoded data.
        /// </param>
        /// <param name="fields">
        /// When this method returns, contains a value
        /// that specifies the location, label, and data location of the encoded data;
        /// or that specifies those locations as empty if no PEM-encoded data is found.
        /// This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        /// <c>true</c> if PEM-encoded data was found; otherwise <c>false</c>.
        /// </returns>
        public static bool TryFind(ReadOnlySpan<char> pemData, out PemFields fields)
        {
            // Check for the minimum possible encoded length of a PEM structure
            // and exit early if there is no way the input could contain a well-formed
            // PEM.
            if (pemData.Length < PreEBPrefix.Length + Ending.Length * 2 + PostEBPrefix.Length)
            {
                fields = default;
                return false;
            }

            const int postebStackBufferSize = 256;
            Span<char> postebStackBuffer = stackalloc char[postebStackBufferSize];
            int areaOffset = 0;
            int preebIndex;
            ReadOnlySpan<char> pemArea = pemData;
            while ((preebIndex = pemArea.IndexOf(PreEBPrefix)) >= 0)
            {
                int labelStartIndex = preebIndex + PreEBPrefix.Length;
                int preebIndexInFullData = preebIndex + areaOffset;

                if (preebIndexInFullData > 0 &&
                    !char.IsWhiteSpace(pemData[preebIndexInFullData - 1]))
                {
                    Debug.Assert(labelStartIndex > 0);
                    areaOffset += labelStartIndex;
                    pemArea = pemArea[labelStartIndex..];
                    continue;
                }

                int preebEndIndex = pemArea[labelStartIndex..].IndexOf(Ending);

                if (preebEndIndex < 0)
                {
                    fields = default;
                    return false;
                }

                int labelEndingIndex = labelStartIndex + preebEndIndex;
                int contentStartIndex = labelEndingIndex + Ending.Length;
                ReadOnlySpan<char> label = pemArea[labelStartIndex..labelEndingIndex];

                // There could be a preeb that is valid after this one if it has an invalid
                // label, so move from there.
                if (!IsValidLabel(label))
                {
                    goto next_after_label;
                }

                Range labelRange = (areaOffset + labelStartIndex)..(areaOffset + labelEndingIndex);
                int postebLength = PostEBPrefix.Length + label.Length + Ending.Length;
                Span<char> postebBuffer = postebLength > postebStackBufferSize ? new char[postebLength] : postebStackBuffer;
                PostEBPrefix.AsSpan().CopyTo(postebBuffer);
                label.CopyTo(postebBuffer[PostEBPrefix.Length..]);
                Ending.AsSpan().CopyTo(postebBuffer[(PostEBPrefix.Length + label.Length)..]);
                ReadOnlySpan<char> posteb = postebBuffer[..postebLength];
                int postebStartIndex = pemArea[contentStartIndex..].IndexOf(posteb);

                if (postebStartIndex < 0)
                {
                    goto next_after_label;
                }

                int contentEndIndex = postebStartIndex + contentStartIndex;
                int pemEndIndex = contentEndIndex + postebLength;

                if (pemEndIndex < pemArea.Length - 1 &&
                    !char.IsWhiteSpace(pemArea[pemEndIndex]))
                {
                    goto next_after_label;
                }

                Range contentRange = (areaOffset + contentStartIndex)..(areaOffset + contentEndIndex);

                if (!IsValidBase64(pemArea[contentStartIndex..contentEndIndex],
                                   out int base64start,
                                   out int base64end,
                                   out int decodedSize))
                {
                    Debug.Assert(labelEndingIndex > 0);
                    areaOffset += labelEndingIndex;
                    pemArea = pemArea[labelEndingIndex..];
                    continue;
                }

                Range pemRange = (areaOffset + preebIndex)..(areaOffset + pemEndIndex);
                Range base64range = (contentStartIndex + base64start + areaOffset)..(contentEndIndex + base64end + areaOffset);
                fields = new PemFields(labelRange, base64range, pemRange, decodedSize);
                return true;

                next_after_label:
                if (labelEndingIndex == 0)
                {
                    // We somehow ended up in a situation where we will advance 0 characters, which means we'll
                    // we'll probably end up here again in a loop. To avoid getting stuck in a loop, detect this
                    // situation and return.
                    fields = default;
                    return false;
                }
                areaOffset += labelEndingIndex;
                pemArea = pemArea[labelEndingIndex..];
            }

            fields = default;
            return false;
        }

        private static bool IsValidLabel(ReadOnlySpan<char> data)
        {
            static bool IsLabelChar(char c) => (uint)(c - 0x21u) <= 0x5du && c != '-';

            // Empty labels are permitted per RFC 7468.
            if (data.IsEmpty)
                return true;

            // First character of label must be a labelchar, which is a character
            // in 0x21..0x7e (both inclusive), except hyphens.
            if (!IsLabelChar(data[0]))
                return false;

            bool previousSpaceOrHyphen = false;
            for (int index = 1; index < data.Length; index++)
            {
                char c = data[index];

                if (IsLabelChar(c))
                {
                    previousSpaceOrHyphen = false;
                    continue;
                }

                bool isSpaceOrHyphen = c == ' ' || c == '-';

                if (!isSpaceOrHyphen || previousSpaceOrHyphen)
                    return false;

                previousSpaceOrHyphen = true;

            }
            return true;
        }

        private static bool IsValidBase64(
            ReadOnlySpan<char> data,
            out int base64Start,
            out int base64End,
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
            for (int index = 0; index < data.Length; index++)
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
                    base64Start = default;
                    base64End = default;
                    decodedSize = 0;
                    return false;
                }
            }

            base64Start = precedingWhiteSpace;
            base64End = -trailingWhiteSpace;
            decodedSize = (base64chars * 3) / 4;
            return true;
        }

        /// <summary>
        /// Determines the length of a PEM-encoded value, in characters,
        /// given the length of a label and binary data.
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
        /// The length of the PEM-encoded value is larger than <see cref="int.MaxValue"/>.
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

            int preebLength = PreEBPrefix.Length + labelLength + Ending.Length;
            int postebLength = PostEBPrefix.Length + labelLength + Ending.Length;
            int totalEncapLength = preebLength + postebLength + 1; //Add one for newline after preeb

            // dataLength is already known to not overflow here
            int encodedDataLength = ((dataLength + 2) / 3) << 2;
            int lineCount = Math.DivRem(encodedDataLength, EncodedLineLength, out int remainder);

            if (remainder > 0)
                lineCount++;

            int encodedDataLengthWithBreaks = encodedDataLength + lineCount;

            if (int.MaxValue - encodedDataLengthWithBreaks < totalEncapLength)
                throw new ArgumentException(SR.Argument_PemEncoding_EncodedSizeTooLarge);

            return encodedDataLengthWithBreaks + totalEncapLength;
        }

        /// <summary>
        /// Tries to write the provided data and label as PEM-encoded data into
        /// a provided buffer.
        /// </summary>
        /// <param name="label">
        /// The label to write.
        /// </param>
        /// <param name="data">
        /// The data to write.
        /// </param>
        /// <param name="destination">
        /// The buffer to receive the PEM-encoded text.
        /// </param>
        /// <param name="charsWritten">
        /// When this method returns, this parameter contains the number of characters
        /// written to <paramref name="destination"/>. This parameter is treated
        /// as uninitialized.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="destination"/> is large enough to contain
        /// the PEM-encoded text, otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method always wraps the base-64 encoded text to 64 characters, per the
        /// recommended wrapping of IETF RFC 7468. Unix-style line endings are used for line breaks.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="label"/> exceeds the maximum possible label length.
        ///   <para>
        ///       -or-
        ///   </para>
        ///   <paramref name="data"/> exceeds the maximum possible encoded data length.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The resulting PEM-encoded text is larger than <see cref="int.MaxValue"/>.
        ///   <para>
        ///       - or -
        ///   </para>
        /// <paramref name="label"/> contains invalid characters.
        /// </exception>
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
                {
                    Debug.Fail("Convert.TryToBase64Chars failed with a pre-sized buffer");
                    throw new CryptographicException();
                }

                offset += base64Written;
            }

            if (!IsValidLabel(label))
                throw new ArgumentException(SR.Argument_PemEncoding_InvalidLabel, nameof(label));

            const string NewLine = "\n";
            const int BytesPerLine = 48;
            int encodedSize = GetEncodedSize(label.Length, data.Length);

            if (destination.Length < encodedSize)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = 0;
            Write(PreEBPrefix, destination, ref charsWritten);
            Write(label, destination, ref charsWritten);
            Write(Ending, destination, ref charsWritten);
            Write(NewLine, destination, ref charsWritten);

            ReadOnlySpan<byte> remainingData = data;
            while (remainingData.Length >= BytesPerLine)
            {
                WriteBase64(remainingData[..BytesPerLine], destination, ref charsWritten);
                remainingData = remainingData[BytesPerLine..];
                Write(NewLine, destination, ref charsWritten);
            }

            Debug.Assert(remainingData.Length < BytesPerLine);

            if (remainingData.Length > 0)
            {
                WriteBase64(remainingData, destination, ref charsWritten);
                Write(NewLine, destination, ref charsWritten);
                remainingData = default;
            }

            Write(PostEBPrefix, destination, ref charsWritten);
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
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="label"/> exceeds the maximum possible label length.
        ///   <para>
        ///       -or-
        ///   </para>
        ///   <paramref name="data"/> exceeds the maximum possible encoded data length.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The resulting PEM-encoded text is larger than <see cref="int.MaxValue"/>.
        ///   <para>
        ///       - or -
        ///   </para>
        /// <paramref name="label"/> contains invalid characters.
        /// </exception>
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
