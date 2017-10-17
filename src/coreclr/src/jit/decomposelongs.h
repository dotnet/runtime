// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    static void DecomposeRange(Compiler* compiler, unsigned blockWeight, LIR::Range& range);

private:
    inline LIR::Range& Range() const
    {
        return *m_range;
    }

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
    GenTree* DecomposeFieldList(GenTreeFieldList* listNode, GenTreeOp* longNode);
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
    GenTree* DecomposeSimd(LIR::Use& use);
    GenTree* DecomposeSimdGetItem(LIR::Use& use);

    // Helper functions
    GenTree* FinalizeDecomposition(LIR::Use& use, GenTree* loResult, GenTree* hiResult, GenTree* insertResultAfter);
    GenTree* RepresentOpAsLocalVar(GenTree* op, GenTree* user, GenTree** edge);
    GenTree* EnsureIntSized(GenTree* node, bool signExtend);

    GenTree* StoreNodeToVar(LIR::Use& use);
    static genTreeOps GetHiOper(genTreeOps oper);
    static genTreeOps GetLoOper(genTreeOps oper);

    // Data
    Compiler*   m_compiler;
    unsigned    m_blockWeight;
    LIR::Range* m_range;
};

#endif // _DECOMPOSELONGS_H_
