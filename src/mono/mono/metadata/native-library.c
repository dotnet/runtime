#include "config.h"
#include "mono/metadata/class-internals.h"
#include "mono/metadata/loader-internals.h"
#include "mono/metadata/loader.h"
#include "mono/utils/checked-build.h"
#include "mono/utils/mono-compiler.h"
#include "mono/utils/mono-logger-internals.h"
#include "mono/utils/mono-path.h"

#ifdef ENABLE_NETCORE
static int pinvoke_search_directories_count;
static char **pinvoke_search_directories;
#endif

#ifndef DISABLE_DLLMAP
static MonoDllMap *global_dll_map;
#endif
static GHashTable *global_module_map;

static MonoDl *internal_module;

typedef enum {
	LOOKUP_PINVOKE_ERR_OK = 0, /* No error */
	LOOKUP_PINVOKE_ERR_NO_LIB, /* DllNotFoundException */
	LOOKUP_PINVOKE_ERR_NO_SYM, /* EntryPointNotFoundException */
} MonoLookupPInvokeErr;

/* We should just use a MonoError, but mono_lookup_pinvoke_call has this legacy
 * error reporting mechanism where it returns an exception class and a string
 * message.  So instead we return an error code and message, and for internal
 * callers convert it to a MonoError.
 *
 * Don't expose this type to the runtime.  It's just an implementation
 * detail for backward compatability.
 */
typedef struct MonoLookupPInvokeStatus {
	MonoLookupPInvokeErr err_code;
	char *err_arg;
} MonoLookupPInvokeStatus;

/* Class lazy loading functions */
GENERATE_GET_CLASS_WITH_CACHE (appdomain_unloaded_exception, "System", "AppDomainUnloadedException")
GENERATE_TRY_GET_CLASS_WITH_CACHE (appdomain_unloaded_exception, "System", "AppDomainUnloadedException")

#ifndef DISABLE_DLLMAP
/*
 * LOCKING: Assumes the relevant lock is held.
 * For the global DllMap, this is `global_loader_data_mutex`, and for images it's their internal lock.
 */
static int
mono_dllmap_lookup_list (MonoDllMap *dll_map, const char *dll, const char* func, const char **rdll, const char **rfunc) {
	int found = 0;

	*rdll = dll;
	*rfunc = func;

	if (!dll_map)
		goto exit;

	/* 
	 * we use the first entry we find that matches, since entries from
	 * the config file are prepended to the list and we document that the
	 * later entries win.
	 */
	for (; dll_map; dll_map = dll_map->next) {
		// Check case-insensitively when the dll name is prefixed with 'i:'
		gboolean case_insensitive_match = strncmp (dll_map->dll, "i:", 2) == 0 && g_ascii_strcasecmp (dll_map->dll + 2, dll) == 0;
		gboolean case_sensitive_match = strcmp (dll_map->dll, dll) == 0;
		if (!(case_insensitive_match || case_sensitive_match))
			continue;

		if (!found && dll_map->target) {
			*rdll = dll_map->target;
			found = 1;
			/* we don't quit here, because we could find a full
			 * entry that also matches the function, which takes priority.
			 */
		}
		if (dll_map->func && strcmp (dll_map->func, func) == 0) {
			*rdll = dll_map->target;
			*rfunc = dll_map->target_func;
			break;
		}
	}

exit:
	*rdll = g_strdup (*rdll);
	*rfunc = g_strdup (*rfunc);
	return found;
}

/*
 * The locking and GC state transitions here are wonky due to the fact the image lock is a coop lock
 * and the global loader data lock is an OS lock.
 */
static int
mono_dllmap_lookup (MonoImage *assembly, const char *dll, const char* func, const char **rdll, const char **rfunc)
{
	int res;

	MONO_REQ_GC_UNSAFE_MODE;

	if (assembly && assembly->dll_map) {
		mono_image_lock (assembly);
		res = mono_dllmap_lookup_list (assembly->dll_map, dll, func, rdll, rfunc);
		mono_image_unlock (assembly);
		if (res)
			return res;
	}

	MONO_ENTER_GC_SAFE;

	mono_global_loader_data_lock ();
	res = mono_dllmap_lookup_list (global_dll_map, dll, func, rdll, rfunc);
	mono_global_loader_data_unlock ();

	MONO_EXIT_GC_SAFE;

	return res;
}

