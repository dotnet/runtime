// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "log.h"
#include <string.h>

#ifndef MINIPAL_LOG_RUNTIME_TAG
#define MINIPAL_LOG_RUNTIME_TAG "DOTNET"
#endif

#ifdef HOST_ANDROID
#include <android/log.h>
#include <malloc.h>

// Android defines its LOGGER_ENTRY_MAX_PAYLOAD to 4068 bytes.
// Use 4000 bytes to include some slack for future changes to LOGGER_ENTRY_MAX_PAYLOAD.
#define MINIPAL_LOG_MAX_PAYLOAD 4000

// Android defines its internal log buffer used in __android_log_vprint to 1024 bytes.
// Use same internal stack buffer size avoiding dynamic memory allocation for majority of logging.
#define MINIPAL_LOG_BUF_SIZE 1024

static int android_log_flag(minipal_log_flags flags)
{
    switch(flags)
    {
    case minipal_log_flags_fatal:
        return ANDROID_LOG_FATAL;
    case minipal_log_flags_error:
        return ANDROID_LOG_ERROR;
    case minipal_log_flags_warning:
        return ANDROID_LOG_WARN;
    case minipal_log_flags_info:
        return ANDROID_LOG_INFO;
    case minipal_log_flags_debug:
        return ANDROID_LOG_DEBUG;
    case minipal_log_flags_verbose:
        return ANDROID_LOG_VERBOSE;
    default:
        return ANDROID_LOG_UNKNOWN;
    }
}

static size_t log_write(minipal_log_flags flags, const char* msg, size_t msg_len)
{
    return __android_log_write(android_log_flag(flags), MINIPAL_LOG_RUNTIME_TAG, msg) == 1 ? msg_len : 0;
}

int minipal_log_print(minipal_log_flags flags, const char* fmt, ... )
{
    va_list args;
    va_start(args, fmt);
    int bytes_written = minipal_log_vprint(flags, fmt, args);
    va_end(args);
    return bytes_written;
}

int minipal_log_vprint(minipal_log_flags flags, const char* fmt, va_list args)
{
    char stack_buffer[MINIPAL_LOG_BUF_SIZE];
    int bytes_written = 0;
    va_list args_copy;

    va_copy(args_copy, args);

    int len = vsnprintf(stack_buffer, sizeof(stack_buffer), fmt, args_copy);
    if (len < sizeof(stack_buffer))
    {
        bytes_written = minipal_log_write(flags, stack_buffer);
    }
    else
    {
        char* dyn_buffer = (char*)malloc(len + 1);
        if (dyn_buffer != NULL)
        {
            vsnprintf(dyn_buffer, len + 1, fmt, args);
            bytes_written = minipal_log_write(flags, dyn_buffer);
            free(dyn_buffer);
        }
    }

    va_end(args_copy);
    return bytes_written;
}

void minipal_log_flush(minipal_log_flags flags)
{
}

static size_t log_write_line(minipal_log_flags flags, const char* msg, size_t msg_len)
{
    char buffer[MINIPAL_LOG_MAX_PAYLOAD];
    if (msg_len < MINIPAL_LOG_MAX_PAYLOAD)
    {
        strncpy(buffer, msg, msg_len);
        buffer[msg_len] = '\0';
        return log_write(flags, buffer, msg_len);
    }

    const char* msg_end = msg + msg_len;
    size_t bytes_written = 0;
    while (msg < msg_end)
    {
        ptrdiff_t chunk_size = MINIPAL_LOG_MAX_PAYLOAD - 1;
        if (msg_end - msg < chunk_size)
        {
            chunk_size = msg_end - msg;
        }

        strncpy(buffer, msg, chunk_size);
        buffer[chunk_size] = '\0';
        bytes_written += log_write(flags, buffer, chunk_size);
        msg += chunk_size;
    }

    return bytes_written;
}

int minipal_log_write(minipal_log_flags flags, const char* msg)
{
    if (msg == NULL || msg[0] == '\0')
        return 0;

    size_t msg_len = strlen(msg);
    const char* msg_end = msg + msg_len;

    if (msg_len < MINIPAL_LOG_MAX_PAYLOAD)
        return (int)log_write(flags, msg, msg_len);

    const char* next_msg = NULL;
    size_t bytes_written = 0;
    for (next_msg = msg; next_msg < msg_end;)
    {
        const char* next_line_break = strchr(next_msg, '\n');
        if (next_line_break == NULL && (msg_end - next_msg < MINIPAL_LOG_MAX_PAYLOAD))
        {
            bytes_written += log_write(flags, next_msg, msg_end - next_msg);
            break;
        }
        else if (next_line_break == NULL)
        {
            bytes_written += log_write_line(flags, next_msg, msg_end - next_msg);
            break;
        }
        else
        {
            bytes_written += log_write_line(flags, next_msg, next_line_break - next_msg);
            next_msg = next_line_break + 1;
        }
    }

    return (int)bytes_written;
}

