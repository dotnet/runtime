// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Test.Cryptography;
using Xunit;

namespace System.Formats.Asn1.Tests.Reader
{
    internal ref struct AsnReaderWrapper
    {
        private AsnReader _classReader;
        private ValueAsnReader _valueReader;

        internal static AsnReaderWrapper CreateClassReader(
            ReadOnlyMemory<byte> data,
            AsnEncodingRules ruleSet,
            AsnReaderOptions options)
        {
            AsnReaderWrapper wrapper = new();
            wrapper._classReader = new AsnReader(data, ruleSet, options);
            return wrapper;
        }

        internal static AsnReaderWrapper CreateValueReader(
            ReadOnlyMemory<byte> data,
            AsnEncodingRules ruleSet,
            AsnReaderOptions options)
        {
            AsnReaderWrapper wrapper = new();
            wrapper._valueReader = new ValueAsnReader(data.Span, ruleSet, options);
            return wrapper;
        }

        private static AsnReaderWrapper FromClassReader(AsnReader reader)
        {
            AsnReaderWrapper wrapper = new();
            wrapper._classReader = reader;
            return wrapper;
        }

        private static AsnReaderWrapper FromValueReader(ValueAsnReader reader)
        {
            AsnReaderWrapper wrapper = new();
            wrapper._valueReader = reader;
            return wrapper;
        }

        internal void ReadNull(Asn1Tag? expectedTag = default)
        {
            if (_classReader is not null)
            {
                _classReader.ReadNull(expectedTag);
            }
            else
            {
                _valueReader.ReadNull(expectedTag);
            }
        }

        internal bool HasData
        {
            get
            {
                if (_classReader is not null)
                {
                    return _classReader.HasData;
                }
                else
                {
                    return _valueReader.HasData;
                }
            }
        }

        internal AsnEncodingRules RuleSet
        {
            get
            {
                if (_classReader is not null)
                {
                    return _classReader.RuleSet;
                }
                else
                {
                    return _valueReader.RuleSet;
                }
            }
        }

        // For the ValueAsnReader, return-by-value is effectively a copy (clone).
        internal AsnReaderWrapper Clone()
        {
            if (_classReader is not null)
            {
                return FromClassReader(_classReader.Clone());
            }
            else
            {
                return this;
            }
        }

        internal Asn1Tag PeekTag()
        {
            if (_classReader is not null)
            {
                return _classReader.PeekTag();
            }
            else
            {
                return _valueReader.PeekTag();
            }
        }

        internal bool ReadBoolean(Asn1Tag? expectedTag = default)
        {
            if (_classReader is not null)
            {
                return _classReader.ReadBoolean(expectedTag);
            }
            else
            {
                return _valueReader.ReadBoolean(expectedTag);
            }
        }

        internal string ReadObjectIdentifier(Asn1Tag? expectedTag = default)
        {
            if (_classReader is not null)
            {
                return _classReader.ReadObjectIdentifier(expectedTag);
            }
            else
            {
                return _valueReader.ReadObjectIdentifier(expectedTag);
            }
        }

        internal DateTimeOffset ReadUtcTime(Asn1Tag? expectedTag = default)
        {
            if (_classReader is not null)
            {
                return _classReader.ReadUtcTime(expectedTag);
            }
            else
            {
                return _valueReader.ReadUtcTime(expectedTag);
            }
        }

        internal DateTimeOffset ReadUtcTime(int twoDigitYearMax, Asn1Tag? expectedTag = default)
        {
            if (_classReader is not null)
            {
                return _classReader.ReadUtcTime(twoDigitYearMax, expectedTag);
            }
            else
            {
                return _valueReader.ReadUtcTime(twoDigitYearMax, expectedTag);
            }
        }

        internal DateTimeOffset ReadGeneralizedTime(Asn1Tag? expectedTag = default)
        {
            if (_classReader is not null)
            {
                return _classReader.ReadGeneralizedTime(expectedTag);
            }
            else
            {
                return _valueReader.ReadGeneralizedTime(expectedTag);
            }
        }

        internal bool TryReadPrimitiveBitString(
            out int unusedBitCount,
            out ReadOnlySpan<byte> value,
            Asn1Tag? expectedTag = default)
        {
            if (_classReader is not null)
            {
                bool result = _classReader.TryReadPrimitiveBitString(out unusedBitCount, out ReadOnlyMemory<byte> memory, expectedTag);
                value = memory.Span;
                return result;
            }
            else
            {
                return _valueReader.TryReadPrimitiveBitString(out unusedBitCount, out value, expectedTag);
            }
        }

        internal bool TryReadBitString(
            Span<byte> destination,
            out int unusedBitCount,
            out int bytesWritten,
            Asn1Tag? expectedTag = default)
        {
            if (_classReader is not null)
            {
                return _classReader.TryReadBitString(destination, out unusedBitCount, out bytesWritten, expectedTag);
            }
            else
            {
                return _valueReader.TryReadBitString(destination, out unusedBitCount, out bytesWritten, expectedTag);
            }
        }

        internal byte[] ReadBitString(out int unusedBitCount, Asn1Tag? expectedTag = default)
        {
            if (_classReader is not null)
            {
                return _classReader.ReadBitString(out unusedBitCount, expectedTag);
            }
            else
            {
                return _valueReader.ReadBitString(out unusedBitCount, expectedTag);
            }
        }

        internal string ReadCharacterString(UniversalTagNumber encodingType, Asn1Tag? expectedTag = default)
        {
            if (_classReader is not null)
            {
                return _classReader.ReadCharacterString(encodingType, expectedTag);
            }
            else
            {
                return _valueReader.ReadCharacterString(encodingType, expectedTag);
            }
        }

        internal bool TryReadCharacterString(
            Span<char> destination,
            UniversalTagNumber encodingType,
            out int charsWritten,
            Asn1Tag? expectedTag = default)
        {
            if (_classReader is not null)
            {
                return _classReader.TryReadCharacterString(destination, encodingType, out charsWritten, expectedTag);
            }
            else
            {
                return _valueReader.TryReadCharacterString(destination, encodingType, out charsWritten, expectedTag);
            }
        }

        internal bool TryReadCharacterStringBytes(
            Span<byte> destination,
            Asn1Tag expectedTag,
            out int bytesWritten)
        {
            if (_classReader is not null)
            {
                return _classReader.TryReadCharacterStringBytes(destination, expectedTag, out bytesWritten);
            }
            else
            {
                return _valueReader.TryReadCharacterStringBytes(destination, expectedTag, out bytesWritten);
            }
        }

        internal bool TryReadPrimitiveCharacterStringBytes(
            Asn1Tag expectedTag,
            out ReadOnlySpan<byte> value)
        {
            if (_classReader is not null)
            {
                bool result = _classReader.TryReadPrimitiveCharacterStringBytes(expectedTag, out ReadOnlyMemory<byte> memory);
                value = memory.Span;
                return result;
            }
            else
            {
                return _valueReader.TryReadPrimitiveCharacterStringBytes(expectedTag, out value);
            }
        }

        internal ReadOnlySpan<byte> PeekEncodedValue()
        {
            if (_classReader is not null)
            {
                return _classReader.PeekEncodedValue().Span;
            }
            else
            {
                return _valueReader.PeekEncodedValue();
            }
        }

        internal ReadOnlySpan<byte> PeekContentBytes()
        {
            if (_classReader is not null)
            {
                return _classReader.PeekContentBytes().Span;
            }
            else
            {
                return _valueReader.PeekContentBytes();
            }
        }

        internal ReadOnlySpan<byte> ReadEncodedValue()
        {
            if (_classReader is not null)
            {
                return _classReader.ReadEncodedValue().Span;
            }
            else
            {
                return _valueReader.ReadEncodedValue();
            }
        }

        internal void ThrowIfNotEmpty()
        {
            if (_classReader is not null)
            {
                _classReader.ThrowIfNotEmpty();
            }
            else
            {
                _valueReader.ThrowIfNotEmpty();
            }
        }

        internal TEnum ReadEnumeratedValue<TEnum>(Asn1Tag? expectedTag = default) where TEnum : Enum
        {
            if (_classReader is not null)
            {
                return _classReader.ReadEnumeratedValue<TEnum>(expectedTag);
            }
            else
            {
                return _valueReader.ReadEnumeratedValue<TEnum>(expectedTag);
            }
        }

        internal Enum ReadEnumeratedValue(Type enumType, Asn1Tag? expectedTag = default)
        {
            if (_classReader is not null)
            {
                return _classReader.ReadEnumeratedValue(enumType, expectedTag);
            }
            else
            {
                return _valueReader.ReadEnumeratedValue(enumType, expectedTag);
            }
        }

        internal ReadOnlySpan<byte> ReadEnumeratedBytes(Asn1Tag? expectedTag = default)
        {
            if (_classReader is not null)
            {
                return _classReader.ReadEnumeratedBytes(expectedTag).Span;
            }
            else
            {
                return _valueReader.ReadEnumeratedBytes(expectedTag);
            }
        }

        internal ReadOnlySpan<byte> ReadIntegerBytes(Asn1Tag? expectedTag = default)
        {
            if (_classReader is not null)
            {
                return _classReader.ReadIntegerBytes(expectedTag).Span;
            }
            else
            {
                return _valueReader.ReadIntegerBytes(expectedTag);
            }
        }

        internal BigInteger ReadInteger(Asn1Tag? expectedTag = default)
        {
            if (_classReader is not null)
            {
                return _classReader.ReadInteger(expectedTag);
            }
            else
            {
                return _valueReader.ReadInteger(expectedTag);
            }
        }

        internal bool TryReadInt32(out int value, Asn1Tag? expectedTag = default)
        {
            if (_classReader is not null)
            {
                return _classReader.TryReadInt32(out value, expectedTag);
            }
            else
            {
                return _valueReader.TryReadInt32(out value, expectedTag);
            }
        }

        internal bool TryReadUInt32(out uint value, Asn1Tag? expectedTag = default)
        {
            if (_classReader is not null)
            {
                return _classReader.TryReadUInt32(out value, expectedTag);
            }
            else
            {
                return _valueReader.TryReadUInt32(out value, expectedTag);
            }
        }

        internal bool TryReadInt64(out long value, Asn1Tag? expectedTag = default)
        {
            if (_classReader is not null)
            {
                return _classReader.TryReadInt64(out value, expectedTag);
            }
            else
            {
                return _valueReader.TryReadInt64(out value, expectedTag);
            }
        }

        internal bool TryReadUInt64(out ulong value, Asn1Tag? expectedTag = default)
        {
            if (_classReader is not null)
            {
                return _classReader.TryReadUInt64(out value, expectedTag);
            }
            else
            {
                return _valueReader.TryReadUInt64(out value, expectedTag);
            }
        }

        internal bool TryReadPrimitiveOctetString(
            out ReadOnlySpan<byte> contents,
            Asn1Tag? expectedTag = default)
        {
            if (_classReader is not null)
            {
                bool result = _classReader.TryReadPrimitiveOctetString(out ReadOnlyMemory<byte> memory, expectedTag);
                contents = memory.Span;
                return result;
            }
            else
            {
                return _valueReader.TryReadPrimitiveOctetString(out contents, expectedTag);
            }
        }

        internal bool TryReadOctetString(
            Span<byte> destination,
            out int bytesWritten,
            Asn1Tag? expectedTag = default)
        {
            if (_classReader is not null)
            {
                return _classReader.TryReadOctetString(destination, out bytesWritten, expectedTag);
            }
            else
            {
                return _valueReader.TryReadOctetString(destination, out bytesWritten, expectedTag);
            }
        }

        internal byte[] ReadOctetString(Asn1Tag? expectedTag = default)
        {
            if (_classReader is not null)
            {
                return _classReader.ReadOctetString(expectedTag);
            }
            else
            {
                return _valueReader.ReadOctetString(expectedTag);
            }
        }

        internal AsnReaderWrapper ReadSequence(Asn1Tag? expectedTag = default)
        {
            if (_classReader is not null)
            {
                return FromClassReader(_classReader.ReadSequence(expectedTag));
            }
            else
            {
                return FromValueReader(_valueReader.ReadSequence(expectedTag));
            }
        }

        internal AsnReaderWrapper ReadSetOf(Asn1Tag? expectedTag = default)
        {
            if (_classReader is not null)
            {
                return FromClassReader(_classReader.ReadSetOf(expectedTag));
            }
            else
            {
                return FromValueReader(_valueReader.ReadSetOf(expectedTag));
            }
        }

        internal AsnReaderWrapper ReadSetOf(bool skipSortOrderValidation, Asn1Tag? expectedTag = default)
        {
            if (_classReader is not null)
            {
                return FromClassReader(_classReader.ReadSetOf(skipSortOrderValidation, expectedTag));
            }
            else
            {
                return FromValueReader(_valueReader.ReadSetOf(skipSortOrderValidation, expectedTag));
            }
        }

        internal TFlagsEnum ReadNamedBitListValue<TFlagsEnum>(Asn1Tag? expectedTag = default) where TFlagsEnum : Enum
        {
            if (_classReader is not null)
            {
                return _classReader.ReadNamedBitListValue<TFlagsEnum>(expectedTag);
            }
            else
            {
                return _valueReader.ReadNamedBitListValue<TFlagsEnum>(expectedTag);
            }
        }

        internal Enum ReadNamedBitListValue(Type flagsEnumType, Asn1Tag? expectedTag = default)
        {
            if (_classReader is not null)
            {
                return _classReader.ReadNamedBitListValue(flagsEnumType, expectedTag);
            }
            else
            {
                return _valueReader.ReadNamedBitListValue(flagsEnumType, expectedTag);
            }
        }

        internal BitArray ReadNamedBitList(Asn1Tag? expectedTag = default)
        {
            if (_classReader is not null)
            {
                return _classReader.ReadNamedBitList(expectedTag);
            }
            else
            {
                return _valueReader.ReadNamedBitList(expectedTag);
            }
        }
    }
}
