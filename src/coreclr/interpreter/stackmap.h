// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

struct InterpreterStackMapSlot
{
    unsigned m_offsetBytes;
    unsigned m_gcSlotFlags;
};

class InterpreterStackMap
{
    void PopulateStackMap (ICorJitInfo* jitInfo, CORINFO_CLASS_HANDLE classHandle);

public:
    unsigned m_slotCount;
    InterpreterStackMapSlot* m_slots;

    InterpreterStackMap (ICorJitInfo* jitInfo, CORINFO_CLASS_HANDLE classHandle)
        : m_slotCount(0)
        , m_slots(nullptr)
    {
        PopulateStackMap(jitInfo, classHandle);
    }

    ~InterpreterStackMap ()
    {
        if (m_slots)
            free(m_slots);
        m_slots = nullptr;
    }
};
