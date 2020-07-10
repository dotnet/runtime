// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"

#include "rangetree.h"

#ifndef DACCESS_COMPILE

void RangeTree::Node::Init(SIZE_T rangeStart, SIZE_T rangeEnd
                           DEBUGARG(DWORD ord))
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    _ASSERTE(rangeEnd >= rangeStart);

    start = rangeStart;
    end   = rangeEnd;

    mask = GetRangeCommonMask(start, end);

    IsIntermediate(FALSE);
#ifdef _DEBUG
    ordinal = ord;
#endif

    children[0] = NULL;
    children[1] = NULL;
}


RangeTree::RangeTree() :
    m_root(NULL), m_pool(sizeof(Node), 16, 16)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

#ifdef _DEBUG
    m_nodeCount = 0;
#endif
}

RangeTree::Node *RangeTree::Lookup(SIZE_T address) const
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    Node *node = m_root;

    //
    // When comparing an address to a node,
    // there are 5 possibilities:
    // * the node is null - no match
    // * the address doesn't contain the prefix m - no match
    // * the address is inside the node's range (and necessarily
    //   contains the prefix m) - match
    // * the address is less than the range (and necessarily
    //   has the prefix m0) - traverse the zero child
    // * the address is greater than the range (and necessarily
    //   has the prefix m1) - traverse the one child
    //

    while (node != NULL
           && (address < node->start || address >= node->end))
    {
        //
        // See if the address has prefix m.
        //

        if ((address & node->mask) != (node->start & node->mask))
            return NULL;

        //
        // Determine which subnode to look in.
        //

        node = *node->Child(address);
    }

    if (node != NULL && node->IsIntermediate())
        node = NULL;

    return node;
}

RangeTree::Node *RangeTree::LookupEndInclusive(SIZE_T address)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    //
    // Lookup an address which may be the ending range
    // of a node.  In order for this to make sense, it
    // must be the case that address is never the starting
    // address of the node.  (Otherwise there is an
    // ambiguity when 2 nodes are adjacent.)
    //

    Node *result = Lookup(address-1);

    if ((result != NULL) && (address >= result->start)
        && (address <= result->end))
        return result;
    else
        return NULL;
}

HRESULT RangeTree::AddNode(Node *addNode, SIZE_T start, SIZE_T end)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    } CONTRACTL_END;

    addNode->Init(start, end DEBUGARG(++m_nodeCount));

    Node **nodePtr = &m_root;

    while (TRUE)
    {
        Node *node = *nodePtr;

        //
        // See if we can live here
        //

        if (node == NULL)
        {
            *nodePtr = addNode;
            return S_OK;
        }

        //
        // Decide if we are a child of the
        // current node, or it is a child
        // of us, or neither.
        //

        SIZE_T diffBits = start ^ node->start;

        // See if the nodes are disjoint
        if (diffBits & (node->mask & addNode->mask))
        {
            // We need to construct a intermediate node to be the parent of these two.

            // AddIntermediateNode throws to indicate OOM. We need to either
            // propagate this behavior upward or convert the exception here.
            CONTRACT_VIOLATION(ThrowsViolation);
            *nodePtr = AddIntermediateNode(node, addNode);
            // <TODO> data structure is hosed at this point if we get an exception - should
            // we undo the operation?</TODO>

            return S_OK;
        }
        else
        {
            SIZE_T maskDiff = node->mask ^ addNode->mask;

            if (maskDiff == 0)
            {
                // Masks are the same size, ranges overlap.
                // This must be an intermediate node or we have a problem.
                if (!node->IsIntermediate())
                    return E_INVALIDARG;

                // Replace the intermediate node with this one.
                addNode->children[0] = node->children[0];
                addNode->children[1] = node->children[1];
                *nodePtr = addNode;
                FreeIntermediate(node);

                return S_OK;
            }

            // Make sure the range doesn't intersect.
            if (end > node->start && start < node->end)
                return E_INVALIDARG;

            else if (addNode->mask & maskDiff)
            {
                // added node's mask is bigger - it should be the child
                nodePtr = node->Child(start);
            }
            else
            {
                // existing node's mask is bigger - it should be the child
                *nodePtr = addNode;
                nodePtr = addNode->Child(node->start);
                addNode = node;
            }
        }
    }
}

HRESULT RangeTree::RemoveNode(Node *removeNode)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    Node **nodePtr = &m_root;

    while (TRUE)
    {
        Node *node = *nodePtr;

        _ASSERTE(node != NULL);

        if (node == removeNode)
        {
            if (node->children[0] == NULL)
                *nodePtr = node->children[1];
            else if (node->children[1] == NULL)
                *nodePtr = node->children[0];
            else
            {
                *nodePtr = AddIntermediateNode(node->children[0],
                                               node->children[1]);
            }

            return S_OK;
        }
        else if (node->IsIntermediate())
        {
            if (node->children[0] == removeNode)
            {
				if (removeNode->children[0] == NULL && removeNode->children[1] == NULL)
				{
					*nodePtr = node->children[1];
	                FreeIntermediate(node);
					return S_OK;
				}
            }
            else if (node->children[1] == removeNode)
            {
				if (removeNode->children[0] == NULL && removeNode->children[1] == NULL)
				{
					*nodePtr = node->children[0];
					FreeIntermediate(node);
					return S_OK;
				}
            }
        }

        nodePtr = node->Child(removeNode->start);
    }
}

void RangeTree::Iterate(IterationCallback pCallback, void *context)
{
    WRAPPER_NO_CONTRACT;

    if (m_root != NULL)
        IterateNode(m_root, pCallback, context);
}

