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

#include <config.h>
#include "mono-logger-internals.h"
#include <mono/utils/mono-flight-recorder.h>
#include <mono/utils/mono-utility-thread.h>

#define MAX_RECORDER_LOG_LEN 500
#define MAX_RECORDER_MSG_LEN 500

typedef struct {
	gchar message [MAX_RECORDER_MSG_LEN];
} LogMessage;

typedef struct {
	LogMessage *messages;
	size_t num_messages;
	size_t max_num_messages;
} LogQueueDumpRequest;

typedef enum {
	MONO_FLIGHT_RECORDER_INVALID = 0,
	MONO_FLIGHT_RECORDER_APPEND = 1,
	MONO_FLIGHT_RECORDER_DUMP = 2,
} LogQueueCommand;

typedef struct {
	LogQueueCommand command;
	union {
		LogMessage message;
		LogQueueDumpRequest *dump;
	};
} LogQueueEntry;

static void
mono_log_dump_recorder_internal (MonoFlightRecorder *recorder, LogQueueDumpRequest *req);

static void
init (gpointer *out)
{
	MonoFlightRecorder *recorder = mono_flight_recorder_init (MAX_RECORDER_LOG_LEN, sizeof (LogMessage));
	*out = (gpointer) recorder;
}

static void
handle_command (gpointer state, gpointer payload, gboolean at_shutdown)
{
	MonoFlightRecorder *recorder = (MonoFlightRecorder *) state;
	LogQueueEntry *entry = (LogQueueEntry *) payload;

	switch (entry->command) {
	case MONO_FLIGHT_RECORDER_APPEND:
		mono_flight_recorder_append (recorder, &entry->message);

#if 0
		// Dump all messages on each append. This is an aggressive, slow
		// debugging method. 

		LogMessage messages [MAX_RECORDER_LOG_LEN];
		LogQueueDumpRequest dump;
		dump.messages = (LogMessage *) messages;
		dump.num_messages = 0;
		dump.max_num_messages = MAX_RECORDER_LOG_LEN;
		mono_log_dump_recorder_internal (recorder, &dump);
		fprintf (stderr, "%" G_GSIZE_FORMAT "u messages\n", dump.num_messages);

		for (int i=0; i < dump.num_messages; i++)
			fprintf (stderr, "\t(%d): %s\n", i, dump.messages [i].message);
#endif

		break;
	case MONO_FLIGHT_RECORDER_DUMP:
		fprintf (stderr, "Log received dump\n");
		mono_log_dump_recorder_internal (recorder, entry->dump);
		break;
	default:
		g_assert_not_reached ();
	}
}

static void
cleanup (gpointer state)
{
	mono_flight_recorder_free ((MonoFlightRecorder *) state);
}

static MonoUtilityThread *logger_thread;


/**
 * mono_log_open_recorder:
 * \param path Unused
 * \param userData Unused
 * Open access to recorder 
 */   
void
mono_log_open_recorder (const char *path, void *userData)
{
	MonoUtilityThreadCallbacks callbacks;
	callbacks.early_init = NULL;
	callbacks.init = init;
	callbacks.command = handle_command;
	callbacks.cleanup = cleanup;
	logger_thread = mono_utility_thread_launch (sizeof (LogMessage), &callbacks, MONO_MEM_ACCOUNT_OTHER);
}

/**
 * mono_log_write_recorder:
 * \param domain Identifier string
 * \param level Logging level flags
 * \param format \c printf format string
 * \param vargs Variable argument list
 * Write data to recorder.
 */
void
mono_log_write_recorder (const char *log_domain, GLogLevelFlags level, mono_bool hdr, const char *message)
{
	int small_id = mono_thread_info_get_small_id ();
	if (small_id < 0) {
#if MONO_PRINT_DROPPED_MESSAGES
		fprintf (stderr, "Dropping message because thread not attached yet:\n\t%s\n", message);
#endif
		return;
	} else if (level & G_LOG_LEVEL_ERROR) {
		fprintf (stderr, "\nFatal Error Occured: %s\n\nHistory:\n", message);
		mono_log_dump_recorder ();
		abort();
	} else if (!logger_thread->run_thread) {
#if MONO_PRINT_DROPPED_MESSAGES
		fprintf (stderr, "Dropping message because thread killed:\n\t%s\n", message);
#endif
		return;
	}

	LogQueueEntry entry;
	entry.command = MONO_FLIGHT_RECORDER_APPEND;
	g_snprintf ((gchar *) &entry.message.message, MAX_RECORDER_MSG_LEN, "%s", message);
	mono_utility_thread_send (logger_thread, &entry);
}

/**
 * mono_log_close_recorder
 *
 * 	Close access to recorder 
 */
void
mono_log_close_recorder (void)
{
	int small_id = mono_thread_info_get_small_id ();
	if (small_id < 0) {
#if MONO_PRINT_DROPPED_MESSAGES
		fprintf (stderr, "Dropping attempt to close recorder, thread not attached yet\n");
#endif
		return;
	} else if (!logger_thread->run_thread) {
		return;
	}

	fprintf (stderr, "\nFlight recorder closed (pre dump):\n");

	mono_log_dump_recorder ();

	fprintf (stderr, "\nFlight recorder closed (post dump):\n");

	mono_utility_thread_stop (logger_thread);
}

void
mono_log_dump_recorder (void)
{
	int small_id = mono_thread_info_get_small_id ();
	if (small_id < 0) {
#if MONO_PRINT_DROPPED_MESSAGES
		fprintf (stderr, "Dropping attempt to dump recorder, thread not attached yet\n");
#endif
		return;
	}

	LogMessage messages [MAX_RECORDER_LOG_LEN];
	LogQueueDumpRequest dump;
	dump.messages = (LogMessage *) messages;
	dump.num_messages = 0;
	dump.max_num_messages = MAX_RECORDER_LOG_LEN;

	LogQueueEntry entry;
	entry.command = MONO_FLIGHT_RECORDER_DUMP;
	entry.dump = &dump;

	gboolean success = mono_utility_thread_send_sync (logger_thread, &entry);

	if (success) {
		fprintf (stderr, "Recent Logs Inserted\n");
		fprintf (stderr, "%" G_GSIZE_FORMAT "u messages\n", dump.num_messages);

		for (int i=0; i < dump.num_messages; i++)
			fprintf (stderr, "\t(%d): %s\n", i, dump.messages [i].message);
	}
}

static void
mono_log_dump_recorder_internal (MonoFlightRecorder *recorder, LogQueueDumpRequest *req)
{
	MonoFlightRecorderIter diter;
	MonoFlightRecorderHeader header;
	int index = 0;

	mono_flight_recorder_iter_init (recorder, &diter);
	while (mono_flight_recorder_iter_next (&diter, &header, (gpointer *) &req->messages [index++]));
	mono_flight_recorder_iter_destroy (&diter);

	// FIXME: do something with header/counter?

	req->num_messages = index - 1;
}


