/**
 * \file
 * Routines for loading assemblies.
 * 
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <stdio.h>
#include <glib.h>
#include <errno.h>
#include <string.h>
#include <stdlib.h>
#include "assembly.h"
#include "assembly-internals.h"
#include "image.h"
#include "image-internals.h"
#include "object-internals.h"
#include <mono/metadata/loader.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/custom-attrs-internals.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/domain-internals.h>
#include <mono/metadata/exception-internals.h>
#include <mono/metadata/reflection-internals.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/mono-debug.h>
#include <mono/utils/mono-uri.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/mono-config-internals.h>
#include <mono/metadata/mono-config-dirs.h>
#include <mono/utils/mono-digest.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-path.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/coree.h>
#include <mono/metadata/cil-coff.h>
#include <mono/utils/mono-io-portability.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-os-mutex.h>
#include <mono/metadata/mono-private-unstable.h>

#ifndef HOST_WIN32
#include <sys/types.h>
#include <unistd.h>
#include <sys/stat.h>
#endif

#ifdef HOST_DARWIN
#include <mach-o/dyld.h>
#endif

/* the default search path is empty, the first slot is replaced with the computed value */
static char*
default_path [] = {
	NULL,
	NULL,
	NULL
};

/* Contains the list of directories to be searched for assemblies (MONO_PATH) */
static char **assemblies_path = NULL;

/* keeps track of loaded assemblies, excluding dynamic ones */
static GList *loaded_assemblies = NULL;
static guint32 loaded_assembly_count = 0;
static MonoAssembly *corlib;

static char* unquote (const char *str);

// This protects loaded_assemblies
static mono_mutex_t assemblies_mutex;

static inline void
mono_assemblies_lock ()
{
	mono_os_mutex_lock (&assemblies_mutex);
}

static inline void
mono_assemblies_unlock ()
{
	mono_os_mutex_unlock (&assemblies_mutex);
}

/* If defined, points to the bundled assembly information */
static const MonoBundledAssembly **bundles;

static const MonoBundledSatelliteAssembly **satellite_bundles;

/* Class lazy loading functions */
static GENERATE_TRY_GET_CLASS_WITH_CACHE (debuggable_attribute, "System.Diagnostics", "DebuggableAttribute")

static GENERATE_TRY_GET_CLASS_WITH_CACHE (internals_visible, "System.Runtime.CompilerServices", "InternalsVisibleToAttribute")
static MonoAssembly*
mono_assembly_invoke_search_hook_internal (MonoAssemblyLoadContext *alc, MonoAssembly *requesting, MonoAssemblyName *aname, gboolean postload);

static MonoAssembly *
invoke_assembly_preload_hook (MonoAssemblyLoadContext *alc, MonoAssemblyName *aname, gchar **apath);

static const char *
mono_asmctx_get_name (const MonoAssemblyContext *asmctx);

static gboolean
assembly_loadfrom_asmctx_from_path (const char *filename, MonoAssembly *requesting_assembly, gpointer user_data, MonoAssemblyContextKind *out_asmctx);

static gchar*
encode_public_tok (const guchar *token, gint32 len)
{
	const static gchar allowed [] = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };
	gchar *res;
	int i;

	res = (gchar *)g_malloc (len * 2 + 1);
	for (i = 0; i < len; i++) {
		res [i * 2] = allowed [token [i] >> 4];
		res [i * 2 + 1] = allowed [token [i] & 0xF];
	}
	res [len * 2] = 0;
	return res;
}

/**
 * mono_public_tokens_are_equal:
 * \param pubt1 first public key token
 * \param pubt2 second public key token
 *
 * Compare two public key tokens and return TRUE is they are equal and FALSE
 * otherwise.
 */
gboolean
mono_public_tokens_are_equal (const unsigned char *pubt1, const unsigned char *pubt2)
{
	return g_ascii_strncasecmp ((const char*) pubt1, (const char*) pubt2, 16) == 0;
}

/**
 * mono_set_assemblies_path:
 * \param path list of paths that contain directories where Mono will look for assemblies
 *
 * Use this method to override the standard assembly lookup system and
 * override any assemblies coming from the GAC.  This is the method
 * that supports the \c MONO_PATH variable.
 *
 * Notice that \c MONO_PATH and this method are really a very bad idea as
 * it prevents the GAC from working and it prevents the standard
 * resolution mechanisms from working.  Nonetheless, for some debugging
 * situations and bootstrapping setups, this is useful to have. 
 */
void
mono_set_assemblies_path (const char* path)
{
	char **splitted, **dest;

	splitted = g_strsplit (path, G_SEARCHPATH_SEPARATOR_S, 1000);
	if (assemblies_path)
		g_strfreev (assemblies_path);
	assemblies_path = dest = splitted;
	while (*splitted) {
		char *tmp = *splitted;
		if (*tmp)
			*dest++ = mono_path_canonicalize (tmp);
		g_free (tmp);
		splitted++;
	}
	*dest = *splitted;

	if (g_hasenv ("MONO_DEBUG"))
		return;

	splitted = assemblies_path;
	while (*splitted) {
		if (**splitted && !g_file_test (*splitted, G_FILE_TEST_IS_DIR))
			g_warning ("'%s' in MONO_PATH doesn't exist or has wrong permissions.", *splitted);

		splitted++;
	}
}

void
mono_set_assemblies_path_direct (char **path)
{
	g_strfreev (assemblies_path);
	assemblies_path = path;
}

static void
check_path_env (void)
{
	if (assemblies_path != NULL)
		return;

	char* path = g_getenv ("MONO_PATH");
	if (!path)
		return;

	mono_set_assemblies_path(path);
	g_free (path);
}

static void
mono_assembly_binding_info_free (MonoAssemblyBindingInfo *info)
{
	if (!info)
		return;

	g_free (info->name);
	g_free (info->culture);
}

/**
 * mono_assembly_names_equal:
 * \param l first assembly
 * \param r second assembly.
 *
 * Compares two \c MonoAssemblyName instances and returns whether they are equal.
 *
 * This compares the names, the cultures, the release version and their
 * public tokens.
 *
 * \returns TRUE if both assembly names are equal.
 */
gboolean
mono_assembly_names_equal (MonoAssemblyName *l, MonoAssemblyName *r)
{
	return mono_assembly_names_equal_flags (l, r, MONO_ANAME_EQ_NONE);
}

/**
 * mono_assembly_names_equal_flags:
 * \param l first assembly name
 * \param r second assembly name
 * \param flags flags that affect what is compared.
 *
 * Compares two \c MonoAssemblyName instances and returns whether they are equal.
 *
 * This compares the simple names and cultures and optionally the versions and
 * public key tokens, depending on the \c flags.
 *
 * \returns TRUE if both assembly names are equal.
 */
gboolean
mono_assembly_names_equal_flags (MonoAssemblyName *l, MonoAssemblyName *r, MonoAssemblyNameEqFlags flags)
{
	g_assert (l != NULL);
	g_assert (r != NULL);

	if (!l->name || !r->name)
		return FALSE;

	if ((flags & MONO_ANAME_EQ_IGNORE_CASE) != 0 && g_strcasecmp (l->name, r->name))
		return FALSE;

	if ((flags & MONO_ANAME_EQ_IGNORE_CASE) == 0 && strcmp (l->name, r->name))
		return FALSE;

	if (l->culture && r->culture && strcmp (l->culture, r->culture))
		return FALSE;

	if ((l->major != r->major || l->minor != r->minor ||
	     l->build != r->build || l->revision != r->revision) &&
	    (flags & MONO_ANAME_EQ_IGNORE_VERSION) == 0)
		if (! ((l->major == 0 && l->minor == 0 && l->build == 0 && l->revision == 0) || (r->major == 0 && r->minor == 0 && r->build == 0 && r->revision == 0)))
			return FALSE;

	if (!l->public_key_token [0] || !r->public_key_token [0] || (flags & MONO_ANAME_EQ_IGNORE_PUBKEY) != 0)
		return TRUE;

	if (!mono_public_tokens_are_equal (l->public_key_token, r->public_key_token))
		return FALSE;

	return TRUE;
}

/**
 * assembly_names_compare_versions:
 * \param l left assembly name
 * \param r right assembly name
 * \param maxcomps how many version components to compare, or -1 to compare all.
 *
 * \returns a negative if \p l is a lower version than \p r; a positive value
 * if \p r is a lower version than \p l, or zero if \p l and \p r are equal
 * versions (comparing upto \p maxcomps components).
 *
 * Components are \c major, \c minor, \c revision, and \c build. \p maxcomps 1 means just compare
 * majors. 2 means majors then minors. etc.
 */
static int
assembly_names_compare_versions (MonoAssemblyName *l, MonoAssemblyName *r, int maxcomps)
{
	int i = 0;
	if (maxcomps < 0) maxcomps = 4;
#define CMP(field) do {				\
		if (l-> field < r-> field && i < maxcomps) return -1;	\
		if (l-> field > r-> field && i < maxcomps) return 1;	\
	} while (0)
	CMP (major);
	++i;
	CMP (minor);
	++i;
	CMP (revision);
	++i;
	CMP (build);
#undef CMP
	return 0;
}

/**
 * mono_assembly_request_prepare_load:
 * \param req the load request to be initialized
 * \param asmctx the assembly load context kind
 * \param alc the AssemblyLoadContext in netcore
 *
 * Initialize an assembly loader request.  Its state will be reset and the assembly context kind will be prefilled with \p asmctx.
 */
void
mono_assembly_request_prepare_load (MonoAssemblyLoadRequest *req, MonoAssemblyContextKind asmctx, MonoAssemblyLoadContext *alc)
{
	memset (req, 0, sizeof (MonoAssemblyLoadRequest));
	req->asmctx = asmctx;
	req->alc = alc;
}

/**
 * mono_assembly_request_prepare_open:
 * \param req the open request to be initialized
 * \param asmctx the assembly load context kind
 * \param alc the AssemblyLoadContext in netcore
 *
 * Initialize an assembly loader request intended to be used for open operations.  Its state will be reset and the assembly context kind will be prefilled with \p asmctx.
 */
void
mono_assembly_request_prepare_open (MonoAssemblyOpenRequest *req, MonoAssemblyContextKind asmctx, MonoAssemblyLoadContext *alc)
{
	memset (req, 0, sizeof (MonoAssemblyOpenRequest));
	req->request.asmctx = asmctx;
	req->request.alc = alc;
}

/**
 * mono_assembly_request_prepare_byname:
 * \param req the byname request to be initialized
 * \param asmctx the assembly load context kind
 * \param alc the AssemblyLoadContext in netcore
 *
 * Initialize an assembly load by name request.  Its state will be reset and the assembly context kind will be prefilled with \p asmctx.
 */
void
mono_assembly_request_prepare_byname (MonoAssemblyByNameRequest *req, MonoAssemblyContextKind asmctx, MonoAssemblyLoadContext *alc)
{
	memset (req, 0, sizeof (MonoAssemblyByNameRequest));
	req->request.asmctx = asmctx;
	req->request.alc = alc;
}

static MonoAssembly *
load_in_path (const char *basename, const char** search_path, const MonoAssemblyOpenRequest *req, MonoImageOpenStatus *status)
{
	int i;
	char *fullpath;
	MonoAssembly *result;

	for (i = 0; search_path [i]; ++i) {
		fullpath = g_build_filename (search_path [i], basename, (const char*)NULL);
		result = mono_assembly_request_open (fullpath, req, status);
		g_free (fullpath);
		if (result)
			return result;
	}
	return NULL;
}

/**
 * mono_assembly_setrootdir:
 * \param root_dir The pathname of the root directory where we will locate assemblies
 *
 * This routine sets the internal default root directory for looking up
 * assemblies.
 *
 * This is used by Windows installations to compute dynamically the
 * place where the Mono assemblies are located.
 *
 */
void
mono_assembly_setrootdir (const char *root_dir)
{
	/*
	 * Override the MONO_ASSEMBLIES directory configured at compile time.
	 */
	if (default_path [0])
		g_free (default_path [0]);
	default_path [0] = g_strdup (root_dir);
}

/**
 * mono_assembly_getrootdir:
 * 
 * Obtains the root directory used for looking up assemblies.
 *
 * Returns: a string with the directory, this string should not be freed.
 */
G_CONST_RETURN gchar *
mono_assembly_getrootdir (void)
{
	return default_path [0];
}

/**
 * mono_native_getrootdir:
 * 
 * Obtains the root directory used for looking up native libs (.so, .dylib).
 *
 * Returns: a string with the directory, this string should be freed by
 * the caller.
 */
gchar *
mono_native_getrootdir (void)
{
	gchar* fullpath = g_build_path (G_DIR_SEPARATOR_S, mono_assembly_getrootdir (), mono_config_get_reloc_lib_dir(), (const char*)NULL);
	return fullpath;
}

/**
 * mono_set_dirs:
 * \param assembly_dir the base directory for assemblies
 * \param config_dir the base directory for configuration files
 *
 * This routine is used internally and by developers embedding
 * the runtime into their own applications.
 *
 * There are a number of cases to consider: Mono as a system-installed
 * package that is available on the location preconfigured or Mono in
 * a relocated location.
 *
 * If you are using a system-installed Mono, you can pass NULL
 * to both parameters.  If you are not, you should compute both
 * directory values and call this routine.
 *
 * The values for a given PREFIX are:
 *
 *    assembly_dir: PREFIX/lib
 *    config_dir:   PREFIX/etc
 *
 * Notice that embedders that use Mono in a relocated way must
 * compute the location at runtime, as they will be in control
 * of where Mono is installed.
 */
void
mono_set_dirs (const char *assembly_dir, const char *config_dir)
{
	if (assembly_dir == NULL)
		assembly_dir = mono_config_get_assemblies_dir ();
	if (config_dir == NULL)
		config_dir = mono_config_get_cfg_dir ();
	mono_assembly_setrootdir (assembly_dir);
	mono_set_config_dir (config_dir);
}

#ifndef HOST_WIN32

static char *
compute_base (char *path)
{
	char *p = strrchr (path, '/');
	if (p == NULL)
		return NULL;

	/* Not a well known Mono executable, we are embedded, cant guess the base  */
	if (strcmp (p, "/mono") && strcmp (p, "/mono-boehm") && strcmp (p, "/mono-sgen") && strcmp (p, "/pedump") && strcmp (p, "/monodis"))
		return NULL;
	    
	*p = 0;
	p = strrchr (path, '/');
	if (p == NULL)
		return NULL;
	
	if (strcmp (p, "/bin") != 0)
		return NULL;
	*p = 0;
	return path;
}

static void
fallback (void)
{
	mono_set_dirs (mono_config_get_assemblies_dir (), mono_config_get_cfg_dir ());
}

static G_GNUC_UNUSED void
set_dirs (char *exe)
{
	char *base;
	char *config, *lib, *mono;
	struct stat buf;
	const char *bindir;
	
	/*
	 * Only /usr prefix is treated specially
	 */
	bindir = mono_config_get_bin_dir ();
	g_assert (bindir);
	if (strncmp (exe, bindir, strlen (bindir)) == 0 || (base = compute_base (exe)) == NULL){
		fallback ();
		return;
	}

	config = g_build_filename (base, "etc", (const char*)NULL);
	lib = g_build_filename (base, "lib", (const char*)NULL);
	mono = g_build_filename (lib, "mono/4.5", (const char*)NULL);  // FIXME: stop hardcoding 4.5 here
	if (stat (mono, &buf) == -1)
		fallback ();
	else {
		mono_set_dirs (lib, config);
	}
	
	g_free (config);
	g_free (lib);
	g_free (mono);
}

#endif /* HOST_WIN32 */

/**
 * mono_set_rootdir:
 *
 * Registers the root directory for the Mono runtime, for Linux and Solaris 10,
 * this auto-detects the prefix where Mono was installed. 
 */
