// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;

namespace System.Security.Cryptography.Dsa.Tests
{
    public sealed class DSAImportExport_Csp : DSAImportExport
    {
        protected override DSAProvider DSAFactory { get; } = DSACryptoServiceProviderProvider.Instance;
    }

    public sealed class DSAFactoryTests_Csp : DSAFactoryTests
    {
        protected override DSAProvider DSAFactory { get; } = DSACryptoServiceProviderProvider.Instance;
    }

    public sealed class DSAKeyGeneration_Csp : DSAKeyGeneration
    {
        protected override DSAProvider DSAFactory { get; } = DSACryptoServiceProviderProvider.Instance;
    }

    public sealed class DSAKeyFileTests_Csp : DSAKeyFileTests
    {
        protected override DSAProvider DSAFactory { get; } = DSACryptoServiceProviderProvider.Instance;
    }

    public sealed class DSAKeyPemTests_Csp : DSAKeyPemTests
    {
        protected override DSAProvider DSAFactory { get; } = DSACryptoServiceProviderProvider.Instance;
    }

    public sealed class DSAXml_Csp : DSAXml
    {
        protected override DSAProvider DSAFactory { get; } = DSACryptoServiceProviderProvider.Instance;
    }

    public sealed class DSASignatureFormatterTests_Csp : DSASignatureFormatterTests
    {
        protected override DSAProvider DSAFactory { get; } = DSACryptoServiceProviderProvider.Instance;
    }

    public sealed class DSASignVerify_Array_Csp : DSASignVerify_Array
    {
        protected override DSAProvider DSAFactory { get; } = DSACryptoServiceProviderProvider.Instance;
    }

    public sealed class DSASignVerify_Stream_Csp : DSASignVerify_Stream
    {
        protected override DSAProvider DSAFactory { get; } = DSACryptoServiceProviderProvider.Instance;
    }

    public sealed class DSASignVerify_Span_Csp : DSASignVerify_Span
    {
        protected override DSAProvider DSAFactory { get; } = DSACryptoServiceProviderProvider.Instance;
    }

    public sealed class DsaArraySignatureFormatTests_Csp : DsaArraySignatureFormatTests
    {
        private static KeyDescription[] s_keys;

        protected override DSAProvider DSAFactory { get; } = DSACryptoServiceProviderProvider.Instance;
        protected override KeyDescription[] GenerateTestKeys() => s_keys ??= LocalGenerateTestKeys().ToArray();
    }

    public sealed class DsaArrayOffsetSignatureFormatTests_Csp : DsaArrayOffsetSignatureFormatTests
    {
        private static KeyDescription[] s_keys;

        protected override DSAProvider DSAFactory { get; } = DSACryptoServiceProviderProvider.Instance;
        protected override KeyDescription[] GenerateTestKeys() => s_keys ??= LocalGenerateTestKeys().ToArray();
    }

    public sealed class DsaSpanSignatureFormatTests_Csp : DsaSpanSignatureFormatTests
    {
        private static KeyDescription[] s_keys;

        protected override DSAProvider DSAFactory { get; } = DSACryptoServiceProviderProvider.Instance;
        protected override KeyDescription[] GenerateTestKeys() => s_keys ??= LocalGenerateTestKeys().ToArray();
    }
}
