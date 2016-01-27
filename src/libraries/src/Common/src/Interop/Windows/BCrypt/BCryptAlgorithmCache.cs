// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Cryptography;

using Microsoft.Win32.SafeHandles;

internal partial class Interop
{
    internal partial class BCrypt
    {
        internal static class BCryptAlgorithmCache
        {
            /// <summary>
            ///     Return a SafeBCryptAlgorithmHandle of the desired algorithm and flags. This is a shared handle so do not dispose it!
            /// </summary>
            public static SafeBCryptAlgorithmHandle GetCachedBCryptAlgorithmHandle(string hashAlgorithmId, BCryptOpenAlgorithmProviderFlags flags)
            {
                // There aren't that many hash algorithms around so rather than use a LowLevelDictionary and guard it with a lock,
                // we'll use a simple list. To avoid locking, we'll recreate the entire list each time an entry is added and replace it atomically.
                //
                // This does mean that on occasion, racing threads may create two handles of the same type, but this is ok.
    
                // Latch the _cache value into a local so we aren't disrupted by concurrent changes to it.
                Entry[] cache = _cache;
                foreach (Entry entry in cache)
                {
                    if (entry.HashAlgorithmId == hashAlgorithmId && entry.Flags == flags)
                        return entry.Handle;
                }
    
                SafeBCryptAlgorithmHandle safeBCryptAlgorithmHandle;
                NTSTATUS ntStatus = Interop.BCrypt.BCryptOpenAlgorithmProvider(out safeBCryptAlgorithmHandle, hashAlgorithmId, null, flags);
                if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                    throw Interop.BCrypt.CreateCryptographicException(ntStatus);
    
                Entry[] newCache = new Entry[cache.Length + 1];
                Entry newEntry = new Entry(hashAlgorithmId, flags, safeBCryptAlgorithmHandle);
                Array.Copy(cache, 0, newCache, 0, cache.Length);
                newCache[newCache.Length - 1] = newEntry;
    
                // Atomically overwrite the cache with our new cache. It's possible some other thread raced to add a new entry with us - if so, one of the new entries
                // will be lost and the next guy that requests it will have to allocate it again. That's considered acceptable collateral damage.
                _cache = newCache;
                return newEntry.Handle;
            }
    
            private static volatile Entry[] _cache = Array.Empty<Entry>();
    
            private struct Entry
            {
                public Entry(string hashAlgorithmId, BCryptOpenAlgorithmProviderFlags flags, SafeBCryptAlgorithmHandle handle)
                    : this()
                {
                    HashAlgorithmId = hashAlgorithmId;
                    Flags = flags;
                    Handle = handle;
                }
    
                public string HashAlgorithmId { get; private set; }
                public BCryptOpenAlgorithmProviderFlags Flags { get; private set; }
                public SafeBCryptAlgorithmHandle Handle { get; private set; }
            }
        }
    }
}
