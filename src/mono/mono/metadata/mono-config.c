/**
 * \file
 *
 * Runtime and assembly configuration file support routines.
 *
 * Author: Paolo Molaro (lupus@ximian.com)
 *
 * Copyright 2002-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include "config.h"
#include <glib.h>
#include <string.h>

#include "mono/metadata/assembly.h"
#include "mono/metadata/loader.h"
#include "mono/metadata/mono-config.h"
#include "mono/metadata/mono-config-internals.h"
#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/object-internals.h"
#include "mono/utils/mono-logger-internals.h"

#if defined(TARGET_PS3)
#define CONFIG_OS "CellOS"
#elif defined(__linux__)
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
#elif defined(__WIN32__) || defined(TARGET_WIN32)
#define CONFIG_OS "windows"
#elif defined(_IBMR2)
#define CONFIG_OS "aix"
#elif defined(__hpux)
#define CONFIG_OS "hpux"
#elif defined(__HAIKU__)
#define CONFIG_OS "haiku"
#elif defined (TARGET_WASM)
#define CONFIG_OS "wasm"
#else
#warning Unknown operating system
#define CONFIG_OS "unknownOS"
#endif

#ifndef CONFIG_CPU
#if defined(__i386__) || defined(TARGET_X86)
#define CONFIG_CPU "x86"
#define CONFIG_WORDSIZE "32"
#elif defined(__x86_64__) || defined(TARGET_AMD64)
#define CONFIG_CPU "x86-64"
#define CONFIG_WORDSIZE "64"
#elif defined(sparc) || defined(__sparc__)
#define CONFIG_CPU "sparc"
#define CONFIG_WORDSIZE "32"
#elif defined(__ppc64__) || defined(__powerpc64__) || defined(_ARCH_64) || defined(TARGET_POWERPC)
#define CONFIG_WORDSIZE "64"
#ifdef __mono_ppc_ilp32__ 
#   define CONFIG_CPU "ppc64ilp32"
#else
#   define CONFIG_CPU "ppc64"
#endif
#elif defined(__ppc__) || defined(__powerpc__)
#define CONFIG_CPU "ppc"
#define CONFIG_WORDSIZE "32"
#elif defined(__s390x__)
#define CONFIG_CPU "s390x"
#define CONFIG_WORDSIZE "64"
#elif defined(__s390__)
#define CONFIG_CPU "s390"
#define CONFIG_WORDSIZE "32"
#elif defined(__arm__)
#define CONFIG_CPU "arm"
#define CONFIG_WORDSIZE "32"
#elif defined(__aarch64__)
#define CONFIG_CPU "armv8"
#define CONFIG_WORDSIZE "64"
#elif defined(mips) || defined(__mips) || defined(_mips)
#define CONFIG_CPU "mips"
#define CONFIG_WORDSIZE "32"
#elif defined (TARGET_RISCV32)
#define CONFIG_CPU "riscv32"
#define CONFIG_WORDSIZE "32"
#elif defined (TARGET_RISCV64)
#define CONFIG_CPU "riscv64"
#define CONFIG_WORDSIZE "64"
#elif defined(TARGET_WASM)
#define CONFIG_CPU "wasm"
#define CONFIG_WORDSIZE "32"
#else
#error Unknown CPU
#define CONFIG_CPU "unknownCPU"
#endif
#endif

/**
 * mono_config_get_os:
 *
 * Returns the operating system that Mono is running on, as used for dllmap entries.
 */
const char *
mono_config_get_os (void)
{
	return CONFIG_OS;
}

/**
 * mono_config_get_cpu:
 *
 * Returns the architecture that Mono is running on, as used for dllmap entries.
 */
const char *
mono_config_get_cpu (void)
{
	return CONFIG_CPU;
}

/**
 * mono_config_get_wordsize:
 *
 * Returns the word size that Mono is running on, as used for dllmap entries.
 */
const char *
mono_config_get_wordsize (void)
{
	return CONFIG_WORDSIZE;
}

static void start_element (GMarkupParseContext *context, 
                           const gchar         *element_name,
			   const gchar        **attribute_names,
			   const gchar        **attribute_values,
			   gpointer             user_data,
			   GError             **gerror);

static void end_element   (GMarkupParseContext *context,
                           const gchar         *element_name,
			   gpointer             user_data,
			   GError             **gerror);

static void parse_text    (GMarkupParseContext *context,
                           const gchar         *text,
			   gsize                text_len,
			   gpointer             user_data,
			   GError             **gerror);

static void passthrough   (GMarkupParseContext *context,
                           const gchar         *text,
			   gsize                text_len,
			   gpointer             user_data,
			   GError             **gerror);

