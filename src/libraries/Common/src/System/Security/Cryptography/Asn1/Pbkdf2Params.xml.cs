// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct Pbkdf2Params
    {
        private static byte[] s_defaultPrf = { 0x30, 0x0C, 0x06, 0x08, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x02, 0x07, 0x05, 0x00 };
  
        internal System.Security.Cryptography.Asn1.Pbkdf2SaltChoice Salt;
        internal int IterationCount;
        internal byte? KeyLength;
        internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn Prf;
      
#if DEBUG  
        static Pbkdf2Params()
        {
            Pbkdf2Params decoded = default;
            AsnReader reader;

            reader = new AsnReader(s_defaultPrf, AsnEncodingRules.DER);
            System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(reader, out decoded.Prf);
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
            
            Salt.Encode(writer);
            writer.WriteInteger(IterationCount);

            if (KeyLength.HasValue)
            {
                writer.WriteInteger(KeyLength.Value);
            }

        
            // DEFAULT value handler for Prf.
            {
                using (AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER))
                {
                    Prf.Encode(tmp);
                    ReadOnlySpan<byte> encoded = tmp.EncodeAsSpan();

                    if (!encoded.SequenceEqual(s_defaultPrf))
                    {
                        writer.WriteEncodedValue(encoded.ToArray());
                    }
                }
            }

            writer.PopSequence(tag);
        }

        internal static Pbkdf2Params Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }
        
        internal static Pbkdf2Params Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, expectedTag, out Pbkdf2Params decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out Pbkdf2Params decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Decode(reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(AsnReader reader, Asn1Tag expectedTag, out Pbkdf2Params decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            AsnReader sequenceReader = reader.ReadSequence(expectedTag);
            AsnReader defaultReader;
            
            System.Security.Cryptography.Asn1.Pbkdf2SaltChoice.Decode(sequenceReader, out decoded.Salt);

            if (!sequenceReader.TryReadInt32(out decoded.IterationCount))
            {
                sequenceReader.ThrowIfNotEmpty();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Integer))
            {

                if (sequenceReader.TryReadUInt8(out byte tmpKeyLength))
                {
                    decoded.KeyLength = tmpKeyLength;
                }
                else
                {
                    sequenceReader.ThrowIfNotEmpty();
                }

            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Sequence))
            {
                System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(sequenceReader, out decoded.Prf);
            }
            else
            {
                defaultReader = new AsnReader(s_defaultPrf, AsnEncodingRules.DER);
                System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(defaultReader, out decoded.Prf);
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
