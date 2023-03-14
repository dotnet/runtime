// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ---------------------------------------------------------------------------
// unreachable.h
// ---------------------------------------------------------------------------


#ifndef __UNREACHABLE_H__
#define __UNREACHABLE_H__

#if defined(_MSC_VER) || defined(_PREFIX_)
#define __UNREACHABLE() __assume(0)
#else
#define __UNREACHABLE() __builtin_unreachable()
#endif

#endif // __UNREACHABLE_H__

