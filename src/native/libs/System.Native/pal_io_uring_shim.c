// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_io_uring_shim.h"
#include "pal_errno.h"

#include <stdint.h>
#include <stddef.h>
#include <errno.h>
#include <stdlib.h>
#include <string.h>
#include <stdatomic.h>

#if HAVE_LINUX_IO_URING_H && HAVE_SYS_POLL_H
#include <linux/io_uring.h>
#include <sys/mman.h>
#include <sys/eventfd.h>
#include <sys/syscall.h>
#include <unistd.h>
#include <signal.h>
#endif

#include <pal_error_common.h>

// Mirror the syscall-number defines from pal_io_uring.c for setup and enter.
// Register is gated separately because __NR_io_uring_register may not exist.
#if HAVE_LINUX_IO_URING_H && HAVE_SYS_POLL_H && \
    (defined(__NR_io_uring_setup) || defined(SYS_io_uring_setup)) && \
    (defined(__NR_io_uring_enter) || defined(SYS_io_uring_enter)) && \
    (__SIZEOF_POINTER__ == 8)
#define SHIM_HAVE_IO_URING 1
#else
#define SHIM_HAVE_IO_URING 0
#endif

#if SHIM_HAVE_IO_URING

#define SHIM_EINTR_RETRY_LIMIT 1024
#define SHIM_TEST_FORCE_ENTER_EINTR_RETRY_LIMIT_ONCE_ENV "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_FORCE_ENTER_EINTR_RETRY_LIMIT_ONCE"

#if defined(IORING_SETUP_CLOEXEC)
_Static_assert(IORING_SETUP_CLOEXEC == (1U << 19), "Unexpected IORING_SETUP_CLOEXEC value");
#endif

#if defined(__NR_io_uring_setup)
#define IO_URING_SYSCALL_SETUP __NR_io_uring_setup
#else
#define IO_URING_SYSCALL_SETUP SYS_io_uring_setup
#endif

#if defined(__NR_io_uring_enter)
#define IO_URING_SYSCALL_ENTER __NR_io_uring_enter
#else
#define IO_URING_SYSCALL_ENTER SYS_io_uring_enter
#endif

#if defined(__NR_io_uring_register) || defined(SYS_io_uring_register)
#define SHIM_HAVE_IO_URING_REGISTER 1
#if defined(__NR_io_uring_register)
#define IO_URING_SYSCALL_REGISTER __NR_io_uring_register
#else
#define IO_URING_SYSCALL_REGISTER SYS_io_uring_register
#endif
#else
#define SHIM_HAVE_IO_URING_REGISTER 0
#endif

// The io_uring_getevents_arg struct for IORING_ENTER_EXT_ARG.
// Defined locally to avoid dependency on kernel header version.
typedef struct ShimIoUringGeteventsArg
{
    uint64_t sigmask;
    uint32_t sigmask_sz;
    uint32_t min_wait_usec;
    uint64_t ts;
} ShimIoUringGeteventsArg;

static int32_t ConsumeForceEnterEintrRetryLimitOnce(void)
{
    static atomic_int s_forceEnterEintrRetryLimitOnce = ATOMIC_VAR_INIT(-1);

    int32_t state = atomic_load_explicit(&s_forceEnterEintrRetryLimitOnce, memory_order_relaxed);
    if (state < 0)
    {
        const char* configuredValue = getenv(SHIM_TEST_FORCE_ENTER_EINTR_RETRY_LIMIT_ONCE_ENV);
        int32_t initializedState = configuredValue != NULL && strcmp(configuredValue, "1") == 0 ? 1 : 0;
        int expected = -1;
        if (!atomic_compare_exchange_strong_explicit(
                &s_forceEnterEintrRetryLimitOnce,
                &expected,
                initializedState,
                memory_order_relaxed,
                memory_order_relaxed))
        {
            initializedState = expected;
        }

        state = initializedState;
    }

    if (state == 0)
    {
        return 0;
    }

    int expected = 1;
    return atomic_compare_exchange_strong_explicit(
               &s_forceEnterEintrRetryLimitOnce,
               &expected,
               0,
               memory_order_relaxed,
               memory_order_relaxed)
        ? 1
        : 0;
}

