// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

namespace jitstd
{

struct placement_t
{
};

}

inline void* operator new(size_t sz, void* p, jitstd::placement_t /* syntax_difference */)
{
    return p;
}
