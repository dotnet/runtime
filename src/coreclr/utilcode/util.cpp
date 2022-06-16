// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
#include "corinfo.h"
#include "volatile.h"
#include "mdfileformat.h"
#include <configuration.h>

#ifndef DACCESS_COMPILE
UINT32 g_nClrInstanceId = 0;
#endif //!DACCESS_COMPILE

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

namespace
{
    HRESULT FakeCoCallDllGetClassObject(
        REFCLSID rclsid,
        LPCWSTR wszDllPath,
        REFIID riid,
        void **ppv,
        HMODULE *phmodDll)
    {
        CONTRACTL
        {
            THROWS;
        }
        CONTRACTL_END;

        _ASSERTE(ppv != nullptr);

        HRESULT hr = S_OK;

        // Initialize [out] HMODULE (if it was requested)
        if (phmodDll != nullptr)
            *phmodDll = nullptr;

        bool fIsDllPathPrefix = (wszDllPath != nullptr) && (wcslen(wszDllPath) > 0) && (wszDllPath[wcslen(wszDllPath) - 1] == W('\\'));

        // - An empty string will be treated as NULL.
        // - A string ending will a backslash will be treated as a prefix for where to look for the DLL
        //   if the InProcServer32 value is just a DLL name and not a full path.
        StackSString ssDllName;
        if ((wszDllPath == nullptr) || (wszDllPath[0] == W('\0')) || fIsDllPathPrefix)
        {
#ifdef HOST_WINDOWS
            IfFailRet(Clr::Util::Com::FindInprocServer32UsingCLSID(rclsid, ssDllName));

            EX_TRY
            {
                if (fIsDllPathPrefix)
                {
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
#else // HOST_WINDOWS
            return E_FAIL;
#endif // HOST_WINDOWS
        }
        _ASSERTE(wszDllPath != nullptr);

        // We've got the name of the DLL to load, so load it.
        HModuleHolder hDll = WszLoadLibraryEx(wszDllPath, nullptr, GetLoadWithAlteredSearchPathFlag());
        if (hDll == nullptr)
            return HRESULT_FROM_GetLastError();

        // We've loaded the DLL, so find the DllGetClassObject function.
        DLLGETCLASSOBJECT *dllGetClassObject = (DLLGETCLASSOBJECT*)GetProcAddress(hDll, "DllGetClassObject");
        if (dllGetClassObject == nullptr)
            return HRESULT_FROM_GetLastError();

        // Call the function to get a class object for the rclsid and riid passed in.
        IfFailRet(dllGetClassObject(rclsid, riid, ppv));

        hDll.SuppressRelease();

        if (phmodDll != nullptr)
            *phmodDll = hDll.GetValue();

        return hr;
    }
}

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
//        be found.
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
    HModuleHolder hDll;
    ReleaseHolder<IClassFactory> classFactory;
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

//
// Allocate free memory with specific alignment.
//
LPVOID ClrVirtualAllocAligned(LPVOID lpAddress, SIZE_T dwSize, DWORD flAllocationType, DWORD flProtect, SIZE_T alignment)
{
    // Verify that the alignment is a power of 2
    _ASSERTE(alignment != 0);
    _ASSERTE((alignment & (alignment - 1)) == 0);

#ifdef HOST_WINDOWS

    // The VirtualAlloc on Windows ensures 64kB alignment
    _ASSERTE(alignment <= 0x10000);
    return ClrVirtualAlloc(lpAddress, dwSize, flAllocationType, flProtect);

#else // HOST_WINDOWS

    if(alignment < GetOsPageSize()) alignment = GetOsPageSize();

    // UNIXTODO: Add a specialized function to PAL so that we don't have to waste memory
    dwSize += alignment;
    SIZE_T addr = (SIZE_T)ClrVirtualAlloc(lpAddress, dwSize, flAllocationType, flProtect);
    return (LPVOID)((addr + (alignment - 1)) & ~(alignment - 1));

#endif // HOST_WINDOWS
}

#ifdef _DEBUG
static DWORD ShouldInjectFaultInRange()
{
    static DWORD fInjectFaultInRange = 99;

    if (fInjectFaultInRange == 99)
        fInjectFaultInRange = (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_InjectFault) & 0x40);
    return fInjectFaultInRange;
}
#endif

// Reserves free memory within the range [pMinAddr..pMaxAddr] using
// ClrVirtualQuery to find free memory and ClrVirtualAlloc to reserve it.
//
// This method only supports the flAllocationType of MEM_RESERVE, and expects that the memory
// is being reserved for the purpose of eventually storing executable code.
//
// Callers also should set dwSize to a multiple of sysInfo.dwAllocationGranularity (64k).
// That way they can reserve a large region and commit smaller sized pages
// from that region until it fills up.
//
// This functions returns the reserved memory block upon success
//
// It returns NULL when it fails to find any memory that satisfies
// the range.
//

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

    BYTE *pResult = nullptr;  // our return value;

    static unsigned countOfCalls = 0;  // We log the number of tims we call this method
    countOfCalls++;                    // increment the call counter

    if (dwSize == 0)
    {
        return nullptr;
    }

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