static void
dllmap_insert_global (const char *dll, const char *func, const char *tdll, const char *tfunc)
{
	MonoDllMap *entry;

	mono_loader_init ();

	entry = (MonoDllMap *)g_malloc0 (sizeof (MonoDllMap));
	entry->dll = dll? g_strdup (dll): NULL;
	entry->target = tdll? g_strdup (tdll): NULL;
	entry->func = func? g_strdup (func): NULL;
	entry->target_func = tfunc? g_strdup (tfunc): (func? g_strdup (func): NULL);

	// No transition here because this is early in startup
	mono_global_loader_data_lock ();
	entry->next = global_dll_map;
	global_dll_map = entry;
	mono_global_loader_data_unlock ();

}

static void
dllmap_insert_image (MonoImage *assembly, const char *dll, const char *func, const char *tdll, const char *tfunc)
{
	MonoDllMap *entry;
	g_assert (assembly != NULL);

	MONO_REQ_GC_UNSAFE_MODE;

	mono_loader_init ();

	entry = (MonoDllMap *)mono_image_alloc0 (assembly, sizeof (MonoDllMap));
	entry->dll = dll? mono_image_strdup (assembly, dll): NULL;
	entry->target = tdll? mono_image_strdup (assembly, tdll): NULL;
	entry->func = func? mono_image_strdup (assembly, func): NULL;
	entry->target_func = tfunc? mono_image_strdup (assembly, tfunc): (func? mono_image_strdup (assembly, func): NULL);

	mono_image_lock (assembly);
	entry->next = assembly->dll_map;
	assembly->dll_map = entry;
	mono_image_unlock (assembly);
}

/*
 * LOCKING: Assumes the relevant lock is held.
 * For the global DllMap, this is `global_loader_data_mutex`, and for images it's their internal lock.
 */
static void
free_dllmap (MonoDllMap *map)
{
	while (map) {
		MonoDllMap *next = map->next;

		g_free (map->dll);
		g_free (map->target);
		g_free (map->func);
		g_free (map->target_func);
		g_free (map);
		map = next;
	}
}

void
mono_dllmap_insert_internal (MonoImage *assembly, const char *dll, const char *func, const char *tdll, const char *tfunc)
{
	// The locking in here is _really_ wonky, and I'm not convinced this function should exist.
	// I've split it into an internal version to offer flexibility in the future.
	if (!assembly)
		dllmap_insert_global (dll, func, tdll, tfunc);
	else
		dllmap_insert_image (assembly, dll, func, tdll, tfunc);
}

void
mono_global_dllmap_cleanup (void)
{
	// No need for a transition here since the thread is already detached from the runtime
	mono_global_loader_data_lock ();

	free_dllmap (global_dll_map);
	global_dll_map = NULL;

	mono_global_loader_data_unlock ();
}
#endif

/**
 * mono_dllmap_insert:
 * \param assembly if NULL, this is a global mapping, otherwise the remapping of the dynamic library will only apply to the specified assembly
 * \param dll The name of the external library, as it would be found in the \c DllImport declaration.  If prefixed with <code>i:</code> the matching of the library name is done without case sensitivity
 * \param func if not null, the mapping will only applied to the named function (the value of <code>EntryPoint</code>)
 * \param tdll The name of the library to map the specified \p dll if it matches.
 * \param tfunc The name of the function that replaces the invocation.  If NULL, it is replaced with a copy of \p func.
 *
 * LOCKING: Acquires the image lock, or the loader data lock if an image is not passed.
 *
 * This function is used to programatically add \c DllImport remapping in either
 * a specific assembly, or as a global remapping.   This is done by remapping
 * references in a \c DllImport attribute from the \p dll library name into the \p tdll
 * name. If the \p dll name contains the prefix <code>i:</code>, the comparison of the 
 * library name is done without case sensitivity.
 *
 * If you pass \p func, this is the name of the \c EntryPoint in a \c DllImport if specified
 * or the name of the function as determined by \c DllImport. If you pass \p func, you
 * must also pass \p tfunc which is the name of the target function to invoke on a match.
 *
 * Example:
 *
 * <code>mono_dllmap_insert (NULL, "i:libdemo.dll", NULL, relocated_demo_path, NULL);</code>
 *
 * The above will remap \c DllImport statements for \c libdemo.dll and \c LIBDEMO.DLL to
 * the contents of \c relocated_demo_path for all assemblies in the Mono process.
 *
 * NOTE: This can be called before the runtime is initialized, for example from
 * \c mono_config_parse.
 */
