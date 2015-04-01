//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//*****************************************************************************
//  util.cpp
//

//
//  This contains a bunch of C++ utility classes.
//
//*****************************************************************************
#include "stdafx.h"                     // Precompiled header key.
#include "utilcode.h"
#include "metadata.h"
#include "ex.h"
#include "pedecoder.h"
#include "loaderheap.h"
#include "sigparser.h"
#include "cor.h"

#ifndef FEATURE_CORECLR
#include "metahost.h"
#endif // !FEATURE_CORECLR

const char g_RTMVersion[]= "v1.0.3705";

#ifndef DACCESS_COMPILE
UINT32 g_nClrInstanceId = 0;
#endif //!DACCESS_COMPILE

//********** Code. ************************************************************

#if defined(FEATURE_COMINTEROP) && !defined(FEATURE_CORESYSTEM)
extern WinRTStatusEnum gWinRTStatus = WINRT_STATUS_UNINITED;
#endif // FEATURE_COMINTEROP && !FEATURE_CORESYSTEM

#if defined(FEATURE_COMINTEROP) && !defined(FEATURE_CORESYSTEM)
//------------------------------------------------------------------------------
//
// Attempt to detect the presense of Windows Runtime support on the current OS.
// Our algorithm to do this is to ensure that:
//      1. combase.dll exists
//      2. combase.dll contains a RoInitialize export
//

void InitWinRTStatus()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;
    STATIC_CONTRACT_SO_TOLERANT;
    
    WinRTStatusEnum winRTStatus = WINRT_STATUS_UNSUPPORTED;

    const WCHAR wszComBaseDll[] = W("\\combase.dll");
    const SIZE_T cchComBaseDll = _countof(wszComBaseDll);

    WCHAR wszComBasePath[MAX_PATH + 1];
    const SIZE_T cchComBasePath = _countof(wszComBasePath);

    ZeroMemory(wszComBasePath, cchComBasePath * sizeof(wszComBasePath[0]));

    UINT cchSystemDirectory = WszGetSystemDirectory(wszComBasePath, MAX_PATH);

    // Make sure that we're only probing in the system directory.  If we can't find the system directory, or
    // we find it but combase.dll doesn't fit into it, we'll fall back to a safe default of saying that WinRT
    // is simply not present.
    if (cchSystemDirectory > 0 && cchComBasePath - cchSystemDirectory >= cchComBaseDll)
    {
        if (wcscat_s(wszComBasePath, wszComBaseDll) == 0)
        {
            HModuleHolder hComBase(WszLoadLibrary(wszComBasePath));
            if (hComBase != NULL)
            {
                FARPROC activateInstace = GetProcAddress(hComBase, "RoInitialize");
                if (activateInstace != NULL)
                {
                    winRTStatus = WINRT_STATUS_SUPPORTED;
                }
            }
        }
    }

    gWinRTStatus = winRTStatus;
}
#endif // FEATURE_COMINTEROP && !FEATURE_CORESYSTEM
//*****************************************************************************
// Convert a string of hex digits into a hex value of the specified # of bytes.
//*****************************************************************************
HRESULT GetHex(                         // Return status.
    LPCSTR      szStr,                  // String to convert.
    int         size,                   // # of bytes in pResult.
    void        *pResult)               // Buffer for result.
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;
    
    int         count = size * 2;       // # of bytes to take from string.
    unsigned int Result = 0;           // Result value.
    char          ch;

    _ASSERTE(size == 1 || size == 2 || size == 4);

    while (count-- && (ch = *szStr++) != '\0')
    {
        switch (ch)
        {
            case '0': case '1': case '2': case '3': case '4': 
            case '5': case '6': case '7': case '8': case '9': 
            Result = 16 * Result + (ch - '0');
            break;

            case 'A': case 'B': case 'C': case 'D': case 'E': case 'F':
            Result = 16 * Result + 10 + (ch - 'A');
            break;

            case 'a': case 'b': case 'c': case 'd': case 'e': case 'f':
            Result = 16 * Result + 10 + (ch - 'a');
            break;

            default:
            return (E_FAIL);
        }
    }

    // Set the output.
    switch (size)
    {
        case 1:
        *((BYTE *) pResult) = (BYTE) Result;
        break;

        case 2:
        *((WORD *) pResult) = (WORD) Result;
        break;

        case 4:
        *((DWORD *) pResult) = Result;
        break;

        default:
        _ASSERTE(0);
        break;
    }
    return (S_OK);
}

//*****************************************************************************
// Convert a pointer to a string into a GUID.
//*****************************************************************************
HRESULT LPCSTRToGuid(                   // Return status.
    LPCSTR      szGuid,                 // String to convert.
    GUID        *psGuid)                // Buffer for converted GUID.
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;
    
    int i;

    // Verify the surrounding syntax.
    if (strlen(szGuid) != 38 || szGuid[0] != '{' || szGuid[9] != '-' ||
        szGuid[14] != '-' || szGuid[19] != '-' || szGuid[24] != '-' || szGuid[37] != '}')
    {
        return (E_FAIL);
    }

    // Parse the first 3 fields.
    if (FAILED(GetHex(szGuid + 1, 4, &psGuid->Data1)))
        return E_FAIL;
    if (FAILED(GetHex(szGuid + 10, 2, &psGuid->Data2)))
        return E_FAIL;
    if (FAILED(GetHex(szGuid + 15, 2, &psGuid->Data3)))
        return E_FAIL;

    // Get the last two fields (which are byte arrays).
    for (i = 0; i < 2; ++i)
    {
        if (FAILED(GetHex(szGuid + 20 + (i * 2), 1, &psGuid->Data4[i])))
        {
            return E_FAIL;
        }
    }
    for (i=0; i < 6; ++i)
    {
        if (FAILED(GetHex(szGuid + 25 + (i * 2), 1, &psGuid->Data4[i+2])))
        {
            return E_FAIL;
        }
    }
    return S_OK;
}

//
//
// Global utility functions.
//
//



typedef HRESULT __stdcall DLLGETCLASSOBJECT(REFCLSID rclsid,
                                            REFIID   riid,
                                            void   **ppv);

EXTERN_C const IID _IID_IClassFactory = 
    {0x00000001, 0x0000, 0x0000, {0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46}};

// ----------------------------------------------------------------------------
// FakeCoCreateInstanceEx
// 
// Description:
//     A private function to do the equivalent of a CoCreateInstance in cases where we
//     can't make the real call. Use this when, for instance, you need to create a symbol
//     reader in the Runtime but we're not CoInitialized. Obviously, this is only good
//     for COM objects for which CoCreateInstance is just a glorified find-and-load-me
//     operation.
//     
// Arguments:
//    * rclsid - [in] CLSID of object to instantiate
//    * wszDllPath [in] - Path to profiler DLL.  If wszDllPath is NULL, FakeCoCreateInstanceEx 
//        will look up the registry to find the path of the COM dll associated with rclsid.
//        If the path ends in a backslash, FakeCoCreateInstanceEx will treat this as a prefix
//        if the InprocServer32 found in the registry is a simple filename (not a full path).
//        This allows the caller to specify the directory in which the InprocServer32 should
//        be found. Also, if this path is provided and the InprocServer32 is MSCOREE.DLL, then
//        the Server value is used instead, if it exists.
//    * riid - [in] IID of interface on object to return in ppv
//    * ppv - [out] Pointer to implementation of requested interface
//    * phmodDll - [out] HMODULE of DLL that was loaded to instantiate the COM object.
//        The caller may eventually call FreeLibrary() on this if it can be determined
//        that we no longer reference the generated COM object or dependencies. Else, the
//        caller may ignore this and the DLL will stay loaded forever. If caller
//        specifies phmodDll==NULL, then this parameter is ignored and the HMODULE is not
//        returned.
//        
// Return Value:
//    HRESULT indicating success or failure.
//    
// Notes:
//    * (*phmodDll) on [out] may always be trusted, even if this function returns an
//        error. Therefore, even if creation of the COM object failed, if (*phmodDll !=
//        NULL), then the DLL was actually loaded. The caller may wish to call
//        FreeLibrary on (*phmodDll) in such a case.
HRESULT FakeCoCreateInstanceEx(REFCLSID       rclsid,
                               LPCWSTR        wszDllPath,
                               REFIID         riid,
                               void **        ppv,
                               HMODULE *      phmodDll)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // Call the function to get a class factory for the rclsid passed in.
    ReleaseHolder<IClassFactory> classFactory;
    HModuleHolder hDll;
    IfFailRet(FakeCoCallDllGetClassObject(rclsid, wszDllPath, _IID_IClassFactory, (void**)&classFactory, &hDll));

    // Ask the class factory to create an instance of the
    // necessary object.
    IfFailRet(classFactory->CreateInstance(NULL, riid, ppv));

    hDll.SuppressRelease();

    if (phmodDll != NULL)
    {
        *phmodDll = hDll.GetValue();
    }

    return hr;
}

HRESULT FakeCoCallDllGetClassObject(REFCLSID       rclsid,
                               LPCWSTR        wszDllPath,
                               REFIID riid,
                               void **        ppv,
                               HMODULE *      phmodDll)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    _ASSERTE(ppv != NULL);
    
    HRESULT hr = S_OK;
    
    if (phmodDll != NULL)
    {   // Initialize [out] HMODULE (if it was requested)
        *phmodDll = NULL;
    }

    bool fIsDllPathPrefix = (wszDllPath != NULL) && (wszDllPath[wcslen(wszDllPath) - 1] == W('\\'));

    // - An empty string will be treated as NULL.
    // - A string ending will a backslash will be treated as a prefix for where to look for the DLL
    //   if the InProcServer32 value is just a DLL name and not a full path.
    StackSString ssDllName;
    if ((wszDllPath == NULL) || (wszDllPath[0] == W('\0')) || fIsDllPathPrefix)
    {
#ifndef FEATURE_PAL    
        IfFailRet(Clr::Util::Com::FindInprocServer32UsingCLSID(rclsid, ssDllName));

        EX_TRY
        {
            if (fIsDllPathPrefix)
            {
                if (Clr::Util::Com::IsMscoreeInprocServer32(ssDllName))
                {   // If the InprocServer32 is mscoree.dll, then we skip the shim and look for
                    // the corresponding server DLL (if it exists) in the directory provided.
                    hr = Clr::Util::Com::FindServerUsingCLSID(rclsid, ssDllName);

                    if (FAILED(hr))
                    {   // We don't fail if there is no server object, because in this case we assume that
                        // the clsid is implemented in the runtime itself (clr.dll) and we do not place 
                        // entries in the registry for this case.
                        ssDllName.Set(MAIN_CLR_MODULE_NAME_W);
                    }
                }

                SString::Iterator i = ssDllName.Begin();
                if (!ssDllName.Find(i, W('\\')))
                {   // If the InprocServer32 is just a DLL name (not a fully qualified path), then
                    // prefix wszFilePath with wszDllPath.
                    ssDllName.Insert(i, wszDllPath);
                }
            }
        }
        EX_CATCH_HRESULT(hr);
        IfFailRet(hr);

        wszDllPath = ssDllName.GetUnicode();
#else // !FEATURE_PAL
        return E_FAIL;
#endif // !FEATURE_PAL
    }
    _ASSERTE(wszDllPath != NULL);

    // We've got the name of the DLL to load, so load it.
    HModuleHolder hDll = WszLoadLibraryEx(wszDllPath, NULL, GetLoadWithAlteredSearchPathFlag());
    if (hDll == NULL)
    {
        return HRESULT_FROM_GetLastError();
    }

    // We've loaded the DLL, so find the DllGetClassObject function.
    DLLGETCLASSOBJECT *dllGetClassObject = (DLLGETCLASSOBJECT*)GetProcAddress(hDll, "DllGetClassObject");
    if (dllGetClassObject == NULL)
    {
        return HRESULT_FROM_GetLastError();
    }

    // Call the function to get a class object for the rclsid and riid passed in.
    IfFailRet(dllGetClassObject(rclsid, riid, ppv));

    hDll.SuppressRelease();

    if (phmodDll != NULL)
    {
        *phmodDll = hDll.GetValue();
    }

    return hr;
}

#if USE_UPPER_ADDRESS
static BYTE * s_CodeMinAddr;        // Preferred region to allocate the code in.
static BYTE * s_CodeMaxAddr;
static BYTE * s_CodeAllocStart;
static BYTE * s_CodeAllocHint;      // Next address to try to allocate for code in the preferred region.
#endif

//
// Use this function to initialize the s_CodeAllocHint
// during startup. base is runtime .dll base address,
// size is runtime .dll virtual size.
//
void InitCodeAllocHint(SIZE_T base, SIZE_T size, int randomPageOffset)
{
#if USE_UPPER_ADDRESS

#ifdef _DEBUG
    // If GetForceRelocs is enabled we don't constrain the pMinAddr
    if (PEDecoder::GetForceRelocs())
        return;
#endif
    
//
    // If we are using the UPPER_ADDRESS space (on Win64)
    // then for any code heap that doesn't specify an address
    // range using [pMinAddr..pMaxAddr] we place it in the
    // upper address space
    // This enables us to avoid having to use long JumpStubs
    // to reach the code for our ngen-ed images.
    // Which are also placed in the UPPER_ADDRESS space.
    //
    SIZE_T reach = 0x7FFF0000u;
    
    // We will choose the preferred code region based on the address of clr.dll. The JIT helpers
    // in clr.dll are the most heavily called functions.
    s_CodeMinAddr = (base + size > reach) ? (BYTE *)(base + size - reach) : (BYTE *)0;
    s_CodeMaxAddr = (base + reach > base) ? (BYTE *)(base + reach) : (BYTE *)-1;

    BYTE * pStart;

    if (s_CodeMinAddr <= (BYTE *)CODEHEAP_START_ADDRESS && 
        (BYTE *)CODEHEAP_START_ADDRESS < s_CodeMaxAddr)
    {
        // clr.dll got loaded at its preferred base address? (OS without ASLR - pre-Vista)
        // Use the code head start address that does not cause collisions with NGen images.
        // This logic is coupled with scripts that we use to assign base addresses.
        pStart = (BYTE *)CODEHEAP_START_ADDRESS;
    }
    else
    if (base > UINT32_MAX)
    {
        // clr.dll got address assigned by ASLR?
        // Try to occupy the space as far as possible to minimize collisions with other ASLR assigned
        // addresses. Do not start at s_CodeMinAddr exactly so that we can also reach common native images 
        // that can be placed at higher addresses than clr.dll.
        pStart = s_CodeMinAddr + (s_CodeMaxAddr - s_CodeMinAddr) / 8;
    }
    else
    {
        // clr.dll missed the base address?
        // Try to occupy the space right after it.
        pStart = (BYTE *)(base + size);
    }

    // Randomize the adddress space
    pStart += PAGE_SIZE * randomPageOffset;

    s_CodeAllocStart = pStart;
    s_CodeAllocHint = pStart;
#endif
}

//
// Use this function to reset the s_CodeAllocHint
// after unloading an AppDomain
//
void ResetCodeAllocHint()
{
    LIMITED_METHOD_CONTRACT;
#if USE_UPPER_ADDRESS
    s_CodeAllocHint = s_CodeAllocStart;
#endif
}

//
// Returns TRUE if p is located in near clr.dll that allows us
// to use rel32 IP-relative addressing modes.
//
BOOL IsPreferredExecutableRange(void * p)
{
    LIMITED_METHOD_CONTRACT;
#if USE_UPPER_ADDRESS
    if (s_CodeMinAddr <= (BYTE *)p && (BYTE *)p < s_CodeMaxAddr)
        return TRUE;
#endif
    return FALSE;
}

//
// Allocate free memory that will be used for executable code
// Handles the special requirements that we have on 64-bit platforms
// where we want the executable memory to be located near clr.dll
//
BYTE * ClrVirtualAllocExecutable(SIZE_T dwSize, 
                                 DWORD flAllocationType,
                                 DWORD flProtect)
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

#if USE_UPPER_ADDRESS
    //
    // If we are using the UPPER_ADDRESS space (on Win64)
    // then for any heap that will contain executable code
    // we will place it in the upper address space
    //
    // This enables us to avoid having to use JumpStubs
    // to reach the code for our ngen-ed images on x64,
    // since they are also placed in the UPPER_ADDRESS space.
    //
    BYTE * pHint = s_CodeAllocHint;
 
    if (dwSize <= (SIZE_T)(s_CodeMaxAddr - s_CodeMinAddr) && pHint != NULL)
    {
        // Try to allocate in the preferred region after the hint
        BYTE * pResult = ClrVirtualAllocWithinRange(pHint, s_CodeMaxAddr, dwSize, flAllocationType, flProtect);

        if (pResult != NULL)
        {
            s_CodeAllocHint = pResult + dwSize;
            return pResult;
        }

        // Try to allocate in the preferred region before the hint
        pResult = ClrVirtualAllocWithinRange(s_CodeMinAddr, pHint + dwSize, dwSize, flAllocationType, flProtect);

        if (pResult != NULL)
        {
            s_CodeAllocHint = pResult + dwSize;
            return pResult;
        }

        s_CodeAllocHint = NULL;
    }

    // Fall through to 
#endif // USE_UPPER_ADDRESS

    return (BYTE *) ClrVirtualAlloc (NULL, dwSize, flAllocationType, flProtect);

}

