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
    internal partial struct PrivateKeyInfoAsn
    {
        internal int Version;
        internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn PrivateKeyAlgorithm;
        internal ReadOnlyMemory<byte> PrivateKey;
        internal System.Security.Cryptography.Asn1.AttributeAsn[]? Attributes;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);

            writer.WriteInteger(Version);
            PrivateKeyAlgorithm.Encode(writer);
            writer.WriteOctetString(PrivateKey.Span);

            if (Attributes != null)
            {

                writer.PushSetOf(new Asn1Tag(TagClass.ContextSpecific, 0));
                for (int i = 0; i < Attributes.Length; i++)
                {
                    Attributes[i].Encode(writer);
                }
                writer.PopSetOf(new Asn1Tag(TagClass.ContextSpecific, 0));

            }

            writer.PopSequence(tag);
        }

        internal static PrivateKeyInfoAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static PrivateKeyInfoAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, encoded, out PrivateKeyInfoAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref ValueAsnReader reader, ReadOnlyMemory<byte> rebind, out PrivateKeyInfoAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out PrivateKeyInfoAsn decoded)
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

        private static void DecodeCore(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out PrivateKeyInfoAsn decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ValueAsnReader collectionReader;
            ReadOnlySpan<byte> rebindSpan = rebind.Span;
            int offset;
            ReadOnlySpan<byte> tmpSpan;


            if (!sequenceReader.TryReadInt32(out decoded.Version))
            {
                sequenceReader.ThrowIfNotEmpty();
            }

            System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref sequenceReader, rebind, out decoded.PrivateKeyAlgorithm);

            if (sequenceReader.TryReadPrimitiveOctetString(out tmpSpan))
            {
                decoded.PrivateKey = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            }
            else
            {
                decoded.PrivateKey = sequenceReader.ReadOctetString();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {

                // Decode SEQUENCE OF for Attributes
                {
                    collectionReader = sequenceReader.ReadSetOf(new Asn1Tag(TagClass.ContextSpecific, 0));
                    var tmpList = new List<System.Security.Cryptography.Asn1.AttributeAsn>();
                    System.Security.Cryptography.Asn1.AttributeAsn tmpItem;

                    while (collectionReader.HasData)
                    {
                        System.Security.Cryptography.Asn1.AttributeAsn.Decode(ref collectionReader, rebind, out tmpItem);
                        tmpList.Add(tmpItem);
                    }

                    decoded.Attributes = tmpList.ToArray();
                }

            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref partial struct ValuePrivateKeyInfoAsn
    {
        internal int Version;
        internal System.Security.Cryptography.Asn1.ValueAlgorithmIdentifierAsn PrivateKeyAlgorithm;
        internal ReadOnlySpan<byte> PrivateKey;

        internal ReadOnlySpan<byte> Attributes
        {
            get;
            private set
            {
                HasAttributes = true;
                field = value;
            }
        }

        internal bool HasAttributes { get; private set; }
        internal int AttributesLength { get; private set; }
        private AttributesEnumerableCache _attributesCache;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);

            writer.WriteInteger(Version);
            PrivateKeyAlgorithm.Encode(writer);
            writer.WriteOctetString(PrivateKey);

            if (HasAttributes)
            {

                try
                {
                    writer.WriteEncodedValue(Attributes);
                }
                catch (ArgumentException e)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
                }
            }

            writer.PopSequence(tag);
        }

        internal static ValuePrivateKeyInfoAsn Decode(ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static ValuePrivateKeyInfoAsn Decode(Asn1Tag expectedTag, ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(encoded, ruleSet);

                ValuePrivateKeyInfoAsn decoded = DecodeCore(ref reader, expectedTag);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static ValuePrivateKeyInfoAsn Decode(scoped ref ValueAsnReader reader)
        {
            return Decode(ref reader, Asn1Tag.Sequence);
        }

        internal static ValuePrivateKeyInfoAsn Decode(scoped ref ValueAsnReader reader, Asn1Tag expectedTag)
        {
            try
            {
                return DecodeCore(ref reader, expectedTag);
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        private static ValuePrivateKeyInfoAsn DecodeCore(scoped ref ValueAsnReader reader, Asn1Tag expectedTag)
        {
            ValuePrivateKeyInfoAsn decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ReadOnlySpan<byte> tmpSpan;


            if (!sequenceReader.TryReadInt32(out decoded.Version))
            {
                sequenceReader.ThrowIfNotEmpty();
            }

            decoded.PrivateKeyAlgorithm = System.Security.Cryptography.Asn1.ValueAlgorithmIdentifierAsn.Decode(ref sequenceReader);

            if (sequenceReader.TryReadPrimitiveOctetString(out tmpSpan))
            {
                decoded.PrivateKey = tmpSpan;
            }
            else
            {
                decoded.PrivateKey = sequenceReader.ReadOctetString();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                decoded.Attributes = sequenceReader.ReadEncodedValue();
                {
                    int count = 0;
                    ValueAsnReader cacheReader = new ValueAsnReader(decoded.Attributes, sequenceReader.RuleSet);
                    ValueAsnReader collReader = cacheReader.ReadSetOf(new Asn1Tag(TagClass.ContextSpecific, 0));
                    cacheReader.ThrowIfNotEmpty();

                    while (collReader.HasData)
                    {
                        checked { count++; }
                        System.Security.Cryptography.Asn1.ValueAttributeAsn item = System.Security.Cryptography.Asn1.ValueAttributeAsn.Decode(ref collReader);

                        if (count <= 10)
                        {
                            switch (count)
                            {
                                case 1: decoded._attributesCache.Attributes1 = item; break;
                                case 2: decoded._attributesCache.Attributes2 = item; break;
                                case 3: decoded._attributesCache.Attributes3 = item; break;
                                case 4: decoded._attributesCache.Attributes4 = item; break;
                                case 5: decoded._attributesCache.Attributes5 = item; break;
                                case 6: decoded._attributesCache.Attributes6 = item; break;
                                case 7: decoded._attributesCache.Attributes7 = item; break;
                                case 8: decoded._attributesCache.Attributes8 = item; break;
                                case 9: decoded._attributesCache.Attributes9 = item; break;
                                case 10: decoded._attributesCache.Attributes10 = item; break;
                            }
                        }
                    }

                    if (count <= 10)
                    {
                        decoded._attributesCache.Success = true;
                    }
                    decoded._attributesCache.Count = count;
                    decoded._attributesCache.RuleSet = sequenceReader.RuleSet;
                    decoded.AttributesLength = count;
                }
                decoded.HasAttributes = true;
            }


            sequenceReader.ThrowIfNotEmpty();
            return decoded;
        }


        internal AttributesEnumerable GetAttributes()
        {
            return new AttributesEnumerable(Attributes, _attributesCache);
        }

        internal ref struct AttributesEnumerableCache
        {
            internal System.Security.Cryptography.Asn1.ValueAttributeAsn Attributes1;
            internal System.Security.Cryptography.Asn1.ValueAttributeAsn Attributes2;
            internal System.Security.Cryptography.Asn1.ValueAttributeAsn Attributes3;
            internal System.Security.Cryptography.Asn1.ValueAttributeAsn Attributes4;
            internal System.Security.Cryptography.Asn1.ValueAttributeAsn Attributes5;
            internal System.Security.Cryptography.Asn1.ValueAttributeAsn Attributes6;
            internal System.Security.Cryptography.Asn1.ValueAttributeAsn Attributes7;
            internal System.Security.Cryptography.Asn1.ValueAttributeAsn Attributes8;
            internal System.Security.Cryptography.Asn1.ValueAttributeAsn Attributes9;
            internal System.Security.Cryptography.Asn1.ValueAttributeAsn Attributes10;
            internal int Count;
            internal bool Success;
            internal AsnEncodingRules RuleSet;
        }

        internal readonly ref struct AttributesEnumerable
        {
            private readonly ReadOnlySpan<byte> _encoded;
            private readonly AttributesEnumerableCache _cache;

            internal AttributesEnumerable(ReadOnlySpan<byte> encoded, AttributesEnumerableCache cache)
            {
                _encoded = encoded;
                _cache = cache;
            }

            public Enumerator GetEnumerator() => new Enumerator(_encoded, _cache);

            internal ref struct Enumerator
            {
                private ValueAsnReader _reader;
                private System.Security.Cryptography.Asn1.ValueAttributeAsn _current;
                private readonly AttributesEnumerableCache _cache;
                private int _index;

                internal Enumerator(ReadOnlySpan<byte> encoded, AttributesEnumerableCache cache)
                {
                    _cache = cache;
                    _index = 0;

                    if (!cache.Success && !encoded.IsEmpty)
                    {
                        ValueAsnReader outerReader = new ValueAsnReader(encoded, cache.RuleSet);
                        _reader = outerReader.ReadSetOf(new Asn1Tag(TagClass.ContextSpecific, 0));
                        outerReader.ThrowIfNotEmpty();
                    }

                    _current = default;
                }

                public System.Security.Cryptography.Asn1.ValueAttributeAsn Current => _current;

                public bool MoveNext()
                {
                    if (_cache.Success)
                    {
                        if (_index >= _cache.Count)
                        {
                            return false;
                        }

                        _current = _index switch
                        {
                            0 => _cache.Attributes1,
                            1 => _cache.Attributes2,
                            2 => _cache.Attributes3,
                            3 => _cache.Attributes4,
                            4 => _cache.Attributes5,
                            5 => _cache.Attributes6,
                            6 => _cache.Attributes7,
                            7 => _cache.Attributes8,
                            8 => _cache.Attributes9,
                            9 => _cache.Attributes10,
                            _ => default,
                        };
                        _index++;

                        return true;
                    }

                    if (!_reader.HasData)
                    {
                        return false;
                    }

                    _current = System.Security.Cryptography.Asn1.ValueAttributeAsn.Decode(ref _reader);
                    return true;
                }
            }
        }
    }
}