void
mono_dllmap_insert (MonoImage *assembly, const char *dll, const char *func, const char *tdll, const char *tfunc)
{
#ifndef DISABLE_DLLMAP
	mono_dllmap_insert_internal (assembly, dll, func, tdll, tfunc);
#endif
}

static MonoDl*
cached_module_load (const char *name, int flags, char **err)
{
	MonoDl *res;

	if (err)
		*err = NULL;

	MONO_ENTER_GC_SAFE;

	mono_global_loader_data_lock ();

	if (!global_module_map)
		global_module_map = g_hash_table_new (g_str_hash, g_str_equal);

	res = (MonoDl *)g_hash_table_lookup (global_module_map, name);
	if (res)
		goto exit;

	res = mono_dl_open (name, flags, err);
	if (res)
		g_hash_table_insert (global_module_map, g_strdup (name), res);

exit:
	mono_global_loader_data_unlock ();

	MONO_EXIT_GC_SAFE;

	return res;
}

void
mono_loader_register_module (const char *name, MonoDl *module)
{
	// No transition here because this is early in startup
	mono_global_loader_data_lock ();

	if (!global_module_map)
		global_module_map = g_hash_table_new (g_str_hash, g_str_equal);

	g_hash_table_insert (global_module_map, g_strdup (name), module);

	mono_global_loader_data_unlock ();
}

static void
remove_cached_module (gpointer key, gpointer value, gpointer user_data)
{
	mono_dl_close((MonoDl*)value);
}

void
mono_cached_module_cleanup (void)
{
	// No need for a transition here since the thread is already detached from the runtime
	mono_global_loader_data_lock ();

	if (global_module_map != NULL) {
		g_hash_table_foreach(global_module_map, remove_cached_module, NULL);

		g_hash_table_destroy(global_module_map);
		global_module_map = NULL;
	}

	mono_global_loader_data_unlock ();
}

static gboolean
is_absolute_path (const char *path)
{
#ifdef HOST_DARWIN
	if (!strncmp (path, "@executable_path/", 17) || !strncmp (path, "@loader_path/", 13) ||
	    !strncmp (path, "@rpath/", 7))
	    return TRUE;
#endif
	return g_path_is_absolute (path);
}

static gpointer
lookup_pinvoke_call_impl (MonoMethod *method, MonoLookupPInvokeStatus *status_out);

static void
pinvoke_probe_convert_status_for_api (MonoLookupPInvokeStatus *status, const char **exc_class, const char **exc_arg)
{
	if (!exc_class)
		return;
	switch (status->err_code) {
	case LOOKUP_PINVOKE_ERR_OK:
		*exc_class = NULL;
		*exc_arg = NULL;
		break;
	case LOOKUP_PINVOKE_ERR_NO_LIB:
		*exc_class = "DllNotFoundException";
		*exc_arg = status->err_arg;
		status->err_arg = NULL;
		break;
	case LOOKUP_PINVOKE_ERR_NO_SYM:
		*exc_class = "EntryPointNotFoundException";
		*exc_arg = status->err_arg;
		status->err_arg = NULL;
		break;
	default:
		g_assert_not_reached ();
	}
}

