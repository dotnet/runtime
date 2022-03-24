// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// disable the "Conditional expression is constant" warning
#pragma warning(disable:4127)


template <typename TRAITS>
SHash<TRAITS>::SHash()
  : m_table(nullptr),
    m_tableSize(0),
    m_tableCount(0),
    m_tableOccupied(0),
    m_tableMax(0)
{
    C_ASSERT(TRAITS::s_growth_factor_numerator > TRAITS::s_growth_factor_denominator);
    C_ASSERT(TRAITS::s_density_factor_numerator < TRAITS::s_density_factor_denominator);
}

template <typename TRAITS>
SHash<TRAITS>::~SHash()
{
    delete [] m_table;
}

template <typename TRAITS>
typename SHash<TRAITS>::count_t SHash<TRAITS>::GetCount() const
{
    return m_tableCount;
}

template <typename TRAITS>
typename SHash<TRAITS>::count_t SHash<TRAITS>::GetCapacity() const
{
    return m_tableMax;
}

template <typename TRAITS>
typename SHash< TRAITS>::element_t SHash<TRAITS>::Lookup(key_t key) const
{
    const element_t *pRet = Lookup(m_table, m_tableSize, key);
    return ((pRet != NULL) ? (*pRet) : TRAITS::Null());
}

template <typename TRAITS>
const typename SHash< TRAITS>::element_t* SHash<TRAITS>::LookupPtr(key_t key) const
{
    return Lookup(m_table, m_tableSize, key);
}

template <typename TRAITS>
bool SHash<TRAITS>::Add(const element_t &element)
{
    if (!CheckGrowth())
        return false;

    if (Add(m_table, m_tableSize, element))
        m_tableOccupied++;
    m_tableCount++;

    return true;
}

template <typename TRAITS>
bool SHash<TRAITS>::AddOrReplace(const element_t &element)
{
    if (!CheckGrowth())
        return false;

    AddOrReplace(m_table, m_tableSize, element);
    return true;
}

template <typename TRAITS>
void SHash<TRAITS>::Remove(key_t key)
{
    Remove(m_table, m_tableSize, key);
}

template <typename TRAITS>
void SHash<TRAITS>::Remove(Iterator& i)
{
    RemoveElement(m_table, m_tableSize, (element_t*)&(*i));
}

template <typename TRAITS>
void SHash<TRAITS>::Remove(KeyIterator& i)
{
    RemoveElement(m_table, m_tableSize, (element_t*)&(*i));
}

template <typename TRAITS>
void SHash<TRAITS>::RemovePtr(element_t * p)
{
    RemoveElement(m_table, m_tableSize, p);
}

template <typename TRAITS>
void SHash<TRAITS>::RemoveAll()
{
    delete [] m_table;

    m_table = NULL;
    m_tableSize = 0;
    m_tableCount = 0;
    m_tableOccupied = 0;
    m_tableMax = 0;
}

template <typename TRAITS>
typename SHash<TRAITS>::Iterator SHash<TRAITS>::Begin() const
{
    Iterator i(this, true);
    i.First();
    return i;
}

template <typename TRAITS>
typename SHash<TRAITS>::Iterator SHash<TRAITS>::End() const
{
    return Iterator(this, false);
}

template <typename TRAITS>
typename SHash<TRAITS>::KeyIterator SHash<TRAITS>::Begin(key_t key) const
{
    KeyIterator k(this, true);
    k.SetKey(key);
    return k;
}

template <typename TRAITS>
typename SHash<TRAITS>::KeyIterator SHash<TRAITS>::End(key_t key) const
{
    return KeyIterator(this, false);
}

template <typename TRAITS>
bool SHash<TRAITS>::CheckGrowth()
{
    if (m_tableOccupied == m_tableMax)
    {
        return Grow();
    }

    return true;
}

template <typename TRAITS>
bool SHash<TRAITS>::Grow()
{
    count_t newSize = (count_t) (m_tableCount
                                 * TRAITS::s_growth_factor_numerator / TRAITS::s_growth_factor_denominator
                                 * TRAITS::s_density_factor_denominator / TRAITS::s_density_factor_numerator);
    if (newSize < TRAITS::s_minimum_allocation)
        newSize = TRAITS::s_minimum_allocation;

    // handle potential overflow
    if (newSize < m_tableCount)
    {
        TRAITS::OnFailure(ftOverflow);
        return false;
    }

    return Reallocate(newSize);
}

