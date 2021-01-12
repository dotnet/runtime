// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// DbgShim.h
//
//*****************************************************************************

#include <windows.h>

typedef VOID (*PSTARTUP_CALLBACK)(IUnknown *pCordb, PVOID parameter, HRESULT hr);

EXTERN_C HRESULT
CreateProcessForLaunch(
    __in LPWSTR lpCommandLine,
    __in BOOL bSuspendProcess,
    __in LPVOID lpEnvironment,
    __in LPCWSTR lpCurrentDirectory,
    __out PDWORD pProcessId,
    __out HANDLE *pResumeHandle);

EXTERN_C HRESULT
ResumeProcess(
    __in HANDLE hResumeHandle);

EXTERN_C HRESULT
CloseResumeHandle(
    __in HANDLE hResumeHandle);

EXTERN_C HRESULT
RegisterForRuntimeStartup(
    __in DWORD dwProcessId,
    __in PSTARTUP_CALLBACK pfnCallback,
    __in PVOID parameter,
    __out PVOID *ppUnregisterToken);

EXTERN_C HRESULT
RegisterForRuntimeStartupEx(
    __in DWORD dwProcessId,
    __in LPCWSTR szApplicationGroupId,
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

EXTERN_C
DLLEXPORT
HRESULT
CreateDebuggingInterfaceFromVersion2(
    __in int iDebuggerVersion,
    __in LPCWSTR szDebuggeeVersion,
    __in LPCWSTR szApplicationGroupId,
    __out IUnknown ** ppCordb);

EXTERN_C HRESULT
CreateDebuggingInterfaceFromVersion(
    __in LPCWSTR szDebuggeeVersion,
    __out IUnknown ** ppCordb);
