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
    internal partial struct SigningCertificateAsn
    {
        internal System.Security.Cryptography.Pkcs.Asn1.EssCertId[] Certs;
        internal System.Security.Cryptography.Pkcs.Asn1.PolicyInformation[]? Policies;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);


            writer.PushSequence();
            for (int i = 0; i < Certs.Length; i++)
            {
                Certs[i].Encode(writer);
            }
            writer.PopSequence();


            if (Policies != null)
            {

                writer.PushSequence();
                for (int i = 0; i < Policies.Length; i++)
                {
                    Policies[i].Encode(writer);
                }
                writer.PopSequence();

            }

            writer.PopSequence(tag);
        }

        internal static SigningCertificateAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static SigningCertificateAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, encoded, out SigningCertificateAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out SigningCertificateAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out SigningCertificateAsn decoded)
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

        private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out SigningCertificateAsn decoded)
        {
            decoded = default;
            AsnValueReader sequenceReader = reader.ReadSequence(expectedTag);
            AsnValueReader collectionReader;


            // Decode SEQUENCE OF for Certs
            {
                collectionReader = sequenceReader.ReadSequence();
                var tmpList = new List<System.Security.Cryptography.Pkcs.Asn1.EssCertId>();
                System.Security.Cryptography.Pkcs.Asn1.EssCertId tmpItem;

                while (collectionReader.HasData)
                {
                    System.Security.Cryptography.Pkcs.Asn1.EssCertId.Decode(ref collectionReader, rebind, out tmpItem);
                    tmpList.Add(tmpItem);
                }

                decoded.Certs = tmpList.ToArray();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Sequence))
            {

                // Decode SEQUENCE OF for Policies
                {
                    collectionReader = sequenceReader.ReadSequence();
                    var tmpList = new List<System.Security.Cryptography.Pkcs.Asn1.PolicyInformation>();
                    System.Security.Cryptography.Pkcs.Asn1.PolicyInformation tmpItem;

                    while (collectionReader.HasData)
                    {
                        System.Security.Cryptography.Pkcs.Asn1.PolicyInformation.Decode(ref collectionReader, rebind, out tmpItem);
                        tmpList.Add(tmpItem);
                    }

                    decoded.Policies = tmpList.ToArray();
                }

            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
