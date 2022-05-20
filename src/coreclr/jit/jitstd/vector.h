// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                                vector<T>                                  XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#pragma once

#include "allocator.h"
#include "iterator.h"

namespace jitstd
{

template <typename T, typename Allocator = allocator<T> >
class vector
{
public:
    typedef Allocator allocator_type;
    typedef T* pointer;
    typedef T& reference;
    typedef const T* const_pointer;
    typedef const T& const_reference;

    typedef size_t size_type;
    typedef ptrdiff_t difference_type;
    typedef T value_type;

    // nested classes
    class iterator : public jitstd::iterator<random_access_iterator_tag, T>
    {
        iterator(T* ptr);
    public:
        iterator();
        iterator(const iterator& it);

        iterator& operator++();
        iterator& operator++(int);
        iterator& operator--();
        iterator& operator--(int);
        iterator operator+(difference_type n);
        iterator operator-(difference_type n);
        size_type operator-(const iterator& that);
        bool operator==(const iterator& it);
        bool operator!=(const iterator& it);
        T& operator*();
        T* operator&();
        operator T*();

    private:
        friend class vector<T, Allocator>;
        pointer m_pElem;
    };

    class const_iterator : public jitstd::iterator<random_access_iterator_tag, T>
    {
    private:
        const_iterator(T* ptr);
        const_iterator();
    public:
        const_iterator(const const_iterator& it);

        const_iterator& operator++();
        const_iterator& operator++(int);
        const_iterator& operator--();
        const_iterator& operator--(int);
        const_iterator operator+(difference_type n);
        const_iterator operator-(difference_type n);
        size_type operator-(const const_iterator& that);
        bool operator==(const const_iterator& it) const;
        bool operator!=(const const_iterator& it) const;
        const T& operator*() const;
        const T* operator&() const;
        operator const T*() const;

    private:
        friend class vector<T, Allocator>;
        pointer m_pElem;
    };

    class reverse_iterator : public jitstd::iterator<random_access_iterator_tag, T>
    {
    private:
        reverse_iterator(T* ptr);
    public:
        reverse_iterator();
        reverse_iterator(const reverse_iterator& it);

        reverse_iterator& operator++();
        reverse_iterator& operator++(int);
        reverse_iterator& operator--();
        reverse_iterator& operator--(int);
        reverse_iterator operator+(difference_type n);
        reverse_iterator operator-(difference_type n);
        size_type operator-(const reverse_iterator& that);
        bool operator==(const reverse_iterator& it);
        bool operator!=(const reverse_iterator& it);
        T& operator*();
        T* operator&();
        operator T*();

    private:
        friend class vector<T, Allocator>;
        pointer m_pElem;
    };

    class const_reverse_iterator : public jitstd::iterator<random_access_iterator_tag, T>
    {
    private:
        const_reverse_iterator(T* ptr);
    public:
        const_reverse_iterator();
        const_reverse_iterator(const const_reverse_iterator& it);

        const_reverse_iterator& operator++();
        const_reverse_iterator& operator++(int);
        const_reverse_iterator& operator--();
        const_reverse_iterator& operator--(int);
        const_reverse_iterator operator+(difference_type n);
        const_reverse_iterator operator-(difference_type n);
        size_type operator-(const const_reverse_iterator& that);
        bool operator==(const const_reverse_iterator& it) const;
        bool operator!=(const const_reverse_iterator& it) const;
        const T& operator*() const;
        const T* operator&() const;
        operator const T*() const;

    private:
        friend class vector<T, Allocator>;
        pointer m_pElem;
    };

    // ctors
    explicit vector(const Allocator& allocator);
    explicit vector(size_type n, const T& value, const Allocator& allocator);

    template <typename InputIterator>
    vector(InputIterator first, InputIterator last, const Allocator& allocator);

    // cctors
    vector(const vector& vec);

    template <typename Alt, typename AltAllocator>
    explicit vector(const vector<Alt, AltAllocator>& vec);

    // dtor
    ~vector();

    template <class InputIterator>
    void assign(InputIterator first, InputIterator last);
    void assign(size_type size, const T& value);

    const_reference at(size_type n) const;
    reference at(size_type n);

    reference back();
    const_reference back() const;

    iterator begin();
    const_iterator begin() const;
    const_iterator cbegin() const;

    size_type capacity() const;

