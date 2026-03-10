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
    internal partial struct PolicyInformation
    {
        internal string PolicyIdentifier;
        internal System.Security.Cryptography.Pkcs.Asn1.PolicyQualifierInfo[]? PolicyQualifiers;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);

            try
            {
                writer.WriteObjectIdentifier(PolicyIdentifier);
            }
            catch (ArgumentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }

            if (PolicyQualifiers != null)
            {

                writer.PushSequence();
                for (int i = 0; i < PolicyQualifiers.Length; i++)
                {
                    PolicyQualifiers[i].Encode(writer);
                }
                writer.PopSequence();

            }

            writer.PopSequence(tag);
        }

        internal static PolicyInformation Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static PolicyInformation Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, encoded, out PolicyInformation decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref ValueAsnReader reader, ReadOnlyMemory<byte> rebind, out PolicyInformation decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out PolicyInformation decoded)
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

        private static void DecodeCore(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out PolicyInformation decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ValueAsnReader collectionReader;

            decoded.PolicyIdentifier = sequenceReader.ReadObjectIdentifier();

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Sequence))
            {

                // Decode SEQUENCE OF for PolicyQualifiers
                {
                    collectionReader = sequenceReader.ReadSequence();
                    var tmpList = new List<System.Security.Cryptography.Pkcs.Asn1.PolicyQualifierInfo>();
                    System.Security.Cryptography.Pkcs.Asn1.PolicyQualifierInfo tmpItem;

                    while (collectionReader.HasData)
                    {
                        System.Security.Cryptography.Pkcs.Asn1.PolicyQualifierInfo.Decode(ref collectionReader, rebind, out tmpItem);
                        tmpList.Add(tmpItem);
                    }

                    decoded.PolicyQualifiers = tmpList.ToArray();
                }

            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref partial struct ValuePolicyInformation
    {
        internal string PolicyIdentifier;
        internal ReadOnlySpan<byte> PolicyQualifiers;
        internal bool HasPolicyQualifiers;

        internal static void Decode(ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValuePolicyInformation decoded)
        {
            Decode(Asn1Tag.Sequence, encoded, ruleSet, out decoded);
        }

        internal static void Decode(Asn1Tag expectedTag, ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValuePolicyInformation decoded)
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

        internal static void Decode(scoped ref ValueAsnReader reader, out ValuePolicyInformation decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValuePolicyInformation decoded)
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

        private static void DecodeCore(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValuePolicyInformation decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);

            decoded.PolicyIdentifier = sequenceReader.ReadObjectIdentifier();

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Sequence))
            {
                decoded.PolicyQualifiers = sequenceReader.ReadEncodedValue();
                decoded.HasPolicyQualifiers = true;
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
