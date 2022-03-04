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
#ifndef _MONO_METADATA_PRIVATE_UNSTABLE_TYPES_H
#define _MONO_METADATA_PRIVATE_UNSTABLE_TYPES_H

#include <mono/utils/details/mono-publib-types.h>
#include <mono/utils/mono-forward.h>

MONO_BEGIN_DECLS

typedef MonoGCHandle MonoAssemblyLoadContextGCHandle;

typedef MonoAssembly * (*MonoAssemblyPreLoadFuncV3) (MonoAssemblyLoadContextGCHandle alc_gchandle, MonoAssemblyName *aname, char **assemblies_path, void *user_data, MonoError *error);

typedef struct _MonoBundledSatelliteAssembly MonoBundledSatelliteAssembly;

typedef void * (*PInvokeOverrideFn) (const char *libraryName, const char *entrypointName);

MONO_END_DECLS

#endif /* _MONO_METADATA_PRIVATE_UNSTABLE_TYPES_H */
