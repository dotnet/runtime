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

    FILE* fpOut = fopen(nameOfOutput, "w");
    if (fpOut == NULL)
    {
        LogError("Failed to open input 1 '%s'. errno=%d", nameOfOutput, errno);
        return -1;
    }

#define bufflen 50000
    char  buff[bufflen];
    int   offset = 0;
    ZeroMemory(&buff[0], bufflen);
    offset += sprintf_s(buff, bufflen, "Title,MC#,");
    offset += MethodContext::dumpStatTitleToBuffer(&buff[offset], bufflen - offset);
    fprintf(fpOut, "%s\n", buff);

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
        fprintf(fpOut, "%s\n", buff);
        savedCount++;
    }

    if (fclose(fpOut) != 0)
    {
        LogError("fclose failed. errno=%d", errno);
        return -1;
    }

    LogInfo("Loaded %d, Stat'd %d", mci.MethodContextNumber(), savedCount);

    if (!mci.Destroy())
        return -1;

    return 0;
}
