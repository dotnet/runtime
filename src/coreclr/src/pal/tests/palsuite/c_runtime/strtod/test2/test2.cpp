// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test2.c
**
** Purpose: Tests strtod with overflows
**
**
**===================================================================*/

#include <palsuite.h>

PALTEST(c_runtime_strtod_test2_paltest_strtod_test2, "c_runtime/strtod/test2/paltest_strtod_test2")
{
    /* Representation of positive infinty for a IEEE 64-bit double */
    INT64 PosInifity = (INT64)(0x7ff00000) << 32;    
    double HugeVal = *(double*) &PosInifity;
    char *PosStr = "1E+10000";
    char *NegStr = "-1E+10000";
    double result;  
  

    if (PAL_Initialize(argc,argv))
    {
        return FAIL;
    }
    
    result = strtod(PosStr, NULL);    

    if (result != HugeVal)
    {        
        Fail("ERROR: wcstod interpreted \"%s\" as %g instead of %g\n",
            PosStr, result, HugeVal);
    }

    result = strtod(NegStr, NULL);
    
    if (result != -HugeVal)
    {
        Fail("ERROR: wcstod interpreted \"%s\" as %g instead of %g\n",
             NegStr, result, -HugeVal);
    }
    
    PAL_Terminate();

    return PASS;
}















