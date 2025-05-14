// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef CONDITIONAL_WEAK_TABLE_H
#define CONDITIONAL_WEAK_TABLE_H

#include "object.h"

class ConditionalWeakTableContainerObject;
class ConditionalWeakTableObject;

#ifdef USE_CHECKED_OBJECTREFS
typedef REF<ConditionalWeakTableContainerObject> CONDITIONAL_WEAK_TABLE_CONTAINER_REF;
typedef REF<ConditionalWeakTableObject> CONDITIONAL_WEAK_TABLE_REF;
#else
typedef DPTR(ConditionalWeakTableContainerObject) CONDITIONAL_WEAK_TABLE_CONTAINER_REF;
typedef DPTR(ConditionalWeakTableObject) CONDITIONAL_WEAK_TABLE_REF;
#endif

class ConditionalWeakTableContainerObject final : public Object
{
    friend class CoreLibBinder;
    struct Entry
    {
        int32_t HashCode;
        int32_t Next;
        OBJECTHANDLE depHnd;
    };

#ifdef USE_CHECKED_OBJECTREFS
    using ENTRYARRAYREF = REF<Array<Entry>>;
#else
    using ENTRYARRAYREF = DPTR(Array<Entry>);
#endif

    CONDITIONAL_WEAK_TABLE_REF _parent;
    I4ARRAYREF _buckets;
    ENTRYARRAYREF _entries;

public:
#ifdef DACCESS_COMPILE
    bool TryGetValue(OBJECTREF key, OBJECTREF* value);
#endif
};

class ConditionalWeakTableObject final : public Object
{
    friend class CoreLibBinder;
    OBJECTREF _lock;
    VolatilePtr<ConditionalWeakTableContainerObject, CONDITIONAL_WEAK_TABLE_CONTAINER_REF> _container;
public:
#ifdef DACCESS_COMPILE
    // Currently, we only use this for access from the DAC, so we don't need to worry about
    // locking or tracking the active enumerator count.
    // If we need to use this in a context where the runtime isn't suspended, we need to add
    // the locking and tracking support.
    template<typename TKey, typename TValue>
    bool TryGetValue(TKey key, TValue* value)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(key != nullptr && value != nullptr);
        *value = nullptr;
        OBJECTREF valueObj = nullptr;
        bool found = _container->TryGetValue((OBJECTREF)key, &valueObj);
        if (found)
        {
            *value = (TValue)valueObj;
        }

        return found;
    }
#endif
};

#endif // CONDITIONAL_WEAK_TABLE_H
