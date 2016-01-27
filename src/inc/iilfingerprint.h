// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: IILFingerprint.h
// ===========================================================================

#ifndef __IILFingerprint_h__
#define __IILFingerprint_h__

#include "cor.h"

//**********************************************************************
// IILFingerprint:
//
// When ngen images are present, the CLR often avoids opening IL files altogether while
// still making assumptions about their contents (in particular when binding native images.)
// This raise a vulnerability where someone may overwrite the IL during a running process.
// Should the runtime need to delay-load the IL file (e.g. in an IJW app), it may find to
// its dismay that the IL file's contents have changed, or were out of sync with the native image
// cache's data all along.
//
// To address this, the CLR maintains a process-wide "ILFingerprint" object for each IL file with a unique path.
// As the process runs, and components (i.e. native binder) makes assumptions about the contents
// of the unopened IL file, the fingerprint collects these assumptions and ensures that everyone
// work off the same assumptions.
//
// If the file is ever opened, the fingerprint reads the actual data from the IL file and compares
// it to the stored assumptions. If they are conflict, the runtime raises a torn state condition
// and refuses to load the file.
//
// Data model:
//
//   The fingerprint is conceptually a property bag. For efficiency purposes, each property type
//   is associated with a fixed-size memory blob with non-customizable copy and compare operations
//   (they are memcpy and memcmp, respectively.)
//
//   This allows for efficient operation (native binding is perf sensitive) at the expense of
//   some flexibility. (Given the free-for-all that IAssemblyNames became when it became too accomodating a
//   property bag, that inflexibility is by design, actually.)
//
// Implementation:
//   The actual implementation of ILFingerprint is the PEFingerprint class, implemented in VM\PEFingerprint.cpp.
//   Since PEImages are already memoized by pathname, they serve as the fingerprint custodian.


typedef enum
{
    ILFTagTimestamp           = 0,   // datatype == FILETIME (8 bytes)
    ILFTagSize                = 1,   // datatype == DWORD (4 bytes)
    ILFTagMvid                = 2,   // datatype == GUID (16 bytes)

    // NB: If you add or change constants here, you must update PEFingerprint::s_offsets and PEFingerprint::s_sizes in PEFingerprint.cpp

    ILFTagCount               = 3,   // used for range verification
} ILFingerprintTag;

typedef struct
{
    ILFingerprintTag _tag;
    LPCVOID          _data;
} ILFingerprintComponent;

interface IILFingerprint
{
  public:
    //---------------------------------------------------------------------------------------------
    // Lifetime management methods.
    //---------------------------------------------------------------------------------------------
    STDMETHOD_(ULONG, AddRef)() = 0;
    STDMETHOD_(ULONG, Release)() = 0;

    //---------------------------------------------------------------------------------------------
    // Convenience fcn: equivalent to calling CommitAndCompareMulti() with one component.
    //---------------------------------------------------------------------------------------------
    STDMETHOD_(BOOL, CommitAndCompare)(
        ILFingerprintTag componentType,
        LPCVOID data) = 0;

    //---------------------------------------------------------------------------------------------
    // CommitAndCompareMulti(): Atomically commits one or more fingerprint components into
    // the fingerprint. Once a component is committed, its value can never change.
    //
    // An attempt to commit a component succeeds only if the component was not already committed
    // or the prior value maches the new one exactly.
    //
    // Calling CommitAndCompare() multiple times is not equivalent to calling CommitAndCompareMulti().
    // CommitAndCompareMulti() is atomic - either all the commits happen or none of them do.
    //
    // Returns:
    //    TRUE:  All passed components committed successful.
    //    FALSE: At leat one component failed to commit successfully.
    //---------------------------------------------------------------------------------------------
    STDMETHOD_(BOOL, CommitAndCompareMulti)(
        UINT numComponents,
        const ILFingerprintComponent *pComponents) = 0;


    //---------------------------------------------------------------------------------------------
    // LockAndLoadIL()
    //
    //   Forces the runtime to open the IL file and lock it against future overwrites. This
    //   is bad for working set so this should be avoided.
    //
    //   Once opened and locked, this method extracts the actual fingerprint from the IL file
    //   and attempts to commit it into the ILFingerprint. If successful, all future commits
    //   will now be compared against this trusted data. If unsuccessful, this is a torn state
    //   situation and LockAndLoadIL() throws the torn state exception.
    //---------------------------------------------------------------------------------------------
    STDMETHOD_(VOID, LockAndLoadIL)() = 0;
};

interface IILFingerprintFactory
{
  public:
    //---------------------------------------------------------------------------------------------
    // Lifetime management methods.
    //---------------------------------------------------------------------------------------------
    STDMETHOD_(ULONG, AddRef)() = 0;
    STDMETHOD_(ULONG, Release)() = 0;

    STDMETHOD(GetILFingerprintForPath)(
        LPCWSTR path, 
        IILFingerprint **ppFingerprint) = 0;
};

#endif // __IILFingerprint_h__

