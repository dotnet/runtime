// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Defines the type "ValueNum".

// This file exists only to break an include file cycle -- had been in ValueNum.h.  But that
// file wanted to include gentree.h to get GT_COUNT, and gentree.h wanted to include ValueNum.h for
// the ValueNum type.

/*****************************************************************************/
#ifndef _VALUENUMTYPE_H_
#define _VALUENUMTYPE_H_
/*****************************************************************************/

// We will represent ValueNum's as unsigned integers.
typedef UINT32 ValueNum;

// There are two "kinds" of value numbers, which differ in their modeling of the actions of other threads.
// "Liberal" value numbers assume that the other threads change contents of memory locations only at
// synchronization points.  Liberal VNs are appropriate, for example, in identifying CSE opportunities.
// "Conservative" value numbers assume that the contents of memory locations change arbitrarily between
// every two accesses.  Conservative VNs are appropriate, for example, in assertion prop, where an observation
// of a property of the value in some storage location is used to perform an optimization downstream on
// an operation involving the contents of that storage location.  If other threads may modify the storage
// location between the two accesses, the observed property may no longer hold -- and conservative VNs make
// it clear that the values need not be the same.
//
enum ValueNumKind
{
    VNK_Liberal,
    VNK_Conservative
};

struct ValueNumPair
{
private:
    ValueNum m_liberal;
    ValueNum m_conservative;

public:
    ValueNum GetLiberal() const
    {
        return m_liberal;
    }
    void SetLiberal(ValueNum vn)
    {
        m_liberal = vn;
    }
    ValueNum GetConservative() const
    {
        return m_conservative;
    }
    void SetConservative(ValueNum vn)
    {
        m_conservative = vn;
    }

    ValueNum* GetLiberalAddr()
    {
        return &m_liberal;
    }
    ValueNum* GetConservativeAddr()
    {
        return &m_conservative;
    }

    ValueNum Get(ValueNumKind vnk)
    {
        if (vnk == VNK_Liberal)
        {
            return m_liberal;
        }
        else
        {
            assert(vnk == VNK_Conservative);
            return m_conservative;
        }
    }

    void Set(ValueNumKind vnk, ValueNum vn)
    {
        if (vnk == VNK_Liberal)
        {
            SetLiberal(vn);
        }
        else
        {
            assert(vnk == VNK_Conservative);
            SetConservative(vn);
        }
    }

    void SetBoth(ValueNum vn)
    {
        m_liberal      = vn;
        m_conservative = vn;
    }

    bool operator==(const ValueNumPair& other) const
    {
        return (m_liberal == other.m_liberal) && (m_conservative == other.m_conservative);
    }

    bool operator!=(const ValueNumPair& other) const
    {
        return !(*this == other);
    }

    void operator=(const ValueNumPair& vn2)
    {
        m_liberal      = vn2.m_liberal;
        m_conservative = vn2.m_conservative;
    }

    // Initializes both elements to "NoVN".  Defined in ValueNum.cpp.
    ValueNumPair();

    ValueNumPair(ValueNum lib, ValueNum cons) : m_liberal(lib), m_conservative(cons)
    {
    }

    // True iff neither element is "NoVN".  Defined in ValueNum.cpp.
    bool BothDefined() const;

    bool BothEqual() const
    {
        return m_liberal == m_conservative;
    }
};

#endif // _VALUENUMTYPE_H_
