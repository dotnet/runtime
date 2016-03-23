/*
 * mono-mmap-internals.h: Internal virtual memory stuff.
 *
 * Copyright (C) 2014 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __MONO_UTILS_MMAP_INTERNAL_H__
#define __MONO_UTILS_MMAP_INTERNAL_H__

#include "mono-compiler.h"

int mono_pages_not_faulted (void *addr, size_t length);

#endif /* __MONO_UTILS_MMAP_INTERNAL_H__ */

