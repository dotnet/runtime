//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#ifndef _CoreClrCommonCallbacks
#define _CoreClrCommonCallbacks

#include "runtimedetails.h"

IExecutionEngine* IEE_t();
HRESULT GetCORSystemDirectory(LPWSTR pbuffer, DWORD cchBuffer, DWORD* pdwlength);
LPVOID EEHeapAllocInProcessHeap(DWORD dwFlags, SIZE_T dwBytes);
BOOL EEHeapFreeInProcessHeap(DWORD dwFlags, LPVOID lpMem);
void* GetCLRFunction(LPCSTR functionName);

typedef LPVOID (*pfnEEHeapAllocInProcessHeap)(DWORD dwFlags, SIZE_T dwBytes);
typedef BOOL (*pfnEEHeapFreeInProcessHeap)(DWORD dwFlags, LPVOID lpMem);

#endif
