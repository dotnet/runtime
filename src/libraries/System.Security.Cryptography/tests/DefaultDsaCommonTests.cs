// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;

namespace System.Security.Cryptography.Dsa.Tests
{
    public sealed class DSAImportExport_Default : DSAImportExport
    {
        protected override DSAProvider DSAFactory { get; } = DefaultDSAProvider.Instance;
    }

    public sealed class DSAFactoryTests_Default : DSAFactoryTests
    {
        protected override DSAProvider DSAFactory { get; } = DefaultDSAProvider.Instance;
    }

    public sealed class DSAKeyGeneration_Default : DSAKeyGeneration
    {
        protected override DSAProvider DSAFactory { get; } = DefaultDSAProvider.Instance;
    }

    public sealed class DSAKeyFileTests_Default : DSAKeyFileTests
    {
        protected override DSAProvider DSAFactory { get; } = DefaultDSAProvider.Instance;
    }

    public sealed class DSAKeyPemTests_Default : DSAKeyPemTests
    {
        protected override DSAProvider DSAFactory { get; } = DefaultDSAProvider.Instance;
    }

    public sealed class DSAXml_Default : DSAXml
    {
        protected override DSAProvider DSAFactory { get; } = DefaultDSAProvider.Instance;
    }

    public sealed class DSASignatureFormatterTests_Default : DSASignatureFormatterTests
    {
        protected override DSAProvider DSAFactory { get; } = DefaultDSAProvider.Instance;
    }

    public sealed class DSASignVerify_Array_Default : DSASignVerify_Array
    {
        protected override DSAProvider DSAFactory { get; } = DefaultDSAProvider.Instance;
    }

    public sealed class DSASignVerify_Stream_Default : DSASignVerify_Stream
    {
        protected override DSAProvider DSAFactory { get; } = DefaultDSAProvider.Instance;
    }

#if NET
    public sealed class DSASignVerify_Span_Default : DSASignVerify_Span
    {
        protected override DSAProvider DSAFactory { get; } = DefaultDSAProvider.Instance;
    }
#endif

    public sealed class DsaArraySignatureFormatTests_Default : DsaArraySignatureFormatTests
    {
        private static KeyDescription[] s_keys;

        protected override DSAProvider DSAFactory { get; } = DefaultDSAProvider.Instance;
        protected override KeyDescription[] GenerateTestKeys() => s_keys ??= LocalGenerateTestKeys().ToArray();
    }

    public sealed class DsaArrayOffsetSignatureFormatTests_Default : DsaArrayOffsetSignatureFormatTests
    {
        private static KeyDescription[] s_keys;

        protected override DSAProvider DSAFactory { get; } = DefaultDSAProvider.Instance;
        protected override KeyDescription[] GenerateTestKeys() => s_keys ??= LocalGenerateTestKeys().ToArray();
    }

    public sealed class DsaSpanSignatureFormatTests_Default : DsaSpanSignatureFormatTests
    {
        private static KeyDescription[] s_keys;

        protected override DSAProvider DSAFactory { get; } = DefaultDSAProvider.Instance;
        protected override KeyDescription[] GenerateTestKeys() => s_keys ??= LocalGenerateTestKeys().ToArray();
    }
}
