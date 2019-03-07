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
    internal partial struct X509ExtensionAsn
    {
        private static byte[] s_defaultCritical = { 0x01, 0x01, 0x00 };
  
        internal Oid ExtnId;
        internal bool Critical;
        internal ReadOnlyMemory<byte> ExtnValue;
      
#if DEBUG  
        static X509ExtensionAsn()
        {
            X509ExtensionAsn decoded = default;
            AsnReader reader;

            reader = new AsnReader(s_defaultCritical, AsnEncodingRules.DER);
            decoded.Critical = reader.ReadBoolean();
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
            
            writer.WriteObjectIdentifier(ExtnId);
        
            // DEFAULT value handler for Critical.
            {
                using (AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER))
                {
                    tmp.WriteBoolean(Critical);
                    ReadOnlySpan<byte> encoded = tmp.EncodeAsSpan();

                    if (!encoded.SequenceEqual(s_defaultCritical))
                    {
                        writer.WriteEncodedValue(encoded.ToArray());
                    }
                }
            }

            writer.WriteOctetString(ExtnValue.Span);
            writer.PopSequence(tag);
        }

        internal static X509ExtensionAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }
        
        internal static X509ExtensionAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, expectedTag, out X509ExtensionAsn decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out X509ExtensionAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Decode(reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(AsnReader reader, Asn1Tag expectedTag, out X509ExtensionAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            AsnReader sequenceReader = reader.ReadSequence(expectedTag);
            AsnReader defaultReader;
            
            decoded.ExtnId = sequenceReader.ReadObjectIdentifier();

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Boolean))
            {
                decoded.Critical = sequenceReader.ReadBoolean();
            }
            else
            {
                defaultReader = new AsnReader(s_defaultCritical, AsnEncodingRules.DER);
                decoded.Critical = defaultReader.ReadBoolean();
            }


            if (sequenceReader.TryReadPrimitiveOctetStringBytes(out ReadOnlyMemory<byte> tmpExtnValue))
            {
                decoded.ExtnValue = tmpExtnValue;
            }
            else
            {
                decoded.ExtnValue = sequenceReader.ReadOctetString();
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
