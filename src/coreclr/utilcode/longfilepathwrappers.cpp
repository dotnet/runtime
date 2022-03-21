// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"
#include "windows.h"
#include "longfilepathwrappers.h"
#include "sstring.h"
#include "ex.h"

class LongFile
{
private:
#ifdef HOST_WINDOWS
        static const WCHAR* ExtendedPrefix;
        static const WCHAR* DevicePathPrefix;
        static const WCHAR* UNCPathPrefix;
        static const WCHAR* UNCExtendedPathPrefix;
        static const WCHAR VolumeSeparatorChar;
		#define UNCPATHPREFIX W("\\\\")
#endif //HOST_WINDOWS
        static const WCHAR DirectorySeparatorChar;
        static const WCHAR AltDirectorySeparatorChar;
public:
        static BOOL ContainsDirectorySeparator(SString & path);
        static BOOL IsDirectorySeparator(WCHAR c);
        static BOOL IsPathNotFullyQualified(const SString & path);

        static HRESULT NormalizePath(SString& path);

#ifdef HOST_WINDOWS
        static BOOL IsExtended(const SString & path);
        static BOOL IsUNCExtended(const SString & path);
        static BOOL IsDevice(const SString & path);
        static void NormalizeDirectorySeparators(SString& path);
#endif
};

HMODULE
LoadLibraryExWrapper(
        LPCWSTR lpLibFileName,
        HANDLE hFile,
        DWORD dwFlags
        )
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    HRESULT hr   = S_OK;
    HMODULE ret = NULL;
    DWORD lastError = 0;

    EX_TRY
    {

        LongPathString path(LongPathString::Literal, lpLibFileName);

        if (LongFile::IsPathNotFullyQualified(path) || SUCCEEDED(LongFile::NormalizePath(path)))
        {
#ifdef HOST_WINDOWS
            //Adding the assert to ensure relative paths which are not just filenames are not used for LoadLibrary Calls
            _ASSERTE(!LongFile::IsPathNotFullyQualified(path) || !LongFile::ContainsDirectorySeparator(path));
            LongFile::NormalizeDirectorySeparators(path);
#endif //HOST_WINDOWS

            ret = LoadLibraryExW(path.GetUnicode(), hFile, dwFlags);
        }

        lastError = GetLastError();
    }
    EX_CATCH_HRESULT(hr);

    if (hr != S_OK)
    {
        SetLastError(hr);
    }
    else if(ret == NULL)
    {
        SetLastError(lastError);
    }

    return ret;
}

HANDLE
CreateFileWrapper(
        _In_ LPCWSTR lpFileName,
        _In_ DWORD dwDesiredAccess,
        _In_ DWORD dwShareMode,
        _In_opt_ LPSECURITY_ATTRIBUTES lpSecurityAttributes,
        _In_ DWORD dwCreationDisposition,
        _In_ DWORD dwFlagsAndAttributes,
        _In_opt_ HANDLE hTemplateFile
        )
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    DWORD lastError = 0;
    HANDLE ret = INVALID_HANDLE_VALUE;

    EX_TRY
    {
        LongPathString path(LongPathString::Literal, lpFileName);

        if (SUCCEEDED(LongFile::NormalizePath(path)))
        {
            ret = CreateFileW(path.GetUnicode(),
                    dwDesiredAccess,
                    dwShareMode,
                    lpSecurityAttributes,
                    dwCreationDisposition,
                    dwFlagsAndAttributes,
                    hTemplateFile);

        }

        lastError = GetLastError();
    }
    EX_CATCH_HRESULT(hr);

    if (hr != S_OK )
    {
        SetLastError(hr);
    }
    else if(ret == INVALID_HANDLE_VALUE)
    {
        SetLastError(lastError);
    }

    return ret;
}

DWORD
GetFileAttributesWrapper(
        _In_ LPCWSTR lpFileName
        )
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    DWORD  ret = INVALID_FILE_ATTRIBUTES;
    DWORD lastError = 0;

    EX_TRY
    {
        LongPathString path(LongPathString::Literal, lpFileName);

        if (SUCCEEDED(LongFile::NormalizePath(path)))
        {
            ret = GetFileAttributesW(
                    path.GetUnicode()
                );
        }

        lastError = GetLastError();
    }
    EX_CATCH_HRESULT(hr);

    if (hr != S_OK )
    {
        SetLastError(hr);
    }
    else if(ret == INVALID_FILE_ATTRIBUTES)
    {
        SetLastError(lastError);
    }

    return ret;
}

