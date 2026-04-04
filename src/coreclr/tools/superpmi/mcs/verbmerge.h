// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// verbMerge.h - verb that merges multiple .MC into one .MCH file
// It allows .MC names to be Unicode names with long paths.
//----------------------------------------------------------
#ifndef _verbMerge
#define _verbMerge

#include "removedup.h"

#ifdef TARGET_WINDOWS
typedef _WIN32_FIND_DATAW FindData;
#else
struct FindData
{
    unsigned char d_type;
    WCHAR *cFileName;

    FindData() : d_type(0), cFileName(nullptr)
    {
    }   

    FindData(unsigned char type, WCHAR *fileName)
    {
        d_type = type;
        cFileName = fileName;
    }

    // Prevent copying of the FindData, only allow moving so that the cFileName doesn't need to be duplicated.
    FindData(const FindData& other) = delete;
    FindData& operator=(const FindData& other) = delete;

    FindData(FindData&& other) : d_type(other.d_type), cFileName(other.cFileName)
    {
        other.d_type = 0;
        other.cFileName = nullptr;
    }

    FindData& operator=(FindData&& other)
    {
        d_type = other.d_type;
        cFileName = other.cFileName;
        other.d_type = 0;
        other.cFileName = nullptr;

        return *this;
    }

    ~FindData()
    {
        delete [] cFileName;
    }
};
#endif

class verbMerge
{
public:
    static int DoWork(const char* nameOfOutputFile, const char* pattern, bool recursive, bool dedup, bool stripCR);

private:

#ifdef TARGET_WINDOWS
    typedef _WIN32_FIND_DATAW FilterArgType;
#else
    typedef struct FindData FilterArgType;
#endif

    typedef bool (*DirectoryFilterFunction_t)(FilterArgType*);
    static bool DirectoryFilterDirectories(FilterArgType* findData);
    static bool DirectoryFilterFile(FilterArgType* findData);
    static int __cdecl FindData_qsort_helper(const void* p1, const void* p2);
    static int FilterDirectory(LPCWSTR                      dir,
                               LPCWSTR                      searchPattern,
                               DirectoryFilterFunction_t    filter,
                               /* out */ FindData** ppFileArray,
                               int*                         pElemCount);

    static LPWSTR MergePathStrings(LPCWSTR dir, LPCWSTR file);

    static char* ConvertWideCharToMultiByte(LPCWSTR wstr);
    static WCHAR* ConvertMultiByteToWideChar(LPCSTR str);

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
