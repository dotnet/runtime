//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include "standardpch.h"
#include "spmiutil.h"
#include "coreclrcallbacks.h"
#include "iexecutionengine.h"

IExecutionEngine* STDMETHODCALLTYPE IEE_t()
{
    MyIEE* iee = InitIExecutionEngine();
    return iee;
}

/*#pragma warning( suppress :4996 ) //deprecated
HRESULT STDMETHODCALLTYPE GetCORSystemDirectory(LPWSTR pbuffer, DWORD cchBuffer, DWORD* pdwlength)
{
    DebugBreakorAV(131);
    return 0;
}
*/

HANDLE ourHeap = nullptr;

LPVOID STDMETHODCALLTYPE EEHeapAllocInProcessHeap(DWORD dwFlags, SIZE_T dwBytes)
{
    if (ourHeap == nullptr)
        ourHeap = HeapCreate(0, 4096, 0);
    if (ourHeap == nullptr)
    {
        LogError("HeapCreate Failed");
        __debugbreak();
        return nullptr;
    }
    LPVOID result = HeapAlloc(ourHeap, dwFlags, dwBytes);
    //   LogDebug("EEHeapAllocInProcessHeap %p %u %u", result, dwFlags, dwBytes);
    return result;
}

BOOL STDMETHODCALLTYPE EEHeapFreeInProcessHeap(DWORD dwFlags, LPVOID lpMem)
{
    //  return true;
    return HeapFree(ourHeap, dwFlags, lpMem);
}

void* STDMETHODCALLTYPE GetCLRFunction(LPCSTR functionName)
{
    if (strcmp(functionName, "EEHeapAllocInProcessHeap") == 0)
        return (void*)EEHeapAllocInProcessHeap;
    if (strcmp(functionName, "EEHeapFreeInProcessHeap") == 0)
        return (void*)EEHeapFreeInProcessHeap;
    DebugBreakorAV(132);
    return nullptr;
}

CoreClrCallbacks* InitCoreClrCallbacks()
{
    CoreClrCallbacks* temp = new CoreClrCallbacks();
    ::ZeroMemory(temp, sizeof(CoreClrCallbacks));

    temp->m_hmodCoreCLR              = (HINSTANCE)(size_t)0xbadbad01; // any non-null value seems okay...
    temp->m_pfnIEE                   = IEE_t;
    temp->m_pfnGetCORSystemDirectory = nullptr; // GetCORSystemDirectory;
    temp->m_pfnGetCLRFunction        = GetCLRFunction;

    return temp;
}
