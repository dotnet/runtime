// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Rsa.Tests
{
    public sealed class SignVerify_Array_OpenSsl : SignVerify_Array
    {
        protected override RSAProvider RSAFactory => RSAOpenSslProvider.Instance;
    }

    public sealed class SignVerify_AllocatingSpan_OpenSsl : SignVerify_AllocatingSpan
    {
        protected override RSAProvider RSAFactory => RSAOpenSslProvider.Instance;
    }

    public sealed class SignVerify_Span_OpenSsl : SignVerify_Span
    {
        protected override RSAProvider RSAFactory => RSAOpenSslProvider.Instance;
    }

    public sealed class SignVerify_TrySpan_OpenSsl : SignVerify_TrySpan
    {
        protected override RSAProvider RSAFactory => RSAOpenSslProvider.Instance;
    }

    public sealed class RSASignatureFormatterTests_OpenSsl : RSASignatureFormatterTests
    {
        protected override RSAProvider RSAFactory => RSAOpenSslProvider.Instance;
    }

    public sealed class EncryptDecrypt_Array_OpenSsl : EncryptDecrypt_Array
    {
        protected override RSAProvider RSAFactory => RSAOpenSslProvider.Instance;
    }

    public sealed class EncryptDecrypt_Span_OpenSsl : EncryptDecrypt_Span
    {
        protected override RSAProvider RSAFactory => RSAOpenSslProvider.Instance;
    }

    public sealed class EncryptDecrypt_AllocatingSpan_OpenSsl : EncryptDecrypt_AllocatingSpan
    {
        protected override RSAProvider RSAFactory => RSAOpenSslProvider.Instance;
    }

    public sealed class EncryptDecrypt_TrySpan_OpenSsl : EncryptDecrypt_TrySpan
    {
        protected override RSAProvider RSAFactory => RSAOpenSslProvider.Instance;
    }

    public sealed class RSAFactoryTests_OpenSsl : RSAFactoryTests
    {
        protected override RSAProvider RSAFactory => RSAOpenSslProvider.Instance;
    }

    public sealed class ImportExport_OpenSsl : ImportExport
    {
        protected override RSAProvider RSAFactory => RSAOpenSslProvider.Instance;
    }

    public sealed class RSAXml_OpenSsl : RSAXml
    {
        protected override RSAProvider RSAFactory => RSAOpenSslProvider.Instance;
    }

    public sealed class RSAKeyFileTests_OpenSsl : RSAKeyFileTests
    {
        protected override RSAProvider RSAFactory => RSAOpenSslProvider.Instance;
    }

    public sealed class RSAKeyPemTests_OpenSsl : RSAKeyPemTests
    {
        protected override RSAProvider RSAFactory => RSAOpenSslProvider.Instance;
    }

    public sealed class RSAKeyExchangeFormatterTests_OpenSsl : RSAKeyExchangeFormatterTests
    {
        protected override RSAProvider RSAFactory => RSAOpenSslProvider.Instance;
    }

    public sealed class KeyGeneration_OpenSsl : KeyGeneration
    {
        protected override RSAProvider RSAFactory => RSAOpenSslProvider.Instance;
    }
}
