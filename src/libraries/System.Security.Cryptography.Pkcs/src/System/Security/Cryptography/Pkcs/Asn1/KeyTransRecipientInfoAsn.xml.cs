// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct KeyTransRecipientInfoAsn
    {
        internal int Version;
        internal System.Security.Cryptography.Pkcs.Asn1.RecipientIdentifierAsn Rid;
        internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn KeyEncryptionAlgorithm;
        internal ReadOnlyMemory<byte> EncryptedKey;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);

            writer.WriteInteger(Version);
            Rid.Encode(writer);
            KeyEncryptionAlgorithm.Encode(writer);
            writer.WriteOctetString(EncryptedKey.Span);
            writer.PopSequence(tag);
        }

        internal static KeyTransRecipientInfoAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static KeyTransRecipientInfoAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, encoded, out KeyTransRecipientInfoAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out KeyTransRecipientInfoAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out KeyTransRecipientInfoAsn decoded)
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

        private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out KeyTransRecipientInfoAsn decoded)
        {
            decoded = default;
            AsnValueReader sequenceReader = reader.ReadSequence(expectedTag);
            ReadOnlySpan<byte> rebindSpan = rebind.Span;
            int offset;
            ReadOnlySpan<byte> tmpSpan;


            if (!sequenceReader.TryReadInt32(out decoded.Version))
            {
                sequenceReader.ThrowIfNotEmpty();
            }

            System.Security.Cryptography.Pkcs.Asn1.RecipientIdentifierAsn.Decode(ref sequenceReader, rebind, out decoded.Rid);
            System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref sequenceReader, rebind, out decoded.KeyEncryptionAlgorithm);

            if (sequenceReader.TryReadPrimitiveOctetString(out tmpSpan))
            {
                decoded.EncryptedKey = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            }
            else
            {
                decoded.EncryptedKey = sequenceReader.ReadOctetString();
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
