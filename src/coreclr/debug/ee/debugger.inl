// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: debugger.inl
//

//
// Inline definitions for the Left-Side of the CLR debugging services
// This is logically part of the header file.
//
//*****************************************************************************

#ifndef DEBUGGER_INL_
#define DEBUGGER_INL_

//=============================================================================
// Inlined methods for Debugger.
//=============================================================================
inline bool Debugger::HasLazyData()
{
    LIMITED_METHOD_CONTRACT;
    return (m_pLazyData != NULL);
}
inline RCThreadLazyInit *Debugger::GetRCThreadLazyData()
{
    LIMITED_METHOD_CONTRACT;
    return &(GetLazyData()->m_RCThread);
}

inline DebuggerLazyInit *Debugger::GetLazyData()
{
    LIMITED_METHOD_DAC_CONTRACT;
    _ASSERTE(m_pLazyData != NULL);
    return m_pLazyData;
}

inline DebuggerModuleTable * Debugger::GetModuleTable()
{
    LIMITED_METHOD_CONTRACT;

    return m_pModules;
}


//=============================================================================
// Inlined methods for DebuggerModule.
//=============================================================================


//-----------------------------------------------------------------------------
// Constructor for a Debugger-Module.
// @dbgtodo inspection - get rid of this entire class as we move things out-of-proc.
//-----------------------------------------------------------------------------
inline DebuggerModule::DebuggerModule(Module *      pRuntimeModule,
                                      DomainAssembly *  pDomainAssembly,
                                      AppDomain *   pAppDomain) :
        m_enableClassLoadCallbacks(FALSE),
        m_pPrimaryModule(NULL),
        m_pRuntimeModule(pRuntimeModule),
        m_pRuntimeDomainAssembly(pDomainAssembly),
        m_pAppDomain(pAppDomain)
{
    LOG((LF_CORDB,LL_INFO10000, "DM::DM this:0x%x Module:0x%x DF:0x%x AD:0x%x\n",
        this, pRuntimeModule, pDomainAssembly, pAppDomain));

    // Pick a primary module.
    // Arguably, this could be in DebuggerModuleTable::AddModule
    PickPrimaryModule();


    // Do we have any optimized code?
    DWORD dwDebugBits = pRuntimeModule->GetDebuggerInfoBits();
    m_fHasOptimizedCode = CORDebuggerAllowJITOpts(dwDebugBits);

    // Dynamic modules must receive ClassLoad callbacks in order to receive metadata updates as the module
    // evolves. So we force this on here and refuse to change it for all dynamic modules.
    if (pRuntimeModule->IsReflection())
    {
        EnableClassLoadCallbacks(TRUE);
    }
}

//-----------------------------------------------------------------------------
// Returns true if we have any optimized code in the module.
//
// Notes:
//    JMC-probes aren't emitted in optimized code.
//    <TODO> Life would be nice if the Jit tracked this. </TODO>
//-----------------------------------------------------------------------------
inline bool DebuggerModule::HasAnyOptimizedCode()
{
    LIMITED_METHOD_CONTRACT;
    Module * pModule = this->GetPrimaryModule()->GetRuntimeModule();
    DWORD dwDebugBits = pModule->GetDebuggerInfoBits();
    return CORDebuggerAllowJITOpts(dwDebugBits);
}

//-----------------------------------------------------------------------------
// Return true if we've enabled class-load callbacks.
//-----------------------------------------------------------------------------
inline BOOL DebuggerModule::ClassLoadCallbacksEnabled(void)
{
    return m_enableClassLoadCallbacks;
}

//-----------------------------------------------------------------------------
// Set whether we should enable class-load callbacks for this module.
//-----------------------------------------------------------------------------
inline void DebuggerModule::EnableClassLoadCallbacks(BOOL f)
{
    if (m_enableClassLoadCallbacks != f)
    {
        if (f)
        {
            _ASSERTE(g_pDebugger != NULL);
            g_pDebugger->IncrementClassLoadCallbackCount();
        }
        else
        {
            _ASSERTE(g_pDebugger != NULL);
            g_pDebugger->DecrementClassLoadCallbackCount();
        }

        m_enableClassLoadCallbacks = f;
    }
}

//-----------------------------------------------------------------------------
// Return the appdomain that this module exists in.
//-----------------------------------------------------------------------------
inline AppDomain* DebuggerModule::GetAppDomain()
{
    return m_pAppDomain;
}

//-----------------------------------------------------------------------------
// Return the EE module that this module corresponds to.
//-----------------------------------------------------------------------------
inline Module * DebuggerModule::GetRuntimeModule()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_pRuntimeModule;
}

