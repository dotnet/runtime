// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"
#include "smallhash.h"

#ifdef _MSC_VER
#pragma hdrstop
#endif

LIR::Use::Use() : m_range(nullptr), m_edge(nullptr), m_user(nullptr)
{
}

LIR::Use::Use(const Use& other)
{
    *this = other;
}

//------------------------------------------------------------------------
// LIR::Use::Use: Constructs a use <-> def edge given the range that
//                contains the use and the def, the use -> def edge, and
//                the user.
//
// Arguments:
//    range - The range that contains the use and the def.
//    edge - The use -> def edge.
//    user - The node that uses the def.
//
// Return Value:
//
LIR::Use::Use(Range& range, GenTree** edge, GenTree* user) : m_range(&range), m_edge(edge), m_user(user)
{
    AssertIsValid();
}

LIR::Use& LIR::Use::operator=(const Use& other)
{
    m_range = other.m_range;
    m_user  = other.m_user;
    m_edge  = other.IsDummyUse() ? &m_user : other.m_edge;

    assert(IsDummyUse() == other.IsDummyUse());
    return *this;
}

LIR::Use& LIR::Use::operator=(Use&& other)
{
    *this = other;
    return *this;
}

//------------------------------------------------------------------------
// LIR::Use::GetDummyUse: Returns a dummy use for a node.
//
// This method is provided as a convenience to allow transforms to work
// uniformly over Use values. It allows the creation of a Use given a node
// that is not used.
//
// Arguments:
//    range - The range that contains the node.
//    node - The node for which to create a dummy use.
//
// Return Value:
//
LIR::Use LIR::Use::GetDummyUse(Range& range, GenTree* node)
{
    assert(node != nullptr);

    Use dummyUse;
    dummyUse.m_range = &range;
    dummyUse.m_user  = node;
    dummyUse.m_edge  = &dummyUse.m_user;

    assert(dummyUse.IsInitialized());
    return dummyUse;
}

//------------------------------------------------------------------------
// LIR::Use::IsDummyUse: Indicates whether or not a use is a dummy use.
//
// This method must be called before attempting to call the User() method
// below: for dummy uses, the user is the same node as the def.
//
// Return Value: true if this use is a dummy use; false otherwise.
//
bool LIR::Use::IsDummyUse() const
{
    return m_edge == &m_user;
}

//------------------------------------------------------------------------
// LIR::Use::Def: Returns the node that produces the def for this use.
//
GenTree* LIR::Use::Def() const
{
    assert(IsInitialized());

    return *m_edge;
}

//------------------------------------------------------------------------
// LIR::Use::User: Returns the node that uses the def for this use.
///
GenTree* LIR::Use::User() const
{
    assert(IsInitialized());
    assert(!IsDummyUse());

    return m_user;
}

//------------------------------------------------------------------------
// LIR::Use::IsInitialized: Returns true if the use is minimally valid; false otherwise.
//
bool LIR::Use::IsInitialized() const
{
    return (m_range != nullptr) && (m_user != nullptr) && (m_edge != nullptr);
}

//------------------------------------------------------------------------
// LIR::Use::AssertIsValid: DEBUG function to assert on many validity conditions.
//
void LIR::Use::AssertIsValid() const
{
    assert(IsInitialized());
    assert(m_range->Contains(m_user));
    assert(Def() != nullptr);

    GenTree** useEdge = nullptr;
    assert(m_user->TryGetUse(Def(), &useEdge));
    assert(useEdge == m_edge);
}

//------------------------------------------------------------------------
// LIR::Use::ReplaceWith: Changes the use to point to a new value.
//
// For example, given the following LIR:
//
//    t15 =    lclVar    int    arg1
//    t16 =    lclVar    int    arg1
//
//          /--*  t15 int
//          +--*  t16 int
//    t17 = *  ==        int
//
//          /--*  t17 int
//          *  jmpTrue   void
//
// If we wanted to replace the use of t17 with a use of the constant "1", we
// might do the following (where `opEq` is a `Use` value that represents the
// use of t17):
//
//    GenTree* constantOne = compiler->gtNewIconNode(1);
//    range.InsertAfter(opEq.Def(), constantOne);
//    opEq.ReplaceWith(compiler, constantOne);
//
// Which would produce something like the following LIR:
//
//    t15 =    lclVar    int    arg1
//    t16 =    lclVar    int    arg1
//
//          /--*  t15 int
//          +--*  t16 int
//    t17 = *  ==        int
//
//    t18 =    const     int    1
//
//          /--*  t18 int
//          *  jmpTrue   void
//
// Elminating the now-dead compare and its operands using `LIR::Range::Remove`
// would then give us:
//
//    t18 =    const     int    1
//
//          /--*  t18 int
//          *  jmpTrue   void
//
// Arguments:
//    compiler - The Compiler context.
//    replacement - The replacement node.
//
void LIR::Use::ReplaceWith(Compiler* compiler, GenTree* replacement)
{
    assert(IsInitialized());
    assert(compiler != nullptr);
    assert(replacement != nullptr);
    assert(IsDummyUse() || m_range->Contains(m_user));
    assert(m_range->Contains(replacement));

    GenTree* replacedNode = *m_edge;

    *m_edge = replacement;
    if (!IsDummyUse() && m_user->IsCall())
    {
        compiler->fgFixupArgTabEntryPtr(m_user, replacedNode, replacement);
    }
}

