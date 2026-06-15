// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Dsa.Tests
{
    public sealed class DSAImportExport_Cng : DSAImportExport
    {
        protected override DSAProvider DSAFactory { get; } = DSACngProvider.Instance;
    }

    public sealed class DSAFactoryTests_Cng : DSAFactoryTests
    {
        protected override DSAProvider DSAFactory { get; } = DSACngProvider.Instance;
    }

    public sealed class DSAKeyGeneration_Cng : DSAKeyGeneration
    {
        protected override DSAProvider DSAFactory { get; } = DSACngProvider.Instance;
    }

    public sealed class DSAKeyFileTests_Cng : DSAKeyFileTests
    {
        protected override DSAProvider DSAFactory { get; } = DSACngProvider.Instance;
    }

    public sealed class DSAKeyPemTests_Cng : DSAKeyPemTests
    {
        protected override DSAProvider DSAFactory { get; } = DSACngProvider.Instance;
    }

    public sealed class DSAXml_Cng : DSAXml
    {
        protected override DSAProvider DSAFactory { get; } = DSACngProvider.Instance;
    }

    public sealed class DSASignatureFormatterTests_Cng : DSASignatureFormatterTests
    {
        protected override DSAProvider DSAFactory { get; } = DSACngProvider.Instance;
    }

    public sealed class DSASignVerify_Array_Cng : DSASignVerify_Array
    {
        protected override DSAProvider DSAFactory { get; } = DSACngProvider.Instance;
    }

    public sealed class DSASignVerify_Stream_Cng : DSASignVerify_Stream
    {
        protected override DSAProvider DSAFactory { get; } = DSACngProvider.Instance;
    }

    public sealed class DSASignVerify_Span_Cng : DSASignVerify_Span
    {
        protected override DSAProvider DSAFactory { get; } = DSACngProvider.Instance;
    }

    public sealed class DsaArraySignatureFormatTests_Cng : DsaArraySignatureFormatTests
    {
        protected override DSAProvider DSAFactory { get; } = DSACngProvider.Instance;
    }

    public sealed class DsaArrayOffsetSignatureFormatTests_Cng : DsaArrayOffsetSignatureFormatTests
    {
        protected override DSAProvider DSAFactory { get; } = DSACngProvider.Instance;
    }

    public sealed class DsaSpanSignatureFormatTests_Cng : DsaSpanSignatureFormatTests
    {
        protected override DSAProvider DSAFactory { get; } = DSACngProvider.Instance;
    }
}
