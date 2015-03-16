//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// StubMgr.h
//

//
// The stub manager exists so that the debugger can accurately step through 
// the myriad stubs & wrappers which exist in the EE, without imposing undue 
// overhead on the stubs themselves.
//
// Each type of stub (except those which the debugger can treat as atomic operations)
// needs to have a stub manager to represent it.  The stub manager is responsible for
// (a) identifying the stub as such, and
// (b) tracing into the stub & reporting what the stub will call.  This
//        report can consist of
//              (i) a managed code address
//              (ii) an unmanaged code address
//              (iii) another stub address
//              (iv) a "frame patch" address - that is, an address in the stub, 
//                      which the debugger can patch. When the patch is hit, the debugger
//                      will query the topmost frame to trace itself.  (Thus this is 
//                      a way of deferring the trace logic to the frame which the stub
//                      will push.)
//
// The set of stub managers is extensible, but should be kept to a reasonable number
// as they are currently linearly searched & queried for each stub.
//


#ifndef __stubmgr_h__
#define __stubmgr_h__

#include "simplerwlock.hpp"

// When 'TraceStub' returns, it gives the address of where the 'target' is for a stub'
// TraceType indicates what this 'target' is
enum TraceType
{
    TRACE_ENTRY_STUB,               // Stub goes to an unmanaged entry stub 
    TRACE_STUB,                     // Stub goes to another stub
    TRACE_UNMANAGED,                // Stub goes to unmanaged code
    TRACE_MANAGED,                  // Stub goes to Jitted code
    TRACE_UNJITTED_METHOD,          // Is the prestub, since there is no code, the address will actually be a MethodDesc*

    TRACE_FRAME_PUSH,               // Don't know where stub goes, stop at address, and then ask the frame that is on the stack
    TRACE_MGR_PUSH,                 // Don't know where stub goes, stop at address then call TraceManager() below to find out 

    TRACE_OTHER                     // We are going somewhere you can't step into (eg. ee helper function)
};

class StubManager;
class SString;

class DebuggerRCThread;

enum StubCodeBlockKind : int;

// A TraceDestination describes where code is going to call. This can be used by the Debugger's Step-In functionality
// to skip through stubs and place a patch directly at a call's target.
// TD are supplied by the stubmanagers.
class TraceDestination
{
public:
    friend class DebuggerRCThread;
    
    TraceDestination() { }

#ifdef _DEBUG
    // Get a string representation of this TraceDestination
    // Uses the supplied buffer to store the memory (or may return a string literal).
    // This will also print the TD's arguments.    
    const WCHAR * DbgToString(SString &buffer);
#endif

    // Initialize for unmanaged code.
    // The addr is in unmanaged code. Used for Step-in from managed to native.
    void InitForUnmanaged(PCODE addr)
    {
        STATIC_CONTRACT_SO_TOLERANT;
        this->type = TRACE_UNMANAGED;
        this->address = addr;
        this->stubManager = NULL;        
    }

    // The addr is inside jitted code (eg, there's a JitManaged that will claim it)
    void InitForManaged(PCODE addr)
    {
        STATIC_CONTRACT_SO_TOLERANT;
        this->type = TRACE_MANAGED;
        this->address = addr;
        this->stubManager = NULL;
    }

    // Initialize for an unmanaged entry stub.
    void InitForUnmanagedStub(PCODE addr)
    {
        STATIC_CONTRACT_SO_TOLERANT;
        this->type = TRACE_ENTRY_STUB;
        this->address = addr;
        this->stubManager = NULL;
    }

    // Initialize for a stub.
    void InitForStub(PCODE addr)
    {
        STATIC_CONTRACT_SO_TOLERANT;
        this->type = TRACE_STUB;
        this->address = addr;
        this->stubManager = NULL;
    }

    // Init for a managed unjitted method.
    // This will place an IL patch that will get bound when the debugger gets a Jit complete
    // notification for this method.
    // If pDesc is a wrapper methoddesc, we will unwrap it.
    void InitForUnjittedMethod(MethodDesc * pDesc);

    // Place a patch at the given addr, and then when it's hit,
    // call pStubManager->TraceManager() to get the next TraceDestination.
    void InitForManagerPush(PCODE addr, StubManager * pStubManager)
    {
        STATIC_CONTRACT_SO_TOLERANT;
        this->type = TRACE_MGR_PUSH;
        this->address = addr;
        this->stubManager = pStubManager;
    }

