// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef _SHASH_H_
#define _SHASH_H_

#include "utilcode.h" // for string hash functions
#include "clrtypes.h"
#include "check.h"
#include "iterator.h"

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

// Common "gotchas":
// - The Add method never replaces an element. The new element will be added even if an element with the same
//   key is already present. If you don't want this, use AddOrReplace.
// - You need special sentinel values for the element to represent Null and Deleted. 0 and -1 are the default
//   choices but you will need something else if elements can legally have any of these two values.
// - Deriving directly from the general purpose classes (such as SHash itself) requires implementing a
//   TRAITS class which can be daunting. Consider using one of the specialized classes (e.g. WStringSHash) first.

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
//                                              Traits (COUNT_T).
//
// static key_t GetKey(const element_t &e)      Get key from element.  Should be stable for a given e.
// static BOOL Equals(key_t k1, key_t k2)       Compare 2 keys for equality.  Again, should be stable.
// static count_t Hash(key_t k)                 Compute hash from a key.  For efficient operation, the hashes
//                                              for a set of elements should have random uniform distribution.
//
// static const bool s_NoThrow                  TRUE if GetKey, Equals, and hash are NOTHROW functions.
//                                              Affects the THROWS clauses of several SHash functions.
//                                              (Note that the Null- and Deleted-related functions below
//                                              are not affected by this and must always be NOTHROW.)
//
// static element_t Null()                      Return the Null sentinal value.  May be inherited from
//                                              default traits if it can be assigned from 0.
// static element_t Deleted()                   Return the Deleted sentinal value.  May be inherited from the
//                                              default traits if it can be assigned from -1.
// static const bool IsNull(const ELEMENT &e)   Compare element with Null sentinal value. May be inherited from
//                                              default traits if it can be assigned from 0.
// static const bool IsDeleted(const ELEMENT &e) Compare element with Deleted sentinal value. May be inherited from the
//                                              default traits if it can be assigned from -1.
// static bool ShouldDelete(const ELEMENT &e)   Called in addition to IsDeleted() when s_supports_autoremove is true, see more
//                                              information there.
//
// static void OnDestructPerEntryCleanupAction(ELEMENT& e) Called on every element when in hashtable destructor.
//                                              s_DestructPerEntryCleanupAction must be set to true if implemented.
// static void OnRemovePerEntryCleanupAction(ELEMENT& e) Called when an element is removed from the hashtable, including when
//                                              the hashtable is destructed. s_RemovePerEntryCleanupAction must be set to true
//                                              if implemented.
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
// s_supports_remove                            Set to false for a slightly faster implementation that does not
//                                              support deletes. There is a downside to the s_supports_remove flag,
//                                              in that there may be more copies of the template instantiated through
//                                              the system as different variants are used.
//
// s_supports_autoremove                        When autoremove is supported, ShouldDelete() is called in addition to
//                                              IsDeleted() in any situation that involves walking the table's elements, to
//                                              determine if an element should be deleted. It enables the hash table to
//                                              self-clean elements whose underlying lifetime may be controlled externally. Note
//                                              that since some lookup/iteration operations are const (can operate on a
//                                              "const SHash"), when autoremove is supported, any such const operation may still
//                                              modify the hash table. If this is set to true, s_supports_remove must also be
//                                              true.
//
// s_DestructPerEntryCleanupAction              Set to true if OnDestructPerEntryCleanupAction has non-empty implementation.
// s_RemovePerEntryCleanupAction                Set to true if OnRemovePerEntryCleanupAction has non-empty implementation.
//                                              Only one of s_DestructPerEntryCleanupAction and s_RemovePerEntryCleanupAction
//                                              may be set to true.
//
// DefaultHashTraits provides defaults for seldomly customized values in traits classes.

template < typename ELEMENT >
class DefaultSHashTraits
{
  public:
    typedef COUNT_T count_t;
    typedef ELEMENT element_t;
    typedef DPTR(element_t) PTR_element_t;   // by default SHash is DAC-aware. For RS
                                             // only SHash use NonDacAwareSHashTraits
                                             // (which typedefs element_t* PTR_element_t)

    static const COUNT_T s_growth_factor_numerator = 3;
    static const COUNT_T s_growth_factor_denominator = 2;

    static const COUNT_T s_density_factor_numerator = 3;
    static const COUNT_T s_density_factor_denominator = 4;

