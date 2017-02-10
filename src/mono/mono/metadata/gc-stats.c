/**
 * \file
 * GC statistics.
 *
 * Copyright (C) 2015 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
