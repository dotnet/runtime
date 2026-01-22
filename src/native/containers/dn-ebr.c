// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "dn-ebr.h"
#include <minipal/mutex.h>
#include <minipal/tls.h>
#include <minipal/atomic.h>
#include <string.h>
#include <stdint.h>

// ============================================
// Internal Types
// ============================================

// Number of epoch slots (current, current-1, current-2)
#define DN_EBR_NUM_EPOCHS 3

// Forward declaration
typedef struct _dn_ebr_thread_data_t dn_ebr_thread_data_t;

// Per-thread EBR state
struct _dn_ebr_thread_data_t {
    dn_ebr_collector_t *collector;
    uint32_t local_epoch; // UINT32_MAX indicates quiescent
    uint32_t critical_region_depth;
    dn_ebr_thread_data_t *next_in_collector;  // Link in collector's thread list
};

// Pending deletion entry
typedef struct _dn_ebr_pending_t {
	void *object;
	const dn_ebr_deletion_traits_t *traits;
	size_t estimated_size;
	struct _dn_ebr_pending_t *next;
} dn_ebr_pending_t;

// Collector structure now declared in header (dn-ebr.h)

// ============================================
// TLS Management (per-collector)
// ============================================

static void dn_ebr_tls_destructor (void *data);
static void dn_ebr_unregister_thread_internal (dn_ebr_collector_t *collector, dn_ebr_thread_data_t *thread_data);

static bool
dn_ebr_tls_init (dn_ebr_collector_t *collector)
{
    if (!minipal_tls_key_create (&collector->tls_key, dn_ebr_tls_destructor)) {
        collector->fatal_callback ("dn_ebr: TLS key create failed");
        return false;
    }
    collector->tls_initialized = true;
    return true;
}

static void
dn_ebr_tls_shutdown (dn_ebr_collector_t *collector)
{
    if (collector->tls_initialized) {
        minipal_tls_key_delete (&collector->tls_key);
        collector->tls_initialized = false;
    }
}

static dn_ebr_thread_data_t *
dn_ebr_tls_get (dn_ebr_collector_t *collector)
{
    if (!collector->tls_initialized) {
        collector->fatal_callback ("dn_ebr: TLS get before initialization");
        return NULL;
    }
    return (dn_ebr_thread_data_t *)minipal_tls_get (&collector->tls_key);
}

static bool
dn_ebr_tls_set (dn_ebr_collector_t *collector, dn_ebr_thread_data_t *thread_data)
{
    if (!collector->tls_initialized) {
        collector->fatal_callback ("dn_ebr: TLS set before initialization");
        return false;
    }
    if (!minipal_tls_set (&collector->tls_key, thread_data)) {
        collector->fatal_callback ("dn_ebr: TLS set failed");
        return false;
    }
    return true;
}

// TLS destructor - called automatically on thread exit
static void
dn_ebr_tls_destructor (void *data)
{
	dn_ebr_thread_data_t *thread_data = (dn_ebr_thread_data_t *)data;
	if (!thread_data)
		return;

	dn_ebr_unregister_thread_internal (thread_data->collector, thread_data);
}

// Get or create thread data for this collector
static dn_ebr_thread_data_t *
dn_ebr_get_thread_data (dn_ebr_collector_t *collector)
{
	dn_ebr_thread_data_t *thread_data = dn_ebr_tls_get (collector);
	if (thread_data)
		return thread_data;

	// Allocate new thread data
	thread_data = (dn_ebr_thread_data_t *)dn_allocator_alloc (collector->allocator, sizeof (dn_ebr_thread_data_t));
	if (!thread_data) {
		collector->fatal_callback ("dn_ebr: failed to allocate thread data");
		return NULL;
	}

	memset (thread_data, 0, sizeof (dn_ebr_thread_data_t));
	thread_data->collector = collector;
    thread_data->critical_region_depth = 0;
    // Start quiescent until thread enters its first critical region
    thread_data->local_epoch = UINT32_MAX;

	// Store in TLS
	if (!dn_ebr_tls_set (collector, thread_data)) {
		dn_allocator_free (collector->allocator, thread_data);
		return NULL;
	}

	// Add to collector's thread list
	minipal_mutex_enter (&collector->thread_list_lock);
	thread_data->next_in_collector = collector->thread_list_head;
	collector->thread_list_head = thread_data;
	minipal_mutex_leave (&collector->thread_list_lock);

	return thread_data;
}

// Remove thread data from collector's list (called during unregister)
static void
dn_ebr_unregister_thread_internal (dn_ebr_collector_t *collector, dn_ebr_thread_data_t *thread_data)
{
	if (!thread_data)
		return;

	// Remove from collector's thread list
	minipal_mutex_enter (&collector->thread_list_lock);

	dn_ebr_thread_data_t **pp = &collector->thread_list_head;
	while (*pp) {
		if (*pp == thread_data) {
			*pp = thread_data->next_in_collector;
			break;
		}
		pp = &(*pp)->next_in_collector;
	}

	minipal_mutex_leave (&collector->thread_list_lock);

	// Free the thread data
	dn_allocator_free (collector->allocator, thread_data);
}

