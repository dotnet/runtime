// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-BLOCKING-OBJ.H
//

#ifndef __MONO_DEBUGGER_CORDB_BLOCKING_OBJ_H__
#define __MONO_DEBUGGER_CORDB_BLOCKING_OBJ_H__

#include <cordb.h>

class CordbBlockingObjectEnum : public CordbBaseMono,
                                public ICorDebugBlockingObjectEnum {
public:
  CordbBlockingObjectEnum(Connection *conn);
  ULONG AddRef(void) { return (BaseAddRef()); }
  ULONG Release(void) { return (BaseRelease()); }
  const char *GetClassName() { return "CordbBlockingObjectEnum"; }
  HRESULT Next(ULONG celt, CorDebugBlockingObject values[],
               ULONG *pceltFetched);
  HRESULT Skip(ULONG celt);
  HRESULT Reset(void);
  HRESULT Clone(ICorDebugEnum **ppEnum);
  HRESULT GetCount(ULONG *pcelt);
  HRESULT QueryInterface(REFIID riid, void **ppvObject);
};

#endif