//------------------------------------------------------------------------
// LIR::Use::ReplaceWithLclVar: Assigns the def for this use to a local
//                              var and points the use to a use of that
//                              local var. If no local number is provided,
//                              creates a new local var.
//
// For example, given the following IR:
//
//    t15 =    lclVar    int    arg1
//    t16 =    lclVar    int    arg1
//
//          /--*  t15 int
//          +--*  t16 int
//    t17 = *  ==        int
//
//          /--*  t17 int
//          *  jmpTrue   void
//
// If we wanted to replace the use of t17 with a use of a new local var
// that holds the value represented by t17, we might do the following
// (where `opEq` is a `Use` value that represents the use of t17):
//
//    opEq.ReplaceUseWithLclVar(compiler, block->getBBWeight(compiler));
//
// This would produce the following LIR:
//
//    t15 =    lclVar    int    arg1
//    t16 =    lclVar    int    arg1
//
//          /--*  t15 int
//          +--*  t16 int
//    t17 = *  ==        int
//
//          /--*  t17 int
//          *  st.lclVar int    tmp0
//
//    t18 =    lclVar    int    tmp0
//
//          /--*  t18 int
//          *  jmpTrue   void
//
// Arguments:
//    compiler - The Compiler context.
//    blockWeight - The weight of the basic block that contains the use.
//    lclNum - The local to use for temporary storage. If BAD_VAR_NUM (the
//             default) is provided, this method will create and use a new
//             local var.
//
// Return Value: The number of the local var used for temporary storage.
//
unsigned LIR::Use::ReplaceWithLclVar(Compiler* compiler, unsigned blockWeight, unsigned lclNum)
{
    assert(IsInitialized());
    assert(compiler != nullptr);
    assert(m_range->Contains(m_user));
    assert(m_range->Contains(*m_edge));

    GenTree* node = *m_edge;

    if (lclNum == BAD_VAR_NUM)
    {
        lclNum = compiler->lvaGrabTemp(true DEBUGARG("ReplaceWithLclVar is creating a new local variable"));
    }

    // Increment its lvRefCnt and lvRefCntWtd twice, one for the def and one for the use
    compiler->lvaTable[lclNum].incRefCnts(blockWeight, compiler);
    compiler->lvaTable[lclNum].incRefCnts(blockWeight, compiler);

    GenTreeLclVar* store = compiler->gtNewTempAssign(lclNum, node)->AsLclVar();

    GenTree* load =
        new (compiler, GT_LCL_VAR) GenTreeLclVar(store->TypeGet(), store->AsLclVarCommon()->GetLclNum(), BAD_IL_OFFSET);

    m_range->InsertAfter(node, store, load);

    ReplaceWith(compiler, load);

    JITDUMP("ReplaceWithLclVar created store :\n");
    DISPNODE(store);

    return lclNum;
}

LIR::ReadOnlyRange::ReadOnlyRange() : m_firstNode(nullptr), m_lastNode(nullptr)
{
}

LIR::ReadOnlyRange::ReadOnlyRange(ReadOnlyRange&& other) : m_firstNode(other.m_firstNode), m_lastNode(other.m_lastNode)
{
#ifdef DEBUG
    other.m_firstNode = nullptr;
    other.m_lastNode  = nullptr;
#endif
}

//------------------------------------------------------------------------
// LIR::ReadOnlyRange::ReadOnlyRange:
//    Creates a `ReadOnlyRange` value given the first and last node in
//    the range.
//
// Arguments:
//    firstNode - The first node in the range.
//    lastNode  - The last node in the range.
//
LIR::ReadOnlyRange::ReadOnlyRange(GenTree* firstNode, GenTree* lastNode) : m_firstNode(firstNode), m_lastNode(lastNode)
{
    assert((m_firstNode == nullptr) == (m_lastNode == nullptr));
    assert((m_firstNode == m_lastNode) || (Contains(m_lastNode)));
}

//------------------------------------------------------------------------
// LIR::ReadOnlyRange::FirstNode: Returns the first node in the range.
//
GenTree* LIR::ReadOnlyRange::FirstNode() const
{
    return m_firstNode;
}

//------------------------------------------------------------------------
// LIR::ReadOnlyRange::LastNode: Returns the last node in the range.
//
GenTree* LIR::ReadOnlyRange::LastNode() const
{
    return m_lastNode;
}

//------------------------------------------------------------------------
// LIR::ReadOnlyRange::IsEmpty: Returns true if the range is empty; false
//                              otherwise.
//
bool LIR::ReadOnlyRange::IsEmpty() const
{
    assert((m_firstNode == nullptr) == (m_lastNode == nullptr));
    return m_firstNode == nullptr;
}

//------------------------------------------------------------------------
// LIR::ReadOnlyRange::begin: Returns an iterator positioned at the first
//                            node in the range.
//
LIR::ReadOnlyRange::Iterator LIR::ReadOnlyRange::begin() const
{
    return Iterator(m_firstNode);
}

//------------------------------------------------------------------------
// LIR::ReadOnlyRange::end: Returns an iterator positioned after the last
//                          node in the range.
//
LIR::ReadOnlyRange::Iterator LIR::ReadOnlyRange::end() const
{
    return Iterator(m_lastNode == nullptr ? nullptr : m_lastNode->gtNext);
}

//------------------------------------------------------------------------
// LIR::ReadOnlyRange::rbegin: Returns an iterator positioned at the last
//                             node in the range.
//
LIR::ReadOnlyRange::ReverseIterator LIR::ReadOnlyRange::rbegin() const
{
    return ReverseIterator(m_lastNode);
}

//------------------------------------------------------------------------
// LIR::ReadOnlyRange::rend: Returns an iterator positioned before the first
//                           node in the range.
//
LIR::ReadOnlyRange::ReverseIterator LIR::ReadOnlyRange::rend() const
{
    return ReverseIterator(m_firstNode == nullptr ? nullptr : m_firstNode->gtPrev);
}

#ifdef DEBUG

//------------------------------------------------------------------------
// LIR::ReadOnlyRange::Contains: Indicates whether or not this range
//                               contains a given node.
//
// Arguments:
//    node - The node to find.
//
// Return Value: True if this range contains the given node; false
//               otherwise.
//
bool LIR::ReadOnlyRange::Contains(GenTree* node) const
{
    assert(node != nullptr);

    // TODO-LIR: derive this from the # of nodes in the function as well as
    // the debug level. Checking small functions is pretty cheap; checking
    // large functions is not.
    if (JitConfig.JitExpensiveDebugCheckLevel() < 2)
    {
        return true;
    }

    for (GenTree* n : *this)
    {
        if (n == node)
        {
            return true;
        }
    }

    return false;
}

#endif

LIR::Range::Range() : ReadOnlyRange()
{
}

LIR::Range::Range(Range&& other) : ReadOnlyRange(std::move(other))
{
}

