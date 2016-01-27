// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// File: breakpoint.cpp
// 

//
//*****************************************************************************
#include "stdafx.h"

/* ------------------------------------------------------------------------- *
 * Breakpoint class
 * ------------------------------------------------------------------------- */

CordbBreakpoint::CordbBreakpoint(CordbProcess * pProcess, CordbBreakpointType bpType)
  : CordbBase(pProcess, 0, enumCordbBreakpoint), 
  m_active(false), m_type(bpType)
{
}

// Neutered by CordbAppDomain
void CordbBreakpoint::Neuter()
{
    m_pAppDomain = NULL; // clear ref
    CordbBase::Neuter();
}

HRESULT CordbBreakpoint::QueryInterface(REFIID id, void **pInterface)
{
    if (id == IID_ICorDebugBreakpoint)
    {
        *pInterface = static_cast<ICorDebugBreakpoint*>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown *>(static_cast<ICorDebugBreakpoint*>(this));
    }
    else
    {
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
}

HRESULT CordbBreakpoint::BaseIsActive(BOOL *pbActive)
{
    *pbActive = m_active ? TRUE : FALSE;

    return S_OK;
}

/* ------------------------------------------------------------------------- *
 * Function Breakpoint class
 * ------------------------------------------------------------------------- */

CordbFunctionBreakpoint::CordbFunctionBreakpoint(CordbCode *code,
                                                 SIZE_T offset)
  : CordbBreakpoint(code->GetProcess(), CBT_FUNCTION), 
  m_code(code), m_offset(offset)
{
    // Remember the app domain we came from so that breakpoints can be
    // deactivated from within the ExitAppdomain callback.
    m_pAppDomain = m_code->GetAppDomain();
    _ASSERTE(m_pAppDomain != NULL);
}

CordbFunctionBreakpoint::~CordbFunctionBreakpoint()
{
    // @todo- eventually get CordbFunctionBreakpoint rooted and enable this.
    //_ASSERTE(this->IsNeutered());
    //_ASSERTE(m_code == NULL);
}

void CordbFunctionBreakpoint::Neuter()
{
    Disconnect();
    CordbBreakpoint::Neuter();
}

HRESULT CordbFunctionBreakpoint::QueryInterface(REFIID id, void **pInterface)
{
    if (id == IID_ICorDebugFunctionBreakpoint)
    {
        *pInterface = static_cast<ICorDebugFunctionBreakpoint*>(this);
    }
    else
    {
        // Not looking for a function breakpoint? See if the base class handles
        // this interface. (issue 143976)
        return CordbBreakpoint::QueryInterface(id, pInterface);
    }

    ExternalAddRef();
    return S_OK;
}

HRESULT CordbFunctionBreakpoint::GetFunction(ICorDebugFunction **ppFunction)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppFunction, ICorDebugFunction **);

    if (m_code == NULL)
    {
        return CORDBG_E_PROCESS_TERMINATED;
    }        
    if (m_code->IsNeutered())
    {
        return CORDBG_E_CODE_NOT_AVAILABLE;
    }

    *ppFunction = static_cast<ICorDebugFunction *> (m_code->GetFunction());
    (*ppFunction)->AddRef();

    return S_OK;
}

// m_id is actually a LSPTR_BREAKPOINT. Get it as a type-safe member.
LSPTR_BREAKPOINT CordbFunctionBreakpoint::GetLsPtrBP()
{
    LSPTR_BREAKPOINT p;
    p.Set((void*) m_id);
    return p;
}

HRESULT CordbFunctionBreakpoint::GetOffset(ULONG32 *pnOffset)
{
  //REVISIT_TODO: is this casting correct for ia64?
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pnOffset, SIZE_T *);
    
    *pnOffset = (ULONG32)m_offset;

    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// Activates or removes a breakpoint 
