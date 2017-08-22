/**
 * \file
 * Architecture dependent parts of SGen.
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright (C) 2012 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_SGENARCHDEP_H__
#define __MONO_SGENARCHDEP_H__

#include <mono/utils/mono-context.h>

#if defined(MONO_CROSS_COMPILE)

#define REDZONE_SIZE	0

#elif defined(TARGET_X86)

#define REDZONE_SIZE	0

#ifndef MONO_ARCH_HAS_MONO_CONTEXT
#error 0
#endif

#elif defined(TARGET_AMD64)

#ifdef HOST_WIN32
/* The Windows x64 ABI defines no "red zone". The ABI states:
   "All memory beyond the current address of RSP is considered volatile" */
#define REDZONE_SIZE	0
#else
#define REDZONE_SIZE	128
#endif

#elif defined(TARGET_POWERPC)

#define REDZONE_SIZE	224

#elif defined(TARGET_ARM)

#define REDZONE_SIZE	0

#elif defined(TARGET_ARM64)

#ifdef __linux__
#define REDZONE_SIZE    0
#elif defined(__APPLE__)
#define REDZONE_SIZE	128
#else
#error "Not implemented."
#endif

#elif defined(__mips__)

#define REDZONE_SIZE	0

#elif defined(__s390x__)

#define REDZONE_SIZE	0

#elif defined(__sparc__)

#define REDZONE_SIZE	0

#elif defined (TARGET_WASM)

#define REDZONE_SIZE	0

#endif

#endif /* __MONO_SGENARCHDEP_H__ */
