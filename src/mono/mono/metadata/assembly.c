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
#endif

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

/*
 * keeps track of loaded assemblies
 */
static GList *loaded_assemblies = NULL;
static MonoAssembly *corlib;

/* This protects loaded_assemblies and image->references */
static CRITICAL_SECTION assemblies_mutex;

/* A hastable of thread->assembly list mappings */
static GHashTable *assemblies_loading;

/* If defined, points to the bundled assembly information */
const MonoBundledAssembly **bundles;

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
search_loaded (MonoAssemblyName* aname)
{
	GList *tmp;
	MonoAssembly *ass;
	GList *loading;

	ass = mono_assembly_invoke_search_hook (aname);
	if (ass)
		return ass;
	
	/*
	 * The assembly might be under load by this thread. In this case, it is
	 * safe to return an incomplete instance to prevent loops.
	 */
	loading = g_hash_table_lookup (assemblies_loading, GetCurrentThread ());
	for (tmp = loading; tmp; tmp = tmp->next) {
		ass = tmp->data;
		if (!mono_assembly_names_equal (aname, &ass->aname))
			continue;

		return ass;
	}

	return NULL;
}

static MonoAssembly *
load_in_path (const char *basename, const char** search_path, MonoImageOpenStatus *status)
{
	int i;
	char *fullpath;
	MonoAssembly *result;

	for (i = 0; search_path [i]; ++i) {
		fullpath = g_build_filename (search_path [i], basename, NULL);
		result = mono_assembly_open (fullpath, status);
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

	reference = mono_assembly_load (&aname, image->assembly->basedir, &status);

	if (reference == NULL){
		/*
		** Temporary work around: any System.* which are 3300 build, will get
		** remapped, this is to keep old applications running that might have
		** been linked against our 5000 API, before we were strongnamed, and
		** hence were labeled as 3300 builds by reflection.c
		*/
		if (aname.build == 3300 && strncmp (aname.name, "System", 6) == 0){
			aname.build = 5000;
				
			reference = mono_assembly_load (&aname, image->assembly->basedir, &status);
		}
		if (reference != NULL){
			if (g_getenv ("MONO_SILENT_WARNING") == NULL)
				g_printerr ("Compat mode: the request from %s to load %s was remapped (http://www.go-mono.com/remap.html)\n",
							image->name, aname.name);
			
		} else {
			char *extra_msg = g_strdup ("");

			if (status == MONO_IMAGE_ERROR_ERRNO) {
				extra_msg = g_strdup_printf ("System error: %s\n", strerror (errno));
			} else if (status == MONO_IMAGE_MISSING_ASSEMBLYREF) {
				extra_msg = g_strdup ("Cannot find an assembly referenced from this one.\n");
			} else if (status == MONO_IMAGE_IMAGE_INVALID) {
				extra_msg = g_strdup ("The file exists but is not a valid assembly.\n");
			}
			
			g_warning ("Could not find assembly %s, references from %s (assemblyref_index=%d)\n"
					   "     Major/Minor: %d,%d\n"
					   "     Build:       %d,%d\n"
					   "     Token:       %s\n%s",
					   aname.name, image->name, index,
					   aname.major, aname.minor, aname.build, aname.revision,
					   aname.public_key_token, extra_msg);
			g_free (extra_msg);
		}
	}

	if (reference == NULL)
		/* Flag as not found */
		reference = (gpointer)-1;

	EnterCriticalSection (&assemblies_mutex);
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

typedef struct AssemblyPreLoadHook AssemblyPreLoadHook;
struct AssemblyPreLoadHook {
	AssemblyPreLoadHook *next;
	MonoAssemblyPreLoadFunc func;
	gpointer user_data;
};

AssemblyPreLoadHook *assembly_preload_hook = NULL;

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
mono_assembly_open_from_bundle (const char *filename, MonoImageOpenStatus *status)
{
	int i;
	char *name = g_path_get_basename (filename);
	char *dot = strrchr (name, '.');
	MonoImage *image = NULL;

	/*
	 * we do a very simple search for bundled assemblies: it's not a general 
	 * purpose assembly loading mechanism.
	 */
	EnterCriticalSection (&assemblies_mutex);
	for (i = 0; !image && bundles [i]; ++i) {
		if (strcmp (bundles [i]->name, name) == 0) {
			image = mono_image_open_from_data ((char*)bundles [i]->data, bundles [i]->size, FALSE, status);
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
do_mono_assembly_open (const char *filename, MonoImageOpenStatus *status)
{
	MonoImage *image = NULL;

	if (bundles != NULL){
		image = mono_assembly_open_from_bundle (filename, status);

		if (image != NULL)
			return image;
	}
	EnterCriticalSection (&assemblies_mutex);
	image = mono_image_open (filename, status);
	LeaveCriticalSection (&assemblies_mutex);

	return image;
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
	image = do_mono_assembly_open (fname, status);

	if (!image){
		*status = MONO_IMAGE_ERROR_ERRNO;
		g_free (fname);
		return NULL;
	}

	ass = mono_assembly_load_from (image, fname, status);

	if (ass) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY,
				"Assembly Loader loaded assembly from location: '%s'.", filename);
		mono_config_for_assembly (ass->image);
	}

	g_free (fname);

	return ass;
}

MonoAssembly *
mono_assembly_load_from (MonoImage *image, const char*fname, 
			 MonoImageOpenStatus *status)
{
	MonoAssembly *ass, *ass2;
	char *base_dir;
	GList *loading;

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
	ass->image = image;

	mono_assembly_fill_assembly_name (image, &ass->aname);

	/* 
	 * Atomically search the loaded list and add ourselves to it if necessary.
	 */
	EnterCriticalSection (&assemblies_mutex);
	if (ass->aname.name)
		/* avoid loading the same assembly twice for now... */
		if ((ass2 = search_loaded (&ass->aname))) {
			g_free (ass);
			g_free (base_dir);
			mono_image_close (image);
			*status = MONO_IMAGE_OK;
			LeaveCriticalSection (&assemblies_mutex);
			return ass2;
		}
	loading = g_hash_table_lookup (assemblies_loading, GetCurrentThread ());
	loading = g_list_prepend (loading, ass);
	g_hash_table_insert (assemblies_loading, GetCurrentThread (), loading);
	LeaveCriticalSection (&assemblies_mutex);

	image->assembly = ass;

	mono_assembly_load_references (image, status);

	EnterCriticalSection (&assemblies_mutex);

	loading = g_hash_table_lookup (assemblies_loading, GetCurrentThread ());
	loading = g_list_remove (loading, ass);
	if (loading == NULL)
		/* Prevent memory leaks */
		g_hash_table_remove (assemblies_loading, GetCurrentThread ());
	else
		g_hash_table_insert (assemblies_loading, GetCurrentThread (), loading);
	if (*status != MONO_IMAGE_OK) {
		LeaveCriticalSection (&assemblies_mutex);
		mono_assembly_close (ass);
		return NULL;
	}

	if (ass->aname.name) {
		ass2 = search_loaded (&ass->aname);
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

static MonoAssembly*
probe_for_partial_name (const char *basepath, const char *fullname, MonoImageOpenStatus *status)
{
	MonoAssembly *res = NULL;
	gchar *fullpath;
	GDir *dirhandle;
	const char* direntry;

	dirhandle = g_dir_open (basepath, 0, NULL);
	if (!dirhandle)
		return NULL;

	while ((direntry = g_dir_read_name (dirhandle))) {
		fullpath = g_build_path (G_DIR_SEPARATOR_S, basepath, direntry, fullname, NULL);
		res = mono_assembly_open (fullpath, status);
		g_free (fullpath);
		if (res)
			break;
	}
	g_dir_close (dirhandle);

	return res;
}

MonoAssembly*
mono_assembly_load_with_partial_name (const char *name, MonoImageOpenStatus *status)
{
	MonoAssembly *res;
	MonoAssemblyName aname;
	gchar *fullname, *gacpath;
	gchar **paths;

	memset (&aname, 0, sizeof (MonoAssemblyName));
	aname.name = name;

	res = invoke_assembly_preload_hook (&aname, assemblies_path);
	if (res) {
		res->in_gac = FALSE;
		return res;
	}

	res = mono_assembly_loaded (&aname);
	if (res)
		return res;

	fullname = g_strdup_printf ("%s.dll", name);

	if (extra_gac_paths) {
		paths = extra_gac_paths;
		while (!res && *paths) {
			gacpath = g_build_path (G_DIR_SEPARATOR_S, *paths, "lib", "mono", "gac", name, NULL);
			res = probe_for_partial_name (gacpath, fullname, status);
			g_free (gacpath);
			paths++;
		}
	}

	if (res) {
		res->in_gac = TRUE;
		g_free (fullname);
		return res;
		
	}

	gacpath = g_build_path (G_DIR_SEPARATOR_S, mono_assembly_getrootdir (), "mono", "gac", name, NULL);
	res = probe_for_partial_name (gacpath, fullname, status);
	g_free (gacpath);

	if (res)
		res->in_gac = TRUE;

	g_free (fullname);

	return res;
}

/**
 * mono_assembly_load_from_gac
 *
 * @aname: The assembly name object
 */
static MonoAssembly*
mono_assembly_load_from_gac (MonoAssemblyName *aname,  gchar *filename, MonoImageOpenStatus *status)
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
			result = mono_assembly_open (fullpath, status);
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
	result = mono_assembly_open (fullpath, status);
	g_free (fullpath);

	if (result)
		result->in_gac = TRUE;
	
	g_free (subpath);

	return result;
}

	
MonoAssembly*
mono_assembly_load (MonoAssemblyName *aname, const char *basedir, MonoImageOpenStatus *status)
{
	MonoAssembly *result;
	char *fullpath, *filename;

	result = invoke_assembly_preload_hook (aname, assemblies_path);
	if (result) {
		result->in_gac = FALSE;
		return result;
	}

	result = mono_assembly_loaded (aname);
	if (result)
		return result;

	/* g_print ("loading %s\n", aname->name); */
	/* special case corlib */
	if (strcmp (aname->name, "mscorlib") == 0) {
		char *corlib_file;
		if (corlib) {
			/* g_print ("corlib already loaded\n"); */
			return corlib;
		}
		/* g_print ("corlib load\n"); */
		if (assemblies_path) {
			corlib = load_in_path ("mscorlib.dll", (const char**)assemblies_path, status);
			if (corlib)
				return corlib;
		}
		corlib = load_in_path ("mscorlib.dll", default_path, status);

		if (corlib)
			return corlib;
	
		/* Load corlib from mono/<version> */
		
		corlib_file = g_build_filename ("mono", mono_get_framework_version (), "mscorlib.dll", NULL);
		if (assemblies_path) {
			corlib = load_in_path (corlib_file, (const char**)assemblies_path, status);
			if (corlib) {
				g_free (corlib_file);
				return corlib;
			}
		}
		corlib = load_in_path (corlib_file, default_path, status);
		g_free (corlib_file);
	
		return corlib;
	}

	if (strstr (aname->name, ".dll"))
		filename = g_strdup (aname->name);
	else
		filename = g_strconcat (aname->name, ".dll", NULL);

	result = mono_assembly_load_from_gac (aname, filename, status);
	if (result) {
		g_free (filename);
		return result;
	}

	if (basedir) {
		fullpath = g_build_filename (basedir, filename, NULL);
		result = mono_assembly_open (fullpath, status);
		g_free (fullpath);
		if (result) {
			result->in_gac = FALSE;
			g_free (filename);
			return result;
		}
	}

	result = load_in_path (filename, default_path, status);
	if (result)
		result->in_gac = FALSE;
	g_free (filename);
	return result;
}

MonoAssembly*
mono_assembly_loaded (MonoAssemblyName *aname)
{
	MonoAssembly *res;

	EnterCriticalSection (&assemblies_mutex);
	res = search_loaded (aname);
	LeaveCriticalSection (&assemblies_mutex);

	return res;
}

void
mono_assembly_close (MonoAssembly *assembly)
{
	MonoImage *image;
	int i;
	
	g_return_if_fail (assembly != NULL);

	EnterCriticalSection (&assemblies_mutex);
	if (--assembly->ref_count != 0) {
		LeaveCriticalSection (&assemblies_mutex);
		return;
	}
	loaded_assemblies = g_list_remove (loaded_assemblies, assembly);
	LeaveCriticalSection (&assemblies_mutex);
	image = assembly->image;
	if (image->references) {
		for (i = 0; image->references [i] != NULL; i++)
			if (image->references [i] && (image->references [i] != (gpointer)-1))
				mono_image_close (image->references [i]->image);
		g_free (image->references);
	}

	mono_image_close (assembly->image);

	g_free (assembly->basedir);
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
