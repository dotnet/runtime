// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-PROCESS.CPP
//

#include <fstream>
#include <iostream>

#include <cordb-appdomain.h>
#include <cordb-frame.h>
#include <cordb-function.h>
#include <cordb-process.h>
#include <cordb-stepper.h>
#include <cordb-thread.h>
#include <cordb.h>

using namespace std;

CordbProcess::CordbProcess() : CordbBaseMono(NULL) {
  suspended = false;
  appdomains = g_ptr_array_new();
}

HRESULT CordbProcess::EnumerateLoaderHeapMemoryRegions(
    /* [out] */ ICorDebugMemoryRangeEnum **ppRanges) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - EnumerateLoaderHeapMemoryRegions - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::EnableGCNotificationEvents(BOOL fEnable) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - EnableGCNotificationEvents - NOT IMPLEMENTED\n"));

  return S_OK;
}

HRESULT CordbProcess::EnableExceptionCallbacksOutsideOfMyCode(
    /* [in] */ BOOL enableExceptionsOutsideOfJMC) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - EnableExceptionCallbacksOutsideOfMyCode - "
       "NOT IMPLEMENTED\n"));

  return S_OK;
}

HRESULT CordbProcess::SetWriteableMetadataUpdateMode(
    WriteableMetadataUpdateMode flags) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - SetWriteableMetadataUpdateMode - NOT IMPLEMENTED\n"));

  return S_OK;
}

HRESULT CordbProcess::GetGCHeapInformation(
    /* [out] */ COR_HEAPINFO *pHeapInfo) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - GetGCHeapInformation - NOT IMPLEMENTED\n"));

  return S_OK;
}

