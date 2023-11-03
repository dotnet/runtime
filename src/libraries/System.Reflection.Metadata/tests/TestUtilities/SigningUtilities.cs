// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Security.Cryptography;

namespace System.Reflection.PortableExecutable.Tests
{
    internal static class SigningUtilities
    {
        public static bool SupportsSigning { get; } =
            System.Security.Cryptography.Tests.SignatureSupport.CanProduceSha1Signature(RSA.Create());

        public static byte[] CalculateRsaSignature(IEnumerable<Blob> content, byte[] privateKey)
        {
            var hash = CalculateSha1(content);

            using (var rsa = RSA.Create())
            {
                rsa.ImportParameters(RSAParametersFromBlob(privateKey));
                var signature = rsa.SignHash(hash, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
                Array.Reverse(signature);
                return signature;
            }
        }

        public static byte[] CalculateSha1(IEnumerable<Blob> content)
        {
            MemoryStream stream = new();

            foreach (Blob blob in content)
            {
                var segment = blob.GetBytes();
                stream.Write(segment.Array, segment.Offset, segment.Count);
            }

            stream.Position = 0;

            using (SHA1 sha1 = SHA1.Create())
            {
                return sha1.ComputeHash(stream);
            }
        }

        private static RSAParameters RSAParametersFromBlob(byte[] blob)
        {
            RSAParameters key;

            var reader = new BR(blob);

            if (reader.ReadInt32() != 0x00000207)
                throw new CryptographicException("Private key expected");

            reader.ReadInt32(); // ALG_ID

            if (reader.ReadInt32() != 0x32415352) // 'RSA2'
                throw new CryptographicException("RSA key expected");

            int bitLen = reader.ReadInt32();
            if (bitLen % 16 != 0)
                throw new CryptographicException("Invalid bitLen");

            int byteLen = bitLen / 8;
            int halfLen = bitLen / 16;

            key.Exponent = reader.ReadBigInteger(4);
            key.Modulus = reader.ReadBigInteger(byteLen);
            key.P = reader.ReadBigInteger(halfLen);
            key.Q = reader.ReadBigInteger(halfLen);
            key.DP = reader.ReadBigInteger(halfLen);
            key.DQ = reader.ReadBigInteger(halfLen);
            key.InverseQ = reader.ReadBigInteger(halfLen);
            key.D = reader.ReadBigInteger(byteLen);

            return key;
        }

        struct BR
        {
            byte[] _blob;
            int _offset;

            public BR(byte[] blob)
            {
                _blob = blob;
                _offset = 0;
            }

            public int ReadInt32()
            {
                int offset = _offset;
                _offset = offset + 4;
                return _blob[offset] | (_blob[offset + 1] << 8) | (_blob[offset + 2] << 16) | (_blob[offset + 3] << 24);
            }

            public byte[] ReadBigInteger(int length)
            {
                byte[] arr = new byte[length];
                Array.Copy(_blob, _offset, arr, 0, length);
                _offset += length;
                Array.Reverse(arr);
                return arr;
            }
        }
    }
}
