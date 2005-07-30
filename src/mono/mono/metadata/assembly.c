/*
 * assembly.c: Routines for loading assemblies.
 * 
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * (C) 2001 Ximian, Inc.  http://www.ximian.com
 *
 */
#include <config.h>
#include <stdio.h>
#include <glib.h>
#include <errno.h>
#include <string.h>
#include <stdlib.h>
#include "assembly.h"
#include "image.h"
#include "cil-coff.h"
#include "rawbuffer.h"
#include <mono/metadata/loader.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/domain-internals.h>
#include <mono/io-layer/io-layer.h>
#include <mono/utils/mono-uri.h>
#include <mono/metadata/mono-config.h>
#include <mono/utils/mono-digest.h>
#include <mono/utils/mono-logger.h>
#ifdef PLATFORM_WIN32
#include <mono/os/util.h>
#ifdef _MSC_VER
	/* not used on Windows - see mono_set_rootdir () */
	#define MONO_ASSEMBLIES		NULL
#endif
#endif

/* AssemblyVersionMap: an assembly name and the assembly version set on which it is based */
typedef struct  {
	const char* assembly_name;
	guint8 version_set_index;
} AssemblyVersionMap;

/* the default search path is just MONO_ASSEMBLIES */
static const char*
default_path [] = {
	MONO_ASSEMBLIES,
	NULL
};

/* Contains the list of directories to be searched for assemblies (MONO_PATH) */
static char **assemblies_path = NULL;

/* Contains the list of directories that point to auxiliary GACs */
static char **extra_gac_paths = NULL;

/* The list of system assemblies what will be remapped to the running
 * runtime version. WARNING: this list must be sorted.
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
	{"Mono.Data.SqliteClient", 0},
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
	{"System.Configuration.Install", 0},
	{"System.Data", 0},
	{"System.Data.OracleClient", 0},
	{"System.Data.SqlXml", 0},
	{"System.Design", 0},
	{"System.DirectoryServices", 0},
	{"System.Drawing", 0},
	{"System.Drawing.Design", 0},
	{"System.EnterpriseServices", 0},
	{"System.Management", 0},
	{"System.Messaging", 0},
	{"System.Runtime.Remoting", 0},
	{"System.Runtime.Serialization.Formatters.Soap", 0},
	{"System.Security", 0},
	{"System.ServiceProcess", 0},
	{"System.Web", 0},
	{"System.Web.Mobile", 0},
	{"System.Web.Services", 0},
	{"System.Windows.Forms", 0},
	{"System.Xml", 0},
	{"mscorlib", 0}
};

/*
 * keeps track of loaded assemblies
 */
static GList *loaded_assemblies = NULL;
static MonoAssembly *corlib;

/* This protects loaded_assemblies and image->references */
static CRITICAL_SECTION assemblies_mutex;

/* A hastable of thread->assembly list mappings */
static GHashTable *assemblies_loading;

/* A hashtable of reflection only load thread->assemblies mappings */
static GHashTable *assemblies_refonly_loading;

/* If defined, points to the bundled assembly information */
const MonoBundledAssembly **bundles;

/* Reflection only private hook functions */
static MonoAssembly* mono_assembly_refonly_invoke_search_hook (MonoAssemblyName *aname);

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

static void
check_path_env (void)
{
	const char *path;
	char **splitted;
	
	path = g_getenv ("MONO_PATH");
	if (!path)
		return;

	splitted = g_strsplit (path, G_SEARCHPATH_SEPARATOR_S, 1000);
	if (assemblies_path)
		g_strfreev (assemblies_path);
	assemblies_path = splitted;
	if (g_getenv ("MONO_DEBUG") == NULL)
		return;

	while (*splitted) {
		if (**splitted && !g_file_test (*splitted, G_FILE_TEST_IS_DIR))
			g_warning ("'%s' in MONO_PATH doesn't exist or has wrong permissions.", *splitted);

		splitted++;
	}
}

