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

void* STDMETHODCALLTYPE GetCLRFunction(LPCSTR functionName)
{
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
