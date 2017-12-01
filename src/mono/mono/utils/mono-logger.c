/**
 * \file
 */

#include <string.h>
#include <stdlib.h>
#include <stdio.h>
#include <glib.h>

#include "mono-compiler.h"
#include "mono-logger-internals.h"

typedef struct {
	GLogLevelFlags	level;
	MonoTraceMask	mask;
} MonoLogLevelEntry;

GLogLevelFlags mono_internal_current_level	= INT_MAX;
MonoTraceMask  mono_internal_current_mask	= ~((MonoTraceMask)0);
gboolean mono_trace_log_header			= FALSE;

static GQueue		*level_stack		= NULL;
static const char	*mono_log_domain	= "Mono";
static MonoPrintCallback print_callback, printerr_callback;

static MonoLogCallParm logCallback = {
	.opener = NULL,
	.writer = NULL,
	.closer = NULL,
	.header = FALSE
};

typedef struct {
   MonoLogCallback legacy_callback;
   gpointer user_data;
} UserSuppliedLoggerUserData;

/**
 * mono_trace_init:
 *
 * Initializes the mono tracer.
 */
void 
mono_trace_init (void)
{
	if(level_stack == NULL) {
		mono_internal_current_level = G_LOG_LEVEL_ERROR;
		level_stack = g_queue_new();

		char *mask = g_getenv ("MONO_LOG_MASK");
		char *level = g_getenv ("MONO_LOG_LEVEL");
		char *header = g_getenv ("MONO_LOG_HEADER");
		char *dest = g_getenv ("MONO_LOG_DEST");

		mono_trace_set_mask_string(mask);
		mono_trace_set_level_string(level);
		mono_trace_set_logheader_string(header);
		mono_trace_set_logdest_string(dest);

		g_free (mask);
		g_free (level);
		g_free (header);
		g_free (dest);
	}
}

/**
 * mono_trace_cleanup:
 *
 * Releases the mono tracer.
 */
void 
mono_trace_cleanup (void)
{
	if(level_stack != NULL) {
		while(!g_queue_is_empty (level_stack)) {
			g_free (g_queue_pop_head (level_stack));
		}

		logCallback.closer();
		g_queue_free (level_stack);
		level_stack = NULL;
	}
}

/**
 * mono_tracev_inner:
 * \param level Verbose level of the specified message
 * \param mask Type of the specified message
 * Traces a new message, depending on the current logging level
 * and trace mask.
 */
void 
mono_tracev_inner (GLogLevelFlags level, MonoTraceMask mask, const char *format, va_list args)
{
	char *log_message;
	if (level_stack == NULL) {
		mono_trace_init ();
		if(level > mono_internal_current_level || !(mask & mono_internal_current_mask))
			return;
	}

	g_assert (logCallback.opener); // mono_trace_init should have provided us with one!

	if (g_vasprintf (&log_message, format, args) < 0)
		return;
	logCallback.writer (mono_log_domain, level, logCallback.header, log_message);
	g_free (log_message);
}

/**
 * mono_trace_set_level:
 * \param level Verbose level to set
 * Sets the current logging level. Every subsequent call to
 * \c mono_trace will check the visibility of a message against this
 * value.
 */
void 
mono_trace_set_level (GLogLevelFlags level)
{
	if(level_stack == NULL)
		mono_trace_init();

	mono_internal_current_level = level;
}

/**
 * mono_trace_set_mask:
 * \param mask Mask of visible message types.
 * Sets the current logging level. Every subsequent call to
 * \c mono_trace will check the visibility of a message against this
 * value.
 */
void 
mono_trace_set_mask (MonoTraceMask mask)
{
	if(level_stack == NULL)
		mono_trace_init();

	mono_internal_current_mask = mask;
}

/**
 * mono_trace_set_logdest:
 * \param dest Destination for logging
 * Sets the current logging destination. This can be a file or, if supported,
 * syslog.
 */
