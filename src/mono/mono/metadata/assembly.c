/*
 * assembly.c: Routines for loading assemblies.
 * 
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 */
#include <config.h>
#include <stdio.h>
#include <glib.h>
#include <errno.h>
#include <string.h>
#include <stdlib.h>
#include "assembly.h"
#include "image.h"
#include "object-internals.h"
#include <mono/metadata/loader.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/domain-internals.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/mono-debug.h>
#include <mono/io-layer/io-layer.h>
#include <mono/utils/mono-uri.h>
#include <mono/metadata/mono-config.h>
#include <mono/utils/mono-digest.h>
#include <mono/utils/mono-logger-internal.h>
#include <mono/utils/mono-path.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/coree.h>
#include <mono/utils/mono-io-portability.h>
#include <mono/utils/atomic.h>

#ifndef HOST_WIN32
#include <sys/types.h>
#include <unistd.h>
#include <sys/stat.h>
#endif

#ifdef PLATFORM_MACOSX
#include <mach-o/dyld.h>
#endif

/* AssemblyVersionMap: an assembly name and the assembly version set on which it is based */
typedef struct  {
	const char* assembly_name;
	guint8 version_set_index;
} AssemblyVersionMap;

/* the default search path is empty, the first slot is replaced with the computed value */
static const char*
default_path [] = {
	NULL,
	NULL,
	NULL
};

/* Contains the list of directories to be searched for assemblies (MONO_PATH) */
static char **assemblies_path = NULL;

/* Contains the list of directories that point to auxiliary GACs */
static char **extra_gac_paths = NULL;

#ifndef DISABLE_ASSEMBLY_REMAPPING
/* The list of system assemblies what will be remapped to the running
 * runtime version. WARNING: this list must be sorted.
 * The integer number is an index in the MonoRuntimeInfo structure, whose
 * values can be found in domain.c - supported_runtimes. Look there
 * to understand what remapping will be made.
 */
static const AssemblyVersionMap framework_assemblies [] = {
	{"Accessibility", 0},
	{"Commons.Xml.Relaxng", 0},
	{"I18N", 0},
	{"I18N.CJK", 0},
	{"I18N.MidEast", 0},
	{"I18N.Other", 0},
	{"I18N.Rare", 0},
	{"I18N.West", 0},
	{"Microsoft.VisualBasic", 1},
	{"Microsoft.VisualC", 1},
	{"Mono.Cairo", 0},
	{"Mono.CompilerServices.SymbolWriter", 0},
	{"Mono.Data", 0},
	{"Mono.Data.SybaseClient", 0},
	{"Mono.Data.Tds", 0},
	{"Mono.Data.TdsClient", 0},
	{"Mono.GetOptions", 0},
	{"Mono.Http", 0},
	{"Mono.Posix", 0},
	{"Mono.Security", 0},
	{"Mono.Security.Win32", 0},
	{"Mono.Xml.Ext", 0},
	{"Novell.Directory.Ldap", 0},
	{"Npgsql", 0},
	{"PEAPI", 0},
	{"System", 0},
	{"System.ComponentModel.DataAnnotations", 2},
	{"System.Configuration", 0},
	{"System.Configuration.Install", 0},
	{"System.Core", 2},
	{"System.Data", 0},
	{"System.Data.Linq", 2},
	{"System.Data.OracleClient", 0},
	{"System.Data.Services", 2},
	{"System.Data.Services.Client", 2},
	{"System.Data.SqlXml", 0},
	{"System.Design", 0},
	{"System.DirectoryServices", 0},
	{"System.Drawing", 0},
	{"System.Drawing.Design", 0},
	{"System.EnterpriseServices", 0},
	{"System.Management", 0},
	{"System.Messaging", 0},
	{"System.Net", 2},
	{"System.Runtime.Remoting", 0},
	{"System.Runtime.Serialization", 3},
	{"System.Runtime.Serialization.Formatters.Soap", 0},
	{"System.Security", 0},
	{"System.ServiceModel.Web", 2},
	{"System.ServiceProcess", 0},
	{"System.Transactions", 0},
	{"System.Web", 0},
	{"System.Web.Abstractions", 2},
	{"System.Web.DynamicData", 2},
	{"System.Web.Extensions", 2},
	{"System.Web.Mobile", 0},
	{"System.Web.Routing", 2},
	{"System.Web.Services", 0},
	{"System.Windows.Forms", 0},
	{"System.Xml", 0},
	{"System.Xml.Linq", 2},
	{"WindowsBase", 3},
	{"mscorlib", 0}
};
#endif

/*
 * keeps track of loaded assemblies
 */
static GList *loaded_assemblies = NULL;
static MonoAssembly *corlib;

#if defined(__native_client__)

/* On Native Client, allow mscorlib to be loaded from memory  */
/* instead of loaded off disk.  If these are not set, default */
/* mscorlib loading will take place                           */

/* NOTE: If mscorlib data is passed to mono in this way then */
/* it needs to remain allocated during the use of mono.      */

static void *corlibData = NULL;
static size_t corlibSize = 0;

void
mono_set_corlib_data (void *data, size_t size)
{
  corlibData = data;
  corlibSize = size;
}

#endif

/* This protects loaded_assemblies and image->references */
#define mono_assemblies_lock() EnterCriticalSection (&assemblies_mutex)
#define mono_assemblies_unlock() LeaveCriticalSection (&assemblies_mutex)
static CRITICAL_SECTION assemblies_mutex;

/* If defined, points to the bundled assembly information */
const MonoBundledAssembly **bundles;

/* Loaded assembly binding info */
static GSList *loaded_assembly_bindings = NULL;

static MonoAssembly*
mono_assembly_invoke_search_hook_internal (MonoAssemblyName *aname, gboolean refonly, gboolean postload);
static MonoBoolean
mono_assembly_is_in_gac (const gchar *filanem);

static gchar*
encode_public_tok (const guchar *token, gint32 len)
{
	const static gchar allowed [] = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };
	gchar *res;
	int i;

	res = g_malloc (len * 2 + 1);
	for (i = 0; i < len; i++) {
		res [i * 2] = allowed [token [i] >> 4];
		res [i * 2 + 1] = allowed [token [i] & 0xF];
	}
	res [len * 2] = 0;
	return res;
}

/**
 * mono_public_tokens_are_equal:
 * @pubt1: first public key token
 * @pubt2: second public key token
 *
 * Compare two public key tokens and return #TRUE is they are equal and #FALSE
 * otherwise.
 */
gboolean
mono_public_tokens_are_equal (const unsigned char *pubt1, const unsigned char *pubt2)
{
	return memcmp (pubt1, pubt2, 16) == 0;
}

/**
 * mono_set_assemblies_path:
 * @path: list of paths that contain directories where Mono will look for assemblies
 *
 * Use this method to override the standard assembly lookup system and
 * override any assemblies coming from the GAC.  This is the method
 * that supports the MONO_PATH variable.
 *
 * Notice that MONO_PATH and this method are really a very bad idea as
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
	while (*splitted){
		if (**splitted)
			*dest++ = *splitted;
		splitted++;
	}
	*dest = *splitted;
	
	if (g_getenv ("MONO_DEBUG") == NULL)
		return;

	splitted = assemblies_path;
	while (*splitted) {
		if (**splitted && !g_file_test (*splitted, G_FILE_TEST_IS_DIR))
			g_warning ("'%s' in MONO_PATH doesn't exist or has wrong permissions.", *splitted);

		splitted++;
	}
}

/* Native Client can't get this info from an environment variable so */
/* it's passed in to the runtime, or set manually by embedding code. */
#ifdef __native_client__
char* nacl_mono_path = NULL;
#endif

static void
check_path_env (void)
{
	const char* path;
	path = g_getenv ("MONO_PATH");
#ifdef __native_client__
	if (!path)
		path = nacl_mono_path;
#endif
	if (!path || assemblies_path != NULL)
		return;

	mono_set_assemblies_path(path);
}

static void
check_extra_gac_path_env (void) {
	const char *path;
	char **splitted, **dest;
	
	path = g_getenv ("MONO_GAC_PREFIX");
	if (!path)
		return;

	splitted = g_strsplit (path, G_SEARCHPATH_SEPARATOR_S, 1000);
	if (extra_gac_paths)
		g_strfreev (extra_gac_paths);
	extra_gac_paths = dest = splitted;
	while (*splitted){
		if (**splitted)
			*dest++ = *splitted;
		splitted++;
	}
	*dest = *splitted;
	
	if (g_getenv ("MONO_DEBUG") == NULL)
		return;

	while (*splitted) {
		if (**splitted && !g_file_test (*splitted, G_FILE_TEST_IS_DIR))
			g_warning ("'%s' in MONO_GAC_PREFIX doesn't exist or has wrong permissions.", *splitted);

		splitted++;
	}
}

static gboolean
assembly_binding_maps_name (MonoAssemblyBindingInfo *info, MonoAssemblyName *aname)
{
	if (!info || !info->name)
		return FALSE;

	if (strcmp (info->name, aname->name))
		return FALSE;

	if (info->major != aname->major || info->minor != aname->minor)
		return FALSE;

	if ((info->culture != NULL && info->culture [0]) != (aname->culture != NULL && aname->culture [0])) 
		return FALSE;
	
	if (info->culture && aname->culture && strcmp (info->culture, aname->culture))
		return FALSE;
	
	if (!mono_public_tokens_are_equal (info->public_key_token, aname->public_key_token))
		return FALSE;

	return TRUE;
}

static void
mono_assembly_binding_info_free (MonoAssemblyBindingInfo *info)
{
	if (!info)
		return;

	g_free (info->name);
	g_free (info->culture);
}

