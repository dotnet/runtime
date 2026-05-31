// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "verbremovedup.h"
#include "removedup.h"

int verbRemoveDup::DoWork(const char* nameOfInput, const char* nameOfOutput, bool stripCR, bool legacyCompare)
{
    LogVerbose("Removing duplicates from '%s', writing to '%s'", nameOfInput, nameOfOutput);

    FILE* fpOut = fopen(nameOfOutput, "wb");
    if (fpOut == NULL)
    {
        LogError("Failed to open output '%s'. errno=%d", nameOfOutput, errno);
        return -1;
    }

    RemoveDup removeDups;
    if (!removeDups.Initialize(stripCR, legacyCompare, /* cleanup */ false)
        || !removeDups.CopyAndRemoveDups(nameOfInput, fpOut))
    {
        LogError("Failed to remove dups");
        return -1;
    }

    if (fclose(fpOut) != 0)
    {
        LogError("CloseHandle failed. errno=%d", errno);
        return -1;
    }

    return 0;
}
