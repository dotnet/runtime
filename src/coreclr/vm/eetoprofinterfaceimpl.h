// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// EEToProfInterfaceImpl.h
//

//
// Declaration of class that wraps calling into the profiler's implementation
// of ICorProfilerCallback*
//

// ======================================================================================


#ifndef __EETOPROFINTERFACEIMPL_H__
#define __EETOPROFINTERFACEIMPL_H__

#include <stddef.h>
#include "profilepriv.h"
#include "eeprofinterfaces.h"
#include "shash.h"
#include "eventtracebase.h"
#include "gcinterface.h"

#ifdef FEATURE_PERFTRACING
#include "eventpipeadaptertypes.h"
#endif // FEATURE_PERFTRACING

class SimpleRWLock;

class ProfToEEInterfaceImpl;

interface IAssemblyBindingClosure;
struct AssemblyReferenceClosureWalkContextForProfAPI;

class EEToProfInterfaceImpl
{
    friend class ProfControlBlock;
public:

    //
    // Internal initialization / cleanup
    //

    EEToProfInterfaceImpl();
    ~EEToProfInterfaceImpl();

    HRESULT Init(
        ProfToEEInterfaceImpl * pProfToEE,
        const CLSID * pClsid,
        __inout_z LPCSTR szClsid,
        _In_z_ LPCWSTR wszProfileDLL,
        BOOL fLoadedViaAttach,
        DWORD dwConcurrentGCWaitTimeoutInMs);

    void SetProfilerInfo(ProfilerInfo *pProfilerInfo);

    BOOL IsCallback3Supported();
    BOOL IsCallback4Supported();
    BOOL IsCallback5Supported();
    BOOL IsCallback6Supported();
    BOOL IsCallback7Supported();
    BOOL IsCallback8Supported();

    HRESULT SetEventMask(DWORD dwEventMask, DWORD dwEventMaskHigh);

    // Used in ProfToEEInterfaceImpl.cpp to set this to the profiler's hook's
    // function pointer (see SetFunctionIDMapper).
    void SetFunctionIDMapper(FunctionIDMapper * pFunc);
    void SetFunctionIDMapper2(FunctionIDMapper2 * pFunc, void * clientData);

    FunctionIDMapper * GetFunctionIDMapper();
    FunctionIDMapper2 * GetFunctionIDMapper2();
    BOOL IsLoadedViaAttach();
    HRESULT EnsureProfilerDetachable();
    void SetUnrevertiblyModifiedILFlag();
    void SetModifiedRejitState();

    FunctionEnter *              GetEnterHook();
    FunctionLeave *              GetLeaveHook();
    FunctionTailcall *           GetTailcallHook();

    FunctionEnter2 *             GetEnter2Hook();
    FunctionLeave2 *             GetLeave2Hook();
    FunctionTailcall2 *          GetTailcall2Hook();

    FunctionEnter3 *             GetEnter3Hook();
    FunctionLeave3 *             GetLeave3Hook();
    FunctionTailcall3 *          GetTailcall3Hook();
    FunctionEnter3WithInfo *     GetEnter3WithInfoHook();
    FunctionLeave3WithInfo *     GetLeave3WithInfoHook();
    FunctionTailcall3WithInfo *  GetTailcall3WithInfoHook();

    BOOL IsClientIDToFunctionIDMappingEnabled();

    UINT_PTR LookupClientIDFromCache(FunctionID functionID);

    HRESULT SetEnterLeaveFunctionHooks(
        FunctionEnter * pFuncEnter,
        FunctionLeave * pFuncLeave,
        FunctionTailcall * pFuncTailcall);

    HRESULT SetEnterLeaveFunctionHooks2(
        FunctionEnter2 * pFuncEnter,
        FunctionLeave2 * pFuncLeave,
        FunctionTailcall2 * pFuncTailcall);

