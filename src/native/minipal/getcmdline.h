// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_GETCMDLINE_H
#define HAVE_MINIPAL_GETCMDLINE_H

#include <errno.h>
#include <limits.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>

#if defined(__APPLE__)
#include <mach-o/dyld.h>
#include <sys/sysctl.h>
#elif defined(__FreeBSD__)
#include <string.h>
#include <sys/types.h>
#include <sys/param.h>
#include <sys/sysctl.h>
#elif defined(__OpenBSD__)
#include <string.h>
#include <unistd.h>
#include <sys/stat.h>
#include <sys/sysctl.h>
#elif defined(_WIN32)
#include <windows.h>
#elif defined(__HAIKU__)
#include <FindDirectory.h>
#include <StorageDefs.h>
#elif defined(TARGET_WASI)
#include <string.h>
#elif HAVE_GETAUXVAL
#include <sys/auxv.h>
#endif

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Get the raw command line for the current process as an array of strings.
 * The returned array is NULL-terminated (the last element is NULL).
 * The caller is responsible for releasing the memory by calling a single free() on the returned pointer.
 *
 * @param count A pointer to an integer that receives the number of arguments returned.
 * @return A NULL-terminated array of null-terminated strings, or NULL if an error occurs.
 */
static inline char** minipal_getcmdline(int* count)
{
    if (count == NULL)
    {
        return NULL;
    }
    *count = 0;

#if defined(TARGET_BROWSER)
    size_t allocSize = (2 * sizeof(char*)) + 2;
    char** resultArgv = (char**)malloc(allocSize);
    if (resultArgv == NULL)
    {
        return NULL;
    }
    char* stringBuffer = (char*)(resultArgv + 2);
    stringBuffer[0] = '/';
    stringBuffer[1] = '\0';

    resultArgv[0] = stringBuffer;
    resultArgv[1] = NULL;

    *count = 1;
    return resultArgv;

#elif defined(__linux__) || defined(__sun)
    int fd = open("/proc/self/cmdline", O_RDONLY);
    if (fd < 0)
    {
        return NULL;
    }

    size_t bufSize = 1024;
    char* buf = (char*)malloc(bufSize);
    size_t totalBytes = 0;
    ssize_t bytesRead;

    while ((bytesRead = read(fd, buf + totalBytes, bufSize - totalBytes - 1)) > 0)
    {
        totalBytes += (size_t)bytesRead;
        if (totalBytes >= bufSize - 1)
        {
            bufSize *= 2;
            char* newBuf = (char*)realloc(buf, bufSize);
            if (newBuf == NULL)
            {
                free(buf);
                close(fd);
                return NULL;
            }
            buf = newBuf;
        }
    }
    close(fd);

    if (totalBytes == 0)
    {
        free(buf);
        return NULL;
    }
    buf[totalBytes] = '\0';

    int argc = 0;
    for (size_t i = 0; i < totalBytes; i++)
    {
        if (buf[i] == '\0')
        {
            argc++;
        }
    }
    if (totalBytes > 0 && buf[totalBytes - 1] != '\0')
    {
        argc++;
    }

    size_t allocSize = ((size_t)(argc + 1) * sizeof(char*)) + totalBytes + 1;
    char** resultArgv = (char**)malloc(allocSize);
    if (resultArgv == NULL)
    {
        free(buf);
        return NULL;
    }

    char* stringBuffer = (char*)(resultArgv + argc + 1);
    memcpy(stringBuffer, buf, totalBytes + 1);
    free(buf);

    int idx = 0;
    char* ptr = stringBuffer;
    while (idx < argc)
    {
        resultArgv[idx++] = ptr;
        ptr += strlen(ptr) + 1;
    }
    resultArgv[argc] = NULL;

    *count = argc;
    return resultArgv;

#elif defined(__FreeBSD__)
    int name[] = { CTL_KERN, KERN_PROC, KERN_PROC_ARGS, getpid() };
    size_t len = 0;
    if (sysctl(name, 4, NULL, &len, NULL, 0) != 0 || len == 0)
    {
        return NULL;
    }

    char* sysctlBuf = (char*)malloc(len);
    if (sysctlBuf == NULL)
    {
        return NULL;
    }

    if (sysctl(name, 4, sysctlBuf, &len, NULL, 0) != 0)
    {
        free(sysctlBuf);
        return NULL;
    }

    int argc = 0;
    for (size_t i = 0; i < len; i++)
    {
        if (sysctlBuf[i] == '\0')
        {
            argc++;
        }
    }

    size_t allocSize = ((size_t)(argc + 1) * sizeof(char*)) + len;
    char** resultArgv = (char**)malloc(allocSize);
    if (resultArgv == NULL)
    {
        free(sysctlBuf);
        return NULL;
    }

    char* stringBuffer = (char*)(resultArgv + argc + 1);
    memcpy(stringBuffer, sysctlBuf, len);
    free(sysctlBuf);

    int idx = 0;
    char* ptr = stringBuffer;
    while (idx < argc)
    {
        resultArgv[idx++] = ptr;
        ptr += strlen(ptr) + 1;
    }
    resultArgv[argc] = NULL;

    *count = argc;
    return resultArgv;

#elif defined(__OpenBSD__)
    int name[] = { CTL_KERN, KERN_PROC_ARGS, getpid(), KERN_PROC_ARGV };
    size_t len = 0;
    if (sysctl(name, 4, NULL, &len, NULL, 0) != 0 || len == 0)
    {
        return NULL;
    }

    char* sysctlBuf = (char*)malloc(len);
    if (sysctlBuf == NULL)
    {
        return NULL;
    }

    if (sysctl(name, 4, sysctlBuf, &len, NULL, 0) != 0)
    {
        free(sysctlBuf);
        return NULL;
    }

    char** openbsdArgv = (char**)sysctlBuf;
    int argc = 0;
    size_t totalStringBytes = 0;
    while (openbsdArgv[argc] != NULL)
    {
        totalStringBytes += strlen(openbsdArgv[argc]) + 1;
        argc++;
    }

    size_t allocSize = ((size_t)(argc + 1) * sizeof(char*)) + totalStringBytes;
    char** resultArgv = (char**)malloc(allocSize);
    if (resultArgv == NULL)
    {
        free(sysctlBuf);
        return NULL;
    }

    char* stringBuffer = (char*)(resultArgv + argc + 1);
    for (int i = 0; i < argc; i++)
    {
        resultArgv[i] = stringBuffer;
        size_t argLen = strlen(openbsdArgv[i]) + 1;
        memcpy(stringBuffer, openbsdArgv[i], argLen);
        stringBuffer += argLen;
    }
    resultArgv[argc] = NULL;

    *count = argc;
    free(sysctlBuf);
    return resultArgv;
#elif defined(__APPLE__)
    int name[] = { CTL_KERN, KERN_PROCARGS2, getpid() };
    size_t len = 0;
    if (sysctl(name, 3, NULL, &len, NULL, 0) != 0)
    {
        return NULL;
    }

    char* sysctlBuf = (char*)malloc(len);
    if (sysctlBuf == NULL)
    {
        return NULL;
    }

    if (sysctl(name, 3, sysctlBuf, &len, NULL, 0) != 0)
    {
        free(sysctlBuf);
        return NULL;
    }

    int argc = 0;
    memcpy(&argc, sysctlBuf, sizeof(argc));

    char* ptr = sysctlBuf + sizeof(argc);
    ptr += strlen(ptr) + 1;

    while (ptr < sysctlBuf + len && *ptr == '\0')
    {
        ptr++;
    }

    char* startPtr = ptr;
    size_t totalStringBytes = 0;
    int actualCount = 0;
    for (int i = 0; i < argc; i++)
    {
        if (ptr >= sysctlBuf + len)
        {
            break;
        }
        size_t argLen = strlen(ptr) + 1;
        totalStringBytes += argLen;
        ptr += argLen;
        actualCount++;
    }

    size_t allocSize = (((size_t)actualCount + 1) * sizeof(char*)) + totalStringBytes;
    char** resultArgv = (char**)malloc(allocSize);
    if (resultArgv == NULL)
    {
        free(sysctlBuf);
        return NULL;
    }

    char* stringBuffer = (char*)(resultArgv + actualCount + 1);
    if (totalStringBytes > 0)
    {
        memcpy(stringBuffer, startPtr, totalStringBytes);
    }
    free(sysctlBuf);

    char* fillPtr = stringBuffer;
    for (int i = 0; i < actualCount; i++)
    {
        resultArgv[i] = fillPtr;
        fillPtr += strlen(fillPtr) + 1;
    }
    resultArgv[actualCount] = NULL;

    *count = actualCount;
    return resultArgv;

#elif defined(__HAIKU__)
    image_info info;
    int32 cookie = 0;
    while (get_next_image_info(B_CURRENT_TEAM, &cookie, &info) == B_OK)
    {
        if (info.type == B_APP_IMAGE)
        {
            size_t len = strlen(info.args);
            int argc = 0;

            if (len > 0) argc = 1;
            for (size_t i = 0; i < len; i++)
            {
                if (info.args[i] == ' ') argc++;
            }

            size_t allocSize = ((size_t)(argc + 1) * sizeof(char*)) + len + 1;
            char** resultArgv = (char**)malloc(allocSize);
            if (resultArgv == NULL)
            {
                return NULL;
            }

            char* stringBuffer = (char*)(resultArgv + argc + 1);
            memcpy(stringBuffer, info.args, len + 1);

            int idx = 0;
            char* scanPtr = stringBuffer;
            if (len > 0)
            {
                resultArgv[idx++] = scanPtr;
            }
            for (size_t i = 0; i < len; i++)
            {
                if (stringBuffer[i] == ' ')
                {
                    stringBuffer[i] = '\0';
                    resultArgv[idx++] = &stringBuffer[i + 1];
                }
            }
            resultArgv[argc] = NULL;

            *count = argc;
            return resultArgv;
        }
    }
    return NULL;

#elif defined(TARGET_WASI)
    unsigned short __wasi_args_sizes_get(size_t *retptr0, size_t *retptr1) __attribute__((__import_module__("wasi_snapshot_preview1"), __import_name__("args_sizes_get")));
    unsigned short __wasi_args_get(uint8_t **argv, uint8_t *argv_buf) __attribute__((__import_module__("wasi_snapshot_preview1"), __import_name__("args_get")));

    size_t argc = 0;
    size_t argvBufSize = 0;
    if (__wasi_args_sizes_get(&argc, &argvBufSize) != 0)
    {
        return NULL;
    }

    char** wasiArgv = (char**)malloc((argc + 1) * sizeof(char*));
    char* wasiBuf = (char*)malloc(argvBufSize);
    if (wasiArgv == NULL || wasiBuf == NULL)
    {
        free(wasiArgv);
        free(wasiBuf);
        return NULL;
    }

    if (__wasi_args_get((uint8_t**)wasiArgv, (uint8_t*)wasiBuf) != 0)
    {
        free(wasiArgv);
        free(wasiBuf);
        return NULL;
    }

    size_t allocSize = ((argc + 1) * sizeof(char*)) + argvBufSize;
    char** resultArgv = (char**)malloc(allocSize);
    if (resultArgv == NULL)
    {
        free(wasiArgv);
        free(wasiBuf);
        return NULL;
    }

    char* stringBuffer = (char*)((char**)resultArgv + argc + 1);
    memcpy(stringBuffer, wasiBuf, argvBufSize);

    size_t wasiOffset = 0;
    size_t k = 0;
wasi_rebase:
    if (k < argc)
    {
        resultArgv[k] = stringBuffer + wasiOffset;
        wasiOffset += strlen(stringBuffer + wasiOffset) + 1;
        k++;
        goto wasi_rebase;
    }
    resultArgv[argc] = NULL;

    *count = (int)argc;

    free(wasiArgv);
    free(wasiBuf);
    return resultArgv;

#else
    return NULL;
#endif
}

#ifdef __cplusplus
}
#endif // extern "C"

#endif // HAVE_MINIPAL_GETCMDLINE_H
