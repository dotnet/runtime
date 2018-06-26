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

#define MONO_FLIGHT_RECORDER_SENTINEL -1

// Mutex has to be held when called
void
mono_flight_recorder_iter_init (MonoFlightRecorder *recorder, MonoFlightRecorderIter *iter) 
{
	// Make sure we are initialized
	g_assert (recorder->max_size > 0);

	if (recorder->cursor == MONO_FLIGHT_RECORDER_SENTINEL) {
		iter->lowest_index = MONO_FLIGHT_RECORDER_SENTINEL; 
		iter->highest_index = MONO_FLIGHT_RECORDER_SENTINEL;
	} else if (recorder->cursor >= recorder->max_size) {
		// Ring buffer has wrapped around
		// So the item *after* the highest index is the lowest index
		iter->highest_index = (recorder->cursor + 1) % recorder->max_size;
		iter->lowest_index = (iter->highest_index + 1) % recorder->max_size;
	} else {
		iter->lowest_index = 0;
		iter->highest_index = recorder->cursor;
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
mono_flight_recorder_iter_next (MonoFlightRecorder *recorder, MonoFlightRecorderIter *iter, gpointer **payload)
{
	if ((iter->lowest_index == MONO_FLIGHT_RECORDER_SENTINEL) || (iter->lowest_index == iter->highest_index ||
		|| (iter->lowest_index == MONO_FLIGHT_RECORDER_SENTINEL))
		return FALSE;

	g_assert (iter->lowest_index >= 0);
	g_assert (iter->lowest_index < recorder->max_size);

	// Reference to the variably-sized logging payload
	*item = (gpointer *) &recorder->items [iter->lowest_index]->payload;
	iter->lowest_index++;

	if (iter->lowest_index >= recorder->max_size)
		iter->lowest_index = iter->lowest_index % recorder->max_size;

	return TRUE;
}

MonoCoopMutex *
mono_flight_recorder_mutex (MonoFlightRecorder *recorder)
{
	return &recorder->mutex;
}

MonoFlightRecorder *
mono_flight_recorder_init (size_t max_size, size_t payload_size)
{
	MonoFlightRecorder *recorder = g_malloc0 (sizeof (MonoFlightRecorder));
	recorder->max_count = max_size;
	recorder->cursor = MONO_FLIGHT_RECORDER_SENTINEL;
	recorder->payload_size = payload_size;

	size_t item_size = sizeof (FlightRecorderItem) + payload_size;
	recorder->items = g_malloc0 (item_size * recorder->max_size);

	mono_coop_mutex_init (&recorder->mutex);
}

void
mono_flight_recorder_free (MonoFlightRecorder *recorder)
{
	// FIXME: use hazard pointers here
	// Currently we push burden of making sure not used after freed to caller
	g_free (recorder->items);
	g_free (recorder);
}

void
mono_flight_recorder_append (MonoFlightRecorder *recorder, gpointer payload)
{
	MonoDebugLogItem *item, *old_item;

	mono_coop_mutex_lock (&recorder->mutex);

	if (recorder->cursor == MONO_FLIGHT_RECORDER_SENTINEL) {
		item = &recorder->items [0];
		item->counter = 0;
	} else {
		// We have a ring buffer
		old_item = &recorder->items [recorder->cursor % recorder->max_size];
		item = &recorder->items [(recorder->cursor + 1) % recorder->max_size];
		item->counter = old_item->counter + 1;
	}

	recorder->cursor++;

	memcpy (item->payload, payload, item->payload_size);

	// Memcpy has to happen in lock in case of contention,
	// can't have two threads writing to same memory block
	// if the index loops around

	// FIXME: make memory-freeing callback + hazard pointers

	mono_coop_mutex_unlock (&recorder->mutex);
}

