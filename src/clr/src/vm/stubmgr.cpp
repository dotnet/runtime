// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "common.h"
#include "stubmgr.h"
#include "virtualcallstub.h"
#include "dllimportcallback.h"
#include "stubhelpers.h"
#include "asmconstants.h"
#ifdef FEATURE_COMINTEROP
#include "olecontexthelpers.h"
#endif

#ifdef LOGGING
const char *GetTType( TraceType tt)
{
    LIMITED_METHOD_CONTRACT;

    switch( tt )
    {
        case TRACE_ENTRY_STUB:      return "TRACE_ENTRY_STUB";
        case TRACE_STUB:            return "TRACE_STUB";
        case TRACE_UNMANAGED:       return "TRACE_UNMANAGED";
        case TRACE_MANAGED:         return "TRACE_MANAGED";
        case TRACE_FRAME_PUSH:      return "TRACE_FRAME_PUSH";
        case TRACE_MGR_PUSH:        return "TRACE_MGR_PUSH";
        case TRACE_OTHER:           return "TRACE_OTHER";
        case TRACE_UNJITTED_METHOD: return "TRACE_UNJITTED_METHOD";
    }
    return "TRACE_REALLY_WACKED";
}

void LogTraceDestination(const char * szHint, PCODE stubAddr, TraceDestination * pTrace)
{
    LIMITED_METHOD_CONTRACT;
    if (pTrace->GetTraceType() == TRACE_UNJITTED_METHOD)
    {
        MethodDesc * md = pTrace->GetMethodDesc();
        LOG((LF_CORDB, LL_INFO10000, "'%s' yields '%s' to method 0x%p for input 0x%p.\n",
            szHint, GetTType(pTrace->GetTraceType()),
            md, stubAddr));
    }
    else
    {
        LOG((LF_CORDB, LL_INFO10000, "'%s' yields '%s' to address 0x%p for input 0x%p.\n",
            szHint, GetTType(pTrace->GetTraceType()),
            pTrace->GetAddress(), stubAddr));
    }
}
#endif

#ifdef _DEBUG
// Get a string representation of this TraceDestination
// Uses the supplied buffer to store the memory (or may return a string literal).
const WCHAR * TraceDestination::DbgToString(SString & buffer)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    const WCHAR * pValue = W("unknown");

#ifndef DACCESS_COMPILE  
    if (!StubManager::IsStubLoggingEnabled())
    {
        return W("<unavailable while native-debugging>");
    }
    // Now that we know we're not interop-debugging, we can safely call new.
    SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE;  

    
    FAULT_NOT_FATAL();

    EX_TRY
    {
        switch(this->type)
        {
            case TRACE_ENTRY_STUB:
                buffer.Printf("TRACE_ENTRY_STUB(addr=0x%p)", GetAddress());
                pValue = buffer.GetUnicode();
                break;

            case TRACE_STUB:
                buffer.Printf("TRACE_STUB(addr=0x%p)", GetAddress());
                pValue = buffer.GetUnicode();
                break;

            case TRACE_UNMANAGED:
                buffer.Printf("TRACE_UNMANAGED(addr=0x%p)", GetAddress());
                pValue = buffer.GetUnicode();
                break;

            case TRACE_MANAGED:
                buffer.Printf("TRACE_MANAGED(addr=0x%p)", GetAddress());
                pValue = buffer.GetUnicode();
                break;

            case TRACE_UNJITTED_METHOD:
            {
                MethodDesc * md = this->GetMethodDesc();
                buffer.Printf("TRACE_UNJITTED_METHOD(md=0x%p, %s::%s)", md, md->m_pszDebugClassName, md->m_pszDebugMethodName);
                pValue = buffer.GetUnicode();                
            }
                break;

            case TRACE_FRAME_PUSH:
                buffer.Printf("TRACE_FRAME_PUSH(addr=0x%p)", GetAddress());
                pValue = buffer.GetUnicode();                
                break;

            case TRACE_MGR_PUSH:
                buffer.Printf("TRACE_MGR_PUSH(addr=0x%p, sm=%s)", GetAddress(), this->GetStubManager()->DbgGetName());
                pValue = buffer.GetUnicode();
                break;

            case TRACE_OTHER:        
                pValue = W("TRACE_OTHER");
                break;
        }
    }
    EX_CATCH
    {
        pValue = W("(OOM while printing TD)");
    }
    EX_END_CATCH(SwallowAllExceptions);    
#endif            
    return pValue;
}
#endif


void TraceDestination::InitForUnjittedMethod(MethodDesc * pDesc)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        MODE_ANY;

        PRECONDITION(CheckPointer(pDesc));
    }
    CONTRACTL_END;

    _ASSERTE(pDesc->SanityCheck());

    {
        // If this is a wrapper stub, then find the real method that it will go to and patch that.
        // This is more than just a convenience - converted wrapper MD to real MD is required for correct behavior.
        // Wrapper MDs look like unjitted MethodDescs. So when the debugger patches one, 
        // it won't actually bind + apply the patch (it'll wait for the jit-complete instead).
        // But if the wrapper MD is for prejitted code, then we'll never get the Jit-complete.
        // Thus it'll miss the patch completely.
        if (pDesc->IsWrapperStub())
        {
            MethodDesc * pNewDesc = NULL;

            FAULT_NOT_FATAL();


#ifndef DACCESS_COMPILE                        
            EX_TRY  
            {    
                pNewDesc = pDesc->GetExistingWrappedMethodDesc();
            }
            EX_CATCH
            {
                // In case of an error, we'll just stick w/ the original method desc.
            } EX_END_CATCH(SwallowAllExceptions)
#else
            // @todo - DAC needs this too, but the method is currently not DACized.
            // However, we don't throw here b/c the error may not be fatal.
            // DacNotImpl();
#endif

            if (pNewDesc != NULL)
            {
                pDesc = pNewDesc;

                LOG((LF_CORDB, LL_INFO10000, "TD::UnjittedMethod: wrapper md: %p --> %p", pDesc, pNewDesc));

            }
        }
    }


    this->type = TRACE_UNJITTED_METHOD;
    this->pDesc = pDesc;
    this->stubManager = NULL;
}


// Initialize statics.
#ifdef _DEBUG
SString * StubManager::s_pDbgStubManagerLog = NULL; 
CrstStatic StubManager::s_DbgLogCrst;

#endif

SPTR_IMPL(StubManager, StubManager, g_pFirstManager);

CrstStatic StubManager::s_StubManagerListCrst;

//-----------------------------------------------------------
// For perf reasons, the stub managers are now kept in a two
// tier system: all stub managers but the VirtualStubManagers
// are in the first tier. A VirtualStubManagerManager takes
// care of all VirtualStubManagers, and is iterated last of
// all. It does a smarter job of looking up the owning
// manager for virtual stubs, checking the current and shared
// appdomains before checking the remaining managers.
//
// Thus, this iterator will run the regular list until it
// hits the end, then it will check the VSMM, then it will
// end.
//-----------------------------------------------------------
class StubManagerIterator
{
  public:
    StubManagerIterator();
    ~StubManagerIterator();

    void Reset();
    BOOL Next();
    PTR_StubManager Current();

  protected:
    enum SMI_State
    {
        SMI_START,
        SMI_NORMAL,
        SMI_VIRTUALCALLSTUBMANAGER,
        SMI_END
    };

    SMI_State               m_state;
    PTR_StubManager m_pCurMgr;
    SimpleReadLockHolder    m_lh;
};

//-----------------------------------------------------------
// Ctor
//-----------------------------------------------------------
StubManagerIterator::StubManagerIterator()
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    Reset();
}

void StubManagerIterator::Reset()
{
    LIMITED_METHOD_DAC_CONTRACT;
    m_pCurMgr = NULL;
    m_state = SMI_START;
}

//-----------------------------------------------------------
// Ctor
//-----------------------------------------------------------
StubManagerIterator::~StubManagerIterator()
{
    LIMITED_METHOD_DAC_CONTRACT;
}

//-----------------------------------------------------------
// Move to the next element. Iterators are created at
// start-1, so must call Next before using Current
//-----------------------------------------------------------
BOOL StubManagerIterator::Next()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
#ifndef DACCESS_COMPILE
        CAN_TAKE_LOCK;         // because of m_lh.Assign()
#else
        CANNOT_TAKE_LOCK;
#endif
    }
    CONTRACTL_END;

    SUPPORTS_DAC;

    do {
        if (m_state == SMI_START) {
            m_state = SMI_NORMAL;
            m_pCurMgr = StubManager::g_pFirstManager;
        }
        else if (m_state == SMI_NORMAL) {
            if (m_pCurMgr != NULL) {
                m_pCurMgr = m_pCurMgr->m_pNextManager;
            }
            else {
                // If we've reached the end of the regular list of stub managers, then we
                // set the VirtualCallStubManagerManager is the current item (effectively
                // forcing it to always be the last manager checked).
                m_state = SMI_VIRTUALCALLSTUBMANAGER;
                VirtualCallStubManagerManager *pVCSMMgr = VirtualCallStubManagerManager::GlobalManager();
                m_pCurMgr = PTR_StubManager(pVCSMMgr);
#ifndef DACCESS_COMPILE
                m_lh.Assign(&pVCSMMgr->m_RWLock);
#endif
            }
        }
        else if (m_state == SMI_VIRTUALCALLSTUBMANAGER) {
            m_state = SMI_END;
            m_pCurMgr = NULL;
#ifndef DACCESS_COMPILE
            m_lh.Clear();
#endif
        }
    } while (m_state != SMI_END && m_pCurMgr == NULL);

    CONSISTENCY_CHECK(m_state == SMI_END || m_pCurMgr != NULL);
    return (m_state != SMI_END);
}

//-----------------------------------------------------------
// Get the current contents of the iterator
//-----------------------------------------------------------
PTR_StubManager StubManagerIterator::Current()
{
    LIMITED_METHOD_DAC_CONTRACT;
    CONSISTENCY_CHECK(m_state != SMI_START);
    CONSISTENCY_CHECK(m_state != SMI_END);
    CONSISTENCY_CHECK(CheckPointer(m_pCurMgr));

    return m_pCurMgr;
}

