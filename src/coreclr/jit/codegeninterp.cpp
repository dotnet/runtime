// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "codegen.h"
#include "codegeninterp.h"
#include "interpdump.h"

#define INTERPRETER_COMPILER_INTERNAL
#include "interpmethod.h"

//------------------------------------------------------------------------
// genInterpCode: Generate interpreter bytecode for the method from rationalized LIR.
//
// Arguments:
//    methodCodePtr  - [out] the published method code
//    methodCodeSize - [out] its size in bytes
//
void Compiler::genInterpCode(void** methodCodePtr, uint32_t* methodCodeSize)
{
    InterpCodeGen codeGen(this);
    codeGen.CompileMethod(methodCodePtr, methodCodeSize);
}

// TODO-Interp: replace with a real offset allocator for temporaries and call arguments.
int32_t InterpCodeGen::AllocTempVar()
{
    int32_t offset = (int32_t)(m_tempVarBase + m_tempVarCount * INTERP_STACK_SLOT_SIZE);
    m_tempVarCount++;
    return offset;
}

//------------------------------------------------------------------------
// Bailout: Stop generating bytecode for this method. The JIT returns CORJIT_SKIPPED,
// so the VM falls back to the next code generator for the method.
//
void InterpCodeGen::Bailout(const char* reason)
{
    JITDUMP("Interpreter codegen bailout: %s\n", reason);
    fatal(CORJIT_SKIPPED);
}

void InterpCodeGen::EmitLdc(int32_t dstOffset, int32_t value)
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

void InterpCodeGen::EmitBranchTarget(int32_t instrOffset, BasicBlock* target)
{
    m_branchRelocs[m_branchRelocCount].patchLocation = m_ip;
    m_branchRelocs[m_branchRelocCount].instrOffset   = instrOffset;
    m_branchRelocs[m_branchRelocCount].targetBB      = target;
    m_branchRelocCount++;

    // Patched with the relative offset once all the code has been generated.
    Emit(0);
}

InterpOpcode InterpCodeGen::BranchOpForRelop(GenTree* relop)
{
    bool isUnsigned = relop->IsUnsigned();
    switch (relop->OperGet())
    {
        case GT_LT:
            return isUnsigned ? INTOP_BLT_UN_I4 : INTOP_BLT_I4;
        case GT_LE:
            return isUnsigned ? INTOP_BLE_UN_I4 : INTOP_BLE_I4;
        case GT_GE:
            return isUnsigned ? INTOP_BGE_UN_I4 : INTOP_BGE_I4;
        case GT_GT:
            return isUnsigned ? INTOP_BGT_UN_I4 : INTOP_BGT_I4;
        case GT_EQ:
            return INTOP_BEQ_I4;
        case GT_NE:
            return INTOP_BNE_UN_I4;
        default:
            Bailout("unsupported relational operator");
    }
}

// TODO-Interp: only int32 values are supported so far.
void InterpCodeGen::RequireInt32(GenTree* node)
{
    if (!node->TypeIs(TYP_INT))
    {
        Bailout("unsupported value type");
    }
}

//------------------------------------------------------------------------
// EmitGenTree: Translate a node into interpreter bytecode.
//
// Arguments:
//   node              - the node to translate
//   destinationOffset - the stack offset the result must be produced into, or -1 to let the
//                       node produce its result wherever is cheapest (a local's own home, or
//                       a freshly allocated temporary)
//
// Return Value:
//   The stack offset holding the node's result, or -1 for roots (stores, branches,
//   returns) that produce no value.
//
int32_t InterpCodeGen::EmitGenTree(GenTree* node, int32_t destinationOffset)
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
            Emit(INTOP_MOV_4);
            Emit(destinationOffset);
            Emit(srcOffset);
            return destinationOffset;
        }

        case GT_CNS_INT:
        {
            RequireInt32(node);
            int32_t dstOffset = (destinationOffset < 0) ? AllocTempVar() : destinationOffset;
            EmitLdc(dstOffset, (int32_t)node->AsIntCon()->IconValue());
            return dstOffset;
        }

        case GT_ADD:
        {
            RequireInt32(node);
            int32_t  dstOffset = (destinationOffset < 0) ? AllocTempVar() : destinationOffset;
            GenTree* op1       = node->AsOp()->gtGetOp1();
            GenTree* op2       = node->AsOp()->gtGetOp2();

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
                RequireInt32(retVal);
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
        case GT_NOP:
            return -1;

        default:
            Bailout("unsupported operator");
    }
}