//
// Allocate free memory with specific alignment.                         
//
LPVOID ClrVirtualAllocAligned(LPVOID lpAddress, SIZE_T dwSize, DWORD flAllocationType, DWORD flProtect, SIZE_T alignment)
{
    // Verify that the alignment is a power of 2
    _ASSERTE(alignment != 0);
    _ASSERTE((alignment & (alignment - 1)) == 0);

#ifndef FEATURE_PAL

    // The VirtualAlloc on Windows ensures 64kB alignment
    _ASSERTE(alignment <= 0x10000);
    return ClrVirtualAlloc(lpAddress, dwSize, flAllocationType, flProtect);

#else // !FEATURE_PAL

    // UNIXTODO: Add a specialized function to PAL so that we don't have to waste memory
    dwSize += alignment;
    SIZE_T addr = (SIZE_T)ClrVirtualAlloc(lpAddress, dwSize, flAllocationType, flProtect);
    return (LPVOID)((addr + (alignment - 1)) & ~(alignment - 1));

#endif // !FEATURE_PAL
}

// Reserves free memory within the range [pMinAddr..pMaxAddr] using
// ClrVirtualQuery to find free memory and ClrVirtualAlloc to reserve it.
//
// This method only supports the flAllocationType of MEM_RESERVE
// Callers also should set dwSize to a multiple of sysInfo.dwAllocationGranularity (64k).
// That way they can reserve a large region and commit smaller sized pages
// from that region until it fills up.  
//
// This functions returns the reserved memory block upon success
//
// It returns NULL when it fails to find any memory that satisfies
// the range.
//

#ifdef _DEBUG
static DWORD ShouldInjectFaultInRange()
{
    static DWORD fInjectFaultInRange = 99;

    if (fInjectFaultInRange == 99)
        fInjectFaultInRange = (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_InjectFault) & 0x40);
    return fInjectFaultInRange;
}
#endif

BYTE * ClrVirtualAllocWithinRange(const BYTE *pMinAddr,
                                   const BYTE *pMaxAddr,
                                   SIZE_T dwSize, 
                                   DWORD flAllocationType,
                                   DWORD flProtect)
{
    CONTRACTL
    {
        NOTHROW;
        PRECONDITION(dwSize != 0);
        PRECONDITION(flAllocationType == MEM_RESERVE);
    }
    CONTRACTL_END;

    BYTE *pResult = NULL;
    //
    // First lets normalize the pMinAddr and pMaxAddr values
    //
    // If pMinAddr is NULL then set it to BOT_MEMORY
    if ((pMinAddr == 0) || (pMinAddr < (BYTE *) BOT_MEMORY))
    {
        pMinAddr = (BYTE *) BOT_MEMORY;
    }

    // If pMaxAddr is NULL then set it to TOP_MEMORY
    if ((pMaxAddr == 0) || (pMaxAddr > (BYTE *) TOP_MEMORY))
    {
        pMaxAddr = (BYTE *) TOP_MEMORY;
    }

    // If pMinAddr is BOT_MEMORY and pMaxAddr is TOP_MEMORY
    // then we can call ClrVirtualAlloc instead 
    if ((pMinAddr == (BYTE *) BOT_MEMORY) && (pMaxAddr == (BYTE *) TOP_MEMORY))
    {
        return (BYTE*) ClrVirtualAlloc(NULL, dwSize, flAllocationType, flProtect);
        }

    // If pMaxAddr is not greater than pMinAddr we can not make an allocation
    if (dwSize == 0 || pMaxAddr <= pMinAddr)
    {
        return NULL;
    }

        // We will do one scan: [pMinAddr .. pMaxAddr]
    // Align to 64k. See docs for VirtualAllocEx and lpAddress and 64k alignment for reasons.
    BYTE *tryAddr = (BYTE *)ALIGN_UP((BYTE *)pMinAddr, VIRTUAL_ALLOC_RESERVE_GRANULARITY);

        // Now scan memory and try to find a free block of the size requested.
    while ((tryAddr + dwSize) <= (BYTE *) pMaxAddr)
        {
        MEMORY_BASIC_INFORMATION mbInfo;
            
            // Use VirtualQuery to find out if this address is MEM_FREE
            //
            if (!ClrVirtualQuery((LPCVOID)tryAddr, &mbInfo, sizeof(mbInfo)))
                break;
            
            // Is there enough memory free from this start location?
            if ((mbInfo.State == MEM_FREE)  && (mbInfo.RegionSize >= (SIZE_T) dwSize))
            {
                // Try reserving the memory using VirtualAlloc now
            pResult = (BYTE*) ClrVirtualAlloc(tryAddr, dwSize, MEM_RESERVE, flProtect);
                
                if (pResult != NULL) 
                {
                return pResult;
                }
#ifdef _DEBUG 
                // pResult == NULL
                else if (ShouldInjectFaultInRange())
                {
                return NULL;
                }
#endif // _DEBUG

                // We could fail in a race.  Just move on to next region and continue trying
            tryAddr = tryAddr + VIRTUAL_ALLOC_RESERVE_GRANULARITY;
            }
            else
            {
                // Try another section of memory
            tryAddr = max(tryAddr + VIRTUAL_ALLOC_RESERVE_GRANULARITY,
                              (BYTE*) mbInfo.BaseAddress + mbInfo.RegionSize);
            }
        }

    // Our tryAddr reached pMaxAddr
    return NULL;
}

//******************************************************************************
// NumaNodeInfo 
//******************************************************************************
#if !defined(FEATURE_REDHAWK) && !defined(FEATURE_PAL)
#if !defined(FEATURE_CORESYSTEM)
/*static*/ NumaNodeInfo::PGNPN    NumaNodeInfo::m_pGetNumaProcessorNode = NULL;
#endif
/*static*/ NumaNodeInfo::PGNHNN NumaNodeInfo::m_pGetNumaHighestNodeNumber = NULL;
/*static*/ NumaNodeInfo::PVAExN NumaNodeInfo::m_pVirtualAllocExNuma = NULL;

#if !defined(FEATURE_CORESYSTEM)
/*static*/ BOOL NumaNodeInfo::GetNumaProcessorNode(UCHAR proc_no, PUCHAR node_no)
{
    return (*m_pGetNumaProcessorNode)(proc_no, node_no);
}
#endif

/*static*/ LPVOID NumaNodeInfo::VirtualAllocExNuma(HANDLE hProc, LPVOID lpAddr, SIZE_T dwSize,
		    		     DWORD allocType, DWORD prot, DWORD node)
{
    return (*m_pVirtualAllocExNuma)(hProc, lpAddr, dwSize, allocType, prot, node);
}
#if !defined(FEATURE_CORECLR) || defined(FEATURE_CORESYSTEM)
/*static*/ NumaNodeInfo::PGNPNEx NumaNodeInfo::m_pGetNumaProcessorNodeEx = NULL;

/*static*/ BOOL NumaNodeInfo::GetNumaProcessorNodeEx(PPROCESSOR_NUMBER proc_no, PUSHORT node_no)
{
    return (*m_pGetNumaProcessorNodeEx)(proc_no, node_no);
}
#endif
#endif

/*static*/ BOOL NumaNodeInfo::m_enableGCNumaAware = FALSE;
/*static*/ BOOL NumaNodeInfo::InitNumaNodeInfoAPI()
{
#if !defined(FEATURE_REDHAWK) && !defined(FEATURE_PAL)
    //check for numa support if multiple heaps are used
    ULONG highest = 0;
	
    if (CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_GCNumaAware) == 0)
        return FALSE;

    // check if required APIs are supported
    HMODULE hMod = GetModuleHandleW(WINDOWS_KERNEL32_DLLNAME_W);
    if (hMod == NULL)
        return FALSE;

    m_pGetNumaHighestNodeNumber = (PGNHNN) GetProcAddress(hMod, "GetNumaHighestNodeNumber");
    if (m_pGetNumaHighestNodeNumber == NULL)
        return FALSE;

    // fail to get the highest numa node number
    if (!m_pGetNumaHighestNodeNumber(&highest) || (highest == 0))
        return FALSE;

#if !defined(FEATURE_CORESYSTEM)
    m_pGetNumaProcessorNode = (PGNPN) GetProcAddress(hMod, "GetNumaProcessorNode");
    if (m_pGetNumaProcessorNode == NULL)
        return FALSE;
#endif

#if !defined(FEATURE_CORECLR) || defined(FEATURE_CORESYSTEM)
    m_pGetNumaProcessorNodeEx = (PGNPNEx) GetProcAddress(hMod, "GetNumaProcessorNodeEx");
    if (m_pGetNumaProcessorNodeEx == NULL)
        return FALSE;
#endif

    m_pVirtualAllocExNuma = (PVAExN) GetProcAddress(hMod, "VirtualAllocExNuma");
    if (m_pVirtualAllocExNuma == NULL)
        return FALSE;

    return TRUE;
#else
    return FALSE;
#endif
}

/*static*/ BOOL NumaNodeInfo::CanEnableGCNumaAware()
{
    return m_enableGCNumaAware;
}

/*static*/ void NumaNodeInfo::InitNumaNodeInfo()
{
    m_enableGCNumaAware = InitNumaNodeInfoAPI();
}

//******************************************************************************
// NumaNodeInfo 
//******************************************************************************
#if !defined(FEATURE_REDHAWK) && !defined(FEATURE_PAL)
/*static*/ CPUGroupInfo::PGLPIEx CPUGroupInfo::m_pGetLogicalProcessorInformationEx = NULL;
/*static*/ CPUGroupInfo::PSTGA   CPUGroupInfo::m_pSetThreadGroupAffinity = NULL;
/*static*/ CPUGroupInfo::PGTGA   CPUGroupInfo::m_pGetThreadGroupAffinity = NULL;
#if !defined(FEATURE_CORESYSTEM) && !defined(FEATURE_CORECLR)
/*static*/ CPUGroupInfo::PGCPNEx CPUGroupInfo::m_pGetCurrentProcessorNumberEx = NULL;
/*static*/ CPUGroupInfo::PGST    CPUGroupInfo::m_pGetSystemTimes = NULL;
#endif
/*static*/ //CPUGroupInfo::PNTQSIEx CPUGroupInfo::m_pNtQuerySystemInformationEx = NULL;

/*static*/ BOOL CPUGroupInfo::GetLogicalProcessorInformationEx(DWORD relationship,
                         SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX *slpiex, PDWORD count)
{
    LIMITED_METHOD_CONTRACT;
    return (*m_pGetLogicalProcessorInformationEx)(relationship, slpiex, count);
}

/*static*/ BOOL CPUGroupInfo::SetThreadGroupAffinity(HANDLE h, 
                        GROUP_AFFINITY *groupAffinity, GROUP_AFFINITY *previousGroupAffinity)
{
    LIMITED_METHOD_CONTRACT;
    return (*m_pSetThreadGroupAffinity)(h, groupAffinity, previousGroupAffinity);
}

/*static*/ BOOL CPUGroupInfo::GetThreadGroupAffinity(HANDLE h, GROUP_AFFINITY *groupAffinity)
{
    LIMITED_METHOD_CONTRACT;
    return (*m_pGetThreadGroupAffinity)(h, groupAffinity);
}

#if !defined(FEATURE_CORESYSTEM) && !defined(FEATURE_CORECLR)
/*static*/ BOOL CPUGroupInfo::GetSystemTimes(FILETIME *idleTime, FILETIME *kernelTime, FILETIME *userTime)
{
    LIMITED_METHOD_CONTRACT;
    return (*m_pGetSystemTimes)(idleTime, kernelTime, userTime);
}
#endif
#endif

/*static*/ BOOL  CPUGroupInfo::m_enableGCCPUGroups = FALSE;
/*static*/ BOOL  CPUGroupInfo::m_threadUseAllCpuGroups = FALSE;
/*static*/ WORD  CPUGroupInfo::m_nGroups = 0;
/*static*/ WORD  CPUGroupInfo::m_nProcessors = 0;
/*static*/ WORD  CPUGroupInfo::m_initialGroup = 0;
/*static*/ CPU_Group_Info *CPUGroupInfo::m_CPUGroupInfoArray = NULL;
/*static*/ LONG   CPUGroupInfo::m_initialization = 0;

// Check and setup function pointers for >64 LP Support
/*static*/ BOOL CPUGroupInfo::InitCPUGroupInfoAPI()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#if !defined(FEATURE_REDHAWK) && defined(_TARGET_AMD64_) && !defined(FEATURE_PAL)
    HMODULE hMod = GetModuleHandleW(WINDOWS_KERNEL32_DLLNAME_W);
    if (hMod == NULL)
        return FALSE;

    m_pGetLogicalProcessorInformationEx = (PGLPIEx)GetProcAddress(hMod, "GetLogicalProcessorInformationEx");
    if (m_pGetLogicalProcessorInformationEx == NULL)
        return FALSE;

    m_pSetThreadGroupAffinity = (PSTGA)GetProcAddress(hMod, "SetThreadGroupAffinity");
    if (m_pSetThreadGroupAffinity == NULL)
        return FALSE;

    m_pGetThreadGroupAffinity = (PGTGA)GetProcAddress(hMod, "GetThreadGroupAffinity");
    if (m_pGetThreadGroupAffinity == NULL)
        return FALSE;

#if !defined(FEATURE_CORESYSTEM) && !defined(FEATURE_CORECLR)
    m_pGetCurrentProcessorNumberEx = (PGCPNEx)GetProcAddress(hMod, "GetCurrentProcessorNumberEx");
    if (m_pGetCurrentProcessorNumberEx == NULL)
        return FALSE;

    m_pGetSystemTimes = (PGST)GetProcAddress(hMod, "GetSystemTimes");
    if (m_pGetSystemTimes == NULL)
        return FALSE;
#endif

    return TRUE;
#else
    return FALSE;
#endif
}

/*static*/ BOOL CPUGroupInfo::InitCPUGroupInfoArray()
{
    CONTRACTL
    {
        NOTHROW;
        SO_TOLERANT;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#if !defined(FEATURE_REDHAWK) && defined(_TARGET_AMD64_) && !defined(FEATURE_PAL)
    BYTE *bBuffer = NULL;
    SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX *pSLPIEx = NULL;
    SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX *pRecord = NULL;
    DWORD cbSLPIEx = 0;
    DWORD byteOffset = 0;
    DWORD dwNumElements = 0;
    DWORD dwWeight = 1;

    if (CPUGroupInfo::GetLogicalProcessorInformationEx(RelationGroup, pSLPIEx, &cbSLPIEx) &&
                      GetLastError() != ERROR_INSUFFICIENT_BUFFER)
        return FALSE;

    _ASSERTE(cbSLPIEx);

    // Fail to allocate buffer
    bBuffer = new (nothrow) BYTE[ cbSLPIEx ];
    if (bBuffer == NULL)
        return FALSE;

    pSLPIEx = (SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX *)bBuffer;
    if (!m_pGetLogicalProcessorInformationEx(RelationGroup, pSLPIEx, &cbSLPIEx))
    {
        delete[] bBuffer;
        return FALSE;
    }

    pRecord = pSLPIEx;
    while (byteOffset < cbSLPIEx)
    {
        if (pRecord->Relationship == RelationGroup)
        {
            m_nGroups = pRecord->Group.ActiveGroupCount;
            break;
        }
        byteOffset += pRecord->Size;
        pRecord = (SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX *)(bBuffer + byteOffset);
    }

    m_CPUGroupInfoArray = new (nothrow) CPU_Group_Info[m_nGroups];
    if (m_CPUGroupInfoArray == NULL) 
    {
        delete[] bBuffer;
        return FALSE;
    }

    for (DWORD i = 0; i < m_nGroups; i++)
    {
        m_CPUGroupInfoArray[i].nr_active   = (WORD)pRecord->Group.GroupInfo[i].ActiveProcessorCount;
        m_CPUGroupInfoArray[i].active_mask = pRecord->Group.GroupInfo[i].ActiveProcessorMask;
        m_nProcessors += m_CPUGroupInfoArray[i].nr_active;
        dwWeight *= (DWORD)m_CPUGroupInfoArray[i].nr_active;
    }

    //NOTE: the weight setting should work fine with 4 CPU groups upto 64 LPs each. the minimum number of threads
    //     per group before the weight overflow is 2^32/(2^6x2^6x2^6) = 2^14 (i.e. 16K threads)
    for (DWORD i = 0; i < m_nGroups; i++)
    {
        m_CPUGroupInfoArray[i].groupWeight = dwWeight / (DWORD)m_CPUGroupInfoArray[i].nr_active;
        m_CPUGroupInfoArray[i].activeThreadWeight = 0;
    }

    delete[] bBuffer;  // done with it; free it
    return TRUE;
#else
    return FALSE;
#endif
}

/*static*/ BOOL CPUGroupInfo::InitCPUGroupInfoRange()
{
    LIMITED_METHOD_CONTRACT;

#if !defined(FEATURE_REDHAWK) && defined(_TARGET_AMD64_) && !defined(FEATURE_PAL)
    WORD begin   = 0;
    WORD nr_proc = 0;

    for (WORD i = 0; i < m_nGroups; i++) 
    {
        nr_proc += m_CPUGroupInfoArray[i].nr_active;
        m_CPUGroupInfoArray[i].begin = begin;
        m_CPUGroupInfoArray[i].end   = nr_proc - 1;
        begin = nr_proc;
    }
    return TRUE;
#else
    return FALSE;
#endif
}

/*static*/ void CPUGroupInfo::InitCPUGroupInfo()
{
    CONTRACTL
    {
        NOTHROW;
        SO_TOLERANT;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#if !defined(FEATURE_REDHAWK) && defined(_TARGET_AMD64_) && !defined(FEATURE_PAL)
    BOOL enableGCCPUGroups     = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_GCCpuGroup) != 0;
	BOOL threadUseAllCpuGroups = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_Thread_UseAllCpuGroups) != 0;

	if (!enableGCCPUGroups)
		return;

    if (!InitCPUGroupInfoAPI())
        return;

    if (!InitCPUGroupInfoArray())
        return;

    if (!InitCPUGroupInfoRange())
        return;

    // initalGroup is whatever the CPU group that the main thread is running on
    GROUP_AFFINITY groupAffinity;
    CPUGroupInfo::GetThreadGroupAffinity(GetCurrentThread(), &groupAffinity);
    m_initialGroup = groupAffinity.Group;  

	// only enable CPU groups if more than one group exists
	BOOL hasMultipleGroups = m_nGroups > 1;
	m_enableGCCPUGroups = enableGCCPUGroups && hasMultipleGroups;
	m_threadUseAllCpuGroups = threadUseAllCpuGroups && hasMultipleGroups;
#endif // _TARGET_AMD64_
}

