// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/* ------------------------------------------------------------------------- *
 * DbgIPCEvents.h -- header file for private Debugger data shared by various
//

 *                   debugger components.
 * ------------------------------------------------------------------------- */

#ifndef _DbgIPCEvents_h_
#define _DbgIPCEvents_h_

#include <new.hpp>
#include <cor.h>
#include <cordebug.h>
#include <corjit.h> // for ICorDebugInfo::VarLocType & VarLoc
#include <specstrings.h>

#include "dbgtargetcontext.h"


// Get version numbers for IPCHeader stamp
#include "clrversion.h"
#include "dbgappdomain.h"

#include "./common.h"

//-----------------------------------------------------------------------------
// V3 additions to IPC protocol between LS and RS.
//-----------------------------------------------------------------------------

// Special Exception code for LS to communicate with RS.
// LS will raise this exception to communicate managed debug events to the RS.
// Exception codes can't use bit 0x10000000, that's reserved by OS.
#define CLRDBG_NOTIFICATION_EXCEPTION_CODE  ((DWORD) 0x04242420)

// This is exception argument 0 included in debugger notification events.
// The debugger uses this as a sanity check.
// This could be very volatile data that changes between builds.
#define CLRDBG_EXCEPTION_DATA_CHECKSUM ((DWORD) 0x31415927)


// Reasons for hijack.
namespace EHijackReason
{
    enum EHijackReason
    {
        kUnhandledException = 1,
        kM2UHandoff = 2,
        kFirstChanceSuspend = 3,
        kGenericHijack = 4,
        kMax
    };
    inline bool IsValid(EHijackReason value)
    {
        SUPPORTS_DAC;
        return (value > 0) && (value < kMax);
    }
}



#define     MAX_LOG_SWITCH_NAME_LEN     256

//-----------------------------------------------------------------------------
// Versioning note:
// This file describes the IPC communication protocol between the LS (mscorwks)
// and the RS (mscordbi). For Desktop builds, it is private and can change on a
// daily basis. The version of the LS will always match the version of the RS
// (but see the discussion of CoreCLR below). They are like a single conceptual
// DLL split across 2 processes.
// The only restriction is that it should be flavor agnostic - so don't change
// layout based off '#ifdef DEBUG'. This lets us drop a Debug flavor RS onto
// a retail installation w/o any further installation woes. That's very useful
// for debugging.
//-----------------------------------------------------------------------------


// We want this available for DbgInterface.h - put it here.
typedef enum
{
    IPC_TARGET_OUTOFPROC,
    IPC_TARGET_COUNT,
} IpcTarget;

//
// Names of the setup sync event and shared memory used for IPC between the Left Side and the Right Side. NOTE: these
// names must include a %d for the process id. The process id used is the process id of the debuggee.
//

#define CorDBIPCSetupSyncEventName W("CorDBIPCSetupSyncEvent_%d")

//
// This define controls whether we always pass first chance exceptions to the in-process first chance hijack filter
// during interop debugging or if we try to short-circuit and make the decision out-of-process as much as possible.
//
#define CorDB_Short_Circuit_First_Chance_Ownership 1

//
// Defines for current version numbers for the left and right sides
//
#define CorDB_LeftSideProtocolCurrent           2
#define CorDB_LeftSideProtocolMinSupported      2
#define CorDB_RightSideProtocolCurrent          2
#define CorDB_RightSideProtocolMinSupported     2

//
// The remaining data structures in this file can be shared between two processes and for network transport
// based debugging this can mean two different platforms as well.  The two platforms that can share these
// data structures must have identical layouts for them (each field must lie at the same offset and have the
// same length). The MSLAYOUT macro should be applied to each structure to avoid any compiler packing differences.
//

//
// DebuggerIPCRuntimeOffsets contains addresses and offsets of important global variables, functions, and fields in
// Runtime objects. This is populated during Left Side initialization and is read by the Right Side. This struct is
// mostly to facilitate unmanaged debugging support, but it may have some small uses for managed debugging.
//
struct MSLAYOUT DebuggerIPCRuntimeOffsets
{
#ifdef FEATURE_INTEROP_DEBUGGING
    void   *m_genericHijackFuncAddr;
    void   *m_signalHijackStartedBPAddr;
    void   *m_excepForRuntimeHandoffStartBPAddr;
    void   *m_excepForRuntimeHandoffCompleteBPAddr;
    void   *m_signalHijackCompleteBPAddr;
    void   *m_excepNotForRuntimeBPAddr;
    void   *m_notifyRSOfSyncCompleteBPAddr;
    DWORD   m_debuggerWordTLSIndex;                     // The TLS slot for the debugger word used in the debugger hijack functions
#endif // FEATURE_INTEROP_DEBUGGING
    SIZE_T  m_TLSIndex;                                 // The TLS index of the thread-local storage for coreclr.dll
    SIZE_T  m_TLSEEThreadOffset;                        // TLS Offset of the Thread pointer.
    SIZE_T  m_TLSIsSpecialOffset;                       // TLS Offset of the "IsSpecial" status for a thread.
    SIZE_T  m_TLSCantStopOffset;                        // TLS Offset of the Can't-Stop count.
    SIZE_T  m_EEThreadStateOffset;                      // Offset of m_state in a Thread
    SIZE_T  m_EEThreadStateNCOffset;                    // Offset of m_stateNC in a Thread
    SIZE_T  m_EEThreadPGCDisabledOffset;                // Offset of the bit for whether PGC is disabled or not in a Thread
    DWORD   m_EEThreadPGCDisabledValue;                 // Value at m_EEThreadPGCDisabledOffset that equals "PGC disabled".
    SIZE_T  m_EEThreadFrameOffset;                      // Offset of the Frame ptr in a Thread
    SIZE_T  m_EEThreadMaxNeededSize;                    // Max memory to read to get what we need out of a Thread object
    DWORD   m_EEThreadSteppingStateMask;                // Mask for Thread::TSNC_DebuggerIsStepping
    DWORD   m_EEMaxFrameValue;                          // The max Frame value
    SIZE_T  m_EEThreadDebuggerFilterContextOffset;      // Offset of debugger's filter context within a Thread Object.
    SIZE_T  m_EEFrameNextOffset;                        // Offset of the next ptr in a Frame
    DWORD   m_EEIsManagedExceptionStateMask;            // Mask for Thread::TSNC_DebuggerIsManagedException
    void   *m_pPatches;                                 // Addr of patch table
    BOOL   *m_pPatchTableValid;                         // Addr of g_patchTableValid
    SIZE_T  m_offRgData;                                // Offset of m_pcEntries
    SIZE_T  m_offCData;                                 // Offset of count of m_pcEntries
    SIZE_T  m_cbPatch;                                  // Size per patch entry
    SIZE_T  m_offAddr;                                  // Offset within patch of target addr
    SIZE_T  m_offOpcode;                                // Offset within patch of target opcode
    SIZE_T  m_cbOpcode;                                 // Max size of opcode
    SIZE_T  m_offTraceType;                             // Offset of the trace.type within a patch
    DWORD   m_traceTypeUnmanaged;                       // TRACE_UNMANAGED

    DebuggerIPCRuntimeOffsets()
    {
        ZeroMemory(this, sizeof(DebuggerIPCRuntimeOffsets));
    }
};

//
// The size of the send and receive IPC buffers.
// These must be big enough to fit a DebuggerIPCEvent. Also, the bigger they are, the fewer events
// it takes to send variable length stuff like the stack trace.
// But for perf reasons, they need to be small enough to not just push us over a page boundary in an IPC block.
// Unfortunately, there's a lot of other goo in the IPC block, so we can't use some clean formula. So we
// have to resort to just tuning things.
//

// When using a network transport rather than shared memory buffers CorDBIPC_BUFFER_SIZE is the upper bound
// for a single DebuggerIPCEvent structure. This now relates to the maximal size of a network message and is
// orthogonal to the host's page size. Because of this we defer definition of CorDBIPC_BUFFER_SIZE until we've
// declared DebuggerIPCEvent at the end of this header (and we can do so because in the transport case there
// aren't any embedded buffers in the DebuggerIPCControlBlock).

#if defined(TARGET_X86) || defined(TARGET_ARM)
#ifdef HOST_64BIT
#define CorDBIPC_BUFFER_SIZE 2104
#else
#define CorDBIPC_BUFFER_SIZE 2092
#endif
#else  // !TARGET_X86 && !TARGET_ARM
// This is the size of a DebuggerIPCEvent.  You will hit an assert in Cordb::Initialize() (di\rsmain.cpp)
// if this is not defined correctly.  AMD64 actually has a page size of 0x1000, not 0x2000.
#define CorDBIPC_BUFFER_SIZE 4016 // (4016 + 6) * 2 + 148 = 8192 (two (DebuggerIPCEvent + alignment padding) + other fields = page size)
#endif // TARGET_X86 || TARGET_ARM

//
// DebuggerIPCControlBlock describes the layout of the shared memory shared between the Left Side and the Right
// Side. This includes error information, handles for the IPC channel, and space for the send/receive buffers.
//
struct MSLAYOUT DebuggerIPCControlBlock
{
    // Version data should be first in the control block to ensure that we can read it even if the control block
    // changes.
    SIZE_T                     m_DCBSize;           // note this field is used as a semaphore to indicate the DCB is initialized
    ULONG                      m_verMajor;          // CLR build number for the Left Side.
    ULONG                      m_verMinor;          // CLR build number for the Left Side.

    // This next stuff fits in a  DWORD.
    bool                       m_checkedBuild;      // CLR build type for the Left Side.
    // using the first padding byte to indicate if hosted in fiber mode.
    // We actually just need one bit. So if needed, can turn this to a bit.
    // BYTE padding1;
    bool                       m_bHostingInFiber;
    BYTE padding2;
    BYTE padding3;

    ULONG                      m_leftSideProtocolCurrent;       // Current protocol version for the Left Side.
    ULONG                      m_leftSideProtocolMinSupported;  // Minimum protocol the Left Side can support.

    ULONG                      m_rightSideProtocolCurrent;      // Current protocol version for the Right Side.
    ULONG                      m_rightSideProtocolMinSupported; // Minimum protocol the Right Side requires.

    HRESULT                    m_errorHR;
    unsigned int               m_errorCode;

#if defined(TARGET_64BIT)
    // 64-bit needs this padding to make the handles after this aligned.
    // But x86 can't have this padding b/c it breaks binary compatibility between v1.1 and v2.0.
    ULONG padding4;
#endif // TARGET_64BIT


    RemoteHANDLE               m_rightSideEventAvailable;
    RemoteHANDLE               m_rightSideEventRead;

