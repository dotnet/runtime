/**
 * \file
 * Internal virtual memory stuff.
 *
 * Copyright (C) 2014 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __MONO_UTILS_MMAP_INTERNAL_H__
#define __MONO_UTILS_MMAP_INTERNAL_H__

#include "mono-compiler.h"

void *
mono_malloc_shared_area (int pid);

char*
mono_aligned_address (char *mem, size_t size, size_t alignment);

void
mono_account_mem (MonoMemAccountType type, ssize_t size);

gboolean
mono_valloc_can_alloc (size_t size);

void
mono_valloc_set_limit (size_t size);

#endif /* __MONO_UTILS_MMAP_INTERNAL_H__ */
