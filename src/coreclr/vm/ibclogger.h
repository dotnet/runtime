// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// IBClogger.H
//

//
// Infrastructure for recording touches of EE data structures
//
//


#ifndef IBCLOGGER_H
#define IBCLOGGER_H

#include <holder.h>
#include <sarray.h>
#include <crst.h>
#include <synch.h>
#include <shash.h>

// The IBCLogger class records touches of EE data structures.  It is important to
// minimize the overhead of IBC recording on non-recording scenarios.  Our goal is
// for all public methods to be inlined, and that the cost of doing the instrumentation
// check does not exceed one comparison and one branch.
//

class MethodDesc;
class MethodTable;
class EEClass;
class TypeHandle;
struct DispatchSlot;
class Module;
struct EEClassHashEntry;
class IBCLogger;

extern IBCLogger g_IBCLogger;

typedef PTR_VOID HashDatum;

typedef Pair< Module*, mdToken > RidMapLogData;

#if !defined(DACCESS_COMPILE)
#define IBCLOGGER_ENABLED
#endif

#ifdef IBCLOGGER_ENABLED
//
//  Base class for IBC probe callback
//
typedef void (* const pfnIBCAccessCallback)(IBCLogger* pLogger, const void * pValue, const void * pValue2);

class IbcCallback
{
public:
    IbcCallback(pfnIBCAccessCallback pCallback, const void * pValue1, const void * pValue2)
        : m_pCallback(pCallback),
        m_pValue1(pValue1),
        m_pValue2(pValue2),
        m_tryCount(0)
#ifdef _DEBUG
        , m_id(0)
#endif
    { LIMITED_METHOD_CONTRACT; }

    void Invoke() const
    {
        WRAPPER_NO_CONTRACT;

        m_pCallback(&g_IBCLogger, m_pValue1, m_pValue2);
    }

    SIZE_T GetPfn() const
    {
        LIMITED_METHOD_CONTRACT;

        return (SIZE_T) m_pCallback;
    }

    pfnIBCAccessCallback  GetCallback() const
    {
        LIMITED_METHOD_CONTRACT;

        return m_pCallback;
    }

    const void * GetValue1() const
    {
        LIMITED_METHOD_CONTRACT;

        return m_pValue1;
    }

    const void * GetValue2() const
    {
        LIMITED_METHOD_CONTRACT;

        return m_pValue2;
    }

    void SetValid()
    {
        LIMITED_METHOD_CONTRACT;
#ifdef _DEBUG
        m_id = ++s_highestId;
#endif
    }

    void Invalidate()
    {
        LIMITED_METHOD_CONTRACT;
#ifdef _DEBUG
        m_id = 0;
#endif
    }

    bool IsValid() const
    {
        WRAPPER_NO_CONTRACT;

#ifdef _DEBUG
        return (m_id > 0) && (m_id <= s_highestId);
#else
        return true;
#endif
    }

    int IncrementTryCount()
    {
        return ++m_tryCount;
    }

    int GetTryCount() const
    {
        return m_tryCount;
    }

private:
    pfnIBCAccessCallback    m_pCallback;
    const void *            m_pValue1;
    const void *            m_pValue2;

    int                     m_tryCount;

#ifdef _DEBUG
    unsigned                m_id;
    static unsigned         s_highestId;
#endif
};

class DelayCallbackTableTraits : public DefaultSHashTraits< IbcCallback * >
{
public:
    typedef IbcCallback * key_t;

    static key_t GetKey(element_t e)
    {
        LIMITED_METHOD_CONTRACT;
        return e;
    }

    static BOOL Equals(key_t k1, key_t k2)
    {
        LIMITED_METHOD_CONTRACT;

        return (k1->GetCallback() == k2->GetCallback()) &&
               (k1->GetValue1() == k2->GetValue1()) &&
               (k1->GetValue2() == k2->GetValue2());
    }

