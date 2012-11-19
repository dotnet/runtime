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
extern long long stat_scan_object_called_major;

#ifdef FIXED_HEAP
#define PREFETCH_DYNAMIC_HEAP(addr)
#else
#define PREFETCH_DYNAMIC_HEAP(addr)	PREFETCH ((addr))
#endif

#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	do {					\
		void *__old = *(ptr);					\
		void *__copy;						\
		if (__old) {						\
			PREFETCH_DYNAMIC_HEAP (__old);			\
			major_copy_or_mark_object ((ptr), queue);	\
			__copy = *(ptr);				\
			SGEN_COND_LOG (9, __old != __copy, "Overwrote field at %p with %p (was: %p)", (ptr), *(ptr), __old); \
			if (G_UNLIKELY (sgen_ptr_in_nursery (__copy) && !sgen_ptr_in_nursery ((ptr)))) \
				sgen_add_to_global_remset ((ptr));	\
		}							\
	} while (0)

static void
major_scan_object (char *start, SgenGrayQueue *queue)
{
#include "sgen-scan-object.h"

	HEAVY_STAT (++stat_scan_object_called_major);
}
