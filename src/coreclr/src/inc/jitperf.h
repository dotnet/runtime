// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//-----------------------------------------------------------------------------
// JitPerf.h
// Internal interface for gathering JIT perfmormance stats. These stats are
// logged (or displayed) in two ways. If PERF_COUNTERS are enabled the 
// perfmon etc. would display the jit stats. If ENABLE_PERF_LOG is enabled
// and PERF_OUTPUT env var is defined then the jit stats are displayed on the 
// stdout. (The jit stats are outputted in a specific format to a file for 
// automated perf tests.)
//

//-----------------------------------------------------------------------------


#ifndef __JITPERF_H__
#define __JITPERF_H__

#include "mscoree.h"
#include "clrinternal.h"

// ENABLE_JIT_PERF tag used to activate JIT specific profiling.
#define ENABLE_JIT_PERF

#if defined(ENABLE_JIT_PERF)

extern __int64 g_JitCycles;
extern size_t g_NonJitCycles;
extern CRITSEC_COOKIE g_csJit;
extern __int64 g_tlsJitCycles;
extern int g_fJitPerfOn;

extern size_t g_dwTlsx86CodeSize;
extern size_t g_TotalILCodeSize;
extern size_t g_Totalx86CodeSize;
extern size_t g_TotalMethodsJitted;

// Public interface to initialize jit stats data structs
void InitJitPerf(void);
// Public interface to deallocate datastruct and output the stats.
void DoneJitPerfStats(void);

// Start/StopNonJITPerf macros are used many times. Factor out the payload
// into helper method to reduce code size.
void StartNonJITPerfWorker(LARGE_INTEGER * pCycleStart);
void StopNonJITPerfWorker(LARGE_INTEGER * pCycleStart);

// Use the callee's stack frame (so START & STOP functions can share variables)
#define START_JIT_PERF()                                                \
    if (g_fJitPerfOn) {                                                 \
        ClrFlsSetValue (TlsIdx_JitPerf, (LPVOID)0);                     \
        g_dwTlsx86CodeSize = 0;                                         \
        ClrFlsSetValue (TlsIdx_JitX86Perf, (LPVOID)g_dwTlsx86CodeSize); \
    } 


#define STOP_JIT_PERF()                                                 \
    if (g_fJitPerfOn) {                                                 \
        size_t dwTlsNonJitCycles = (size_t)ClrFlsGetValue (TlsIdx_JitPerf); \
        size_t dwx86CodeSize = (size_t)ClrFlsGetValue (TlsIdx_JitX86Perf); \
        CRITSEC_Holder csh (g_csJit);                                   \
        g_JitCycles += static_cast<size_t>(CycleStop.QuadPart - CycleStart.QuadPart);      \
        g_NonJitCycles += dwTlsNonJitCycles;                            \
        g_TotalILCodeSize += methodInfo.ILCodeSize;                     \
        g_Totalx86CodeSize += dwx86CodeSize;                            \
        g_TotalMethodsJitted ++;                                        \
    }

#define START_NON_JIT_PERF()                                            \
    LARGE_INTEGER CycleStart;                                           \
    if(g_fJitPerfOn) {                                                  \
        StartNonJITPerfWorker(&CycleStart);                             \
    }

#define STOP_NON_JIT_PERF()                                             \
    if(g_fJitPerfOn) {                                                  \
        StopNonJITPerfWorker(&CycleStart);                              \
    }

#define JIT_PERF_UPDATE_X86_CODE_SIZE(size)                             \
    if(g_fJitPerfOn) {                                                  \
        size_t dwx86CodeSize = (size_t)ClrFlsGetValue (TlsIdx_JitX86Perf); \
        dwx86CodeSize += (size);                                        \
        ClrFlsSetValue (TlsIdx_JitX86Perf, (LPVOID)dwx86CodeSize);      \
    }


#else //ENABLE_JIT_PERF
#define START_JIT_PERF()
#define STOP_JIT_PERF()
#define START_NON_JIT_PERF()
#define STOP_NON_JIT_PERF()
#define JIT_PERF_UPDATE_X86_CODE_SIZE(size)                 
#endif //ENABLE_JIT_PERF

#endif //__JITPERF_H__
