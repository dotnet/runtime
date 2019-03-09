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
    internal partial struct RecipientKeyIdentifier
    {
        internal ReadOnlyMemory<byte> SubjectKeyIdentifier;
        internal DateTimeOffset? Date;
        internal System.Security.Cryptography.Pkcs.Asn1.OtherKeyAttributeAsn? Other;
      
        internal void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }
    
        internal void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);
            
            writer.WriteOctetString(SubjectKeyIdentifier.Span);

            if (Date.HasValue)
            {
                writer.WriteGeneralizedTime(Date.Value);
            }


            if (Other.HasValue)
            {
                Other.Value.Encode(writer);
            }

            writer.PopSequence(tag);
        }

        internal static RecipientKeyIdentifier Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }
        
        internal static RecipientKeyIdentifier Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, expectedTag, out RecipientKeyIdentifier decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out RecipientKeyIdentifier decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Decode(reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(AsnReader reader, Asn1Tag expectedTag, out RecipientKeyIdentifier decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            AsnReader sequenceReader = reader.ReadSequence(expectedTag);
            

            if (sequenceReader.TryReadPrimitiveOctetStringBytes(out ReadOnlyMemory<byte> tmpSubjectKeyIdentifier))
            {
                decoded.SubjectKeyIdentifier = tmpSubjectKeyIdentifier;
            }
            else
            {
                decoded.SubjectKeyIdentifier = sequenceReader.ReadOctetString();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.GeneralizedTime))
            {
                decoded.Date = sequenceReader.ReadGeneralizedTime();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Sequence))
            {
                System.Security.Cryptography.Pkcs.Asn1.OtherKeyAttributeAsn tmpOther;
                System.Security.Cryptography.Pkcs.Asn1.OtherKeyAttributeAsn.Decode(sequenceReader, out tmpOther);
                decoded.Other = tmpOther;

            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