    // Place a patch at the given addr, and then when it's hit
    // call GetThread()->GetFrame()->TraceFrame() to get the next TraceDestination.
    // This address must be safe to run a callstack at.
    void InitForFramePush(PCODE addr)
    {
        this->type = TRACE_FRAME_PUSH;
        this->address = addr;
        this->stubManager = NULL;
    }

    // Nobody recognized the target address. We will not be able to step-in to it.
    // This is ok if the target just calls into mscorwks (such as an Fcall) because
    // there's no managed code to step in to, and we don't support debugging the CLR
    // itself, so there's no native code to step into either.
    void InitForOther(PCODE addr)
    {
        this->type = TRACE_OTHER;
        this->address = addr;
        this->stubManager = NULL;
    }

    // Accessors
    TraceType GetTraceType() { return type; }
    PCODE GetAddress() 
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(type != TRACE_UNJITTED_METHOD);
        return address; 
    }
    MethodDesc* GetMethodDesc()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(type == TRACE_UNJITTED_METHOD);
        return pDesc;
    }    

    StubManager * GetStubManager()
    {
        return stubManager;
    }

    // Expose this b/c DebuggerPatchTable::AddPatchForAddress() needs it.
    // Ideally we'd get rid of this.
    void Bad_SetTraceType(TraceType t)
    {
        this->type = t;
    }
private:
    TraceType                       type;               // The kind of code the stub is going to
    PCODE                           address;            // Where the stub is going    
    StubManager                     *stubManager;       // The manager that claims this stub
    MethodDesc                      *pDesc;
};

// For logging
#ifdef LOGGING
    void LogTraceDestination(const char * szHint, PCODE stubAddr, TraceDestination * pTrace);
    #define LOG_TRACE_DESTINATION(_tracedestination, stubAddr, _stHint)  LogTraceDestination(_stHint, stubAddr, _tracedestination)
#else
    #define LOG_TRACE_DESTINATION(_tracedestination, stubAddr, _stHint)    
#endif

typedef VPTR(class StubManager) PTR_StubManager;

class StubManager
{
    friend class StubManagerIterator;

    VPTR_BASE_VTABLE_CLASS(StubManager)
    
  public:
    // Startup and shutdown the global stubmanager service.
    static void InitializeStubManagers();
    static void TerminateStubManagers();

    // Does any sub manager recognise this EIP?
    static BOOL IsStub(PCODE stubAddress)
    {
        WRAPPER_NO_CONTRACT;
        return FindStubManager(stubAddress) != NULL;
    }

    // Find stub manager for given code address
    static PTR_StubManager FindStubManager(PCODE stubAddress);
        
    // Look for stubAddress, if found return TRUE, and set 'trace' to 
    static BOOL TraceStub(PCODE stubAddress, TraceDestination *trace);
    
    // if 'trace' indicates TRACE_STUB, keep calling TraceStub on 'trace', until you get out of all stubs
    // returns true if successfull
    static BOOL FollowTrace(TraceDestination *trace);
    
#ifdef DACCESS_COMPILE
    static void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

    static void AddStubManager(StubManager *mgr);

    // NOTE: Very important when using this. It is not thread safe, except in this very
    //       limited scenario: the thread must have the runtime suspended.
    static void UnlinkStubManager(StubManager *mgr);
    
#ifndef DACCESS_COMPILE
    StubManager();
    virtual ~StubManager();
#endif


#ifdef _DEBUG
    // Debug helper to help identify stub-managers. Make it pure to force stub managers to implement it.
    virtual const char * DbgGetName() = 0;
#endif

    // Only Stubmanagers that return 'TRACE_MGR_PUSH' as a trace type need to implement this function
    // Fills in 'trace' (the target), and 'pRetAddr' (the method that called the stub) (this is needed
    // as a 'fall back' so that the debugger can at least stop when the stub returns.  
    virtual BOOL TraceManager(Thread *thread, TraceDestination *trace,
                              T_CONTEXT *pContext, BYTE **pRetAddr)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(!"Default impl of TraceManager should never be called!");
        return FALSE;
    }

    // The worker for IsStub. This calls CheckIsStub_Internal, but wraps it w/ 
    // a try-catch.
    BOOL CheckIsStub_Worker(PCODE stubStartAddress);