BOOL
GetFileAttributesExWrapper(
        _In_ LPCWSTR lpFileName,
        _In_ GET_FILEEX_INFO_LEVELS fInfoLevelId,
        _Out_writes_bytes_(sizeof(WIN32_FILE_ATTRIBUTE_DATA)) LPVOID lpFileInformation
        )
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    BOOL   ret = FALSE;
    DWORD lastError = 0;

    EX_TRY
    {
        LongPathString path(LongPathString::Literal, lpFileName);

        if (SUCCEEDED(LongFile::NormalizePath(path)))
        {
            ret = GetFileAttributesExW(
                    path.GetUnicode(),
                    fInfoLevelId,
                    lpFileInformation
                    );

        }

        lastError = GetLastError();
    }
    EX_CATCH_HRESULT(hr);

    if (hr != S_OK )
    {
        SetLastError(hr);
    }
    else if(ret == FALSE)
    {
        SetLastError(lastError);
    }

    return ret;
}

BOOL
DeleteFileWrapper(
        _In_ LPCWSTR lpFileName
        )
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    BOOL   ret = FALSE;
    DWORD lastError = 0;

    EX_TRY
    {
        LongPathString path(LongPathString::Literal, lpFileName);

        if (SUCCEEDED(LongFile::NormalizePath(path)))
        {
            ret = DeleteFileW(
                    path.GetUnicode()
                    );
        }

        lastError = GetLastError();
    }
    EX_CATCH_HRESULT(hr);

    if (hr != S_OK )
    {
        SetLastError(hr);
    }
    else if(ret == FALSE)
    {
        SetLastError(lastError);
    }

    return ret;
}

BOOL
MoveFileExWrapper(
        _In_     LPCWSTR lpExistingFileName,
        _In_opt_ LPCWSTR lpNewFileName,
        _In_     DWORD    dwFlags
        )
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    HRESULT hr  = S_OK;
    BOOL    ret = FALSE;
    DWORD lastError = 0;

    EX_TRY
    {
        LongPathString Existingpath(LongPathString::Literal, lpExistingFileName);
        LongPathString Newpath(LongPathString::Literal, lpNewFileName);

        if (SUCCEEDED(LongFile::NormalizePath(Existingpath)) && SUCCEEDED(LongFile::NormalizePath(Newpath)))
        {
            ret = MoveFileExW(
                    Existingpath.GetUnicode(),
                    Newpath.GetUnicode(),
                    dwFlags
                    );
        }

        lastError = GetLastError();
    }
    EX_CATCH_HRESULT(hr);

    if (hr != S_OK )
    {
        SetLastError(hr);
    }
    else if(ret == FALSE)
    {
        SetLastError(lastError);
    }

    return ret;

}

DWORD
SearchPathWrapper(
        _In_opt_ LPCWSTR lpPath,
        _In_ LPCWSTR lpFileName,
        _In_opt_ LPCWSTR lpExtension,
        _In_ BOOL getPath,
        SString& lpBuffer,
        _Out_opt_ LPWSTR * lpFilePart
        )
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    HRESULT hr  = S_OK;
    DWORD    ret = 0;
    DWORD lastError = 0;

    EX_TRY
    {
        LongPathString Existingpath(LongPathString::Literal, lpPath);

        if (lpPath != NULL)
        {
            if (FAILED(LongFile::NormalizePath(Existingpath)))
            {
                ret = FALSE;
            }
            else
            {
                lpPath = Existingpath.GetUnicode();
            }
        }

        if (!getPath)
        {
            ret = SearchPathW(
                    lpPath,
                    lpFileName,
                    lpExtension,
                    0,
                    NULL,
                    NULL
                    );
        }
        else
        {
            COUNT_T size = lpBuffer.GetUnicodeAllocation() + 1;

            ret = SearchPathW(
                    lpPath,
                    lpFileName,
                    lpExtension,
                    size,
                    lpBuffer.OpenUnicodeBuffer(size - 1),
                    lpFilePart
                    );

            if (ret > size)
            {
                lpBuffer.CloseBuffer();
                ret = SearchPathW(
                        lpPath,
                        lpFileName,
                        lpExtension,
                        ret,
                        lpBuffer.OpenUnicodeBuffer(ret - 1),
                        lpFilePart
                        );
            }

            lpBuffer.CloseBuffer(ret);

        }

        lastError = GetLastError();
    }
    EX_CATCH_HRESULT(hr);

    if (hr != S_OK)
    {
        SetLastError(hr);
    }
    else if (ret == 0)
    {
        SetLastError(lastError);
    }

    return ret;

}

