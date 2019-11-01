// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "stdafx.h"
#include "jitperf.h"
#include "perflog.h"
#include "clrhost.h"
#include "contract.h"
#include "utilcode.h"
#include "sstring.h"

//=============================================================================
// ALL THE JIT PERF STATS GATHERING CODE IS COMPILED ONLY IF THE ENABLE_JIT_PERF WAS DEFINED.
#if defined(ENABLE_JIT_PERF)

__int64 g_JitCycles = 0;
size_t g_NonJitCycles = 0;
CRITSEC_COOKIE g_csJit;
__int64 g_tlsJitCycles = 0;
int g_fJitPerfOn;

size_t g_dwTlsx86CodeSize = 0;
size_t g_TotalILCodeSize = 0;
size_t g_Totalx86CodeSize = 0;
size_t g_TotalMethodsJitted = 0;

void OutputStats ()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    } CONTRACTL_END;

    LARGE_INTEGER cycleFreq;
    if (QueryPerformanceFrequency (&cycleFreq)) 
    {
        double dJitC = (double) g_JitCycles;
        double dNonJitC = (double) g_NonJitCycles;
        double dFreq = (double)cycleFreq.QuadPart;
        double compileSpeed = (double)g_TotalILCodeSize/(dJitC/dFreq);

        PERFLOG((W("Jit Cycles"), (dJitC - dNonJitC), CYCLES));
        PERFLOG((W("Jit Time"), (dJitC - dNonJitC)/dFreq, SECONDS));
        PERFLOG((W("Non Jit Cycles"), dNonJitC, CYCLES));
        PERFLOG((W("Non Jit Time"), dNonJitC/dFreq, SECONDS));
        PERFLOG((W("Total Jit Cycles"), dJitC, CYCLES));
        PERFLOG((W("Total Jit Time"), dJitC/dFreq, SECONDS));
        PERFLOG((W("Methods Jitted"), (UINT_PTR)g_TotalMethodsJitted, COUNT));
        PERFLOG((W("IL Code Compiled"), (UINT_PTR)g_TotalILCodeSize, BYTES));
        PERFLOG((W("X86 Code Emitted"), (UINT_PTR)g_Totalx86CodeSize, BYTES));
        // Included the perf counter description in this case because its not obvious what we are reporting.
        PERFLOG((W("ExecTime"), compileSpeed/1000, KBYTES_PER_SEC, W("IL Code compiled/sec")));
    }
}

void InitJitPerf(void) 
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    } CONTRACTL_END;

    InlineSString<4> lpszValue;
    g_fJitPerfOn = WszGetEnvironmentVariable (W("JIT_PERF_OUTPUT"), lpszValue);
    if (g_fJitPerfOn) 
    {
        g_csJit = ClrCreateCriticalSection(CrstJitPerf,CRST_UNSAFE_ANYMODE);
    }
}

void DoneJitPerfStats()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    } CONTRACTL_END;

    if (g_fJitPerfOn) 
    {
        ClrDeleteCriticalSection(g_csJit);
    
        // Output stats to stdout and if necessary to the perf automation file.
        OutputStats();
    }
    

}

void StartNonJITPerfWorker(LARGE_INTEGER * pCycleStart)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    } CONTRACTL_END;

    pCycleStart->QuadPart = 0;

    size_t pTlsNonJitCycles = (size_t) ClrFlsGetValue (TlsIdx_JitPerf);
    if ((pTlsNonJitCycles & 1) == 0 ) { /* odd value indicates we are in the EE */
        ClrFlsSetValue(TlsIdx_JitPerf, (LPVOID)(pTlsNonJitCycles + 1));
        QueryPerformanceCounter(pCycleStart);
    }
}

void StopNonJITPerfWorker(LARGE_INTEGER * pCycleStart)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    } CONTRACTL_END;

    LARGE_INTEGER CycleStop;

    if (pCycleStart->QuadPart != 0 && QueryPerformanceCounter(&CycleStop) ) {
        size_t pTlsNonJitCycles = (size_t)ClrFlsGetValue (TlsIdx_JitPerf);
        pTlsNonJitCycles += static_cast<size_t>(CycleStop.QuadPart - pCycleStart->QuadPart);
        pTlsNonJitCycles &= ~1; /*  even indicate we are not in EE */
        ClrFlsSetValue(TlsIdx_JitPerf, (LPVOID)(pTlsNonJitCycles));
    }
}


#endif //ENABLE_JIT_PERF