void
mono_set_rootdir (void)
{
#if defined(HOST_WIN32) || (defined(HOST_DARWIN) && !defined(TARGET_ARM))
	gchar *bindir, *installdir, *root, *name, *resolvedname, *config;

#ifdef HOST_WIN32
	name = mono_get_module_file_name ((HMODULE) &__ImageBase);
#else
 	{
		/* 
		 * _NSGetExecutablePath may return -1 to indicate buf is not large
		 *  enough, but we ignore that case to avoid having to do extra dynamic
		 *  allocation for the path and hope that 4096 is enough - this is 
		 *  ok in the Linux/Solaris case below at least...
		 */
 		
		gchar buf[4096];
 		guint buf_size = sizeof (buf);
 
		name = NULL;
 		if (_NSGetExecutablePath (buf, &buf_size) == 0)
 			name = g_strdup (buf);
 
 		if (name == NULL) {
 			fallback ();
 			return;
 		}
 	}
#endif

	resolvedname = mono_path_resolve_symlinks (name);

	bindir = g_path_get_dirname (resolvedname);
	installdir = g_path_get_dirname (bindir);
	root = g_build_path (G_DIR_SEPARATOR_S, installdir, "lib", (const char*)NULL);

	config = g_build_filename (root, "..", "etc", (const char*)NULL);
#ifdef HOST_WIN32
	mono_set_dirs (root, config);
#else
	if (g_file_test (root, G_FILE_TEST_EXISTS) && g_file_test (config, G_FILE_TEST_EXISTS))
		mono_set_dirs (root, config);
	else
		fallback ();
#endif

	g_free (config);
	g_free (root);
	g_free (installdir);
	g_free (bindir);
	g_free (name);
	g_free (resolvedname);
#elif defined(DISABLE_MONO_AUTODETECTION)
	fallback ();
#else
	char buf [4096];
	int  s;
	char *str;

#if defined(HAVE_READLINK)
	/* Linux style */
	s = readlink ("/proc/self/exe", buf, sizeof (buf)-1);
#else
	s = -1;
#endif

	if (s != -1){
		buf [s] = 0;
		set_dirs (buf);
		return;
	}

	/* Solaris 10 style */
	str = g_strdup_printf ("/proc/%d/path/a.out", getpid ());

#if defined(HAVE_READLINK)
	s = readlink (str, buf, sizeof (buf)-1);
#else
	s = -1;
#endif

	g_free (str);
	if (s != -1){
		buf [s] = 0;
		set_dirs (buf);
		return;
	} 
	fallback ();
#endif
}

/**
 * mono_assemblies_init:
 *
 *  Initialize global variables used by this module.
 */
void
mono_assemblies_init (void)
{
	/*
	 * Initialize our internal paths if we have not been initialized yet.
	 * This happens when embedders use Mono.
	 */
	if (mono_assembly_getrootdir () == NULL)
		mono_set_rootdir ();

	check_path_env ();

	mono_os_mutex_init_recursive (&assemblies_mutex);

	mono_install_assembly_asmctx_from_path_hook (assembly_loadfrom_asmctx_from_path, NULL);
}

gboolean
mono_assembly_fill_assembly_name_full (MonoImage *image, MonoAssemblyName *aname, gboolean copyBlobs)
{
	MonoTableInfo *t = &image->tables [MONO_TABLE_ASSEMBLY];
	guint32 cols [MONO_ASSEMBLY_SIZE];
	gint32 machine, flags;

	if (!table_info_get_rows (t))
		return FALSE;

	mono_metadata_decode_row (t, 0, cols, MONO_ASSEMBLY_SIZE);

	aname->hash_len = 0;
	aname->hash_value = NULL;
	aname->name = mono_metadata_string_heap (image, cols [MONO_ASSEMBLY_NAME]);
	if (copyBlobs)
		aname->name = g_strdup (aname->name);
	aname->culture = mono_metadata_string_heap (image, cols [MONO_ASSEMBLY_CULTURE]);
	if (copyBlobs)
		aname->culture = g_strdup (aname->culture);
	aname->flags = cols [MONO_ASSEMBLY_FLAGS];
	aname->major = cols [MONO_ASSEMBLY_MAJOR_VERSION];
	aname->minor = cols [MONO_ASSEMBLY_MINOR_VERSION];
	aname->build = cols [MONO_ASSEMBLY_BUILD_NUMBER];
	aname->revision = cols [MONO_ASSEMBLY_REV_NUMBER];
	aname->hash_alg = cols [MONO_ASSEMBLY_HASH_ALG];
	if (cols [MONO_ASSEMBLY_PUBLIC_KEY]) {
		guchar* token = (guchar *)g_malloc (8);
		gchar* encoded;
		const gchar* pkey;
		int len;

		pkey = mono_metadata_blob_heap (image, cols [MONO_ASSEMBLY_PUBLIC_KEY]);
		len = mono_metadata_decode_blob_size (pkey, &pkey);
		aname->public_key = (guchar*)pkey;

		mono_digest_get_public_token (token, aname->public_key, len);
		encoded = encode_public_tok (token, 8);
		g_strlcpy ((char*)aname->public_key_token, encoded, MONO_PUBLIC_KEY_TOKEN_LENGTH);

		g_free (encoded);
		g_free (token);
	}
	else {
		aname->public_key = NULL;
		memset (aname->public_key_token, 0, MONO_PUBLIC_KEY_TOKEN_LENGTH);
	}

	if (cols [MONO_ASSEMBLY_PUBLIC_KEY]) {
		aname->public_key = (guchar*)mono_metadata_blob_heap (image, cols [MONO_ASSEMBLY_PUBLIC_KEY]);
		if (copyBlobs) {
			const gchar *pkey_end;
			int len = mono_metadata_decode_blob_size ((const gchar*) aname->public_key, &pkey_end);
			pkey_end += len; /* move to end */
			size_t size = pkey_end - (const gchar*)aname->public_key;
			guchar *tmp = g_new (guchar, size);
			memcpy (tmp, aname->public_key, size);
			aname->public_key = tmp;
		}

	}
	else
		aname->public_key = 0;

	machine = image->image_info->cli_header.coff.coff_machine;
	flags = image->image_info->cli_cli_header.ch_flags;
	switch (machine) {
	case COFF_MACHINE_I386:
		/* https://bugzilla.xamarin.com/show_bug.cgi?id=17632 */
		if (flags & (CLI_FLAGS_32BITREQUIRED|CLI_FLAGS_PREFERRED32BIT))
			aname->arch = MONO_PROCESSOR_ARCHITECTURE_X86;
		else if ((flags & 0x70) == 0x70)
			aname->arch = MONO_PROCESSOR_ARCHITECTURE_NONE;
		else
			aname->arch = MONO_PROCESSOR_ARCHITECTURE_MSIL;
		break;
	case COFF_MACHINE_IA64:
		aname->arch = MONO_PROCESSOR_ARCHITECTURE_IA64;
		break;
	case COFF_MACHINE_AMD64:
		aname->arch = MONO_PROCESSOR_ARCHITECTURE_AMD64;
		break;
	case COFF_MACHINE_ARM:
		aname->arch = MONO_PROCESSOR_ARCHITECTURE_ARM;
		break;
	default:
		break;
	}

	return TRUE;
}

/**
 * mono_assembly_fill_assembly_name:
 * \param image Image
 * \param aname Name
 * \returns TRUE if successful
 */
gboolean
mono_assembly_fill_assembly_name (MonoImage *image, MonoAssemblyName *aname)
{
	return mono_assembly_fill_assembly_name_full (image, aname, FALSE);
}

/**
 * mono_stringify_assembly_name:
 * \param aname the assembly name.
 *
 * Convert \p aname into its string format. The returned string is dynamically
 * allocated and should be freed by the caller.
 *
 * \returns a newly allocated string with a string representation of
 * the assembly name.
 */
char*
mono_stringify_assembly_name (MonoAssemblyName *aname)
{
	const char *quote = (aname->name && g_ascii_isspace (aname->name [0])) ? "\"" : "";

	GString *str;
	str = g_string_new (NULL);
	g_string_append_printf (str, "%s%s%s", quote, aname->name, quote);
	if (!aname->without_version)
		g_string_append_printf (str, ", Version=%d.%d.%d.%d", aname->major, aname->minor, aname->build, aname->revision);
	if (!aname->without_culture) {
		if (aname->culture && *aname->culture)
			g_string_append_printf (str, ", Culture=%s", aname->culture);
		else
			g_string_append_printf (str, ", Culture=%s", "neutral");
	}
	if (!aname->without_public_key_token) {
		if (aname->public_key_token [0])
			g_string_append_printf (str,", PublicKeyToken=%s%s", (char *)aname->public_key_token, (aname->flags & ASSEMBLYREF_RETARGETABLE_FLAG) ? ", Retargetable=Yes" : "");
		else g_string_append_printf (str,", PublicKeyToken=%s%s", "null", (aname->flags & ASSEMBLYREF_RETARGETABLE_FLAG) ? ", Retargetable=Yes" : "");
	}

	char *result = g_string_free (str, FALSE); //  result is the final formatted string.

	return result;
}

static gchar*
assemblyref_public_tok (MonoImage *image, guint32 key_index, guint32 flags)
{
	const gchar *public_tok;
	int len;

	public_tok = mono_metadata_blob_heap (image, key_index);
	len = mono_metadata_decode_blob_size (public_tok, &public_tok);

	if (flags & ASSEMBLYREF_FULL_PUBLIC_KEY_FLAG) {
		guchar token [8];
		mono_digest_get_public_token (token, (guchar*)public_tok, len);
		return encode_public_tok (token, 8);
	}

	return encode_public_tok ((guchar*)public_tok, len);
}

static gchar*
assemblyref_public_tok_checked (MonoImage *image, guint32 key_index, guint32 flags, MonoError *error)
{
	const gchar *public_tok;
	int len;

	public_tok = mono_metadata_blob_heap_checked (image, key_index, error);
	return_val_if_nok (error, NULL);
	if (!public_tok) {
		mono_error_set_bad_image (error, image, "expected public key token (index = %d) in assembly reference, but the Blob heap is NULL", key_index);
		return NULL;
	}
	len = mono_metadata_decode_blob_size (public_tok, &public_tok);

	if (flags & ASSEMBLYREF_FULL_PUBLIC_KEY_FLAG) {
		guchar token [8];
		mono_digest_get_public_token (token, (guchar*)public_tok, len);
		return encode_public_tok (token, 8);
	}
	return encode_public_tok ((guchar*)public_tok, len);
}

/**
 * mono_assembly_addref:
 * \param assembly the assembly to reference
 *
 * This routine increments the reference count on a MonoAssembly.
 * The reference count is reduced every time the method mono_assembly_close() is
 * invoked.
 */
gint32
mono_assembly_addref (MonoAssembly *assembly)
{
	return mono_atomic_inc_i32 (&assembly->ref_count);
}

gint32
mono_assembly_decref (MonoAssembly *assembly)
{
	return mono_atomic_dec_i32 (&assembly->ref_count);
}

/*
 * CAUTION: This table must be kept in sync with
 *          ivkm/reflect/Fusion.cs
 */

#define SILVERLIGHT_KEY "7cec85d7bea7798e"
#define WINFX_KEY "31bf3856ad364e35"
#define ECMA_KEY "b77a5c561934e089"
#define MSFINAL_KEY "b03f5f7f11d50a3a"
#define COMPACTFRAMEWORK_KEY "969db8053d3322ac"

typedef struct {
	const char *name;
	const char *from;
	const char *to;
} KeyRemapEntry;

static KeyRemapEntry key_remap_table[] = {
	{ "CustomMarshalers", COMPACTFRAMEWORK_KEY, MSFINAL_KEY },
	{ "Microsoft.CSharp", WINFX_KEY, MSFINAL_KEY },
	{ "Microsoft.VisualBasic", COMPACTFRAMEWORK_KEY, MSFINAL_KEY },
	{ "System", SILVERLIGHT_KEY, ECMA_KEY },
	{ "System", COMPACTFRAMEWORK_KEY, ECMA_KEY },
	{ "System.ComponentModel.Composition", WINFX_KEY, ECMA_KEY },
	{ "System.ComponentModel.DataAnnotations", "ddd0da4d3e678217", WINFX_KEY },
	{ "System.Core", SILVERLIGHT_KEY, ECMA_KEY },
	{ "System.Core", COMPACTFRAMEWORK_KEY, ECMA_KEY },
	{ "System.Data", COMPACTFRAMEWORK_KEY, ECMA_KEY },
	{ "System.Data.DataSetExtensions", COMPACTFRAMEWORK_KEY, ECMA_KEY },
	{ "System.Drawing", COMPACTFRAMEWORK_KEY, MSFINAL_KEY },
	{ "System.Messaging", COMPACTFRAMEWORK_KEY, MSFINAL_KEY },
	// FIXME: MS uses MSFINAL_KEY for .NET 4.5
	{ "System.Net", SILVERLIGHT_KEY, MSFINAL_KEY },
	{ "System.Numerics", WINFX_KEY, ECMA_KEY },
	{ "System.Runtime.Serialization", SILVERLIGHT_KEY, ECMA_KEY },
	{ "System.Runtime.Serialization", COMPACTFRAMEWORK_KEY, ECMA_KEY },
	{ "System.ServiceModel", WINFX_KEY, ECMA_KEY },
	{ "System.ServiceModel", COMPACTFRAMEWORK_KEY, ECMA_KEY },
	{ "System.ServiceModel.Web", SILVERLIGHT_KEY, WINFX_KEY },
	{ "System.Web.Services", COMPACTFRAMEWORK_KEY, MSFINAL_KEY },
	{ "System.Windows", SILVERLIGHT_KEY, MSFINAL_KEY },
	{ "System.Windows.Forms", COMPACTFRAMEWORK_KEY, ECMA_KEY },
	{ "System.Xml", SILVERLIGHT_KEY, ECMA_KEY },
	{ "System.Xml", COMPACTFRAMEWORK_KEY, ECMA_KEY },
	{ "System.Xml.Linq", WINFX_KEY, ECMA_KEY },
	{ "System.Xml.Linq", COMPACTFRAMEWORK_KEY, ECMA_KEY },
	{ "System.Xml.Serialization", WINFX_KEY, ECMA_KEY }
};

static void
remap_keys (MonoAssemblyName *aname)
{
	int i;
	for (i = 0; i < G_N_ELEMENTS (key_remap_table); i++) {
		const KeyRemapEntry *entry = &key_remap_table [i];

		if (strcmp (aname->name, entry->name) ||
		    !mono_public_tokens_are_equal (aname->public_key_token, (const unsigned char*) entry->from))
			continue;

		memcpy (aname->public_key_token, entry->to, MONO_PUBLIC_KEY_TOKEN_LENGTH);
		     
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY,
			    "Remapped public key token of retargetable assembly %s from %s to %s",
			    aname->name, entry->from, entry->to);
		return;
	}
}

static MonoAssemblyName *
mono_assembly_remap_version (MonoAssemblyName *aname, MonoAssemblyName *dest_aname)
{
	const MonoRuntimeInfo *current_runtime;

	if (aname->name == NULL) return aname;

	current_runtime = mono_get_runtime_info ();

	if (aname->flags & ASSEMBLYREF_RETARGETABLE_FLAG) {
		const AssemblyVersionSet* vset;

		/* Remap to current runtime */
		vset = &current_runtime->version_sets [0];

		memcpy (dest_aname, aname, sizeof(MonoAssemblyName));
		dest_aname->major = vset->major;
		dest_aname->minor = vset->minor;
		dest_aname->build = vset->build;
		dest_aname->revision = vset->revision;
		dest_aname->flags &= ~ASSEMBLYREF_RETARGETABLE_FLAG;

		/* Remap assembly name */
		if (!strcmp (aname->name, "System.Net"))
			dest_aname->name = g_strdup ("System");
		
		remap_keys (dest_aname);

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY,
					"The request to load the retargetable assembly %s v%d.%d.%d.%d was remapped to %s v%d.%d.%d.%d",
					aname->name,
					aname->major, aname->minor, aname->build, aname->revision,
					dest_aname->name,
					vset->major, vset->minor, vset->build, vset->revision
					);

		return dest_aname;
	}

	return aname;
}