//-----------------------------------------------------------------------------
// <TODO> (8/12/2002)
// Currently we create a new DebuggerModules for each appdomain a shared
// module lives in. We then pretend there aren't any shared modules.
// This is bad. We need to move away from this.
// Once we stop lying, then every module will be it's own PrimaryModule. :)
//
// Currently, Module* is 1:n w/ DebuggerModule.
// We add a notion of PrimaryModule so that:
// Module* is 1:1 w/ DebuggerModule::GetPrimaryModule();
// This should help transition towards exposing shared modules.
// If the Runtime module is shared, then this gives a common DM.
// If the runtime module is not shared, then this is an identity function.
// </TODO>
//-----------------------------------------------------------------------------
inline DebuggerModule * DebuggerModule::GetPrimaryModule()
{
    _ASSERTE(m_pPrimaryModule != NULL);
    return m_pPrimaryModule;
}

//-----------------------------------------------------------------------------
// This is called by DebuggerModuleTable to set our primary module.
//-----------------------------------------------------------------------------
inline void DebuggerModule::SetPrimaryModule(DebuggerModule * pPrimary)
{
    _ASSERTE(pPrimary != NULL);
    // Our primary module must by definition refer to the same runtime module as us
    _ASSERTE(pPrimary->GetRuntimeModule() == this->GetRuntimeModule());

    LOG((LF_CORDB, LL_EVERYTHING, "DM::SetPrimaryModule - this=%p, pPrimary=%p\n", this, pPrimary));
    m_pPrimaryModule = pPrimary;
}

inline DebuggerEval * FuncEvalFrame::GetDebuggerEval()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_pDebuggerEval;
}

inline unsigned FuncEvalFrame::GetFrameAttribs(void)
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (GetDebuggerEval()->m_evalDuringException)
    {
        return FRAME_ATTR_NONE;
    }
    else
    {
        return FRAME_ATTR_RESUMABLE;    // Treat the next frame as the top frame.
    }
}

inline TADDR FuncEvalFrame::GetReturnAddressPtr()
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (GetDebuggerEval()->m_evalDuringException)
    {
        return NULL;
    }
    else
    {
        return PTR_HOST_MEMBER_TADDR(FuncEvalFrame, this, m_ReturnAddress);
    }
}

