//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include "standardpch.h"
#include "ieememorymanager.h"
#include "superpmi-shim-simple.h"

//***************************************************************************
// IUnknown methods
//***************************************************************************
HRESULT STDMETHODCALLTYPE interceptor_IEEMM::QueryInterface(REFIID id, void** pInterface)
{
    return original_IEEMM->QueryInterface(id, pInterface);
}
ULONG STDMETHODCALLTYPE interceptor_IEEMM::AddRef()
{
    return original_IEEMM->AddRef();
}
ULONG STDMETHODCALLTYPE interceptor_IEEMM::Release()
{
    return original_IEEMM->Release();
}

//***************************************************************************
// IEEMemoryManager methods for locking
//***************************************************************************
LPVOID STDMETHODCALLTYPE interceptor_IEEMM::ClrVirtualAlloc(LPVOID lpAddress,
                                                            SIZE_T dwSize,
                                                            DWORD  flAllocationType,
                                                            DWORD  flProtect)
{
    return original_IEEMM->ClrVirtualAlloc(lpAddress, dwSize, flAllocationType, flProtect);
}
BOOL STDMETHODCALLTYPE interceptor_IEEMM::ClrVirtualFree(LPVOID lpAddress, SIZE_T dwSize, DWORD dwFreeType)
{
    return original_IEEMM->ClrVirtualFree(lpAddress, dwSize, dwFreeType);
}
SIZE_T STDMETHODCALLTYPE interceptor_IEEMM::ClrVirtualQuery(LPCVOID                   lpAddress,
                                                            PMEMORY_BASIC_INFORMATION lpBuffer,
                                                            SIZE_T                    dwLength)
{
    return original_IEEMM->ClrVirtualQuery(lpAddress, lpBuffer, dwLength);
}
BOOL STDMETHODCALLTYPE interceptor_IEEMM::ClrVirtualProtect(LPVOID lpAddress,
                                                            SIZE_T dwSize,
                                                            DWORD  flNewProtect,
                                                            PDWORD lpflOldProtect)
{
    return original_IEEMM->ClrVirtualProtect(lpAddress, dwSize, flNewProtect, lpflOldProtect);
}
HANDLE STDMETHODCALLTYPE interceptor_IEEMM::ClrGetProcessHeap()
{
    return original_IEEMM->ClrGetProcessHeap();
}
HANDLE STDMETHODCALLTYPE interceptor_IEEMM::ClrHeapCreate(DWORD flOptions, SIZE_T dwInitialSize, SIZE_T dwMaximumSize)
{
    return original_IEEMM->ClrHeapCreate(flOptions, dwInitialSize, dwMaximumSize);
}
BOOL STDMETHODCALLTYPE interceptor_IEEMM::ClrHeapDestroy(HANDLE hHeap)
{
    return original_IEEMM->ClrHeapDestroy(hHeap);
}
LPVOID STDMETHODCALLTYPE interceptor_IEEMM::ClrHeapAlloc(HANDLE hHeap, DWORD dwFlags, SIZE_T dwBytes)
{
    return original_IEEMM->ClrHeapAlloc(hHeap, dwFlags, dwBytes);
}
BOOL STDMETHODCALLTYPE interceptor_IEEMM::ClrHeapFree(HANDLE hHeap, DWORD dwFlags, LPVOID lpMem)
{
    return original_IEEMM->ClrHeapFree(hHeap, dwFlags, lpMem);
}
BOOL STDMETHODCALLTYPE interceptor_IEEMM::ClrHeapValidate(HANDLE hHeap, DWORD dwFlags, LPCVOID lpMem)
{
    return original_IEEMM->ClrHeapValidate(hHeap, dwFlags, lpMem);
}
HANDLE STDMETHODCALLTYPE interceptor_IEEMM::ClrGetProcessExecutableHeap()
{
    return original_IEEMM->ClrGetProcessExecutableHeap();
}
