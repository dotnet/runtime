// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// PInvokePassingByRefNative.cpp : Defines the entry point for the DLL application.
//
#include <xplatform.h>
#include <limits.h>
#include "helper.h"

//#####################################################################
//ByRef Array, ByRef SizeParamIndex
//#####################################################################

//uint8_t 1 ==> 0 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayByte_AsByRef_AsSizeParamIndex(uint8_t* arrSize, uint8_t** ppActual)
{
    return CheckAndChangeArrayByRef(ppActual, arrSize, (uint8_t)1, (uint8_t)0);
}

//CHAR 10 ==> CHAR.Max size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArraySbyte_AsByRef_AsSizeParamIndex(CHAR* arrSize, CHAR** ppActual)
{
    return CheckAndChangeArrayByRef(ppActual, arrSize, (CHAR)10, (CHAR)CHAR_MAX);
}

//int16_t -1 ==> 20 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayShort_AsByRef_AsSizeParamIndex(int16_t* arrSize, int16_t** ppActual)
{
    if(*arrSize != -1)
    {
        printf("%s,ManagedtoNative Error!\n",__FUNCTION__);
        printf("arrSize != -1");
        return FALSE;
    }

    int16_t* pExpectedArr = InitArray<int16_t>((int16_t)Array_Size);

    if(!EqualArray(*ppActual, (int16_t)Array_Size, pExpectedArr, (int16_t)Array_Size))
    {
        printf("%s,ManagedtoNative Error!\n",__FUNCTION__);
        return FALSE;
    }

    CoreClrFree(*ppActual);
    *ppActual = (int16_t*)CoreClrAlloc(sizeof(int16_t)*CArray_Size);

    *arrSize = CArray_Size;

    for(int16_t i = 0; i < CArray_Size; ++i)
    {
        (*ppActual)[i] = CArray_Size - 1 - i;
    }
    return TRUE;
}

//int16_t 10 ==> -1 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayShortReturnNegative_AsByRef_AsSizeParamIndex(int16_t* arrSize, int16_t** ppActual)
{
    int16_t* pExpectedArr = InitArray<int16_t>((int16_t)Array_Size);

    if(!EqualArray(*ppActual, (int16_t)Array_Size, pExpectedArr, (int16_t)Array_Size))
    {
        printf("%s,ManagedtoNative Error!\n",__FUNCTION__);
        return FALSE;
    }

    CoreClrFree(*ppActual);
    *ppActual = (int16_t*)CoreClrAlloc(sizeof(int16_t)*CArray_Size);

    *arrSize = (int16_t)-1;

    for(int16_t i = 0; i < CArray_Size; ++i)
    {
        (*ppActual)[i] = CArray_Size - 1 - i;
    }
    return TRUE;
}

//uint16_t 20 ==> ushort.Max  size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayUshort_AsByRef_AsSizeParamIndex(uint16_t** ppActual, uint16_t* arrSize)
{
    return CheckAndChangeArrayByRef(ppActual, arrSize, (uint16_t)20, (uint16_t)65535);
}

//Int32 10 ==> 1 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayInt_AsByRef_AsSizeParamIndex(int32_t* arrSize,int32_t unused,int32_t** ppActual)
{
    return CheckAndChangeArrayByRef(ppActual, arrSize, (int32_t)10, (int32_t)1);
}

//uint32_t 1234 ==> 4321 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayUInt_AsByRef_AsSizeParamIndex(uint32_t** ppActual,uint32_t unused, uint32_t* arrSize)
{
    return CheckAndChangeArrayByRef(ppActual, arrSize, (uint32_t)1234, (uint32_t)4321);
}

//int64_t 10 ==> 20 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayLong_AsByRef_AsSizeParamIndex(int64_t* arrSize, int64_t** ppActual)
{
    return CheckAndChangeArrayByRef(ppActual, arrSize, (int64_t)10, (int64_t)20);
}

//uint64_t 0 ==> 0 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayUlong_AsByRef_AsSizeParamIndex(uint64_t* arrSize, uint64_t** ppActual)
{
    return CheckAndChangeArrayByRef(ppActual, arrSize, (uint64_t)0, (uint64_t)0);
}
#ifdef _WIN32
//String size Array 20 ==> BSTR 10 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayString_AsByRef_AsSizeParamIndex(int16_t* arrSize, BSTR** ppBSTR,char *** pppStr)
{
    BSTR* pExpectedArr = InitArrayBSTR(20);

    if(!EqualArrayBSTR(*ppBSTR,*arrSize,pExpectedArr,20))
    {
        printf("%s,ManagedtoNative Error!\n",__FUNCTION__);
        return FALSE;
    }

    CoreClrFree(*ppBSTR);

    *ppBSTR = (BSTR*)CoreClrAlloc(sizeof(BSTR)*10);
    for(int i = 0;i<10;++i)
    {
        (*ppBSTR)[i] = ToBSTR(10 - 1 - i);
    }

    *arrSize = 10;

    return TRUE;
}
#endif
