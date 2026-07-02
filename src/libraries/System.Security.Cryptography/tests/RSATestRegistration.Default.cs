// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Rsa.Tests
{
    public sealed class SignVerify_Array_Default : SignVerify_Array
    {
        protected override RSAProvider RSAFactory => DefaultRSAProvider.Instance;
    }

    public sealed class SignVerify_AllocatingSpan_Default : SignVerify_AllocatingSpan
    {
        protected override RSAProvider RSAFactory => DefaultRSAProvider.Instance;
    }

    public sealed class SignVerify_Span_Default : SignVerify_Span
    {
        protected override RSAProvider RSAFactory => DefaultRSAProvider.Instance;
    }

    public sealed class SignVerify_TrySpan_Default : SignVerify_TrySpan
    {
        protected override RSAProvider RSAFactory => DefaultRSAProvider.Instance;
    }

    public sealed class RSASignatureFormatterTests_Default : RSASignatureFormatterTests
    {
        protected override RSAProvider RSAFactory => DefaultRSAProvider.Instance;
    }

    public sealed class EncryptDecrypt_Array_Default : EncryptDecrypt_Array
    {
        protected override RSAProvider RSAFactory => DefaultRSAProvider.Instance;
    }

    public sealed class EncryptDecrypt_Span_Default : EncryptDecrypt_Span
    {
        protected override RSAProvider RSAFactory => DefaultRSAProvider.Instance;
    }

    public sealed class EncryptDecrypt_AllocatingSpan_Default : EncryptDecrypt_AllocatingSpan
    {
        protected override RSAProvider RSAFactory => DefaultRSAProvider.Instance;
    }

    public sealed class EncryptDecrypt_TrySpan_Default : EncryptDecrypt_TrySpan
    {
        protected override RSAProvider RSAFactory => DefaultRSAProvider.Instance;
    }

    public sealed class RSAFactoryTests_Default : RSAFactoryTests
    {
        protected override RSAProvider RSAFactory => DefaultRSAProvider.Instance;
    }

    public sealed class ImportExport_Default : ImportExport
    {
        protected override RSAProvider RSAFactory => DefaultRSAProvider.Instance;
    }

    public sealed class RSAXml_Default : RSAXml
    {
        protected override RSAProvider RSAFactory => DefaultRSAProvider.Instance;
    }

    public sealed class RSAKeyFileTests_Default : RSAKeyFileTests
    {
        protected override RSAProvider RSAFactory => DefaultRSAProvider.Instance;
    }

    public sealed class RSAKeyPemTests_Default : RSAKeyPemTests
    {
        protected override RSAProvider RSAFactory => DefaultRSAProvider.Instance;
    }

    public sealed class RSAKeyExchangeFormatterTests_Default : RSAKeyExchangeFormatterTests
    {
        protected override RSAProvider RSAFactory => DefaultRSAProvider.Instance;
    }

    public sealed class KeyGeneration_Default : KeyGeneration
    {
        protected override RSAProvider RSAFactory => DefaultRSAProvider.Instance;
    }
}
