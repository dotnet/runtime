// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test2.c
**
** Purpose: Tests wcstod with overflows
**
**
**===================================================================*/

#include <palsuite.h>

PALTEST(c_runtime_wcstod_test2_paltest_wcstod_test2, "c_runtime/wcstod/test2/paltest_wcstod_test2")
{
    /* Representation of positive infinty for a IEEE 64-bit double */
    INT64 PosInifity = (INT64)(0x7ff00000) << 32;    
    double HugeVal = *(double*) &PosInifity;
    char *PosStr = "1E+10000";
    char *NegStr = "-1E+10000";
    WCHAR *wideStr;
    double result;  
  

    if (PAL_Initialize(argc,argv))
    {
        return FAIL;
    }
  
    wideStr = convert(PosStr);
    result = wcstod(wideStr, NULL);    
    free(wideStr);

    if (result != HugeVal)
    {        
        Fail("ERROR: wcstod interpreted \"%s\" as %g instead of %g\n",
            PosStr, result, HugeVal);
    }



    wideStr = convert(NegStr);
    result = wcstod(wideStr, NULL);
    free(wideStr);
    
    if (result != -HugeVal)
    {
        Fail("ERROR: wcstod interpreted \"%s\" as %g instead of %g\n",
            NegStr, result, -HugeVal);
    }


    PAL_Terminate();

    return PASS;
}

