// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



#pragma once

namespace jitstd
{

template <typename InputIterator, typename CompareValue>
InputIterator find(InputIterator first, InputIterator last,
                   const CompareValue& value)
{
    for (; first != last; ++first)
    {
        if (*first == value)
        {
            return first;
        }
    }
    return last;
}

template <typename InputIterator, typename Pred>
InputIterator find_if(InputIterator first, InputIterator last, const Pred& pred)
{
    for (; first != last; ++first)
    {
        if (pred(*first))
        {
            return first;
        }
    }
    return last;
}

template<typename InputIterator, typename Function>
Function for_each(InputIterator first, InputIterator last, Function func)
{
    for (; first != last; ++first)
    {
        func(*first);
    }
    return func;
}

}
