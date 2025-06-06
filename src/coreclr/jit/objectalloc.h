// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                         ObjectAllocator                                   XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

/*****************************************************************************/
#ifndef OBJECTALLOC_H
#define OBJECTALLOC_H
/*****************************************************************************/

//===============================================================================
#include "phase.h"
#include "smallhash.h"
#include "vector.h"

// A use or def of an enumerator var in the code
//
struct EnumeratorVarAppearance
{
    EnumeratorVarAppearance(BasicBlock* block, Statement* stmt, GenTree** use, unsigned lclNum, bool isDef)
        : m_block(block)
        , m_stmt(stmt)
        , m_use(use)
        , m_lclNum(lclNum)
        , m_isDef(isDef)
        , m_isGuard(false)
    {
    }

    BasicBlock* m_block;
    Statement*  m_stmt;
    GenTree**   m_use;
    unsigned    m_lclNum;
    bool        m_isDef;
    bool        m_isGuard;
};

// Information about def and uses of enumerator vars, plus...
//
struct EnumeratorVar
{
    EnumeratorVarAppearance*                  m_def                = nullptr;
    jitstd::vector<EnumeratorVarAppearance*>* m_appearances        = nullptr;
    bool                                      m_hasMultipleDefs    = false;
    bool                                      m_isAllocTemp        = false;
    bool                                      m_isInitialAllocTemp = false;
    bool                                      m_isFinalAllocTemp   = false;
    bool                                      m_isUseTemp          = false;
};

typedef JitHashTable<unsigned, JitSmallPrimitiveKeyFuncs<unsigned>, EnumeratorVar*> EnumeratorVarMap;

// Describes a GDV guard
//
struct GuardInfo
{
    unsigned             m_local = BAD_VAR_NUM;
    CORINFO_CLASS_HANDLE m_type  = NO_CLASS_HANDLE;
    BasicBlock*          m_block = nullptr;
};

// Describes a guarded enumerator cloning candidate
//
struct CloneInfo : public GuardInfo
{
    CloneInfo()
    {
        m_blocks = BitVecOps::UninitVal();
    }

    // Pseudo-local tracking conditional escapes
    unsigned m_pseudoIndex = BAD_VAR_NUM;

    // Local allocated for the address of the enumerator
    unsigned m_enumeratorLocal = BAD_VAR_NUM;

    // Locals that must be rewritten in the clone, and map
    // to their appearances
    EnumeratorVarMap*         m_appearanceMap   = nullptr;
    unsigned                  m_appearanceCount = 0;
    jitstd::vector<unsigned>* m_allocTemps      = nullptr;

    // Where the enumerator allocation happens
    GenTree*    m_allocTree  = nullptr;
    Statement*  m_allocStmt  = nullptr;
    BasicBlock* m_allocBlock = nullptr;

    // Block holding the GDV test that decides if the enumerator will be allocated
    BasicBlock* m_domBlock = nullptr;

    // Blocks to clone (in order), and a set representation
    // of the same
    jitstd::vector<BasicBlock*>* m_blocksToClone = nullptr;
    BitVec                       m_blocks;

    // How to scale the profile in the cloned code
    weight_t m_profileScale = 0.0;

    // Status of this candidate
    bool m_checkedCanClone = false;
    bool m_canClone        = false;
    bool m_willClone       = false;
};

struct StoreInfo
{
    StoreInfo(unsigned index, bool connected = false)
        : m_index(index)
        , m_connected(connected)
    {
    }
    unsigned m_index;
    bool     m_connected;
};

typedef JitHashTable<unsigned, JitSmallPrimitiveKeyFuncs<unsigned>, CloneInfo*> CloneMap;
typedef JitHashTable<GenTree*, JitPtrKeyFuncs<GenTree>, StoreInfo>              NodeToIndexMap;

class ObjectAllocator final : public Phase
{
    enum ObjectAllocationType
    {
        OAT_NONE,
        OAT_NEWOBJ,
        OAT_NEWOBJ_HEAP,
        OAT_NEWARR
    };

    struct AllocationCandidate
    {
        AllocationCandidate(
            BasicBlock* block, Statement* statement, GenTree* tree, unsigned lclNum, ObjectAllocationType allocType)
            : m_block(block)
            , m_statement(statement)
            , m_tree(tree)
            , m_lclNum(lclNum)
            , m_allocType(allocType)
            , m_onHeapReason(nullptr)
            , m_bashCall(false)
        {
        }

        BasicBlock* const          m_block;
        Statement* const           m_statement;
        GenTree* const             m_tree;
        unsigned const             m_lclNum;
        ObjectAllocationType const m_allocType;
        const char*                m_onHeapReason;
        bool                       m_bashCall;
    };

    typedef SmallHashTable<unsigned int, unsigned int, 8U> LocalToLocalMap;

