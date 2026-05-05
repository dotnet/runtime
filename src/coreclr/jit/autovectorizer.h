// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _AUTOVECTORIZER_H_
#define _AUTOVECTORIZER_H_

class AutoVectorizer
{
public:
    explicit AutoVectorizer(Compiler* compiler);

    PhaseStatus RunAnalyze();
    PhaseStatus RunRewrite();

private:
    struct LoopVectorizationPlan
    {
        FlowGraphNaturalLoop* Loop      = nullptr;
        BasicBlock*           Preheader = nullptr;
        BasicBlock*           Header    = nullptr;
        BasicBlock*           Latch     = nullptr;
        BasicBlock*           Exit      = nullptr;

        unsigned  InductionVar       = BAD_VAR_NUM;
        GenTree*  End                = nullptr;
        genTreeOps TestOper          = GT_COUNT;
        int       Step               = 0;
        unsigned  VectorSizeBytes    = 0;
        unsigned  VectorizationFactor = 0;
    };

    Compiler* m_compiler;

    bool IsEnabled() const;
    bool IsSupportedCompilation() const;
    bool TryCreateLoopPlan(FlowGraphNaturalLoop* loop, LoopVectorizationPlan* plan);
    void Reject(FlowGraphNaturalLoop* loop, const char* reason) const;

    bool ShouldDump() const;
    void Dump(const char* format, ...) const;
};

#endif // _AUTOVECTORIZER_H_
