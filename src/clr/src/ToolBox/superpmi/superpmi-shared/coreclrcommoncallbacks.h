//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#ifndef _CoreClrCommonCallbacks
#define _CoreClrCommonCallbacks

#include "runtimedetails.h"

IExecutionEngine* STDMETHODCALLTYPE IEE_t();
HRESULT STDMETHODCALLTYPE GetCORSystemDirectory(LPWSTR pbuffer, DWORD cchBuffer, DWORD* pdwlength);
void* STDMETHODCALLTYPE GetCLRFunction(LPCSTR functionName);

#endif