    HRESULT SetEnterLeaveFunctionHooks3(
        FunctionEnter3 * pFuncEnter3,
        FunctionLeave3 * pFuncLeave3,
        FunctionTailcall3 * pFuncTailcall3);

    HRESULT SetEnterLeaveFunctionHooks3WithInfo(
        FunctionEnter3WithInfo * pFuncEnter3WithInfo,
        FunctionLeave3WithInfo * pFuncLeave3WithInfo,
        FunctionTailcall3WithInfo * pFuncTailcall3WithInfo);

    BOOL RequiresGenericsContextForEnterLeave();

    UINT_PTR EEFunctionIDMapper(FunctionID funcId, BOOL * pbHookFunction);

    //
    // Initialize callback
    //

    HRESULT Initialize();

    HRESULT InitializeForAttach(void * pvClientData, UINT cbClientData);

    HRESULT ProfilerAttachComplete();

    //
    // Thread Events
    //

    HRESULT ThreadCreated(
        ThreadID    threadID);

    HRESULT ThreadDestroyed(
        ThreadID    threadID);

    HRESULT ThreadAssignedToOSThread(ThreadID managedThreadId,
                                           DWORD osThreadId);

    HRESULT ThreadNameChanged(ThreadID managedThreadId,
                              ULONG cchName,
                              _In_reads_bytes_opt_(cchName) WCHAR name[]);

    //
    // Startup/Shutdown Events
    //

    HRESULT Shutdown();

    //
    // JIT/Function Events
    //

    HRESULT FunctionUnloadStarted(
        FunctionID  functionId);

    HRESULT JITCompilationFinished(
        FunctionID  functionId,
        HRESULT     hrStatus,
        BOOL        fIsSafeToBlock);

    HRESULT JITCompilationStarted(
        FunctionID  functionId,
        BOOL        fIsSafeToBlock);

    HRESULT DynamicMethodJITCompilationStarted(
        FunctionID  functionId,
        BOOL        fIsSafeToBlock,
        LPCBYTE     pILHeader,
        ULONG       cbILHeader);

    HRESULT DynamicMethodJITCompilationFinished(
        FunctionID  functionId,
        HRESULT     hrStatus,
        BOOL        fIsSafeToBlock);

    HRESULT DynamicMethodUnloaded(
        FunctionID  functionId);

    HRESULT JITCachedFunctionSearchStarted(
        /* [in] */  FunctionID functionId,
        /* [out] */ BOOL * pbUseCachedFunction);

    HRESULT JITCachedFunctionSearchFinished(
        /* [in] */  FunctionID functionId,
        /* [in] */  COR_PRF_JIT_CACHE result);

    HRESULT JITFunctionPitched(FunctionID functionId);

    HRESULT JITInlining(
        /* [in] */  FunctionID    callerId,
        /* [in] */  FunctionID    calleeId,
        /* [out] */ BOOL *        pfShouldInline);

    HRESULT ReJITCompilationStarted(
        /* [in] */  FunctionID    functionId,
        /* [in] */  ReJITID       reJitId,
        /* [in] */  BOOL          fIsSafeToBlock);

    HRESULT GetReJITParameters(
        /* [in] */  ModuleID      moduleId,
        /* [in] */  mdMethodDef   methodId,
        /* [in] */  ICorProfilerFunctionControl *
                                  pFunctionControl);

    HRESULT ReJITCompilationFinished(
        /* [in] */  FunctionID    functionId,
        /* [in] */  ReJITID       reJitId,
        /* [in] */  HRESULT       hrStatus,
        /* [in] */  BOOL          fIsSafeToBlock);

    HRESULT ReJITError(
        /* [in] */  ModuleID      moduleId,
        /* [in] */  mdMethodDef   methodId,
        /* [in] */  FunctionID    functionId,
        /* [in] */  HRESULT       hrStatus);

    //
    // Module Events
    //

    HRESULT ModuleLoadStarted(
        ModuleID    moduleId);

    HRESULT ModuleLoadFinished(
        ModuleID    moduleId,
        HRESULT     hrStatus);

