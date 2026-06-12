// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;

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

#if NET
    public sealed class DSASignVerify_Span_Cng : DSASignVerify_Span
    {
        protected override DSAProvider DSAFactory { get; } = DSACngProvider.Instance;
    }
#endif

    public sealed class DsaArraySignatureFormatTests_Cng : DsaArraySignatureFormatTests
    {
        private static KeyDescription[] s_keys;

        protected override DSAProvider DSAFactory { get; } = DSACngProvider.Instance;
        protected override KeyDescription[] GenerateTestKeys() => s_keys ??= LocalGenerateTestKeys().ToArray();
    }

    public sealed class DsaArrayOffsetSignatureFormatTests_Cng : DsaArrayOffsetSignatureFormatTests
    {
        private static KeyDescription[] s_keys;

        protected override DSAProvider DSAFactory { get; } = DSACngProvider.Instance;
        protected override KeyDescription[] GenerateTestKeys() => s_keys ??= LocalGenerateTestKeys().ToArray();
    }

    public sealed class DsaSpanSignatureFormatTests_Cng : DsaSpanSignatureFormatTests
    {
        private static KeyDescription[] s_keys;

        protected override DSAProvider DSAFactory { get; } = DSACngProvider.Instance;
        protected override KeyDescription[] GenerateTestKeys() => s_keys ??= LocalGenerateTestKeys().ToArray();
    }
}
