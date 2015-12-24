//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

// common.h : include file for standard system include files,
// or project specific include files that are used frequently, but
// are changed infrequently
//

#pragma once

#define _CRT_SECURE_NO_WARNINGS

#include <stdint.h>
#include <stddef.h>
#include <stdio.h>
#include <wchar.h>
#include <assert.h>
#include <stdarg.h>
#include <memory.h>

#include <new>

#ifdef PLATFORM_UNIX
#include <pthread.h>
#endif

using namespace std;
