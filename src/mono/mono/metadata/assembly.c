/*
 * assembly.c: Routines for loading assemblies.
 * 
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * (C) 2001 Ximian, Inc.  http://www.ximian.com
 *
 * TODO:
 *   Implement big-endian versions of the reading routines.
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
#ifdef WITH_BUNDLE
#include "mono-bundle.h"
#endif
#include <mono/io-layer/io-layer.h>

/* the default search path is just MONO_ASSEMBLIES */
static const char*
default_path [] = {
	MONO_ASSEMBLIES,
	NULL
};

static char **assemblies_path = NULL;

/*
 * keeps track of loaded assemblies
 */
static GList *loaded_assemblies = NULL;
static MonoAssembly *corlib;

/* This protects loaded_assemblies and image->references */
static CRITICAL_SECTION assemblies_mutex;

/* A hastable of thread->assembly list mappings */
static GHashTable *assemblies_loading;

#ifdef PLATFORM_WIN32

static void
init_default_path (void)
{
	int i;

	default_path [0] = g_strdup (MONO_ASSEMBLIES);
	for (i = strlen (MONO_ASSEMBLIES) - 1; i >= 0; i--) {
		if (default_path [0][i] == '/')
			((char*) default_path [0])[i] = '\\';
	}
}
#endif

static void
check_env (void) {
	const char *path;
	char **splitted;
	
	path = getenv ("MONO_PATH");
	if (!path)
		return;
	splitted = g_strsplit (path, G_SEARCHPATH_SEPARATOR_S, 1000);
	if (assemblies_path)
		g_strfreev (assemblies_path);
	assemblies_path = splitted;
}

