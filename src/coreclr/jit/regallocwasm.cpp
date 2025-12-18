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
    AllocateAndResolve();
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
// GetCompiler: Get the compiler field.
//
// Bridges the field naming difference for common RA code.
//
// Return Value:
//    The 'this->m_compiler' field.
//
Compiler* WasmRegAlloc::GetCompiler() const
{
    return m_compiler;
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
// IdentifyCandidates: Identify locals eligible for allocation to WASM locals.
//
// Analogous to "LinearScan::identifyCandidates()". Sets the lvLRACandidate
// bit on locals.
//
void WasmRegAlloc::IdentifyCandidates()
{
    for (unsigned lclNum = 0; lclNum < m_compiler->lvaCount; lclNum++)
    {
        LclVarDsc* varDsc = m_compiler->lvaGetDesc(lclNum);
        varDsc->SetRegNum(REG_STK);
        varDsc->lvLRACandidate = isRegCandidate(varDsc);

        if (varDsc->lvLRACandidate)
        {
            // TODO-WASM-RA: enable register candidates.
            varDsc->lvLRACandidate = false;
        }

        if (varDsc->lvLRACandidate)
        {
            JITDUMP("RA candidate: V%02u", lclNum);
        }
    }
}

//------------------------------------------------------------------------
// AllocateAndResolve: allocate WASM locals and rewrite the IR accordingly.
//
void WasmRegAlloc::AllocateAndResolve()
{
    for (BasicBlock* block : m_compiler->Blocks())
    {
        AllocateAndResolveBlock(block);
    }
}

//------------------------------------------------------------------------
// AllocateAndResolveBlock: allocate WASM locals and rewrite the IR for one block.
//
// Arguments:
//    block - The block
//
void WasmRegAlloc::AllocateAndResolveBlock(BasicBlock* block)
{
    m_currentBlock = block;
    for (GenTree* node : LIR::AsRange(block))
    {
        AllocateAndResolveNode(node);
    }
    m_currentBlock = nullptr;
}

//------------------------------------------------------------------------
// AllocateAndResolveNode: allocate WASM locals and rewrite the IR for one node.
//
// Arguments:
//    node - The IR node
//
void WasmRegAlloc::AllocateAndResolveNode(GenTree* node)
{
    if (node->OperIsAnyLocal())
    {
        LclVarDsc* varDsc = m_compiler->lvaGetDesc(node->AsLclVarCommon());
        if (!varDsc->lvIsRegCandidate())
        {
            if (node->OperIsLocalStore())
            {
                RewriteLocalStackStore(node->AsLclVarCommon());
            }

            if (m_fpReg == REG_NA)
            {
                m_fpReg = AllocateFreeRegister(TYP_I_IMPL);
            }
        }
    }
    else if (node->OperIs(GT_LCLHEAP))
    {
        if (m_spReg == REG_NA)
        {
            m_spReg = AllocateFreeRegister(TYP_I_IMPL);
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
    GenTree*             op = lclNode->Data();
    GenTree::VisitResult visitResult;
    do
    {
        visitResult = op->VisitOperands([&op](GenTree* operand) {
            op = operand;
            return GenTree::VisitResult::Abort;
        });
    } while (visitResult == GenTree::VisitResult::Abort);

    GenTree*     store;
    GenTreeFlags indFlags = GTF_IND_NONFAULTING | GTF_IND_TGT_NOT_HEAP;
    if (lclNode->TypeIs(TYP_STRUCT))
    {
        store = m_compiler->gtNewStoreBlkNode(lclNode->GetLayout(m_compiler), lclNode, lclNode->Data(), indFlags);
    }
    else
    {
        store = m_compiler->gtNewStoreIndNode(lclNode->TypeGet(), lclNode, lclNode->Data(), indFlags);
    }
    CurrentRange().InsertAfter(lclNode, store);
    CurrentRange().Remove(lclNode);

    // TODO-WASM-RA: figure out the address mode story here. Right now this will produce an address not folded
    // into the store's address mode. We can utilize a contained LEA, but that will require some liveness work.
    lclNode->SetOper(GT_LCL_ADDR);
    lclNode->ChangeType(TYP_I_IMPL);
    CurrentRange().InsertBefore(op, lclNode);
}

//------------------------------------------------------------------------
// AllocateFreeRegister: Allocate a register from the (currently) free set.
//
// Arguments:
//    type - The register's type
//
regNumber WasmRegAlloc::AllocateFreeRegister(var_types type)
{
    // TODO-WASM-RA: implement.
    return MakeWasmReg(0, type);
}

//------------------------------------------------------------------------
// PublishAllocationResults: Publish the results of register allocation.
//
// Initializes various fields denoting allocation outcomes on the codegen object.
//
void WasmRegAlloc::PublishAllocationResults()
{
    CodeGenInterface* codeGen = m_compiler->codeGen;
    if (m_spReg != REG_NA)
    {
        codeGen->SetStackPointerReg(m_spReg);
    }
    else if (m_fpReg != REG_NA)
    {
        codeGen->SetStackPointerReg(m_fpReg);
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
}
