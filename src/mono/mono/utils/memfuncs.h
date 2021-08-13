/**
 * \file
 * Our own bzero/memmove.
 *
 * Copyright (C) 2015 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __MONO_UTILS_MEMFUNCS_H__
#define __MONO_UTILS_MEMFUNCS_H__

#include <stdlib.h>
#include <glib.h>
#include "mono-compiler.h"

/*
These functions must be used when it's possible that either destination is not
word aligned or size is not a multiple of word size.
*/
void mono_gc_bzero_atomic (void *dest, size_t size);
void mono_gc_bzero_aligned (void *dest, size_t size);
MONO_COMPONENT_API void mono_gc_memmove_atomic (void *dest, const void *src, size_t size);
void mono_gc_memmove_aligned (void *dest, const void *src, size_t size);
guint64 mono_determine_physical_ram_size (void);
guint64 mono_determine_physical_ram_available_size (void);

#endif
