// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "gcinfoencoder.h" // for GcSlotFlags

#include "interpreter.h"
#include "stackmap.h"

extern "C" {
    #include "../../native/containers/dn-simdhash.h"
    #include "../../native/containers/dn-simdhash-specializations.h"

    void assertAbort(const char* why, const char* file, unsigned line);

    void
    dn_simdhash_assert_fail (const char* file, int line, const char* condition);

    void
    dn_simdhash_assert_fail (const char* file, int line, const char* condition) {
        assertAbort(condition, file, line);
    }
}

thread_local dn_simdhash_ptr_ptr_t *t_sharedStackMapLookup = nullptr;

InterpreterStackMap* GetInterpreterStackMap(ICorJitInfo* jitInfo, CORINFO_CLASS_HANDLE classHandle)
{
    InterpreterStackMap* result = nullptr;
    if (!t_sharedStackMapLookup)
        t_sharedStackMapLookup = dn_simdhash_ptr_ptr_new(0, nullptr);
    if (!dn_simdhash_ptr_ptr_try_get_value(t_sharedStackMapLookup, classHandle, (void **)&result))
    {
        result = new InterpreterStackMap(jitInfo, classHandle);
        dn_simdhash_ptr_ptr_try_add(t_sharedStackMapLookup, classHandle, result);
    }
    return result;
}

void InterpreterStackMap::PopulateStackMap(ICorJitInfo* jitInfo, CORINFO_CLASS_HANDLE classHandle)
{
    unsigned size = jitInfo->getClassSize(classHandle);
    // getClassGClayout assumes it's given a buffer of exactly this size
    unsigned maxGcPtrs = (size + sizeof(void *) - 1) / sizeof(void *);
    if (maxGcPtrs < 1)
        return;

    uint8_t *gcPtrs = (uint8_t *)alloca(maxGcPtrs);
    unsigned numGcPtrs = jitInfo->getClassGClayout(classHandle, gcPtrs),
        newCapacity = m_slotCount + numGcPtrs;

    // Allocate enough space in case all the offsets in the buffer are GC pointers
    m_slots = (InterpreterStackMapSlot *)malloc(sizeof(InterpreterStackMapSlot) * newCapacity);

    for (unsigned i = 0; i < numGcPtrs; i++) {
        GcSlotFlags flags;

        switch (gcPtrs[i]) {
            case TYPE_GC_NONE:
            case TYPE_GC_OTHER:
                continue;
            case TYPE_GC_BYREF:
                flags = GC_SLOT_INTERIOR;
                break;
            case TYPE_GC_REF:
                flags = GC_SLOT_BASE;
                break;
            default:
                assert(false);
                continue;
        }

        unsigned slotOffset = (sizeof(void *) * i);
        m_slots[m_slotCount++] = { slotOffset, (unsigned)flags };
    }

    // Shrink our allocation based on the number of slots we actually recorded
    unsigned finalSize = sizeof(InterpreterStackMapSlot) * m_slotCount;
    if (finalSize == 0)
        finalSize = sizeof(InterpreterStackMapSlot);
    m_slots = (InterpreterStackMapSlot *)realloc(m_slots, finalSize);
}
