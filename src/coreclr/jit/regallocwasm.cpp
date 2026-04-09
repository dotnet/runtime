// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "regallocwasm.h"

#include "lower.h" // for LowerRange()

RegAllocInterface* GetRegisterAllocator(Compiler* compiler)
{
    return new (compiler->getAllocator(CMK_LSRA)) WasmRegAlloc(compiler);
}

WasmRegAlloc::WasmRegAlloc(Compiler* compiler)
    : m_compiler(compiler)
    , m_codeGen(compiler->codeGen)
    , m_currentBlock(nullptr)
    , m_currentFunclet(ROOT_FUNC_IDX)
    , m_perFuncletData(compiler->compFuncCount(), nullptr, compiler->getAllocator(CMK_LSRA))
{
}

PhaseStatus WasmRegAlloc::doRegisterAllocation()
{
    for (unsigned i = 0; i < m_compiler->compFuncCount(); i++)
    {
        m_perFuncletData[i] = new (m_compiler->getAllocator(CMK_LSRA)) PerFuncletData(m_compiler);
    }

    IdentifyCandidates();
    CollectReferences();
    ResolveReferences();
    PublishAllocationResults();

    return PhaseStatus::MODIFIED_EVERYTHING;
}

//------------------------------------------------------------------------
// recordVarLocationsAtStartOfBB: update enregistered local vars to
//   reflect the current register assignment
//
// Arguments:
//   bb - the basic block whose start is being processed
//
// Notes:
//   This relies on m_currentFunclet to try and avoid work in some cases.
//
void WasmRegAlloc::recordVarLocationsAtStartOfBB(BasicBlock* bb)
{
    // Register assignments only change at funclet boundaries
    //
    bool const isFuncEntry    = m_compiler->fgFirstBB == bb;
    bool const isFuncletEntry = m_compiler->bbIsFuncletBeg(bb);

    if (!isFuncletEntry && !isFuncEntry)
    {
        return;
    }

    unsigned const funcIdx = isFuncEntry ? ROOT_FUNC_IDX : m_compiler->funGetFuncIdx(bb);

    // Walk all the assignments for this funclet, and update or verify the LclVarDscs accordingly.
    //
    auto updateOrVerifyAssignments = [=](bool verify = false) {
        PerFuncletData* const            funcData      = m_perFuncletData[funcIdx];
        const jitstd::vector<regNumber>& assignments   = funcData->m_physicalRegAssignments;
        bool                             hasAssignment = false;

        if (isFuncletEntry)
        {
            JITDUMP("%s Var Locations to start of funclet %u entry " FMT_BB "\n", verify ? "Reporting" : "Updating",
                    funcIdx, bb->bbNum);
        }
        else
        {
            JITDUMP("%s Var Locations to start of method entry " FMT_BB "\n", verify ? "Reporting" : "Updating",
                    bb->bbNum);
        }

        for (unsigned varIndex = 0; varIndex < assignments.size(); varIndex++)
        {
            unsigned const   lclNum = m_compiler->lvaTrackedIndexToLclNum(varIndex);
            LclVarDsc* const varDsc = m_compiler->lvaGetDesc(lclNum);
            regNumber const  reg    = assignments[varIndex];

            if (verify)
            {
                assert(varDsc->GetRegNum() == reg);
            }
            else
            {
                varDsc->SetRegNum(reg);

                // Unlike LSRA, we do not change assignments within a funclet.
                // And no locals are live across a funclet boundary. So there
                // is no need for any debug liveness update here.
            }

            if (reg != REG_STK)
            {
                JITDUMP("  V%02u(%s)", lclNum, getRegName(reg));
                hasAssignment = true;
            }
        }

        JITDUMP("%s\n", hasAssignment ? "" : "  <none>");
    };

    // The current assignments may already hold the desired state.
    //
    if (m_currentFunclet == funcIdx)
    {
#ifdef DEBUG
        // No work required, just verify/dump the current state
        updateOrVerifyAssignments(/* verify */ true);
#endif // DEBUG

        return;
    }

    updateOrVerifyAssignments();

    // Record what the LclVarDsc assignments hold.
    //
    m_currentFunclet = funcIdx;
}

bool WasmRegAlloc::willEnregisterLocalVars() const
{
    return m_compiler->compEnregLocals();
}

#if TRACK_LSRA_STATS
void WasmRegAlloc::dumpLsraStatsCsv(FILE* file)
{
}

void WasmRegAlloc::dumpLsraStatsSummary(FILE* file)
{
}
#endif // TRACK_LSRA_STATS

//------------------------------------------------------------------------
// CurrentRange: Get the LIR range under current processing.
//
// Return Value:
//    The LIR range currently being processed.
//
LIR::Range& WasmRegAlloc::CurrentRange()
{
    return LIR::AsRange(m_currentBlock);
}

//------------------------------------------------------------------------
// IdentifyCandidates: Identify locals eligible for register allocation.
//
// Also allocate them to virtual registers.
//
void WasmRegAlloc::IdentifyCandidates()
{
    InitializeStackPointer();

    bool anyFrameLocals = false;
    for (unsigned lclNum = 0; lclNum < m_compiler->lvaCount; lclNum++)
    {
        LclVarDsc* varDsc = m_compiler->lvaGetDesc(lclNum);
        varDsc->SetRegNum(REG_STK);

        if (isRegCandidate(varDsc))
        {
            JITDUMP("RA candidate: V%02u\n", lclNum);
            InitializeCandidate(varDsc);
        }
        else if (varDsc->lvRefCnt() != 0)
        {
            anyFrameLocals = true;
        }
    }

    if (anyFrameLocals || m_compiler->compLocallocUsed || (m_compiler->compFuncCount() > 1))
    {
        AllocateFramePointer();
    }
}

