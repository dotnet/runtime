// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// A set of integers in the range [0..N], for some given N.

/*****************************************************************************/
#ifndef _BITSET_H_
#define _BITSET_H_
/*****************************************************************************/

// This class provides some constant declarations and some static utility methods useful
// for bitset implementations.
class BitSetSupport
{
#ifdef DEBUG
    template <typename BitSetType, unsigned Brand, typename Env, typename BitSetTraits>
    static void RunTests(Env env);
#endif

public:
    static const unsigned BitsInByte = 8;

    // This maps 4-bit ("nibble") values into the number of 1 bits they contain.
    static unsigned BitCountTable[16];

    // Returns the number of 1 bits in the binary representation of "u".
    template <typename T>
    static unsigned CountBitsInIntegral(T u)
    {
        unsigned res = 0;
        // We process "u" in 4-bit nibbles, hence the "*2" below.
        for (unsigned int i = 0; i < sizeof(T) * 2; i++)
        {
            res += BitCountTable[u & 0xf];
            u >>= 4;
        }
        return res;
    }

#ifdef DEBUG
    // This runs the "TestSuite" method for a few important instantiations of BitSet.
    static void TestSuite(CompAllocator env);
#endif

    enum Operation
    {
#define BSOPNAME(x) x,
#include "bitsetops.h"
#undef BSOPNAME
        BSOP_NUMOPS
    };
    static const char* OpNames[BSOP_NUMOPS];

    class BitSetOpCounter
    {
        unsigned    TotalOps;
        unsigned    OpCounts[BSOP_NUMOPS];
        const char* m_fileName;
        FILE*       OpOutputFile;

    public:
        BitSetOpCounter(const char* fileName) : TotalOps(0), m_fileName(fileName), OpOutputFile(nullptr)
        {
            for (unsigned i = 0; i < BSOP_NUMOPS; i++)
            {
                OpCounts[i] = 0;
            }
        }

        void RecordOp(Operation op);
    };
};

template <>
FORCEINLINE unsigned BitSetSupport::CountBitsInIntegral<unsigned>(unsigned c)
{
    // Make sure we're 32 bit.
    assert(sizeof(unsigned) == 4);
    c = (c & 0x55555555) + ((c >> 1) & 0x55555555);
    c = (c & 0x33333333) + ((c >> 2) & 0x33333333);
    c = (c & 0x0f0f0f0f) + ((c >> 4) & 0x0f0f0f0f);
    c = (c & 0x00ff00ff) + ((c >> 8) & 0x00ff00ff);
    c = (c & 0x0000ffff) + ((c >> 16) & 0x0000ffff);
    return c;
}

// A "BitSet" represents a set of integers from a "universe" [0..N-1].  This implementation assumes that "N"
// (the "Size") is provided by the "Env" template argument type discussed below, and accessed from the Env
// via a static method of the BitSetTraits type discussed below.  The intent of "BitSet" is that the set is
// represented as a bit array.  Various binary operations therefore only make sense if the operands are
// subsets of the same universe.  Further, the integers in the set that the BitSet represents may have
// different interpretations at a higher level, so even if the range of the universe stays the same,
// the higher-level meaning of those bits may change.  For these reasons, we assume the Env can provide
// (again, via static methods of the BitSetTraits) the current "epoch" number.  The Env must keep the
// Size the same while the epoch has a given value; a BitSet implementation may legally stamp BitSets
// with the current epoch, and assert that BitSets from different epochs are not intermixed.

// Some implementations may use a representation that (at least sometimes) is a pointer to a
// heap-allocated data structure.  (The operations of BitSetOps are static methods, rather than
// declaring a BitSet class type with multiple subtypes, to allow maximally efficient raw
// primitive type representations.)  Therefore, we must be careful about assignment and
// initialization.  We often want to reason about BitSets as immutable values, and just copying
// the representation would introduce sharing in the indirect case, which is usually not what's
// desired.  On the other hand, there are many cases in which the RHS value has just been
// created functionally, and the initialization/assignment is obviously its last use.  In these
// cases, allocating a new indirect representation for the lhs (if it does not already have one)
// would be unnecessary and wasteful.  Thus, for assignment, we have a "normal" assignment
// function, which makes a copy of the referent data structure in the indirect case, and an
// "AssignNoCopy" version, which does not, and instead introduces sharing in the indirect case.
// Obviously, the latter should be used with care.
//
// (Orthogonally, there are also further versions of assignment that differ in whether the "rhs"
// argument may be uninitialized.  The normal assignment operation requires the "rhs" argument not be
// uninitialized; "AssignNoCopy" has the same requirement.  The "AssignAllowUninitRhs" version allows
// the "rhs" to be the uninit value, and sets the "lhs" to be uninitialized in that case.)