static void
get_publisher_policy_info (MonoImage *image, MonoAssemblyName *aname, MonoAssemblyBindingInfo *binding_info)
{
	MonoTableInfo *t;
	guint32 cols [MONO_MANIFEST_SIZE];
	const gchar *filename;
	gchar *subpath, *fullpath;

	t = &image->tables [MONO_TABLE_MANIFESTRESOURCE];
	/* MS Impl. accepts policy assemblies with more than
	 * one manifest resource, and only takes the first one */
	if (t->rows < 1) {
		binding_info->is_valid = FALSE;
		return;
	}
	
	mono_metadata_decode_row (t, 0, cols, MONO_MANIFEST_SIZE);
	if ((cols [MONO_MANIFEST_IMPLEMENTATION] & MONO_IMPLEMENTATION_MASK) != MONO_IMPLEMENTATION_FILE) {
		binding_info->is_valid = FALSE;
		return;
	}
	
	filename = mono_metadata_string_heap (image, cols [MONO_MANIFEST_NAME]);
	g_assert (filename != NULL);
	
	subpath = g_path_get_dirname (image->name);
	fullpath = g_build_path (G_DIR_SEPARATOR_S, subpath, filename, NULL);
	mono_config_parse_publisher_policy (fullpath, binding_info);
	g_free (subpath);
	g_free (fullpath);
	
	/* Define the optional elements/attributes before checking */
	if (!binding_info->culture)
		binding_info->culture = g_strdup ("");
	
	/* Check that the most important elements/attributes exist */
	if (!binding_info->name || !binding_info->public_key_token [0] || !binding_info->has_old_version_bottom ||
			!binding_info->has_new_version || !assembly_binding_maps_name (binding_info, aname)) {
		mono_assembly_binding_info_free (binding_info);
		binding_info->is_valid = FALSE;
		return;
	}

	binding_info->is_valid = TRUE;
}

static int
compare_versions (AssemblyVersionSet *v, MonoAssemblyName *aname)
{
	if (v->major > aname->major)
		return 1;
	else if (v->major < aname->major)
		return -1;

	if (v->minor > aname->minor)
		return 1;
	else if (v->minor < aname->minor)
		return -1;

	if (v->build > aname->build)
		return 1;
	else if (v->build < aname->build)
		return -1;

	if (v->revision > aname->revision)
		return 1;
	else if (v->revision < aname->revision)
		return -1;

	return 0;
}

static gboolean
check_policy_versions (MonoAssemblyBindingInfo *info, MonoAssemblyName *name)
{
	if (!info->is_valid)
		return FALSE;
	
	/* If has_old_version_top doesn't exist, we don't have an interval */
	if (!info->has_old_version_top) {
		if (compare_versions (&info->old_version_bottom, name) == 0)
			return TRUE;

		return FALSE;
	}

	/* Check that the version defined by name is valid for the interval */
	if (compare_versions (&info->old_version_top, name) < 0)
		return FALSE;

	/* We should be greater or equal than the small version */
	if (compare_versions (&info->old_version_bottom, name) > 0)
		return FALSE;

	return TRUE;
}

/**
 * mono_assembly_names_equal:
 * @l: first assembly
 * @r: second assembly.
 *
 * Compares two MonoAssemblyNames and returns whether they are equal.
 *
 * This compares the names, the cultures, the release version and their
 * public tokens.
 *
 * Returns: TRUE if both assembly names are equal.
 */
gboolean
mono_assembly_names_equal (MonoAssemblyName *l, MonoAssemblyName *r)
{
	if (!l->name || !r->name)
		return FALSE;

	if (strcmp (l->name, r->name))
		return FALSE;

	if (l->culture && r->culture && strcmp (l->culture, r->culture))
		return FALSE;

	if (l->major != r->major || l->minor != r->minor ||
			l->build != r->build || l->revision != r->revision)
		if (! ((l->major == 0 && l->minor == 0 && l->build == 0 && l->revision == 0) || (r->major == 0 && r->minor == 0 && r->build == 0 && r->revision == 0)))
			return FALSE;

	if (!l->public_key_token [0] || !r->public_key_token [0])
		return TRUE;

	if (!mono_public_tokens_are_equal (l->public_key_token, r->public_key_token))
		return FALSE;

	return TRUE;
}

static MonoAssembly *
load_in_path (const char *basename, const char** search_path, MonoImageOpenStatus *status, MonoBoolean refonly)
{
	int i;
	char *fullpath;
	MonoAssembly *result;

	for (i = 0; search_path [i]; ++i) {
		fullpath = g_build_filename (search_path [i], basename, NULL);
		result = mono_assembly_open_full (fullpath, status, refonly);
		g_free (fullpath);
		if (result)
			return result;
	}
	return NULL;
}

/**
 * mono_assembly_setrootdir:
 * @root_dir: The pathname of the root directory where we will locate assemblies
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
	/* Leak if called more than once */
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
 * mono_set_dirs:
 * @assembly_dir: the base directory for assemblies
 * @config_dir: the base directory for configuration files
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
#if defined (MONO_ASSEMBLIES)
	if (assembly_dir == NULL)
		assembly_dir = MONO_ASSEMBLIES;
#endif
#if defined (MONO_CFG_DIR)
	if (config_dir == NULL)
		config_dir = MONO_CFG_DIR;
#endif
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
	if (strcmp (p, "/mono") && strcmp (p, "/mono-sgen") && strcmp (p, "/pedump") && strcmp (p, "/monodis") && strcmp (p, "/mint") && strcmp (p, "/monodiet"))
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
	mono_set_dirs (MONO_ASSEMBLIES, MONO_CFG_DIR);
}

static G_GNUC_UNUSED void
set_dirs (char *exe)
{
	char *base;
	char *config, *lib, *mono;
	struct stat buf;
	
	/*
	 * Only /usr prefix is treated specially
	 */
	if (strncmp (exe, MONO_BINDIR, strlen (MONO_BINDIR)) == 0 || (base = compute_base (exe)) == NULL){
		fallback ();
		return;
	}

	config = g_build_filename (base, "etc", NULL);
	lib = g_build_filename (base, "lib", NULL);
	mono = g_build_filename (lib, "mono/2.0", NULL);
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
#if defined(HOST_WIN32) || (defined(PLATFORM_MACOSX) && !defined(TARGET_ARM))
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
	root = g_build_path (G_DIR_SEPARATOR_S, installdir, "lib", NULL);

	config = g_build_filename (root, "..", "etc", NULL);
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

	/* Linux style */
	s = readlink ("/proc/self/exe", buf, sizeof (buf)-1);

	if (s != -1){
		buf [s] = 0;
		set_dirs (buf);
		return;
	}

	/* Solaris 10 style */
	str = g_strdup_printf ("/proc/%d/path/a.out", getpid ());
	s = readlink (str, buf, sizeof (buf)-1);
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
	check_extra_gac_path_env ();

	InitializeCriticalSection (&assemblies_mutex);
}

gboolean
mono_assembly_fill_assembly_name (MonoImage *image, MonoAssemblyName *aname)
{
	MonoTableInfo *t = &image->tables [MONO_TABLE_ASSEMBLY];
	guint32 cols [MONO_ASSEMBLY_SIZE];

	if (!t->rows)
		return FALSE;

	mono_metadata_decode_row (t, 0, cols, MONO_ASSEMBLY_SIZE);

	aname->hash_len = 0;
	aname->hash_value = NULL;
	aname->name = mono_metadata_string_heap (image, cols [MONO_ASSEMBLY_NAME]);
	aname->culture = mono_metadata_string_heap (image, cols [MONO_ASSEMBLY_CULTURE]);
	aname->flags = cols [MONO_ASSEMBLY_FLAGS];
	aname->major = cols [MONO_ASSEMBLY_MAJOR_VERSION];
	aname->minor = cols [MONO_ASSEMBLY_MINOR_VERSION];
	aname->build = cols [MONO_ASSEMBLY_BUILD_NUMBER];
	aname->revision = cols [MONO_ASSEMBLY_REV_NUMBER];
	aname->hash_alg = cols [MONO_ASSEMBLY_HASH_ALG];
	if (cols [MONO_ASSEMBLY_PUBLIC_KEY]) {
		guchar* token = g_malloc (8);
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
	}
	else
		aname->public_key = 0;

	return TRUE;
}

/**
 * mono_stringify_assembly_name:
 * @aname: the assembly name.
 *
 * Convert @aname into its string format. The returned string is dynamically
 * allocated and should be freed by the caller.
 *
 * Returns: a newly allocated string with a string representation of
 * the assembly name.
 */
