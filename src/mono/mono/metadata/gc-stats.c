/*
 * gc-stats.c: GC statistics.
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

#include "mono/sgen/gc-internal-agnostic.h"

/*
 * Due to a bug in the linker on Darwin we need to initialize this struct, or there will be
 * "undefined symbol" errors.
 */
#if defined(__APPLE__)
GCStats gc_stats = {};
#else
GCStats gc_stats;
#endif