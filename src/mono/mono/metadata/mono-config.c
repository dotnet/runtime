/*
 * mono-config.c
 *
 * Runtime and assembly configuration file support routines.
 *
 * Author: Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */
#include "config.h"
#include <glib.h>
#include <string.h>
#include "mono/metadata/loader.h"
#include "mono/metadata/mono-config.h"

static void start_element (GMarkupParseContext *context, 
                           const gchar         *element_name,
			   const gchar        **attribute_names,
			   const gchar        **attribute_values,
			   gpointer             user_data,
			   GError             **error);

static void end_element   (GMarkupParseContext *context,
                           const gchar         *element_name,
			   gpointer             user_data,
			   GError             **error);

static void parse_text    (GMarkupParseContext *context,
                           const gchar         *text,
			   gsize                text_len,
			   gpointer             user_data,
			   GError             **error);

static void passthrough   (GMarkupParseContext *context,
                           const gchar         *text,
			   gsize                text_len,
			   gpointer             user_data,
			   GError             **error);

static void parse_error   (GMarkupParseContext *context,
                           GError              *error,
			   gpointer             user_data);

static const GMarkupParser 
mono_parser = {
	start_element,
	end_element,
	parse_text,
	passthrough,
	parse_error
};

static GHashTable *config_handlers;

/* when this interface is stable, export it. */
typedef struct MonoParseHandler MonoParseHandler;

struct MonoParseHandler {
	const char *element_name;
	void*(*init)   (void);
	void (*start)  (gpointer user_data, const gchar *name,
	                const gchar **attributes,
		        const gchar **values);
	void (*text)   (gpointer user_data, const char *text, gsize test_len);
	void (*end)    (gpointer user_data, const char *name);
	void (*finish) (gpointer user_data);
};

typedef struct {
	MonoParseHandler *current;
	void *user_data;
	int inited;
} ParseState;

static void start_element (GMarkupParseContext *context, 
                           const gchar         *element_name,
			   const gchar        **attribute_names,
			   const gchar        **attribute_values,
			   gpointer             user_data,
			   GError             **error)
{
	ParseState *state = user_data;
	if (!state->current) {
		state->current = g_hash_table_lookup (config_handlers, element_name);
		if (state->current && state->current->init)
			state->user_data = state->current->init ();
	}
	if (state->current && state->current->start)
		state->current->start (state->user_data, element_name, attribute_names, attribute_values);
}

static void end_element   (GMarkupParseContext *context,
                           const gchar         *element_name,
			   gpointer             user_data,
			   GError             **error)
{
	ParseState *state = user_data;
	if (state->current) {
		if (state->current->end)
			state->current->end (state->user_data, element_name);
		if (strcmp (state->current->element_name, element_name) == 0) {
			if (state->current->finish)
				state->current->finish (state->user_data);
			state->current = NULL;
			state->user_data = NULL;
		}
	}
}

static void parse_text    (GMarkupParseContext *context,
                           const gchar         *text,
			   gsize                text_len,
			   gpointer             user_data,
			   GError             **error)
{
	ParseState *state = user_data;
	if (state->current && state->current->text)
		state->current->text (state->user_data, text, text_len);
}

static void passthrough   (GMarkupParseContext *context,
                           const gchar         *text,
			   gsize                text_len,
			   gpointer             user_data,
			   GError             **error)
{
	/* do nothing */
}

static void parse_error   (GMarkupParseContext *context,
                           GError              *error,
			   gpointer             user_data)
{
}

typedef struct {
	char *dll;
	char *target;
} DllInfo;

static void*
dllmap_init (void) {
	DllInfo *info = g_new0 (DllInfo, 1);
	return info;
}

static void
dllmap_start (gpointer user_data, 
              const gchar         *element_name,
              const gchar        **attribute_names,
              const gchar        **attribute_values)
{
	int i;
	DllInfo *info = user_data;
	
	if (strcmp (element_name, "dllmap") == 0) {
		g_free (info->dll);
		g_free (info->target);
		info->dll = info->target = NULL;
		for (i = 0; attribute_names [i]; ++i) {
			if (strcmp (attribute_names [i], "dll") == 0)
				info->dll = g_strdup (attribute_values [i]);
			else if (strcmp (attribute_names [i], "target") == 0)
				info->target = g_strdup (attribute_values [i]);
		}
		mono_dllmap_insert (info->dll, NULL, info->target, NULL);
	} else if (strcmp (element_name, "dllentry") == 0) {
		const char *name = NULL, *target = NULL, *dll = NULL;
		for (i = 0; attribute_names [i]; ++i) {
			if (strcmp (attribute_names [i], "dll") == 0)
				dll = attribute_values [i];
			else if (strcmp (attribute_names [i], "target") == 0)
				target = attribute_values [i];
			else if (strcmp (attribute_names [i], "name") == 0)
				name = attribute_values [i];
		}
		if (!dll)
			dll = info->dll;
		mono_dllmap_insert (info->dll, name, dll, target);
	}
}

static void
dllmap_finish (gpointer user_data)
{
	DllInfo *info = user_data;

	g_free (info->dll);
	g_free (info->target);
	g_free (info);
}

static const MonoParseHandler
dllmap_handler = {
	"dllmap",
	dllmap_init,
	dllmap_start,
	NULL, /* text */
	NULL, /* end */
	dllmap_finish
};


static int inited = 0;

static void
mono_config_init (void)
{
	inited = 1;
	config_handlers = g_hash_table_new (g_str_hash, g_str_equal);
	g_hash_table_insert (config_handlers, (gpointer) dllmap_handler.element_name, (gpointer) &dllmap_handler);
}

/* FIXME: error handling */

static void
mono_config_parse_file (const char *filename)
{
	GMarkupParseContext *context;
	ParseState state = {NULL};
	char *text;
	gsize len;

	if (!inited)
		mono_config_init ();

	if (!g_file_get_contents (filename, &text, &len, NULL))
		return;
	context = g_markup_parse_context_new (&mono_parser, 0, &state, NULL);
	if (g_markup_parse_context_parse (context, text, len, NULL)) {
		g_markup_parse_context_end_parse (context, NULL);
	}
	g_markup_parse_context_free (context);
	g_free (text);
}

/*
 * Pass a NULL filename to parse the default config files
 * (or the file in the MONO_CONFIG env var).
 */
void
mono_config_parse (const char *filename) {
	const char *home;
	char *user_cfg;
	char *mono_cfg;
	extern char *mono_cfg_dir;
	
	if (filename) {
		mono_config_parse_file (filename);
		return;
	}

	home = g_getenv ("MONO_CONFIG");
	if (home) {
		mono_config_parse_file (home);
		return;
	}

	/* Ensure mono_cfg_dir gets a value */
	mono_install_get_config_dir ();
	mono_cfg = g_build_filename (mono_cfg_dir, "mono", "config", NULL);
	mono_config_parse_file (mono_cfg);
	g_free (mono_cfg);

#ifndef PLATFORM_WIN32
	home = g_get_home_dir ();
	user_cfg = g_strconcat (home, G_DIR_SEPARATOR_S, ".mono/config", NULL);
	mono_config_parse_file (user_cfg);
	g_free (user_cfg);
#endif
}

