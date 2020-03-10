// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
        m_flags(0),
        m_patchpointId(0)
    {
    }

    // Flag bits
    enum 
    {
        patchpoint_triggered = 0x1,
        patchpoint_invalid = 0x2
    };

    PCODE m_osrMethodCode;
    LONG m_patchpointCount;
    LONG m_flags;

    int m_patchpointId;
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
public:
    static void StaticInitialize();

public:
    OnStackReplacementManager();
    
public:
    PerPatchpointInfo* GetPerPatchpointInfo(PCODE ip);

private:

    enum
    {
        INITIAL_TABLE_SIZE = 10
    };

    static int s_patchpointId;
    static CrstStatic s_lock;

private:

    JitPatchpointTable m_jitPatchpointTable;
};

#else // FEATURE_TIERED_COMPILATION

class OnStackReplacementManager
{
public:
    static void StaticInitialize() {}
public:

    OnStackReplacementManager() {}
};

#endif // FEATURE_TIERED_COMPILATION

typedef DPTR(OnStackReplacementManager) PTR_OnStackReplacementManager;

#endif // ON_STACK_REPLACEMENT_H
