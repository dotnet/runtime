// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.X509Certificates.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct TbsCertificateAsn
    {
        private static ReadOnlySpan<byte> DefaultVersion => new byte[] { 0x02, 0x01, 0x00 };

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

        internal void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);


            // DEFAULT value handler for Version.
            {
                using (AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER))
                {
                    tmp.WriteInteger(Version);
                    ReadOnlySpan<byte> encoded = tmp.EncodeAsSpan();

                    if (!encoded.SequenceEqual(DefaultVersion))
                    {
                        writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                        writer.WriteEncodedValue(encoded);
                        writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                    }
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

            writer.WriteEncodedValue(Issuer.Span);
            Validity.Encode(writer);
            // Validator for tag constraint for Subject
            {
                if (!Asn1Tag.TryDecode(Subject.Span, out Asn1Tag validateTag, out _) ||
                    !validateTag.HasSameClassAndValue(new Asn1Tag((UniversalTagNumber)16)))
                {
                    throw new CryptographicException();
                }
            }

            writer.WriteEncodedValue(Subject.Span);
            SubjectPublicKeyInfo.Encode(writer);

            if (IssuerUniqueId.HasValue)
            {
                writer.WriteBitString(new Asn1Tag(TagClass.ContextSpecific, 1), IssuerUniqueId.Value.Span);
            }


            if (SubjectUniqueId.HasValue)
            {
                writer.WriteBitString(new Asn1Tag(TagClass.ContextSpecific, 2), SubjectUniqueId.Value.Span);
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
            AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);

            Decode(ref reader, expectedTag, encoded, out TbsCertificateAsn decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out TbsCertificateAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out TbsCertificateAsn decoded)
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
            System.Security.Cryptography.X509Certificates.Asn1.ValidityAsn.Decode(ref sequenceReader, rebind, out decoded.Validity);
            if (!sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag((UniversalTagNumber)16)))
            {
                throw new CryptographicException();
            }

            tmpSpan = sequenceReader.ReadEncodedValue();
            decoded.Subject = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            System.Security.Cryptography.Asn1.SubjectPublicKeyInfoAsn.Decode(ref sequenceReader, rebind, out decoded.SubjectPublicKeyInfo);

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {

                if (sequenceReader.TryReadPrimitiveBitStringValue(new Asn1Tag(TagClass.ContextSpecific, 1), out _, out tmpSpan))
                {
                    decoded.IssuerUniqueId = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
                }
                else
                {
                    decoded.IssuerUniqueId = sequenceReader.ReadBitString(new Asn1Tag(TagClass.ContextSpecific, 1), out _);
                }

            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 2)))
            {

                if (sequenceReader.TryReadPrimitiveBitStringValue(new Asn1Tag(TagClass.ContextSpecific, 2), out _, out tmpSpan))
                {
                    decoded.SubjectUniqueId = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
                }
                else
                {
                    decoded.SubjectUniqueId = sequenceReader.ReadBitString(new Asn1Tag(TagClass.ContextSpecific, 2), out _);
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
