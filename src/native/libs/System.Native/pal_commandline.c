// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Kept separate from pal_process.c so that statically linked hosts (NativeAOT) only pull in the
// .init_array initializer below when SystemNative_GetCommandLine is referenced.

#include "pal_config.h"
#include "pal_process.h"

#include <errno.h>
#include <fcntl.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#if HAVE_CRT_EXTERNS_H
#include <crt_externs.h>
#endif

#if defined(__FreeBSD__) || defined(__OpenBSD__)
#include <sys/types.h>
#include <sys/param.h>
#include <sys/sysctl.h>
#endif

#if defined(TARGET_BROWSER) || defined(__APPLE__) || defined(__OpenBSD__) || defined(__HAIKU__) || defined(__GLIBC__) || defined(TARGET_ANDROID)
// Packs the arguments into a single NULL-terminated allocation that can be released with one free().
static char** PackCommandLine(int32_t count, const char* const* args, int32_t* argc)
{
    size_t stringBytes = 0;
    for (int32_t i = 0; i < count; i++)
    {
        stringBytes += strlen(args[i]) + 1;
    }

    char** result = (char**)malloc(((size_t)count + 1) * sizeof(char*) + stringBytes);
    if (result == NULL)
    {
        return NULL;
    }

    char* strings = (char*)(result + count + 1);
    for (int32_t i = 0; i < count; i++)
    {
        size_t length = strlen(args[i]) + 1;
        memcpy(strings, args[i], length);
        result[i] = strings;
        strings += length;
    }

    result[count] = NULL;
    *argc = count;
    return result;
}
#endif

#if defined(__linux__) || defined(__sun) || defined(__FreeBSD__)
// Packs into a single NULL-terminated allocation up to maxCount arguments stored back to back as
// NUL-terminated strings, so that it can be released with one free().
static char** PackNulSeparatedCommandLine(const char* buffer, size_t length, int32_t maxCount, int32_t* argc)
{
    int32_t count = 0;
    size_t used = 0;
    while (used < length && count < maxCount)
    {
        const char* end = (const char*)memchr(buffer + used, '\0', length - used);
        used = end != NULL ? (size_t)(end - buffer) + 1 : length;
        count++;
    }

    char** result = (char**)malloc(((size_t)count + 1) * sizeof(char*) + used + 1);
    if (result == NULL)
    {
        return NULL;
    }

    char* strings = (char*)(result + count + 1);
    memcpy(strings, buffer, used);
    strings[used] = '\0';

    for (int32_t i = 0; i < count; i++)
    {
        result[i] = strings;
        strings += strlen(strings) + 1;
    }

    result[count] = NULL;
    *argc = count;
    return result;
}
#endif

#if defined(__FreeBSD__) || defined(__OpenBSD__)
// Returns a malloc'ed buffer with the sysctl value, or NULL on failure.
static char* ReadSysctl(int* mib, u_int mibLength, size_t* length)
{
    *length = 0;
    if (sysctl(mib, mibLength, NULL, length, NULL, 0) != 0 || *length == 0)
    {
        return NULL;
    }

    char* buffer = (char*)malloc(*length);
    if (buffer != NULL && sysctl(mib, mibLength, buffer, length, NULL, 0) != 0)
    {
        free(buffer);
        buffer = NULL;
    }

    return buffer;
}
#endif

#if defined(__HAIKU__)
extern int __libc_argc;
extern char** __libc_argv;
#endif

#if defined(__GLIBC__) || defined(TARGET_ANDROID)
static int s_initArgc;
static char** s_initArgv;

// glibc and bionic pass argc, argv and envp to .init_array functions as a non-standard extension,
// including for libraries loaded with dlopen. Other libcs, such as musl, pass no arguments.
static void CaptureCommandLine(int argc, char** argv, char** envp)
{
    (void)envp;
    s_initArgc = argc;
    s_initArgv = argv;
}

__attribute__((section(".init_array"), used))
static void (*const s_captureCommandLine)(int, char**, char**) = &CaptureCommandLine;
#endif

char** SystemNative_GetCommandLine(int32_t* argc)
{
    *argc = 0;

#if defined(TARGET_BROWSER)
    static const char* const browserArgs[] = { "/" }; // keep in sync with browserVirtualAppBase in minipal_getexepath
    return PackCommandLine(1, browserArgs, argc);
#elif defined(__linux__) || defined(__sun)
#if defined(__GLIBC__) || defined(TARGET_ANDROID)
    if (s_initArgv != NULL)
    {
        // Some command line parsers (e.g. GLib and Qt) move consumed arguments to the end of argv
        // and replace them with NULL without updating argc.
        int32_t count = 0;
        while (count < s_initArgc && s_initArgv[count] != NULL)
        {
            count++;
        }

        return PackCommandLine(count, (const char* const*)s_initArgv, argc);
    }
#endif

    int fd;
    while ((fd = open("/proc/self/cmdline", O_RDONLY | O_CLOEXEC)) < 0 && errno == EINTR);
    if (fd < 0)
    {
        return NULL;
    }

    size_t capacity = 4096;
    size_t length = 0;
    char* buffer = (char*)malloc(capacity);
    while (buffer != NULL)
    {
        ssize_t bytesRead;
        while ((bytesRead = read(fd, buffer + length, capacity - length)) < 0 && errno == EINTR);
        if (bytesRead <= 0)
        {
            if (bytesRead < 0)
            {
                free(buffer);
                buffer = NULL;
            }
            break;
        }

        length += (size_t)bytesRead;
        if (length == capacity)
        {
            capacity *= 2;
            char* newBuffer = (char*)realloc(buffer, capacity);
            if (newBuffer == NULL)
            {
                free(buffer);
            }
            buffer = newBuffer;
        }
    }
    close(fd);

    if (buffer == NULL)
    {
        return NULL;
    }

    char** result = PackNulSeparatedCommandLine(buffer, length, INT32_MAX, argc);
    free(buffer);
    return result;
#elif defined(__FreeBSD__)
    int mib[] = { CTL_KERN, KERN_PROC, KERN_PROC_ARGS, getpid() };
    size_t length;
    char* buffer = ReadSysctl(mib, 4, &length);
    if (buffer == NULL)
    {
        return NULL;
    }

    char** result = PackNulSeparatedCommandLine(buffer, length, INT32_MAX, argc);
    free(buffer);
    return result;
#elif defined(__OpenBSD__)
    int mib[] = { CTL_KERN, KERN_PROC_ARGS, getpid(), KERN_PROC_ARGV };
    size_t length;
    char* buffer = ReadSysctl(mib, 4, &length);
    if (buffer == NULL)
    {
        return NULL;
    }

    // The buffer starts with a NULL-terminated argv array pointing into the rest of the buffer.
    char** args = (char**)buffer;
    int32_t count = 0;
    while (args[count] != NULL)
    {
        count++;
    }

    char** result = PackCommandLine(count, (const char* const*)args, argc);
    free(buffer);
    return result;
#elif defined(__APPLE__)
    return PackCommandLine(*_NSGetArgc(), (const char* const*)*_NSGetArgv(), argc);
#elif defined(__HAIKU__)
    return PackCommandLine(__libc_argc, (const char* const*)__libc_argv, argc);
#else
    return NULL;
#endif
}
