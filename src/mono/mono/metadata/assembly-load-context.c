#include "config.h"
#include "mono/metadata/domain-internals.h"
#include "mono/metadata/icall-decl.h"
#include "mono/metadata/loader-internals.h"
#include "mono/metadata/loaded-images-internals.h"
#include "mono/utils/mono-error-internals.h"

#ifdef ENABLE_NETCORE
/* MonoAssemblyLoadContext support only in netcore Mono */

void
mono_alc_init (MonoAssemblyLoadContext *alc, MonoDomain *domain)
{
	MonoLoadedImages *li = g_new0 (MonoLoadedImages, 1);
	mono_loaded_images_init (li, alc);
	alc->domain = domain;
	alc->loaded_images = li;
}

void
mono_alc_cleanup (MonoAssemblyLoadContext *alc)
{
	mono_loaded_images_free (alc->loaded_images);
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
	} else {
		/* create it */
		alc = mono_domain_create_individual_alc (domain, this_gchandle, collectible, error);
	}
	return alc;
}

#endif /* ENABLE_NETCORE */
