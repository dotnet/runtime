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
    internal partial struct MessageImprint
    {
        internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn HashAlgorithm;
        internal ReadOnlyMemory<byte> HashedMessage;
      
        internal void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }
    
        internal void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);
            
            HashAlgorithm.Encode(writer);
            writer.WriteOctetString(HashedMessage.Span);
            writer.PopSequence(tag);
        }

        internal static MessageImprint Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }
        
        internal static MessageImprint Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, expectedTag, out MessageImprint decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out MessageImprint decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Decode(reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(AsnReader reader, Asn1Tag expectedTag, out MessageImprint decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            AsnReader sequenceReader = reader.ReadSequence(expectedTag);
            
            System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(sequenceReader, out decoded.HashAlgorithm);

            if (sequenceReader.TryReadPrimitiveOctetStringBytes(out ReadOnlyMemory<byte> tmpHashedMessage))
            {
                decoded.HashedMessage = tmpHashedMessage;
            }
            else
            {
                decoded.HashedMessage = sequenceReader.ReadOctetString();
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
