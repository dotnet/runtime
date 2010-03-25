/*
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
#ifndef __MONO_SGENGC_H__
#define __MONO_SGENGC_H__

/* pthread impl */
#include <pthread.h>

#define ARCH_THREAD_TYPE pthread_t
#define ARCH_GET_THREAD pthread_self
#define ARCH_THREAD_EQUALS(a,b) pthread_equal (a, b)

/*
 * Recursion is not allowed for the thread lock.
 */
#define LOCK_DECLARE(name) pthread_mutex_t name = PTHREAD_MUTEX_INITIALIZER
#define LOCK_INIT(name)
#define LOCK_GC pthread_mutex_lock (&gc_mutex)
#define UNLOCK_GC pthread_mutex_unlock (&gc_mutex)
#define LOCK_INTERRUPTION pthread_mutex_lock (&interruption_mutex)
#define UNLOCK_INTERRUPTION pthread_mutex_unlock (&interruption_mutex)

/* non-pthread will need to provide their own version of start/stop */
#define USE_SIGNAL_BASED_START_STOP_WORLD 1
/* we intercept pthread_create calls to know which threads exist */
#define USE_PTHREAD_INTERCEPT 1

#endif /* __MONO_SGENGC_H__ */

