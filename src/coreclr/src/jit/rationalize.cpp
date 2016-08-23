// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

// state carried over the tree walk, to be used in making
// a splitting decision.
struct SplitData
{
    GenTree*      root; // root stmt of tree being processed
    BasicBlock*   block;
    Rationalizer* thisPhase;
};

//------------------------------------------------------------------------------
// isNodeCallArg - given a context (stack of parent nodes), determine if the TOS is an arg to a call
//------------------------------------------------------------------------------

GenTree* isNodeCallArg(ArrayStack<GenTree*>* parentStack)
{
    for (int i = 1; // 0 is current node, so start at 1
         i < parentStack->Height(); i++)
    {
        GenTree* node = parentStack->Index(i);
        switch (node->OperGet())
        {
            case GT_LIST:
            case GT_ARGPLACE:
                break;
            case GT_NOP:
                // Currently there's an issue when the rationalizer performs
                // the fixup of a call argument: the case is when we remove an
                // inserted NOP as a parent of a call introduced by fgMorph;
                // when then the rationalizer removes it, the tree stack in the
                // walk is not consistent with the node it was just deleted, so the
                // solution is just to go 1 level deeper.
                // TODO-Cleanup: This has to be fixed in a proper way: make the rationalizer
                // correctly modify the evaluation stack when removing treenodes.
                if (node->gtOp.gtOp1->gtOper == GT_CALL)
                {
                    return node->gtOp.gtOp1;
                }
                break;
            case GT_CALL:
                return node;
            default:
                return nullptr;
        }
    }
    return nullptr;
}

// return op that is the store equivalent of the given load opcode
genTreeOps storeForm(genTreeOps loadForm)
{
    switch (loadForm)
    {
        case GT_LCL_VAR:
            return GT_STORE_LCL_VAR;
        case GT_LCL_FLD:
            return GT_STORE_LCL_FLD;
        case GT_REG_VAR:
            noway_assert(!"reg vars only supported in classic backend\n");
            unreached();
        default:
            noway_assert(!"not a data load opcode\n");
            unreached();
    }
}

// return op that is the addr equivalent of the given load opcode
genTreeOps addrForm(genTreeOps loadForm)
{
    switch (loadForm)
    {
        case GT_LCL_VAR:
            return GT_LCL_VAR_ADDR;
        case GT_LCL_FLD:
            return GT_LCL_FLD_ADDR;
        default:
            noway_assert(!"not a data load opcode\n");
            unreached();
    }
}

// return op that is the load equivalent of the given addr opcode
genTreeOps loadForm(genTreeOps addrForm)
{
    switch (addrForm)
    {
        case GT_LCL_VAR_ADDR:
            return GT_LCL_VAR;
        case GT_LCL_FLD_ADDR:
            return GT_LCL_FLD;
        default:
            noway_assert(!"not a local address opcode\n");
            unreached();
    }
}

// copy the flags determined by mask from src to dst
void copyFlags(GenTree* dst, GenTree* src, unsigned mask)
{
    dst->gtFlags &= ~mask;
    dst->gtFlags |= (src->gtFlags & mask);
}

// call args have other pointers to them which must be fixed up if
// they are replaced
void Compiler::fgFixupIfCallArg(ArrayStack<GenTree*>* parentStack, GenTree* oldChild, GenTree* newChild)
{
    GenTree* parentCall = isNodeCallArg(parentStack);
    if (!parentCall)
    {
        return;
    }

    // we have replaced an arg, so update pointers in argtable
    fgFixupArgTabEntryPtr(parentCall, oldChild, newChild);
}

//------------------------------------------------------------------------
// fgFixupArgTabEntryPtr: Fixup the fgArgTabEntryPtr of parentCall after
//                        replacing oldArg with newArg
//
// Arguments:
//    parentCall - a pointer to the parent call node
//    oldArg     - the original argument node
//    newArg     - the replacement argument node
//

void Compiler::fgFixupArgTabEntryPtr(GenTreePtr parentCall, GenTreePtr oldArg, GenTreePtr newArg)
{
    assert(parentCall != nullptr);
    assert(oldArg != nullptr);
    assert(newArg != nullptr);

    JITDUMP("parent call was :\n");
    DISPNODE(parentCall);

    JITDUMP("old child was :\n");
    DISPNODE(oldArg);

    if (oldArg->gtFlags & GTF_LATE_ARG)
    {
        newArg->gtFlags |= GTF_LATE_ARG;
    }
    else
    {
        fgArgTabEntryPtr fp = Compiler::gtArgEntryByNode(parentCall, oldArg);
        assert(fp->node == oldArg);
        fp->node = newArg;
    }
}

