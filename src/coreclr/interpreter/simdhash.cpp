// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "simdhash.h"

// Arena allocator stores size_t prefix before each allocation to support realloc
void *dn_simdhash_arenaallocator::arena_alloc(dn_allocator_t *_this, size_t size)
{
    dn_simdhash_arenaallocator *allocator = static_cast<dn_simdhash_arenaallocator *>(_this);
    // Allocate extra space for the size prefix, checking for overflow
    size_t totalSize = size + sizeof(size_t);
    if (totalSize < size)
        return nullptr;
    void *block = allocator->m_arenaAllocator.allocate<uint8_t>(totalSize);
    if (block == nullptr)
        return nullptr;
    // Store the size at the beginning of the block
    *static_cast<size_t *>(block) = size;
    // Return pointer past the size prefix
    return static_cast<uint8_t *>(block) + sizeof(size_t);
}

void *dn_simdhash_arenaallocator::arena_realloc(dn_allocator_t *_this, void *ptr, size_t size)
{
    if (ptr == nullptr)
        return arena_alloc(_this, size);

    // Get the original size from the prefix
    size_t *sizePtr = reinterpret_cast<size_t *>(static_cast<uint8_t *>(ptr) - sizeof(size_t));
    size_t oldSize = *sizePtr;

    if (oldSize >= size)
    {
        // Existing block is large enough
        return ptr;
    }

    // Allocate new block
    void *newBlock = arena_alloc(_this, size);
    if (newBlock == nullptr)
        return nullptr;

    // Copy old data to new block
    size_t copySize = (oldSize < size) ? oldSize : size;
    memcpy(newBlock, ptr, copySize);

    // Note: we don't free the old block since arena allocators don't support individual frees
    return newBlock;
}

void dn_simdhash_arenaallocator::arena_free(dn_allocator_t *_this, void *ptr)
{
    // Arena allocators don't support individual frees - memory is freed when the arena is destroyed
    (void)_this;
    (void)ptr;
}

_dn_allocator_vtable_t dn_simdhash_arenaallocator::vtable = {
    &dn_simdhash_arenaallocator::arena_alloc,
    &dn_simdhash_arenaallocator::arena_realloc,
    &dn_simdhash_arenaallocator::arena_free
};
