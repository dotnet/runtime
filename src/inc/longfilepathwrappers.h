// Licensed to the .NET Foundation under one or more agreements. 
// The .NET Foundation licenses this file to you under the MIT license. 
// See the LICENSE file in the project root for more information. 

#ifndef _WIN_PATH_APIS_WRAPPER_
#define _WIN_PATH_APIS_WRAPPER_
class SString;

HMODULE
LoadLibraryExWrapper(
    _In_ LPCWSTR lpLibFileName,
    _Reserved_ HANDLE hFile = NULL,
    _In_ DWORD dwFlags = 0
    );

HANDLE
CreateFileWrapper(
    _In_ LPCWSTR lpFileName,
    _In_ DWORD dwDesiredAccess,
    _In_ DWORD dwShareMode,
    _In_opt_ LPSECURITY_ATTRIBUTES lpSecurityAttributes,
    _In_ DWORD dwCreationDisposition,
    _In_ DWORD dwFlagsAndAttributes,
    _In_opt_ HANDLE hTemplateFile
    );

BOOL
SetFileAttributesWrapper(
    _In_ LPCWSTR lpFileName,
    _In_ DWORD dwFileAttributes
    );

DWORD
GetFileAttributesWrapper(
    _In_ LPCWSTR lpFileName
    );

BOOL
GetFileAttributesExWrapper(
    _In_ LPCWSTR lpFileName,
    _In_ GET_FILEEX_INFO_LEVELS fInfoLevelId,
    _Out_writes_bytes_(sizeof(WIN32_FILE_ATTRIBUTE_DATA)) LPVOID lpFileInformation
    );
BOOL
DeleteFileWrapper(
    _In_ LPCWSTR lpFileName
    );

HANDLE
FindFirstFileExWrapper(
    _In_ LPCWSTR lpFileName,
    _In_ FINDEX_INFO_LEVELS fInfoLevelId,
    _Out_writes_bytes_(sizeof(WIN32_FIND_DATAW)) LPVOID lpFindFileData,
    _In_ FINDEX_SEARCH_OPS fSearchOp,
    _Reserved_ LPVOID lpSearchFilter,
    _In_ DWORD dwAdditionalFlags
    );

BOOL
CopyFileWrapper(
    _In_ LPCWSTR lpExistingFileName,
    _In_ LPCWSTR lpNewFileName,
    _In_ BOOL bFailIfExists
    );

#ifndef FEATURE_PAL
BOOL
CopyFileExWrapper(
    _In_        LPCWSTR lpExistingFileName,
    _In_        LPCWSTR lpNewFileName,
    _In_opt_    LPPROGRESS_ROUTINE lpProgressRoutine,
    _In_opt_    LPVOID lpData,
    _When_(pbCancel != NULL, _Pre_satisfies_(*pbCancel == FALSE))
    _Inout_opt_ LPBOOL pbCancel,
    _In_        DWORD dwCopyFlags
    );
#endif //FEATURE_PAL

BOOL
MoveFileExWrapper(
    _In_     LPCWSTR lpExistingFileName,
    _In_opt_ LPCWSTR lpNewFileName,
    _In_     DWORD    dwFlags
    );

BOOL
CreateDirectoryWrapper(
    _In_ LPCWSTR lpPathName,
    _In_opt_ LPSECURITY_ATTRIBUTES lpSecurityAttributes
    );

BOOL
RemoveDirectoryWrapper(
    _In_ LPCWSTR lpPathName
    );

BOOL
CreateHardLinkWrapper(
    _In_       LPCWSTR lpFileName,
    _In_       LPCWSTR lpExistingFileName,
    _Reserved_ LPSECURITY_ATTRIBUTES lpSecurityAttributes
    );

DWORD
SearchPathWrapper(
    _In_opt_ LPCWSTR lpPath,
    _In_ LPCWSTR lpFileName,
    _In_opt_ LPCWSTR lpExtension,
    _In_ BOOL getPath,
    SString& lpBuffer,
    _Out_opt_ LPWSTR * lpFilePart
    );


DWORD
GetShortPathNameWrapper(
    _In_ LPCWSTR lpszLongPath,
    SString& lpszShortPath
    );

DWORD
GetLongPathNameWrapper(
    _In_ LPCWSTR lpszShortPath,
    SString& lpszLongPath
    );

UINT WINAPI GetTempFileNameWrapper(
    _In_  LPCTSTR lpPathName,
    _In_  LPCTSTR lpPrefixString,
    _In_  UINT    uUnique,
    SString&  lpTempFileName
    );

DWORD WINAPI GetTempPathWrapper(
    SString& lpBuffer
    );

DWORD WINAPI GetCurrentDirectoryWrapper(
    SString&  lpBuffer
    );

DWORD
GetModuleFileNameWrapper(
    _In_opt_ HMODULE hModule,
    SString& buffer
    );

DWORD WINAPI GetEnvironmentVariableWrapper(
    _In_opt_  LPCTSTR lpName,
    _Out_opt_ SString&  lpBuffer
    );

BOOL PAL_GetPALDirectoryWrapper(SString& pbuffer);

#endif //_WIN_PATH_APIS_WRAPPER_

