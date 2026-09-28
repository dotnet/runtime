// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// CdacStress.h
//
// Infrastructure for verifying cDAC stack reference reporting against the
// runtime's own GC root enumeration at stress trigger points.
//
// Enabled via DOTNET_CdacStress
//

#ifndef _CDAC_STRESS_H_
#define _CDAC_STRESS_H_

// Forward declarations
class Thread;

// Trigger points for cDAC stress verification.
enum cdac_trigger_points
{
    cdac_on_alloc,      // Verify at allocation points (gchelpers.cpp)
};

namespace CdacStressPolicy
{
    // Initialize the cDAC stress framework. No-op if DOTNET_CdacStress is unset.
    // Idempotent. Called once early in EE startup.
    void Initialize();

    // Tear down the framework, release the cDAC reader, flush logs, print summary.
    void Shutdown();
}

#if defined(CDAC_STRESS) && !defined(DACCESS_COMPILE)

// Per-trigger class template. The primary template is intentionally empty --
// only the explicit specializations below are usable.
template <cdac_trigger_points tp>
class CdacStress;

template<>
class CdacStress<cdac_on_alloc>
{
public:
    // Returns true if alloc-point cDAC stress is enabled (DOTNET_CdacStress has CDACSTRESS_ALLOC).
    static bool IsEnabled();

    // Verify cDAC stack refs at an allocation point. Captures the current thread
    // context internally and walks past the caller's frame.
    static void MaybeVerify();
};

#else // active in runtime only

// Stubs for DAC builds and !CDAC_STRESS -- all calls compile to nothing.
template <cdac_trigger_points tp>
class CdacStress
{
public:
    FORCEINLINE static bool IsEnabled() { return false; }
    FORCEINLINE static void MaybeVerify() { }
};

#endif // CDAC_STRESS && !DACCESS_COMPILE
#endif // _CDAC_STRESS_H_
