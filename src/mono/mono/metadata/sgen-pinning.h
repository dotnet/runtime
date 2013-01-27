/*
 * sgen-pinning.h: All about pinning objects.
 *
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
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
#ifndef __MONO_SGEN_PINNING_H__
#define __MONO_SGEN_PINNING_H__

enum {
	PIN_TYPE_STACK,
	PIN_TYPE_STATIC_DATA,
	PIN_TYPE_OTHER,
	PIN_TYPE_MAX
};

void sgen_pin_stage_ptr (void *ptr) MONO_INTERNAL;
void sgen_optimize_pin_queue (int start_slot) MONO_INTERNAL;
void sgen_init_pinning (void) MONO_INTERNAL;
void sgen_finish_pinning (void) MONO_INTERNAL;
void sgen_pin_queue_clear_discarded_entries (GCMemSection *section, int max_pin_slot) MONO_INTERNAL;
int sgen_get_pinned_count (void) MONO_INTERNAL;
void sgen_pinning_setup_section (GCMemSection *section) MONO_INTERNAL;
void sgen_pinning_trim_queue_to_section (GCMemSection *section) MONO_INTERNAL;

void sgen_dump_pin_queue (void) MONO_INTERNAL;

/* Pinning stats */

void sgen_pin_stats_register_address (char *addr, int pin_type) MONO_INTERNAL;
size_t sgen_pin_stats_get_pinned_byte_count (int pin_type) MONO_INTERNAL;
ObjectList *sgen_pin_stats_get_object_list (void) MONO_INTERNAL;
void sgen_pin_stats_reset (void) MONO_INTERNAL;

/* Perpetual pinning, aka cementing */

void sgen_cement_init (gboolean enabled) MONO_INTERNAL;
void sgen_cement_reset (void) MONO_INTERNAL;
void sgen_cement_concurrent_start (void) MONO_INTERNAL;
void sgen_cement_concurrent_finish (void) MONO_INTERNAL;
gboolean sgen_cement_lookup (char *obj) MONO_INTERNAL;
gboolean sgen_cement_lookup_or_register (char *obj) MONO_INTERNAL;
void sgen_cement_iterate (IterateObjectCallbackFunc callback, void *callback_data) MONO_INTERNAL;
void sgen_cement_clear_below_threshold (void) MONO_INTERNAL;

#endif
