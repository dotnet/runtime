// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>
#include <minipal/file.h>

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

    if ((path[0] == L'\0') || (path[1] == L'\0'))
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
    if (path[0] == L'\0')
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
#else // TARGET_WINDOWS

#include <sys/types.h>
#include <sys/stat.h>
#include <stdlib.h>
#include <minipal/utf8.h>
#include <minipal/strings.h>

#if HAVE_STAT64
#define stat_ stat64
#define fstat_ fstat64
#define lstat_ lstat64
#else /* HAVE_STAT64 */
#define stat_ stat
#define fstat_ fstat
#define lstat_ lstat
#endif  /* HAVE_STAT64 */

/* Magic number explanation:

   To 1970:
   Both epochs are Gregorian. 1970 - 1601 = 369. Assuming a leap
   year every four years, 369 / 4 = 92. However, 1700, 1800, and 1900
   were NOT leap years, so 89 leap years, 280 non-leap years.
   89 * 366 + 280 * 365 = 134774 days between epochs. Of course
   60 * 60 * 24 = 86400 seconds per day, so 134774 * 86400 =
   11644473600 = SECS_BETWEEN_1601_AND_1970_EPOCHS.

   To 2001:
   Again, both epochs are Gregorian. 2001 - 1601 = 400. Assuming a leap
   year every four years, 400 / 4 = 100. However, 1700, 1800, and 1900
   were NOT leap years (2000 was because it was divisible by 400), so
   97 leap years, 303 non-leap years.
   97 * 366 + 303 * 365 = 146097 days between epochs. 146097 * 86400 =
   12622780800 = SECS_BETWEEN_1601_AND_2001_EPOCHS.

   This result is also confirmed in the MSDN documentation on how
   to convert a time_t value to a win32 FILETIME.
*/
static const int64_t SECS_BETWEEN_1601_AND_1970_EPOCHS = 11644473600LL;
static const int64_t SECS_TO_100NS = 10000000; /* 10^7 */

static uint64_t UnixTimeToWin32FileTime(struct timespec ts)
{
    return (uint64_t)(((int64_t)ts.tv_sec + SECS_BETWEEN_1601_AND_1970_EPOCHS) * SECS_TO_100NS +
        (ts.tv_nsec / 100));
}

#endif // TARGET_WINDOWS

bool minipal_file_get_attributes_utf16(const char16_t* path, minipal_file_attr_t* attributes)
{
#ifdef TARGET_WINDOWS
    if (!path || !attributes)
        return false;

    WCHAR* extendedPath = NormalizePath(path);
    if (extendedPath)
        path = extendedPath;

    WIN32_FILE_ATTRIBUTE_DATA faData;
    bool ret = GetFileAttributesExW(path, GetFileExInfoStandard, &faData);

    if (ret)
    {
        attributes->size = ((uint64_t)faData.nFileSizeHigh << 32) | (uint64_t)faData.nFileSizeLow;
        attributes->lastWriteTime =  ((uint64_t)faData.ftLastWriteTime.dwHighDateTime << 32) | (uint64_t)faData.ftLastWriteTime.dwLowDateTime;
    }

    if (extendedPath)
        free(extendedPath);

    return ret;
#else // TARGET_WINDOWS
    if (!path || !attributes)
        return false;

    size_t u16Len = minipal_u16_strlen(path);
    size_t u8Len = minipal_get_length_utf16_to_utf8(path, u16Len, 0);
    char* u8Path = (char*)malloc(u8Len + 1);
    minipal_convert_utf16_to_utf8(path, u16Len, u8Path, u8Len + 1, 0);

    struct stat_ stat_data;
    bool ret = stat_(u8Path, &stat_data) == 0;

    if (ret)
    {
        attributes->size = stat_data.st_size;
        attributes->lastWriteTime = UnixTimeToWin32FileTime(stat_data.st_mtim);
    }

    free(u8Path);
    return ret;
#endif // TARGET_WINDOWS
}
