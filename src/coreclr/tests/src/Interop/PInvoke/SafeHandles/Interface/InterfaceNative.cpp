// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <stdio.h>
#include <stdlib.h>
#include <windows.h>
#include <oleauto.h>
#include <xplatform.h>
#include <platformdefines.h>
#include "StructDefs.h"

/////////////////////////////////Internal Helper Methods
/////////////////////////////////
BOOL static CheckGetIDsOfNamesHR(HRESULT hr)
{
    if( FAILED(hr) )
    {
        printf("\t\t\tGetIDsOfNames Call FAILED!\n");
        if( hr == E_OUTOFMEMORY )
            printf("\t\t\thr == E_OUTOFMEMORY\n");
        else if( hr == DISP_E_UNKNOWNNAME )
            printf("\t\t\thr == DISP_E_UNKNOWNNAME\n");
        else 
            printf("\t\t\thr == DISP_E_UNKNOWNLCID\n");
        return FALSE;
    }
    return TRUE;
}

BOOL static CheckInvokeHR(HRESULT hr)
{
    if( FAILED(hr) )
    {
        printf("\t\t\tInvoke FAILED!\n");
        if( hr == DISP_E_BADPARAMCOUNT )
            printf("\t\t\thr == DISP_E_BADPARAMCOUNT\n");
        else if( hr == DISP_E_EXCEPTION )
            printf("\t\t\thr == DISP_E_EXCEPTION\n");
        else if( hr == DISP_E_MEMBERNOTFOUND )
            printf("\t\t\thr == DISP_E_MEMBERNOTFOUND\n");
        else if( hr == DISP_E_UNKNOWNINTERFACE )
            printf("\t\t\thr == DISP_E_UNKNOWNINTERFACE\n");
        else if( hr == DISP_E_BADVARTYPE )
            printf("\t\t\thr == DISP_E_BADVARTYPE\n");
        else if( hr == DISP_E_NONAMEDARGS )
            printf("\t\t\thr == DISP_E_NONAMEDARGS\n");
        else if( hr == DISP_E_OVERFLOW )
            printf("\t\t\thr == DISP_E_OVERFLOW\n");
        else if( hr == DISP_E_PARAMNOTFOUND )
            printf("\t\t\thr == DISP_E_PARAMNOTFOUND\n");
        else if( hr == DISP_E_TYPEMISMATCH )
            printf("\t\t\thr == DISP_E_TYPEMISMATCH\n");
        else if( hr == DISP_E_UNKNOWNLCID )
            printf("\t\t\thr == DISP_E_UNKNOWNLCID\n");
        else 
            printf("\t\t\thr == DISP_E_PARAMNOTOPTIONAL\n");
        return FALSE;
    }
    return TRUE;
}

