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

        RelativeFixupPointer<PTR_MethodTable> * ppMT = pMD->GetMethodTablePtr();
        if (ppMT->IsNull())
            goto DelayCallback;

        TADDR pMaybeTaggedMT = ppMT->GetValueMaybeTagged((TADDR)ppMT);
        if (CORCOMPILE_IS_POINTER_TAGGED(pMaybeTaggedMT))
            goto DelayCallback;

        MethodTable *pMT = (MethodTable *)pMaybeTaggedMT;
        if (!pMT->IsRestored_NoLogging())
            goto DelayCallback;

#ifdef FEATURE_PREJIT
        LogMethodTableAccessHelper(pMT);
#endif

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

#ifdef FEATURE_PREJIT
                Module *pPZModule = Module::GetPreferredZapModuleForMethodDesc(pMD);
                token = pPZModule->LogInstantiatedMethod(pMD, flagNum);
                if (!IsNilToken(token))
                {
                    pPZModule->LogTokenAccess(token, MethodProfilingData, flagNum);
                }
#endif
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

#ifdef FEATURE_PREJIT
void IBCLogger::LogMethodDescAccessHelper(const MethodDesc *pMD)
{
    WRAPPER_NO_CONTRACT;

    LogMethodAccessHelper(pMD, ReadMethodDesc);
}

void IBCLogger::LogMethodDescWriteAccessHelper(MethodDesc *pMD)
{
    WRAPPER_NO_CONTRACT;

    LogMethodAccessHelper(pMD, ReadMethodDesc);
    LogMethodAccessHelper(pMD, WriteMethodDesc);
}

void IBCLogger::LogMethodPrecodeAccessHelper(MethodDesc *pMD)
{
    WRAPPER_NO_CONTRACT;

    LogMethodAccessHelper(pMD, ReadMethodPrecode);
}

void IBCLogger::LogMethodPrecodeWriteAccessHelper(MethodDesc *pMD)
{
    WRAPPER_NO_CONTRACT;

    LogMethodAccessHelper(pMD, ReadMethodPrecode);
    LogMethodAccessHelper(pMD, WriteMethodPrecode);
}

// Log access to the method code and method header for NDirect calls
void IBCLogger::LogNDirectCodeAccessHelper(MethodDesc *pMD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(g_IBCLogger.InstrEnabled());
    }
    CONTRACTL_END;

    LogMethodAccessHelper(pMD, ReadMethodDesc);
    LogMethodAccessHelper(pMD, ReadMethodCode);
}

// Log access to method table
void IBCLogger::LogMethodTableAccessHelper(MethodTable const * pMT)
{
    WRAPPER_NO_CONTRACT;

    LogTypeAccessHelper(pMT, ReadMethodTable);
}

// Log access to method table
void IBCLogger::LogTypeMethodTableAccessHelper(const TypeHandle *th)
{
    WRAPPER_NO_CONTRACT;

    LogTypeAccessHelper(*th, ReadMethodTable);
}

// Log write access to method table
void IBCLogger::LogTypeMethodTableWriteableAccessHelper(const TypeHandle *th)
{
    WRAPPER_NO_CONTRACT;

    LogTypeAccessHelper(*th, ReadTypeDesc);
    LogTypeAccessHelper(*th, WriteTypeDesc);
}

