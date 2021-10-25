// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// IBClogger.CPP
//

//
// Infrastructure for recording touches of EE data structures
//
//


#include "common.h"
#ifdef IBCLOGGER_ENABLED
#include "method.hpp"
#include "corbbtprof.h"
#include "metadatatracker.h"
#include "field.h"
#include "typekey.h"
#include "ibclogger.h"

//#ifdef _DEBUG
//#define DEBUG_IBCLOGGER
//#endif

#ifdef DEBUG_IBCLOGGER

#define DEBUG_PRINTF1(a)            printf(a)
#define DEBUG_PRINTF2(a,b)          printf(a,b)
#define DEBUG_PRINTF3(a,b,c)        printf(a,b,c)
#define DEBUG_PRINTF4(a,b,c,d)      printf(a,b,c,d)
#define DEBUG_PRINTF5(a,b,c,d,e)    printf(a,b,c,d,e)
#else
#define DEBUG_PRINTF1(a)
#define DEBUG_PRINTF2(a,b)
#define DEBUG_PRINTF3(a,b,c)
#define DEBUG_PRINTF4(a,b,c,d)
#define DEBUG_PRINTF5(a,b,c,d,e)
#endif

DWORD dwIBCLogCount = 0;
CrstStatic IBCLogger::m_sync;

#ifdef _DEBUG
/*static*/ unsigned IbcCallback::s_highestId = 0;
#endif

IBCLoggingDisabler::IBCLoggingDisabler()
{
    m_pInfo = NULL;
    m_fDisabled = false;

    if (g_IBCLogger.InstrEnabled())
    {
        m_pInfo = GetThread()->GetIBCInfo();
        if (m_pInfo != NULL)
        {
            m_fDisabled = m_pInfo->DisableLogging();
        }
    }
}

IBCLoggingDisabler::IBCLoggingDisabler(bool ignore)
{
    m_pInfo = NULL;
    m_fDisabled = false;

    if (ignore == false)
    {
        if (g_IBCLogger.InstrEnabled())
        {
            m_pInfo = GetThread()->GetIBCInfo();
            if (m_pInfo != NULL)
            {
                m_fDisabled = m_pInfo->DisableLogging();
            }
        }
    }
}

IBCLoggingDisabler::IBCLoggingDisabler(ThreadLocalIBCInfo* pInfo)
{
    LIMITED_METHOD_CONTRACT;
    m_pInfo = pInfo;

    if (m_pInfo != NULL)
    {
        m_fDisabled = m_pInfo->DisableLogging();
    }
    else
    {
        m_fDisabled = false;
    }
}

IBCLoggingDisabler::~IBCLoggingDisabler()
{
    LIMITED_METHOD_CONTRACT;
    if (m_fDisabled)
        m_pInfo->EnableLogging();
}

IBCLoggerAwareAllocMemTracker::~IBCLoggerAwareAllocMemTracker()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!m_fReleased)
    {
        GetThread()->FlushIBCInfo();
    }
}

IBCLogger::IBCLogger()
    : dwInstrEnabled(0)
{
    LIMITED_METHOD_CONTRACT;
    m_sync.Init(CrstIbcProfile, CrstFlags(CRST_UNSAFE_ANYMODE | CRST_REENTRANCY | CRST_DEBUGGER_THREAD));
}

IBCLogger::~IBCLogger()
{
    WRAPPER_NO_CONTRACT;

    m_sync.Destroy();
}

void IBCLogger::LogAccessThreadSafeHelperStatic(const void * p, pfnIBCAccessCallback callback)
{
    WRAPPER_NO_CONTRACT;
    /* To make the logging callsite as small as possible keep the part that passes extra */
    /* argument to LogAccessThreadSafeHelper in separate non-inlined function */
    g_IBCLogger.LogAccessThreadSafeHelper(p, callback);
}

