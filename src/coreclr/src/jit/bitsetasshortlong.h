// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// A set of integers in the range [0..N], for some N defined by the "Env" (via "BitSetTraits").
// 
// Represented as a pointer-sized item.  If N bits can fit in this item, the representation is "direct"; otherwise,
// the item is a pointer to an array of K size_t's, where K is the number of size_t's necessary to hold N bits.

#ifndef bitSetAsShortLong_DEFINED
#define bitSetAsShortLong_DEFINED 1

#include "bitset.h"
#include "compilerbitsettraits.h"

typedef size_t* BitSetShortLongRep;

template<typename Env, typename BitSetTraits>
class BitSetOps</*BitSetType*/BitSetShortLongRep,
                /*Brand*/BSShortLong,
                /*Env*/Env,
                /*BitSetTraits*/BitSetTraits>
{
public:
    typedef BitSetShortLongRep Rep;
private:
    static const unsigned BitsInSizeT = sizeof(size_t) * BitSetSupport::BitsInByte;

    inline static bool IsShort(Env env)
    {
        return BitSetTraits::GetArrSize(env, sizeof(size_t)) <= 1;
    }

    // The operations on the "long" (pointer-to-array-of-size_t) versions of the representation.
    static void AssignLong(Env env, BitSetShortLongRep& lhs, BitSetShortLongRep rhs);
    static BitSetShortLongRep MakeSingletonLong(Env env, unsigned bitNum);
    static BitSetShortLongRep MakeCopyLong(Env env, BitSetShortLongRep bs);
    static bool IsEmptyLong(Env env, BitSetShortLongRep bs);
    static unsigned CountLong(Env env, BitSetShortLongRep bs);
    static void UnionDLong(Env env, BitSetShortLongRep& bs1, BitSetShortLongRep bs2);
    static void DiffDLong(Env env, BitSetShortLongRep& bs1, BitSetShortLongRep bs2);
    static void AddElemDLong(Env env, BitSetShortLongRep& bs, unsigned i);
    static void RemoveElemDLong(Env env, BitSetShortLongRep& bs, unsigned i);
    static void ClearDLong(Env env, BitSetShortLongRep& bs);
    static BitSetShortLongRep MakeUninitArrayBits(Env env);
    static BitSetShortLongRep MakeEmptyArrayBits(Env env);
    static BitSetShortLongRep MakeFullArrayBits(Env env);
    static bool IsMemberLong(Env env, BitSetShortLongRep bs, unsigned i);
    static bool EqualLong(Env env, BitSetShortLongRep bs1, BitSetShortLongRep bs2);
    static bool IsSubsetLong(Env env, BitSetShortLongRep bs1, BitSetShortLongRep bs2); 
    static bool IsEmptyIntersectionLong(Env env, BitSetShortLongRep bs1, BitSetShortLongRep bs2); 
    static void IntersectionDLong(Env env, BitSetShortLongRep& bs1, BitSetShortLongRep bs2);
#ifdef DEBUG
    static const char* ToStringLong(Env env, BitSetShortLongRep bs);
#endif

public:

    inline static BitSetShortLongRep UninitVal()
    {
        return nullptr;
    }

    static bool MayBeUninit(BitSetShortLongRep bs)
    {
        return bs == UninitVal();
    }

    static void Assign(Env env, BitSetShortLongRep& lhs, BitSetShortLongRep rhs)
    {
        // We can't assert that rhs != UninitVal in the Short case, because in that
        // case it's a legal value.
        if (IsShort(env))
        {
            // Both are short.
            lhs = rhs;
        }
        else if (lhs == UninitVal())
        {
            assert(rhs != UninitVal());
            lhs = MakeCopy(env, rhs);
        }
        else
        {
            AssignLong(env, lhs, rhs);
        }
    }

    static void AssignAllowUninitRhs(Env env, BitSetShortLongRep& lhs, BitSetShortLongRep rhs)
    {
        if (IsShort(env))
        {
            // Both are short.
            lhs = rhs;
        }
        else if (rhs == UninitVal())
        {
            lhs = rhs;
        }
        else if (lhs == UninitVal())
        {
            lhs = MakeCopy(env, rhs);
        }
        else
        {
            AssignLong(env, lhs, rhs);
        }
    }


    static void AssignNoCopy(Env env, BitSetShortLongRep& lhs, BitSetShortLongRep rhs)
    {
        lhs = rhs;
    }

    static void ClearD(Env env, BitSetShortLongRep& bs)
    {
        if (IsShort(env))
        {
            bs = (BitSetShortLongRep)0;
        }
        else
        {
            assert(bs != UninitVal());
            ClearDLong(env, bs);
        }
    }

    static BitSetShortLongRep MakeSingleton(Env env, unsigned bitNum)
    {
        assert(bitNum < BitSetTraits::GetSize(env));
        if (IsShort(env))
        {
            return BitSetShortLongRep(((size_t)1) << bitNum);
        }
        else
        {
            return MakeSingletonLong(env, bitNum);
        }
    }

    static BitSetShortLongRep MakeCopy(Env env, BitSetShortLongRep bs)
    {
        if (IsShort(env))
        {
            return bs;
        }
        else
        {
            return MakeCopyLong(env, bs);
        }
    }

    static bool IsEmpty(Env env, BitSetShortLongRep bs)
    {
        if (IsShort(env)) 
        {
            return bs == nullptr;
        }
        else
        {
            assert(bs != UninitVal());
            return IsEmptyLong(env, bs);
        }
    }

    static unsigned Count(Env env, BitSetShortLongRep bs)
    {
        if (IsShort(env)) 
        {
            return BitSetSupport::CountBitsInIntegral(size_t(bs));
        }
        else
        {
            assert(bs != UninitVal());
            return CountLong(env, bs);
        }
    }

    static void UnionD(Env env, BitSetShortLongRep& bs1, BitSetShortLongRep bs2)
    {
        if (IsShort(env))
        {
            bs1 = (BitSetShortLongRep)(((size_t)bs1) | ((size_t)bs2));
        }
        else
        {
            UnionDLong(env, bs1, bs2);
        }
    }
    static BitSetShortLongRep Union(Env env, BitSetShortLongRep bs1, BitSetShortLongRep bs2)
    {
        BitSetShortLongRep res = MakeCopy(env, bs1);
        UnionD(env, res, bs2);
        return res;
    }
  
    static void DiffD(Env env, BitSetShortLongRep& bs1, BitSetShortLongRep bs2)
    {
        if (IsShort(env))
        {
            bs1 = (BitSetShortLongRep)(((size_t)bs1) & (~(size_t)bs2));
        }
        else
        {
            DiffDLong(env, bs1, bs2);
        }
    }
    static BitSetShortLongRep Diff(Env env, BitSetShortLongRep bs1, BitSetShortLongRep bs2)
    {
        BitSetShortLongRep res = MakeCopy(env, bs1);
        DiffD(env, res, bs2);
        return res;
    }

    static void RemoveElemD(Env env, BitSetShortLongRep& bs, unsigned i)
    {
        assert(i < BitSetTraits::GetSize(env));
        if (IsShort(env))
        {
            size_t mask = ((size_t)1) << i;
            mask = ~mask;
            bs = (BitSetShortLongRep)(((size_t)bs) & mask);
        }
        else
        {
            assert(bs != UninitVal());
            RemoveElemDLong(env, bs, i);
        }
    }
    static BitSetShortLongRep RemoveElem(Env env, BitSetShortLongRep bs, unsigned i)
    {
        BitSetShortLongRep res = MakeCopy(env, bs);
        RemoveElemD(env, res, i);
        return res;
    }

    static void AddElemD(Env env, BitSetShortLongRep& bs, unsigned i)
    {
        assert(i < BitSetTraits::GetSize(env));
        if (IsShort(env))
        {
            size_t mask = ((size_t)1) << i;
            bs = (BitSetShortLongRep)(((size_t)bs) | mask);
        }
        else
        {
            AddElemDLong(env, bs, i);
        }
    }
    static BitSetShortLongRep AddElem(Env env, BitSetShortLongRep bs, unsigned i)
    {
        BitSetShortLongRep res = MakeCopy(env, bs);
        AddElemD(env, res, i);
        return res;
    }

    static bool IsMember(Env env, const BitSetShortLongRep bs, unsigned i)
    {
        assert(i < BitSetTraits::GetSize(env));
        if (IsShort(env))
        {
            size_t mask = ((size_t)1) << i;
            return (((size_t)bs) & mask) != 0;
        }
        else
        {
            assert(bs != UninitVal());
            return IsMemberLong(env, bs, i);
        }
    }

    static void IntersectionD(Env env, BitSetShortLongRep& bs1, BitSetShortLongRep bs2)
    {
        if (IsShort(env))
        {
            (size_t&)bs1 &= (size_t)bs2;
        }
        else
        {
            IntersectionDLong(env, bs1, bs2);
        }
    }

    static BitSetShortLongRep Intersection(Env env, BitSetShortLongRep bs1, BitSetShortLongRep bs2)
    {
        BitSetShortLongRep res = MakeCopy(env, bs1);
        IntersectionD(env, res, bs2);
        return res;
    }
    static bool IsEmptyIntersection(Env env, BitSetShortLongRep bs1, BitSetShortLongRep bs2)
    {
        if (IsShort(env))
        {
            return (((size_t)bs1) & ((size_t)bs2)) == 0;
        }
        else
        {
            return IsEmptyIntersectionLong(env, bs1, bs2);
        }
    }

    static bool IsSubset(Env env, BitSetShortLongRep bs1, BitSetShortLongRep bs2)
    {
        if (IsShort(env))
        {
            size_t u1 = (size_t)bs1;
            size_t u2 = (size_t)bs2;
            return (u1 & u2) == u1;
        }
        else
        {
            return IsSubsetLong(env, bs1, bs2);
        }
    }

    static bool Equal(Env env, BitSetShortLongRep bs1, BitSetShortLongRep bs2)
    {
        if (IsShort(env))
        {
            return (size_t)bs1 == (size_t)bs2;
        }
        else
        {
            return EqualLong(env, bs1, bs2);
        }
    }

#ifdef DEBUG
    // Returns a string valid until the allocator releases the memory.
    static const char* ToString(Env env, BitSetShortLongRep bs)
    {
        if (IsShort(env))
        {
            assert(sizeof(BitSetShortLongRep) == sizeof(size_t));
            IAllocator* alloc = BitSetTraits::GetDebugOnlyAllocator(env);
            const int CharsForSizeT = sizeof(size_t)*2;
            char * res = nullptr;
            const int ShortAllocSize = CharsForSizeT + 4;
            res = (char*)alloc->Alloc(ShortAllocSize);
            size_t bits = (size_t)bs;
            unsigned remaining = ShortAllocSize;
            char* ptr = res;
            if (sizeof(size_t) == sizeof(int64_t))
            {
                sprintf_s(ptr, remaining, "%016llX", bits);
            }
            else
            {
                assert(sizeof(size_t) == sizeof(int));
                sprintf_s(ptr, remaining, "%08X", bits);
            }
            return res;
        }
        else
        {
            return ToStringLong(env, bs);
        }
    }
#endif

    static BitSetShortLongRep MakeEmpty(Env env)
    {
        if (IsShort(env))
        {
            return nullptr;
        }
        else
        {
            return MakeEmptyArrayBits(env);
        }
    }

    static BitSetShortLongRep MakeFull(Env env)
    {
        if (IsShort(env))
        {
            // Can't just shift by numBits+1, since that might be 32 (and (1 << 32( == 1, for an unsigned).
            unsigned numBits = BitSetTraits::GetSize(env);
            if (numBits == BitsInSizeT)
            {
                // Can't use the implementation below to get all 1's...
                return BitSetShortLongRep(size_t(-1));
            }
            else
            {
                return BitSetShortLongRep((size_t(1) << numBits) - 1);
            }
        }
        else
        {
            return MakeFullArrayBits(env);
        }
    }

    class Iter
    {
        BitSetShortLongRep m_bs;   // The BitSet that we're iterating over.
        size_t m_bits;             // The "current" bits remaining to be iterated over.  
        // In the "short" case, these are all the remaining bits.
        // In the "long" case, these are remaining bits in element "m_index"; 
        // these and the bits in the remaining elements comprise the remaining bits.
        unsigned m_index;          // If "m_bs" uses the long (indirect) representation, the current index in the array.
        // the index of the element in A(bs) that is currently being iterated.
        unsigned m_bitNum;         // The number of bits that have already been iterated over (set or clear).  If you
                                   // add this to the bit number of the next bit in "m_bits", you get the proper bit number of that
                                   // bit in "m_bs".

    public:
        Iter(Env env, const BitSetShortLongRep& bs): m_bs(bs), m_bitNum(0)
        {
            if (BitSetOps::IsShort(env))
            {
                m_index = 0;
                m_bits = (size_t)bs;
            }
            else
            {
                assert(bs != BitSetOps::UninitVal());
                m_index = 0;
                m_bits = bs[0];
            }
        }

        bool NextElem(Env env, unsigned* pElem)
        {
#if BITSET_TRACK_OPCOUNTS
            BitSetStaticsImpl::RecordOp(BitSetStaticsImpl::BSOP_NextBit);
#endif
            for (;;)
            {
                DWORD nextBit;
                BOOL hasBit;
#ifdef _WIN64
                static_assert_no_msg(sizeof(size_t) == 8);
                hasBit = BitScanForward64(&nextBit, m_bits);
#else
                static_assert_no_msg(sizeof(size_t) == 4);
                hasBit = BitScanForward(&nextBit, m_bits);
#endif

                // If there's a bit, doesn't matter if we're short or long.
                if (hasBit)
                {
                    *pElem = m_bitNum + nextBit;
                    m_bitNum += nextBit + 1;
                    m_bits >>= nextBit; m_bits >>= 1;  // Have to do these separately -- if we have 0x80000000, nextBit == 31, and shifting by 32 bits does nothing.
                    return true;
                }
                else
                {
                    unsigned len = BitSetTraits::GetArrSize(env, sizeof(size_t));
                    if (len <= 1)
                    {
                        return false;
                    }
                    else
                    {
                        m_index++;
                        if (m_index == len) return false;
                        // Otherwise...
                        m_bitNum = m_index * sizeof(size_t) * BitSetSupport::BitsInByte;
                        m_bits = m_bs[m_index];
                        continue;
                    }
                }
            }
        }
    };

    friend class Iter;

    typedef size_t* ValArgType;
    typedef size_t* RetValType;
};

