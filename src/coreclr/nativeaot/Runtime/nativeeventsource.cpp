// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gcenv.h"
#include "clretwallmain.h"

#ifdef FEATURE_PERFTRACING

// These are native  call into corresponding FireEtw* events for events that want to be emitted from the managed
// side using NativeRuntimeEventSource.
// See https://github.com/dotnet/runtime/pull/47829 for an example of how to do this.
EXTERN_C NATIVEAOT_API void __cdecl RhNativeRuntimeEventSource_LogThreadPoolWorkerThreadStart(uint32_t activeWorkerThreadCount, uint32_t retiredWorkerThreadCount, uint16_t clrInstanceID)
{
    FireEtwThreadPoolWorkerThreadStart(activeWorkerThreadCount, retiredWorkerThreadCount, clrInstanceID);
}

// extern "C" void __cdecl LogThreadPoolWorkerThreadStop(uint activeWorkerThreadCount, uint retiredWorkerThreadCount, short clrInstanceID)
// {
//     FireEtwThreadPoolWorkerThreadStop(activeWorkerThreadCount, retiredWorkerThreadCount, clrInstanceID);
// }

// extern "C" void __cdecl LogThreadPoolWorkerThreadWait(uint activeWorkerThreadCount, uint retiredWorkerThreadCount, short clrInstanceID)
// {
//     FireEtwThreadPoolWorkerThreadWait(activeWorkerThreadCount, retiredWorkerThreadCount, clrInstanceID);
// }

// extern "C" void __cdecl LogThreadPoolMinMaxThreads(short minWorkerThreads, short maxWorkerThreads, short minIOCompletionThreads, short maxIOCompletionThreads, short clrInstanceID)
// {
//     FireEtwThreadPoolMinMaxThreads(minWorkerThreads, maxWorkerThreads, minIOCompletionThreads, maxIOCompletionThreads, clrInstanceID);
// }

// extern "C" void __cdecl LogThreadPoolWorkerThreadAdjustmentSample(double throughput, short clrInstanceID)
// {
//     FireEtwThreadPoolWorkerThreadAdjustmentSample(throughput, clrInstanceID);
// }

// extern "C" void __cdecl LogThreadPoolWorkerThreadAdjustmentAdjustment(double averageThroughput, uint newWorkerThreadCount, uint reason, short clrInstanceID)
// {
//     FireEtwThreadPoolWorkerThreadAdjustmentAdjustment(averageThroughput, newWorkerThreadCount, reason, clrInstanceID);
// }

// extern "C" void __cdecl LogThreadPoolWorkerThreadAdjustmentStats(double duration, double throughput, double threadWave, double throughputWave, double throughputErrorEstimate, double AverageThroughputErrorEstimate, double ThroughputRatio, double confidence, double newControlSetting, short newThreadWaveMagnitude, short ClrInstanceID)
// {
//     FireEtwThreadPoolWorkerThreadAdjustmentStats(duration, throughput, threadWave, throughputWave, throughputErrorEstimate, AverageThroughputErrorEstimate, ThroughputRatio, confidence, newControlSetting, newThreadWaveMagnitude, ClrInstanceID);
// }

// extern "C" void __cdecl LogThreadPoolIOEnqueue(void* nativeOverlapped, void* overlapped, bool multiDequeues, short ClrInstanceID)
// {
//     FireEtwThreadPoolIOEnqueue(nativeOverlapped, overlapped, multiDequeues, ClrInstanceID);
// }

// extern "C" void __cdecl LogThreadPoolIODequeue(void* nativeOverlapped, void* overlapped, short ClrInstanceID)
// {
//     QCALL_CONTRACT;
//     BEGIN_QCALL;

//     FireEtwThreadPoolIODequeue(nativeOverlapped, overlapped, ClrInstanceID);

//     END_QCALL;
// }

// extern "C" void __cdecl LogThreadPoolWorkingThreadCount(uint count, short ClrInstanceID)
// {
//     QCALL_CONTRACT;
//     BEGIN_QCALL;

//     FireEtwThreadPoolWorkingThreadCount(count, ClrInstanceID);

//     END_QCALL;
// }

// extern "C" void __cdecl LogThreadPoolIOPack(void* nativeOverlapped, void* overlapped, short ClrInstanceID)
// {
//     QCALL_CONTRACT;
//     BEGIN_QCALL;

//     FireEtwThreadPoolIOPack(nativeOverlapped, overlapped, ClrInstanceID);

//     END_QCALL;
// }
#endif // FEATURE_PERFTRACING