    void clear();
    bool empty() const;

    iterator end();
    const_iterator end() const;
    const_iterator cend() const;

    iterator erase(iterator position);
    iterator erase(iterator first, iterator last);

    reference front();
    const_reference front() const;

    allocator_type get_allocator() const;

    iterator insert(iterator position, const T& value);
    void insert(iterator position, size_type size, const T& value);

    template <typename InputIterator>
    void insert(iterator position, InputIterator first, InputIterator last);

    size_type max_size() const;

    vector& operator=(const vector& vec);
    template <typename Alt, typename AltAllocator>
    vector<T, Allocator>& operator=(const vector<Alt, AltAllocator>& vec);

    reference operator[](size_type n);
    const_reference operator[](size_type n) const;

    void pop_back();
    void push_back(const T& value);

    reverse_iterator rbegin();
    const_reverse_iterator rbegin() const;

    reverse_iterator rend();
    const_reverse_iterator rend() const;

    void reserve(size_type n);

    void resize(size_type sz, const T&);

    size_type size() const;

    T* data() { return m_pArray; }

    void swap(vector<T, Allocator>& vec);

private:

    typename Allocator::template rebind<T>::allocator m_allocator;
    T* m_pArray;
    size_type m_nSize;
    size_type m_nCapacity;

    inline
    bool ensure_capacity(size_type capacity);

    template <typename InputIterator>
    void construct_helper(InputIterator first, InputIterator last, forward_iterator_tag);
    template <typename InputIterator>
    void construct_helper(InputIterator first, InputIterator last, int_not_an_iterator_tag);
    void construct_helper(size_type size, const T& value);

    template <typename InputIterator>
    void insert_helper(iterator iter, InputIterator first, InputIterator last, forward_iterator_tag);
    template <typename InputIterator>
    void insert_helper(iterator iter, InputIterator first, InputIterator last, int_not_an_iterator_tag);
    void insert_elements_helper(iterator iter, size_type size, const T& value);

    template <typename InputIterator>
    void assign_helper(InputIterator first, InputIterator last, forward_iterator_tag);
    template <typename InputIterator>
    void assign_helper(InputIterator first, InputIterator last, int_not_an_iterator_tag);

    template <typename Alt, typename AltAllocator>
    friend class vector;
};

}// namespace jitstd



// Implementation of vector.

