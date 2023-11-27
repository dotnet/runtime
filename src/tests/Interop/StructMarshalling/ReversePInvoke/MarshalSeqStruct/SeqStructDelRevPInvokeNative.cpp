// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <xplatform.h>
#include <platformdefines.h>
#include "SeqStructDelRevPInvokeNative.h"

const char* NativeStr = "Native";
//const size_t size = strlen(NativeStr);

struct ScriptParamType
{
    int idata;
    int useless; //Use this as pedding, since in the Manage code,the union starts at offset 8
    union unionType
    {
        bool bdata;
        double ddata;
        int * ptrdata;
    }udata;
};

struct ComplexStruct
{
    int32_t i;
    CHAR b;
    LPCSTR str;
    //use this(padding), since in the Mac, it use 4bytes as struct pack(In windows, it is 8 bytes).
    //if i dont add this. In Mac, it will try to replace the value of (pedding) with idata's value
    LPVOID padding;//padding
    ScriptParamType type;
};


extern "C" DLL_EXPORT BOOL _cdecl MarshalStructComplexStructByRef_Cdecl(ComplexStruct * pcs)
{
    //Check the Input
    if((321 != pcs->i)||(!pcs->b)||(0 != strcmp(pcs->str,"Managed"))||(123 != pcs->type.idata)||(0x120000 != (int)(int64_t)(pcs->type.udata.ptrdata)))
    {

        printf("The parameter for MarshalRefStruct_Cdecl is wrong\n");
        printf("ComplexStruct:%d:%d,%s,%d,%d\n",pcs->i,pcs->b,pcs->str,pcs->type.idata,(int)(int64_t)(pcs->type.udata.ptrdata));
        return FALSE;
    }
    CoreClrFree((LPVOID)pcs->str);

    const char* lNativeStr = "Native";
    const size_t lsize = strlen(lNativeStr);
    char * pstr = (char*)CoreClrAlloc((lsize + 1) * sizeof(char));
    memset(pstr,0,lsize+1);

    strncpy_s(pstr,lsize+1,lNativeStr,lsize);

    //Change the value
    pcs->i = 9999;
    pcs->b = false;
    pcs->str = pstr;
    pcs->type.idata = -1;
    pcs->type.udata.ddata = 3.14159;

    return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructComplexStructByRef_StdCall(ComplexStruct * pcs)
{
    //Check the input
    if((321 != pcs->i)||(!pcs->b)||(0 != strcmp(pcs->str,"Managed"))||(123 != pcs->type.idata)||(0x120000 != (int)(int64_t)(pcs->type.udata.ptrdata)))
    {
        printf("The parameter for MarshalRefStruct_StdCall is wrong\n");
        return FALSE;
    }
    CoreClrFree((LPVOID)pcs->str);

    const char* lNativeStr = "Native";
    const size_t lsize = strlen(lNativeStr);
    char * pstr = (char*)CoreClrAlloc(lsize + 1);
    memset(pstr,0,lsize+1);
    strncpy_s(pstr,lsize+1,lNativeStr,lsize);

    //Change the value
    pcs->i = 9999;
    pcs->b = false;
    pcs->str = pstr;
    pcs->type.idata = -1;
    pcs->type.udata.ddata = 3.14159;

    return TRUE;
}

typedef BOOL (_cdecl *ComplexStructByRefCdeclCaller)(ComplexStruct* pcs);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalStructComplexStructByRef_Cdecl(ComplexStructByRefCdeclCaller caller)
{
    const char* lNativeStr = "Native";
    const size_t lsize = strlen(lNativeStr);

    //Init
    char * pstr = (char*)CoreClrAlloc(lsize + 1);
    memset(pstr,0,lsize+1);
    strncpy_s(pstr,lsize+1,lNativeStr,lsize);

    ComplexStruct cs;
    cs.i = 9999;
    cs.b = false;
    cs.str = pstr;
    cs.type.idata = -1;
    cs.type.udata.ddata = 3.14159;

    if(!caller(&cs))
    {
        printf("DoCallBack_MarshalByRefStruct_Cdecl:The Caller return wrong value!\n");
        return FALSE;
    }

    if((321 != cs.i)||(!cs.b)||(0 != strcmp(cs.str,"Managed"))||(123 != cs.type.idata)||(0x120000 != (int)(int64_t)(cs.type.udata.ptrdata)))
    {
        printf("The parameter for DoCallBack_MarshalRefStruct_Cdecl is wrong\n");
        return FALSE;
    }
    CoreClrFree((LPVOID)cs.str);
    return TRUE;
}

typedef BOOL (__stdcall *ComplexStructByRefStdCallCaller)(ComplexStruct* pcs);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalStructComplexStructByRef_StdCall(ComplexStructByRefStdCallCaller caller)
{
    const char* lNativeStr = "Native";
    const size_t lsize = strlen(lNativeStr);

    //init
    char * pstr = (char*)CoreClrAlloc(lsize + 1);
    memset(pstr,0,lsize+1);
    strncpy_s(pstr,lsize+1,lNativeStr,lsize);

    ComplexStruct cs;
    cs.i = 9999;
    cs.b = false;
    cs.str = pstr;
    cs.type.idata = -1;
    cs.type.udata.ddata = 3.14159;

    if(!caller(&cs))
    {
        printf("DoCallBack_MarshalByRefStruct_StdCall:The Caller returns wrong value\n");
        return FALSE;
    }

    if((321 != cs.i)||(!cs.b)||(0 != strcmp(cs.str,"Managed"))||(123 != cs.type.idata)||(0x120000 != (int)(int64_t)(cs.type.udata.ptrdata)))
    {
        printf("The parameter for DoCallBack_MarshalRefStruct_StdCall is wrong\n");
        return FALSE;
    }
    CoreClrFree((LPVOID)cs.str);
    return TRUE;
}
//Delegate PInvoke,passbyref
typedef BOOL (_cdecl *ComplexStructDelegatePInvokeByRefCdeclCaller)(ComplexStruct* pcs);
extern "C" DLL_EXPORT ComplexStructDelegatePInvokeByRefCdeclCaller _cdecl Get_MarshalStructComplexStructByRef_Cdecl_FuncPtr()
{
    return MarshalStructComplexStructByRef_Cdecl;
}

typedef BOOL (__stdcall *ComplexStructDelegatePInvokeByRefStdCallCaller)(ComplexStruct* pcs);
extern "C" DLL_EXPORT ComplexStructDelegatePInvokeByRefStdCallCaller __stdcall Get_MarshalStructComplexStructByRef_StdCall_FuncPtr()
{
    return MarshalStructComplexStructByRef_StdCall;
}

//Passby value
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructComplexStructByVal_Cdecl(ComplexStruct cs)
{
    //Check the Input
    if((321 != cs.i)||(!cs.b)||(0 != strcmp(cs.str,"Managed"))||(123 != cs.type.idata)||(0x120000 != (int)(int64_t)(cs.type.udata.ptrdata)))
    {

        printf("The parameter for MarshalStructComplexStructByVal_Cdecl is wrong\n");
        printf("ComplexStruct:%d:%d,%s,%d,%d\n",cs.i,cs.b,cs.str,cs.type.idata,(int)(int64_t)(cs.type.udata.ptrdata));
        return FALSE;
    }

    cs.i = 9999;
    cs.b = false;
    cs.str = "Native";
    cs.type.idata = -1;
    cs.type.udata.ddata = 3.14159;

    return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructComplexStructByVal_StdCall(ComplexStruct cs)
{
    //Check the input
    if((321 != cs.i)||(!cs.b)||(0 != strcmp(cs.str,"Managed"))||(123 != cs.type.idata)||(0x120000 != (int)(int64_t)(cs.type.udata.ptrdata)))
    {
        printf("The parameter for MarshalStructComplexStructByVal_StdCall is wrong\n");
        return FALSE;
    }

    cs.i = 9999;
    cs.b = false;
    cs.str = "Native";
    cs.type.idata = -1;
    cs.type.udata.ddata = 3.14159;

    return TRUE;
}

typedef BOOL (_cdecl *ComplexStructByValCdeclCaller)(ComplexStruct cs);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalStructComplexStructByVal_Cdecl(ComplexStructByValCdeclCaller caller)
{
    //Init
    ComplexStruct cs;
    cs.i = 9999;
    cs.b = false;
    cs.str = "Native";
    cs.type.idata = -1;
    cs.type.udata.ddata = 3.14159;

    if(!caller(cs))
    {
        printf("DoCallBack_MarshalStructComplexStructByVal_Cdecl:The Caller returns wrong value\n");
        return FALSE;
    }

    //Verify the value unchanged
    if((9999 != cs.i)||(cs.b)||(0 != strcmp(cs.str,NativeStr))||(-1 != cs.type.idata)||(3.14159 != cs.type.udata.ddata))
    {
        printf("The parameter for DoCallBack_MarshalByValStruct_Cdecl is wrong\n");
        return FALSE;
    }
    return TRUE;
}

typedef BOOL (__stdcall *ComplexStructByValStdCallCaller)(ComplexStruct cs);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalStructComplexStructByVal_StdCall(ComplexStructByValStdCallCaller caller)
{
    ComplexStruct cs;
    cs.i = 9999;
    cs.b = false;
    cs.str = "Native";
    cs.type.idata = -1;
    cs.type.udata.ddata = 3.14159;

    if(!caller(cs))
    {
        printf("The parameter for DoCallBack_MarshalStructComplexStructByVal_StdCall is wrong\n");
        return FALSE;
    }

    //Verify the value unchanged
    if((9999 != cs.i)||(cs.b)||(0 != strcmp(cs.str,NativeStr))||(-1 != cs.type.idata)||(3.14159 != cs.type.udata.ddata))
    {
        printf("DoCallBack_MarshalStructComplexStructByVal_StdCall:The Caller returns wrong value\n");
        return FALSE;
    }
    return TRUE;
}
//Delegate PInvoke,passbyref
typedef BOOL (_cdecl *ComplexStructDelegatePInvokeByValCdeclCaller)(ComplexStruct cs);
extern "C" DLL_EXPORT ComplexStructDelegatePInvokeByValCdeclCaller _cdecl Get_MarshalStructComplexStructByVal_Cdecl_FuncPtr()
{
    return MarshalStructComplexStructByVal_Cdecl;
}

typedef BOOL (__stdcall *ComplexStructDelegatePInvokeByValStdCallCaller)(ComplexStruct cs);
extern "C" DLL_EXPORT ComplexStructDelegatePInvokeByValStdCallCaller __stdcall Get_MarshalStructComplexStructByVal_StdCall_FuncPtr()
{
    return MarshalStructComplexStructByVal_StdCall;
}

///////////////////////////////////////////Methods for InnerSequential struct////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructInnerSequentialByRef_Cdecl(InnerSequential* argstr)
{
    if(!IsCorrectInnerSequential(argstr))
    {
        printf("\tMarshalStructInnerSequentialByRef_Cdecl: InnerSequential param not as expected\n");
        PrintInnerSequential(argstr,"argstr");
        return FALSE;
    }
    ChangeInnerSequential(argstr);
    return TRUE;
}
extern "C" DLL_EXPORT BOOL __stdcall MarshalStructInnerSequentialByRef_StdCall(InnerSequential* argstr)
{
    if(!IsCorrectInnerSequential(argstr))
    {
        printf("\tMarshalStructInnerSequentialByRef_StdCall: InnerSequential param not as expected\n");
        PrintInnerSequential(argstr,"argstr");
        return FALSE;
    }
    ChangeInnerSequential(argstr);
    return TRUE;
}
typedef BOOL (_cdecl *InnerSequentialByRefCdeclCaller)(InnerSequential* pcs);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalStructInnerSequentialByRef_Cdecl(InnerSequentialByRefCdeclCaller caller)
{
    //Init
    InnerSequential argstr;
    argstr.f1 = 77;
    argstr.f2 = 77.0;
    const char* lpstr = "changed string";
    size_t size = sizeof(char) * (strlen(lpstr) + 1);
    LPSTR temp = (LPSTR)CoreClrAlloc( size );
    memset(temp, 0, size);
    if(temp)
    {
        strcpy_s((char*)temp,size,lpstr);
        argstr.f3 = temp;
    }

    if(!caller(&argstr))
    {
        printf("DoCallBack_MarshalStructInnerSequentialByRef_Cdecl:The Caller returns wrong value\n");
        return FALSE;
    }
    //Verify the value unchanged
    if(!IsCorrectInnerSequential(&argstr))
    {
        printf("The parameter for DoCallBack_MarshalStructInnerSequentialByRef_Cdecl is wrong\n");
        return FALSE;
    }
    return TRUE;
}

typedef BOOL (__stdcall *InnerSequentialByRefStdCallCaller)(InnerSequential* pcs);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalStructInnerSequentialByRef_StdCall(InnerSequentialByRefStdCallCaller caller)
{
    //Init
    InnerSequential argstr;
    argstr.f1 = 77;
    argstr.f2 = 77.0;
    const char* lpstr = "changed string";
    size_t size = sizeof(char) * (strlen(lpstr) + 1);
    LPSTR temp = (LPSTR)CoreClrAlloc( size );
    memset(temp, 0, size);
    if(temp)
    {
        strcpy_s((char*)temp,size,lpstr);
        argstr.f3 = temp;
    }

    if(!caller(&argstr))
    {
        printf("DoCallBack_MarshalStructInnerSequentialByRef_StdCall:The Caller returns wrong value\n");
        return FALSE;
    }
    //Verify the value unchanged
    if(!IsCorrectInnerSequential(&argstr))
    {
        printf("The parameter for DoCallBack_MarshalStructInnerSequentialByRef_StdCall is wrong\n");
        return FALSE;
    }
    return TRUE;
}
//Delegate PInvoke,passbyref
typedef BOOL (_cdecl *InnerSequentialDelegatePInvokeByRefCdeclCaller)(InnerSequential* pcs);
extern "C" DLL_EXPORT InnerSequentialDelegatePInvokeByRefCdeclCaller _cdecl Get_MarshalStructInnerSequentialByRef_Cdecl_FuncPtr()
{
    return MarshalStructInnerSequentialByRef_Cdecl;
}

typedef BOOL (__stdcall *InnerSequentialDelegatePInvokeByRefStdCallCaller)(InnerSequential* pcs);
extern "C" DLL_EXPORT InnerSequentialDelegatePInvokeByRefStdCallCaller __stdcall Get_MarshalStructInnerSequentialByRef_StdCall_FuncPtr()
{
    return MarshalStructInnerSequentialByRef_StdCall;
}


//Passby value
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructInnerSequentialByVal_Cdecl(InnerSequential argstr)
{
    //Check the Input
    if(!IsCorrectInnerSequential(&argstr))
    {
        printf("\tMarshalStructInnerSequentialByVal_Cdecl: InnerSequential param not as expected\n");
        PrintInnerSequential(&argstr,"argstr");
        return FALSE;
    }

    argstr.f1 = 77;
    argstr.f2 = 77.0;
    const char* lpstr = "changed string";
    size_t size = sizeof(char) * (strlen(lpstr) + 1);
    LPSTR temp = (LPSTR)CoreClrAlloc( size );
    memset(temp, 0, size);
    if(temp)
    {
        strcpy_s((char*)temp,size,lpstr);
        argstr.f3 = temp;
    }
    return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructInnerSequentialByVal_StdCall(InnerSequential argstr)
{
    //Check the Input
    if(!IsCorrectInnerSequential(&argstr))
    {
        printf("\tMarshalStructInnerSequentialByVal_StdCall: InnerSequential param not as expected\n");
        PrintInnerSequential(&argstr,"argstr");
        return FALSE;
    }

    argstr.f1 = 77;
    argstr.f2 = 77.0;
    const char* lpstr = "changed string";
    size_t size = sizeof(char) * (strlen(lpstr) + 1);
    LPSTR temp = (LPSTR)CoreClrAlloc( size );
    memset(temp, 0, size);
    if(temp)
    {
        strcpy_s((char*)temp,size,lpstr);
        argstr.f3 = temp;
    }
    return TRUE;
}

typedef BOOL (_cdecl *InnerSequentialByValCdeclCaller)(InnerSequential cs);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalStructInnerSequentialByVal_Cdecl(InnerSequentialByValCdeclCaller caller)
{
    //Init
    InnerSequential argstr{};
    argstr.f1 = 77;
    argstr.f2 = 77.0;
    const char* lpstr = "changed string";
    size_t size = sizeof(char) * (strlen(lpstr) + 1);
    LPSTR temp = (LPSTR)CoreClrAlloc( size );
    memset(temp, 0, size);
    if(temp)
    {
        strcpy_s((char*)temp,size,lpstr);
        argstr.f3 = temp;
    }

    if(!caller(argstr))
    {
        printf("DoCallBack_MarshalStructInnerSequentialByVal_Cdecl:The Caller returns wrong value\n");
        return FALSE;
    }

    //Verify the value unchanged
    if(argstr.f1 != 77 || argstr.f2 != 77.0 || strcmp((char*)argstr.f3, "changed string") != 0)
    {
        printf("The parameter for DoCallBack_MarshalStructInnerSequentialByVal_Cdecl is wrong\n");
        return FALSE;
    }
    return TRUE;
}

typedef BOOL (__stdcall *InnerSequentialByValStdCallCaller)(InnerSequential cs);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalStructInnerSequentialByVal_StdCall(InnerSequentialByValStdCallCaller caller)
{
    //Init
    InnerSequential argstr{};
    argstr.f1 = 77;
    argstr.f2 = 77.0;
    const char* lpstr = "changed string";
    size_t size = sizeof(char) * (strlen(lpstr) + 1);
    LPSTR temp = (LPSTR)CoreClrAlloc( size );
    memset(temp, 0, size);
    if(temp)
    {
        strcpy_s((char*)temp,size,lpstr);
        argstr.f3 = temp;
    }

    if(!caller(argstr))
    {
        printf("DoCallBack_MarshalStructInnerSequentialByVal_StdCall:The Caller returns wrong value\n");
        return FALSE;
    }

    //Verify the value unchanged
    if(argstr.f1 != 77 || argstr.f2 != 77.0 || strcmp((char*)argstr.f3, "changed string") != 0)
    {
        printf("The parameter for DoCallBack_MarshalStructInnerSequentialByVal_StdCall is wrong\n");
        return FALSE;
    }
    return TRUE;
}
//Delegate PInvoke,passbyval
typedef BOOL (_cdecl *InnerSequentialDelegatePInvokeByValCdeclCaller)(InnerSequential cs);
extern "C" DLL_EXPORT InnerSequentialDelegatePInvokeByValCdeclCaller _cdecl Get_MarshalStructInnerSequentialByVal_Cdecl_FuncPtr()
{
    return MarshalStructInnerSequentialByVal_Cdecl;
}

typedef BOOL (__stdcall *InnerSequentialDelegatePInvokeByValStdCallCaller)(InnerSequential cs);
extern "C" DLL_EXPORT InnerSequentialDelegatePInvokeByValStdCallCaller __stdcall Get_MarshalStructInnerSequentialByVal_StdCall_FuncPtr()
{
    return MarshalStructInnerSequentialByVal_StdCall;
}


///////////////////////////////////////////Methods for InnerArraySequential struct////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructInnerArraySequentialByRef_Cdecl(InnerArraySequential* argstr)
{
    if(!IsCorrectInnerArraySequential(argstr))
    {
        printf("\tMarshalStructInnerArraySequentialByRef_Cdecl: InnerArraySequential param not as expected\n");
        PrintInnerArraySequential(argstr,"argstr");
        return FALSE;
    }
    ChangeInnerArraySequential(argstr);
    return TRUE;
}
extern "C" DLL_EXPORT BOOL __stdcall MarshalStructInnerArraySequentialByRef_StdCall(InnerArraySequential* argstr)
{
    if(!IsCorrectInnerArraySequential(argstr))
    {
        printf("\tMarshalStructInnerArraySequentialByRef_StdCall: InnerArraySequential param not as expected\n");
        PrintInnerArraySequential(argstr,"argstr");
        return FALSE;
    }
    ChangeInnerArraySequential(argstr);
    return TRUE;
}
typedef BOOL (_cdecl *InnerArraySequentialByRefCdeclCaller)(InnerArraySequential* pcs);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalStructInnerArraySequentialByRef_Cdecl(InnerArraySequentialByRefCdeclCaller caller)
{
    //Init
    InnerArraySequential argstr;

    for(int i = 0;i<NumArrElements;i++)
    {
        argstr.arr[i].f1 = 77;
        argstr.arr[i].f2 = 77.0;
        const char* lpstr = "changed string";
        size_t size = sizeof(char) * (strlen(lpstr) + 1);
        LPSTR temp = (LPSTR)CoreClrAlloc( size );
        memset(temp, 0, size);
        if(temp)
        {
            strcpy_s((char*)temp,size,lpstr);
            argstr.arr[i].f3 = temp;
        }
        else
        {
            printf("Memory Allocated Failed!");
        }
    }

    if(!caller(&argstr))
    {
        printf("DoCallBack_MarshalStructInnerArraySequentialByRef_Cdecl:The Caller returns wrong value\n");
        return FALSE;
    }
    //Verify the value unchanged
    if(!IsCorrectInnerArraySequential(&argstr))
    {
        printf("The parameter for DoCallBack_MarshalStructInnerArraySequentialByRef_Cdecl is wrong\n");
        return FALSE;
    }
    return TRUE;
}

typedef BOOL (__stdcall *InnerArraySequentialByRefStdCallCaller)(InnerArraySequential* pcs);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalStructInnerArraySequentialByRef_StdCall(InnerArraySequentialByRefStdCallCaller caller)
{
    //Init
    InnerArraySequential argstr;
    for(int i = 0;i<NumArrElements;i++)
    {
        argstr.arr[i].f1 = 77;
        argstr.arr[i].f2 = 77.0;
        const char* lpstr = "changed string";
        size_t size = sizeof(char) * (strlen(lpstr) + 1);
        LPSTR temp = (LPSTR)CoreClrAlloc( size );
        memset(temp, 0, size);
        if(temp)
        {
            strcpy_s((char*)temp,size,lpstr);
            argstr.arr[i].f3 = temp;
        }
        else
        {
            printf("Memory Allocated Failed!");
        }
    }
    if(!caller(&argstr))
    {
        printf("DoCallBack_MarshalStructInnerArraySequentialByRef_StdCall:The Caller returns wrong value\n");
        return FALSE;
    }
    //Verify the value unchanged
    if(!IsCorrectInnerArraySequential(&argstr))
    {
        printf("The parameter for DoCallBack_MarshalStructInnerArraySequentialByRef_StdCall is wrong\n");
        return FALSE;
    }
    return TRUE;
}
//Delegate PInvoke,passbyref
typedef BOOL (_cdecl *InnerArraySequentialDelegatePInvokeByRefCdeclCaller)(InnerArraySequential* pcs);
extern "C" DLL_EXPORT InnerArraySequentialDelegatePInvokeByRefCdeclCaller _cdecl Get_MarshalStructInnerArraySequentialByRef_Cdecl_FuncPtr()
{
    return MarshalStructInnerArraySequentialByRef_Cdecl;
}

typedef BOOL (__stdcall *InnerArraySequentialDelegatePInvokeByRefStdCallCaller)(InnerArraySequential* pcs);
extern "C" DLL_EXPORT InnerArraySequentialDelegatePInvokeByRefStdCallCaller __stdcall Get_MarshalStructInnerArraySequentialByRef_StdCall_FuncPtr()
{
    return MarshalStructInnerArraySequentialByRef_StdCall;
}


//Passby value
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructInnerArraySequentialByVal_Cdecl(InnerArraySequential argstr)
{
    //Check the Input
    if(!IsCorrectInnerArraySequential(&argstr))
    {
        printf("\tMarshalStructInnerArraySequentialByVal_Cdecl: InnerArraySequential param not as expected\n");
        PrintInnerArraySequential(&argstr,"argstr");
        return FALSE;
    }

    for(int i = 0;i<NumArrElements;i++)
    {
        argstr.arr[i].f1 = 77;
        argstr.arr[i].f2 = 77.0;
        const char* lpstr = "changed string";
        size_t size = sizeof(char) * (strlen(lpstr) + 1);
        LPSTR temp = (LPSTR)CoreClrAlloc( size );
        memset(temp, 0, size);
        if(temp)
        {
            strcpy_s((char*)temp,size,lpstr);
            argstr.arr[i].f3 = temp;
        }
        else
        {
            printf("Memory Allocated Failed!");
        }
    }
    return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructInnerArraySequentialByVal_StdCall(InnerArraySequential argstr)
{
    //Check the Input
    if(!IsCorrectInnerArraySequential(&argstr))
    {
        printf("\tMarshalStructInnerArraySequentialByVal_StdCall: InnerArraySequential param not as expected\n");
        PrintInnerArraySequential(&argstr,"argstr");
        return FALSE;
    }
    for(int i = 0; i<NumArrElements;i++)
    {
        argstr.arr[i].f1 = 77;
        argstr.arr[i].f2 = 77.0;
        const char* lpstr = "changed string";
        size_t size = sizeof(char) * (strlen(lpstr) + 1);
        LPSTR temp = (LPSTR)CoreClrAlloc( size );
        memset(temp, 0, size);
        if(temp)
        {
            strcpy_s((char*)temp,size,lpstr);
            argstr.arr[i].f3 = temp;
        }
        else
        {
            printf("Memory Allocated Failed!");
        }
    }
    return TRUE;
}

typedef BOOL (_cdecl *InnerArraySequentialByValCdeclCaller)(InnerArraySequential cs);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalStructInnerArraySequentialByVal_Cdecl(InnerArraySequentialByValCdeclCaller caller)
{
    //Init
    InnerArraySequential argstr;
    for(int i = 0;i<NumArrElements;i++)
    {
        argstr.arr[i].f1 = 77;
        argstr.arr[i].f2 = 77.0;
        const char* lpstr = "changed string";
        size_t size = sizeof(char) * (strlen(lpstr) + 1);
        LPSTR temp = (LPSTR)CoreClrAlloc( size );
        memset(temp, 0, size);
        if(temp)
        {
            strcpy_s((char*)temp,size,lpstr);
            argstr.arr[i].f3 = temp;
        }
        else
        {
            printf("Memory Allocated Failed!");
        }
    }

    if(!caller(argstr))
    {
        printf("DoCallBack_MarshalStructInnerArraySequentialByVal_Cdecl:The Caller returns wrong value\n");
        return FALSE;
    }

    //Verify the value unchanged
    for(int i =0;i<NumArrElements;i++)
    {
        if(argstr.arr[i].f1 != 77 || argstr.arr[i].f2 != 77.0 || strcmp((char*)argstr.arr[i].f3, "changed string") != 0)
        {
            printf("The parameter for DoCallBack_MarshalStructInnerArraySequentialByVal_Cdecl is wrong\n");
            return FALSE;
        }
    }
    return TRUE;
}

typedef BOOL (__stdcall *InnerArraySequentialByValStdCallCaller)(InnerArraySequential cs);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalStructInnerArraySequentialByVal_StdCall(InnerArraySequentialByValStdCallCaller caller)
{
    //Init
    InnerArraySequential argstr;
    for(int i = 0;i<NumArrElements;i++)
    {
        argstr.arr[i].f1 = 77;
        argstr.arr[i].f2 = 77.0;
        const char* lpstr = "changed string";
        size_t size = sizeof(char) * (strlen(lpstr) + 1);
        LPSTR temp = (LPSTR)CoreClrAlloc( size );
        memset(temp, 0, size);
        if(temp)
        {
            strcpy_s((char*)temp,size,lpstr);
            argstr.arr[i].f3 = temp;
        }
        else
        {
            printf("Memory Allocated Failed!");
        }
    }

    if(!caller(argstr))
    {
        printf("DoCallBack_MarshalStructInnerArraySequentialByVal_StdCall:The Caller returns wrong value\n");
        return FALSE;
    }

    //Verify the value unchanged
    for(int i = 0;i<NumArrElements;i++)
    {
        if(argstr.arr[i].f1 != 77 || argstr.arr[i].f2 != 77.0 || strcmp((char*)argstr.arr[i].f3, "changed string") != 0)
        {
            printf("The parameter for DoCallBack_MarshalStructInnerArraySequentialByVal_StdCall is wrong\n");
            return FALSE;
        }
    }
    return TRUE;
}
//Delegate PInvoke,passbyval
typedef BOOL (_cdecl *InnerArraySequentialDelegatePInvokeByValCdeclCaller)(InnerArraySequential cs);
extern "C" DLL_EXPORT InnerArraySequentialDelegatePInvokeByValCdeclCaller _cdecl Get_MarshalStructInnerArraySequentialByVal_Cdecl_FuncPtr()
{
    return MarshalStructInnerArraySequentialByVal_Cdecl;
}

typedef BOOL (__stdcall *InnerArraySequentialDelegatePInvokeByValStdCallCaller)(InnerArraySequential cs);
extern "C" DLL_EXPORT InnerArraySequentialDelegatePInvokeByValStdCallCaller __stdcall Get_MarshalStructInnerArraySequentialByVal_StdCall_FuncPtr()
{
    return MarshalStructInnerArraySequentialByVal_StdCall;
}

///////////////////////////////////////////Methods for CharSetAnsiSequential struct////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructCharSetAnsiSequentialByRef_Cdecl(CharSetAnsiSequential* argstr)
{
    if(!IsCorrectCharSetAnsiSequential(argstr))
    {
        printf("\tMarshalStructCharSetAnsiSequentialByRef_Cdecl: CharSetAnsiSequential param not as expected\n");
        PrintCharSetAnsiSequential(argstr,"argstr");
        return FALSE;
    }
    ChangeCharSetAnsiSequential(argstr);
    return TRUE;
}
extern "C" DLL_EXPORT BOOL __stdcall MarshalStructCharSetAnsiSequentialByRef_StdCall(CharSetAnsiSequential* argstr)
{
    if(!IsCorrectCharSetAnsiSequential(argstr))
    {
        printf("\tMarshalStructCharSetAnsiSequentialByRef_StdCall: CharSetAnsiSequential param not as expected\n");
        PrintCharSetAnsiSequential(argstr,"argstr");
        return FALSE;
    }
    ChangeCharSetAnsiSequential(argstr);
    return TRUE;
}
typedef BOOL (_cdecl *CharSetAnsiSequentialByRefCdeclCaller)(CharSetAnsiSequential* pcs);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalStructCharSetAnsiSequentialByRef_Cdecl(CharSetAnsiSequentialByRefCdeclCaller caller)
{
    //Init
    CharSetAnsiSequential argstr;

    const char* strSource = "change string";
    size_t size = strlen(strSource) + 1;
    LPSTR temp = (LPSTR)CoreClrAlloc(size);
    if(temp != NULL)
    {
        strcpy_s((char*)temp,size,strSource);
        argstr.f1 = temp;
        argstr.f2 = 'n';
    }
    else
    {
        printf("Memory Allocated Failed!");
    }

    if(!caller(&argstr))
    {
        printf("DoCallBack_MarshalStructCharSetAnsiSequentialByRef_Cdecl:The Caller returns wrong value\n");
        return FALSE;
    }
    //Verify the value unchanged
    if(!IsCorrectCharSetAnsiSequential(&argstr))
    {
        printf("The parameter for DoCallBack_MarshalStructCharSetAnsiSequentialByRef_Cdecl is wrong\n");
        return FALSE;
    }
    return TRUE;
}

typedef BOOL (__stdcall *CharSetAnsiSequentialByRefStdCallCaller)(CharSetAnsiSequential* pcs);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalStructCharSetAnsiSequentialByRef_StdCall(CharSetAnsiSequentialByRefStdCallCaller caller)
{
    //Init
    CharSetAnsiSequential argstr;

    const char* strSource = "change string";
    size_t size = strlen(strSource) + 1;
    LPSTR temp = (LPSTR)CoreClrAlloc(size);
    if(temp != NULL)
    {
        strcpy_s((char*)temp,size,strSource);
        argstr.f1 = temp;
        argstr.f2 = 'n';
    }
    else
    {
        printf("Memory Allocated Failed!");
    }

    if(!caller(&argstr))
    {
        printf("DoCallBack_MarshalStructCharSetAnsiSequentialByRef_StdCall:The Caller returns wrong value\n");
        return FALSE;
    }
    //Verify the value unchanged
    if(!IsCorrectCharSetAnsiSequential(&argstr))
    {
        printf("The parameter for DoCallBack_MarshalStructCharSetAnsiSequentialByRef_StdCall is wrong\n");
        return FALSE;
    }
    return TRUE;
}
//Delegate PInvoke,passbyref
typedef BOOL (_cdecl *CharSetAnsiSequentialDelegatePInvokeByRefCdeclCaller)(CharSetAnsiSequential* pcs);
extern "C" DLL_EXPORT CharSetAnsiSequentialDelegatePInvokeByRefCdeclCaller _cdecl Get_MarshalStructCharSetAnsiSequentialByRef_Cdecl_FuncPtr()
{
    return MarshalStructCharSetAnsiSequentialByRef_Cdecl;
}

typedef BOOL (__stdcall *CharSetAnsiSequentialDelegatePInvokeByRefStdCallCaller)(CharSetAnsiSequential* pcs);
extern "C" DLL_EXPORT CharSetAnsiSequentialDelegatePInvokeByRefStdCallCaller __stdcall Get_MarshalStructCharSetAnsiSequentialByRef_StdCall_FuncPtr()
{
    return MarshalStructCharSetAnsiSequentialByRef_StdCall;
}


//Passby value
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructCharSetAnsiSequentialByVal_Cdecl(CharSetAnsiSequential argstr)
{
    //Check the Input
    if(!IsCorrectCharSetAnsiSequential(&argstr))
    {
        printf("\tMarshalStructCharSetAnsiSequentialByVal_Cdecl: CharSetAnsiSequential param not as expected\n");
        PrintCharSetAnsiSequential(&argstr,"argstr");
        return FALSE;
    }

    const char* strSource = "change string";
    size_t size = strlen(strSource) + 1;
    LPSTR temp = (LPSTR)CoreClrAlloc(size);
    if(temp != NULL)
    {
        strcpy_s((char*)temp,size,strSource);
        argstr.f1 = temp;
        argstr.f2 = 'n';
    }
    else
    {
        printf("Memory Allocated Failed!");
    }
    return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructCharSetAnsiSequentialByVal_StdCall(CharSetAnsiSequential argstr)
{
    //Check the Input
    if(!IsCorrectCharSetAnsiSequential(&argstr))
    {
        printf("\tMarshalStructCharSetAnsiSequentialByVal_StdCall: CharSetAnsiSequential param not as expected\n");
        PrintCharSetAnsiSequential(&argstr,"argstr");
        return FALSE;
    }

    const char* strSource = "change string";
    size_t size = strlen(strSource) + 1;
    LPSTR temp = (LPSTR)CoreClrAlloc(size);
    if(temp != NULL)
    {
        strcpy_s((char*)temp,size,strSource);
        argstr.f1 = temp;
        argstr.f2 = 'n';
    }
    else
    {
        printf("Memory Allocated Failed!");
    }
    return TRUE;
}

typedef BOOL (_cdecl *CharSetAnsiSequentialByValCdeclCaller)(CharSetAnsiSequential cs);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalStructCharSetAnsiSequentialByVal_Cdecl(CharSetAnsiSequentialByValCdeclCaller caller)
{
    //Init
    CharSetAnsiSequential argstr{};
    const char* strSource = "change string";
    size_t size = strlen(strSource) + 1;
    LPSTR temp = (LPSTR)CoreClrAlloc(size);
    if(temp != NULL)
    {
        strcpy_s((char*)temp,size,strSource);
        argstr.f1 = temp;
        argstr.f2 = 'n';
    }
    else
    {
        printf("Memory Allocated Failed!");
    }

    if(!caller(argstr))
    {
        printf("DoCallBack_MarshalStructCharSetAnsiSequentialByVal_Cdecl:The Caller returns wrong value\n");
        return FALSE;
    }

    //Verify the value unchanged
    if(strcmp((char*)argstr.f1,"change string") != 0 || argstr.f2 != 'n')
    {
        printf("The parameter for DoCallBack_MarshalStructCharSetAnsiSequentialByVal_Cdecl is wrong\n");
        return FALSE;
    }
    return TRUE;
}

typedef BOOL (__stdcall *CharSetAnsiSequentialByValStdCallCaller)(CharSetAnsiSequential cs);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalStructCharSetAnsiSequentialByVal_StdCall(CharSetAnsiSequentialByValStdCallCaller caller)
{
    //Init
    CharSetAnsiSequential argstr{};
    const char* strSource = "change string";
    size_t size = strlen(strSource) + 1;
    LPSTR temp = (LPSTR)CoreClrAlloc(size);
    if(temp != NULL)
    {
        strcpy_s((char*)temp,size,strSource);
        argstr.f1 = temp;
        argstr.f2 = 'n';
    }
    else
    {
        printf("Memory Allocated Failed!");
    }

    if(!caller(argstr))
    {
        printf("DoCallBack_MarshalStructCharSetAnsiSequentialByVal_StdCall:The Caller returns wrong value\n");
        return FALSE;
    }

    //Verify the value unchanged
    if(strcmp((char*)argstr.f1,"change string") != 0 || argstr.f2 != 'n')
    {
        printf("The parameter for DoCallBack_MarshalStructCharSetAnsiSequentialByVal_StdCall is wrong\n");
        return FALSE;
    }
    return TRUE;
}
//Delegate PInvoke,passbyval
typedef BOOL (_cdecl *CharSetAnsiSequentialDelegatePInvokeByValCdeclCaller)(CharSetAnsiSequential cs);
extern "C" DLL_EXPORT CharSetAnsiSequentialDelegatePInvokeByValCdeclCaller _cdecl Get_MarshalStructCharSetAnsiSequentialByVal_Cdecl_FuncPtr()
{
    return MarshalStructCharSetAnsiSequentialByVal_Cdecl;
}

typedef BOOL (__stdcall *CharSetAnsiSequentialDelegatePInvokeByValStdCallCaller)(CharSetAnsiSequential cs);
extern "C" DLL_EXPORT CharSetAnsiSequentialDelegatePInvokeByValStdCallCaller __stdcall Get_MarshalStructCharSetAnsiSequentialByVal_StdCall_FuncPtr()
{
    return MarshalStructCharSetAnsiSequentialByVal_StdCall;
}


///////////////////////////////////////////Methods for CharSetUnicodeSequential struct////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructCharSetUnicodeSequentialByRef_Cdecl(CharSetUnicodeSequential* argstr)
{
    if(!IsCorrectCharSetUnicodeSequential(argstr))
    {
        printf("\tMarshalStructCharSetUnicodeSequentialByRef_Cdecl: CharSetUnicodeSequential param not as expected\n");
        PrintCharSetUnicodeSequential(argstr,"argstr");
        return FALSE;
    }
    ChangeCharSetUnicodeSequential(argstr);
    return TRUE;
}
extern "C" DLL_EXPORT BOOL __stdcall MarshalStructCharSetUnicodeSequentialByRef_StdCall(CharSetUnicodeSequential* argstr)
{
    if(!IsCorrectCharSetUnicodeSequential(argstr))
    {
        printf("\tMarshalStructCharSetUnicodeSequentialByRef_StdCall: CharSetUnicodeSequential param not as expected\n");
        PrintCharSetUnicodeSequential(argstr,"argstr");
        return FALSE;
    }
    ChangeCharSetUnicodeSequential(argstr);
    return TRUE;
}
typedef BOOL (_cdecl *CharSetUnicodeSequentialByRefCdeclCaller)(CharSetUnicodeSequential* pcs);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalStructCharSetUnicodeSequentialByRef_Cdecl(CharSetUnicodeSequentialByRefCdeclCaller caller)
{
    //Init
    CharSetUnicodeSequential argstr;

    WCHAR* strSource = (WCHAR*)(W("change string"));
    size_t len = TP_slen(strSource);
    LPCWSTR temp = (LPCWSTR)CoreClrAlloc(sizeof(WCHAR)*(len+1));
    if(temp != NULL)
    {
        //wcscpy((WCHAR*)temp, (len+1)*sizeof(WCHAR), strSource);
        TP_scpy_s((WCHAR*)temp,len+1,strSource);
        argstr.f1 = temp;
        argstr.f2 = 'n';
    }
    else
    {
        printf("Memory Allocated Failed!");
    }

    if(!caller(&argstr))
    {
        printf("DoCallBack_MarshalStructCharSetUnicodeSequentialByRef_Cdecl:The Caller returns wrong value\n");
        return FALSE;
    }
    //Verify the value unchanged
    if(!IsCorrectCharSetUnicodeSequential(&argstr))
    {
        printf("The parameter for DoCallBack_MarshalStructCharSetUnicodeSequentialByRef_Cdecl is wrong\n");
        return FALSE;
    }
    return TRUE;
}

typedef BOOL (__stdcall *CharSetUnicodeSequentialByRefStdCallCaller)(CharSetUnicodeSequential* pcs);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalStructCharSetUnicodeSequentialByRef_StdCall(CharSetUnicodeSequentialByRefStdCallCaller caller)
{
    //Init
    CharSetUnicodeSequential argstr;

    WCHAR* strSource = (WCHAR*)(W("change string"));
    size_t len = TP_slen(strSource);
    LPCWSTR temp = (LPCWSTR)CoreClrAlloc(sizeof(WCHAR)*(len+1));
    if(temp != NULL)
    {
        //wcscpy((WCHAR*)temp, (len+1)*sizeof(WCHAR), strSource);
        TP_scpy_s((WCHAR*)temp,len+1,strSource);
        argstr.f1 = temp;
        argstr.f2 = 'n';
    }
    else
    {
        printf("Memory Allocated Failed!");
    }

    if(!caller(&argstr))
    {
        printf("DoCallBack_MarshalStructCharSetUnicodeSequentialByRef_StdCall:The Caller returns wrong value\n");
        return FALSE;
    }
    //Verify the value unchanged
    if(!IsCorrectCharSetUnicodeSequential(&argstr))
    {
        printf("The parameter for DoCallBack_MarshalStructCharSetUnicodeSequentialByRef_StdCall is wrong\n");
        return FALSE;
    }
    return TRUE;
}
//Delegate PInvoke,passbyref
typedef BOOL (_cdecl *CharSetUnicodeSequentialDelegatePInvokeByRefCdeclCaller)(CharSetUnicodeSequential* pcs);
extern "C" DLL_EXPORT CharSetUnicodeSequentialDelegatePInvokeByRefCdeclCaller _cdecl Get_MarshalStructCharSetUnicodeSequentialByRef_Cdecl_FuncPtr()
{
    return MarshalStructCharSetUnicodeSequentialByRef_Cdecl;
}

typedef BOOL (__stdcall *CharSetUnicodeSequentialDelegatePInvokeByRefStdCallCaller)(CharSetUnicodeSequential* pcs);
extern "C" DLL_EXPORT CharSetUnicodeSequentialDelegatePInvokeByRefStdCallCaller __stdcall Get_MarshalStructCharSetUnicodeSequentialByRef_StdCall_FuncPtr()
{
    return MarshalStructCharSetUnicodeSequentialByRef_StdCall;
}


//Passby value
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructCharSetUnicodeSequentialByVal_Cdecl(CharSetUnicodeSequential argstr)
{
    //Check the Input
    if(!IsCorrectCharSetUnicodeSequential(&argstr))
    {
        printf("\tMarshalStructCharSetUnicodeSequentialByVal_Cdecl: CharSetUnicodeSequential param not as expected\n");
        PrintCharSetUnicodeSequential(&argstr,"argstr");
        return FALSE;
    }

    WCHAR* strSource = (WCHAR*)(W("change string"));
    size_t len = TP_slen(strSource);
    LPCWSTR temp = (LPCWSTR)CoreClrAlloc(sizeof(WCHAR)*(len+1));
    if(temp != NULL)
    {
        //wcscpy((WCHAR*)temp, (len+1)*sizeof(WCHAR), strSource);
        TP_scpy_s((WCHAR*)temp,len+1,strSource);
        argstr.f1 = temp;
        argstr.f2 = 'n';
    }
    else
    {
        printf("Memory Allocated Failed!");
    }
    return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructCharSetUnicodeSequentialByVal_StdCall(CharSetUnicodeSequential argstr)
{
    //Check the Input
    if(!IsCorrectCharSetUnicodeSequential(&argstr))
    {
        printf("\tMarshalStructCharSetUnicodeSequentialByVal_StdCall: CharSetUnicodeSequential param not as expected\n");
        PrintCharSetUnicodeSequential(&argstr,"argstr");
        return FALSE;
    }

    WCHAR* strSource = (WCHAR*)(W("change string"));
    size_t len = TP_slen(strSource);
    LPCWSTR temp = (LPCWSTR)CoreClrAlloc(sizeof(WCHAR)*(len+1));
    if(temp != NULL)
    {
        //wcscpy((WCHAR*)temp, (len+1)*sizeof(WCHAR), strSource);
        TP_scpy_s((WCHAR*)temp,len+1,strSource);
        argstr.f1 = temp;
        argstr.f2 = 'n';
    }
    else
    {
        printf("Memory Allocated Failed!");
    }
    return TRUE;
}

typedef BOOL (_cdecl *CharSetUnicodeSequentialByValCdeclCaller)(CharSetUnicodeSequential cs);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalStructCharSetUnicodeSequentialByVal_Cdecl(CharSetUnicodeSequentialByValCdeclCaller caller)
{
    //Init
    CharSetUnicodeSequential argstr{};
    WCHAR* strSource = (WCHAR*)(W("change string"));
    size_t len =TP_slen(strSource);
    LPCWSTR temp = (LPCWSTR)CoreClrAlloc(sizeof(WCHAR)*(len+1));
    if(temp != NULL)
    {
        //wcscpy((WCHAR*)temp, (len+1)*sizeof(WCHAR), strSource);
        TP_scpy_s((WCHAR*)temp,len+1,strSource);
        argstr.f1 = temp;
        argstr.f2 = 'n';
    }
    else
    {
        printf("Memory Allocated Failed!");
    }

    if(!caller(argstr))
    {
        printf("DoCallBack_MarshalStructCharSetUnicodeSequentialByVal_Cdecl:The Caller returns wrong value\n");
        return FALSE;
    }

    //Verify the value unchanged
    if(0 != TP_wcmp_s(const_cast<WCHAR*>(argstr.f1), const_cast<WCHAR*>(W("change string"))) || argstr.f2 != L'n')
    {
        printf("The parameter for DoCallBack_MarshalStructCharSetUnicodeSequentialByVal_Cdecl is wrong\n");
        return FALSE;
    }
    return TRUE;
}

typedef BOOL (__stdcall *CharSetUnicodeSequentialByValStdCallCaller)(CharSetUnicodeSequential cs);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalStructCharSetUnicodeSequentialByVal_StdCall(CharSetUnicodeSequentialByValStdCallCaller caller)
{
    //Init
    CharSetUnicodeSequential argstr{};
    WCHAR* strSource = (WCHAR*)(W("change string"));
    size_t len =TP_slen(strSource);
    LPCWSTR temp = (LPCWSTR)CoreClrAlloc(sizeof(WCHAR)*(len+1));
    if(temp != NULL)
    {
        //wcscpy((WCHAR*)temp, (len+1)*sizeof(WCHAR), strSource);
        TP_scpy_s((WCHAR*)temp,len+1,strSource);
        argstr.f1 = temp;
        argstr.f2 = 'n';
    }
    else
    {
        printf("Memory Allocated Failed!");
    }

    if(!caller(argstr))
    {
        printf("DoCallBack_MarshalStructCharSetUnicodeSequentialByVal_StdCall:The Caller returns wrong value\n");
        return FALSE;
    }

    //Verify the value unchanged
    if(0 != TP_wcmp_s(const_cast<WCHAR*>(argstr.f1), const_cast<WCHAR*>(W("change string"))) || argstr.f2 != L'n')
    {
        printf("The parameter for DoCallBack_MarshalStructCharSetUnicodeSequentialByVal_StdCall is wrong\n");
        return FALSE;
    }
    return TRUE;
}
//Delegate PInvoke,passbyval
typedef BOOL (_cdecl *CharSetUnicodeSequentialDelegatePInvokeByValCdeclCaller)(CharSetUnicodeSequential cs);
extern "C" DLL_EXPORT CharSetUnicodeSequentialDelegatePInvokeByValCdeclCaller _cdecl Get_MarshalStructCharSetUnicodeSequentialByVal_Cdecl_FuncPtr()
{
    return MarshalStructCharSetUnicodeSequentialByVal_Cdecl;
}

typedef BOOL (__stdcall *CharSetUnicodeSequentialDelegatePInvokeByValStdCallCaller)(CharSetUnicodeSequential cs);
extern "C" DLL_EXPORT CharSetUnicodeSequentialDelegatePInvokeByValStdCallCaller __stdcall Get_MarshalStructCharSetUnicodeSequentialByVal_StdCall_FuncPtr()
{
    return MarshalStructCharSetUnicodeSequentialByVal_StdCall;
}


///////////////////////////////////////////Methods for NumberSequential struct////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructNumberSequentialByRef_Cdecl(NumberSequential* argstr)
{
    if(!IsCorrectNumberSequential(argstr))
    {
        printf("\tMarshalStructNumberSequentialByRef_Cdecl: NumberSequential param not as expected\n");
        PrintNumberSequential(argstr,"argstr");
        return FALSE;
    }
    ChangeNumberSequential(argstr);
    return TRUE;
}
extern "C" DLL_EXPORT BOOL __stdcall MarshalStructNumberSequentialByRef_StdCall(NumberSequential* argstr)
{
    if(!IsCorrectNumberSequential(argstr))
    {
        printf("\tMarshalStructNumberSequentialByRef_StdCall: NumberSequential param not as expected\n");
        PrintNumberSequential(argstr,"argstr");
        return FALSE;
    }
    ChangeNumberSequential(argstr);
    return TRUE;
}
typedef BOOL (_cdecl *NumberSequentialByRefCdeclCaller)(NumberSequential* pcs);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalStructNumberSequentialByRef_Cdecl(NumberSequentialByRefCdeclCaller caller)
{
    //Init
    NumberSequential argstr;

    argstr.i32 = 0;
    argstr.ui32 = 32;
    argstr.s1 = 0;
    argstr.us1 = 16;
    argstr.b = 0;
    argstr.sb = 8;
    argstr.i16 = 0;
    argstr.ui16 = 16;
    argstr.i64 = 0;
    argstr.ui64 = 64;
    argstr.sgl = 64.0;
    argstr.d = 6.4;

    if(!caller(&argstr))
    {
        printf("DoCallBack_MarshalStructNumberSequentialByRef_Cdecl:The Caller returns wrong value\n");
        return FALSE;
    }
    //Verify the value unchanged
    if(!IsCorrectNumberSequential(&argstr))
    {
        printf("The parameter for DoCallBack_MarshalStructNumberSequentialByRef_Cdecl is wrong\n");
        return FALSE;
    }
    return TRUE;
}

typedef BOOL (__stdcall *NumberSequentialByRefStdCallCaller)(NumberSequential* pcs);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalStructNumberSequentialByRef_StdCall(NumberSequentialByRefStdCallCaller caller)
{
    //Init
    NumberSequential argstr;

    argstr.i32 = 0;
    argstr.ui32 = 32;
    argstr.s1 = 0;
    argstr.us1 = 16;
    argstr.b = 0;
    argstr.sb = 8;
    argstr.i16 = 0;
    argstr.ui16 = 16;
    argstr.i64 = 0;
    argstr.ui64 = 64;
    argstr.sgl = 64.0;
    argstr.d = 6.4;

    if(!caller(&argstr))
    {
        printf("DoCallBack_MarshalStructNumberSequentialByRef_StdCall:The Caller returns wrong value\n");
        return FALSE;
    }
    //Verify the value unchanged
    if(!IsCorrectNumberSequential(&argstr))
    {
        printf("The parameter for DoCallBack_MarshalStructNumberSequentialByRef_StdCall is wrong\n");
        return FALSE;
    }
    return TRUE;
}
//Delegate PInvoke,passbyref
typedef BOOL (_cdecl *NumberSequentialDelegatePInvokeByRefCdeclCaller)(NumberSequential* pcs);
extern "C" DLL_EXPORT NumberSequentialDelegatePInvokeByRefCdeclCaller _cdecl Get_MarshalStructNumberSequentialByRef_Cdecl_FuncPtr()
{
    return MarshalStructNumberSequentialByRef_Cdecl;
}

typedef BOOL (__stdcall *NumberSequentialDelegatePInvokeByRefStdCallCaller)(NumberSequential* pcs);
extern "C" DLL_EXPORT NumberSequentialDelegatePInvokeByRefStdCallCaller __stdcall Get_MarshalStructNumberSequentialByRef_StdCall_FuncPtr()
{
    return MarshalStructNumberSequentialByRef_StdCall;
}


//Passby value
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructNumberSequentialByVal_Cdecl(NumberSequential argstr)
{
    //Check the Input
    if(!IsCorrectNumberSequential(&argstr))
    {
        printf("\tMarshalStructNumberSequentialByVal_Cdecl: NumberSequential param not as expected\n");
        PrintNumberSequential(&argstr,"argstr");
        return FALSE;
    }

    argstr.i32 = 0;
    argstr.ui32 = 32;
    argstr.s1 = 0;
    argstr.us1 = 16;
    argstr.b = 0;
    argstr.sb = 8;
    argstr.i16 = 0;
    argstr.ui16 = 16;
    argstr.i64 = 0;
    argstr.ui64 = 64;
    argstr.sgl = 64.0;
    argstr.d = 6.4;

    return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructNumberSequentialByVal_StdCall(NumberSequential argstr)
{
    //Check the Input
    if(!IsCorrectNumberSequential(&argstr))
    {
        printf("\tMarshalStructNumberSequentialByVal_StdCall: NumberSequential param not as expected\n");
        PrintNumberSequential(&argstr,"argstr");
        return FALSE;
    }

    argstr.i32 = 0;
    argstr.ui32 = 32;
    argstr.s1 = 0;
    argstr.us1 = 16;
    argstr.b = 0;
    argstr.sb = 8;
    argstr.i16 = 0;
    argstr.ui16 = 16;
    argstr.i64 = 0;
    argstr.ui64 = 64;
    argstr.sgl = 64.0;
    argstr.d = 6.4;

    return TRUE;
}

typedef NumberSequential (_cdecl *NumberSequentialByValCdeclCaller)(NumberSequential cs);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalStructNumberSequentialByVal_Cdecl(NumberSequentialByValCdeclCaller caller)
{
    //Init
    NumberSequential argstr;

    argstr.i32 = 0;
    argstr.ui32 = 32;
    argstr.s1 = 0;
    argstr.us1 = 16;
    argstr.b = 0;
    argstr.sb = 8;
    argstr.i16 = 0;
    argstr.ui16 = 16;
    argstr.i64 = 0;
    argstr.ui64 = 64;
    argstr.sgl = 64.0;
    argstr.d = 6.4;

    NumberSequential retstr = caller(argstr);

    if (!IsCorrectNumberSequential(&retstr))
    {
        printf("DoCallBack_MarshalStructNumberSequentialByVal_Cdecl:The Caller returns wrong value\n");
        PrintNumberSequential(&retstr, "retstr");
        return FALSE;
    }

    //Verify the value unchanged
    if(argstr.i32 != 0 || argstr.ui32 != 32 || argstr.s1 != 0 || argstr.us1 != 16 || argstr.b != 0 ||
        argstr.sb != 8 || argstr.i16 != 0 || argstr.ui16 != 16 || argstr.i64 != 0 || argstr.ui64 != 64 ||
        argstr.sgl != 64.0 || argstr.d != 6.4)
    {
        printf("The parameter for DoCallBack_MarshalStructNumberSequentialByVal_Cdecl is wrong\n");
        return FALSE;
    }
    return TRUE;
}

typedef NumberSequential (__stdcall *NumberSequentialByValStdCallCaller)(NumberSequential cs);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalStructNumberSequentialByVal_StdCall(NumberSequentialByValStdCallCaller caller)
{
    //Init
    NumberSequential argstr;

    argstr.i32 = 0;
    argstr.ui32 = 32;
    argstr.s1 = 0;
    argstr.us1 = 16;
    argstr.b = 0;
    argstr.sb = 8;
    argstr.i16 = 0;
    argstr.ui16 = 16;
    argstr.i64 = 0;
    argstr.ui64 = 64;
    argstr.sgl = 64.0;
    argstr.d = 6.4;

    NumberSequential retstr = caller(argstr);

    if (!IsCorrectNumberSequential(&retstr))
    {
        printf("DoCallBack_MarshalStructNumberSequentialByVal_StdCall:The Caller returns wrong value\n");
        PrintNumberSequential(&retstr, "retstr");
        return FALSE;
    }

    //Verify the value unchanged
    if(argstr.i32 != 0 || argstr.ui32 != 32 || argstr.s1 != 0 || argstr.us1 != 16 || argstr.b != 0 ||
        argstr.sb != 8 || argstr.i16 != 0 || argstr.ui16 != 16 || argstr.i64 != 0 || argstr.ui64 != 64 ||
        argstr.sgl != 64.0 || argstr.d != 6.4)
    {
        printf("The parameter for DoCallBack_MarshalStructNumberSequentialByVal_StdCall is wrong\n");
        return FALSE;
    }
    return TRUE;
}
//Delegate PInvoke,passbyval
typedef BOOL (_cdecl *NumberSequentialDelegatePInvokeByValCdeclCaller)(NumberSequential cs);
extern "C" DLL_EXPORT NumberSequentialDelegatePInvokeByValCdeclCaller _cdecl Get_MarshalStructNumberSequentialByVal_Cdecl_FuncPtr()
{
    return MarshalStructNumberSequentialByVal_Cdecl;
}

typedef BOOL (__stdcall *NumberSequentialDelegatePInvokeByValStdCallCaller)(NumberSequential cs);
extern "C" DLL_EXPORT NumberSequentialDelegatePInvokeByValStdCallCaller __stdcall Get_MarshalStructNumberSequentialByVal_StdCall_FuncPtr()
{
    return MarshalStructNumberSequentialByVal_StdCall;
}


///////////////////////////////////////////Methods for S3 struct////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructS3ByRef_Cdecl(S3* argstr)
{
    if(!IsCorrectS3(argstr))
    {
        printf("\tMarshalStructS3ByRef_Cdecl: S3 param not as expected\n");
        PrintS3(argstr,"argstr");
        return FALSE;
    }
    ChangeS3(argstr);
    return TRUE;
}
extern "C" DLL_EXPORT BOOL __stdcall MarshalStructS3ByRef_StdCall(S3* argstr)
{
    if(!IsCorrectS3(argstr))
    {
        printf("\tMarshalStructS3ByRef_StdCall: S3 param not as expected\n");
        PrintS3(argstr,"argstr");
        return FALSE;
    }
    ChangeS3(argstr);
    return TRUE;
}
typedef BOOL (_cdecl *S3ByRefCdeclCaller)(S3* pcs);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalStructS3ByRef_Cdecl(S3ByRefCdeclCaller caller)
{
    //Init
    S3 argstr;

    argstr.flag = false;
    const char* strSource = "change string";
    size_t len =strlen(strSource) + 1;
    LPCSTR temp = (LPCSTR)CoreClrAlloc(sizeof(char)*len);
    if(temp != NULL)
    {
        strcpy_s((char*)temp,len,strSource);
        argstr.str = temp;
    }
    for(int i = 1;i<257;i++)
    {
        argstr.vals[i-1] = i;
    }

    if(!caller(&argstr))
    {
        printf("DoCallBack_MarshalStructS3ByRef_Cdecl:The Caller returns wrong value\n");
        return FALSE;
    }
    //Verify the value unchanged
    if(!IsCorrectS3(&argstr))
    {
        printf("The parameter for DoCallBack_MarshalStructS3ByRef_Cdecl is wrong\n");
        return FALSE;
    }
    return TRUE;
}

typedef BOOL (__stdcall *S3ByRefStdCallCaller)(S3* pcs);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalStructS3ByRef_StdCall(S3ByRefStdCallCaller caller)
{
    //Init
    S3 argstr;

    argstr.flag = false;
    const char* strSource = "change string";
    size_t len =strlen(strSource) + 1;
    LPCSTR temp = (LPCSTR)CoreClrAlloc(sizeof(char)*len);
    if(temp != NULL)
    {
        strcpy_s((char*)temp,len,strSource);
        argstr.str = temp;
    }
    for(int i = 1;i<257;i++)
    {
        argstr.vals[i-1] = i;
    }

    if(!caller(&argstr))
    {
        printf("DoCallBack_MarshalStructS3ByRef_StdCall:The Caller returns wrong value\n");
        return FALSE;
    }
    //Verify the value unchanged
    if(!IsCorrectS3(&argstr))
    {
        printf("The parameter for DoCallBack_MarshalStructS3ByRef_StdCall is wrong\n");
        return FALSE;
    }
    return TRUE;
}
//Delegate PInvoke,passbyref
typedef BOOL (_cdecl *S3DelegatePInvokeByRefCdeclCaller)(S3* pcs);
extern "C" DLL_EXPORT S3DelegatePInvokeByRefCdeclCaller _cdecl Get_MarshalStructS3ByRef_Cdecl_FuncPtr()
{
    return MarshalStructS3ByRef_Cdecl;
}

typedef BOOL (__stdcall *S3DelegatePInvokeByRefStdCallCaller)(S3* pcs);
extern "C" DLL_EXPORT S3DelegatePInvokeByRefStdCallCaller __stdcall Get_MarshalStructS3ByRef_StdCall_FuncPtr()
{
    return MarshalStructS3ByRef_StdCall;
}


//Passby value
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructS3ByVal_Cdecl(S3 argstr)
{
    //Check the Input
    if(!IsCorrectS3(&argstr))
    {
        printf("\tMarshalStructS3ByVal_Cdecl: S3 param not as expected\n");
        PrintS3(&argstr,"argstr");
        return FALSE;
    }

    argstr.flag = false;
    const char* strSource = "change string";
    size_t len =strlen(strSource) + 1;
    LPCSTR temp = (LPCSTR)CoreClrAlloc(sizeof(char)*len);
    if(temp != NULL)
    {
        strcpy_s((char*)temp,len,strSource);
        argstr.str = temp;
    }
    for(int i = 1;i<257;i++)
    {
        argstr.vals[i-1] = i;
    }
    return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructS3ByVal_StdCall(S3 argstr)
{
    //Check the Input
    if(!IsCorrectS3(&argstr))
    {
        printf("\tMarshalStructS3ByVal_StdCall: S3 param not as expected\n");
        PrintS3(&argstr,"argstr");
        return FALSE;
    }

    argstr.flag = false;
    const char* strSource = "change string";
    size_t len =strlen(strSource) + 1;
    LPCSTR temp = (LPCSTR)CoreClrAlloc(sizeof(char)*len);
    if(temp != NULL)
    {
        strcpy_s((char*)temp,len,strSource);
        argstr.str = temp;
    }
    for(int i = 1;i<257;i++)
    {
        argstr.vals[i-1] = i;
    }
    return TRUE;
}

typedef BOOL (_cdecl *S3ByValCdeclCaller)(S3 cs);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalStructS3ByVal_Cdecl(S3ByValCdeclCaller caller)
{
    //Init
    S3 argstr;

    argstr.flag = false;
    const char* strSource = "change string";
    size_t len =strlen(strSource) + 1;
    LPCSTR temp = (LPCSTR)CoreClrAlloc(sizeof(char)*len);
    if(temp != NULL)
    {
        strcpy_s((char*)temp,len,strSource);
        argstr.str = temp;
    }
    for(int i = 1;i<257;i++)
    {
        argstr.vals[i-1] = i;
    }

    if(!caller(argstr))
    {
        printf("DoCallBack_MarshalStructS3ByVal_Cdecl:The Caller returns wrong value\n");
        return FALSE;
    }

    //Verify the value unchanged
    int iflag = 0;
    if(argstr.flag || strcmp((char*)argstr.str,"change string") != 0)
        return false;
    for (int i = 1; i < 257; i++)
    {
        if (argstr.vals[i-1] != i)
        {
            printf("\tThe Index of %i is not expected",i);
            iflag++;
        }
    }
    if (iflag != 0)
    {
        return false;
    }

    return TRUE;
}

typedef BOOL (__stdcall *S3ByValStdCallCaller)(S3 cs);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalStructS3ByVal_StdCall(S3ByValStdCallCaller caller)
{
    //Init
    S3 argstr;

    argstr.flag = false;
    const char* strSource = "change string";
    size_t len =strlen(strSource) + 1;
    LPCSTR temp = (LPCSTR)CoreClrAlloc(sizeof(char)*len);
    if(temp != NULL)
    {
        strcpy_s((char*)temp,len,strSource);
        argstr.str = temp;
    }
    for(int i = 1;i<257;i++)
    {
        argstr.vals[i-1] = i;
    }

    if(!caller(argstr))
    {
        printf("DoCallBack_MarshalStructS3ByVal_StdCall:The Caller returns wrong value\n");
        return FALSE;
    }

    //Verify the value unchanged
    int iflag = 0;
    if(argstr.flag || strcmp((char*)argstr.str,"change string") != 0)
        return false;
    for (int i = 1; i < 257; i++)
    {
        if (argstr.vals[i-1] != i)
        {
            printf("\tThe Index of %i is not expected",i);
            iflag++;
        }
    }
    if (iflag != 0)
    {
        return false;
    }
    return TRUE;
}
//Delegate PInvoke,passbyval
typedef BOOL (_cdecl *S3DelegatePInvokeByValCdeclCaller)(S3 cs);
extern "C" DLL_EXPORT S3DelegatePInvokeByValCdeclCaller _cdecl Get_MarshalStructS3ByVal_Cdecl_FuncPtr()
{
    return MarshalStructS3ByVal_Cdecl;
}

typedef BOOL (__stdcall *S3DelegatePInvokeByValStdCallCaller)(S3 cs);
extern "C" DLL_EXPORT S3DelegatePInvokeByValStdCallCaller __stdcall Get_MarshalStructS3ByVal_StdCall_FuncPtr()
{
    return MarshalStructS3ByVal_StdCall;
}



///////////////////////////////////////////Methods for S5 struct////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructS5ByRef_Cdecl(S5* argstr)
{
    if(!IsCorrectS5(argstr))
    {
        printf("\tMarshalStructS5ByRef_Cdecl: S5 param not as expected\n");
        PrintS5(argstr,"argstr");
        return FALSE;
    }
    ChangeS5(argstr);
    return TRUE;
}
extern "C" DLL_EXPORT BOOL __stdcall MarshalStructS5ByRef_StdCall(S5* argstr)
{
    if(!IsCorrectS5(argstr))
    {
        printf("\tMarshalStructS5ByRef_StdCall: S5 param not as expected\n");
        PrintS5(argstr,"argstr");
        return FALSE;
    }
    ChangeS5(argstr);
    return TRUE;
}
typedef BOOL (_cdecl *S5ByRefCdeclCaller)(S5* pcs);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalStructS5ByRef_Cdecl(S5ByRefCdeclCaller caller)
{
    //Init
    S5 argstr;

    Enum1 eInstance = e2;
    const char* strSource = "change string";
    size_t len =strlen(strSource) + 1;
    LPCSTR temp = (LPCSTR)CoreClrAlloc(sizeof(char)*len);
    if(temp != NULL)
    {
        strcpy_s((char*)temp,len,strSource);
        argstr.s4.name = temp;
    }
    argstr.s4.age = 64;
    argstr.ef = eInstance;

    if(!caller(&argstr))
    {
        printf("DoCallBack_MarshalStructS5ByRef_Cdecl:The Caller returns wrong value\n");
        return FALSE;
    }
    //Verify the value unchanged
    if(!IsCorrectS5(&argstr))
    {
        printf("The parameter for DoCallBack_MarshalStructS5ByRef_Cdecl is wrong\n");
        return FALSE;
    }
    return TRUE;
}

typedef BOOL (__stdcall *S5ByRefStdCallCaller)(S5* pcs);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalStructS5ByRef_StdCall(S5ByRefStdCallCaller caller)
{
    //Init
    S5 argstr;

    Enum1 eInstance = e2;
    const char* strSource = "change string";
    size_t len =strlen(strSource) + 1;
    LPCSTR temp = (LPCSTR)CoreClrAlloc(sizeof(char)*len);
    if(temp != NULL)
    {
        strcpy_s((char*)temp,len,strSource);
        argstr.s4.name = temp;
    }
    argstr.s4.age = 64;
    argstr.ef = eInstance;

    if(!caller(&argstr))
    {
        printf("DoCallBack_MarshalStructS5ByRef_StdCall:The Caller returns wrong value\n");
        return FALSE;
    }
    //Verify the value unchanged
    if(!IsCorrectS5(&argstr))
    {
        printf("The parameter for DoCallBack_MarshalStructS5ByRef_StdCall is wrong\n");
        return FALSE;
    }
    return TRUE;
}
//Delegate PInvoke,passbyref
typedef BOOL (_cdecl *S5DelegatePInvokeByRefCdeclCaller)(S5* pcs);
extern "C" DLL_EXPORT S5DelegatePInvokeByRefCdeclCaller _cdecl Get_MarshalStructS5ByRef_Cdecl_FuncPtr()
{
    return MarshalStructS5ByRef_Cdecl;
}

typedef BOOL (__stdcall *S5DelegatePInvokeByRefStdCallCaller)(S5* pcs);
extern "C" DLL_EXPORT S5DelegatePInvokeByRefStdCallCaller __stdcall Get_MarshalStructS5ByRef_StdCall_FuncPtr()
{
    return MarshalStructS5ByRef_StdCall;
}


//Passby value
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructS5ByVal_Cdecl(S5 argstr)
{
    //Check the Input
    if(!IsCorrectS5(&argstr))
    {
        printf("\tMarshalStructS5ByVal_Cdecl: S5 param not as expected\n");
        PrintS5(&argstr,"argstr");
        return FALSE;
    }

    Enum1 eInstance = e2;
    const char* strSource = "change string";
    size_t len =strlen(strSource) + 1;
    LPCSTR temp = (LPCSTR)CoreClrAlloc(sizeof(char)*len);
    if(temp != NULL)
    {
        strcpy_s((char*)temp,len,strSource);
        argstr.s4.name = temp;
    }
    argstr.s4.age = 64;
    argstr.ef = eInstance;

    return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructS5ByVal_StdCall(S5 argstr)
{
    //Check the Input
    if(!IsCorrectS5(&argstr))
    {
        printf("\tMarshalStructS5ByVal_StdCall: S5 param not as expected\n");
        PrintS5(&argstr,"argstr");
        return FALSE;
    }

    Enum1 eInstance = e2;
    const char* strSource = "change string";
    size_t len =strlen(strSource) + 1;
    LPCSTR temp = (LPCSTR)CoreClrAlloc(sizeof(char)*len);
    if(temp != NULL)
    {
        strcpy_s((char*)temp,len,strSource);
        argstr.s4.name = temp;
    }
    argstr.s4.age = 64;
    argstr.ef = eInstance;
    return TRUE;
}

typedef BOOL (_cdecl *S5ByValCdeclCaller)(S5 cs);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalStructS5ByVal_Cdecl(S5ByValCdeclCaller caller)
{
    //Init
    S5 argstr{};

    Enum1 eInstance = e2;
    const char* strSource = "change string";
    size_t len =strlen(strSource) + 1;
    LPCSTR temp = (LPCSTR)CoreClrAlloc(sizeof(char)*len);
    if(temp != NULL)
    {
        strcpy_s((char*)temp,len,strSource);
        argstr.s4.name = temp;
    }
    argstr.s4.age = 64;
    argstr.ef = eInstance;

    if(!caller(argstr))
    {
        printf("DoCallBack_MarshalStructS5ByVal_Cdecl:The Caller returns wrong value\n");
        return FALSE;
    }

    //Verify the value unchanged
    if(argstr.s4.age != 64 || strcmp((char*)argstr.s4.name,"change string") != 0)
        return false;
    if(argstr.ef != eInstance)
    {
        return false;
    }
    return TRUE;
}

typedef BOOL (__stdcall *S5ByValStdCallCaller)(S5 cs);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalStructS5ByVal_StdCall(S5ByValStdCallCaller caller)
{
    //Init
    S5 argstr{};

    Enum1 eInstance = e2;
    const char* strSource = "change string";
    size_t len =strlen(strSource) + 1;
    LPCSTR temp = (LPCSTR)CoreClrAlloc(sizeof(char)*len);
    if(temp != NULL)
    {
        strcpy_s((char*)temp,len,strSource);
        argstr.s4.name = temp;
    }
    argstr.s4.age = 64;
    argstr.ef = eInstance;

    if(!caller(argstr))
    {
        printf("DoCallBack_MarshalStructS5ByVal_StdCall:The Caller returns wrong value\n");
        return FALSE;
    }

    //Verify the value unchanged
    if(argstr.s4.age != 64 || strcmp((char*)argstr.s4.name,"change string") != 0)
        return false;
    if(argstr.ef != eInstance)
    {
        return false;
    }
    return TRUE;
}
//Delegate PInvoke,passbyval
typedef BOOL (_cdecl *S5DelegatePInvokeByValCdeclCaller)(S5 cs);
extern "C" DLL_EXPORT S5DelegatePInvokeByValCdeclCaller _cdecl Get_MarshalStructS5ByVal_Cdecl_FuncPtr()
{
    return MarshalStructS5ByVal_Cdecl;
}

typedef BOOL (__stdcall *S5DelegatePInvokeByValStdCallCaller)(S5 cs);
extern "C" DLL_EXPORT S5DelegatePInvokeByValStdCallCaller __stdcall Get_MarshalStructS5ByVal_StdCall_FuncPtr()
{
    return MarshalStructS5ByVal_StdCall;
}


///////////////////////////////////////////Methods for StringStructSequentialAnsi struct////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructStringStructSequentialAnsiByRef_Cdecl(StringStructSequentialAnsi* argstr)
{
    if(!IsCorrectStringStructSequentialAnsi(argstr))
    {
        printf("\tMarshalStructStringStructSequentialAnsiByRef_Cdecl: StringStructSequentialAnsi param not as expected\n");
        PrintStringStructSequentialAnsi(argstr,"argstr");
        return FALSE;
    }
    ChangeStringStructSequentialAnsi(argstr);
    return TRUE;
}
extern "C" DLL_EXPORT BOOL __stdcall MarshalStructStringStructSequentialAnsiByRef_StdCall(StringStructSequentialAnsi* argstr)
{
    if(!IsCorrectStringStructSequentialAnsi(argstr))
    {
        printf("\tMarshalStructStringStructSequentialAnsiByRef_StdCall: StringStructSequentialAnsi param not as expected\n");
        PrintStringStructSequentialAnsi(argstr,"argstr");
        return FALSE;
    }
    ChangeStringStructSequentialAnsi(argstr);
    return TRUE;
}
typedef BOOL (_cdecl *StringStructSequentialAnsiByRefCdeclCaller)(StringStructSequentialAnsi* pcs);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalStructStringStructSequentialAnsiByRef_Cdecl(StringStructSequentialAnsiByRefCdeclCaller caller)
{
    //Init
    StringStructSequentialAnsi argstr;

    char* newFirst = (char*)CoreClrAlloc(sizeof(char)*513);
    char* newLast = (char*)CoreClrAlloc(sizeof(char)*513);
    for (int i = 0; i < 512; ++i)
    {
        newFirst[i] = 'b';
        newLast[i] = 'a';
    }
    newFirst[512] = '\0';
    newLast[512] = '\0';
    argstr.first = newFirst;
    argstr.last = newLast;

    if(!caller(&argstr))
    {
        printf("DoCallBack_MarshalStructStringStructSequentialAnsiByRef_Cdecl:The Caller returns wrong value\n");
        return FALSE;
    }
    //Verify the value unchanged
    if(!IsCorrectStringStructSequentialAnsi(&argstr))
    {
        printf("The parameter for DoCallBack_MarshalStructStringStructSequentialAnsiByRef_Cdecl is wrong\n");
        return FALSE;
    }
    return TRUE;
}

typedef BOOL (__stdcall *StringStructSequentialAnsiByRefStdCallCaller)(StringStructSequentialAnsi* pcs);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalStructStringStructSequentialAnsiByRef_StdCall(StringStructSequentialAnsiByRefStdCallCaller caller)
{
    //Init
    StringStructSequentialAnsi argstr;

    char* newFirst = (char*)CoreClrAlloc(sizeof(char)*513);
    char* newLast = (char*)CoreClrAlloc(sizeof(char)*513);
    for (int i = 0; i < 512; ++i)
    {
        newFirst[i] = 'b';
        newLast[i] = 'a';
    }
    newFirst[512] = '\0';
    newLast[512] = '\0';
    argstr.first = newFirst;
    argstr.last = newLast;

    if(!caller(&argstr))
    {
        printf("DoCallBack_MarshalStructStringStructSequentialAnsiByRef_StdCall:The Caller returns wrong value\n");
        return FALSE;
    }
    //Verify the value unchanged
    if(!IsCorrectStringStructSequentialAnsi(&argstr))
    {
        printf("The parameter for DoCallBack_MarshalStructStringStructSequentialAnsiByRef_StdCall is wrong\n");
        return FALSE;
    }
    return TRUE;
}
//Delegate PInvoke,passbyref
typedef BOOL (_cdecl *StringStructSequentialAnsiDelegatePInvokeByRefCdeclCaller)(StringStructSequentialAnsi* pcs);
extern "C" DLL_EXPORT StringStructSequentialAnsiDelegatePInvokeByRefCdeclCaller _cdecl Get_MarshalStructStringStructSequentialAnsiByRef_Cdecl_FuncPtr()
{
    return MarshalStructStringStructSequentialAnsiByRef_Cdecl;
}

typedef BOOL (__stdcall *StringStructSequentialAnsiDelegatePInvokeByRefStdCallCaller)(StringStructSequentialAnsi* pcs);
extern "C" DLL_EXPORT StringStructSequentialAnsiDelegatePInvokeByRefStdCallCaller __stdcall Get_MarshalStructStringStructSequentialAnsiByRef_StdCall_FuncPtr()
{
    return MarshalStructStringStructSequentialAnsiByRef_StdCall;
}


//Passby value
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructStringStructSequentialAnsiByVal_Cdecl(StringStructSequentialAnsi argstr)
{
    //Check the Input
    if(!IsCorrectStringStructSequentialAnsi(&argstr))
    {
        printf("\tMarshalStructStringStructSequentialAnsiByVal_Cdecl: StringStructSequentialAnsi param not as expected\n");
        PrintStringStructSequentialAnsi(&argstr,"argstr");
        return FALSE;
    }

    char* newFirst = (char*)CoreClrAlloc(sizeof(char)*513);
    char* newLast = (char*)CoreClrAlloc(sizeof(char)*513);
    for (int i = 0; i < 512; ++i)
    {
        newFirst[i] = 'b';
        newLast[i] = 'a';
    }
    newFirst[512] = '\0';
    newLast[512] = '\0';
    argstr.first = newFirst;
    argstr.last = newLast;

    return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructStringStructSequentialAnsiByVal_StdCall(StringStructSequentialAnsi argstr)
{
    //Check the Input
    if(!IsCorrectStringStructSequentialAnsi(&argstr))
    {
        printf("\tMarshalStructStringStructSequentialAnsiByVal_StdCall: StringStructSequentialAnsi param not as expected\n");
        PrintStringStructSequentialAnsi(&argstr,"argstr");
        return FALSE;
    }

    char* newFirst = (char*)CoreClrAlloc(sizeof(char)*513);
    char* newLast = (char*)CoreClrAlloc(sizeof(char)*513);
    for (int i = 0; i < 512; ++i)
    {
        newFirst[i] = 'b';
        newLast[i] = 'a';
    }
    newFirst[512] = '\0';
    newLast[512] = '\0';
    argstr.first = newFirst;
    argstr.last = newLast;

    return TRUE;
}

typedef BOOL (_cdecl *StringStructSequentialAnsiByValCdeclCaller)(StringStructSequentialAnsi cs);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalStructStringStructSequentialAnsiByVal_Cdecl(StringStructSequentialAnsiByValCdeclCaller caller)
{
    //Init
    StringStructSequentialAnsi argstr;
    char* newFirst = (char*)CoreClrAlloc(sizeof(char)*513);
    char* newLast = (char*)CoreClrAlloc(sizeof(char)*513);
    for (int i = 0; i < 512; ++i)
    {
        newFirst[i] = 'b';
        newLast[i] = 'a';
    }
    newFirst[512] = '\0';
    newLast[512] = '\0';
    argstr.first = newFirst;
    argstr.last = newLast;

    if(!caller(argstr))
    {
        printf("DoCallBack_MarshalStructStringStructSequentialAnsiByVal_Cdecl:The Caller returns wrong value\n");
        return FALSE;
    }

    //Verify the value unchanged
    if(memcmp(argstr.first,newFirst,512)!= 0)
        return false;
    if(memcmp(argstr.last,newLast,512)!= 0)
        return false;

    return TRUE;
}

typedef BOOL (__stdcall *StringStructSequentialAnsiByValStdCallCaller)(StringStructSequentialAnsi cs);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalStructStringStructSequentialAnsiByVal_StdCall(StringStructSequentialAnsiByValStdCallCaller caller)
{
    //Init
    StringStructSequentialAnsi argstr;
    char* newFirst = (char*)CoreClrAlloc(sizeof(char)*513);
    char* newLast = (char*)CoreClrAlloc(sizeof(char)*513);
    for (int i = 0; i < 512; ++i)
    {
        newFirst[i] = 'b';
        newLast[i] = 'a';
    }
    newFirst[512] = '\0';
    newLast[512] = '\0';
    argstr.first = newFirst;
    argstr.last = newLast;

    if(!caller(argstr))
    {
        printf("DoCallBack_MarshalStructStringStructSequentialAnsiByVal_StdCall:The Caller returns wrong value\n");
        return FALSE;
    }

    //Verify the value unchanged
    if(memcmp(argstr.first,newFirst,512)!= 0)
        return false;
    if(memcmp(argstr.last,newLast,512)!= 0)
        return false;

    return TRUE;
}
//Delegate PInvoke,passbyval
typedef BOOL (_cdecl *StringStructSequentialAnsiDelegatePInvokeByValCdeclCaller)(StringStructSequentialAnsi cs);
extern "C" DLL_EXPORT StringStructSequentialAnsiDelegatePInvokeByValCdeclCaller _cdecl Get_MarshalStructStringStructSequentialAnsiByVal_Cdecl_FuncPtr()
{
    return MarshalStructStringStructSequentialAnsiByVal_Cdecl;
}

typedef BOOL (__stdcall *StringStructSequentialAnsiDelegatePInvokeByValStdCallCaller)(StringStructSequentialAnsi cs);
extern "C" DLL_EXPORT StringStructSequentialAnsiDelegatePInvokeByValStdCallCaller __stdcall Get_MarshalStructStringStructSequentialAnsiByVal_StdCall_FuncPtr()
{
    return MarshalStructStringStructSequentialAnsiByVal_StdCall;
}




///////////////////////////////////////////Methods for StringStructSequentialUnicode struct////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructStringStructSequentialUnicodeByRef_Cdecl(StringStructSequentialUnicode* argstr)
{
    if(!IsCorrectStringStructSequentialUnicode(argstr))
    {
        printf("\tMarshalStructStringStructSequentialUnicodeByRef_Cdecl: StringStructSequentialUnicode param not as expected\n");
        PrintStringStructSequentialUnicode(argstr,"argstr");
        return FALSE;
    }
    ChangeStringStructSequentialUnicode(argstr);
    return TRUE;
}
extern "C" DLL_EXPORT BOOL __stdcall MarshalStructStringStructSequentialUnicodeByRef_StdCall(StringStructSequentialUnicode* argstr)
{
    if(!IsCorrectStringStructSequentialUnicode(argstr))
    {
        printf("\tMarshalStructStringStructSequentialUnicodeByRef_StdCall: StringStructSequentialUnicode param not as expected\n");
        PrintStringStructSequentialUnicode(argstr,"argstr");
        return FALSE;
    }
    ChangeStringStructSequentialUnicode(argstr);
    return TRUE;
}
typedef BOOL (_cdecl *StringStructSequentialUnicodeByRefCdeclCaller)(StringStructSequentialUnicode* pcs);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalStructStringStructSequentialUnicodeByRef_Cdecl(StringStructSequentialUnicodeByRefCdeclCaller caller)
{
    //Init
    StringStructSequentialUnicode argstr;

    WCHAR* newFirst = (WCHAR*)CoreClrAlloc(sizeof(WCHAR)*257);
    WCHAR* newLast = (WCHAR*)CoreClrAlloc(sizeof(WCHAR)*257);
    for (int i = 0; i < 256; ++i)
    {
        newFirst[i] = L'b';
        newLast[i] = L'a';
    }
    newFirst[256] = L'\0';
    newLast[256] = L'\0';
    argstr.first = (const WCHAR*)newFirst;
    argstr.last = (const WCHAR*)newLast;

    if(!caller(&argstr))
    {
        printf("DoCallBack_MarshalStructStringStructSequentialUnicodeByRef_Cdecl:The Caller returns wrong value\n");
        return FALSE;
    }
    //Verify the value unchanged
    if(!IsCorrectStringStructSequentialUnicode(&argstr))
    {
        printf("The parameter for DoCallBack_MarshalStructStringStructSequentialUnicodeByRef_Cdecl is wrong\n");
        return FALSE;
    }
    return TRUE;
}

typedef BOOL (__stdcall *StringStructSequentialUnicodeByRefStdCallCaller)(StringStructSequentialUnicode* pcs);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalStructStringStructSequentialUnicodeByRef_StdCall(StringStructSequentialUnicodeByRefStdCallCaller caller)
{
    //Init
    StringStructSequentialUnicode argstr;

    WCHAR* newFirst = (WCHAR*)CoreClrAlloc(sizeof(WCHAR)*257);
    WCHAR* newLast = (WCHAR*)CoreClrAlloc(sizeof(WCHAR)*257);
    for (int i = 0; i < 256; ++i)
    {
        newFirst[i] = L'b';
        newLast[i] = L'a';
    }
    newFirst[256] = L'\0';
    newLast[256] = L'\0';
    argstr.first = (const WCHAR*)newFirst;
    argstr.last = (const WCHAR*)newLast;

    if(!caller(&argstr))
    {
        printf("DoCallBack_MarshalStructStringStructSequentialUnicodeByRef_StdCall:The Caller returns wrong value\n");
        return FALSE;
    }
    //Verify the value unchanged
    if(!IsCorrectStringStructSequentialUnicode(&argstr))
    {
        printf("The parameter for DoCallBack_MarshalStructStringStructSequentialUnicodeByRef_StdCall is wrong\n");
        return FALSE;
    }
    return TRUE;
}
//Delegate PInvoke,passbyref
typedef BOOL (_cdecl *StringStructSequentialUnicodeDelegatePInvokeByRefCdeclCaller)(StringStructSequentialUnicode* pcs);
extern "C" DLL_EXPORT StringStructSequentialUnicodeDelegatePInvokeByRefCdeclCaller _cdecl Get_MarshalStructStringStructSequentialUnicodeByRef_Cdecl_FuncPtr()
{
    return MarshalStructStringStructSequentialUnicodeByRef_Cdecl;
}

typedef BOOL (__stdcall *StringStructSequentialUnicodeDelegatePInvokeByRefStdCallCaller)(StringStructSequentialUnicode* pcs);
extern "C" DLL_EXPORT StringStructSequentialUnicodeDelegatePInvokeByRefStdCallCaller __stdcall Get_MarshalStructStringStructSequentialUnicodeByRef_StdCall_FuncPtr()
{
    return MarshalStructStringStructSequentialUnicodeByRef_StdCall;
}


//Passby value
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructStringStructSequentialUnicodeByVal_Cdecl(StringStructSequentialUnicode argstr)
{
    //Check the Input
    if(!IsCorrectStringStructSequentialUnicode(&argstr))
    {
        printf("\tMarshalStructStringStructSequentialUnicodeByVal_Cdecl: StringStructSequentialUnicode param not as expected\n");
        PrintStringStructSequentialUnicode(&argstr,"argstr");
        return FALSE;
    }

    WCHAR* newFirst = (WCHAR*)CoreClrAlloc(sizeof(WCHAR)*257);
    WCHAR* newLast = (WCHAR*)CoreClrAlloc(sizeof(WCHAR)*257);
    for (int i = 0; i < 256; ++i)
    {
        newFirst[i] = L'b';
        newLast[i] = L'a';
    }
    newFirst[256] = L'\0';
    newLast[256] = L'\0';
    argstr.first = (const WCHAR*)newFirst;
    argstr.last = (const WCHAR*)newLast;

    return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructStringStructSequentialUnicodeByVal_StdCall(StringStructSequentialUnicode argstr)
{
    //Check the Input
    if(!IsCorrectStringStructSequentialUnicode(&argstr))
    {
        printf("\tMarshalStructStringStructSequentialUnicodeByVal_StdCall: StringStructSequentialUnicode param not as expected\n");
        PrintStringStructSequentialUnicode(&argstr,"argstr");
        return FALSE;
    }

    WCHAR* newFirst = (WCHAR*)CoreClrAlloc(sizeof(WCHAR)*257);
    WCHAR* newLast = (WCHAR*)CoreClrAlloc(sizeof(WCHAR)*257);
    for (int i = 0; i < 256; ++i)
    {
        newFirst[i] = L'b';
        newLast[i] = L'a';
    }
    newFirst[256] = L'\0';
    newLast[256] = L'\0';
    argstr.first = (const WCHAR*)newFirst;
    argstr.last = (const WCHAR*)newLast;

    return TRUE;
}

typedef BOOL (_cdecl *StringStructSequentialUnicodeByValCdeclCaller)(StringStructSequentialUnicode cs);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalStructStringStructSequentialUnicodeByVal_Cdecl(StringStructSequentialUnicodeByValCdeclCaller caller)
{
    //Init
    StringStructSequentialUnicode argstr;

    WCHAR* newFirst = (WCHAR*)CoreClrAlloc(sizeof(WCHAR)*257);
    WCHAR* newLast = (WCHAR*)CoreClrAlloc(sizeof(WCHAR)*257);
    for (int i = 0; i < 256; ++i)
    {
        newFirst[i] = L'b';
        newLast[i] = L'a';
    }
    newFirst[256] = L'\0';
    newLast[256] = L'\0';
    argstr.first = (const WCHAR*)newFirst;
    argstr.last = (const WCHAR*)newLast;

    if(!caller(argstr))
    {
        printf("DoCallBack_MarshalStructStringStructSequentialUnicodeByVal_Cdecl:The Caller returns wrong value\n");
        return FALSE;
    }

    //Verify the value unchanged
    if(memcmp(argstr.first,newFirst,256*sizeof(WCHAR)) != 0)
        return false;
    if(memcmp(argstr.last,newLast,256*sizeof(WCHAR)) != 0)
        return false;

    return TRUE;
}

typedef BOOL (__stdcall *StringStructSequentialUnicodeByValStdCallCaller)(StringStructSequentialUnicode cs);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalStructStringStructSequentialUnicodeByVal_StdCall(StringStructSequentialUnicodeByValStdCallCaller caller)
{
    //Init
    StringStructSequentialUnicode argstr;

    WCHAR* newFirst = (WCHAR*)CoreClrAlloc(sizeof(WCHAR)*257);
    WCHAR* newLast = (WCHAR*)CoreClrAlloc(sizeof(WCHAR)*257);
    for (int i = 0; i < 256; ++i)
    {
        newFirst[i] = L'b';
        newLast[i] = L'a';
    }
    newFirst[256] = L'\0';
    newLast[256] = L'\0';
    argstr.first = (const WCHAR*)newFirst;
    argstr.last = (const WCHAR*)newLast;

    if(!caller(argstr))
    {
        printf("DoCallBack_MarshalStructStringStructSequentialUnicodeByVal_StdCall:The Caller returns wrong value\n");
        return FALSE;
    }

    //Verify the value unchanged
    if(memcmp(argstr.first,newFirst,256*sizeof(WCHAR)) != 0)
        return false;
    if(memcmp(argstr.last,newLast,256*sizeof(WCHAR)) != 0)
        return false;

    return TRUE;
}
//Delegate PInvoke,passbyval
typedef BOOL (_cdecl *StringStructSequentialUnicodeDelegatePInvokeByValCdeclCaller)(StringStructSequentialUnicode cs);
extern "C" DLL_EXPORT StringStructSequentialUnicodeDelegatePInvokeByValCdeclCaller _cdecl Get_MarshalStructStringStructSequentialUnicodeByVal_Cdecl_FuncPtr()
{
    return MarshalStructStringStructSequentialUnicodeByVal_Cdecl;
}

typedef BOOL (__stdcall *StringStructSequentialUnicodeDelegatePInvokeByValStdCallCaller)(StringStructSequentialUnicode cs);
extern "C" DLL_EXPORT StringStructSequentialUnicodeDelegatePInvokeByValStdCallCaller __stdcall Get_MarshalStructStringStructSequentialUnicodeByVal_StdCall_FuncPtr()
{
    return MarshalStructStringStructSequentialUnicodeByVal_StdCall;
}




///////////////////////////////////////////Methods for S8 struct////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructS8ByRef_Cdecl(S8* argstr)
{
    if(!IsCorrectS8(argstr))
    {
        printf("\tMarshalStructS8ByRef_Cdecl: S8 param not as expected\n");
        PrintS8(argstr,"argstr");
        return FALSE;
    }
    ChangeS8(argstr);
    return TRUE;
}
extern "C" DLL_EXPORT BOOL __stdcall MarshalStructS8ByRef_StdCall(S8* argstr)
{
    if(!IsCorrectS8(argstr))
    {
        printf("\tMarshalStructS8ByRef_StdCall: S8 param not as expected\n");
        PrintS8(argstr,"argstr");
        return FALSE;
    }
    ChangeS8(argstr);
    return TRUE;
}
typedef BOOL (_cdecl *S8ByRefCdeclCaller)(S8* pcs);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalStructS8ByRef_Cdecl(S8ByRefCdeclCaller caller)
{
    //Init
    S8 argstr;

    const char* lpstr = "world";
    size_t size = sizeof(char) * (strlen(lpstr) + 1);
    LPSTR temp = (LPSTR)CoreClrAlloc( size );
    memset(temp, 0, size);
    if(temp)
    {
        strcpy_s((char*)temp,size,lpstr);
        argstr.name = temp;
    }
    else
    {
        printf("Memory Allocated Failed!");
    }
    argstr.gender = false;
    argstr.jobNum = 1;
    argstr.i32 = 256;
    argstr.ui32 = 256;
    argstr.mySByte = 64;

    if(!caller(&argstr))
    {
        printf("DoCallBack_MarshalStructS8ByRef_Cdecl:The Caller returns wrong value\n");
        return FALSE;
    }
    //Verify the value unchanged
    if(!IsCorrectS8(&argstr))
    {
        printf("The parameter for DoCallBack_MarshalStructS8ByRef_Cdecl is wrong\n");
        return FALSE;
    }
    return TRUE;
}

typedef BOOL (__stdcall *S8ByRefStdCallCaller)(S8* pcs);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalStructS8ByRef_StdCall(S8ByRefStdCallCaller caller)
{
    //Init
    S8 argstr;

    const char* lpstr = "world";
    size_t size = sizeof(char) * (strlen(lpstr) + 1);
    LPSTR temp = (LPSTR)CoreClrAlloc( size );
    memset(temp, 0, size);
    if(temp)
    {
        strcpy_s((char*)temp,size,lpstr);
        argstr.name = temp;
    }
    else
    {
        printf("Memory Allocated Failed!");
    }
    argstr.gender = false;
    argstr.jobNum = 1;
    argstr.i32 = 256;
    argstr.ui32 = 256;
    argstr.mySByte = 64;

    if(!caller(&argstr))
    {
        printf("DoCallBack_MarshalStructS8ByRef_StdCall:The Caller returns wrong value\n");
        return FALSE;
    }
    //Verify the value unchanged
    if(!IsCorrectS8(&argstr))
    {
        printf("The parameter for DoCallBack_MarshalStructS8ByRef_StdCall is wrong\n");
        return FALSE;
    }
    return TRUE;
}
//Delegate PInvoke,passbyref
typedef BOOL (_cdecl *S8DelegatePInvokeByRefCdeclCaller)(S8* pcs);
extern "C" DLL_EXPORT S8DelegatePInvokeByRefCdeclCaller _cdecl Get_MarshalStructS8ByRef_Cdecl_FuncPtr()
{
    return MarshalStructS8ByRef_Cdecl;
}

typedef BOOL (__stdcall *S8DelegatePInvokeByRefStdCallCaller)(S8* pcs);
extern "C" DLL_EXPORT S8DelegatePInvokeByRefStdCallCaller __stdcall Get_MarshalStructS8ByRef_StdCall_FuncPtr()
{
    return MarshalStructS8ByRef_StdCall;
}


//Passby value
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructS8ByVal_Cdecl(S8 argstr)
{
    //Check the Input
    if(!IsCorrectS8(&argstr))
    {
        printf("\tMarshalStructS8ByVal_Cdecl: S8 param not as expected\n");
        PrintS8(&argstr,"argstr");
        return FALSE;
    }

    const char* lpstr = "world";
    size_t size = sizeof(char) * (strlen(lpstr) + 1);
    LPSTR temp = (LPSTR)CoreClrAlloc( size );
    memset(temp, 0, size);
    if(temp)
    {
        strcpy_s((char*)temp,size,lpstr);
        argstr.name = temp;
    }
    else
    {
        printf("Memory Allocated Failed!");
    }
    argstr.gender = false;
    argstr.jobNum = 1;
    argstr.i32 = 256;
    argstr.ui32 = 256;
    argstr.mySByte = 64;

    return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructS8ByVal_StdCall(S8 argstr)
{
    //Check the Input
    if(!IsCorrectS8(&argstr))
    {
        printf("\tMarshalStructS8ByVal_StdCall: S8 param not as expected\n");
        PrintS8(&argstr,"argstr");
        return FALSE;
    }

    const char* lpstr = "world";
    size_t size = sizeof(char) * (strlen(lpstr) + 1);
    LPSTR temp = (LPSTR)CoreClrAlloc( size );
    memset(temp, 0, size);
    if(temp)
    {
        strcpy_s((char*)temp,size,lpstr);
        argstr.name = temp;
    }
    else
    {
        printf("Memory Allocated Failed!");
    }
    argstr.gender = false;
    argstr.jobNum = 1;
    argstr.i32 = 256;
    argstr.ui32 = 256;
    argstr.mySByte = 64;

    return TRUE;
}

typedef BOOL (_cdecl *S8ByValCdeclCaller)(S8 cs);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalStructS8ByVal_Cdecl(S8ByValCdeclCaller caller)
{
    //Init
    S8 argstr{};

    const char* lpstr = "world";
    size_t size = sizeof(char) * (strlen(lpstr) + 1);
    LPSTR temp = (LPSTR)CoreClrAlloc( size );
    memset(temp, 0, size);
    if(temp)
    {
        strcpy_s((char*)temp,size,lpstr);
        argstr.name = temp;
    }
    else
    {
        printf("Memory Allocated Failed!");
    }
    argstr.gender = false;
    argstr.jobNum = 1;
    argstr.i32 = 256;
    argstr.ui32 = 256;
    argstr.mySByte = 64;

    if(!caller(argstr))
    {
        printf("DoCallBack_MarshalStructS8ByVal_Cdecl:The Caller returns wrong value\n");
        return FALSE;
    }

    //Verify the value unchanged
    if(memcmp( argstr.name,"world", strlen("world")*sizeof(char)+1 )!= 0)
        return false;
    if(argstr.gender)
        return false;
    if(argstr.jobNum != 1)
        return false;
    if(argstr.i32!= 256 || argstr.ui32 != 256)
        return false;
    if(argstr.mySByte != 64)
        return false;

    return TRUE;
}

typedef BOOL (__stdcall *S8ByValStdCallCaller)(S8 cs);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalStructS8ByVal_StdCall(S8ByValStdCallCaller caller)
{
    //Init
    S8 argstr{};

    const char* lpstr = "world";
    size_t size = sizeof(char) * (strlen(lpstr) + 1);
    LPSTR temp = (LPSTR)CoreClrAlloc( size );
    memset(temp, 0, size);
    if(temp)
    {
        strcpy_s((char*)temp,size,lpstr);
        argstr.name = temp;
    }
    else
    {
        printf("Memory Allocated Failed!");
    }
    argstr.gender = false;
    argstr.jobNum = 1;
    argstr.i32 = 256;
    argstr.ui32 = 256;
    argstr.mySByte = 64;

    if(!caller(argstr))
    {
        printf("DoCallBack_MarshalStructS8ByVal_StdCall:The Caller returns wrong value\n");
        return FALSE;
    }

    //Verify the value unchanged
    if(memcmp( argstr.name,"world", strlen("world")*sizeof(char)+1 )!= 0)
        return false;
    if(argstr.gender)
        return false;
    if(argstr.jobNum != 1)
        return false;
    if(argstr.i32!= 256 || argstr.ui32 != 256)
        return false;
    if(argstr.mySByte != 64)
        return false;
    return TRUE;
}
//Delegate PInvoke,passbyval
typedef BOOL (_cdecl *S8DelegatePInvokeByValCdeclCaller)(S8 cs);
extern "C" DLL_EXPORT S8DelegatePInvokeByValCdeclCaller _cdecl Get_MarshalStructS8ByVal_Cdecl_FuncPtr()
{
    return MarshalStructS8ByVal_Cdecl;
}

typedef BOOL (__stdcall *S8DelegatePInvokeByValStdCallCaller)(S8 cs);
extern "C" DLL_EXPORT S8DelegatePInvokeByValStdCallCaller __stdcall Get_MarshalStructS8ByVal_StdCall_FuncPtr()
{
    return MarshalStructS8ByVal_StdCall;
}



///////////////////////////////////////////Methods for S9 struct////////////////////////////////////////////////////
extern "C" DLL_EXPORT void NtestMethod(S9 str1)
{
    printf("\tAction of the delegate");
}
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructS9ByRef_Cdecl(S9* argstr)
{
    if(argstr->i32 != 128 ||
        argstr->myDelegate1 == NULL)
    {
        printf("\tMarshalStructS9ByRef_Cdecl: S9 param not as expected\n");
        return FALSE;
    }
    argstr->i32 = 256;
    argstr->myDelegate1 = NULL;
    return TRUE;
}
extern "C" DLL_EXPORT BOOL __stdcall MarshalStructS9ByRef_StdCall(S9* argstr)
{
    if(argstr->i32 != 128 ||
        argstr->myDelegate1 == NULL)
    {
        printf("\tMarshalStructS9ByRef_StdCall: S9 param not as expected\n");
        return FALSE;
    }
    argstr->i32 = 256;
    argstr->myDelegate1 = NULL;
    return TRUE;
}
typedef BOOL (_cdecl *S9ByRefCdeclCaller)(S9* pcs);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalStructS9ByRef_Cdecl(S9ByRefCdeclCaller caller)
{
    //Init
    S9 argstr;

    argstr.i32 = 256;
    argstr.myDelegate1 = NULL;

    if(!caller(&argstr))
    {
        printf("DoCallBack_MarshalStructS9ByRef_Cdecl:The Caller returns wrong value\n");
        return FALSE;
    }
    //Verify the value unchanged
    if(argstr.i32 != 128 ||
        argstr.myDelegate1 == NULL)
    {
        printf("The parameter for DoCallBack_MarshalStructS9ByRef_Cdecl is wrong\n");
        return FALSE;
    }
    return TRUE;
}

typedef BOOL (__stdcall *S9ByRefStdCallCaller)(S9* pcs);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalStructS9ByRef_StdCall(S9ByRefStdCallCaller caller)
{
    //Init
    S9 argstr;

    argstr.i32 = 256;
    argstr.myDelegate1 = NULL;

    if(!caller(&argstr))
    {
        printf("DoCallBack_MarshalStructS9ByRef_StdCall:The Caller returns wrong value\n");
        return FALSE;
    }
    //Verify the value unchanged
    if(argstr.i32 != 128 ||
        argstr.myDelegate1 == NULL)
    {
        printf("The parameter for DoCallBack_MarshalStructS9ByRef_StdCall is wrong\n");
        return FALSE;
    }
    return TRUE;
}
//Delegate PInvoke,passbyref
typedef BOOL (_cdecl *S9DelegatePInvokeByRefCdeclCaller)(S9* pcs);
extern "C" DLL_EXPORT S9DelegatePInvokeByRefCdeclCaller _cdecl Get_MarshalStructS9ByRef_Cdecl_FuncPtr()
{
    return MarshalStructS9ByRef_Cdecl;
}

typedef BOOL (__stdcall *S9DelegatePInvokeByRefStdCallCaller)(S9* pcs);
extern "C" DLL_EXPORT S9DelegatePInvokeByRefStdCallCaller __stdcall Get_MarshalStructS9ByRef_StdCall_FuncPtr()
{
    return MarshalStructS9ByRef_StdCall;
}


//Passby value
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructS9ByVal_Cdecl(S9 argstr)
{
    //Check the Input
    if(argstr.i32 != 128 ||
        argstr.myDelegate1 == NULL)
    {
        printf("\tMarshalStructS9ByVal_Cdecl: S9 param not as expected\n");
        return FALSE;
    }
    argstr.i32 = 256;
    argstr.myDelegate1 = NULL;

    return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructS9ByVal_StdCall(S9 argstr)
{
    //Check the Input
    if(argstr.i32 != 128 ||
        argstr.myDelegate1 == NULL)
    {
        printf("\tMarshalStructS9ByVal_StdCall: S9 param not as expected\n");
        return FALSE;
    }

    argstr.i32 = 256;
    argstr.myDelegate1 = NULL;
    return TRUE;
}

typedef BOOL (_cdecl *S9ByValCdeclCaller)(S9 cs);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalStructS9ByVal_Cdecl(S9ByValCdeclCaller caller)
{
    //Init
    S9 argstr;

    argstr.i32 = 256;
    argstr.myDelegate1 = NULL;

    if(!caller(argstr))
    {
        printf("DoCallBack_MarshalStructS9ByVal_Cdecl:The Caller returns wrong value\n");
        return FALSE;
    }

    //Verify the value unchanged
    if(argstr.i32 != 256 || argstr.myDelegate1 != NULL)
        return false;
    return TRUE;
}

typedef BOOL (__stdcall *S9ByValStdCallCaller)(S9 cs);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalStructS9ByVal_StdCall(S9ByValStdCallCaller caller)
{
    //Init
    S9 argstr;

    argstr.i32 = 256;
    argstr.myDelegate1 = NULL;

    if(!caller(argstr))
    {
        printf("DoCallBack_MarshalStructS9ByVal_StdCall:The Caller returns wrong value\n");
        return FALSE;
    }

    //Verify the value unchanged
    if(argstr.i32 != 256 || argstr.myDelegate1 != NULL)
        return false;
    return TRUE;
}
//Delegate PInvoke,passbyval
typedef BOOL (_cdecl *S9DelegatePInvokeByValCdeclCaller)(S9 cs);
extern "C" DLL_EXPORT S9DelegatePInvokeByValCdeclCaller _cdecl Get_MarshalStructS9ByVal_Cdecl_FuncPtr()
{
    return MarshalStructS9ByVal_Cdecl;
}

typedef BOOL (__stdcall *S9DelegatePInvokeByValStdCallCaller)(S9 cs);
extern "C" DLL_EXPORT S9DelegatePInvokeByValStdCallCaller _cdecl Get_MarshalStructS9ByVal_StdCall_FuncPtr()
{
    return MarshalStructS9ByVal_StdCall;
}

///////////////////////////////////////////Methods for IncludeOuterIntegerStructSequential struct////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructIncludeOuterIntegerStructSequentialByRef_Cdecl(IncludeOuterIntegerStructSequential* argstr)
{
    if(!IsCorrectIncludeOuterIntegerStructSequential(argstr))
    {
        printf("\tMarshalStructIncludeOuterIntegerStructSequentialByRef_Cdecl: IncludeOuterIntegerStructSequential param not as expected\n");
        PrintIncludeOuterIntegerStructSequential(argstr,"argstr");
        return FALSE;
    }
    ChangeIncludeOuterIntegerStructSequential(argstr);
    return TRUE;
}
extern "C" DLL_EXPORT BOOL __stdcall MarshalStructIncludeOuterIntegerStructSequentialByRef_StdCall(IncludeOuterIntegerStructSequential* argstr)
{
    if(!IsCorrectIncludeOuterIntegerStructSequential(argstr))
    {
        printf("\tMarshalStructIncludeOuterIntegerStructSequentialByRef_StdCall: IncludeOuterIntegerStructSequential param not as expected\n");
        PrintIncludeOuterIntegerStructSequential(argstr,"argstr");
        return FALSE;
    }
    ChangeIncludeOuterIntegerStructSequential(argstr);
    return TRUE;
}
typedef BOOL (_cdecl *IncludeOuterIntegerStructSequentialByRefCdeclCaller)(IncludeOuterIntegerStructSequential* pcs);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalStructIncludeOuterIntegerStructSequentialByRef_Cdecl(IncludeOuterIntegerStructSequentialByRefCdeclCaller caller)
{
    //Init
    IncludeOuterIntegerStructSequential argstr;

    argstr.s.s_int.i = 64;
    argstr.s.i = 64;

    if(!caller(&argstr))
    {
        printf("DoCallBack_MarshalStructIncludeOuterIntegerStructSequentialByRef_Cdecl:The Caller returns wrong value\n");
        return FALSE;
    }
    //Verify the value unchanged
    if(!IsCorrectIncludeOuterIntegerStructSequential(&argstr))
    {
        printf("The parameter for DoCallBack_MarshalStructIncludeOuterIntegerStructSequentialByRef_Cdecl is wrong\n");
        return FALSE;
    }
    return TRUE;
}

typedef BOOL (__stdcall *IncludeOuterIntegerStructSequentialByRefStdCallCaller)(IncludeOuterIntegerStructSequential* pcs);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalStructIncludeOuterIntegerStructSequentialByRef_StdCall(IncludeOuterIntegerStructSequentialByRefStdCallCaller caller)
{
    //Init
    IncludeOuterIntegerStructSequential argstr;

    argstr.s.s_int.i = 64;
    argstr.s.i = 64;

    if(!caller(&argstr))
    {
        printf("DoCallBack_MarshalStructIncludeOuterIntegerStructSequentialByRef_StdCall:The Caller returns wrong value\n");
        return FALSE;
    }
    //Verify the value unchanged
    if(!IsCorrectIncludeOuterIntegerStructSequential(&argstr))
    {
        printf("The parameter for DoCallBack_MarshalStructIncludeOuterIntegerStructSequentialByRef_StdCall is wrong\n");
        return FALSE;
    }
    return TRUE;
}
//Delegate PInvoke,passbyref
typedef BOOL (_cdecl *IncludeOuterIntegerStructSequentialDelegatePInvokeByRefCdeclCaller)(IncludeOuterIntegerStructSequential* pcs);
extern "C" DLL_EXPORT IncludeOuterIntegerStructSequentialDelegatePInvokeByRefCdeclCaller _cdecl Get_MarshalStructIncludeOuterIntegerStructSequentialByRef_Cdecl_FuncPtr()
{
    return MarshalStructIncludeOuterIntegerStructSequentialByRef_Cdecl;
}

typedef BOOL (__stdcall *IncludeOuterIntegerStructSequentialDelegatePInvokeByRefStdCallCaller)(IncludeOuterIntegerStructSequential* pcs);
extern "C" DLL_EXPORT IncludeOuterIntegerStructSequentialDelegatePInvokeByRefStdCallCaller __stdcall Get_MarshalStructIncludeOuterIntegerStructSequentialByRef_StdCall_FuncPtr()
{
    return MarshalStructIncludeOuterIntegerStructSequentialByRef_StdCall;
}


//Passby value
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructIncludeOuterIntegerStructSequentialByVal_Cdecl(IncludeOuterIntegerStructSequential argstr)
{
    //Check the Input
    if(!IsCorrectIncludeOuterIntegerStructSequential(&argstr))
    {
        printf("\tMarshalStructIncludeOuterIntegerStructSequentialByVal_Cdecl: IncludeOuterIntegerStructSequential param not as expected\n");
        PrintIncludeOuterIntegerStructSequential(&argstr,"argstr");
        return FALSE;
    }

    argstr.s.s_int.i = 64;
    argstr.s.i = 64;

    return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructIncludeOuterIntegerStructSequentialByVal_StdCall(IncludeOuterIntegerStructSequential argstr)
{
    //Check the Input
    if(!IsCorrectIncludeOuterIntegerStructSequential(&argstr))
    {
        printf("\tMarshalStructIncludeOuterIntegerStructSequentialByVal_StdCall: IncludeOuterIntegerStructSequential param not as expected\n");
        PrintIncludeOuterIntegerStructSequential(&argstr,"argstr");
        return FALSE;
    }

    argstr.s.s_int.i = 64;
    argstr.s.i = 64;

    return TRUE;
}

typedef IncludeOuterIntegerStructSequential (_cdecl *IncludeOuterIntegerStructSequentialByValCdeclCaller)(IncludeOuterIntegerStructSequential cs);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalStructIncludeOuterIntegerStructSequentialByVal_Cdecl(IncludeOuterIntegerStructSequentialByValCdeclCaller caller)
{
    //Init
    IncludeOuterIntegerStructSequential argstr;

    argstr.s.s_int.i = 64;
    argstr.s.i = 64;

    IncludeOuterIntegerStructSequential retstr = caller(argstr);

    if (!IsCorrectIncludeOuterIntegerStructSequential(&retstr))
    {
        printf("DoCallBack_MarshalStructIncludeOuterIntegerStructSequentialByVal_Cdecl:The Caller returns wrong value\n");
        PrintIncludeOuterIntegerStructSequential(&retstr, "retstr");
        return FALSE;
    }

    //Verify the value unchanged
    if(argstr.s.s_int.i != 64)
        return false;
    if(argstr.s.i != 64)
        return false;

    return TRUE;
}

typedef IncludeOuterIntegerStructSequential (__stdcall *IncludeOuterIntegerStructSequentialByValStdCallCaller)(IncludeOuterIntegerStructSequential cs);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalStructIncludeOuterIntegerStructSequentialByVal_StdCall(IncludeOuterIntegerStructSequentialByValStdCallCaller caller)
{
    //Init
    IncludeOuterIntegerStructSequential argstr;

    argstr.s.s_int.i = 64;
    argstr.s.i = 64;

    IncludeOuterIntegerStructSequential retstr = caller(argstr);

    if (!IsCorrectIncludeOuterIntegerStructSequential(&retstr))
    {
        printf("DoCallBack_MarshalStructIncludeOuterIntegerStructSequentialByVal_StdCall:The Caller returns wrong value\n");
        PrintIncludeOuterIntegerStructSequential(&retstr, "retstr");
        return FALSE;
    }

    //Verify the value unchanged
    if(argstr.s.s_int.i != 64)
        return false;
    if(argstr.s.i != 64)
        return false;
    return TRUE;
}
//Delegate PInvoke,passbyval
typedef BOOL (_cdecl *IncludeOuterIntegerStructSequentialDelegatePInvokeByValCdeclCaller)(IncludeOuterIntegerStructSequential cs);
extern "C" DLL_EXPORT IncludeOuterIntegerStructSequentialDelegatePInvokeByValCdeclCaller _cdecl Get_MarshalStructIncludeOuterIntegerStructSequentialByVal_Cdecl_FuncPtr()
{
    return MarshalStructIncludeOuterIntegerStructSequentialByVal_Cdecl;
}

typedef BOOL (__stdcall *IncludeOuterIntegerStructSequentialDelegatePInvokeByValStdCallCaller)(IncludeOuterIntegerStructSequential cs);
extern "C" DLL_EXPORT IncludeOuterIntegerStructSequentialDelegatePInvokeByValStdCallCaller __stdcall Get_MarshalStructIncludeOuterIntegerStructSequentialByVal_StdCall_FuncPtr()
{
    return MarshalStructIncludeOuterIntegerStructSequentialByVal_StdCall;
}

///////////////////////////////////////////Methods for S11 struct////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructS11ByRef_Cdecl(S11* argstr)
{
    if(argstr->i32 != (int32_t*)(32) || argstr->i != 32)
    {
        printf("\tMarshalStructS11ByRef_Cdecl: S11 param not as expected\n");
        return FALSE;
    }
    argstr->i32 = (int32_t*)(64);
    argstr->i = 64;
    return TRUE;
}
extern "C" DLL_EXPORT BOOL __stdcall MarshalStructS11ByRef_StdCall(S11* argstr)
{
    if(argstr->i32 != (int32_t*)(32) || argstr->i != 32)
    {
        printf("\tMarshalStructS11ByRef_StdCall: S11 param not as expected\n");
        return FALSE;
    }
    argstr->i32 = (int32_t*)(64);
    argstr->i = 64;
    return TRUE;
}
typedef BOOL (_cdecl *S11ByRefCdeclCaller)(S11* pcs);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalStructS11ByRef_Cdecl(S11ByRefCdeclCaller caller)
{
    //Init
    S11 argstr;

    argstr.i32 = (int32_t*)(64);
    argstr.i = 64;

    if(!caller(&argstr))
    {
        printf("DoCallBack_MarshalStructS11ByRef_Cdecl:The Caller returns wrong value\n");
        return FALSE;
    }
    //Verify the value unchanged
    if(argstr.i32 != (int32_t*)32 || argstr.i != 32)
    {
        printf("The parameter for DoCallBack_MarshalStructS11ByRef_Cdecl is wrong\n");
        return FALSE;
    }
    return TRUE;
}

typedef BOOL (__stdcall *S11ByRefStdCallCaller)(S11* pcs);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalStructS11ByRef_StdCall(S11ByRefStdCallCaller caller)
{
    //Init
    S11 argstr;

    argstr.i32 = (int32_t*)(64);
    argstr.i = 64;

    if(!caller(&argstr))
    {
        printf("DoCallBack_MarshalStructS11ByRef_StdCall:The Caller returns wrong value\n");
        return FALSE;
    }
    //Verify the value unchanged
    if(argstr.i32 != (int32_t*)(32) || argstr.i != 32)
    {
        printf("The parameter for DoCallBack_MarshalStructS11ByRef_StdCall is wrong\n");
        return FALSE;
    }
    return TRUE;
}
//Delegate PInvoke,passbyref
typedef BOOL (_cdecl *S11DelegatePInvokeByRefCdeclCaller)(S11* pcs);
extern "C" DLL_EXPORT S11DelegatePInvokeByRefCdeclCaller _cdecl Get_MarshalStructS11ByRef_Cdecl_FuncPtr()
{
    return MarshalStructS11ByRef_Cdecl;
}

typedef BOOL (__stdcall *S11DelegatePInvokeByRefStdCallCaller)(S11* pcs);
extern "C" DLL_EXPORT S11DelegatePInvokeByRefStdCallCaller __stdcall Get_MarshalStructS11ByRef_StdCall_FuncPtr()
{
    return MarshalStructS11ByRef_StdCall;
}


//Passby value
extern "C" DLL_EXPORT BOOL _cdecl MarshalStructS11ByVal_Cdecl(S11 argstr)
{
    //Check the Input
    if(argstr.i32 != (int32_t*)(32) || argstr.i != 32)
    {
        printf("\tMarshalStructS11ByVal_Cdecl: S11 param not as expected\n");
        return FALSE;
    }

    argstr.i32 = (int32_t*)(64);
    argstr.i = 64;

    return TRUE;
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalStructS11ByVal_StdCall(S11 argstr)
{
    //Check the Input
    if(argstr.i32 != (int32_t*)(32) || argstr.i != 32)
    {
        printf("\tMarshalStructS11ByVal_StdCall: S11 param not as expected\n");
        return FALSE;
    }

    argstr.i32 = (int32_t*)(64);
    argstr.i = 64;

    return TRUE;
}

typedef BOOL (_cdecl *S11ByValCdeclCaller)(S11 cs);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalStructS11ByVal_Cdecl(S11ByValCdeclCaller caller)
{
    //Init
    S11 argstr;

    argstr.i32 = (int32_t*)(64);
    argstr.i = 64;

    if(!caller(argstr))
    {
        printf("DoCallBack_MarshalStructS11ByVal_Cdecl:The Caller returns wrong value\n");
        return FALSE;
    }

    //Verify the value unchanged
    if(argstr.i32 != (int32_t*)(64) || argstr.i != 64)
        return false;

    return TRUE;
}

typedef BOOL (__stdcall *S11ByValStdCallCaller)(S11 cs);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalStructS11ByVal_StdCall(S11ByValStdCallCaller caller)
{
    //Init
    S11 argstr;

    argstr.i32 = (int32_t*)(64);
    argstr.i = 64;

    if(!caller(argstr))
    {
        printf("DoCallBack_MarshalStructS11ByVal_StdCall:The Caller returns wrong value\n");
        return FALSE;
    }

    //Verify the value unchanged
    if(argstr.i32 != (int32_t*)(64) || argstr.i != 64)
        return false;
    return TRUE;
}
//Delegate PInvoke,passbyval
typedef BOOL (_cdecl *S11DelegatePInvokeByValCdeclCaller)(S11 cs);
extern "C" DLL_EXPORT S11DelegatePInvokeByValCdeclCaller _cdecl Get_MarshalStructS11ByVal_Cdecl_FuncPtr()
{
    return MarshalStructS11ByVal_Cdecl;
}

typedef BOOL (__stdcall *S11DelegatePInvokeByValStdCallCaller)(S11 cs);
extern "C" DLL_EXPORT S11DelegatePInvokeByValStdCallCaller __stdcall Get_MarshalStructS11ByVal_StdCall_FuncPtr()
{
    return MarshalStructS11ByVal_StdCall;
}


typedef ByteStruct3Byte (__stdcall *ByteStruct3ByteByValStdCallCaller)(ByteStruct3Byte cs, BOOL *pBool);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalStructByVal_StdCall_ByteStruct3Byte(ByteStruct3ByteByValStdCallCaller caller)
{
    //Init
    ByteStruct3Byte argstr;

    argstr.b1 = 1;
    argstr.b2 = 42;
    argstr.b3 = 90;

    BOOL result;
    ByteStruct3Byte retstruct = caller(argstr, &result);

    if(!IsCorrectByteStruct3Byte(&retstruct))
    {
        printf("DoCallBack_MarshalStructByVal_StdCall_ByteStruct3Byte:The Caller returns wrong value\n");
        return FALSE;
    }

    if(!result)
    {
        printf("DoCallBack_MarshalStructByVal_StdCall_ByteStruct3Byte:The Caller failed\n");
        return FALSE;
    }

    //Verify the value unchanged
    if(argstr.b1 != 1 || argstr.b2 != 42 || argstr.b3 != 90)
        return false;
    return TRUE;
}

typedef ByteStruct3Byte (_cdecl *ByteStruct3ByteByValCdeclCaller)(ByteStruct3Byte cs, BOOL *pBool);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalStructByVal_Cdecl_ByteStruct3Byte(ByteStruct3ByteByValCdeclCaller caller)
{
    //Init
    ByteStruct3Byte argstr;

    argstr.b1 = 1;
    argstr.b2 = 42;
    argstr.b3 = 90;

    BOOL result;
    ByteStruct3Byte retstruct = caller(argstr, &result);

    if(!IsCorrectByteStruct3Byte(&retstruct))
    {
        printf("DoCallBack_MarshalStructByVal_StdCall_ByteStruct3Byte:The Caller returns wrong value\n");
        return FALSE;
    }

    if(!result)
    {
        printf("DoCallBack_MarshalStructByVal_StdCall_ByteStruct3Byte:The Caller failed\n");
        return FALSE;
    }

    //Verify the value unchanged
    if(argstr.b1 != 1 || argstr.b2 != 42 || argstr.b3 != 90)
        return false;
    return TRUE;
}

typedef IntegerStructSequential (_cdecl *IntegerStructSequentialByValCdeclCaller)(IntegerStructSequential cs);
extern "C" DLL_EXPORT BOOL _cdecl DoCallBack_MarshalStructIntegerStructSequentialByVal_Cdecl(IntegerStructSequentialByValCdeclCaller caller)
{
    //Init
    IntegerStructSequential argstr;

    argstr.i = 64;

    IntegerStructSequential retstr = caller(argstr);

    if (!IsCorrectIntegerStructSequential(&retstr))
    {
        printf("DoCallBack_MarshalStructIntegerStructSequentialByVal_Cdecl:The Caller returns wrong value\n");
        PrintIntegerStructSequential(&retstr, "retstr");
        return FALSE;
    }

    //Verify the value unchanged
    if(argstr.i != 64)
        return false;
    return TRUE;
}

typedef IntegerStructSequential (__stdcall *IntegerStructSequentialByValStdCallCaller)(IntegerStructSequential cs);
extern "C" DLL_EXPORT BOOL __stdcall DoCallBack_MarshalStructIntegerStructSequentialByVal_StdCall(IntegerStructSequentialByValStdCallCaller caller)
{
    //Init
    IntegerStructSequential argstr;

    argstr.i = 64;

    IntegerStructSequential retstr = caller(argstr);

    if (!IsCorrectIntegerStructSequential(&retstr))
    {
        printf("DoCallBack_MarshalStructIntegerStructSequentialByVal_StdCall:The Caller returns wrong value\n");
        PrintIntegerStructSequential(&retstr, "retstr");
        return FALSE;
    }

    //Verify the value unchanged
    if(argstr.i != 64)
        return false;
    return TRUE;
}