static void
pinvoke_probe_convert_status_to_error (MonoLookupPInvokeStatus *status, MonoError *error)
{
	/* Note: this has to return a MONO_ERROR_GENERIC because mono_mb_emit_exception_for_error only knows how to decode generic errors. */
	switch (status->err_code) {
	case LOOKUP_PINVOKE_ERR_OK:
		return;
	case LOOKUP_PINVOKE_ERR_NO_LIB:
		mono_error_set_generic_error (error, "System", "DllNotFoundException", "%s", status->err_arg);
		g_free (status->err_arg);
		status->err_arg = NULL;
		break;
	case LOOKUP_PINVOKE_ERR_NO_SYM:
		mono_error_set_generic_error (error, "System", "EntryPointNotFoundException", "%s", status->err_arg);
		g_free (status->err_arg);
		status->err_arg = NULL;
		break;
	default:
		g_assert_not_reached ();
	}
}

/**
 * mono_lookup_pinvoke_call:
 */
gpointer
mono_lookup_pinvoke_call (MonoMethod *method, const char **exc_class, const char **exc_arg)
{
	gpointer result;
	MONO_ENTER_GC_UNSAFE;
	MonoLookupPInvokeStatus status;
	memset (&status, 0, sizeof (status));
	result = lookup_pinvoke_call_impl (method, &status);
	pinvoke_probe_convert_status_for_api (&status, exc_class, exc_arg);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

static MonoDl*
pinvoke_probe_for_module (MonoImage *image, const char*new_scope, char **found_name_out, char **error_msg_out);

static MonoDl*
pinvoke_probe_for_module_relative_directories (MonoImage *image, const char *file_name, char **found_name_out);

static gpointer
pinvoke_probe_for_symbol (MonoDl *module, MonoMethodPInvoke *piinfo, const char *import, char **error_msg_out);

gpointer
mono_lookup_pinvoke_call_internal (MonoMethod *method, MonoError *error)
{
	gpointer result;
	MonoLookupPInvokeStatus status;
	memset (&status, 0, sizeof (status));
	result = lookup_pinvoke_call_impl (method, &status);
	if (status.err_code)
		pinvoke_probe_convert_status_to_error (&status, error);
	return result;
}

gpointer
lookup_pinvoke_call_impl (MonoMethod *method, MonoLookupPInvokeStatus *status_out)
{
	MonoImage *image = m_class_get_image (method->klass);
	MonoMethodPInvoke *piinfo = (MonoMethodPInvoke *)method;
	MonoTableInfo *tables = image->tables;
	MonoTableInfo *im = &tables [MONO_TABLE_IMPLMAP];
	MonoTableInfo *mr = &tables [MONO_TABLE_MODULEREF];
	guint32 im_cols [MONO_IMPLMAP_SIZE];
	guint32 scope_token;
	const char *orig_import = NULL;
	const char *new_import = NULL;
	const char *orig_scope = NULL;
	const char *new_scope = NULL;
	char *error_msg = NULL;
	char *found_name = NULL;
	MonoDl *module = NULL;
	gboolean cached = FALSE;
	gpointer addr = NULL;

	g_assert (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL);

	g_assert (status_out);

	if (piinfo->addr)
		return piinfo->addr;

	if (image_is_dynamic (m_class_get_image (method->klass))) {
		MonoReflectionMethodAux *method_aux = 
			(MonoReflectionMethodAux *)g_hash_table_lookup (
				((MonoDynamicImage*)m_class_get_image (method->klass))->method_aux_hash, method);
		if (!method_aux)
			goto exit;

		orig_import = method_aux->dllentry;
		orig_scope = method_aux->dll;
	}
	else {
		if (!piinfo->implmap_idx || piinfo->implmap_idx > im->rows)
			goto exit;

		mono_metadata_decode_row (im, piinfo->implmap_idx - 1, im_cols, MONO_IMPLMAP_SIZE);

		if (!im_cols [MONO_IMPLMAP_SCOPE] || im_cols [MONO_IMPLMAP_SCOPE] > mr->rows)
			goto exit;

		piinfo->piflags = im_cols [MONO_IMPLMAP_FLAGS];
		orig_import = mono_metadata_string_heap (image, im_cols [MONO_IMPLMAP_NAME]);
		scope_token = mono_metadata_decode_row_col (mr, im_cols [MONO_IMPLMAP_SCOPE] - 1, MONO_MODULEREF_NAME);
		orig_scope = mono_metadata_string_heap (image, scope_token);
	}

#ifndef DISABLE_DLLMAP
	// FIXME: The dllmap remaps System.Native to mono-native
	mono_dllmap_lookup (image, orig_scope, orig_import, &new_scope, &new_import);
#else
	new_scope = g_strdup (orig_scope);
	new_import = g_strdup (orig_import);
#endif

	if (!module) {
		mono_image_lock (image);
		if (!image->pinvoke_scopes) {
			image->pinvoke_scopes = g_hash_table_new_full (g_str_hash, g_str_equal, g_free, NULL);
			image->pinvoke_scope_filenames = g_hash_table_new_full (g_str_hash, g_str_equal, g_free, g_free);
		}
		module = (MonoDl *)g_hash_table_lookup (image->pinvoke_scopes, new_scope);
		found_name = (char *)g_hash_table_lookup (image->pinvoke_scope_filenames, new_scope);
		mono_image_unlock (image);
		if (module)
			cached = TRUE;
		found_name = g_strdup (found_name);
	}

	if (!module)
		module = pinvoke_probe_for_module (image, new_scope, &found_name, &error_msg);

	if (!module) {
		mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_DLLIMPORT,
				"DllImport unable to load library '%s'.",
				error_msg);

		status_out->err_code = LOOKUP_PINVOKE_ERR_NO_LIB;
		status_out->err_arg = g_strdup (new_scope);
		goto exit;
	}

	if (!cached) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
					"DllImport loaded library '%s'.", found_name);
		mono_image_lock (image);
		if (!g_hash_table_lookup (image->pinvoke_scopes, new_scope)) {
			g_hash_table_insert (image->pinvoke_scopes, g_strdup (new_scope), module);
			g_hash_table_insert (image->pinvoke_scope_filenames, g_strdup (new_scope), g_strdup (found_name));
		}
		mono_image_unlock (image);
	}

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
				"DllImport searching in: '%s' ('%s').", new_scope, found_name);

	addr = pinvoke_probe_for_symbol (module, piinfo, new_import, &error_msg);

	if (!addr) {
		status_out->err_code = LOOKUP_PINVOKE_ERR_NO_SYM;
		status_out->err_arg = g_strdup (new_import);
		goto exit;
	}
	piinfo->addr = addr;