    // @dbgtodo  inspection - this is where LSEA and LSER used to be. We need to the padding to maintain binary compatibility.
    // Eventually, we expect to remove this whole block.
    RemoteHANDLE               m_paddingObsoleteLSEA;
    RemoteHANDLE               m_paddingObsoleteLSER;

    RemoteHANDLE               m_rightSideProcessHandle;

    //.............................................................................
    // Everything above this point must have the exact same binary layout as v1.1.
    // See protocol details below.
    //.............................................................................

    RemoteHANDLE               m_leftSideUnmanagedWaitEvent;



    // This is set immediately when the helper thread is created.
    // This will be set even if there's a temporary helper thread or if the real helper
    // thread is not yet pumping (eg, blocked on a loader lock).
    DWORD                      m_realHelperThreadId;

    // This is only published once the helper thread starts running in its main loop.
    // Thus we can use this field to see if the real helper thread is actually pumping.
    DWORD                      m_helperThreadId;

    // This is non-zero if the LS has a temporary helper thread.
    DWORD                      m_temporaryHelperThreadId;

    // ID of the Helper's canary thread.
    DWORD                      m_CanaryThreadId;

    DebuggerIPCRuntimeOffsets *m_pRuntimeOffsets;
    void                      *m_helperThreadStartAddr;
    void                      *m_helperRemoteStartAddr;
    DWORD                     *m_specialThreadList;

    BYTE                       m_receiveBuffer[CorDBIPC_BUFFER_SIZE];
    BYTE                       m_sendBuffer[CorDBIPC_BUFFER_SIZE];

    DWORD                      m_specialThreadListLength;
    bool                       m_shutdownBegun;
    bool                       m_rightSideIsWin32Debugger;  // RS status
    bool                       m_specialThreadListDirty;

    bool                       m_rightSideShouldCreateHelperThread;

    // NOTE The Init method works since there are no virtual functions - don't add any virtual functions without
    // changing this!
    // Only initialized by the LS, opened by the RS.
    HRESULT Init(
                 HANDLE rsea,
                 HANDLE rser,
                 HANDLE lsea,
                 HANDLE lser,
                 HANDLE lsuwe
                );

};

#if defined(FEATURE_DBGIPC_TRANSPORT_VM) || defined(FEATURE_DBGIPC_TRANSPORT_DI)

// We need an alternate definition for the control block if using the transport, because the control block has to be sent over the transport
// In particular we can't nest the send/receive buffers inside of it and we don't use any of the remote handles

struct MSLAYOUT DebuggerIPCControlBlockTransport
{
    // Version data should be first in the control block to ensure that we can read it even if the control block
    // changes.
    SIZE_T                     m_DCBSize;           // note this field is used as a semaphore to indicate the DCB is initialized
    ULONG                      m_verMajor;          // CLR build number for the Left Side.
    ULONG                      m_verMinor;          // CLR build number for the Left Side.

    // This next stuff fits in a  DWORD.
    bool                       m_checkedBuild;      // CLR build type for the Left Side.
    // using the first padding byte to indicate if hosted in fiber mode.
    // We actually just need one bit. So if needed, can turn this to a bit.
    // BYTE padding1;
    bool                       m_bHostingInFiber;
    BYTE padding2;
    BYTE padding3;

    ULONG                      m_leftSideProtocolCurrent;       // Current protocol version for the Left Side.
    ULONG                      m_leftSideProtocolMinSupported;  // Minimum protocol the Left Side can support.

    ULONG                      m_rightSideProtocolCurrent;      // Current protocol version for the Right Side.
    ULONG                      m_rightSideProtocolMinSupported; // Minimum protocol the Right Side requires.

    HRESULT                    m_errorHR;
    unsigned int               m_errorCode;

#if defined(TARGET_64BIT)
    // 64-bit needs this padding to make the handles after this aligned.
    // But x86 can't have this padding b/c it breaks binary compatibility between v1.1 and v2.0.
    ULONG padding4;
#endif // TARGET_64BIT

    // This is set immediately when the helper thread is created.
    // This will be set even if there's a temporary helper thread or if the real helper
    // thread is not yet pumping (eg, blocked on a loader lock).
    DWORD                      m_realHelperThreadId;

    // This is only published once the helper thread starts running in its main loop.
    // Thus we can use this field to see if the real helper thread is actually pumping.
    DWORD                      m_helperThreadId;

    // This is non-zero if the LS has a temporary helper thread.
    DWORD                      m_temporaryHelperThreadId;

    // ID of the Helper's canary thread.
    DWORD                      m_CanaryThreadId;

    DebuggerIPCRuntimeOffsets *m_pRuntimeOffsets;
    void                      *m_helperThreadStartAddr;
    void                      *m_helperRemoteStartAddr;
    DWORD                     *m_specialThreadList;

    DWORD                      m_specialThreadListLength;
    bool                       m_shutdownBegun;
    bool                       m_rightSideIsWin32Debugger;  // RS status
    bool                       m_specialThreadListDirty;

    bool                       m_rightSideShouldCreateHelperThread;

    // NOTE The Init method works since there are no virtual functions - don't add any virtual functions without
    // changing this!
    // Only initialized by the LS, opened by the RS.
    HRESULT Init();

};

#endif // defined(FEATURE_DBGIPC_TRANSPORT_VM) || defined(FEATURE_DBGIPC_TRANSPORT_DI)

#if defined(FEATURE_DBGIPC_TRANSPORT_VM) || defined(FEATURE_DBGIPC_TRANSPORT_DI)
#include "dbgtransportsession.h"
#endif // defined(FEATURE_DBGIPC_TRANSPORT_VM) || defined(FEATURE_DBGIPC_TRANSPORT_DI)

#define INITIAL_APP_DOMAIN_INFO_LIST_SIZE   16


//-----------------------------------------------------------------------------
// Provide some Type-safety in the IPC block when we pass remote pointers around.
//-----------------------------------------------------------------------------


//-----------------------------------------------------------------------------
// This is the same in both the LS & RS.
// Definitions on the LS & RS should be binary compatible. So all storage is
// declared in GeneralLsPointer, and then the Ls & RS each have their own
// derived accessors.
//-----------------------------------------------------------------------------
class MSLAYOUT GeneralLsPointer
{
protected:
    friend ULONG_PTR LsPtrToCookie(GeneralLsPointer p);
    void * m_ptr;

public:
    bool IsNull() { return m_ptr == NULL; }
};

class MSLAYOUT GeneralRsPointer
{
protected:
    UINT m_data;

public:
    bool IsNull() { return m_data == 0; }
};

// In some cases, we need to get a uuid from a pointer (ie, in a hash)
inline ULONG_PTR LsPtrToCookie(GeneralLsPointer p) {
    return (ULONG_PTR) p.m_ptr;
}
#define VmPtrToCookie(vm) LsPtrToCookie((vm).ToLsPtr())


#ifdef RIGHT_SIDE_COMPILE
//-----------------------------------------------------------------------------
// Infrasturcture for RS Definitions
//-----------------------------------------------------------------------------

// On the RS, we don't have the LS classes defined, so we can't templatize that
// in terms of <class T>, but we still want things to be unique.
// So we create an empty enum for each LS type and then templatize it in terms
// of the enum.
template <typename T>
class MSLAYOUT LsPointer : public GeneralLsPointer
{
public:
    void Set(void * p)
    {
        m_ptr = p;
    }
    void * UnsafeGet()
    {
        return m_ptr;
    }

    static LsPointer<T> NullPtr()
    {
        return MakePtr(NULL);
    }

    static LsPointer<T> MakePtr(T* p)
    {
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:6001) // PREfast warning: Using uninitialize memory 't'
#endif // _PREFAST_

        LsPointer<T> t;
        t.Set(p);
        return t;

#ifdef _PREFAST_
#pragma warning(pop)
#endif // _PREFAST_
    }

    bool operator!= (void * p) { return m_ptr != p; }
    bool operator== (void * p) { return m_ptr == p; }
    bool operator==(LsPointer<T> p) { return p.m_ptr == this->m_ptr; }

    // We should never UnWrap() them in the RS, so we don't define that here.
};

class CordbProcess;
template <class T> UINT AllocCookie(CordbProcess * pProc, T * p);
template <class T> T * UnwrapCookie(CordbProcess * pProc, UINT cookie);

UINT AllocCookieCordbEval(CordbProcess * pProc, class CordbEval * p);
class CordbEval * UnwrapCookieCordbEval(CordbProcess * pProc, UINT cookie);

template <class CordbEval> UINT AllocCookie(CordbProcess * pProc, CordbEval * p)
{
    return AllocCookieCordbEval(pProc, p);
}
template <class CordbEval> CordbEval * UnwrapCookie(CordbProcess * pProc, UINT cookie)
{
    return UnwrapCookieCordbEval(pProc, cookie);
}



// This is how the RS sees the pointers in the IPC block.
template<class T>
class MSLAYOUT RsPointer : public GeneralRsPointer
{
public:
    // Since we're being used inside a union, we can't have a ctor.

    static RsPointer<T> NullPtr()
    {
        RsPointer<T> t;
        t.m_data = 0;
        return t;
    }

    bool AllocHandle(CordbProcess *pProc, T* p)
    {
        // This will force validation.
        m_data = AllocCookie<T>(pProc, p);
        return (m_data != 0);
    }

    bool operator==(RsPointer<T> p) { return p.m_data == this->m_data; }

    T* UnWrapAndRemove(CordbProcess *pProc)
    {
        return UnwrapCookie<T>(pProc, m_data);
    }

protected:
};

// Forward declare a class so that each type of LS pointer can have
// its own type.  We use the real class name to be compatible with VMPTRs.
#define DEFINE_LSPTR_TYPE(ls_type, ptr_name) \
    ls_type; \
    typedef LsPointer<ls_type> ptr_name;


#define DEFINE_RSPTR_TYPE(rs_type, ptr_name) \
    class rs_type; \
    typedef RsPointer<rs_type> ptr_name;

#else // !RIGHT_SIDE_COMPILE
//-----------------------------------------------------------------------------
// Infrastructure for LS Definitions
//-----------------------------------------------------------------------------

// This is how the LS sees the pointers in the IPC block.
template<typename T>
class MSLAYOUT LsPointer : public GeneralLsPointer
{
public:
    // Since we're being used inside a union, we can't have a ctor.
    //LsPointer() { }

    static LsPointer<T> NullPtr()
    {
        return MakePtr(NULL);
    }

    static LsPointer<T> MakePtr(T * p)
    {
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:6001) // PREfast warning: Using uninitialize memory 't'
#endif // _PREFAST_

