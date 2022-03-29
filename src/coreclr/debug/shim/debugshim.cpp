// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// debugshim.cpp
//

//
//*****************************************************************************

#include "debugshim.h"
#include "dbgutil.h"
#include <crtdbg.h>
#include <clrinternal.h> //has the CLR_ID_V4_DESKTOP guid in it
#include "palclr.h"

#ifndef IMAGE_FILE_MACHINE_ARMNT
#define IMAGE_FILE_MACHINE_ARMNT             0x01c4  // ARM Thumb-2 Little-Endian
#endif

#ifndef IMAGE_FILE_MACHINE_ARM64
#define IMAGE_FILE_MACHINE_ARM64             0xAA64  // ARM64 Little-Endian
#endif

//*****************************************************************************
// CLRDebuggingImpl implementation (ICLRDebugging)
//*****************************************************************************

typedef HRESULT (STDAPICALLTYPE  *OpenVirtualProcessImpl2FnPtr)(ULONG64 clrInstanceId,
    IUnknown * pDataTarget,
    LPCWSTR pDacModulePath,
    CLR_DEBUGGING_VERSION * pMaxDebuggerSupportedVersion,
    REFIID riid,
    IUnknown ** ppInstance,
    CLR_DEBUGGING_PROCESS_FLAGS * pdwFlags);

typedef HRESULT (STDAPICALLTYPE  *OpenVirtualProcessImplFnPtr)(ULONG64 clrInstanceId,
    IUnknown * pDataTarget,
    HMODULE hDacDll,
    CLR_DEBUGGING_VERSION * pMaxDebuggerSupportedVersion,
    REFIID riid,
    IUnknown ** ppInstance,
    CLR_DEBUGGING_PROCESS_FLAGS * pdwFlags);

typedef HRESULT (STDAPICALLTYPE  *OpenVirtualProcess2FnPtr)(ULONG64 clrInstanceId,
    IUnknown * pDataTarget,
    HMODULE hDacDll,
    REFIID riid,
    IUnknown ** ppInstance,
    CLR_DEBUGGING_PROCESS_FLAGS * pdwFlags);

typedef HMODULE (STDAPICALLTYPE  *LoadLibraryWFnPtr)(LPCWSTR lpLibFileName);

static bool IsTargetWindows(ICorDebugDataTarget* pDataTarget)
{
    CorDebugPlatform targetPlatform;

    HRESULT result = pDataTarget->GetPlatform(&targetPlatform);

    if(FAILED(result))
    {
        _ASSERTE(!"Unexpected error");
        return false;
    }

    switch (targetPlatform)
    {
        case CORDB_PLATFORM_WINDOWS_X86:
        case CORDB_PLATFORM_WINDOWS_AMD64:
        case CORDB_PLATFORM_WINDOWS_IA64:
        case CORDB_PLATFORM_WINDOWS_ARM:
        case CORDB_PLATFORM_WINDOWS_ARM64:
            return true;
        default:
            return false;
    }
}