//------------------------------------------------------------------------
// LIR::Range::Range: Creates a `Range` value given the first and last
//                    node in the range.
//
// Arguments:
//    firstNode - The first node in the range.
//    lastNode  - The last node in the range.
//
LIR::Range::Range(GenTree* firstNode, GenTree* lastNode) : ReadOnlyRange(firstNode, lastNode)
{
}

//------------------------------------------------------------------------
// LIR::Range::LastPhiNode: Returns the last phi node in the range or
//                          `nullptr` if no phis exist.
//
GenTree* LIR::Range::LastPhiNode() const
{
    GenTree* lastPhiNode = nullptr;
    for (GenTree* node : *this)
    {
        if (!node->IsPhiNode())
        {
            break;
        }

        lastPhiNode = node;
    }

    return lastPhiNode;
}

//------------------------------------------------------------------------
// LIR::Range::FirstNonPhiNode: Returns the first non-phi node in the
//                              range or `nullptr` if no non-phi nodes
//                              exist.
//
GenTree* LIR::Range::FirstNonPhiNode() const
{
    for (GenTree* node : *this)
    {
        if (!node->IsPhiNode())
        {
            return node;
        }
    }

    return nullptr;
}

//------------------------------------------------------------------------
// LIR::Range::FirstNonPhiOrCatchArgNode: Returns the first node after all
//                                        phi or catch arg nodes in this
//                                        range.
//
GenTree* LIR::Range::FirstNonPhiOrCatchArgNode() const
{
    for (GenTree* node : NonPhiNodes())
    {
        if (node->OperGet() == GT_CATCH_ARG)
        {
            continue;
        }
        else if ((node->OperGet() == GT_STORE_LCL_VAR) && (node->gtGetOp1()->OperGet() == GT_CATCH_ARG))
        {
            continue;
        }

        return node;
    }

    return nullptr;
}

//------------------------------------------------------------------------
// LIR::Range::PhiNodes: Returns the range of phi nodes inside this range.
//
LIR::ReadOnlyRange LIR::Range::PhiNodes() const
{
    GenTree* lastPhiNode = LastPhiNode();
    if (lastPhiNode == nullptr)
    {
        return ReadOnlyRange();
    }

    return ReadOnlyRange(m_firstNode, lastPhiNode);
}

//------------------------------------------------------------------------
// LIR::Range::PhiNodes: Returns the range of non-phi nodes inside this
//                       range.
//
LIR::ReadOnlyRange LIR::Range::NonPhiNodes() const
{
    GenTree* firstNonPhiNode = FirstNonPhiNode();
    if (firstNonPhiNode == nullptr)
    {
        return ReadOnlyRange();
    }

    return ReadOnlyRange(firstNonPhiNode, m_lastNode);
}

//------------------------------------------------------------------------
// LIR::Range::InsertBefore: Inserts a node before another node in this range.
//
// Arguments:
//    insertionPoint - The node before which `node` will be inserted. If non-null, must be part
//                     of this range. If null, insert at the end of the range.
//    node - The node to insert. Must not be part of any range.
//
void LIR::Range::InsertBefore(GenTree* insertionPoint, GenTree* node)
{
    assert(node != nullptr);
    assert(node->gtPrev == nullptr);
    assert(node->gtNext == nullptr);

    FinishInsertBefore(insertionPoint, node, node);
}

//------------------------------------------------------------------------
// LIR::Range::InsertBefore: Inserts 2 nodes before another node in this range.
//
// Arguments:
//    insertionPoint - The node before which the nodes will be inserted. If non-null, must be part
//                     of this range. If null, insert at the end of the range.
//    node1 - The first node to insert. Must not be part of any range.
//    node2 - The second node to insert. Must not be part of any range.
//
// Notes:
// Resulting order:
//      previous insertionPoint->gtPrev <-> node1 <-> node2 <-> insertionPoint
//
void LIR::Range::InsertBefore(GenTree* insertionPoint, GenTree* node1, GenTree* node2)
{
    assert(node1 != nullptr);
    assert(node2 != nullptr);

    assert(node1->gtNext == nullptr);
    assert(node1->gtPrev == nullptr);
    assert(node2->gtNext == nullptr);
    assert(node2->gtPrev == nullptr);

    node1->gtNext = node2;
    node2->gtPrev = node1;

    FinishInsertBefore(insertionPoint, node1, node2);
}

//------------------------------------------------------------------------
// LIR::Range::InsertBefore: Inserts 3 nodes before another node in this range.
//
// Arguments:
//    insertionPoint - The node before which the nodes will be inserted. If non-null, must be part
//                     of this range. If null, insert at the end of the range.
//    node1 - The first node to insert. Must not be part of any range.
//    node2 - The second node to insert. Must not be part of any range.
//    node3 - The third node to insert. Must not be part of any range.
//
// Notes:
// Resulting order:
//      previous insertionPoint->gtPrev <-> node1 <-> node2 <-> node3 <-> insertionPoint
//
void LIR::Range::InsertBefore(GenTree* insertionPoint, GenTree* node1, GenTree* node2, GenTree* node3)
{
    assert(node1 != nullptr);
    assert(node2 != nullptr);
    assert(node3 != nullptr);

    assert(node1->gtNext == nullptr);
    assert(node1->gtPrev == nullptr);
    assert(node2->gtNext == nullptr);
    assert(node2->gtPrev == nullptr);
    assert(node3->gtNext == nullptr);
    assert(node3->gtPrev == nullptr);

    node1->gtNext = node2;

    node2->gtPrev = node1;
    node2->gtNext = node3;

    node3->gtPrev = node2;

    FinishInsertBefore(insertionPoint, node1, node3);
}

