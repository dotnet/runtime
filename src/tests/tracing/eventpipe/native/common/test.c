// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
 * Derived from the EGLib Unit Group/Test Runners.
 * Copyright (C) 2006 Novell, Inc.
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#include "test.h"

#include <stdarg.h>
#include <stdio.h>

bool run_group(const Group *group, size_t *passed, size_t *total)
{
    size_t group_passed = 0;
    size_t group_total = 0;

    printf("[%s]\n", group->name);

    for (const Test *test = group->handler(); test->name != NULL; test++)
    {
        printf("  %s: ", test->name);
        fflush(stdout);
        RESULT result = test->handler();
        group_total++;

        if (result == OK)
        {
            group_passed++;
            printf("OK\n");
        }
        else
        {
            printf("FAILED (%s)\n", result);
            free(result);
        }
    }

    *passed += group_passed;
    *total += group_total;
    printf("  %zu / %zu\n", group_passed, group_total);

    if (group_total == 0)
    {
        fprintf(stderr, "No tests registered in group '%s'.\n", group->name);
        return false;
    }

    return group_passed == group_total;
}

RESULT FAILED(const char *format, ...)
{
    va_list args;
    va_start(args, format);
    int length = vsnprintf(NULL, 0, format, args);
    va_end(args);

    if (length < 0)
    {
        fputs("Unable to format native test failure.\n", stderr);
        abort();
    }

    size_t size = (size_t)length + 1;
    char *result = malloc(size);
    if (result == NULL)
    {
        fputs("Unable to allocate native test failure.\n", stderr);
        abort();
    }

    va_start(args, format);
    int written = vsnprintf(result, size, format, args);
    va_end(args);

    if (written != length)
    {
        fputs("Unable to format native test failure.\n", stderr);
        free(result);
        abort();
    }

    return result;
}