/**
 * mono_assembly_get_assemblyref:
 * \param image pointer to the \c MonoImage to extract the information from.
 * \param index index to the assembly reference in the image.
 * \param aname pointer to a \c MonoAssemblyName that will hold the returned value.
 *
 * Fills out the \p aname with the assembly name of the \p index assembly reference in \p image.
 */
void
mono_assembly_get_assemblyref (MonoImage *image, int index, MonoAssemblyName *aname)
{
	MonoTableInfo *t;
	guint32 cols [MONO_ASSEMBLYREF_SIZE];
	const char *hash;

	t = &image->tables [MONO_TABLE_ASSEMBLYREF];

	mono_metadata_decode_row (t, index, cols, MONO_ASSEMBLYREF_SIZE);

	// ECMA-335: II.22.5 - AssemblyRef
	// HashValue can be null or non-null.  If non-null it's an index into the blob heap
	// Sometimes ILasm can create an image without a Blob heap.
	hash = mono_metadata_blob_heap_null_ok (image, cols [MONO_ASSEMBLYREF_HASH_VALUE]);
	if (hash) {
		aname->hash_len = mono_metadata_decode_blob_size (hash, &hash);
		aname->hash_value = hash;
	} else {
		aname->hash_len = 0;
		aname->hash_value = NULL;
	}
	aname->name = mono_metadata_string_heap (image, cols [MONO_ASSEMBLYREF_NAME]);
	aname->culture = mono_metadata_string_heap (image, cols [MONO_ASSEMBLYREF_CULTURE]);
	aname->flags = cols [MONO_ASSEMBLYREF_FLAGS];
	aname->major = cols [MONO_ASSEMBLYREF_MAJOR_VERSION];
	aname->minor = cols [MONO_ASSEMBLYREF_MINOR_VERSION];
	aname->build = cols [MONO_ASSEMBLYREF_BUILD_NUMBER];
	aname->revision = cols [MONO_ASSEMBLYREF_REV_NUMBER];

	if (cols [MONO_ASSEMBLYREF_PUBLIC_KEY]) {
		gchar *token = assemblyref_public_tok (image, cols [MONO_ASSEMBLYREF_PUBLIC_KEY], aname->flags);
		g_strlcpy ((char*)aname->public_key_token, token, MONO_PUBLIC_KEY_TOKEN_LENGTH);
		g_free (token);
	} else {
		memset (aname->public_key_token, 0, MONO_PUBLIC_KEY_TOKEN_LENGTH);
	}
}

static MonoAssembly *
search_bundle_for_assembly (MonoAssemblyLoadContext *alc, MonoAssemblyName *aname)
{
	if (bundles == NULL && satellite_bundles == NULL)
		return NULL;

	MonoImageOpenStatus status;
	MonoImage *image;
	MonoAssemblyLoadRequest req;
	image = mono_assembly_open_from_bundle (alc, aname->name, &status, aname->culture);
	if (!image && !g_str_has_suffix (aname->name, ".dll")) {
		char *name = g_strdup_printf ("%s.dll", aname->name);
		image = mono_assembly_open_from_bundle (alc, name, &status, aname->culture);
	}
	if (image) {
		mono_assembly_request_prepare_load (&req, MONO_ASMCTX_DEFAULT, alc);
		return mono_assembly_request_load_from (image, aname->name, &req, &status);
	}
	return NULL;
}

static MonoAssembly*
netcore_load_reference (MonoAssemblyName *aname, MonoAssemblyLoadContext *alc, MonoAssembly *requesting, gboolean postload)
{
	g_assert (alc != NULL);

	MonoAssemblyName mapped_aname;

	aname = mono_assembly_remap_version (aname, &mapped_aname);

	MonoAssembly *reference = NULL;

	gboolean is_satellite = !mono_assembly_name_culture_is_neutral (aname);
	gboolean is_default = mono_alc_is_default (alc);

	/*
	 * Try these until one of them succeeds (by returning a non-NULL reference):
	 * 1. Check if it's already loaded by the ALC.
	 *
	 * 2. If it's a non-default ALC, call the Load() method.
	 *
	 * 3. If the ALC is not the default and this is not a satellite request,
	 *    check if it's already loaded by the default ALC.
	 *
	 * 4. If we have a bundle registered and this is not a satellite request,
	 *    search the images for a matching name.
	 *
	 * 5. If we have a satellite bundle registered and this is a satellite request,
	 *    find the parent ALC and search the images for a matching name and culture.
	 *
	 * 6. If the ALC is the default or this is not a satellite request,
	 *    check the TPA list, APP_PATHS, and ApplicationBase.
	 *
	 * 7. If this is a satellite request, call the ALC ResolveSatelliteAssembly method.
	 *
	 * 8. Call the ALC Resolving event.  If the ALC is not the default and this is not
	 *    a satellite request, call the Resolving event in the default ALC first.
	 *
	 * 9. Call the ALC AssemblyResolve event (except for corlib satellite assemblies).
	 *
	 * 10. Return NULL.
	 */

	reference = mono_assembly_loaded_internal (alc, aname);
	if (reference) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Assembly already loaded in the active ALC: '%s'.", aname->name);
		goto leave;
	}

	if (!is_default) {
		reference = mono_alc_invoke_resolve_using_load_nofail (alc, aname);
		if (reference) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Assembly found using Load method: '%s'.", aname->name);
			goto leave;
		}
	}

	if (!is_default && !is_satellite) {
		reference = mono_assembly_loaded_internal (mono_alc_get_default (), aname);
		if (reference) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Assembly already loaded in the default ALC: '%s'.", aname->name);
			goto leave;
		}
	}

	if (bundles != NULL && !is_satellite) {
		reference = search_bundle_for_assembly (mono_alc_get_default (), aname);
		if (reference) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Assembly found in the bundle: '%s'.", aname->name);
			goto leave;
		}
	}

	if (satellite_bundles != NULL && is_satellite) {
		// Satellite assembly byname requests should be loaded in the same ALC as their parent assembly
		size_t name_len = strlen (aname->name);
		char *parent_name = NULL;
		MonoAssemblyLoadContext *parent_alc = NULL;
		if (g_str_has_suffix (aname->name, MONO_ASSEMBLY_RESOURCE_SUFFIX))
			parent_name = g_strdup_printf ("%s.dll", g_strndup (aname->name, name_len - strlen (MONO_ASSEMBLY_RESOURCE_SUFFIX)));

		if (parent_name) {
			MonoAssemblyOpenRequest req;
			mono_assembly_request_prepare_open (&req, MONO_ASMCTX_DEFAULT, alc);
			MonoAssembly *parent_assembly = mono_assembly_request_open (parent_name, &req, NULL);
			parent_alc = mono_assembly_get_alc (parent_assembly);
		}

		if (parent_alc)
			reference = search_bundle_for_assembly (parent_alc, aname);

		if (reference) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Assembly found in the satellite bundle: '%s'.", aname->name);
			goto leave;
		}
	}

	if (is_default || !is_satellite) {
		reference = invoke_assembly_preload_hook (mono_alc_get_default (), aname, assemblies_path);
		if (reference) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Assembly found with the filesystem probing logic: '%s'.", aname->name);
			goto leave;
		}
	}

	if (is_satellite) {
		reference = mono_alc_invoke_resolve_using_resolve_satellite_nofail (alc, aname);
		if (reference) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Assembly found with ResolveSatelliteAssembly method: '%s'.", aname->name);
			goto leave;
		}
	}

	// For compatibility with CoreCLR, invoke the Resolving event in the default ALC first whenever loading
	// a non-satellite assembly into a non-default ALC.  See: https://github.com/dotnet/runtime/issues/54814
	if (!is_default && !is_satellite) {
		reference = mono_alc_invoke_resolve_using_resolving_event_nofail (mono_alc_get_default (), aname);
		if (reference) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Assembly found with the Resolving event (default ALC): '%s'.", aname->name);
			goto leave;
		}
	}
	reference = mono_alc_invoke_resolve_using_resolving_event_nofail (alc, aname);
	if (reference) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Assembly found with the Resolving event: '%s'.", aname->name);
		goto leave;
	}

	// Looking up corlib resources here can cause an infinite loop
	// See: https://github.com/dotnet/coreclr/blob/0a762eb2f3a299489c459da1ddeb69e042008f07/src/vm/appdomain.cpp#L5178-L5239
	if (!(strcmp (aname->name, MONO_ASSEMBLY_CORLIB_RESOURCE_NAME) == 0 && is_satellite) && postload) {
		reference = mono_assembly_invoke_search_hook_internal (alc, requesting, aname, TRUE);
		if (reference) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Assembly found with AssemblyResolve event: '%s'.", aname->name);
			goto leave;
		}
	}

leave:
	return reference;
}

/**
 * mono_assembly_get_assemblyref_checked:
 * \param image pointer to the \c MonoImage to extract the information from.
 * \param index index to the assembly reference in the image.
 * \param aname pointer to a \c MonoAssemblyName that will hold the returned value.
 * \param error set on error
 *
 * Fills out the \p aname with the assembly name of the \p index assembly reference in \p image.
 *
 * \returns TRUE on success, otherwise sets \p error and returns FALSE
 */
gboolean
mono_assembly_get_assemblyref_checked (MonoImage *image, int index, MonoAssemblyName *aname, MonoError *error)
{
	guint32 cols [MONO_ASSEMBLYREF_SIZE];
	const char *hash;

	if (image_is_dynamic (image)) {
		MonoDynamicTable *t = &(((MonoDynamicImage*) image)->tables [MONO_TABLE_ASSEMBLYREF]);
		if (!mono_metadata_decode_row_dynamic_checked ((MonoDynamicImage*)image, t, index, cols, MONO_ASSEMBLYREF_SIZE, error))
			return FALSE;
	}
	else {
		MonoTableInfo *t = &image->tables [MONO_TABLE_ASSEMBLYREF];
		if (!mono_metadata_decode_row_checked (image, t, index, cols, MONO_ASSEMBLYREF_SIZE, error))
			return FALSE;
	}



	// ECMA-335: II.22.5 - AssemblyRef
	// HashValue can be null or non-null.  If non-null it's an index into the blob heap
	// Sometimes ILasm can create an image without a Blob heap.
	hash = mono_metadata_blob_heap_checked (image, cols [MONO_ASSEMBLYREF_HASH_VALUE], error);
	return_val_if_nok (error, FALSE);
	if (hash) {
		aname->hash_len = mono_metadata_decode_blob_size (hash, &hash);
		aname->hash_value = hash;
	} else {
		aname->hash_len = 0;
		aname->hash_value = NULL;
	}
	aname->name = mono_metadata_string_heap_checked (image, cols [MONO_ASSEMBLYREF_NAME], error);
	return_val_if_nok (error, FALSE);
	aname->culture = mono_metadata_string_heap_checked (image, cols [MONO_ASSEMBLYREF_CULTURE], error);
	return_val_if_nok (error, FALSE);
	aname->flags = cols [MONO_ASSEMBLYREF_FLAGS];
	aname->major = cols [MONO_ASSEMBLYREF_MAJOR_VERSION];
	aname->minor = cols [MONO_ASSEMBLYREF_MINOR_VERSION];
	aname->build = cols [MONO_ASSEMBLYREF_BUILD_NUMBER];
	aname->revision = cols [MONO_ASSEMBLYREF_REV_NUMBER];
	if (cols [MONO_ASSEMBLYREF_PUBLIC_KEY]) {
		gchar *token = assemblyref_public_tok_checked (image, cols [MONO_ASSEMBLYREF_PUBLIC_KEY], aname->flags, error);
		return_val_if_nok (error, FALSE);
		g_strlcpy ((char*)aname->public_key_token, token, MONO_PUBLIC_KEY_TOKEN_LENGTH);
		g_free (token);
	} else {
		memset (aname->public_key_token, 0, MONO_PUBLIC_KEY_TOKEN_LENGTH);
	}
	return TRUE;
}

/**
 * mono_assembly_load_reference:
 */
void
mono_assembly_load_reference (MonoImage *image, int index)
{
	MonoAssembly *reference;
	MonoAssemblyName aname;
	MonoImageOpenStatus status = MONO_IMAGE_OK;
	memset (&aname, 0, sizeof (MonoAssemblyName));
	/*
	 * image->references is shared between threads, so we need to access
	 * it inside a critical section.
	 */
	mono_image_lock (image);
	if (!image->references) {
		MonoTableInfo *t = &image->tables [MONO_TABLE_ASSEMBLYREF];
	
		int n = table_info_get_rows (t);
		image->references = g_new0 (MonoAssembly *, n + 1);
		image->nreferences = n;
	}
	reference = image->references [index];
	mono_image_unlock (image);
	if (reference)
		return;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Requesting loading reference %d (of %d) of %s", index, image->nreferences, image->name);

	ERROR_DECL (local_error);
	mono_assembly_get_assemblyref_checked (image, index, &aname, local_error);
	if (!is_ok (local_error)) {
		mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_ASSEMBLY, "Decoding assembly reference %d (of %d) of %s failed due to: %s", index, image->nreferences, image->name, mono_error_get_message (local_error));
		mono_error_cleanup (local_error);
		goto commit_reference;
	}

	if (image->assembly) {
		if (mono_trace_is_traced (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY)) {
			char *aname_str = mono_stringify_assembly_name (&aname);
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Loading reference %d of %s asmctx %s, looking for %s",
				    index, image->name, mono_asmctx_get_name (&image->assembly->context),
				    aname_str);
			g_free (aname_str);
		}

		MonoAssemblyByNameRequest req;
		mono_assembly_request_prepare_byname (&req, MONO_ASMCTX_DEFAULT, mono_image_get_alc (image));
		req.requesting_assembly = image->assembly;
		//req.no_postload_search = TRUE; // FIXME: should this be set?
		reference = mono_assembly_request_byname (&aname, &req, NULL);
	} else {
		g_assertf (image->assembly, "While loading reference %d MonoImage %s doesn't have a MonoAssembly", index, image->name);
	}

	if (reference == NULL){
		char *extra_msg;

		if (status == MONO_IMAGE_ERROR_ERRNO && errno == ENOENT) {
			extra_msg = g_strdup_printf ("The assembly was not found in the Global Assembly Cache, a path listed in the MONO_PATH environment variable, or in the location of the executing assembly (%s).\n", image->assembly != NULL ? image->assembly->basedir : "" );
		} else if (status == MONO_IMAGE_ERROR_ERRNO) {
			extra_msg = g_strdup_printf ("System error: %s\n", strerror (errno));
		} else if (status == MONO_IMAGE_MISSING_ASSEMBLYREF) {
			extra_msg = g_strdup ("Cannot find an assembly referenced from this one.\n");
		} else if (status == MONO_IMAGE_IMAGE_INVALID) {
			extra_msg = g_strdup ("The file exists but is not a valid assembly.\n");
		} else {
			extra_msg = g_strdup ("");
		}
		
		mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_ASSEMBLY, "The following assembly referenced from %s could not be loaded:\n"
				   "     Assembly:   %s    (assemblyref_index=%d)\n"
				   "     Version:    %d.%d.%d.%d\n"
				   "     Public Key: %s\n%s",
				   image->name, aname.name, index,
				   aname.major, aname.minor, aname.build, aname.revision,
				   strlen ((char*)aname.public_key_token) == 0 ? "(none)" : (char*)aname.public_key_token, extra_msg);
		g_free (extra_msg);

	}

commit_reference:
	mono_image_lock (image);
	if (reference == NULL) {
		/* Flag as not found */
		reference = (MonoAssembly *)REFERENCE_MISSING;
	}	

	if (!image->references [index]) {
		if (reference != REFERENCE_MISSING){
			mono_assembly_addref (reference);
			if (image->assembly)
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Assembly Ref addref %s[%p] -> %s[%p]: %d",
				    image->assembly->aname.name, image->assembly, reference->aname.name, reference, reference->ref_count);
		} else {
			if (image->assembly)
				mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Failed to load assembly %s[%p].",
				    image->assembly->aname.name, image->assembly);
		}
		
		image->references [index] = reference;
	}
	mono_image_unlock (image);

	if (image->references [index] != reference) {
		/* Somebody loaded it before us */
		mono_assembly_close (reference);
	}
}

