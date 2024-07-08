// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*
An implementation of the tracepoint.h interface.
This implementation writes directly to user_events.
*/

#include <tracepoint/tracepoint.h>
#include <tracepoint/tracepoint-impl.h>

#include <assert.h>
#include <errno.h>
#include <stdint.h>
#include <stddef.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include <fcntl.h>
#include <pthread.h>
#include <unistd.h>
#include <linux/types.h>
#include <sys/ioctl.h>

#ifndef _tp_FUNC_ATTRIBUTES
#define _tp_FUNC_ATTRIBUTES
#endif // _tp_FUNC_ATTRIBUTES

#define STRCMP_PREFIX(prefix, str)  strncmp(prefix, str, sizeof(prefix) - 1)

//#include <linux/user_events.h>

/*
 * Describes an event registration and stores the results of the registration.
 * This structure is passed to the DIAG_IOCSREG ioctl, callers at a minimum
 * must set the size and name_args before invocation.
 */
struct user_reg {

    /* Input: Size of the user_reg structure being used */
    __u32 size;

    /* Input: Bit in enable address to use */
    __u8 enable_bit;

    /* Input: Enable size in bytes at address */
    __u8 enable_size;

    /* Input: Flags to use, if any */
    __u16 flags;

    /* Input: Address to update when enabled */
    __u64 enable_addr;

    /* Input: Pointer to string with event name, description and flags */
    __u64 name_args;

    /* Output: Index of the event to use when writing data */
    __u32 write_index;
} __attribute__((__packed__));

/*
 * Describes an event unregister, callers must set the size, address and bit.
 * This structure is passed to the DIAG_IOCSUNREG ioctl to disable bit updates.
 */
struct user_unreg {
    /* Input: Size of the user_unreg structure being used */
    __u32 size;

    /* Input: Bit to unregister */
    __u8 disable_bit;

    /* Input: Reserved, set to 0 */
    __u8 __reserved;

    /* Input: Reserved, set to 0 */
    __u16 __reserved2;

    /* Input: Address to unregister */
    __u64 disable_addr;
} __attribute__((__packed__));

#define DIAG_IOC_MAGIC '*'

/* Request to register a user_event */
#define DIAG_IOCSREG _IOWR(DIAG_IOC_MAGIC, 0, struct user_reg *)

/* Request to delete a user_event */
#define DIAG_IOCSDEL _IOW(DIAG_IOC_MAGIC, 1, char *)

/* Requests to unregister a user_event */
#define DIAG_IOCSUNREG _IOW(DIAG_IOC_MAGIC, 2, struct user_unreg*)

/*
Guards all stores to any tracepoint_provider_state or tracepoint_state.

Also guards loads from the tracepoint_provider_node Next/Prev fields.

All other fields may be read outside the lock via atomic_load, so they must be
updated within the lock via atomic_store.
*/
static pthread_mutex_t s_providers_mutex = PTHREAD_MUTEX_INITIALIZER;

static int
get_failure_errno(void)
{
    int err = errno;
    assert(err > 0);
    if (err <= 0)
    {
        err = ENOENT;
    }

    return err;
}

static int
is_space_char(char ch)
{
    return ch == ' ' || ch == '\t';
}

static int
is_nonspace_char(char ch)
{
    return ch != '\0' && !is_space_char(ch);
}

