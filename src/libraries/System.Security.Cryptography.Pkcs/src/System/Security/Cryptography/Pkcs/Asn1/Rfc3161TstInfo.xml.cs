// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    file static class SharedRfc3161TstInfo
    {
        internal static ReadOnlySpan<byte> DefaultOrdering => [0x01, 0x01, 0x00];

#if DEBUG
        static SharedRfc3161TstInfo()
        {
            Rfc3161TstInfo decoded = default;
            ValueAsnReader reader;

            reader = new ValueAsnReader(SharedRfc3161TstInfo.DefaultOrdering, AsnEncodingRules.DER);
            decoded.Ordering = reader.ReadBoolean();
            reader.ThrowIfNotEmpty();
        }
#endif
    }

    [StructLayout(LayoutKind.Sequential)]
    internal partial struct Rfc3161TstInfo
    {
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

                if (!tmp.EncodedValueEquals(SharedRfc3161TstInfo.DefaultOrdering))
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
                ValueAsnReader reader = new ValueAsnReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, encoded, out Rfc3161TstInfo decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref ValueAsnReader reader, ReadOnlyMemory<byte> rebind, out Rfc3161TstInfo decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out Rfc3161TstInfo decoded)
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

        private static void DecodeCore(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out Rfc3161TstInfo decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ValueAsnReader explicitReader;
            ValueAsnReader defaultReader;
            ValueAsnReader collectionReader;
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
                defaultReader = new ValueAsnReader(SharedRfc3161TstInfo.DefaultOrdering, AsnEncodingRules.DER);
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

    [StructLayout(LayoutKind.Sequential)]
    internal ref partial struct ValueRfc3161TstInfo
    {
        internal int Version;
        internal string Policy;
        internal System.Security.Cryptography.Pkcs.Asn1.ValueMessageImprint MessageImprint;
        internal ReadOnlySpan<byte> SerialNumber;
        internal DateTimeOffset GenTime;
        internal System.Security.Cryptography.Pkcs.Asn1.Rfc3161Accuracy? Accuracy;
        internal bool Ordering;

        internal ReadOnlySpan<byte> Nonce
        {
            get;
            set
            {
                HasNonce = true;
                field = value;
            }
        }

        internal bool HasNonce { get; private set; }

        internal ReadOnlySpan<byte> Tsa
        {
            get;
            set
            {
                HasTsa = true;
                field = value;
            }
        }

        internal bool HasTsa { get; private set; }

        internal ReadOnlySpan<byte> Extensions
        {
            get;
            set
            {
                HasExtensions = true;
                field = value;
            }
        }

        internal bool HasExtensions { get; private set; }

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
            writer.WriteInteger(SerialNumber);
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

                if (!tmp.EncodedValueEquals(SharedRfc3161TstInfo.DefaultOrdering))
                {
                    tmp.CopyTo(writer);
                }
            }


            if (HasNonce)
            {
                writer.WriteInteger(Nonce);
            }


            if (HasTsa)
            {
                writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0));

                try
                {
                    writer.WriteEncodedValue(Tsa);
                }
                catch (ArgumentException e)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
                }
                writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
            }


            if (HasExtensions)
            {

                try
                {
                    writer.WriteEncodedValue(Extensions);
                }
                catch (ArgumentException e)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
                }
            }

            writer.PopSequence(tag);
        }

        internal static void Decode(ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueRfc3161TstInfo decoded)
        {
            Decode(Asn1Tag.Sequence, encoded, ruleSet, out decoded);
        }

        internal static void Decode(Asn1Tag expectedTag, ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueRfc3161TstInfo decoded)
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

        internal static void Decode(scoped ref ValueAsnReader reader, out ValueRfc3161TstInfo decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueRfc3161TstInfo decoded)
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

        private static void DecodeCore(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueRfc3161TstInfo decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ValueAsnReader explicitReader;
            ValueAsnReader defaultReader;


            if (!sequenceReader.TryReadInt32(out decoded.Version))
            {
                sequenceReader.ThrowIfNotEmpty();
            }

            decoded.Policy = sequenceReader.ReadObjectIdentifier();
            System.Security.Cryptography.Pkcs.Asn1.ValueMessageImprint.Decode(ref sequenceReader, out decoded.MessageImprint);
            decoded.SerialNumber = sequenceReader.ReadIntegerBytes();
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
                defaultReader = new ValueAsnReader(SharedRfc3161TstInfo.DefaultOrdering, AsnEncodingRules.DER);
                decoded.Ordering = defaultReader.ReadBoolean();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Integer))
            {
                decoded.Nonce = sequenceReader.ReadIntegerBytes();
                decoded.HasNonce = true;
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                decoded.Tsa = explicitReader.ReadEncodedValue();
                decoded.HasTsa = true;
                explicitReader.ThrowIfNotEmpty();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {
                decoded.Extensions = sequenceReader.ReadEncodedValue();
                decoded.HasExtensions = true;
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
