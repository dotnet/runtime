// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Rsa.Tests
{
    public sealed class SignVerify_Array_Cng : SignVerify_Array
    {
        protected override RSAProvider RSAFactory => RSACngProvider.Instance;
    }

    public sealed class SignVerify_AllocatingSpan_Cng : SignVerify_AllocatingSpan
    {
        protected override RSAProvider RSAFactory => RSACngProvider.Instance;
    }

    public sealed class SignVerify_Span_Cng : SignVerify_Span
    {
        protected override RSAProvider RSAFactory => RSACngProvider.Instance;
    }

    public sealed class SignVerify_TrySpan_Cng : SignVerify_TrySpan
    {
        protected override RSAProvider RSAFactory => RSACngProvider.Instance;
    }

    public sealed class RSASignatureFormatterTests_Cng : RSASignatureFormatterTests
    {
        protected override RSAProvider RSAFactory => RSACngProvider.Instance;
    }

    public sealed class EncryptDecrypt_Array_Cng : EncryptDecrypt_Array
    {
        protected override RSAProvider RSAFactory => RSACngProvider.Instance;
    }

    public sealed class EncryptDecrypt_Span_Cng : EncryptDecrypt_Span
    {
        protected override RSAProvider RSAFactory => RSACngProvider.Instance;
    }

    public sealed class EncryptDecrypt_AllocatingSpan_Cng : EncryptDecrypt_AllocatingSpan
    {
        protected override RSAProvider RSAFactory => RSACngProvider.Instance;
    }

    public sealed class EncryptDecrypt_TrySpan_Cng : EncryptDecrypt_TrySpan
    {
        protected override RSAProvider RSAFactory => RSACngProvider.Instance;
    }

    public sealed class RSAFactoryTests_Cng : RSAFactoryTests
    {
        protected override RSAProvider RSAFactory => RSACngProvider.Instance;
    }

    public sealed class ImportExport_Cng : ImportExport
    {
        protected override RSAProvider RSAFactory => RSACngProvider.Instance;
    }

    public sealed class RSAXml_Cng : RSAXml
    {
        protected override RSAProvider RSAFactory => RSACngProvider.Instance;
    }

    public sealed class RSAKeyFileTests_Cng : RSAKeyFileTests
    {
        protected override RSAProvider RSAFactory => RSACngProvider.Instance;
    }

    public sealed class RSAKeyPemTests_Cng : RSAKeyPemTests
    {
        protected override RSAProvider RSAFactory => RSACngProvider.Instance;
    }

    public sealed class RSAKeyExchangeFormatterTests_Cng : RSAKeyExchangeFormatterTests
    {
        protected override RSAProvider RSAFactory => RSACngProvider.Instance;
    }

    public sealed class KeyGeneration_Cng : KeyGeneration
    {
        protected override RSAProvider RSAFactory => RSACngProvider.Instance;
    }
}