template <typename TRAITS>
bool SHash<TRAITS>::CheckGrowth(count_t newElements)
{
    count_t newCount = (m_tableCount + newElements);

    // handle potential overflow
    if (newCount < newElements)
    {
        TRAITS::OnFailure(ftOverflow);
        return false;
    }

    // enough space in the table?
    if (newCount < m_tableMax)
        return true;

    count_t newSize = (count_t) (newCount * TRAITS::s_density_factor_denominator / TRAITS::s_density_factor_numerator) + 1;

    // handle potential overflow
    if (newSize < newCount)
    {
        TRAITS::OnFailure(ftOverflow);
        return false;
    }

    // accelerate the growth to avoid unnecessary rehashing
    count_t newSize2 = (m_tableCount * TRAITS::s_growth_factor_numerator / TRAITS::s_growth_factor_denominator
                                     * TRAITS::s_density_factor_denominator / TRAITS::s_density_factor_numerator);

    if (newSize < newSize2)
        newSize = newSize2;

    if (newSize < TRAITS::s_minimum_allocation)
        newSize = TRAITS::s_minimum_allocation;

    return Reallocate(newSize);
}

template <typename TRAITS>
bool SHash<TRAITS>::Reallocate(count_t newTableSize)
{
    ASSERT(newTableSize >=
                 (count_t) (GetCount() * TRAITS::s_density_factor_denominator / TRAITS::s_density_factor_numerator));

    // Allocation size must be a prime number.  This is necessary so that hashes uniformly
    // distribute to all indices, and so that chaining will visit all indices in the hash table.
    newTableSize = NextPrime(newTableSize);
    if (newTableSize == 0)
    {
        TRAITS::OnFailure(ftOverflow);
        return false;
    }

    element_t *newTable = new (nothrow) element_t [newTableSize];
    if (newTable == NULL)
    {
        TRAITS::OnFailure(ftAllocation);
        return false;
    }

    element_t *p = newTable, *pEnd = newTable + newTableSize;
    while (p < pEnd)
    {
        *p = TRAITS::Null();
        p++;
    }

    // Move all entries over to new table.

    for (Iterator i = Begin(), end = End(); i != end; i++)
    {
        const element_t & cur = (*i);
        if (!TRAITS::IsNull(cur) && !TRAITS::IsDeleted(cur))
            Add(newTable, newTableSize, cur);
    }

    // @todo:
    // We might want to try to delay this cleanup to allow asynchronous readers

    delete [] m_table;

    m_table = PTR_element_t(newTable);
    m_tableSize = newTableSize;
    m_tableMax = (count_t) (newTableSize * TRAITS::s_density_factor_numerator / TRAITS::s_density_factor_denominator);
    m_tableOccupied = m_tableCount;

    return true;
}

template <typename TRAITS>
const typename SHash<TRAITS>::element_t * SHash<TRAITS>::Lookup(PTR_element_t table, count_t tableSize, key_t key)
{
    if (tableSize == 0)
        return NULL;

    count_t hash = TRAITS::Hash(key);
    count_t index = hash % tableSize;
    count_t increment = 0; // delay computation

    while (true)
    {
        element_t& current = table[index];

        if (TRAITS::IsNull(current))
            return NULL;

        if (!TRAITS::IsDeleted(current)
            && TRAITS::Equals(key, TRAITS::GetKey(current)))
        {
            return &current;
        }

        if (increment == 0)
            increment = (hash % (tableSize-1)) + 1;

        index += increment;
        if (index >= tableSize)
            index -= tableSize;
    }
}

template <typename TRAITS>
bool SHash<TRAITS>::Add(element_t *table, count_t tableSize, const element_t &element)
{
    key_t key = TRAITS::GetKey(element);

    count_t hash = TRAITS::Hash(key);
    count_t index = hash % tableSize;
    count_t increment = 0; // delay computation

    while (true)
    {
        element_t& current = table[index];

        if (TRAITS::IsNull(current))
        {
            table[index] = element;
            return true;
        }

        if (TRAITS::IsDeleted(current))
        {
            table[index] = element;
            return false;
        }

        if (increment == 0)
            increment = (hash % (tableSize-1)) + 1;

        index += increment;
        if (index >= tableSize)
            index -= tableSize;
    }
}

