// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef __MONO_METADATA_BUNDLED_RESOURCES_INTERNALS_H__
#define __MONO_METADATA_BUNDLED_RESOURCES_INTERNALS_H__

#include <stdbool.h>
#include <stdint.h>

typedef enum {
	MONO_BUNDLED_DATA,
	MONO_BUNDLED_ASSEMBLY,
	MONO_BUNDLED_SATELLITE_ASSEMBLY,
	MONO_BUNDLED_RESOURCE_COUNT
} MonoBundledResourceType;

typedef struct _MonoBundledResource {
	MonoBundledResourceType type;
	const char *id;
	void (*free_bundled_resource_func)(void *);
} MonoBundledResource;

typedef struct _MonoBundledData {
	const char *name;
	const uint8_t *data;
	uint32_t size;
} MonoBundledData;

typedef struct _MonoBundledDataResource {
	MonoBundledResource resource;
	MonoBundledData data;
} MonoBundledDataResource;

typedef struct _MonoBundledSymbolData {
	const uint8_t *data;
	uint32_t size;
} MonoBundledSymbolData;

typedef struct _MonoBundledAssemblyData {
	const char *name;
	const uint8_t *data;
	uint32_t size;
} MonoBundledAssemblyData;

typedef struct _MonoBundledAssemblyResource {
	MonoBundledResource resource;
	MonoBundledAssemblyData assembly;
	MonoBundledSymbolData symbol_data;
} MonoBundledAssemblyResource;

typedef struct _MonoBundledSatelliteAssemblyData {
	const char *name;
	const char *culture;
	const uint8_t *data;
	uint32_t size;
} MonoBundledSatelliteAssemblyData;

typedef struct _MonoBundledSatelliteAssemblyResource {
	MonoBundledResource resource;
	MonoBundledSatelliteAssemblyData satellite_assembly;
} MonoBundledSatelliteAssemblyResource;

void
mono_bundled_resources_free (void);

void
mono_bundled_resources_free_bundled_resource_func (void *resource);

void
mono_wasm_free_bundled_resource_func (void *resource);

void
mono_bundled_resources_add (MonoBundledResource **resources_to_bundle, uint32_t len);

void
mono_bundled_resources_add_assembly_resource (const char *name, const uint8_t *data, uint32_t size, void (*free_bundled_resource_func)(void *));

void
mono_bundled_resources_add_satellite_assembly_resource (const char *id, const char *name, const char *culture, const uint8_t *data, uint32_t size, void (*free_bundled_resource_func)(void *));

MonoBundledResource *
mono_bundled_resources_get (const char *id);

MonoBundledAssemblyResource *
mono_bundled_resources_get_assembly_resource (const char *id);

MonoBundledSatelliteAssemblyResource *
mono_bundled_resources_get_satellite_assembly_resource (const char *id);

MonoBundledDataResource *
mono_bundled_resources_get_data_resource (const char *id);

bool
mono_bundled_resources_get_assembly_resource_values (const char *id, const uint8_t **data_out, uint32_t *size_out, const uint8_t **symbol_data_out, uint32_t *symbol_size_out);

bool
mono_bundled_resources_get_satellite_assembly_resource_values (const char *id, const uint8_t **data_out, uint32_t *size_out);

bool
mono_bundled_resources_get_data_resource_values (const char *id, const uint8_t **data_out, uint32_t *size_out);

bool
mono_bundled_resources_contains_assemblies (void);

bool
mono_bundled_resources_contains_satellite_assemblies (void);

#endif /* __MONO_METADATA_BUNDLED_RESOURCES_INTERNALS_H__ */