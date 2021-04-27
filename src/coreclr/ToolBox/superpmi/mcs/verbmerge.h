// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// verbMerge.h - verb that merges multiple .MC into one .MCH file
// It allows .MC names to be Unicode names with long paths.
//----------------------------------------------------------
#ifndef _verbMerge
#define _verbMerge

#include "removedup.h"

class verbMerge
{
public:
    static int DoWork(const char* nameOfOutputFile, const char* pattern, bool recursive, bool dedup, bool stripCR);

private:
    typedef bool (*DirectoryFilterFunction_t)(WIN32_FIND_DATAW*);
    static bool DirectoryFilterDirectories(WIN32_FIND_DATAW* findData);
    static bool DirectoryFilterFile(WIN32_FIND_DATAW* findData);
    static int __cdecl WIN32_FIND_DATAW_qsort_helper(const void* p1, const void* p2);
    static int FilterDirectory(LPCWSTR                      searchPattern,
                               DirectoryFilterFunction_t    filter,
                               /* out */ WIN32_FIND_DATAW** ppFileArray,
                               int*                         pElemCount);

    static LPWSTR MergePathStrings(LPCWSTR dir, LPCWSTR file);

    static char* ConvertWideCharToMultiByte(LPCWSTR wstr);

    static int AppendFileRaw(HANDLE hFileOut, LPCWSTR fileFullPath, unsigned char* buffer, size_t bufferSize);
    static int AppendFile(HANDLE hFileOut, LPCWSTR fileFullPath, bool dedup, unsigned char* buffer, size_t bufferSize);
    static int AppendAllInDir(HANDLE              hFileOut,
                              LPCWSTR             dir,
                              LPCWSTR             file,
                              unsigned char*      buffer,
                              size_t              bufferSize,
                              bool                recursive,
                              bool                dedup,
                              /* out */ LONGLONG* size);

    static RemoveDup m_removeDups;
};
#endif
