// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// dn-ebr.h - Epoch-Based Reclamation for safe memory reclamation

// Overview
// --------
// Epoch-Based Reclamation (EBR) is a technique for safe, low-overhead memory
// reclamation in concurrent lock-free/read-heavy data structures. Threads that
// wish to access shared objects enter a critical region, during which they may
// read shared pointers without additional per-access synchronization. When an
// object is removed from a shared structure, it is not immediately freed.
// Instead, it is queued for deferred deletion and reclaimed only after all
// threads that could have had a reference (i.e., those that were in a critical
// region at the time of retirement) have subsequently passed through a
// quiescent state.
//
// Resources
// ---------
// - Keir Fraser, "Practical Lock-Freedom":
//   https://www.cl.cam.ac.uk/techreports/UCAM-CL-TR-579.pdf
//
// This implementation
// -------------------
// - Per-collector epochs cycle over a small fixed range [0, DN_EBR_NUM_EPOCHS-1].
// - Each thread calls dn_ebr_enter_critical_region() before reading shared state
//   and dn_ebr_exit_critical_region() when finished.
// - When retiring an object, call dn_ebr_queue_for_deletion(). The object will be
//   reclaimed after all threads have passed through a quiescent point.
// - A memory budget can be provided to trigger periodic reclamation.
// - A fatal error callback is required to report unrecoverable conditions
//   (e.g., TLS/OS allocation failures or invalid arguments).
//
//
// Typical usage
// -------------
//   // At startup initialize a collector (likely a global singleton). The collector defines
//   // the memory budget for pending deletions, the allocator used for internal bookkeeping,
//   // and the fatal error callback. The collector allows up to budget bytes to be in a pending
//   // deletion state before attempting reclamation.
//   dn_ebr_collector_t storage;
//   dn_ebr_collector_t *collector = dn_ebr_collector_init(&storage, /*budget*/ 1<<17,
//                                                  DN_DEFAULT_ALLOCATOR, fatal_cb);
//
//   // For each object type that needs to have deferred deletion define traits that will
//   // will estimate the object's memory size and free it. Objects can be arbitrarily complex
//   // graphs using arbitrary allocators as long as provided function pointers handle them correctly.
//   // Memory estimates don't have to be accurate, but those are the sizes that will be used to determine
//   // when to trigger reclamation.
//   typedef struct Node { /* ... */ } Node;
//
//   static void node_delete(void *p) { free(p); }
//   static size_t node_size(void *p) { (void)p; return sizeof(Node); }
//
//   static const dn_ebr_deletion_traits_t node_traits = {
//       .estimate_size = node_size,
//       .delete_object = node_delete,
//   };
//
//   // Instances of these objects are expected to have pointers stored in some shared location.
//   // When threads want to access these objects safely they enter a critical region. The critical
//   // region is not a lock - multiple threads can be in critical regions simultaneously. Threads
//   // need to at least occasionally leave the critical region so that old objects can be reclaimed
//   // but its fine to re-enter rapidly if need be.
//   dn_ebr_enter_critical_region(collector);
//
//   // read shared pointers safely, optionally swap and retire old pointers:
//   //   old = atomic_exchange(&g_shared, new);
//   //   if (old) dn_ebr_queue_for_deletion(collector, old, &node_traits);
//   dn_ebr_exit_critical_region(collector);
//
//   // At process shutdown, after threads quiesce we can clean up any remaining objects if desired
//   dn_ebr_collector_shutdown(collector);
//
//
// Performance
// -----------
// EBR is designed to be low-overhead for read-heavy workloads with infrequent
// updates. There is an ebr-bench microbenchmark app that can measure the critical region enter/exit
// overhead in the tests directory. On my local dev machine I'm seeing approximately 7ns per call pair
// and it could likely be improved a bit further if a scenario needed it.

#ifndef __DN_EBR_H__
#define __DN_EBR_H__

#include "dn-utils.h"
#include "dn-allocator.h"
#include <minipal/mutex.h>
#include <minipal/tls.h>
#include <stdbool.h>