    // If pMaxAddr is not greater than pMinAddr we can not make an allocation
    if (pMaxAddr <= pMinAddr)
    {
        return nullptr;
    }

    // If pMinAddr is BOT_MEMORY and pMaxAddr is TOP_MEMORY
    // then we can call ClrVirtualAlloc instead
    if ((pMinAddr == (BYTE *) BOT_MEMORY) && (pMaxAddr == (BYTE *) TOP_MEMORY))
    {
        return (BYTE*) ClrVirtualAlloc(nullptr, dwSize, flAllocationType, flProtect);
    }

#ifdef HOST_UNIX
    pResult = (BYTE *)PAL_VirtualReserveFromExecutableMemoryAllocatorWithinRange(pMinAddr, pMaxAddr, dwSize, TRUE /* fStoreAllocationInfo */);
    if (pResult != nullptr)
    {
        return pResult;
    }
#endif // HOST_UNIX

    // We will do one scan from [pMinAddr .. pMaxAddr]
    // First align the tryAddr up to next 64k base address.
    // See docs for VirtualAllocEx and lpAddress and 64k alignment for reasons.
    //
    BYTE *   tryAddr            = (BYTE *)ALIGN_UP((BYTE *)pMinAddr, VIRTUAL_ALLOC_RESERVE_GRANULARITY);
    bool     virtualQueryFailed = false;
    bool     faultInjected      = false;
    unsigned virtualQueryCount  = 0;

    // Now scan memory and try to find a free block of the size requested.
    while ((tryAddr + dwSize) <= (BYTE *) pMaxAddr)
    {
        MEMORY_BASIC_INFORMATION mbInfo;

        // Use VirtualQuery to find out if this address is MEM_FREE
        //
        virtualQueryCount++;
        if (!ClrVirtualQuery((LPCVOID)tryAddr, &mbInfo, sizeof(mbInfo)))
        {
            // Exit and return nullptr if the VirtualQuery call fails.
            virtualQueryFailed = true;
            break;
        }

        // Is there enough memory free from this start location?
        // Note that for most versions of UNIX the mbInfo.RegionSize returned will always be 0
        if ((mbInfo.State == MEM_FREE) &&
            (mbInfo.RegionSize >= (SIZE_T) dwSize || mbInfo.RegionSize == 0))
        {
            // Try reserving the memory using VirtualAlloc now
            pResult = (BYTE*)ClrVirtualAlloc(tryAddr, dwSize, MEM_RESERVE, flProtect);

            // Normally this will be successful
            //
            if (pResult != nullptr)
            {
                // return pResult
                break;
            }

#ifdef _DEBUG
            if (ShouldInjectFaultInRange())
            {
                // return nullptr (failure)
                faultInjected = true;
                break;
            }
#endif // _DEBUG

            // On UNIX we can also fail if our request size 'dwSize' is larger than 64K and
            // and our tryAddr is pointing at a small MEM_FREE region (smaller than 'dwSize')
            // However we can't distinguish between this and the race case.

            // We might fail in a race.  So just move on to next region and continue trying
            tryAddr = tryAddr + VIRTUAL_ALLOC_RESERVE_GRANULARITY;
        }
        else
        {
            // Try another section of memory
            tryAddr = max(tryAddr + VIRTUAL_ALLOC_RESERVE_GRANULARITY,
                          (BYTE*) mbInfo.BaseAddress + mbInfo.RegionSize);
        }
    }

    STRESS_LOG7(LF_JIT, LL_INFO100,
                "ClrVirtualAllocWithinRange request #%u for %08x bytes in [ %p .. %p ], query count was %u - returned %s: %p\n",
                countOfCalls, (DWORD)dwSize, pMinAddr, pMaxAddr,
                virtualQueryCount, (pResult != nullptr) ? "success" : "failure", pResult);

    // If we failed this call the process will typically be terminated
    // so we log any additional reason for failing this call.
    //
    if (pResult == nullptr)
    {
        if ((tryAddr + dwSize) > (BYTE *)pMaxAddr)
        {
            // Our tryAddr reached pMaxAddr
            STRESS_LOG0(LF_JIT, LL_INFO100, "Additional reason: Address space exhausted.\n");
        }

        if (virtualQueryFailed)
        {
            STRESS_LOG0(LF_JIT, LL_INFO100, "Additional reason: VirtualQuery operation failed.\n");
        }

        if (faultInjected)
        {
            STRESS_LOG0(LF_JIT, LL_INFO100, "Additional reason: fault injected.\n");
        }
    }

    return pResult;
}

//******************************************************************************
// NumaNodeInfo
//******************************************************************************
#if !defined(FEATURE_NATIVEAOT)

/*static*/ LPVOID NumaNodeInfo::VirtualAllocExNuma(HANDLE hProc, LPVOID lpAddr, SIZE_T dwSize,
                         DWORD allocType, DWORD prot, DWORD node)
{
    return ::VirtualAllocExNuma(hProc, lpAddr, dwSize, allocType, prot, node);
}

