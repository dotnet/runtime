// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

// This file contains the definition of the "Range check cloning" phase.
//
// See rangecheckcloning.cpp for context and overview.
//

// Min number of bounds checks required to form a group
#define MIN_CHECKS_PER_GROUP 4

// Max number of bounds checks allowed in a group.
// This is just an arbitrary number to avoid cloning too many checks.
#define MAX_CHECKS_PER_GROUP 64

// See comments in DoesComplexityExceed function for more details.
#define BUDGET_MULTIPLIER 40

struct BoundCheckLocation
{
    Statement* stmt;
    GenTree**  bndChkUse;
    int        stmtIdx;

    BoundCheckLocation(Statement* stmt, GenTree** bndChkUse, int stmtIdx)
        : stmt(stmt)
        , bndChkUse(bndChkUse)
        , stmtIdx(stmtIdx)
    {
        assert(stmt != nullptr);
        assert((bndChkUse != nullptr));
        assert((*bndChkUse) != nullptr);
        assert((*bndChkUse)->OperIs(GT_BOUNDS_CHECK));
        assert(stmtIdx >= 0);
    }
};

struct BoundsCheckInfo
{
    Statement* stmt;
    GenTree**  bndChkUse;
    ValueNum   lenVN;
    ValueNum   idxVN;
    int        offset;
    int        stmtIdx;

    BoundsCheckInfo()
        : stmt(nullptr)
        , bndChkUse(nullptr)
        , lenVN(ValueNumStore::NoVN)
        , idxVN(ValueNumStore::NoVN)
        , offset(0)
        , stmtIdx(0)
    {
    }

    bool Initialize(const Compiler* comp, Statement* statement, int statementIdx, GenTree** bndChkUse);

    GenTreeBoundsChk* BndChk() const
    {
        return (*bndChkUse)->AsBoundsChk();
    }
};

struct IdxLenPair
{
    IdxLenPair(ValueNum idx, ValueNum len)
        : idxVN(idx)
        , lenVN(len)
    {
    }

    ValueNum idxVN;
    ValueNum lenVN;
};

struct LargePrimitiveKeyFuncsIdxLenPair
{
    static unsigned GetHashCode(const IdxLenPair& val)
    {
        // VNs are mostly small integers
        return val.idxVN ^ (val.lenVN << 16);
    }

    static bool Equals(const IdxLenPair& x, const IdxLenPair& y)
    {
        return (x.idxVN == y.idxVN) && (x.lenVN == y.lenVN);
    }
};

typedef ArrayStack<BoundsCheckInfo> BoundsCheckInfoStack;

typedef JitHashTable<IdxLenPair, LargePrimitiveKeyFuncsIdxLenPair, BoundsCheckInfoStack*> BoundsCheckInfoMap;
