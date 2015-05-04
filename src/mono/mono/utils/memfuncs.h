/*
 * memfuncs.h: Our own bzero/memmove.
 *
 * Copyright (C) 2015 Xamarin Inc
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Library General Public
 * License 2.0 as published by the Free Software Foundation;
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Library General Public License for more details.
 *
 * You should have received a copy of the GNU Library General Public
 * License 2.0 along with this library; if not, write to the Free
 * Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
 */

#ifndef __MONO_UTILS_MEMFUNCS_H__
#define __MONO_UTILS_MEMFUNCS_H__

#include <stdlib.h>

/*
These functions must be used when it's possible that either destination is not
word aligned or size is not a multiple of word size.
*/
void mono_gc_bzero_atomic (void *dest, size_t size);
void mono_gc_bzero_aligned (void *dest, size_t size);
void mono_gc_memmove_atomic (void *dest, const void *src, size_t size);
void mono_gc_memmove_aligned (void *dest, const void *src, size_t size);

#endif
