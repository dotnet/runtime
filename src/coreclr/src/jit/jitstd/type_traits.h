// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#pragma once

namespace jitstd
{
template <typename T>
struct remove_const
{
    typedef T type;
};

template <typename T>
struct remove_const<const T>
{
    typedef T type;
};

template <typename T>
struct remove_volatile
{
    typedef T type;
};

template <typename T>
struct remove_volatile<volatile T>
{
    typedef T type;
};

template <typename T>
struct remove_cv : remove_const<typename remove_volatile<T>::type>
{
};

template <typename T>
struct remove_reference
{
    typedef T type;
};

template <typename T>
struct remove_reference<T&>
{
    typedef T type;
};

template <typename T>
struct remove_reference<T&&>
{
    typedef T type;
};

template <typename T>
struct is_lvalue_reference
{
    enum { value = false };
};

template <typename T>
struct is_lvalue_reference<T&>
{
    enum { value = true };
};

template <typename T>
struct is_unqualified_pointer
{
    enum { value = false };
};

template <typename T>
struct is_unqualified_pointer<T*>
{
    enum { value = true };
};

template <typename T>
struct is_pointer : is_unqualified_pointer<typename remove_cv<T>::type>
{
};

template <typename T>
struct is_integral
{
    enum { value = false };
};

template<>
struct is_integral<bool>
{
    enum { value = true };
};

template<>
struct is_integral<char>
{
    enum { value = true };
};

template<>
struct is_integral<unsigned char>
{
    enum { value = true };
};

template<>
struct is_integral<signed char>
{
    enum { value = true };
};

template<>
struct is_integral<unsigned short>
{
    enum { value = true };
};

template<>
struct is_integral<signed short>
{
    enum { value = true };
};

template<>
struct is_integral<unsigned int>
{
    enum { value = true };
};

template<>
struct is_integral<signed int>
{
    enum { value = true };
};

template<>
struct is_integral<unsigned __int64>
{
    enum { value = true };
};

template<>
struct is_integral<signed __int64>
{
    enum { value = true };
};


template<bool Pred, typename Type1, typename Type2>
struct conditional
{
};

template<typename Type1, typename Type2>
struct conditional<true, Type1, Type2>
{
    typedef Type1 type;
};

template<typename Type1, typename Type2>
struct conditional<false, Type1, Type2>
{
    typedef Type2 type;
};

template<typename Type1>
struct make_unsigned
{
};

template<>
struct make_unsigned<int>
{
    typedef unsigned int type;
};

#ifndef _HOST_UNIX_

template<>
struct make_unsigned<long>
{
    typedef unsigned long type;
};

#endif // !_HOST_UNIX_

template<>
struct make_unsigned<__int64>
{
    typedef unsigned __int64 type;
};

template<>
struct make_unsigned<size_t>
{
    typedef size_t type;
};

template<typename Type1>
struct make_signed
{
};

template<>
struct make_signed<unsigned int>
{
    typedef signed int type;
};

#ifndef _HOST_UNIX_

template<>
struct make_signed<unsigned long>
{
    typedef signed long type;
};

#endif // !_HOST_UNIX_

template<>
struct make_signed<unsigned __int64>
{
    typedef signed __int64 type;
};

} // namespace jitstd
