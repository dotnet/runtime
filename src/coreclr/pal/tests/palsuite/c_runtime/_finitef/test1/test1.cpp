// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Checks that _finitef correctly classifies all types
**          of floating point numbers (NaN, -Infinity, Infinity,
**          finite nonzero, unnormalized, 0, and -0)
**
**==========================================================================*/

#include <palsuite.h>

/*
The IEEE single precision floating point standard looks like this:

  S EEEEEEEE FFFFFFFFFFFFFFFFFFFFFFF
  0 1      8 9                    31

S is the sign bit.  The E bits are the exponent, and the 23 F bits are
the fraction.  These represent a value, V.

If E=255 and F is nonzero, then V=NaN ("Not a number")
If E=255 and F is zero and S is 1, then V=-Infinity
If E=255 and F is zero and S is 0, then V=Infinity
If 0<E<255 then V=(-1)^S * 2^(E-1028) * (1.F) where "1.F" is the binary
   number created by prefixing F with a leading 1 and a binary point.
If E=0 and F is nonzero, then V=(-1)^S * 2^(-127) * (0.F) These are
   "unnormalized" values.
If E=0 and F is zero and S is 1, then V=-0
If E=0 and F is zero and S is 0, then V=0

*/

#define TO_FLOAT(x)    (*((float*)((void*)&x)))

PALTEST(c_runtime__finitef_test1_paltest_finitef_test1, "c_runtime/_finitef/test1/paltest_finitef_test1")
{
    /*non-finite numbers*/
    UINT32 lsnan =              0xffffffffu;
    UINT32 lqnan =              0x7fffffffu;
    UINT32 lneginf =            0xff800000u;
    UINT32 lposinf =            0x7f800000u;

    float snan =               TO_FLOAT(lsnan);
    float qnan =               TO_FLOAT(lqnan);
    float neginf =             TO_FLOAT(lneginf);
    float posinf =             TO_FLOAT(lposinf);

    /*finite numbers*/
    UINT32 lnegunnormalized =   0x807fffffu;
    UINT32 lposunnormalized =   0x007fffffu;
    UINT32 lnegzero =           0x80000000u;

    float negunnormalized =    TO_FLOAT(lnegunnormalized);
    float posunnormalized =    TO_FLOAT(lposunnormalized);
    float negzero =            TO_FLOAT(lnegzero);

    /*
     * Initialize the PAL and return FAIL if this fails
     */
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    /*non-finite numbers*/
    if (_finitef(snan) || _finitef(qnan))
    {
        Fail("_finitef() found NAN to be finite.\n");
    }

    if (_finitef(neginf))
    {
        Fail("_finitef() found negative infinity to be finite.\n");
    }

    if (_finitef(posinf))
    {
        Fail("_finitef() found infinity to be finite.\n");
    }

    /*finite numbers*/
    if (!_finitef(negunnormalized))
    {
        Fail("_finitef() found a negative unnormalized value to be infinite.\n");
    }

    if (!_finitef(posunnormalized))
    {
        Fail("_finitef() found an unnormalized value to be infinite.\n");
    }

    if (!_finitef(negzero))
    {
        Fail("_finitef() found negative zero to be infinite.\n");
    }

    if (!_finitef(+0.0f))
    {
        Fail("_finitef() found zero to be infinite.\n");
    }

    if (!_finitef(-123.456f))
    {
        Fail("_finitef() found %f to be infinite.\n", -123.456f);
    }

    if (!_finitef(+123.456f))
    {
        Fail("_finitef() found %f to be infinite.\n", +123.456f);
    }

    PAL_Terminate();
    return PASS;
}