// Implementation of ICLRDebugging::OpenVirtualProcess
//
// Arguments:
//   moduleBaseAddress - the address of the module which might be a CLR
//   pDataTarget - the data target for inspecting the process
//   pLibraryProvider - a callback for locating DBI and DAC
//   pMaxDebuggerSupportedVersion - the max version of the CLR that this debugger will support debugging
//   riidProcess - the IID of the interface that should be passed back in ppProcess
//   ppProcess - output for the ICorDebugProcess# if this module is a CLR
//   pVersion - the CLR version if this module is a CLR
//   pFlags - output, see the CLR_DEBUGGING_PROCESS_FLAGS for more details. Right now this has only one possible
//            value which indicates this runtime had an unhandled exception
STDMETHODIMP CLRDebuggingImpl::OpenVirtualProcess(
    ULONG64 moduleBaseAddress,
    IUnknown * pDataTarget,
    ICLRDebuggingLibraryProvider * pLibraryProvider,
    CLR_DEBUGGING_VERSION * pMaxDebuggerSupportedVersion,
    REFIID riidProcess,
    IUnknown ** ppProcess,
    CLR_DEBUGGING_VERSION * pVersion,
    CLR_DEBUGGING_PROCESS_FLAGS * pFlags)
{
    //PRECONDITION(CheckPointer(pDataTarget));

    HRESULT hr = S_OK;
    ICorDebugDataTarget * pDt = NULL;
    HMODULE hDbi = NULL;
    HMODULE hDac = NULL;
    LPWSTR pDacModulePath = NULL;
    LPWSTR pDbiModulePath = NULL;
    DWORD dbiTimestamp;
    DWORD dbiSizeOfImage;
    WCHAR dbiName[MAX_PATH_FNAME] = { 0 };
    DWORD dacTimestamp;
    DWORD dacSizeOfImage;
    WCHAR dacName[MAX_PATH_FNAME] = { 0 };
    CLR_DEBUGGING_VERSION version;
    BOOL versionSupportedByCaller = FALSE;

    // argument checking
    if ((ppProcess != NULL || pFlags != NULL) && pLibraryProvider == NULL)
    {
        hr = E_POINTER; // the library provider must be specified if either
                            // ppProcess or pFlags is non-NULL
    }
    else if ((ppProcess != NULL || pFlags != NULL) && pMaxDebuggerSupportedVersion == NULL)
    {
        hr = E_POINTER; // the max supported version must be specified if either
                            // ppProcess or pFlags is non-NULL
    }
    else if (pVersion != NULL && pVersion->wStructVersion != 0)
    {
        hr = CORDBG_E_UNSUPPORTED_VERSION_STRUCT;
    }
    else if (FAILED(pDataTarget->QueryInterface(__uuidof(ICorDebugDataTarget), (void**)&pDt)))
    {
        hr = CORDBG_E_MISSING_DATA_TARGET_INTERFACE;
    }

    if (SUCCEEDED(hr))
    {
        // get CLR version
        // The expectation is that new versions of the CLR will continue to use the same GUID
        // (unless there's a reason to hide them from older shims), but debuggers will tell us the
        // CLR version they're designed for and mscordbi.dll can decide whether or not to accept it.
        version.wStructVersion = 0;
        hr = GetCLRInfo(pDt,
            moduleBaseAddress,
            &version,
            &dbiTimestamp,
            &dbiSizeOfImage,
            dbiName,
            MAX_PATH_FNAME,
            &dacTimestamp,
            &dacSizeOfImage,
            dacName,
            MAX_PATH_FNAME);
    }

    // If we need to fetch either the process info or the flags info then we need to find
    // mscordbi and DAC and do the version specific OVP work
    if (SUCCEEDED(hr) && (ppProcess != NULL || pFlags != NULL))
    {
        ICLRDebuggingLibraryProvider2* pLibraryProvider2;
        if (SUCCEEDED(pLibraryProvider->QueryInterface(__uuidof(ICLRDebuggingLibraryProvider2), (void**)&pLibraryProvider2)))
        {
            if (FAILED(pLibraryProvider2->ProvideLibrary2(dbiName, dbiTimestamp, dbiSizeOfImage, &pDbiModulePath)) ||
                pDbiModulePath == NULL)
            {
                hr = CORDBG_E_LIBRARY_PROVIDER_ERROR;
            }

            if (SUCCEEDED(hr))
            {
                hDbi = LoadLibraryW(pDbiModulePath);
                if (hDbi == NULL)
                {
                    hr = HRESULT_FROM_WIN32(GetLastError());
                }
            }

            if (SUCCEEDED(hr))
            {
                // Adjust the timestamp and size of image if this DAC is a known buggy version and needs to be retargeted
                RetargetDacIfNeeded(&dacTimestamp, &dacSizeOfImage);

                // Ask library provider for dac
                if (FAILED(pLibraryProvider2->ProvideLibrary2(dacName, dacTimestamp, dacSizeOfImage, &pDacModulePath)) ||
                    pDacModulePath == NULL)
                {
                    hr = CORDBG_E_LIBRARY_PROVIDER_ERROR;
                }

                if (SUCCEEDED(hr))
                {
                    hDac = LoadLibraryW(pDacModulePath);
                    if (hDac == NULL)
                    {
                        hr = HRESULT_FROM_WIN32(GetLastError());
                    }
                }
            }

            pLibraryProvider2->Release();
        }
        else {
            // Ask library provider for dbi
            if (FAILED(pLibraryProvider->ProvideLibrary(dbiName, dbiTimestamp, dbiSizeOfImage, &hDbi)) ||
                hDbi == NULL)
            {
                hr = CORDBG_E_LIBRARY_PROVIDER_ERROR;
            }

            if (SUCCEEDED(hr))
            {
                // Adjust the timestamp and size of image if this DAC is a known buggy version and needs to be retargeted
                RetargetDacIfNeeded(&dacTimestamp, &dacSizeOfImage);

                // ask library provider for dac
                if (FAILED(pLibraryProvider->ProvideLibrary(dacName, dacTimestamp, dacSizeOfImage, &hDac)) ||
                    hDac == NULL)
                {
                    hr = CORDBG_E_LIBRARY_PROVIDER_ERROR;
                }
            }
        }

        *ppProcess = NULL;

        if (SUCCEEDED(hr) && pDacModulePath != NULL)
        {
            // Get access to the latest OVP implementation and call it
            OpenVirtualProcessImpl2FnPtr ovpFn = (OpenVirtualProcessImpl2FnPtr)GetProcAddress(hDbi, "OpenVirtualProcessImpl2");
            if (ovpFn != NULL)
            {
                hr = ovpFn(moduleBaseAddress, pDataTarget, pDacModulePath, pMaxDebuggerSupportedVersion, riidProcess, ppProcess, pFlags);
                if (FAILED(hr))
                {
                    _ASSERTE(ppProcess == NULL || *ppProcess == NULL);
                    _ASSERTE(pFlags == NULL || *pFlags == 0);
                }
            }
#ifdef HOST_UNIX
            else
            {
                // On Linux/MacOS the DAC module handle needs to be re-created using the DAC PAL instance
                // before being passed to DBI's OpenVirtualProcess* implementation. The DBI and DAC share
                // the same PAL where dbgshim has it's own.
                LoadLibraryWFnPtr loadLibraryWFn = (LoadLibraryWFnPtr)GetProcAddress(hDac, "LoadLibraryW");
                if (loadLibraryWFn != NULL)
                {
                    hDac = loadLibraryWFn(pDacModulePath);
                    if (hDac == NULL)
                    {
                        hr = E_HANDLE;
                    }
                }
                else
                {
                    hr = E_HANDLE;
                }
            }
#endif // HOST_UNIX
        }

        // If no errors so far and "OpenVirtualProcessImpl2" doesn't exist
        if (SUCCEEDED(hr) && *ppProcess == NULL)
        {
            // Get access to OVP and call it
            OpenVirtualProcessImplFnPtr ovpFn = (OpenVirtualProcessImplFnPtr)GetProcAddress(hDbi, "OpenVirtualProcessImpl");
            if (ovpFn == NULL)
            {
                // Fallback to CLR v4 Beta1 path, but skip some of the checking we'd normally do (maxSupportedVersion, etc.)
                OpenVirtualProcess2FnPtr ovp2Fn = (OpenVirtualProcess2FnPtr)GetProcAddress(hDbi, "OpenVirtualProcess2");
                if (ovp2Fn == NULL)
                {
                    hr = CORDBG_E_LIBRARY_PROVIDER_ERROR;
                }
                else
                {
                    hr = ovp2Fn(moduleBaseAddress, pDataTarget, hDac, riidProcess, ppProcess, pFlags);
                }
            }
            else
            {
                // Have a CLR v4 Beta2+ DBI, call it and let it do the version check
                hr = ovpFn(moduleBaseAddress, pDataTarget, hDac, pMaxDebuggerSupportedVersion, riidProcess, ppProcess, pFlags);
                if (FAILED(hr))
                {
                    _ASSERTE(ppProcess == NULL || *ppProcess == NULL);
                    _ASSERTE(pFlags == NULL || *pFlags == 0);
                }
            }
        }
    }

    //version is still valid in some failure cases
    if (pVersion != NULL &&
        (SUCCEEDED(hr) ||
        (hr == CORDBG_E_UNSUPPORTED_DEBUGGING_MODEL) ||
            (hr == CORDBG_E_UNSUPPORTED_FORWARD_COMPAT)))
    {
        memcpy(pVersion, &version, sizeof(CLR_DEBUGGING_VERSION));
    }

    if (pDacModulePath != NULL)
    {
#ifdef HOST_UNIX
        free(pDacModulePath);
#else
        CoTaskMemFree(pDacModulePath);
#endif
    }

    if (pDbiModulePath != NULL)
    {
#ifdef HOST_UNIX
        free(pDbiModulePath);
#else
        CoTaskMemFree(pDbiModulePath);
#endif
    }

    // free the data target we QI'ed earlier
    if (pDt != NULL)
    {
        pDt->Release();
    }

    return hr;
}

