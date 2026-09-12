// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file declares InterpBackend, which translates a method's optimized
// LIR into interpreter bytecode. See interpbackend.cpp for the implementation.

#ifndef _INTERPBACKEND_H_
#define _INTERPBACKEND_H_

#ifdef FEATURE_INTERPRETER

// Interpreter opcode definitions (INTOP_ enum / InterpOpcode).
#include "../interpreter/inc/intopsshared.h"

class Compiler;
struct GenTree;
struct BasicBlock;

//------------------------------------------------------------------------
// InterpBackend: Translates a method's optimized LIR into interpreter
// bytecode. Modeled on the interpreter library's InterpCompiler: it owns all
// interpreter-specific compilation state (var offset assignment, the bytecode
// buffer, branch relocations) and exposes a single CompileMethod entry point.
//
// The translation is destination-driven: CompileMethod walks LIR roots (stores,
// branches, returns) and recurses into their operand trees, emitting each
// operation directly into its destination var and forwarding locals / immediates
// without materializing redundant temporaries.
//
class InterpBackend
{
    // A pending conditional/unconditional branch whose relative target offset is
    // patched once the native offset of every basic block is known.
    struct BranchReloc
    {
        int32_t*    patchLocation; // slot in the buffer that receives the relative offset
        int32_t     instrOffset;   // offset (in slots) of the branch instruction itself
        BasicBlock* targetBB;      // block the branch jumps to
    };

    // Native (slot) offset of the start of a basic block, used to resolve branches.
    struct BBOffset
    {
        BasicBlock* bb;
        int32_t     nativeOffset; // in int32_t slots from the start of the buffer
    };

    Compiler* m_compiler; // the owning JIT compiler (locals, flowgraph, allocations)

    // Var offset assignment.
    unsigned* m_varOffsetMap;  // JIT local number -> interp var byte offset
    unsigned  m_nextVarOffset; // running high-water of assigned local byte offsets
    unsigned  m_tempVarBase;   // byte offset where temp vars begin
    unsigned  m_tempVarCount;  // number of temp vars handed out

    // Bytecode buffer.
    int32_t* m_codeBuffer; // start of the emitted bytecode
    int32_t* m_ip;         // current emit position

    // Branch patching.
    BBOffset*    m_bbOffsets;
    unsigned     m_bbOffsetsCount;
    BranchReloc* m_branchRelocs;
    unsigned     m_branchRelocCount;

    // The block whose LIR is currently being emitted (used by branch roots to find
    // their targets).
    BasicBlock* m_currentBB;

    // Emit primitives.
    void    Emit(int32_t value) { *m_ip++ = value; }
    int32_t CurInstrOffset() { return (int32_t)(m_ip - m_codeBuffer); }
    int32_t AllocTempVar();
    void    EmitLdc(int32_t dstOffset, int32_t value);
    void    EmitBranchTarget(int32_t instrOffset, BasicBlock* target);
    InterpOpcode BranchOpForRelop(GenTree* relop);

    // Abandon interpreter-IR generation and fall back to normal JIT codegen. Throws
    // out of the whole compile as CORJIT_SKIPPED (never returns).
    [[noreturn]] void Bailout(const char* reason);
    void RequireInt32(GenTree* node);

    // Recursive translator: emit a gentree, producing its result into dstOffset when
    // one is given (>= 0), or wherever is cheapest otherwise. Returns the offset the
    // result lives at (-1 for statement roots that produce no value).
    int32_t EmitGenTree(GenTree* node, int32_t dstOffset);
    void    EmitJTrue(GenTree* jtrue);
    void    EmitReturn(GenTree* ret);

    // Phases.
    void AssignVarOffsets();
    void EmitCode();
    void PatchBranches();
    void BuildOutput(void** methodCodePtr, uint32_t* methodCodeSize);

public:
    InterpBackend(Compiler* compiler)
        : m_compiler(compiler)
        , m_varOffsetMap(nullptr)
        , m_nextVarOffset(0)
        , m_tempVarBase(0)
        , m_tempVarCount(0)
        , m_codeBuffer(nullptr)
        , m_ip(nullptr)
        , m_bbOffsets(nullptr)
        , m_bbOffsetsCount(0)
        , m_branchRelocs(nullptr)
        , m_branchRelocCount(0)
        , m_currentBB(nullptr)
    {
    }

    void CompileMethod(void** methodCodePtr, uint32_t* methodCodeSize);
};

#endif // FEATURE_INTERPRETER

#endif // _INTERPBACKEND_H_
