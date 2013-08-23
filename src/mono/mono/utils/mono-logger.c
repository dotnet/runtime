#include <string.h>
#include <stdlib.h>
#include <stdio.h>
#include <glib.h>

#include "mono-compiler.h"
#include "mono-logger-internal.h"

typedef struct {
	GLogLevelFlags	level;
	MonoTraceMask	mask;
} MonoLogLevelEntry;

static GLogLevelFlags current_level		= G_LOG_LEVEL_ERROR;
static MonoTraceMask current_mask		= MONO_TRACE_ALL;

static const char	*mono_log_domain	= "Mono";
static GQueue		*level_stack		= NULL;

/**
 * mono_trace_init:
 *
 * Initializes the mono tracer.
 */
static void 
mono_trace_init (void)
{
	if(level_stack == NULL) {
		level_stack = g_queue_new();

		mono_trace_set_mask_string(g_getenv("MONO_LOG_MASK"));
		mono_trace_set_level_string(g_getenv("MONO_LOG_LEVEL"));
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
 * mono_trace:
 *
 *	@level: Verbose level of the specified message
 *	@mask: Type of the specified message
 *
 * Traces a new message, depending on the current logging level
 * and trace mask.
 */
void
mono_trace(GLogLevelFlags level, MonoTraceMask mask, const char *format, ...) 
{
	if(level_stack == NULL)
		mono_trace_init();

	if(level <= current_level && mask & current_mask) {
		va_list args;
		va_start (args, format);
		g_logv (mono_log_domain, level, format, args);
		va_end (args);
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
mono_tracev (GLogLevelFlags level, MonoTraceMask mask, const char *format, va_list args)
{
	if (level_stack == NULL)
		mono_trace_init ();

	if(level <= current_level && mask & current_mask)
		g_logv (mono_log_domain, level, format, args);
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

	current_level = level;
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

	current_mask	= mask;
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
		MonoLogLevelEntry *entry = g_malloc(sizeof(MonoLogLevelEntry));
		entry->level	= current_level;
		entry->mask		= current_mask;

		g_queue_push_head (level_stack, (gpointer)entry);

		/* Set the new level and mask
		 */
		current_level = level;
		current_mask  = mask;
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
			current_level = entry->level;
			current_mask  = entry->mask;

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

	const char *valid_flags[] = {"asm", "type", "dll", "gc", "cfg", "aot", "security", "all", NULL};
	const MonoTraceMask	valid_masks[] = {MONO_TRACE_ASSEMBLY, MONO_TRACE_TYPE, MONO_TRACE_DLLIMPORT,
						 MONO_TRACE_GC, MONO_TRACE_CONFIG, MONO_TRACE_AOT, MONO_TRACE_SECURITY, 
						 MONO_TRACE_ALL };

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

	mono_trace_set_mask (flags);
}

/*
 * mono_trace_is_traced:
 *
 *   Returns whenever a message with @level and @mask will be printed or not.
 */
gboolean
mono_trace_is_traced (GLogLevelFlags level, MonoTraceMask mask)
{
	return (level <= current_level && mask & current_mask);
}
