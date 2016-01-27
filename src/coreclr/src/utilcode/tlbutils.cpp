// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Utilities used to help manipulating typelibs.

#include "stdafx.h"                     // Precompiled header key.

#include "tlbutils.h"
#include "dispex.h"
#include "posterror.h"
#include "ndpversion.h"

#define CUSTOM_MARSHALER_ASM ", CustomMarshalers, Version=" VER_ASSEMBLYVERSION_STR ", Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"

static const LPCWSTR        DLL_EXTENSION           = {W(".dll")};
static const int            DLL_EXTENSION_LEN       = 4;
static const LPCWSTR        EXE_EXTENSION           = {W(".exe")};
static const int            EXE_EXTENSION_LEN       = 4;

const StdConvertibleItfInfo aStdConvertibleInterfaces[] = 
{
    { "System.Runtime.InteropServices.Expando.IExpando", (GUID*)&IID_IDispatchEx, 
      "System.Runtime.InteropServices.CustomMarshalers.ExpandoToDispatchExMarshaler" CUSTOM_MARSHALER_ASM, "IExpando" },

    { "System.Reflection.IReflect", (GUID*)&IID_IDispatchEx, 
      "System.Runtime.InteropServices.CustomMarshalers.ExpandoToDispatchExMarshaler" CUSTOM_MARSHALER_ASM, "IReflect" },

    { "System.Collections.IEnumerator", (GUID*)&IID_IEnumVARIANT,
      "System.Runtime.InteropServices.CustomMarshalers.EnumeratorToEnumVariantMarshaler" CUSTOM_MARSHALER_ASM, "" },

    { "System.Type", (GUID*)&IID_ITypeInfo,
      "System.Runtime.InteropServices.CustomMarshalers.TypeToTypeInfoMarshaler" CUSTOM_MARSHALER_ASM, "" },
};