char*
mono_stringify_assembly_name (MonoAssemblyName *aname)
{
	const char *quote = (aname->name && g_ascii_isspace (aname->name [0])) ? "\"" : "";

	return g_strdup_printf (
		"%s%s%s, Version=%d.%d.%d.%d, Culture=%s, PublicKeyToken=%s%s",
		quote, aname->name, quote,
		aname->major, aname->minor, aname->build, aname->revision,
		aname->culture && *aname->culture? aname->culture: "neutral",
		aname->public_key_token [0] ? (char *)aname->public_key_token : "null",
		(aname->flags & ASSEMBLYREF_RETARGETABLE_FLAG) ? ", Retargetable=Yes" : "");
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

/**
 * mono_assembly_addref:
 * @assemnly: the assembly to reference
 *
 * This routine increments the reference count on a MonoAssembly.
 * The reference count is reduced every time the method mono_assembly_close() is
 * invoked.
 */
void
mono_assembly_addref (MonoAssembly *assembly)
{
	InterlockedIncrement (&assembly->ref_count);
}

/*
 * CAUTION: This table must be kept in sync with
 *          ivkm/reflect/Fusion.cs
 */

#define SILVERLIGHT_KEY "7cec85d7bea7798e"
#define WINFX_KEY "31bf3856ad364e35"
#define ECMA_KEY "b77a5c561934e089"
#define MSFINAL_KEY "b03f5f7f11d50a3a"

typedef struct {
	const char *name;
	const char *from;
	const char *to;
} KeyRemapEntry;

static KeyRemapEntry key_remap_table[] = {
	{ "Microsoft.CSharp", WINFX_KEY, MSFINAL_KEY },
	{ "System", SILVERLIGHT_KEY, ECMA_KEY },
	{ "System.ComponentModel.Composition", WINFX_KEY, ECMA_KEY },
	{ "System.ComponentModel.DataAnnotations", "ddd0da4d3e678217", WINFX_KEY },
	{ "System.Core", SILVERLIGHT_KEY, ECMA_KEY },
	// FIXME: MS uses MSFINAL_KEY for .NET 4.5
	{ "System.Net", SILVERLIGHT_KEY, MSFINAL_KEY },
	{ "System.Numerics", WINFX_KEY, ECMA_KEY },
	{ "System.Runtime.Serialization", SILVERLIGHT_KEY, ECMA_KEY },
	{ "System.ServiceModel", WINFX_KEY, ECMA_KEY },
	{ "System.ServiceModel.Web", SILVERLIGHT_KEY, WINFX_KEY },
	{ "System.Windows", SILVERLIGHT_KEY, MSFINAL_KEY },
	{ "System.Xml", SILVERLIGHT_KEY, ECMA_KEY },
	{ "System.Xml.Linq", WINFX_KEY, ECMA_KEY },
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
		     
		mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_ASSEMBLY,
			    "Remapped public key token of retargetable assembly %s from %s to %s",
			    aname->name, entry->from, entry->to);
		return;
	}
}

static MonoAssemblyName *
mono_assembly_remap_version (MonoAssemblyName *aname, MonoAssemblyName *dest_aname)
{
	const MonoRuntimeInfo *current_runtime;
	int pos, first, last;

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

		mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_ASSEMBLY,
					"The request to load the retargetable assembly %s v%d.%d.%d.%d was remapped to %s v%d.%d.%d.%d",
					aname->name,
					aname->major, aname->minor, aname->build, aname->revision,
					dest_aname->name,
					vset->major, vset->minor, vset->build, vset->revision
					);

		return dest_aname;
	}
	
#ifndef DISABLE_ASSEMBLY_REMAPPING
	first = 0;
	last = G_N_ELEMENTS (framework_assemblies) - 1;
	
	while (first <= last) {
		int res;
		pos = first + (last - first) / 2;
		res = strcmp (aname->name, framework_assemblies[pos].assembly_name);
		if (res == 0) {
			const AssemblyVersionSet* vset;
			int index = framework_assemblies[pos].version_set_index;
			g_assert (index < G_N_ELEMENTS (current_runtime->version_sets));
			vset = &current_runtime->version_sets [index];

			if (aname->major == vset->major && aname->minor == vset->minor &&
				aname->build == vset->build && aname->revision == vset->revision)
				return aname;
		
			if ((aname->major | aname->minor | aname->build | aname->revision) != 0)
				mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_ASSEMBLY,
					"The request to load the assembly %s v%d.%d.%d.%d was remapped to v%d.%d.%d.%d",
							aname->name,
							aname->major, aname->minor, aname->build, aname->revision,
							vset->major, vset->minor, vset->build, vset->revision
							);
			
			memcpy (dest_aname, aname, sizeof(MonoAssemblyName));
			dest_aname->major = vset->major;
			dest_aname->minor = vset->minor;
			dest_aname->build = vset->build;
			dest_aname->revision = vset->revision;
			return dest_aname;
		} else if (res < 0) {
			last = pos - 1;
		} else {
			first = pos + 1;
		}
	}
#endif

	return aname;
}

/*
 * mono_assembly_get_assemblyref:
 *
 *   Fill out ANAME with the assembly name of the INDEXth assembly reference in IMAGE.
 */
void
mono_assembly_get_assemblyref (MonoImage *image, int index, MonoAssemblyName *aname)
{
	MonoTableInfo *t;
	guint32 cols [MONO_ASSEMBLYREF_SIZE];
	const char *hash;

	t = &image->tables [MONO_TABLE_ASSEMBLYREF];

	mono_metadata_decode_row (t, index, cols, MONO_ASSEMBLYREF_SIZE);
		
	hash = mono_metadata_blob_heap (image, cols [MONO_ASSEMBLYREF_HASH_VALUE]);
	aname->hash_len = mono_metadata_decode_blob_size (hash, &hash);
	aname->hash_value = hash;
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

void
mono_assembly_load_reference (MonoImage *image, int index)
{
	MonoAssembly *reference;
	MonoAssemblyName aname;
	MonoImageOpenStatus status;

	/*
	 * image->references is shared between threads, so we need to access
	 * it inside a critical section.
	 */
	mono_assemblies_lock ();
	if (!image->references) {
		MonoTableInfo *t = &image->tables [MONO_TABLE_ASSEMBLYREF];
	
		image->references = g_new0 (MonoAssembly *, t->rows + 1);
		image->nreferences = t->rows;
	}
	reference = image->references [index];
	mono_assemblies_unlock ();
	if (reference)
		return;

	mono_assembly_get_assemblyref (image, index, &aname);

	if (image->assembly && image->assembly->ref_only) {
		/* We use the loaded corlib */
		if (!strcmp (aname.name, "mscorlib"))
			reference = mono_assembly_load_full (&aname, image->assembly->basedir, &status, FALSE);
		else {
			reference = mono_assembly_loaded_full (&aname, TRUE);
			if (!reference)
				/* Try a postload search hook */
				reference = mono_assembly_invoke_search_hook_internal (&aname, TRUE, TRUE);
		}

		/*
		 * Here we must advice that the error was due to
		 * a non loaded reference using the ReflectionOnly api
		*/
		if (!reference)
			reference = REFERENCE_MISSING;
	} else
		reference = mono_assembly_load (&aname, image->assembly? image->assembly->basedir: NULL, &status);

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

	mono_assemblies_lock ();
	if (reference == NULL) {
		/* Flag as not found */
		reference = REFERENCE_MISSING;
	}	

	if (!image->references [index]) {
		if (reference != REFERENCE_MISSING){
			mono_assembly_addref (reference);
			if (image->assembly)
				mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Assembly Ref addref %s[%p] -> %s[%p]: %d",
				    image->assembly->aname.name, image->assembly, reference->aname.name, reference, reference->ref_count);
		} else {
			if (image->assembly)
				mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Failed to load assembly %s[%p]\n",
				    image->assembly->aname.name, image->assembly);
		}
		
		image->references [index] = reference;
	}
	mono_assemblies_unlock ();

	if (image->references [index] != reference) {
		/* Somebody loaded it before us */
		mono_assembly_close (reference);
	}
}

void
mono_assembly_load_references (MonoImage *image, MonoImageOpenStatus *status)
{
	/* This is a no-op now but it is part of the embedding API so we can't remove it */
	*status = MONO_IMAGE_OK;
}

typedef struct AssemblyLoadHook AssemblyLoadHook;
struct AssemblyLoadHook {
	AssemblyLoadHook *next;
	MonoAssemblyLoadFunc func;
	gpointer user_data;
};

AssemblyLoadHook *assembly_load_hook = NULL;

void
mono_assembly_invoke_load_hook (MonoAssembly *ass)
{
	AssemblyLoadHook *hook;

	for (hook = assembly_load_hook; hook; hook = hook->next) {
		hook->func (ass, hook->user_data);
	}
}

void
mono_install_assembly_load_hook (MonoAssemblyLoadFunc func, gpointer user_data)
{
	AssemblyLoadHook *hook;
	
	g_return_if_fail (func != NULL);

	hook = g_new0 (AssemblyLoadHook, 1);
	hook->func = func;
	hook->user_data = user_data;
	hook->next = assembly_load_hook;
	assembly_load_hook = hook;
}

static void
free_assembly_load_hooks (void)
{
	AssemblyLoadHook *hook, *next;

	for (hook = assembly_load_hook; hook; hook = next) {
		next = hook->next;
		g_free (hook);
	}
}

typedef struct AssemblySearchHook AssemblySearchHook;
struct AssemblySearchHook {
	AssemblySearchHook *next;
	MonoAssemblySearchFunc func;
	gboolean refonly;
	gboolean postload;
	gpointer user_data;
};

AssemblySearchHook *assembly_search_hook = NULL;

static MonoAssembly*
mono_assembly_invoke_search_hook_internal (MonoAssemblyName *aname, gboolean refonly, gboolean postload)
{
	AssemblySearchHook *hook;

	for (hook = assembly_search_hook; hook; hook = hook->next) {
		if ((hook->refonly == refonly) && (hook->postload == postload)) {
			MonoAssembly *ass = hook->func (aname, hook->user_data);
			if (ass)
				return ass;
		}
	}

	return NULL;
}

MonoAssembly*
mono_assembly_invoke_search_hook (MonoAssemblyName *aname)
{
	return mono_assembly_invoke_search_hook_internal (aname, FALSE, FALSE);
}

static void
mono_install_assembly_search_hook_internal (MonoAssemblySearchFunc func, gpointer user_data, gboolean refonly, gboolean postload)
{
	AssemblySearchHook *hook;
	
	g_return_if_fail (func != NULL);

	hook = g_new0 (AssemblySearchHook, 1);
	hook->func = func;
	hook->user_data = user_data;
	hook->refonly = refonly;
	hook->postload = postload;
	hook->next = assembly_search_hook;
	assembly_search_hook = hook;
}