#ifdef _DEBUG
public:
    //-----------------------------------------------------------------------------
    // Debugging Stubmanager bugs is very painful. You need to figure out
    // how you go to where you got and which stub-manager is at fault.
    // To help with this, we track a rolling log so that we can give very
    // informative asserts. this log is not thread-safe, but we really only expect
    // a single stub-manager usage at a time.
    //
    // A stub manager for a step-in operation may be used across 
    // both the helper thread and then the managed thread doing the step-in.
    // These threads will coordinate to have exclusive access (helper will only access
    // when stopped; and managed thread will only access when running).
    //
    // It's also possible (but rare) for a single thread to have multiple step-in operations.
    // Since that's so rare, no present need to expand our logging to support it.    
    //-----------------------------------------------------------------------------


    static bool IsStubLoggingEnabled();

    // Call to reset the log. This is used at the start of a new step-operation.    
    static void DbgBeginLog(TADDR addrCallInstruction, TADDR addrCallTarget);
    static void DbgFinishLog();
    
    // Log arbitrary string. This is a nop if it's outside the Begin/Finish window.
    // We could consider making each log entry type-safe (and thus avoid the string operations).
    static void DbgWriteLog(const CHAR *format, ...);
    
    // Get the log as a string.
    static void DbgGetLog(SString * pStringOut);

protected:
    // Implement log as a SString.
    static SString * s_pDbgStubManagerLog;

    static CrstStatic s_DbgLogCrst;

#endif

        
protected:

    // Each stubmanaged implements this. 
    // This may throw, AV, etc depending on the implementation. This should not 
    // be called directly unless you know exactly what you're doing.
    virtual BOOL CheckIsStub_Internal(PCODE stubStartAddress) = 0;

    // The worker for TraceStub
    virtual BOOL DoTraceStub(PCODE stubStartAddress, TraceDestination *trace) = 0;

#ifdef _DEBUG_IMPL
    static BOOL IsSingleOwner(PCODE stubAddress, StubManager * pOwner);
#endif

#ifdef DACCESS_COMPILE
    virtual void DoEnumMemoryRegions(CLRDataEnumMemoryFlags flags);

public:
    // This is used by DAC to provide more information on who owns a stub.
    virtual LPCWSTR GetStubManagerName(PCODE addr) = 0;
#endif
 
private:
    SPTR_DECL(StubManager, g_pFirstManager);
    PTR_StubManager m_pNextManager;

    static CrstStatic s_StubManagerListCrst;
};

// -------------------------------------------------------
// This just wraps the RangeList methods in a read or
// write lock depending on the operation.
// -------------------------------------------------------

class LockedRangeList : public RangeList
{
  public:
    VPTR_VTABLE_CLASS(LockedRangeList, RangeList)
    
    LockedRangeList() : RangeList(), m_RangeListRWLock(COOPERATIVE_OR_PREEMPTIVE, LOCK_TYPE_DEFAULT)
    {
        LIMITED_METHOD_CONTRACT;
    }

    ~LockedRangeList()
    {
        LIMITED_METHOD_CONTRACT;
    }

  protected:

    virtual BOOL AddRangeWorker(const BYTE *start, const BYTE *end, void *id)
    {
        WRAPPER_NO_CONTRACT;
        SimpleWriteLockHolder lh(&m_RangeListRWLock);
        return RangeList::AddRangeWorker(start,end,id);
    }

    virtual void RemoveRangesWorker(void *id, const BYTE *start = NULL, const BYTE *end = NULL)
    {
        WRAPPER_NO_CONTRACT;
        SimpleWriteLockHolder lh(&m_RangeListRWLock);
        RangeList::RemoveRangesWorker(id,start,end);
    }

    virtual BOOL IsInRangeWorker(TADDR address, TADDR *pID = NULL)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
        SimpleReadLockHolder lh(&m_RangeListRWLock);
        return RangeList::IsInRangeWorker(address, pID);
    }

    SimpleRWLock m_RangeListRWLock;
};

