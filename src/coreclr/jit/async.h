// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

struct ReturnTypeInfo
{
    var_types    ReturnType   = TYP_UNDEF;
    ClassLayout* ReturnLayout = nullptr;

    ReturnTypeInfo(var_types returnType, ClassLayout* returnLayout)
        : ReturnType(returnType)
        , ReturnLayout(returnLayout)
    {
    }
};

struct ReturnInfo
{
    ReturnTypeInfo Type;
    unsigned       Alignment;
    unsigned       Offset;
    unsigned       Size;

    ReturnInfo(ReturnTypeInfo type)
        : Type(type)
    {
    }

    unsigned HeapAlignment() const
    {
        return std::min(Alignment, (unsigned)TARGET_POINTER_SIZE);
    }
};

struct LiveLocalInfo
{
    unsigned LclNum;
    unsigned Alignment;
    unsigned Offset;
    unsigned Size;

    explicit LiveLocalInfo(unsigned lclNum)
        : LclNum(lclNum)
    {
    }

    unsigned HeapAlignment() const
    {
        return std::min(Alignment, (unsigned)TARGET_POINTER_SIZE);
    }
};

struct ContinuationLayoutBuilder
{
private:
    Compiler* m_compiler;
    bool      m_needsOSRAddress          = false;
    bool      m_needsException           = false;
    bool      m_needsContinuationContext = false;
    bool      m_needsKeepAlive           = false;
    bool      m_needsExecutionContext    = false;

    jitstd::vector<ReturnTypeInfo> m_returns;
    jitstd::vector<unsigned>       m_locals;

public:
    ContinuationLayoutBuilder(Compiler* compiler)
        : m_compiler(compiler)
        , m_returns(compiler->getAllocator(CMK_Async))
        , m_locals(compiler->getAllocator(CMK_Async))
    {
    }

    void SetNeedsOSRAddress()
    {
        m_needsOSRAddress = true;
    }
    bool NeedsOSRAddress() const
    {
        return m_needsOSRAddress;
    }
    void SetNeedsException()
    {
        m_needsException = true;
    }
    bool NeedsException() const
    {
        return m_needsException;
    }
    void SetNeedsContinuationContext()
    {
        m_needsContinuationContext = true;
    }
    bool NeedsContinuationContext() const
    {
        return m_needsContinuationContext;
    }
    void SetNeedsKeepAlive()
    {
        m_needsKeepAlive = true;
    }
    bool NeedsKeepAlive() const
    {
        return m_needsKeepAlive;
    }
    void SetNeedsExecutionContext()
    {
        m_needsExecutionContext = true;
    }
    bool NeedsExecutionContext() const
    {
        return m_needsExecutionContext;
    }
    void AddReturn(const ReturnTypeInfo& info);
    void AddLocal(unsigned lclNum);
    bool ContainsLocal(unsigned lclNum) const;

    const jitstd::vector<unsigned>& Locals() const
    {
        return m_locals;
    }

    struct ContinuationLayout* Create();

    static ContinuationLayoutBuilder* CreateSharedLayout(Compiler*                                comp,
                                                         const jitstd::vector<struct AsyncState>& states);
};

struct ContinuationLayout
{
    unsigned                      Size                      = 0;
    unsigned                      OSRAddressOffset          = UINT_MAX;
    unsigned                      ExceptionOffset           = UINT_MAX;
    unsigned                      ContinuationContextOffset = UINT_MAX;
    unsigned                      KeepAliveOffset           = UINT_MAX;
    unsigned                      ExecutionContextOffset    = UINT_MAX;
    jitstd::vector<LiveLocalInfo> Locals;
    jitstd::vector<ReturnInfo>    Returns;
    CORINFO_CLASS_HANDLE          ClassHnd = NO_CLASS_HANDLE;

    ContinuationLayout(Compiler* comp)
        : Locals(comp->getAllocator(CMK_Async))
        , Returns(comp->getAllocator(CMK_Async))
    {
    }

    const ReturnInfo* FindReturn(Compiler* comp, GenTreeCall* call) const;
#ifdef DEBUG
    void Dump(int indent = 0);
#endif
};

