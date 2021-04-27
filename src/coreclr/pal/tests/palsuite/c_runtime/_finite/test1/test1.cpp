// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Checks that _finite correctly classifies all types
**          of floating point numbers (NaN, -Infinity, Infinity,
**          finite nonzero, unnormalized, 0, and -0)
**
**==========================================================================*/

#include <palsuite.h>

/*
The IEEE double precision floating point standard looks like this:

  S EEEEEEEEEEE FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF
  0 1        11 12                                                63

S is the sign bit.  The E bits are the exponent, and the 52 F bits are
the fraction.  These represent a value, V.

If E=2047 and F is nonzero, then V=NaN ("Not a number")
If E=2047 and F is zero and S is 1, then V=-Infinity
If E=2047 and F is zero and S is 0, then V=Infinity
If 0<E<2047 then V=(-1)^S * 2^(E-1023) * (1.F) where "1.F" is the binary
   number created by prefixing F with a leading 1 and a binary point.
If E=0 and F is nonzero, then V=(-1)^S * 2^(-1022) * (0.F) These are
   "unnormalized" values.
If E=0 and F is zero and S is 1, then V=-0
If E=0 and F is zero and S is 0, then V=0

*/

#define TO_DOUBLE(x)    (*((double*)((void*)&x)))

PALTEST(c_runtime__finite_test1_paltest_finite_test1, "c_runtime/_finite/test1/paltest_finite_test1")
{
    /*non-finite numbers*/
    UINT64 lsnan =              UI64(0xffffffffffffffff);
    UINT64 lqnan =              UI64(0x7fffffffffffffff);
    UINT64 lneginf =            UI64(0xfff0000000000000);
    UINT64 lposinf =            UI64(0x7ff0000000000000);

    double snan =               TO_DOUBLE(lsnan);
    double qnan =               TO_DOUBLE(lqnan);
    double neginf =             TO_DOUBLE(lneginf);
    double posinf =             TO_DOUBLE(lposinf);

    /*finite numbers*/
    UINT64 lnegunnormalized =   UI64(0x800fffffffffffff);
    UINT64 lposunnormalized =   UI64(0x000fffffffffffff);
    UINT64 lnegzero =           UI64(0x8000000000000000);

    double negunnormalized =    TO_DOUBLE(lnegunnormalized);
    double posunnormalized =    TO_DOUBLE(lposunnormalized);
    double negzero =            TO_DOUBLE(lnegzero);

    /*
     * Initialize the PAL and return FAIL if this fails
     */
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    /*non-finite numbers*/
    if (_finite(snan) || _finite(qnan))
    {
        Fail("_finite() found NAN to be finite.\n");
    }

    if (_finite(neginf))
    {
        Fail("_finite() found negative infinity to be finite.\n");
    }

    if (_finite(posinf))
    {
        Fail("_finite() found infinity to be finite.\n");
    }

    /*finite numbers*/
    if (!_finite(negunnormalized))
    {
        Fail("_finite() found a negative unnormalized value to be infinite.\n");
    }

    if (!_finite(posunnormalized))
    {
        Fail("_finite() found an unnormalized value to be infinite.\n");
    }

    if (!_finite(negzero))
    {
        Fail("_finite() found negative zero to be infinite.\n");
    }

    if (!_finite(+0.0))
    {
        Fail("_finite() found zero to be infinite.\n");
    }

    if (!_finite(-123.456))
    {
        Fail("_finite() found %f to be infinite.\n", -123.456);
    }

    if (!_finite(+123.456))
    {
        Fail("_finite() found %f to be infinite.\n", +123.456);
    }

    PAL_Terminate();
    return PASS;
}
