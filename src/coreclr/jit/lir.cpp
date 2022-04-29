// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#include "smallhash.h"
#include "sideeffects.h"

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
// LIR::Use::MakeDummyUse: Make a use into a dummy use.
//
// This method is provided as a convenience to allow transforms to work
// uniformly over Use values. It allows the creation of a Use given a node
// that is not used.
//
// Arguments:
//    range - The range that contains the node.
//    node - The node for which to create a dummy use.
//    dummyUse - [out] the resulting dummy use
//
void LIR::Use::MakeDummyUse(Range& range, GenTree* node, LIR::Use* dummyUse)
{
    assert(node != nullptr);

    dummyUse->m_range = &range;
    dummyUse->m_user  = node;
    dummyUse->m_edge  = &dummyUse->m_user;

    assert(dummyUse->IsInitialized());
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
//    opEq.ReplaceWith(constantOne);
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
// Eliminating the now-dead compare and its operands using `LIR::Range::Remove`
// would then give us:
//
//    t18 =    const     int    1
//
//          /--*  t18 int
//          *  jmpTrue   void
//
// Arguments:
//    replacement - The replacement node.
//
void LIR::Use::ReplaceWith(GenTree* replacement)
{
    assert(IsInitialized());
    assert(replacement != nullptr);
    assert(IsDummyUse() || m_range->Contains(m_user));
    assert(m_range->Contains(replacement));

    if (!IsDummyUse())
    {
        m_user->ReplaceOperand(m_edge, replacement);
    }
    else
    {
        *m_edge = replacement;
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
//    lclNum - The local to use for temporary storage. If BAD_VAR_NUM (the
//             default) is provided, this method will create and use a new
//             local var.
//    assign - On return, if non null, contains the created assignment node
//
// Return Value: The number of the local var used for temporary storage.
//
unsigned LIR::Use::ReplaceWithLclVar(Compiler* compiler, unsigned lclNum, GenTree** assign)
{
    assert(IsInitialized());
    assert(compiler != nullptr);
    assert(m_range->Contains(m_user));
    assert(m_range->Contains(*m_edge));

    GenTree* const node = *m_edge;

    if (lclNum == BAD_VAR_NUM)
    {
        lclNum = compiler->lvaGrabTemp(true DEBUGARG("ReplaceWithLclVar is creating a new local variable"));
    }

    GenTreeLclVar* const store = compiler->gtNewTempAssign(lclNum, node)->AsLclVar();
    assert(store != nullptr);
    assert(store->gtOp1 == node);

    GenTree* const load =
        new (compiler, GT_LCL_VAR) GenTreeLclVar(GT_LCL_VAR, store->TypeGet(), store->AsLclVarCommon()->GetLclNum());

    m_range->InsertAfter(node, store, load);

    ReplaceWith(load);

    JITDUMP("ReplaceWithLclVar created store :\n");
    DISPNODE(store);

    if (assign != nullptr)
    {
        *assign = store;
    }
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
// LIR::Range::FirstNonCatchArgNode: Returns the first node after all catch arg nodes in this range.
//
GenTree* LIR::Range::FirstNonCatchArgNode() const
{
    for (GenTree* node : *this)
    {
        if (node->OperIs(GT_CATCH_ARG))
        {
            continue;
        }
        else if ((node->OperIs(GT_STORE_LCL_VAR)) && (node->gtGetOp1()->OperIs(GT_CATCH_ARG)))
        {
            continue;
        }

        return node;
    }

    return nullptr;
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
            first->gtPrev      = m_lastNode;
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
            last->gtNext        = m_firstNode;
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
//    markOperandsUnused - If true, marks the node's operands as unused.
//
void LIR::Range::Remove(GenTree* node, bool markOperandsUnused)
{
    assert(node != nullptr);
    assert(Contains(node));

    if (markOperandsUnused)
    {
        node->VisitOperands([](GenTree* operand) -> GenTree::VisitResult {
            // The operand of JTRUE does not produce a value (just sets the flags).
            if (operand->IsValue())
            {
                operand->SetUnusedValue();
            }
            return GenTree::VisitResult::Continue;
        });
    }

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
// been called.
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
    DEBUG_DESTROY_NODE(node);
}

//------------------------------------------------------------------------
// LIR::Range::Delete: Deletes a subrange from this range.
//
// Both the start and the end of the subrange must be part of this range.
// Note that the deleted nodes must not be used after this function has
// been called.
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
// been called.
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
    if (node->IsValue() && !node->IsUnusedValue() && (node != LastNode()))
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
            firstNode->VisitOperands([&markCount](GenTree* operand) -> GenTree::VisitResult {
                operand->gtLIRFlags |= LIR::Flags::Mark;
                markCount++;
                return GenTree::VisitResult::Continue;
            });

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
    root->VisitOperands([&markCount](GenTree* operand) -> GenTree::VisitResult {
        operand->gtLIRFlags |= LIR::Flags::Mark;
        markCount++;
        return GenTree::VisitResult::Continue;
    });

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
// CheckLclVarSemanticsHelper checks lclVar semantics.
//
// Specifically, ensure that an unaliasable lclVar is not redefined between the
// point at which a use appears in linear order and the point at which it is used by its user.
// This ensures that it is always safe to treat a lclVar use as happening at the user (rather than at
// the lclVar node).
class CheckLclVarSemanticsHelper
{
public:
    //------------------------------------------------------------------------
    // CheckLclVarSemanticsHelper constructor: Init arguments for the helper.
    //
    // This needs unusedDefs because unused lclVar reads may otherwise appear as outstanding reads
    // and produce false indications that a write to a lclVar occurs while outstanding reads of that lclVar
    // exist.
    //
    // Arguments:
    //    compiler - A compiler context.
    //    range - a range to do the check.
    //    unusedDefs - map of defs that do no have users.
    //
    CheckLclVarSemanticsHelper(Compiler*         compiler,
                               const LIR::Range* range,
                               SmallHashTable<GenTree*, bool, 32U>& unusedDefs)
        : compiler(compiler)
        , range(range)
        , unusedDefs(unusedDefs)
        , unusedLclVarReads(compiler->getAllocator(CMK_DebugOnly))
    {
    }

    //------------------------------------------------------------------------
    // Check: do the check.
    // Return Value:
    //    'true' if the Local variables semantics for the specified range is legal.
    bool Check()
    {
        for (GenTree* node : *range)
        {
            if (!node->isContained()) // a contained node reads operands in the parent.
            {
                UseNodeOperands(node);
            }

            AliasSet::NodeInfo nodeInfo(compiler, node);
            if (nodeInfo.IsLclVarRead() && node->IsValue() && !unusedDefs.Contains(node))
            {
                jitstd::list<GenTree*>* reads;
                if (!unusedLclVarReads.TryGetValue(nodeInfo.LclNum(), &reads))
                {
                    reads = new (compiler, CMK_DebugOnly) jitstd::list<GenTree*>(compiler->getAllocator(CMK_DebugOnly));
                    unusedLclVarReads.AddOrUpdate(nodeInfo.LclNum(), reads);
                }

                reads->push_back(node);
            }

            if (nodeInfo.IsLclVarWrite())
            {
                // If this node is a lclVar write, it must be not alias a lclVar with an outstanding read
                jitstd::list<GenTree*>* reads;
                if (unusedLclVarReads.TryGetValue(nodeInfo.LclNum(), &reads))
                {
                    for (GenTree* read : *reads)
                    {
                        AliasSet::NodeInfo readInfo(compiler, read);
                        assert(readInfo.IsLclVarRead() && readInfo.LclNum() == nodeInfo.LclNum());
                        unsigned readStart  = readInfo.LclOffs();
                        unsigned readEnd    = readStart + genTypeSize(read->TypeGet());
                        unsigned writeStart = nodeInfo.LclOffs();
                        unsigned writeEnd   = writeStart + genTypeSize(node->TypeGet());
                        if ((readEnd > writeStart) && (writeEnd > readStart))
                        {
                            JITDUMP("Write to local overlaps outstanding read (write: %u..%u, read: %u..%u)\n",
                                    writeStart, writeEnd, readStart, readEnd);

                            LIR::Use use;
                            bool     found = const_cast<LIR::Range*>(range)->TryGetUse(read, &use);
                            GenTree* user  = found ? use.User() : nullptr;

                            for (GenTree* rangeNode : *range)
                            {
                                const char* prefix = nullptr;
                                if (rangeNode == read)
                                {
                                    prefix = "read:  ";
                                }
                                else if (rangeNode == node)
                                {
                                    prefix = "write: ";
                                }
                                else if (rangeNode == user)
                                {
                                    prefix = "user:  ";
                                }
                                else
                                {
                                    prefix = "       ";
                                }

                                compiler->gtDispLIRNode(rangeNode, prefix);
                            }

                            assert(!"Write to unaliased local overlaps outstanding read");
                            break;
                        }
                    }
                }
            }
        }

        return true;
    }

private:
    //------------------------------------------------------------------------
    // UseNodeOperands: mark the node's operands as used.
    //
    // Arguments:
    //    node - the node to use operands from.
    void UseNodeOperands(GenTree* node)
    {
        for (GenTree* operand : node->Operands())
        {
            if (operand->isContained())
            {
                UseNodeOperands(operand);
            }
            AliasSet::NodeInfo operandInfo(compiler, operand);
            if (operandInfo.IsLclVarRead())
            {
                jitstd::list<GenTree*>* reads;
                const bool              foundList = unusedLclVarReads.TryGetValue(operandInfo.LclNum(), &reads);
                assert(foundList);

                bool found = false;
                for (jitstd::list<GenTree*>::iterator it = reads->begin(); it != reads->end(); ++it)
                {
                    if (*it == operand)
                    {
                        reads->erase(it);
                        found = true;
                        break;
                    }
                }

                assert(found || !"Could not find consumed local in unusedLclVarReads");
            }
        }
    }

private:
    Compiler*         compiler;
    const LIR::Range* range;
    SmallHashTable<GenTree*, bool, 32U>&              unusedDefs;
    SmallHashTable<int, jitstd::list<GenTree*>*, 16U> unusedLclVarReads;
};

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

    SmallHashTable<GenTree*, bool, 32> unusedDefs(compiler->getAllocatorDebugOnly());

    GenTree* prev = nullptr;
    for (Iterator node = begin(), end = this->end(); node != end; prev = *node, ++node)
    {
        // Verify that the node is allowed in LIR.
        assert(node->OperIsLIR());

        // Some nodes should never be marked unused, as they must be contained in the backend.
        // These may be marked as unused during dead code elimination traversal, but they *must* be subsequently
        // removed.
        assert(!node->IsUnusedValue() || !node->OperIs(GT_FIELD_LIST, GT_INIT_VAL));

        // Verify that the REVERSE_OPS flag is not set. NOTE: if we ever decide to reuse the bit assigned to
        // GTF_REVERSE_OPS for an LIR-only flag we will need to move this check to the points at which we
        // insert nodes into an LIR range.
        assert((node->gtFlags & GTF_REVERSE_OPS) == 0);

        // TODO: validate catch arg stores

        for (GenTree** useEdge : node->UseEdges())
        {
            GenTree* def = *useEdge;

            assert(!(checkUnusedValues && def->IsUnusedValue()) && "operands should never be marked as unused values");

            if (!def->IsValue())
            {
                // Stack arguments do not produce a value, but they are considered children of the call.
                // It may be useful to remove these from being call operands, but that may also impact
                // other code that relies on being able to reach all the operands from a call node.
                // The argument of a JTRUE doesn't produce a value (just sets a flag).
                assert(((node->OperGet() == GT_CALL) && def->OperIs(GT_PUTARG_STK)) ||
                       ((node->OperGet() == GT_JTRUE) && (def->TypeGet() == TYP_VOID) &&
                        ((def->gtFlags & GTF_SET_FLAGS) != 0)));
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
            assert(node->IsUnusedValue() && "found an unmarked unused value");
            assert(!node->isContained() && "a contained node should have a user");
        }
    }

    CheckLclVarSemanticsHelper checkLclVarSemanticsHelper(compiler, this, unusedDefs);
    assert(checkLclVarSemanticsHelper.Check());

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

const LIR::Range& LIR::AsRange(const BasicBlock* block)
{
    return *static_cast<const Range*>(block);
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
// LIR::SeqTree:
//    Given a newly created, unsequenced HIR tree, set the evaluation
//    order (call gtSetEvalOrder) and sequence the tree (set gtNext/gtPrev
//    pointers by calling fgSetTreeSeq), and return a Range representing
//    the list of nodes. It is expected this will later be spliced into
//    an LIR range.
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
    return Range(compiler->fgSetTreeSeq(tree, /* isLIR */ true), tree);
}

//------------------------------------------------------------------------
// LIR::InsertBeforeTerminator:
//    Insert an LIR range before the terminating instruction in the given
//    basic block. If the basic block has no terminating instruction (i.e.
//    it has a jump kind that is not `BBJ_RETURN`, `BBJ_COND`, or
//    `BBJ_SWITCH`), the range is inserted at the end of the block.
//
// Arguments:
//    block - The block in which to insert the range.
//    range - The range to insert.
//
void LIR::InsertBeforeTerminator(BasicBlock* block, LIR::Range&& range)
{
    LIR::Range& blockRange = LIR::AsRange(block);

    GenTree* insertionPoint = nullptr;
    if (block->KindIs(BBJ_COND, BBJ_SWITCH, BBJ_RETURN))
    {
        insertionPoint = blockRange.LastNode();
        assert(insertionPoint != nullptr);

#if DEBUG
        switch (block->bbJumpKind)
        {
            case BBJ_COND:
                assert(insertionPoint->OperIsConditionalJump());
                break;

            case BBJ_SWITCH:
                assert((insertionPoint->OperGet() == GT_SWITCH) || (insertionPoint->OperGet() == GT_SWITCH_TABLE));
                break;

            case BBJ_RETURN:
                assert((insertionPoint->OperGet() == GT_RETURN) || (insertionPoint->OperGet() == GT_JMP) ||
                       (insertionPoint->OperGet() == GT_CALL));
                break;

            default:
                unreached();
        }
#endif
    }

    blockRange.InsertBefore(insertionPoint, std::move(range));
}

#ifdef DEBUG
void GenTree::dumpLIRFlags()
{
    JITDUMP("[%c%c]", IsUnusedValue() ? 'U' : '-', IsRegOptional() ? 'O' : '-');
}
#endif