//
// Arguments:
//    fActivate - TRUE if to activate the breakpoint, else FALSE.
//
// Return Value:
//    S_OK if successful, else a specific error code detailing the type of failure.
//
//---------------------------------------------------------------------------------------
HRESULT CordbFunctionBreakpoint::Activate(BOOL fActivate)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    OK_IF_NEUTERED(this); // we'll check again later

    if (fActivate == (m_active == true) )
    {
        return S_OK;
    }

    // For backwards compat w/ everett, we let the other error codes
    // take precedence over neutering error codes.
    if ((m_code == NULL) || this->IsNeutered())
    {
        return CORDBG_E_PROCESS_TERMINATED;
    }

    HRESULT hr;
    ATT_ALLOW_LIVE_DO_STOPGO(GetProcess());

    // For legacy, check this error condition. We must do this under the stop-go lock to ensure
    // that the m_code object was not deleted out from underneath us.
    //
    // 6/23/09 - This isn't just for legacy anymore, collectible types should be able to hit this
    // by unloading the module containing the code this breakpoint is bound to.
    if (m_code->IsNeutered())
    {
        return CORDBG_E_CODE_NOT_AVAILABLE;
    }

        
    //
    // <REVISIT_TODO>@todo: when we implement module and value breakpoints, then
    // we'll want to factor some of this code out.</REVISIT_TODO>
    //
    CordbProcess * pProcess = GetProcess();

    RSLockHolder lockHolder(pProcess->GetProcessLock());
    pProcess->ClearPatchTable(); // if we add something, then the right side 
                                // view of the patch table is no longer valid

    DebuggerIPCEvent * pEvent = (DebuggerIPCEvent *) _alloca(CorDBIPC_BUFFER_SIZE);

    CordbAppDomain * pAppDomain = GetAppDomain();
    _ASSERTE (pAppDomain != NULL);

    if (fActivate)
    {
        pProcess->InitIPCEvent(pEvent, DB_IPCE_BREAKPOINT_ADD, true, pAppDomain->GetADToken());

        pEvent->BreakpointData.funcMetadataToken = m_code->GetMetadataToken();
        pEvent->BreakpointData.vmDomainFile = m_code->GetModule()->GetRuntimeDomainFile();
        pEvent->BreakpointData.encVersion = m_code->GetVersion();

        BOOL fIsIL = m_code->IsIL();

        pEvent->BreakpointData.isIL = fIsIL ? true : false;
        pEvent->BreakpointData.offset = m_offset;
        if (fIsIL)
        {
            pEvent->BreakpointData.nativeCodeMethodDescToken = pEvent->BreakpointData.nativeCodeMethodDescToken.NullPtr();
        }
        else
        {
            pEvent->BreakpointData.nativeCodeMethodDescToken = 
                (m_code.GetValue()->AsNativeCode())->GetVMNativeCodeMethodDescToken().ToLsPtr();
        }

        // Note: we're sending a two-way event, so it blocks here
        // until the breakpoint is really added and the reply event is
        // copied over the event we sent.
        lockHolder.Release();
        hr = pProcess->SendIPCEvent(pEvent, CorDBIPC_BUFFER_SIZE);
        lockHolder.Acquire();

        hr = WORST_HR(hr, pEvent->hr);

        if (FAILED(hr))
        {
            return hr;
        }

            
        m_id = LsPtrToCookie(pEvent->BreakpointData.breakpointToken);

        // If we weren't able to allocate the BP, we should have set the
        // hr on the left side.
        _ASSERTE(m_id != 0);


        pAppDomain->m_breakpoints.AddBase(this);
        m_active = true;

        // Continue called automatically by StopContinueHolder
    }
    else
    {
        _ASSERTE (pAppDomain != NULL);

        if (pProcess->IsSafeToSendEvents())
        {            
            pProcess->InitIPCEvent(pEvent, DB_IPCE_BREAKPOINT_REMOVE, false, pAppDomain->GetADToken());

            pEvent->BreakpointData.breakpointToken = GetLsPtrBP(); 

            lockHolder.Release();
            hr = pProcess->SendIPCEvent(pEvent, CorDBIPC_BUFFER_SIZE);            
            lockHolder.Acquire();

            hr = WORST_HR(hr, pEvent->hr);
        }
        else
        {
            hr = CORDBHRFromProcessState(pProcess, pAppDomain);
        }            
        
        pAppDomain->m_breakpoints.RemoveBase(LsPtrToCookie(GetLsPtrBP()));
        m_active = false;
    }

    return hr;
}