#ifndef DACCESS_COMPILE
//-----------------------------------------------------------
//-----------------------------------------------------------
StubManager::StubManager()
  : m_pNextManager(NULL)
{
    LIMITED_METHOD_CONTRACT;
}

//-----------------------------------------------------------
//-----------------------------------------------------------
StubManager::~StubManager()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        CAN_TAKE_LOCK;     // StubManager::UnlinkStubManager uses a crst
        PRECONDITION(CheckPointer(this));
    } CONTRACTL_END;

    UnlinkStubManager(this);
}
#endif // #ifndef DACCESS_COMPILE

#ifdef _DEBUG_IMPL
//-----------------------------------------------------------
// Verify that the stub is owned by the given stub manager
// and no other stub manager. If a stub is claimed by multiple managers,
// then the wrong manager may claim ownership and improperly trace the stub.
//-----------------------------------------------------------
BOOL StubManager::IsSingleOwner(PCODE stubAddress, StubManager * pOwner)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_CAN_TAKE_LOCK;         // courtesy StubManagerIterator

    // ensure this stubmanager owns it.
    _ASSERTE(pOwner != NULL);

    // ensure nobody else does.
    bool ownerFound = false;
    int count = 0;
    StubManagerIterator it;
    while (it.Next())
    {
        // Callers would have iterated till pOwner.
        if (!ownerFound && it.Current() != pOwner)
            continue;

        if (it.Current() == pOwner)
            ownerFound = true;
            
        if (it.Current()->CheckIsStub_Worker(stubAddress))
        {
            // If you hit this assert, you can tell what 2 stub managers are conflicting by inspecting their vtable.
            CONSISTENCY_CHECK_MSGF((it.Current() == pOwner), ("Stub at 0x%p is owner by multiple managers (0x%p, 0x%p)",
                (void*) stubAddress, pOwner, it.Current()));            
            count++;
        }
        else
        {
            _ASSERTE(it.Current() != pOwner);
        }
    }

    _ASSERTE(ownerFound);
    
    // We expect pOwner to be the only one to own this stub.
    return (count == 1);
}
#endif



//-----------------------------------------------------------
//-----------------------------------------------------------
BOOL StubManager::CheckIsStub_Worker(PCODE stubStartAddress)
{
    CONTRACTL
    {
        NOTHROW;
        CAN_TAKE_LOCK;     // CheckIsStub_Internal can enter SimpleRWLock
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    SUPPORTS_DAC;

    // @todo - consider having a single check for null right up front.
    // Though this may cover bugs where stub-managers don't handle bad addresses.
    // And someone could just as easily pass (0x01) as NULL.
    if (stubStartAddress == NULL)
    {
        return FALSE;
    }

    struct Param
    {
        BOOL fIsStub;
        StubManager *pThis;
        TADDR stubStartAddress;
    } param;
    param.fIsStub = FALSE;
    param.pThis = this;
    param.stubStartAddress = stubStartAddress;

    // This may be called from DAC, and DAC + non-DAC have very different
    // exception handling.
#ifdef DACCESS_COMPILE
    PAL_TRY(Param *, pParam, &param)
#else    
    Param *pParam = &param;
    EX_TRY
#endif    
    {
		SUPPORTS_DAC;

#ifndef DACCESS_COMPILE    
        // Use CheckIsStub_Internal may AV. That's ok. 
        AVInRuntimeImplOkayHolder AVOkay;
#endif

        // Make a Polymorphic call to derived stub manager.
        // Try to see if this address is for a stub. If the address is
        // completely bogus, then this might fault, so we protect it
        // with SEH.
        pParam->fIsStub = pParam->pThis->CheckIsStub_Internal(pParam->stubStartAddress);
    }
#ifdef DACCESS_COMPILE
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
#else
    EX_CATCH
#endif    
    {
        LOG((LF_CORDB, LL_INFO10000, "D::GASTSI: exception indicated addr is bad.\n"));

        param.fIsStub = FALSE;
    }
#ifdef DACCESS_COMPILE
    PAL_ENDTRY
#else
    EX_END_CATCH(SwallowAllExceptions);
#endif

    return param.fIsStub;
}

//-----------------------------------------------------------
// stubAddress may be an invalid address.
//-----------------------------------------------------------
PTR_StubManager StubManager::FindStubManager(PCODE stubAddress)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CAN_TAKE_LOCK;             // courtesy StubManagerIterator
    }
    CONTRACTL_END;

    SUPPORTS_DAC;
    
    StubManagerIterator it;
    while (it.Next())
    {
        if (it.Current()->CheckIsStub_Worker(stubAddress))
        {
            _ASSERTE_IMPL(IsSingleOwner(stubAddress, it.Current()));
            return it.Current();
        }
    }

    return NULL;
}

//-----------------------------------------------------------
// Given an address, figure out a TraceDestination describing where
// the instructions at that address will eventually transfer execution to.
//-----------------------------------------------------------
BOOL StubManager::TraceStub(PCODE stubStartAddress, TraceDestination *trace)
{
    WRAPPER_NO_CONTRACT;

    StubManagerIterator it;
    while (it.Next())
    {
        StubManager * pCurrent = it.Current();
        if (pCurrent->CheckIsStub_Worker(stubStartAddress))
        {
            LOG((LF_CORDB, LL_INFO10000,
                 "StubManager::TraceStub: addr 0x%p claimed by mgr "
                 "0x%p.\n", stubStartAddress, pCurrent));

            _ASSERTE_IMPL(IsSingleOwner(stubStartAddress, pCurrent));                

            BOOL fValid = pCurrent->DoTraceStub(stubStartAddress, trace);
#ifdef _DEBUG
            if (IsStubLoggingEnabled())
            {
            DbgWriteLog("Doing TraceStub for Address 0x%p, claimed by '%s' (0x%p)\n", stubStartAddress, pCurrent->DbgGetName(), pCurrent);            
            if (fValid)
            {
                SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE;
                FAULT_NOT_FATAL();
                SString buffer;
                DbgWriteLog("  td=%S\n", trace->DbgToString(buffer));
            }
            else
            {
                DbgWriteLog("  stubmanager returned false. Does not expect to call managed code\n");
                
            }
            } // logging
#endif
            return fValid;
        }
    }

    if (ExecutionManager::IsManagedCode(stubStartAddress))
    {
        trace->InitForManaged(stubStartAddress);

#ifdef _DEBUG
        DbgWriteLog("Doing TraceStub for Address 0x%p is jitted code claimed by codemanager\n", stubStartAddress);
#endif        

        LOG((LF_CORDB, LL_INFO10000,
             "StubManager::TraceStub: addr 0x%p is managed code\n",
             stubStartAddress));

        return TRUE;
    }

    LOG((LF_CORDB, LL_INFO10000,
         "StubManager::TraceStub: addr 0x%p unknown. TRACE_OTHER...\n",
         stubStartAddress));

#ifdef _DEBUG
    DbgWriteLog("Doing TraceStub for Address 0x%p is unknown!!!\n", stubStartAddress);
#endif            

    trace->InitForOther(stubStartAddress);
    return FALSE;
}

//-----------------------------------------------------------
//-----------------------------------------------------------
BOOL StubManager::FollowTrace(TraceDestination *trace)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    while (trace->GetTraceType() == TRACE_STUB)
    {
        LOG((LF_CORDB, LL_INFO10000,
             "StubManager::FollowTrace: TRACE_STUB for 0x%p\n",
             trace->GetAddress()));
        
        if (!TraceStub(trace->GetAddress(), trace))
        {
            //
            // No stub manager claimed it - it must be an EE helper or something.
            // 

            trace->InitForOther(trace->GetAddress());
        }
    }

    LOG_TRACE_DESTINATION(trace, NULL, "StubManager::FollowTrace");
    
    return trace->GetTraceType() != TRACE_OTHER;
}

#ifndef DACCESS_COMPILE

//-----------------------------------------------------------
//-----------------------------------------------------------
void StubManager::AddStubManager(StubManager *mgr)
{
    WRAPPER_NO_CONTRACT;
    CONSISTENCY_CHECK(CheckPointer(g_pFirstManager, NULL_OK));
    CONSISTENCY_CHECK(CheckPointer(mgr));

    GCX_COOP_NO_THREAD_BROKEN();

    CrstHolder ch(&s_StubManagerListCrst);

    if (g_pFirstManager == NULL)
    {
        g_pFirstManager = mgr;
    }
    else
    {
        mgr->m_pNextManager = g_pFirstManager;
        g_pFirstManager = mgr;
    }

    LOG((LF_CORDB, LL_EVERYTHING, "StubManager::AddStubManager - 0x%p (vptr %x%p)\n", mgr, (*(PVOID*)mgr)));
}

//-----------------------------------------------------------
// NOTE: The runtime MUST be suspended to use this in a
//       truly safe manner.
//-----------------------------------------------------------
void StubManager::UnlinkStubManager(StubManager *mgr)
{
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_CAN_TAKE_LOCK;
    CONSISTENCY_CHECK(CheckPointer(g_pFirstManager, NULL_OK));
    CONSISTENCY_CHECK(CheckPointer(mgr));

    CrstHolder ch(&s_StubManagerListCrst);

    StubManager **m = &g_pFirstManager;
    while (*m != NULL) 
    {
        if (*m == mgr) 
        {
            *m = (*m)->m_pNextManager;
            return;
        }
        m = &(*m)->m_pNextManager;
    }
}

#endif // #ifndef DACCESS_COMPILE

#ifdef DACCESS_COMPILE

//-----------------------------------------------------------
//-----------------------------------------------------------
void
StubManager::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    // Report the global list head.
    DacEnumMemoryRegion(DacGlobalBase() +
                        g_dacGlobals.StubManager__g_pFirstManager,
                        sizeof(TADDR));

    //
    // Report the list contents.
    //
    
    StubManagerIterator it;
    while (it.Next())
    {
        it.Current()->DoEnumMemoryRegions(flags);
    }
}