// Rewrite InitBlk involving SIMD vector into stlcl.var of a SIMD type.
//
// Arguments:
//    ppTree      - A pointer-to-a-pointer for the GT_INITBLK
//    fgWalkData  - A pointer to tree walk data providing the context
//
// Return Value:
//    None.
//
// TODO-Cleanup: Once SIMD types are plumbed through the frontend, this will no longer
// be required.
//
void Rationalizer::RewriteInitBlk(LIR::Use& use)
{
#ifdef FEATURE_SIMD
    // No lowering is needed for non-SIMD nodes, so early out if featureSIMD is not enabled.
    if (!comp->featureSIMD)
    {
        return;
    }

    // See if this is a SIMD initBlk that needs to be changed to a simple st.lclVar.
    GenTreeInitBlk* initBlk = use.Def()->AsInitBlk();

    // Is the dstAddr is addr of a SIMD type lclVar?
    GenTree* dstAddr = initBlk->Dest();
    if (!comp->isAddrOfSIMDType(dstAddr) || !dstAddr->OperIsLocalAddr())
    {
        return;
    }

    unsigned lclNum = dstAddr->AsLclVarCommon()->gtLclNum;
    if (!comp->lvaTable[lclNum].lvSIMDType)
    {
        return;
    }

    var_types            baseType      = comp->lvaTable[lclNum].lvBaseType;
    CORINFO_CLASS_HANDLE typeHnd       = comp->lvaTable[lclNum].lvVerTypeInfo.GetClassHandle();
    unsigned             simdLocalSize = comp->getSIMDTypeSizeInBytes(typeHnd);

    JITDUMP("Rewriting SIMD InitBlk\n");
    DISPTREERANGE(BlockRange(), initBlk);

    assert((dstAddr->gtFlags & GTF_VAR_USEASG) == 0);

    // There are currently only three sizes supported: 8 bytes, 16 bytes or the vector register length.
    GenTreeIntConCommon* sizeNode = initBlk->Size()->AsIntConCommon();
    unsigned int         size     = (unsigned int)roundUp(sizeNode->IconValue(), TARGET_POINTER_SIZE);
    var_types            simdType = comp->getSIMDTypeForSize(size);
    assert(roundUp(simdLocalSize, TARGET_POINTER_SIZE) == size);

    GenTree*     initVal  = initBlk->InitVal();
    GenTreeSIMD* simdNode = new (comp, GT_SIMD)
        GenTreeSIMD(simdType, initVal, SIMDIntrinsicInit, baseType, (unsigned)sizeNode->IconValue());

    dstAddr->SetOper(GT_STORE_LCL_VAR);
    GenTreeLclVar* store = dstAddr->AsLclVar();
    store->gtType        = simdType;
    store->gtOp.gtOp1    = simdNode;
    store->gtFlags |= ((simdNode->gtFlags & GTF_ALL_EFFECT) | GTF_ASG);
    BlockRange().Remove(store);

    // Insert the new nodes into the block
    BlockRange().InsertAfter(initVal, simdNode, store);
    use.ReplaceWith(comp, store);

    // Remove the old size and GT_INITBLK nodes.
    BlockRange().Remove(sizeNode);
    BlockRange().Remove(initBlk);

    JITDUMP("After rewriting SIMD InitBlk:\n");
    DISPTREERANGE(BlockRange(), use.Def());
    JITDUMP("\n");
#endif // FEATURE_SIMD
}

