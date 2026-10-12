// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef HOST_WINDOWS
#include <windows.h>
#endif // HOST_WINDOWS

#include "minipal.h"
#include <minipal/log.h>

#include <assert.h>
#include <utility>

void VMToOSInterface::MemoryMappingHolder::Assign(MemoryMapHandle mapping)
{
    if (m_mapping != mapping)
    {
        Reset();
        m_mapping = mapping;
    }
}

void VMToOSInterface::MemoryMappingHolder::Reset()
{
    MemoryMapHandle mapping = m_mapping;
    m_mapping = InvalidMemoryMapHandle;

    if (mapping != InvalidMemoryMapHandle && !CloseMemoryMapping(mapping))
    {
        int32_t error = GetLastMemoryMappingError();
        minipal_log_print_error("Closing memory mapping failed with error 0x%08x.\n", static_cast<unsigned int>(error));
    }
}

void VMToOSInterface::MemoryMappedViewHolder::Assign(void* address, size_t size)
{
    assert((address == nullptr) == (size == 0));

    if (m_address != address)
    {
        Reset();
    }

    m_address = address;
    m_size = size;
}

void VMToOSInterface::MemoryMappedViewHolder::Swap(MemoryMappedViewHolder& other)
{
    std::swap(m_address, other.m_address);
    std::swap(m_size, other.m_size);
}

void VMToOSInterface::MemoryMappedViewHolder::Reset()
{
    void* address = m_address;
    size_t size = m_size;
    m_address = nullptr;
    m_size = 0;

    if (address != nullptr && UnmapMemory(address, size) != 0)
    {
        int32_t error = GetLastMemoryMappingError();
        minipal_log_print_error("Unmapping memory view failed with error 0x%08x.\n", static_cast<unsigned int>(error));
    }
}

void VMToOSInterface::MemoryReservationHolder::Assign(void* address, size_t size)
{
    assert((address == nullptr) == (size == 0));
    Reset();
    m_address = address;
    m_size = size;
}

void VMToOSInterface::MemoryReservationHolder::Reset()
{
    void* address = m_address;
    size_t size = m_size;
    m_address = nullptr;
    m_size = 0;

    if (address != nullptr && !ReleaseMemoryReservation(address, size))
    {
        int32_t error = GetLastMemoryMappingError();
        minipal_log_print_error("Releasing memory reservation failed with error 0x%08x.\n", static_cast<unsigned int>(error));
    }
}

void* VMToOSInterface::MemoryReservationHolder::ExtractPrefix(size_t size)
{
    assert(size != 0 && size <= m_size);
    if (size < m_size && !SplitMemoryPlaceholder(m_address, size))
        return nullptr;

    void* address = m_address;
    m_size -= size;
    m_address = m_size == 0 ? nullptr : static_cast<char*>(m_address) + size;
    return address;
}
