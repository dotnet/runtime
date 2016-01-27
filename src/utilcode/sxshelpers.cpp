// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// 
//  sxshelpers.cpp
//
//  Some helping classes and methods for SxS in mscoree and mscorwks
//

//*****************************************************************************

#include "stdafx.h"
#include "utilcode.h"
#include "sxshelpers.h"

#define SXS_GUID_INFORMATION_CLR_FLAG_IS_SURROGATE          (0x00000001)
#define SXS_GUID_INFORMATION_CLR_FLAG_IS_CLASS              (0x00000002)

typedef struct _SXS_GUID_INFORMATION_CLR
{
    DWORD       cbSize;
    DWORD       dwFlags;
    PCWSTR      pcwszRuntimeVersion;
    PCWSTR      pcwszTypeName;
    PCWSTR      pcwszAssemblyIdentity;
} SXS_GUID_INFORMATION_CLR, *PSXS_GUID_INFORMATION_CLR;
typedef const SXS_GUID_INFORMATION_CLR *PCSXS_GUID_INFORMATION_CLR;

#define SXS_LOOKUP_CLR_GUID_USE_ACTCTX                      (0x00000001)
#define SXS_LOOKUP_CLR_GUID_FIND_SURROGATE                  (0x00010000)
#define SXS_LOOKUP_CLR_GUID_FIND_CLR_CLASS                  (0x00020000)
#define SXS_LOOKUP_CLR_GUID_FIND_ANY                        (SXS_LOOKUP_CLR_GUID_FIND_CLR_CLASS | SXS_LOOKUP_CLR_GUID_FIND_SURROGATE)

#define SXS_DLL_NAME_W                                      (W("sxs.dll"))
#define SXS_LOOKUP_CLR_GUID                                 ("SxsLookupClrGuid")

typedef BOOL (WINAPI* PFN_SXS_LOOKUP_CLR_GUID)(
    IN DWORD       dwFlags,
    IN LPGUID      pClsid,
    IN HANDLE      hActCtx,
    IN OUT PVOID   pvOutputBuffer,
    IN SIZE_T      cbOutputBuffer,
    OUT PSIZE_T    pcbOutputBuffer
    );

// forward declaration
BOOL TranslateWin32AssemblyIdentityToFusionDisplayName(__deref_out_z LPWSTR *ppwzFusionDisplayName, PCWSTR lpWin32AssemblyIdentity);

// The initial size of the buffer passed to SxsLookupClrGuid.
#define INIT_GUID_LOOKUP_BUFFER_SIZE 512

// Function pointer to the function to lookup a CLR type by GUID in the unmanaged
// fusion activation context.
PFN_SXS_LOOKUP_CLR_GUID g_pfnLookupGuid = NULL;
Volatile<BOOL> g_fSxSInfoInitialized = FALSE;

HMODULE g_hmSxsDll = NULL;

// And Here are the functions for getting shim info from 
// Win32 activation context

//  FindShimInfoFromWin32
//
//  This method is used in ComInterop. If a COM client calls 
//  CoCreateInstance on a managed COM server, we will use this method
//  trying to find required info of the managed COM server from Win32 subsystem.
//  If this fails, we will fall back to query the registry. 
//
//  Parameters:
//      rclsid:              [in]  The CLSID of the managed COM server
//      bLoadRecord:         [in]  Set to TRUE if we are looking for a record
//      *ppwzRuntimeVersion: [out] Runtime version
//      *ppwzClassName:      [out] Class name
//      *ppwzAssemblyString: [out] Assembly display name
//      *pfRegFreePIA:       [out] TRUE if the entry is <clrSurrogate>
//  Return:
//      FAILED(hr) if cannot find shim info from Win32
//      SUCCEEDED(HR) if shim info is found from Win32

HRESULT
FindShimInfoFromWin32(
    REFCLSID rClsid,
    BOOL bLoadRecord, 
    __deref_out_z __deref_opt_out_opt LPWSTR *ppwszRuntimeVersion,
    __deref_out_z __deref_opt_out_opt LPWSTR *ppwszSupportedRuntimeVersions,
    __deref_out_z __deref_opt_out_opt LPWSTR *ppwszClassName,
    __deref_out_z __deref_opt_out_opt LPWSTR *ppwszAssemblyString,
    BOOL *pfRegFreePIA
    )
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FAULT;

    CQuickBytes rDataBuffer;
    SIZE_T cbWritten;
    HRESULT hr = S_OK;
    PCSXS_GUID_INFORMATION_CLR pFoundInfo = NULL;
    SIZE_T cch;
    GUID MyGuid = rClsid;
    DWORD dwFlags = bLoadRecord ? SXS_LOOKUP_CLR_GUID_FIND_SURROGATE : SXS_LOOKUP_CLR_GUID_FIND_ANY;

    if (!ppwszRuntimeVersion && !ppwszClassName && !ppwszAssemblyString)
        IfFailGo(E_INVALIDARG);

    if (ppwszRuntimeVersion)
        *ppwszRuntimeVersion = NULL;

    if (ppwszSupportedRuntimeVersions)
        *ppwszSupportedRuntimeVersions = NULL;

    if (ppwszClassName)
        *ppwszClassName = NULL;

    if (ppwszAssemblyString)
        *ppwszAssemblyString = NULL;

    if (pfRegFreePIA)
        *pfRegFreePIA = FALSE;

    // If we haven't initialized the SxS info yet, then do so now.
    if (!g_fSxSInfoInitialized)
    {
        if (g_hmSxsDll == NULL)
            g_hmSxsDll = WszLoadLibrary(SXS_DLL_NAME_W);
        
        if (g_hmSxsDll != NULL)
        {
            // Lookup the SxsLookupClrGuid function in the SxS DLL.
            g_pfnLookupGuid = (PFN_SXS_LOOKUP_CLR_GUID)GetProcAddress(g_hmSxsDll, SXS_LOOKUP_CLR_GUID);
        }

        // The SxS info has been initialized.
        g_fSxSInfoInitialized = TRUE;
    }

    // If we don't have the proc address of SxsLookupClrGuid, then return a failure.
    if (g_pfnLookupGuid == NULL)
        IfFailGo(E_FAIL);

    // Resize the CQuickBytes to the initial buffer size.
    IfFailGo(rDataBuffer.ReSizeNoThrow(INIT_GUID_LOOKUP_BUFFER_SIZE));

    if (!g_pfnLookupGuid(dwFlags, &MyGuid, INVALID_HANDLE_VALUE, rDataBuffer.Ptr(), rDataBuffer.Size(), &cbWritten))
    {
        const DWORD dwLastError = ::GetLastError();

        // Failed b/c we need more space? Expand and try again.
        if (dwLastError == ERROR_INSUFFICIENT_BUFFER) 
        {
            IfFailGo(rDataBuffer.ReSizeNoThrow(cbWritten));

            // Still failed even with enough space? Bummer.
            if (!g_pfnLookupGuid(dwFlags, &MyGuid, INVALID_HANDLE_VALUE, rDataBuffer.Ptr(), rDataBuffer.Size(), &cbWritten))
                IfFailGo(E_FAIL);
        }
        // All other failures are real failures - probably the section isn't present
        // or some other problem.
        else
        {
            IfFailGo(E_FAIL);
        }
    }

    pFoundInfo = (PCSXS_GUID_INFORMATION_CLR)rDataBuffer.Ptr();

    if (pFoundInfo->dwFlags == SXS_GUID_INFORMATION_CLR_FLAG_IS_SURROGATE && ppwszRuntimeVersion)
    {
        // Surrogate does not have runtime version information !!!
        IfFailGo(E_FAIL);
    }

    //
    // This is special - translate the win32 assembly name into a managed
    // assembly identity.
    //
    if (ppwszAssemblyString && pFoundInfo->pcwszAssemblyIdentity)
    {
        if (!TranslateWin32AssemblyIdentityToFusionDisplayName(ppwszAssemblyString, pFoundInfo->pcwszAssemblyIdentity))
            IfFailGo(E_FAIL);
    }    

    //
    // For each field, allocate the outbound pointer and call through.
    //
    if (ppwszClassName && pFoundInfo->pcwszTypeName)
    {
        cch = wcslen(pFoundInfo->pcwszTypeName);

        if (cch > 0)
        {
            IfNullGo(*ppwszClassName = new (nothrow) WCHAR[cch + 1]);
            wcscpy_s(*ppwszClassName, cch+1, pFoundInfo->pcwszTypeName);
        }
        else
            IfFailGo(E_FAIL);
    }    

    if (ppwszRuntimeVersion)
    {
        if (pFoundInfo->pcwszRuntimeVersion && (cch = wcslen(pFoundInfo->pcwszRuntimeVersion)) > 0)
        {
            IfNullGo(*ppwszRuntimeVersion = new (nothrow) WCHAR[cch + 1]);
            wcscpy_s(*ppwszRuntimeVersion, cch+1, pFoundInfo->pcwszRuntimeVersion);
        }
        else
        {
            // Sxs.dll returns empty string even when the runtimeVersion attribute is missing so
            // we cannot tell whether it's not there or is empty. We'll return 1.0 in both cases.
            //
            // The goal is to emulate pre-4.0 behavior where this function wasn't called with
            // non-NULL ppwszRuntimeVersion on the COM activation path at all so shim loaded the
            // latest runtime on the machine.
            IfNullGo(*ppwszRuntimeVersion = DuplicateString(V1_VERSION_NUM));
        }
    }    

    if (pfRegFreePIA)
    {
        *pfRegFreePIA = (pFoundInfo->dwFlags == SXS_GUID_INFORMATION_CLR_FLAG_IS_SURROGATE);
    }

