// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// SHash is a templated closed chaining hash table of pointers.  It provides
// for multiple entries under the same key, and also for deleting elements.

// Synchronization:
// Synchronization requirements depend on use.  There are several properties to take into account:
//
// - Lookups may be asynchronous with each other
// - Lookups must be exclusive with Add operations
//    (@todo: this can be remedied by delaying destruction of old tables during reallocation, e.g. during GC)
// - Remove operations may be asynchronous with Lookup/Add, unless elements are also deallocated. (In which
//    case full synchronization is required)


// A SHash is templated by a class of TRAITS.  These traits define the various specifics of the
// particular hash table.
// The required traits are:
//
// element_t                                    Type of elements in the hash table.  These elements are stored
//                                              by value in the hash table. Elements must look more or less
//                                              like primitives - they must support assignment relatively
//                                              efficiently.  There are 2 required sentinel values:
//                                              Null and Deleted (described below).  (Note that element_t is
//                                              very commonly a pointer type.)
//
//                                              The key must be derivable from the element; if your
//                                              table's keys are independent of the stored values, element_t
//                                              should be a key/value pair.
//
// key_t                                        Type of the lookup key.  The key is used for identity
//                                              comparison between elements, and also as a key for lookup.
//                                              This is also used by value and should support
//                                              efficient assignment.
//
// count_t                                      integral type for counts.  Typically inherited by default
//                                              Traits (count_t).
//
// static key_t GetKey(const element_t &e)      Get key from element.  Should be stable for a given e.
// static bool Equals(key_t k1, key_t k2)       Compare 2 keys for equality.  Again, should be stable.
// static count_t Hash(key_t k)                 Compute hash from a key.  For efficient operation, the hashes
//                                              for a set of elements should have random uniform distribution.
//
// static element_t Null()                      Return the Null sentinel value.  May be inherited from
//                                              default traits if it can be assigned from 0.
// static element_t Deleted()                   Return the Deleted sentinel value.  May be inherited from the
//                                              default traits if it can be assigned from -1.
// static bool IsNull(const ELEMENT &e)         Compare element with Null sentinel value. May be inherited from
//                                              default traits if it can be assigned from 0.
// static bool IsDeleted(const ELEMENT &e)      Compare element with Deleted sentinel value. May be inherited
//                                              from the default traits if it can be assigned from -1.
// static void OnFailure(FailureType failureType) Called when a failure occurs during SHash operation
//
// s_growth_factor_numerator
// s_growth_factor_denominator                  Factor to grow allocation (numerator/denominator).
//                                              Typically inherited from default traits (3/2)
//
// s_density_factor_numerator
// s_density_factor_denominator                 Maximum occupied density of table before growth
//                                              occurs (num/denom).  Typically inherited (3/4).
//
// s_minimum_allocation                         Minimum table allocation count (size on first growth.)  It is
//                                              probably preferable to call Reallocate on initialization rather
//                                              than override his from the default traits. (7)
//
// s_supports_remove                            Set to false for a slightly faster implementation that does not
//                                              support deletes. There is a downside to the s_supports_remove flag,
//                                              in that there may be more copies of the template instantiated through
//                                              the system as different variants are used.

#ifndef __shash_h__
#define __shash_h__

// disable the "Conditional expression is constant" warning
#pragma warning(push)
#pragma warning(disable:4127)


enum FailureType { ftAllocation, ftOverflow };

// DefaultHashTraits provides defaults for seldomly customized values in traits classes.

template < typename ELEMENT, typename COUNT_T = uint32_t >
class DefaultSHashTraits
{
  public:
    typedef COUNT_T count_t;
    typedef ELEMENT element_t;
    typedef DPTR(element_t) PTR_element_t;   // by default SHash is DAC-aware. For RS
                                             // only SHash use NonDacAwareSHashTraits
                                             // (which typedefs element_t* PTR_element_t)
    static const count_t s_growth_factor_numerator = 3;
    static const count_t s_growth_factor_denominator = 2;

