// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "verbmerge.h"
#include "simpletimer.h"
#include "logging.h"
#include <stdio.h>

// Do reads/writes in large 256MB chunks.
#define BUFFER_SIZE 0x10000000

// static member
RemoveDup verbMerge::m_removeDups;

// MergePathStrings: take two file system path components, compose them together, and return the merged pathname string.
// The caller must delete the returned string with delete[].
//
// static
LPWSTR verbMerge::MergePathStrings(LPCWSTR dir, LPCWSTR file)
{
    size_t dirlen  = wcslen(dir);
    size_t filelen = wcslen(file);
    size_t newlen  = dirlen + 1 /* slash */ + filelen + 1 /* null */;
    LPWSTR newpath = new WCHAR[newlen];
    wcscpy(newpath, dir);
    wcscat(newpath, DIRECTORY_SEPARATOR_STR_W);
    wcscat(newpath, file);
    return newpath;
}

char* verbMerge::ConvertWideCharToMultiByte(LPCWSTR wstr)
{
    unsigned int codePage   = CP_UTF8;
    int          sizeNeeded = WideCharToMultiByte(codePage, 0, wstr, -1, NULL, 0, NULL, NULL);
    char*        encodedStr = new char[sizeNeeded];
    WideCharToMultiByte(codePage, 0, wstr, -1, encodedStr, sizeNeeded, NULL, NULL);
    return encodedStr;
}