// Log access via method table, to a token-based type or an instantiated type.
void IBCLogger::LogTypeAccessHelper(TypeHandle th, ULONG flagNum)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(g_IBCLogger.InstrEnabled());
    }
    CONTRACTL_END;

    CONTRACT_VIOLATION( ThrowsViolation );

    idTypeSpec token = idTypeSpecNil;
    Module*    pPreferredZapModule = NULL;

    if (th.IsNull() || th.IsEncodedFixup())
        return;

    // we cannot do any logging before the ObjectClass and StringClass are loaded
    if (g_pObjectClass == NULL || g_pStringClass == NULL)
        goto DelayCallback;

    if (!th.IsRestored_NoLogging())
        goto DelayCallback;

    //
    // We assign the pPreferredZapModule and the token, then fall out to the LogTokenAccess
    //
    // Logging accesses to TypeDescs is done by blob and we create a special IBC token for the blob
    if (th.IsTypeDesc())
    {
        pPreferredZapModule = Module::GetPreferredZapModuleForTypeHandle(th);

        token = pPreferredZapModule->LogInstantiatedType(th, flagNum);
    }
    else
    {
        MethodTable *pMT = th.AsMethodTable();

        if (pMT->IsArray())
        {
            pPreferredZapModule = Module::GetPreferredZapModuleForMethodTable(pMT);

            token = pPreferredZapModule->LogInstantiatedType(th, flagNum);
        }
        else
        {
            Module* pModule = pMT->GetModule();

            // Instantiations of generic types (like other parameterized types like arrays)
            // need to be handled specially. Generic instantiations do not have a ready-made token
            // in the loader module and need special handling
            //
            if (pMT->HasInstantiation() && // Is this any of List<T>, List<Blah<T>>, or List<String>?
                !pMT->IsGenericTypeDefinition() && // Ignore the type definition (List<T>) as it corresponds to the typeDef token
                !pMT->ContainsGenericVariables()) // We more or less don't save these anyway, apart from the GenericTypeDefinition
            {
                Instantiation inst = pMT->GetInstantiation();

                // This function can get called from BuildMethodTableThrowing(). The instantiation info is not yet set then
                if (!inst.IsEmpty() && !inst[0].IsNull())
                {
                    pPreferredZapModule = Module::GetPreferredZapModuleForMethodTable(pMT);

                    token = pPreferredZapModule->LogInstantiatedType(th, flagNum);
                }
            }
            else
            {
                pPreferredZapModule = pModule;
                token = pMT->GetCl_NoLogging();
            }
        }
    }

    if (!IsNilToken(token))
        pPreferredZapModule->LogTokenAccess(token, TypeProfilingData, flagNum);

    return;

DelayCallback:
    DelayedCallbackPtr(LogTypeAccessWrapper, th.AsPtr(), (void *)(SIZE_T)flagNum);
}

void IBCLogger::LogTypeAccessWrapper(IBCLogger* pLogger, const void * pValue, const void * pValue2)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    pLogger->LogTypeAccessHelper(TypeHandle::FromPtr((void *)pValue), (ULONG)(SIZE_T)pValue2);
}

// Log access to method tables which are private (i.e. methodtables that are updated in the ngen image)
void IBCLogger::LogMethodTableWriteableDataAccessHelper(MethodTable const * pMT)
{
    WRAPPER_NO_CONTRACT;

    LogTypeAccessHelper(pMT, ReadMethodTable);
    LogTypeAccessHelper(pMT, ReadMethodTableWriteableData);
}

// Log access to method tables which are private (i.e. methodtables that are updated in the ngen image)
void IBCLogger::LogMethodTableWriteableDataWriteAccessHelper(MethodTable *pMT)
{
    WRAPPER_NO_CONTRACT;

    LogTypeAccessHelper(pMT, ReadMethodTable);
    LogTypeAccessHelper(pMT, WriteMethodTableWriteableData);
}

void IBCLogger::LogMethodTableNonVirtualSlotsAccessHelper(MethodTable const * pMT)
{
    WRAPPER_NO_CONTRACT;

    LogTypeAccessHelper(pMT, ReadMethodTable);
    LogTypeAccessHelper(pMT, ReadNonVirtualSlots);
}

// Log access to EEClass
void IBCLogger::LogEEClassAndMethodTableAccessHelper(MethodTable * pMT)
{
    WRAPPER_NO_CONTRACT;

    if (pMT == NULL)
        return;

    LogTypeAccessHelper(pMT, ReadMethodTable);

    if (!pMT->IsCanonicalMethodTable()) {
        pMT = pMT->GetCanonicalMethodTable();
        LogTypeAccessHelper(pMT, ReadMethodTable);
    }

    LogTypeAccessHelper(pMT, ReadEEClass);
}