//------------------------------------------------------------------------
// EmitJTrue: Emit a GT_JTRUE as a compare-and-branch to the current block's true target.
//
void InterpCodeGen::EmitJTrue(GenTree* node)
{
    BasicBlock* block = m_currentBB;
    assert(block->KindIs(BBJ_COND));

    GenTree* relop = node->AsOp()->gtGetOp1();
    if (!relop->OperIsCompare())
    {
        Bailout("unsupported branch condition");
    }

    GenTree* op1 = relop->AsOp()->gtGetOp1();
    GenTree* op2 = relop->AsOp()->gtGetOp2();
    RequireInt32(op1);
    RequireInt32(op2);

    BasicBlock*  trueTarget = block->GetTrueTarget();
    InterpOpcode branchOp   = BranchOpForRelop(relop);
    int32_t      src1       = EmitGenTree(op1, -1);
    int32_t      src2       = EmitGenTree(op2, -1);

    // Poll for GC / thread abort before taking a backward (loop) branch.
    if (trueTarget->HasFlag(BBF_BACKWARD_JUMP_TARGET))
    {
        Emit(INTOP_SAFEPOINT);
    }

    int32_t instrOffset = CurInstrOffset();
    Emit(branchOp);
    Emit(src1);
    Emit(src2);
    EmitBranchTarget(instrOffset, trueTarget);
}

//------------------------------------------------------------------------
// AssignVarOffsets: Map each JIT local to an interpreter stack slot. Parameters come
// first and in order, matching the interpreter calling convention.
//
void InterpCodeGen::AssignVarOffsets()
{
    Compiler* comp = m_compiler;

    m_varOffsetMap  = new (comp, CMK_Codegen) unsigned[comp->lvaCount];
    m_nextVarOffset = 0;

    for (unsigned lclNum = 0; lclNum < comp->lvaCount; lclNum++)
    {
        LclVarDsc* varDsc = comp->lvaGetDesc(lclNum);

        // Parameters keep their slots to preserve the calling convention; other dead locals get none.
        if (!varDsc->lvIsParam && (varDsc->lvRefCnt(RCS_NORMAL) == 0))
        {
            continue;
        }

        if (!varDsc->TypeIs(TYP_INT))
        {
            Bailout("unsupported local type");
        }

        m_varOffsetMap[lclNum] = m_nextVarOffset;
        m_nextVarOffset += INTERP_STACK_SLOT_SIZE;
    }

    m_tempVarBase = m_nextVarOffset;
}

