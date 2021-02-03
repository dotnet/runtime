// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-APPDOMAIN.CPP
//

#include <cordb-appdomain.h>
#include <cordb-process.h>
#include <cordb.h>

using namespace std;

CordbAppDomain::CordbAppDomain(Connection *conn, ICorDebugProcess *ppProcess)
    : CordbBaseMono(conn) {
  pProcess = ppProcess;
  ((CordbProcess*)(ppProcess))->appdomains->Append(this);
}

HRESULT CordbAppDomain::Stop(/* [in] */ DWORD dwTimeoutIgnored) {
  LOG((LF_CORDB, LL_INFO100000, "CordbAppDomain - Stop - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbAppDomain::Continue(/* [in] */ BOOL fIsOutOfBand) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbAppDomain - Continue - NOT IMPLEMENTED\n"));

  pProcess->Continue(fIsOutOfBand);
  return S_OK;
}

HRESULT CordbAppDomain::IsRunning(/* [out] */ BOOL *pbRunning) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbAppDomain - IsRunning - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbAppDomain::HasQueuedCallbacks(/* [in] */ ICorDebugThread *pThread,
                                           /* [out] */ BOOL *pbQueued) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbAppDomain - HasQueuedCallbacks - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT
CordbAppDomain::EnumerateThreads(/* [out] */ ICorDebugThreadEnum **ppThreads) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbAppDomain - EnumerateThreads - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbAppDomain::SetAllThreadsDebugState(
    /* [in] */ CorDebugThreadState state, /* [in] */
    ICorDebugThread *pExceptThisThread) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbAppDomain - SetAllThreadsDebugState - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbAppDomain::Detach(void) {
  LOG((LF_CORDB, LL_INFO100000, "CordbAppDomain - Detach - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbAppDomain::Terminate(/* [in] */ UINT exitCode) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbAppDomain - Terminate - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbAppDomain::CanCommitChanges(
    /* [in] */ ULONG cSnapshots,                    /* [size_is][in] */
    ICorDebugEditAndContinueSnapshot *pSnapshots[], /* [out] */
    ICorDebugErrorInfoEnum **pError) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbAppDomain - CanCommitChanges - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbAppDomain::CommitChanges(
    /* [in] */ ULONG cSnapshots,                    /* [size_is][in] */
    ICorDebugEditAndContinueSnapshot *pSnapshots[], /* [out] */
    ICorDebugErrorInfoEnum **pError) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbAppDomain - CommitChanges - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbAppDomain::QueryInterface(
    /* [in] */ REFIID id, /* [iid_is][out] */
    _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppInterface) {
  if (id == IID_ICorDebugAppDomain) {
    *ppInterface = (ICorDebugAppDomain *)this;
  } else if (id == IID_ICorDebugAppDomain2) {
    *ppInterface = (ICorDebugAppDomain2 *)this;
  } else if (id == IID_ICorDebugAppDomain3) {
    *ppInterface = (ICorDebugAppDomain3 *)this;
  } else if (id == IID_ICorDebugAppDomain4) {
    *ppInterface = (ICorDebugAppDomain4 *)this;
  } else if (id == IID_ICorDebugController)
    *ppInterface = (ICorDebugController *)(ICorDebugAppDomain *)this;
  else if (id == IID_IUnknown)
    *ppInterface = (IUnknown *)(ICorDebugAppDomain *)this;
  else {
    *ppInterface = NULL;
    return E_NOINTERFACE;
  }
  return S_OK;
}

ULONG CordbAppDomain::AddRef(void) { return S_OK; }

ULONG CordbAppDomain::Release(void) { return S_OK; }

HRESULT CordbAppDomain::GetProcess(
    /* [out] */ ICorDebugProcess **ppProcess) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbAppDomain - GetProcess - NOT IMPLEMENTED\n"));

  *ppProcess = pProcess;
  return S_OK;
}

HRESULT CordbAppDomain::EnumerateAssemblies(
    /* [out] */ ICorDebugAssemblyEnum **ppAssemblies) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbAppDomain - EnumerateAssemblies - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbAppDomain::GetModuleFromMetaDataInterface(
    /* [in] */ IUnknown *pIMetaData, /* [out] */
    ICorDebugModule **ppModule) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbAppDomain - GetModuleFromMetaDataInterface - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbAppDomain::EnumerateBreakpoints(
    /* [out] */ ICorDebugBreakpointEnum **ppBreakpoints) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbAppDomain - EnumerateBreakpoints - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbAppDomain::EnumerateSteppers(
    /* [out] */ ICorDebugStepperEnum **ppSteppers) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbAppDomain - EnumerateSteppers - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbAppDomain::IsAttached(/* [out] */ BOOL *pbAttached) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbAppDomain - IsAttached - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT
CordbAppDomain::GetName(/* [in] */ ULONG32 cchName,
                        /* [out] */ ULONG32 *pcchName,
                        /* [length_is][size_is][out] */ WCHAR szName[]) {
  LOG((LF_CORDB, LL_INFO1000000, "CordbAppDomain - GetName - IMPLEMENTED\n"));
  if (cchName < strlen("DefaultDomain")) {
    *pcchName = strlen("DefaultDomain") + 1;
    return S_OK;
  }
  wcscpy(szName, L"DefaultDomain");

  return S_OK;
}

HRESULT CordbAppDomain::GetObject(/* [out] */ ICorDebugValue **ppObject) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbAppDomain - GetObject - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbAppDomain::Attach(void) {
  LOG((LF_CORDB, LL_INFO100000, "CordbAppDomain - Attach - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbAppDomain::GetID(/* [out] */ ULONG32 *pId) {
  *pId = 0;
  LOG((LF_CORDB, LL_INFO1000000, "CordbAppDomain - GetID - IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbAppDomain::GetArrayOrPointerType(
    /* [in] */ CorElementType elementType, /* [in] */ ULONG32 nRank,
    /* [in] */
    ICorDebugType *pTypeArg, /* [out] */ ICorDebugType **ppType) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbAppDomain - GetArrayOrPointerType - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbAppDomain::GetFunctionPointerType(
    /* [in] */ ULONG32 nTypeArgs, /* [size_is][in] */
    ICorDebugType *ppTypeArgs[],  /* [out] */
    ICorDebugType **ppType) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbAppDomain - GetFunctionPointerType - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbAppDomain::GetCachedWinRTTypesForIIDs(
    /* [in] */ ULONG32 cReqTypes, /* [size_is][in] */
    GUID *iidsToResolve,          /* [out] */
    ICorDebugTypeEnum **ppTypesEnum) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbAppDomain - GetCachedWinRTTypesForIIDs - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbAppDomain::GetCachedWinRTTypes(
    /* [out] */ ICorDebugGuidToTypeEnum **ppGuidToTypeEnum) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbAppDomain - GetCachedWinRTTypes - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT
CordbAppDomain::GetObjectForCCW(/* [in] */ CORDB_ADDRESS ccwPointer, /* [out] */
                                ICorDebugValue **ppManagedObject) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbAppDomain - GetObjectForCCW - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbAppDomainEnum::Next(ULONG celt,
                                                   ICorDebugAppDomain *values[],
                                                   ULONG *pceltFetched) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbAppDomainEnum - Next - NOT IMPLEMENTED\n"));
  *pceltFetched = celt;
  for (int i = 0; i < celt; i++) {
    if (current_pos >= pProcess->appdomains->GetCount()) {
      *pceltFetched = 0;
      return S_FALSE;
    }
    CordbAppDomain* appdomain = (CordbAppDomain*)pProcess->appdomains->Get(current_pos);
    appdomain->QueryInterface(IID_ICorDebugAppDomain,
                              (void **)values + current_pos);
    current_pos++;
  }
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbAppDomainEnum::Skip(ULONG celt) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbAppDomainEnum - Skip - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbAppDomainEnum::Reset(void) {
  current_pos = 0;
  LOG((LF_CORDB, LL_INFO100000,
       "CordbAppDomainEnum - Reset - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbAppDomainEnum::Clone(ICorDebugEnum **ppEnum) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbAppDomainEnum - Clone - NOT IMPLEMENTED\n"));
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbAppDomainEnum::GetCount(ULONG *pcelt) {
  LOG((LF_CORDB, LL_INFO1000000,
       "CordbAppDomainEnum - GetCount - IMPLEMENTED\n"));
  *pcelt = pProcess->appdomains->GetCount();
  return S_OK;
}

HRESULT STDMETHODCALLTYPE
CordbAppDomainEnum::QueryInterface(REFIID id, void **pInterface) {
  if (id == IID_IUnknown)
    *pInterface =
        static_cast<IUnknown *>(static_cast<ICorDebugAppDomainEnum *>(this));
  else if (id == IID_ICorDebugAppDomainEnum)
    *pInterface = static_cast<ICorDebugAppDomainEnum *>(this);
  else if (id == IID_ICorDebugEnum)
    *pInterface = static_cast<ICorDebugEnum *>(this);
  else {
    LOG((LF_CORDB, LL_INFO100000,
         "CordbAppDomain - QueryInterface - NOT IMPLEMENTED\n"));
    return E_NOINTERFACE;
  }
  return S_OK;
}

ULONG STDMETHODCALLTYPE CordbAppDomainEnum::AddRef(void) { return 1; }

ULONG STDMETHODCALLTYPE CordbAppDomainEnum::Release(void) { return 1; }

CordbAppDomainEnum::CordbAppDomainEnum(Connection *conn,
                                       CordbProcess *ppProcess)
    : CordbBaseMono(conn) {
  current_pos = 0;
  this->pProcess = ppProcess;
}
