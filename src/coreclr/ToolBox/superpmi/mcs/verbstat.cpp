// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "verbstat.h"
#include "simpletimer.h"
#include "methodcontext.h"
#include "methodcontextiterator.h"
#include "errorhandling.h"

int verbStat::DoWork(const char* nameOfInput, const char* nameOfOutput, int indexCount, const int* indexes)
{
    LogVerbose("Stat'ing from '%s' and writing output into '%s'", nameOfInput, nameOfOutput);

    MethodContextIterator mci(indexCount, indexes, true);
    if (!mci.Initialize(nameOfInput))
        return -1;

    int savedCount = 0;

    HANDLE hFileOut = CreateFileA(nameOfOutput, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS,
                                  FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (hFileOut == INVALID_HANDLE_VALUE)
    {
        LogError("Failed to open input 1 '%s'. GetLastError()=%u", nameOfOutput, GetLastError());
        return -1;
    }

#define bufflen 50000
    DWORD bytesWritten;
    char  buff[bufflen];
    int   offset = 0;
    ZeroMemory(&buff[0], bufflen);
    offset += sprintf_s(buff, bufflen, "Title,MC#,");
    offset += MethodContext::dumpStatTitleToBuffer(&buff[offset], bufflen - offset);
    buff[offset++] = 0x0d;
    buff[offset++] = 0x0a;
    WriteFile(hFileOut, &buff[0], offset, &bytesWritten, nullptr);

    while (mci.MoveNext())
    {
        MethodContext* mc = mci.Current();

        offset = 0;
        ZeroMemory(&buff[0], bufflen);
        if ((mc->cr->ProcessName != nullptr) && (mc->cr->ProcessName->GetCount() > 0))
        {
            const char* procname = mc->cr->repProcessName();
            strcpy_s(&buff[offset], bufflen, procname);
            offset += (int)strlen(procname);
        }
        buff[offset++] = ',';
        offset += sprintf_s(&buff[offset], bufflen - offset, "%d,", mci.MethodContextNumber());
        offset += mc->dumpStatToBuffer(&buff[offset], bufflen - offset);
        buff[offset++] = 0x0d;
        buff[offset++] = 0x0a;
        WriteFile(hFileOut, &buff[0], offset, &bytesWritten, nullptr);
        savedCount++;
    }

    if (!CloseHandle(hFileOut))
    {
        LogError("2nd CloseHandle failed. GetLastError()=%u", GetLastError());
        return -1;
    }

    LogInfo("Loaded %d, Stat'd %d", mci.MethodContextNumber(), savedCount);

    if (!mci.Destroy())
        return -1;

    return 0;
}
