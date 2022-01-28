// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>
    /// RandomNumberGenerator implementation is the 64-bit random number generator based on the Xoshiro256StarStar algorithm (known as shift-register generators).
    /// </summary>
    internal sealed class RandomNumberGenerator
    {
        [ThreadStatic] private static RandomNumberGenerator? t_random;

        private ulong _s0, _s1, _s2, _s3;

        public static RandomNumberGenerator Current
        {
            get
            {
                if (t_random == null)
                {
                    t_random = new RandomNumberGenerator();
                }
                return t_random;
            }
        }

#if ALLOW_PARTIALLY_TRUSTED_CALLERS
        [System.Security.SecuritySafeCriticalAttribute]
#endif
        public unsafe RandomNumberGenerator()
        {
            do
            {
                Guid g1 = Guid.NewGuid();
                Guid g2 = Guid.NewGuid();
                ulong* g1p = (ulong*)&g1;
                ulong* g2p = (ulong*)&g2;
                _s0 = *g1p;
                _s1 = *(g1p + 1);
                _s2 = *g2p;
                _s3 = *(g2p + 1);

                // Guid uses the 4 most significant bits of the first long as the version which would be fixed and not randomized.
                // and uses 2 other bits in the second long for variants which would be fixed and not randomized too.
                // let's overwrite the fixed bits in each long part by the other long.
                _s0 = (_s0 & 0x0FFFFFFFFFFFFFFF) | (_s1 & 0xF000000000000000);
                _s2 = (_s2 & 0x0FFFFFFFFFFFFFFF) | (_s3 & 0xF000000000000000);
                _s1 = (_s1 & 0xFFFFFFFFFFFFFF3F) | (_s0 & 0x00000000000000C0);
                _s3 = (_s3 & 0xFFFFFFFFFFFFFF3F) | (_s2 & 0x00000000000000C0);
            }
            while ((_s0 | _s1 | _s2 | _s3) == 0);
        }

        private static ulong Rol64(ulong x, int k) => (x << k) | (x >> (64 - k));

        public long Next()
        {
            ulong result = Rol64(_s1 * 5, 7) * 9;
            ulong t = _s1 << 17;

            _s2 ^= _s0;
            _s3 ^= _s1;
            _s1 ^= _s2;
            _s0 ^= _s3;

            _s2 ^= t;
            _s3 = Rol64(_s3, 45);

            return (long)result;
        }
    }
}