// Transform CopyBlk involving SIMD vectors into stlclvar or stind of a SIMD type.
// Transformation is done if either src or dst are known to be SIMD vectors.
//
// Arguments:
//    ppTree      - A pointer-to-a-pointer for the GT_COPYBLK
//    fgWalkData  - A pointer to tree walk data providing the context
//
// Return Value:
//    None.
//
// If either the source or the dst are known to be SIMD (a lclVar or SIMD intrinsic),
// get the simdType (TYP_DOUBLE or a SIMD type for SSE2) from the size of the SIMD node.
//
// For the source:
// - If it is a SIMD intrinsic or a lvSIMDType lclVar, change the node type to simdType.
// - Otherwise, add a GT_IND of simdType.
// For the dst:
// - If it is a lclVar of a SIMD type, chanage the node type to simdType.
// - Otherwise, change it to a GT_STORE_IND of simdType
//
// TODO-Cleanup: Once SIMD types are plumbed through the frontend, this will no longer
// be required.
//
void Rationalizer::RewriteCopyBlk(LIR::Use& use)
{
#ifdef FEATURE_SIMD
    // No need to transofrm non-SIMD nodes, if featureSIMD is not enabled.
    if (!comp->featureSIMD)
    {
        return;
    }

    // See if this is a SIMD copyBlk
    GenTreeCpBlk* cpBlk   = use.Def()->AsCpBlk();
    GenTreePtr    dstAddr = cpBlk->Dest();
    GenTree*      srcAddr = cpBlk->Source();

    const bool srcIsSIMDAddr = comp->isAddrOfSIMDType(srcAddr);
    const bool dstIsSIMDAddr = comp->isAddrOfSIMDType(dstAddr);

    // Do not transform if neither src or dst is known to be a SIMD type.
    // If src tree type is something we cannot reason but if dst is known to be of a SIMD type
    // we will treat src tree as a SIMD type and vice versa.
    if (!srcIsSIMDAddr && !dstIsSIMDAddr)
    {
        return;
    }

    // At this point it is known to be a copyblk of SIMD vectors and we can
    // start transforming the original tree. Prior to this point do not perform
    // any modifications to the original tree.
    JITDUMP("\nRewriting SIMD CopyBlk\n");
    DISPTREERANGE(BlockRange(), cpBlk);

    // There are currently only three sizes supported: 8 bytes, 12 bytes, 16 bytes or the vector register length.
    GenTreeIntConCommon* sizeNode = cpBlk->Size()->AsIntConCommon();
    var_types            simdType = comp->getSIMDTypeForSize((unsigned int)sizeNode->IconValue());

    // Remove 'size' from execution order
    BlockRange().Remove(sizeNode);

    // Is destination a lclVar which is not an arg?
    // If yes then we can turn it to a stlcl.var, otherwise turn into stind.
    GenTree*   simdDst = nullptr;
    genTreeOps oper    = GT_NONE;
    if (dstIsSIMDAddr && dstAddr->OperIsLocalAddr())
    {
        simdDst         = dstAddr;
        simdDst->gtType = simdType;
        oper            = GT_STORE_LCL_VAR;

        // For structs that are padded (e.g. Vector3f, Vector3i), the morpher will have marked them
        // as GTF_VAR_USEASG.  Unmark them.
        simdDst->gtFlags &= ~(GTF_VAR_USEASG);
    }
    else
    {
        // Address of a non-local var
        simdDst = dstAddr;
        oper    = GT_STOREIND;
    }

    GenTree* simdSrc = nullptr;
    if ((srcAddr->OperGet() == GT_ADDR) && varTypeIsSIMD(srcAddr->gtGetOp1()))
    {
        // Get rid of parent node of GT_ADDR(..) if its child happens to be of a SIMD type.
        BlockRange().Remove(srcAddr);
        simdSrc = srcAddr->gtGetOp1();
    }
    else if (srcIsSIMDAddr && srcAddr->OperIsLocalAddr())
    {
        // If the source has been rewritten into a local addr node, rewrite it back into a
        // local var node.
        simdSrc = srcAddr;
        simdSrc->SetOper(loadForm(srcAddr->OperGet()));
    }
    else
    {
        // Since destination is known to be a SIMD type, src must be a SIMD type too
        // though we cannot figure it out easily enough. Transform src into
        // GT_IND(src) of simdType.
        GenTree* indir = comp->gtNewOperNode(GT_IND, simdType, srcAddr);
        BlockRange().InsertAfter(srcAddr, indir);

        cpBlk->gtGetOp1()->gtOp.gtOp2 = indir;
        simdSrc                       = indir;
    }
    simdSrc->gtType = simdType;

    // Change cpblk to either a st.lclvar or st.ind.
    // At this point we are manipulating cpblk node with the knowledge of
    // its internals (i.e. op1 is the size node, and the src & dst are in a GT_LIST on op2).
    // This logic might need to be changed if we ever restructure cpblk node.

    assert(simdDst != nullptr);
    assert(simdSrc != nullptr);

    GenTree* newNode = nullptr;
    if (oper == GT_STORE_LCL_VAR)
    {
        newNode = simdDst;
        newNode->SetOper(oper);

        GenTreeLclVar* store = newNode->AsLclVar();
        store->gtOp1         = simdSrc;
        store->gtType        = simdType;
        store->gtFlags |= ((simdSrc->gtFlags & GTF_ALL_EFFECT) | GTF_ASG);

        BlockRange().Remove(simdDst);
        BlockRange().InsertAfter(simdSrc, store);
    }
    else
    {
        assert(oper == GT_STOREIND);

        newNode = cpBlk->gtGetOp1();
        newNode->SetOper(oper);

        GenTreeStoreInd* storeInd = newNode->AsStoreInd();
        storeInd->gtType          = simdType;
        storeInd->gtFlags |= ((simdSrc->gtFlags & GTF_ALL_EFFECT) | GTF_ASG);
        storeInd->gtOp1 = simdDst;
        storeInd->gtOp2 = simdSrc;

        BlockRange().InsertBefore(cpBlk, storeInd);
    }

    use.ReplaceWith(comp, newNode);
    BlockRange().Remove(cpBlk);

    JITDUMP("After rewriting SIMD CopyBlk:\n");
    DISPTREERANGE(BlockRange(), use.Def());
    JITDUMP("\n");
#endif // FEATURE_SIMD
}

// Rewrite GT_OBJ of SIMD Vector as GT_IND(GT_LEA(obj.op1)) of a SIMD type.
//
// Arguments:
//    ppTree      - A pointer-to-a-pointer for the GT_OBJ
//    fgWalkData  - A pointer to tree walk data providing the context
//
// Return Value:
//    None.
//
// TODO-Cleanup: Once SIMD types are plumbed through the frontend, this will no longer
// be required.
//
void Rationalizer::RewriteObj(LIR::Use& use)
{
#ifdef FEATURE_SIMD
    GenTreeObj* obj = use.Def()->AsObj();

// For UNIX struct passing, we can have Obj nodes for arguments.
// For other cases, we should never see a non-SIMD type here.
#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
    if (!varTypeIsSIMD(obj))
    {
        return;
    }
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING

    // Should come here only if featureSIMD is enabled
    noway_assert(comp->featureSIMD);

    // We should only call this with a SIMD type.
    noway_assert(varTypeIsSIMD(obj));
    var_types simdType = obj->TypeGet();

    // If the operand of obj is a GT_ADDR(GT_LCL_VAR) and LclVar is known to be a SIMD type,
    // replace obj by GT_LCL_VAR.
    GenTree* srcAddr = obj->gtGetOp1();
    if (srcAddr->OperIsLocalAddr() && comp->isAddrOfSIMDType(srcAddr))
    {
        BlockRange().Remove(obj);

        srcAddr->SetOper(loadForm(srcAddr->OperGet()));
        srcAddr->gtType = simdType;
        use.ReplaceWith(comp, srcAddr);
    }
    else
    {
        obj->SetOper(GT_IND);
        obj->gtType = simdType;
    }
#else
    // we should never reach without feature SIMD
    assert(!"Unexpected obj during rationalization\n");
    unreached();
#endif
}

// RewriteNodeAsCall : Replace the given tree node by a GT_CALL.
//
// Arguments:
//    ppTree      - A pointer-to-a-pointer for the tree node
//    fgWalkData  - A pointer to tree walk data providing the context
//    callHnd     - The method handle of the call to be generated
//    entryPoint  - The method entrypoint of the call to be generated
//    args        - The argument list of the call to be generated
//
// Return Value:
//    None.
//

