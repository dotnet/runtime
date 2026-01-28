// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: executioncontrol.cpp
//
// Implementation of execution control for interpreter breakpoints.
//
//*****************************************************************************

#include "stdafx.h"
#include "executioncontrol.h"
#include "controller.h"
#include "../../vm/codeman.h"

#ifdef FEATURE_INTERPRETER
#include "../../interpreter/intops.h"
#endif

#if !defined(DACCESS_COMPILE)
#ifdef FEATURE_INTERPRETER

//=============================================================================
// InterpreterExecutionControl - Interpreter bytecode breakpoints
//=============================================================================

InterpreterExecutionControl InterpreterExecutionControl::s_instance;

InterpreterExecutionControl* InterpreterExecutionControl::GetInstance()
{
    return &s_instance;
}

bool InterpreterExecutionControl::ApplyPatch(DebuggerControllerPatch* patch)
{
    _ASSERTE(patch != NULL);
    _ASSERTE(!patch->IsActivated());
    _ASSERTE(patch->IsBound());

    LOG((LF_CORDB, LL_INFO10000, "InterpreterEC::ApplyPatch %p at bytecode addr %p\n",
        patch, patch->address));

    patch->opcode = *(int32_t*)patch->address;
    *(uint32_t*)patch->address = INTOP_BREAKPOINT;

    LOG((LF_CORDB, LL_EVERYTHING, "InterpreterEC::ApplyPatch Breakpoint inserted at %p, saved opcode %x\n",
        patch->address, patch->opcode));

    return true;
}

bool InterpreterExecutionControl::UnapplyPatch(DebuggerControllerPatch* patch)
{
    _ASSERTE(patch != NULL);
    _ASSERTE(patch->address != NULL);
    _ASSERTE(patch->IsActivated());

    LOG((LF_CORDB, LL_INFO1000, "InterpreterEC::UnapplyPatch %p at bytecode addr %p, replacing with original opcode 0x%x\n",
        patch, patch->address, patch->opcode));

    // Restore the original opcode
    *(uint32_t*)patch->address = (uint32_t)patch->opcode; // Opcodes are stored in uint32_t slots
    InitializePRD(&(patch->opcode));

    LOG((LF_CORDB, LL_EVERYTHING, "InterpreterEC::UnapplyPatch Restored opcode at %p\n",
        patch->address));

    return true;
}

#endif // FEATURE_INTERPRETER
#endif // !DACCESS_COMPILE
