// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
/**
 *
 * Private unstable APIs.
 *
 * WARNING: The declarations and behavior of functions in this header are NOT STABLE and can be modified or removed at
 * any time.
 *
 */
// This file does not have ifdef guards, it is meant to be included multiple times with different definitions of MONO_API_FUNCTION
#ifndef MONO_API_FUNCTION
#error "MONO_API_FUNCTION(ret,name,args) macro not defined before including function declaration header"
#endif

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoAssembly *, mono_assembly_load_full_alc, (MonoAssemblyLoadContextGCHandle alc_gchandle, MonoAssemblyName *aname, const char *basedir, MonoImageOpenStatus *status))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoImage *, mono_image_open_from_data_alc, (MonoAssemblyLoadContextGCHandle alc_gchandle, char *data, uint32_t data_len, mono_bool need_copy, MonoImageOpenStatus *status, const char *name))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_install_assembly_preload_hook_v3, (MonoAssemblyPreLoadFuncV3 func, void *user_data, mono_bool append))

// This can point at NULL before the default ALC is initialized
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoAssemblyLoadContextGCHandle, mono_alc_get_default_gchandle, (void))

MONO_API_FUNCTION(void, mono_register_bundled_satellite_assemblies, (const MonoBundledSatelliteAssembly **assemblies))

MONO_API_FUNCTION(MonoBundledSatelliteAssembly *, mono_create_new_bundled_satellite_assembly, (const char *name, const char *culture, const unsigned char *data, unsigned int size))


MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void*, mono_method_get_unmanaged_callers_only_ftnptr, (MonoMethod *method, MonoError *error))
