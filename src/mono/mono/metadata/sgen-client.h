/*
 * sgen-client.h: SGen client interface.
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

#include <metadata/sgen-pointer-queue.h>

void sgen_client_init_early (void);
void sgen_client_init (void);

mword sgen_client_slow_object_get_size (GCVTable *vtable, GCObject* o);

/*
 * Returns the vtable used for dummy objects to fill the nursery for ease and speed of
 * walking.  Must be a valid vtable that is not used by any actual objects.  Must be
 * idempotent.
 */
GCVTable* sgen_client_get_array_fill_vtable (void);

/*
 * Fill the given range with a dummy object.  Its vtable must be the one returned by
 * `sgen_client_get_array_fill_vtable()`.  If the range is too short to be filled with an
 * object, null it.  Return `TRUE` if the range was filled with an object, `FALSE` if it was
 * nulled.
 */
gboolean sgen_client_array_fill_range (char *start, size_t size);

/*
 * This is called if the nursery clearing policy at `clear-at-gc`, which is usually only
 * used for debugging.  If `size` is large enough for the memory to have been filled with a
 * dummy, object, zero its header.  Note that there might not actually be a header there.
 */
void sgen_client_zero_array_fill_header (void *p, size_t size);

gboolean sgen_client_object_is_array_fill (GCObject *o);

gboolean sgen_client_object_has_critical_finalizer (GCObject *obj);

/*
 * Called after an object is enqueued for finalization.  This is a very low-level callback.
 * It should almost certainly be a NOP.
 *
 * FIXME: Can we merge this with `sgen_client_object_has_critical_finalizer()`?
 */
void sgen_client_object_queued_for_finalization (GCObject *obj);

gboolean sgen_client_mark_ephemerons (ScanCopyContext ctx);

/*
 * Clear ephemeron pairs with unreachable keys.
 * We pass the copy func so we can figure out if an array was promoted or not.
 */
void sgen_client_clear_unreachable_ephemerons (ScanCopyContext ctx);

/*
 * This is called for objects that are larger than one card.  If it's possible to scan only
 * parts of the object based on which cards are marked, do so and return TRUE.  Otherwise,
 * return FALSE.
 */
gboolean sgen_client_cardtable_scan_object (char *obj, mword block_obj_size, guint8 *cards, gboolean mod_union, ScanCopyContext ctx);

void sgen_client_nursery_objects_pinned (void **definitely_pinned, int count);

void sgen_client_collecting_minor (SgenPointerQueue *fin_ready_queue, SgenPointerQueue *critical_fin_queue);
void sgen_client_collecting_major_1 (void);
void sgen_client_pinned_los_object (char *obj);
void sgen_client_collecting_major_2 (void);
void sgen_client_collecting_major_3 (SgenPointerQueue *fin_ready_queue, SgenPointerQueue *critical_fin_queue);

void sgen_client_degraded_allocation (size_t size);

void sgen_client_total_allocated_heap (mword allocated_heap);

/*
 * If the client has registered any internal memory types, this must return a string
 * describing the given type.  Only used for debugging.
 */
const char* sgen_client_description_for_internal_mem_type (int type);

/* FIXME: Use `sgen_client_vtable_get_name()` instead of this. */
const char* sgen_client_object_safe_name (GCObject *obj);

const char* sgen_client_vtable_get_namespace (GCVTable *vtable);
const char* sgen_client_vtable_get_name (GCVTable *vtable);

void sgen_client_pre_collection_checks (void);

void sgen_client_thread_register (SgenThreadInfo* info, void *stack_bottom_fallback);

void sgen_client_scan_thread_data (void *start_nursery, void *end_nursery, gboolean precise, ScanCopyContext ctx);

int sgen_client_stop_world (int generation);
int sgen_client_restart_world (int generation, GGTimingInfo *timing);

void sgen_client_log_timing (GGTimingInfo *info, mword last_major_num_sections, mword last_los_memory_usage);

gboolean sgen_client_handle_gc_param (const char *opt);
void sgen_client_print_gc_params_usage (void);

gboolean sgen_client_handle_gc_debug (const char *opt);
void sgen_client_print_gc_debug_usage (void);

#define TYPE_INT int
#define TYPE_LONGLONG long long
#define TYPE_SIZE size_t
#define TYPE_POINTER gpointer
#define TYPE_BOOL gboolean

#define BEGIN_PROTOCOL_ENTRY0(method) \
	void sgen_client_ ## method (void);
#define BEGIN_PROTOCOL_ENTRY_HEAVY0(method) \
	void sgen_client_ ## method (void);
#define BEGIN_PROTOCOL_ENTRY1(method,t1,f1) \
	void sgen_client_ ## method (t1 f1);
#define BEGIN_PROTOCOL_ENTRY_HEAVY1(method,t1,f1) \
	void sgen_client_ ## method (t1 f1);
#define BEGIN_PROTOCOL_ENTRY2(method,t1,f1,t2,f2) \
	void sgen_client_ ## method (t1 f1, t2 f2);
#define BEGIN_PROTOCOL_ENTRY_HEAVY2(method,t1,f1,t2,f2) \
	void sgen_client_ ## method (t1 f1, t2 f2);
#define BEGIN_PROTOCOL_ENTRY3(method,t1,f1,t2,f2,t3,f3) \
	void sgen_client_ ## method (t1 f1, t2 f2, t3 f3);
#define BEGIN_PROTOCOL_ENTRY_HEAVY3(method,t1,f1,t2,f2,t3,f3) \
	void sgen_client_ ## method (t1 f1, t2 f2, t3 f3);
#define BEGIN_PROTOCOL_ENTRY4(method,t1,f1,t2,f2,t3,f3,t4,f4) \
	void sgen_client_ ## method (t1 f1, t2 f2, t3 f3, t4 f4);
#define BEGIN_PROTOCOL_ENTRY_HEAVY4(method,t1,f1,t2,f2,t3,f3,t4,f4) \
	void sgen_client_ ## method (t1 f1, t2 f2, t3 f3, t4 f4);
#define BEGIN_PROTOCOL_ENTRY5(method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5) \
	void sgen_client_ ## method (t1 f1, t2 f2, t3 f3, t4 f4, t5 f5);
#define BEGIN_PROTOCOL_ENTRY_HEAVY5(method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5) \
	void sgen_client_ ## method (t1 f1, t2 f2, t3 f3, t4 f4, t5 f5);
#define BEGIN_PROTOCOL_ENTRY6(method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5,t6,f6) \
	void sgen_client_ ## method (t1 f1, t2 f2, t3 f3, t4 f4, t5 f5, t6 f6);
#define BEGIN_PROTOCOL_ENTRY_HEAVY6(method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5,t6,f6) \
	void sgen_client_ ## method (t1 f1, t2 f2, t3 f3, t4 f4, t5 f5, t6 f6);

#define FLUSH()

#define DEFAULT_PRINT()
#define CUSTOM_PRINT(_)

#define IS_ALWAYS_MATCH(_)
#define MATCH_INDEX(_)
#define IS_VTABLE_MATCH(_)

#define END_PROTOCOL_ENTRY
#define END_PROTOCOL_ENTRY_HEAVY

#include "sgen-protocol-def.h"

#undef TYPE_INT
#undef TYPE_LONGLONG
#undef TYPE_SIZE
#undef TYPE_POINTER
#undef TYPE_BOOL
