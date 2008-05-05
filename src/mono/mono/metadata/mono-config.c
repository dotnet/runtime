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

#include "mono/metadata/assembly.h"
#include "mono/metadata/loader.h"
#include "mono/metadata/mono-config.h"
#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/object-internals.h"
#include "mono/utils/mono-logger.h"

#if defined(__linux__)
#define CONFIG_OS "linux"
#elif defined(__APPLE__)
#define CONFIG_OS "osx"
#elif defined(sun)
#define CONFIG_OS "solaris"
#elif defined(__FreeBSD__)
#define CONFIG_OS "freebsd"
#elif defined(__NetBSD__)
#define CONFIG_OS "netbsd"
#elif defined(__OpenBSD__)
#define CONFIG_OS "openbsd"
#elif defined(__WIN32__)
#define CONFIG_OS "windows"
#elif defined(_IBMR2)
#define CONFIG_OS "aix"
#elif defined(__hpux)
#define CONFIG_OS "hpux"
#else
#warning Unknown operating system
#define CONFIG_OS "unknownOS"
#endif

#if defined(__i386__)
#define CONFIG_CPU "x86"
#elif defined(__x86_64__)
#define CONFIG_CPU "x86-64"
#elif defined(sparc) || defined(__sparc__)
#define CONFIG_CPU "sparc"
#elif defined(__ppc64__) || defined(__powerpc64__)
#define CONFIG_CPU "ppc64"
#elif defined(__ppc__) || defined(__powerpc__)
#define CONFIG_CPU "ppc"
#elif defined(__s390x__)
#define CONFIG_CPU "s390x"
#elif defined(__s390__)
#define CONFIG_CPU "s390"
#elif defined(__arm__)
#define CONFIG_CPU "arm"
#elif defined(__ia64__)
#define CONFIG_CPU "ia64"
#elif defined(__alpha__)
#define CONFIG_CPU "alpha"
#elif defined(hppa) || defined(__hppa__)
#define CONFIG_CPU "hppa"
#elif defined(mips) || defined(__mips) || defined(_mips)
#define CONFIG_CPU "mips"
#else
#warning Unknown CPU
#define CONFIG_CPU "unknownCPU"
#endif

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

static int
arch_matches (const char* arch, const char *value)
{
	char **splitted, **p;
	int found = FALSE;
	if (value [0] == '!')
		return !arch_matches (arch, value + 1);
	p = splitted = g_strsplit (value, ",", 0);
	while (*p) {
		if (strcmp (arch, *p) == 0) {
			found = TRUE;
			break;
		}
		p++;
	}
	g_strfreev (splitted);
	return found;
}