//------------------------------------------------------------------------
// LIR::Range::InsertBefore: Inserts 4 nodes before another node in this range.
//
// Arguments:
//    insertionPoint - The node before which the nodes will be inserted. If non-null, must be part
//                     of this range. If null, insert at the end of the range.
//    node1 - The first node to insert. Must not be part of any range.
//    node2 - The second node to insert. Must not be part of any range.
//    node3 - The third node to insert. Must not be part of any range.
//    node4 - The fourth node to insert. Must not be part of any range.
//
// Notes:
// Resulting order:
//      previous insertionPoint->gtPrev <-> node1 <-> node2 <-> node3 <-> node4 <-> insertionPoint
//
void LIR::Range::InsertBefore(GenTree* insertionPoint, GenTree* node1, GenTree* node2, GenTree* node3, GenTree* node4)
{
    assert(node1 != nullptr);
    assert(node2 != nullptr);
    assert(node3 != nullptr);
    assert(node4 != nullptr);

    assert(node1->gtNext == nullptr);
    assert(node1->gtPrev == nullptr);
    assert(node2->gtNext == nullptr);
    assert(node2->gtPrev == nullptr);
    assert(node3->gtNext == nullptr);
    assert(node3->gtPrev == nullptr);
    assert(node4->gtNext == nullptr);
    assert(node4->gtPrev == nullptr);

    node1->gtNext = node2;

    node2->gtPrev = node1;
    node2->gtNext = node3;

    node3->gtPrev = node2;
    node3->gtNext = node4;

    node4->gtPrev = node3;

    FinishInsertBefore(insertionPoint, node1, node4);
}

//------------------------------------------------------------------------
// LIR::Range::FinishInsertBefore: Helper function to finalize InsertBefore processing: link the
// range to insertionPoint. gtNext/gtPrev links between first and last are already set.
//
// Arguments:
//    insertionPoint - The node before which the nodes will be inserted. If non-null, must be part
//                     of this range. If null, indicates to insert at the end of the range.
//    first - The first node of the range to insert.
//    last - The last node of the range to insert.
//
// Notes:
// Resulting order:
//      previous insertionPoint->gtPrev <-> first <-> ... <-> last <-> insertionPoint
//
void LIR::Range::FinishInsertBefore(GenTree* insertionPoint, GenTree* first, GenTree* last)
{
    assert(first != nullptr);
    assert(last != nullptr);
    assert(first->gtPrev == nullptr);
    assert(last->gtNext == nullptr);

    if (insertionPoint == nullptr)
    {
        if (m_firstNode == nullptr)
        {
            m_firstNode = first;
        }
        else
        {
            assert(m_lastNode != nullptr);
            assert(m_lastNode->gtNext == nullptr);
            m_lastNode->gtNext = first;
            first->gtPrev = m_lastNode;
        }
        m_lastNode = last;
    }
    else
    {
        assert(Contains(insertionPoint));

        first->gtPrev = insertionPoint->gtPrev;
        if (first->gtPrev == nullptr)
        {
            assert(insertionPoint == m_firstNode);
            m_firstNode = first;
        }
        else
        {
            first->gtPrev->gtNext = first;
        }

        last->gtNext           = insertionPoint;
        insertionPoint->gtPrev = last;
    }
}

//------------------------------------------------------------------------
// LIR::Range::InsertAfter: Inserts a node after another node in this range.
//
// Arguments:
//    insertionPoint - The node after which `node` will be inserted. If non-null, must be part
//                     of this range. If null, insert at the beginning of the range.
//    node - The node to insert. Must not be part of any range.
//
// Notes:
// Resulting order:
//      insertionPoint <-> node <-> previous insertionPoint->gtNext
//
void LIR::Range::InsertAfter(GenTree* insertionPoint, GenTree* node)
{
    assert(node != nullptr);

    assert(node->gtNext == nullptr);
    assert(node->gtPrev == nullptr);

    FinishInsertAfter(insertionPoint, node, node);
}

//------------------------------------------------------------------------
// LIR::Range::InsertAfter: Inserts 2 nodes after another node in this range.
//
// Arguments:
//    insertionPoint - The node after which the nodes will be inserted. If non-null, must be part
//                     of this range. If null, insert at the beginning of the range.
//    node1 - The first node to insert. Must not be part of any range.
//    node2 - The second node to insert. Must not be part of any range. Inserted after node1.
//
// Notes:
// Resulting order:
//      insertionPoint <-> node1 <-> node2 <-> previous insertionPoint->gtNext
//
void LIR::Range::InsertAfter(GenTree* insertionPoint, GenTree* node1, GenTree* node2)
{
    assert(node1 != nullptr);
    assert(node2 != nullptr);

    assert(node1->gtNext == nullptr);
    assert(node1->gtPrev == nullptr);
    assert(node2->gtNext == nullptr);
    assert(node2->gtPrev == nullptr);

    node1->gtNext = node2;
    node2->gtPrev = node1;

    FinishInsertAfter(insertionPoint, node1, node2);
}

//------------------------------------------------------------------------
// LIR::Range::InsertAfter: Inserts 3 nodes after another node in this range.
//
// Arguments:
//    insertionPoint - The node after which the nodes will be inserted. If non-null, must be part
//                     of this range. If null, insert at the beginning of the range.
//    node1 - The first node to insert. Must not be part of any range.
//    node2 - The second node to insert. Must not be part of any range. Inserted after node1.
//    node3 - The third node to insert. Must not be part of any range. Inserted after node2.
//
// Notes:
// Resulting order:
//      insertionPoint <-> node1 <-> node2 <-> node3 <-> previous insertionPoint->gtNext
//
void LIR::Range::InsertAfter(GenTree* insertionPoint, GenTree* node1, GenTree* node2, GenTree* node3)
{
    assert(node1 != nullptr);
    assert(node2 != nullptr);
    assert(node3 != nullptr);

    assert(node1->gtNext == nullptr);
    assert(node1->gtPrev == nullptr);
    assert(node2->gtNext == nullptr);
    assert(node2->gtPrev == nullptr);
    assert(node3->gtNext == nullptr);
    assert(node3->gtPrev == nullptr);

    node1->gtNext = node2;

    node2->gtPrev = node1;
    node2->gtNext = node3;

    node3->gtPrev = node2;

    FinishInsertAfter(insertionPoint, node1, node3);
}