exit:
	g_free ((char *)new_import);
	g_free ((char *)new_scope);
	g_free (error_msg);
	g_free (found_name);
	return addr;
}

/**
 * pinvoke_probe_transform_path:
 *
 * Try transforming the library path given in \p new_scope in different ways
 * depending on \p phase
 *
 * \returns \c TRUE if a transformation was applied and the transformed path
 * components are written to the out arguments, or \c FALSE if a transformation
 * did not apply.
 */
static gboolean
pinvoke_probe_transform_path (const char *new_scope, int phase, char **file_name_out, char **base_name_out, char **dir_name_out, gboolean *is_absolute_out)
{
	char *file_name = NULL, *base_name = NULL, *dir_name = NULL;
	gboolean changed = FALSE;
	gboolean is_absolute = is_absolute_path (new_scope);
	switch (phase) {
	case 0:
		/* Try the original name */
		file_name = g_strdup (new_scope);
		changed = TRUE;
		break;
	case 1:
		/* Try trimming the .dll extension */
		if (strstr (new_scope, ".dll") == (new_scope + strlen (new_scope) - 4)) {
			file_name = g_strdup (new_scope);
			file_name [strlen (new_scope) - 4] = '\0';
			changed = TRUE;
		}
		break;
	case 2:
		if (is_absolute) {
			dir_name = g_path_get_dirname (new_scope);
			base_name = g_path_get_basename (new_scope);
			if (strstr (base_name, "lib") != base_name) {
				char *tmp = g_strdup_printf ("lib%s", base_name);       
				g_free (base_name);
				base_name = tmp;
				file_name = g_strdup_printf ("%s%s%s", dir_name, G_DIR_SEPARATOR_S, base_name);
				changed = TRUE;
			}
		} else if (strstr (new_scope, "lib") != new_scope) {
			file_name = g_strdup_printf ("lib%s", new_scope);
			changed = TRUE;
		}
		break;
	case 3:
		if (!is_absolute && mono_dl_get_system_dir ()) {
			dir_name = (char*)mono_dl_get_system_dir ();
			file_name = g_path_get_basename (new_scope);
			base_name = NULL;
			changed = TRUE;
		}
		break;
	default:
#ifndef TARGET_WIN32
		if (!g_ascii_strcasecmp ("user32.dll", new_scope) ||
		    !g_ascii_strcasecmp ("kernel32.dll", new_scope) ||
		    !g_ascii_strcasecmp ("user32", new_scope) ||
		    !g_ascii_strcasecmp ("kernel", new_scope)) {
			file_name = g_strdup ("libMonoSupportW.so");
			changed = TRUE;
		}
#endif
		break;
	}
	if (changed && is_absolute) {
		if (!dir_name)
			dir_name = g_path_get_dirname (file_name);
		if (!base_name)
			base_name = g_path_get_basename (file_name);
	}
	*file_name_out = file_name;
	*base_name_out = base_name;
	*dir_name_out = dir_name;
	*is_absolute_out = is_absolute;
	return changed;
}

