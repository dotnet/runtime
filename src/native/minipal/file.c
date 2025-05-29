// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <minipal/file.h>
#include <minipal/utf8.h>

#ifdef TARGET_WINDOWS
#include <Windows.h>

#define ExtendedPrefix L"\\\\?\\"
#define DevicePathPrefix L"\\\\.\\"
#define UNCExtendedPathPrefix L"\\\\?\\UNC\\"
#define UNCPathPrefix L"\\\\"
#define DIRECTORY_SEPARATOR_CHAR L'\\'
#define ALT_DIRECTORY_SEPARATOR_CHAR L'/'
#define VOLUME_SEPARATOR_CHAR L':'

static bool IsDirectorySeparator(WCHAR ch)
{
    return (ch == DIRECTORY_SEPARATOR_CHAR) || (ch == ALT_DIRECTORY_SEPARATOR_CHAR);
}

static bool IsPathNotFullyQualified(WCHAR* path)
{
    // Relative here means it could be relative to current directory on the relevant drive
    // NOTE: Relative segments ( \..\) are not considered relative
    // Returns true if the path specified is relative to the current drive or working directory.
    // Returns false if the path is fixed to a specific drive or UNC path.  This method does no
    // validation of the path (URIs will be returned as relative as a result).
    // Handles paths that use the alternate directory separator.  It is a frequent mistake to
    // assume that rooted paths (Path.IsPathRooted) are not relative.  This isn't the case.

    if ((path[0] == '\0') || (path[1] == '\0'))
    {
        return true;  // It isn't fixed, it must be relative.  There is no way to specify a fixed path with one character (or less).
    }

    if (IsDirectorySeparator(path[0]))
    {
        return !IsDirectorySeparator(path[1]); // There is no valid way to specify a relative path with two initial slashes
    }
    
    return !((wcslen(path) >= 3)           //The only way to specify a fixed path that doesn't begin with two slashes is the drive, colon, slash format- i.e. "C:\"
            && (path[1] == VOLUME_SEPARATOR_CHAR)
            && IsDirectorySeparator(path[2]));
}

// Returns newly allocated path if needs to expand the path, or NULL if no need.
// The normalization examples are :
//  C:\foo\<long>\bar   => \\?\C:\foo\<long>\bar
//  \\server\<long>\bar => \\?\UNC\server\<long>\bar
static WCHAR* NormalizePath(const WCHAR* path)
{
    if (path[0] == '\0')
        return NULL;

    if (wcsncmp(path, DevicePathPrefix, sizeof(DevicePathPrefix) / sizeof(WCHAR) - 1) == 0)
        return NULL;

    if (wcsncmp(path, ExtendedPrefix, sizeof(ExtendedPrefix) / sizeof(WCHAR) - 1) == 0)
        return NULL;

    if (wcsncmp(path, UNCExtendedPathPrefix, sizeof(UNCExtendedPathPrefix) / sizeof(WCHAR) - 1) == 0)
        return NULL;

    size_t length = wcslen(path);

    if (!IsPathNotFullyQualified(path) && length < MAX_PATH)
        return NULL;
    
    // Now the path will be normalized

    size_t prefixLength = sizeof(ExtendedPrefix) / sizeof(WCHAR) - 1;
    size_t bufferLength = length + prefixLength + 1;
    WCHAR* buffer = (WCHAR*)malloc(bufferLength * sizeof(WCHAR));

    if (!buffer)
        return NULL;

    DWORD retSize = GetFullPathNameW(path, (DWORD)(bufferLength - prefixLength), buffer + prefixLength, NULL);

    if (retSize > bufferLength - prefixLength)
    {
        free(buffer);
        bufferLength = retSize + prefixLength;
        buffer = (WCHAR*)malloc(bufferLength * sizeof(WCHAR));

        if (!buffer)
            return NULL;

        retSize = GetFullPathNameW(path, (DWORD)(bufferLength - prefixLength), buffer + prefixLength, NULL);
    }

    if (retSize <= 0)
    {
        free(buffer);
        return NULL;
    }

    memcpy(buffer, ExtendedPrefix, bufferLength * sizeof(WCHAR));

    return buffer;
}

#endif