template<typename Env, typename BitSetTraits>
void
BitSetOps</*BitSetType*/BitSetShortLongRep,
          /*Brand*/BSShortLong,
          /*Env*/Env,
          /*BitSetTraits*/BitSetTraits>::
  AssignLong(Env env, BitSetShortLongRep& lhs, BitSetShortLongRep rhs)
{
    assert(!IsShort(env));
    unsigned len = BitSetTraits::GetArrSize(env, sizeof(size_t));
    for (unsigned i = 0; i < len; i++) lhs[i] = rhs[i];
}

template<typename Env, typename BitSetTraits>
BitSetShortLongRep
BitSetOps</*BitSetType*/BitSetShortLongRep,
          /*Brand*/BSShortLong,
          /*Env*/Env,
          /*BitSetTraits*/BitSetTraits>::
  MakeSingletonLong(Env env, unsigned bitNum)
{
    assert(!IsShort(env));
    BitSetShortLongRep res = MakeEmptyArrayBits(env);
    unsigned index = bitNum/BitsInSizeT;
    res[index] = ((size_t)1) << (bitNum % BitsInSizeT);
    return res;
}

template<typename Env, typename BitSetTraits>
BitSetShortLongRep
BitSetOps</*BitSetType*/BitSetShortLongRep,
          /*Brand*/BSShortLong,
          /*Env*/Env,
          /*BitSetTraits*/BitSetTraits>::
  MakeCopyLong(Env env, BitSetShortLongRep bs)
{
    assert(!IsShort(env));
    BitSetShortLongRep res = MakeUninitArrayBits(env);
    unsigned len = BitSetTraits::GetArrSize(env, sizeof(size_t));
    for (unsigned i = 0; i < len; i++) res[i] = bs[i];
    return res;
}