        LsPointer<T> t;
        t.Set(p);
        return t;

#ifdef _PREFAST_
#pragma warning(pop)
#endif // _PREFAST_
    }

    bool operator!= (void * p) { return m_ptr != p; }
    bool operator== (void * p) { return m_ptr == p; }
    bool operator==(LsPointer<T> p) { return p.m_ptr == this->m_ptr; }

    // @todo - we want to be able to swap out Set + Unwrap functions
    void Set(T * p)
    {
        SUPPORTS_DAC;
        // We could validate the pointer here.
        m_ptr = p;
    }

    T * UnWrap()
    {
        // If we wanted to validate the pointer, here's our chance.
        return static_cast<T*>(m_ptr);
    }
};

template <class n>
class MSLAYOUT RsPointer : public GeneralRsPointer
{
public:
    static RsPointer<n> NullPtr()
    {
        RsPointer<n> t;
        t.m_data = 0;
        return t;
    }

    bool operator==(RsPointer<n> p) { return p.m_data == this->m_data; }

    // We should never UnWrap() them in the LS, so we don't define that here.
};

#define DEFINE_LSPTR_TYPE(ls_type, ptr_name) \
    ls_type; \
    typedef LsPointer<ls_type> ptr_name;

#define DEFINE_RSPTR_TYPE(rs_type, ptr_name) \
    enum __RS__##rs_type { };  \
    typedef RsPointer<__RS__##rs_type> ptr_name;

#endif // !RIGHT_SIDE_COMPILE

// We must be binary compatible w/ a pointer.
static_assert_no_msg(sizeof(LsPointer<void>) == sizeof(GeneralLsPointer));

static_assert_no_msg(sizeof(void*) == sizeof(GeneralLsPointer));



//-----------------------------------------------------------------------------
// Definitions for Left-Side ptrs.
// NOTE: Use VMPTR instead of LSPTR. Don't add new LSPTR types.
//
//-----------------------------------------------------------------------------



DEFINE_LSPTR_TYPE(class Assembly,  LSPTR_ASSEMBLY);
DEFINE_LSPTR_TYPE(class DebuggerJitInfo, LSPTR_DJI);
DEFINE_LSPTR_TYPE(class DebuggerMethodInfo, LSPTR_DMI);
DEFINE_LSPTR_TYPE(class MethodDesc,         LSPTR_METHODDESC);
DEFINE_LSPTR_TYPE(class DebuggerBreakpoint, LSPTR_BREAKPOINT);
DEFINE_LSPTR_TYPE(class DebuggerDataBreakpoint, LSPTR_DATA_BREAKPOINT);
DEFINE_LSPTR_TYPE(class DebuggerEval,       LSPTR_DEBUGGEREVAL);
DEFINE_LSPTR_TYPE(class DebuggerStepper,    LSPTR_STEPPER);

// Need to be careful not to annoy the compiler here since DT_CONTEXT is a typedef, not a struct.
#if defined(RIGHT_SIDE_COMPILE)
typedef LsPointer<DT_CONTEXT> LSPTR_CONTEXT;
#else  // RIGHT_SIDE_COMPILE
typedef LsPointer<DT_CONTEXT> LSPTR_CONTEXT;
#endif // RIGHT_SIDE_COMPILE

DEFINE_LSPTR_TYPE(struct OBJECTHANDLE__,    LSPTR_OBJECTHANDLE);
DEFINE_LSPTR_TYPE(class TypeHandleDummyPtr, LSPTR_TYPEHANDLE); // TypeHandle in the LS is not a direct pointer.

//-----------------------------------------------------------------------------
// Definitions for Right-Side ptrs.
//-----------------------------------------------------------------------------
DEFINE_RSPTR_TYPE(CordbEval,               RSPTR_CORDBEVAL);


//---------------------------------------------------------------------------------------
// VMPTR_Base is the base type for an abstraction over pointers into the VM so
// that DBI can treat them as opaque handles. Classes will derive from it to
// provide type-safe Target pointers, which ICD will view as opaque handles.
//
// Lifetimes:
//   VMPTR_ objects survive across flushing the DAC cache. Therefore, the underlying
//   storage must be a target-pointer (and not a marshalled host pointer).
//   The RS must ensure they're still in sync with the LS (eg, by
//   tracking unload events).
//
//
// Assumptions:
//    These handles are TADDR pointers and must not require any cleanup from DAC/DBI.
//    For direct untyped pointers into the VM, use CORDB_ADDRESS.
//
// Notes:
//  1. This helps enforce that DBI goes through the primitives interface
//     for all access (and that it doesn't accidentally start calling
//     dac-ized methods on the objects)
//  2. This isolates DBI from VM headers.
//  3. This isolates DBI from the dac implementation (of DAC_Ptr)
//  4. This is distinct from LSPTR because LSPTRs are truly opaque handles, whereas VMPtrs
//     move across VM, DAC, and DBI, exposing proper functionality in each component.
//  5. VMPTRs are blittable because they are Target Addresses which act as opaque
//     handles outside of the Target / Dac-marshaller.
//
//---------------------------------------------------------------------------------------


template <typename TTargetPtr, typename TDacPtr>
class MSLAYOUT VMPTR_Base
{
    // Underlying pointer into Target address space.
    // Target pointers are blittable.
    // - In Target: can be used as normal local pointers.
    // - In DAC: must be marshalled to a host-pointer and then they can be used via DAC
    // - In RS: opaque handles.
private:
    TADDR m_addr;

public:
    typedef VMPTR_Base<TTargetPtr,TDacPtr> VMPTR_This;

    // For DBI, VMPTRs are opaque handles.
    // But the DAC side is allowed to inspect the handles to get at the raw pointer.
#if defined(ALLOW_VMPTR_ACCESS)
    //
    // Case 1: Using in DAcDbi implementation
    //

    // DAC accessor
    TDacPtr GetDacPtr() const
    {
        SUPPORTS_DAC;
        return TDacPtr(m_addr);
    }


    // This will initialize the handle to a given target-pointer.
    // We choose TADDR to make it explicit that it's a target pointer and avoid the risk
    // of it accidentally getting marshalled to a host pointer.
    void SetDacTargetPtr(TADDR addr)
    {
        SUPPORTS_DAC;
        m_addr = addr;
    }

    void SetHostPtr(const TTargetPtr * pObject)
    {
        SUPPORTS_DAC;
        m_addr = PTR_HOST_TO_TADDR(pObject);
    }


#elif !defined(RIGHT_SIDE_COMPILE)
    //
    // Case 2: Used in Left-side. Can get/set from local pointers.
    //

    // This will set initialize from a Target pointer. Since this is happening in the
    // Left-side (Target), the pointer is local.
    // This is commonly used by the Left-side to create a VMPTR_ for a notification event.
    void SetRawPtr(TTargetPtr * ptr)
    {
        m_addr = reinterpret_cast<TADDR>(ptr);
    }

    // This will get the raw underlying target pointer.
    // This can be used by inproc Left-side code to unwrap a VMPTR (Eg, for a func-eval
    // hijack or in-proc worker threads)
    TTargetPtr * GetRawPtr()
    {
        return reinterpret_cast<TTargetPtr*>(m_addr);
    }

    // Convenience for converting TTargetPtr --> VMPTR
    static VMPTR_This MakePtr(TTargetPtr * ptr)
    {
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:6001) // PREfast warning: Using uninitialize memory 't'
#endif // _PREFAST_

        VMPTR_This t;
        t.SetRawPtr(ptr);
        return t;

#ifdef _PREFAST_
#pragma warning(pop)
#endif // _PREFAST_
    }


#else
    //
    // Case 3: Used in RS. Opaque handles only.
    //
#endif


#ifndef DACCESS_COMPILE
    // For compatibility, these can be converted to LSPTRs on the RS or LS (case 2 and 3).  We don't allow
    // this in the DAC case because it's a cast between address spaces which we're trying to eliminate
    // in the DAC code.
    // @dbgtodo  inspection: LSPTRs will go away entirely once we've moved completely over to DAC
    LsPointer<TTargetPtr> ToLsPtr()
    {
        return LsPointer<TTargetPtr>::MakePtr( reinterpret_cast<TTargetPtr *>(m_addr));
    }
#endif

    //
    // Operators to emulate Pointer semantics.
    //
    bool IsNull() { SUPPORTS_DAC; return m_addr == NULL; }

    static VMPTR_This NullPtr()
    {
        SUPPORTS_DAC;

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:6001) // PREfast warning: Using uninitialize memory 't'
#endif // _PREFAST_

        VMPTR_This dummy;
        dummy.m_addr = NULL;
        return dummy;

#ifdef _PREFAST_
#pragma warning(pop)
#endif // _PREFAST_
    }

    bool operator!= (VMPTR_This vmOther) const { SUPPORTS_DAC; return this->m_addr != vmOther.m_addr; }
    bool operator== (VMPTR_This vmOther) const { SUPPORTS_DAC; return this->m_addr == vmOther.m_addr; }
};

#if defined(ALLOW_VMPTR_ACCESS)
// Helper macro to define a VMPTR.
// This is used in the DAC case, so this definition connects the pointers up to their DAC values.
#define DEFINE_VMPTR(ls_type, dac_ptr_type, ptr_name) \
    ls_type;  \
    typedef VMPTR_Base<ls_type, dac_ptr_type> ptr_name;

#else
// Helper macro to define a VMPTR.
// This is used in the Right-side and Left-side (but not DAC) case.
// This definition explicitly ignores dac_ptr_type to prevent accidental DAC usage.
#define DEFINE_VMPTR(ls_type, dac_ptr_type, ptr_name) \
    ls_type;  \
    typedef VMPTR_Base<ls_type, void> ptr_name;

#endif

// Declare VMPTRs.
// The naming convention for instantiating a VMPTR is a 'vm' prefix.
//
//           VM definition,         DAC definition,     pretty name for VMPTR
DEFINE_VMPTR(class AppDomain,       PTR_AppDomain,      VMPTR_AppDomain);

// Need to be careful not to annoy the compiler here since DT_CONTEXT is a typedef, not a struct.
// DEFINE_VMPTR(struct _CONTEXT,       PTR_CONTEXT,        VMPTR_CONTEXT);
#if defined(ALLOW_VMPTR_ACCESS)
typedef VMPTR_Base<DT_CONTEXT, PTR_CONTEXT> VMPTR_CONTEXT;
#else
typedef VMPTR_Base<DT_CONTEXT, void > VMPTR_CONTEXT;
#endif

// DomainAssembly is a base-class for a CLR module, with app-domain affinity.
// For domain-neutral modules (like CoreLib), there is a DomainAssembly instance
// for each appdomain the module lives in.
// This is the canonical handle ICorDebug uses to a CLR module.
DEFINE_VMPTR(class DomainAssembly,  PTR_DomainAssembly, VMPTR_DomainAssembly);
DEFINE_VMPTR(class Module,          PTR_Module,         VMPTR_Module);

