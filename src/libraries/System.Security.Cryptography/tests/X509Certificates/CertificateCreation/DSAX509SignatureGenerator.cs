// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;
using System.Numerics;
using Test.Cryptography;

namespace System.Security.Cryptography.X509Certificates.Tests.CertificateCreation
{
    internal sealed class DSAX509SignatureGenerator : X509SignatureGenerator
    {
        private readonly DSA _key;

        public DSAX509SignatureGenerator(DSA key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            _key = key;
        }

        public override byte[] GetSignatureAlgorithmIdentifier(HashAlgorithmName hashAlgorithm)
        {
            if (hashAlgorithm == HashAlgorithmName.SHA1)
            {
                return "300906072A8648CE380403".HexToByteArray();
            }

            if (hashAlgorithm == HashAlgorithmName.SHA256)
            {
                return "300B0609608648016503040302".HexToByteArray();
            }

            throw new InvalidOperationException();
        }

        public override byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm)
        {
            return _key.SignData(data, hashAlgorithm, DSASignatureFormat.Rfc3279DerSequence);
        }

        protected override PublicKey BuildPublicKey()
        {
            // DSA
            Oid oid = new Oid("1.2.840.10040.4.1");

            DSAParameters dsaParameters = _key.ExportParameters(false);

            // Dss-Parms ::= SEQUENCE {
            //   p INTEGER,
            //   q INTEGER,
            //   g INTEGER
            // }

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            using (writer.PushSequence())
            {
                WriteUnsignedInteger(writer, dsaParameters.P);
                WriteUnsignedInteger(writer, dsaParameters.Q);
                WriteUnsignedInteger(writer, dsaParameters.G);
            }

            byte[] algParameters = writer.Encode();

            writer.Reset();
            WriteUnsignedInteger(writer, dsaParameters.Y);
            byte[] keyValue = writer.Encode();

            return new PublicKey(
                oid,
                new AsnEncodedData(oid, algParameters),
                new AsnEncodedData(oid, keyValue));
        }

        private static void WriteUnsignedInteger(AsnWriter writer, ReadOnlySpan<byte> value)
        {
            BigInteger integer = new BigInteger(value, isUnsigned: true, isBigEndian: true);
            writer.WriteInteger(integer);
        }
    }
}
