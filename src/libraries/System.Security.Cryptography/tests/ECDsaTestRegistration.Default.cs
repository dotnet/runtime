// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.EcDsa.Tests
{
    public sealed class ECDsaTests_Array_Default : ECDsaTests_Array
    {
        protected override ECDsaProvider ECDsaFactory { get; } = DefaultECDsaProvider.Instance;
    }

    public sealed class ECDsaTests_Stream_Default : ECDsaTests_Stream
    {
        protected override ECDsaProvider ECDsaFactory { get; } = DefaultECDsaProvider.Instance;
    }

    public sealed class ECDsaTests_Span_Default : ECDsaTests_Span
    {
        protected override ECDsaProvider ECDsaFactory { get; } = DefaultECDsaProvider.Instance;
    }

    public sealed class ECDsaTests_AllocatingSpan_Default : ECDsaTests_AllocatingSpan
    {
        protected override ECDsaProvider ECDsaFactory { get; } = DefaultECDsaProvider.Instance;
    }

    public sealed class ECDsaTests_TrySpan_Default : ECDsaTests_TrySpan
    {
        protected override ECDsaProvider ECDsaFactory { get; } = DefaultECDsaProvider.Instance;
    }

    public sealed class ECDsaFactoryTests_Default : ECDsaFactoryTests
    {
        protected override ECDsaProvider ECDsaFactory { get; } = DefaultECDsaProvider.Instance;
    }

    public sealed class ECDsaImportExportTests_Default : ECDsaImportExportTests
    {
        protected override ECDsaProvider ECDsaFactory { get; } = DefaultECDsaProvider.Instance;
        protected override bool CanDeriveNewPublicKey { get; } = EcDiffieHellman.Tests.DefaultECDiffieHellmanProvider.Instance.CanDeriveNewPublicKey;
    }

    public sealed class ECDsaKeyFileTests_Default : ECDsaKeyFileTests
    {
        protected override ECDsaProvider ECDsaFactory { get; } = DefaultECDsaProvider.Instance;
        protected override bool CanDeriveNewPublicKey { get; } = EcDiffieHellman.Tests.DefaultECDiffieHellmanProvider.Instance.CanDeriveNewPublicKey;
    }

    public sealed class ECDsaXml_Default : ECDsaXml
    {
        protected override ECDsaProvider ECDsaFactory { get; } = DefaultECDsaProvider.Instance;
    }

    public sealed class ECDsaArraySignatureFormatTests_Default : ECDsaArraySignatureFormatTests
    {
        protected override ECDsaProvider ECDsaFactory { get; } = DefaultECDsaProvider.Instance;
    }

    public sealed class ECDsaArrayOffsetSignatureFormatTests_Default : ECDsaArrayOffsetSignatureFormatTests
    {
        protected override ECDsaProvider ECDsaFactory { get; } = DefaultECDsaProvider.Instance;
    }

    public sealed class ECDsaSpanSignatureFormatTests_Default : ECDsaSpanSignatureFormatTests
    {
        protected override ECDsaProvider ECDsaFactory { get; } = DefaultECDsaProvider.Instance;
    }
}
