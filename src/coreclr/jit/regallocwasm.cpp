// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "regallocwasm.h"

RegAllocInterface* GetRegisterAllocator(Compiler* compiler)
{
    return new (compiler->getAllocator(CMK_LSRA)) WasmRegAlloc(compiler);
}

WasmRegAlloc::WasmRegAlloc(Compiler* compiler)
    : m_compiler(compiler)
{
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

bool WasmRegAlloc::isContainableMemoryOp(GenTree* node)
{
    NYI_WASM("isContainableMemoryOp");
    return false;
}

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
    m_spVarDsc = m_compiler->lvaGetDesc(m_compiler->lvaWasmSpArg);

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

    if (anyFrameLocals)
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
    var_types type = genActualType(varDsc->GetRegisterType());
    regNumber reg  = AllocateVirtualRegister(type);
    varDsc->SetRegNum(reg);
    varDsc->lvLRACandidate = true;
}

//------------------------------------------------------------------------
// AllocateStackPointer: Allocate a virtual register for the SP.
//
regNumber WasmRegAlloc::AllocateStackPointer()
{
    if (m_spVarDsc->lvIsRegCandidate())
    {
        assert(genIsValidReg(m_spVarDsc->GetRegNum())); // Already allocated.
    }
    else
    {
        // For allocation purposes it is convenient to consider the SP always enregistered. Some references to it
        // (e. g. through the frame pointer) are implicit, so we need to track it regardless. This way, the tracking
        // is uniform for candidate and non-candidate cases.
        assert(!genIsValidReg(m_spVarDsc->GetRegNum()));
        m_spVarDsc->SetRegNum(AllocateVirtualRegister(TYP_I_IMPL));
    }
    return m_spVarDsc->GetRegNum();
}

