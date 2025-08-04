// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef TARGET_WINDOWS
#include <assert.h>
#include <pthread.h>
#include <stdbool.h>
#include <stdint.h>
#include <stdio.h>
#include <sys/mman.h>
#include <unistd.h>
#include <minipal/flushprocesswritebuffers.h>

#ifdef __APPLE__
#include <mach/thread_state.h>

#define CHECK_MACH(_msg, machret) do {                                      \
        if (machret != KERN_SUCCESS)                                        \
        {                                                                   \
            char _szError[1024];                                            \
            snprintf(_szError, ARRAY_SIZE(_szError), "%s: %u: %s", __FUNCTION__, __LINE__, _msg);  \
            mach_error(_szError, machret);                                  \
            abort();                                                        \
        }                                                                   \
    } while (false)

#endif // __APPLE__

#ifdef __linux__
#include <linux/membarrier.h>
#include <sys/syscall.h>
#define membarrier(...) syscall(__NR_membarrier, __VA_ARGS__)
#elif HAVE_SYS_MEMBARRIER_H
#include <sys/membarrier.h>
#ifdef TARGET_BROWSER
#define membarrier(cmd, flags, cpu_id) 0 // browser/wasm is currently single threaded
#endif
#endif

static bool CanFlushUsingMembarrier(void)
{
#if defined(__linux__) || HAVE_SYS_MEMBARRIER_H

#ifdef TARGET_ANDROID
    // Avoid calling membarrier on older Android versions where membarrier
    // may be barred by seccomp causing the process to be killed.
    int apiLevel = android_get_device_api_level();
    if (apiLevel < __ANDROID_API_Q__)
    {
        return false;
    }
#endif

    // Starting with Linux kernel 4.14, process memory barriers can be generated
    // using MEMBARRIER_CMD_PRIVATE_EXPEDITED.

    int mask = membarrier(MEMBARRIER_CMD_QUERY, 0, 0);

    if (mask >= 0 &&
        mask & MEMBARRIER_CMD_PRIVATE_EXPEDITED &&
        // Register intent to use the private expedited command.
        membarrier(MEMBARRIER_CMD_REGISTER_PRIVATE_EXPEDITED, 0, 0) == 0)
    {
        return true;
    }
#endif

    return false;
}

//
// Tracks if the OS supports FlushProcessWriteBuffers using membarrier
//
static int s_flushUsingMemBarrier = 0;

// Helper memory page used by the FlushProcessWriteBuffers
static uint8_t* g_helperPage = 0;

// Mutex to make the FlushProcessWriteBuffersMutex thread safe
static pthread_mutex_t g_flushProcessWriteBuffersMutex;

static size_t s_pageSize = 0;

bool minipal_initialize_flush_process_write_buffers(void)
{
    int pageSize = sysconf( _SC_PAGE_SIZE );

    s_pageSize = (size_t)((pageSize > 0) ? pageSize : 0x1000);

#ifndef TARGET_WASM
    //
    // support for FlusProcessWriteBuffers
    //
    assert(s_flushUsingMemBarrier == 0);

    if (CanFlushUsingMembarrier())
    {
        s_flushUsingMemBarrier = true;
    }
#ifndef TARGET_APPLE
    else
    {
        assert(g_helperPage == 0);

        g_helperPage = (uint8_t*)(mmap(0, s_pageSize, PROT_READ | PROT_WRITE, MAP_ANONYMOUS | MAP_PRIVATE, -1, 0));

        if (g_helperPage == MAP_FAILED)
        {
            return false;
        }

        // Verify that the s_helperPage is really aligned to the g_SystemInfo.dwPageSize
        assert((((size_t)g_helperPage) & (s_pageSize - 1)) == 0);

        // Locking the page ensures that it stays in memory during the two mprotect
        // calls in the FlushProcessWriteBuffers below. If the page was unmapped between
        // those calls, they would not have the expected effect of generating IPI.
        int status = mlock(g_helperPage, s_pageSize);

        if (status != 0)
        {
            return false;
        }

        status = pthread_mutex_init(&g_flushProcessWriteBuffersMutex, NULL);
        if (status != 0)
        {
            munlock(g_helperPage, s_pageSize);
            return false;
        }
    }
#endif // !TARGET_APPLE
#endif // !TARGET_WASM
    return true;
}

