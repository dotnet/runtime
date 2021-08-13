// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// DbgShim.h
//
//*****************************************************************************

#include <mono/utils/mono-publib.h>

#if defined(TARGET_WINDOWS)
#include <windows.h>
#include <libloaderapi.h>
#endif

#include <unknwn.h>

typedef VOID (*PSTARTUP_CALLBACK)(IUnknown *pCordb, PVOID parameter, HRESULT hr);


MONO_API HRESULT
CreateProcessForLaunch(
    __in LPWSTR lpCommandLine,
    __in BOOL bSuspendProcess,
    __in LPVOID lpEnvironment,
    __in LPCWSTR lpCurrentDirectory,
    __out PDWORD pProcessId,
    __out HANDLE *pResumeHandle);

MONO_API HRESULT
ResumeProcess(
    __in HANDLE hResumeHandle);

MONO_API HRESULT
CloseResumeHandle(
    __in HANDLE hResumeHandle);

MONO_API HRESULT
RegisterForRuntimeStartup(
    __in DWORD dwProcessId,
    __in PSTARTUP_CALLBACK pfnCallback,
    __in PVOID parameter,
    __out PVOID *ppUnregisterToken);

MONO_API HRESULT
RegisterForRuntimeStartupEx(
    __in DWORD dwProcessId,
    __in LPCWSTR szApplicationGroupId,
    __in PSTARTUP_CALLBACK pfnCallback,
    __in PVOID parameter,
    __out PVOID *ppUnregisterToken);

MONO_API HRESULT
UnregisterForRuntimeStartup(
    __in PVOID pUnregisterToken);

MONO_API HRESULT
GetStartupNotificationEvent(
    __in DWORD debuggeePID,
    __out HANDLE* phStartupEvent);

MONO_API HRESULT
EnumerateCLRs(DWORD debuggeePID,
    __out HANDLE** ppHandleArrayOut,
    __out LPWSTR** ppStringArrayOut,
    __out DWORD* pdwArrayLengthOut);

MONO_API HRESULT
CloseCLREnumeration(
    __in HANDLE* pHandleArray,
    __in LPWSTR* pStringArray,
    __in DWORD dwArrayLength);

MONO_API HRESULT
CreateVersionStringFromModule(
    __in DWORD pidDebuggee,
    __in LPCWSTR szModuleName,
    __out_ecount_part(cchBuffer, *pdwLength) LPWSTR pBuffer,
    __in DWORD cchBuffer,
    __out DWORD* pdwLength);

MONO_API HRESULT
CreateDebuggingInterfaceFromVersionEx(
    __in int iDebuggerVersion,
    __in LPCWSTR szDebuggeeVersion,
    __out IUnknown ** ppCordb);

MONO_API
HRESULT
CreateDebuggingInterfaceFromVersion2(
    __in int iDebuggerVersion,
    __in LPCWSTR szDebuggeeVersion,
    __in LPCWSTR szApplicationGroupId,
    __out IUnknown ** ppCordb);

MONO_API HRESULT
CreateDebuggingInterfaceFromVersion(
    __in LPCWSTR szDebuggeeVersion,
    __out IUnknown ** ppCordb);