//------------------------------------------------------------------------
// InitializeCandidate: Initialize the candidate local.
//
// Allocates a virtual register for this local.
//
// Arguments:
//    varDsc - The local's descriptor
//
void WasmRegAlloc::InitializeCandidate(LclVarDsc* varDsc)
{
    regNumber reg = AllocateVirtualRegister(varDsc->GetRegisterType());
    varDsc->SetRegNum(reg);
    varDsc->lvLRACandidate = true;
}

//------------------------------------------------------------------------
// InitializeStackPointer: Initialize the stack pointer local.
//
// The stack pointer (as referenced in IR) presents a bit of a problem for
// the allocator. We don't have precise liveness for it due to the various
// implicit uses, so it can't be a complete candidate. At the same time, we
// don't want to needlessly spill it to the stack even in debug code.
// We solve these problems by neutering the SP local descriptor to represent
// an unreferenced local and rewriting all references to it into PHYS_REGs.
//
// It is a fudge, but this way we don't need to introduce any new contracts
// between RA and codegen beyond "SetStackPointerReg".
//
void WasmRegAlloc::InitializeStackPointer()
{
    LclVarDsc* spVarDsc = m_compiler->lvaGetDesc(m_compiler->lvaWasmSpArg);
    assert(spVarDsc->lvRefCnt() != 0); // TODO-WASM-RA-CQ: precise usage tracking for SP.

    // We don't neuter the live sets and such since that's currently not needed.
    spVarDsc->lvImplicitlyReferenced = false;
    spVarDsc->setLvRefCnt(0);

    AllocateStackPointer();
}

//------------------------------------------------------------------------
// AllocateStackPointer: Allocate a virtual register for the SP.
//
void WasmRegAlloc::AllocateStackPointer()
{
    // This is the same virtual register in all regions
    //
    if (m_perFuncletData[ROOT_FUNC_IDX]->m_spReg == REG_NA)
    {
        regNumber spReg = AllocateVirtualRegister(TYP_I_IMPL);

        for (unsigned i = 0; i < m_compiler->compFuncCount(); i++)
        {
            m_perFuncletData[i]->m_spReg = spReg;
        }
    }
}

//------------------------------------------------------------------------
// AllocateFramePointer: Allocate a virtual register for the FP.
//
void WasmRegAlloc::AllocateFramePointer()
{
    // FP is initialized with SP in the prolog, so ensure the latter is allocated.
    AllocateStackPointer();

    regNumber const spReg = m_perFuncletData[ROOT_FUNC_IDX]->m_spReg;

    bool const      needUniqueFpReg = m_compiler->compLocallocUsed || (m_compiler->compFuncCount() > 1);
    regNumber const fpReg           = needUniqueFpReg ? AllocateVirtualRegister(TYP_I_IMPL) : REG_NA;

    // Main method can use SP for frame access, if there is no localloc.
    //
    if (m_compiler->compLocallocUsed)
    {
        m_perFuncletData[ROOT_FUNC_IDX]->m_fpReg = fpReg;
    }
    else
    {
        m_perFuncletData[ROOT_FUNC_IDX]->m_fpReg = spReg;
    }

    // Funclets must always use a distinct FP for frame access
    //
    for (unsigned i = 1; i < m_compiler->compFuncCount(); i++)
    {
        m_perFuncletData[i]->m_fpReg = fpReg;
    }
}

//------------------------------------------------------------------------
// AllocateVirtualRegister: Allocate a new virtual register.
//
// Arguments:
//    type - The register's type
//
regNumber WasmRegAlloc::AllocateVirtualRegister(var_types type)
{
    WasmValueType wasmType = ActualTypeToWasmValueType(type);
    return AllocateVirtualRegister(wasmType);
}

//------------------------------------------------------------------------
// AllocateVirtualRegister: Allocate a new virtual register.
//
// Arguments:
//    type - The register's WASM type
//
// Return Value:
//    The allocated register.
//
regNumber WasmRegAlloc::AllocateVirtualRegister(WasmValueType type)
{
    VirtualRegStack* virtRegs = &m_virtualRegs[static_cast<unsigned>(type)];
    if (!virtRegs->IsInitialized())
    {
        *virtRegs = VirtualRegStack(type);
    }
    regNumber virtReg = virtRegs->Push();
    return virtReg;
}

//------------------------------------------------------------------------
// AllocateTemporaryRegister: Allocate a new temporary (virtual) register.
//
// Arguments:
//    type - The register's type
//
// Return Value:
//    The allocated register.
//
// Notes:
//    Temporary (virtual) regisers live in an index space different from
//    the ordinary virtual registers, to which they are mapped just before
//    resolution.
//
regNumber WasmRegAlloc::AllocateTemporaryRegister(var_types type)
{
    WasmValueType wasmType = ActualTypeToWasmValueType(type);
    unsigned      index    = m_temporaryRegs[static_cast<unsigned>(wasmType)].Push();
    return MakeWasmReg(index, wasmType);
}