void 
mono_trace_set_logdest_string (const char *dest)
{
	MonoLogCallParm logger;

	if(level_stack == NULL)
		mono_trace_init();

#if HOST_ANDROID
	logger.opener = mono_log_open_logcat;
	logger.writer = mono_log_write_logcat;
	logger.closer = mono_log_close_logcat;
	logger.dest   = (char*) dest;
#elif defined (HOST_IOS)
	logger.opener = mono_log_open_asl;
	logger.writer = mono_log_write_asl;
	logger.closer = mono_log_close_asl;
	logger.dest   = (char*) dest;
#else
	if ((dest == NULL) || (strcmp("syslog", dest) != 0)) {
		logger.opener = mono_log_open_logfile;
		logger.writer = mono_log_write_logfile;
		logger.closer = mono_log_close_logfile;
		logger.dest   = (char *) dest;
	} else {
		logger.opener = mono_log_open_syslog;
		logger.writer = mono_log_write_syslog;
		logger.closer = mono_log_close_syslog;
		logger.dest   = (char *) dest;
	}
#endif
	mono_trace_set_log_handler_internal(&logger, NULL);
}

/**
 * mono_trace_set_logheader:
 * \param head Whether we want pid/date/time header on log messages
 * Sets the current logging header option.
 */
void 
mono_trace_set_logheader_string(const char *head)
{
	if (head == NULL) {
		mono_trace_log_header = FALSE;
	} else {
		mono_trace_log_header = TRUE;
	}
}

/**
 * mono_trace_push:
 * \param level Verbose level to set
 * \param mask Mask of visible message types.
 * Saves the current values of level and mask then calls \c mono_trace_set
 * with the specified new values.
 */
void 
mono_trace_push (GLogLevelFlags level, MonoTraceMask mask)
{
	if(level_stack == NULL)
		g_error("%s: cannot use mono_trace_push without calling mono_trace_init first.", __func__);
	else {
		MonoLogLevelEntry *entry = (MonoLogLevelEntry *) g_malloc(sizeof(MonoLogLevelEntry));
		entry->level	= mono_internal_current_level;
		entry->mask		= mono_internal_current_mask;

		g_queue_push_head (level_stack, (gpointer)entry);

		/* Set the new level and mask
		 */
		mono_internal_current_level = level;
		mono_internal_current_mask  = mask;
	}
}

/**
 * mono_trace_pop:
 *
 * Restores level and mask values saved from a previous call to mono_trace_push.
 */
void 
mono_trace_pop (void)
{
	if(level_stack == NULL)
		g_error("%s: cannot use mono_trace_pop without calling mono_trace_init first.", __func__);
	else {
		if(!g_queue_is_empty (level_stack)) {
			MonoLogLevelEntry *entry = (MonoLogLevelEntry*)g_queue_pop_head (level_stack);

			/*	Restore previous level and mask
			 */
			mono_internal_current_level = entry->level;
			mono_internal_current_mask  = entry->mask;

			g_free (entry);
		}
	}
}


void 
mono_trace_set_level_string (const char *value)
{
	int i = 0;
	const char *valid_vals[] = {"error", "critical", "warning", "message", "info", "debug", NULL};
	const GLogLevelFlags valid_ids[] = {G_LOG_LEVEL_ERROR, G_LOG_LEVEL_CRITICAL, G_LOG_LEVEL_WARNING,
										G_LOG_LEVEL_MESSAGE, G_LOG_LEVEL_INFO, G_LOG_LEVEL_DEBUG };

	if(!value)
		return;

	while(valid_vals[i]) {
		if(!strcmp(valid_vals[i], value)){
			mono_trace_set_level(valid_ids[i]);
			return;
		}
		i++;
	}

	if(*value)
		g_print("Unknown trace loglevel: %s\n", value);
}