static void parse_error   (GMarkupParseContext *context,
                           GError              *gerror,
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

static char *mono_cfg_dir = NULL;

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
	MonoAssemblyBindingInfo *info;
	void (*info_parsed)(MonoAssemblyBindingInfo *info, void *user_data);
	void *user_data;
} ParserUserData;

typedef struct {
	MonoParseHandler *current;
	void *user_data;
	MonoImage *assembly;
	int inited;
} MonoConfigParseState;

static void start_element (GMarkupParseContext *context, 
                           const gchar         *element_name,
			   const gchar        **attribute_names,
			   const gchar        **attribute_values,
			   gpointer             user_data,
			   GError             **gerror)
{
	MonoConfigParseState *state = (MonoConfigParseState *)user_data;
	if (!state->current) {
		state->current = (MonoParseHandler *)g_hash_table_lookup (config_handlers, element_name);
		if (state->current && state->current->init)
			state->user_data = state->current->init (state->assembly);
	}
	if (state->current && state->current->start)
		state->current->start (state->user_data, element_name, attribute_names, attribute_values);
}

static void end_element   (GMarkupParseContext *context,
                           const gchar         *element_name,
			   gpointer             user_data,
			   GError             **gerror)
{
	MonoConfigParseState *state = (MonoConfigParseState *)user_data;
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
			   GError             **gerror)
{
	MonoConfigParseState *state = (MonoConfigParseState *)user_data;
	if (state->current && state->current->text)
		state->current->text (state->user_data, text, text_len);
}

static void passthrough   (GMarkupParseContext *context,
                           const gchar         *text,
			   gsize                text_len,
			   gpointer             user_data,
			   GError             **gerror)
{
	/* do nothing */
}