/*static*/ BOOL CPUGroupInfo::IsInitialized()
{
    LIMITED_METHOD_CONTRACT;
    return (m_initialization == -1);
}

/*static*/ void CPUGroupInfo::EnsureInitialized()
{
    CONTRACTL
    {
        NOTHROW;
        SO_TOLERANT;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // CPUGroupInfo needs to be initialized only once. This could happen in three cases 
    // 1. CLR initialization at begining of EEStartup, or
    // 2. Sometimes, when hosted by ASP.NET, the hosting process may initialize ThreadPool
    //    before initializing CLR, thus require CPUGroupInfo to be initialized to determine
    //    if CPU group support should/could be enabled.
    // 3. Call into Threadpool functions before Threadpool _and_ CLR is initialized.
    // Vast majority of time, CPUGroupInfo is initialized in case 1. or 2. 
    // The chance of contention will be extremely small, so the following code should be fine
    //
retry:
    if (IsInitialized())
        return;

    if (InterlockedCompareExchange(&m_initialization, 1, 0) == 0)
    {
        InitCPUGroupInfo();
        m_initialization = -1;
    }
    else //some other thread started initialization, just wait until complete;
    {
        while (m_initialization != -1)
        {
            SwitchToThread();
        }
        goto retry;
    }
}

/*static*/ WORD CPUGroupInfo::GetNumActiveProcessors()
{
    LIMITED_METHOD_CONTRACT;
    return (WORD)m_nProcessors;
}

/*static*/ void CPUGroupInfo::GetGroupForProcessor(WORD processor_number,
		                         WORD* group_number, WORD* group_processor_number)
{
    LIMITED_METHOD_CONTRACT;

#if !defined(FEATURE_REDHAWK) && defined(_TARGET_AMD64_) && !defined(FEATURE_PAL)
    WORD bTemp = 0;
    WORD bDiff = processor_number - bTemp;

    for (WORD i=0; i < m_nGroups; i++)
    {
        bTemp += m_CPUGroupInfoArray[i].nr_active;
        if (bTemp > processor_number)
        {
            *group_number = i;
            *group_processor_number = bDiff;
            break;
        }
        bDiff = processor_number - bTemp;
    }
#else
    *group_number = 0;
    *group_processor_number = 0;
#endif
}

/*static*/ DWORD CPUGroupInfo::CalculateCurrentProcessorNumber()
{
    CONTRACTL
    {
        NOTHROW;
        SO_TOLERANT;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#if !defined(FEATURE_REDHAWK) && !defined(FEATURE_CORESYSTEM) && !defined(FEATURE_CORECLR) && defined(_TARGET_AMD64_) && !defined(FEATURE_PAL)
    // m_enableGCCPUGroups and m_threadUseAllCpuGroups must be TRUE
    _ASSERTE(m_enableGCCPUGroups && m_threadUseAllCpuGroups);

    PROCESSOR_NUMBER proc_no;
    proc_no.Group=0;
    proc_no.Number=0;
    proc_no.Reserved=0;
    (*m_pGetCurrentProcessorNumberEx)(&proc_no);

    DWORD fullNumber = 0;
    for (WORD i = 0; i < proc_no.Group; i++)
        fullNumber += (DWORD)m_CPUGroupInfoArray[i].nr_active;
    fullNumber += (DWORD)(proc_no.Number);

    return fullNumber;
#else
    return 0;
#endif
}

#if !defined(FEATURE_REDHAWK) && !defined(FEATURE_CORESYSTEM) && !defined(FEATURE_CORECLR) && !defined(FEATURE_PAL)
//Lock ThreadStore before calling this function, so that updates of weights/counts are consistent
/*static*/ void CPUGroupInfo::ChooseCPUGroupAffinity(GROUP_AFFINITY *gf)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#if defined(_TARGET_AMD64_)
    WORD i, minGroup = 0;
    DWORD minWeight = 0;

    // m_enableGCCPUGroups and m_threadUseAllCpuGroups must be TRUE
    _ASSERTE(m_enableGCCPUGroups && m_threadUseAllCpuGroups);

    for (i = 0; i < m_nGroups; i++) 
    {
        minGroup = (m_initialGroup + i) % m_nGroups;	

        // the group is not filled up, use it
        if (m_CPUGroupInfoArray[minGroup].activeThreadWeight / m_CPUGroupInfoArray[minGroup].groupWeight
                          < (DWORD)m_CPUGroupInfoArray[minGroup].nr_active)
            goto found;
    }

    // all groups filled up, distribute proportionally
    minGroup = m_initialGroup;
    minWeight = m_CPUGroupInfoArray[m_initialGroup].activeThreadWeight; 
    for (i = 0; i < m_nGroups; i++) 
    {
        if (m_CPUGroupInfoArray[i].activeThreadWeight < minWeight)
        {
            minGroup = i;
            minWeight = m_CPUGroupInfoArray[i].activeThreadWeight;
        }
    }

found:
    gf->Group = minGroup;
    gf->Mask = m_CPUGroupInfoArray[minGroup].active_mask;
    gf->Reserved[0] = 0;
    gf->Reserved[1] = 0;
    gf->Reserved[2] = 0;
    m_CPUGroupInfoArray[minGroup].activeThreadWeight += m_CPUGroupInfoArray[minGroup].groupWeight;
#endif
}

//Lock ThreadStore before calling this function, so that updates of weights/counts are consistent
/*static*/ void CPUGroupInfo::ClearCPUGroupAffinity(GROUP_AFFINITY *gf)
{
    LIMITED_METHOD_CONTRACT;
#if defined(_TARGET_AMD64_)
    // m_enableGCCPUGroups and m_threadUseAllCpuGroups must be TRUE
    _ASSERTE(m_enableGCCPUGroups && m_threadUseAllCpuGroups);

    WORD group = gf->Group;
    m_CPUGroupInfoArray[group].activeThreadWeight -= m_CPUGroupInfoArray[group].groupWeight;
#endif
}
#endif

/*static*/ BOOL CPUGroupInfo::CanEnableGCCPUGroups()
{
    LIMITED_METHOD_CONTRACT;
    return m_enableGCCPUGroups;
}

/*static*/ BOOL CPUGroupInfo::CanEnableThreadUseAllCpuGroups()
{
    LIMITED_METHOD_CONTRACT;
    return m_threadUseAllCpuGroups;
}

//******************************************************************************
// Returns the number of processors that a process has been configured to run on
//******************************************************************************
//******************************************************************************
// Returns the number of processors that a process has been configured to run on
//******************************************************************************
int GetCurrentProcessCpuCount()
{
    CONTRACTL
    {
        NOTHROW;
        SO_TOLERANT;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;
    
    static int cCPUs = 0;

    if (cCPUs != 0)
        return cCPUs;

#if !defined(FEATURE_CORESYSTEM)

    DWORD_PTR pmask, smask;

    if (!GetProcessAffinityMask(GetCurrentProcess(), &pmask, &smask))
        return 1;

    if (pmask == 1)
        return 1;

    pmask &= smask;
        
    int count = 0;
    while (pmask)
    {
        if (pmask & 1)
            count++;
                
        pmask >>= 1;
    }
        
    // GetProcessAffinityMask can return pmask=0 and smask=0 on systems with more
    // than 64 processors, which would leave us with a count of 0.  Since the GC
    // expects there to be at least one processor to run on (and thus at least one
    // heap), we'll return 64 here if count is 0, since there are likely a ton of
    // processors available in that case.  The GC also cannot (currently) handle
    // the case where there are more than 64 processors, so we will return a
    // maximum of 64 here.
    if (count == 0 || count > 64)
        count = 64;

    cCPUs = count;
            
    return count;

#else // !FEATURE_CORESYSTEM

    SYSTEM_INFO sysInfo;
    ::GetSystemInfo(&sysInfo);
    cCPUs = sysInfo.dwNumberOfProcessors;
    return sysInfo.dwNumberOfProcessors;

#endif // !FEATURE_CORESYSTEM
}

DWORD_PTR GetCurrentProcessCpuMask()
{
    CONTRACTL
    {
        NOTHROW;
        SO_TOLERANT;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

#if !defined(FEATURE_CORESYSTEM)
    DWORD_PTR pmask, smask;

    if (!GetProcessAffinityMask(GetCurrentProcess(), &pmask, &smask))
        return 1;

    pmask &= smask;
    return pmask;
#else
    return 0;
#endif
}

/**************************************************************************/

/**************************************************************************/
void ConfigMethodSet::init(const CLRConfig::ConfigStringInfo & info)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;
    
    // make sure that the memory was zero initialized
    _ASSERTE(m_inited == 0 || m_inited == 1);

    LPWSTR str = CLRConfig::GetConfigValue(info);
    if (str) 
    {
        m_list.Insert(str);
        delete[] str;
    }
    m_inited = 1;
}

/**************************************************************************/
bool ConfigMethodSet::contains(LPCUTF8 methodName, LPCUTF8 className, PCCOR_SIGNATURE sig)
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;
    
    _ASSERTE(m_inited == 1);

    if (m_list.IsEmpty())
        return false;
    return(m_list.IsInList(methodName, className, sig));
}

/**************************************************************************/
void ConfigDWORD::init_DontUse_(__in_z LPCWSTR keyName, DWORD defaultVal)
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;
    
    // make sure that the memory was zero initialized
    _ASSERTE(m_inited == 0 || m_inited == 1);

    m_value = REGUTIL::GetConfigDWORD_DontUse_(keyName, defaultVal);
    m_inited = 1;
}

/**************************************************************************/
void ConfigString::init(const CLRConfig::ConfigStringInfo & info)
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;
    
    // make sure that the memory was zero initialized
    _ASSERTE(m_inited == 0 || m_inited == 1);

    // Note: m_value will be leaking
    m_value = CLRConfig::GetConfigValue(info);
    m_inited = 1;
}

//=============================================================================
// AssemblyNamesList
//=============================================================================
// The string should be of the form
// MyAssembly
// MyAssembly;mscorlib;System
// MyAssembly;mscorlib System

AssemblyNamesList::AssemblyNamesList(__in LPWSTR list)
{
    CONTRACTL {
        THROWS;
    } CONTRACTL_END;

    WCHAR prevChar = '?'; // dummy
    LPWSTR nameStart = NULL; // start of the name currently being processed. NULL if no current name
    AssemblyName ** ppPrevLink = &m_pNames;
    
    for (LPWSTR listWalk = list; prevChar != '\0'; prevChar = *listWalk, listWalk++)
    {
        WCHAR curChar = *listWalk;
        
        if (iswspace(curChar) || curChar == ';' || curChar == '\0' )
        {
            //
            // Found white-space
            //
            
            if (nameStart)
            {
                // Found the end of the current name
                
                AssemblyName * newName = new AssemblyName();
                size_t nameLen = listWalk - nameStart;
                
                MAKE_UTF8PTR_FROMWIDE(temp, nameStart);
                newName->m_assemblyName = new char[nameLen + 1];
                memcpy(newName->m_assemblyName, temp, nameLen * sizeof(newName->m_assemblyName[0]));
                newName->m_assemblyName[nameLen] = '\0';

                *ppPrevLink = newName;
                ppPrevLink = &newName->m_next;

                nameStart = NULL;
            }
        }
        else if (!nameStart)
        {
            //
            // Found the start of a new name
            //
            
            nameStart = listWalk;
        }
    }

    _ASSERTE(!nameStart); // cannot be in the middle of a name
    *ppPrevLink = NULL;
}

AssemblyNamesList::~AssemblyNamesList()
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    for (AssemblyName * pName = m_pNames; pName; /**/)
    {
        AssemblyName * cur = pName;
        pName = pName->m_next;

        delete [] cur->m_assemblyName;
        delete cur;
    }
}

bool AssemblyNamesList::IsInList(LPCUTF8 assemblyName)
{
    if (IsEmpty())
        return false;
    
    for (AssemblyName * pName = m_pNames; pName; pName = pName->m_next)
    {
        if (_stricmp(pName->m_assemblyName, assemblyName) == 0)
            return true;
    }

    return false;
}

//=============================================================================
// MethodNamesList
//=============================================================================
//  str should be of the form :
// "foo1 MyNamespace.MyClass:foo3 *:foo4 foo5(x,y,z)"
// "MyClass:foo2 MyClass:*" will match under _DEBUG
//

void MethodNamesListBase::Insert(__in_z LPWSTR str)
{
    CONTRACTL {
        THROWS;
    } CONTRACTL_END;

    enum State { NO_NAME, CLS_NAME, FUNC_NAME, ARG_LIST }; // parsing state machine

    const char   SEP_CHAR = ' ';     // current character use to separate each entry
//  const char   SEP_CHAR = ';';     // better  character use to separate each entry

    WCHAR lastChar = '?'; // dummy
    LPWSTR nameStart = NULL; // while walking over the classname or methodname, this points to start
    MethodName nameBuf; // Buffer used while parsing the current entry
    MethodName** lastName = &pNames; // last entry inserted into the list
    bool         bQuote   = false;

    nameBuf.methodName = NULL;
    nameBuf.className = NULL;
    nameBuf.numArgs = -1;
    nameBuf.next = NULL;

    for(State state = NO_NAME; lastChar != '\0'; str++)
    {
        lastChar = *str;

        switch(state)
        {
        case NO_NAME:
            if (*str != SEP_CHAR)
            {
                nameStart = str;
                state = CLS_NAME; // we have found the start of the next entry
            }
            break;

        case CLS_NAME:
            if (*nameStart == '"')
            {
                while (*str && *str!='"')
                {
                    str++;
                }
                nameStart++;
                bQuote=true;
            }

            if (*str == ':')
            {
                if (*nameStart == '*' && !bQuote)
                {
                    // Is the classname string a wildcard. Then set it to NULL
                    nameBuf.className = NULL;
                }
                else
                {
                    int len = (int)(str - nameStart);

                    // Take off the quote
                    if (bQuote) { len--; bQuote=false; }
                    
                    nameBuf.className = new char[len + 1];
                    MAKE_UTF8PTR_FROMWIDE(temp, nameStart);
                    memcpy(nameBuf.className, temp, len*sizeof(nameBuf.className[0]));
                    nameBuf.className[len] = '\0';
                }
                if (str[1] == ':')      // Accept class::name syntax too
                    str++;
                nameStart = str + 1;
                state = FUNC_NAME;
            }
            else if (*str == '\0' || *str == SEP_CHAR || *str == '(')
            {
                /* This was actually a method name without any class */
                nameBuf.className = NULL;
                goto DONE_FUNC_NAME;
            }
            break;

        case FUNC_NAME:
            if (*nameStart == '"')
            {
                while ( (nameStart==str)    || // workaround to handle when className!=NULL
                        (*str && *str!='"'))
                {
                    str++;
                }
                       
                nameStart++;
                bQuote=true;
            }

            if (*str == '\0' || *str == SEP_CHAR || *str == '(')
            {
            DONE_FUNC_NAME:
                _ASSERTE(*str == '\0' || *str == SEP_CHAR || *str == '(');

                if (*nameStart == '*' && !bQuote)
                {
                    // Is the name string a wildcard. Then set it to NULL
                    nameBuf.methodName = NULL;
                }
                else
                {
                    int len = (int)(str - nameStart);

                    // Take off the quote
                    if (bQuote) { len--; bQuote=false; }

                    nameBuf.methodName = new char[len + 1];
                    MAKE_UTF8PTR_FROMWIDE(temp, nameStart);
                    memcpy(nameBuf.methodName, temp, len*sizeof(nameBuf.methodName[0]));
                    nameBuf.methodName[len] = '\0';
                }

                if (*str == '\0' || *str == SEP_CHAR)
                {
                    nameBuf.numArgs = -1;
                    goto DONE_ARG_LIST;
                }
                else
                {
                    _ASSERTE(*str == '(');
                    nameBuf.numArgs = -1;
                    state = ARG_LIST;
                }
            }
            break;

        case ARG_LIST:
            if (*str == '\0' || *str == ')')
            {
                if (nameBuf.numArgs == -1)
                    nameBuf.numArgs = 0;

            DONE_ARG_LIST:
                _ASSERTE(*str == '\0' || *str == SEP_CHAR || *str == ')');

                // We have parsed an entire method name.
                // Create a new entry in the list for it

                MethodName * newName = new MethodName();
                *newName = nameBuf;
                newName->next = NULL;
                *lastName = newName;
                lastName = &newName->next;
                state = NO_NAME;

                // Skip anything after the argument list until we find the next
                // separator character, otherwise if we see "func(a,b):foo" we
                // create entries for "func(a,b)" as well as ":foo".
                if (*str == ')')
                {
                    while (*str && *str != SEP_CHAR)
                    {
                        str++;
                    }
                    lastChar = *str;
                }
            }
            else
            {
                if (*str != SEP_CHAR && nameBuf.numArgs == -1)
                    nameBuf.numArgs = 1;
                if (*str == ',')
                    nameBuf.numArgs++;
            }
            break;

        default: _ASSERTE(!"Bad state"); break;
        }
    }
}

/**************************************************************/

void MethodNamesListBase::Destroy() 
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;
    
    for(MethodName * pName = pNames; pName; /**/)
    {
        if (pName->className)
            delete [] pName->className;
        if (pName->methodName)
            delete [] pName->methodName;

        MethodName * curName = pName;
        pName = pName->next;
        delete curName;
    }
}