// Checks to see if this DAC is one of a known set of old DAC builds which contains an issue.
// If so we retarget to a newer compatible version which has the bug fixed. This is done
// by changing the PE information used to lookup the DAC.
//
// Arguments
//   pdwTimeStamp - on input, the timestamp of DAC as embedded in the CLR image
//                  on output, a potentially new timestamp for an updated DAC to use
//                  instead
//   pdwSizeOfImage - on input, the sizeOfImage of DAC as embedded in the CLR image
//                  on output, a potentially new sizeOfImage for an updated DAC to use
//                  instead
VOID CLRDebuggingImpl::RetargetDacIfNeeded(DWORD* pdwTimeStamp,
                                           DWORD* pdwSizeOfImage)
{

    // This code is auto generated by the CreateRetargetTable tool
    // on 3/4/2011 6:35 PM
    // and then copy-pasted here.
    //
    //
    //
    // Retarget the GDR1 amd64 build
    if( (*pdwTimeStamp == 0x4d536868) && (*pdwSizeOfImage == 0x17b000))
    {
        *pdwTimeStamp = 0x4d71a160;
        *pdwSizeOfImage = 0x17b000;
    }
    // Retarget the GDR1 x86 build
    else if( (*pdwTimeStamp == 0x4d5368f2) && (*pdwSizeOfImage == 0x120000))
    {
        *pdwTimeStamp = 0x4d71a14f;
        *pdwSizeOfImage = 0x120000;
    }
    // Retarget the RTM amd64 build
    else if( (*pdwTimeStamp == 0x4ba21fa7) && (*pdwSizeOfImage == 0x17b000))
    {
        *pdwTimeStamp = 0x4d71a13c;
        *pdwSizeOfImage = 0x17b000;
    }
    // Retarget the RTM x86 build
    else if( (*pdwTimeStamp == 0x4ba1da25) && (*pdwSizeOfImage == 0x120000))
    {
        *pdwTimeStamp = 0x4d71a128;
        *pdwSizeOfImage = 0x120000;
    }
    // This code is auto generated by the CreateRetargetTable tool
    // on 8/17/2011 1:28 AM
    // and then copy-pasted here.
    //
    //
    //
    // Retarget the GDR2 amd64 build
    else if( (*pdwTimeStamp == 0x4da428c7) && (*pdwSizeOfImage == 0x17b000))
    {
        *pdwTimeStamp = 0x4e4b7bc2;
        *pdwSizeOfImage = 0x17b000;
    }
    // Retarget the GDR2 x86 build
    else if( (*pdwTimeStamp == 0x4da3fe52) && (*pdwSizeOfImage == 0x120000))
    {
        *pdwTimeStamp = 0x4e4b7bb1;
        *pdwSizeOfImage = 0x120000;
    }
    // End auto-generated code
}

