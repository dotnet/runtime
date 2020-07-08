// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdlib.h>
#include <locale.h>
#include <xplatform.h>
#include <platformdefines.h>

#pragma warning( push )
#pragma warning( disable : 4996)


static int fails = 0; //record the fail numbers
// Overload methods for reportfailure	
static int ReportFailure(const char* s)
{
    printf(" === Fail:%s\n", s);
    return (++fails);
}

extern  "C" DLL_EXPORT int __cdecl GetResult()
{
    return fails;
}

//This method is used on Windows Only
extern "C" DLL_EXPORT char __cdecl GetByteForWideChar()
{
#ifdef WINDOWS
    char * p = new char[3];
    WideCharToMultiByte(CP_ACP, 0, W("\x263c"), -1, p, 2, NULL, NULL);
    p[1] = '\0';
    char breturn = p[0];

    delete p;

    return breturn;
#else
    return 0; //It wont be called MAC
#endif

}

//x86: Managed(Encoding: utf8)---->Marshaler(Encoding:ASCII)---->Native(Encoding:utf8)
//MAC(x64):Managed(Encoding:utf8)----->Marshaler(Encoding:utf8)---->Native(Encoding:utf8)
//Now  both side(Managed Side and native side) takes the utf8 encoding when comparing string
bool CheckInput(LPSTR str)
{
    //int WideCharToMultiByte(
    //  UINT CodePage, 
    //  DWORD dwFlags, 
    //  LPCWSTR lpWideCharStr, 
    //  int cchWideChar, 
    //  LPSTR lpMultiByteStr, 
    //  int cbMultiByte, 
    //  LPCSTR lpDefaultChar, 
    //  LPBOOL lpUsedDefaultChar 
    //);
#ifdef WINDOWS
    char * p = new char[3];
    WideCharToMultiByte(CP_ACP, 0, W("\x263c"), -1, p, 2, NULL, NULL);
    p[1] = '\0';
#else
    char*  p = new char[4]; //00bc98e2,the utf8 code of "\263c",we can get these char value through the following code with C#
    p[0] = (char)0xe2;      //Encoding enc = Encoding.Default;//UTF8 Encoding
    p[1] = (char)0x98;      //Byte[] by = enc.GetBytes("\x263c");
    p[2] = (char)0xbc;
    p[3] = (char)0;
#endif
    if (0 != strncmp(str, p, 4))
    {
        printf("CheckInput:Expected:%s,Actual:%d\n", p, str[0]);
        delete[]p;
        return false;
    }
    delete[]p;
    return true;

}

//C Call,In attribute,LPstr
extern "C" DLL_EXPORT LPSTR __cdecl CLPStr_In(LPSTR pStr)
{
    //Check the Input
    if (!CheckInput(pStr))
    {
        ReportFailure("CLPStr_In:Native Side");
    }

    //alloc,copy, since we cannot depend the Marshaler's activity.
    size_t len = strlen(pStr) + 1; //+1, Include the NULL Character.
    LPSTR pBack = (LPSTR)CoreClrAlloc(sizeof(char) * len);
    strncpy(pBack, pStr, len);

    return pBack;
}

extern "C" DLL_EXPORT LPSTR __cdecl CLPStr_Out(LPSTR pStr)
{
    const char* pTemp = "AAAA";
    size_t len = strlen(pTemp) + 1; //+1, Include the NULL Character.
    LPSTR pBack = (LPSTR)CoreClrAlloc(sizeof(char) * len);
    strncpy(pBack, pTemp, strlen(pTemp) + 1);

    strncpy(pStr, pTemp, strlen(pTemp) + 1);

    return pBack;
}

extern "C" DLL_EXPORT LPSTR __cdecl CLPStr_InOut(LPSTR pStr)
{
    //Check the Input
    if (!CheckInput(pStr))
    {
        ReportFailure("CLPStr_InOut:Native Side");
    }

    //alloc,copy, since we cannot depend the Marshaler's activity.
    size_t len = strlen(pStr) + 1; //+1, Include the NULL Character.
    LPSTR pBack = (LPSTR)CoreClrAlloc(len);
    strncpy(pBack, pStr, len);

    return pBack;
}

extern "C" DLL_EXPORT LPSTR __cdecl CLPStr_InByRef(LPSTR* ppStr)
{
    //Check the Input
    if (!CheckInput(*ppStr))
    {
        ReportFailure("CLPStr_InByRef:Native Side");
    }

    //alloc,copy, since we cannot depend the Marshaler's activity.
    size_t len = strlen(*ppStr) + 1; //+1, Include the NULL Character.
    LPSTR pBack = (LPSTR)CoreClrAlloc(len);
    strncpy(pBack, *ppStr, len);

    return pBack;
}

