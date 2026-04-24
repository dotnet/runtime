// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: interpreterwalker.cpp
//
// Implementation of interpreter bytecode walker for debugger stepping
//
//*****************************************************************************

#include "stdafx.h"
#include "interpreterwalker.h"
#include "controller.h"

#ifdef FEATURE_INTERPRETER

#include "../../interpreter/intops.h"
#include "../../interpreter/inc/interpretershared.h"

// Generate local copy of interpreter opcode length table for use in debugger.
// This avoids linker dependency on the interpreter module.
static const uint8_t s_interpOpLenWalker[] = {
#define OPDEF(a,b,c,d,e,f) c,
#include "../../interpreter/inc/intops.def"
#undef OPDEF
};

void InterpreterWalker::Init(const int32_t* ip, InterpMethod* pInterpMethod)
{
    _ASSERTE(ip != NULL);
    m_ip = ip;
    m_pInterpMethod = pInterpMethod;
    m_switchCaseCount = 0;
    Decode();
}

int32_t InterpreterWalker::ResolveOpcode(const int32_t* ip) const
{
    _ASSERTE(ip != NULL);
    int32_t opcode = *ip;

    // If this is a breakpoint patch, get the original opcode from the patch table
    if (opcode == INTOP_BREAKPOINT)
    {
        DebuggerController::ControllerLockHolder lockController;
        DebuggerPatchTable* patchTable = DebuggerController::GetPatchTable();
        if (patchTable != NULL)
        {
            DebuggerControllerPatch* patch = patchTable->GetPatch((CORDB_ADDRESS_TYPE*)ip);
            if (patch != NULL && patch->IsActivated())
            {
                opcode = (int32_t)patch->opcode;
            }
        }
    }

    return opcode;
}

int InterpreterWalker::GetOpcodeLength(int32_t opcode) const
{
    _ASSERTE(opcode >= 0 && opcode < (int)(sizeof(s_interpOpLenWalker)/sizeof(s_interpOpLenWalker[0])));

    int len = s_interpOpLenWalker[opcode];
    if (len == 0)
    {
        // INTOP_SWITCH has variable length: 3 + number of cases
        _ASSERTE(opcode == INTOP_SWITCH);
        len = 3 + m_ip[2]; // ip[2] contains case count
    }
    return len;
}

const int32_t* InterpreterWalker::GetBranchTarget() const
{
    // Branch offset is at the last slot of the instruction (ip[length-1]).
    // Layout varies by instruction:
    //   INTOP_BR/CALL_FINALLY/LEAVE_CATCH (len 2): [opcode, offset]
    //   INTOP_BRFALSE_* etc (len 3): [opcode, svar, offset]
    //   INTOP_BEQ_* etc (len 4): [opcode, svar1, svar2, offset]
    int instrLen = GetOpcodeLength(m_opcode);
    return m_ip + m_ip[instrLen - 1];
}

