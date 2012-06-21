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
#ifndef __MONO_SGEN_MEMORY_GOVERNOR_H__
#define __MONO_SGEN_MEMORY_GOVERNOR_H__

void sgen_memgov_init (glong max_heap, glong soft_limit) MONO_INTERNAL;
void sgen_memgov_release_space (mword size, int space) MONO_INTERNAL;
gboolean sgen_memgov_try_alloc_space (mword size, int space) MONO_INTERNAL;
mword sgen_memgov_adjust_allowance (mword allowance_estimate, mword new_heap_size) MONO_INTERNAL;
mword sgen_memgov_min_allowance (void) MONO_INTERNAL;
mword sgen_memgov_available_free_space (void) MONO_INTERNAL;

#endif

