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

private:
    inline LIR::Range& BlockRange() const
    {
        return LIR::AsRange(m_block);
    }

    // Intrinsic related transformations
    void RewriteNodeAsCall(GenTree**             use,
                           CORINFO_SIG_INFO*     sig,
                           ArrayStack<GenTree*>& parents,
                           CORINFO_METHOD_HANDLE callHnd,
#if defined(FEATURE_READYTORUN)
                           CORINFO_CONST_LOOKUP entryPoint,
#endif // FEATURE_READYTORUN
                           GenTree** operands,
                           size_t    operandCount);

    void RewriteIntrinsicAsUserCall(GenTree** use, Compiler::GenTreeStack& parents);
#if defined(FEATURE_HW_INTRINSICS)
    void RewriteHWIntrinsicAsUserCall(GenTree** use, Compiler::GenTreeStack& parents);
#endif // FEATURE_HW_INTRINSICS

#ifdef TARGET_ARM64
    void RewriteSubLshDiv(GenTree** use);
#endif

    // Root visitor
    Compiler::fgWalkResult RewriteNode(GenTree** useEdge, Compiler::GenTreeStack& parents);

private:
    class RationalizeVisitor final : public GenTreeVisitor<RationalizeVisitor>
    {
        Rationalizer& m_rationalizer;

    public:
        enum
        {
            ComputeStack      = true,
            DoPreOrder        = true,
            DoPostOrder       = true,
            UseExecutionOrder = true,
        };

        RationalizeVisitor(Rationalizer& rationalizer)
            : GenTreeVisitor<RationalizeVisitor>(rationalizer.comp)
            , m_rationalizer(rationalizer)
        {
        }

        fgWalkResult PreOrderVisit(GenTree** use, GenTree* user);
        fgWalkResult PostOrderVisit(GenTree** use, GenTree* user);
    };
};

inline Rationalizer::Rationalizer(Compiler* _comp)
    : Phase(_comp, PHASE_RATIONALIZE)
{
}

#endif
