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
#include "../../vm/interpexec.h"

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
    if (currentOpcode == INTOP_BREAKPOINT)
    {
        LOG((LF_CORDB, LL_INFO1000, "InterpreterEC::ApplyPatch Patch already applied at %p\n",
            patch->address));
        return false;
    }

    patch->opcode = currentOpcode; // Save original opcode
    patch->m_interpActivated = true; // Mark as activated (needed since opcode 0 is valid for interpreter)
    *(uint32_t*)patch->address = INTOP_BREAKPOINT;
    LOG((LF_CORDB, LL_INFO10000, "InterpreterEC::ApplyPatch Breakpoint inserted at %p, saved opcode 0x%x (%s)\n",
        patch->address, patch->opcode, GetInterpOpName((int)patch->opcode)));

    return true;
}

bool InterpreterExecutionControl::UnapplyPatch(DebuggerControllerPatch* patch)
{
    _ASSERTE(patch != NULL);
    _ASSERTE(patch->address != NULL);
    _ASSERTE(patch->IsActivated());

    LOG((LF_CORDB, LL_INFO1000, "InterpreterEC::UnapplyPatch %p at bytecode addr %p, replacing with original opcode 0x%x (%s)\n",
        patch, patch->address, patch->opcode, GetInterpOpName((int)patch->opcode)));

    // Restore the original opcode
    *(uint32_t*)patch->address = (uint32_t)patch->opcode; // Opcodes are stored in uint32_t slots
    InitializePRD(&(patch->opcode));
    patch->m_interpActivated = false; // Clear activation flag

    LOG((LF_CORDB, LL_EVERYTHING, "InterpreterEC::UnapplyPatch Restored opcode at %p\n",
        patch->address));

    return true;
}

void InterpreterExecutionControl::BypassPatch(DebuggerControllerPatch* patch, CONTEXT* filterCtx)
{
    _ASSERTE(patch != NULL);
    _ASSERTE(filterCtx != NULL);

    InterpMethodContextFrame *pFrame = (InterpMethodContextFrame *)GetSP(filterCtx);
    _ASSERTE(pFrame != NULL);

    pFrame->SetBypass((const int32_t*)patch->address, (int32_t)patch->opcode);

    LOG((LF_CORDB, LL_INFO10000, "InterpreterEC::BypassPatch at %p, opcode 0x%x (%s)\n",
        patch->address, patch->opcode, GetInterpOpName((int)patch->opcode)));
}

#endif // FEATURE_INTERPRETER
#endif // !DACCESS_COMPILE
