// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#pragma once

namespace jitstd
{

template <class Category, class T, class Distance = ptrdiff_t, class Pointer = T*, class Reference = T&>
struct iterator
{
    typedef T value_type;
    typedef Distance difference_type;
    typedef Pointer pointer;
    typedef Reference reference;
    typedef Category iterator_category;
};

struct input_iterator_tag
{
};

struct forward_iterator_tag : public input_iterator_tag
{
};

struct bidirectional_iterator_tag : public forward_iterator_tag
{
};

struct random_access_iterator_tag : public bidirectional_iterator_tag
{
};

struct int_not_an_iterator_tag
{
};

template <typename Iterator>
struct iterator_traits
{
    typedef typename Iterator::difference_type difference_type;
    typedef typename Iterator::value_type value_type;
    typedef typename Iterator::pointer pointer;
    typedef typename Iterator::reference reference;
    typedef typename Iterator::iterator_category iterator_category;
};

template <typename T>
struct iterator_traits<T*>
{
    typedef ptrdiff_t difference_type;
    typedef T value_type;
    typedef T* pointer;
    typedef T& reference;
    typedef random_access_iterator_tag iterator_category;
};

template <typename T>
struct iterator_traits<const T*>
{
    typedef ptrdiff_t difference_type;
    typedef T value_type;
    typedef const T* pointer;
    typedef const T& reference;
    typedef random_access_iterator_tag iterator_category;
};

template<>
struct iterator_traits<bool>
{
    typedef int_not_an_iterator_tag iterator_category;
};

template<>
struct iterator_traits<char>
{
    typedef int_not_an_iterator_tag iterator_category;
};

template<>
struct iterator_traits<signed char>
{
    typedef int_not_an_iterator_tag iterator_category;
};

template<>
struct iterator_traits<unsigned char>
{
    typedef int_not_an_iterator_tag iterator_category;
};

template<>
struct iterator_traits<short>
{
    typedef int_not_an_iterator_tag iterator_category;
};

template<>
struct iterator_traits<unsigned short>
{
    typedef int_not_an_iterator_tag iterator_category;
};

template<>
struct iterator_traits<int>
{
    typedef int_not_an_iterator_tag iterator_category;
};

template<>
struct iterator_traits<unsigned int>
{
    typedef int_not_an_iterator_tag iterator_category;
};

template<>
struct iterator_traits<__int64>
{
    typedef int_not_an_iterator_tag iterator_category;
};

template<>
struct iterator_traits<unsigned __int64>
{
    typedef int_not_an_iterator_tag iterator_category;
};

namespace util
{
template<class Iterator>
inline
typename iterator_traits<Iterator>::iterator_category
    iterator_category(const Iterator&)
{
    typename iterator_traits<Iterator>::iterator_category categ;
    return categ;
}
} // end of namespace util.

} // end of namespace jitstd.
