// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//-----------------------------------------------------------------------------
// MethodStatsEmitter.cpp - Emits useful method stats for compiled methods for analysis
//-----------------------------------------------------------------------------

#include "standardpch.h"
#include "methodstatsemitter.h"
#include "logging.h"

MethodStatsEmitter::MethodStatsEmitter(char* nameOfInput)
{
    char filename[MAX_PATH + 1];
    sprintf_s(filename, MAX_PATH + 1, "%s.stats", nameOfInput);

    hStatsFile =
        CreateFileA(filename, GENERIC_WRITE, FILE_SHARE_WRITE, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    if (hStatsFile == INVALID_HANDLE_VALUE)
    {
        LogError("Failed to open output file '%s'. GetLastError()=%u", filename, GetLastError());
    }
}

MethodStatsEmitter::~MethodStatsEmitter()
{
    if (hStatsFile != INVALID_HANDLE_VALUE)
    {
        if (CloseHandle(hStatsFile) == 0)
        {
            LogError("CloseHandle failed. GetLastError()=%u", GetLastError());
        }
    }
}

void MethodStatsEmitter::Emit(int methodNumber, MethodContext* mc, ULONGLONG firstTime, ULONGLONG secondTime)
{
    if (hStatsFile != INVALID_HANDLE_VALUE)
    {
        // Print the CSV header row
        char  rowData[2048];
        DWORD charCount    = 0;
        DWORD bytesWritten = 0;

        if (strchr(statsTypes, '*') != NULL || strchr(statsTypes, 'h') != NULL || strchr(statsTypes, 'H') != NULL)
        {
            // Obtain the method Hash
            char md5Hash[MD5_HASH_BUFFER_SIZE];
            if (mc->dumpMethodMD5HashToBuffer(md5Hash, MD5_HASH_BUFFER_SIZE) != MD5_HASH_BUFFER_SIZE)
                md5Hash[0] = 0;

            charCount += sprintf_s(rowData + charCount, _countof(rowData) - charCount, "%s,", md5Hash);
        }
        if (strchr(statsTypes, '*') != NULL || strchr(statsTypes, 'n') != NULL || strchr(statsTypes, 'N') != NULL)
        {
            charCount += sprintf_s(rowData + charCount, _countof(rowData) - charCount, "%d,", methodNumber);
        }
        if (strchr(statsTypes, '*') != NULL || strchr(statsTypes, 'i') != NULL || strchr(statsTypes, 'I') != NULL)
        {
            // Obtain the IL code size for this method
            CORINFO_METHOD_INFO info;
            unsigned            flags = 0;
            mc->repCompileMethod(&info, &flags);

            charCount += sprintf_s(rowData + charCount, _countof(rowData) - charCount, "%d,", info.ILCodeSize);
        }
        if (strchr(statsTypes, '*') != NULL || strchr(statsTypes, 'a') != NULL || strchr(statsTypes, 'A') != NULL)
        {
            // Obtain the compiled method ASM size
            BYTE*        temp;
            DWORD        codeSize;
            CorJitResult result;
            if (mc->cr->CompileMethod != nullptr)
                mc->cr->repCompileMethod(&temp, &codeSize, &result);
            else
                codeSize = 0; // this is likely a thin mc

            charCount += sprintf_s(rowData + charCount, _countof(rowData) - charCount, "%d,", codeSize);
        }
        if (strchr(statsTypes, '*') != NULL || strchr(statsTypes, 't') != NULL || strchr(statsTypes, 'T') != NULL)
        {
            charCount +=
                sprintf_s(rowData + charCount, _countof(rowData) - charCount, "%llu,%llu,", firstTime, secondTime);
        }

        // get rid of the final ',' and replace it with a '\n'
        rowData[charCount - 1] = '\n';

        if (!WriteFile(hStatsFile, rowData, charCount, &bytesWritten, nullptr) || bytesWritten != charCount)
        {
            LogError("Failed to write row header '%s'. GetLastError()=%u", rowData, GetLastError());
        }
    }
}

void MethodStatsEmitter::SetStatsTypes(char* types)
{
    statsTypes = types;

    if (hStatsFile != INVALID_HANDLE_VALUE)
    {
        // Print the CSV header row
        char  rowHeader[1024];
        DWORD charCount    = 0;
        DWORD bytesWritten = 0;

        if (strchr(statsTypes, '*') != NULL || strchr(statsTypes, 'h') != NULL || strchr(statsTypes, 'H') != NULL)
            charCount += sprintf_s(rowHeader + charCount, _countof(rowHeader) - charCount, "HASH,");
        if (strchr(statsTypes, '*') != NULL || strchr(statsTypes, 'n') != NULL || strchr(statsTypes, 'N') != NULL)
            charCount += sprintf_s(rowHeader + charCount, _countof(rowHeader) - charCount, "METHOD_NUMBER,");
        if (strchr(statsTypes, '*') != NULL || strchr(statsTypes, 'i') != NULL || strchr(statsTypes, 'I') != NULL)
            charCount += sprintf_s(rowHeader + charCount, _countof(rowHeader) - charCount, "IL_CODE_SIZE,");
        if (strchr(statsTypes, '*') != NULL || strchr(statsTypes, 'a') != NULL || strchr(statsTypes, 'A') != NULL)
            charCount += sprintf_s(rowHeader + charCount, _countof(rowHeader) - charCount, "ASM_CODE_SIZE,");
        if (strchr(statsTypes, '*') != NULL || strchr(statsTypes, 't') != NULL || strchr(statsTypes, 'T') != NULL)
            charCount += sprintf_s(rowHeader + charCount, _countof(rowHeader) - charCount, "Time1,Time2,");

        // get rid of the final ',' and replace it with a '\n'
        rowHeader[charCount - 1] = '\n';

        if (!WriteFile(hStatsFile, rowHeader, charCount, &bytesWritten, nullptr) || bytesWritten != charCount)
        {
            LogError("Failed to write row header '%s'. GetLastError()=%u", rowHeader, GetLastError());
        }
    }
}