void 
mono_trace_set_mask_string (const char *value)
{
	int i;
	const char *tok;
	guint32 flags = 0;

	static const struct { const char * const flag; const MonoTraceMask mask; } flag_mask_map[] = {
		{ "asm", MONO_TRACE_ASSEMBLY },
		{ "type", MONO_TRACE_TYPE },
		{ "dll", MONO_TRACE_DLLIMPORT },
		{ "gc", MONO_TRACE_GC },
		{ "cfg", MONO_TRACE_CONFIG },
		{ "aot", MONO_TRACE_AOT },
		{ "security", MONO_TRACE_SECURITY },
		{ "threadpool", MONO_TRACE_THREADPOOL },
		{ "io-threadpool", MONO_TRACE_IO_SELECTOR },
		{ "io-selector", MONO_TRACE_IO_SELECTOR },
		{ "io-layer-process", MONO_TRACE_IO_LAYER_PROCESS },
		{ "io-layer-socket", MONO_TRACE_IO_LAYER_SOCKET },
		{ "io-layer-file", MONO_TRACE_IO_LAYER_FILE },
		{ "io-layer-console", MONO_TRACE_IO_LAYER_FILE },
		{ "io-layer-pipe", MONO_TRACE_IO_LAYER_FILE },
		{ "io-layer-event", MONO_TRACE_IO_LAYER_EVENT },
		{ "io-layer-semaphore", MONO_TRACE_IO_LAYER_SEMAPHORE },
		{ "io-layer-mutex", MONO_TRACE_IO_LAYER_MUTEX },
		{ "io-layer-handle", MONO_TRACE_IO_LAYER_HANDLE },
		{ "io-layer", MONO_TRACE_IO_LAYER_PROCESS
		               | MONO_TRACE_IO_LAYER_SOCKET
		               | MONO_TRACE_IO_LAYER_FILE
		               | MONO_TRACE_IO_LAYER_EVENT
		               | MONO_TRACE_IO_LAYER_SEMAPHORE
		               | MONO_TRACE_IO_LAYER_MUTEX
		               | MONO_TRACE_IO_LAYER_HANDLE },
		{ "w32handle", MONO_TRACE_IO_LAYER_HANDLE },
		{ "all", ~((MonoTraceMask)0) },
		{ NULL, 0 },
	};

	if(!value)
		return;

	tok = value;

	while (*tok) {
		if (*tok == ',') {
			tok++;
			continue;
		}
		for (i = 0; flag_mask_map[i].flag; i++) {
			size_t len = strlen (flag_mask_map[i].flag);
			if (strncmp (tok, flag_mask_map[i].flag, len) == 0 && (tok[len] == 0 || tok[len] == ',')) {
				flags |= flag_mask_map[i].mask;
				tok += len;
				break;
			}
		}
		if (!flag_mask_map[i].flag) {
			g_print("Unknown trace flag: %s\n", tok);
			break;
		}
	}

	mono_trace_set_mask ((MonoTraceMask) flags);
}

/*
 * mono_trace_is_traced:
 *
 *   Returns whenever a message with @level and @mask will be printed or not.
 */
gboolean
mono_trace_is_traced (GLogLevelFlags level, MonoTraceMask mask)
{
	return (level <= mono_internal_current_level && (mask & mono_internal_current_mask));
}

/**
 * log_level_get_name
 * @log_level severity level
 *
 * Convert log level into a string for legacy log handlers
 */
static const char *
log_level_get_name (GLogLevelFlags log_level)
{
        switch (log_level & G_LOG_LEVEL_MASK) {
        case G_LOG_LEVEL_ERROR: return "error";
        case G_LOG_LEVEL_CRITICAL: return "critical";
        case G_LOG_LEVEL_WARNING: return "warning";
        case G_LOG_LEVEL_MESSAGE: return "message";
        case G_LOG_LEVEL_INFO: return "info";
        case G_LOG_LEVEL_DEBUG: return "debug";
        default: return "unknown";
        }
}

/**
 * callback_adapter
 * 
 *  @log_domain Message prefix
 *  @log_level Severity
 *  @message Message to be written
 *  @fatal Fatal flag - write then abort
 *  @user_data Argument passed to @callback
 *
 * This adapts the old callback writer exposed by MonoCallback to the newer method of
 * logging. We ignore the header request as legacy handlers never had headers.
 */
static void
callback_adapter (const char *domain, GLogLevelFlags level, mono_bool fatal, const char *message)
{
	UserSuppliedLoggerUserData *ll =logCallback.user_data;

	ll->legacy_callback (domain, log_level_get_name(level), message, fatal, ll->user_data);
}