void IBCLogger::LogAccessThreadSafeHelper(const void * p, pfnIBCAccessCallback callback)
{
    WRAPPER_NO_CONTRACT;
    CONTRACT_VIOLATION( HostViolation );

    /* For the Global Class we may see p == NULL */
    if (p == NULL)
        return;

    Thread * pThread = GetThreadNULLOk();

    /* This could be called by the concurrent GC thread*/
    /* where GetThread() returns NULL. In such cases,*/
    /* we want to log data accessed by the GC, but we will just ignore it for now.*/
    if (pThread == NULL)
        return;

    ThreadLocalIBCInfo* pInfo = pThread->GetIBCInfo();
    if (pInfo == NULL)
    {
        CONTRACT_VIOLATION( ThrowsViolation | FaultViolation);
        pInfo = new ThreadLocalIBCInfo();
        pThread->SetIBCInfo(pInfo);
    }

    //
    // During certain events we disable IBC logging.
    // This may be to prevent deadlocks or we might
    // not want to have IBC logging during these events.
    //
    if ( !pInfo->IsLoggingDisabled() )
    {
        CONTRACT_VIOLATION( ThrowsViolation | TakesLockViolation | FaultViolation);
        pInfo->CallbackHelper(p, callback);
    }
}

CrstStatic* IBCLogger::GetSync()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return &m_sync;
}

void IBCLogger::DelayedCallbackPtr(pfnIBCAccessCallback callback, const void * pValue1, const void * pValue2 /*=NULL*/)
{
    WRAPPER_NO_CONTRACT;

    ThreadLocalIBCInfo* pInfo = GetThread()->GetIBCInfo();

    // record that we could not currently resolve this callback
    pInfo->SetCallbackFailed();

    // If we are processing the delayed list then we don't want or need to
    // add this pair <callback, pValue> to the delay list.
    if (pInfo->ProcessingDelayedList())
    {
        return;
    }

    // We could throw an out of memory exception
    CONTRACT_VIOLATION( ThrowsViolation );

    // Get our thread local hashtable
    DelayCallbackTable * pTable = pInfo->GetPtrDelayList();

    // Create IbcCallback in our stack frame to use as a key for the Lookup
    IbcCallback key(callback, pValue1, pValue2);

    // Perform lookup of this key in our hashtable
    IbcCallback * pEntry = pTable->Lookup(&key);

    // If we already have this pair <callback, pValue> in our table
    // then just return, because we don't need to add a duplicate
    if (pEntry != NULL)
    {
        // Print out a debug message if we are debugging this
        DEBUG_PRINTF4("Did not add duplicate delayed ptr callback: pfn=0x%08x, pValue1=0x%8p, pValue2=0x%8p\n",
                pEntry->GetPfn(), pEntry->GetValue1(), pEntry->GetValue2());
        return;
    }
    // Now that we know that we will add a new entry into our hashtable
    // We create a new IbcCallback in the heap to use as a persisted key
    pEntry = new IbcCallback(callback, pValue1, pValue2);

    // Mark this key as new valid IbcCallback
    pEntry->SetValid();

    // Add the entry into our hashtable.
    pTable->Add(pEntry);

    // Print out a debug message if we are debugging this
    DEBUG_PRINTF4("Added a new delayed ptr callback: pfn=0x%08x, pValue1=0x%8p, pValue2=0x%8p\n",
            key.GetPfn(), key.GetValue1(), key.GetValue2());
}

// some of IBC probes never complete successfully at all.
// and there is no point for them to stay in the delay list forever,
// because it significantly slows down the IBC instrumentation.
// c_maxRetries: the maximun number of times the unsuccessful IBC probe is tried
// c_minCount: is the minimum number of entries in the delay list that we
//             need before we will call ProcessDelayedCallbacks()
// c_minCountIncr: is the minimum number of entries in the delay list that we
//             need to add before we will call ProcessDelayedCallbacks() again
//
static const int c_maxRetries    =  10;
static const int c_minCount      =   8;
static const int c_minCountIncr  =   8;