/**************************************************************/
bool MethodNamesListBase::IsInList(LPCUTF8 methName, LPCUTF8 clsName, PCCOR_SIGNATURE sig) 
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;
    
    ULONG numArgs = -1;
    if (sig != NULL)
    {
        sig++;      // Skip calling convention
        numArgs = CorSigUncompressData(sig);  
    }

    // Try to match all the entries in the list

    for(MethodName * pName = pNames; pName; pName = pName->next)
    {
        // If numArgs is valid, check for mismatch
        if (pName->numArgs != -1 && (ULONG)pName->numArgs != numArgs)
            continue;

        // If methodName is valid, check for mismatch
        if (pName->methodName) {
            if (strcmp(pName->methodName, methName) != 0) {

                // C++ embeds the class name into the method name,
                // deal with that here (workaround)
                const char* ptr = strchr(methName, ':');
                if (ptr != 0 && ptr[1] == ':' && strcmp(&ptr[2], pName->methodName) == 0) {
                    unsigned clsLen = (unsigned)(ptr - methName);
                    if (pName->className == 0 || strncmp(pName->className, methName, clsLen) == 0)
                        return true;
                }
                continue;
            }
        }

        // check for class Name exact match
        if (clsName == 0 || pName->className == 0 || strcmp(pName->className, clsName) == 0)
            return true;

        // check for suffix wildcard like System.*
        unsigned len = (unsigned)strlen(pName->className);
        if (len > 0 && pName->className[len-1] == '*' && strncmp(pName->className, clsName, len-1) == 0)
            return true;

#ifdef _DEBUG
            // Maybe className doesnt include namespace. Try to match that
        LPCUTF8 onlyClass = ns::FindSep(clsName);
        if (onlyClass && strcmp(pName->className, onlyClass+1) == 0)
            return true;
#endif
    }
    return(false);
}

//=============================================================================
// Signature Validation Functions (scaled down version from MDValidator
//=============================================================================

//*****************************************************************************
// This function validates one argument given an offset into the signature
// where the argument begins.  This function assumes that the signature is well
// formed as far as the compression scheme is concerned.
// <TODO>@todo: Validate tokens embedded.</TODO>
//*****************************************************************************
HRESULT validateOneArg(
    mdToken     tk,                     // [IN] Token whose signature needs to be validated.
    SigParser  *pSig, 
    ULONG       *pulNSentinels,         // [IN/OUT] Number of sentinels
    IMDInternalImport*  pImport,        // [IN] Internal MD Import interface ptr
    BOOL        bNoVoidAllowed)         // [IN] Flag indicating whether "void" is disallowed for this arg

{
    CONTRACTL
    {
        NOTHROW;
        SO_TOLERANT;
    }
    CONTRACTL_END;
    
    BYTE        elementType;          // Current element type being processed.
    mdToken     token;                  // Embedded token.
    ULONG       ulArgCnt;               // Argument count for function pointer.
    ULONG       ulIndex;                // Index for type parameters
    ULONG       ulRank;                 // Rank of the array.
    ULONG       ulSizes;                // Count of sized dimensions of the array.
    ULONG       ulLbnds;                // Count of lower bounds of the array.
    ULONG       ulCallConv;
    
    HRESULT     hr = S_OK;              // Value returned.
    BOOL        bRepeat = TRUE;         // MODOPT and MODREQ belong to the arg after them
    
    BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD(return COR_E_STACKOVERFLOW);
    while(bRepeat)
    {
        bRepeat = FALSE;
        // Validate that the argument is not missing.
        
        // Get the element type.
        if (FAILED(pSig->GetByte(&elementType)))
        {
            IfFailGo(VLDTR_E_SIG_MISSARG);
        }
        
        // Walk past all the modifier types.
        while (elementType & ELEMENT_TYPE_MODIFIER)
        {
            if (elementType == ELEMENT_TYPE_SENTINEL)
            {
                if(pulNSentinels) *pulNSentinels+=1;
                if(TypeFromToken(tk) != mdtMemberRef) IfFailGo(VLDTR_E_SIG_SENTINMETHODDEF);
            }
            if (FAILED(pSig->GetByte(&elementType)))
            {
                IfFailGo(VLDTR_E_SIG_MISSELTYPE);
            }
        }
        
        switch (elementType)
        {
            case ELEMENT_TYPE_VOID:
                if(bNoVoidAllowed) IfFailGo(VLDTR_E_SIG_BADVOID);
                
            case ELEMENT_TYPE_BOOLEAN:
            case ELEMENT_TYPE_CHAR:
            case ELEMENT_TYPE_I1:
            case ELEMENT_TYPE_U1:
            case ELEMENT_TYPE_I2:
            case ELEMENT_TYPE_U2:
            case ELEMENT_TYPE_I4:
            case ELEMENT_TYPE_U4:
            case ELEMENT_TYPE_I8:
            case ELEMENT_TYPE_U8:
            case ELEMENT_TYPE_R4:
            case ELEMENT_TYPE_R8:
            case ELEMENT_TYPE_STRING:
            case ELEMENT_TYPE_OBJECT:
            case ELEMENT_TYPE_TYPEDBYREF:
            case ELEMENT_TYPE_U:
            case ELEMENT_TYPE_I:
                break;
            case ELEMENT_TYPE_PTR:
                // Validate the referenced type.
                if(FAILED(hr = validateOneArg(tk, pSig, pulNSentinels, pImport, FALSE))) IfFailGo(hr);
                break;
            case ELEMENT_TYPE_BYREF:  //fallthru
                if(TypeFromToken(tk)==mdtFieldDef) IfFailGo(VLDTR_E_SIG_BYREFINFIELD);
            case ELEMENT_TYPE_PINNED:
            case ELEMENT_TYPE_SZARRAY:
                // Validate the referenced type.
                if(FAILED(hr = validateOneArg(tk, pSig, pulNSentinels, pImport, TRUE))) IfFailGo(hr);
                break;
            case ELEMENT_TYPE_CMOD_OPT:
            case ELEMENT_TYPE_CMOD_REQD:
                bRepeat = TRUE; // go on validating, we're not done with this arg
            case ELEMENT_TYPE_VALUETYPE: //fallthru
            case ELEMENT_TYPE_CLASS:
                // See if the token is missing.
                if (FAILED(pSig->GetToken(&token)))
                {
                    IfFailGo(VLDTR_E_SIG_MISSTKN);
                }
                // Token validation .
                if(pImport)
                {
                    ULONG   rid = RidFromToken(token);
                    ULONG   typ = TypeFromToken(token);
                    ULONG   maxrid = pImport->GetCountWithTokenKind(typ);
                    if(typ == mdtTypeDef) maxrid++;
                    if((rid==0)||(rid > maxrid)) IfFailGo(VLDTR_E_SIG_TKNBAD);
                }
                break;
                
            case ELEMENT_TYPE_FNPTR: 
                // <TODO>@todo: More function pointer validation?</TODO>
                // Validate that calling convention is present.
                if (FAILED(pSig->GetCallingConvInfo(&ulCallConv)))
                {
                    IfFailGo(VLDTR_E_SIG_MISSFPTR);
                }
                if(((ulCallConv & IMAGE_CEE_CS_CALLCONV_MASK) >= IMAGE_CEE_CS_CALLCONV_MAX) 
                    ||((ulCallConv & IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS)
                    &&(!(ulCallConv & IMAGE_CEE_CS_CALLCONV_HASTHIS)))) IfFailGo(VLDTR_E_MD_BADCALLINGCONV);
                
                // Validate that argument count is present.
                if (FAILED(pSig->GetData(&ulArgCnt)))
                {
                    IfFailGo(VLDTR_E_SIG_MISSFPTRARGCNT);
                }
                
                // FNPTR signature must follow the rules of MethodDef
                // Validate and consume return type.
                IfFailGo(validateOneArg(mdtMethodDef, pSig, NULL, pImport, FALSE));
                
                // Validate and consume the arguments.
                while(ulArgCnt--)
                {
                    IfFailGo(validateOneArg(mdtMethodDef, pSig, NULL, pImport, TRUE));
                }
                break;
                
            case ELEMENT_TYPE_ARRAY:
                // Validate and consume the base type.
                IfFailGo(validateOneArg(tk, pSig, pulNSentinels, pImport, TRUE));
                
                // Validate that the rank is present.
                if (FAILED(pSig->GetData(&ulRank)))
                {
                    IfFailGo(VLDTR_E_SIG_MISSRANK);
                }
                
                // Process the sizes.
                if (ulRank)
                {
                    // Validate that the count of sized-dimensions is specified.
                    if (FAILED(pSig->GetData(&ulSizes)))
                    {
                        IfFailGo(VLDTR_E_SIG_MISSNSIZE);
                    }
                    
                    // Loop over the sizes.
                    while(ulSizes--)
                    {
                        // Validate the current size.
                        if (FAILED(pSig->GetData(NULL)))
                        {
                            IfFailGo(VLDTR_E_SIG_MISSSIZE);
                        }
                    }
                    
                    // Validate that the count of lower bounds is specified.
                    if (FAILED(pSig->GetData(&ulLbnds)))
                    {
                        IfFailGo(VLDTR_E_SIG_MISSNLBND);
                    }
                    
                    // Loop over the lower bounds.
                    while(ulLbnds--)
                    {
                        // Validate the current lower bound.
                        if (FAILED(pSig->GetData(NULL)))
                        {
                            IfFailGo(VLDTR_E_SIG_MISSLBND);
                        }
                    }
                }
                break;
                case ELEMENT_TYPE_VAR:
                case ELEMENT_TYPE_MVAR:
                    // Validate that index is present.
                    if (FAILED(pSig->GetData(&ulIndex)))
                    {
                        IfFailGo(VLDTR_E_SIG_MISSFPTRARGCNT);
                    }
                    
                    //@todo GENERICS: check that index is in range
                    break;
                    
                case ELEMENT_TYPE_GENERICINST:
                    // Validate the generic type.
                    IfFailGo(validateOneArg(tk, pSig, pulNSentinels, pImport, TRUE));
                    
                    // Validate that parameter count is present.
                    if (FAILED(pSig->GetData(&ulArgCnt)))
                    {
                        IfFailGo(VLDTR_E_SIG_MISSFPTRARGCNT);
                    }
                    
                //@todo GENERICS: check that number of parameters matches definition?
                    
                    // Validate and consume the parameters.
                    while(ulArgCnt--)
                    {
                        IfFailGo(validateOneArg(tk, pSig, NULL, pImport, TRUE));
                    }
                    break;
                    
            case ELEMENT_TYPE_SENTINEL: // this case never works because all modifiers are skipped before switch
                if(TypeFromToken(tk) == mdtMethodDef) IfFailGo(VLDTR_E_SIG_SENTINMETHODDEF);
                break;
                
            default:
                IfFailGo(VLDTR_E_SIG_BADELTYPE);
                break;
        }   // switch (ulElementType)
    } // end while(bRepeat)
ErrExit:
    
    END_SO_INTOLERANT_CODE;
    return hr;
}   // validateOneArg()

//*****************************************************************************
// This function validates the given Method/Field/Standalone signature.  
//@todo GENERICS: MethodInstantiation?
//*****************************************************************************
HRESULT validateTokenSig(
    mdToken             tk,                     // [IN] Token whose signature needs to be validated.
    PCCOR_SIGNATURE     pbSig,                  // [IN] Signature.
    ULONG               cbSig,                  // [IN] Size in bytes of the signature.
    DWORD               dwFlags,                // [IN] Method flags.
    IMDInternalImport*  pImport)               // [IN] Internal MD Import interface ptr
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;
    
    ULONG       ulCallConv;             // Calling convention.
    ULONG       ulArgCount = 1;         // Count of arguments (1 because of the return type)
    ULONG       ulTyArgCount = 0;         // Count of type arguments
    ULONG       ulArgIx = 0;            // Starting index of argument (standalone sig: 1)
    ULONG       i;                      // Looping index.
    HRESULT     hr = S_OK;              // Value returned.
    ULONG       ulNSentinels = 0;
    SigParser   sig(pbSig, cbSig);
    
    _ASSERTE(TypeFromToken(tk) == mdtMethodDef ||
             TypeFromToken(tk) == mdtMemberRef ||
             TypeFromToken(tk) == mdtSignature ||
             TypeFromToken(tk) == mdtFieldDef);

    // Check for NULL signature.
    if (!pbSig || !cbSig) return VLDTR_E_SIGNULL;

    // Validate the calling convention.
    
    // Moves behind calling convention
    IfFailRet(sig.GetCallingConvInfo(&ulCallConv));
    i = ulCallConv & IMAGE_CEE_CS_CALLCONV_MASK;
    switch(TypeFromToken(tk))
    {
        case mdtMethodDef: // MemberRefs have no flags available
            // If HASTHIS is set on the calling convention, the method should not be static.
            if ((ulCallConv & IMAGE_CEE_CS_CALLCONV_HASTHIS) &&
                IsMdStatic(dwFlags)) return VLDTR_E_MD_THISSTATIC;
            
            // If HASTHIS is not set on the calling convention, the method should be static.
            if (!(ulCallConv & IMAGE_CEE_CS_CALLCONV_HASTHIS) &&
                !IsMdStatic(dwFlags)) return VLDTR_E_MD_NOTTHISNOTSTATIC;
            // fall thru to callconv check;
            
        case mdtMemberRef:
            if(i == IMAGE_CEE_CS_CALLCONV_FIELD) return validateOneArg(tk, &sig, NULL, pImport, TRUE);
            
            // EXPLICITTHIS and native call convs are for stand-alone sigs only (for calli)
            if(((i != IMAGE_CEE_CS_CALLCONV_DEFAULT)&&( i != IMAGE_CEE_CS_CALLCONV_VARARG))
                || (ulCallConv & IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS)) return VLDTR_E_MD_BADCALLINGCONV;
            break;
            
        case mdtSignature:
            if(i != IMAGE_CEE_CS_CALLCONV_LOCAL_SIG) // then it is function sig for calli
            {
                if((i >= IMAGE_CEE_CS_CALLCONV_MAX) 
                    ||((ulCallConv & IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS)
                    &&(!(ulCallConv & IMAGE_CEE_CS_CALLCONV_HASTHIS)))) return VLDTR_E_MD_BADCALLINGCONV;
            }
            else
                ulArgIx = 1;        // Local variable signatures don't have a return type 
            break;
            
        case mdtFieldDef:
            if(i != IMAGE_CEE_CS_CALLCONV_FIELD) return VLDTR_E_MD_BADCALLINGCONV;
            return validateOneArg(tk, &sig, NULL, pImport, TRUE);
    }
    // Is there any sig left for arguments?
    
    // Get the type argument count
    if (ulCallConv & IMAGE_CEE_CS_CALLCONV_GENERIC)
    {
        if (FAILED(sig.GetData(&ulTyArgCount)))
        {
            return VLDTR_E_MD_NOARGCNT;
        }
    }
    
    // Get the argument count.
    if (FAILED(sig.GetData(&ulArgCount)))
    {
        return VLDTR_E_MD_NOARGCNT;
    }
    
    // Validate the return type and the arguments.
    // (at this moment ulArgCount = num.args+1, ulArgIx = (standalone sig. ? 1 :0); )
    for(; ulArgIx < ulArgCount; ulArgIx++)
    {
        if(FAILED(hr = validateOneArg(tk, &sig, &ulNSentinels, pImport, (ulArgIx!=0)))) return hr;
    }
    
    // <TODO>@todo: we allow junk to be at the end of the signature (we may not consume it all)
    // do we care?</TODO>
    
    if((ulNSentinels != 0) && ((ulCallConv & IMAGE_CEE_CS_CALLCONV_MASK) != IMAGE_CEE_CS_CALLCONV_VARARG ))
        return VLDTR_E_SIG_SENTMUSTVARARG;
    if(ulNSentinels > 1) return VLDTR_E_SIG_MULTSENTINELS;
    return S_OK;
}   // validateTokenSig()

const CHAR g_VersionBase[] = "v1.";
const CHAR g_DevelopmentVersion[] = "x86";
const CHAR g_RetString[] = "retail";
const CHAR g_ComplusString[] = "COMPLUS";

const WCHAR g_VersionBaseW[] = W("v1.");
const WCHAR g_DevelopmentVersionW[] = W("x86");
const WCHAR g_RetStringW[] = W("retail");
const WCHAR g_ComplusStringW[] = W("COMPLUS");

//*****************************************************************************
// Determine the version number of the runtime that was used to build the
// specified image. The pMetadata pointer passed in is the pointer to the
// metadata contained in the image.
//*****************************************************************************
static BOOL IsReallyRTM(LPCWSTR szVersion)
{
    LIMITED_METHOD_CONTRACT;
    if (szVersion==NULL)
        return FALSE;

    size_t lgth = sizeof(g_VersionBaseW) / sizeof(WCHAR) - 1;
    size_t foundLgth = wcslen(szVersion);

    // Have normal version, v1.*
    if ( (foundLgth >= lgth+2) &&
         !wcsncmp(szVersion, g_VersionBaseW, lgth) ) {

        // v1.0.* means RTM
        if (szVersion[lgth+1] == W('.')) {
            if (szVersion[lgth] == W('0'))
               return TRUE;
        }
        
        // Check for dev version (v1.x86ret, v1.x86fstchk...)
        else if(!wcsncmp(szVersion+lgth, g_DevelopmentVersionW,
                         (sizeof(g_DevelopmentVersionW) / sizeof(WCHAR) - 1)))
            return TRUE;
    }
    // Some weird version...
    else if( (!wcscmp(szVersion, g_RetStringW)) ||
             (!wcscmp(szVersion, g_ComplusStringW)) )
        return TRUE;
    return FALSE;   
}

void AdjustImageRuntimeVersion(SString* pVersion)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;
    
    if (IsReallyRTM(*pVersion))
    {
        pVersion->SetANSI(g_RTMVersion);
    }
};

