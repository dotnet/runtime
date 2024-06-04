// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct EssCertIdV2
    {
        private static ReadOnlySpan<byte> DefaultHashAlgorithm => [0x30, 0x0B, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01];

        internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn HashAlgorithm;
        internal ReadOnlyMemory<byte> Hash;
        internal System.Security.Cryptography.Pkcs.Asn1.CadesIssuerSerial? IssuerSerial;

#if DEBUG
        static EssCertIdV2()
        {
            EssCertIdV2 decoded = default;
            ReadOnlyMemory<byte> rebind = default;
            AsnValueReader reader;

            reader = new AsnValueReader(DefaultHashAlgorithm, AsnEncodingRules.DER);
            System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref reader, rebind, out decoded.HashAlgorithm);
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


            // DEFAULT value handler for HashAlgorithm.
            {
                AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER);
                HashAlgorithm.Encode(tmp);

                if (!tmp.EncodedValueEquals(DefaultHashAlgorithm))
                {
                    tmp.CopyTo(writer);
                }
            }

            writer.WriteOctetString(Hash.Span);

            if (IssuerSerial.HasValue)
            {
                IssuerSerial.Value.Encode(writer);
            }

            writer.PopSequence(tag);
        }

        internal static EssCertIdV2 Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static EssCertIdV2 Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, encoded, out EssCertIdV2 decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out EssCertIdV2 decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out EssCertIdV2 decoded)
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

        private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out EssCertIdV2 decoded)
        {
            decoded = default;
            AsnValueReader sequenceReader = reader.ReadSequence(expectedTag);
            AsnValueReader defaultReader;
            ReadOnlySpan<byte> rebindSpan = rebind.Span;
            int offset;
            ReadOnlySpan<byte> tmpSpan;


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Sequence))
            {
                System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref sequenceReader, rebind, out decoded.HashAlgorithm);
            }
            else
            {
                defaultReader = new AsnValueReader(DefaultHashAlgorithm, AsnEncodingRules.DER);
                System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref defaultReader, rebind, out decoded.HashAlgorithm);
            }


            if (sequenceReader.TryReadPrimitiveOctetString(out tmpSpan))
            {
                decoded.Hash = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            }
            else
            {
                decoded.Hash = sequenceReader.ReadOctetString();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Sequence))
            {
                System.Security.Cryptography.Pkcs.Asn1.CadesIssuerSerial tmpIssuerSerial;
                System.Security.Cryptography.Pkcs.Asn1.CadesIssuerSerial.Decode(ref sequenceReader, rebind, out tmpIssuerSerial);
                decoded.IssuerSerial = tmpIssuerSerial;

            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