    static const count_t s_density_factor_numerator = 3;
    static const count_t s_density_factor_denominator = 4;

    static const count_t s_minimum_allocation = 7;

    static const bool s_supports_remove = true;

    static const ELEMENT Null() { return (const ELEMENT) 0; }
    static const ELEMENT Deleted() { return (const ELEMENT) -1; }
    static bool IsNull(const ELEMENT &e) { return e == (const ELEMENT) 0; }
    static bool IsDeleted(const ELEMENT &e) { return e == (const ELEMENT) -1; }

    static void OnFailure(FailureType /*ft*/) { }

    // No defaults - must specify:
    //
    // typedef key_t;
    // static key_t GetKey(const element_t &i);
    // static bool Equals(key_t k1, key_t k2);
    // static count_t Hash(key_t k);
};

// Hash table class definition

template <typename TRAITS>
class SHash : public TRAITS
{
  private:
    class Index;
    friend class Index;

    class KeyIndex;
    friend class KeyIndex;
    class Iterator;
    class KeyIterator;

  public:
    // explicitly declare local typedefs for these traits types, otherwise
    // the compiler may get confused
    typedef typename TRAITS::element_t element_t;
    typedef typename TRAITS::PTR_element_t PTR_element_t;
    typedef typename TRAITS::key_t key_t;
    typedef typename TRAITS::count_t count_t;

    // Constructor/destructor.  SHash tables always start out empty, with no
    // allocation overhead.  Call Reallocate to prime with an initial size if
    // desired.

    SHash();
    ~SHash();

    // Lookup an element in the table by key.  Returns NULL if no element in the table
    // has the given key.  Note that multiple entries for the same key may be stored -
    // this will return the first element added.  Use KeyIterator to find all elements
    // with a given key.

    element_t Lookup(key_t key) const;

    // Pointer-based flavor of Lookup (allows efficient access to tables of structures)

    const element_t* LookupPtr(key_t key) const;

    // Add an element to the hash table.  This will never replace an element; multiple
    // elements may be stored with the same key.
    //
    // Returns 'true' on success, 'false' on failure.

    bool Add(const element_t &element);

    // Add a new element to the hash table, if no element with the same key is already
    // there. Otherwise, it will replace the existing element. This has the effect of
    // updating an element rather than adding a duplicate.
    //
    // Returns 'true' on success, 'false' on failure.

    bool AddOrReplace(const element_t & element);

    // Remove the first element matching the key from the hash table.

    void Remove(key_t key);

    // Remove the specific element.

    void Remove(Iterator& i);
    void Remove(KeyIterator& i);

    // Pointer-based flavor of Remove (allows efficient access to tables of structures)

    void RemovePtr(element_t * element);

    // Remove all elements in the hashtable

    void RemoveAll();

    // Begin and End pointers for iteration over entire table.

    Iterator Begin() const;
    Iterator End() const;

    // Begin and End pointers for iteration over all elements with a given key.

    KeyIterator Begin(key_t key) const;
    KeyIterator End(key_t key) const;

    // Return the number of elements currently stored in the table

    count_t GetCount() const;

    // Return the number of elements that the table is capable storing currently

    count_t GetCapacity() const;

    // Reallocates a hash table to a specific size.  The size must be big enough
    // to hold all elements in the table appropriately.
    //
    // Note that the actual table size must always be a prime number; the number
    // passed in will be upward adjusted if necessary.
    //
    // Returns 'true' on success, 'false' on failure.

    bool Reallocate(count_t newTableSize);

    // See if it is OK to grow the hash table by one element.  If not, reallocate
    // the hash table.
    //
    // Returns 'true' on success, 'false' on failure.

    bool CheckGrowth();

    // See if it is OK to grow the hash table by N elementsone element.  If not, reallocate
    // the hash table.