ThreadLocalIBCInfo::ThreadLocalIBCInfo()
{
    LIMITED_METHOD_CONTRACT;

    m_fCallbackFailed        = false;
    m_fProcessingDelayedList = false;
    m_fLoggingDisabled       = false;
    m_iMinCountToProcess     = c_minCount;
    m_pDelayList             = NULL;
}

ThreadLocalIBCInfo:: ~ThreadLocalIBCInfo()
{
    WRAPPER_NO_CONTRACT;

    if (m_pDelayList != NULL)
    {
        // We have one last call to the CallbackHelper to
        // flush out any remaining items on our delay list
        //
        // CONTRACT_VIOLATION( ThrowsViolation | TakesLockViolation );
        // CallbackHelper(NULL, NULL);

        DeleteDelayedCallbacks();
    }
}

void ThreadLocalIBCInfo::DeleteDelayedCallbacks()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    for (DelayCallbackTable::Iterator elem = m_pDelayList->Begin(),
                                        end = m_pDelayList->End();
            (elem != end); elem++)
    {
        IbcCallback * pCallback = const_cast<IbcCallback *>(*elem);

        _ASSERTE(pCallback->IsValid());

        // free up each of the IbcCallback pointers that we allocated
        pCallback->Invalidate();
        delete pCallback;
    }

    delete m_pDelayList;
    m_pDelayList = NULL;
}

void ThreadLocalIBCInfo::FlushDelayedCallbacks()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_pDelayList != NULL)
    {
        CONTRACT_VIOLATION( ThrowsViolation );
        CallbackHelper(NULL, NULL);

        DeleteDelayedCallbacks();
    }
}

DelayCallbackTable * ThreadLocalIBCInfo::GetPtrDelayList()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_pDelayList == NULL)
    {
        m_pDelayList = new DelayCallbackTable;
    }

    return m_pDelayList;
}

int ThreadLocalIBCInfo::ProcessDelayedCallbacks()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    int removedCount = 0;  // Our return result

    _ASSERTE(m_pDelayList != NULL);
    _ASSERTE(m_fProcessingDelayedList == false);

    m_fProcessingDelayedList = true;

    // Processing Delayed Callback list
    DEBUG_PRINTF2("Processing Delayed Callback list: GetCount()=%d\n", m_pDelayList->GetCount());

    // try callbacks in the list
    for (DelayCallbackTable::Iterator elem = m_pDelayList->Begin(),
                                       end = m_pDelayList->End();
         (elem != end); elem++)
    {
        IbcCallback * pCallback = const_cast<IbcCallback *>(*elem);

        _ASSERTE(pCallback->IsValid());

        // For each callback that we process we use the
        // field m_fCallbackFailed to record wheather we
        // failed or succeeded in resolving the callback
        //
        m_fCallbackFailed = false;

        pCallback->Invoke();

        if (m_fCallbackFailed == false)
        {
            // Successfully proccessed a delayed callback
            DEBUG_PRINTF5("Successfully processed a delayed callback: pfn=0x%08x, value1=0x%8p, value2=0x%8p, retries=%d\n",
                    pCallback->GetPfn(), pCallback->GetValue1(), pCallback->GetValue2(), pCallback->GetTryCount());

            m_pDelayList->Remove(pCallback);
            pCallback->Invalidate();
            delete pCallback;
            removedCount++;
        }
        else if (pCallback->IncrementTryCount() > c_maxRetries)
        {
            // Failed a delayed callback by hitting c_maxRetries
            DEBUG_PRINTF4("Failed a delayed callback by hitting c_maxRetries: pfn=0x%08x, value1=0x%8p, value2=0x%8p\n",
                    pCallback->GetPfn(), pCallback->GetValue1(), pCallback->GetValue2());

            m_pDelayList->Remove(pCallback);
            pCallback->Invalidate();
            delete pCallback;
            removedCount++;
        }
    }

    // Done Processing Delayed Callback list
    DEBUG_PRINTF3("Done Processing Delayed Callback list: removed %d items, %d remain\n",
            removedCount, m_pDelayList->GetCount());

    _ASSERTE(m_fProcessingDelayedList == true);
    m_fProcessingDelayedList = false;

    return removedCount;
}