    //===============================================================================
    // Data members
    bool         m_IsObjectStackAllocationEnabled;
    bool         m_AnalysisDone;
    bool         m_isR2R;
    unsigned     m_bvCount;
    BitVecTraits m_bitVecTraits;
    unsigned     m_unknownSourceIndex;
    BitVec       m_EscapingPointers;
    // We keep the set of possibly-stack-pointing pointers as a superset of the set of
    // definitely-stack-pointing pointers. All definitely-stack-pointing pointers are in both sets.
    BitVec              m_PossiblyStackPointingPointers;
    BitVec              m_DefinitelyStackPointingPointers;
    LocalToLocalMap     m_HeapLocalToStackObjLocalMap;
    LocalToLocalMap     m_HeapLocalToStackArrLocalMap;
    BitSetShortLongRep* m_ConnGraphAdjacencyMatrix;
    unsigned int        m_StackAllocMaxSize;
    unsigned            m_stackAllocationCount;

    // Info for conditionally-escaping locals
    LocalToLocalMap m_EnumeratorLocalToPseudoIndexMap;
    CloneMap        m_CloneMap;
    unsigned        m_nextLocalIndex;
    unsigned        m_firstPseudoIndex;
    unsigned        m_numPseudos;
    unsigned        m_maxPseudos;
    unsigned        m_regionsToClone;

    // Struct fields
    bool           m_trackFields;
    NodeToIndexMap m_StoreAddressToIndexMap;

    //===============================================================================
    // Methods
public:
    ObjectAllocator(Compiler* comp);
    bool IsObjectStackAllocationEnabled() const;
    void EnableObjectStackAllocation();
    bool CanAllocateLclVarOnStack(unsigned int         lclNum,
                                  CORINFO_CLASS_HANDLE clsHnd,
                                  ObjectAllocationType allocType,
                                  ssize_t              length,
                                  unsigned int*        blockSize,
                                  const char**         reason,
                                  bool                 preliminaryCheck = false);

protected:
    virtual PhaseStatus DoPhase() override;

private:
    bool         IsTrackedType(var_types type);
    bool         IsTrackedLocal(unsigned lclNum);
    unsigned     LocalToIndex(unsigned lclNum);
    unsigned     IndexToLocal(unsigned bvIndex);
    bool         CanLclVarEscape(unsigned int lclNum);
    bool         CanIndexEscape(unsigned int index);
    void         MarkLclVarAsPossiblyStackPointing(unsigned int lclNum);
    void         MarkIndexAsPossiblyStackPointing(unsigned int index);
    void         MarkLclVarAsDefinitelyStackPointing(unsigned int lclNum);
    void         MarkIndexAsDefinitelyStackPointing(unsigned int index);
    bool         MayLclVarPointToStack(unsigned int lclNum);
    bool         DoesLclVarPointToStack(unsigned int lclNum);
    bool         MayIndexPointToStack(unsigned int index);
    bool         DoesIndexPointToStack(unsigned int index);
    void         PrepareAnalysis();
    void         DoAnalysis();
    void         MarkLclVarAsEscaping(unsigned int lclNum);
    void         MarkIndexAsEscaping(unsigned int lclNum);
    void         MarkEscapingVarsAndBuildConnGraph();
    void         AddConnGraphEdge(unsigned int sourceLclNum, unsigned int targetLclNum);
    void         AddConnGraphEdgeIndex(unsigned int sourceIndex, unsigned int targetIndex);
    void         ComputeEscapingNodes(BitVecTraits* bitVecTraits, BitVec& escapingNodes);
    void         ComputeStackObjectPointers(BitVecTraits* bitVecTraits);
    bool         MorphAllocObjNodes();
    void         MorphAllocObjNode(AllocationCandidate& candidate);
    bool         MorphAllocObjNodeHelper(AllocationCandidate& candidate);
    bool         MorphAllocObjNodeHelperArr(AllocationCandidate& candidate);
    bool         MorphAllocObjNodeHelperObj(AllocationCandidate& candidate);
    void         RewriteUses();
    GenTree*     MorphAllocObjNodeIntoHelperCall(GenTreeAllocObj* allocObj);
    unsigned int MorphAllocObjNodeIntoStackAlloc(GenTreeAllocObj* allocObj,
                                                 ClassLayout*     layout,
                                                 BasicBlock*      block,
                                                 Statement*       stmt);
    unsigned int MorphNewArrNodeIntoStackAlloc(GenTreeCall*         newArr,
                                               CORINFO_CLASS_HANDLE clsHnd,
                                               unsigned int         length,
                                               unsigned int         blockSize,
                                               BasicBlock*          block,
                                               Statement*           stmt);
    struct BuildConnGraphVisitorCallbackData;
    void AnalyzeParentStack(ArrayStack<GenTree*>* parentStack, unsigned int lclNum, BasicBlock* block);
    void UpdateAncestorTypes(
        GenTree* tree, ArrayStack<GenTree*>* parentStack, var_types newType, ClassLayout* newLayout, bool retypeFields);
    ObjectAllocationType AllocationKind(GenTree* tree);

