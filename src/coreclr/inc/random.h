// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// random.h
//

//
// Defines a random number generator, initially from the System.Random code in the BCL.
//
// Main advantages over rand() are:
//
// 1) It generates better random numbers
// 2) It can have multiple instantiations with different seeds
// 3) It behaves the same regardless of whether we build with VC++ or GCC
//
// If you are working in the VM, we have a convenience method: code:GetRandomInt.  This usess a thread-local
// Random instance if a Thread object is available, and otherwise falls back to a global instance
// with a spin-lock.
//

#ifndef _CLRRANDOM_H_
#define _CLRRANDOM_H_

#include <math.h>

//
// Forbid the use of srand()/rand(), as these are globally shared facilities and our use of them would
// interfere with native user code in the same process. This override is not compatible with stl headers.
//
#if !defined(DO_NOT_DISABLE_RAND) && !defined(USE_STL)

#ifdef srand
#undef srand
#endif
#define srand Do_not_use_srand

#ifdef rand
#undef rand
#endif
#define rand Do_not_use_rand

#endif //!DO_NOT_DISABLE_RAND && !USE_STL


class CLRRandom
{
private:
    //
    // Private Constants
    //
    static const int MBIG =  INT_MAX;
    static const int MSEED = 161803398;
    static const int MZ = 0;


    //
    // Member Variables
    //
    int inext;
    int inextp;
    int SeedArray[56];

    bool initialized;

public:
    //
    // Constructors
    //

    CLRRandom()
    {
        LIMITED_METHOD_CONTRACT;
        initialized = false;
    }

    void Init()
    {
        LIMITED_METHOD_CONTRACT;
        LARGE_INTEGER time;
        if (!QueryPerformanceCounter(&time))
            time.QuadPart = GetTickCount();
        Init((int)time.u.LowPart ^ GetCurrentThreadId() ^ GetCurrentProcessId());
    }

    void Init(int Seed)
    {
        LIMITED_METHOD_CONTRACT;

        int ii;
        int mj, mk;

        //Initialize our Seed array.
        mj = MSEED - abs(Seed);
        SeedArray[55]=mj;
        mk=1;
        for (int i=1; i<55; i++) {  //Apparently the range [1..55] is special (Knuth) and so we're wasting the 0'th position.
            ii = (21*i)%55;
            SeedArray[ii]=mk;
            mk = mj - mk;
            if (mk<0) mk+=MBIG;
            mj=SeedArray[ii];
        }
        for (int k=1; k<5; k++) {
            for (int i=1; i<56; i++) {
                SeedArray[i] -= SeedArray[1+(i+30)%55];
                if (SeedArray[i]<0) SeedArray[i]+=MBIG;
            }
        }
        inext=0;
        inextp = 21;
        Seed = 1;

        initialized = true;
    }

    bool IsInitialized()
    {
        LIMITED_METHOD_CONTRACT;
        return initialized;
    }

private:
    //
    // Package Private Methods
    //

    /*====================================Sample====================================
    **Action: Return a new random number [0..1) and reSeed the Seed array.
    **Returns: A double [0..1)
    **Arguments: None
    **Exceptions: None
    ==============================================================================*/
    double Sample()
    {
        LIMITED_METHOD_CONTRACT;

        //Including this division at the end gives us significantly improved
        //random number distribution.
        return (InternalSample()*(1.0/MBIG));
    }

    int InternalSample()
    {
        LIMITED_METHOD_CONTRACT;

        int retVal;
        int locINext = inext;
        int locINextp = inextp;

        if (++locINext >=56) locINext=1;
        if (++locINextp>= 56) locINextp = 1;

        retVal = SeedArray[locINext]-SeedArray[locINextp];

        if (retVal == MBIG) retVal--;
        if (retVal<0) retVal+=MBIG;

        SeedArray[locINext]=retVal;

        inext = locINext;
        inextp = locINextp;

        return retVal;
    }

    double GetSampleForLargeRange()
    {
        LIMITED_METHOD_CONTRACT;

        // The distribution of double value returned by Sample
        // is not distributed well enough for a large range.
        // If we use Sample for a range [Int32.MinValue..Int32.MaxValue)
        // We will end up getting even numbers only.

        int result = InternalSample();
        // Note we can't use addition here. The distribution will be bad if we do that.
        bool negative = (InternalSample()%2 == 0) ? true : false;  // decide the sign based on second sample
        if( negative) {
            result = -result;
        }
        double d = result;
        d += (INT_MAX - 1); // get a number in range [0 .. 2 * Int32MaxValue - 1)
        d /= 2*(unsigned int)INT_MAX - 1  ;
        return d;
    }

public:
    //
    // Public Instance Methods
    //


    /*=====================================Next=====================================
    **Returns: An int [0..Int32.MaxValue)
    **Arguments: None
    **Exceptions: None.
    ==============================================================================*/
    int Next()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(initialized);
        return InternalSample();
    }


    /*=====================================Next=====================================
    **Returns: An int [minvalue..maxvalue)
    **Arguments: minValue -- the least legal value for the Random number.
    **           maxValue -- One greater than the greatest legal return value.
    **Exceptions: None.
    ==============================================================================*/
    int Next(int minValue, int maxValue)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(initialized);
        _ASSERTE(minValue < maxValue);

        LONGLONG range = (LONGLONG)maxValue-minValue;
        double result;

        if( range <= (LONGLONG)INT_MAX)
            result = (Sample() * range) + minValue;
        else
            result = (GetSampleForLargeRange() * range) + minValue;

        _ASSERTE(result >= minValue && result < maxValue);
        return (int)result;
    }


    /*=====================================Next=====================================
    **Returns: An int [0..maxValue)
    **Arguments: maxValue -- One more than the greatest legal return value.
    **Exceptions: None.
    ==============================================================================*/
    int Next(int maxValue)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(initialized);
        double result = Sample()*maxValue;
        _ASSERTE(result >= 0 && result < maxValue);
        return (int)result;
    }


    /*=====================================Next=====================================
    **Returns: A double [0..1)
    **Arguments: None
    **Exceptions: None
    ==============================================================================*/
    double NextDouble()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(initialized);
        double result = Sample();
        _ASSERTE(result >= 0 && result < 1);
        return result;
    }


    /*==================================NextBytes===================================
    **Action:  Fills the byte array with random bytes [0..0x7f].  The entire array is filled.
    **Returns:Void
    **Arguments:  buffer -- the array to be filled.
    **Exceptions: None
    ==============================================================================*/
    void NextBytes(_Out_writes_(length) BYTE buffer[], int length)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(initialized);
        for (int i=0; i<length; i++) {
            buffer[i]=(BYTE)(InternalSample()%(256));
        }
    }
};

#endif //_CLRRANDOM_H_
