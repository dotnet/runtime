// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c (_rotr)
**
** Purpose: Tests the PAL implementation of the _rotr function. 
**          The _rotr function rotates the unsigned value. _rotr
**          rotates the value right and "wraps" bits rotated off 
**          one end of value to the other end.  
**          This test compares the result to a previously 
**          determined value.
**
**
**===================================================================*/
#include <palsuite.h>

PALTEST(c_runtime__rotr_test1_paltest_rotr_test1, "c_runtime/_rotr/test1/paltest_rotr_test1")
{
    unsigned results = 0;
    int i,j;
   
    unsigned hTestNums[5][8] = {
        {0x00ff, 0x8000007f, 0xc000003f, 0xe000001f, 
            0xf000000f, 0xf8000007, 0xfc000003, 0xfe000001},
        {0x0055, 0x8000002a, 0x40000015, 0xa000000a, 
            0x50000005, 0xa8000002, 0x54000001, 0xaa000000},
        {0x0099, 0x8000004c, 0x40000026, 0x20000013,
            0x90000009, 0xc8000004, 0x64000002, 0x32000001},
        {0x0036, 0x001b, 0x8000000d, 0xc0000006, 
            0x60000003, 0xb0000001, 0xd8000000, 0x6c000000},
        {0x008f, 0x80000047, 0xc0000023, 0xe0000011,
            0xf0000008, 0x78000004, 0x3c000002 ,0x1e000001}};
  
    
    if ((PAL_Initialize(argc, argv)) != 0)
    {
        return (FAIL);
    }

    /*Loop through expected test results*/
    for (j = 0; j <= 4; j++)
    {
        for(i = 1; i <= 7; i++)
        {
            results = _rotr(hTestNums[j][0], i);
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
