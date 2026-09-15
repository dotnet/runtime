// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file implements generation of interpreter IR from JIT optimized GenTrees.
// When DOTNET_InterpOptMethod is set, the JIT runs its normal optimization passes
// but instead of generating native code, it translates the optimized LIR into
// interpreter bytecodes that can be executed by the interpreter execution engine.

#include "jitpch.h"

#ifdef FEATURE_INTERPRETER

// Include interpreter opcode definitions for the INTOP_ enum.
#include "../interpreter/inc/intopsshared.h"

#define INTERPRETER_COMPILER_INTERNAL
#include "../interpreter/inc/interpmethod.h"

#include "interpbackend.h"
#include "interpdump.h"


// TODO: This needs to be replaced with a real offset allocator for temporary vars and call args
int32_t InterpBackend::AllocTempVar()
{
    int32_t offset = (int32_t)(m_tempVarBase + m_tempVarCount * INTERP_STACK_SLOT_SIZE);
    m_tempVarCount++;
    return offset;
}

// Skip jit based interpreter compilation, falling back to interpreter library. Fully replacing the interpreter
// library implies that all callsites of this are fixed so we never bailout.
[[noreturn]] void InterpBackend::Bailout(const char* reason)
{
    JITDUMP("InterpIR bailout: %s\n", reason);
    fatal(CORJIT_SKIPPED);
}

void InterpBackend::EmitLdc(int32_t dstOffset, int32_t value)
{
    if (value == 0)
    {
        Emit(INTOP_LDC_I4_0);
        Emit(dstOffset);
    }
    else
    {
        Emit(INTOP_LDC_I4);
        Emit(dstOffset);
        Emit(value);
    }
}

void InterpBackend::EmitBranchTarget(int32_t instrOffset, BasicBlock* target)
{
    m_branchRelocs[m_branchRelocCount].patchLocation = m_ip;
    m_branchRelocs[m_branchRelocCount].instrOffset   = instrOffset;
    m_branchRelocs[m_branchRelocCount].targetBB      = target;
    m_branchRelocCount++;
    // This will be patched with the real offset, after the entire method code is generated
    Emit(0);
}

InterpOpcode InterpBackend::BranchOpForRelop(GenTree* relop)
{
    bool isUnsigned = relop->IsUnsigned();
    switch (relop->OperGet())
    {
        case GT_LT: return isUnsigned ? INTOP_BLT_UN_I4 : INTOP_BLT_I4;
        case GT_LE: return isUnsigned ? INTOP_BLE_UN_I4 : INTOP_BLE_I4;
        case GT_GE: return isUnsigned ? INTOP_BGE_UN_I4 : INTOP_BGE_I4;
        case GT_GT: return isUnsigned ? INTOP_BGT_UN_I4 : INTOP_BGT_I4;
        case GT_EQ: return INTOP_BEQ_I4;
        case GT_NE: return INTOP_BNE_UN_I4;
        default:
            Bailout("unsupported relational operator");
    }
}

// Temporary limitaton for nodes as we only support i32 values for now
void InterpBackend::RequireInt32(GenTree* node)
{
    if (!node->TypeIs(TYP_INT))
    {
        Bailout("unsupported value type");
    }
}

