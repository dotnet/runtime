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
};

struct ContinuationLayout
{
    unsigned                             Size                      = 0;
    ClassLayout*                         ReturnStructLayout        = nullptr;
    unsigned                             ReturnAlignment           = 0;
    unsigned                             ReturnSize                = 0;
    unsigned                             ReturnValOffset           = UINT_MAX;
    unsigned                             ExceptionOffset           = UINT_MAX;
    unsigned                             ExecContextOffset         = UINT_MAX;
    unsigned                             ContinuationContextOffset = UINT_MAX;
    const jitstd::vector<LiveLocalInfo>& Locals;
    CORINFO_CLASS_HANDLE                 ClassHnd = NO_CLASS_HANDLE;

    explicit ContinuationLayout(const jitstd::vector<LiveLocalInfo>& locals)
        : Locals(locals)
    {
    }
};

struct CallDefinitionInfo
{
    GenTreeLclVarCommon* DefinitionNode = nullptr;

    // Where to insert new IR for suspension checks.
    GenTree* InsertAfter = nullptr;
};

class AsyncTransformation
{
    friend class AsyncLiveness;

    Compiler*                     m_comp;
    jitstd::vector<LiveLocalInfo> m_liveLocalsScratch;
    CORINFO_ASYNC_INFO*           m_asyncInfo;
    jitstd::vector<BasicBlock*>   m_resumptionBBs;
    CORINFO_METHOD_HANDLE         m_resumeStub = NO_METHOD_HANDLE;
    CORINFO_CONST_LOOKUP          m_resumeStubLookup;
    unsigned                      m_returnedContinuationVar = BAD_VAR_NUM;
    unsigned                      m_newContinuationVar      = BAD_VAR_NUM;
    unsigned                      m_dataArrayVar            = BAD_VAR_NUM;
    unsigned                      m_gcDataArrayVar          = BAD_VAR_NUM;
    unsigned                      m_resultBaseVar           = BAD_VAR_NUM;
    unsigned                      m_exceptionVar            = BAD_VAR_NUM;
    BasicBlock*                   m_lastSuspensionBB        = nullptr;
    BasicBlock*                   m_lastResumptionBB        = nullptr;
    BasicBlock*                   m_sharedReturnBB          = nullptr;

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
                                    jitstd::vector<LiveLocalInfo>&  liveLocals);

    void LiftLIREdges(BasicBlock*                     block,
                      const jitstd::vector<GenTree*>& defs,
                      jitstd::vector<LiveLocalInfo>&  liveLocals);

    ContinuationLayout LayOutContinuation(BasicBlock*                    block,
                                          GenTreeCall*                   call,
                                          jitstd::vector<LiveLocalInfo>& liveLocals);

    void ClearSuspendedIndicator(BasicBlock* block, GenTreeCall* call);

    CallDefinitionInfo CanonicalizeCallDefinition(BasicBlock* block, GenTreeCall* call, AsyncLiveness& life);

    BasicBlock* CreateSuspension(
        BasicBlock* block, GenTreeCall* call, unsigned stateNum, AsyncLiveness& life, const ContinuationLayout& layout);
    GenTreeCall* CreateAllocContinuationCall(AsyncLiveness&       life,
                                             GenTree*             prevContinuation,
                                             CORINFO_CLASS_HANDLE contClassHnd);
    void         FillInDataOnSuspension(GenTreeCall* call, const ContinuationLayout& layout, BasicBlock* suspendBB);
    void         CreateCheckAndSuspendAfterCall(BasicBlock*               block,
                                                GenTreeCall*              call,
                                                const CallDefinitionInfo& callDefInfo,
                                                AsyncLiveness&            life,
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
    GenTreeStoreInd* StoreAtOffset(GenTree* base, unsigned offset, GenTree* value, var_types storeType);

    unsigned GetResultBaseVar();
    unsigned GetExceptionVar();

    GenTree* CreateResumptionStubAddrTree();
    GenTree* CreateFunctionTargetAddr(CORINFO_METHOD_HANDLE methHnd, const CORINFO_CONST_LOOKUP& lookup);

    void CreateResumptionSwitch();

public:
    AsyncTransformation(Compiler* comp)
        : m_comp(comp)
        , m_liveLocalsScratch(comp->getAllocator(CMK_Async))
        , m_resumptionBBs(comp->getAllocator(CMK_Async))
    {
    }

    PhaseStatus Run();
};