HRESULT GetImageRuntimeVersionString(PVOID pMetaData, LPCSTR* pString)
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;
    
    _ASSERTE(pString);
    STORAGESIGNATURE* pSig = (STORAGESIGNATURE*) pMetaData;

    // Verify the signature. 

    // If signature didn't match, you shouldn't be here.
    if (pSig->GetSignature() != STORAGE_MAGIC_SIG)
        return CLDB_E_FILE_CORRUPT;

    // The version started in version 1.1
    if (pSig->GetMajorVer() < 1)
        return CLDB_E_FILE_OLDVER;

    if (pSig->GetMajorVer() == 1 && pSig->GetMinorVer() < 1)
        return CLDB_E_FILE_OLDVER;
    
    // Header data starts after signature.
    *pString = (LPCSTR) pSig->pVersion;
    if(*pString) {
        size_t lgth = sizeof(g_VersionBase) / sizeof(char) - 1;
        size_t foundLgth = strlen(*pString);

        // Have normal version, v1.*
        if ( (foundLgth >= lgth+2) &&
             !strncmp(*pString, g_VersionBase, lgth) ) {

            // v1.0.* means RTM
            if ((*pString)[lgth+1] == '.') {
                if ((*pString)[lgth] == '0')
                    *pString = g_RTMVersion;
            }
            
            // Check for dev version (v1.x86ret, v1.x86fstchk...)
            else if(!strncmp(&(*pString)[lgth], g_DevelopmentVersion,
                             (sizeof(g_DevelopmentVersion) / sizeof(char) - 1)))
                *pString = g_RTMVersion;
        }

        // Some weird version...
        else if( (!strcmp(*pString, g_RetString)) ||
                 (!strcmp(*pString, g_ComplusString)) )
            *pString = g_RTMVersion;
    }

    return S_OK;
}

//*****************************************************************************
// Convert a UTF8 string to Unicode, into a CQuickArray<WCHAR>.
//*****************************************************************************
HRESULT Utf2Quick(
    LPCUTF8     pStr,                   // The string to convert.
    CQuickArray<WCHAR> &rStr,           // The QuickArray<WCHAR> to convert it into.
    int         iCurLen)                // Inital characters in the array to leave (default 0).
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;
    
    HRESULT     hr = S_OK;              // A result.
    int         iReqLen;                // Required additional length.
    int         iActLen;
    int         bAlloc = 0;             // If non-zero, allocation was required.

    if (iCurLen < 0 )
    {
        _ASSERTE_MSG(false, "Invalid current length");
        return E_INVALIDARG;
    }

    // Calculate the space available
    S_SIZE_T cchAvail = S_SIZE_T(rStr.MaxSize()) - S_SIZE_T(iCurLen);
    if (cchAvail.IsOverflow() || cchAvail.Value() > INT_MAX)
    {
        _ASSERTE_MSG(false, "Integer overflow/underflow");
        return HRESULT_FROM_WIN32(ERROR_ARITHMETIC_OVERFLOW);
    }

    // Attempt the conversion.
    LPWSTR rNewStr = rStr.Ptr()+iCurLen;
    if(rNewStr < rStr.Ptr())
    {
        _ASSERTE_MSG(false, "Integer overflow/underflow");
        return HRESULT_FROM_WIN32(ERROR_ARITHMETIC_OVERFLOW);
    }
    iReqLen = WszMultiByteToWideChar(CP_UTF8, 0, pStr, -1, rNewStr, (int)(cchAvail.Value()));

    // If the buffer was too small, determine what is required.
    if (iReqLen == 0) 
        bAlloc = iReqLen = WszMultiByteToWideChar(CP_UTF8, 0, pStr, -1, 0, 0);
    // Resize the buffer.  If the buffer was large enough, this just sets the internal
    //  counter, but if it was too small, this will attempt a reallocation.  Note that 
    //  the length includes the terminating W('/0').
    IfFailGo(rStr.ReSizeNoThrow(iCurLen+iReqLen));
    // If we had to realloc, then do the conversion again, now that the buffer is 
    //  large enough.
    if (bAlloc) {
        //recalculating cchAvail since MaxSize could have been changed.
        cchAvail = S_SIZE_T(rStr.MaxSize()) - S_SIZE_T(iCurLen);
        if (cchAvail.IsOverflow() || cchAvail.Value() > INT_MAX)
        {
            _ASSERTE_MSG(false, "Integer overflow/underflow");
            return HRESULT_FROM_WIN32(ERROR_ARITHMETIC_OVERFLOW);
        }
        //reculculating rNewStr
        rNewStr = rStr.Ptr()+iCurLen;
        
        if(rNewStr < rStr.Ptr())
        {
        _ASSERTE_MSG(false, "Integer overflow/underflow");
        return HRESULT_FROM_WIN32(ERROR_ARITHMETIC_OVERFLOW);
        }
        iActLen = WszMultiByteToWideChar(CP_UTF8, 0, pStr, -1, rNewStr, (int)(cchAvail.Value()));
        _ASSERTE(iReqLen == iActLen);
    }
ErrExit:
    return hr;
} // HRESULT Utf2Quick()


//*****************************************************************************
//  Extract the movl 64-bit unsigned immediate from an IA64 bundle
//  (Format X2)
//*****************************************************************************
UINT64 GetIA64Imm64(UINT64 * pBundle)
{
    WRAPPER_NO_CONTRACT;
    
    UINT64 temp0 = PTR_UINT64(pBundle)[0];
    UINT64 temp1 = PTR_UINT64(pBundle)[1];

    return GetIA64Imm64(temp0, temp1);
}

UINT64 GetIA64Imm64(UINT64 qword0, UINT64 qword1)
{
    LIMITED_METHOD_CONTRACT;
    
    UINT64 imm64 = 0;
    
#ifdef _DEBUG_IMPL
    //
    // make certain we're decoding a movl opcode, with template 4 or 5
    //
    UINT64    templa = (qword0 >>  0) & 0x1f;
    UINT64    opcode = (qword1 >> 60) & 0xf;
    
    _ASSERTE((opcode == 0x6) && ((templa == 0x4) || (templa == 0x5)));
#endif        

    imm64  = (qword1 >> 59) << 63;       //  1 i
    imm64 |= (qword1 << 41) >>  1;       // 23 high bits of imm41 
    imm64 |= (qword0 >> 46) << 22;       // 18 low  bits of imm41
    imm64 |= (qword1 >> 23) & 0x200000;  //  1 ic
    imm64 |= (qword1 >> 29) & 0x1F0000;  //  5 imm5c
    imm64 |= (qword1 >> 43) & 0xFF80;    //  9 imm9d
    imm64 |= (qword1 >> 36) & 0x7F;      //  7 imm7b

    return imm64;
}

//*****************************************************************************
//  Deposit the movl 64-bit unsigned immediate into an IA64 bundle
//  (Format X2)
//*****************************************************************************
void PutIA64Imm64(UINT64 * pBundle, UINT64 imm64)
{
    LIMITED_METHOD_CONTRACT;
    
#ifdef _DEBUG_IMPL
    //
    // make certain we're decoding a movl opcode, with template 4 or 5
    //
    UINT64    templa = (pBundle[0] >>  0) & 0x1f;
    UINT64    opcode = (pBundle[1] >> 60) & 0xf ;
    
    _ASSERTE((opcode == 0x6) && ((templa == 0x4) || (templa == 0x5)));
#endif        

    const UINT64 mask0 = UI64(0x00003FFFFFFFFFFF);
    const UINT64 mask1 = UI64(0xF000080FFF800000);

    /* Clear all bits used as part of the imm64 */
    pBundle[0] &= mask0;
    pBundle[1] &= mask1;

    UINT64 temp0;
    UINT64 temp1;

    temp1  = (imm64 >> 63)      << 59;  //  1 i
    temp1 |= (imm64 & 0xFF80)   << 43;  //  9 imm9d
    temp1 |= (imm64 & 0x1F0000) << 29;  //  5 imm5c
    temp1 |= (imm64 & 0x200000) << 23;  //  1 ic
    temp1 |= (imm64 & 0x7F)     << 36;  //  7 imm7b
    temp1 |= (imm64 <<  1)      >> 41;  // 23 high bits of imm41
    temp0  = (imm64 >> 22)      << 46;  // 18 low bits of imm41

    /* Or in the new bits used in the imm64 */
    pBundle[0] |= temp0;
    pBundle[1] |= temp1;
    FlushInstructionCache(GetCurrentProcess(),pBundle,16);
}

//*****************************************************************************
//  Extract the IP-Relative signed 25-bit immediate from an IA64 bundle 
//  (Formats B1, B2 or B3)
//  Note that due to branch target alignment requirements 
//       the lowest four bits in the result will always be zero.
//*****************************************************************************
INT32 GetIA64Rel25(UINT64 * pBundle, UINT32 slot)
{
    WRAPPER_NO_CONTRACT;
    
    UINT64 temp0 = PTR_UINT64(pBundle)[0];
    UINT64 temp1 = PTR_UINT64(pBundle)[1];

    return GetIA64Rel25(temp0, temp1, slot);
}

INT32 GetIA64Rel25(UINT64 qword0, UINT64 qword1, UINT32 slot)
{
    LIMITED_METHOD_CONTRACT;
    
    INT32 imm25 = 0;
    
    if (slot == 2)
    {
        if ((qword1 >> 59) & 1)
            imm25 = 0xFF000000;
        imm25 |= (qword1 >> 32) & 0x00FFFFF0;    // 20 imm20b
    }
    else if (slot == 1)
    {
        if ((qword1 >> 18) & 1)
            imm25 = 0xFF000000;
        imm25 |= (qword1 <<  9) & 0x00FFFE00;    // high 15 of imm20b
        imm25 |= (qword0 >> 55) & 0x000001F0;    // low   5 of imm20b
    }
    else if (slot == 0)
    {
        if ((qword0 >> 41) & 1)
            imm25 = 0xFF000000;
        imm25 |= (qword0 >> 14) & 0x00FFFFF0;    // 20 imm20b
    }

    return imm25;
}

//*****************************************************************************
//  Deposit the IP-Relative signed 25-bit immediate into an IA64 bundle
//  (Formats B1, B2 or B3)
//  Note that due to branch target alignment requirements 
//       the lowest four bits are required to be zero.
//*****************************************************************************
void PutIA64Rel25(UINT64 * pBundle, UINT32 slot, INT32 imm25)
{
    LIMITED_METHOD_CONTRACT;
    
    _ASSERTE((imm25 & 0xF) == 0);

    if (slot == 2)
    {
        const UINT64 mask1 = UI64(0xF700000FFFFFFFFF);
        /* Clear all bits used as part of the imm25 */
        pBundle[1] &= mask1;

        UINT64 temp1;
        
        temp1  = (UINT64) (imm25 & 0x1000000) << 35;     //  1 s
        temp1 |= (UINT64) (imm25 & 0x0FFFFF0) << 32;     // 20 imm20b
        
        /* Or in the new bits used in the imm64 */
        pBundle[1] |= temp1;
    }
    else if (slot == 1)
    {
        const UINT64 mask0 = UI64(0x0EFFFFFFFFFFFFFF);
        const UINT64 mask1 = UI64(0xFFFFFFFFFFFB8000);
        /* Clear all bits used as part of the imm25 */
        pBundle[0] &= mask0;
        pBundle[1] &= mask1;
        
        UINT64 temp0;
        UINT64 temp1;
        
        temp1  = (UINT64) (imm25 & 0x1000000) >>  7;     //  1 s
        temp1 |= (UINT64) (imm25 & 0x0FFFE00) >>  9;     // high 15 of imm20b
        temp0  = (UINT64) (imm25 & 0x00001F0) << 55;     // low   5 of imm20b
        
        /* Or in the new bits used in the imm64 */
        pBundle[0] |= temp0;
        pBundle[1] |= temp1;
    }
    else if (slot == 0)
    {
        const UINT64 mask0 = UI64(0xFFFFFDC00003FFFF);
        /* Clear all bits used as part of the imm25 */
        pBundle[0] &= mask0;

        UINT64 temp0;
        
        temp0  = (UINT64) (imm25 & 0x1000000) << 16;     //  1 s
        temp0 |= (UINT64) (imm25 & 0x0FFFFF0) << 14;     // 20 imm20b
        
        /* Or in the new bits used in the imm64 */
        pBundle[0] |= temp0;

    }
    FlushInstructionCache(GetCurrentProcess(),pBundle,16);
}

//*****************************************************************************
//  Extract the IP-Relative signed 64-bit immediate from an IA64 bundle 
//  (Formats X3 or X4)
//*****************************************************************************
INT64 GetIA64Rel64(UINT64 * pBundle)
{
    WRAPPER_NO_CONTRACT;
    
    UINT64 temp0 = PTR_UINT64(pBundle)[0];
    UINT64 temp1 = PTR_UINT64(pBundle)[1];

    return GetIA64Rel64(temp0, temp1);
}

INT64 GetIA64Rel64(UINT64 qword0, UINT64 qword1)
{
    LIMITED_METHOD_CONTRACT;
    
    INT64 imm64 = 0;
    
#ifdef _DEBUG_IMPL
    //
    // make certain we're decoding a brl opcode, with template 4 or 5
    //
    UINT64       templa = (qword0 >>  0) & 0x1f;
    UINT64       opcode = (qword1 >> 60) & 0xf;
    
    _ASSERTE(((opcode == 0xC) || (opcode == 0xD)) &&
             ((templa == 0x4) || (templa == 0x5)));
#endif        

    imm64  = (qword1 >> 59) << 63;         //  1 i
    imm64 |= (qword1 << 41) >>  1;         // 23 high bits of imm39 
    imm64 |= (qword0 >> 48) << 24;         // 16 low  bits of imm39
    imm64 |= (qword1 >> 32) & 0xFFFFF0;    // 20 imm20b
                                          //  4 bits of zeros
    return imm64;
}

//*****************************************************************************
//  Deposit the IP-Relative signed 64-bit immediate into an IA64 bundle
//  (Formats X3 or X4)
//*****************************************************************************
void PutIA64Rel64(UINT64 * pBundle, INT64 imm64)
{
    LIMITED_METHOD_CONTRACT;
    
#ifdef _DEBUG_IMPL
    //
    // make certain we're decoding a brl opcode, with template 4 or 5
    //
    UINT64    templa = (pBundle[0] >>  0) & 0x1f;
    UINT64    opcode = (pBundle[1] >> 60) & 0xf;
    
    _ASSERTE(((opcode == 0xC) || (opcode == 0xD)) &&
             ((templa == 0x4) || (templa == 0x5)));
    _ASSERTE((imm64 & 0xF) == 0);
#endif        

    const UINT64 mask0 = UI64(0x00003FFFFFFFFFFF);
    const UINT64 mask1 = UI64(0xF700000FFF800000);

    /* Clear all bits used as part of the imm64 */
    pBundle[0] &= mask0;
    pBundle[1] &= mask1;

    UINT64 temp0  = (imm64 & UI64(0x000000FFFF000000)) << 24;  // 16 low  bits of imm39
    UINT64 temp1  = (imm64 & UI64(0x8000000000000000)) >>  4   //  1 i
                  | (imm64 & UI64(0x7FFFFF0000000000)) >> 40   // 23 high bits of imm39 
                  | (imm64 & UI64(0x0000000000FFFFF0)) << 32;  // 20 imm20b

    /* Or in the new bits used in the imm64 */
    pBundle[0] |= temp0;
    pBundle[1] |= temp1;
    FlushInstructionCache(GetCurrentProcess(),pBundle,16);
}

//*****************************************************************************
//  Extract the 16-bit immediate from ARM Thumb2 Instruction (format T2_N)
//*****************************************************************************
static FORCEINLINE UINT16 GetThumb2Imm16(UINT16 * p)
{
    LIMITED_METHOD_CONTRACT;
    
    return ((p[0] << 12) & 0xf000) |
           ((p[0] <<  1) & 0x0800) |
           ((p[1] >>  4) & 0x0700) |
           ((p[1] >>  0) & 0x00ff);
}

//*****************************************************************************
//  Extract the 32-bit immediate from movw/movt sequence
//*****************************************************************************
UINT32 GetThumb2Mov32(UINT16 * p)
{
    LIMITED_METHOD_CONTRACT;

    // Make sure we are decoding movw/movt sequence
    _ASSERTE_IMPL((*(p+0) & 0xFBF0) == 0xF240);
    _ASSERTE_IMPL((*(p+2) & 0xFBF0) == 0xF2C0);

    return (UINT32)GetThumb2Imm16(p) + ((UINT32)GetThumb2Imm16(p + 2) << 16);
}

//*****************************************************************************
//  Deposit the 16-bit immediate into ARM Thumb2 Instruction (format T2_N)
//*****************************************************************************
static FORCEINLINE void PutThumb2Imm16(UINT16 * p, UINT16 imm16)
{
    LIMITED_METHOD_CONTRACT;

    USHORT Opcode0 = p[0];
    USHORT Opcode1 = p[1];
    Opcode0 &= ~((0xf000 >> 12) | (0x0800 >> 1));
    Opcode1 &= ~((0x0700 <<  4) | (0x00ff << 0));
    Opcode0 |= (imm16 & 0xf000) >> 12;
    Opcode0 |= (imm16 & 0x0800) >>  1;
    Opcode1 |= (imm16 & 0x0700) <<  4;
    Opcode1 |= (imm16 & 0x00ff) <<  0;
    p[0] = Opcode0;
    p[1] = Opcode1;
}

//*****************************************************************************
//  Deposit the 32-bit immediate into movw/movt Thumb2 sequence
//*****************************************************************************
void PutThumb2Mov32(UINT16 * p, UINT32 imm32)
{
    LIMITED_METHOD_CONTRACT;

    // Make sure we are decoding movw/movt sequence
    _ASSERTE_IMPL((*(p+0) & 0xFBF0) == 0xF240);
    _ASSERTE_IMPL((*(p+2) & 0xFBF0) == 0xF2C0);

    PutThumb2Imm16(p, (UINT16)imm32);
    PutThumb2Imm16(p + 2, (UINT16)(imm32 >> 16));
}