// ============================================
// Epoch Management
// ============================================

// Check if all threads have observed the current epoch
static bool
dn_ebr_can_advance_epoch (dn_ebr_collector_t *collector)
{
    uint32_t current_epoch = minipal_atomic_load_u32 (&collector->global_epoch);

	// Must hold thread_list_lock when calling this
	dn_ebr_thread_data_t *thread_data = collector->thread_list_head;
	while (thread_data) {
		// Only active threads (in critical region) matter
        // Ignore quiescent threads (local_epoch == UINT32_MAX)
        if (thread_data->local_epoch != UINT32_MAX) {
			// If any active thread hasn't observed the current epoch, we can't advance
			if (thread_data->local_epoch != current_epoch)
				return false;
		}
		thread_data = thread_data->next_in_collector;
	}

	return true;
}

// Try to advance the global epoch
static bool
dn_ebr_try_advance_epoch (dn_ebr_collector_t *collector)
{
	minipal_mutex_enter (&collector->thread_list_lock);

	bool can_advance = dn_ebr_can_advance_epoch (collector);
	if (can_advance) {
		// Keep epoch in range [0, DN_EBR_NUM_EPOCHS-1]
        uint32_t new_epoch = (collector->global_epoch + 1) % DN_EBR_NUM_EPOCHS;
        minipal_atomic_store_u32 (&collector->global_epoch, new_epoch);
	}

	minipal_mutex_leave (&collector->thread_list_lock);

	return can_advance;
}

// ============================================
// Reclamation
// ============================================

// Delete all objects in a pending queue
static size_t
dn_ebr_drain_queue (dn_ebr_collector_t *collector, uint32_t slot)
{
	size_t freed_size = 0;

	dn_ebr_pending_t *pending = collector->pending_heads[slot];
	collector->pending_heads[slot] = NULL;

	while (pending) {
		dn_ebr_pending_t *next = pending->next;

		// Delete the object
		pending->traits->delete_object (pending->object);
		freed_size += pending->estimated_size;

		// Free the pending entry
		dn_allocator_free (collector->allocator, pending);

		pending = next;
	}

	return freed_size;
}

// Attempt to reclaim objects from old epochs
static void
dn_ebr_try_reclaim (dn_ebr_collector_t *collector)
{
	// Try to advance the epoch
	// If successful, objects from 2 epochs ago are safe to delete
	if (dn_ebr_try_advance_epoch (collector)) {
		minipal_mutex_enter (&collector->pending_lock);

        uint32_t current_epoch = minipal_atomic_load_u32 (&collector->global_epoch);
		// Objects retired at epoch E are safe when global epoch has advanced twice past E
		// With epoch in [0, DN_EBR_NUM_EPOCHS-1], the safe slot is (current + 1) % DN_EBR_NUM_EPOCHS
		// because that's the slot that was current 2 advances ago
		uint32_t safe_slot = (current_epoch + 1) % DN_EBR_NUM_EPOCHS;

		size_t freed = dn_ebr_drain_queue (collector, safe_slot);
        if (freed > 0) {
            minipal_atomic_sub_size (&collector->pending_size, freed);
        }

		minipal_mutex_leave (&collector->pending_lock);
	}
}

// ============================================
// Public API Implementation
// ============================================

dn_ebr_collector_t *
dn_ebr_collector_init (
	dn_ebr_collector_t *collector,
	size_t memory_budget,
	dn_allocator_t *allocator,
	dn_ebr_fatal_callback_t fatal_callback)
{
	if (!fatal_callback) {
		// Can't report this error without a callback, just return NULL
		return NULL;
	}

    DN_ASSERT (collector);

	memset (collector, 0, sizeof (dn_ebr_collector_t));

	collector->memory_budget = memory_budget;
	collector->allocator = allocator;
	collector->fatal_callback = fatal_callback;
	collector->global_epoch = 0;
	collector->pending_size = 0;
	collector->thread_list_head = NULL;
	collector->tls_initialized = false;

	// Initialize mutexes
	if (!minipal_mutex_init (&collector->thread_list_lock)) {
		fatal_callback ("dn_ebr: failed to initialize thread_list_lock");
		return NULL;
	}

	if (!minipal_mutex_init (&collector->pending_lock)) {
		minipal_mutex_destroy (&collector->thread_list_lock);
		fatal_callback ("dn_ebr: failed to initialize pending_lock");
		return NULL;
	}

	// Initialize TLS slot for this collector
	if (!dn_ebr_tls_init (collector)) {
		minipal_mutex_destroy (&collector->pending_lock);
		minipal_mutex_destroy (&collector->thread_list_lock);
		return NULL;
	}

	for (uint32_t i = 0; i < DN_EBR_NUM_EPOCHS; i++) {
		collector->pending_heads[i] = NULL;
	}

	return collector;
}

