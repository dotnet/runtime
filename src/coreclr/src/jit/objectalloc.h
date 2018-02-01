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

class ObjectAllocator final : public Phase
{
    //===============================================================================
    // Data members
    bool m_IsObjectStackAllocationEnabled;
    bool m_AnalysisDone;
    //===============================================================================
    // Methods
public:
    ObjectAllocator(Compiler* comp);
    bool IsObjectStackAllocationEnabled() const;
    void EnableObjectStackAllocation();

protected:
    virtual void DoPhase() override;

private:
    bool CanAllocateLclVarOnStack(unsigned int lclNum) const;
    void     DoAnalysis();
    void     MorphAllocObjNodes();
    GenTree* MorphAllocObjNodeIntoHelperCall(GenTreeAllocObj* allocObj);
    GenTree* MorphAllocObjNodeIntoStackAlloc(GenTreeAllocObj* allocObj, BasicBlock* block, GenTreeStmt* stmt);
#ifdef DEBUG
    static Compiler::fgWalkResult AssertWhenAllocObjFoundVisitor(GenTree** pTree, Compiler::fgWalkData* data);
#endif // DEBUG
};

//===============================================================================

inline ObjectAllocator::ObjectAllocator(Compiler* comp)
    : Phase(comp, "Allocate Objects", PHASE_ALLOCATE_OBJECTS)
    , m_IsObjectStackAllocationEnabled(false)
    , m_AnalysisDone(false)
{
}

inline bool ObjectAllocator::IsObjectStackAllocationEnabled() const
{
    return m_IsObjectStackAllocationEnabled;
}

inline void ObjectAllocator::EnableObjectStackAllocation()
{
    m_IsObjectStackAllocationEnabled = true;
}

//------------------------------------------------------------------------
// CanAllocateLclVarOnStack: Returns true iff local variable can not
//                           potentially escape from the method and
//                           can be allocated on the stack.
inline bool ObjectAllocator::CanAllocateLclVarOnStack(unsigned int lclNum) const
{
    assert(m_AnalysisDone);
    // TODO-ObjectStackAllocation
    NYI("CanAllocateLclVarOnStack");
    return false;
}

//===============================================================================

#endif // OBJECTALLOC_H
