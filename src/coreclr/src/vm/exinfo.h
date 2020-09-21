// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//


#ifndef __ExInfo_h__
#define __ExInfo_h__
#if !defined(FEATURE_EH_FUNCLETS)

#include "exstatecommon.h"

typedef DPTR(class ExInfo) PTR_ExInfo;
class ExInfo
{
    friend class ThreadExceptionState;
    friend class ClrDataExceptionState;

public:

    BOOL    IsHeapAllocated()
    {
        LIMITED_METHOD_CONTRACT;
        return m_StackAddress != (void *) this;
    }

    void CopyAndClearSource(ExInfo *from);

    void UnwindExInfo(VOID* limit);

    // Q: Why does this thing take an EXCEPTION_RECORD rather than an ExceptionCode?
    // A: Because m_ExceptionCode and Ex_WasThrownByUs have to be kept
    //    in sync and this function needs the exception parms inside the record to figure
    //    out the "IsTagged" part.
    void SetExceptionCode(const EXCEPTION_RECORD *pCER);

    DWORD GetExceptionCode()
    {
        LIMITED_METHOD_CONTRACT;
        return m_ExceptionCode;
    }

public:  // @TODO: make more of these private!
    // Note: the debugger assumes that m_pThrowable is a strong
    // reference so it can check it for NULL with preemptive GC
    // enabled.
    OBJECTHANDLE    m_hThrowable;       // thrown exception
    PTR_Frame       m_pSearchBoundary;  // topmost frame for current managed frame group
private:
    DWORD           m_ExceptionCode;    // After a catch of a COM+ exception, pointers/context are trashed.
public:
    PTR_EXCEPTION_REGISTRATION_RECORD m_pBottomMostHandler; // most recent EH record registered

    // Reference to the topmost handler we saw during an SO that goes past us
    PTR_EXCEPTION_REGISTRATION_RECORD m_pTopMostHandlerDuringSO;

    LPVOID              m_dEsp;             // Esp when  fault occurred, OR esp to restore on endcatch

    StackTraceInfo      m_StackTraceInfo;

    PTR_ExInfo          m_pPrevNestedInfo;  // pointer to nested info if are handling nested exception

    size_t*             m_pShadowSP;        // Zero this after endcatch

    PTR_EXCEPTION_RECORD    m_pExceptionRecord;
    PTR_EXCEPTION_POINTERS  m_pExceptionPointers;
    PTR_CONTEXT             m_pContext;

    // We have a rare case where (re-entry to the EE from an unmanaged filter) where we
    // need to create a new ExInfo ... but don't have a nested handler for it.  The handlers
    // use stack addresses to figure out their correct lifetimes.  This stack location is
    // used for that.  For most records, it will be the stack address of the ExInfo ... but
    // for some records, it will be a pseudo stack location -- the place where we think
    // the record should have been (except for the re-entry case).
    //
    //
    //
    void* m_StackAddress; // A pseudo or real stack location for this record.

#ifndef TARGET_UNIX
private:
    EHWatsonBucketTracker m_WatsonBucketTracker;
public:
    inline PTR_EHWatsonBucketTracker GetWatsonBucketTracker()
    {
        LIMITED_METHOD_CONTRACT;
        return PTR_EHWatsonBucketTracker(PTR_HOST_MEMBER_TADDR(ExInfo, this, m_WatsonBucketTracker));
    }
#endif

private:
    BOOL                    m_fDeliveredFirstChanceNotification;
public:
    inline BOOL DeliveredFirstChanceNotification()
    {
        LIMITED_METHOD_CONTRACT;

        return m_fDeliveredFirstChanceNotification;
    }

    inline void SetFirstChanceNotificationStatus(BOOL fDelivered)
    {
        LIMITED_METHOD_CONTRACT;

        m_fDeliveredFirstChanceNotification = fDelivered;
    }

    // Returns the exception tracker previous to the current
    inline PTR_ExInfo GetPreviousExceptionTracker()
    {
        LIMITED_METHOD_CONTRACT;

        return m_pPrevNestedInfo;
    }

    // Returns the throwable associated with the tracker
    inline OBJECTREF GetThrowable()
    {
        LIMITED_METHOD_CONTRACT;

        return (m_hThrowable != NULL)?ObjectFromHandle(m_hThrowable):NULL;
    }

    // Returns the throwble associated with the tracker as handle
    inline OBJECTHANDLE GetThrowableAsHandle()
    {
        LIMITED_METHOD_CONTRACT;

        return m_hThrowable;
    }

public:

    DebuggerExState     m_DebuggerExState;
    EHClauseInfo        m_EHClauseInfo;
    ExceptionFlags      m_ExceptionFlags;

#if defined(TARGET_X86) && defined(DEBUGGING_SUPPORTED)
    EHContext           m_InterceptionContext;
    BOOL                m_ValidInterceptionContext;
#endif

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

    void Init();
    ExInfo() DAC_EMPTY();

    void DestroyExceptionHandle();

private:
    // Don't allow this
    ExInfo& operator=(const ExInfo &from);
};

#if defined(TARGET_X86)
PTR_ExInfo GetEHTrackerForPreallocatedException(OBJECTREF oPreAllocThrowable, PTR_ExInfo pStartingEHTracker);
#endif // TARGET_X86

#endif // !FEATURE_EH_FUNCLETS
#endif // __ExInfo_h__
