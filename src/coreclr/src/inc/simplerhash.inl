//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

// ==++==
// 

// 

//
// ==--==

#ifndef _SIMPLERHASHTABLE_INL_
#define _SIMPLERHASHTABLE_INL_

// Many SimplerHashTable functions do not throw on their own, but may propagate an exception
// from Hash, Equals, or GetKey.
#define NOTHROW_UNLESS_BEHAVIOR_THROWS     if (Behavior::s_NoThrow) NOTHROW; else THROWS

void DECLSPEC_NORETURN ThrowOutOfMemory();

// To implement magic-number divide with a 32-bit magic number,
// multiply by the magic number, take the top 64 bits, and shift that
// by the amount given in the table.

inline
unsigned magicNumberDivide(unsigned numerator, const PrimeInfo &p)
{
    unsigned __int64 num = numerator;
    unsigned __int64 mag = p.magic;
    unsigned __int64 product = (num * mag) >> (32 + p.shift);
    return (unsigned) product;
}

inline
unsigned magicNumberRem(unsigned numerator, const PrimeInfo &p)
{
    unsigned div = magicNumberDivide(numerator, p);
    unsigned result = numerator - (div * p.prime);
    assert(result == numerator % p.prime);
    return result;
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
SimplerHashTable<Key,KeyFuncs,Value,Behavior>::SimplerHashTable(IAllocator* alloc)
  : m_alloc(alloc),
    m_table(NULL),
    m_tableCount(0),
    m_tableSizeInfo(),
    m_tableMax(0)
{
    LIMITED_METHOD_CONTRACT;

    if (m_alloc == NULL) m_alloc = DefaultAllocator::Singleton();

#ifndef __GNUC__ // these crash GCC
    static_assert_no_msg(Behavior::s_growth_factor_numerator > Behavior::s_growth_factor_denominator);
    static_assert_no_msg(Behavior::s_density_factor_numerator < Behavior::s_density_factor_denominator);
#endif
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
SimplerHashTable<Key,KeyFuncs,Value,Behavior>::~SimplerHashTable()
{
    LIMITED_METHOD_CONTRACT;

    RemoveAll();
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
void * SimplerHashTable<Key,KeyFuncs,Value,Behavior>::operator new(size_t sz, IAllocator * alloc)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
    return alloc->Alloc(sz);
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
void * SimplerHashTable<Key,KeyFuncs,Value,Behavior>::operator new[](size_t sz, IAllocator * alloc)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
    return alloc->Alloc(sz);
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
void SimplerHashTable<Key,KeyFuncs,Value,Behavior>::operator delete(void * p, IAllocator * alloc)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
    alloc->Free(p);
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
void SimplerHashTable<Key,KeyFuncs,Value,Behavior>::operator delete[](void * p, IAllocator * alloc)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
    alloc->Free(p);
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
unsigned SimplerHashTable<Key,KeyFuncs,Value,Behavior>::GetCount() const 
{
    LIMITED_METHOD_CONTRACT;

    return m_tableCount;
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
bool SimplerHashTable<Key,KeyFuncs,Value,Behavior>::Lookup(Key key, Value* pVal) const
{
    CONTRACTL
    {
        NOTHROW_UNLESS_BEHAVIOR_THROWS;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
    }
    CONTRACTL_END;

    Node* pN = FindNode(key);

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

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
Value *SimplerHashTable<Key,KeyFuncs,Value,Behavior>::LookupPointer(Key key) const
{
    CONTRACTL
    {
        NOTHROW_UNLESS_BEHAVIOR_THROWS;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
    }
    CONTRACTL_END;

    Node* pN = FindNode(key);

    if (pN != NULL)
        return &(pN->m_val);
    else
        return NULL;
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
typename SimplerHashTable<Key,KeyFuncs,Value,Behavior>::Node*
SimplerHashTable<Key,KeyFuncs,Value,Behavior>::FindNode(Key k) const
{
    CONTRACT(Node*)
    {
        NOTHROW_UNLESS_BEHAVIOR_THROWS;
        GC_NOTRIGGER;
        POSTCONDITION(RETVAL == NULL || KeyFuncs::Equals(k, RETVAL->m_key));
    }
    CONTRACT_END;

    if (m_tableSizeInfo.prime == 0)
        RETURN NULL;

    unsigned index = GetIndexForKey(k);

    Node* pN = m_table[index];
    if (pN == NULL) RETURN NULL;
    // Otherwise...
    while (pN != NULL && !KeyFuncs::Equals(k, pN->m_key))
        pN = pN->m_next;
    // If pN != NULL, it's the node for the key, else the key isn't mapped.
    RETURN pN;    
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
unsigned SimplerHashTable<Key,KeyFuncs,Value,Behavior>::GetIndexForKey(Key k) const
{
    CONTRACTL
    {
        NOTHROW_UNLESS_BEHAVIOR_THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    unsigned hash = KeyFuncs::GetHashCode(k);
    
    unsigned index = magicNumberRem(hash, m_tableSizeInfo);

    return index;
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
bool SimplerHashTable<Key,KeyFuncs,Value,Behavior>::Set(Key k, Value v)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
    }
    CONTRACTL_END;

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

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
bool SimplerHashTable<Key,KeyFuncs,Value,Behavior>::Remove(Key k)
{
    CONTRACTL
    {
        NOTHROW_UNLESS_BEHAVIOR_THROWS;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
    }
    CONTRACTL_END;

    unsigned index = GetIndexForKey(k);

    Node* pN = m_table[index];
    Node** ppN = &m_table[index];
    while (pN != NULL && !KeyFuncs::Equals(k, pN->m_key))
    {
        ppN = &pN->m_next;
        pN = pN->m_next;
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

// TBD
template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
void SimplerHashTable<Key,KeyFuncs,Value,Behavior>::Remove(KeyIterator& i)
{
#if 0
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
        PRECONDITION(s_supports_remove);
        PRECONDITION(!(IsNull(*i)));
        PRECONDITION(!(IsDeleted(*i)));
    }
    CONTRACT_END;

    RemoveElement(m_table, m_tableSize, (element_t*)&(*i));
#endif
    return;
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
void SimplerHashTable<Key,KeyFuncs,Value,Behavior>::RemoveAll()
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
    }
    CONTRACT_END;

    for (unsigned i = 0; i < m_tableSizeInfo.prime; i++)
    {
        for (Node* pN = m_table[i]; pN != NULL; )
        {
            Node* pNext = pN->m_next;
            Node::operator delete(pN, m_alloc);
            pN = pNext;
        }
    }
    m_alloc->Free(m_table);

    m_table = NULL;
    m_tableSizeInfo = PrimeInfo();
    m_tableCount = 0;
    m_tableMax = 0;

    RETURN;
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
typename SimplerHashTable<Key,KeyFuncs,Value,Behavior>::KeyIterator SimplerHashTable<Key,KeyFuncs,Value,Behavior>::Begin() const
{
    LIMITED_METHOD_CONTRACT;

    KeyIterator i(this, TRUE);
    return i;
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
typename SimplerHashTable<Key,KeyFuncs,Value,Behavior>::KeyIterator SimplerHashTable<Key,KeyFuncs,Value,Behavior>::End() const
{
    LIMITED_METHOD_CONTRACT;

    return KeyIterator(this, FALSE);
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
void SimplerHashTable<Key,KeyFuncs,Value,Behavior>::CheckGrowth()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
    }
    CONTRACTL_END;

    if (m_tableCount == m_tableMax)
    {
        Grow();
    }
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
void SimplerHashTable<Key,KeyFuncs,Value,Behavior>::Grow()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
    }
    CONTRACTL_END;

    unsigned newSize = (unsigned) (m_tableCount 
                                   * Behavior::s_growth_factor_numerator / Behavior::s_growth_factor_denominator
                                   * Behavior::s_density_factor_denominator / Behavior::s_density_factor_numerator);
    if (newSize < Behavior::s_minimum_allocation)
        newSize = Behavior::s_minimum_allocation;

    // handle potential overflow
    if (newSize < m_tableCount)
        ThrowOutOfMemory();

    Reallocate(newSize);
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
void SimplerHashTable<Key,KeyFuncs,Value,Behavior>::Reallocate(unsigned newTableSize)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
        PRECONDITION(newTableSize >= 
                     (GetCount() * Behavior::s_density_factor_denominator / Behavior::s_density_factor_numerator));
    }
    CONTRACTL_END;

    // Allocation size must be a prime number.  This is necessary so that hashes uniformly
    // distribute to all indices, and so that chaining will visit all indices in the hash table.
    PrimeInfo newPrime = NextPrime(newTableSize);
    newTableSize = newPrime.prime;

    Node** newTable = (Node**)m_alloc->ArrayAlloc(newTableSize, sizeof(Node*));

    for (unsigned i = 0; i < newTableSize; i++) {
        newTable[i] = NULL;
    }

    // Move all entries over to new table (re-using the Node structures.)

    for (unsigned i = 0; i < m_tableSizeInfo.prime; i++)
    {
        Node* pN = m_table[i];
        while (pN != NULL)
        {
            Node* pNext = pN->m_next;
            
            unsigned newIndex = magicNumberRem(KeyFuncs::GetHashCode(pN->m_key), newPrime);
            pN->m_next = newTable[newIndex];
            newTable[newIndex] = pN;
            
            pN = pNext;
        }
    }

    // @todo:
    // We might want to try to delay this cleanup to allow asynchronous readers
    if (m_table != NULL)
        m_alloc->Free(m_table);

    m_table = newTable;
    m_tableSizeInfo = newPrime;
    m_tableMax = (unsigned) (newTableSize * Behavior::s_density_factor_numerator / Behavior::s_density_factor_denominator);
}

// Table of primes and their magic-number-divide constant.
// For more info see the book "Hacker's Delight" chapter 10.9 "Unsigned Division by Divisors >= 1"
// These were selected by looking for primes, each roughly twice as big as the next, having
// 32-bit magic numbers, (because the algorithm for using 33-bit magic numbers is slightly slower). 
//

SELECTANY const PrimeInfo primeInfo[] = 
{
    PrimeInfo(9,         0x38e38e39, 1),
    PrimeInfo(23,        0xb21642c9, 4),
    PrimeInfo(59,        0x22b63cbf, 3),
    PrimeInfo(131,       0xfa232cf3, 7),
    PrimeInfo(239,       0x891ac73b, 7),
    PrimeInfo(433,       0x975a751,  4),
    PrimeInfo(761,       0x561e46a5, 8),
    PrimeInfo(1399,      0xbb612aa3, 10),
    PrimeInfo(2473,      0x6a009f01, 10),
    PrimeInfo(4327,      0xf2555049, 12),
    PrimeInfo(7499,      0x45ea155f, 11),
    PrimeInfo(12973,     0x1434f6d3, 10),               
    PrimeInfo(22433,     0x2ebe18db, 12),               
    PrimeInfo(46559,     0xb42bebd5, 15),               
    PrimeInfo(96581,     0xadb61b1b, 16),
    PrimeInfo(200341,    0x29df2461, 15),
    PrimeInfo(415517,    0xa181c46d, 18),
    PrimeInfo(861719,    0x4de0bde5, 18),
    PrimeInfo(1787021,   0x9636c46f, 20),
    PrimeInfo(3705617,   0x4870adc1, 20),
    PrimeInfo(7684087,   0x8bbc5b83, 22),
    PrimeInfo(15933877,  0x86c65361, 23),           
    PrimeInfo(33040633,  0x40fec79b, 23),           
    PrimeInfo(68513161,  0x7d605cd1, 25),           
    PrimeInfo(142069021, 0xf1da390b, 27),           
    PrimeInfo(294594427, 0x74a2507d, 27),
    PrimeInfo(733045421, 0x5dbec447, 28),
};

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
PrimeInfo SimplerHashTable<Key,KeyFuncs,Value,Behavior>::NextPrime(unsigned number)
{
    CONTRACT(PrimeInfo)
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    for (int i = 0; i < (int) (sizeof(primeInfo) / sizeof(primeInfo[0])); i++) {
        if (primeInfo[i].prime >= number)
            RETURN primeInfo[i];
    }

    // overflow
    ThrowOutOfMemory();
}

#endif // _SIMPLERHASHTABLE_INL_
