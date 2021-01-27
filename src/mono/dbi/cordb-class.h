// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-CLASS.H
//

#ifndef __MONO_DEBUGGER_CORDB_CLASS_H__
#define __MONO_DEBUGGER_CORDB_CLASS_H__

#include <cordb.h>

class CordbClass : public CordbBaseMono,
                   public ICorDebugClass,
                   public ICorDebugClass2 {
  mdToken token;

public:
  int module_id;
  CordbClass(Connection *conn, mdToken token, int module_id);
  HRESULT STDMETHODCALLTYPE GetModule(ICorDebugModule **pModule);
  HRESULT STDMETHODCALLTYPE GetToken(mdTypeDef *pTypeDef);
  HRESULT STDMETHODCALLTYPE GetStaticFieldValue(mdFieldDef fieldDef,
                                                ICorDebugFrame *pFrame,
                                                ICorDebugValue **ppValue);
  HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void **ppvObject);
  ULONG STDMETHODCALLTYPE AddRef(void);
  ULONG STDMETHODCALLTYPE Release(void);
  HRESULT STDMETHODCALLTYPE GetParameterizedType(CorElementType elementType,
                                                 ULONG32 nTypeArgs,
                                                 ICorDebugType *ppTypeArgs[],
                                                 ICorDebugType **ppType);
  HRESULT STDMETHODCALLTYPE SetJMCStatus(BOOL bIsJustMyCode);
};

#endif