static void
check_extra_gac_path_env (void) {
	const char *path;
	char **splitted;
	
	path = g_getenv ("MONO_GAC_PREFIX");
	if (!path)
		return;

	splitted = g_strsplit (path, G_SEARCHPATH_SEPARATOR_S, 1000);
	if (extra_gac_paths)
		g_strfreev (extra_gac_paths);
	extra_gac_paths = splitted;
	if (g_getenv ("MONO_DEBUG") == NULL)
		return;

	while (*splitted) {
		if (**splitted && !g_file_test (*splitted, G_FILE_TEST_IS_DIR))
			g_warning ("'%s' in MONO_GAC_PATH doesn't exist or has wrong permissions.", *splitted);

		splitted++;
	}
}

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

	if (strcmp (l->public_key_token, r->public_key_token))
		return FALSE;

	return TRUE;
}

static MonoAssembly*
search_loaded (MonoAssemblyName* aname, gboolean refonly)
{
	GList *tmp;
	MonoAssembly *ass;
	GList *loading;

	ass = refonly ? mono_assembly_refonly_invoke_search_hook (aname) : mono_assembly_invoke_search_hook (aname);
	if (ass)
		return ass;
	
	/*
	 * The assembly might be under load by this thread. In this case, it is
	 * safe to return an incomplete instance to prevent loops.
	 */
	loading = g_hash_table_lookup (refonly ? assemblies_refonly_loading : assemblies_loading, GetCurrentThread ());
	for (tmp = loading; tmp; tmp = tmp->next) {
		ass = tmp->data;
		if (!mono_assembly_names_equal (aname, &ass->aname))
			continue;

		return ass;
	}

	return NULL;
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
 * assemblies.  This is used by Windows installations to compute dynamically
 * the place where the Mono assemblies are located.
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

G_CONST_RETURN gchar *
mono_assembly_getrootdir (void)
{
	return default_path [0];
}

/**
 * mono_assemblies_init:
 *
 *  Initialize global variables used by this module.
 */
void
mono_assemblies_init (void)
{
#ifdef PLATFORM_WIN32
	mono_set_rootdir ();
#endif

	check_path_env ();
	check_extra_gac_path_env ();

	InitializeCriticalSection (&assemblies_mutex);

	assemblies_loading = g_hash_table_new (NULL, NULL);
	assemblies_refonly_loading = g_hash_table_new (NULL, NULL);
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
		gchar* token = g_malloc (8);
		gchar* encoded;
		int len;

		aname->public_key = mono_metadata_blob_heap (image, cols [MONO_ASSEMBLY_PUBLIC_KEY]);
		len = mono_metadata_decode_blob_size (aname->public_key, (const char**)&aname->public_key);

		mono_digest_get_public_token (token, aname->public_key, len);
		encoded = encode_public_tok (token, 8);
		g_strlcpy (aname->public_key_token, encoded, MONO_PUBLIC_KEY_TOKEN_LENGTH);

		g_free (encoded);
		g_free (token);
	}
	else {
		aname->public_key = NULL;
		memset (aname->public_key_token, 0, MONO_PUBLIC_KEY_TOKEN_LENGTH);
	}

	if (cols [MONO_ASSEMBLY_PUBLIC_KEY]) {
		aname->public_key = mono_metadata_blob_heap (image, cols [MONO_ASSEMBLY_PUBLIC_KEY]);
	}
	else
		aname->public_key = 0;

	return TRUE;
}

static gchar*
assemblyref_public_tok (MonoImage *image, guint32 key_index, guint32 flags)
{
	const gchar *public_tok;
	int len;

	public_tok = mono_metadata_blob_heap (image, key_index);
	len = mono_metadata_decode_blob_size (public_tok, &public_tok);

	if (flags & ASSEMBLYREF_FULL_PUBLIC_KEY_FLAG) {
		gchar token [8];
		mono_digest_get_public_token (token, public_tok, len);
		return encode_public_tok (token, 8);
	}

	return encode_public_tok (public_tok, len);
}

