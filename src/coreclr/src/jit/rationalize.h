// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//===============================================================================
#include "phase.h"

class Rationalizer : public Phase
{
private:
    BasicBlock*  m_block;
    GenTreeStmt* m_statement;

public:
    Rationalizer(Compiler* comp);

#ifdef DEBUG
    static void ValidateStatement(GenTree* tree, BasicBlock* block);

    // general purpose sanity checking of de facto standard GenTree
    void SanityCheck();

    // sanity checking of rationalized IR
    void SanityCheckRational();

#endif // DEBUG

    virtual void DoPhase() override;

    static void RewriteAssignmentIntoStoreLcl(GenTreeOp* assignment);

private:
    inline LIR::Range& BlockRange() const
    {
        return LIR::AsRange(m_block);
    }

    // SIMD related
    void RewriteSIMDOperand(LIR::Use& use, bool keepBlk);

    // Intrinsic related transformations
    void RewriteNodeAsCall(GenTree**             use,
                           ArrayStack<GenTree*>& parents,
                           CORINFO_METHOD_HANDLE callHnd,
#ifdef FEATURE_READYTORUN_COMPILER
                           CORINFO_CONST_LOOKUP entryPoint,
#endif
                           GenTreeArgList* args);

    void RewriteIntrinsicAsUserCall(GenTree** use, ArrayStack<GenTree*>& parents);

    // Other transformations
    void RewriteAssignment(LIR::Use& use);
    void RewriteAddress(LIR::Use& use);

    // Root visitor
    Compiler::fgWalkResult RewriteNode(GenTree** useEdge, ArrayStack<GenTree*>& parents);
};

inline Rationalizer::Rationalizer(Compiler* _comp) : Phase(_comp, "IR Rationalize", PHASE_RATIONALIZE)
{
#ifdef DEBUG
    comp->compNumStatementLinksTraversed = 0;
#endif
}