    static count_t Hash(key_t k)
    {
        LIMITED_METHOD_CONTRACT;

        SIZE_T hashLarge = (SIZE_T)k->GetCallback() ^
               (SIZE_T)k->GetValue1() ^
               (SIZE_T)k->GetValue2();

#if POINTER_BITS == 32
        // sizeof(SIZE_T) == sizeof(COUNT_T)
        return hashLarge;
#else
        // xor in the upper half as well.
        count_t hash = *(count_t *)(&hashLarge);
        for (unsigned int i = 1; i < POINTER_BITS / 32; i++)
        {
            hash ^= ((count_t *)&hashLarge)[i];
        }

        return hash;
#endif // POINTER_BITS
    }

    static element_t Null()
{
        WRAPPER_NO_CONTRACT;
        return NULL;
    }

    static bool IsNull(element_t e)
    {
        LIMITED_METHOD_CONTRACT;
        return e == NULL;
    }

    static element_t Deleted()
    {
        WRAPPER_NO_CONTRACT;
        return (element_t)-1;
    }

    static bool IsDeleted(const element_t e)
    {
        LIMITED_METHOD_CONTRACT;
        return e == (element_t)-1;
    }
};

typedef  SHash< DelayCallbackTableTraits >  DelayCallbackTable;

class ThreadLocalIBCInfo
{
public:
    ThreadLocalIBCInfo();
    ~ThreadLocalIBCInfo();

    // BOOL IsLoggingDisable()
    // This indicates that logging is currently disabled for this thread
    // This is used to prevent the logging functionality from
    // triggerring more logging (and thus causing a deadlock)
    // It is also used to prevent IBC logging whenever a IBCLoggingDisabler
    // object is used. For example we use this to disable IBC profiling
    // whenever a thread starts a JIT compile event. That is because we
    // don't want to "pollute" the IBC data gathering for the things
    // that the JIT compiler touches.
    // Finally since our IBC logging will need to allocate unmanaged memory
    // we also disable IBC logging when we are inside a "can't alloc region"
    // Typically this occurs when a thread is performing a GC.
    BOOL IsLoggingDisabled()
    {
        LIMITED_METHOD_CONTRACT;
        return m_fLoggingDisabled || IsInCantAllocRegion();
    }

    // We want to disable IBC logging, any further log calls are to be ignored until
    // we call EnableLogging()
    //
    // This method returns true if it changed the value of m_fLoggingDisabled from false to true
    // it returns false if the value of m_fLoggingDisabled was already set to true
    // after this method executes the value of m_fLoggingDisabled will be true
    bool DisableLogging()
    {
        LIMITED_METHOD_CONTRACT;

        bool result = (m_fLoggingDisabled == false);
        m_fLoggingDisabled = true;

        return result;
    }

    // We want to re-enable IBC logging
    void EnableLogging()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(m_fLoggingDisabled == true);

        m_fLoggingDisabled = false;
    }

    bool ProcessingDelayedList()
    {
        LIMITED_METHOD_CONTRACT;
        return m_fProcessingDelayedList;
    }

    void SetCallbackFailed()
    {
        LIMITED_METHOD_CONTRACT;
        m_fCallbackFailed = true;
    }

    int GetMinCountToProcess()
    {
        LIMITED_METHOD_CONTRACT;
        return m_iMinCountToProcess;
    }

    void IncMinCountToProcess(int increment)
    {
        LIMITED_METHOD_CONTRACT;
        m_iMinCountToProcess += increment;
    }

    DelayCallbackTable * GetPtrDelayList();

    void DeleteDelayedCallbacks();

    void FlushDelayedCallbacks();

    int  ProcessDelayedCallbacks();

    void CallbackHelper(const void * p, pfnIBCAccessCallback callback);

private:
    bool        m_fProcessingDelayedList;
    bool        m_fCallbackFailed;
    bool        m_fLoggingDisabled;

    int         m_iMinCountToProcess;

    DelayCallbackTable * m_pDelayList;
};

