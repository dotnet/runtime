// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    // The current RSA contracts require that users cast up to RSACryptoServiceProvider.
    // There is an expectation that Soon there will be some contract changes enabling RSA
    // to function as a standalone type, and then this wrapper will go away.
    public sealed partial class RSACryptoServiceProvider : RSA, ICspAsymmetricAlgorithm
    {
        private readonly RSAOpenSsl _defer;

        public RSACryptoServiceProvider()
        {
            _defer = new RSAOpenSsl();
        }

        public RSACryptoServiceProvider(int dwKeySize)
        {
            _defer = new RSAOpenSsl(dwKeySize);
        }

        public RSACryptoServiceProvider(int dwKeySize, CspParameters parameters)
        {
            throw new PlatformNotSupportedException();
        }

        public RSACryptoServiceProvider(CspParameters parameters)
        {
            throw new PlatformNotSupportedException();
        }

        public override int KeySize
        {
            get { return _defer.KeySize; }
            set { _defer.KeySize = value; }
        }

        public byte[] Decrypt(byte[] rgb, bool fOAEP)
        {
            return _defer.Decrypt(
                rgb,
                fOAEP ?
                    Interop.libcrypto.OpenSslRsaPadding.RSA_PKCS1_OAEP_PADDING :
                    Interop.libcrypto.OpenSslRsaPadding.RSA_PKCS1_PADDING);
        }

        public override byte[] DecryptValue(byte[] rgb)
        {
            return _defer.DecryptValue(rgb);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _defer.Dispose();
            }

            base.Dispose(disposing);
        }

        public byte[] Encrypt(byte[] rgb, bool fOAEP)
        {
            return _defer.Encrypt(
                rgb,
                fOAEP ?
                    Interop.libcrypto.OpenSslRsaPadding.RSA_PKCS1_OAEP_PADDING :
                    Interop.libcrypto.OpenSslRsaPadding.RSA_PKCS1_PADDING);
        }

        public override byte[] EncryptValue(byte[] rgb)
        {
            return _defer.EncryptValue(rgb);
        }

        public override RSAParameters ExportParameters(bool includePrivateParameters)
        {
            return _defer.ExportParameters(includePrivateParameters);
        }

        public override void ImportParameters(RSAParameters parameters)
        {
            _defer.ImportParameters(parameters);
        }

        public byte[] SignData(byte[] buffer, int offset, int count, object halg)
        {
            // 1964: Implement RSA Sign/Verify on Unix.
            throw new NotImplementedException();
        }

        public byte[] SignData(byte[] buffer, object halg)
        {
            throw new NotImplementedException();
        }

        public byte[] SignData(Stream inputStream, object halg)
        {
            throw new NotImplementedException();
        }

        public byte[] SignHash(byte[] rgbHash, string str)
        {
            throw new NotImplementedException();
        }

        public bool VerifyData(byte[] buffer, object halg, byte[] signature)
        {
            throw new NotImplementedException();
        }

        public bool VerifyHash(byte[] rgbHash, string str, byte[] rgbSignature)
        {
            throw new NotImplementedException();
        }

        public CspKeyContainerInfo CspKeyContainerInfo
        { 
            get
            {
                throw new PlatformNotSupportedException();
            } 
        }

        public void ImportCspBlob(byte[] keyBlob)
        {
            throw new PlatformNotSupportedException();
        }

        public byte[] ExportCspBlob(bool includePrivateParameters)
        {
            throw new PlatformNotSupportedException();
        }

        public bool PersistKeyInCsp
        {
            get
            {
                throw new PlatformNotSupportedException();
            } 
            set
            {
                throw new PlatformNotSupportedException();
            } 
        }

        public bool PublicOnly
        {
            get
            {
                return true;
            }
        }

        public static bool UseMachineKeyStore
        {
            get
            {
                throw new PlatformNotSupportedException();
            } 
            set
            {
                throw new PlatformNotSupportedException();
            } 
        }
    }
}