void Rationalizer::RewriteNodeAsCall(GenTree**             use,
                                     Compiler::fgWalkData* data,
                                     CORINFO_METHOD_HANDLE callHnd,
#ifdef FEATURE_READYTORUN_COMPILER
                                     CORINFO_CONST_LOOKUP entryPoint,
#endif
                                     GenTreeArgList* args)
{
    GenTreePtr tree          = *use;
    Compiler*  comp          = data->compiler;
    SplitData* tmpState      = (SplitData*)data->pCallbackData;
    GenTreePtr root          = tmpState->root;
    GenTreePtr treeFirstNode = comp->fgGetFirstNode(tree);
    GenTreePtr treeLastNode  = tree;
    GenTreePtr treePrevNode  = treeFirstNode->gtPrev;
    GenTreePtr treeNextNode  = treeLastNode->gtNext;

    // Create the call node
    GenTreeCall* call = comp->gtNewCallNode(CT_USER_FUNC, callHnd, tree->gtType, args);
    call              = comp->fgMorphArgs(call);
#ifdef FEATURE_READYTORUN_COMPILER
    call->gtCall.setEntryPoint(entryPoint);
#endif

    // Replace "tree" with "call"
    *use = call;

    // Rebuild the evaluation order.
    comp->gtSetStmtInfo(root);

    // Rebuild the execution order.
    comp->fgSetTreeSeq(call, treePrevNode);

    // Restore linear-order Prev and Next for "call".
    if (treePrevNode)
    {
        treeFirstNode         = comp->fgGetFirstNode(call);
        treeFirstNode->gtPrev = treePrevNode;
        treePrevNode->gtNext  = treeFirstNode;
    }
    else
    {
        // Update the linear oder start of "root" if treeFirstNode
        // appears to have replaced the original first node.
        assert(treeFirstNode == root->gtStmt.gtStmtList);
        root->gtStmt.gtStmtList = comp->fgGetFirstNode(call);
    }

    if (treeNextNode)
    {
        treeLastNode         = call;
        treeLastNode->gtNext = treeNextNode;
        treeNextNode->gtPrev = treeLastNode;
    }

    comp->fgFixupIfCallArg(data->parentStack, tree, call);

    // Propagate flags of "call" to its parents.
    // 0 is current node, so start at 1
    for (int i = 1; i < data->parentStack->Height(); i++)
    {
        GenTree* node = data->parentStack->Index(i);
        node->gtFlags |= GTF_CALL;
        node->gtFlags |= call->gtFlags & GTF_ALL_EFFECT;
    }

    // Since "tree" is replaced with "call", pop "tree" node (i.e the current node)
    // and replace it with "call" on parent stack.
    assert(data->parentStack->Top() == tree);
    (void)data->parentStack->Pop();
    data->parentStack->Push(call);
}

// RewriteIntrinsicAsUserCall : Rewrite an intrinsic operator as a GT_CALL to the original method.
//
// Arguments:
//    ppTree      - A pointer-to-a-pointer for the intrinsic node
//    fgWalkData  - A pointer to tree walk data providing the context
//
// Return Value:
//    None.
//
// Some intrinsics, such as operation Sqrt, are rewritten back to calls, and some are not.
// The ones that are not being rewritten here must be handled in Codegen.
// Conceptually, the lower is the right place to do the rewrite. Keeping it in rationalization is
// mainly for throughput issue.

void Rationalizer::RewriteIntrinsicAsUserCall(GenTree** use, Compiler::fgWalkData* data)
{
    GenTreeIntrinsic* intrinsic = (*use)->AsIntrinsic();
    Compiler*         comp      = data->compiler;

    GenTreeArgList* args;
    if (intrinsic->gtOp.gtOp2 == nullptr)
    {
        args = comp->gtNewArgList(intrinsic->gtGetOp1());
    }
    else
    {
        args = comp->gtNewArgList(intrinsic->gtGetOp1(), intrinsic->gtGetOp2());
    }

    RewriteNodeAsCall(use, data, intrinsic->gtMethodHandle,
#ifdef FEATURE_READYTORUN_COMPILER
                      intrinsic->gtEntryPoint,
#endif
                      args);
}

// FixupIfSIMDLocal: Fixup the type of a lclVar tree, as needed, if it is a SIMD type vector.
//
// Arguments:
//    comp      - the Compiler object.
//    tree      - the GenTreeLclVarCommon tree to be fixed up.
//
// Return Value:
//    None.
//
// TODO-1stClassStructs: This is now only here to preserve existing behavior. It is actually not
// desirable to change the lclFld nodes back to TYP_SIMD (it will cause them to be loaded
// into a vector register, and then moved to an int register).

