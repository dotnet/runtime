// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ******************************************************************************
// WARNING!!!: These values are used by SOS in the diagnostics repo. Values should
// added or removed in a backwards and forwards compatible way.
// See: https://github.com/dotnet/diagnostics/blob/main/src/shared/inc/gcinfo.h
// ******************************************************************************

/*****************************************************************************/
#ifndef _GCINFO_H_
#define _GCINFO_H_
/*****************************************************************************/

#include "daccess.h"

// Use the lower 2 bits of the offsets stored in the tables
// to encode properties

const unsigned        OFFSET_MASK  = 0x3;  // mask to access the low 2 bits

//
//  Note that these definitions should also match the definitions of
//   GC_CALL_INTERIOR and GC_CALL_PINNED in VM/gc.h
//
const unsigned  byref_OFFSET_FLAG  = 0x1;  // the offset is an interior ptr
const unsigned pinned_OFFSET_FLAG  = 0x2;  // the offset is a pinned ptr
#if defined(TARGET_X86)
// JIT32_ENCODER has additional restriction on x86 without funclets: 
// - for untracked locals the flags allowed are "pinned" and "byref"
// - for tracked locals the flags allowed are "this" and "byref"
const unsigned   this_OFFSET_FLAG  = 0x2;  // the offset is "this"
#endif

//-----------------------------------------------------------------------------
// The current GCInfo Version
//-----------------------------------------------------------------------------

#define GCINFO_VERSION 3

//-----------------------------------------------------------------------------
// GCInfoToken: A wrapper that contains the GcInfo data and version number.
//
// The version# is not stored in the GcInfo structure -- because it is
// wasteful to store the version once for every method.
// Instead, the version# istracked per range-section of generated/loaded methods.
//
// The GCInfo version is computed as :
// 1) The current GCINFO_VERSION for JITted and Ngened images
// 2) A function of the Ready - to - run major version stored in READYTORUN_HEADER
//   for ready - to - run images.ReadyToRunJitManager::JitTokenToGCInfoVersion()
//   provides the GcInfo version for any Method.
//-----------------------------------------------------------------------------

struct GCInfoToken
{
    PTR_VOID Info;
    uint32_t Version;

#ifdef FEATURE_NATIVEAOT
    GCInfoToken(PTR_VOID info)
    {
        Info = info;
        Version = GCINFO_VERSION;
    }
#endif

    static uint32_t ReadyToRunVersionToGcInfoVersion(uint32_t readyToRunMajorVersion, uint32_t readyToRunMinorVersion)
    {
        // Once MINIMUM_READYTORUN_MAJOR_VERSION is bumped to 10+
        // delete the following and just return GCINFO_VERSION
        //
        // R2R 9.0 and 9.1 use GCInfo v2
        // R2R 9.2 uses GCInfo v3
        if (readyToRunMajorVersion == 9 && readyToRunMinorVersion < 2)
            return 2;

        return GCINFO_VERSION;
    }
};

/*****************************************************************************/
#endif //_GCINFO_H_
/*****************************************************************************/