//*****************************************************************************
//  Extract the 24-bit rel offset from bl instruction
//*****************************************************************************
INT32 GetThumb2BlRel24(UINT16 * p)
{
    LIMITED_METHOD_CONTRACT;

    USHORT Opcode0 = p[0];
    USHORT Opcode1 = p[1];

    UINT32 S  = Opcode0 >> 10;
    UINT32 J2 = Opcode1 >> 11;
    UINT32 J1 = Opcode1 >> 13;

    INT32 ret = 
        ((S << 24)              & 0x1000000) |
        (((J1 ^ S ^ 1) << 23)   & 0x0800000) |
        (((J2 ^ S ^ 1) << 22)   & 0x0400000) |
        ((Opcode0 << 12)        & 0x03FF000) |
        ((Opcode1 <<  1)        & 0x0000FFE);

    // Sign-extend and return
    return (ret << 7) >> 7;
}

//*****************************************************************************
//  Extract the 24-bit rel offset from bl instruction
//*****************************************************************************
void PutThumb2BlRel24(UINT16 * p, INT32 imm24)
{
    LIMITED_METHOD_CONTRACT;

    // Verify that we got a valid offset
    _ASSERTE(FitsInThumb2BlRel24(imm24));

#if defined(_TARGET_ARM_)
    // Ensure that the ThumbBit is not set on the offset
    // as it cannot be encoded.
    _ASSERTE(!(imm24 & THUMB_CODE));
#endif // _TARGET_ARM_    

    USHORT Opcode0 = p[0];
    USHORT Opcode1 = p[1];
    Opcode0 &= 0xF800;
    Opcode1 &= 0xD000;

    UINT32 S  =  (imm24 & 0x1000000) >> 24;
    UINT32 J1 = ((imm24 & 0x0800000) >> 23) ^ S ^ 1;
    UINT32 J2 = ((imm24 & 0x0400000) >> 22) ^ S ^ 1;

    Opcode0 |=  ((imm24 & 0x03FF000) >> 12) | (S << 10);
    Opcode1 |=  ((imm24 & 0x0000FFE) >>  1) | (J1 << 13) | (J2 << 11);

    p[0] = Opcode0;
    p[1] = Opcode1;

    _ASSERTE(GetThumb2BlRel24(p) == imm24);
}

//*****************************************************************************
//  Extract the PC-Relative offset from a b or bl instruction 
//*****************************************************************************
INT32 GetArm64Rel28(UINT32 * pCode)
{
    LIMITED_METHOD_CONTRACT;

    UINT32 branchInstr = *pCode;

    // first shift 6 bits left to set the sign bit, 
    // then arithmetic shift right by 4 bits 
    INT32 imm28 = (((INT32)(branchInstr & 0x03FFFFFF)) << 6) >> 4;

    return imm28;
}

//*****************************************************************************
//  Deposit the PC-Relative offset 'imm28' into a b or bl instruction 
//*****************************************************************************
void PutArm64Rel28(UINT32 * pCode, INT32 imm28)
{
    LIMITED_METHOD_CONTRACT;

    // Verify that we got a valid offset
    _ASSERTE(FitsInRel28(imm28));
    _ASSERTE((imm28 & 0x3) == 0);    // the low two bits must be zero

    UINT32 branchInstr = *pCode;

    branchInstr &= 0xFC000000;       // keep bits 31-26

    // Assemble the pc-relative delta 'imm28' into the branch instruction
    branchInstr |= ((imm28 >> 2) & 0x03FFFFFF);

    *pCode = branchInstr;          // write the assembled instruction

    _ASSERTE(GetArm64Rel28(pCode) == imm28);
}

//---------------------------------------------------------------------
// Splits a command line into argc/argv lists, using the VC7 parsing rules.
//
// This functions interface mimics the CommandLineToArgvW api.
//
// If function fails, returns NULL.
//
// If function suceeds, call delete [] on return pointer when done.
//
//---------------------------------------------------------------------
// NOTE: Implementation-wise, once every few years it would be a good idea to
// compare this code with the C runtime library's parse_cmdline method,
// which is in vctools\crt\crtw32\startup\stdargv.c.  (Note we don't
// support wild cards, and we use Unicode characters exclusively.)
// We are up to date as of ~6/2005.
//---------------------------------------------------------------------
LPWSTR *SegmentCommandLine(LPCWSTR lpCmdLine, DWORD *pNumArgs)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FAULT;


    *pNumArgs = 0;

    int nch = (int)wcslen(lpCmdLine);

    // Calculate the worstcase storage requirement. (One pointer for
    // each argument, plus storage for the arguments themselves.)
    int cbAlloc = (nch+1)*sizeof(LPWSTR) + sizeof(WCHAR)*(nch + 1);
    LPWSTR pAlloc = new (nothrow) WCHAR[cbAlloc / sizeof(WCHAR)];
    if (!pAlloc)
        return NULL;

    LPWSTR *argv = (LPWSTR*) pAlloc;  // We store the argv pointers in the first halt
    LPWSTR  pdst = (LPWSTR)( ((BYTE*)pAlloc) + sizeof(LPWSTR)*(nch+1) ); // A running pointer to second half to store arguments
    LPCWSTR psrc = lpCmdLine;
    WCHAR   c;
    BOOL    inquote;
    BOOL    copychar;
    int     numslash;

    // First, parse the program name (argv[0]). Argv[0] is parsed under
    // special rules. Anything up to the first whitespace outside a quoted
    // subtring is accepted. Backslashes are treated as normal characters.
    argv[ (*pNumArgs)++ ] = pdst;
    inquote = FALSE;
    do {
        if (*psrc == W('"') )
        {
            inquote = !inquote;
            c = *psrc++;
            continue;
        }
        *pdst++ = *psrc;

        c = *psrc++;

    } while ( (c != W('\0') && (inquote || (c != W(' ') && c != W('\t')))) );

    if ( c == W('\0') ) {
        psrc--;
    } else {
        *(pdst-1) = W('\0');
    }

    inquote = FALSE;



    /* loop on each argument */
    for(;;)
    {
        if ( *psrc )
        {
            while (*psrc == W(' ') || *psrc == W('\t'))
            {
                ++psrc;
            }
        }

        if (*psrc == W('\0'))
            break;              /* end of args */

        /* scan an argument */
        argv[ (*pNumArgs)++ ] = pdst;

        /* loop through scanning one argument */
        for (;;)
        {
            copychar = 1;
            /* Rules: 2N backslashes + " ==> N backslashes and begin/end quote
               2N+1 backslashes + " ==> N backslashes + literal "
               N backslashes ==> N backslashes */
            numslash = 0;
            while (*psrc == W('\\'))
            {
                /* count number of backslashes for use below */
                ++psrc;
                ++numslash;
            }
            if (*psrc == W('"'))
            {
                /* if 2N backslashes before, start/end quote, otherwise
                   copy literally */
                if (numslash % 2 == 0)
                {
                    if (inquote && psrc[1] == W('"'))
                    {
                        psrc++;    /* Double quote inside quoted string */
                    }
                    else
                    {
                        /* skip first quote char and copy second */
                        copychar = 0;       /* don't copy quote */
                        inquote = !inquote;
                    }
                }
                numslash /= 2;          /* divide numslash by two */
            }
    
            /* copy slashes */
            while (numslash--)
            {
                *pdst++ = W('\\');
            }
    
            /* if at end of arg, break loop */
            if (*psrc == W('\0') || (!inquote && (*psrc == W(' ') || *psrc == W('\t'))))
                break;
    
            /* copy character into argument */
            if (copychar)
            {
                *pdst++ = *psrc;
            }
            ++psrc;
        }

        /* null-terminate the argument */

        *pdst++ = W('\0');          /* terminate string */
    }

    /* We put one last argument in -- a null ptr */
    argv[ (*pNumArgs) ] = NULL;

    // If we hit this assert, we overwrote our destination buffer.
    // Since we're supposed to allocate for the worst
    // case, either the parsing rules have changed or our worse case
    // formula is wrong.
    _ASSERTE((BYTE*)pdst <= (BYTE*)pAlloc + cbAlloc);
    return argv;
}

Volatile<PVOID> ForbidCallsIntoHostOnThisThread::s_pvOwningFiber = NULL;

#ifdef ENABLE_CONTRACTS_IMPL

enum SOViolationType {
    SO_Violation_Intolerant = 0,
    SO_Violation_NotMainline = 1,
    SO_Violation_Backout = 2,
};

struct HashedSOViolations {
    ULONG m_hash;
    HashedSOViolations* m_pNext;
    HashedSOViolations(ULONG hash, HashedSOViolations *pNext) : m_hash(hash), m_pNext(pNext) {}
};

static HashedSOViolations *s_pHashedSOViolations = NULL;

void SOViolation(const char *szFunction, const char *szFile, int lineNum, SOViolationType violation);


//
// SOTolerantViolation is used to report an SO-intolerant function that is not running behind a probe.
//
void SOTolerantViolation(const char *szFunction, const char *szFile, int lineNum) 
{
    return SOViolation(szFunction, szFile, lineNum, SO_Violation_Intolerant);
}

//
// SONotMainlineViolation is used to report any code with SO_NOT_MAINLINE being run in a test environment
// with COMPLUS_NO_SO_NOT_MAINLINE enabled
//
void SONotMainlineViolation(const char *szFunction, const char *szFile, int lineNum) 
{
    return SOViolation(szFunction, szFile, lineNum, SO_Violation_NotMainline);
}

//
// SONotMainlineViolation is used to report any code with SO_NOT_MAINLINE being run in a test environment
// with COMPLUS_NO_SO_NOT_MAINLINE enabled
//
void SOBackoutViolation(const char *szFunction, const char *szFile, int lineNum) 
{
    return SOViolation(szFunction, szFile, lineNum, SO_Violation_Backout);
}

//
// Code common to SO violations
//
// The default is to throw up an ASSERT.  But the function can also dump violations to a file and
// ensure that only unique violations are tracked.
//
void SOViolation(const char *szFunction, const char *szFile, int lineNum, SOViolationType violationType)
{
    // This function is called from places that don't allow a throw.  But this is debug-only 
    // code that should eventually never be called once all the violations are gone.
    CONTRACT_VIOLATION(ThrowsViolation|FaultViolation|TakesLockViolation);

    static BOOL fDumpToFileInitialized = FALSE;
    static BOOL fDumpToFile = FALSE;

#pragma warning(disable:4640)      // Suppress warning: construction of local static object is not thread-safe 
    static SString hashFN;
    static SString fnameFN;
    static SString detailsFN;
#pragma warning(default:4640)

    static int dumpLock = -1;

    static CHAR szExprWithStack[10480];
    static DWORD stackTraceLength = 20;

    if (fDumpToFileInitialized == FALSE)
    {
        stackTraceLength = REGUTIL::GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_SODumpViolationsStackTraceLength, stackTraceLength);  
        // Limit the length or we'll overflow our buffer
        if (stackTraceLength > cfrMaxAssertStackLevels)
        {
            stackTraceLength = cfrMaxAssertStackLevels;
        }
        NewArrayHolder<WCHAR> dumpDir(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_SODumpViolationsDir));  
        if (dumpDir == NULL)
        {
            fDumpToFileInitialized = TRUE;
        }
        else
        {
            fDumpToFile = TRUE;
            hashFN.Append(SString(dumpDir.GetValue()));
            hashFN.Append(W("\\SOViolationHashes.txt"));
            fnameFN.Append(SString(dumpDir.GetValue()));
            fnameFN.Append(W("\\SOViolationFunctionNames.txt"));
            detailsFN.Append(SString(dumpDir.GetValue()));
            detailsFN.Append(W("\\SOViolationDetails.txt"));
        }
    }

    char buff[1024];

    if (violationType == SO_Violation_NotMainline) 
    {
        sprintf_s(buff, 
                _countof(buff),
                        "CONTRACT VIOLATION by %s at \"%s\" @ %d\n\n" 
                        "SO-not-mainline function being called with not-mainline checking enabled.\n"
                        "\nPlease open a bug against the feature owner.\n"
                        "\nNOTE: You can disable this ASSERT by setting COMPLUS_SOEnableDefaultRWValidation=0.\n"
                        "      or by turning of not-mainline checking by by setting COMPLUS_NO_SO_NOT_MAINLINE=0.\n"
                        "\nFor details about this feature, see, in a CLR enlistment,\n"
                        "src\\ndp\\clr\\doc\\OtherDevDocs\\untriaged\\clrdev_web\\SO Guide for CLR Developers.doc\n",
                            szFunction, szFile, lineNum);
    }
    else if (violationType == SO_Violation_Backout) 
    {
        sprintf_s(buff, 
                _countof(buff),
                        "SO Backout Marker overrun.\n\n" 
                        "A dtor or handler path exceeded the backout code stack consumption limit.\n"
                        "\nPlease open a bug against the feature owner.\n"
                        "\nNOTE: You can disable this ASSERT by setting COMPLUS_SOEnableBackoutStackValidation=0.\n"
                        "\nFor details about this feature, see, in a CLR enlistment,\n"
                        "src\\ndp\\clr\\doc\\OtherDevDocs\\untriaged\\clrdev_web\\SO Guide for CLR Developers.doc\n",
                            szFunction, szFile, lineNum);
    }
    else 
    {
        sprintf_s(buff, 
                _countof(buff),
                        "CONTRACT VIOLATION by %s at \"%s\" @ %d\n\n" 
                        "SO-intolerant function called outside an SO probe.\n"
                        "\nPlease open a bug against the feature owner.\n"
                        "\nNOTE: You can disable this ASSERT by setting COMPLUS_SOEnableDefaultRWValidation=0.\n"
                        "\nFor details about this feature, see, in a CLR enlistment,\n"
                        "src\\ndp\\clr\\doc\\OtherDevDocs\\untriaged\\clrdev_web\\SO Guide for CLR Developers.doc\n",
                            szFunction, szFile, lineNum);
    }
                        
    // At this point, we've checked if we should dump to file or not and so can either
    // do the assert or fall through and dump to a file.
    if (! fDumpToFile)
    {
        DbgAssertDialog((char *)szFile, lineNum, buff);
        return;
    }

    // If we are dumping violations to a file, we want to avoid duplicates so that we can run multiple tests
    // and find unique violations and not end up with massively long files.
    // We keep three files: 
    //    1) a list of the hashed strings for each unique filename/function
    //    2) a list of the actual filename/function for unique violations and 
    //    3) a detailed assert dump for the violation itself
    //
    // First thing to do is read in the hashes file if this is our first violation.  We read the filenames into a linked
    // list with their hashes.
    //
    // Then we want to search through the list for that violation

    // If it's new, then we insert the violation at the front of our list and append it to the violation files
    // Otherwise, if we've already seen this violation, we can ignore it.

    
    HANDLE hashesDumpFileHandle  = INVALID_HANDLE_VALUE;

    StackScratchBuffer buffer;
    // First see if we've initialized yet
    if (fDumpToFileInitialized == FALSE)
    {
        LONG lAlreadyOwned = InterlockedExchange((LPLONG)&dumpLock, 1);
        if (lAlreadyOwned == 1)
        {
            // somebody else has gotten here first.  So just skip this violation.
            return;
        }

        // This is our first time through, so read in the existing file and create a linked list of hashed names from it.
        hashesDumpFileHandle = CreateFileA(
                hashFN.GetANSI(buffer), 
                GENERIC_READ,
                FILE_SHARE_READ,
                NULL,
                OPEN_ALWAYS,
                FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN | FILE_FLAG_WRITE_THROUGH,
                NULL);

        // If we successfully opened the file, pull out each hash number and add it to a linked list of known violations.
        // Otherwise, if we couldn't open the file, assume that there were no preexisting violations.  The worse thing
        // that will happen in this case is that we might report some dups.
        if (hashesDumpFileHandle != INVALID_HANDLE_VALUE)
        {
            DWORD dwFileSize = GetFileSize( hashesDumpFileHandle, NULL );

            NewArrayHolder<char> pBuffer(new char[dwFileSize]);
            DWORD cbBuffer = dwFileSize;
            DWORD cbRead;
            DWORD result = ReadFile( hashesDumpFileHandle, pBuffer.GetValue(), cbBuffer, &cbRead, NULL );

            CloseHandle( hashesDumpFileHandle );
            hashesDumpFileHandle = INVALID_HANDLE_VALUE;

            // If we couldn't read the file, assume that there were no preexisting violations.  Worse thing
            // that will happen is we might report some dups.
            if (result && cbRead == cbBuffer)
            {
                char *pBuf = pBuffer.GetValue();
                COUNT_T count = 0;
                LOG((LF_EH, LL_INFO100000, "SOTolerantViolation: Reading known violations\n"));
                while (count < cbRead)
                {
                    char *pHashStart = pBuf + count;
                    char *pHashEnd = strstr(pHashStart, "\r\n");
                    COUNT_T len = static_cast<COUNT_T>(pHashEnd-pHashStart);
                    SString hashString(SString::Ascii, pHashStart, len);
                    ULONG hashValue = wcstoul(hashString.GetUnicode(), NULL, 16);
                    HashedSOViolations *pHashedSOViolations = new HashedSOViolations(hashValue, s_pHashedSOViolations);
                    s_pHashedSOViolations = pHashedSOViolations;
                    count += (len + 2);
                    LOG((LF_ALWAYS, LL_ALWAYS, "    %8.8x\n", pHashedSOViolations->m_hash));
                }
            }
        }
        fDumpToFileInitialized = TRUE;
        dumpLock = -1;
    }


    SString violation;
    violation.Append(SString(SString::Ascii, szFile));
    violation.Append(W(" "));
    violation.Append(SString(SString::Ascii, szFunction));
    HashedSOViolations *cur = s_pHashedSOViolations;

    // look for the violation in the list
    while (cur != NULL)
    {
        if (cur->m_hash == violation.Hash())
        {
            return;
        }
        cur = cur->m_pNext;
    }

    LONG lAlreadyOwned = InterlockedExchange((LPLONG)&dumpLock, 1);
    if (lAlreadyOwned == 1)
    {
        // somebody else has gotten here first.  So just skip this violation. 
        return;
    }

    HANDLE functionsDumpFileHandle = INVALID_HANDLE_VALUE;
    HANDLE detailsDumpFileHandle = INVALID_HANDLE_VALUE;

    // This is a new violation
    // Append new violations to the output files
    functionsDumpFileHandle = CreateFileA(
            fnameFN.GetANSI(buffer), GENERIC_WRITE, FILE_SHARE_READ, NULL, OPEN_ALWAYS,
            FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN | FILE_FLAG_WRITE_THROUGH, NULL);

    if (functionsDumpFileHandle != INVALID_HANDLE_VALUE)
    {
        // First write it to the filename dump
        SetFilePointer(functionsDumpFileHandle, NULL, NULL, FILE_END);

        DWORD written;
        char *szExpr = &szExprWithStack[0];
        sprintf_s(szExpr, _countof(szExprWithStack), "%s %8.8x\r\n", violation.GetANSI(buffer), violation.Hash());
        WriteFile(functionsDumpFileHandle, szExpr, static_cast<DWORD>(strlen(szExpr)), &written, NULL);
        CloseHandle(functionsDumpFileHandle);        

        // Now write it to the hashes dump.  Once we've got it in the filename dump, we don't
        // care if these others fail.  We can live w/o detailed info or with dups.
        hashesDumpFileHandle = CreateFileA(
                hashFN.GetANSI(buffer), GENERIC_WRITE, FILE_SHARE_READ, NULL, OPEN_ALWAYS,
                FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN | FILE_FLAG_WRITE_THROUGH, NULL);

        if (hashesDumpFileHandle != INVALID_HANDLE_VALUE)
        {
            SetFilePointer(hashesDumpFileHandle, NULL, NULL, FILE_END);

            DWORD written;
            sprintf_s(szExpr, _countof(szExprWithStack), "%8.8x", violation.Hash());
            strcat_s(szExpr, _countof(szExprWithStack), "\r\n");
            WriteFile(hashesDumpFileHandle, szExpr, static_cast<DWORD>(strlen(szExpr)), &written, NULL);
            CloseHandle(hashesDumpFileHandle);  
            hashesDumpFileHandle = INVALID_HANDLE_VALUE;
        }

        // Now write it to the details dump
        strcpy_s(szExpr, _countof(szExprWithStack), buff);
        strcat_s(szExpr, _countof(szExprWithStack), "\n\n");
#ifndef FEATURE_PAL        
        GetStringFromStackLevels(1, stackTraceLength, szExprWithStack + strlen(szExprWithStack));
        strcat_s(szExpr, _countof(szExprWithStack), "\n\n");
#endif // FEATURE_PAL        
        char exeName[300];
        GetModuleFileNameA(NULL, exeName, sizeof(exeName)/sizeof(WCHAR));
        strcat_s(szExpr, _countof(szExprWithStack), exeName);
        strcat_s(szExpr, _countof(szExprWithStack), "\n\n\n");

        detailsDumpFileHandle = CreateFileA(
            detailsFN.GetANSI(buffer), GENERIC_WRITE, FILE_SHARE_READ, NULL, OPEN_ALWAYS,
            FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN | FILE_FLAG_WRITE_THROUGH, NULL);

        if (detailsDumpFileHandle != INVALID_HANDLE_VALUE)
        {
            SetFilePointer(detailsDumpFileHandle, NULL, NULL, FILE_END);
            WriteFile(detailsDumpFileHandle, szExpr, static_cast<DWORD>(strlen(szExpr)), &written, NULL);
            CloseHandle(detailsDumpFileHandle);        
            detailsDumpFileHandle = INVALID_HANDLE_VALUE;
        }

        // add the new violation to our list
        HashedSOViolations *pHashedSOViolations = new HashedSOViolations(violation.Hash(), s_pHashedSOViolations);
        s_pHashedSOViolations = pHashedSOViolations;
        LOG((LF_ALWAYS, LL_ALWAYS, "SOTolerantViolation: Adding new violation %8.8x %s\n", pHashedSOViolations->m_hash, violation.GetANSI(buffer)));
        dumpLock = -1;
    }
}

