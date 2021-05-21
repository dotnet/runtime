// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                               DecomposeLongs                              XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#ifndef _DECOMPOSELONGS_H_
#define _DECOMPOSELONGS_H_

#include "compiler.h"

class DecomposeLongs
{
public:
    DecomposeLongs(Compiler* compiler) : m_compiler(compiler)
    {
    }

    void PrepareForDecomposition();
    void DecomposeBlock(BasicBlock* block);

    static void DecomposeRange(Compiler* compiler, LIR::Range& range);

private:
    inline LIR::Range& Range() const
    {
        return *m_range;
    }

    void PromoteLongVars();

    // Driver functions
    void     DecomposeRangeHelper();
    GenTree* DecomposeNode(GenTree* tree);

    // Per-node type decompose cases
    GenTree* DecomposeLclVar(LIR::Use& use);
    GenTree* DecomposeLclFld(LIR::Use& use);
    GenTree* DecomposeStoreLclVar(LIR::Use& use);
    GenTree* DecomposeStoreLclFld(LIR::Use& use);
    GenTree* DecomposeCast(LIR::Use& use);
    GenTree* DecomposeCnsLng(LIR::Use& use);
    GenTree* DecomposeFieldList(GenTreeFieldList* fieldList, GenTreeOp* longNode);
    GenTree* DecomposeCall(LIR::Use& use);
    GenTree* DecomposeInd(LIR::Use& use);
    GenTree* DecomposeStoreInd(LIR::Use& use);
    GenTree* DecomposeNot(LIR::Use& use);
    GenTree* DecomposeNeg(LIR::Use& use);
    GenTree* DecomposeArith(LIR::Use& use);
    GenTree* DecomposeShift(LIR::Use& use);
    GenTree* DecomposeRotate(LIR::Use& use);
    GenTree* DecomposeMul(LIR::Use& use);
    GenTree* DecomposeUMod(LIR::Use& use);

#ifdef FEATURE_HW_INTRINSICS
    GenTree* DecomposeHWIntrinsic(LIR::Use& use);
    GenTree* DecomposeHWIntrinsicGetElement(LIR::Use& use, GenTreeHWIntrinsic* node);
#endif // FEATURE_HW_INTRINSICS

    // Helper functions
    GenTree* FinalizeDecomposition(LIR::Use& use, GenTree* loResult, GenTree* hiResult, GenTree* insertResultAfter);
    GenTree* RepresentOpAsLocalVar(GenTree* op, GenTree* user, GenTree** edge);
    GenTree* EnsureIntSized(GenTree* node, bool signExtend);

    GenTree* StoreNodeToVar(LIR::Use& use);
    static genTreeOps GetHiOper(genTreeOps oper);
    static genTreeOps GetLoOper(genTreeOps oper);

    // Data
    Compiler*   m_compiler;
    LIR::Range* m_range;
};

#endif // _DECOMPOSELONGS_H_