static int
user_events_data_update(int* staticFileOrError)
{
    int newFileOrError;
    FILE* mountsFile;

    // Find the mount path for tracefs:

    /*
    Goals:
    - Minimal external dependencies.
    - Efficient (minimal impact to startup time).
    - Works correctly if the system is properly configured.
    - System administrator can control how this ends up behaving (i.e. by
      setting permissions on the user_events_data file).
    - Can be made to work in a container.
    */

    // Start by probing the most likely absolute path.
    // This improves performance and also makes it simpler to set up a container:
    // The container creator will have to project the user_events_data file but
    // won't need to make a dummy /proc/mounts file for us to parse.
    newFileOrError = open("/sys/kernel/tracing/user_events_data", O_WRONLY);
    if (0 <= newFileOrError)
    {
        // Success.
    }
    else if (NULL == (mountsFile = fopen("/proc/mounts", "r")))
    {
        newFileOrError = -get_failure_errno();
    }
    else
    {
        char path[274]; // 256 + sizeof("/user_events_data")
        path[0] = 0;

        for (;;)
        {
            char line[4097];
            if (!fgets(line, sizeof(line), mountsFile))
            {
                break;
            }

            // line is "device_name mount_point file_system other_stuff..."

            size_t line_pos = 0;

            // device_name
            while (is_nonspace_char(line[line_pos]))
            {
                line_pos += 1;
            }

            // whitespace
            while (is_space_char(line[line_pos]))
            {
                line_pos += 1;
            }

            // mount_point
            size_t const mount_begin = line_pos;
            while (is_nonspace_char(line[line_pos]))
            {
                line_pos += 1;
            }

            size_t const mount_end = line_pos;

            // whitespace
            while (is_space_char(line[line_pos]))
            {
                line_pos += 1;
            }

            // file_system
            size_t const fs_begin = line_pos;
            while (is_nonspace_char(line[line_pos]))
            {
                line_pos += 1;
            }

            size_t const fs_end = line_pos;

            if (!is_space_char(line[line_pos]))
            {
                // Ignore line if no whitespace after file_system.
                continue;
            }

            char const* path_suffix;
            size_t path_suffix_len; // includes NUL
            char keepLooking;

            const char* const pchTracefs = "tracefs";
            size_t const cchTracefs = sizeof("tracefs") - 1;
            const char* const pchDebugfs = "debugfs";
            size_t const cchDebugfs = sizeof("debugfs") - 1;

            size_t const fs_len = fs_end - fs_begin;
            if (fs_len == cchTracefs && 0 == memcmp(line + fs_begin, pchTracefs, cchTracefs))
            {
                // "tracefsMountPoint/user_events_data"
                path_suffix = "/user_events_data";
                path_suffix_len = sizeof("/user_events_data"); // includes NUL
                keepLooking = 0; // prefer "tracefs" over "debugfs".
            }
            else if (path[0] == 0 &&
                fs_len == cchDebugfs && 0 == memcmp(line + fs_begin, pchDebugfs, cchDebugfs))
            {
                // "debugfsMountPoint/tracing/user_events_data"
                path_suffix = "/tracing/user_events_data";
                path_suffix_len = sizeof("/tracing/user_events_data"); // includes NUL
                keepLooking = 1; // prefer "tracefs" over "debugfs".
            }
            else
            {
                continue;
            }

            size_t const mount_len = mount_end - mount_begin;
            size_t const path_len = mount_len + path_suffix_len; // includes NUL
            if (path_len > sizeof(path))
            {
                continue;
            }

            // path = mountpoint + suffix
            memcpy(path, line + mount_begin, mount_len);
            memcpy(path + mount_len, path_suffix, path_suffix_len); // includes NUL

            if (!keepLooking)
            {
                break;
            }
        }

        fclose(mountsFile);

        if (path[0] == 0)
        {
            // No "tracefs" or "debugfs" mount point found.
            newFileOrError = -ENOTSUP;
        }
        else
        {
            // path is now something like "/sys/kernel/tracing/user_events_data\0" or
            // "/sys/kernel/debug/tracing/user_events_data\0".
            newFileOrError = open(path, O_WRONLY);
            if (0 > newFileOrError)
            {
                newFileOrError = -get_failure_errno();
            }
        }
    }

    int oldFileOrError = -EAGAIN;
    for (;;)
    {
        if (__atomic_compare_exchange_n(
            staticFileOrError,
            &oldFileOrError,
            newFileOrError,
            0,
            __ATOMIC_RELAXED,
            __ATOMIC_RELAXED))
        {
            // The cmpxchg set *staticFileOrError = newFileOrError.
            return newFileOrError;
        }

        // The cmpxchg set oldFileOrError = *staticFileOrError.

        if (oldFileOrError >= 0 || newFileOrError < 0)
        {
            // Prefer the existing contents of staticFileOrError.
            if (newFileOrError >= 0)
            {
                close(newFileOrError);
            }

            return oldFileOrError;
        }
    }
}