//-----------------------------------------------------------
//-----------------------------------------------------------
void
StubManager::DoEnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    DAC_ENUM_VTHIS();
    EMEM_OUT(("MEM: %p StubManager base\n", dac_cast<TADDR>(this)));
}

#endif // #ifdef DACCESS_COMPILE

//-----------------------------------------------------------
// Initialize the global stub manager service.
//-----------------------------------------------------------
void StubManager::InitializeStubManagers()
{
#if !defined(DACCESS_COMPILE)

#if defined(_DEBUG)
    s_DbgLogCrst.Init(CrstDebuggerHeapLock, (CrstFlags)(CRST_UNSAFE_ANYMODE | CRST_DEBUGGER_THREAD | CRST_TAKEN_DURING_SHUTDOWN));    
#endif
    s_StubManagerListCrst.Init(CrstDebuggerHeapLock, (CrstFlags)(CRST_UNSAFE_ANYMODE | CRST_DEBUGGER_THREAD | CRST_TAKEN_DURING_SHUTDOWN));

#endif // !DACCESS_COMPILE
}

//-----------------------------------------------------------
// Terminate the global stub manager service.
//-----------------------------------------------------------
void StubManager::TerminateStubManagers()
{
#if !defined(DACCESS_COMPILE)

#if defined(_DEBUG)
    DbgFinishLog();
    s_DbgLogCrst.Destroy();
#endif

    s_StubManagerListCrst.Destroy();
#endif // !DACCESS_COMPILE
}

#ifdef _DEBUG

//-----------------------------------------------------------
// Should stub-manager logging be enabled?
//-----------------------------------------------------------
bool StubManager::IsStubLoggingEnabled()
{
    // Our current logging impl uses SString, which uses new(), which can't be called
    // on the helper thread. (B/c it may deadlock. See SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE)

    // We avoid this by just not logging when native-debugging.
    if (IsDebuggerPresent())
    {
        return false;
    }

    return true;
}


//-----------------------------------------------------------
// Call to reset the log. This is used at the start of a new step-operation.
// pThread is the managed thread doing the stepping. 
// It should either be the current thread or the helper thread.
//-----------------------------------------------------------
void StubManager::DbgBeginLog(TADDR addrCallInstruction, TADDR addrCallTarget)
{
#ifndef DACCESS_COMPILE
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;


    // We can't call new() if another thread holds the heap lock and is then suspended by
    // an interop-debugging. Since this is debug-only logging code, we'll just skip
    // it under those cases.
    if (!IsStubLoggingEnabled())
    {
        return;
    }
    // Now that we know we're not interop-debugging, we can safely call new.
    SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE;
    FAULT_NOT_FATAL();

    {
        CrstHolder ch(&s_DbgLogCrst);
        EX_TRY
        {
            if (s_pDbgStubManagerLog == NULL)
            {
                s_pDbgStubManagerLog = new SString();
            }
            s_pDbgStubManagerLog->Clear();
        }
        EX_CATCH
        {
            DbgFinishLog();
        }
        EX_END_CATCH(SwallowAllExceptions);                
    }

    DbgWriteLog("Beginning Step-in. IP after Call instruction is at 0x%p, call target is at 0x%p\n", 
        addrCallInstruction, addrCallTarget);
#endif        
}

//-----------------------------------------------------------
// Finish logging for this thread.
// pThread is the managed thread doing the stepping. 
// It should either be the current thread or the helper thread.
//-----------------------------------------------------------
void StubManager::DbgFinishLog()
{
#ifndef DACCESS_COMPILE
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    CrstHolder ch(&s_DbgLogCrst);

    // Since this is just a tool for debugging, we don't care if we call new.
    SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE;    
    FAULT_NOT_FATAL();
    
    delete s_pDbgStubManagerLog;
    s_pDbgStubManagerLog = NULL;

       
#endif    
}


//-----------------------------------------------------------
// Write an arbitrary string to the log.
//-----------------------------------------------------------
void StubManager::DbgWriteLog(const CHAR *format, ...)
{
#ifndef DACCESS_COMPILE                        
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;


    if (!IsStubLoggingEnabled())
    {
        return;
    }

    // Since this is just a tool for debugging, we don't care if we call new.
    SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE;
    FAULT_NOT_FATAL();

    CrstHolder ch(&s_DbgLogCrst);

    if (s_pDbgStubManagerLog == NULL)
    {
        return;
    }

    // Suppress asserts about lossy encoding conversion in SString::Printf
    CHECK chk;
    BOOL fEntered = chk.EnterAssert();

    EX_TRY
    {
        va_list args;
        va_start(args, format);
        s_pDbgStubManagerLog->AppendVPrintf(format, args);
        va_end(args); 
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);  

    if (fEntered) chk.LeaveAssert();
#endif
}



//-----------------------------------------------------------
// Get the log as a string.
//-----------------------------------------------------------
void StubManager::DbgGetLog(SString * pStringOut)
{
#ifndef DACCESS_COMPILE                        
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;

        PRECONDITION(CheckPointer(pStringOut));
    }
    CONTRACTL_END;

    if (!IsStubLoggingEnabled())
    {
        return;
    }

    // Since this is just a tool for debugging, we don't care if we call new.
    SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE;
    FAULT_NOT_FATAL();

    CrstHolder ch(&s_DbgLogCrst);

    if (s_pDbgStubManagerLog == NULL)
    {
        return;
    }
    
    EX_TRY
    {
        pStringOut->Set(*s_pDbgStubManagerLog);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);    
#endif    
}


#endif // _DEBUG

extern "C" void STDCALL ThePreStubPatchLabel(void);

//-----------------------------------------------------------
//-----------------------------------------------------------
BOOL ThePreStubManager::DoTraceStub(PCODE stubStartAddress, TraceDestination *trace)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;

        PRECONDITION(stubStartAddress != NULL);
        PRECONDITION(CheckPointer(trace));
    }
    CONTRACTL_END;
    
    //
    // We cannot tell where the stub will end up
    // until after the prestub worker has been run.
    //

    trace->InitForFramePush(GetEEFuncEntryPoint(ThePreStubPatchLabel));

    return TRUE;
}

//-----------------------------------------------------------
BOOL ThePreStubManager::CheckIsStub_Internal(PCODE stubStartAddress)
{
    LIMITED_METHOD_DAC_CONTRACT;
    return stubStartAddress == GetPreStubEntryPoint();

}


// -------------------------------------------------------
// Stub manager functions & globals
// -------------------------------------------------------

SPTR_IMPL(PrecodeStubManager, PrecodeStubManager, g_pManager);

#ifndef DACCESS_COMPILE

/* static */
void PrecodeStubManager::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END

    g_pManager = new PrecodeStubManager();
    StubManager::AddStubManager(g_pManager);
}

#endif // #ifndef DACCESS_COMPILE

/* static */
BOOL PrecodeStubManager::CheckIsStub_Internal(PCODE stubStartAddress)
{
    CONTRACTL
    {
        THROWS; // address may be bad, so we may AV.
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    // Forwarded to from RangeSectionStubManager
    return FALSE;
}

BOOL PrecodeStubManager::DoTraceStub(PCODE stubStartAddress,
                                     TraceDestination *trace)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END

    LOG((LF_CORDB, LL_EVERYTHING, "PrecodeStubManager::DoTraceStub called\n"));

    MethodDesc* pMD = NULL;

#ifdef HAS_COMPACT_ENTRYPOINTS
    if (MethodDescChunk::IsCompactEntryPointAtAddress(stubStartAddress))
    {
        pMD = MethodDescChunk::GetMethodDescFromCompactEntryPoint(stubStartAddress);
    }
    else
#endif // HAS_COMPACT_ENTRYPOINTS
    {
        Precode* pPrecode = Precode::GetPrecodeFromEntryPoint(stubStartAddress);
        PREFIX_ASSUME(pPrecode != NULL);

        switch (pPrecode->GetType())
        {
        case PRECODE_STUB:
            break;

#ifdef HAS_NDIRECT_IMPORT_PRECODE
        case PRECODE_NDIRECT_IMPORT:
#ifndef DACCESS_COMPILE
            trace->InitForUnmanaged(GetEEFuncEntryPoint(NDirectImportThunk));
#else
            trace->InitForOther(NULL);
#endif
            LOG_TRACE_DESTINATION(trace, stubStartAddress, "PrecodeStubManager::DoTraceStub - NDirect import");
            return TRUE;
#endif // HAS_NDIRECT_IMPORT_PRECODE

#ifdef HAS_FIXUP_PRECODE
        case PRECODE_FIXUP:
            break;
#endif // HAS_FIXUP_PRECODE

#ifdef HAS_THISPTR_RETBUF_PRECODE
        case PRECODE_THISPTR_RETBUF:
            break;
#endif // HAS_THISPTR_RETBUF_PRECODE

        default:
            _ASSERTE_IMPL(!"DoTraceStub: Unexpected precode type");
            break;
        }

        PCODE target = pPrecode->GetTarget();

        // check if the method has been jitted
        if (!pPrecode->IsPointingToPrestub(target))
        {
            trace->InitForStub(target);
            LOG_TRACE_DESTINATION(trace, stubStartAddress, "PrecodeStubManager::DoTraceStub - code");
            return TRUE;
        }

        pMD = pPrecode->GetMethodDesc();
    }

    PREFIX_ASSUME(pMD != NULL);

    // If the method is not IL, then we patch the prestub because no one will ever change the call here at the
    // MethodDesc. If, however, this is an IL method, then we are at risk to have another thread backpatch the call
    // here, so we'd miss if we patched the prestub. Therefore, we go right to the IL method and patch IL offset 0
    // by using TRACE_UNJITTED_METHOD.
    if (!pMD->IsIL())
    {
        trace->InitForStub(GetPreStubEntryPoint());
    }
    else
    {
        trace->InitForUnjittedMethod(pMD);
    }

    LOG_TRACE_DESTINATION(trace, stubStartAddress, "PrecodeStubManager::DoTraceStub - prestub");
    return TRUE;
}

