//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include "standardpch.h"
#include "verbremovedup.h"
#include "removedup.h"

int verbRemoveDup::DoWork(const char* nameOfInput, const char* nameOfOutput, bool stripCR, bool legacyCompare)
{
    LogVerbose("Removing duplicates from '%s', writing to '%s'", nameOfInput, nameOfOutput);

    HANDLE hFileOut = CreateFileA(nameOfOutput, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS,
                                  FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (hFileOut == INVALID_HANDLE_VALUE)
    {
        LogError("Failed to open output '%s'. GetLastError()=%u", nameOfOutput, GetLastError());
        return -1;
    }

    RemoveDup removeDups;
    if (!removeDups.Initialize(stripCR, legacyCompare, /* cleanup */ false)
        || !removeDups.CopyAndRemoveDups(nameOfInput, hFileOut))
    {
        LogError("Failed to remove dups");
        return -1;
    }

    if (CloseHandle(hFileOut) == 0)
    {
        LogError("CloseHandle failed. GetLastError()=%u", GetLastError());
        return -1;
    }

    return 0;
}
