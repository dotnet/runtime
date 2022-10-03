// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __GCINFODUMPER_H__
#define __GCINFODUMPER_H__

#include "gcinfotypes.h"
#include "gcinfodecoder.h"

// *****************************************************************************
// WARNING!!!: These values and code are also used by SOS in the diagnostics
// repo. Should updated in a backwards and forwards compatible way.
// See: https://github.com/dotnet/diagnostics/blob/master/src/inc/gcinfodumper.h
// *****************************************************************************

//
// This class dumps the contents of the gc encodings, providing outputs
// similar to the inputs to GcInfoEncoder.  This uses the same GcInfoDecoder
// functions that the EE uses (vs. decoding the bits directly).
//
class GcInfoDumper
{
public:

    GcInfoDumper (GCInfoToken gcInfoToken);
    ~GcInfoDumper ();

    // Returns TRUE to stop decoding.
    typedef BOOL InterruptibleStateChangeProc (
            UINT32 CodeOffset,
            BOOL fInterruptible,
            PVOID pvData);

    // Returns TRUE to stop decoding.
    typedef BOOL OnSafePointProc (
            UINT32 CodeOffset,
            PVOID pvData);

    // Returns TRUE to stop decoding.
    typedef BOOL RegisterStateChangeProc (
            UINT32 CodeOffset,
            UINT32 RegisterNumber,
            GcSlotFlags Flags,
            GcSlotState NewState,
            PVOID pvData);

    // Returns TRUE to stop decoding.
    typedef BOOL StackSlotStateChangeProc (
            UINT32 CodeOffset,
            GcSlotFlags flags,
            GcStackSlotBase BaseRegister,
            SSIZE_T StackOffset,
            GcSlotState NewState,
            PVOID pvData);

    enum EnumerateStateChangesResults
    {
        SUCCESS = 0,
        OUT_OF_MEMORY,
        REPORTED_REGISTER_IN_CALLERS_FRAME,
        REPORTED_FRAME_POINTER,
        REPORTED_INVALID_BASE_REGISTER,
        REPORTED_INVALID_POINTER,
        DECODER_FAILED,
    };

    // Returns TRUE if successful.  FALSE if out of memory, invalid data, etc.
    EnumerateStateChangesResults EnumerateStateChanges (
            InterruptibleStateChangeProc *pfnInterruptibleStateChange,
            RegisterStateChangeProc *pfnRegisterStateChange,
            StackSlotStateChangeProc *pfnStackSlotStateChange,
            OnSafePointProc *pfnSafePointFunc,
            PVOID pvData);

    size_t GetGCInfoSize();

private:

    struct LivePointerRecord
    {
        OBJECTREF *ppObject;
        DWORD flags;
        LivePointerRecord *pNext;
        UINT marked;
    };

    GCInfoToken m_gcTable;
    UINT32 m_StackBaseRegister;
    UINT32 m_SizeOfEditAndContinuePreservedArea;
    LivePointerRecord *m_pRecords;
    RegisterStateChangeProc *m_pfnRegisterStateChange;
    StackSlotStateChangeProc *m_pfnStackSlotStateChange;
    PVOID m_pvCallbackData;
    EnumerateStateChangesResults m_Error;
    size_t m_gcInfoSize;

    static void LivePointerCallback (
            LPVOID          hCallback,      // callback data
            OBJECTREF*      pObject,        // address of object-reference we are reporting
            uint32_t        flags           // is this a pinned and/or interior pointer
            DAC_ARG(DacSlotLocation loc));  // the location the pointer came from

    static void FreePointerRecords (LivePointerRecord *pRecords);

    // Return TRUE if callback requested to stop decoding.
    BOOL ReportPointerRecord (
            UINT32 CodeOffset,
            BOOL fLive,
            REGDISPLAY *pRD,
            LivePointerRecord *pRecord);

    // Return TRUE if callback requested to stop decoding.
    BOOL ReportPointerDifferences (
            UINT32 offset,
            REGDISPLAY *pRD,
            LivePointerRecord *pPrevState);
};


#endif // !__GCINFODUMPER_H__

