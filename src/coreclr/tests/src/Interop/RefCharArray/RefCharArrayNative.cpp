// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <xplatform.h>
#include <platformdefines.h>
#include <stdio.h>
#include <stdlib.h>

const int LEN = 10;
extern "C" BOOL DLL_EXPORT __cdecl MarshalRefCharArray_Cdecl(char ** pstr)
{
    //Check the Input
    for(int i = 0; i < LEN; i++)
    {	
        if((*pstr)[i]!=('a'+i))
        {
            printf("MarshalRefCharArray_Cdecl: The value item %d is wrong",i+1);
            return FALSE;
        }
    }

    //Change the value
    for(int i = 0;i<LEN;i++)
    {
        (*pstr)[i] = 'z';
    }
    return TRUE;
}
extern "C" BOOL DLL_EXPORT __stdcall MarshalRefCharArray_Stdcall(char ** pstr)
{
    //Check the Input
    for(int i = 0;i < LEN;i++)
    {	
        if((*pstr)[i]!=('a'+i))
        {
            printf("MarshalRefCharArray_Cdecl: The value item %d is wrong",i+1);
            return FALSE;
        }
    }

    //Change the value
    for(int i = 0;i<LEN;i++)
    {
        (*pstr)[i] = 'z';
    }
    return TRUE;
}

typedef BOOL(__cdecl *CdeclCallBack)(char ** pstr);
extern "C" BOOL DLL_EXPORT __cdecl DoCallBack_MarshalRefCharArray_Cdecl(CdeclCallBack caller)
{
    char * str = (char*)CoreClrAlloc(LEN);
    for(int i = 0;i<LEN;i++)
    {
        str[i] = 'z';
    }
    if(!caller(&str))
    {
        printf("DoCallBack_MarshalRefCharArray_Cdecl:The Caller returns wrong value.\n");
        return FALSE;
    }
    if(str[0]!='a')
    {
        CoreClrFree(str);
        return FALSE;
    }
    return TRUE;
}

typedef BOOL(__stdcall *StdCallBack)(char ** pstr);
extern "C" BOOL DLL_EXPORT __stdcall DoCallBack_MarshalRefCharArray_Stdcall(StdCallBack caller)
{
    char * str = (char*)CoreClrAlloc(LEN);
    for(int i = 0;i<LEN;i++)
    {
        str[i] = 'z';
    }
    if(!caller(&str))
    {
        printf("DoCallBack_MarshalRefCharArray_Stdcall:The Caller returns wrong value.\n");
        return FALSE;
    }
    if(str[0]!='a')
    {

        CoreClrFree(str);
        return FALSE;
    }
    return TRUE;
}

typedef BOOL (__cdecl * DelegatePInvoke_Cdecl)(char **pstr);
extern "C" DLL_EXPORT DelegatePInvoke_Cdecl __cdecl DelegatePinvoke_Cdecl()
{
    return MarshalRefCharArray_Cdecl;
}

typedef BOOL (__stdcall * DelegatePInvoke_Stdcall)(char **pstr);
extern "C" DLL_EXPORT DelegatePInvoke_Stdcall __stdcall DelegatePinvoke_Stdcall()
{
    return MarshalRefCharArray_Stdcall;
}
