// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal static partial class Helpers
    {
        [UnsupportedOSPlatformGuard("browser")]
        internal static bool HasSymmetricEncryption { get; } =
#if NETCOREAPP
            !OperatingSystem.IsBrowser();
#else
            true;
#endif

#if NETCOREAPP
        [UnsupportedOSPlatformGuard("ios")]
        [UnsupportedOSPlatformGuard("tvos")]
        public static bool IsDSASupported => !OperatingSystem.IsIOS() && !OperatingSystem.IsTvOS();
#else
        public static bool IsDSASupported => true;
#endif

#if NETCOREAPP
        [UnsupportedOSPlatformGuard("android")]
        [UnsupportedOSPlatformGuard("browser")]
        public static bool IsRC2Supported => !OperatingSystem.IsAndroid() && !OperatingSystem.IsBrowser();
#else
        public static bool IsRC2Supported => true;
#endif

        [UnsupportedOSPlatformGuard("browser")]
        internal static bool HasMD5 { get; } =
#if NETCOREAPP
            !OperatingSystem.IsBrowser();
#else
            true;
#endif

        [return: NotNullIfNotNull(nameof(src))]
        public static byte[]? CloneByteArray(this byte[]? src)
        {
            return src switch
            {
                null => null,
                { Length: 0 } => src,
                _ => (byte[])src.Clone(),
            };
        }

        internal static bool TryCopyToDestination(this ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            if (source.TryCopyTo(destination))
            {
                bytesWritten = source.Length;
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        internal static int HashOidToByteLength(string hashOid)
        {
            // This file is compiled in netstandard2.0, can't use the HashSizeInBytes consts.
            return hashOid switch
            {
                Oids.Sha256 => 256 >> 3,
                Oids.Sha384 => 384 >> 3,
                Oids.Sha512 => 512 >> 3,
                Oids.Sha1 => 160 >> 3,
                Oids.Md5 => 128 >> 3,
                _ => throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashOid)),
            };
        }
    }
}
