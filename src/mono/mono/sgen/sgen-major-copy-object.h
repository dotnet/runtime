/**
 * \file
 * Object copying in the major collectors.
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright (C) 2012 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#define collector_pin_object(obj, queue) do { \
	if (sgen_ptr_in_nursery (obj)) {	\
		sgen_pin_object (obj, queue);	\
	} else {	\
		g_assert (objsize <= SGEN_MAX_SMALL_OBJ_SIZE);	\
		pin_major_object (obj, queue);	\
	}	\
} while (0)

#define COLLECTOR_SERIAL_ALLOC_FOR_PROMOTION sgen_minor_collector.alloc_for_promotion
#define COLLECTOR_PARALLEL_ALLOC_FOR_PROMOTION sgen_minor_collector.alloc_for_promotion_par

#include "sgen-copy-object.h"
