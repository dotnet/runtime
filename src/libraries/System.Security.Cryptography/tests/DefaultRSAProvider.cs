// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Tests;
using Test.Cryptography;

namespace System.Security.Cryptography.Rsa.Tests
{
    public class DefaultRSAProvider : IRSAProvider
    {
        private bool? _supportsSha1Signatures;
        private bool? _supportsMd5Signatures;

        public RSA Create() => RSA.Create();

        public RSA Create(int keySize)
        {
#if NET
            return RSA.Create(keySize);
#else
            RSA rsa = Create();

            rsa.KeySize = keySize;
            return rsa;
#endif
        }

        public bool Supports384PrivateKey => PlatformSupport.IsRSA384Supported;
        public bool SupportsSha1Signatures => _supportsSha1Signatures ??= SignatureSupport.CanProduceSha1Signature(Create());
        public bool SupportsMd5Signatures => _supportsMd5Signatures ??= SignatureSupport.CanProduceMd5Signature(Create());

        public bool SupportsLargeExponent => true;

        public bool SupportsSha2Oaep { get; } = true;

        public bool SupportsPss { get; } = true;

        public bool SupportsSha3 { get; } = SHA3_256.IsSupported; // If SHA3_256 is supported, assume 384 and 512 are, too.
    }

    // Concrete test classes for DefaultRSAProvider
    public class DefaultRSAImportExport : ImportExport<DefaultRSAProvider> { }
    public class DefaultRSAEncryptDecrypt_Array : EncryptDecrypt_Array<DefaultRSAProvider> { }
    public class DefaultRSAEncryptDecrypt_Span : EncryptDecrypt_Span<DefaultRSAProvider> { }
    public class DefaultRSAEncryptDecrypt_AllocatingSpan : EncryptDecrypt_AllocatingSpan<DefaultRSAProvider> { }
    public class DefaultRSAEncryptDecrypt_TrySpan : EncryptDecrypt_TrySpan<DefaultRSAProvider> { }
    public class DefaultRSASignVerify_Array : SignVerify_Array<DefaultRSAProvider> { }
    public class DefaultRSASignVerify_AllocatingSpan : SignVerify_AllocatingSpan<DefaultRSAProvider> { }
    public class DefaultRSASignVerify_Span : SignVerify_Span<DefaultRSAProvider> { }
    public class DefaultRSASignVerify_TrySpan : SignVerify_TrySpan<DefaultRSAProvider> { }
    public class DefaultRSAKeyGeneration : KeyGeneration<DefaultRSAProvider> { }
    public class DefaultRSAXml : RSAXml<DefaultRSAProvider> { }
    public class DefaultRSASignatureFormatterTests : RSASignatureFormatterTests<DefaultRSAProvider> { }
    public class DefaultRSAKeyExchangeFormatterTests : RSAKeyExchangeFormatterTests<DefaultRSAProvider> { }
    public class DefaultRSAFactoryTests : RSAFactoryTests<DefaultRSAProvider> { }
    public class DefaultRSAKeyFileTests : RSAKeyFileTests<DefaultRSAProvider> { }
    public class DefaultRSAKeyPemTests : RSAKeyPemTests<DefaultRSAProvider> { }
}
