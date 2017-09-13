//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include "standardpch.h"
#include "coreclrcallbacks.h"
#include "iexecutionengine.h"

CoreClrCallbacks*           original_CoreClrCallbacks         = nullptr;
pfnEEHeapAllocInProcessHeap original_EEHeapAllocInProcessHeap = nullptr;
pfnEEHeapFreeInProcessHeap  original_EEHeapFreeInProcessHeap  = nullptr;

IExecutionEngine* STDMETHODCALLTYPE IEE_t()
{
    interceptor_IEE* iee = new interceptor_IEE();
    iee->original_IEE    = original_CoreClrCallbacks->m_pfnIEE();
    return iee;
}

/*#pragma warning( suppress :4996 ) //deprecated
HRESULT STDMETHODCALLTYPE GetCORSystemDirectory(LPWSTR pbuffer, DWORD cchBuffer, DWORD* pdwlength)
{
    DebugBreakorAV(131);
    return 0;
}
*/

LPVOID STDMETHODCALLTYPE EEHeapAllocInProcessHeap(DWORD dwFlags, SIZE_T dwBytes)
{
    if (original_EEHeapAllocInProcessHeap == nullptr)
        __debugbreak();
    return original_EEHeapAllocInProcessHeap(dwFlags, dwBytes);
}

BOOL STDMETHODCALLTYPE EEHeapFreeInProcessHeap(DWORD dwFlags, LPVOID lpMem)
{
    if (original_EEHeapFreeInProcessHeap == nullptr)
        __debugbreak();
    return original_EEHeapFreeInProcessHeap(dwFlags, lpMem);
}

void* STDMETHODCALLTYPE GetCLRFunction(LPCSTR functionName)
{
    if (strcmp(functionName, "EEHeapAllocInProcessHeap") == 0)
    {
        original_EEHeapAllocInProcessHeap =
            (pfnEEHeapAllocInProcessHeap)original_CoreClrCallbacks->m_pfnGetCLRFunction("EEHeapAllocInProcessHeap");
        return (void*)EEHeapAllocInProcessHeap;
    }
    if (strcmp(functionName, "EEHeapFreeInProcessHeap") == 0)
    {
        original_EEHeapFreeInProcessHeap =
            (pfnEEHeapFreeInProcessHeap)original_CoreClrCallbacks->m_pfnGetCLRFunction("EEHeapFreeInProcessHeap");
        return (void*)EEHeapFreeInProcessHeap;
    }
    return original_CoreClrCallbacks->m_pfnGetCLRFunction(functionName);
}