void SoTolerantViolationHelper(const char *szFunction,
                                      const char *szFile,
                                      int   lineNum)
{
    // Keep this function separate to avoid overhead of EH in the normal case where we don't assert
    // Enter SO-tolerant mode for scope of this call so that we don't get contract asserts
    // in anything called downstream of CONTRACT_ASSERT.  If we unwind out of here, our dtor
    // will reset our state to what it was on entry.
    CONTRACT_VIOLATION(SOToleranceViolation);    

    SOTolerantViolation(szFunction, szFile, lineNum);

}

void CloseSOTolerantViolationFile()
{
    // We used to have a file to close.  Now we just cleanup the memory.
    HashedSOViolations *ptr = s_pHashedSOViolations;
    while (ptr != NULL)
    {
        s_pHashedSOViolations = s_pHashedSOViolations->m_pNext;
        delete ptr;
        ptr = s_pHashedSOViolations;
    }
}
#endif //ENABLE_CONTRACTS_IMPL

BOOL FileExists(LPCWSTR filename)
{
    WIN32_FIND_DATA data;        
    HANDLE h = WszFindFirstFile(filename, &data);
    if (h == INVALID_HANDLE_VALUE)
    {
        return FALSE;
    }

    ::FindClose(h);

    return TRUE;                
}

#ifndef FEATURE_CORECLR
// Current users for FileLock are ngen and ngen service

FileLockHolder::FileLockHolder()
{
    _hLock = INVALID_HANDLE_VALUE;
}
    
FileLockHolder::~FileLockHolder()
{
    Release();
}

// the amount of time we want to wait
#define FILE_LOCK_RETRY_TIME 100

void FileLockHolder::Acquire(LPCWSTR lockName, HANDLE hInterrupt, BOOL* pInterrupted)
{
    DWORD dwErr = 0;
    DWORD dwAccessDeniedRetry = 0;
    const DWORD MAX_ACCESS_DENIED_RETRIES = 10;

    if (pInterrupted)
    {
        *pInterrupted = FALSE;
    }

    _ASSERTE(_hLock == INVALID_HANDLE_VALUE);

    for (;;) {
        _hLock = WszCreateFile(lockName, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_FLAG_DELETE_ON_CLOSE, NULL);
        if (_hLock != INVALID_HANDLE_VALUE) {
            return; 
        }

        dwErr = GetLastError();
        // Logically we should only expect ERROR_SHARING_VIOLATION, but Windows can also return
        // ERROR_ACCESS_DENIED for underlying NtStatus DELETE_PENDING.  That happens when another process
        // (gacutil.exe or indexer) have the file opened.  Unfortunately there is no public API that would
        // allow us to detect this NtStatus and distinguish it from 'real' access denied (candidates are
        // RtlGetLastNtStatus that is not documented on MSDN and NtCreateFile that is internal and can change
        // at any time), so we retry on access denied, but only for a limited number of times.
        if (dwErr == ERROR_SHARING_VIOLATION ||
            (dwErr == ERROR_ACCESS_DENIED && ++dwAccessDeniedRetry <= MAX_ACCESS_DENIED_RETRIES))
        {
            // Somebody is holding the lock. Let's sleep, and come back again.
            if (hInterrupt)
            {
                _ASSERTE(pInterrupted && 
                    "If you can be interrupted, you better want to know if you actually were interrupted");
                if (WaitForSingleObject(hInterrupt, FILE_LOCK_RETRY_TIME) == WAIT_OBJECT_0)
                {
                      if (pInterrupted)
                      {
                        *pInterrupted = TRUE;
                      }

                      // We've been interrupted, so return without acquiring
                      return;
                }
            }
            else
            {
                ClrSleepEx(FILE_LOCK_RETRY_TIME, FALSE);
            }
        }
        else {
            ThrowHR(HRESULT_FROM_WIN32(dwErr));
        }
    }
}


HRESULT FileLockHolder::AcquireNoThrow(LPCWSTR lockName, HANDLE hInterrupt, BOOL* pInterrupted)
{
    HRESULT hr = S_OK;
    
    EX_TRY
    {
        Acquire(lockName, hInterrupt, pInterrupted);
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

BOOL FileLockHolder::IsTaken(LPCWSTR lockName)
{
    
    // We don't want to do an acquire the lock to know if its taken, so we want to see if the file
    // exists. However, in situations like unplugging a machine, a DELETE_ON_CLOSE still leaves the file
    // around. We try to delete it here. If the lock is acquired, DeleteFile will fail, as the file is
    // not opened with SHARE_DELETE.
    WszDeleteFile(lockName);

    return FileExists(lockName);
}

void FileLockHolder::Release()
{
    if (_hLock != INVALID_HANDLE_VALUE) {
        CloseHandle(_hLock);
        _hLock = INVALID_HANDLE_VALUE;
    }
}
#endif // FEATURE_CORECLR

//======================================================================
// This function returns true, if it can determine that the instruction pointer
// refers to a code address that belongs in the range of the given image.
// <TODO>@TODO: Merge with IsIPInModule from vm\util.hpp</TODO>

BOOL IsIPInModule(HMODULE_TGT hModule, PCODE ip)
{
    STATIC_CONTRACT_LEAF;
    SUPPORTS_DAC;

    struct Param
    {
        HMODULE_TGT hModule;
        PCODE ip;
        BOOL fRet;
    } param;
    param.hModule = hModule;
    param.ip = ip;
    param.fRet = FALSE;

// UNIXTODO: implement a proper version for PAL
#ifndef FEATURE_PAL   
    PAL_TRY(Param *, pParam, &param)
    {
        PTR_BYTE pBase = dac_cast<PTR_BYTE>(pParam->hModule);

        PTR_IMAGE_DOS_HEADER pDOS = NULL;
        PTR_IMAGE_NT_HEADERS pNT  = NULL;
        USHORT cbOptHdr;
        PCODE baseAddr;
        
        //
        // First, must validate the format of the PE headers to make sure that
        // the fields we're interested in using exist in the image.
        //

        // Validate the DOS header.
        pDOS = PTR_IMAGE_DOS_HEADER(pBase);
        if (pDOS->e_magic != VAL16(IMAGE_DOS_SIGNATURE) ||
            pDOS->e_lfanew == 0)
        {
            goto lDone;
        }

        // Validate the NT header
        pNT = PTR_IMAGE_NT_HEADERS(pBase + VAL32(pDOS->e_lfanew));

        if (pNT->Signature != VAL32(IMAGE_NT_SIGNATURE))
        {
            goto lDone;
        }

        // Validate that the optional header is large enough to contain the fields
        // we're interested, namely IMAGE_OPTIONAL_HEADER::SizeOfImage. The reason
        // we don't just check that SizeOfOptionalHeader == IMAGE_SIZEOF_NT_OPTIONAL_HEADER
        // is due to VSW443590, which states that the extensibility of this structure
        // is such that it is possible to include only a portion of the optional header.
        cbOptHdr = pNT->FileHeader.SizeOfOptionalHeader;

        // Check that the magic field is contained by the optional header and set to the correct value.
        if (cbOptHdr < (offsetof(IMAGE_OPTIONAL_HEADER, Magic) + sizeofmember(IMAGE_OPTIONAL_HEADER, Magic)) ||
            pNT->OptionalHeader.Magic != VAL16(IMAGE_NT_OPTIONAL_HDR_MAGIC))
        {
            goto lDone;
        }

        // Check that the SizeOfImage is contained by the optional header.
        if (cbOptHdr < (offsetof(IMAGE_OPTIONAL_HEADER, SizeOfImage) + sizeofmember(IMAGE_OPTIONAL_HEADER, SizeOfImage)))
        {
            goto lDone;
        }

        //
        // The real check
        //

        baseAddr = dac_cast<PCODE>(pBase);
        if ((pParam->ip < baseAddr) || (pParam->ip >= (baseAddr + VAL32(pNT->OptionalHeader.SizeOfImage))))
        {
            goto lDone;
        }

        pParam->fRet = TRUE;

lDone: ;
    }
    PAL_EXCEPT (EXCEPTION_EXECUTE_HANDLER)
    {
    }
    PAL_ENDTRY
#endif // !FEATURE_PAL    

    return param.fRet;
}

#ifdef FEATURE_CORRUPTING_EXCEPTIONS

// To include definition of EXCEPTION_SOFTSO
#include "corexcep.h"

// These functions provide limited support for corrupting exceptions
// outside the VM folder. Its limited since we don't have access to the
// throwable.
//
// These functions are also wrapped by the corresponding CEHelper 
// methods in excep.cpp.

// Given an exception code, this method returns a BOOL to indicate if the
// code belongs to a corrupting exception or not.
BOOL IsProcessCorruptedStateException(DWORD dwExceptionCode, BOOL fCheckForSO /*=TRUE*/)
{
    LIMITED_METHOD_CONTRACT;

    // By default, assume its not corrupting
    BOOL fIsCorruptedStateException = FALSE;

    if (CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_legacyCorruptedStateExceptionsPolicy) == 1)
    {
        return fIsCorruptedStateException;
    }

    // If we have been asked not to include SO in the CSE check
    // and the code represent SO, then exit now.
    if ((fCheckForSO == FALSE) && (dwExceptionCode == STATUS_STACK_OVERFLOW))
    {
        return fIsCorruptedStateException;
    }

    switch(dwExceptionCode)
    {
        case STATUS_ACCESS_VIOLATION:
        case STATUS_STACK_OVERFLOW:
        case EXCEPTION_ILLEGAL_INSTRUCTION:
        case EXCEPTION_IN_PAGE_ERROR:
        case EXCEPTION_INVALID_DISPOSITION:
        case EXCEPTION_NONCONTINUABLE_EXCEPTION:
        case EXCEPTION_PRIV_INSTRUCTION:
        case STATUS_UNWIND_CONSOLIDATE:
            fIsCorruptedStateException = TRUE;
            break;
    }

    return fIsCorruptedStateException;
}

#endif // FEATURE_CORRUPTING_EXCEPTIONS

void EnableTerminationOnHeapCorruption()
{
    HeapSetInformation(NULL, HeapEnableTerminationOnCorruption, NULL, 0);
}

RUNTIMEVERSIONINFO RUNTIMEVERSIONINFO::notDefined;

BOOL IsV2RuntimeLoaded(void)
{
#ifndef FEATURE_CORECLR
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    ReleaseHolder<ICLRMetaHost>    pMetaHost(NULL);
    ReleaseHolder<IEnumUnknown>    pEnum(NULL);
    ReleaseHolder<IUnknown>        pUnk(NULL);
    ReleaseHolder<ICLRRuntimeInfo> pRuntime(NULL);
    HRESULT hr;

    HModuleHolder hModule = WszLoadLibrary(MSCOREE_SHIM_W);
    if (hModule == NULL)
        return FALSE;

    CLRCreateInstanceFnPtr pfnCLRCreateInstance = (CLRCreateInstanceFnPtr)::GetProcAddress(hModule, "CLRCreateInstance");
    if (pfnCLRCreateInstance == NULL)
        return FALSE;

    hr = (*pfnCLRCreateInstance)(CLSID_CLRMetaHost, IID_ICLRMetaHost, (LPVOID *)&pMetaHost);
    if (FAILED(hr))
        return FALSE;

    hr = pMetaHost->EnumerateLoadedRuntimes(GetCurrentProcess(), &pEnum);
    if (FAILED(hr))
        return FALSE;

    while (pEnum->Next(1, &pUnk, NULL) == S_OK)
    {
        hr = pUnk->QueryInterface(IID_ICLRRuntimeInfo, (void **)&pRuntime);
        if (FAILED(hr))
            continue;

        WCHAR wszVersion[30];
        DWORD cchVersion = _countof(wszVersion);
        hr = pRuntime->GetVersionString(wszVersion, &cchVersion);
        if (FAILED(hr))
            continue;

        // Is it a V2 runtime?
        if ((cchVersion < 3) || 
            ((wszVersion[0] != W('v')) && (wszVersion[0] != W('V'))) || 
            (wszVersion[1] != W('2')) || 
            (wszVersion[2] != W('.')))
            continue;

        return TRUE;
    }
#endif //  FEATURE_CORECLR

    return FALSE;
}

#ifdef FEATURE_COMINTEROP
BOOL IsClrHostedLegacyComObject(REFCLSID rclsid)
{
    // let's simply check for all CLSIDs that are known to be runtime implemented and capped to 2.0
    return (
            rclsid == CLSID_ComCallUnmarshal ||
#ifdef FEATURE_INCLUDE_ALL_INTERFACES
            rclsid == CLSID_CorRuntimeHost ||
            rclsid == CLSID_CLRRuntimeHost ||
            rclsid == CLSID_CLRProfiling ||
#endif
            rclsid == CLSID_CorMetaDataDispenser ||
            rclsid == CLSID_CorMetaDataDispenserRuntime ||
            rclsid == CLSID_TypeNameFactory);
}
#endif // FEATURE_COMINTEROP

// Returns the directory for HMODULE. So, if HMODULE was for "C:\Dir1\Dir2\Filename.DLL",
// then this would return "C:\Dir1\Dir2\" (note the trailing backslash).
HRESULT GetHModuleDirectory(
    __in                          HMODULE   hMod,
    __out_z __out_ecount(cchPath) LPWSTR    wszPath,
                                  size_t    cchPath)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    DWORD dwRet = WszGetModuleFileName(hMod, wszPath, static_cast<DWORD>(cchPath));

    if (dwRet == cchPath)
    {   // If there are cchPath characters in the string, it means that the string
        // itself is longer than cchPath and GetModuleFileName had to truncate at cchPath.
        return HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
    }
    else if (dwRet == 0)
    {   // Some other error.
        return HRESULT_FROM_GetLastError();
    }

    LPWSTR wszEnd = wcsrchr(wszPath, W('\\'));
    if (wszEnd == NULL)
    {   // There was no backslash? Not sure what's going on.
        return E_UNEXPECTED;
    }

    // Include the backslash in the resulting string.
    *(++wszEnd) = W('\0');

    return S_OK;
}

