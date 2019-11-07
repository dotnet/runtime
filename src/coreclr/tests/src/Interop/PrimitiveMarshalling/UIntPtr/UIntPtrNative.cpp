// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <xplatform.h>
#include <stdio.h>

UINT_PTR uintPtrManaged = 1000;
UINT_PTR uintPtrNative = 2000;
UINT_PTR uintPtrReturn = 3000;
UINT_PTR uintPtrErrReturn = 4000;

//
// PInvokeUIntPtrTest.cs declares that all of these APIs use STDCALL.
//
extern "C" DLL_EXPORT UINT_PTR STDMETHODCALLTYPE Marshal_In(/*[in]*/UINT_PTR uintPtr)
{
    //Check the input
    if(uintPtr != uintPtrManaged)
    {
        printf("Error in Function Marshal_In(Native Client)\n");

        //Return the error value instead if verification failed
        return uintPtrErrReturn;
    }

    return uintPtrReturn;
}

extern "C" DLL_EXPORT UINT_PTR STDMETHODCALLTYPE Marshal_InOut(/*[In,Out]*/UINT_PTR uintPtr)
{
    //Check the input
    if(uintPtr != uintPtrManaged)
    {
        printf("Error in Function Marshal_In(Native Client)\n");    

        //Return the error value instead if verification failed
        return uintPtrErrReturn;
    }

    //In-Place Change
    uintPtr = uintPtrNative;

    //Return
    return uintPtrReturn;
}

extern "C" DLL_EXPORT UINT_PTR STDMETHODCALLTYPE Marshal_Out(/*[Out]*/UINT_PTR uintPtr)
{
    uintPtr = uintPtrNative;

    //Return
    return uintPtrReturn;
}

extern "C" DLL_EXPORT UINT_PTR STDMETHODCALLTYPE MarshalPointer_In(/*[in]*/UINT_PTR *puintPtr)
{
    //Check the input
    if(*puintPtr != uintPtrManaged)
    {
        printf("Error in Function Marshal_In(Native Client)\n");
        //Return the error value instead if verification failed
        return uintPtrErrReturn;
    }
    
    return uintPtrReturn;
}

extern "C" DLL_EXPORT UINT_PTR STDMETHODCALLTYPE MarshalPointer_InOut(/*[in,out]*/UINT_PTR *puintPtr)
{
    //Check the input
    if(*puintPtr != uintPtrManaged)
    {
        printf("Error in Function Marshal_In(Native Client)\n");
        //Return the error value instead if verification failed
        return uintPtrErrReturn;
    }

    //In-Place Change
    *puintPtr = uintPtrNative;

    //Return
    return uintPtrReturn;
}

extern "C" DLL_EXPORT UINT_PTR STDMETHODCALLTYPE MarshalPointer_Out(/*[out]*/ UINT_PTR *puintPtr)
{
    *puintPtr = uintPtrNative;

    //Return
    return uintPtrReturn;
}