void CordbFunctionBreakpoint::Disconnect()
{
    m_code.Clear();
}

/* ------------------------------------------------------------------------- *
 * Stepper class
 * ------------------------------------------------------------------------- */

CordbStepper::CordbStepper(CordbThread *thread, CordbFrame *frame)
  : CordbBase(thread->GetProcess(), 0, enumCordbStepper), 
    m_thread(thread), m_frame(frame),
    m_stepperToken(0), m_active(false),
    m_rangeIL(TRUE),
    m_fIsJMCStepper(false),
    m_rgfMappingStop(STOP_OTHER_UNMAPPED),
    m_rgfInterceptStop(INTERCEPT_NONE)
{
}

HRESULT CordbStepper::QueryInterface(REFIID id, void **pInterface)
{
    if (id == IID_ICorDebugStepper)
        *pInterface = static_cast<ICorDebugStepper *>(this);
    else if (id == IID_ICorDebugStepper2)
        *pInterface = static_cast<ICorDebugStepper2 *>(this);
    else if (id == IID_IUnknown)
        *pInterface = static_cast<IUnknown *>(static_cast<ICorDebugStepper *>(this));
    else
        return E_NOINTERFACE;

    ExternalAddRef();
    return S_OK;
}

HRESULT CordbStepper::SetRangeIL(BOOL bIL)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    m_rangeIL = (bIL != FALSE);

    return S_OK;
}

HRESULT CordbStepper::SetJMC(BOOL fIsJMCStepper)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    // Can't have JMC and stopping with anything else.
    if (m_rgfMappingStop & STOP_ALL)
        return E_INVALIDARG;
            
    m_fIsJMCStepper = (fIsJMCStepper != FALSE);
    return S_OK;
}

HRESULT CordbStepper::IsActive(BOOL *pbActive)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pbActive, BOOL *);
    
    *pbActive = m_active;

    return S_OK;
}

// M_id is a ptr to the stepper in the LS process.
LSPTR_STEPPER CordbStepper::GetLsPtrStepper()
{
    LSPTR_STEPPER p;
    p.Set((void*) m_id);
    return p;
}

HRESULT CordbStepper::Deactivate()
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    if (!m_active)
        return S_OK;
        
    FAIL_IF_NEUTERED(this);

    if (m_thread == NULL)
        return CORDBG_E_PROCESS_TERMINATED;

    HRESULT hr;
    CordbProcess *process = GetProcess();
    ATT_ALLOW_LIVE_DO_STOPGO(process);
    
    process->Lock();

    if (!m_active) // another thread may be deactivating (e.g. step complete event)
    {
        process->Unlock();
        return S_OK;
    }

    CordbAppDomain *pAppDomain = GetAppDomain();
    _ASSERTE (pAppDomain != NULL);

    DebuggerIPCEvent event;
    process->InitIPCEvent(&event, 
                          DB_IPCE_STEP_CANCEL, 
                          false,
                          pAppDomain->GetADToken());

    event.StepData.stepperToken = GetLsPtrStepper(); 

    process->Unlock();
    hr = process->SendIPCEvent(&event, sizeof(DebuggerIPCEvent));
    hr = WORST_HR(hr, event.hr);
    process->Lock();


    process->m_steppers.RemoveBase((ULONG_PTR)m_id);
    m_active = false;

    process->Unlock();

    return hr;
}

HRESULT CordbStepper::SetInterceptMask(CorDebugIntercept mask)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    m_rgfInterceptStop = mask;
    return S_OK;
}

HRESULT CordbStepper::SetUnmappedStopMask(CorDebugUnmappedStop mask)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    
    // You must be Win32 attached to stop in unmanaged code.
    if ((mask & STOP_UNMANAGED) && !GetProcess()->IsInteropDebugging())
        return E_INVALIDARG;

    // Limitations on JMC Stepping - if JMC stepping is active,
    // all other stop masks must be disabled.
    // The jit can't place JMC probes before the prolog, so if we're 
    // we're JMC stepping, we'll stop after the prolog. 
    // The implementation for JMC stepping also doesn't let us stop in
    // unmanaged code. (because there are no probes there).
    // So enforce those implementation limitations here.
    if (m_fIsJMCStepper)
    {
        if (mask & STOP_ALL)
            return E_INVALIDARG;
    }

    // @todo- Ensure that we only set valid bits.
    
    
    m_rgfMappingStop = mask;
    return S_OK;
}

