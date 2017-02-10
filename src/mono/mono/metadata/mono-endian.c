/**
 * \file
 *
 * Author:
 *	Mono Project (http://www.mono-project.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <mono/utils/mono-compiler.h>
#include "mono-endian.h"

#if NO_UNALIGNED_ACCESS

typedef union {
	char c [2];
	guint16 i;
} mono_rint16;

typedef union {
	char c [4];
	guint32 i;
} mono_rint32;

typedef union {
	char c [8];
	guint64 i;
} mono_rint64;

guint16 
mono_read16 (const unsigned char *x)
{
	mono_rint16 r;
#if G_BYTE_ORDER == G_LITTLE_ENDIAN
	r.c [0] = x [0];
	r.c [1] = x [1];
#else
	r.c [1] = x [0];
	r.c [0] = x [1];
#endif
	return r.i;
}

guint32 
mono_read32 (const unsigned char *x)
{
	mono_rint32 r;
#if G_BYTE_ORDER == G_LITTLE_ENDIAN
	r.c [0] = x [0];
	r.c [1] = x [1];
	r.c [2] = x [2];
	r.c [3] = x [3];
#else
	r.c [3] = x [0];
	r.c [2] = x [1];
	r.c [1] = x [2];
	r.c [0] = x [3];
#endif
	return r.i;
}

guint64 
mono_read64 (const unsigned char *x)
{
	mono_rint64 r;
#if G_BYTE_ORDER == G_LITTLE_ENDIAN
	r.c [0] = x [0];
	r.c [1] = x [1];
	r.c [2] = x [2];
	r.c [3] = x [3];
	r.c [4] = x [4];
	r.c [5] = x [5];
	r.c [6] = x [6];
	r.c [7] = x [7];
#else
	r.c [7] = x [0];
	r.c [6] = x [1];
	r.c [5] = x [2];
	r.c [4] = x [3];
	r.c [3] = x [4];
	r.c [2] = x [5];
	r.c [1] = x [6];
	r.c [0] = x [7];
#endif
	return r.i;
}

#else /* NO_UNALIGNED_ACCESS */

MONO_EMPTY_SOURCE_FILE (mono_endian);

#endif
