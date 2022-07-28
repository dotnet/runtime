// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// This file does not have ifdef guards, it is meant to be included multiple times with different definitions of MONO_API_FUNCTION
#ifndef MONO_API_FUNCTION
#error "MONO_API_FUNCTION(ret,name,args) macro not defined before including function declaration header"
#endif

MONO_API_FUNCTION(void, mono_assemblies_init, (void))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_assemblies_cleanup, (void))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoAssembly *, mono_assembly_open, (const char *filename, MonoImageOpenStatus *status))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoAssembly *, mono_assembly_open_full, (const char *filename, MonoImageOpenStatus *status, mono_bool refonly))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoAssembly*, mono_assembly_load, (MonoAssemblyName *aname, const char *basedir, MonoImageOpenStatus *status))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoAssembly*, mono_assembly_load_full, (MonoAssemblyName *aname, const char *basedir, MonoImageOpenStatus *status, mono_bool refonly))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoAssembly*, mono_assembly_load_from,  (MonoImage *image, const char *fname, MonoImageOpenStatus *status))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoAssembly*, mono_assembly_load_from_full, (MonoImage *image, const char *fname, MonoImageOpenStatus *status, mono_bool refonly))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoAssembly*, mono_assembly_load_with_partial_name, (const char *name, MonoImageOpenStatus *status))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoAssembly*, mono_assembly_loaded, (MonoAssemblyName *aname))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoAssembly*, mono_assembly_loaded_full, (MonoAssemblyName *aname, mono_bool refonly))
MONO_API_FUNCTION(void, mono_assembly_get_assemblyref, (MonoImage *image, int index, MonoAssemblyName *aname))
MONO_API_FUNCTION(void, mono_assembly_load_reference, (MonoImage *image, int index))
MONO_API_FUNCTION(void, mono_assembly_load_references, (MonoImage *image, MonoImageOpenStatus *status))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoImage*, mono_assembly_load_module, (MonoAssembly *assembly, uint32_t idx))
MONO_API_FUNCTION(void, mono_assembly_close, (MonoAssembly *assembly))
MONO_API_FUNCTION(void, mono_assembly_foreach, (MonoFunc func, void* user_data))
MONO_API_FUNCTION(void, mono_assembly_set_main, (MonoAssembly *assembly))
MONO_API_FUNCTION(MonoAssembly *, mono_assembly_get_main, (void))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoImage *, mono_assembly_get_image, (MonoAssembly *assembly))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoAssemblyName *, mono_assembly_get_name, (MonoAssembly *assembly))
MONO_API_FUNCTION(mono_bool, mono_assembly_fill_assembly_name, (MonoImage *image, MonoAssemblyName *aname))
MONO_API_FUNCTION(mono_bool, mono_assembly_names_equal, (MonoAssemblyName *l, MonoAssemblyName *r))
MONO_API_FUNCTION(char*, mono_stringify_assembly_name, (MonoAssemblyName *aname))


MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_install_assembly_load_hook, (MonoAssemblyLoadFunc func, void* user_data))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_install_assembly_search_hook, (MonoAssemblySearchFunc func, void* user_data))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_install_assembly_refonly_search_hook, (MonoAssemblySearchFunc func, void* user_data))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoAssembly*, mono_assembly_invoke_search_hook, (MonoAssemblyName *aname))

/*
 * Installs a new search function which is used as a last resort when loading
 * an assembly fails. This could invoke AssemblyResolve events.
 */
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_install_assembly_postload_search_hook, (MonoAssemblySearchFunc func, void* user_data))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_install_assembly_postload_refonly_search_hook, (MonoAssemblySearchFunc func, void* user_data))


MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_install_assembly_preload_hook, (MonoAssemblyPreLoadFunc func, void* user_data))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_install_assembly_refonly_preload_hook, (MonoAssemblyPreLoadFunc func, void* user_data))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_assembly_invoke_load_hook, (MonoAssembly *ass))

MONO_API_FUNCTION(MonoAssemblyName*, mono_assembly_name_new, (const char *name))
MONO_API_FUNCTION(const char*, mono_assembly_name_get_name, (MonoAssemblyName *aname))
MONO_API_FUNCTION(const char*, mono_assembly_name_get_culture, (MonoAssemblyName *aname))
MONO_API_FUNCTION(uint16_t, mono_assembly_name_get_version, (MonoAssemblyName *aname, uint16_t *minor, uint16_t *build, uint16_t *revision))
MONO_API_FUNCTION(mono_byte*, mono_assembly_name_get_pubkeytoken, (MonoAssemblyName *aname))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_assembly_name_free, (MonoAssemblyName *aname))

MONO_API_FUNCTION(void, mono_register_bundled_assemblies, (const MonoBundledAssembly **assemblies))
MONO_API_FUNCTION(void, mono_register_symfile_for_assembly, (const char* assembly_name, const mono_byte *raw_contents, int size))
MONO_API_FUNCTION(const mono_byte *, mono_get_symfile_bytes_from_bundle, (const char* assembly_name, int *size))

MONO_API_FUNCTION(void, mono_set_assemblies_path, (const char* path))

/**
 * These functions are deprecated.
 */
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_set_rootdir, (void)) // no-ops
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_set_dirs, (const char *assembly_dir, const char *config_dir)) // ignores config_dir parameter, use mono_set_assemblies_path instead
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY char *, mono_native_getrootdir, (void)) // returns the same value as mono_assembly_getrootdir
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_assembly_setrootdir, (const char *root_dir)) // use mono_set_assemblies_path instead
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MONO_CONST_RETURN char *, mono_assembly_getrootdir, (void))

/**
 * These functions are deprecated and no-ops since app.config/machine.config handling is not available in dotnet/runtime.
 */
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_register_config_for_assembly, (const char* assembly_name, const char* config_xml))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_register_machine_config, (const char *config_xml))