template<typename Env, typename BitSetTraits>
bool
BitSetOps</*BitSetType*/BitSetShortLongRep,
          /*Brand*/BSShortLong,
          /*Env*/Env,
          /*BitSetTraits*/BitSetTraits>::
  IsEmptyLong(Env env, BitSetShortLongRep bs)
{
    assert(!IsShort(env));
    unsigned len = BitSetTraits::GetArrSize(env, sizeof(size_t));
    for (unsigned i = 0; i < len; i++)
    {
        if (bs[i] != 0) return false;
    }
    return true;
}

template<typename Env, typename BitSetTraits>
unsigned
BitSetOps</*BitSetType*/BitSetShortLongRep,
          /*Brand*/BSShortLong,
          /*Env*/Env,
          /*BitSetTraits*/BitSetTraits>::
  CountLong(Env env, BitSetShortLongRep bs)
{
    assert(!IsShort(env));
    unsigned len = BitSetTraits::GetArrSize(env, sizeof(size_t));
    unsigned res = 0;
    for (unsigned i = 0; i < len; i++)
    {
        res += BitSetSupport::CountBitsInIntegral(bs[i]);
    }
    return res;
}

template<typename Env, typename BitSetTraits>
void
BitSetOps</*BitSetType*/BitSetShortLongRep,
          /*Brand*/BSShortLong,
          /*Env*/Env,
          /*BitSetTraits*/BitSetTraits>::
  UnionDLong(Env env, BitSetShortLongRep& bs1, BitSetShortLongRep bs2)
{
    assert(!IsShort(env));
    unsigned len = BitSetTraits::GetArrSize(env, sizeof(size_t));
    for (unsigned i = 0; i < len; i++) bs1[i] |= bs2[i];
}

