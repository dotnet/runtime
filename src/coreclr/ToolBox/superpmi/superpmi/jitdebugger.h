// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _JitDebugger
#define _JitDebugger

//
// Functions to support just-in-time debugging.
//

BOOL GetRegistryLongValue(HKEY    hKeyParent,              // Parent key.
                          LPCWSTR szKey,                   // Key name to look at.
                          LPCWSTR szName,                  // Name of value to get.
                          long*   pValue,                  // Put value here, if found.
                          BOOL    fReadNonVirtualizedKey); // Whether to read 64-bit hive on WOW64

HRESULT GetCurrentModuleFileName(_Out_writes_(*pcchBuffer) LPWSTR pBuffer, __inout DWORD* pcchBuffer);

#ifndef _WIN64
BOOL RunningInWow64();
#endif

BOOL    IsCurrentModuleFileNameInAutoExclusionList();
HRESULT GetDebuggerSettingInfoWorker(_Out_writes_to_opt_(*pcchDebuggerString, *pcchDebuggerString)
                                         LPWSTR wszDebuggerString,
                                     DWORD*     pcchDebuggerString,
                                     BOOL*      pfAuto);
void GetDebuggerSettingInfo(LPWSTR wszDebuggerString, DWORD cchDebuggerString, BOOL* pfAuto);

int DbgBreakCheck(const char* szFile, int iLine, const char* szExpr);

#endif // !_JitDebugger
