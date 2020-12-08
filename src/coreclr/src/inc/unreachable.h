// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ---------------------------------------------------------------------------
// unreachable.h
// ---------------------------------------------------------------------------


#ifndef __UNREACHABLE_H__
#define __UNREACHABLE_H__

#if defined(_MSC_VER) || defined(_PREFIX_)
#if defined(TARGET_AMD64)
// Empty methods that consist of UNREACHABLE() result in a zero-sized declspec(noreturn) method
// which causes the pdb file to make the next method declspec(noreturn) as well, thus breaking BBT
// Remove when we get a VC compiler that fixes VSW 449170
# define __UNREACHABLE() do { DebugBreak(); __assume(0); } while (0)
#else
# define __UNREACHABLE() __assume(0)
#endif
#else
#define __UNREACHABLE() __builtin_unreachable()
#endif

#endif // __UNREACHABLE_H__