void          
mono_install_assembly_search_hook (MonoAssemblySearchFunc func, gpointer user_data)
{
	mono_install_assembly_search_hook_internal (func, user_data, FALSE, FALSE);
}	

static void
free_assembly_search_hooks (void)
{
	AssemblySearchHook *hook, *next;

	for (hook = assembly_search_hook; hook; hook = next) {
		next = hook->next;
		g_free (hook);
	}
}

void
mono_install_assembly_refonly_search_hook (MonoAssemblySearchFunc func, gpointer user_data)
{
	mono_install_assembly_search_hook_internal (func, user_data, TRUE, FALSE);
}

void          
mono_install_assembly_postload_search_hook (MonoAssemblySearchFunc func, gpointer user_data)
{
	mono_install_assembly_search_hook_internal (func, user_data, FALSE, TRUE);
}	

void
mono_install_assembly_postload_refonly_search_hook (MonoAssemblySearchFunc func, gpointer user_data)
{
	mono_install_assembly_search_hook_internal (func, user_data, TRUE, TRUE);
}

typedef struct AssemblyPreLoadHook AssemblyPreLoadHook;
struct AssemblyPreLoadHook {
	AssemblyPreLoadHook *next;
	MonoAssemblyPreLoadFunc func;
	gpointer user_data;
};

static AssemblyPreLoadHook *assembly_preload_hook = NULL;
static AssemblyPreLoadHook *assembly_refonly_preload_hook = NULL;

static MonoAssembly *
invoke_assembly_preload_hook (MonoAssemblyName *aname, gchar **assemblies_path)
{
	AssemblyPreLoadHook *hook;
	MonoAssembly *assembly;

	for (hook = assembly_preload_hook; hook; hook = hook->next) {
		assembly = hook->func (aname, assemblies_path, hook->user_data);
		if (assembly != NULL)
			return assembly;
	}

	return NULL;
}

static MonoAssembly *
invoke_assembly_refonly_preload_hook (MonoAssemblyName *aname, gchar **assemblies_path)
{
	AssemblyPreLoadHook *hook;
	MonoAssembly *assembly;

	for (hook = assembly_refonly_preload_hook; hook; hook = hook->next) {
		assembly = hook->func (aname, assemblies_path, hook->user_data);
		if (assembly != NULL)
			return assembly;
	}

	return NULL;
}

void
mono_install_assembly_preload_hook (MonoAssemblyPreLoadFunc func, gpointer user_data)
{
	AssemblyPreLoadHook *hook;
	
	g_return_if_fail (func != NULL);

	hook = g_new0 (AssemblyPreLoadHook, 1);
	hook->func = func;
	hook->user_data = user_data;
	hook->next = assembly_preload_hook;
	assembly_preload_hook = hook;
}

void
mono_install_assembly_refonly_preload_hook (MonoAssemblyPreLoadFunc func, gpointer user_data)
{
	AssemblyPreLoadHook *hook;
	
	g_return_if_fail (func != NULL);

	hook = g_new0 (AssemblyPreLoadHook, 1);
	hook->func = func;
	hook->user_data = user_data;
	hook->next = assembly_refonly_preload_hook;
	assembly_refonly_preload_hook = hook;
}

static void
free_assembly_preload_hooks (void)
{
	AssemblyPreLoadHook *hook, *next;

	for (hook = assembly_preload_hook; hook; hook = next) {
		next = hook->next;
		g_free (hook);
	}

	for (hook = assembly_refonly_preload_hook; hook; hook = next) {
		next = hook->next;
		g_free (hook);
	}
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
		res = g_strconcat (part, G_DIR_SEPARATOR_S, NULL);
		g_free (part);
		return res;
	}

	cwd = g_get_current_dir ();
	mixed = g_build_filename (cwd, filename, NULL);
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

/** 
 * mono_assembly_open_from_bundle:
 * @filename: Filename requested
 * @status: return value
 *
 * This routine tries to open the assembly specified by `filename' from the
 * defined bundles, if found, returns the MonoImage for it, if not found
 * returns NULL
 */
MonoImage *
mono_assembly_open_from_bundle (const char *filename, MonoImageOpenStatus *status, gboolean refonly)
{
	int i;
	char *name;
	MonoImage *image = NULL;

	/*
	 * we do a very simple search for bundled assemblies: it's not a general 
	 * purpose assembly loading mechanism.
	 */

	if (!bundles)
		return NULL;

	name = g_path_get_basename (filename);

	mono_assemblies_lock ();
	for (i = 0; !image && bundles [i]; ++i) {
		if (strcmp (bundles [i]->name, name) == 0) {
			image = mono_image_open_from_data_with_name ((char*)bundles [i]->data, bundles [i]->size, FALSE, status, refonly, name);
			break;
		}
	}
	mono_assemblies_unlock ();
	g_free (name);
	if (image) {
		mono_image_addref (image);
		return image;
	}
	return NULL;
}

MonoAssembly *
mono_assembly_open_full (const char *filename, MonoImageOpenStatus *status, gboolean refonly)
{
	MonoImage *image;
	MonoAssembly *ass;
	MonoImageOpenStatus def_status;
	gchar *fname;
	gchar *new_fname;
	
	g_return_val_if_fail (filename != NULL, NULL);

	if (!status)
		status = &def_status;
	*status = MONO_IMAGE_OK;

	if (strncmp (filename, "file://", 7) == 0) {
		GError *error = NULL;
		gchar *uri = (gchar *) filename;
		gchar *tmpuri;

		/*
		 * MS allows file://c:/... and fails on file://localhost/c:/... 
		 * They also throw an IndexOutOfRangeException if "file://"
		 */
		if (uri [7] != '/')
			uri = g_strdup_printf ("file:///%s", uri + 7);
	
		tmpuri = uri;
		uri = mono_escape_uri_string (tmpuri);
		fname = g_filename_from_uri (uri, NULL, &error);
		g_free (uri);

		if (tmpuri != filename)
			g_free (tmpuri);

		if (error != NULL) {
			g_warning ("%s\n", error->message);
			g_error_free (error);
			fname = g_strdup (filename);
		}
	} else {
		fname = g_strdup (filename);
	}

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY,
			"Assembly Loader probing location: '%s'.", fname);

	new_fname = NULL;
	if (!mono_assembly_is_in_gac (fname))
		new_fname = mono_make_shadow_copy (fname);
	if (new_fname && new_fname != fname) {
		g_free (fname);
		fname = new_fname;
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY,
			    "Assembly Loader shadow-copied assembly to: '%s'.", fname);
	}
	
	image = NULL;

	if (bundles != NULL)
		image = mono_assembly_open_from_bundle (fname, status, refonly);

	if (!image)
		image = mono_image_open_full (fname, status, refonly);

	if (!image){
		if (*status == MONO_IMAGE_OK)
			*status = MONO_IMAGE_ERROR_ERRNO;
		g_free (fname);
		return NULL;
	}

	if (image->assembly) {
		/* Already loaded by another appdomain */
		mono_assembly_invoke_load_hook (image->assembly);
		mono_image_close (image);
		g_free (fname);
		return image->assembly;
	}

	ass = mono_assembly_load_from_full (image, fname, status, refonly);

	if (ass) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY,
				"Assembly Loader loaded assembly from location: '%s'.", filename);
		if (!refonly)
			mono_config_for_assembly (ass->image);
	}

	/* Clear the reference added by mono_image_open */
	mono_image_close (image);
	
	g_free (fname);

	return ass;
}

static void
free_item (gpointer val, gpointer user_data)
{
	g_free (val);
}

/*
 * mono_assembly_load_friends:
 * @ass: an assembly
 *
 * Load the list of friend assemblies that are allowed to access
 * the assembly's internal types and members. They are stored as assembly
 * names in custom attributes.
 *
 * This is an internal method, we need this because when we load mscorlib
 * we do not have the mono_defaults.internals_visible_class loaded yet,
 * so we need to load these after we initialize the runtime. 
 *
 * LOCKING: Acquires the assemblies lock plus the loader lock.
 */
void
mono_assembly_load_friends (MonoAssembly* ass)
{
	int i;
	MonoCustomAttrInfo* attrs;
	GSList *list;

	if (ass->friend_assembly_names_inited)
		return;

	attrs = mono_custom_attrs_from_assembly (ass);
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

	list = NULL;
	/* 
	 * We build the list outside the assemblies lock, the worse that can happen
	 * is that we'll need to free the allocated list.
	 */
	for (i = 0; i < attrs->num_attrs; ++i) {
		MonoCustomAttrEntry *attr = &attrs->attrs [i];
		MonoAssemblyName *aname;
		const gchar *data;
		guint slen;
		/* Do some sanity checking */
		if (!attr->ctor || attr->ctor->klass != mono_defaults.internals_visible_class)
			continue;
		if (attr->data_size < 4)
			continue;
		data = (const char*)attr->data;
		/* 0xFF means null string, see custom attr format */
		if (data [0] != 1 || data [1] != 0 || (data [2] & 0xFF) == 0xFF)
			continue;
		slen = mono_metadata_decode_value (data + 2, &data);
		aname = g_new0 (MonoAssemblyName, 1);
		/*g_print ("friend ass: %s\n", data);*/
		if (mono_assembly_name_parse_full (data, aname, TRUE, NULL, NULL)) {
			list = g_slist_prepend (list, aname);
		} else {
			g_free (aname);
		}
	}
	mono_custom_attrs_free (attrs);

	mono_assemblies_lock ();
	if (ass->friend_assembly_names_inited) {
		mono_assemblies_unlock ();
		g_slist_foreach (list, free_item, NULL);
		g_slist_free (list);
		return;
	}
	ass->friend_assembly_names = list;

	/* Because of the double checked locking pattern above */
	mono_memory_barrier ();
	ass->friend_assembly_names_inited = TRUE;
	mono_assemblies_unlock ();
}

