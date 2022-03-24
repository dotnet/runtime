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
    _In_ LPWSTR lpCommandLine,
    _In_ BOOL bSuspendProcess,
    _In_ LPVOID lpEnvironment,
    _In_ LPCWSTR lpCurrentDirectory,
    _Out_ PDWORD pProcessId,
    _Out_ HANDLE *pResumeHandle);

EXTERN_C HRESULT
ResumeProcess(
    _In_ HANDLE hResumeHandle);

EXTERN_C HRESULT
CloseResumeHandle(
    _In_ HANDLE hResumeHandle);

EXTERN_C HRESULT
RegisterForRuntimeStartup(
    _In_ DWORD dwProcessId,
    _In_ PSTARTUP_CALLBACK pfnCallback,
    _In_ PVOID parameter,
    _Out_ PVOID *ppUnregisterToken);

EXTERN_C HRESULT
RegisterForRuntimeStartupEx(
    _In_ DWORD dwProcessId,
    _In_ LPCWSTR szApplicationGroupId,
    _In_ PSTARTUP_CALLBACK pfnCallback,
    _In_ PVOID parameter,
    _Out_ PVOID *ppUnregisterToken);

EXTERN_C HRESULT
UnregisterForRuntimeStartup(
    _In_ PVOID pUnregisterToken);

EXTERN_C HRESULT
GetStartupNotificationEvent(
    _In_ DWORD debuggeePID,
    _Out_ HANDLE* phStartupEvent);

EXTERN_C HRESULT
EnumerateCLRs(DWORD debuggeePID,
    _Out_ HANDLE** ppHandleArrayOut,
    _Out_ LPWSTR** ppStringArrayOut,
    _Out_ DWORD* pdwArrayLengthOut);

EXTERN_C HRESULT
CloseCLREnumeration(
    _In_ HANDLE* pHandleArray,
    _In_ LPWSTR* pStringArray,
    _In_ DWORD dwArrayLength);

EXTERN_C HRESULT
CreateVersionStringFromModule(
    _In_ DWORD pidDebuggee,
    _In_ LPCWSTR szModuleName,
    _Out_writes_to_opt_(cchBuffer, *pdwLength) LPWSTR pBuffer,
    _In_ DWORD cchBuffer,
    _Out_ DWORD* pdwLength);

EXTERN_C HRESULT
CreateDebuggingInterfaceFromVersionEx(
    _In_ int iDebuggerVersion,
    _In_ LPCWSTR szDebuggeeVersion,
    _Out_ IUnknown ** ppCordb);

EXTERN_C
DLLEXPORT
HRESULT
CreateDebuggingInterfaceFromVersion2(
    _In_ int iDebuggerVersion,
    _In_ LPCWSTR szDebuggeeVersion,
    _In_ LPCWSTR szApplicationGroupId,
    _Out_ IUnknown ** ppCordb);

EXTERN_C HRESULT
CreateDebuggingInterfaceFromVersion(
    _In_ LPCWSTR szDebuggeeVersion,
    _Out_ IUnknown ** ppCordb);