#ifdef HOST_WINDOWS
/*static*/ BOOL NumaNodeInfo::GetNumaProcessorNodeEx(PPROCESSOR_NUMBER proc_no, PUSHORT node_no)
{
    return ::GetNumaProcessorNodeEx(proc_no, node_no);
}
/*static*/ bool NumaNodeInfo::GetNumaInfo(PUSHORT total_nodes, DWORD* max_procs_per_node)
{
    if (m_enableGCNumaAware)
    {
        DWORD currentProcsOnNode = 0;
        for (uint16_t i = 0; i < m_nNodes; i++)
        {
            GROUP_AFFINITY processorMask;
            if (GetNumaNodeProcessorMaskEx(i, &processorMask))
            {
                DWORD procsOnNode = 0;
                uintptr_t mask = (uintptr_t)processorMask.Mask;
                while (mask)
                {
                    procsOnNode++;
                    mask &= mask - 1;
                }

                currentProcsOnNode = max(currentProcsOnNode, procsOnNode);
            }
        }

        *max_procs_per_node = currentProcsOnNode;
        *total_nodes = m_nNodes;
        return true;
    }

    return false;
}
#else // HOST_WINDOWS
/*static*/ BOOL NumaNodeInfo::GetNumaProcessorNodeEx(USHORT proc_no, PUSHORT node_no)
{
    return PAL_GetNumaProcessorNode(proc_no, node_no);
}
#endif // HOST_WINDOWS
#endif

/*static*/ BOOL NumaNodeInfo::m_enableGCNumaAware = FALSE;
/*static*/ uint16_t NumaNodeInfo::m_nNodes = 0;
/*static*/ BOOL NumaNodeInfo::InitNumaNodeInfoAPI()
{
#if !defined(FEATURE_NATIVEAOT)
    //check for numa support if multiple heaps are used
    ULONG highest = 0;

    if (CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_GCNumaAware) == 0)
        return FALSE;

    // fail to get the highest numa node number
    if (!::GetNumaHighestNodeNumber(&highest) || (highest == 0))
        return FALSE;

    m_nNodes = (USHORT)(highest + 1);

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

#ifdef HOST_WINDOWS

//******************************************************************************
// CPUGroupInfo
//******************************************************************************
#if !defined(FEATURE_NATIVEAOT)
/*static*/ //CPUGroupInfo::PNTQSIEx CPUGroupInfo::m_pNtQuerySystemInformationEx = NULL;

/*static*/ BOOL CPUGroupInfo::GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP relationship,
                         SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX *slpiex, PDWORD count)
{
    LIMITED_METHOD_CONTRACT;
    return ::GetLogicalProcessorInformationEx(relationship, slpiex, count);
}

/*static*/ BOOL CPUGroupInfo::SetThreadGroupAffinity(HANDLE h,
                        const GROUP_AFFINITY *groupAffinity, GROUP_AFFINITY *previousGroupAffinity)
{
    LIMITED_METHOD_CONTRACT;
    return ::SetThreadGroupAffinity(h, groupAffinity, previousGroupAffinity);
}

/*static*/ BOOL CPUGroupInfo::GetThreadGroupAffinity(HANDLE h, GROUP_AFFINITY *groupAffinity)
{
    LIMITED_METHOD_CONTRACT;
    return ::GetThreadGroupAffinity(h, groupAffinity);
}

/*static*/ BOOL CPUGroupInfo::GetSystemTimes(FILETIME *idleTime, FILETIME *kernelTime, FILETIME *userTime)
{
    LIMITED_METHOD_CONTRACT;

#ifdef HOST_WINDOWS
    return ::GetSystemTimes(idleTime, kernelTime, userTime);
#else
    return FALSE;
#endif
}
#endif

/*static*/ BOOL CPUGroupInfo::m_enableGCCPUGroups = FALSE;
/*static*/ BOOL CPUGroupInfo::m_threadUseAllCpuGroups = FALSE;
/*static*/ BOOL CPUGroupInfo::m_threadAssignCpuGroups = FALSE;
/*static*/ WORD CPUGroupInfo::m_nGroups = 0;
/*static*/ WORD CPUGroupInfo::m_nProcessors = 0;
/*static*/ WORD CPUGroupInfo::m_initialGroup = 0;
/*static*/ CPU_Group_Info *CPUGroupInfo::m_CPUGroupInfoArray = NULL;
/*static*/ LONG CPUGroupInfo::m_initialization = 0;

#if !defined(FEATURE_NATIVEAOT) && (defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64))
// Calculate greatest common divisor
DWORD GCD(DWORD u, DWORD v)
{
    while (v != 0)
    {
        DWORD dwTemp = v;
        v = u % v;
        u = dwTemp;
    }

    return u;
}

// Calculate least common multiple
DWORD LCM(DWORD u, DWORD v)
{
    return u / GCD(u, v) * v;
}
#endif

