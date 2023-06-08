// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class RandomTwister64
{
    const int NN = 312;
    const int MM = 156;
    const ulong MATRIX_A = 0xB5026F5AA96619E9;
    const ulong UM = 0xFFFFFFFF80000000; /* Most significant 33 bits */
    const ulong LM = 0x7FFFFFFF; /* Least significant 31 bits */

    private ulong[] mt = new ulong[NN];
    private ulong mti = NN + 1;

    public RandomTwister64(ulong seed)
    {
        mt[0] = seed;
        for (mti = 1; mti < NN; mti++)
            mt[mti] = (6364136223846793005 * (mt[mti - 1] ^ (mt[mti - 1] >> 62)) + mti);
    }

    private ulong[] mag01 = { 0, MATRIX_A };

    private ulong genrand64_int64()
    {
        int i;
        ulong x;

        if (mti >= NN)
        { /* generate NN words at one time */
            for (i = 0; i < NN - MM; i++)
            {
                x = (mt[i] & UM) | (mt[i + 1] & LM);
                mt[i] = mt[i + MM] ^ (x >> 1) ^ mag01[(int)(x & 1)];
            }
            for (; i < NN - 1; i++)
            {
                x = (mt[i] & UM) | (mt[i + 1] & LM);
                mt[i] = mt[i + (MM - NN)] ^ (x >> 1) ^ mag01[(int)(x & 1)];
            }
            x = (mt[NN - 1] & UM) | (mt[0] & LM);
            mt[NN - 1] = mt[MM - 1] ^ (x >> 1) ^ mag01[(int)(x & 1)];

            mti = 0;
        }

        x = mt[mti++];

        x ^= (x >> 29) & 0x5555555555555555;
        x ^= (x << 17) & 0x71D67FFFEDA60000;
        x ^= (x << 37) & 0xFFF7EEE000000000;
        x ^= (x >> 43);

        return x;
    }

    /// <summary>
    /// Returns a Random number on [0..1]
    /// </summary>
    /// <returns></returns>
    public double RandomDoubleClosed()
    {
        lock (this)
        {
            return (genrand64_int64() >> 11) * (1.0 / 9007199254740991.0);
        }
    }
}


public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        int i;
        int countZero = 0;
        // Create a Mersenne Twister with seed 123456
        RandomTwister64 rand = new RandomTwister64(123456);
        //Print 100 doubles
        for (i = 1; i <= 100; i++)
        {
            double d = rand.RandomDoubleClosed();

            if (d == 0.0)
                countZero++;

            Console.Write(String.Format("{0} ", d));
            if (i % 5 == 0)
                Console.WriteLine();
        }

        // NOTE: When I reproed this, I got 100 zeros (0)
        // in the unfixed case, and some double between
        // 0 and 1 in the fixed case. Actually never saw 
        // the 0 or the 1, always a double in between. 
        if (countZero < 2)
        {
            Console.WriteLine("!!!!!!! PASSED !!!!!!!");
            return 100;
        }
        else
        {
            Console.WriteLine("!!!!!!! FAILED !!!!!!!");
            return 666;
        }
    }
}

