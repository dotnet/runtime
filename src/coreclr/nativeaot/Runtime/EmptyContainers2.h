// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __EmptyContainers2_h__
#define __EmptyContainers2_h__

// This header file will contains minimal containers that are needed for EventPipe library implementation
// shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
// @TODO - this will likely be replaced by the common container classes to be added to the EventPipe library
// Hence initially, the bare boned implementation focus is on unblocking HW/Simple Trace bring up

// TODO: Should shash.h FailureType be used instead?
enum FailureType_EP { ftAllocation_EP, ftOverflow_EP };

template < typename ELEMENT, typename COUNT_T = uint32_t  >
class DefaultSHashTraits_EP
{
public:
    typedef COUNT_T count_t;
    typedef ELEMENT element_t;
    typedef element_t* PTR_element_t;

    static const count_t s_growth_factor_numerator = 3;
    static const count_t s_growth_factor_denominator = 2;

    static const count_t s_density_factor_numerator = 3;
    static const count_t s_density_factor_denominator = 4;

    static const count_t s_minimum_allocation = 7;

    static const bool s_NoThrow = true;

    static const ELEMENT Null() { return (const ELEMENT) 0; }
    static const ELEMENT Deleted() { return (const ELEMENT) -1; }
    static bool IsNull(const ELEMENT &e) { return e == (const ELEMENT) 0; }
    static bool IsDeleted(const ELEMENT &e) 
    { 
        //PalDebugBreak();
        return false;
        //return e == (const ELEMENT) -1;
    }

    static void OnFailure(FailureType_EP /*ft*/) { }

};

template <typename TRAITS>
class SHash_EP : public TRAITS
{
  public:
    class Index;
    friend class Index;
    class Iterator;

    typedef typename TRAITS::element_t element_t;
    typedef typename TRAITS::PTR_element_t PTR_element_t;
    typedef typename TRAITS::key_t key_t;
    typedef typename TRAITS::count_t count_t;

    SHash_EP():m_table(nullptr),
    m_tableSize(0),
    m_tableCount(0),
    m_tableOccupied(0),
    m_tableMax(0)
    {        
    }

    ~SHash_EP()
    {
        delete [] m_table;
    }

    const element_t * Lookup(element_t* table, count_t tableSize, key_t key) const
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

    const element_t* LookupPtr(key_t key) const
    {
        return Lookup(m_table, m_tableSize, key);        
    }

    bool Add(const element_t &element)
    {
        if (!CheckGrowth())
            return false;

        if (Add(m_table, m_tableSize, element))
            m_tableOccupied++;
        m_tableCount++;

        return true;
    }

    bool CheckGrowth()
    {
        if (m_tableOccupied == m_tableMax)
        {
            return Grow();
        }

        return true;
    }

