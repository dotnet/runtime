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

#if SIZEOF_REGISTER == 4
typedef gint32 mgreg_t;
#elif SIZEOF_REGISTER == 8
typedef gint64 mgreg_t;
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