/**
 * mono_assembly_load_references:
 * \param image
 * \param status
 * \deprecated There is no reason to use this method anymore, it does nothing
 *
 * This method is now a no-op, it does nothing other than setting the \p status to \c MONO_IMAGE_OK
 */
void
mono_assembly_load_references (MonoImage *image, MonoImageOpenStatus *status)
{
	/* This is a no-op now but it is part of the embedding API so we can't remove it */
	if (status)
		*status = MONO_IMAGE_OK;
}

typedef struct AssemblyLoadHook AssemblyLoadHook;
struct AssemblyLoadHook {
	AssemblyLoadHook *next;
	union {
		MonoAssemblyLoadFunc v1;
		MonoAssemblyLoadFuncV2 v2;
	} func;
	int version;
	gpointer user_data;
};

static AssemblyLoadHook *assembly_load_hook = NULL;

void
mono_assembly_invoke_load_hook_internal (MonoAssemblyLoadContext *alc, MonoAssembly *ass)
{
	AssemblyLoadHook *hook;

	for (hook = assembly_load_hook; hook; hook = hook->next) {
		if (hook->version == 1) {
			hook->func.v1 (ass, hook->user_data);
		} else {
			ERROR_DECL (hook_error);
			g_assert (hook->version == 2);
			hook->func.v2 (alc, ass, hook->user_data, hook_error);
			mono_error_assert_ok (hook_error); /* FIXME: proper error handling */
		}
	}
}

/**
 * mono_assembly_invoke_load_hook:
 */
void
mono_assembly_invoke_load_hook (MonoAssembly *ass)
{
	mono_assembly_invoke_load_hook_internal (mono_alc_get_default (), ass);
}

static void
mono_install_assembly_load_hook_v1 (MonoAssemblyLoadFunc func, gpointer user_data)
{
	AssemblyLoadHook *hook;
	
	g_return_if_fail (func != NULL);

	hook = g_new0 (AssemblyLoadHook, 1);
	hook->version = 1;
	hook->func.v1 = func;
	hook->user_data = user_data;
	hook->next = assembly_load_hook;
	assembly_load_hook = hook;
}

void
mono_install_assembly_load_hook_v2 (MonoAssemblyLoadFuncV2 func, gpointer user_data, gboolean append)
{
	g_return_if_fail (func != NULL);

	AssemblyLoadHook *hook = g_new0 (AssemblyLoadHook, 1);
	hook->version = 2;
	hook->func.v2 = func;
	hook->user_data = user_data;

	if (append && assembly_load_hook != NULL) { // If we don't have any installed hooks, append vs prepend is irrelevant
		AssemblyLoadHook *old = assembly_load_hook;
		while (old->next != NULL)
			old = old->next;
		old->next = hook;
	} else {
		hook->next = assembly_load_hook;
		assembly_load_hook = hook;
	}
}

/**
 * mono_install_assembly_load_hook:
 */
void
mono_install_assembly_load_hook (MonoAssemblyLoadFunc func, gpointer user_data)
{
	mono_install_assembly_load_hook_v1 (func, user_data);
}

typedef struct AssemblySearchHook AssemblySearchHook;
struct AssemblySearchHook {
	AssemblySearchHook *next;
	union {
		MonoAssemblySearchFunc v1;
		MonoAssemblySearchFuncV2 v2;
	} func;
	gboolean postload;
	int version;
	gpointer user_data;
};

static AssemblySearchHook *assembly_search_hook = NULL;

static MonoAssembly*
mono_assembly_invoke_search_hook_internal (MonoAssemblyLoadContext *alc, MonoAssembly *requesting, MonoAssemblyName *aname, gboolean postload)
{
	AssemblySearchHook *hook;

	for (hook = assembly_search_hook; hook; hook = hook->next) {
		if (hook->postload == postload) {
			MonoAssembly *ass;
			if (hook->version == 1) {
				ass = hook->func.v1 (aname, hook->user_data);
			} else {
				ERROR_DECL (hook_error);
				g_assert (hook->version == 2);
				ass = hook->func.v2 (alc, requesting, aname, postload, hook->user_data, hook_error);
				mono_error_assert_ok (hook_error); /* FIXME: proper error handling */
			}
			if (ass)
				return ass;
		}
	}

	return NULL;
}

/**
 * mono_assembly_invoke_search_hook:
 */
MonoAssembly*
mono_assembly_invoke_search_hook (MonoAssemblyName *aname)
{
	return mono_assembly_invoke_search_hook_internal (NULL, NULL, aname, FALSE);
}

static void
mono_install_assembly_search_hook_internal_v1 (MonoAssemblySearchFunc func, gpointer user_data, gboolean postload)
{
	AssemblySearchHook *hook;
	
	g_return_if_fail (func != NULL);

	hook = g_new0 (AssemblySearchHook, 1);
	hook->version = 1;
	hook->func.v1 = func;
	hook->user_data = user_data;
	hook->postload = postload;
	hook->next = assembly_search_hook;
	assembly_search_hook = hook;
}

void
mono_install_assembly_search_hook_v2 (MonoAssemblySearchFuncV2 func, gpointer user_data, gboolean postload, gboolean append)
{
	if (func == NULL)
		return;

	AssemblySearchHook *hook = g_new0 (AssemblySearchHook, 1);
	hook->version = 2;
	hook->func.v2 = func;
	hook->user_data = user_data;
	hook->postload = postload;

	if (append && assembly_search_hook != NULL) { // If we don't have any installed hooks, append vs prepend is irrelevant
		AssemblySearchHook *old = assembly_search_hook;
		while (old->next != NULL)
			old = old->next;
		old->next = hook;
	} else {
		hook->next = assembly_search_hook;
		assembly_search_hook = hook;
	}
}

/**
 * mono_install_assembly_search_hook:
 */
void
mono_install_assembly_search_hook (MonoAssemblySearchFunc func, gpointer user_data)
{
	mono_install_assembly_search_hook_internal_v1 (func, user_data, FALSE);
}	

/**
 * mono_install_assembly_refonly_search_hook:
 */
void
mono_install_assembly_refonly_search_hook (MonoAssemblySearchFunc func, gpointer user_data)
{
	/* Ignore refonly hooks, they will never flre */
}

/**
 * mono_install_assembly_postload_search_hook:
 */
void
mono_install_assembly_postload_search_hook (MonoAssemblySearchFunc func, gpointer user_data)
{
	mono_install_assembly_search_hook_internal_v1 (func, user_data, TRUE);
}	

void
mono_install_assembly_postload_refonly_search_hook (MonoAssemblySearchFunc func, gpointer user_data)
{
	/* Ignore refonly hooks, they will never flre */
}


typedef struct AssemblyPreLoadHook AssemblyPreLoadHook;
struct AssemblyPreLoadHook {
	AssemblyPreLoadHook *next;
	union {
		MonoAssemblyPreLoadFunc v1; // legacy internal use
		MonoAssemblyPreLoadFuncV2 v2; // current internal use
		MonoAssemblyPreLoadFuncV3 v3; // netcore external use
	} func;
	gpointer user_data;
	gint32 version;
};

static AssemblyPreLoadHook *assembly_preload_hook = NULL;

static MonoAssembly *
invoke_assembly_preload_hook (MonoAssemblyLoadContext *alc, MonoAssemblyName *aname, gchar **apath)
{
	AssemblyPreLoadHook *hook;
	MonoAssembly *assembly;

	for (hook = assembly_preload_hook; hook; hook = hook->next) {
		if (hook->version == 1)
			assembly = hook->func.v1 (aname, apath, hook->user_data);
		else {
			ERROR_DECL (error);
			g_assert (hook->version == 2 || hook->version == 3);
			if (hook->version == 2)
				assembly = hook->func.v2 (alc, aname, apath, hook->user_data, error);
			else { // v3
				/*
				 * For the default ALC, pass the globally known gchandle (since it's never collectible, it's always a strong handle).
				 * For other ALCs, make a new strong handle that is passed to the caller.
				 * Early at startup, when the default ALC exists, but its managed object doesn't, so the default ALC gchandle points to null.
				 */
				gboolean needs_free = TRUE;
				MonoGCHandle strong_gchandle;
				if (mono_alc_is_default (alc)) {
					needs_free = FALSE;
					strong_gchandle = alc->gchandle;
				} else
					strong_gchandle = mono_gchandle_from_handle (mono_gchandle_get_target_handle (alc->gchandle), TRUE);
				assembly = hook->func.v3 (strong_gchandle, aname, apath, hook->user_data, error);
				if (needs_free)
					mono_gchandle_free_internal (strong_gchandle);
			}
			/* TODO: propagage error out to callers */
			mono_error_assert_ok (error);
		}
		if (assembly != NULL)
			return assembly;
	}

	return NULL;
}

/**
 * mono_install_assembly_preload_hook:
 */
void
mono_install_assembly_preload_hook (MonoAssemblyPreLoadFunc func, gpointer user_data)
{
	AssemblyPreLoadHook *hook;
	
	g_return_if_fail (func != NULL);

	hook = g_new0 (AssemblyPreLoadHook, 1);
	hook->version = 1;
	hook->func.v1 = func;
	hook->user_data = user_data;
	hook->next = assembly_preload_hook;
	assembly_preload_hook = hook;
}

/**
 * mono_install_assembly_refonly_preload_hook:
 */
void
mono_install_assembly_refonly_preload_hook (MonoAssemblyPreLoadFunc func, gpointer user_data)
{
	/* Ignore refonly hooks, they never fire */
}

void
mono_install_assembly_preload_hook_v2 (MonoAssemblyPreLoadFuncV2 func, gpointer user_data, gboolean append)
{
	AssemblyPreLoadHook *hook;

	g_return_if_fail (func != NULL);

	AssemblyPreLoadHook **hooks = &assembly_preload_hook;

	hook = g_new0 (AssemblyPreLoadHook, 1);
	hook->version = 2;
	hook->func.v2 = func;
	hook->user_data = user_data;

	if (append && *hooks != NULL) { // If we don't have any installed hooks, append vs prepend is irrelevant
		AssemblyPreLoadHook *old = *hooks;
		while (old->next != NULL)
			old = old->next;
		old->next = hook;
	} else {
		hook->next = *hooks;
		*hooks = hook;
	}
}

void
mono_install_assembly_preload_hook_v3 (MonoAssemblyPreLoadFuncV3 func, gpointer user_data, gboolean append)
{
	AssemblyPreLoadHook *hook;

	g_return_if_fail (func != NULL);

	hook = g_new0 (AssemblyPreLoadHook, 1);
	hook->version = 3;
	hook->func.v3 = func;
	hook->user_data = user_data;

	if (append && assembly_preload_hook != NULL) {
		AssemblyPreLoadHook *old = assembly_preload_hook;
		while (old->next != NULL)
			old = old->next;
		old->next = hook;
	} else {
		hook->next = assembly_preload_hook;
		assembly_preload_hook = hook;
	}
}

typedef struct AssemblyAsmCtxFromPathHook AssemblyAsmCtxFromPathHook;
struct AssemblyAsmCtxFromPathHook {
	AssemblyAsmCtxFromPathHook *next;
	MonoAssemblyAsmCtxFromPathFunc func;
	gpointer user_data;
};

static AssemblyAsmCtxFromPathHook *assembly_asmctx_from_path_hook = NULL;

/**
 * mono_install_assembly_asmctx_from_path_hook:
 *
 * \param func Hook function
 * \param user_data User data
 *
 * Installs a hook function \p func that when called with an absolute path name
 * returns \c TRUE and writes to \c out_asmctx if an assembly that name would
 * be found by that asmctx.  The hooks are called in the order from most
 * recently added to oldest.
 *
 */
void
mono_install_assembly_asmctx_from_path_hook (MonoAssemblyAsmCtxFromPathFunc func, gpointer user_data)
{
	g_return_if_fail (func != NULL);

	AssemblyAsmCtxFromPathHook *hook = g_new0 (AssemblyAsmCtxFromPathHook, 1);
	hook->func = func;
	hook->user_data = user_data;
	hook->next = assembly_asmctx_from_path_hook;
	assembly_asmctx_from_path_hook = hook;
}

/**
 * mono_assembly_invoke_asmctx_from_path_hook:
 *
 * \param absfname absolute path name
 * \param requesting_assembly the \c MonoAssembly that requested the load, may be \c NULL
 * \param out_asmctx assembly context kind, written on output
 *
 * Invokes hooks to find the assembly context that would have searched for the
 * given assembly name.  Writes to \p out_asmctx the assembly context kind from
 * the first hook to return \c TRUE.  \returns \c TRUE if any hook wrote to \p
 * out_asmctx, or \c FALSE otherwise.
 */
static gboolean
assembly_invoke_asmctx_from_path_hook (const char *absfname, MonoAssembly *requesting_assembly, MonoAssemblyContextKind *out_asmctx)
{
	g_assert (absfname);
	g_assert (out_asmctx);
	AssemblyAsmCtxFromPathHook *hook;

	for (hook = assembly_asmctx_from_path_hook; hook; hook = hook->next) {
		*out_asmctx = MONO_ASMCTX_INDIVIDUAL;
		if (hook->func (absfname, requesting_assembly, hook->user_data, out_asmctx))
			return TRUE;
	}
	return FALSE;
}

static gchar *
absolute_dir (const gchar *filename)
{
	gchar *cwd;
	gchar *mixed;
	gchar **parts;
	gchar *part;
	GList *list, *tmp;
	GString *result;
	gchar *res;
	gint i;

	if (g_path_is_absolute (filename)) {
		part = g_path_get_dirname (filename);
		res = g_strconcat (part, G_DIR_SEPARATOR_S, (const char*)NULL);
		g_free (part);
		return res;
	}

	cwd = g_get_current_dir ();
	mixed = g_build_filename (cwd, filename, (const char*)NULL);
	parts = g_strsplit (mixed, G_DIR_SEPARATOR_S, 0);
	g_free (mixed);
	g_free (cwd);

	list = NULL;
	for (i = 0; (part = parts [i]) != NULL; i++) {
		if (!strcmp (part, "."))
			continue;

		if (!strcmp (part, "..")) {
			if (list && list->next) /* Don't remove root */
				list = g_list_delete_link (list, list);
		} else {
			list = g_list_prepend (list, part);
		}
	}

	result = g_string_new ("");
	list = g_list_reverse (list);

	/* Ignores last data pointer, which should be the filename */
	for (tmp = list; tmp && tmp->next != NULL; tmp = tmp->next){
		if (tmp->data)
			g_string_append_printf (result, "%s%c", (char *) tmp->data,
								G_DIR_SEPARATOR);
	}

	res = result->str;
	g_string_free (result, FALSE);
	g_list_free (list);
	g_strfreev (parts);
	if (*res == '\0') {
		g_free (res);
		return g_strdup (".");
	}

	return res;
}

static MonoImage *
open_from_bundle_internal (MonoAssemblyLoadContext *alc, const char *filename, MonoImageOpenStatus *status, gboolean is_satellite)
{
	if (!bundles)
		return NULL;

	MonoImage *image = NULL;
	char *name = is_satellite ? g_strdup (filename) : g_path_get_basename (filename);
	for (int i = 0; !image && bundles [i]; ++i) {
		if (strcmp (bundles [i]->name, name) == 0) {
			// Since bundled images don't exist on disk, don't give them a legit filename
			image = mono_image_open_from_data_internal (alc, (char*)bundles [i]->data, bundles [i]->size, FALSE, status, FALSE, name, NULL);
			break;
		}
	}

	g_free (name);
	return image;
}