// Log write to EEClass
void IBCLogger::LogEEClassCOWTableAccessHelper(MethodTable * pMT)
{
    WRAPPER_NO_CONTRACT;

    if (pMT == NULL)
        return;

    LogTypeAccessHelper(pMT, ReadMethodTable);

    if (!pMT->IsCanonicalMethodTable()) {
        pMT = pMT->GetCanonicalMethodTable();
        LogTypeAccessHelper(pMT, ReadMethodTable);
    }

    LogTypeAccessHelper(pMT, ReadEEClass);
    LogTypeAccessHelper(pMT, WriteEEClass);
}

// Log access to FieldDescs list in EEClass
void IBCLogger::LogFieldDescsAccessHelper(FieldDesc * pFD)
{
    WRAPPER_NO_CONTRACT;

    MethodTable * pMT = pFD->GetApproxEnclosingMethodTable_NoLogging();

    LogTypeAccessHelper(pMT, ReadMethodTable);

    if (!pMT->IsCanonicalMethodTable()) {
        pMT = pMT->GetCanonicalMethodTable();
        LogTypeAccessHelper(pMT, ReadMethodTable);
    }

    LogTypeAccessHelper(pMT, ReadFieldDescs);
}

void IBCLogger::LogDispatchMapAccessHelper(MethodTable *pMT)
{
    WRAPPER_NO_CONTRACT;

    LogTypeAccessHelper(pMT, ReadMethodTable);
    LogTypeAccessHelper(pMT, ReadDispatchMap);
}

void IBCLogger::LogDispatchTableAccessHelper(MethodTable *pMT)
{
    WRAPPER_NO_CONTRACT;

    LogTypeAccessHelper(pMT, ReadMethodTable);
    LogTypeAccessHelper(pMT, ReadDispatchMap);
    LogTypeAccessHelper(pMT, ReadDispatchTable);
}

void IBCLogger::LogDispatchTableSlotAccessHelper(DispatchSlot *pDS)
{
    WRAPPER_NO_CONTRACT;

    if (pDS->IsNull())
        return;

    MethodDesc *pMD = MethodTable::GetMethodDescForSlotAddress(pDS->GetTarget());
    MethodTable *pMT = pMD->GetMethodTable_NoLogging();
    LogDispatchTableAccessHelper(pMT);
}

// Log access to cctor info table
void IBCLogger::LogCCtorInfoReadAccessHelper(MethodTable *pMT)
{
    WRAPPER_NO_CONTRACT;
    LogTypeAccessHelper(pMT, ReadCCtorInfo);
}


void IBCLogger::LogTypeHashTableAccessHelper(const TypeHandle *th)
{
    WRAPPER_NO_CONTRACT;

    LogTypeAccessHelper(*th, ReadTypeHashTable);
}

// Log access to class hash table
void IBCLogger::LogClassHashTableAccessHelper(EEClassHashEntry *pEntry)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(g_IBCLogger.InstrEnabled());
    }
    CONTRACTL_END;

    // ExecutionManager::FindZapModule may enter the host (if we were hosted), but it's
    // ok since we're just logging IBC data.
    CONTRACT_VIOLATION( HostViolation );

    Module *pModule = ExecutionManager::FindZapModule(dac_cast<TADDR>(pEntry));
    if (pModule == NULL)
    {
        // if FindZapModule returns NULL, it always will return NULL
        // so there is no point in adding a DelayedCallback here.
        return;
    }

    // we cannot log before the ObjectClass or StringClass are loaded
    if (g_pObjectClass == NULL || g_pStringClass == NULL)
        goto DelayCallback;

    HashDatum datum;
    datum = pEntry->GetData();
    mdToken token;
    if ((((ULONG_PTR) datum) & EECLASSHASH_TYPEHANDLE_DISCR) == 0)
    {
        TypeHandle t = TypeHandle::FromPtr(datum);
        _ASSERTE(!t.IsNull());
        MethodTable *pMT = t.GetMethodTable();
        if (pMT == NULL)
            goto DelayCallback;

        token = pMT->GetCl_NoLogging();
    }
    else if (((ULONG_PTR)datum & EECLASSHASH_MDEXPORT_DISCR) == 0)
    {
        DWORD dwDatum = (DWORD)(DWORD_PTR)(datum); // <TODO> WIN64 - Pointer Truncation</TODO>
        token = ((dwDatum >> 1) & 0x00ffffff) | mdtTypeDef;
    }
    else
        return;

    pModule->LogTokenAccess(token, TypeProfilingData, ReadClassHashTable);
    return;

