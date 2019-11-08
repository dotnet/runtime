#include "config.h"
#include "mono/utils/mono-compiler.h"

#ifdef ENABLE_NETCORE // MonoAssemblyLoadContext support only in netcore Mono

#include "mono/metadata/assembly.h"
#include "mono/metadata/domain-internals.h"
#include "mono/metadata/exception-internals.h"
#include "mono/metadata/icall-decl.h"
#include "mono/metadata/loader-internals.h"
#include "mono/metadata/loaded-images-internals.h"
#include "mono/utils/mono-error-internals.h"
#include "mono/utils/mono-logger-internals.h"

GENERATE_GET_CLASS_WITH_CACHE (assembly_load_context, "System.Runtime.Loader", "AssemblyLoadContext");

void
mono_alc_init (MonoAssemblyLoadContext *alc, MonoDomain *domain)
{
	MonoLoadedImages *li = g_new0 (MonoLoadedImages, 1);
	mono_loaded_images_init (li, alc);
	alc->domain = domain;
	alc->loaded_images = li;
	alc->loaded_assemblies = NULL;
	alc->pinvoke_scopes = g_hash_table_new_full (g_str_hash, g_str_equal, g_free, NULL);
	mono_coop_mutex_init (&alc->assemblies_lock);
	mono_coop_mutex_init (&alc->pinvoke_lock);
}

void
mono_alc_cleanup (MonoAssemblyLoadContext *alc)
{
	/*
	 * As it stands, ALC and domain cleanup is probably broken on netcore. Without ALC collectability, this should not
	 * be hit. I've documented roughly the things that still need to be accomplish, but the implementation is TODO and
	 * the ideal order and locking unclear.
	 * 
	 * For now, this makes two important assumptions:
	 *   1. The minimum refcount on assemblies is 2: one for the domain and one for the ALC. The domain refcount might 
	 *        be less than optimal on netcore, but its removal is too likely to cause issues for now.
	 *   2. An ALC will have been removed from the domain before cleanup.
	 */
	//GSList *tmp;
	//MonoDomain *domain = alc->domain;

	/*
	 * Missing steps:
	 * 
	 * + Release GC roots for all assemblies in the ALC
	 * + Iterate over the domain_assemblies and remove ones that belong to the ALC, which will probably require
	 *     converting domain_assemblies to a doubly-linked list, ideally GQueue
	 * + Close dynamic and then remaining assemblies, potentially nulling the data field depending on refcount
	 * + Second pass to call mono_assembly_close_finish on remaining assemblies
	 * + Free the loaded_assemblies list itself
	 */

	alc->loaded_assemblies = NULL;
	g_hash_table_destroy (alc->pinvoke_scopes);
	mono_coop_mutex_destroy (&alc->assemblies_lock);
	mono_coop_mutex_destroy (&alc->pinvoke_lock);

	mono_loaded_images_free (alc->loaded_images);

	g_assert_not_reached ();
}

void
mono_alc_assemblies_lock (MonoAssemblyLoadContext *alc)
{
	mono_coop_mutex_lock (&alc->assemblies_lock);
}

void
mono_alc_assemblies_unlock (MonoAssemblyLoadContext *alc)
{
	mono_coop_mutex_unlock (&alc->assemblies_lock);
}

gpointer
ves_icall_System_Runtime_Loader_AssemblyLoadContext_InternalInitializeNativeALC (gpointer this_gchandle_ptr, MonoBoolean is_default_alc, MonoBoolean collectible, MonoError *error)
{
	/* If the ALC is collectible, this_gchandle is weak, otherwise it's strong. */
	uint32_t this_gchandle = (uint32_t)GPOINTER_TO_UINT (this_gchandle_ptr);
	if (collectible) {
		mono_error_set_execution_engine (error, "Collectible AssemblyLoadContexts are not yet supported by MonoVM");
		return NULL;
	}

	MonoDomain *domain = mono_domain_get ();
	MonoAssemblyLoadContext *alc = NULL;

	if (is_default_alc) {
		alc = mono_domain_default_alc (domain);
		g_assert (alc);
		if (!alc->gchandle)
			alc->gchandle = this_gchandle;
	} else {
		/* create it */
		alc = mono_domain_create_individual_alc (domain, this_gchandle, collectible, error);
	}
	return alc;
}

gpointer
ves_icall_System_Runtime_Loader_AssemblyLoadContext_GetLoadContextForAssembly (MonoReflectionAssemblyHandle assm_obj, MonoError *error)
{
	MonoAssembly *assm = MONO_HANDLE_GETVAL (assm_obj, assembly);
	MonoAssemblyLoadContext *alc = mono_assembly_get_alc (assm);

	return GUINT_TO_POINTER (alc->gchandle);
}

gboolean
mono_alc_is_default (MonoAssemblyLoadContext *alc)
{
	return alc == mono_alc_domain (alc)->default_alc;
}

