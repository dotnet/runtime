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

    // Pseudo-local tracking conditinal escapes
    unsigned m_pseudoLocal = BAD_VAR_NUM;

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

typedef JitHashTable<unsigned, JitSmallPrimitiveKeyFuncs<unsigned>, CloneInfo*> CloneMap;

class ObjectAllocator final : public Phase
{
    typedef SmallHashTable<unsigned int, unsigned int, 8U> LocalToLocalMap;
    enum ObjectAllocationType
    {
        OAT_NONE,
        OAT_NEWOBJ,
        OAT_NEWARR
    };

    //===============================================================================
    // Data members
    bool         m_IsObjectStackAllocationEnabled;
    bool         m_AnalysisDone;
    BitVecTraits m_bitVecTraits;
    BitVec       m_EscapingPointers;
    // We keep the set of possibly-stack-pointing pointers as a superset of the set of
    // definitely-stack-pointing pointers. All definitely-stack-pointing pointers are in both sets.
    BitVec              m_PossiblyStackPointingPointers;
    BitVec              m_DefinitelyStackPointingPointers;
    LocalToLocalMap     m_HeapLocalToStackLocalMap;
    BitSetShortLongRep* m_ConnGraphAdjacencyMatrix;
    unsigned int        m_StackAllocMaxSize;

    // Info for conditionally-escaping locals
    LocalToLocalMap m_EnumeratorLocalToPseudoLocalMap;
    CloneMap        m_CloneMap;
    unsigned        m_maxPseudoLocals;
    unsigned        m_numPseudoLocals;
    unsigned        m_regionsToClone;

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
    bool         CanLclVarEscape(unsigned int lclNum);
    void         MarkLclVarAsPossiblyStackPointing(unsigned int lclNum);
    void         MarkLclVarAsDefinitelyStackPointing(unsigned int lclNum);
    bool         MayLclVarPointToStack(unsigned int lclNum);
    bool         DoesLclVarPointToStack(unsigned int lclNum);
    void         DoAnalysis();
    void         MarkLclVarAsEscaping(unsigned int lclNum);
    void         MarkEscapingVarsAndBuildConnGraph();
    void         AddConnGraphEdge(unsigned int sourceLclNum, unsigned int targetLclNum);
    void         ComputeEscapingNodes(BitVecTraits* bitVecTraits, BitVec& escapingNodes);
    void         ComputeStackObjectPointers(BitVecTraits* bitVecTraits);
    bool         MorphAllocObjNodes();
    void         RewriteUses();
    GenTree*     MorphAllocObjNodeIntoHelperCall(GenTreeAllocObj* allocObj);
    unsigned int MorphAllocObjNodeIntoStackAlloc(
        GenTreeAllocObj* allocObj, CORINFO_CLASS_HANDLE clsHnd, bool isValueClass, BasicBlock* block, Statement* stmt);
    unsigned int MorphNewArrNodeIntoStackAlloc(GenTreeCall*         newArr,
                                               CORINFO_CLASS_HANDLE clsHnd,
                                               unsigned int         length,
                                               unsigned int         blockSize,
                                               BasicBlock*          block,
                                               Statement*           stmt);
    struct BuildConnGraphVisitorCallbackData;
    bool CanLclVarEscapeViaParentStack(ArrayStack<GenTree*>* parentStack, unsigned int lclNum, BasicBlock* block);
    void UpdateAncestorTypes(GenTree* tree, ArrayStack<GenTree*>* parentStack, var_types newType);

    // Conditionally escaping allocation support
    //
    void     CheckForGuardedAllocationOrCopy(BasicBlock* block, Statement* stmt, GenTree** use, unsigned lclNum);
    bool     CheckForGuardedUse(BasicBlock* block, GenTree* tree, unsigned lclNum);
    bool     CheckForEnumeratorUse(unsigned lclNum, unsigned dstLclNum);
    bool     IsGuarded(BasicBlock* block, GenTree* tree, GuardInfo* info, bool testOutcome);
    GenTree* IsGuard(BasicBlock* block, GuardInfo* info);
    unsigned NewPseudoLocal();