void Rationalizer::FixupIfSIMDLocal(GenTreeLclVarCommon* node)
{
#ifdef FEATURE_SIMD
    if (!comp->featureSIMD)
    {
        return;
    }

    LclVarDsc* varDsc = &(comp->lvaTable[node->gtLclNum]);

    // Don't mark byref of SIMD vector as a SIMD type.
    // Note that struct args though marked as lvIsSIMD=true,
    // the tree node representing such an arg should not be
    // marked as a SIMD type, since it is a byref of a SIMD type.
    if (!varTypeIsSIMD(varDsc))
    {
        return;
    }
    switch (node->OperGet())
    {
        default:
            // Nothing to do for most tree nodes.
            break;

        case GT_LCL_FLD:
            // We may see a lclFld used for pointer-sized structs that have been morphed, in which
            // case we can change it to GT_LCL_VAR.
            // However, we may also see a lclFld with FieldSeqStore::NotAField() for structs that can't
            // be analyzed, e.g. those with overlapping fields such as the IL implementation of Vector<T>.
            if ((node->AsLclFld()->gtFieldSeq == FieldSeqStore::NotAField()) && (node->AsLclFld()->gtLclOffs == 0) &&
                (node->gtType == TYP_I_IMPL) && (varDsc->lvExactSize == TARGET_POINTER_SIZE))
            {
                node->SetOper(GT_LCL_VAR);
                node->gtFlags &= ~(GTF_VAR_USEASG);
            }
            else
            {
                // If we access a field of a SIMD lclVar via GT_LCL_FLD, it cannot have been
                // independently promoted.
                assert(comp->lvaGetPromotionType(varDsc) != Compiler::PROMOTION_TYPE_INDEPENDENT);
                return;
            }
            break;
        case GT_STORE_LCL_FLD:
            assert(node->gtType == TYP_I_IMPL);
            node->SetOper(GT_STORE_LCL_VAR);
            node->gtFlags &= ~(GTF_VAR_USEASG);
            break;
    }
    unsigned simdSize = (unsigned int)roundUp(varDsc->lvExactSize, TARGET_POINTER_SIZE);
    node->gtType      = comp->getSIMDTypeForSize(simdSize);
#endif // FEATURE_SIMD
}

#ifdef DEBUG

void Rationalizer::ValidateStatement(GenTree* tree, BasicBlock* block)
{
    assert(tree->gtOper == GT_STMT);
    DBEXEC(TRUE, JitTls::GetCompiler()->fgDebugCheckNodeLinks(block, tree));
}

// sanity checks that apply to all kinds of IR
void Rationalizer::SanityCheck()
{
    // TODO: assert(!IsLIR());
    BasicBlock* block;
    foreach_block(comp, block)
    {
        for (GenTree* statement = block->bbTreeList; statement != nullptr; statement = statement->gtNext)
        {
            ValidateStatement(statement, block);

            for (GenTree* tree = statement->gtStmt.gtStmtList; tree; tree = tree->gtNext)
            {
                // QMARK nodes should have been removed before this phase.
                assert(tree->OperGet() != GT_QMARK);

                if (tree->OperGet() == GT_ASG)
                {
                    if (tree->gtGetOp1()->OperGet() == GT_LCL_VAR)
                    {
                        assert(tree->gtGetOp1()->gtFlags & GTF_VAR_DEF);
                    }
                    else if (tree->gtGetOp2()->OperGet() == GT_LCL_VAR)
                    {
                        assert(!(tree->gtGetOp2()->gtFlags & GTF_VAR_DEF));
                    }
                }
            }
        }
    }
}

void Rationalizer::SanityCheckRational()
{
    // TODO-Cleanup : check that the tree is rational here
    // then do normal checks
    SanityCheck();
}

#endif // DEBUG

static void RewriteAssignmentIntoStoreLclCore(GenTreeOp* assignment,
                                              GenTree*   location,
                                              GenTree*   value,
                                              genTreeOps locationOp)
{
    assert(assignment != nullptr);
    assert(assignment->OperGet() == GT_ASG);
    assert(location != nullptr);
    assert(value != nullptr);

    genTreeOps storeOp = storeForm(locationOp);

#ifdef DEBUG
    JITDUMP("rewriting asg(%s, X) to %s(X)\n", GenTree::NodeName(locationOp), GenTree::NodeName(storeOp));
#endif // DEBUG

    assignment->SetOper(storeOp);
    GenTreeLclVarCommon* store = assignment->AsLclVarCommon();

    GenTreeLclVarCommon* var = location->AsLclVarCommon();
    store->SetLclNum(var->gtLclNum);
    store->SetSsaNum(var->gtSsaNum);

    if (locationOp == GT_LCL_FLD)
    {
        store->gtLclFld.gtLclOffs  = var->gtLclFld.gtLclOffs;
        store->gtLclFld.gtFieldSeq = var->gtLclFld.gtFieldSeq;
    }

    copyFlags(store, var, GTF_LIVENESS_MASK);
    store->gtFlags &= ~GTF_REVERSE_OPS;

    store->gtType = var->TypeGet();
    store->gtOp1  = value;

    DISPNODE(store);
    JITDUMP("\n");
}

void Rationalizer::RewriteAssignmentIntoStoreLcl(GenTreeOp* assignment)
{
    assert(assignment != nullptr);
    assert(assignment->OperGet() == GT_ASG);

    GenTree* location = assignment->gtGetOp1();
    GenTree* value    = assignment->gtGetOp2();

    RewriteAssignmentIntoStoreLclCore(assignment, location, value, location->OperGet());
}