void ThreadLocalIBCInfo::CallbackHelper(const void * p, pfnIBCAccessCallback callback)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    // Acquire the Crst lock before creating the IBCLoggingDisabler object.
    // Only one thread at a time can be processing an IBC logging event.
    CrstHolder lock(IBCLogger::GetSync());
    {
        // @ToDo: methods called from here should assert that they have the lock that we just took

        IBCLoggingDisabler disableLogging( this );  // runs IBCLoggingDisabler::DisableLogging

        // Just in case the processing of delayed list was terminated with exception
        m_fProcessingDelayedList = false;

        if (callback != NULL)
        {
            _ASSERTE(p != NULL);

            // For each callback that we process we use the
            // field m_fCallbackFailed to record whether we
            // failed or succeeded in resolving the callback
            //
            m_fCallbackFailed = false;

            callback(&g_IBCLogger, p, NULL);

            if (m_fCallbackFailed == false)
            {
                // If we were able to successfully process this ibc probe then
                // the chances are good that the delayed probes will succeed too.
                // Thus it may be worth proccessing the delayed call back list.
                // We will process this list if it currently has at least
                // MinCountToProcess items in the delay list.
                //
                int delayListAfter  = (m_pDelayList == NULL) ? 0 : m_pDelayList->GetCount();
                if (delayListAfter >= GetMinCountToProcess())
                {
                    int numRemoved = ProcessDelayedCallbacks();
                    if (numRemoved > 0)
                    {
                        // Reset the min count back down to the number that we still have remaining
                        m_iMinCountToProcess = m_pDelayList->GetCount();
                    }

                    // we increase the minCount by the min count increment  so
                    // that we have to add a few new items to the delay list
                    // before we retry ProcessDelayedCallbacks() again.
                    IncMinCountToProcess(c_minCountIncr);
                }
            }
        }
        else // (callback == NULL) -- This is a special case
        {
            _ASSERTE(p == NULL);

            // We just need to call ProcessDelayedCallbacks() unconditionally
            if (m_pDelayList->GetCount() > 0)
            {
                ProcessDelayedCallbacks();
            }
        }

        // runs IBCLoggingDisabler::~IBCLoggingDisabler
        // which runs IBCLoggingDisabler::EnableLogging
    }
}


void IBCLogger::LogMethodAccessHelper(const MethodDesc* pMD, ULONG flagNum)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(g_IBCLogger.InstrEnabled());
    }
    CONTRACTL_END;

    {
        // Don't set the ReadMethodCode flag for EE implemented methods such as Invoke
        if ((flagNum == ReadMethodCode) && pMD->IsEEImpl())
            return;

        // we cannot log before the ObjectClass or StringClass are loaded
        if (g_pObjectClass == NULL || g_pStringClass == NULL)
            goto DelayCallback;

        PTR_MethodTable pMT = pMD->GetMethodTable();
        if (pMT == NULL)
            goto DelayCallback;

        if (!pMT->IsRestored_NoLogging())
            goto DelayCallback;

        Module *pModule = pMT->GetModule();

        if (MethodDescAccessInstrEnabled())
        {
            mdToken token;
            if ( pMD->HasClassOrMethodInstantiation_NoLogging() )
            {
                // We will need to defer the Logging if we cannot compute the PreferredZapModule

                //
                //  If we are creating a generic type or method we can have null TypeHandle args
                //  TFS: 749998
                //  We can also have unrestored MethodTables in our Instantiation args during FixupNativeEntry
                //
                Instantiation classInst  = pMD->GetClassInstantiation();
                Instantiation methodInst = pMD->GetMethodInstantiation();
                for (DWORD i = 0; i < classInst.GetNumArgs(); i++)
                {
                    TypeHandle thArg = classInst[i];
                    if (thArg.IsNull() || thArg.IsEncodedFixup() || !thArg.IsRestored_NoLogging())
                        goto DelayCallback;
                }
                for (DWORD i = 0; i < methodInst.GetNumArgs(); i++)
                {
                    TypeHandle thArg = methodInst[i];
                    if (thArg.IsNull() || thArg.IsEncodedFixup() || !thArg.IsRestored_NoLogging())
                        goto DelayCallback;
                }
            }
            else
            {
                token = pMD->GetMemberDef_NoLogging();
                pModule->LogTokenAccess(token, MethodProfilingData, flagNum);
            }
        }
        return;
    }