BOOL static InvokeAllFldsAndProps(IDispatch* ptoIntf, long shfld1Val, long shfld2Val)
{
    HRESULT hr;
    DISPID dispid_DangerousGetHandle, dispid_IsInvalid, dispid_shfld1, dispid_shfld2_prop;
    OLECHAR FAR* szMember;
    VARIANTARG varargs;
    DISPPARAMS DispParams = {&varargs, NULL, 0, 0};
    VARIANT  VarResult, VarResultBool, VarResultDangHnd;

    printf("\t\t\tCalling GetIDsOfNames() for shfld1...\n");
    szMember = (LPOLESTR)W("shfld1");
    hr = ptoIntf->GetIDsOfNames(IID_NULL, 
        &szMember, 
        1, 
        LOCALE_SYSTEM_DEFAULT, 
        &dispid_shfld1);
    if( !CheckGetIDsOfNamesHR(hr) )
        return FALSE;
    else
        printf("\t\t\tCall completed successfully.\n");

    printf("\t\t\tCalling GetIDsOfNames() for shfld2_prop...\n");
    szMember = (LPOLESTR)W("shfld2_prop");
    hr = ptoIntf->GetIDsOfNames(IID_NULL, 
        &szMember, 
        1, 
        LOCALE_SYSTEM_DEFAULT, 
        &dispid_shfld2_prop);
    if( !CheckGetIDsOfNamesHR(hr) )
        return FALSE;
    else
        printf("\t\t\tCall completed successfully.\n");

    printf("\n");

    //////////////////////////////////////////////////////////////////
    //NOTE: The SH fld is being returned as type VT_DISPATCH
    /*	printf("\t\t\tInvoking shfld1...\n");
    hr = ptoIntf->Invoke(dispid_shfld1, 
    IID_NULL, 
    LOCALE_SYSTEM_DEFAULT, 
    DISPATCH_PROPERTYGET, 
    &DispParams, 
    &VarResult, 
    NULL, 
    NULL);
    if( !CheckInvokeHR(hr) )
    return FALSE;
    else
    printf("\t\t\tCall completed successfully.\n");
    if( VarResult.vt != VT_INT || VarResult.intVal != shfld1Val )
    {
    printf("\t\t\t\tThe return Variant is incorrect!\n");
    return FALSE;
    }
    */

    //Get the property
    printf("\t\t\tInvoking shfld2_prop (Getting)...\n");
    hr = ptoIntf->Invoke(dispid_shfld2_prop, 
        IID_NULL, 
        LOCALE_SYSTEM_DEFAULT, 
        DISPATCH_PROPERTYGET, 
        &DispParams, 
        &VarResult, 
        NULL, 
        NULL);
    if( !CheckInvokeHR(hr) )
        return FALSE;
    else
        printf("\t\t\tCall completed successfully.\n");
    if( VarResult.vt != VT_DISPATCH )
    {
        printf("\t\t\t\tThe return Variant is incorrect!\n");
        return FALSE;
    }
    else //invoke DangerousGetHandle on this SH property using VarResult.pdispVal
    {
        printf("\t\t\t\tCalling GetIDsOfNames() for IsInvalid (shfld2_prop)...\n");
        szMember = (LPOLESTR)W("IsInvalid");
        hr = (VarResult.pdispVal)->GetIDsOfNames(IID_NULL, 
            &szMember, 
            1, 
            LOCALE_SYSTEM_DEFAULT, 
            &dispid_IsInvalid);
        if( !CheckGetIDsOfNamesHR(hr) )
            return FALSE;
        else
            printf("\t\t\t\tCall completed successfully.\n");

        printf("\t\t\t\tInvoking IsInValid (shfld2_prop)...\n");
        hr = (VarResult.pdispVal)->Invoke(dispid_IsInvalid, 
            IID_NULL, 
            LOCALE_SYSTEM_DEFAULT, 
            DISPATCH_PROPERTYGET, 
            &DispParams, 
            &VarResultBool, 
            NULL, 
            NULL);
        if( !CheckInvokeHR(hr) )
            return FALSE;
        else
            printf("\t\t\t\tCall completed successfully.\n");
        if( VarResultBool.vt != VT_BOOL || VarResultBool.bVal != 0 ) //should be valid
        {
            printf("\t\t\t\t\tThe return Variant is incorrect!\n");
            return FALSE;
        }

        printf("\t\t\t\tCalling GetIDsOfNames() for DangerousGetHandle (shfld2_prop)...\n");
        szMember = (LPOLESTR)W("DangerousGetHandle");
        hr = (VarResult.pdispVal)->GetIDsOfNames(IID_NULL, 
            &szMember, 
            1, 
            LOCALE_SYSTEM_DEFAULT, 
            &dispid_DangerousGetHandle);
        if( !CheckGetIDsOfNamesHR(hr) )
            return FALSE;
        else
            printf("\t\t\t\tCall completed successfully.\n");

        printf("\t\t\t\tInvoking DangerousGetHandle (shfld2_prop)...\n");
        hr = (VarResult.pdispVal)->Invoke(dispid_DangerousGetHandle, 
            IID_NULL, 
            LOCALE_SYSTEM_DEFAULT, 
            DISPATCH_METHOD, 
            &DispParams, 
            &VarResultDangHnd, 
            NULL, 
            NULL);
        if( !CheckInvokeHR(hr) )
            return FALSE;
        else
            printf("\t\t\t\tCall completed successfully.\n");
        if( VarResultDangHnd.vt != VT_INT || VarResultDangHnd.intVal != shfld2Val ) 
        {
            printf("\t\t\t\t\tThe return Variant is incorrect!\n");
            return FALSE;
        }

    } //end of else

    //Set the property to a SFH interface
    DISPPARAMS DispParams_prop = {&varargs,NULL, 1, 0};
    DispParams_prop.rgvarg[0].vt = VT_DISPATCH;
    DispParams_prop.rgvarg[0].pdispVal = ptoIntf; //Set the value of the property to its parent pointer
    printf("\t\t\tInvoking shfld2_prop (Setting)...\n");
    hr = ptoIntf->Invoke(dispid_shfld2_prop, 
        IID_NULL, 
        LOCALE_SYSTEM_DEFAULT, 
        DISPATCH_PROPERTYPUT, 
        &DispParams_prop, 
        &VarResult, 
        NULL, 
        NULL);
    if( !CheckInvokeHR(hr) )
        return FALSE;
    else
        printf("\t\t\tCall completed successfully.\n");


    return TRUE;
} //end of InvokeAllFldsAndProps