void InterpreterWalker::Decode()
{
    LOG((LF_CORDB, LL_INFO10000, "InterpreterWalker::Decode: ip=%p\n", m_ip));

    // Initialize to unknown state
    m_type = WALK_UNKNOWN;
    m_skipIP = NULL;
    m_nextIP = NULL;
    m_opcode = 0;
    m_switchCaseCount = 0;

    if (m_ip == NULL)
        return;

    // Resolve the opcode (handles breakpoint patches)
    m_opcode = ResolveOpcode(m_ip);

    LOG((LF_CORDB, LL_INFO10000, "InterpreterWalker::Decode: opcode=0x%x\n", m_opcode));

    // Calculate skip IP (instruction after current one)
    int instrLen = GetOpcodeLength(m_opcode);
    m_skipIP = m_ip + instrLen;

    // Classify the opcode and set walk type using helper functions from intops.h
    if (InterpOpIsReturn(m_opcode))
    {
        m_type = WALK_RETURN;
        m_nextIP = NULL; // Return address not known statically
        LOG((LF_CORDB, LL_INFO10000, "InterpreterWalker::Decode: WALK_RETURN\n"));
    }
    else if (InterpOpIsTailCall(m_opcode))
    {
        m_type = WALK_RETURN; // Tail calls also leave current method
        m_nextIP = NULL;
        LOG((LF_CORDB, LL_INFO10000, "InterpreterWalker::Decode: WALK_RETURN (tail call)\n"));
    }
    else if (InterpOpIsDirectCall(m_opcode))
    {
        m_type = WALK_CALL;
        m_nextIP = NULL; // Target resolved at step time, not statically
        LOG((LF_CORDB, LL_INFO10000, "InterpreterWalker::Decode: WALK_CALL (direct)\n"));
    }
    else if (InterpOpIsIndirectCall(m_opcode))
    {
        m_type = WALK_CALL;
        m_nextIP = NULL; // Target not known statically
        LOG((LF_CORDB, LL_INFO10000, "InterpreterWalker::Decode: WALK_CALL (indirect)\n"));
    }
    else if (InterpOpIsUncondBranch(m_opcode))
    {
        m_type = WALK_BRANCH;
        m_nextIP = GetBranchTarget();
        LOG((LF_CORDB, LL_INFO10000, "InterpreterWalker::Decode: WALK_BRANCH to %p\n", m_nextIP));
    }
    else if (m_opcode == INTOP_CALL_FINALLY || m_opcode == INTOP_LEAVE_CATCH)
    {
        // Exception handling branches
        m_type = WALK_BRANCH;
        m_nextIP = GetBranchTarget();
        LOG((LF_CORDB, LL_INFO10000, "InterpreterWalker::Decode: WALK_BRANCH (EH) to %p\n", m_nextIP));
    }
    else if (InterpOpIsCondBranch(m_opcode))
    {
        m_type = WALK_COND_BRANCH;
        m_nextIP = GetBranchTarget();
        LOG((LF_CORDB, LL_INFO10000, "InterpreterWalker::Decode: WALK_COND_BRANCH to %p, fallthrough to %p\n", m_nextIP, m_skipIP));
    }
    else if (m_opcode == INTOP_SWITCH)
    {
        m_type = WALK_COND_BRANCH;
        m_switchCaseCount = m_ip[2]; // Number of cases
        m_nextIP = NULL; // Use GetSwitchTarget() for targets
        LOG((LF_CORDB, LL_INFO10000, "InterpreterWalker::Decode: WALK_COND_BRANCH (switch) with %d cases\n", m_switchCaseCount));
    }
    else if (InterpOpIsThrow(m_opcode))
    {
        m_type = WALK_THROW;
        m_nextIP = NULL;
        LOG((LF_CORDB, LL_INFO10000, "InterpreterWalker::Decode: WALK_THROW\n"));
    }
    else if (m_opcode == INTOP_BREAKPOINT || m_opcode == INTOP_HALT)
    {
        m_type = WALK_BREAK;
        m_nextIP = m_skipIP;
        LOG((LF_CORDB, LL_INFO10000, "InterpreterWalker::Decode: WALK_BREAK\n"));
    }
    else
    {
        // All other instructions - normal sequential execution
        m_type = WALK_NEXT;
        m_nextIP = m_skipIP;
        LOG((LF_CORDB, LL_INFO10000, "InterpreterWalker::Decode: WALK_NEXT to %p\n", m_nextIP));
    }
}

const int32_t* InterpreterWalker::GetSwitchTarget(int32_t caseIndex) const
{
    // Switch instruction layout:
    // ip[0] = INTOP_SWITCH
    // ip[1] = source var offset
    // ip[2] = case count (n)
    // ip[3] to ip[3+n-1] = relative offsets for each case

    if (m_opcode != INTOP_SWITCH)
        return NULL;

    int32_t caseCount = m_ip[2];

    // If caseIndex equals caseCount, return fallthrough address (after switch)
    if (caseIndex == caseCount)
    {
        return m_skipIP;
    }

    if (caseIndex < 0 || caseIndex >= caseCount)
        return NULL;

    // Each case target is stored as a relative offset from the switch instruction
    int32_t offset = m_ip[3 + caseIndex];
    return m_ip + offset;
}

#endif // FEATURE_INTERPRETER
