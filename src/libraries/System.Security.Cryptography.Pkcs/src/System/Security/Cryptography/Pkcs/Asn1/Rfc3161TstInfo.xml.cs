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
    internal partial struct Rfc3161TstInfo
    {
        private static ReadOnlySpan<byte> DefaultOrdering => [0x01, 0x01, 0x00];

        internal int Version;
        internal string Policy;
        internal System.Security.Cryptography.Pkcs.Asn1.MessageImprint MessageImprint;
        internal ReadOnlyMemory<byte> SerialNumber;
        internal DateTimeOffset GenTime;
        internal System.Security.Cryptography.Pkcs.Asn1.Rfc3161Accuracy? Accuracy;
        internal bool Ordering;
        internal ReadOnlyMemory<byte>? Nonce;
        internal System.Security.Cryptography.Asn1.GeneralNameAsn? Tsa;
        internal System.Security.Cryptography.Asn1.X509ExtensionAsn[]? Extensions;

#if DEBUG
        static Rfc3161TstInfo()
        {
            Rfc3161TstInfo decoded = default;
            AsnValueReader reader;

            reader = new AsnValueReader(DefaultOrdering, AsnEncodingRules.DER);
            decoded.Ordering = reader.ReadBoolean();
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

            writer.WriteInteger(Version);
            try
            {
                writer.WriteObjectIdentifier(Policy);
            }
            catch (ArgumentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
            MessageImprint.Encode(writer);
            writer.WriteInteger(SerialNumber.Span);
            writer.WriteGeneralizedTime(GenTime, false);

            if (Accuracy.HasValue)
            {
                Accuracy.Value.Encode(writer);
            }


            // DEFAULT value handler for Ordering.
            {
                const int AsnBoolDerEncodeSize = 3;
                AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER, initialCapacity: AsnBoolDerEncodeSize);
                tmp.WriteBoolean(Ordering);

                if (!tmp.EncodedValueEquals(DefaultOrdering))
                {
                    tmp.CopyTo(writer);
                }
            }


            if (Nonce.HasValue)
            {
                writer.WriteInteger(Nonce.Value.Span);
            }


            if (Tsa.HasValue)
            {
                writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                Tsa.Value.Encode(writer);
                writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
            }


            if (Extensions != null)
            {

                writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
                for (int i = 0; i < Extensions.Length; i++)
                {
                    Extensions[i].Encode(writer);
                }
                writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 1));

            }

            writer.PopSequence(tag);
        }

        internal static Rfc3161TstInfo Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static Rfc3161TstInfo Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, encoded, out Rfc3161TstInfo decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out Rfc3161TstInfo decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out Rfc3161TstInfo decoded)
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

        private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out Rfc3161TstInfo decoded)
        {
            decoded = default;
            AsnValueReader sequenceReader = reader.ReadSequence(expectedTag);
            AsnValueReader explicitReader;
            AsnValueReader defaultReader;
            AsnValueReader collectionReader;
            ReadOnlySpan<byte> rebindSpan = rebind.Span;
            int offset;
            ReadOnlySpan<byte> tmpSpan;


            if (!sequenceReader.TryReadInt32(out decoded.Version))
            {
                sequenceReader.ThrowIfNotEmpty();
            }

            decoded.Policy = sequenceReader.ReadObjectIdentifier();
            System.Security.Cryptography.Pkcs.Asn1.MessageImprint.Decode(ref sequenceReader, rebind, out decoded.MessageImprint);
            tmpSpan = sequenceReader.ReadIntegerBytes();
            decoded.SerialNumber = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            decoded.GenTime = sequenceReader.ReadGeneralizedTime();

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Sequence))
            {
                System.Security.Cryptography.Pkcs.Asn1.Rfc3161Accuracy tmpAccuracy;
                System.Security.Cryptography.Pkcs.Asn1.Rfc3161Accuracy.Decode(ref sequenceReader, out tmpAccuracy);
                decoded.Accuracy = tmpAccuracy;

            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Boolean))
            {
                decoded.Ordering = sequenceReader.ReadBoolean();
            }
            else
            {
                defaultReader = new AsnValueReader(DefaultOrdering, AsnEncodingRules.DER);
                decoded.Ordering = defaultReader.ReadBoolean();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Integer))
            {
                tmpSpan = sequenceReader.ReadIntegerBytes();
                decoded.Nonce = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                System.Security.Cryptography.Asn1.GeneralNameAsn tmpTsa;
                System.Security.Cryptography.Asn1.GeneralNameAsn.Decode(ref explicitReader, rebind, out tmpTsa);
                decoded.Tsa = tmpTsa;

                explicitReader.ThrowIfNotEmpty();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {

                // Decode SEQUENCE OF for Extensions
                {
                    collectionReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
                    var tmpList = new List<System.Security.Cryptography.Asn1.X509ExtensionAsn>();
                    System.Security.Cryptography.Asn1.X509ExtensionAsn tmpItem;

                    while (collectionReader.HasData)
                    {
                        System.Security.Cryptography.Asn1.X509ExtensionAsn.Decode(ref collectionReader, rebind, out tmpItem);
                        tmpList.Add(tmpItem);
                    }

                    decoded.Extensions = tmpList.ToArray();
                }

            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
