//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include "standardpch.h"
#include "spmiutil.h"
#include "ieememorymanager.h"

IEEMemoryManager* pIEEMM      = nullptr;
HANDLE            processHeap = INVALID_HANDLE_VALUE;

//***************************************************************************
// IUnknown methods
//***************************************************************************

HRESULT STDMETHODCALLTYPE MyIEEMM::QueryInterface(REFIID id, void** pInterface)
{
    DebugBreakorAV(133);
    return 0;
}
ULONG STDMETHODCALLTYPE MyIEEMM::AddRef()
{
    DebugBreakorAV(134);
    return 0;
}
ULONG STDMETHODCALLTYPE MyIEEMM::Release()
{
    DebugBreakorAV(135);
    return 0;
}

HANDLE virtHeap = INVALID_HANDLE_VALUE;

//***************************************************************************
// IEEMemoryManager methods for locking
//***************************************************************************
LPVOID STDMETHODCALLTYPE MyIEEMM::ClrVirtualAlloc(LPVOID lpAddress,
                                                  SIZE_T dwSize,
                                                  DWORD  flAllocationType,
                                                  DWORD  flProtect)
{
    if (virtHeap == INVALID_HANDLE_VALUE)
        virtHeap = HeapCreate(0, 0xFFFF, 0);
    if (virtHeap != INVALID_HANDLE_VALUE)
        return HeapAlloc(virtHeap, HEAP_ZERO_MEMORY, dwSize);
    return nullptr;
}
BOOL STDMETHODCALLTYPE MyIEEMM::ClrVirtualFree(LPVOID lpAddress, SIZE_T dwSize, DWORD dwFreeType)
{
    return HeapFree(virtHeap, 0, lpAddress);
}
SIZE_T STDMETHODCALLTYPE MyIEEMM::ClrVirtualQuery(LPCVOID                   lpAddress,
                                                  PMEMORY_BASIC_INFORMATION lpBuffer,
                                                  SIZE_T                    dwLength)
{
    DebugBreakorAV(136);
    return 0;
}
BOOL STDMETHODCALLTYPE MyIEEMM::ClrVirtualProtect(LPVOID lpAddress,
                                                  SIZE_T dwSize,
                                                  DWORD  flNewProtect,
                                                  PDWORD lpflOldProtect)
{
    DebugBreakorAV(137);
    return 0;
}
HANDLE STDMETHODCALLTYPE MyIEEMM::ClrGetProcessHeap()
{
    DebugBreakorAV(138);
    return 0;
}
HANDLE STDMETHODCALLTYPE MyIEEMM::ClrHeapCreate(DWORD flOptions, SIZE_T dwInitialSize, SIZE_T dwMaximumSize)
{
    DebugBreakorAV(139);
    return 0;
}
BOOL STDMETHODCALLTYPE MyIEEMM::ClrHeapDestroy(HANDLE hHeap)
{
    DebugBreakorAV(140);
    return 0;
}
LPVOID STDMETHODCALLTYPE MyIEEMM::ClrHeapAlloc(HANDLE hHeap, DWORD dwFlags, SIZE_T dwBytes)
{
    return HeapAlloc(hHeap, dwFlags, dwBytes);
}
BOOL STDMETHODCALLTYPE MyIEEMM::ClrHeapFree(HANDLE hHeap, DWORD dwFlags, LPVOID lpMem)
{
    return HeapFree(hHeap, dwFlags, lpMem);
}
BOOL STDMETHODCALLTYPE MyIEEMM::ClrHeapValidate(HANDLE hHeap, DWORD dwFlags, LPCVOID lpMem)
{
    DebugBreakorAV(141);
    return 0;
}
HANDLE STDMETHODCALLTYPE MyIEEMM::ClrGetProcessExecutableHeap()
{
    if (processHeap == INVALID_HANDLE_VALUE)
    {
        DWORD flOptions = 0;
#ifndef FEATURE_PAL // TODO-Review: PAL doesn't have HEAP_CREATE_ENABLE_EXECUTE. Is this ok?
        flOptions = HEAP_CREATE_ENABLE_EXECUTE;
#endif // !FEATURE_PAL
        processHeap = HeapCreate(flOptions, 10000, 0);
    }
    return processHeap;
}

IEEMemoryManager* InitIEEMemoryManager(JitInstance* jitInstance)
{
    if (pIEEMM == nullptr)
    {
        MyIEEMM* ieemm     = new MyIEEMM();
        ieemm->jitInstance = jitInstance;
        pIEEMM             = ieemm;
    }
    return pIEEMM;
}