//------------------------------------------------------------------------
// ReleaseTemporaryRegister: Release the most recently allocated temporary register.
//
// Arguments:
//    type - The register's type
//
// Return Value:
//    The released register.
//
regNumber WasmRegAlloc::ReleaseTemporaryRegister(var_types type)
{
    WasmValueType wasmType = TypeToWasmValueType(type);
    return ReleaseTemporaryRegister(wasmType);
}

//------------------------------------------------------------------------
// ReleaseTemporaryRegister: Release the most recently allocated temporary register.
//
// Arguments:
//    wasmType - The register's wasm type
//
// Return Value:
//    The released register.
//
regNumber WasmRegAlloc::ReleaseTemporaryRegister(WasmValueType wasmType)
{
    unsigned index = m_temporaryRegs[static_cast<unsigned>(wasmType)].Pop();
    return MakeWasmReg(index, wasmType);
}

//------------------------------------------------------------------------
// CollectReferences: collect candidate references and rewrite the IR.
//
void WasmRegAlloc::CollectReferences()
{
    for (BasicBlock* block : m_compiler->Blocks())
    {
        CollectReferencesForBlock(block);
    }
}

//------------------------------------------------------------------------
// CollectReferencesForBlock: collect candidate references and rewrite the IR for one block.
//
// Arguments:
//    block - The block
//
void WasmRegAlloc::CollectReferencesForBlock(BasicBlock* block)
{
    m_currentBlock = block;

    if (m_compiler->bbIsFuncletBeg(block))
    {
        m_currentFunclet = m_compiler->funGetFuncIdx(block);
    }

    // We may modify the range while iterating.
    //
    // For now, we assume reordering happens only for already visited
    // nodes, and any newly introduced nodes do not need collection.
    //
    GenTree* node = LIR::AsRange(block).FirstNode();

    while (node != nullptr)
    {
        GenTree* nextNode = node->gtNext;
        CollectReferencesForNode(node);
        node = nextNode;
    }

    m_currentBlock = nullptr;
}

//------------------------------------------------------------------------
// CollectReferencesForNode: collect candidate references and rewrite the IR for one node.
//
// Arguments:
//    node - The IR node
//
void WasmRegAlloc::CollectReferencesForNode(GenTree* node)
{
    switch (node->OperGet())
    {
        case GT_LCL_VAR:
            CollectReferencesForLclVar(node->AsLclVar());
            break;

        case GT_STORE_LCL_VAR:
        case GT_STORE_LCL_FLD:
        {
            LclVarDsc* varDsc = m_compiler->lvaGetDesc(node->AsLclVarCommon());
            if (varDsc->lvIsRegCandidate())
            {
                assert(node->OperIs(GT_STORE_LCL_VAR));
                CollectReference(node->AsLclVarCommon());
            }
            else
            {
                RewriteLocalStackStore(node->AsLclVarCommon());
            }
        }
        break;

        case GT_DIV:
        case GT_UDIV:
        case GT_MOD:
        case GT_UMOD:
            CollectReferencesForDivMod(node->AsOp());
            break;

        case GT_LCLHEAP:
            CollectReferencesForLclHeap(node->AsOp());
            break;

        case GT_CALL:
            CollectReferencesForCall(node->AsCall());
            break;

        case GT_CAST:
            CollectReferencesForCast(node->AsOp());
            break;

        case GT_ADD:
        case GT_SUB:
        case GT_MUL:
            CollectReferencesForBinop(node->AsOp());
            break;

        case GT_STOREIND:
            CollectReferencesForStoreInd(node->AsStoreInd());
            break;

        case GT_STORE_BLK:
            CollectReferencesForBlockStore(node->AsBlk());
            break;

        case GT_INDEX_ADDR:
            CollectReferencesForIndexAddr(node->AsIndexAddr());
            break;

        default:
            assert(!node->OperIsLocalStore());
            break;
    }

    RequestTemporaryRegisterForMultiplyUsedNode(node);
}

//------------------------------------------------------------------------
// CollectReferencesForDivMod: Collect virtual register references for a DIV/MOD.
//
// Consumes temporary registers for the div-by-zero and overflow checks.
//
// Arguments:
//    divModNode - The [U]DIV/[U]MOD node
//
void WasmRegAlloc::CollectReferencesForDivMod(GenTreeOp* divModNode)
{
    ConsumeTemporaryRegForOperand(divModNode->gtGetOp2() DEBUGARG("div-by-zero / overflow check"));
    ConsumeTemporaryRegForOperand(divModNode->gtGetOp1() DEBUGARG("div-by-zero / overflow check"));
}

//------------------------------------------------------------------------
// CollectReferencesForLclHeap: Collect virtual register references for a LCLHEAP.
//
// Reserves internal register for unknown-sized LCLHEAP operations
//
// Arguments:
//    lclHeapNode - The LCLHEAP node
//
void WasmRegAlloc::CollectReferencesForLclHeap(GenTreeOp* lclHeapNode)
{
    // Known-sized allocations have contained size operand, so they don't require internal register.
    if (!lclHeapNode->gtGetOp1()->isContainedIntOrIImmed())
    {
        regNumber internalReg = RequestInternalRegister(lclHeapNode, TYP_I_IMPL);
        regNumber releasedReg = ReleaseTemporaryRegister(WasmRegToType(internalReg));
        assert(releasedReg == internalReg);
    }
}

