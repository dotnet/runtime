// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct SignerInfoAsn
    {
        internal int Version;
        internal System.Security.Cryptography.Pkcs.Asn1.SignerIdentifierAsn Sid;
        internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn DigestAlgorithm;
        internal ReadOnlyMemory<byte>? SignedAttributes;
        internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn SignatureAlgorithm;
        internal ReadOnlyMemory<byte> SignatureValue;
        internal System.Security.Cryptography.Asn1.AttributeAsn[]? UnsignedAttributes;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);

            writer.WriteInteger(Version);
            Sid.Encode(writer);
            DigestAlgorithm.Encode(writer);

            if (SignedAttributes.HasValue)
            {
                // Validator for tag constraint for SignedAttributes
                {
                    if (!Asn1Tag.TryDecode(SignedAttributes.Value.Span, out Asn1Tag validateTag, out _) ||
                        !validateTag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
                    {
                        throw new CryptographicException();
                    }
                }

                try
                {
                    writer.WriteEncodedValue(SignedAttributes.Value.Span);
                }
                catch (ArgumentException e)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
                }
            }

            SignatureAlgorithm.Encode(writer);
            writer.WriteOctetString(SignatureValue.Span);

            if (UnsignedAttributes != null)
            {

                writer.PushSetOf(new Asn1Tag(TagClass.ContextSpecific, 1));
                for (int i = 0; i < UnsignedAttributes.Length; i++)
                {
                    UnsignedAttributes[i].Encode(writer);
                }
                writer.PopSetOf(new Asn1Tag(TagClass.ContextSpecific, 1));

            }

            writer.PopSequence(tag);
        }

        internal static SignerInfoAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static SignerInfoAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, encoded, out SignerInfoAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out SignerInfoAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out SignerInfoAsn decoded)
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

        private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out SignerInfoAsn decoded)
        {
            decoded = default;
            AsnValueReader sequenceReader = reader.ReadSequence(expectedTag);
            AsnValueReader collectionReader;
            ReadOnlySpan<byte> rebindSpan = rebind.Span;
            int offset;
            ReadOnlySpan<byte> tmpSpan;


            if (!sequenceReader.TryReadInt32(out decoded.Version))
            {
                sequenceReader.ThrowIfNotEmpty();
            }

            System.Security.Cryptography.Pkcs.Asn1.SignerIdentifierAsn.Decode(ref sequenceReader, rebind, out decoded.Sid);
            System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref sequenceReader, rebind, out decoded.DigestAlgorithm);

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                tmpSpan = sequenceReader.ReadEncodedValue();
                decoded.SignedAttributes = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            }

            System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref sequenceReader, rebind, out decoded.SignatureAlgorithm);

            if (sequenceReader.TryReadPrimitiveOctetString(out tmpSpan))
            {
                decoded.SignatureValue = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            }
            else
            {
                decoded.SignatureValue = sequenceReader.ReadOctetString();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {

                // Decode SEQUENCE OF for UnsignedAttributes
                {
                    collectionReader = sequenceReader.ReadSetOf(new Asn1Tag(TagClass.ContextSpecific, 1));
                    var tmpList = new List<System.Security.Cryptography.Asn1.AttributeAsn>();
                    System.Security.Cryptography.Asn1.AttributeAsn tmpItem;

                    while (collectionReader.HasData)
                    {
                        System.Security.Cryptography.Asn1.AttributeAsn.Decode(ref collectionReader, rebind, out tmpItem);
                        tmpList.Add(tmpItem);
                    }

                    decoded.UnsignedAttributes = tmpList.ToArray();
                }

            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
