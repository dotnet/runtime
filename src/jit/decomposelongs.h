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

private:
    inline LIR::Range& BlockRange() const
    {
        return LIR::AsRange(m_block);
    }

    // Driver functions
    GenTree* DecomposeNode(LIR::Use& use);

    // Per-node type decompose cases
    GenTree* DecomposeLclVar(LIR::Use& use);
    GenTree* DecomposeLclFld(LIR::Use& use);
    GenTree* DecomposeStoreLclVar(LIR::Use& use);
    GenTree* DecomposeCast(LIR::Use& use);
    GenTree* DecomposeCnsLng(LIR::Use& use);
    GenTree* DecomposeCall(LIR::Use& use);
    GenTree* DecomposeInd(LIR::Use& use);
    GenTree* DecomposeStoreInd(LIR::Use& use);
    GenTree* DecomposeNot(LIR::Use& use);
    GenTree* DecomposeNeg(LIR::Use& use);
    GenTree* DecomposeArith(LIR::Use& use);

    // Helper functions
    GenTree* FinalizeDecomposition(LIR::Use& use, GenTree* loResult, GenTree* hiResult);

    static genTreeOps GetHiOper(genTreeOps oper);
    static genTreeOps GetLoOper(genTreeOps oper);

    // Data
    Compiler*   m_compiler;
    BasicBlock* m_block;
};

#endif // _DECOMPOSELONGS_H_
