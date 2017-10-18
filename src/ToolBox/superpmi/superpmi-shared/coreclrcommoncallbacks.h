//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#ifndef _CoreClrCommonCallbacks
#define _CoreClrCommonCallbacks

#include "runtimedetails.h"

IExecutionEngine* STDMETHODCALLTYPE IEE_t();
HRESULT STDMETHODCALLTYPE GetCORSystemDirectory(LPWSTR pbuffer, DWORD cchBuffer, DWORD* pdwlength);
LPVOID STDMETHODCALLTYPE EEHeapAllocInProcessHeap(DWORD dwFlags, SIZE_T dwBytes);
BOOL STDMETHODCALLTYPE EEHeapFreeInProcessHeap(DWORD dwFlags, LPVOID lpMem);
void* STDMETHODCALLTYPE GetCLRFunction(LPCSTR functionName);

typedef LPVOID(STDMETHODCALLTYPE* pfnEEHeapAllocInProcessHeap)(DWORD dwFlags, SIZE_T dwBytes);
typedef BOOL(STDMETHODCALLTYPE* pfnEEHeapFreeInProcessHeap)(DWORD dwFlags, LPVOID lpMem);

#endif
