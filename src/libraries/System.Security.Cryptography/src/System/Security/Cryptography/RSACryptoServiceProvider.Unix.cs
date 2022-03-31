// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;
using System.IO;
using System.Runtime.Versioning;

namespace System.Security.Cryptography
{
    public sealed partial class RSACryptoServiceProvider : RSA, ICspAsymmetricAlgorithm, IRuntimeAlgorithm
    {
        private const int DefaultKeySize = 1024;

        private readonly RSA _impl;
        private bool _publicOnly;

        [UnsupportedOSPlatform("browser")]
        public RSACryptoServiceProvider()
            : this(DefaultKeySize) { }

        [UnsupportedOSPlatform("browser")]
        public RSACryptoServiceProvider(int dwKeySize)
        {
            if (dwKeySize < 0)
                throw new ArgumentOutOfRangeException(nameof(dwKeySize), SR.ArgumentOutOfRange_NeedNonNegNum);

            // This class wraps RSA
            _impl = RSA.Create(dwKeySize);
        }

        [SupportedOSPlatform("windows")]
        public RSACryptoServiceProvider(int dwKeySize, CspParameters parameters) =>
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CAPI_Required, nameof(CspParameters)));

        [SupportedOSPlatform("windows")]
        public RSACryptoServiceProvider(CspParameters parameters) =>
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CAPI_Required, nameof(CspParameters)));

        [SupportedOSPlatform("windows")]
        public CspKeyContainerInfo CspKeyContainerInfo =>
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CAPI_Required, nameof(CspKeyContainerInfo)));

        public byte[] Decrypt(byte[] rgb!!, bool fOAEP)
        {
            // size check -- must be exactly the modulus size
            if (rgb.Length != (KeySize / 8))
                throw new CryptographicException(SR.Cryptography_RSA_DecryptWrongSize);

            return _impl.Decrypt(rgb, fOAEP ? RSAEncryptionPadding.OaepSHA1 : RSAEncryptionPadding.Pkcs1);
        }

        public override byte[] Decrypt(byte[] data!!, RSAEncryptionPadding padding!!)
        {
            return
                padding == RSAEncryptionPadding.Pkcs1 ? Decrypt(data, fOAEP: false) :
                padding == RSAEncryptionPadding.OaepSHA1 ? Decrypt(data, fOAEP: true) : // For compat, this prevents OaepSHA2 options as fOAEP==true will cause Decrypt to use OaepSHA1
                throw PaddingModeNotSupported();
        }

        public override bool TryDecrypt(ReadOnlySpan<byte> data, Span<byte> destination, RSAEncryptionPadding padding!!, out int bytesWritten)
        {
            if (data.Length != (KeySize / 8))
                throw new CryptographicException(SR.Cryptography_RSA_DecryptWrongSize);
            if (padding != RSAEncryptionPadding.Pkcs1 && padding != RSAEncryptionPadding.OaepSHA1)
                throw PaddingModeNotSupported();

            return _impl.TryDecrypt(data, destination, padding, out bytesWritten);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _impl.Dispose();
                base.Dispose(disposing);
            }
        }

        public byte[] Encrypt(byte[] rgb!!, bool fOAEP)
        {
            return _impl.Encrypt(rgb, fOAEP ? RSAEncryptionPadding.OaepSHA1 : RSAEncryptionPadding.Pkcs1);
        }

        public override byte[] Encrypt(byte[] data!!, RSAEncryptionPadding padding!!)
        {
            return
                padding == RSAEncryptionPadding.Pkcs1 ? Encrypt(data, fOAEP: false) :
                padding == RSAEncryptionPadding.OaepSHA1 ?  Encrypt(data, fOAEP: true) : // For compat, this prevents OaepSHA2 options as fOAEP==true will cause Decrypt to use OaepSHA1
                throw PaddingModeNotSupported();
        }

        public override bool TryEncrypt(ReadOnlySpan<byte> data, Span<byte> destination, RSAEncryptionPadding padding!!, out int bytesWritten)
        {
            if (padding != RSAEncryptionPadding.Pkcs1 && padding != RSAEncryptionPadding.OaepSHA1)
                throw PaddingModeNotSupported();

            return _impl.TryEncrypt(data, destination, padding, out bytesWritten);
        }

        public byte[] ExportCspBlob(bool includePrivateParameters)
        {
            RSAParameters parameters = ExportParameters(includePrivateParameters);
            return parameters.ToKeyBlob();
        }

        public override RSAParameters ExportParameters(bool includePrivateParameters) =>
            _impl.ExportParameters(includePrivateParameters);

        public override void FromXmlString(string xmlString) => _impl.FromXmlString(xmlString);

        public void ImportCspBlob(byte[] keyBlob)
        {
            RSAParameters parameters = CapiHelper.ToRSAParameters(keyBlob, !IsPublic(keyBlob));
            ImportParameters(parameters);
        }

        public override void ImportParameters(RSAParameters parameters)
        {
            // Although _impl supports larger Exponent, limit here for compat.
            if (parameters.Exponent == null || parameters.Exponent.Length > 4)
                throw new CryptographicException(SR.Argument_InvalidValue);

            _impl.ImportParameters(parameters);

            // P was verified in ImportParameters
            _publicOnly = (parameters.P == null || parameters.P.Length == 0);
        }

        public override void ImportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            _impl.ImportEncryptedPkcs8PrivateKey(passwordBytes, source, out bytesRead);
        }

        public override void ImportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            _impl.ImportEncryptedPkcs8PrivateKey(password, source, out bytesRead);
        }

        public override string? KeyExchangeAlgorithm => _impl.KeyExchangeAlgorithm;

        public override int KeySize
        {
            get { return _impl.KeySize; }
            set { _impl.KeySize = value; }
        }

        // RSAOpenSsl is (512, 16384, 8), RSASecurityTransforms is (1024, 16384, 8)
        // Either way the minimum is lifted off of CAPI's 384, due to platform constraints.
        public override KeySizes[] LegalKeySizes => _impl.LegalKeySizes;

        // PersistKeyInCsp has no effect in Unix
        public bool PersistKeyInCsp { get; set; }

        public bool PublicOnly => _publicOnly;

        public override string SignatureAlgorithm => "http://www.w3.org/2000/09/xmldsig#rsa-sha1";

        public override byte[] SignData(Stream data, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding!!) =>
            padding != RSASignaturePadding.Pkcs1 ? throw PaddingModeNotSupported() :
            _impl.SignData(data, hashAlgorithm, padding);

        public override byte[] SignData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding!!) =>
            padding != RSASignaturePadding.Pkcs1 ? throw PaddingModeNotSupported() :
            _impl.SignData(data, offset, count, hashAlgorithm, padding);

        public override bool TrySignData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding!!, out int bytesWritten) =>
            padding != RSASignaturePadding.Pkcs1 ? throw PaddingModeNotSupported() :
            _impl.TrySignData(data, destination, hashAlgorithm, padding, out bytesWritten);

        public byte[] SignData(byte[] buffer, int offset, int count, object halg) =>
            _impl.SignData(buffer, offset, count, CapiHelper.ObjToHashAlgorithmName(halg), RSASignaturePadding.Pkcs1);

        public byte[] SignData(byte[] buffer, object halg) =>
            _impl.SignData(buffer, CapiHelper.ObjToHashAlgorithmName(halg), RSASignaturePadding.Pkcs1);

        public byte[] SignData(Stream inputStream, object halg) =>
            _impl.SignData(inputStream, CapiHelper.ObjToHashAlgorithmName(halg), RSASignaturePadding.Pkcs1);

        public override byte[] SignHash(byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding!!) =>
            padding != RSASignaturePadding.Pkcs1 ? throw PaddingModeNotSupported() :
            _impl.SignHash(hash, hashAlgorithm, padding);

        public override bool TrySignHash(ReadOnlySpan<byte> hash, Span<byte> destination, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding!!, out int bytesWritten) =>
            padding != RSASignaturePadding.Pkcs1 ? throw PaddingModeNotSupported() :
            _impl.TrySignHash(hash, destination, hashAlgorithm, padding, out bytesWritten);

        public byte[] SignHash(byte[] rgbHash!!, string str)
        {
            if (PublicOnly)
                throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);

            HashAlgorithmName algName = CapiHelper.NameOrOidToHashAlgorithmName(str);
            return _impl.SignHash(rgbHash, algName, RSASignaturePadding.Pkcs1);
        }

        public override string ToXmlString(bool includePrivateParameters) => _impl.ToXmlString(includePrivateParameters);

        public bool VerifyData(byte[] buffer, object halg, byte[] signature) =>
            _impl.VerifyData(buffer, signature, CapiHelper.ObjToHashAlgorithmName(halg), RSASignaturePadding.Pkcs1);

        public override bool VerifyData(byte[] data, int offset, int count, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding!!) =>
            padding != RSASignaturePadding.Pkcs1 ? throw PaddingModeNotSupported() :
            _impl.VerifyData(data, offset, count, signature, hashAlgorithm, padding);

        public override bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding!!) =>
            padding != RSASignaturePadding.Pkcs1 ? throw PaddingModeNotSupported() :
            _impl.VerifyData(data, signature, hashAlgorithm, padding);

        public override bool VerifyHash(byte[] hash!!, byte[] signature!!, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            return VerifyHash((ReadOnlySpan<byte>)hash, (ReadOnlySpan<byte>)signature, hashAlgorithm, padding);
        }

        public override bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding!!) =>
            padding != RSASignaturePadding.Pkcs1 ? throw PaddingModeNotSupported() :
            _impl.VerifyHash(hash, signature, hashAlgorithm, padding);

        public bool VerifyHash(byte[] rgbHash!!, string str, byte[] rgbSignature!!)
        {
            return VerifyHash(
                (ReadOnlySpan<byte>)rgbHash, (ReadOnlySpan<byte>)rgbSignature,
                CapiHelper.NameOrOidToHashAlgorithmName(str), RSASignaturePadding.Pkcs1);
        }

        // UseMachineKeyStore has no effect in Unix
        public static bool UseMachineKeyStore { get; set; }

        private static Exception PaddingModeNotSupported()
        {
            return new CryptographicException(SR.Cryptography_InvalidPaddingMode);
        }

        /// <summary>
        /// find whether an RSA key blob is public.
        /// </summary>
        private static bool IsPublic(byte[] keyBlob!!)
        {
            // The CAPI RSA public key representation consists of the following sequence:
            //  - BLOBHEADER
            //  - RSAPUBKEY

            // The first should be PUBLICKEYBLOB and magic should be RSA_PUB_MAGIC "RSA1"
            if (keyBlob[0] != CapiHelper.PUBLICKEYBLOB)
            {
                return false;
            }

            if (keyBlob[11] != 0x31 || keyBlob[10] != 0x41 || keyBlob[9] != 0x53 || keyBlob[8] != 0x52)
            {
                return false;
            }

            return true;
        }
    }
}
