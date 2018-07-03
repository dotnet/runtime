// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    public class Random
    {
        //
        // Private Constants 
        //
        private const int MBIG = int.MaxValue;
        private const int MSEED = 161803398;
        private const int MZ = 0;

        //
        // Member Variables
        //
        private int _inext;
        private int _inextp;
        private int[] _seedArray = new int[56];

        //
        // Public Constants
        //

        //
        // Native Declarations
        //

        //
        // Constructors
        //

        /*=========================================================================================
        **Action: Initializes a new instance of the Random class, using a default seed value
        ===========================================================================================*/
        public Random()
          : this(GenerateSeed())
        {
        }

        /*=========================================================================================
        **Action: Initializes a new instance of the Random class, using a specified seed value
        ===========================================================================================*/
        public Random(int Seed)
        {
            int ii = 0;
            int mj, mk;

            //Initialize our Seed array.
            int subtraction = (Seed == int.MinValue) ? int.MaxValue : Math.Abs(Seed);
            mj = MSEED - subtraction;
            _seedArray[55] = mj;
            mk = 1;
            for (int i = 1; i < 55; i++)
            {  //Apparently the range [1..55] is special (Knuth) and so we're wasting the 0'th position.
                if ((ii += 21) >= 55) ii -= 55;
                _seedArray[ii] = mk;
                mk = mj - mk;
                if (mk < 0) mk += MBIG;
                mj = _seedArray[ii];
            }
            for (int k = 1; k < 5; k++)
            {
                for (int i = 1; i < 56; i++)
                {
                    int n = i + 30;
                    if (n >= 55) n -= 55;
                    _seedArray[i] -= _seedArray[1 + n];
                    if (_seedArray[i] < 0) _seedArray[i] += MBIG;
                }
            }
            _inext = 0;
            _inextp = 21;
            Seed = 1;
        }

        //
        // Package Private Methods
        //

        /*====================================Sample====================================
        **Action: Return a new random number [0..1) and reSeed the Seed array.
        **Returns: A double [0..1)
        **Arguments: None
        **Exceptions: None
        ==============================================================================*/
        protected virtual double Sample()
        {
            //Including this division at the end gives us significantly improved
            //random number distribution.
            return (InternalSample() * (1.0 / MBIG));
        }

        private int InternalSample()
        {
            int retVal;
            int locINext = _inext;
            int locINextp = _inextp;

            if (++locINext >= 56) locINext = 1;
            if (++locINextp >= 56) locINextp = 1;

            retVal = _seedArray[locINext] - _seedArray[locINextp];

            if (retVal == MBIG) retVal--;
            if (retVal < 0) retVal += MBIG;

            _seedArray[locINext] = retVal;

            _inext = locINext;
            _inextp = locINextp;

            return retVal;
        }

        [ThreadStatic]
        private static Random t_threadRandom;
        private static readonly Random s_globalRandom = new Random(GenerateGlobalSeed());

        /*=====================================GenerateSeed=====================================
        **Returns: An integer that can be used as seed values for consecutively
                   creating lots of instances on the same thread within a short period of time.
        ========================================================================================*/
        private static int GenerateSeed()
        {
            Random rnd = t_threadRandom;
            if (rnd == null)
            {
                int seed;
                lock (s_globalRandom)
                {
                    seed = s_globalRandom.Next();
                }
                rnd = new Random(seed);
                t_threadRandom = rnd;
            }
            return rnd.Next();
        }

        /*==================================GenerateGlobalSeed====================================
        **Action:  Creates a number to use as global seed.
        **Returns: An integer that is safe to use as seed values for thread-local seed generators.
        ==========================================================================================*/
        private static unsafe int GenerateGlobalSeed()
        {
            int result;
            Interop.GetRandomBytes((byte*)&result, sizeof(int));
            return result;
        }

        //
        // Public Instance Methods
        // 


        /*=====================================Next=====================================
        **Returns: An int [0..int.MaxValue)
        **Arguments: None
        **Exceptions: None.
        ==============================================================================*/
        public virtual int Next()
        {
            return InternalSample();
        }

        private double GetSampleForLargeRange()
        {
            // The distribution of double value returned by Sample 
            // is not distributed well enough for a large range.
            // If we use Sample for a range [int.MinValue..int.MaxValue)
            // We will end up getting even numbers only.

            int result = InternalSample();
            // Note we can't use addition here. The distribution will be bad if we do that.
            bool negative = (InternalSample() % 2 == 0) ? true : false;  // decide the sign based on second sample
            if (negative)
            {
                result = -result;
            }
            double d = result;
            d += (int.MaxValue - 1); // get a number in range [0 .. 2 * Int32MaxValue - 1)
            d /= 2 * (uint)int.MaxValue - 1;
            return d;
        }


        /*=====================================Next=====================================
        **Returns: An int [minvalue..maxvalue)
        **Arguments: minValue -- the least legal value for the Random number.
        **           maxValue -- One greater than the greatest legal return value.
        **Exceptions: None.
        ==============================================================================*/
        public virtual int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(minValue), SR.Format(SR.Argument_MinMaxValue, nameof(minValue), nameof(maxValue)));
            }

            long range = (long)maxValue - minValue;
            if (range <= int.MaxValue)
            {
                return ((int)(Sample() * range) + minValue);
            }
            else
            {
                return (int)((long)(GetSampleForLargeRange() * range) + minValue);
            }
        }


        /*=====================================Next=====================================
        **Returns: An int [0..maxValue)
        **Arguments: maxValue -- One more than the greatest legal return value.
        **Exceptions: None.
        ==============================================================================*/
        public virtual int Next(int maxValue)
        {
            if (maxValue < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxValue), SR.Format(SR.ArgumentOutOfRange_MustBePositive, nameof(maxValue)));
            }
            return (int)(Sample() * maxValue);
        }


        /*=====================================Next=====================================
        **Returns: A double [0..1)
        **Arguments: None
        **Exceptions: None
        ==============================================================================*/
        public virtual double NextDouble()
        {
            return Sample();
        }


        /*==================================NextBytes===================================
        **Action:  Fills the byte array with random bytes [0..0x7f].  The entire array is filled.
        **Returns:Void
        **Arguments:  buffer -- the array to be filled.
        **Exceptions: None
        ==============================================================================*/
        public virtual void NextBytes(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
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
    }
}