BOOL static InvokeAllInSHIntf(IDispatch* ptoIntf, long shVal, long shfld1Val, long shfld2Val)
{
    HRESULT hr;
    DISPID dispid_DangerousGetHandle, dispid_IsClosed, dispid_IsInvalid, dispid_Close, dispid_Dispose, dispid_SetHandleAsInvalid;
    OLECHAR FAR* szMember;
    VARIANTARG varargs;
    DISPPARAMS DispParams = {&varargs, NULL, 0, 0};
    VARIANT  VarResult;

    ///////////////////////////////////////////////////////////////////
    printf("\t\t\tCalling GetIDsOfNames() for DangerousGetHandle...\n");
    szMember = (LPOLESTR)W("DangerousGetHandle");
    hr = ptoIntf->GetIDsOfNames(IID_NULL, 
        &szMember, 
        1, 
        LOCALE_SYSTEM_DEFAULT, 
        &dispid_DangerousGetHandle);
    if( !CheckGetIDsOfNamesHR(hr) )
        return FALSE;
    else
        printf("\t\t\tCall completed successfully.\n");

    printf("\t\t\tCalling GetIDsOfNames() for IsClosed...\n");
    szMember = (LPOLESTR)W("IsClosed");
    hr = ptoIntf->GetIDsOfNames(IID_NULL, 
        &szMember, 
        1, 
        LOCALE_SYSTEM_DEFAULT, 
        &dispid_IsClosed);
    if( !CheckGetIDsOfNamesHR(hr) )
        return FALSE;
    else
        printf("\t\t\tCall completed successfully.\n");

    printf("\t\t\tCalling GetIDsOfNames() for IsInvalid...\n");
    szMember = (LPOLESTR)W("IsInvalid");
    hr = ptoIntf->GetIDsOfNames(IID_NULL, 
        &szMember, 
        1, 
        LOCALE_SYSTEM_DEFAULT, 
        &dispid_IsInvalid);
    if( !CheckGetIDsOfNamesHR(hr) )
        return FALSE;
    else
        printf("\t\t\tCall completed successfully.\n");

    printf("\t\t\tCalling GetIDsOfNames() for Close...\n");
    szMember = (LPOLESTR)W("Close");
    hr = ptoIntf->GetIDsOfNames(IID_NULL, 
        &szMember, 
        1, 
        LOCALE_SYSTEM_DEFAULT, 
        &dispid_Close);
    if( !CheckGetIDsOfNamesHR(hr) )
        return FALSE;
    else
        printf("\t\t\tCall completed successfully.\n");

    printf("\t\t\tCalling GetIDsOfNames() for Dispose...\n");
    szMember = (LPOLESTR)W("Dispose");
    hr = ptoIntf->GetIDsOfNames(IID_NULL, 
        &szMember, 
        1, 
        LOCALE_SYSTEM_DEFAULT, 
        &dispid_Dispose);
    if( !CheckGetIDsOfNamesHR(hr) )
        return FALSE;
    else
        printf("\t\t\tCall completed successfully.\n");

    printf("\t\t\tCalling GetIDsOfNames() for SetHandleAsInvalid...\n");
    szMember = (LPOLESTR)W("SetHandleAsInvalid");
    hr = ptoIntf->GetIDsOfNames(IID_NULL, 
        &szMember, 
        1, 
        LOCALE_SYSTEM_DEFAULT, 
        &dispid_SetHandleAsInvalid);
    if( !CheckGetIDsOfNamesHR(hr) )
        return FALSE;
    else
        printf("\t\t\tCall completed successfully.\n");


    printf("\n");
    //////////////////////////////////////////////////////////////////
    printf("\t\t\tInvoking IsInvalid...\n");
    hr = ptoIntf->Invoke(dispid_IsInvalid, 
        IID_NULL, 
        LOCALE_SYSTEM_DEFAULT, 
        DISPATCH_PROPERTYGET, 
        &DispParams, 
        &VarResult, 
        NULL, 
        NULL);
    if( !CheckInvokeHR(hr) )
        return FALSE;
    else
        printf("\t\t\tCall completed successfully.\n");
    if( VarResult.vt != VT_BOOL || VarResult.boolVal != 0 ) 
    {
        printf("\t\t\t\tThe return Variant is incorrect!\n");
        return FALSE;
    }

    //////////////////////////////////////////////////////////////////
    printf("\t\t\tInvoking IsClosed...\n");
    hr = ptoIntf->Invoke(dispid_IsClosed, 
        IID_NULL, 
        LOCALE_SYSTEM_DEFAULT, 
        DISPATCH_PROPERTYGET, 
        &DispParams, 
        &VarResult, 
        NULL, 
        NULL);
    if( !CheckInvokeHR(hr) )
        return FALSE;
    else
        printf("\t\t\tCall completed successfully.\n");
    if( VarResult.vt != VT_BOOL || VarResult.boolVal != 0 ) 
    {
        printf("\t\t\t\tThe return Variant is incorrect!\n");
        return FALSE;
    }

    //////////////////////////////////////////////////////////////////
    printf("\t\t\tInvoking DangerousGetHandle...\n");
    hr = ptoIntf->Invoke(dispid_DangerousGetHandle, 
        IID_NULL, 
        LOCALE_SYSTEM_DEFAULT, 
        DISPATCH_METHOD, 
        &DispParams, 
        &VarResult, 
        NULL, 
        NULL);
    if( !CheckInvokeHR(hr) )
        return FALSE;
    else
        printf("\t\t\tCall completed successfully.\n");
    if( VarResult.vt != VT_INT || VarResult.intVal != shVal ) 
    {
        printf("\t\t\t\tThe return Variant is incorrect!\n");
        return FALSE;
    }

    /////////////////////////////////////////////////////////////////////
    //invoke close repetitively to make sure nothing out of the ordinary happens
    for(int i = 0; i < 3; i++) 
    {
        printf("\t\t\tInvoking Close...\n");
        hr = ptoIntf->Invoke(dispid_Close, 
            IID_NULL, 
            LOCALE_SYSTEM_DEFAULT, 
            DISPATCH_METHOD, 
            &DispParams, 
            &VarResult, 
            NULL, 
            NULL);
        if( !CheckInvokeHR(hr) )
            return FALSE;
        else
            printf("\t\t\tCall completed successfully.\n");
    }

    /////////////////////////////////////////////////////////////////////
    printf("\t\t\tInvoking Dispose...\n");
    hr = ptoIntf->Invoke(dispid_Dispose, 
        IID_NULL, 
        LOCALE_SYSTEM_DEFAULT, 
        DISPATCH_METHOD, 
        &DispParams, 
        &VarResult, 
        NULL, 
        NULL);
    if( !CheckInvokeHR(hr) )
        return FALSE;
    else
        printf("\t\t\tCall completed successfully.\n");

    /////////////////////////////////////////////////////////////////////
    /* NOTE: This method should really be named SetHandleAsClosed since it
    only closes the handle and does not set it to its invalid values; this 
    decision was taken because the runtime would have had to waste storage 
    on remembering what the invalid values for this handle were */
    printf("\t\t\tInvoking SetHandleAsInvalid...\n");
    hr = ptoIntf->Invoke(dispid_SetHandleAsInvalid, 
        IID_NULL, 
        LOCALE_SYSTEM_DEFAULT, 
        DISPATCH_METHOD, 
        &DispParams, 
        &VarResult, 
        NULL, 
        NULL);
    if( !CheckInvokeHR(hr) )
        return FALSE;
    else
        printf("\t\t\tCall completed successfully.\n");

    ////////////////////////////////////////////////////////////////////
    /* 01/16/04 comment: This check is not needed (see note in previous
    call); removing because I am also removing the IsClosed check in the 
    IsInvalid property of the handle; since we no longer check IsClosed, 
    calling IsInvalid after SetHandleAsInvalid returns false since 
    SetHandleAsInvalid only closes the handle 

    printf("\t\t\tInvoking IsInvalid...\n");
    hr = ptoIntf->Invoke(dispid_IsInvalid, 
    IID_NULL, 
    LOCALE_SYSTEM_DEFAULT, 
    DISPATCH_PROPERTYGET, 
    &DispParams, 
    &VarResult, 
    NULL, 
    NULL);
    if( !CheckInvokeHR(hr) )
    return FALSE;
    else
    printf("\t\t\tCall completed successfully.\n");
    if( VarResult.vt != VT_BOOL || VarResult.boolVal != -1 ) //should be invalid since we closed the handle 
    {
    printf("\t\t\t\tThe return Variant is incorrect!\n");
    return FALSE;
    }
    */
    //////////////////////////////////////////////////////////////////
    printf("\t\t\tInvoking IsClosed...\n");
    hr = ptoIntf->Invoke(dispid_IsClosed, 
        IID_NULL, 
        LOCALE_SYSTEM_DEFAULT, 
        DISPATCH_PROPERTYGET, 
        &DispParams, 
        &VarResult, 
        NULL, 
        NULL);
    if( !CheckInvokeHR(hr) )
        return FALSE;
    else
        printf("\t\t\tCall completed successfully.\n");
    if( VarResult.vt != VT_BOOL || VarResult.boolVal != -1 ) //should be invalid since we closed the handle
    {
        printf("\t\t\t\tThe return Variant is incorrect!\n");
        return FALSE;
    }

    printf("\n");
    /////////////////////////////////////////////////////////////
    ///Invoke SH fields and properties that are defined in SFH class
    ///////////////////////////////////////////////////////////////////
    if( !InvokeAllFldsAndProps(ptoIntf, shfld1Val, shfld2Val) )
        return FALSE;


    return TRUE;
} //end of InvokeAllInSHIntf


