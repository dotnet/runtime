/*
 * mono-mmap-internal.h: Internal virtual memory stuff.
 *
 * Copyright (C) 2014 Xamarin Inc
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

#ifndef __MONO_UTILS_MMAP_INTERNAL_H__
#define __MONO_UTILS_MMAP_INTERNAL_H__

#include "mono-compiler.h"

int mono_pages_not_faulted (void *addr, size_t length);

#endif /* __MONO_UTILS_MMAP_INTERNAL_H__ */