static MonoDl*
pinvoke_probe_for_module (MonoImage *image, const char *new_scope, char **found_name_out, char **error_msg_out)
{
	char *full_name, *file_name;
	char *error_msg = NULL;
	char *found_name = NULL;
	int i;
	MonoDl *module = NULL;

	g_assert (found_name_out);
	g_assert (error_msg_out);

	if (!module) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
					"DllImport attempting to load: '%s'.", new_scope);

		/* we allow a special name to dlopen from the running process namespace */
		if (strcmp (new_scope, "__Internal") == 0){
			if (internal_module == NULL)
				internal_module = mono_dl_open (NULL, MONO_DL_LAZY, &error_msg);
			module = internal_module;
			goto leave;
		}
	}

	/*
	 * Try loading the module using a variety of names
	 */
	for (i = 0; i < 5; ++i) {
		char *base_name = NULL, *dir_name = NULL;
		gboolean is_absolute;

		gboolean changed = pinvoke_probe_transform_path (new_scope, i, &file_name, &base_name, &dir_name, &is_absolute);
		if (!changed)
			continue;
		
		if (!module && is_absolute) {
			module = cached_module_load (file_name, MONO_DL_LAZY, &error_msg);
			if (!module) {
				mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
						"DllImport error loading library '%s': '%s'.",
							file_name, error_msg);
				g_free (error_msg);
			} else {
				found_name = g_strdup (file_name);
			}
		}

		if (!module && !is_absolute) {
			module = pinvoke_probe_for_module_relative_directories (image, file_name, &found_name);
		}

		if (!module) {
			void *iter = NULL;
			char *file_or_base = is_absolute ? base_name : file_name;
			while ((full_name = mono_dl_build_path (dir_name, file_or_base, &iter))) {
				module = cached_module_load (full_name, MONO_DL_LAZY, &error_msg);
				if (!module) {
					mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
							"DllImport error loading library '%s': '%s'.",
								full_name, error_msg);
					g_free (error_msg);
				} else {
					found_name = g_strdup (full_name);
				}
				g_free (full_name);
				if (module)
					break;
			}
		}

		if (!module) {
			module = cached_module_load (file_name, MONO_DL_LAZY, &error_msg);
			if (!module) {
				mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
						"DllImport error loading library '%s': '%s'.",
							file_name, error_msg);
			} else {
				found_name = g_strdup (file_name);
			}
		}

		g_free (file_name);
		if (is_absolute) {
			g_free (base_name);
			g_free (dir_name);
		}

		if (module)
			break;
	}