//------------------------------------------------------------------------
// LIR::Range::InsertAfter: Inserts 4 nodes after another node in this range.
//
// Arguments:
//    insertionPoint - The node after which the nodes will be inserted. If non-null, must be part
//                     of this range. If null, insert at the beginning of the range.
//    node1 - The first node to insert. Must not be part of any range.
//    node2 - The second node to insert. Must not be part of any range. Inserted after node1.
//    node3 - The third node to insert. Must not be part of any range. Inserted after node2.
//    node4 - The fourth node to insert. Must not be part of any range. Inserted after node3.
//
// Notes:
// Resulting order:
//      insertionPoint <-> node1 <-> node2 <-> node3 <-> node4 <-> previous insertionPoint->gtNext
//
void LIR::Range::InsertAfter(GenTree* insertionPoint, GenTree* node1, GenTree* node2, GenTree* node3, GenTree* node4)
{
    assert(node1 != nullptr);
    assert(node2 != nullptr);
    assert(node3 != nullptr);
    assert(node4 != nullptr);

    assert(node1->gtNext == nullptr);
    assert(node1->gtPrev == nullptr);
    assert(node2->gtNext == nullptr);
    assert(node2->gtPrev == nullptr);
    assert(node3->gtNext == nullptr);
    assert(node3->gtPrev == nullptr);
    assert(node4->gtNext == nullptr);
    assert(node4->gtPrev == nullptr);

    node1->gtNext = node2;

    node2->gtPrev = node1;
    node2->gtNext = node3;

    node3->gtPrev = node2;
    node3->gtNext = node4;

    node4->gtPrev = node3;

    FinishInsertAfter(insertionPoint, node1, node4);
}

//------------------------------------------------------------------------
// LIR::Range::FinishInsertAfter: Helper function to finalize InsertAfter processing: link the
// range to insertionPoint. gtNext/gtPrev links between first and last are already set.
//
// Arguments:
//    insertionPoint - The node after which the nodes will be inserted. If non-null, must be part
//                     of this range. If null, insert at the beginning of the range.
//    first - The first node of the range to insert.
//    last - The last node of the range to insert.
//
// Notes:
// Resulting order:
//      insertionPoint <-> first <-> ... <-> last <-> previous insertionPoint->gtNext
//
void LIR::Range::FinishInsertAfter(GenTree* insertionPoint, GenTree* first, GenTree* last)
{
    assert(first != nullptr);
    assert(last != nullptr);
    assert(first->gtPrev == nullptr);
    assert(last->gtNext == nullptr);

    if (insertionPoint == nullptr)
    {
        if (m_lastNode == nullptr)
        {
            m_lastNode = last;
        }
        else
        {
            assert(m_firstNode != nullptr);
            assert(m_firstNode->gtPrev == nullptr);
            m_firstNode->gtPrev = last;
            last->gtNext = m_firstNode;
        }
        m_firstNode = first;
    }
    else
    {
        assert(Contains(insertionPoint));

        last->gtNext = insertionPoint->gtNext;
        if (last->gtNext == nullptr)
        {
            assert(insertionPoint == m_lastNode);
            m_lastNode = last;
        }
        else
        {
            last->gtNext->gtPrev = last;
        }

        first->gtPrev          = insertionPoint;
        insertionPoint->gtNext = first;
    }
}

//------------------------------------------------------------------------
// LIR::Range::InsertBefore: Inserts a range before another node in `this` range.
//
// Arguments:
//    insertionPoint - The node before which the nodes will be inserted. If non-null, must be part
//                     of this range. If null, insert at the end of the range.
//    range - The range to splice in.
//
void LIR::Range::InsertBefore(GenTree* insertionPoint, Range&& range)
{
    assert(!range.IsEmpty());
    FinishInsertBefore(insertionPoint, range.m_firstNode, range.m_lastNode);
}

//------------------------------------------------------------------------
// LIR::Range::InsertAfter: Inserts a range after another node in `this` range.
//
// Arguments:
//    insertionPoint - The node after which the nodes will be inserted. If non-null, must be part
//                     of this range. If null, insert at the beginning of the range.
//    range - The range to splice in.
//
void LIR::Range::InsertAfter(GenTree* insertionPoint, Range&& range)
{
    assert(!range.IsEmpty());
    FinishInsertAfter(insertionPoint, range.m_firstNode, range.m_lastNode);
}

//------------------------------------------------------------------------
// LIR::Range::InsertAtBeginning: Inserts a node at the beginning of this range.
//
// Arguments:
//    node - The node to insert. Must not be part of any range.
//
void LIR::Range::InsertAtBeginning(GenTree* node)
{
    InsertBefore(m_firstNode, node);
}

//------------------------------------------------------------------------
// LIR::Range::InsertAtEnd: Inserts a node at the end of this range.
//
// Arguments:
//    node - The node to insert. Must not be part of any range.
//
void LIR::Range::InsertAtEnd(GenTree* node)
{
    InsertAfter(m_lastNode, node);
}

//------------------------------------------------------------------------
// LIR::Range::InsertAtBeginning: Inserts a range at the beginning of `this` range.
//
// Arguments:
//    range - The range to splice in.
//
void LIR::Range::InsertAtBeginning(Range&& range)
{
    InsertBefore(m_firstNode, std::move(range));
}

//------------------------------------------------------------------------
// LIR::Range::InsertAtEnd: Inserts a range at the end of `this` range.
//
// Arguments:
//    range - The range to splice in.
//
void LIR::Range::InsertAtEnd(Range&& range)
{
    InsertAfter(m_lastNode, std::move(range));
}

//------------------------------------------------------------------------
// LIR::Range::Remove: Removes a node from this range.
//
// Arguments:
//    node - The node to remove. Must be part of this range.
//
void LIR::Range::Remove(GenTree* node)
{
    assert(node != nullptr);
    assert(Contains(node));

    GenTree* prev = node->gtPrev;
    GenTree* next = node->gtNext;

    if (prev != nullptr)
    {
        prev->gtNext = next;
    }
    else
    {
        assert(node == m_firstNode);
        m_firstNode = next;
    }

    if (next != nullptr)
    {
        next->gtPrev = prev;
    }
    else
    {
        assert(node == m_lastNode);
        m_lastNode = prev;
    }

    node->gtPrev = nullptr;
    node->gtNext = nullptr;
}

