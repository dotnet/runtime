// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.EcDsa.Tests
{
    public sealed class ECDsaTests_Array_OpenSsl : ECDsaTests_Array
    {
        protected override ECDsaProvider ECDsaFactory { get; } = ECDsaOpenSslProvider.Instance;
    }

    public sealed class ECDsaTests_Stream_OpenSsl : ECDsaTests_Stream
    {
        protected override ECDsaProvider ECDsaFactory { get; } = ECDsaOpenSslProvider.Instance;
    }

    public sealed class ECDsaTests_Span_OpenSsl : ECDsaTests_Span
    {
        protected override ECDsaProvider ECDsaFactory { get; } = ECDsaOpenSslProvider.Instance;
    }

    public sealed class ECDsaTests_AllocatingSpan_OpenSsl : ECDsaTests_AllocatingSpan
    {
        protected override ECDsaProvider ECDsaFactory { get; } = ECDsaOpenSslProvider.Instance;
    }

    public sealed class ECDsaTests_TrySpan_OpenSsl : ECDsaTests_TrySpan
    {
        protected override ECDsaProvider ECDsaFactory { get; } = ECDsaOpenSslProvider.Instance;
    }

    public sealed class ECDsaFactoryTests_OpenSsl : ECDsaFactoryTests
    {
        protected override ECDsaProvider ECDsaFactory { get; } = ECDsaOpenSslProvider.Instance;
    }

    public sealed class ECDsaImportExportTests_OpenSsl : ECDsaImportExportTests
    {
        protected override ECDsaProvider ECDsaFactory { get; } = ECDsaOpenSslProvider.Instance;
        protected override bool CanDeriveNewPublicKey { get; } = EcDiffieHellman.Tests.ECDiffieHellmanOpenSslProvider.Instance.CanDeriveNewPublicKey;
    }

    public sealed class ECDsaKeyFileTests_OpenSsl : ECDsaKeyFileTests
    {
        protected override ECDsaProvider ECDsaFactory { get; } = ECDsaOpenSslProvider.Instance;
        protected override bool CanDeriveNewPublicKey { get; } = EcDiffieHellman.Tests.ECDiffieHellmanOpenSslProvider.Instance.CanDeriveNewPublicKey;
    }

    public sealed class ECDsaXml_OpenSsl : ECDsaXml
    {
        protected override ECDsaProvider ECDsaFactory { get; } = ECDsaOpenSslProvider.Instance;
    }

    public sealed class ECDsaArraySignatureFormatTests_OpenSsl : ECDsaArraySignatureFormatTests
    {
        protected override ECDsaProvider ECDsaFactory { get; } = ECDsaOpenSslProvider.Instance;
    }

    public sealed class ECDsaArrayOffsetSignatureFormatTests_OpenSsl : ECDsaArrayOffsetSignatureFormatTests
    {
        protected override ECDsaProvider ECDsaFactory { get; } = ECDsaOpenSslProvider.Instance;
    }

    public sealed class ECDsaSpanSignatureFormatTests_OpenSsl : ECDsaSpanSignatureFormatTests
    {
        protected override ECDsaProvider ECDsaFactory { get; } = ECDsaOpenSslProvider.Instance;
    }
}
