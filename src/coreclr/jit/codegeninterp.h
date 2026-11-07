// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _CODEGENINTERP_H_
#define _CODEGENINTERP_H_

#include "intopsshared.h"

//------------------------------------------------------------------------
// InterpCodeGen: Translates a method's rationalized LIR into interpreter bytecode.
//
// The translation is destination-driven: it walks LIR roots (stores, branches,
// returns) and recurses into their operand trees, emitting each operation directly
// into its destination stack slot and forwarding locals / immediates without
// materializing redundant temporaries.
//
// Anything not supported yet makes the JIT return CORJIT_SKIPPED, so the VM falls
// back to the next code generator for the method.
//
class InterpCodeGen
{
    // A branch whose relative target offset is patched once every block's offset is known.
    struct BranchReloc
    {
        int32_t*    patchLocation; // slot in the buffer that receives the relative offset
        int32_t     instrOffset;   // offset (in slots) of the branch instruction itself
        BasicBlock* targetBB;      // block the branch jumps to
    };

    struct BBOffset
    {
        BasicBlock* bb;
        int32_t     offset; // in int32_t slots from the start of the buffer
    };

    Compiler* m_compiler;

    // Stack slot assignment.
    unsigned* m_varOffsetMap  = nullptr; // JIT local number -> interpreter stack byte offset
    unsigned  m_nextVarOffset = 0;       // high-water mark of assigned local byte offsets
    unsigned  m_tempVarBase   = 0;       // byte offset where temporaries begin
    unsigned  m_tempVarCount  = 0;       // number of temporaries handed out

    // Bytecode buffer.
    int32_t* m_codeBuffer = nullptr;
    int32_t* m_ip         = nullptr;

    // Branch patching.
    BBOffset*    m_bbOffsets        = nullptr;
    unsigned     m_bbOffsetsCount   = 0;
    BranchReloc* m_branchRelocs     = nullptr;
    unsigned     m_branchRelocCount = 0;

    BasicBlock* m_currentBB = nullptr;

    void Emit(int32_t value)
    {
        *m_ip++ = value;
    }

    int32_t CurInstrOffset() const
    {
        return (int32_t)(m_ip - m_codeBuffer);
    }

    int32_t      AllocTempVar();
    void         EmitLdc(int32_t dstOffset, int32_t value);
    void         EmitBranchTarget(int32_t instrOffset, BasicBlock* target);
    InterpOpcode BranchOpForRelop(GenTree* relop);

    [[noreturn]] void Bailout(const char* reason);
    void              RequireInt32(GenTree* node);

    int32_t EmitGenTree(GenTree* node, int32_t dstOffset);
    void    EmitJTrue(GenTree* jtrue);

    void AssignVarOffsets();
    void EmitCode();
    void PatchBranches();
    void BuildOutput(void** methodCodePtr, uint32_t* methodCodeSize);
    void BuildGCInfo(uint32_t codeSizeBytes);

public:
    InterpCodeGen(Compiler* compiler)
        : m_compiler(compiler)
    {
    }

    void CompileMethod(void** methodCodePtr, uint32_t* methodCodeSize);
};

#endif // _CODEGENINTERP_H_
