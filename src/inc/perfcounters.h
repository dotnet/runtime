// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//-----------------------------------------------------------------------------
// PerfCounters.h
//
// Internal Interface for CLR to use Performance counters
//-----------------------------------------------------------------------------


#ifndef _PerfCounters_h_
#define _PerfCounters_h_

#include "perfcounterdefs.h"

#ifdef ENABLE_PERF_COUNTERS
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// This code section active iff we're using Perf Counters
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// PerfCounter class serves as namespace with data protection. 
// Enforce this by making constructor private
//-----------------------------------------------------------------------------
class PerfCounters
{
private:
	PerfCounters();

public:
	static HRESULT Init();
	static void Terminate();

    static PerfCounterIPCControlBlock * GetPrivatePerfCounterPtr();

private:
	static HANDLE m_hPrivateMapPerf;

	static PerfCounterIPCControlBlock * m_pPrivatePerf;

	static BOOL m_fInit;
	
// Set pointers to garbage so they're never null.
	static PerfCounterIPCControlBlock m_garbage;

    friend PerfCounterIPCControlBlock & GetPerfCounters();
};

//-----------------------------------------------------------------------------
// Utility functions
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// Get the perf counters specific to our process
//-----------------------------------------------------------------------------
inline PerfCounterIPCControlBlock & GetPerfCounters()
{
    LIMITED_METHOD_CONTRACT;
    
    return *PerfCounters::m_pPrivatePerf;
}

inline PerfCounterIPCControlBlock *PerfCounters::GetPrivatePerfCounterPtr()
{
    LIMITED_METHOD_CONTRACT;

    return m_pPrivatePerf;
};

#define COUNTER_ONLY(x) x

#define PERF_COUNTER_NUM_OF_ITERATIONS 10

#if defined(_X86_) && defined(_MSC_VER)

inline UINT64 GetCycleCount_UINT64()
{
    LIMITED_METHOD_CONTRACT;
    return __rdtsc();
}

#else // defined(_X86_) && defined(_MSC_VER)
inline UINT64 GetCycleCount_UINT64()
{
    LIMITED_METHOD_CONTRACT;
    
    LARGE_INTEGER qwTmp;
    QueryPerformanceCounter(&qwTmp);
    return qwTmp.QuadPart;
}
#endif // defined(_X86_) && defined(_MSC_VER)

#define PERF_COUNTER_TIMER_PRECISION UINT64
#define GET_CYCLE_COUNT GetCycleCount_UINT64

#define PERF_COUNTER_TIMER_START() \
PERF_COUNTER_TIMER_PRECISION _startPerfCounterTimer = GET_CYCLE_COUNT();

#define PERF_COUNTER_TIMER_STOP(global) \
global = (GET_CYCLE_COUNT() - _startPerfCounterTimer);




#else // ENABLE_PERF_COUNTERS
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// This code section active iff we're NOT using Perf Counters
// Note, not even a class definition, so all usages of PerfCounters in client
// should be in #ifdef or COUNTER_ONLY(). 
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------

#define COUNTER_ONLY(x)


#endif // ENABLE_PERF_COUNTERS


#endif // _PerfCounters_h_