void
mono_assembly_addref (MonoAssembly *assembly)
{
	InterlockedIncrement (&assembly->ref_count);
}

static MonoAssemblyName *
mono_assembly_remap_version (MonoAssemblyName *aname, MonoAssemblyName *dest_aname)
{
	const MonoRuntimeInfo *current_runtime;
	int pos, first, last;

	if (aname->name == NULL) return aname;
	current_runtime = mono_get_runtime_info ();

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
	return aname;
}

void
mono_assembly_load_reference (MonoImage *image, int index)
{
	MonoTableInfo *t;
	guint32 cols [MONO_ASSEMBLYREF_SIZE];
	const char *hash;
	MonoAssembly *reference;
	MonoAssemblyName aname;
	MonoImageOpenStatus status;

	/*
	 * image->references is shared between threads, so we need to access
	 * it inside a critical section.
	 */
	EnterCriticalSection (&assemblies_mutex);
	reference = image->references [index];
	LeaveCriticalSection (&assemblies_mutex);
	if (reference)
		return;

	t = &image->tables [MONO_TABLE_ASSEMBLYREF];

	mono_metadata_decode_row (t, index, cols, MONO_ASSEMBLYREF_SIZE);
		
	hash = mono_metadata_blob_heap (image, cols [MONO_ASSEMBLYREF_HASH_VALUE]);
	aname.hash_len = mono_metadata_decode_blob_size (hash, &hash);
	aname.hash_value = hash;
	aname.name = mono_metadata_string_heap (image, cols [MONO_ASSEMBLYREF_NAME]);
	aname.culture = mono_metadata_string_heap (image, cols [MONO_ASSEMBLYREF_CULTURE]);
	aname.flags = cols [MONO_ASSEMBLYREF_FLAGS];
	aname.major = cols [MONO_ASSEMBLYREF_MAJOR_VERSION];
	aname.minor = cols [MONO_ASSEMBLYREF_MINOR_VERSION];
	aname.build = cols [MONO_ASSEMBLYREF_BUILD_NUMBER];
	aname.revision = cols [MONO_ASSEMBLYREF_REV_NUMBER];

	if (cols [MONO_ASSEMBLYREF_PUBLIC_KEY]) {
		gchar *token = assemblyref_public_tok (image, cols [MONO_ASSEMBLYREF_PUBLIC_KEY], aname.flags);
		g_strlcpy (aname.public_key_token, token, MONO_PUBLIC_KEY_TOKEN_LENGTH);
		g_free (token);
	} else {
		memset (aname.public_key_token, 0, MONO_PUBLIC_KEY_TOKEN_LENGTH);
	}

	if (image->assembly->ref_only) {
		/* We use the loaded corlib */
		if (!strcmp (aname.name, "mscorlib"))
			reference = mono_assembly_load_full (&aname, image->assembly->basedir, &status, FALSE);
		else
			reference = mono_assembly_loaded_full (&aname, TRUE);
		/*
		 * Here we must advice that the error was due to
		 * a non loaded reference using the ReflectionOnly api
		*/
		if (!reference)
			reference = (gpointer)-1;
	} else
		reference = mono_assembly_load (&aname, image->assembly->basedir, &status);

	if (reference == NULL){
		char *extra_msg = g_strdup ("");

		if (status == MONO_IMAGE_ERROR_ERRNO && errno == ENOENT) {
			extra_msg = g_strdup_printf ("The assembly was not found in the Global Assembly Cache, a path listed in the MONO_PATH environment variable, or in the location of the executing assembly (%s).\n", image->assembly->basedir);
		} else if (status == MONO_IMAGE_ERROR_ERRNO) {
			extra_msg = g_strdup_printf ("System error: %s\n", strerror (errno));
		} else if (status == MONO_IMAGE_MISSING_ASSEMBLYREF) {
			extra_msg = g_strdup ("Cannot find an assembly referenced from this one.\n");
		} else if (status == MONO_IMAGE_IMAGE_INVALID) {
			extra_msg = g_strdup ("The file exists but is not a valid assembly.\n");
		}
		
		g_warning ("The following assembly referenced from %s could not be loaded:\n"
				   "     Assembly:   %s    (assemblyref_index=%d)\n"
				   "     Version:    %d.%d.%d.%d\n"
				   "     Public Key: %s\n%s",
				   image->name, aname.name, index,
				   aname.major, aname.minor, aname.build, aname.revision,
				   strlen(aname.public_key_token) == 0 ? "(none)" : (char*)aname.public_key_token, extra_msg);
		g_free (extra_msg);
	}

	EnterCriticalSection (&assemblies_mutex);
	if (reference == NULL) {
		/* Flag as not found */
		reference = (gpointer)-1;
	} else {
		mono_assembly_addref (reference);
	}	

	if (!image->references [index])
		image->references [index] = reference;
	LeaveCriticalSection (&assemblies_mutex);

	if (image->references [index] != reference) {
		/* Somebody loaded it before us */
		mono_assembly_close (reference);
	}
}

