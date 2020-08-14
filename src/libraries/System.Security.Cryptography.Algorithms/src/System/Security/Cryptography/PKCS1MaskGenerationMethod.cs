// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public class PKCS1MaskGenerationMethod : MaskGenerationMethod
    {
        private string _hashNameValue;
        private const string DefaultHash = "SHA1";

        public PKCS1MaskGenerationMethod()
        {
            _hashNameValue = DefaultHash;
        }

        public string HashName
        {
            get { return _hashNameValue; }
            set { _hashNameValue = value ?? DefaultHash; }
        }

        public override byte[] GenerateMask(byte[] rgbSeed, int cbReturn)
        {
            using (HashAlgorithm? hasher = CryptoConfig.CreateFromName(_hashNameValue) as HashAlgorithm)
            {
                if (hasher is null)
                {
                    throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, _hashNameValue));
                }

                byte[] rgbCounter = new byte[4];
                byte[] rgbT = new byte[cbReturn];

                uint counter = 0;
                for (int ib = 0; ib < rgbT.Length;)
                {
                    //  Increment counter -- up to 2^32 * sizeof(Hash)
                    BinaryPrimitives.WriteUInt32BigEndian(rgbCounter, counter++);
                    hasher.TransformBlock(rgbSeed, 0, rgbSeed.Length, rgbSeed, 0);
                    hasher.TransformFinalBlock(rgbCounter, 0, 4);
                    Debug.Assert(hasher.Hash != null);
                    byte[] hash = hasher.Hash;
                    hasher.Initialize();
                    Buffer.BlockCopy(hash, 0, rgbT, ib, Math.Min(rgbT.Length - ib, hash.Length));

                    ib += hasher.Hash.Length;
                }

                return rgbT;
            }
        }
    }
}
