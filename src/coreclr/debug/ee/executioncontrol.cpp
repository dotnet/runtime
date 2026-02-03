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

// TODO: Remove, this is just for pretty printing opcodes in logging.
// Generate local copy of interpreter opcode length table for use in debugger.
// This avoids linker dependency on the interpreter module.
static const uint8_t s_interpOpLen[] = {
#define OPDEF(a,b,c,d,e,f) c,
#include "../../interpreter/inc/intops.def"
#undef OPDEF
};

// Generate local opcode name table (same pattern as intops.cpp)
struct InterpOpNameChars
{
#define OPDEF(a,b,c,d,e,f) char a[sizeof(b)];
#include "../../interpreter/inc/intops.def"
#undef OPDEF
};

static const InterpOpNameChars s_interpOpNameChars = {
#define OPDEF(a,b,c,d,e,f) b,
#include "../../interpreter/inc/intops.def"
#undef OPDEF
};

static const uint32_t s_interpOpNameOffsets[] = {
#define OPDEF(a,b,c,d,e,f) offsetof(InterpOpNameChars, a),
#include "../../interpreter/inc/intops.def"
#undef OPDEF
};

static const char* GetInterpOpName(int op)
{
    return ((const char*)&s_interpOpNameChars) + s_interpOpNameOffsets[op];
}
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

// Assume controller lock is held by caller
bool InterpreterExecutionControl::ApplyPatch(DebuggerControllerPatch* patch)
{
    _ASSERTE(patch != NULL);
    _ASSERTE(!patch->IsActivated());
    _ASSERTE(patch->IsBound());

    LOG((LF_CORDB, LL_INFO10000, "InterpreterEC::ApplyPatch %p at bytecode addr %p\n",
        patch, patch->address));

    // Check if there is already a breakpoint patch at this address
    uint32_t currentOpcode = *(uint32_t*)patch->address;
    if (currentOpcode == INTOP_BREAKPOINT || currentOpcode == INTOP_SINGLESTEP)
    {
        LOG((LF_CORDB, LL_INFO1000, "InterpreterEC::ApplyPatch Patch already applied at %p\n",
            patch->address));
        return false;
    }

    patch->opcode = currentOpcode; // Save original opcode

    // Check if this is a single-step patch by looking at the controller's thread's interpreter SS flag.
    Thread* pThread = patch->controller->GetThread();
    if (pThread != NULL && pThread->IsInterpreterSingleStepEnabled())
    {
        *(uint32_t*)patch->address = INTOP_SINGLESTEP;
        LOG((LF_CORDB, LL_INFO10000, "InterpreterEC::ApplyPatch SingleStep inserted at %p, saved opcode 0x%x (%s)\n",
            patch->address, patch->opcode, GetInterpOpName(patch->opcode)));
    }
    else
    {
        *(uint32_t*)patch->address = INTOP_BREAKPOINT;
        LOG((LF_CORDB, LL_INFO10000, "InterpreterEC::ApplyPatch Breakpoint inserted at %p, saved opcode 0x%x (%s)\n",
            patch->address, patch->opcode, GetInterpOpName(patch->opcode)));
    }

    return true;
}

bool InterpreterExecutionControl::UnapplyPatch(DebuggerControllerPatch* patch)
{
    _ASSERTE(patch != NULL);
    _ASSERTE(patch->address != NULL);
    _ASSERTE(patch->IsActivated());

    LOG((LF_CORDB, LL_INFO1000, "InterpreterEC::UnapplyPatch %p at bytecode addr %p, replacing with original opcode 0x%x (%s)\n",
        patch, patch->address, patch->opcode, GetInterpOpName(patch->opcode)));

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

            LOG((LF_CORDB, LL_INFO10000, "InterpreterEC::GetBreakpointInfo at %p: opcode=0x%x (%s), isStepOut=%d\n",
                address, info.originalOpcode, GetInterpOpName(info.originalOpcode), info.isStepOut));
            return info;
        }
    }

    // No patch at this address, read opcode from memory, not a step-out
    info.originalOpcode = (InterpOpcode)(*(const uint32_t*)address);
    info.isStepOut = false;
    LOG((LF_CORDB, LL_INFO10000, "InterpreterEC::GetBreakpointInfo at %p: no patch, opcode=0x%x (%s)\n",
        address, info.originalOpcode, GetInterpOpName(info.originalOpcode)));
    return info;
}
#endif // FEATURE_INTERPRETER
#endif // !DACCESS_COMPILE
