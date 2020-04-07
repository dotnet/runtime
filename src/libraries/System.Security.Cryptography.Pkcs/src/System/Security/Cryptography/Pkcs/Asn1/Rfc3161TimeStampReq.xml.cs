﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct Rfc3161TimeStampReq
    {
        private static ReadOnlySpan<byte> DefaultCertReq => new byte[] { 0x01, 0x01, 0x00 };

        internal int Version;
        internal System.Security.Cryptography.Pkcs.Asn1.MessageImprint MessageImprint;
        internal Oid? ReqPolicy;
        internal ReadOnlyMemory<byte>? Nonce;
        internal bool CertReq;
        internal System.Security.Cryptography.Asn1.X509ExtensionAsn[]? Extensions;

#if DEBUG
        static Rfc3161TimeStampReq()
        {
            Rfc3161TimeStampReq decoded = default;
            AsnValueReader reader;

            reader = new AsnValueReader(DefaultCertReq, AsnEncodingRules.DER);
            decoded.CertReq = reader.ReadBoolean();
            reader.ThrowIfNotEmpty();
        }
#endif

        internal void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);

            writer.WriteInteger(Version);
            MessageImprint.Encode(writer);

            if (ReqPolicy != null)
            {
                writer.WriteObjectIdentifier(ReqPolicy);
            }


            if (Nonce.HasValue)
            {
                writer.WriteInteger(Nonce.Value.Span);
            }


            // DEFAULT value handler for CertReq.
            {
                using (AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER))
                {
                    tmp.WriteBoolean(CertReq);
                    ReadOnlySpan<byte> encoded = tmp.EncodeAsSpan();

                    if (!encoded.SequenceEqual(DefaultCertReq))
                    {
                        writer.WriteEncodedValue(encoded);
                    }
                }
            }


            if (Extensions != null)
            {

                writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                for (int i = 0; i < Extensions.Length; i++)
                {
                    Extensions[i].Encode(writer);
                }
                writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));

            }

            writer.PopSequence(tag);
        }

        internal static Rfc3161TimeStampReq Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static Rfc3161TimeStampReq Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);

            Decode(ref reader, expectedTag, encoded, out Rfc3161TimeStampReq decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out Rfc3161TimeStampReq decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out Rfc3161TimeStampReq decoded)
        {
            decoded = default;
            AsnValueReader sequenceReader = reader.ReadSequence(expectedTag);
            AsnValueReader defaultReader;
            AsnValueReader collectionReader;
            ReadOnlySpan<byte> rebindSpan = rebind.Span;
            int offset;
            ReadOnlySpan<byte> tmpSpan;


            if (!sequenceReader.TryReadInt32(out decoded.Version))
            {
                sequenceReader.ThrowIfNotEmpty();
            }

            System.Security.Cryptography.Pkcs.Asn1.MessageImprint.Decode(ref sequenceReader, rebind, out decoded.MessageImprint);

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.ObjectIdentifier))
            {
                decoded.ReqPolicy = sequenceReader.ReadObjectIdentifier();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Integer))
            {
                tmpSpan = sequenceReader.ReadIntegerBytes();
                decoded.Nonce = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Boolean))
            {
                decoded.CertReq = sequenceReader.ReadBoolean();
            }
            else
            {
                defaultReader = new AsnValueReader(DefaultCertReq, AsnEncodingRules.DER);
                decoded.CertReq = defaultReader.ReadBoolean();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {

                // Decode SEQUENCE OF for Extensions
                {
                    collectionReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
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
