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

    FILE* hFileIn1 = fopen(nameOfFile1, "ab");
    if (hFileIn1 == nullptr)
    {
        LogError("Failed to open input 1 '%s'. errno=%d", nameOfFile1, errno);
        return -1;
    }

    FILE* hFileIn2 = fopen(nameOfFile2, "rb");
    if (hFileIn2 == nullptr)
    {
        LogError("Failed to open input 2 '%s'. errno=%d", nameOfFile2, errno);
        return -1;
    }
    int64_t fileSize2 = fgetsize(hFileIn2);
    if (fileSize2 < 0)
    {
        LogError("fgetsize failed. errno=%d", errno);
        return -1;
    }

    unsigned char* buffer = new unsigned char[BUFFER_SIZE];

    st1.Start();
    for (LONGLONG offset = 0; offset < fileSize2; offset += BUFFER_SIZE)
    {
        size_t bytesRead = fread(buffer, 1, BUFFER_SIZE, hFileIn2);
        if (ferror(hFileIn2))
        {
            LogError("Failed to read '%s' from offset %lld. errno=%d", nameOfFile2, offset, errno);
            delete[] buffer;
            return -1;
        }
        size_t bytesWritten = fwrite(buffer, 1, bytesRead, hFileIn1);
        if (bytesRead != bytesWritten)
        {
            LogError("Failed to write '%s' at offset %lld. errno=%d", nameOfFile1, offset, errno);
            delete[] buffer;
            return -1;
        }
    }
    st1.Stop();

    delete[] buffer;

    if (fclose(hFileIn1) != 0)
    {
        LogError("fclose failed. errno=%d", errno);
        return -1;
    }
    if (fclose(hFileIn2) != 0)
    {
        LogError("2nd fclose failed. errno=%d", errno);
        return -1;
    }

    LogInfo("Read/Wrote %lld MB @ %4.2f MB/s.\n", fileSize2 / (1000 * 1000),
            (((double)fileSize2) / (1000 * 1000)) /
                st1.GetSeconds()); // yes yes.. http://en.wikipedia.org/wiki/Megabyte_per_second#Megabyte_per_second

    return 0;
}
