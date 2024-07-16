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

    //===============================================================================
    // Methods
public:
    ObjectAllocator(Compiler* comp);
    bool IsObjectStackAllocationEnabled() const;
    void EnableObjectStackAllocation();

protected:
    virtual PhaseStatus DoPhase() override;

private:
    bool         CanAllocateLclVarOnStack(unsigned int         lclNum,
                                          CORINFO_CLASS_HANDLE clsHnd,
                                          unsigned int         length,
                                          unsigned int*        blockSize,
                                          const char**         reason);
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
    bool CanLclVarEscapeViaParentStack(ArrayStack<GenTree*>* parentStack, unsigned int lclNum);
    void UpdateAncestorTypes(GenTree* tree, ArrayStack<GenTree*>* parentStack, var_types newType);

    static const unsigned int s_StackAllocMaxSize = 0x2000U;
};

//===============================================================================

inline ObjectAllocator::ObjectAllocator(Compiler* comp)
    : Phase(comp, PHASE_ALLOCATE_OBJECTS)
    , m_IsObjectStackAllocationEnabled(false)
    , m_AnalysisDone(false)
    , m_bitVecTraits(comp->lvaCount, comp)
    , m_HeapLocalToStackLocalMap(comp->getAllocator())
{
    m_EscapingPointers                = BitVecOps::UninitVal();
    m_PossiblyStackPointingPointers   = BitVecOps::UninitVal();
    m_DefinitelyStackPointingPointers = BitVecOps::UninitVal();
    m_ConnGraphAdjacencyMatrix        = nullptr;
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
//    reason  - [out, required] if result is false, reason why
//
// Return Value:
//    Returns true iff local variable can be allocated on the stack.
//
inline bool ObjectAllocator::CanAllocateLclVarOnStack(
    unsigned int lclNum, CORINFO_CLASS_HANDLE clsHnd, unsigned int length, unsigned int* blockSize, const char** reason)
{
    assert(m_AnalysisDone);

    bool enableBoxedValueClasses = true;
    bool enableRefClasses        = true;
    bool enableArrays            = true;
    *reason                      = "[ok]";

#ifdef DEBUG
    enableBoxedValueClasses = (JitConfig.JitObjectStackAllocationBoxedValueClass() != 0);
    enableRefClasses        = (JitConfig.JitObjectStackAllocationRefClass() != 0);
    enableArrays            = (JitConfig.JitObjectStackAllocationArray() != 0);
#endif

    unsigned int classSize = 0;

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
    else if (comp->info.compCompHnd->isSDArray(clsHnd))
    {
        if (!enableArrays)
        {
            *reason = "[disabled by config]";
            return false;
        }

        CORINFO_CLASS_HANDLE elemClsHnd = NO_CLASS_HANDLE;
        CorInfoType          corType    = comp->info.compCompHnd->getChildType(clsHnd, &elemClsHnd);
        var_types            type       = JITtype2varType(corType);
        ClassLayout*         elemLayout = type == TYP_STRUCT ? comp->typGetObjLayout(elemClsHnd) : nullptr;
        if (varTypeIsGC(type) || ((elemLayout != nullptr) && elemLayout->HasGCPtr()))
        {
            *reason = "[array contains gc refs]";
            return false;
        }

        unsigned elemSize = elemLayout != nullptr ? elemLayout->GetSize() : genTypeSize(type);
        classSize         = (unsigned int)OFFSETOF__CORINFO_Array__data + elemSize * length;
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

    if (classSize > s_StackAllocMaxSize)
    {
        *reason = "[too large]";
        return false;
    }

    const bool escapes = CanLclVarEscape(lclNum);

    if (escapes)
    {
        *reason = "[escapes]";
        return false;
    }

    if (blockSize != nullptr)
    {
        *blockSize = (unsigned int)classSize;
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
