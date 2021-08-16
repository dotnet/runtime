// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef bitSetAsUint64InClass_DEFINED
#define bitSetAsUint64InClass_DEFINED 1

#include "bitset.h"
#include "bitsetasuint64.h"
#include "stdmacros.h"

template <typename Env, typename BitSetTraits>
class BitSetUint64ValueRetType;

template <typename Env, typename BitSetTraits>
class BitSetUint64Iter;

template <typename Env, typename BitSetTraits>
class BitSetUint64
{
public:
    typedef BitSetUint64<Env, BitSetTraits> Rep;

private:
    friend class BitSetOps</*BitSetType*/ BitSetUint64<Env, BitSetTraits>,
                           /*Brand*/ BSUInt64Class,
                           /*Env*/ Env,
                           /*BitSetTraits*/ BitSetTraits>;

    friend class BitSetUint64ValueRetType<Env, BitSetTraits>;

    UINT64 m_bits;

#ifdef DEBUG
    unsigned m_epoch;
#endif

    typedef BitSetOps<UINT64, BSUInt64, Env, BitSetTraits> Uint64BitSetOps;

    void CheckEpoch(Env env) const
    {
#ifdef DEBUG
        assert(m_epoch == BitSetTraits::GetEpoch(env));
#endif
    }

    bool operator==(const BitSetUint64& bs) const
    {
        return m_bits == bs.m_bits
#ifdef DEBUG
               && m_epoch == bs.m_epoch
#endif
            ;
    }

public:
    BitSetUint64& operator=(const BitSetUint64& bs)
    {
        m_bits = bs.m_bits;
#ifdef DEBUG
        m_epoch = bs.m_epoch;
#endif // DEBUG
        return (*this);
    }

    BitSetUint64(const BitSetUint64& bs)
        : m_bits(bs.m_bits)
#ifdef DEBUG
        , m_epoch(bs.m_epoch)
#endif
    {
    }

private:
    // Return the number of bits set in the BitSet.
    inline unsigned Count(Env env) const
    {
        CheckEpoch(env);
        return Uint64BitSetOps::Count(env, m_bits);
    }

    inline void DiffD(Env env, const BitSetUint64& bs2)
    {
        CheckEpoch(env);
        bs2.CheckEpoch(env);
        Uint64BitSetOps::DiffD(env, m_bits, bs2.m_bits);
    }

    inline BitSetUint64 Diff(Env env, const BitSetUint64& bs2) const
    {
        CheckEpoch(env);
        bs2.CheckEpoch(env);
        BitSetUint64 res(*this);
        Uint64BitSetOps::DiffD(env, res.m_bits, bs2.m_bits);
        return res;
    }

    inline void RemoveElemD(Env env, unsigned i)
    {
        CheckEpoch(env);
        Uint64BitSetOps::RemoveElemD(env, m_bits, i);
    }

    inline BitSetUint64 RemoveElem(Env env, unsigned i) const
    {
        CheckEpoch(env);
        BitSetUint64 res(*this);
        Uint64BitSetOps::RemoveElemD(env, res.m_bits, i);
        return res;
    }

    inline void AddElemD(Env env, unsigned i)
    {
        CheckEpoch(env);
        Uint64BitSetOps::AddElemD(env, m_bits, i);
    }

    inline BitSetUint64 AddElem(Env env, unsigned i) const
    {
        CheckEpoch(env);
        BitSetUint64 res(*this);
        Uint64BitSetOps::AddElemD(env, res.m_bits, i);
        return res;
    }

    inline bool IsMember(Env env, unsigned i) const
    {
        CheckEpoch(env);
        return Uint64BitSetOps::IsMember(env, m_bits, i);
    }

    inline void IntersectionD(Env env, const BitSetUint64& bs2)
    {
        CheckEpoch(env);
        bs2.CheckEpoch(env);
        m_bits = m_bits & bs2.m_bits;
    }

    inline BitSetUint64 Intersection(Env env, const BitSetUint64& bs2) const
    {
        CheckEpoch(env);
        bs2.CheckEpoch(env);
        BitSetUint64 res(*this);
        Uint64BitSetOps::IntersectionD(env, res.m_bits, bs2.m_bits);
        return res;
    }

    inline bool IsEmptyUnion(Env env, const BitSetUint64& bs2) const
    {
        CheckEpoch(env);
        bs2.CheckEpoch(env);
        return Uint64BitSetOps::IsEmptyUnion(env, m_bits, bs2.m_bits);
    }

    inline void UnionD(Env env, const BitSetUint64& bs2)
    {
        CheckEpoch(env);
        bs2.CheckEpoch(env);
        Uint64BitSetOps::UnionD(env, m_bits, bs2.m_bits);
    }

    inline BitSetUint64 Union(Env env, const BitSetUint64& bs2) const
    {
        CheckEpoch(env);
        bs2.CheckEpoch(env);
        BitSetUint64 res(*this);
        Uint64BitSetOps::UnionD(env, res.m_bits, bs2.m_bits);
        return res;
    }