//------------------------------------------------------------------------
// CollectReferencesForIndexAddr: Collect virtual register references for an INDEX_ADDR.
//
// Reserves temporary registers for bounds-checked INDEX_ADDR operations
//
// Arguments:
//    indexAddrNode - The INDEX_ADDR node
//
void WasmRegAlloc::CollectReferencesForIndexAddr(GenTreeIndexAddr* indexAddrNode)
{
    // Bounds checking requires both operands be used multiple times.
    //
    ConsumeTemporaryRegForOperand(indexAddrNode->Index() DEBUGARG("bounds check"));
    ConsumeTemporaryRegForOperand(indexAddrNode->Arr() DEBUGARG("bounds check"));
}

//------------------------------------------------------------------------
// CollectReferencesForCall: Collect virtual register references for a call.
//
// Consumes temporary registers for a call.
//
// Arguments:
//    callNode - The GT_CALL node
//
void WasmRegAlloc::CollectReferencesForCall(GenTreeCall* callNode)
{
    CallArg* thisArg = callNode->gtArgs.GetThisArg();

    if (thisArg != nullptr)
    {
        ConsumeTemporaryRegForOperand(thisArg->GetNode() DEBUGARG("call this argument"));
    }
}

//------------------------------------------------------------------------
// CollectReferencesForCast: Collect virtual register references for a cast.
//
// Consumes temporary registers for a cast.
//
// Arguments:
//    castNode - The GT_CAST node
//
void WasmRegAlloc::CollectReferencesForCast(GenTreeOp* castNode)
{
    ConsumeTemporaryRegForOperand(castNode->gtGetOp1() DEBUGARG("cast overflow check"));
}

//------------------------------------------------------------------------
// CollectReferencesForBinop: Collect virtual register references for a binary operation.
//
// Consumes temporary registers for a binary operation.
//
// Arguments:
//    binopNode - The binary operation node
//
void WasmRegAlloc::CollectReferencesForBinop(GenTreeOp* binopNode)
{
    regNumber internalReg = REG_NA;
    if (binopNode->gtOverflow())
    {
        if (binopNode->OperIs(GT_ADD) || binopNode->OperIs(GT_SUB))
        {
            internalReg = RequestInternalRegister(binopNode, binopNode->TypeGet());
        }
        else if (binopNode->OperIs(GT_MUL))
        {
            assert(binopNode->TypeIs(TYP_INT));
            internalReg = RequestInternalRegister(binopNode, TYP_LONG);
        }
    }

    if (internalReg != REG_NA)
    {
        regNumber releasedReg = ReleaseTemporaryRegister(WasmRegToType(internalReg));
        assert(releasedReg == internalReg);
    }

    ConsumeTemporaryRegForOperand(binopNode->gtGetOp2() DEBUGARG("binop overflow check"));
    ConsumeTemporaryRegForOperand(binopNode->gtGetOp1() DEBUGARG("binop overflow check"));
}

//------------------------------------------------------------------------
// CollectReferencesForStoreInd: Collect virtual register references for an indirect store
//
// Arguments:
//    node - The GT_STOREIND node
//
void WasmRegAlloc::CollectReferencesForStoreInd(GenTreeStoreInd* node)
{
    GenTree* const addr = node->Addr();
    ConsumeTemporaryRegForOperand(addr DEBUGARG("storeind null check"));
}

//------------------------------------------------------------------------
// CollectReferencesForBlockStore: Collect virtual register references for a block store.
//
// Arguments:
//    node - The GT_STORE_BLK node
//
void WasmRegAlloc::CollectReferencesForBlockStore(GenTreeBlk* node)
{
    GenTree* src = node->Data();
    if (src->OperIs(GT_IND))
    {
        src = src->gtGetOp1();
    }

    ConsumeTemporaryRegForOperand(src DEBUGARG("block store source"));
    ConsumeTemporaryRegForOperand(node->Addr() DEBUGARG("block store destination"));
}

//------------------------------------------------------------------------
// CollectReferencesForLclVar: Collect virtual register references for a LCL_VAR.
//
// Rewrites SP references into PHYS_REGs.
//
// Arguments:
//    lclVar - The LCL_VAR node
//
void WasmRegAlloc::CollectReferencesForLclVar(GenTreeLclVar* lclVar)
{
    if (lclVar->GetLclNum() == m_compiler->lvaWasmSpArg)
    {
        lclVar->ChangeOper(GT_PHYSREG);
        lclVar->AsPhysReg()->gtSrcReg = m_perFuncletData[m_currentFunclet]->m_spReg;
        CollectReference(lclVar);
    }
}