struct CallDefinitionInfo
{
    GenTreeLclVarCommon* DefinitionNode = nullptr;

    // Where to insert new IR after the call in the original block, for
    // suspension checks and for the async suspension for diagnostics purposes.
    GenTree* InsertAfter = nullptr;
};

struct AsyncState
{
    AsyncState(unsigned                   number,
               ContinuationLayoutBuilder* layout,
               BasicBlock*                callBlock,
               GenTreeCall*               call,
               CallDefinitionInfo         callDefInfo,
               BasicBlock*                suspensionBB,
               BasicBlock*                resumptionBB,
               bool                       resumeReachable,
               VARSET_TP                  mutatedSincePreviousResumption)
        : Number(number)
        , Layout(layout)
        , CallBlock(callBlock)
        , Call(call)
        , CallDefInfo(callDefInfo)
        , SuspensionBB(suspensionBB)
        , ResumptionBB(resumptionBB)
        , ResumeReachable(resumeReachable)
        , MutatedSincePreviousResumption(mutatedSincePreviousResumption)
    {
    }

    unsigned                   Number;
    ContinuationLayoutBuilder* Layout;
    BasicBlock*                CallBlock;
    GenTreeCall*               Call;
    CallDefinitionInfo         CallDefInfo;
    BasicBlock*                SuspensionBB;
    BasicBlock*                ResumptionBB;
    // Is this suspension point reachable after a previous resumption?
    bool ResumeReachable;
    // Set of variables that may have been mutated since the previous resumption.
    VARSET_TP MutatedSincePreviousResumption;
};

// See DefaultValueAnalysis::Run for an explanation of the analysis.
class DefaultValueAnalysis
{
    Compiler*  m_compiler;
    VARSET_TP* m_mutatedVars;   // Per-block set of locals mutated to non-default.
    VARSET_TP* m_mutatedVarsIn; // Per-block set of locals mutated to non-default on entry.

public:
    DefaultValueAnalysis(Compiler* compiler)
        : m_compiler(compiler)
        , m_mutatedVars(nullptr)
        , m_mutatedVarsIn(nullptr)
    {
    }

    void             Run();
    const VARSET_TP& GetMutatedVarsIn(BasicBlock* block) const;

private:
    void ComputePerBlockMutatedVars();
    void ComputeInterBlockDefaultValues();

#ifdef DEBUG
    void DumpMutatedVars();
    void DumpMutatedVarsIn();
#endif
};

// See PreservedValueAnalysis::Run for an explanation of the analysis.
class PreservedValueAnalysis
{
    Compiler* m_compiler;

    BitVecTraits m_blockTraits;

    // Blocks that have awaits in them.
    BitVec m_awaitBlocks;

    // Blocks that may be entered after we resumed.
    BitVec m_resumeReachableBlocks;

    // Per-block set of locals that may be mutated by each block after a resumption.
    VARSET_TP* m_mutatedVars;

    // Per-block incoming set of locals possibly mutated since previous resumption.
    VARSET_TP* m_mutatedVarsIn;

public:
    PreservedValueAnalysis(Compiler* compiler)
        : m_compiler(compiler)
        , m_blockTraits(compiler->fgBBNumMax + 1, compiler)
        , m_awaitBlocks(BitVecOps::UninitVal())
        , m_resumeReachableBlocks(BitVecOps::UninitVal())
        , m_mutatedVars(nullptr)
        , m_mutatedVarsIn(nullptr)
    {
    }

    void             Run(ArrayStack<BasicBlock*>& awaitBlocks);
    const VARSET_TP& GetMutatedVarsIn(BasicBlock* block) const;
    bool             IsResumeReachable(BasicBlock* block);

private:
    void ComputeResumeReachableBlocks(ArrayStack<BasicBlock*>& awaitBlocks);
    void ComputePerBlockMutatedVars();
    void ComputeInterBlockMutatedVars();

#ifdef DEBUG
    void DumpAwaitBlocks();
    void DumpResumeReachableBlocks();
    void DumpMutatedVars();
    void DumpMutatedVarsIn();
#endif
};

