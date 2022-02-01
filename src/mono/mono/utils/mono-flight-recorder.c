/**
 * \file
 *     A lightweight log storage medium with limited history
 *
 * Author:
 *   Alexander Kyte (alkyte@microsoft.com)
 *
 * (C) 2018 Microsoft, Inc.
 *
 */

#include <glib.h>
#include <mono/utils/mono-flight-recorder.h>

// Need to make lockless because mutex is really slow
// at each log statement.
//
// Better/concurrent design:
// - Make thread for flight recorder
// - Send messages to flight recorder using utils/lock-free-queue.h
// - Flight recorder doesn't share memory, all owned by thread



#define MONO_FLIGHT_RECORDER_SENTINEL -1

// Mutex has to be held when called
void
mono_flight_recorder_iter_init (MonoFlightRecorder *recorder, MonoFlightRecorderIter *iter) 
{
	// Make sure we are initialized
	g_assert (recorder->max_count > 0);

	iter->recorder = recorder;
	if (recorder->cursor == MONO_FLIGHT_RECORDER_SENTINEL) {
		iter->lowest_index = MONO_FLIGHT_RECORDER_SENTINEL; 
		iter->highest_index = MONO_FLIGHT_RECORDER_SENTINEL;
	} else if (recorder->cursor >= recorder->max_count) {
		// Ring buffer has wrapped around
		// So the item *after* the highest index is the lowest index
		iter->highest_index = (recorder->cursor + 1) % recorder->max_count;
		iter->lowest_index = (iter->highest_index + 1) % recorder->max_count;
	} else {
		iter->lowest_index = 0;
		iter->highest_index = recorder->cursor + 1;
	}
}

void
mono_flight_recorder_iter_destroy (MonoFlightRecorderIter *iter)
{
	// Does nothing now, but might want to in future with iterator
	return;
}

// Mutex has to be held when called
gboolean
mono_flight_recorder_iter_next (MonoFlightRecorderIter *iter, MonoFlightRecorderHeader *header, gpointer *payload)
{
	gboolean empty_log = (iter->lowest_index == MONO_FLIGHT_RECORDER_SENTINEL) && (iter->lowest_index == MONO_FLIGHT_RECORDER_SENTINEL);
	if (empty_log || (iter->lowest_index == iter->highest_index))
		return FALSE;

	g_assert (iter->lowest_index >= 0);
	g_assert (iter->lowest_index < iter->recorder->max_count);

	// Reference to the variably-sized logging payload
	memcpy (payload, (gpointer *) &iter->recorder->items [iter->lowest_index]->payload, iter->recorder->payload_size);
	memcpy (header, (gpointer *) &iter->recorder->items [iter->lowest_index]->header, sizeof (MonoFlightRecorderHeader));
	iter->lowest_index++;

	if (iter->lowest_index >= iter->recorder->max_count)
		iter->lowest_index = iter->lowest_index % iter->recorder->max_count;

	return TRUE;
}

MonoCoopMutex *
mono_flight_recorder_mutex (MonoFlightRecorder *recorder)
{
	return &recorder->mutex;
}

static size_t
mono_flight_recorder_item_size (size_t payload_size)
{
	return offsetof(MonoFlightRecorderItem, payload) + payload_size;
}

MonoFlightRecorder *
mono_flight_recorder_init (size_t max_count, size_t payload_size)
{
	size_t item_size = mono_flight_recorder_item_size (payload_size);
	size_t size_of_items = item_size * max_count;
	size_t size_of_item_ptrs = sizeof (gpointer) * max_count;
	size_t size_of_recorder_prefix = offsetof(MonoFlightRecorder, items);
	MonoFlightRecorder *recorder = (MonoFlightRecorder*)g_malloc0 (size_of_recorder_prefix + size_of_item_ptrs + size_of_items);
	intptr_t end_of_memory = ((intptr_t) recorder) + (size_of_recorder_prefix + size_of_item_ptrs + size_of_items);

	recorder->max_count = max_count;
	recorder->cursor = MONO_FLIGHT_RECORDER_SENTINEL;
	recorder->payload_size = payload_size;

	// First byte after end of pointer array is the flexible-shape memory
	intptr_t memory = (intptr_t) &recorder->items[recorder->max_count];
	for (int i=0; i < recorder->max_count; i++) {
		recorder->items [i] = (MonoFlightRecorderItem *) (memory + (item_size * i));
		g_assert ((intptr_t) recorder->items [i] < end_of_memory);
	}

	mono_coop_mutex_init (&recorder->mutex);

	return recorder;
}

void
mono_flight_recorder_free (MonoFlightRecorder *recorder)
{
	// FIXME: use hazard pointers here
	// Currently we push burden of making sure not used after freed to caller
	g_free (recorder);
}

void
mono_flight_recorder_append (MonoFlightRecorder *recorder, gpointer payload)
{
	MonoFlightRecorderItem *item;

	mono_coop_mutex_lock (&recorder->mutex);

	if (G_UNLIKELY(recorder->cursor == MONO_FLIGHT_RECORDER_SENTINEL)) {
		item = recorder->items [0];
		item->header.counter = 0;
		recorder->cursor = 0;
	} else {
		// cursor points to the field that was just filled out
		MonoFlightRecorderItem *old_item = recorder->items [recorder->cursor % recorder->max_count];

		item = recorder->items [(recorder->cursor + 1) % recorder->max_count];
		item->header.counter = old_item->header.counter + 1;

		recorder->cursor++;
	}

	memcpy ((gpointer *) &item->payload, payload, recorder->payload_size);

	// Memcpy has to happen in lock in case of contention,
	// can't have two threads writing to same memory block
	// if the index loops around

	mono_coop_mutex_unlock (&recorder->mutex);
}

