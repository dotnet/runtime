//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#ifndef _CoreClrCallbacks
#define _CoreClrCallbacks

#include "runtimedetails.h"

IExecutionEngine* STDMETHODCALLTYPE IEE_t();
HRESULT STDMETHODCALLTYPE GetCORSystemDirectory(LPWSTR pbuffer, DWORD cchBuffer, DWORD* pdwlength);
LPVOID STDMETHODCALLTYPE EEHeapAllocInProcessHeap (DWORD dwFlags, SIZE_T dwBytes);
BOOL STDMETHODCALLTYPE EEHeapFreeInProcessHeap (DWORD dwFlags, LPVOID lpMem);
void* STDMETHODCALLTYPE GetCLRFunction(LPCSTR functionName);
CoreClrCallbacks *InitCoreClrCallbacks();

#endif