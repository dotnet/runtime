// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-PROCESS.H
//

#ifndef __MONO_DEBUGGER_CORDB_PROCESS_H__
#define __MONO_DEBUGGER_CORDB_PROCESS_H__

#include <arraylist.h>
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
                     public ICorDebugProcess11 {
  ArrayList *m_pBreakpoints;
  ArrayList *m_pThreads;
  ArrayList *m_pFunctions;
  ArrayList *m_pModules;
  ArrayList *m_pPendingEval;
  CordbAppDomainEnum *m_pAppDomainEnum;
  Cordb *m_pCordb;

public:
  ArrayList *appDomains;
  CordbProcess(Cordb *cordb);
  ULONG AddRef(void) { return (BaseAddRef()); }
  ULONG Release(void) { return (BaseRelease()); }
  const char *GetClassName() { return "CordbProcess"; }
  Cordb *GetCordb() const { return m_pCordb; }
  ~CordbProcess();
  HRESULT EnumerateLoaderHeapMemoryRegions(ICorDebugMemoryRangeEnum **ppRanges);
  HRESULT EnableGCNotificationEvents(BOOL fEnable);
  HRESULT
  EnableExceptionCallbacksOutsideOfMyCode(BOOL enableExceptionsOutsideOfJMC);
  HRESULT
  SetWriteableMetadataUpdateMode(WriteableMetadataUpdateMode flags);
  HRESULT
  GetGCHeapInformation(COR_HEAPINFO *pHeapInfo);
  HRESULT
  EnumerateHeap(ICorDebugHeapEnum **ppObjects);
  HRESULT
  EnumerateHeapRegions(ICorDebugHeapSegmentEnum **ppRegions);
  HRESULT
  GetObject(CORDB_ADDRESS addr, ICorDebugObjectValue **pObject);
  HRESULT
  EnumerateGCReferences(BOOL enumerateWeakReferences,
                        ICorDebugGCReferenceEnum **ppEnum);
  HRESULT
  EnumerateHandles(CorGCReferenceType types, ICorDebugGCReferenceEnum **ppEnum);
  HRESULT GetTypeID(CORDB_ADDRESS obj, COR_TYPEID *pId);
  HRESULT GetTypeForTypeID(COR_TYPEID id, ICorDebugType **ppType);
  HRESULT GetArrayLayout(COR_TYPEID id, COR_ARRAY_LAYOUT *pLayout);
  HRESULT GetTypeLayout(COR_TYPEID id, COR_TYPE_LAYOUT *pLayout);
  HRESULT GetTypeFields(COR_TYPEID id, ULONG32 celt, COR_FIELD fields[],
                        ULONG32 *pceltNeeded);
  HRESULT
  EnableNGENPolicy(CorDebugNGENPolicy ePolicy);
  HRESULT
  Filter(const BYTE pRecord[], DWORD countBytes, CorDebugRecordFormat format,
         DWORD dwFlags, DWORD dwThreadId, ICorDebugManagedCallback *pCallback,
         CORDB_CONTINUE_STATUS *pContinueStatus);
  HRESULT
  ProcessStateChanged(CorDebugStateChange eChange);
  HRESULT SetEnableCustomNotification(ICorDebugClass *pClass, BOOL fEnable);
  HRESULT GetID(DWORD *pdwProcessId);
  HRESULT GetHandle(HPROCESS *phProcessHandle);
  HRESULT GetThread(DWORD dwThreadId, ICorDebugThread **ppThread);
  HRESULT
  EnumerateObjects(ICorDebugObjectEnum **ppObjects);
  HRESULT IsTransitionStub(CORDB_ADDRESS address, BOOL *pbTransitionStub);
  HRESULT IsOSSuspended(DWORD threadID, BOOL *pbSuspended);
  HRESULT
  GetThreadContext(DWORD threadID, ULONG32 contextSize, BYTE context[]);
  HRESULT
  SetThreadContext(DWORD threadID, ULONG32 contextSize, BYTE context[]);
  HRESULT
  ReadMemory(CORDB_ADDRESS address, DWORD size, BYTE buffer[], SIZE_T *read);
  HRESULT
  WriteMemory(CORDB_ADDRESS address, DWORD size, BYTE buffer[],
              SIZE_T *written);
  HRESULT ClearCurrentException(DWORD threadID);
  HRESULT EnableLogMessages(BOOL fOnOff);
  HRESULT
  ModifyLogSwitch(_In_ WCHAR *pLogSwitchName, LONG lLevel);
  HRESULT
  EnumerateAppDomains(ICorDebugAppDomainEnum **ppAppDomains);
  HRESULT GetObject(ICorDebugValue **ppObject);
  HRESULT ThreadForFiberCookie(DWORD fiberCookie, ICorDebugThread **ppThread);
  HRESULT GetHelperThreadID(DWORD *pThreadID);
  HRESULT GetThreadForTaskID(TASKID taskid, ICorDebugThread2 **ppThread);
  HRESULT GetVersion(COR_VERSION *version);
  HRESULT SetUnmanagedBreakpoint(CORDB_ADDRESS address, ULONG32 bufsize,
                                 BYTE buffer[], ULONG32 *bufLen);
  HRESULT
  ClearUnmanagedBreakpoint(CORDB_ADDRESS address);
  HRESULT
  SetDesiredNGENCompilerFlags(DWORD pdwFlags);
  HRESULT
  GetDesiredNGENCompilerFlags(DWORD *pdwFlags);
  HRESULT
  GetReferenceValueFromGCHandle(UINT_PTR handle,
                                ICorDebugReferenceValue **pOutValue);
  HRESULT
  QueryInterface(REFIID id, _COM_Outptr_ void __RPC_FAR *__RPC_FAR *pInterface);

  HRESULT Stop(DWORD dwTimeoutIgnored);
  HRESULT Continue(BOOL fIsOutOfBand);
  HRESULT IsRunning(BOOL *pbRunning);
  HRESULT HasQueuedCallbacks(ICorDebugThread *pThread, BOOL *pbQueued);
  HRESULT
  EnumerateThreads(ICorDebugThreadEnum **ppThreads);
  HRESULT
  SetAllThreadsDebugState(CorDebugThreadState state,
                          ICorDebugThread *pExceptThisThread);
  HRESULT Detach(void);
  HRESULT Terminate(UINT exitCode);
  HRESULT
  CanCommitChanges(ULONG cSnapshots,
                   ICorDebugEditAndContinueSnapshot *pSnapshots[],
                   ICorDebugErrorInfoEnum **pError);
  HRESULT
  CommitChanges(ULONG cSnapshots,
                ICorDebugEditAndContinueSnapshot *pSnapshots[],
                ICorDebugErrorInfoEnum **pError);

  void AddThread(CordbThread *thread);
  void AddFunction(CordbFunction *function);
  void AddModule(CordbModule *module);
  void AddAppDomain(CordbAppDomain *appDomain);
  void AddBreakpoint(CordbFunctionBreakpoint *bp);
  void AddPendingEval(CordbEval *eval);
  CordbFunction *FindFunction(int id);
  CordbFunction *FindFunctionByToken(int token);
  CordbModule *GetModule(int module_id);
  CordbAppDomain *GetCurrentAppDomain();
  CordbThread *FindThread(long thread_id);
  CordbFunctionBreakpoint *GetBreakpointByOffsetAndFuncId(int64_t offset,
                                                          int method_id);
  void CheckPendingEval();
};

#endif
