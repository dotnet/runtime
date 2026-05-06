// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ReversePInvokePassingByRefNative.cpp : Defines the entry point for the DLL application.
#include <xplatform.h>
#include <limits.h>
#include "platformdefines.h"
#include "helper.h"

//Func Pointer
typedef BOOL (__cdecl *DelByteArrByRefAsCdeclCaller)(uint8_t** arrByte, uint8_t* arraySize);
typedef BOOL (__cdecl *DelSbyteArrByRefAsCdeclCaller)(CHAR* arraySize, CHAR** arrSbyte);
typedef BOOL (__cdecl *DelShortArrByRefAsCdeclCaller)(int16_t** arrShort, int16_t* arraySize);
typedef BOOL (__cdecl *DelUshortArrByRefAsCdeclCaller)(uint16_t** arrUshort, uint16_t* arraySize);
typedef BOOL (__cdecl *DelInt32ArrByRefAsCdeclCaller)(int32_t** arrInt32, int32_t* arraySize);
typedef BOOL (__cdecl *DelUint32ArrByRefAsCdeclCaller)(uint32_t** arrUint32, uint32_t* arraySize);
typedef BOOL (__cdecl *DelLongArrByRefAsCdeclCaller)(int64_t** arrLong, int64_t* arraySize);
typedef BOOL (__cdecl *DelUlongArrByRefAsCdeclCaller)(uint64_t** arrUlong, uint64_t* arraySize);
typedef BOOL (__cdecl *DelStringArrByRefAsCdeclCaller)(BSTR** arrString, int32_t* arraySize);

//#######################################################
//Test Method
//#######################################################

//uint8_t 0 ==> 20 size Array
extern "C" DLL_EXPORT BOOL __cdecl DoCallBack_MarshalByteArray_AsParam_AsByRef(DelByteArrByRefAsCdeclCaller caller)
{
    uint8_t arrSize = 0;
    uint8_t* arrByte = InitArray<uint8_t>(arrSize);

    if(!caller(&arrByte, &arrSize))
    {
        printf("DoCallBack_MarshalByteArray_AsParam_AsByRef:\n\tThe Caller returns wrong value\n");
        CoreClrFree(arrByte);
        return FALSE;
    }

    return CheckArray(arrByte, arrSize, (uint8_t)20);
}

//CHAR 1 ==> CHAR.Max size Array
extern "C" DLL_EXPORT BOOL __cdecl DoCallBack_MarshalSbyteArray_AsParam_AsByRef(DelSbyteArrByRefAsCdeclCaller caller)
{
    CHAR arrSize = 1;
    CHAR* arrSbyte = InitArray<CHAR>((size_t)arrSize);

    if(!caller(&arrSize, &arrSbyte))
    {
        printf("DoCallBack_MarshalSbyteArray_AsParam_AsByRef:\n\tThe Caller returns wrong value\n");
        CoreClrFree(arrSbyte);
        return FALSE;
    }

    return CheckArray(arrSbyte, (size_t)arrSize, (CHAR)127);
}

//int16_t -1 ==> 20 size Array(Actual: 10 ==> 20)
extern "C" DLL_EXPORT BOOL __cdecl DoCallBack_MarshalShortArray_AsParam_AsByRef(DelShortArrByRefAsCdeclCaller caller)
{
    int16_t arrSize = -1;
    int16_t* arrShort = InitArray<int16_t>(int16_t(10));

    if(!caller(&arrShort, &arrSize))
    {
        printf("DoCallBack_MarshalShortArray_AsParam_AsByRef:\n\tThe Caller returns wrong value\n");
        CoreClrFree(arrShort);
        return FALSE;
    }

    return CheckArray(arrShort, (size_t)arrSize, (int16_t)20);
}

//int16_t 10 ==> -1 size Array(Actual: 10 ==> 20)
extern "C" DLL_EXPORT BOOL __cdecl DoCallBack_MarshalShortArrayReturnNegativeSize_AsParam_AsByRef(DelShortArrByRefAsCdeclCaller caller)
{
    int16_t arrSize = 10;
    int16_t* arrShort = InitArray<int16_t>((size_t)arrSize);

    if(!caller(&arrShort, &arrSize))
    {
        printf("DoCallBack_MarshalShortArrayReturnNegativeSize_AsParam_AsByRef:\n\tThe Caller returns wrong value\n");
        CoreClrFree(arrShort);
        return FALSE;
    }

    if(arrSize == -1)
        return CheckArray(arrShort, (int16_t)20, (int16_t)20);
    else
        return FALSE;
}