DEFINE_VMPTR(class Assembly,        PTR_Assembly,       VMPTR_Assembly);

DEFINE_VMPTR(class PEAssembly,      PTR_PEAssembly,     VMPTR_PEAssembly);
DEFINE_VMPTR(class MethodDesc,      PTR_MethodDesc,     VMPTR_MethodDesc);
DEFINE_VMPTR(class FieldDesc,       PTR_FieldDesc,      VMPTR_FieldDesc);

// ObjectHandle is a safe way to represent an object into the GC heap. It gets updated
// when a GC occurs.
DEFINE_VMPTR(struct OBJECTHANDLE__, TADDR,              VMPTR_OBJECTHANDLE);

DEFINE_VMPTR(class TypeHandle,      PTR_TypeHandle,     VMPTR_TypeHandle);

// A VMPTR_Thread represents a thread that has entered the runtime at some point.
// It may or may not have executed managed code yet; and it may or may not have managed code
// on its callstack.
DEFINE_VMPTR(class Thread,          PTR_Thread,         VMPTR_Thread);

DEFINE_VMPTR(class Object,          PTR_Object,         VMPTR_Object);

DEFINE_VMPTR(class CrstBase,        PTR_Crst,           VMPTR_Crst);
DEFINE_VMPTR(class SimpleRWLock,    PTR_SimpleRWLock,   VMPTR_SimpleRWLock);
DEFINE_VMPTR(class SimpleRWLock,    PTR_SimpleRWLock,   VMPTR_RWLock);
DEFINE_VMPTR(struct ReJitInfo,       PTR_ReJitInfo,      VMPTR_ReJitInfo);
DEFINE_VMPTR(struct SharedReJitInfo, PTR_SharedReJitInfo, VMPTR_SharedReJitInfo);
DEFINE_VMPTR(class NativeCodeVersionNode, PTR_NativeCodeVersionNode, VMPTR_NativeCodeVersionNode);
DEFINE_VMPTR(class ILCodeVersionNode, PTR_ILCodeVersionNode, VMPTR_ILCodeVersionNode);

typedef CORDB_ADDRESS GENERICS_TYPE_TOKEN;


//-----------------------------------------------------------------------------
// We pass some fixed size strings in the IPC block.
// Helper class to wrap the buffer and protect against buffer overflows.
// This should be binary compatible w/ a wchar[] array.
//-----------------------------------------------------------------------------

template <int nMaxLengthIncludingNull>
class MSLAYOUT EmbeddedIPCString
{
public:
    // Set, caller responsibility that wcslen(pData) < nMaxLengthIncludingNull
    void SetString(const WCHAR * pData)
    {
        // If the string doesn't fit into the buffer, that's an issue (and so this is a real
        // assert, not just a simplifying assumption). To fix it, either:
        // - make the buffer larger
        // - don't pass the string as an embedded string in the IPC block.
        // This will truncate (rather than AV on the RS).
        int ret;
        ret = SafeCopy(pData);

        // See comment above - caller should guarantee that buffer is large enough.
        _ASSERTE(ret != STRUNCATE);
    }

    // Set a string from a substring. This will truncate if necessary.
    void SetStringTruncate(const WCHAR * pData)
    {
        // ignore return value because truncation is ok.
        SafeCopy(pData);
    }

    const WCHAR * GetString()
    {
        // For a null-termination just in case an issue in the debuggee process
        // yields a malformed string.
        m_data[nMaxLengthIncludingNull - 1] = W('\0');
        return &m_data[0];
    }
    int GetMaxSize() const { return nMaxLengthIncludingNull; }

private:
    int SafeCopy(const WCHAR * pData)
    {
        return wcsncpy_s(
            m_data, nMaxLengthIncludingNull,
            pData, _TRUNCATE);
    }
    WCHAR m_data[nMaxLengthIncludingNull];
};

//
// Types of events that can be sent between the Runtime Controller and
// the Debugger Interface. Some of these events are one way only, while
// others go both ways. The grouping of the event numbers is an attempt
// to show this distinction and perhaps even allow generic operations
// based on the type of the event.
//
enum DebuggerIPCEventType
{
#define IPC_EVENT_TYPE0(type, val)  type = val,
#define IPC_EVENT_TYPE1(type, val)  type = val,
#define IPC_EVENT_TYPE2(type, val)  type = val,
#include "dbgipceventtypes.h"
#undef IPC_EVENT_TYPE2
#undef IPC_EVENT_TYPE1
#undef IPC_EVENT_TYPE0
};

#ifdef _DEBUG

// This is a static debugging structure to help breaking at the right place.
// Debug only. This is to track the number of events that have been happened so far.
// User can choose to set break point base on the number of events.
// Variables are named as the event name with prefix m_iDebugCount. For example
// m_iDebugCount_DB_IPCE_BREAKPOINT if for event DB_IPCE_BREAKPOINT.
struct MSLAYOUT DebugEventCounter
{
// we don't need the event type 0
#define IPC_EVENT_TYPE0(type, val)
#define IPC_EVENT_TYPE1(type, val)  int m_iDebugCount_##type;
#define IPC_EVENT_TYPE2(type, val)  int m_iDebugCount_##type;
#include "dbgipceventtypes.h"
#undef IPC_EVENT_TYPE2
#undef IPC_EVENT_TYPE1
#undef IPC_EVENT_TYPE0
};
#endif // _DEBUG


#if !defined(DACCESS_COMPILE)

struct MSLAYOUT IPCEventTypeNameMapping
    {
            DebuggerIPCEventType    eventType;
            const char *            eventName;
};

extern const IPCEventTypeNameMapping DbgIPCEventTypeNames[];

extern const size_t nameCount;

struct MSLAYOUT IPCENames // We use a class/struct so that the function can remain in a shared header file
{
    static DebuggerIPCEventType GetEventType(_In_z_ char * strEventType)
    {
        // pass in the string of event name and find the matching enum value
        // This is a linear search which is pretty slow. However, this is only used
        // at startup time when debug assert is turn on and with registry key set. So it is not that bad.
        //
        for (size_t i = 0; i < nameCount; i++)
        {
            if (_stricmp(DbgIPCEventTypeNames[i].eventName, strEventType) == 0)
                return DbgIPCEventTypeNames[i].eventType;
        }
        return DB_IPCE_INVALID_EVENT;
    }
    static const char * GetName(DebuggerIPCEventType eventType)
    {

        enum DbgIPCEventTypeNum
        {
        #define IPC_EVENT_TYPE0(type, val)  type##_Num,
        #define IPC_EVENT_TYPE1(type, val)  type##_Num,
        #define IPC_EVENT_TYPE2(type, val)  type##_Num,
        #include "dbgipceventtypes.h"
        #undef IPC_EVENT_TYPE2
        #undef IPC_EVENT_TYPE1
        #undef IPC_EVENT_TYPE0
        };

        size_t i, lim;

        if (eventType < DB_IPCE_DEBUGGER_FIRST)
        {
            i = DB_IPCE_RUNTIME_FIRST_Num + 1;
            lim = DB_IPCE_DEBUGGER_FIRST_Num;
        }
        else
        {
            i = DB_IPCE_DEBUGGER_FIRST_Num + 1;
            lim = nameCount;
        }

        for (/**/; i < lim; i++)
        {
            if (DbgIPCEventTypeNames[i].eventType == eventType)
                return DbgIPCEventTypeNames[i].eventName;
        }

        return DbgIPCEventTypeNames[nameCount - 1].eventName;
    }
};

#endif // !DACCESS_COMPILE

//
// NOTE:  CPU-specific values below!
//
// DebuggerREGDISPLAY is very similar to the EE REGDISPLAY structure. It holds
// register values that can be saved over calls for each frame in a stack
// trace.
//
// DebuggerIPCE_FloatCount is the number of doubles in the processor's
// floating point stack.
//
// <TODO>Note: We used to just pass the values of the registers for each frame to the Right Side, but I had to add in the
// address of each register, too, to support using enregistered variables on non-leaf frames as args to a func eval. Its
// very, very possible that we would rework the entire code base to just use the register's address instead of passing
// both, but its way, way too late in V1 to undertake that, so I'm just using these addresses to suppport our one func
// eval case. Clearly, this needs to be cleaned up post V1.
//
// -- Fri Feb 09 11:21:24 2001</TODO>
//

struct MSLAYOUT DebuggerREGDISPLAY
{
#if defined(TARGET_X86)
    #define DebuggerIPCE_FloatCount 8

    SIZE_T  Edi;
    void   *pEdi;
    SIZE_T  Esi;
    void   *pEsi;
    SIZE_T  Ebx;
    void   *pEbx;
    SIZE_T  Edx;
    void   *pEdx;
    SIZE_T  Ecx;
    void   *pEcx;
    SIZE_T  Eax;
    void   *pEax;
    SIZE_T  FP;
    void   *pFP;
    SIZE_T  SP;
    SIZE_T  PC;

#elif defined(TARGET_AMD64)
    #define DebuggerIPCE_FloatCount 16

    SIZE_T  Rax;
    void   *pRax;
    SIZE_T  Rcx;
    void   *pRcx;
    SIZE_T  Rdx;
    void   *pRdx;
    SIZE_T  Rbx;
    void   *pRbx;
    SIZE_T  Rbp;
    void   *pRbp;
    SIZE_T  Rsi;
    void   *pRsi;
    SIZE_T  Rdi;
    void   *pRdi;

    SIZE_T  R8;
    void   *pR8;
    SIZE_T  R9;
    void   *pR9;
    SIZE_T  R10;
    void   *pR10;
    SIZE_T  R11;
    void   *pR11;
    SIZE_T  R12;
    void   *pR12;
    SIZE_T  R13;
    void   *pR13;
    SIZE_T  R14;
    void   *pR14;
    SIZE_T  R15;
    void   *pR15;

    SIZE_T  SP;
    SIZE_T  PC;
#elif defined(TARGET_ARM)
    #define DebuggerIPCE_FloatCount 32

    SIZE_T  R0;
    void   *pR0;
    SIZE_T  R1;
    void   *pR1;
    SIZE_T  R2;
    void   *pR2;
    SIZE_T  R3;
    void   *pR3;
    SIZE_T  R4;
    void   *pR4;
    SIZE_T  R5;
    void   *pR5;
    SIZE_T  R6;
    void   *pR6;
    SIZE_T  R7;
    void   *pR7;
    SIZE_T  R8;
    void   *pR8;
    SIZE_T  R9;
    void   *pR9;
    SIZE_T  R10;
    void   *pR10;
    SIZE_T  R11;
    void   *pR11;
    SIZE_T  R12;
    void   *pR12;
    SIZE_T  SP;
    void   *pSP;
    SIZE_T  LR;
    void   *pLR;
    SIZE_T  PC;
    void   *pPC;
#elif defined(TARGET_ARM64)
    #define DebuggerIPCE_FloatCount 32

