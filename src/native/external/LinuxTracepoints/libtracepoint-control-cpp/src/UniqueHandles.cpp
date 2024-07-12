// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include <tracepoint/TracepointSession.h>

#include <unistd.h>
#include <sys/mman.h>

using namespace tracepoint_control;

TracepointSession::unique_fd::~unique_fd()
{
    reset(-1);
}

TracepointSession::unique_fd::unique_fd() noexcept
    : m_fd(-1)
{
    return;
}

TracepointSession::unique_fd::unique_fd(int fd) noexcept
    : m_fd(fd)
{
    return;
}

TracepointSession::unique_fd::unique_fd(unique_fd&& other) noexcept
    : m_fd(other.m_fd)
{
    other.m_fd = -1;
}

TracepointSession::unique_fd&
TracepointSession::unique_fd::operator=(unique_fd&& other) noexcept
{
    int fd = other.m_fd;
    other.m_fd = -1;
    reset(fd);
    return *this;
}

TracepointSession::unique_fd::operator bool() const noexcept
{
    return m_fd != -1;
}

void
TracepointSession::unique_fd::reset() noexcept
{
    reset(-1);
}

void
TracepointSession::unique_fd::reset(int fd) noexcept
{
    if (m_fd != -1)
    {
        close(m_fd);
    }
    m_fd = fd;
}

int
TracepointSession::unique_fd::get() const noexcept
{
    return m_fd;
}

TracepointSession::unique_mmap::~unique_mmap()
{
    reset(MAP_FAILED, 0);
}

TracepointSession::unique_mmap::unique_mmap() noexcept
    : m_addr(MAP_FAILED)
    , m_size(0)
{
    return;
}

TracepointSession::unique_mmap::unique_mmap(void* addr, size_t size) noexcept
    : m_addr(addr)
    , m_size(size)
{
    return;
}

TracepointSession::unique_mmap::unique_mmap(unique_mmap&& other) noexcept
    : m_addr(other.m_addr)
    , m_size(other.m_size)
{
    other.m_addr = MAP_FAILED;
    other.m_size = 0;
}

TracepointSession::unique_mmap&
TracepointSession::unique_mmap::operator=(unique_mmap&& other) noexcept
{
    void* addr = other.m_addr;
    size_t size = other.m_size;
    other.m_addr = MAP_FAILED;
    other.m_size = 0;
    reset(addr, size);
    return *this;
}

TracepointSession::unique_mmap::operator bool() const noexcept
{
    return m_addr != MAP_FAILED;
}

void
TracepointSession::unique_mmap::reset() noexcept
{
    reset(MAP_FAILED, 0);
}

void
TracepointSession::unique_mmap::reset(void* addr, size_t size) noexcept
{
    if (m_addr != MAP_FAILED)
    {
        munmap(m_addr, m_size);
    }
    m_addr = addr;
    m_size = size;
}

void*
TracepointSession::unique_mmap::get() const noexcept
{
    return m_addr;
}

size_t
TracepointSession::unique_mmap::get_size() const noexcept
{
    return m_size;
}
