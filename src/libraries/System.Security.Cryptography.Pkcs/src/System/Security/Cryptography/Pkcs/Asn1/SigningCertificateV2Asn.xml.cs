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
    internal partial struct SigningCertificateV2Asn
    {
        internal System.Security.Cryptography.Pkcs.Asn1.EssCertIdV2[] Certs;
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

        internal static SigningCertificateV2Asn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static SigningCertificateV2Asn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, encoded, out SigningCertificateV2Asn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref ValueAsnReader reader, ReadOnlyMemory<byte> rebind, out SigningCertificateV2Asn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out SigningCertificateV2Asn decoded)
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

        private static void DecodeCore(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out SigningCertificateV2Asn decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ValueAsnReader collectionReader;


            // Decode SEQUENCE OF for Certs
            {
                collectionReader = sequenceReader.ReadSequence();
                var tmpList = new List<System.Security.Cryptography.Pkcs.Asn1.EssCertIdV2>();
                System.Security.Cryptography.Pkcs.Asn1.EssCertIdV2 tmpItem;

                while (collectionReader.HasData)
                {
                    System.Security.Cryptography.Pkcs.Asn1.EssCertIdV2.Decode(ref collectionReader, rebind, out tmpItem);
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

    [StructLayout(LayoutKind.Sequential)]
    internal ref partial struct ValueSigningCertificateV2Asn
    {
        internal ReadOnlySpan<byte> Certs;
        internal ReadOnlySpan<byte> Policies;
        internal bool HasPolicies;

        internal static void Decode(ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueSigningCertificateV2Asn decoded)
        {
            Decode(Asn1Tag.Sequence, encoded, ruleSet, out decoded);
        }

        internal static void Decode(Asn1Tag expectedTag, ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueSigningCertificateV2Asn decoded)
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

        internal static void Decode(scoped ref ValueAsnReader reader, out ValueSigningCertificateV2Asn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueSigningCertificateV2Asn decoded)
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

        private static void DecodeCore(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueSigningCertificateV2Asn decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);

            decoded.Certs = sequenceReader.ReadEncodedValue();

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Sequence))
            {
                decoded.Policies = sequenceReader.ReadEncodedValue();
                decoded.HasPolicies = true;
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
