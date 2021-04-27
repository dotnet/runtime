// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// ZapInnerPtr.h
//

//
// ZapNode that points into middle of other ZapNode
//
// ======================================================================================

#include "common.h"

#include "zapinnerptr.h"

ZapNode * ZapInnerPtrTable::Get(ZapNode * pBase, SSIZE_T offset)
{
    // Nothing to do if the offset is zero
    if (offset == 0)
        return pBase;

    // Flatten the hiearchy of inner ptrs. Inner ptrs pointing to other inner ptrs
    // would not work well during the Resolve phase
    while (pBase->GetType() == ZapNodeType_InnerPtr)
    {
        ZapInnerPtr * pWrapper = (ZapInnerPtr *)pBase;

        offset += pWrapper->GetOffset();
        pBase = pWrapper->GetBase();
    }

    ZapInnerPtr * pInnerPtr = m_entries.Lookup(InnerPtrKey(pBase, offset));

    if (pInnerPtr != NULL)
        return pInnerPtr;

    switch (offset)
    {
    case 1:
        pInnerPtr = new (m_pWriter->GetHeap()) InnerPtrConst<1>(pBase);
        break;
    case 2:
        pInnerPtr = new (m_pWriter->GetHeap()) InnerPtrConst<2>(pBase);
        break;
    case 4:
        pInnerPtr = new (m_pWriter->GetHeap()) InnerPtrConst<4>(pBase);
        break;
    case 8:
        pInnerPtr = new (m_pWriter->GetHeap()) InnerPtrConst<8>(pBase);
        break;
    default:
        pInnerPtr = new (m_pWriter->GetHeap()) InnerPtrVar(pBase, offset);
        break;
    }

    m_entries.Add(pInnerPtr);
    return pInnerPtr;
}

void ZapInnerPtrTable::Resolve()
{
    for (InnerPtrTable::Iterator i = m_entries.Begin(), end = m_entries.End(); i != end; i++)
    {
        (*i)->Resolve();
    }
}