template<typename Env, typename BitSetTraits>
void
BitSetOps</*BitSetType*/BitSetShortLongRep,
          /*Brand*/BSShortLong,
          /*Env*/Env,
          /*BitSetTraits*/BitSetTraits>::
  DiffDLong(Env env, BitSetShortLongRep& bs1, BitSetShortLongRep bs2)
{
    assert(!IsShort(env));
    unsigned len = BitSetTraits::GetArrSize(env, sizeof(size_t));
    for (unsigned i = 0; i < len; i++) bs1[i] &= ~bs2[i];
}

template<typename Env, typename BitSetTraits>
void
BitSetOps</*BitSetType*/BitSetShortLongRep,
          /*Brand*/BSShortLong,
          /*Env*/Env,
          /*BitSetTraits*/BitSetTraits>::
  AddElemDLong(Env env, BitSetShortLongRep& bs, unsigned i)
{
    assert(!IsShort(env));
    unsigned index = i/BitsInSizeT;
    size_t mask = ((size_t)1) << (i % BitsInSizeT);
    bs[index] |= mask;
}

template<typename Env, typename BitSetTraits>
void
BitSetOps</*BitSetType*/BitSetShortLongRep,
          /*Brand*/BSShortLong,
          /*Env*/Env,
          /*BitSetTraits*/BitSetTraits>::
  RemoveElemDLong(Env env, BitSetShortLongRep& bs, unsigned i)
{
    assert(!IsShort(env));
    unsigned index = i/BitsInSizeT;
    size_t mask = ((size_t)1) << (i % BitsInSizeT);
    mask = ~mask;
    bs[index] &= mask;
}