void RangeTree::IterateNode(Node *node, IterationCallback pCallback, void *context)
{
    WRAPPER_NO_CONTRACT;

    if (node->children[0] != NULL)
        IterateNode(node->children[0], pCallback, context);

    if (!node->IsIntermediate())
        pCallback(node, context);

    if (node->children[1] != NULL)
        IterateNode(node->children[1], pCallback, context);
}

void RangeTree::IterateRange(SIZE_T start, SIZE_T end, IterationCallback pCallback, void *context)
{
    WRAPPER_NO_CONTRACT;

    if (m_root != NULL)
        IterateRangeNode(m_root, start, end, GetRangeCommonMask(start, end),
                         pCallback, context);
}

void RangeTree::IterateRangeNode(Node *node, SIZE_T start, SIZE_T end,
                                 SIZE_T mask, IterationCallback pCallback, void *context)
{
    WRAPPER_NO_CONTRACT;

    // Compute which bits are different between the two start ranges
    SIZE_T diffBits = start ^ node->start;

    // See if the nodes are disjoint
    if (diffBits & (node->mask & mask))
    {
        return;
    }
    else
    {
        if (node->children[0] != NULL)
            IterateRangeNode(node->children[0], start, end, mask, pCallback, context);

        if (!node->IsIntermediate()
            && (end > node->start && start < node->end))
            (*pCallback)(node, context);

        if (node->children[1] != NULL)
            IterateRangeNode(node->children[1], start, end, mask, pCallback, context);
    }
}


BOOL RangeTree::Overlaps(SIZE_T start, SIZE_T end)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    if (m_root != NULL)
        return OverlapsNode(m_root, start, end, GetRangeCommonMask(start, end));
    else
        return FALSE;
}

BOOL RangeTree::OverlapsNode(Node *node, SIZE_T start, SIZE_T end, SIZE_T mask)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    //
    // Decide if we are a child of the
    // current node, or it is a child
    // of us, or neither.
    //

    SIZE_T diffBits = start ^ node->start;

    // See if the nodes are disjoint
    if (diffBits & (node->mask & mask))
    {
        return FALSE;
    }
    else
    {
        if (!node->IsIntermediate()
            && (end > node->start && start < node->end))
            return TRUE;

        if ((node->children[0] != NULL && OverlapsNode(node->children[0], start, end, mask))
            || (node->children[1] != NULL && OverlapsNode(node->children[1], start, end, mask)))
            return TRUE;

        return FALSE;
    }
}


void RangeTree::RemoveRange(SIZE_T start, SIZE_T end)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    if (m_root != NULL)
        RemoveRangeNode(&m_root, start, end, GetRangeCommonMask(start, end));
}


void RangeTree::RemoveRangeNode(Node **nodePtr, SIZE_T start, SIZE_T end, SIZE_T mask)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    Node *node = *nodePtr;

    // Compute which bits are different between the two start ranges
    SIZE_T diffBits = start ^ node->start;

    // See if the nodes are disjoint
    if (diffBits & (node->mask & mask))
    {
        // do nothing
    }
    else
    {
        // First, remove from children
        if (node->children[0] != NULL)
            RemoveRangeNode(&node->children[0], start, end, mask);
        if (node->children[1] != NULL)
            RemoveRangeNode(&node->children[1], start, end, mask);

        // Now, remove this node if necessary.
        if (node->IsIntermediate())
        {
            if (node->children[0] == NULL)
            {
                *nodePtr = node->children[0];
                FreeIntermediate(node);
            }
            else if (node->children[1] == NULL)
            {
                *nodePtr = node->children[1];
                FreeIntermediate(node);
            }
        }
        else if (end > node->start && start < node->end)
        {
            if (node->children[0] == NULL)
                *nodePtr = node->children[1];
            else if (node->children[1] == NULL)
                *nodePtr = node->children[0];
            else
            {

                // AddIntermediateNode throws to indicate OOM. We need to either
                // propagate this behavior upward or convert the exception here.
                CONTRACT_VIOLATION(ThrowsViolation);
                *nodePtr = AddIntermediateNode(node->children[0],
                                               node->children[1]);
            }
        }
    }
}


SIZE_T RangeTree::GetRangeCommonMask(SIZE_T start, SIZE_T end)
{
    LIMITED_METHOD_CONTRACT;

    // Compute which bits are the different

    SIZE_T diff = start ^ end;

    // Fill up bits to the right until all are set

    diff |= (diff>>1);
    diff |= (diff>>2);
    diff |= (diff>>4);
    diff |= (diff>>8);
    diff |= (diff>>16);

#if (POINTER_BITS > 32)
    diff |= (diff>>32);
#endif

    // flip bits to form high mask
    return ~diff;
}

RangeTree::Node *RangeTree::AddIntermediateNode(Node *node0,
                                                Node *node1)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    SIZE_T mask = GetRangeCommonMask(node0->start,
                                     node1->start);

    _ASSERTE((mask & ~node0->mask) == 0);
    _ASSERTE((mask & ~node1->mask) == 0);
    _ASSERTE((node0->start & mask) == (node1->start & mask));
    _ASSERTE((node0->start & mask) == (node1->start & mask));

    SIZE_T middle = (node0->start & mask) + (~mask>>1);

    Node *intermediate = AllocateIntermediate();

    intermediate->start = middle;
    intermediate->end   = middle+1;
    intermediate->mask  = mask;
    intermediate->IsIntermediate(TRUE);
#ifdef _DEBUG
    intermediate->ordinal = ++m_nodeCount;
#endif

    int less = (node0->start < node1->start);

    intermediate->children[!less] = node0;
    intermediate->children[less] = node1;

    return intermediate;
}

RangeTree::Node *RangeTree::AllocateIntermediate()
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    return (RangeTree::Node *) m_pool.AllocateElement();
}

void RangeTree::FreeIntermediate(Node *node)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    m_pool.FreeElement(node);
}

#endif
