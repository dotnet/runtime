// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: 
** Test _isnan with a number of trivial values, to ensure they indicated that
** they are numbers.  Then try with Positive/Negative Infinite, which should
** also be numbers.  Finally set the least and most significant bits of 
** the fraction to positive and negative, at which point it should return
** the true value. 
**
**==========================================================================*/

#include <palsuite.h>

#define TO_DOUBLE(x)    (*((double*)((void*)&x)))
#define TO_I64(x)       (*((INT64*)((void*)&x)))

/*
 * NaN: any double with maximum exponent (0x7ff) and non-zero fraction
 */
PALTEST(c_runtime__isnan_test1_paltest_isnan_test1, "c_runtime/_isnan/test1/paltest_isnan_test1")
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
    if (_isnan(0.0))
    {
        Fail("_isnan() incorrectly identified %f as NaN!\n", 0.0);
    }

    if (_isnan(1.23456))
    {
        Fail("_isnan() incorrectly identified %f as NaN!\n", 1.234567);
    }

    if (_isnan(42.0))
    {
        Fail("_isnan() incorrectly identified %f as NaN!\n", 42.0);
    }

    UINT64 lneginf =            UI64(0xfff0000000000000);
    UINT64 lposinf =            UI64(0x7ff0000000000000);
    
    double neginf =             TO_DOUBLE(lneginf);
    double posinf =             TO_DOUBLE(lposinf);

    /*
     * Try positive and negative infinity
     */
    if (_isnan(neginf))
    {
        Fail("_isnan() incorrectly identified negative infinity as NaN!\n");
    }

    if (_isnan(posinf))
    {
        Fail("_isnan() incorrectly identified infinity as NaN!\n");
    }

    /*
     * Try setting the least significant bit of the fraction,
     * positive and negative
     */
    UINT64 lsnan =              UI64(0xfff0000000000001);
    double snan =               TO_DOUBLE(lsnan);
    
    if (!_isnan(snan))
    {
        Fail("_isnan() failed to identify %I64x as NaN!\n", lsnan);
    }

    UINT64 lqnan =              UI64(0x7ff0000000000001);
    double qnan =               TO_DOUBLE(lqnan);
    
    if (!_isnan(qnan))
    {
        Fail("_isnan() failed to identify %I64x as NaN!\n", lqnan);
    }

    /*
     * Try setting the most significant bit of the fraction,
     * positive and negative
     */
    lsnan =                     UI64(0xfff8000000000000);
    snan =                      TO_DOUBLE(lsnan);

    if (!_isnan(snan))
    {
        Fail ("_isnan() failed to identify %I64x as NaN!\n", lsnan);
    }

    lqnan =                     UI64(0x7ff8000000000000);
    qnan =                      TO_DOUBLE(lqnan);

    if (!_isnan(qnan))
    {
        Fail ("_isnan() failed to identify %I64x as NaN!\n", lqnan);
    }

    PAL_Terminate();
    return PASS;
}
