// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NativeDateTime.cpp : Defines the exported functions for the DLL application.

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <windows.h>
#include <oleauto.h>
#include <xplatform.h>

#define LCID_ENGLISH MAKELCID(MAKELANGID(0x09, 0x01), SORT_DEFAULT)

#pragma pack (push)
#pragma pack (8)
struct Stru_Seq_DateAsStructAsFld // size = 16 bytes
{
    DATE dt;
    INT iInt;
    BSTR bstr;
};
#pragma pack (pop)

#pragma pack (push)
#pragma pack (1)
struct Stru_Exp_DateAsStructAsFld // size = 16 bytes
{
    INT iInt;
    INT padding;
    DATE dt;
};
#pragma pack (pop)

extern "C" BOOL VerifySeqStruct(struct Stru_Seq_DateAsStructAsFld* StDate)
{
    BSTR str;
    VarBstrFromDate(StDate->dt, LCID_ENGLISH, VAR_FOURDIGITYEARS, &str);
    if(wcscmp(L"7/4/2008", (wchar_t *)str) != 0 )
    {
        wprintf(L"FAILURE! InDATE expected '07/04/2008' but received: %s\n", str);
        return FALSE;
    }
    if(StDate->iInt != 100)
    {
        wprintf(L"FAILURE! iInt expected 100 but received: %d\n", StDate->iInt);
        return FALSE;
    }
    if(wcscmp(L"Managed", (wchar_t *)(StDate->bstr)) != 0 )
    {
        wprintf(L"FAILURE! bstr expected 'Managed' but received: %s\n", StDate->bstr);
        return FALSE;
    }
    return TRUE;
}

extern "C" BOOL VerifyExpStruct(struct Stru_Exp_DateAsStructAsFld* StDate)
{
    BSTR str;
    VarBstrFromDate(StDate->dt, LCID_ENGLISH, VAR_FOURDIGITYEARS, &str);
    if(wcscmp(L"7/4/2008", (wchar_t *)str) != 0 )
    {
        wprintf(L"FAILURE! InDATE expected '07/04/2008' but received: %s\n", str);
        return FALSE;
    }
    if(StDate->iInt != 100)
    {
        wprintf(L"FAILURE! iInt expected 100 but received: %d\n", StDate->iInt);
        return FALSE;
    }
    return TRUE;
}

extern "C" void ChangeStru_Seq_DateAsStructAsFld(Stru_Seq_DateAsStructAsFld * StDate)
{
    VarDateFromStr(SysAllocString(L"8/15/1947"), 0, 0, &(StDate->dt));
}

extern "C" void ChangeStru_Exp_DateAsStructAsFld(Stru_Exp_DateAsStructAsFld * StDate)
{
    VarDateFromStr(SysAllocString(L"8/15/1947"), 0, 0, &(StDate->dt));
}

extern "C" DLL_EXPORT BOOL __stdcall Marshal_In_stdcall(DATE d)
{
    BSTR str;
    //DATE ptoD;
    
    // always use the ENGLISH locale so that the string comes out as 11/16/1977 as opposed to 
    // say 16/11/1977 for German locale; otherwise this test would fail on non-ENU locales

    VarBstrFromDate(d, LCID_ENGLISH, VAR_FOURDIGITYEARS, &str);

    if(wcscmp(L"7/4/2008", (wchar_t *)str) != 0 )
    {
        wprintf(L"FAILURE! InDATE expected '07/04/2008' but received: %s\n", str);
        return FALSE;
    }

    VarDateFromStr(SysAllocString(L"8/15/1947"), 0, 0, &d);
    return TRUE;

}

