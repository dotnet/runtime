// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is minimal implementation of posix functions files required to cross compile
// libunwind on a Windows host for UNW_REMOTE_ONLY application.

// This a completely thread unsafe implementation
// It is likely sufficient for a single thread's usage of UNW_REMOTE_ONLY debugging of
// a read-only dump.

#include <pthread.h>
#include <signal.h>
#include <stdlib.h>
#include <sys/mman.h>
#include <sys/stat.h>
#include <unistd.h>
#include "libunwind_i.h"
#include "compiler.h"

int getpagesize(void)
{
    // 4096 is truth for most targets
    // Unlikely to matter in dump debugging
    return 4096;
}

void* mmap(void *addr, size_t length, int prot, int flags, int fd, size_t offset)
{
    // We shouldn't be doing anything other than anonymous mappings
    if ((flags & MAP_ANONYMOUS) == 0)
        return MAP_FAILED;

    return calloc(1, length);
}

int munmap(void *addr, size_t length)
{
    free(addr);
    return 0;
}

int pthread_key_create(pthread_key_t *key, void (*destroy)(void*))
{
    // We are not implementing pthread_getspecific so this sholdn't matter much
    return 0;
}

int pthread_setspecific(pthread_key_t key, const void *value)
{
    // We are not implementing pthread_getspecific so this sholdn't matter much
    return 0;
}

int pthread_mutex_init(pthread_mutex_t *mutex, const pthread_mutexattr_t *attr)
{
    // For dump debugging we don't need locking
    // We expect to run in a single thread
    return 0;
}

int pthread_mutex_lock(pthread_mutex_t *mutex)
{
    // For dump debugging we don't need locking
    // We expect to run in a single thread
    return 0;
}

int pthread_mutex_unlock(pthread_mutex_t *mutex)
{
    // For dump debugging we don't need locking
    // We expect to run in a single thread
    return 0;
}

int pthread_once(pthread_once_t *control, void (*init)(void))
{
    if (control == 0)
        return -1;

    // We expect to run in a single thread
    // We don't need atomics here
    if (*control != PTHREAD_ONCE_INIT)
    {
        (*init)();
        *control = ~PTHREAD_ONCE_INIT;
    }
    return 0;
}

int sigfillset(sigset_t *set)
{
    return 0;
}

ssize_t read(int fd, void *buf, size_t count)
{
    // For dump debugging we shouldn't need to open files
    // Especially since we didn't implement open()
    return -1;
}

int close(int fd)
{
    // For dump debugging we shouldn't need to open files
    // Especially since we didn't implement open()
    return -1;
}

// ALIAS(x) is nop.  We need this alias to link properly
unw_accessors_t * unw_get_accessors_int (unw_addr_space_t as)
{
    return unw_get_accessors(as);
}

int stat(const char *path, struct stat *buf)
{
    return 0;
}

int fstat(int fd, struct stat *buf)
{
    return 0;
}
