// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Rsa.Tests
{
    public sealed class SignVerify_Array_Csp : SignVerify_Array
    {
        protected override RSAProvider RSAFactory => RSACryptoServiceProviderProvider.Instance;
    }

    public sealed class SignVerify_AllocatingSpan_Csp : SignVerify_AllocatingSpan
    {
        protected override RSAProvider RSAFactory => RSACryptoServiceProviderProvider.Instance;
    }

    public sealed class SignVerify_Span_Csp : SignVerify_Span
    {
        protected override RSAProvider RSAFactory => RSACryptoServiceProviderProvider.Instance;
    }

    public sealed class SignVerify_TrySpan_Csp : SignVerify_TrySpan
    {
        protected override RSAProvider RSAFactory => RSACryptoServiceProviderProvider.Instance;
    }

    public sealed class RSASignatureFormatterTests_Csp : RSASignatureFormatterTests
    {
        protected override RSAProvider RSAFactory => RSACryptoServiceProviderProvider.Instance;
    }

    public sealed class EncryptDecrypt_Array_Csp : EncryptDecrypt_Array
    {
        protected override RSAProvider RSAFactory => RSACryptoServiceProviderProvider.Instance;
    }

    public sealed class EncryptDecrypt_Span_Csp : EncryptDecrypt_Span
    {
        protected override RSAProvider RSAFactory => RSACryptoServiceProviderProvider.Instance;
    }

    public sealed class EncryptDecrypt_AllocatingSpan_Csp : EncryptDecrypt_AllocatingSpan
    {
        protected override RSAProvider RSAFactory => RSACryptoServiceProviderProvider.Instance;
    }

    public sealed class EncryptDecrypt_TrySpan_Csp : EncryptDecrypt_TrySpan
    {
        protected override RSAProvider RSAFactory => RSACryptoServiceProviderProvider.Instance;
    }

    public sealed class RSAFactoryTests_Csp : RSAFactoryTests
    {
        protected override RSAProvider RSAFactory => RSACryptoServiceProviderProvider.Instance;
    }

    public sealed class ImportExport_Csp : ImportExport
    {
        protected override RSAProvider RSAFactory => RSACryptoServiceProviderProvider.Instance;
    }

    public sealed class RSAXml_Csp : RSAXml
    {
        protected override RSAProvider RSAFactory => RSACryptoServiceProviderProvider.Instance;
    }

    public sealed class RSAKeyFileTests_Csp : RSAKeyFileTests
    {
        protected override RSAProvider RSAFactory => RSACryptoServiceProviderProvider.Instance;
    }

    public sealed class RSAKeyPemTests_Csp : RSAKeyPemTests
    {
        protected override RSAProvider RSAFactory => RSACryptoServiceProviderProvider.Instance;
    }

    public sealed class RSAKeyExchangeFormatterTests_Csp : RSAKeyExchangeFormatterTests
    {
        protected override RSAProvider RSAFactory => RSACryptoServiceProviderProvider.Instance;
    }

    public sealed class KeyGeneration_Csp : KeyGeneration
    {
        protected override RSAProvider RSAFactory => RSACryptoServiceProviderProvider.Instance;
    }
}
