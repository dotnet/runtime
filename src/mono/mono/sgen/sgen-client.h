/**
 * \file
 * SGen client interface.
 *
 * Copyright (C) 2014 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "mono/sgen/sgen-pointer-queue.h"

/*
 * Init whatever needs initing.  This is called relatively early in SGen initialization.
 * Must initialized the small ID for the current thread.
 */
void sgen_client_init (void);

/*
 * The slow path for getting an object's size.  We're passing in the vtable because we've
 * already fetched it.
 */
mword sgen_client_slow_object_get_size (GCVTable vtable, GCObject* o);

/*
 * Fill the given range with a dummy object.  If the range is too short to be filled with an
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

/*
 * Return whether the given object is an array fill dummy object.
 */
gboolean sgen_client_object_is_array_fill (GCObject *o);

/*
 * Return whether the given finalizable object's finalizer is critical, i.e., needs to run
 * after all non-critical finalizers have run.
 */
gboolean sgen_client_object_has_critical_finalizer (GCObject *obj);

/*
 * Called after an object is enqueued for finalization.  This is a very low-level callback.
 * It should almost certainly be a NOP.
 *
 * FIXME: Can we merge this with `sgen_client_object_has_critical_finalizer()`?
 */
void sgen_client_object_queued_for_finalization (GCObject *obj);

/*
 * Run the given object's finalizer.
 */
void sgen_client_run_finalize (GCObject *obj);

/*
 * Is called after a collection if there are objects to finalize.  The world is still
 * stopped.  This will usually notify the finalizer thread that it needs to run.
 */
void sgen_client_finalize_notify (void);

/*
 * Returns TRUE if no ephemerons have been marked.  Will be called again if it returned
 * FALSE.  If ephemerons are not supported, just return TRUE.
 */
gboolean sgen_client_mark_ephemerons (ScanCopyContext ctx)
    MONO_PERMIT (need (sgen_gc_locked));

/*
 * Clear ephemeron pairs with unreachable keys.
 * We pass the copy func so we can figure out if an array was promoted or not.
 */
void sgen_client_clear_unreachable_ephemerons (ScanCopyContext ctx)
    MONO_PERMIT (need (sgen_gc_locked));

/*
 * May return NULL.  Must be an aligned pointer.
 */
gpointer sgen_client_default_metadata (void);
gpointer sgen_client_metadata_for_object (GCObject *obj);

/*
 * No action required.
 */
void sgen_client_gchandle_created (int handle_type, GCObject *obj, guint32 handle);
void sgen_client_gchandle_destroyed (int handle_type, guint32 handle);
void sgen_client_ensure_weak_gchandles_accessible (void);

/*
 * This is called for objects that are larger than one card.  If it's possible to scan only
 * parts of the object based on which cards are marked, do so and return TRUE.  Otherwise,
 * return FALSE.
 */
gboolean sgen_client_cardtable_scan_object (GCObject *obj, guint8 *cards, ScanCopyContext ctx);

/*
 * Called after nursery objects have been pinned.  No action is necessary.
 */
void sgen_client_nursery_objects_pinned (void **definitely_pinned, int count);

/*
 * Called at a semi-random point during minor collections.  No action is necessary.
 */
void sgen_client_collecting_minor_report_roots (SgenPointerQueue *fin_ready_queue, SgenPointerQueue *critical_fin_queue);

/*
 * Called at semi-random points during major collections.  No action is necessary.
 */
void sgen_client_collecting_major_report_roots (SgenPointerQueue *fin_ready_queue, SgenPointerQueue *critical_fin_queue);

/*
 * Called after a LOS object has been pinned.  No action is necessary.
 */
void sgen_client_pinned_los_object (GCObject *obj);

/*
 * Called for each cemented obj
 */
void sgen_client_pinned_cemented_object (GCObject *obj);

/*
 * Called for each major heap obj pinned
 */
void sgen_client_pinned_major_heap_object (GCObject *obj);

void sgen_client_pinning_start (void);
void sgen_client_pinning_end (void);

/*
 * Called for every degraded allocation.  No action is necessary.
 */
void sgen_client_degraded_allocation (void);

/*
 * Called whenever the amount of memory allocated for the managed heap changes.  No action
 * is necessary.
 */
void sgen_client_total_allocated_heap_changed (size_t allocated_heap_size);

/*
 * If the client has registered any internal memory types, this must return a string
 * describing the given type.  Only used for debugging.
 */
const char* sgen_client_description_for_internal_mem_type (int type);

/*
 * Only used for debugging.  `sgen_client_vtable_get_namespace()` may return NULL.
 */
