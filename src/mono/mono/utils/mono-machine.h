/**
 * \file
 * machine definitions
 *
 * Authors:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * Copyright (c) 2011 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __MONO_MONO_MACHINE_H__
#define __MONO_MONO_MACHINE_H__

/* C type matching the size of a machine register. Not always the same as 'int' */
/* Note that member 'p' of MonoInst must be the same type, as OP_PCONST is defined
 * as one of the OP_ICONST types, so inst_c0 must be the same as inst_p0
 */

#include "config.h"
#include <glib.h>

// __ILP32__ means integer, long, and pointers are 32bits, and nothing about registers.
// MONO_ARCH_ILP32 means integer, long, and pointers are 32bits, and 64bit registers.
// This is for example x32, arm6432, mipsn32, Alpha/NT.
#ifdef MONO_ARCH_ILP32
typedef gint64 host_mgreg_t;
typedef guint64 host_umgreg_t;
#else
typedef gssize host_mgreg_t;
typedef gsize host_umgreg_t;
#endif

/* SIZEOF_REGISTER      ... machine register size of target machine
 * TARGET_SIZEOF_VOID_P ... pointer size of target machine
 *
 * SIZEOF_REGISTER is usually the same as TARGET_SIZEOF_VOID_P, except when MONO_ARCH_ILP32 is defined
 */
#if SIZEOF_REGISTER == 4
typedef gint32 target_mgreg_t;
#else
typedef gint64 target_mgreg_t;
#endif

/* Alignment for MonoArray.vector */
#if defined(_AIX)
/*
 * HACK: doubles in structs always align to 4 on AIX... even on 64-bit,
 * which is bad for aligned usage like what System.Array.FastCopy does
 */
typedef guint64 mono_64bitaligned_t;
#else
typedef double mono_64bitaligned_t;
#endif

#endif /* __MONO_MONO_MACHINE_H__ */
