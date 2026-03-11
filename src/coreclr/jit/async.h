// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    bool m_needsOSRILOffset = false;
    bool m_needsException = false;
    bool m_needsContinuationContext = false;
    bool m_needsKeepAlive = false;
    bool m_needsExecutionContext = false;

    var_types ReturnType = TYP_VOID;
    ClassLayout* ReturnLayout = nullptr;

    jitstd::vector<unsigned> m_locals;

public:
    ContinuationLayoutBuilder(Compiler* compiler)
        : m_compiler(compiler)
        , m_locals(compiler->getAllocator(CMK_Async))
    {
    }

    void SetNeedsOSRILOffset()
    {
        m_needsOSRILOffset = true;
    }
    void SetNeedsException()
    {
        m_needsException = true;
    }
    void SetNeedsContinuationContext()
    {
        m_needsContinuationContext = true;
    }
    void SetNeedsKeepAlive()
    {
        m_needsKeepAlive = true;
    }
    void SetNeedsExecutionContext()
    {
        m_needsExecutionContext = true;
    }
    void SetReturn(var_types type, ClassLayout* layout);
    void AddLocal(unsigned lclNum);

    const jitstd::vector<unsigned> Locals() const
    {
        return m_locals;
    }
};

struct ContinuationLayout
{
    unsigned                             Size                      = 0;
    unsigned                             OSRILOffset               = UINT_MAX;
    unsigned                             ExceptionOffset           = UINT_MAX;
    unsigned                             ContinuationContextOffset = UINT_MAX;
    unsigned                             KeepAliveOffset           = UINT_MAX;
    ClassLayout*                         ReturnStructLayout        = nullptr;
    unsigned                             ReturnAlignment           = 0;
    unsigned                             ReturnSize                = 0;
    unsigned                             ReturnValOffset           = UINT_MAX;
    unsigned                             ExecutionContextOffset    = UINT_MAX;
    const jitstd::vector<LiveLocalInfo>& Locals;
    CORINFO_CLASS_HANDLE                 ClassHnd = NO_CLASS_HANDLE;

    explicit ContinuationLayout(const jitstd::vector<LiveLocalInfo>& locals)
        : Locals(locals)
    {
    }

    unsigned ReturnHeapAlignment() const
    {
        return std::min(ReturnAlignment, (unsigned)TARGET_POINTER_SIZE);
    }
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
    AsyncState(ContinuationLayoutBuilder* layout, BasicBlock* suspensionBB, BasicBlock* resumptionBB)
        : Layout(layout)
        , SuspensionBB(suspensionBB)
        , ResumptionBB(resumptionBB)
    {
    }

    ContinuationLayoutBuilder* Layout;
    BasicBlock* SuspensionBB;
    BasicBlock* ResumptionBB;
};

class AsyncTransformation
{
    friend class AsyncLiveness;

    Compiler*                     m_compiler;
    CORINFO_ASYNC_INFO*           m_asyncInfo;
    jitstd::vector<AsyncState>    m_states;
    unsigned                      m_returnedContinuationVar = BAD_VAR_NUM;
    unsigned                      m_newContinuationVar      = BAD_VAR_NUM;
    unsigned                      m_dataArrayVar            = BAD_VAR_NUM;
    unsigned                      m_gcDataArrayVar          = BAD_VAR_NUM;
    unsigned                      m_resultBaseVar           = BAD_VAR_NUM;
    unsigned                      m_exceptionVar            = BAD_VAR_NUM;
    BasicBlock*                   m_lastSuspensionBB        = nullptr;
    BasicBlock*                   m_lastResumptionBB        = nullptr;
    BasicBlock*                   m_sharedReturnBB          = nullptr;

    void        TransformTailAwait(BasicBlock* block, GenTreeCall* call, BasicBlock** remainder);
    BasicBlock* CreateTailAwaitSuspension(BasicBlock* block, GenTreeCall* call);

    bool IsLive(unsigned lclNum);
    void Transform(BasicBlock*               block,
                   GenTreeCall*              call,
                   jitstd::vector<GenTree*>& defs,
                   class AsyncLiveness&      life,
                   BasicBlock**              remainder);

    void CreateLiveSetForSuspension(BasicBlock*                     block,
                                    GenTreeCall*                    call,
                                    const jitstd::vector<GenTree*>& defs,
                                    AsyncLiveness&                  life,
                                    ContinuationLayoutBuilder*      layoutBuilder);

    bool HasNonContextRestoreExceptionalFlow(BasicBlock* block);

    void LiftLIREdges(BasicBlock*                     block,
                      const jitstd::vector<GenTree*>& defs,
                      ContinuationLayoutBuilder*      layoutBuilder);

    bool ContinuationNeedsKeepAlive(class AsyncLiveness& life);

    void BuildContinuation(BasicBlock* block,
                           GenTreeCall* call,
                           bool needsKeepAlive,
                           ContinuationLayoutBuilder* layoutBuilder);

    ContinuationLayout LayOutContinuation(BasicBlock*                    block,
                                          GenTreeCall*                   call,
                                          bool                           needsKeepAlive,
                                          jitstd::vector<LiveLocalInfo>& liveLocals);

    CallDefinitionInfo CanonicalizeCallDefinition(BasicBlock* block, GenTreeCall* call, AsyncLiveness* life);

    BasicBlock* CreateSuspensionBlock(BasicBlock* block, GenTreeCall* call, unsigned stateNum);
    BasicBlock* CreateSuspension(
        BasicBlock* block, GenTreeCall* call, unsigned stateNum, AsyncLiveness& life, const ContinuationLayout& layout);
    GenTreeCall* CreateAllocContinuationCall(AsyncLiveness&            life,
                                             GenTree*                  prevContinuation,
                                             const ContinuationLayout& layout);
    void         FillInDataOnSuspension(GenTreeCall* call, const ContinuationLayout& layout, BasicBlock* suspendBB);
    void         RestoreContexts(BasicBlock* block, GenTreeCall* call, BasicBlock* suspendBB);
    void         CreateCheckAndSuspendAfterCall(BasicBlock*               block,
                                                GenTreeCall*              call,
                                                const CallDefinitionInfo& callDefInfo,
                                                BasicBlock*               suspendBB,
                                                BasicBlock**              remainder);
    BasicBlock*  CreateResumption(BasicBlock*               block,
                                  BasicBlock*               remainder,
                                  GenTreeCall*              call,
                                  const CallDefinitionInfo& callDefInfo,
                                  unsigned                  stateNum,
                                  const ContinuationLayout& layout);
    void         SetSuspendedIndicator(BasicBlock* block, BasicBlock* callBlock, GenTreeCall* call);
    void         RestoreFromDataOnResumption(const ContinuationLayout& layout, BasicBlock* resumeBB);
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

    void        CreateDebugInfoForSuspensionPoint(const ContinuationLayout& layout);
    unsigned    GetReturnedContinuationVar();
    unsigned    GetNewContinuationVar();
    unsigned    GetResultBaseVar();
    unsigned    GetExceptionVar();
    BasicBlock* GetSharedReturnBB();

    void CreateResumptionSwitch();

public:
    AsyncTransformation(Compiler* comp)
        : m_compiler(comp)
        , m_states(comp->getAllocator(CMK_Async))
    {
    }

    PhaseStatus Run();
};