static MonoImage *
open_from_satellite_bundle (MonoAssemblyLoadContext *alc, const char *filename, MonoImageOpenStatus *status, const char *culture)
{
	if (!satellite_bundles)
		return NULL;

	MonoImage *image = NULL;
	char *name = g_strdup (filename);

	for (int i = 0; !image && satellite_bundles [i]; ++i) {
		if (strcmp (satellite_bundles [i]->name, name) == 0 && strcmp (satellite_bundles [i]->culture, culture) == 0) {
			char *bundle_name = g_strconcat (culture, "/", name, (const char *)NULL);
			image = mono_image_open_from_data_internal (alc, (char *)satellite_bundles [i]->data, satellite_bundles [i]->size, FALSE, status, FALSE, bundle_name, NULL);
			g_free (bundle_name);
			break;
		}
	}

	g_free (name);
	return image;
}

/** 
 * mono_assembly_open_from_bundle:
 * \param filename Filename requested
 * \param status return status code
 *
 * This routine tries to open the assembly specified by \p filename from the
 * defined bundles, if found, returns the MonoImage for it, if not found
 * returns NULL
 */
MonoImage *
mono_assembly_open_from_bundle (MonoAssemblyLoadContext *alc, const char *filename, MonoImageOpenStatus *status, const char *culture)
{
	/*
	 * we do a very simple search for bundled assemblies: it's not a general
	 * purpose assembly loading mechanism.
	 */
	MonoImage *image = NULL;
	gboolean is_satellite = culture && culture [0] != 0;

	if (is_satellite)
		image = open_from_satellite_bundle (alc, filename, status, culture);
	else
		image = open_from_bundle_internal (alc, filename, status, FALSE);

	if (image) {
		mono_image_addref (image);
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Assembly Loader loaded assembly from bundle: '%s'.", filename);
	}
	return image;
}

/**
 * mono_assembly_open_full:
 * \param filename the file to load
 * \param status return status code 
 * \param refonly Whether this assembly is being opened in "reflection-only" mode.
 *
 * This loads an assembly from the specified \p filename. The \p filename allows
 * a local URL (starting with a \c file:// prefix).  If a file prefix is used, the
 * filename is interpreted as a URL, and the filename is URL-decoded.   Otherwise the file
 * is treated as a local path.
 *
 * First, an attempt is made to load the assembly from the bundled executable (for those
 * deployments that have been done with the \c mkbundle tool or for scenarios where the
 * assembly has been registered as an embedded assembly).   If this is not the case, then
 * the assembly is loaded from disk using `api:mono_image_open_full`.
 *
 * If \p refonly is set to true, then the assembly is loaded purely for inspection with
 * the \c System.Reflection API.
 *
 * \returns NULL on error, with the \p status set to an error code, or a pointer
 * to the assembly.
 */
MonoAssembly *
mono_assembly_open_full (const char *filename, MonoImageOpenStatus *status, gboolean refonly)
{
	if (refonly) {
		if (status)
			*status = MONO_IMAGE_IMAGE_INVALID;
		return NULL;
	}
	MonoAssembly *res;
	MONO_ENTER_GC_UNSAFE;
	MonoAssemblyOpenRequest req;
	mono_assembly_request_prepare_open (&req,
	                               MONO_ASMCTX_DEFAULT,
	                               mono_alc_get_default ());
	res = mono_assembly_request_open (filename, &req, status);
	MONO_EXIT_GC_UNSAFE;
	return res;
}

static gboolean
assembly_loadfrom_asmctx_from_path (const char *filename, MonoAssembly *requesting_assembly,
				    gpointer user_data, MonoAssemblyContextKind *out_asmctx)
{
	if (requesting_assembly && mono_asmctx_get_kind (&requesting_assembly->context) == MONO_ASMCTX_LOADFROM) {
		if (mono_path_filename_in_basedir (filename, requesting_assembly->basedir)) {
			*out_asmctx = MONO_ASMCTX_LOADFROM;
			return TRUE;
		}
	}
	return FALSE;
}

MonoAssembly *
mono_assembly_request_open (const char *filename, const MonoAssemblyOpenRequest *open_req,
			    MonoImageOpenStatus *status)
{
	MonoImage *image;
	MonoAssembly *ass;
	MonoImageOpenStatus def_status;
	gchar *fname;
	gboolean loaded_from_bundle;

	MonoAssemblyLoadRequest load_req;
	/* we will be overwriting the load request's asmctx.*/
	memcpy (&load_req, &open_req->request, sizeof (load_req));
	
	g_return_val_if_fail (filename != NULL, NULL);

	if (!status)
		status = &def_status;
	*status = MONO_IMAGE_OK;

	fname = g_strdup (filename);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY,
			"Assembly Loader probing location: '%s'.", fname);

	{
		MonoAssemblyContextKind out_asmctx;
		/* If the path belongs to the appdomain base dir or the
		 * base dir of the requesting assembly, load the
		 * assembly in the corresponding asmctx.
		 */
		if (assembly_invoke_asmctx_from_path_hook (fname, open_req->requesting_assembly, &out_asmctx))
			load_req.asmctx = out_asmctx;
	}
	
	image = NULL;

	/* for LoadFrom(string), LoadFile(string) and Load(byte[]), allow them
	 * to load problematic images.  Not sure if ReflectionOnlyLoad(string)
	 * and ReflectionOnlyLoadFrom(string) should also be allowed - let's
	 * say, yes.
	 */
	const gboolean load_from_context = load_req.asmctx == MONO_ASMCTX_LOADFROM || load_req.asmctx == MONO_ASMCTX_INDIVIDUAL;

	// If VM built with mkbundle
	loaded_from_bundle = FALSE;
	if (bundles != NULL || satellite_bundles != NULL) {
		/* We don't know the culture of the filename we're loading here, so this call is not culture aware. */
		image = mono_assembly_open_from_bundle (load_req.alc, fname, status, NULL);
		loaded_from_bundle = image != NULL;
	}

	if (!image)
		image = mono_image_open_a_lot (load_req.alc, fname, status, load_from_context);

	if (!image){
		if (*status == MONO_IMAGE_OK)
			*status = MONO_IMAGE_ERROR_ERRNO;
		g_free (fname);
		return NULL;
	}

	if (image->assembly) {
		/* We want to return the MonoAssembly that's already loaded,
		 * but if we're using the strict assembly loader, we also need
		 * to check that the previously loaded assembly matches the
		 * predicate.  It could be that we previously loaded a
		 * different version that happens to have the filename that
		 * we're currently probing. */
		if (mono_loader_get_strict_assembly_name_check () &&
		    load_req.predicate && !load_req.predicate (image->assembly, load_req.predicate_ud)) {
			mono_image_close (image);
			g_free (fname);
			return NULL;
		} else {
			/* Already loaded by another appdomain */
			mono_assembly_invoke_load_hook_internal (load_req.alc, image->assembly);
			mono_image_close (image);
			g_free (fname);
			return image->assembly;
		}
	}

	ass = mono_assembly_request_load_from (image, fname, &load_req, status);

	if (ass) {
		if (!loaded_from_bundle)
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY,
				"Assembly Loader loaded assembly from location: '%s'.", filename);
	}

	/* Clear the reference added by mono_image_open */
	mono_image_close (image);
	
	g_free (fname);

	return ass;
}

static void
free_assembly_name_item (gpointer val, gpointer user_data)
{
	mono_assembly_name_free_internal ((MonoAssemblyName *)val);
	g_free (val);
}

/**
 * mono_assembly_load_friends:
 * \param ass an assembly
 *
 * Load the list of friend assemblies that are allowed to access
 * the assembly's internal types and members. They are stored as assembly
 * names in custom attributes.
 *
 * This is an internal method, we need this because when we load mscorlib
 * we do not have the internals visible cattr loaded yet,
 * so we need to load these after we initialize the runtime. 
 *
 * LOCKING: Acquires the assemblies lock plus the loader lock.
 */
void
mono_assembly_load_friends (MonoAssembly* ass)
{
	ERROR_DECL (error);
	int i;
	MonoCustomAttrInfo* attrs;

	if (ass->friend_assembly_names_inited)
		return;

	attrs = mono_custom_attrs_from_assembly_checked (ass, FALSE, error);
	mono_error_assert_ok (error);
	if (!attrs) {
		mono_assemblies_lock ();
		ass->friend_assembly_names_inited = TRUE;
		mono_assemblies_unlock ();
		return;
	}

	mono_assemblies_lock ();
	if (ass->friend_assembly_names_inited) {
		mono_assemblies_unlock ();
		return;
	}
	mono_assemblies_unlock ();

	GSList *visible_list = NULL;
	GSList *ignores_list = NULL;

	/* 
	 * We build the list outside the assemblies lock, the worse that can happen
	 * is that we'll need to free the allocated list.
	 */
	for (i = 0; i < attrs->num_attrs; ++i) {
		MonoCustomAttrEntry *attr = &attrs->attrs [i];
		MonoAssemblyName *aname;
		const gchar *data;
		uint32_t data_length;
		gchar *data_with_terminator;
		/* Do some sanity checking */
		if (!attr->ctor)
			continue;
		gboolean has_visible = FALSE;
		gboolean has_ignores = FALSE;
		has_visible = attr->ctor->klass == mono_class_try_get_internals_visible_class ();
		/* IgnoresAccessChecksToAttribute is dynamically generated, so it's not necessarily in CoreLib */
		/* FIXME: should we only check for it in dynamic modules? */
		has_ignores = (!strcmp ("IgnoresAccessChecksToAttribute", m_class_get_name (attr->ctor->klass)) &&
			       !strcmp ("System.Runtime.CompilerServices", m_class_get_name_space (attr->ctor->klass)));
		if (!has_visible && !has_ignores)
			continue;
		if (attr->data_size < 4)
			continue;
		data = (const char*)attr->data;
		/* 0xFF means null string, see custom attr format */
		if (data [0] != 1 || data [1] != 0 || (data [2] & 0xFF) == 0xFF)
			continue;
		data_length = mono_metadata_decode_value (data + 2, &data);
		data_with_terminator = (char *)g_memdup (data, data_length + 1);
		data_with_terminator[data_length] = 0;
		aname = g_new0 (MonoAssemblyName, 1);
		/*g_print ("friend ass: %s\n", data);*/
		if (mono_assembly_name_parse_full (data_with_terminator, aname, TRUE, NULL, NULL)) {
			if (has_visible)
				visible_list = g_slist_prepend (visible_list, aname);
			if (has_ignores)
				ignores_list = g_slist_prepend (ignores_list, aname);
		} else {
			g_free (aname);
		}
		g_free (data_with_terminator);
	}
	mono_custom_attrs_free (attrs);

	mono_assemblies_lock ();
	if (ass->friend_assembly_names_inited) {
		mono_assemblies_unlock ();
		g_slist_foreach (visible_list, free_assembly_name_item, NULL);
		g_slist_free (visible_list);
		g_slist_foreach (ignores_list, free_assembly_name_item, NULL);
		g_slist_free (ignores_list);
		return;
	}
	ass->friend_assembly_names = visible_list;
	ass->ignores_checks_assembly_names = ignores_list;

	/* Because of the double checked locking pattern above */
	mono_memory_barrier ();
	ass->friend_assembly_names_inited = TRUE;
	mono_assemblies_unlock ();
}

struct HasReferenceAssemblyAttributeIterData {
	gboolean has_attr;
};

static gboolean
has_reference_assembly_attribute_iterator (MonoImage *image, guint32 typeref_scope_token, const char *nspace, const char *name, guint32 method_token, gpointer user_data)
{
	gboolean stop_scanning = FALSE;
	struct HasReferenceAssemblyAttributeIterData *iter_data = (struct HasReferenceAssemblyAttributeIterData*)user_data;

	if (!strcmp (name, "ReferenceAssemblyAttribute") && !strcmp (nspace, "System.Runtime.CompilerServices")) {
		/* Note we don't check the assembly name, same as coreCLR. */
		iter_data->has_attr = TRUE;
		stop_scanning = TRUE;
	}

	return stop_scanning;
}

/**
 * mono_assembly_has_reference_assembly_attribute:
 * \param assembly a MonoAssembly
 * \param error set on error.
 *
 * \returns TRUE if \p assembly has the \c System.Runtime.CompilerServices.ReferenceAssemblyAttribute set.
 * On error returns FALSE and sets \p error.
 */
gboolean
mono_assembly_has_reference_assembly_attribute (MonoAssembly *assembly, MonoError *error)
{
	g_assert (assembly && assembly->image);
	/* .NET Framework appears to ignore the attribute on dynamic
	 * assemblies, so don't call this function for dynamic assemblies. */
	g_assert (!image_is_dynamic (assembly->image));
	error_init (error);

	/*
	 * This might be called during assembly loading, so do everything using the low-level
	 * metadata APIs.
	 */

	struct HasReferenceAssemblyAttributeIterData iter_data = { FALSE };

	mono_assembly_metadata_foreach_custom_attr (assembly, &has_reference_assembly_attribute_iterator, &iter_data);

	return iter_data.has_attr;
}

/**
 * mono_assembly_open:
 * \param filename Opens the assembly pointed out by this name
 * \param status return status code
 *
 * This loads an assembly from the specified \p filename. The \p filename allows
 * a local URL (starting with a \c file:// prefix).  If a file prefix is used, the
 * filename is interpreted as a URL, and the filename is URL-decoded.   Otherwise the file
 * is treated as a local path.
 *
 * First, an attempt is made to load the assembly from the bundled executable (for those
 * deployments that have been done with the \c mkbundle tool or for scenarios where the
 * assembly has been registered as an embedded assembly).   If this is not the case, then
 * the assembly is loaded from disk using `api:mono_image_open_full`.
 *
 * \returns a pointer to the \c MonoAssembly if \p filename contains a valid
 * assembly or NULL on error.  Details about the error are stored in the
 * \p status variable.
 */
MonoAssembly *
mono_assembly_open (const char *filename, MonoImageOpenStatus *status)
{
	MonoAssembly *res;
	MONO_ENTER_GC_UNSAFE;
	MonoAssemblyOpenRequest req;
	mono_assembly_request_prepare_open (&req, MONO_ASMCTX_DEFAULT, mono_alc_get_default ());
	res = mono_assembly_request_open (filename, &req, status);
	MONO_EXIT_GC_UNSAFE;
	return res;
}

/**
 * mono_assembly_load_from_full:
 * \param image Image to load the assembly from
 * \param fname assembly name to associate with the assembly
 * \param status returns the status condition
 * \param refonly Whether this assembly is being opened in "reflection-only" mode.
 *
 * If the provided \p image has an assembly reference, it will process the given
 * image as an assembly with the given name.
 *
 * Most likely you want to use the `api:mono_assembly_load_full` method instead.
 *
 * Returns: A valid pointer to a \c MonoAssembly* on success and the \p status will be
 * set to \c MONO_IMAGE_OK;  or NULL on error.
 *
 * If there is an error loading the assembly the \p status will indicate the
 * reason with \p status being set to \c MONO_IMAGE_INVALID if the
 * image did not contain an assembly reference table.
 */
MonoAssembly *
mono_assembly_load_from_full (MonoImage *image, const char*fname, 
			      MonoImageOpenStatus *status, gboolean refonly)
{
	if (refonly) {
		if (status)
			*status = MONO_IMAGE_IMAGE_INVALID;
		return NULL;
	}
	MonoAssembly *res;
	MONO_ENTER_GC_UNSAFE;
	MonoAssemblyLoadRequest req;
	MonoImageOpenStatus def_status;
	if (!status)
		status = &def_status;
	mono_assembly_request_prepare_load (&req, MONO_ASMCTX_DEFAULT, mono_alc_get_default ());
	res = mono_assembly_request_load_from (image, fname, &req, status);
	MONO_EXIT_GC_UNSAFE;
	return res;
}