// This method returns the custom marshaler info to convert the native interface
// to its managed equivalent. Or null if the interface is not a standard convertible interface.
const StdConvertibleItfInfo *GetConvertionInfoFromNativeIID(REFGUID rGuidNativeItf)
{
    CONTRACT (const StdConvertibleItfInfo*)
    {
        NOTHROW;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;
    
    // Look in the table of interfaces that have standard convertions to see if the
    // specified interface is there.
    for (int i = 0; i < sizeof(aStdConvertibleInterfaces) / sizeof(StdConvertibleItfInfo); i++)
    {
        if (IsEqualGUID(rGuidNativeItf, *(aStdConvertibleInterfaces[i].m_pNativeTypeIID)))
            RETURN &aStdConvertibleInterfaces[i];
    }

    // The interface is not in the table.
    RETURN NULL;
}

//*****************************************************************************
// Given a typelib, determine the managed namespace name.
//*****************************************************************************
HRESULT GetNamespaceNameForTypeLib(     // S_OK or error.
    ITypeLib    *pITLB,                 // [IN] The TypeLib.
    BSTR        *pwzNamespace)          // [OUT] Put the namespace name here.
{
    CONTRACTL
    {
        DISABLED(NOTHROW);  // PostError goes down a throwing path right now.  Revisit this when fixed.
        PRECONDITION(CheckPointer(pITLB));
        PRECONDITION(CheckPointer(pwzNamespace));
    }
    CONTRACTL_END;
    
    HRESULT     hr = S_OK;              // A result.
    ITypeLib2   *pITLB2=0;              //For getting custom value.
    TLIBATTR    *pAttr=0;               // Typelib attributes.
    BSTR        szPath=0;               // Typelib path.
    
    // If custom attribute for namespace exists, use it.
    if (pITLB->QueryInterface(IID_ITypeLib2, (void **)&pITLB2) == S_OK)
    {
        VARIANT vt;
        VariantInit(&vt);
        if (pITLB2->GetCustData(GUID_ManagedName, &vt) == S_OK)
        {   
            if (V_VT(&vt) == VT_BSTR)
            {   
                // If the namespace ends with .dll then remove the extension.
                LPWSTR pDest = wcsstr(vt.bstrVal, DLL_EXTENSION);
                if (pDest && (pDest[DLL_EXTENSION_LEN] == 0 || pDest[DLL_EXTENSION_LEN] == ' '))
                    *pDest = 0;

                if (!pDest)
                {
                    // If the namespace ends with .exe then remove the extension.
                    pDest = wcsstr(vt.bstrVal, EXE_EXTENSION);
                    if (pDest && (pDest[EXE_EXTENSION_LEN] == 0 || pDest[EXE_EXTENSION_LEN] == ' '))
                        *pDest = 0;
                }

                if (pDest)
                {
                    // We removed the extension so re-allocate a string of the new length.
                    *pwzNamespace = SysAllocString(vt.bstrVal);
                    SysFreeString(vt.bstrVal);
                }
                else
                {
                    // There was no extension to remove so we can use the string returned
                    // by GetCustData().
                    *pwzNamespace = vt.bstrVal;
                }        

                goto ErrExit;
            }
            else
            {
                VariantClear(&vt);
            }
        }
    }
    
    // No custom attribute, use library name.
    IfFailGo(pITLB->GetDocumentation(MEMBERID_NIL, pwzNamespace, 0, 0, 0));

ErrExit:
    if (szPath)
        ::SysFreeString(szPath);
    if (pAttr)
        pITLB->ReleaseTLibAttr(pAttr);
    if (pITLB2)
        pITLB2->Release();
    
    return hr;
} // HRESULT GetNamespaceNameForTypeLib()

//*****************************************************************************
// Given an ITypeInfo, determine the managed name.  Optionally supply a default
//  namespace, otherwise derive namespace from containing typelib.
//*****************************************************************************
HRESULT GetManagedNameForTypeInfo(      // S_OK or error.
    ITypeInfo   *pITI,                  // [IN] The TypeInfo.
    LPCWSTR     wzNamespace,            // [IN, OPTIONAL] Default namespace name.
    LPCWSTR     wzAsmName,              // [IN, OPTIONAL] Assembly name.
    BSTR        *pwzName)               // [OUT] Put the name here.
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        PRECONDITION(CheckPointer(pITI));
        PRECONDITION(CheckPointer(wzNamespace, NULL_OK));
        PRECONDITION(CheckPointer(wzAsmName, NULL_OK));
        PRECONDITION(CheckPointer(pwzName));
    }
    CONTRACTL_END;
    
    HRESULT     hr = S_OK;              // A result.
    ITypeInfo2  *pITI2=0;               // For getting custom value.
    ITypeLib    *pITLB=0;               // Containing typelib.
    
    BSTR        bstrName=0;             // Typeinfo's name.
    BSTR        bstrNamespace=0;        // Typelib's namespace.
    int         cchFullyQualifiedName;  // Size of namespace + name buffer.
    int         cchAsmName=0;           // The size of the assembly name.
    int         cchAsmQualifiedName=0;  // The size of the assembly qualified name buffer.
    CQuickArray<WCHAR> qbFullyQualifiedName;  // The fully qualified type name.  

    // Check for a custom value with name.
    if (pITI->QueryInterface(IID_ITypeInfo2, (void **)&pITI2) == S_OK)
    {
        VARIANT     vt;                     // For getting custom value.
        ::VariantInit(&vt);
        if (pITI2->GetCustData(GUID_ManagedName, &vt) == S_OK && vt.vt == VT_BSTR)
        {   // There is a custom value with the name.  Just believe it.
            *pwzName = vt.bstrVal;
            vt.bstrVal = 0;
            vt.vt = VT_EMPTY;
            goto ErrExit;
        }
    }
    
    // Still need name, get the namespace.
    if (wzNamespace == 0)
    {
        IfFailGo(pITI->GetContainingTypeLib(&pITLB, 0));
        IfFailGo(GetNamespaceNameForTypeLib(pITLB, &bstrNamespace));
        wzNamespace = bstrNamespace;
    }
    
    // Get the name, and combine with namespace.
    IfFailGo(pITI->GetDocumentation(MEMBERID_NIL, &bstrName, 0,0,0));
    cchFullyQualifiedName = (int)(wcslen(bstrName) + wcslen(wzNamespace) + 1);
    IfFailGo(qbFullyQualifiedName.ReSizeNoThrow(cchFullyQualifiedName + 1));
    ns::MakePath(qbFullyQualifiedName.Ptr(), cchFullyQualifiedName + 1, wzNamespace, bstrName);

    // If the assembly name is specified, then add it to the type name.
    if (wzAsmName)
    {
        cchAsmName = (int)wcslen(wzAsmName);
        cchAsmQualifiedName = cchFullyQualifiedName + cchAsmName + 3;
        IfNullGo(*pwzName = ::SysAllocStringLen(0, cchAsmQualifiedName));
        ns::MakeAssemblyQualifiedName(*pwzName, cchAsmQualifiedName, qbFullyQualifiedName.Ptr(), cchFullyQualifiedName, wzAsmName, cchAsmName);
    }
    else
    {
        IfNullGo(*pwzName = ::SysAllocStringLen(qbFullyQualifiedName.Ptr(), cchFullyQualifiedName));
    }

ErrExit:
    if (bstrName)
        ::SysFreeString(bstrName);
    if (bstrNamespace)
        ::SysFreeString(bstrNamespace);
    if (pITLB)
        pITLB->Release();
    if (pITI2)
        pITI2->Release();

    return (hr);
} // HRESULT GetManagedNameForTypeInfo()
