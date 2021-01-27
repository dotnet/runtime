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
  DEBUG_PRINTF(
      1, "CordbProcess - EnumerateLoaderHeapMemoryRegions - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::EnableGCNotificationEvents(BOOL fEnable) {
  DEBUG_PRINTF(1,
               "CordbProcess - EnableGCNotificationEvents - NOT IMPLEMENTED\n");

  return S_OK;
}

HRESULT CordbProcess::EnableExceptionCallbacksOutsideOfMyCode(
    /* [in] */ BOOL enableExceptionsOutsideOfJMC) {
  DEBUG_PRINTF(1, "CordbProcess - EnableExceptionCallbacksOutsideOfMyCode - "
                  "NOT IMPLEMENTED\n");

  return S_OK;
}

HRESULT CordbProcess::SetWriteableMetadataUpdateMode(
    WriteableMetadataUpdateMode flags) {
  DEBUG_PRINTF(
      1, "CordbProcess - SetWriteableMetadataUpdateMode - NOT IMPLEMENTED\n");

  return S_OK;
}

HRESULT CordbProcess::GetGCHeapInformation(
    /* [out] */ COR_HEAPINFO *pHeapInfo) {
  DEBUG_PRINTF(1, "CordbProcess - GetGCHeapInformation - NOT IMPLEMENTED\n");

  return S_OK;
}

