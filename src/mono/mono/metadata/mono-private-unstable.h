/**
 * \file
 * 
 * Private unstable APIs.
 *
 * WARNING: The declarations and behavior of functions in this header are NOT STABLE and can be modified or removed at
 * any time.
 *
 */


#ifndef __MONO_METADATA_MONO_PRIVATE_UNSTABLE_H__
#define __MONO_METADATA_MONO_PRIVATE_UNSTABLE_H__

#include <mono/utils/mono-publib.h>

typedef MonoGCHandle MonoAssemblyLoadContextGCHandle;

MONO_API MONO_RT_EXTERNAL_ONLY
MonoAssembly *mono_assembly_load_full_alc (MonoAssemblyLoadContextGCHandle alc_gchandle, MonoAssemblyName *aname, const char *basedir, MonoImageOpenStatus *status);

typedef MonoAssembly * (*MonoAssemblyPreLoadFuncV3) (MonoAssemblyLoadContextGCHandle *alc_gchandle, MonoAssemblyName *aname, char **assemblies_path, gpointer user_data, MonoError *error);
void mono_install_assembly_preload_hook_v3 (MonoAssemblyPreLoadFuncV3 func, gpointer user_data, gboolean append);

#endif /*__MONO_METADATA_MONO_PRIVATE_UNSTABLE_H__*/