ErrExit:
    //
    // Deallocate in case of failure
    //
    if (FAILED(hr))
    {
        if (ppwszRuntimeVersion && *ppwszRuntimeVersion)
        {
            delete [] *ppwszRuntimeVersion;
            *ppwszRuntimeVersion = NULL;
        }
        if (ppwszAssemblyString && *ppwszAssemblyString)
        {
            delete [] *ppwszAssemblyString;
            *ppwszAssemblyString = NULL;
        }
        if (ppwszClassName && *ppwszClassName)
        {
            delete [] *ppwszClassName;
            *ppwszClassName = NULL;
        }
    }

    return hr;
}

// TranslateWin32AssemblyIdentityToFusionDisplayName
//
// Culture info is missing in the assemblyIdentity returned from win32,
// So Need to do a little more work here to get the correct fusion display name
//
// replace "language=" in assemblyIdentity to "culture=" if any.
// If "language=" is not present in assemblyIdentity, add "culture=neutral" 
// to it.
//
// Also check other attributes as well. 
//
// Parameters:
//     ppwzFusionDisplayName: the corrected output of assembly displayname
//     lpWin32AssemblyIdentity: input assemblyIdentity returned from win32
//
// returns:
//     TRUE if the conversion is done.
//     FALSE otherwise

BOOL TranslateWin32AssemblyIdentityToFusionDisplayName(__deref_out_z LPWSTR *ppwzFusionDisplayName, PCWSTR lpWin32AssemblyIdentity)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FAULT;

    size_t size = 0;
    LPWSTR lpAssemblyIdentityCopy = NULL;
    LPWSTR lpVersionKey = W("version=");
    LPWSTR lpPublicKeyTokenKey = W("publickeytoken=");
    LPWSTR lpCultureKey = W("culture=");
    LPWSTR lpNeutral = W("neutral");
    LPWSTR lpLanguageKey = W("language=");
    LPWSTR lpMatch = NULL;
    LPWSTR lpwzFusionDisplayName = NULL;
    
    if (ppwzFusionDisplayName == NULL) return FALSE;
    *ppwzFusionDisplayName = NULL;
    
    if (lpWin32AssemblyIdentity == NULL) return FALSE;

    size = wcslen(lpWin32AssemblyIdentity);
    if (size == 0) return FALSE;

    // make a local copy
    lpAssemblyIdentityCopy = new (nothrow) WCHAR[size+1];
    if (!lpAssemblyIdentityCopy)
        return FALSE;

    wcscpy_s(lpAssemblyIdentityCopy, size+1, lpWin32AssemblyIdentity);

    // convert to lower case
    _wcslwr_s(lpAssemblyIdentityCopy, size+1);

    // check if "version" key is presented
    if (!wcsstr(lpAssemblyIdentityCopy, lpVersionKey))
    {
        // version is not presented, append it
        size += wcslen(lpVersionKey)+8; // length of ","+"0.0.0.0"
        lpwzFusionDisplayName = new (nothrow) WCHAR[size+1];
        if (!lpwzFusionDisplayName)
        {
            // clean up
            delete[] lpAssemblyIdentityCopy;
            return FALSE;
        }

        //copy old one
        wcscpy_s(lpwzFusionDisplayName, size+1, lpAssemblyIdentityCopy);
        wcscat_s(lpwzFusionDisplayName, size+1, W(","));
        wcscat_s(lpwzFusionDisplayName, size+1, lpVersionKey);
        wcscat_s(lpwzFusionDisplayName, size+1, W("0.0.0.0"));

        // delete the old copy
        delete[] lpAssemblyIdentityCopy;

        // lpAssemblyIdentityCopy has the new copy
        lpAssemblyIdentityCopy = lpwzFusionDisplayName;
        lpwzFusionDisplayName = NULL;
    }

    // check if "publickeytoken" key is presented
    if (!wcsstr(lpAssemblyIdentityCopy, lpPublicKeyTokenKey))
    {
        // publickeytoken is not presented, append it
        size += wcslen(lpPublicKeyTokenKey)+5; //length of ","+"null"
        lpwzFusionDisplayName = new (nothrow) WCHAR[size+1];
        if (!lpwzFusionDisplayName)
        {
            // clean up
            delete[] lpAssemblyIdentityCopy;
            return FALSE;
        }

        // copy the old one
        wcscpy_s(lpwzFusionDisplayName, size+1, lpAssemblyIdentityCopy);
        wcscat_s(lpwzFusionDisplayName, size+1, W(","));
        wcscat_s(lpwzFusionDisplayName, size+1, lpPublicKeyTokenKey);
        wcscat_s(lpwzFusionDisplayName, size+1, W("null"));

        // delete the old copy
        delete[] lpAssemblyIdentityCopy;

        // lpAssemblyIdentityCopy has the new copy
        lpAssemblyIdentityCopy = lpwzFusionDisplayName;
        lpwzFusionDisplayName = NULL;
    }
    
    if (wcsstr(lpAssemblyIdentityCopy, lpCultureKey))
    {
        // culture info is already included in the assemblyIdentity
        // nothing need to be done
        lpwzFusionDisplayName = lpAssemblyIdentityCopy;
        *ppwzFusionDisplayName = lpwzFusionDisplayName;
        return TRUE;
    }

    if ((lpMatch = wcsstr(lpAssemblyIdentityCopy, lpLanguageKey)) !=NULL )
    {
        // language info is included in the assembly identity
        // need to replace it with culture
        
        // final size 
        size += wcslen(lpCultureKey)-wcslen(lpLanguageKey);
        lpwzFusionDisplayName = new (nothrow) WCHAR[size + 1];
        if (!lpwzFusionDisplayName)
        {
            // clean up
            delete[] lpAssemblyIdentityCopy;
            return FALSE;
        }
        wcsncpy_s(lpwzFusionDisplayName, size+1, lpAssemblyIdentityCopy, lpMatch-lpAssemblyIdentityCopy);
        lpwzFusionDisplayName[lpMatch-lpAssemblyIdentityCopy] = W('\0');
        wcscat_s(lpwzFusionDisplayName, size+1, lpCultureKey);
        wcscat_s(lpwzFusionDisplayName, size+1, lpMatch+wcslen(lpLanguageKey));
        *ppwzFusionDisplayName = lpwzFusionDisplayName;
        
        // clean up
        delete[] lpAssemblyIdentityCopy;
        return TRUE;
    }
    else 
    {
        // neither culture or language key is presented
        // let us attach culture info key to the identity
        size += wcslen(lpCultureKey)+wcslen(lpNeutral)+1;
        lpwzFusionDisplayName = new (nothrow) WCHAR[size + 1];
        if (!lpwzFusionDisplayName)
        {
            // clean up
            delete[] lpAssemblyIdentityCopy;
            return FALSE;
        }
            
        wcscpy_s(lpwzFusionDisplayName, size+1, lpAssemblyIdentityCopy);
        wcscat_s(lpwzFusionDisplayName, size+1, W(","));
        wcscat_s(lpwzFusionDisplayName, size+1, lpCultureKey);
        wcscat_s(lpwzFusionDisplayName, size+1, lpNeutral);
        *ppwzFusionDisplayName = lpwzFusionDisplayName;

        // clean up
        delete[] lpAssemblyIdentityCopy;
        return TRUE;
    }
}