//uint16_t ushort.Max ==> 20 size Array
extern "C" DLL_EXPORT BOOL __cdecl DoCallBack_MarshalUshortArray_AsParam_AsByRef(DelUshortArrByRefAsCdeclCaller caller)
{
    uint16_t arrSize = 65535;
    uint16_t* arrUshort = InitArray<uint16_t>(arrSize);

    if(!caller(&arrUshort, &arrSize))
    {
        printf("DoCallBack_MarshalUshortArray_AsParam_AsByRef:\n\tThe Caller returns wrong value\n");
        CoreClrFree(arrUshort);
        return FALSE;
    }

    return CheckArray(arrUshort, arrSize, (uint16_t)20);
}

//Int32 10 ==> 20 size Array
extern "C" DLL_EXPORT BOOL __cdecl DoCallBack_MarshalInt32Array_AsParam_AsByRef(DelInt32ArrByRefAsCdeclCaller caller)
{
    int32_t arrSize = 10;
    int32_t* arrInt32 = InitArray<int32_t>((size_t)arrSize);

    if(!caller(&arrInt32, &arrSize))
    {
        printf("DoCallBack_MarshalInt32Array_AsParam_AsByRef:\n\tThe Caller returns wrong value\n");
        CoreClrFree(arrInt32);
        return FALSE;
    }

    return CheckArray(arrInt32, (size_t)arrSize, (int32_t)20);
}

//UInt32 10 ==> 20 size Array
extern "C" DLL_EXPORT BOOL __cdecl DoCallBack_MarshalUint32Array_AsParam_AsByRef(DelUint32ArrByRefAsCdeclCaller caller)
{
    uint32_t arrSize = 10;
    uint32_t* arrUint32 = InitArray<uint32_t>(arrSize);

    if(!caller(&arrUint32, &arrSize))
    {
        printf("DoCallBack_MarshalUint32Array_AsParam_AsByRef:\n\tThe Caller returns wrong value\n");
        CoreClrFree(arrUint32);
        return FALSE;
    }

    return CheckArray(arrUint32, arrSize, (uint32_t)20);
}

//int64_t 10 ==> 20 size Array
extern "C" DLL_EXPORT BOOL __cdecl DoCallBack_MarshalLongArray_AsParam_AsByRef(DelLongArrByRefAsCdeclCaller caller)
{
    int64_t arrSize = 10;
    int64_t* arrLong = InitArray<int64_t>(SIZE_T(arrSize));

    if(!caller(&arrLong, &arrSize))
    {
        printf("DoCallBack_MarshalLongArray_AsParam_AsByRef:\n\tThe Caller returns wrong value\n");
        CoreClrFree(arrLong);
        return FALSE;
    }

    return CheckArray(arrLong, (SIZE_T)arrSize, 20);
}

//uint64_t 10 ==> 20 size Array
extern "C" DLL_EXPORT BOOL __cdecl DoCallBack_MarshalUlongArray_AsParam_AsByRef(DelUlongArrByRefAsCdeclCaller caller)
{
    uint64_t arrSize = 10;
    uint64_t* arrUlong = InitArray<uint64_t>(SIZE_T(arrSize));

    if(!caller(&arrUlong, &arrSize))
    {
        printf("DoCallBack_MarshalUlongArray_AsParam_AsByRef:\n\tThe Caller returns wrong value\n");
        CoreClrFree(arrUlong);
        return FALSE;
    }

    return CheckArray(arrUlong, (SIZE_T)arrSize, 20);
}
#ifdef _WIN32
//BSTR 10 ==> 20 size Array
extern "C" DLL_EXPORT BOOL __cdecl DoCallBack_MarshalStringArray_AsParam_AsByRef(DelStringArrByRefAsCdeclCaller caller)
{
    int32_t arrSize = 10;
    BSTR* arrString = InitArrayBSTR(arrSize);

    if(!caller(&arrString, &arrSize))
    {
        printf("DoCallBack_MarshalStringArray_AsParam_AsByRef:\n\tThe Caller returns wrong value\n");
        CoreClrFree(arrString);
        return FALSE;
    }

    int32_t ExpectedArraySize = 20;
    BSTR* pExpectedArr = (BSTR*)CoreClrAlloc(sizeof(BSTR)*ExpectedArraySize);
    for(int32_t i = 0; i < ExpectedArraySize; ++i)
    {
        pExpectedArr[i] = ToBSTR(ExpectedArraySize - 1 - i);
    }

    if(!EqualArrayBSTR(arrString, arrSize, pExpectedArr, ExpectedArraySize))
    {
        printf("ManagedtoNative Error in Method: %s!\n",__FUNCTION__);
        return FALSE;
    }

    CoreClrFree(arrString);
    CoreClrFree(pExpectedArr);
    return TRUE;
}
#endif