typedef struct {
	char *dll;
	char *target;
	int ignore;
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
		info->ignore = FALSE;
		for (i = 0; attribute_names [i]; ++i) {
			if (strcmp (attribute_names [i], "dll") == 0)
				info->dll = g_strdup (attribute_values [i]);
			else if (strcmp (attribute_names [i], "target") == 0)
				info->target = g_strdup (attribute_values [i]);
			else if (strcmp (attribute_names [i], "os") == 0 && !arch_matches (CONFIG_OS, attribute_values [i]))
				info->ignore = TRUE;
			else if (strcmp (attribute_names [i], "cpu") == 0 && !arch_matches (CONFIG_CPU, attribute_values [i]))
				info->ignore = TRUE;
		}
		if (!info->ignore)
			mono_dllmap_insert (info->assembly, info->dll, NULL, info->target, NULL);
	} else if (strcmp (element_name, "dllentry") == 0) {
		const char *name = NULL, *target = NULL, *dll = NULL;
		int ignore = FALSE;
		for (i = 0; attribute_names [i]; ++i) {
			if (strcmp (attribute_names [i], "dll") == 0)
				dll = attribute_values [i];
			else if (strcmp (attribute_names [i], "target") == 0)
				target = attribute_values [i];
			else if (strcmp (attribute_names [i], "name") == 0)
				name = attribute_values [i];
			else if (strcmp (attribute_names [i], "os") == 0 && !arch_matches (CONFIG_OS, attribute_values [i]))
				ignore = TRUE;
			else if (strcmp (attribute_names [i], "cpu") == 0 && !arch_matches (CONFIG_CPU, attribute_values [i]))
				ignore = TRUE;
		}
		if (!dll)
			dll = info->dll;
		if (!info->ignore && !ignore)
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

static void
legacyUEP_start (gpointer user_data, 
              const gchar         *element_name,
              const gchar        **attribute_names,
              const gchar        **attribute_values) {
	if ((strcmp (element_name, "legacyUnhandledExceptionPolicy") == 0) &&
			(attribute_names [0] != NULL) &&
			(strcmp (attribute_names [0], "enabled") == 0)) {
		if ((strcmp (attribute_values [0], "1") == 0) ||
				(g_strcasecmp (attribute_values [0], "true") == 0)) {
			mono_runtime_unhandled_exception_policy_set (MONO_UNHANLED_POLICY_LEGACY);
		}
	}
}

static const MonoParseHandler
legacyUEP_handler = {
	"legacyUnhandledExceptionPolicy",
	NULL, /* init */
	legacyUEP_start,
	NULL, /* text */
	NULL, /* end */
	NULL, /* finish */
};

static int inited = 0;

static void
mono_config_init (void)
{
	inited = 1;
	config_handlers = g_hash_table_new (g_str_hash, g_str_equal);
	g_hash_table_insert (config_handlers, (gpointer) dllmap_handler.element_name, (gpointer) &dllmap_handler);
	g_hash_table_insert (config_handlers, (gpointer) legacyUEP_handler.element_name, (gpointer) &legacyUEP_handler);
}

/* FIXME: error handling */

static void
mono_config_parse_xml_with_context (ParseState *state, const char *text, gsize len)
{
	GMarkupParseContext *context;

	if (!inited)
		mono_config_init ();

	context = g_markup_parse_context_new (&mono_parser, 0, state, NULL);
	if (g_markup_parse_context_parse (context, text, len, NULL)) {
		g_markup_parse_context_end_parse (context, NULL);
	}
	g_markup_parse_context_free (context);
}

/* If assembly is NULL, parse in the global context */
static int
mono_config_parse_file_with_context (ParseState *state, const char *filename)
{
	char *text;
	gsize len;

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_CONFIG,
			"Config attempting to parse: '%s'.", filename);

	if (!g_file_get_contents (filename, &text, &len, NULL))
		return 0;
	mono_config_parse_xml_with_context (state, text, len);
	g_free (text);
	return 1;
}

/**
 * mono_config_parse_memory:
 * @buffer: a pointer to an string XML representation of the configuration
 *
 * Parses the configuration from a buffer
 */