//****************************************************************************
//  AssemblyVersion
//  
//  class to handle assembly version
//  Since only functions in this file will use it,
//  we declare it in the cpp file so other people won't use it.
//
//****************************************************************************

// Extract version info from pwzVersion, expecting "a.b.c.d",
// where a,b,c and d are all digits.
HRESULT AssemblyVersion::Init(__in_z LPCWSTR pcwzVersion, BOOL bStartsWithV)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FAULT;

    HRESULT hr = S_OK;
    LPWSTR  pwzVersionCopy = NULL;
    LPWSTR  pwzTokens = NULL;
    LPWSTR  pwzToken = NULL;
    size_t  size = 0;
    int iVersion = 0;

    if ((pcwzVersion == NULL) || (*pcwzVersion == W('\0')))
        IfFailGo(E_INVALIDARG);

    // If fStartsWithV is true, then the version string should start with a v.
    // Verify this and if it is the case, start tokenizing at the character after the v.
    if (bStartsWithV)
    {
        if (*pcwzVersion == W('v') || *pcwzVersion == W('V'))
            pcwzVersion++;
        else
            IfFailGo(E_INVALIDARG);
    }

    IfFailGo(ValidateVersion(pcwzVersion));
    
    size = wcslen(pcwzVersion);
    
    IfNullGo(pwzVersionCopy = new (nothrow) WCHAR[size + 1]);
   
    wcscpy_s(pwzVersionCopy, size+1, pcwzVersion);
    pwzTokens = pwzVersionCopy;
    
    // parse major version
    pwzToken = wcstok_s(pwzTokens, W("."),&pwzTokens);
    if (pwzToken != NULL)
    {
        iVersion = _wtoi(pwzToken);
        if (iVersion > 0xffff)
            IfFailGo(E_INVALIDARG);
        _major = (WORD)iVersion;
    }

    // parse minor version
    pwzToken = wcstok_s(pwzTokens, W("."),&pwzTokens);
    if (pwzToken != NULL)
    {
        iVersion = _wtoi(pwzToken);
        if (iVersion > 0xffff)
            IfFailGo(E_INVALIDARG);
        _minor = (WORD)iVersion;
    }

    // parse build version
    pwzToken = wcstok_s(pwzTokens, W("."),&pwzTokens);
    if (pwzToken != NULL)
    {
        iVersion = _wtoi(pwzToken);
        if (iVersion > 0xffff)
            IfFailGo(E_INVALIDARG);
        _build = (WORD)iVersion;
    }

    // parse revision version
    pwzToken = wcstok_s(pwzTokens, W("."),&pwzTokens);
    if (pwzToken != NULL)
    {
        iVersion = _wtoi(pwzToken);
        if (iVersion > 0xffff)
            IfFailGo(E_INVALIDARG);
        _revision = (WORD)iVersion;
    }
   
ErrExit:
    if (pwzVersionCopy)
        delete[] pwzVersionCopy;
    return hr;
}

// pcwzVersion must be in format of a.b.c.d, where a, b, c, d are numbers
HRESULT AssemblyVersion::ValidateVersion(LPCWSTR pcwzVersion)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FAULT;

    LPCWSTR   pwCh = pcwzVersion;
    INT       dots = 0; // number of dots
    BOOL      bIsDot = FALSE; // is previous char a dot?

    // first char cannot be .
    if (*pwCh == W('.'))
        return E_INVALIDARG;
    
    for(;*pwCh != W('\0');pwCh++)
    {
        if (*pwCh == W('.'))
        {
            if (bIsDot) // ..
                return E_INVALIDARG;
            else 
            {
                dots++;
                bIsDot = TRUE;
            }
        }
        /*
        // We can't do this sort of validation, because then our v1.2.x86chk version numbers will be invalid
        else if (!iswdigit(*pwCh))
            return E_INVALIDARG;
        */
        else
            bIsDot = FALSE;
    }

    if (dots > 3)
        return E_INVALIDARG;

    return S_OK;
}

BOOL operator==(const AssemblyVersion& version1, 
                const AssemblyVersion& version2)
{
    LIMITED_METHOD_CONTRACT;

    return ((version1._major == version2._major)
            && (version1._minor == version2._minor)
            && (version1._build == version2._build)
            && (version1._revision == version2._revision));
}