MonoAssembly *
mono_assembly_request_load_from (MonoImage *image, const char *fname,
				 const MonoAssemblyLoadRequest *req,
				   MonoImageOpenStatus *status)
{
	MonoAssemblyContextKind asmctx;
	MonoAssemblyCandidatePredicate predicate;
	gpointer user_data;

	MonoAssembly *ass, *ass2;
	char *base_dir;

	g_assert (status != NULL);

	asmctx = req->asmctx;
	predicate = req->predicate;
	user_data = req->predicate_ud;

	if (!table_info_get_rows (&image->tables [MONO_TABLE_ASSEMBLY])) {
		/* 'image' doesn't have a manifest -- maybe someone is trying to Assembly.Load a .netmodule */
		*status = MONO_IMAGE_IMAGE_INVALID;
		return NULL;
	}

#if defined (HOST_WIN32)
	{
		gchar *tmp_fn;
		int i;

		tmp_fn = g_strdup (fname);
		for (i = strlen (tmp_fn) - 1; i >= 0; i--) {
			if (tmp_fn [i] == '/')
				tmp_fn [i] = '\\';
		}

		base_dir = absolute_dir (tmp_fn);
		g_free (tmp_fn);
	}
#else
	base_dir = absolute_dir (fname);
#endif

	/*
	 * Create assembly struct, and enter it into the assembly cache
	 */
	ass = g_new0 (MonoAssembly, 1);
	ass->basedir = base_dir;
	ass->context.kind = asmctx;
	ass->image = image;

	MONO_PROFILER_RAISE (assembly_loading, (ass));

	mono_assembly_fill_assembly_name (image, &ass->aname);

	if (mono_defaults.corlib && strcmp (ass->aname.name, MONO_ASSEMBLY_CORLIB_NAME) == 0) {
		// MS.NET doesn't support loading other mscorlibs
		g_free (ass);
		g_free (base_dir);
		mono_image_addref (mono_defaults.corlib);
		*status = MONO_IMAGE_OK;
		return mono_defaults.corlib->assembly;
	}

	/* Add a non-temporary reference because of ass->image */
	mono_image_addref (image);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Image addref %s[%p] (asmctx %s) -> %s[%p]: %d", ass->aname.name, ass, mono_asmctx_get_name (&ass->context), image->name, image, image->ref_count);

	/* 
	 * The load hooks might take locks so we can't call them while holding the
	 * assemblies lock.
	 */
	if (ass->aname.name && asmctx != MONO_ASMCTX_INDIVIDUAL) {
		/* FIXME: I think individual context should probably also look for an existing MonoAssembly here, we just need to pass the asmctx to the search hook so that it does a filename match (I guess?) */
		ass2 = mono_assembly_invoke_search_hook_internal (req->alc, NULL, &ass->aname, FALSE);
		if (ass2) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Image %s[%p] reusing existing assembly %s[%p]", ass->aname.name, ass, ass2->aname.name, ass2);
			g_free (ass);
			g_free (base_dir);
			mono_image_close (image);
			*status = MONO_IMAGE_OK;
			return ass2;
		}
	}

	/* We need to check for ReferenceAssemblyAttribute before we
	 * mark the assembly as loaded and before we fire the load
	 * hook. Otherwise mono_domain_fire_assembly_load () in
	 * appdomain.c will cache a mapping from the assembly name to
	 * this image and we won't be able to look for a different
	 * candidate. */

	{
		ERROR_DECL (refasm_error);
		if (mono_assembly_has_reference_assembly_attribute (ass, refasm_error)) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Image for assembly '%s' (%s) has ReferenceAssemblyAttribute, skipping", ass->aname.name, image->name);
			g_free (ass);
			g_free (base_dir);
			mono_image_close (image);
			*status = MONO_IMAGE_IMAGE_INVALID;
			return NULL;
		}
		mono_error_cleanup (refasm_error);
	}

	if (predicate && !predicate (ass, user_data)) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Predicate returned FALSE, skipping '%s' (%s)\n", ass->aname.name, image->name);
		g_free (ass);
		g_free (base_dir);
		mono_image_close (image);
		*status = MONO_IMAGE_IMAGE_INVALID;
		return NULL;
	}

	mono_assemblies_lock ();

	/* If an assembly is loaded into an individual context, always return a
	 * new MonoAssembly, even if another assembly with the same name has
	 * already been loaded.
	 */
	if (image->assembly && asmctx != MONO_ASMCTX_INDIVIDUAL) {
		/*
		 * This means another thread has already loaded the assembly, but not yet
		 * called the load hooks so the search hook can't find the assembly.
		 */
		mono_assemblies_unlock ();
		ass2 = image->assembly;
		g_free (ass);
		g_free (base_dir);
		mono_image_close (image);
		*status = MONO_IMAGE_OK;
		return ass2;
	}

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Prepared to set up assembly '%s' (%s)", ass->aname.name, image->name);

	/* If asmctx is INDIVIDUAL, image->assembly might not be NULL, so don't
	 * overwrite it. */
	if (image->assembly == NULL)
		image->assembly = ass;

	loaded_assemblies = g_list_prepend (loaded_assemblies, ass);
	loaded_assembly_count++;
	mono_assemblies_unlock ();

#ifdef HOST_WIN32
	if (m_image_is_module_handle (image))
		mono_image_fixup_vtable (image);
#endif

	mono_assembly_invoke_load_hook_internal (req->alc, ass);

	MONO_PROFILER_RAISE (assembly_loaded, (ass));
	
	return ass;
}

/**
 * mono_assembly_load_from:
 * \param image Image to load the assembly from
 * \param fname assembly name to associate with the assembly
 * \param status return status code
 *
 * If the provided \p image has an assembly reference, it will process the given
 * image as an assembly with the given name.
 *
 * Most likely you want to use the `api:mono_assembly_load_full` method instead.
 *
 * This is equivalent to calling `api:mono_assembly_load_from_full` with the
 * \p refonly parameter set to FALSE.
 * \returns A valid pointer to a \c MonoAssembly* on success and then \p status will be
 * set to \c MONO_IMAGE_OK; or NULL on error.
 *
 * If there is an error loading the assembly the \p status will indicate the
 * reason with \p status being set to \c MONO_IMAGE_INVALID if the
 * image did not contain an assembly reference table.
 
 */
MonoAssembly *
mono_assembly_load_from (MonoImage *image, const char *fname,
			 MonoImageOpenStatus *status)
{
	MonoAssembly *res;
	MONO_ENTER_GC_UNSAFE;
	MonoAssemblyLoadRequest req;
	MonoImageOpenStatus def_status;
	if (!status)
		status = &def_status;
	mono_assembly_request_prepare_load (&req, MONO_ASMCTX_DEFAULT, mono_alc_get_default ());
	res = mono_assembly_request_load_from (image, fname, &req, status);
	MONO_EXIT_GC_UNSAFE;
	return res;
}

/**
 * mono_assembly_name_free_internal:
 * \param aname assembly name to free
 * 
 * Frees the provided assembly name object.
 * (it does not frees the object itself, only the name members).
 */
void
mono_assembly_name_free_internal (MonoAssemblyName *aname)
{
	MONO_REQ_GC_UNSAFE_MODE;

	if (aname == NULL)
		return;

	g_free ((void *) aname->name);
	g_free ((void *) aname->culture);
	g_free ((void *) aname->hash_value);
	g_free ((guint8*) aname->public_key);
}

static gboolean
parse_public_key (const gchar *key, gchar** pubkey, gboolean *is_ecma)
{
	const gchar *pkey;
	gchar header [16], val, *arr, *endp;
	gint i, j, offset, bitlen, keylen, pkeylen;

	//both pubkey and is_ecma are required arguments
	g_assert (pubkey && is_ecma);

	keylen = strlen (key) >> 1;
	if (keylen < 1)
		return FALSE;

	/* allow the ECMA standard key */
	if (strcmp (key, "00000000000000000400000000000000") == 0) {
		*pubkey = NULL;
		*is_ecma = TRUE;
		return TRUE;
	}
	*is_ecma = FALSE;
	val = g_ascii_xdigit_value (key [0]) << 4;
	val |= g_ascii_xdigit_value (key [1]);
	switch (val) {
		case 0x00:
			if (keylen < 13)
				return FALSE;
			val = g_ascii_xdigit_value (key [24]);
			val |= g_ascii_xdigit_value (key [25]);
			if (val != 0x06)
				return FALSE;
			pkey = key + 24;
			break;
		case 0x06:
			pkey = key;
			break;
		default:
			return FALSE;
	}
		
	/* We need the first 16 bytes
	* to check whether this key is valid or not */
	pkeylen = strlen (pkey) >> 1;
	if (pkeylen < 16)
		return FALSE;
		
	for (i = 0, j = 0; i < 16; i++) {
		header [i] = g_ascii_xdigit_value (pkey [j++]) << 4;
		header [i] |= g_ascii_xdigit_value (pkey [j++]);
	}

	if (header [0] != 0x06 || /* PUBLICKEYBLOB (0x06) */
			header [1] != 0x02 || /* Version (0x02) */
			header [2] != 0x00 || /* Reserved (word) */
			header [3] != 0x00 ||
			(guint)(read32 (header + 8)) != 0x31415352) /* DWORD magic = RSA1 */
		return FALSE;

	/* Based on this length, we _should_ be able to know if the length is right */
	bitlen = read32 (header + 12) >> 3;
	if ((bitlen + 16 + 4) != pkeylen)
		return FALSE;
		
	arr = (gchar *)g_malloc (keylen + 4);
	/* Encode the size of the blob */
	mono_metadata_encode_value (keylen, &arr[0], &endp);
	offset = (gint)(endp-arr);
		
	for (i = offset, j = 0; i < keylen + offset; i++) {
		arr [i] = g_ascii_xdigit_value (key [j++]) << 4;
		arr [i] |= g_ascii_xdigit_value (key [j++]);
	}

	*pubkey = arr;

	return TRUE;
}

static gboolean
build_assembly_name (const char *name, const char *version, const char *culture, const char *token, const char *key, guint32 flags, guint32 arch, MonoAssemblyName *aname, gboolean save_public_key)
{
	gint len;
	gint version_parts;
	gchar *pkeyptr, *encoded, tok [8];

	memset (aname, 0, sizeof (MonoAssemblyName));

	if (version) {
		int parts [4];
		int i;
		int part_len;

		parts [2] = -1;
		parts [3] = -1;
		const char *s = version;
		version_parts = 0;
		for (i = 0; i < 4; ++i) {
			int n = sscanf (s, "%u%n", &parts [i], &part_len);
			if (n != 1)
				return FALSE;
			if (parts [i] < 0 || parts [i] > 65535)
				return FALSE;
			if (i < 2 && parts [i] == 65535)
				return FALSE;
			version_parts ++;
			s += part_len;
			if (s [0] == '\0')
				break;
			if (i < 3) {
				if (s [0] != '.')
					return FALSE;
				s ++;
			}
		}
		if (s [0] != '\0')
			return FALSE;
		if (version_parts < 2 || version_parts > 4)
			return FALSE;
		aname->major = parts [0];
		aname->minor = parts [1];
		if (version_parts >= 3)
			aname->build = parts [2];
		else
			aname->build = -1;
		if (version_parts == 4)
			aname->revision = parts [3];
		else
			aname->revision = -1;
	}
	
	aname->flags = flags;
	aname->arch = arch;
	aname->name = g_strdup (name);
	
	if (culture) {
		if (g_ascii_strcasecmp (culture, "neutral") == 0)
			aname->culture = g_strdup ("");
		else
			aname->culture = g_strdup (culture);
	}
	
	if (token && strncmp (token, "null", 4) != 0) {
		char *lower;

		/* the constant includes the ending NULL, hence the -1 */
		if (strlen (token) != (MONO_PUBLIC_KEY_TOKEN_LENGTH - 1)) {
			mono_assembly_name_free_internal (aname);
			return FALSE;
		}
		lower = g_ascii_strdown (token, MONO_PUBLIC_KEY_TOKEN_LENGTH);
		g_strlcpy ((char*)aname->public_key_token, lower, MONO_PUBLIC_KEY_TOKEN_LENGTH);
		g_free (lower);
	}

	if (key) {
		gboolean is_ecma = FALSE;
		gchar *pkey = NULL;
		if (strcmp (key, "null") == 0 || !parse_public_key (key, &pkey, &is_ecma)) {
			mono_assembly_name_free_internal (aname);
			return FALSE;
		}

		if (is_ecma) {
			g_assert (pkey == NULL);
			aname->public_key = NULL;
			g_strlcpy ((gchar*)aname->public_key_token, "b77a5c561934e089", MONO_PUBLIC_KEY_TOKEN_LENGTH);
			return TRUE;
		}
		
		len = mono_metadata_decode_blob_size ((const gchar *) pkey, (const gchar **) &pkeyptr);
		// We also need to generate the key token
		mono_digest_get_public_token ((guchar*) tok, (guint8*) pkeyptr, len);
		encoded = encode_public_tok ((guchar*) tok, 8);
		g_strlcpy ((gchar*)aname->public_key_token, encoded, MONO_PUBLIC_KEY_TOKEN_LENGTH);
		g_free (encoded);

		if (save_public_key)
			aname->public_key = (guint8*) pkey;
		else
			g_free (pkey);
	}

	return TRUE;
}

static gboolean
split_key_value (const gchar *pair, gchar **key, guint32 *keylen, gchar **value)
{
	char *eqsign = (char*)strchr (pair, '=');
	if (!eqsign) {
		*key = NULL;
		*keylen = 0;
		*value = NULL;
		return FALSE;
	}

	*key = (gchar*)pair;
	*keylen = eqsign - *key;
	while (*keylen > 0 && g_ascii_isspace ((*key) [*keylen - 1]))
		(*keylen)--;
	*value = g_strstrip (eqsign + 1);
	return TRUE;
}

