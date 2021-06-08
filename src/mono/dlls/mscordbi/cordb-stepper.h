// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-STEPPER.H
//

#ifndef __MONO_DEBUGGER_CORDB_STEPPER_H__
#define __MONO_DEBUGGER_CORDB_STEPPER_H__

#include <cordb.h>

class CordbStepper : public CordbBaseMono, public ICorDebugStepper, public ICorDebugStepper2
{
    CordbThread* m_pThread;
    int          m_debuggerId;
    bool         m_bIsActive;

public:
    CordbStepper(Connection* conn, CordbThread* thread);
    ~CordbStepper();
    ULONG STDMETHODCALLTYPE AddRef(void)
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release(void)
    {
        return (BaseRelease());
    }
    const char* GetClassName()
    {
        return "CordbStepper";
    }
    HRESULT STDMETHODCALLTYPE IsActive(BOOL* pbActive);
    HRESULT STDMETHODCALLTYPE Deactivate(void);
    HRESULT STDMETHODCALLTYPE SetInterceptMask(CorDebugIntercept mask);
    HRESULT STDMETHODCALLTYPE SetUnmappedStopMask(CorDebugUnmappedStop mask);
    HRESULT STDMETHODCALLTYPE Step(BOOL bStepIn);
    HRESULT STDMETHODCALLTYPE StepRange(BOOL bStepIn, COR_DEBUG_STEP_RANGE ranges[], ULONG32 cRangeCount);
    HRESULT STDMETHODCALLTYPE StepOut(void);
    HRESULT STDMETHODCALLTYPE SetRangeIL(BOOL bIL);
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppvObject);
    HRESULT STDMETHODCALLTYPE SetJMC(BOOL fIsJMCStepper);
    int     GetDebuggerId() const
    {
        return m_debuggerId;
    }
};

#endif
