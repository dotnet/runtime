// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// common.h : include file for standard system include files,
// or project specific include files that are used frequently, but
// are changed infrequently
//

#pragma once

#ifndef _CRT_SECURE_NO_WARNINGS
 #define _CRT_SECURE_NO_WARNINGS
#endif // _CRT_SECURE_NO_WARNINGS

#include <cstddef>
#include <assert.h>
#include <inttypes.h>
#include <stdio.h>
#include <string.h>
#include <stdlib.h>

#if defined(FEATURE_NATIVEAOT) && !defined(TARGET_WINDOWS)
#include "CommonTypes.h"
#else
#include <new>
#endif

#ifdef TARGET_UNIX
#include <pthread.h>
#endif

using namespace std;
