// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Security.Cryptography.Asn1
{
    internal partial class AsnReader
    {
        /// <summary>
        ///   Reads the next value as character string with a UNIVERSAL tag appropriate to the specified
        ///   encoding type, returning the contents as an unprocessed <see cref="ReadOnlyMemory{T}"/>
        ///   over the original data.
        /// </summary>
        /// <param name="encodingType">
        ///   A <see cref="UniversalTagNumber"/> corresponding to the value type to process.
        /// </param>
        /// <param name="contents">
        ///   On success, receives a <see cref="ReadOnlyMemory{T}"/> over the original data
        ///   corresponding to the contents of the character string.
        /// </param>
        /// <returns>
        ///   <c>true</c> and advances the reader if the value had a primitive encoding,
        ///   <c>false</c> and does not advance the reader if it had a constructed encoding.
        /// </returns>
        /// <remarks>
        ///   This method does not determine if the string used only characters defined by the encoding.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="encodingType"/> is not a known character string type.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        /// <seealso cref="TryCopyCharacterStringBytes(UniversalTagNumber,Span{byte},out int)"/>
        public bool TryReadPrimitiveCharacterStringBytes(
            UniversalTagNumber encodingType,
            out ReadOnlyMemory<byte> contents)
        {
            return TryReadPrimitiveCharacterStringBytes(
                new Asn1Tag(encodingType),
                encodingType,
                out contents);
        }

        /// <summary>
        ///   Reads the next value as a character with a specified tag, returning the contents
        ///   as an unprocessed <see cref="ReadOnlyMemory{T}"/> over the original data.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <param name="encodingType">
        ///   A <see cref="UniversalTagNumber"/> corresponding to the value type to process.
        /// </param>
        /// <param name="contents">
        ///   On success, receives a <see cref="ReadOnlyMemory{T}"/> over the original data
        ///   corresponding to the value of the OCTET STRING.
        /// </param>
        /// <returns>
        ///   <c>true</c> and advances the reader if the OCTET STRING value had a primitive encoding,
        ///   <c>false</c> and does not advance the reader if it had a constructed encoding.
        /// </returns>
        /// <remarks>
        ///   This method does not determine if the string used only characters defined by the encoding.
        /// </remarks>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not the same as
        ///   <paramref name="encodingType"/>.
        /// </exception>
        /// <seealso cref="TryCopyCharacterStringBytes(Asn1Tag,UniversalTagNumber,Span{byte},out int)"/>
        public bool TryReadPrimitiveCharacterStringBytes(
            Asn1Tag expectedTag,
            UniversalTagNumber encodingType,
            out ReadOnlyMemory<byte> contents)
        {
            CheckCharacterStringEncodingType(encodingType);

            // T-REC-X.690-201508 sec 8.23.3, all character strings are encoded as octet strings.
            return TryReadPrimitiveOctetStringBytes(expectedTag, encodingType, out contents);
        }

        /// <summary>
        ///   Reads the next value as character string with a UNIVERSAL tag appropriate to the specified
        ///   encoding type, copying the value into a provided destination buffer.
        /// </summary>
        /// <param name="encodingType">
        ///   A <see cref="UniversalTagNumber"/> corresponding to the value type to process.
        /// </param>
        /// <param name="destination">The buffer in which to write.</param>
        /// <param name="bytesWritten">
        ///   On success, receives the number of bytes written to <paramref name="destination"/>.
        /// </param>
        /// <returns>
        ///   <c>true</c> and advances the reader if <paramref name="destination"/> had sufficient
        ///   length to receive the value, otherwise
        ///   <c>false</c> and the reader does not advance.
        /// </returns>
        /// <remarks>
        ///   This method does not determine if the string used only characters defined by the encoding.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="encodingType"/> is not a known character string type.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        /// <seealso cref="TryReadPrimitiveCharacterStringBytes(UniversalTagNumber,out ReadOnlyMemory{byte})"/>
        /// <seealso cref="ReadCharacterString(UniversalTagNumber)"/>
        /// <seealso cref="TryCopyCharacterString(UniversalTagNumber,Span{char},out int)"/>
        public bool TryCopyCharacterStringBytes(
            UniversalTagNumber encodingType,
            Span<byte> destination,
            out int bytesWritten)
        {
            return TryCopyCharacterStringBytes(
                new Asn1Tag(encodingType),
                encodingType,
                destination,
                out bytesWritten);
        }

        /// <summary>
        ///   Reads the next value as character string with the specified tag and
        ///   encoding type, copying the value into a provided destination buffer.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <param name="encodingType">
        ///   A <see cref="UniversalTagNumber"/> corresponding to the value type to process.
        /// </param>
        /// <param name="destination">The buffer in which to write.</param>
        /// <param name="bytesWritten">
        ///   On success, receives the number of bytes written to <paramref name="destination"/>.
        /// </param>
        /// <returns>
        ///   <c>true</c> and advances the reader if <paramref name="destination"/> had sufficient
        ///   length to receive the value, otherwise
        ///   <c>false</c> and the reader does not advance.
        /// </returns>
        /// <remarks>
        ///   This method does not determine if the string used only characters defined by the encoding.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="encodingType"/> is not a known character string type.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not the same as
        ///   <paramref name="encodingType"/>.
        /// </exception>
        /// <seealso cref="TryReadPrimitiveCharacterStringBytes(Asn1Tag,UniversalTagNumber,out ReadOnlyMemory{byte})"/>
        /// <seealso cref="ReadCharacterString(Asn1Tag,UniversalTagNumber)"/>
        /// <seealso cref="TryCopyCharacterString(Asn1Tag,UniversalTagNumber,Span{char},out int)"/>
        public bool TryCopyCharacterStringBytes(
            Asn1Tag expectedTag,
            UniversalTagNumber encodingType,
            Span<byte> destination,
            out int bytesWritten)
        {
            CheckCharacterStringEncodingType(encodingType);

            bool copied = TryCopyCharacterStringBytes(
                expectedTag,
                encodingType,
                destination,
                out int bytesRead,
                out bytesWritten);

            if (copied)
            {
                _data = _data.Slice(bytesRead);
            }

            return copied;
        }

        /// <summary>
        ///   Reads the next value as character string with a UNIVERSAL tag appropriate to the specified
        ///   encoding type, copying the value into a provided destination buffer.
        /// </summary>
        /// <param name="encodingType">
        ///   A <see cref="UniversalTagNumber"/> corresponding to the value type to process.
        /// </param>
        /// <param name="destination">The buffer in which to write.</param>
        /// <param name="bytesWritten">
        ///   On success, receives the number of bytes written to <paramref name="destination"/>.
        /// </param>
        /// <returns>
        ///   <c>true</c> and advances the reader if <paramref name="destination"/> had sufficient
        ///   length to receive the value, otherwise
        ///   <c>false</c> and the reader does not advance.
        /// </returns>
        /// <remarks>
        ///   This method does not determine if the string used only characters defined by the encoding.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="encodingType"/> is not a known character string type.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        /// <seealso cref="TryReadPrimitiveCharacterStringBytes(UniversalTagNumber,out ReadOnlyMemory{byte})"/>
        /// <seealso cref="ReadCharacterString(UniversalTagNumber)"/>
        /// <seealso cref="TryCopyCharacterString(UniversalTagNumber,Span{char},out int)"/>
        public bool TryCopyCharacterStringBytes(
            UniversalTagNumber encodingType,
            ArraySegment<byte> destination,
            out int bytesWritten)
        {
            return TryCopyCharacterStringBytes(
                new Asn1Tag(encodingType),
                encodingType,
                destination.AsSpan(),
                out bytesWritten);
        }

        /// <summary>
        ///   Reads the next value as character string with the specified tag and
        ///   encoding type, copying the value into a provided destination buffer.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <param name="encodingType">
        ///   A <see cref="UniversalTagNumber"/> corresponding to the value type to process.
        /// </param>
        /// <param name="destination">The buffer in which to write.</param>
        /// <param name="bytesWritten">
        ///   On success, receives the number of bytes written to <paramref name="destination"/>.
        /// </param>
        /// <returns>
        ///   <c>true</c> and advances the reader if <paramref name="destination"/> had sufficient
        ///   length to receive the value, otherwise
        ///   <c>false</c> and the reader does not advance.
        /// </returns>
        /// <remarks>
        ///   This method does not determine if the string used only characters defined by the encoding.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="encodingType"/> is not a known character string type.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not the same as
        ///   <paramref name="encodingType"/>.
        /// </exception>
        /// <seealso cref="TryReadPrimitiveCharacterStringBytes(Asn1Tag,UniversalTagNumber,out ReadOnlyMemory{byte})"/>
        /// <seealso cref="ReadCharacterString(Asn1Tag,UniversalTagNumber)"/>
        /// <seealso cref="TryCopyCharacterString(Asn1Tag,UniversalTagNumber,Span{char},out int)"/>
        public bool TryCopyCharacterStringBytes(
            Asn1Tag expectedTag,
            UniversalTagNumber encodingType,
            ArraySegment<byte> destination,
            out int bytesWritten)
        {
            return TryCopyCharacterStringBytes(
                expectedTag,
                encodingType,
                destination.AsSpan(),
                out bytesWritten);
        }

        /// <summary>
        ///   Reads the next value as character string with a UNIVERSAL tag appropriate to the specified
        ///   encoding type, copying the decoded value into a provided destination buffer.
        /// </summary>
        /// <param name="encodingType">
        ///   A <see cref="UniversalTagNumber"/> corresponding to the value type to process.
        /// </param>
        /// <param name="destination">The buffer in which to write.</param>
        /// <param name="charsWritten">
        ///   On success, receives the number of chars written to <paramref name="destination"/>.
        /// </param>
        /// <returns>
        ///   <c>true</c> and advances the reader if <paramref name="destination"/> had sufficient
        ///   length to receive the value, otherwise
        ///   <c>false</c> and the reader does not advance.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="encodingType"/> is not a known character string type.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules --OR--
        ///   the string did not successfully decode
        /// </exception>
        /// <seealso cref="TryReadPrimitiveCharacterStringBytes(UniversalTagNumber,out ReadOnlyMemory{byte})"/>
        /// <seealso cref="ReadCharacterString(UniversalTagNumber)"/>
        /// <seealso cref="TryCopyCharacterStringBytes(UniversalTagNumber,Span{byte},out int)"/>
        public bool TryCopyCharacterString(
            UniversalTagNumber encodingType,
            Span<char> destination,
            out int charsWritten)
        {
            return TryCopyCharacterString(
                new Asn1Tag(encodingType),
                encodingType,
                destination,
                out charsWritten);
        }

        /// <summary>
        ///   Reads the next value as character string with the specified tag and
        ///   encoding type, copying the decoded value into a provided destination buffer.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <param name="encodingType">
        ///   A <see cref="UniversalTagNumber"/> corresponding to the value type to process.
        /// </param>
        /// <param name="destination">The buffer in which to write.</param>
        /// <param name="charsWritten">
        ///   On success, receives the number of chars written to <paramref name="destination"/>.
        /// </param>
        /// <returns>
        ///   <c>true</c> and advances the reader if <paramref name="destination"/> had sufficient
        ///   length to receive the value, otherwise
        ///   <c>false</c> and the reader does not advance.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="encodingType"/> is not a known character string type.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules --OR--
        ///   the string did not successfully decode
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not the same as
        ///   <paramref name="encodingType"/>.
        /// </exception>
        /// <seealso cref="TryReadPrimitiveCharacterStringBytes(Asn1Tag,UniversalTagNumber,out ReadOnlyMemory{byte})"/>
        /// <seealso cref="TryCopyCharacterStringBytes(Asn1Tag,UniversalTagNumber,Span{byte},out int)"/>
        /// <seealso cref="ReadCharacterString(Asn1Tag,UniversalTagNumber)"/>
        public bool TryCopyCharacterString(
            Asn1Tag expectedTag,
            UniversalTagNumber encodingType,
            Span<char> destination,
            out int charsWritten)
        {
            Text.Encoding encoding = AsnCharacterStringEncodings.GetEncoding(encodingType);
            return TryCopyCharacterString(expectedTag, encodingType, encoding, destination, out charsWritten);
        }

        /// <summary>
        ///   Reads the next value as character string with a UNIVERSAL tag appropriate to the specified
        ///   encoding type, copying the decoded value into a provided destination buffer.
        /// </summary>
        /// <param name="encodingType">
        ///   A <see cref="UniversalTagNumber"/> corresponding to the value type to process.
        /// </param>
        /// <param name="destination">The buffer in which to write.</param>
        /// <param name="charsWritten">
        ///   On success, receives the number of chars written to <paramref name="destination"/>.
        /// </param>
        /// <returns>
        ///   <c>true</c> and advances the reader if <paramref name="destination"/> had sufficient
        ///   length to receive the value, otherwise
        ///   <c>false</c> and the reader does not advance.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="encodingType"/> is not a known character string type.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules --OR--
        ///   the string did not successfully decode
        /// </exception>
        /// <seealso cref="TryReadPrimitiveCharacterStringBytes(UniversalTagNumber,out ReadOnlyMemory{byte})"/>
        /// <seealso cref="ReadCharacterString(UniversalTagNumber)"/>
        /// <seealso cref="TryCopyCharacterStringBytes(UniversalTagNumber,ArraySegment{byte},out int)"/>
        /// <seealso cref="TryCopyCharacterString(Asn1Tag,UniversalTagNumber,ArraySegment{char},out int)"/>
        public bool TryCopyCharacterString(
            UniversalTagNumber encodingType,
            ArraySegment<char> destination,
            out int charsWritten)
        {
            return TryCopyCharacterString(
                new Asn1Tag(encodingType),
                encodingType,
                destination.AsSpan(),
                out charsWritten);
        }

        /// <summary>
        ///   Reads the next value as character string with the specified tag and
        ///   encoding type, copying the decoded value into a provided destination buffer.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <param name="encodingType">
        ///   A <see cref="UniversalTagNumber"/> corresponding to the value type to process.
        /// </param>
        /// <param name="destination">The buffer in which to write.</param>
        /// <param name="charsWritten">
        ///   On success, receives the number of chars written to <paramref name="destination"/>.
        /// </param>
        /// <returns>
        ///   <c>true</c> and advances the reader if <paramref name="destination"/> had sufficient
        ///   length to receive the value, otherwise
        ///   <c>false</c> and the reader does not advance.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="encodingType"/> is not a known character string type.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules --OR--
        ///   the string did not successfully decode
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not the same as
        ///   <paramref name="encodingType"/>.
        /// </exception>
        /// <seealso cref="TryReadPrimitiveCharacterStringBytes(Asn1Tag,UniversalTagNumber,out ReadOnlyMemory{byte})"/>
        /// <seealso cref="TryCopyCharacterStringBytes(Asn1Tag,UniversalTagNumber,ArraySegment{byte},out int)"/>
        /// <seealso cref="ReadCharacterString(Asn1Tag,UniversalTagNumber)"/>
        public bool TryCopyCharacterString(
            Asn1Tag expectedTag,
            UniversalTagNumber encodingType,
            ArraySegment<char> destination,
            out int charsWritten)
        {
            return TryCopyCharacterString(
                expectedTag,
                encodingType,
                destination.AsSpan(),
                out charsWritten);
        }

        /// <summary>
        ///   Reads the next value as character string with a UNIVERSAL tag appropriate to the specified
        ///   encoding type, returning the decoded value as a <see cref="string"/>.
        /// </summary>
        /// <param name="encodingType">
        ///   A <see cref="UniversalTagNumber"/> corresponding to the value type to process.
        /// </param>
        /// <returns>
        ///   the decoded value as a <see cref="string"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="encodingType"/> is not a known character string type.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules --OR--
        ///   the string did not successfully decode
        /// </exception>
        /// <seealso cref="TryReadPrimitiveCharacterStringBytes(UniversalTagNumber,out ReadOnlyMemory{byte})"/>
        /// <seealso cref="TryCopyCharacterStringBytes(UniversalTagNumber,Span{byte},out int)"/>
        /// <seealso cref="TryCopyCharacterString(UniversalTagNumber,Span{char},out int)"/>
        /// <seealso cref="ReadCharacterString(Asn1Tag,UniversalTagNumber)"/>
        public string ReadCharacterString(UniversalTagNumber encodingType) =>
            ReadCharacterString(new Asn1Tag(encodingType), encodingType);

        /// <summary>
        ///   Reads the next value as character string with the specified tag and
        ///   encoding type, returning the decoded value as a <see cref="string"/>.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <param name="encodingType">
        ///   A <see cref="UniversalTagNumber"/> corresponding to the value type to process.
        /// </param>
        /// <returns>
        ///   the decoded value as a <see cref="string"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="encodingType"/> is not a known character string type.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules --OR--
        ///   the string did not successfully decode
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not the same as
        ///   <paramref name="encodingType"/>.
        /// </exception>
        /// <seealso cref="TryReadPrimitiveCharacterStringBytes(Asn1Tag,UniversalTagNumber,out ReadOnlyMemory{byte})"/>
        /// <seealso cref="TryCopyCharacterStringBytes(Asn1Tag,UniversalTagNumber,Span{byte},out int)"/>
        /// <seealso cref="TryCopyCharacterString(Asn1Tag,UniversalTagNumber,Span{char},out int)"/>
        public string ReadCharacterString(Asn1Tag expectedTag, UniversalTagNumber encodingType)
        {
            Text.Encoding encoding = AsnCharacterStringEncodings.GetEncoding(encodingType);
            return ReadCharacterString(expectedTag, encodingType, encoding);
        }

        // T-REC-X.690-201508 sec 8.23
        private bool TryCopyCharacterStringBytes(
            Asn1Tag expectedTag,
            UniversalTagNumber universalTagNumber,
            Span<byte> destination,
            out int bytesRead,
            out int bytesWritten)
        {
            // T-REC-X.690-201508 sec 8.23.3, all character strings are encoded as octet strings.
            if (TryReadPrimitiveOctetStringBytes(
                expectedTag,
                out Asn1Tag actualTag,
                out int? contentLength,
                out int headerLength,
                out ReadOnlyMemory<byte> contents,
                universalTagNumber))
            {
                bytesWritten = contents.Length;

                if (destination.Length < bytesWritten)
                {
                    bytesWritten = 0;
                    bytesRead = 0;
                    return false;
                }

                contents.Span.CopyTo(destination);
                bytesRead = headerLength + bytesWritten;
                return true;
            }

            Debug.Assert(actualTag.IsConstructed);

            bool copied = TryCopyConstructedOctetStringContents(
                Slice(_data, headerLength, contentLength),
                destination,
                contentLength == null,
                out int contentBytesRead,
                out bytesWritten);

            if (copied)
            {
                bytesRead = headerLength + contentBytesRead;
            }
            else
            {
                bytesRead = 0;
            }

            return copied;
        }

        private static unsafe bool TryCopyCharacterString(
            ReadOnlySpan<byte> source,
            Span<char> destination,
            Text.Encoding encoding,
            out int charsWritten)
        {
            if (source.Length == 0)
            {
                charsWritten = 0;
                return true;
            }

            fixed (byte* bytePtr = &MemoryMarshal.GetReference(source))
            fixed (char* charPtr = &MemoryMarshal.GetReference(destination))
            {
                try
                {
                    int charCount = encoding.GetCharCount(bytePtr, source.Length);

                    if (charCount > destination.Length)
                    {
                        charsWritten = 0;
                        return false;
                    }

                    charsWritten = encoding.GetChars(bytePtr, source.Length, charPtr, destination.Length);
                    Debug.Assert(charCount == charsWritten);
                }
                catch (DecoderFallbackException e)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
                }

                return true;
            }
        }

        private string ReadCharacterString(
            Asn1Tag expectedTag,
            UniversalTagNumber universalTagNumber,
            Text.Encoding encoding)
        {
            byte[] rented = null;

            // T-REC-X.690-201508 sec 8.23.3, all character strings are encoded as octet strings.
            ReadOnlySpan<byte> contents = GetOctetStringContents(
                expectedTag,
                universalTagNumber,
                out int bytesRead,
                ref rented);

            try
            {
                string str;

                if (contents.Length == 0)
                {
                    str = string.Empty;
                }
                else
                {
                    unsafe
                    {
                        fixed (byte* bytePtr = &MemoryMarshal.GetReference(contents))
                        {
                            try
                            {
                                str = encoding.GetString(bytePtr, contents.Length);
                            }
                            catch (DecoderFallbackException e)
                            {
                                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
                            }
                        }
                    }
                }

                _data = _data.Slice(bytesRead);
                return str;
            }
            finally
            {
                if (rented != null)
                {
                    Array.Clear(rented, 0, contents.Length);
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }
        }

        private bool TryCopyCharacterString(
            Asn1Tag expectedTag,
            UniversalTagNumber universalTagNumber,
            Text.Encoding encoding,
            Span<char> destination,
            out int charsWritten)
        {
            byte[] rented = null;

            // T-REC-X.690-201508 sec 8.23.3, all character strings are encoded as octet strings.
            ReadOnlySpan<byte> contents = GetOctetStringContents(
                expectedTag,
                universalTagNumber,
                out int bytesRead,
                ref rented);

            try
            {
                bool copied = TryCopyCharacterString(
                    contents,
                    destination,
                    encoding,
                    out charsWritten);

                if (copied)
                {
                    _data = _data.Slice(bytesRead);
                }

                return copied;
            }
            finally
            {
                if (rented != null)
                {
                    Array.Clear(rented, 0, contents.Length);
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }
        }

        private static void CheckCharacterStringEncodingType(UniversalTagNumber encodingType)
        {
            // T-REC-X.680-201508 sec 41
            switch (encodingType)
            {
                case UniversalTagNumber.BMPString:
                case UniversalTagNumber.GeneralString:
                case UniversalTagNumber.GraphicString:
                case UniversalTagNumber.IA5String:
                case UniversalTagNumber.ISO646String:
                case UniversalTagNumber.NumericString:
                case UniversalTagNumber.PrintableString:
                case UniversalTagNumber.TeletexString:
                // T61String is an alias for TeletexString (already listed)
                case UniversalTagNumber.UniversalString:
                case UniversalTagNumber.UTF8String:
                case UniversalTagNumber.VideotexString:
                    // VisibleString is an alias for ISO646String (already listed)
                    return;
            }

            throw new ArgumentOutOfRangeException(nameof(encodingType));
        }
    }
}