//
// This updates the register display for a FuncEvalFrame.
//
inline void FuncEvalFrame::UpdateRegDisplay(const PREGDISPLAY pRD, bool updateFloats)
{
    SUPPORTS_DAC;
    DebuggerEval * pDE = GetDebuggerEval();

    // No context to update if we're doing a func eval from within exception processing.
    if (pDE->m_evalDuringException)
    {
        return;
    }

#ifndef FEATURE_EH_FUNCLETS
    // Reset pContext; it's only valid for active (top-most) frame.
    pRD->pContext = NULL;
#endif // !FEATURE_EH_FUNCLETS


#ifdef TARGET_X86
    // Update all registers in the reg display from the CONTEXT we stored when the thread was hijacked for this func
    // eval. We have to update all registers, not just the callee saved registers, because we can hijack a thread at any
    // point for a func eval, not just at a call site.
    pRD->SetEdiLocation(&(pDE->m_context.Edi));
    pRD->SetEsiLocation(&(pDE->m_context.Esi));
    pRD->SetEbxLocation(&(pDE->m_context.Ebx));
    pRD->SetEdxLocation(&(pDE->m_context.Edx));
    pRD->SetEcxLocation(&(pDE->m_context.Ecx));
    pRD->SetEaxLocation(&(pDE->m_context.Eax));
    pRD->SetEbpLocation(&(pDE->m_context.Ebp));
    pRD->PCTAddr = GetReturnAddressPtr();

#ifdef FEATURE_EH_FUNCLETS

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.

    pRD->pCurrentContext->Eip = *PTR_PCODE(pRD->PCTAddr);
    pRD->pCurrentContext->Esp = (DWORD)GetSP(&pDE->m_context);

    SyncRegDisplayToCurrentContext(pRD);

#else // FEATURE_EH_FUNCLETS

    pRD->ControlPC = *PTR_PCODE(pRD->PCTAddr);
    pRD->SP = (DWORD)GetSP(&pDE->m_context);

#endif // FEATURE_EH_FUNCLETS

#elif defined(TARGET_AMD64)
    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this flag.  This is only temporary.

    memcpy(pRD->pCurrentContext, &(pDE->m_context), sizeof(CONTEXT));

    pRD->pCurrentContextPointers->Rax = &(pDE->m_context.Rax);
    pRD->pCurrentContextPointers->Rcx = &(pDE->m_context.Rcx);
    pRD->pCurrentContextPointers->Rdx = &(pDE->m_context.Rdx);
    pRD->pCurrentContextPointers->R8  = &(pDE->m_context.R8);
    pRD->pCurrentContextPointers->R9  = &(pDE->m_context.R9);
    pRD->pCurrentContextPointers->R10 = &(pDE->m_context.R10);
    pRD->pCurrentContextPointers->R11 = &(pDE->m_context.R11);

    pRD->pCurrentContextPointers->Rbx = &(pDE->m_context.Rbx);
    pRD->pCurrentContextPointers->Rsi = &(pDE->m_context.Rsi);
    pRD->pCurrentContextPointers->Rdi = &(pDE->m_context.Rdi);
    pRD->pCurrentContextPointers->Rbp = &(pDE->m_context.Rbp);
    pRD->pCurrentContextPointers->R12 = &(pDE->m_context.R12);
    pRD->pCurrentContextPointers->R13 = &(pDE->m_context.R13);
    pRD->pCurrentContextPointers->R14 = &(pDE->m_context.R14);
    pRD->pCurrentContextPointers->R15 = &(pDE->m_context.R15);

    // SyncRegDisplayToCurrentContext() sets the pRD->SP and pRD->ControlPC on AMD64.
    SyncRegDisplayToCurrentContext(pRD);

#elif defined(TARGET_ARM)
    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this flag.  This is only temporary.

    memcpy(pRD->pCurrentContext, &(pDE->m_context), sizeof(T_CONTEXT));

    pRD->pCurrentContextPointers->R4 = &(pDE->m_context.R4);
    pRD->pCurrentContextPointers->R5 = &(pDE->m_context.R5);
    pRD->pCurrentContextPointers->R6 = &(pDE->m_context.R6);
    pRD->pCurrentContextPointers->R7 = &(pDE->m_context.R7);
    pRD->pCurrentContextPointers->R8 = &(pDE->m_context.R8);
    pRD->pCurrentContextPointers->R9 = &(pDE->m_context.R9);
    pRD->pCurrentContextPointers->R10 = &(pDE->m_context.R10);
    pRD->pCurrentContextPointers->R11 = &(pDE->m_context.R11);
    pRD->pCurrentContextPointers->Lr = &(pDE->m_context.Lr);

    pRD->volatileCurrContextPointers.R0 = &(pDE->m_context.R0);
    pRD->volatileCurrContextPointers.R1 = &(pDE->m_context.R1);
    pRD->volatileCurrContextPointers.R2 = &(pDE->m_context.R2);
    pRD->volatileCurrContextPointers.R3 = &(pDE->m_context.R3);
    pRD->volatileCurrContextPointers.R12 = &(pDE->m_context.R12);

    SyncRegDisplayToCurrentContext(pRD);

#elif defined(TARGET_ARM64)
    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid = FALSE;        // Don't add usage of this flag.  This is only temporary.

    memcpy(pRD->pCurrentContext, &(pDE->m_context), sizeof(T_CONTEXT));

    pRD->pCurrentContextPointers->X19 = &(pDE->m_context.X19);
    pRD->pCurrentContextPointers->X20 = &(pDE->m_context.X20);
    pRD->pCurrentContextPointers->X21 = &(pDE->m_context.X21);
    pRD->pCurrentContextPointers->X22 = &(pDE->m_context.X22);
    pRD->pCurrentContextPointers->X23 = &(pDE->m_context.X23);
    pRD->pCurrentContextPointers->X24 = &(pDE->m_context.X24);
    pRD->pCurrentContextPointers->X25 = &(pDE->m_context.X25);
    pRD->pCurrentContextPointers->X26 = &(pDE->m_context.X26);
    pRD->pCurrentContextPointers->X27 = &(pDE->m_context.X27);
    pRD->pCurrentContextPointers->X28 = &(pDE->m_context.X28);
    pRD->pCurrentContextPointers->Lr = &(pDE->m_context.Lr);
    pRD->pCurrentContextPointers->Fp = &(pDE->m_context.Fp);

    pRD->volatileCurrContextPointers.X0 = &(pDE->m_context.X0);
    pRD->volatileCurrContextPointers.X1 = &(pDE->m_context.X1);
    pRD->volatileCurrContextPointers.X2 = &(pDE->m_context.X2);
    pRD->volatileCurrContextPointers.X3 = &(pDE->m_context.X3);
    pRD->volatileCurrContextPointers.X4 = &(pDE->m_context.X4);
    pRD->volatileCurrContextPointers.X5 = &(pDE->m_context.X5);
    pRD->volatileCurrContextPointers.X6 = &(pDE->m_context.X6);
    pRD->volatileCurrContextPointers.X7 = &(pDE->m_context.X7);
    pRD->volatileCurrContextPointers.X8 = &(pDE->m_context.X8);
    pRD->volatileCurrContextPointers.X9 = &(pDE->m_context.X9);
    pRD->volatileCurrContextPointers.X10 = &(pDE->m_context.X10);
    pRD->volatileCurrContextPointers.X11 = &(pDE->m_context.X11);
    pRD->volatileCurrContextPointers.X12 = &(pDE->m_context.X12);
    pRD->volatileCurrContextPointers.X13 = &(pDE->m_context.X13);
    pRD->volatileCurrContextPointers.X14 = &(pDE->m_context.X14);
    pRD->volatileCurrContextPointers.X15 = &(pDE->m_context.X15);
    pRD->volatileCurrContextPointers.X16 = &(pDE->m_context.X16);
    pRD->volatileCurrContextPointers.X17 = &(pDE->m_context.X17);

    SyncRegDisplayToCurrentContext(pRD);
#elif defined(TARGET_RISCV64)
    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid = FALSE;        // Don't add usage of this flag.  This is only temporary.

    memcpy(pRD->pCurrentContext, &(pDE->m_context), sizeof(T_CONTEXT));

    pRD->pCurrentContextPointers->S1 = &(pDE->m_context.S1);
    pRD->pCurrentContextPointers->S2 = &(pDE->m_context.S2);
    pRD->pCurrentContextPointers->S3 = &(pDE->m_context.S3);
    pRD->pCurrentContextPointers->S4 = &(pDE->m_context.S4);
    pRD->pCurrentContextPointers->S5 = &(pDE->m_context.S5);
    pRD->pCurrentContextPointers->S6 = &(pDE->m_context.S6);
    pRD->pCurrentContextPointers->S7 = &(pDE->m_context.S7);
    pRD->pCurrentContextPointers->S8 = &(pDE->m_context.S8);
    pRD->pCurrentContextPointers->S9 = &(pDE->m_context.S9);
    pRD->pCurrentContextPointers->S10 = &(pDE->m_context.S10);
    pRD->pCurrentContextPointers->S11 = &(pDE->m_context.S11);
    pRD->pCurrentContextPointers->Fp = &(pDE->m_context.Fp);
    pRD->pCurrentContextPointers->Gp = &(pDE->m_context.Gp);
    pRD->pCurrentContextPointers->Tp = &(pDE->m_context.Tp);
    pRD->pCurrentContextPointers->Ra = &(pDE->m_context.Ra);

    pRD->volatileCurrContextPointers.R0 = &(pDE->m_context.R0);
    pRD->volatileCurrContextPointers.A0 = &(pDE->m_context.A0);
    pRD->volatileCurrContextPointers.A1 = &(pDE->m_context.A1);
    pRD->volatileCurrContextPointers.A2 = &(pDE->m_context.A2);
    pRD->volatileCurrContextPointers.A3 = &(pDE->m_context.A3);
    pRD->volatileCurrContextPointers.A4 = &(pDE->m_context.A4);
    pRD->volatileCurrContextPointers.A5 = &(pDE->m_context.A5);
    pRD->volatileCurrContextPointers.A6 = &(pDE->m_context.A6);
    pRD->volatileCurrContextPointers.A7 = &(pDE->m_context.A7);
    pRD->volatileCurrContextPointers.T0 = &(pDE->m_context.T0);
    pRD->volatileCurrContextPointers.T1 = &(pDE->m_context.T1);
    pRD->volatileCurrContextPointers.T2 = &(pDE->m_context.T2);
    pRD->volatileCurrContextPointers.T3 = &(pDE->m_context.T3);
    pRD->volatileCurrContextPointers.T4 = &(pDE->m_context.T4);
    pRD->volatileCurrContextPointers.T5 = &(pDE->m_context.T5);
    pRD->volatileCurrContextPointers.T6 = &(pDE->m_context.T6);

    SyncRegDisplayToCurrentContext(pRD);
#else
    PORTABILITY_ASSERT("FuncEvalFrame::UpdateRegDisplay is not implemented on this platform.");
#endif
}

#endif  // DEBUGGER_INL_
