//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


#ifndef bitSetAsUint64_DEFINED
#define bitSetAsUint64_DEFINED 1

#include "bitset.h"

template<typename Env, typename BitSetTraits>
class BitSetOps</*BitSetType*/UINT64, 
                /*Brand*/BSUInt64,
                /*Env*/Env, 
                /*BitSetTraits*/BitSetTraits>
{
public:
    typedef UINT64 Rep;
private:
    static UINT64 Singleton(unsigned bitNum)
    {
        assert(bitNum < sizeof(UINT64)*BitSetSupport::BitsInByte);
        return (UINT64)1 << bitNum;
    }

public:
 
    static void Assign(Env env, UINT64& lhs, UINT64 rhs)
    {
        lhs = rhs;
    }

    static void AssignNouninit(Env env, UINT64& lhs, UINT64 rhs)
    {
        lhs = rhs;
    }

    static void AssignAllowUninitRhs(Env env, UINT64& lhs, UINT64 rhs)
    {
        lhs = rhs;
    }

    static void AssignNoCopy(Env env, UINT64& lhs, UINT64 rhs)
    {
        lhs = rhs;
    }

    static void ClearD(Env env, UINT64& bs)
    {
        bs = 0;
    }

    static UINT64 MakeSingleton(Env env, unsigned bitNum)
    {
        assert(bitNum < BitSetTraits::GetSize(env));
        return Singleton(bitNum);
    }

    static UINT64 MakeCopy(Env env, UINT64 bs)
    {
        return bs;
    }

    static bool IsEmpty(Env env, UINT64 bs)
    {
        return bs == 0;
    }

    static unsigned Count(Env env, UINT64 bs)
    {
        return BitSetSupport::CountBitsInIntegral(bs);
    }

    static void UnionD(Env env, UINT64& bs1, UINT64 bs2)
    {
        bs1 |= bs2;
    }

    static UINT64 Union(Env env, UINT64& bs1, UINT64 bs2)
    {
        return bs1 | bs2;
    }

    static void DiffD(Env env, UINT64& bs1, UINT64 bs2)
    {
        bs1 = bs1 & ~bs2;
    }

    static UINT64 Diff(Env env, UINT64 bs1, UINT64 bs2)
    {
        return bs1 & ~bs2;
    }

    static void RemoveElemD(Env env, UINT64& bs1, unsigned i)
    {
        assert(i < BitSetTraits::GetSize(env));
        bs1 &= ~Singleton(i);
    }

    static UINT64 RemoveElem(Env env, UINT64 bs1, unsigned i)
    {
         return bs1 & ~Singleton(i);
    }

    static void AddElemD(Env env, UINT64& bs1, unsigned i)
    {
        assert(i < BitSetTraits::GetSize(env));
        bs1 |= Singleton(i);
    }

    static UINT64 AddElem(Env env, UINT64 bs1, unsigned i)
    {
        assert(i < BitSetTraits::GetSize(env));
        return bs1 | Singleton(i);
    }

    static bool IsMember(Env env, const UINT64 bs1, unsigned i)
    {
        assert(i < BitSetTraits::GetSize(env));
        return (bs1 & Singleton(i)) != 0;
    }

    static void IntersectionD(Env env, UINT64& bs1, UINT64 bs2)
    {
        bs1 &= bs2;
    }

    static UINT64 Intersection(Env env, UINT64 bs1, UINT64 bs2)
    {
        return bs1 & bs2;
    }

    static bool IsEmptyIntersection(Env env, UINT64 bs1, UINT64 bs2)
    {
        return (bs1 & bs2) == 0;
    }

    static bool IsSubset(Env env, UINT64 bs1, UINT64 bs2)
    {
        return ((bs1 & bs2) == bs1);
    }

    static bool Equal(Env env, UINT64 bs1, UINT64 bs2)
    {
        return bs1 == bs2;
    }

    static UINT64 MakeEmpty(Env env)
    {
        return 0;
    }

    static UINT64 MakeFull(Env env)
    {
        unsigned sz = BitSetTraits::GetSize(env);
        if (sz == sizeof(UINT64)*8)
        {
            return UINT64(-1);
        }
        else
        {
            return (UINT64(1) << sz) - 1;
        }
    }

#ifdef DEBUG
    static const char* ToString(Env env, UINT64 bs)
    {
        IAllocator* alloc = BitSetTraits::GetDebugOnlyAllocator(env);
        const int CharsForUINT64 = sizeof(UINT64)*2;
        char * res = NULL;
        const int AllocSize = CharsForUINT64 + 4;
        res = (char*)alloc->Alloc(AllocSize);
        UINT64 bits = bs;
        unsigned remaining = AllocSize;
        char* ptr = res;
        for (unsigned bytesDone = 0; bytesDone < sizeof(UINT64); bytesDone += sizeof(unsigned))
        {
            unsigned bits0 = (unsigned)bits;
            sprintf_s(ptr, remaining, "%08X", bits0);
            ptr += 8;
            remaining -= 8;
            bytesDone += 4;  assert(sizeof(unsigned) == 4);
            // Doing this twice by 16, rather than once by 32, avoids warnings when size_t == unsigned.
            bits = bits >> 16; bits = bits >> 16;
        }
        return res;
    }
#endif

    static UINT64 UninitVal()
    {
        return 0;
    }

    static bool MayBeUninit(UINT64 bs)
    {
        return bs == UninitVal();
    }

    class Iter
    {
        UINT64 m_bits;
      public:

        Iter(Env env, const UINT64& bits) : m_bits(bits) {}

        bool NextElem(Env env, unsigned* pElem)
        {
            if (m_bits)
            {
                unsigned bitNum = *pElem;
                while ((m_bits & 0x1) == 0) { bitNum++; m_bits >>= 1; }
                *pElem = bitNum;
                m_bits &= ~0x1;
                return true;
            }
            else
            {
                return false;
            }
        }
    };

    typedef UINT64 ValArgType;
    typedef UINT64 RetValType;
};

#endif // bitSetAsUint64_DEFINED
