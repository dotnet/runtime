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

#include <config.h>

#ifdef HAVE_PTHREAD
#include <mono/metadata/threads-pthread-types.h>
#else
#warning "No thread support found!"
#include <mono/metadata/threads-dummy-types.h>
#endif

extern void mono_threads_synchronisation_init(MonoThreadsSync *);
extern void mono_threads_synchronisation_free(MonoThreadsSync *);

#endif /* _MONO_METADATA_THREADS_TYPES_H_ */