extern "C" DLL_EXPORT BOOL __cdecl Marshal_InOut_cdecl(/*[in,out]*/ DATE* d)
{
    BSTR str;
    //DATE ptoD;
    
    // always use the ENGLISH locale so that the string comes out as 11/16/1977 as opposed to 
    // say 16/11/1977 for German locale; otherwise this test would fail on non-ENU locales

    VarBstrFromDate(*d, LCID_ENGLISH, VAR_FOURDIGITYEARS, &str);

    if(wcscmp(L"7/4/2008", (wchar_t *)str) != 0 )
    {
        wprintf(L"FAILURE! InDATE expected '07/04/2008' but received: %s\n", str);
        return FALSE;
    }

    VarDateFromStr(SysAllocString(L"8/15/1947"), 0, 0, d);
    return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall Marshal_Out_stdcall(/*[out]*/ DATE* d)
{
    VarDateFromStr(SysAllocString(L"8/15/1947"), 0, 0, d);
    return TRUE;
}

extern "C" DLL_EXPORT DATE __stdcall Marshal_Ret_stdcall()
{
    DATE d;
    VarDateFromStr(SysAllocString(L"8/15/1947"), 0, 0, &d);
    return d;
}

typedef BOOL (__cdecl * Datetime_Del_Marshal_InOut_cdecl)(/*[in,out]*/ DATE* t);
extern "C" DLL_EXPORT Datetime_Del_Marshal_InOut_cdecl __stdcall GetDel_Marshal_InOut_cdecl()
{
    return Marshal_InOut_cdecl;
}

typedef DATE (__stdcall * Datetime_Del_Marshal_Ret_stdcall)();
extern "C" DLL_EXPORT Datetime_Del_Marshal_Ret_stdcall __stdcall GetDel_Marshal_Ret_stdcall()
{
    return Marshal_Ret_stdcall;
}


typedef BOOL (__stdcall * Datetime_Del_Marshal_Out_stdcall)(/*[out]*/ DATE* t);
extern "C" DLL_EXPORT  Datetime_Del_Marshal_Out_stdcall __stdcall GetDel_Marshal_Out_stdcall()
{
    return Marshal_Out_stdcall;
}

extern "C" DLL_EXPORT BOOL __cdecl RevP_Marshal_InOut_cdecl(Datetime_Del_Marshal_InOut_cdecl d)
{
    DATE ptoD;
    BSTR str;

    VarDateFromStr(SysAllocString(L"8/15/1947"), 0, 0, &ptoD);
    if(d(&ptoD) == FALSE)
    {
        wprintf(L"FAILURE! RevP_Marshal_InOut_cdecl : Date on managed side didn't match\n");
        return FALSE;
    }
    
    //Verify the changes are visible
    VarBstrFromDate(ptoD, LCID_ENGLISH, VAR_FOURDIGITYEARS, &str);
    if(wcscmp(L"8/14/1947", (wchar_t *)str) != 0 )
    {
        wprintf(L"FAILURE! RevP_Marshal_InOut_cdecl : InDATE expected '8/14/1947' but received: %s\n", str);
        return FALSE;
    }
    return TRUE;
}


extern "C" DLL_EXPORT BOOL __stdcall RevP_Marshal_Ret_stdcall(Datetime_Del_Marshal_Ret_stdcall d)
{
    DATE date;
    BSTR str;

    date = d();

    VarBstrFromDate(date, LCID_ENGLISH, VAR_FOURDIGITYEARS, &str);
    if(wcscmp(L"7/4/2008", (wchar_t *)str) != 0 )
    {
        wprintf(L"FAILURE! RevP_Marshal_Ret_stdcall : InDATE expected '07/04/2008' but received: %s\n", str);
        return FALSE;
    }
    return TRUE;
}

extern "C" DLL_EXPORT BOOL __cdecl MarshalSeqStruct_InOut_cdecl(/*[in,out]*/ struct Stru_Seq_DateAsStructAsFld * t)
{
    if(!VerifySeqStruct(t))
        return FALSE;
    
    ChangeStru_Seq_DateAsStructAsFld(t);
    return TRUE;
}

extern "C" DLL_EXPORT BOOL __cdecl MarshalExpStruct_InOut_cdecl(/*[in,out]*/ struct Stru_Exp_DateAsStructAsFld * t)
{
    if(!VerifyExpStruct(t))
        return FALSE;

    ChangeStru_Exp_DateAsStructAsFld(t);
    return TRUE;
}


typedef BOOL (__cdecl * Datetime_Del_MarshalExpStruct_InOut_cdecl)(/*[in,out]*/ struct Stru_Exp_DateAsStructAsFld * t);
extern "C" DLL_EXPORT Datetime_Del_MarshalExpStruct_InOut_cdecl __stdcall GetDel_Del_MarshalExpStruct_InOut_cdecl()
{
    return MarshalExpStruct_InOut_cdecl;
}


typedef BOOL (__cdecl * Datetime_Del_MarshalSeqStruct_InOut_cdecl)(/*[in,out]*/ struct Stru_Seq_DateAsStructAsFld * t);
extern "C" DLL_EXPORT Datetime_Del_MarshalSeqStruct_InOut_cdecl __stdcall GetDel_Del_MarshalSeqStruct_InOut_cdecl()
{
    return MarshalSeqStruct_InOut_cdecl;
}
