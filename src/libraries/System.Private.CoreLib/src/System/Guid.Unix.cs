// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public partial struct Guid
    {
        // Batch size for cached GUID generation. Chosen to balance memory usage vs syscall overhead.
        // 64 GUIDs = 1024 bytes, which provides good amortization of the syscall cost while
        // keeping memory footprint reasonable for thread-local storage.
        private const int GuidCacheBatchSize = 64;

        [ThreadStatic]
        private static Guid[]? t_guidCache;

        [ThreadStatic]
        private static int t_guidCacheIndex;

        // This will create a new random guid based on the https://www.ietf.org/rfc/rfc4122.txt
        public static unsafe Guid NewGuid()
        {
#if !TARGET_WASI
            // Use thread-local caching to amortize the cost of secure random byte generation.
            // This significantly improves performance on Linux where the getrandom/urandom syscall
            // overhead is much higher than Windows' BCryptGenRandom.

            Guid[]? cache = t_guidCache;
            int index = t_guidCacheIndex;

            if (cache is null || (uint)index >= (uint)cache.Length)
            {
                return NewGuidWithCacheRefill();
            }

            t_guidCacheIndex = index + 1;
            return cache[index];
#else
            // TODOWASI: crypto secure random bytes - no caching for WASI
            Guid g;
            Interop.GetRandomBytes((byte*)&g, sizeof(Guid));

            unchecked
            {
                // time_hi_and_version
                Unsafe.AsRef(in g._c) = (short)((g._c & ~VersionMask) | Version4Value);
                // clock_seq_hi_and_reserved
                Unsafe.AsRef(in g._d) = (byte)((g._d & ~Variant10xxMask) | Variant10xxValue);
            }

            return g;
#endif
        }

#if !TARGET_WASI
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe Guid NewGuidWithCacheRefill()
        {
            Guid[]? cache = t_guidCache ??= new Guid[GuidCacheBatchSize];

            // Fill the entire cache with random bytes in a single syscall
            // Guid.NewGuid is often used as a cheap source of random data that are sometimes used for security purposes.
            // Windows implementation uses secure RNG to implement it. We use secure RNG for Unix too to avoid subtle security
            // vulnerabilities in applications that depend on it. See https://github.com/dotnet/runtime/issues/42752 for details.
            fixed (Guid* pCache = cache)
            {
                Interop.GetCryptographicallySecureRandomBytes((byte*)pCache, sizeof(Guid) * GuidCacheBatchSize);
            }

            // Set version and variant bits for all GUIDs in the cache
            for (int i = 0; i < GuidCacheBatchSize; i++)
            {
                ref Guid g = ref cache[i];
                unchecked
                {
                    // time_hi_and_version
                    Unsafe.AsRef(in g._c) = (short)((g._c & ~VersionMask) | Version4Value);
                    // clock_seq_hi_and_reserved
                    Unsafe.AsRef(in g._d) = (byte)((g._d & ~Variant10xxMask) | Variant10xxValue);
                }
            }

            t_guidCacheIndex = 1;
            return cache[0];
        }
#endif
    }
}