#define PE_FIXEDFILEINFO_SIGNATURE 0xFEEF04BD

// The format of the special debugging resource we embed in CLRs starting in
// v4
struct CLR_DEBUG_RESOURCE
{
    DWORD dwVersion;
    GUID signature;
    DWORD dwDacTimeStamp;
    DWORD dwDacSizeOfImage;
    DWORD dwDbiTimeStamp;
    DWORD dwDbiSizeOfImage;
};

// Checks to see if a module is a CLR and if so, fetches the debug data
// from the embedded resource
//
// Arguments
//   pDataTarget - dataTarget for the process we are inspecting
//   moduleBaseAddress - base address of a module we should inspect
//   pVersion - output, the version of the CLR detected if this is a CLR
//   pdwDbiTimeStamp - the timestamp of DBI as embedded in the CLR image
//   pdwDbiSizeOfImage - the SizeOfImage of DBI as embedded in the CLR image
//   pDbiName - output, the filename of DBI (as calculated by this function but that might change)
//   dwDbiNameCharCount - input, the number of WCHARs in the buffer pointed to by pDbiName
//   pdwDacTimeStampe - the timestamp of DAC as embedded in the CLR image
//   pdwDacSizeOfImage - the SizeOfImage of DAC as embedded in the CLR image
//   pDacName - output, the filename of DAC (as calculated by this function but that might change)
//   dwDacNameCharCount - input, the number of WCHARs in the buffer pointed to by pDacName
HRESULT CLRDebuggingImpl::GetCLRInfo(ICorDebugDataTarget* pDataTarget,
                                     ULONG64 moduleBaseAddress,
                                     CLR_DEBUGGING_VERSION* pVersion,
                                     DWORD* pdwDbiTimeStamp,
                                     DWORD* pdwDbiSizeOfImage,
                                     _Inout_updates_z_(dwDbiNameCharCount) WCHAR* pDbiName,
                                     DWORD  dwDbiNameCharCount,
                                     DWORD* pdwDacTimeStamp,
                                     DWORD* pdwDacSizeOfImage,
                                     _Inout_updates_z_(dwDacNameCharCount) WCHAR* pDacName,
                                     DWORD  dwDacNameCharCount)
{
#ifdef HOST_WINDOWS
    if(IsTargetWindows(pDataTarget))
    {
        WORD imageFileMachine = 0;
        DWORD resourceSectionRVA = 0;
        HRESULT hr = GetMachineAndResourceSectionRVA(pDataTarget, moduleBaseAddress, &imageFileMachine, &resourceSectionRVA);

        // We want the version resource which has type = RT_VERSION = 16, name = 1, language = 0x409
        DWORD versionResourceRVA = 0;
        DWORD versionResourceSize = 0;
        if(SUCCEEDED(hr))
        {
            hr = GetResourceRvaFromResourceSectionRva(pDataTarget, moduleBaseAddress, resourceSectionRVA, 16, 1, 0x409,
                     &versionResourceRVA, &versionResourceSize);
        }

        // At last we get our version info
        VS_FIXEDFILEINFO fixedFileInfo = {0};
        if(SUCCEEDED(hr))
        {
            // The version resource has 3 words, then the unicode string "VS_VERSION_INFO"
            // (16 WCHARS including the null terminator)
            // then padding to a 32-bit boundary, then the VS_FIXEDFILEINFO struct
            DWORD fixedFileInfoRVA = ((versionResourceRVA + 3*2 + 16*2 + 3)/4)*4;
            hr = ReadFromDataTarget(pDataTarget, moduleBaseAddress + fixedFileInfoRVA, (BYTE*)&fixedFileInfo, sizeof(fixedFileInfo));
        }

        //Verify the signature on the version resource
        if(SUCCEEDED(hr) && fixedFileInfo.dwSignature != PE_FIXEDFILEINFO_SIGNATURE)
        {
            hr = CORDBG_E_NOT_CLR;
        }

        // Record the version information
        if(SUCCEEDED(hr))
        {
            pVersion->wMajor = (WORD) (fixedFileInfo.dwProductVersionMS >> 16);
            pVersion->wMinor = (WORD) (fixedFileInfo.dwProductVersionMS & 0xFFFF);
            pVersion->wBuild = (WORD) (fixedFileInfo.dwProductVersionLS >> 16);
            pVersion->wRevision = (WORD) (fixedFileInfo.dwProductVersionLS & 0xFFFF);
        }

        // Now grab the special clr debug info resource
        // We may need to scan a few different names searching though...
        // 1) CLRDEBUGINFO<host_os><host_arch> where host_os = 'WINDOWS' or 'CORESYS' and host_arch = 'X86' or 'ARM' or 'AMD64'
        // 2) For back-compat if the host os is windows and the host architecture matches the target then CLRDEBUGINFO is used with no suffix.
        DWORD debugResourceRVA = 0;
        DWORD debugResourceSize = 0;
        BOOL useCrossPlatformNaming = FALSE;
        if(SUCCEEDED(hr))
        {
            // the initial state is that we haven't found a proper resource
            HRESULT hrGetResource = E_FAIL;

            // First check for the resource which has type = RC_DATA = 10, name = "CLRDEBUGINFO<host_os><host_arch>", language = 0
    #if defined (HOST_WINDOWS) && defined(HOST_X86)
            const WCHAR * resourceName = W("CLRDEBUGINFOWINDOWSX86");
    #endif

    #if !defined (HOST_WINDOWS) && defined(HOST_X86)
            const WCHAR * resourceName = W("CLRDEBUGINFOCORESYSX86");
    #endif

    #if defined (HOST_WINDOWS) && defined(HOST_AMD64)
            const WCHAR * resourceName = W("CLRDEBUGINFOWINDOWSAMD64");
    #endif

    #if !defined (HOST_WINDOWS) && defined(HOST_AMD64)
            const WCHAR * resourceName = W("CLRDEBUGINFOCORESYSAMD64");
    #endif

    #if defined (HOST_WINDOWS) && defined(HOST_ARM64)
            const WCHAR * resourceName = W("CLRDEBUGINFOWINDOWSARM64");
    #endif

    #if !defined (HOST_WINDOWS) && defined(HOST_ARM64)
            const WCHAR * resourceName = W("CLRDEBUGINFOCORESYSARM64");
    #endif

    #if defined (HOST_WINDOWS) && defined(HOST_ARM)
            const WCHAR * resourceName = W("CLRDEBUGINFOWINDOWSARM");
    #endif

    #if !defined (HOST_WINDOWS) && defined(HOST_ARM)
            const WCHAR * resourceName = W("CLRDEBUGINFOCORESYSARM");
    #endif

            hrGetResource = GetResourceRvaFromResourceSectionRvaByName(pDataTarget, moduleBaseAddress, resourceSectionRVA, 10, resourceName, 0,
                     &debugResourceRVA, &debugResourceSize);
            useCrossPlatformNaming = SUCCEEDED(hrGetResource);


    #if defined(HOST_WINDOWS) && (defined(HOST_X86) || defined(HOST_AMD64) || defined(HOST_ARM))
      #if defined(HOST_X86)
        #define _HOST_MACHINE_TYPE IMAGE_FILE_MACHINE_I386
      #elif defined(HOST_AMD64)
        #define _HOST_MACHINE_TYPE IMAGE_FILE_MACHINE_AMD64
      #elif defined(HOST_ARM)
        #define _HOST_MACHINE_TYPE IMAGE_FILE_MACHINE_ARMNT
      #endif

            // if this is windows, and if host_arch matches target arch then we can fallback to searching for CLRDEBUGINFO on failure
            if(FAILED(hrGetResource) && (imageFileMachine == _HOST_MACHINE_TYPE))
            {
                hrGetResource = GetResourceRvaFromResourceSectionRvaByName(pDataTarget, moduleBaseAddress, resourceSectionRVA, 10, W("CLRDEBUGINFO"), 0,
                     &debugResourceRVA, &debugResourceSize);
            }

      #undef _HOST_MACHINE_TYPE
    #endif
            // if the search failed, we don't recognize the CLR
            if(FAILED(hrGetResource))
                hr = CORDBG_E_NOT_CLR;
        }

        CLR_DEBUG_RESOURCE debugResource;
        if(SUCCEEDED(hr) && debugResourceSize != sizeof(debugResource))
        {
            hr = CORDBG_E_NOT_CLR;
        }

        // Get the special debug resource from the image and return the results
        if(SUCCEEDED(hr))
        {
            hr = ReadFromDataTarget(pDataTarget, moduleBaseAddress + debugResourceRVA, (BYTE*)&debugResource, sizeof(debugResource));
        }
        if(SUCCEEDED(hr) && (debugResource.dwVersion != 0))
        {
            hr = CORDBG_E_NOT_CLR;
        }

        // The signature needs to match m_skuId exactly, except for m_skuId=CLR_ID_ONECORE_CLR which is
        // also compatible with the older CLR_ID_PHONE_CLR signature.
        if(SUCCEEDED(hr) &&
           (debugResource.signature != m_skuId) &&
           !( (debugResource.signature == CLR_ID_PHONE_CLR) && (m_skuId == CLR_ID_ONECORE_CLR) ))
        {
            hr = CORDBG_E_NOT_CLR;
        }

        if(SUCCEEDED(hr) &&
           (debugResource.signature != CLR_ID_ONECORE_CLR) &&
           useCrossPlatformNaming)
        {
            FormatLongDacModuleName(pDacName, dwDacNameCharCount, imageFileMachine, &fixedFileInfo);
            swprintf_s(pDbiName, dwDbiNameCharCount, W("%s_%s.dll"), MAIN_DBI_MODULE_NAME_W, W("x86"));
        }
        else
        {
            if(m_skuId == CLR_ID_V4_DESKTOP)
                swprintf_s(pDacName, dwDacNameCharCount, W("%s.dll"), CLR_DAC_MODULE_NAME_W);
            else
                swprintf_s(pDacName, dwDacNameCharCount, W("%s.dll"), CORECLR_DAC_MODULE_NAME_W);
            swprintf_s(pDbiName, dwDbiNameCharCount, W("%s.dll"), MAIN_DBI_MODULE_NAME_W);
        }

        if(SUCCEEDED(hr))
        {
            *pdwDbiTimeStamp = debugResource.dwDbiTimeStamp;
            *pdwDbiSizeOfImage = debugResource.dwDbiSizeOfImage;
            *pdwDacTimeStamp = debugResource.dwDacTimeStamp;
            *pdwDacSizeOfImage = debugResource.dwDacSizeOfImage;
        }

        // any failure should be interpreted as this module not being a CLR
        if(FAILED(hr))
        {
            return CORDBG_E_NOT_CLR;
        }
        else
        {
            return S_OK;
        }
    }
    else
#endif // !HOST_WINDOWS
    {
        swprintf_s(pDacName, dwDacNameCharCount, W("%s"), MAKEDLLNAME_W(CORECLR_DAC_MODULE_NAME_W));
        swprintf_s(pDbiName, dwDbiNameCharCount, W("%s"), MAKEDLLNAME_W(MAIN_DBI_MODULE_NAME_W));

        pVersion->wMajor = 0;
        pVersion->wMinor = 0;
        pVersion->wBuild = 0;
        pVersion->wRevision = 0;

        *pdwDbiTimeStamp = 0;
        *pdwDbiSizeOfImage = 0;
        *pdwDacTimeStamp = 0;
        *pdwDacSizeOfImage = 0;

        return S_OK;
    }
}

