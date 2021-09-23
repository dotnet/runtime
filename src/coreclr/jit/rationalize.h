// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//===============================================================================
#ifndef JIT_RATIONALIZE_H
#define JIT_RATIONALIZE_H

#include "phase.h"

class Rationalizer final : public Phase
{
private:
    BasicBlock* m_block;
    Statement*  m_statement;

public:
    Rationalizer(Compiler* comp);

#ifdef DEBUG
    static void ValidateStatement(Statement* stmt, BasicBlock* block);

    // general purpose sanity checking of de facto standard GenTree
    void SanityCheck();

    // sanity checking of rationalized IR
    void SanityCheckRational();

#endif // DEBUG

    virtual PhaseStatus DoPhase() override;

    static void RewriteAssignmentIntoStoreLcl(GenTreeOp* assignment);

private:
    inline LIR::Range& BlockRange() const
    {
        return LIR::AsRange(m_block);
    }

    void RewriteIndir(LIR::Use& use);

    // SIMD related
    void RewriteSIMDIndir(LIR::Use& use);

    // Intrinsic related transformations
    void RewriteNodeAsCall(GenTree**             use,
                           ArrayStack<GenTree*>& parents,
                           CORINFO_METHOD_HANDLE callHnd,
#ifdef FEATURE_READYTORUN
                           CORINFO_CONST_LOOKUP entryPoint,
#endif
                           GenTreeCall::Use* args);

    void RewriteIntrinsicAsUserCall(GenTree** use, Compiler::GenTreeStack& parents);

    // Other transformations
    void RewriteAssignment(LIR::Use& use);
    void RewriteAddress(LIR::Use& use);

    // Root visitor
    Compiler::fgWalkResult RewriteNode(GenTree** useEdge, Compiler::GenTreeStack& parents);
};

inline Rationalizer::Rationalizer(Compiler* _comp) : Phase(_comp, PHASE_RATIONALIZE)
{
#ifdef DEBUG
    comp->compNumStatementLinksTraversed = 0;
#endif
}

#endif
