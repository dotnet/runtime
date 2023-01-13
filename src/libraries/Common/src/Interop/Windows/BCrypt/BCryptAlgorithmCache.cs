// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections.Concurrent;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class BCrypt
    {
        internal static class BCryptAlgorithmCache
        {
            private static readonly ConcurrentDictionary<(string HashAlgorithmId, BCryptOpenAlgorithmProviderFlags Flags), (SafeBCryptAlgorithmHandle Handle, int HashSizeInBytes)> s_handles = new();

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
        }
    }
}
