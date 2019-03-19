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
    internal partial struct MacData
    {
        private static byte[] s_defaultIterationCount = { 0x02, 0x01, 0x01 };
  
        internal System.Security.Cryptography.Pkcs.Asn1.DigestInfoAsn Mac;
        internal ReadOnlyMemory<byte> MacSalt;
        internal int IterationCount;
      
#if DEBUG  
        static MacData()
        {
            MacData decoded = default;
            AsnReader reader;

            reader = new AsnReader(s_defaultIterationCount, AsnEncodingRules.DER);

            if (!reader.TryReadInt32(out decoded.IterationCount))
            {
                reader.ThrowIfNotEmpty();
            }

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
            
            Mac.Encode(writer);
            writer.WriteOctetString(MacSalt.Span);
        
            // DEFAULT value handler for IterationCount.
            {
                using (AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER))
                {
                    tmp.WriteInteger(IterationCount);
                    ReadOnlySpan<byte> encoded = tmp.EncodeAsSpan();

                    if (!encoded.SequenceEqual(s_defaultIterationCount))
                    {
                        writer.WriteEncodedValue(encoded.ToArray());
                    }
                }
            }

            writer.PopSequence(tag);
        }

        internal static MacData Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }
        
        internal static MacData Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, expectedTag, out MacData decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out MacData decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Decode(reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(AsnReader reader, Asn1Tag expectedTag, out MacData decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            AsnReader sequenceReader = reader.ReadSequence(expectedTag);
            AsnReader defaultReader;
            
            System.Security.Cryptography.Pkcs.Asn1.DigestInfoAsn.Decode(sequenceReader, out decoded.Mac);

            if (sequenceReader.TryReadPrimitiveOctetStringBytes(out ReadOnlyMemory<byte> tmpMacSalt))
            {
                decoded.MacSalt = tmpMacSalt;
            }
            else
            {
                decoded.MacSalt = sequenceReader.ReadOctetString();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Integer))
            {

                if (!sequenceReader.TryReadInt32(out decoded.IterationCount))
                {
                    sequenceReader.ThrowIfNotEmpty();
                }

            }
            else
            {
                defaultReader = new AsnReader(s_defaultIterationCount, AsnEncodingRules.DER);

                if (!defaultReader.TryReadInt32(out decoded.IterationCount))
                {
                    defaultReader.ThrowIfNotEmpty();
                }

            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
