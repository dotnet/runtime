//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#ifndef _IEEMemoryManager
#define _IEEMemoryManager

#include "runtimedetails.h"

class interceptor_IEEMM : public IEEMemoryManager
{
public:
    interceptor_IEEMM() : original_IEEMM(nullptr)
    {
    }

private:
    //***************************************************************************
    // IUnknown methods
    //***************************************************************************
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID id, void** pInterface);
    ULONG STDMETHODCALLTYPE AddRef();
    ULONG STDMETHODCALLTYPE Release();

    //***************************************************************************
    // IEEMemoryManager methods for locking
    //***************************************************************************
    LPVOID STDMETHODCALLTYPE ClrVirtualAlloc(LPVOID lpAddress, SIZE_T dwSize, DWORD flAllocationType, DWORD flProtect);
    BOOL STDMETHODCALLTYPE ClrVirtualFree(LPVOID lpAddress, SIZE_T dwSize, DWORD dwFreeType);
    SIZE_T STDMETHODCALLTYPE ClrVirtualQuery(LPCVOID lpAddress, PMEMORY_BASIC_INFORMATION lpBuffer, SIZE_T dwLength);
    BOOL STDMETHODCALLTYPE ClrVirtualProtect(LPVOID lpAddress,
                                             SIZE_T dwSize,
                                             DWORD  flNewProtect,
                                             PDWORD lpflOldProtect);
    HANDLE STDMETHODCALLTYPE ClrGetProcessHeap();
    HANDLE STDMETHODCALLTYPE ClrHeapCreate(DWORD flOptions, SIZE_T dwInitialSize, SIZE_T dwMaximumSize);
    BOOL STDMETHODCALLTYPE ClrHeapDestroy(HANDLE hHeap);
    LPVOID STDMETHODCALLTYPE ClrHeapAlloc(HANDLE hHeap, DWORD dwFlags, SIZE_T dwBytes);
    BOOL STDMETHODCALLTYPE ClrHeapFree(HANDLE hHeap, DWORD dwFlags, LPVOID lpMem);
    BOOL STDMETHODCALLTYPE ClrHeapValidate(HANDLE hHeap, DWORD dwFlags, LPCVOID lpMem);
    HANDLE STDMETHODCALLTYPE ClrGetProcessExecutableHeap();

public:
    IEEMemoryManager* original_IEEMM;
};
#endif
