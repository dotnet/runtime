// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-STEPPER.H
//

#ifndef __MONO_DEBUGGER_CORDB_STEPPER_H__
#define __MONO_DEBUGGER_CORDB_STEPPER_H__

#include <cordb.h>

class CordbStepper : public CordbBaseMono,
                     public ICorDebugStepper,
                     public ICorDebugStepper2 {
  CordbThread *m_pThread;
  int m_commandId;

public:
  CordbStepper(Connection *conn, CordbThread *thread);
  ~CordbStepper();
  ULONG AddRef(void) { return (BaseAddRef()); }
  ULONG Release(void) { return (BaseRelease()); }
  const char *GetClassName() { return "CordbStepper"; }
  HRESULT IsActive(BOOL *pbActive);
  HRESULT Deactivate(void);
  HRESULT SetInterceptMask(CorDebugIntercept mask);
  HRESULT SetUnmappedStopMask(CorDebugUnmappedStop mask);
  HRESULT Step(BOOL bStepIn);
  HRESULT StepRange(BOOL bStepIn, COR_DEBUG_STEP_RANGE ranges[],
                    ULONG32 cRangeCount);
  HRESULT StepOut(void);
  HRESULT SetRangeIL(BOOL bIL);
  HRESULT QueryInterface(REFIID riid, void **ppvObject);

  HRESULT SetJMC(BOOL fIsJMCStepper);
};

#endif