// On success, returns a non-negative file descriptor.
// On failure, returns -errno.
static int
user_events_data_get()
{
    static int staticFileOrError = -EAGAIN; // Intentionally leaked.
    int fileOrError = __atomic_load_n(&staticFileOrError, __ATOMIC_RELAXED);
    return fileOrError != -EAGAIN
        ? fileOrError
        : user_events_data_update(&staticFileOrError);
}

static void
event_unregister(tracepoint_state* tp_state)
{
    if (tp_state->write_index >= 0)
    {
        struct user_unreg unreg = { 0 };
        unreg.size = sizeof(struct user_unreg);
        unreg.disable_bit = 0;
        unreg.disable_addr = (uintptr_t)&tp_state->status_word;
        ioctl(tp_state->provider_state->data_file, DIAG_IOCSUNREG, &unreg);
        ((tracepoint_provider_state*)tp_state->provider_state)->ref_count -= 1; // For debugging purposes.
    }
}

#ifdef __cplusplus
extern "C" {
#endif // __cplusplus

    void
    tracepoint_close_provider(
        tracepoint_provider_state* provider_state) _tp_FUNC_ATTRIBUTES;
    void
    tracepoint_close_provider(
        tracepoint_provider_state* provider_state)
    {
        pthread_mutex_lock(&s_providers_mutex);

        if (provider_state->data_file != -1)
        {
            assert(provider_state->data_file > -1);

            // Need to unregister events when we're done.
            tracepoint_list_node* node = provider_state->tracepoint_list_head.next;
            if (node != NULL)
            {
                assert(node->prev == &provider_state->tracepoint_list_head);
                while (node != &provider_state->tracepoint_list_head)
                {
                    tracepoint_state* tp_state = (tracepoint_state*)((char*)node - offsetof(tracepoint_state, tracepoint_list_link));
                    node = node->next;
                    assert(node->prev == &tp_state->tracepoint_list_link);

                    assert(provider_state == tp_state->provider_state);
                    event_unregister(tp_state);
                }
            }
        }

        assert(provider_state->ref_count == 0); // register count == unregister count?
        tracepoint_close_provider_impl(provider_state);

        pthread_mutex_unlock(&s_providers_mutex);
    }

    int
    tracepoint_open_provider(
        tracepoint_provider_state* provider_state) _tp_FUNC_ATTRIBUTES;
    int
    tracepoint_open_provider(
        tracepoint_provider_state* provider_state)
    {
        int const fileOrError = user_events_data_get();
        int const err = fileOrError >= 0 ? 0 : -fileOrError;

        pthread_mutex_lock(&s_providers_mutex);

        if (provider_state->data_file != -1)
        {
            assert(provider_state->data_file == -1); // PRECONDITION
            abort(); // PRECONDITION
        }

        if (err == 0)
        {
            /*
            This will clear any events that were already "connected".
            - If the higher layer connects events after open, ensure a blank slate.
            - If the higher layer connects events on-demand, this will reset them so they
              can reconnect now that we're ready for them.
            */
            tracepoint_open_provider_impl(provider_state, fileOrError);
        }

        assert(provider_state->ref_count == 0);
        pthread_mutex_unlock(&s_providers_mutex);

        return err;
    }

    int
    tracepoint_open_provider_with_tracepoints(
        tracepoint_provider_state* provider_state,
        tracepoint_definition const** tp_definition_start,
        tracepoint_definition const** tp_definition_stop) _tp_FUNC_ATTRIBUTES;
    int
    tracepoint_open_provider_with_tracepoints(
        tracepoint_provider_state* provider_state,
        tracepoint_definition const** tp_definition_start,
        tracepoint_definition const** tp_definition_stop)
    {
        return tracepoint_open_provider_with_tracepoints_impl(
            provider_state,
            tp_definition_start,
            tp_definition_stop);
    }

    int
    tracepoint_connect2(
        tracepoint_state* tp_state,
        tracepoint_provider_state* provider_state,
        char const* tp_name_args,
        unsigned flags) _tp_FUNC_ATTRIBUTES;
    int
    tracepoint_connect2(
        tracepoint_state* tp_state,
        tracepoint_provider_state* provider_state,
        char const* tp_name_args,
        unsigned flags)
    {
        int err;
        int write_index = -1;

        pthread_mutex_lock(&s_providers_mutex);

        event_unregister(tp_state);

        if (NULL == provider_state ||
            -1 == provider_state->data_file)
        {
            err = 0;
        }
        else
        {
            struct user_reg reg = { 0 };
            reg.size = sizeof(reg);
            reg.enable_bit = 0;
            reg.enable_size = sizeof(tp_state->status_word);
            reg.flags = (__u16)flags;
            reg.enable_addr = (uintptr_t)&tp_state->status_word;
            reg.name_args = (uintptr_t)tp_name_args;

            if (0 > ioctl(provider_state->data_file, DIAG_IOCSREG, &reg))
            {
                err = errno;
            }
            else
            {
                assert(reg.write_index <= 0x7fffffff);
                provider_state->ref_count += 1;
                write_index = (int)reg.write_index;
                err = 0;
            }
        }

        tracepoint_connect_impl(tp_state, provider_state, write_index);

        pthread_mutex_unlock(&s_providers_mutex);
        return err;
    }

    int
    tracepoint_connect(
        tracepoint_state* tp_state,
        tracepoint_provider_state* provider_state,
        char const* tp_name_args) _tp_FUNC_ATTRIBUTES;
    int
    tracepoint_connect(
        tracepoint_state* tp_state,
        tracepoint_provider_state* provider_state,
        char const* tp_name_args)
    {
        return tracepoint_connect2(tp_state, provider_state, tp_name_args, 0);
    }

    int
    tracepoint_write(
        tracepoint_state const* tp_state,
        unsigned data_count,
        struct iovec* data_vecs) _tp_FUNC_ATTRIBUTES;
    int
    tracepoint_write(
        tracepoint_state const* tp_state,
        unsigned data_count,
        struct iovec* data_vecs)
    {
        assert((int)data_count >= 1);
        assert(data_vecs[0].iov_len == 0);

        if (!TRACEPOINT_ENABLED(tp_state))
        {
            return EBADF;
        }

        tracepoint_provider_state const* provider_state = __atomic_load_n(&tp_state->provider_state, __ATOMIC_RELAXED);
        if (provider_state == NULL)
        {
            return EBADF;
        }

        // Workaround: Events don't show up correctly with 0 bytes of data.
        // If event has 0 bytes of data, add a '\0' byte to avoid the problem.
        struct {
            int32_t write_index;
            char workaround;
        } data0 = {
            __atomic_load_n(&tp_state->write_index, __ATOMIC_RELAXED),
            0
        };
        data_vecs[0].iov_base = &data0;
        data_vecs[0].iov_len = sizeof(int32_t) + (data_count == 1);

        int data_file = __atomic_load_n(&provider_state->data_file, __ATOMIC_RELAXED);
        int err = 0 > data_file || 0 <= writev(data_file, data_vecs, (int)data_count)
            ? 0
            : errno;
        return err;
    }

#ifdef __cplusplus
} // extern "C"
#endif // __cplusplus
