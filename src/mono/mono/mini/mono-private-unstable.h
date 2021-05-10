/**
 * \file
 * 
 * Private unstable APIs.
 *
 * WARNING: The declarations and behavior of functions in this header are NOT STABLE and can be modified or removed at
 * any time.
 *
 */


#ifndef __MONO_JIT_MONO_PRIVATE_UNSTABLE_H__
#define __MONO_JIT_MONO_PRIVATE_UNSTABLE_H__

#include <mono/utils/mono-publib.h>
#include <mono/metadata/image.h>
#include <mono/metadata/mono-private-unstable.h>

typedef struct {
	uint32_t kind; // 0 = Path of runtimeconfig.blob, 1 = pointer to image data, >= 2 undefined
	union {
		struct {
			const char *path;
		} name;
		struct {
			const char *data;
			uint32_t data_len;
		} data;
	} runtimeconfig;
} MonovmRuntimeConfigArguments;

typedef void (*MonovmRuntimeConfigArgumentsCleanup)          (MonovmRuntimeConfigArguments *args, void *user_data);

typedef struct {
	uint32_t assembly_count;
	char **basenames; /* Foo.dll */
	uint32_t *basename_lens;
	char **assembly_filepaths; /* /blah/blah/blah/Foo.dll */
} MonoCoreTrustedPlatformAssemblies;

typedef struct {
	uint32_t dir_count;
	char **dirs;
} MonoCoreLookupPaths;

typedef struct {
	MonoCoreTrustedPlatformAssemblies *trusted_platform_assemblies;
	MonoCoreLookupPaths *app_paths;
	MonoCoreLookupPaths *native_dll_search_directories;
	PInvokeOverrideFn pinvoke_override;
} MonoCoreRuntimeProperties;

/* These are used to load the AOT data for aot images compiled with MONO_AOT_FILE_FLAG_SEPARATE_DATA */
/*
 * Return the AOT data for ASSEMBLY. SIZE is the size of the data. OUT_HANDLE should be set to a handle which is later
 * passed to the free function.
 */
typedef unsigned char* (*MonoLoadAotDataFunc)          (MonoAssembly *assembly, int size, void* user_data, void **out_handle);
/* Not yet used */
typedef void  (*MonoFreeAotDataFunc)          (MonoAssembly *assembly, int size, void* user_data, void *handle);
MONO_API MONO_RT_EXTERNAL_ONLY void
mono_install_load_aot_data_hook (MonoLoadAotDataFunc load_func, MonoFreeAotDataFunc free_func, void* user_data);

MONO_API int
monovm_initialize (int propertyCount, const char **propertyKeys, const char **propertyValues);

MONO_API int 
monovm_runtimeconfig_initialize (MonovmRuntimeConfigArguments *arg, MonovmRuntimeConfigArgumentsCleanup cleanup_fn, void *user_data);

// The wrapper MonoCoreRuntimeProperties struct can be stack-allocated or freed, but the structs inside it _must_ be heap-allocated and never freed, as they are not copied to avoid extra allocations
MONO_API int
monovm_initialize_preparsed (MonoCoreRuntimeProperties *parsed_properties, int propertyCount, const char **propertyKeys, const char **propertyValues);

//#ifdef HOST_WASM
typedef void* (*MonoWasmGetNativeToInterpTramp) (MonoMethod *method, void *extra_arg);

MONO_API void
mono_wasm_install_get_native_to_interp_tramp (MonoWasmGetNativeToInterpTramp cb);
//#endif

#endif /*__MONO_JIT_MONO_PRIVATE_UNSTABLE_H__*/
