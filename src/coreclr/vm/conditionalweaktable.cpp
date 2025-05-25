// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include "conditionalweaktable.h"
#include "gchandleutilities.h"
#include "../debug/daccess/gcinterface.dac.h"

bool ConditionalWeakTableContainerObject::TryGetValue(OBJECTREF key, OBJECTREF* value)
{
    STANDARD_VM_CONTRACT;
    SUPPORTS_DAC;
    _ASSERTE(key != nullptr && value != nullptr);

    INT32 hashCode = key->TryGetHashCode();

    if (hashCode == 0)
    {
        *value = nullptr;
        return false;
    }

    hashCode &= INT32_MAX;
    int bucket = hashCode & (_buckets->GetNumComponents() - 1);
    PTR_int32_t buckets = _buckets->GetDirectPointerToNonObjectElements();
    DPTR(Entry) entries = _entries->GetDirectPointerToNonObjectElements();
    for (int entriesIndex = buckets[bucket]; entriesIndex != -1; entriesIndex = entries[entriesIndex].Next)
    {
        if (entries[entriesIndex].HashCode == hashCode && ObjectFromHandle(entries[entriesIndex].depHnd) == key)
        {
            *value = HndGetHandleExtraInfo(entries[entriesIndex].depHnd);
            return true;
        }
    }

    *value = nullptr;
    return false;
}
