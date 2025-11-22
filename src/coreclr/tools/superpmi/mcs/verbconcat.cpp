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

    FILE* fp1 = fopen(nameOfFile1, "ab+");
    if (fp1 == NULL)
    {
        LogError("Failed to open input 1 '%s'. errno=%d", nameOfFile1, errno);
        return -1;
    }

    FILE* fp2 = fopen(nameOfFile2, "rb");
    if (fp2 == NULL)
    {
        LogError("Failed to open input 2 '%s'. errno=%d", nameOfFile2, errno);
        return -1;
    }

    unsigned char* buffer = new unsigned char[BUFFER_SIZE];
    int64_t        offset = 0;

    st1.Start();
    while (!feof(fp2))
    {
        size_t bytesRead = fread(buffer, 1, BUFFER_SIZE, fp2);
        if (bytesRead <= 0)
        {
            LogError("Failed to read '%s' from offset %lld. errno=%d", nameOfFile2, offset, errno);
            delete[] buffer;
            return -1;
        }
        size_t bytesWritten = fwrite(buffer, 1, bytesRead, fp1);
        if (bytesWritten <= 0)
        {
            LogError("Failed to write '%s' at offset %lld. errno=%d", nameOfFile1, offset, errno);
            delete[] buffer;
            return -1;
        }
        if (bytesRead != bytesWritten)
        {
            LogError("Failed to read/write matching bytes %u!=%u", bytesRead, bytesWritten);
            delete[] buffer;
            return -1;
        }
        offset += bytesRead;
    }
    st1.Stop();

    delete[] buffer;

    if (fclose(fp1) != 0)
    {
        LogError("CloseHandle failed. errno=%d", errno);
        return -1;
    }
    if (fclose(fp2) != 0)
    {
        LogError("2nd CloseHandle failed. errno=%d", errno);
        return -1;
    }

    LogInfo("Read/Wrote %lld MB @ %4.2f MB/s.\n", offset / (1000 * 1000),
            (((double)offset) / (1000 * 1000)) /
                st1.GetSeconds()); // yes yes.. http://en.wikipedia.org/wiki/Megabyte_per_second#Megabyte_per_second

    return 0;
}