BOOL operator>=(const AssemblyVersion& version1,
                const AssemblyVersion& version2)
{
    LIMITED_METHOD_CONTRACT;

    ULONGLONG ulVersion1;
    ULONGLONG ulVersion2;

    ulVersion1 = version1._major;
    ulVersion1 = (ulVersion1<<16)|version1._minor;
    ulVersion1 = (ulVersion1<<16)|version1._build;
    ulVersion1 = (ulVersion1<<16)|version1._revision;

    ulVersion2 = version2._major;
    ulVersion2 = (ulVersion2<<16)|version2._minor;
    ulVersion2 = (ulVersion2<<16)|version2._build;
    ulVersion2 = (ulVersion2<<16)|version2._revision;

    return (ulVersion1 >= ulVersion2);
}

enum RegistryBasePath
{
    RegistryBasePath_Record,
    RegistryBasePath_CLSID_InprocServer32,
    RegistryBasePath_CLSID_LocalServer32_32Key,
    RegistryBasePath_CLSID_LocalServer32_64Key,
};

// Find which subkey has the highest verion
// If return S_OK, *ppwzHighestVersion has the highest version string.
//      *pbIsTopKey indicates if top key is the one with highest version.
// If return S_FALSE, cannot find any version. *ppwzHighestVersion is set
//      to NULL, and *pbIsTopKey is TRUE.
// If failed, *ppwzHighestVersion will be set to NULL, and *pbIsTopKey is 
// undefined.
// Note: If succeeded, this function will allocate memory for *ppwzVersion. 
//      Caller is responsible to release them
HRESULT FindHighestVersion(REFCLSID rclsid, RegistryBasePath basePath, AssemblyVersion *prvHighestAllowed, 
                           __deref_out_z LPWSTR *ppwzHighestVersion, BOOL *pbIsTopKey, BOOL *pbIsUnmanagedObject)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FAULT;

    HRESULT     hr = S_OK;
    WCHAR       szID[64];
    WCHAR       clsidKeyname[128];
    WCHAR       wzSubKeyName[32]; 
    DWORD       cwSubKeySize;
    DWORD       dwIndex;          // subkey index
    HKEY        hKeyCLSID = NULL;
    HKEY        hSubKey = NULL;
    DWORD       type;
    DWORD       size;
    BOOL        bIsTopKey = FALSE;   // Does top key have the highest version?
    BOOL        bGotVersion = FALSE; // Do we get anything out of registry?
    LONG        lResult;
    LPWSTR      wzAssemblyString = NULL;
    DWORD       numSubKeys = 0;
    AssemblyVersion avHighest; WCHAR wzHighest[32];
    AssemblyVersion avCurrent; WCHAR wzCurrent[32];

    _ASSERTE(pbIsUnmanagedObject != NULL);
    *pbIsUnmanagedObject = FALSE;

    if ((ppwzHighestVersion == NULL) || (pbIsTopKey == NULL))
        IfFailGo(E_INVALIDARG);

    *ppwzHighestVersion = NULL;
    *pbIsTopKey = FALSE;

    if (!GuidToLPWSTR(rclsid, szID, NumItems(szID))) 
        IfFailGo(E_INVALIDARG);

    if (basePath == RegistryBasePath_Record)
    {
        wcscpy_s(clsidKeyname, 128, W("Record\\"));
        wcscat_s(clsidKeyname, 128, szID);
    }
    else
    {
        wcscpy_s(clsidKeyname, 128, W("CLSID\\"));
        wcscat_s(clsidKeyname, 128, szID);
        if (basePath == RegistryBasePath_CLSID_InprocServer32)
        {
            wcscat_s(clsidKeyname, 128, W("\\InprocServer32"));
        }
        else
        {
            _ASSERTE(basePath == RegistryBasePath_CLSID_LocalServer32_32Key || basePath == RegistryBasePath_CLSID_LocalServer32_64Key);
            wcscat_s(clsidKeyname, 128, W("\\LocalServer32"));
        }
    }

    // Open HKCR\CLSID\<clsid> , or HKCR\Record\<RecordId>
    REGSAM accessFlags = KEY_ENUMERATE_SUB_KEYS | KEY_READ;
    if (basePath == RegistryBasePath_CLSID_LocalServer32_32Key)
    {
        // open the WoW key
        accessFlags |= KEY_WOW64_32KEY;
    }
    else if (basePath == RegistryBasePath_CLSID_LocalServer32_64Key)
    {
        // open the 64-bit key
        accessFlags |= KEY_WOW64_64KEY;
    }

    IfFailWin32Go(WszRegOpenKeyEx(
                    HKEY_CLASSES_ROOT,
                    clsidKeyname,
                    0, 
                    accessFlags,
                    &hKeyCLSID));


    //
    // Start by looking for a version subkey.
    //

    IfFailWin32Go(WszRegQueryInfoKey(hKeyCLSID, NULL, NULL, NULL,
                  &numSubKeys, NULL, NULL, NULL, NULL, NULL, NULL, NULL));
    
    for ( dwIndex = 0; dwIndex < numSubKeys;  dwIndex++)
    {
        cwSubKeySize = NumItems(wzSubKeyName);
        
        IfFailWin32Go(WszRegEnumKeyEx(hKeyCLSID, //HKCR\CLSID\<clsid>\InprocServer32
                        dwIndex,             // which subkey
                        wzSubKeyName,        // subkey name
                        &cwSubKeySize,       // size of subkey name
                        NULL,                // lpReserved
                        NULL,                // lpClass
                        NULL,                // lpcbClass
                        NULL));              // lpftLastWriteTime
       
        hr = avCurrent.Init(wzSubKeyName, FALSE);
        if (FAILED(hr))
        {
            // not valid version subkey, ignore
            continue;
        }
        wcscpy_s(wzCurrent, COUNTOF(wzCurrent), wzSubKeyName);

        IfFailWin32Go(WszRegOpenKeyEx(
                    hKeyCLSID,
                    wzSubKeyName,
                    0,
                    accessFlags,
                    &hSubKey));

        // Check if this is a non-interop scenario
        lResult = WszRegQueryValueEx(
                        hSubKey,
                        SBSVERSIONVALUE,
                        NULL,
                        &type,
                        NULL,
                        &size);  
        if (lResult == ERROR_SUCCESS)
        {
            *pbIsUnmanagedObject = TRUE;
        }
        // This is an interop assembly
        else
        {
            lResult = WszRegQueryValueEx(
                            hSubKey,
                            W("Assembly"),
                            NULL,
                            &type,
                            NULL,
                            &size);  
            if (!((lResult == ERROR_SUCCESS)&&(type == REG_SZ)&&(size > 0)))
            {
                // do not have value "Assembly"
                RegCloseKey(hSubKey);
                hSubKey = NULL;
                continue;
            }

            lResult = WszRegQueryValueEx(
                            hSubKey,
                            W("Class"),
                            NULL,
                            &type,
                            NULL,
                            &size);
            if (!((lResult == ERROR_SUCCESS)&&(type == REG_SZ)&&(size > 0)))
            {
                // do not have value "Class"
                RegCloseKey(hSubKey);
                hSubKey = NULL;
                continue;
            }

            lResult = WszRegQueryValueEx(
                            hSubKey,
                            W("RuntimeVersion"),
                            NULL,
                            &type,
                            NULL,
                            &size);
            if (!((lResult == ERROR_SUCCESS)&&(type == REG_SZ)&&(size > 0)))
            {
                // We didn't find a RuntimeVersion value. This is fine for Records since
                // in V1.1, Records didn't have a RuntimeVersion value. However if we aren't
                // dealing with a Record, then this version subkey is invalid.
                if (basePath != RegistryBasePath_Record)
                {
                    // do not have value "RuntimeVersion"
                    RegCloseKey(hSubKey);
                    hSubKey = NULL;
                    continue;
                }
            }
            else
            {               
                // If a highest allowed runtime version was specified, make sure that the component
                // was built with a runtime version lower or equal to the highest.
                if (prvHighestAllowed)
                {
                    NewArrayHolder<WCHAR> wzRuntimeVersionString = 
                        new (nothrow) WCHAR[(size/sizeof(WCHAR)) + 1];
                    IfNullGo(wzRuntimeVersionString);
                    // RegQueryValueEx() does not guarantee NULL-terminated strings
                    wzRuntimeVersionString[size/sizeof(WCHAR)] = W('\0');
                    IfFailWin32Go(WszRegQueryValueEx(
                                  hSubKey,
                                  W("RuntimeVersion"),
                                  NULL,
                                  &type,
                                  (LPBYTE)(WCHAR*)wzRuntimeVersionString,
                                  &size));

                    AssemblyVersion rvCurrent;
                    rvCurrent.Init(wzRuntimeVersionString, TRUE);

                    // We only care about major and minor version number.
                    rvCurrent.SetBuild(0);
                    rvCurrent.SetRevision(0);

                    if ((*prvHighestAllowed) < rvCurrent)
                    {                   
                        // This version of the component was built with a runtime version higher
                        // than maximum allowed one so we don't want to consider it.
                        RegCloseKey(hSubKey);
                        hSubKey = NULL;
                        continue;
                    }                                       
                }
            }
        }
                
        // ok. Now I believe this is a valid subkey
        RegCloseKey(hSubKey);
        hSubKey = NULL;

        if (bGotVersion)
        {
            if (avCurrent >= avHighest)
            {
                avHighest = avCurrent;
                wcscpy_s(wzHighest, COUNTOF(wzHighest), wzCurrent);
            }
        }
        else
        {
            avHighest = avCurrent;
            wcscpy_s(wzHighest, COUNTOF(wzHighest), wzCurrent);
        }

        bGotVersion = TRUE;
    }


    //
    // If there are no subkeys, then look at the top level key.
    //
    
    if (!bGotVersion)
    {
        // make sure value Class exists
        // If not dealing with record, also make sure RuntimeVersion exists.
        if ((WszRegQueryValueEx(hKeyCLSID, W("Class"), NULL, &type, NULL, &size) == ERROR_SUCCESS) && (type == REG_SZ) && (size > 0))
        {
            // If there is no RuntimeVersion value, we will assume the component was built against
            // the V1.0 CLR.
            BOOL bSupportedVersion = TRUE;
            
            lResult = WszRegQueryValueEx(
                            hKeyCLSID,
                            W("RuntimeVersion"),
                            NULL,
                            &type,
                            NULL,
                            &size);
            if ((lResult == ERROR_SUCCESS) && (type == REG_SZ) && (size > 0))
            {                      
                // If a highest allowed runtime version was specified, make sure that the component
                // was built with a runtime version lower or equal to the highest.
                if (prvHighestAllowed)
                {
                    NewArrayHolder<WCHAR> wzRuntimeVersionString = 
                        new (nothrow) WCHAR[(size/sizeof(WCHAR)) + 1];
                    IfNullGo(wzRuntimeVersionString);
                    // RegQueryValueEx() does not guarantee NULL-terminated strings
                    wzRuntimeVersionString[size/sizeof(WCHAR)] = W('\0');
                    IfFailWin32Go(WszRegQueryValueEx(
                                  hKeyCLSID,
                                  W("RuntimeVersion"),
                                  NULL,
                                  &type,
                                  (LPBYTE)(WCHAR*)wzRuntimeVersionString,
                                  &size));

                    AssemblyVersion rvCurrent;
                    rvCurrent.Init(wzRuntimeVersionString, TRUE);

                    // We only care about major and minor version number.
                    rvCurrent.SetBuild(0);
                    rvCurrent.SetRevision(0);

                    if ((*prvHighestAllowed) < rvCurrent)
                    {                   
                        // This version of the component was built with a runtime version higher
                        // than maximum allowed one so we don't want to consider it.
                        bSupportedVersion = FALSE;
                    }                                       
                }
            }

            if (bSupportedVersion)
            {           
                // Get the size of assembly display name
                lResult = WszRegQueryValueEx(
                                hKeyCLSID,
                                W("Assembly"),
                                NULL,
                                &type,
                                NULL,
                                &size);
            
                if ((lResult == ERROR_SUCCESS) && (type == REG_SZ) && (size > 0))
                {
                    IfNullGo(wzAssemblyString = new (nothrow) WCHAR[size + 1]);
                    IfFailWin32Go(WszRegQueryValueEx(
                                  hKeyCLSID,
                                  W("Assembly"),
                                  NULL,
                                  &type,
                                  (LPBYTE)wzAssemblyString,
                                  &size));
                
                    // Now we have the assembly display name.
                    // Extract the version out.

                    // first lowercase display name
                    _wcslwr_s(wzAssemblyString,size+1);

                    // locate "version="
                    LPWSTR pwzVersion = wcsstr(wzAssemblyString, W("version="));
                    if (pwzVersion) {
                        // point to the character after "version="
                        pwzVersion += 8; // length of W("version=")

                        // Now find the next W(',')
                        LPWSTR pwzEnd = pwzVersion;

                        while((*pwzEnd != W(',')) && (*pwzEnd != W('\0')))
                            pwzEnd++;

                        // terminate version string
                        *pwzEnd = W('\0');

                        // trim version string
                        while(iswspace(*pwzVersion)) 
                            pwzVersion++;

                        pwzEnd--;
                        while(iswspace(*pwzEnd)&&(pwzEnd > pwzVersion))
                        {
                            *pwzEnd = W('\0');
                            pwzEnd--;
                        }
                               
                        // Make sure the version is valid.
                        if(SUCCEEDED(avHighest.Init(pwzVersion, FALSE)))
                        {
                            // This is the first version found, so it is the highest version
                            wcscpy_s(wzHighest, COUNTOF(wzHighest), pwzVersion);
                            bIsTopKey = TRUE;
                            bGotVersion = TRUE;
                        }
                    }
                }
            }
        } // end of handling of key HKCR\CLSID\<clsid>\InprocServer32
    }

    if (bGotVersion)
    {
        // Now we have the highest version. Copy it out
        size_t cchHighest = wcslen(wzHighest) + 1; 
        *ppwzHighestVersion = new (nothrow) WCHAR[cchHighest];
        wcscpy_s(*ppwzHighestVersion, cchHighest, wzHighest);

        *pbIsTopKey = bIsTopKey;

        // return S_OK to indicate we successfully found the highest version.
        hr = S_OK;
    }
    else
    {
        // return E_CLASSNOTREG to indicate that we didn't find anything
        hr = REGDB_E_CLASSNOTREG;
    }

