// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <stdio.h>
#include <xplatform.h>
#include <platformdefines.h>

int intManaged = 1000;
int intNative = 2000;
int intReturn = 3000;
int intErrReturn = 4000;

extern "C" DLL_EXPORT int STDMETHODCALLTYPE Marshal_In(/*[in]*/int intValue)
{
    //Check the input
    if(intValue != intManaged)
    {
        printf("Error in Function Marshal_In(Native Client)\n");

        //Expected
        printf("Expected:%u\n", intManaged);

        //Actual
        printf("Actual:%u\n",intValue);

        //Return the error value instead if verification failed
        return intErrReturn;
    }

    return intReturn;
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE Marshal_InOut(/*[In,Out]*/int intValue)
{
    //Check the input
    if(intValue != intManaged)
    {
        printf("Error in Function Marshal_InOut(Native Client)\n");

        //Expected
        printf("Expected:%u\n", intManaged);

        //Actual
        printf("Actual:%u\n",intValue);

        //Return the error value instead if verification failed
        return intErrReturn;
    }

    //In-Place Change
    intValue = intNative;

    //Return
    return intReturn;
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE Marshal_Out(/*[Out]*/int intValue)
{
    intValue = intNative;

    //Return
    return intReturn;
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE MarshalPointer_In(/*[in]*/int *pintValue)
{
    //Check the input
    if(*pintValue != intManaged)
    {
        printf("Error in Function MarshalPointer_In(Native Client)\n");

        //Expected
        printf("Expected:%u\n", intManaged);

        //Actual
        printf("Actual:%u\n",*pintValue);

        //Return the error value instead if verification failed
        return intErrReturn;
    }
    
    return intReturn;
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE MarshalPointer_InOut(/*[in,out]*/int *pintValue)
{
    //Check the input
    if(*pintValue != intManaged)
    {
        printf("Error in Function MarshalPointer_InOut(Native Client)\n");

        //Expected
        printf("Expected:%u\n", intManaged);

        //Actual
        printf("Actual:%u\n",*pintValue);

        //Return the error value instead if verification failed
        return intErrReturn;
    }

    //In-Place Change
    *pintValue = intNative;

    //Return
    return intReturn;
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE MarshalPointer_Out(/*[out]*/ int *pintValue)
{
    *pintValue = intNative;

    //Return
    return intReturn;
}
