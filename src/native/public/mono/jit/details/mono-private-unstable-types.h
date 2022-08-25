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
#ifndef _MONO_JIT_PRIVATE_UNSTABLE_TYPES_H
#define _MONO_JIT_PRIVATE_UNSTABLE_TYPES_H

#include <mono/utils/details/mono-publib-types.h>
#include <mono/metadata/details/image-types.h>
#include <mono/metadata/details/mono-private-unstable-types.h>

MONO_BEGIN_DECLS

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

//#ifdef HOST_WASM
typedef void* (*MonoWasmGetNativeToInterpTramp) (MonoMethod *method, void *extra_arg);
typedef void* (*MonoWasmNativeToInterpCallback) (char * cookie);
//#endif

MONO_END_DECLS

#endif /* _MONO_JIT_PRIVATE_UNSTABLE_TYPES_H */
