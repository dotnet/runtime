// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Tests;

namespace System.Security.Cryptography.Rsa.Tests
{
    public class RSAOpenSslProvider : IRSAProvider
    {
        private bool? _supportsSha1Signatures;
        private bool? _supportsMd5Signatures;

        public RSA Create() => new RSAOpenSsl();

        public RSA Create(int keySize) => new RSAOpenSsl(keySize);

        public bool Supports384PrivateKey => true;

        public bool SupportsLargeExponent => true;

        public bool SupportsSha2Oaep => true;

        public bool SupportsPss => true;

        public bool SupportsSha1Signatures => _supportsSha1Signatures ??= SignatureSupport.CanProduceSha1Signature(Create());
        public bool SupportsMd5Signatures => _supportsMd5Signatures ??= SignatureSupport.CanProduceMd5Signature(Create());

        public bool SupportsSha3 => SHA3_256.IsSupported; // If SHA3_256 is supported, assume 384 and 512 are, too.
    }

    // Concrete test classes for RSAOpenSslProvider
    public class RSAOpenSslImportExport : ImportExport<RSAOpenSslProvider> { }
    public class RSAOpenSslEncryptDecrypt_Array : EncryptDecrypt_Array<RSAOpenSslProvider> { }
    public class RSAOpenSslEncryptDecrypt_Span : EncryptDecrypt_Span<RSAOpenSslProvider> { }
    public class RSAOpenSslEncryptDecrypt_AllocatingSpan : EncryptDecrypt_AllocatingSpan<RSAOpenSslProvider> { }
    public class RSAOpenSslEncryptDecrypt_TrySpan : EncryptDecrypt_TrySpan<RSAOpenSslProvider> { }
    public class RSAOpenSslSignVerify_Array : SignVerify_Array<RSAOpenSslProvider> { }
    public class RSAOpenSslSignVerify_AllocatingSpan : SignVerify_AllocatingSpan<RSAOpenSslProvider> { }
    public class RSAOpenSslSignVerify_Span : SignVerify_Span<RSAOpenSslProvider> { }
    public class RSAOpenSslSignVerify_TrySpan : SignVerify_TrySpan<RSAOpenSslProvider> { }
    public class RSAOpenSslKeyGeneration : KeyGeneration<RSAOpenSslProvider> { }
    public class RSAOpenSslXml : RSAXml<RSAOpenSslProvider> { }
    public class RSAOpenSslSignatureFormatterTests : RSASignatureFormatterTests<RSAOpenSslProvider> { }
    public class RSAOpenSslKeyExchangeFormatterTests : RSAKeyExchangeFormatterTests<RSAOpenSslProvider> { }
    public class RSAOpenSslFactoryTests : RSAFactoryTests<RSAOpenSslProvider> { }
    public class RSAOpenSslKeyPemTests : RSAKeyPemTests<RSAOpenSslProvider> { }
    public class RSAOpenSslKeyFileTests : RSAKeyFileTests<RSAOpenSslProvider> { }
}
