// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct PkiStatusInfo
    {
        internal int Status;
        internal ReadOnlyMemory<byte>? StatusString;
        internal System.Security.Cryptography.Pkcs.Asn1.PkiFailureInfo? FailInfo;
      
        internal void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }
    
        internal void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);
            
            writer.WriteInteger(Status);

            if (StatusString.HasValue)
            {
                // Validator for tag constraint for StatusString
                {
                    if (!Asn1Tag.TryDecode(StatusString.Value.Span, out Asn1Tag validateTag, out _) ||
                        !validateTag.HasSameClassAndValue(new Asn1Tag((UniversalTagNumber)16)))
                    {
                        throw new CryptographicException();
                    }
                }

                writer.WriteEncodedValue(StatusString.Value.Span);
            }


            if (FailInfo.HasValue)
            {
                writer.WriteNamedBitList(FailInfo.Value);
            }

            writer.PopSequence(tag);
        }

        internal static PkiStatusInfo Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }
        
        internal static PkiStatusInfo Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, expectedTag, out PkiStatusInfo decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out PkiStatusInfo decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Decode(reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(AsnReader reader, Asn1Tag expectedTag, out PkiStatusInfo decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            AsnReader sequenceReader = reader.ReadSequence(expectedTag);
            

            if (!sequenceReader.TryReadInt32(out decoded.Status))
            {
                sequenceReader.ThrowIfNotEmpty();
            }


            if (sequenceReader.HasData)
            {
                decoded.StatusString = sequenceReader.ReadEncodedValue();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.PrimitiveBitString))
            {
                decoded.FailInfo = sequenceReader.ReadNamedBitListValue<System.Security.Cryptography.Pkcs.Asn1.PkiFailureInfo>();
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