class AsyncAnalysis
{
    Compiler*               m_compiler;
    TreeLifeUpdater<false>  m_updater;
    unsigned                m_numVars;
    DefaultValueAnalysis&   m_defaultValueAnalysis;
    PreservedValueAnalysis& m_preservedValueAnalysis;
    VARSET_TP               m_mutatedValues;
    bool                    m_resumeReachable = false;
    VARSET_TP               m_mutatedSinceResumption;

public:
    AsyncAnalysis(Compiler*               comp,
                  DefaultValueAnalysis&   defaultValueAnalysis,
                  PreservedValueAnalysis& preservedValueAnalysis)
        : m_compiler(comp)
        , m_updater(comp)
        , m_numVars(comp->lvaCount)
        , m_defaultValueAnalysis(defaultValueAnalysis)
        , m_preservedValueAnalysis(preservedValueAnalysis)
        , m_mutatedValues(VarSetOps::MakeEmpty(comp))
        , m_mutatedSinceResumption(VarSetOps::MakeEmpty(comp))
    {
    }

    void StartBlock(BasicBlock* block);
    void Update(GenTree* node);
    bool IsLive(unsigned lclNum);
    bool IsResumeReachable() const
    {
        return m_resumeReachable;
    }

    VARSET_TP GetMutatedSinceResumption() const
    {
        return m_mutatedSinceResumption;
    }

    template <typename Functor>
    void GetLiveLocals(ContinuationLayoutBuilder* layoutBuilder, Functor includeLocal)
    {
        for (unsigned lclNum = 0; lclNum < m_numVars; lclNum++)
        {
            if (includeLocal(lclNum) && IsLive(lclNum))
            {
                layoutBuilder->AddLocal(lclNum);
            }
        }
    }

    static void PrintVarSet(Compiler* comp, VARSET_VALARG_TP set);
private:
    bool IsLocalCaptureUnnecessary(unsigned lclNum);
};

enum class SaveSet
{
    All,
    UnmutatedLocals,
    MutatedLocals,
};

class AsyncTransformation
{
    friend class AsyncAnalysis;

    Compiler*                  m_compiler;
    CORINFO_ASYNC_INFO*        m_asyncInfo;
    jitstd::vector<AsyncState> m_states;
    unsigned                   m_returnedContinuationVar = BAD_VAR_NUM;
    unsigned                   m_newContinuationVar      = BAD_VAR_NUM;
    unsigned                   m_reuseContinuationVar    = BAD_VAR_NUM;
    unsigned                   m_dataArrayVar            = BAD_VAR_NUM;
    unsigned                   m_gcDataArrayVar          = BAD_VAR_NUM;
    unsigned                   m_resultBaseVar           = BAD_VAR_NUM;
    unsigned                   m_exceptionVar            = BAD_VAR_NUM;
    BasicBlock*                m_lastSuspensionBB        = nullptr;
    BasicBlock*                m_lastResumptionBB        = nullptr;
    BasicBlock*                m_sharedReturnBB          = nullptr;

    void FindAwaits(ArrayStack<BasicBlock*>& blocksWithNormalAwaits,
                    ArrayStack<BasicBlock*>& blocksWithTailAwaits,
                    int*                     numNormalAwaits,
                    int*                     numTailAwaits);

    void        TransformTailAwaits(ArrayStack<BasicBlock*>& blocksWithTailAwaits);
    void        TransformTailAwait(BasicBlock* block, GenTreeCall* call, BasicBlock** remainder);
    BasicBlock* CreateTailAwaitSuspension(BasicBlock* block, GenTreeCall* call);

    void Transform(BasicBlock*               block,
                   GenTreeCall*              call,
                   jitstd::vector<GenTree*>& defs,
                   AsyncAnalysis&            analyses,
                   BasicBlock**              remainder);

    void CreateLiveSetForSuspension(BasicBlock*                     block,
                                    GenTreeCall*                    call,
                                    const jitstd::vector<GenTree*>& defs,
                                    AsyncAnalysis&                  analyses,
                                    ContinuationLayoutBuilder*      layoutBuilder);

    bool HasNonContextRestoreExceptionalFlow(BasicBlock* block);

    void LiftLIREdges(BasicBlock*                     block,
                      const jitstd::vector<GenTree*>& defs,
                      ContinuationLayoutBuilder*      layoutBuilder);