DWORD
GetModuleFileNameWrapper(
    _In_opt_ HMODULE hModule,
    SString& buffer
    )
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    DWORD ret = 0;
    DWORD lastError = 0;

    EX_TRY
    {
        COUNT_T size = buffer.GetUnicodeAllocation() + 1;

        ret = GetModuleFileNameW(
            hModule,
            buffer.OpenUnicodeBuffer(size - 1),
            (DWORD)size
            );


        while (ret == size )
        {
            buffer.CloseBuffer();
            size = size * 2;
            ret = GetModuleFileNameW(
                hModule,
                buffer.OpenUnicodeBuffer(size - 1),
                (DWORD)size
                );

        }


        lastError = GetLastError();
        buffer.CloseBuffer(ret);
    }
    EX_CATCH_HRESULT(hr);

    if (hr != S_OK)
    {
        SetLastError(hr);
    }
    else if (ret == 0)
    {
        SetLastError(lastError);
    }

    return ret;
}

UINT WINAPI GetTempFileNameWrapper(
    _In_  LPCTSTR lpPathName,
    _In_  LPCTSTR lpPrefixString,
    _In_  UINT    uUnique,
    SString&  lpTempFileName
    )
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    UINT ret = 0;
    DWORD lastError = 0;

    EX_TRY
    {
        //Change the behaviour in Redstone to retry
        COUNT_T size = MAX_LONGPATH;
        WCHAR* buffer = lpTempFileName.OpenUnicodeBuffer(size - 1);
        ret  = GetTempFileNameW(
            lpPathName,
            lpPrefixString,
            uUnique,
            buffer
            );

        lastError = GetLastError();
        size = (COUNT_T)wcslen(buffer);
        lpTempFileName.CloseBuffer(size);

    }
    EX_CATCH_HRESULT(hr);

    if (hr != S_OK)
    {
        SetLastError(hr);
    }
    else if (ret == 0)
    {
        SetLastError(lastError);
    }

    return ret;
}
DWORD WINAPI GetTempPathWrapper(
    SString& lpBuffer
    )
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    DWORD ret = 0;
    DWORD lastError = 0;

    EX_TRY
    {
        //Change the behaviour in Redstone to retry
        COUNT_T size = MAX_LONGPATH;

        ret = GetTempPathW(
            size,
            lpBuffer.OpenUnicodeBuffer(size - 1)
            );

        lastError = GetLastError();
        lpBuffer.CloseBuffer(ret);
    }
    EX_CATCH_HRESULT(hr);

    if (hr != S_OK)
    {
        SetLastError(hr);
    }
    else if (ret == 0)
    {
        SetLastError(lastError);
    }

    return ret;
}

DWORD WINAPI GetCurrentDirectoryWrapper(
    SString&  lpBuffer
    )
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    DWORD ret = 0;
    DWORD lastError = 0;

    EX_TRY
    {
        //Change the behaviour in Redstone to retry
        COUNT_T size = MAX_LONGPATH;

        ret = GetCurrentDirectoryW(
            size,
            lpBuffer.OpenUnicodeBuffer(size - 1)
            );

        lastError = GetLastError();
        lpBuffer.CloseBuffer(ret);
    }
    EX_CATCH_HRESULT(hr);

    if (hr != S_OK)
    {
        SetLastError(hr);
    }
    else if (ret == 0)
    {
        SetLastError(lastError);
    }

    return ret;
}

DWORD WINAPI GetEnvironmentVariableWrapper(
    _In_opt_  LPCTSTR lpName,
    _Out_opt_ SString&  lpBuffer
    )
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    DWORD ret = 0;
    DWORD lastError = 0;

    EX_TRY
    {

        COUNT_T size = lpBuffer.GetUnicodeAllocation() + 1;

        ret = GetEnvironmentVariableW(
            lpName,
            lpBuffer.OpenUnicodeBuffer(size - 1),
            size
            );

        // We loop round getting the length of the env var and then trying to copy
        // the value into a the allocated buffer. Usually we'll go through this loop
        // precisely once, but the caution is ncessary in case the variable mutates
        // beneath us, as the environment variable can be modified by another thread
        //between two calls to GetEnvironmentVariableW

        while (ret > size)
        {
            size = ret;
            lpBuffer.CloseBuffer();
            ret = GetEnvironmentVariableW(
                lpName,
                lpBuffer.OpenUnicodeBuffer(size - 1),
                size);
        }

        lastError = GetLastError();
        lpBuffer.CloseBuffer(ret);
    }
    EX_CATCH_HRESULT(hr);

    if (hr != S_OK)
    {
        SetLastError(hr);
    }
    else if (ret == 0)
    {
        SetLastError(lastError);
    }

    return ret;
}


