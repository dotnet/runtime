//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include "standardpch.h"
#include "verbdump.h"
#include "logging.h"
#include "methodcontext.h"
#include "methodcontextiterator.h"
#include "errorhandling.h"

int verbDump::DoWork(const char* nameOfInput, int indexCount, const int* indexes)
{
    LogVerbose("Dumping '%s' to console", nameOfInput);

    MethodContextIterator mci(indexCount, indexes);
    if (!mci.Initialize(nameOfInput))
        return -1;

    int dumpedCount = 0;

    while (mci.MoveNext())
    {
        MethodContext* mc = mci.Current();
        mc->dumpToConsole(mci.MethodContextNumber());
        dumpedCount++;
    }

    LogVerbose("Dumped %d methodContexts", dumpedCount);

    if (!mci.Destroy())
        return -1;

    return 0;
}
