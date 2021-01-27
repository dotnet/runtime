// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-ASSEMBLY.CPP
//

#include <fstream>
#include <iostream>

#include <cordb-appdomain.h>
#include <cordb-assembly.h>
#include <cordb-class.h>
#include <cordb-function.h>
#include <cordb-process.h>
#include <cordb-symbol.h>
#include <cordb.h>

using namespace std;

CordbAssembly::CordbAssembly(Connection *conn, CordbProcess *process,
                             CordbAppDomain *appDomain, int id_assembly)
    : CordbBaseMono(conn) {
  pProcess = process;
  pAppDomain = appDomain;
  id = id_assembly;
}

HRESULT CordbAssembly::IsFullyTrusted(/* [out] */ BOOL *pbFullyTrusted) {
  *pbFullyTrusted = true;
  DEBUG_PRINTF(1, "CorDebugAssembly - IsFullyTrusted - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbAssembly::GetAppDomain(
    /* [out] */ ICorDebugAppDomain **ppAppDomain) {
  DEBUG_PRINTF(1, "CorDebugAssembly - GetAppDomain - IMPLEMENTED\n");
  *ppAppDomain = static_cast<ICorDebugAppDomain *>(pAppDomain);
  return S_OK;
}

HRESULT CordbAssembly::EnumerateModules(
    /* [out] */ ICorDebugModuleEnum **ppModules) {
  DEBUG_PRINTF(1, "CorDebugAssembly - EnumerateModules - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbAssembly::GetCodeBase(
    /* [in] */ ULONG32 cchName,
    /* [out] */ ULONG32 *pcchName,
    /* [length_is][size_is][out] */ WCHAR szName[]) {
  DEBUG_PRINTF(1, "CorDebugAssembly - GetCodeBase - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbAssembly::GetName(
    /* [in] */ ULONG32 cchName,
    /* [out] */ ULONG32 *pcchName,
    /* [length_is][size_is][out] */ WCHAR szName[]) {
  DEBUG_PRINTF(1, "CorDebugAssembly - GetName - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbAssembly::QueryInterface(
    /* [in] */ REFIID id,
    /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppInterface) {
  if (id == IID_ICorDebugAssembly)
    *ppInterface = static_cast<ICorDebugAssembly *>(this);
  else if (id == IID_ICorDebugAssembly2)
    *ppInterface = static_cast<ICorDebugAssembly2 *>(this);
  else if (id == IID_IUnknown)
    *ppInterface =
        static_cast<IUnknown *>(static_cast<ICorDebugAssembly *>(this));
  else {
    DEBUG_PRINTF(1, "CordbAssembly - QueryInterface - E_NOTIMPL\n");

    *ppInterface = NULL;
    return E_NOINTERFACE;
  }

  return S_OK;
}

ULONG CordbAssembly::AddRef(void) { return S_OK; }

ULONG CordbAssembly::Release(void) { return S_OK; }

CordbModule::CordbModule(Connection *conn, CordbProcess *process,
                         CordbAssembly *assembly, int id_assembly)
    : CordbBaseMono(conn) {
  pProcess = process;
  pCordbSymbol = NULL;
  pAssembly = assembly;
  id = id_assembly;
  dwFlags = 0;
}

HRESULT CordbModule::QueryInterface(REFIID id, void **pInterface) {
  if (id == IID_ICorDebugModule) {
    *pInterface = static_cast<ICorDebugModule *>(this);
  } else if (id == IID_ICorDebugModule2) {
    *pInterface = static_cast<ICorDebugModule2 *>(this);
  } else if (id == IID_ICorDebugModule3) {
    *pInterface = static_cast<ICorDebugModule3 *>(this);
  } else if (id == IID_ICorDebugModule4) {
    *pInterface = static_cast<ICorDebugModule4 *>(this);
  } else if (id == IID_IUnknown) {
    *pInterface = static_cast<IUnknown *>(static_cast<ICorDebugModule *>(this));
  } else {
    DEBUG_PRINTF(1, "CordbModule - QueryInterface - E_NOTIMPL\n");

    *pInterface = NULL;
    return E_NOINTERFACE;
  }
  return S_OK;
}

ULONG CordbModule::AddRef(void) { return S_OK; }

ULONG CordbModule::Release(void) { return S_OK; }

HRESULT CordbModule::IsMappedLayout(
    /* [out] */ BOOL *pIsMapped) {
  *pIsMapped = FALSE;
  DEBUG_PRINTF(1, "CordbModule - IsMappedLayout - IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbModule::CreateReaderForInMemorySymbols(
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ void **ppObj) {
  DEBUG_PRINTF(
      1, "CordbModule - CreateReaderForInMemorySymbols - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbModule::SetJMCStatus(
    /* [in] */ BOOL bIsJustMyCode,
    /* [in] */ ULONG32 cTokens,
    /* [size_is][in] */ mdToken pTokens[]) {
  DEBUG_PRINTF(1, "CordbModule - SetJMCStatus - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbModule::ApplyChanges(
    /* [in] */ ULONG cbMetadata,
    /* [size_is][in] */ BYTE pbMetadata[],
    /* [in] */ ULONG cbIL,
    /* [size_is][in] */ BYTE pbIL[]) {
  DEBUG_PRINTF(1, "CordbModule - ApplyChanges - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbModule::SetJITCompilerFlags(
    /* [in] */ DWORD dwFlags) {
  this->dwFlags = dwFlags;
  DEBUG_PRINTF(1, "CordbModule - SetJITCompilerFlags - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbModule::GetJITCompilerFlags(
    /* [out] */ DWORD *pdwFlags) {
  *pdwFlags = dwFlags;
  DEBUG_PRINTF(1, "CordbModule - GetJITCompilerFlags - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbModule::ResolveAssembly(
    /* [in] */ mdToken tkAssemblyRef,
    /* [out] */ ICorDebugAssembly **ppAssembly) {
  DEBUG_PRINTF(1, "CordbModule - ResolveAssembly - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbModule::GetProcess(
    /* [out] */ ICorDebugProcess **ppProcess) {
  DEBUG_PRINTF(1, "CordbModule - GetProcess - NOT IMPLEMENTED\n");
  // *ppProcess = pProcess;
  return S_OK;
}

HRESULT CordbModule::GetBaseAddress(
    /* [out] */ CORDB_ADDRESS *pAddress) {
  MdbgProtBuffer localbuf;
  m_dbgprot_buffer_init(&localbuf, 128);
  m_dbgprot_buffer_add_id(&localbuf, id);
  int cmdId = conn->send_event(MDBGPROT_CMD_SET_ASSEMBLY, MDBGPROT_CMD_ASSEMBLY_GET_METADATA_BLOB,
                               &localbuf);
  m_dbgprot_buffer_free(&localbuf);

  MdbgProtBuffer *bAnswer = conn->get_answer(cmdId);
  assembly_metadata_blob = m_dbgprot_decode_byte_array(
      bAnswer->buf, &bAnswer->buf, bAnswer->end, &assembly_metadata_len);

  DEBUG_PRINTF(1, "CordbModule - GetBaseAddress\n");

  *pAddress = (CORDB_ADDRESS)assembly_metadata_blob;
  return S_OK;
}

HRESULT CordbModule::GetName(
    /* [in] */ ULONG32 cchName,
    /* [out] */ ULONG32 *pcchName,
    /* [length_is][size_is][out] */ WCHAR szName[]) {
  DEBUG_PRINTF(1, "CordbModule - GetName - IMPLEMENTED\n");
  MdbgProtBuffer localbuf;
  m_dbgprot_buffer_init(&localbuf, 128);
  m_dbgprot_buffer_add_id(&localbuf, id);
  int cmdId =
      conn->send_event(MDBGPROT_CMD_SET_ASSEMBLY, MDBGPROT_CMD_ASSEMBLY_GET_LOCATION, &localbuf);
  m_dbgprot_buffer_free(&localbuf);

  MdbgProtBuffer *bAnswer = conn->get_answer(cmdId);
  char *assembly_name =
      m_dbgprot_decode_string(bAnswer->buf, &bAnswer->buf, bAnswer->end);

  DEBUG_PRINTF(1, "CordbModule - assembly_name - %d - %s\n", id, assembly_name);

  if (cchName < strlen(assembly_name) + 1) {
    *pcchName = strlen(assembly_name) + 1;
    g_free(assembly_name);
    return S_OK;
  }
  mbstowcs(szName, assembly_name, strlen(assembly_name) + 1);
  *pcchName = strlen(assembly_name) + 1;
  g_free(assembly_name);
  return S_OK;
}

HRESULT CordbModule::EnableJITDebugging(
    /* [in] */ BOOL bTrackJITInfo,
    /* [in] */ BOOL bAllowJitOpts) {
  DEBUG_PRINTF(1, "CordbModule - EnableJITDebugging - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbModule::EnableClassLoadCallbacks(
    /* [in] */ BOOL bClassLoadCallbacks) {
  DEBUG_PRINTF(1, "CordbModule - EnableClassLoadCallbacks - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbModule::GetFunctionFromToken(
    /* [in] */ mdMethodDef methodDef,
    /* [out] */ ICorDebugFunction **ppFunction) {
  // check in a cache before talk to mono runtime to get info
  DEBUG_PRINTF(1, "CordbModule - GetFunctionFromToken - IMPLEMENTED\n");
  MdbgProtBuffer localbuf;
  m_dbgprot_buffer_init(&localbuf, 128);
  m_dbgprot_buffer_add_id(&localbuf, id);
  m_dbgprot_buffer_add_int(&localbuf, methodDef);
  int cmdId = conn->send_event(MDBGPROT_CMD_SET_ASSEMBLY,
                               MDBGPROT_CMD_ASSEMBLY_GET_METHOD_FROM_TOKEN, &localbuf);
  m_dbgprot_buffer_free(&localbuf);

  MdbgProtBuffer *bAnswer = conn->get_answer(cmdId);
  int id = m_dbgprot_decode_id(bAnswer->buf, &bAnswer->buf, bAnswer->end);
  CordbFunction *func = NULL;
  func = pProcess->cordb->findFunction(id);
  if (func == NULL) {
    func = new CordbFunction(conn, methodDef, id, this);
    g_ptr_array_add(pProcess->cordb->functions, func);
  }
  *ppFunction = func;
  return S_OK;
}

HRESULT CordbModule::GetFunctionFromRVA(
    /* [in] */ CORDB_ADDRESS rva,
    /* [out] */ ICorDebugFunction **ppFunction) {
  DEBUG_PRINTF(1, "CordbModule - GetFunctionFromRVA - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbModule::GetClassFromToken(
    /* [in] */ mdTypeDef typeDef,
    /* [out] */ ICorDebugClass **ppClass) {
  CordbClass *pClass = new CordbClass(conn, typeDef, id);
  *ppClass = static_cast<ICorDebugClass *>(pClass);
  return S_OK;
}

HRESULT CordbModule::CreateBreakpoint(
    /* [out] */ ICorDebugModuleBreakpoint **ppBreakpoint) {
  DEBUG_PRINTF(1, "CordbModule - CreateBreakpoint - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbModule::GetEditAndContinueSnapshot(
    /* [out] */ ICorDebugEditAndContinueSnapshot **ppEditAndContinueSnapshot) {
  DEBUG_PRINTF(1,
               "CordbModule - GetEditAndContinueSnapshot - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbModule::GetMetaDataInterface(
    /* [in] */ REFIID riid,
    /* [out] */ IUnknown **ppObj) {
  if (pCordbSymbol == NULL)
    pCordbSymbol = new RegMeta(pAssembly, this);
  pCordbSymbol->QueryInterface(riid, (void **)ppObj);
  DEBUG_PRINTF(1, "CordbModule - GetMetaDataInterface - IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbModule::GetToken(
    /* [out] */ mdModule *pToken) {
  DEBUG_PRINTF(1, "CordbModule - GetToken - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbModule::IsDynamic(
    /* [out] */ BOOL *pDynamic) {
  DEBUG_PRINTF(1, "CordbModule - IsDynamic - IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbModule::GetGlobalVariableValue(
    /* [in] */ mdFieldDef fieldDef,
    /* [out] */ ICorDebugValue **ppValue) {
  DEBUG_PRINTF(1, "CordbModule - GetGlobalVariableValue - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT CordbModule::GetSize(
    /* [out] */ ULONG32 *pcBytes) {
  DEBUG_PRINTF(1, "CordbModule - GetSize -IMPLEMENTED\n");
  *pcBytes = assembly_metadata_len;
  return S_OK;
}

HRESULT CordbModule::IsInMemory(
    /* [out] */ BOOL *pInMemory) {
  DEBUG_PRINTF(1, "CordbModule - IsInMemory - IMPLEMENTED\n");
  return S_OK;
}
