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
    static const unsigned MaxPackNodes = 64;
    static const unsigned MaxPackDepth = 64;

    enum class PackKind
    {
        Invalid,
        LoadContiguous,
        StoreContiguous,
        SplatConstant,
        SplatScalar,
        UnaryOp,
        BinaryOp,
        TernaryOp,
        CompareOp,
        Select,
    };

    struct PackNode
    {
        PackKind       Kind            = PackKind::Invalid;
        var_types      ElementType     = TYP_UNDEF;
        genTreeOps     Oper            = GT_COUNT;
        NamedIntrinsic IntrinsicName   = NI_Illegal;
        unsigned       LaneCount       = 0;
        unsigned       AccessIndex     = UINT_MAX;
        GenTree*       Lanes[MaxLanes] = {};
        PackNode*      Operands[3]     = {};
        unsigned       Cost            = 0;
    };

    struct SLPPlan
    {
        PackNode  Nodes[MaxPackNodes];
        PackNode* Roots[8]               = {};
        unsigned  NodeCount              = 0;
        unsigned  RootCount              = 0;
        PackNode* Root                   = nullptr;
        unsigned  EstimatedScalarCost    = 0;
        unsigned  EstimatedVectorCost    = 0;
        unsigned  EstimatedCodeSizeDelta = 0;
    };

    struct LoopVectorizationPlan
    {
        static const unsigned MaxAddressUpdates = 4;
        static const unsigned MaxAccesses       = 16;
        static const unsigned MaxLocalDefs      = 32;
        static const unsigned MaxStores         = 12;

        struct ScalarAccess
        {
            Statement* StatementRoot         = nullptr;
            GenTree*   Address               = nullptr;
            unsigned   BaseLocalIfKnown      = BAD_VAR_NUM;
            unsigned   OffsetLocalIfKnown    = BAD_VAR_NUM;
            unsigned   InvariantIndexLocal   = BAD_VAR_NUM;
            int        IndexOffset           = 0;
            unsigned   ElementSize           = 0;
            var_types  ElementType           = TYP_UNDEF;
            bool       IsLoad                = false;
            bool       IsStore               = false;
            bool       IsArray               = false;
            bool       IsVolatile            = false;
            bool       IsByrefLocal          = false;
            bool       IsByrefBaseWithOffset = false;
            bool       IsByrefWithIndex      = false;
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
        bool        HasConstInit                           = false;
        int         ConstInitValue                         = 0;
        var_types   ElementType                            = TYP_UNDEF;
        unsigned    ElementSize                            = 0;
        unsigned    VectorSizeBytes                        = 0;
        unsigned    VectorizationFactor                    = 0;
        int         MinIndexOffset                         = 0;
        int         MaxIndexOffset                         = 0;
        bool        SawBoundsCheck                         = false;
        bool        NeedsOverlapCheck                      = false;

        Statement*   StoreStmts[MaxStores]     = {};
        GenTree*     StoreValues[MaxStores]    = {};
        ScalarAccess StoreAccesses[MaxStores]  = {};
        unsigned     StoreCount                = 0;
        ScalarAccess LoadAccesses[MaxAccesses] = {};
        unsigned     LoadCount                 = 0;
        Statement*   ReductionStmt             = nullptr;
        unsigned     ReductionLcl              = BAD_VAR_NUM;
        unsigned     ReductionVectorLcl        = BAD_VAR_NUM;
        genTreeOps   ReductionOper             = GT_COUNT;
        GenTree*     ReductionValue            = nullptr;
        PackNode*    ReductionPack             = nullptr;
        Statement*   StoreStmt                 = nullptr;
        ScalarAccess StoreAccess;
        ScalarAccess LoadAccess;
        SLPPlan      BodyPlan;
    };

    Compiler* m_compiler;

    bool      IsEnabled() const;
    bool      IsAggressiveVectorizing() const;
    bool      IsSupportedCompilation() const;
    unsigned  GetVectorSizeBytes(var_types elementType) const;
    bool      ReportVectorIsa(unsigned vectorSizeBytes) const;
    bool      RecomputeLoopTable();
    bool      IsSupportedElementType(var_types elementType) const;
    bool      IsSupportedUnaryOp(genTreeOps oper, var_types elementType) const;
    bool      IsSupportedBinaryOp(genTreeOps oper, var_types elementType) const;
    bool      IsSupportedCompareOp(genTreeOps oper, var_types elementType) const;
    bool      IsSupportedIntrinsic(NamedIntrinsic intrinsic, var_types elementType) const;
    bool      TrySelectVectorSizeAndBuildSLPPlan(LoopVectorizationPlan* plan);
    bool      IsProfitableVectorSize(const LoopVectorizationPlan* plan, unsigned maxVectorSizeBytes) const;
    bool      TryGetConstantTripCount(const LoopVectorizationPlan* plan, unsigned* tripCount) const;
    unsigned  EstimateVectorPressure(const LoopVectorizationPlan* plan) const;
    bool      TryCreateLoopPlan(FlowGraphNaturalLoop* loop, LoopVectorizationPlan* plan);
    bool      TryCreatePostIVLoopPlan(FlowGraphNaturalLoop* loop, LoopVectorizationPlan* plan);
    bool      TryCreateLocalLimitLoopPlan(FlowGraphNaturalLoop* loop, LoopVectorizationPlan* plan);
    bool      TryAnalyzeMemory(LoopVectorizationPlan* plan);
    bool      TryAnalyzePostIVMemory(LoopVectorizationPlan* plan);
    bool      AddStore(LoopVectorizationPlan*                     plan,
                       Statement*                                 stmt,
                       GenTree*                                   value,
                       const LoopVectorizationPlan::ScalarAccess& access);
    bool      TryAddReduction(LoopVectorizationPlan* plan, Statement* stmt, GenTreeLclVarCommon* storeLcl);
    bool      AddLoad(LoopVectorizationPlan* plan, const LoopVectorizationPlan::ScalarAccess& access, unsigned* index);
    bool      ValidateMemoryDependences(LoopVectorizationPlan* plan);
    bool      MayAlias(const LoopVectorizationPlan::ScalarAccess& first,
                       const LoopVectorizationPlan::ScalarAccess& second) const;
    bool      TryGetIndirOperand(GenTree* tree, GenTree** indir);
    bool      TryNormalizeScalarValue(GenTree** value, var_types elementType) const;
    PackNode* TryBuildPack(
        LoopVectorizationPlan* plan, Statement* stmt, GenTree* value, var_types elementType, unsigned depth = 0);
    PackNode* TryBuildComparePack(
        LoopVectorizationPlan* plan, Statement* stmt, GenTree* value, var_types elementType, unsigned depth);
    PackNode* TryBuildScalarHWINTRINSICPack(
        LoopVectorizationPlan* plan, Statement* stmt, GenTreeHWIntrinsic* intrinsic, var_types elementType, unsigned depth);
    bool TryGetScalarFromCreateScalar(LoopVectorizationPlan* plan, GenTree* tree, GenTree** scalar, unsigned depth = 0);
    bool        TryBuildSLPPlan(LoopVectorizationPlan* plan);
    bool        TryRewritePlan(LoopVectorizationPlan* plan);
    PackNode*   NewPackNode(SLPPlan* slpPlan, PackKind kind, var_types elementType, unsigned laneCount);
    const char* PackKindName(PackKind kind) const;
    void        DumpSLPPlan(const LoopVectorizationPlan& plan) const;
    GenTree*    BuildAddress(LoopVectorizationPlan* plan, const LoopVectorizationPlan::ScalarAccess& access);
    GenTree*    BuildArrayAddress(LoopVectorizationPlan* plan, const LoopVectorizationPlan::ScalarAccess& access);
    GenTree*    BuildByrefAddress(LoopVectorizationPlan* plan, const LoopVectorizationPlan::ScalarAccess& access);
    GenTree*    BuildVectorLoopTest(LoopVectorizationPlan* plan);
    GenTree*    BuildPostIVSameStartCheck(LoopVectorizationPlan* plan);
    GenTree*    BuildPostIVStoreBeforeLoadCheck(LoopVectorizationPlan* plan);
    GenTree*    BuildPostIVLoadBeforeStoreCheck(LoopVectorizationPlan* plan);
    GenTree*    BuildPostIVAddress(LoopVectorizationPlan* plan, const LoopVectorizationPlan::ScalarAccess& access);
    GenTree*    BuildPackNode(LoopVectorizationPlan* plan, PackNode* node);
    GenTree*    BuildVectorStore(LoopVectorizationPlan* plan, PackNode* node);
    GenTree*    BuildReductionInit(LoopVectorizationPlan* plan);
    GenTree*    BuildReductionUpdate(LoopVectorizationPlan* plan);
    GenTree*    BuildReductionFinalize(LoopVectorizationPlan* plan);
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
    bool        TryAnalyzeIndirAccess(LoopVectorizationPlan*               plan,
                                      Statement*                           stmt,
                                      GenTree*                             indir,
                                      bool                                 isStore,
                                      unsigned                             ivLcl,
                                      LoopVectorizationPlan::ScalarAccess* access);
    bool        TryAnalyzePostIVAddress(Statement* stmt, GenTree* addr, LoopVectorizationPlan::ScalarAccess* access);
    bool        TryAnalyzeByrefLocalAddress(GenTree* addr, unsigned* lclNum);
    bool        TryAnalyzeByrefAddress(LoopVectorizationPlan*               plan,
                                       GenTree*                             addr,
                                       unsigned                             ivLcl,
                                       var_types                            elementType,
                                       LoopVectorizationPlan::ScalarAccess* access);
    bool        TryAnalyzePostIVArrayAddress(GenTreeArrAddr* arrAddr, LoopVectorizationPlan::ScalarAccess* access);
    bool        TryAnalyzeIndexExpr(LoopVectorizationPlan* plan,
                                    GenTree*               tree,
                                    unsigned               ivLcl,
                                    int*                   offset,
                                    unsigned*              invariantLcl = nullptr,
                                    bool*                  sawIv        = nullptr,
                                    unsigned               depth        = 0);
    bool        TryAnalyzeArrayAddress(LoopVectorizationPlan*               plan,
                                       GenTreeArrAddr*                      arrAddr,
                                       unsigned                             ivLcl,
                                       LoopVectorizationPlan::ScalarAccess* access);
    bool        TryProveRemainingBoundsChecks(LoopVectorizationPlan* plan);
    bool        IsSameLimit(LoopVectorizationPlan* plan, GenTree* first, GenTree* second, unsigned depth = 0);
    void        RecordLocalDefs(LoopVectorizationPlan* plan, GenTree* tree, bool* foundBoundsCheck = nullptr);
    void        RecordLocalDef(LoopVectorizationPlan* plan, unsigned lclNum, GenTree* value);
    bool        TryGetLocalDef(LoopVectorizationPlan* plan, unsigned lclNum, GenTree** value);
    bool        TryCollectArrayLengthLimitLocals(LoopVectorizationPlan* plan,
                                                 GenTree*               tree,
                                                 unsigned*              lclNums,
                                                 int*                   offsets,
                                                 unsigned               maxCount,
                                                 unsigned*              count,
                                                 unsigned               depth = 0);
    bool        TryGetArrayLengthLimitLocal(
               LoopVectorizationPlan* plan, GenTree* tree, unsigned* lclNum, int* offset, unsigned depth = 0);
    bool TryGetArrayLengthLocal(GenTree* tree, unsigned* lclNum);
    bool IsCompatibleScalarType(GenTree* tree, var_types elementType) const;
    bool TryGetInvariantOperand(FlowGraphNaturalLoop* loop, unsigned ivLcl, GenTree* tree, var_types elementType);
    void RecordAddressUpdate(LoopVectorizationPlan* plan, unsigned addressVar, int delta);
    bool HasAddressUpdate(LoopVectorizationPlan* plan, unsigned addressVar);
};

#endif // _AUTOVECTORIZER_H_