    HRESULT ModuleUnloadStarted(
        ModuleID    moduleId);

    HRESULT ModuleUnloadFinished(
        ModuleID    moduleId,
        HRESULT     hrStatus);

    HRESULT ModuleAttachedToAssembly(
        ModuleID    moduleId,
        AssemblyID  AssemblyId);

    HRESULT ModuleInMemorySymbolsUpdated(
        ModuleID    moduleId);

    //
    // Class Events
    //

    HRESULT ClassLoadStarted(
        ClassID      classId);

    HRESULT ClassLoadFinished(
        ClassID      classId,
        HRESULT     hrStatus);

    HRESULT ClassUnloadStarted(
        ClassID classId);

    HRESULT ClassUnloadFinished(
        ClassID classId,
        HRESULT hrStatus);

    //
    // AppDomain Events
    //

    HRESULT AppDomainCreationStarted(
        AppDomainID appDomainId);

    HRESULT AppDomainCreationFinished(
        AppDomainID appDomainId,
        HRESULT     hrStatus);

    HRESULT AppDomainShutdownStarted(
        AppDomainID appDomainId);

    HRESULT AppDomainShutdownFinished(
        AppDomainID appDomainId,
        HRESULT     hrStatus);

    //
    // Assembly Events
    //

    HRESULT AssemblyLoadStarted(
        AssemblyID  assemblyId);

    HRESULT AssemblyLoadFinished(
        AssemblyID  assemblyId,
        HRESULT     hrStatus);

    HRESULT AssemblyUnloadStarted(
        AssemblyID  assemblyId);

    HRESULT AssemblyUnloadFinished(
        AssemblyID  assemblyId,
        HRESULT     hrStatus);

    //
    // Transition Events
    //

    HRESULT UnmanagedToManagedTransition(
        FunctionID functionId,
        COR_PRF_TRANSITION_REASON reason);

    HRESULT ManagedToUnmanagedTransition(
        FunctionID functionId,
        COR_PRF_TRANSITION_REASON reason);

    //
    // Exception Events
    //

    HRESULT ExceptionThrown(
        ObjectID thrownObjectId);

    HRESULT ExceptionSearchFunctionEnter(
        FunctionID functionId);

    HRESULT ExceptionSearchFunctionLeave();

    HRESULT ExceptionSearchFilterEnter(
        FunctionID funcId);

    HRESULT ExceptionSearchFilterLeave();

    HRESULT ExceptionSearchCatcherFound(
        FunctionID functionId);

    HRESULT ExceptionOSHandlerEnter(
        FunctionID funcId);

    HRESULT ExceptionOSHandlerLeave(
        FunctionID funcId);

    HRESULT ExceptionUnwindFunctionEnter(
        FunctionID functionId);

    HRESULT ExceptionUnwindFunctionLeave();

    HRESULT ExceptionUnwindFinallyEnter(
        FunctionID functionId);

    HRESULT ExceptionUnwindFinallyLeave();

    HRESULT ExceptionCatcherEnter(
        FunctionID functionId,
        ObjectID objectId);

    HRESULT ExceptionCatcherLeave();

    //
    // CCW Events
    //

    HRESULT COMClassicVTableCreated(
        /* [in] */ ClassID wrappedClassId,
        /* [in] */ REFGUID implementedIID,
        /* [in] */ void * pVTable,
        /* [in] */ ULONG cSlots);

    HRESULT COMClassicVTableDestroyed(
        /* [in] */ ClassID wrappedClassId,
        /* [in] */ REFGUID implementedIID,
        /* [in] */ void * pVTable);

    //
    // GC Events
    //

    HRESULT RuntimeSuspendStarted(COR_PRF_SUSPEND_REASON suspendReason);

    HRESULT RuntimeSuspendFinished();

    HRESULT RuntimeSuspendAborted();

    HRESULT RuntimeResumeStarted();