    SIZE_T  X[29];
    SIZE_T  FP;
    SIZE_T  LR;
    SIZE_T  SP;
    SIZE_T  PC;
#elif defined(TARGET_LOONGARCH64)
    #define DebuggerIPCE_FloatCount 32
    SIZE_T  RA;
    SIZE_T  TP;
    SIZE_T  SP;
    SIZE_T  A0;
    SIZE_T  A1;
    SIZE_T  A2;
    SIZE_T  A3;
    SIZE_T  A4;
    SIZE_T  A5;
    SIZE_T  A6;
    SIZE_T  A7;
    SIZE_T  T0;
    SIZE_T  T1;
    SIZE_T  T2;
    SIZE_T  T3;
    SIZE_T  T4;
    SIZE_T  T5;
    SIZE_T  T6;
    SIZE_T  T7;
    SIZE_T  T8;
    SIZE_T  X0;
    SIZE_T  FP;
    SIZE_T  S0;
    SIZE_T  S1;
    SIZE_T  S2;
    SIZE_T  S3;
    SIZE_T  S4;
    SIZE_T  S5;
    SIZE_T  S6;
    SIZE_T  S7;
    SIZE_T  S8;
    SIZE_T  PC;
#else
    #define DebuggerIPCE_FloatCount 1

    SIZE_T PC;
    SIZE_T FP;
    SIZE_T SP;
    void   *pFP;
#endif
};

inline LPVOID GetSPAddress(const DebuggerREGDISPLAY * display)
{
    return (LPVOID)&display->SP;
}

#if !defined(TARGET_AMD64) && !defined(TARGET_ARM)
inline LPVOID GetFPAddress(const DebuggerREGDISPLAY * display)
{
    return (LPVOID)&display->FP;
}
#endif // !TARGET_AMD64


class MSLAYOUT FramePointer
{
friend bool IsCloserToLeaf(FramePointer fp1, FramePointer fp2);
friend bool IsCloserToRoot(FramePointer fp1, FramePointer fp2);
friend bool IsEqualOrCloserToLeaf(FramePointer fp1, FramePointer fp2);
friend bool IsEqualOrCloserToRoot(FramePointer fp1, FramePointer fp2);

public:

    static FramePointer MakeFramePointer(LPVOID sp)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        FramePointer fp;
        fp.m_sp = sp;
        return fp;
    }

    static FramePointer MakeFramePointer(UINT_PTR sp)
    {
        SUPPORTS_DAC;
        return MakeFramePointer((LPVOID)sp);
    }

    inline bool operator==(FramePointer fp)
    {
        return (m_sp == fp.m_sp);
    }

    inline bool operator!=(FramePointer fp)
    {
        return !(*this == fp);
    }

    // This is needed because on the RS, the m_id values of CordbFrame and
    // CordbChain are really FramePointers.
    LPVOID GetSPValue() const
    {
        return m_sp;
    }


private:
    // Declare some private constructors which signatures matching common usage of FramePointer
    // to prevent people from accidentally assigning a pointer to a FramePointer().
    FramePointer &operator=(LPVOID sp);
    FramePointer &operator=(BYTE* sp);
    FramePointer &operator=(const BYTE* sp);

    LPVOID m_sp;
};

// For non-IA64 platforms, we use stack pointers as frame pointers.
// (Stack grows towards smaller address.)
#define LEAF_MOST_FRAME FramePointer::MakeFramePointer((LPVOID)NULL)
#define ROOT_MOST_FRAME FramePointer::MakeFramePointer((LPVOID)-1)

static_assert_no_msg(sizeof(FramePointer) == sizeof(void*));


inline bool IsCloserToLeaf(FramePointer fp1, FramePointer fp2)
{
    return (fp1.m_sp < fp2.m_sp);
}

inline bool IsCloserToRoot(FramePointer fp1, FramePointer fp2)
{
    return (fp1.m_sp > fp2.m_sp);
}

inline bool IsEqualOrCloserToLeaf(FramePointer fp1, FramePointer fp2)
{
    return !IsCloserToRoot(fp1, fp2);
}

inline bool IsEqualOrCloserToRoot(FramePointer fp1, FramePointer fp2)
{
    return !IsCloserToLeaf(fp1, fp2);
}


// struct DebuggerIPCE_FuncData:   DebuggerIPCE_FuncData holds data
// to describe a given function, its
// class, and a little bit about the code for the function. This is used
// in the stack trace result data to pass function information back that
// may be needed. Its also used when getting data about a specific function.
//
// void* nativeStartAddressPtr: Ptr to CORDB_ADDRESS, which is
//          the address of the real start address of the native code.
//          This field will be NULL only if the method hasn't been JITted
//          yet (and thus no code is available).  Otherwise, it will be
//          the address of a CORDB_ADDRESS in the remote memory.  This
//          CORDB_ADDRESS may be NULL, in which case the code is unavailable
//          has been pitched (return CORDBG_E_CODE_NOT_AVAILABLE)
//
// SIZE_T nVersion: The version of the code that this instance of the
//          function is using.
struct MSLAYOUT DebuggerIPCE_FuncData
{
    mdMethodDef funcMetadataToken;
    VMPTR_DomainAssembly vmDomainAssembly;

    mdTypeDef   classMetadataToken;

    void*       ilStartAddress;
    SIZE_T      ilSize;

    SIZE_T      currentEnCVersion;

    mdSignature  localVarSigToken;


};

// struct DebuggerIPCE_JITFuncData:   DebuggerIPCE_JITFuncData holds
// a little bit about the JITted code for the function.
//
// void* nativeStartAddressPtr: Ptr to CORDB_ADDRESS, which is
//          the address of the real start address of the native code.
//          This field will be NULL only if the method hasn't been JITted
//          yet (and thus no code is available).  Otherwise, it will be
//          the address of a CORDB_ADDRESS in the remote memory.  This
//          CORDB_ADDRESS may be NULL, in which case the code is unavailable
//          or has been pitched (return CORDBG_E_CODE_NOT_AVAILABLE)
//
// SIZE_T nativeSize: Size of the native code.
//
// SIZE_T nativeOffset: Offset from the beginning of the function,
//          in bytes.  This may be non-zero even when nativeStartAddressPtr
//          is NULL
// void * nativeCodeJITInfoToken: An opaque value to hand back to the left
//          side when fetching the JITInfo for the native code, i.e. the
//          IL->native maps for the variables.  This may be NULL if no JITInfo is available.
// void * nativeCodeMethodDescToken: An opaque value to hand back to the left
//          side when fetching the code.  In addition this token can act as the
//          unique identity for the native code in the case where there are
//          multiple blobs of native code per IL method (i.e. if the method is
//          generic code of some kind)
// BOOL isInstantiatedGeneric: Indicates if the method is
//          generic code of some kind.
// BOOL justAfterILThrow: indicates that code just threw a software exception and
//          nativeOffset points to an instruction just after [call IL_Throw].
//          This is being used to figure out a real offset of the exception origin.
//          By subtracting STACKWALK_CONTROLPC_ADJUST_OFFSET from nativeOffset you can get
//          an address somewhere inside [call IL_Throw] instruction.
// void *ilToNativeMapAddr etc.: If nativeCodeJITInfoToken is not NULL then these
//          specify the table giving the mapping of IPs.
struct MSLAYOUT DebuggerIPCE_JITFuncData
{
    TADDR       nativeStartAddressPtr;
    SIZE_T      nativeHotSize;

    // If we have a cold region, need its size & the pointer to where starts.
    TADDR       nativeStartAddressColdPtr;
    SIZE_T      nativeColdSize;


    SIZE_T      nativeOffset;
    LSPTR_DJI   nativeCodeJITInfoToken;
    VMPTR_MethodDesc vmNativeCodeMethodDescToken;

#ifdef FEATURE_EH_FUNCLETS
    BOOL         fIsFilterFrame;
    SIZE_T       parentNativeOffset;
    FramePointer fpParentOrSelf;
#endif // FEATURE_EH_FUNCLETS

    // indicates if the MethodDesc is a generic function or a method inside a generic class (or
    // both!).
    BOOL         isInstantiatedGeneric;

    // this is the version of the jitted code
    SIZE_T       enCVersion;

    BOOL         justAfterILThrow;
};

//
// DebuggerIPCE_STRData holds data for each stack frame or chain. This data is passed
// from the RC to the DI during a stack walk.
//
#if defined(_MSC_VER)
#pragma warning( push )
#pragma warning( disable:4324 ) // the compiler pads a structure to comply with alignment requirements
#endif                          // ARM context structures have a 16-byte alignment requirement
struct MSLAYOUT DebuggerIPCE_STRData
{
    FramePointer            fp;
    // @dbgtodo  stackwalker/shim- Ideally we should be able to get rid of the DebuggerREGDISPLAY and just use the CONTEXT.
    DT_CONTEXT              ctx;
    DebuggerREGDISPLAY      rd;
    bool                    quicklyUnwound;

    VMPTR_AppDomain         vmCurrentAppDomainToken;


    enum EType
    {
        cMethodFrame = 0,
        cChain,
        cStubFrame,
        cRuntimeNativeFrame
    } eType;

    union MSLAYOUT
    {
        // Data for a chain
        struct MSLAYOUT
        {
            CorDebugChainReason chainReason;
            bool                managed;
        } u;

        // Data for a Method
        struct MSLAYOUT
        {
            struct DebuggerIPCE_FuncData funcData;
            struct DebuggerIPCE_JITFuncData jitFuncData;
            SIZE_T                       ILOffset;
            CorDebugMappingResult        mapping;

            bool        fVarArgs;

            // Indicates whether the managed method has any metadata.
            // Some dynamic methods such as IL stubs and LCG methods don't have any metadata.
            // This is used only by the V3 stackwalker, not the V2 one, because we only
            // expose dynamic methods as real stack frames in V3.
            bool        fNoMetadata;

            TADDR       taAmbientESP;

            GENERICS_TYPE_TOKEN exactGenericArgsToken;
            DWORD               dwExactGenericArgsTokenIndex;

        } v;

        // Data for an Stub Frame.
        struct MSLAYOUT
        {
            mdMethodDef funcMetadataToken;
            VMPTR_DomainAssembly vmDomainAssembly;
            VMPTR_MethodDesc vmMethodDesc;
            CorDebugInternalFrameType frameType;
        } stubFrame;

    };
};
#if defined(_MSC_VER)
#pragma warning( pop )
#endif

