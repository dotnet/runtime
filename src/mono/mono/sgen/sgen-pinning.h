/**
 * \file
 * All about pinning objects.
 *
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
 * Copyright (C) 2012 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_SGEN_PINNING_H__
#define __MONO_SGEN_PINNING_H__

#include "mono/sgen/sgen-pointer-queue.h"

enum {
	PIN_TYPE_STACK,
	PIN_TYPE_STATIC_DATA,
	PIN_TYPE_OTHER,
	PIN_TYPE_MAX
};

void sgen_pinning_init (void);
void sgen_pin_stage_ptr (void *ptr);
void sgen_optimize_pin_queue (void);
void sgen_init_pinning (void);
void sgen_init_pinning_for_conc (void);
void sgen_finish_pinning_for_conc (void);
void sgen_finish_pinning (void);
void sgen_pinning_register_pinned_in_nursery (GCObject *obj);
void sgen_scan_pin_queue_objects (ScanCopyContext ctx);
void sgen_pin_queue_clear_discarded_entries (GCMemSection *section, size_t max_pin_slot);
size_t sgen_get_pinned_count (void);
void sgen_pinning_setup_section (GCMemSection *section);
void sgen_pinning_trim_queue_to_section (GCMemSection *section);

void sgen_dump_pin_queue (void);

gboolean sgen_find_optimized_pin_queue_area (void *start, void *end, size_t *first_out, size_t *last_out);
void sgen_find_section_pin_queue_start_end (GCMemSection *section);
void** sgen_pinning_get_entry (size_t index);
void sgen_pin_objects_in_section (GCMemSection *section, ScanCopyContext ctx);

/* Pinning stats */

void sgen_pin_stats_register_address (char *addr, int pin_type);
size_t sgen_pin_stats_get_pinned_byte_count (int pin_type);
SgenPointerQueue *sgen_pin_stats_get_object_list (void);
void sgen_pin_stats_reset (void);

/* Perpetual pinning, aka cementing */

void sgen_cement_init (gboolean enabled);
void sgen_cement_reset (void);
void sgen_cement_force_pinned (void);
gboolean sgen_cement_is_forced (GCObject *obj);
gboolean sgen_cement_lookup (GCObject *obj);
gboolean sgen_cement_lookup_or_register (GCObject *obj);
void sgen_pin_cemented_objects (void);
void sgen_cement_clear_below_threshold (void);

#endif