    bool Reallocate(count_t newTableSize)
    {
        // ASSERT(newTableSize >=
        //             (count_t) (GetCount() * TRAITS::s_density_factor_denominator / TRAITS::s_density_factor_numerator));

        // Allocation size must be a prime number.  This is necessary so that hashes uniformly
        // distribute to all indices, and so that chaining will visit all indices in the hash table.
        newTableSize = NextPrime(newTableSize);
        if (newTableSize == 0)
        {
            TRAITS::OnFailure(ftOverflow_EP);
            return false;
        }

        element_t *newTable = new (nothrow) element_t [newTableSize];
        if (newTable == NULL)
        {
            TRAITS::OnFailure(ftAllocation_EP);
            return false;
        }

        element_t *p = newTable, *pEnd = newTable + newTableSize;
        while (p < pEnd)
        {
            *p = TRAITS::Null();
            p++;
        }

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

    //
    // Enumerator, provides a template to produce an iterator on an existing class
    // with a single iteration variable.
    //

    template <typename SUBTYPE>
    class Enumerator
    {
     private:
        const SUBTYPE *This() const
        {
            return (const SUBTYPE *) this;
        }

        SUBTYPE *This()
        {
            return (SUBTYPE *)this;
        }

      public:

        Enumerator()
        {
        }

        const element_t &operator*() const
        {
            return This()->Get();
        }
        const element_t *operator->() const
        {
            return &(This()->Get());
        }
        SUBTYPE &operator++()
        {
            This()->Next();
            return *This();
        }
        SUBTYPE operator++(int)
        {
            SUBTYPE i = *This();
            This()->Next();
            return i;
        }
        bool operator==(const SUBTYPE &i) const
        {
            return This()->Equal(i);
        }
        bool operator!=(const SUBTYPE &i) const
        {
            return !This()->Equal(i);
        }
    };

    //
    // Index for whole table iterator.  This is also the base for the keyed iterator.
    //

    class Index
    {
        friend class SHash_EP;
        friend class Iterator;
        friend class Enumerator<Iterator>;

        // The methods implementation has to be here for portability
        // Some compilers won't compile the separate implementation in shash.inl
      protected:

        PTR_element_t m_table;
        count_t m_tableSize;
        count_t m_index;

        Index(const SHash_EP *hash, bool begin)
        : m_table(hash->m_table),
            m_tableSize(hash->m_tableSize),
            m_index(begin ? 0 : m_tableSize)
        {
        }

        const element_t &Get() const
        {
            return m_table[m_index];
        }

        void First()
        {
            if (m_index < m_tableSize)
                if (TRAITS::IsNull(m_table[m_index]) || TRAITS::IsDeleted(m_table[m_index]))
                    Next();
        }

        void Next()
        {
            if (m_index >= m_tableSize)
                return;

            for (;;)
            {
                m_index++;
                if (m_index >= m_tableSize)
                    break;
                if (!TRAITS::IsNull(m_table[m_index]) && !TRAITS::IsDeleted(m_table[m_index]))
                    break;
            }
        }

        bool Equal(const Index &i) const
        {
            return i.m_index == m_index;
        }
    };


    class Iterator : public Index, public Enumerator<Iterator>
    {
        friend class SHash_EP;
        TRAITS* _t;

    public:
        Iterator(const SHash_EP *hash, bool begin)
          : Index(hash, begin)
        {
        }
    };

    Iterator Begin() const
    {
        Iterator i(this, true);
        i.First();
        return i;
    }
    Iterator End() const
    {
        return Iterator(this, false);
    }
    
    bool AddNoThrow(const element_t &element)
    {
        //PalDebugBreak();
        return false;
    }
    bool AddOrReplaceNoThrow(const element_t &element)
    {
        //PalDebugBreak();
        return false;
    }

    count_t GetCount() const
    {
        return m_tableCount;
    }

    void Remove(key_t key)
    {
        //PalDebugBreak();
    }

    // Remove the specific element.
    void Remove(Iterator& i)
    {
        //PalDebugBreak();
    }
    void RemoveAll()
    {
        delete [] m_table;

        m_table = NULL;
        m_tableSize = 0;
        m_tableCount = 0;
        m_tableOccupied = 0;
        m_tableMax = 0;
    }

    // Instance members
  private:
    PTR_element_t  m_table;                // pointer to table
    count_t       m_tableSize;            // allocated size of table
    count_t       m_tableCount;           // number of elements in table
    count_t       m_tableOccupied;        // number, includes deleted slots
    count_t       m_tableMax;             // maximum occupied count before reallocating

    bool Grow()
    {
        count_t newSize = (count_t) (m_tableCount
                                    * TRAITS::s_growth_factor_numerator / TRAITS::s_growth_factor_denominator
                                    * TRAITS::s_density_factor_denominator / TRAITS::s_density_factor_numerator);
        if (newSize < TRAITS::s_minimum_allocation)
            newSize = TRAITS::s_minimum_allocation;

        // handle potential overflow
        if (newSize < m_tableCount)
        {
            TRAITS::OnFailure(ftOverflow_EP);
            return false;
        }

        return Reallocate(newSize);
    }

    static bool IsPrime(count_t number)
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

    static constexpr uint32_t g_shash_primes[] = {
        11,17,23,29,37,47,59,71,89,107,131,163,197,239,293,353,431,521,631,761,919,
        1103,1327,1597,1931,2333,2801,3371,4049,4861,5839,7013,8419,10103,12143,14591,
        17519,21023,25229,30293,36353,43627,52361,62851,75431,90523, 108631, 130363,
        156437, 187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403,
        968897, 1162687, 1395263, 1674319, 2009191, 2411033, 2893249, 3471899, 4166287,
        4999559, 5999471, 7199369 };

    // Returns a prime larger than 'number' or 0, in case of overflow
    static count_t NextPrime(count_t number)
    {
        for (int i = 0; i < (int) (sizeof(g_shash_primes) / sizeof(g_shash_primes[0])); i++)
        {
            if (g_shash_primes[i] >= number)
                return (typename SHash_EP<TRAITS>::count_t)(g_shash_primes[i]);
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

    static bool Add(element_t *table, count_t tableSize, const element_t &element)
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

};

template <typename KEY, typename VALUE>
class KeyValuePair_EP{
    KEY     key;
    VALUE   value;

public:
    KeyValuePair_EP()
    {
    }

    KeyValuePair_EP(const KEY& k, const VALUE& v)
        : key(k), value(v)
    {
    }

    KEY const & Key() const
    {
        return key;
    }

    VALUE const & Value() const
    {
        return value;
    }
};

template <typename PARENT>
class NoRemoveSHashTraits_EP : public PARENT
{
public:
    typedef typename PARENT::element_t element_t;
    typedef typename PARENT::count_t count_t;
};

template <typename KEY, typename VALUE>
class MapSHashTraits_EP : public DefaultSHashTraits_EP< KeyValuePair_EP<KEY,VALUE> >
{
public:
    typedef typename DefaultSHashTraits_EP< KeyValuePair_EP<KEY,VALUE> >::element_t element_t;
    typedef typename DefaultSHashTraits_EP< KeyValuePair_EP<KEY,VALUE> >::count_t count_t;

    typedef KEY key_t;
    typedef VALUE value_t;

    static key_t GetKey(element_t e)
    {
        return e.Key();
    }
    static bool Equals(key_t k1, key_t k2)
    {
        return k1 == k2;
    }
    static count_t Hash(key_t k)
    {
        return (count_t)(size_t)k;
    }

    static const element_t Null() 
    { 
        return element_t(KEY(),VALUE());
    }
    static bool IsNull(const element_t &e) 
    { 
        return e.Key() == KEY();
    }
    static bool IsDeleted(const element_t &e) 
    { 
        return e.Key() == KEY(-1); 
    }

    key_t const & Key() const
    {
        //PalDebugBreak();
        return *(new KEY());
    }

    value_t const & Value() const
    {
        //PalDebugBreak();
        return *(new VALUE());
    }
};


#endif // __EmptyContainers2_h__
