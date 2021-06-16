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

#include <mono/utils/mono-forward.h>
#include <mono/utils/mono-publib.h>

typedef MonoGCHandle MonoAssemblyLoadContextGCHandle;

MONO_API MONO_RT_EXTERNAL_ONLY MonoAssembly *
mono_assembly_load_full_alc (MonoAssemblyLoadContextGCHandle alc_gchandle, MonoAssemblyName *aname, const char *basedir, MonoImageOpenStatus *status);

MONO_API MONO_RT_EXTERNAL_ONLY MonoImage *
mono_image_open_from_data_alc (MonoAssemblyLoadContextGCHandle alc_gchandle, char *data, uint32_t data_len, mono_bool need_copy, MonoImageOpenStatus *status, const char *name);

typedef MonoAssembly * (*MonoAssemblyPreLoadFuncV3) (MonoAssemblyLoadContextGCHandle alc_gchandle, MonoAssemblyName *aname, char **assemblies_path, void *user_data, MonoError *error);

MONO_API MONO_RT_EXTERNAL_ONLY void
mono_install_assembly_preload_hook_v3 (MonoAssemblyPreLoadFuncV3 func, void *user_data, mono_bool append);

// This can point at NULL before the default ALC is initialized
MONO_API MONO_RT_EXTERNAL_ONLY MonoAssemblyLoadContextGCHandle
mono_alc_get_default_gchandle (void);

typedef struct _MonoBundledSatelliteAssembly MonoBundledSatelliteAssembly;

MONO_API void
mono_register_bundled_satellite_assemblies (const MonoBundledSatelliteAssembly **assemblies);

MONO_API MonoBundledSatelliteAssembly *
mono_create_new_bundled_satellite_assembly (const char *name, const char *culture, const unsigned char *data, unsigned int size);

typedef void * (*PInvokeOverrideFn) (const char *libraryName, const char *entrypointName);

#endif /*__MONO_METADATA_MONO_PRIVATE_UNSTABLE_H__*/