class IBCLoggingDisabler
{
public:
    IBCLoggingDisabler();
    IBCLoggingDisabler(bool ignore);      // When ignore is true we treat this as a nop
    IBCLoggingDisabler(ThreadLocalIBCInfo* pInfo);
    ~IBCLoggingDisabler();

private:
    ThreadLocalIBCInfo* m_pInfo;
    bool                m_fDisabled;  // true if this holder actually disable the logging
                                      // false when this is a nested occurrence and logging was already disabled
};

//
// IBCLoggerAwareAllocMemTracker should be used for allocation of IBC tracked structures during type loading.
//
// If type loading fails, the delayed IBC callbacks may contain pointers to the failed type or method.
// IBCLoggerAwareAllocMemTracker will ensure that the delayed IBC callbacks are flushed before the memory of
// the failed type or method is reclaimed. Otherwise, there would be stale pointers in the delayed IBC callbacks
// that would cause crashed during IBC logging.
//
class IBCLoggerAwareAllocMemTracker : public AllocMemTracker
{
public:
    IBCLoggerAwareAllocMemTracker()
    {
        WRAPPER_NO_CONTRACT;
    }

    ~IBCLoggerAwareAllocMemTracker();
};

#else // IBCLOGGER_ENABLED

typedef const void * pfnIBCAccessCallback;

class ThreadLocalIBCInfo;
class IBCLoggingDisabler
{
public:
    IBCLoggingDisabler()
    {
    }

    IBCLoggingDisabler(ThreadLocalIBCInfo*)
    {
    }

    ~IBCLoggingDisabler()
    {
    }
};

class ThreadLocalIBCInfo
{
public:
    ThreadLocalIBCInfo()
    {
    }

    ~ThreadLocalIBCInfo()
    {
    }

    void FlushDelayedCallbacks()
    {
    }
};

class IBCLoggerAwareAllocMemTracker : public AllocMemTracker
{
public:
    IBCLoggerAwareAllocMemTracker()
    {
    }

    ~IBCLoggerAwareAllocMemTracker()
    {
    }
};

#endif // IBCLOGGER_ENABLED


