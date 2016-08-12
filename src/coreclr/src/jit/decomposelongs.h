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

    DecomposeLongs(Compiler* compiler)
        : m_compiler(compiler)
    {
    }

    void PrepareForDecomposition();
    void DecomposeBlock(BasicBlock* block);
    
private:

    // Driver functions
    static Compiler::fgWalkResult DecompNodeHelper(GenTree** ppTree, Compiler::fgWalkData* data);
    void DecomposeStmt(GenTreeStmt* stmt);
    void DecomposeNode(GenTree** ppTree, Compiler::fgWalkData* data);

    // Per-node type decompose cases
    void DecomposeLclVar(GenTree** ppTree, Compiler::fgWalkData* data);
    void DecomposeLclFld(GenTree** ppTree, Compiler::fgWalkData* data);
    void DecomposeStoreLclVar(GenTree** ppTree, Compiler::fgWalkData* data);
    void DecomposeCast(GenTree** ppTree, Compiler::fgWalkData* data);
    void DecomposeCnsLng(GenTree** ppTree, Compiler::fgWalkData* data);
    void DecomposeCall(GenTree** ppTree, Compiler::fgWalkData* data);
    void DecomposeInd(GenTree** ppTree, Compiler::fgWalkData* data);
    void DecomposeStoreInd(GenTree** ppTree, Compiler::fgWalkData* data);
    void DecomposeNot(GenTree** ppTree, Compiler::fgWalkData* data);
    void DecomposeNeg(GenTree** ppTree, Compiler::fgWalkData* data);
    void DecomposeArith(GenTree** ppTree, Compiler::fgWalkData* data);

    // Helper functions
    void FinalizeDecomposition(GenTree** ppTree, Compiler::fgWalkData* data, GenTree* loResult, GenTree* hiResult);
    void InsertNodeAsStmt(GenTree* node);
    GenTreeStmt* CreateTemporary(GenTree** ppTree);
    static genTreeOps GetHiOper(genTreeOps oper);
    static genTreeOps GetLoOper(genTreeOps oper);
    void SimpleLinkNodeAfter(GenTree* insertionPoint, GenTree* node);

    // Data
    Compiler* m_compiler;
};

#endif // _DECOMPOSELONGS_H_
