//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//*****************************************************************************
// DbgShim.h
// 
//*****************************************************************************

#include <windows.h>

typedef VOID (*PSTARTUP_CALLBACK)(IUnknown *pCordb, PVOID parameter, HRESULT hr);

EXTERN_C HRESULT
RegisterForRuntimeStartup(
    __in DWORD dwProcessId,
    __in PSTARTUP_CALLBACK pfnCallback,
    __in PVOID parameter,
    __out PVOID *ppUnregisterToken);

EXTERN_C HRESULT
UnregisterForRuntimeStartup(
    __in PVOID pUnregisterToken);

EXTERN_C HRESULT 
GetStartupNotificationEvent(
    __in DWORD debuggeePID,
    __out HANDLE* phStartupEvent);

EXTERN_C HRESULT 
EnumerateCLRs(DWORD debuggeePID, 
    __out HANDLE** ppHandleArrayOut,
    __out LPWSTR** ppStringArrayOut,
    __out DWORD* pdwArrayLengthOut);

EXTERN_C HRESULT 
CloseCLREnumeration(
    __in HANDLE* pHandleArray,
    __in LPWSTR* pStringArray,
    __in DWORD dwArrayLength);

EXTERN_C HRESULT 
CreateVersionStringFromModule(
    __in DWORD pidDebuggee,
    __in LPCWSTR szModuleName,
    __out_ecount_part(cchBuffer, *pdwLength) LPWSTR pBuffer,
    __in DWORD cchBuffer,
    __out DWORD* pdwLength);

EXTERN_C HRESULT 
CreateDebuggingInterfaceFromVersionEx(
    __in int iDebuggerVersion,
    __in LPCWSTR szDebuggeeVersion,
    __out IUnknown ** ppCordb);

EXTERN_C HRESULT 
CreateDebuggingInterfaceFromVersion(
    __in LPCWSTR szDebuggeeVersion, 
    __out IUnknown ** ppCordb);

