// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _MSLAYOUT_H_
#define _MSLAYOUT_H_

// Structures marshalled between different platforms must have the same layout.
// On x86, use MSVC-style packing instead of GCC's default packing.
#if defined(__GNUC__) && defined(HOST_X86)
#define MSLAYOUT __attribute__((__ms_struct__))
#else
#define MSLAYOUT
#endif

#endif // _MSLAYOUT_H_