extern "C" DLL_EXPORT LPSTR __cdecl CLPStr_OutByRef(LPSTR* ppStr)
{
    const char* pTemp = "AAAA";
    size_t len = strlen(pTemp) + 1; //+1, Include the NULL Character.
    LPSTR pBack = (LPSTR)CoreClrAlloc(sizeof(char) * len);
    strncpy(pBack, pTemp, strlen(pTemp) + 1);

    *ppStr = (LPSTR)CoreClrAlloc(sizeof(char) * len);
    strncpy(*ppStr, pTemp, strlen(pTemp) + 1);
    return pBack;
}

extern "C" DLL_EXPORT LPSTR __cdecl CLPStr_InOutByRef(LPSTR* ppStr)
{
    //Check the Input
    if (!CheckInput(*ppStr))
    {
        ReportFailure("CLPStr_InOutByRef:Native Side");
    }

    //alloc,copy, since we cannot depend the Marshaler's activity.
    size_t len = strlen(*ppStr) + 1; //+1, Include the NULL Character.
    LPSTR pBack = (LPSTR)CoreClrAlloc(len);
    strncpy(pBack, *ppStr, len);
    return pBack;
}


typedef LPSTR (__cdecl* delegate_cdecl)(LPSTR* ppstr);
extern "C" DLL_EXPORT delegate_cdecl __cdecl CLPStr_DelegatePInvoke()
{
    return CLPStr_InOutByRef;
}

//stdcall

extern "C" DLL_EXPORT LPSTR STDMETHODCALLTYPE SLPStr_In(LPSTR pStr)
{
    //Check the Input
    if (!CheckInput(pStr))
    {
        ReportFailure("SLPStr_In:NativeSide");
    }

    //alloc,copy, since we cannot depend the Marshaler's activity.
    size_t len = strlen(pStr) + 1; //+1, Include the NULL Character.
    LPSTR pBack = (LPSTR)CoreClrAlloc(len);
    strncpy(pBack, pStr, len);
    return pBack;
}

extern "C" DLL_EXPORT LPSTR STDMETHODCALLTYPE SLPStr_Out(LPSTR pStr)
{
    const char* pTemp = "AAAA";
    size_t len = strlen(pTemp) + 1; //+1, Include the NULL Character.
    LPSTR pBack = (LPSTR)CoreClrAlloc(sizeof(char) * len);
    strncpy(pBack, pTemp, strlen(pTemp) + 1);

    strncpy(pStr, pTemp, strlen(pTemp) + 1);
    return pBack;
}

extern "C" DLL_EXPORT LPSTR STDMETHODCALLTYPE SLPStr_InOut(LPSTR pStr)
{
    //Check the Input
    if (!CheckInput(pStr))
    {
        ReportFailure("SLPStr_InOut:NativeSide");
    }

    //alloc,copy, since we cannot depend the Marshaler's activity.
    size_t len = strlen(pStr) + 1; //+1, Include the NULL Character.
    LPSTR pBack = (LPSTR)CoreClrAlloc(len);
    strncpy(pBack, pStr, len);
    return pBack;
}

extern "C" DLL_EXPORT LPSTR STDMETHODCALLTYPE SLPStr_InByRef(LPSTR* ppStr)
{
    //Check the Input
    if (!CheckInput(*ppStr))
    {
        ReportFailure("SLPStr_InByRef:NativeSide");
    }
    //alloc,copy, since we cannot depend the Marshaler's activity.
    size_t len = strlen(*ppStr) + 1; //+1, Include the NULL Character.
    LPSTR pBack = (LPSTR)CoreClrAlloc(len);
    strncpy(pBack, *ppStr, len);
    return pBack;
}

extern "C" DLL_EXPORT LPSTR STDMETHODCALLTYPE SLPStr_OutByRef(LPSTR* ppStr)
{
    const char* pTemp = "AAAA";
    size_t len = strlen(pTemp) + 1; //+1, Include the NULL Character.
    LPSTR pBack = (LPSTR)CoreClrAlloc(sizeof(char) * len);
    strncpy(pBack, pTemp, strlen(pTemp) + 1);

    *ppStr = (LPSTR)CoreClrAlloc(sizeof(char) * len);
    strncpy(*ppStr, pTemp, strlen(pTemp) + 1);

    return pBack;
}