//------------------------------------------------------------------------
// LIR::Range::Remove: Removes a subrange from this range.
//
// Both the start and the end of the subrange must be part of this range.
//
// Arguments:
//    firstNode - The first node in the subrange.
//    lastNode - The last node in the subrange.
//
// Returns:
//    A mutable range containing the removed nodes.
//
LIR::Range LIR::Range::Remove(GenTree* firstNode, GenTree* lastNode)
{
    assert(firstNode != nullptr);
    assert(lastNode != nullptr);
    assert(Contains(firstNode));
    assert((firstNode == lastNode) || firstNode->Precedes(lastNode));

    GenTree* prev = firstNode->gtPrev;
    GenTree* next = lastNode->gtNext;

    if (prev != nullptr)
    {
        prev->gtNext = next;
    }
    else
    {
        assert(firstNode == m_firstNode);
        m_firstNode = next;
    }

    if (next != nullptr)
    {
        next->gtPrev = prev;
    }
    else
    {
        assert(lastNode == m_lastNode);
        m_lastNode = prev;
    }

    firstNode->gtPrev = nullptr;
    lastNode->gtNext  = nullptr;

    return Range(firstNode, lastNode);
}

//------------------------------------------------------------------------
// LIR::Range::Remove: Removes a subrange from this range.
//
// Arguments:
//    range - The subrange to remove. Must be part of this range.
//
// Returns:
//    A mutable range containing the removed nodes.
//
LIR::Range LIR::Range::Remove(ReadOnlyRange&& range)
{
    return Remove(range.m_firstNode, range.m_lastNode);
}

//------------------------------------------------------------------------
// LIR::Range::Delete: Deletes a node from this range.
//
// Note that the deleted node must not be used after this function has
// been called. If the deleted node is part of a block, this function also
// calls `Compiler::lvaDecRefCnts` as necessary.
//
// Arguments:
//    node - The node to delete. Must be part of this range.
//    block - The block that contains the node, if any. May be null.
//    compiler - The compiler context. May be null if block is null.
//
void LIR::Range::Delete(Compiler* compiler, BasicBlock* block, GenTree* node)
{
    assert(node != nullptr);
    assert((block == nullptr) == (compiler == nullptr));

    Remove(node);

    if (block != nullptr)
    {
        if (((node->OperGet() == GT_CALL) && ((node->gtFlags & GTF_CALL_UNMANAGED) != 0)) ||
            (node->OperIsLocal() && !node->IsPhiNode()))
        {
            compiler->lvaDecRefCnts(block, node);
        }
    }

    DEBUG_DESTROY_NODE(node);
}

//------------------------------------------------------------------------
// LIR::Range::Delete: Deletes a subrange from this range.
//
// Both the start and the end of the subrange must be part of this range.
// Note that the deleted nodes must not be used after this function has
// been called. If the deleted nodes are part of a block, this function
// also calls `Compiler::lvaDecRefCnts` as necessary.
//
// Arguments:
//    firstNode - The first node in the subrange.
//    lastNode - The last node in the subrange.
//    block - The block that contains the subrange, if any. May be null.
//    compiler - The compiler context. May be null if block is null.
//
void LIR::Range::Delete(Compiler* compiler, BasicBlock* block, GenTree* firstNode, GenTree* lastNode)
{
    assert(firstNode != nullptr);
    assert(lastNode != nullptr);
    assert((block == nullptr) == (compiler == nullptr));

    Remove(firstNode, lastNode);

    assert(lastNode->gtNext == nullptr);

    if (block != nullptr)
    {
        for (GenTree* node = firstNode; node != nullptr; node = node->gtNext)
        {
            if (((node->OperGet() == GT_CALL) && ((node->gtFlags & GTF_CALL_UNMANAGED) != 0)) ||
                (node->OperIsLocal() && !node->IsPhiNode()))
            {
                compiler->lvaDecRefCnts(block, node);
            }
        }
    }

#ifdef DEBUG
    // We can't do this in the loop above because it causes `IsPhiNode` to return a false negative
    // for `GT_STORE_LCL_VAR` nodes that participate in phi definitions.
    for (GenTree* node = firstNode; node != nullptr; node = node->gtNext)
    {
        DEBUG_DESTROY_NODE(node);
    }
#endif
}

//------------------------------------------------------------------------
// LIR::Range::Delete: Deletes a subrange from this range.
//
// Both the start and the end of the subrange must be part of this range.
// Note that the deleted nodes must not be used after this function has
// been called. If the deleted nodes are part of a block, this function
// also calls `Compiler::lvaDecRefCnts` as necessary.
//
// Arguments:
//    range - The subrange to delete.
//    block - The block that contains the subrange, if any. May be null.
//    compiler - The compiler context. May be null if block is null.
//
void LIR::Range::Delete(Compiler* compiler, BasicBlock* block, ReadOnlyRange&& range)
{
    Delete(compiler, block, range.m_firstNode, range.m_lastNode);
}


//------------------------------------------------------------------------
// LIR::Range::TryGetUse: Try to find the use for a given node.
//
// Arguments:
//    node - The node for which to find the corresponding use.
//    use (out) - The use of the corresponding node, if any. Invalid if
//                this method returns false.
//
// Return Value: Returns true if a use was found; false otherwise.
//
bool LIR::Range::TryGetUse(GenTree* node, Use* use)
{
    assert(node != nullptr);
    assert(use != nullptr);
    assert(Contains(node));

    // Don't bother looking for uses of nodes that are not values.
    // If the node is the last node, we won't find a use (and we would
    // end up creating an illegal range if we tried).
    if (node->IsValue() && (node != LastNode()))
    {
        for (GenTree* n : ReadOnlyRange(node->gtNext, m_lastNode))
        {
            GenTree** edge;
            if (n->TryGetUse(node, &edge))
            {
                *use = Use(*this, edge, n);
                return true;
            }
        }
    }

    *use = Use();
    return false;
}