/*static*/ BOOL CPUGroupInfo::InitCPUGroupInfoArray()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#if !defined(FEATURE_NATIVEAOT) && (defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64))
    BYTE *bBuffer = NULL;
    SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX *pSLPIEx = NULL;
    SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX *pRecord = NULL;
    DWORD cbSLPIEx = 0;
    DWORD byteOffset = 0;
    DWORD dwNumElements = 0;
    DWORD dwWeight = 1;

    if (CPUGroupInfo::GetLogicalProcessorInformationEx(RelationGroup, pSLPIEx, &cbSLPIEx) ||
        GetLastError() != ERROR_INSUFFICIENT_BUFFER)
        return FALSE;

    _ASSERTE(cbSLPIEx);

    // Fail to allocate buffer
    bBuffer = new (nothrow) BYTE[ cbSLPIEx ];
    if (bBuffer == NULL)
        return FALSE;

    pSLPIEx = (SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX *)bBuffer;
    if (!::GetLogicalProcessorInformationEx(RelationGroup, pSLPIEx, &cbSLPIEx))
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
        m_CPUGroupInfoArray[i].begin       = m_nProcessors;
        m_nProcessors += m_CPUGroupInfoArray[i].nr_active;
        dwWeight = LCM(dwWeight, (DWORD)m_CPUGroupInfoArray[i].nr_active);
    }

    // The number of threads per group that can be supported will depend on the number of CPU groups
    // and the number of LPs within each processor group. For example, when the number of LPs in
    // CPU groups is the same and is 64, the number of threads per group before weight overflow
    // would be 2^32/2^6 = 2^26 (64M threads)
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

/*static*/ void CPUGroupInfo::InitCPUGroupInfo()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#if !defined(FEATURE_NATIVEAOT) && (defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64))
    BOOL enableGCCPUGroups = Configuration::GetKnobBooleanValue(W("System.GC.CpuGroup"), CLRConfig::EXTERNAL_GCCpuGroup);

    if (!enableGCCPUGroups)
        return;

    if (!InitCPUGroupInfoArray())
        return;

    // Enable processor groups only if more than one group exists
    if (m_nGroups > 1)
    {
        m_enableGCCPUGroups = TRUE;
        m_threadUseAllCpuGroups = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_Thread_UseAllCpuGroups) != 0;
        m_threadAssignCpuGroups = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_Thread_AssignCpuGroups) != 0;

        // Save the processor group affinity of the initial thread
        GROUP_AFFINITY groupAffinity;
        CPUGroupInfo::GetThreadGroupAffinity(GetCurrentThread(), &groupAffinity);
        m_initialGroup = groupAffinity.Group;
    }
#endif
}

/*static*/ BOOL CPUGroupInfo::IsInitialized()
{
    LIMITED_METHOD_CONTRACT;
    return VolatileLoad(&m_initialization) == -1;
}

/*static*/ void CPUGroupInfo::EnsureInitialized()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // CPUGroupInfo needs to be initialized only once. This could happen in three cases
    // 1. CLR initialization at beginning of EEStartup, or
    // 2. Sometimes, when hosted by ASP.NET, the hosting process may initialize ThreadPool
    //    before initializing CLR, thus require CPUGroupInfo to be initialized to determine
    //    if CPU group support should/could be enabled.
    // 3. Call into Threadpool functions before Threadpool _and_ CLR is initialized.
    // Vast majority of time, CPUGroupInfo is initialized in case 1. or 2.
    // The chance of contention will be extremely small, so the following code should be fine
    //
    if (IsInitialized())
        return;

    if (InterlockedCompareExchange(&m_initialization, 1, 0) == 0)
    {
        InitCPUGroupInfo();
        VolatileStore(&m_initialization, -1L);
    }
    else
    {
        // Some other thread started initialization, just wait until complete
        while (VolatileLoad(&m_initialization) != -1)
        {
            SwitchToThread();
        }
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

#if !defined(FEATURE_NATIVEAOT) && (defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64))
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
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#if !defined(FEATURE_NATIVEAOT) && (defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64))
    _ASSERTE(m_enableGCCPUGroups && m_threadUseAllCpuGroups);

    PROCESSOR_NUMBER proc_no;
    proc_no.Group=0;
    proc_no.Number=0;
    proc_no.Reserved=0;
    ::GetCurrentProcessorNumberEx(&proc_no);

    DWORD fullNumber = 0;
    for (WORD i = 0; i < proc_no.Group; i++)
        fullNumber += (DWORD)m_CPUGroupInfoArray[i].nr_active;
    fullNumber += (DWORD)(proc_no.Number);

    return fullNumber;
#else
    return 0;
#endif
}

// There can be different numbers of procs in groups. We take the max.
/*static*/ bool CPUGroupInfo::GetCPUGroupInfo(PUSHORT total_groups, DWORD* max_procs_per_group)
{
    if (m_enableGCCPUGroups)
    {
        *total_groups = m_nGroups;
        DWORD currentProcsInGroup = 0;
        for (WORD i = 0; i < m_nGroups; i++)
        {
            currentProcsInGroup = max(currentProcsInGroup, m_CPUGroupInfoArray[i].nr_active);
        }
        *max_procs_per_group = currentProcsInGroup;
        return true;
    }

    return false;
}