//-----------------------------------------------------------
// Stub manager for the prestub.  Although there is just one, it has
// unique behavior so it gets its own stub manager.
//-----------------------------------------------------------
class ThePreStubManager : public StubManager
{
    VPTR_VTABLE_CLASS(ThePreStubManager, StubManager)
    
  public:
#ifndef DACCESS_COMPILE
    ThePreStubManager() { LIMITED_METHOD_CONTRACT; }
#endif

#ifdef _DEBUG
    virtual const char * DbgGetName() { LIMITED_METHOD_CONTRACT; return "ThePreStubManager"; }
#endif

    virtual BOOL CheckIsStub_Internal(PCODE stubStartAddress);

    virtual BOOL DoTraceStub(PCODE stubStartAddress, TraceDestination *trace);

#ifndef DACCESS_COMPILE
    static void Init(void);
#endif

#ifdef DACCESS_COMPILE
  protected:
    virtual LPCWSTR GetStubManagerName(PCODE addr)
        { LIMITED_METHOD_CONTRACT; return W("ThePreStub"); }
#endif
};

// -------------------------------------------------------
// Stub manager classes for method desc prestubs & normal
// frame-pushing, StubLinker created stubs
// -------------------------------------------------------

typedef VPTR(class PrecodeStubManager) PTR_PrecodeStubManager;

class PrecodeStubManager : public StubManager
{
    VPTR_VTABLE_CLASS(PrecodeStubManager, StubManager)

  public:

    SPTR_DECL(PrecodeStubManager, g_pManager);

#ifdef _DEBUG
        // Debug helper to help identify stub-managers.
        virtual const char * DbgGetName() { LIMITED_METHOD_CONTRACT; return "PrecodeStubManager"; }
#endif


    static void Init();

#ifndef DACCESS_COMPILE
    PrecodeStubManager() {LIMITED_METHOD_CONTRACT;}
    ~PrecodeStubManager() {WRAPPER_NO_CONTRACT;}
#endif

  public:
    virtual BOOL CheckIsStub_Internal(PCODE stubStartAddress);

    virtual BOOL DoTraceStub(PCODE stubStartAddress, TraceDestination *trace);
#ifndef DACCESS_COMPILE
    virtual BOOL TraceManager(Thread *thread,
                              TraceDestination *trace,
                              CONTEXT *pContext,
                              BYTE **pRetAddr);
#endif

#ifdef DACCESS_COMPILE
    virtual void DoEnumMemoryRegions(CLRDataEnumMemoryFlags flags);

  protected:
    virtual LPCWSTR GetStubManagerName(PCODE addr)
        { LIMITED_METHOD_CONTRACT; return W("MethodDescPrestub"); }
#endif
};

// Note that this stub was written by a debugger guy, and thus when he refers to 'multicast'
// stub, he really means multi or single cast stub.  This was done b/c the same stub
// services both types of stub.
// Note from the debugger guy: the way to understand what this manager does is to
// first grok EmitMulticastInvoke for the platform you're working on (right now, just x86).
// Then return here, and understand that (for x86) the only way we know which method
// we're going to invoke next is by inspecting EDI when we've got the debuggee stopped
// in the stub, and so our trace frame will either (FRAME_PUSH) put a breakpoint
// in the stub, or (if we hit the BP) examine EDI, etc, & figure out where we're going next.

typedef VPTR(class StubLinkStubManager) PTR_StubLinkStubManager;

class StubLinkStubManager : public StubManager
{
    VPTR_VTABLE_CLASS(StubLinkStubManager, StubManager)

  public:

#ifdef _DEBUG
    virtual const char * DbgGetName() { LIMITED_METHOD_CONTRACT; return "StubLinkStubManager"; }
#endif    


    SPTR_DECL(StubLinkStubManager, g_pManager);

    static void Init();

#ifndef DACCESS_COMPILE
    StubLinkStubManager() : StubManager(), m_rangeList() {LIMITED_METHOD_CONTRACT;}
    ~StubLinkStubManager() {WRAPPER_NO_CONTRACT;}
#endif
  
  protected:
    LockedRangeList m_rangeList;
  public:
    // Get dac-ized pointer to rangelist.
    PTR_RangeList GetRangeList() 
    {
        SUPPORTS_DAC;

        TADDR addr = PTR_HOST_MEMBER_TADDR(StubLinkStubManager, this, m_rangeList);
        return PTR_RangeList(addr);
    }


