// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Security.Cryptography;

namespace System.Formats.Asn1
{
    internal ref struct AsnValueReader
    {
        private static readonly byte[] s_singleByte = new byte[1];

        private ReadOnlySpan<byte> _span;
        private readonly AsnEncodingRules _ruleSet;

        internal AsnValueReader(ReadOnlySpan<byte> span, AsnEncodingRules ruleSet)
        {
            _span = span;
            _ruleSet = ruleSet;
        }

        internal bool HasData => !_span.IsEmpty;

        internal void ThrowIfNotEmpty()
        {
            if (!_span.IsEmpty)
            {
                new AsnReader(s_singleByte, _ruleSet).ThrowIfNotEmpty();
            }
        }

        internal Asn1Tag PeekTag()
        {
            return Asn1Tag.Decode(_span, out _);
        }

        internal ReadOnlySpan<byte> PeekContentBytes()
        {
            AsnDecoder.ReadEncodedValue(
                _span,
                _ruleSet,
                out int contentOffset,
                out int contentLength,
                out _);

            return _span.Slice(contentOffset, contentLength);
        }

        internal ReadOnlySpan<byte> PeekEncodedValue()
        {
            AsnDecoder.ReadEncodedValue(_span, _ruleSet, out _, out _, out int consumed);
            return _span.Slice(0, consumed);
        }

        internal ReadOnlySpan<byte> ReadEncodedValue()
        {
            ReadOnlySpan<byte> value = PeekEncodedValue();
            _span = _span.Slice(value.Length);
            return value;
        }

        internal bool ReadBoolean(Asn1Tag? expectedTag = default)
        {
            bool ret = AsnDecoder.ReadBoolean(_span, _ruleSet, out int consumed, expectedTag);
            _span = _span.Slice(consumed);
            return ret;
        }

        internal BigInteger ReadInteger(Asn1Tag? expectedTag = default)
        {
            BigInteger ret = AsnDecoder.ReadInteger(_span, _ruleSet, out int consumed, expectedTag);
            _span = _span.Slice(consumed);
            return ret;
        }

        internal bool TryReadInt32(out int value, Asn1Tag? expectedTag = default)
        {
            bool ret = AsnDecoder.TryReadInt32(_span, _ruleSet, out value, out int consumed, expectedTag);
            _span = _span.Slice(consumed);
            return ret;
        }

        internal ReadOnlySpan<byte> ReadIntegerBytes(Asn1Tag? expectedTag = default)
        {
            ReadOnlySpan<byte> ret = AsnDecoder.ReadIntegerBytes(_span, _ruleSet, out int consumed, expectedTag);
            _span = _span.Slice(consumed);
            return ret;
        }

        internal bool TryReadPrimitiveBitString(
            out int unusedBitCount,
            out ReadOnlySpan<byte> value,
            Asn1Tag? expectedTag = default)
        {
            bool ret = AsnDecoder.TryReadPrimitiveBitString(
                _span,
                _ruleSet,
                out unusedBitCount,
                out value,
                out int consumed,
                expectedTag);

            _span = _span.Slice(consumed);
            return ret;
        }

        internal byte[] ReadBitString(out int unusedBitCount, Asn1Tag? expectedTag = default)
        {
            byte[] ret = AsnDecoder.ReadBitString(
                _span,
                _ruleSet,
                out unusedBitCount,
                out int consumed,
                expectedTag);

            _span = _span.Slice(consumed);
            return ret;
        }

        internal TFlagsEnum ReadNamedBitListValue<TFlagsEnum>(Asn1Tag? expectedTag = default) where TFlagsEnum : Enum
        {
            TFlagsEnum ret = AsnDecoder.ReadNamedBitListValue<TFlagsEnum>(_span, _ruleSet, out int consumed, expectedTag);
            _span = _span.Slice(consumed);
            return ret;
        }

        internal bool TryReadPrimitiveOctetString(
            out ReadOnlySpan<byte> value,
            Asn1Tag? expectedTag = default)
        {
            bool ret = AsnDecoder.TryReadPrimitiveOctetString(
                _span,
                _ruleSet,
                out value,
                out int consumed,
                expectedTag);

            _span = _span.Slice(consumed);
            return ret;
        }

        internal byte[] ReadOctetString(Asn1Tag? expectedTag = default)
        {
            byte[] ret = AsnDecoder.ReadOctetString(
                _span,
                _ruleSet,
                out int consumed,
                expectedTag);

            _span = _span.Slice(consumed);
            return ret;
        }

        internal string ReadObjectIdentifier(Asn1Tag? expectedTag = default)
        {
            string ret = AsnDecoder.ReadObjectIdentifier(_span, _ruleSet, out int consumed, expectedTag);
            _span = _span.Slice(consumed);
            return ret;
        }

        internal AsnValueReader ReadSequence(Asn1Tag? expectedTag = default)
        {
            AsnDecoder.ReadSequence(
                _span,
                _ruleSet,
                out int contentOffset,
                out int contentLength,
                out int bytesConsumed,
                expectedTag);

            ReadOnlySpan<byte> content = _span.Slice(contentOffset, contentLength);
            _span = _span.Slice(bytesConsumed);
            return new AsnValueReader(content, _ruleSet);
        }

        internal AsnValueReader ReadSetOf(Asn1Tag? expectedTag = default, bool skipSortOrderValidation = false)
        {
            AsnDecoder.ReadSetOf(
                _span,
                _ruleSet,
                out int contentOffset,
                out int contentLength,
                out int bytesConsumed,
                skipSortOrderValidation: skipSortOrderValidation,
                expectedTag: expectedTag);

            ReadOnlySpan<byte> content = _span.Slice(contentOffset, contentLength);
            _span = _span.Slice(bytesConsumed);
            return new AsnValueReader(content, _ruleSet);
        }

        internal DateTimeOffset ReadUtcTime(Asn1Tag? expectedTag = default)
        {
            DateTimeOffset ret = AsnDecoder.ReadUtcTime(_span, _ruleSet, out int consumed, expectedTag: expectedTag);
            _span = _span.Slice(consumed);
            return ret;
        }

        internal DateTimeOffset ReadGeneralizedTime(Asn1Tag? expectedTag = default)
        {
            DateTimeOffset ret = AsnDecoder.ReadGeneralizedTime(_span, _ruleSet, out int consumed, expectedTag);
            _span = _span.Slice(consumed);
            return ret;
        }

        internal string ReadCharacterString(UniversalTagNumber encodingType, Asn1Tag? expectedTag = default)
        {
            string ret = AsnDecoder.ReadCharacterString(_span, _ruleSet, encodingType, out int consumed, expectedTag);
            _span = _span.Slice(consumed);
            return ret;
        }

        internal TEnum ReadEnumeratedValue<TEnum>(Asn1Tag? expectedTag = null) where TEnum : Enum
        {
            TEnum ret = AsnDecoder.ReadEnumeratedValue<TEnum>(_span, _ruleSet, out int consumed, expectedTag);
            _span = _span.Slice(consumed);
            return ret;
        }
    }

    internal static class AsnWriterExtensions
    {
        internal static void WriteEncodedValueForCrypto(
            this AsnWriter writer,
            ReadOnlySpan<byte> value)
        {
            try
            {
                writer.WriteEncodedValue(value);
            }
            catch (ArgumentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void WriteObjectIdentifierForCrypto(
            this AsnWriter writer,
            string value)
        {
            try
            {
                writer.WriteObjectIdentifier(value);
            }
            catch (ArgumentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static ArraySegment<byte> RentAndEncode(this AsnWriter writer)
        {
            byte[] rented = CryptoPool.Rent(writer.GetEncodedLength());
            int written = writer.Encode(rented);
            return new ArraySegment<byte>(rented, 0, written);
        }
    }
}