//------------------------------------------------------------------------
// LIR::Range::GetTreeRange: Computes the subrange that includes all nodes
//                           in the dataflow trees rooted at a particular
//                           set of nodes.
//
// This method logically uses the following algorithm to compute the
// range:
//
//    worklist = { set }
//    firstNode = start
//    isClosed = true
//
//    while not worklist.isEmpty:
//        if not worklist.contains(firstNode):
//            isClosed = false
//        else:
//            for operand in firstNode:
//                worklist.add(operand)
//
//            worklist.remove(firstNode)
//
//        firstNode = firstNode.previousNode
//
//    return firstNode
//
// Instead of using a set for the worklist, the implementation uses the
// `LIR::Mark` bit of the `GenTree::LIRFlags` field to track whether or
// not a node is in the worklist.
//
// Note also that this algorithm depends LIR nodes being SDSU, SDSU defs
// and uses occurring in the same block, and correct dataflow (i.e. defs
// occurring before uses).
//
// Arguments:
//    root        - The root of the dataflow tree.
//    isClosed    - An output parameter that is set to true if the returned
//                  range contains only nodes in the dataflow tree and false
//                  otherwise.
//
// Returns:
//    The computed subrange.
//
LIR::ReadOnlyRange LIR::Range::GetMarkedRange(unsigned  markCount,
                                              GenTree*  start,
                                              bool*     isClosed,
                                              unsigned* sideEffects) const
{
    assert(markCount != 0);
    assert(start != nullptr);
    assert(isClosed != nullptr);
    assert(sideEffects != nullptr);

    bool     sawUnmarkedNode    = false;
    unsigned sideEffectsInRange = 0;

    GenTree* firstNode = start;
    GenTree* lastNode  = nullptr;
    for (;;)
    {
        if ((firstNode->gtLIRFlags & LIR::Flags::Mark) != 0)
        {
            if (lastNode == nullptr)
            {
                lastNode = firstNode;
            }

            // Mark the node's operands
            for (GenTree* operand : firstNode->Operands())
            {
                // Do not mark nodes that do not appear in the execution order
                assert(operand->OperGet() != GT_LIST);
                if (operand->OperGet() == GT_ARGPLACE)
                {
                    continue;
                }

                operand->gtLIRFlags |= LIR::Flags::Mark;
                markCount++;
            }

            // Unmark the the node and update `firstNode`
            firstNode->gtLIRFlags &= ~LIR::Flags::Mark;
            markCount--;
        }
        else if (lastNode != nullptr)
        {
            sawUnmarkedNode = true;
        }

        if (lastNode != nullptr)
        {
            sideEffectsInRange |= (firstNode->gtFlags & GTF_ALL_EFFECT);
        }

        if (markCount == 0)
        {
            break;
        }

        firstNode = firstNode->gtPrev;

        // This assert will fail if the dataflow that feeds the root node
        // is incorrect in that it crosses a block boundary or if it involves
        // a use that occurs before its corresponding def.
        assert(firstNode != nullptr);
    }

    assert(lastNode != nullptr);

    *isClosed    = !sawUnmarkedNode;
    *sideEffects = sideEffectsInRange;
    return ReadOnlyRange(firstNode, lastNode);
}

//------------------------------------------------------------------------
// LIR::Range::GetTreeRange: Computes the subrange that includes all nodes
//                           in the dataflow tree rooted at a particular
//                           node.
//
// Arguments:
//    root        - The root of the dataflow tree.
//    isClosed    - An output parameter that is set to true if the returned
//                  range contains only nodes in the dataflow tree and false
//                  otherwise.
//
// Returns:
//    The computed subrange.
LIR::ReadOnlyRange LIR::Range::GetTreeRange(GenTree* root, bool* isClosed) const
{
    unsigned unused;
    return GetTreeRange(root, isClosed, &unused);
}

//------------------------------------------------------------------------
// LIR::Range::GetTreeRange: Computes the subrange that includes all nodes
//                           in the dataflow tree rooted at a particular
//                           node.
//
// Arguments:
//    root        - The root of the dataflow tree.
//    isClosed    - An output parameter that is set to true if the returned
//                  range contains only nodes in the dataflow tree and false
//                  otherwise.
//    sideEffects - An output parameter that summarizes the side effects
//                  contained in the returned range.
//
// Returns:
//    The computed subrange.
LIR::ReadOnlyRange LIR::Range::GetTreeRange(GenTree* root, bool* isClosed, unsigned* sideEffects) const
{
    assert(root != nullptr);

    // Mark the root of the tree
    const unsigned markCount = 1;
    root->gtLIRFlags |= LIR::Flags::Mark;

    return GetMarkedRange(markCount, root, isClosed, sideEffects);
}

//------------------------------------------------------------------------
// LIR::Range::GetTreeRange: Computes the subrange that includes all nodes
//                           in the dataflow trees rooted by the operands
//                           to a particular node.
//
// Arguments:
//    root        - The root of the dataflow tree.
//    isClosed    - An output parameter that is set to true if the returned
//                  range contains only nodes in the dataflow tree and false
//                  otherwise.
//    sideEffects - An output parameter that summarizes the side effects
//                  contained in the returned range.
//
// Returns:
//    The computed subrange.
//
LIR::ReadOnlyRange LIR::Range::GetRangeOfOperandTrees(GenTree* root, bool* isClosed, unsigned* sideEffects) const
{
    assert(root != nullptr);
    assert(isClosed != nullptr);
    assert(sideEffects != nullptr);

    // Mark the root node's operands
    unsigned markCount = 0;
    for (GenTree* operand : root->Operands())
    {
        operand->gtLIRFlags |= LIR::Flags::Mark;
        markCount++;
    }

    if (markCount == 0)
    {
        *isClosed    = true;
        *sideEffects = 0;
        return ReadOnlyRange();
    }

    return GetMarkedRange(markCount, root, isClosed, sideEffects);
}

#ifdef DEBUG

