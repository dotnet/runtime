// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// This include file determines how BitVec is implemented.
//
#ifndef _BITVEC_INCLUDED_
#define _BITVEC_INCLUDED_ 1

// This class simplifies creation and usage of "ShortLong" bitsets.
//
// Create new bitsets like so:
//
//   BitVecTraits traits(size, pCompiler);
//   BitVec bitvec = BitVecOps::MakeEmpty(&traits);
//
// and call functions like so:
//
//   BitVecOps::AddElemD(&traits, bitvec, 10);
//   BitVecOps::IsMember(&traits, bitvec, 10));
//

#include "bitset.h"
#include "compilerbitsettraits.h"
#include "bitsetasshortlong.h"

typedef BitSetOps</*BitSetType*/ BitSetShortLongRep,
                  /*Brand*/ BSShortLong,
                  /*Env*/ BitVecTraits*,
                  /*BitSetTraits*/ BitVecTraits>
    BitVecOps;

typedef BitSetShortLongRep BitVec;

// These types should be used as the types for BitVec arguments and return values, respectively.
typedef BitVecOps::ValArgType BitVec_ValArg_T;
typedef BitVecOps::RetValType BitVec_ValRet_T;

// A set of small unsigned integers kept as one bit each. It is sized when first needed and grown
// on demand, so an index past the end is simply not a member. Unlike BitVec it needs no traits and
// no size up front, which suits a set whose index space is still being allocated while it fills.
class GrowableBitSet
{
    // Machine words, so the 32-bit host does no 64-bit arithmetic.
    typedef size_t        Word;
    static const unsigned BitsPerWord = sizeof(Word) * 8;

    CompAllocator m_alloc;
    Word*         m_words = nullptr;
    unsigned      m_count = 0;

public:
    GrowableBitSet(CompAllocator alloc)
        : m_alloc(alloc)
    {
    }

    bool Test(unsigned index) const
    {
        unsigned word = index / BitsPerWord;
        return (word < m_count) && ((m_words[word] & ((Word)1 << (index % BitsPerWord))) != 0);
    }

    void Set(unsigned index)
    {
        unsigned word = index / BitsPerWord;
        if (word >= m_count)
        {
            unsigned count = max(word + 1, m_count * 2);
            Word*    words = m_alloc.allocate<Word>(count);
            if (m_count != 0)
            {
                memcpy(words, m_words, m_count * sizeof(Word));
            }
            memset(words + m_count, 0, (count - m_count) * sizeof(Word));
            m_words = words;
            m_count = count;
        }
        m_words[word] |= (Word)1 << (index % BitsPerWord);
    }

    void Clear()
    {
        if (m_count != 0)
        {
            memset(m_words, 0, m_count * sizeof(Word));
        }
    }
};

#endif // _BITVEC_INCLUDED_
