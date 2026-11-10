// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _SIMDHASH_H_
#define _SIMDHASH_H_

#include "failures.h"
#include "../../native/containers/dn-simdhash.h"
#include "../../native/containers/dn-simdhash-specializations.h"
#include "../../native/containers/dn-simdhash-utils.h"
#include "interpalloc.h"

struct dn_simdhash_arenaallocator final : public dn_allocator_t
{
    InterpAllocator m_arenaAllocator;
    static _dn_allocator_vtable_t vtable;

    static void *arena_alloc(dn_allocator_t *_this, size_t size);
    static void *arena_realloc(dn_allocator_t *_this, void *ptr, size_t size);
    static void arena_free(dn_allocator_t *_this, void *ptr);
public:
    dn_simdhash_arenaallocator(InterpAllocator arenaAllocator)
        : m_arenaAllocator(arenaAllocator)
    {
        this->_vtable = &vtable;
    }
};

class dn_simdhash_ptr_ptr_holder
{
    dn_simdhash_ptr_ptr_t *Value;
    dn_simdhash_arenaallocator *ArenaAllocator;

public:
    dn_simdhash_ptr_ptr_holder(InterpAllocator arenaAllocator)
        : Value(nullptr)
        , ArenaAllocator(new (arenaAllocator) dn_simdhash_arenaallocator(arenaAllocator))
    {
    }

    dn_simdhash_ptr_ptr_t* GetValue()
    {
        if (!Value)
            Value = dn_simdhash_ptr_ptr_new(0, ArenaAllocator);

        if (Value == nullptr)
            NOMEM();

        return Value;
    }

    bool HasValue()
    {
        return Value != nullptr;
    }

    dn_simdhash_ptr_ptr_holder(const dn_simdhash_ptr_ptr_holder&) = delete;
    dn_simdhash_ptr_ptr_holder& operator=(const dn_simdhash_ptr_ptr_holder&) = delete;
    dn_simdhash_ptr_ptr_holder(dn_simdhash_ptr_ptr_holder&& other)
    {
        Value = other.Value;
        ArenaAllocator = other.ArenaAllocator;
        other.Value = nullptr;
        other.ArenaAllocator = nullptr;
    }
    dn_simdhash_ptr_ptr_holder& operator=(dn_simdhash_ptr_ptr_holder&& other)
    {
        if (this != &other)
        {
            Value = other.Value;
            ArenaAllocator = other.ArenaAllocator;
            other.Value = nullptr;
            other.ArenaAllocator = nullptr;
        }
        return *this;
    }
};

class dn_simdhash_u32_ptr_holder
{
    dn_simdhash_u32_ptr_t *Value;
    dn_simdhash_arenaallocator *ArenaAllocator;

public:
    dn_simdhash_u32_ptr_holder(InterpAllocator arenaAllocator)
        : Value(nullptr)
        , ArenaAllocator(new (arenaAllocator) dn_simdhash_arenaallocator(arenaAllocator))
    {
    }

    dn_simdhash_u32_ptr_t* GetValue()
    {
        if (!Value)
            Value = dn_simdhash_u32_ptr_new(0, ArenaAllocator);

        if (Value == nullptr)
            NOMEM();

        return Value;
    }

    bool HasValue()
    {
        return Value != nullptr;
    }

    dn_simdhash_u32_ptr_holder(const dn_simdhash_u32_ptr_holder&) = delete;
    dn_simdhash_u32_ptr_holder& operator=(const dn_simdhash_u32_ptr_holder&) = delete;
    dn_simdhash_u32_ptr_holder(dn_simdhash_u32_ptr_holder&& other)
    {
        Value = other.Value;
        ArenaAllocator = other.ArenaAllocator;
        other.Value = nullptr;
        other.ArenaAllocator = nullptr;
    }
    dn_simdhash_u32_ptr_holder& operator=(dn_simdhash_u32_ptr_holder&& other)
    {
        if (this != &other)
        {
            Value = other.Value;
            ArenaAllocator = other.ArenaAllocator;
            other.Value = nullptr;
            other.ArenaAllocator = nullptr;
        }
        return *this;
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
