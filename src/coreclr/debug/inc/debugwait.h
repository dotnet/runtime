// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _DEBUGWAIT_H_
#define _DEBUGWAIT_H_

#include <stdint.h>

#ifdef HOST_WINDOWS
#include <windows.h>
#endif

class WaitHandle
{
public:
    static constexpr uint32_t Infinite = UINT32_MAX;
    static constexpr int32_t Timeout = -1;
    static constexpr int32_t Failed = -2;

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
    static int32_t Wait(const WaitHandle& handle, uint32_t timeout)
    {
        const WaitHandle* handles[] = { &handle };
        return Wait(handles, 1, timeout);
    }

    static int32_t Wait(
        const WaitHandle* const* handles,
        uint32_t count,
        uint32_t timeout);

    WaitHandle(const WaitHandle& handle);
    WaitHandle& operator=(const WaitHandle& handle) = delete;
    virtual ~WaitHandle();

protected:
#ifdef HOST_WINDOWS
    explicit WaitHandle(HANDLE handle);
#else
    explicit WaitHandle(void* handle);
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

class WaitEvent final : public WaitHandle
{
public:
    explicit WaitEvent(bool initialState);

#ifdef HOST_WINDOWS
    // Duplicates an existing native event handle.
    explicit WaitEvent(HANDLE handle);
#else
    // A non-Windows waitable is a debug PAL primitive, not a PAL HANDLE, so an existing
    // handle can't be imported. Reject pointers rather than letting them silently select
    // the initial state constructor and create an unrelated event.
    explicit WaitEvent(void* handle) = delete;
#endif
    WaitEvent(const WaitEvent& event) = default;
    WaitEvent& operator=(const WaitEvent& event) = delete;

    bool Set();
    bool Reset();
};

class WaitLatch final : public WaitHandle
{
public:
    WaitLatch();
    WaitLatch(const WaitLatch& latch) = default;
    WaitLatch& operator=(const WaitLatch& latch) = delete;

    bool Set();
};

#ifdef HOST_WINDOWS
class NativeHandle final : public WaitHandle
{
public:
    // Duplicates an existing native handle.
    explicit NativeHandle(HANDLE handle);
    NativeHandle(const NativeHandle& handle) = default;
    NativeHandle& operator=(const NativeHandle& handle) = delete;
};
#endif // HOST_WINDOWS

#endif // _DEBUGWAIT_H_
