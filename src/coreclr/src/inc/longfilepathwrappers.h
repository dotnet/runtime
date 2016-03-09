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

UINT GetTempFileNameWrapper(
    _In_  LPCTSTR lpPathName,
    _In_  LPCTSTR lpPrefixString,
    _In_  UINT    uUnique,
    SString&  lpTempFileName
    );

DWORD GetTempPathWrapper(
    SString& lpBuffer
    );

DWORD GetCurrentDirectoryWrapper(
    SString&  lpBuffer
    );

DWORD
GetModuleFileNameWrapper(
    _In_opt_ HMODULE hModule,
    SString& buffer
    );

DWORD GetEnvironmentVariableWrapper(
    _In_opt_  LPCTSTR lpName,
    _Out_opt_ SString&  lpBuffer
    );

BOOL PAL_GetPALDirectoryWrapper(SString& pbuffer);

#ifndef FEATURE_CORECLR
//Temporarily providing direct OS Calls Till All of the Desktop CLR start using the above format
inline DWORD
SearchPathWrapper(
    _In_opt_ LPCWSTR lpPath,
    _In_ LPCWSTR lpFileName,
    _In_opt_ LPCWSTR lpExtension,
    _In_ BOOL getPath,
    _Out_     LPWSTR  lpBuffer,
    _Out_opt_ LPWSTR * lpFilePart
    )
{
    return SearchPathW(
        lpPath,
        lpFileName,
        lpExtension,
        getPath,
        lpBuffer,
        lpFilePart
        );
}


inline DWORD
GetShortPathNameWrapper(
    _In_ LPCWSTR lpszLongPath,
    _Out_ LPWSTR  lpszShortPath,
    _In_  DWORD   cchBuffer
    )
{
    return GetShortPathNameW(
        lpszLongPath,
        lpszShortPath,
        cchBuffer
        );
}

inline DWORD
GetLongPathNameWrapper(
    _In_ LPCWSTR lpszShortPath,
    _Out_ LPWSTR  lpszLongPath,
    _In_  DWORD   cchBuffer
    )
{
    return GetLongPathNameW(
        lpszShortPath,
        lpszLongPath,
        cchBuffer
        );
}

inline UINT GetTempFileNameWrapper(
    _In_  LPCWSTR lpPathName,
    _In_  LPCWSTR lpPrefixString,
    _In_  UINT    uUnique,
    _Out_ LPWSTR  lpTempFileName
    )
{
    return GetTempFileNameW(
        lpPathName,
        lpPrefixString,
        uUnique,
        lpTempFileName
        );
}

inline DWORD GetTempPathWrapper(
    _In_  DWORD  nBufferLength,
    _Out_ LPWSTR lpBuffer
    )
{
    return GetTempPathW(
        nBufferLength,
        lpBuffer
        );
}

inline DWORD GetCurrentDirectoryWrapper(
    _In_  DWORD  nBufferLength,
    _Out_ LPWSTR lpBuffer
    )
{
    return GetCurrentDirectoryW(
        nBufferLength,
        lpBuffer
        );
}

inline DWORD
GetModuleFileNameWrapper(
    _In_opt_ HMODULE hModule,
    _Out_    LPWSTR  lpFilename,
    _In_     DWORD   nSize
    )
{
    return GetModuleFileNameW(
        hModule,
        lpFilename,
        nSize
        );
}

inline DWORD GetEnvironmentVariableWrapper(
    _In_opt_  LPCWSTR lpName,
    _Out_opt_ LPWSTR  lpBuffer,
    _In_      DWORD   nSize
    )
{
    return GetEnvironmentVariableW(
       lpName,
       lpBuffer,
       nSize
        );
}
#endif //FEATURE_CORECLR
#endif //_WIN_PATH_APIS_WRAPPER_

