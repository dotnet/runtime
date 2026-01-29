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

InterpreterExecutionControl* InterpreterExecutionControl::GetInstance()
{
    static InterpreterExecutionControl s_instance;
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

BreakpointInfo InterpreterExecutionControl::GetBreakpointInfo(const void* address) const
{
    _ASSERTE(address != NULL);

    BreakpointInfo info = { INTOP_NOP, false };

    DebuggerController::ControllerLockHolder lockController;

    DebuggerPatchTable* patchTable = DebuggerController::GetPatchTable();
    if (patchTable != NULL)
    {
        DebuggerControllerPatch* patch = patchTable->GetPatch((CORDB_ADDRESS_TYPE*)address);
        if (patch != NULL && patch->IsActivated())
        {
            // Get the original opcode from the first activated patch
            info.originalOpcode = (InterpOpcode)patch->opcode;

            // Iterate through ALL patches at this address to check if ANY is a step-out patch.
            // Multiple patches can exist at the same address (e.g., IDE breakpoint + step-out from Debugger.Break()).
            // The step-out patch may not be the first in the list, so we must check all of them.
            for (DebuggerControllerPatch* p = patch; p != NULL; p = patchTable->GetNextPatch(p))
            {
                if (p->GetKind() == PATCH_KIND_NATIVE_MANAGED)
                {
                    info.isStepOut = true;
                    break;
                }
            }

            LOG((LF_CORDB, LL_INFO10000, "InterpreterEC::GetBreakpointInfo at %p: opcode=0x%x, isStepOut=%d\n",
                address, info.originalOpcode, info.isStepOut));
            return info;
        }
    }

    // No patch at this address, read opcode from memory, not a step-out
    info.originalOpcode = (InterpOpcode)(*(const uint32_t*)address);
    info.isStepOut = false;
    LOG((LF_CORDB, LL_INFO10000, "InterpreterEC::GetBreakpointInfo at %p: no patch, opcode=0x%x\n",
        address, info.originalOpcode));
    return info;
}
#endif // FEATURE_INTERPRETER
#endif // !DACCESS_COMPILE
