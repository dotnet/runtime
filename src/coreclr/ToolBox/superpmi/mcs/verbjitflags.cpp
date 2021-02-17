// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "verbjitflags.h"
#include "methodcontext.h"
#include "methodcontextiterator.h"
#include "errorhandling.h"
#include "spmidumphelper.h"

int verbJitFlags::DoWork(const char* nameOfInput)
{
    MethodContextIterator mci;
    if (!mci.Initialize(nameOfInput))
        return -1;

    LightWeightMap<unsigned long long, unsigned> flagMap;
    unsigned mcCount = 0;

    while (mci.MoveNext())
    {
        MethodContext* mc = mci.Current();
        CORJIT_FLAGS corJitFlags;
        mc->repGetJitFlags(&corJitFlags, sizeof(corJitFlags));
        unsigned long long rawFlags = corJitFlags.GetFlagsRaw();

        int index = flagMap.GetIndex(rawFlags);
        if (index == -1)
        {
            flagMap.Add(rawFlags, 1);
        }
        else
        {
            int oldVal = flagMap.GetItem(index);
            flagMap.Update(index, oldVal + 1);
        }

        mcCount++;
    }

    if (!mci.Destroy())
        return -1;

    printf("\nGrouped Flag Appearances (%u contexts)\n\n", mcCount);
    printf("%-16s %8s %8s  parsed\n", "bits", "count", "percent");

    unsigned appearancesPerBit[64] = {};

    const unsigned int count = flagMap.GetCount();
    unsigned long long* pFlags = flagMap.GetRawKeys();

    for (unsigned int i = 0; i < count; i++)
    {
        const unsigned long long flags = *pFlags++;
        const int index = flagMap.GetIndex(flags);
        const unsigned appearances = flagMap.GetItem(index);

        printf("%016llx %8u %7.2f%% %s\n", flags, appearances, 100.0 * ((double) appearances / mcCount), SpmiDumpHelper::DumpJitFlags(flags).c_str());

        for (unsigned int bit = 0; bit < 64; bit++)
        {
            if (flags & (1ull << bit))
            {
                appearancesPerBit[bit] += appearances;
            }
        }
    }

    printf("\nIndividual Flag Appearances\n\n");
    for (unsigned int bit = 0; bit < 64; bit++)
    {
        unsigned perBit = appearancesPerBit[bit];
        if (perBit > 0)
        {
            printf("%8u %7.2f%% %s\n", perBit, 100.0 * (double) perBit / mcCount, SpmiDumpHelper::DumpJitFlags(1ull<<bit).c_str());
        }
    }

    return 0;
}