gboolean
mono_assembly_name_parse_full (const char *name, MonoAssemblyName *aname, gboolean save_public_key, gboolean *is_version_defined, gboolean *is_token_defined)
{
	gchar *dllname;
	gchar *dllname_uq;
	gchar *version = NULL;
	gchar *version_uq;
	gchar *culture = NULL;
	gchar *culture_uq;
	gchar *token = NULL;
	gchar *token_uq;
	gchar *key = NULL;
	gchar *key_uq;
	gchar *retargetable = NULL;
	gchar *retargetable_uq;
	gchar *procarch;
	gchar *procarch_uq;
	gboolean res;
	gchar *value, *part_name;
	guint32 part_name_len;
	gchar **parts;
	gchar **tmp;
	gboolean version_defined;
	gboolean token_defined;
	guint32 flags = 0;
	guint32 arch = MONO_PROCESSOR_ARCHITECTURE_NONE;

	if (!is_version_defined)
		is_version_defined = &version_defined;
	*is_version_defined = FALSE;
	if (!is_token_defined)
		is_token_defined = &token_defined;
	*is_token_defined = FALSE;
	
	parts = tmp = g_strsplit (name, ",", 6);
	if (!tmp || !*tmp) {
		goto cleanup_and_fail;
	}

	dllname = g_strstrip (*tmp);
	// Simple name cannot be empty
	if (!*dllname) {
		goto cleanup_and_fail;
	}
	// Characters /, :, and \ not allowed in simple names
	while (*dllname) {
		gchar tmp_char = *dllname;
		if (tmp_char == '/' || tmp_char == ':' || tmp_char == '\\')
			goto cleanup_and_fail;
		dllname++;
	}
	dllname = *tmp;
	
	tmp++;

	while (*tmp) {
		if (!split_key_value (g_strstrip (*tmp), &part_name, &part_name_len, &value))
			goto cleanup_and_fail;

		if (part_name_len == 7 && !g_ascii_strncasecmp (part_name, "Version", part_name_len)) {
			*is_version_defined = TRUE;
			version = value;
			if (strlen (version) == 0) {
				goto cleanup_and_fail;
			}
			tmp++;
			continue;
		}

		if (part_name_len == 7 && !g_ascii_strncasecmp (part_name, "Culture", part_name_len)) {
			culture = value;
			if (strlen (culture) == 0) {
				goto cleanup_and_fail;
			}
			tmp++;
			continue;
		}

		if (part_name_len == 14 && !g_ascii_strncasecmp (part_name, "PublicKeyToken", part_name_len)) {
			*is_token_defined = TRUE;
			token = value;
			if (strlen (token) == 0) {
				goto cleanup_and_fail;
			}
			tmp++;
			continue;
		}

		if (part_name_len == 9 && !g_ascii_strncasecmp (part_name, "PublicKey", part_name_len)) {
			key = value;
			if (strlen (key) == 0) {
				goto cleanup_and_fail;
			}
			tmp++;
			continue;
		}

		if (part_name_len == 12 && !g_ascii_strncasecmp (part_name, "Retargetable", part_name_len)) {
			retargetable = value;
			retargetable_uq = unquote (retargetable);
			if (retargetable_uq != NULL)
				retargetable = retargetable_uq;

			if (!g_ascii_strcasecmp (retargetable, "yes")) {
				flags |= ASSEMBLYREF_RETARGETABLE_FLAG;
			} else if (g_ascii_strcasecmp (retargetable, "no")) {
				g_free (retargetable_uq);
				goto cleanup_and_fail;
			}

			g_free (retargetable_uq);
			tmp++;
			continue;
		}

		if (part_name_len == 21 && !g_ascii_strncasecmp (part_name, "ProcessorArchitecture", part_name_len)) {
			procarch = value;
			procarch_uq = unquote (procarch);
			if (procarch_uq != NULL)
				procarch = procarch_uq;

			if (!g_ascii_strcasecmp (procarch, "MSIL"))
				arch = MONO_PROCESSOR_ARCHITECTURE_MSIL;
			else if (!g_ascii_strcasecmp (procarch, "X86"))
				arch = MONO_PROCESSOR_ARCHITECTURE_X86;
			else if (!g_ascii_strcasecmp (procarch, "IA64"))
				arch = MONO_PROCESSOR_ARCHITECTURE_IA64;
			else if (!g_ascii_strcasecmp (procarch, "AMD64"))
				arch = MONO_PROCESSOR_ARCHITECTURE_AMD64;
			else if (!g_ascii_strcasecmp (procarch, "ARM"))
				arch = MONO_PROCESSOR_ARCHITECTURE_ARM;
			else {
				g_free (procarch_uq);
				goto cleanup_and_fail;
			}

			flags |= arch << 4;

			g_free (procarch_uq);
			tmp++;
			continue;
		}

		goto cleanup_and_fail;
	}

	/* if retargetable flag is set, then we must have a fully qualified name */
	if (retargetable != NULL && (version == NULL || culture == NULL || (key == NULL && token == NULL))) {
		goto cleanup_and_fail;
	}

	dllname_uq = unquote (dllname);
	version_uq = unquote (version);
	culture_uq = unquote (culture);
	token_uq = unquote (token);
	key_uq = unquote (key);

	res = build_assembly_name (
		dllname_uq == NULL ? dllname : dllname_uq,
		version_uq == NULL ? version : version_uq,
		culture_uq == NULL ? culture : culture_uq,
		token_uq == NULL ? token : token_uq,
		key_uq == NULL ? key : key_uq,
		flags, arch, aname, save_public_key);

	g_free (dllname_uq);
	g_free (version_uq);
	g_free (culture_uq);
	g_free (token_uq);
	g_free (key_uq);

	g_strfreev (parts);
	return res;

cleanup_and_fail:
	g_strfreev (parts);
	return FALSE;
}

static char*
unquote (const char *str)
{
	gint slen;
	const char *end;

	if (str == NULL)
		return NULL;

	slen = strlen (str);
	if (slen < 2)
		return NULL;

	if (*str != '\'' && *str != '\"')
		return NULL;

	end = str + slen - 1;
	if (*str != *end)
		return NULL;

	return g_strndup (str + 1, slen - 2);
}

/**
 * mono_assembly_name_parse:
 * \param name name to parse
 * \param aname the destination assembly name
 * 
 * Parses an assembly qualified type name and assigns the name,
 * version, culture and token to the provided assembly name object.
 *
 * \returns TRUE if the name could be parsed.
 */
gboolean
mono_assembly_name_parse (const char *name, MonoAssemblyName *aname)
{
	return mono_assembly_name_parse_full (name, aname, FALSE, NULL, NULL);
}

/**
 * mono_assembly_name_new:
 * \param name name to parse
 *
 * Allocate a new \c MonoAssemblyName and fill its values from the
 * passed \p name.
 *
 * \returns a newly allocated structure or NULL if there was any failure.
 */
