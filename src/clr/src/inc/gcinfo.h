// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/*****************************************************************************/
#ifndef _GCINFO_H_
#define _GCINFO_H_
/*****************************************************************************/

#include "daccess.h"
#include "windef.h"     // For BYTE

// Some declarations in this file are used on non-x86 platforms, but most are x86-specific.

// Use the lower 2 bits of the offsets stored in the tables
// to encode properties

const unsigned        OFFSET_MASK  = 0x3;  // mask to access the low 2 bits

//
//  Note for untracked locals the flags allowed are "pinned" and "byref"
//   and for tracked locals the flags allowed are "this" and "byref"
//  Note that these definitions should also match the definitions of
//   GC_CALL_INTERIOR and GC_CALL_PINNED in VM/gc.h
//
const unsigned  byref_OFFSET_FLAG  = 0x1;  // the offset is an interior ptr
const unsigned pinned_OFFSET_FLAG  = 0x2;  // the offset is a pinned ptr
const unsigned   this_OFFSET_FLAG  = 0x2;  // the offset is "this"

//-----------------------------------------------------------------------------
// The current GCInfo Version
//-----------------------------------------------------------------------------

#define GCINFO_VERSION 1

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
//   provides the GcInfo version for any Method.Currently, there's only one
//   version of GCInfo.
//-----------------------------------------------------------------------------

struct GCInfoToken
{
    PTR_VOID Info;
    UINT32 Version;
};

/*****************************************************************************/
#endif //_GCINFO_H_
/*****************************************************************************/
