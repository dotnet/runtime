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
#ifndef __MONO_FLIGHT_RECORDER__
#define __MONO_FLIGHT_RECORDER__

#include <glib.h>
#include <mono/utils/mono-coop-mutex.h>

typedef struct {
	long counter; // The number of messages allocated thus far, acts like a global, monotonic clock
} MonoFlightRecorderHeader;

typedef struct {
	MonoFlightRecorderHeader header;
	gpointer payload [MONO_ZERO_LEN_ARRAY]; // We have a variably-sized payload
} MonoFlightRecorderItem;

typedef struct {
	intptr_t cursor; // Signed, for sentinel value of -1
	size_t max_count; // Maximum number of items in logger
	size_t payload_size; // Size of data reserved for logging message
	MonoCoopMutex mutex; // Not owned exclusively by us, used by api consumers too
	MonoFlightRecorderItem *items [MONO_ZERO_LEN_ARRAY]; // The data of the history
} MonoFlightRecorder;

MONO_COMPONENT_API MonoCoopMutex *
mono_flight_recorder_mutex (MonoFlightRecorder *recorder);

MONO_COMPONENT_API MonoFlightRecorder *
mono_flight_recorder_init (size_t max_size, size_t payload_size);

MONO_COMPONENT_API void
mono_flight_recorder_free (MonoFlightRecorder *recorder);

MONO_COMPONENT_API void
mono_flight_recorder_append (MonoFlightRecorder *recorder, gpointer payload);

// Used to traverse the ring buffer in order of oldest to newest message

typedef struct {
	intptr_t lowest_index;
	intptr_t highest_index;
	MonoFlightRecorder *recorder;
} MonoFlightRecorderIter;

// Mutex has to be held when called
MONO_COMPONENT_API void
mono_flight_recorder_iter_init (MonoFlightRecorder *recorder, MonoFlightRecorderIter *iter);

// Mutex has to be held when called
MONO_COMPONENT_API void
mono_flight_recorder_iter_destroy (MonoFlightRecorderIter *iter);

// Mutex has to be held when called
MONO_COMPONENT_API gboolean
mono_flight_recorder_iter_next (MonoFlightRecorderIter *iter, MonoFlightRecorderHeader *header, gpointer *payload);

#endif
