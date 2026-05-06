// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Unicode;

namespace System.Security.Cryptography
{
    /// <summary>
    /// Provides methods for reading and writing the IETF RFC 7468
    /// subset of PEM (Privacy-Enhanced Mail) textual encodings.
    /// This class cannot be inherited.
    /// </summary>
    public static class PemEncoding
    {
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
        /// <remarks>
        /// IETF RFC 7468 permits different decoding rules. This method
        /// always uses lax rules.
        /// </remarks>
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
        /// The text containing the PEM-encoded data.
        /// </param>
        /// <exception cref="ArgumentException">
        /// <paramref name="pemData"/> does not contain a well-formed PEM-encoded value.
        /// </exception>
        /// <returns>
        /// A value that specifies the location, label, and data location of
        /// the encoded data.
        /// </returns>
        /// <remarks>
        ///   <para>IETF RFC 7468 permits different decoding rules. This method always uses lax rules.</para>
        ///   <para>
        ///     This does not validate the UTF-8 data outside of encapsulation boundaries and is ignored. It is the caller's
        ///     responsibility to ensure the entire input is UTF-8 if required.
        ///   </para>
        /// </remarks>
        public static PemFields FindUtf8(ReadOnlySpan<byte> pemData)
        {
            if (!TryFindUtf8(pemData, out PemFields fields))
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
        /// <remarks>
        /// IETF RFC 7468 permits different decoding rules. This method
        /// always uses lax rules.
        /// </remarks>
        public static bool TryFind(ReadOnlySpan<char> pemData, out PemFields fields)
        {
            return TryFindCore<char, Utf16PemEncoder>(pemData, out fields);
        }

        /// <summary>
        ///   Attempts to find the first PEM-encoded data.
        /// </summary>
        /// <param name="pemData">
        ///   The text containing the PEM-encoded data.
        /// </param>
        /// <param name="fields">
        ///   When this method returns, contains a value
        ///   that specifies the location, label, and data location of the encoded data;
        ///   or that specifies those locations as empty if no PEM-encoded data is found.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if PEM-encoded data was found; otherwise <see langword="false" />.
        /// </returns>
        /// <remarks>
        ///   <para>IETF RFC 7468 permits different decoding rules. This method always uses lax rules.</para>
        ///   <para>
        ///     This does not validate the UTF-8 data outside of encapsulation boundaries and is ignored. It is the caller's
        ///     responsibility to ensure the entire input is UTF-8 if required.
        ///   </para>
        /// </remarks>
        public static bool TryFindUtf8(ReadOnlySpan<byte> pemData, out PemFields fields)
        {
            return TryFindCore<byte, Utf8PemEncoder>(pemData, out fields);
        }

        private static bool TryFindCore<TChar, T>(ReadOnlySpan<TChar> pemData, out PemFields fields)
            where T : IPemEncoder<TChar>
            where TChar : unmanaged, IEquatable<TChar>, INumber<TChar>
        {
            // Check for the minimum possible encoded length of a PEM structure
            // and exit early if there is no way the input could contain a well-formed
            // PEM.
            if (pemData.Length < T.PreEBPrefix.Length + T.Ending.Length * 2 + T.PostEBPrefix.Length)
            {
                fields = default;
                return false;
            }

            const int PostebStackBufferSize = 256;
            Span<TChar> postebStackBuffer = stackalloc TChar[PostebStackBufferSize];
            int areaOffset = 0;
            int preebIndex;
            while ((preebIndex = pemData.IndexOfByOffset(T.PreEBPrefix, areaOffset)) >= 0)
            {
                int labelStartIndex = preebIndex + T.PreEBPrefix.Length;

                // If there are any previous characters, the one prior to the PreEB
                // must be a white space character.
                if (preebIndex > 0 && !IsWhiteSpaceCharacter(pemData[preebIndex - 1], T.Whitespace))
                {
                    areaOffset = labelStartIndex;
                    continue;
                }

                int preebEndIndex = pemData.IndexOfByOffset(T.Ending, labelStartIndex);

                // There is no ending sequence, -----, in the remainder of
                // the document. Therefore, there can never be a complete PreEB
                // and we can exit.
                if (preebEndIndex < 0)
                {
                    fields = default;
                    return false;
                }

                Range labelRange = labelStartIndex..preebEndIndex;
                ReadOnlySpan<TChar> label = pemData[labelRange];

                // There could be a preeb that is valid after this one if it has an invalid
                // label, so move from there.
                if (!IsValidLabel(label))
                {
                    goto NextAfterLabel;
                }

                int contentStartIndex = preebEndIndex + T.Ending.Length;
                int postebLength = T.PostEBPrefix.Length + label.Length + T.Ending.Length;

                Span<TChar> postebBuffer = postebLength > PostebStackBufferSize
                    ? new TChar[postebLength]
                    : postebStackBuffer;
                ReadOnlySpan<TChar> posteb = WritePostEB(label, postebBuffer);
                int postebStartIndex = pemData.IndexOfByOffset(posteb, contentStartIndex);

                if (postebStartIndex < 0)
                {
                    goto NextAfterLabel;
                }

                int pemEndIndex = postebStartIndex + postebLength;

                // The PostEB must either end at the end of the string, or
                // have at least one white space character after it.
                if (pemEndIndex < pemData.Length - 1 &&
                    !IsWhiteSpaceCharacter(pemData[pemEndIndex], T.Whitespace))
                {
                    goto NextAfterLabel;
                }

                Range contentRange = contentStartIndex..postebStartIndex;

                if (!TryCountBase64<TChar, T>(pemData[contentRange], out int base64start, out int base64end, out int decodedSize))
                {
                    goto NextAfterLabel;
                }

                Range pemRange = preebIndex..pemEndIndex;
                Range base64range = (contentStartIndex + base64start)..(contentStartIndex + base64end);
                fields = new PemFields(labelRange, base64range, pemRange, decodedSize);
                return true;

            NextAfterLabel:
                if (preebEndIndex <= areaOffset)
                {
                    // We somehow ended up in a situation where we will advance
                    // backward or not at all, which means we'll probably end up here again,
                    // advancing backward, in a loop. To avoid getting stuck,
                    // detect this situation and return.
                    fields = default;
                    return false;
                }
                areaOffset = preebEndIndex;
            }

            fields = default;
            return false;

            static ReadOnlySpan<TChar> WritePostEB(ReadOnlySpan<TChar> label, Span<TChar> destination)
            {
                int size = T.PostEBPrefix.Length + label.Length + T.Ending.Length;
                Debug.Assert(destination.Length >= size);
                T.PostEBPrefix.CopyTo(destination);
                label.CopyTo(destination.Slice(T.PostEBPrefix.Length));
                T.Ending.CopyTo(destination.Slice(T.PostEBPrefix.Length + label.Length));
                return destination.Slice(0, size);
            }
        }

        private static int IndexOfByOffset<TChar>(this ReadOnlySpan<TChar> str, ReadOnlySpan<TChar> value, int startPosition)
            where TChar : IEquatable<TChar>
        {
            Debug.Assert(startPosition <= str.Length);
            int index = str.Slice(startPosition).IndexOf(value);
            return index == -1 ? -1 : index + startPosition;
        }

        private static bool IsValidLabel<TChar>(ReadOnlySpan<TChar> data)
            where TChar : IEquatable<TChar>, INumber<TChar>
        {
            static bool IsLabelChar(TChar c)
            {
                return (c - TChar.CreateTruncating(0x21u)) <= TChar.CreateTruncating(0x5du) &&
                        c != TChar.CreateTruncating('-');
            }

            // Empty labels are permitted per RFC 7468.
            if (data.IsEmpty)
                return true;

            // The first character must be a labelchar, so initialize to false
            bool previousIsLabelChar = false;

            for (int index = 0; index < data.Length; index++)
            {
                TChar c = data[index];

                if (IsLabelChar(c))
                {
                    previousIsLabelChar = true;
                    continue;
                }

                bool isSpaceOrHyphen = c == TChar.CreateTruncating(' ') || c == TChar.CreateTruncating('-');

                // IETF RFC 7468 states that every character in a label must
                // be a labelchar, and each labelchar may have zero or one
                // preceding space or hyphen, except the first labelchar.
                // If this character is not a space or hyphen, then this characer
                // is invalid.
                // If it is a space or hyphen, and the previous character was
                // also not a labelchar (another hyphen or space), then we have
                // two consecutive spaces or hyphens which is invalid.
                if (!isSpaceOrHyphen || !previousIsLabelChar)
                {
                    return false;
                }

                previousIsLabelChar = false;
            }

            // The last character must also be a labelchar. It cannot be a
            // hyphen or space since these are only allowed to precede
            // a labelchar.
            return previousIsLabelChar;
        }

        private static bool TryCountBase64<TChar, T>(
            ReadOnlySpan<TChar> str,
            out int base64Start,
            out int base64End,
            out int base64DecodedSize) where TChar : IEquatable<TChar> where T : IPemEncoder<TChar>
        {
            // Trim starting and ending allowed white space characters
            int start = 0;
            int end = str.Length - 1;
            for (; start < str.Length && IsWhiteSpaceCharacter(str[start], T.Whitespace); start++);
            for (; end > start && IsWhiteSpaceCharacter(str[end], T.Whitespace); end--);

            // Validate that the remaining characters are valid base-64 encoded data.
            if (T.IsValidBase64(str.Slice(start, end + 1 - start), out base64DecodedSize))
            {
                base64Start = start;
                base64End = end + 1;
                return true;
            }

            base64Start = 0;
            base64End = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsWhiteSpaceCharacter<TChar>(TChar ch, ReadOnlySpan<TChar> whitespace)
            where TChar : IEquatable<TChar>
        {
            return whitespace.Contains(ch);
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

            ArgumentOutOfRangeException.ThrowIfNegative(labelLength);
            ArgumentOutOfRangeException.ThrowIfNegative(dataLength);

            if (labelLength > MaxLabelSize)
                throw new ArgumentOutOfRangeException(nameof(labelLength), SR.Argument_PemEncoding_EncodedSizeTooLarge);
            if (dataLength > MaxDataLength)
                throw new ArgumentOutOfRangeException(nameof(dataLength), SR.Argument_PemEncoding_EncodedSizeTooLarge);

            Debug.Assert(Utf16PemEncoder.PostEBPrefix.Length == Utf8PemEncoder.PostEBPrefix.Length);
            Debug.Assert(Utf16PemEncoder.PreEBPrefix.Length == Utf8PemEncoder.PreEBPrefix.Length);
            Debug.Assert(Utf16PemEncoder.Ending.Length == Utf8PemEncoder.Ending.Length);

            // Which PemEncoder that is used for determining the length of things we use does not matter since the
            // reported value is in characters.
            int preebLength = Utf16PemEncoder.PreEBPrefix.Length + labelLength + Utf16PemEncoder.Ending.Length;
            int postebLength = Utf16PemEncoder.PostEBPrefix.Length + labelLength + Utf16PemEncoder.Ending.Length;
            int totalEncapLength = preebLength + postebLength + 1; //Add one for newline after preeb

            // dataLength is already known to not overflow here
            int encodedDataLength = ((dataLength + 2) / 3) << 2;
            int lineCount = Math.DivRem(encodedDataLength, EncodedLineLength, out int remainder);

            if (remainder > 0)
            {
                lineCount++;
            }

            int encodedDataLengthWithBreaks = encodedDataLength + lineCount;

            if (int.MaxValue - encodedDataLengthWithBreaks < totalEncapLength)
            {
                throw new ArgumentException(SR.Argument_PemEncoding_EncodedSizeTooLarge);
            }

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
            if (!IsValidLabel(label))
                throw new ArgumentException(SR.Argument_PemEncoding_InvalidLabel, nameof(label));

            int encodedSize = GetEncodedSize(label.Length, data.Length);

            if (destination.Length < encodedSize)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = WriteCore<char, Utf16PemEncoder>(label, data, destination);
            Debug.Assert(encodedSize == charsWritten);
            return true;
        }

        /// <summary>
        /// Tries to write the provided data and label as PEM-encoded data into
        /// a provided buffer.
        /// </summary>
        /// <param name="utf8Label">
        /// The label to write.
        /// </param>
        /// <param name="data">
        /// The data to write.
        /// </param>
        /// <param name="destination">
        /// The buffer to receive the PEM-encoded text.
        /// </param>
        /// <param name="bytesWritten">
        /// When this method returns, this parameter contains the number of UTF-8 encoded bytes
        /// written to <paramref name="destination"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="destination"/> is large enough to contain
        /// the PEM-encoded text, otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// This method always wraps the base-64 encoded text to 64 characters, per the
        /// recommended wrapping of IETF RFC 7468. Unix-style line endings are used for line breaks.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="utf8Label"/> exceeds the maximum possible label length.
        ///   <para>
        ///       -or-
        ///   </para>
        ///   <paramref name="data"/> exceeds the maximum possible encoded data length.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <para>
        ///   The resulting PEM-encoded text is larger than <see cref="int.MaxValue"/>.
        /// </para>
        /// <para>- or -</para>
        /// <para>
        ///   <paramref name="utf8Label"/> contains invalid characters or is malformed UTF-8.
        /// </para>
        /// </exception>
        public static bool TryWriteUtf8(
            ReadOnlySpan<byte> utf8Label,
            ReadOnlySpan<byte> data,
            Span<byte> destination,
            out int bytesWritten)
        {
            if (!Utf8.IsValid(utf8Label) || !IsValidLabel(utf8Label))
                throw new ArgumentException(SR.Argument_PemEncoding_InvalidLabel, nameof(utf8Label));

            int encodedSize = GetEncodedSize(utf8Label.Length, data.Length);

            if (destination.Length < encodedSize)
            {
                bytesWritten = 0;
                return false;
            }

            bytesWritten = WriteCore<byte, Utf8PemEncoder>(utf8Label, data, destination);
            Debug.Assert(encodedSize == bytesWritten);
            return true;
        }

        /// <summary>
        /// Creates an encoded PEM with the given label and data.
        /// </summary>
        /// <param name="utf8Label">
        /// The label to encode.
        /// </param>
        /// <param name="data">
        /// The data to encode.
        /// </param>
        /// <returns>
        ///   An array containing the bytes representing the UTF-8 encoding of the PEM.
        /// </returns>
        /// <remarks>
        /// This method always wraps the base-64 encoded text to 64 characters, per the
        /// recommended wrapping of RFC-7468. Unix-style line endings are used for line breaks.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <para>
        ///     <paramref name="utf8Label"/> exceeds the maximum possible label length.
        ///   </para>
        ///   <para>
        ///       -or-
        ///   </para>
        ///   <para>
        ///     <paramref name="data"/> exceeds the maximum possible encoded data length.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <para>
        ///   The resulting PEM-encoded text is larger than <see cref="int.MaxValue"/>.
        /// </para>
        /// <para> -or- </para>
        /// <para>
        ///   <paramref name="utf8Label"/> contains invalid characters or is malformed UTF-8.
        /// </para>
        /// </exception>
        public static byte[] WriteUtf8(ReadOnlySpan<byte> utf8Label, ReadOnlySpan<byte> data)
        {
            if (!Utf8.IsValid(utf8Label) || !IsValidLabel(utf8Label))
                throw new ArgumentException(SR.Argument_PemEncoding_InvalidLabel, nameof(utf8Label));

            int encodedSize = GetEncodedSize(utf8Label.Length, data.Length);
            byte[] buffer = new byte[encodedSize];

            int byteWritten = WriteCore<byte, Utf8PemEncoder>(utf8Label, data, buffer);
            Debug.Assert(byteWritten == encodedSize);
            return buffer;
        }

        private static int WriteCore<TChar, T>(ReadOnlySpan<TChar> label, ReadOnlySpan<byte> data, Span<TChar> destination)
            where T : IPemEncoder<TChar>
            where TChar : IEquatable<TChar>
        {
            static int Write(ReadOnlySpan<TChar> str, Span<TChar> dest, int offset)
            {
                str.CopyTo(dest.Slice(offset));
                return str.Length;
            }

            const int BytesPerLine = 48;

            int charsWritten = 0;
            charsWritten += Write(T.PreEBPrefix, destination, charsWritten);
            charsWritten += Write(label, destination, charsWritten);
            charsWritten += Write(T.Ending, destination, charsWritten);
            charsWritten += Write(T.NewLine, destination, charsWritten);

            ReadOnlySpan<byte> remainingData = data;
            while (remainingData.Length >= BytesPerLine)
            {
                charsWritten += T.WriteBase64(remainingData.Slice(0, BytesPerLine), destination, charsWritten);
                charsWritten += Write(T.NewLine, destination, charsWritten);
                remainingData = remainingData.Slice(BytesPerLine);
            }

            Debug.Assert(remainingData.Length < BytesPerLine);

            if (remainingData.Length > 0)
            {
                charsWritten += T.WriteBase64(remainingData, destination, charsWritten);
                charsWritten += Write(T.NewLine, destination, charsWritten);
            }

            charsWritten += Write(T.PostEBPrefix, destination, charsWritten);
            charsWritten += Write(label, destination, charsWritten);
            charsWritten += Write(T.Ending, destination, charsWritten);

            return charsWritten;
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
            if (!IsValidLabel(label))
                throw new ArgumentException(SR.Argument_PemEncoding_InvalidLabel, nameof(label));

            int encodedSize = GetEncodedSize(label.Length, data.Length);
            char[] buffer = new char[encodedSize];

            int charsWritten = WriteCore<char, Utf16PemEncoder>(label, data, buffer);
            Debug.Assert(charsWritten == encodedSize);
            return buffer;
        }

        /// <summary>
        ///   Creates an encoded PEM with the given label and data.
        /// </summary>
        /// <param name="label">
        ///   The label to encode.
        /// </param>
        /// <param name="data">
        ///   The data to encode.
        /// </param>
        /// <returns>
        ///   A string of the encoded PEM.
        /// </returns>
        /// <remarks>
        ///   This method always wraps the base-64 encoded text to 64 characters, per the
        ///   recommended wrapping of RFC-7468. Unix-style line endings are used for line breaks.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="label"/> exceeds the maximum possible label length.
        ///
        ///   -or-
        ///
        ///   <paramref name="data"/> exceeds the maximum possible encoded data length.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   The resulting PEM-encoded text is larger than <see cref="int.MaxValue"/>.
        ///
        ///   - or -
        ///
        ///   <paramref name="label"/> contains invalid characters.
        /// </exception>
        public static unsafe string WriteString(ReadOnlySpan<char> label, ReadOnlySpan<byte> data)
        {
            if (!IsValidLabel(label))
                throw new ArgumentException(SR.Argument_PemEncoding_InvalidLabel, nameof(label));

            int encodedSize = GetEncodedSize(label.Length, data.Length);

            return string.Create(
                encodedSize,
                (LabelPointer: (IntPtr)(&label), DataPointer: (IntPtr)(&data)),
                static (destination, state) =>
                {
                    ReadOnlySpan<char> label = *(ReadOnlySpan<char>*)state.LabelPointer;
                    ReadOnlySpan<byte> data = *(ReadOnlySpan<byte>*)state.DataPointer;

                    int charsWritten = WriteCore<char, Utf16PemEncoder>(label, data, destination);

                    if (charsWritten != destination.Length)
                    {
                        Debug.Fail("WriteCore wrote the wrong amount of data");
                        throw new CryptographicException();
                    }
                });
        }

        private interface IPemEncoder<TChar> where TChar : IEquatable<TChar>
        {
            static abstract ReadOnlySpan<TChar> PreEBPrefix { get; }
            static abstract ReadOnlySpan<TChar> PostEBPrefix { get; }
            static abstract ReadOnlySpan<TChar> Ending { get; }
            static abstract ReadOnlySpan<TChar> Whitespace { get; }
            static abstract ReadOnlySpan<TChar> NewLine { get; }
            static abstract bool IsValidBase64(ReadOnlySpan<TChar> base64Text, out int decodedLength);
            static abstract int WriteBase64(ReadOnlySpan<byte> bytes, Span<TChar> destination, int offset);
        }

        private sealed class Utf16PemEncoder : IPemEncoder<char>
        {
            public static ReadOnlySpan<char> PreEBPrefix => "-----BEGIN ";
            public static ReadOnlySpan<char> PostEBPrefix => "-----END ";
            public static ReadOnlySpan<char> Ending => "-----";
            public static ReadOnlySpan<char> Whitespace => " \t\n\r";
            public static ReadOnlySpan<char> NewLine => "\n";

            public static bool IsValidBase64(ReadOnlySpan<char> base64Text, out int decodedLength)
            {
                return Base64.IsValid(base64Text, out decodedLength);
            }

            public static int WriteBase64(ReadOnlySpan<byte> bytes, Span<char> destination, int offset)
            {
                bool success = Convert.TryToBase64Chars(bytes, destination.Slice(offset), out int base64Written);

                if (!success)
                {
                    Debug.Fail("Convert.TryToBase64Chars failed with a pre-sized buffer");
                    throw new ArgumentException(null, nameof(destination));
                }

                return base64Written;
            }
        }

        private sealed class Utf8PemEncoder : IPemEncoder<byte>
        {
            public static ReadOnlySpan<byte> PreEBPrefix => "-----BEGIN "u8;
            public static ReadOnlySpan<byte> PostEBPrefix => "-----END "u8;
            public static ReadOnlySpan<byte> Ending => "-----"u8;
            public static ReadOnlySpan<byte> Whitespace => " \t\n\r"u8;
            public static ReadOnlySpan<byte> NewLine => "\n"u8;

            public static bool IsValidBase64(ReadOnlySpan<byte> base64Text, out int decodedLength)
            {
                return Base64.IsValid(base64Text, out decodedLength);
            }

            public static int WriteBase64(ReadOnlySpan<byte> bytes, Span<byte> destination, int offset)
            {
                OperationStatus status = Base64.EncodeToUtf8(
                    bytes,
                    destination.Slice(offset),
                    out int bytesConsumed,
                    out int bytesWritten);

                if (status != OperationStatus.Done)
                {
                    Debug.Fail("Base64.EncodeToUtf8 failed with a pre-sized buffer");
                    throw new ArgumentException(null, nameof(destination));
                }

                Debug.Assert(bytesConsumed == bytes.Length);
                return bytesWritten;
            }
        }
    }
}