static void parse_error   (GMarkupParseContext *context,
                           GError              *gerror,
			   gpointer             user_data)
{
	MonoConfigParseState *state = (MonoConfigParseState *)user_data;
	const gchar *msg;
	const gchar *filename;

	filename = state && state->user_data ? (gchar *) state->user_data : "<unknown>";
	msg = gerror && gerror->message ? gerror->message : "";
	g_warning ("Error parsing %s: %s", filename, msg);
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
	DllInfo *info = (DllInfo *)user_data;
	
	if (strcmp (element_name, "dllmap") == 0) {
		g_free (info->dll);
		g_free (info->target);
		info->dll = info->target = NULL;
		info->ignore = FALSE;
		for (i = 0; attribute_names [i]; ++i) {
			if (strcmp (attribute_names [i], "dll") == 0)
				info->dll = g_strdup (attribute_values [i]);
			else if (strcmp (attribute_names [i], "target") == 0){
				const char* p = strstr (attribute_values [i], "$mono_libdir");
				if (p != NULL){
					char *libdir = mono_native_getrootdir ();
					size_t libdir_len = strlen (libdir);
					char *result;
					
					result = (char *)g_malloc (libdir_len-strlen("$mono_libdir")+strlen(attribute_values[i])+1);
					strncpy (result, attribute_values[i], p-attribute_values[i]);
					strcpy (result+(p-attribute_values[i]), libdir);
					g_free (libdir);
					strcat (result, p+strlen("$mono_libdir"));
					info->target = result;
				} else 
					info->target = g_strdup (attribute_values [i]);
			} else if (strcmp (attribute_names [i], "os") == 0 && !arch_matches (CONFIG_OS, attribute_values [i]))
				info->ignore = TRUE;
			else if (strcmp (attribute_names [i], "cpu") == 0 && !arch_matches (CONFIG_CPU, attribute_values [i]))
				info->ignore = TRUE;
			else if (strcmp (attribute_names [i], "wordsize") == 0 && !arch_matches (CONFIG_WORDSIZE, attribute_values [i]))
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
			else if (strcmp (attribute_names [i], "wordsize") == 0 && !arch_matches (CONFIG_WORDSIZE, attribute_values [i]))
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
	DllInfo *info = (DllInfo *)user_data;

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
				(g_ascii_strcasecmp (attribute_values [0], "true") == 0)) {
			mono_runtime_unhandled_exception_policy_set (MONO_UNHANDLED_POLICY_LEGACY);
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

static void
aot_cache_start (gpointer user_data,
				 const gchar         *element_name,
				 const gchar        **attribute_names,
				 const gchar        **attribute_values)
{
	int i;
	MonoAotCacheConfig *config;

	if (strcmp (element_name, "aotcache") != 0)
		return;

	config = mono_get_aot_cache_config ();

	/* Per-app configuration */
	for (i = 0; attribute_names [i]; ++i) {
		if (!strcmp (attribute_names [i], "app")) {
			config->apps = g_slist_prepend (config->apps, g_strdup (attribute_values [i]));
		}
	}

	/* Global configuration */
	for (i = 0; attribute_names [i]; ++i) {
		if (!strcmp (attribute_names [i], "assemblies")) {
			char **parts, **ptr;
			char *part;

			parts = g_strsplit (attribute_values [i], " ", -1);
			for (ptr = parts; ptr && *ptr; ptr ++) {
				part = *ptr;
				config->assemblies = g_slist_prepend (config->assemblies, g_strdup (part));
			}
			g_strfreev (parts);
		} else if (!strcmp (attribute_names [i], "options")) {
			config->aot_options = g_strdup (attribute_values [i]);
		}
	}
}

static const MonoParseHandler
aot_cache_handler = {
	"aotcache",
	NULL, /* init */
	aot_cache_start,
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
	g_hash_table_insert (config_handlers, (gpointer) aot_cache_handler.element_name, (gpointer) &aot_cache_handler);
}

/**
 * mono_config_cleanup:
 */
void
mono_config_cleanup (void)
{
	if (config_handlers)
		g_hash_table_destroy (config_handlers);
	g_free (mono_cfg_dir);
}

/* FIXME: error handling */

static void
mono_config_parse_xml_with_context (MonoConfigParseState *state, const char *text, gsize len)
{
	GMarkupParseContext *context;

	if (!inited)
		mono_config_init ();

	context = g_markup_parse_context_new (&mono_parser, (GMarkupParseFlags)0, state, NULL);
	if (g_markup_parse_context_parse (context, text, len, NULL)) {
		g_markup_parse_context_end_parse (context, NULL);
	}
	g_markup_parse_context_free (context);
}

/* If assembly is NULL, parse in the global context */
static int
mono_config_parse_file_with_context (MonoConfigParseState *state, const char *filename)
{
	gchar *text;
	gsize len;
	gint offset;

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_CONFIG,
			"Config attempting to parse: '%s'.", filename);

	if (!g_file_get_contents (filename, &text, &len, NULL))
		return 0;

	offset = 0;
	if (len > 3 && text [0] == '\xef' && text [1] == (gchar) '\xbb' && text [2] == '\xbf')
		offset = 3; /* Skip UTF-8 BOM */
	if (state->user_data == NULL)
		state->user_data = (gpointer) filename;
	mono_config_parse_xml_with_context (state, text + offset, len - offset);
	g_free (text);
	return 1;
}

/**
 * mono_config_parse_memory:
 * \param buffer a pointer to an string XML representation of the configuration
 * Parses the configuration from a buffer
 */
void
mono_config_parse_memory (const char *buffer)
{
	MonoConfigParseState state = {NULL};

	state.user_data = (gpointer) "<buffer>";
	mono_config_parse_xml_with_context (&state, buffer, strlen (buffer));
}

static void
mono_config_parse_file (const char *filename)
{
	MonoConfigParseState state = {NULL};
	state.user_data = (gpointer) filename;
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

/**
 * mono_register_config_for_assembly:
 */
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

/**
 * mono_config_string_for_assembly_file:
 */
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
mono_config_for_assembly_internal (MonoImage *assembly)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoConfigParseState state = {NULL};
	int got_it = 0, i;
	char *aname, *cfg, *cfg_name;
	const char *bundled_config;
	
	state.assembly = assembly;

	bundled_config = mono_config_string_for_assembly_file (assembly->module_name);
	if (bundled_config) {
		state.user_data = (gpointer) "<bundled>";
		mono_config_parse_xml_with_context (&state, bundled_config, strlen (bundled_config));
	}

	cfg_name = g_strdup_printf ("%s.config", mono_image_get_filename (assembly));
	mono_config_parse_file_with_context (&state, cfg_name);
	g_free (cfg_name);

	cfg_name = g_strdup_printf ("%s.config", mono_image_get_name (assembly));
	const char *cfg_dir = mono_get_config_dir ();
	if (!cfg_dir) {
		g_free (cfg_name);
		return;
	}

	for (i = 0; (aname = get_assembly_filename (assembly, i)) != NULL; ++i) {
		cfg = g_build_filename (cfg_dir, "mono", "assemblies", aname, cfg_name, NULL);
		got_it += mono_config_parse_file_with_context (&state, cfg);
		g_free (cfg);

#ifdef TARGET_WIN32
		const char *home = g_get_home_dir ();
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
 * \param filename the filename to load the configuration variables from.
 * Pass a NULL filename to parse the default config files
 * (or the file in the \c MONO_CONFIG env var).
 */
void
mono_config_parse (const char *filename) {
	const char *home;
	char *mono_cfg;
#ifndef TARGET_WIN32
	char *user_cfg;
#endif

	if (filename) {
		mono_config_parse_file (filename);
		return;
	}

	// FIXME: leak, do we store any references to home
	char *env_home = g_getenv ("MONO_CONFIG");
	if (env_home) {
		mono_config_parse_file (env_home);
		return;
	}

	const char *cfg_dir = mono_get_config_dir ();
	if (cfg_dir) {
		mono_cfg = g_build_filename (cfg_dir, "mono", "config", NULL);
		mono_config_parse_file (mono_cfg);
		g_free (mono_cfg);
	}

#if !defined(TARGET_WIN32)
	home = g_get_home_dir ();
	user_cfg = g_strconcat (home, G_DIR_SEPARATOR_S, ".mono/config", NULL);
	mono_config_parse_file (user_cfg);
	g_free (user_cfg);
#endif
}

/**
 * mono_set_config_dir:
 * Invoked during startup
 */
void
mono_set_config_dir (const char *dir)
{
	/* If this environment variable is set, overrides the directory computed */
	char *env_mono_cfg_dir = g_getenv ("MONO_CFG_DIR");
	if (env_mono_cfg_dir == NULL && dir != NULL)
		env_mono_cfg_dir = g_strdup (dir);

	if (mono_cfg_dir)
		g_free (mono_cfg_dir);
	mono_cfg_dir = env_mono_cfg_dir;
}

/**
 * mono_get_config_dir:
 */
const char* 
mono_get_config_dir (void)
{
	if (mono_cfg_dir == NULL)
		mono_set_dirs (NULL, NULL);

	return mono_cfg_dir;
}

/**
 * mono_register_machine_config:
 */
void
mono_register_machine_config (const char *config_xml)
{
	bundled_machine_config = config_xml;
}

/**
 * mono_get_machine_config:
 */
const char *
mono_get_machine_config (void)
{
	return bundled_machine_config;
}

static void
assembly_binding_end (gpointer user_data, const char *element_name)
{
	ParserUserData *pud = (ParserUserData *)user_data;

	if (!strcmp (element_name, "dependentAssembly")) {
		if (pud->info_parsed && pud->info) {
			pud->info_parsed (pud->info, pud->user_data);
			g_free (pud->info->name);
			g_free (pud->info->culture);
		}
	}
}

static void
publisher_policy_start (gpointer user_data,
		const gchar *element_name,
		const gchar **attribute_names,
		const gchar **attribute_values)
{
	ParserUserData *pud;
	MonoAssemblyBindingInfo *info;
	int n;

	pud = (ParserUserData *)user_data;
	info = pud->info;
	if (!strcmp (element_name, "dependentAssembly")) {
		info->name = NULL;
		info->culture = NULL;
		info->has_old_version_bottom = FALSE;
		info->has_old_version_top = FALSE;
		info->has_new_version = FALSE;
		info->is_valid = FALSE;
		memset (&info->old_version_bottom, 0, sizeof (info->old_version_bottom));
		memset (&info->old_version_top, 0, sizeof (info->old_version_top));
		memset (&info->new_version, 0, sizeof (info->new_version));
	} else if (!strcmp (element_name, "assemblyIdentity")) {
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
	ParserUserData user_data = {
		info,
		NULL,
		NULL
	};
	MonoConfigParseState state = {
		&publisher_policy_parser, /* MonoParseHandler */
		&user_data, /* user_data */
		NULL, /* MonoImage (we don't need it right now)*/
		TRUE /* We are already inited */
	};
	
	mono_config_parse_file_with_context (&state, filename);
}

static MonoParseHandler
config_assemblybinding_parser = {
	"", /* We don't need to use declare an xml element */
	NULL,
	publisher_policy_start,
	NULL,
	assembly_binding_end,
	NULL
};

void
mono_config_parse_assembly_bindings (const char *filename, int amajor, int aminor, void *user_data, void (*infocb)(MonoAssemblyBindingInfo *info, void *user_data))
{
	MonoAssemblyBindingInfo info;
	ParserUserData pud;
	MonoConfigParseState state;

	info.major = amajor;
	info.minor = aminor;

	pud.info = &info;
	pud.info_parsed = infocb;
	pud.user_data = user_data;

	state.current = &config_assemblybinding_parser;  /* MonoParseHandler */
	state.user_data = &pud;
	state.assembly = NULL; /* MonoImage (we don't need it right now)*/
	state.inited = TRUE; /* We are already inited */

	mono_config_parse_file_with_context (&state, filename);
}

static mono_bool mono_server_mode = FALSE;

/**
 * mono_config_set_server_mode:
 */
void
mono_config_set_server_mode (mono_bool server_mode)
{
	mono_server_mode = server_mode;
}

/**
 * mono_config_is_server_mode:
 */
mono_bool
mono_config_is_server_mode (void)
{
	return mono_server_mode;
}

