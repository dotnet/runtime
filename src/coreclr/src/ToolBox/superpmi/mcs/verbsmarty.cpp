//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include "standardpch.h"
#include "verbsmarty.h"
#include "simpletimer.h"
#include "methodcontext.h"
#include "methodcontextiterator.h"

//
// Constructs a new verbSmarty.
//
// Arguments:
//    hFile  - A handle to the output file that we are writing the Smarty Test IDs
//
// Assumptions:
//    hFile refers to an open and writeable file handle.
//
verbSmarty::verbSmarty(HANDLE hFile)
{
    m_hFile = hFile;
}

//
// Dumps the Smarty TestID to file
//
// Arguments:
//    testID    - Smarty Test ID
//
void verbSmarty::DumpTestInfo(int testID)
{
#define bufflen 4096
    DWORD bytesWritten;

    char buff[bufflen];
    int  buff_offset = 0;
    ZeroMemory(buff, bufflen * sizeof(char));

    buff_offset += sprintf_s(&buff[buff_offset], bufflen - buff_offset, "%i\r\n", testID);
    WriteFile(m_hFile, buff, buff_offset * sizeof(char), &bytesWritten, nullptr);
}

int verbSmarty::DoWork(const char* nameOfInput, const char* nameOfOutput, int indexCount, const int* indexes)
{
    LogVerbose("Reading from '%s' reading Smarty ID for the Mc Indexes and writing into '%s'", nameOfInput,
               nameOfOutput);

    MethodContextIterator mci(indexCount, indexes);
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

    verbSmarty* verbList = new verbSmarty(hFileOut);

    // TODO-Cleanup: look to use toc for this
    while (mci.MoveNext())
    {
        MethodContext* mc = mci.Current();

        int testID = mc->repGetTestID();
        if (testID != -1)
        {
            // write to the file
            verbList->DumpTestInfo(testID);
        }
        else
        {
            LogError("Smarty ID not found for '%s'", mc->cr->repProcessName());
        }
    }

    delete verbList;

    if (!CloseHandle(hFileOut))
    {
        LogError("2nd CloseHandle failed. GetLastError()=%u", GetLastError());
        return -1;
    }

    LogInfo("Loaded %d, Saved %d", mci.MethodContextNumber(), savedCount);

    if (!mci.Destroy())
        return -1;

    return 0;
}