//------------------------------------------------------------------------
// RewriteLocalStackStore: rewrite a store to the stack to STOREIND(LCL_ADDR, ...).
//
// This is needed to obey WASM stack ordering constraints: as in IR, the
// address operands comes first and we can't insert that into the middle
// of an already generated instruction stream at codegen time.
//
// Arguments:
//    lclNode - The local store node
//
void WasmRegAlloc::RewriteLocalStackStore(GenTreeLclVarCommon* lclNode)
{
    // At this point, the IR is already stackified, so we just need to find the first node in the dataflow.
    // TODO-WASM-TP: this is nice and simple, but can we do this more efficiently?
    GenTree* value          = lclNode->Data();
    GenTree* insertionPoint = value->gtFirstNodeInOperandOrder();

    // TODO-WASM-RA: figure out the address mode story here. Right now this will produce an address not folded
    // into the store's address mode. We can utilize a contained LEA, but that will require some liveness work.

    var_types storeType = lclNode->TypeGet();
    // We can end up with a block copy operation storing a non-STRUCT into a STRUCT due to type erasure.
    if ((storeType == TYP_STRUCT) && lclNode->OperIsCopyBlkOp())
    {
        LclVarDsc* varDsc     = m_compiler->lvaGetDesc(lclNode->GetLclNum());
        var_types  lclRegType = varDsc->GetRegisterType(lclNode);
        if (lclRegType != TYP_UNDEF)
        {
            storeType = lclRegType;
        }
    }

    bool         isStruct = storeType == TYP_STRUCT;
    uint16_t     offset   = lclNode->GetLclOffs();
    ClassLayout* layout   = isStruct ? lclNode->GetLayout(m_compiler) : nullptr;
    lclNode->SetOper(GT_LCL_ADDR);
    lclNode->ChangeType(TYP_I_IMPL);
    lclNode->AsLclFld()->SetLclOffs(offset);

    GenTree*     store;
    GenTreeFlags indFlags = GTF_IND_NONFAULTING | GTF_IND_TGT_NOT_HEAP;
    if (isStruct)
    {
        store = m_compiler->gtNewStoreBlkNode(layout, lclNode, value, indFlags);
    }
    else
    {
        store = m_compiler->gtNewStoreIndNode(storeType, lclNode, value, indFlags);
    }
    CurrentRange().InsertAfter(lclNode, store);
    CurrentRange().Remove(lclNode);
    CurrentRange().InsertBefore(insertionPoint, lclNode);

    LIR::ReadOnlyRange storeRange(store, store);
    m_compiler->GetLowering()->LowerRange(m_currentBlock, storeRange);

    // FIXME-WASM: Should we be doing this here?
    // CollectReferencesForNode(store);
}

//------------------------------------------------------------------------
// CollectReference: Add 'node' to the candidate reference list.
//
// To be later assigned a physical register.
//
// Arguments:
//    node - The node to collect
//
void WasmRegAlloc::CollectReference(GenTree* node)
{
    PerFuncletData* const data = m_perFuncletData[m_currentFunclet];
    VirtualRegReferences* refs = data->m_virtualRegRefs;

    // We may make multiple consecutive collection calls for the same node.
    // We only want to collect it once.
    //
    if (data->m_lastVirtualRegRefsCount > 0)
    {
        assert(refs != nullptr);
        if (node == refs->Nodes[data->m_lastVirtualRegRefsCount - 1])
        {
            return;
        }
    }

    if (refs == nullptr)
    {
        refs                   = new (m_compiler->getAllocator(CMK_LSRA_RefPosition)) VirtualRegReferences();
        data->m_virtualRegRefs = refs;
    }
    else if (data->m_lastVirtualRegRefsCount == ARRAY_SIZE(refs->Nodes))
    {
        refs                            = new (m_compiler->getAllocator(CMK_LSRA_RefPosition)) VirtualRegReferences();
        refs->Prev                      = data->m_virtualRegRefs;
        data->m_virtualRegRefs          = refs;
        data->m_lastVirtualRegRefsCount = 0;
    }

    assert(data->m_lastVirtualRegRefsCount < ARRAY_SIZE(refs->Nodes));
    refs->Nodes[data->m_lastVirtualRegRefsCount++] = node;
}

//------------------------------------------------------------------------
// RequestTemporaryRegisterForMultiplyUsedNode: request a temporary register for a node with multiple uses.
//
// To be later assigned a physical register.
//
// Arguments:
//    node - A node possibly needing a temporary register
//
void WasmRegAlloc::RequestTemporaryRegisterForMultiplyUsedNode(GenTree* node)
{
    if ((node->gtLIRFlags & LIR::Flags::MultiplyUsed) == LIR::Flags::None)
    {
        return;
    }

    assert(node->IsValue());
    if (node->IsUnusedValue())
    {
        // Liveness removed the parent node - no need to do anything.
        node->gtLIRFlags &= ~LIR::Flags::MultiplyUsed;
        return;
    }

    if (node->OperIs(GT_LCL_VAR) && m_compiler->lvaGetDesc(node->AsLclVar())->lvIsRegCandidate())
    {
        // Will be allocated into its own register, no need for a temporary.
        node->gtLIRFlags &= ~LIR::Flags::MultiplyUsed;
        return;
    }

    // Note how due to the fact we're processing nodes in stack order,
    // we don't need to maintain free/busy sets, only a simple stack.
    regNumber reg = AllocateTemporaryRegister(node->TypeGet());
    assert((node->GetRegNum() == REG_NA) && "Trying to double-assign a temporary register");
    node->SetRegNum(reg);
}

//------------------------------------------------------------------------
// ConsumeTemporaryRegForOperand: Consume the temporary register for a multiply-used operand.
//
// The contract with codegen is that such operands will have their register
// number set to a valid WASM local, to be "local.tee"d after the node is
// generated.
//
// Arguments:
//    operand - The node to consume the register for
//    reason  - What was the register allocated for
//
void WasmRegAlloc::ConsumeTemporaryRegForOperand(GenTree* operand DEBUGARG(const char* reason))
{
    if ((operand->gtLIRFlags & LIR::Flags::MultiplyUsed) == LIR::Flags::None)
    {
        return;
    }

    regNumber reg = ReleaseTemporaryRegister(genActualType(operand));
    assert((reg == operand->GetRegNum()) && "Temporary reg being consumed out of order");
    CollectReference(operand);

    operand->gtLIRFlags &= ~LIR::Flags::MultiplyUsed;
    JITDUMP("Consumed a temporary reg for [%06u]: %s\n", Compiler::dspTreeID(operand), reason);
}

