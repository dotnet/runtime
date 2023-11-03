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

        explicit LiveLocalInfo(unsigned lclNum) : LclNum(lclNum)
        {
        }
    };

    Compiler*                     m_comp;
    jitstd::vector<LiveLocalInfo> m_liveLocals;
    CORINFO_ASYNC2_INFO           m_async2Info;
    CORINFO_CLASS_HANDLE          m_objectClsHnd;
    CORINFO_CLASS_HANDLE          m_byteClsHnd;
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

    void LiftLIREdges(BasicBlock*                    block,
                      GenTree*                       beyond,
                      jitstd::vector<GenTree*>&      defs,
                      jitstd::vector<LiveLocalInfo>& liveLocals);
    bool IsLive(unsigned lclNum);
    void Transform(BasicBlock*               block,
                   GenTreeCall*              call,
                   jitstd::vector<GenTree*>& defs,
                   class AsyncLiveness&      life,
                   BasicBlock**              remainder);

    GenTreeIndir* LoadFromOffset(GenTree*     base,
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
        : m_comp(comp), m_liveLocals(comp->getAllocator(CMK_Async2)), m_resumptionBBs(comp->getAllocator(CMK_Async2))
    {
    }

    PhaseStatus Run();
};
