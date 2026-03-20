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
    bool      m_needsOSRILOffset         = false;
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

    void SetNeedsOSRILOffset()
    {
        m_needsOSRILOffset = true;
    }
    bool NeedsOSRILOffset() const
    {
        return m_needsOSRILOffset;
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
    unsigned                      OSRILOffset               = UINT_MAX;
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
               BasicBlock*                resumptionBB)
        : Number(number)
        , Layout(layout)
        , CallBlock(callBlock)
        , Call(call)
        , CallDefInfo(callDefInfo)
        , SuspensionBB(suspensionBB)
        , ResumptionBB(resumptionBB)
    {
    }

    unsigned                   Number;
    ContinuationLayoutBuilder* Layout;
    BasicBlock*                CallBlock;
    GenTreeCall*               Call;
    CallDefinitionInfo         CallDefInfo;
    BasicBlock*                SuspensionBB;
    BasicBlock*                ResumptionBB;
};

class AsyncTransformation
{
    friend class AsyncLiveness;

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

    void BuildContinuation(BasicBlock*                block,
                           GenTreeCall*               call,
                           bool                       needsKeepAlive,
                           ContinuationLayoutBuilder* layoutBuilder);

    CallDefinitionInfo CanonicalizeCallDefinition(BasicBlock* block, GenTreeCall* call, AsyncLiveness* life);

    BasicBlock* CreateSuspensionBlock(BasicBlock* block, unsigned stateNum);
    void        CreateSuspension(BasicBlock*                      callBlock,
                                 GenTreeCall*                     call,
                                 BasicBlock*                      suspendBB,
                                 unsigned                         stateNum,
                                 const ContinuationLayout&        layout,
                                 const ContinuationLayoutBuilder& subLayout);

    GenTreeCall* CreateAllocContinuationCall(bool                      hasKeepAlive,
                                             GenTree*                  prevContinuation,
                                             const ContinuationLayout& layout);
    void         FillInDataOnSuspension(GenTreeCall*                     call,
                                        const ContinuationLayout&        layout,
                                        const ContinuationLayoutBuilder& subLayout,
                                        BasicBlock*                      suspendBB);
    void         RestoreContexts(BasicBlock* block, GenTreeCall* call, BasicBlock* insertionBB);
    void         CreateCheckAndSuspendAfterCall(BasicBlock*               block,
                                                GenTreeCall*              call,
                                                const CallDefinitionInfo& callDefInfo,
                                                BasicBlock*               suspendBB,
                                                BasicBlock**              remainder);
    BasicBlock*  CreateResumptionBlock(BasicBlock* remainder, unsigned stateNum);
    void         CreateResumption(BasicBlock*                      callBlock,
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

    void        CreateDebugInfoForSuspensionPoint(const ContinuationLayout&        layout,
                                                  const ContinuationLayoutBuilder& subLayout);
    unsigned    GetReturnedContinuationVar();
    unsigned    GetNewContinuationVar();
    unsigned    GetResultBaseVar();
    unsigned    GetExceptionVar();
    BasicBlock* GetSharedReturnBB();

    bool ReuseContinuations();
    void CreateResumptionsAndSuspensions();
    void CreateResumptionSwitch();

public:
    AsyncTransformation(Compiler* comp)
        : m_compiler(comp)
        , m_states(comp->getAllocator(CMK_Async))
    {
    }

    PhaseStatus Run();
};