// AppendFileRaw: append the file named by 'fileFullPath' to the output file referred to by 'hFileOut'. The 'hFileOut'
// handle is assumed to be open, and the file position is assumed to be at the correct spot for writing, to append.
// The file is simply appended.
//
// 'buffer' is memory that can be used to do reading/buffering.
//
// static
int verbMerge::AppendFileRaw(HANDLE hFileOut, LPCWSTR fileFullPath, unsigned char* buffer, size_t bufferSize)
{
    int result = 0; // default to zero == success

    char* fileNameAsChar = ConvertWideCharToMultiByte(fileFullPath);
    LogInfo("Appending file '%s'", fileNameAsChar);

    HANDLE hFileIn = CreateFileW(fileFullPath, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING,
                                 FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (hFileIn == INVALID_HANDLE_VALUE)
    {
        // If you use a relative path, you can get GetLastError()==3, if the absolute path is longer
        // than MAX_PATH.
        LogError("Failed to open input file '%s'. GetLastError()=%u", fileNameAsChar, GetLastError());
        return -1;
    }

    LARGE_INTEGER fileSize;
    if (GetFileSizeEx(hFileIn, &fileSize) == 0)
    {
        LogError("GetFileSizeEx on '%s' failed. GetLastError()=%u", fileNameAsChar, GetLastError());
        result = -1;
        goto CLEAN_UP;
    }

    for (LONGLONG offset = 0; offset < fileSize.QuadPart; offset += bufferSize)
    {
        DWORD bytesRead = -1;
        BOOL  res       = ReadFile(hFileIn, buffer, (DWORD)bufferSize, &bytesRead, nullptr);
        if (!res)
        {
            LogError("Failed to read '%s' from offset %lld. GetLastError()=%u", fileNameAsChar, offset, GetLastError());
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

    delete[] fileNameAsChar;

    if (CloseHandle(hFileIn) == 0)
    {
        LogError("CloseHandle failed. GetLastError()=%u", GetLastError());
        result = -1;
    }

    return result;
}

// AppendFile: append the file named by 'fileFullPath' to the output file referred to by 'hFileOut'. The 'hFileOut'
// handle is assumed to be open, and the file position is assumed to be at the correct spot for writing, to append.
//
// 'buffer' is memory that can be used to do reading/buffering.
//
// static
int verbMerge::AppendFile(HANDLE hFileOut, LPCWSTR fileFullPath, bool dedup, unsigned char* buffer, size_t bufferSize)
{
    int result = 0; // default to zero == success

    if (dedup)
    {
        // Need to conver the fileFullPath to non-Unicode.
        char* fileFullPathAsChar = ConvertWideCharToMultiByte(fileFullPath);
        LogInfo("Appending file '%s'", fileFullPathAsChar);
        bool ok = m_removeDups.CopyAndRemoveDups(fileFullPathAsChar, hFileOut);
        delete[] fileFullPathAsChar;
        if (!ok)
        {
            LogError("Failed to remove dups");
            return -1;
        }
    }
    else
    {
        result = AppendFileRaw(hFileOut, fileFullPath, buffer, bufferSize);
    }

    return result;
}

// Return true if this is a directory
//
// static
bool verbMerge::DirectoryFilterDirectories(WIN32_FIND_DATAW* findData)
{
    if ((findData->dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) != 0)
    {
// It's a directory. See if we want to exclude it because of other reasons, such as:
// 1. reparse points: avoid the possibility of loops
// 2. system directories
// 3. hidden directories
// 4. "." or ".."

#ifndef TARGET_UNIX // FILE_ATTRIBUTE_REPARSE_POINT is not defined in the PAL
        if ((findData->dwFileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0)
            return false;
#endif // !TARGET_UNIX
        if ((findData->dwFileAttributes & FILE_ATTRIBUTE_SYSTEM) != 0)
            return false;
        if ((findData->dwFileAttributes & FILE_ATTRIBUTE_HIDDEN) != 0)
            return false;

        if (wcscmp(findData->cFileName, W(".")) == 0)
            return false;
        if (wcscmp(findData->cFileName, W("..")) == 0)
            return false;

        return true;
    }

    return false;
}

// Return true if this is a file.
//
// static
bool verbMerge::DirectoryFilterFile(WIN32_FIND_DATAW* findData)
{
    if ((findData->dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) == 0)
    {
        // This is not a directory, so it must be a file.
        return true;
    }

    return false;
}

// static
int __cdecl verbMerge::WIN32_FIND_DATAW_qsort_helper(const void* p1, const void* p2)
{
    const WIN32_FIND_DATAW* file1 = (WIN32_FIND_DATAW*)p1;
    const WIN32_FIND_DATAW* file2 = (WIN32_FIND_DATAW*)p2;
    return wcscmp(file1->cFileName, file2->cFileName);
}

// Enumerate a directory for the files specified by "searchPattern". For each element in the directory,
// pass it to the filter function. If the filter returns true, we keep it, otherwise we ignore it. Return
// an array of information for the files that we kept, sorted by filename.
//
// Returns 0 on success, non-zero on failure.
// If success, fileArray and elemCount are set.
//
// static
int verbMerge::FilterDirectory(LPCWSTR                      searchPattern,
                               DirectoryFilterFunction_t    filter,
                               /* out */ WIN32_FIND_DATAW** ppFileArray,
                               int*                         pElemCount)
{
    // First, build up a list, then create an array and sort it after we know how many elements there are.
    struct findDataList
    {
        findDataList(WIN32_FIND_DATAW* newFindData, findDataList* newNext) : findData(*newFindData), next(newNext)
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

        WIN32_FIND_DATAW findData;
        findDataList*    next;
    };

    WIN32_FIND_DATAW* retArray = nullptr;
    findDataList*     first    = nullptr;

    int result    = 0; // default to zero == success
    int elemCount = 0;

    // NOTE: this function only works on Windows 7 and later.
    WIN32_FIND_DATAW findData;
    HANDLE           hSearch;
#ifdef TARGET_UNIX
    // PAL doesn't have FindFirstFileEx(). So just use FindFirstFile(). The only reason we use
    // the Ex version is potentially better performance (don't populate short name; use large fetch),
    // not functionality.
    hSearch = FindFirstFileW(searchPattern, &findData);
#else  // !TARGET_UNIX
    hSearch = FindFirstFileExW(searchPattern,
                               FindExInfoBasic, // We don't care about the short names
                               &findData,
                               FindExSearchNameMatch, // standard name matching
                               NULL, FIND_FIRST_EX_LARGE_FETCH);
#endif // !TARGET_UNIX

    if (hSearch == INVALID_HANDLE_VALUE)
    {
        DWORD lastErr = GetLastError();
        if (lastErr == ERROR_FILE_NOT_FOUND)
        {
            // This is ok; there was just nothing matching the pattern.
        }
        else
        {
            LogError("Failed to find pattern '%S'. GetLastError()=%u", searchPattern, GetLastError());
            result = -1;
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

        BOOL ok = FindNextFileW(hSearch, &findData);
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

    retArray = new WIN32_FIND_DATAW[elemCount];
    i        = 0;
    for (findDataList* tmp = first; tmp != nullptr; tmp = tmp->next)
    {
        retArray[i++] = tmp->findData;
    }

    qsort(retArray, elemCount, sizeof(retArray[0]), WIN32_FIND_DATAW_qsort_helper);

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
                              LPCWSTR             dir,
                              LPCWSTR             file,
                              unsigned char*      buffer,
                              size_t              bufferSize,
                              bool                recursive,
                              bool                dedup,
                              /* out */ LONGLONG* size)
{
    int      result    = 0; // default to zero == success
    LONGLONG totalSize = 0;

    LPWSTR searchPattern = MergePathStrings(dir, file);

    _WIN32_FIND_DATAW* fileArray = nullptr;
    int                elemCount = 0;
    result                       = FilterDirectory(searchPattern, DirectoryFilterFile, &fileArray, &elemCount);
    if (result != 0)
    {
        goto CLEAN_UP;
    }

    for (int i = 0; i < elemCount; i++)
    {
        const _WIN32_FIND_DATAW& findData     = fileArray[i];
        LPWSTR                   fileFullPath = MergePathStrings(dir, findData.cFileName);

        if (wcslen(fileFullPath) > MAX_PATH) // This path is too long, use \\?\ to access it.
        {
            if (wcscmp(dir, W(".")) == 0)
            {
                LogError("can't access the relative path with UNC");
                goto CLEAN_UP;
            }
            LPWSTR newBuffer = new WCHAR[wcslen(fileFullPath) + 30];
            wcscpy(newBuffer, W("\\\\?\\"));
            if (*fileFullPath == '\\') // It is UNC path, use \\?\UNC\serverName to access it.
            {
                LPWSTR serverName = fileFullPath;
                wcscat(newBuffer, W("UNC\\"));
                while (*serverName == '\\')
                {
                    serverName++;
                }
                wcscat(newBuffer, serverName);
            }
            else
            {
                wcscat(newBuffer, fileFullPath);
            }
            delete[] fileFullPath;

            fileFullPath = newBuffer;
        }

        // Is it zero length? If so, skip it.
        if ((findData.nFileSizeLow == 0) && (findData.nFileSizeHigh == 0))
        {
            char* fileFullPathAsChar = ConvertWideCharToMultiByte(fileFullPath);
            LogInfo("Skipping zero-length file '%s'", fileFullPathAsChar);
            delete[] fileFullPathAsChar;
        }
        else
        {
            result = AppendFile(hFileOut, fileFullPath, dedup, buffer, bufferSize);
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

        searchPattern = MergePathStrings(dir, W("*"));
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
            const _WIN32_FIND_DATAW& findData = fileArray[i];

            LPWSTR fileFullPath = MergePathStrings(dir, findData.cFileName);
            result              = AppendAllInDir(hFileOut, fileFullPath, file, buffer, bufferSize, recursive, dedup, &dirSize);
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
// If "dedup" is true, the we remove duplicates while we are merging. If "stripCR" is also true, we remove CompileResults
// while deduplicating.
//
// static
int verbMerge::DoWork(const char* nameOfOutputFile, const char* pattern, bool recursive, bool dedup, bool stripCR)
{
    int         result = 0; // default to zero == success
    SimpleTimer st1;

    LogInfo("Merging files matching '%s' into '%s'", pattern, nameOfOutputFile);

    if (dedup)
    {
        // Initialize the deduplication object
        if (!m_removeDups.Initialize(stripCR, /* legacyCompare */ false, /* cleanup */ false))
        {
            LogError("Failed to initialize the deduplicator");
            return -1;
        }
    }

    int    nameLength              = (int)strlen(nameOfOutputFile) + 1;
    LPWSTR nameOfOutputFileAsWchar = new WCHAR[nameLength];
    MultiByteToWideChar(CP_ACP, 0, nameOfOutputFile, -1, nameOfOutputFileAsWchar, nameLength);

    int    patternLength  = (int)strlen(pattern) + 1;
    LPWSTR patternAsWchar = new WCHAR[patternLength];
    MultiByteToWideChar(CP_ACP, 0, pattern, -1, patternAsWchar, patternLength);

    HANDLE hFileOut = CreateFileW(nameOfOutputFileAsWchar, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS,
                                  FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (hFileOut == INVALID_HANDLE_VALUE)
    {
        LogError("Failed to open output file '%s'. GetLastError()=%u", nameOfOutputFile, GetLastError());
        return -1;
    }

    // Create a buffer we can use for all the copies.
    unsigned char* buffer = new unsigned char[BUFFER_SIZE];
    LPCWSTR        dir    = nullptr;
    LPCWSTR        file   = nullptr;

    LPWSTR lastSlash = wcsrchr(patternAsWchar, DIRECTORY_SEPARATOR_CHAR_A);
    if (lastSlash == NULL)
    {
        // The user may have passed a relative path without a slash, or the current directory.
        // If there is a wildcard, we use it as the file pattern. If there isn't, we assume it's a relative directory
        // name and use it as a directory, with "*" as the file pattern.
        LPCWSTR wildcard = wcschr(patternAsWchar, '*');
        if (wildcard == NULL)
        {
            file = W("*");
            dir  = patternAsWchar;
        }
        else
        {
            file = patternAsWchar;
            dir  = W(".");
        }
    }
    else
    {
        dir              = patternAsWchar;
        LPCWSTR wildcard = wcschr(lastSlash, '*');
        if (wildcard == NULL)
        {
            file = W("*");

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

    result = AppendAllInDir(hFileOut, dir, file, buffer, BUFFER_SIZE, recursive, dedup, &dirSize);
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

    delete[] patternAsWchar;
    delete[] buffer;

    if (CloseHandle(hFileOut) == 0)
    {
        LogError("CloseHandle failed. GetLastError()=%u", GetLastError());
        result = -1;
    }

    if (result != 0)
    {
        // There was a failure. Delete the output file, to avoid leaving some half-created file.
        int st = remove(nameOfOutputFile);
        if (st != 0)
        {
            LogError("Failed to delete file after MCS /merge failed. GetLastError()=%u", GetLastError());
        }
    }
    else
    {
        if (totalSize == 0)
        {
            // If the total size of merged files is zero, or there were no files found to merge,
            // then there was some problem.
            LogError("No files found to merge.");
            result = 1;
        }
    }

    delete[] nameOfOutputFileAsWchar;

    return result;
}
