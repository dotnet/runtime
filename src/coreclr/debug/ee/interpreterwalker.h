// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: interpreterwalker.h
//
// Interpreter bytecode walker for debugger stepping
// Mirrors NativeWalker pattern for interpreter code analysis
//
//*****************************************************************************

#ifndef INTERPRETERWALKER_H_
#define INTERPRETERWALKER_H_

#include "walker.h"

#ifdef FEATURE_INTERPRETER

// Forward declaration
struct InterpMethod;

// InterpreterWalker decodes interpreter bytecode to determine control flow
// for stepping operations. Similar to NativeWalker but for interpreter opcodes.
class InterpreterWalker
{
public:
    InterpreterWalker()
        : m_type(WALK_UNKNOWN), m_opcode(0), m_ip(NULL), m_skipIP(NULL),
          m_nextIP(NULL), m_pInterpMethod(NULL), m_switchCaseCount(0) {}

    // Initialize the walker at a given instruction pointer
    // ip - pointer to current interpreter bytecode instruction
    // pInterpMethod - InterpMethod containing the code (for data items lookup)
    void Init(const int32_t* ip, InterpMethod* pInterpMethod);

    // Get the current instruction pointer
    const int32_t* GetIP() const { return m_ip; }

    // Get the walk type for the current opcode
    WALK_TYPE GetOpcodeWalkType() const { return m_type; }

    // Get the raw opcode value (after resolving patches)
    int32_t GetOpcode() const { return m_opcode; }

    // Get the skip IP - address after the current instruction (for step-over)
    const int32_t* GetSkipIP() const { return m_skipIP; }

    // Get the next IP - branch/call target address (for step-into)
    const int32_t* GetNextIP() const { return m_nextIP; }

    // Get switch case count (only valid for INTOP_SWITCH)
    int32_t GetSwitchCaseCount() const { return m_switchCaseCount; }

    // Get switch target for a specific case index
    // caseIndex: 0 to GetSwitchCaseCount()-1 for case targets
    //            GetSwitchCaseCount() for fall-through target (after switch)
    const int32_t* GetSwitchTarget(int32_t caseIndex) const;

    // Decode the instruction at the current IP
    void Decode();

private:
    // Resolve opcode at address, handling breakpoint patches
    int32_t ResolveOpcode(const int32_t* ip) const;

    // Get instruction length for an opcode
    int GetOpcodeLength(int32_t opcode) const;

    // Get branch target from instruction
    const int32_t* GetBranchTarget() const;

    WALK_TYPE       m_type;             // Walk type of current instruction
    int32_t         m_opcode;           // Current opcode (resolved from patches)
    const int32_t*  m_ip;               // Current instruction pointer
    const int32_t*  m_skipIP;           // IP after current instruction
    const int32_t*  m_nextIP;           // Branch/call target IP
    InterpMethod*   m_pInterpMethod;    // InterpMethod for data item lookups
    int32_t         m_switchCaseCount;  // Number of switch cases (for INTOP_SWITCH)
};

#endif // FEATURE_INTERPRETER
#endif // INTERPRETERWALKER_H_
