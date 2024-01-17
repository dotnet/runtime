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

//uint8_t 0 ==> 20 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayByte_AsByOut_AsSizeParamIndex(uint8_t* arrSize, uint8_t** ppActual)
{
    return CheckAndChangeArrayByOut(ppActual, arrSize, (uint8_t)1);
}

//CHAR 1 ==> CHAR.Max size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArraySbyte_AsByOut_AsSizeParamIndex(CHAR* arrSize, CHAR** ppActual)
{
    return CheckAndChangeArrayByOut(ppActual, arrSize, (CHAR)SCHAR_MAX);
}

//int16_t -1 ==> 20 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayShort_AsByOut_AsSizeParamIndex(/*out*/int16_t* arrSize, int16_t** ppActual)
{
    int16_t shortArray_Size = 16384;//SHRT_MAX+1/2

    *ppActual = (int16_t*)CoreClrAlloc(sizeof(int16_t)*shortArray_Size);

    *arrSize = shortArray_Size;

    for(int16_t i = 0; i < shortArray_Size; ++i)
    {
        (*ppActual)[i] = shortArray_Size - 1 - i;
    }
    return TRUE;
}

//int16_t 10 ==> -1 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayShortReturnNegative_AsByOut_AsSizeParamIndex(int16_t* arrSize, int16_t** ppActual)
{
    *ppActual = (int16_t*)CoreClrAlloc(sizeof(int16_t)*CArray_Size);
    *arrSize = -1;

    for(int16_t i = 0; i < CArray_Size; ++i)
    {
        (*ppActual)[i] = CArray_Size - 1 - i;
    }
    return TRUE;
}

//uint16_t ? ==> ushort.Max ==>  size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayUshort_AsByOut_AsSizeParamIndex(uint16_t** ppActual, uint16_t* arrSize)
{
    return CheckAndChangeArrayByOut(ppActual, arrSize, (uint16_t)USHRT_MAX);
}

//Int32 ? ==> 20 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayInt_AsByOut_AsSizeParamIndex(int32_t* arrSize,int32_t** ppActual)
{
    return CheckAndChangeArrayByOut(ppActual, arrSize, (int32_t)0);
}

//uint32_t 10 ==> 20 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayUInt_AsByOut_AsSizeParamIndex(uint32_t* arrSize, uint32_t** ppActual)
{
    return CheckAndChangeArrayByOut(ppActual, arrSize, (uint32_t)20);
}

//int64_t 10 ==> 20 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayLong_AsByOut_AsSizeParamIndex(int64_t* arrSize, int64_t** ppActual)
{
    return CheckAndChangeArrayByOut(ppActual, arrSize, (int64_t)20);
}

//uint64_t 10 ==> 20 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayUlong_AsByOut_AsSizeParamIndex(uint64_t** ppActual,uint64_t* arrSize,uint64_t _unused)
{
    return CheckAndChangeArrayByOut(ppActual, arrSize, (uint64_t)1000);
}
#ifdef _WIN32
//String 10 size Array ==> BSTR 20 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayString_AsByOut_AsSizeParamIndex(BSTR** ppBSTR,int16_t* arrSize)
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
