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
extern long long stat_scan_object_called_nursery;
extern long long stat_scan_object_called_major;

#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	do {	\
		void *__old = *(ptr);	\
		void *__copy;		\
		if (__old) {	\
			copy_object ((ptr), queue);	\
			__copy = *(ptr);	\
			DEBUG (9, if (__old != __copy) fprintf (gc_debug_file, "Overwrote field at %p with %p (was: %p)\n", (ptr), *(ptr), __old));	\
			if (G_UNLIKELY (ptr_in_nursery (__copy) && !ptr_in_nursery ((ptr)))) \
				mono_sgen_add_to_global_remset ((ptr));	\
		}	\
	} while (0)

/*
 * Scan the object pointed to by @start for references to
 * other objects between @from_start and @from_end and copy
 * them to the gray_objects area.
 */
static void
minor_scan_object (char *start, SgenGrayQueue *queue)
{
#include "sgen-scan-object.h"

	HEAVY_STAT (++stat_scan_object_called_nursery);
}

/*
 * scan_vtype:
 *
 * Scan the valuetype pointed to by START, described by DESC for references to
 * other objects between @from_start and @from_end and copy them to the gray_objects area.
 * Returns a pointer to the end of the object.
 */
static void
minor_scan_vtype (char *start, mword desc, SgenGrayQueue *queue)
{
	/* The descriptors include info about the MonoObject header as well */
	start -= sizeof (MonoObject);

#define SCAN_OBJECT_NOVTABLE
#include "sgen-scan-object.h"
}

#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	do {	\
		void *__old = *(ptr);	\
		void *__copy;		\
		if (__old) {	\
			nopar_copy_object ((ptr), queue);	\
			__copy = *(ptr);	\
			DEBUG (9, if (__old != __copy) fprintf (gc_debug_file, "Overwrote field at %p with %p (was: %p)\n", (ptr), *(ptr), __old));	\
			if (G_UNLIKELY (ptr_in_nursery (__copy) && !ptr_in_nursery ((ptr)))) \
				mono_sgen_add_to_global_remset ((ptr));	\
		}	\
	} while (0)

static void
nopar_minor_scan_object (char *start, SgenGrayQueue *queue)
{
#include "sgen-scan-object.h"

	HEAVY_STAT (++stat_scan_object_called_nursery);
}

static void
nopar_minor_scan_vtype (char *start, mword desc, SgenGrayQueue *queue)
{
	/* The descriptors include info about the MonoObject header as well */
	start -= sizeof (MonoObject);

#define SCAN_OBJECT_NOVTABLE
#include "sgen-scan-object.h"
}

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
			DEBUG (9, if (__old != __copy) mono_sgen_debug_printf (9, "Overwrote field at %p with %p (was: %p)\n", (ptr), *(ptr), __old)); \
			if (G_UNLIKELY (ptr_in_nursery (__copy) && !ptr_in_nursery ((ptr)))) \
				mono_sgen_add_to_global_remset ((ptr));	\
		}							\
	} while (0)

static void
major_scan_object (char *start, SgenGrayQueue *queue)
{
#include "sgen-scan-object.h"

	HEAVY_STAT (++stat_scan_object_called_major);
}

#define FILL_COLLECTOR_SCAN_OBJECT(collector)	do {			\
		(collector)->major_scan_object = major_scan_object;	\
		(collector)->minor_scan_object = minor_scan_object;	\
		(collector)->nopar_minor_scan_object = nopar_minor_scan_object;	\
		(collector)->minor_scan_vtype = minor_scan_vtype;	\
		(collector)->nopar_minor_scan_vtype = nopar_minor_scan_vtype; \
	} while (0)
