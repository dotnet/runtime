// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// CdacStress.h
//
// Infrastructure for verifying cDAC stack reference reporting against the
// runtime's own GC root enumeration at stress trigger points.
//
// Enabled via DOTNET_CdacStress (bit flags) or legacy DOTNET_GCStress=0x20.
//

#ifndef _CDAC_STRESS_H_
#define _CDAC_STRESS_H_

// Trigger points for cDAC stress verification.
enum cdac_trigger_points
{
    cdac_on_alloc,      // Verify at allocation points
    cdac_on_gc,         // Verify at GC trigger points
    cdac_on_instr,      // Verify at instruction-level stress points (needs GCStress=0x4)
};

#ifdef HAVE_GCCOVER

// Bit flags for DOTNET_CdacStress configuration.
//
// Low nibble:  WHERE to trigger verification
// High nibble: WHAT to validate
// Modifier:    HOW to filter
enum CdacStressFlags : DWORD
{
    // Trigger points (low nibble — where stress fires)
    CDACSTRESS_ALLOC        = 0x1,    // Verify at allocation points
    CDACSTRESS_GC           = 0x2,    // Verify at GC trigger points (future)
    CDACSTRESS_INSTR        = 0x4,    // Verify at instruction stress points (needs GCStress=0x4)

    // Validation types (high nibble — what to check)
    CDACSTRESS_REFS         = 0x10,   // Compare GC stack references
    CDACSTRESS_WALK         = 0x20,   // Compare IXCLRDataStackWalk frame-by-frame
    CDACSTRESS_USE_DAC      = 0x40,   // Also load legacy DAC and compare cDAC against it

    // Modifiers
    CDACSTRESS_UNIQUE       = 0x100,  // Only verify on unique (IP, SP) pairs
};

// Forward declarations
class Thread;

// Accessor for the resolved stress level — called by template specializations.
DWORD GetCdacStressLevel();

class CdacStress
{
public:
    static bool Initialize();
    static void Shutdown();
    static bool IsInitialized();

    // Returns true if cDAC stress is enabled via DOTNET_CdacStress or legacy GCSTRESS_CDAC.
    static bool IsEnabled();

    // Template-based trigger point check, following the GCStress<T> pattern.
    template <enum cdac_trigger_points>
    static bool IsEnabled();

    // Returns true if unique-stack filtering is active.
    static bool IsUniqueEnabled();

    // Verify at a stress point if the given trigger is enabled and not skipped.
    // Follows the GCStress<T>::MaybeTrigger pattern — call sites are one-liners.
    template <enum cdac_trigger_points tp>
    FORCEINLINE static void MaybeVerify(Thread* pThread, PCONTEXT regs)
    {
        if (IsEnabled<tp>() && !ShouldSkipStressPoint())
            VerifyAtStressPoint(pThread, regs);
    }

    // Allocation-point variant: captures thread context automatically.
    template <enum cdac_trigger_points tp>
    FORCEINLINE static void MaybeVerify()
    {
        if (IsEnabled<tp>() && !ShouldSkipStressPoint())
            VerifyAtAllocPoint();
    }

    // Main entry point: verify cDAC stack refs match runtime stack refs.
    static void VerifyAtStressPoint(Thread* pThread, PCONTEXT regs);

    // Verify at an allocation point. Captures current thread context.
    static void VerifyAtAllocPoint();

    // Returns true if this stress point should be skipped (step throttling).
    static bool ShouldSkipStressPoint();
};

template<> FORCEINLINE bool CdacStress::IsEnabled<cdac_on_alloc>()
{
    return IsInitialized() && (GetCdacStressLevel() & CDACSTRESS_ALLOC) != 0;
}

template<> FORCEINLINE bool CdacStress::IsEnabled<cdac_on_gc>()
{
    return IsInitialized() && (GetCdacStressLevel() & CDACSTRESS_GC) != 0;
}

template<> FORCEINLINE bool CdacStress::IsEnabled<cdac_on_instr>()
{
    return IsInitialized() && (GetCdacStressLevel() & CDACSTRESS_INSTR) != 0;
}

#else // !HAVE_GCCOVER

// Stub when HAVE_GCCOVER is not defined — all calls compile to nothing.
class CdacStress
{
public:
    template <enum cdac_trigger_points tp>
    FORCEINLINE static void MaybeVerify(Thread* pThread, PCONTEXT regs) { }
    template <enum cdac_trigger_points tp>
    FORCEINLINE static void MaybeVerify() { }
};

#endif // HAVE_GCCOVER
#endif // _CDAC_STRESS_H_