#ifdef __cplusplus
extern "C" {
#endif

// ============================================
// Types
// ============================================

typedef struct _dn_ebr_collector_t dn_ebr_collector_t;
typedef struct _dn_ebr_deletion_traits_t dn_ebr_deletion_traits_t;

// Callback to estimate the size of an object (for budget tracking)
typedef size_t (*dn_ebr_estimate_size_func_t)(void *object);

// Callback to delete an object
typedef void (*dn_ebr_delete_func_t)(void *object);

// Callback invoked on fatal errors (TLS allocation failure, bookkeeping allocation failure,
// OS API failures, or invalid arguments). After this callback returns, the operation that
// triggered the error will return without completing its work.
typedef void (*dn_ebr_fatal_callback_t)(const char *message);

// Deletion traits - describes how to handle a particular type of retired object
// Instances must remain valid until all objects using them have been deleted
struct _dn_ebr_deletion_traits_t {
	dn_ebr_estimate_size_func_t estimate_size;  // Required, must not be NULL
	dn_ebr_delete_func_t delete_object;         // Required, must not be NULL
};

// ============================================
// Internal types exposed for allocation needs
// ============================================

// Number of epoch slots
#define DN_EBR_NUM_EPOCHS 3

typedef struct _dn_ebr_thread_data_t dn_ebr_thread_data_t;

struct _dn_ebr_collector_t {
    // Configuration
    size_t memory_budget;
    dn_allocator_t *allocator;
    dn_ebr_fatal_callback_t fatal_callback;

    // Per-collector TLS slot
    minipal_tls_key tls_key;
    bool tls_initialized;

    // Epoch management
    volatile uint32_t global_epoch;

    // Thread tracking
    minipal_mutex thread_list_lock;
    dn_ebr_thread_data_t *thread_list_head;

    // Pending deletions - one list per epoch slot
    minipal_mutex pending_lock;
    struct _dn_ebr_pending_t *pending_heads[DN_EBR_NUM_EPOCHS];
    volatile size_t pending_size;
};

// ============================================
// Collector Lifecycle
// ============================================

// Initialize an EBR collector in caller-provided memory
// memory_budget: target max size of pending deletions before attempting reclamation
// allocator: used only for internal bookkeeping (thread list, deletion queues); NULL for malloc/free
// fatal_callback: invoked on fatal errors; must not be NULL
// Returns collector on success, NULL on failure (after invoking fatal_callback)
dn_ebr_collector_t *
dn_ebr_collector_init (
	dn_ebr_collector_t *collector,
	size_t memory_budget,
	dn_allocator_t *allocator,
	dn_ebr_fatal_callback_t fatal_callback);

// Shutdown an EBR collector, releasing internal resources
// All threads should be unregistered before calling
// Deletes any remaining pending objects
void
dn_ebr_collector_shutdown (dn_ebr_collector_t *collector);

// ============================================
// Thread Registration
// ============================================

// Explicitly register the current thread with the collector
// Optional - threads are auto-registered on first dn_ebr_enter_critical_region
// Safe to call multiple times (subsequent calls are no-ops)
void
dn_ebr_register_thread (dn_ebr_collector_t *collector);

// Unregister the current thread from the collector
// Called automatically on thread exit (via TLS destructor) on supported platforms
// May be called explicitly for early cleanup
// Must not be called from within a critical region
void
dn_ebr_unregister_thread (dn_ebr_collector_t *collector);

// ============================================
// Critical Region
// ============================================

// Enter a critical region
// While in a critical region, objects queued for deletion will not be freed
// Re-entrant: nested calls increment a counter; only outermost affects epoch
void
dn_ebr_enter_critical_region (dn_ebr_collector_t *collector);

// Exit a critical region
// Must be paired with dn_ebr_enter_critical_region
// When exiting outermost region, thread becomes quiescent
void
dn_ebr_exit_critical_region (dn_ebr_collector_t *collector);

// ============================================
// Object Deletion
// ============================================

// Queue an object for deferred deletion
// Must be called from within a critical region
// The object will be deleted (via traits->delete_object) once all threads
// have passed through a quiescent state
// If memory budget is exceeded, attempts reclamation before returning
void
dn_ebr_queue_for_deletion (
	dn_ebr_collector_t *collector,
	void *object,
	const dn_ebr_deletion_traits_t *traits);

// ============================================
// Utilities
// ============================================

// Returns true if the current thread is in a critical region for this collector
bool
dn_ebr_in_critical_region (dn_ebr_collector_t *collector);

#ifdef __cplusplus
} // extern "C"
#endif

#endif /* __DN_EBR_H__ */
