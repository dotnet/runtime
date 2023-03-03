// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif
#include "bitset.h"
#include "bitsetasuint64.h"
#include "bitsetasshortlong.h"
#include "bitsetasuint64inclass.h"

// clang-format off
unsigned BitSetSupport::BitCountTable[16] = { 0, 1, 1, 2,
                                              1, 2, 2, 3,
                                              1, 2, 2, 3,
                                              2, 3, 3, 4 };
// clang-format on

#ifdef DEBUG
template <typename BitSetType, unsigned Uniq, typename Env, typename BitSetTraits>
void BitSetSupport::RunTests(Env env)
{

    typedef BitSetOps<BitSetType, Uniq, Env, BitSetTraits> LclBitSetOps;

    // The tests require that the Size is at least 52...
    assert(BitSetTraits::GetSize(env) > 51);

    BitSetType bs1;
    LclBitSetOps::AssignNoCopy(env, bs1, LclBitSetOps::MakeEmpty(env));
    unsigned bs1bits[] = {0, 10, 44, 45};
    LclBitSetOps::AddElemD(env, bs1, bs1bits[0]);
    LclBitSetOps::AddElemD(env, bs1, bs1bits[1]);
    LclBitSetOps::AddElemD(env, bs1, bs1bits[2]);
    LclBitSetOps::AddElemD(env, bs1, bs1bits[3]);

    typename LclBitSetOps::Iter bsi(env, bs1);
    unsigned                    bitNum = 0;
    unsigned                    k      = 0;
    while (bsi.NextElem(&bitNum))
    {
        assert(bitNum == bs1bits[k]);
        k++;
    }
    assert(k == 4);

    assert(LclBitSetOps::Equal(env, bs1, LclBitSetOps::Union(env, bs1, bs1)));
    assert(LclBitSetOps::Equal(env, bs1, LclBitSetOps::Intersection(env, bs1, bs1)));
    assert(LclBitSetOps::IsSubset(env, bs1, bs1));

    BitSetType bs2;
    LclBitSetOps::AssignNoCopy(env, bs2, LclBitSetOps::MakeEmpty(env));
    unsigned bs2bits[] = {0, 10, 50, 51};
    LclBitSetOps::AddElemD(env, bs2, bs2bits[0]);
    LclBitSetOps::AddElemD(env, bs2, bs2bits[1]);
    LclBitSetOps::AddElemD(env, bs2, bs2bits[2]);
    LclBitSetOps::AddElemD(env, bs2, bs2bits[3]);

    unsigned   unionBits[] = {0, 10, 44, 45, 50, 51};
    BitSetType bsU12;
    LclBitSetOps::AssignNoCopy(env, bsU12, LclBitSetOps::Union(env, bs1, bs2));
    k      = 0;
    bsi    = typename LclBitSetOps::Iter(env, bsU12);
    bitNum = 0;
    while (bsi.NextElem(&bitNum))
    {
        assert(bitNum == unionBits[k]);
        k++;
    }
    assert(k == 6);

    k                                = 0;
    typename LclBitSetOps::Iter bsiL = typename LclBitSetOps::Iter(env, bsU12);
    bitNum                           = 0;
    while (bsiL.NextElem(&bitNum))
    {
        assert(bitNum == unionBits[k]);
        k++;
    }
    assert(k == 6);

    unsigned   intersectionBits[] = {0, 10};
    BitSetType bsI12;
    LclBitSetOps::AssignNoCopy(env, bsI12, LclBitSetOps::Intersection(env, bs1, bs2));
    k      = 0;
    bsi    = typename LclBitSetOps::Iter(env, bsI12);
    bitNum = 0;
    while (bsi.NextElem(&bitNum))
    {
        assert(bitNum == intersectionBits[k]);
        k++;
    }
    assert(k == 2);
}

class TestBitSetTraits
{
public:
    static void* Alloc(CompAllocator alloc, size_t byteSize)
    {
        return alloc.allocate<char>(byteSize);
    }
    static unsigned GetSize(CompAllocator alloc)
    {
        return 64;
    }
    static unsigned GetArrSize(CompAllocator alloc)
    {
        return (64 / 8) / sizeof(size_t);
    }
    static unsigned GetEpoch(CompAllocator alloc)
    {
        return 0;
    }
};

void BitSetSupport::TestSuite(CompAllocator env)
{
    BitSetSupport::RunTests<UINT64, BSUInt64, CompAllocator, TestBitSetTraits>(env);
    BitSetSupport::RunTests<BitSetShortLongRep, BSShortLong, CompAllocator, TestBitSetTraits>(env);
    BitSetSupport::RunTests<BitSetUint64<CompAllocator, TestBitSetTraits>, BSUInt64Class, CompAllocator,
                            TestBitSetTraits>(env);
}
#endif

const char* BitSetSupport::OpNames[BitSetSupport::BSOP_NUMOPS] = {
#define BSOPNAME(x) #x,
#include "bitsetops.h"
#undef BSOPNAME
};

void BitSetSupport::BitSetOpCounter::RecordOp(BitSetSupport::Operation op)
{
    OpCounts[op]++;
    TotalOps++;

    if ((TotalOps % 1000000) == 0)
    {
        if (OpOutputFile == nullptr)
        {
            OpOutputFile = fopen(m_fileName, "a");
        }
        fprintf(OpOutputFile, "@ %d total ops.\n", TotalOps);

        unsigned OpOrder[BSOP_NUMOPS];
        bool     OpOrdered[BSOP_NUMOPS];

        // First sort by total operations (into an index permutation array, using a simple n^2 sort).
        for (unsigned k = 0; k < BitSetSupport::BSOP_NUMOPS; k++)
        {
            OpOrdered[k] = false;
        }
        for (unsigned k = 0; k < BitSetSupport::BSOP_NUMOPS; k++)
        {
            bool     candSet = false;
            unsigned cand    = 0;
            unsigned candInd = 0;
            for (unsigned j = 0; j < BitSetSupport::BSOP_NUMOPS; j++)
            {
                if (OpOrdered[j])
                {
                    continue;
                }
                if (!candSet || OpCounts[j] > cand)
                {
                    candInd = j;
                    cand    = OpCounts[j];
                    candSet = true;
                }
            }
            assert(candSet);
            OpOrder[k]         = candInd;
            OpOrdered[candInd] = true;
        }

        for (unsigned ii = 0; ii < BitSetSupport::BSOP_NUMOPS; ii++)
        {
            unsigned i = OpOrder[ii];
            fprintf(OpOutputFile, "   Op %40s: %8d\n", OpNames[i], OpCounts[i]);
        }
    }
}
