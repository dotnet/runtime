// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "verbfracture.h"
#include "simpletimer.h"
#include "methodcontext.h"
#include "methodcontextiterator.h"
#include "errorhandling.h"
#include "logging.h"

int verbFracture::DoWork(
    const char* nameOfInput, const char* nameOfOutput, int indexCount, const int* indexes, bool stripCR)
{
    int rangeSize = indexes[0];

    LogVerbose("Reading from '%s' copying %d MethodContexts files into each output file of '%s'", nameOfInput,
               rangeSize, nameOfOutput);

    MethodContextIterator mci(true);
    if (!mci.Initialize(nameOfInput))
        return -1;

    int  fileCount = 0;
    char fileName[512];

    HANDLE hFileOut = INVALID_HANDLE_VALUE;
    while (mci.MoveNext())
    {
        MethodContext* mc = mci.Current();

        if ((hFileOut == INVALID_HANDLE_VALUE) || (((mci.MethodContextNumber() - 1) % rangeSize) == 0))
        {
            if (hFileOut != INVALID_HANDLE_VALUE)
            {
                if (!CloseHandle(hFileOut))
                {
                    LogError("1st CloseHandle failed. GetLastError()=%u", GetLastError());
                    return -1;
                }
                hFileOut = INVALID_HANDLE_VALUE;
            }
            sprintf_s(fileName, 512, "%s-%0*d.mch", nameOfOutput, 5, fileCount++);
            hFileOut = CreateFileA(fileName, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS,
                                   FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
            if (hFileOut == INVALID_HANDLE_VALUE)
            {
                LogError("Failed to open output file '%s'. GetLastError()=%u", fileName, GetLastError());
                return -1;
            }
        }
        if (stripCR)
        {
            delete mc->cr;
            mc->cr = new CompileResult();
        }
        mc->saveToFile(hFileOut);
    }

    if (hFileOut != INVALID_HANDLE_VALUE)
    {
        if (!CloseHandle(hFileOut))
        {
            LogError("2nd CloseHandle failed. GetLastError()=%u", GetLastError());
            return -1;
        }
    }

    LogInfo("Output fileCount %d", fileCount);

    if (!mci.Destroy())
        return -1;

    return 0;
}