//
// DebuggerIPCE_BasicTypeData and DebuggerIPCE_ExpandedTypeData
// hold data for each type sent across the
// boundary, whether it be a constructed type List<String> or a non-constructed
// type such as String, Foo or Object.
//
// Logically speaking DebuggerIPCE_BasicTypeData might just be "typeHandle", as
// we could then send further events to ask what the elementtype, typeToken and moduleToken
// are for the type handle.  But as
// nearly all types are non-generic we send across even the basic type information in
// the slightly expanded form shown below, sending the element type and the
// tokens with the type handle itself. The fields debuggerModuleToken, metadataToken and typeHandle
// are only used as follows:
//                                   elementType    debuggerModuleToken metadataToken      typeHandle
//     E_T_INT8    :                  E_T_INT8         No                     No              No
//     Boxed E_T_INT8:                E_T_CLASS        No                     No              No
//     E_T_CLASS, non-generic class:  E_T_CLASS       Yes                    Yes              No
//     E_T_VALUETYPE, non-generic:    E_T_VALUETYPE   Yes                    Yes              No
//     E_T_CLASS,     generic class:  E_T_CLASS       Yes                    Yes             Yes
//     E_T_VALUETYPE, generic class:  E_T_VALUETYPE   Yes                    Yes             Yes
//     E_T_BYREF                   :  E_T_BYREF        No                     No             Yes
//     E_T_PTR                     :  E_T_PTR          No                     No             Yes
//     E_T_ARRAY etc.              :  E_T_ARRAY        No                     No             Yes
//     E_T_FNPTR etc.              :  E_T_FNPTR        No                     No             Yes
// This allows us to always set "typeHandle" to NULL except when dealing with highly nested
// types or function-pointer types (the latter are too complexe to transfer over in one hit).
//

struct MSLAYOUT DebuggerIPCE_BasicTypeData
{
    CorElementType  elementType;
    mdTypeDef       metadataToken;
    VMPTR_Module     vmModule;
    VMPTR_DomainAssembly vmDomainAssembly;
    VMPTR_TypeHandle vmTypeHandle;
};

// DebuggerIPCE_ExpandedTypeData contains more information showing further
// details for array types, byref types etc.
// Whenever you fetch type information from the left-side
// you get back one of these.  These in turn contain further
// DebuggerIPCE_BasicTypeData's and typeHandles which you can
// then query to get further information about the type parameters.
// This copes with the nested cases, e.g. jagged arrays,
// String ****, &(String*), Pair<String,Pair<String>>
// and so on.
//
// So this type information is not "fully expanded", it's just a little
// more detail then DebuggerIPCE_BasicTypeData.  For type
// instantiatons (e.g. List<int>) and
// function pointer types you will need to make further requests for
// information about the type parameters.
// For array types there is always only one type parameter so
// we include that as part of the expanded data.
//
//
struct MSLAYOUT DebuggerIPCE_ExpandedTypeData
{
    CorElementType  elementType; // Note this is _never_ E_T_VAR, E_T_WITH or E_T_MVAR
    union MSLAYOUT
    {
        // used for E_T_CLASS and E_T_VALUECLASS, E_T_PTR, E_T_BYREF etc.
        // For non-constructed E_T_CLASS or E_T_VALUECLASS the tokens will be set and the typeHandle will be NULL
        // For constructed E_T_CLASS or E_T_VALUECLASS the tokens will be set and the typeHandle will be non-NULL
        // For E_T_PTR etc. the tokens will be NULL and the typeHandle will be non-NULL.
        struct MSLAYOUT
         {
            mdTypeDef       metadataToken;
            VMPTR_Module vmModule;
            VMPTR_DomainAssembly vmDomainAssembly;
            VMPTR_TypeHandle typeHandle; // if non-null then further fetches will be needed to get type arguments
        } ClassTypeData;

        // used for E_T_PTR, E_T_BYREF etc.
        struct MSLAYOUT
         {
            DebuggerIPCE_BasicTypeData unaryTypeArg;  // used only when sending back to debugger
        } UnaryTypeData;


        // used for E_T_ARRAY etc.
        struct MSLAYOUT
        {
          DebuggerIPCE_BasicTypeData arrayTypeArg; // used only when sending back to debugger
            DWORD           arrayRank;
        } ArrayTypeData;

        // used for E_T_FNPTR
        struct MSLAYOUT
         {
            VMPTR_TypeHandle typeHandle; // if non-null then further fetches needed to get type arguments
        } NaryTypeData;

    };
};

// DebuggerIPCE_TypeArgData is used when sending type arguments
// across to a funceval.  It contains the DebuggerIPCE_ExpandedTypeData describing the
// essence of the type, but the typeHandle and other
// BasicTypeData fields should be zero and will be ignored.
// The DebuggerIPCE_ExpandedTypeData is then followed
// by the required number of type arguments, each of which
// will be a further DebuggerIPCE_TypeArgData record in the stream of
// flattened type argument data.
struct MSLAYOUT DebuggerIPCE_TypeArgData
{
    DebuggerIPCE_ExpandedTypeData  data;
    unsigned int                   numTypeArgs; // number of immediate children on the type tree
};


//
// DebuggerIPCE_ObjectData holds the results of a
// GetAndSendObjectInfo, i.e., all the info about an object that the
// Right Side would need to access it. (This include array, string,
// and nstruct info.)
//
struct MSLAYOUT DebuggerIPCE_ObjectData
{
    void           *objRef;
    bool            objRefBad;
    SIZE_T          objSize;

    // Offset from the beginning of the object to the beginning of the first field
    SIZE_T          objOffsetToVars;

    // The type of the object....
    struct DebuggerIPCE_ExpandedTypeData objTypeData;

    union MSLAYOUT
    {
        struct MSLAYOUT
        {
            SIZE_T          length;
            SIZE_T          offsetToStringBase;
        } stringInfo;

        struct MSLAYOUT
        {
            SIZE_T          rank;
            SIZE_T          offsetToArrayBase;
            SIZE_T          offsetToLowerBounds; // 0 if not present
            SIZE_T          offsetToUpperBounds; // 0 if not present
            SIZE_T          componentCount;
            SIZE_T          elementSize;
        } arrayInfo;

        struct MSLAYOUT
        {
            struct DebuggerIPCE_BasicTypeData typedByrefType; // the type of the thing contained in a typedByref...
        } typedByrefInfo;
    };
};

//
// Remote enregistered info used by CordbValues and for passing
// variable homes between the left and right sides during a func eval.
//

enum RemoteAddressKind
{
    RAK_NONE = 0,
    RAK_REG,
    RAK_REGREG,
    RAK_REGMEM,
    RAK_MEMREG,
    RAK_FLOAT,
    RAK_END
};

const CORDB_ADDRESS kLeafFrameRegAddr = 0;
const CORDB_ADDRESS kNonLeafFrameRegAddr = (CORDB_ADDRESS)(-1);

struct MSLAYOUT RemoteAddress
{
    RemoteAddressKind    kind;
    void                *frame;

    CorDebugRegister     reg1;
    void                *reg1Addr;
    SIZE_T               reg1Value;         // this is the actual value of the register

    union MSLAYOUT
    {
        struct MSLAYOUT
        {
            CorDebugRegister  reg2;
            void             *reg2Addr;
            SIZE_T            reg2Value;    // this is the actual value of the register
        } u;

        CORDB_ADDRESS    addr;
        DWORD            floatIndex;
    };
};

//
// DebuggerIPCE_FuncEvalType specifies the type of a function
// evaluation that will occur.
//
enum DebuggerIPCE_FuncEvalType
{
    DB_IPCE_FET_NORMAL,
    DB_IPCE_FET_NEW_OBJECT,
    DB_IPCE_FET_NEW_OBJECT_NC,
    DB_IPCE_FET_NEW_STRING,
    DB_IPCE_FET_NEW_ARRAY
};


enum NameChangeType
{
    APP_DOMAIN_NAME_CHANGE,
    THREAD_NAME_CHANGE
};

//
// DebuggerIPCE_FuncEvalArgData holds data for each argument to a
// function evaluation.
//
struct MSLAYOUT DebuggerIPCE_FuncEvalArgData
{
    RemoteAddress     argHome;  // enregistered variable home
    void             *argAddr;  // address if not enregistered
    CorElementType    argElementType;
    unsigned int      fullArgTypeNodeCount; // Pointer to LS (DebuggerIPCE_TypeArgData *) buffer holding full description of the argument type (if needed - only needed for struct types)
    void             *fullArgType; // Pointer to LS (DebuggerIPCE_TypeArgData *) buffer holding full description of the argument type (if needed - only needed for struct types)
    BYTE              argLiteralData[8]; // copy of generic value data
    bool              argIsLiteral; // true if value is in argLiteralData
    bool              argIsHandleValue; // true if argAddr is OBJECTHANDLE
};


//
// DebuggerIPCE_FuncEvalInfo holds info necessary to setup a func eval
// operation.
//
struct MSLAYOUT DebuggerIPCE_FuncEvalInfo
{
    VMPTR_Thread               vmThreadToken;
    DebuggerIPCE_FuncEvalType  funcEvalType;
    mdMethodDef                funcMetadataToken;
    mdTypeDef                  funcClassMetadataToken;
    VMPTR_DomainAssembly       vmDomainAssembly;
    RSPTR_CORDBEVAL            funcEvalKey;
    bool                       evalDuringException;

    unsigned int               argCount;
    unsigned int               genericArgsCount;
    unsigned int               genericArgsNodeCount;

    SIZE_T                     stringSize;

    SIZE_T                     arrayRank;
};


//
// Used in DebuggerIPCFirstChanceData. This tells the LS what action to take within the hijack
//
enum HijackAction
{
    HIJACK_ACTION_EXIT_UNHANDLED,
    HIJACK_ACTION_EXIT_HANDLED,
    HIJACK_ACTION_WAIT
};

//
// DebuggerIPCFirstChanceData holds info communicated from the LS to the RS when signaling that an exception does not
// belong to the runtime from a first chance hijack. This is used when Win32 debugging only.
//
struct MSLAYOUT DebuggerIPCFirstChanceData
{
    LSPTR_CONTEXT     pLeftSideContext;
    HijackAction      action;
    UINT              debugCounter;
};

//
// DebuggerIPCSecondChanceData holds info communicated from the RS
// to the LS when setting up a second chance exception hijack. This is
// used when Win32 debugging only.
//
struct MSLAYOUT DebuggerIPCSecondChanceData
{
    DT_CONTEXT       threadContext;
};



