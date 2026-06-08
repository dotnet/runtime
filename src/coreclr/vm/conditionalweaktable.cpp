// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include "conditionalweaktable.h"
#include "gchandleutilities.h"

#ifdef DACCESS_COMPILE
#include "../debug/daccess/gcinterface.dac.h"
#endif // DACCESS_COMPILE

bool ConditionalWeakTableContainerObject::TryGetValue(OBJECTREF key, OBJECTREF* value)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
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
        const Entry& entry = entries[entriesIndex];
        if (entry.HashCode == hashCode && ObjectFromHandle(entry.depHnd) == key)
        {
#ifdef DACCESS_COMPILE
            // In the DACCESS_COMPILE, the handle helper is directly accessible.
            *value = GetDependentHandleSecondary(entry.depHnd);
#else
            IGCHandleManager* mgr = GCHandleUtilities::GetGCHandleManager();
            *value = ObjectToOBJECTREF(mgr->GetDependentHandleSecondary(entry.depHnd));
#endif // !DACCESS_COMPILE
            return true;
        }
    }

    *value = nullptr;
    return false;
}
