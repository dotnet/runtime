// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef DN_SAFECRT_H
#define DN_SAFECRT_H

// The Microsoft CRT secure functions that shared CoreCLR code uses. Windows gets them from its CRT;
// elsewhere they are implemented in Unix/dn-safecrt.cpp.
#ifndef HOST_WINDOWS

#include <stdarg.h>
#include <stddef.h>
#include <stdlib.h>
#include <string.h>

typedef int errno_t;

#ifndef STRUNCATE
#define STRUNCATE 80
#endif

#ifndef _TRUNCATE
#define _TRUNCATE ((size_t)-1)
#endif

#ifdef __cplusplus
extern "C"
{
#endif

int sprintf_s(char *string, size_t sizeInBytes, const char *format, ...);
int _snprintf_s(char *string, size_t sizeInBytes, size_t count, const char *format, ...);
int vsprintf_s(char *string, size_t sizeInBytes, const char *format, va_list arglist);
int _vsnprintf_s(char *string, size_t sizeInBytes, size_t count, const char *format, va_list arglist);
int sscanf_s(const char *string, const char *format, ...);
errno_t memcpy_s(void *dst, size_t sizeInBytes, const void *src, size_t count);
errno_t memmove_s(void *dst, size_t sizeInBytes, const void *src, size_t count);

#ifdef __cplusplus
}

// Returns the length of the formatted string, excluding the terminator.
inline int _vscprintf(const char *format, va_list argList)
{
    int guess = 10;

    while (true)
    {
        char *buf = (char *)malloc(guess * sizeof(char));
        if (buf == nullptr)
            return 0;

        va_list argListCopy;
        va_copy(argListCopy, argList);
        int ret = _vsnprintf_s(buf, guess, (size_t)-1 /* _TRUNCATE */, format, argListCopy);
        free(buf);
        va_end(argListCopy);

        if ((ret != -1) && (ret < guess))
            return ret;

        guess *= 2;
    }
}

// The string functions below abort on invalid parameters, as the Windows CRT does by default.
namespace dn_safecrt
{
    // Debug builds fill the unused tail [offset, size) to catch reads past the terminator.
    template <typename T>
    inline void FillString(T *dst, size_t size, size_t offset)
    {
#ifdef _DEBUG
        if (offset < size)
            memset(dst + offset, 0xFD, (size - offset) * sizeof(T));
#endif
    }

    template <typename T>
    inline errno_t Copy(T *dst, size_t size, const T *src)
    {
        if (dst == nullptr || size == 0 || src == nullptr)
            abort();

        T *p = dst;
        size_t available = size;
        while ((*p++ = *src++) != 0 && --available > 0)
        {
        }

        if (available == 0)
            abort();

        FillString(dst, size, size - available + 1);
        return 0;
    }

    template <typename T>
    inline errno_t CopyN(T *dst, size_t size, const T *src, size_t count)
    {
        if (count == 0 && dst == nullptr && size == 0)
            return 0;

        if (dst == nullptr || size == 0)
            abort();

        if (count == 0)
        {
            // src can be null in this case.
            *dst = 0;
            FillString(dst, size, 1);
            return 0;
        }

        if (src == nullptr)
            abort();

        T *p = dst;
        size_t available = size;
        if (count == _TRUNCATE)
        {
            while ((*p++ = *src++) != 0 && --available > 0)
            {
            }
        }
        else
        {
            while ((*p++ = *src++) != 0 && --available > 0 && --count > 0)
            {
            }

            if (count == 0)
                *p = 0;
        }

        if (available == 0)
        {
            if (count != _TRUNCATE)
                abort();

            dst[size - 1] = 0;
            return STRUNCATE;
        }

        FillString(dst, size, size - available + 1);
        return 0;
    }

    template <typename T>
    inline errno_t Concat(T *dst, size_t size, const T *src)
    {
        if (dst == nullptr || size == 0 || src == nullptr)
            abort();

        T *p = dst;
        size_t available = size;
        while (available > 0 && *p != 0)
        {
            p++;
            available--;
        }

        if (available == 0)
            abort(); // dst is not terminated

        while ((*p++ = *src++) != 0 && --available > 0)
        {
        }

        if (available == 0)
            abort();

        FillString(dst, size, size - available + 1);
        return 0;
    }

    template <typename T>
    inline errno_t ConcatN(T *dst, size_t size, const T *src, size_t count)
    {
        if (count == 0 && dst == nullptr && size == 0)
            return 0;

        if (dst == nullptr || size == 0 || (count != 0 && src == nullptr))
            abort();

        T *p = dst;
        size_t available = size;
        while (available > 0 && *p != 0)
        {
            p++;
            available--;
        }

        if (available == 0)
            abort(); // dst is not terminated

        if (count == _TRUNCATE)
        {
            while ((*p++ = *src++) != 0 && --available > 0)
            {
            }
        }
        else
        {
            while (count > 0 && (*p++ = *src++) != 0 && --available > 0)
            {
                count--;
            }

            if (count == 0)
                *p = 0;
        }

        if (available == 0)
        {
            if (count != _TRUNCATE)
                abort();

            dst[size - 1] = 0;
            return STRUNCATE;
        }

        FillString(dst, size, size - available + 1);
        return 0;
    }
}

inline errno_t strcpy_s(char *dst, size_t size, const char *src) { return dn_safecrt::Copy(dst, size, src); }
inline errno_t wcscpy_s(char16_t *dst, size_t size, const char16_t *src) { return dn_safecrt::Copy(dst, size, src); }
inline errno_t strncpy_s(char *dst, size_t size, const char *src, size_t count) { return dn_safecrt::CopyN(dst, size, src, count); }
inline errno_t wcsncpy_s(char16_t *dst, size_t size, const char16_t *src, size_t count) { return dn_safecrt::CopyN(dst, size, src, count); }
inline errno_t strcat_s(char *dst, size_t size, const char *src) { return dn_safecrt::Concat(dst, size, src); }
inline errno_t wcscat_s(char16_t *dst, size_t size, const char16_t *src) { return dn_safecrt::Concat(dst, size, src); }
inline errno_t strncat_s(char *dst, size_t size, const char *src, size_t count) { return dn_safecrt::ConcatN(dst, size, src, count); }
inline errno_t wcsncat_s(char16_t *dst, size_t size, const char16_t *src, size_t count) { return dn_safecrt::ConcatN(dst, size, src, count); }
#endif

#endif // !HOST_WINDOWS

#endif // DN_SAFECRT_H
