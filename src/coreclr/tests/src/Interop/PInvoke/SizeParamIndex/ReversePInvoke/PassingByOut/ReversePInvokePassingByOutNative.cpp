// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ReversePInvokePassingByOutNative.cpp : Defines the entry point for the DLL application.
//
#include <xplatform.h>
#include <limits.h>
#include "platformdefines.h"
#include "helper.h"

//Func Pointer
typedef BOOL (__cdecl *DelByteArrByOutAsCdeclCaller)(BYTE** arrByte, BYTE* arraySize);
typedef BOOL (__cdecl *DelSbyteArrByOutAsCdeclCaller)(CHAR* arraySize, CHAR** arrSbyte);
typedef BOOL (__cdecl *DelShortArrByOutAsCdeclCaller)(SHORT** arrShort, SHORT* arraySize);
typedef BOOL (__cdecl *DelUshortArrByOutAsCdeclCaller)(USHORT** arrUshort, USHORT* arraySize);
typedef BOOL (__cdecl *DelInt32ArrByOutAsCdeclCaller)(LONG** arrInt32, LONG* arraySize);
typedef BOOL (__cdecl *DelUint32ArrByOutAsCdeclCaller)(ULONG** arrUint32, ULONG* arraySize);
typedef BOOL (__cdecl *DelLongArrByOutAsCdeclCaller)(LONGLONG** arrLong, LONGLONG* arraySize);
typedef BOOL (__cdecl *DelUlongArrByOutAsCdeclCaller)(ULONGLONG** arrUlong, ULONGLONG* arraySize);
typedef BOOL (__cdecl *DelStringArrByOutAsCdeclCaller)(BSTR** arrString, LONG* arraySize);

//#######################################################
//Test Method
//#######################################################

//BYTE 0 ==> 20 size Array
extern "C" DLL_EXPORT BOOL __cdecl DoCallBack_MarshalByteArray_AsParam_AsByOut(DelByteArrByOutAsCdeclCaller caller)
{
    BYTE arrSize = 0;
    BYTE* arrByte = InitArray<BYTE>(arrSize);

    if(!caller(&arrByte, &arrSize))
    {
        printf("DoCallBack_MarshalByteArray_AsParam_AsByOut:\n\tThe Caller returns wrong value\n");
        CoreClrFree(arrByte);
        return FALSE;
    }

    return CheckArray(arrByte, arrSize, (BYTE)20);
}

//CHAR 1 ==> CHAR.Max size Array
extern "C" DLL_EXPORT BOOL __cdecl DoCallBack_MarshalSbyteArray_AsParam_AsByOut(DelSbyteArrByOutAsCdeclCaller caller)
{
    CHAR arrSize = 1;
    CHAR* arrSbyte = InitArray<CHAR>((size_t)arrSize);

    if(!caller(&arrSize, &arrSbyte))
    {
        printf("DoCallBack_MarshalSbyteArray_AsParam_AsByOut:\n\tThe Caller returns wrong value\n");
        CoreClrFree(arrSbyte);
        return FALSE;
    }

    return CheckArray(arrSbyte, (size_t)arrSize, (CHAR)127);
}

//SHORT -1 ==> 20 size Array(Actual: 10 ==> 20)
extern "C" DLL_EXPORT BOOL __cdecl DoCallBack_MarshalShortArray_AsParam_AsByOut(DelShortArrByOutAsCdeclCaller caller)
{
    SHORT arrSize = -1;
    SHORT* arrShort = InitArray<SHORT>(SHORT(10));

    if(!caller(&arrShort, &arrSize))
    {
        printf("DoCallBack_MarshalShortArray_AsParam_AsByOut:\n\tThe Caller returns wrong value\n");
        CoreClrFree(arrShort);
        return FALSE;
    }

    return CheckArray(arrShort, (size_t)arrSize, (SHORT)20);
}

//SHORT 10 ==> -1 size Array(Actual: 10 ==> 20)
extern "C" DLL_EXPORT BOOL __cdecl DoCallBack_MarshalShortArrayReturnNegativeSize_AsParam_AsByOut(DelShortArrByOutAsCdeclCaller caller)
{
    SHORT arrSize = 10;
    SHORT* arrShort = InitArray<SHORT>((size_t)arrSize);

    if(!caller(&arrShort, &arrSize))
    {
        printf("DoCallBack_MarshalShortArrayReturnNegativeSize_AsParam_AsByOut:\n\tThe Caller returns wrong value\n");
        CoreClrFree(arrShort);
        return FALSE;
    }

    if(arrSize == -1)
        return CheckArray(arrShort, (SHORT)20, (SHORT)20);
    else
        return FALSE;
}