#if !defined(FEATURE_NATIVEAOT)
//Lock ThreadStore before calling this function, so that updates of weights/counts are consistent
/*static*/ void CPUGroupInfo::ChooseCPUGroupAffinity(GROUP_AFFINITY *gf)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#if (defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64))
    WORD i, minGroup = 0;
    DWORD minWeight = 0;

    _ASSERTE(m_enableGCCPUGroups && m_threadUseAllCpuGroups && m_threadAssignCpuGroups);

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
#if (defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64))
    _ASSERTE(m_enableGCCPUGroups && m_threadUseAllCpuGroups && m_threadAssignCpuGroups);

    WORD group = gf->Group;
    m_CPUGroupInfoArray[group].activeThreadWeight -= m_CPUGroupInfoArray[group].groupWeight;
#endif
}

BOOL CPUGroupInfo::GetCPUGroupRange(WORD group_number, WORD* group_begin, WORD* group_size)
{
    if (group_number >= m_nGroups)
    {
        return FALSE;
    }

    *group_begin = m_CPUGroupInfoArray[group_number].begin;
    *group_size = m_CPUGroupInfoArray[group_number].nr_active;

    return TRUE;
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
    _ASSERTE(m_enableGCCPUGroups || !m_threadUseAllCpuGroups);
    return m_threadUseAllCpuGroups;
}

/*static*/ BOOL CPUGroupInfo::CanAssignCpuGroupsToThreads()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(m_enableGCCPUGroups || !m_threadAssignCpuGroups);
    return m_threadAssignCpuGroups;
}
#endif // HOST_WINDOWS

extern SYSTEM_INFO g_SystemInfo;

int GetTotalProcessorCount()
{
    LIMITED_METHOD_CONTRACT;

#ifdef HOST_WINDOWS
    if (CPUGroupInfo::CanEnableGCCPUGroups())
    {
        return CPUGroupInfo::GetNumActiveProcessors();
    }
    else
    {
        return g_SystemInfo.dwNumberOfProcessors;
    }
#else // HOST_WINDOWS
    return PAL_GetTotalCpuCount();
#endif // HOST_WINDOWS
}

// The cached number of CPUs available for the current process
static DWORD g_currentProcessCpuCount = 0;

//******************************************************************************
// Returns the number of processors that a process has been configured to run on
//******************************************************************************
int GetCurrentProcessCpuCount()
{
    CONTRACTL
    {
        NOTHROW;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    if (g_currentProcessCpuCount > 0)
        return g_currentProcessCpuCount;

    DWORD count;

    // If the configuration value has been set, it takes precedence. Otherwise, take into account
    // process affinity and CPU quota limit.

    DWORD configValue = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_ProcessorCount);
    const unsigned int MAX_PROCESSOR_COUNT = 0xffff;

    if (0 < configValue && configValue <= MAX_PROCESSOR_COUNT)
    {
        count = configValue;
    }
    else
    {
#ifdef HOST_WINDOWS
        CPUGroupInfo::EnsureInitialized();

        if (CPUGroupInfo::CanEnableThreadUseAllCpuGroups())
        {
            count = CPUGroupInfo::GetNumActiveProcessors();
        }
        else
        {
            DWORD_PTR pmask, smask;

            if (!GetProcessAffinityMask(GetCurrentProcess(), &pmask, &smask))
            {
                count = 1;
            }
            else
            {
                pmask &= smask;
                count = 0;

                while (pmask)
                {
                    pmask &= (pmask - 1);
                    count++;
                }

                // GetProcessAffinityMask can return pmask=0 and smask=0 on systems with more
                // than 64 processors, which would leave us with a count of 0.  Since the GC
                // expects there to be at least one processor to run on (and thus at least one
                // heap), we'll return 64 here if count is 0, since there are likely a ton of
                // processors available in that case.
                if (count == 0)
                    count = 64;
            }
        }

        JOBOBJECT_CPU_RATE_CONTROL_INFORMATION cpuRateControl;

        if (QueryInformationJobObject(NULL, JobObjectCpuRateControlInformation, &cpuRateControl,
            sizeof(cpuRateControl), NULL))
        {
            const DWORD HardCapEnabled = JOB_OBJECT_CPU_RATE_CONTROL_ENABLE | JOB_OBJECT_CPU_RATE_CONTROL_HARD_CAP;
            const DWORD MinMaxRateEnabled = JOB_OBJECT_CPU_RATE_CONTROL_ENABLE | JOB_OBJECT_CPU_RATE_CONTROL_MIN_MAX_RATE;
            DWORD maxRate = 0;

            if ((cpuRateControl.ControlFlags & HardCapEnabled) == HardCapEnabled)
            {
                maxRate = cpuRateControl.CpuRate;
            }
            else if ((cpuRateControl.ControlFlags & MinMaxRateEnabled) == MinMaxRateEnabled)
            {
                maxRate = cpuRateControl.MaxRate;
            }

            // The rate is the percentage times 100
            const DWORD MAXIMUM_CPU_RATE = 10000;

            if (0 < maxRate && maxRate < MAXIMUM_CPU_RATE)
            {
                DWORD cpuLimit = (maxRate * GetTotalProcessorCount() + MAXIMUM_CPU_RATE - 1) / MAXIMUM_CPU_RATE;
                if (cpuLimit < count)
                    count = cpuLimit;
            }
        }

#else // HOST_WINDOWS
        count = PAL_GetLogicalCpuCountFromOS();

        uint32_t cpuLimit;
        if (PAL_GetCpuLimit(&cpuLimit) && cpuLimit < count)
            count = cpuLimit;
#endif // HOST_WINDOWS
    }

    _ASSERTE(count > 0);
    g_currentProcessCpuCount = count;

    return count;
}

