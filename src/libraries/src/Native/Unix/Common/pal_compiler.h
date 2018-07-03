// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#ifndef __has_extension
#define __has_extension(...) 0
#endif

#ifdef static_assert
#define c_static_assert(e) static_assert((e),"")
#elif __has_extension(c_static_assert)
#define c_static_assert(e) _Static_assert((e), "")
#else
#define c_static_assert(e) typedef char __c_static_assert__[(e)?1:-1]
#endif

#define DLLEXPORT __attribute__ ((__visibility__ ("default")))