template<typename Env, typename BitSetTraits>
void
BitSetOps</*BitSetType*/BitSetShortLongRep,
          /*Brand*/BSShortLong,
          /*Env*/Env,
          /*BitSetTraits*/BitSetTraits>::
  ClearDLong(Env env, BitSetShortLongRep& bs)
{
    assert(!IsShort(env));
    // Recall that ClearD does *not* require "bs" to be of the current epoch.
    // Therefore, we must allocate a new representation.
    bs = MakeEmptyArrayBits(env);
}

template<typename Env, typename BitSetTraits>
BitSetShortLongRep
BitSetOps</*BitSetType*/BitSetShortLongRep,
          /*Brand*/BSShortLong,
          /*Env*/Env,
          /*BitSetTraits*/BitSetTraits>::
  MakeUninitArrayBits(Env env)
{
    assert(!IsShort(env));
    unsigned len = BitSetTraits::GetArrSize(env, sizeof(size_t));
    assert(len > 1); // Or else would not require an array.
    return (BitSetShortLongRep)(BitSetTraits::GetAllocator(env)->Alloc(len*sizeof(size_t)));
}

template<typename Env, typename BitSetTraits>
BitSetShortLongRep
BitSetOps</*BitSetType*/BitSetShortLongRep,
          /*Brand*/BSShortLong,
          /*Env*/Env,
          /*BitSetTraits*/BitSetTraits>::
  MakeEmptyArrayBits(Env env)
{
    assert(!IsShort(env));
    unsigned len = BitSetTraits::GetArrSize(env, sizeof(size_t));
    assert(len > 1); // Or else would not require an array.
    BitSetShortLongRep res = (BitSetShortLongRep)(BitSetTraits::GetAllocator(env)->Alloc(len*sizeof(size_t)));
    for (unsigned i = 0; i < len; i++) res[i] = 0;
    return res;
}