template <typename TRAITS>
void SHash<TRAITS>::AddOrReplace(element_t *table, count_t tableSize, const element_t &element)
{
    ASSERT(!TRAITS::s_supports_remove);

    key_t key = TRAITS::GetKey(element);

    count_t hash = TRAITS::Hash(key);
    count_t index = hash % tableSize;
    count_t increment = 0; // delay computation

    while (true)
    {
        element_t& current = table[index];
        ASSERT(!TRAITS::IsDeleted(current));

        if (TRAITS::IsNull(current))
        {
            table[index] = element;
            m_tableCount++;
            m_tableOccupied++;
            return;
        }
        else if (TRAITS::Equals(key, TRAITS::GetKey(current)))
        {
            table[index] = element;
            return;
        }

        if (increment == 0)
            increment = (hash % (tableSize-1)) + 1;

        index += increment;
        if (index >= tableSize)
            index -= tableSize;
    }
}

#ifdef _MSC_VER
#pragma warning (disable: 4702) // Workaround bogus unreachable code warning
#endif
template <typename TRAITS>
void SHash<TRAITS>::Remove(element_t *table, count_t tableSize, key_t key)
{
    ASSERT(TRAITS::s_supports_remove);
    ASSERT(Lookup(table, tableSize, key) != NULL);

    count_t hash = TRAITS::Hash(key);
    count_t index = hash % tableSize;
    count_t increment = 0; // delay computation

    while (true)
    {
        element_t& current = table[index];

        if (TRAITS::IsNull(current))
            return;

        if (!TRAITS::IsDeleted(current)
            && TRAITS::Equals(key, TRAITS::GetKey(current)))
        {
            table[index] = TRAITS::Deleted();
      	    m_tableCount--;
            return;
        }

        if (increment == 0)
            increment = (hash % (tableSize-1)) + 1;

        index += increment;
        if (index >= tableSize)
            index -= tableSize;
    }
}
#ifdef _MSC_VER
#pragma warning (default: 4702)
#endif

template <typename TRAITS>
void SHash<TRAITS>::RemoveElement(element_t *table, count_t tableSize, element_t *element)
{
    ASSERT(TRAITS::s_supports_remove);
    ASSERT(table <= element && element < table + tableSize);
    ASSERT(!TRAITS::IsNull(*element) && !TRAITS::IsDeleted(*element));

    *element = TRAITS::Deleted();
    m_tableCount--;
}

template <typename TRAITS>
bool SHash<TRAITS>::IsPrime(count_t number)
{
    // This is a very low-tech check for primality, which doesn't scale very well.
    // There are more efficient tests if this proves to be burdensome for larger
    // tables.

    if ((number&1) == 0)
        return false;

    count_t factor = 3;
    while (factor * factor <= number)
    {
        if ((number % factor) == 0)
            return false;
        factor += 2;
    }

    return true;
}

namespace
{
    const uint32_t g_shash_primes[] = {
        11,17,23,29,37,47,59,71,89,107,131,163,197,239,293,353,431,521,631,761,919,
        1103,1327,1597,1931,2333,2801,3371,4049,4861,5839,7013,8419,10103,12143,14591,
        17519,21023,25229,30293,36353,43627,52361,62851,75431,90523, 108631, 130363,
        156437, 187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403,
        968897, 1162687, 1395263, 1674319, 2009191, 2411033, 2893249, 3471899, 4166287,
        4999559, 5999471, 7199369 };
}


// Returns a prime larger than 'number' or 0, in case of overflow
template <typename TRAITS>
typename SHash<TRAITS>::count_t SHash<TRAITS>::NextPrime(typename SHash<TRAITS>::count_t number)
{
    for (int i = 0; i < (int) (sizeof(g_shash_primes) / sizeof(g_shash_primes[0])); i++)
    {
        if (g_shash_primes[i] >= number)
            return (typename SHash<TRAITS>::count_t)(g_shash_primes[i]);
    }

    if ((number&1) == 0)
        number++;

    while (number != 1)
    {
        if (IsPrime(number))
            return number;
        number += 2;
    }

    return 0;
}

// restore "Conditional expression is constant" warning to default value
#pragma warning(default:4127)

