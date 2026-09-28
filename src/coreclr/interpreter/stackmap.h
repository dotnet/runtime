// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "interpalloc.h"

struct InterpreterStackMapSlot
{
    unsigned m_offsetBytes = 0;
    unsigned m_gcSlotFlags = 0;
};

class InterpreterStackMap
{
    void PopulateStackMap (ICorJitInfo* jitInfo, CORINFO_CLASS_HANDLE classHandle, InterpAllocator allocator);

public:
    unsigned m_slotCount;
    InterpreterStackMapSlot* m_slots;

    InterpreterStackMap (ICorJitInfo* jitInfo, CORINFO_CLASS_HANDLE classHandle, InterpAllocator allocator)
        : m_slotCount(0)
        , m_slots(nullptr)
    {
        PopulateStackMap(jitInfo, classHandle, allocator);
    }
};
