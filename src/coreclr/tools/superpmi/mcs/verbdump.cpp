// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "verbdump.h"
#include "logging.h"
#include "methodcontext.h"
#include "methodcontextiterator.h"
#include "errorhandling.h"

int verbDump::DoWork(const char* nameOfInput, int indexCount, const int* indexes, bool simple)
{
    LogVerbose("Dumping '%s' to console", nameOfInput);

    MethodContextIterator mci(indexCount, indexes);
    if (!mci.Initialize(nameOfInput))
        return -1;

    int dumpedCount = 0;

    while (mci.MoveNext())
    {
        MethodContext* mc = mci.Current();
        mc->dumpToConsole(mci.MethodContextNumber(), simple);
        dumpedCount++;
    }

    LogVerbose("Dumped %d methodContexts", dumpedCount);

    if (!mci.Destroy())
        return -1;

    return 0;
}
