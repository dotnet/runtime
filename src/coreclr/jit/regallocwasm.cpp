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
    , m_currentFunclet(0)
    , m_perRegionData(compiler->compFuncInfoCount, nullptr, compiler->getAllocator(CMK_LSRA))
{
    for (unsigned i = 0; i < m_compiler->compFuncInfoCount; i++)
    {
        m_perRegionData[i] = new (compiler->getAllocator(CMK_LSRA)) PerRegionData();
    }
}

PhaseStatus WasmRegAlloc::doRegisterAllocation()
{
    IdentifyCandidates();
    CollectReferences();
    ResolveReferences();
    PublishAllocationResults();

    return PhaseStatus::MODIFIED_EVERYTHING;
}

void WasmRegAlloc::recordVarLocationsAtStartOfBB(BasicBlock* bb)
{
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

    if (anyFrameLocals || m_compiler->compLocallocUsed)
    {
        AllocateFramePointer();
    }

    if (m_compiler->compFuncInfoCount > 1)
    {
        AllocateExceptionPointer();
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
    if (m_perRegionData[0]->m_spReg == REG_NA)
    {
        regNumber spReg = AllocateVirtualRegister(TYP_I_IMPL);

        for (unsigned i = 0; i < m_compiler->compFuncInfoCount; i++)
        {
            m_perRegionData[i]->m_spReg = spReg;
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

    regNumber const spReg = m_perRegionData[0]->m_spReg;
    regNumber const fpReg = AllocateVirtualRegister(TYP_I_IMPL);

    // Main method can use SP for frame access, if there is no localloc.
    //
    m_perRegionData[0]->m_fpReg = m_compiler->compLocallocUsed ? fpReg : spReg;

    // Funclets must always use FP for frame access
    //
    for (unsigned i = 1; i < m_compiler->compFuncInfoCount; i++)
    {
        m_perRegionData[i]->m_fpReg = fpReg;
    }
}

//------------------------------------------------------------------------
// AllocateExceptionPointer: Allocate a virtual register for the exception pointer.
//
void WasmRegAlloc::AllocateExceptionPointer()
{
    regNumber exReg = REG_NA;

    for (unsigned i = 0; i < m_compiler->compFuncInfoCount; i++)
    {
        const FuncInfoDsc& funcInfo = m_compiler->compFuncInfos[i];

        if (funcInfo.funKind == FuncKind::FUNC_ROOT)
        {
            continue;
        }

        EHblkDsc* const ehDsc = m_compiler->ehGetDsc(funcInfo.funEHIndex);

        if (ehDsc->HasCatchHandler())
        {
            if (exReg == REG_NA)
            {
                exReg = AllocateVirtualRegister(TYP_REF);
            }

            m_perRegionData[i]->m_exReg = exReg;
        }
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

        case GT_CATCH_ARG:
            CollectReferencesForCatchArg(node);
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
        lclVar->AsPhysReg()->gtSrcReg = m_perRegionData[m_currentFunclet]->m_spReg;
        CollectReference(lclVar);
    }
}

//------------------------------------------------------------------------
// CollectReferencesForCatchArg: Collect virtual register references for a CATCH_ARG node.
//
// Rewrites SP references into PHYS_REGs.
//
// Arguments:
//    catchArg - The CATCH_ARG node
//
void WasmRegAlloc::CollectReferencesForCatchArg(GenTree* catchArg)
{
    catchArg->ChangeOper(GT_PHYSREG);
    catchArg->AsPhysReg()->gtSrcReg = m_perRegionData[m_currentFunclet]->m_exReg;
    CollectReference(catchArg);
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
    PerRegionData* const  data = m_perRegionData[m_currentFunclet];
    VirtualRegReferences* refs = data->m_virtualRegRefs;
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
    m_codeGen->internalRegisters.Add(m_currentFunclet, node, reg);
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

    // Process funclet by funclet
    //
    for (m_currentFunclet = 0; m_currentFunclet < m_compiler->compFuncInfoCount; m_currentFunclet++)
    {
        PhysicalRegBank virtToPhysRegMap[static_cast<unsigned>(WasmValueType::Count)];
        for (WasmValueType type = WasmValueType::First; type < WasmValueType::Count; ++type)
        {
            VirtualRegStack& virtRegs = m_virtualRegs[static_cast<unsigned>(type)];
            PhysicalRegBank& physRegs = virtToPhysRegMap[static_cast<unsigned>(type)];
            physRegs.DeclaredCount    = virtRegs.Count();
        }

        unsigned             indexBase = 0;
        const FuncInfoDsc&   funcInfo  = m_compiler->compFuncInfos[m_currentFunclet];
        const bool           inFunclet = funcInfo.funKind != FuncKind::FUNC_ROOT;
        PerRegionData* const data      = m_perRegionData[m_currentFunclet];

        switch (funcInfo.funKind)
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
                // TODO: add ABI information for funclets?
                //
                // All funclets have two intial arguments sp and fp.
                //
                WasmValueType argType = TypeToWasmValueType(TYP_I_IMPL);

                virtToPhysRegMap[static_cast<unsigned>(argType)].DeclaredCount--;
                virtToPhysRegMap[static_cast<unsigned>(argType)].DeclaredCount--;
                indexBase += 2;

                // Filter and catch have a third argument - the exception object.
                //
                EHblkDsc* const eh = m_compiler->ehGetDsc(funcInfo.funEHIndex);
                if ((funcInfo.funKind == FuncKind::FUNC_FILTER) || eh->HasCatchHandler())
                {
                    argType = TypeToWasmValueType(TYP_REF);
                    virtToPhysRegMap[static_cast<unsigned>(argType)].DeclaredCount--;
                    indexBase += 1;
                }
            }

            break;

            default:
                assert(!"Unexpected funclet kind");
                break;
        }

        for (WasmValueType type = WasmValueType::First; type < WasmValueType::Count; ++type)
        {
            PhysicalRegBank& physRegs = virtToPhysRegMap[static_cast<unsigned>(type)];
            physRegs.IndexBase        = indexBase;
            physRegs.Index            = indexBase;
            indexBase += physRegs.DeclaredCount;
        }

        auto allocPhysReg = [&](regNumber virtReg, LclVarDsc* varDsc) {
            regNumber physReg;
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
            else
            {
                WasmValueType type         = WasmRegToType(virtReg);
                unsigned      physRegIndex = virtToPhysRegMap[static_cast<unsigned>(type)].Index++;
                physReg                    = MakeWasmReg(physRegIndex, type);
            }

            assert(genIsValidReg(physReg));
            if ((varDsc != nullptr) && varDsc->lvIsRegCandidate())
            {
                if (varDsc->lvIsParam || varDsc->lvIsParamRegTarget)
                {
                    // This is the register codegen will move the local from its ABI location in prolog.
                    varDsc->SetArgInitReg(physReg);
                }

                // This is the location for the "first" def. In our case all defs share the same register.
                //
                // TODO-WASM: this may no longer hold with funclets. If a local has disjoint lifetimes that do not
                // cross funclet boundaries, it may need to be allocated to different registers in different funclets.
                //
                // assert(!varDsc->lvRegister);
                varDsc->SetRegNum(physReg);
                varDsc->lvRegister = true;
                varDsc->lvOnFrame  = false;
            }
            return physReg;
        };

        // Allocate all our virtual registers to physical ones.
        //
        regNumber spVirtReg = data->m_spReg;
        regNumber exVirtReg = data->m_exReg;

        if (!inFunclet)
        {
            if (spVirtReg != REG_NA)
            {
                data->m_spReg = allocPhysReg(spVirtReg, m_compiler->lvaGetDesc(m_compiler->lvaWasmSpArg));
            }

            if (data->m_fpReg != REG_NA)
            {
                data->m_fpReg = (spVirtReg == data->m_fpReg) ? data->m_spReg : allocPhysReg(data->m_fpReg, nullptr);
            }

            assert(data->m_exReg == REG_NA);
        }
        else
        {
            // Funclets always have SP and FP, and maybe EX.
            // We do not have lclVars for funclet params.
            // SP is the same physreg as in the main method.
            //
            assert(data->m_spReg != REG_NA);
            data->m_spReg = MakeWasmReg(0, TypeToWasmValueType(TYP_I_IMPL));
            assert(data->m_spReg == m_perRegionData[0]->m_spReg);

            assert(data->m_fpReg != REG_NA);
            data->m_fpReg = MakeWasmReg(1, TypeToWasmValueType(TYP_I_IMPL));

            if (data->m_exReg != REG_NA)
            {
                data->m_exReg = MakeWasmReg(2, TypeToWasmValueType(TYP_REF));
            }
        }

        for (unsigned varIndex = 0; varIndex < m_compiler->lvaTrackedCount; varIndex++)
        {
            unsigned lclNum = m_compiler->lvaTrackedIndexToLclNum(varIndex);
            if (lclNum == m_compiler->lvaWasmSpArg)
            {
                continue; // Handled above.
            }

            LclVarDsc* varDsc = m_compiler->lvaGetDesc(lclNum);
            if (varDsc->lvIsRegCandidate())
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
                GenTree* node = refs->Nodes[i];
                if (node->OperIs(GT_PHYSREG))
                {
                    if (node->AsPhysReg()->gtSrcReg == exVirtReg)
                    {
                        // Former CATCH_ARG -- should only see in funclets
                        assert(inFunclet);
                        node->AsPhysReg()->gtSrcReg = data->m_exReg;
                    }
                    else
                    {
                        assert(node->AsPhysReg()->gtSrcReg == spVirtReg);
                        node->AsPhysReg()->gtSrcReg = data->m_spReg;
                    }

                    assert(!genIsValidReg(node->GetRegNum())); // Currently we do not need to multi-use any PHYSREGs.
                    continue;
                }

                regNumber physReg;
                if (node->OperIs(GT_STORE_LCL_VAR))
                {
                    physReg = m_compiler->lvaGetDesc(node->AsLclVarCommon())->GetRegNum();
                }
                else
                {
                    assert(!node->OperIsLocal() || !m_compiler->lvaGetDesc(node->AsLclVarCommon())->lvIsRegCandidate());
                    WasmValueType type;
                    unsigned      index = UnpackWasmReg(node->GetRegNum(), &type);
                    physReg             = temporaryRegMap[static_cast<unsigned>(type)].Regs[index];
                }

                node->SetRegNum(physReg);
            }
            refsCount = ARRAY_SIZE(refs->Nodes);
        }

        NodeInternalRegistersTable* const internalRegTable = m_codeGen->internalRegisters.GetTable(m_currentFunclet);
        if (internalRegTable != nullptr)
        {
            for (NodeInternalRegistersTable::Node* nodeWithInternalRegs :
                 m_codeGen->internalRegisters.Iterate(internalRegTable))
            {
                InternalRegs* regs  = &nodeWithInternalRegs->GetValueRef();
                unsigned      count = regs->Count();
                for (unsigned i = 0; i < count; i++)
                {
                    WasmValueType type;
                    unsigned      index   = UnpackWasmReg(regs->GetAt(i), &type);
                    regNumber     physReg = temporaryRegMap[static_cast<unsigned>(type)].Regs[index];
                    regs->SetAt(i, physReg);
                }
            }
        }

        assert(m_compiler->codeGen->WasmLocalsDecls[m_currentFunclet] == nullptr);

        jitstd::vector<CodeGenInterface::WasmLocalsDecl>* decls = new (m_compiler->getAllocator(CMK_Codegen))
            jitstd::vector<CodeGenInterface::WasmLocalsDecl>(m_compiler->getAllocator(CMK_Codegen));
        m_compiler->codeGen->WasmLocalsDecls[m_currentFunclet] = decls;

        for (WasmValueType type = WasmValueType::First; type < WasmValueType::Count; ++type)
        {
            PhysicalRegBank& physRegs = virtToPhysRegMap[static_cast<unsigned>(type)];
            if (physRegs.DeclaredCount != 0)
            {
                decls->push_back({type, physRegs.DeclaredCount});
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

    for (unsigned i = 0; i < m_compiler->compFuncInfoCount; i++)
    {
        PerRegionData* const data = m_perRegionData[i];

#ifdef DEBUG
        if (i == 0)
        {
            JITDUMP("Allocated function SP into: %s\n", getRegName(data->m_spReg));

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

        m_codeGen->SetStackPointerReg(i, data->m_spReg);

        if (data->m_fpReg != REG_NA)
        {
            m_codeGen->SetFramePointerReg(i, data->m_fpReg);
            usesFramePointer |= (data->m_fpReg != data->m_spReg);
        }
    }

    m_codeGen->setFramePointerUsed(usesFramePointer);

    // We don't need to publish the exRegs for codegen; all references were in codegen
    // and all were converted to GT_PHYSREG.

    m_compiler->raMarkStkVars();
    m_compiler->compRegAllocDone = true;
}