/////////////////////////////////Exported Methods
/////////////////////////////////

extern "C" DLL_EXPORT BOOL __stdcall SH_MAIntf(IDispatch* ptoIntf, long shIntfVal, long shfld1Val, long shfld2Val)
{
    printf("\t\tIN SH_MAIntf!\n");

    return InvokeAllInSHIntf(ptoIntf, shIntfVal, shfld1Val, shfld2Val);
}

extern "C" DLL_EXPORT BOOL __stdcall SH_MAIntf_Ref(IDispatch** ptoIntf, long shIntfVal, long shfld1Val, long shfld2Val)
{
    printf("\t\tIN SH_MAIntf_Ref!\n");

    return InvokeAllInSHIntf(*ptoIntf, shIntfVal, shfld1Val, shfld2Val);
}


extern "C" DLL_EXPORT BOOL __stdcall SHFld_MAIntf(StructMAIntf s, long shndVal, long shfld1Val, long shfld2Val)
{
    printf("\t\tIN SHFld_MAIntf!\n");

    return InvokeAllInSHIntf(s.ptoIntf, shndVal, shfld1Val, shfld2Val);
}

extern "C" DLL_EXPORT BOOL __stdcall SHObjectParam(VARIANT v, int shVal, int shfld1Val, int shfld2Val, LPSTR objtype)
{
    if( strcmp(objtype, "DispatchWrapper") == 0 )
    {
        if( v.vt != VT_DISPATCH )
        {
            printf("\tSHObjectParam: v.vt != VT_DISPATCH\n");
            return FALSE;
        }
        else //use the IDispatch pointer to invoke some methods
            return InvokeAllInSHIntf(v.pdispVal, shVal, shfld1Val, shfld2Val);
    }
    else if( strcmp(objtype, "UnknownWrapper") == 0 )
    {
        if( v.vt != VT_UNKNOWN )
        {
            printf("\tSHObjectParam: v.vt != VT_UNKNOWN\n");
            return FALSE;
        }
        else //use the IDispatch pointer to invoke some methods
            return InvokeAllInSHIntf(v.pdispVal, shVal, shfld1Val, shfld2Val);
    }
    else if( strcmp(objtype, "SafeFileHandle") == 0 )
    {
        if( v.vt != VT_DISPATCH ) //if the Object implements IDISPATCH, then it is marshaled to VT_DISPATCH
        {
            printf("\tSHObjectParam: v.vt != VT_DISPATCH\n");
            return FALSE;
        }
        else //use the IUnknown pointer to QI for IDispatch
        {
            HRESULT hr;
            IDispatch* pIDisp;

            //QI for IDispatch pointer to SH
            printf("\tSHObjectParam: QI for IDispatch using IUnknown pointer (v.punkVal)\n");
            hr = v.punkVal->QueryInterface(IID_IDispatch, (void**)&pIDisp);
            if( FAILED(hr) )
            {
                printf("\tSHObjectParam: hr = E_NOINTERFACE\n");
                return FALSE;
            }
            printf("\tSHObjectParam: QI Done\n");

            //use IDispatch pointer obtained from QI
            return InvokeAllInSHIntf(pIDisp, shVal, shfld1Val, shfld2Val);
        }
    }
    else
    {
        printf("\tSHObjectParam: ERROR! String param is not as expected!\n");
        printf("\tSHObjectParam: objtype = %s\n",objtype);
        return FALSE;
    }
}

