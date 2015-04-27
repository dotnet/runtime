//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//*****************************************************************************
// DbgShim.h
// 
//*****************************************************************************

#include <windows.h>

EXTERN_C HRESULT GetStartupNotificationEvent(DWORD debuggeePID,
                                    __out HANDLE* phStartupEvent);

EXTERN_C HRESULT CloseCLREnumeration(HANDLE* pHandleArray, LPWSTR* pStringArray, DWORD dwArrayLength);

EXTERN_C HRESULT EnumerateCLRs(DWORD debuggeePID, 
                      __out HANDLE** ppHandleArrayOut,
                      __out LPWSTR** ppStringArrayOut,
                      __out DWORD* pdwArrayLengthOut);

EXTERN_C HRESULT CreateVersionStringFromModule(DWORD pidDebuggee,
                                      LPCWSTR szModuleName,
                                      __out_ecount_part(cchBuffer, *pdwLength) LPWSTR pBuffer,
                                      DWORD cchBuffer,
                                      __out DWORD* pdwLength);

EXTERN_C HRESULT CreateDebuggingInterfaceFromVersionEx(
    int iDebuggerVersion,
    LPCWSTR szDebuggeeVersion,
    IUnknown ** ppCordb);

EXTERN_C HRESULT CreateDebuggingInterfaceFromVersion(
    LPCWSTR szDebuggeeVersion, 
    IUnknown ** ppCordb);

