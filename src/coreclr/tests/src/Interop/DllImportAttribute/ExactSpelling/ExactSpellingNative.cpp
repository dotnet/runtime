// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <stdio.h>
#include <platformdefines.h>

int intManaged = 1000;
int intNative = 2000;

int intReturn = 3000;
int intReturnA = 4000;
int intReturnW = 5000;

int intErrReturn = 6000;

extern "C" DLL_EXPORT int STDMETHODCALLTYPE Marshal_Int_InOut(/*[In,Out]*/int intValue)
{
    //Check the input
    if(intValue != intManaged)
    {
        printf("Error in Function Marshal_Int_InOut(Native Client)\n");

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

extern "C" DLL_EXPORT int STDMETHODCALLTYPE Marshal_Int_InOutA(/*[In,Out]*/int intValue)
{
    //Check the input
    if(intValue != intManaged)
    {
        printf("Error in Function Marshal_Int_InOutA(Native Client)\n");

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
    return intReturnA;
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE Marshal_Int_InOutW(/*[In,Out]*/int intValue)
{
    //Check the input
    if(intValue != intManaged)
    {
        printf("Error in Function Marshal_Int_InOutW(Native Client)\n");

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
    return intReturnW;
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE MarshalPointer_Int_InOut(/*[in,out]*/int *pintValue)
{
    //Check the input
    if(*pintValue != intManaged)
    {
        printf("Error in Function MarshalPointer_Int_InOut(Native Client)\n");

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

extern "C" DLL_EXPORT int STDMETHODCALLTYPE MarshalPointer_Int_InOutA(/*[in,out]*/int *pintValue)
{
    //Check the input
    if(*pintValue != intManaged)
    {
        printf("Error in Function MarshalPointer_Int_InOutA(Native Client)\n");

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
    return intReturnA;
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE MarshalPointer_Int_InOutW(/*[in,out]*/int *pintValue)
{
    //Check the input
    if(*pintValue != intManaged)
    {
        printf("Error in Function MarshalPointer_Int_InOutW(Native Client)\n");

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
    return intReturnW;
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE Marshal_Int_InOut2A(/*[In,Out]*/int intValue)
{
    return Marshal_Int_InOutA(intValue);
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE Marshal_Int_InOut2W(/*[In,Out]*/int intValue)
{
    return Marshal_Int_InOutW(intValue);
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE MarshalPointer_Int_InOut2A(/*[in,out]*/int *pintValue)
{
    return MarshalPointer_Int_InOutA(pintValue);
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE MarshalPointer_Int_InOut2W(/*[in,out]*/int *pintValue)
{
    return MarshalPointer_Int_InOutW(pintValue);
}
