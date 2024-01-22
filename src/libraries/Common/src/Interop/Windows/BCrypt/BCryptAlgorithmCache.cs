// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class BCrypt
    {
        internal static class BCryptAlgorithmCache
        {
            private static readonly ConcurrentDictionary<(string HashAlgorithmId, BCryptOpenAlgorithmProviderFlags Flags), (SafeBCryptAlgorithmHandle Handle, int HashSizeInBytes)> s_handles = new();
            private static readonly ConcurrentDictionary<(string HashAlgorithmId, BCryptOpenAlgorithmProviderFlags Flags), bool> s_supported = new();

            /// <summary>
            /// Returns a SafeBCryptAlgorithmHandle of the desired algorithm and flags. This is a shared handle so do not dispose it!
            /// </summary>
            public static unsafe SafeBCryptAlgorithmHandle GetCachedBCryptAlgorithmHandle(string hashAlgorithmId, BCryptOpenAlgorithmProviderFlags flags, out int hashSizeInBytes)
            {
                var key = (hashAlgorithmId, flags);

                while (true)
                {
                    if (s_handles.TryGetValue(key, out (SafeBCryptAlgorithmHandle Handle, int HashSizeInBytes) result))
                    {
                        hashSizeInBytes = result.HashSizeInBytes;
                        return result.Handle;
                    }

                    SafeBCryptAlgorithmHandle handle = BCryptOpenAlgorithmProvider(
                        key.hashAlgorithmId,
                        null,
                        key.flags);

                    int hashSize = BCryptGetDWordProperty(handle, BCryptPropertyStrings.BCRYPT_HASH_LENGTH);
                    Debug.Assert(hashSize > 0);

                    if (!s_handles.TryAdd(key, (handle, hashSize)))
                    {
                        handle.Dispose();
                    }
                }
            }

            public static unsafe bool IsBCryptAlgorithmSupported(string hashAlgorithmId, BCryptOpenAlgorithmProviderFlags flags)
            {
                var key = (hashAlgorithmId, flags);

                if (s_supported.TryGetValue(key, out bool supported))
                {
                    return supported;
                }

                NTSTATUS status = BCryptOpenAlgorithmProvider(
                    out SafeBCryptAlgorithmHandle handle,
                    key.hashAlgorithmId,
                    null,
                    key.flags);

                bool isSupported = status == NTSTATUS.STATUS_SUCCESS;

                if (s_supported.TryAdd(key, isSupported) && isSupported)
                {
                    // It's a valid algorithm. Let's prime the handle cache while we are here. Presumably it's
                    // going to get used if we're asking if it's supported.
                    int hashSize = BCryptGetDWordProperty(handle, BCryptPropertyStrings.BCRYPT_HASH_LENGTH);
                    Debug.Assert(hashSize > 0);

                    if (s_handles.TryAdd(key, (handle, hashSize)))
                    {
                        // If we added the handle to the cache, don't dispose of it and return our answer.
                        return isSupported;
                    }
                }

                // Either the algorithm isn't supported or we don't need it for priming the cache, so Dispose.
                handle.Dispose();
                return isSupported;
            }
        }
    }
}
