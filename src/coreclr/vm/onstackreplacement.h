// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: onstackreplacement.h
//
// ===========================================================================

#ifndef ON_STACK_REPLACEMENT_H
#define ON_STACK_REPLACEMENT_H

#ifdef FEATURE_ON_STACK_REPLACEMENT

#include "daccess.h"
#include "eehash.h"

// PerPatchpointInfo is the runtime state tracked for each active patchpoint.
//
// A patchpoint becomes active when the JIT_HELP_PATCHPOINT helper is invoked
// by jitted code.
//
struct PerPatchpointInfo
{
    PerPatchpointInfo() : 
        m_osrMethodCode(0),
        m_patchpointCount(0),
        m_flags(0)
#if _DEBUG
        , m_patchpointId(0)
#endif
    {
    }

    // Flag bits
    enum 
    {
        patchpoint_triggered = 0x1,
        patchpoint_invalid = 0x2
    };

    // The OSR method entry point for this patchpoint.
    // NULL if no method has yet been jitted, or jitting failed.
    PCODE m_osrMethodCode;
    // Number of times jitted code has called the helper at this patchpoint.
    LONG m_patchpointCount;
    // Status of this patchpoint
    LONG m_flags;

#if _DEBUG
    int m_patchpointId;
#endif
};

typedef DPTR(PerPatchpointInfo) PTR_PerPatchpointInfo;
typedef EEPtrHashTable JitPatchpointTable;

// OnStackReplacementManager keeps track of mapping from patchpoint id to 
// per patchpoint info.
//
// Patchpoint identity is currently the return address of the helper call
//  in the jitted code.
//
class OnStackReplacementManager
{
#if DACCESS_COMPILE
public:
    OnStackReplacementManager(LoaderAllocator *) {};
#else
public:
    static void StaticInitialize();

public:
    OnStackReplacementManager(LoaderAllocator * loaderHeaAllocator);

public:
    PerPatchpointInfo* GetPerPatchpointInfo(PCODE ip);
#endif // DACCESS_COMPILE

private:

    enum
    {
        INITIAL_TABLE_SIZE = 10
    };

    static CrstStatic s_lock;

#if _DEBUG
    static int s_patchpointId;
#endif

private:

    PTR_LoaderAllocator m_allocator;
    JitPatchpointTable m_jitPatchpointTable;
};

#else // FEATURE_TIERED_COMPILATION

class OnStackReplacementManager
{
public:
    static void StaticInitialize() {}
public:

    OnStackReplacementManager(LoaderAllocator *) {}
};

#endif // FEATURE_TIERED_COMPILATION

typedef DPTR(OnStackReplacementManager) PTR_OnStackReplacementManager;

#endif // ON_STACK_REPLACEMENT_H
