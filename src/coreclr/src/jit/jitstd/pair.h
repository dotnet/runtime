// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



#pragma once

namespace jitstd
{
template <typename Type1, typename Type2>
class pair
{
public:
    Type1 first;
    Type2 second;

    pair(const Type1& fst, const Type2& sec)
        : first(fst)
        , second(sec)
    {
    }

    template <typename AltType1, typename AltType2>
    pair(const AltType1& fst, const AltType2& sec)
        : first((Type1) fst)
        , second((Type2) sec)
    {
    }

    template <typename AltType1, typename AltType2>
    pair(const pair<AltType1, AltType2>& that)
        : first((Type1) that.first)
        , second((Type2) that.second)
    {
    }

    pair(const pair& that)
        : first(that.first)
        , second(that.second)
    {
    }

    template <typename AltType1, typename AltType2>
    const pair<Type1, Type2>& operator=(const pair<AltType1, AltType2>& pair)
    {
        first = pair.first;
        second = pair.second;
        return *this;
    }

    bool operator==(const pair<Type1, Type2>& other) const
    {
        return (other.first == first && other.second == second);
    }
};
}
