/*
 * sgen-os-posix.c: Simple generational GC.
 *
 * Author:
 *	Paolo Molaro (lupus@ximian.com)
 *	Mark Probst (mprobst@novell.com)
 * 	Geoff Norton (gnorton@novell.com)
 *
 * Copyright 2010 Novell, Inc (http://www.novell.com)
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

#include "config.h"
#ifdef HAVE_SGEN_GC
#include <glib.h>
#include "metadata/gc-internal.h"
#include "metadata/sgen-gc.h"
#include "metadata/sgen-archdep.h"
#include "metadata/object-internals.h"

#if !defined(__MACH__) && !MONO_MACH_ARCH_SUPPORTED
int
mono_sgen_thread_handshake (int signum)
{
	int count, i, result;
	SgenThreadInfo **thread_table;
	SgenThreadInfo *info;
	pthread_t me = pthread_self ();

	thread_table = mono_sgen_get_thread_table ();
	count = 0;
	for (i = 0; i < THREAD_HASH_SIZE; ++i) {
		for (info = thread_table [i]; info; info = info->next) {
			if (ARCH_THREAD_EQUALS (info->id, me)) {
				continue;
			}
			/*if (signum == suspend_signal_num && info->stop_count == global_stop_count)
				continue;*/
			result = pthread_kill (info->id, signum);
			if (result == 0) {
				count++;
			} else {
				info->skip = 1;
			}
		}
	}

	mono_sgen_wait_for_suspend_ack (count);

	return count;
}
#endif
#endif