    bool CheckGrowth(count_t newElements);

private:

    // Resizes a hash table for growth.  The new size is computed based
    // on the current population, growth factor, and maximum density factor.
    //
    // Returns 'true' on success, 'false' on failure.

    bool Grow();

    // Utility function to add a new element to the hash table.  Note that
    // it is perfectly find for the element to be a duplicate - if so it
    // is added an additional time. Returns true if a new empty spot was used;
    // false if an existing deleted slot.

    static bool Add(element_t *table, count_t tableSize, const element_t &element);

    // Utility function to add a new element to the hash table, if no element with the same key
    // is already there. Otherwise, it will replace the existing element. This has the effect of
    // updating an element rather than adding a duplicate.

    void AddOrReplace(element_t *table, count_t tableSize, const element_t &element);

    // Utility function to find the first element with the given key in
    // the hash table.

    static const element_t* Lookup(PTR_element_t table, count_t tableSize, key_t key);

    // Utility function to remove the first element with the given key
    // in the hash table.

    void Remove(element_t *table, count_t tableSize, key_t key);

    // Utility function to remove the specific element.

    void RemoveElement(element_t *table, count_t tableSize, element_t *element);

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
        friend class SHash;
        friend class Iterator;
        friend class Enumerator<Iterator>;

        // The methods implementation has to be here for portability
        // Some compilers won't compile the separate implementation in shash.inl
      protected:

        PTR_element_t m_table;
        count_t m_tableSize;
        count_t m_index;

        Index(const SHash *hash, bool begin)
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
        friend class SHash;

      public:
        Iterator(const SHash *hash, bool begin)
          : Index(hash, begin)
        {
        }
    };

    //
    // Index for iterating elements with a given key.
    // Note that the m_index field is artificially bumped to m_tableSize when the end
    // of iteration is reached. This allows a canonical End iterator to be used.
    //

    class KeyIndex : public Index
    {
        friend class SHash;
        friend class KeyIterator;
        friend class Enumerator<KeyIterator>;

        // The methods implementation has to be here for portability
        // Some compilers won't compile the separate implementation in shash.inl
      protected:
        key_t       m_key;
        count_t     m_increment;

        KeyIndex(const SHash *hash, bool begin)
        : Index(hash, begin),
            m_increment(0)
        {
        }

        void SetKey(key_t key)
        {
            if (m_tableSize > 0)
            {
                m_key = key;
                count_t hash = Hash(key);

                TRAITS::m_index = hash % m_tableSize;
                m_increment = (hash % (m_tableSize-1)) + 1;

                // Find first valid element
                if (IsNull(m_table[TRAITS::m_index]))
                    TRAITS::m_index = m_tableSize;
                else if (IsDeleted(m_table[TRAITS::m_index])
                        || !Equals(m_key, GetKey(m_table[TRAITS::m_index])))
                    Next();
            }
        }

        void Next()
        {
            while (true)
            {
                TRAITS::m_index += m_increment;
                if (TRAITS::m_index >= m_tableSize)
                    TRAITS::m_index -= m_tableSize;

                if (IsNull(m_table[TRAITS::m_index]))
                {
                    TRAITS::m_index = m_tableSize;
                    break;
                }

                if (!IsDeleted(m_table[TRAITS::m_index])
                        && Equals(m_key, GetKey(m_table[TRAITS::m_index])))
                {
                    break;
                }
            }
        }
    };

    class KeyIterator : public KeyIndex, public Enumerator<KeyIterator>
    {
        friend class SHash;

      public:

        operator Iterator &()
        {
            return *(Iterator*)this;
        }

        operator const Iterator &()
        {
            return *(const Iterator*)this;
        }

        KeyIterator(const SHash *hash, bool begin)
          : KeyIndex(hash, begin)
        {
        }
    };

    // Test for prime number.
    static bool IsPrime(count_t number);

    // Find the next prime number >= the given value.

    static count_t NextPrime(count_t number);

    // Instance members

    PTR_element_t m_table;                // pointer to table
    count_t       m_tableSize;            // allocated size of table
    count_t       m_tableCount;           // number of elements in table
    count_t       m_tableOccupied;        // number, includes deleted slots
    count_t       m_tableMax;             // maximum occupied count before reallocating
};