MonoAssemblyName*
mono_assembly_name_new (const char *name)
{
	MonoAssemblyName *result = NULL;
	MONO_ENTER_GC_UNSAFE;
	MonoAssemblyName *aname = g_new0 (MonoAssemblyName, 1);
	if (mono_assembly_name_parse (name, aname))
		result = aname;
	else
		g_free (aname);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

/**
 * mono_assembly_name_get_name:
 */
const char*
mono_assembly_name_get_name (MonoAssemblyName *aname)
{
	const char *result = NULL;
	MONO_ENTER_GC_UNSAFE;
	result = aname->name;
	MONO_EXIT_GC_UNSAFE;
	return result;
}

/**
 * mono_assembly_name_get_culture:
 */
const char*
mono_assembly_name_get_culture (MonoAssemblyName *aname)
{
	const char *result = NULL;
	MONO_ENTER_GC_UNSAFE;
	result = aname->culture;
	MONO_EXIT_GC_UNSAFE;
	return result;
}

/**
 * mono_assembly_name_get_pubkeytoken:
 */
mono_byte*
mono_assembly_name_get_pubkeytoken (MonoAssemblyName *aname)
{
	if (aname->public_key_token [0])
		return aname->public_key_token;
	return NULL;
}

/**
 * mono_assembly_name_get_version:
 */
uint16_t
mono_assembly_name_get_version (MonoAssemblyName *aname, uint16_t *minor, uint16_t *build, uint16_t *revision)
{
	if (minor)
		*minor = aname->minor;
	if (build)
		*build = aname->build;
	if (revision)
		*revision = aname->revision;
	return aname->major;
}

gboolean
mono_assembly_name_culture_is_neutral (const MonoAssemblyName *aname)
{
	return (!aname->culture || aname->culture [0] == 0);
}

/**
 * mono_assembly_load_with_partial_name:
 * \param name an assembly name that is then parsed by `api:mono_assembly_name_parse`.
 * \param status return status code
 *
 * Loads a \c MonoAssembly from a name.  The name is parsed using `api:mono_assembly_name_parse`,
 * so it might contain a qualified type name, version, culture and token.
 *
 * This will load the assembly from the file whose name is derived from the assembly name
 * by appending the \c .dll extension.
 *
 * The assembly is loaded from either one of the extra Global Assembly Caches specified
 * by the extra GAC paths (specified by the \c MONO_GAC_PREFIX environment variable) or
 * if that fails from the GAC.
 *
 * \returns NULL on failure, or a pointer to a \c MonoAssembly on success.
 */
MonoAssembly*
mono_assembly_load_with_partial_name (const char *name, MonoImageOpenStatus *status)
{
	MonoAssembly *result;
	MONO_ENTER_GC_UNSAFE;
	MonoImageOpenStatus def_status;
	if (!status)
		status = &def_status;
	result = mono_assembly_load_with_partial_name_internal (name, mono_alc_get_default (), status);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

MonoAssembly*
mono_assembly_load_with_partial_name_internal (const char *name, MonoAssemblyLoadContext *alc, MonoImageOpenStatus *status)
{
	ERROR_DECL (error);
	MonoAssembly *res;
	MonoAssemblyName *aname, base_name;
	MonoAssemblyName mapped_aname;

	MONO_REQ_GC_UNSAFE_MODE;

	g_assert (status != NULL);

	memset (&base_name, 0, sizeof (MonoAssemblyName));
	aname = &base_name;

	if (!mono_assembly_name_parse (name, aname))
		return NULL;

	/* 
	 * If no specific version has been requested, make sure we load the
	 * correct version for system assemblies.
	 */ 
	if ((aname->major | aname->minor | aname->build | aname->revision) == 0)
		aname = mono_assembly_remap_version (aname, &mapped_aname);
	
	res = mono_assembly_loaded_internal (alc, aname);
	if (res) {
		mono_assembly_name_free_internal (aname);
		return res;
	}

	res = invoke_assembly_preload_hook (alc, aname, assemblies_path);
	if (res) {
		mono_assembly_name_free_internal (aname);
		return res;
	}

	mono_assembly_name_free_internal (aname);

	if (!res) {
		res = mono_try_assembly_resolve (alc, name, NULL, error);
		if (!is_ok (error)) {
			mono_error_cleanup (error);
			if (*status == MONO_IMAGE_OK)
				*status = MONO_IMAGE_IMAGE_INVALID;
		}
	}

	return res;
}

MonoAssembly*
mono_assembly_load_corlib (MonoImageOpenStatus *status)
{
	MonoAssemblyName *aname;
	MonoAssemblyOpenRequest req;
	mono_assembly_request_prepare_open (&req, MONO_ASMCTX_DEFAULT, mono_alc_get_default ());

	if (corlib) {
		/* g_print ("corlib already loaded\n"); */
		return corlib;
	}

	aname = mono_assembly_name_new (MONO_ASSEMBLY_CORLIB_NAME);
	corlib = invoke_assembly_preload_hook (req.request.alc, aname, NULL);
	/* MonoCore preload hook should know how to find it */
	/* FIXME: AOT compiler comes here without an installed hook. */
	if (!corlib) {
		if (assemblies_path) { // Custom assemblies path set via MONO_PATH or mono_set_assemblies_path
			char *corlib_name = g_strdup_printf ("%s.dll", MONO_ASSEMBLY_CORLIB_NAME);
			corlib = load_in_path (corlib_name, (const char**)assemblies_path, &req, status);
		}
	}
	if (!corlib) {
		/* Maybe its in a bundle */
		char *corlib_name = g_strdup_printf ("%s.dll", MONO_ASSEMBLY_CORLIB_NAME);
		corlib = mono_assembly_request_open (corlib_name, &req, status);
	}
	g_assert (corlib);
		
	return corlib;
}

gboolean
mono_assembly_candidate_predicate_sn_same_name (MonoAssembly *candidate, gpointer ud)
{
	MonoAssemblyName *wanted_name = (MonoAssemblyName*)ud;
	MonoAssemblyName *candidate_name = &candidate->aname;

	g_assert (wanted_name != NULL);
	g_assert (candidate_name != NULL);

	if (mono_trace_is_traced (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY)) {
		char * s = mono_stringify_assembly_name (wanted_name);
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Predicate: wanted = %s", s);
		g_free (s);
		s = mono_stringify_assembly_name (candidate_name);
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Predicate: candidate = %s", s);
		g_free (s);
	}

	return mono_assembly_check_name_match (wanted_name, candidate_name);
}

gboolean
mono_assembly_check_name_match (MonoAssemblyName *wanted_name, MonoAssemblyName *candidate_name)
{
	gboolean result = mono_assembly_names_equal_flags (wanted_name, candidate_name, MONO_ANAME_EQ_IGNORE_VERSION | MONO_ANAME_EQ_IGNORE_PUBKEY);
	if (result && assembly_names_compare_versions (wanted_name, candidate_name, -1) > 0)
		result = FALSE;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Predicate: candidate and wanted names %s",
		    result ? "match, returning TRUE" : "don't match, returning FALSE");
	return result;

}

MonoAssembly*
mono_assembly_request_byname (MonoAssemblyName *aname, const MonoAssemblyByNameRequest *req, MonoImageOpenStatus *status)
{
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Request to load %s in alc %p", aname->name, (gpointer)req->request.alc);
	MonoAssembly *result;
	if (status)
		*status = MONO_IMAGE_OK;
	result = netcore_load_reference (aname, req->request.alc, req->requesting_assembly, !req->no_postload_search);
	return result;
}

MonoAssembly *
mono_assembly_load_full_alc (MonoGCHandle alc_gchandle, MonoAssemblyName *aname, const char *basedir, MonoImageOpenStatus *status)
{
	MonoAssembly *res;
	MONO_ENTER_GC_UNSAFE;
	MonoAssemblyByNameRequest req;
	MonoAssemblyLoadContext *alc = mono_alc_from_gchandle (alc_gchandle);

	mono_assembly_request_prepare_byname (&req, MONO_ASMCTX_DEFAULT, alc);
	req.requesting_assembly = NULL;
	req.basedir = basedir;
	res = mono_assembly_request_byname (aname, &req, status);
	MONO_EXIT_GC_UNSAFE;
	return res;
}

/**
 * mono_assembly_load_full:
 * \param aname A MonoAssemblyName with the assembly name to load.
 * \param basedir A directory to look up the assembly at.
 * \param status a pointer to a MonoImageOpenStatus to return the status of the load operation
 * \param refonly Whether this assembly is being opened in "reflection-only" mode.
 *
 * Loads the assembly referenced by \p aname, if the value of \p basedir is not NULL, it
 * attempts to load the assembly from that directory before probing the standard locations.
 *
 * If the assembly is being opened in reflection-only mode (\p refonly set to TRUE) then no 
 * assembly binding takes place.
 *
 * \returns the assembly referenced by \p aname loaded or NULL on error. On error the
 * value pointed by \p status is updated with an error code.
 */
MonoAssembly*
mono_assembly_load_full (MonoAssemblyName *aname, const char *basedir, MonoImageOpenStatus *status, gboolean refonly)
{
	if (refonly) {
		if (status)
			*status = MONO_IMAGE_IMAGE_INVALID;
		return NULL;
	}
	MonoAssembly *res;
	MONO_ENTER_GC_UNSAFE;
	MonoAssemblyByNameRequest req;
	mono_assembly_request_prepare_byname (&req,
										  MONO_ASMCTX_DEFAULT,
										  mono_alc_get_default ());
	req.requesting_assembly = NULL;
	req.basedir = basedir;
	res = mono_assembly_request_byname (aname, &req, status);
	MONO_EXIT_GC_UNSAFE;
	return res;
}

/**
 * mono_assembly_load:
 * \param aname A MonoAssemblyName with the assembly name to load.
 * \param basedir A directory to look up the assembly at.
 * \param status a pointer to a MonoImageOpenStatus to return the status of the load operation
 *
 * Loads the assembly referenced by \p aname, if the value of \p basedir is not NULL, it
 * attempts to load the assembly from that directory before probing the standard locations.
 *
 * \returns the assembly referenced by \p aname loaded or NULL on error. On error the
 * value pointed by \p status is updated with an error code.
 */
MonoAssembly*
mono_assembly_load (MonoAssemblyName *aname, const char *basedir, MonoImageOpenStatus *status)
{
	MonoAssemblyByNameRequest req;
	mono_assembly_request_prepare_byname (&req, MONO_ASMCTX_DEFAULT, mono_alc_get_default ());
	req.requesting_assembly = NULL;
	req.basedir = basedir;
	return mono_assembly_request_byname (aname, &req, status);
}

/**
 * mono_assembly_loaded_full:
 * \param aname an assembly to look for.
 * \param refonly Whether this assembly is being opened in "reflection-only" mode.
 *
 * This is used to determine if the specified assembly has been loaded
 * \returns NULL If the given \p aname assembly has not been loaded, or a pointer to
 * a \c MonoAssembly that matches the \c MonoAssemblyName specified.
 */
MonoAssembly*
mono_assembly_loaded_full (MonoAssemblyName *aname, gboolean refonly)
{
	if (refonly)
		return NULL;
	MonoAssemblyLoadContext *alc = mono_alc_get_default ();
	return mono_assembly_loaded_internal (alc, aname);
}

MonoAssembly *
mono_assembly_loaded_internal (MonoAssemblyLoadContext *alc, MonoAssemblyName *aname)
{
	MonoAssembly *res;
	MonoAssemblyName mapped_aname;

	aname = mono_assembly_remap_version (aname, &mapped_aname);

	res = mono_assembly_invoke_search_hook_internal (alc, NULL, aname, FALSE);

	return res;
}

/**
 * mono_assembly_loaded:
 * \param aname an assembly to look for.
 *
 * This is used to determine if the specified assembly has been loaded
 
 * \returns NULL If the given \p aname assembly has not been loaded, or a pointer to
 * a \c MonoAssembly that matches the \c MonoAssemblyName specified.
 */
MonoAssembly*
mono_assembly_loaded (MonoAssemblyName *aname)
{
	MonoAssembly *res;
	MONO_ENTER_GC_UNSAFE;
	res = mono_assembly_loaded_internal (mono_alc_get_default (), aname);
	MONO_EXIT_GC_UNSAFE;
	return res;
}

void
mono_assembly_release_gc_roots (MonoAssembly *assembly)
{
	if (assembly == NULL || assembly == REFERENCE_MISSING)
		return;

	if (assembly_is_dynamic (assembly)) {
		int i;
		MonoDynamicImage *dynimg = (MonoDynamicImage *)assembly->image;
		for (i = 0; i < dynimg->image.module_count; ++i)
			mono_dynamic_image_release_gc_roots ((MonoDynamicImage *)dynimg->image.modules [i]);
		mono_dynamic_image_release_gc_roots (dynimg);
	}
}

/*
 * Returns whether mono_assembly_close_finish() must be called as
 * well.  See comment for mono_image_close_except_pools() for why we
 * unload in two steps.
 */
gboolean
mono_assembly_close_except_image_pools (MonoAssembly *assembly)
{
	g_return_val_if_fail (assembly != NULL, FALSE);

	if (assembly == REFERENCE_MISSING)
		return FALSE;

	/* Might be 0 already */
	if (mono_assembly_decref (assembly) > 0)
		return FALSE;

	MONO_PROFILER_RAISE (assembly_unloading, (assembly));

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Unloading assembly %s [%p].", assembly->aname.name, assembly);

	mono_debug_close_image (assembly->image);

	mono_assemblies_lock ();
	loaded_assemblies = g_list_remove (loaded_assemblies, assembly);
	loaded_assembly_count--;
	mono_assemblies_unlock ();

	assembly->image->assembly = NULL;

	if (!mono_image_close_except_pools (assembly->image))
		assembly->image = NULL;

	g_slist_foreach (assembly->friend_assembly_names, free_assembly_name_item, NULL);
	g_slist_foreach (assembly->ignores_checks_assembly_names, free_assembly_name_item, NULL);
	g_slist_free (assembly->friend_assembly_names);
	g_slist_free (assembly->ignores_checks_assembly_names);
	g_free (assembly->basedir);

	MONO_PROFILER_RAISE (assembly_unloaded, (assembly));

	return TRUE;
}

void
mono_assembly_close_finish (MonoAssembly *assembly)
{
	g_assert (assembly && assembly != REFERENCE_MISSING);

	if (assembly->image)
		mono_image_close_finish (assembly->image);

	if (assembly_is_dynamic (assembly)) {
		g_free ((char*)assembly->aname.culture);
	} else {
		g_free (assembly);
	}
}

/**
 * mono_assembly_close:
 * \param assembly the assembly to release.
 *
 * This method releases a reference to the \p assembly.  The assembly is
 * only released when all the outstanding references to it are released.
 */
void
mono_assembly_close (MonoAssembly *assembly)
{
	if (mono_assembly_close_except_image_pools (assembly))
		mono_assembly_close_finish (assembly);
}

/**
 * mono_assembly_load_module:
 */
MonoImage*
mono_assembly_load_module (MonoAssembly *assembly, guint32 idx)
{
	ERROR_DECL (error);
	MonoImage *result = mono_assembly_load_module_checked (assembly, idx, error);
	mono_error_assert_ok (error);
	return result;
}

MONO_API MonoImage*
mono_assembly_load_module_checked (MonoAssembly *assembly, uint32_t idx, MonoError *error)
{
	return mono_image_load_file_for_image_checked (assembly->image, idx, error);
}

/**
 * mono_assembly_foreach:
 * \param func function to invoke for each assembly loaded
 * \param user_data data passed to the callback
 *
 * Invokes the provided \p func callback for each assembly loaded into
 * the runtime.   The first parameter passed to the callback  is the
 * \c MonoAssembly*, and the second parameter is the \p user_data.
 *
 * This is done for all assemblies loaded in the runtime, not just
 * those loaded in the current application domain.
 */
void
mono_assembly_foreach (GFunc func, gpointer user_data)
{
	GList *copy;

	/*
	 * We make a copy of the list to avoid calling the callback inside the 
	 * lock, which could lead to deadlocks.
	 */
	mono_assemblies_lock ();
	copy = g_list_copy (loaded_assemblies);
	mono_assemblies_unlock ();

	g_list_foreach (loaded_assemblies, func, user_data);

	g_list_free (copy);
}

/**
 * mono_assemblies_cleanup:
 *
 * Free all resources used by this module.
 */
void
mono_assemblies_cleanup (void)
{
}

/*
 * Holds the assembly of the application, for
 * System.Diagnostics.Process::MainModule
 */
static MonoAssembly *main_assembly=NULL;

/**
 * mono_assembly_set_main:
 */
void
mono_assembly_set_main (MonoAssembly *assembly)
{
	main_assembly = assembly;
}

/**
 * mono_assembly_get_main:
 *
 * Returns: the assembly for the application, the first assembly that is loaded by the VM
 */
MonoAssembly *
mono_assembly_get_main (void)
{
	return (main_assembly);
}

/**
 * mono_assembly_get_image:
 * \param assembly The assembly to retrieve the image from
 *
 * \returns the \c MonoImage associated with this assembly.
 */
MonoImage*
mono_assembly_get_image (MonoAssembly *assembly)
{
	MonoImage *res;
	MONO_ENTER_GC_UNSAFE;
	res = mono_assembly_get_image_internal (assembly);
	MONO_EXIT_GC_UNSAFE;
	return res;
}

MonoImage*
mono_assembly_get_image_internal (MonoAssembly *assembly)
{
	MONO_REQ_GC_UNSAFE_MODE;
	return assembly->image;
}

/**
 * mono_assembly_get_name:
 * \param assembly The assembly to retrieve the name from
 *
 * The returned name's lifetime is the same as \p assembly's.
 *
 * \returns the \c MonoAssemblyName associated with this assembly.
 */
MonoAssemblyName *
mono_assembly_get_name (MonoAssembly *assembly)
{
	MonoAssemblyName *res;
	MONO_ENTER_GC_UNSAFE;
	res = mono_assembly_get_name_internal (assembly);
	MONO_EXIT_GC_UNSAFE;
	return res;
}

MonoAssemblyName *
mono_assembly_get_name_internal (MonoAssembly *assembly)
{
	MONO_REQ_GC_UNSAFE_MODE;
	return &assembly->aname;
}

/**
 * mono_register_bundled_assemblies:
 */
void
mono_register_bundled_assemblies (const MonoBundledAssembly **assemblies)
{
	bundles = assemblies;
}

/**
 * mono_create_new_bundled_satellite_assembly:
 */
MonoBundledSatelliteAssembly *
mono_create_new_bundled_satellite_assembly (const char *name, const char *culture, const unsigned char *data, unsigned int size)
{
	MonoBundledSatelliteAssembly *satellite_assembly = g_new0 (MonoBundledSatelliteAssembly, 1);
	satellite_assembly->name = strdup (name);
	satellite_assembly->culture = strdup (culture);
	satellite_assembly->data = data;
	satellite_assembly->size = size;
	return satellite_assembly;
}

/**
 * mono_register_bundled_satellite_assemblies:
 */
void
mono_register_bundled_satellite_assemblies (const MonoBundledSatelliteAssembly **assemblies)
{
	satellite_bundles = assemblies;
}

#define MONO_DECLSEC_FORMAT_10		0x3C
#define MONO_DECLSEC_FORMAT_20		0x2E
#define MONO_DECLSEC_FIELD		0x53
#define MONO_DECLSEC_PROPERTY		0x54

#define SKIP_VISIBILITY_XML_ATTRIBUTE ("\"SkipVerification\"")
#define SKIP_VISIBILITY_ATTRIBUTE_NAME ("System.Security.Permissions.SecurityPermissionAttribute")
#define SKIP_VISIBILITY_ATTRIBUTE_SIZE (sizeof (SKIP_VISIBILITY_ATTRIBUTE_NAME) - 1)
#define SKIP_VISIBILITY_PROPERTY_NAME ("SkipVerification")
#define SKIP_VISIBILITY_PROPERTY_SIZE (sizeof (SKIP_VISIBILITY_PROPERTY_NAME) - 1)

static gboolean
mono_assembly_try_decode_skip_verification_param (const char *p, const char **resp, gboolean *abort_decoding)
{
	int len;
	switch (*p++) {
	case MONO_DECLSEC_PROPERTY:
		break;
	case MONO_DECLSEC_FIELD:
	default:
		*abort_decoding = TRUE;
		return FALSE;
		break;
	}

	if (*p++ != MONO_TYPE_BOOLEAN) {
		*abort_decoding = TRUE;
		return FALSE;
	}
		
	/* property name length */
	len = mono_metadata_decode_value (p, &p);

	if (len >= SKIP_VISIBILITY_PROPERTY_SIZE && !memcmp (p, SKIP_VISIBILITY_PROPERTY_NAME, SKIP_VISIBILITY_PROPERTY_SIZE)) {
		p += len;
		return *p;
	}
	p += len + 1;

	*resp = p;
	return FALSE;
}

static gboolean
mono_assembly_try_decode_skip_verification (const char *p, const char *endn)
{
	int i, j, num, len, params_len;

	if (*p == MONO_DECLSEC_FORMAT_10) {
		gsize read, written;
		char *res = g_convert (p, endn - p, "UTF-8", "UTF-16LE", &read, &written, NULL);
		if (res) {
			gboolean found = strstr (res, SKIP_VISIBILITY_XML_ATTRIBUTE) != NULL;
			g_free (res);
			return found;
		}
		return FALSE;
	}
	if (*p++ != MONO_DECLSEC_FORMAT_20)
		return FALSE;

	/* number of encoded permission attributes */
	num = mono_metadata_decode_value (p, &p);
	for (i = 0; i < num; ++i) {
		gboolean is_valid = FALSE;
		gboolean abort_decoding = FALSE;

		/* attribute name length */
		len =  mono_metadata_decode_value (p, &p);

		/* We don't really need to fully decode the type. Comparing the name is enough */
		is_valid = len >= SKIP_VISIBILITY_ATTRIBUTE_SIZE && !memcmp (p, SKIP_VISIBILITY_ATTRIBUTE_NAME, SKIP_VISIBILITY_ATTRIBUTE_SIZE);

		p += len;

		/*size of the params table*/
		params_len =  mono_metadata_decode_value (p, &p);
		if (is_valid) {
			const char *params_end = p + params_len;
			
			/* number of parameters */
			len = mono_metadata_decode_value (p, &p);
	
			for (j = 0; j < len; ++j) {
				if (mono_assembly_try_decode_skip_verification_param (p, &p, &abort_decoding))
					return TRUE;
				if (abort_decoding)
					break;
			}
			p = params_end;
		} else {
			p += params_len;
		}
	}
	
	return FALSE;
}


gboolean
mono_assembly_has_skip_verification (MonoAssembly *assembly)
{
	MonoTableInfo *t;	
	guint32 cols [MONO_DECL_SECURITY_SIZE];
	const char *blob;
	int i, len;

	if (MONO_SECMAN_FLAG_INIT (assembly->skipverification))
		return MONO_SECMAN_FLAG_GET_VALUE (assembly->skipverification);

	t = &assembly->image->tables [MONO_TABLE_DECLSECURITY];

	int rows = table_info_get_rows (t);
	for (i = 0; i < rows; ++i) {
		mono_metadata_decode_row (t, i, cols, MONO_DECL_SECURITY_SIZE);
		if ((cols [MONO_DECL_SECURITY_PARENT] & MONO_HAS_DECL_SECURITY_MASK) != MONO_HAS_DECL_SECURITY_ASSEMBLY)
			continue;
		if (cols [MONO_DECL_SECURITY_ACTION] != SECURITY_ACTION_REQMIN)
			continue;

		blob = mono_metadata_blob_heap (assembly->image, cols [MONO_DECL_SECURITY_PERMISSIONSET]);
		len = mono_metadata_decode_blob_size (blob, &blob);
		if (!len)
			continue;

		if (mono_assembly_try_decode_skip_verification (blob, blob + len)) {
			MONO_SECMAN_FLAG_SET_VALUE (assembly->skipverification, TRUE);
			return TRUE;
		}
	}

	MONO_SECMAN_FLAG_SET_VALUE (assembly->skipverification, FALSE);
	return FALSE;
}

MonoAssemblyContextKind
mono_asmctx_get_kind (const MonoAssemblyContext *ctx)
{
	return ctx->kind;
}

static const char *
mono_asmctx_get_name (const MonoAssemblyContext *asmctx)
{
	static const char* names [] = {
		"DEFAULT",
		"LOADFROM",
		"INDIVIDIUAL",
		"INTERNAL"
	};
	g_assert (asmctx->kind >= 0 && asmctx->kind <= MONO_ASMCTX_LAST);
	return names [asmctx->kind];
}

/**
 * mono_assembly_is_jit_optimizer_disabled:
 *
 * \param assm the assembly
 *
 * Returns TRUE if the System.Diagnostics.DebuggableAttribute has the
 *  DebuggingModes.DisableOptimizations bit set.
 *
 */
gboolean
mono_assembly_is_jit_optimizer_disabled (MonoAssembly *ass)
{
	ERROR_DECL (error);

	g_assert (ass);
	if (ass->jit_optimizer_disabled_inited)
		return ass->jit_optimizer_disabled;

	MonoClass *klass = mono_class_try_get_debuggable_attribute_class ();

	if (!klass) {
		/* Linked away */
		ass->jit_optimizer_disabled = FALSE;
		mono_memory_barrier ();
		ass->jit_optimizer_disabled_inited = TRUE;
		return FALSE;
	}

	gboolean disable_opts = FALSE;
	MonoCustomAttrInfo* attrs = mono_custom_attrs_from_assembly_checked (ass, FALSE, error);
	mono_error_cleanup (error); /* FIXME don't swallow the error */
	if (attrs) {
		for (int i = 0; i < attrs->num_attrs; ++i) {
			MonoCustomAttrEntry *attr = &attrs->attrs [i];
			const gchar *p;
			MonoMethodSignature *sig;

			if (!attr->ctor || attr->ctor->klass != klass)
				continue;
			/* Decode the attribute. See reflection.c */
			p = (const char*)attr->data;
			g_assert (read16 (p) == 0x0001);
			p += 2;

			// FIXME: Support named parameters
			sig = mono_method_signature_internal (attr->ctor);
			MonoClass *param_class;
			if (sig->param_count == 2 && sig->params [0]->type == MONO_TYPE_BOOLEAN && sig->params [1]->type == MONO_TYPE_BOOLEAN) {

				/* Two boolean arguments */
				p ++;
				disable_opts = *p;
			} else if (sig->param_count == 1 &&
				   sig->params[0]->type == MONO_TYPE_VALUETYPE &&
				   (param_class = mono_class_from_mono_type_internal (sig->params[0])) != NULL &&
				   m_class_is_enumtype (param_class) &&
				   !strcmp (m_class_get_name (param_class), "DebuggingModes")) {
				/* System.Diagnostics.DebuggableAttribute+DebuggingModes */
				int32_t flags = read32 (p);
				p += 4;
				disable_opts = (flags & 0x0100) != 0;
			}
		}
		mono_custom_attrs_free (attrs);
	}

	ass->jit_optimizer_disabled = disable_opts;
	mono_memory_barrier ();
	ass->jit_optimizer_disabled_inited = TRUE;

	return disable_opts;

}

guint32
mono_assembly_get_count (void)
{
	return loaded_assembly_count;
}