// Flush write buffers of processors that are executing threads of the current process
void minipal_flush_process_write_buffers(void)
{
#ifndef TARGET_WASM
#if defined(__linux__) || HAVE_SYS_MEMBARRIER_H
    if (s_flushUsingMemBarrier)
    {
        int status = membarrier(MEMBARRIER_CMD_PRIVATE_EXPEDITED, 0, 0);
        assert(status == 0 && "Failed to flush using membarrier");
    }
    else
#endif
    if (g_helperPage != 0)
    {
        int status = pthread_mutex_lock(&g_flushProcessWriteBuffersMutex);
        assert(status == 0 && "Failed to lock the flushProcessWriteBuffersMutex lock");

        // causes the OS to issue IPI to flush TLBs on all processors. This also
        // results in flushing the processor buffers.
        // Changing a helper memory page protection from read / write to no access
        status = mprotect(g_helperPage, s_pageSize, PROT_READ | PROT_WRITE);
        assert(status == 0 && "Failed to change helper page protection to read / write");

        // Ensure that the page is dirty before we change the protection so that
        // we prevent the OS from skipping the global TLB flush.
        __sync_add_and_fetch((size_t*)g_helperPage, 1);

        status = mprotect(g_helperPage, s_pageSize, PROT_NONE);
        assert(status == 0 && "Failed to change helper page protection to no access");

        status = pthread_mutex_unlock(&g_flushProcessWriteBuffersMutex);
        assert(status == 0 && "Failed to unlock the flushProcessWriteBuffersMutex lock");
    }
#ifdef TARGET_APPLE
    else
    {
        mach_msg_type_number_t cThreads;
        thread_act_t *pThreads;
        kern_return_t machret = task_threads(mach_task_self(), &pThreads, &cThreads);
        CHECK_MACH("task_threads()", machret);

        uintptr_t sp;
        uintptr_t registerValues[128];

        // Iterate through each of the threads in the list.
        for (mach_msg_type_number_t i = 0; i < cThreads; i++)
        {
            if (__builtin_available (macOS 10.14, iOS 12, tvOS 9, *))
            {
                // Request the threads pointer values to force the thread to emit a memory barrier
                size_t registers = 128;
                machret = thread_get_register_pointer_values(pThreads[i], &sp, &registers, registerValues);
            }
            else
            {
                // fallback implementation for older OS versions
#if defined(HOST_AMD64)
                x86_thread_state64_t threadState;
                mach_msg_type_number_t count = x86_THREAD_STATE64_COUNT;
                machret = thread_get_state(pThreads[i], x86_THREAD_STATE64, (thread_state_t)&threadState, &count);
#elif defined(HOST_ARM64)
                arm_thread_state64_t threadState;
                mach_msg_type_number_t count = ARM_THREAD_STATE64_COUNT;
                machret = thread_get_state(pThreads[i], ARM_THREAD_STATE64, (thread_state_t)&threadState, &count);
#else
                #error Unexpected architecture
#endif
            }

            if (machret == KERN_INSUFFICIENT_BUFFER_SIZE)
            {
                CHECK_MACH("thread_get_register_pointer_values()", machret);
            }

            machret = mach_port_deallocate(mach_task_self(), pThreads[i]);
            CHECK_MACH("mach_port_deallocate()", machret);
        }
        // Deallocate the thread list now we're done with it.
        machret = vm_deallocate(mach_task_self(), (vm_address_t)pThreads, cThreads * sizeof(thread_act_t));
        CHECK_MACH("vm_deallocate()", machret);
    }
#endif // TARGET_APPLE
#endif // !TARGET_WASM
}
#else // !TARGET_WINDOWS
void minipal_flush_process_write_buffers(void)
{
    ::FlushProcessWriteBuffers();
}
#endif // !TARGET_WINDOWS
