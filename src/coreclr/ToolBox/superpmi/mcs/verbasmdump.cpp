// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "verbasmdump.h"
#include "simpletimer.h"
#include "methodcontext.h"
#include "methodcontextiterator.h"
#include "asmdumper.h"
#include "errorhandling.h"

#define BUFFER_SIZE 0xFFFFFF

int verbASMDump::DoWork(const char* nameOfInput, const char* nameOfOutput, int indexCount, const int* indexes)
{
    LogVerbose("Loading from '%s' and writing ASM output into '%s-MC#.asm'", nameOfInput, nameOfOutput);

    MethodContextIterator mci(indexCount, indexes, true);
    if (!mci.Initialize(nameOfInput))
        return -1;

    int savedCount = 0;

    while (mci.MoveNext())
    {
        MethodContext* mc = mci.Current();

        char buff[500];
        sprintf_s(buff, 500, "%s-%d.asm", nameOfOutput, mci.MethodContextNumber());

        HANDLE hFileOut = CreateFileA(buff, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS,
                                      FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
        if (hFileOut == INVALID_HANDLE_VALUE)
        {
            LogError("Failed to open output '%s'. GetLastError()=%u", buff, GetLastError());
            return -1;
        }

        if (mc->cr->IsEmpty())
        {
            const size_t bufflen = 4096;
            DWORD        bytesWritten;
            char         buff[bufflen];
            ZeroMemory(buff, bufflen * sizeof(char));
            int buff_offset = sprintf_s(buff, bufflen, ";;Method context has no compile result");
            WriteFile(hFileOut, buff, buff_offset * sizeof(char), &bytesWritten, nullptr);
        }
        else
        {
            ASMDumper::DumpToFile(hFileOut, mc, mc->cr);
        }

        if (!CloseHandle(hFileOut))
        {
            LogError("CloseHandle failed. GetLastError()=%u", GetLastError());
            return -1;
        }
        savedCount++;
    }

    LogInfo("Asm'd %d", savedCount);

    if (!mci.Destroy())
        return -1;

    return 0;
}