    inline void ClearD(Env env)
    {
        assert(m_epoch == BitSetTraits::GetEpoch(env));
        Uint64BitSetOps::ClearD(env, m_bits);
    }

    inline bool IsEmpty(Env env) const
    {
        CheckEpoch(env);
        return Uint64BitSetOps::IsEmpty(env, m_bits);
    }

    inline void LivenessD(Env env, const BitSetUint64& def, const BitSetUint64& use, const BitSetUint64& out)
    {
        CheckEpoch(env);
        def.CheckEpoch(env);
        use.CheckEpoch(env);
        out.CheckEpoch(env);
        return Uint64BitSetOps::LivenessD(env, m_bits, def.m_bits, use.m_bits, out.m_bits);
    }

    inline bool IsSubset(Env env, const BitSetUint64& bs2) const
    {
        CheckEpoch(env);
        bs2.CheckEpoch(env);
        return Uint64BitSetOps::IsSubset(env, m_bits, bs2.m_bits);
    }

    inline bool IsEmptyIntersection(Env env, const BitSetUint64& bs2) const
    {
        CheckEpoch(env);
        bs2.CheckEpoch(env);
        return Uint64BitSetOps::IsEmptyIntersection(env, m_bits, bs2.m_bits);
    }

    inline bool Equal(Env env, const BitSetUint64& bs2) const
    {
        CheckEpoch(env);
        bs2.CheckEpoch(env);
        return Uint64BitSetOps::Equal(env, m_bits, bs2.m_bits);
    }

    const char* ToString(Env env) const
    {
        return Uint64BitSetOps::ToString(env, m_bits);
    }

public:
    // Uninint
    BitSetUint64()
        : m_bits(0)
#ifdef DEBUG
        , m_epoch(UINT32_MAX) // Undefined.
#endif
    {
    }

    BitSetUint64(Env env, bool full = false)
        : m_bits(0)
#ifdef DEBUG
        , m_epoch(BitSetTraits::GetEpoch(env))
#endif
    {
        if (full)
        {
            m_bits = Uint64BitSetOps::MakeFull(env);
        }
    }

    inline BitSetUint64(const BitSetUint64ValueRetType<Env, BitSetTraits>& rt);

    BitSetUint64(Env env, unsigned bitNum)
        : m_bits(Uint64BitSetOps::MakeSingleton(env, bitNum))
#ifdef DEBUG
        , m_epoch(BitSetTraits::GetEpoch(env))
#endif
    {
        assert(bitNum < BitSetTraits::GetSize(env));
    }
};

template <typename Env, typename BitSetTraits>
class BitSetUint64ValueRetType
{
    friend class BitSetUint64<Env, BitSetTraits>;

    BitSetUint64<Env, BitSetTraits> m_bs;

public:
    BitSetUint64ValueRetType(const BitSetUint64<Env, BitSetTraits>& bs) : m_bs(bs)
    {
    }
};

template <typename Env, typename BitSetTraits>
BitSetUint64<Env, BitSetTraits>::BitSetUint64(const BitSetUint64ValueRetType<Env, BitSetTraits>& rt)
    : m_bits(rt.m_bs.m_bits)
#ifdef DEBUG
    , m_epoch(rt.m_bs.m_epoch)
#endif
{
}