ErrExit:
    if (hKeyCLSID)
        RegCloseKey(hKeyCLSID);
    if (hSubKey)
        RegCloseKey(hSubKey);
    if (wzAssemblyString)
        delete[] wzAssemblyString;

    return hr;
}

// If the value exists and is retrieved successfully, returns S_OK
// If the value does not exist, returns S_FALSE
// If some other error occurs, returns error-specific HRESULT
static
HRESULT ReadRegistryStringValue(
    HKEYHolder &hKey,
    LPCWSTR     wszName,
    __deref_out_z __deref_out_opt LPWSTR *pwszValue)
{
    HRESULT hr = S_OK;

    _ASSERTE(pwszValue != NULL);
    *pwszValue = NULL;
    
    // extract the string value.
    DWORD dwSize;
    DWORD dwType;
    NewArrayHolder<WCHAR> wszValue(NULL);
    hr = HRESULT_FROM_WIN32(WszRegQueryValueEx(hKey, wszName, NULL, &dwType, NULL, &dwSize));

    // If the function succeeds, the return value is ERROR_SUCCESS.
    // If the wszName registry value does not exist, the function returns ERROR_FILE_NOT_FOUND.
    if (hr == HRESULT_FROM_WIN32(ERROR_SUCCESS))
    {
        // The value is not a string
        if (dwType != REG_SZ)
            return E_INVALIDARG;

        if(!ClrSafeInt<DWORD>::addition(dwSize, 1, dwSize))
            IfFailWin32Ret(ERROR_ARITHMETIC_OVERFLOW);

        IfNullRet(wszValue = new (nothrow) WCHAR[dwSize]);
        IfFailWin32Ret(WszRegQueryValueEx(hKey, wszName, NULL, NULL, (LPBYTE)static_cast<LPCWSTR>(wszValue), &dwSize));
        hr = S_OK;
    }
    else if (hr == HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND))
    {
        return S_FALSE;
    }

    _ASSERTE(hr == S_OK);
    wszValue.SuppressRelease();
    *pwszValue = wszValue;
    return hr;
}