template<typename Env, typename BitSetTraits>
BitSetShortLongRep
BitSetOps</*BitSetType*/BitSetShortLongRep,
          /*Brand*/BSShortLong,
          /*Env*/Env,
          /*BitSetTraits*/BitSetTraits>::
  MakeFullArrayBits(Env env)
{
    assert(!IsShort(env));
    unsigned len = BitSetTraits::GetArrSize(env, sizeof(size_t));
    assert(len > 1); // Or else would not require an array.
    BitSetShortLongRep res = (BitSetShortLongRep)(BitSetTraits::GetAllocator(env)->Alloc(len*sizeof(size_t)));
    for (unsigned i = 0; i < len-1; i++) res[i] = size_t(-1);
    // Start with all ones, shift in zeros in the last elem.
    unsigned lastElemBits = (BitSetTraits::GetSize(env)-1) % BitsInSizeT + 1;
    res[len-1] = (size_t(-1) >> (BitsInSizeT - lastElemBits));
    return res;
}

template<typename Env, typename BitSetTraits>
bool
BitSetOps</*BitSetType*/BitSetShortLongRep,
          /*Brand*/BSShortLong,
          /*Env*/Env,
          /*BitSetTraits*/BitSetTraits>::
  IsMemberLong(Env env, BitSetShortLongRep bs, unsigned i)
{
    assert(!IsShort(env));
    unsigned index = i/BitsInSizeT;
    unsigned bitInElem = (i % BitsInSizeT);
    size_t mask = ((size_t)1) << bitInElem;
    return (bs[index] & mask) != 0;
}

