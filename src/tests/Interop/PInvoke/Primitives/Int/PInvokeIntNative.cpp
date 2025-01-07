// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

extern "C" DLL_EXPORT int STDMETHODCALLTYPE Marshal_InMany(/*[in]*/int16_t i1, int16_t i2, int16_t i3, int16_t i4, int16_t i5, int16_t i6, int16_t i7, int16_t i8, int16_t i9, int16_t i10, int16_t i11, uint8_t i12, uint8_t i13, int i14, int16_t i15)
{
    //Check the input
    if(i1 != 1 || i2 != 2 || i3 != 3 || i4 != 4 || i5 != 5 || i6 != 6 || i7 != 7 || i8 != 8 || i9 != 9 || i10 != 10 || i11 != 11 || i12 != 12 || i13 != 13 || i14 != 14 || i15 != 15)
    {
        printf("Error in Function Marshal_InMany(Native Client)\n");

        //Expected
        printf("Expected: 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15\n");

        //Actual
        printf("Actual: %hi, %hi, %hi, %hi, %hi, %hi, %hi, %hi, %hi, %hi, %hi, %i, %i, %i, %hi\n", i1, i2, i3, i4, i5, i6, i7, i8, i9, i10, i11, (int)i12, (int)i13, i14, i15);

        //Return the error value instead if verification failed
        return intErrReturn;
    }

    return i1 + i2 + i3 + i4 + i5 + i6 + i7 + i8 + i9 + i10 + i11 + i12 + i13 + i14 + i15;
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE Marshal_InMany_InOutPointer(/*[in]*/int16_t i1, int16_t i2, int16_t i3, int16_t i4, int16_t i5, int16_t i6, int16_t i7, int16_t i8, int16_t i9, int16_t i10, int16_t i11, uint8_t i12, uint8_t i13, int i14, int16_t i15, /*[in,out]*/int *pintValue)
{
    //Check the input
    if(i1 != 1 || i2 != 2 || i3 != 3 || i4 != 4 || i5 != 5 || i6 != 6 || i7 != 7 || i8 != 8 || i9 != 9 || i10 != 10 || i11 != 11 || i12 != 12 || i13 != 13 || i14 != 14 || i15 != 15 || (*pintValue != 1000))
    {
        printf("Error in Function Marshal_InMany_InOutPointer(Native Client)\n");

        //Expected
        printf("Expected: 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 1000\n");

        //Actual
        printf("Actual: %hi, %hi, %hi, %hi, %hi, %hi, %hi, %hi, %hi, %hi, %hi, %i, %i, %i, %hi, %i\n", i1, i2, i3, i4, i5, i6, i7, i8, i9, i10, i11, (int)i12, (int)i13, i14, i15, *pintValue);

        //Return the error value instead if verification failed
        return intErrReturn;
    }

    *pintValue = 2000;

    return i1 + i2 + i3 + i4 + i5 + i6 + i7 + i8 + i9 + i10 + i11 + i12 + i13 + i14 + i15;
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
