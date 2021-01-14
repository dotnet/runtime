// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>
    /// RandomNumberGenerator implementation is borrowed from the Random class which implement the Additive Number Generator algorithm for generating random numbers.
    /// The difference here is RandomNumberGenerator based on long numbers instead of int numbers
    /// to allow generating long numbers and increase the period (which is when the generated number can repeat again)
    /// </summary>
    internal class RandomNumberGenerator
    {
        private readonly long[] _seedArray = new long[56];
        private int _inext;
        private int _inextp;

        [ThreadStatic] private static RandomNumberGenerator? t_random;

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

        public RandomNumberGenerator() : this(((long)Guid.NewGuid().GetHashCode() << 32) | (long)Guid.NewGuid().GetHashCode()) { }

        public RandomNumberGenerator(long Seed)
        {
            long subtraction = (Seed == long.MinValue) ? long.MaxValue : Math.Abs(Seed);
            long mj = 1618033988749894848L - subtraction; // magic number based on Phi (golden ratio)
            _seedArray[55] = mj;
            long mk = 1;

            int ii = 0;
            for (int i = 1; i < 55; i++)
            {
                // The range [1..55] is special (Knuth) and so we're wasting the 0'th position.
                if ((ii += 21) >= 55)
                {
                    ii -= 55;
                }

                _seedArray[ii] = mk;
                mk = mj - mk;
                if (mk < 0)
                {
                    mk += int.MaxValue;
                }

                mj = _seedArray[ii];
            }

            for (int k = 1; k < 5; k++)
            {
                for (int i = 1; i < 56; i++)
                {
                    int n = i + 30;
                    if (n >= 55)
                    {
                        n -= 55;
                    }

                    _seedArray[i] -= _seedArray[1 + n];
                    if (_seedArray[i] < 0)
                    {
                        _seedArray[i] += int.MaxValue;
                    }
                }
            }

            _inextp = 21;
        }

        public long Next() => InternalSample();

        private long InternalSample()
        {
            int locINext = _inext;
            if (++locINext >= 56)
            {
                locINext = 1;
            }

            int locINextp = _inextp;
            if (++locINextp >= 56)
            {
                locINextp = 1;
            }

            long retVal = _seedArray[locINext] - _seedArray[locINextp];

            if (retVal == long.MaxValue)
            {
                retVal--;
            }

            if (retVal < 0)
            {
                retVal += long.MaxValue;
            }

            _seedArray[locINext] = retVal;
            _inext = locINext;
            _inextp = locINextp;

            return retVal;
        }
    }
}
