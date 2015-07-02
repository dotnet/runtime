// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Reflection;
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
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException("offset");
            if (count < 0 || count > buffer.Length - offset)
                throw new ArgumentOutOfRangeException("count");

            HashAlgorithmName hashAlgorithmName = LookupHashAlgorithm(halg);

            byte[] hash = _defer.HashData(buffer, offset, count, hashAlgorithmName);
            return _defer.SignHash(hash, hashAlgorithmName);
        }

        public byte[] SignData(byte[] buffer, object halg)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            return SignData(buffer, 0, buffer.Length, halg);
        }

        public byte[] SignData(Stream inputStream, object halg)
        {
            if (inputStream == null)
                throw new ArgumentNullException("inputStream");

            HashAlgorithmName hashAlgorithmName = LookupHashAlgorithm(halg);

            byte[] hash = _defer.HashData(inputStream, hashAlgorithmName);
            return _defer.SignHash(hash, hashAlgorithmName);
        }

        public byte[] SignHash(byte[] rgbHash, string str)
        {
            if (rgbHash == null)
                throw new ArgumentNullException("rgbHash");

            HashAlgorithmName hashAlgorithmName = LookupHashAlgorithm(str);

            return _defer.SignHash(rgbHash, hashAlgorithmName);
        }

        public bool VerifyData(byte[] buffer, object halg, byte[] signature)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (signature == null)
                throw new ArgumentNullException("signature");

            HashAlgorithmName hashAlgorithmName = LookupHashAlgorithm(halg);
            byte[] hash = _defer.HashData(buffer, 0, buffer.Length, hashAlgorithmName);
            return _defer.VerifyHash(hash, signature, hashAlgorithmName);
        }

        public bool VerifyHash(byte[] rgbHash, string str, byte[] rgbSignature)
        {
            if (rgbHash == null)
                throw new ArgumentNullException("rgbHash");
            if (rgbSignature == null)
                throw new ArgumentNullException("rgbSignature");

            HashAlgorithmName hashAlgorithmName = LookupHashAlgorithm(str);
            return _defer.VerifyHash(rgbHash, rgbSignature, hashAlgorithmName);
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

        private static HashAlgorithmName LookupHashAlgorithm(object halg)
        {
            string algString = halg as string;

            if (algString != null)
            {
                return LookupHashAlgorithm(algString);
            }

            HashAlgorithm hashAlgorithmInstance = halg as HashAlgorithm;

            if (hashAlgorithmInstance != null)
            {
                return LookupHashAlgorithm(hashAlgorithmInstance);
            }

            Type hashAlgorithmType = halg as Type;

            if (hashAlgorithmType != null)
            {
                return LookupHashAlgorithm(hashAlgorithmType);
            }

            throw new ArgumentException(SR.Argument_InvalidValue);
        }

        private static HashAlgorithmName LookupHashAlgorithm(Type hashAlgorithmType)
        {
            TypeInfo hashAlgTypeInfo = hashAlgorithmType.GetTypeInfo();

            if (typeof(MD5).GetTypeInfo().IsAssignableFrom(hashAlgTypeInfo))
            {
                return HashAlgorithmName.MD5;
            }

            if (typeof(SHA1).GetTypeInfo().IsAssignableFrom(hashAlgTypeInfo))
            {
                return HashAlgorithmName.SHA1;
            }

            if (typeof(SHA256).GetTypeInfo().IsAssignableFrom(hashAlgTypeInfo))
            {
                return HashAlgorithmName.SHA256;
            }

            if (typeof(SHA384).GetTypeInfo().IsAssignableFrom(hashAlgTypeInfo))
            {
                return HashAlgorithmName.SHA384;
            }

            if (typeof(SHA512).GetTypeInfo().IsAssignableFrom(hashAlgTypeInfo))
            {
                return HashAlgorithmName.SHA512;
            }

            throw new ArgumentException(SR.Argument_InvalidValue);
        }

        private static HashAlgorithmName LookupHashAlgorithm(HashAlgorithm hashAlgorithm)
        {
            if (hashAlgorithm is MD5)
            {
                return HashAlgorithmName.MD5;
            }

            if (hashAlgorithm is SHA1)
            {
                return HashAlgorithmName.SHA1;
            }

            if (hashAlgorithm is SHA256)
            {
                return HashAlgorithmName.SHA256;
            }

            if (hashAlgorithm is SHA384)
            {
                return HashAlgorithmName.SHA384;
            }

            if (hashAlgorithm is SHA512)
            {
                return HashAlgorithmName.SHA512;
            }

            throw new ArgumentException(SR.Argument_InvalidValue);
        }

        private static HashAlgorithmName LookupHashAlgorithm(string algString)
        {
            const string Md5Oid = "1.2.840.113549.2.5";
            const string Sha1Oid = "1.3.14.3.2.26";
            const string Sha256Oid = "2.16.840.1.101.3.4.2.1";
            const string Sha384Oid = "2.16.840.1.101.3.4.2.2";
            const string Sha512Oid = "2.16.840.1.101.3.4.2.3";

            if (algString.Length > 0 && !char.IsDigit(algString[0]))
            {
                // If algString is an understood OID FriendlyName it will become the numeric OID,
                // otherwise it will remain as it was.
                algString = new Oid(algString).Value ?? algString;
            }

            switch (algString)
            {
                case Md5Oid:
                    return HashAlgorithmName.MD5;
                case Sha1Oid:
                    return HashAlgorithmName.SHA1;
                case Sha256Oid:
                    return HashAlgorithmName.SHA256;
                case Sha384Oid:
                    return HashAlgorithmName.SHA384;
                case Sha512Oid:
                    return HashAlgorithmName.SHA512;
            }

            throw new CryptographicException(SR.Cryptography_InvalidOID);
        }
    }
}