//------------------------------------------------------------------------
// EmitGenTree: Translate a gentree into interpreter bytecode.
//
// Arguments:
//   node      - the gentree to translate
//   dstOffset - the interpreter var offset the result must be produced into, or -1
//               to let the node produce its result wherever is cheapest (a local's
//               own home, or a freshly allocated temp)
//
// Return Value:
//   The interpreter var offset holding the node's result, or -1 for statement roots
//   (stores, branches, returns) that produce no value.
//
// A destination is supplied only by roots that already know where the value must go
// (e.g. GT_STORE_LCL_VAR passes the local's home). Nested operands are emitted with
// -1, so a computed value lands in a temp.
//
int32_t InterpBackend::EmitGenTree(GenTree* node, int32_t destinationOffset)
{
    switch (node->OperGet())
    {
        case GT_LCL_VAR:
        {
            RequireInt32(node);
            int32_t srcOffset = (int32_t)m_varOffsetMap[node->AsLclVar()->GetLclNum()];
            if (destinationOffset < 0)
            {
                return srcOffset;
            }
            // Copy the local into the requested destination.
            Emit(INTOP_MOV_4);
            Emit(destinationOffset);
            Emit(srcOffset);
            return destinationOffset;
        }

        case GT_CNS_INT:
        {
            RequireInt32(node);
            int32_t  dstOffset = (destinationOffset < 0) ? AllocTempVar() : destinationOffset;
            EmitLdc(dstOffset, (int32_t)node->AsIntCon()->IconValue());
            return dstOffset;
        }

        case GT_ADD:
        {
            RequireInt32(node);
            int32_t  dstOffset = (destinationOffset < 0) ? AllocTempVar() : destinationOffset;
            GenTree* op1 = node->AsOp()->gtGetOp1();
            GenTree* op2 = node->AsOp()->gtGetOp2();

            // Fold a constant second operand directly into the instruction.
            if (op2->OperIs(GT_CNS_INT) && op2->TypeIs(TYP_INT))
            {
                int32_t srcOffset1 = EmitGenTree(op1, -1);
                Emit(INTOP_ADD_I4_IMM);
                Emit(dstOffset);
                Emit(srcOffset1);
                Emit((int32_t)op2->AsIntCon()->IconValue());
            }
            else
            {
                int32_t srcOffset1 = EmitGenTree(op1, -1);
                int32_t srcOffset2 = EmitGenTree(op2, -1);
                Emit(INTOP_ADD_I4);
                Emit(dstOffset);
                Emit(srcOffset1);
                Emit(srcOffset2);
            }
            return dstOffset;
        }

        case GT_STORE_LCL_VAR:
            RequireInt32(node);
            EmitGenTree(node->AsLclVar()->Data(), (int32_t)m_varOffsetMap[node->AsLclVar()->GetLclNum()]);
            return -1;

        case GT_JTRUE:
            EmitJTrue(node);
            return -1;

        case GT_RETURN:
        {
            GenTree* retVal = node->AsOp()->GetReturnValue();
            if (retVal != nullptr)
            {
                int32_t srcOffset = EmitGenTree(retVal, -1);
                Emit(INTOP_RET);
                Emit(srcOffset);
            }
            else
            {
                Emit(INTOP_RET_VOID);
            }
            return -1;
        }
        case GT_IL_OFFSET:
            return -1;

        default:
            Bailout("unsupported operator");
    }
}

// Emit a GT_JTRUE as a compare+branch to the current block's true target.
void InterpBackend::EmitJTrue(GenTree* node)
{
    BasicBlock* block = m_currentBB;
    assert(block->KindIs(BBJ_COND));

    GenTree* relop = node->AsOp()->gtGetOp1();

    assert(relop->OperIsCompare());

    GenTree*    op1        = relop->AsOp()->gtGetOp1();
    GenTree*    op2        = relop->AsOp()->gtGetOp2();
    BasicBlock* trueTarget = block->GetTrueTarget();
    bool        isBackward = trueTarget->HasFlag(BBF_BACKWARD_JUMP_TARGET);

    if (relop->OperIs(GT_LT) && !relop->IsUnsigned() && op2->OperIs(GT_CNS_INT) &&
        op2->TypeIs(TYP_INT))
    {
        int32_t src1     = EmitGenTree(op1, -1);
        int32_t instrOff = CurInstrOffset();
        Emit(INTOP_BLT_I4_IMM);
        Emit(src1);
        Emit((int32_t)op2->AsIntCon()->IconValue());
        EmitBranchTarget(instrOff, trueTarget);
        return;
    }

    InterpOpcode branchOp = BranchOpForRelop(relop);
    int32_t      src1     = EmitGenTree(op1, -1);
    int32_t      src2     = EmitGenTree(op2, -1);

    // Poll for GC / thread abort before taking a backward (loop) branch.
    if (isBackward)
    {
        Emit(INTOP_SAFEPOINT);
    }

    int32_t instrOff = CurInstrOffset();
    Emit(branchOp);
    Emit(src1);
    Emit(src2);
    EmitBranchTarget(instrOff, trueTarget);
}