//------------------------------------------------------------------------
// RequestInternalRegister: request an internal register for a node with specific type.
//
// To be later assigned a physical register.
//
// Arguments:
//    node - node whose codegen will need an internal register
//    type - type of the internal register
//
// Returns:
//    reg number of internal register.
//
regNumber WasmRegAlloc::RequestInternalRegister(GenTree* node, var_types type)
{
    regNumber reg = AllocateTemporaryRegister(type);
    m_codeGen->internalRegisters.Add(node, reg);
    CollectReference(node);
    return reg;
}

//------------------------------------------------------------------------
// ResolveReferences: Translate virtual registers to physical ones (WASM locals).
//
// And fill-in the references collected earlier.
//
void WasmRegAlloc::ResolveReferences()
{
    // Finish the allocation by allocating temporary registers to virtual registers.
    struct TemporaryRegBank
    {
        regNumber* Regs;
        unsigned   Count;
    };
    TemporaryRegBank temporaryRegMap[static_cast<unsigned>(WasmValueType::Count)];
    for (WasmValueType type = WasmValueType::First; type < WasmValueType::Count; ++type)
    {
        TemporaryRegStack& temporaryRegs          = m_temporaryRegs[static_cast<unsigned>(type)];
        TemporaryRegBank&  allocatedTemporaryRegs = temporaryRegMap[static_cast<unsigned>(type)];
        assert((temporaryRegs.Count == 0) && "Some temporary regs were not consumed/released");

        allocatedTemporaryRegs.Count = temporaryRegs.MaxCount;
        if (allocatedTemporaryRegs.Count == 0)
        {
            continue;
        }

        allocatedTemporaryRegs.Regs = new (m_compiler->getAllocator(CMK_LSRA)) regNumber[allocatedTemporaryRegs.Count];
        for (unsigned i = 0; i < allocatedTemporaryRegs.Count; i++)
        {
            allocatedTemporaryRegs.Regs[i] = AllocateVirtualRegister(type);
        }
    }

    struct PhysicalRegBank
    {
        unsigned DeclaredCount;
        unsigned IndexBase;
        unsigned Index;
    };

    // Resolve funclet by funclet, in reverse order, so that we process the main method region last.
    //
    for (FuncInfoDsc* const funcInfo : m_compiler->Funcs().Reverse())
    {
        // Make the funclet index available globally
        //
        m_currentFunclet = funcInfo->GetFuncletIdx(m_compiler);

        PhysicalRegBank virtToPhysRegMap[static_cast<unsigned>(WasmValueType::Count)];
        for (WasmValueType type = WasmValueType::First; type < WasmValueType::Count; ++type)
        {
            VirtualRegStack& virtRegs = m_virtualRegs[static_cast<unsigned>(type)];
            PhysicalRegBank& physRegs = virtToPhysRegMap[static_cast<unsigned>(type)];
            physRegs.DeclaredCount    = virtRegs.Count();
        }

        unsigned              indexBase = 0;
        const bool            inFunclet = funcInfo->funKind != FuncKind::FUNC_ROOT;
        PerFuncletData* const data      = m_perFuncletData[m_currentFunclet];
        regNumber const       spVirtReg = data->m_spReg;
        regNumber const       fpVirtReg = data->m_fpReg;

        switch (funcInfo->funKind)
        {
            case FuncKind::FUNC_ROOT:
            {
                for (unsigned argLclNum = 0; argLclNum < m_compiler->info.compArgsCount; argLclNum++)
                {
                    const ABIPassingInformation& abiInfo = m_compiler->lvaGetParameterABIInfo(argLclNum);
                    for (const ABIPassingSegment& segment : abiInfo.Segments())
                    {
                        if (segment.IsPassedInRegister())
                        {
                            WasmValueType argType;
                            regNumber     argReg   = segment.GetRegister();
                            unsigned      argIndex = UnpackWasmReg(argReg, &argType);
                            indexBase              = max(indexBase, argIndex + 1);

                            LclVarDsc* argVarDsc = m_compiler->lvaGetDesc(argLclNum);
                            if ((argVarDsc->GetRegNum() == argReg) || (data->m_spReg == argReg))
                            {
                                assert(abiInfo.HasExactlyOneRegisterSegment());
                                virtToPhysRegMap[static_cast<unsigned>(argType)].DeclaredCount--;
                            }

                            const ParameterRegisterLocalMapping* mapping =
                                m_compiler->FindParameterRegisterLocalMappingByRegister(argReg);
                            if ((mapping != nullptr) &&
                                (m_compiler->lvaGetDesc(mapping->LclNum)->GetRegNum() == argReg))
                            {
                                virtToPhysRegMap[static_cast<unsigned>(argType)].DeclaredCount--;
                            }
                        }
                    }
                }
            }
            break;

            case FuncKind::FUNC_HANDLER:
            case FuncKind::FUNC_FILTER:
            {
                // TODO-WASM: add ABI information for funclets?
                //
                WasmValueType argType = TypeToWasmValueType(TYP_I_IMPL);

                if (spVirtReg != REG_NA)
                {
                    virtToPhysRegMap[static_cast<unsigned>(argType)].DeclaredCount--;
                }

                if ((fpVirtReg != REG_NA) && (fpVirtReg != spVirtReg))
                {
                    virtToPhysRegMap[static_cast<unsigned>(argType)].DeclaredCount--;
                }

                EHblkDsc* const ehDsc = funcInfo->GetEHDesc(m_compiler);
                indexBase             = ehDsc->HasCatchHandler() ? 3 : 2;
            }

            break;

            default:
                unreached();
        }

        for (WasmValueType type = WasmValueType::First; type < WasmValueType::Count; ++type)
        {
            PhysicalRegBank& physRegs = virtToPhysRegMap[static_cast<unsigned>(type)];
            physRegs.IndexBase        = indexBase;
            physRegs.Index            = indexBase;
            indexBase += physRegs.DeclaredCount;
        }

        // Allocate all our virtual registers to physical ones.
        //
        auto allocPhysReg = [&](regNumber virtReg, LclVarDsc* varDsc) {
            regNumber physReg = REG_NA;

            if (!inFunclet)
            {
                // ABI registers in the main method
                //
                if ((varDsc != nullptr) && varDsc->lvIsRegArg && !varDsc->lvIsStructField)
                {
                    unsigned                     lclNum  = m_compiler->lvaGetLclNum(varDsc);
                    const ABIPassingInformation& abiInfo = m_compiler->lvaGetParameterABIInfo(lclNum);
                    assert(abiInfo.HasExactlyOneRegisterSegment());
                    physReg = abiInfo.Segment(0).GetRegister();
                }
                else if ((varDsc != nullptr) && varDsc->lvIsParamRegTarget)
                {
                    unsigned                             lclNum = m_compiler->lvaGetLclNum(varDsc);
                    const ParameterRegisterLocalMapping* mapping =
                        m_compiler->FindParameterRegisterLocalMappingByLocal(lclNum, 0);
                    assert(mapping != nullptr);
                    physReg = mapping->RegisterSegment->GetRegister();
                }
            }
            else
            {
                // ABI registers in the funclets
                if (virtReg == spVirtReg)
                {
                    physReg = MakeWasmReg(0, TypeToWasmValueType(TYP_I_IMPL));
                }
                else if (virtReg == fpVirtReg)
                {
                    physReg = MakeWasmReg(1, TypeToWasmValueType(TYP_I_IMPL));
                }
            }

            if (physReg == REG_NA)
            {
                WasmValueType type         = WasmRegToType(virtReg);
                unsigned      physRegIndex = virtToPhysRegMap[static_cast<unsigned>(type)].Index++;
                physReg                    = MakeWasmReg(physRegIndex, type);
            }

            assert(genIsValidReg(physReg));
            if ((varDsc != nullptr) && varDsc->lvIsRegCandidate())
            {
                data->m_physicalRegAssignments[varDsc->lvVarIndex] = physReg;
            }
            return physReg;
        };

        // Map SP and FP to physical registers.
        //
        if (spVirtReg != REG_NA)
        {
            data->m_spReg = allocPhysReg(spVirtReg, m_compiler->lvaGetDesc(m_compiler->lvaWasmSpArg));
        }

        if (fpVirtReg != REG_NA)
        {
            data->m_fpReg = (spVirtReg == fpVirtReg) ? data->m_spReg : allocPhysReg(fpVirtReg, nullptr);
        }

        // Do likewise for the enregistered locals.
        //
        // Note we do not update the LclVarDsc here.
        //
        // We will update the LclVarDsc at the end of this method to be the assignments
        // for the main method.
        //
        // Then when codegen calls back to the allocator via recordVarLocationsAtStartOfBB
        // we will either verify the assignments are correct for the block, or update them
        // using the per-funclet mappings kept in the per-funclet m_physicalRegAssignments.
        //
        for (unsigned varIndex = 0; varIndex < m_compiler->lvaTrackedCount; varIndex++)
        {
            unsigned   lclNum = m_compiler->lvaTrackedIndexToLclNum(varIndex);
            LclVarDsc* varDsc = m_compiler->lvaGetDesc(lclNum);

            if (lclNum == m_compiler->lvaWasmSpArg)
            {
                // Allocation was handled above.
            }
            else if (!varDsc->lvIsRegCandidate())
            {
                continue;
            }
            else
            {
                allocPhysReg(varDsc->GetRegNum(), varDsc);
            }
        }

        for (WasmValueType type = WasmValueType::First; type < WasmValueType::Count; ++type)
        {
            TemporaryRegBank& regs = temporaryRegMap[static_cast<unsigned>(type)];
            for (unsigned i = 0; i < regs.Count; i++)
            {
                regs.Regs[i] = allocPhysReg(regs.Regs[i], nullptr);
            }
        }

        // Now remap all remaining virtual register references.
        unsigned refsCount = data->m_lastVirtualRegRefsCount;
        for (VirtualRegReferences* refs = data->m_virtualRegRefs; refs != nullptr; refs = refs->Prev)
        {
            for (size_t i = 0; i < refsCount; i++)
            {
                GenTree* const node = refs->Nodes[i];

                if (node->OperIs(GT_PHYSREG))
                {
                    assert(node->AsPhysReg()->gtSrcReg == spVirtReg);
                    node->AsPhysReg()->gtSrcReg = data->m_spReg;
                    assert(!genIsValidReg(node->GetRegNum())); // Currently we do not need to multi-use any PHYSREGs.
                    continue;
                }

                // Since we now collect references for nodes with internal registers, we may see
                // cases where the node itself does not have a valid reg.
                //
                regNumber physReg = REG_NA;
                if (node->OperIs(GT_STORE_LCL_VAR))
                {
                    LclVarDsc* const varDsc = m_compiler->lvaGetDesc(node->AsLclVarCommon());
                    physReg                 = data->m_physicalRegAssignments[varDsc->lvVarIndex];
                }
                else if (genIsValidReg(node->GetRegNum()))
                {
                    assert(!node->OperIsLocal() || !m_compiler->lvaGetDesc(node->AsLclVarCommon())->lvIsRegCandidate());
                    WasmValueType type;
                    unsigned      index = UnpackWasmReg(node->GetRegNum(), &type);
                    physReg             = temporaryRegMap[static_cast<unsigned>(type)].Regs[index];
                }

                if (physReg != REG_NA)
                {
                    node->SetRegNum(physReg);
                }

                // If there are internal registers associated with this node, allocate them now.
                //
                InternalRegs* const internalRegs = m_codeGen->internalRegisters.GetAll(node);

                if (internalRegs != nullptr)
                {
                    unsigned count = internalRegs->Count();
                    for (unsigned i = 0; i < count; i++)
                    {
                        WasmValueType type;
                        unsigned      index   = UnpackWasmReg(internalRegs->GetAt(i), &type);
                        regNumber     physReg = temporaryRegMap[static_cast<unsigned>(type)].Regs[index];
                        internalRegs->SetAt(i, physReg);
                    }
                }
            }
            refsCount = ARRAY_SIZE(refs->Nodes);
        }

        // Set up the per-funclet local info Wasm needs
        //
        assert(funcInfo->funWasmLocalDecls == nullptr);

        jitstd::vector<FuncInfoDsc::WasmLocalsDecl>* decls = new (m_compiler->getAllocator(CMK_Codegen))
            jitstd::vector<FuncInfoDsc::WasmLocalsDecl>(m_compiler->getAllocator(CMK_Codegen));
        funcInfo->funWasmLocalDecls = decls;

        for (WasmValueType type = WasmValueType::First; type < WasmValueType::Count; ++type)
        {
            PhysicalRegBank& physRegs = virtToPhysRegMap[static_cast<unsigned>(type)];
            if (physRegs.DeclaredCount != 0)
            {
                decls->push_back({type, physRegs.DeclaredCount});
            }
        }
    }

    // Set all lcl var assignments to the main method's allocations.
    //
    const jitstd::vector<regNumber>& mainFuncAssignment = m_perFuncletData[ROOT_FUNC_IDX]->m_physicalRegAssignments;
    for (unsigned varIndex = 0; varIndex < m_compiler->lvaTrackedCount; varIndex++)
    {
        LclVarDsc* const varDsc      = m_compiler->lvaGetDescByTrackedIndex(varIndex);
        regNumber const  assignedReg = mainFuncAssignment[varIndex];

        if (genIsValidReg(assignedReg))
        {
            varDsc->SetRegNum(mainFuncAssignment[varIndex]);

            // While register allocations are fixed per-funclet, they are unlikely
            // to agree across all funclets.
            //
            varDsc->lvRegister = (m_compiler->compFuncCount() == 1);
            varDsc->lvOnFrame  = false;

            if (varDsc->lvIsParam || varDsc->lvIsParamRegTarget)
            {
                // This is the register codegen will move the local to from its
                // ABI location in main function's prolog.
                //
                varDsc->SetArgInitReg(assignedReg);
            }
        }
    }
}