    virtual BOOL CheckIsStub_Internal(PCODE stubStartAddress);

    virtual BOOL DoTraceStub(PCODE stubStartAddress, TraceDestination *trace);
#ifndef DACCESS_COMPILE
    virtual BOOL TraceManager(Thread *thread,
                              TraceDestination *trace,
                              CONTEXT *pContext,
                              BYTE **pRetAddr);
#endif

#ifdef DACCESS_COMPILE
    virtual void DoEnumMemoryRegions(CLRDataEnumMemoryFlags flags);

  protected:
    virtual LPCWSTR GetStubManagerName(PCODE addr)
        { LIMITED_METHOD_CONTRACT; return W("StubLinkStub"); }
#endif
} ;

// Stub manager for thunks.

typedef VPTR(class ThunkHeapStubManager) PTR_ThunkHeapStubManager;

class ThunkHeapStubManager : public StubManager
{
    VPTR_VTABLE_CLASS(ThunkHeapStubManager, StubManager)

  public:

    SPTR_DECL(ThunkHeapStubManager, g_pManager);

    static void Init();

#ifndef DACCESS_COMPILE
    ThunkHeapStubManager() : StubManager(), m_rangeList() { LIMITED_METHOD_CONTRACT; }
    ~ThunkHeapStubManager() {WRAPPER_NO_CONTRACT;}
#endif

#ifdef _DEBUG
    virtual const char * DbgGetName() { LIMITED_METHOD_CONTRACT; return "ThunkHeapStubManager"; }
#endif

  protected:
    LockedRangeList m_rangeList;
  public:
    // Get dac-ized pointer to rangelist.
    PTR_RangeList GetRangeList() 
    {
        SUPPORTS_DAC;
        TADDR addr = PTR_HOST_MEMBER_TADDR(ThunkHeapStubManager, this, m_rangeList);
        return PTR_RangeList(addr);
    }
    virtual BOOL CheckIsStub_Internal(PCODE stubStartAddress);

  private:
    virtual BOOL DoTraceStub(PCODE stubStartAddress, TraceDestination *trace);

#ifdef DACCESS_COMPILE
    virtual void DoEnumMemoryRegions(CLRDataEnumMemoryFlags flags);

  protected:
    virtual LPCWSTR GetStubManagerName(PCODE addr)
        { LIMITED_METHOD_CONTRACT; return W("ThunkHeapStub"); }
#endif
};

//
// Stub manager for jump stubs created by ExecutionManager::jumpStub()
// These are currently used only on the 64-bit targets IA64 and AMD64
//
typedef VPTR(class JumpStubStubManager) PTR_JumpStubStubManager;

class JumpStubStubManager : public StubManager
{
    VPTR_VTABLE_CLASS(JumpStubStubManager, StubManager)

  public:

    SPTR_DECL(JumpStubStubManager, g_pManager);

    static void Init();

#ifndef DACCESS_COMPILE
    JumpStubStubManager() {LIMITED_METHOD_CONTRACT;}
    ~JumpStubStubManager() {WRAPPER_NO_CONTRACT;}

#endif
  
#ifdef _DEBUG
    virtual const char * DbgGetName() { LIMITED_METHOD_CONTRACT; return "JumpStubStubManager"; }
#endif

    virtual BOOL CheckIsStub_Internal(PCODE stubStartAddress);

    virtual BOOL DoTraceStub(PCODE stubStartAddress, TraceDestination *trace);

#ifdef DACCESS_COMPILE
    virtual void DoEnumMemoryRegions(CLRDataEnumMemoryFlags flags);

  protected:
    virtual LPCWSTR GetStubManagerName(PCODE addr)
        { LIMITED_METHOD_CONTRACT; return W("JumpStub"); }
#endif
};

//
// Stub manager for code sections. It forwards the query to the more appropriate 
// stub manager, or handles the query itself.
//
typedef VPTR(class RangeSectionStubManager) PTR_RangeSectionStubManager;

class RangeSectionStubManager : public StubManager
{
    VPTR_VTABLE_CLASS(RangeSectionStubManager, StubManager)

  public:
    SPTR_DECL(RangeSectionStubManager, g_pManager);

