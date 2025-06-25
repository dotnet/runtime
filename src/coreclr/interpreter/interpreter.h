// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>
#include <wchar.h>
#include <stdio.h>
#include <stddef.h>
#include <stdlib.h>
#include <limits.h>
#include <string.h>
#include <float.h>
#include <cstdlib>
#include <cmath>
#include <assert.h>
#ifdef HOST_WINDOWS
#include <malloc.h>
#endif

#include "corhdr.h"
#include "corjit.h"

#include "interpretershared.h"
#include "compiler.h"
#include "interpconfig.h"

#define ALIGN_UP_TO(val,align) ((((size_t)val) + (size_t)((align) - 1)) & (~((size_t)(align - 1))))

#ifdef _DEBUG
extern "C" void assertAbort(const char* why, const char* file, unsigned line);
#undef assert
#define assert(p) (void)((p) || (assertAbort(#p, __FILE__, __LINE__), 0))
#endif // _DEBUG
