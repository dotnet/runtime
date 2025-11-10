// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _SIMDHASH_H_
#define _SIMDHASH_H_

#include "failures.h"
#include "../../native/containers/dn-simdhash.h"
#include "../../native/containers/dn-simdhash-specializations.h"
#include "../../native/containers/dn-simdhash-utils.h"

class dn_simdhash_ptr_ptr_holder
{
public:
    dn_simdhash_ptr_ptr_foreach_func ValueDestroyCallback;

private:
    dn_simdhash_ptr_ptr_t *Value;

    void free_hash_and_values()
    {
        if (Value == nullptr)
            return;
        if (ValueDestroyCallback)
            dn_simdhash_ptr_ptr_foreach(Value, ValueDestroyCallback, nullptr);
        dn_simdhash_free(Value);
        Value = nullptr;
    }

public:
    dn_simdhash_ptr_ptr_holder(dn_simdhash_ptr_ptr_foreach_func valueDestroyCallback = nullptr)
        : ValueDestroyCallback(valueDestroyCallback)
        , Value(nullptr)
    {
    }

    dn_simdhash_ptr_ptr_t* GetValue()
    {
        if (!Value)
            Value = dn_simdhash_ptr_ptr_new(0, nullptr);
        return Value;
    }

    dn_simdhash_ptr_ptr_holder(const dn_simdhash_ptr_ptr_holder&) = delete;
    dn_simdhash_ptr_ptr_holder& operator=(const dn_simdhash_ptr_ptr_holder&) = delete;
    dn_simdhash_ptr_ptr_holder(dn_simdhash_ptr_ptr_holder&& other)
    {
        Value = other.Value;
        other.Value = nullptr;
    }
    dn_simdhash_ptr_ptr_holder& operator=(dn_simdhash_ptr_ptr_holder&& other)
    {
        if (this != &other)
        {
            free_hash_and_values();
            Value = other.Value;
            other.Value = nullptr;
        }
        return *this;
    }

    ~dn_simdhash_ptr_ptr_holder()
    {
        free_hash_and_values();
    }
};

// Asserts that no error occurred during a simdhash add, but does not mind if no new item was inserted
inline void checkNoError(dn_simdhash_add_result result)
{
    if (result == DN_SIMDHASH_OUT_OF_MEMORY)
        NOMEM();
    else if (result < 0)
        NO_WAY("Internal error in simdhash");
}

// Asserts that a new item was successfully inserted into the simdhash
inline void checkAddedNew(dn_simdhash_add_result result)
{
    if (result == DN_SIMDHASH_OUT_OF_MEMORY)
        NOMEM();
    else if (result != DN_SIMDHASH_ADD_INSERTED)
        NO_WAY("Failed to add new item into simdhash");
}

#endif // _SIMDHASH_H_
