// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;

namespace System.Security.Cryptography.Dsa.Tests
{
    public sealed class DSAImportExport_OpenSsl : DSAImportExport
    {
        protected override DSAProvider DSAFactory { get; } = DSAOpenSslProvider.Instance;
    }

    public sealed class DSAFactoryTests_OpenSsl : DSAFactoryTests
    {
        protected override DSAProvider DSAFactory { get; } = DSAOpenSslProvider.Instance;
    }

    public sealed class DSAKeyGeneration_OpenSsl : DSAKeyGeneration
    {
        protected override DSAProvider DSAFactory { get; } = DSAOpenSslProvider.Instance;
    }

    public sealed class DSAKeyFileTests_OpenSsl : DSAKeyFileTests
    {
        protected override DSAProvider DSAFactory { get; } = DSAOpenSslProvider.Instance;
    }

    public sealed class DSAKeyPemTests_OpenSsl : DSAKeyPemTests
    {
        protected override DSAProvider DSAFactory { get; } = DSAOpenSslProvider.Instance;
    }

    public sealed class DSAXml_OpenSsl : DSAXml
    {
        protected override DSAProvider DSAFactory { get; } = DSAOpenSslProvider.Instance;
    }

    public sealed class DSASignatureFormatterTests_OpenSsl : DSASignatureFormatterTests
    {
        protected override DSAProvider DSAFactory { get; } = DSAOpenSslProvider.Instance;
    }

    public sealed class DSASignVerify_Array_OpenSsl : DSASignVerify_Array
    {
        protected override DSAProvider DSAFactory { get; } = DSAOpenSslProvider.Instance;
    }

    public sealed class DSASignVerify_Stream_OpenSsl : DSASignVerify_Stream
    {
        protected override DSAProvider DSAFactory { get; } = DSAOpenSslProvider.Instance;
    }

    public sealed class DSASignVerify_Span_OpenSsl : DSASignVerify_Span
    {
        protected override DSAProvider DSAFactory { get; } = DSAOpenSslProvider.Instance;
    }

    public sealed class DsaArraySignatureFormatTests_OpenSsl : DsaArraySignatureFormatTests
    {
        private static KeyDescription[] s_keys;

        protected override DSAProvider DSAFactory { get; } = DSAOpenSslProvider.Instance;
        protected override KeyDescription[] GenerateTestKeys() => s_keys ??= LocalGenerateTestKeys().ToArray();
    }

    public sealed class DsaArrayOffsetSignatureFormatTests_OpenSsl : DsaArrayOffsetSignatureFormatTests
    {
        private static KeyDescription[] s_keys;

        protected override DSAProvider DSAFactory { get; } = DSAOpenSslProvider.Instance;
        protected override KeyDescription[] GenerateTestKeys() => s_keys ??= LocalGenerateTestKeys().ToArray();
    }

    public sealed class DsaSpanSignatureFormatTests_OpenSsl : DsaSpanSignatureFormatTests
    {
        private static KeyDescription[] s_keys;

        protected override DSAProvider DSAFactory { get; } = DSAOpenSslProvider.Instance;
        protected override KeyDescription[] GenerateTestKeys() => s_keys ??= LocalGenerateTestKeys().ToArray();
    }
}
