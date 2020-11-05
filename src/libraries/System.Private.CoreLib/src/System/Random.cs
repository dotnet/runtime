// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    public class Random
    {
        private static readonly Random s_globalRandom = new Random(GenerateGlobalSeed());
        [ThreadStatic]
        private static Random? t_threadRandom;

        private readonly int[] _seedArray = new int[56];
        private int _inext;
        private int _inextp;

        public Random() : this(ThreadStaticRandom.Next())
        {
        }

        public Random(int Seed)
        {
            // Initialize seed array.

            int subtraction = (Seed == int.MinValue) ? int.MaxValue : Math.Abs(Seed);
            int mj = 161803398 - subtraction; // magic number based on Phi (golden ratio)
            _seedArray[55] = mj;
            int mk = 1;

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

        protected virtual double Sample() =>
            // Including the division at the end gives us significantly improved random number distribution.
            InternalSample() * (1.0 / int.MaxValue);

        public virtual int Next() => InternalSample();

        public virtual int Next(int maxValue)
        {
            if (maxValue < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxValue), SR.Format(SR.ArgumentOutOfRange_MustBePositive, nameof(maxValue)));
            }

            return (int)(Sample() * maxValue);
        }

        public virtual int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(minValue), SR.Format(SR.Argument_MinMaxValue, nameof(minValue), nameof(maxValue)));
            }

            long range = (long)maxValue - minValue;
            return range <= int.MaxValue ?
                (int)(Sample() * range) + minValue :
                (int)((long)(GetSampleForLargeRange() * range) + minValue);
        }

        public virtual double NextDouble() => Sample();

        public virtual void NextBytes(byte[] buffer)
        {
            if (buffer is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.buffer);
            }

            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (byte)InternalSample();
            }
        }

        public virtual void NextBytes(Span<byte> buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (byte)Next();
            }
        }

        private int InternalSample()
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

            int retVal = _seedArray[locINext] - _seedArray[locINextp];

            if (retVal == int.MaxValue)
            {
                retVal--;
            }
            if (retVal < 0)
            {
                retVal += int.MaxValue;
            }

            _seedArray[locINext] = retVal;
            _inext = locINext;
            _inextp = locINextp;

            return retVal;
        }

        private static Random ThreadStaticRandom
        {
            get
            {
                return t_threadRandom ??= CreateThreadStaticRandom();

                static Random CreateThreadStaticRandom()
                {
                    int seed;
                    lock (s_globalRandom)
                    {
                        seed = s_globalRandom.Next();
                    }

                    return new Random(seed);
                }
            }
        }

        private static unsafe int GenerateGlobalSeed()
        {
            int result;
            Interop.GetRandomBytes((byte*)&result, sizeof(int));
            return result;
        }

        private double GetSampleForLargeRange()
        {
            // The distribution of the double returned by Sample is not good enough for a large range.
            // If we use Sample for a range [int.MinValue..int.MaxValue), we will end up getting even numbers only.
            int result = InternalSample();

            // We can't use addition here: the distribution will be bad if we do that.
            if (InternalSample() % 2 == 0) // decide the sign based on second sample
            {
                result = -result;
            }

            double d = result;
            d += int.MaxValue - 1; // get a number in range [0..2*int.MaxValue-1)
            d /= 2u * int.MaxValue - 1;
            return d;
        }
    }
}
