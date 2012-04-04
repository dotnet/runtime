/*
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
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

#endif