// Formats the long name for DAC
HRESULT CLRDebuggingImpl::FormatLongDacModuleName(_Inout_updates_z_(cchBuffer) WCHAR * pBuffer,
                                                  DWORD cchBuffer,
                                                  DWORD targetImageFileMachine,
                                                  VS_FIXEDFILEINFO * pVersion)
{

#ifndef HOST_WINDOWS
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
#endif

#if defined(HOST_X86)
    const WCHAR* pHostArch = W("x86");
#elif defined(HOST_AMD64)
    const WCHAR* pHostArch = W("amd64");
#elif defined(HOST_ARM)
    const WCHAR* pHostArch = W("arm");
#elif defined(HOST_ARM64)
    const WCHAR* pHostArch = W("arm64");
#else
    _ASSERTE(!"Unknown host arch");
    return E_NOTIMPL;
#endif

    const WCHAR* pDacBaseName = NULL;
    if(m_skuId == CLR_ID_V4_DESKTOP)
        pDacBaseName = CLR_DAC_MODULE_NAME_W;
    else if(m_skuId == CLR_ID_CORECLR || m_skuId == CLR_ID_PHONE_CLR || m_skuId == CLR_ID_ONECORE_CLR)
        pDacBaseName = CORECLR_DAC_MODULE_NAME_W;
    else
    {
        _ASSERTE(!"Unknown SKU id");
        return E_UNEXPECTED;
    }

    const WCHAR* pTargetArch = NULL;
    if(targetImageFileMachine == IMAGE_FILE_MACHINE_I386)
    {
        pTargetArch = W("x86");
    }
    else if(targetImageFileMachine == IMAGE_FILE_MACHINE_AMD64)
    {
        pTargetArch = W("amd64");
    }
    else if(targetImageFileMachine == IMAGE_FILE_MACHINE_ARMNT)
    {
        pTargetArch = W("arm");
    }
    else if(targetImageFileMachine == IMAGE_FILE_MACHINE_ARM64)
    {
        pTargetArch = W("arm64");
    }
    else
    {
        _ASSERTE(!"Unknown target image file machine type");
        return E_INVALIDARG;
    }

    const WCHAR* pBuildFlavor = W("");
    if(pVersion->dwFileFlags & VS_FF_DEBUG)
    {
        if(pVersion->dwFileFlags & VS_FF_SPECIALBUILD)
            pBuildFlavor = W(".dbg");
        else
            pBuildFlavor = W(".chk");
    }

    // WARNING: if you change the formatting make sure you recalculate the maximum
    // possible size string and verify callers pass a big enough buffer. This doesn't
    // have to be a tight estimate, just make sure its >= the biggest possible DAC name
    // and it can be calculated statically
    DWORD minCchBuffer =
        (DWORD) wcslen(CLR_DAC_MODULE_NAME_W) + (DWORD) wcslen(CORECLR_DAC_MODULE_NAME_W) + // max name
        10 + // max host arch
        10 + // max target arch
        40 + // max version
        10 + // max build flavor
        (DWORD) wcslen(W("name_host_target_version.flavor.dll")) + // max intermediate formatting chars
        1; // null terminator

    // validate the output buffer is larger than our estimate above
    _ASSERTE(cchBuffer >= minCchBuffer);
    if(!(cchBuffer >= minCchBuffer)) return E_INVALIDARG;

    swprintf_s(pBuffer, cchBuffer, W("%s_%s_%s_%u.%u.%u.%02u%s.dll"),
        pDacBaseName,
        pHostArch,
        pTargetArch,
        pVersion->dwProductVersionMS >> 16,
        pVersion->dwProductVersionMS & 0xFFFF,
        pVersion->dwProductVersionLS >> 16,
        pVersion->dwProductVersionLS & 0xFFFF,
        pBuildFlavor);
    return S_OK;
}