extern "C" DLL_EXPORT LPSTR STDMETHODCALLTYPE SLPStr_InOutByRef(LPSTR* ppStr)
{
    //Check the Input
    if (!CheckInput(*ppStr))
    {
        ReportFailure("SLPStr_InOutByRef:NativeSide");
    }

    //alloc,copy, since we cannot depend the Marshaler's activity.
    size_t len = strlen(*ppStr) + 1; //+1, Include the NULL Character.
    LPSTR pBack = (LPSTR)CoreClrAlloc(len);
    strncpy(pBack, *ppStr, len);
    return pBack;
}

typedef LPSTR (STDMETHODCALLTYPE *delegate_stdcall)(LPSTR* ppstr);
extern "C" DLL_EXPORT delegate_stdcall SLPStr_DelegatePInvoke()
{
    return SLPStr_InOutByRef;
}

///Cdecl, Reverse PInvoke

typedef LPSTR (__cdecl *CCallBackIn)(LPSTR pstr);
extern "C" DLL_EXPORT void __cdecl DoCCallBack_LPSTR_In(CCallBackIn callback)
{
  const char* pTemp = "AAAA";
  size_t len = strlen(pTemp)+1;
  LPSTR pStr = (LPSTR)CoreClrAlloc(len);
  strncpy(pStr,pTemp,len);

  if(!CheckInput(callback(pStr)))
  {
  	ReportFailure("DoCCallBack_LPSTR_In:NativeSide");
  }
  CoreClrFree(pStr);
}

typedef LPSTR (__cdecl *CCallBackOut)(LPSTR pstr);
extern "C" DLL_EXPORT void __cdecl DoCCallBack_LPSTR_Out(CCallBackOut callback)
{
    size_t len = 10;
    LPSTR pStr = (LPSTR)CoreClrAlloc(len);

    //Check the return value
    if (!CheckInput(callback(pStr)))
    {
        ReportFailure("DoCCallBack_LPSTR_Out:NativeSide,the first check");
    }
    if (!CheckInput(pStr))
    {
        ReportFailure("DoCCallBack_LPSTR_Out:NativeSide,the Second Check");
    }
    CoreClrFree(pStr);
}

typedef LPSTR (__cdecl *CCallBackInOut)(LPSTR pstr);
extern "C" DLL_EXPORT void __cdecl DoCCallBack_LPSTR_InOut(CCallBackInOut callback)
{
    const char* pTemp = "AAAA";
    size_t len = strlen(pTemp) + 1;
    LPSTR pStr = (LPSTR)CoreClrAlloc(len);
    strncpy(pStr, pTemp, len);

    if (!CheckInput(callback(pStr)))
    {
        ReportFailure("DoCCallBack_LPSTR_InOut:NativeSide,the first check");
    }
    if (!CheckInput(pStr))
    {
        ReportFailure("DoCCallBack_LPSTR_InOut:NativeSide,the Second Check");
    }
    CoreClrFree(pStr);
}

typedef LPSTR (__cdecl *CallBackInByRef)(LPSTR* pstr);
extern "C" DLL_EXPORT void __cdecl DoCCallBack_LPSTR_InByRef(CallBackInByRef callback)
{
    const char* pTemp = "AAAA";
    size_t len = strlen(pTemp) + 1;
    LPSTR pStr = (LPSTR)CoreClrAlloc(len);
    strncpy(pStr, pTemp, len);

    if (!CheckInput(callback(&pStr)))
    {
        ReportFailure("DoCCallBack_LPSTR_InByRef:NativeSide");
    }
    CoreClrFree(pStr);
}

typedef LPSTR (__cdecl *CCallBackOutByRef)(LPSTR* pstr);
extern "C" DLL_EXPORT void __cdecl DoCCallBack_LPSTR_OutByRef(CCallBackOutByRef callback)
{
    size_t len = 10;
    LPSTR pStr = (LPSTR)CoreClrAlloc(len);

    if (!CheckInput(callback(&pStr)))
    {
        ReportFailure("DoCCallBack_LPSTR_OutByRef:NativeSide,the first Check");
    }
    if (!CheckInput(pStr))
    {
        ReportFailure("DoCCallBack_LPSTR_OutByRef:NativeSide,the Second Check");
    }
    CoreClrFree(pStr);
}

typedef LPSTR (__cdecl *CCallBackInOutByRef)(LPSTR* pstr);
extern "C" DLL_EXPORT void __cdecl DoCCallBack_LPSTR_InOutByRef(CCallBackInOutByRef callback)
{
    const char* pTemp = "AAAA";
    size_t len = strlen(pTemp) + 1;
    LPSTR pStr = (LPSTR)CoreClrAlloc(len);
    strncpy(pStr, pTemp, len);

    if (!CheckInput(callback(&pStr)))
    {
        ReportFailure("DoCCallBack_LPSTR_InOutByRef:NativeSide");
    }
    if (!CheckInput(pStr))
    {
        ReportFailure("DoCCallBack_LPSTR_InOutByRef:NativeSide,the Second Check");
    }
    CoreClrFree(pStr);
}

