// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-APPDOMAIN.H
//

#ifndef __MONO_DEBUGGER_CORDB_APPDOMAIN_H__
#define __MONO_DEBUGGER_CORDB_APPDOMAIN_H__

#include <cordb.h>

class CordbAppDomain : public CordbBaseMono,
                       public ICorDebugAppDomain,
                       public ICorDebugAppDomain2,
                       public ICorDebugAppDomain3,
                       public ICorDebugAppDomain4 {
public:
  CordbAppDomain(Connection *conn, ICorDebugProcess *ppProcess);
  ICorDebugProcess *pProcess;
  HRESULT STDMETHODCALLTYPE Stop(/* [in] */ DWORD dwTimeoutIgnored);
  HRESULT STDMETHODCALLTYPE Continue(/* [in] */ BOOL fIsOutOfBand);
  HRESULT STDMETHODCALLTYPE IsRunning(/* [out] */ BOOL *pbRunning);
  HRESULT STDMETHODCALLTYPE HasQueuedCallbacks(
      /* [in] */ ICorDebugThread *pThread, /* [out] */ BOOL *pbQueued);
  HRESULT STDMETHODCALLTYPE
  EnumerateThreads(/* [out] */ ICorDebugThreadEnum **ppThreads);
  HRESULT STDMETHODCALLTYPE
  SetAllThreadsDebugState(/* [in] */ CorDebugThreadState state, /* [in] */
                          ICorDebugThread *pExceptThisThread);
  HRESULT STDMETHODCALLTYPE Detach(void);
  HRESULT STDMETHODCALLTYPE Terminate(/* [in] */ UINT exitCode);
  HRESULT STDMETHODCALLTYPE
  CanCommitChanges(/* [in] */ ULONG cSnapshots, /* [size_is][in] */
                   ICorDebugEditAndContinueSnapshot *pSnapshots[], /* [out] */
                   ICorDebugErrorInfoEnum **pError);
  HRESULT STDMETHODCALLTYPE
  CommitChanges(/* [in] */ ULONG cSnapshots, /* [size_is][in] */
                ICorDebugEditAndContinueSnapshot *pSnapshots[], /* [out] */
                ICorDebugErrorInfoEnum **pError);
  HRESULT STDMETHODCALLTYPE
  QueryInterface(/* [in] */ REFIID id, /* [iid_is][out] */
                 _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppInterface);
  ULONG STDMETHODCALLTYPE AddRef(void);
  ULONG STDMETHODCALLTYPE Release(void);
  HRESULT STDMETHODCALLTYPE
  GetProcess(/* [out] */ ICorDebugProcess **ppProcess);
  HRESULT STDMETHODCALLTYPE
  EnumerateAssemblies(/* [out] */ ICorDebugAssemblyEnum **ppAssemblies);
  HRESULT STDMETHODCALLTYPE GetModuleFromMetaDataInterface(
      /* [in] */ IUnknown *pIMetaData, /* [out] */ ICorDebugModule **ppModule);
  HRESULT STDMETHODCALLTYPE
  EnumerateBreakpoints(/* [out] */ ICorDebugBreakpointEnum **ppBreakpoints);
  HRESULT STDMETHODCALLTYPE
  EnumerateSteppers(/* [out] */ ICorDebugStepperEnum **ppSteppers);
  HRESULT STDMETHODCALLTYPE IsAttached(/* [out] */ BOOL *pbAttached);
  HRESULT STDMETHODCALLTYPE
  GetName(/* [in] */ ULONG32 cchName, /* [out] */ ULONG32 *pcchName,
          /* [length_is][size_is][out] */ WCHAR szName[]);
  HRESULT STDMETHODCALLTYPE GetObject(/* [out] */ ICorDebugValue **ppObject);
  HRESULT STDMETHODCALLTYPE Attach(void);
  HRESULT STDMETHODCALLTYPE GetID(/* [out] */ ULONG32 *pId);
  HRESULT STDMETHODCALLTYPE GetArrayOrPointerType(
      /* [in] */ CorElementType elementType, /* [in] */ ULONG32 nRank,
      /* [in] */ ICorDebugType *pTypeArg, /* [out] */
      ICorDebugType **ppType);
  HRESULT STDMETHODCALLTYPE
  GetFunctionPointerType(/* [in] */ ULONG32 nTypeArgs, /* [size_is][in] */
                         ICorDebugType *ppTypeArgs[],  /* [out] */
                         ICorDebugType **ppType);
  HRESULT STDMETHODCALLTYPE
  GetCachedWinRTTypesForIIDs(/* [in] */ ULONG32 cReqTypes, /* [size_is][in] */
                             GUID *iidsToResolve,          /* [out] */
                             ICorDebugTypeEnum **ppTypesEnum);
  HRESULT STDMETHODCALLTYPE
  GetCachedWinRTTypes(/* [out] */ ICorDebugGuidToTypeEnum **ppGuidToTypeEnum);
  HRESULT STDMETHODCALLTYPE
  GetObjectForCCW(/* [in] */ CORDB_ADDRESS ccwPointer, /* [out] */
                  ICorDebugValue **ppManagedObject);
};

class CordbAppDomainEnum : public CordbBaseMono, public ICorDebugAppDomainEnum {
  int current_pos;
  CordbProcess *pProcess;

public:
  CordbAppDomainEnum(Connection *conn, CordbProcess *ppProcess);
  HRESULT STDMETHODCALLTYPE Next(ULONG celt, ICorDebugAppDomain *values[],
                                 ULONG *pceltFetched);
  HRESULT STDMETHODCALLTYPE Skip(ULONG celt);
  HRESULT STDMETHODCALLTYPE Reset(void);
  HRESULT STDMETHODCALLTYPE Clone(ICorDebugEnum **ppEnum);
  HRESULT STDMETHODCALLTYPE GetCount(ULONG *pcelt);
  HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void **ppvObject);
  ULONG STDMETHODCALLTYPE AddRef(void);
  ULONG STDMETHODCALLTYPE Release(void);
};

#endif