// FindRuntimeVersionFromRegistry
//
// Find the runtimeVersion corresponding to the highest version
HRESULT FindRuntimeVersionFromRegistry(
    REFCLSID rclsid,
    __deref_out_z __deref_out_opt LPWSTR *ppwzRuntimeVersion,
    __deref_out_z __deref_out_opt LPWSTR *ppwzSupportedVersions)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FAULT;

    HRESULT               hr = S_OK;
    HKEYHolder            userKey;
    WCHAR                 szID[64];
    WCHAR                 keyname[256];
    DWORD                 size;
    DWORD                 type;
    NewArrayHolder<WCHAR> pwzVersion = NULL;
    BOOL                  bIsTopKey;
    BOOL                  bIsUnmanagedObject = FALSE;
    NewArrayHolder<WCHAR> pwzRuntimeVersion = NULL;
    NewArrayHolder<WCHAR> pwzSupportedRuntimeVersions = NULL;

    if (ppwzRuntimeVersion == NULL)
        IfFailRet(E_INVALIDARG);

    // Initialize the string passed in to NULL.
    *ppwzRuntimeVersion = NULL;

    if (ppwzSupportedVersions != NULL)
        *ppwzSupportedVersions = NULL;

    // Convert the GUID to its string representation.
    if (GuidToLPWSTR(rclsid, szID, NumItems(szID)) == 0)
        IfFailRet(E_INVALIDARG);
    
    // retrieve the highest version.
    IfFailRet(FindHighestVersion(rclsid, RegistryBasePath_CLSID_InprocServer32, NULL, &pwzVersion, &bIsTopKey, &bIsUnmanagedObject));

    if (!bIsUnmanagedObject)
    {
        // if highest version is in top key,
        // we will look at HKCR\CLSID\<clsid>\InprocServer32 or HKCR\Record\<RecordId>
        // Otherwise we will look at HKCR\CLSID\<clsid>\InprocServer32\<version> or HKCR\Record\<RecordId>\<Version>
        wcscpy_s(keyname, 256, W("CLSID\\"));
        wcscat_s(keyname, 256, szID);
        wcscat_s(keyname, 256, W("\\InprocServer32"));
        if (!bIsTopKey)
        {
            wcscat_s(keyname, 256, W("\\"));
            wcscat_s(keyname, 256, pwzVersion);
        }
   
        // open the registry key
        IfFailWin32Ret(WszRegOpenKeyEx(HKEY_CLASSES_ROOT, keyname, 0, KEY_READ, &userKey));

        // extract the runtime version.
        IfFailRet(ReadRegistryStringValue(userKey, W("RuntimeVersion"), &pwzRuntimeVersion));
        if (hr == S_FALSE)
        {
            IfNullRet(pwzRuntimeVersion = DuplicateString(V1_VERSION_NUM));
        }

        // extract the supported runtime versions
        if (ppwzSupportedVersions != NULL)
            IfFailRet(ReadRegistryStringValue(userKey, W("SupportedRuntimeVersions"), &pwzSupportedRuntimeVersions));
    }

    else
    {
        // We need to prepend the 'v' to the version string
        IfNullRet(pwzRuntimeVersion = new (nothrow) WCHAR[wcslen(pwzVersion)+1+1]); // +1 for the v, +1 for the null
        *pwzRuntimeVersion = W('v');
        wcscpy_s(pwzRuntimeVersion+1, wcslen(pwzVersion)+1, pwzVersion);
    }

    _ASSERTE(SUCCEEDED(hr));

    // now we have the data, copy it out
    pwzRuntimeVersion.SuppressRelease();
    *ppwzRuntimeVersion = pwzRuntimeVersion;

    if (ppwzSupportedVersions != NULL)
    {
        pwzSupportedRuntimeVersions.SuppressRelease();
        *ppwzSupportedVersions = pwzSupportedRuntimeVersions;
    }

    return hr;
}

