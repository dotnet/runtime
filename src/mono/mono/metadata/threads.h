/*
 * threads.h: Generic thread support (includes system-specific files)
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2001 Ximian, Inc
 */

#ifndef _MONO_METADATA_THREADS_H_
#define _MONO_METADATA_THREADS_H_

#include <config.h>

extern void mono_thread_init(void);
extern void mono_thread_cleanup(void);

#ifdef HAVE_PTHREAD_H
#include <mono/metadata/threads-pthread.h>
#else
#warning "No thread support found!"
#include <mono/metadata/threads-dummy.h>
#endif

#endif /* _MONO_METADATA_THREADS_H_ */
