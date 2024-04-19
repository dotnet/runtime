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
    internal partial struct TbsCertificateAsn
    {
        private static ReadOnlySpan<byte> DefaultVersion => [0x02, 0x01, 0x00];

        internal int Version;
        internal ReadOnlyMemory<byte> SerialNumber;
        internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn SignatureAlgorithm;
        internal ReadOnlyMemory<byte> Issuer;
        internal System.Security.Cryptography.X509Certificates.Asn1.ValidityAsn Validity;
        internal ReadOnlyMemory<byte> Subject;
        internal System.Security.Cryptography.Asn1.SubjectPublicKeyInfoAsn SubjectPublicKeyInfo;
        internal ReadOnlyMemory<byte>? IssuerUniqueId;
        internal ReadOnlyMemory<byte>? SubjectUniqueId;
        internal System.Security.Cryptography.Asn1.X509ExtensionAsn[]? Extensions;

#if DEBUG
        static TbsCertificateAsn()
        {
            TbsCertificateAsn decoded = default;
            AsnValueReader reader;

            reader = new AsnValueReader(DefaultVersion, AsnEncodingRules.DER);

            if (!reader.TryReadInt32(out decoded.Version))
            {
                reader.ThrowIfNotEmpty();
            }

            reader.ThrowIfNotEmpty();
        }
#endif

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);


            // DEFAULT value handler for Version.
            {
                const int AsnManagedIntegerDerMaxEncodeSize = 6;
                AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER, initialCapacity: AsnManagedIntegerDerMaxEncodeSize);
                tmp.WriteInteger(Version);

                if (!tmp.EncodedValueEquals(DefaultVersion))
                {
                    writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                    tmp.CopyTo(writer);
                    writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                }
            }

            writer.WriteInteger(SerialNumber.Span);
            SignatureAlgorithm.Encode(writer);
            // Validator for tag constraint for Issuer
            {
                if (!Asn1Tag.TryDecode(Issuer.Span, out Asn1Tag validateTag, out _) ||
                    !validateTag.HasSameClassAndValue(new Asn1Tag((UniversalTagNumber)16)))
                {
                    throw new CryptographicException();
                }
            }

            try
            {
                writer.WriteEncodedValue(Issuer.Span);
            }
            catch (ArgumentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
            Validity.Encode(writer);
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

            if (IssuerUniqueId.HasValue)
            {
                writer.WriteBitString(IssuerUniqueId.Value.Span, 0, new Asn1Tag(TagClass.ContextSpecific, 1));
            }


            if (SubjectUniqueId.HasValue)
            {
                writer.WriteBitString(SubjectUniqueId.Value.Span, 0, new Asn1Tag(TagClass.ContextSpecific, 2));
            }


            if (Extensions != null)
            {
                writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 3));

                writer.PushSequence();
                for (int i = 0; i < Extensions.Length; i++)
                {
                    Extensions[i].Encode(writer);
                }
                writer.PopSequence();

                writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 3));
            }

            writer.PopSequence(tag);
        }

        internal static TbsCertificateAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static TbsCertificateAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, encoded, out TbsCertificateAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out TbsCertificateAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out TbsCertificateAsn decoded)
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

        private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out TbsCertificateAsn decoded)
        {
            decoded = default;
            AsnValueReader sequenceReader = reader.ReadSequence(expectedTag);
            AsnValueReader explicitReader;
            AsnValueReader defaultReader;
            AsnValueReader collectionReader;
            ReadOnlySpan<byte> rebindSpan = rebind.Span;
            int offset;
            ReadOnlySpan<byte> tmpSpan;


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));

                if (!explicitReader.TryReadInt32(out decoded.Version))
                {
                    explicitReader.ThrowIfNotEmpty();
                }

                explicitReader.ThrowIfNotEmpty();
            }
            else
            {
                defaultReader = new AsnValueReader(DefaultVersion, AsnEncodingRules.DER);

                if (!defaultReader.TryReadInt32(out decoded.Version))
                {
                    defaultReader.ThrowIfNotEmpty();
                }

            }

            tmpSpan = sequenceReader.ReadIntegerBytes();
            decoded.SerialNumber = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref sequenceReader, rebind, out decoded.SignatureAlgorithm);
            if (!sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag((UniversalTagNumber)16)))
            {
                throw new CryptographicException();
            }

            tmpSpan = sequenceReader.ReadEncodedValue();
            decoded.Issuer = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            System.Security.Cryptography.X509Certificates.Asn1.ValidityAsn.Decode(ref sequenceReader, out decoded.Validity);
            if (!sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag((UniversalTagNumber)16)))
            {
                throw new CryptographicException();
            }

            tmpSpan = sequenceReader.ReadEncodedValue();
            decoded.Subject = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            System.Security.Cryptography.Asn1.SubjectPublicKeyInfoAsn.Decode(ref sequenceReader, rebind, out decoded.SubjectPublicKeyInfo);

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {

                if (sequenceReader.TryReadPrimitiveBitString(out _, out tmpSpan, new Asn1Tag(TagClass.ContextSpecific, 1)))
                {
                    decoded.IssuerUniqueId = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
                }
                else
                {
                    decoded.IssuerUniqueId = sequenceReader.ReadBitString(out _, new Asn1Tag(TagClass.ContextSpecific, 1));
                }

            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 2)))
            {

                if (sequenceReader.TryReadPrimitiveBitString(out _, out tmpSpan, new Asn1Tag(TagClass.ContextSpecific, 2)))
                {
                    decoded.SubjectUniqueId = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
                }
                else
                {
                    decoded.SubjectUniqueId = sequenceReader.ReadBitString(out _, new Asn1Tag(TagClass.ContextSpecific, 2));
                }

            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 3)))
            {
                explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 3));

                // Decode SEQUENCE OF for Extensions
                {
                    collectionReader = explicitReader.ReadSequence();
                    var tmpList = new List<System.Security.Cryptography.Asn1.X509ExtensionAsn>();
                    System.Security.Cryptography.Asn1.X509ExtensionAsn tmpItem;

                    while (collectionReader.HasData)
                    {
                        System.Security.Cryptography.Asn1.X509ExtensionAsn.Decode(ref collectionReader, rebind, out tmpItem);
                        tmpList.Add(tmpItem);
                    }

                    decoded.Extensions = tmpList.ToArray();
                }

                explicitReader.ThrowIfNotEmpty();
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
