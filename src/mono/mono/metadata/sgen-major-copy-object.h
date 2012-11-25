/*
 * sgen-major-copy-object.h: Object copying in the major collectors.
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright (C) 2012 Xamarin Inc
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

#define collector_pin_object(obj, queue) do { \
	if (sgen_ptr_in_nursery (obj)) {	\
		sgen_pin_object (obj, queue);	\
	} else {	\
		g_assert (objsize <= SGEN_MAX_SMALL_OBJ_SIZE);	\
		pin_major_object (obj, queue);	\
	}	\
} while (0)

#define COLLECTOR_SERIAL_ALLOC_FOR_PROMOTION sgen_minor_collector.alloc_for_promotion
#define COLLECTOR_PARALLEL_ALLOC_FOR_PROMOTION sgen_minor_collector.par_alloc_for_promotion


#include "sgen-copy-object.h"