void Rationalizer::RewriteAssignment(LIR::Use& use)
{
    assert(use.IsInitialized());

    GenTreeOp* assignment = use.Def()->AsOp();
    assert(assignment->OperGet() == GT_ASG);

    GenTree* location = assignment->gtGetOp1();
    GenTree* value    = assignment->gtGetOp2();

    genTreeOps locationOp = location->OperGet();
    switch (locationOp)
    {
        case GT_LCL_VAR:
        case GT_LCL_FLD:
        case GT_REG_VAR:
        case GT_PHI_ARG:
            RewriteAssignmentIntoStoreLclCore(assignment, location, value, locationOp);
            BlockRange().Remove(location);
            break;

        case GT_IND:
        {
            GenTreeStoreInd* store =
                new (comp, GT_STOREIND) GenTreeStoreInd(location->TypeGet(), location->gtGetOp1(), value);

            copyFlags(store, assignment, GTF_ALL_EFFECT);
            copyFlags(store, location, GTF_IND_FLAGS);

            if (assignment->IsReverseOp())
            {
                store->gtFlags |= GTF_REVERSE_OPS;
            }

            // TODO: JIT dump

            // Remove the GT_IND node and replace the assignment node with the store
            BlockRange().Remove(location);
            BlockRange().InsertBefore(assignment, store);
            use.ReplaceWith(comp, store);
            BlockRange().Remove(assignment);
        }
        break;

        case GT_CLS_VAR:
        {
            location->SetOper(GT_CLS_VAR_ADDR);
            location->gtType = TYP_BYREF;

            assignment->SetOper(GT_STOREIND);

            // TODO: JIT dump
        }
        break;

        default:
            unreached();
            break;
    }
}

void Rationalizer::RewriteAddress(LIR::Use& use)
{
    assert(use.IsInitialized());

    GenTreeUnOp* address = use.Def()->AsUnOp();
    assert(address->OperGet() == GT_ADDR);

    GenTree*   location   = address->gtGetOp1();
    genTreeOps locationOp = location->OperGet();

    if (location->IsLocal())
    {
// We are changing the child from GT_LCL_VAR TO GT_LCL_VAR_ADDR.
// Therefore gtType of the child needs to be changed to a TYP_BYREF
#ifdef DEBUG
        if (locationOp == GT_LCL_VAR)
        {
            JITDUMP("Rewriting GT_ADDR(GT_LCL_VAR) to GT_LCL_VAR_ADDR:\n");
        }
        else
        {
            assert(locationOp == GT_LCL_FLD);
            JITDUMP("Rewriting GT_ADDR(GT_LCL_FLD) to GT_LCL_FLD_ADDR:\n");
        }
#endif // DEBUG

        location->SetOper(addrForm(locationOp));
        location->gtType = TYP_BYREF;
        copyFlags(location, address, GTF_ALL_EFFECT);

        use.ReplaceWith(comp, location);
        BlockRange().Remove(address);
    }
    else if (locationOp == GT_CLS_VAR)
    {
        location->SetOper(GT_CLS_VAR_ADDR);
        location->gtType = TYP_BYREF;
        copyFlags(location, address, GTF_ALL_EFFECT);

        use.ReplaceWith(comp, location);
        BlockRange().Remove(address);

        JITDUMP("Rewriting GT_ADDR(GT_CLS_VAR) to GT_CLS_VAR_ADDR:\n");
    }
    else if (locationOp == GT_IND)
    {
        use.ReplaceWith(comp, location->gtGetOp1());
        BlockRange().Remove(location);
        BlockRange().Remove(address);

        JITDUMP("Rewriting GT_ADDR(GT_IND(X)) to X:\n");
    }

    DISPTREERANGE(BlockRange(), use.Def());
    JITDUMP("\n");
}