leave:
	*found_name_out = found_name;
	*error_msg_out = error_msg;
	return module;
}

#ifdef ENABLE_NETCORE
void
mono_set_pinvoke_search_directories (int dir_count, char **dirs)
{
	pinvoke_search_directories_count = dir_count;
	pinvoke_search_directories = dirs;
}
#endif

static MonoDl*
pinvoke_probe_for_module_in_directory (const char *mdirname, const char *file_name, char **found_name_out)
{
	void *iter = NULL;
	char *full_name;
	MonoDl* module = NULL;

	while ((full_name = mono_dl_build_path (mdirname, file_name, &iter)) && module == NULL) {
		char *error_msg;
		module = cached_module_load (full_name, MONO_DL_LAZY, &error_msg);
		if (!module) {
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT, "DllImport error loading library '%s': '%s'.", full_name, error_msg);
			g_free (error_msg);
		} else {
			*found_name_out = g_strdup (full_name);
		}
		g_free (full_name);
	}
	g_free (full_name);

	return module;
}

static MonoDl*
pinvoke_probe_for_module_relative_directories (MonoImage *image, const char *file_name, char **found_name_out)
{
	char *found_name = NULL;
	MonoDl* module = NULL;

	g_assert (found_name_out);

#if ENABLE_NETCORE
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_DLLIMPORT, "netcore DllImport handler: wanted '%s'", file_name);

	// Search in predefined directories first
	for (int j = 0; j < pinvoke_search_directories_count && module == NULL; ++j) {
		module = pinvoke_probe_for_module_in_directory (pinvoke_search_directories[j], file_name, &found_name);
	}

	// Fallback to image directory
	if (module == NULL) {
		// TODO: Check DefaultDllImportSearchPathsAttribute, NativeLibrary callback
		char *mdirname = g_path_get_dirname (image->name);
		if (mdirname)
			module = pinvoke_probe_for_module_in_directory (mdirname, file_name, &found_name);
		g_free (mdirname);
	}
#else
			for (int j = 0; j < 3; ++j) {
				char *mdirname = NULL;
				switch (j) {
					case 0:
						mdirname = g_path_get_dirname (image->name);
						break;
					case 1: /* @executable_path@/../lib */
					{
						char buf [4096];
						int binl;
						binl = mono_dl_get_executable_path (buf, sizeof (buf));
						if (binl != -1) {
							char *base, *newbase;
							char *resolvedname;
							buf [binl] = 0;
							resolvedname = mono_path_resolve_symlinks (buf);

							base = g_path_get_dirname (resolvedname);
							newbase = g_path_get_dirname(base);

							// On Android the executable for the application is going to be /system/bin/app_process{32,64} depending on
							// the application's architecture. However, libraries for the different architectures live in different
							// subdirectories of `/system`: `lib` for 32-bit apps and `lib64` for 64-bit ones. Thus appending `/lib` below
							// will fail to load the DSO for a 64-bit app, even if it exists there, because it will have a different
							// architecture. This is the cause of https://github.com/xamarin/xamarin-android/issues/2780 and the ifdef
							// below is the fix.
							mdirname = g_strdup_printf (
#if defined(TARGET_ANDROID) && (defined(TARGET_ARM64) || defined(TARGET_AMD64))
									"%s/lib64",
#else
									"%s/lib",
#endif
									newbase);
							g_free (resolvedname);
							g_free (base);
							g_free (newbase);
						}
						break;
					}
#ifdef __MACH__
					case 2: /* @executable_path@/../Libraries */
					{
						char buf [4096];
						int binl;
						binl = mono_dl_get_executable_path (buf, sizeof (buf));
						if (binl != -1) {
							char *base, *newbase;
							char *resolvedname;
							buf [binl] = 0;
							resolvedname = mono_path_resolve_symlinks (buf);

							base = g_path_get_dirname (resolvedname);
							newbase = g_path_get_dirname(base);
							mdirname = g_strdup_printf ("%s/Libraries", newbase);

							g_free (resolvedname);
							g_free (base);
							g_free (newbase);
						}
						break;
					}
#endif
				}

				if (!mdirname)
					continue;

				module = pinvoke_probe_for_module_in_directory (mdirname, file_name, &found_name);
				g_free (mdirname);
				if (module)
					break;
			}
