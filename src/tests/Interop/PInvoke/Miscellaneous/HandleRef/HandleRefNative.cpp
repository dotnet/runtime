// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <xplatform.h>
#include <platformdefines.h>

const int intManaged = 1000;
const int intNative = 2000;
const int intReturn = 3000;
const int intErrReturn = 4000;
const int expectedStackGuard = 5000;

extern "C" DLL_EXPORT int STDMETHODCALLTYPE MarshalPointer_In(/*[in]*/int *pintValue, int stackGuard)
{
    if (*pintValue != intManaged)
    {
        printf("Error in Function MarshalPointer_In(Native Client)\n");
        printf("Expected:%u\n", intManaged);
        printf("Actual:%u\n",*pintValue);

        // Return the error value instead if verification failed
        return intErrReturn;
    }
    if (stackGuard != expectedStackGuard)
    {
        printf("Stack error in Function MarshalPointer_In(Native Client)\n");
        return intErrReturn;
    }
    
    return intReturn;
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE MarshalPointer_InOut(/*[in,out]*/int *pintValue, int stackGuard)
{
    if(*pintValue != intManaged)
    {
        printf("Error in Function MarshalPointer_InOut(Native Client)\n");
        printf("Expected:%u\n", intManaged);
        printf("Actual:%u\n",*pintValue);

        // Return the error value instead if verification failed
        return intErrReturn;
    }
    if (stackGuard != expectedStackGuard)
    {
        printf("Stack error in Function MarshalPointer_In(Native Client)\n");
        return intErrReturn;
    }

    // In-Place Change
    *pintValue = intNative;

    return intReturn;
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE MarshalPointer_Out(/*[out]*/ int *pintValue, int stackGuard)
{
    *pintValue = intNative;
    if (stackGuard != expectedStackGuard)
    {
        printf("Stack error in Function MarshalPointer_In(Native Client)\n");
        return intErrReturn;
    }

    return intReturn;
}

typedef void (*GCCallback)(void);
extern "C" DLL_EXPORT int STDMETHODCALLTYPE TestNoGC(int *pintValue, GCCallback gcCallback)
{
    int origValue = *pintValue;
    gcCallback();
    int afterGCValue = *pintValue;
    if (origValue != afterGCValue)
    {
        printf("Error in Function TestNoGC(Native Client)\n");
        printf("Expected:%u\n", origValue);
        printf("Actual:%u\n", afterGCValue);
        return intErrReturn;
    }

    return intReturn;
}

extern "C" DLL_EXPORT void* STDMETHODCALLTYPE InvalidMarshalPointer_Return()
{
    return nullptr;
}
