// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#pragma once

namespace jitstd
{

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

} // end of namespace jitstd.