gboolean sgen_client_vtable_is_inited (GCVTable vtable);
const char* sgen_client_vtable_get_namespace (GCVTable vtable);
const char* sgen_client_vtable_get_name (GCVTable vtable);

/*
 * Called before starting collections.  The world is already stopped.  No action is
 * necessary.
 */
void sgen_client_pre_collection_checks (void);

/*
 * Must set the thread's thread info to `info`.  If the thread's small ID was not already
 * initialized in `sgen_client_init()` (for the main thread, usually), it must be done here.
 *
 * `stack_bottom_fallback` is the value passed through via `sgen_thread_attach()`.
 */
void sgen_client_thread_attach (SgenThreadInfo* info);

void sgen_client_thread_detach_with_lock (SgenThreadInfo *p);

/*
 * Called on each worker thread when it starts up.  Must initialize the thread's small ID.
 */
void sgen_client_thread_register_worker (void);

/*
 * The least this function needs to do is scan all registers and thread stacks.  To do this
 * conservatively, use `sgen_conservatively_pin_objects_from()`.
 */
void sgen_client_scan_thread_data (void *start_nursery, void *end_nursery, gboolean precise, ScanCopyContext ctx);

/*
 * Stop and restart the world, i.e., all threads that interact with the managed heap.  For
 * single-threaded programs this is a nop.
 */
void sgen_client_stop_world (int generation)
    MONO_PERMIT (need (sgen_gc_locked));
void sgen_client_restart_world (int generation, gint64 *stw_time)
    MONO_PERMIT (need (sgen_gc_locked));

/*
 * Must return FALSE.  The bridge is not supported outside of Mono.
 */
gboolean sgen_client_bridge_need_processing (void);

/*
 * None of these should ever be called.
 */
void sgen_client_bridge_reset_data (void);
void sgen_client_bridge_processing_stw_step (void);
void sgen_client_bridge_wait_for_processing (void);
void sgen_client_bridge_processing_finish (int generation);
gboolean sgen_client_bridge_is_bridge_object (GCObject *obj);
void sgen_client_bridge_register_finalized_object (GCObject *object);

/*
 * No action is necessary.
 */
void sgen_client_mark_togglerefs (char *start, char *end, ScanCopyContext ctx);
void sgen_client_clear_togglerefs (char *start, char *end, ScanCopyContext ctx);

/*
 * Called to handle `MONO_GC_PARAMS` and `MONO_GC_DEBUG` options.  The `handle` functions
 * must return TRUE if they have recognized and processed the option, FALSE otherwise.
 */
gboolean sgen_client_handle_gc_param (const char *opt);
void sgen_client_print_gc_params_usage (void);
gboolean sgen_client_handle_gc_debug (const char *opt);
void sgen_client_print_gc_debug_usage (void);

/*
 * Called to obtain an identifier for the current location, such as a method pointer. This
 * is used for logging the provenances of allocations with the heavy binary protocol.
 */
gpointer sgen_client_get_provenance (void);

/*
 * Called by the debugging infrastructure to describe pointers that have an invalid vtable.
 * Should usually print to `stdout`.
 */
void sgen_client_describe_invalid_pointer (GCObject *ptr);

/*
 * Return the weak bitmap for a class
 */
gsize *sgen_client_get_weak_bitmap (GCVTable vt, int *nbits);

/*
 * Scheduled @cv to be invoked later in the background.
 *
 * This function is idepotent WRT background execution. Meaning that calling it multiple times with the same funciton pointer before any bg execution happens will only call @cb once.
 */
void sgen_client_schedule_background_job (void (*cb)(void));

/*
 * These client binary protocol functions are called from the respective binary protocol
 * functions.  No action is necessary.  We suggest implementing them as inline functions in
 * the client header file so that no overhead is incurred if they don't actually do
 * anything.
 */

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

#define DEFAULT_PRINT()
#define CUSTOM_PRINT(_)

#define IS_ALWAYS_MATCH(_)
#define MATCH_INDEX(_)
#define IS_VTABLE_MATCH(_)

#define END_PROTOCOL_ENTRY
#define END_PROTOCOL_ENTRY_FLUSH
#define END_PROTOCOL_ENTRY_HEAVY

#include "sgen-protocol-def.h"

#undef TYPE_INT
#undef TYPE_LONGLONG
#undef TYPE_SIZE
#undef TYPE_POINTER
#undef TYPE_BOOL

#ifdef SGEN_WITHOUT_MONO
/*
 * Get the current thread's thread info.  This will only be called on managed threads.
 */
SgenThreadInfo* mono_thread_info_current (void);

/*
 * Get the current thread's small ID.  This will be called on managed and worker threads.
 */
int mono_thread_info_get_small_id (void);

#endif