extern "C" DLL_EXPORT BOOL __stdcall SHStructWithObjectFldParam(StructWithVARIANTFld s, int shVal, int shfld1Val, int shfld2Val, LPSTR objtype)
{
    if( strcmp(objtype, "DispatchWrapper") == 0 )
    {
        if( s.v.vt != VT_DISPATCH )
        {
            printf("\tSHStructWithObjectFldParam: s.v.vt != VT_DISPATCH\n");
            return FALSE;
        }
        else //use the IDispatch pointer to invoke some methods
            return InvokeAllInSHIntf(s.v.pdispVal, shVal, shfld1Val, shfld2Val);
    }
    else if( strcmp(objtype, "UnknownWrapper") == 0 )
    {
        if( s.v.vt != VT_UNKNOWN )
        {
            printf("\tSHStructWithObjectFldParam: s.v.vt != VT_UNKNOWN\n");
            return FALSE;
        }
        else //use the IUnknown pointer to QI for IDispatch
        {
            HRESULT hr;
            IDispatch* pIDisp;

            //QI for IDispatch pointer to SH
            printf("\tSHStructWithObjectFldParam: QI for IDispatch using IUnknown pointer (s.v.punkVal)\n");
            hr = s.v.punkVal->QueryInterface(IID_IDispatch, (void**)&pIDisp);
            if( FAILED(hr) )
            {
                printf("\tSHStructWithObjectFldParam: hr = E_NOINTERFACE\n");
                return FALSE;
            }
            printf("\tSHStructWithObjectFldParam: QI Done\n");

            //use IDispatch pointer obtained from QI
            return InvokeAllInSHIntf(pIDisp, shVal, shfld1Val, shfld2Val);
        }
    }
    else if( strcmp(objtype, "SafeFileHandle") == 0 )
    {
        if( s.v.vt != VT_DISPATCH )
        {
            printf("\tSHStructWithObjectFldParam: s.v.vt != VT_DISPATCH\n");
            return FALSE;
        }
        else //use the IDispatch pointer to invoke some methods
            return InvokeAllInSHIntf(s.v.pdispVal, shVal, shfld1Val, shfld2Val);
    }
    else
    {
        printf("\tSHStructWithObjectFldParam: ERROR! String param is not as expected!\n");
        printf("\tSHStructWithObjectFldParam: objtype = %s\n",objtype);
        return FALSE;
    }
}