DelayCallback:
    DelayedCallbackPtr(LogClassHashTableAccessWrapper, pEntry);
}

// Log access to meta data
void IBCLogger::LogMetaDataAccessHelper(const void * addr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(g_IBCLogger.InstrEnabled());
    }
    CONTRACTL_END;

    // ExecutionManager::FindZapModule may enter the host (if we were hosted), but it's
    // ok since we're just logging IBC data.
    CONTRACT_VIOLATION( HostViolation );

#if METADATATRACKER_ENABLED
    if (Module *pModule = ExecutionManager::FindZapModule(dac_cast<TADDR>(addr)))
    {
        mdToken token = MetaDataTracker::MapAddrToToken(addr);

        pModule->LogTokenAccess(token, ProfilingFlags_MetaData);
        pModule->LogTokenAccess(token, CommonMetaData);
        return;
    }
#endif //METADATATRACKER_ENABLED

    // if FindZapModule returns NULL, it always will return NULL
    // so there is no point in adding a DelayedCallback here.
}

// Log a search to meta data
// See the comment above CMiniMdRW::GetHotMetadataTokensSearchAware
void IBCLogger::LogMetaDataSearchAccessHelper(const void * result)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(g_IBCLogger.InstrEnabled());
    }
    CONTRACTL_END;

    // ExecutionManager::FindZapModule may enter the host (if we were hosted), but it's
    // ok since we're just logging IBC data.
    CONTRACT_VIOLATION( HostViolation );

#if METADATATRACKER_ENABLED
    if (Module *pModule = ExecutionManager::FindZapModule(dac_cast<TADDR>(result)))
    {
        mdToken token = MetaDataTracker::MapAddrToToken(result);

        pModule->LogTokenAccess(token, ProfilingFlags_MetaData);
        pModule->LogTokenAccess(token, CommonMetaData);
        pModule->LogTokenAccess(token, ProfilingFlags_MetaDataSearch);
        return;
    }
#endif //METADATATRACKER_ENABLED

    // if FindZapModule returns NULL, it always will return NULL
    // so there is no point in adding a DelayedCallback here.
}

// Log access to method list associated with a CER
void IBCLogger::LogCerMethodListReadAccessHelper(MethodDesc *pMD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(g_IBCLogger.InstrEnabled());
    }
    CONTRACTL_END;

    LogMethodAccessHelper(pMD, ReadCerMethodList);
}

void IBCLogger::LogRidMapAccessHelper( RidMapLogData data )
{
    WRAPPER_NO_CONTRACT;

    data.First()->LogTokenAccess( data.Second(), RidMap );
}

// Log access to RVA data
void IBCLogger::LogRVADataAccessHelper(FieldDesc *pFD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(g_IBCLogger.InstrEnabled());
    }
    CONTRACTL_END;

    // we cannot log before the ObjectClass or StringClass are loaded
    if (g_pObjectClass == NULL || g_pStringClass == NULL)
        goto DelayCallback;

    if (CORCOMPILE_IS_POINTER_TAGGED(SIZE_T(pFD)))
        return;

    MethodTable * pMT;
    pMT = pFD->GetApproxEnclosingMethodTable();

    if (!pMT->IsRestored_NoLogging())
        goto DelayCallback;

    if (pMT->HasInstantiation())
        return;

    pMT->GetModule()->LogTokenAccess(pFD->GetMemberDef(), TypeProfilingData, RVAFieldData);
    return;

DelayCallback:
    DelayedCallbackPtr(LogRVADataAccessWrapper, pFD);
}

#endif // FEATURE_PREJIT

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