    static const COUNT_T s_minimum_allocation = 7;

    static const bool s_supports_remove = true;
    static const bool s_supports_autoremove = false;

    static ELEMENT Null() { return (ELEMENT)(TADDR)0; }
    static ELEMENT Deleted() { return (ELEMENT)(TADDR)-1; }
    static bool IsNull(const ELEMENT &e) { return e == (ELEMENT)(TADDR)0; }
    static bool IsDeleted(const ELEMENT &e) { return e == (ELEMENT)(TADDR)-1; }
    static bool ShouldDelete(const ELEMENT &e) { return false; }

    static inline void OnDestructPerEntryCleanupAction(const ELEMENT& e) { /* Do nothing */ }
    static const bool s_DestructPerEntryCleanupAction = false;

    static void OnRemovePerEntryCleanupAction(const ELEMENT &e) {}
    static const bool s_RemovePerEntryCleanupAction = false;

    static const bool s_NoThrow = true;

    // No defaults - must specify:
    //
    // typedef key_t;
    // static key_t GetKey(const element_t &i);
    // static BOOL Equals(key_t k1, key_t k2);
    // static count_t Hash(key_t k);
};

// Hash table class definition

template <typename TRAITS>
class EMPTY_BASES_DECL SHash : public TRAITS
                             , private noncopyable
{
    friend class VerifyLayoutsMD;  // verifies class layout doesn't accidentally change

  public:
    // explicitly declare local typedefs for these traits types, otherwise
    // the compiler may get confused
    typedef typename TRAITS::element_t element_t;
    typedef typename TRAITS::PTR_element_t PTR_element_t;
    typedef typename TRAITS::key_t key_t;
    typedef typename TRAITS::count_t count_t;

    class Index;
    friend class Index;
    class Iterator;

    class KeyIndex;
    friend class KeyIndex;
    class KeyIterator;

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

    // Pointer-based flavor to replace an existing element (allows efficient access to tables of structures)

    void ReplacePtr(const element_t *elementPtr, const element_t &newElement, bool invokeCleanupAction = true);

    // Add an element to the hash table.  This will never replace an element; multiple
    // elements may be stored with the same key.

    void Add(const element_t &element);

    // NoThrow version of Add. Returns TRUE if element was added, FALSE otherwise.
    BOOL AddNoThrow(const element_t &element);

    // Add a new element to the hash table, if no element with the same key is already
    // there. Otherwise, it will replace the existing element. This has the effect of
    // updating an element rather than adding a duplicate.
    void AddOrReplace(const element_t & element);

    // NoThrow version of AddOrReplace. Returns TRUE if element was added/replaced, FALSE otherwise.
    BOOL AddOrReplaceNoThrow(const element_t &element);

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

    // Return the number of elements allocated in the table

    count_t GetCapacity() const;

    // Resizes a hash table for growth.  The new size is computed based
    // on the current population, growth factor, and maximum density factor.

    void Grow();

    // Reallocates a hash table to a specific size.  The size must be big enough
    // to hold all elements in the table appropriately.
    //
    // Note that the actual table size must always be a prime number; the number
    // passed in will be upward adjusted if necessary.

    void Reallocate(count_t newTableSize);

    // Makes a call on the Functor for each element in the hash table, passing
    // the element as an argument. Functor is expected to look like this:
    //
    // class Functor
    // {
    //     public:
    //     void operator() (element_t &element) { ... }
    // }

    template<typename Functor> void ForEach(Functor &functor);

  private:

    // NoThrow version of Grow function. Returns FALSE on failure.
    BOOL GrowNoThrow();

    // See if it is OK to grow the hash table by one element.  If not, reallocate
    // the hash table.
    BOOL CheckGrowth();

    // NoThrow version of CheckGrowth function. Returns FALSE on failure.
    BOOL CheckGrowthNoThrow();

    // Allocates new resized hash table for growth. Does not update the hash table on the object.
    // The new size is computed based on the current population, growth factor, and maximum density factor.
    element_t * Grow_OnlyAllocateNewTable(count_t * pcNewSize);

    // NoThrow version of Grow_OnlyAllocateNewTable. Returns NULL on failure.
    element_t * Grow_OnlyAllocateNewTableNoThrow(count_t * pcNewSize);

    // Utility function to allocate new table (does not copy the values into it yet). Returns the size of new table in
    // *pcNewTableSize (finds next prime).
    // Phase 1 of code:Reallocate
    element_t * AllocateNewTable(count_t requestedSize, count_t * pcNewTableSize);

    // NoThrow version of AllocateNewTable utility function. Returns NULL on failure.
    element_t * AllocateNewTableNoThrow(count_t requestedSize, count_t * pcNewTableSize);

    // Utility function to replace old table with newly allocated table (as allocated by
    // code:AllocateNewTable). Copies all 'old' values into the new table first.
    // Returns the old table. Caller is expected to delete it (via code:DeleteOldTable).
    // Phase 2 of code:Reallocate
    element_t * ReplaceTable(element_t * newTable, count_t newTableSize);

    // Utility function to delete old table (as returned by code:ReplaceTable).
    // Phase 3 of code:Reallocate
    void DeleteOldTable(element_t * oldTable);

    // Utility function that does not call code:CheckGrowth.
    // Add an element to the hash table.  This will never replace an element; multiple
    // elements may be stored with the same key.
    void Add_GrowthChecked(const element_t & element);

    // Utility function to add a new element to the hash table.  Note that
    // it is perfectly fine for the element to be a duplicate - if so it
    // is added an additional time. Returns TRUE if a new empty spot was used;
    // FALSE if an existing deleted slot.
    BOOL Add(element_t *table, count_t tableSize, const element_t &element);

    // Utility function to add a new element to the hash table, if no element with the same key
    // is already there. Otherwise, it will replace the existing element. This has the effect of
    // updating an element rather than adding a duplicate.
    void AddOrReplace(element_t *table, count_t tableSize, const element_t &element);

    // Utility function to find the first element with the given key in
    // the hash table.

    const element_t* Lookup(PTR_element_t table, count_t tableSize, key_t key) const;

    // Utility function to remove the first element with the given key
    // in the hash table.

    void Remove(element_t *table, count_t tableSize, key_t key);

    // Utility function to remove the specific element.

    void RemoveElement(element_t *table, count_t tableSize, element_t *element);

    // Index for whole table iterator.  This is also the base for the keyed iterator.

  public:

    class EMPTY_BASES_DECL Index
        : public CheckedIteratorBase< SHash<TRAITS> >
    {
        friend class SHash;
        friend class Iterator;
        friend class Enumerator<const element_t, Iterator>;

        // The methods implementation has to be here for portability
        // Some compilers won't compile the separate implementation in shash.inl
      protected:

        SHash *m_hash;
        PTR_element_t m_table;
        count_t m_tableSize;
        count_t m_index;

        // Iteration may modify the hash table if s_supports_autoremove is true. Since it typically does not modify the hash
        // table, it should be possible to iterate over a "const SHash". So this takes a "const SHash *" and casts away the
        // const to support autoremove.
        Index(const SHash *hash, BOOL begin)
        : m_hash(const_cast<SHash *>(hash)),
            m_table(hash->m_table),
            m_tableSize(hash->m_tableSize),
            m_index(begin ? 0 : m_tableSize)
        {
            LIMITED_METHOD_CONTRACT;
        }

        const element_t &Get() const
        {
            LIMITED_METHOD_CONTRACT;

            return m_table[m_index];
        }

        void First()
        {
            LIMITED_METHOD_CONTRACT;

            if (m_index >= m_tableSize)
            {
                return;
            }

            if (!TRAITS::IsNull(m_table[m_index]) && !TRAITS::IsDeleted(m_table[m_index]))
            {
                if (TRAITS::s_supports_autoremove && TRAITS::ShouldDelete(m_table[m_index]))
                {
                    m_hash->RemoveElement(m_table, m_tableSize, &m_table[m_index]);
                }
                else
                {
                    return;
                }
            }

            Next();
        }

        void Next()
        {
            LIMITED_METHOD_CONTRACT;

            if (m_index >= m_tableSize)
                return;

            for (;;)
            {
                m_index++;
                if (m_index >= m_tableSize)
                    break;
                if (!TRAITS::IsNull(m_table[m_index]) && !TRAITS::IsDeleted(m_table[m_index]))
                {
                    if (TRAITS::s_supports_autoremove && TRAITS::ShouldDelete(m_table[m_index]))
                    {
                        m_hash->RemoveElement(m_table, m_tableSize, &m_table[m_index]);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        bool Equal(const Index &i) const
        {
            LIMITED_METHOD_CONTRACT;

            return i.m_index == m_index;
        }

        CHECK DoCheck() const
        {
            CHECK_OK;
        }
    };

    class EMPTY_BASES_DECL Iterator : public Index, public Enumerator<const element_t, Iterator>
    {
        friend class SHash;

      public:
        Iterator(const SHash *hash, BOOL begin)
          : Index(hash, begin)
        {
        }
    };

    // Index for iterating elements with a given key.
    //
    // Note that the m_index field
    // is artificially bumped to m_tableSize when the end of iteration is reached.
    // This allows a canonical End iterator to be used.

    class EMPTY_BASES_DECL KeyIndex : public Index
    {
        friend class SHash;
        friend class KeyIterator;
        friend class Enumerator<const element_t, KeyIterator>;

        // The methods implementation has to be here for portability
        // Some compilers won't compile the separate implementation in shash.inl
      protected:
        key_t       m_key;
        count_t     m_increment;

        KeyIndex(const SHash *hash, BOOL begin)
        : Index(hash, begin),
            m_increment(0)
        {
            LIMITED_METHOD_CONTRACT;
        }

        void SetKey(key_t key)
        {
            LIMITED_METHOD_CONTRACT;

            if (Index::m_tableSize > 0)
            {
                m_key = key;
                count_t hash = TRAITS::Hash(key);

                this->m_index = hash % this->m_tableSize;
                m_increment = (hash % (this->m_tableSize-1)) + 1;

                // Find first valid element

                if (TRAITS::IsNull(this->m_table[this->m_index]))
                {
                    this->m_index = this->m_tableSize;
                    return;
                }

                if (!TRAITS::IsDeleted(this->m_table[this->m_index]))
                {
                    if (TRAITS::s_supports_autoremove && TRAITS::ShouldDelete(this->m_table[this->m_index]))
                    {
                        this->m_hash->RemoveElement(this->m_table, this->m_tableSize, &this->m_table[this->m_index]);
                    }
                    else if (TRAITS::Equals(m_key, TRAITS::GetKey(this->m_table[this->m_index])))
                    {
                        return;
                    }
                }

                Next();
            }
        }

        void Next()
        {
            LIMITED_METHOD_CONTRACT;

            while (TRUE)
            {
                this->m_index += m_increment;
                if (this->m_index >= this->m_tableSize)
                    this->m_index -= this->m_tableSize;

                if (TRAITS::IsNull(this->m_table[this->m_index]))
                {
                    this->m_index = this->m_tableSize;
                    break;
                }

                if (TRAITS::IsDeleted(this->m_table[this->m_index]))
                {
                    continue;
                }

                if (TRAITS::s_supports_autoremove && TRAITS::ShouldDelete(this->m_table[this->m_index]))
                {
                    this->m_hash->RemoveElement(this->m_table, this->m_tableSize, &this->m_table[this->m_index]);
                    continue;
                }

                if (TRAITS::Equals(m_key, TRAITS::GetKey(this->m_table[this->m_index])))
                {
                    break;
                }
            }
        }
    };

    class EMPTY_BASES_DECL KeyIterator : public KeyIndex, public Enumerator<const element_t, KeyIterator>
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

        KeyIterator(const SHash *hash, BOOL begin)
          : KeyIndex(hash, begin)
        {
        }
    };


  private:

    // Test for prime number.
    static BOOL IsPrime(COUNT_T number);

    // Find the next prime number >= the given value.

    static COUNT_T NextPrime(COUNT_T number);

    // Instance members

    PTR_element_t m_table;                // pointer to table
    count_t       m_tableSize;            // allocated size of table
    count_t       m_tableCount;           // number of elements in table
    count_t       m_tableOccupied;        // number, includes deleted slots
    count_t       m_tableMax;             // maximum occupied count before reallocating
};  // class SHash

// disables support for DAC marshaling. Useful for defining right-side only SHashes
template <typename PARENT>
class EMPTY_BASES_DECL NonDacAwareSHashTraits : public PARENT
{
public:
    typedef typename PARENT::element_t element_t;
    typedef element_t * PTR_element_t;
};

// disables support for removing elements - produces slightly faster implementation

template <typename PARENT>
class EMPTY_BASES_DECL NoRemoveSHashTraits : public PARENT
{
public:
    // explicitly declare local typedefs for these traits types, otherwise
    // the compiler may get confused
    typedef typename PARENT::element_t element_t;
    typedef typename PARENT::count_t count_t;

    static const bool s_supports_remove = false;
    static element_t Deleted() { UNREACHABLE(); }
    static bool IsDeleted(const element_t &e) { LIMITED_METHOD_DAC_CONTRACT; return false; }
};

// PtrHashTraits is a template to provides useful defaults for pointer hash tables
// It relies on methods GetKey and Hash defined on ELEMENT

template <typename ELEMENT, typename KEY>
class EMPTY_BASES_DECL PtrSHashTraits : public DefaultSHashTraits<ELEMENT *>
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
        WRAPPER_NO_CONTRACT;
        return e->GetKey();
    }
    static BOOL Equals(key_t k1, key_t k2)
    {
        LIMITED_METHOD_CONTRACT;
        return k1 == k2;
    }
    static count_t Hash(key_t k)
    {
        WRAPPER_NO_CONTRACT;
        return ELEMENT::Hash(k);
    }
};

template <typename ELEMENT, typename KEY>
class EMPTY_BASES_DECL PtrSHash : public SHash< PtrSHashTraits<ELEMENT, KEY> >
{
};

template <typename ELEMENT, typename KEY>
class PtrSHashWithCleanupTraits
    : public PtrSHashTraits<ELEMENT, KEY>
{
public:
    void OnDestructPerEntryCleanupAction(ELEMENT * elem)
    {
        delete elem;
    }
    static const bool s_DestructPerEntryCleanupAction = true;
};

// a class that automatically deletes data referenced by the pointers (so effectively it takes ownership of the data)
// since I was too lazy to implement Remove() APIs properly, removing entries is disallowed
template <typename ELEMENT, typename KEY>
class EMPTY_BASES_DECL PtrSHashWithCleanup : public SHash< NoRemoveSHashTraits< PtrSHashWithCleanupTraits<ELEMENT, KEY> > >
{
};

// Provides case-sensitive comparison and hashing functionality through static
// and functor object methods. Can be instantiated with CHAR or WCHAR.
template <typename CharT>
struct CaseSensitiveStringCompareHash
{
private:
    typedef CharT const * str_t;

    static size_t _strcmp(CHAR const *left, CHAR const *right)
    {
        return ::strcmp(left, right);
    }

    static size_t _strcmp(WCHAR const *left, WCHAR const *right)
    {
        return ::wcscmp(left, right);
    }

    static size_t _hash(CHAR const *str)
    {
        return HashStringA(str);
    }

    static size_t _hash(WCHAR const *str)
    {
        return HashString(str);
    }

public:
    static size_t compare(str_t left, str_t right)
    {
        return _strcmp(left, right);
    }

    size_t operator()(str_t left, str_t right)
    {
        return compare(left, right);
    }

    static size_t hash(str_t str)
    {
        return _hash(str);
    }

    size_t operator()(str_t str)
    {
        return hash(str);
    }
};

// Provides case-insensitive comparison and hashing functionality through static
// and functor object methods. Can be instantiated with CHAR or WCHAR.
template <typename CharT>
struct CaseInsensitiveStringCompareHash
{
private:
    typedef CharT const * str_t;

    static size_t _strcmp(str_t left, str_t right)
    {
        return ::SString::_tstricmp(left, right);
    }

    static size_t _hash(CHAR const *str)
    {
        return HashiStringA(str);
    }

    static size_t _hash(WCHAR const *str)
    {
        return HashiString(str);
    }

public:
    static size_t compare(str_t left, str_t right)
    {
        return _strcmp(left, right);
    }

    size_t operator()(str_t left, str_t right)
    {
        return compare(left, right);
    }

    static size_t hash(str_t str)
    {
        return _hash(str);
    }

    size_t operator()(str_t str)
    {
        return hash(str);
    }
};

// StringSHashTraits is a traits class useful for string-keyed
// pointer hash tables.

template <typename ElementT, typename CharT, typename ComparerT = CaseSensitiveStringCompareHash<CharT> >
class EMPTY_BASES_DECL StringSHashTraits : public PtrSHashTraits<ElementT, CharT const *>
{
public:
    // explicitly declare local typedefs for these traits types, otherwise
    // the compiler may get confused
    typedef PtrSHashTraits<ElementT, CharT const *> PARENT;
    typedef typename PARENT::element_t element_t;
    typedef typename PARENT::key_t key_t;
    typedef typename PARENT::count_t count_t;

    static BOOL Equals(key_t k1, key_t k2)
    {
        LIMITED_METHOD_CONTRACT;

        if (k1 == NULL && k2 == NULL)
            return TRUE;
        if (k1 == NULL || k2 == NULL)
            return FALSE;
        return ComparerT::compare(k1, k2) == 0;
    }
    static count_t Hash(key_t k)
    {
        LIMITED_METHOD_CONTRACT;

        if (k == NULL)
            return 0;
        else
            return (count_t)ComparerT::hash(k);
    }
};

template <typename COMINTERFACE, typename CharT> // Could use IUnknown but would rather take advantage of C++ type checking
struct StringHashElement
{
    const CharT *GetKey()
    {
        return String;
    }

    COMINTERFACE *Object;
    CharT *String;
};

template <typename COMINTERFACE, typename CharT, typename ComparerT = CaseSensitiveStringCompareHash<CharT> >
class EMPTY_BASES_DECL StringHashWithCleanupTraits : public StringSHashTraits<StringHashElement<COMINTERFACE, CharT>, CharT, ComparerT>
{
public:
    void OnDestructPerEntryCleanupAction(StringHashElement<COMINTERFACE, CharT> * e)
    {
        if (e->String != NULL)
        {
            delete[] e->String;
        }

        if (e->Object != NULL)
        {
            e->Object->Release();
        }
    }
    static const bool s_DestructPerEntryCleanupAction = true;
};

template <typename COMINTERFACE, typename CharT, typename ComparerT = CaseSensitiveStringCompareHash<CharT> >
class EMPTY_BASES_DECL StringSHashWithCleanup : public SHash< StringHashWithCleanupTraits<COMINTERFACE, CharT, ComparerT> >
{
};

template <typename ELEMENT>
class EMPTY_BASES_DECL StringSHash : public SHash< StringSHashTraits<ELEMENT, CHAR> >
{
};

template <typename ELEMENT>
class EMPTY_BASES_DECL WStringSHash : public SHash< StringSHashTraits<ELEMENT, WCHAR> >
{
};

template <typename ELEMENT>
class EMPTY_BASES_DECL SStringSHashTraits : public PtrSHashTraits<ELEMENT, SString>
{
  public:
    typedef PtrSHashTraits<ELEMENT, SString> PARENT;
    typedef typename PARENT::element_t element_t;
    typedef typename PARENT::key_t key_t;
    typedef typename PARENT::count_t count_t;

    static const bool s_NoThrow = false;

    static BOOL Equals(const key_t &k1, const key_t &k2)
    {
        WRAPPER_NO_CONTRACT;
        return k1.Equals(k2);
    }
    static count_t Hash(const key_t &k)
    {
        WRAPPER_NO_CONTRACT;
        return k.Hash();
    }
};

template <typename ELEMENT>
class EMPTY_BASES_DECL SStringSHash : public SHash< SStringSHashTraits<ELEMENT> >
{
};

template <typename ELEMENT>
class EMPTY_BASES_DECL SetSHashTraits : public DefaultSHashTraits<ELEMENT>
{
public:
    // explicitly declare local typedefs for these traits types, otherwise
    // the compiler may get confused
    typedef typename DefaultSHashTraits<ELEMENT>::element_t element_t;
    typedef typename DefaultSHashTraits<ELEMENT>::count_t count_t;

    typedef ELEMENT key_t;

    static key_t GetKey(element_t e)
    {
        LIMITED_METHOD_CONTRACT;
        return e;
    }
    static BOOL Equals(key_t k1, key_t k2)
    {
        LIMITED_METHOD_CONTRACT;
        return k1 == k2;
    }
    static count_t Hash(key_t k)
    {
        LIMITED_METHOD_CONTRACT;
        return (count_t)(size_t)k;
    }
};

template <typename ELEMENT, typename TRAITS = NoRemoveSHashTraits< SetSHashTraits <ELEMENT> > >
class EMPTY_BASES_DECL SetSHash : public SHash< TRAITS >
{
    typedef SHash<TRAITS> PARENT;

public:
    BOOL Contains(ELEMENT key) const
    {
        return PARENT::LookupPtr(key) != NULL;
    }
};

template <typename ELEMENT>
class EMPTY_BASES_DECL PtrSetSHashTraits : public SetSHashTraits<ELEMENT>
{
  public:

    // explicitly declare local typedefs for these traits types, otherwise
    // the compiler may get confused
    typedef SetSHashTraits<ELEMENT> PARENT;
    typedef typename PARENT::element_t element_t;
    typedef typename PARENT::key_t key_t;
    typedef typename PARENT::count_t count_t;

    static count_t Hash(key_t k)
    {
        WRAPPER_NO_CONTRACT;
        return (count_t)(size_t)k >> 2;
    }
};

template <typename PARENT_TRAITS>
class EMPTY_BASES_DECL DeleteElementsOnDestructSHashTraits : public PARENT_TRAITS
{
public:
    static inline void OnDestructPerEntryCleanupAction(typename PARENT_TRAITS::element_t e)
    {
        delete e;
    }
    static const bool s_DestructPerEntryCleanupAction = true;
};

#if !defined(CC_JIT) // workaround: Key is redefined in JIT64

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
        LIMITED_METHOD_CONTRACT;
        return key;
    }

    VALUE const & Value() const
    {
        LIMITED_METHOD_CONTRACT;
        return value;
    }
};