Compiler::fgWalkResult Rationalizer::RewriteNode(GenTree** useEdge, ArrayStack<GenTree*>& parentStack)
{
    assert(useEdge != nullptr);

    GenTree* node = *useEdge;
    assert(node != nullptr);

#ifdef DEBUG
    const bool isLateArg = (node->gtFlags & GTF_LATE_ARG) != 0;
#endif

    // First, remove any preceeding GT_LIST nodes, which are not otherwise visited by the tree walk.
    //
    // NOTE: GT_LIST nodes that are used by block ops and phi nodes will in fact be visited.
    for (GenTree* prev = node->gtPrev; prev != nullptr && prev->OperGet() == GT_LIST; prev = node->gtPrev)
    {
        BlockRange().Remove(prev);
    }

    // In addition, remove the current node if it is a GT_LIST node.
    if ((*useEdge)->OperGet() == GT_LIST)
    {
        BlockRange().Remove(*useEdge);
        return Compiler::WALK_CONTINUE;
    }

    LIR::Use use;
    if (parentStack.Height() < 2)
    {
        use = LIR::Use::GetDummyUse(BlockRange(), *useEdge);
    }
    else
    {
        use = LIR::Use(BlockRange(), useEdge, parentStack.Index(1));
    }

    assert(node == use.Def());
    switch (node->OperGet())
    {
        case GT_ASG:
            RewriteAssignment(use);
            break;

        case GT_BOX:
            // GT_BOX at this level just passes through so get rid of it
            use.ReplaceWith(comp, node->gtGetOp1());
            BlockRange().Remove(node);
            break;

        case GT_ADDR:
            RewriteAddress(use);
            break;

        case GT_NOP:
            // fgMorph sometimes inserts NOP nodes between defs and uses
            // supposedly 'to prevent constant folding'. In this case, remove the
            // NOP.
            if (node->gtGetOp1() != nullptr)
            {
                use.ReplaceWith(comp, node->gtGetOp1());
                BlockRange().Remove(node);
            }
            break;

        case GT_COMMA:
        {
            GenTree* op1 = node->gtGetOp1();
            if ((op1->gtFlags & GTF_ALL_EFFECT) == 0)
            {
                // The LHS has no side effects. Remove it.
                bool               isClosed    = false;
                unsigned           sideEffects = 0;
                LIR::ReadOnlyRange lhsRange    = BlockRange().GetTreeRange(op1, &isClosed, &sideEffects);

                // None of the transforms performed herein violate tree order, so these
                // should always be true.
                assert(isClosed);
                assert((sideEffects & GTF_ALL_EFFECT) == 0);

                BlockRange().Delete(comp, m_block, std::move(lhsRange));
            }

            GenTree* replacement = node->gtGetOp2();
            if (!use.IsDummyUse())
            {
                use.ReplaceWith(comp, replacement);
            }
            else
            {
                // This is a top-level comma. If the RHS has no side effects we can remove
                // it as well.
                if ((replacement->gtFlags & GTF_ALL_EFFECT) == 0)
                {
                    bool               isClosed    = false;
                    unsigned           sideEffects = 0;
                    LIR::ReadOnlyRange rhsRange    = BlockRange().GetTreeRange(replacement, &isClosed, &sideEffects);

                    // None of the transforms performed herein violate tree order, so these
                    // should always be true.
                    assert(isClosed);
                    assert((sideEffects & GTF_ALL_EFFECT) == 0);

                    BlockRange().Delete(comp, m_block, std::move(rhsRange));
                }
            }

            BlockRange().Remove(node);
        }
        break;

        case GT_ARGPLACE:
            // Remove argplace and list nodes from the execution order.
            //
            // TODO: remove phi args and phi nodes as well?
            BlockRange().Remove(node);
            break;

#ifdef _TARGET_XARCH_
        case GT_CLS_VAR:
        {
            // Class vars that are the target of an assignment will get rewritten into
            // GT_STOREIND(GT_CLS_VAR_ADDR, val) by RewriteAssignment. This check is
            // not strictly necessary--the GT_IND(GT_CLS_VAR_ADDR) pattern that would
            // otherwise be generated would also be picked up by RewriteAssignment--but
            // skipping the rewrite here saves an allocation and a bit of extra work.
            const bool isLHSOfAssignment = (use.User()->OperGet() == GT_ASG) && (use.User()->gtGetOp1() == node);
            if (!isLHSOfAssignment)
            {
                GenTree* ind = comp->gtNewOperNode(GT_IND, node->TypeGet(), node);

                node->SetOper(GT_CLS_VAR_ADDR);
                node->gtType = TYP_BYREF;

                BlockRange().InsertAfter(node, ind);
                use.ReplaceWith(comp, ind);

                // TODO: JIT dump
            }
        }
        break;
#endif // _TARGET_XARCH_

        case GT_INTRINSIC:
            // Non-target intrinsics should have already been rewritten back into user calls.
            assert(Compiler::IsTargetIntrinsic(node->gtIntrinsic.gtIntrinsicId));
            break;

#ifdef FEATURE_SIMD
        case GT_INITBLK:
            RewriteInitBlk(use);
            break;

        case GT_COPYBLK:
            RewriteCopyBlk(use);
            break;

        case GT_OBJ:
            RewriteObj(use);
            break;

        case GT_LCL_FLD:
        case GT_STORE_LCL_FLD:
            // TODO-1stClassStructs: Eliminate this.
            FixupIfSIMDLocal(node->AsLclVarCommon());
            break;

        case GT_STOREIND:
        case GT_IND:
            if (node->gtType == TYP_STRUCT)
            {
                GenTree* addr = node->AsIndir()->Addr();
                assert(addr->TypeGet() == TYP_BYREF);

                if (addr->OperIsLocal())
                {
                    LclVarDsc* varDsc = &(comp->lvaTable[addr->AsLclVarCommon()->gtLclNum]);
                    assert(varDsc->lvSIMDType);
                    unsigned simdSize = (unsigned int)roundUp(varDsc->lvExactSize, TARGET_POINTER_SIZE);
                    node->gtType      = comp->getSIMDTypeForSize(simdSize);
                }
#if DEBUG
                else
                {
                    // If the address is not a local var, assert that the user of this IND is an ADDR node.
                    assert((use.User()->OperGet() == GT_ADDR) || use.User()->OperIsLocalAddr());
                }
#endif
            }
            break;

        case GT_SIMD:
        {
            noway_assert(comp->featureSIMD);
            GenTreeSIMD* simdNode = node->AsSIMD();
            unsigned     simdSize = simdNode->gtSIMDSize;
            var_types    simdType = comp->getSIMDTypeForSize(simdSize);

            // TODO-1stClassStructs: This should be handled more generally for enregistered or promoted
            // structs that are passed or returned in a different register type than their enregistered
            // type(s).
            if (simdNode->gtType == TYP_I_IMPL && simdNode->gtSIMDSize == TARGET_POINTER_SIZE)
            {
                // This happens when it is consumed by a GT_RET_EXPR.
                // It can only be a Vector2f or Vector2i.
                assert(genTypeSize(simdNode->gtSIMDBaseType) == 4);
                simdNode->gtType = TYP_SIMD8;
            }
            else if (simdNode->gtType == TYP_STRUCT || varTypeIsSIMD(simdNode))
            {
                node->gtType = simdType;
            }

            // Certain SIMD trees require rationalizing.
            if (simdNode->gtSIMD.gtSIMDIntrinsicID == SIMDIntrinsicInitArray)
            {
                // Rewrite this as an explicit load.
                JITDUMP("Rewriting GT_SIMD array init as an explicit load:\n");
                unsigned int baseTypeSize = genTypeSize(simdNode->gtSIMDBaseType);
                GenTree*     address = new (comp, GT_LEA) GenTreeAddrMode(TYP_BYREF, simdNode->gtOp1, simdNode->gtOp2,
                                                                      baseTypeSize, offsetof(CORINFO_Array, u1Elems));
                GenTree* ind = comp->gtNewOperNode(GT_IND, simdType, address);

                BlockRange().InsertBefore(simdNode, address, ind);
                use.ReplaceWith(comp, ind);
                BlockRange().Remove(simdNode);

                DISPTREERANGE(BlockRange(), use.Def());
                JITDUMP("\n");
            }
            else
            {
                // This code depends on the fact that NONE of the SIMD intrinsics take vector operands
                // of a different width.  If that assumption changes, we will EITHER have to make these type
                // transformations during importation, and plumb the types all the way through the JIT,
                // OR add a lot of special handling here.
                GenTree* op1 = simdNode->gtGetOp1();
                if (op1 != nullptr && op1->gtType == TYP_STRUCT)
                {
                    op1->gtType = simdType;
                }

                GenTree* op2 = simdNode->gtGetOp2();
                if (op2 != nullptr && op2->gtType == TYP_STRUCT)
                {
                    op2->gtType = simdType;
                }
            }
        }
        break;
#endif // FEATURE_SIMD

        default:
            break;
    }

    // Do some extra processing on top-level nodes to remove unused local reads.
    if (use.IsDummyUse() && node->OperIsLocalRead())
    {
        assert((node->gtFlags & GTF_ALL_EFFECT) == 0);

        comp->lvaDecRefCnts(node);
        BlockRange().Remove(node);
    }

    assert(isLateArg == ((node->gtFlags & GTF_LATE_ARG) != 0));

    return Compiler::WALK_CONTINUE;
}