static MonoAssembly*
invoke_resolve_method (MonoMethod *resolve_method, MonoAssemblyLoadContext *alc, MonoAssemblyName *aname, MonoError *error)
{
	MonoAssembly *result = NULL;
	char* aname_str = NULL;

	if (mono_runtime_get_no_exec ())
		return NULL;

	HANDLE_FUNCTION_ENTER ();

	aname_str = mono_stringify_assembly_name (aname);

	MonoStringHandle aname_obj = mono_string_new_handle (mono_alc_domain (alc), aname_str, error);
	goto_if_nok (error, leave);

	MonoReflectionAssemblyHandle assm;
	gpointer gchandle;
	gchandle = GUINT_TO_POINTER (alc->gchandle);
	gpointer args [2];
	args [0] = &gchandle;
	args [1] = MONO_HANDLE_RAW (aname_obj);
	assm = MONO_HANDLE_CAST (MonoReflectionAssembly, mono_runtime_try_invoke_handle (resolve_method, NULL_HANDLE, args, error));
	goto_if_nok (error, leave);

	if (MONO_HANDLE_BOOL (assm))
		result = MONO_HANDLE_GETVAL (assm, assembly);

leave:
	g_free (aname_str);
	HANDLE_FUNCTION_RETURN_VAL (result);
}

static MonoAssembly*
mono_alc_invoke_resolve_using_load (MonoAssemblyLoadContext *alc, MonoAssemblyName *aname, MonoError *error)
{
	static MonoMethod *resolve;

	if (!resolve) {
		ERROR_DECL (local_error);
		MonoClass *alc_class = mono_class_get_assembly_load_context_class ();
		g_assert (alc_class);
		MonoMethod *m = mono_class_get_method_from_name_checked (alc_class, "MonoResolveUsingLoad", -1, 0, local_error);
		mono_error_assert_ok (local_error);
		resolve = m;
	}
	g_assert (resolve);

	return invoke_resolve_method (resolve, alc, aname, error);
}

MonoAssembly*
mono_alc_invoke_resolve_using_load_nofail (MonoAssemblyLoadContext *alc, MonoAssemblyName *aname)
{
	MonoAssembly *result = NULL;
	ERROR_DECL (error);

	result = mono_alc_invoke_resolve_using_load (alc, aname, error);
	if (!is_ok (error))
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Error while invoking ALC Load(\"%s\") method: '%s'", aname->name, mono_error_get_message (error));

	mono_error_cleanup (error);

	return result;
}

static MonoAssembly*
mono_alc_invoke_resolve_using_resolving_event (MonoAssemblyLoadContext *alc, MonoAssemblyName *aname, MonoError *error)
{
	static MonoMethod *resolve;

	if (!resolve) {
		ERROR_DECL (local_error);
		MonoClass *alc_class = mono_class_get_assembly_load_context_class ();
		g_assert (alc_class);
		MonoMethod *m = mono_class_get_method_from_name_checked (alc_class, "MonoResolveUsingResolvingEvent", -1, 0, local_error);
		mono_error_assert_ok (local_error);
		resolve = m;
	}
	g_assert (resolve);

	return invoke_resolve_method (resolve, alc, aname, error);
}

MonoAssembly*
mono_alc_invoke_resolve_using_resolving_event_nofail (MonoAssemblyLoadContext *alc, MonoAssemblyName *aname)
{
	MonoAssembly *result = NULL;
	ERROR_DECL (error);

	result = mono_alc_invoke_resolve_using_resolving_event (alc, aname, error);
	if (!is_ok (error))
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Error while invoking ALC Resolving(\"%s\") event: '%s'", aname->name, mono_error_get_message (error));

	mono_error_cleanup (error);

	return result;
}

static MonoAssembly*
mono_alc_invoke_resolve_using_resolve_satellite (MonoAssemblyLoadContext *alc, MonoAssemblyName *aname, MonoError *error)
{
	static MonoMethod *resolve;

	if (!resolve) {
		ERROR_DECL (local_error);
		MonoClass *alc_class = mono_class_get_assembly_load_context_class ();
		g_assert (alc_class);
		MonoMethod *m = mono_class_get_method_from_name_checked (alc_class, "MonoResolveUsingResolveSatelliteAssembly", -1, 0, local_error);
		mono_error_assert_ok (local_error);
		resolve = m;
	}
	g_assert (resolve);

	return invoke_resolve_method (resolve, alc, aname, error);
}

MonoAssembly*
mono_alc_invoke_resolve_using_resolve_satellite_nofail (MonoAssemblyLoadContext *alc, MonoAssemblyName *aname)
{
	MonoAssembly *result = NULL;
	ERROR_DECL (error);

	result = mono_alc_invoke_resolve_using_resolve_satellite (alc, aname, error);
	if (!is_ok (error))
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Error while invoking ALC ResolveSatelliteAssembly(\"%s\") method: '%s'", aname->name, mono_error_get_message (error));

	mono_error_cleanup (error);

	return result;
}

#endif /* ENABLE_NETCORE */

MONO_EMPTY_SOURCE_FILE (assembly_load_context)
