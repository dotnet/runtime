// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "iallocator.h"

// JitHashTable implements a mapping from a Key type to a Value type,
// via a hash table.

// Synchronization is the responsibility of the caller: if a
// JitHashTable is used in a multithreaded context, the table should be
// associated with a lock.

// JitHashTable actually takes four template arguments: Key,
// KeyFuncs, Value, and Behavior.  We don't assume that Key has hash or equality
// functions specific names; rather, we assume that KeyFuncs has
// statics methods
//    int GetHashCode(Key)
// and
//    bool Equals(Key, Key)
// and use those.  An
// instantiator can thus make a small "adaptor class" to invoke
// existing instance method hash and/or equality functions.  If the
// implementor of a candidate Key class K understands this convention,
// these static methods can be implemented by K, so that K can be used
// as the actual arguments for the both Key and KeyTrait classes.
//
// The "Behavior" argument provides the following static members:
//
// s_growth_factor_numerator
// s_growth_factor_denominator                  Factor to grow allocation (numerator/denominator).
//                                              Typically inherited from default traits (3/2)
//
// s_density_factor_numerator
// s_density_factor_denominator                 Maxium occupied density of table before growth
//                                              occurs (num/denom).  Typically inherited (3/4).
//
// s_minimum_allocation                         Minimum table allocation count (size on first growth.)  It is
//                                              probably preferable to call Reallocate on initialization rather
//                                              than override his from the default traits. (7)
//
// NoMemory()                                   Called when the hash table is unable to grow due to potential
//                                              overflow or the lack of a sufficiently large prime.

class JitHashTableBehavior
{
public:
    static const unsigned s_growth_factor_numerator   = 3;
    static const unsigned s_growth_factor_denominator = 2;

    static const unsigned s_density_factor_numerator   = 3;
    static const unsigned s_density_factor_denominator = 4;

    static const unsigned s_minimum_allocation = 7;

    inline static void DECLSPEC_NORETURN NoMemory()
    {
        NOMEM();
    }
};

// Stores info about primes, including the magic number and shift amount needed
// to implement a divide without using the divide instruction
class JitPrimeInfo
{
public:
    JitPrimeInfo() : prime(0), magic(0), shift(0)
    {
    }
    JitPrimeInfo(unsigned p, unsigned m, unsigned s) : prime(p), magic(m), shift(s)
    {
    }
    unsigned prime;
    unsigned magic;
    unsigned shift;

    unsigned magicNumberDivide(unsigned numerator) const
    {
        unsigned __int64 num     = numerator;
        unsigned __int64 mag     = magic;
        unsigned __int64 product = (num * mag) >> (32 + shift);
        return (unsigned)product;
    }

    unsigned magicNumberRem(unsigned numerator) const
    {
        unsigned div    = magicNumberDivide(numerator);
        unsigned result = numerator - (div * prime);
        assert(result == numerator % prime);
        return result;
    }
};

// Table of primes and their magic-number-divide constant.
// For more info see the book "Hacker's Delight" chapter 10.9 "Unsigned Division by Divisors >= 1"
// These were selected by looking for primes, each roughly twice as big as the next, having
// 32-bit magic numbers, (because the algorithm for using 33-bit magic numbers is slightly slower).

