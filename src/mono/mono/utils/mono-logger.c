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

GLogLevelFlags mono_internal_current_level		= INT_MAX;
MonoTraceMask  mono_internal_current_mask		= MONO_TRACE_ALL;

static GQueue		*level_stack		= NULL;
static const char	*mono_log_domain	= "Mono";
static MonoPrintCallback print_callback, printerr_callback;

static MonoLogCallback logCallback = {
	.opener = NULL,
	.writer = NULL,
	.closer = NULL
};

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

		mono_trace_set_mask_string(g_getenv("MONO_LOG_MASK"));
		mono_trace_set_level_string(g_getenv("MONO_LOG_LEVEL"));
		mono_trace_set_logdest_string(g_getenv("MONO_LOG_DEST"));
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

		g_queue_free (level_stack);
		level_stack = NULL;
	}
}

/**
 * mono_tracev:
 *
 *	@level: Verbose level of the specified message
 *	@mask: Type of the specified message
 *
 * Traces a new message, depending on the current logging level
 * and trace mask.
 */
void 
mono_tracev_inner (GLogLevelFlags level, MonoTraceMask mask, const char *format, va_list args)
{
	if (level_stack == NULL) {
		mono_trace_init ();
		if(level > mono_internal_current_level || !(mask & mono_internal_current_mask))
			return;
	}

	if (logCallback.opener == NULL) {
		logCallback.opener = mono_log_open_logfile;
		logCallback.writer = mono_log_write_logfile;
		logCallback.closer = mono_log_close_logfile;
		logCallback.opener(NULL, NULL);
	}
	logCallback.writer(mono_log_domain, level, format, args);
}

/**
 * mono_trace_set_level:
 *
 *	@level: Verbose level to set
 *
 * Sets the current logging level. Every subsequent call to
 * mono_trace will check the visibility of a message against this
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
 *
 *	@mask: Mask of visible message types.
 *
 * Sets the current logging level. Every subsequent call to
 * mono_trace will check the visibility of a message against this
 * value.
 */
void 
mono_trace_set_mask (MonoTraceMask mask)
{
	if(level_stack == NULL)
		mono_trace_init();

	mono_internal_current_mask	= mask;
}

/**
 * mono_trace_set_logdest:
 *
 *	@dest: Destination for logging
 *
 * Sets the current logging destination. This can be a file or, if supported,
 * syslog.
 */
void 
mono_trace_set_logdest_string (const char *dest)
{
	MonoLogCallback logger;

	if(level_stack == NULL)
		mono_trace_init();

	if ((dest == NULL) || (strcmp("syslog", dest) != 0)) {
		logger.opener = mono_log_open_logfile;
		logger.writer = mono_log_write_logfile;
		logger.closer = mono_log_close_logfile;
		mono_trace_set_log_handler(&logger, dest, NULL);
	} else {
		logger.opener = mono_log_open_syslog;
		logger.writer = mono_log_write_syslog;
		logger.closer = mono_log_close_syslog;
		mono_trace_set_log_handler(&logger, mono_log_domain, NULL);
	}
}

/**
 * mono_trace_push:
 *
 *	@level: Verbose level to set
 *	@mask: Mask of visible message types.
 *
 * Saves the current values of level and mask then calls mono_trace_set
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

	const char *valid_flags[] = {"asm", "type", "dll", "gc", "cfg", "aot", "security", "threadpool", "io-threadpool", "io-layer", "all", NULL};
	const MonoTraceMask	valid_masks[] = {MONO_TRACE_ASSEMBLY, MONO_TRACE_TYPE, MONO_TRACE_DLLIMPORT,
						 MONO_TRACE_GC, MONO_TRACE_CONFIG, MONO_TRACE_AOT, MONO_TRACE_SECURITY,
						 MONO_TRACE_THREADPOOL, MONO_TRACE_IO_THREADPOOL, MONO_TRACE_IO_LAYER, MONO_TRACE_ALL };

	if(!value)
		return;

	tok = value;

	while (*tok) {
		if (*tok == ',') {
			tok++;
			continue;
		}
		for (i = 0; valid_flags[i]; i++) {
			int len = strlen (valid_flags[i]);
			if (strncmp (tok, valid_flags[i], len) == 0 && (tok[len] == 0 || tok[len] == ',')) {
				flags |= valid_masks[i];
				tok += len;
				break;
			}
		}
		if (!valid_flags[i]) {
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
	return (level <= mono_internal_current_level && mask & mono_internal_current_mask);
}

/**
 * mono_trace_set_log_handler:
 *
 *  @callback The callback that will replace the default logging handler
 *  @user_data Argument passed to @callback
 *
 * The log handler replaces the default runtime logger. All logging requests with be routed to it.
 * If the fatal argument in the callback is true, the callback must abort the current process. The runtime expects that
 * execution will not resume after a fatal error.
 */
void
mono_trace_set_log_handler (MonoLogCallback *callback, const char *dest, void *user_data)
{
	g_assert (callback);
	logCallback.opener = callback->opener;
	logCallback.writer = callback->writer;
	logCallback.closer = callback->closer;
	logCallback.opener(dest, user_data);
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
 *
 * @callback The callback that will replace the default runtime behavior for stdout output.
 *
 * The print handler replaces the default runtime stdout output handler. This is used by free form output done by the runtime.
 *
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
 *
 * @callback The callback that will replace the default runtime behavior for stderr output.
 *
 * The print handler replaces the default runtime stderr output handler. This is used by free form output done by the runtime.
 *
 */
void
mono_trace_set_printerr_handler (MonoPrintCallback callback)
{
	g_assert (callback);
	printerr_callback = callback;
	g_set_printerr_handler (printerr_handler);
}