#ifdef HOST_WINDOWS
DWORD_PTR GetCurrentProcessCpuMask()
{
    CONTRACTL
    {
        NOTHROW;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

#ifdef HOST_WINDOWS
    DWORD_PTR pmask, smask;

    if (!GetProcessAffinityMask(GetCurrentProcess(), &pmask, &smask))
        return 1;

    pmask &= smask;
    return pmask;
#else
    return 0;
#endif
}
#endif // HOST_WINDOWS

uint32_t GetOsPageSizeUncached()
{
    SYSTEM_INFO sysInfo;
    ::GetSystemInfo(&sysInfo);
    return sysInfo.dwAllocationGranularity ? sysInfo.dwAllocationGranularity : 0x1000;
}

namespace
{
    Volatile<uint32_t> g_pageSize = 0;
}

uint32_t GetOsPageSize()
{
#ifdef HOST_UNIX
    size_t result = g_pageSize.LoadWithoutBarrier();

    if(!result)
    {
        result = GetOsPageSizeUncached();

        g_pageSize.StoreWithoutBarrier(result);
    }

    return result;
#else
    return 0x1000;
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
bool ConfigMethodSet::contains(LPCUTF8 methodName, LPCUTF8 className, CORINFO_SIG_INFO* pSigInfo)
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    _ASSERTE(m_inited == 1);

    if (m_list.IsEmpty())
        return false;
    return(m_list.IsInList(methodName, className, pSigInfo));
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

AssemblyNamesList::AssemblyNamesList(_In_ LPWSTR list)
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

void MethodNamesListBase::Insert(_In_z_ LPWSTR str)
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

    int numArgs = -1;
    if (sig != NULL)
    {
        sig++;      // Skip calling convention
        numArgs = CorSigUncompressData(sig);
    }

    return IsInList(methName, clsName, numArgs);
}

/**************************************************************/
bool MethodNamesListBase::IsInList(LPCUTF8 methName, LPCUTF8 clsName, CORINFO_SIG_INFO* pSigInfo)
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    int numArgs = -1;
    if (pSigInfo != NULL)
    {
        numArgs = pSigInfo->numArgs;
    }

    return IsInList(methName, clsName, numArgs);
}

/**************************************************************/
bool MethodNamesListBase::IsInList(LPCUTF8 methName, LPCUTF8 clsName, int numArgs)
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    // Try to match all the entries in the list

    for(MethodName * pName = pNames; pName; pName = pName->next)
    {
        // If numArgs is valid, check for mismatch
        if (pName->numArgs != -1 && pName->numArgs != numArgs)
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
    }
    CONTRACTL_END;

    BYTE        elementType;          // Current element type being processed.
    mdToken     token;                  // Embedded token.
    uint32_t    ulArgCnt;               // Argument count for function pointer.
    uint32_t    ulIndex;                // Index for type parameters
    uint32_t    ulRank;                 // Rank of the array.
    uint32_t    ulSizes;                // Count of sized dimensions of the array.
    uint32_t    ulLbnds;                // Count of lower bounds of the array.
    uint32_t    ulCallConv;

    HRESULT     hr = S_OK;              // Value returned.
    BOOL        bRepeat = TRUE;         // MODOPT and MODREQ belong to the arg after them

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
                FALLTHROUGH;

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
            case ELEMENT_TYPE_BYREF:
            case ELEMENT_TYPE_PINNED:
            case ELEMENT_TYPE_SZARRAY:
                // Validate the referenced type.
                if(FAILED(hr = validateOneArg(tk, pSig, pulNSentinels, pImport, TRUE))) IfFailGo(hr);
                break;
            case ELEMENT_TYPE_CMOD_OPT:
            case ELEMENT_TYPE_CMOD_REQD:
                bRepeat = TRUE; // go on validating, we're not done with this arg
                FALLTHROUGH;
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

    uint32_t    ulCallConv;             // Calling convention.
    uint32_t    ulArgCount = 1;         // Count of arguments (1 because of the return type)
    uint32_t    ulTyArgCount = 0;       // Count of type arguments
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
            FALLTHROUGH;

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

#if defined(TARGET_ARM)
    // Ensure that the ThumbBit is not set on the offset
    // as it cannot be encoded.
    _ASSERTE(!(imm24 & THUMB_CODE));
#endif // TARGET_ARM

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
//  Extract the PC-Relative offset from an adrp instruction
//*****************************************************************************
INT32 GetArm64Rel21(UINT32 * pCode)
{
    LIMITED_METHOD_CONTRACT;

    UINT32 addInstr = *pCode;

    // 23-5 bits for the high part. Shift it by 5.
    INT32 immhi = (((INT32)(addInstr & 0xFFFFE0))) >> 5;
    // 30,29 bits for the lower part. Shift it by 29.
    INT32 immlo = ((INT32)(addInstr & 0x60000000)) >> 29;

    // Merge them
    INT32 imm21 = (immhi << 2) | immlo;

    return imm21;
}

