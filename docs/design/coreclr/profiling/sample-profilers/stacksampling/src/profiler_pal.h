// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#ifndef WIN32
#include <cstdlib>
#include "pal_mstypes.h"
#include "pal.h"
#include "ntimage.h"
#include "corhdr.h"

#define CoTaskMemAlloc(cb) malloc(cb)
#define CoTaskMemFree(cb) free(cb)

#define UINT_PTR_FORMAT "lx"

#define PROFILER_STUB __attribute__((visibility("hidden"))) EXTERN_C void STDMETHODCALLTYPE

#else
#define PROFILER_STUB EXTERN_C void STDMETHODCALLTYPE
#define UINT_PTR_FORMAT "llx"
#endif