//------------------------------------------------------------------------
// LIR::Range::CheckLIR: Performs a set of correctness checks on the LIR
//                       contained in this range.
//
// This method checks the following properties:
// - Defs are singly-used
// - Uses follow defs
// - Uses are correctly linked into the block
// - Nodes that do not produce values are not used
// - Only LIR nodes are present in the block
// - If any phi nodes are present in the range, they precede all other
//   nodes
//
// The first four properties are verified by walking the range's LIR in execution order,
// inserting defs into a set as they are visited, and removing them as they are used. The
// different cases are distinguished only when an error is detected.
//
// Arguments:
//    compiler - A compiler context.
//
// Return Value:
//    'true' if the LIR for the specified range is legal.
//
bool LIR::Range::CheckLIR(Compiler* compiler, bool checkUnusedValues) const
{
    if (IsEmpty())
    {
        // Nothing more to check.
        return true;
    }

    // Check the gtNext/gtPrev links: (1) ensure there are no circularities, (2) ensure the gtPrev list is
    // precisely the inverse of the gtNext list.
    //
    // To detect circularity, use the "tortoise and hare" 2-pointer algorithm.

    GenTree* slowNode = FirstNode();
    assert(slowNode != nullptr); // because it's a non-empty range
    GenTree* fastNode1    = nullptr;
    GenTree* fastNode2    = slowNode;
    GenTree* prevSlowNode = nullptr;
    while (((fastNode1 = fastNode2->gtNext) != nullptr) && ((fastNode2 = fastNode1->gtNext) != nullptr))
    {
        if ((slowNode == fastNode1) || (slowNode == fastNode2))
        {
            assert(!"gtNext nodes have a circularity!");
        }
        assert(slowNode->gtPrev == prevSlowNode);
        prevSlowNode = slowNode;
        slowNode     = slowNode->gtNext;
        assert(slowNode != nullptr); // the fastNodes would have gone null first.
    }
    // If we get here, the list had no circularities, so either fastNode1 or fastNode2 must be nullptr.
    assert((fastNode1 == nullptr) || (fastNode2 == nullptr));

    // Need to check the rest of the gtPrev links.
    while (slowNode != nullptr)
    {
        assert(slowNode->gtPrev == prevSlowNode);
        prevSlowNode = slowNode;
        slowNode     = slowNode->gtNext;
    }

    SmallHashTable<GenTree*, bool, 32> unusedDefs(compiler);

    bool     pastPhis = false;
    GenTree* prev     = nullptr;
    for (Iterator node = begin(), end = this->end(); node != end; prev = *node, ++node)
    {
        // Verify that the node is allowed in LIR.
        assert(node->IsLIR());

        // TODO: validate catch arg stores

        // Check that all phi nodes (if any) occur at the start of the range.
        if ((node->OperGet() == GT_PHI_ARG) || (node->OperGet() == GT_PHI) || node->IsPhiDefn())
        {
            assert(!pastPhis);
        }
        else
        {
            pastPhis = true;
        }

        for (GenTree** useEdge : node->UseEdges())
        {
            GenTree* def = *useEdge;

            assert((!checkUnusedValues || ((def->gtLIRFlags & LIR::Flags::IsUnusedValue) == 0)) &&
                   "operands should never be marked as unused values");

            if (def->OperGet() == GT_ARGPLACE)
            {
                // ARGPLACE nodes are not represented in the LIR sequence. Ignore them.
                continue;
            }
            else if (!def->IsValue())
            {
                // Calls may contain "uses" of nodes that do not produce a value. This is an artifact of
                // the HIR and should probably be fixed, but doing so is an unknown amount of work.
                assert(node->OperGet() == GT_CALL);
                continue;
            }

            bool v;
            bool foundDef = unusedDefs.TryRemove(def, &v);
            if (!foundDef)
            {
                // First, scan backwards and look for a preceding use.
                for (GenTree* prev = *node; prev != nullptr; prev = prev->gtPrev)
                {
                    // TODO: dump the users and the def
                    GenTree** earlierUseEdge;
                    bool      foundEarlierUse = prev->TryGetUse(def, &earlierUseEdge) && earlierUseEdge != useEdge;
                    assert(!foundEarlierUse && "found multiply-used LIR node");
                }

                // The def did not precede the use. Check to see if it exists in the block at all.
                for (GenTree* next = node->gtNext; next != nullptr; next = next->gtNext)
                {
                    // TODO: dump the user and the def
                    assert(next != def && "found def after use");
                }

                // The def might not be a node that produces a value.
                assert(def->IsValue() && "found use of a node that does not produce a value");

                // By this point, the only possibility is that the def is not threaded into the LIR sequence.
                assert(false && "found use of a node that is not in the LIR sequence");
            }
        }

        if (node->IsValue())
        {
            bool added = unusedDefs.AddOrUpdate(*node, true);
            assert(added);
        }
    }

    assert(prev == m_lastNode);

    // At this point the unusedDefs map should contain only unused values.
    if (checkUnusedValues)
    {
        for (auto kvp : unusedDefs)
        {
            GenTree* node = kvp.Key();
            assert(((node->gtLIRFlags & LIR::Flags::IsUnusedValue) != 0) && "found an unmarked unused value");
        }
    }

    return true;
}

#endif // DEBUG

//------------------------------------------------------------------------
// LIR::AsRange: Returns an LIR view of the given basic block.
//
LIR::Range& LIR::AsRange(BasicBlock* block)
{
    return *static_cast<Range*>(block);
}

//------------------------------------------------------------------------
// LIR::EmptyRange: Constructs and returns an empty range.
//
// static
LIR::Range LIR::EmptyRange()
{
    return Range(nullptr, nullptr);
}

//------------------------------------------------------------------------
// LIR::SeqTree: Given a newly created, unsequenced HIR tree, set the evaluation
// order (call gtSetEvalOrder) and sequence the tree (set gtNext/gtPrev pointers
// by calling fgSetTreeSeq), and return a Range representing the list of nodes.
// It is expected this will later be spliced into the LIR graph.
//
// Arguments:
//    compiler - The Compiler context.
//    tree - The tree to sequence.
//
// Return Value: The newly constructed range.
//
// static
LIR::Range LIR::SeqTree(Compiler* compiler, GenTree* tree)
{
    // TODO-LIR: it would be great to assert that the tree has not already been
    // threaded into an order, but I'm not sure that will be practical at this
    // point.

    compiler->gtSetEvalOrder(tree);
    return Range(compiler->fgSetTreeSeq(tree, nullptr, true), tree);
}
