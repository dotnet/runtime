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

//BYTE 1 ==> 0 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayByte_AsByRef_AsSizeParamIndex(BYTE* arrSize, BYTE** ppActual)
{
    return CheckAndChangeArrayByRef(ppActual, arrSize, (BYTE)1, (BYTE)0);
}

//CHAR 10 ==> CHAR.Max size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArraySbyte_AsByRef_AsSizeParamIndex(CHAR* arrSize, CHAR** ppActual)
{
    return CheckAndChangeArrayByRef(ppActual, arrSize, (CHAR)10, (CHAR)CHAR_MAX); 
}

//SHORT -1 ==> 20 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayShort_AsByRef_AsSizeParamIndex(SHORT* arrSize, SHORT** ppActual)
{
    if(*arrSize != -1)
    {
        printf("%s,ManagedtoNative Error!\n",__FUNCTION__);
        printf("arrSize != -1");
        return FALSE;
    }

    SHORT* pExpectedArr = InitArray<SHORT>((SHORT)Array_Size);

    if(!EqualArray(*ppActual, (SHORT)Array_Size, pExpectedArr, (SHORT)Array_Size))
    {
        printf("%s,ManagedtoNative Error!\n",__FUNCTION__);
        return FALSE;
    }

    CoreClrFree(*ppActual);
    *ppActual = (SHORT*)CoreClrAlloc(sizeof(SHORT)*CArray_Size);

    *arrSize = CArray_Size;

    for(SHORT i = 0; i < CArray_Size; ++i)
    {
        (*ppActual)[i] = CArray_Size - 1 - i;
    }
    return TRUE;
}

//SHORT 10 ==> -1 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayShortReturnNegative_AsByRef_AsSizeParamIndex(SHORT* arrSize, SHORT** ppActual)
{
    SHORT* pExpectedArr = InitArray<SHORT>((SHORT)Array_Size);

    if(!EqualArray(*ppActual, (SHORT)Array_Size, pExpectedArr, (SHORT)Array_Size))
    {
        printf("%s,ManagedtoNative Error!\n",__FUNCTION__);
        return FALSE;
    }

    CoreClrFree(*ppActual);
    *ppActual = (SHORT*)CoreClrAlloc(sizeof(SHORT)*CArray_Size);

    *arrSize = (SHORT)-1;

    for(SHORT i = 0; i < CArray_Size; ++i)
    {
        (*ppActual)[i] = CArray_Size - 1 - i;
    }
    return TRUE;
}

//USHORT 20 ==> ushort.Max  size Array 
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayUshort_AsByRef_AsSizeParamIndex(USHORT** ppActual, USHORT* arrSize)
{
    return CheckAndChangeArrayByRef(ppActual, arrSize, (USHORT)20, (USHORT)65535);
}

//Int32 10 ==> 1 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayInt_AsByRef_AsSizeParamIndex(LONG* arrSize,LONG unused,LONG** ppActual)
{
    return CheckAndChangeArrayByRef(ppActual, arrSize, (LONG)10, (LONG)1);
}

//ULONG 1234 ==> 4321 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayUInt_AsByRef_AsSizeParamIndex(ULONG** ppActual,ULONG unused, ULONG* arrSize)
{
    return CheckAndChangeArrayByRef(ppActual, arrSize, (ULONG)1234, (ULONG)4321);
}

//LONGLONG 10 ==> 20 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayLong_AsByRef_AsSizeParamIndex(LONGLONG* arrSize, LONGLONG** ppActual)
{
    return CheckAndChangeArrayByRef(ppActual, arrSize, (LONGLONG)10, (LONGLONG)20);
}

//ULONGLONG 0 ==> 0 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayUlong_AsByRef_AsSizeParamIndex(ULONGLONG* arrSize, ULONGLONG** ppActual)
{
    return CheckAndChangeArrayByRef(ppActual, arrSize, (ULONGLONG)0, (ULONGLONG)0);
}
#ifdef _WIN32
//String size Array 20 ==> BSTR 10 size Array
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalCStyleArrayString_AsByRef_AsSizeParamIndex(short* arrSize, BSTR** ppBSTR,char *** pppStr)
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
