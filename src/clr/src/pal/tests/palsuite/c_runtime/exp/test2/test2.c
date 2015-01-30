//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests that exp handles underflows, overflows, and NaNs
**          corectly.
**
**
**===================================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char **argv)
{
    double zero = 0.0;
    double PosInf = 1.0 / zero;
    double NaN = 0.0 / zero;
    // Force 'value' to be converted to a double to avoid
    // using the internal precision of the FPU for comparisons.
    volatile double value;

    if (PAL_Initialize(argc, argv) != 0)
    {
	    return FAIL;
    }

    /* Test overflows give PosInf */
    value = exp(800.0);
    if (value != PosInf)
    {
        Fail( "exp(%g) returned %g when it should have returned %g\n",
                    800.0, value, PosInf);
    }

    value = exp(PosInf);
    if (value != PosInf)
    {
        Fail( "exp(%g) returned %g when it should have returned %g\n",
            PosInf, value, PosInf);
    }

    /* Test underflows give 0 */
    value = exp(-800);
    if (value != 0)
    {
        Fail( "exp(%g) returned %g when it should have returned %g\n",
                    -800.0, value, 0.0);
    }    

    /* Test that a NaN as input gives a NaN */
    value = exp(NaN);
    if (_isnan(value) == 0)
    {
        Fail( "exp( NaN ) returned %g when it should have returned NaN\n", 
            value);
    }

    PAL_Terminate();
    return PASS;
}