//*****************************************************************************
//  Extract the PC-Relative offset from an add instruction
//*****************************************************************************
INT32 GetArm64Rel12(UINT32 * pCode)
{
    LIMITED_METHOD_CONTRACT;

    UINT32 addInstr = *pCode;

    // 21-10 contains value. Mask 12 bits and shift by 10 bits.
    INT32 imm12 = (INT32)(addInstr & 0x003FFC00) >> 10;

    return imm12;
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

//*****************************************************************************
//  Deposit the PC-Relative offset 'imm21' into an adrp instruction
//*****************************************************************************
void PutArm64Rel21(UINT32 * pCode, INT32 imm21)
{
    LIMITED_METHOD_CONTRACT;

    // Verify that we got a valid offset
    _ASSERTE(FitsInRel21(imm21));

    UINT32 adrpInstr = *pCode;
    // Check adrp opcode 1ii1 0000 ...
    _ASSERTE((adrpInstr & 0x9F000000) == 0x90000000);

    adrpInstr &= 0x9F00001F;               // keep bits 31, 28-24, 4-0.
    INT32 immlo = imm21 & 0x03;            // Extract low 2 bits which will occupy 30-29 bits.
    INT32 immhi = (imm21 & 0x1FFFFC) >> 2; // Extract high 19 bits which will occupy 23-5 bits.
    adrpInstr |= ((immlo << 29) | (immhi << 5));

    *pCode = adrpInstr;                    // write the assembled instruction

    _ASSERTE(GetArm64Rel21(pCode) == imm21);
}

//*****************************************************************************
//  Deposit the PC-Relative offset 'imm12' into an add instruction
//*****************************************************************************
void PutArm64Rel12(UINT32 * pCode, INT32 imm12)
{
    LIMITED_METHOD_CONTRACT;

    // Verify that we got a valid offset
    _ASSERTE(FitsInRel12(imm12));

    UINT32 addInstr = *pCode;
    // Check add opcode 1001 0001 00...
    _ASSERTE((addInstr & 0xFFC00000) == 0x91000000);

    addInstr &= 0xFFC003FF;     // keep bits 31-22, 9-0
    addInstr |= (imm12 << 10);  // Occupy 21-10.

    *pCode = addInstr;          // write the assembled instruction

    _ASSERTE(GetArm64Rel12(pCode) == imm12);
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

//======================================================================
// This function returns true, if it can determine that the instruction pointer
// refers to a code address that belongs in the range of the given image.
BOOL IsIPInModule(PTR_VOID pModuleBaseAddress, PCODE ip)
{
    STATIC_CONTRACT_LEAF;
    SUPPORTS_DAC;

    struct Param
    {
        PTR_VOID pModuleBaseAddress;
        PCODE ip;
        BOOL fRet;
    } param;
    param.pModuleBaseAddress = pModuleBaseAddress;
    param.ip = ip;
    param.fRet = FALSE;

// UNIXTODO: implement a proper version for PAL
#ifdef HOST_WINDOWS
    PAL_TRY(Param *, pParam, &param)
    {
        PTR_BYTE pBase = dac_cast<PTR_BYTE>(pParam->pModuleBaseAddress);

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
#endif // HOST_WINDOWS

    return param.fRet;
}

namespace Clr
{
namespace Util
{
#ifdef HOST_WINDOWS
    // Struct used to scope suspension of client impersonation for the current thread.
    // https://docs.microsoft.com/en-us/windows/desktop/secauthz/client-impersonation
    class SuspendImpersonation
    {
    public:
        SuspendImpersonation()
            : _token(nullptr)
        {
            // The approach used here matches what is used elsewhere in CLR (RevertIfImpersonated).
            // In general, OpenThreadToken fails with ERROR_NO_TOKEN if impersonation is not active,
            // fails with ERROR_CANT_OPEN_ANONYMOUS if anonymous impersonation is active, and otherwise
            // succeeds and returns the active impersonation token.
            BOOL res = ::OpenThreadToken(::GetCurrentThread(), TOKEN_IMPERSONATE, /* OpenAsSelf */ TRUE, &_token);
            if (res != FALSE)
            {
                ::RevertToSelf();
            }
            else
            {
                _token = nullptr;
            }
        }

        ~SuspendImpersonation()
        {
            if (_token != nullptr)
                ::SetThreadToken(nullptr, _token);
        }

    private:
        HandleHolder _token;
    };

    struct ProcessIntegrityResult
    {
        BOOL Success;
        DWORD Integrity;
        HRESULT LastError;

        HRESULT RecordAndReturnError(HRESULT hr)
        {
            LastError = hr;
            return hr;
        }
    };

    // The system calls in this code can fail if run with reduced privileges.
    // It is the caller's responsibility to choose an appropriate default in the event
    // that this function fails to retrieve the current process integrity.
    HRESULT GetCurrentProcessIntegrity(DWORD *integrity)
    {
        static ProcessIntegrityResult s_Result = { FALSE, 0, S_FALSE };

        if (FALSE != InterlockedCompareExchangeT(&s_Result.Success, FALSE, FALSE))
        {
            *integrity = s_Result.Integrity;
            return S_OK;
        }

        // Temporarily suspend impersonation (if possible) while computing the integrity level.
        // If impersonation is active, the OpenProcessToken call below will check the impersonation
        // token against the process token ACL, and will generally fail with ERROR_ACCESS_DENIED if
        // the impersonation token is less privileged than this process's primary token.
        Clr::Util::SuspendImpersonation si;

        HandleHolder hToken;
        if(!OpenProcessToken(GetCurrentProcess(), TOKEN_READ, &hToken))
            return s_Result.RecordAndReturnError(HRESULT_FROM_GetLastError());

        DWORD dwSize = 0;
        DWORD err = ERROR_SUCCESS;
        if(!GetTokenInformation(hToken, (TOKEN_INFORMATION_CLASS)TokenIntegrityLevel, nullptr, 0, &dwSize))
            err = GetLastError();

        // We need to make sure that GetTokenInformation failed in a predictable manner so we know that
        // dwSize has the correct buffer size in it.
        if (err != ERROR_INSUFFICIENT_BUFFER || dwSize == 0)
            return s_Result.RecordAndReturnError((err == ERROR_SUCCESS) ? E_FAIL : HRESULT_FROM_WIN32(err));

        NewArrayHolder<BYTE> pLabel = new (nothrow) BYTE[dwSize];
        if (pLabel == NULL)
            return s_Result.RecordAndReturnError(E_OUTOFMEMORY);

        if(!GetTokenInformation(hToken, (TOKEN_INFORMATION_CLASS)TokenIntegrityLevel, pLabel, dwSize, &dwSize))
            return s_Result.RecordAndReturnError(HRESULT_FROM_GetLastError());

        TOKEN_MANDATORY_LABEL *ptml = (TOKEN_MANDATORY_LABEL *)(void*)pLabel;
        PSID psidIntegrityLevelLabel = ptml->Label.Sid;

        s_Result.Integrity = *GetSidSubAuthority(psidIntegrityLevelLabel, (*GetSidSubAuthorityCount(psidIntegrityLevelLabel) - 1));
        *integrity = s_Result.Integrity;
        InterlockedExchangeT(&s_Result.Success, TRUE);
        return S_OK;
    }

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

    HRESULT ReadStringValue(HKEY hKey, LPCWSTR wszSubKey, LPCWSTR wszName, _Outptr_ _Outptr_result_z_ LPWSTR* pwszValue)
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
        __success(return == S_OK)
        static
        HRESULT FindSubKeyDefaultValueForCLSID(REFCLSID rclsid, LPCWSTR wszSubKeyName, SString & ssValue)
        {
            STANDARD_VM_CONTRACT;

            WCHAR wszClsid[39];
            if (GuidToLPWSTR(rclsid, wszClsid, ARRAY_SIZE(wszClsid)) == 0)
                return E_UNEXPECTED;

            StackSString ssKeyName;
            ssKeyName.Append(SL(W("CLSID\\")));
            ssKeyName.Append(wszClsid);
            ssKeyName.Append(SL(W("\\")));
            ssKeyName.Append(wszSubKeyName);

            // Query HKCR first to retain backwards compat with previous implementation where HKCR was only queried.
            // This is being done due to registry caching. This value will be used if the process integrity is medium or less.
            HRESULT hkcrResult = Clr::Util::Reg::ReadStringValue(HKEY_CLASSES_ROOT, ssKeyName.GetUnicode(), nullptr, ssValue);

            // HKCR is a virtualized registry hive that weaves together HKCU\Software\Classes and HKLM\Software\Classes
            // Processes with high integrity or greater should only read from HKLM to avoid being hijacked by medium
            // integrity processes writing to HKCU.
            DWORD integrity = SECURITY_MANDATORY_PROTECTED_PROCESS_RID;
            HRESULT hr = Clr::Util::GetCurrentProcessIntegrity(&integrity);
            if (hr != S_OK)
            {
                // In the event that we are unable to get the current process integrity,
                // we assume that this process is running in an elevated state.
                // GetCurrentProcessIntegrity may fail if the process has insufficient rights to get the integrity level
                integrity = SECURITY_MANDATORY_PROTECTED_PROCESS_RID;
            }

            if (integrity > SECURITY_MANDATORY_MEDIUM_RID)
            {
                Clr::Util::SuspendImpersonation si;

                // Clear the previous HKCR queried value
                ssValue.Clear();

                // Force to use HKLM
                StackSString ssHklmKeyName(SL(W("SOFTWARE\\Classes\\")));
                ssHklmKeyName.Append(ssKeyName);
                return Clr::Util::Reg::ReadStringValue(HKEY_LOCAL_MACHINE, ssHklmKeyName.GetUnicode(), nullptr, ssValue);
            }

            return hkcrResult;
        }
    }

    HRESULT FindInprocServer32UsingCLSID(REFCLSID rclsid, SString & ssInprocServer32Name)
    {
        WRAPPER_NO_CONTRACT;
        return __imp::FindSubKeyDefaultValueForCLSID(rclsid, W("InprocServer32"), ssInprocServer32Name);
    }
} // namespace Com
#endif //  HOST_WINDOWS

} // namespace Util
} // namespace Clr