    bool ContinuationNeedsKeepAlive(AsyncAnalysis& analyses);

    void BuildContinuation(BasicBlock*                block,
                           GenTreeCall*               call,
                           bool                       needsKeepAlive,
                           ContinuationLayoutBuilder* layoutBuilder);

    CallDefinitionInfo CanonicalizeCallDefinition(BasicBlock* block, GenTreeCall* call, AsyncAnalysis* analyses);

    BasicBlock* CreateSuspensionBlock(BasicBlock* block, unsigned stateNum);
    void        CreateSuspension(BasicBlock*                      callBlock,
                                 GenTreeCall*                     call,
                                 BasicBlock*                      suspendBB,
                                 unsigned                         stateNum,
                                 const ContinuationLayout&        layout,
                                 const ContinuationLayoutBuilder& subLayout,
                                 bool                             resumeReachable,
                                 VARSET_VALARG_TP                 mutatedSinceResumption);

    GenTreeCall* CreateAllocContinuationCall(bool                      hasKeepAlive,
                                             GenTree*                  prevContinuation,
                                             const ContinuationLayout& layout);

    void        FillInDataOnSuspension(GenTreeCall*                     call,
                                       const ContinuationLayout&        layout,
                                       const ContinuationLayoutBuilder& subLayout,
                                       BasicBlock*                      suspendBB,
                                       VARSET_VALARG_TP                 mutatedSinceResumption,
                                       SaveSet                          saveSet);
    SaveSet     GetLocalSaveSet(const LclVarDsc* dsc, VARSET_VALARG_TP mutatedSinceResumption);
    void        RestoreContexts(BasicBlock* block, GenTreeCall* call, BasicBlock* insertionBB);
    void        CreateCheckAndSuspendAfterCall(BasicBlock*               block,
                                               GenTreeCall*              call,
                                               const CallDefinitionInfo& callDefInfo,
                                               BasicBlock*               suspendBB,
                                               BasicBlock**              remainder);
    BasicBlock* CreateResumptionBlock(BasicBlock* remainder, unsigned stateNum);
    void        CreateResumption(BasicBlock*                      callBlock,
                                 GenTreeCall*                     call,
                                 BasicBlock*                      resumeBB,
                                 const CallDefinitionInfo&        callDefInfo,
                                 const ContinuationLayout&        layout,
                                 const ContinuationLayoutBuilder& subLayout);

    void        RestoreFromDataOnResumption(const ContinuationLayout&        layout,
                                            const ContinuationLayoutBuilder& subLayout,
                                            BasicBlock*                      resumeBB);
    BasicBlock* RethrowExceptionOnResumption(BasicBlock* block, const ContinuationLayout& layout, BasicBlock* resumeBB);
    void        CopyReturnValueOnResumption(GenTreeCall*              call,
                                            const CallDefinitionInfo& callDefInfo,
                                            const ContinuationLayout& layout,
                                            BasicBlock*               storeResultBB);

    GenTreeIndir*    LoadFromOffset(GenTree*     base,
                                    unsigned     offset,
                                    var_types    type,
                                    GenTreeFlags indirFlags = GTF_IND_NONFAULTING);
    GenTreeStoreInd* StoreAtOffset(GenTree*     base,
                                   unsigned     offset,
                                   GenTree*     value,
                                   var_types    storeType,
                                   GenTreeFlags indirFlags = GTF_IND_NONFAULTING);

    void     CreateDebugInfoForSuspensionPoint(const ContinuationLayout&        layout,
                                               const ContinuationLayoutBuilder& subLayout);
    unsigned GetReturnedContinuationVar();
    unsigned GetNewContinuationVar();
    unsigned GetResultBaseVar();
    unsigned GetExceptionVar();
    void     CreateSharedReturnBB();
    bool     ReuseContinuations();
    void     CreateResumptionsAndSuspensions();
    void     CreateResumptionSwitch();

public:
    AsyncTransformation(Compiler* comp)
        : m_compiler(comp)
        , m_states(comp->getAllocator(CMK_Async))
    {
    }

    PhaseStatus Run();
};