// FindShimInfoFromRegistry
//
HRESULT FindShimInfoFromRegistryWorker(
    REFCLSID rclsid,
    RegistryBasePath basePath,
    WORD wHighestRuntimeMajorVersion, 
    WORD wHighestRuntimeMinorVersion,
    __deref_out_z LPWSTR *ppwzClassName,
    __deref_out_z LPWSTR *ppwzAssemblyString, 
    __deref_out_z LPWSTR *ppwzCodeBase)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FAULT;

    HRESULT hr = S_OK;
    HKEY    userKey = NULL;
    WCHAR   szID[64];
    WCHAR   keyname[256];
    DWORD   size;
    DWORD   type;
    LPWSTR  pwzVersion = NULL;
    BOOL    bIsTopKey;
    NewArrayHolder<WCHAR> wzClassName = NULL;
    NewArrayHolder<WCHAR> wzAssemblyString = NULL;
    NewArrayHolder<WCHAR> wzCodeBase = NULL;
    LONG    lResult;
    AssemblyVersion highestRuntimeVersion;
        
    // at least one should be specified.
    // codebase is optional
    if ((ppwzClassName == NULL) && (ppwzAssemblyString == NULL))
        IfFailGo(E_INVALIDARG);

    // Initialize the strings passed in to NULL.
    if (ppwzClassName)
        *ppwzClassName = NULL;
    if (ppwzAssemblyString)
        *ppwzAssemblyString = NULL;
    if (ppwzCodeBase)
        *ppwzCodeBase = NULL;

    // Convert the GUID to its string representation.
    if (GuidToLPWSTR(rclsid, szID, NumItems(szID)) == 0)
        IfFailGo(E_INVALIDARG);
    
    // retrieve the highest version.
    BOOL bIsUnmanaged = FALSE;

    // Initialize the highest runtime version based on the passed in major and minor version numbers.
    highestRuntimeVersion.Init(wHighestRuntimeMajorVersion, wHighestRuntimeMinorVersion, 0, 0);
    
    IfFailGo(FindHighestVersion(rclsid, basePath, &highestRuntimeVersion, &pwzVersion, &bIsTopKey, &bIsUnmanaged));

    // if highest version is in top key,
    // we will look at HKCR\CLSID\<clsid>\{Inproc,Local}Server32 or HKCR\Record\<RecordId>
    // Otherwise we will look at HKCR\CLSID\<clsid>\{Inproc,Local}Server32\<version> or HKCR\Record\<RecordId>\<Version>
    if (basePath == RegistryBasePath_Record)
    {
        wcscpy_s(keyname, 256, W("Record\\"));
        wcscat_s(keyname, 256, szID);
    }
    else
    {
        wcscpy_s(keyname, 256, W("CLSID\\"));
        wcscat_s(keyname, 256, szID);
        if (basePath == RegistryBasePath_CLSID_InprocServer32)
        {
            wcscat_s(keyname, 256, W("\\InprocServer32"));
        }
        else
        {
            _ASSERTE(basePath == RegistryBasePath_CLSID_LocalServer32_32Key || basePath == RegistryBasePath_CLSID_LocalServer32_64Key);
            wcscat_s(keyname, 256, W("\\LocalServer32"));
        }
    }
    if (!bIsTopKey)
    {
         wcscat_s(keyname, 256, W("\\"));
         wcscat_s(keyname, 256, pwzVersion);
    }
  
    // open the registry
    REGSAM accessFlags = KEY_READ;
    if (basePath == RegistryBasePath_CLSID_LocalServer32_32Key)
    {
        // open the WoW key
        accessFlags |= KEY_WOW64_32KEY;
    }
    else if (basePath == RegistryBasePath_CLSID_LocalServer32_64Key)
    {
        // open the 64-bit key
        accessFlags |= KEY_WOW64_64KEY;
    }

    IfFailWin32Go(WszRegOpenKeyEx(HKEY_CLASSES_ROOT, keyname, 0, accessFlags, &userKey));
  
    // get the class name
    IfFailWin32Go(WszRegQueryValueEx(userKey, W("Class"), NULL, &type, NULL, &size));
    IfNullGo(wzClassName = new (nothrow) WCHAR[size + 1]);
    IfFailWin32Go(WszRegQueryValueEx(userKey, W("Class"), NULL, NULL, (LPBYTE)wzClassName.GetValue(), &size));

    // get the assembly string 
    IfFailWin32Go(WszRegQueryValueEx(userKey, W("Assembly"), NULL, &type, NULL, &size));
    IfNullGo(wzAssemblyString = new (nothrow) WCHAR[size + 1]);
    IfFailWin32Go(WszRegQueryValueEx(userKey, W("Assembly"), NULL, NULL, (LPBYTE)wzAssemblyString.GetValue(), &size));

    // get the code base if requested
    if (ppwzCodeBase)
    {
        // get the codebase, however not finding it does not constitute
        // a fatal error.
        lResult = WszRegQueryValueEx(userKey, W("CodeBase"), NULL, &type, NULL, &size);
        if ((lResult == ERROR_SUCCESS) && (type == REG_SZ) && (size > 0))
        {
            IfNullGo(wzCodeBase = new (nothrow) WCHAR[size + 1]);
            IfFailWin32Go(WszRegQueryValueEx(userKey, W("CodeBase"), NULL, NULL, (LPBYTE)wzCodeBase.GetValue(), &size));                        
        }
    }

    // now we got everything. Copy them out
    if (ppwzClassName)
        *ppwzClassName = wzClassName.Extract();

    if (ppwzAssemblyString)
        *ppwzAssemblyString = wzAssemblyString.Extract();

    if (ppwzCodeBase)
        *ppwzCodeBase = wzCodeBase.Extract();

    hr = S_OK;

ErrExit:
    if (userKey)
        RegCloseKey(userKey);
    
    if (pwzVersion)
        delete[] pwzVersion;

    return hr;
}

// Find shim info corresponding to the highest version
HRESULT FindShimInfoFromRegistry(
    REFCLSID rclsid,
    BOOL bLoadRecord,
    WORD wHighestRuntimeMajorVersion, 
    WORD wHighestRuntimeMinorVersion,
    __deref_out_z LPWSTR *ppwzClassName,
    __deref_out_z LPWSTR *ppwzAssemblyString, 
    __deref_out_z LPWSTR *ppwzCodeBase)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FAULT;

    if (bLoadRecord)
    {
        return FindShimInfoFromRegistryWorker(rclsid,
                                              RegistryBasePath_Record,
                                              wHighestRuntimeMajorVersion,
                                              wHighestRuntimeMinorVersion,
                                              ppwzClassName,
                                              ppwzAssemblyString,
                                              ppwzCodeBase);
    }

    // try InprocServer32 first
    HRESULT hr = FindShimInfoFromRegistryWorker(rclsid,
                                                RegistryBasePath_CLSID_InprocServer32,
                                                wHighestRuntimeMajorVersion,
                                                wHighestRuntimeMinorVersion,
                                                ppwzClassName,
                                                ppwzAssemblyString,
                                                ppwzCodeBase);

    // if it fails try both 64-bit and WoW LocalServer32
    if (FAILED(hr))
    {
        // prefer the bitness of the process, use the other bitness as a fallback
        hr = FindShimInfoFromRegistryWorker(rclsid,
#ifdef _WIN64
                                            RegistryBasePath_CLSID_LocalServer32_64Key,
#else // _WIN64
                                            RegistryBasePath_CLSID_LocalServer32_32Key,
#endif // _WIN64
                                            wHighestRuntimeMajorVersion,
                                            wHighestRuntimeMinorVersion,
                                            ppwzClassName,
                                            ppwzAssemblyString,
                                            ppwzCodeBase);

        if (FAILED(hr)
#ifndef _WIN64
            && RunningInWow64()
#endif // !_WIN64
            )
        {
            hr = FindShimInfoFromRegistryWorker(rclsid,
#ifdef _WIN64
                                                RegistryBasePath_CLSID_LocalServer32_32Key,
#else // _WIN64
                                                RegistryBasePath_CLSID_LocalServer32_64Key,
#endif // _WIN64
                                                wHighestRuntimeMajorVersion,
                                                wHighestRuntimeMinorVersion,
                                                ppwzClassName,
                                                ppwzAssemblyString,
                                                ppwzCodeBase);
        }
    }
    return hr;
}