void
dn_ebr_collector_shutdown (dn_ebr_collector_t *collector)
{
    DN_ASSERT (collector);

	// Drain all pending queues (force delete everything)
	minipal_mutex_enter (&collector->pending_lock);

	for (uint32_t i = 0; i < DN_EBR_NUM_EPOCHS; i++) {
		dn_ebr_drain_queue (collector, i);
	}

	minipal_mutex_leave (&collector->pending_lock);

	// Shutdown TLS
	dn_ebr_tls_shutdown (collector);

	// Destroy mutexes
	minipal_mutex_destroy (&collector->pending_lock);
	minipal_mutex_destroy (&collector->thread_list_lock);

	// Note: Threads should have been unregistered before shutdown
	// Any remaining thread_data will be orphaned but the TLS destructor
	// will no longer be called since we freed the TLS slot
}

void
dn_ebr_register_thread (dn_ebr_collector_t *collector)
{
    DN_ASSERT (collector);

	// This will create thread data if it doesn't exist
    dn_ebr_get_thread_data (collector);
}

void
dn_ebr_unregister_thread (dn_ebr_collector_t *collector)
{
    DN_ASSERT (collector);

	dn_ebr_thread_data_t *thread_data = dn_ebr_tls_get (collector);
	if (!thread_data)
		return;

	// Clear TLS slot first (so destructor won't be called again)
	dn_ebr_tls_set (collector, NULL);

	// Now unregister
	dn_ebr_unregister_thread_internal (collector, thread_data);
}

void
dn_ebr_enter_critical_region (dn_ebr_collector_t *collector)
{
    DN_ASSERT (collector);

    dn_ebr_thread_data_t *thread_data = dn_ebr_get_thread_data (collector);
	if (!thread_data)
		return;

	thread_data->critical_region_depth++;

	// Only update epoch on outermost entry
	if (thread_data->critical_region_depth == 1) {
        thread_data->local_epoch = minipal_atomic_load_u32 (&collector->global_epoch);
	}
}

void
dn_ebr_exit_critical_region (dn_ebr_collector_t *collector)
{
    DN_ASSERT (collector);

	dn_ebr_thread_data_t *thread_data = dn_ebr_tls_get (collector);
	if (!thread_data) {
		collector->fatal_callback ("dn_ebr: exit_critical_region called but thread not registered");
		return;
	}

	if (thread_data->critical_region_depth == 0) {
		collector->fatal_callback ("dn_ebr: exit_critical_region called without matching enter");
		return;
	}

	thread_data->critical_region_depth--;

    // Only mark quiescent on outermost exit
    if (thread_data->critical_region_depth == 0) {
        thread_data->local_epoch = UINT32_MAX;
    }
}

void
dn_ebr_queue_for_deletion (
	dn_ebr_collector_t *collector,
	void *object,
	const dn_ebr_deletion_traits_t *traits)
{
    DN_ASSERT (collector);

	if (!object) {
		collector->fatal_callback ("dn_ebr: queue_for_deletion called with NULL object");
		return;
	}

	if (!traits || !traits->estimate_size || !traits->delete_object) {
		collector->fatal_callback ("dn_ebr: queue_for_deletion called with invalid traits");
		return;
	}

	// Must be in a critical region
	dn_ebr_thread_data_t *thread_data = dn_ebr_tls_get (collector);
	if (!thread_data || thread_data->critical_region_depth == 0) {
		collector->fatal_callback ("dn_ebr: queue_for_deletion called outside critical region");
		return;
	}

	// Allocate pending entry
	dn_ebr_pending_t *pending = (dn_ebr_pending_t *)dn_allocator_alloc (collector->allocator, sizeof (dn_ebr_pending_t));
	if (!pending) {
		collector->fatal_callback ("dn_ebr: failed to allocate pending entry");
		return;
	}

	pending->object = object;
	pending->traits = traits;
	pending->estimated_size = traits->estimate_size (object);
	pending->next = NULL;

	// Add to the appropriate epoch queue
	minipal_mutex_enter (&collector->pending_lock);

    uint32_t current_epoch = minipal_atomic_load_u32 (&collector->global_epoch);
	uint32_t slot = current_epoch;  // epoch is already in [0, DN_EBR_NUM_EPOCHS-1]

	// Push-front into list; deletion order not required to be FIFO
	pending->next = collector->pending_heads[slot];
	collector->pending_heads[slot] = pending;

    minipal_atomic_add_size (&collector->pending_size, pending->estimated_size);

	minipal_mutex_leave (&collector->pending_lock);

	// Check if we need to try reclamation
    if (minipal_atomic_load_size (&collector->pending_size) > collector->memory_budget) {
		dn_ebr_try_reclaim (collector);
	}
}

bool
dn_ebr_in_critical_region (dn_ebr_collector_t *collector)
{
    DN_ASSERT (collector);

	dn_ebr_thread_data_t *thread_data = dn_ebr_tls_get (collector);
	return thread_data && thread_data->critical_region_depth > 0;
}
