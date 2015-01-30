//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//



#pragma once

namespace jitstd
{

template <typename T>
void swap(T& a, T& b)
{
    T t(a);
    a = b;
    b = t;
}

template <typename Arg, typename Result>
struct unary_function
{
    typedef Arg argument_type;
    typedef Result result_type;
};

template <typename Arg1, typename Arg2, typename Result>
struct binary_function
{
    typedef Arg1 first_argument_type;
    typedef Arg2 second_argument_type;
    typedef Result result_type;
};

template <typename T>
struct greater : binary_function<T, T, bool>
{
    bool operator()(const T& lhs, const T& rhs) const
    {
        return lhs > rhs;
    }
};

template <typename T>
struct equal_to : binary_function<T, T, bool>
{
    bool operator()(const T& lhs, const T& rhs) const
    {
        return lhs == rhs;
    }
};

template <typename T>
struct identity : unary_function<T, T>
{
    const T& operator()(const T& op) const
    {
        return op;
    }
};

} // end of namespace jitstd.
