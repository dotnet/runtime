// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "new.h"

namespace jitstd
{

template <typename T>
class allocator;

template <>
class allocator<void>
{
public:
    typedef size_t size_type;
    typedef ptrdiff_t difference_type;
    typedef void* pointer;
    typedef const void* const_pointer;
    typedef void value_type;

    template <typename U>
    struct rebind
    {
        typedef allocator<U> allocator;
    };

private:
    allocator();

public:
    inline allocator(CompAllocator alloc);

    template <typename U>
    inline allocator(const allocator<U>& alloc);

    inline allocator(const allocator& alloc);

    template <typename U>
    inline allocator& operator=(const allocator<U>& alloc);

private:
    CompAllocator m_alloc;
    template <typename U>
    friend class allocator;
};

allocator<void>::allocator(CompAllocator alloc)
    : m_alloc(alloc)
{
}

allocator<void>::allocator(const allocator& alloc)
    : m_alloc(alloc.m_alloc)
{
}

template <typename U>
allocator<void>::allocator(const allocator<U>& alloc)
    : m_alloc(alloc.m_alloc)
{
}

template <typename U>
allocator<void>& allocator<void>::operator=(const allocator<U>& alloc)
{
    m_alloc = alloc.m_alloc;
    return *this;
}

template <typename T>
class allocator
{
public:
    typedef size_t size_type;
    typedef ptrdiff_t difference_type;
    typedef T* pointer;
    typedef T& reference;
    typedef const T* const_pointer;
    typedef const T& const_reference;
    typedef T value_type;

private:
    allocator();
public:
    allocator(CompAllocator alloc);

    template <typename U>
    allocator(const allocator<U>& alloc);

    allocator(const allocator& alloc);

    template <typename U>
    allocator& operator=(const allocator<U>& alloc);

    pointer address(reference val);
    const_pointer address(const_reference val) const;
    pointer allocate(size_type count, allocator<void>::const_pointer hint = nullptr);
    void construct(pointer ptr, const_reference val);
    void deallocate(pointer ptr, size_type size);
    void destroy(pointer ptr);
    size_type max_size() const;
    template <typename U>
    struct rebind
    {
        typedef allocator<U> allocator;
    };

private:
    CompAllocator m_alloc;
    template <typename U>
    friend class allocator;
};

} // end of namespace jitstd


namespace jitstd
{

template <typename T>
allocator<T>::allocator(CompAllocator alloc)
    : m_alloc(alloc)
{
}

template <typename T>
template <typename U>
allocator<T>::allocator(const allocator<U>& alloc)
    : m_alloc(alloc.m_alloc)
{
}

template <typename T>
allocator<T>::allocator(const allocator<T>& alloc)
    : m_alloc(alloc.m_alloc)
{
}

template <typename T>
template <typename U>
allocator<T>& allocator<T>::operator=(const allocator<U>& alloc)
{
    m_alloc = alloc.m_alloc;
    return *this;
}

template <typename T>
typename allocator<T>::pointer allocator<T>::address(reference val)
{
    return &val;
}

template <typename T>
typename allocator<T>::const_pointer allocator<T>::address(const_reference val) const
{
    return &val;
}

template <typename T>
T* allocator<T>::allocate(size_type count, allocator<void>::const_pointer hint)
{
    return m_alloc.allocate<value_type>(count);
}

template <typename T>
void allocator<T>::construct(pointer ptr, const_reference val)
{
    new (ptr, placement_t()) value_type(val);
}

template <typename T>
void allocator<T>::deallocate(pointer ptr, size_type size)
{
    m_alloc.deallocate(ptr);
}

template <typename T>
void allocator<T>::destroy(pointer ptr)
{
    ptr->~T();
}

template <typename T>
typename allocator<T>::size_type allocator<T>::max_size() const
{
    return (size_type) -1;
}

} // end of namespace jitstd
