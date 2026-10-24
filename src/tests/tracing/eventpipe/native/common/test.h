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

#ifndef NATIVE_TEST_RUNNER_H
#define NATIVE_TEST_RUNNER_H

#include <stdbool.h>
#include <stddef.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

typedef char *RESULT;
typedef RESULT (*RunTestHandler)(void);

typedef struct
{
    const char *name;
    RunTestHandler handler;
} Test;

typedef const Test *(*LoadGroupHandler)(void);

typedef struct
{
    const char *name;
    LoadGroupHandler handler;
} Group;

#define OK NULL
#define DEFINE_TEST_GROUP_INIT(name, table) const Test *name(void) { return table; }
#define DEFINE_TEST_GROUP_INIT_H(name) const Test *name(void)

#undef FAILED
RESULT FAILED(const char *format, ...);
bool run_group(const Group *group, size_t *passed, size_t *total);
int run_tests_main(const Group *groups, int argc, char **argv);

#endif // NATIVE_TEST_RUNNER_H