void
mono_assembly_load_references (MonoImage *image, MonoImageOpenStatus *status)
{
	MonoTableInfo *t;
	int i;

	*status = MONO_IMAGE_OK;

	t = &image->tables [MONO_TABLE_ASSEMBLYREF];
	
	image->references = g_new0 (MonoAssembly *, t->rows + 1);

	/* resolve assembly references for modules */
	for (i = 0; i < image->module_count; i++){
		if (image->modules [i]) {
			image->modules [i]->assembly = image->assembly;
			mono_assembly_load_references (image->modules [i], status);
		}
	}
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

typedef struct AssemblySearchHook AssemblySearchHook;
struct AssemblySearchHook {
	AssemblySearchHook *next;
	MonoAssemblySearchFunc func;
	gpointer user_data;
};

AssemblySearchHook *assembly_search_hook = NULL;
static AssemblySearchHook *assembly_refonly_search_hook = NULL;

MonoAssembly*
mono_assembly_invoke_search_hook (MonoAssemblyName *aname)
{
	AssemblySearchHook *hook;

	for (hook = assembly_search_hook; hook; hook = hook->next) {
		MonoAssembly *ass = hook->func (aname, hook->user_data);
		if (ass)
			return ass;
	}

	return NULL;
}

static MonoAssembly*
mono_assembly_refonly_invoke_search_hook (MonoAssemblyName *aname)
{
	AssemblySearchHook *hook;

	for (hook = assembly_refonly_search_hook; hook; hook = hook->next) {
		MonoAssembly *ass = hook->func (aname, hook->user_data);
		if (ass)
			return ass;
	}

	return NULL;
}

void          
mono_install_assembly_search_hook (MonoAssemblySearchFunc func, gpointer user_data)
{
	AssemblySearchHook *hook;
	
	g_return_if_fail (func != NULL);

	hook = g_new0 (AssemblySearchHook, 1);
	hook->func = func;
	hook->user_data = user_data;
	hook->next = assembly_search_hook;
	assembly_search_hook = hook;
}	

void
mono_install_assembly_refonly_search_hook (MonoAssemblySearchFunc func, gpointer user_data)
{
	AssemblySearchHook *hook;

	g_return_if_fail (func != NULL);

	hook = g_new0 (AssemblySearchHook, 1);
	hook->func = func;
	hook->user_data = user_data;
	hook->next = assembly_refonly_search_hook;
	assembly_refonly_search_hook = hook;
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

	if (g_path_is_absolute (filename))
		return g_path_get_dirname (filename);

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
	for (tmp = list; tmp && tmp->next != NULL; tmp = tmp->next)
		if (tmp->data)
			g_string_append_printf (result, "%s%c", (char *) tmp->data,
								G_DIR_SEPARATOR);
	
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
static MonoImage *
mono_assembly_open_from_bundle (const char *filename, MonoImageOpenStatus *status, gboolean refonly)
{
	int i;
	char *name = g_path_get_basename (filename);
	MonoImage *image = NULL;

	/*
	 * we do a very simple search for bundled assemblies: it's not a general 
	 * purpose assembly loading mechanism.
	 */
	EnterCriticalSection (&assemblies_mutex);
	for (i = 0; !image && bundles [i]; ++i) {
		if (strcmp (bundles [i]->name, name) == 0) {
			image = mono_image_open_from_data_full ((char*)bundles [i]->data, bundles [i]->size, FALSE, status, refonly);
			break;
		}
	}
	LeaveCriticalSection (&assemblies_mutex);
	g_free (name);
	if (image) {
		mono_image_addref (image);
		return image;
	}
	return NULL;
}

static MonoImage*
do_mono_assembly_open (const char *filename, MonoImageOpenStatus *status, gboolean refonly)
{
	MonoImage *image = NULL;

	if (bundles != NULL){
		image = mono_assembly_open_from_bundle (filename, status, refonly);

		if (image != NULL)
			return image;
	}
	EnterCriticalSection (&assemblies_mutex);
	image = mono_image_open_full (filename, status, refonly);
	LeaveCriticalSection (&assemblies_mutex);

	return image;
}

MonoAssembly *
mono_assembly_open_full (const char *filename, MonoImageOpenStatus *status, gboolean refonly)
{
	MonoImage *image;
	MonoAssembly *ass;
	MonoImageOpenStatus def_status;
	gchar *fname;
	
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
			"Assembly Loader probing location: '%s'.", filename);
	image = do_mono_assembly_open (fname, status, refonly);

	if (!image){
		*status = MONO_IMAGE_ERROR_ERRNO;
		g_free (fname);
		return NULL;
	}

	ass = mono_assembly_load_from_full (image, fname, status, refonly);

	if (ass) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY,
				"Assembly Loader loaded assembly from location: '%s'.", filename);
		if (!refonly)
			mono_config_for_assembly (ass->image);
	}

	g_free (fname);

	return ass;
}

