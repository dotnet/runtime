// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <stdlib.h>
#include <stdio.h>
#include <xplatform.h>

/*-----------------------------------------------------------------------------*
*                                                                              *
*						For MarshalDelegateAsField_AsFunctionPtr.cs            *
*							 /  MarshalDelegateAsField_AsDefault.cs			   *
*-----------------------------------------------------------------------------*/

typedef int (STDMETHODCALLTYPE *FuncPtr)();

//auxiliary verification value
const int COMMONMETHODCALLED_RIGHT_RETVAL = 10;

extern "C" DLL_EXPORT int STDMETHODCALLTYPE CommonMethod()
{
    printf("\n\tCalling CommonMethodCalled() by FuncPtr...");
    return COMMONMETHODCALLED_RIGHT_RETVAL;
}

//FuncPtr funcPtr = CommonMethod;

///////////////////////Struct_Sequential/////////////////////////
typedef struct _Struct1_FuncPtrAsField1_Seq{
    BOOL verification;
    FuncPtr funcPtr;
} Struct1_FuncPtrAsField1_Seq;

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeDelegateAsFieldInStruct_Seq(Struct1_FuncPtrAsField1_Seq sfs)
{
    if(!sfs.verification || sfs.funcPtr == NULL)
    {
        printf("TakeDelegateAsFieldInStruct_Seq:NULL field member.\n");
        return FALSE;
    }
    else
    {	
        return sfs.verification && (sfs.funcPtr() == COMMONMETHODCALLED_RIGHT_RETVAL);
    }
}

///////////////////////Struct_Explicit///////////////////////
typedef struct _Struct1_FuncPtrAsField2_Exp{
    BOOL verification;
    int Padding;
    FuncPtr funcPtr;
} Struct1_FuncPtrAsField2_Exp;

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeDelegateAsFieldInStruct_Exp(Struct1_FuncPtrAsField2_Exp sfe)
{
    if(!sfe.verification || sfe.funcPtr == NULL)
    {
        printf("TakeDelegateAsFieldInStruct_Exp:NULL field member.\n");
        return FALSE;	
    }
    else 
    {
        return sfe.verification && sfe.funcPtr() == COMMONMETHODCALLED_RIGHT_RETVAL;
    }
}

///////////////////////Struct_Sequential/////////////////////////
class Class1_FuncPtrAsField3_Seq{
public:
    BOOL verification;
    FuncPtr funcPtr;
} ;

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeDelegateAsFieldInClass_Seq(Class1_FuncPtrAsField3_Seq *cfs)
{
    if(!cfs->verification || cfs->funcPtr == NULL)
    {
        printf("TakeDelegateAsFieldInClass_Seq:NULL field member.\n");
        return FALSE;
    }
    else
    {	
        return cfs->verification && (cfs->funcPtr() == COMMONMETHODCALLED_RIGHT_RETVAL);
    }
}

///////////////////////Struct_Explicit///////////////////////
class Class1_FuncPtrAsField4_Exp{
public:
    BOOL verification;
    int  Padding;
    FuncPtr funcPtr;
};

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeDelegateAsFieldInClass_Exp(Class1_FuncPtrAsField4_Exp *cfe)
{
    if(!cfe->verification || cfe->funcPtr == NULL)
    {
        printf("TakeDelegateAsFieldInClass_Exp:NULL field member.\n");
        return FALSE;
    }
    else 
    {
        return cfe->verification && (cfe->funcPtr() == COMMONMETHODCALLED_RIGHT_RETVAL);
    }
}

#ifdef _WIN32
#include <windows.h>

