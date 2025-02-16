// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#define MIN_CHECKS_PER_GROUP 4

struct BoundsCheckInfo
{
    Statement*        stmt;
    GenTree*          bndChkParent;
    GenTreeBoundsChk* bndChk;
    ValueNum          lenVN;
    ValueNum          idxVN;
    int               offset;

    BoundsCheckInfo()
        : stmt(nullptr)
        , bndChkParent(nullptr)
        , bndChk(nullptr)
        , lenVN(ValueNumStore::NoVN)
        , idxVN(ValueNumStore::NoVN)
        , offset(0)
    {
    }

    bool Initialize(const Compiler*   comp,
                    Statement*        statement,
                    GenTreeBoundsChk* bndChkNode,
                    GenTree*          bndChkParentNode);
};

struct IndexLengthPair
{
    IndexLengthPair(ValueNum idx, ValueNum len)
        : idxVN(idx)
        , lenVN(len)
    {
    }

    ValueNum idxVN;
    ValueNum lenVN;
};
struct LargePrimitiveKeyFuncsTwoVNs
{
    static unsigned GetHashCode(const IndexLengthPair& val)
    {
        // Value numbers are mostly small integers
        return val.idxVN ^ (val.lenVN << 16);
    }

    static bool Equals(const IndexLengthPair& x, const IndexLengthPair& y)
    {
        return x.idxVN == y.idxVN && x.lenVN == y.lenVN;
    }
};

typedef ArrayStack<BoundsCheckInfo> BoundsCheckInfoStack;

typedef JitHashTable<IndexLengthPair, LargePrimitiveKeyFuncsTwoVNs, BoundsCheckInfoStack*> BoundsCheckInfoMap;