/**
 * mono_assembly_open:
 * @filename: Opens the assembly pointed out by this name
 * @status: where a status code can be returned
 *
 * mono_assembly_open opens the PE-image pointed by @filename, and
 * loads any external assemblies referenced by it.
 *
 * NOTE: we could do lazy loading of the assemblies.  Or maybe not worth
 * it. 
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
	GList *loading;
	GHashTable *ass_loading;

#if defined (PLATFORM_WIN32)
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
	 * To avoid deadlocks and scalability problems, we load assemblies outside
	 * the assembly lock. This means that multiple threads might try to load
	 * the same assembly at the same time. The first one to load it completely
	 * "wins", the other threads free their copy and use the one loaded by
	 * the winning thread.
	 */

	/*
	 * Create assembly struct, and enter it into the assembly cache
	 */
	ass = g_new0 (MonoAssembly, 1);
	ass->basedir = base_dir;
	ass->ref_only = refonly;
	ass->image = image;
	ass->ref_count = 1;

	mono_assembly_fill_assembly_name (image, &ass->aname);

	/* 
	 * Atomically search the loaded list and add ourselves to it if necessary.
	 */
	EnterCriticalSection (&assemblies_mutex);
	if (ass->aname.name) {
		/* avoid loading the same assembly twice for now... */
		ass2 = search_loaded (&ass->aname, refonly);
		if (ass2) {
			g_free (ass);
			g_free (base_dir);
			mono_image_close (image);
			*status = MONO_IMAGE_OK;
			LeaveCriticalSection (&assemblies_mutex);
			return ass2;
		}
	}
	ass_loading = refonly ? assemblies_refonly_loading : assemblies_loading;
	loading = g_hash_table_lookup (ass_loading, GetCurrentThread ());
	loading = g_list_prepend (loading, ass);
	g_hash_table_insert (ass_loading, GetCurrentThread (), loading);
	LeaveCriticalSection (&assemblies_mutex);

	image->assembly = ass;

	mono_assembly_load_references (image, status);

	EnterCriticalSection (&assemblies_mutex);

	loading = g_hash_table_lookup (ass_loading, GetCurrentThread ());
	loading = g_list_remove (loading, ass);
	if (loading == NULL)
		/* Prevent memory leaks */
		g_hash_table_remove (ass_loading, GetCurrentThread ());
	else
		g_hash_table_insert (ass_loading, GetCurrentThread (), loading);
	if (*status != MONO_IMAGE_OK) {
		LeaveCriticalSection (&assemblies_mutex);
		mono_assembly_close (ass);
		return NULL;
	}

	if (ass->aname.name) {
		ass2 = search_loaded (&ass->aname, refonly);
		if (ass2) {
			/* Somebody else has loaded the assembly before us */
			LeaveCriticalSection (&assemblies_mutex);
			mono_assembly_close (ass);
			return ass2;
		}
	}

	loaded_assemblies = g_list_prepend (loaded_assemblies, ass);
	LeaveCriticalSection (&assemblies_mutex);

	mono_assembly_invoke_load_hook (ass);

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
build_assembly_name (const char *name, const char *version, const char *culture, const char *token, MonoAssemblyName *aname)
{
	gint major, minor, build, revision;

	memset (aname, 0, sizeof (MonoAssemblyName));

	if (version) {
		if (sscanf (version, "%u.%u.%u.%u", &major, &minor, &build, &revision) != 4)
			return FALSE;

		aname->major = major;
		aname->minor = minor;
		aname->build = build;
		aname->revision = revision;
	}
	
	aname->name = g_strdup (name);
	
	if (culture) {
		if (g_strcasecmp (culture, "neutral") == 0)
			aname->culture = g_strdup ("");
		else
			aname->culture = g_strdup (culture);
	}
	
	if (token && strncmp (token, "null", 4) != 0)
		g_strlcpy ((char*)aname->public_key_token, token, MONO_PUBLIC_KEY_TOKEN_LENGTH);
	
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
	
	res = build_assembly_name (name, parts[0], parts[1], parts[2], aname);
	g_strfreev (parts);
	return res;
}

