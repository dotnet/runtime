// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.X509Certificates.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct CertificationRequestInfoAsn
    {
        internal System.Numerics.BigInteger Version;
        internal ReadOnlyMemory<byte> Subject;
        internal System.Security.Cryptography.Asn1.SubjectPublicKeyInfoAsn SubjectPublicKeyInfo;
        internal System.Security.Cryptography.Asn1.AttributeAsn[] Attributes;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);

            writer.WriteInteger(Version);
            // Validator for tag constraint for Subject
            {
                if (!Asn1Tag.TryDecode(Subject.Span, out Asn1Tag validateTag, out _) ||
                    !validateTag.HasSameClassAndValue(new Asn1Tag((UniversalTagNumber)16)))
                {
                    throw new CryptographicException();
                }
            }

            try
            {
                writer.WriteEncodedValue(Subject.Span);
            }
            catch (ArgumentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
            SubjectPublicKeyInfo.Encode(writer);

            writer.PushSetOf(new Asn1Tag(TagClass.ContextSpecific, 0));
            for (int i = 0; i < Attributes.Length; i++)
            {
                Attributes[i].Encode(writer);
            }
            writer.PopSetOf(new Asn1Tag(TagClass.ContextSpecific, 0));

            writer.PopSequence(tag);
        }

        internal static CertificationRequestInfoAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static CertificationRequestInfoAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, encoded, out CertificationRequestInfoAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out CertificationRequestInfoAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out CertificationRequestInfoAsn decoded)
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

        private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out CertificationRequestInfoAsn decoded)
        {
            decoded = default;
            AsnValueReader sequenceReader = reader.ReadSequence(expectedTag);
            AsnValueReader collectionReader;
            ReadOnlySpan<byte> rebindSpan = rebind.Span;
            int offset;
            ReadOnlySpan<byte> tmpSpan;

            decoded.Version = sequenceReader.ReadInteger();
            if (!sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag((UniversalTagNumber)16)))
            {
                throw new CryptographicException();
            }

            tmpSpan = sequenceReader.ReadEncodedValue();
            decoded.Subject = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            System.Security.Cryptography.Asn1.SubjectPublicKeyInfoAsn.Decode(ref sequenceReader, rebind, out decoded.SubjectPublicKeyInfo);

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


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