HRESULT CordbProcess::EnumerateHeap(
    /* [out] */ ICorDebugHeapEnum **ppObjects) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - EnumerateHeap - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::EnumerateHeapRegions(
    /* [out] */ ICorDebugHeapSegmentEnum **ppRegions) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - EnumerateHeapRegions - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::GetObject(
    /* [in] */ CORDB_ADDRESS addr,
    /* [out] */ ICorDebugObjectValue **pObject) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - GetObject - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::EnumerateGCReferences(
    /* [in] */ BOOL enumerateWeakReferences,
    /* [out] */ ICorDebugGCReferenceEnum **ppEnum) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - EnumerateGCReferences - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::EnumerateHandles(
    /* [in] */ CorGCReferenceType types,
    /* [out] */ ICorDebugGCReferenceEnum **ppEnum) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - EnumerateHandles - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::GetTypeID(
    /* [in] */ CORDB_ADDRESS obj,
    /* [out] */ COR_TYPEID *pId) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - GetTypeID - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::GetTypeForTypeID(
    /* [in] */ COR_TYPEID id,
    /* [out] */ ICorDebugType **ppType) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - GetTypeForTypeID - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::GetArrayLayout(
    /* [in] */ COR_TYPEID id,
    /* [out] */ COR_ARRAY_LAYOUT *pLayout) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - GetArrayLayout - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::GetTypeLayout(
    /* [in] */ COR_TYPEID id,
    /* [out] */ COR_TYPE_LAYOUT *pLayout) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - GetTypeLayout - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::GetTypeFields(
    /* [in] */ COR_TYPEID id, ULONG32 celt, COR_FIELD fields[],
    ULONG32 *pceltNeeded) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - GetTypeFields - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::EnableNGENPolicy(
    /* [in] */ CorDebugNGENPolicy ePolicy) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - EnableNGENPolicy - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::Filter(
    /* [size_is][length_is][in] */ const BYTE pRecord[],
    /* [in] */ DWORD countBytes,
    /* [in] */ CorDebugRecordFormat format,
    /* [in] */ DWORD dwFlags,
    /* [in] */ DWORD dwThreadId,
    /* [in] */ ICorDebugManagedCallback *pCallback,
    /* [out][in] */ CORDB_CONTINUE_STATUS *pContinueStatus) {
  LOG((LF_CORDB, LL_INFO100000, "CordbProcess - Filter - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::ProcessStateChanged(
    /* [in] */ CorDebugStateChange eChange) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - ProcessStateChanged - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::SetEnableCustomNotification(ICorDebugClass *pClass,
                                                  BOOL fEnable) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - SetEnableCustomNotification - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::GetID(
    /* [out] */ DWORD *pdwProcessId) {
  LOG((LF_CORDB, LL_INFO100000, "CordbProcess - GetID - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::GetHandle(
    /* [out] */ HPROCESS *phProcessHandle) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - GetHandle - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::GetThread(
    /* [in] */ DWORD dwThreadId,
    /* [out] */ ICorDebugThread **ppThread) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - GetThread - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::EnumerateObjects(
    /* [out] */ ICorDebugObjectEnum **ppObjects) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - EnumerateObjects - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::IsTransitionStub(
    /* [in] */ CORDB_ADDRESS address,
    /* [out] */ BOOL *pbTransitionStub) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - IsTransitionStub - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::IsOSSuspended(
    /* [in] */ DWORD threadID,
    /* [out] */ BOOL *pbSuspended) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - IsOSSuspended - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::GetThreadContext(
    /* [in] */ DWORD threadID,
    /* [in] */ ULONG32 contextSize,
    /* [size_is][length_is][out][in] */ BYTE context[]) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - GetThreadContext - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::SetThreadContext(
    /* [in] */ DWORD threadID,
    /* [in] */ ULONG32 contextSize,
    /* [size_is][length_is][in] */ BYTE context[]) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - SetThreadContext - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::ReadMemory(
    /* [in] */ CORDB_ADDRESS address,
    /* [in] */ DWORD size,
    /* [length_is][size_is][out] */ BYTE buffer[],
    /* [out] */ SIZE_T *read) {
  memcpy(buffer, (void *)address, size);
  if (read != NULL)
    *read = size;
  LOG((LF_CORDB, LL_INFO1000000, "CordbProcess - ReadMemory - IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::WriteMemory(
    /* [in] */ CORDB_ADDRESS address,
    /* [in] */ DWORD size,
    /* [size_is][in] */ BYTE buffer[],
    /* [out] */ SIZE_T *written) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - WriteMemory - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::ClearCurrentException(
    /* [in] */ DWORD threadID) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - ClearCurrentException - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::EnableLogMessages(
    /* [in] */ BOOL fOnOff) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - EnableLogMessages - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::ModifyLogSwitch(
    /* [annotation][in] */
    _In_ WCHAR *pLogSwitchName,
    /* [in] */ LONG lLevel) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - ModifyLogSwitch - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::EnumerateAppDomains(
    /* [out] */ ICorDebugAppDomainEnum **ppAppDomains) {
  *ppAppDomains = new CordbAppDomainEnum(conn, this);
  LOG((LF_CORDB, LL_INFO1000000,
       "CordbProcess - EnumerateAppDomains - IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::GetObject(
    /* [out] */ ICorDebugValue **ppObject) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - GetObject - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::ThreadForFiberCookie(
    /* [in] */ DWORD fiberCookie,
    /* [out] */ ICorDebugThread **ppThread) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - ThreadForFiberCookie - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::GetHelperThreadID(
    /* [out] */ DWORD *pThreadID) {
  LOG((LF_CORDB, LL_INFO100000, "GetHelperThreadID - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::GetThreadForTaskID(
    /* [in] */ TASKID taskid,
    /* [out] */ ICorDebugThread2 **ppThread) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - GetHelperThreadID - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::GetVersion(
    /* [out] */ COR_VERSION *version) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - GetVersion - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::SetUnmanagedBreakpoint(
    /* [in] */ CORDB_ADDRESS address,
    /* [in] */ ULONG32 bufsize,
    /* [length_is][size_is][out] */ BYTE buffer[],
    /* [out] */ ULONG32 *bufLen) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - SetUnmanagedBreakpoint - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::ClearUnmanagedBreakpoint(
    /* [in] */ CORDB_ADDRESS address) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - ClearUnmanagedBreakpoint - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::SetDesiredNGENCompilerFlags(
    /* [in] */ DWORD pdwFlags) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - SetDesiredNGENCompilerFlags - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::GetDesiredNGENCompilerFlags(
    /* [out] */ DWORD *pdwFlags) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - GetDesiredNGENCompilerFlags - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::GetReferenceValueFromGCHandle(
    /* [in] */ UINT_PTR handle,
    /* [out] */ ICorDebugReferenceValue **pOutValue) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - GetReferenceValueFromGCHandle - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::QueryInterface(
    /* [in] */ REFIID id,
    /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *pInterface) {
  if (id == IID_ICorDebugProcess) {
    *pInterface = static_cast<ICorDebugProcess *>(this);
  } else if (id == IID_ICorDebugController) {
    *pInterface = static_cast<ICorDebugController *>(
        static_cast<ICorDebugProcess *>(this));
  } else if (id == IID_ICorDebugProcess2) {
    *pInterface = static_cast<ICorDebugProcess2 *>(this);
  } else if (id == IID_ICorDebugProcess3) {
    *pInterface = static_cast<ICorDebugProcess3 *>(this);
  } else if (id == IID_ICorDebugProcess4) {
    *pInterface = static_cast<ICorDebugProcess4 *>(this);
  } else if (id == IID_ICorDebugProcess5) {
    *pInterface = static_cast<ICorDebugProcess5 *>(this);
  } else if (id == IID_ICorDebugProcess7) {
    *pInterface = static_cast<ICorDebugProcess7 *>(this);
  } else if (id == IID_ICorDebugProcess8) {
    *pInterface = static_cast<ICorDebugProcess8 *>(this);
  } else if (id == IID_ICorDebugProcess10) {
    *pInterface = static_cast<ICorDebugProcess10 *>(this);
  } else if (id == IID_ICorDebugProcess11) {
    *pInterface = static_cast<ICorDebugProcess11 *>(this);
  } else if (id == IID_IUnknown) {
    *pInterface =
        static_cast<IUnknown *>(static_cast<ICorDebugProcess *>(this));
  }

  else {
    *pInterface = NULL;
    return E_NOINTERFACE;
  }

  return S_OK;
}

ULONG CordbProcess::AddRef(void) { return S_OK; }

ULONG CordbProcess::Release(void) { return S_OK; }

HRESULT CordbProcess::Stop(
    /* [in] */ DWORD dwTimeoutIgnored) {
  LOG((LF_CORDB, LL_INFO1000000, "CordbProcess - Stop - IMPLEMENTED\n"));
  MdbgProtBuffer sendbuf;
  m_dbgprot_buffer_init(&sendbuf, 128);
  conn->send_event(MDBGPROT_CMD_SET_VM, MDBGPROT_CMD_VM_SUSPEND, &sendbuf);
  suspended = true;
  return S_OK;
}

HRESULT CordbProcess::Continue(
    /* [in] */ BOOL fIsOutOfBand) {
  LOG((LF_CORDB, LL_INFO1000000, "CordbProcess - Continue - IMPLEMENTED\n"));
  MdbgProtBuffer sendbuf;
  m_dbgprot_buffer_init(&sendbuf, 128);
  conn->send_event(MDBGPROT_CMD_SET_VM, MDBGPROT_CMD_VM_RESUME, &sendbuf);
  return S_OK;
}

HRESULT CordbProcess::IsRunning(
    /* [out] */ BOOL *pbRunning) {
  *pbRunning = true;
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - IsRunning - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::HasQueuedCallbacks(
    /* [in] */ ICorDebugThread *pThread,
    /* [out] */ BOOL *pbQueued) {
  // conn->process_packet_from_queue();
  *pbQueued = false;
  LOG((LF_CORDB, LL_INFO1000000,
       "CordbProcess - HasQueuedCallbacks - IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::EnumerateThreads(
    /* [out] */ ICorDebugThreadEnum **ppThreads) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - EnumerateThreads - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::SetAllThreadsDebugState(
    /* [in] */ CorDebugThreadState state,
    /* [in] */ ICorDebugThread *pExceptThisThread) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - SetAllThreadsDebugState - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::Detach(void) {
  LOG((LF_CORDB, LL_INFO100000, "CordbProcess - Detach - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::Terminate(
    /* [in] */ UINT exitCode) {
  MdbgProtBuffer sendbuf;
  m_dbgprot_buffer_init(&sendbuf, 128);
  m_dbgprot_buffer_add_int(&sendbuf, -1);
  conn->send_event(MDBGPROT_CMD_SET_VM, MDBGPROT_CMD_VM_EXIT, &sendbuf);
  return S_OK;
}

HRESULT CordbProcess::CanCommitChanges(
    /* [in] */ ULONG cSnapshots,
    /* [size_is][in] */ ICorDebugEditAndContinueSnapshot *pSnapshots[],
    /* [out] */ ICorDebugErrorInfoEnum **pError) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - CanCommitChanges - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT CordbProcess::CommitChanges(
    /* [in] */ ULONG cSnapshots,
    /* [size_is][in] */ ICorDebugEditAndContinueSnapshot *pSnapshots[],
    /* [out] */ ICorDebugErrorInfoEnum **pError) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbProcess - CommitChanges - NOT IMPLEMENTED\n"));
  return S_OK;
}
