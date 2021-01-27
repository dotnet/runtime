// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-VALUE.CPP
//

#include <fstream>
#include <iostream>

#include <cordb-breakpoint.h>
#include <cordb-class.h>
#include <cordb-frame.h>
#include <cordb-function.h>
#include <cordb-stepper.h>
#include <cordb-thread.h>
#include <cordb-type.h>
#include <cordb-value.h>
#include <cordb.h>

using namespace std;

CordbValue::CordbValue(Connection *conn, CorElementType type,
                       CordbContent value, int size)
    : CordbBaseMono(conn) {
  this->type = type;
  this->value = value;
  this->size = size;
  this->conn = conn;
}

HRESULT STDMETHODCALLTYPE CordbValue::GetType(CorElementType *pType) {
  *pType = type;
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbValue::GetSize(ULONG32 *pSize) {
  *pSize = size;
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbValue::GetAddress(CORDB_ADDRESS *pAddress) {
  *pAddress = (CORDB_ADDRESS)&value;
  DEBUG_PRINTF(1, "CordbValue - GetAddress - IMPLEMENTED\n");
  return S_OK;
}

HRESULT STDMETHODCALLTYPE
CordbValue::CreateBreakpoint(ICorDebugValueBreakpoint **ppBreakpoint) {
  DEBUG_PRINTF(1, "CordbValue - CreateBreakpoint - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbValue::QueryInterface(REFIID id,
                                                     void **pInterface) {
  if (id == IID_ICorDebugValue) {
    *pInterface = static_cast<ICorDebugValue *>(
        static_cast<ICorDebugGenericValue *>(this));
  } else if (id == IID_ICorDebugValue2) {
    *pInterface = static_cast<ICorDebugValue2 *>(this);
  } else if (id == IID_ICorDebugValue3) {
    *pInterface = static_cast<ICorDebugValue3 *>(this);
  } else if (id == IID_ICorDebugGenericValue) {
    *pInterface = static_cast<ICorDebugGenericValue *>(this);
  } else if (id == IID_IUnknown) {
    *pInterface =
        static_cast<IUnknown *>(static_cast<ICorDebugGenericValue *>(this));
  } else {
    *pInterface = NULL;
    return E_NOINTERFACE;
  }
  return S_OK;
}

ULONG STDMETHODCALLTYPE CordbValue::AddRef(void) { return 0; }

ULONG STDMETHODCALLTYPE CordbValue::Release(void) { return 0; }

HRESULT STDMETHODCALLTYPE CordbValue::GetExactType(ICorDebugType **ppType) {
  DEBUG_PRINTF(1, "CordbValue - GetExactType - IMPLEMENTED\n");
  CordbType *tp = new CordbType(type);
  *ppType = static_cast<ICorDebugType *>(tp);
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbValue::GetSize64(ULONG64 *pSize) {
  DEBUG_PRINTF(1, "CordbValue - GetSize64 - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbValue::GetValue(void *pTo) {
  DEBUG_PRINTF(1, "CordbValue - GetValue - IMPLEMENTED\n");
  memcpy(pTo, &value, size);
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbValue::SetValue(void *pFrom) {
  memcpy(&value, pFrom, size);
  DEBUG_PRINTF(1, "CordbValue - SetValue - IMPLEMENTED\n");
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbReferenceValue::GetType(CorElementType *pType) {
  DEBUG_PRINTF(1, "CordbReferenceValue - GetType - IMPLEMENTED\n");
  *pType = type;
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbReferenceValue::GetSize(ULONG32 *pSize) {
  DEBUG_PRINTF(1, "CordbReferenceValue - GetSize - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbReferenceValue::GetAddress(CORDB_ADDRESS *pAddress) {
  *pAddress = (CORDB_ADDRESS)&object_id;
  DEBUG_PRINTF(1, "CordbReferenceValue - GetAddress - IMPLEMENTED\n");
  return S_OK;
}

HRESULT STDMETHODCALLTYPE
CordbReferenceValue::CreateBreakpoint(ICorDebugValueBreakpoint **ppBreakpoint) {
  DEBUG_PRINTF(1, "CordbReferenceValue - CreateBreakpoint - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbReferenceValue::QueryInterface(REFIID id, void **pInterface) {
  if (id == IID_ICorDebugValue) {
    *pInterface = static_cast<ICorDebugValue *>(
        static_cast<ICorDebugReferenceValue *>(this));
  } else if (id == IID_ICorDebugValue2) {
    *pInterface = static_cast<ICorDebugValue2 *>(this);
  } else if (id == IID_ICorDebugValue3) {
    *pInterface = static_cast<ICorDebugValue3 *>(this);
  } else if (id == IID_ICorDebugReferenceValue) {
    *pInterface = static_cast<ICorDebugReferenceValue *>(this);
  } else if (id == IID_IUnknown) {
    *pInterface =
        static_cast<IUnknown *>(static_cast<ICorDebugReferenceValue *>(this));
  } else {
    *pInterface = NULL;
    return E_NOINTERFACE;
  }
  return S_OK;
}

ULONG STDMETHODCALLTYPE CordbReferenceValue::AddRef(void) { return 1; }

ULONG STDMETHODCALLTYPE CordbReferenceValue::Release(void) { return 1; }

HRESULT STDMETHODCALLTYPE
CordbReferenceValue::GetExactType(ICorDebugType **ppType) {
  DEBUG_PRINTF(1, "CordbReferenceValue - GetExactType - IMPLEMENTED - %d\n",
               type);
  if (cordbtype) {
    DEBUG_PRINTF(1,
                 "CordbReferenceValue - GetExactType - IMPLEMENTED - %d - "
                 "tinha cordbtype\n",
                 type);
    *ppType = static_cast<ICorDebugType *>(cordbtype);
    return S_OK;
  }
  if (klass != NULL) {
    DEBUG_PRINTF(
        1,
        "CordbReferenceValue - GetExactType - IMPLEMENTED - %d - tinha klass\n",
        type);
    cordbtype = new CordbType(type, klass);
    *ppType = static_cast<ICorDebugType *>(cordbtype);
    return S_OK;
  }
  if (type == ELEMENT_TYPE_CLASS && object_id != -1) {
    MdbgProtBuffer localbuf;
    m_dbgprot_buffer_init(&localbuf, 128);
    m_dbgprot_buffer_add_id(&localbuf, object_id);

    int cmdId = conn->send_event(MDBGPROT_CMD_SET_OBJECT_REF, MDBGPROT_CMD_OBJECT_REF_GET_TYPE,
                                 &localbuf);
    m_dbgprot_buffer_free(&localbuf);
    MdbgProtBuffer *bAnswer = conn->get_answer(cmdId);
    int type_id = m_dbgprot_decode_int(bAnswer->buf, &bAnswer->buf, bAnswer->end);

    m_dbgprot_buffer_init(&localbuf, 128);
    m_dbgprot_buffer_add_id(&localbuf, type_id);

    cmdId = conn->send_event(MDBGPROT_CMD_SET_TYPE, MDBGPROT_CMD_TYPE_GET_INFO, &localbuf);
    m_dbgprot_buffer_free(&localbuf);
    bAnswer = conn->get_answer(cmdId);
    char *namespace_str =
        m_dbgprot_decode_string(bAnswer->buf, &bAnswer->buf, bAnswer->end);
    char *class_name_str =
        m_dbgprot_decode_string(bAnswer->buf, &bAnswer->buf, bAnswer->end);
    char *class_fullname_str =
        m_dbgprot_decode_string(bAnswer->buf, &bAnswer->buf, bAnswer->end);
    int assembly_id =
        m_dbgprot_decode_id(bAnswer->buf, &bAnswer->buf, bAnswer->end);
    int module_id = m_dbgprot_decode_id(bAnswer->buf, &bAnswer->buf, bAnswer->end);
    type_id = m_dbgprot_decode_id(bAnswer->buf, &bAnswer->buf, bAnswer->end);
    int type_id2 = m_dbgprot_decode_id(bAnswer->buf, &bAnswer->buf, bAnswer->end);
    int token = m_dbgprot_decode_int(bAnswer->buf, &bAnswer->buf, bAnswer->end);
    DEBUG_PRINTF(1,
                 "CordbReferenceValue - GetExactType - IMPLEMENTED - 1.0 - %d "
                 "- %d - %d - %d\n",
                 type_id, token, assembly_id, module_id);
    klass = new CordbClass(conn, token, assembly_id);
    cordbtype = new CordbType(type, klass);
    *ppType = static_cast<ICorDebugType *>(cordbtype);
    return S_OK;
  }
  if (type == ELEMENT_TYPE_SZARRAY && object_id != -1) {
    CordbClass *klass = NULL;
    MdbgProtBuffer localbuf;
    m_dbgprot_buffer_init(&localbuf, 128);
    m_dbgprot_buffer_add_id(&localbuf, object_id);

    int cmdId =
        conn->send_event(MDBGPROT_CMD_SET_ARRAY_REF, MDBGPROT_CMD_ARRAY_REF_GET_TYPE, &localbuf);
    m_dbgprot_buffer_free(&localbuf);
    MdbgProtBuffer *bAnswer = conn->get_answer(cmdId);
    int type_id = m_dbgprot_decode_byte(bAnswer->buf, &bAnswer->buf, bAnswer->end);
    DEBUG_PRINTF(1, "ELEMENT_TYPE_SZARRAY - %d\n", type_id);
    int rank = m_dbgprot_decode_int(bAnswer->buf, &bAnswer->buf, bAnswer->end);
    if (type_id == ELEMENT_TYPE_CLASS) {
      int klass_id =
          m_dbgprot_decode_int(bAnswer->buf, &bAnswer->buf, bAnswer->end);

      m_dbgprot_buffer_init(&localbuf, 128);
      m_dbgprot_buffer_add_id(&localbuf, klass_id);

      cmdId = conn->send_event(MDBGPROT_CMD_SET_TYPE, MDBGPROT_CMD_TYPE_GET_INFO, &localbuf);
      m_dbgprot_buffer_free(&localbuf);
      bAnswer = conn->get_answer(cmdId);
      char *namespace_str =
          m_dbgprot_decode_string(bAnswer->buf, &bAnswer->buf, bAnswer->end);
      char *class_name_str =
          m_dbgprot_decode_string(bAnswer->buf, &bAnswer->buf, bAnswer->end);
      char *class_fullname_str =
          m_dbgprot_decode_string(bAnswer->buf, &bAnswer->buf, bAnswer->end);
      int assembly_id =
          m_dbgprot_decode_id(bAnswer->buf, &bAnswer->buf, bAnswer->end);
      int module_id =
          m_dbgprot_decode_id(bAnswer->buf, &bAnswer->buf, bAnswer->end);
      int type_id3 = m_dbgprot_decode_id(bAnswer->buf, &bAnswer->buf, bAnswer->end);
      int type_id2 = m_dbgprot_decode_id(bAnswer->buf, &bAnswer->buf, bAnswer->end);
      int token = m_dbgprot_decode_int(bAnswer->buf, &bAnswer->buf, bAnswer->end);
      DEBUG_PRINTF(
          1,
          "CordbReferenceValue - GetExactType - IMPLEMENTED - 1.1 - %d - %d\n",
          klass_id, token);
      klass = new CordbClass(conn, token, module_id);
    }

    cordbtype = new CordbType(type, NULL,
                              new CordbType((CorElementType)type_id, klass));
    *ppType = static_cast<ICorDebugType *>(cordbtype);
    DEBUG_PRINTF(1, "CordbReferenceValue - GetExactType - IMPLEMENTED\n");

    return S_OK;
  }
  CordbType *tp = new CordbType(type);
  *ppType = static_cast<ICorDebugType *>(tp);
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbReferenceValue::GetSize64(ULONG64 *pSize) {
  DEBUG_PRINTF(1, "CordbReferenceValue - GetSize64 - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbReferenceValue::GetValue(void *pTo) {
  DEBUG_PRINTF(1, "CordbReferenceValue - GetValue - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbReferenceValue::SetValue(void *pFrom) {
  DEBUG_PRINTF(1, "CordbReferenceValue - SetValue - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbReferenceValue::IsNull(/* [out] */ BOOL *pbNull) {
  if (object_id == -1)
    *pbNull = true;
  DEBUG_PRINTF(1, "CordbReferenceValue - IsNull - IMPLEMENTED\n");
  return S_OK;
}

HRESULT STDMETHODCALLTYPE
CordbReferenceValue::GetValue(/* [out] */ CORDB_ADDRESS *pValue) {
  if (object_id == -1)
    *pValue = NULL;
  else
    *pValue = (CORDB_ADDRESS)&object_id;
  DEBUG_PRINTF(1, "CordbReferenceValue - GetValue - IMPLEMENTED\n");
  return S_OK;
}

HRESULT STDMETHODCALLTYPE
CordbReferenceValue::SetValue(/* [in] */ CORDB_ADDRESS value) {
  DEBUG_PRINTF(
      1, "CordbReferenceValue - SetValue CORDB_ADDRESS - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbReferenceValue::Dereference(/* [out] */ ICorDebugValue **ppValue) {
  if (object_id == -1)
    return CORDBG_E_BAD_REFERENCE_VALUE;
  if (type == ELEMENT_TYPE_SZARRAY || type == ELEMENT_TYPE_ARRAY) {
    CordbArrayValue *objectValue =
        new CordbArrayValue(conn, cordbtype, object_id, klass);
    objectValue->QueryInterface(IID_ICorDebugValue, (void **)ppValue);
  } else {
    CordbObjectValue *objectValue =
        new CordbObjectValue(conn, type, object_id, klass);
    objectValue->QueryInterface(IID_ICorDebugValue, (void **)ppValue);
  }
  DEBUG_PRINTF(1, "CordbReferenceValue - Dereference - IMPLEMENTED\n");
  return S_OK;
}

HRESULT STDMETHODCALLTYPE
CordbReferenceValue::DereferenceStrong(/* [out] */ ICorDebugValue **ppValue) {
  DEBUG_PRINTF(1,
               "CordbReferenceValue - DereferenceStrong - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

CordbReferenceValue::CordbReferenceValue(Connection *conn, CorElementType type,
                                         int object_id, CordbClass *klass,
                                         CordbType *cordbtype)
    : CordbBaseMono(conn) {
  this->type = type;
  this->object_id = object_id;
  this->conn = conn;
  this->klass = klass;
  this->cordbtype = cordbtype;
}

CordbObjectValue::CordbObjectValue(Connection *conn, CorElementType type,
                                   int object_id, CordbClass *klass)
    : CordbBaseMono(conn) {
  this->type = type;
  this->object_id = object_id;
  this->klass = klass;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetType(CorElementType *pType) {
  DEBUG_PRINTF(1, "CordbObjectValue - GetType - IMPLEMENTED\n");
  *pType = type;
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetSize(ULONG32 *pSize) {
  DEBUG_PRINTF(1, "CordbObjectValue - GetSize - NOT IMPLEMENTED\n");
  *pSize = 10;
  return S_OK;
}

HRESULT STDMETHODCALLTYPE
CordbObjectValue::GetAddress(CORDB_ADDRESS *pAddress) {
  *pAddress = (CORDB_ADDRESS)&object_id;
  DEBUG_PRINTF(1, "CordbObjectValue - GetAddress - IMPLEMENTED\n");
  return S_OK;
}

HRESULT STDMETHODCALLTYPE
CordbObjectValue::CreateBreakpoint(ICorDebugValueBreakpoint **ppBreakpoint) {
  DEBUG_PRINTF(1, "CordbObjectValue - CreateBreakpoint - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

ULONG STDMETHODCALLTYPE CordbObjectValue::AddRef(void) { return 1; }

ULONG STDMETHODCALLTYPE CordbObjectValue::Release(void) { return 1; }

HRESULT STDMETHODCALLTYPE
CordbObjectValue::GetExactType(ICorDebugType **ppType) {
  DEBUG_PRINTF(1, "CordbObjectValue - GetExactType - IMPLEMENTED\n");
  CordbType *tp = new CordbType(type, klass);
  *ppType = static_cast<ICorDebugType *>(tp);
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetSize64(ULONG64 *pSize) {
  DEBUG_PRINTF(1, "CordbObjectValue - GetSize64 - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetValue(void *pTo) {
  DEBUG_PRINTF(1, "CordbObjectValue - GetValue - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::SetValue(void *pFrom) {
  DEBUG_PRINTF(1, "CordbObjectValue - SetValue - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetVirtualMethodAndType(
    mdMemberRef memberRef, ICorDebugFunction **ppFunction,
    ICorDebugType **ppType) {
  DEBUG_PRINTF(
      1, "CordbObjectValue - GetVirtualMethodAndType - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetLength(ULONG32 *pcchString) {
  if (object_id == -1)
    return S_OK;
  if (type == ELEMENT_TYPE_STRING) {
    MdbgProtBuffer localbuf;
    m_dbgprot_buffer_init(&localbuf, 128);
    m_dbgprot_buffer_add_id(&localbuf, object_id);

    int cmdId = conn->send_event(MDBGPROT_CMD_SET_STRING_REF, MDBGPROT_CMD_STRING_REF_GET_LENGTH,
                                 &localbuf);
    m_dbgprot_buffer_free(&localbuf);
    MdbgProtBuffer *bAnswer = conn->get_answer(cmdId);
    *pcchString = m_dbgprot_decode_long(bAnswer->buf, &bAnswer->buf, bAnswer->end);
    return S_OK;
  }
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetString(ULONG32 cchString,
                                                      ULONG32 *pcchString,
                                                      WCHAR szString[]) {
  if (object_id == -1)
    return S_OK;
  if (type == ELEMENT_TYPE_STRING) {
    MdbgProtBuffer localbuf;
    m_dbgprot_buffer_init(&localbuf, 128);
    m_dbgprot_buffer_add_id(&localbuf, object_id);

    int cmdId = conn->send_event(MDBGPROT_CMD_SET_STRING_REF, MDBGPROT_CMD_STRING_REF_GET_VALUE,
                                 &localbuf);
    m_dbgprot_buffer_free(&localbuf);
    MdbgProtBuffer *bAnswer = conn->get_answer(cmdId);
    *pcchString = cchString;
    int use_utf16 =
        m_dbgprot_decode_byte(bAnswer->buf, &bAnswer->buf, bAnswer->end);
    if (use_utf16) {
      DEBUG_PRINTF(
          1, "CordbObjectValue - GetString - NOT IMPLEMENTED - use_utf16\n");
    } else {
      char *value =
          m_dbgprot_decode_string(bAnswer->buf, &bAnswer->buf, bAnswer->end);
      DEBUG_PRINTF(1, "CordbObjectValue - GetString - %s\n", value);
      if (cchString >= strlen(value)) {
        mbstowcs(szString, value, strlen(value) + 1);
        *pcchString = strlen(value);
      }
    }
    return S_OK;
  }
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::CreateHandle(
    CorDebugHandleType type, ICorDebugHandleValue **ppHandle) {
  DEBUG_PRINTF(1, "CordbObjectValue - CreateHandle - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetThreadOwningMonitorLock(
    ICorDebugThread **ppThread, DWORD *pAcquisitionCount) {
  DEBUG_PRINTF(
      1, "CordbObjectValue - GetThreadOwningMonitorLock - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbObjectValue::GetMonitorEventWaitList(ICorDebugThreadEnum **ppThreadEnum) {
  DEBUG_PRINTF(
      1, "CordbObjectValue - GetMonitorEventWaitList - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::EnumerateExceptionCallStack(
    ICorDebugExceptionObjectCallStackEnum **ppCallStackEnum) {
  DEBUG_PRINTF(
      1, "CordbObjectValue - EnumerateExceptionCallStack - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetCachedInterfaceTypes(
    BOOL bIInspectableOnly, ICorDebugTypeEnum **ppInterfacesEnum) {
  DEBUG_PRINTF(
      1, "CordbObjectValue - GetCachedInterfaceTypes - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetCachedInterfacePointers(
    BOOL bIInspectableOnly, ULONG32 celt, ULONG32 *pcEltFetched,
    CORDB_ADDRESS *ptrs) {
  DEBUG_PRINTF(
      1, "CordbObjectValue - GetCachedInterfacePointers - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbObjectValue::GetTarget(ICorDebugReferenceValue **ppObject) {
  DEBUG_PRINTF(1, "CordbObjectValue - GetTarget - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbObjectValue::GetFunction(ICorDebugFunction **ppFunction) {
  DEBUG_PRINTF(1, "CordbObjectValue - GetFunction - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbObjectValue::GetClass(/* [out] */ ICorDebugClass **ppClass) {
  DEBUG_PRINTF(1, "CordbObjectValue - GetClass - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetFieldValue(
    ICorDebugClass *pClass, mdFieldDef fieldDef, ICorDebugValue **ppValue) {

  DEBUG_PRINTF(1, "CordbObjectValue - GetFieldValue - IMPLEMENTED - %d - %d\n",
               fieldDef, object_id);
  if (object_id == -1)
    return S_FALSE;

  MdbgProtBuffer localbuf;
  m_dbgprot_buffer_init(&localbuf, 128);
  m_dbgprot_buffer_add_id(&localbuf, object_id);
  m_dbgprot_buffer_add_int(&localbuf, fieldDef);

  int cmdId = conn->send_event(MDBGPROT_CMD_SET_OBJECT_REF,
                               MDBGPROT_CMD_OBJECT_REF_GET_VALUES_ICORDBG, &localbuf);
  m_dbgprot_buffer_free(&localbuf);

  ReceivedReplyPacket *received_reply_packet =
      conn->get_answer_with_error(cmdId);
  CHECK_ERROR_RETURN_FALSE(received_reply_packet);
  MdbgProtBuffer *bAnswer = received_reply_packet->buf;

  return CreateCordbValue(conn, bAnswer, ppValue);
}

HRESULT CordbObjectValue::CreateCordbValue(Connection *conn, MdbgProtBuffer *bAnswer,
                                           ICorDebugValue **ppValue) {
  CorElementType type = (CorElementType)m_dbgprot_decode_byte(
      bAnswer->buf, &bAnswer->buf, bAnswer->end);

  DEBUG_PRINTF(1, "CreateCordbValue type - %x\n", type);
  CordbContent value;
  switch (type) {
  case MONO_TYPE_BOOLEAN:
  case MONO_TYPE_I1:
  case MONO_TYPE_U1:
    value.booleanValue =
        m_dbgprot_decode_int(bAnswer->buf, &bAnswer->buf, bAnswer->end);
    DEBUG_PRINTF(1, "bool value - %d\n", value.booleanValue);
    break;
  case MONO_TYPE_CHAR:
  case MONO_TYPE_I2:
  case MONO_TYPE_U2:
    value.charValue =
        m_dbgprot_decode_int(bAnswer->buf, &bAnswer->buf, bAnswer->end);
    DEBUG_PRINTF(1, "char value - %c\n", value.charValue);
    break;
  case MONO_TYPE_I4:
  case MONO_TYPE_U4:
  case MONO_TYPE_R4:
    value.intValue =
        m_dbgprot_decode_int(bAnswer->buf, &bAnswer->buf, bAnswer->end);
    DEBUG_PRINTF(1, "int value - %d\n", value.intValue);
    break;
  case MONO_TYPE_I8:
  case MONO_TYPE_U8:
  case MONO_TYPE_R8:
    value.longValue =
        m_dbgprot_decode_long(bAnswer->buf, &bAnswer->buf, bAnswer->end);
    break;
  case MONO_TYPE_CLASS:
  case MONO_TYPE_SZARRAY:
  case MONO_TYPE_STRING: {
    int object_id = m_dbgprot_decode_id(bAnswer->buf, &bAnswer->buf, bAnswer->end);
    CordbReferenceValue *refValue =
        new CordbReferenceValue(conn, type, object_id);
    refValue->QueryInterface(IID_ICorDebugValue, (void **)ppValue);
    return S_OK;
  }
  case MDBGPROT_VALUE_TYPE_ID_NULL: {
    CorElementType type = (CorElementType)m_dbgprot_decode_byte(
        bAnswer->buf, &bAnswer->buf, bAnswer->end);
    DEBUG_PRINTF(1, "NULL value - type - %d\n", type);
    if (type == MONO_TYPE_CLASS || type == MONO_TYPE_STRING) {
      int klass_id = (CorElementType)m_dbgprot_decode_id(bAnswer->buf, &bAnswer->buf,
                                               bAnswer->end);

      MdbgProtBuffer localbuf;
      m_dbgprot_buffer_init(&localbuf, 128);
      m_dbgprot_buffer_add_id(&localbuf, klass_id);
      int cmdId = conn->send_event(MDBGPROT_CMD_SET_TYPE, MDBGPROT_CMD_TYPE_GET_INFO, &localbuf);
      m_dbgprot_buffer_free(&localbuf);
      bAnswer = conn->get_answer(cmdId);
      char *namespace_str =
          m_dbgprot_decode_string(bAnswer->buf, &bAnswer->buf, bAnswer->end);
      char *class_name_str =
          m_dbgprot_decode_string(bAnswer->buf, &bAnswer->buf, bAnswer->end);
      char *class_fullname_str =
          m_dbgprot_decode_string(bAnswer->buf, &bAnswer->buf, bAnswer->end);
      int assembly_id =
          m_dbgprot_decode_id(bAnswer->buf, &bAnswer->buf, bAnswer->end);
      int module_id =
          m_dbgprot_decode_id(bAnswer->buf, &bAnswer->buf, bAnswer->end);
      int type_id = m_dbgprot_decode_id(bAnswer->buf, &bAnswer->buf, bAnswer->end);
      int type_id2 = m_dbgprot_decode_id(bAnswer->buf, &bAnswer->buf, bAnswer->end);
      int token = m_dbgprot_decode_int(bAnswer->buf, &bAnswer->buf, bAnswer->end);

      CordbClass *klass = new CordbClass(conn, token, module_id);
      CordbReferenceValue *refValue =
          new CordbReferenceValue(conn, type, -1, klass);
      refValue->QueryInterface(IID_ICorDebugValue, (void **)ppValue);
    }
    if (type == MONO_TYPE_SZARRAY) {
      DEBUG_PRINTF(1, "NULL value - MONO_TYPE_SZARRAY\n");
      CordbClass *klass = NULL;
      int type_id =
          m_dbgprot_decode_byte(bAnswer->buf, &bAnswer->buf, bAnswer->end);
      if (type_id == ELEMENT_TYPE_CLASS) {
        DEBUG_PRINTF(1,
                     "NULL value - MONO_TYPE_SZARRAY - ELEMENT_TYPE_CLASS\n");
        int klass_id =
            m_dbgprot_decode_int(bAnswer->buf, &bAnswer->buf, bAnswer->end);

        MdbgProtBuffer localbuf;
        m_dbgprot_buffer_init(&localbuf, 128);
        m_dbgprot_buffer_add_id(&localbuf, klass_id);

        int cmdId =
            conn->send_event(MDBGPROT_CMD_SET_TYPE, MDBGPROT_CMD_TYPE_GET_INFO, &localbuf);
        m_dbgprot_buffer_free(&localbuf);
        bAnswer = conn->get_answer(cmdId);
        char *namespace_str =
            m_dbgprot_decode_string(bAnswer->buf, &bAnswer->buf, bAnswer->end);
        char *class_name_str =
            m_dbgprot_decode_string(bAnswer->buf, &bAnswer->buf, bAnswer->end);
        char *class_fullname_str =
            m_dbgprot_decode_string(bAnswer->buf, &bAnswer->buf, bAnswer->end);
        int assembly_id =
            m_dbgprot_decode_id(bAnswer->buf, &bAnswer->buf, bAnswer->end);
        int module_id =
            m_dbgprot_decode_id(bAnswer->buf, &bAnswer->buf, bAnswer->end);
        int type_id3 =
            m_dbgprot_decode_id(bAnswer->buf, &bAnswer->buf, bAnswer->end);
        int type_id2 =
            m_dbgprot_decode_id(bAnswer->buf, &bAnswer->buf, bAnswer->end);
        int token = m_dbgprot_decode_int(bAnswer->buf, &bAnswer->buf, bAnswer->end);
        DEBUG_PRINTF(1, "CreateCordbValue - IMPLEMENTED - 1.1 - %d - %d\n",
                     klass_id, token);
        klass = new CordbClass(conn, token, module_id);
      }
      CordbType *cordbtype = new CordbType(
          type, NULL, new CordbType((CorElementType)type_id, klass));
      CordbReferenceValue *refValue =
          new CordbReferenceValue(conn, type, -1, klass, cordbtype);
      refValue->QueryInterface(IID_ICorDebugValue, (void **)ppValue);
    }
    return S_OK;
  }
  default:
    DEBUG_PRINTF(1, "default value - %d\n", type);
    return S_FALSE;
  }

  *ppValue = new CordbValue(conn, type, value,
                            convert_mono_type_2_icordbg_size(type)); //
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::GetVirtualMethod(
    mdMemberRef memberRef, ICorDebugFunction **ppFunction) {
  DEBUG_PRINTF(1, "CordbObjectValue - GetVirtualMethod - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbObjectValue::GetContext(ICorDebugContext **ppContext) {
  DEBUG_PRINTF(1, "CordbObjectValue - GetContext - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::IsValueClass(BOOL *pbIsValueClass) {
  DEBUG_PRINTF(1, "CordbObjectValue - IsValueClass - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbObjectValue::GetManagedCopy(IUnknown **ppObject) {
  DEBUG_PRINTF(1, "CordbObjectValue - GetManagedCopy - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbObjectValue::SetFromManagedCopy(IUnknown *pObject) {
  DEBUG_PRINTF(1, "CordbObjectValue - SetFromManagedCopy - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::IsValid(BOOL *pbValid) {
  DEBUG_PRINTF(1, "CordbObjectValue - IsValid - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::CreateRelocBreakpoint(
    ICorDebugValueBreakpoint **ppBreakpoint) {
  DEBUG_PRINTF(1,
               "CordbObjectValue - CreateRelocBreakpoint - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbObjectValue::QueryInterface(REFIID id,
                                                           void **pInterface) {
  DEBUG_PRINTF(1, "CordbObjectValue - QueryInterface - IMPLEMENTED - %d\n",
               type);
  if (id == IID_ICorDebugValue) {
    DEBUG_PRINTF(1, "CordbObjectValue - QueryInterface - IID_ICorDebugValue - "
                    "IMPLEMENTED\n");
    *pInterface = static_cast<ICorDebugValue *>(
        static_cast<ICorDebugObjectValue *>(this));
  } else if (id == IID_ICorDebugValue2) {
    DEBUG_PRINTF(1, "CordbObjectValue - QueryInterface - IID_ICorDebugValue2 - "
                    "IMPLEMENTED\n");
    *pInterface = static_cast<ICorDebugValue2 *>(this);
  } else if (id == IID_ICorDebugValue3) {
    DEBUG_PRINTF(1, "CordbObjectValue - QueryInterface - IID_ICorDebugValue3 - "
                    "IMPLEMENTED\n");
    *pInterface = static_cast<ICorDebugValue3 *>(this);
  } else if (id == IID_ICorDebugObjectValue) {
    DEBUG_PRINTF(1, "CordbObjectValue - QueryInterface - "
                    "IID_ICorDebugObjectValue - IMPLEMENTED\n");
    *pInterface = static_cast<ICorDebugObjectValue *>(this);
  } else if (id == IID_ICorDebugObjectValue2) {
    DEBUG_PRINTF(1, "CordbObjectValue - QueryInterface - "
                    "IID_ICorDebugObjectValue2 - IMPLEMENTED\n");
    *pInterface = static_cast<ICorDebugObjectValue2 *>(this);
  } else if (id == IID_ICorDebugGenericValue) {
    DEBUG_PRINTF(1, "CordbObjectValue - QueryInterface - "
                    "IID_ICorDebugGenericValue - IMPLEMENTED\n");
    *pInterface = static_cast<ICorDebugGenericValue *>(this);
  } else if (id == IID_ICorDebugHeapValue) {
    DEBUG_PRINTF(1, "CordbObjectValue - QueryInterface - "
                    "IID_ICorDebugHeapValue - IMPLEMENTED\n");
    *pInterface = static_cast<ICorDebugHeapValue *>(this);
  } else if (id == IID_ICorDebugHeapValue2) {
    DEBUG_PRINTF(1, "CordbObjectValue - QueryInterface - "
                    "IID_ICorDebugHeapValue2 - IMPLEMENTED\n");
    *pInterface = static_cast<ICorDebugHeapValue2 *>(this);
  } else if (id == IID_ICorDebugHeapValue3) {
    DEBUG_PRINTF(1, "CordbObjectValue - QueryInterface - "
                    "IID_ICorDebugHeapValue3 - IMPLEMENTED\n");
    *pInterface = static_cast<ICorDebugHeapValue3 *>(this);
  } else if ((id == IID_ICorDebugStringValue) &&
             (type == ELEMENT_TYPE_STRING)) {
    DEBUG_PRINTF(1, "CordbObjectValue - QueryInterface - "
                    "IID_ICorDebugStringValue - IMPLEMENTED\n");
    *pInterface = static_cast<ICorDebugStringValue *>(this);
  } else /*if (id == IID_ICorDebugExceptionObjectValue && m_fIsExceptionObject)
        {
                *pInterface =
        static_cast<IUnknown*>(static_cast<ICorDebugExceptionObjectValue*>(this));
        }
        else if (id == IID_ICorDebugComObjectValue && m_fIsRcw)
        {
                *pInterface = static_cast<ICorDebugComObjectValue*>(this);
        }
        else if (id == IID_ICorDebugDelegateObjectValue && m_fIsDelegate)
        {
                *pInterface = static_cast<ICorDebugDelegateObjectValue*>(this);
        }
        else*/
      if (id == IID_IUnknown) {
    DEBUG_PRINTF(
        1, "CordbObjectValue - QueryInterface - IID_IUnknown - IMPLEMENTED\n");
    *pInterface =
        static_cast<IUnknown *>(static_cast<ICorDebugObjectValue *>(this));
  } else {
    *pInterface = NULL;
    return E_NOINTERFACE;
  }
  return S_OK;
}

CordbArrayValue::CordbArrayValue(Connection *conn, CordbType *type,
                                 int object_id, CordbClass *klass)
    : CordbBaseMono(conn) {
  this->type = type;
  this->object_id = object_id;
  this->klass = klass;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetClass(ICorDebugClass **ppClass) {
  DEBUG_PRINTF(1, "CordbArrayValue - GetClass - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetFieldValue(
    ICorDebugClass *pClass, mdFieldDef fieldDef, ICorDebugValue **ppValue) {
  DEBUG_PRINTF(1, "CordbArrayValue - GetFieldValue - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetVirtualMethod(
    mdMemberRef memberRef, ICorDebugFunction **ppFunction) {
  DEBUG_PRINTF(1, "CordbArrayValue - GetVirtualMethod - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbArrayValue::GetContext(ICorDebugContext **ppContext) {
  DEBUG_PRINTF(1, "CordbArrayValue - GetContext - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::IsValueClass(BOOL *pbIsValueClass) {
  DEBUG_PRINTF(1, "CordbArrayValue - IsValueClass - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetManagedCopy(IUnknown **ppObject) {
  DEBUG_PRINTF(1, "CordbArrayValue - GetManagedCopy - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbArrayValue::SetFromManagedCopy(IUnknown *pObject) {
  DEBUG_PRINTF(1, "CordbArrayValue - SetFromManagedCopy - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetType(CorElementType *pType) {
  DEBUG_PRINTF(1, "CordbArrayValue - GetType - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetSize(ULONG32 *pSize) {
  DEBUG_PRINTF(1, "CordbArrayValue - GetSize - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetAddress(CORDB_ADDRESS *pAddress) {
  *pAddress = (CORDB_ADDRESS)&object_id;
  DEBUG_PRINTF(1, "CordbArrayValue - GetAddress - IMPLEMENTED\n");
  return S_OK;
}

HRESULT STDMETHODCALLTYPE
CordbArrayValue::CreateBreakpoint(ICorDebugValueBreakpoint **ppBreakpoint) {
  DEBUG_PRINTF(1, "CordbArrayValue - CreateBreakpoint - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::QueryInterface(REFIID id,
                                                          void **pInterface) {
  if (id == IID_ICorDebugValue) {
    *pInterface =
        static_cast<ICorDebugValue *>(static_cast<ICorDebugArrayValue *>(this));
  } else if (id == IID_ICorDebugValue2) {
    *pInterface = static_cast<ICorDebugValue2 *>(this);
  } else if (id == IID_ICorDebugValue3) {
    *pInterface = static_cast<ICorDebugValue3 *>(this);
  } else if (id == IID_ICorDebugArrayValue) {
    *pInterface = static_cast<ICorDebugArrayValue *>(this);
  } else if (id == IID_ICorDebugGenericValue) {
    *pInterface = static_cast<ICorDebugGenericValue *>(this);
  } else if (id == IID_ICorDebugHeapValue2) {
    *pInterface = static_cast<ICorDebugHeapValue2 *>(this);
  } else if (id == IID_ICorDebugHeapValue3) {
    *pInterface = static_cast<ICorDebugHeapValue3 *>(this);
  } else if (id == IID_IUnknown) {
    *pInterface =
        static_cast<IUnknown *>(static_cast<ICorDebugArrayValue *>(this));
  } else {
    *pInterface = NULL;
    return E_NOINTERFACE;
  }

  return S_OK;
}

ULONG STDMETHODCALLTYPE CordbArrayValue::AddRef(void) { return 0; }

ULONG STDMETHODCALLTYPE CordbArrayValue::Release(void) { return 0; }

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetVirtualMethodAndType(
    mdMemberRef memberRef, ICorDebugFunction **ppFunction,
    ICorDebugType **ppType) {
  DEBUG_PRINTF(1,
               "CordbArrayValue - GetVirtualMethodAndType - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetValue(void *pTo) {
  DEBUG_PRINTF(1, "CordbArrayValue - GetValue - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::SetValue(void *pFrom) {
  DEBUG_PRINTF(1, "CordbArrayValue - SetValue - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetLength(ULONG32 *pcchString) {
  DEBUG_PRINTF(1, "CordbArrayValue - GetLength - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetString(ULONG32 cchString,
                                                     ULONG32 *pcchString,
                                                     WCHAR szString[]) {
  DEBUG_PRINTF(1, "CordbArrayValue - GetString - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::IsValid(BOOL *pbValid) {
  DEBUG_PRINTF(1, "CordbArrayValue - IsValid - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::CreateRelocBreakpoint(
    ICorDebugValueBreakpoint **ppBreakpoint) {
  DEBUG_PRINTF(1,
               "CordbArrayValue - CreateRelocBreakpoint - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbArrayValue::GetExactType(ICorDebugType **ppType) {
  *ppType = static_cast<ICorDebugType *>(type);
  DEBUG_PRINTF(1, "CordbArrayValue - GetExactType - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetSize64(ULONG64 *pSize) {
  DEBUG_PRINTF(1, "CordbArrayValue - GetSize64 - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::CreateHandle(
    CorDebugHandleType type, ICorDebugHandleValue **ppHandle) {
  DEBUG_PRINTF(1, "CordbArrayValue - CreateHandle - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetThreadOwningMonitorLock(
    ICorDebugThread **ppThread, DWORD *pAcquisitionCount) {
  DEBUG_PRINTF(
      1, "CordbArrayValue - GetThreadOwningMonitorLock - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbArrayValue::GetMonitorEventWaitList(ICorDebugThreadEnum **ppThreadEnum) {
  DEBUG_PRINTF(1,
               "CordbArrayValue - GetMonitorEventWaitList - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::EnumerateExceptionCallStack(
    ICorDebugExceptionObjectCallStackEnum **ppCallStackEnum) {
  DEBUG_PRINTF(
      1, "CordbArrayValue - EnumerateExceptionCallStack - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetCachedInterfaceTypes(
    BOOL bIInspectableOnly, ICorDebugTypeEnum **ppInterfacesEnum) {
  DEBUG_PRINTF(1,
               "CordbArrayValue - GetCachedInterfaceTypes - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetCachedInterfacePointers(
    BOOL bIInspectableOnly, ULONG32 celt, ULONG32 *pcEltFetched,
    CORDB_ADDRESS *ptrs) {
  DEBUG_PRINTF(
      1, "CordbArrayValue - GetCachedInterfacePointers - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbArrayValue::GetTarget(ICorDebugReferenceValue **ppObject) {
  DEBUG_PRINTF(1, "CordbArrayValue - GetTarget - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbArrayValue::GetFunction(ICorDebugFunction **ppFunction) {
  DEBUG_PRINTF(1, "CordbArrayValue - GetFunction - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
CordbArrayValue::GetElementType(CorElementType *pType) {
  DEBUG_PRINTF(1, "CordbArrayValue - GetElementType - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetRank(ULONG32 *pnRank) {
  MdbgProtBuffer localbuf;
  m_dbgprot_buffer_init(&localbuf, 128);
  m_dbgprot_buffer_add_id(&localbuf, object_id);

  int cmdId =
      conn->send_event(MDBGPROT_CMD_SET_ARRAY_REF, MDBGPROT_CMD_ARRAY_REF_GET_LENGTH, &localbuf);
  m_dbgprot_buffer_free(&localbuf);
  MdbgProtBuffer *bAnswer = conn->get_answer(cmdId);
  int rank = m_dbgprot_decode_int(bAnswer->buf, &bAnswer->buf, bAnswer->end);
  DEBUG_PRINTF(1, "CordbArrayValue - GetRank - IMPLEMENTED - %d\n", rank);
  *pnRank = rank;
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetCount(ULONG32 *pnCount) {
  MdbgProtBuffer localbuf;
  m_dbgprot_buffer_init(&localbuf, 128);
  m_dbgprot_buffer_add_id(&localbuf, object_id);

  int cmdId =
      conn->send_event(MDBGPROT_CMD_SET_ARRAY_REF, MDBGPROT_CMD_ARRAY_REF_GET_LENGTH, &localbuf);
  m_dbgprot_buffer_free(&localbuf);
  MdbgProtBuffer *bAnswer = conn->get_answer(cmdId);
  int rank = m_dbgprot_decode_int(bAnswer->buf, &bAnswer->buf, bAnswer->end);
  count = m_dbgprot_decode_int(bAnswer->buf, &bAnswer->buf, bAnswer->end);
  DEBUG_PRINTF(1, "CordbArrayValue - GetCount - IMPLEMENTED - %d\n", count);
  *pnCount = count;
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetDimensions(ULONG32 cdim,
                                                         ULONG32 dims[]) {
  DEBUG_PRINTF(1, "CordbArrayValue - GetDimensions - IMPLEMENTED\n");
  dims[0] = count;
  return S_OK;
}

HRESULT STDMETHODCALLTYPE
CordbArrayValue::HasBaseIndicies(BOOL *pbHasBaseIndicies) {
  DEBUG_PRINTF(1, "CordbArrayValue - HasBaseIndicies - NOT IMPLEMENTED\n");
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetBaseIndicies(ULONG32 cdim,
                                                           ULONG32 indicies[]) {
  DEBUG_PRINTF(1, "CordbArrayValue - GetBaseIndicies - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetElement(
    ULONG32 cdim, ULONG32 indices[], ICorDebugValue **ppValue) {
  DEBUG_PRINTF(1, "CordbArrayValue - GetElement - IMPLEMENTED - %d - %d\n",
               cdim, indices[0]);

  MdbgProtBuffer localbuf;
  m_dbgprot_buffer_init(&localbuf, 128);
  m_dbgprot_buffer_add_id(&localbuf, object_id);
  m_dbgprot_buffer_add_int(&localbuf, indices[cdim - 1]);
  m_dbgprot_buffer_add_int(&localbuf, 1);

  int cmdId =
      conn->send_event(MDBGPROT_CMD_SET_ARRAY_REF, MDBGPROT_CMD_ARRAY_REF_GET_VALUES, &localbuf);
  m_dbgprot_buffer_free(&localbuf);
  MdbgProtBuffer *bAnswer = conn->get_answer(cmdId);
  CordbObjectValue::CreateCordbValue(conn, bAnswer, ppValue);
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbArrayValue::GetElementAtPosition(
    ULONG32 nPosition, ICorDebugValue **ppValue) {
  DEBUG_PRINTF(1, "CordbArrayValue - GetElementAtPosition - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}
