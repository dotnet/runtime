/*
 * sgen-cardtable.c: Card table implementation for sgen
 *
 * Author:
 * 	Rodrigo Kumpera (rkumpera@novell.com)
 *
 * SGen is licensed under the terms of the MIT X11 license
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
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

#include "metadata/sgen-gc.h"
#include "metadata/sgen-memory-governor.h"
#include "utils/mono-counters.h"

#define MIN_MINOR_COLLECTION_ALLOWANCE	((mword)(DEFAULT_NURSERY_SIZE * SGEN_MIN_ALLOWANCE_NURSERY_SIZE_RATIO))

/*heap limits*/
static mword max_heap_size = ((mword)0)- ((mword)1);
static mword soft_heap_limit = ((mword)0) - ((mword)1);
static mword allocated_heap;


mword
sgen_memgov_available_free_space (void)
{
	return max_heap_size - MIN (allocated_heap, max_heap_size);
}

mword
sgen_memgov_min_allowance (void)
{
	return MIN_MINOR_COLLECTION_ALLOWANCE;
}

mword
sgen_memgov_adjust_allowance (mword allowance_estimate, mword new_heap_size)
{
	if (new_heap_size + allowance_estimate > soft_heap_limit) {
		if (new_heap_size > soft_heap_limit)
			return MIN_MINOR_COLLECTION_ALLOWANCE;
		else
			return MAX (soft_heap_limit - new_heap_size, MIN_MINOR_COLLECTION_ALLOWANCE);
	}
	return allowance_estimate;
}

void
sgen_memgov_release_space (mword size, int space)
{
	allocated_heap -= size;
}

gboolean
sgen_memgov_try_alloc_space (mword size, int space)
{
	if (sgen_memgov_available_free_space () < size)
		return FALSE;

	allocated_heap += size;
	mono_runtime_resource_check_limit (MONO_RESOURCE_GC_HEAP, allocated_heap);
	return TRUE;
}

void
sgen_memgov_init (glong max_heap, glong soft_limit)
{
	if (soft_limit)
		soft_heap_limit = soft_limit;

	if (max_heap == 0)
		return;

	if (max_heap < soft_limit) {
		fprintf (stderr, "max-heap-size must be at least as large as soft-heap-limit.\n");
		exit (1);
	}

	if (max_heap < sgen_nursery_size * 4) {
		fprintf (stderr, "max-heap-size must be at least 4 times larger than nursery size.\n");
		exit (1);
	}
	max_heap_size = max_heap - sgen_nursery_size;
}

#endif
