// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.X509Certificates.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct CertificatePolicyMappingAsn
    {
        internal string IssuerDomainPolicy;
        internal string SubjectDomainPolicy;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);

            try
            {
                writer.WriteObjectIdentifier(IssuerDomainPolicy);
            }
            catch (ArgumentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
            try
            {
                writer.WriteObjectIdentifier(SubjectDomainPolicy);
            }
            catch (ArgumentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
            writer.PopSequence(tag);
        }

        internal static CertificatePolicyMappingAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static CertificatePolicyMappingAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, out CertificatePolicyMappingAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref AsnValueReader reader, out CertificatePolicyMappingAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, out CertificatePolicyMappingAsn decoded)
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

        private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, out CertificatePolicyMappingAsn decoded)
        {
            decoded = default;
            AsnValueReader sequenceReader = reader.ReadSequence(expectedTag);

            decoded.IssuerDomainPolicy = sequenceReader.ReadObjectIdentifier();
            decoded.SubjectDomainPolicy = sequenceReader.ReadObjectIdentifier();

            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
