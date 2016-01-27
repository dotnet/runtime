// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
**
**==========================================================================*/

#include <palsuite.h>

#define TO_DOUBLE(a) (*(double*)&a)
#define TO_I64(a) (*(INT64*) &a)

/*
 * NaN: any double with maximum exponent (0x7ff) and non-zero fraction
 */
int __cdecl main(int argc, char *argv[])
{
    UINT64 PosInf=0;
    UINT64 NegInf=0;
    UINT64 val=0;

    /*
     * Initialize the PAL and return FAIL if this fails
     */
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }
    /*
     * Try some trivial values
     */
    if (_isnan(0))
    {
        Fail ("_isnan() incorrectly identified %f as NaN!\n", 0);
    }
    if (_isnan(1.2423456))
    {
        Fail ("_isnan() incorrectly identified %f as NaN!\n", 0);
    }
    if (_isnan(42))
    {
        Fail ("_isnan() incorrectly identified %f as NaN!\n", 0);
    }


    PosInf = 0x7ff00000;
    PosInf <<=32;

    NegInf = 0xfff00000;
    NegInf <<=32;

    /*
     * Try positive and negative infinity
     */
    if (_isnan(TO_DOUBLE(PosInf)))
    {
        Fail ("_isnan() incorrectly identified %I64x as NaN!\n", PosInf);
    }

    if (_isnan(TO_DOUBLE(NegInf)))
    {
        Fail ("_isnan() incorrectly identified %I64x as NaN!\n", NegInf);
    }

    /*
     * Try setting the least significant bit of the fraction,
     * positive and negative
     */
    val = PosInf + 1;
    if (!_isnan(TO_DOUBLE(val)))
    {
        Fail ("_isnan() failed to identify %I64x as NaN!\n", val);
    }

    val = NegInf + 1;
    if (!_isnan(TO_DOUBLE(val)))
    {
        Fail ("_isnan() failed to identify %I64x as NaN!\n", val);
    }


    /*
     * Try setting the most significant bit of the fraction,
     * positive and negative
     */
    val = 0x7ff80000;
    val <<=32;
    if (!_isnan(TO_DOUBLE(val)))
    {
        Fail ("_isnan() failed to identify %I64x as NaN!\n", val);
    }

    val = 0xfff80000;
    val <<=32;
    if (!_isnan(TO_DOUBLE(val)))
    {
        Fail ("_isnan() failed to identify %I64x as NaN!\n", val);
    }

    PAL_Terminate();

    return PASS;
}