#ifndef DACCESS_COMPILE
BOOL PrecodeStubManager::TraceManager(Thread *thread,
                            TraceDestination *trace,
                            T_CONTEXT *pContext,
                            BYTE **pRetAddr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(thread, NULL_OK));
        PRECONDITION(CheckPointer(trace));
        PRECONDITION(CheckPointer(pContext));
        PRECONDITION(CheckPointer(pRetAddr));
    }
    CONTRACTL_END;

    _ASSERTE(!"Unexpected call to PrecodeStubManager::TraceManager");
    return FALSE;
}
#endif

// -------------------------------------------------------
// StubLinkStubManager
// -------------------------------------------------------

SPTR_IMPL(StubLinkStubManager, StubLinkStubManager, g_pManager);

#ifndef DACCESS_COMPILE

/* static */
void StubLinkStubManager::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END

    g_pManager = new StubLinkStubManager();
    StubManager::AddStubManager(g_pManager);
}

#endif // #ifndef DACCESS_COMPILE

BOOL StubLinkStubManager::CheckIsStub_Internal(PCODE stubStartAddress)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    return GetRangeList()->IsInRange(stubStartAddress);
}


BOOL StubLinkStubManager::DoTraceStub(PCODE stubStartAddress,
                                      TraceDestination *trace)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END

    LOG((LF_CORDB, LL_INFO10000,
         "StubLinkStubManager::DoTraceStub: stubStartAddress=0x%08x\n",
         stubStartAddress));

    Stub *stub = Stub::RecoverStub(stubStartAddress);

    LOG((LF_CORDB, LL_INFO10000,
         "StubLinkStubManager::DoTraceStub: stub=0x%08x\n", stub));

    //
    // If this is an intercept stub, we may be able to step
    // into the intercepted stub.
    //
    // <TODO>!!! Note that this case should not be necessary, it's just
    // here until I get all of the patch offsets & frame patch
    // methods in place.</TODO>
    //
    TADDR pRealAddr = 0;
    if (stub->IsIntercept()) 
    {
        InterceptStub *is = dac_cast<PTR_InterceptStub>(stub);

        if (*is->GetInterceptedStub() == NULL) 
        {
            pRealAddr = *is->GetRealAddr();
            LOG((LF_CORDB, LL_INFO10000, "StubLinkStubManager::DoTraceStub"
                " Intercept stub, no following stub, real addr:0x%x\n",
                pRealAddr));
        }
        else 
        {
            stub = *is->GetInterceptedStub();

            pRealAddr = stub->GetEntryPoint();

            LOG((LF_CORDB, LL_INFO10000,
                 "StubLinkStubManager::DoTraceStub: intercepted "
                 "stub=0x%08x, ep=0x%08x\n",
                 stub, stub->GetEntryPoint()));
        }
        _ASSERTE( pRealAddr );

        // !!! will push a frame???
        return TraceStub(pRealAddr, trace);
    }
    else if (stub->IsMulticastDelegate()) 
    {
        LOG((LF_CORDB, LL_INFO10000,
             "StubLinkStubManager(MCDel)::DoTraceStub: stubStartAddress=0x%08x\n",
             stubStartAddress));

        LOG((LF_CORDB, LL_INFO10000,
             "StubLinkStubManager(MCDel)::DoTraceStub: stub=0x%08x MGR_PUSH to entrypoint:0x%x\n", stub,
             stub->GetEntryPoint()));

        // If it's a MC delegate, then we want to set a BP & do a context-ful
        // manager push, so that we can figure out if this call will be to a
        // single multicast delegate or a multi multicast delegate
        trace->InitForManagerPush(stubStartAddress, this);

        return TRUE;
    }
    else if (stub->GetPatchOffset() == 0) 
    {
        LOG((LF_CORDB, LL_INFO10000, "StubLinkStubManager::DoTraceStub: patch offset is 0!\n"));

        return FALSE;
    }
    else 
    {
        trace->InitForFramePush((PCODE)stub->GetPatchAddress());

        LOG_TRACE_DESTINATION(trace, stubStartAddress, "StubLinkStubManager::DoTraceStub");

        return TRUE;
    }
}

#ifndef DACCESS_COMPILE

BOOL StubLinkStubManager::TraceManager(Thread *thread,
                                       TraceDestination *trace,
                                       T_CONTEXT *pContext,
                                       BYTE **pRetAddr)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(return FALSE;);
    }
    CONTRACTL_END

    // NOTE that we're assuming that this will be called if and ONLY if
    // we're examing a multicast delegate stub.  Otherwise, we'll have to figure out
    // what we're looking iat

    BYTE *pbDel = 0;

    LPVOID pc = (LPVOID)GetIP(pContext);

    *pRetAddr = (BYTE *)StubManagerHelpers::GetReturnAddress(pContext);

    pbDel = (BYTE *)StubManagerHelpers::GetThisPtr(pContext);

    LOG((LF_CORDB,LL_INFO10000, "SLSM:TM at 0x%x, retAddr is 0x%x\n", pc, (*pRetAddr)));

    return DelegateInvokeStubManager::TraceDelegateObject(pbDel, trace);
}

#endif // #ifndef DACCESS_COMPILE

// -------------------------------------------------------
// Stub manager for thunks.
//
// Note, the only reason we have this stub manager is so that we can recgonize UMEntryThunks for IsTransitionStub. If it
// turns out that having a full-blown stub manager for these things causes problems else where, then we can just attach
// a range list to the thunk heap and have IsTransitionStub check that after checking with the main stub manager.
// -------------------------------------------------------

SPTR_IMPL(ThunkHeapStubManager, ThunkHeapStubManager, g_pManager);

#ifndef DACCESS_COMPILE 

/* static */
void ThunkHeapStubManager::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    g_pManager = new ThunkHeapStubManager();
    StubManager::AddStubManager(g_pManager);
}

#endif // !DACCESS_COMPILE

BOOL ThunkHeapStubManager::CheckIsStub_Internal(PCODE stubStartAddress)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    // Its a stub if its in our heaps range.
    return GetRangeList()->IsInRange(stubStartAddress);
}

BOOL ThunkHeapStubManager::DoTraceStub(PCODE stubStartAddress,
                                       TraceDestination *trace)
{
    LIMITED_METHOD_CONTRACT;
    // We never trace through these stubs when stepping through managed code. The only reason we have this stub manager
    // is so that IsTransitionStub can recgonize UMEntryThunks.
    return FALSE;
}

// -------------------------------------------------------
// JumpStub stubs
//
// Stub manager for jump stubs created by ExecutionManager::jumpStub()
// These are currently used only on the 64-bit targets IA64 and AMD64
//
// -------------------------------------------------------

SPTR_IMPL(JumpStubStubManager, JumpStubStubManager, g_pManager);

#ifndef DACCESS_COMPILE
/* static */
void JumpStubStubManager::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END

    g_pManager = new JumpStubStubManager();
    StubManager::AddStubManager(g_pManager);
}
#endif // #ifndef DACCESS_COMPILE

BOOL JumpStubStubManager::CheckIsStub_Internal(PCODE stubStartAddress)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    // Forwarded to from RangeSectionStubManager
    return FALSE;
}

BOOL JumpStubStubManager::DoTraceStub(PCODE stubStartAddress,
                                     TraceDestination *trace)
{
    LIMITED_METHOD_CONTRACT;

    PCODE jumpTarget = decodeBackToBackJump(stubStartAddress);
    trace->InitForStub(jumpTarget);
    
    LOG_TRACE_DESTINATION(trace, stubStartAddress, "JumpStubStubManager::DoTraceStub");

    return TRUE;
}

//
// Stub manager for code sections. It forwards the query to the more appropriate 
// stub manager, or handles the query itself.
//

SPTR_IMPL(RangeSectionStubManager, RangeSectionStubManager, g_pManager);

#ifndef DACCESS_COMPILE
/* static */
void RangeSectionStubManager::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END

    g_pManager = new RangeSectionStubManager();
    StubManager::AddStubManager(g_pManager);
}
#endif // #ifndef DACCESS_COMPILE

BOOL RangeSectionStubManager::CheckIsStub_Internal(PCODE stubStartAddress)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    switch (GetStubKind(stubStartAddress))
    {
    case STUB_CODE_BLOCK_PRECODE:
    case STUB_CODE_BLOCK_JUMPSTUB:
    case STUB_CODE_BLOCK_STUBLINK:
    case STUB_CODE_BLOCK_VIRTUAL_METHOD_THUNK:
    case STUB_CODE_BLOCK_EXTERNAL_METHOD_THUNK:
    case STUB_CODE_BLOCK_METHOD_CALL_THUNK:
        return TRUE;
    default:
        break;
    }

    return FALSE;
}

BOOL RangeSectionStubManager::DoTraceStub(PCODE stubStartAddress, TraceDestination *trace)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END

    switch (GetStubKind(stubStartAddress))
    {
    case STUB_CODE_BLOCK_PRECODE:
        return PrecodeStubManager::g_pManager->DoTraceStub(stubStartAddress, trace);

    case STUB_CODE_BLOCK_JUMPSTUB:
        return JumpStubStubManager::g_pManager->DoTraceStub(stubStartAddress, trace);

    case STUB_CODE_BLOCK_STUBLINK:
        return StubLinkStubManager::g_pManager->DoTraceStub(stubStartAddress, trace);

#ifdef FEATURE_PREJIT
    case STUB_CODE_BLOCK_VIRTUAL_METHOD_THUNK:
        {
            PCODE pTarget = GetMethodThunkTarget(stubStartAddress);
            if (pTarget == ExecutionManager::FindZapModule(stubStartAddress)->
                                        GetNGenLayoutInfo()->m_pVirtualImportFixupJumpStub)
            {
#ifdef DACCESS_COMPILE
                DacNotImpl();
#else
                trace->InitForManagerPush(GetEEFuncEntryPoint(VirtualMethodFixupPatchLabel), this);
#endif
            }
            else
            {
                trace->InitForStub(pTarget);
            }
            return TRUE;
        }

    case STUB_CODE_BLOCK_EXTERNAL_METHOD_THUNK:
        {
            PCODE pTarget = GetMethodThunkTarget(stubStartAddress);
            if (pTarget != ExecutionManager::FindZapModule(stubStartAddress)->
                                        GetNGenLayoutInfo()->m_pExternalMethodFixupJumpStub)
            {
                trace->InitForStub(pTarget);
                return TRUE;
            }
        }

        __fallthrough;
#endif

    case STUB_CODE_BLOCK_METHOD_CALL_THUNK:
#ifdef DACCESS_COMPILE
        DacNotImpl();
#else
        trace->InitForManagerPush(GetEEFuncEntryPoint(ExternalMethodFixupPatchLabel), this);
#endif
        return TRUE;

    default:
        break;
    }

    return FALSE;
}

