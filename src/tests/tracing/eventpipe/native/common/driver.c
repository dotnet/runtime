// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
 * Derived from the EGLib Unit Test Driver.
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

#include <stdio.h>

int run_tests_main(const Group *groups, int argc, char **argv)
{
    if (argc == 2 && (strcmp(argv[1], "--list") == 0 || strcmp(argv[1], "--help") == 0))
    {
        if (strcmp(argv[1], "--help") == 0)
            printf("Usage: %s [--list | GROUP ...]\n", argv[0]);

        for (const Group *group = groups; group->name != NULL; group++)
            puts(group->name);

        return EXIT_SUCCESS;
    }

    for (int i = 1; i < argc; i++)
    {
        bool found = false;
        for (const Group *group = groups; group->name != NULL; group++)
        {
            if (strcmp(argv[i], group->name) == 0)
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            fprintf(stderr, "Unknown test group '%s'. Use --list to list groups.\n", argv[i]);
            return EXIT_FAILURE;
        }
    }

    size_t passed = 0;
    size_t total = 0;
    bool success = true;
    for (const Group *group = groups; group->name != NULL; group++)
    {
        bool selected = argc == 1;
        for (int i = 1; i < argc && !selected; i++)
            selected = strcmp(argv[i], group->name) == 0;

        if (selected && !run_group(group, &passed, &total))
            success = false;
    }

    if (total == 0)
    {
        fputs("No native tests executed.\n", stderr);
        success = false;
    }

    printf("Overall result: %s : %zu / %zu\n", success ? "OK" : "FAILED", passed, total);
    return success ? EXIT_SUCCESS : EXIT_FAILURE;
}
