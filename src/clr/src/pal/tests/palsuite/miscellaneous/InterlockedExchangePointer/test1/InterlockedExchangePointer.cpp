// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source: InterlockedExchangePointer
**
** Purpose: Positive test the InterlockedExchangePointer API.
**          Call InterlockedExchangePointer to exchange a pair of
**          value
**          
**
**
**============================================================*/
#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{
    int err;
    int i1 = 10;
    int i2 = 20;
    int *pOldValue = &i1;
    int *pNewValue = &i2;
    PVOID pReturnValue;
   
    /*Initialize the PAL environment*/
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        return FAIL;
    }



    /*
      Testing
      =======
    */
        
    pReturnValue = InterlockedExchangePointer((PVOID)&pOldValue,
                                     (PVOID)pNewValue);
    /*check the returned value*/
    if(*(int *)pReturnValue != i1)
    {
        Fail("\nFailed to call InterlockedExchangePointer API, "
                "return pointer does not point to the origional value\n");
    }

    /*check the exchanged value*/
    if(*pOldValue != *pNewValue)
    {
        Fail("\nFailed to call InterlockedExchangePointer API, "
                "exchanged value is not right\n");
    }



    /*
      Clean Up
    */
    PAL_Terminate();
    return PASS;
}
