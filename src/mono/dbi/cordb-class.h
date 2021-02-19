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
  mdToken m_metadataToken;
  int m_debuggerId;

public:
  CordbClass(Connection *conn, mdToken token, int module_id);
  ULONG AddRef(void) { return (BaseAddRef()); }
  ULONG Release(void) { return (BaseRelease()); }
  const char *GetClassName() { return "CordbClass"; }
  HRESULT GetModule(ICorDebugModule **pModule);
  HRESULT GetToken(mdTypeDef *pTypeDef);
  HRESULT GetStaticFieldValue(mdFieldDef fieldDef, ICorDebugFrame *pFrame,
                              ICorDebugValue **ppValue);
  HRESULT QueryInterface(REFIID riid, void **ppvObject);

  HRESULT GetParameterizedType(CorElementType elementType, ULONG32 nTypeArgs,
                               ICorDebugType *ppTypeArgs[],
                               ICorDebugType **ppType);
  HRESULT SetJMCStatus(BOOL bIsJustMyCode);
};

#endif
