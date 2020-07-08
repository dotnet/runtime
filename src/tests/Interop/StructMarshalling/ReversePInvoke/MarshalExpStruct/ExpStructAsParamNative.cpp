// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "ExpStructAsParamNative.h"
#include <stdio.h>
#include <stdlib.h>
#include <xplatform.h>

const char* NativeStr = "Native";
const size_t size=strlen(NativeStr);

#define PRINT_ERR_INFO() \
    printf("\t%s : unexpected error \n",__FUNCTION__)

//----------method called byref----------//
/////
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructAsParam_AsExpByRefINNER2_Cdecl(INNER2* inner)
{
    if(!IsCorrectINNER2(inner))
    {
        PRINT_ERR_INFO();
        PrintINNER2(inner,"inner");
        return FALSE;
    }
    ChangeINNER2(inner);
    return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructAsParam_AsExpByRefINNER2_Stdcall(INNER2* inner)
{
    if(!IsCorrectINNER2(inner))
    {
        PRINT_ERR_INFO();
        PrintINNER2(inner,"inner");
        return FALSE;
    }
    ChangeINNER2(inner);
    return TRUE;
}

/////
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructAsParam_AsExpByRefInnerExplicit_Cdecl(InnerExplicit* inner)
{
    if(inner->f1 != 1 || memcmp(inner->f3, "some string",11*sizeof(char)) != 0)
    {
        PRINT_ERR_INFO();
        PrintInnerExplicit(inner,"inner");
        return FALSE;
    }
    ChangeInnerExplicit(inner);
    return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructAsParam_AsExpByRefInnerExplicit_Stdcall(InnerExplicit* inner)
{
    if(inner->f1 != 1 || memcmp(inner->f3, "some string",11*sizeof(char)) != 0)
    {
        PRINT_ERR_INFO();
        PrintInnerExplicit(inner,"inner");
        return FALSE;
    }
    ChangeInnerExplicit(inner);
    return TRUE;
}

/////
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructAsParam_AsExpByRefInnerArrayExplicit_Cdecl(InnerArrayExplicit* outer2)
{
    for(int i = 0;i<NumArrElements;i++)
    {
        if(outer2->arr[i].f1 != 1)
        {
            PRINT_ERR_INFO();
            return FALSE;
        }
    }
    if(memcmp(outer2->f4,"some string2",12) != 0)
    {
        PRINT_ERR_INFO();
        return FALSE;
    }
    for(int i =0;i<NumArrElements;i++)
    {
        outer2->arr[i].f1 = 77;
    }
    const char* temp = "change string2";
    size_t len = strlen(temp);
    LPCSTR str = (LPCSTR)CoreClrAlloc( sizeof(char)*(len+1) );
    strcpy_s((char*)str,len+1,temp);
    outer2->f4 = str;
    return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructAsParam_AsExpByRefInnerArrayExplicit_Stdcall(InnerArrayExplicit* outer2)
{
    for(int i = 0;i<NumArrElements;i++)
    {
        if(outer2->arr[i].f1 != 1)
        {
            PRINT_ERR_INFO();
            return FALSE;
        }
    }
    if(memcmp(outer2->f4,"some string2",12) != 0)
    {
        PRINT_ERR_INFO();
        return FALSE;
    }
    for(int i =0;i<NumArrElements;i++)
    {
        outer2->arr[i].f1 = 77;
    }
    const char* temp = "change string2";
    size_t len = strlen(temp);
    LPCSTR str = (LPCSTR)CoreClrAlloc( sizeof(char)*(len+1) );
    strcpy_s((char*)str,len+1,temp);
    outer2->f4 = str;
    return TRUE;
}

/////
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructAsParam_AsExpByRefOUTER3_Cdecl(OUTER3* outer3)
{
    if(!IsCorrectOUTER3(outer3))
    {
        PRINT_ERR_INFO();
        PrintOUTER3(outer3,"OUTER3");
        return FALSE;
    }
    ChangeOUTER3(outer3);
    return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructAsParam_AsExpByRefOUTER3_Stdcall(OUTER3* outer3)
{
    if(!IsCorrectOUTER3(outer3))
    {
        PRINT_ERR_INFO();
        PrintOUTER3(outer3,"OUTER3");
        return FALSE;
    }
    ChangeOUTER3(outer3);
    return TRUE;
}

/////
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructAsParam_AsExpByRefU_Cdecl(U* str1)
{
    if(!IsCorrectU(str1))
    {
        PRINT_ERR_INFO();
        PrintU(str1, "str1");
        return FALSE;
    }
    ChangeU(str1);
    return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructAsParam_AsExpByRefU_Stdcall(U* str1)
{
    if(!IsCorrectU(str1))
    {
        PRINT_ERR_INFO();
        PrintU(str1, "str1");
        return FALSE;
    }
    ChangeU(str1);
    return TRUE;
}

/////
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructAsParam_AsExpByRefByteStructPack2Explicit_Cdecl(ByteStructPack2Explicit* str1)
{
    if(!IsCorrectByteStructPack2Explicit(str1))
    {
        PRINT_ERR_INFO();
        PrintByteStructPack2Explicit(str1, "str1");
        return FALSE;
    }
    ChangeByteStructPack2Explicit(str1);
    return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructAsParam_AsExpByRefByteStructPack2Explicit_Stdcall(ByteStructPack2Explicit* str1)
{
    if(!IsCorrectByteStructPack2Explicit(str1))
    {
        PRINT_ERR_INFO();
        PrintByteStructPack2Explicit(str1, "str1");
        return FALSE;
    }
    ChangeByteStructPack2Explicit(str1);
    return TRUE;
}

/////
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructAsParam_AsExpByRefShortStructPack4Explicit_Cdecl(ShortStructPack4Explicit* str1)
{
    if(!IsCorrectShortStructPack4Explicit(str1))
    {
        PRINT_ERR_INFO();
        PrintShortStructPack4Explicit(str1, "str1");
        return FALSE;
    }
    ChangeShortStructPack4Explicit(str1);
    return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructAsParam_AsExpByRefShortStructPack4Explicit_Stdcall(ShortStructPack4Explicit* str1)
{
    if(!IsCorrectShortStructPack4Explicit(str1))
    {
        PRINT_ERR_INFO();
        PrintShortStructPack4Explicit(str1, "str1");
        return FALSE;
    }
    ChangeShortStructPack4Explicit(str1);
    return TRUE;
}

/////
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructAsParam_AsExpByRefIntStructPack8Explicit_Cdecl(IntStructPack8Explicit* str1)
{
    if(!IsCorrectIntStructPack8Explicit(str1))
    {
        PRINT_ERR_INFO();
        PrintIntStructPack8Explicit(str1, "str1");
        return FALSE;
    }
    ChangeIntStructPack8Explicit(str1);
    return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructAsParam_AsExpByRefIntStructPack8Explicit_Stdcall(IntStructPack8Explicit* str1)
{
    if(!IsCorrectIntStructPack8Explicit(str1))
    {
        PRINT_ERR_INFO();
        PrintIntStructPack8Explicit(str1, "str1");
        return FALSE;
    }
    ChangeIntStructPack8Explicit(str1);
    return TRUE;
}

/////
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructAsParam_AsExpByRefLongStructPack16Explicit_Cdecl(LongStructPack16Explicit* str1)
{
    if(!IsCorrectLongStructPack16Explicit(str1))
    {
        PRINT_ERR_INFO();
        PrintLongStructPack16Explicit(str1, "str1");
        return FALSE;
    }
    ChangeLongStructPack16Explicit(str1);
    return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructAsParam_AsExpByRefLongStructPack16Explicit_Stdcall(LongStructPack16Explicit* str1)
{
    if(!IsCorrectLongStructPack16Explicit(str1))
    {
        PRINT_ERR_INFO();
        PrintLongStructPack16Explicit(str1, "str1");
        return FALSE;
    }
    ChangeLongStructPack16Explicit(str1);
    return TRUE;
}
/////
//---------------------------- ----------//


//----------method called byval----------//
/////
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructAsParam_AsExpByValINNER2_Cdecl(INNER2 str1)
{
    return MarshalStructAsParam_AsExpByRefINNER2_Cdecl(&str1);
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructAsParam_AsExpByValINNER2_Stdcall(INNER2 str1)
{
    return MarshalStructAsParam_AsExpByRefINNER2_Stdcall(&str1);
}
/////

extern "C" DLL_EXPORT BOOL _cdecl MarshalStructAsParam_AsExpByValInnerExplicit_Cdecl(InnerExplicit str1)
{
    return MarshalStructAsParam_AsExpByRefInnerExplicit_Cdecl(&str1);
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructAsParam_AsExpByValInnerExplicit_Stdcall(InnerExplicit str1)
{
    return MarshalStructAsParam_AsExpByRefInnerExplicit_Stdcall(&str1);
}

/////
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructAsParam_AsExpByValInnerArrayExplicit_Cdecl(InnerArrayExplicit str1)
{
    return MarshalStructAsParam_AsExpByRefInnerArrayExplicit_Cdecl(&str1);
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructAsParam_AsExpByValInnerArrayExplicit_Stdcall(InnerArrayExplicit str1)
{
    return MarshalStructAsParam_AsExpByRefInnerArrayExplicit_Stdcall(&str1);
}
/////

extern "C" DLL_EXPORT BOOL _cdecl MarshalStructAsParam_AsExpByValOUTER3_Cdecl(OUTER3 str1)
{
    return MarshalStructAsParam_AsExpByRefOUTER3_Cdecl(&str1);
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructAsParam_AsExpByValOUTER3_Stdcall(OUTER3 str1)
{
    return MarshalStructAsParam_AsExpByRefOUTER3_Stdcall( &str1);
}

/////
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructAsParam_AsExpByValU_Cdecl(U str1)
{
    return MarshalStructAsParam_AsExpByRefU_Cdecl(&str1);
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructAsParam_AsExpByValU_Stdcall(U str1)
{
    return MarshalStructAsParam_AsExpByRefU_Stdcall(&str1);
}
/////

extern "C" DLL_EXPORT BOOL _cdecl MarshalStructAsParam_AsExpByValByteStructPack2Explicit_Cdecl(ByteStructPack2Explicit str1)
{
    return MarshalStructAsParam_AsExpByRefByteStructPack2Explicit_Cdecl(&str1);
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructAsParam_AsExpByValByteStructPack2Explicit_Stdcall(ByteStructPack2Explicit str1)
{
    return MarshalStructAsParam_AsExpByRefByteStructPack2Explicit_Stdcall(&str1);
}

/////
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructAsParam_AsExpByValShortStructPack4Explicit_Cdecl(ShortStructPack4Explicit str1)
{
    return MarshalStructAsParam_AsExpByRefShortStructPack4Explicit_Cdecl(&str1);
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructAsParam_AsExpByValShortStructPack4Explicit_Stdcall(ShortStructPack4Explicit str1)
{
    return MarshalStructAsParam_AsExpByRefShortStructPack4Explicit_Stdcall(&str1);
}
/////

extern "C" DLL_EXPORT BOOL _cdecl MarshalStructAsParam_AsExpByValIntStructPack8Explicit_Cdecl(IntStructPack8Explicit str1)
{
    return MarshalStructAsParam_AsExpByRefIntStructPack8Explicit_Cdecl(&str1);
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructAsParam_AsExpByValIntStructPack8Explicit_Stdcall(IntStructPack8Explicit str1)
{
    return MarshalStructAsParam_AsExpByRefIntStructPack8Explicit_Stdcall(&str1);
}

/////
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructAsParam_AsExpByValLongStructPack16Explicit_Cdecl(LongStructPack16Explicit str1)
{
    return MarshalStructAsParam_AsExpByRefLongStructPack16Explicit_Cdecl(&str1);
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructAsParam_AsExpByValLongStructPack16Explicit_Stdcall(LongStructPack16Explicit str1)
{
    return MarshalStructAsParam_AsExpByRefLongStructPack16Explicit_Stdcall(&str1);
}
/////
//---------------------------- ----------//


//----------Delegate Pinvoke. PassByRef----------//
/////
typedef BOOL(_cdecl *DelegatePinvokeByRefCdeclCaller_INNER2)(INNER2* inner);
extern "C" DLL_EXPORT DelegatePinvokeByRefCdeclCaller_INNER2 _cdecl Get_MarshalStructAsParam_AsExpByRefINNER2_Cdecl_FuncPtr()
{
    return MarshalStructAsParam_AsExpByRefINNER2_Cdecl;
}

typedef BOOL(__stdcall *DelegatePinvokeByRefStdcallCaller_INNER2)(INNER2* inner);
extern "C" DLL_EXPORT DelegatePinvokeByRefStdcallCaller_INNER2 __stdcall Get_MarshalStructAsParam_AsExpByRefINNER2_Stdcall_FuncPtr()
{
    return MarshalStructAsParam_AsExpByRefINNER2_Stdcall;
}
/////

typedef BOOL(_cdecl *DelegatePinvokeByRefCdeclCaller_InnerExplicit)(InnerExplicit* ie);
extern "C" DLL_EXPORT DelegatePinvokeByRefCdeclCaller_InnerExplicit _cdecl Get_MarshalStructAsParam_AsExpByRefInnerExplicit_Cdecl_FuncPtr()
{
    return MarshalStructAsParam_AsExpByRefInnerExplicit_Cdecl;
}

typedef BOOL(__stdcall *DelegatePinvokeByRefStdcallCaller_InnerExplicit)(InnerExplicit* ie);
extern "C" DLL_EXPORT DelegatePinvokeByRefStdcallCaller_InnerExplicit __stdcall Get_MarshalStructAsParam_AsExpByRefInnerExplicit_Stdcall_FuncPtr()
{
    return MarshalStructAsParam_AsExpByRefInnerExplicit_Stdcall;
}

/////
typedef BOOL(_cdecl *DelegatePinvokeByRefCdeclCaller_InnerArrayExplicit)(InnerArrayExplicit* iae);
extern "C" DLL_EXPORT DelegatePinvokeByRefCdeclCaller_InnerArrayExplicit _cdecl Get_MarshalStructAsParam_AsExpByRefInnerArrayExplicit_Cdecl_FuncPtr()
{
    return MarshalStructAsParam_AsExpByRefInnerArrayExplicit_Cdecl;
}

typedef BOOL(__stdcall *DelegatePinvokeByRefStdcallCaller_InnerArrayExplicit)(InnerArrayExplicit* iae);
extern "C" DLL_EXPORT DelegatePinvokeByRefStdcallCaller_InnerArrayExplicit __stdcall Get_MarshalStructAsParam_AsExpByRefInnerArrayExplicit_Stdcall_FuncPtr()
{
    return MarshalStructAsParam_AsExpByRefInnerArrayExplicit_Stdcall;
}
/////

typedef BOOL(_cdecl *DelegatePinvokeByRefCdeclCaller_OUTER3)(OUTER3* outer);
extern "C" DLL_EXPORT DelegatePinvokeByRefCdeclCaller_OUTER3 _cdecl Get_MarshalStructAsParam_AsExpByRefOUTER3_Cdecl_FuncPtr()
{
    return MarshalStructAsParam_AsExpByRefOUTER3_Cdecl;
}

typedef BOOL(__stdcall *DelegatePinvokeByRefStdcallCaller_OUTER3)(OUTER3* outer);
extern "C" DLL_EXPORT DelegatePinvokeByRefStdcallCaller_OUTER3 __stdcall Get_MarshalStructAsParam_AsExpByRefOUTER3_Stdcall_FuncPtr()
{
    return MarshalStructAsParam_AsExpByRefOUTER3_Stdcall;
}

/////
typedef BOOL(_cdecl *DelegatePinvokeByRefCdeclCaller_U)(U* inner);
extern "C" DLL_EXPORT DelegatePinvokeByRefCdeclCaller_U _cdecl Get_MarshalStructAsParam_AsExpByRefU_Cdecl_FuncPtr()
{
    return MarshalStructAsParam_AsExpByRefU_Cdecl;
}

typedef BOOL(__stdcall *DelegatePinvokeByRefStdcallCaller_U)(U* inner);
extern "C" DLL_EXPORT DelegatePinvokeByRefStdcallCaller_U __stdcall Get_MarshalStructAsParam_AsExpByRefU_Stdcall_FuncPtr()
{
    return MarshalStructAsParam_AsExpByRefU_Stdcall;
}
/////

typedef BOOL(_cdecl *DelegatePinvokeByRefCdeclCaller_ByteStructPack2Explicit)(ByteStructPack2Explicit* bspe);
extern "C" DLL_EXPORT DelegatePinvokeByRefCdeclCaller_ByteStructPack2Explicit _cdecl Get_MarshalStructAsParam_AsExpByRefByteStructPack2Explicit_Cdecl_FuncPtr()
{
    return MarshalStructAsParam_AsExpByRefByteStructPack2Explicit_Cdecl;
}

typedef BOOL(__stdcall *DelegatePinvokeByRefStdcallCaller_ByteStructPack2Explicit)(ByteStructPack2Explicit* bspe);
extern "C" DLL_EXPORT DelegatePinvokeByRefStdcallCaller_ByteStructPack2Explicit __stdcall Get_MarshalStructAsParam_AsExpByRefByteStructPack2Explicit_Stdcall_FuncPtr()
{
    return MarshalStructAsParam_AsExpByRefByteStructPack2Explicit_Stdcall;
}

/////
typedef BOOL(_cdecl *DelegatePinvokeByRefCdeclCaller_ShortStructPack4Explicit)(ShortStructPack4Explicit* sspe);
extern "C" DLL_EXPORT DelegatePinvokeByRefCdeclCaller_ShortStructPack4Explicit _cdecl Get_MarshalStructAsParam_AsExpByRefShortStructPack4Explicit_Cdecl_FuncPtr()
{
    return MarshalStructAsParam_AsExpByRefShortStructPack4Explicit_Cdecl;
}

typedef BOOL(__stdcall *DelegatePinvokeByRefStdcallCaller_ShortStructPack4Explicit)(ShortStructPack4Explicit* sspe);
extern "C" DLL_EXPORT DelegatePinvokeByRefStdcallCaller_ShortStructPack4Explicit __stdcall Get_MarshalStructAsParam_AsExpByRefShortStructPack4Explicit_Stdcall_FuncPtr()
{
    return MarshalStructAsParam_AsExpByRefShortStructPack4Explicit_Stdcall;
}
/////

typedef BOOL(_cdecl *DelegatePinvokeByRefCdeclCaller_IntStructPack8Explicit)(IntStructPack8Explicit* ispe);
extern "C" DLL_EXPORT DelegatePinvokeByRefCdeclCaller_IntStructPack8Explicit _cdecl Get_MarshalStructAsParam_AsExpByRefIntStructPack8Explicit_Cdecl_FuncPtr()
{
    return MarshalStructAsParam_AsExpByRefIntStructPack8Explicit_Cdecl;
}

typedef BOOL(__stdcall *DelegatePinvokeByRefStdcallCaller_IntStructPack8Explicit)(IntStructPack8Explicit* ispe);
extern "C" DLL_EXPORT DelegatePinvokeByRefStdcallCaller_IntStructPack8Explicit __stdcall Get_MarshalStructAsParam_AsExpByRefIntStructPack8Explicit_Stdcall_FuncPtr()
{
    return MarshalStructAsParam_AsExpByRefIntStructPack8Explicit_Stdcall;
}

/////
typedef BOOL(_cdecl *DelegatePinvokeByRefCdeclCaller_LongStructPack16Explicit)(LongStructPack16Explicit* ispe);
extern "C" DLL_EXPORT DelegatePinvokeByRefCdeclCaller_LongStructPack16Explicit _cdecl Get_MarshalStructAsParam_AsExpByRefLongStructPack16Explicit_Cdecl_FuncPtr()
{
    return MarshalStructAsParam_AsExpByRefLongStructPack16Explicit_Cdecl;
}

typedef BOOL(__stdcall *DelegatePinvokeByRefStdcallCaller_LongStructPack16Explicit)(LongStructPack16Explicit* ispe);
extern "C" DLL_EXPORT DelegatePinvokeByRefStdcallCaller_LongStructPack16Explicit __stdcall Get_MarshalStructAsParam_AsExpByRefLongStructPack16Explicit_Stdcall_FuncPtr()
{
    return MarshalStructAsParam_AsExpByRefLongStructPack16Explicit_Stdcall;
}
/////
//---------------------------- ----------//


//----------Delegate Pinvoke. PassByVal----------//
/////
typedef BOOL(_cdecl *DelegatePinvokeByValCdeclCaller_INNER2)(INNER2 inner);
extern "C" DLL_EXPORT DelegatePinvokeByValCdeclCaller_INNER2 _cdecl Get_MarshalStructAsParam_AsExpByValINNER2_Cdecl_FuncPtr()
{
    return MarshalStructAsParam_AsExpByValINNER2_Cdecl;
}

typedef BOOL(__stdcall *DelegatePinvokeByValStdcallCaller_INNER2)(INNER2 inner);
extern "C" DLL_EXPORT DelegatePinvokeByValStdcallCaller_INNER2 __stdcall Get_MarshalStructAsParam_AsExpByValINNER2_Stdcall_FuncPtr()
{
    return MarshalStructAsParam_AsExpByValINNER2_Stdcall;
}
/////

typedef BOOL(_cdecl *DelegatePinvokeByValCdeclCaller_InnerExplicit)(InnerExplicit ie);
extern "C" DLL_EXPORT DelegatePinvokeByValCdeclCaller_InnerExplicit _cdecl Get_MarshalStructAsParam_AsExpByValInnerExplicit_Cdecl_FuncPtr()
{
    return MarshalStructAsParam_AsExpByValInnerExplicit_Cdecl;
}

typedef BOOL(__stdcall *DelegatePinvokeByValStdcallCaller_InnerExplicit)(InnerExplicit ie);
extern "C" DLL_EXPORT DelegatePinvokeByValStdcallCaller_InnerExplicit __stdcall Get_MarshalStructAsParam_AsExpByValInnerExplicit_Stdcall_FuncPtr()
{
    return MarshalStructAsParam_AsExpByValInnerExplicit_Stdcall;
}

/////
typedef BOOL(_cdecl *DelegatePinvokeByValCdeclCaller_InnerArrayExplicit)(InnerArrayExplicit iae);
extern "C" DLL_EXPORT DelegatePinvokeByValCdeclCaller_InnerArrayExplicit _cdecl Get_MarshalStructAsParam_AsExpByValInnerArrayExplicit_Cdecl_FuncPtr()
{
    return MarshalStructAsParam_AsExpByValInnerArrayExplicit_Cdecl;
}

typedef BOOL(__stdcall *DelegatePinvokeByValStdcallCaller_InnerArrayExplicit)(InnerArrayExplicit iae);
extern "C" DLL_EXPORT DelegatePinvokeByValStdcallCaller_InnerArrayExplicit __stdcall Get_MarshalStructAsParam_AsExpByValInnerArrayExplicit_Stdcall_FuncPtr()
{
    return MarshalStructAsParam_AsExpByValInnerArrayExplicit_Stdcall;
}
/////

typedef BOOL(_cdecl *DelegatePinvokeByValCdeclCaller_OUTER3)(OUTER3 outer);
extern "C" DLL_EXPORT DelegatePinvokeByValCdeclCaller_OUTER3 _cdecl Get_MarshalStructAsParam_AsExpByValOUTER3_Cdecl_FuncPtr()
{
    return MarshalStructAsParam_AsExpByValOUTER3_Cdecl;
}

typedef BOOL(__stdcall *DelegatePinvokeByValStdcallCaller_OUTER3)(OUTER3 outer);
extern "C" DLL_EXPORT DelegatePinvokeByValStdcallCaller_OUTER3 __stdcall Get_MarshalStructAsParam_AsExpByValOUTER3_Stdcall_FuncPtr()
{
    return MarshalStructAsParam_AsExpByValOUTER3_Stdcall;
}

/////
typedef BOOL(_cdecl *DelegatePinvokeByValCdeclCaller_U)(U inner);
extern "C" DLL_EXPORT DelegatePinvokeByValCdeclCaller_U _cdecl Get_MarshalStructAsParam_AsExpByValU_Cdecl_FuncPtr()
{
    return MarshalStructAsParam_AsExpByValU_Cdecl;
}

typedef BOOL(__stdcall *DelegatePinvokeByValStdcallCaller_U)(U inner);
extern "C" DLL_EXPORT DelegatePinvokeByValStdcallCaller_U __stdcall Get_MarshalStructAsParam_AsExpByValU_Stdcall_FuncPtr()
{
    return MarshalStructAsParam_AsExpByValU_Stdcall;
}
/////

typedef BOOL(_cdecl *DelegatePinvokeByValCdeclCaller_ByteStructPack2Explicit)(ByteStructPack2Explicit bspe);
extern "C" DLL_EXPORT DelegatePinvokeByValCdeclCaller_ByteStructPack2Explicit _cdecl Get_MarshalStructAsParam_AsExpByValByteStructPack2Explicit_Cdecl_FuncPtr()
{
    return MarshalStructAsParam_AsExpByValByteStructPack2Explicit_Cdecl;
}

typedef BOOL(__stdcall *DelegatePinvokeByValStdcallCaller_ByteStructPack2Explicit)(ByteStructPack2Explicit bspe);
extern "C" DLL_EXPORT DelegatePinvokeByValStdcallCaller_ByteStructPack2Explicit __stdcall Get_MarshalStructAsParam_AsExpByValByteStructPack2Explicit_Stdcall_FuncPtr()
{
    return MarshalStructAsParam_AsExpByValByteStructPack2Explicit_Stdcall;
}

/////
typedef BOOL(_cdecl *DelegatePinvokeByValCdeclCaller_ShortStructPack4Explicit)(ShortStructPack4Explicit sspe);
extern "C" DLL_EXPORT DelegatePinvokeByValCdeclCaller_ShortStructPack4Explicit _cdecl Get_MarshalStructAsParam_AsExpByValShortStructPack4Explicit_Cdecl_FuncPtr()
{
    return MarshalStructAsParam_AsExpByValShortStructPack4Explicit_Cdecl;
}

typedef BOOL(__stdcall *DelegatePinvokeByValStdcallCaller_ShortStructPack4Explicit)(ShortStructPack4Explicit sspe);
extern "C" DLL_EXPORT DelegatePinvokeByValStdcallCaller_ShortStructPack4Explicit __stdcall Get_MarshalStructAsParam_AsExpByValShortStructPack4Explicit_Stdcall_FuncPtr()
{
    return MarshalStructAsParam_AsExpByValShortStructPack4Explicit_Stdcall;
}
/////

typedef BOOL(_cdecl *DelegatePinvokeByValCdeclCaller_IntStructPack8Explicit)(IntStructPack8Explicit ispe);
extern "C" DLL_EXPORT DelegatePinvokeByValCdeclCaller_IntStructPack8Explicit _cdecl Get_MarshalStructAsParam_AsExpByValIntStructPack8Explicit_Cdecl_FuncPtr()
{
    return MarshalStructAsParam_AsExpByValIntStructPack8Explicit_Cdecl;
}

typedef BOOL(__stdcall *DelegatePinvokeByValStdcallCaller_IntStructPack8Explicit)(IntStructPack8Explicit ispe);
extern "C" DLL_EXPORT DelegatePinvokeByValStdcallCaller_IntStructPack8Explicit __stdcall Get_MarshalStructAsParam_AsExpByValIntStructPack8Explicit_Stdcall_FuncPtr()
{
    return MarshalStructAsParam_AsExpByValIntStructPack8Explicit_Stdcall;
}

/////
typedef BOOL(_cdecl *DelegatePinvokeByValCdeclCaller_LongStructPack16Explicit)(LongStructPack16Explicit ispe);
extern "C" DLL_EXPORT DelegatePinvokeByValCdeclCaller_LongStructPack16Explicit _cdecl Get_MarshalStructAsParam_AsExpByValLongStructPack16Explicit_Cdecl_FuncPtr()
{
    return MarshalStructAsParam_AsExpByValLongStructPack16Explicit_Cdecl;
}

typedef BOOL(__stdcall *DelegatePinvokeByValStdcallCaller_LongStructPack16Explicit)(LongStructPack16Explicit ispe);
extern "C" DLL_EXPORT DelegatePinvokeByValStdcallCaller_LongStructPack16Explicit __stdcall Get_MarshalStructAsParam_AsExpByValLongStructPack16Explicit_Stdcall_FuncPtr()
{
    return MarshalStructAsParam_AsExpByValLongStructPack16Explicit_Stdcall;
}
/////
//---------------------------- ----------//


//----------Reverse Pinvoke. PassByRef----------//
/////
typedef BOOL (_cdecl *ByRefCdeclCaller_INNER2)(INNER2* inner2);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalByRefStruct_Cdecl_INNER2(ByRefCdeclCaller_INNER2 caller)
{
    //init
    INNER2 inner2;
    inner2.f1 = 77;
    inner2.f2 = 77.0;

    char* pstr = GetNativeString();
    inner2.f3 = pstr;

    if(!caller(&inner2))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if(!IsCorrectINNER2(&inner2))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    //TP_CoreClrFree((void*)inner2.f3);
    return TRUE;
}

typedef BOOL (__stdcall *ByRefStdcallCaller_INNER2)(INNER2* inner2);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalByRefStruct_Stdcall_INNER2(ByRefStdcallCaller_INNER2 caller)
{
    const char* lNativeStr = "Native";
    const size_t lsize = strlen(lNativeStr);

    //init
    INNER2 inner2;
    inner2.f1 = 77;
    inner2.f2 = 77.0;

    char* pstr = (char*)CoreClrAlloc(lsize + 1);
    memset(pstr,0,lsize+1);
    strncpy_s(pstr,lsize+1,lNativeStr,lsize);

    inner2.f3 = pstr;

    if(!caller(&inner2))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if(!IsCorrectINNER2(&inner2))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    //TP_CoreClrFree((void*)inner2.f3);
    return TRUE;
}
/////

typedef BOOL (_cdecl *ByRefCdeclCaller_InnerExplicit)(InnerExplicit* inner2);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalByRefStruct_Cdecl_InnerExplicit(ByRefCdeclCaller_InnerExplicit caller)
{

    const char* lNativeStr = "Native";
    const size_t lsize = strlen(lNativeStr);

    //init
    InnerExplicit ie;
    ie.f1 = 77;

    char* pstr = (char*)CoreClrAlloc(lsize + 1);
    memset(pstr,0,lsize+1);
    strncpy_s(pstr,lsize+1,lNativeStr,lsize);
    ie.f3 = pstr;

    if(!caller(&ie))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if( ie.f1 != 1 || 0 != strcmp((char*)ie.f3, "some string") )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}

typedef BOOL (__stdcall *ByRefStdcallCaller_InnerExplicit)(InnerExplicit* inner2);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalByRefStruct_Stdcall_InnerExplicit(ByRefStdcallCaller_InnerExplicit caller)
{
    const char* lNativeStr = "Native";
    const size_t lsize = strlen(lNativeStr);

    //init
    InnerExplicit ie;
    ie.f1 = 77;

    char* pstr = (char*)CoreClrAlloc(lsize + 1);
    memset(pstr,0,lsize+1);
    strncpy_s(pstr,lsize+1,lNativeStr,lsize);
    ie.f3 = pstr;

    if(!caller(&ie))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if( ie.f1 != 1 || 0 != strcmp((char*)ie.f3, "some string") )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}

/////
typedef BOOL (_cdecl *ByRefCdeclCaller_InnerArrayExplicit)(InnerArrayExplicit* iae);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalByRefStruct_Cdecl_InnerArrayExplicit(ByRefCdeclCaller_InnerArrayExplicit caller)
{
    const char* lNativeStr = "Native";
    const size_t lsize = strlen(lNativeStr);

    //init
    InnerArrayExplicit iae;
    LPSTR str = NULL;
    for( size_t i = 0; i < NumArrElements; i++ )
    {
        iae.arr[i].f1 = 77;
        str = (LPSTR)CoreClrAlloc( lsize+1 );
        memset(str,0,lsize+1);
        strncpy_s((char*)str,lsize+1,lNativeStr,lsize);

        iae.arr[i].f3 = str;
        str = NULL;
    }

    str = (LPSTR)CoreClrAlloc( lsize+1 );
    memset(str,0,lsize+1);
    strncpy_s((char*)str,lsize+1,lNativeStr,lsize);
    iae.f4 = str;

    if(!caller(&iae))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    for( size_t i = 0; i < NumArrElements; i++ )
    {
        if( iae.arr[i].f1 != 1 || 0 != strcmp((char*)iae.arr[i].f3, "some string"))
        {
            PRINT_ERR_INFO();
            return FALSE;
        }
    }
    if( 0 != strcmp((char*)iae.f4, "some string") )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}

typedef BOOL (__stdcall *ByRefStdcallCaller_InnerArrayExplicit)(InnerArrayExplicit* iae);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalByRefStruct_Stdcall_InnerArrayExplicit(ByRefStdcallCaller_InnerArrayExplicit caller)
{
    //init
    InnerArrayExplicit iae;
    LPSTR str = NULL;
    for( size_t i = 0; i < NumArrElements; i++ )
    {
        iae.arr[i].f1 = 77;
        str = GetNativeString();
        iae.arr[i].f3 = str;
        str = NULL;
    }

    str = GetNativeString();
    iae.f4 = str;

    if(!caller(&iae))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    for( size_t i = 0; i < NumArrElements; i++ )
    {
        if( iae.arr[i].f1 != 1 || 0 != strcmp((char*)iae.arr[i].f3, "some string"))
        {
            PRINT_ERR_INFO();
            return FALSE;
        }
    }
    if( 0 != strcmp((char*)iae.f4, "some string") )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}
/////

typedef BOOL (_cdecl *ByRefCdeclCaller_OUTER3)(OUTER3* outer3);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalByRefStruct_Cdecl_OUTER3(ByRefCdeclCaller_OUTER3 caller)
{
    //init
    OUTER3 outer3;
    LPSTR str = NULL;
    for( size_t i = 0; i < NumArrElements; i++ )
    {
        outer3.arr[i].f1 = 77;
        outer3.arr[i].f2 = 77.0;
        str = GetNativeString();
        outer3.arr[i].f3 = (LPCSTR)str;
        str = NULL;
    }

    str = GetNativeString();
    outer3.f4 = (LPCSTR)str;

    if(!caller(&outer3))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if( !IsCorrectOUTER3( &outer3 ) )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}

typedef BOOL (__stdcall *ByRefStdcallCaller_OUTER3)(OUTER3* outer3);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalByRefStruct_Stdcall_OUTER3(ByRefStdcallCaller_OUTER3 caller)
{

    //init
    OUTER3 outer3;
    LPSTR str = NULL;
    for( size_t i = 0; i < NumArrElements; i++ )
    {
        outer3.arr[i].f1 = 77;
        outer3.arr[i].f2 = 77.0;
        str = GetNativeString();
        outer3.arr[i].f3 = (LPCSTR)str;
        str = NULL;
    }

    str = GetNativeString();
    outer3.f4 = (LPCSTR)str;

    if(!caller(&outer3))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if( !IsCorrectOUTER3( &outer3 ) )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}

/////
typedef BOOL (_cdecl *ByRefCdeclCaller_U)(U* u);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalByRefStruct_Cdecl_U(ByRefCdeclCaller_U caller)
{
    U u;
    u.d = 1.23;

    if(!caller(&u))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if( !IsCorrectU( &u ) )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}

typedef BOOL (__stdcall *ByRefStdcallCaller_U)(U* u);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalByRefStruct_Stdcall_U(ByRefStdcallCaller_U caller)
{
    U u;
    u.d = 1.23;

    if(!caller(&u))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if( !IsCorrectU( &u ) )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}
/////

typedef BOOL (_cdecl *ByRefCdeclCaller_ByteStructPack2Explicit)(ByteStructPack2Explicit* bspe);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalByRefStruct_Cdecl_ByteStructPack2Explicit(ByRefCdeclCaller_ByteStructPack2Explicit caller)
{
    ByteStructPack2Explicit bspe;
    bspe.b1 = 64;
    bspe.b2 = 64;

    if(!caller(&bspe))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if( !IsCorrectByteStructPack2Explicit( &bspe ) )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}

typedef BOOL (__stdcall *ByRefStdcallCaller_ByteStructPack2Explicit)(ByteStructPack2Explicit* bspe);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalByRefStruct_Stdcall_ByteStructPack2Explicit(ByRefStdcallCaller_ByteStructPack2Explicit caller)
{
    ByteStructPack2Explicit bspe;
    bspe.b1 = 64;
    bspe.b2 = 64;

    if(!caller(&bspe))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if( !IsCorrectByteStructPack2Explicit( &bspe ) )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}

/////
typedef BOOL (_cdecl *ByRefCdeclCaller_ShortStructPack4Explicit)(ShortStructPack4Explicit* sspe);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalByRefStruct_Cdecl_ShortStructPack4Explicit(ByRefCdeclCaller_ShortStructPack4Explicit caller) 
{
    ShortStructPack4Explicit sspe;
    sspe.s1 = 64;
    sspe.s2 = 64;

    if(!caller(&sspe))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if( !IsCorrectShortStructPack4Explicit( &sspe ) )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}

typedef BOOL (__stdcall *ByRefStdcallCaller_ShortStructPack4Explicit)(ShortStructPack4Explicit* sspe);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalByRefStruct_Stdcall_ShortStructPack4Explicit(ByRefStdcallCaller_ShortStructPack4Explicit caller) 
{
    ShortStructPack4Explicit sspe;
    sspe.s1 = 64;
    sspe.s2 = 64;

    if(!caller(&sspe))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if( !IsCorrectShortStructPack4Explicit( &sspe ) )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}
/////

typedef BOOL (_cdecl *ByRefCdeclCaller_IntStructPack8Explicit)(IntStructPack8Explicit* ispe);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalByRefStruct_Cdecl_IntStructPack8Explicit(ByRefCdeclCaller_IntStructPack8Explicit caller) 
{
    IntStructPack8Explicit ispe;
    ispe.i1 = 64;
    ispe.i2 = 64;

    if(!caller(&ispe))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if( !IsCorrectIntStructPack8Explicit( &ispe ) )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}

typedef BOOL (__stdcall *ByRefStdcallCaller_IntStructPack8Explicit)(IntStructPack8Explicit* ispe);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalByRefStruct_Stdcall_IntStructPack8Explicit(ByRefStdcallCaller_IntStructPack8Explicit caller) 
{
    IntStructPack8Explicit ispe;
    ispe.i1 = 64;
    ispe.i2 = 64;

    if(!caller(&ispe))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if( !IsCorrectIntStructPack8Explicit( &ispe ) )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}

/////
typedef BOOL (_cdecl *ByRefCdeclCaller_LongStructPack16Explicit)(LongStructPack16Explicit* lspe);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalByRefStruct_Cdecl_LongStructPack16Explicit(ByRefCdeclCaller_LongStructPack16Explicit caller) 
{
    LongStructPack16Explicit lspe;
    lspe.l1 = 64;
    lspe.l2 = 64;

    if(!caller(&lspe))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if( !IsCorrectLongStructPack16Explicit( &lspe ) )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}

typedef BOOL (__stdcall *ByRefStdcallCaller_LongStructPack16Explicit)(LongStructPack16Explicit* lspe);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalByRefStruct_Stdcall_LongStructPack16Explicit(ByRefStdcallCaller_LongStructPack16Explicit caller) 
{
    LongStructPack16Explicit lspe;
    lspe.l1 = 64;
    lspe.l2 = 64;

    if(!caller(&lspe))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if( !IsCorrectLongStructPack16Explicit( &lspe ) )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}
/////
//---------------------------- ----------//


//----------Reverse Pinvoke. PassByVal---------//
/////
typedef BOOL (_cdecl *ByValCdeclCaller_INNER2)(INNER2 inner2);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalByValStruct_Cdecl_INNER2(ByValCdeclCaller_INNER2 caller)
{
    //init
    INNER2 inner2;
    inner2.f1 = 1;
    inner2.f2 = 1.0;

    char* pstr = GetSomeString();
    inner2.f3 = pstr;

    if(!caller(inner2))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if(!IsCorrectINNER2(&inner2))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}

typedef BOOL (__stdcall *ByValStdcallCaller_INNER2)(INNER2 inner2);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalByValStruct_Stdcall_INNER2(ByValStdcallCaller_INNER2 caller)
{
    //init
    INNER2 inner2;
    inner2.f1 = 1;
    inner2.f2 = 1.0;

    char* pstr = GetSomeString();
    inner2.f3 = pstr;

    if(!caller(inner2))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if(!IsCorrectINNER2(&inner2))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}
/////

typedef BOOL (_cdecl *ByValCdeclCaller_InnerExplicit)(InnerExplicit inner2);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalByValStruct_Cdecl_InnerExplicit(ByValCdeclCaller_InnerExplicit caller)
{
    //init
    InnerExplicit ie;
    ie.f1 = 1;

    char* pstr = GetNativeString();
    ie.f3 = pstr;

    if(!caller(ie))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if( ie.f1 != 1 || 0 != strcmp((char*)ie.f3, (char*)NativeStr) )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}

typedef BOOL (__stdcall *ByValStdcallCaller_InnerExplicit)(InnerExplicit inner2);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalByValStruct_Stdcall_InnerExplicit(ByValStdcallCaller_InnerExplicit caller)
{
    //init
    InnerExplicit ie;
    ie.f1 = 1;

    char* pstr = GetNativeString();
    ie.f3 = pstr;

    if(!caller(ie))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if( ie.f1 != 1 || 0 != strcmp((char*)ie.f3, (char*)NativeStr) )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}

/////
typedef BOOL (_cdecl *ByValCdeclCaller_InnerArrayExplicit)(InnerArrayExplicit iae);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalByValStruct_Cdecl_InnerArrayExplicit(ByValCdeclCaller_InnerArrayExplicit caller)
{
    //init
    InnerArrayExplicit iae;
    LPSTR str = NULL;
    for( size_t i = 0; i < NumArrElements; i++ )
    {
        iae.arr[i].f1 = 1;
        str = GetSomeString();
        iae.arr[i].f3 = str;
        str = NULL;
    }

    str = GetSomeString();
    iae.f4 = str;

    if(!caller(iae))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    for( size_t i = 0; i < NumArrElements; i++ )
    {
        if( iae.arr[i].f1 != 1 || 0 != strcmp((char*)iae.arr[i].f3, "some string"))
        {
            PRINT_ERR_INFO();
            return FALSE;
        }
    }
    if( 0 != strcmp((char*)iae.f4, "some string") )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}

typedef BOOL (__stdcall *ByValStdcallCaller_InnerArrayExplicit)(InnerArrayExplicit iae);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalByValStruct_Stdcall_InnerArrayExplicit(ByValStdcallCaller_InnerArrayExplicit caller)
{
    //init
    InnerArrayExplicit iae;
    LPSTR str = NULL;

    for( size_t i = 0; i < NumArrElements; i++ )
    {
        iae.arr[i].f1 = 1;

        str = GetSomeString();
        iae.arr[i].f3 = str;
        str = NULL;
    }

    str = GetSomeString();
    iae.f4 = str;

    if(!caller(iae))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    for( size_t i = 0; i < NumArrElements; i++ )
    {
        if( iae.arr[i].f1 != 1 || 0 != strcmp((char*)iae.arr[i].f3, "some string"))
        {
            PRINT_ERR_INFO();
            return FALSE;
        }
    }
    if( 0 != strcmp((char*)iae.f4, "some string") )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}
/////

typedef BOOL (_cdecl *ByValCdeclCaller_OUTER3)(OUTER3 outer3);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalByValStruct_Cdecl_OUTER3(ByValCdeclCaller_OUTER3 caller)
{
    //init
    OUTER3 outer3;
    LPSTR str = NULL;

    for( size_t i = 0; i < NumArrElements; i++ )
    {
        outer3.arr[i].f1 = 1;
        outer3.arr[i].f2 = 1.0;
        str = GetSomeString();
        outer3.arr[i].f3 = (LPCSTR)str;
        str = NULL;
    }

    str = GetSomeString();
    outer3.f4 = (LPCSTR)str;

    if(!caller(outer3))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if( !IsCorrectOUTER3( &outer3 ) )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}

typedef BOOL (__stdcall *ByValStdcallCaller_OUTER3)(OUTER3 outer3);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalByValStruct_Stdcall_OUTER3(ByValStdcallCaller_OUTER3 caller)
{
    //init
    OUTER3 outer3;
    LPSTR str = NULL;
    for( size_t i = 0; i < NumArrElements; i++ )
    {
        outer3.arr[i].f1 = 1;
        outer3.arr[i].f2 = 1.0;
        str = GetSomeString();
        outer3.arr[i].f3 = (LPCSTR)str;
        str = NULL;
    }

    str = GetSomeString();
    outer3.f4 = (LPCSTR)str;

    if(!caller(outer3))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if( !IsCorrectOUTER3( &outer3 ) )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}

/////
typedef BOOL (_cdecl *ByValCdeclCaller_U)(U u);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalByValStruct_Cdecl_U(ByValCdeclCaller_U caller)
{
    U u;
    u.d = 3.2;

    if(!caller(u))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if( !IsCorrectU( &u ) )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}

typedef BOOL (__stdcall *ByValStdcallCaller_U)(U u);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalByValStruct_Stdcall_U(ByValStdcallCaller_U caller)
{
    U u;
    u.d = 3.2;

    if(!caller(u))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if( !IsCorrectU( &u ) )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}
/////

typedef BOOL (_cdecl *ByValCdeclCaller_ByteStructPack2Explicit)(ByteStructPack2Explicit bspe);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalByValStruct_Cdecl_ByteStructPack2Explicit(ByValCdeclCaller_ByteStructPack2Explicit caller)
{
    ByteStructPack2Explicit bspe;
    bspe.b1 = 32;
    bspe.b2 = 32;

    if(!caller(bspe))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if( !IsCorrectByteStructPack2Explicit( &bspe ) )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}

typedef BOOL (__stdcall *ByValStdcallCaller_ByteStructPack2Explicit)(ByteStructPack2Explicit bspe);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalByValStruct_Stdcall_ByteStructPack2Explicit(ByValStdcallCaller_ByteStructPack2Explicit caller)
{
    ByteStructPack2Explicit bspe;
    bspe.b1 = 32;
    bspe.b2 = 32;

    if(!caller(bspe))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if( !IsCorrectByteStructPack2Explicit( &bspe ) )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}

/////
typedef BOOL (_cdecl *ByValCdeclCaller_ShortStructPack4Explicit)(ShortStructPack4Explicit sspe);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalByValStruct_Cdecl_ShortStructPack4Explicit(ByValCdeclCaller_ShortStructPack4Explicit caller) 
{
    ShortStructPack4Explicit sspe;
    sspe.s1 = 32;
    sspe.s2 = 32;

    if(!caller(sspe))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if( !IsCorrectShortStructPack4Explicit( &sspe ) )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}

typedef BOOL (__stdcall *ByValStdcallCaller_ShortStructPack4Explicit)(ShortStructPack4Explicit sspe);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalByValStruct_Stdcall_ShortStructPack4Explicit(ByValStdcallCaller_ShortStructPack4Explicit caller) 
{
    ShortStructPack4Explicit sspe;
    sspe.s1 = 32;
    sspe.s2 = 32;

    if(!caller(sspe))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if( !IsCorrectShortStructPack4Explicit( &sspe ) )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}
/////

typedef BOOL (_cdecl *ByValCdeclCaller_IntStructPack8Explicit)(IntStructPack8Explicit ispe);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalByValStruct_Cdecl_IntStructPack8Explicit(ByValCdeclCaller_IntStructPack8Explicit caller) 
{
    IntStructPack8Explicit ispe;
    ispe.i1 = 32;
    ispe.i2 = 32;

    if(!caller(ispe))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if( !IsCorrectIntStructPack8Explicit( &ispe ) )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}

typedef BOOL (__stdcall *ByValStdcallCaller_IntStructPack8Explicit)(IntStructPack8Explicit ispe);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalByValStruct_Stdcall_IntStructPack8Explicit(ByValStdcallCaller_IntStructPack8Explicit caller) 
{
    IntStructPack8Explicit ispe;
    ispe.i1 = 32;
    ispe.i2 = 32;

    if(!caller(ispe))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if( !IsCorrectIntStructPack8Explicit( &ispe ) )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}

/////
typedef BOOL (_cdecl *ByValCdeclCaller_LongStructPack16Explicit)(LongStructPack16Explicit lspe);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalByValStruct_Cdecl_LongStructPack16Explicit(ByValCdeclCaller_LongStructPack16Explicit caller) 
{
    LongStructPack16Explicit lspe;
    lspe.l1 = 32;
    lspe.l2 = 32;

    if(!caller(lspe))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if( !IsCorrectLongStructPack16Explicit( &lspe ) )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}

typedef BOOL (__stdcall *ByValStdcallCaller_LongStructPack16Explicit)(LongStructPack16Explicit lspe);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalByValStruct_Stdcall_LongStructPack16Explicit(ByValStdcallCaller_LongStructPack16Explicit caller) 
{
    LongStructPack16Explicit lspe;
    lspe.l1 = 32;
    lspe.l2 = 32;

    if(!caller(lspe))
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    if( !IsCorrectLongStructPack16Explicit( &lspe ) )
    {
        PRINT_ERR_INFO();
        return FALSE;
    }

    return TRUE;
}
/////
//---------------------------- ----------//
