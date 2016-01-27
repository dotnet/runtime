// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
//

//

//
// ==--==

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                          hashtable<K,V,H,P,A,KO>                          XX
XX                                                                           XX
XX  Implemented using a vector of list iterators begin and end whose range   XX
XX  is a single bucket. A chain of buckets is maintained in a linked list    XX
XX  (doubly) for holding the key-value pairs.                                XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#pragma once

#include "hash.h"
#include "functional.h"
#include "allocator.h"
#include "vector.h"
#include "list.h"
#include "pair.h"

namespace jitstd
{

static const float kflDefaultLoadFactor = 3.0f;

template <typename Key,
          typename Value = Key,
          typename Hash = jitstd::hash<Key>,
          typename Pred = jitstd::equal_to<Key>,
          typename Alloc = jitstd::allocator<Value>,
          typename KeyOf = jitstd::identity<Value>>
class hashtable
{
public:
    typedef Key key_type;
    typedef Value value_type;
    typedef Hash hasher;
    typedef Pred key_equal;
    typedef Alloc allocator_type;
    typedef typename allocator_type::pointer pointer;
    typedef typename allocator_type::const_pointer const_pointer;
    typedef typename allocator_type::reference reference;
    typedef typename allocator_type::const_reference const_reference;
    typedef size_t size_type;
    typedef ptrdiff_t difference_type;
    typedef typename list<Value, Alloc>::iterator iterator;
    typedef typename list<Value, Alloc>::reverse_iterator reverse_iterator;
    typedef typename list<Value, Alloc>::const_iterator const_iterator;
    typedef typename list<Value, Alloc>::iterator local_iterator;

protected:
    hashtable();

    typedef pair<iterator, iterator> BucketEntry;
    typedef vector<BucketEntry, typename Alloc::template rebind<BucketEntry>::allocator> Buckets;
    typedef list<Value, typename Alloc::template rebind<Value>::allocator> Elements;

protected:
    explicit hashtable(size_type,
        const allocator_type& a,
        const KeyOf& keyOf = KeyOf());

    hashtable(size_type n,
        const hasher& hf,
        const key_equal& eq,
        const allocator_type& a,
        const KeyOf& keyOf = KeyOf());

    template<typename InputIterator> 
    hashtable(
        InputIterator f, InputIterator l,
        size_type n,
        const hasher& hf,
        const key_equal& eq, 
        const allocator_type& a,
        const KeyOf& keyOf = KeyOf());

    explicit hashtable(const allocator_type& a, const KeyOf& keyOf = KeyOf());

    hashtable(const hashtable& other);

    ~hashtable();

public:
    hashtable& operator=(const hashtable& other);

    allocator_type get_allocator() const;

    bool empty() const;

    size_type size() const;
    size_type max_size() const;

    iterator begin();
    iterator end();

    // Even though we have an unordered set and there is no concept of forward and
    // reverse, rbegin will just return the first element inserted. This is not in STL.
    reverse_iterator rbegin();
    reverse_iterator rend();

    const_iterator begin() const;
    const_iterator end() const;
    const_iterator cbegin() const;
    const_iterator cend() const;
    local_iterator begin(size_type size);
    local_iterator end(size_type size);

    pair<iterator, bool> insert(const value_type& value);
    iterator insert(const_iterator, const value_type& value);
    template<typename InputIterator>
    void insert(InputIterator first, InputIterator last);

    iterator erase(iterator position);
    size_type erase(const key_type& key);
    iterator erase(iterator first, iterator last);

    void clear();
    void swap(hashtable& table);

    hasher hash_function() const;
    key_equal key_eq() const;

    const_iterator find(const key_type& key) const;
    iterator find(const key_type& key);

    size_type count(const key_type& key) const;

    size_type bucket_count() const;
    size_type max_bucket_count() const;

    size_type bucket_size(size_type size) const;
    size_type bucket(const key_type& key) const;

    float load_factor() const;
    float max_load_factor() const;
    void max_load_factor(float);

    void rehash(size_type);

protected:
    template <typename Compare>
    iterator find(const key_type&, const Compare& comp);

    // helpers
    bool check_load();
    void copy_helper(const hashtable& other);
    size_type hash_helper(const key_type& value, size_type buckets) const;
    pair<iterator, bool> insert_helper(const value_type& value, Buckets& buckets, Elements& elements, bool fRehashing);
    iterator erase_helper(const_iterator position);
    void dump_helper();
    void debug_check();

private:

    // member objects
    Hash m_hasher;
    Alloc m_allocator;
    Pred m_pred;

    Buckets m_buckets;
    Elements m_elements;
    size_type m_nSize;
    KeyOf m_keyOf;