    HRESULT RuntimeResumeFinished();

    HRESULT RuntimeThreadSuspended(ThreadID suspendedThreadId);

    HRESULT RuntimeThreadResumed(ThreadID resumedThreadId);

    HRESULT ObjectAllocated(
        /* [in] */ ObjectID objectId,
        /* [in] */ ClassID classId);

    HRESULT FinalizeableObjectQueued(BOOL isCritical, ObjectID objectID);

    //
    // GC Moved References and RootReferences2 Notification Stuff
    //

    HRESULT MovedReference(BYTE * pbMemBlockStart,
                           BYTE * pbMemBlockEnd,
                           ptrdiff_t cbRelocDistance,
                           void * pHeapId,
                           BOOL fCompacting);

    HRESULT EndMovedReferences(void * pHeapId);

    HRESULT RootReference2(BYTE * objectId,
                           EtwGCRootKind dwEtwRootKind,
                           EtwGCRootFlags dwEtwRootFlags,
                           void * rootID,
                           void * pHeapId);

    HRESULT EndRootReferences2(void * pHeapId);

    HRESULT ConditionalWeakTableElementReference(BYTE * primaryObjectId,
                           BYTE * secondaryObjectId,
                           void * rootID,
                           void * pHeapId);

    HRESULT EndConditionalWeakTableElementReferences(void * pHeapId);

    //
    // GC Root notification stuff
    //

    HRESULT AllocByClass(ObjectID objId, ClassID classId, void* pHeapId);

    HRESULT EndAllocByClass(void * pHeapId);

    //
    // Heap walk notification stuff
    //
    HRESULT ObjectReference(ObjectID objId,
                            ClassID classId,
                            ULONG cNumRefs,
                            ObjectID * arrObjRef);

    //
    // GC Handle creation / destruction notifications
    //
    HRESULT HandleCreated(UINT_PTR handleId, ObjectID initialObjectId);

    HRESULT HandleDestroyed(UINT_PTR handleId);

    HRESULT GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason);

    HRESULT GarbageCollectionFinished();

    //
    // Detach
    //
    HRESULT ProfilerDetachSucceeded();

    BOOL HasTimedOutWaitingForConcurrentGC();

    HRESULT GetAssemblyReferences(LPCWSTR wszAssemblyPath, IAssemblyBindingClosure * pClosure, AssemblyReferenceClosureWalkContextForProfAPI * pContext);

    //
    // Event Pipe
    //
    HRESULT EventPipeEventDelivered(
        EventPipeProvider *provider,
        DWORD eventId,
        DWORD eventVersion,
        ULONG cbMetadataBlob,
        LPCBYTE metadataBlob,
        ULONG cbEventData,
        LPCBYTE eventData,
        LPCGUID pActivityId,
        LPCGUID pRelatedActivityId,
        Thread *pEventThread,
        ULONG numStackFrames,
        UINT_PTR stackFrames[]);

    HRESULT EventPipeProviderCreated(EventPipeProvider *provider);

    HRESULT LoadAsNotificationOnly(BOOL *pbNotificationOnly);

    ProfToEEInterfaceImpl *GetProfToEE()
    {
        return m_pProfToEE;
    }
