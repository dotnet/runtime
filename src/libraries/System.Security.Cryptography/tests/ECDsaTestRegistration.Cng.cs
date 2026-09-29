// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.EcDsa.Tests
{
    public sealed class ECDsaTests_Array_Cng : ECDsaTests_Array
    {
        protected override ECDsaProvider ECDsaFactory { get; } = ECDsaCngProvider.Instance;
    }

    public sealed class ECDsaTests_Stream_Cng : ECDsaTests_Stream
    {
        protected override ECDsaProvider ECDsaFactory { get; } = ECDsaCngProvider.Instance;
    }

    public sealed class ECDsaTests_Span_Cng : ECDsaTests_Span
    {
        protected override ECDsaProvider ECDsaFactory { get; } = ECDsaCngProvider.Instance;
    }

    public sealed class ECDsaTests_AllocatingSpan_Cng : ECDsaTests_AllocatingSpan
    {
        protected override ECDsaProvider ECDsaFactory { get; } = ECDsaCngProvider.Instance;
    }

    public sealed class ECDsaTests_TrySpan_Cng : ECDsaTests_TrySpan
    {
        protected override ECDsaProvider ECDsaFactory { get; } = ECDsaCngProvider.Instance;
    }

    public sealed class ECDsaFactoryTests_Cng : ECDsaFactoryTests
    {
        protected override ECDsaProvider ECDsaFactory { get; } = ECDsaCngProvider.Instance;
    }

    public sealed class ECDsaImportExportTests_Cng : ECDsaImportExportTests
    {
        protected override ECDsaProvider ECDsaFactory { get; } = ECDsaCngProvider.Instance;
        protected override bool CanDeriveNewPublicKey { get; } = EcDiffieHellman.Tests.ECDiffieHellmanCngProvider.Instance.CanDeriveNewPublicKey;
    }

    public sealed class ECDsaKeyFileTests_Cng : ECDsaKeyFileTests
    {
        protected override ECDsaProvider ECDsaFactory { get; } = ECDsaCngProvider.Instance;
        protected override bool CanDeriveNewPublicKey { get; } = EcDiffieHellman.Tests.ECDiffieHellmanCngProvider.Instance.CanDeriveNewPublicKey;
    }

    public sealed class ECDsaXml_Cng : ECDsaXml
    {
        protected override ECDsaProvider ECDsaFactory { get; } = ECDsaCngProvider.Instance;
    }

    public sealed class ECDsaArraySignatureFormatTests_Cng : ECDsaArraySignatureFormatTests
    {
        protected override ECDsaProvider ECDsaFactory { get; } = ECDsaCngProvider.Instance;
    }

    public sealed class ECDsaArrayOffsetSignatureFormatTests_Cng : ECDsaArrayOffsetSignatureFormatTests
    {
        protected override ECDsaProvider ECDsaFactory { get; } = ECDsaCngProvider.Instance;
    }

    public sealed class ECDsaSpanSignatureFormatTests_Cng : ECDsaSpanSignatureFormatTests
    {
        protected override ECDsaProvider ECDsaFactory { get; } = ECDsaCngProvider.Instance;
    }
}