HRESULT CordbProcess::EnumerateHeap(
    /* [out] */ ICorDebugHeapEnum **ppObjects) {
  DEBUG_PRINTF(1, "CordbProcess - EnumerateHeap - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::EnumerateHeapRegions(
    /* [out] */ ICorDebugHeapSegmentEnum **ppRegions) {
  DEBUG_PRINTF(1, "CordbProcess - EnumerateHeapRegions - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::GetObject(
    /* [in] */ CORDB_ADDRESS addr,
    /* [out] */ ICorDebugObjectValue **pObject) {
  DEBUG_PRINTF(1, "CordbProcess - GetObject - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::EnumerateGCReferences(
    /* [in] */ BOOL enumerateWeakReferences,
    /* [out] */ ICorDebugGCReferenceEnum **ppEnum) {
  DEBUG_PRINTF(1, "CordbProcess - EnumerateGCReferences - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::EnumerateHandles(
    /* [in] */ CorGCReferenceType types,
    /* [out] */ ICorDebugGCReferenceEnum **ppEnum) {
  DEBUG_PRINTF(1, "CordbProcess - EnumerateHandles - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::GetTypeID(
    /* [in] */ CORDB_ADDRESS obj,
    /* [out] */ COR_TYPEID *pId) {
  DEBUG_PRINTF(1, "CordbProcess - GetTypeID - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::GetTypeForTypeID(
    /* [in] */ COR_TYPEID id,
    /* [out] */ ICorDebugType **ppType) {
  DEBUG_PRINTF(1, "CordbProcess - GetTypeForTypeID - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::GetArrayLayout(
    /* [in] */ COR_TYPEID id,
    /* [out] */ COR_ARRAY_LAYOUT *pLayout) {
  DEBUG_PRINTF(1, "CordbProcess - GetArrayLayout - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::GetTypeLayout(
    /* [in] */ COR_TYPEID id,
    /* [out] */ COR_TYPE_LAYOUT *pLayout) {
  DEBUG_PRINTF(1, "CordbProcess - GetTypeLayout - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::GetTypeFields(
    /* [in] */ COR_TYPEID id, ULONG32 celt, COR_FIELD fields[],
    ULONG32 *pceltNeeded) {
  DEBUG_PRINTF(1, "CordbProcess - GetTypeFields - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::EnableNGENPolicy(
    /* [in] */ CorDebugNGENPolicy ePolicy) {
  DEBUG_PRINTF(1, "CordbProcess - EnableNGENPolicy - NOT IMPLEMENTED\n");
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
  DEBUG_PRINTF(1, "CordbProcess - Filter - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::ProcessStateChanged(
    /* [in] */ CorDebugStateChange eChange) {
  DEBUG_PRINTF(1, "CordbProcess - ProcessStateChanged - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::SetEnableCustomNotification(ICorDebugClass *pClass,
                                                  BOOL fEnable) {
  DEBUG_PRINTF(
      1, "CordbProcess - SetEnableCustomNotification - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::GetID(
    /* [out] */ DWORD *pdwProcessId) {
  DEBUG_PRINTF(1, "CordbProcess - GetID - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::GetHandle(
    /* [out] */ HPROCESS *phProcessHandle) {
  DEBUG_PRINTF(1, "CordbProcess - GetHandle - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::GetThread(
    /* [in] */ DWORD dwThreadId,
    /* [out] */ ICorDebugThread **ppThread) {
  DEBUG_PRINTF(1, "CordbProcess - GetThread - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::EnumerateObjects(
    /* [out] */ ICorDebugObjectEnum **ppObjects) {
  DEBUG_PRINTF(1, "CordbProcess - EnumerateObjects - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::IsTransitionStub(
    /* [in] */ CORDB_ADDRESS address,
    /* [out] */ BOOL *pbTransitionStub) {
  DEBUG_PRINTF(1, "CordbProcess - IsTransitionStub - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::IsOSSuspended(
    /* [in] */ DWORD threadID,
    /* [out] */ BOOL *pbSuspended) {
  DEBUG_PRINTF(1, "CordbProcess - IsOSSuspended - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::GetThreadContext(
    /* [in] */ DWORD threadID,
    /* [in] */ ULONG32 contextSize,
    /* [size_is][length_is][out][in] */ BYTE context[]) {
  DEBUG_PRINTF(1, "CordbProcess - GetThreadContext - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::SetThreadContext(
    /* [in] */ DWORD threadID,
    /* [in] */ ULONG32 contextSize,
    /* [size_is][length_is][in] */ BYTE context[]) {
  DEBUG_PRINTF(1, "CordbProcess - SetThreadContext - NOT IMPLEMENTED\n");
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
  DEBUG_PRINTF(1, "CordbProcess - ReadMemory - IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::WriteMemory(
    /* [in] */ CORDB_ADDRESS address,
    /* [in] */ DWORD size,
    /* [size_is][in] */ BYTE buffer[],
    /* [out] */ SIZE_T *written) {
  DEBUG_PRINTF(1, "CordbProcess - WriteMemory - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::ClearCurrentException(
    /* [in] */ DWORD threadID) {
  DEBUG_PRINTF(1, "CordbProcess - ClearCurrentException - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::EnableLogMessages(
    /* [in] */ BOOL fOnOff) {
  DEBUG_PRINTF(1, "CordbProcess - EnableLogMessages - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::ModifyLogSwitch(
    /* [annotation][in] */
    _In_ WCHAR *pLogSwitchName,
    /* [in] */ LONG lLevel) {
  DEBUG_PRINTF(1, "CordbProcess - ModifyLogSwitch - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::EnumerateAppDomains(
    /* [out] */ ICorDebugAppDomainEnum **ppAppDomains) {
  *ppAppDomains = new CordbAppDomainEnum(conn, this);
  DEBUG_PRINTF(1, "CordbProcess - EnumerateAppDomains - IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::GetObject(
    /* [out] */ ICorDebugValue **ppObject) {
  DEBUG_PRINTF(1, "CordbProcess - GetObject - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::ThreadForFiberCookie(
    /* [in] */ DWORD fiberCookie,
    /* [out] */ ICorDebugThread **ppThread) {
  DEBUG_PRINTF(1, "CordbProcess - ThreadForFiberCookie - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::GetHelperThreadID(
    /* [out] */ DWORD *pThreadID) {
  DEBUG_PRINTF(1, "GetHelperThreadID - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::GetThreadForTaskID(
    /* [in] */ TASKID taskid,
    /* [out] */ ICorDebugThread2 **ppThread) {
  DEBUG_PRINTF(1, "CordbProcess - GetHelperThreadID - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::GetVersion(
    /* [out] */ COR_VERSION *version) {
  DEBUG_PRINTF(1, "CordbProcess - GetVersion - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::SetUnmanagedBreakpoint(
    /* [in] */ CORDB_ADDRESS address,
    /* [in] */ ULONG32 bufsize,
    /* [length_is][size_is][out] */ BYTE buffer[],
    /* [out] */ ULONG32 *bufLen) {
  DEBUG_PRINTF(1, "CordbProcess - SetUnmanagedBreakpoint - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::ClearUnmanagedBreakpoint(
    /* [in] */ CORDB_ADDRESS address) {
  DEBUG_PRINTF(1,
               "CordbProcess - ClearUnmanagedBreakpoint - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::SetDesiredNGENCompilerFlags(
    /* [in] */ DWORD pdwFlags) {
  DEBUG_PRINTF(
      1, "CordbProcess - SetDesiredNGENCompilerFlags - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::GetDesiredNGENCompilerFlags(
    /* [out] */ DWORD *pdwFlags) {
  DEBUG_PRINTF(
      1, "CordbProcess - GetDesiredNGENCompilerFlags - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::GetReferenceValueFromGCHandle(
    /* [in] */ UINT_PTR handle,
    /* [out] */ ICorDebugReferenceValue **pOutValue) {
  DEBUG_PRINTF(
      1, "CordbProcess - GetReferenceValueFromGCHandle - NOT IMPLEMENTED\n");
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
    DEBUG_PRINTF(1, "CordbProcess - QueryInterface - E_NOTIMPL\n");

    *pInterface = NULL;
    return E_NOINTERFACE;
  }

  return S_OK;
}

ULONG CordbProcess::AddRef(void) { return S_OK; }

ULONG CordbProcess::Release(void) { return S_OK; }

HRESULT CordbProcess::Stop(
    /* [in] */ DWORD dwTimeoutIgnored) {
  DEBUG_PRINTF(1, "CordbProcess - Stop - IMPLEMENTED\n");
  MdbgProtBuffer sendbuf;
  m_dbgprot_buffer_init(&sendbuf, 128);
  conn->send_event(MDBGPROT_CMD_SET_VM, MDBGPROT_CMD_VM_SUSPEND, &sendbuf);
  suspended = true;
  return S_OK;
}

HRESULT CordbProcess::Continue(
    /* [in] */ BOOL fIsOutOfBand) {
  DEBUG_PRINTF(1, "CordbProcess - Continue - IMPLEMENTED\n");
  MdbgProtBuffer sendbuf;
  m_dbgprot_buffer_init(&sendbuf, 128);
  conn->send_event(MDBGPROT_CMD_SET_VM, MDBGPROT_CMD_VM_RESUME, &sendbuf);
  return S_OK;
}

HRESULT CordbProcess::IsRunning(
    /* [out] */ BOOL *pbRunning) {
  *pbRunning = true;
  DEBUG_PRINTF(1, "CordbProcess - IsRunning - IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::HasQueuedCallbacks(
    /* [in] */ ICorDebugThread *pThread,
    /* [out] */ BOOL *pbQueued) {
  // conn->process_packet_from_queue();
  *pbQueued = false;
  DEBUG_PRINTF(1, "CordbProcess - HasQueuedCallbacks - IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::EnumerateThreads(
    /* [out] */ ICorDebugThreadEnum **ppThreads) {
  DEBUG_PRINTF(1, "CordbProcess - EnumerateThreads - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::SetAllThreadsDebugState(
    /* [in] */ CorDebugThreadState state,
    /* [in] */ ICorDebugThread *pExceptThisThread) {
  DEBUG_PRINTF(1, "CordbProcess - SetAllThreadsDebugState - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::Detach(void) {
  DEBUG_PRINTF(1, "CordbProcess - Detach - NOT IMPLEMENTED\n");
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
  DEBUG_PRINTF(1, "CordbProcess - CanCommitChanges - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbProcess::CommitChanges(
    /* [in] */ ULONG cSnapshots,
    /* [size_is][in] */ ICorDebugEditAndContinueSnapshot *pSnapshots[],
    /* [out] */ ICorDebugErrorInfoEnum **pError) {
  DEBUG_PRINTF(1, "CordbProcess - CommitChanges - NOT IMPLEMENTED\n");
  return S_OK;
}