//------------------------------------------------------------------------
// EmitCode: Walk the LIR roots of every block and emit the bytecode into m_codeBuffer.
//
void InterpCodeGen::EmitCode()
{
    Compiler* comp = m_compiler;

    if (comp->compHndBBtabCount != 0)
    {
        Bailout("exception handling");
    }

    unsigned nodeCount = 0;
    for (BasicBlock* const block : comp->Blocks())
    {
        for (GenTree* const node : LIR::AsRange(block))
        {
            nodeCount++;
        }
    }

    // Each node emits at most a few slots, and each block adds a safepoint and a branch.
    unsigned maxCodeSlots = nodeCount * 8 + comp->fgBBcount * 4 + 16;
    m_codeBuffer          = new (comp, CMK_Codegen) int32_t[maxCodeSlots];
    m_ip                  = m_codeBuffer;

    m_bbOffsets      = new (comp, CMK_Codegen) BBOffset[comp->fgBBcount + 1];
    m_bbOffsetsCount = 0;

    // One relocation per conditional branch (a JTRUE node) and per BBJ_ALWAYS terminator.
    m_branchRelocs     = new (comp, CMK_Codegen) BranchReloc[nodeCount + comp->fgBBcount + 1];
    m_branchRelocCount = 0;

    for (BasicBlock* const block : comp->Blocks())
    {
        if (!block->KindIs(BBJ_COND, BBJ_ALWAYS, BBJ_RETURN))
        {
            Bailout("unsupported block kind");
        }

        m_currentBB = block;

        m_bbOffsets[m_bbOffsetsCount].bb     = block;
        m_bbOffsets[m_bbOffsetsCount].offset = CurInstrOffset();
        m_bbOffsetsCount++;

        if (block == comp->fgFirstBB)
        {
            Emit(INTOP_SAFEPOINT);
        }

        for (GenTree* const node : LIR::AsRange(block))
        {
            // Nodes whose value is used are emitted as part of their user.
            if (node->IsValue() && !node->IsUnusedValue())
            {
                continue;
            }
            EmitGenTree(node, -1);
        }

        if (block->KindIs(BBJ_ALWAYS))
        {
            BasicBlock* target = block->GetTarget();
            if (!block->NextIs(target))
            {
                if (target->HasFlag(BBF_BACKWARD_JUMP_TARGET))
                {
                    Emit(INTOP_SAFEPOINT);
                }
                int32_t instrOffset = CurInstrOffset();
                Emit(INTOP_BR);
                EmitBranchTarget(instrOffset, target);
            }
        }
    }

    assert((unsigned)CurInstrOffset() <= maxCodeSlots);
}

//------------------------------------------------------------------------
// PatchBranches: Resolve each recorded branch to a relative offset (the interpreter
// executes `ip += offset`).
//
void InterpCodeGen::PatchBranches()
{
    for (unsigned i = 0; i < m_branchRelocCount; i++)
    {
        BasicBlock* targetBB     = m_branchRelocs[i].targetBB;
        int32_t     targetOffset = -1;

        for (unsigned j = 0; j < m_bbOffsetsCount; j++)
        {
            if (m_bbOffsets[j].bb == targetBB)
            {
                targetOffset = m_bbOffsets[j].offset;
                break;
            }
        }

        assert(targetOffset >= 0);
        *m_branchRelocs[i].patchLocation = targetOffset - m_branchRelocs[i].instrOffset;
    }
}

//------------------------------------------------------------------------
// BuildOutput: Package the bytecode into the interpreter method-data format.
//
// The layout follows InterpMethodDataBuilder (interpreter/interpmethoddata.h): an
// InterpByteCodeStart header, the bytecode, then the InterpMethod, each at its aligned
// offset. The DataItems, AsyncSuspendData and IntervalMaps sections are not emitted yet.
// The block is allocated through ICorJitInfo::allocMem, as the interpreter library does.
//
void InterpCodeGen::BuildOutput(void** methodCodePtr, uint32_t* methodCodeSize)
{
    Compiler* comp = m_compiler;

    int32_t  codeSlots     = CurInstrOffset();
    uint32_t codeSizeBytes = (uint32_t)codeSlots * sizeof(int32_t);

    int32_t  argsSize   = (int32_t)(comp->info.compArgsCount * INTERP_STACK_SLOT_SIZE);
    uint32_t allocaSize = roundUp(m_nextVarOffset + m_tempVarCount * INTERP_STACK_SLOT_SIZE, INTERP_STACK_ALIGNMENT);
    bool     initLocals = comp->info.compInitMem;

    uint32_t headerOffset       = 0;
    uint32_t bytecodeOffset     = roundUp(headerOffset + (uint32_t)sizeof(InterpMethod*), (uint32_t)sizeof(int32_t));
    uint32_t interpMethodOffset = roundUp(bytecodeOffset + codeSizeBytes, (uint32_t)sizeof(void*));
    uint32_t totalSize          = interpMethodOffset + (uint32_t)sizeof(InterpMethod);

    AllocMemChunk codeChunk = {};
    codeChunk.alignment     = sizeof(void*);
    codeChunk.size          = totalSize;
    codeChunk.flags         = CORJIT_ALLOCMEM_HOT_CODE;

    AllocMemArgs args = {};
    args.chunks       = &codeChunk;
    args.chunksCount  = 1;
    args.xcptnsCount  = 0;
    comp->info.compCompHnd->allocMem(&args);

    // The interpreter never executes this memory as native code, so block and blockRW are the same.
    uint8_t* base = (uint8_t*)codeChunk.blockRW;
    memset(base, 0, totalSize);
    memcpy(base + bytecodeOffset, m_codeBuffer, codeSizeBytes);

    InterpMethod* pMethod = new (base + interpMethodOffset)
        InterpMethod(comp->info.compMethodHnd, argsSize, (int32_t)allocaSize, /* pDataItems */ nullptr, initLocals,
                     /* unmanagedCallersOnly */ false, /* publishSecretStubParam */ false, codeSlots);

    *(InterpMethod**)(base + headerOffset) = pMethod;

    *methodCodePtr  = codeChunk.block;
    *methodCodeSize = totalSize;

    BuildGCInfo(codeSizeBytes);

    JITDUMP("Generated %d bytecode slots (%u bytes), allocaSize=%u, argsSize=%d, totalSize=%u\n", codeSlots,
            codeSizeBytes, allocaSize, argsSize, totalSize);
}