    // Conditionally escaping allocation support
    //
    void     CheckForGuardedAllocationOrCopy(BasicBlock* block, Statement* stmt, GenTree** use, unsigned lclNum);
    bool     CheckForGuardedUse(BasicBlock* block, GenTree* tree, unsigned lclNum);
    bool     CheckForEnumeratorUse(unsigned lclNum, unsigned dstLclNum);
    bool     IsGuarded(BasicBlock* block, GenTree* tree, GuardInfo* info, bool testOutcome);
    GenTree* IsGuard(BasicBlock* block, GuardInfo* info);
    unsigned NewPseudoIndex();

    bool CanHavePseudos()
    {
        return (m_maxPseudos > 0);
    }

    void RecordAppearance(unsigned lclNum, BasicBlock* block, Statement* stmt, GenTree** use);
    bool AnalyzeIfCloningCanPreventEscape(BitVecTraits* bitVecTraits,
                                          BitVec&       escapingNodes,
                                          BitVec&       escapingNodesToProcess);
    bool CanClone(CloneInfo* info);
    bool CheckCanClone(CloneInfo* info);
    bool CloneOverlaps(CloneInfo* info);
    bool ShouldClone(CloneInfo* info);
    void CloneAndSpecialize(CloneInfo* info);
    void CloneAndSpecialize();

    static const unsigned int s_StackAllocMaxSize = 0x2000U;

    ClassLayout* GetBoxedLayout(ClassLayout* structLayout);
    ClassLayout* GetNonGCLayout(ClassLayout* existingLayout);
    ClassLayout* GetByrefLayout(ClassLayout* existingLayout);

#ifdef DEBUG
    void DumpIndex(unsigned bvIndex);
#endif
};

//------------------------------------------------------------------------
// IsObjectStackAllocationEnabled: Returns true iff object stack allocation is enabled
//
// Return Value:
//    Returns true iff object stack allocation is enabled

inline bool ObjectAllocator::IsObjectStackAllocationEnabled() const
{
    return m_IsObjectStackAllocationEnabled;
}

//------------------------------------------------------------------------
// EnableObjectStackAllocation:       Enable object stack allocation.

inline void ObjectAllocator::EnableObjectStackAllocation()
{
    m_IsObjectStackAllocationEnabled = true;
}

//------------------------------------------------------------------------
// CanIndexEscape:          Returns true iff resource described by index can
//                           potentially escape from the method
//
// Arguments:
//    index   - bv index
//
// Return Value:
//    Returns true if so

inline bool ObjectAllocator::CanIndexEscape(unsigned int index)
{
    return BitVecOps::IsMember(&m_bitVecTraits, m_EscapingPointers, index);
}

//------------------------------------------------------------------------
// CanLclVarEscape:          Returns true iff local variable can
//                           potentially escape from the method
//
// Arguments:
//    lclNum   - Local variable number
//
// Return Value:
//    Returns true iff local variable can potentially escape from the method

inline bool ObjectAllocator::CanLclVarEscape(unsigned int lclNum)
{
    if (!IsTrackedLocal(lclNum))
    {
        return true;
    }

    return CanIndexEscape(LocalToIndex(lclNum));
}

//------------------------------------------------------------------------
// MayIndexPointToStack:          Returns true iff the resource described by index may
//                                 point to a stack-allocated object
//
// Arguments:
//    index   - bv index
//
// Return Value:
//    Returns true if so.
//
inline bool ObjectAllocator::MayIndexPointToStack(unsigned int index)
{
    assert(m_AnalysisDone);
    return BitVecOps::IsMember(&m_bitVecTraits, m_PossiblyStackPointingPointers, index);
}

//------------------------------------------------------------------------
// MayLclVarPointToStack:          Returns true iff local variable may
//                                 point to a stack-allocated object
//
// Arguments:
//    lclNum   - Local variable number
//
// Return Value:
//    Returns true iff local variable may point to a stack-allocated object
//
inline bool ObjectAllocator::MayLclVarPointToStack(unsigned int lclNum)
{
    assert(m_AnalysisDone);

    if (!IsTrackedLocal(lclNum))
    {
        return false;
    }

    return MayIndexPointToStack(LocalToIndex(lclNum));
}

//------------------------------------------------------------------------
// DoesIndexPointToStack:         Returns true iff the resource described by index definitely
//                                 points to a stack-allocated object (or is null)
//
// Arguments:
//    index   - bv index
//
// Return Value:
//    Returns true if so.
//
inline bool ObjectAllocator::DoesIndexPointToStack(unsigned int index)
{
    assert(m_AnalysisDone);
    return BitVecOps::IsMember(&m_bitVecTraits, m_DefinitelyStackPointingPointers, index);
}

//------------------------------------------------------------------------
// DoesLclVarPointToStack:         Returns true iff local variable definitely
//                                 points to a stack-allocated object (or is null)
//
// Arguments:
//    lclNum   - Local variable number
//
// Return Value:
//    Returns true iff local variable definitely points to a stack-allocated object
//    (or is null)
//
inline bool ObjectAllocator::DoesLclVarPointToStack(unsigned int lclNum)
{
    assert(m_AnalysisDone);

    if (!IsTrackedLocal(lclNum))
    {
        return false;
    }

    return DoesIndexPointToStack(LocalToIndex(lclNum));
}

//===============================================================================

#endif // OBJECTALLOC_H
