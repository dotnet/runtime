// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// PInvokePassingByOutNative.cpp : Defines the entry point for the DLL application.
//
#include <xplatform.h>
#include <limits.h>
#include "platformdefines.h"
#include "helper.h"

//#####################################################################
//ByOut Array, ByRef SizeParamIndex
//#####################################################################

//BYTE 0 ==> 20 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayByte_AsByOut_AsSizeParamIndex(BYTE* arrSize, BYTE** ppActual)
{
    return CheckAndChangeArrayByOut(ppActual, arrSize, (BYTE)1);
}

//CHAR 1 ==> CHAR.Max size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArraySbyte_AsByOut_AsSizeParamIndex(CHAR* arrSize, CHAR** ppActual)
{
    return CheckAndChangeArrayByOut(ppActual, arrSize, (CHAR)SCHAR_MAX); 
}

//SHORT -1 ==> 20 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayShort_AsByOut_AsSizeParamIndex(/*out*/SHORT* arrSize, SHORT** ppActual)
{
    short shortArray_Size = 16384;//SHRT_MAX+1/2

    *ppActual = (SHORT*)CoreClrAlloc(sizeof(SHORT)*shortArray_Size);

    *arrSize = shortArray_Size;

    for(SHORT i = 0; i < shortArray_Size; ++i)
    {
        (*ppActual)[i] = shortArray_Size - 1 - i;
    }
    return TRUE;
}

//SHORT 10 ==> -1 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayShortReturnNegative_AsByOut_AsSizeParamIndex(SHORT* arrSize, SHORT** ppActual)
{
    *ppActual = (SHORT*)CoreClrAlloc(sizeof(SHORT)*CArray_Size);
    *arrSize = -1;

    for(SHORT i = 0; i < CArray_Size; ++i)
    {
        (*ppActual)[i] = CArray_Size - 1 - i;
    }
    return TRUE;
}

//USHORT ? ==> ushort.Max ==>  size Array 
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayUshort_AsByOut_AsSizeParamIndex(USHORT** ppActual, USHORT* arrSize)
{
    return CheckAndChangeArrayByOut(ppActual, arrSize, (USHORT)USHRT_MAX);
}

//Int32 ? ==> 20 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayInt_AsByOut_AsSizeParamIndex(LONG* arrSize,LONG** ppActual)
{
    return CheckAndChangeArrayByOut(ppActual, arrSize, (LONG)0);
}

//ULONG 10 ==> 20 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayUInt_AsByOut_AsSizeParamIndex(ULONG* arrSize, ULONG** ppActual)
{
    return CheckAndChangeArrayByOut(ppActual, arrSize, (ULONG)20);
}

//LONGLONG 10 ==> 20 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayLong_AsByOut_AsSizeParamIndex(LONGLONG* arrSize, LONGLONG** ppActual)
{
    return CheckAndChangeArrayByOut(ppActual, arrSize, (LONGLONG)20);
}

//ULONGLONG 10 ==> 20 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayUlong_AsByOut_AsSizeParamIndex(ULONGLONG** ppActual,ULONGLONG* arrSize,ULONGLONG _unused)
{
    return CheckAndChangeArrayByOut(ppActual, arrSize, (ULONGLONG)1000);
}
#ifdef _WIN32
//String 10 size Array ==> BSTR 20 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayString_AsByOut_AsSizeParamIndex(BSTR** ppBSTR,short* arrSize)
{
    *ppBSTR = (BSTR*)CoreClrAlloc(sizeof(BSTR)*CArray_Size);
    for(int i = 0;i<CArray_Size;++i)
    {
        (*ppBSTR)[i] = ToBSTR(CArray_Size - 1 - i);
    }

    *arrSize = CArray_Size;

    return TRUE;
}
#endif