// An implementation of ICLRDebugging::CanUnloadNow
//
// Arguments:
//   hModule - a handle to a module provided earlier by ProvideLibrary
//
// Returns:
//   S_OK if the library is no longer in use and can be unloaded, S_FALSE otherwise
//
STDMETHODIMP CLRDebuggingImpl::CanUnloadNow(HMODULE hModule)
{
    // In V4 at least we don't support any unloading.
    HRESULT hr = S_FALSE;

    return hr;
}



STDMETHODIMP CLRDebuggingImpl::QueryInterface(REFIID riid, void **ppvObject)
{
    HRESULT hr = S_OK;

    if (riid == __uuidof(IUnknown))
    {
        IUnknown *pItf = static_cast<IUnknown *>(this);
        pItf->AddRef();
        *ppvObject = pItf;
    }
    else if (riid == __uuidof(ICLRDebugging))
    {
        ICLRDebugging *pItf = static_cast<ICLRDebugging *>(this);
        pItf->AddRef();
        *ppvObject = pItf;
    }
    else
        hr = E_NOINTERFACE;

    return hr;
}

// Standard AddRef implementation
ULONG CLRDebuggingImpl::AddRef()
{
    return InterlockedIncrement(&m_cRef);
}

// Standard Release implementation.
ULONG CLRDebuggingImpl::Release()
{
    _ASSERTE(m_cRef > 0);

    ULONG cRef = InterlockedDecrement(&m_cRef);

    if (cRef == 0)
        delete this; // Relies on virtual dtor to work properly.

    return cRef;
}
