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

        FILE* fpOut = fopen(buff, "w");
        if (fpOut == NULL)
        {
            LogError("Failed to open output '%s'. errno=%d", buff, errno);
            return -1;
        }

        if (mc->cr->IsEmpty())
        {
            fprintf(fpOut, ";;Method context has no compile result");
        }
        else
        {
            ASMDumper::DumpToFile(fpOut, mc, mc->cr);
        }

        if (fclose(fpOut) != 0)
        {
            LogError("fclose failed. errno=%d", errno);
            return -1;
        }
        savedCount++;
    }

    LogInfo("Asm'd %d", savedCount);

    if (!mci.Destroy())
        return -1;

    return 0;
}