#ifndef DACCESS_COMPILE
BOOL RangeSectionStubManager::TraceManager(Thread *thread,
                            TraceDestination *trace,
                            CONTEXT *pContext,
                            BYTE **pRetAddr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef FEATURE_PREJIT
    // Both virtual and external import thunks have the same structure. We can use
    // common code to handle them.
    _ASSERTE(GetIP(pContext) == GetEEFuncEntryPoint(VirtualMethodFixupPatchLabel) 
         || GetIP(pContext) == GetEEFuncEntryPoint(ExternalMethodFixupPatchLabel));
#else
    _ASSERTE(GetIP(pContext) == GetEEFuncEntryPoint(ExternalMethodFixupPatchLabel));
#endif

    *pRetAddr = (BYTE *)StubManagerHelpers::GetReturnAddress(pContext);

    PCODE target = StubManagerHelpers::GetTailCallTarget(pContext);
    trace->InitForStub(target);
    return TRUE;
}
#endif

PCODE RangeSectionStubManager::GetMethodThunkTarget(PCODE stubStartAddress)
{
    WRAPPER_NO_CONTRACT;

#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_)
    return rel32Decode(stubStartAddress+1);
#elif defined(_TARGET_ARM_)
    TADDR pInstr = PCODEToPINSTR(stubStartAddress);
    return *dac_cast<PTR_PCODE>(pInstr + 2 * sizeof(DWORD));
#else
    PORTABILITY_ASSERT("RangeSectionStubManager::GetMethodThunkTarget");
    return NULL;
#endif
}

#ifdef DACCESS_COMPILE
LPCWSTR RangeSectionStubManager::GetStubManagerName(PCODE addr)
{
    WRAPPER_NO_CONTRACT;

    switch (GetStubKind(addr))
    {
    case STUB_CODE_BLOCK_PRECODE:
        return W("MethodDescPrestub");

    case STUB_CODE_BLOCK_JUMPSTUB:
        return W("JumpStub");

    case STUB_CODE_BLOCK_STUBLINK:
        return W("StubLinkStub");

    case STUB_CODE_BLOCK_VIRTUAL_METHOD_THUNK:
        return W("VirtualMethodThunk");

    case STUB_CODE_BLOCK_EXTERNAL_METHOD_THUNK:
        return W("ExternalMethodThunk");

    case STUB_CODE_BLOCK_METHOD_CALL_THUNK:
        return W("MethodCallThunk");

    default:
        break;
    }

    return W("UnknownRangeSectionStub");
}
#endif // DACCESS_COMPILE

StubCodeBlockKind
RangeSectionStubManager::GetStubKind(PCODE stubStartAddress)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    RangeSection * pRS = ExecutionManager::FindCodeRange(stubStartAddress, ExecutionManager::ScanReaderLock);
    if (pRS == NULL)
        return STUB_CODE_BLOCK_UNKNOWN;

    return pRS->pjit->GetStubCodeBlockKind(pRS, stubStartAddress);
}

//
// This is the stub manager for IL stubs.
//

#ifndef DACCESS_COMPILE

/* static */
void ILStubManager::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END

    StubManager::AddStubManager(new ILStubManager());
}

#endif // #ifndef DACCESS_COMPILE

BOOL ILStubManager::CheckIsStub_Internal(PCODE stubStartAddress)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    MethodDesc *pMD = ExecutionManager::GetCodeMethodDesc(stubStartAddress);

    return (pMD != NULL) && pMD->IsILStub();
}

BOOL ILStubManager::DoTraceStub(PCODE stubStartAddress, 
                                TraceDestination *trace)
{
    LIMITED_METHOD_CONTRACT;

    LOG((LF_CORDB, LL_EVERYTHING, "ILStubManager::DoTraceStub called\n"));

#ifndef DACCESS_COMPILE

    PCODE traceDestination = NULL;

#ifdef FEATURE_MULTICASTSTUB_AS_IL
    MethodDesc* pStubMD = ExecutionManager::GetCodeMethodDesc(stubStartAddress);
    if (pStubMD != NULL && pStubMD->AsDynamicMethodDesc()->IsMulticastStub())
    {
        traceDestination = GetEEFuncEntryPoint(StubHelpers::MulticastDebuggerTraceHelper);
    }
    else
#endif // FEATURE_MULTICASTSTUB_AS_IL
    {
        // This call is going out to unmanaged code, either through pinvoke or COM interop.
        traceDestination = stubStartAddress;
    }

    trace->InitForManagerPush(traceDestination, this);   
    LOG_TRACE_DESTINATION(trace, traceDestination, "ILStubManager::DoTraceStub");

    return TRUE;

#else // !DACCESS_COMPILE
    trace->InitForOther(NULL);
    return FALSE;

#endif // !DACCESS_COMPILE
}

#ifndef DACCESS_COMPILE
#ifdef FEATURE_COMINTEROP
PCODE ILStubManager::GetCOMTarget(Object *pThis, ComPlusCallInfo *pComPlusCallInfo)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // calculate the target interface pointer
    SafeComHolder<IUnknown> pUnk;
    
    OBJECTREF oref = ObjectToOBJECTREF(pThis);
    GCPROTECT_BEGIN(oref);
    pUnk = ComObject::GetComIPFromRCWThrowing(&oref, pComPlusCallInfo->m_pInterfaceMT);
    GCPROTECT_END();
    
    LPVOID *lpVtbl = *(LPVOID **)(IUnknown *)pUnk;

    PCODE target = (PCODE)lpVtbl[pComPlusCallInfo->m_cachedComSlot];
    return target;
}

// This function should return the same result as StubHelpers::GetWinRTFactoryObject followed by
// ILStubManager::GetCOMTarget. The difference is that it does not allocate managed memory, so it
// does not trigger GC.
// 
// The reason why GC (and potentially a stack walk for other purposes, such as exception handling)
// would be problematic is that we are stopped at the first instruction of an IL stub which is
// not a GC-safe point. Technically, the function still has the GC_TRIGGERS contract but we should
// not see GC in practice here without allocating.
// 
// Note that the GC suspension logic detects the debugger-is-attached-at-GC-unsafe-point case and
// will back off and retry. This means that it suffices to ensure that this thread does not trigger
// GC, allocations on other threads will wait and not cause major trouble.
PCODE ILStubManager::GetWinRTFactoryTarget(ComPlusCallMethodDesc *pCMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    MethodTable *pMT = pCMD->GetMethodTable();
    
    // GetComClassFactory could load types and trigger GC, get class name manually
    InlineSString<DEFAULT_NONSTACK_CLASSNAME_SIZE> ssClassName;
    pMT->_GetFullyQualifiedNameForClass(ssClassName);

    IID iid;
    pCMD->m_pComPlusCallInfo->m_pInterfaceMT->GetGuid(&iid, FALSE, FALSE);

    SafeComHolder<IInspectable> pFactory;
    {            
        GCX_PREEMP();
        if (SUCCEEDED(RoGetActivationFactory(WinRtStringRef(ssClassName.GetUnicode(), ssClassName.GetCount()), iid, &pFactory)))
        {
            LPVOID *lpVtbl = *(LPVOID **)(IUnknown *)pFactory;
            return (PCODE)lpVtbl[pCMD->m_pComPlusCallInfo->m_cachedComSlot];
        }
    }

    return NULL;
}
#endif // FEATURE_COMINTEROP

