// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



#pragma once

#include "type_traits.h"
#include <stdio.h>

namespace jitstd
{
template<typename Type>
class hash
{
public:
    size_t operator()(const Type& val) const
    {
        div_t qrem = ::div((int)(size_t) val, 127773);
        qrem.rem = 16807 * qrem.rem - 2836 * qrem.quot;
        if (qrem.rem < 0)
        {
            qrem.rem += 2147483647;
        }
        return ((size_t) qrem.rem);
    }
};

template<>
class hash<int>
{
public:
    size_t operator()(const int& val) const
    {
        return val;
    }
};

template<>
class hash<unsigned __int64>
{
private:
    typedef unsigned __int64 Type;

public:
    size_t operator()(const Type& val) const
    {
        return (hash<int>()((int)(val & 0xffffffffUL)) ^ hash<int>()((int)(val >> 32)));
    }
};

template<>
class hash<__int64>
{
private:
    typedef __int64 Type;

public:
    size_t operator()(const Type& val) const
    {
        return (hash<unsigned __int64>()((unsigned __int64) val));
    }
};

template<typename Type>
class hash<Type*>
{
private:
    typedef typename conditional<sizeof (Type*) <= sizeof (int), int, __int64>::type TInteger;
public:
    size_t operator()(const Type* val) const
    {
        return (hash<TInteger>()((TInteger) val));
    }
};

template<>
class hash<float>
{
private:
    typedef float Type;
public:
    size_t operator()(const Type& val) const
    {
        unsigned long bits = *(unsigned long*) &val;
        return (hash<unsigned long>()(bits == 0x80000000 ? 0 : bits));
    }
};

template<>
class hash<double>
{
public:
    typedef double Type;
    size_t operator()(const Type& val) const
    {
        unsigned __int64 bits = *(unsigned __int64*)&val;
        return (hash<unsigned __int64>()((bits & (((unsigned __int64) -1) >> 1)) == 0 ? 0 : bits));
    }
};

}
