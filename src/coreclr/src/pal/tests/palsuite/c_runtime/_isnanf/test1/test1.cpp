// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: 
** Test _isnanf with a number of trivial values, to ensure they indicated that
** they are numbers.  Then try with Positive/Negative Infinite, which should
** also be numbers.  Finally set the least and most significant bits of 
** the fraction to positive and negative, at which point it should return
** the true value. 
**
**==========================================================================*/

#include <palsuite.h>

#define TO_FLOAT(x)    (*((float*)((void*)&x)))
#define TO_I32(x)      (*((INT32*)((void*)&x)))

/*
 * NaN: any float with maximum exponent (0x7f8) and non-zero fraction
 */
PALTEST(c_runtime__isnanf_test1_paltest_isnanf_test1, "c_runtime/_isnanf/test1/paltest_isnanf_test1")
{
    /*
     * Initialize the PAL and return FAIL if this fails
     */
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    /*
     * Try some trivial values
     */
    if (_isnanf(0.0f))
    {
        Fail("_isnanf() incorrectly identified %f as NaN!\n", 0.0f);
    }

    if (_isnanf(1.234567f))
    {
        Fail("_isnanf() incorrectly identified %f as NaN!\n", 1.234567f);
    }

    if (_isnanf(42.0f))
    {
        Fail("_isnanf() incorrectly identified %f as NaN!\n", 42.0f);
    }

    UINT32 lneginf =           0xff800000u;
    UINT32 lposinf =           0x7f800000u;
    
    float neginf =             TO_FLOAT(lneginf);
    float posinf =             TO_FLOAT(lposinf);

    /*
     * Try positive and negative infinity
     */
    if (_isnanf(neginf))
    {
        Fail("_isnanf() incorrectly identified negative infinity as NaN!\n");
    }

    if (_isnanf(posinf))
    {
        Fail("_isnanf() incorrectly identified infinity as NaN!\n");
    }

    /*
     * Try setting the least significant bit of the fraction,
     * positive and negative
     */
    UINT32 lsnan =             0xff800001u;
    float snan =               TO_FLOAT(lsnan);
    
    if (!_isnanf(snan))
    {
        Fail("_isnanf() failed to identify %I32x as NaN!\n", lsnan);
    }

    UINT32 lqnan =             0x7f800001u;
    float qnan =               TO_FLOAT(lqnan);
    
    if (!_isnanf(qnan))
    {
        Fail("_isnanf() failed to identify %I32x as NaN!\n", lqnan);
    }

    /*
     * Try setting the most significant bit of the fraction,
     * positive and negative
     */
    lsnan =                     0xffc00000u;
    snan =                      TO_FLOAT(lsnan);

    if (!_isnanf(snan))
    {
        Fail ("_isnanf() failed to identify %I32x as NaN!\n", lsnan);
    }

    lqnan =                     0x7fc00000u;
    qnan =                      TO_FLOAT(lqnan);

    if (!_isnanf(qnan))
    {
        Fail ("_isnanf() failed to identify %I32x as NaN!\n", lqnan);
    }

    PAL_Terminate();
    return PASS;
}
