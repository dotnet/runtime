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
XX                          unordered_map<K,V,H,P,A>                         XX
XX  Derives from hashtable for most implementation. Inserted elements are    XX
XX  value pairs and the hash key is provided by the helper method that       XX
XX  extracts the key from the key value pair                                 XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#pragma once

#include "hashtable.h"

namespace jitstd
{

template <typename Key, typename Value>
struct pair_key
{
    Key& operator()(const jitstd::pair<Key, Value>& pair) const
    {
        return pair.first;
    }
};

template<typename Key,
         typename Value,
         typename Hash = jitstd::hash<Key>,
         typename Pred = jitstd::equal_to<Key>,
         typename Alloc = jitstd::allocator<jitstd::pair<const Key, Value> > >
class unordered_map
    : public hashtable<Key, pair<const Key, Value>, Hash, Pred, Alloc, pair_key<const Key, Value>>
{
public:

    typedef Key key_type;
    typedef Value mapped_type;
    typedef jitstd::pair<const Key, Value> value_type;
    typedef Hash hasher;
    typedef Pred key_equal;
    typedef Alloc allocator_type;
    typedef typename allocator_type::pointer pointer;             
    typedef typename allocator_type::const_pointer const_pointer;       
    typedef typename allocator_type::reference reference;
    typedef typename allocator_type::const_reference const_reference;
    typedef size_t size_type;
    typedef ptrdiff_t difference_type;

    explicit unordered_map(size_type size, const hasher& hasher, const key_equal& pred, const allocator_type& allocator);
    explicit unordered_map(size_type size, const allocator_type& allocator);
    template<typename InputIterator> 
    unordered_map(InputIterator, InputIterator, 
                  size_type size, 
                  const hasher& hasher,
                  const key_equal& pred, 
                  const allocator_type& allocator);

    unordered_map(const unordered_map& map);
    explicit unordered_map(const allocator_type& allocator);
    unordered_map(const unordered_map& map, const allocator_type& allocator);
    ~unordered_map();

    unordered_map& operator=(unordered_map const&);
    mapped_type& operator[](const Key& key);
    mapped_type& operator[](key_type&& key);

    typename unordered_map<Key, Value, Hash, Pred, Alloc>::iterator insert(const key_type& key, const mapped_type& value);

private:
    typedef hashtable<Key, pair<const Key, Value>, Hash, Pred, Alloc, pair_key<const Key, Value>> base_type;
};

}


namespace jitstd
{

template<typename Key, typename Value, typename Hash, typename Pred, typename Alloc>
unordered_map<Key, Value, Hash, Pred, Alloc>::unordered_map(size_type size, const hasher& hasher, const key_equal& pred, const allocator_type& allocator)
    : base_type(size, hasher, pred, allocator)
{
}

template<typename Key, typename Value, typename Hash, typename Pred, typename Alloc>
unordered_map<Key, Value, Hash, Pred, Alloc>::unordered_map(size_type size, const allocator_type& allocator)
    : base_type(size, allocator)
{
}

template<typename Key, typename Value, typename Hash, typename Pred, typename Alloc>
template<typename InputIterator> 
unordered_map<Key, Value, Hash, Pred, Alloc>::unordered_map(InputIterator first, InputIterator last, 
                size_type size, 
                const hasher& hasher,
                const key_equal& pred, 
                const allocator_type& allocator)
    : base_type(first, last, size, hasher, pred, allocator)
{
}

template<typename Key, typename Value, typename Hash, typename Pred, typename Alloc>
unordered_map<Key, Value, Hash, Pred, Alloc>::unordered_map(const unordered_map& map)
    : base_type(map)
{
}

template<typename Key, typename Value, typename Hash, typename Pred, typename Alloc>
unordered_map<Key, Value, Hash, Pred, Alloc>::unordered_map(const allocator_type& allocator)
    : base_type(allocator)
{
}

template<typename Key, typename Value, typename Hash, typename Pred, typename Alloc>
unordered_map<Key, Value, Hash, Pred, Alloc>::unordered_map(const unordered_map& map, const allocator_type& allocator)
    : base_type(map, allocator)
{
}

template<typename Key, typename Value, typename Hash, typename Pred, typename Alloc>
unordered_map<Key, Value, Hash, Pred, Alloc>::~unordered_map()
{
}

template<typename Key, typename Value, typename Hash, typename Pred, typename Alloc>
unordered_map<Key, Value, Hash, Pred, Alloc>& unordered_map<Key, Value, Hash, Pred, Alloc>::operator=(const unordered_map& map)
{
    base_type::operator=(map);
    return *this;
}

template<typename Key, typename Value, typename Hash, typename Pred, typename Alloc>
Value& unordered_map<Key, Value, Hash, Pred, Alloc>::operator[](const Key& key)
{
    typename unordered_map<Key, Value, Hash, Pred, Alloc>::iterator iter = base_type::find(key, this->key_eq());
    if (iter == this->end())
    {
        iter = base_type::insert(jitstd::pair<const Key, mapped_type>(key, mapped_type())).first;
    }
    return (*iter).second;
}

template<typename Key, typename Value, typename Hash, typename Pred, typename Alloc>
Value& unordered_map<Key, Value, Hash, Pred, Alloc>::operator[](key_type&& key)
{
    typename unordered_map<Key, Value, Hash, Pred, Alloc>::iterator iter = base_type::find(key, this->key_eq());
    if (iter == this->end())
    {
        iter = base_type::insert(jitstd::pair<const Key, mapped_type>(key, mapped_type())).first;
    }
    return (*iter).second;
}


template<typename Key, typename Value, typename Hash, typename Pred, typename Alloc>
typename unordered_map<Key, Value, Hash, Pred, Alloc>::iterator
unordered_map<Key, Value, Hash, Pred, Alloc>::insert(const key_type& key, const mapped_type& value)
{
    typename unordered_map<Key, Value, Hash, Pred, Alloc>::iterator iter = base_type::find(key, this->key_eq());
    iter = base_type::insert(jitstd::pair<const Key, mapped_type>(key, value)).first;
    return iter;
}

}