template <typename KEY, typename VALUE>
class EMPTY_BASES_DECL MapSHashTraits : public DefaultSHashTraits< KeyValuePair<KEY,VALUE> >
{
public:
    // explicitly declare local typedefs for these traits types, otherwise
    // the compiler may get confused
    typedef typename DefaultSHashTraits< KeyValuePair<KEY,VALUE> >::element_t element_t;
    typedef typename DefaultSHashTraits< KeyValuePair<KEY,VALUE> >::count_t count_t;

    typedef KEY key_t;

    static key_t GetKey(element_t e)
    {
        LIMITED_METHOD_CONTRACT;
        return e.Key();
    }
    static BOOL Equals(key_t k1, key_t k2)
    {
        LIMITED_METHOD_CONTRACT;
        return k1 == k2;
    }
    static count_t Hash(key_t k)
    {
        LIMITED_METHOD_CONTRACT;
        return (count_t)(size_t)k;
    }

    static const element_t Null() { LIMITED_METHOD_CONTRACT; return element_t(KEY(),VALUE()); }
    static const element_t Deleted() { LIMITED_METHOD_CONTRACT; return element_t(KEY(-1), VALUE()); }
    static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return e.Key() == KEY(); }
    static bool IsDeleted(const element_t &e) { return e.Key() == KEY(-1); }
};

