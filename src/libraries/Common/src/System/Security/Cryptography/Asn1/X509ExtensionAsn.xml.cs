// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct X509ExtensionAsn
    {
        private static ReadOnlySpan<byte> DefaultCritical => [0x01, 0x01, 0x00];

        internal string ExtnId;
        internal bool Critical;
        internal ReadOnlyMemory<byte> ExtnValue;

#if DEBUG
        static X509ExtensionAsn()
        {
            X509ExtensionAsn decoded = default;
            AsnValueReader reader;

            reader = new AsnValueReader(DefaultCritical, AsnEncodingRules.DER);
            decoded.Critical = reader.ReadBoolean();
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

            try
            {
                writer.WriteObjectIdentifier(ExtnId);
            }
            catch (ArgumentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }

            // DEFAULT value handler for Critical.
            {
                const int AsnBoolDerEncodeSize = 3;
                AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER, initialCapacity: AsnBoolDerEncodeSize);
                tmp.WriteBoolean(Critical);

                if (!tmp.EncodedValueEquals(DefaultCritical))
                {
                    tmp.CopyTo(writer);
                }
            }

            writer.WriteOctetString(ExtnValue.Span);
            writer.PopSequence(tag);
        }

        internal static X509ExtensionAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static X509ExtensionAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, encoded, out X509ExtensionAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out X509ExtensionAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out X509ExtensionAsn decoded)
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

        private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out X509ExtensionAsn decoded)
        {
            decoded = default;
            AsnValueReader sequenceReader = reader.ReadSequence(expectedTag);
            AsnValueReader defaultReader;
            ReadOnlySpan<byte> rebindSpan = rebind.Span;
            int offset;
            ReadOnlySpan<byte> tmpSpan;

            decoded.ExtnId = sequenceReader.ReadObjectIdentifier();

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Boolean))
            {
                decoded.Critical = sequenceReader.ReadBoolean();
            }
            else
            {
                defaultReader = new AsnValueReader(DefaultCritical, AsnEncodingRules.DER);
                decoded.Critical = defaultReader.ReadBoolean();
            }


            if (sequenceReader.TryReadPrimitiveOctetString(out tmpSpan))
            {
                decoded.ExtnValue = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            }
            else
            {
                decoded.ExtnValue = sequenceReader.ReadOctetString();
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
