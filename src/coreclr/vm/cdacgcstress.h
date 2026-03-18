// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// cdacgcstress.h
//
// Infrastructure for verifying cDAC stack reference reporting against the
// runtime's own GC root enumeration at GC stress instruction-level trigger points.
//
// Enabled via GCSTRESS_CDAC (0x20) flag in DOTNET_GCStress.
//

#ifndef _CDAC_GC_STRESS_H_
#define _CDAC_GC_STRESS_H_

#ifdef HAVE_GCCOVER

// Forward declarations
class Thread;

class CdacGcStress
{
public:
    // Initialize the cDAC in-process for GC stress verification.
    // Must be called after the contract descriptor is built and GC is initialized.
    // Returns true if initialization succeeded.
    static bool Initialize();

    // Shutdown and release cDAC resources.
    static void Shutdown();

    // Returns true if cDAC GC stress verification is initialized and ready.
    static bool IsInitialized();

    // Returns true if GCSTRESS_CDAC flag is set in the GCStress level.
    static bool IsEnabled();

    // Main entry point: verify cDAC stack refs match runtime stack refs at a GC stress point.
    // Called from DoGcStress before StressHeap().
    //   pThread - the thread being stress-tested
    //   regs    - the register context at the stress point
    static void VerifyAtStressPoint(Thread* pThread, PCONTEXT regs);

    // Verify at an allocation stress point. Captures the current thread context
    // and calls VerifyAtStressPoint. Called from the allocation path when
    // GCSTRESS_CDAC is enabled with allocation-based stress (0x1 + 0x20).
    static void VerifyAtAllocPoint();

    // Returns true if this stress point should be skipped based on the step interval
    // (DOTNET_GCStressCdacStep). When true, the caller should skip both cDAC verification
    // AND StressHeap to reduce overhead while maintaining code path diversity.
    static bool ShouldSkipStressPoint();
};

#endif // HAVE_GCCOVER
#endif // _CDAC_GC_STRESS_H_