    // metadata
    float m_flMaxLoadFactor;
};

} // end of namespace jitstd


namespace jitstd
{

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
void hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::dump_helper()
{
    for (size_type i = 0; i < m_buckets.size(); ++i)
    {
        printf("\n");
        printf("--------------=BEGIN=--------------\n");
        printf("Load factor = %f\n", load_factor());
        printf("-----------------------------------\n");
        printf("Bucket number = %d %p %p\n", i, *((ptrdiff_t*)&(m_buckets[i].first)), *((ptrdiff_t*)&(m_buckets[i].second)));
        printf("-----------------------------------\n");
        for (typename Elements::iterator value = (m_buckets[i]).first; value != (m_buckets[i]).second; ++value)
        {
            printf("%d, ", *((ptrdiff_t*)&value), *value);
        }
        printf("-----------------------------------\n");
    }
}

// We can't leave this permanently enabled -- it makes algorithms cubic, and causes tests to time out.
// Enable when/if you have reason to believe there's a problem in hashtable.
#define JITSTD_DO_HASHTABLE_DEBUGCHECK 0

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
void hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::debug_check()
{
#if JITSTD_DO_HASHTABLE_DEBUGCHECK
    for (iterator iter = m_elements.begin(); iter != m_elements.end(); ++iter)
    {
        size_type nHash = hash_helper(m_keyOf(*iter), m_buckets.size());
        BucketEntry& entry = m_buckets[nHash];
        iterator iter2 = entry.first;
        bool present = false;
        while (iter2 != entry.second)
        {
            if (iter2 == iter)
            {
                present = true;
            }
            iter2++;
        }
        if (!present)
        {
            present = false;
        }
        assert(present);
    }
#endif
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
template <typename Compare>
typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::iterator
hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::find(const key_type& key, const Compare& comp)
{
    if (empty())
    {
        return end();
    }
    size_type nHash = hash_helper(key, m_buckets.size());
    BucketEntry& entry = m_buckets[nHash];
    for (iterator i = entry.first; i != entry.second; ++i)
    {
        if (comp(m_keyOf(*i), key))
        {
            return i;
        }
    }
    return end();
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
bool hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::check_load()
{
    float flLoadFactor = load_factor();
    if (flLoadFactor > m_flMaxLoadFactor)
    {
        rehash(m_buckets.size());
        return true;
    }
    return false;
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::iterator
hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::erase_helper(const_iterator position)
{
    const Key& key = m_keyOf(*position);
    size_type nHash = hash_helper(key, m_buckets.size());
    BucketEntry& entry = m_buckets[nHash];
    iterator eraseNext = end();
    for (iterator first = entry.first; first != entry.second; ++first)
    {
        if (m_pred(m_keyOf(*first), key))       
        {
            if (first == entry.first)
            {
                if (first != m_elements.begin())
                {
                    iterator update = first;
                    update--;
                    size_type nUpdateHash = hash_helper(m_keyOf(*update), m_buckets.size());
                    if (nUpdateHash != nHash)
                    {
                        BucketEntry& updateEntry = m_buckets[nUpdateHash];
                        if (updateEntry.second == first)
                        {
                            updateEntry.second = first;
                            updateEntry.second++;
                        }
                        if (updateEntry.first == first)
                        {
                            updateEntry.first = first;
                            updateEntry.first++;
                        }
                    }
                }
                entry.first = m_elements.erase(first);
                eraseNext = entry.first;
            }
            else
            {
                eraseNext = m_elements.erase(first);
            }

            --m_nSize;
#ifdef DEBUG
            debug_check();
#endif
            return eraseNext;
        }
    }
    return end();
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
pair<typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::iterator, bool>
hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::insert_helper(
    const Value& value, Buckets& buckets, Elements& elements, bool fRehashing)
{
    const Key& key = m_keyOf(value);
    size_t nHash = hash_helper(key, buckets.size());
    BucketEntry& entry = buckets[nHash];

    iterator ret;
    if (entry.first == entry.second)
    {
        entry.first = elements.insert(elements.begin(), value);
        entry.second = entry.first;
        entry.second++; // end iterator is one past always.
        ret = entry.first;
    }
    else
    {
        for (iterator first = entry.first; first != entry.second; ++first)
        {
            if (m_pred(m_keyOf(*first), key))
            {
                return pair<iterator, bool>(first, false);
            }
        }
        iterator firstNext = entry.first;
        firstNext++;
        ret = elements.insert(firstNext, value);
        if (entry.second == entry.first)
        {
            entry.second = firstNext;
        }
    }
    bool fRehashed = false;
    if (!fRehashing)
    {
        m_nSize += 1;
        fRehashed = check_load();
    }

#ifdef DEBUG
    debug_check();
#endif

    return pair<iterator, bool>(fRehashed ? find(key, m_pred) : ret, true);
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::size_type
    hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::hash_helper(
    const key_type& key, size_type buckets) const
{
    return m_hasher(key) % buckets;
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
void hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::rehash(size_type n)
{
    size_type nCurBuckets = m_buckets.size();
    float flLoadFactor = load_factor();
    if (nCurBuckets >= n && flLoadFactor <= m_flMaxLoadFactor)
    {
        return;
    }

    size_type nBuckets = max(nCurBuckets, 1);
    if (flLoadFactor > m_flMaxLoadFactor)
    {
        nBuckets *= 2;
    }

    if (nBuckets < n)
    {
        nBuckets = n;
    }

    Buckets buckets(m_allocator);
    Elements elements(m_allocator);

    buckets.resize(nBuckets, BucketEntry(m_elements.end(), m_elements.end())); // both equal means empty.
    for (typename Elements::iterator iter = m_elements.begin(); iter != m_elements.end(); ++iter)
    {
        (void) insert_helper(*iter, buckets, elements, true);
    }
    m_buckets.swap(buckets);
    m_elements.swap(elements);
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::hashtable(
    size_type n,
    allocator_type const& allocator,
    const KeyOf& keyOf)
    : m_allocator(allocator)
    , m_buckets(Alloc::template rebind<hashtable::BucketEntry>::allocator(allocator))
    , m_elements(allocator)
    , m_flMaxLoadFactor(kflDefaultLoadFactor)
    , m_nSize(0)
    , m_keyOf(keyOf)
{
    rehash(n);
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::hashtable(
    size_type n,
    hasher const& hf,
    key_equal const& eq,
    allocator_type const& allocator,
    const KeyOf& keyOf)
    : m_hasher(hf)
    , m_pred(eq)
    , m_allocator(allocator)
    , m_buckets(Alloc::template rebind<BucketEntry>::allocator(allocator))
    , m_elements(allocator)
    , m_flMaxLoadFactor(kflDefaultLoadFactor)
    , m_nSize(0)
    , m_keyOf(keyOf)
{
    rehash(n);
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
template<typename InputIterator>
hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::hashtable(
    InputIterator f, InputIterator l,
    size_type n,
    const hasher& hf,
    const key_equal& eq, 
    const allocator_type& allocator,
    const KeyOf& keyOf)
    : m_hasher(hf)
    , m_pred(eq)
    , m_allocator(allocator)
    , m_buckets(Alloc::template rebind<BucketEntry>::allocator(allocator))
    , m_elements(allocator)
    , m_flMaxLoadFactor(kflDefaultLoadFactor)
    , m_nSize(0)
    , m_keyOf(keyOf)
{
    rehash(n);
    insert(this->first, this->last);
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::hashtable(const allocator_type& allocator, const KeyOf& keyOf)
    : m_allocator(allocator)
    , m_buckets(Alloc::template rebind<BucketEntry>::allocator(allocator))
    , m_elements(allocator)
    , m_flMaxLoadFactor(kflDefaultLoadFactor)
    , m_nSize(0)
    , m_keyOf(keyOf)
{
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
void hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::copy_helper(const hashtable& other)
{
    m_buckets.clear();
    m_elements.clear();
    m_nSize = 0;

    rehash(other.m_buckets.size());
    for (const_iterator i = other.m_elements.begin(); i != other.m_elements.end(); ++i)
    {
        insert_helper(*i, m_buckets, m_elements, false);
    }
    m_nSize = other.m_nSize;
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::hashtable(const hashtable& other)
    : m_hasher(other.m_hasher)
    , m_pred(other.m_pred)
    , m_allocator(other.m_allocator)
    , m_flMaxLoadFactor(other.m_flMaxLoadFactor)
    , m_keyOf(other.m_keyOf)
    , m_elements(other.m_allocator)
    , m_buckets(other.m_allocator)
{
    copy_helper(other);
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::~hashtable()
{
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>&
    hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::operator=(hashtable const& other)
{
    m_hasher = other.m_hasher;
    m_pred = other.m_pred;
    m_allocator = other.m_allocator;
    m_flMaxLoadFactor = other.m_flMaxLoadFactor;
    m_keyOf = other.m_keyOf;
    copy_helper(other);
    return *this;
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::allocator_type
    hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::get_allocator() const
{
    return m_allocator;
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
bool hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::empty() const
{
    return m_nSize == 0;
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::size_type
    hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::size() const
{
    return m_nSize;
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::size_type
    hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::max_size() const
{
    return ((size_type)(-1)) >> 1;
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::iterator
hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::begin()
{
    return m_elements.begin();
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::reverse_iterator
hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::rbegin()
{
    return m_elements.rbegin();
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::iterator
hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::end()
{
    return m_elements.end();
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::reverse_iterator
hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::rend()
{
    return m_elements.rend();
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::const_iterator
hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::begin() const
{
    return m_elements.begin();
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::const_iterator
hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::end() const
{
    return m_elements.end();
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::const_iterator
hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::cbegin() const
{
    return m_elements.begin();
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::const_iterator
hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::cend() const
{
    return m_elements.end();
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
jitstd::pair<typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::iterator, bool>
    hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::insert(const Value& val)
{
    // Allocate some space first.
    rehash(2);
    return insert_helper(val, m_buckets, m_elements, false);
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::iterator
    hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::insert(const_iterator position, const Value& value)
{
    // Allocate some space first.
    rehash(2);

    // We will not use the hint here, we can consider doing this later.
    return insert_helper(this->val, m_buckets, m_elements, false).first;
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
template<typename InputIterator>
void hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::insert(InputIterator first, InputIterator last)
{
    // Allocate some space first.
    rehash(2);
    while (first != last)
    {
        (void) insert_helper(*first, m_buckets, m_elements, false);
        ++first;
    }
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::iterator
    hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::erase(iterator position)
{
    return erase_helper(position);
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::size_type
    hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::erase(const key_type& key)
{
    iterator iter = erase_helper(find(key));
    return iter == end() ? 0 : 1;
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::iterator
    hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::erase(iterator first, iterator last)
{
    iterator iter = end();
    while (first != last)
    {
        iter = erase_helper(find(m_keyOf(*first)));
        ++first;
    }
    return iter;
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
void hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::clear()
{
    m_buckets.clear();
    m_elements.clear();
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
void hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::swap(hashtable& set)
{
    std::swap(set.m_buckets, m_buckets);
    std::swap(set.m_elements, m_elements);
    std::swap(set.m_flLoadFactor, this->m_flLoadFactor);
    std::swap(set.m_flMaxLoadFactor, this->m_flMaxLoadFactor);
    std::swap(set.m_keyOf, this->m_keyOf);
}


template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::hasher
    hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::hash_function() const
{
    return m_hasher;
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::key_equal
    hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::key_eq() const
{
    return m_pred;
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::const_iterator
    hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::find(const key_type& key) const
{
    if (empty())
    {
        return end();
    }
    size_type nHash = hash_helper(key, m_buckets.size());
    BucketEntry& entry = m_buckets[nHash];
    for (iterator i = entry.first; i != entry.second; ++i)
    {
        if (m_pred(m_keyOf(*i), key))
        {
            return i;
        }
    }
    return end();
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::iterator
    hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::find(const key_type& key)
{
    if (empty())
    {
        return end();
    }
    size_type nHash = hash_helper(key, m_buckets.size());
    BucketEntry& entry = m_buckets[nHash];
    for (iterator i = entry.first; i != entry.second; ++i)
    {
        if (m_pred(m_keyOf(*i), key))
        {
            return i;
        }
    }
    return end();
}


template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::size_type
    hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::count(const key_type& key) const
{
    size_type nCount = 0;
    size_type nHash = hash_helper(key, m_buckets.size());
    BucketEntry& bucket = m_buckets[nHash];
    for (iterator i = bucket.first; i != bucket.second; ++i)
    {
        if (m_pred(m_keyOf(*i), key))
        {
            ++nCount;
        }
    }
    return nCount;  
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::size_type
hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::bucket_count() const
{   
    return m_buckets.size();
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::size_type
hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::max_bucket_count() const
{
    return m_buckets.size();
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::size_type
hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::bucket_size(size_type size) const
{
    rehash(size);
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::size_type
hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::bucket(const key_type& key) const
{
    return hash_helper(key, m_buckets.size());
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::local_iterator
hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::begin(size_type size)
{
    return m_buckets[size].first;
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
typename hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::local_iterator
hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::end(size_type size)
{
    return m_buckets[size].second;
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
float hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::load_factor() const
{
    return m_nSize ? (((float) m_nSize) /  m_buckets.size()) : 0;
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
float hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::max_load_factor() const
{
    return m_flMaxLoadFactor;
}

template <typename Key, typename Value, typename Hash, typename Pred, typename Alloc, typename KeyOf>
void hashtable<Key, Value, Hash, Pred, Alloc, KeyOf>::max_load_factor(float flLoadFactor)
{
    m_flMaxLoadFactor = flLoadFactor;
    rehash(m_buckets.size());
}

} // end of namespace jitstd.