// clang-format off
SELECTANY const JitPrimeInfo jitPrimeInfo[]
{
    JitPrimeInfo(9,         0x38e38e39, 1),
    JitPrimeInfo(23,        0xb21642c9, 4),
    JitPrimeInfo(59,        0x22b63cbf, 3),
    JitPrimeInfo(131,       0xfa232cf3, 7),
    JitPrimeInfo(239,       0x891ac73b, 7),
    JitPrimeInfo(433,       0x975a751,  4),
    JitPrimeInfo(761,       0x561e46a5, 8),
    JitPrimeInfo(1399,      0xbb612aa3, 10),
    JitPrimeInfo(2473,      0x6a009f01, 10),
    JitPrimeInfo(4327,      0xf2555049, 12),
    JitPrimeInfo(7499,      0x45ea155f, 11),
    JitPrimeInfo(12973,     0x1434f6d3, 10),               
    JitPrimeInfo(22433,     0x2ebe18db, 12),               
    JitPrimeInfo(46559,     0xb42bebd5, 15),               
    JitPrimeInfo(96581,     0xadb61b1b, 16),
    JitPrimeInfo(200341,    0x29df2461, 15),
    JitPrimeInfo(415517,    0xa181c46d, 18),
    JitPrimeInfo(861719,    0x4de0bde5, 18),
    JitPrimeInfo(1787021,   0x9636c46f, 20),
    JitPrimeInfo(3705617,   0x4870adc1, 20),
    JitPrimeInfo(7684087,   0x8bbc5b83, 22),
    JitPrimeInfo(15933877,  0x86c65361, 23),           
    JitPrimeInfo(33040633,  0x40fec79b, 23),           
    JitPrimeInfo(68513161,  0x7d605cd1, 25),           
    JitPrimeInfo(142069021, 0xf1da390b, 27),           
    JitPrimeInfo(294594427, 0x74a2507d, 27),
    JitPrimeInfo(733045421, 0x5dbec447, 28),
};
// clang-format on

// Hash table class definition

template <typename Key, typename KeyFuncs, typename Value, typename Behavior = JitHashTableBehavior>
class JitHashTable
{
public:
    // Forward declaration.
    class KeyIterator;

    // Constructor/destructor.  SHash tables always start out empty, with no
    // allocation overhead.  Call Reallocate to prime with an initial size if
    // desired. Pass NULL as the IAllocator* if you want to use DefaultAllocator
    // (basically, operator new/delete).

    JitHashTable(IAllocator* alloc) : m_alloc(alloc), m_table(NULL), m_tableSizeInfo(), m_tableCount(0), m_tableMax(0)
    {
        assert(m_alloc != nullptr);

#ifndef __GNUC__ // these crash GCC
        static_assert_no_msg(Behavior::s_growth_factor_numerator > Behavior::s_growth_factor_denominator);
        static_assert_no_msg(Behavior::s_density_factor_numerator < Behavior::s_density_factor_denominator);
#endif
    }

    ~JitHashTable()
    {
        RemoveAll();
    }

    // operators new/delete when an IAllocator is to be used.
    void* operator new(size_t sz, IAllocator* alloc)
    {
        return alloc->Alloc(sz);
    }

    void* operator new[](size_t sz, IAllocator* alloc)
    {
        return alloc->Alloc(sz);
    }

    void operator delete(void* p, IAllocator* alloc)
    {
        alloc->Free(p);
    }

    void operator delete[](void* p, IAllocator* alloc)
    {
        alloc->Free(p);
    }

