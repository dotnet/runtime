// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.X509Certificates.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct BasicConstraintsAsn
    {
        private static readonly byte[] s_defaultCA = { 0x01, 0x01, 0x00 };

        internal bool CA;
        internal int? PathLengthConstraint;

#if DEBUG
        static BasicConstraintsAsn()
        {
            BasicConstraintsAsn decoded = default;
            AsnValueReader reader;

            reader = new AsnValueReader(s_defaultCA, AsnEncodingRules.DER);
            decoded.CA = reader.ReadBoolean();
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


            // DEFAULT value handler for CA.
            {
                using (AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER))
                {
                    tmp.WriteBoolean(CA);
                    ReadOnlySpan<byte> encoded = tmp.EncodeAsSpan();

                    if (!encoded.SequenceEqual(s_defaultCA))
                    {
                        writer.WriteEncodedValue(encoded);
                    }
                }
            }


            if (PathLengthConstraint.HasValue)
            {
                writer.WriteInteger(PathLengthConstraint.Value);
            }

            writer.PopSequence(tag);
        }

        internal static BasicConstraintsAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static BasicConstraintsAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);

            Decode(ref reader, expectedTag, encoded, out BasicConstraintsAsn decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out BasicConstraintsAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out BasicConstraintsAsn decoded)
        {
            decoded = default;
            AsnValueReader sequenceReader = reader.ReadSequence(expectedTag);
            AsnValueReader defaultReader;


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Boolean))
            {
                decoded.CA = sequenceReader.ReadBoolean();
            }
            else
            {
                defaultReader = new AsnValueReader(s_defaultCA, AsnEncodingRules.DER);
                decoded.CA = defaultReader.ReadBoolean();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Integer))
            {

                if (sequenceReader.TryReadInt32(out int tmpPathLengthConstraint))
                {
                    decoded.PathLengthConstraint = tmpPathLengthConstraint;
                }
                else
                {
                    sequenceReader.ThrowIfNotEmpty();
                }

            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
