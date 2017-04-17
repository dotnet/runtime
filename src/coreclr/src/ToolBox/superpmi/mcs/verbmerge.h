//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//----------------------------------------------------------
// verbMerge.h - verb that merges multiple .MC into one .MCH file
//----------------------------------------------------------
#ifndef _verbMerge
#define _verbMerge

class verbMerge
{
public:
    static int DoWork(const char* nameOfOutputFile, const char* pattern, bool recursive);

private:
    typedef bool (*DirectoryFilterFunction_t)(WIN32_FIND_DATAA*);
    static bool DirectoryFilterDirectories(WIN32_FIND_DATAA* findData);
    static bool DirectoryFilterFile(WIN32_FIND_DATAA* findData);
    static int __cdecl WIN32_FIND_DATAA_qsort_helper(const void* p1, const void* p2);
    static int FilterDirectory(const char*                  searchPattern,
                               DirectoryFilterFunction_t    filter,
                               /* out */ WIN32_FIND_DATAA** ppFileArray,
                               int*                         pElemCount);

    static char* MergePathStrings(const char* dir, const char* file);

    static int AppendFile(HANDLE hFileOut, const char* fileName, unsigned char* buffer, size_t bufferSize);
    static int AppendAllInDir(HANDLE              hFileOut,
                              const char*         dir,
                              const char*         file,
                              unsigned char*      buffer,
                              size_t              bufferSize,
                              bool                recursive,
                              /* out */ LONGLONG* size);
};
#endif