// IBCLogger is responsible for collecting profile data.  Logging is turned on by the
// COMPlus_ZapBBInstr environment variable, and the actual writing to the file
// occurs in code:Module.WriteMethodProfileDataLogFile
class IBCLogger
{
    //
    // Methods for logging EE data structure accesses.  All methods should be defined
    // using the LOGACCESS macros, which creates the wrapper method that calls the
    // helper when instrumentation is enabled.  The public name of these methods should
    // be of the form Log##name##Access where name describes the type of access to be
    // logged.  The private helpers are implemented in IBClogger.cpp.
    //

#ifdef IBCLOGGER_ENABLED

#define LOGACCESS_PTR(name, type)                       \
    LOGACCESS(name, type*, (type*), (const void *));

#define LOGACCESS_VALUE(name, type)                     \
    LOGACCESS(name, type, *(type*), (const void *)&);

#define LOGACCESS(name, type, totype, toptr)            \
public:                                                 \
    __forceinline void Log##name##Access(type p)        \
    {                                                   \
        WRAPPER_NO_CONTRACT;                               \
        /* We expect this to get inlined, so that it */ \
        /* has low overhead when not instrumenting. */  \
        /* So keep the function really small */         \
        if ( InstrEnabled() )                           \
            Log##name##AccessStatic(toptr p);           \
    }                                                   \
                                                        \
private:                                                \
    NOINLINE static void Log##name##AccessStatic(const void * p) \
    {                                                   \
        WRAPPER_NO_CONTRACT;                               \
        /* To make the logging callsite as small as */  \
        /* possible keep the part that passes extra */  \
        /* argument to LogAccessThreadSafeHelper */     \
        /* in separate non-inlined static functions */  \
        LogAccessThreadSafeHelperStatic(p, Log##name##AccessWrapper); \
    }                                                   \
                                                        \
    static void Log##name##AccessWrapper(IBCLogger* pLogger, const void * pValue1, const void * pValue2) \
    {                                                   \
        WRAPPER_NO_CONTRACT;                               \
        return pLogger->Log##name##AccessHelper(totype pValue1); \
    }                                                   \
    void Log##name##AccessHelper(type p);               \

private:
    static void LogAccessThreadSafeHelperStatic( const void * p, pfnIBCAccessCallback callback);
    void LogAccessThreadSafeHelper( const void * p, pfnIBCAccessCallback callback);

    void DelayedCallbackPtr(pfnIBCAccessCallback callback, const void * pValue1, const void * pValue2 = NULL);

#else // IBCLOGGER_ENABLED

#define LOGACCESS_PTR(name,type)                        \
public:                                                 \
    void Log##name##Access(type* p) { SUPPORTS_DAC; }   \

#define LOGACCESS_VALUE(name, type)                     \
public:                                                 \
    void Log##name##Access(type p) { SUPPORTS_DAC; }    \

#endif // IBCLOGGER_ENABLED

    // Log access to method code or method header
    // Implemented by : code:IBCLogger.LogMethodCodeAccessHelper
    LOGACCESS_PTR(MethodCode, MethodDesc)

    // Log access to gc info
    // Implemented by : code:IBCLogger.LogMethodGCInfoAccessHelper
    LOGACCESS_PTR(MethodGCInfo, MethodDesc)

#undef LOGACCESS_PTR
#undef LOGACCESS_VALUE

#define LOGACCESS_PTR(name,type)                        \
public:                                                 \
    void Log##name##Access(type* p) { SUPPORTS_DAC; }   \

#define LOGACCESS_VALUE(name, type)                     \
public:                                                 \
    void Log##name##Access(type p) { SUPPORTS_DAC; }    \

    // Log access to method desc (which adds the method desc to the required list)
    // Implemented by : code:IBCLogger.LogMethodDescAccessHelper
    LOGACCESS_PTR(MethodDesc, const MethodDesc)

    // Log access to the NDirect data stored for a MethodDesc
    // also implies that the IL_STUB for the NDirect method is executed
    // Implemented by : code:IBCLogger.LogNDirectCodeAccessHelper
    LOGACCESS_PTR(NDirectCode,MethodDesc)

    // Log access to method desc (which addes the method desc to the required list)
    // Implemented by : code:IBCLogger.LogMethodDescWriteAccessHelper
    LOGACCESS_PTR(MethodDescWrite,MethodDesc)

    // Log access to method desc (which adds the method desc to the required list)
    // Implemented by : code:IBCLogger.LogMethodPrecodeAccessHelper
    LOGACCESS_PTR(MethodPrecode, MethodDesc)

    // Log access to method desc (which addes the method desc to the required list)
    // Implemented by : code:IBCLogger.LogMethodPrecodeWriteAccessHelper
    LOGACCESS_PTR(MethodPrecodeWrite,MethodDesc)

    // Log access to method table
    // Implemented by : code:IBCLogger.LogMethodTableAccessHelper
    LOGACCESS_PTR(MethodTable, MethodTable const)

    // Log access to method table
    // Implemented by : code:IBCLogger.LogTypeMethodTableAccessHelper
    LOGACCESS_PTR(TypeMethodTable, TypeHandle const)

    // Log write access to method table
    // Implemented by : code:IBCLogger.LogTypeMethodTableWriteableAccessHelper
    LOGACCESS_PTR(TypeMethodTableWriteable, TypeHandle const)

    // Log read access to private (written to) method table area
    // Macro expands to : code:LogMethodTableWriteableDataAccessHelper
    LOGACCESS_PTR(MethodTableWriteableData, MethodTable const)

    // Log write access to private (written to) method table area
    // Implemented by : code:IBCLogger.LogMethodTableWriteableDataWriteAccessHelper
    LOGACCESS_PTR(MethodTableWriteableDataWrite,MethodTable)

    // Log access to method table's NonVirtualSlotsArray
    // Implemented by : code:IBCLogger.LogMethodTableNonVirtualSlotsAccessHelper
    LOGACCESS_PTR(MethodTableNonVirtualSlots, MethodTable const)

    // Log access to EEClass
    // Implemented by : code:IBCLogger.LogEEClassAndMethodTableAccessHelper
    LOGACCESS_PTR(EEClassAndMethodTable, MethodTable)

    // Log access to EEClass COW table
    // Implemented by : code:IBCLogger.LogEEClassCOWTableAccessHelper
    LOGACCESS_PTR(EEClassCOWTable, MethodTable)

    // Log access to the FieldDescs list in the EEClass
    // Implemented by : code:IBCLogger.LogFieldDescsAccessHelper
    LOGACCESS_PTR(FieldDescs, FieldDesc)

    // Log access to the MTs dispatch map
    // Implemented by : code:IBCLogger.LogDispatchMapAccessHelper
    LOGACCESS_PTR(DispatchMap,MethodTable)

    // Log read access to the MTs dispatch implementation table
    // Implemented by : code:IBCLogger.LogDispatchTableAccessHelper
    LOGACCESS_PTR(DispatchTable,MethodTable)

    // Log read access to the MTs dispatch implementation table
    // Implemented by : code:IBCLogger.LogDispatchTableAccessHelper
    LOGACCESS_PTR(DispatchTableSlot,DispatchSlot)

    // Log a lookup  in the cctor info table
    // Implemented by : code:IBCLogger.LogCCtorInfoReadAccessHelper
    LOGACCESS_PTR(CCtorInfoRead,MethodTable)

    // Log a lookup  in the class hash table
    // Implemented by : code:IBCLogger.LogClassHashTableAccessHelper
    LOGACCESS_PTR(ClassHashTable,EEClassHashEntry)

    // Log a lookup  of the method list for a CER
    // Implemented by : code:IBCLogger.LogCerMethodListReadAccessHelper
    LOGACCESS_PTR(CerMethodListRead,MethodDesc)

    // Log a metadata access
    // Implemented by : code:IBCLogger.LogMetaDataAccessHelper
    LOGACCESS_PTR(MetaData,const void)

    // Log a metadata search
    // Implemented by : code:IBCLogger.LogMetaDataSearchAccessHelper
    LOGACCESS_PTR(MetaDataSearch,const void)

    // Log a RVA fielddesc access */
    // Implemented by : code:IBCLogger.LogRVADataAccessHelper
    LOGACCESS_PTR(RVAData,FieldDesc)

    // Log a lookup  in the type hash table
    // Implemented by : code:IBCLogger.LogTypeHashTableAccessHelper
    LOGACCESS_PTR(TypeHashTable,TypeHandle const)

    // Log a lookup  in the Rid map
    // Implemented by : code:IBCLogger.LogRidMapAccessHelper
    LOGACCESS_VALUE( RidMap, RidMapLogData );

public:

#ifdef IBCLOGGER_ENABLED
    IBCLogger();
    ~IBCLogger();

    // Methods for enabling/disabling instrumentation.
    void EnableAllInstr();
    void DisableAllInstr();

    void DisableRidAccessOrderInstr();
    void DisableMethodDescAccessInstr();

    inline BOOL InstrEnabled()
    {
        SUPPORTS_DAC;
        return (dwInstrEnabled != 0);
    }

    static CrstStatic * GetSync();

private:
    void LogMethodAccessHelper(const MethodDesc* pMD, ULONG flagNum);
    static void LogMethodAccessWrapper(IBCLogger* pLogger, const void * pValue1, const void * pValue2);

    void LogTypeAccessHelper(TypeHandle th, ULONG flagNum);
    static void LogTypeAccessWrapper(IBCLogger* pLogger, const void * pValue1, const void * pValue2);

    BOOL MethodDescAccessInstrEnabled();
    BOOL RidAccessInstrEnabled();

private:
    DWORD dwInstrEnabled;

    static CrstStatic m_sync;
#else // IBCLOGGER_ENABLED
    void EnableAllInstr()
    {
    }

    void DisableAllInstr()
    {
    }

    inline BOOL InstrEnabled()
    {
        return false;
    }

    static CrstStatic * GetSync()
    {
        _ASSERTE(false);
        return NULL;
    }
#endif // IBCLOGGER_ENABLED
};

#endif // IBCLOGGER_H
