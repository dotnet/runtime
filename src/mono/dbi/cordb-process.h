// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-PROCESS.H
//

#ifndef __MONO_DEBUGGER_CORDB_PROCESS_H__
#define __MONO_DEBUGGER_CORDB_PROCESS_H__

#include <arraylist.h>
#include <shash.h>
#include <cordb.h>



class CordbProcess : public CordbBaseMono,
                     public ICorDebugProcess,
                     public ICorDebugProcess2,
                     public ICorDebugProcess3,
                     public ICorDebugProcess4,
                     public ICorDebugProcess5,
                     public ICorDebugProcess7,
                     public ICorDebugProcess8,
                     public ICorDebugProcess10,
                     public ICorDebugProcess11
{
    ArrayList*          m_pBreakpoints;
    ArrayList*          m_pThreads;
    ArrayList*          m_pFunctions;
    ArrayList*          m_pModules;
    ArrayList*          m_pPendingEval;
    ArrayList*          m_pSteppers;
    CordbAppDomainEnum* m_pAppDomainEnum;
    Cordb*              m_pCordb;
    BOOL                m_bIsJustMyCode;
    ArrayList*          m_pTypeMapArray; //TODO: define a better data structure to find CordbType
    MapSHashWithRemove<mdToken, CordbClass*>    m_classMap;
public:
    ArrayList* m_pAddDomains;
    CordbProcess(Cordb* cordb);
    ULONG STDMETHODCALLTYPE AddRef(void)
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release(void)
    {
        return (BaseRelease());
    }
    const char* GetClassName()
    {
        return "CordbProcess";
    }
    Cordb* GetCordb() const
    {
        return m_pCordb;
    }
    ~CordbProcess();
    HRESULT STDMETHODCALLTYPE EnumerateLoaderHeapMemoryRegions(ICorDebugMemoryRangeEnum** ppRanges);
    HRESULT STDMETHODCALLTYPE EnableGCNotificationEvents(BOOL fEnable);
    HRESULT STDMETHODCALLTYPE EnableExceptionCallbacksOutsideOfMyCode(BOOL enableExceptionsOutsideOfJMC);
    HRESULT STDMETHODCALLTYPE SetWriteableMetadataUpdateMode(WriteableMetadataUpdateMode flags);
    HRESULT STDMETHODCALLTYPE GetGCHeapInformation(COR_HEAPINFO* pHeapInfo);
    HRESULT STDMETHODCALLTYPE EnumerateHeap(ICorDebugHeapEnum** ppObjects);
    HRESULT STDMETHODCALLTYPE EnumerateHeapRegions(ICorDebugHeapSegmentEnum** ppRegions);
    HRESULT STDMETHODCALLTYPE GetObject(CORDB_ADDRESS addr, ICorDebugObjectValue** pObject);
    HRESULT STDMETHODCALLTYPE EnumerateGCReferences(BOOL enumerateWeakReferences, ICorDebugGCReferenceEnum** ppEnum);
    HRESULT STDMETHODCALLTYPE EnumerateHandles(CorGCReferenceType types, ICorDebugGCReferenceEnum** ppEnum);
    HRESULT STDMETHODCALLTYPE GetTypeID(CORDB_ADDRESS obj, COR_TYPEID* pId);
    HRESULT STDMETHODCALLTYPE GetTypeForTypeID(COR_TYPEID id, ICorDebugType** ppType);
    HRESULT STDMETHODCALLTYPE GetArrayLayout(COR_TYPEID id, COR_ARRAY_LAYOUT* pLayout);
    HRESULT STDMETHODCALLTYPE GetTypeLayout(COR_TYPEID id, COR_TYPE_LAYOUT* pLayout);
    HRESULT STDMETHODCALLTYPE GetTypeFields(COR_TYPEID id, ULONG32 celt, COR_FIELD fields[], ULONG32* pceltNeeded);
    HRESULT STDMETHODCALLTYPE EnableNGENPolicy(CorDebugNGENPolicy ePolicy);
    HRESULT STDMETHODCALLTYPE Filter(const BYTE                pRecord[],
           DWORD                     countBytes,
           CorDebugRecordFormat      format,
           DWORD                     dwFlags,
           DWORD                     dwThreadId,
           ICorDebugManagedCallback* pCallback,
           CORDB_CONTINUE_STATUS*    pContinueStatus);
    HRESULT STDMETHODCALLTYPE ProcessStateChanged(CorDebugStateChange eChange);
    HRESULT STDMETHODCALLTYPE SetEnableCustomNotification(ICorDebugClass* pClass, BOOL fEnable);
    HRESULT STDMETHODCALLTYPE GetID(DWORD* pdwProcessId);
    HRESULT STDMETHODCALLTYPE GetHandle(HPROCESS* phProcessHandle);
    HRESULT STDMETHODCALLTYPE GetThread(DWORD dwThreadId, ICorDebugThread** ppThread);
    HRESULT STDMETHODCALLTYPE EnumerateObjects(ICorDebugObjectEnum** ppObjects);
    HRESULT STDMETHODCALLTYPE IsTransitionStub(CORDB_ADDRESS address, BOOL* pbTransitionStub);
    HRESULT STDMETHODCALLTYPE IsOSSuspended(DWORD threadID, BOOL* pbSuspended);
    HRESULT STDMETHODCALLTYPE GetThreadContext(DWORD threadID, ULONG32 contextSize, BYTE context[]);
    HRESULT STDMETHODCALLTYPE SetThreadContext(DWORD threadID, ULONG32 contextSize, BYTE context[]);
    HRESULT STDMETHODCALLTYPE ReadMemory(CORDB_ADDRESS address, DWORD size, BYTE buffer[], SIZE_T* read);
    HRESULT STDMETHODCALLTYPE WriteMemory(CORDB_ADDRESS address, DWORD size, BYTE buffer[], SIZE_T* written);
    HRESULT STDMETHODCALLTYPE ClearCurrentException(DWORD threadID);
    HRESULT STDMETHODCALLTYPE EnableLogMessages(BOOL fOnOff);
    HRESULT STDMETHODCALLTYPE ModifyLogSwitch(_In_ WCHAR* pLogSwitchName, LONG lLevel);
    HRESULT STDMETHODCALLTYPE EnumerateAppDomains(ICorDebugAppDomainEnum** ppAppDomains);
    HRESULT STDMETHODCALLTYPE GetObject(ICorDebugValue** ppObject);
    HRESULT STDMETHODCALLTYPE ThreadForFiberCookie(DWORD fiberCookie, ICorDebugThread** ppThread);
    HRESULT STDMETHODCALLTYPE GetHelperThreadID(DWORD* pThreadID);
    HRESULT STDMETHODCALLTYPE GetThreadForTaskID(TASKID taskid, ICorDebugThread2** ppThread);
    HRESULT STDMETHODCALLTYPE GetVersion(COR_VERSION* version);
    HRESULT STDMETHODCALLTYPE SetUnmanagedBreakpoint(CORDB_ADDRESS address, ULONG32 bufsize, BYTE buffer[], ULONG32* bufLen);
    HRESULT STDMETHODCALLTYPE ClearUnmanagedBreakpoint(CORDB_ADDRESS address);
    HRESULT STDMETHODCALLTYPE SetDesiredNGENCompilerFlags(DWORD pdwFlags);
    HRESULT STDMETHODCALLTYPE GetDesiredNGENCompilerFlags(DWORD* pdwFlags);
    HRESULT STDMETHODCALLTYPE GetReferenceValueFromGCHandle(UINT_PTR handle, ICorDebugReferenceValue** pOutValue);
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID id, _COM_Outptr_ void __RPC_FAR* __RPC_FAR* pInterface);

    HRESULT STDMETHODCALLTYPE Stop(DWORD dwTimeoutIgnored);
    HRESULT STDMETHODCALLTYPE Continue(BOOL fIsOutOfBand);
    HRESULT STDMETHODCALLTYPE IsRunning(BOOL* pbRunning);
    HRESULT STDMETHODCALLTYPE HasQueuedCallbacks(ICorDebugThread* pThread, BOOL* pbQueued);
    HRESULT STDMETHODCALLTYPE EnumerateThreads(ICorDebugThreadEnum** ppThreads);
    HRESULT STDMETHODCALLTYPE SetAllThreadsDebugState(CorDebugThreadState state, ICorDebugThread* pExceptThisThread);
    HRESULT STDMETHODCALLTYPE Detach(void);
    HRESULT STDMETHODCALLTYPE Terminate(UINT exitCode);
    HRESULT STDMETHODCALLTYPE CanCommitChanges(ULONG cSnapshots, ICorDebugEditAndContinueSnapshot* pSnapshots[], ICorDebugErrorInfoEnum** pError);
    HRESULT STDMETHODCALLTYPE CommitChanges(ULONG cSnapshots, ICorDebugEditAndContinueSnapshot* pSnapshots[], ICorDebugErrorInfoEnum** pError);

    void                     AddThread(CordbThread* thread);
    void                     AddFunction(CordbFunction* function);
    void                     AddModule(CordbModule* module);
    void                     AddAppDomain(CordbAppDomain* appDomain);
    void                     AddBreakpoint(CordbFunctionBreakpoint* bp);
    void                     AddPendingEval(CordbEval* eval);
    void                     AddStepper(CordbStepper* step);
    CordbClass*              FindOrAddClass(mdToken token, int module_id);
    CordbType*               FindOrAddPrimitiveType(CorElementType type);
    CordbType*               FindOrAddClassType(CorElementType type, CordbClass *klass); 
    CordbType*               FindOrAddArrayType(CorElementType type, CordbType* elementType);
    //CordbType*             FindOrAddGenericInstanceType(CorElementType type, std::initializer_list<CordbType*> arrayType); //use std::initializer_list for generic instances
    CordbFunction*           FindFunction(int id);
    CordbModule*             GetModule(int module_id);
    CordbStepper*            GetStepper(int id);
    CordbAppDomain*          GetCurrentAppDomain();
    CordbThread*             FindThread(long thread_id);
    CordbFunctionBreakpoint* GetBreakpoint(int id);
    void                     CheckPendingEval();
    void                     SetJMCStatus(BOOL bIsJustMyCode);
    BOOL                     GetJMCStatus();
};

#endif
