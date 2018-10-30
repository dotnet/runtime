// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    //===============================================================================
    // Data members
    bool                m_IsObjectStackAllocationEnabled;
    bool                m_AnalysisDone;
    BitVecTraits        m_bitVecTraits;
    BitVec              m_EscapingPointers;
    LocalToLocalMap     m_HeapLocalToStackLocalMap;
    BitSetShortLongRep* m_ConnGraphAdjacencyMatrix;

    //===============================================================================
    // Methods
public:
    ObjectAllocator(Compiler* comp);
    bool IsObjectStackAllocationEnabled() const;
    void EnableObjectStackAllocation();

protected:
    virtual void DoPhase() override;

private:
    bool CanAllocateLclVarOnStack(unsigned int lclNum, CORINFO_CLASS_HANDLE clsHnd);
    bool CanLclVarEscape(unsigned int lclNum);
    void DoAnalysis();
    void MarkLclVarAsEscaping(unsigned int lclNum);
    bool IsLclVarEscaping(unsigned int lclNum);
    void MarkEscapingVarsAndBuildConnGraph();
    void AddConnGraphEdge(unsigned int sourceLclNum, unsigned int targetLclNum);
    void ComputeEscapingNodes(BitVecTraits* bitVecTraits, BitVec& escapingNodes);
    bool     MorphAllocObjNodes();
    void     RewriteUses();
    GenTree* MorphAllocObjNodeIntoHelperCall(GenTreeAllocObj* allocObj);
    unsigned int MorphAllocObjNodeIntoStackAlloc(GenTreeAllocObj* allocObj, BasicBlock* block, GenTreeStmt* stmt);
    struct BuildConnGraphVisitorCallbackData;
    bool CanLclVarEscapeViaParentStack(ArrayStack<GenTree*>* parentStack, unsigned int lclNum);
#ifdef DEBUG
    static Compiler::fgWalkResult AssertWhenAllocObjFoundVisitor(GenTree** pTree, Compiler::fgWalkData* data);
#endif // DEBUG
    static const unsigned int s_StackAllocMaxSize = 0x2000U;
};

//===============================================================================

inline ObjectAllocator::ObjectAllocator(Compiler* comp)
    : Phase(comp, "Allocate Objects", PHASE_ALLOCATE_OBJECTS)
    , m_IsObjectStackAllocationEnabled(false)
    , m_AnalysisDone(false)
    , m_bitVecTraits(comp->lvaCount, comp)
    , m_HeapLocalToStackLocalMap(comp->getAllocator())
{
    // Disable checks since this phase runs before fgComputePreds phase.
    // Checks are not expected to pass before fgComputePreds.
    doChecks                   = false;
    m_EscapingPointers         = BitVecOps::UninitVal();
    m_ConnGraphAdjacencyMatrix = nullptr;
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
//    clsHnd   - Class handle of the variable class
//
// Return Value:
//    Returns true iff local variable can be allocated on the stack.
//
// Notes:
//    Stack allocation of objects with gc fields and boxed objects is currently disabled.

inline bool ObjectAllocator::CanAllocateLclVarOnStack(unsigned int lclNum, CORINFO_CLASS_HANDLE clsHnd)
{
    assert(m_AnalysisDone);

    DWORD classAttribs = comp->info.compCompHnd->getClassAttribs(clsHnd);

    if ((classAttribs & CORINFO_FLG_CONTAINS_GC_PTR) != 0)
    {
        // TODO-ObjectStackAllocation: enable stack allocation of objects with gc fields
        return false;
    }

    if ((classAttribs & CORINFO_FLG_VALUECLASS) != 0)
    {
        // TODO-ObjectStackAllocation: enable stack allocation of boxed structs
        return false;
    }

    if (!comp->info.compCompHnd->canAllocateOnStack(clsHnd))
    {
        return false;
    }

    const unsigned int classSize = comp->info.compCompHnd->getHeapClassSize(clsHnd);

    return !CanLclVarEscape(lclNum) && (classSize <= s_StackAllocMaxSize);
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
    assert(m_AnalysisDone);
    return BitVecOps::IsMember(&m_bitVecTraits, m_EscapingPointers, lclNum);
}

//===============================================================================

#endif // OBJECTALLOC_H
