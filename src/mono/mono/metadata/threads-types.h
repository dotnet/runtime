/*
 * threads-types.h: Generic thread typedef support (includes
 * system-specific files)
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2001 Ximian, Inc
 */

#ifndef _MONO_METADATA_THREADS_TYPES_H_
#define _MONO_METADATA_THREADS_TYPES_H_

#include <glib.h>

#include <mono/io-layer/io-layer.h>

typedef struct
{
	guint32 owner;			/* thread ID */
	guint32 nest;
	volatile guint32 entry_count;
	HANDLE entry_sem;
	GSList *wait_list;
} MonoThreadsSync;

#endif /* _MONO_METADATA_THREADS_TYPES_H_ */