template<typename Env, typename BitSetTraits>
void
BitSetOps</*BitSetType*/BitSetShortLongRep,
          /*Brand*/BSShortLong,
          /*Env*/Env,
          /*BitSetTraits*/BitSetTraits>::
  IntersectionDLong(Env env, BitSetShortLongRep& bs1, BitSetShortLongRep bs2)
{
    assert(!IsShort(env));
    unsigned len = BitSetTraits::GetArrSize(env, sizeof(size_t));
    for (unsigned i = 0; i < len; i++) bs1[i] &= bs2[i];
}

template<typename Env, typename BitSetTraits>
bool
BitSetOps</*BitSetType*/BitSetShortLongRep,
          /*Brand*/BSShortLong,
          /*Env*/Env,
          /*BitSetTraits*/BitSetTraits>::
  IsEmptyIntersectionLong(Env env, BitSetShortLongRep bs1, BitSetShortLongRep bs2)
{
    assert(!IsShort(env));
    unsigned len = BitSetTraits::GetArrSize(env, sizeof(size_t));
    for (unsigned i = 0; i < len; i++)
    {
        if ((bs1[i] & bs2[i]) != 0) return false;
    }
    return true;
}

template<typename Env, typename BitSetTraits>
bool
BitSetOps</*BitSetType*/BitSetShortLongRep,
          /*Brand*/BSShortLong,
          /*Env*/Env,
          /*BitSetTraits*/BitSetTraits>::
  EqualLong(Env env, BitSetShortLongRep bs1, BitSetShortLongRep bs2)
{
    assert(!IsShort(env));
    unsigned len = BitSetTraits::GetArrSize(env, sizeof(size_t));
    for (unsigned i = 0; i < len; i++)
    {
        if (bs1[i] != bs2[i]) return false;
    }
    return true;
}

template<typename Env, typename BitSetTraits>
bool
BitSetOps</*BitSetType*/BitSetShortLongRep,
          /*Brand*/BSShortLong,
          /*Env*/Env,
          /*BitSetTraits*/BitSetTraits>::
  IsSubsetLong(Env env, BitSetShortLongRep bs1, BitSetShortLongRep bs2)
{
    assert(!IsShort(env));
    unsigned len = BitSetTraits::GetArrSize(env, sizeof(size_t));
    for (unsigned i = 0; i < len; i++)
    {
        if ((bs1[i] & bs2[i]) != bs1[i]) return false;
    }
    return true;
}

#ifdef DEBUG
template<typename Env, typename BitSetTraits>
const char*
BitSetOps</*BitSetType*/BitSetShortLongRep,
          /*Brand*/BSShortLong,
          /*Env*/Env,
          /*BitSetTraits*/BitSetTraits>::
  ToStringLong(Env env, BitSetShortLongRep bs)
{
    assert(!IsShort(env));
    unsigned len = BitSetTraits::GetArrSize(env, sizeof(size_t));
    const int CharsForSizeT = sizeof(size_t)*2;
    unsigned allocSz = len * CharsForSizeT + 4;
    unsigned remaining = allocSz;
    IAllocator* alloc = BitSetTraits::GetDebugOnlyAllocator(env);
    char * res = (char*)alloc->Alloc(allocSz);
    char * temp = res;
    for (unsigned i = len; 0 < i; i--)
    {
        size_t bits = bs[i-1];
        for (unsigned bytesDone = 0; bytesDone < sizeof(size_t); bytesDone += sizeof(unsigned))
        {
            unsigned bits0 = (unsigned)bits;
            sprintf_s(temp, remaining, "%08X", bits0);
            temp += 8;
            remaining -= 8;
            bytesDone += 4;  assert(sizeof(unsigned) == 4);
            // Doing this twice by 16, rather than once by 32, avoids warnings when size_t == unsigned.
            bits = bits >> 16; bits = bits >> 16;
        }
    }
    return res;
}
#endif

#endif // bitSetAsShortLong_DEFINED