    bool CanHavePseudoLocals()
    {
        return (m_maxPseudoLocals > 0);
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
};

//===============================================================================

inline ObjectAllocator::ObjectAllocator(Compiler* comp)
    : Phase(comp, PHASE_ALLOCATE_OBJECTS)
    , m_IsObjectStackAllocationEnabled(false)
    , m_AnalysisDone(false)
    , m_bitVecTraits(BitVecTraits(comp->lvaCount, comp))
    , m_HeapLocalToStackLocalMap(comp->getAllocator(CMK_ObjectAllocator))
    , m_EnumeratorLocalToPseudoLocalMap(comp->getAllocator(CMK_ObjectAllocator))
    , m_CloneMap(comp->getAllocator(CMK_ObjectAllocator))
    , m_maxPseudoLocals(0)
    , m_numPseudoLocals(0)
    , m_regionsToClone(0)

{
    // If we are going to do any conditional escape analysis, allocate
    // extra BV space for the "pseudo" locals we'll need.
    //
    // For now, disable conditional escape analysis with OSR
    // since the dominance picture is muddled at this point.
    //
    // The conditionally escaping allocation sites will likely be in loops anyways.
    //
    bool const hasEnumeratorLocals = comp->hasImpEnumeratorGdvLocalMap();

    if (hasEnumeratorLocals)
    {
        unsigned const enumeratorLocalCount = comp->getImpEnumeratorGdvLocalMap()->GetCount();
        assert(enumeratorLocalCount > 0);

        bool const enableConditionalEscape = JitConfig.JitObjectStackAllocationConditionalEscape() > 0;
        bool const isOSR                   = comp->opts.IsOSR();

        if (enableConditionalEscape && !isOSR)
        {

#ifdef DEBUG
            static ConfigMethodRange JitObjectStackAllocationConditionalEscapeRange;
            JitObjectStackAllocationConditionalEscapeRange.EnsureInit(
                JitConfig.JitObjectStackAllocationConditionalEscapeRange());
            const unsigned hash    = comp->info.compMethodHash();
            const bool     inRange = JitObjectStackAllocationConditionalEscapeRange.Contains(hash);
#else
            const bool inRange = true;
#endif

            if (inRange)
            {
                m_maxPseudoLocals = enumeratorLocalCount;
                m_bitVecTraits    = BitVecTraits(comp->lvaCount + enumeratorLocalCount + 1, comp);
                JITDUMP("Enabling conditional escape analysis [%u pseudo-vars]\n", enumeratorLocalCount);
            }
            else
            {
                JITDUMP("Not enabling conditional escape analysis (disabled by range config)\n");
            }
        }
        else
        {
            JITDUMP("Not enabling conditional escape analysis [%u pseudo-vars]: %s\n", enumeratorLocalCount,
                    enableConditionalEscape ? "OSR" : "disabled by config");
        }
    }

    m_EscapingPointers                = BitVecOps::UninitVal();
    m_PossiblyStackPointingPointers   = BitVecOps::UninitVal();
    m_DefinitelyStackPointingPointers = BitVecOps::UninitVal();
    m_ConnGraphAdjacencyMatrix        = nullptr;

    m_StackAllocMaxSize = (unsigned)JitConfig.JitObjectStackAllocationSize();
}

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
// CanAllocateLclVarOnStack: Returns true iff local variable can be
//                           allocated on the stack.
//
// Arguments:
//    lclNum   - Local variable number
//    clsHnd   - Class/struct handle of the variable class
//    allocType - Type of allocation (newobj or newarr)
//    length    - Length of the array (for newarr)
//    blockSize - [out, optional] exact size of the object
//    reason   - [out, required] if result is false, reason why
//    preliminaryCheck - if true, allow checking before analysis is done
//                 (for things that inherently disqualify the local)
//
// Return Value:
//    Returns true iff local variable can be allocated on the stack.
//
inline bool ObjectAllocator::CanAllocateLclVarOnStack(unsigned int         lclNum,
                                                      CORINFO_CLASS_HANDLE clsHnd,
                                                      ObjectAllocationType allocType,
                                                      ssize_t              length,
                                                      unsigned int*        blockSize,
                                                      const char**         reason,
                                                      bool                 preliminaryCheck)
{
    assert(preliminaryCheck || m_AnalysisDone);

    bool enableBoxedValueClasses = true;
    bool enableRefClasses        = true;
    bool enableArrays            = true;
    *reason                      = "[ok]";

#ifdef DEBUG
    enableBoxedValueClasses = (JitConfig.JitObjectStackAllocationBoxedValueClass() != 0);
    enableRefClasses        = (JitConfig.JitObjectStackAllocationRefClass() != 0);
    enableArrays            = (JitConfig.JitObjectStackAllocationArray() != 0);
#endif

    unsigned classSize = 0;

    if (allocType == OAT_NEWARR)
    {
        if (!enableArrays)
        {
            *reason = "[disabled by config]";
            return false;
        }

        if ((length < 0) || (length > CORINFO_Array_MaxLength))
        {
            *reason = "[invalid array length]";
            return false;
        }

        ClassLayout* const layout = comp->typGetArrayLayout(clsHnd, (unsigned)length);
        classSize                 = layout->GetSize();
    }
    else if (allocType == OAT_NEWOBJ)
    {
        if (comp->info.compCompHnd->isValueClass(clsHnd))
        {
            if (!enableBoxedValueClasses)
            {
                *reason = "[disabled by config]";
                return false;
            }

            if (comp->info.compCompHnd->getTypeForBoxOnStack(clsHnd) == NO_CLASS_HANDLE)
            {
                *reason = "[no boxed type available]";
                return false;
            }

            classSize = comp->info.compCompHnd->getClassSize(clsHnd);
        }
        else
        {
            if (!enableRefClasses)
            {
                *reason = "[disabled by config]";
                return false;
            }

            if (!comp->info.compCompHnd->canAllocateOnStack(clsHnd))
            {
                *reason = "[runtime disallows]";
                return false;
            }

            classSize = comp->info.compCompHnd->getHeapClassSize(clsHnd);
        }
    }
    else
    {
        assert(!"Unexpected allocation type");
        return false;
    }

    if (classSize > m_StackAllocMaxSize)
    {
        *reason = "[too large]";
        return false;
    }

    if (preliminaryCheck)
    {
        return true;
    }

    const bool escapes = CanLclVarEscape(lclNum);

    if (escapes)
    {
        *reason = "[escapes]";
        return false;
    }

    if (blockSize != nullptr)
    {
        *blockSize = classSize;
    }

    return true;
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
    return BitVecOps::IsMember(&m_bitVecTraits, m_EscapingPointers, lclNum);
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

inline bool ObjectAllocator::MayLclVarPointToStack(unsigned int lclNum)
{
    assert(m_AnalysisDone);
    return BitVecOps::IsMember(&m_bitVecTraits, m_PossiblyStackPointingPointers, lclNum);
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

inline bool ObjectAllocator::DoesLclVarPointToStack(unsigned int lclNum)
{
    assert(m_AnalysisDone);
    return BitVecOps::IsMember(&m_bitVecTraits, m_DefinitelyStackPointingPointers, lclNum);
}

//===============================================================================

#endif // OBJECTALLOC_H