template <typename KEY, typename VALUE, typename TRAITS = NoRemoveSHashTraits< MapSHashTraits <KEY, VALUE> > >
class EMPTY_BASES_DECL MapSHash : public SHash< TRAITS >
{
    typedef SHash< TRAITS > PARENT;

public:
    void Add(KEY key, VALUE value)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            PRECONDITION(key != (KEY)0);
        }
        CONTRACTL_END;

        PARENT::Add(KeyValuePair<KEY,VALUE>(key, value));
    }

    BOOL Lookup(KEY key, VALUE* pValue) const;
};

template <typename KEY, typename VALUE>
class EMPTY_BASES_DECL MapSHashWithRemove : public SHash< MapSHashTraits <KEY, VALUE> >
{
    typedef SHash< MapSHashTraits <KEY, VALUE> > PARENT;

public:
    void Add(KEY key, VALUE value)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            PRECONDITION(key != (KEY)0 && key != (KEY)-1);
        }
        CONTRACTL_END;

        PARENT::Add(KeyValuePair<KEY,VALUE>(key, value));
    }

    BOOL Lookup(KEY key, VALUE* pValue) const
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            PRECONDITION(key != (KEY)0 && key != (KEY)-1);
        }
        CONTRACTL_END;

        const KeyValuePair<KEY,VALUE> *pRet = PARENT::LookupPtr(key);
        if (pRet == NULL)
            return FALSE;

        *pValue = pRet->Value();
        return TRUE;
    }

    void Remove(KEY key)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            PRECONDITION(key != (KEY)0 && key != (KEY)-1);
        }
        CONTRACTL_END;

        PARENT::Remove(key);
    }
};

#endif // CC_JIT

#include "shash.inl"

#endif // _SHASH_H_
