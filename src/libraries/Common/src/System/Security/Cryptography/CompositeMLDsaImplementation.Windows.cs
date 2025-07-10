// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

#pragma warning disable IDE0060 // Remove unused parameter

namespace System.Security.Cryptography
{
    internal sealed partial class CompositeMLDsaImplementation : CompositeMLDsa
    {
        public CompositeMLDsaImplementation(CompositeMLDsaAlgorithm algorithm)
            : base(algorithm)
        {
            throw new PlatformNotSupportedException();
        }

        internal static partial bool SupportsAny()
        {
#if !NETFRAMEWORK
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return false;
            }
#endif

            return CompositeMLDsaManaged.SupportsAny();
        }

        internal static partial bool IsAlgorithmSupportedImpl(CompositeMLDsaAlgorithm algorithm)
        {
#if !NETFRAMEWORK
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return false;
            }
#endif

            return CompositeMLDsaManaged.IsAlgorithmSupportedImpl(algorithm);
        }

        internal static partial CompositeMLDsa GenerateKeyImpl(CompositeMLDsaAlgorithm algorithm) =>
            throw new PlatformNotSupportedException();

        internal static partial CompositeMLDsa ImportCompositeMLDsaPublicKeyImpl(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
#if !NETFRAMEWORK
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new PlatformNotSupportedException();
            }
#endif

            return CompositeMLDsaManaged.ImportCompositeMLDsaPublicKeyImpl(algorithm, source);
        }


        internal static partial CompositeMLDsa ImportCompositeMLDsaPrivateKeyImpl(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
#if !NETFRAMEWORK
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new PlatformNotSupportedException();
            }
#endif

            return CompositeMLDsaManaged.ImportCompositeMLDsaPrivateKeyImpl(algorithm, source);
        }

        protected override bool TrySignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination, out int bytesWritten) =>
            throw new PlatformNotSupportedException();

        protected override bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            throw new PlatformNotSupportedException();

        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten) =>
            throw new PlatformNotSupportedException();

        protected override bool TryExportCompositeMLDsaPublicKeyCore(Span<byte> destination, out int bytesWritten) =>
            throw new PlatformNotSupportedException();

        protected override bool TryExportCompositeMLDsaPrivateKeyCore(Span<byte> destination, out int bytesWritten) =>
            throw new PlatformNotSupportedException();
    }
}
