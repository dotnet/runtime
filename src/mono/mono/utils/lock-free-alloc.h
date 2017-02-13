/**
 * \file
 * Lock free allocator.
 *
 * (C) Copyright 2011 Novell, Inc
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

#ifndef __MONO_LOCKFREEALLOC_H__
#define __MONO_LOCKFREEALLOC_H__

#include <glib.h>
#include <mono/utils/lock-free-queue.h>
#include <mono/utils/mono-mmap.h>

typedef struct {
	MonoLockFreeQueue partial;
	unsigned int slot_size;
	unsigned int block_size;
} MonoLockFreeAllocSizeClass;

struct _MonoLockFreeAllocDescriptor;

typedef struct {
	struct _MonoLockFreeAllocDescriptor *active;
	MonoLockFreeAllocSizeClass *sc;
	MonoMemAccountType account_type;
} MonoLockFreeAllocator;

#define LOCK_FREE_ALLOC_SB_MAX_SIZE					16384
#define LOCK_FREE_ALLOC_SB_HEADER_SIZE				(sizeof (gpointer))
#define LOCK_FREE_ALLOC_SB_USABLE_SIZE(block_size)	((block_size) - LOCK_FREE_ALLOC_SB_HEADER_SIZE)

MONO_API void mono_lock_free_allocator_init_size_class (MonoLockFreeAllocSizeClass *sc, unsigned int slot_size, unsigned int block_size);
MONO_API void mono_lock_free_allocator_init_allocator (MonoLockFreeAllocator *heap, MonoLockFreeAllocSizeClass *sc, MonoMemAccountType account_type);

MONO_API gpointer mono_lock_free_alloc (MonoLockFreeAllocator *heap);
MONO_API void mono_lock_free_free (gpointer ptr, size_t block_size);

MONO_API gboolean mono_lock_free_allocator_check_consistency (MonoLockFreeAllocator *heap);

#endif