/*-----------------------------------------------------------------------------*
*                                                                             *
*					For MarshalDelegateAsField_AsInterface.cs                  *
*																			   *
*-----------------------------------------------------------------------------*/

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
    printf("\n\tCommonMethod1() Calling...\n");
    result.result1 = COMMONMETHOD1_RESULT;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE CommonMethod2()
{
    printf("\n\tCommonMethod2() Calling...\n");
    result.result2 = COMMONMETHOD2_RESULT;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE CommonMethod3()
{
    printf("\n\tCommonMethod3() Calling...\n");
    result.result3 = COMMONMETHOD3_RESULT;
}

bool STDMETHODCALLTYPE Verify(Result expectedR, Result resultR)
{
    return expectedR.result1 == resultR.result1
        && expectedR.result2 == resultR.result2
        && expectedR.result3 == resultR.result3;
}


typedef struct _Struct3_InterfacePtrAsField1_Seq{
    BOOL  verification;
    _Delegate * p_dele;
}Struct3_InterfacePtrAsField1_Seq;

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE Take_DelegatePtrAsFieldInStruct_Seq(Struct3_InterfacePtrAsField1_Seq sis)
{
    HRESULT hr;

    ResetToZero();

    if(sis.verification == NULL || sis.p_dele == NULL)
    {
        printf("NULL field member.\n");
        return FALSE;
    }
    else 
    {
        hr = sis.p_dele->DynamicInvoke( NULL, NULL);
        if(FAILED(hr))
        {
            return FALSE;
        }
        bool tempBool = sis.verification && Verify( expected, result);


        //IDispatch::Invoke
        ResetToZero();

        BSTR bstrNames[1];
        bstrNames[0] = SysAllocString(L"DynamicInvoke");
        DISPID dispid = 0;
        hr = sis.p_dele->GetIDsOfNames(
            IID_NULL,
            bstrNames,
            sizeof(bstrNames) / sizeof(bstrNames[0]),
            GetUserDefaultLCID(),
            &dispid);

        SysFreeString(bstrNames[0]);

        if(FAILED(hr)) 
        { 
            printf("\nERROR: Invoke failed: 0x%x\n", (unsigned int)hr);
            return FALSE;
        }		

        VARIANT args[1];
        VariantInit(&args[0]);
        args[0].vt = VT_ARRAY|VT_VARIANT;
        args[0].parray = NULL;
        DISPPARAMS params = { args, NULL, 1, 0 };

        hr = sis.p_dele->Invoke(
            dispid, 
            IID_NULL, 
            GetUserDefaultLCID(), 
            DISPATCH_METHOD,
            &params,
            NULL, 
            NULL, 
            NULL);

        if(FAILED(hr)) 
        { 
            printf("\nERROR: Invoke failed: 0x%x\n", (unsigned int)hr);
            return FALSE;
        }

        return tempBool && Verify(expected, result);
    }
}

typedef struct _Struct3_InterfacePtrAsField2_Exp{
    bool verification;
    int  Padding;
    _Delegate * p_dele;
}Struct3_InterfacePtrAsField2_Exp;

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE Take_DelegatePtrAsFieldInStruct_Exp(Struct3_InterfacePtrAsField2_Exp sie)
{
    HRESULT hr;

    ResetToZero();

    if(sie.verification == NULL || sie.p_dele == NULL)
    {
        printf("NULL field member.\n");
        return FALSE;
    }
    else 
    {
        hr = sie.p_dele->DynamicInvoke(NULL, NULL);
        if(FAILED(hr))
        {
            return FALSE;
        }
        bool tempBool = sie.verification && Verify( expected, result);


        //IDispatch::Invoke
        ResetToZero();

        BSTR bstrNames[1];
        bstrNames[0] = SysAllocString(L"DynamicInvoke");
        DISPID dispid = 0;
        hr = sie.p_dele->GetIDsOfNames(
            IID_NULL,
            bstrNames,
            sizeof(bstrNames) / sizeof(bstrNames[0]),
            GetUserDefaultLCID(),
            &dispid);

        SysFreeString(bstrNames[0]);

        if(FAILED(hr)) 
        { 
            printf("\nERROR: Invoke failed: 0x%x\n", (unsigned int)hr);
            return FALSE;
        }		

        VARIANT args[1];
        VariantInit(&args[0]);
        args[0].vt = VT_ARRAY|VT_VARIANT;
        args[0].parray = NULL;
        DISPPARAMS params = { args, NULL, 1, 0 };

        hr = sie.p_dele->Invoke(
            dispid, 
            IID_NULL, 
            GetUserDefaultLCID(), 
            DISPATCH_METHOD,
            &params,
            NULL, 
            NULL, 
            NULL);
        if(FAILED(hr)) 
        { 
            printf("\nERROR: Invoke failed: 0x%x\n", (unsigned int)hr);
            return FALSE;
        }

        return tempBool && Verify(expected, result);
    }
}

class Class3_InterfacePtrAsField3_Seq
{
public:
    bool verification;
    _Delegate * p_dele;
};

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE Take_DelegatePtrAsFieldInClass_Seq(Class3_InterfacePtrAsField3_Seq *cis)
{
    HRESULT hr;

    ResetToZero();

    if(cis->verification == NULL || cis->p_dele == NULL)
    {
        printf("NULL field member.\n");
        return FALSE;
    }
    else 
    {
        hr = (cis->p_dele)->DynamicInvoke(NULL, NULL);
        if(FAILED(hr))
        {
            return FALSE;
        }
        bool tempBool = cis->verification && Verify( expected, result);


        //IDispatch::Invoke
        BSTR bstrNames[1];
        bstrNames[0] = SysAllocString(L"DynamicInvoke");
        DISPID dispid = 0;
        hr = (cis->p_dele)->GetIDsOfNames(
            IID_NULL,
            bstrNames,
            sizeof(bstrNames) / sizeof(bstrNames[0]),
            GetUserDefaultLCID(),
            &dispid);

        SysFreeString(bstrNames[0]);

        if(FAILED(hr)) 
        { 
            printf("\nERROR: Invoke failed: 0x%x\n", (unsigned int)hr);
            return FALSE;
        }		

        VARIANT args[1];
        VariantInit(&args[0]);
        args[0].vt = VT_ARRAY|VT_VARIANT;
        args[0].parray = NULL;
        DISPPARAMS params = { args, NULL, 1, 0 };

        hr = (cis->p_dele)->Invoke(
            dispid, 
            IID_NULL, 
            GetUserDefaultLCID(), 
            DISPATCH_METHOD,
            &params,
            NULL, 
            NULL, 
            NULL);

        if(FAILED(hr)) 
        { 
            printf("\nERROR: Invoke failed: 0x%x\n", (unsigned int)hr);
            return FALSE;
        }

        return tempBool && Verify(expected, result);
    }

}

class Class3_InterfacePtrAsField4_Exp{
public:
    bool verification;
    int  Padding;
    _Delegate * p_dele;
};

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE Take_DelegatePtrAsFieldInClass_Exp(Class3_InterfacePtrAsField4_Exp *cie)
{
    HRESULT hr;

    ResetToZero();

    if(cie->verification == NULL || cie->p_dele == NULL)
    {
        printf("NULL field member.\n");
        return FALSE;
    }
    else 
    {
        hr = (cie->p_dele)->DynamicInvoke(NULL, NULL);
        if(FAILED(hr))
        {
            return FALSE;
        }
        bool tempBool = cie->verification && Verify( expected, result);


        //IDispatch::Invoke
        BSTR bstrNames[1];
        bstrNames[0] = SysAllocString(L"DynamicInvoke");
        DISPID dispid = 0;
        hr = (cie->p_dele)->GetIDsOfNames(
            IID_NULL,
            bstrNames,
            sizeof(bstrNames) / sizeof(bstrNames[0]),
            GetUserDefaultLCID(),
            &dispid);

        SysFreeString(bstrNames[0]);

        if(FAILED(hr)) 
        { 
            printf("\nERROR: Invoke failed: 0x%x\n", (unsigned int)hr);
            return FALSE;
        }		

        VARIANT args[1];
        VariantInit(&args[0]);
        args[0].vt = VT_ARRAY|VT_VARIANT;
        args[0].parray = NULL;
        DISPPARAMS params = { args, NULL, 1, 0 };

        hr = (cie->p_dele)->Invoke(
            dispid, 
            IID_NULL, 
            GetUserDefaultLCID(), 
            DISPATCH_METHOD,
            &params,
            NULL, 
            NULL, 
            NULL);
        if(FAILED(hr)) 
        { 
            printf("\nERROR: Invoke failed: 0x%x\n", (unsigned int)hr);
            return FALSE;
        }

        return tempBool && Verify(expected, result);
    }
}
#endif