//-----------------------------------------------------------------------------
// This struct holds pointer from the LS and needs to copy to
// the RS. We have to free the memory on the RS.
// The transfer function is called when the RS first reads the event. At this point,
// the LS is stopped while sending the event. Thus the LS pointers only need to be
// valid while the LS is in SendIPCEvent.
//
// Since this data is in an IPC/Marshallable block, it can't have any Ctors (holders)
// in it.
//-----------------------------------------------------------------------------
struct MSLAYOUT Ls_Rs_BaseBuffer
{
#ifdef RIGHT_SIDE_COMPILE
protected:
    // copy data can happen on both LS and RS. In LS case,
    // ReadProcessMemory is really reading from its own process memory.
    //
    void CopyLSDataToRSWorker(ICorDebugDataTarget * pTargethProcess);

    // retrieve the RS data and own it
    BYTE *TransferRSDataWorker()
    {
        BYTE *pbRS = m_pbRS;
        m_pbRS = NULL;
        return pbRS;
    }
public:


    void CleanUp()
    {
        if (m_pbRS != NULL)
        {
            delete [] m_pbRS;
            m_pbRS = NULL;
        }
    }
#else
public:
    // Only LS can call this API
    void SetLsData(BYTE *pbLS, DWORD cbSize)
    {
        m_pbRS = NULL;
        m_pbLS = pbLS;
        m_cbSize = cbSize;
    }
#endif // RIGHT_SIDE_COMPILE

public:
    // Common APIs.
    DWORD  GetSize() { return m_cbSize; }



protected:
    // Size of data in bytes
    DWORD  m_cbSize;

    // If this is non-null, pointer into LS for buffer.
    // LS can free this after the debug event is continued.
    BYTE  *m_pbLS; // @dbgtodo  cross-plat- for cross-platform purposes, this should be a TADDR

    // If this is non-null, pointer into RS for buffer. RS must then free this.
    // This buffer was copied from the LS (via CopyLSDataToRSWorker).
    BYTE  *m_pbRS;
};

//-----------------------------------------------------------------------------
// Byte wrapper around the buffer.
//-----------------------------------------------------------------------------
struct MSLAYOUT Ls_Rs_ByteBuffer : public Ls_Rs_BaseBuffer
{
#ifdef RIGHT_SIDE_COMPILE
    BYTE *GetRSPointer()
    {
        return m_pbRS;
    }

    void CopyLSDataToRS(ICorDebugDataTarget * pTarget);
    BYTE *TransferRSData()
    {
        return TransferRSDataWorker();
    }
#endif
};

//-----------------------------------------------------------------------------
// Wrapper around a Ls_rS_Buffer to get it as a string.
// This can also do some sanity checking.
//-----------------------------------------------------------------------------
struct MSLAYOUT Ls_Rs_StringBuffer : public Ls_Rs_BaseBuffer
{
#ifdef RIGHT_SIDE_COMPILE
    const WCHAR * GetString()
    {
        return reinterpret_cast<const WCHAR*> (m_pbRS);
    }

    // Copy over the string.
    void CopyLSDataToRS(ICorDebugDataTarget * pTarget);

    // Caller will pick up ownership.
    // Since caller will delete this data, we can't give back a constant pointer.
    WCHAR * TransferStringData()
    {
        return reinterpret_cast<WCHAR*> (TransferRSDataWorker());
    }
#endif
};


// Data for an Managed Debug Assistant Probe (MDA).
struct MSLAYOUT DebuggerMDANotification
{
    Ls_Rs_StringBuffer szName;
    Ls_Rs_StringBuffer szDescription;
    Ls_Rs_StringBuffer szXml;
    DWORD        dwOSThreadId;
    CorDebugMDAFlags flags;
};


// The only remaining problem is that register number mappings are different for each platform. It turns out
// that the debugger only uses REGNUM_SP and REGNUM_AMBIENT_SP though, so we can just virtualize these two for
// the target platform.
// Keep this is sync with the definitions in inc/corinfo.h.
#if defined(TARGET_X86)
#define DBG_TARGET_REGNUM_SP 4
#define DBG_TARGET_REGNUM_AMBIENT_SP 9
#ifdef TARGET_X86
static_assert_no_msg(DBG_TARGET_REGNUM_SP == ICorDebugInfo::REGNUM_SP);
static_assert_no_msg(DBG_TARGET_REGNUM_AMBIENT_SP == ICorDebugInfo::REGNUM_AMBIENT_SP);
#endif // TARGET_X86
#elif defined(TARGET_AMD64)
#define DBG_TARGET_REGNUM_SP 4
#define DBG_TARGET_REGNUM_AMBIENT_SP 17
#ifdef TARGET_AMD64
static_assert_no_msg(DBG_TARGET_REGNUM_SP == ICorDebugInfo::REGNUM_SP);
static_assert_no_msg(DBG_TARGET_REGNUM_AMBIENT_SP == ICorDebugInfo::REGNUM_AMBIENT_SP);
#endif // TARGET_AMD64
#elif defined(TARGET_ARM)
#define DBG_TARGET_REGNUM_SP 13
#define DBG_TARGET_REGNUM_AMBIENT_SP 17
#ifdef TARGET_ARM
C_ASSERT(DBG_TARGET_REGNUM_SP == ICorDebugInfo::REGNUM_SP);
C_ASSERT(DBG_TARGET_REGNUM_AMBIENT_SP == ICorDebugInfo::REGNUM_AMBIENT_SP);
#endif // TARGET_ARM
#elif defined(TARGET_ARM64)
#define DBG_TARGET_REGNUM_SP 31
#define DBG_TARGET_REGNUM_AMBIENT_SP 34
#ifdef TARGET_ARM64
C_ASSERT(DBG_TARGET_REGNUM_SP == ICorDebugInfo::REGNUM_SP);
C_ASSERT(DBG_TARGET_REGNUM_AMBIENT_SP == ICorDebugInfo::REGNUM_AMBIENT_SP);
#endif // TARGET_ARM64
#elif defined(TARGET_LOONGARCH64)
#define DBG_TARGET_REGNUM_SP 3
#define DBG_TARGET_REGNUM_AMBIENT_SP 34
#ifdef TARGET_LOONGARCH64
C_ASSERT(DBG_TARGET_REGNUM_SP == ICorDebugInfo::REGNUM_SP);
C_ASSERT(DBG_TARGET_REGNUM_AMBIENT_SP == ICorDebugInfo::REGNUM_AMBIENT_SP);
#endif
#else
#error Target registers are not defined for this platform
#endif


//
// Event structure that is passed between the Runtime Controller and the
// Debugger Interface. Some types of events are a fixed size and have
// entries in the main union, while others are variable length and have
// more specialized data structures that are attached to the end of this
// structure.
//
struct MSLAYOUT DebuggerIPCEvent
{
    DebuggerIPCEvent*       next;
    DebuggerIPCEventType    type;
    DWORD             processId;
    DWORD             threadId;
    VMPTR_AppDomain   vmAppDomain;
    VMPTR_Thread      vmThread;

    HRESULT           hr;
    bool              replyRequired;
    bool              asyncSend;

    union MSLAYOUT
    {
        struct MSLAYOUT
        {
            // Pointer to a BOOL in the target.
            CORDB_ADDRESS pfBeingDebugged;
        } LeftSideStartupData;

        struct MSLAYOUT
        {
            // Module whose metadata is being updated
            // This tells the RS that the metadata for that module has become invalid.
            VMPTR_DomainAssembly vmDomainAssembly;

        } MetadataUpdateData;

        struct MSLAYOUT
        {
            // Handle to CLR's internal appdomain object.
            VMPTR_AppDomain vmAppDomain;
        } AppDomainData;

        struct MSLAYOUT
        {
            VMPTR_DomainAssembly vmDomainAssembly;
        } AssemblyData;

#ifdef TEST_DATA_CONSISTENCY
        // information necessary for testing whether the LS holds a lock on data
        // the RS needs to inspect. See code:DataTest::TestDataSafety and
        // code:IDacDbiInterface::TestCrst for more information
        struct MSLAYOUT
        {
            // the lock to be tested
            VMPTR_Crst vmCrst;
            // indicates whether the LS holds the lock
            bool       fOkToTake;
        } TestCrstData;

        // information necessary for testing whether the LS holds a lock on data
        // the RS needs to inspect. See code:DataTest::TestDataSafety and
        // code:IDacDbiInterface::TestCrst for more information
        struct MSLAYOUT
        {
            // the lock to be tested
            VMPTR_SimpleRWLock vmRWLock;
            // indicates whether the LS holds the lock
            bool               fOkToTake;
        } TestRWLockData;
#endif // TEST_DATA_CONSISTENCY

        // Debug event that a module has been loaded
        struct MSLAYOUT
        {
            // Module that was just loaded.
            VMPTR_DomainAssembly vmDomainAssembly;
        }LoadModuleData;


        struct MSLAYOUT
        {
            VMPTR_DomainAssembly vmDomainAssembly;
            LSPTR_ASSEMBLY debuggerAssemblyToken;
        } UnloadModuleData;


        // The given module's pdb has been updated.
        // Queury PDB from OOP
        struct MSLAYOUT
        {
            VMPTR_DomainAssembly vmDomainAssembly;
        } UpdateModuleSymsData;

        DebuggerMDANotification MDANotification;

        struct MSLAYOUT
        {
            LSPTR_BREAKPOINT breakpointToken;
            mdMethodDef  funcMetadataToken;
            VMPTR_DomainAssembly vmDomainAssembly;
            bool         isIL;
            SIZE_T       offset;
            SIZE_T       encVersion;
            LSPTR_METHODDESC  nativeCodeMethodDescToken; // points to the MethodDesc if !isIL
        } BreakpointData;

        struct MSLAYOUT
        {
            LSPTR_BREAKPOINT breakpointToken;
        } BreakpointSetErrorData;

        struct MSLAYOUT
        {
#ifdef FEATURE_DATABREAKPOINT
            CONTEXT context;
#else
            int dummy;
#endif
        } DataBreakpointData;

        struct MSLAYOUT
        {
            LSPTR_STEPPER        stepperToken;
            VMPTR_Thread         vmThreadToken;
            FramePointer         frameToken;
            bool                 stepIn;
            bool                 rangeIL;
            bool                 IsJMCStop;
            unsigned int         totalRangeCount;
            CorDebugStepReason   reason;
            CorDebugUnmappedStop rgfMappingStop;
            CorDebugIntercept    rgfInterceptStop;
            unsigned int         rangeCount;
            COR_DEBUG_STEP_RANGE range; //note that this is an array
        } StepData;

        struct MSLAYOUT
        {
            // An unvalidated GC-handle
            VMPTR_OBJECTHANDLE GCHandle;
        } GetGCHandleInfo;

        struct MSLAYOUT
        {
            // An unvalidated GC-handle for which we're returning the results
            LSPTR_OBJECTHANDLE GCHandle;