#endif

	*found_name_out = found_name;
	return module;
}

static gpointer
pinvoke_probe_for_symbol (MonoDl *module, MonoMethodPInvoke *piinfo, const char *import, char **error_msg_out)
{
	char *error_msg = NULL;
	gpointer addr = NULL;

	g_assert (error_msg_out);

#ifdef HOST_WIN32
	if (import && import [0] == '#' && isdigit (import [1])) {
		char *end;
		long id;

		id = strtol (import + 1, &end, 10);
		if (id > 0 && *end == '\0')
			import++;
	}
#endif
	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
				"Searching for '%s'.", import);

	if (piinfo->piflags & PINVOKE_ATTRIBUTE_NO_MANGLE) {
		error_msg = mono_dl_symbol (module, import, &addr); 
	} else {
		/*
		 * Search using a variety of mangled names
		 */
		for (int mangle_stdcall = 0; mangle_stdcall <= 1 && addr == NULL; mangle_stdcall++) {
#if HOST_WIN32 && HOST_X86
			const int max_managle_param_count = (mangle_stdcall == 0) ? 0 : 256;
#else
			const int max_managle_param_count = 0;
#endif
			for (int mangle_charset = 0; mangle_charset <= 1 && addr == NULL; mangle_charset ++) {
				for (int mangle_param_count = 0; mangle_param_count <= max_managle_param_count && addr == NULL; mangle_param_count += 4) {

					char *mangled_name = (char*)import;
					switch (piinfo->piflags & PINVOKE_ATTRIBUTE_CHAR_SET_MASK) {
					case PINVOKE_ATTRIBUTE_CHAR_SET_UNICODE:
						/* Try the mangled name first */
						if (mangle_charset == 0)
							mangled_name = g_strconcat (import, "W", (const char*)NULL);
						break;
					case PINVOKE_ATTRIBUTE_CHAR_SET_AUTO:
#ifdef HOST_WIN32
						if (mangle_charset == 0)
							mangled_name = g_strconcat (import, "W", (const char*)NULL);
#else
						/* Try the mangled name last */
						if (mangle_charset == 1)
							mangled_name = g_strconcat (import, "A", (const char*)NULL);
#endif
						break;
					case PINVOKE_ATTRIBUTE_CHAR_SET_ANSI:
					default:
						/* Try the mangled name last */
						if (mangle_charset == 1)
							mangled_name = g_strconcat (import, "A", (const char*)NULL);
						break;
					}

#if HOST_WIN32 && HOST_X86
					/* Try the stdcall mangled name */
					/* 
					 * gcc under windows creates mangled names without the underscore, but MS.NET
					 * doesn't support it, so we doesn't support it either.
					 */
					if (mangle_stdcall == 1) {
						MonoMethod *method = &piinfo->method;
						int param_count;
						if (mangle_param_count == 0)
							param_count = mono_method_signature_internal (method)->param_count * sizeof (gpointer);
						else
							/* Try brute force, since it would be very hard to compute the stack usage correctly */
							param_count = mangle_param_count;

						char *mangled_stdcall_name = g_strdup_printf ("_%s@%d", mangled_name, param_count);

						if (mangled_name != import)
							g_free (mangled_name);

						mangled_name = mangled_stdcall_name;
					}
#endif
					mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
								"Probing '%s'.", mangled_name);

					error_msg = mono_dl_symbol (module, mangled_name, &addr);

					if (addr)
						mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
									"Found as '%s'.", mangled_name);
					else
						mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
									"Could not find '%s' due to '%s'.", mangled_name, error_msg);

					g_free (error_msg);
					error_msg = NULL;

					if (mangled_name != import)
						g_free (mangled_name);
				}
			}
		}
	}

	*error_msg_out = error_msg;
	return addr;
}