//------------------------------------------------------------------------
// PublishAllocationResults: Publish the results of register allocation.
//
// Initializes various fields denoting allocation outcomes on the codegen object.
//
void WasmRegAlloc::PublishAllocationResults()
{
    bool usesFramePointer = false;

    for (unsigned i = 0; i < m_compiler->compFuncCount(); i++)
    {
        PerFuncletData* const data = m_perFuncletData[i];

#ifdef DEBUG
        if (i == ROOT_FUNC_IDX)
        {
            if (data->m_spReg != REG_NA)
            {
                JITDUMP("Allocated function SP into: %s\n", getRegName(data->m_spReg));
            }

            if (data->m_fpReg != REG_NA)
            {
                JITDUMP("Allocated function FP into: %s\n", getRegName(data->m_fpReg));
            }
        }
        else
        {
            JITDUMP("Allocated funclet %u SP into %s\n", i, getRegName(data->m_spReg));
            JITDUMP("Allocated funclet %u FP into %s\n", i, getRegName(data->m_fpReg));
        }
#endif // DEBUG

        if (data->m_spReg != REG_NA)
        {
            m_codeGen->SetStackPointerReg(i, data->m_spReg);
        }

        if (data->m_fpReg != REG_NA)
        {
            m_codeGen->SetFramePointerReg(i, data->m_fpReg);
            usesFramePointer = true;
        }
    }

    m_codeGen->setFramePointerUsed(usesFramePointer);

    m_compiler->raMarkStkVars();
    m_compiler->compRegAllocDone = true;
}
