// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _FROZENOBJECTHEAP_H
#define _FROZENOBJECTHEAP_H

#include "appdomain.hpp"

class FrozenObjectHeap
{
public:
    void Init(void* start, size_t size);
    void* Alloc(size_t size);

private:
    uint8_t* m_pStart;
    uint8_t* m_pCurrent;
    size_t m_Size;
};

#endif // _FROZENOBJECTHEAP_H