void minipal_log_sync(minipal_log_flags flags)
{
}
#else
#include <stdio.h>

#define MINIPAL_LOG_MAX_PAYLOAD 32767

static FILE * get_std_file(minipal_log_flags flags)
{
    switch(flags)
    {
    case minipal_log_flags_fatal:
    case minipal_log_flags_error:
        return stderr;
    case minipal_log_flags_warning:
    case minipal_log_flags_info:
    case minipal_log_flags_debug:
    case minipal_log_flags_verbose:
    default:
        return stdout;
    }
}

int minipal_log_print(minipal_log_flags flags, const char* fmt, ... )
{
    va_list args;
    va_start(args, fmt);
    int status = vfprintf(get_std_file(flags), fmt, args);
    va_end(args);
    return status;
}

int minipal_log_vprint(minipal_log_flags flags, const char* fmt, va_list args)
{
    return vfprintf(get_std_file(flags), fmt, args);
}

void minipal_log_flush(minipal_log_flags flags)
{
    FILE* file = get_std_file(flags);
    if (file != NULL)
        fflush(file);
}

#if defined(HOST_WINDOWS)
#include <Windows.h>

static HANDLE get_std_handle(minipal_log_flags flags)
{
    switch(flags)
    {
    case minipal_log_flags_fatal:
    case minipal_log_flags_error:
        return GetStdHandle(STD_ERROR_HANDLE);
    case minipal_log_flags_warning:
    case minipal_log_flags_info:
    case minipal_log_flags_debug:
    case minipal_log_flags_verbose:
    default:
        return GetStdHandle(STD_OUTPUT_HANDLE);
    }
}

static size_t write_file(HANDLE handle, const char* msg, size_t bytes_to_write)
{
    if (handle == INVALID_HANDLE_VALUE)
        return 0;

    if (msg == NULL || msg[0] == '\0')
        return 0;

    DWORD bytes_written = 0;
    if (!WriteFile(handle, msg, (DWORD)bytes_to_write, &bytes_written, NULL))
        bytes_written = 0;

    return bytes_written;
}

static void sync_file(HANDLE handle)
{
    if (handle == INVALID_HANDLE_VALUE)
        return;

    FlushFileBuffers(handle);
}
#endif

#if !defined(HOST_WINDOWS)
#include <errno.h>
#include <unistd.h>
#include <fcntl.h>

#define HANDLE int

static HANDLE get_std_handle(minipal_log_flags flags)
{
    return fileno(get_std_file(flags));
}

static int write_file(HANDLE handle, const char* msg, size_t bytes_to_write)
{
    if (handle == -1)
        return 0;

    if (msg == NULL || msg[0] == '\0')
        return 0;

    return write(handle, msg, bytes_to_write);
}

static void sync_file(HANDLE handle)
{
    if (handle == -1)
        return;

    bool retry = false;
#if HAVE_FSYNC || defined(__APPLE__)
    do
    {
#if defined(__APPLE__)
        if (fcntl(handle, F_FULLFSYNC) != -1)
            break;
#else
        if (fsync(handle) == 0)
            break;
#endif
        switch (errno)
        {
        case EINTR:
            retry = true;
            break;
        default:
            retry = false;
            break;
        }
    } while (retry);
#else
    sync();
#endif
}
#endif

int minipal_log_write(minipal_log_flags flags, const char* msg)
{
    if (msg == NULL || msg[0] == '\0')
        return 0;

    size_t bytes_to_write = strlen(msg);
    size_t bytes_written = 0;

    HANDLE handle = get_std_handle(flags);
    while (bytes_to_write > 0)
    {
        size_t chunk_to_write = bytes_to_write < MINIPAL_LOG_MAX_PAYLOAD ? bytes_to_write : MINIPAL_LOG_MAX_PAYLOAD;
        size_t chunk_written = write_file(handle, msg, chunk_to_write);

        if (chunk_written == 0)
            break;

        msg = msg + chunk_written;
        bytes_to_write -= chunk_written;
        bytes_written += chunk_written;
    }

    return (int)bytes_written;
}

void minipal_log_sync(minipal_log_flags flags)
{
    sync_file(get_std_handle(flags));
}
#endif
