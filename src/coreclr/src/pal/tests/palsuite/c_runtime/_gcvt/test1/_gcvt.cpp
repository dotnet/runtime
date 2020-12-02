// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: _gcvt.c
**
** Purpose: Positive test the _gcvt API.
**          Call _gcvt to convert a floatable value to a string 
**          with specified sigficant digits stored
**
**
**============================================================*/
#include <palsuite.h>

PALTEST(c_runtime__gcvt_test1_paltest_gcvt_test1, "c_runtime/_gcvt/test1/paltest_gcvt_test1")
{
    int err;
    double dValue = -3.1415926535;
    char buffer[1024];
    char *pChar7 = "-3.141593";

    /*Initialize the PAL environment*/
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        return FAIL;
    }


    /* zero the buffer */
    memset(buffer, 0, 1024);

    
    /*

     Testing
     =======
        
     To convert a floating-point value to 
     a string to save 7 significant digits
    */
    _gcvt(dValue, 7, buffer);
    if(strcmp(pChar7, buffer))
    {
        Fail("\nFailed to call _gcvt to convert a floating-point value "
                "to a string with 7 sigficants digits stored\n");
    }
   

    /* 
       Clean up and exit
    */

    PAL_Terminate();
    return PASS;
}
