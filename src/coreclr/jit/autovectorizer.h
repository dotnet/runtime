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
    static const unsigned MaxLanes = 16;
    static const unsigned MaxPackNodes = 8;

    enum class PackKind
    {
        Invalid,
        LoadContiguous,
        StoreContiguous,
        SplatConstant,
        SplatScalar,
        BinaryOp,
    };

    struct PackNode
    {
        PackKind  Kind        = PackKind::Invalid;
        var_types ElementType = TYP_UNDEF;
        genTreeOps Oper       = GT_COUNT;
        unsigned  LaneCount   = 0;
        GenTree*  Lanes[MaxLanes] = {};
        PackNode* Operands[2] = {};
        unsigned  Cost        = 0;
    };

    struct SLPPlan
    {
        PackNode  Nodes[MaxPackNodes];
        unsigned  NodeCount           = 0;
        PackNode* Root                = nullptr;
        unsigned  EstimatedScalarCost = 0;
        unsigned  EstimatedVectorCost = 0;
        unsigned  EstimatedCodeSizeDelta = 0;
    };

    struct LoopVectorizationPlan
    {
        struct ScalarAccess
        {
            Statement* StatementRoot     = nullptr;
            GenTree*   Address           = nullptr;
            unsigned   BaseLocalIfKnown  = BAD_VAR_NUM;
            int        IndexOffset       = 0;
            unsigned   ElementSize       = 0;
            var_types  ElementType       = TYP_UNDEF;
            bool       IsLoad            = false;
            bool       IsStore           = false;
            bool       IsArray           = false;
            bool       IsVolatile        = false;
        };

        FlowGraphNaturalLoop* Loop      = nullptr;
        BasicBlock*           Preheader = nullptr;
        BasicBlock*           Header    = nullptr;
        BasicBlock*           Latch     = nullptr;
        BasicBlock*           Exit      = nullptr;

        unsigned  InductionVar       = BAD_VAR_NUM;
        GenTree*  End                = nullptr;
        GenTree*  IterTree           = nullptr;
        GenTree*  TestTree           = nullptr;
        BasicBlock* TestBlock        = nullptr;
        genTreeOps TestOper          = GT_COUNT;
        int       Step               = 0;
        unsigned  VectorSizeBytes    = 0;
        unsigned  VectorizationFactor = 0;

        Statement*    StoreStmt          = nullptr;
        ScalarAccess  StoreAccess;
        ScalarAccess  LoadAccess;
        GenTree*      ScalarOperand      = nullptr;
        genTreeOps    ScalarOper         = GT_COUNT;
        bool          ScalarOperandIsRhs = true;
        SLPPlan       BodyPlan;
    };

    Compiler* m_compiler;

    bool IsEnabled() const;
    bool IsSupportedCompilation() const;
    bool TryCreateLoopPlan(FlowGraphNaturalLoop* loop, LoopVectorizationPlan* plan);
    bool TryAnalyzeMemory(LoopVectorizationPlan* plan);
    bool TryBuildSLPPlan(LoopVectorizationPlan* plan);
    PackNode* NewPackNode(SLPPlan* slpPlan, PackKind kind, var_types elementType, unsigned laneCount);
    const char* PackKindName(PackKind kind) const;
    void DumpSLPPlan(const LoopVectorizationPlan& plan) const;
    bool TryAnalyzeArrayAccess(
        Statement* stmt, GenTree* indir, bool isStore, unsigned ivLcl, LoopVectorizationPlan::ScalarAccess* access);
    bool TryAnalyzeIndexExpr(GenTree* tree, unsigned ivLcl, int* offset);
    bool TryAnalyzeArrayAddress(GenTreeArrAddr* arrAddr, unsigned ivLcl, LoopVectorizationPlan::ScalarAccess* access);
    bool TryGetArrayLengthLocal(GenTree* tree, unsigned* lclNum);
    bool TryGetInvariantInt(FlowGraphNaturalLoop* loop, unsigned ivLcl, GenTree* tree);
    bool ContainsOper(GenTree* tree, genTreeOps oper);
    void Reject(FlowGraphNaturalLoop* loop, const char* reason) const;

    bool ShouldDump() const;
    void Dump(const char* format, ...) const;
};

#endif // _AUTOVECTORIZER_H_
