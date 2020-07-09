// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _INTEROP_PLATFORM_H_
#define _INTEROP_PLATFORM_H_

#include <assert.h>
#include <stdint.h>
#include <string.h>

#ifndef _ASSERTE
#define _ASSERTE(x) assert((x))
#endif

#ifdef _WIN32
#include <Windows.h>
#include <objidl.h> // COM interfaces

// Common macro for working in COM
#define RETURN_IF_FAILED(exp) { hr = exp; if (FAILED(hr)) { _ASSERTE(false && #exp); return hr; } }
#define RETURN_VOID_IF_FAILED(exp) { hr = exp; if (FAILED(hr)) { _ASSERTE(false && #exp); return; } }

#endif // _WIN32

#define ABI_ASSERT(abi_definition) static_assert((abi_definition), "ABI is being invalidated.")

// Runtime headers
#include <volatile.h>

#endif // _INTEROP_PLATFORM_H_