//------------------------------------------------------------------------
// AssignVarOffsets: Map each JIT local to a interpreter stack slot.
//
void InterpBackend::AssignVarOffsets()
{
    Compiler* comp = m_compiler;

    m_varOffsetMap  = new (comp, CMK_Generic) unsigned[comp->lvaCount];
    m_nextVarOffset = 0;

    for (unsigned i = 0; i < comp->lvaCount; i++)
    {
        LclVarDsc* lcl = comp->lvaGetDesc(i);

#if FEATURE_FIXED_OUT_ARGS
        // This should never be used in the current stage of the compilation.
        // We shouldn't reach GT_PUTARG_STK opcodes.
        if (i == comp->lvaOutgoingArgSpaceVar)
        {
            continue;
        }
#endif

        // Method params need to be kept to preserve the call convention. We skip allocation
        // for other dead vars.
        if (!lcl->lvIsParam && (lcl->lvRefCnt(RCS_NORMAL) == 0))
        {
            continue;
        }

        if (!lcl->TypeIs(TYP_INT))
        {
            Bailout("unsupported parameter type");
        }

        m_varOffsetMap[i] = m_nextVarOffset;
        m_nextVarOffset += INTERP_STACK_SLOT_SIZE;
    }

    m_tempVarBase = m_nextVarOffset;
}

//------------------------------------------------------------------------
// EmitCode: Walk LIR roots and emit the interpreter bytecode into m_codeBuffer.
//
void InterpBackend::EmitCode()
{
    Compiler* comp = m_compiler;

    // Count nodes to compute an upper bound on the bytecode / relocation sizes.
    unsigned nodeCount = 0;
    for (BasicBlock* block = comp->fgFirstBB; block != nullptr; block = block->Next())
    {
        for (GenTree* node = block->GetFirstLIRNode(); node != nullptr; node = node->gtNext)
        {
            nodeCount++;
        }
    }

    // Upper bound on the bytecode size for allocation. Each node emits at most
    // ~8 slots, and each block adds per-block overhead (entry / backward-branch
    // safepoints and a BBJ_ALWAYS terminator) not accounted for by nodeCount.
    unsigned maxCodeSlots = nodeCount * 8 + comp->fgBBcount * 4 + 16;
    m_codeBuffer = new (comp, CMK_Generic) int32_t[maxCodeSlots];
    m_ip         = m_codeBuffer;

    m_bbOffsets      = new (comp, CMK_Generic) BBOffset[comp->fgBBcount + 1];
    m_bbOffsetsCount = 0;

    // A relocation is emitted per conditional branch (a JTRUE root, counted in
    // nodeCount) and per BBJ_ALWAYS terminator (one per block, not a node), so
    // bound the array by nodeCount + fgBBcount.
    m_branchRelocs     = new (comp, CMK_Generic) BranchReloc[nodeCount + comp->fgBBcount + 1];
    m_branchRelocCount = 0;

    for (BasicBlock* block = comp->fgFirstBB; block != nullptr; block = block->Next())
    {
        // We only translate the block kinds handled below; anything else (switch,
        // exception-handling flow, call-finally, etc.) falls back to native codegen.
        if (!block->KindIs(BBJ_COND, BBJ_ALWAYS, BBJ_RETURN))
        {
            Bailout("unsupported block kind");
        }

        m_currentBB = block;

        // Record BB's native offset
        m_bbOffsets[m_bbOffsetsCount].bb           = block;
        m_bbOffsets[m_bbOffsetsCount].nativeOffset = CurInstrOffset();
        m_bbOffsetsCount++;

        // Emit INTOP_SAFEPOINT at method entry (first block)
        if (block == comp->fgFirstBB)
        {
            Emit(INTOP_SAFEPOINT);
        }

        for (GenTree* node = block->GetFirstLIRNode(); node != nullptr; node = node->gtNext)
        {
            if (node->IsValue() && !node->IsUnusedValue())
            {
                // If the node has a used result value, it means is part of another gen tree. We will
                // reach this node again when emitting the containing gentree.
                continue;
            }
            EmitGenTree(node, -1);
        }

        // For BBJ_ALWAYS blocks, emit an unconditional branch if target is not the next block
        if (block->KindIs(BBJ_ALWAYS))
        {
            BasicBlock* target = block->GetTarget();
            if (target != block->Next())
            {
                // Emit INTOP_SAFEPOINT before backward branches
                if (target->HasFlag(BBF_BACKWARD_JUMP_TARGET))
                {
                    Emit(INTOP_SAFEPOINT);
                }
                int32_t brInstrOffset = CurInstrOffset();
                Emit(INTOP_BR);
                EmitBranchTarget(brInstrOffset, target);
            }
        }
    }
}