namespace jitstd
{

namespace
{

template <typename InputIterator>
size_t iterator_difference(InputIterator first, const InputIterator& last)
{
    size_t size = 0;
    for (; first != last; ++first, ++size);
    return size;
}

}


template <typename T, typename Allocator>
vector<T, Allocator>::vector(const Allocator& allocator)
    : m_allocator(allocator)
    , m_pArray(nullptr)
    , m_nSize(0)
    , m_nCapacity(0)
{
}

template <typename T, typename Allocator>
vector<T, Allocator>::vector(size_type size, const T& value, const Allocator& allocator)
    : m_allocator(allocator)
    , m_pArray(NULL)
    , m_nSize(0)
    , m_nCapacity(0)
{
    construct_helper(size, value);
}

template <typename T, typename Allocator>
template <typename InputIterator>
vector<T, Allocator>::vector(InputIterator first, InputIterator last, const Allocator& allocator)
    : m_allocator(allocator)
    , m_pArray(NULL)
    , m_nSize(0)
    , m_nCapacity(0)
{
    construct_helper(first, last, iterator_traits<InputIterator>::iterator_category());
}

template <typename T, typename Allocator>
template <typename Alt, typename AltAllocator>
vector<T, Allocator>::vector(const vector<Alt, AltAllocator>& vec)
    : m_allocator(vec.m_allocator)
    , m_pArray(NULL)
    , m_nSize(0)
    , m_nCapacity(0)
{
    ensure_capacity(vec.m_nSize);
    for (size_type i = 0, j = 0; i < vec.m_nSize; ++i, ++j)
    {
        new (m_pArray + i, placement_t()) T((T) vec.m_pArray[j]);
    }

    m_nSize = vec.m_nSize;
}

template <typename T, typename Allocator>
vector<T, Allocator>::vector(const vector<T, Allocator>& vec)
    : m_allocator(vec.m_allocator)
    , m_pArray(NULL)
    , m_nSize(0)
    , m_nCapacity(0)
{
    ensure_capacity(vec.m_nSize);
    for (size_type i = 0, j = 0; i < vec.m_nSize; ++i, ++j)
    {
        new (m_pArray + i, placement_t()) T(vec.m_pArray[j]);
    }

    m_nSize = vec.m_nSize;
}


template <typename T, typename Allocator>
vector<T, Allocator>::~vector()
{
    for (size_type i = 0; i < m_nSize; ++i)
    {
        m_pArray[i].~T();
    }
    m_allocator.deallocate(m_pArray, m_nCapacity);
    m_nSize = 0;
    m_nCapacity = 0;
}


// public methods

template <typename T, typename Allocator>
template <typename InputIterator>
void vector<T, Allocator>::assign(InputIterator first, InputIterator last)
{
    construct_helper(first, last, iterator_traits<InputIterator>::iterator_category());
}

template <typename T, typename Allocator>
void vector<T, Allocator>::assign(size_type size, const T& value)
{
    ensure_capacity(size);
    for (int i = 0; i < size; ++i)
    {
        m_pArray[i] = value;
    }
    m_nSize = size;
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::const_reference
    vector<T, Allocator>::at(size_type i) const
{
    return operator[](i);
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::reference
    vector<T, Allocator>::at(size_type i)
{
    return operator[](i);
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::reference
    vector<T, Allocator>::back()
{
    return operator[](m_nSize - 1);
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::const_reference
    vector<T, Allocator>::back() const
{
    return operator[](m_nSize - 1);
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::iterator
    vector<T, Allocator>::begin()
{
    return iterator(m_pArray);
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::const_iterator
    vector<T, Allocator>::begin() const
{
    return const_iterator(m_pArray);
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::const_iterator
    vector<T, Allocator>::cbegin() const
{
    return const_iterator(m_pArray);
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::size_type
    vector<T, Allocator>::capacity() const
{
    return m_nCapacity;
}


template <typename T, typename Allocator>
void vector<T, Allocator>::clear()
{
    for (size_type i = 0; i < m_nSize; ++i)
    {
        m_pArray[i].~T();
    }
    m_nSize = 0;
}

template <typename T, typename Allocator>
bool vector<T, Allocator>::empty() const
{
    return m_nSize == 0;
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::iterator vector<T, Allocator>::end()
{
    return iterator(m_pArray + m_nSize);
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::const_iterator
    vector<T, Allocator>::end() const
{
    return const_iterator(m_pArray + m_nSize);
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::const_iterator vector<T, Allocator>::cend() const
{
    return const_iterator(m_pArray + m_nSize);
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::iterator
    vector<T, Allocator>::erase(
        typename vector<T, Allocator>::iterator position)
{
    return erase(position, position + 1);
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::iterator
    vector<T, Allocator>::erase(
        typename vector<T, Allocator>::iterator first,
        typename vector<T, Allocator>::iterator last)
{
    assert(m_nSize > 0);
    assert(first.m_pElem >= m_pArray);
    assert(last.m_pElem >= m_pArray);
    assert(first.m_pElem <= m_pArray + m_nSize);
    assert(last.m_pElem <= m_pArray + m_nSize);
    assert(last.m_pElem > first.m_pElem);

    pointer fptr = first.m_pElem;
    pointer lptr = last.m_pElem;
    pointer eptr = m_pArray + m_nSize;
    for (; lptr != eptr; ++lptr, fptr++)
    {
        (*fptr).~T();
        *fptr = *lptr;
    }
    m_nSize -= (size_type)(lptr - fptr);
    return first;
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::reference
    vector<T, Allocator>::front()
{
    return operator[](0);
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::const_reference
    vector<T, Allocator>::front() const
{
    return operator[](0);
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::allocator_type
    vector<T, Allocator>::get_allocator() const
{
    return m_allocator;
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::iterator
    vector<T, Allocator>::insert(
        typename vector<T, Allocator>::iterator iter,
        const T& value)
{
    size_type pos = (size_type) (iter.m_pElem - m_pArray);
    insert_elements_helper(iter, 1, value);
    return iterator(m_pArray + pos);
}

template <typename T, typename Allocator>
void vector<T, Allocator>::insert(
    iterator iter,
    size_type size,
    const T& value)
{
    insert_elements_helper(iter, size, value);
}

template <typename T, typename Allocator>
template <typename InputIterator>
void vector<T, Allocator>::insert(
    iterator iter,
    InputIterator first,
    InputIterator last)
{
    insert_helper(iter, first, last, iterator_traits<InputIterator>::iterator_category());
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::size_type
    vector<T, Allocator>::max_size() const
{
    return ((size_type) -1) >> 1;
}

template <typename T, typename Allocator>
template <typename Alt, typename AltAllocator>
vector<T, Allocator>& vector<T, Allocator>::operator=(const vector<Alt, AltAllocator>& vec)
{
    // We'll not observe copy-on-write for now.
    m_allocator = vec.m_allocator;
    ensure_capacity(vec.m_nSize);
    m_nSize = vec.m_nSize;
    for (size_type i = 0; i < m_nSize; ++i)
    {
        m_pArray[i] = (T) vec.m_pArray[i];
    }
    return *this;
}

template <typename T, typename Allocator>
vector<T, Allocator>& vector<T, Allocator>::operator=(const vector<T, Allocator>& vec)
{
    if (this == &vec)
    {
        return *this;
    }

    // We'll not observe copy-on-write for now.
    m_allocator = vec.m_allocator;
    ensure_capacity(vec.m_nSize);
    m_nSize = vec.m_nSize;
    for (size_type i = 0; i < m_nSize; ++i)
    {
        new (m_pArray + i, placement_t()) T(vec.m_pArray[i]);
    }
    return *this;
}


template <typename T, typename Allocator>
typename vector<T, Allocator>::reference vector<T, Allocator>::operator[](size_type n)
{
    return m_pArray[n];
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::const_reference
    vector<T, Allocator>::operator[](size_type n) const
{
    return m_pArray[n];
}

template <typename T, typename Allocator>
void vector<T, Allocator>::pop_back()
{
    m_pArray[m_nSize - 1].~T();
    --m_nSize;
}

template <typename T, typename Allocator>
void vector<T, Allocator>::push_back(const T& value)
{
    ensure_capacity(m_nSize + 1);
    new (m_pArray + m_nSize, placement_t()) T(value);
    ++m_nSize;
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::reverse_iterator vector<T, Allocator>::rbegin()
{
    return reverse_iterator(m_pArray + m_nSize - 1);
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::const_reverse_iterator
    vector<T, Allocator>::rbegin() const
{
    return const_reverse_iterator(m_pArray + m_nSize - 1);
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::reverse_iterator
    vector<T, Allocator>::rend()
{
    return reverse_iterator(m_pArray - 1);
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::const_reverse_iterator
    vector<T, Allocator>::rend() const
{
    return const_reverse_iterator(m_pArray - 1);
}

template <typename T, typename Allocator>
void vector<T, Allocator>::reserve(size_type n)
{
    ensure_capacity(n);
}

template <typename T, typename Allocator>
void vector<T, Allocator>::resize(
    size_type sz,
    const T& c)
{
    for (; m_nSize > sz; m_nSize--)
    {
        m_pArray[m_nSize - 1].~T();
    }
    ensure_capacity(sz);
    for (; m_nSize < sz; m_nSize++)
    {
        new (m_pArray + m_nSize, placement_t()) T(c);
    }
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::size_type vector<T, Allocator>::size() const
{
    return m_nSize;
}

template <typename T, typename Allocator>
void vector<T, Allocator>::swap(vector<T, Allocator>& vec)
{
    std::swap(m_pArray, vec.m_pArray);
    std::swap(m_nSize, vec.m_nSize);
    std::swap(m_nCapacity, vec.m_nCapacity);
    std::swap(m_nCapacity, vec.m_nCapacity);
    std::swap(m_allocator, vec.m_allocator);
}

// =======================================================================================

template <typename T, typename Allocator>
void vector<T, Allocator>::construct_helper(size_type size, const T& value)
{
    ensure_capacity(size);

    for (size_type i = 0; i < size; ++i)
    {
        new (m_pArray + i, placement_t()) T(value);
    }

    m_nSize = size;
}


template <typename T, typename Allocator>
template <typename InputIterator>
void vector<T, Allocator>::construct_helper(InputIterator first, InputIterator last, int_not_an_iterator_tag)
{
    construct_helper(first, last);
}

template <typename T, typename Allocator>
template <typename InputIterator>
void vector<T, Allocator>::construct_helper(InputIterator first, InputIterator last, forward_iterator_tag)
{
    size_type size = iterator_difference(first, last);

    ensure_capacity(size);
    for (size_type i = 0; i < size; ++i)
    {
        new (m_pArray + i, placement_t()) T(*first);
        first++;
    }

    m_nSize = size;
}

// =======================================================================================

template <typename T, typename Allocator>
void vector<T, Allocator>::insert_elements_helper(iterator iter, size_type size, const T& value)
{
    assert(size < max_size());

    // m_pElem could be NULL then m_pArray would be NULL too.
    size_type pos = iter.m_pElem - m_pArray;

    assert(pos <= m_nSize); // <= could insert at end.
    assert(pos >= 0);

    ensure_capacity(m_nSize + size);

    for (int src = m_nSize - 1, dst = m_nSize + size - 1; src >= (int) pos; --src, --dst)
    {
        m_pArray[dst] = m_pArray[src];
    }

    for (size_type i = 0; i < size; ++i)
    {
        new (m_pArray + pos + i, placement_t()) T(value);
    }

    m_nSize += size;
}

template <typename T, typename Allocator>
template <typename InputIterator>
void vector<T, Allocator>::insert_helper(iterator iter, InputIterator first, InputIterator last, int_not_an_iterator_tag)
{
    insert_elements_helper(iter, first, last);
}

template <typename T, typename Allocator>
template <typename InputIterator>
void vector<T, Allocator>::insert_helper(iterator iter, InputIterator first, InputIterator last, forward_iterator_tag)
{
    // m_pElem could be NULL then m_pArray would be NULL too.
    size_type pos = iter.m_pElem - m_pArray;

    assert(pos <= m_nSize); // <= could insert at end.
    assert(pos >= 0);

    size_type size = iterator_difference(first, last);
    assert(size < max_size());

    ensure_capacity(m_nSize + size);

    pointer lst = m_pArray + m_nSize + size - 1;
    for (size_type i = pos; i < m_nSize; ++i)
    {
        *lst-- = m_pArray[i];
    }
    for (size_type i = 0; i < size; ++i, ++first)
    {
        m_pArray[pos + i] = *first;
    }

    m_nSize += size;
}

// =======================================================================================

template <typename T, typename Allocator>
template <typename InputIterator>
void vector<T, Allocator>::assign_helper(InputIterator first, InputIterator last, forward_iterator_tag)
{
    size_type size = iterator_difference(first, last);

    ensure_capacity(size);
    for (size_type i = 0; i < size; ++i)
    {
        m_pArray[i] = *first;
        first++;
    }

    m_nSize = size;
}

template <typename T, typename Allocator>
template <typename InputIterator>
void vector<T, Allocator>::assign_helper(InputIterator first, InputIterator last, int_not_an_iterator_tag)
{
    assign_helper(first, last);
}

// =======================================================================================

template <typename T, typename Allocator>
bool vector<T, Allocator>::ensure_capacity(size_type newCap)
{
    if (newCap <= m_nCapacity)
    {
        return false;
    }

    // Double the alloc capacity based on size.
    size_type allocCap = m_nSize * 2;

    // Is it still not sufficient?
    if (allocCap < newCap)
    {
        allocCap = newCap;
    }

    // Allocate space.
    pointer ptr = m_allocator.allocate(allocCap);

    // Copy over.
    for (size_type i = 0; i < m_nSize; ++i)
    {
        new (ptr + i, placement_t()) T(m_pArray[i]);
    }

    // Deallocate currently allocated space.
    m_allocator.deallocate(m_pArray, m_nCapacity);

    // Update the pointers and capacity;
    m_pArray = ptr;
    m_nCapacity = allocCap;
    return true;
}

} // end of namespace jitstd.



// Implementation of vector iterators

namespace jitstd
{

// iterator
template <typename T, typename Allocator>
vector<T, Allocator>::iterator::iterator()
    : m_pElem(NULL)
{
}

template <typename T, typename Allocator>
vector<T, Allocator>::iterator::iterator(T* ptr)
    : m_pElem(ptr)
{
}

template <typename T, typename Allocator>
vector<T, Allocator>::iterator::iterator(const iterator& it)
    : m_pElem(it.m_pElem)
{
}


template <typename T, typename Allocator>
typename vector<T, Allocator>::iterator& vector<T, Allocator>::iterator::operator++()
{
    ++m_pElem;
    return *this;
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::iterator& vector<T, Allocator>::iterator::operator++(int)
{
    ++m_pElem;
    return *this;
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::iterator& vector<T, Allocator>::iterator::operator--()
{
    --m_pElem;
    return *this;
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::iterator& vector<T, Allocator>::iterator::operator--(int)
{
    --m_pElem;
    return *this;
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::iterator vector<T, Allocator>::iterator::operator+(difference_type n)
{
    return iterator(m_pElem + n);
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::iterator vector<T, Allocator>::iterator::operator-(difference_type n)
{
    return iterator(m_pElem - n);
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::size_type
    vector<T, Allocator>::iterator::operator-(
        const typename vector<T, Allocator>::iterator& that)
{
    return m_pElem - that.m_pElem;
}

template <typename T, typename Allocator>
bool vector<T, Allocator>::iterator::operator==(const iterator& it)
{
    return (m_pElem == it.m_pElem);
}

template <typename T, typename Allocator>
bool vector<T, Allocator>::iterator::operator!=(const iterator& it)
{
    return !operator==(it);
}

template <typename T, typename Allocator>
T& vector<T, Allocator>::iterator::operator*()
{
    return *m_pElem;
}

template <typename T, typename Allocator>
T* vector<T, Allocator>::iterator::operator&()
{
    return &m_pElem;
}

template <typename T, typename Allocator>
vector<T, Allocator>::iterator::operator T*()
{
    return m_pElem;
}

// const_iterator
template <typename T, typename Allocator>
vector<T, Allocator>::const_iterator::const_iterator()
    : m_pElem(NULL)
{
}

template <typename T, typename Allocator>
vector<T, Allocator>::const_iterator::const_iterator(T* ptr)
    : m_pElem(ptr)
{
}

template <typename T, typename Allocator>
vector<T, Allocator>::const_iterator::const_iterator(const const_iterator& it)
    : m_pElem(it.m_pElem)
{
}


template <typename T, typename Allocator>
typename vector<T, Allocator>::const_iterator& vector<T, Allocator>::const_iterator::operator++()
{
    ++m_pElem;
    return *this;
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::const_iterator& vector<T, Allocator>::const_iterator::operator++(int)
{
    ++m_pElem;
    return *this;
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::const_iterator& vector<T, Allocator>::const_iterator::operator--()
{
    --m_pElem;
    return *this;
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::const_iterator& vector<T, Allocator>::const_iterator::operator--(int)
{
    --m_pElem;
    return *this;
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::const_iterator vector<T, Allocator>::const_iterator::operator+(difference_type n)
{
    return const_iterator(m_pElem + n);
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::const_iterator vector<T, Allocator>::const_iterator::operator-(difference_type n)
{
    return const_iterator(m_pElem - n);
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::size_type
    vector<T, Allocator>::const_iterator::operator-(
        const typename vector<T, Allocator>::const_iterator& that)
{
    return m_pElem - that.m_pElem;
}

template <typename T, typename Allocator>
bool vector<T, Allocator>::const_iterator::operator==(const const_iterator& it) const
{
    return (m_pElem == it.m_pElem);
}

template <typename T, typename Allocator>
bool vector<T, Allocator>::const_iterator::operator!=(const const_iterator& it) const
{
    return !operator==(it);
}

template <typename T, typename Allocator>
const T& vector<T, Allocator>::const_iterator::operator*() const
{
    return *m_pElem;
}


template <typename T, typename Allocator>
const T* vector<T, Allocator>::const_iterator::operator&() const
{
    return &m_pElem;
}

template <typename T, typename Allocator>
vector<T, Allocator>::const_iterator::operator const T*() const
{
    return &m_pElem;
}


// reverse_iterator
template <typename T, typename Allocator>
vector<T, Allocator>::reverse_iterator::reverse_iterator()
    : m_pElem(NULL)
{
}

template <typename T, typename Allocator>
vector<T, Allocator>::reverse_iterator::reverse_iterator(T* ptr)
    : m_pElem(ptr)
{
}

template <typename T, typename Allocator>
vector<T, Allocator>::reverse_iterator::reverse_iterator(const reverse_iterator& it)
    : m_pElem(it.m_pElem)
{
}


template <typename T, typename Allocator>
typename vector<T, Allocator>::reverse_iterator& vector<T, Allocator>::reverse_iterator::operator++()
{
    --m_pElem;
    return *this;
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::reverse_iterator& vector<T, Allocator>::reverse_iterator::operator++(int)
{
    --m_pElem;
    return *this;
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::reverse_iterator& vector<T, Allocator>::reverse_iterator::operator--()
{
    ++m_pElem;
    return *this;
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::reverse_iterator& vector<T, Allocator>::reverse_iterator::operator--(int)
{
    ++m_pElem;
    return *this;
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::reverse_iterator vector<T, Allocator>::reverse_iterator::operator+(difference_type n)
{
    return reverse_iterator(m_pElem + n);
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::reverse_iterator vector<T, Allocator>::reverse_iterator::operator-(difference_type n)
{
    return reverse_iterator(m_pElem - n);
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::size_type
    vector<T, Allocator>::reverse_iterator::operator-(
        const typename vector<T, Allocator>::reverse_iterator& that)
{
    return m_pElem - that.m_pElem;
}

template <typename T, typename Allocator>
bool vector<T, Allocator>::reverse_iterator::operator==(const reverse_iterator& it)
{
    return (m_pElem == it.m_pElem);
}

template <typename T, typename Allocator>
bool vector<T, Allocator>::reverse_iterator::operator!=(const reverse_iterator& it)
{
    return !operator==(it);
}

template <typename T, typename Allocator>
T& vector<T, Allocator>::reverse_iterator::operator*()
{
    return *m_pElem;
}

template <typename T, typename Allocator>
T* vector<T, Allocator>::reverse_iterator::operator&()
{
    return &m_pElem;
}

template <typename T, typename Allocator>
vector<T, Allocator>::reverse_iterator::operator T*()
{
    return m_pElem;
}

// const_reverse_iterator
template <typename T, typename Allocator>
vector<T, Allocator>::const_reverse_iterator::const_reverse_iterator()
    : m_pElem(NULL)
{
}

template <typename T, typename Allocator>
vector<T, Allocator>::const_reverse_iterator::const_reverse_iterator(T* ptr)
    : m_pElem(ptr)
{
}

template <typename T, typename Allocator>
vector<T, Allocator>::const_reverse_iterator::const_reverse_iterator(const const_reverse_iterator& it)
    : m_pElem(it.m_pElem)
{
}


template <typename T, typename Allocator>
typename vector<T, Allocator>::const_reverse_iterator& vector<T, Allocator>::const_reverse_iterator::operator++()
{
    --m_pElem;
    return *this;
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::const_reverse_iterator& vector<T, Allocator>::const_reverse_iterator::operator++(int)
{
    --m_pElem;
    return *this;
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::const_reverse_iterator& vector<T, Allocator>::const_reverse_iterator::operator--()
{
    ++m_pElem;
    return *this;
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::const_reverse_iterator& vector<T, Allocator>::const_reverse_iterator::operator--(int)
{
    ++m_pElem;
    return *this;
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::const_reverse_iterator vector<T, Allocator>::const_reverse_iterator::operator+(difference_type n)
{
    return const_reverse_iterator(m_pElem + n);
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::const_reverse_iterator vector<T, Allocator>::const_reverse_iterator::operator-(difference_type n)
{
    return const_reverse_iterator(m_pElem - n);
}

template <typename T, typename Allocator>
typename vector<T, Allocator>::size_type
    vector<T, Allocator>::const_reverse_iterator::operator-(
        const typename vector<T, Allocator>::const_reverse_iterator& that)
{
    return m_pElem - that.m_pElem;
}

template <typename T, typename Allocator>
bool vector<T, Allocator>::const_reverse_iterator::operator==(const const_reverse_iterator& it) const
{
    return (m_pElem == it.m_pElem);
}

template <typename T, typename Allocator>
bool vector<T, Allocator>::const_reverse_iterator::operator!=(const const_reverse_iterator& it) const
{
    return !operator==(it);
}

template <typename T, typename Allocator>
const T& vector<T, Allocator>::const_reverse_iterator::operator*() const
{
    return *m_pElem;
}

template <typename T, typename Allocator>
const T* vector<T, Allocator>::const_reverse_iterator::operator&() const
{
    return &m_pElem;
}

template <typename T, typename Allocator>
vector<T, Allocator>::const_reverse_iterator::operator const T*() const
{
    return &m_pElem;
}

}
