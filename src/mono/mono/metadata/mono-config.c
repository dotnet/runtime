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
#include "mono/metadata/metadata-internals.h"
#include "mono/utils/mono-logger.h"

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
	void*(*init)   (MonoImage *assembly);
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
	MonoImage *assembly;
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
			state->user_data = state->current->init (state->assembly);
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
	MonoImage *assembly;
} DllInfo;

static void*
dllmap_init (MonoImage *assembly) {
	DllInfo *info = g_new0 (DllInfo, 1);
	info->assembly = assembly;
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
		mono_dllmap_insert (info->assembly, info->dll, NULL, info->target, NULL);
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
		mono_dllmap_insert (info->assembly, info->dll, name, dll, target);
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

/* If assembly is NULL, parse in the global context */
static int
mono_config_parse_file_with_context (ParseState *state, const char *filename)
{
	GMarkupParseContext *context;
	char *text;
	gsize len;

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_CONFIG,
			"Config attempting to parse: '%s'.", filename);

	if (!inited)
		mono_config_init ();

	if (!g_file_get_contents (filename, &text, &len, NULL))
		return 0;
	context = g_markup_parse_context_new (&mono_parser, 0, state, NULL);
	if (g_markup_parse_context_parse (context, text, len, NULL)) {
		g_markup_parse_context_end_parse (context, NULL);
	}
	g_markup_parse_context_free (context);
	g_free (text);
	return 1;
}

static void
mono_config_parse_file (const char *filename)
{
	ParseState state = {NULL};
	mono_config_parse_file_with_context (&state, filename);
}

/* 
 * use the equivalent lookup code from the GAC when available.
 * Depending on state, this should give something like:
 * 	aname/version-pubtoken/
 * 	aname/version/
 * 	aname
 */
static char*
get_assembly_filename (MonoImage *image, int state)
{
	switch (state) {
	case 0:
		return g_strdup (mono_image_get_name (image));
	default:
		return NULL;
	}
}

void 
mono_config_for_assembly (MonoImage *assembly)
{
	ParseState state = {NULL};
	int got_it = 0, i;
	char *aname, *cfg, *cfg_name;
	const char *home;
	
	state.assembly = assembly;
	cfg_name = g_strdup_printf ("%s.config", mono_image_get_filename (assembly));
	mono_config_parse_file_with_context (&state, cfg_name);
	g_free (cfg_name);

	cfg_name = g_strdup_printf ("%s.config", mono_image_get_name (assembly));

	home = g_get_home_dir ();

	for (i = 0; (aname = get_assembly_filename (assembly, i)) != NULL; ++i) {
		cfg = g_build_filename (mono_get_config_dir (), "mono", "assemblies", aname, cfg_name, NULL);
		got_it += mono_config_parse_file_with_context (&state, cfg);
		g_free (cfg);

#ifndef PLATFORM_WIN32
		cfg = g_build_filename (home, ".mono", "assemblies", aname, cfg_name, NULL);
		got_it += mono_config_parse_file_with_context (&state, cfg);
		g_free (cfg);
#endif
		g_free (aname);
		if (got_it)
			break;
	}
	g_free (cfg_name);
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
	
	if (filename) {
		mono_config_parse_file (filename);
		return;
	}

	home = g_getenv ("MONO_CONFIG");
	if (home) {
		mono_config_parse_file (home);
		return;
	}

	mono_cfg = g_build_filename (mono_get_config_dir (), "mono", "config", NULL);
	mono_config_parse_file (mono_cfg);
	g_free (mono_cfg);

#ifndef PLATFORM_WIN32
	home = g_get_home_dir ();
	user_cfg = g_strconcat (home, G_DIR_SEPARATOR_S, ".mono/config", NULL);
	mono_config_parse_file (user_cfg);
	g_free (user_cfg);
#endif
}

static const char *mono_cfg_dir = NULL;

static void    
mono_install_get_config_dir (void)
{
#ifdef PLATFORM_WIN32
  gchar *prefix;
#endif

  mono_cfg_dir = g_getenv ("MONO_CFG_DIR");

  if (!mono_cfg_dir) {
#ifndef PLATFORM_WIN32
    mono_cfg_dir = MONO_CFG_DIR;
#else
    prefix = g_path_get_dirname (mono_assembly_getrootdir ());
    mono_cfg_dir = g_build_filename (prefix, "etc", NULL);
    g_free (prefix);
#endif
  }
}

const char* 
mono_get_config_dir (void)
{
	if (!mono_cfg_dir)
		mono_install_get_config_dir ();
	return mono_cfg_dir;
}

