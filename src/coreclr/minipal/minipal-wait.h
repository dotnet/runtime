// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __MINIPAL_WAIT_H__
#define __MINIPAL_WAIT_H__

#include <stdint.h>

#ifdef HOST_WINDOWS
#include <windows.h>
#endif

constexpr uint32_t MINIPAL_MAX_WAIT_OBJECTS = 64;
constexpr uint32_t MINIPAL_WAIT_INFINITE = UINT32_MAX;
constexpr int32_t MINIPAL_WAIT_TIMEOUT = -1;
constexpr int32_t MINIPAL_WAIT_FAILED = -2;

struct minipal_wait_handle
{
    bool IsValid() const
    {
        return m_handle != nullptr;
    }

#ifdef HOST_WINDOWS
    HANDLE GetRawHandle() const
    {
        return m_handle;
    }
#endif

    // Waits return the zero-based index of the acquired handle, or one of the negative results above.
    // Handles must remain valid until the wait returns.
    static int32_t Wait(const minipal_wait_handle& handle, uint32_t timeout)
    {
        const minipal_wait_handle* handles[] = { &handle };
        return Wait(handles, 1, timeout);
    }

    static int32_t Wait(
        const minipal_wait_handle* const* handles,
        uint32_t count,
        uint32_t timeout);

    minipal_wait_handle(const minipal_wait_handle& handle);
    minipal_wait_handle& operator=(const minipal_wait_handle& handle) = delete;
    virtual ~minipal_wait_handle();

protected:
#ifdef HOST_WINDOWS
    explicit minipal_wait_handle(HANDLE handle);
#else
    explicit minipal_wait_handle(void* handle);
#endif

#ifndef HOST_WINDOWS
    void* GetWaitable() const
    {
        return m_handle;
    }
#endif

private:
#ifdef HOST_WINDOWS
    HANDLE m_handle;
#else
    void* m_handle;
#endif
};

struct minipal_event final : minipal_wait_handle
{
    explicit minipal_event(bool initialState);

#ifdef HOST_WINDOWS
    // Duplicates an existing native event handle.
    explicit minipal_event(HANDLE handle);
#endif
    minipal_event(const minipal_event& event) = default;
    minipal_event& operator=(const minipal_event& event) = delete;

    bool Set();
    bool Reset();
};

struct minipal_latch final : minipal_wait_handle
{
    minipal_latch();
    minipal_latch(const minipal_latch& latch) = default;
    minipal_latch& operator=(const minipal_latch& latch) = delete;

    bool Set();
};

#ifdef HOST_WINDOWS
struct minipal_native_handle final : minipal_wait_handle
{
    // Duplicates an existing native handle.
    explicit minipal_native_handle(HANDLE handle);
    minipal_native_handle(const minipal_native_handle& handle) = default;
    minipal_native_handle& operator=(const minipal_native_handle& handle) = delete;
};
#endif // HOST_WINDOWS

#endif // __MINIPAL_WAIT_H__