    static void Init();

#ifndef DACCESS_COMPILE
    RangeSectionStubManager() {LIMITED_METHOD_CONTRACT;}
    ~RangeSectionStubManager() {WRAPPER_NO_CONTRACT;}
#endif

    static StubCodeBlockKind GetStubKind(PCODE stubStartAddress);

    static PCODE GetMethodThunkTarget(PCODE stubStartAddress);
  
  public:
#ifdef _DEBUG
    virtual const char * DbgGetName() { LIMITED_METHOD_CONTRACT; return "RangeSectionStubManager"; }
#endif

    virtual BOOL CheckIsStub_Internal(PCODE stubStartAddress);

  private:

    virtual BOOL DoTraceStub(PCODE stubStartAddress, TraceDestination *trace);

#ifndef DACCESS_COMPILE
    virtual BOOL TraceManager(Thread *thread,
                              TraceDestination *trace,
                              CONTEXT *pContext,
                              BYTE **pRetAddr);
#endif

#ifdef DACCESS_COMPILE
    virtual void DoEnumMemoryRegions(CLRDataEnumMemoryFlags flags);

  protected:
    virtual LPCWSTR GetStubManagerName(PCODE addr);
#endif
};

//
// This is the stub manager for IL stubs.
//
typedef VPTR(class ILStubManager) PTR_ILStubManager;

#ifdef FEATURE_COMINTEROP
struct ComPlusCallInfo;
#endif // FEATURE_COMINTEROP

class ILStubManager : public StubManager
{
    VPTR_VTABLE_CLASS(ILStubManager, StubManager)

  public:
    static void Init();

#ifndef DACCESS_COMPILE
    ILStubManager() : StubManager() {WRAPPER_NO_CONTRACT;}
    ~ILStubManager()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            CAN_TAKE_LOCK;     // StubManager::UnlinkStubManager uses a crst
        }
        CONTRACTL_END;
    }
#endif

   public:

#ifdef _DEBUG
    virtual const char * DbgGetName() { LIMITED_METHOD_CONTRACT; return "ILStubManager"; }
#endif

    virtual BOOL CheckIsStub_Internal(PCODE stubStartAddress);

  private:

    virtual BOOL DoTraceStub(PCODE stubStartAddress, TraceDestination *trace);

#ifndef DACCESS_COMPILE
#ifdef FEATURE_COMINTEROP
    static PCODE GetCOMTarget(Object *pThis, ComPlusCallInfo *pComPlusCallInfo);
    static PCODE GetWinRTFactoryTarget(ComPlusCallMethodDesc *pCMD);
#endif // FEATURE_COMINTEROP

    virtual BOOL TraceManager(Thread *thread,
                              TraceDestination *trace,
                              CONTEXT *pContext,
                              BYTE **pRetAddr);
#endif

#ifdef DACCESS_COMPILE
    virtual void DoEnumMemoryRegions(CLRDataEnumMemoryFlags flags);

  protected:
    virtual LPCWSTR GetStubManagerName(PCODE addr)
        { LIMITED_METHOD_CONTRACT; return W("ILStub"); }
#endif
};

// This is used to recognize
//   GenericComPlusCallStub()
//   VarargPInvokeStub()
//   GenericPInvokeCalliHelper()
typedef VPTR(class InteropDispatchStubManager) PTR_InteropDispatchStubManager;

class InteropDispatchStubManager : public StubManager
{
    VPTR_VTABLE_CLASS(InteropDispatchStubManager, StubManager)

  public:
    static void Init();

#ifndef DACCESS_COMPILE
    InteropDispatchStubManager() : StubManager() {WRAPPER_NO_CONTRACT;}
    ~InteropDispatchStubManager() {WRAPPER_NO_CONTRACT;}
#endif

#ifdef _DEBUG
    virtual const char * DbgGetName() { LIMITED_METHOD_CONTRACT; return "InteropDispatchStubManager"; }
#endif

    virtual BOOL CheckIsStub_Internal(PCODE stubStartAddress);

  private:

    virtual BOOL DoTraceStub(PCODE stubStartAddress, TraceDestination *trace);

#ifndef DACCESS_COMPILE
    virtual BOOL TraceManager(Thread *thread,
                              TraceDestination *trace,
                              CONTEXT *pContext,
                              BYTE **pRetAddr);
#endif

#ifdef DACCESS_COMPILE
    virtual void DoEnumMemoryRegions(CLRDataEnumMemoryFlags flags);