//------------------------------------------------------------------------
// PatchBranches: Resolve each recorded branch relocation to a relative offset.
//
void InterpBackend::PatchBranches()
{
    for (unsigned i = 0; i < m_branchRelocCount; i++)
    {
        BasicBlock* targetBB        = m_branchRelocs[i].targetBB;
        int32_t     targetAbsOffset = -1;

        for (unsigned j = 0; j < m_bbOffsetsCount; j++)
        {
            if (m_bbOffsets[j].bb == targetBB)
            {
                targetAbsOffset = m_bbOffsets[j].nativeOffset;
                break;
            }
        }
        assert(targetAbsOffset >= 0);
        // The interpreter uses RELATIVE branch offsets: ip += offset
        int32_t relativeOffset             = targetAbsOffset - m_branchRelocs[i].instrOffset;
        *m_branchRelocs[i].patchLocation   = relativeOffset;
    }
}

//------------------------------------------------------------------------
// BuildOutput: Package the emitted bytecode into the interpreter method-data format.
//
// This mirrors the interpreter library so we don't invent our own format:
//  - the memory layout follows InterpMethodDataBuilder's section model
//    (interpreter/interpmethoddata.h). The full set of sections that model defines,
//    and which ones we currently emit:
//      Header           // InterpByteCodeStart (InterpMethod ptr)   -- emitted
//      Bytecode         // int32_t[] opcodes                        -- emitted
//      InterpMethod     // InterpMethod struct                      -- emitted
//      DataItems        // void*[] array                            -- not emitted yet
//      AsyncSuspendData // InterpAsyncSuspendData structs           -- not emitted yet
//      IntervalMaps     // InterpIntervalMapEntry arrays            -- not emitted yet
//    Each section starts at its aligned offset, in the order above.
//  - the field values and the InterpMethod construction mirror
//    InterpCompiler::PrepareInterpMethod / FinalizeMethodData, reusing the shared
//    InterpMethod constructor (interpmethod.h) rather than writing the fields by hand.
//
// The allocation itself goes through the VM code manager via ICorJitInfo::allocMem. On
// the interpreter-IR path the JitInfo is a CInterpreterJitInfo, whose allocMem override
// produces the InterpreterCodeHeader-shaped block (the same path the interpreter library
// uses), so the block is tracked by the loader allocator instead of a raw heap allocation.
//
void InterpBackend::BuildOutput(void** methodCodePtr, uint32_t* methodCodeSize)
{
    Compiler* comp = m_compiler;

    int32_t  codeSlots     = CurInstrOffset();
    uint32_t codeSizeBytes = (uint32_t)codeSlots * sizeof(int32_t);

    // Method-data fields, as InterpCompiler::PrepareInterpMethod records them.
    int32_t  argsSize   = (int32_t)(comp->info.compArgsCount * INTERP_STACK_SLOT_SIZE);
    unsigned allocaSize = m_nextVarOffset + m_tempVarCount * INTERP_STACK_SLOT_SIZE;
    allocaSize          = (allocaSize + (INTERP_STACK_ALIGNMENT - 1)) & ~(INTERP_STACK_ALIGNMENT - 1);
    bool initLocals     = comp->info.compInitMem;

    // Section layout, following InterpMethodDataBuilder: each section starts at its
    // aligned offset, in section order.
    auto alignUp = [](uint32_t value, uint32_t alignment) -> uint32_t {
        return (value + alignment - 1) & ~(alignment - 1);
    };
    uint32_t headerOffset       = 0;                                                     // Header section
    uint32_t bytecodeOffset     = alignUp(headerOffset + sizeof(InterpMethod*), sizeof(int32_t)); // Bytecode section
    uint32_t interpMethodOffset = alignUp(bytecodeOffset + codeSizeBytes, sizeof(void*));          // InterpMethod section
    uint32_t totalSize          = interpMethodOffset + sizeof(InterpMethod);
    // No DataItems / AsyncSuspendData / IntervalMaps sections yet.

    // Reserve the unified block through the VM code manager, mirroring how the
    // interpreter library allocates it (eeinterp.cpp). For the interpreter,
    // block == blockRW since the memory is never executed as native code.
    AllocMemChunk codeChunk = {};
    codeChunk.alignment     = sizeof(void*);
    codeChunk.size          = totalSize;
    codeChunk.flags         = CORJIT_ALLOCMEM_HOT_CODE;

    AllocMemArgs args = {};
    args.chunks       = &codeChunk;
    args.chunksCount  = 1;
    args.xcptnsCount  = 0;
    comp->info.compCompHnd->allocMem(&args);

    uint8_t* base = (uint8_t*)codeChunk.blockRW;
    memset(base, 0, totalSize);

    // Finalize, following InterpCompiler::FinalizeMethodData.

    // Bytecode section: copy the emitted opcodes.
    memcpy(base + bytecodeOffset, m_codeBuffer, codeSizeBytes);

    // InterpMethod section: construct it via its shared constructor.
    InterpMethod* pMethod = new (base + interpMethodOffset)
        InterpMethod(comp->info.compMethodHnd, argsSize, (int32_t)allocaSize, /* pDataItems */ nullptr,
                     initLocals, /* unmanagedCallersOnly */ false, /* publishSecretStubParam */ false, codeSlots);

    // Header section: the InterpByteCodeStart, a single pointer to the InterpMethod.
    *(InterpMethod**)(base + headerOffset) = pMethod;

    *methodCodePtr  = codeChunk.block;
    *methodCodeSize = totalSize;

#ifdef DEBUG
    printf("JIT->InterpIR: Generated %d bytecode slots (%d bytes), allocaSize=%d, argsSize=%d, totalSize=%u\n",
           codeSlots, codeSizeBytes, (int32_t)allocaSize, argsSize, totalSize);
#endif
}

//------------------------------------------------------------------------
// CompileMethod: Drive the full LIR -> interpreter bytecode translation.
//
void InterpBackend::CompileMethod(void** methodCodePtr, uint32_t* methodCodeSize)
{
#ifdef DEBUG
    printf("JIT->InterpIR: Generating interpreter IR for method %s\n", m_compiler->info.compFullName);
#endif

    AssignVarOffsets();
    EmitCode();
    PatchBranches();

#ifdef DEBUG
    if (m_compiler->verbose)
    {
        int32_t codeSlots = CurInstrOffset();
        printf("\nGenerated interpreter IR for %s [%d slots]:\n", m_compiler->info.compFullName, codeSlots);
        dumpInterpCode(m_codeBuffer, codeSlots);
    }
#endif

    BuildOutput(methodCodePtr, methodCodeSize);
}

#endif // FEATURE_INTERPRETER