/**
 * mono_assembly_open:
 * @filename: Opens the assembly pointed out by this name
 * @status: where a status code can be returned
 *
 * mono_assembly_open opens the PE-image pointed by @filename, and
 * loads any external assemblies referenced by it.
 *
 * Return: a pointer to the MonoAssembly if @filename contains a valid
 * assembly or NULL on error.  Details about the error are stored in the
 * @status variable.
 */
MonoAssembly *
mono_assembly_open (const char *filename, MonoImageOpenStatus *status)
{
	return mono_assembly_open_full (filename, status, FALSE);
}

MonoAssembly *
mono_assembly_load_from_full (MonoImage *image, const char*fname, 
			      MonoImageOpenStatus *status, gboolean refonly)
{
	MonoAssembly *ass, *ass2;
	char *base_dir;

	if (!image->tables [MONO_TABLE_ASSEMBLY].rows) {
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
	ass->ref_only = refonly;
	ass->image = image;

	mono_profiler_assembly_event (ass, MONO_PROFILE_START_LOAD);

	mono_assembly_fill_assembly_name (image, &ass->aname);

	if (mono_defaults.corlib && strcmp (ass->aname.name, "mscorlib") == 0) {
		// MS.NET doesn't support loading other mscorlibs
		g_free (ass);
		g_free (base_dir);
		mono_image_addref (mono_defaults.corlib);
		*status = MONO_IMAGE_OK;
		return mono_defaults.corlib->assembly;
	}

	/* Add a non-temporary reference because of ass->image */
	mono_image_addref (image);

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Image addref %s[%p] -> %s[%p]: %d", ass->aname.name, ass, image->name, image, image->ref_count);

	/* 
	 * The load hooks might take locks so we can't call them while holding the
	 * assemblies lock.
	 */
	if (ass->aname.name) {
		ass2 = mono_assembly_invoke_search_hook_internal (&ass->aname, refonly, FALSE);
		if (ass2) {
			g_free (ass);
			g_free (base_dir);
			mono_image_close (image);
			*status = MONO_IMAGE_OK;
			return ass2;
		}
	}

	mono_assemblies_lock ();

	if (image->assembly) {
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

	image->assembly = ass;

	loaded_assemblies = g_list_prepend (loaded_assemblies, ass);
	mono_assemblies_unlock ();

#ifdef HOST_WIN32
	if (image->is_module_handle)
		mono_image_fixup_vtable (image);
#endif

	mono_assembly_invoke_load_hook (ass);

	mono_profiler_assembly_loaded (ass, MONO_PROFILE_OK);
	
	return ass;
}

MonoAssembly *
mono_assembly_load_from (MonoImage *image, const char *fname,
			 MonoImageOpenStatus *status)
{
	return mono_assembly_load_from_full (image, fname, status, FALSE);
}

/**
 * mono_assembly_name_free:
 * @aname: assembly name to free
 * 
 * Frees the provided assembly name object.
 * (it does not frees the object itself, only the name members).
 */
void
mono_assembly_name_free (MonoAssemblyName *aname)
{
	if (aname == NULL)
		return;

	g_free ((void *) aname->name);
	g_free ((void *) aname->culture);
	g_free ((void *) aname->hash_value);
}

static gboolean
parse_public_key (const gchar *key, gchar** pubkey, gboolean *is_ecma)
{
	const gchar *pkey;
	gchar header [16], val, *arr;
	gint i, j, offset, bitlen, keylen, pkeylen;
	
	keylen = strlen (key) >> 1;
	if (keylen < 1)
		return FALSE;

	/* allow the ECMA standard key */
	if (strcmp (key, "00000000000000000400000000000000") == 0) {
		if (pubkey) {
			*pubkey = g_strdup (key);
			*is_ecma = TRUE;
		}
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

	/* parsing is OK and the public key itself is not requested back */
	if (!pubkey)
		return TRUE;
		
	/* Encode the size of the blob */
	offset = 0;
	if (keylen <= 127) {
		arr = g_malloc (keylen + 1);
		arr [offset++] = keylen;
	} else {
		arr = g_malloc (keylen + 2);
		arr [offset++] = 0x80; /* 10bs */
		arr [offset++] = keylen;
	}
		
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
	gint major, minor, build, revision;
	gint len;
	gint version_parts;
	gchar *pkey, *pkeyptr, *encoded, tok [8];

	memset (aname, 0, sizeof (MonoAssemblyName));

	if (version) {
		version_parts = sscanf (version, "%u.%u.%u.%u", &major, &minor, &build, &revision);
		if (version_parts < 2 || version_parts > 4)
			return FALSE;

		/* FIXME: we should set build & revision to -1 (instead of 0)
		if these are not set in the version string. That way, later on,
		we can still determine if these were specified.	*/
		aname->major = major;
		aname->minor = minor;
		if (version_parts >= 3)
			aname->build = build;
		else
			aname->build = 0;
		if (version_parts == 4)
			aname->revision = revision;
		else
			aname->revision = 0;
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
			mono_assembly_name_free (aname);
			return FALSE;
		}
		lower = g_ascii_strdown (token, MONO_PUBLIC_KEY_TOKEN_LENGTH);
		g_strlcpy ((char*)aname->public_key_token, lower, MONO_PUBLIC_KEY_TOKEN_LENGTH);
		g_free (lower);
	}

	if (key) {
		gboolean is_ecma;
		if (strcmp (key, "null") == 0 || !parse_public_key (key, &pkey, &is_ecma)) {
			mono_assembly_name_free (aname);
			return FALSE;
		}

		if (is_ecma) {
			if (save_public_key)
				aname->public_key = (guint8*)pkey;
			else
				g_free (pkey);
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
parse_assembly_directory_name (const char *name, const char *dirname, MonoAssemblyName *aname)
{
	gchar **parts;
	gboolean res;
	
	parts = g_strsplit (dirname, "_", 3);
	if (!parts || !parts[0] || !parts[1] || !parts[2]) {
		g_strfreev (parts);
		return FALSE;
	}
	
	res = build_assembly_name (name, parts[0], parts[1], parts[2], NULL, 0, 0, aname, FALSE);
	g_strfreev (parts);
	return res;
}

static gboolean
split_key_value (const gchar *pair, gchar **key, guint32 *keylen, gchar **value)
{
	char *eqsign = strchr (pair, '=');
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
	gchar *version = NULL;
	gchar *culture = NULL;
	gchar *token = NULL;
	gchar *key = NULL;
	gchar *retargetable = NULL;
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
		g_strfreev (tmp);
		return FALSE;
	}

	dllname = g_strstrip (*tmp);
	
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
			if (strlen (retargetable) == 0) {
				goto cleanup_and_fail;
			}
			if (!g_ascii_strcasecmp (retargetable, "yes")) {
				flags |= ASSEMBLYREF_RETARGETABLE_FLAG;
			} else if (g_ascii_strcasecmp (retargetable, "no")) {
				goto cleanup_and_fail;
			}
			tmp++;
			continue;
		}

		if (part_name_len == 21 && !g_ascii_strncasecmp (part_name, "ProcessorArchitecture", part_name_len)) {
			if (!g_ascii_strcasecmp (value, "None"))
				arch = MONO_PROCESSOR_ARCHITECTURE_NONE;
			else if (!g_ascii_strcasecmp (value, "MSIL"))
				arch = MONO_PROCESSOR_ARCHITECTURE_MSIL;
			else if (!g_ascii_strcasecmp (value, "X86"))
				arch = MONO_PROCESSOR_ARCHITECTURE_X86;
			else if (!g_ascii_strcasecmp (value, "IA64"))
				arch = MONO_PROCESSOR_ARCHITECTURE_IA64;
			else if (!g_ascii_strcasecmp (value, "AMD64"))
				arch = MONO_PROCESSOR_ARCHITECTURE_AMD64;
			else
				goto cleanup_and_fail;
			tmp++;
			continue;
		}

		g_strfreev (parts);
		return FALSE;
	}

	/* if retargetable flag is set, then we must have a fully qualified name */
	if (retargetable != NULL && (version == NULL || culture == NULL || (key == NULL && token == NULL))) {
		goto cleanup_and_fail;
	}

	res = build_assembly_name (dllname, version, culture, token, key, flags, arch,
		aname, save_public_key);
	g_strfreev (parts);
	return res;

cleanup_and_fail:
	g_strfreev (parts);
	return FALSE;
}

/**
 * mono_assembly_name_parse:
 * @name: name to parse
 * @aname: the destination assembly name
 * 
 * Parses an assembly qualified type name and assigns the name,
 * version, culture and token to the provided assembly name object.
 *
 * Returns: true if the name could be parsed.
 */
gboolean
mono_assembly_name_parse (const char *name, MonoAssemblyName *aname)
{
	return mono_assembly_name_parse_full (name, aname, FALSE, NULL, NULL);
}

/**
 * mono_assembly_name_new:
 * @name: name to parse
 *
 * Allocate a new MonoAssemblyName and fill its values from the
 * passed @name.
 *
 * Returns: a newly allocated structure or NULL if there was any failure.
 */
MonoAssemblyName*
mono_assembly_name_new (const char *name)
{
	MonoAssemblyName *aname = g_new0 (MonoAssemblyName, 1);
	if (mono_assembly_name_parse (name, aname))
		return aname;
	g_free (aname);
	return NULL;
}

const char*
mono_assembly_name_get_name (MonoAssemblyName *aname)
{
	return aname->name;
}

const char*
mono_assembly_name_get_culture (MonoAssemblyName *aname)
{
	return aname->culture;
}

mono_byte*
mono_assembly_name_get_pubkeytoken (MonoAssemblyName *aname)
{
	if (aname->public_key_token [0])
		return aname->public_key_token;
	return NULL;
}

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

static MonoAssembly*
probe_for_partial_name (const char *basepath, const char *fullname, MonoAssemblyName *aname, MonoImageOpenStatus *status)
{
	gchar *fullpath = NULL;
	GDir *dirhandle;
	const char* direntry;
	MonoAssemblyName gac_aname;
	gint major=-1, minor=0, build=0, revision=0;
	gboolean exact_version;
	
	dirhandle = g_dir_open (basepath, 0, NULL);
	if (!dirhandle)
		return NULL;
		
	exact_version = (aname->major | aname->minor | aname->build | aname->revision) != 0;

	while ((direntry = g_dir_read_name (dirhandle))) {
		gboolean match = TRUE;
		
		if(!parse_assembly_directory_name (aname->name, direntry, &gac_aname))
			continue;
		
		if (aname->culture != NULL && strcmp (aname->culture, gac_aname.culture) != 0)
			match = FALSE;
			
		if (match && strlen ((char*)aname->public_key_token) > 0 && 
				!mono_public_tokens_are_equal (aname->public_key_token, gac_aname.public_key_token))
			match = FALSE;
		
		if (match) {
			if (exact_version) {
				match = (aname->major == gac_aname.major && aname->minor == gac_aname.minor &&
						 aname->build == gac_aname.build && aname->revision == gac_aname.revision); 
			}
			else if (gac_aname.major < major)
				match = FALSE;
			else if (gac_aname.major == major) {
				if (gac_aname.minor < minor)
					match = FALSE;
				else if (gac_aname.minor == minor) {
					if (gac_aname.build < build)
						match = FALSE;
					else if (gac_aname.build == build && gac_aname.revision <= revision)
						match = FALSE; 
				}
			}
		}
		
		if (match) {
			major = gac_aname.major;
			minor = gac_aname.minor;
			build = gac_aname.build;
			revision = gac_aname.revision;
			g_free (fullpath);
			fullpath = g_build_path (G_DIR_SEPARATOR_S, basepath, direntry, fullname, NULL);
		}

		mono_assembly_name_free (&gac_aname);
	}
	
	g_dir_close (dirhandle);
	
	if (fullpath == NULL)
		return NULL;
	else {
		MonoAssembly *res = mono_assembly_open (fullpath, status);
		g_free (fullpath);
		return res;
	}
}

MonoAssembly*
mono_assembly_load_with_partial_name (const char *name, MonoImageOpenStatus *status)
{
	MonoAssembly *res;
	MonoAssemblyName *aname, base_name;
	MonoAssemblyName mapped_aname;
	gchar *fullname, *gacpath;
	gchar **paths;

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
	
	res = mono_assembly_loaded (aname);
	if (res) {
		mono_assembly_name_free (aname);
		return res;
	}

	res = invoke_assembly_preload_hook (aname, assemblies_path);
	if (res) {
		res->in_gac = FALSE;
		mono_assembly_name_free (aname);
		return res;
	}

	fullname = g_strdup_printf ("%s.dll", aname->name);

	if (extra_gac_paths) {
		paths = extra_gac_paths;
		while (!res && *paths) {
			gacpath = g_build_path (G_DIR_SEPARATOR_S, *paths, "lib", "mono", "gac", aname->name, NULL);
			res = probe_for_partial_name (gacpath, fullname, aname, status);
			g_free (gacpath);
			paths++;
		}
	}

	if (res) {
		res->in_gac = TRUE;
		g_free (fullname);
		mono_assembly_name_free (aname);
		return res;
	}

	gacpath = g_build_path (G_DIR_SEPARATOR_S, mono_assembly_getrootdir (), "mono", "gac", aname->name, NULL);
	res = probe_for_partial_name (gacpath, fullname, aname, status);
	g_free (gacpath);

	if (res)
		res->in_gac = TRUE;
	else {
		MonoDomain *domain = mono_domain_get ();
		MonoReflectionAssembly *refasm = mono_try_assembly_resolve (domain, mono_string_new (domain, name), FALSE);
		if (refasm)
			res = refasm->assembly;
	}
	
	g_free (fullname);
	mono_assembly_name_free (aname);

	return res;
}

static MonoBoolean
mono_assembly_is_in_gac (const gchar *filename)
{
	const gchar *rootdir;
	gchar *gp;
	gchar **paths;

	if (filename == NULL)
		return FALSE;

	for (paths = extra_gac_paths; paths && *paths; paths++) {
		if (strstr (*paths, filename) != *paths)
			continue;

		gp = (gchar *) (filename + strlen (*paths));
		if (*gp != G_DIR_SEPARATOR)
			continue;
		gp++;
		if (strncmp (gp, "lib", 3))
			continue;
		gp += 3;
		if (*gp != G_DIR_SEPARATOR)
			continue;
		gp++;
		if (strncmp (gp, "mono", 4))
			continue;
		gp += 4;
		if (*gp != G_DIR_SEPARATOR)
			continue;
		gp++;
		if (strncmp (gp, "gac", 3))
			continue;
		gp += 3;
		if (*gp != G_DIR_SEPARATOR)
			continue;

		return TRUE;
	}

	rootdir = mono_assembly_getrootdir ();
	if (strstr (filename, rootdir) != filename)
		return FALSE;

	gp = (gchar *) (filename + strlen (rootdir));
	if (*gp != G_DIR_SEPARATOR)
		return FALSE;
	gp++;
	if (strncmp (gp, "mono", 4))
		return FALSE;
	gp += 4;
	if (*gp != G_DIR_SEPARATOR)
		return FALSE;
	gp++;
	if (strncmp (gp, "gac", 3))
		return FALSE;
	gp += 3;
	if (*gp != G_DIR_SEPARATOR)
		return FALSE;
	return TRUE;
}

static MonoImage*
mono_assembly_load_publisher_policy (MonoAssemblyName *aname)
{
	MonoImage *image;
	gchar *filename, *pname, *name, *culture, *version, *fullpath, *subpath;
	gchar **paths;
	gint32 len;

	if (strstr (aname->name, ".dll")) {
		len = strlen (aname->name) - 4;
		name = g_malloc (len);
		strncpy (name, aname->name, len);
	} else
		name = g_strdup (aname->name);
	
	if (aname->culture)
		culture = g_utf8_strdown (aname->culture, -1);
	else
		culture = g_strdup ("");
	
	pname = g_strdup_printf ("policy.%d.%d.%s", aname->major, aname->minor, name);
	version = g_strdup_printf ("0.0.0.0_%s_%s", culture, aname->public_key_token);
	g_free (name);
	g_free (culture);
	
	filename = g_strconcat (pname, ".dll", NULL);
	subpath = g_build_path (G_DIR_SEPARATOR_S, pname, version, filename, NULL);
	g_free (pname);
	g_free (version);
	g_free (filename);

	image = NULL;
	if (extra_gac_paths) {
		paths = extra_gac_paths;
		while (!image && *paths) {
			fullpath = g_build_path (G_DIR_SEPARATOR_S, *paths,
					"lib", "mono", "gac", subpath, NULL);
			image = mono_image_open (fullpath, NULL);
			g_free (fullpath);
			paths++;
		}
	}

	if (image) {
		g_free (subpath);
		return image;
	}

	fullpath = g_build_path (G_DIR_SEPARATOR_S, mono_assembly_getrootdir (), 
			"mono", "gac", subpath, NULL);
	image = mono_image_open (fullpath, NULL);
	g_free (subpath);
	g_free (fullpath);
	
	return image;
}

static MonoAssemblyName*
mono_assembly_bind_version (MonoAssemblyBindingInfo *info, MonoAssemblyName *aname, MonoAssemblyName *dest_name)
{
	memcpy (dest_name, aname, sizeof (MonoAssemblyName));
	dest_name->major = info->new_version.major;
	dest_name->minor = info->new_version.minor;
	dest_name->build = info->new_version.build;
	dest_name->revision = info->new_version.revision;
	
	return dest_name;
}

/* LOCKING: Assumes that we are already locked */
static MonoAssemblyBindingInfo*
search_binding_loaded (MonoAssemblyName *aname)
{
	GSList *tmp;

	for (tmp = loaded_assembly_bindings; tmp; tmp = tmp->next) {
		MonoAssemblyBindingInfo *info = tmp->data;
		if (assembly_binding_maps_name (info, aname))
			return info;
	}

	return NULL;
}

static inline gboolean
info_compare_versions (AssemblyVersionSet *left, AssemblyVersionSet *right)
{
	if (left->major != right->major || left->minor != right->minor ||
	    left->build != right->build || left->revision != right->revision)
		return FALSE;

	return TRUE;
}

static inline gboolean
info_versions_equal (MonoAssemblyBindingInfo *left, MonoAssemblyBindingInfo *right)
{
	if (left->has_old_version_bottom != right->has_old_version_bottom)
		return FALSE;

	if (left->has_old_version_top != right->has_old_version_top)
		return FALSE;

	if (left->has_new_version != right->has_new_version)
		return FALSE;

	if (left->has_old_version_bottom && !info_compare_versions (&left->old_version_bottom, &right->old_version_bottom))
		return FALSE;

	if (left->has_old_version_top && !info_compare_versions (&left->old_version_top, &right->old_version_top))
		return FALSE;

	if (left->has_new_version && !info_compare_versions (&left->new_version, &right->new_version))
		return FALSE;

	return TRUE;
}

/* LOCKING: assumes all the necessary locks are held */
static void
assembly_binding_info_parsed (MonoAssemblyBindingInfo *info, void *user_data)
{
	MonoAssemblyBindingInfo *info_copy;
	GSList *tmp;
	MonoAssemblyBindingInfo *info_tmp;
	MonoDomain *domain = (MonoDomain*)user_data;

	if (!domain)
		return;

	for (tmp = domain->assembly_bindings; tmp; tmp = tmp->next) {
		info_tmp = tmp->data;
		if (strcmp (info->name, info_tmp->name) == 0 && info_versions_equal (info, info_tmp))
			return;
	}

	info_copy = mono_mempool_alloc0 (domain->mp, sizeof (MonoAssemblyBindingInfo));
	memcpy (info_copy, info, sizeof (MonoAssemblyBindingInfo));
	if (info->name)
		info_copy->name = mono_mempool_strdup (domain->mp, info->name);
	if (info->culture)
		info_copy->culture = mono_mempool_strdup (domain->mp, info->culture);

	domain->assembly_bindings = g_slist_append_mempool (domain->mp, domain->assembly_bindings, info_copy);
}

static inline gboolean
info_major_minor_in_range (MonoAssemblyBindingInfo *info, MonoAssemblyName *aname)
{
	if (!info->has_old_version_bottom)
		return FALSE;

	if (info->old_version_bottom.major > aname->major || info->old_version_bottom.minor > aname->minor)
		return FALSE;

	if (info->has_old_version_top && (info->old_version_top.major < aname->major || info->old_version_top.minor < aname->minor))
		return FALSE;

	/* This is not the nicest way to do it, but it's a by-product of the way parsing is done */
	info->major = aname->major;
	info->minor = aname->minor;

	return TRUE;
}

/* LOCKING: Assumes that we are already locked - both loader and domain locks */
static MonoAssemblyBindingInfo*
get_per_domain_assembly_binding_info (MonoDomain *domain, MonoAssemblyName *aname)
{
	MonoAssemblyBindingInfo *info;
	GSList *list;

	if (!domain->assembly_bindings)
		return NULL;

	info = NULL;
	for (list = domain->assembly_bindings; list; list = list->next) {
		info = list->data;
		if (info && !strcmp (aname->name, info->name) && info_major_minor_in_range (info, aname))
			break;
		info = NULL;
	}

	if (info) {
		if (info->name && info->public_key_token [0] && info->has_old_version_bottom &&
		    info->has_new_version && assembly_binding_maps_name (info, aname))
			info->is_valid = TRUE;
		else
			info->is_valid = FALSE;
	}

	return info;
}

static MonoAssemblyName*
mono_assembly_apply_binding (MonoAssemblyName *aname, MonoAssemblyName *dest_name)
{
	MonoAssemblyBindingInfo *info, *info2;
	MonoImage *ppimage;
	MonoDomain *domain;

	if (aname->public_key_token [0] == 0)
		return aname;

	domain = mono_domain_get ();
	mono_loader_lock ();
	info = search_binding_loaded (aname);
	if (!info) {
		mono_domain_lock (domain);
		info = get_per_domain_assembly_binding_info (domain, aname);
		mono_domain_unlock (domain);
	}

	mono_loader_unlock ();
	if (info) {
		if (!check_policy_versions (info, aname))
			return aname;
		
		mono_assembly_bind_version (info, aname, dest_name);
		return dest_name;
	}

	if (domain && domain->setup && domain->setup->configuration_file) {
		mono_domain_lock (domain);
		if (!domain->assembly_bindings_parsed) {
			gchar *domain_config_file_name = mono_string_to_utf8 (domain->setup->configuration_file);
			gchar *domain_config_file_path = mono_portability_find_file (domain_config_file_name, TRUE);

			if (!domain_config_file_path)
				domain_config_file_path = domain_config_file_name;
			
			mono_config_parse_assembly_bindings (domain_config_file_path, aname->major, aname->minor, domain, assembly_binding_info_parsed);
			domain->assembly_bindings_parsed = TRUE;
			if (domain_config_file_name != domain_config_file_path)
				g_free (domain_config_file_name);
			g_free (domain_config_file_path);
		}
		mono_domain_unlock (domain);

		mono_loader_lock ();
		mono_domain_lock (domain);
		info2 = get_per_domain_assembly_binding_info (domain, aname);

		if (info2) {
			info = g_memdup (info2, sizeof (MonoAssemblyBindingInfo));
			info->name = g_strdup (info2->name);
			info->culture = g_strdup (info2->culture);
			info->domain_id = domain->domain_id;
		}

		mono_domain_unlock (domain);
		mono_loader_unlock ();
	}

	if (!info) {
		info = g_new0 (MonoAssemblyBindingInfo, 1);
		info->major = aname->major;
		info->minor = aname->minor;
	}

	if (!info->is_valid) {
		ppimage = mono_assembly_load_publisher_policy (aname);
		if (ppimage) {
			get_publisher_policy_info (ppimage, aname, info);
			mono_image_close (ppimage);
		}
	}

	/* Define default error value if needed */
	if (!info->is_valid) {
		info->name = g_strdup (aname->name);
		info->culture = g_strdup (aname->culture);
		g_strlcpy ((char *)info->public_key_token, (const char *)aname->public_key_token, MONO_PUBLIC_KEY_TOKEN_LENGTH);
	}
	
	mono_loader_lock ();
	info2 = search_binding_loaded (aname);
	if (info2) {
		/* This binding was added by another thread 
		 * before us */
		mono_assembly_binding_info_free (info);
		g_free (info);
		
		info = info2;
	} else
		loaded_assembly_bindings = g_slist_prepend (loaded_assembly_bindings, info);
		
	mono_loader_unlock ();
	
	if (!info->is_valid || !check_policy_versions (info, aname))
		return aname;

	mono_assembly_bind_version (info, aname, dest_name);
	return dest_name;
}

/**
 * mono_assembly_load_from_gac
 *
 * @aname: The assembly name object
 */
static MonoAssembly*
mono_assembly_load_from_gac (MonoAssemblyName *aname,  gchar *filename, MonoImageOpenStatus *status, MonoBoolean refonly)
{
	MonoAssembly *result = NULL;
	gchar *name, *version, *culture, *fullpath, *subpath;
	gint32 len;
	gchar **paths;
	char *pubtok;

	if (aname->public_key_token [0] == 0) {
		return NULL;
	}

	if (strstr (aname->name, ".dll")) {
		len = strlen (filename) - 4;
		name = g_malloc (len);
		strncpy (name, aname->name, len);
	} else {
		name = g_strdup (aname->name);
	}

	if (aname->culture) {
		culture = g_utf8_strdown (aname->culture, -1);
	} else {
		culture = g_strdup ("");
	}

	pubtok = g_ascii_strdown ((char*)aname->public_key_token, MONO_PUBLIC_KEY_TOKEN_LENGTH);
	version = g_strdup_printf ("%d.%d.%d.%d_%s_%s", aname->major,
			aname->minor, aname->build, aname->revision,
			culture, pubtok);
	g_free (pubtok);
	
	subpath = g_build_path (G_DIR_SEPARATOR_S, name, version, filename, NULL);
	g_free (name);
	g_free (version);
	g_free (culture);

	if (extra_gac_paths) {
		paths = extra_gac_paths;
		while (!result && *paths) {
			fullpath = g_build_path (G_DIR_SEPARATOR_S, *paths, "lib", "mono", "gac", subpath, NULL);
			result = mono_assembly_open_full (fullpath, status, refonly);
			g_free (fullpath);
			paths++;
		}
	}

	if (result) {
		result->in_gac = TRUE;
		g_free (subpath);
		return result;
	}

	fullpath = g_build_path (G_DIR_SEPARATOR_S, mono_assembly_getrootdir (),
			"mono", "gac", subpath, NULL);
	result = mono_assembly_open_full (fullpath, status, refonly);
	g_free (fullpath);

	if (result)
		result->in_gac = TRUE;
	
	g_free (subpath);

	return result;
}

MonoAssembly*
mono_assembly_load_corlib (const MonoRuntimeInfo *runtime, MonoImageOpenStatus *status)
{
	char *corlib_file;
	MonoAssemblyName *aname;

	if (corlib) {
		/* g_print ("corlib already loaded\n"); */
		return corlib;
	}

#if defined(__native_client__)
	if (corlibData != NULL && corlibSize != 0) {
		int status = 0;
		/* First "FALSE" instructs mono not to make a copy. */
		/* Second "FALSE" says this is not just a ref.      */
		MonoImage* image = mono_image_open_from_data_full (corlibData, corlibSize, FALSE, &status, FALSE);
		if (image == NULL || status != 0)
			g_print("mono_image_open_from_data_full failed: %d\n", status);
		corlib = mono_assembly_load_from_full (image, "mscorlib", &status, FALSE);
		if (corlib == NULL || status != 0)
			g_print ("mono_assembly_load_from_full failed: %d\n", status);
		if (corlib)
			return corlib;
	}
#endif

	aname = mono_assembly_name_new ("mscorlib.dll");
	corlib = invoke_assembly_preload_hook (aname, assemblies_path);
	mono_assembly_name_free (aname);
	g_free (aname);
	if (corlib != NULL)
		return corlib;

	if (assemblies_path) {
		corlib = load_in_path ("mscorlib.dll", (const char**)assemblies_path, status, FALSE);
		if (corlib)
			return corlib;
	}

	/* Load corlib from mono/<version> */
	
	corlib_file = g_build_filename ("mono", runtime->framework_version, "mscorlib.dll", NULL);
	if (assemblies_path) {
		corlib = load_in_path (corlib_file, (const char**)assemblies_path, status, FALSE);
		if (corlib) {
			g_free (corlib_file);
			return corlib;
		}
	}
	corlib = load_in_path (corlib_file, default_path, status, FALSE);
	g_free (corlib_file);
	
	if (corlib && !strcmp (runtime->framework_version, "4.5"))
		default_path [1] = g_strdup_printf ("%s/mono/4.5/Facades", default_path [0]);
		
	return corlib;
}

MonoAssembly*
mono_assembly_load_full_nosearch (MonoAssemblyName *aname, 
								  const char       *basedir, 
								  MonoImageOpenStatus *status,
								  gboolean refonly)
{
	MonoAssembly *result;
	char *fullpath, *filename;
	MonoAssemblyName maped_aname;
	MonoAssemblyName maped_name_pp;
	int ext_index;
	const char *ext;
	int len;

	aname = mono_assembly_remap_version (aname, &maped_aname);
	
	/* Reflection only assemblies don't get assembly binding */
	if (!refonly)
		aname = mono_assembly_apply_binding (aname, &maped_name_pp);
	
	result = mono_assembly_loaded_full (aname, refonly);
	if (result)
		return result;

	result = refonly ? invoke_assembly_refonly_preload_hook (aname, assemblies_path) : invoke_assembly_preload_hook (aname, assemblies_path);
	if (result) {
		result->in_gac = FALSE;
		return result;
	}

	/* Currently we retrieve the loaded corlib for reflection 
	 * only requests, like a common reflection only assembly 
	 */
	if (strcmp (aname->name, "mscorlib") == 0 || strcmp (aname->name, "mscorlib.dll") == 0) {
		return mono_assembly_load_corlib (mono_get_runtime_info (), status);
	}

	len = strlen (aname->name);
	for (ext_index = 0; ext_index < 2; ext_index ++) {
		ext = ext_index == 0 ? ".dll" : ".exe";
		if (len > 4 && (!strcmp (aname->name + len - 4, ".dll") || !strcmp (aname->name + len - 4, ".exe"))) {
			filename = g_strdup (aname->name);
			/* Don't try appending .dll/.exe if it already has one of those extensions */
			ext_index++;
		} else {
			filename = g_strconcat (aname->name, ext, NULL);
		}

		result = mono_assembly_load_from_gac (aname, filename, status, refonly);
		if (result) {
			g_free (filename);
			return result;
		}

		if (basedir) {
			fullpath = g_build_filename (basedir, filename, NULL);
			result = mono_assembly_open_full (fullpath, status, refonly);
			g_free (fullpath);
			if (result) {
				result->in_gac = FALSE;
				g_free (filename);
				return result;
			}
		}

		result = load_in_path (filename, default_path, status, refonly);
		if (result)
			result->in_gac = FALSE;
		g_free (filename);
		if (result)
			return result;
	}

	return result;
}

/**
 * mono_assembly_load_full:
 * @aname: A MonoAssemblyName with the assembly name to load.
 * @basedir: A directory to look up the assembly at.
 * @status: a pointer to a MonoImageOpenStatus to return the status of the load operation
 * @refonly: Whether this assembly is being opened in "reflection-only" mode.
 *
 * Loads the assembly referenced by @aname, if the value of @basedir is not NULL, it
 * attempts to load the assembly from that directory before probing the standard locations.
 *
 * If the assembly is being opened in reflection-only mode (@refonly set to TRUE) then no 
 * assembly binding takes place.
 *
 * Returns: the assembly referenced by @aname loaded or NULL on error.   On error the
 * value pointed by status is updated with an error code.
 */
MonoAssembly*
mono_assembly_load_full (MonoAssemblyName *aname, const char *basedir, MonoImageOpenStatus *status, gboolean refonly)
{
	MonoAssembly *result = mono_assembly_load_full_nosearch (aname, basedir, status, refonly);
	
	if (!result)
		/* Try a postload search hook */
		result = mono_assembly_invoke_search_hook_internal (aname, refonly, TRUE);
	return result;
}

/**
 * mono_assembly_load:
 * @aname: A MonoAssemblyName with the assembly name to load.
 * @basedir: A directory to look up the assembly at.
 * @status: a pointer to a MonoImageOpenStatus to return the status of the load operation
 *
 * Loads the assembly referenced by @aname, if the value of @basedir is not NULL, it
 * attempts to load the assembly from that directory before probing the standard locations.
 *
 * Returns: the assembly referenced by @aname loaded or NULL on error.   On error the
 * value pointed by status is updated with an error code.
 */
MonoAssembly*
mono_assembly_load (MonoAssemblyName *aname, const char *basedir, MonoImageOpenStatus *status)
{
	return mono_assembly_load_full (aname, basedir, status, FALSE);
}
	
MonoAssembly*
mono_assembly_loaded_full (MonoAssemblyName *aname, gboolean refonly)
{
	MonoAssembly *res;
	MonoAssemblyName maped_aname;

	aname = mono_assembly_remap_version (aname, &maped_aname);

	res = mono_assembly_invoke_search_hook_internal (aname, refonly, FALSE);

	return res;
}

/**
 * mono_assembly_loaded:
 * @aname: an assembly to look for.
 *
 * Returns: NULL If the given @aname assembly has not been loaded, or a pointer to
 * a MonoAssembly that matches the MonoAssemblyName specified.
 */
MonoAssembly*
mono_assembly_loaded (MonoAssemblyName *aname)
{
	return mono_assembly_loaded_full (aname, FALSE);
}

void
mono_assembly_release_gc_roots (MonoAssembly *assembly)
{
	if (assembly == NULL || assembly == REFERENCE_MISSING)
		return;

	if (assembly->dynamic) {
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
	GSList *tmp;
	g_return_val_if_fail (assembly != NULL, FALSE);

	if (assembly == REFERENCE_MISSING)
		return FALSE;

	/* Might be 0 already */
	if (InterlockedDecrement (&assembly->ref_count) > 0)
		return FALSE;

	mono_profiler_assembly_event (assembly, MONO_PROFILE_START_UNLOAD);

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Unloading assembly %s [%p].", assembly->aname.name, assembly);

	mono_debug_close_image (assembly->image);

	mono_assemblies_lock ();
	loaded_assemblies = g_list_remove (loaded_assemblies, assembly);
	mono_assemblies_unlock ();

	assembly->image->assembly = NULL;

	if (!mono_image_close_except_pools (assembly->image))
		assembly->image = NULL;

	for (tmp = assembly->friend_assembly_names; tmp; tmp = tmp->next) {
		MonoAssemblyName *fname = tmp->data;
		mono_assembly_name_free (fname);
		g_free (fname);
	}
	g_slist_free (assembly->friend_assembly_names);
	g_free (assembly->basedir);

	mono_profiler_assembly_event (assembly, MONO_PROFILE_END_UNLOAD);

	return TRUE;
}

void
mono_assembly_close_finish (MonoAssembly *assembly)
{
	g_assert (assembly && assembly != REFERENCE_MISSING);

	if (assembly->image)
		mono_image_close_finish (assembly->image);

	if (assembly->dynamic) {
		g_free ((char*)assembly->aname.culture);
	} else {
		g_free (assembly);
	}
}

/**
 * mono_assembly_close:
 * @assembly: the assembly to release.
 *
 * This method releases a reference to the @assembly.  The assembly is
 * only released when all the outstanding references to it are released.
 */
void
mono_assembly_close (MonoAssembly *assembly)
{
	if (mono_assembly_close_except_image_pools (assembly))
		mono_assembly_close_finish (assembly);
}

MonoImage*
mono_assembly_load_module (MonoAssembly *assembly, guint32 idx)
{
	return mono_image_load_file_for_image (assembly->image, idx);
}

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
	GSList *l;

	DeleteCriticalSection (&assemblies_mutex);

	for (l = loaded_assembly_bindings; l; l = l->next) {
		MonoAssemblyBindingInfo *info = l->data;

		mono_assembly_binding_info_free (info);
		g_free (info);
	}
	g_slist_free (loaded_assembly_bindings);

	free_assembly_load_hooks ();
	free_assembly_search_hooks ();
	free_assembly_preload_hooks ();
}

/*LOCKING assumes loader lock is held*/
void
mono_assembly_cleanup_domain_bindings (guint32 domain_id)
{
	GSList **iter = &loaded_assembly_bindings;

	while (*iter) {
		GSList *l = *iter;
		MonoAssemblyBindingInfo *info = l->data;

		if (info->domain_id == domain_id) {
			*iter = l->next;
			mono_assembly_binding_info_free (info);
			g_free (info);
			g_slist_free_1 (l);
		} else {
			iter = &l->next;
		}
	}
}

/*
 * Holds the assembly of the application, for
 * System.Diagnostics.Process::MainModule
 */
static MonoAssembly *main_assembly=NULL;

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
 * @assembly: The assembly to retrieve the image from
 *
 * Returns: the MonoImage associated with this assembly.
 */
MonoImage*
mono_assembly_get_image (MonoAssembly *assembly)
{
	return assembly->image;
}

void
mono_register_bundled_assemblies (const MonoBundledAssembly **assemblies)
{
	bundles = assemblies;
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

	for (i = 0; i < t->rows; ++i) {
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