/**
* mono_assembly_name_parse:
* @name: name to parse
* @aname: the destination assembly name
* Returns: true if the name could be parsed.
* 
* Parses an assembly qualified type name and assigns the name,
* version, culture and token to the provided assembly name object.
*/
gboolean
mono_assembly_name_parse (const char *name, MonoAssemblyName *aname)
{
	gchar *dllname;
	gchar *version = NULL;
	gchar *culture = NULL;
	gchar *token = NULL;
	gboolean res;
	gchar *value;
	gchar **parts;
	gchar **tmp;

	parts = tmp = g_strsplit (name, ",", 4);
	if (!tmp || !*tmp) {
		g_strfreev (tmp);
		return FALSE;
	}

	dllname = g_strstrip (*tmp);
	
	tmp++;

	while (*tmp) {
		value = g_strstrip (*tmp);
		if (!g_ascii_strncasecmp (value, "Version=", 8)) {
			version = g_strstrip (value + 8);
			tmp++;
			continue;
		}

		if (!g_ascii_strncasecmp (value, "Culture=", 8)) {
			culture = g_strstrip (value + 8);
			tmp++;
			continue;
		}

		if (!g_ascii_strncasecmp (value, "PublicKeyToken=", 15)) {
			token = g_strstrip (value + 15);
			tmp++;
			continue;
		}
		
		g_strfreev (parts);
		return FALSE;
	}

	res = build_assembly_name (dllname, version, culture, token, aname);
	g_strfreev (parts);
	return res;
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
		
		parse_assembly_directory_name (aname->name, direntry, &gac_aname);
		
		if (aname->culture != NULL && strcmp (aname->culture, gac_aname.culture) != 0)
			match = FALSE;
			
		if (match && strlen ((char*)aname->public_key_token) > 0 && 
				strcmp ((char*)aname->public_key_token, (char*)gac_aname.public_key_token) != 0)
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
	MonoAssemblyName *aname, base_name, maped_aname;
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
		aname = mono_assembly_remap_version (aname, &maped_aname);
	
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

	g_free (fullname);
	mono_assembly_name_free (aname);

	return res;
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
		culture = g_strdup (aname->culture);
		g_strdown (culture);
	} else {
		culture = g_strdup ("");
	}

	version = g_strdup_printf ("%d.%d.%d.%d_%s_%s", aname->major,
			aname->minor, aname->build, aname->revision,
			culture, aname->public_key_token);
	
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

	if (corlib) {
		/* g_print ("corlib already loaded\n"); */
		return corlib;
	}
	
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

	return corlib;
}


