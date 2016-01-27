// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// winrthelpers.inl
//

//
// Helpers to fetch the first WinRT Type def from metadata import
// 
// ======================================================================================

#include "common.h"

// --------------------------------------------------------------------------------------
// Return the first public WinRT type's namespace and typename - the names have the lifetime of the MetaData scope.
// 
//static 
HRESULT GetFirstWinRTTypeDef(
    IMDInternalImport * pMDInternalImport, 
    LPCSTR *            pszNameSpace,       // Tight to the lifetime of pssFakeNameSpaceAllocationBuffer when the WinMD file is empty
    LPCSTR *            pszTypeName, 
    LPCWSTR             wszAssemblyPath,    // Used for creating fake binding type name in case the WinMD file is empty
    SString *           pssFakeNameSpaceAllocationBuffer)   // Used as allocation buffer for fake namespace
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    _ASSERTE((wszAssemblyPath == NULL) || (pssFakeNameSpaceAllocationBuffer != NULL));
    
    static const char const_szWinRTPrefix[] = "<WinRT>";
    
    HRESULT hr = S_OK;
    HENUMInternalHolder hEnum(pMDInternalImport);
    mdToken tk;
    
    hEnum.EnumTypeDefInit();
    
    while (pMDInternalImport->EnumTypeDefNext(&hEnum, &tk))
    {
        DWORD dwAttr;
        IfFailRet(pMDInternalImport->GetTypeDefProps(tk, &dwAttr, NULL));
        if (IsTdPublic(dwAttr) && IsTdWindowsRuntime(dwAttr))
        {
            IfFailRet(pMDInternalImport->GetNameOfTypeDef(tk, pszTypeName, pszNameSpace));
            return hr;
        }
    }
    
    // We didn't find any public Windows runtime types.  In the case of 1st party WinMDs, this means
    // it's not exporting anything so we really cannot bind to it.
    // For WinMDs built with WinMDExp, it's because the adapter has promoted the CLR implementation to
    // public (no WindowsRuntime flag, though), and made the WinRT copy private.
    // So there should exist a public type (not nested, not an interface), which has a corresponding
    // private type with the same name prepended with <WinRT> that is marked as windows runtime
    // This isn't very efficient O(n^2) but we expect all public types in WinMDs to have WinRT visible
    // versions too so it should early out in the first iteration in almost all cases.
    HENUMInternalHolder hEnum2(pMDInternalImport);
    hEnum2.EnumTypeDefInit();
    
    while (pMDInternalImport->EnumTypeDefNext(&hEnum2, &tk))
    {
        DWORD dwAttr;
        IfFailRet(pMDInternalImport->GetTypeDefProps(tk, &dwAttr, NULL));
        if (IsTdPublic(dwAttr) && !IsTdInterface(dwAttr))
        {
            // Look for a matching private windows runtime type
            mdToken tkPrivate;
            HENUMInternalHolder hSubEnum(pMDInternalImport);
            
            LPCSTR szNameSpace = NULL;
            LPCSTR szName = NULL;
            IfFailRet(pMDInternalImport->GetNameOfTypeDef(tk, &szName, &szNameSpace));
            
            hSubEnum.EnumTypeDefInit();
            
            while (pMDInternalImport->EnumTypeDefNext(&hSubEnum, &tkPrivate))
            {
                DWORD dwSubAttr;
                IfFailRet(pMDInternalImport->GetTypeDefProps(tkPrivate, &dwSubAttr, NULL));
                if (IsTdNotPublic(dwSubAttr) && IsTdWindowsRuntime(dwSubAttr))
                {
                    LPCSTR szSubNameSpace = NULL;
                    LPCSTR szSubName = NULL;
                    IfFailRet(pMDInternalImport->GetNameOfTypeDef(tkPrivate, &szSubName, &szSubNameSpace));
                    if (!strncmp(szSubName, const_szWinRTPrefix, strlen(const_szWinRTPrefix)))
                    {
                        szSubName += strlen(const_szWinRTPrefix);
                        // Skip over the <WinRT> prefix.  Now pointing at type name
                        if (!strcmp(szSubNameSpace, szNameSpace) &&
                            !strcmp(szSubName, szName))
                        {
                            *pszNameSpace = szNameSpace;
                            *pszTypeName = szName;
                            return S_OK;
                        }
                    }
                }
            }
        }
    }
    // The .winmd file is empty - i.e. there is no type we can bind to

    if ((wszAssemblyPath != NULL) && (*wszAssemblyPath != 0))
    {   // Create fake name for WinMD binding purposes (used when .winmd file is loaded by file path - ngen, NativeBinder, etc.)
        // We will use WinMD file name as namespace and use fake hardcoded type name
        SString ssAssemblyPath(wszAssemblyPath);
        SString ssAssemblyName;
        SplitPath(ssAssemblyPath, 
                  NULL,     // drive
                  NULL,     // dir
                  &ssAssemblyName,  // name
                  NULL);    // ext
        if (!ssAssemblyName.IsEmpty())
        {
            *pszTypeName = "FakeTypeNameForCLRBinding";
            ssAssemblyName.ConvertToUTF8(*pssFakeNameSpaceAllocationBuffer);
            *pszNameSpace = pssFakeNameSpaceAllocationBuffer->GetUTF8NoConvert();
            return S_OK;
        }
    }
    
    return CLR_E_BIND_TYPE_NOT_FOUND;
} // GetFirstWinRTTypeDef

// --------------------------------------------------------------------------------------
//static 
HRESULT 
GetBindableWinRTName(
    IMDInternalImport * pMDInternalImport, 
    IAssemblyName *     pIAssemblyName)
{
    STANDARD_VM_CONTRACT;
    
    HRESULT hr = S_OK;
    
    LPCSTR  szNameSpace;
    LPCSTR  szTypeName;
    
    // Note: This function is used only by native binder which does not support empty WinMDs - see code:CEECompileInfo::LoadAssemblyByPath
    // Therefore we do not have to use file name to create fake type name
    IfFailRet(GetFirstWinRTTypeDef(pMDInternalImport, &szNameSpace, &szTypeName, NULL, NULL));
    
    DWORD dwSize = MAX_PATH_FNAME;
    WCHAR wzAsmName[MAX_PATH_FNAME];
    
    dwSize = MAX_PATH_FNAME * sizeof(WCHAR);
    IfFailRet(pIAssemblyName->GetProperty(ASM_NAME_NAME, wzAsmName, &dwSize));

    StackSString sNamespaceAndType(wzAsmName);
    sNamespaceAndType.Append(W("!"));
    sNamespaceAndType.AppendUTF8(szNameSpace);
    sNamespaceAndType.Append(W("."));
    sNamespaceAndType.AppendUTF8(szTypeName);
    
    pIAssemblyName->SetProperty(ASM_NAME_NAME, sNamespaceAndType.GetUnicode(), (lstrlenW(sNamespaceAndType.GetUnicode()) + 1) * sizeof(WCHAR));
    
    return hr;
}