// This class has static methods that provide the operations on BitSets.
//
// An instantiation requires:
//    typename BitSetType:         the representation type of this kind of BitSet.
//
//    unsigned Brand:              an integer constant.  This is unused by the implementation; it exists
//                                 *only* to ensure that we can have, if desired, multiple distinct BitSetOps
//                                 implementations for the same BitSetType, by instantiating these with different
//                                 values for Brand (thus "branding" them so that they are distinct from one another.)
//
//    typename Env:                a type that determines the (current) size of the given BitSet type, as well
//                                 as an allocation function, and the current epoch (integer that changes when
//                                 "universe" of the BitSet changes) -- all via static methods of the "BitSetTraits"
//                                 type.
//
//    typename BitSetTraits:
//      An "adapter" class that provides methods that retrieves things from the Env:
//        static void* Alloc(Env, size_t byteSize): Allocates memory the BitSet implementation can use.
//        static unsigned    GetSize(Env):          the current size (= # of bits) of this bitset type.
//        static unsigned    GetArrSize(Env):       The number of size_t chunks sufficient to hold "GetSize".
//        static unsigned    GetEpoch(Env):         the current epoch.
//
// (For many instantiations, BitSetValueArgType and BitSetValueRetType will be the same as BitSetType; in cases where
// BitSetType is a class, type, BitSetValueArgType may need to be "const BitSetType&", for example.)
//
// In addition to implementing the method signatures here, an instantiation of BitSetOps must also export a
// BitSetOps::Iter type, which supports the following operations:
//      Iter(BitSetValueArgType):        a constructor
//      bool NextElem(unsigned* pElem):  returns true if the iteration is not complete, and sets *pElem to the next
//                                       yielded member.
//
// Finally, it should export two further types:
//
//    ValArgType: the type used to pass a BitSet as a by-value argument.
//    RetValType: the type that should be used to return a BitSet.
//
// For many instantiations, these can be identical to BitSetTypes.  When the representation type is a class,
// however, ValArgType may need to be "const BitSetType&", and RetValArg may need to be a helper class, if the
// class hides default copy constructors and assignment operators to detect erroneous usage.
//
template <typename BitSetType, unsigned Brand, typename Env, typename BitSetTraits>
class BitSetOps
{
#if 0
    // Below are the set of methods that an instantiation of BitSetOps should provide.  This is
    // #if'd out because it doesn't make any difference; C++ has no mechanism for checking that
    // the methods of an instantiation are consistent with these signatures, other than the expectations
    // embodied in the program that uses the instantiation(s).  But it's useful documentation, and
    // we should try to keep it up to date.

  public:

    // The uninitialized value -- not a real bitset (if possible).
    static BitSetValueRetType UninitVal();

    // Returns "true" iff "bs" may be the uninit value.
    static bool MayBeUninit(BitSetValueArgType bs);

    // Returns the a new BitSet that is empty.  Uses the Allocator of "env" to allocate memory for
    // the representation, if necessary.
    static BitSetValueRetType MakeEmpty(Env env);

    // Returns the a new BitSet that is "full" -- represents all the integers in the current range.
    // Uses the Allocator of "env" to allocate memory for the representation, if necessary.
    static BitSetValueRetType MakeFull(Env env);

    // Returns the set containing the single element "bitNum" (which is required to be within the
    // BitSet's current range).  Uses the Allocator of "env" to allocate memory for the representation,
    // if necessary.
    static BitSetValueRetType MakeSingleton(Env env, unsigned bitNum);

    // Assign "rhs" to "lhs".  "rhs" must not be the uninitialized value.  "lhs" may be, in which case
    // "rhs" will be copied if necessary.
    static void Assign(Env env, BitSetType& lhs, BitSetValueArgType rhs);

