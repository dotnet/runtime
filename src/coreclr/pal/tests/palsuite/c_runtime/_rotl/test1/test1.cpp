// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c (_rotl)
**
** Purpose: Tests the PAL implementation of the _rotl function. 
**          The _rotl function rotates the unsigned value. _rotl
**          rotates the value left and "wraps" bits rotated off 
**          one end of value to the other end.  
**          This test compares the result to a previously determined
**          value.
**          
**
**
**===================================================================*/
#include <palsuite.h>

PALTEST(c_runtime__rotl_test1_paltest_rotl_test1, "c_runtime/_rotl/test1/paltest_rotl_test1")
{
    unsigned results = 0;
    int i,j;
   
    unsigned hTestNums[5][8] = {
        {0x00ff, 0x01fe, 0x03fc, 0x07f8, 0x0ff0, 0x1fe0, 0x3fc0, 0x7f80},
        {0x0055, 0x00aa, 0x0154, 0x02a8, 0x0550, 0x0aa0, 0x1540, 0x2a80},
        {0x0099, 0x0132, 0x0264, 0x04c8, 0x0990, 0x1320, 0x2640, 0x4c80},
        {0x0036, 0x006c, 0x00d8, 0x01b0, 0x0360, 0x06c0, 0x0d80, 0x1b00},
        {0x008f, 0x011e, 0x023c, 0x0478, 0x08f0, 0x11e0, 0x23c0, 0x4780}};
  
    if ((PAL_Initialize(argc, argv)) != 0)
    {
        return (FAIL);
    }

    /*Loop through expected test results*/
    for (j = 0; j <= 4; j++)
    {
        for(i = 1; i <= 7; i++)
        {
            results = _rotl(hTestNums[j][0], i);
            if (results != hTestNums[j][i])
            {
                Fail("ERROR: \"0x%4.4x\" rotated bits to the left %d times"
                    " gave \"0x%4.4x\", expected \"0x%4.4x\"\n", 
                    hTestNums[j][0], i, results, hTestNums[j][i]) ;
            }
        }
    }

    PAL_Terminate();
    return (PASS);
}
