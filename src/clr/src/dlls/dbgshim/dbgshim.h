//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//*****************************************************************************
// DbgShim.h
// 
//*****************************************************************************

#include <windows.h>

HRESULT GetStartupNotificationEvent(DWORD debuggeePID,
                                    __out HANDLE* phStartupEvent);

HRESULT CloseCLREnumeration(HANDLE* pHandleArray, LPWSTR* pStringArray, DWORD dwArrayLength);

HRESULT EnumerateCLRs(DWORD debuggeePID, 
                      __out HANDLE** ppHandleArrayOut,
                      __out LPWSTR** ppStringArrayOut,
                      __out DWORD* pdwArrayLengthOut);

HRESULT CreateVersionStringFromModule(DWORD pidDebuggee,
                                      LPCWSTR szModuleName,
                                      __out_ecount_part(cchBuffer, *pdwLength) LPWSTR pBuffer,
                                      DWORD cchBuffer,
                                      __out DWORD* pdwLength);

HRESULT CreateDebuggingInterfaceFromVersionEx(
    int iDebuggerVersion,
    LPCWSTR szDebuggeeVersion,
    IUnknown ** ppCordb);

HRESULT CreateDebuggingInterfaceFromVersion(
    LPCWSTR szDebuggeeVersion, 
    IUnknown ** ppCordb);