MonoAssembly*
mono_assembly_load_full (MonoAssemblyName *aname, const char *basedir, MonoImageOpenStatus *status, gboolean refonly)
{
	MonoAssembly *result;
	char *fullpath, *filename;
	MonoAssemblyName maped_aname;

	aname = mono_assembly_remap_version (aname, &maped_aname);
	
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

	if (strstr (aname->name, ".dll"))
		filename = g_strdup (aname->name);
	else
		filename = g_strconcat (aname->name, ".dll", NULL);

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
	return result;
}

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

	EnterCriticalSection (&assemblies_mutex);
	res = search_loaded (aname, refonly);
	LeaveCriticalSection (&assemblies_mutex);

	return res;
}

MonoAssembly*
mono_assembly_loaded (MonoAssemblyName *aname)
{
	return mono_assembly_loaded_full (aname, FALSE);
}

void
mono_assembly_close (MonoAssembly *assembly)
{
	g_return_if_fail (assembly != NULL);

	if (InterlockedDecrement (&assembly->ref_count))
		return;
	
	EnterCriticalSection (&assemblies_mutex);
	loaded_assemblies = g_list_remove (loaded_assemblies, assembly);
	LeaveCriticalSection (&assemblies_mutex);
	/* assemblies belong to domains, so the domain code takes care of unloading the
	 * referenced assemblies
	 */

	mono_image_close (assembly->image);

	g_free (assembly->basedir);
	if (!assembly->dynamic)
		g_free (assembly);
}

MonoImage*
mono_assembly_load_module (MonoAssembly *assembly, guint32 idx)
{
	MonoImageOpenStatus status;
	MonoImage *module;

	module = mono_image_load_file_for_image (assembly->image, idx);
	if (module)
		mono_assembly_load_references (module, &status);

	return module;
}

void
mono_assembly_foreach (GFunc func, gpointer user_data)
{
	GList *copy;

	/*
	 * We make a copy of the list to avoid calling the callback inside the 
	 * lock, which could lead to deadlocks.
	 */
	EnterCriticalSection (&assemblies_mutex);
	copy = g_list_copy (loaded_assemblies);
	LeaveCriticalSection (&assemblies_mutex);

	g_list_foreach (loaded_assemblies, func, user_data);

	g_list_free (copy);
}

/*
 * Holds the assembly of the application, for
 * System.Diagnostics.Process::MainModule
 */
static MonoAssembly *main_assembly=NULL;

void
mono_assembly_set_main (MonoAssembly *assembly)
{
	main_assembly=assembly;
}

MonoAssembly *
mono_assembly_get_main (void)
{
	return(main_assembly);
}

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
