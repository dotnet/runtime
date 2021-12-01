// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "verbconcat.h"
#include "simpletimer.h"
#include "logging.h"

#define BUFFER_SIZE 0xFFFFFF

int verbConcat::DoWork(const char* nameOfFile1, const char* nameOfFile2)
{
    SimpleTimer st1;

    LogVerbose("Concatenating '%s'+'%s' into %s", nameOfFile1, nameOfFile2, nameOfFile1);

    LARGE_INTEGER DataTemp1;
    LARGE_INTEGER DataTemp2;

    HANDLE hFileIn1 = CreateFileA(nameOfFile1, GENERIC_READ | GENERIC_WRITE, 0, NULL, OPEN_ALWAYS,
                                  FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (hFileIn1 == INVALID_HANDLE_VALUE)
    {
        LogError("Failed to open input 1 '%s'. GetLastError()=%u", nameOfFile1, GetLastError());
        return -1;
    }
    if (GetFileSizeEx(hFileIn1, &DataTemp1) == 0)
    {
        LogError("GetFileSizeEx failed. GetLastError()=%u", GetLastError());
        return -1;
    }

    LONG  highDWORD = 0;
    DWORD dwPtr     = SetFilePointer(hFileIn1, 0, &highDWORD, FILE_END);
    if (dwPtr == INVALID_SET_FILE_POINTER)
    {
        LogError("Failed to SetFilePointer on input 1 '%s'. GetLastError()=%u", nameOfFile1, GetLastError());
        return -1;
    }

    HANDLE hFileIn2 = CreateFileA(nameOfFile2, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING,
                                  FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (hFileIn2 == INVALID_HANDLE_VALUE)
    {
        LogError("Failed to open input 2 '%s'. GetLastError()=%u", nameOfFile2, GetLastError());
        return -1;
    }
    if (GetFileSizeEx(hFileIn2, &DataTemp2) == 0)
    {
        LogError("2nd GetFileSizeEx failed. GetLastError()=%u", GetLastError());
        return -1;
    }

    unsigned char* buffer = new unsigned char[BUFFER_SIZE];

    st1.Start();
    for (LONGLONG offset = 0; offset < DataTemp2.QuadPart; offset += BUFFER_SIZE)
    {
        DWORD bytesRead = -1;
        BOOL  res       = ReadFile(hFileIn2, buffer, BUFFER_SIZE, &bytesRead, nullptr);
        if (res == 0)
        {
            LogError("Failed to read '%s' from offset %lld. GetLastError()=%u", nameOfFile2, offset, GetLastError());
            return -1;
        }
        DWORD bytesWritten = -1;
        BOOL  res2         = WriteFile(hFileIn1, buffer, bytesRead, &bytesWritten, nullptr);
        if (res2 == 0)
        {
            LogError("Failed to write '%s' at offset %lld. GetLastError()=%u", nameOfFile1, offset, GetLastError());
            return -1;
        }
        if (bytesRead != bytesWritten)
        {
            LogError("Failed to read/write matching bytes %u!=%u", bytesRead, bytesWritten);
            return -1;
        }
    }
    st1.Stop();

    delete[] buffer;

    if (CloseHandle(hFileIn1) == 0)
    {
        LogError("CloseHandle failed. GetLastError()=%u", GetLastError());
        return -1;
    }
    if (CloseHandle(hFileIn2) == 0)
    {
        LogError("2nd CloseHandle failed. GetLastError()=%u", GetLastError());
        return -1;
    }

    LogInfo("Read/Wrote %lld MB @ %4.2f MB/s.\n", DataTemp2.QuadPart / (1000 * 1000),
            (((double)DataTemp2.QuadPart) / (1000 * 1000)) /
                st1.GetSeconds()); // yes yes.. http://en.wikipedia.org/wiki/Megabyte_per_second#Megabyte_per_second

    return 0;
}
