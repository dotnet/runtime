// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1.Pkcs12
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct SecretBagAsn
    {
        internal string SecretTypeId;
        internal ReadOnlyMemory<byte> SecretValue;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);

            try
            {
                writer.WriteObjectIdentifier(SecretTypeId);
            }
            catch (ArgumentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
            writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
            try
            {
                writer.WriteEncodedValue(SecretValue.Span);
            }
            catch (ArgumentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
            writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
            writer.PopSequence(tag);
        }

        internal static SecretBagAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static SecretBagAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, encoded, out SecretBagAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref ValueAsnReader reader, ReadOnlyMemory<byte> rebind, out SecretBagAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out SecretBagAsn decoded)
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

        private static void DecodeCore(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out SecretBagAsn decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ValueAsnReader explicitReader;
            ReadOnlySpan<byte> rebindSpan = rebind.Span;
            int offset;
            ReadOnlySpan<byte> tmpSpan;

            decoded.SecretTypeId = sequenceReader.ReadObjectIdentifier();

            explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
            tmpSpan = explicitReader.ReadEncodedValue();
            decoded.SecretValue = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            explicitReader.ThrowIfNotEmpty();


            sequenceReader.ThrowIfNotEmpty();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref partial struct ValueSecretBagAsn
    {
        internal string SecretTypeId;
        internal ReadOnlySpan<byte> SecretValue;

        internal static void Decode(ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueSecretBagAsn decoded)
        {
            Decode(Asn1Tag.Sequence, encoded, ruleSet, out decoded);
        }

        internal static void Decode(Asn1Tag expectedTag, ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueSecretBagAsn decoded)
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

        internal static void Decode(scoped ref ValueAsnReader reader, out ValueSecretBagAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueSecretBagAsn decoded)
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

        private static void DecodeCore(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueSecretBagAsn decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ValueAsnReader explicitReader;

            decoded.SecretTypeId = sequenceReader.ReadObjectIdentifier();

            explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
            decoded.SecretValue = explicitReader.ReadEncodedValue();
            explicitReader.ThrowIfNotEmpty();


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