#ifndef CROSSGEN_COMPILE
BOOL ILStubManager::TraceManager(Thread *thread,
                                 TraceDestination *trace,
                                 T_CONTEXT *pContext,
                                 BYTE **pRetAddr)
{
    // See code:ILStubCache.CreateNewMethodDesc for the code that sets flags on stub MDs

    PCODE stubIP = GetIP(pContext);
    *pRetAddr = (BYTE *)StubManagerHelpers::GetReturnAddress(pContext);

#ifdef FEATURE_MULTICASTSTUB_AS_IL
    if (stubIP == GetEEFuncEntryPoint(StubHelpers::MulticastDebuggerTraceHelper))
    {
        stubIP = (PCODE)*pRetAddr;
        *pRetAddr = (BYTE*)StubManagerHelpers::GetRetAddrFromMulticastILStubFrame(pContext);      
    }
#endif

    DynamicMethodDesc *pStubMD = Entry2MethodDesc(stubIP, NULL)->AsDynamicMethodDesc();

    TADDR arg = StubManagerHelpers::GetHiddenArg(pContext);

    Object * pThis = StubManagerHelpers::GetThisPtr(pContext);

    // See code:ILStubCache.CreateNewMethodDesc for the code that sets flags on stub MDs
    PCODE target;

#ifdef FEATURE_MULTICASTSTUB_AS_IL
    if(pStubMD->IsMulticastStub())
    {
        _ASSERTE(GetIP(pContext) == GetEEFuncEntryPoint(StubHelpers::MulticastDebuggerTraceHelper));

        int delegateCount = (int)StubManagerHelpers::GetSecondArg(pContext);
        
        int totalDelegateCount = (int)*(size_t*)((BYTE*)pThis + DelegateObject::GetOffsetOfInvocationCount());

        if (delegateCount == totalDelegateCount)
        {
            LOG((LF_CORDB, LL_INFO1000, "MF::TF: Executed all stubs, should return\n"));
            // We've executed all the stubs, so we should return
            return FALSE;
        }
        else
        {
            // We're going to execute stub delegateCount next, so go and grab it.
            BYTE *pbDelInvocationList = *(BYTE **)((BYTE*)pThis + DelegateObject::GetOffsetOfInvocationList());

            BYTE* pbDel = *(BYTE**)( ((ArrayBase *)pbDelInvocationList)->GetDataPtr() +
                               ((ArrayBase *)pbDelInvocationList)->GetComponentSize()*delegateCount);

            _ASSERTE(pbDel);
            return DelegateInvokeStubManager::TraceDelegateObject(pbDel, trace);
        }

    }
    else 
#endif // FEATURE_MULTICASTSTUB_AS_IL
    if (pStubMD->IsReverseStub())
    {
        if (pStubMD->IsStatic())
        {
            // This is reverse P/Invoke stub, the argument is UMEntryThunk
            UMEntryThunk *pEntryThunk = (UMEntryThunk *)arg;
            target = pEntryThunk->GetManagedTarget();

            LOG((LF_CORDB, LL_INFO10000, "ILSM::TraceManager: Reverse P/Invoke case 0x%x\n", target));
        }
        else
        {
            // This is COM-to-CLR stub, the argument is the target
            target = (PCODE)arg;
            LOG((LF_CORDB, LL_INFO10000, "ILSM::TraceManager: COM-to-CLR case 0x%x\n", target));
        }
        trace->InitForManaged(target);
    }
    else if (pStubMD->IsDelegateStub())
    {
        // This is forward delegate P/Invoke stub, the argument is undefined
        DelegateObject *pDel = (DelegateObject *)pThis;
        target = pDel->GetMethodPtrAux();

        LOG((LF_CORDB, LL_INFO10000, "ILSM::TraceManager: Forward delegate P/Invoke case 0x%x\n", target));
        trace->InitForUnmanaged(target);
    }
    else if (pStubMD->IsCALLIStub())
    {
        // This is unmanaged CALLI stub, the argument is the target
        target = (PCODE)arg;
        
        // The value is mangled on 64-bit
#ifdef _TARGET_AMD64_
        target = target >> 1; // call target is encoded as (addr << 1) | 1
#endif // _TARGET_AMD64_

        LOG((LF_CORDB, LL_INFO10000, "ILSM::TraceManager: Unmanaged CALLI case 0x%x\n", target));
        trace->InitForUnmanaged(target);
    }
#ifdef FEATURE_COMINTEROP
    else if (pStubMD->IsDelegateCOMStub())
    {
        // This is a delegate, but the target is COM.
        DelegateObject *pDel = (DelegateObject *)pThis;
        DelegateEEClass *pClass = (DelegateEEClass *)pDel->GetMethodTable()->GetClass();

        target = GetCOMTarget(pThis, pClass->m_pComPlusCallInfo);

        LOG((LF_CORDB, LL_INFO10000, "ILSM::TraceManager: CLR-to-COM via delegate case 0x%x\n", target));
        trace->InitForUnmanaged(target);
    }
#endif // FEATURE_COMINTEROP
    else
    {
        // This is either direct forward P/Invoke or a CLR-to-COM call, the argument is MD
        MethodDesc *pMD = (MethodDesc *)arg;

        if (pMD->IsNDirect())
        {
            target = (PCODE)((NDirectMethodDesc *)pMD)->GetNativeNDirectTarget();
            LOG((LF_CORDB, LL_INFO10000, "ILSM::TraceManager: Forward P/Invoke case 0x%x\n", target));
            trace->InitForUnmanaged(target);
        }
#ifdef FEATURE_COMINTEROP
        else
        {
            _ASSERTE(pMD->IsComPlusCall());
            ComPlusCallMethodDesc *pCMD = (ComPlusCallMethodDesc *)pMD;

            if (pCMD->IsStatic() || pCMD->IsCtor())
            {
                // pThis is not the object we'll be calling, we need to get the factory object instead
                MethodTable *pMTOfTypeToCreate = pCMD->GetMethodTable();
                pThis = OBJECTREFToObject(GetAppDomain()->LookupWinRTFactoryObject(pMTOfTypeToCreate, GetCurrentCtxCookie()));

                if (pThis == NULL)
                {
                    // If we don't have an RCW of the factory object yet, don't create it. We would
                    // risk triggering GC which is not safe here because the IL stub is not at a GC
                    // safe point. Instead, query WinRT directly and release the factory immediately.
                    target = GetWinRTFactoryTarget(pCMD);

                    if (target != NULL)
                    {
                        LOG((LF_CORDB, LL_INFO10000, "ILSM::TraceManager: CLR-to-COM WinRT factory RCW-does-not-exist-yet case 0x%x\n", target));
                        trace->InitForUnmanaged(target);
                    }
                }
            }

            if (pThis != NULL)
            {
                target = GetCOMTarget(pThis, pCMD->m_pComPlusCallInfo);

                LOG((LF_CORDB, LL_INFO10000, "ILSM::TraceManager: CLR-to-COM case 0x%x\n", target));
                trace->InitForUnmanaged(target);
            }
        }
#endif // FEATURE_COMINTEROP
    }

    return TRUE;
}
#endif // !CROSSGEN_COMPILE
#endif //!DACCESS_COMPILE

// This is used to recognize GenericComPlusCallStub, VarargPInvokeStub, and GenericPInvokeCalliHelper.

#ifndef DACCESS_COMPILE

/* static */
void InteropDispatchStubManager::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END

    StubManager::AddStubManager(new InteropDispatchStubManager());
}

#endif // #ifndef DACCESS_COMPILE

PCODE TheGenericComplusCallStub(); // clrtocom.cpp

#ifndef DACCESS_COMPILE
static BOOL IsVarargPInvokeStub(PCODE stubStartAddress)
{
    LIMITED_METHOD_CONTRACT;

    if (stubStartAddress == GetEEFuncEntryPoint(VarargPInvokeStub))
        return TRUE;

#if !defined(_TARGET_X86_) && !defined(_TARGET_ARM64_)
    if (stubStartAddress == GetEEFuncEntryPoint(VarargPInvokeStub_RetBuffArg))
        return TRUE;
#endif

    return FALSE;
}
#endif // #ifndef DACCESS_COMPILE

BOOL InteropDispatchStubManager::CheckIsStub_Internal(PCODE stubStartAddress)
{
    WRAPPER_NO_CONTRACT;
    //@dbgtodo dharvey implement DAC suport

#ifndef DACCESS_COMPILE
#ifdef FEATURE_COMINTEROP
    if (stubStartAddress == GetEEFuncEntryPoint(GenericComPlusCallStub))
    {
        return true;
    }
#endif // FEATURE_COMINTEROP    

    if (IsVarargPInvokeStub(stubStartAddress))
    {
        return true;
    }

    if (stubStartAddress == GetEEFuncEntryPoint(GenericPInvokeCalliHelper))
    {
        return true;
    }   

#endif // !DACCESS_COMPILE
    return false;
}

BOOL InteropDispatchStubManager::DoTraceStub(PCODE stubStartAddress, TraceDestination *trace)
{
    LIMITED_METHOD_CONTRACT;

    LOG((LF_CORDB, LL_EVERYTHING, "InteropDispatchStubManager::DoTraceStub called\n"));

#ifndef DACCESS_COMPILE
     _ASSERTE(CheckIsStub_Internal(stubStartAddress));

    trace->InitForManagerPush(stubStartAddress, this);

    LOG_TRACE_DESTINATION(trace, stubStartAddress, "InteropDispatchStubManager::DoTraceStub");

    return TRUE;

#else // !DACCESS_COMPILE
    trace->InitForOther(NULL);
    return FALSE;

#endif // !DACCESS_COMPILE
}

#ifndef DACCESS_COMPILE

BOOL InteropDispatchStubManager::TraceManager(Thread *thread,
                                              TraceDestination *trace,
                                              T_CONTEXT *pContext,
                                              BYTE **pRetAddr)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    *pRetAddr = (BYTE *)StubManagerHelpers::GetReturnAddress(pContext);

    TADDR arg = StubManagerHelpers::GetHiddenArg(pContext);

    // IL stub may not exist at this point so we init directly for the target (TODO?)

    if (IsVarargPInvokeStub(GetIP(pContext)))
    {
        NDirectMethodDesc *pNMD = (NDirectMethodDesc *)arg;
        _ASSERTE(pNMD->IsNDirect());
        PCODE target = (PCODE)pNMD->GetNDirectTarget();

        LOG((LF_CORDB, LL_INFO10000, "IDSM::TraceManager: Vararg P/Invoke case 0x%x\n", target));
        trace->InitForUnmanaged(target);
    }
    else if (GetIP(pContext) == GetEEFuncEntryPoint(GenericPInvokeCalliHelper))
    {
        PCODE target = (PCODE)arg;
        LOG((LF_CORDB, LL_INFO10000, "IDSM::TraceManager: Unmanaged CALLI case 0x%x\n", target));
        trace->InitForUnmanaged(target);
    }
#ifdef FEATURE_COMINTEROP
    else
    {
        ComPlusCallMethodDesc *pCMD = (ComPlusCallMethodDesc *)arg;
        _ASSERTE(pCMD->IsComPlusCall());

        Object * pThis = StubManagerHelpers::GetThisPtr(pContext);

        {
            if (!pCMD->m_pComPlusCallInfo->m_pInterfaceMT->IsComEventItfType() && (pCMD->m_pComPlusCallInfo->m_pILStub != NULL))
            {
                // Early-bound CLR->COM call - continue in the IL stub
                trace->InitForStub(pCMD->m_pComPlusCallInfo->m_pILStub);
            }
            else
            {
                // Late-bound CLR->COM call - continue in target's IDispatch::Invoke
                OBJECTREF oref = ObjectToOBJECTREF(pThis);
                GCPROTECT_BEGIN(oref);

                MethodTable *pItfMT = pCMD->m_pComPlusCallInfo->m_pInterfaceMT;
                _ASSERTE(pItfMT->GetComInterfaceType() == ifDispatch);

                SafeComHolder<IUnknown> pUnk = ComObject::GetComIPFromRCWThrowing(&oref, pItfMT);
                LPVOID *lpVtbl = *(LPVOID **)(IUnknown *)pUnk;

                PCODE target = (PCODE)lpVtbl[6]; // DISPATCH_INVOKE_SLOT;
                LOG((LF_CORDB, LL_INFO10000, "CPSM::TraceManager: CLR-to-COM late-bound case 0x%x\n", target));
                trace->InitForUnmanaged(target);

                GCPROTECT_END();
            }
        }
    }