// ----------------------------------------------------------------------------------------------------------
// 
// Gets config file name from Win32 manifest file. Strips the last extension (.manifest) from the manifest 
// file name. Doesn't strip the last extension if it is '.exe'.
// 
// Note: Config file name could be already set in activation context by COM+ (out-of-process COM) - they 
// pick up CLR version information from registry.
// 
// Arguments:
//   wszBuffer  - [in, out] The buffer to fill the configuration file name (can be NULL)
//   cBuffer    - [in] Size of the buffer in wide characters
//   pcNameSize - [out] Size of the name filled in the buffer (in wide characters including terminating null).
//                Can be NULL.
// 
// Return value:
//   S_OK   - Success. Buffer (wszBuffer) and config file name size (pcNameSize) are filled.
//   HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER) - Buffer is too small for filling config file name. 
//                  Sets pcNameSize (if not NULL) to the size of config file name including null terminator.
//
HRESULT GetConfigFileFromWin32Manifest(__out_ecount(cBuffer) WCHAR *wszBuffer, 
                                       SIZE_T                       cBuffer, 
                                       SIZE_T                      *pcNameSize)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FAULT;
    
    HRESULT hr = S_OK;
    
    HANDLE hActCtx         = NULL;
    SIZE_T cbInfo          = 0;
    SIZE_T cConfigFileName = 0;
    
    // Get detailed information about activation activation context
    if (!WszQueryActCtxW(0,         // Flags
                         hActCtx,   // Activation context being queried
                         NULL,      // Specific to the information class
                         ActivationContextDetailedInformation, 
                                    // Information class - detailed information
                         NULL,      // [out] Buffer
                         0,         // [in] Buffer size
                         &cbInfo))  // [out] Written/required sized in buffer
    {
        if (GetLastError() == ERROR_INSUFFICIENT_BUFFER)
        {
            CQuickBytes                              qbInfo;
            ACTIVATION_CONTEXT_DETAILED_INFORMATION *pInfo = NULL;
            
            pInfo = (ACTIVATION_CONTEXT_DETAILED_INFORMATION *)qbInfo.AllocNoThrow(cbInfo);
            IfNullGo(pInfo);
            
            if (WszQueryActCtxW(0,          // Flags
                                hActCtx,    // Activation context being queried
                                NULL,       // Specific to the information class
                                ActivationContextDetailedInformation, 
                                            // Information class - detailed information
                                pInfo,      // [out] Buffer
                                cbInfo,     // [in] Buffer size
                                &cbInfo) && // [out] Written size in the buffer
                pInfo->ulAppDirPathType == ACTIVATION_CONTEXT_PATH_TYPE_WIN32_FILE) 
            {   // Application manifest was loaded from Win32 file (not an URL, AssemblyRef etc.)
                WCHAR     *wszConfigFileName = NULL;
                CQuickWSTR qwszConfigFileName;
                
                if (pInfo->lpRootConfigurationPath != NULL)
                {   // Configuration file name is provided, use it
                    wszConfigFileName = (WCHAR *)pInfo->lpRootConfigurationPath;
                }
                else if (pInfo->lpRootManifestPath != NULL)
                {   // Manifest file name is provided, use it
                    SIZE_T cManifestFileName = wcslen(pInfo->lpRootManifestPath);
                    if (cManifestFileName != 0)
                    {   // Manifest file does exist
                        WCHAR  wszConfigFileExtension[] = W(".config");
                        SIZE_T cConfigFileExtension     = sizeof(wszConfigFileExtension) / sizeof(WCHAR);
                        // Allocate space for manifest file name + .config + terminating null 
                        // (included in .config extension)
                        SIZE_T cConfigFileNameAllocated = cManifestFileName + cConfigFileExtension;
                        wszConfigFileName = qwszConfigFileName.AllocNoThrow(cConfigFileNameAllocated);
                        IfNullGo(wszConfigFileName);
                        // Use manifest file name as the template for config file name
                        wcscpy_s(wszConfigFileName, cConfigFileNameAllocated, pInfo->lpRootManifestPath);
                        
                        // Find the last extension in the manifest file name (or NULL if not found)
                        LPWSTR wszLastExtension = wcsrchr(wszConfigFileName, W('.'));
                        // Is the manifest file in an external separate file?
                        if ((wszLastExtension != NULL) && (_wcsicmp(wszLastExtension, W(".exe")) != 0))
                        {   // It is an external manifest file (with .manifest or similar extension)
                            // Excluded are files without an extension and with .exe extension (embeded 
                            // manifest in executable resources)
                            
                            // Strip the last extension in the manifest file name
                            *wszLastExtension = 0;
                        }
                        // Manifest file name has stripped last extension (.manifest)
                        
                        // Append the .config extension behind the manifest file name
                        wcscat_s(wszConfigFileName, 
                                 cConfigFileNameAllocated, 
                                 wszConfigFileExtension);
                    }
                }
                
                // Do we have a configuration file name?
                if (wszConfigFileName != NULL)
                {   // We have a configuration file name
                    // Get the real configuration file name length (including terminating null)
                    cConfigFileName = wcslen(wszConfigFileName) + 1;
                    // Check the output buffer - is its size sufficient?
                    if ((wszBuffer == NULL) || (cConfigFileName > cBuffer))
                    {   // Insufficient output buffer (too small or not passed)
                        IfFailGo(HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER));
                    }
                    else 
                    {   // Fill output buffer
                        wcscpy_s(wszBuffer, cConfigFileName, wszConfigFileName);
                    }
                }
            }
        }
    }
    
ErrExit:
    // Should we return config file name size?
    if (pcNameSize != NULL)
    {   // We should return name size
        // Return either copied size or potentially copied size
        *pcNameSize = cConfigFileName;
    }
    return hr;
}

HRESULT GetApplicationPathFromWin32Manifest(__out_ecount(dwBuffer) WCHAR* buffer, SIZE_T dwBuffer, SIZE_T* pSize)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FAULT;

    HRESULT hr = S_OK;

    // Get the basic activation context first.
    ACTIVATION_CONTEXT_DETAILED_INFORMATION* pInfo = NULL;
    SIZE_T length = 0;

    HANDLE hActCtx = NULL;
    SIZE_T nCount = 0;

    if (!WszQueryActCtxW(0, hActCtx, NULL, ActivationContextDetailedInformation, 
                         NULL, nCount, &nCount))
    {
        
        if (GetLastError() == ERROR_INSUFFICIENT_BUFFER) 
        {
           
            pInfo = (ACTIVATION_CONTEXT_DETAILED_INFORMATION*) alloca(nCount);
            
            if (WszQueryActCtxW(0, hActCtx, NULL, ActivationContextDetailedInformation, 
                                pInfo, nCount, &nCount) &&
                pInfo->ulAppDirPathType == ACTIVATION_CONTEXT_PATH_TYPE_WIN32_FILE) 
            {
                
                if(pInfo->lpAppDirPath) {
                    length = wcslen(pInfo->lpAppDirPath) + 1;
                    if(length > dwBuffer || buffer == NULL) {
                        hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
                    }
                    else {
                        wcscpy_s(buffer, dwBuffer, pInfo->lpAppDirPath);
                    }
                }

            }
        }
    }
    if(pSize) *pSize = length;
    return hr;
}
