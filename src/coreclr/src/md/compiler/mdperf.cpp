// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// MDperf.cpp
//

//
// This file provides Compiler Support functionality in metadata.
//*****************************************************************************

#include "stdafx.h"

#include "mdperf.h"

#ifdef FEATURE_METADATA_PERF_STATS

//-----------------------------------------------------------------------------
// Global array containing the name of the APIs. This is shared across
// all instances of MDCompilerPerf.
//-----------------------------------------------------------------------------
char g_szNameOfAPI[LAST_MD_API][API_NAME_STR_SIZE];

//-----------------------------------------------------------------------------
// Constructor. Initialize counters to 0. Initialize names of MD APIs.
//-----------------------------------------------------------------------------
MDCompilerPerf::MDCompilerPerf()
{
    // Initialize counters
    for (int idx=0; idx < LAST_MD_API; idx++)
    {
        MDPerfStats[idx].dwCalledNumTimes = 0;
        MDPerfStats[idx].dwQueryPerfCycles = 0;
    }

#undef MD_FUNC
#define MD_FUNC(MDTag)\
    strncpy(g_szNameOfAPI[MDTag ## _ENUM], #MDTag, API_NAME_STR_SIZE-1);

    MD_COMPILER_PERF_TABLE;  // Relies on the MD_FUNC defined above.
}

MDCompilerPerf::~MDCompilerPerf()
    {
        // Output the stats and cleanup.
        MetaDataPerfReport ();
    }

//-----------------------------------------------------------------------------
// Output stats. <TODO>TODO: grow this into stats for per fautomation</TODO>
//-----------------------------------------------------------------------------
void MDCompilerPerf::MetaDataPerfReport ()
{
    LARGE_INTEGER freqVal;
    DWORD totalCalls=0, totalCycles=0;

    if (!QueryPerformanceFrequency(&freqVal))
    {
        printf("Perf counters not supported\n");
        return;
    }

    for (int idx=0; idx < LAST_MD_API; idx++)
    {
        totalCalls += MDPerfStats[idx].dwCalledNumTimes;
        totalCycles += MDPerfStats[idx].dwQueryPerfCycles;
    }

    if (!(totalCalls && totalCycles && freqVal.QuadPart))
    {
        // if any of above is 0 then things don't look good.
        printf("No data gathered ...\n");
        return;
    }

    printf("\n%-32.32s %-16.16s %-16.16s %-16.16s\n", "API Name", "# Calls", "Cycles", "Time (msec)");
    for (idx=0; idx < LAST_MD_API; idx++)
    {
        if(MDPerfStats[idx].dwCalledNumTimes != 0)
            printf( "%-32.32s %-9d [%3.2d%%] %-16d %-8.2f [%3.2d%%]\n",
                    g_szNameOfAPI[idx],
                    MDPerfStats[idx].dwCalledNumTimes,
                    (MDPerfStats[idx].dwCalledNumTimes*100)/totalCalls,
                    MDPerfStats[idx].dwQueryPerfCycles,
                    ((float)MDPerfStats[idx].dwQueryPerfCycles*1000)/(float)freqVal.QuadPart,
                    (MDPerfStats[idx].dwQueryPerfCycles*100)/totalCycles);
    }
    printf( "%-32.32s %-9d [100%%] %-16d %-8.2f [100%%]\n\n",
            "Total Stats",
            totalCalls,
            totalCycles,
            ((float)totalCycles*1000)/(float)freqVal.QuadPart);

}

#endif //FEATURE_METADATA_PERF_STATS
