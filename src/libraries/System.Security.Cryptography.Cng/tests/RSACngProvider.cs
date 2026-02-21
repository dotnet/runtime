// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Test.Cryptography;

namespace System.Security.Cryptography.Rsa.Tests
{
    public class RSACngProvider : IRSAProvider
    {
        public RSA Create() => new RSACng();

        public RSA Create(int keySize) => new RSACng(keySize);

        public bool Supports384PrivateKey => PlatformSupport.IsRSA384Supported;

        public bool SupportsLargeExponent => true;

        public bool SupportsSha2Oaep => true;

        public bool SupportsPss => true;

        public bool SupportsSha1Signatures => true;

        public bool SupportsMd5Signatures => true;

        public bool SupportsSha3 { get; } = SHA3_256.IsSupported; // If SHA3_256 is supported, assume 384 and 512 are, too.
    }

    // Concrete test classes for RSACngProvider
    public class RSACngImportExport : ImportExport<RSACngProvider> { }
    public class RSACngEncryptDecrypt_Array : EncryptDecrypt_Array<RSACngProvider> { }
    public class RSACngEncryptDecrypt_Span : EncryptDecrypt_Span<RSACngProvider> { }
    public class RSACngEncryptDecrypt_AllocatingSpan : EncryptDecrypt_AllocatingSpan<RSACngProvider> { }
    public class RSACngEncryptDecrypt_TrySpan : EncryptDecrypt_TrySpan<RSACngProvider> { }
    public class RSACngSignVerify_Array : SignVerify_Array<RSACngProvider> { }
    public class RSACngSignVerify_AllocatingSpan : SignVerify_AllocatingSpan<RSACngProvider> { }
    public class RSACngSignVerify_Span : SignVerify_Span<RSACngProvider> { }
    public class RSACngSignVerify_TrySpan : SignVerify_TrySpan<RSACngProvider> { }
    public class RSACngKeyGeneration : KeyGeneration<RSACngProvider> { }
    public class RSACngXml : RSAXml<RSACngProvider> { }
    public class RSACngSignatureFormatterTests : RSASignatureFormatterTests<RSACngProvider> { }
    public class RSACngKeyExchangeFormatterTests : RSAKeyExchangeFormatterTests<RSACngProvider> { }
    public class RSACngFactoryTests : RSAFactoryTests<RSACngProvider> { }
    public class RSACngKeyPemTests : RSAKeyPemTests<RSACngProvider> { }
    public class RSACngKeyFileTests : RSAKeyFileTests<RSACngProvider> { }
}