HRESULT CordbStepper::Step(BOOL bStepIn)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());
    
    if (m_thread == NULL)
        return CORDBG_E_PROCESS_TERMINATED;

    return StepRange(bStepIn, NULL, 0);
}

//---------------------------------------------------------------------------------------
//
// Ships off a step-range command to the left-side.  On the next continue the LS will
// step across one range at a time.
//
// Arguments:
//    fStepIn - TRUE if this stepper should execute a step-in, else FALSE
//    rgRanges - Array of ranges that define a single step.
//    cRanges - Count of number of elements in rgRanges.
//
// Returns:
//    S_OK if the stepper is successfully set-up, else an appropriate error code.
//  
HRESULT CordbStepper::StepRange(BOOL fStepIn, 
                                COR_DEBUG_STEP_RANGE rgRanges[], 
                                ULONG32 cRanges)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT_ARRAY_OR_NULL(rgRanges, COR_DEBUG_STEP_RANGE, cRanges, true, true);

    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    if (m_thread == NULL)
    {
        return CORDBG_E_PROCESS_TERMINATED;
    }

    HRESULT hr = S_OK;

    if (m_active)
    {
        //
        // Deactivate the current stepping. 
        // or return an error???
        //
        hr = Deactivate();

        if (FAILED(hr))
        {
            return hr;
        }
    }

    // Validate step-ranges. Ranges are exclusive, so end offset
    // should always be greater than start offset.
    // Ranges don't have to be sorted.
    // Zero ranges is ok; though they ought to just call Step() in that case.
    for (ULONG32 i = 0; i < cRanges; i++)
    {
        if (rgRanges[i].startOffset >= rgRanges[i].endOffset)
        {
            STRESS_LOG2(LF_CORDB, LL_INFO10, "Illegal step range. 0x%x-0x%x\n", rgRanges[i].startOffset, rgRanges[i].endOffset);
            return ErrWrapper(E_INVALIDARG);
        }
    }
    
    CordbProcess * pProcess = GetProcess();
    
    //
    // Build step event
    //

    DebuggerIPCEvent * pEvent = reinterpret_cast<DebuggerIPCEvent *>(_alloca(CorDBIPC_BUFFER_SIZE));

    pProcess->InitIPCEvent(pEvent, DB_IPCE_STEP, true, GetAppDomain()->GetADToken());

    pEvent->StepData.vmThreadToken = m_thread->m_vmThreadToken;
    pEvent->StepData.rgfMappingStop = m_rgfMappingStop;
    pEvent->StepData.rgfInterceptStop = m_rgfInterceptStop;
    pEvent->StepData.IsJMCStop = !!m_fIsJMCStepper;

        
    if (m_frame == NULL)
    {
        pEvent->StepData.frameToken = LEAF_MOST_FRAME;
    }
    else
    {
        pEvent->StepData.frameToken = m_frame->GetFramePointer();
    }

    pEvent->StepData.stepIn = (fStepIn != 0);
    pEvent->StepData.totalRangeCount = cRanges;
    pEvent->StepData.rangeIL = m_rangeIL;

    //
    // Send ranges.  We may have to send > 1 message.
    //

    COR_DEBUG_STEP_RANGE * pRangeStart = &(pEvent->StepData.range);
    COR_DEBUG_STEP_RANGE * pRangeEnd = (reinterpret_cast<COR_DEBUG_STEP_RANGE *> (((BYTE *)pEvent) + CorDBIPC_BUFFER_SIZE)) - 1;

    int cRangesToGo = cRanges;

    if (cRangesToGo > 0)
    {
        while (cRangesToGo > 0)
        {
            //
            // Find the number of ranges we can copy this time thru the loop
            //
            int cRangesToCopy;

            if (cRangesToGo < (pRangeEnd - pRangeStart))
            {
                cRangesToCopy = cRangesToGo;
            }
            else
            {
                cRangesToCopy = (unsigned int)(pRangeEnd - pRangeStart);
            }

            //
            // Copy the ranges into the IPC block now, 1-by-1
            //
            int cRangesCopied = 0;

            while (cRangesCopied != cRangesToCopy)
            {
                pRangeStart[cRangesCopied] = rgRanges[cRanges - cRangesToGo + cRangesCopied];
                cRangesCopied++; 
            }

            pEvent->StepData.rangeCount = cRangesCopied;

            cRangesToGo -= cRangesCopied;

            //
            // Send step event (two-way event here...)
            //

            hr = pProcess->SendIPCEvent(pEvent, CorDBIPC_BUFFER_SIZE);

            hr = WORST_HR(hr, pEvent->hr);
            
            if (FAILED(hr))
            {
                return hr;
            }
        }
    }
    else
    {
        //
        // Send step event without any ranges (two-way event here...)
        //

        hr = pProcess->SendIPCEvent(pEvent, CorDBIPC_BUFFER_SIZE);

        hr = WORST_HR(hr, pEvent->hr);

        if (FAILED(hr))
        {
            return hr;
        }
    }

    m_id = LsPtrToCookie(pEvent->StepData.stepperToken);

    LOG((LF_CORDB,LL_INFO10000, "CS::SR: m_id:0x%x | 0x%x \n", 
         m_id, 
         LsPtrToCookie(pEvent->StepData.stepperToken)));