  protected:
    virtual LPCWSTR GetStubManagerName(PCODE addr)
        { LIMITED_METHOD_CONTRACT; return W("InteropDispatchStub"); }
#endif
};

//
// Since we don't generate delegate invoke stubs at runtime on WIN64, we
// can't use the StubLinkStubManager for these stubs.  Instead, we create
// an additional DelegateInvokeStubManager instead.
//
typedef VPTR(class DelegateInvokeStubManager) PTR_DelegateInvokeStubManager;

class DelegateInvokeStubManager : public StubManager
{
    VPTR_VTABLE_CLASS(DelegateInvokeStubManager, StubManager)

  public:

    SPTR_DECL(DelegateInvokeStubManager, g_pManager);

    static void Init();

#if !defined(DACCESS_COMPILE)
    DelegateInvokeStubManager() : StubManager(), m_rangeList() {LIMITED_METHOD_CONTRACT;}
    ~DelegateInvokeStubManager() {WRAPPER_NO_CONTRACT;}
#endif // DACCESS_COMPILE

    BOOL AddStub(Stub* pStub);
    void RemoveStub(Stub* pStub);

#ifdef _DEBUG
    virtual const char * DbgGetName() { LIMITED_METHOD_CONTRACT; return "DelegateInvokeStubManager"; }
#endif

    virtual BOOL CheckIsStub_Internal(PCODE stubStartAddress);

#if !defined(DACCESS_COMPILE)
    virtual BOOL TraceManager(Thread *thread, TraceDestination *trace, CONTEXT *pContext, BYTE **pRetAddr);
    static BOOL TraceDelegateObject(BYTE *orDel, TraceDestination *trace);
#endif // DACCESS_COMPILE

  private:

    virtual BOOL DoTraceStub(PCODE stubStartAddress, TraceDestination *trace);

   protected:
    LockedRangeList m_rangeList;
   public:
    // Get dac-ized pointer to rangelist.
    PTR_RangeList GetRangeList() 
    {
        SUPPORTS_DAC;

        TADDR addr = PTR_HOST_MEMBER_TADDR(DelegateInvokeStubManager, this, m_rangeList);
        return PTR_RangeList(addr);
    }


#ifdef DACCESS_COMPILE
    virtual void DoEnumMemoryRegions(CLRDataEnumMemoryFlags flags);

  protected:
    virtual LPCWSTR GetStubManagerName(PCODE addr)
        { LIMITED_METHOD_CONTRACT; return W("DelegateInvokeStub"); }
#endif
};

//---------------------------------------------------------------------------------------
//
// This is the stub manager to help the managed debugger step into a tail call.
// It helps the debugger trace through JIT_TailCall().
//

typedef VPTR(class TailCallStubManager) PTR_TailCallStubManager;

class TailCallStubManager : public StubManager
{
    VPTR_VTABLE_CLASS(TailCallStubManager, StubManager)

public:
    static void Init();

#if !defined(DACCESS_COMPILE)
    TailCallStubManager() : StubManager() {WRAPPER_NO_CONTRACT;}
    ~TailCallStubManager() {WRAPPER_NO_CONTRACT;}

    virtual BOOL TraceManager(Thread * pThread, TraceDestination * pTrace, CONTEXT * pContext, BYTE ** ppRetAddr);

    static bool IsTailCallStubHelper(PCODE code);
#endif // DACCESS_COMPILE

#if defined(_DEBUG)
    virtual const char * DbgGetName() { LIMITED_METHOD_CONTRACT; return "TailCallStubManager"; }
#endif // _DEBUG

    virtual BOOL CheckIsStub_Internal(PCODE stubStartAddress);

private:
    virtual BOOL DoTraceStub(PCODE stubStartAddress, TraceDestination * pTrace);

#if defined(DACCESS_COMPILE)
    virtual void DoEnumMemoryRegions(CLRDataEnumMemoryFlags flags);

protected:
    virtual LPCWSTR GetStubManagerName(PCODE addr) {LIMITED_METHOD_CONTRACT; return W("TailCallStub");}
#endif // !DACCESS_COMPILE
};

