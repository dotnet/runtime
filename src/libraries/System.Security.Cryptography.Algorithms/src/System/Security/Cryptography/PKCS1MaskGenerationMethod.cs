// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public class PKCS1MaskGenerationMethod : MaskGenerationMethod
    {
        private readonly string _hashNameValue;
        
        public PKCS1MaskGenerationMethod()
        {
            _hashNameValue = "SHA1";
        }

        public string HashName
        {
            get { return _hashNameValue; }
            set 
            {
                if (value != null && value != "SHA1")
                {
                    throw new PlatformNotSupportedException();
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "CryptoConfig.CreateFromName may return platform-dependent objects, so this implementation is limited to SHA-1 for now")]
        public override byte[] GenerateMask(byte[] rgbSeed, int cbReturn)
        {
            using (HashAlgorithm hasher = SHA1.Create())
            {
                byte[] rgbCounter = new byte[4];
                byte[] rgbT = new byte[cbReturn];

                uint counter = 0;
                for (int ib = 0; ib < rgbT.Length;)
                {
                    //  Increment counter -- up to 2^32 * sizeof(Hash)
                    Helpers.ConvertIntToByteArray(counter++, rgbCounter);
                    hasher.TransformBlock(rgbSeed, 0, rgbSeed.Length, rgbSeed, 0);
                    hasher.TransformFinalBlock(rgbCounter, 0, 4);
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