///STDCALL Reverse PInvoke
typedef LPSTR (STDMETHODCALLTYPE *SCallBackIn)(LPSTR pstr);
extern "C" DLL_EXPORT void __cdecl DoSCallBack_LPSTR_In(SCallBackIn callback)
{
    const char* pTemp = "AAAA";
    size_t len = strlen(pTemp) + 1;
    LPSTR pStr = (LPSTR)CoreClrAlloc(len);
    strncpy(pStr, pTemp, len);

    if (!CheckInput(callback(pStr)))
    {
        ReportFailure("DoSCallBack_LPSTR_In:NativeSide");
    }
    CoreClrFree(pStr);
}

typedef LPSTR (STDMETHODCALLTYPE *SCallBackOut)(LPSTR pstr);
extern "C" DLL_EXPORT void __cdecl DoSCallBack_LPSTR_Out(SCallBackOut callback)
{

    size_t len = 10;
    LPSTR pStr = (LPSTR)CoreClrAlloc(len);

    if (!CheckInput(callback(pStr)))
    {
        ReportFailure("DoSCallBack_LPSTR_Out:NativeSide,the first check");
    }
    if (!CheckInput(pStr))
    {
        ReportFailure("DoSCallBack_LPSTR_Out:NativeSide,the Second Check");
    }
    CoreClrFree(pStr);
}

typedef LPSTR (STDMETHODCALLTYPE *SCallBackInOut)(LPSTR pstr);
extern "C" DLL_EXPORT void __cdecl DoSCallBack_LPSTR_InOut(SCallBackInOut callback)
{
    const char* pTemp = "AAAA";
    size_t len = strlen(pTemp) + 1;
    LPSTR pStr = (LPSTR)CoreClrAlloc(len);
    strncpy(pStr, pTemp, len);

    if (!CheckInput(callback(pStr)))
    {
        ReportFailure("DoSCallBack_LPSTR_InOut:NativeSide,the first check");
    }
    if (!CheckInput(pStr))
    {
        ReportFailure("DoSCallBack_LPSTR_InOut:NativeSide,the second Check");
    }
    CoreClrFree(pStr);
}

typedef LPSTR (STDMETHODCALLTYPE *SCallBackInByRef)(LPSTR* pstr);
extern "C" DLL_EXPORT void __cdecl DoSCallBack_LPSTR_InByRef(SCallBackInByRef callback)
{
    const char* pTemp = "AAAA";
    size_t len = strlen(pTemp) + 1;
    LPSTR pStr = (LPSTR)CoreClrAlloc(len);
    strncpy(pStr, pTemp, len);

    if (!CheckInput(callback(&pStr)))
    {
        ReportFailure("DoSCallBack_LPSTR_InByRef:NativeSide");
    }
    CoreClrFree(pStr);
}

typedef LPSTR (STDMETHODCALLTYPE *SCallBackOutByRef)(LPSTR* pstr);
extern "C" DLL_EXPORT void __cdecl DoSCallBack_LPSTR_OutByRef(SCallBackOutByRef callback)
{
    size_t len = 10;
    LPSTR pStr = (LPSTR)CoreClrAlloc(len);

    if (!CheckInput(callback(&pStr)))
    {
        ReportFailure("DoSCallBack_LPSTR_OutByRef:NativeSide,the first check");
    }
    if (!CheckInput(pStr))
    {
        ReportFailure("DoSCallBack_LPSTR_OutByRef:NativeSide,the second Check");
    }
    CoreClrFree(pStr);
}

typedef LPSTR (STDMETHODCALLTYPE *SCallBackInOutByRef)(LPSTR* pstr);
extern "C" DLL_EXPORT void __cdecl DoSCallBack_LPSTR_InOutByRef(SCallBackInOutByRef callback)
{
    const char* pTemp = "AAAA";
    size_t len = strlen(pTemp) + 1;
    LPSTR pStr = (LPSTR)CoreClrAlloc(len);
    strncpy(pStr, pTemp, len);

    if (!CheckInput(callback(&pStr)))
    {
        ReportFailure("DoSCallBack_LPSTR_InOutByRef:NativeSide,the first check");
    }
    if (!CheckInput(pStr))
    {
        ReportFailure("DoSCallBack_LPSTR_InOutByRef:NativeSide,the second Check");
    }
    CoreClrFree(pStr);
}
#pragma warning( pop )
