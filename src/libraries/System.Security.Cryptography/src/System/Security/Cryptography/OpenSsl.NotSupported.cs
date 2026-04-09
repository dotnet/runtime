// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Security.Cryptography
{
    public sealed class DSAOpenSsl : DSA
    {
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public DSAOpenSsl()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public DSAOpenSsl(int keySize)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public DSAOpenSsl(IntPtr handle)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public DSAOpenSsl(DSAParameters parameters)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public DSAOpenSsl(SafeEvpPKeyHandle pkeyHandle)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);
        }

        public SafeEvpPKeyHandle DuplicateKeyHandle() => null!;
        public override byte[] CreateSignature(byte[] rgbHash) => null!;
        public override DSAParameters ExportParameters(bool includePrivateParameters) => default;
        public override void ImportParameters(DSAParameters parameters) { }
        public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature) => false;
    }
    public sealed class ECDiffieHellmanOpenSsl : ECDiffieHellman
    {
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public ECDiffieHellmanOpenSsl()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public ECDiffieHellmanOpenSsl(int keySize)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public ECDiffieHellmanOpenSsl(IntPtr handle)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public ECDiffieHellmanOpenSsl(ECCurve curve)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public ECDiffieHellmanOpenSsl(SafeEvpPKeyHandle pkeyHandle)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);
        }

        public SafeEvpPKeyHandle DuplicateKeyHandle() => null!;
        public override ECDiffieHellmanPublicKey PublicKey => null!;
        public override ECParameters ExportParameters(bool includePrivateParameters) => default;
        public override void ImportParameters(ECParameters parameters) { }
    }
    public sealed class ECDsaOpenSsl : ECDsa
    {
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public ECDsaOpenSsl()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public ECDsaOpenSsl(int keySize)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public ECDsaOpenSsl(IntPtr handle)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public ECDsaOpenSsl(ECCurve curve)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public ECDsaOpenSsl(SafeEvpPKeyHandle pkeyHandle)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);
        }

        public SafeEvpPKeyHandle DuplicateKeyHandle() => null!;
        public override byte[] SignHash(byte[] hash) => null!;
        public override bool VerifyHash(byte[] hash, byte[] signature) => default;
    }
    public sealed class RSAOpenSsl : RSA
    {
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public RSAOpenSsl()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public RSAOpenSsl(int keySize)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public RSAOpenSsl(IntPtr handle)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public RSAOpenSsl(RSAParameters parameters)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public RSAOpenSsl(SafeEvpPKeyHandle pkeyHandle)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);
        }

        public SafeEvpPKeyHandle DuplicateKeyHandle() => null!;
        public override RSAParameters ExportParameters(bool includePrivateParameters) => default;
        public override void ImportParameters(RSAParameters parameters) { }
    }
    public sealed class SafeEvpPKeyHandle : SafeHandle
    {
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public SafeEvpPKeyHandle() : base(IntPtr.Zero, false)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public SafeEvpPKeyHandle(IntPtr handle, bool ownsHandle) : base(handle, ownsHandle)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public static long OpenSslVersion =>
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public static SafeEvpPKeyHandle OpenPrivateKeyFromEngine(string engineName, string keyId) =>
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public static SafeEvpPKeyHandle OpenPublicKeyFromEngine(string engineName, string keyId) =>
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public static SafeEvpPKeyHandle OpenKeyFromProvider(string providerName, string keyUri) =>
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);

        public SafeEvpPKeyHandle DuplicateHandle() => null!;
        public override bool IsInvalid => true;
        protected override bool ReleaseHandle() => false;
    }
}