            // The following are initialized by the LS in response to our query:
            VMPTR_AppDomain vmAppDomain; // AD that handle is in (only applicable if fValid).
            bool            fValid; // Did the LS determine the GC handle to be valid?
        } GetGCHandleInfoResult;

        // Allocate memory on the left-side
        struct MSLAYOUT
        {
            ULONG      bufSize;             // number of bytes to allocate
        } GetBuffer;

        // Memory allocated on the left-side
        struct MSLAYOUT
        {
            void        *pBuffer;           // LS pointer to the buffer allocated
            HRESULT     hr;                 // success / failure
        } GetBufferResult;

        // Free a buffer allocated on the left-side with GetBuffer
        struct MSLAYOUT
        {
            void        *pBuffer;           // Pointer previously returned in GetBufferResult
        } ReleaseBuffer;

        struct MSLAYOUT
        {
            HRESULT     hr;
        } ReleaseBufferResult;

        // Apply an EnC edit
        struct MSLAYOUT
        {
            VMPTR_DomainAssembly vmDomainAssembly;      // Module to edit
            DWORD cbDeltaMetadata;              // size of blob pointed to by pDeltaMetadata
            CORDB_ADDRESS pDeltaMetadata;       // pointer to delta metadata in debuggee
                                                // it's the RS's responsibility to allocate and free
                                                // this (and pDeltaIL) using GetBuffer / ReleaseBuffer
            CORDB_ADDRESS pDeltaIL;             // pointer to delta IL in debugee
            DWORD cbDeltaIL;                    // size of blob pointed to by pDeltaIL
        } ApplyChanges;

        struct MSLAYOUT
        {
            HRESULT hr;
        } ApplyChangesResult;

        struct MSLAYOUT
        {
            mdTypeDef   classMetadataToken;
            VMPTR_DomainAssembly vmDomainAssembly;
            LSPTR_ASSEMBLY classDebuggerAssemblyToken;
        } LoadClass;

        struct MSLAYOUT
        {
            mdTypeDef   classMetadataToken;
            VMPTR_DomainAssembly vmDomainAssembly;
            LSPTR_ASSEMBLY classDebuggerAssemblyToken;
        } UnloadClass;

        struct MSLAYOUT
        {
            VMPTR_DomainAssembly vmDomainAssembly;
            bool  flag;
        } SetClassLoad;

        struct MSLAYOUT
        {
            VMPTR_OBJECTHANDLE vmExceptionHandle;
            bool        firstChance;
            bool        continuable;
        } Exception;

        struct MSLAYOUT
        {
            VMPTR_Thread   vmThreadToken;
        } ClearException;

        struct MSLAYOUT
        {
            void        *address;
        } IsTransitionStub;

        struct MSLAYOUT
        {
            bool        isStub;
        } IsTransitionStubResult;

        struct MSLAYOUT
        {
            CORDB_ADDRESS    startAddress;
            bool             fCanSetIPOnly;
            VMPTR_Thread     vmThreadToken;
            VMPTR_DomainAssembly vmDomainAssembly;
            mdMethodDef      mdMethod;
            VMPTR_MethodDesc vmMethodDesc;
            SIZE_T           offset;
            bool             fIsIL;
            void *           firstExceptionHandler;
        } SetIP; // this is also used for CanSetIP

        struct MSLAYOUT
        {
            int iLevel;

            EmbeddedIPCString<MAX_LOG_SWITCH_NAME_LEN + 1> szCategory;
            Ls_Rs_StringBuffer szContent;
        } FirstLogMessage;

        struct MSLAYOUT
        {
            int iLevel;
            int iReason;

            EmbeddedIPCString<MAX_LOG_SWITCH_NAME_LEN + 1> szSwitchName;
            EmbeddedIPCString<MAX_LOG_SWITCH_NAME_LEN + 1> szParentSwitchName;
        } LogSwitchSettingMessage;

        // information needed to send to the RS as part of a custom notification from the target
        struct MSLAYOUT
        {
            // Domain file for the domain in which the notification occurred
            VMPTR_DomainAssembly vmDomainAssembly;

            // metadata token for the type of the CustomNotification object's type
            mdTypeDef    classToken;
        } CustomNotification;

        struct MSLAYOUT
        {
            VMPTR_Thread vmThreadToken;
            CorDebugThreadState debugState;
        } SetAllDebugState;

        DebuggerIPCE_FuncEvalInfo FuncEval;

        struct MSLAYOUT
        {
            CORDB_ADDRESS argDataArea;
            LSPTR_DEBUGGEREVAL debuggerEvalKey;
        } FuncEvalSetupComplete;

        struct MSLAYOUT
        {
            RSPTR_CORDBEVAL funcEvalKey;
            bool            successful;
            bool            aborted;
            void           *resultAddr;

            // AppDomain that the result is in.
            VMPTR_AppDomain vmAppDomain;

            VMPTR_OBJECTHANDLE vmObjectHandle;
            DebuggerIPCE_ExpandedTypeData resultType;
        } FuncEvalComplete;

        struct MSLAYOUT
        {
            LSPTR_DEBUGGEREVAL debuggerEvalKey;
        } FuncEvalAbort;

        struct MSLAYOUT
        {
            LSPTR_DEBUGGEREVAL debuggerEvalKey;
        } FuncEvalRudeAbort;

        struct MSLAYOUT
        {
            LSPTR_DEBUGGEREVAL debuggerEvalKey;
        } FuncEvalCleanup;

        struct MSLAYOUT
        {
            void           *objectRefAddress;
            VMPTR_OBJECTHANDLE vmObjectHandle;
            void           *newReference;
        } SetReference;

        struct MSLAYOUT
        {
            NameChangeType  eventType;
            VMPTR_AppDomain vmAppDomain;
            VMPTR_Thread    vmThread;
        } NameChange;

        struct MSLAYOUT
        {
            VMPTR_DomainAssembly vmDomainAssembly;
            BOOL             fAllowJitOpts;
            BOOL             fEnableEnC;
        } JitDebugInfo;

        // EnC Remap opportunity
        struct MSLAYOUT
        {
            VMPTR_DomainAssembly vmDomainAssembly;
            mdMethodDef funcMetadataToken ;        // methodDef of function with remap opportunity
            SIZE_T          currentVersionNumber;  // version currently executing
            SIZE_T          resumeVersionNumber;   // latest version
            SIZE_T          currentILOffset;       // the IL offset of the current IP
            SIZE_T          *resumeILOffset;       // pointer into left-side where an offset to resume
                                                   // to should be written if remap is desired.
        } EnCRemap;

        // EnC Remap has taken place
        struct MSLAYOUT
        {
            VMPTR_DomainAssembly vmDomainAssembly;
            mdMethodDef funcMetadataToken;         // methodDef of function that was remapped
        } EnCRemapComplete;

        // Notification that the LS is about to update a CLR data structure to account for a
        // specific edit made by EnC (function add/update or field add).
        struct MSLAYOUT
        {
            VMPTR_DomainAssembly vmDomainAssembly;
            mdToken         memberMetadataToken;   // Either a methodDef token indicating the function that
                                                   // was updated/added, or a fieldDef token indicating the
                                                   // field which was added.
            mdTypeDef       classMetadataToken;    // TypeDef token of the class in which the update was made
            SIZE_T          newVersionNumber;      // The new function/module version
        } EnCUpdate;

        struct MSLAYOUT
        {
            void      *oldData;
            void      *newData;
            DebuggerIPCE_BasicTypeData type;
        } SetValueClass;


        // Event used to tell LS if a single function is user or non-user code.
        // Same structure used to get function status.
        // @todo - Perhaps we can bundle these up so we can set multiple funcs w/ 1 event?
        struct MSLAYOUT
        {
            VMPTR_DomainAssembly vmDomainAssembly;
            mdMethodDef     funcMetadataToken;
            DWORD           dwStatus;
        } SetJMCFunctionStatus;

        struct MSLAYOUT
        {
            TASKID      taskid;
        } GetThreadForTaskId;

        struct MSLAYOUT
        {
            VMPTR_Thread vmThreadToken;
        } GetThreadForTaskIdResult;

        struct MSLAYOUT
        {
            CONNID     connectionId;
        } ConnectionChange;

        struct MSLAYOUT
        {
            CONNID     connectionId;
            EmbeddedIPCString<MAX_LONGPATH> wzConnectionName;
        } CreateConnection;

        struct MSLAYOUT
        {
            void               *objectToken;
            CorDebugHandleType handleType;
        } CreateHandle;

        struct MSLAYOUT
        {
            VMPTR_OBJECTHANDLE vmObjectHandle;
        } CreateHandleResult;

        // used in DB_IPCE_DISPOSE_HANDLE event
        struct MSLAYOUT
        {
            VMPTR_OBJECTHANDLE vmObjectHandle;
            CorDebugHandleType handleType;
        } DisposeHandle;

        struct MSLAYOUT
        {
            FramePointer                  framePointer;
            SIZE_T                        nOffset;
            CorDebugExceptionCallbackType eventType;
            DWORD                         dwFlags;
            VMPTR_OBJECTHANDLE            vmExceptionHandle;
        } ExceptionCallback2;

        struct MSLAYOUT
        {
            CorDebugExceptionUnwindCallbackType eventType;
            DWORD                               dwFlags;
        } ExceptionUnwind;

        struct MSLAYOUT
        {
            VMPTR_Thread vmThreadToken;
            FramePointer frameToken;
        } InterceptException;

        struct MSLAYOUT
        {
            VMPTR_Module vmModule;
            void * pMetadataStart;
            ULONG nMetadataSize;
        } MetadataUpdateRequest;
    };
};


// When using a network transport rather than shared memory buffers CorDBIPC_BUFFER_SIZE is the upper bound
// for a single DebuggerIPCEvent structure. This now relates to the maximal size of a network message and is
// orthogonal to the host's page size. Round the buffer size up to a multiple of 8 since MSVC seems more
// aggressive in this regard than gcc.
#define CorDBIPC_TRANSPORT_BUFFER_SIZE (((sizeof(DebuggerIPCEvent) + 7) / 8) * 8)

// A DebuggerIPCEvent must fit in the send & receive buffers, which are CorDBIPC_BUFFER_SIZE bytes.
static_assert_no_msg(sizeof(DebuggerIPCEvent) <= CorDBIPC_BUFFER_SIZE);
static_assert_no_msg(CorDBIPC_TRANSPORT_BUFFER_SIZE <= CorDBIPC_BUFFER_SIZE);

// 2*sizeof(WCHAR) for the two string terminating characters in the FirstLogMessage
#define LOG_MSG_PADDING         4

#endif /* _DbgIPCEvents_h_ */
