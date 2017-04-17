//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include "standardpch.h"
#include "verbmerge.h"
#include "simpletimer.h"
#include "logging.h"

// Do reads/writes in large 256MB chunks.
#define BUFFER_SIZE 0x10000000

// MergePathStrings: take two file system path components, compose them together, and return the merged pathname string.
// The caller must delete the returned string with delete[].
//
// static
char* verbMerge::MergePathStrings(const char* dir, const char* file)
{
    size_t dirlen  = strlen(dir);
    size_t filelen = strlen(file);
    size_t newlen  = dirlen + 1 /* slash */ + filelen + 1 /* null */;
    char*  newpath = new char[newlen];
    strcpy(newpath, dir);
    strcat(newpath, DIRECTORY_SEPARATOR_STR_A);
    strcat(newpath, file);
    return newpath;
}

// AppendFile: append the file named by 'fileName' to the output file referred to by 'hFileOut'. The 'hFileOut'
// handle is assumed to be open, and the file position is assumed to be at the correct spot for writing, to append.
//
// 'buffer' is memory that can be used to do reading/buffering.
//
// static
int verbMerge::AppendFile(HANDLE hFileOut, const char* fileName, unsigned char* buffer, size_t bufferSize)
{
    int result = 0; // default to zero == success

    LogInfo("Appending file '%s'", fileName);

    HANDLE hFileIn = CreateFileA(fileName, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING,
                                 FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (hFileIn == INVALID_HANDLE_VALUE)
    {
        LogError("Failed to open input file '%s'. GetLastError()=%u", fileName, GetLastError());
        return -1;
    }

    LARGE_INTEGER fileSize;
    if (GetFileSizeEx(hFileIn, &fileSize) == 0)
    {
        LogError("GetFileSizeEx on '%s' failed. GetLastError()=%u", fileName, GetLastError());
        result = -1;
        goto CLEAN_UP;
    }

    for (LONGLONG offset = 0; offset < fileSize.QuadPart; offset += bufferSize)
    {
        DWORD bytesRead = -1;
        BOOL  res       = ReadFile(hFileIn, buffer, (DWORD)bufferSize, &bytesRead, nullptr);
        if (!res)
        {
            LogError("Failed to read '%s' from offset %lld. GetLastError()=%u", fileName, offset, GetLastError());
            result = -1;
            goto CLEAN_UP;
        }
        DWORD bytesWritten = -1;
        BOOL  res2         = WriteFile(hFileOut, buffer, bytesRead, &bytesWritten, nullptr);
        if (!res2)
        {
            LogError("Failed to write output file at offset %lld. GetLastError()=%u", offset, GetLastError());
            result = -1;
            goto CLEAN_UP;
        }
        if (bytesRead != bytesWritten)
        {
            LogError("Failed to read/write matching bytes %u!=%u", bytesRead, bytesWritten);
            result = -1;
            goto CLEAN_UP;
        }
    }

CLEAN_UP:

    if (CloseHandle(hFileIn) == 0)
    {
        LogError("CloseHandle failed. GetLastError()=%u", GetLastError());
        result = -1;
    }

    return result;
}

// Return true if this is a directory
//
// static
bool verbMerge::DirectoryFilterDirectories(WIN32_FIND_DATAA* findData)
{
    if ((findData->dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) != 0)
    {
// It's a directory. See if we want to exclude it because of other reasons, such as:
// 1. reparse points: avoid the possibility of loops
// 2. system directories
// 3. hidden directories
// 4. "." or ".."

#ifndef FEATURE_PAL // FILE_ATTRIBUTE_REPARSE_POINT is not defined in the PAL
        if ((findData->dwFileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0)
            return false;
#endif // !FEATURE_PAL
        if ((findData->dwFileAttributes & FILE_ATTRIBUTE_SYSTEM) != 0)
            return false;
        if ((findData->dwFileAttributes & FILE_ATTRIBUTE_HIDDEN) != 0)
            return false;

        if (strcmp(findData->cFileName, ".") == 0)
            return false;
        if (strcmp(findData->cFileName, "..") == 0)
            return false;

        return true;
    }

    return false;
}

// Return true if this is a file.
//
// static
bool verbMerge::DirectoryFilterFile(WIN32_FIND_DATAA* findData)
{
    if ((findData->dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) == 0)
    {
        // This is not a directory, so it must be a file.
        return true;
    }

    return false;
}

// static
int __cdecl verbMerge::WIN32_FIND_DATAA_qsort_helper(const void* p1, const void* p2)
{
    const WIN32_FIND_DATAA* file1 = (WIN32_FIND_DATAA*)p1;
    const WIN32_FIND_DATAA* file2 = (WIN32_FIND_DATAA*)p2;
    return strcmp(file1->cFileName, file2->cFileName);
}

// Enumerate a directory for the files specified by "searchPattern". For each element in the directory,
// pass it to the filter function. If the filter returns true, we keep it, otherwise we ignore it. Return
// an array of information for the files that we kept, sorted by filename.
//
// Returns 0 on success, non-zero on failure.
// If success, fileArray and elemCount are set.
//
// static
int verbMerge::FilterDirectory(const char*                  searchPattern,
                               DirectoryFilterFunction_t    filter,
                               /* out */ WIN32_FIND_DATAA** ppFileArray,
                               int*                         pElemCount)
{
    // First, build up a list, then create an array and sort it after we know how many elements there are.
    struct findDataList
    {
        findDataList(WIN32_FIND_DATAA* newFindData, findDataList* newNext) : findData(*newFindData), next(newNext)
        {
        }

        static void DeleteList(findDataList* root)
        {
            for (findDataList* loop = root; loop != nullptr;)
            {
                findDataList* tmp = loop;
                loop              = loop->next;
                delete tmp;
            }
        }

        WIN32_FIND_DATAA findData;
        findDataList*    next;
    };

    WIN32_FIND_DATAA* retArray = nullptr;
    findDataList*     first    = nullptr;

    int result    = 0; // default to zero == success
    int elemCount = 0;

    // NOTE: this function only works on Windows 7 and later.
    WIN32_FIND_DATAA findData;
    HANDLE           hSearch;
#ifdef FEATURE_PAL
    // PAL doesn't have FindFirstFileEx(). So just use FindFirstFile(). The only reason we use
    // the Ex version is potentially better performance (don't populate short name; use large fetch),
    // not functionality.
    hSearch = FindFirstFileA(searchPattern, &findData);
#else  // !FEATURE_PAL
    hSearch = FindFirstFileExA(searchPattern,
                               FindExInfoBasic, // We don't care about the short names
                               &findData,
                               FindExSearchNameMatch, // standard name matching
                               NULL, FIND_FIRST_EX_LARGE_FETCH);
#endif // !FEATURE_PAL

    if (hSearch == INVALID_HANDLE_VALUE)
    {
        DWORD lastErr = GetLastError();
        if (lastErr == ERROR_FILE_NOT_FOUND)
        {
            // This is ok; there was just nothing matching the pattern.
        }
        else
        {
            LogError("Failed to find pattern '%s'. GetLastError()=%u", searchPattern, GetLastError());
        }
        goto CLEAN_UP;
    }

    while (true)
    {
        // Do something with findData...

        if (filter(&findData))
        {
            // Prepend it to the list.
            first = new findDataList(&findData, first);
            ++elemCount;
        }

        BOOL ok = FindNextFileA(hSearch, &findData);
        if (!ok)
        {
            DWORD err = GetLastError();
            if (err != ERROR_NO_MORE_FILES)
            {
                LogError("Failed to find next file. GetLastError()=%u", GetLastError());
                result = -1;
                goto CLEAN_UP;
            }
            break;
        }
    }

    // Now sort the list. Create an array to put everything in.

    int i;

    retArray = new WIN32_FIND_DATAA[elemCount];
    i        = 0;
    for (findDataList* tmp = first; tmp != nullptr; tmp = tmp->next)
    {
        retArray[i++] = tmp->findData;
    }

    qsort(retArray, elemCount, sizeof(retArray[0]), WIN32_FIND_DATAA_qsort_helper);

CLEAN_UP:

    findDataList::DeleteList(first);

    if ((hSearch != INVALID_HANDLE_VALUE) && !FindClose(hSearch))
    {
        LogError("Failed to close search handle. GetLastError()=%u", GetLastError());
        delete[] retArray;
        return -1;
    }

    *ppFileArray = retArray;
    *pElemCount  = elemCount;
    return result;
}

// Append all files in the given directory matching the file pattern.
//
// static
int verbMerge::AppendAllInDir(HANDLE              hFileOut,
                              const char*         dir,
                              const char*         file,
                              unsigned char*      buffer,
                              size_t              bufferSize,
                              bool                recursive,
                              /* out */ LONGLONG* size)
{
    int      result    = 0; // default to zero == success
    LONGLONG totalSize = 0;

    char* searchPattern = MergePathStrings(dir, file);

    WIN32_FIND_DATAA* fileArray = nullptr;
    int               elemCount = 0;
    result                      = FilterDirectory(searchPattern, DirectoryFilterFile, &fileArray, &elemCount);
    if (result != 0)
    {
        goto CLEAN_UP;
    }

    for (int i = 0; i < elemCount; i++)
    {
        const WIN32_FIND_DATAA& findData     = fileArray[i];
        char*                   fileFullPath = MergePathStrings(dir, findData.cFileName);

        // Is it zero length? If so, skip it.
        if ((findData.nFileSizeLow == 0) && (findData.nFileSizeHigh == 0))
        {
            LogInfo("Skipping zero-length file '%s'", fileFullPath);
        }
        else
        {
            result = AppendFile(hFileOut, fileFullPath, buffer, bufferSize);
            if (result != 0)
            {
                // Error was already logged.
                delete[] fileFullPath;
                goto CLEAN_UP;
            }
        }

        delete[] fileFullPath;
        totalSize += ((LONGLONG)findData.nFileSizeHigh << 32) + (LONGLONG)findData.nFileSizeLow;
    }

    // If we need to recurse, then search the directory again for directories, and recursively search each one.
    if (recursive)
    {
        delete[] searchPattern;
        delete[] fileArray;

        searchPattern = MergePathStrings(dir, "*");
        fileArray     = nullptr;
        elemCount     = 0;
        result        = FilterDirectory(searchPattern, DirectoryFilterDirectories, &fileArray, &elemCount);
        if (result != 0)
        {
            goto CLEAN_UP;
        }

        LONGLONG dirSize = 0;
        for (int i = 0; i < elemCount; i++)
        {
            const WIN32_FIND_DATAA& findData = fileArray[i];

            char* fileFullPath = MergePathStrings(dir, findData.cFileName);
            result             = AppendAllInDir(hFileOut, fileFullPath, file, buffer, bufferSize, recursive, &dirSize);
            delete[] fileFullPath;
            if (result != 0)
            {
                // Error was already logged.
                goto CLEAN_UP;
            }

            totalSize += dirSize;
        }
    }

CLEAN_UP:

    delete[] searchPattern;
    delete[] fileArray;

    if (result == 0)
    {
        *size = totalSize;
    }

    return result;
}

// Merge a set of .MC files into an output .MCH file. The .MC files to merge are given as a pattern, one of:
//      1. *.mc -- simple pattern. Assumes current directory.
//      2. foo\bar\*.mc -- simple pattern with relative directory.
//      3. c:\foo\bar\baz\*.mc -- simple pattern with full path.
// If no pattern is given, then the last component of the path is expected to be a directory name, and the pattern is
// assumed to be "*" (that is, all files).
//
// If "recursive" is true, then the pattern is searched for in the specified directory (or implicit current directory)
// and all sub-directories, recursively.
//
// static
int verbMerge::DoWork(const char* nameOfOutputFile, const char* pattern, bool recursive)
{
    int         result = 0; // default to zero == success
    SimpleTimer st1;

    LogInfo("Merging files matching '%s' into '%s'", pattern, nameOfOutputFile);

    HANDLE hFileOut = CreateFileA(nameOfOutputFile, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS,
                                  FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (hFileOut == INVALID_HANDLE_VALUE)
    {
        LogError("Failed to open output file '%s'. GetLastError()=%u", nameOfOutputFile, GetLastError());
        return -1;
    }

    // Create a buffer we can use for all the copies.
    unsigned char* buffer = new unsigned char[BUFFER_SIZE];
    char*          dir    = nullptr;
    const char*    file   = nullptr;

    dir             = _strdup(pattern);
    char* lastSlash = strrchr(dir, DIRECTORY_SEPARATOR_CHAR_A);
    if (lastSlash == NULL)
    {
        // The user may have passed a relative path without a slash, or the current directory.
        // If there is a wildcard, we use it as the file pattern. If there isn't, we assume it's a relative directory
        // name and use it as a directory, with "*" as the file pattern.
        const char* wildcard = strchr(dir, '*');
        if (wildcard == NULL)
        {
            file = "*";
        }
        else
        {
            file = dir;
            dir  = _strdup(".");
        }
    }
    else
    {
        const char* wildcard = strchr(lastSlash, '*');
        if (wildcard == NULL)
        {
            file = "*";

            // Minor canonicalization: if there is a trailing last slash, strip it (probably should do this in a
            // loop...)
            if (*(lastSlash + 1) == '\0')
            {
                *lastSlash = '\0';
            }
        }
        else
        {
            // ok, we found a wildcard after the last slash, so assume there is a pattern. Strip it at the last slash.
            *lastSlash = '\0';
            file       = lastSlash + 1;
        }
    }

    LONGLONG totalSize = 0;
    LONGLONG dirSize   = 0;

    st1.Start();

    result = AppendAllInDir(hFileOut, dir, file, buffer, BUFFER_SIZE, recursive, &dirSize);
    if (result != 0)
    {
        goto CLEAN_UP;
    }
    totalSize += dirSize;

    st1.Stop();

    LogInfo("Read/Wrote %lld MB @ %4.2f MB/s.", totalSize / (1000 * 1000),
            (((double)totalSize) / (1000 * 1000)) /
                st1.GetSeconds()); // yes yes.. http://en.wikipedia.org/wiki/Megabyte_per_second#Megabyte_per_second

CLEAN_UP:

    free((void*)dir);
    delete[] buffer;

    if (CloseHandle(hFileOut) == 0)
    {
        LogError("CloseHandle failed. GetLastError()=%u", GetLastError());
        result = -1;
    }

    if (result != 0)
    {
        // There was a failure. Delete the output file, to avoid leaving some half-created file.
        BOOL ok = DeleteFileA(nameOfOutputFile);
        if (!ok)
        {
            LogError("Failed to delete file after MCS /merge failed. GetLastError()=%u", GetLastError());
        }
    }

    return result;
}
