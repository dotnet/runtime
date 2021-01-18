// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>
    /// RandomNumberGenerator implementation is the 64-bit random number generator based on the Xorshift algorithm (known as shift-register generators).
    /// </summary>
    internal class RandomNumberGenerator
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
            }
            while ((_s0 | _s1 | _s2 | _s3) == 0);
        }

        private ulong Rol64(ulong x, int k) => (x << k) | (x >> (64 - k));

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