#ifdef HOST_WINDOWS

BOOL
CopyFileExWrapper(
        _In_        LPCWSTR lpExistingFileName,
        _In_        LPCWSTR lpNewFileName,
        _In_opt_    LPPROGRESS_ROUTINE lpProgressRoutine,
        _In_opt_    LPVOID lpData,
        _When_(pbCancel != NULL, _Pre_satisfies_(*pbCancel == FALSE))
        _Inout_opt_ LPBOOL pbCancel,
        _In_        DWORD dwCopyFlags
        )
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    HRESULT hr  = S_OK;
    BOOL    ret = FALSE;
    DWORD lastError = 0;

    EX_TRY
    {
        LongPathString Existingpath(LongPathString::Literal, lpExistingFileName);
        LongPathString Newpath(LongPathString::Literal, lpNewFileName);

        if (SUCCEEDED(LongFile::NormalizePath(Existingpath)) && SUCCEEDED(LongFile::NormalizePath(Newpath)))
        {
            ret = CopyFileExW(
                    Existingpath.GetUnicode(),
                    Newpath.GetUnicode(),
                    lpProgressRoutine,
                    lpData,
                    pbCancel,
                    dwCopyFlags
                    );
        }

        lastError = GetLastError();
    }
    EX_CATCH_HRESULT(hr);

    if (hr != S_OK )
    {
        SetLastError(hr);
    }
    else if(ret == FALSE)
    {
        SetLastError(lastError);
    }

    return ret;
}

HANDLE
FindFirstFileExWrapper(
        _In_ LPCWSTR lpFileName,
        _In_ FINDEX_INFO_LEVELS fInfoLevelId,
        _Out_writes_bytes_(sizeof(WIN32_FIND_DATAW)) LPVOID lpFindFileData,
        _In_ FINDEX_SEARCH_OPS fSearchOp,
        _Reserved_ LPVOID lpSearchFilter,
        _In_ DWORD dwAdditionalFlags
        )
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    HANDLE ret = INVALID_HANDLE_VALUE;
    DWORD lastError = 0;

    EX_TRY
    {
        LongPathString path(LongPathString::Literal, lpFileName);

        if (SUCCEEDED(LongFile::NormalizePath(path)))
        {
            ret = FindFirstFileExW(
                    path.GetUnicode(),
                    fInfoLevelId,
                    lpFindFileData,
                    fSearchOp,
                    lpSearchFilter,
                    dwAdditionalFlags
                    );
        }

        lastError = GetLastError();
    }
    EX_CATCH_HRESULT(hr);

    if (hr != S_OK )
    {
        SetLastError(hr);
    }
    else if(ret == INVALID_HANDLE_VALUE)
    {
        SetLastError(lastError);
    }

    return ret;
}
#endif // HOST_WINDOWS

//Implementation of LongFile Helpers
const WCHAR LongFile::DirectorySeparatorChar = W('\\');
const WCHAR LongFile::AltDirectorySeparatorChar = W('/');
#ifdef HOST_WINDOWS
const WCHAR LongFile::VolumeSeparatorChar = W(':');
const WCHAR* LongFile::ExtendedPrefix = W("\\\\?\\");
const WCHAR* LongFile::DevicePathPrefix = W("\\\\.\\");
const WCHAR* LongFile::UNCExtendedPathPrefix = W("\\\\?\\UNC\\");
const WCHAR* LongFile::UNCPathPrefix = UNCPATHPREFIX;

void LongFile::NormalizeDirectorySeparators(SString& path)
{
    for(SString::Iterator i = path.Begin(); i < path.End(); ++i)
    {
        if (*i == AltDirectorySeparatorChar)
        {
            path.Replace(i, DirectorySeparatorChar);
        }
    }
}

BOOL LongFile::IsExtended(const SString & path)
{
    return path.BeginsWith(SL(ExtendedPrefix));
}

BOOL LongFile::IsUNCExtended(const SString & path)
{
    return path.BeginsWith(SL(UNCExtendedPathPrefix));
}

// Relative here means it could be relative to current directory on the relevant drive
// NOTE: Relative segments ( \..\) are not considered relative
// Returns true if the path specified is relative to the current drive or working directory.
// Returns false if the path is fixed to a specific drive or UNC path.  This method does no
// validation of the path (URIs will be returned as relative as a result).
// Handles paths that use the alternate directory separator.  It is a frequent mistake to
// assume that rooted paths (Path.IsPathRooted) are not relative.  This isn't the case.

