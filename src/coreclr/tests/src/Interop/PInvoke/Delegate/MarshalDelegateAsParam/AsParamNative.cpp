// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <stdio.h>
#include <stdlib.h>
#include <xplatform.h>

/*-----------------------------------------------------------------------------*
*                                                                             *
*						For MarshalDelegateAsParam_AsFunctionPtr.cs            *
*																			   *
*-----------------------------------------------------------------------------*/

//auxiliary verification value
const int COMMONMETHODCALLED1_RIGHT_RETVAL = 10;
const int COMMONMETHODCALLED2_RIGHT_RETVAL = 20;

//common method called by function pointer(Delegate)
extern "C" DLL_EXPORT int STDMETHODCALLTYPE CommonMethodCalled1()
{
    printf("\n\tCalling CommonMethodCalled1() by FuncPtr...");
    return COMMONMETHODCALLED1_RIGHT_RETVAL;
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE CommonMethodCalled2()
{
    printf("\n\tCalling CommonMethodCalled2() by FuncPtr...");
    return COMMONMETHODCALLED2_RIGHT_RETVAL;
}

//define function pointer
typedef int (STDMETHODCALLTYPE *DelegateParam)();

//delegate marshalled by val
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeDelegateByValParam(DelegateParam deleParam)
{
    printf("\tdelegate marshaled by val.");

    //verify return value
    if(deleParam == NULL)
    {
        printf("\n\tNULL delegate!"); 
        return FALSE;
    }
    else if( deleParam() != COMMONMETHODCALLED1_RIGHT_RETVAL)
    {
        printf("\n\tReturn Value Err!"); 
        return FALSE;
    }

    return TRUE;
}

//delegate marshalled by ref
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeDelegateByRefParam(DelegateParam* deleParam)
{
    printf("\n\tdelegate marshaled by ref.");

    //verify value
    if((*deleParam) == NULL)
    {
        printf("\n\tNULL delegate!"); 
        return FALSE;
    }
    else if((*deleParam)() != COMMONMETHODCALLED1_RIGHT_RETVAL)
    {
        printf("\n\tReturn Value Err!"); 
        return FALSE;
    }

    //make funcptr point to CommonMethodCalled2
    *deleParam = CommonMethodCalled2;
    printf("\n\tNow FuncPtr point to CommonMethodCalled2 !");
    //verify value return again
    if((*deleParam)() != COMMONMETHODCALLED2_RIGHT_RETVAL)
    {
        printf("\n\tReturn Value Err!"); 
        return FALSE;
    }

    return TRUE;
}

//delegate marshalled by in,val
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeDelegateByInValParam(DelegateParam deleParam)
{
    printf("\n\tdelegate marshalled by in,val.");

    //verify return value
    if(deleParam == NULL)
    {
        printf("\n\tNULL delegate!"); 
        return FALSE;
    }
    else if( deleParam() != COMMONMETHODCALLED1_RIGHT_RETVAL)
    {
        printf("\n\tReturn Value Err!"); 
        return FALSE;
    }

    deleParam = CommonMethodCalled2;

    return TRUE;
}

//delegate marshalled by in,ref
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeDelegateByInRefParam(DelegateParam* deleParam)
{
    printf("\n\tdelegate marshalled by in,ref.");

    //verify return value
    if(*deleParam == NULL)
    {
        printf("\n\tNULL delegate!"); 
        return FALSE;
    }
    else if((*deleParam)() != COMMONMETHODCALLED1_RIGHT_RETVAL)
    {
        printf("\n\tReturn Value Err!"); 
        return FALSE;
    }

    *deleParam = CommonMethodCalled2;

    return TRUE;
}

//delegate marshalled by out,val
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeDelegateByOutValParam(DelegateParam deleParam)
{
    printf("\n\tdelegate marshalled by out,val.");

    //verify return value
    if(deleParam == NULL)
    {
        printf("\n\tNULL delegate!"); 
        return FALSE;
    }
    else if( deleParam() != COMMONMETHODCALLED1_RIGHT_RETVAL)
    {
        printf("\n\tReturn Value Err!"); 
        return FALSE;
    }

    return TRUE;
}

//delegate marshalled by out,ref
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeDelegateByOutRefParam(DelegateParam* deleParam)
{
    printf("\n\tdelegate marshalled by out,ref.");

    //arg getted should be NULL
    //when args ref marshaled as [Out] attribute, the args actually initialized on the native side 
    if(*deleParam != NULL)
    {
        printf("\n\tDelegate is not NULL !"); 
        return FALSE;
    }

    //initial value of delegate marshaled
    *deleParam = CommonMethodCalled2;
    printf("\n\tNow FuncPtr point to CommonMethodCalled2 !");

    //verify value return again
    if((*deleParam)() != COMMONMETHODCALLED2_RIGHT_RETVAL)
    {
        printf("\n\tReturn Value Err!"); 
        return FALSE;
    }

    return TRUE;
}

//delegate marshalled by in,out,val
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeDelegateByInOutValParam(DelegateParam deleParam)
{
    printf("\n\tdelegate marshalled by in,out,val.");
    //verify return value
    if(deleParam == NULL)
    {
        printf("\n\tNULL delegate!"); 
        return FALSE;
    }
    else if( deleParam() != COMMONMETHODCALLED1_RIGHT_RETVAL)
    {
        printf("\n\tReturn Value Err!"); 
        return FALSE;
    }

    return TRUE;
}

//delegate marshalled by in,out,ref
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeDelegateByInOutRefParam(DelegateParam* deleParam)
{
    printf("\n\tdelegate marshalled by in,out,ref.");

    //verify value
    if((*deleParam) == NULL)
    {
        printf("\n\tNULL delegate!"); 
        return FALSE;
    }
    else if((*deleParam)() != COMMONMETHODCALLED1_RIGHT_RETVAL)
    {
        printf("\n\tReturn Value Err!"); 
        return FALSE;
    }

    //make funcptr point to CommonMethodCalled2
    *deleParam = CommonMethodCalled2;
    printf("\n\tNow FuncPtr point to CommonMethodCalled2 !");
    //verify value return again
    if((*deleParam)() != COMMONMETHODCALLED2_RIGHT_RETVAL)
    {
        printf("\n\tReturn Value Err!"); 
        return FALSE;
    }

    return TRUE;
}

//ret delegate by val
extern "C" DLL_EXPORT DelegateParam STDMETHODCALLTYPE ReturnDelegateByVal()
{
    printf("\n\tdelegate marshalled by val.");

    return CommonMethodCalled1;
}

#ifdef _WIN32

#include <windows.h>

/* -----------------------------------------------------------------------------*
*																			    *
*						For MarshalDelegateAsParam_AsInterface.cs             *
*				                                                                *
* -----------------------------------------------------------------------------*/

#import "mscorlib.tlb" no_namespace named_guids raw_interfaces_only rename("ReportEvent","ReportEventNew")

typedef struct{
    int result1;
    int result2;
    int result3;
} Result;

const int COMMONMETHOD1_RESULT = 10;
const int COMMONMETHOD2_RESULT = 20;
const int COMMONMETHOD3_RESULT = 30;

const Result expected = {
    COMMONMETHOD1_RESULT, 
    COMMONMETHOD2_RESULT,
    COMMONMETHOD3_RESULT
};

Result result = {0,0,0};

void STDMETHODCALLTYPE ResetToZero()
{
    result.result1 = result.result2 = result.result3 = 0; 
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE CommonMethod1()
{
    printf("\n\tCommonMethod1() Calling...");
    result.result1 = 10;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE CommonMethod2()
{
    printf("\n\tCommonMethod2() Calling...");
    result.result2 = 20;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE CommonMethod3()
{
    printf("\n\tCommonMethod3() Calling...");
    result.result3 = 30;
}

BOOL STDMETHODCALLTYPE Verify(Result expectedR, Result resultR)
{
    return expectedR.result1 == resultR.result1
        && expectedR.result2 == resultR.result2
        && expectedR.result3 == resultR.result3;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE Take_DelegatePtrByValParam(_Delegate * p_dele)
{
    ResetToZero();

    HRESULT hr;
    hr = p_dele->DynamicInvoke(NULL, NULL);
    if(FAILED(hr))
        return FALSE;
    else
        return Verify(expected, result);
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE Take_DelegatePtrByRefParam(_Delegate **pp_dele)
{
    ResetToZero();

    HRESULT hr;
    hr = (*pp_dele)->DynamicInvoke(NULL, NULL);
    if(FAILED(hr))
    {
        return FALSE;
    }
    else
    {
        *pp_dele = NULL;
        return Verify(expected, result);
    }
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE Take_DelegatePtrByInValParam(_Delegate * p_dele)
{
    ResetToZero();

    HRESULT hr;
    hr = p_dele->DynamicInvoke(NULL, NULL);
    if(FAILED(hr))
    {
        return FALSE;
    }
    else
    {
        return Verify(expected, result);
    }
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE Take_DelegatePtrByInRefParam(_Delegate **pp_dele)
{
    ResetToZero();

    HRESULT hr;
    hr = (*pp_dele)->DynamicInvoke(NULL, NULL);
    if(FAILED(hr))
    {
        return FALSE;
    }
    else
    {
        *pp_dele = NULL;
        return Verify(expected, result);
    }
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE Take_DelegatePtrByOutValParam(_Delegate * p_dele)
{
    ResetToZero();

    HRESULT hr;
    hr = p_dele->DynamicInvoke(NULL, NULL);
    if(FAILED(hr))
    {
        return FALSE;
    }
    else
    {
        p_dele = NULL;
        return Verify(expected, result);
    }
}

//verification method
extern "C" DLL_EXPORT int STDMETHODCALLTYPE RetFieldResult1()
{
    return result.result1;
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE RetFieldResult2()
{
    return result.result2;
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE RetFieldResult3()
{
    return result.result3;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE Take_DelegatePtrByOutRefParam(_Delegate ** pp_dele, _Delegate * pdeleHelper)
{
    printf("In Take_DelegatePtrByOutRefParam native side \n");
    ResetToZero();

    if( *pp_dele != NULL)
    {
        return FALSE;
    }
    else
    {
        *pp_dele = pdeleHelper;
        (*pp_dele)->AddRef();
        return TRUE;
    }
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE Take_DelegatePtrByInOutValParam(_Delegate * p_dele)
{
    ResetToZero();

    HRESULT hr;
    hr = p_dele->DynamicInvoke(NULL, NULL);
    if(FAILED(hr))
    {
        return FALSE;
    }
    else
    {
        return Verify(expected, result);
    }
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE Take_DelegatePtrByInOutRefParam(_Delegate **pp_dele)
{
    ResetToZero();

    HRESULT hr;
    hr = (*pp_dele)->DynamicInvoke(NULL, NULL);
    if(FAILED(hr))
    {
        return FALSE;
    }
    else
    {
        *pp_dele = NULL;
        return Verify(expected, result); 
    }
}

extern "C" DLL_EXPORT _Delegate* ReturnDelegatePtrByVal(_Delegate * pdeleHelper)
{
    pdeleHelper->AddRef();
    return pdeleHelper;
}

#endif