//USHORT ushort.Max ==> 20 size Array
extern "C" DLL_EXPORT BOOL __cdecl DoCallBack_MarshalUshortArray_AsParam_AsByOut(DelUshortArrByOutAsCdeclCaller caller)
{
    USHORT arrSize = 65535;
    USHORT* arrUshort = InitArray<USHORT>(arrSize);

    if(!caller(&arrUshort, &arrSize))
    {
        printf("DoCallBack_MarshalUshortArray_AsParam_AsByOut:\n\tThe Caller returns wrong value\n");
        CoreClrFree(arrUshort);
        return FALSE;
    }

    return CheckArray(arrUshort, arrSize, (USHORT)20);
}

//Int32 10 ==> 20 size Array
extern "C" DLL_EXPORT BOOL __cdecl DoCallBack_MarshalInt32Array_AsParam_AsByOut(DelInt32ArrByOutAsCdeclCaller caller)
{
    LONG arrSize = 10;
    LONG* arrInt32 = InitArray<LONG>((size_t)arrSize);

    if(!caller(&arrInt32, &arrSize))
    {
        printf("DoCallBack_MarshalInt32Array_AsParam_AsByOut:\n\tThe Caller returns wrong value\n");
        CoreClrFree(arrInt32);
        return FALSE;
    }

    return CheckArray(arrInt32, (size_t)arrSize, (LONG)20);
}

//UInt32 10 ==> 20 size Array
extern "C" DLL_EXPORT BOOL __cdecl DoCallBack_MarshalUint32Array_AsParam_AsByOut(DelUint32ArrByOutAsCdeclCaller caller)
{
    ULONG arrSize = 10;
    ULONG* arrUint32 = InitArray<ULONG>(arrSize);

    if(!caller(&arrUint32, &arrSize))
    {
        printf("DoCallBack_MarshalUint32Array_AsParam_AsByOut:\n\tThe Caller returns wrong value\n");
        CoreClrFree(arrUint32);
        return FALSE;
    }

    return CheckArray(arrUint32, arrSize, (ULONG)20);
}

//LONGLONG 10 ==> 20 size Array
extern "C" DLL_EXPORT BOOL __cdecl DoCallBack_MarshalLongArray_AsParam_AsByOut(DelLongArrByOutAsCdeclCaller caller)
{
    LONGLONG arrSize = 10;
    LONGLONG* arrLong = InitArray<LONGLONG>((SIZE_T)arrSize);

    if(!caller(&arrLong, &arrSize))
    {
        printf("DoCallBack_MarshalLongArray_AsParam_AsByOut:\n\tThe Caller returns wrong value\n");
        CoreClrFree(arrLong);
        return FALSE;
    }

    return CheckArray(arrLong, (SIZE_T)arrSize, 20);
}

//ULONGLONG 10 ==> 20 size Array
extern "C" DLL_EXPORT BOOL __cdecl DoCallBack_MarshalUlongArray_AsParam_AsByOut(DelUlongArrByOutAsCdeclCaller caller)
{
    ULONGLONG arrSize = 10;
    ULONGLONG* arrUlong = InitArray<ULONGLONG>((SIZE_T)arrSize);

    if(!caller(&arrUlong, &arrSize))
    {
        printf("DoCallBack_MarshalUlongArray_AsParam_AsByOut:\n\tThe Caller returns wrong value\n");
        CoreClrFree(arrUlong);
        return FALSE;
    }

    return CheckArray(arrUlong, (SIZE_T)arrSize, 20);
}

#ifdef _WIN32
//BSTR 10 ==> 20 size Array
extern "C" DLL_EXPORT BOOL __cdecl DoCallBack_MarshalStringArray_AsParam_AsByOut(DelStringArrByOutAsCdeclCaller caller)
{
    LONG arrSize = 10;
    BSTR* arrString = InitArrayBSTR(arrSize);

    if(!caller(&arrString, &arrSize))
    {
        printf("DoCallBack_MarshalStringArray_AsParam_AsByOut:\n\tThe Caller returns wrong value\n");
        CoreClrFree(arrString);
        return FALSE;
    }

    LONG ExpectedArraySize = 20;
    BSTR* pExpectedArr = (BSTR*)CoreClrAlloc(sizeof(BSTR)*ExpectedArraySize);
    for(LONG i = 0; i < ExpectedArraySize; ++i)
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
