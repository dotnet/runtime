// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace System.Formats.Asn1
{
   public static partial class AsnDecoder
    {
        /// <summary>
        ///   Attempts to get an unprocessed character string value from <paramref name="source"/> with a
        ///   specified tag under the specified encoding rules, if the value is contained in a single
        ///   (primitive) encoding.
        /// </summary>
        /// <param name="source">The buffer containing encoded data.</param>
        /// <param name="ruleSet">The encoding constraints to use when interpreting the data.</param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading.
        /// </param>
        /// <param name="value">
        ///   On success, receives a slice of the input buffer that corresponds to
        ///   the value of the Bit String.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="bytesConsumed">
        ///   When this method returns, the total number of bytes for the encoded value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the character string value has a primitive encoding;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        ///   This method does not determine if the string used only characters defined by the encoding.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="ruleSet"/> is not defined.
        /// </exception>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not a character
        ///   string tag type.
        /// </exception>
        /// <seealso cref="TryReadCharacterStringBytes"/>
        public static bool TryReadPrimitiveCharacterStringBytes(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            Asn1Tag expectedTag,
            out ReadOnlySpan<byte> value,
            out int bytesConsumed)
        {
            // This doesn't matter, except for universal tags. It's eventually used to check that
            // we're not expecting the wrong universal tag; but we'll remove the need for that by
            // IsCharacterStringEncodingType.
            UniversalTagNumber universalTagNumber = UniversalTagNumber.IA5String;

            if (expectedTag.TagClass == TagClass.Universal)
            {
                universalTagNumber = (UniversalTagNumber)expectedTag.TagValue;

                if (!IsCharacterStringEncodingType(universalTagNumber))
                {
                    throw new ArgumentException(SR.Argument_Tag_NotCharacterString, nameof(expectedTag));
                }
            }

            // T-REC-X.690-201508 sec 8.23.3, all character strings are encoded as octet strings.
            return TryReadPrimitiveOctetStringCore(
                source,
                ruleSet,
                expectedTag,
                universalTagNumber,
                contentLength: out _,
                headerLength: out _,
                out value,
                out bytesConsumed);
        }

        /// <summary>
        ///   Attempts to read a character string value from <paramref name="source"/> with a
        ///   specified tag under the specified encoding rules,
        ///   copying the unprocessed bytes into the provided destination buffer.
        /// </summary>
        /// <param name="source">The buffer containing encoded data.</param>
        /// <param name="destination">The buffer in which to write.</param>
        /// <param name="ruleSet">The encoding constraints to use when interpreting the data.</param>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <param name="bytesConsumed">
        ///   When this method returns, the total number of bytes for the encoded value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="bytesWritten">
        ///   On success, receives the number of bytes written to <paramref name="destination"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="destination"/> is large enough to receive the
        ///   value of the unprocessed character string;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        ///   This method does not determine if the string used only characters defined by the encoding.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="ruleSet"/> is not defined.
        /// </exception>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not a character
        ///   string tag type.
        ///
        ///   -or-
        ///
        ///   <paramref name="destination"/> overlaps <paramref name="source"/>.
        /// </exception>
        /// <seealso cref="TryReadPrimitiveCharacterStringBytes"/>
        /// <seealso cref="ReadCharacterString"/>
        /// <seealso cref="TryReadCharacterString"/>
        public static bool TryReadCharacterStringBytes(
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            AsnEncodingRules ruleSet,
            Asn1Tag expectedTag,
            out int bytesConsumed,
            out int bytesWritten)
        {
            if (source.Overlaps(destination))
            {
                throw new ArgumentException(
                    SR.Argument_SourceOverlapsDestination,
                    nameof(destination));
            }

            // This doesn't matter, except for universal tags. It's eventually used to check that
            // we're not expecting the wrong universal tag; but we'll remove the need for that by
            // IsCharacterStringEncodingType.
            UniversalTagNumber universalTagNumber = UniversalTagNumber.IA5String;

            if (expectedTag.TagClass == TagClass.Universal)
            {
                universalTagNumber = (UniversalTagNumber)expectedTag.TagValue;

                if (!IsCharacterStringEncodingType(universalTagNumber))
                {
                    throw new ArgumentException(SR.Argument_Tag_NotCharacterString, nameof(expectedTag));
                }
            }

            return TryReadCharacterStringBytesCore(
                source,
                ruleSet,
                expectedTag,
                universalTagNumber,
                destination,
                out bytesConsumed,
                out bytesWritten);
        }

        /// <summary>
        ///   Reads a character string value from <paramref name="source"/> with a specified tag under
        ///   the specified encoding rules, copying the decoded string into a provided destination buffer.
        /// </summary>
        /// <param name="source">The buffer containing encoded data.</param>
        /// <param name="destination">The buffer in which to write.</param>
        /// <param name="ruleSet">The encoding constraints to use when interpreting the data.</param>
        /// <param name="encodingType">
        ///   One of the enumeration values which represents the value type to process.
        /// </param>
        /// <param name="bytesConsumed">
        ///   When this method returns, the total number of bytes for the encoded value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="charsWritten">
        ///   When this method returns, the number of chars written to <paramref name="destination"/>.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the universal tag that is
        ///   appropriate to the requested encoding type.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> and advances the reader if <paramref name="destination"/> had sufficient
        ///   length to receive the value, otherwise
        ///   <see langword="false"/> and the reader does not advance.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="ruleSet"/> is not defined.
        ///
        ///   -or-
        ///
        ///   <paramref name="encodingType"/> is not a known character string type.
        /// </exception>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the string did not successfully decode.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not the same as
        ///   <paramref name="encodingType"/>.
        /// </exception>
        /// <seealso cref="TryReadPrimitiveCharacterStringBytes"/>
        /// <seealso cref="ReadCharacterString"/>
        public static bool TryReadCharacterString(
            ReadOnlySpan<byte> source,
            Span<char> destination,
            AsnEncodingRules ruleSet,
            UniversalTagNumber encodingType,
            out int bytesConsumed,
            out int charsWritten,
            Asn1Tag? expectedTag = null)
        {
            Text.Encoding encoding = AsnCharacterStringEncodings.GetEncoding(encodingType);

            return TryReadCharacterStringCore(
                source,
                ruleSet,
                expectedTag ?? new Asn1Tag(encodingType),
                encodingType,
                encoding,
                destination,
                out bytesConsumed,
                out charsWritten);
        }

        /// <summary>
        ///   Reads the next value as character string with the specified tag and
        ///   encoding type, returning the decoded string.
        /// </summary>
        /// <param name="source">The buffer containing encoded data.</param>
        /// <param name="ruleSet">The encoding constraints to use when interpreting the data.</param>
        /// <param name="encodingType">
        ///   One of the enumeration values which represents the value type to process.
        /// </param>
        /// <param name="bytesConsumed">
        ///   When this method returns, the total number of bytes for the encoded value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the universal tag that is
        ///   appropriate to the requested encoding type.
        /// </param>
        /// <returns>
        ///   The decoded value.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="ruleSet"/> is not defined.
        ///
        ///   -or-
        ///
        ///   <paramref name="encodingType"/> is not a known character string type.
        /// </exception>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the string did not successfully decode.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not the same as
        ///   <paramref name="encodingType"/>.
        /// </exception>
        /// <seealso cref="TryReadPrimitiveCharacterStringBytes"/>
        /// <seealso cref="TryReadCharacterStringBytes"/>
        /// <seealso cref="TryReadCharacterString"/>
        public static string ReadCharacterString(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            UniversalTagNumber encodingType,
            out int bytesConsumed,
            Asn1Tag? expectedTag = null)
        {
            Text.Encoding encoding = AsnCharacterStringEncodings.GetEncoding(encodingType);

            return ReadCharacterStringCore(
                source,
                ruleSet,
                expectedTag ?? new Asn1Tag(encodingType),
                encodingType,
                encoding,
                out bytesConsumed);
        }

        // T-REC-X.690-201508 sec 8.23
        private static bool TryReadCharacterStringBytesCore(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            Asn1Tag expectedTag,
            UniversalTagNumber universalTagNumber,
            Span<byte> destination,
            out int bytesConsumed,
            out int bytesWritten)
        {
            // T-REC-X.690-201508 sec 8.23.3, all character strings are encoded as octet strings.
            if (TryReadPrimitiveOctetStringCore(
                source,
                ruleSet,
                expectedTag,
                universalTagNumber,
                out int? contentLength,
                out int headerLength,
                out ReadOnlySpan<byte> contents,
                out int consumed))
            {
                if (contents.Length > destination.Length)
                {
                    bytesWritten = 0;
                    bytesConsumed = 0;
                    return false;
                }

                contents.CopyTo(destination);
                bytesWritten = contents.Length;
                bytesConsumed = consumed;
                return true;
            }

            bool copied = TryCopyConstructedOctetStringContents(
                Slice(source, headerLength, contentLength),
                ruleSet,
                destination,
                contentLength == null,
                out int bytesRead,
                out bytesWritten);

            if (copied)
            {
                bytesConsumed = headerLength + bytesRead;
            }
            else
            {
                bytesConsumed = 0;
            }

            return copied;
        }

        private static unsafe bool TryReadCharacterStringCore(
            ReadOnlySpan<byte> source,
            Span<char> destination,
            Text.Encoding encoding,
            out int charsWritten)
        {
            try
            {
#if NET8_0_OR_GREATER
                return encoding.TryGetChars(source, destination, out charsWritten);
#else
                if (source.Length == 0)
                {
                    charsWritten = 0;
                    return true;
                }

                fixed (byte* bytePtr = &MemoryMarshal.GetReference(source))
                fixed (char* charPtr = &MemoryMarshal.GetReference(destination))
                {
                    int charCount = encoding.GetCharCount(bytePtr, source.Length);

                    if (charCount > destination.Length)
                    {
                        charsWritten = 0;
                        return false;
                    }

                    charsWritten = encoding.GetChars(bytePtr, source.Length, charPtr, destination.Length);
                    Debug.Assert(charCount == charsWritten);

                    return true;
                }
#endif
            }
            catch (DecoderFallbackException e)
            {
                throw new AsnContentException(SR.ContentException_DefaultMessage, e);
            }
        }

        private static string ReadCharacterStringCore(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            Asn1Tag expectedTag,
            UniversalTagNumber universalTagNumber,
            Text.Encoding encoding,
            out int bytesConsumed)
        {
            byte[]? rented = null;

            // T-REC-X.690-201508 sec 8.23.3, all character strings are encoded as octet strings.
            ReadOnlySpan<byte> contents = GetOctetStringContents(
                source,
                ruleSet,
                expectedTag,
                universalTagNumber,
                out int bytesRead,
                ref rented);

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
                            throw new AsnContentException(SR.ContentException_DefaultMessage, e);
                        }
                    }
                }
            }

            if (rented != null)
            {
                CryptoPool.Return(rented, contents.Length);
            }

            bytesConsumed = bytesRead;
            return str;
        }

        private static bool TryReadCharacterStringCore(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            Asn1Tag expectedTag,
            UniversalTagNumber universalTagNumber,
            Text.Encoding encoding,
            Span<char> destination,
            out int bytesConsumed,
            out int charsWritten)
        {
            byte[]? rented = null;

            // T-REC-X.690-201508 sec 8.23.3, all character strings are encoded as octet strings.
            ReadOnlySpan<byte> contents = GetOctetStringContents(
                source,
                ruleSet,
                expectedTag,
                universalTagNumber,
                out int bytesRead,
                ref rented);

            bool copied = TryReadCharacterStringCore(
                contents,
                destination,
                encoding,
                out charsWritten);

            if (rented != null)
            {
                CryptoPool.Return(rented, contents.Length);
            }

            if (copied)
            {
                bytesConsumed = bytesRead;
            }
            else
            {
                bytesConsumed = 0;
            }

            return copied;
        }

        private static bool IsCharacterStringEncodingType(UniversalTagNumber encodingType)
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
                    return true;
            }

            return false;
        }
    }

    public partial class AsnReader
    {
        /// <summary>
        ///   Reads the next value as a character with a specified tag, returning the contents
        ///   as an unprocessed <see cref="ReadOnlyMemory{T}"/> over the original data.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <param name="contents">
        ///   On success, receives a <see cref="ReadOnlyMemory{T}"/> over the original data
        ///   corresponding to the value of the character string.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> and advances the reader if the character string value had a primitive encoding,
        ///   <see langword="false"/> and does not advance the reader if it had a constructed encoding.
        /// </returns>
        /// <remarks>
        ///   This method does not determine if the string used only characters defined by the encoding.
        /// </remarks>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not a character
        ///   string tag type.
        /// </exception>
        /// <seealso cref="TryReadCharacterStringBytes"/>
        public bool TryReadPrimitiveCharacterStringBytes(
            Asn1Tag expectedTag,
            out ReadOnlyMemory<byte> contents)
        {
            bool ret = AsnDecoder.TryReadPrimitiveCharacterStringBytes(
                _data.Span,
                RuleSet,
                expectedTag,
                out ReadOnlySpan<byte> span,
                out int consumed);

            if (ret)
            {
                contents = AsnDecoder.Slice(_data, span);
                _data = _data.Slice(consumed);
            }
            else
            {
                contents = default;
            }

            return ret;
        }

        /// <summary>
        ///   Reads the next value as character string with the specified tag,
        ///   copying the unprocessed bytes into a provided destination buffer.
        /// </summary>
        /// <param name="destination">The buffer in which to write.</param>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <param name="bytesWritten">
        ///   On success, receives the number of bytes written to <paramref name="destination"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> and advances the reader if <paramref name="destination"/> had sufficient
        ///   length to receive the value, otherwise
        ///   <see langword="false"/> and the reader does not advance.
        /// </returns>
        /// <remarks>
        ///   This method does not determine if the string used only characters defined by the encoding.
        /// </remarks>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not a character
        ///   string tag type.
        /// </exception>
        /// <seealso cref="TryReadPrimitiveCharacterStringBytes"/>
        /// <seealso cref="ReadCharacterString"/>
        /// <seealso cref="TryReadCharacterString"/>
        public bool TryReadCharacterStringBytes(
            Span<byte> destination,
            Asn1Tag expectedTag,
            out int bytesWritten)
        {
            bool ret = AsnDecoder.TryReadCharacterStringBytes(
                _data.Span,
                destination,
                RuleSet,
                expectedTag,
                out int consumed,
                out bytesWritten);

            if (ret)
            {
                _data = _data.Slice(consumed);
            }

            return ret;
        }

        /// <summary>
        ///   Reads the next value as character string with the specified tag and
        ///   encoding type, copying the decoded value into a provided destination buffer.
        /// </summary>
        /// <param name="encodingType">
        ///   One of the enumeration values representing the value type to process.
        /// </param>
        /// <param name="destination">The buffer in which to write.</param>
        /// <param name="charsWritten">
        ///   On success, receives the number of chars written to <paramref name="destination"/>.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the universal tag that is
        ///   appropriate to the requested encoding type.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> and advances the reader if <paramref name="destination"/> had sufficient
        ///   length to receive the value, otherwise
        ///   <see langword="false"/> and the reader does not advance.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="encodingType"/> is not a known character string type.
        /// </exception>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the string did not successfully decode.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not the same as
        ///   <paramref name="encodingType"/>.
        /// </exception>
        /// <seealso cref="TryReadPrimitiveCharacterStringBytes"/>
        /// <seealso cref="TryReadCharacterStringBytes"/>
        /// <seealso cref="ReadCharacterString"/>
        public bool TryReadCharacterString(
            Span<char> destination,
            UniversalTagNumber encodingType,
            out int charsWritten,
            Asn1Tag? expectedTag = null)
        {
            bool ret = AsnDecoder.TryReadCharacterString(
                _data.Span,
                destination,
                RuleSet,
                encodingType,
                out int consumed,
                out charsWritten,
                expectedTag);

            _data = _data.Slice(consumed);
            return ret;
        }

        /// <summary>
        ///   Reads the next value as character string with the specified tag and
        ///   encoding type, returning the decoded value as a string.
        /// </summary>
        /// <param name="encodingType">
        ///   One of the enumeration values representing the value type to process.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the universal tag that is
        ///   appropriate to the requested encoding type.
        /// </param>
        /// <returns>
        ///   The decoded value.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="encodingType"/> is not a known character string type.
        /// </exception>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the string did not successfully decode.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not the same as
        ///   <paramref name="encodingType"/>.
        /// </exception>
        /// <seealso cref="TryReadPrimitiveCharacterStringBytes"/>
        /// <seealso cref="TryReadCharacterStringBytes"/>
        /// <seealso cref="TryReadCharacterString"/>
        public string ReadCharacterString(UniversalTagNumber encodingType, Asn1Tag? expectedTag = null)
        {
            string ret = AsnDecoder.ReadCharacterString(
                _data.Span,
                RuleSet,
                encodingType,
                out int consumed,
                expectedTag);

            _data = _data.Slice(consumed);
            return ret;
        }
    }
}