void Rationalizer::DoPhase()
{
    DBEXEC(TRUE, SanityCheck());

    comp->compCurBB = nullptr;
    comp->fgOrder   = Compiler::FGOrderLinear;

    BasicBlock* firstBlock = comp->fgFirstBB;

    for (BasicBlock* block = comp->fgFirstBB; block != nullptr; block = block->bbNext)
    {
        comp->compCurBB = block;
        m_block         = block;

        // Establish the first and last nodes for the block. This is necessary in order for the LIR
        // utilities that hang off the BasicBlock type to work correctly.
        GenTreeStmt* firstStatement = block->firstStmt();
        if (firstStatement == nullptr)
        {
            // No statements in this block; skip it.
            block->MakeLIR(nullptr, nullptr);
            continue;
        }

        GenTreeStmt* lastStatement = block->lastStmt();

        // Rewrite intrinsics that are not supported by the target back into user calls.
        // This needs to be done before the transition to LIR because it relies on the use
        // of fgMorphArgs, which is designed to operate on HIR. Once this is done for a
        // particular statement, link that statement's nodes into the current basic block.
        //
        // This walk also clears the GTF_VAR_USEDEF bit on locals, which is not necessary
        // in the backend.
        GenTree* lastNodeInPreviousStatement = nullptr;
        for (GenTreeStmt* statement = firstStatement; statement != nullptr; statement = statement->getNextStmt())
        {
            assert(statement->gtStmtList != nullptr);
            assert(statement->gtStmtList->gtPrev == nullptr);
            assert(statement->gtStmtExpr != nullptr);
            assert(statement->gtStmtExpr->gtNext == nullptr);

            SplitData splitData;
            splitData.root      = statement;
            splitData.block     = block;
            splitData.thisPhase = this;

            comp->fgWalkTreePost(&statement->gtStmtExpr,
                                 [](GenTree** use, Compiler::fgWalkData* walkData) -> Compiler::fgWalkResult {
                                     GenTree* node = *use;
                                     if (node->OperGet() == GT_INTRINSIC &&
                                         Compiler::IsIntrinsicImplementedByUserCall(node->gtIntrinsic.gtIntrinsicId))
                                     {
                                         RewriteIntrinsicAsUserCall(use, walkData);
                                     }
                                     else if (node->OperIsLocal())
                                     {
                                         node->gtFlags &= ~GTF_VAR_USEDEF;
                                     }

                                     return Compiler::WALK_CONTINUE;
                                 },
                                 &splitData, true);

            GenTree* firstNodeInStatement = statement->gtStmtList;
            if (lastNodeInPreviousStatement != nullptr)
            {
                lastNodeInPreviousStatement->gtNext = firstNodeInStatement;
            }

            firstNodeInStatement->gtPrev = lastNodeInPreviousStatement;
            lastNodeInPreviousStatement  = statement->gtStmtExpr;
        }

        block->MakeLIR(firstStatement->gtStmtList, lastStatement->gtStmtExpr);

        // Rewrite HIR nodes into LIR nodes.
        for (GenTreeStmt *statement = firstStatement, *nextStatement; statement != nullptr; statement = nextStatement)
        {
            nextStatement = statement->getNextStmt();

            // If this statement has correct offset information, change it into an IL offset
            // node and insert it into the LIR.
            if (statement->gtStmtILoffsx != BAD_IL_OFFSET)
            {
                assert(!statement->IsPhiDefnStmt());
                statement->SetOper(GT_IL_OFFSET);
                statement->gtNext = nullptr;
                statement->gtPrev = nullptr;

                BlockRange().InsertBefore(statement->gtStmtList, statement);
            }

            m_statement = statement;
            comp->fgWalkTreePost(&statement->gtStmtExpr,
                                 [](GenTree** use, Compiler::fgWalkData* walkData) -> Compiler::fgWalkResult {
                                     return reinterpret_cast<Rationalizer*>(walkData->pCallbackData)
                                         ->RewriteNode(use, *walkData->parentStack);
                                 },
                                 this, true);
        }

        assert(BlockRange().CheckLIR(comp));
    }

    comp->compRationalIRForm = true;
}