//------------------------------------------------------------------------
// BuildGCInfo: Report GC info for the method, as InterpCompiler::BuildGCInfo does.
//
// Arguments:
//    codeSizeBytes - size of the bytecode in bytes
//
// Notes:
//    Only int32 locals are supported so far, so there are no GC slots to report.
//    Code offsets include the InterpByteCodeStart header that precedes the bytecode.
//
void InterpCodeGen::BuildGCInfo(uint32_t codeSizeBytes)
{
    Compiler* comp       = m_compiler;
    uint32_t  codeLength = codeSizeBytes + (uint32_t)sizeof(void*);

    IAllocator*    allocator = new (comp, CMK_GC) CompIAllocator(comp->getAllocatorGC());
    GcInfoEncoder* gcInfoEncoder =
        new (comp, CMK_GC) GcInfoEncoder(comp->info.compCompHnd, comp->info.compMethodInfo, allocator, NOMEM);

    gcInfoEncoder->SetCodeLength(codeLength);
    gcInfoEncoder->SetStackBaseRegister(0);
    gcInfoEncoder->DefineInterruptibleRange(0, codeLength);
    gcInfoEncoder->SetSizeOfStackOutgoingAndScratchArea(0);
    gcInfoEncoder->FinalizeSlotIds();
    gcInfoEncoder->Build();

    // The encoder hands the GC info to the VM through ICorJitInfo::allocGCInfo.
    comp->compInfoBlkAddr = gcInfoEncoder->Emit();
}

//------------------------------------------------------------------------
// CompileMethod: Translate the method's LIR into interpreter bytecode and publish it.
//
void InterpCodeGen::CompileMethod(void** methodCodePtr, uint32_t* methodCodeSize)
{
    JITDUMP("*************** In InterpCodeGen::CompileMethod()\n");

    // Everything that can bail out must run before BuildOutput allocates the method's memory.
    if ((m_compiler->info.compCompHnd->getMethodAttribs(m_compiler->info.compMethodHnd) & CORINFO_FLG_SHAREDINST) != 0)
    {
        Bailout("shared generic code");
    }

    AssignVarOffsets();
    EmitCode();
    PatchBranches();

#ifdef DEBUG
    if (m_compiler->verbose)
    {
        int32_t codeSlots = CurInstrOffset();
        printf("\nGenerated interpreter bytecode for %s [%d slots]:\n", m_compiler->info.compFullName, codeSlots);
        dumpInterpCode(m_codeBuffer, codeSlots);
    }
#endif // DEBUG

    BuildOutput(methodCodePtr, methodCodeSize);
}