DelayCallback:
    DelayedCallbackPtr(LogMethodAccessWrapper, pMD, (void *)(SIZE_T)flagNum);
}

void IBCLogger::LogMethodAccessWrapper(IBCLogger* pLogger, const void * pValue1, const void * pValue2)
{
    WRAPPER_NO_CONTRACT;
    pLogger->LogMethodAccessHelper((MethodDesc *)pValue1, (ULONG)(SIZE_T)pValue2);
}
// Log access to method code or method header
void IBCLogger::LogMethodCodeAccessHelper(MethodDesc *pMD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(g_IBCLogger.InstrEnabled());
    }
    CONTRACTL_END;

    LogMethodAccessHelper(pMD, ReadMethodCode);
}

// Log access to method gc info
void IBCLogger::LogMethodGCInfoAccessHelper(MethodDesc* pMD)
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(InstrEnabled());

    LogMethodAccessHelper(pMD, ReadGCInfo);
    LogMethodAccessHelper(pMD, CommonReadGCInfo);
}

#define LOADORDER_INSTR                 0x00000001
#define RID_ACCESSORDER_INSTR           0x00000002
#define METHODDESC_ACCESS_INSTR         0x00000004
#define ALL_INSTR                       (LOADORDER_INSTR | RID_ACCESSORDER_INSTR | METHODDESC_ACCESS_INSTR)

void IBCLogger::EnableAllInstr()
{
    LIMITED_METHOD_CONTRACT;
#if METADATATRACKER_ENABLED
    MetaDataTracker::Enable();
    MetaDataTracker::s_IBCLogMetaDataAccess = IBCLogger::LogMetaDataAccessStatic;
    MetaDataTracker::s_IBCLogMetaDataSearch = IBCLogger::LogMetaDataSearchAccessStatic;
#endif //METADATATRACKER_ENABLED
    dwInstrEnabled = ALL_INSTR;
}

void IBCLogger::DisableAllInstr()
{
    LIMITED_METHOD_CONTRACT;
    dwInstrEnabled = 0;
}

void IBCLogger::DisableRidAccessOrderInstr()
{
    LIMITED_METHOD_CONTRACT;
    dwInstrEnabled &= (~RID_ACCESSORDER_INSTR);
}

void IBCLogger::DisableMethodDescAccessInstr()
{
    LIMITED_METHOD_CONTRACT;
    dwInstrEnabled &= (~METHODDESC_ACCESS_INSTR);
}

BOOL IBCLogger::MethodDescAccessInstrEnabled()
{
    LIMITED_METHOD_CONTRACT;
    return (dwInstrEnabled & METHODDESC_ACCESS_INSTR);
}

BOOL IBCLogger::RidAccessInstrEnabled()
{
    LIMITED_METHOD_CONTRACT;
    return (dwInstrEnabled & RID_ACCESSORDER_INSTR);
}

#endif // IBCLOGGER_ENABLED
