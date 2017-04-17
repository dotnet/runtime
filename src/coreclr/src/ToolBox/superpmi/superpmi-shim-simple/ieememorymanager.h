//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#ifndef _IEEMemoryManager
#define _IEEMemoryManager

#include "runtimedetails.h"

/*
interface IEEMemoryManager : IUnknown
{
    LPVOID ClrVirtualAlloc(
        [in] LPVOID lpAddress,        // region to reserve or commit
        [in] SIZE_T dwSize,           // size of region
        [in] DWORD flAllocationType,  // type of allocation
        [in] DWORD flProtect          // type of access protection
    )

    BOOL ClrVirtualFree(
        [in] LPVOID lpAddress,   // address of region
        [in] SIZE_T dwSize,      // size of region
        [in] DWORD dwFreeType    // operation type
    )

    SIZE_T ClrVirtualQuery(
        [in] const void* lpAddress,                    // address of region
        [in] PMEMORY_BASIC_INFORMATION lpBuffer,  // information buffer
        [in] SIZE_T dwLength                      // size of buffer
    )

    BOOL ClrVirtualProtect(
        [in] LPVOID lpAddress,       // region of committed pages
        [in] SIZE_T dwSize,          // size of the region
        [in] DWORD flNewProtect,     // desired access protection
        [in] DWORD* lpflOldProtect   // old protection
    )

    HANDLE ClrGetProcessHeap()

    HANDLE ClrHeapCreate(
        [in] DWORD flOptions,       // heap allocation attributes
        [in] SIZE_T dwInitialSize,  // initial heap size
        [in] SIZE_T dwMaximumSize   // maximum heap size
    )

    BOOL ClrHeapDestroy(
        [in] HANDLE hHeap   // handle to heap
    )

    LPVOID ClrHeapAlloc(
        [in] HANDLE hHeap,   // handle to private heap block
        [in] DWORD dwFlags,  // heap allocation control
        [in] SIZE_T dwBytes  // number of bytes to allocate
    )

    BOOL ClrHeapFree(
        [in] HANDLE hHeap,  // handle to heap
        [in] DWORD dwFlags, // heap free options
        [in] LPVOID lpMem   // pointer to memory
    )

    BOOL ClrHeapValidate(
        [in] HANDLE hHeap,  // handle to heap
        [in] DWORD dwFlags, // heap access options
        [in] const void* lpMem   // optional pointer to memory block
    )

    HANDLE ClrGetProcessExecutableHeap()

};  // interface IEEMemoryManager

*/

class interceptor_IEEMM : public IEEMemoryManager
{
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
    // Added so we know where to make the real calls to.
    IEEMemoryManager* original_IEEMM;
};

#endif
