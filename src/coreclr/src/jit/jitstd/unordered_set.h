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
XX                          unordered_set<V,H,P,A>                           XX
XX                                                                           XX
XX  Derives from hashtable for most implementation. The hash key is the      XX
XX  elements themselves                                                      XX
XX                                                                           XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#pragma once

#include "allocator.h"
#include "hashtable.h"

namespace jitstd
{

template <typename Value,
          typename Hash = jitstd::hash<Value>,
          typename Pred = jitstd::equal_to<Value>,
          typename Alloc = jitstd::allocator<Value>>
class unordered_set
    : public hashtable<Value, Value, Hash, Pred, Alloc>
{
public:
    typedef Value key_type;
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
    typedef typename list<Value, Alloc>::const_iterator const_iterator;
    typedef typename list<Value, Alloc>::iterator local_iterator;

private:
    typedef hashtable<Value, Value, Hash, Pred, Alloc> base_type;
    unordered_set();

    typedef pair<iterator, iterator> BucketEntry;
    typedef vector<BucketEntry, typename Alloc::template rebind<BucketEntry>::allocator> Buckets;
    typedef list<Value, Alloc> Elements;

public:
    explicit unordered_set(size_type,
        const allocator_type& a);

    unordered_set(size_type n,
        const hasher& hf,
        const key_equal& eq, 
        const allocator_type&);

    template<typename InputIterator> 
    unordered_set(
        InputIterator f, InputIterator l,
        size_type n,
        const hasher& hf,
        const key_equal& eq, 
        const allocator_type&);

    explicit unordered_set(const allocator_type&);

    unordered_set(const unordered_set& other);

    ~unordered_set();

    unordered_set& operator=(unordered_set const&);
};

} // end of namespace jitstd


namespace jitstd
{

template <typename Value, typename Hash, typename Pred, typename Alloc>
unordered_set<Value, Hash, Pred, Alloc>::unordered_set(
    size_type n,
    allocator_type const& allocator)
    : hashtable<Value>(n, allocator)
{
    this->rehash(n);
}

template <typename Value, typename Hash, typename Pred, typename Alloc>
unordered_set<Value, Hash, Pred, Alloc>::unordered_set(
    size_type n,
    hasher const& hf,
    key_equal const& eq,
    allocator_type const& allocator)
    : hashtable<Value>(n, hf, eq, allocator)
{
    this->rehash(n);
}

template <typename Value, typename Hash, typename Pred, typename Alloc>
template<typename InputIterator>
unordered_set<Value, Hash, Pred, Alloc>::unordered_set(
    InputIterator f, InputIterator l,
    size_type n,
    const hasher& hf,
    const key_equal& eq, 
    const allocator_type& allocator)
    : hashtable<Value>(f, l, n, hf, eq, allocator)
{
    this->rehash(n);
    insert(this->first, this->last);
}

template <typename Value, typename Hash, typename Pred, typename Alloc>
unordered_set<Value, Hash, Pred, Alloc>::unordered_set(const allocator_type& allocator)
: hashtable<Value>(allocator)
{
}

template <typename Value, typename Hash, typename Pred, typename Alloc>
unordered_set<Value, Hash, Pred, Alloc>::unordered_set(const unordered_set& other)
: hashtable<Value>(other)
{
}

template <typename Value, typename Hash, typename Pred, typename Alloc>
unordered_set<Value, Hash, Pred, Alloc>::~unordered_set()
{
}

template <typename Value, typename Hash, typename Pred, typename Alloc>
unordered_set<Value, Hash, Pred, Alloc>&
    unordered_set<Value, Hash, Pred, Alloc>::operator=(unordered_set const& other)
{
    base_type::operator=(other);
    return *this;
}

} // end of namespace jitstd.
