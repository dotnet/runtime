// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
                                      DomainFile *  pDomainFile, 
                                      AppDomain *   pAppDomain) :
        m_enableClassLoadCallbacks(FALSE),
        m_pPrimaryModule(NULL),
        m_pRuntimeModule(pRuntimeModule),
        m_pRuntimeDomainFile(pDomainFile),
        m_pAppDomain(pAppDomain)
{
    LOG((LF_CORDB,LL_INFO10000, "DM::DM this:0x%x Module:0x%x DF:0x%x AD:0x%x\n",
        this, pRuntimeModule, pDomainFile, pAppDomain));

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
inline void FuncEvalFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
{
    SUPPORTS_DAC;
    DebuggerEval * pDE = GetDebuggerEval();

    // No context to update if we're doing a func eval from within exception processing.
    if (pDE->m_evalDuringException)
    {
        return;
    }

#if !defined(_WIN64)
    // Reset pContext; it's only valid for active (top-most) frame.
    pRD->pContext = NULL;
#endif // !_WIN64


#ifdef _TARGET_X86_
    // Update all registers in the reg display from the CONTEXT we stored when the thread was hijacked for this func
    // eval. We have to update all registers, not just the callee saved registers, because we can hijack a thread at any
    // point for a func eval, not just at a call site.
    pRD->pEdi = &(pDE->m_context.Edi);
    pRD->pEsi = &(pDE->m_context.Esi);
    pRD->pEbx = &(pDE->m_context.Ebx);
    pRD->pEdx = &(pDE->m_context.Edx);
    pRD->pEcx = &(pDE->m_context.Ecx);
    pRD->pEax = &(pDE->m_context.Eax);
    pRD->pEbp = &(pDE->m_context.Ebp);
    pRD->SP   = (DWORD)GetSP(&pDE->m_context);
    pRD->PCTAddr = GetReturnAddressPtr();
    pRD->ControlPC = *PTR_PCODE(pRD->PCTAddr);

#elif defined(_TARGET_AMD64_)
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

#elif defined(_TARGET_ARM_)
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
#else 
    PORTABILITY_ASSERT("FuncEvalFrame::UpdateRegDisplay is not implemented on this platform.");
#endif
}

#endif  // DEBUGGER_INL_
