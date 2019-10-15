// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// util.hpp
//
// Miscellaneous useful functions
//
#ifndef _H_UTIL
#define _H_UTIL

#include <objbase.h>

#if defined(_DEBUG)
#include <crtdbg.h>
#undef _ASSERTE    // utilcode defines a custom _ASSERTE
#endif

#include "utilcode.h"

#endif /* _H_UTIL */
