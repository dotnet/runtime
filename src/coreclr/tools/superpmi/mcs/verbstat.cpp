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

    FILE* fp;
    if (!fopen_s(&fp, nameOfOutput, "w"))
    {
        LogError("Failed to open output '%s'. errno=%d", nameOfOutput, errno);
        return -1;
    }

    char buff[50000];
    memset(buff, 0, sizeof(buff));

    fprintf_s(fp, "Title,MC#,");
    MethodContext::dumpStatTitleToBuffer(buff, sizeof(buff));
    fprintf_s(fp, "%s", buff);
    fprintf_s(fp, "\n");

    while (mci.MoveNext())
    {
        MethodContext* mc = mci.Current();

        memset(buff, 0, sizeof(buff));
        if ((mc->cr->ProcessName != nullptr) && (mc->cr->ProcessName->GetCount() > 0))
        {
            fprintf_s(fp, "%s", mc->cr->repProcessName());
        }
        fprintf_s(fp, ",");
        fprintf_s(fp, "%d,", mci.MethodContextNumber());
        mc->dumpStatToBuffer(buff, sizeof(buff));
        fprintf_s(fp, "%s", buff);
        fprintf_s(fp, "\n");
        savedCount++;
    }

    if (fclose(fp) != 0)
    {
        LogError("fclose failed. errno=%d", errno);
        return -1;
    }

    LogInfo("Loaded %d, Stat'd %d", mci.MethodContextNumber(), savedCount);

    if (!mci.Destroy())
        return -1;

    return 0;
}