//------------------------------------------------------------------------
// AllocateStackPointer: Allocate a virtual register for the FP.
//
void WasmRegAlloc::AllocateFramePointer()
{
    assert(m_fpReg == REG_NA);

    // FP is initialized with SP in the prolog, so ensure the latter is allocated.
    AllocateStackPointer();

    if (m_compiler->compLocallocUsed)
    {
        m_fpReg = AllocateVirtualRegister(TYP_I_IMPL);
    }
    else
    {
        m_fpReg = m_spVarDsc->GetRegNum();
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
    WasmValueType    wasmType = TypeToWasmValueType(type);
    VirtualRegStack* virtRegs = &m_virtualRegs[static_cast<int>(wasmType)];
    if (!virtRegs->IsInitialized())
    {
        *virtRegs = VirtualRegStack(wasmType);
    }
    regNumber virtReg = virtRegs->Push();
    return virtReg;
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
    for (GenTree* node : LIR::AsRange(block))
    {
        CollectReferencesForNode(node);
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
    if (node->OperIsAnyLocal())
    {
        LclVarDsc* varDsc = m_compiler->lvaGetDesc(node->AsLclVarCommon());
        if (node->OperIsLocalStore())
        {
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
    GenTree*             value = lclNode->Data();
    GenTree*             op    = value;
    GenTree::VisitResult visitResult;
    do
    {
        visitResult = op->VisitOperands([&op](GenTree* operand) {
            op = operand;
            return GenTree::VisitResult::Abort;
        });
    } while (visitResult == GenTree::VisitResult::Abort);

    // TODO-WASM-RA: figure out the address mode story here. Right now this will produce an address not folded
    // into the store's address mode. We can utilize a contained LEA, but that will require some liveness work.
    uint16_t offset = lclNode->GetLclOffs();
    lclNode->SetOper(GT_LCL_ADDR);
    lclNode->ChangeType(TYP_I_IMPL);
    lclNode->AsLclFld()->SetLclOffs(offset);

    GenTree*     store;
    GenTreeFlags indFlags = GTF_IND_NONFAULTING | GTF_IND_TGT_NOT_HEAP;
    if (lclNode->TypeIs(TYP_STRUCT))
    {
        store = m_compiler->gtNewStoreBlkNode(lclNode->GetLayout(m_compiler), lclNode, value, indFlags);
    }
    else
    {
        store = m_compiler->gtNewStoreIndNode(lclNode->TypeGet(), lclNode, value, indFlags);
    }
    CurrentRange().InsertAfter(lclNode, store);
    CurrentRange().Remove(lclNode);
    CurrentRange().InsertBefore(op, lclNode);
}

//------------------------------------------------------------------------
// CollectReference: Add 'node' to the candidate reference list.
//
// To be later assigned a physical register.
//
// Arguments:
//    node - The node to collect
//
void WasmRegAlloc::CollectReference(GenTreeLclVarCommon* node)
{
    VirtualRegReferences* refs = m_virtualRegRefs;
    if (refs == nullptr)
    {
        refs             = new (m_compiler->getAllocator(CMK_LSRA_RefPosition)) VirtualRegReferences();
        m_virtualRegRefs = refs;
    }
    else if (m_lastVirtualRegRefsCount == ARRAY_SIZE(refs->Nodes))
    {
        refs                      = new (m_compiler->getAllocator(CMK_LSRA_RefPosition)) VirtualRegReferences();
        refs->Prev                = m_virtualRegRefs;
        m_virtualRegRefs          = refs;
        m_lastVirtualRegRefsCount = 0;
    }

    assert(m_lastVirtualRegRefsCount < ARRAY_SIZE(refs->Nodes));
    refs->Nodes[m_lastVirtualRegRefsCount++] = node;
}

//------------------------------------------------------------------------
// ResolveReferences: Translate virtual registers to physical ones (WASM locals).
//
// And fill-in the references collected earlier.
//
void WasmRegAlloc::ResolveReferences()
{
    struct PhysicalRegBank
    {
        unsigned DeclaredCount;
        unsigned IndexBase;
        unsigned Index;
    };

    PhysicalRegBank virtToPhysRegMap[static_cast<unsigned>(WasmValueType::Count)];
    for (WasmValueType type = WasmValueType::First; type < WasmValueType::Count; ++type)
    {
        VirtualRegStack& virtRegs = m_virtualRegs[static_cast<unsigned>(type)];
        PhysicalRegBank& physRegs = virtToPhysRegMap[static_cast<unsigned>(type)];
        physRegs.DeclaredCount    = virtRegs.Count();
    }

    unsigned indexBase = 0;
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
                if (argVarDsc->GetRegNum() == argReg)
                {
                    assert(abiInfo.HasExactlyOneRegisterSegment());
                    virtToPhysRegMap[static_cast<unsigned>(argType)].DeclaredCount--;
                }

                const ParameterRegisterLocalMapping* mapping =
                    m_compiler->FindParameterRegisterLocalMappingByRegister(argReg);
                if ((mapping != nullptr) && (m_compiler->lvaGetDesc(mapping->LclNum)->GetRegNum() == argReg))
                {
                    virtToPhysRegMap[static_cast<unsigned>(argType)].DeclaredCount--;
                }
            }
        }
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
        if ((varDsc != nullptr) && varDsc->lvIsRegArg)
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
            varDsc->SetRegNum(physReg);
            varDsc->lvRegister = true;
            varDsc->lvOnFrame  = false;
        }
        return physReg;
    };

    // Remap all our virtual registers to physical ones.
    regNumber spVirtReg = m_spVarDsc->GetRegNum();
    if (genIsValidReg(spVirtReg))
    {
        m_spVarDsc->SetRegNum(allocPhysReg(spVirtReg, m_spVarDsc));
    }
    if (m_fpReg != REG_NA)
    {
        m_fpReg = (spVirtReg == m_fpReg) ? m_spVarDsc->GetRegNum() : allocPhysReg(m_fpReg, nullptr);
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

    unsigned refsCount = m_lastVirtualRegRefsCount;
    for (VirtualRegReferences* refs = m_virtualRegRefs; refs != nullptr; refs = refs->Prev)
    {
        for (size_t i = 0; i < refsCount; i++)
        {
            GenTreeLclVarCommon* node   = refs->Nodes[i];
            LclVarDsc*           varDsc = m_compiler->lvaGetDesc(node);
            node->SetRegNum(varDsc->GetRegNum());
        }
        refsCount = ARRAY_SIZE(refs->Nodes);
    }

    jitstd::vector<CodeGenInterface::WasmLocalsDecl>& decls = m_compiler->codeGen->WasmLocalsDecls;
    for (WasmValueType type = WasmValueType::First; type < WasmValueType::Count; ++type)
    {
        PhysicalRegBank& physRegs = virtToPhysRegMap[static_cast<unsigned>(type)];
        if (physRegs.DeclaredCount != 0)
        {
            decls.push_back({type, physRegs.DeclaredCount});
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
    CodeGenInterface* codeGen = m_compiler->codeGen;
    regNumber         spReg   = m_spVarDsc->GetRegNum();
    if (genIsValidReg(spReg))
    {
        codeGen->SetStackPointerReg(spReg);

        // Revert the RA-local "enregistering" of SP if needed.
        if (!m_spVarDsc->lvIsRegCandidate())
        {
            m_spVarDsc->SetRegNum(REG_STK);
        }
    }
    if (m_fpReg != REG_NA)
    {
        codeGen->SetFramePointerReg(m_fpReg);
        codeGen->setFramePointerUsed(true);
    }
    else
    {
        codeGen->setFramePointerUsed(false);
    }

    m_compiler->raMarkStkVars();
    m_compiler->compRegAllocDone = true;
}
