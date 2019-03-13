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
    internal partial struct OaepParamsAsn
    {
        private static byte[] s_defaultHashFunc = { 0x30, 0x09, 0x06, 0x05, 0x2B, 0x0E, 0x03, 0x02, 0x1A, 0x05, 0x00 };
  
        private static byte[] s_defaultMaskGenFunc = { 0x30, 0x16, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x08, 0x30, 0x09, 0x06, 0x05, 0x2B, 0x0E, 0x03, 0x02, 0x1A, 0x05, 0x00 };
  
        private static byte[] s_defaultPSourceFunc = { 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x09, 0x04, 0x00 };
  
        internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn HashFunc;
        internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn MaskGenFunc;
        internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn PSourceFunc;
      
#if DEBUG  
        static OaepParamsAsn()
        {
            OaepParamsAsn decoded = default;
            AsnReader reader;

            reader = new AsnReader(s_defaultHashFunc, AsnEncodingRules.DER);
            System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(reader, out decoded.HashFunc);
            reader.ThrowIfNotEmpty();

            reader = new AsnReader(s_defaultMaskGenFunc, AsnEncodingRules.DER);
            System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(reader, out decoded.MaskGenFunc);
            reader.ThrowIfNotEmpty();

            reader = new AsnReader(s_defaultPSourceFunc, AsnEncodingRules.DER);
            System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(reader, out decoded.PSourceFunc);
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
            

            // DEFAULT value handler for HashFunc.
            {
                using (AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER))
                {
                    HashFunc.Encode(tmp);
                    ReadOnlySpan<byte> encoded = tmp.EncodeAsSpan();

                    if (!encoded.SequenceEqual(s_defaultHashFunc))
                    {
                        writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                        writer.WriteEncodedValue(encoded.ToArray());
                        writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                    }
                }
            }


            // DEFAULT value handler for MaskGenFunc.
            {
                using (AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER))
                {
                    MaskGenFunc.Encode(tmp);
                    ReadOnlySpan<byte> encoded = tmp.EncodeAsSpan();

                    if (!encoded.SequenceEqual(s_defaultMaskGenFunc))
                    {
                        writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
                        writer.WriteEncodedValue(encoded.ToArray());
                        writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
                    }
                }
            }


            // DEFAULT value handler for PSourceFunc.
            {
                using (AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER))
                {
                    PSourceFunc.Encode(tmp);
                    ReadOnlySpan<byte> encoded = tmp.EncodeAsSpan();

                    if (!encoded.SequenceEqual(s_defaultPSourceFunc))
                    {
                        writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 2));
                        writer.WriteEncodedValue(encoded.ToArray());
                        writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 2));
                    }
                }
            }

            writer.PopSequence(tag);
        }

        internal static OaepParamsAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }
        
        internal static OaepParamsAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, expectedTag, out OaepParamsAsn decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out OaepParamsAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Decode(reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(AsnReader reader, Asn1Tag expectedTag, out OaepParamsAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            AsnReader sequenceReader = reader.ReadSequence(expectedTag);
            AsnReader explicitReader;
            AsnReader defaultReader;
            

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(explicitReader, out decoded.HashFunc);
                explicitReader.ThrowIfNotEmpty();
            }
            else
            {
                defaultReader = new AsnReader(s_defaultHashFunc, AsnEncodingRules.DER);
                System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(defaultReader, out decoded.HashFunc);
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {
                explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
                System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(explicitReader, out decoded.MaskGenFunc);
                explicitReader.ThrowIfNotEmpty();
            }
            else
            {
                defaultReader = new AsnReader(s_defaultMaskGenFunc, AsnEncodingRules.DER);
                System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(defaultReader, out decoded.MaskGenFunc);
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 2)))
            {
                explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 2));
                System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(explicitReader, out decoded.PSourceFunc);
                explicitReader.ThrowIfNotEmpty();
            }
            else
            {
                defaultReader = new AsnReader(s_defaultPSourceFunc, AsnEncodingRules.DER);
                System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(defaultReader, out decoded.PSourceFunc);
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