//------------------------------------------------------------------------
// The native code generator does not run for the interpreter target (see
// Compiler::compCompile). The definitions below satisfy the target hooks that
// the shared codegen code references.
//

void CodeGen::genMarkLabelsForCodegen()
{
    unreached();
}

void CodeGen::genBeginFnProlog()
{
    unreached();
}

void CodeGen::genHomeRegisterParams(regNumber initReg, bool* initRegStillZeroed)
{
    unreached();
}

void CodeGen::genEnregisterOSRArgsAndLocals(regNumber initReg, bool* pInitRegZeroed)
{
    unreached();
}

void CodeGen::genPushCalleeSavedRegisters(regNumber initReg, bool* pInitRegZeroed)
{
    unreached();
}

void CodeGen::genOSRHandleTier0CalleeSavedRegistersAndFrame()
{
    unreached();
}

void CodeGen::genAllocLclFrame(unsigned frameSize, regNumber initReg, bool* pInitRegZeroed, regMaskTP maskArgRegsLiveIn)
{
    unreached();
}

void CodeGen::genZeroInitFrame(int untrLclHi, int untrLclLo, regNumber initReg, bool* pInitRegZeroed)
{
    unreached();
}

void CodeGen::genReportGenericContextArg(regNumber initReg, bool* pInitRegZeroed)
{
    unreached();
}

void CodeGen::genCaptureFuncletPrologEpilogInfo()
{
    unreached();
}

void CodeGen::genFnEpilog(BasicBlock* block)
{
    unreached();
}

void CodeGen::genFuncletProlog(BasicBlock* block)
{
    unreached();
}

void CodeGen::genFuncletEpilog(BasicBlock* block)
{
    unreached();
}

void CodeGen::genEmitStartBlock(BasicBlock* block)
{
    unreached();
}

void CodeGen::genCodeForTreeNode(GenTree* treeNode)
{
    unreached();
}

void CodeGen::genCallFinally(BasicBlock* block)
{
    unreached();
}

void CodeGen::genEHCatchRet(BasicBlock* block)
{
    unreached();
}

void CodeGen::genIntToIntCast(GenTreeCast* cast)
{
    unreached();
}

void CodeGen::genFloatToFloatCast(GenTree* treeNode)
{
    unreached();
}

void CodeGen::genFloatToIntCast(GenTree* treeNode)
{
    unreached();
}

void CodeGen::genIntToFloatCast(GenTree* treeNode)
{
    unreached();
}

void CodeGen::genLoadLocalIntoReg(regNumber targetReg, unsigned lclNum)
{
    unreached();
}

void CodeGen::genStructReturn(GenTree* treeNode)
{
    unreached();
}

void CodeGen::genEmitGSCookieCheck(bool tailCall)
{
    unreached();
}

void CodeGen::genEmitHelperCall(unsigned helper, int argSize, emitAttr retSize, regNumber callTarget)
{
    unreached();
}

void CodeGen::genProfilingLeaveCallback(unsigned helper)
{
    unreached();
}

void CodeGen::inst_JMP(emitJumpKind jmp, BasicBlock* tgtBlock)
{
    unreached();
}

void CodeGen::instGen_Set_Reg_To_Zero(emitAttr size, regNumber reg, insFlags flags)
{
    unreached();
}

void CodeGen::genReportEH()
{
    unreached();
}

void CodeGen::genCreateAndStoreGCInfo(unsigned            codeSize,
                                      unsigned            prologSize,
                                      unsigned epilogSize DEBUGARG(void* codePtr))
{
    unreached();
}

void RegSet::verifyRegUsed(regNumber reg)
{
}

// Interpreter frames are laid out by the interpreter, so there is no native frame to measure.

int CodeGenInterface::genTotalFrameSize() const
{
    return 0;
}

int CodeGenInterface::genSPtoFPdelta() const
{
    return 0;
}

int CodeGenInterface::genCallerSPtoFPdelta() const
{
    return 0;
}

int CodeGenInterface::genCallerSPtoInitialSPdelta() const
{
    return 0;
}
