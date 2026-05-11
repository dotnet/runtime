// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System
{
    public partial struct Guid
    {
        // This will create a new random guid based on the https://www.ietf.org/rfc/rfc4122.txt
        public static unsafe Guid NewGuid()
        {
            Guid g;
#if !TARGET_WASI
            // Guid.NewGuid is often used as a cheap source of random data that are sometimes used for security purposes.
            // Windows implementation uses secure RNG to implement it. We use secure RNG for Unix too to avoid subtle security
            // vulnerabilities in applications that depend on it. See https://github.com/dotnet/runtime/issues/42752 for details.
            Interop.GetCryptographicallySecureRandomBytes((byte*)&g, sizeof(Guid));
#else
            // TODOWASI: crypto secure random bytes
            Interop.GetRandomBytes((byte*)&g, sizeof(Guid));
#endif

            // Modify bits indicating the type of the GUID

            unchecked
            {
                // time_hi_and_version
                Unsafe.AsRef(in g._c) = (short)((g._c & ~VersionMask) | Version4Value);
                // clock_seq_hi_and_reserved
                Unsafe.AsRef(in g._d) = (byte)((g._d & ~Variant10xxMask) | Variant10xxValue);
            }

            return g;
        }

        // Returns a Guid whose bytes 6..15 contain cryptographically-secure random data, as
        // required by CreateVersion7. Bytes 0..5 are intentionally left uninitialized; the
        // caller overwrites them with the unix_ts_ms timestamp.
        //
        // Asking for only 10 bytes (instead of all 16) hits the Apple CoreCrypto fast path
        // for <=12 byte requests and reduces the amount of entropy pulled from the kernel
        // on other Unix platforms. The version and variant fields are applied by the caller,
        // so this helper does not set them (unlike NewGuid which produces a v4 Guid).
        private static unsafe Guid CreateVersion7Random()
        {
            Unsafe.SkipInit(out Guid g);
#if !TARGET_WASI
            Interop.GetCryptographicallySecureRandomBytes((byte*)&g + 6, 10);
#else
            // TODOWASI: crypto secure random bytes
            Interop.GetRandomBytes((byte*)&g + 6, 10);
#endif
            return g;
        }
    }
}