SString & GetHModuleDirectory(HMODULE hMod, SString &ssDir)
{
    LPWSTR wzDir = ssDir.OpenUnicodeBuffer(_MAX_PATH);
    HRESULT hr = GetHModuleDirectory(hMod, wzDir, _MAX_PATH);
    ssDir.CloseBuffer(FAILED(hr) ? 0 : static_cast<COUNT_T>(wcslen(wzDir)));
    IfFailThrow(hr);
    return ssDir;
}

#if !defined(FEATURE_CORECLR) && !defined(SELF_NO_HOST) && !defined(FEATURE_UTILCODE_NO_DEPENDENCIES)

namespace UtilCode
{

#pragma warning(push)
#pragma warning(disable:4996) // For use of deprecated LoadLibraryShim

    // When a NULL version is passed to LoadLibraryShim, this told the shim to bind the already-loaded
    // runtime or to the latest runtime. In hosted environments, we already know a runtime (or two) is
    // loaded, and since we are no longer guaranteed that a call to mscoree!LoadLibraryShim with a NULL
    // version will return the correct runtime, this code uses the ClrCallbacks infrastructure
    // available to get the ICLRRuntimeInfo for the runtime in which this code is hosted, and then
    // calls ICLRRuntimeInfo::LoadLibrary to make sure that the load occurs within the context of the
    // correct runtime.
    HRESULT LoadLibraryShim(LPCWSTR szDllName, LPCWSTR szVersion, LPVOID pvReserved, HMODULE *phModDll)
    {
        HRESULT hr = S_OK;

        if (szVersion != NULL)
        {   // If a version is provided, then we just fall back to the legacy function to allow
            // it to construct the explicit path and load from that location.
            //@TODO: Can we verify that all callers of LoadLibraryShim in hosted environments always pass null and eliminate this code?
            return ::LoadLibraryShim(szDllName, szVersion, pvReserved, phModDll);
        }

        //
        // szVersion is NULL, which means we should load the DLL from the hosted environment's directory.
        //

        typedef ICLRRuntimeInfo *GetCLRRuntime_t();
        GetCLRRuntime_t *pfnGetCLRRuntime =
            reinterpret_cast<GetCLRRuntime_t *>((*GetClrCallbacks().m_pfnGetCLRFunction)("GetCLRRuntime"));
        if (pfnGetCLRRuntime == NULL)
            return E_UNEXPECTED;

        ICLRRuntimeInfo* pRI = (*pfnGetCLRRuntime)();
        if (pRI == NULL)
            return E_UNEXPECTED;

        return pRI->LoadLibrary(szDllName, phModDll);
    }

#pragma warning(pop)

}

#endif //!FEATURE_CORECLR && !SELF_NO_HOST && !FEATURE_UTILCODE_NO_DEPENDENCIES

namespace Clr
{
namespace Util
{
    static BOOL g_fLocalAppDataDirectoryInitted = FALSE;
    static WCHAR *g_wszLocalAppDataDirectory = NULL;

// This api returns a pointer to a null-terminated string that contains the local appdata directory
// or it returns NULL in the case that the directory could not be found. The return value from this function
// is not actually checked for existence. 
    HRESULT GetLocalAppDataDirectory(LPCWSTR *ppwzLocalAppDataDirectory)
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
        } CONTRACTL_END;

        HRESULT hr = S_OK;
        *ppwzLocalAppDataDirectory = NULL;

        EX_TRY
        {
            if (!g_fLocalAppDataDirectoryInitted)
            {
                WCHAR *wszLocalAppData = NULL;

                DWORD cCharsNeeded;
                cCharsNeeded = GetEnvironmentVariableW(W("LOCALAPPDATA"), NULL, 0);

                if ((cCharsNeeded != 0) && (cCharsNeeded < MAX_PATH))
                {
                    wszLocalAppData = new WCHAR[cCharsNeeded];
                    cCharsNeeded = GetEnvironmentVariableW(W("LOCALAPPDATA"), wszLocalAppData, cCharsNeeded);
                    if (cCharsNeeded != 0)
                    {
                        // We've collected the appropriate app data directory into a local. Now publish it.
                        if (InterlockedCompareExchangeT(&g_wszLocalAppDataDirectory, wszLocalAppData, NULL) == NULL)
                        {
                            // This variable doesn't need to be freed, as it has been stored in the global
                            wszLocalAppData = NULL;
                        }
                    }
                }

                g_fLocalAppDataDirectoryInitted = TRUE;
                delete[] wszLocalAppData;
            }
        }
        EX_CATCH_HRESULT(hr);

        if (SUCCEEDED(hr))
            *ppwzLocalAppDataDirectory = g_wszLocalAppDataDirectory;

        return hr;
    }

    HRESULT SetLocalAppDataDirectory(LPCWSTR pwzLocalAppDataDirectory)
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
        } CONTRACTL_END;

        if (pwzLocalAppDataDirectory == NULL || *pwzLocalAppDataDirectory == W('\0'))
            return E_INVALIDARG;

        if (g_fLocalAppDataDirectoryInitted)
            return E_UNEXPECTED;

        HRESULT hr = S_OK;

        EX_TRY
        {
            size_t size = wcslen(pwzLocalAppDataDirectory) + 1;
            WCHAR *wszLocalAppData = new WCHAR[size];
            wcscpy_s(wszLocalAppData, size, pwzLocalAppDataDirectory);

            // We've collected the appropriate app data directory into a local. Now publish it.
            if (InterlockedCompareExchangeT(&g_wszLocalAppDataDirectory, wszLocalAppData, NULL) != NULL)
            {
                // Someone else already set LocalAppData. Free our copy and return an error.
                delete[] wszLocalAppData;
                hr = E_UNEXPECTED;
            }

            g_fLocalAppDataDirectoryInitted = TRUE;
        }
        EX_CATCH_HRESULT(hr);

        return hr;
    }

#ifndef FEATURE_PAL
namespace Reg
{
    HRESULT ReadStringValue(HKEY hKey, LPCWSTR wszSubKeyName, LPCWSTR wszValueName, SString & ssValue)
    {
        STANDARD_VM_CONTRACT;

        if (hKey == NULL)
        {
            return E_INVALIDARG;
        }

        RegKeyHolder hTargetKey;
        if (wszSubKeyName == NULL || *wszSubKeyName == W('\0'))
        {   // No subkey was requested, use hKey as the resolved key.
            hTargetKey = hKey;
            hTargetKey.SuppressRelease();
        }
        else
        {   // Try to open the specified subkey.
            if (WszRegOpenKeyEx(hKey, wszSubKeyName, 0, KEY_READ, &hTargetKey) != ERROR_SUCCESS)
                return REGDB_E_CLASSNOTREG;
        }

        DWORD type;
        DWORD size;
        if ((WszRegQueryValueEx(hTargetKey, wszValueName, 0, &type, 0, &size) == ERROR_SUCCESS) &&
            type == REG_SZ && size > 0)
        {
            LPWSTR wszValueBuf = ssValue.OpenUnicodeBuffer(static_cast<COUNT_T>((size / sizeof(WCHAR)) - 1));
            LONG lResult = WszRegQueryValueEx(
                hTargetKey,
                wszValueName,
                0,
                0,
                reinterpret_cast<LPBYTE>(wszValueBuf),
                &size);

            _ASSERTE(lResult == ERROR_SUCCESS);
            if (lResult == ERROR_SUCCESS)
            {
                // Can't count on the returned size being accurate - I've seen at least
                // one string with an extra NULL at the end that will cause the resulting
                // SString to count the extra NULL as part of the string. An extra
                // terminating NULL is not a legitimate scenario for REG_SZ - this must
                // be done using REG_MULTI_SZ - however this was tolerated in the
                // past and so it would be a breaking change to stop doing so.
                _ASSERTE(wcslen(wszValueBuf) <= (size / sizeof(WCHAR)) - 1);
                ssValue.CloseBuffer((COUNT_T)wcsnlen(wszValueBuf, (size_t)size));
            }
            else
            {
                ssValue.CloseBuffer(0);
                return HRESULT_FROM_WIN32(lResult);
            }

            return S_OK;
        }
        else
        {
            return REGDB_E_KEYMISSING;
        }
    }

    HRESULT ReadStringValue(HKEY hKey, LPCWSTR wszSubKey, LPCWSTR wszName, __deref_out __deref_out_z LPWSTR* pwszValue)
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
        } CONTRACTL_END;

        HRESULT hr = S_OK;
        EX_TRY
        {
            StackSString ssValue;
            if (SUCCEEDED(hr = ReadStringValue(hKey, wszSubKey, wszName, ssValue)))
            {
                *pwszValue = new WCHAR[ssValue.GetCount() + 1];
                wcscpy_s(*pwszValue, ssValue.GetCount() + 1, ssValue.GetUnicode());
            }
        }
        EX_CATCH_HRESULT(hr);
        return hr;
    }
} // namespace Reg

namespace Com
{
    namespace __imp
    {
        static
        HRESULT FindSubKeyDefaultValueForCLSID(REFCLSID rclsid, LPCWSTR wszSubKeyName, SString & ssValue)
        {
            STANDARD_VM_CONTRACT;

            HRESULT hr = S_OK;

            WCHAR wszClsid[39];
            if (GuidToLPWSTR(rclsid, wszClsid, NumItems(wszClsid)) == 0)
                return E_UNEXPECTED;

            StackSString ssKeyName;
            ssKeyName.Append(SL(W("CLSID\\")));
            ssKeyName.Append(wszClsid);
            ssKeyName.Append(SL(W("\\")));
            ssKeyName.Append(wszSubKeyName);

            return Clr::Util::Reg::ReadStringValue(HKEY_CLASSES_ROOT, ssKeyName.GetUnicode(), NULL, ssValue);
        }

        static
        HRESULT FindSubKeyDefaultValueForCLSID(REFCLSID rclsid, LPCWSTR wszSubKeyName, __deref_out __deref_out_z LPWSTR* pwszValue)
        {
            CONTRACTL {
                NOTHROW;
                GC_NOTRIGGER;
            } CONTRACTL_END;

            HRESULT hr = S_OK;
            EX_TRY
            {
                StackSString ssValue;
                if (SUCCEEDED(hr = FindSubKeyDefaultValueForCLSID(rclsid, wszSubKeyName, ssValue)))
                {
                    *pwszValue = new WCHAR[ssValue.GetCount() + 1];
                    wcscpy_s(*pwszValue, ssValue.GetCount() + 1, ssValue.GetUnicode());
                }
            }
            EX_CATCH_HRESULT(hr);
            return hr;
        }
    }

    HRESULT FindServerUsingCLSID(REFCLSID rclsid, __deref_out __deref_out_z LPWSTR* pwszServerName)
    {
        WRAPPER_NO_CONTRACT;
        return __imp::FindSubKeyDefaultValueForCLSID(rclsid, W("Server"), pwszServerName);
    }

    HRESULT FindServerUsingCLSID(REFCLSID rclsid, SString & ssServerName)
    {
        WRAPPER_NO_CONTRACT;
        return __imp::FindSubKeyDefaultValueForCLSID(rclsid, W("Server"), ssServerName);
    }

    HRESULT FindInprocServer32UsingCLSID(REFCLSID rclsid, __deref_out __deref_out_z LPWSTR* pwszInprocServer32Name)
    {
        WRAPPER_NO_CONTRACT;
        return __imp::FindSubKeyDefaultValueForCLSID(rclsid, W("InprocServer32"), pwszInprocServer32Name);
    }

    HRESULT FindInprocServer32UsingCLSID(REFCLSID rclsid, SString & ssInprocServer32Name)
    {
        WRAPPER_NO_CONTRACT;
        return __imp::FindSubKeyDefaultValueForCLSID(rclsid, W("InprocServer32"), ssInprocServer32Name);
    }

    BOOL IsMscoreeInprocServer32(const SString & ssInprocServer32Name)
    {
        WRAPPER_NO_CONTRACT;

        return (ssInprocServer32Name.EqualsCaseInsensitive(SL(MSCOREE_SHIM_W)) ||
                ssInprocServer32Name.EndsWithCaseInsensitive(SL(W("\\") MSCOREE_SHIM_W)));
    }

    BOOL CLSIDHasMscoreeAsInprocServer32(REFCLSID rclsid)
    {
        WRAPPER_NO_CONTRACT;

        StackSString ssInprocServer32;
        FindInprocServer32UsingCLSID(rclsid, ssInprocServer32);
        return IsMscoreeInprocServer32(ssInprocServer32);
    }

} // namespace Com
#endif //  FEATURE_PAL

namespace Win32
{
    void GetModuleFileName(
        HMODULE hModule,
        SString & ssFileName,
        bool fAllowLongFileNames)
    {
        STANDARD_VM_CONTRACT;

        // Try to use what the SString already has allocated. If it does not have anything allocated
        // or it has < 20 characters allocated, then bump the size requested to _MAX_PATH.
        DWORD dwSize = (DWORD)(ssFileName.GetUnicodeAllocation()) + 1;
        dwSize = (dwSize < 20) ? (_MAX_PATH) : (dwSize);
        DWORD dwResult = WszGetModuleFileName(hModule, ssFileName.OpenUnicodeBuffer(dwSize - 1), dwSize);

        // if there was a failure, dwResult == 0;
        // if there was insufficient buffer, dwResult == dwSize;
        // if there was sufficient buffer and a successful write, dwResult < dwSize
        ssFileName.CloseBuffer(dwResult < dwSize ? dwResult : 0);

        if (dwResult == 0)
            ThrowHR(HRESULT_FROM_GetLastError());

        // Ok, we didn't have enough buffer. Let's loop, doubling the buffer each time, until we succeed.
        while (dwResult == dwSize)
        {
            dwSize = dwSize * 2;
            dwResult = WszGetModuleFileName(hModule, ssFileName.OpenUnicodeBuffer(dwSize - 1), dwSize);
            ssFileName.CloseBuffer(dwResult < dwSize ? dwResult : 0);

            if (dwResult == 0)
                ThrowHR(HRESULT_FROM_GetLastError());
        }

        // Most of the runtime is not able to handle long filenames. fAllowLongFileNames
        // has a default value of false, so that callers will not accidentally get long
        // file names returned.
        if (!fAllowLongFileNames && ssFileName.BeginsWith(SL(LONG_FILENAME_PREFIX_W)))
        {
            ssFileName.Clear();
            ThrowHR(E_UNEXPECTED);
        }

        _ASSERTE(dwResult != 0 && dwResult < dwSize);
    }

    // Returns heap-allocated string in *pwszFileName
    HRESULT GetModuleFileName(
        HMODULE hModule,
        __deref_out_z LPWSTR * pwszFileName,
        bool fAllowLongFileNames)
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            PRECONDITION(CheckPointer(pwszFileName));
        } CONTRACTL_END;

        HRESULT hr = S_OK;
        EX_TRY
        {
            InlineSString<_MAX_PATH> ssFileName;
            GetModuleFileName(hModule, ssFileName);
            *pwszFileName = DuplicateStringThrowing(ssFileName.GetUnicode());
        }
        EX_CATCH_HRESULT(hr);

        return hr;
    }

    void GetFullPathName(
        SString const & ssFileName,
        SString & ssPathName,
        DWORD * pdwFilePartIdx,
        bool fAllowLongFileNames)
    {
        STANDARD_VM_CONTRACT;

        // Get the required buffer length (including terminating NULL).
        DWORD dwLengthRequired = WszGetFullPathName(ssFileName.GetUnicode(), 0, NULL, NULL);

        if (dwLengthRequired == 0)
            ThrowHR(HRESULT_FROM_GetLastError());

        LPWSTR wszPathName = ssPathName.OpenUnicodeBuffer(dwLengthRequired - 1);
        LPWSTR wszFileName = NULL;
        DWORD dwLengthWritten = WszGetFullPathName(
            ssFileName.GetUnicode(),
            dwLengthRequired,
            wszPathName,
            &wszFileName);

        // Calculate the index while the buffer is open and the string pointer is stable.
        if (dwLengthWritten != 0 && dwLengthWritten < dwLengthRequired && pdwFilePartIdx != NULL)
            *pdwFilePartIdx = static_cast<DWORD>(wszFileName - wszPathName);

        ssPathName.CloseBuffer(dwLengthWritten < dwLengthRequired ? dwLengthWritten : 0);

        if (dwLengthRequired == 0)
            ThrowHR(HRESULT_FROM_GetLastError());

        // Overly defensive? Perhaps.
        if (!(dwLengthWritten < dwLengthRequired))
            ThrowHR(E_UNEXPECTED);

        // Most of the runtime is not able to handle long filenames. fAllowLongFileNames
        // has a default value of false, so that callers will not accidentally get long
        // file names returned.
        if (!fAllowLongFileNames && ssFileName.BeginsWith(SL(LONG_FILENAME_PREFIX_W)))
        {
            ssPathName.Clear();
            if (pdwFilePartIdx != NULL)
                *pdwFilePartIdx = 0;
            ThrowHR(E_UNEXPECTED);
        }
    }
} // namespace Win32

} // namespace Util
} // namespace Clr