void
mono_config_parse_memory (const char *buffer)
{
	ParseState state = {NULL};
	
	mono_config_parse_xml_with_context (&state, buffer, strlen (buffer));
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

typedef struct _BundledConfig BundledConfig;

struct _BundledConfig {
	BundledConfig *next;
	const char* aname;
	const char* config_xml;
};

static BundledConfig *bundled_configs = NULL;

static const char *bundled_machine_config = NULL;

void
mono_register_config_for_assembly (const char* assembly_name, const char* config_xml)
{
	BundledConfig *bconfig;

	bconfig = g_new0 (BundledConfig, 1);
	bconfig->aname = assembly_name;
	bconfig->config_xml = config_xml;
	bconfig->next = bundled_configs;
	bundled_configs = bconfig;
}

const char *
mono_config_string_for_assembly_file (const char *filename)
{
	BundledConfig *bconfig;
	
	for (bconfig = bundled_configs; bconfig; bconfig = bconfig->next) {
		if (bconfig->aname && strcmp (bconfig->aname, filename) == 0)
			return bconfig->config_xml;
	}
	return NULL;
}

void 
mono_config_for_assembly (MonoImage *assembly)
{
	ParseState state = {NULL};
	int got_it = 0, i;
	char *aname, *cfg, *cfg_name;
	const char *bundled_config;
	const char *home;
	
	state.assembly = assembly;

	bundled_config = mono_config_string_for_assembly_file (assembly->module_name);
	if (bundled_config)
		mono_config_parse_xml_with_context (&state, bundled_config, strlen (bundled_config));

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

/**
 * mono_config_parse:
 * @filename: the filename to load the configuration variables from.
 *
 * Pass a NULL filename to parse the default config files
 * (or the file in the MONO_CONFIG env var).
 */
void
mono_config_parse (const char *filename) {
	const char *home;
	char *mono_cfg;
#ifndef PLATFORM_WIN32
	char *user_cfg;
#endif

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

/* Invoked during startup */
void
mono_set_config_dir (const char *dir)
{
	/* If this variable is set, overrides the directory computed */
	mono_cfg_dir = g_getenv ("MONO_CFG_DIR");
	if (mono_cfg_dir == NULL)
		mono_cfg_dir = g_strdup (dir);
}

const char* 
mono_get_config_dir (void)
{
	if (mono_cfg_dir == NULL)
		mono_set_dirs (NULL, NULL);

	return mono_cfg_dir;
}

void
mono_register_machine_config (const char *config_xml)
{
	bundled_machine_config = config_xml;
}

const char *
mono_get_machine_config (void)
{
	return bundled_machine_config;
}

static void
publisher_policy_start (gpointer user_data,
		const gchar *element_name,
		const gchar **attribute_names,
		const gchar **attribute_values)
{
	MonoAssemblyBindingInfo *info;
	int n;

	info = user_data;
	if (!strcmp (element_name, "assemblyIdentity")) {
		for (n = 0; attribute_names [n]; n++) {
			const gchar *attribute_name = attribute_names [n];
			
			if (!strcmp (attribute_name, "name"))
				info->name = g_strdup (attribute_values [n]);
			else if (!strcmp (attribute_name, "publicKeyToken")) {
				if (strlen (attribute_values [n]) == MONO_PUBLIC_KEY_TOKEN_LENGTH - 1)
					g_strlcpy ((char *) info->public_key_token, attribute_values [n], MONO_PUBLIC_KEY_TOKEN_LENGTH);
			} else if (!strcmp (attribute_name, "culture")) {
				if (!strcmp (attribute_values [n], "neutral"))
					info->culture = g_strdup ("");
				else
					info->culture = g_strdup (attribute_values [n]);
			}
		}
	} else if (!strcmp (element_name, "bindingRedirect")) {
		for (n = 0; attribute_names [n]; n++) {
			const gchar *attribute_name = attribute_names [n];

			if (!strcmp (attribute_name, "oldVersion")) {
				gchar **numbers, **version, **versions;
				gint major, minor, build, revision;

				/* Invalid value */
				if (!strcmp (attribute_values [n], ""))
					return;
				
				versions = g_strsplit (attribute_values [n], "-", 2);
				version = g_strsplit (*versions, ".", 4);

				/* We assign the values to gint vars to do the checks */
				numbers = version;
				major = *numbers ? atoi (*numbers++) : -1;
				minor = *numbers ? atoi (*numbers++) : -1;
				build = *numbers ? atoi (*numbers++) : -1;
				revision = *numbers ? atoi (*numbers) : -1;
				g_strfreev (version);
				if (major < 0 || minor < 0 || build < 0 || revision < 0) {
					g_strfreev (versions);
					return;
				}

				info->old_version_bottom.major = major;
				info->old_version_bottom.minor = minor;
				info->old_version_bottom.build = build;
				info->old_version_bottom.revision = revision;
				info->has_old_version_bottom = TRUE;

				if (!*(versions + 1)) {
					g_strfreev (versions);
					continue;
				}
				
				numbers = version = g_strsplit (*(versions + 1), ".", 4);
				major = *numbers ? atoi (*numbers++) : -1;
				minor = *numbers ? atoi (*numbers++) : -1;
				build = *numbers ? atoi (*numbers++) : -1;
				revision = *numbers ? atoi (*numbers) : 1;
				g_strfreev (version);
				if (major < 0 || minor < 0 || build < 0 || revision < 0) {
					g_strfreev (versions);
					return;
				}

				info->old_version_top.major = major;
				info->old_version_top.minor = minor;
				info->old_version_top.build = build;
				info->old_version_top.revision = revision;
				info->has_old_version_top = TRUE;

				g_strfreev (versions);
			} else if (!strcmp (attribute_name, "newVersion")) {
				gchar **numbers, **version;

				/* Invalid value */
				if (!strcmp (attribute_values [n], ""))
					return;

				numbers = version = g_strsplit (attribute_values [n], ".", 4);
				info->new_version.major = *numbers ? atoi (*numbers++) : -1;
				info->new_version.minor = *numbers ? atoi (*numbers++) : -1;
				info->new_version.build = *numbers ? atoi (*numbers++) : -1;
				info->new_version.revision = *numbers ? atoi (*numbers) : -1;
				info->has_new_version = TRUE;
				g_strfreev (version);
			}
		}
	}
}

static MonoParseHandler
publisher_policy_parser = {
	"", /* We don't need to use declare an xml element */
	NULL,
	publisher_policy_start,
	NULL,
	NULL,
	NULL
};

void
mono_config_parse_publisher_policy (const gchar *filename, MonoAssemblyBindingInfo *info)
{
	ParseState state = {
		&publisher_policy_parser, /* MonoParseHandler */
		info, /* user_data */
		NULL, /* MonoImage (we don't need it right now)*/
		TRUE /* We are already inited */
	};
	
	mono_config_parse_file_with_context (&state, filename);
}