// disables support for DAC marshaling. Useful for defining right-side only SHashes

template <typename PARENT>
class NonDacAwareSHashTraits : public PARENT
{
public:
    typedef typename PARENT::element_t element_t;
    typedef element_t * PTR_element_t;
};

// disables support for removing elements - produces slightly faster implementation

template <typename PARENT>
class NoRemoveSHashTraits : public PARENT
{
public:
    // explicitly declare local typedefs for these traits types, otherwise
    // the compiler may get confused
    typedef typename PARENT::element_t element_t;
    typedef typename PARENT::count_t count_t;

    static const bool s_supports_remove = false;
    static const element_t Deleted() { UNREACHABLE(); }
    static bool IsDeleted(const element_t &e) { UNREFERENCED_PARAMETER(e); return false; }
};

// PtrHashTraits is a template to provides useful defaults for pointer hash tables
// It relies on methods GetKey and Hash defined on ELEMENT

template <typename ELEMENT, typename KEY>
class PtrSHashTraits : public DefaultSHashTraits<ELEMENT *>
{
  public:

    // explicitly declare local typedefs for these traits types, otherwise
    // the compiler may get confused
    typedef DefaultSHashTraits<ELEMENT *> PARENT;
    typedef typename PARENT::element_t element_t;
    typedef typename PARENT::count_t count_t;

    typedef KEY key_t;

    static key_t GetKey(const element_t &e)
    {
        return e->GetKey();
    }
    static bool Equals(key_t k1, key_t k2)
    {
        return k1 == k2;
    }
    static count_t Hash(key_t k)
    {
        return ELEMENT::Hash(k);
    }
};

template <typename ELEMENT, typename KEY>
class PtrSHash : public SHash< PtrSHashTraits<ELEMENT, KEY> >
{
};

template <typename KEY, typename VALUE>
class KeyValuePair {
    KEY     key;
    VALUE   value;

public:
    KeyValuePair()
    {
    }

    KeyValuePair(const KEY& k, const VALUE& v)
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

template <typename KEY, typename VALUE>
class MapSHashTraits : public DefaultSHashTraits< KeyValuePair<KEY,VALUE> >
{
public:
    // explicitly declare local typedefs for these traits types, otherwise
    // the compiler may get confused
    typedef typename DefaultSHashTraits< KeyValuePair<KEY,VALUE> >::element_t element_t;
    typedef typename DefaultSHashTraits< KeyValuePair<KEY,VALUE> >::count_t count_t;

    typedef KEY key_t;

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

    static const element_t Null() { return element_t((KEY)0,(VALUE)0); }
    static bool IsNull(const element_t &e) { return e.Key() == (KEY)0; }
};

template <typename KEY, typename VALUE>
class MapSHash : public SHash< NoRemoveSHashTraits< MapSHashTraits <KEY, VALUE> > >
{
    typedef SHash< NoRemoveSHashTraits< MapSHashTraits <KEY, VALUE> > > PARENT;

public:
    void Add(KEY key, VALUE value)
    {
        PARENT::Add(KeyValuePair<KEY,VALUE>(key, value));
    }

    bool Lookup(KEY key, VALUE* pValue)
    {
        const KeyValuePair<KEY,VALUE> *pRet = PARENT::LookupPtr(key);
        if (pRet == NULL)
            return false;

        *pValue = pRet->Value();
        return true;
    }
};


// restore "Conditional expression is constant" warning to previous value
#pragma warning(pop)

#endif // __shash_h__