    // Assign "rhs" to "lhs"...*even* if "rhs" is the uninitialized value.
    static void AssignAllowUninitRhs(Env env, BitSetType& lhs, BitSetValueArgType rhs);

    // This is a "destructive" assignment -- it should only be used if the rhs is "dead" after the assignment.
    // In particular, if the rhs has a level of indirection to a heap-allocated data structure, that pointer will
    // be copied into the lhs.
    static void AssignNoCopy(Env env, BitSetType& lhs, BitSetValueArgType rhs);

    // Destructively set "bs" to be the empty set.
    static void ClearD(Env env, BitSetType& bs);

    // Returns a copy of "bs".  If the representation of "bs" involves a level of indirection, the data
    // structure is copied and a pointer to the copy is returned.
    static BitSetValueRetType MakeCopy(Env env, BitSetValueArgType bs);

    // Returns "true" iff ""bs" represents the empty set.
    static bool IsEmpty(Env env, BitSetValueArgType bs);

    // Returns the number of members in "bs".
    static unsigned Count(Env env, BitSetValueArgType bs);

    // Return true if the union of bs1 and bs2 is empty.
    static bool IsEmptyUnion(Env env, BitSetValueArgType bs1, BitSetValueArgType bs2);

    // Returns "true" iff "i" is a member of "bs".
    static bool IsMember(Env env, const BitSetValueArgType bs, unsigned i);

    // Destructively modify "bs" to ensure that "i" is a member.
    static void AddElemD(Env env, BitSetType& bs, unsigned i);
    // Returns a BitSet that is a copy of "bs" with "i" added.
    static BitSetValueRetType AddElem(Env env, BitSetValueArgType bs, unsigned i);

    // Destructively modify "bs" to ensure that "i" is not a member.
    static void RemoveElemD(Env env, BitSetType& bs, unsigned i);
    // Returns a BitSet that is a copy of "bs" with "i" removed.
    static BitSetValueRetType RemoveElem(Env env, BitSetValueArgType bs1, unsigned i);

    // Destructively modify "bs1" to be the union of "bs1" and "bs2".
    static void UnionD(Env env, BitSetType& bs1, BitSetValueArgType bs2);
    // Returns a new BitSet that is the union of "bs1" and "bs2".
    static BitSetValueRetType Union(Env env, BitSetValueArgType bs1, BitSetValueArgType bs2);

    // Destructively modify "bs1" to be the intersection of "bs1" and "bs2".
    static void IntersectionD(Env env, BitSetType& bs1, BitSetValueArgType bs2);
    // Returns a new BitSet that is the intersection of "bs1" and "bs2".
    static BitSetValueRetType Intersection(Env env, BitSetValueArgType bs1, BitSetValueArgType bs2);

    // Returns true iff "bs1" and "bs2" have an empty intersection.
    static bool IsEmptyIntersection(Env env, BitSetValueArgType bs1, BitSetValueArgType bs2);

    // Destructively modify "bs1" to be the set difference of "bs1" and "bs2".
    static void DiffD(Env env, BitSetType& bs1, BitSetValueArgType bs2);

    // Returns a new BitSet that is the set difference of "bs1" and "bs2".
    static BitSetValueRetType Diff(Env env, BitSetValueArgType bs1, BitSetValueArgType bs2);

    // Compute the live_in set. Variable is alive if there is use or it is out set, but not in def.
    // in = use | (out & ~def)
    static void LivenessD(Env env, BitSetType& in, BitSetValueArgType def, BitSetValueArgType use, BitSetValueArgType out);

    // Returns true iff "bs2" is a subset of "bs1."
    static bool IsSubset(Env env, BitSetValueArgType bs1, BitSetValueArgType bs2);

    // Returns true iff "bs1" and "bs2" are equal.
    static bool Equal(Env env, BitSetValueArgType bs1, BitSetValueArgType bs2);

#ifdef DEBUG
    // Returns a string representing the contents of "bs".  Allocates memory for the representation
    // using the Allocator of "env".
    static const char* ToString(Env env, BitSetValueArgType bs);
#endif