/* assemblies_mutex must be held by the caller */
static MonoAssembly*
search_loaded (MonoAssemblyName* aname)
{
	GList *tmp;
	MonoAssembly *ass;
	GList *loading;
	
	for (tmp = loaded_assemblies; tmp; tmp = tmp->next) {
		ass = tmp->data;
		if (!ass->aname.name)
			continue;
		/* we just compare the name, but later we'll do all the checks */
		/* g_print ("compare %s %s\n", aname->name, ass->aname.name); */
		if (strcmp (aname->name, ass->aname.name))
			continue;
		/* g_print ("success\n"); */

		return ass;
	}

	/*
	 * The assembly might be under load by this thread. In this case, it is
	 * safe to return an incomplete instance to prevent loops.
	 */
	loading = g_hash_table_lookup (assemblies_loading, GetCurrentThread ());
	for (tmp = loading; tmp; tmp = tmp->next) {
		ass = tmp->data;
		if (!ass->aname.name)
			continue;
		if (strcmp (aname->name, ass->aname.name))
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

/**
 * mono_assemblies_init:
 *
 *  Initialize global variables used by this module.
 */
void
mono_assemblies_init (void)
{
#ifdef PLATFORM_WIN32
	init_default_path ();
#endif

	check_env ();

	InitializeCriticalSection (&assemblies_mutex);

	assemblies_loading = g_hash_table_new (NULL, NULL);
}

static void
mono_assembly_load_references (MonoImage *image, MonoImageOpenStatus *status)
{
	MonoTableInfo *t;
	guint32 cols [MONO_ASSEMBLYREF_SIZE];
	const char *hash;
	int i;

	*status = MONO_IMAGE_OK;

	t = &image->tables [MONO_TABLE_ASSEMBLYREF];

	image->references = g_new0 (MonoAssembly *, t->rows + 1);

	/*
	 * Load any assemblies this image references
	 */
	for (i = 0; i < t->rows; i++) {
		MonoAssemblyName aname;

		mono_metadata_decode_row (t, i, cols, MONO_ASSEMBLYREF_SIZE);
		
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

		image->references [i] = mono_assembly_load (&aname, image->assembly->basedir, status);

		if (image->references [i] == NULL){
			int j;
			
			for (j = 0; j < i; j++)
				mono_assembly_close (image->references [j]);
			g_free (image->references);

			g_warning ("Could not find assembly %s", aname.name);
			*status = MONO_IMAGE_MISSING_ASSEMBLYREF;
			return;
		}

		/* 
		 * This check is disabled since lots of people seem to have an older
		 * corlib which triggers this.
		 */
		/* 
		if (image->references [i]->image == image)
			g_error ("Error: Assembly %s references itself", image->name);
		*/
	}
	image->references [i] = NULL;

	/* resolve assembly references for modules */
	t = &image->tables [MONO_TABLE_MODULEREF];
	for (i = 0; i < t->rows; i++){
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

static MonoImage*
do_mono_assembly_open (const char *filename, MonoImageOpenStatus *status)
{
	MonoImage *image = NULL;
#ifdef WITH_BUNDLE
	int i;
	char *name = g_path_get_basename (filename);
	char *dot = strrchr (name, '.');
	GList *tmp;
	MonoAssembly *ass;
	
	if (dot)
		*dot = 0;
	if (strcmp (name, "corlib") == 0) {
		g_free (name);
		name = g_strdup ("mscorlib");
	}
	/* we do a very simple search for bundled assemblies: it's not a general 
	 * purpose assembly loading mechanism.
	 */
	EnterCriticalSection (&assemblies_mutex);
	for (tmp = loaded_assemblies; tmp; tmp = tmp->next) {
		ass = tmp->data;
		if (!ass->aname.name)
			continue;
		if (strcmp (name, ass->aname.name))
			continue;
		image = ass->image;
		break;
	}
	LeaveCriticalSection (&assemblies_mutex);
	for (i = 0; !image && bundled_assemblies [i]; ++i) {
		if (strcmp (bundled_assemblies [i]->name, name) == 0) {
			image = mono_image_open_from_data ((char*)bundled_assemblies [i]->data, bundled_assemblies [i]->size, FALSE, status);
			break;
		}
	}
	g_free (name);
	if (image) {
		InterlockedIncrement (&image->ref_count);
		return image;
	}
#endif
	image = mono_image_open (filename, status);
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
	MonoAssembly *ass, *ass2;
	MonoImage *image;
	MonoTableInfo *t;
	guint32 cols [MONO_ASSEMBLY_SIZE];
	char *base_dir;
	MonoImageOpenStatus def_status;
	gchar *fname;
	GList *loading;
	
	g_return_val_if_fail (filename != NULL, NULL);

	if (!status)
		status = &def_status;
	*status = MONO_IMAGE_OK;

	if (strncmp (filename, "file://", 7) == 0) {
		GError *error = NULL;
		gchar *uri = (gchar *) filename;

		/*
		 * MS allows file://c:/... and fails on file://localhost/c:/... 
		 * They also throw an IndexOutOfRangeException if "file://"
		 */
		if (uri [7] != '/')
			uri = g_strdup_printf ("file:///%s", uri + 7);
	
		fname = g_filename_from_uri (uri, NULL, &error);
		if (uri != filename)
			g_free (uri);

		if (error != NULL) {
			g_warning ("%s\n", error->message);
			g_error_free (error);
			fname = g_strdup (filename);
		}
	} else {
		fname = g_strdup (filename);
	}

	/* g_print ("file loading %s\n", fname); */
	image = do_mono_assembly_open (fname, status);

	if (!image){
		*status = MONO_IMAGE_ERROR_ERRNO;
		g_free (fname);
		return NULL;
	}

#if defined (PLATFORM_WIN32)
	{
		gchar *tmp_fn;
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
	ass->image = image;


	g_free (fname);

	t = &image->tables [MONO_TABLE_ASSEMBLY];
	if (t->rows) {
		mono_metadata_decode_row (t, 0, cols, MONO_ASSEMBLY_SIZE);

		ass->aname.hash_len = 0;
		ass->aname.hash_value = NULL;
		ass->aname.name = mono_metadata_string_heap (image, cols [MONO_ASSEMBLY_NAME]);
		ass->aname.culture = mono_metadata_string_heap (image, cols [MONO_ASSEMBLY_CULTURE]);
		ass->aname.flags = cols [MONO_ASSEMBLY_FLAGS];
		ass->aname.major = cols [MONO_ASSEMBLY_MAJOR_VERSION];
		ass->aname.minor = cols [MONO_ASSEMBLY_MINOR_VERSION];
		ass->aname.build = cols [MONO_ASSEMBLY_BUILD_NUMBER];
		ass->aname.revision = cols [MONO_ASSEMBLY_REV_NUMBER];
		ass->aname.hash_alg = cols [MONO_ASSEMBLY_HASH_ALG];
		if (cols [MONO_ASSEMBLY_PUBLIC_KEY])
			ass->aname.public_key = mono_metadata_blob_heap (image, cols [MONO_ASSEMBLY_PUBLIC_KEY]);
	}

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

	/*
	 * We load referenced assemblies outside the lock to prevent deadlocks
	 * with regards to preload hooks.
	 */
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

MonoAssembly*
mono_assembly_load (MonoAssemblyName *aname, const char *basedir, MonoImageOpenStatus *status)
{
	MonoAssembly *result;
	char *fullpath, *filename;

	result = invoke_assembly_preload_hook (aname, assemblies_path);
	if (result)
		return result;

	/* g_print ("loading %s\n", aname->name); */
	/* special case corlib */
	if ((strcmp (aname->name, "mscorlib") == 0) || (strcmp (aname->name, "corlib") == 0)) {
		if (corlib) {
			/* g_print ("corlib already loaded\n"); */
			return corlib;
		}
		/* g_print ("corlib load\n"); */
		if (assemblies_path) {
			corlib = load_in_path ("mscorlib.dll", (const char**)assemblies_path, status);
			if (corlib)
				return corlib;
			corlib = load_in_path ("corlib.dll", (const char**)assemblies_path, status);
			return corlib;
		}
		corlib = load_in_path ("mscorlib.dll", default_path, status);
		if (!corlib)
			corlib = load_in_path ("corlib.dll", default_path, status);
		return corlib;
	}
	result = search_loaded (aname);
	if (result)
		return result;
	/* g_print ("%s not found in cache\n", aname->name); */
	if (strstr (aname->name, ".dll"))
		filename = g_strdup (aname->name);
	else
		filename = g_strconcat (aname->name, ".dll", NULL);
	if (basedir) {
		fullpath = g_build_filename (basedir, filename, NULL);
		result = mono_assembly_open (fullpath, status);
		g_free (fullpath);
		if (result) {
			g_free (filename);
			return result;
		}
	}
	if (assemblies_path) {
		result = load_in_path (filename, (const char**)assemblies_path, status);
		if (result) {
			g_free (filename);
			return result;
		}
	}
	result = load_in_path (filename, default_path, status);
	g_free (filename);
	return result;
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

/* Holds the assembly of the application, for
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