template <typename Env, typename BitSetTraits>
class BitSetOps</*BitSetType*/ BitSetUint64<Env, BitSetTraits>,
                /*Brand*/ BSUInt64Class,
                /*Env*/ Env,
                /*BitSetTraits*/ BitSetTraits>
{
    typedef BitSetUint64<Env, BitSetTraits>             BST;
    typedef const BitSetUint64<Env, BitSetTraits>&      BSTValArg;
    typedef BitSetUint64ValueRetType<Env, BitSetTraits> BSTRetVal;

public:
    static BSTRetVal UninitVal()
    {
        return BitSetUint64<Env, BitSetTraits>();
    }

    static bool MayBeUninit(BSTValArg bs)
    {
        return bs == UninitVal();
    }

    static void Assign(Env env, BST& lhs, BSTValArg rhs)
    {
        lhs = rhs;
    }

    static void AssignNouninit(Env env, BST& lhs, BSTValArg rhs)
    {
        lhs = rhs;
    }

    static void AssignAllowUninitRhs(Env env, BST& lhs, BSTValArg rhs)
    {
        lhs = rhs;
    }

    static void AssignNoCopy(Env env, BST& lhs, BSTValArg rhs)
    {
        lhs = rhs;
    }

    static void ClearD(Env env, BST& bs)
    {
        bs.ClearD(env);
    }

    static BSTRetVal MakeSingleton(Env env, unsigned bitNum)
    {
        assert(bitNum < BitSetTraits::GetSize(env));
        return BST(env, bitNum);
    }

    static BSTRetVal MakeCopy(Env env, BSTValArg bs)
    {
        return bs;
    }

    static bool IsEmpty(Env env, BSTValArg bs)
    {
        return bs.IsEmpty(env);
    }

    static unsigned Count(Env env, BSTValArg bs)
    {
        return bs.Count(env);
    }

    static bool IsEmptyUnion(Env env, BSTValArg bs1, BSTValArg bs2)
    {
        return bs1.IsEmptyUnion(env, bs2);
    }

    static void UnionD(Env env, BST& bs1, BSTValArg bs2)
    {
        bs1.UnionD(env, bs2);
    }

    static BSTRetVal Union(Env env, BSTValArg bs1, BSTValArg bs2)
    {
        return bs1.Union(env, bs2);
    }

    static void DiffD(Env env, BST& bs1, BSTValArg bs2)
    {
        bs1.DiffD(env, bs2);
    }

    static BSTRetVal Diff(Env env, BSTValArg bs1, BSTValArg bs2)
    {
        return bs1.Diff(env, bs2);
    }

    static void RemoveElemD(Env env, BST& bs1, unsigned i)
    {
        assert(i < BitSetTraits::GetSize(env));
        bs1.RemoveElemD(env, i);
    }

    static BSTRetVal RemoveElem(Env env, BSTValArg bs1, unsigned i)
    {
        assert(i < BitSetTraits::GetSize(env));
        return bs1.RemoveElem(env, i);
    }

    static void AddElemD(Env env, BST& bs1, unsigned i)
    {
        assert(i < BitSetTraits::GetSize(env));
        bs1.AddElemD(env, i);
    }

    static BSTRetVal AddElem(Env env, BSTValArg bs1, unsigned i)
    {
        assert(i < BitSetTraits::GetSize(env));
        return bs1.AddElem(env, i);
    }

    static bool IsMember(Env env, BSTValArg bs1, unsigned i)
    {
        assert(i < BitSetTraits::GetSize(env));
        return bs1.IsMember(env, i);
    }

    static void IntersectionD(Env env, BST& bs1, BSTValArg bs2)
    {
        bs1.IntersectionD(env, bs2);
    }

    static BSTRetVal Intersection(Env env, BSTValArg bs1, BSTValArg bs2)
    {
        return bs1.Intersection(env, bs2);
    }

    static bool IsEmptyIntersection(Env env, BSTValArg bs1, BSTValArg bs2)
    {
        return bs1.IsEmptyIntersection(env, bs2);
    }

    static void LivenessD(Env env, BST& in, BSTValArg def, BSTValArg use, BSTValArg out)
    {
        in.LivenessD(env, def, use, out);
    }
    static bool IsSubset(Env env, BSTValArg bs1, BSTValArg bs2)
    {
        return bs1.IsSubset(env, bs2);
    }

    static bool Equal(Env env, BSTValArg bs1, BSTValArg bs2)
    {
        return bs1.Equal(env, bs2);
    }

    static bool NotEqual(Env env, BSTValArg bs1, BSTValArg bs2)
    {
        return !bs1.Equal(env, bs2);
    }

    static BSTRetVal MakeEmpty(Env env)
    {
        return BST(env);
    }

    static BSTRetVal MakeFull(Env env)
    {
        return BST(env, /*full*/ true);
    }

#ifdef DEBUG
    static const char* ToString(Env env, BSTValArg bs)
    {
        return bs.ToString(env);
    }
#endif

    // You *can* clear a bit after it's been iterated.  But you shouldn't otherwise mutate the
    // bitset during bit iteration.
    class Iter
    {
        UINT64   m_bits;
        unsigned m_bitNum;

    public:
        Iter(Env env, const BitSetUint64<Env, BitSetTraits>& bs) : m_bits(bs.m_bits), m_bitNum(0)
        {
        }

        bool NextElem(unsigned* pElem)
        {
            static const unsigned UINT64_SIZE = 64;

            if ((m_bits & 0x1) != 0)
            {
                *pElem = m_bitNum;
                m_bitNum++;
                m_bits >>= 1;
                return true;
            }
            else
            {
                // Skip groups of 4 zeros -- an optimization for sparse bitsets.
                while (m_bitNum < UINT64_SIZE && (m_bits & 0xf) == 0)
                {
                    m_bitNum += 4;
                    m_bits >>= 4;
                }
                while (m_bitNum < UINT64_SIZE && (m_bits & 0x1) == 0)
                {
                    m_bitNum += 1;
                    m_bits >>= 1;
                }
                if (m_bitNum < UINT64_SIZE)
                {
                    *pElem = m_bitNum;
                    m_bitNum++;
                    m_bits >>= 1;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    };

    typedef const BitSetUint64<Env, BitSetTraits>&      ValArgType;
    typedef BitSetUint64ValueRetType<Env, BitSetTraits> RetValType;
};
#endif // bitSetAsUint64InClass_DEFINED
