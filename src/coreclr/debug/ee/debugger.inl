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

inline DebuggerEval * FuncEvalFrame::GetDebuggerEval()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_pDebuggerEval;
}

#endif  // DEBUGGER_INL_