BOOL LongFile::IsPathNotFullyQualified(const SString & path)
{
    if (path.GetCount() < 2)
    {
        return TRUE;  // It isn't fixed, it must be relative.  There is no way to specify a fixed path with one character (or less).
    }

    if (IsDirectorySeparator(path[0]))
    {
        return !IsDirectorySeparator(path[1]); // There is no valid way to specify a relative path with two initial slashes
    }

    return !((path.GetCount() >= 3)           //The only way to specify a fixed path that doesn't begin with two slashes is the drive, colon, slash format- i.e. "C:\"
            && (path[1] == VolumeSeparatorChar)
            && IsDirectorySeparator(path[2]));
}

BOOL LongFile::IsDevice(const SString & path)
{
    return path.BeginsWith(SL(DevicePathPrefix));
}

// This function will normalize paths if the path length exceeds MAX_PATH
// The normalization examples are :
//  C:\foo\<long>\bar   => \\?\C:\foo\<long>\bar
//  \\server\<long>\bar => \\?\UNC\server\<long>\bar
HRESULT LongFile::NormalizePath(SString & path)
{
    HRESULT hr        = S_OK;
    DWORD   ret       = 0;
    COUNT_T prefixLen = 0;
    if (path.IsEmpty()|| IsDevice(path) || IsExtended(path) || IsUNCExtended(path))
        return S_OK;

    if (!IsPathNotFullyQualified(path) && path.GetCount() < MAX_LONGPATH)
        return S_OK;

    //Now the path will be normalized

    SString originalPath(path);
    SString prefix(ExtendedPrefix);
    prefixLen = prefix.GetCount();

    if (path.BeginsWith(SL(UNCPathPrefix)))
    {
        prefix.Set(UNCExtendedPathPrefix);
        //In this case if path is \\server the extended syntax should be like  \\?\UNC\server
        //The below logic populates the path from prefixLen offset from the start. This ensures that first 2 characters are overwritten
        //
        prefixLen = prefix.GetCount() - (COUNT_T)wcslen(UNCPATHPREFIX);
        _ASSERTE(prefixLen > 0 );
    }


    COUNT_T size  = path.GetUnicodeAllocation() + 1;
    WCHAR* buffer = path.OpenUnicodeBuffer(size - 1);

    ret = GetFullPathNameW(
        originalPath.GetUnicode(),
        size - prefixLen,        //memory avilable for path after reserving for prefix
        (buffer + prefixLen),    //reserve memory for prefix
        NULL
        );

    if (ret == 0)
    {
        return E_FAIL;
    }

    if (ret > size - prefixLen)
    {
        path.CloseBuffer();
        size   = ret + prefixLen;
        buffer = path.OpenUnicodeBuffer(size -1);

        ret = GetFullPathNameW(
            originalPath.GetUnicode(),
            ret,                   // memory required for the path
            (buffer + prefixLen),  //reserve memory for prefix
            NULL
            );

        _ASSERTE(ret < size - prefixLen);

        if (ret == 0)
        {
            return E_FAIL;
        }
    }

	SString fullpath(SString::Literal,buffer + prefixLen);

    //Check if the resolved path is a UNC. By default we assume relative path to resolve to disk
    if (fullpath.BeginsWith(SL(UNCPathPrefix)) && prefixLen != prefix.GetCount() - (COUNT_T)wcslen(UNCPATHPREFIX))
    {
        //Remove the leading '\\' from the UNC path to be replaced with UNCExtendedPathPrefix
        fullpath.Replace(fullpath.Begin(), (COUNT_T)wcslen(UNCPATHPREFIX), UNCExtendedPathPrefix);
        path.CloseBuffer();
        path.Set(fullpath);
    }
    else
    {
        //wcscpy_s always termintes with NULL, so we are saving the character that will be overwriiten
        WCHAR temp = buffer[prefix.GetCount()];
        wcscpy_s(buffer, prefix.GetCount() + 1, prefix.GetUnicode());
        buffer[prefix.GetCount()] = temp;
        path.CloseBuffer(ret + prefixLen);
    }

    return S_OK;
}
#else
BOOL LongFile::IsPathNotFullyQualified(const SString & path)
{
    return TRUE;
}

//Don't need to do anything For XPlat
HRESULT LongFile::NormalizePath(SString & path)
{
    return S_OK;
}
#endif //HOST_WINDOWS

BOOL LongFile::ContainsDirectorySeparator(SString & path)
{
    return path.Find(path.Begin(), DirectorySeparatorChar) || path.Find(path.Begin(), AltDirectorySeparatorChar);
}

BOOL LongFile::IsDirectorySeparator(WCHAR c)
{
    return c == DirectorySeparatorChar || c == AltDirectorySeparatorChar;
}



