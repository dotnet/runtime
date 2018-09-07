using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct RSAPrivateKeyAsn
    {
        internal byte Version;
        internal System.Numerics.BigInteger Modulus;
        internal System.Numerics.BigInteger PublicExponent;
        internal System.Numerics.BigInteger PrivateExponent;
        internal System.Numerics.BigInteger Prime1;
        internal System.Numerics.BigInteger Prime2;
        internal System.Numerics.BigInteger Exponent1;
        internal System.Numerics.BigInteger Exponent2;
        internal System.Numerics.BigInteger Coefficient;
      
        internal void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }
    
        internal void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);
            
            writer.WriteInteger(Version);
            writer.WriteInteger(Modulus);
            writer.WriteInteger(PublicExponent);
            writer.WriteInteger(PrivateExponent);
            writer.WriteInteger(Prime1);
            writer.WriteInteger(Prime2);
            writer.WriteInteger(Exponent1);
            writer.WriteInteger(Exponent2);
            writer.WriteInteger(Coefficient);
            writer.PopSequence(tag);
        }

        internal static RSAPrivateKeyAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }
        
        internal static RSAPrivateKeyAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, expectedTag, out RSAPrivateKeyAsn decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out RSAPrivateKeyAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Decode(reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(AsnReader reader, Asn1Tag expectedTag, out RSAPrivateKeyAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            AsnReader sequenceReader = reader.ReadSequence(expectedTag);
            

            if (!sequenceReader.TryReadUInt8(out decoded.Version))
            {
                sequenceReader.ThrowIfNotEmpty();
            }

            decoded.Modulus = sequenceReader.GetInteger();
            decoded.PublicExponent = sequenceReader.GetInteger();
            decoded.PrivateExponent = sequenceReader.GetInteger();
            decoded.Prime1 = sequenceReader.GetInteger();
            decoded.Prime2 = sequenceReader.GetInteger();
            decoded.Exponent1 = sequenceReader.GetInteger();
            decoded.Exponent2 = sequenceReader.GetInteger();
            decoded.Coefficient = sequenceReader.GetInteger();

            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