static void
eglib_log_adapter (const gchar *log_domain, GLogLevelFlags log_level, const gchar *message, gpointer user_data)
{
	UserSuppliedLoggerUserData *ll = logCallback.user_data;

	ll->legacy_callback (log_domain, log_level_get_name (log_level), message, log_level & G_LOG_LEVEL_ERROR, ll->user_data);
}

/**
 * legacy_opener
 *
 * Dummy routine for older style loggers
 */
static void
legacy_opener(const char *path, void *user_data)
{
  /* nothing to do */
}

/**
 * legacy_closer
 *
 * Cleanup routine for older style loggers
 */
static void
legacy_closer(void)
{
	if (logCallback.user_data != NULL) {
		g_free (logCallback.user_data); /* This is a UserSuppliedLoggerUserData struct */
		logCallback.opener = NULL;	
		logCallback.writer = NULL;
		logCallback.closer = NULL;
		logCallback.user_data = NULL;
		logCallback.header = FALSE;
	}
}

/**
 *   mono_trace_set_log_handler:
 *  
 *  @callback The callback that will replace the default logging handler
 *  @user_data Argument passed to @callback
 * 
 * The log handler replaces the default runtime logger. All logging requests with be routed to it.
 * If the fatal argument in the callback is true, the callback must abort the current process. The runtime expects that
 * execution will not resume after a fatal error.
 */
void
mono_trace_set_log_handler (MonoLogCallback callback, void *user_data)
{
	g_assert (callback);

	if (logCallback.closer != NULL)
		logCallback.closer();
	UserSuppliedLoggerUserData *ll = g_malloc (sizeof (UserSuppliedLoggerUserData));
	ll->legacy_callback = callback;
	ll->user_data = user_data;
	logCallback.opener = legacy_opener;
	logCallback.writer = callback_adapter;
	logCallback.closer = legacy_closer;
	logCallback.user_data = ll;
	logCallback.dest = NULL;

	g_log_set_default_handler (eglib_log_adapter, user_data);
}

static void
structured_log_adapter (const gchar *log_domain, GLogLevelFlags log_level, const gchar *message, gpointer user_data)
{
	logCallback.writer (log_domain, log_level, logCallback.header, message);
}

/**
 * mono_trace_set_log_handler_internal:
 * \param callback The callback that will replace the default logging handler
 * \param user_data Argument passed to \p callback
 * The log handler replaces the default runtime logger. All logging requests with be routed to it.
 * If the fatal argument in the callback is true, the callback must abort the current process. The runtime expects that
 * execution will not resume after a fatal error.
 */
void
mono_trace_set_log_handler_internal (MonoLogCallParm *callback, void *user_data)
{
	g_assert (callback);
	if (logCallback.closer != NULL)
		logCallback.closer();
	logCallback.opener = callback->opener;
	logCallback.writer = callback->writer;
	logCallback.closer = callback->closer;
	logCallback.header = mono_trace_log_header;
	logCallback.dest   = callback->dest;
	logCallback.opener (logCallback.dest, user_data);

	g_log_set_default_handler (structured_log_adapter, user_data);
}

static void
print_handler (const char *string)
{
	print_callback (string, TRUE);
}

static void
printerr_handler (const char *string)
{
	printerr_callback (string, FALSE);
}

/**
 * mono_trace_set_print_handler:
 * \param callback The callback that will replace the default runtime behavior for stdout output.
 * The print handler replaces the default runtime stdout output handler. This is used by free form output done by the runtime.
 */
void
mono_trace_set_print_handler (MonoPrintCallback callback)
{
	g_assert (callback);
	print_callback = callback;
	g_set_print_handler (print_handler);
}

/**
 * mono_trace_set_printerr_handler:
 * \param callback The callback that will replace the default runtime behavior for stderr output.
 * The print handler replaces the default runtime stderr output handler. This is used by free form output done by the runtime.
 */
void
mono_trace_set_printerr_handler (MonoPrintCallback callback)
{
	g_assert (callback);
	printerr_callback = callback;
	g_set_printerr_handler (printerr_handler);
}