#endif // FEATURE_COMINTEROP

    return TRUE;
}
#endif //!DACCESS_COMPILE

//
// Since we don't generate delegate invoke stubs at runtime on IA64, we
// can't use the StubLinkStubManager for these stubs.  Instead, we create
// an additional DelegateInvokeStubManager instead.
//
SPTR_IMPL(DelegateInvokeStubManager, DelegateInvokeStubManager, g_pManager);

#ifndef DACCESS_COMPILE

// static
void DelegateInvokeStubManager::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END

    g_pManager = new DelegateInvokeStubManager();
    StubManager::AddStubManager(g_pManager);
}

BOOL DelegateInvokeStubManager::AddStub(Stub* pStub)
{
    WRAPPER_NO_CONTRACT;
    PCODE start = pStub->GetEntryPoint();

    // We don't really care about the size here.  We only stop in these stubs at the first instruction, 
    // so we'll never be asked to claim an address in the middle of a stub.
    return GetRangeList()->AddRange((BYTE *)start, (BYTE *)start + 1, (LPVOID)start);
}

void DelegateInvokeStubManager::RemoveStub(Stub* pStub)
{
    WRAPPER_NO_CONTRACT;
    PCODE start = pStub->GetEntryPoint();

    // We don't really care about the size here.  We only stop in these stubs at the first instruction, 
    // so we'll never be asked to claim an address in the middle of a stub.
    GetRangeList()->RemoveRanges((LPVOID)start);
}

#endif

BOOL DelegateInvokeStubManager::CheckIsStub_Internal(PCODE stubStartAddress)
{
    LIMITED_METHOD_DAC_CONTRACT;

    bool fIsStub = false;

#ifndef DACCESS_COMPILE
#ifndef _TARGET_X86_
    fIsStub = fIsStub || (stubStartAddress == GetEEFuncEntryPoint(SinglecastDelegateInvokeStub));
#endif
#endif // !DACCESS_COMPILE

    fIsStub = fIsStub || GetRangeList()->IsInRange(stubStartAddress);

    return fIsStub;
}

BOOL DelegateInvokeStubManager::DoTraceStub(PCODE stubStartAddress, TraceDestination *trace)
{
    LIMITED_METHOD_CONTRACT;

    LOG((LF_CORDB, LL_EVERYTHING, "DelegateInvokeStubManager::DoTraceStub called\n"));

    _ASSERTE(CheckIsStub_Internal(stubStartAddress));

    // If it's a MC delegate, then we want to set a BP & do a context-ful
    // manager push, so that we can figure out if this call will be to a
    // single multicast delegate or a multi multicast delegate
    trace->InitForManagerPush(stubStartAddress, this);

    LOG_TRACE_DESTINATION(trace, stubStartAddress, "DelegateInvokeStubManager::DoTraceStub");

    return TRUE;
}

#if !defined(DACCESS_COMPILE)

BOOL DelegateInvokeStubManager::TraceManager(Thread *thread, TraceDestination *trace, 
                                             T_CONTEXT *pContext, BYTE **pRetAddr)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    
    PCODE destAddr;

    PCODE pc;
    pc = ::GetIP(pContext);

    BYTE* pThis;
    pThis = NULL;

    // Retrieve the this pointer from the context.
#if defined(_TARGET_X86_)
    (*pRetAddr) = *(BYTE **)(size_t)(pContext->Esp);

    pThis = (BYTE*)(size_t)(pContext->Ecx);

    destAddr = *(PCODE*)(pThis + DelegateObject::GetOffsetOfMethodPtrAux());

#elif defined(_TARGET_AMD64_)

    // <TODO>
    // We need to check whether the following is the correct return address. 
    // </TODO>
    (*pRetAddr) = *(BYTE **)(size_t)(pContext->Rsp);

    LOG((LF_CORDB, LL_INFO10000, "DISM:TM at 0x%p, retAddr is 0x%p\n", pc, (*pRetAddr)));

    DELEGATEREF orDelegate;
    if (GetEEFuncEntryPoint(SinglecastDelegateInvokeStub) == pc)
    {
        LOG((LF_CORDB, LL_INFO10000, "DISM::TraceManager: isSingle\n"));

        orDelegate = (DELEGATEREF)ObjectToOBJECTREF(StubManagerHelpers::GetThisPtr(pContext));

        // _methodPtr is where we are going to next.  However, in ngen cases, we may have a shuffle thunk
        // burned into the ngen image, in which case the shuffle thunk is not added to the range list of
        // the DelegateInvokeStubManager.  So we use _methodPtrAux as a fallback.
        destAddr = orDelegate->GetMethodPtr();
        if (StubManager::TraceStub(destAddr, trace))
        {
            LOG((LF_CORDB,LL_INFO10000, "DISM::TM: ppbDest: 0x%p\n", destAddr));
            LOG((LF_CORDB,LL_INFO10000, "DISM::TM: res: 1, result type: %d\n", trace->GetTraceType()));
            return TRUE;
        }
    }
    else
    {
        // We get here if we are stopped at the beginning of a shuffle thunk.
        // The next address we are going to is _methodPtrAux.
        Stub* pStub = Stub::RecoverStub(pc);

        // We use the patch offset field to indicate whether the stub has a hidden return buffer argument.
        // This field is set in SetupShuffleThunk().
        if (pStub->GetPatchOffset() != 0)
        {
            // This stub has a hidden return buffer argument.
            orDelegate = (DELEGATEREF)ObjectToOBJECTREF(StubManagerHelpers::GetSecondArg(pContext));
        }
        else
        {
            orDelegate = (DELEGATEREF)ObjectToOBJECTREF(StubManagerHelpers::GetThisPtr(pContext));
        }
    }

    destAddr = orDelegate->GetMethodPtrAux();
#elif defined(_TARGET_ARM_)
    (*pRetAddr) = (BYTE *)(size_t)(pContext->Lr);
    pThis = (BYTE*)(size_t)(pContext->R0);

    // Could be in the singlecast invoke stub (in which case the next destination is in _methodPtr) or a
    // shuffle thunk (destination in _methodPtrAux).
    int offsetOfNextDest;
    if (pc == GetEEFuncEntryPoint(SinglecastDelegateInvokeStub))
        offsetOfNextDest = DelegateObject::GetOffsetOfMethodPtr();
    else
        offsetOfNextDest = DelegateObject::GetOffsetOfMethodPtrAux();
    destAddr = *(PCODE*)(pThis + offsetOfNextDest);
#elif defined(_TARGET_ARM64_)
    (*pRetAddr) = (BYTE *)(size_t)(pContext->Lr);
    pThis = (BYTE*)(size_t)(pContext->X0);

    // Could be in the singlecast invoke stub (in which case the next destination is in _methodPtr) or a
    // shuffle thunk (destination in _methodPtrAux).
    int offsetOfNextDest;
    if (pc == GetEEFuncEntryPoint(SinglecastDelegateInvokeStub))
        offsetOfNextDest = DelegateObject::GetOffsetOfMethodPtr();
    else
        offsetOfNextDest = DelegateObject::GetOffsetOfMethodPtrAux();
    destAddr = *(PCODE*)(pThis + offsetOfNextDest);
#else
    PORTABILITY_ASSERT("DelegateInvokeStubManager::TraceManager");
    destAddr = NULL;
#endif

    LOG((LF_CORDB,LL_INFO10000, "DISM::TM: ppbDest: 0x%p\n", destAddr));
    
    BOOL res = StubManager::TraceStub(destAddr, trace);
    LOG((LF_CORDB,LL_INFO10000, "DISM::TM: res: %d, result type: %d\n", res, trace->GetTraceType()));

    return res;
}

