// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __MONO_BUNDLED_SOURCE_H__
#define __MONO_BUNDLED_SOURCE_H__

#include <assert.h>
#include <stdbool.h>

#include <mono/metadata/assembly.h>

typedef enum {
	MONO_BUNDLED_DATA,
	MONO_BUNDLED_ASSEMBLY,
	MONO_BUNDLED_SATELLITE_ASSEMBLY,
	MONO_BUNDLED_RESOURCE_COUNT,
} MonoBundledResourceType;

typedef struct _MonoBundledResource {
	MonoBundledResourceType type;
	void (*free_bundled_resource_func)(void *);
} MonoBundledResource;

typedef struct _MonoBundledData {
	char *name;
	const unsigned char *data;
	unsigned int size;
} MonoBundledData;

typedef struct _MonoBundledDataResource {
	MonoBundledResource resource;
	MonoBundledData data;
} MonoBundledDataResource;

typedef struct _MonoBundledSymbolData {
	const unsigned char *data;
	unsigned int size;
} MonoBundledSymbolData;

typedef struct _MonoBundledAssemblyResource {
	MonoBundledResource resource;
	MonoBundledAssembly assembly;
	MonoBundledSymbolData symbol_data;
} MonoBundledAssemblyResource;

typedef struct _MonoBundledSatelliteAssembly {
	const char *name;
	const char *culture;
	const unsigned char *data;
	unsigned int size;
} MonoBundledSatelliteAssembly;

typedef struct _MonoBundledSatelliteAssemblyResource {
	MonoBundledResource resource;
	MonoBundledSatelliteAssembly satellite_assembly;
	MonoBundledSymbolData symbol_data;
} MonoBundledSatelliteAssemblyResource;

void
mono_bundled_resources_free (void);

void
mono_bundled_resources_add (MonoBundledResource **resources_to_bundle, uint32_t len);

MonoBundledResource *
mono_bundled_resources_get (const char *name);

static inline MonoBundledAssemblyResource *
mono_bundled_resources_get_assembly_resource (const char *name)
{
	MonoBundledAssemblyResource *assembly =
		(MonoBundledAssemblyResource*)mono_bundled_resources_get (name);
	if (!assembly)
		return NULL;
	assert (assembly->resource.type == MONO_BUNDLED_ASSEMBLY);
	return assembly;
}

static inline MonoBundledSatelliteAssemblyResource *
mono_bundled_resources_get_satellite_assembly_resource (const char *name)
{
	MonoBundledSatelliteAssemblyResource *satellite_assembly =
		(MonoBundledSatelliteAssemblyResource*)mono_bundled_resources_get (name);
	if (!satellite_assembly)
		return NULL;
	assert (satellite_assembly->resource.type == MONO_BUNDLED_SATELLITE_ASSEMBLY);
	return satellite_assembly;
}

static inline MonoBundledDataResource *
mono_bundled_resources_get_data_resource (const char *name)
{
	MonoBundledDataResource *data =
		(MonoBundledDataResource*)mono_bundled_resources_get (name);
	if (!data)
		return NULL;
	assert (data->resource.type == MONO_BUNDLED_DATA);
	return data;
}

bool
mono_bundled_resources_contains_assemblies (void);

bool
mono_bundled_resources_contains_satellite_assemblies (void);

#endif /* __MONO_BUNDLED_SOURCE_H__ */