int32_t SystemNative_IoUringShimSetup(uint32_t entries, void* params, int32_t* ringFd)
{
    if (params == NULL || ringFd == NULL)
    {
        return Error_EFAULT;
    }

    int fd = (int)syscall(IO_URING_SYSCALL_SETUP, entries, params);
    if (fd < 0)
    {
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    *ringFd = fd;
    return Error_SUCCESS;
}

int32_t SystemNative_IoUringShimEnter(int32_t ringFd, uint32_t toSubmit, uint32_t minComplete, uint32_t flags, int32_t* result)
{
    if (result == NULL)
    {
        return Error_EFAULT;
    }

    if (ringFd < 0)
    {
        return Error_EBADF;
    }

    if (toSubmit != 0 && ConsumeForceEnterEintrRetryLimitOnce() != 0)
    {
        return SystemNative_ConvertErrorPlatformToPal(EINTR);
    }

    int ret;
    int retryCount = 0;
    while ((ret = (int)syscall(IO_URING_SYSCALL_ENTER, ringFd, toSubmit, minComplete, flags, NULL, 0)) < 0 && errno == EINTR)
    {
        if (++retryCount >= SHIM_EINTR_RETRY_LIMIT)
        {
            return SystemNative_ConvertErrorPlatformToPal(EINTR);
        }
    }

    if (ret < 0)
    {
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    *result = ret;
    return Error_SUCCESS;
}

int32_t SystemNative_IoUringShimEnterExt(int32_t ringFd, uint32_t toSubmit, uint32_t minComplete, uint32_t flags, void* arg, int32_t* result)
{
    if (result == NULL)
    {
        return Error_EFAULT;
    }

    if (ringFd < 0)
    {
        return Error_EBADF;
    }

    if (toSubmit != 0 && ConsumeForceEnterEintrRetryLimitOnce() != 0)
    {
        return SystemNative_ConvertErrorPlatformToPal(EINTR);
    }

    int ret;
    int retryCount = 0;
    while ((ret = (int)syscall(IO_URING_SYSCALL_ENTER, ringFd, toSubmit, minComplete, flags, arg, arg == NULL ? 0 : sizeof(ShimIoUringGeteventsArg))) < 0 && errno == EINTR)
    {
        if (++retryCount >= SHIM_EINTR_RETRY_LIMIT)
        {
            return SystemNative_ConvertErrorPlatformToPal(EINTR);
        }
    }

    if (ret < 0)
    {
        // ETIME: bounded wait timeout expired. The kernel consumed all SQEs before
        // entering the wait phase, so report success with toSubmit as the accepted
        // count. This prevents the managed pending-submission counter from desyncing.
        if (errno == ETIME)
        {
            *result = (int32_t)toSubmit;
            return Error_SUCCESS;
        }
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    *result = ret;
    return Error_SUCCESS;
}

int32_t SystemNative_IoUringShimRegister(int32_t ringFd, uint32_t opcode, void* arg, uint32_t nrArgs, int32_t* result)
{
    if (result == NULL)
    {
        return Error_EFAULT;
    }

    if (ringFd < 0)
    {
        return Error_EBADF;
    }

#if SHIM_HAVE_IO_URING_REGISTER
    int ret;
    int retryCount = 0;
    while ((ret = (int)syscall(IO_URING_SYSCALL_REGISTER, ringFd, opcode, arg, nrArgs)) < 0 && errno == EINTR)
    {
        if (++retryCount >= SHIM_EINTR_RETRY_LIMIT)
        {
            return SystemNative_ConvertErrorPlatformToPal(EINTR);
        }
    }

    if (ret < 0)
    {
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    *result = ret;
    return Error_SUCCESS;
#else
    (void)ringFd;
    (void)opcode;
    (void)arg;
    (void)nrArgs;
    (void)result;
    return Error_ENOSYS;
#endif
}

int32_t SystemNative_IoUringShimMmap(int32_t ringFd, uint64_t size, uint64_t offset, void** mappedPtr)
{
    if (mappedPtr == NULL)
    {
        return Error_EFAULT;
    }

    if (ringFd < 0)
    {
        return Error_EBADF;
    }

    void* ptr = mmap(0, (size_t)size, PROT_READ | PROT_WRITE, MAP_SHARED | MAP_POPULATE, ringFd, (off_t)offset);
    if (ptr == MAP_FAILED)
    {
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    *mappedPtr = ptr;
    return Error_SUCCESS;
}

int32_t SystemNative_IoUringShimMunmap(void* addr, uint64_t size)
{
    if (addr == NULL)
    {
        return Error_EFAULT;
    }

    if (munmap(addr, (size_t)size) != 0)
    {
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    return Error_SUCCESS;
}

int32_t SystemNative_IoUringShimCreateEventFd(int32_t* eventFd)
{
    if (eventFd == NULL)
    {
        return Error_EFAULT;
    }

    int fd = eventfd(0, EFD_CLOEXEC | EFD_NONBLOCK);
    if (fd < 0)
    {
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    *eventFd = fd;
    return Error_SUCCESS;
}

int32_t SystemNative_IoUringShimWriteEventFd(int32_t eventFd)
{
    uint64_t val = 1;
    ssize_t written;
    int retryCount = 0;
    while ((written = write(eventFd, &val, sizeof(val))) < 0 && errno == EINTR)
    {
        if (++retryCount >= SHIM_EINTR_RETRY_LIMIT)
        {
            return SystemNative_ConvertErrorPlatformToPal(EINTR);
        }
    }

    if (written < 0)
    {
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    if (written != (ssize_t)sizeof(val))
    {
        return Error_EIO;
    }

    return Error_SUCCESS;
}

int32_t SystemNative_IoUringShimReadEventFd(int32_t eventFd, uint64_t* value)
{
    if (value == NULL)
    {
        return Error_EFAULT;
    }

    ssize_t bytesRead;
    int retryCount = 0;
    while ((bytesRead = read(eventFd, value, sizeof(*value))) < 0 && errno == EINTR)
    {
        if (++retryCount >= SHIM_EINTR_RETRY_LIMIT)
        {
            return SystemNative_ConvertErrorPlatformToPal(EINTR);
        }
    }

    if (bytesRead < 0)
    {
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    if ((size_t)bytesRead != sizeof(*value))
    {
        return Error_EIO;
    }

    return Error_SUCCESS;
}

int32_t SystemNative_IoUringShimCloseFd(int32_t fd)
{
    // Linux close(2) closes the descriptor even when interrupted (EINTR).
    // Retrying risks closing a reused descriptor opened by another thread.
    if (close(fd) != 0)
    {
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    return Error_SUCCESS;
}

// Layout assertions for managed interop structs (kernel struct mirrors).
c_static_assert(sizeof(size_t) >= 8);
c_static_assert(sizeof(size_t) == sizeof(void*));
c_static_assert(sizeof(struct io_uring_cqe) == 16);
c_static_assert(offsetof(struct io_uring_cqe, user_data) == 0);
c_static_assert(offsetof(struct io_uring_cqe, res) == 8);
c_static_assert(offsetof(struct io_uring_cqe, flags) == 12);

c_static_assert(sizeof(struct io_uring_params) == 120);
c_static_assert(offsetof(struct io_uring_params, sq_entries) == 0);
c_static_assert(offsetof(struct io_uring_params, cq_entries) == 4);
c_static_assert(offsetof(struct io_uring_params, flags) == 8);
c_static_assert(offsetof(struct io_uring_params, features) == 20);
c_static_assert(offsetof(struct io_uring_params, sq_off) == 40);
c_static_assert(offsetof(struct io_uring_params, cq_off) == 80);

c_static_assert(sizeof(struct io_sqring_offsets) == 40);
c_static_assert(offsetof(struct io_sqring_offsets, head) == 0);
c_static_assert(offsetof(struct io_sqring_offsets, tail) == 4);
c_static_assert(offsetof(struct io_sqring_offsets, ring_mask) == 8);
c_static_assert(offsetof(struct io_sqring_offsets, ring_entries) == 12);
c_static_assert(offsetof(struct io_sqring_offsets, flags) == 16);
c_static_assert(offsetof(struct io_sqring_offsets, dropped) == 20);
c_static_assert(offsetof(struct io_sqring_offsets, array) == 24);

c_static_assert(sizeof(struct io_cqring_offsets) == 40);
c_static_assert(offsetof(struct io_cqring_offsets, head) == 0);
c_static_assert(offsetof(struct io_cqring_offsets, tail) == 4);
c_static_assert(offsetof(struct io_cqring_offsets, overflow) == 16);
c_static_assert(offsetof(struct io_cqring_offsets, cqes) == 20);

#else // !SHIM_HAVE_IO_URING

// Stub implementations when io_uring is not available.

int32_t SystemNative_IoUringShimSetup(uint32_t entries, void* params, int32_t* ringFd)
{
    (void)entries;
    if (params == NULL || ringFd == NULL)
    {
        return Error_EFAULT;
    }

    *ringFd = -1;
    return Error_ENOSYS;
}

int32_t SystemNative_IoUringShimEnter(int32_t ringFd, uint32_t toSubmit, uint32_t minComplete, uint32_t flags, int32_t* result)
{
    (void)ringFd; (void)toSubmit; (void)minComplete; (void)flags;
    if (result == NULL)
    {
        return Error_EFAULT;
    }

    if (ringFd < 0)
    {
        return Error_EBADF;
    }

    *result = 0;
    return Error_ENOSYS;
}

int32_t SystemNative_IoUringShimEnterExt(int32_t ringFd, uint32_t toSubmit, uint32_t minComplete, uint32_t flags, void* arg, int32_t* result)
{
    (void)ringFd; (void)toSubmit; (void)minComplete; (void)flags; (void)arg;
    if (result == NULL)
    {
        return Error_EFAULT;
    }

    if (ringFd < 0)
    {
        return Error_EBADF;
    }

    *result = 0;
    return Error_ENOSYS;
}

int32_t SystemNative_IoUringShimRegister(int32_t ringFd, uint32_t opcode, void* arg, uint32_t nrArgs, int32_t* result)
{
    (void)ringFd; (void)opcode; (void)arg; (void)nrArgs;
    if (result == NULL)
    {
        return Error_EFAULT;
    }

    if (ringFd < 0)
    {
        return Error_EBADF;
    }

    *result = 0;
    return Error_ENOSYS;
}

int32_t SystemNative_IoUringShimMmap(int32_t ringFd, uint64_t size, uint64_t offset, void** mappedPtr)
{
    (void)ringFd; (void)size; (void)offset;
    if (mappedPtr == NULL)
    {
        return Error_EFAULT;
    }

    if (ringFd < 0)
    {
        return Error_EBADF;
    }

    *mappedPtr = NULL;
    return Error_ENOSYS;
}

int32_t SystemNative_IoUringShimMunmap(void* addr, uint64_t size)
{
    (void)size;
    if (addr == NULL)
    {
        return Error_EFAULT;
    }

    return Error_ENOSYS;
}

int32_t SystemNative_IoUringShimCreateEventFd(int32_t* eventFd)
{
    if (eventFd == NULL)
    {
        return Error_EFAULT;
    }

    *eventFd = -1;
    return Error_ENOSYS;
}

int32_t SystemNative_IoUringShimWriteEventFd(int32_t eventFd)
{
    (void)eventFd;
    return Error_ENOSYS;
}

int32_t SystemNative_IoUringShimReadEventFd(int32_t eventFd, uint64_t* value)
{
    (void)eventFd;
    if (value == NULL)
    {
        return Error_EFAULT;
    }

    *value = 0;
    return Error_ENOSYS;
}

int32_t SystemNative_IoUringShimCloseFd(int32_t fd)
{
    (void)fd;
    return Error_ENOSYS;
}

#endif // SHIM_HAVE_IO_URING