//
// Helpers for common value locations in stubs to make stub managers more portable
//
class StubManagerHelpers
{
public:
    static PCODE GetReturnAddress(T_CONTEXT * pContext)
    {
#if defined(_TARGET_X86_)
        return *dac_cast<PTR_PCODE>(pContext->Esp);
#elif defined(_TARGET_AMD64_)
        return *dac_cast<PTR_PCODE>(pContext->Rsp);
#elif defined(_TARGET_ARM_)
        return pContext->Lr;
#elif defined(_TARGET_ARM64_)
        return pContext->Lr;
#else
        PORTABILITY_ASSERT("StubManagerHelpers::GetReturnAddress");
        return NULL;
#endif
    }

    static PTR_Object GetThisPtr(T_CONTEXT * pContext)
    {
#if defined(_TARGET_X86_)
        return dac_cast<PTR_Object>(pContext->Ecx);
#elif defined(_TARGET_AMD64_)
#ifdef UNIX_AMD64_ABI
        return dac_cast<PTR_Object>(pContext->Rdi);
#else
        return dac_cast<PTR_Object>(pContext->Rcx);
#endif
#elif defined(_TARGET_ARM_)
        return dac_cast<PTR_Object>(pContext->R0);
#elif defined(_TARGET_ARM64_)
        return dac_cast<PTR_Object>(pContext->X0);
#else
        PORTABILITY_ASSERT("StubManagerHelpers::GetThisPtr");
        return NULL;
#endif
    }

    static PCODE GetTailCallTarget(T_CONTEXT * pContext)
    {
#if defined(_TARGET_X86_)
        return pContext->Eax;
#elif defined(_TARGET_AMD64_)
        return pContext->Rax;
#elif defined(_TARGET_ARM_)
        return pContext->R12;
#else
        PORTABILITY_ASSERT("StubManagerHelpers::GetTailCallTarget");
        return NULL;
#endif
    }

    static TADDR GetHiddenArg(T_CONTEXT * pContext)
    {
#if defined(_TARGET_X86_)
        return pContext->Eax;
#elif defined(_TARGET_AMD64_)
        return pContext->R10;
#elif defined(_TARGET_ARM_)
        return pContext->R12;
#elif defined(_TARGET_ARM64_)
        return pContext->X15;
#else
        PORTABILITY_ASSERT("StubManagerHelpers::GetHiddenArg");
        return NULL;
#endif
    }

#ifndef CROSSGEN_COMPILE
    static PCODE GetRetAddrFromMulticastILStubFrame(T_CONTEXT * pContext)
    {
        /*
                Following is the callstack corresponding to context  received by ILStubManager::TraceManager.
                This function returns the return address (user code address) where control should return after all 
                delegates in multicast delegate have been executed.
              
                StubHelpers::MulticastDebuggerTraceHelper
                IL_STUB_MulticastDelegate_Invoke
                UserCode which invokes multicast delegate <---
              */

#if defined(_TARGET_X86_)
        return *((PCODE *)pContext->Ebp + 1);      
#elif defined(_TARGET_AMD64_)
        T_CONTEXT context(*pContext);
        Thread::VirtualUnwindCallFrame(&context);
        Thread::VirtualUnwindCallFrame(&context);

        return pContext->Rip;
#elif defined(_TARGET_ARM_)
        return *((PCODE *)pContext->R11 + 1);      
#elif defined(_TARGET_ARM64_)
        return *((PCODE *)pContext->Fp + 1);      
#else
        PORTABILITY_ASSERT("StubManagerHelpers::GetRetAddrFromMulticastILStubFrame");
        return NULL;
#endif
    }
#endif // !CROSSGEN_COMPILE

    static TADDR GetSecondArg(T_CONTEXT * pContext)
    {
#if defined(_TARGET_X86_)
        return pContext->Edx;
#elif defined(_TARGET_AMD64_)
#ifdef UNIX_AMD64_ABI
        return pContext->Rsi;
#else
        return pContext->Rdx;
#endif
#elif defined(_TARGET_ARM_)
        return pContext->R1;
#elif defined(_TARGET_ARM_)
        return pContext->X1;
#else
        PORTABILITY_ASSERT("StubManagerHelpers::GetSecondArg");
        return NULL;
#endif
    }

};

#endif
