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
            set
            {
                HasAttributes = true;
                field = value;
            }
        }

        internal bool HasAttributes { get; private set; }

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

        internal static void Decode(ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValuePrivateKeyInfoAsn decoded)
        {
            Decode(Asn1Tag.Sequence, encoded, ruleSet, out decoded);
        }

        internal static void Decode(Asn1Tag expectedTag, ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValuePrivateKeyInfoAsn decoded)
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

        internal static void Decode(scoped ref ValueAsnReader reader, out ValuePrivateKeyInfoAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValuePrivateKeyInfoAsn decoded)
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

        private static void DecodeCore(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValuePrivateKeyInfoAsn decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ReadOnlySpan<byte> tmpSpan;


            if (!sequenceReader.TryReadInt32(out decoded.Version))
            {
                sequenceReader.ThrowIfNotEmpty();
            }

            System.Security.Cryptography.Asn1.ValueAlgorithmIdentifierAsn.Decode(ref sequenceReader, out decoded.PrivateKeyAlgorithm);

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
                decoded.HasAttributes = true;
            }


            sequenceReader.ThrowIfNotEmpty();
        }


        internal AttributesEnumerable GetAttributes(AsnEncodingRules ruleSet)
        {
            return new AttributesEnumerable(Attributes, ruleSet);
        }

        internal readonly ref struct AttributesEnumerable
        {
            private readonly ReadOnlySpan<byte> _encoded;
            private readonly AsnEncodingRules _ruleSet;

            internal AttributesEnumerable(ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet)
            {
                _encoded = encoded;
                _ruleSet = ruleSet;
            }

            public Enumerator GetEnumerator() => new Enumerator(_encoded, _ruleSet);

            internal ref struct Enumerator
            {
                private ValueAsnReader _reader;
                private System.Security.Cryptography.Asn1.ValueAttributeAsn _current;

                internal Enumerator(ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet)
                {
                    if (!encoded.IsEmpty)
                    {
                        ValueAsnReader outerReader = new ValueAsnReader(encoded, ruleSet);
                        _reader = outerReader.ReadSetOf(new Asn1Tag(TagClass.ContextSpecific, 0));
                        outerReader.ThrowIfNotEmpty();
                    }

                    _current = default;
                }

                public System.Security.Cryptography.Asn1.ValueAttributeAsn Current => _current;

                public bool MoveNext()
                {
                    if (!_reader.HasData)
                    {
                        return false;
                    }

                    System.Security.Cryptography.Asn1.ValueAttributeAsn.Decode(ref _reader, out _current);
                    return true;
                }
            }
        }
    }
}
