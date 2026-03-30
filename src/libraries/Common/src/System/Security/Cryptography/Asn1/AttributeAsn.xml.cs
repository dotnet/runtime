// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct AttributeAsn
    {
        internal string AttrType;
        internal ReadOnlyMemory<byte>[] AttrValues;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);

            try
            {
                writer.WriteObjectIdentifier(AttrType);
            }
            catch (ArgumentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }

            writer.PushSetOf();
            for (int i = 0; i < AttrValues.Length; i++)
            {
                try
                {
                    writer.WriteEncodedValue(AttrValues[i].Span);
                }
                catch (ArgumentException e)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
                }
            }
            writer.PopSetOf();

            writer.PopSequence(tag);
        }

        internal static AttributeAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static AttributeAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, encoded, out AttributeAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref ValueAsnReader reader, ReadOnlyMemory<byte> rebind, out AttributeAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out AttributeAsn decoded)
        {
            try
            {
                DecodeCore(ref reader, expectedTag, rebind, out decoded);
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        private static void DecodeCore(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out AttributeAsn decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ValueAsnReader collectionReader;
            ReadOnlySpan<byte> rebindSpan = rebind.Span;
            int offset;
            ReadOnlySpan<byte> tmpSpan;

            decoded.AttrType = sequenceReader.ReadObjectIdentifier();

            // Decode SEQUENCE OF for AttrValues
            {
                collectionReader = sequenceReader.ReadSetOf();
                var tmpList = new List<ReadOnlyMemory<byte>>();
                ReadOnlyMemory<byte> tmpItem;

                while (collectionReader.HasData)
                {
                    tmpSpan = collectionReader.ReadEncodedValue();
                    tmpItem = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
                    tmpList.Add(tmpItem);
                }

                decoded.AttrValues = tmpList.ToArray();
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref partial struct ValueAttributeAsn
    {
        internal string AttrType;
        internal ReadOnlySpan<byte> AttrValues { get; private set; }
        internal int AttrValuesLength { get; private set; }
        private AttrValuesEnumerableCache AttrValuesCache;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);

            try
            {
                writer.WriteObjectIdentifier(AttrType);
            }
            catch (ArgumentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }

            try
            {
                writer.WriteEncodedValue(AttrValues);
            }
            catch (ArgumentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
            writer.PopSequence(tag);
        }

        internal static void Decode(ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueAttributeAsn decoded)
        {
            Decode(Asn1Tag.Sequence, encoded, ruleSet, out decoded);
        }

        internal static void Decode(Asn1Tag expectedTag, ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueAttributeAsn decoded)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(encoded, ruleSet);

                DecodeCore(ref reader, expectedTag, out decoded);
                reader.ThrowIfNotEmpty();
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(scoped ref ValueAsnReader reader, out ValueAttributeAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueAttributeAsn decoded)
        {
            try
            {
                DecodeCore(ref reader, expectedTag, out decoded);
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        private static void DecodeCore(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueAttributeAsn decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);

            decoded.AttrType = sequenceReader.ReadObjectIdentifier();

            if (!sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.SetOf))
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            decoded.AttrValues = sequenceReader.ReadEncodedValue();
            decoded.AttrValuesCache.RuleSet = sequenceReader.RuleSet;
            decoded.InitializeAndValidateAttrValues();

            sequenceReader.ThrowIfNotEmpty();
        }


        internal readonly AttrValuesEnumerable GetAttrValues()
        {
            return new AttrValuesEnumerable(AttrValues, AttrValuesCache);
        }

        private void InitializeAndValidateAttrValues()
        {
            int count = 0;
            ValueAsnReader reader = new ValueAsnReader(AttrValues, AttrValuesCache.RuleSet);
            ValueAsnReader collReader = reader.ReadSetOf();
            reader.ThrowIfNotEmpty();

            while (collReader.HasData)
            {
                checked { count++; }
                ReadOnlySpan<byte> item = collReader.ReadEncodedValue();

                switch (count)
                {
                    case 1: AttrValuesCache.AttrValues1 = item; break;
                    case 2: AttrValuesCache.AttrValues2 = item; break;
                    case 3: AttrValuesCache.AttrValues3 = item; break;
                    case 4: AttrValuesCache.AttrValues4 = item; break;
                    case 5: AttrValuesCache.AttrValues5 = item; break;
                    case 6: AttrValuesCache.AttrValues6 = item; break;
                    case 7: AttrValuesCache.AttrValues7 = item; break;
                    case 8: AttrValuesCache.AttrValues8 = item; break;
                    case 9: AttrValuesCache.AttrValues9 = item; break;
                    case 10: AttrValuesCache.AttrValues10 = item; break;
                    default: break;
                }
            }

            AttrValuesCache.Count = count <= 10 ? count : null;
            AttrValuesLength = count;
        }

        internal ref struct AttrValuesEnumerableCache
        {
            internal ReadOnlySpan<byte> AttrValues1;
            internal ReadOnlySpan<byte> AttrValues2;
            internal ReadOnlySpan<byte> AttrValues3;
            internal ReadOnlySpan<byte> AttrValues4;
            internal ReadOnlySpan<byte> AttrValues5;
            internal ReadOnlySpan<byte> AttrValues6;
            internal ReadOnlySpan<byte> AttrValues7;
            internal ReadOnlySpan<byte> AttrValues8;
            internal ReadOnlySpan<byte> AttrValues9;
            internal ReadOnlySpan<byte> AttrValues10;
            internal int? Count;
            internal AsnEncodingRules RuleSet;
        }

        internal readonly ref struct AttrValuesEnumerable
        {
            private readonly ReadOnlySpan<byte> _encoded;
            private readonly AttrValuesEnumerableCache _cache;

            internal AttrValuesEnumerable(ReadOnlySpan<byte> encoded, AttrValuesEnumerableCache cache)
            {
                _encoded = encoded;
                _cache = cache;
            }

            public Enumerator GetEnumerator() => new Enumerator(_encoded, _cache);

            internal ref struct Enumerator
            {
                private ValueAsnReader _reader;
                private ReadOnlySpan<byte> _current;
                private readonly AttrValuesEnumerableCache _cache;
                private int _index;

                internal Enumerator(ReadOnlySpan<byte> encoded, AttrValuesEnumerableCache cache)
                {
                    _cache = cache;
                    _index = 0;

                    if (!cache.Count.HasValue && !encoded.IsEmpty)
                    {
                        ValueAsnReader outerReader = new ValueAsnReader(encoded, cache.RuleSet);
                        _reader = outerReader.ReadSetOf();
                        outerReader.ThrowIfNotEmpty();
                    }

                    _current = default;
                }

                public ReadOnlySpan<byte> Current => _current;

                public bool MoveNext()
                {
                    if (_cache.Count.HasValue)
                    {
                        if (_index >= _cache.Count.Value)
                        {
                            return false;
                        }

                        _current = _index switch
                        {
                            0 => _cache.AttrValues1,
                            1 => _cache.AttrValues2,
                            2 => _cache.AttrValues3,
                            3 => _cache.AttrValues4,
                            4 => _cache.AttrValues5,
                            5 => _cache.AttrValues6,
                            6 => _cache.AttrValues7,
                            7 => _cache.AttrValues8,
                            8 => _cache.AttrValues9,
                            9 => _cache.AttrValues10,
                            _ => default,
                        };
                        _index++;

                        return true;
                    }

                    if (!_reader.HasData)
                    {
                        return false;
                    }

                    _current = _reader.ReadEncodedValue();
                    return true;
                }
            }
        }
    }
}
