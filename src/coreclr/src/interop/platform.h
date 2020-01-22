// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _INTEROP_PLATFORM_H_
#define _INTEROP_PLATFORM_H_

#include <cstdint>
#include <cstring>

#ifdef _WIN32
#include <Windows.h>
#endif // _WIN32

#define ABI_ASSERT(abi_definition) static_assert((abi_definition), "ABI is being invalidated.")

// BEGIN [TODO] Remove
#include <cassert>
#include <cstdlib>

#ifndef _ASSERTE
#define _ASSERTE(x) assert((x))
#endif
// END [TODO] Remove

#endif // _INTEROP_PLATFORM_H_