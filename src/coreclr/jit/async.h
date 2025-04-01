// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

struct ContinuationLayout
{
    unsigned     DataSize             = 0;
    unsigned     GCRefsCount          = 0;
    ClassLayout* ReturnStructLayout   = nullptr;
    unsigned     ReturnSize           = 0;
    bool         ReturnInGCData       = false;
    unsigned     ReturnValDataOffset  = UINT_MAX;
    unsigned     ExceptionGCDataIndex = UINT_MAX;
};

struct CallDefinitionInfo
{
    GenTreeLclVarCommon* DefinitionNode = nullptr;

    // Where to insert new IR for suspension checks.
    GenTree* InsertAfter = nullptr;
};

class Async2Transformation
{
    friend class AsyncLiveness;

    struct LiveLocalInfo
    {
        unsigned LclNum;
        unsigned Alignment;
        unsigned DataOffset;
        unsigned DataSize;
        unsigned GCDataIndex;
        unsigned GCDataCount;

        explicit LiveLocalInfo(unsigned lclNum)
            : LclNum(lclNum)
        {
        }
    };

    Compiler*                     m_comp;
    jitstd::vector<LiveLocalInfo> m_liveLocalsScratch;
    CORINFO_ASYNC2_INFO           m_async2Info;
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

    void LiftLIREdges(BasicBlock*                     block,
                      GenTree*                        beyond,
                      const jitstd::vector<GenTree*>& defs,
                      jitstd::vector<LiveLocalInfo>&  liveLocals);
    bool IsLive(unsigned lclNum);
    void Transform(BasicBlock*               block,
                   GenTreeCall*              call,
                   jitstd::vector<GenTree*>& defs,
                   class AsyncLiveness&      life,
                   BasicBlock**              remainder);

    void               CreateLiveSetForSuspension(BasicBlock*                     block,
                                                  GenTreeCall*                    call,
                                                  const jitstd::vector<GenTree*>& defs,
                                                  AsyncLiveness&                  life,
                                                  jitstd::vector<LiveLocalInfo>&  liveLocals);
    ContinuationLayout LayOutContinuation(BasicBlock*                    block,
                                          GenTreeCall*                   call,
                                          jitstd::vector<LiveLocalInfo>& liveLocals);

    CallDefinitionInfo CanonicalizeCallDefinition(BasicBlock* block, GenTreeCall* call, AsyncLiveness& life);

    BasicBlock*  CreateSuspension(BasicBlock*                    block,
                                  unsigned                       stateNum,
                                  const ContinuationLayout&      layout,
                                  AsyncLiveness&                 life,
                                  jitstd::vector<LiveLocalInfo>& liveLocals);
    GenTreeCall* CreateAllocContinuationCall(AsyncLiveness& life,
                                             GenTree*       prevContinuation,
                                             unsigned       gcRefsCount,
                                             unsigned int   dataSize);
    void         FillInGCPointersOnSuspension(jitstd::vector<LiveLocalInfo>& liveLocals, BasicBlock* suspendBB);
    void         FillInDataOnSuspension(jitstd::vector<LiveLocalInfo>& liveLocals, BasicBlock* suspendBB);
    void         CreateCheckAndSuspendAfterCall(BasicBlock*               block,
                                                const CallDefinitionInfo& callDefInfo,
                                                AsyncLiveness&            life,
                                                BasicBlock*               suspendBB,
                                                BasicBlock**              remainder);

    BasicBlock* CreateResumption(BasicBlock*                    block,
                                 BasicBlock*                    remainder,
                                 GenTreeCall*                   call,
                                 const CallDefinitionInfo&      callDefInfo,
                                 unsigned                       stateNum,
                                 const ContinuationLayout&      layout,
                                 jitstd::vector<LiveLocalInfo>& liveLocals);
    void        RestoreFromDataOnResumption(unsigned                       resumeByteArrLclNum,
                                            jitstd::vector<LiveLocalInfo>& liveLocals,
                                            BasicBlock*                    resumeBB);
    void        RestoreFromGCPointersOnResumption(unsigned                       resumeObjectArrLclNum,
                                                  jitstd::vector<LiveLocalInfo>& liveLocals,
                                                  BasicBlock*                    resumeBB);
    BasicBlock* RethrowExceptionOnResumption(BasicBlock*               block,
                                             BasicBlock*               remainder,
                                             unsigned                  resumeObjectArrLclNum,
                                             const ContinuationLayout& layout,
                                             BasicBlock*               resumeBB);
    void        CopyReturnValueOnResumption(GenTreeCall*              call,
                                            const CallDefinitionInfo& callDefInfo,
                                            unsigned                  resumeByteArrLclNum,
                                            unsigned                  resumeObjectArrLclNum,
                                            const ContinuationLayout& layout,
                                            BasicBlock*               storeResultBB);

    GenTreeIndir*    LoadFromOffset(GenTree*     base,
                                    unsigned     offset,
                                    var_types    type,
                                    GenTreeFlags indirFlags = GTF_IND_NONFAULTING);
    GenTreeStoreInd* StoreAtOffset(GenTree* base, unsigned offset, GenTree* value);

    unsigned GetDataArrayVar();
    unsigned GetGCDataArrayVar();
    unsigned GetResultBaseVar();
    unsigned GetExceptionVar();

    GenTree* CreateResumptionStubAddrTree();
    GenTree* CreateFunctionTargetAddr(CORINFO_METHOD_HANDLE methHnd, const CORINFO_CONST_LOOKUP& lookup);

    void CreateResumptionSwitch();

public:
    Async2Transformation(Compiler* comp)
        : m_comp(comp)
        , m_liveLocalsScratch(comp->getAllocator(CMK_Async2))
        , m_resumptionBBs(comp->getAllocator(CMK_Async2))
    {
    }

    PhaseStatus Run();
};