// static 
BOOL DelegateInvokeStubManager::TraceDelegateObject(BYTE* pbDel, TraceDestination *trace)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    BYTE **ppbDest = NULL;
    // If we got here, then we're here b/c we're at the start of a delegate stub 
    // need to figure out the kind of delegates we are dealing with

    BYTE *pbDelInvocationList = *(BYTE **)(pbDel + DelegateObject::GetOffsetOfInvocationList());

    LOG((LF_CORDB,LL_INFO10000, "DISM::TMI: invocationList: 0x%x\n", pbDelInvocationList));

    if (pbDelInvocationList == NULL)
    {
        // null invocationList can be one of the following:
        // Instance closed, Instance open non-virt, Instance open virtual, Static closed, Static opened, Unmanaged FtnPtr
        // Instance open virtual is complex and we need to figure out what to do (TODO).
        // For the others the logic is the following:
        // if _methodPtrAux is 0 the target is in _methodPtr, otherwise the taret is _methodPtrAux

        ppbDest = (BYTE **)(pbDel + DelegateObject::GetOffsetOfMethodPtrAux());

        if (*ppbDest == NULL)
        {
            ppbDest = (BYTE **)(pbDel + DelegateObject::GetOffsetOfMethodPtr());

            if (*ppbDest == NULL)
            {
                // it's not looking good, bail out
                LOG((LF_CORDB,LL_INFO10000, "DISM(DelegateStub)::TM: can't trace into it\n"));
                return FALSE;
            }

        }

        LOG((LF_CORDB,LL_INFO10000, "DISM(DelegateStub)::TM: ppbDest: 0x%x *ppbDest:0x%x\n", ppbDest, *ppbDest));

        BOOL res = StubManager::TraceStub((PCODE) (*ppbDest), trace);

        LOG((LF_CORDB,LL_INFO10000, "DISM(MCDel)::TM: res: %d, result type: %d\n", res, trace->GetTraceType()));

        return res;
    }
    
    // invocationList is not null, so it can be one of the following:
    // Multicast, Static closed (special sig), Secure
    
    // rule out the static with special sig
    BYTE *pbCount = *(BYTE **)(pbDel + DelegateObject::GetOffsetOfInvocationCount());

    if (!pbCount)
    {
        // it's a static closed, the target lives in _methodAuxPtr
        ppbDest = (BYTE **)(pbDel + DelegateObject::GetOffsetOfMethodPtrAux());
        
        if (*ppbDest == NULL)
        {
            // it's not looking good, bail out
            LOG((LF_CORDB,LL_INFO10000, "DISM(DelegateStub)::TM: can't trace into it\n"));
            return FALSE;
        }
        
        LOG((LF_CORDB,LL_INFO10000, "DISM(DelegateStub)::TM: ppbDest: 0x%x *ppbDest:0x%x\n", ppbDest, *ppbDest));

        BOOL res = StubManager::TraceStub((PCODE) (*ppbDest), trace);

        LOG((LF_CORDB,LL_INFO10000, "DISM(MCDel)::TM: res: %d, result type: %d\n", res, trace->GetTraceType()));

        return res;
    }

    MethodTable *pType = *(MethodTable**)pbDelInvocationList;
    if (pType->IsDelegate())
    {
        // this is a secure deelgate. The target is hidden inside this field, so recurse in and pray...
        return TraceDelegateObject(pbDelInvocationList, trace);
    }
    
    // Otherwise, we're going for the first invoke of the multi case.
    // In order to go to the correct spot, we have just have to fish out
    // slot 0 of the invocation list, and figure out where that's going to,
    // then put a breakpoint there...
    pbDel = *(BYTE**)(((ArrayBase *)pbDelInvocationList)->GetDataPtr());
    return TraceDelegateObject(pbDel, trace);
}

#endif // DACCESS_COMPILE


#if !defined(DACCESS_COMPILE)

// static
void TailCallStubManager::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END

    StubManager::AddStubManager(new TailCallStubManager());
}

bool TailCallStubManager::IsTailCallStubHelper(PCODE code)
{
    LIMITED_METHOD_CONTRACT;

    return code == GetEEFuncEntryPoint(JIT_TailCall);
}

#endif // !DACCESS_COMPILED

BOOL TailCallStubManager::CheckIsStub_Internal(PCODE stubStartAddress)
{
    LIMITED_METHOD_DAC_CONTRACT;

    bool fIsStub = false;

#if !defined(DACCESS_COMPILE)
    fIsStub = IsTailCallStubHelper(stubStartAddress);
#endif // !DACCESS_COMPILE

    return fIsStub;
}

#if !defined(DACCESS_COMPILE)

#if defined(_TARGET_X86_)
EXTERN_C void STDCALL JIT_TailCallLeave();
EXTERN_C void STDCALL JIT_TailCallVSDLeave();
#endif // _TARGET_X86_

BOOL TailCallStubManager::TraceManager(Thread * pThread, 
                                       TraceDestination * pTrace, 
                                       T_CONTEXT * pContext, 
                                       BYTE ** ppRetAddr)
{
    WRAPPER_NO_CONTRACT;
#if defined(_TARGET_X86_)
    TADDR esp = GetSP(pContext);
    TADDR ebp = GetFP(pContext);

    // Check if we are stopped at the beginning of JIT_TailCall().
    if (GetIP(pContext) == GetEEFuncEntryPoint(JIT_TailCall))
    {
        // There are two cases in JIT_TailCall().  The first one is a normal tail call.
        // The second one is a tail call to a virtual method.
        *ppRetAddr = *(reinterpret_cast<BYTE **>(ebp + sizeof(SIZE_T)));

        // Check whether this is a VSD tail call.
        SIZE_T flags = *(reinterpret_cast<SIZE_T *>(esp + JIT_TailCall_StackOffsetToFlags));
        if (flags & 0x2)
        {
            // This is a VSD tail call.
            pTrace->InitForManagerPush(GetEEFuncEntryPoint(JIT_TailCallVSDLeave), this);
            return TRUE;
        }
        else
        {
            // This is not a VSD tail call.
            pTrace->InitForManagerPush(GetEEFuncEntryPoint(JIT_TailCallLeave), this);
            return TRUE;
        }
    }
    else
    {
        if (GetIP(pContext) == GetEEFuncEntryPoint(JIT_TailCallLeave))
        {
            // This is the simple case.  The tail call goes directly to the target.  There won't be an 
            // explicit frame on the stack.  We should be right at the return instruction which branches to 
            // the call target.  The return address is stored in the second leafmost stack slot.
            *ppRetAddr = *(reinterpret_cast<BYTE **>(esp + sizeof(SIZE_T)));
        }
        else
        {
            _ASSERTE(GetIP(pContext) == GetEEFuncEntryPoint(JIT_TailCallVSDLeave));

            // This is the VSD case.  The tail call goes through a assembly helper function which sets up
            // and tears down an explicit frame.  In this case, the return address is at the same place
            // as on entry to JIT_TailCall().
            *ppRetAddr = *(reinterpret_cast<BYTE **>(ebp + sizeof(SIZE_T)));
        }

        // In both cases, the target address is stored in the leafmost stack slot.
        pTrace->InitForStub((PCODE)*reinterpret_cast<SIZE_T *>(esp));
        return TRUE;
    }

#elif defined(_TARGET_AMD64_) || defined(_TARGET_ARM_)

    _ASSERTE(GetIP(pContext) == GetEEFuncEntryPoint(JIT_TailCall));

    // The target address is the second argument
#ifdef _TARGET_AMD64_
    PCODE target = (PCODE)pContext->Rdx;
#else
    PCODE target = (PCODE)pContext->R1;
#endif
    *ppRetAddr = reinterpret_cast<BYTE *>(target);
    pTrace->InitForStub(target);
    return TRUE;

#else  // !_TARGET_X86_ && !_TARGET_AMD64_ && !_TARGET_ARM_

    _ASSERTE(!"TCSM::TM - TailCallStubManager should not be necessary on this platform");
    return FALSE;

#endif // _TARGET_X86_ || _TARGET_AMD64_
}

#endif // !DACCESS_COMPILE

BOOL TailCallStubManager::DoTraceStub(PCODE stubStartAddress, TraceDestination *trace)
{
    WRAPPER_NO_CONTRACT;

    LOG((LF_CORDB, LL_EVERYTHING, "TailCallStubManager::DoTraceStub called\n"));

    BOOL fResult = FALSE;

    // Make sure we are stopped at the beginning of JIT_TailCall().
    _ASSERTE(CheckIsStub_Internal(stubStartAddress));
    trace->InitForManagerPush(stubStartAddress, this);
    fResult = TRUE;

    LOG_TRACE_DESTINATION(trace, stubStartAddress, "TailCallStubManager::DoTraceStub");
    return fResult;
}


#ifdef DACCESS_COMPILE

void
PrecodeStubManager::DoEnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    WRAPPER_NO_CONTRACT;
    DAC_ENUM_VTHIS();
    EMEM_OUT(("MEM: %p PrecodeStubManager\n", dac_cast<TADDR>(this)));
}

void
StubLinkStubManager::DoEnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    WRAPPER_NO_CONTRACT;
    DAC_ENUM_VTHIS();
    EMEM_OUT(("MEM: %p StubLinkStubManager\n", dac_cast<TADDR>(this)));
    GetRangeList()->EnumMemoryRegions(flags);
}

void
ThunkHeapStubManager::DoEnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    WRAPPER_NO_CONTRACT;
    DAC_ENUM_VTHIS();
    EMEM_OUT(("MEM: %p ThunkHeapStubManager\n", dac_cast<TADDR>(this)));
    GetRangeList()->EnumMemoryRegions(flags);
}

void
JumpStubStubManager::DoEnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    WRAPPER_NO_CONTRACT;
    DAC_ENUM_VTHIS();
    EMEM_OUT(("MEM: %p JumpStubStubManager\n", dac_cast<TADDR>(this)));
}

void
RangeSectionStubManager::DoEnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    WRAPPER_NO_CONTRACT;
    DAC_ENUM_VTHIS();
    EMEM_OUT(("MEM: %p RangeSectionStubManager\n", dac_cast<TADDR>(this)));
}

void
ILStubManager::DoEnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    WRAPPER_NO_CONTRACT;
    DAC_ENUM_VTHIS();
    EMEM_OUT(("MEM: %p ILStubManager\n", dac_cast<TADDR>(this)));
}

void
InteropDispatchStubManager::DoEnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    WRAPPER_NO_CONTRACT;
    DAC_ENUM_VTHIS();
    EMEM_OUT(("MEM: %p InteropDispatchStubManager\n", dac_cast<TADDR>(this)));
}

void
DelegateInvokeStubManager::DoEnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    WRAPPER_NO_CONTRACT;
    DAC_ENUM_VTHIS();
    EMEM_OUT(("MEM: %p DelegateInvokeStubManager\n", dac_cast<TADDR>(this)));
    GetRangeList()->EnumMemoryRegions(flags);
}

void
VirtualCallStubManager::DoEnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    WRAPPER_NO_CONTRACT;
    DAC_ENUM_VTHIS();
    EMEM_OUT(("MEM: %p VirtualCallStubManager\n", dac_cast<TADDR>(this)));
    GetLookupRangeList()->EnumMemoryRegions(flags);
    GetResolveRangeList()->EnumMemoryRegions(flags);
    GetDispatchRangeList()->EnumMemoryRegions(flags);
    GetCacheEntryRangeList()->EnumMemoryRegions(flags);
}

void TailCallStubManager::DoEnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    WRAPPER_NO_CONTRACT;
    DAC_ENUM_VTHIS();
    EMEM_OUT(("MEM: %p TailCallStubManager\n", dac_cast<TADDR>(this)));
}

#endif // #ifdef DACCESS_COMPILE