private:

    //
    // Generation 0 Allocation by Class notification stuff
    //

    // This is for a hashing of ClassID values
    struct CLASSHASHENTRY : HASHENTRY
    {
        ClassID         m_clsId;        // The class ID (also the key)
        size_t          m_count;        // How many of this class have been counted
    };

    // This is a simple implementation of CHashTable to provide a very simple
    // implementation of the Cmp pure virtual function
    class CHashTableImpl : public CHashTable
    {
    public:
        CHashTableImpl(ULONG iBuckets);
        virtual ~CHashTableImpl();

    protected:
        virtual BOOL Cmp(SIZE_T k1, const HASHENTRY * pc2);
    };

    // This contains the data for storing allocation information
    // in terms of numbers of objects sorted by class.
    struct AllocByClassData
    {
        CHashTableImpl *    pHashTable;     // The hash table
        CLASSHASHENTRY *    arrHash;        // Array that the hashtable uses for linking
        ULONG               cHash;          // The total number of elements in arrHash
        ULONG               iHash;          // Next empty entry in the hash array
        ClassID *           arrClsId;       // Array of ClassIDs for the call to ObjectsAllocatedByClass
        ULONG *             arrcObjects;    // Array of counts for the call to ObjectsAllocatedByClass
        size_t              cLength;        // Length of the above two parallel arrays
    };

    static const UINT kcReferencesMax = 512;

    struct GCReferencesData
    {
        size_t curIdx;
        size_t compactingCount;
        BYTE * arrpbMemBlockStartOld[kcReferencesMax];
        BYTE * arrpbMemBlockStartNew[kcReferencesMax];
        union
        {
            size_t arrMemBlockSize[kcReferencesMax];
            ULONG arrULONG[kcReferencesMax];
            BYTE * arrpbRootId[kcReferencesMax];
        };
        GCReferencesData * pNext;
    };

    // Since this stuff can only be performed by one thread (right now), we don't need
    // to make this thread safe and can just have one block we reuse every time around
    static AllocByClassData * m_pSavedAllocDataBlock;

    // Pointer to the profiler's implementation of the callback interface(s).
    // Profilers MUST support ICorProfilerCallback2.
    // Profilers MAY optionally support ICorProfilerCallback3,4,5,6,7,8,9,10
    ICorProfilerCallback2  * m_pCallback2;
    ICorProfilerCallback3  * m_pCallback3;
    ICorProfilerCallback4  * m_pCallback4;
    ICorProfilerCallback5  * m_pCallback5;
    ICorProfilerCallback6  * m_pCallback6;
    ICorProfilerCallback7  * m_pCallback7;
    ICorProfilerCallback8  * m_pCallback8;
    ICorProfilerCallback9  * m_pCallback9;
    ICorProfilerCallback10 * m_pCallback10;
    ICorProfilerCallback11 * m_pCallback11;

    HMODULE                 m_hmodProfilerDLL;

    BOOL                    m_fLoadedViaAttach;
    ProfToEEInterfaceImpl * m_pProfToEE;

    // Used in EEToProfInterfaceImpl.cpp to call into the profiler (see EEFunctionIDMapper)
    FunctionIDMapper * m_pProfilersFuncIDMapper;
    FunctionIDMapper2 * m_pProfilersFuncIDMapper2;
    void * m_pProfilersFuncIDMapper2ClientData;

    // This will contain a list of free ref data structs, so they
    // don't have to be re-allocated on every GC
    GCReferencesData * m_pGCRefDataFreeList;

    // This is for managing access to the free list above.
    CRITSEC_COOKIE m_csGCRefDataFreeList;

    FunctionEnter *         m_pEnter;
    FunctionLeave *         m_pLeave;
    FunctionTailcall *      m_pTailcall;

    FunctionEnter2 *        m_pEnter2;
    FunctionLeave2 *        m_pLeave2;
    FunctionTailcall2 *     m_pTailcall2;

    BOOL m_fIsClientIDToFunctionIDMappingEnabled;

    FunctionEnter3 *        m_pEnter3;
    FunctionLeave3 *        m_pLeave3;
    FunctionTailcall3 *     m_pTailcall3;

    FunctionEnter3WithInfo *    m_pEnter3WithInfo;
    FunctionLeave3WithInfo *    m_pLeave3WithInfo;
    FunctionTailcall3WithInfo * m_pTailcall3WithInfo;


    // Remembers whether the profiler used SetILFunctionBody() which modifies IL in a
    // way that cannot be reverted.  This prevents a detach from succeeding.
    BOOL                    m_fUnrevertiblyModifiedIL;

    // Remember whether the profiler has enabled Rejit, and prevent detach if it has.
    BOOL                    m_fModifiedRejitState;
    ProfilerInfo           *m_pProfilerInfo;

    GCReferencesData * AllocateMovedReferencesData();

    void FreeMovedReferencesData(GCReferencesData * pData);

    HRESULT MovedReferences(GCReferencesData * pData);

    HRESULT RootReferences2(GCReferencesData * pData);

    HRESULT ConditionalWeakTableElementReferences(GCReferencesData * pData);

    HRESULT NotifyAllocByClass(AllocByClassData * pData);

    HRESULT CreateProfiler(
        const CLSID * pClsid,
        _In_z_ LPCSTR szClsid,
        _In_z_ LPCWSTR wszProfileDLL);

    HRESULT DetermineAndSetEnterLeaveFunctionHooksForJit();

    HRESULT STDMETHODCALLTYPE SetEnterLeaveFunctionHooksForJit(
        FunctionEnter3 * pFuncEnter,
        FunctionLeave3 * pFuncLeave,
        FunctionTailcall3 * pFuncTailcall);

    struct FunctionIDAndClientID
    {
        FunctionID functionID;
        UINT_PTR clientID;
    };

    class FunctionIDHashTableTraits : public NoRemoveSHashTraits<DefaultSHashTraits<FunctionIDAndClientID> >
    {
    public:

        static const COUNT_T s_minimum_allocation = 31;
        typedef DefaultSHashTraits<FunctionIDAndClientID *>::count_t count_t;
        typedef UINT_PTR key_t;

        static key_t GetKey(FunctionIDAndClientID e)
        {
            LIMITED_METHOD_CONTRACT;
            return e.functionID;
        }

        static BOOL Equals(key_t k1, key_t k2)
        {
            LIMITED_METHOD_CONTRACT;
            return k1 == k2;
        }

        static count_t Hash(key_t k)
        {
            LIMITED_METHOD_CONTRACT;
            return (count_t)k;
        }

        static const FunctionIDAndClientID Null()
        {
            LIMITED_METHOD_CONTRACT;
            FunctionIDAndClientID functionIDAndClientID;
            functionIDAndClientID.functionID = NULL;
            functionIDAndClientID.clientID   = NULL;
            return functionIDAndClientID;
        }

        static bool IsNull(const FunctionIDAndClientID &functionIDAndClientID)
        {
            LIMITED_METHOD_CONTRACT;
            _ASSERTE((functionIDAndClientID.functionID != NULL) || (functionIDAndClientID.clientID == NULL));
            return functionIDAndClientID.functionID == NULL;
        }
    };

    typedef SHash<FunctionIDHashTableTraits> FunctionIDHashTable;

    // ELT3 no long keeps track of FunctionID of current managed method.  Therefore, a hash table of bookkeeping
    // the mapping from FunctionID to clientID is needed to build up ELT2 on top of ELT3.  When ELT2 (slow-path
    // or fast-path) is registered by the profiler and the profiler's IDFunctionMapper requests to hook up the
    // function being loading, the clientID returned by FunctionIDMapper will be saved as the value to be looked
    // up by the corresponding FunctionID in the hash table.  FunctionIDs can be recycled after an app domain
    // that contains the function bodies is unloaded so this hash table needs to replace the existing FunctionID
    // with new FunctionID if a duplication is found in the hash table.
    FunctionIDHashTable * m_pFunctionIDHashTable;

    // Since the hash table can be read and writen concurrently, a reader-writer lock is used to synchronize
    // all accesses to the hash table.
    SimpleRWLock * m_pFunctionIDHashTableRWLock;

    // Timeout for wait operation on concurrent GC. Only used for attach scenario
    DWORD m_dwConcurrentGCWaitTimeoutInMs;

    // Remember the fact we've timed out when waiting for concurrent GC. Will report the error later
    BOOL m_bHasTimedOutWaitingForConcurrentGC;
};

#endif // __EETOPROFINTERFACEIMPL_H__
