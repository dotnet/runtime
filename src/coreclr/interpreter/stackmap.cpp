// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "gcinfoencoder.h" // for GcSlotFlags

#include "interpreter.h"
#include "stackmap.h"

#include "failures.h"
#include "simdhash.h"

void
dn_simdhash_assert_fail (const char* file, int line, const char* condition) {
#if DEBUG
    assertAbort(condition, file, line);
#else
    NO_WAY(condition);
#endif
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
        newCapacity = numGcPtrs;

    // Allocate enough space in case all the offsets in the buffer are GC pointers
    m_slots = (InterpreterStackMapSlot *)malloc(sizeof(InterpreterStackMapSlot) * newCapacity);

    for (unsigned i = 0; m_slotCount < numGcPtrs; i++) {
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
}
