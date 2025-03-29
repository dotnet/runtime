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
                                      DomainAssembly *  pDomainAssembly) :
        m_enableClassLoadCallbacks(FALSE),
        m_pRuntimeModule(pRuntimeModule),
        m_pRuntimeDomainAssembly(pDomainAssembly)
{
    LOG((LF_CORDB,LL_INFO10000, "DM::DM this:0x%x Module:0x%x DF:0x%x\n",
        this, pRuntimeModule, pDomainAssembly));

    // Do we have any optimized code?
    DWORD dwDebugBits = pRuntimeModule->GetDebuggerInfoBits();
    m_fHasOptimizedCode = CORDebuggerAllowJITOpts(dwDebugBits);

    // Dynamic modules must receive ClassLoad callbacks in order to receive metadata updates as the module
    // evolves. So we force this on here and refuse to change it for all dynamic modules.
    if (pRuntimeModule->IsReflectionEmit())
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
    Module * pModule = GetRuntimeModule();
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
// Return the EE module that this module corresponds to.
//-----------------------------------------------------------------------------
inline Module * DebuggerModule::GetRuntimeModule()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_pRuntimeModule;
}

inline DebuggerEval * FuncEvalFrame::GetDebuggerEval()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_pDebuggerEval;
}

#endif  // DEBUGGER_INL_