    // Declare this as a type -- will be a real class in real instantiations.
    class Iter {
      public:
        Iter(Env env, BitSetValueArgType bs) {}
        bool NextElem(unsigned* pElem) { return false; }
    };

    typename ValArgType;
    typename RetValType;
#endif // 0 -- the above is #if'd out, since it's really just an extended comment on what an instantiation
       // should provide.
};

template <typename BitSetType,
          unsigned Brand,
          typename Env,
          typename BitSetTraits,
          typename BitSetValueArgType,
          typename BitSetValueRetType,
          typename BaseIter>
class BitSetOpsWithCounter
{
    typedef BitSetOps<BitSetType, Brand, Env, BitSetTraits> BSO;

public:
    static BitSetValueRetType UninitVal()
    {
        return BSO::UninitVal();
    }
    static bool MayBeUninit(BitSetValueArgType bs)
    {
        return BSO::MayBeUninit(bs);
    }
    static BitSetValueRetType MakeEmpty(Env env)
    {
        BitSetTraits::GetOpCounter(env)->RecordOp(BitSetSupport::BSOP_MakeEmpty);
        return BSO::MakeEmpty(env);
    }
    static BitSetValueRetType MakeFull(Env env)
    {
        BitSetTraits::GetOpCounter(env)->RecordOp(BitSetSupport::BSOP_MakeFull);
        return BSO::MakeFull(env);
    }
    static BitSetValueRetType MakeSingleton(Env env, unsigned bitNum)
    {
        BitSetTraits::GetOpCounter(env)->RecordOp(BitSetSupport::BSOP_MakeSingleton);
        return BSO::MakeSingleton(env, bitNum);
    }
    static void Assign(Env env, BitSetType& lhs, BitSetValueArgType rhs)
    {
        BitSetTraits::GetOpCounter(env)->RecordOp(BitSetSupport::BSOP_Assign);
        BSO::Assign(env, lhs, rhs);
    }
    static void AssignAllowUninitRhs(Env env, BitSetType& lhs, BitSetValueArgType rhs)
    {
        BitSetTraits::GetOpCounter(env)->RecordOp(BitSetSupport::BSOP_AssignAllowUninitRhs);
        BSO::AssignAllowUninitRhs(env, lhs, rhs);
    }
    static void AssignNoCopy(Env env, BitSetType& lhs, BitSetValueArgType rhs)
    {
        BitSetTraits::GetOpCounter(env)->RecordOp(BitSetSupport::BSOP_AssignNocopy);
        BSO::AssignNoCopy(env, lhs, rhs);
    }
    static void ClearD(Env env, BitSetType& bs)
    {
        BitSetTraits::GetOpCounter(env)->RecordOp(BitSetSupport::BSOP_ClearD);
        BSO::ClearD(env, bs);
    }
    static BitSetValueRetType MakeCopy(Env env, BitSetValueArgType bs)
    {
        BitSetTraits::GetOpCounter(env)->RecordOp(BitSetSupport::BSOP_MakeCopy);
        return BSO::MakeCopy(env, bs);
    }
    static bool IsEmpty(Env env, BitSetValueArgType bs)
    {
        BitSetTraits::GetOpCounter(env)->RecordOp(BitSetSupport::BSOP_IsEmpty);
        return BSO::IsEmpty(env, bs);
    }
    static unsigned Count(Env env, BitSetValueArgType bs)
    {
        BitSetTraits::GetOpCounter(env)->RecordOp(BitSetSupport::BSOP_Count);
        return BSO::Count(env, bs);
    }
    static bool IsMember(Env env, const BitSetValueArgType bs, unsigned i)
    {
        BitSetTraits::GetOpCounter(env)->RecordOp(BitSetSupport::BSOP_IsMember);
        return BSO::IsMember(env, bs, i);
    }
    static void AddElemD(Env env, BitSetType& bs, unsigned i)
    {
        BitSetTraits::GetOpCounter(env)->RecordOp(BitSetSupport::BSOP_AddElemD);
        BSO::AddElemD(env, bs, i);
    }
    static BitSetValueRetType AddElem(Env env, BitSetValueArgType bs, unsigned i)
    {
        BitSetTraits::GetOpCounter(env)->RecordOp(BitSetSupport::BSOP_AddElem);
        return BSO::AddElem(env, bs, i);
    }
    static void RemoveElemD(Env env, BitSetType& bs, unsigned i)
    {
        BitSetTraits::GetOpCounter(env)->RecordOp(BitSetSupport::BSOP_RemoveElemD);
        BSO::RemoveElemD(env, bs, i);
    }
    static BitSetValueRetType RemoveElem(Env env, BitSetValueArgType bs1, unsigned i)
    {
        BitSetTraits::GetOpCounter(env)->RecordOp(BitSetSupport::BSOP_RemoveElem);
        return BSO::RemoveElem(env, bs1, i);
    }
    static void UnionD(Env env, BitSetType& bs1, BitSetValueArgType bs2)
    {
        BitSetTraits::GetOpCounter(env)->RecordOp(BitSetSupport::BSOP_UnionD);
        BSO::UnionD(env, bs1, bs2);
    }
    static BitSetValueRetType Union(Env env, BitSetValueArgType bs1, BitSetValueArgType bs2)
    {
        BitSetTraits::GetOpCounter(env)->RecordOp(BitSetSupport::BSOP_Union);
        return BSO::Union(env, bs1, bs2);
    }
    static void IntersectionD(Env env, BitSetType& bs1, BitSetValueArgType bs2)
    {
        BitSetTraits::GetOpCounter(env)->RecordOp(BitSetSupport::BSOP_IntersectionD);
        BSO::IntersectionD(env, bs1, bs2);
    }
    static BitSetValueRetType Intersection(Env env, BitSetValueArgType bs1, BitSetValueArgType bs2)
    {
        BitSetTraits::GetOpCounter(env)->RecordOp(BitSetSupport::BSOP_Intersection);
        return BSO::Intersection(env, bs1, bs2);
    }
    static bool IsEmptyIntersection(Env env, BitSetValueArgType bs1, BitSetValueArgType bs2)
    {
        BitSetTraits::GetOpCounter(env)->RecordOp(BitSetSupport::BSOP_IsEmptyIntersection);
        return BSO::IsEmptyIntersection(env, bs1, bs2);
    }
    static void DiffD(Env env, BitSetType& bs1, BitSetValueArgType bs2)
    {
        BitSetTraits::GetOpCounter(env)->RecordOp(BitSetSupport::BSOP_DiffD);
        BSO::DiffD(env, bs1, bs2);
    }
    static BitSetValueRetType Diff(Env env, BitSetValueArgType bs1, BitSetValueArgType bs2)
    {
        BitSetTraits::GetOpCounter(env)->RecordOp(BitSetSupport::BSOP_Diff);
        return BSO::Diff(env, bs1, bs2);
    }
    static bool IsSubset(Env env, BitSetValueArgType bs1, BitSetValueArgType bs2)
    {
        BitSetTraits::GetOpCounter(env)->RecordOp(BitSetSupport::BSOP_IsSubset);
        return BSO::IsSubset(env, bs1, bs2);
    }
    static bool Equal(Env env, BitSetValueArgType bs1, BitSetValueArgType bs2)
    {
        BitSetTraits::GetOpCounter(env)->RecordOp(BitSetSupport::BSOP_Equal);
        return BSO::Equal(env, bs1, bs2);
    }
#ifdef DEBUG
    static const char* ToString(Env env, BitSetValueArgType bs)
    {
        BitSetTraits::GetOpCounter(env)->RecordOp(BitSetSupport::BSOP_ToString);
        return BSO::ToString(env, bs);
    }
#endif

    class Iter
    {
        BaseIter m_iter;
        Env      m_env;

    public:
        Iter(Env env, BitSetValueArgType bs) : m_iter(env, bs), m_env(env)
        {
        }

        bool NextElem(unsigned* pElem)
        {
            BitSetTraits::GetOpCounter(m_env)->RecordOp(BitSetSupport::BSOP_NextBit);
            return m_iter.NextElem(pElem);
        }
    };
};

// We define symbolic names for the various bitset implementations available, to allow choices between them.

#define BSUInt64 0
#define BSShortLong 1
#define BSUInt64Class 2

/*****************************************************************************/
#endif // _BITSET_H_
/*****************************************************************************/
