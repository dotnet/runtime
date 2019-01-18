// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <stdio.h>
#include <stdlib.h>
#include <windows.h>
#include <xplatform.h>
#include <platformdefines.h>
#include "StructDefs.h" //all the unmanaged struct defs are in this file

//Global Handle Return Value
HANDLE returnHandleValue = (HANDLE)123;

//Method for Invalid MarshalAs tests
extern "C" DLL_EXPORT BOOL __stdcall SHFldInvalid_MA(StructWithSHFld s)
{
    printf("\t\tIN SHFldInvalid_MA!\n");
    return TRUE;
}

extern "C" DLL_EXPORT HANDLE __stdcall SHInvalid_retMA16()
{
    printf("\t\tIN SHInvalid_retMA16!\n");
    HANDLE *hnd = (HANDLE*)GlobalAlloc(0, sizeof(HANDLE));  //new HANDLE
    *hnd = returnHandleValue;
    return (*hnd);
}

//////////////Methods used by RunParamTests
///////////////////////////////////////////
extern "C" DLL_EXPORT BOOL __stdcall SHParam_In(HANDLE sh1, int sh1Value)
{
    if( (intptr_t)sh1 != sh1Value )
        return FALSE;
    return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall SHParam_Out(HANDLE* sh1)
{
    *sh1 = returnHandleValue;
    return TRUE;
}

extern "C" DLL_EXPORT HRESULT _stdcall SHParam_OutRetVal(HANDLE* sh1)
{
    *sh1 = returnHandleValue;
    return S_OK;
}

extern "C" DLL_EXPORT BOOL __stdcall SHParam_Ref(HANDLE* sh1, int sh1Value)
{
    if( (intptr_t)*sh1 != sh1Value )
        return FALSE;
    //change the value of the HANDLE---this is equivalent to assigning a new HANDLE
    *sh1 = returnHandleValue;
    return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall SHParam_Multiple(HANDLE sh1, HANDLE* sh2, HANDLE* sh3, int sh1Value, int sh3Value)
{
    if( (intptr_t)sh1 != sh1Value )
    {
        printf("\t\tIn SHParam_Multiple: sh1 value not received as expected.\n");
        return FALSE;
    }
    else if( (intptr_t)*sh3 != sh3Value )
    {
        printf("\t\tIn SHParam_Multiple: sh3 value not received as expected.\n");
        //printf("\t\t\t*sh3 = %d, sh3Value = %d\n", (int)*sh3, sh3Value);
        return FALSE;
    }
    //change the out and ref values
    *sh2 = returnHandleValue;
    *sh3 = returnHandleValue;
    return TRUE; 
}

//////////////Methods used by RunStructParamTests
/////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL __stdcall SHStructParam_In(StructWithSHFld s, int shndValue)
{
    if( (intptr_t)s.hnd != shndValue ) 
        return FALSE;
    s.hnd = returnHandleValue; //try to change hnd value; should not be reflected on managed side
    return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall SHStructParam_Out(StructWithSHFld* ps)
{
    ps->hnd = returnHandleValue;
    return TRUE;
}

extern "C" DLL_EXPORT HRESULT _stdcall SHStructParam_OutRetVal(StructWithSHFld* ps)
{
    ps->hnd = returnHandleValue;
    return S_OK;
}

//this method will NOT change the value of the ref SH subclass field
extern "C" DLL_EXPORT BOOL __stdcall SHStructParam_Ref1(StructWithSHFld* ps, int shndValue)
{
    if( (intptr_t)(ps->hnd) != shndValue ) 
        return FALSE;
    return TRUE;
}

//this method will change the value of the SH subclass field
extern "C" DLL_EXPORT BOOL __stdcall SHStructParam_Ref2(StructWithSHFld* ps, int shndValue)
{
    if( (intptr_t)(ps->hnd) != shndValue ) 
        return FALSE;
    //change the value of the HANDLE---this is equivalent to assigning a new HANDLE
    ps->hnd = returnHandleValue;
    return TRUE;
}

//this takes an out param and so is expected to result in an exception
extern "C" DLL_EXPORT BOOL __stdcall SHStructParam_Multiple1(StructWithSHFld s1, StructWithSHFld* ps2,
                                       StructWithSHFld* ps3, int s1hndValue, 
                                       int s3hndValue)
{
    if( (intptr_t)s1.hnd != s1hndValue || (intptr_t)(ps3->hnd) != s3hndValue ) 
        return FALSE;
    //change the handle field of the out parameter
    ps2->hnd = returnHandleValue;
    return TRUE;
}

//this takes a ref param and does not change the SH subclass field
extern "C" DLL_EXPORT BOOL __stdcall SHStructParam_Multiple2(StructWithSHFld s1, StructWithSHFld* ps2,
                                       int s1hndValue, int s2hndValue)
{
    if( (intptr_t)s1.hnd != s1hndValue || (intptr_t)(ps2->hnd) != s2hndValue ) 
        return FALSE;
    return TRUE;
}

//this takes a ref param and tries to change the SH subclass field and so is expected to result in an exception
extern "C" DLL_EXPORT BOOL __stdcall SHStructParam_Multiple3(StructWithSHFld s1, StructWithSHFld* ps2,
                                       int s1hndValue, int s2hndValue)
{
    if( (intptr_t)s1.hnd != s1hndValue || (intptr_t)(ps2->hnd) != s2hndValue ) 
        return FALSE;
    //change the handle field of the ref parameter
    ps2->hnd = returnHandleValue;
    return TRUE;
}

//////////////Methods used by RunMiscellaneousTests
///////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL __stdcall SHArrayParam(HANDLE* ptoArr, int* ptoArrInt32s, int length)
{
    for(int i = 0; i < length; i++)
        if( (intptr_t)ptoArr[i] != ptoArrInt32s[i] )
        {
            printf("\t\tptoArr[%d] = %d, ptoArrInt32s[%d] = %d\n", i, (int)(intptr_t)ptoArr[i], i, ptoArrInt32s[i]);
            return FALSE;
        }
        return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall SHStructArrayParam(StructWithSHFld* ptoArr, int* ptoArrInt32s, int length)
{
    for(int i = 0; i < length; i++)
        if( (intptr_t)(ptoArr[i].hnd) != ptoArrInt32s[i] )
            return FALSE;
    return TRUE;
}

extern "C" DLL_EXPORT HANDLE __stdcall	SHReturn()
{
    HANDLE *hnd = (HANDLE*)GlobalAlloc(0, sizeof(HANDLE));  //new HANDLE
    *hnd = returnHandleValue;
    return (*hnd);
}

extern "C" DLL_EXPORT StructWithSHFld __stdcall	SHReturnStruct()
{
    StructWithSHFld* ptoStruct = (StructWithSHFld*)GlobalAlloc(0, sizeof(StructWithSHFld));
    ptoStruct->hnd = returnHandleValue;
    return (*ptoStruct);
}

extern "C" DLL_EXPORT BOOL __stdcall SHStructNestedParam(StructNestedParent sn, int shndValue)
{
    if( (intptr_t)sn.snOneDeep.s.hnd != shndValue ) 
        return FALSE;
    return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall SHStructParam_In2(StructWithSHArrayFld s, int* ptoArrInt32s, int length)
{
    for(int i = 0; i < length; i++)
        if( (intptr_t)s.sharr[i] != ptoArrInt32s[i] )
        {
            printf("\t\ts.sharr[%d] = %d, ptoArrInt32s[%d] = %d\n", i, (int)(intptr_t)s.sharr[i], i, ptoArrInt32s[i]);
            return FALSE;
        }
        return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall SHMixedParam1(HANDLE sh1, HANDLE* sh2, HANDLE* sh3, StructWithSHFld s1, StructWithSHFld s2,
                             StructWithSHFld* s3, int sh1Value, int sh3Value, int s1fldValue, int s2fldValue,
                             int s3fldValue)
{
    if( (intptr_t)sh1 != sh1Value || (intptr_t)*sh3 != sh3Value || (intptr_t)s1.hnd != s1fldValue || (intptr_t)s2.hnd != s2fldValue
        || (intptr_t)s3->hnd != s3fldValue )
        return FALSE;
    *sh2 = returnHandleValue; //the out parameter
    return TRUE;
}

BOOL static CertifyStructWithManySHFlds(StructWithManySHFlds s, int* ptoArrInt32s)
{
    if( (intptr_t)s.hnd1 != ptoArrInt32s[0] )
    {
        printf("\t\tCertifyStructWithManySHFlds: s.hnd1 != ptoArrInt32s[0]\n");
        return FALSE;
    }
    else if( (intptr_t)s.hnd2 != ptoArrInt32s[1] )
    {
        printf("\t\tCertifyStructWithManySHFlds: s.hnd2 != ptoArrInt32s[1]\n");
        return FALSE;
    }
    else if( (intptr_t)s.hnd3 != ptoArrInt32s[2] )
    {
        printf("\t\tCertifyStructWithManySHFlds: s.hnd3 != ptoArrInt32s[2]\n");
        return FALSE;
    }
    else if( (intptr_t)s.hnd4 != ptoArrInt32s[3] )
    {
        printf("\t\tCertifyStructWithManySHFlds: s.hnd4 != ptoArrInt32s[3]\n");
        return FALSE;
    }
    else if( (intptr_t)s.hnd5 != ptoArrInt32s[4] )
    {
        printf("\t\tCertifyStructWithManySHFlds: s.hnd5 != ptoArrInt32s[4]\n");
        return FALSE;
    }
    else if( (intptr_t)s.hnd6 != ptoArrInt32s[5] )
    {
        printf("\t\tCertifyStructWithManySHFlds: s.hnd6 != ptoArrInt32s[5]\n");
        return FALSE;
    }
    else if( (intptr_t)s.hnd7 != ptoArrInt32s[6] )
    {
        printf("\t\tCertifyStructWithManySHFlds: s.hnd7 != ptoArrInt32s[6]\n");
        return FALSE;
    }
    else if( (intptr_t)s.hnd8 != ptoArrInt32s[7] )
    {
        printf("\t\tCertifyStructWithManySHFlds: s.hnd8 != ptoArrInt32s[7]\n");
        return FALSE;
    }
    else if( (intptr_t)s.hnd9 != ptoArrInt32s[8] )
    {
        printf("\t\tCertifyStructWithManySHFlds: s.hnd9 != ptoArrInt32s[8]\n");
        return FALSE;
    }
    else if( (intptr_t)s.hnd10 != ptoArrInt32s[9] )
    {
        printf("\t\tCertifyStructWithManySHFlds: s.hnd10 != ptoArrInt32s[9]\n");
        return FALSE;
    }
    else if( (intptr_t)s.hnd11 != ptoArrInt32s[10] )
    {
        printf("\t\tCertifyStructWithManySHFlds: s.hnd11 != ptoArrInt32s[10]\n");
        return FALSE;
    }
    else if( (intptr_t)s.hnd12 != ptoArrInt32s[11] )
    {
        printf("\t\tCertifyStructWithManySHFlds: s.hnd12 != ptoArrInt32s[11]\n");
        return FALSE;
    }
    else if( (intptr_t)s.hnd13 != ptoArrInt32s[12] )
    {
        printf("\t\tCertifyStructWithManySHFlds: s.hnd13 != ptoArrInt32s[12]\n");
        return FALSE;
    }
    else if( (intptr_t)s.hnd14 != ptoArrInt32s[13] )
    {
        printf("\t\tCertifyStructWithManySHFlds: s.hnd14 != ptoArrInt32s[13]\n");
        return FALSE;
    }
    else if( (intptr_t)s.hnd15 != ptoArrInt32s[14] )
    {
        printf("\t\tCertifyStructWithManySHFlds: s.hnd15 != ptoArrInt32s[14]\n");
        return FALSE;
    }
    else
        return TRUE;
}

//ptoArrInt32 is a pointer to an array of int32s---each element of which matches a fld of the struct
extern "C" DLL_EXPORT BOOL __stdcall SHStructWithManySHFldsParam_In(StructWithManySHFlds s, int* ptoArrInt32s)
{
    if ( !CertifyStructWithManySHFlds(s, ptoArrInt32s) )
        return FALSE;
    s.hnd14	= returnHandleValue; //try to change a fld; should not be reflected on managed side
    return TRUE;
}

//ptoArrInt32 is a pointer to an array of int32s---each element of which matches a fld of the struct
//Ref1 will not change any of the flds
extern "C" DLL_EXPORT BOOL __stdcall SHStructWithManySHFldsParam_Ref1(StructWithManySHFlds* ptos, int* ptoArrInt32s)
{
    return CertifyStructWithManySHFlds(*ptos, ptoArrInt32s);
}

//ptoArrInt32 is a pointer to an array of int32s---each element of which matches a fld of the struct
//Ref2 will change one of the flds
extern "C" DLL_EXPORT BOOL __stdcall SHStructWithManySHFldsParam_Ref2(StructWithManySHFlds* ptos, int* ptoArrInt32s)
{
    if( !CertifyStructWithManySHFlds(*ptos, ptoArrInt32s) )
        return FALSE;
    else
        (*ptos).hnd14 = returnHandleValue; //change one of the flds
    return TRUE;
}

extern "C" DLL_EXPORT int __stdcall SHInvalid_MA()
{
    // NOT REACHABLE from test because we are supposed to throw MarshalDirectionException from stubs
    // This is needed to make ProjectN happy because ProjectN always do early binding
    return 0;
}

extern "C" DLL_EXPORT int __stdcall SHInvalid_retMA()
{
    // NOT REACHABLE from test because we are supposed to throw MarshalDirectionException from stubs
    // This is needed to make ProjectN happy because ProjectN always do early binding
    return 0;
}
