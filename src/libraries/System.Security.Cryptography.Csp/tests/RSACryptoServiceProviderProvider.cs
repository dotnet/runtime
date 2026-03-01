// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Tests;
using Test.Cryptography;

namespace System.Security.Cryptography.Rsa.Tests
{
    public class RSACryptoServiceProviderProvider : IRSAProvider
    {
        private bool? _supportsSha1Signatures;
        private bool? _supportsMd5Signatures;

        public RSA Create() => new RSACryptoServiceProvider();

        public RSA Create(int keySize) => new RSACryptoServiceProvider(keySize);

        public bool Supports384PrivateKey => PlatformSupport.IsRSA384Supported;

        public bool SupportsLargeExponent => false;

        public bool SupportsSha2Oaep => false;

        public bool SupportsPss => false;

        public bool SupportsSha1Signatures => _supportsSha1Signatures ??= SignatureSupport.CanProduceSha1Signature(Create());
        public bool SupportsMd5Signatures => _supportsMd5Signatures ??= SignatureSupport.CanProduceMd5Signature(Create());

        public bool SupportsSha3 => false;
    }

    // Concrete test classes for RSACryptoServiceProviderProvider
    public class RSACryptoServiceProviderImportExport : ImportExport<RSACryptoServiceProviderProvider> { }
    public class RSACryptoServiceProviderEncryptDecrypt_Array : EncryptDecrypt_Array<RSACryptoServiceProviderProvider> { }
    public class RSACryptoServiceProviderEncryptDecrypt_Span : EncryptDecrypt_Span<RSACryptoServiceProviderProvider> { }
    public class RSACryptoServiceProviderEncryptDecrypt_AllocatingSpan : EncryptDecrypt_AllocatingSpan<RSACryptoServiceProviderProvider> { }
    public class RSACryptoServiceProviderEncryptDecrypt_TrySpan : EncryptDecrypt_TrySpan<RSACryptoServiceProviderProvider> { }
    public class RSACryptoServiceProviderSignVerify_Array : SignVerify_Array<RSACryptoServiceProviderProvider> { }
    public class RSACryptoServiceProviderSignVerify_AllocatingSpan : SignVerify_AllocatingSpan<RSACryptoServiceProviderProvider> { }
    public class RSACryptoServiceProviderSignVerify_Span : SignVerify_Span<RSACryptoServiceProviderProvider> { }
    public class RSACryptoServiceProviderSignVerify_TrySpan : SignVerify_TrySpan<RSACryptoServiceProviderProvider> { }
    public class RSACryptoServiceProviderKeyGeneration : KeyGeneration<RSACryptoServiceProviderProvider> { }
    public class RSACryptoServiceProviderXml : RSAXml<RSACryptoServiceProviderProvider> { }
    public class RSACryptoServiceProviderSignatureFormatterTests : RSASignatureFormatterTests<RSACryptoServiceProviderProvider> { }
    public class RSACryptoServiceProviderKeyExchangeFormatterTests : RSAKeyExchangeFormatterTests<RSACryptoServiceProviderProvider> { }
    public class RSACryptoServiceProviderFactoryTests : RSAFactoryTests<RSACryptoServiceProviderProvider> { }
    public class RSACryptoServiceProviderKeyPemTests : RSAKeyPemTests<RSACryptoServiceProviderProvider> { }
    public class RSACryptoServiceProviderKeyFileTests : RSAKeyFileTests<RSACryptoServiceProviderProvider> { }
}