#ifdef _DEBUG
    CordbAppDomain *pAppDomain = GetAppDomain();
#endif
    _ASSERTE (pAppDomain != NULL);

    pProcess->Lock();

    pProcess->m_steppers.AddBase(this);
    m_active = true;

    pProcess->Unlock();

    return hr;
}

//---------------------------------------------------------------------------------------
//
// Ships off a step-out command to the left-side.  On the next continue the LS will
// execute a step-out
//
// Returns:
//    S_OK if the stepper is successfully set-up, else an appropriate error code.
//  
HRESULT CordbStepper::StepOut()
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());
        
    if (m_thread == NULL)
    {
        return CORDBG_E_PROCESS_TERMINATED;
    }

    HRESULT hr;

    if (m_active)
    {
        //
        // Deactivate the current stepping. 
        // or return an error???
        //

        hr = Deactivate();

        if (FAILED(hr))
        {
            return hr;
        }
    }

    CordbProcess * pProcess = GetProcess();

    // We don't do native step-out.
    if (pProcess->SupportsVersion(ver_ICorDebugProcess2))
    {
        if ((m_rgfMappingStop & STOP_UNMANAGED) != 0)
        {
            return ErrWrapper(CORDBG_E_CANT_INTEROP_STEP_OUT);
        }
    }
    
    //
    // Build step event
    //

    DebuggerIPCEvent * pEvent = (DebuggerIPCEvent *) _alloca(CorDBIPC_BUFFER_SIZE);

    pProcess->InitIPCEvent(pEvent, DB_IPCE_STEP_OUT, true, GetAppDomain()->GetADToken());

    pEvent->StepData.vmThreadToken = m_thread->m_vmThreadToken;
    pEvent->StepData.rgfMappingStop = m_rgfMappingStop;
    pEvent->StepData.rgfInterceptStop = m_rgfInterceptStop;
    pEvent->StepData.IsJMCStop = !!m_fIsJMCStepper;

    if (m_frame == NULL)
    {
        pEvent->StepData.frameToken = LEAF_MOST_FRAME;
    }
    else
    {
        pEvent->StepData.frameToken = m_frame->GetFramePointer();
    }

    pEvent->StepData.totalRangeCount = 0;

    // Note: two-way event here...
    hr = pProcess->SendIPCEvent(pEvent, CorDBIPC_BUFFER_SIZE);
    
    hr = WORST_HR(hr, pEvent->hr);
    
    if (FAILED(hr))
    {
        return hr;
    }

    m_id = LsPtrToCookie(pEvent->StepData.stepperToken);

#ifdef _DEBUG
    CordbAppDomain * pAppDomain = GetAppDomain();
#endif
    _ASSERTE (pAppDomain != NULL);

    pProcess->Lock();

    pProcess->m_steppers.AddBase(this);
    m_active = true;

    pProcess->Unlock();
    
    return S_OK;
}
