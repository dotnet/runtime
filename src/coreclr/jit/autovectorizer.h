// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _AUTOVECTORIZER_H_
#define _AUTOVECTORIZER_H_

class AutoVectorizer
{
public:
    explicit AutoVectorizer(Compiler* compiler);

    PhaseStatus Run();

private:
    static const unsigned MaxLanes     = 64;
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
        PackKind   Kind            = PackKind::Invalid;
        var_types  ElementType     = TYP_UNDEF;
        genTreeOps Oper            = GT_COUNT;
        unsigned   LaneCount       = 0;
        GenTree*   Lanes[MaxLanes] = {};
        PackNode*  Operands[2]     = {};
        unsigned   Cost            = 0;
    };

    struct SLPPlan
    {
        PackNode  Nodes[MaxPackNodes];
        unsigned  NodeCount              = 0;
        PackNode* Root                   = nullptr;
        unsigned  EstimatedScalarCost    = 0;
        unsigned  EstimatedVectorCost    = 0;
        unsigned  EstimatedCodeSizeDelta = 0;
    };

    struct LoopVectorizationPlan
    {
        static const unsigned MaxAddressUpdates = 4;
        static const unsigned MaxLocalDefs      = 8;

        struct ScalarAccess
        {
            Statement* StatementRoot         = nullptr;
            GenTree*   Address               = nullptr;
            unsigned   BaseLocalIfKnown      = BAD_VAR_NUM;
            unsigned   OffsetLocalIfKnown    = BAD_VAR_NUM;
            int        IndexOffset           = 0;
            unsigned   ElementSize           = 0;
            var_types  ElementType           = TYP_UNDEF;
            bool       IsLoad                = false;
            bool       IsStore               = false;
            bool       IsArray               = false;
            bool       IsVolatile            = false;
            bool       IsByrefLocal          = false;
            bool       IsByrefBaseWithOffset = false;
        };

        FlowGraphNaturalLoop* Loop      = nullptr;
        BasicBlock*           Preheader = nullptr;
        BasicBlock*           Header    = nullptr;
        BasicBlock*           Latch     = nullptr;
        BasicBlock*           Exit      = nullptr;

        bool        IsPostIV                               = false;
        unsigned    InductionVar                           = BAD_VAR_NUM;
        unsigned    TripCountVar                           = BAD_VAR_NUM;
        unsigned    AddressUpdateVars[MaxAddressUpdates]   = {};
        int         AddressUpdateDeltas[MaxAddressUpdates] = {};
        unsigned    AddressUpdateCount                     = 0;
        unsigned    LocalDefVars[MaxLocalDefs]             = {};
        GenTree*    LocalDefValues[MaxLocalDefs]           = {};
        unsigned    LocalDefCount                          = 0;
        GenTree*    End                                    = nullptr;
        GenTree*    IterTree                               = nullptr;
        GenTree*    TestTree                               = nullptr;
        BasicBlock* TestBlock                              = nullptr;
        genTreeOps  TestOper                               = GT_COUNT;
        int         Step                                   = 0;
        var_types   ElementType                            = TYP_UNDEF;
        unsigned    ElementSize                            = 0;
        unsigned    VectorSizeBytes                        = 0;
        unsigned    VectorizationFactor                    = 0;

        Statement*   StoreStmt = nullptr;
        ScalarAccess StoreAccess;
        ScalarAccess LoadAccess;
        GenTree*     ScalarOperand      = nullptr;
        genTreeOps   ScalarOper         = GT_COUNT;
        bool         ScalarOperandIsRhs = true;
        SLPPlan      BodyPlan;
    };

    Compiler* m_compiler;

    bool        IsEnabled() const;
    bool        IsSupportedCompilation() const;
    unsigned    GetVectorSizeBytes(var_types elementType) const;
    bool        ReportVectorIsa(unsigned vectorSizeBytes) const;
    bool        RecomputeLoopTable();
    bool        IsSupportedElementType(var_types elementType) const;
    bool        IsSupportedBinaryOp(genTreeOps oper, var_types elementType) const;
    bool        TryCreateLoopPlan(FlowGraphNaturalLoop* loop, LoopVectorizationPlan* plan);
    bool        TryCreatePostIVLoopPlan(FlowGraphNaturalLoop* loop, LoopVectorizationPlan* plan);
    bool        TryAnalyzeMemory(LoopVectorizationPlan* plan);
    bool        TryAnalyzePostIVMemory(LoopVectorizationPlan* plan);
    bool        TryAnalyzePostIVValue(Statement* stmt, GenTree* data, LoopVectorizationPlan* plan);
    bool        TryGetIndirOperand(GenTree* tree, GenTree** indir);
    bool        TryNormalizeScalarValue(GenTree** value, var_types elementType) const;
    bool        TryBuildSLPPlan(LoopVectorizationPlan* plan);
    bool        TryRewritePlan(LoopVectorizationPlan* plan);
    PackNode*   NewPackNode(SLPPlan* slpPlan, PackKind kind, var_types elementType, unsigned laneCount);
    const char* PackKindName(PackKind kind) const;
    void        DumpSLPPlan(const LoopVectorizationPlan& plan) const;
    GenTree*    BuildArrayAddress(LoopVectorizationPlan* plan, const LoopVectorizationPlan::ScalarAccess& access);
    GenTree*    BuildVectorLoopTest(LoopVectorizationPlan* plan);
    GenTree*    BuildPostIVSameStartCheck(LoopVectorizationPlan* plan);
    GenTree*    BuildPostIVStoreBeforeLoadCheck(LoopVectorizationPlan* plan);
    GenTree*    BuildPostIVLoadBeforeStoreCheck(LoopVectorizationPlan* plan);
    GenTree*    BuildPostIVAddress(const LoopVectorizationPlan::ScalarAccess& access);
    GenTree*    BuildVectorStore(LoopVectorizationPlan* plan);
    GenTree*    BuildIVUpdate(LoopVectorizationPlan* plan);
    GenTree*    BuildAddressUpdate(LoopVectorizationPlan* plan, unsigned addressVar);
    GenTree*    BuildTripCountUpdate(LoopVectorizationPlan* plan, int delta);
    GenTree*    BuildScalarRemainderTest(LoopVectorizationPlan* plan);
    GenTree*    UnwrapCommaValue(GenTree* tree);
    bool        TryAnalyzeArrayAccess(LoopVectorizationPlan*               plan,
                                      Statement*                           stmt,
                                      GenTree*                             indir,
                                      bool                                 isStore,
                                      unsigned                             ivLcl,
                                      LoopVectorizationPlan::ScalarAccess* access);
    bool        TryAnalyzePostIVAddress(Statement* stmt, GenTree* addr, LoopVectorizationPlan::ScalarAccess* access);
    bool        TryAnalyzeByrefLocalAddress(GenTree* addr, unsigned* lclNum);
    bool        TryAnalyzePostIVArrayAddress(GenTreeArrAddr* arrAddr, LoopVectorizationPlan::ScalarAccess* access);
    bool        TryAnalyzeIndexExpr(LoopVectorizationPlan* plan, GenTree* tree, unsigned ivLcl, int* offset);
    bool        TryAnalyzeArrayAddress(LoopVectorizationPlan*               plan,
                                       GenTreeArrAddr*                      arrAddr,
                                       unsigned                             ivLcl,
                                       LoopVectorizationPlan::ScalarAccess* access);
    void        RecordLocalDefs(LoopVectorizationPlan* plan, GenTree* tree, bool* foundBoundsCheck = nullptr);
    void        RecordLocalDef(LoopVectorizationPlan* plan, unsigned lclNum, GenTree* value);
    bool        TryGetLocalDef(LoopVectorizationPlan* plan, unsigned lclNum, GenTree** value);
    bool        TryGetArrayLengthLimitLocal(LoopVectorizationPlan* plan, GenTree* tree, unsigned* lclNum, int* offset);
    bool        TryGetArrayLengthLocal(GenTree* tree, unsigned* lclNum);
    bool        IsCompatibleScalarType(GenTree* tree, var_types elementType) const;
    bool TryGetInvariantOperand(FlowGraphNaturalLoop* loop, unsigned ivLcl, GenTree* tree, var_types elementType);
    void RecordAddressUpdate(LoopVectorizationPlan* plan, unsigned addressVar, int delta);
    bool HasAddressUpdate(LoopVectorizationPlan* plan, unsigned addressVar);
};

#endif // _AUTOVECTORIZER_H_
