// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-TYPE.CPP
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

CordbType::CordbType(CorElementType type, CordbClass *klass,
                     CordbType *typeParameter)
    : CordbBaseMono(conn) {
  this->klass = klass;
  this->type = type;
  this->typeParameter = typeParameter;
}

HRESULT STDMETHODCALLTYPE CordbType::GetType(CorElementType *ty) {
  *ty = type;
  DEBUG_PRINTF(1, "CordbType - GetType - IMPLEMENTED\n");
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbType::GetClass(ICorDebugClass **ppClass) {
  DEBUG_PRINTF(1, "CordbType - GetClass - IMPLEMENTED\n");
  if (!klass) {
    DEBUG_PRINTF(1, "CordbType - GetClass - SEM CLASSE\n");
    return S_OK;
  }
  *ppClass = static_cast<ICorDebugClass *>(klass);
  return S_OK;
}

HRESULT STDMETHODCALLTYPE
CordbType::EnumerateTypeParameters(ICorDebugTypeEnum **ppTyParEnum) {
  CordbTypeEnum *tp = new CordbTypeEnum(conn, typeParameter);
  *ppTyParEnum = static_cast<ICorDebugTypeEnum *>(tp);

  DEBUG_PRINTF(1, "CordbType - EnumerateTypeParameters - IMPLEMENTED\n");
  return S_OK;
}

HRESULT STDMETHODCALLTYPE
CordbType::GetFirstTypeParameter(ICorDebugType **value) {
  DEBUG_PRINTF(1, "CordbType - GetFirstTypeParameter - IMPLEMENTED\n");
  *value = static_cast<ICorDebugType *>(typeParameter);
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbType::GetBase(ICorDebugType **pBase) {
  DEBUG_PRINTF(1, "CordbType - GetBase - IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbType::GetStaticFieldValue(
    mdFieldDef fieldDef, ICorDebugFrame *pFrame, ICorDebugValue **ppValue) {
  DEBUG_PRINTF(1, "CordbType - GetStaticFieldValue - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbType::GetRank(ULONG32 *pnRank) {
  DEBUG_PRINTF(1, "CordbType - GetRank - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbType::QueryInterface(REFIID id,
                                                    void **pInterface) {
  if (id == IID_ICorDebugType)
    *pInterface = static_cast<ICorDebugType *>(this);
  else if (id == IID_ICorDebugType2)
    *pInterface = static_cast<ICorDebugType2 *>(this);
  else if (id == IID_IUnknown)
    *pInterface = static_cast<IUnknown *>(static_cast<ICorDebugType *>(this));
  else {
    *pInterface = NULL;
    return E_NOINTERFACE;
  }

  return S_OK;
}

ULONG STDMETHODCALLTYPE CordbType::AddRef(void) { return 0; }

ULONG STDMETHODCALLTYPE CordbType::Release(void) { return 0; }

HRESULT STDMETHODCALLTYPE CordbType::GetTypeID(COR_TYPEID *id) {
  DEBUG_PRINTF(1, "CordbType - GetTypeID - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

CordbTypeEnum::CordbTypeEnum(Connection *conn, CordbType *type)
    : CordbBaseMono(conn) {
  this->type = type;
}

HRESULT STDMETHODCALLTYPE CordbTypeEnum::Next(ULONG celt,
                                              ICorDebugType *values[],
                                              ULONG *pceltFetched) {
  *pceltFetched = celt;
  if (type != NULL)
    values[0] = type;
  DEBUG_PRINTF(1, "CordbTypeEnum - Next - IMPLEMENTED\n");
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbTypeEnum::Skip(ULONG celt) {
  DEBUG_PRINTF(1, "CordbTypeEnum - Skip - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbTypeEnum::Reset(void) {
  DEBUG_PRINTF(1, "CordbTypeEnum - Reset - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbTypeEnum::Clone(ICorDebugEnum **ppEnum) {
  DEBUG_PRINTF(1, "CordbTypeEnum - Clone - NOT IMPLEMENTED\n");
  return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbTypeEnum::GetCount(ULONG *pcelt) {
  if (type != NULL)
    *pcelt = 1;
  else
    *pcelt = 0;
  DEBUG_PRINTF(1, "CordbTypeEnum - GetCount - IMPLEMENTED - %d\n", *pcelt);
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbTypeEnum::QueryInterface(REFIID id,
                                                        void **pInterface) {
  if (id == IID_ICorDebugEnum)
    *pInterface = static_cast<ICorDebugEnum *>(this);
  else if (id == IID_ICorDebugTypeEnum)
    *pInterface = static_cast<ICorDebugTypeEnum *>(this);
  else if (id == IID_IUnknown)
    *pInterface =
        static_cast<IUnknown *>(static_cast<ICorDebugTypeEnum *>(this));
  else {
    DEBUG_PRINTF(1, "CordbTypeEnum - QueryInterface - E_NOTIMPL\n");
    *pInterface = NULL;
    return E_NOINTERFACE;
  }

  return S_OK;
}

ULONG STDMETHODCALLTYPE CordbTypeEnum::AddRef(void) { return 0; }

ULONG STDMETHODCALLTYPE CordbTypeEnum::Release(void) { return 0; }