    // If the table contains a mapping for "key", returns "true" and
    // sets "*pVal" to the value to which "key" maps.  Otherwise,
    // returns false, and does not modify "*pVal".
    bool Lookup(Key k, Value* pVal = NULL) const
    {
        Node* pN = FindNode(k);

        if (pN != NULL)
        {
            if (pVal != NULL)
            {
                *pVal = pN->m_val;
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    Value* LookupPointer(Key k) const
    {
        Node* pN = FindNode(k);

        if (pN != NULL)
            return &(pN->m_val);
        else
            return NULL;
    }

    // Causes the table to map "key" to "val".  Returns "true" if
    // "key" had already been mapped by the table, "false" otherwise.
    bool Set(Key k, Value v)
    {
        CheckGrowth();

        assert(m_tableSizeInfo.prime != 0);

        unsigned index = GetIndexForKey(k);

        Node* pN = m_table[index];
        while (pN != NULL && !KeyFuncs::Equals(k, pN->m_key))
        {
            pN = pN->m_next;
        }
        if (pN != NULL)
        {
            pN->m_val = v;
            return true;
        }
        else
        {
            Node* pNewNode = new (m_alloc) Node(k, v, m_table[index]);
            m_table[index] = pNewNode;
            m_tableCount++;
            return false;
        }
    }

    // Ensures that "key" is not mapped to a value by the "table."
    // Returns "true" iff it had been mapped.
    bool Remove(Key k)
    {
        unsigned index = GetIndexForKey(k);

        Node*  pN  = m_table[index];
        Node** ppN = &m_table[index];
        while (pN != NULL && !KeyFuncs::Equals(k, pN->m_key))
        {
            ppN = &pN->m_next;
            pN  = pN->m_next;
        }
        if (pN != NULL)
        {
            *ppN = pN->m_next;
            m_tableCount--;
            Node::operator delete(pN, m_alloc);
            return true;
        }
        else
        {
            return false;
        }
    }

    // Remove all mappings in the table.
    void RemoveAll()
    {
        for (unsigned i = 0; i < m_tableSizeInfo.prime; i++)
        {
            for (Node* pN = m_table[i]; pN != NULL;)
            {
                Node* pNext = pN->m_next;
                Node::operator delete(pN, m_alloc);
                pN = pNext;
            }
        }
        m_alloc->Free(m_table);

        m_table         = NULL;
        m_tableSizeInfo = JitPrimeInfo();
        m_tableCount    = 0;
        m_tableMax      = 0;

        return;
    }

    // Begin and End pointers for iteration over entire table.
    KeyIterator Begin() const
    {
        KeyIterator i(this, TRUE);
        return i;
    }

    KeyIterator End() const
    {
        return KeyIterator(this, FALSE);
    }

    // Return the number of elements currently stored in the table
    unsigned GetCount() const
    {
        return m_tableCount;
    }

private:
    // Forward declaration of the linked-list node class.
    struct Node;

    unsigned GetIndexForKey(Key k) const
    {
        unsigned hash = KeyFuncs::GetHashCode(k);

        unsigned index = m_tableSizeInfo.magicNumberRem(hash);

        return index;
    }

    // If the table has a mapping for "k", return the node containing
    // that mapping, else "NULL".
    Node* FindNode(Key k) const
    {
        if (m_tableSizeInfo.prime == 0)
            return NULL;

        unsigned index = GetIndexForKey(k);

        Node* pN = m_table[index];
        if (pN == NULL)
            return NULL;

        // Otherwise...
        while (pN != NULL && !KeyFuncs::Equals(k, pN->m_key))
            pN = pN->m_next;

        assert(pN == NULL || KeyFuncs::Equals(k, pN->m_key));

        // If pN != NULL, it's the node for the key, else the key isn't mapped.
        return pN;
    }

    // Resizes a hash table for growth.  The new size is computed based
    // on the current population, growth factor, and maximum density factor.
    void Grow()
    {
        unsigned newSize =
            (unsigned)(m_tableCount * Behavior::s_growth_factor_numerator / Behavior::s_growth_factor_denominator *
                       Behavior::s_density_factor_denominator / Behavior::s_density_factor_numerator);
        if (newSize < Behavior::s_minimum_allocation)
            newSize = Behavior::s_minimum_allocation;

        // handle potential overflow
        if (newSize < m_tableCount)
            Behavior::NoMemory();

        Reallocate(newSize);
    }

    // See if it is OK to grow the hash table by one element.  If not, reallocate
    // the hash table.
    void CheckGrowth()
    {
        if (m_tableCount == m_tableMax)
        {
            Grow();
        }
    }

public:
    // Reallocates a hash table to a specific size.  The size must be big enough
    // to hold all elements in the table appropriately.
    //
    // Note that the actual table size must always be a prime number; the number
    // passed in will be upward adjusted if necessary.
    void Reallocate(unsigned newTableSize)
    {
        assert(newTableSize >=
               (GetCount() * Behavior::s_density_factor_denominator / Behavior::s_density_factor_numerator));

        // Allocation size must be a prime number.  This is necessary so that hashes uniformly
        // distribute to all indices, and so that chaining will visit all indices in the hash table.
        JitPrimeInfo newPrime = NextPrime(newTableSize);
        newTableSize          = newPrime.prime;

        Node** newTable = (Node**)m_alloc->ArrayAlloc(newTableSize, sizeof(Node*));

        for (unsigned i = 0; i < newTableSize; i++)
        {
            newTable[i] = NULL;
        }

        // Move all entries over to new table (re-using the Node structures.)

        for (unsigned i = 0; i < m_tableSizeInfo.prime; i++)
        {
            Node* pN = m_table[i];
            while (pN != NULL)
            {
                Node* pNext = pN->m_next;

                unsigned newIndex  = newPrime.magicNumberRem(KeyFuncs::GetHashCode(pN->m_key));
                pN->m_next         = newTable[newIndex];
                newTable[newIndex] = pN;

                pN = pNext;
            }
        }

        // @todo:
        // We might want to try to delay this cleanup to allow asynchronous readers
        if (m_table != NULL)
            m_alloc->Free(m_table);

        m_table         = newTable;
        m_tableSizeInfo = newPrime;
        m_tableMax =
            (unsigned)(newTableSize * Behavior::s_density_factor_numerator / Behavior::s_density_factor_denominator);
    }

    // For iteration, we use a pattern similar to the STL "forward
    // iterator" pattern.  It basically consists of wrapping an
    // "iteration variable" in an object, and providing pointer-like
    // operators on the iterator. Example usage:
    //
    // for (JitHashTable::KeyIterator iter = foo->Begin(), end = foo->End(); !iter.Equal(end); iter++)
    // {
    //      // use foo, iter.
    // }
    // iter.Get() will yield (a reference to) the
    // current key.  It will assert the equivalent of "iter != end."
    class KeyIterator
    {
    private:
        friend class JitHashTable;

        // The method implementations have to be here for portability.
        // Some compilers won't compile the separate implementation in shash.inl

        Node**   m_table;
        Node*    m_node;
        unsigned m_tableSize;
        unsigned m_index;

    public:
        KeyIterator(const JitHashTable* hash, BOOL begin)
            : m_table(hash->m_table)
            , m_node(NULL)
            , m_tableSize(hash->m_tableSizeInfo.prime)
            , m_index(begin ? 0 : m_tableSize)
        {
            if (begin && hash->m_tableCount > 0)
            {
                assert(m_table != NULL);
                while (m_index < m_tableSize && m_table[m_index] == NULL)
                    m_index++;

                if (m_index >= m_tableSize)
                {
                    return;
                }
                else
                {
                    m_node = m_table[m_index];
                }
                assert(m_node != NULL);
            }
        }

        const Key& Get() const
        {
            assert(m_node != NULL);

            return m_node->m_key;
        }

        const Value& GetValue() const
        {
            assert(m_node != NULL);

            return m_node->m_val;
        }

        void SetValue(const Value& value) const
        {
            assert(m_node != NULL);

            m_node->m_val = value;
        }

        void Next()
        {
            if (m_node != NULL)
            {
                m_node = m_node->m_next;
                if (m_node != NULL)
                {
                    return;
                }

                // Otherwise...
                m_index++;
            }
            while (m_index < m_tableSize && m_table[m_index] == NULL)
                m_index++;

            if (m_index >= m_tableSize)
            {
                m_node = NULL;
                return;
            }
            else
            {
                m_node = m_table[m_index];
            }
            assert(m_node != NULL);
        }

        bool Equal(const KeyIterator& i) const
        {
            return i.m_node == m_node;
        }

        void operator++()
        {
            Next();
        }

        void operator++(int)
        {
            Next();
        }
    };

    Value& operator[](Key k) const
    {
        Value* p = LookupPointer(k);
        assert(p);
        return *p;
    }

private:
    // Find the next prime number >= the given value.
    static JitPrimeInfo NextPrime(unsigned number)
    {
        for (int i = 0; i < (int)(sizeof(jitPrimeInfo) / sizeof(jitPrimeInfo[0])); i++)
        {
            if (jitPrimeInfo[i].prime >= number)
            {
                return jitPrimeInfo[i];
            }
        }

        // overflow
        Behavior::NoMemory();
    }

    // Instance members
    IAllocator* m_alloc; // IAllocator to use in this
                         // table.
    // The node type.
    struct Node
    {
        Node* m_next; // Assume that the alignment requirement of Key and Value are no greater than Node*, so put m_next
                      // to avoid unnecessary padding.
        Key   m_key;
        Value m_val;

        Node(Key k, Value v, Node* next) : m_next(next), m_key(k), m_val(v)
        {
        }

        void* operator new(size_t sz, IAllocator* alloc)
        {
            return alloc->Alloc(sz);
        }

        void operator delete(void* p, IAllocator* alloc)
        {
            alloc->Free(p);
        }
    };

    Node**       m_table;         // pointer to table
    JitPrimeInfo m_tableSizeInfo; // size of table (a prime) and information about it
    unsigned     m_tableCount;    // number of elements in table
    unsigned     m_tableMax;      // maximum occupied count
};

// A few simple KeyFuncs types...

// Base class for types whose equality function is the same as their "==".
template <typename T>
struct JitKeyFuncsDefEquals
{
    static bool Equals(const T& x, const T& y)
    {
        return x == y;
    }
};

template <typename T>
struct JitPtrKeyFuncs : public JitKeyFuncsDefEquals<const T*>
{
public:
    static unsigned GetHashCode(const T* ptr)
    {
        // Hmm.  Maybe (unsigned) ought to be "ssize_t" -- or this ought to be ifdef'd by size.
        return static_cast<unsigned>(reinterpret_cast<uintptr_t>(ptr));
    }
};

template <typename T> // Must be coercable to "unsigned" with no loss of information.
struct JitSmallPrimitiveKeyFuncs : public JitKeyFuncsDefEquals<T>
{
    static unsigned GetHashCode(const T& val)
    {
        return static_cast<unsigned>(val);
    }
};

template <typename T> // Assumed to be of size sizeof(UINT64).
struct JitLargePrimitiveKeyFuncs : public JitKeyFuncsDefEquals<T>
{
    static unsigned GetHashCode(const T val)
    {
        // A static cast when T is a float or a double converts the value (i.e. 0.25 converts to 0)
        //
        // Instead we want to use all of the bits of a float to create the hash value
        // So we cast the address of val to a pointer to an equivalent sized unsigned int
        // This allows us to read the actual bit representation of a float type
        //
        // We can't read beyond the end of val, so we use sizeof(T) to determine
        // exactly how many bytes to read
        //
        if (sizeof(T) == 8)
        {
            // cast &val to (UINT64 *) then deref to get the bits
            UINT64 asUINT64 = *(reinterpret_cast<const UINT64*>(&val));

            // Get the upper and lower 32-bit values from the 64-bit value
            UINT32 upper32 = static_cast<UINT32>(asUINT64 >> 32);
            UINT32 lower32 = static_cast<UINT32>(asUINT64 & 0xFFFFFFFF);

            // Exclusive-Or the upper32 and the lower32 values
            return static_cast<unsigned>(upper32 ^ lower32);
        }
        else if (sizeof(T) == 4)
        {
            // cast &val to (UINT32 *) then deref to get the bits
            UINT32 asUINT32 = *(reinterpret_cast<const UINT32*>(&val));

            // Just return the 32-bit value
            return static_cast<unsigned>(asUINT32);
        }
        else if ((sizeof(T) == 2) || (sizeof(T) == 1))
        {
            // For small sizes we must have an integer type
            // so we can just use the static_cast.
            //
            return static_cast<unsigned>(val);
        }
        else
        {
            // Only support Hashing for types that are 8,4,2 or 1 bytes in size
            assert(!"Unsupported size");
            return static_cast<unsigned>(val); // compile-time error here when we have a illegal size
        }
    }
};
