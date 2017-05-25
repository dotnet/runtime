// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This file is one of a group of files (AesCng.cs, TripleDESCng.cs) that are almost identical except
// for the algorithm name. If you make a change to this file, there's a good chance you'll have to make
// the same change to the other files so please check. This is a pain but given that the contracts demand
// that each of these derive from a different class, it can't be helped.
// 

using Internal.Cryptography;
using Internal.NativeCrypto;

namespace System.Security.Cryptography
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350")] // We are providing the implementation for 3DES not consuming it
    public sealed class TripleDESCng : TripleDES, ICngSymmetricAlgorithm
    {
        public TripleDESCng()
        {
            _core = new CngSymmetricAlgorithmCore(this);
        }

        public TripleDESCng(string keyName)
            : this(keyName, CngProvider.MicrosoftSoftwareKeyStorageProvider)
        {
        }

        public TripleDESCng(string keyName, CngProvider provider)
            : this(keyName, provider, CngKeyOpenOptions.None)
        {
        }

        public TripleDESCng(string keyName, CngProvider provider, CngKeyOpenOptions openOptions)
        {
            _core = new CngSymmetricAlgorithmCore(this, keyName, provider, openOptions);
        }

        public override byte[] Key
        {
            get
            {
                return _core.GetKeyIfExportable();
            }
            set
            {
                _core.SetKey(value);
            }
        }

        public override int KeySize
        {
            get
            {
                return base.KeySize;
            }

            set
            {
                _core.SetKeySize(value, this);
            }
        }

        public override ICryptoTransform CreateDecryptor()
        {
            // Do not change to CreateDecryptor(this.Key, this.IV). this.Key throws if a non-exportable hardware key is being used.
            return _core.CreateDecryptor();
        }

        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
        {
            return _core.CreateDecryptor(rgbKey, rgbIV);
        }

        public override ICryptoTransform CreateEncryptor()
        {
            // Do not change to CreateEncryptor(this.Key, this.IV). this.Key throws if a non-exportable hardware key is being used.
            return _core.CreateEncryptor();
        }

        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
        {
            return _core.CreateEncryptor(rgbKey, rgbIV);
        }
 
        public override void GenerateKey()
        {
            _core.GenerateKey();
        }

        public override void GenerateIV()
        {
            _core.GenerateIV();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        byte[] ICngSymmetricAlgorithm.BaseKey { get { return base.Key; } set { base.Key = value; } }
        int ICngSymmetricAlgorithm.BaseKeySize { get { return base.KeySize; } set { base.KeySize = value; } }

        bool ICngSymmetricAlgorithm.IsWeakKey(byte[] key)
        {
            return TripleDES.IsWeakKey(key);
        }

        SafeAlgorithmHandle ICngSymmetricAlgorithm.GetEphemeralModeHandle()
        {
            return TripleDesBCryptModes.GetSharedHandle(Mode);
        }

        string ICngSymmetricAlgorithm.GetNCryptAlgorithmIdentifier()
        {
            return Interop.NCrypt.NCRYPT_3DES_ALGORITHM;
        }

        byte[] ICngSymmetricAlgorithm.PreprocessKey(byte[] key)
        {
            if (key.Length == 16)
            {
                // Cng does not support Two-Key Triple DES, so manually support it here for consistency with System.Security.Cryptography.Algorithms.
                // Two-Key Triple DES contains two 8-byte keys {K1}{K2} with {K1} appended to make {K1}{K2}{K1}.
                byte[] newkey = new byte[24];
                Array.Copy(key, 0, newkey, 0, 16);
                Array.Copy(key, 0, newkey, 16, 8);
                return newkey;
            }

            return key;
        }

        private CngSymmetricAlgorithmCore _core;
    }
}
