// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    internal static class CngKeyExtensions
    {
        [SupportedOSPlatform("windows")]
        internal static CngKey Duplicate(this CngKey key)
        {
            using (SafeNCryptKeyHandle handle = key.Handle)
            {
                return CngHelpers.Duplicate(handle, key.IsEphemeral);
            }
        }

        internal static void SetExportPolicy(this CngKey key, CngExportPolicies exportPolicy)
        {
            using (SafeNCryptKeyHandle keyHandle = key.Handle)
            {
                CngHelpers.SetExportPolicy(keyHandle, exportPolicy);
            }
        }

        internal static string? GetPropertyAsString(
            this CngKey key,
            string propertyName,
            CngPropertyOptions options = CngPropertyOptions.None)
        {
            using (SafeNCryptKeyHandle keyHandle = key.Handle)
            {
                return CngHelpers.GetPropertyAsString(keyHandle, propertyName, options);
            }
        }

        internal static bool TryExportKeyBlob(
            this CngKey key,
            string blobType,
            Span<byte> destination,
            out int bytesWritten)
        {
            using (SafeNCryptKeyHandle keyHandle = key.Handle)
            {
                return keyHandle.TryExportKeyBlob(blobType, destination, out bytesWritten);
            }
        }

        internal static byte[] ExportPkcs8KeyBlob(
            this CngKey key,
            ReadOnlySpan<char> password,
            int kdfCount)
        {
            using (SafeNCryptKeyHandle keyHandle = key.Handle)
            {
                bool ret = CngHelpers.ExportPkcs8KeyBlob(
                    allocate: true,
                    keyHandle,
                    password,
                    kdfCount,
                    Span<byte>.Empty,
                    out _,
                    out byte[]? allocated);

                Debug.Assert(ret);
                Debug.Assert(allocated != null); // since `allocate: true`
                return allocated;
            }
        }
    }
}
