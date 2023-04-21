// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __MONO_BUNDLED_SOURCE_H__
#define __MONO_BUNDLED_SOURCE_H__

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

typedef struct _MonoBundledSymfile {
	const unsigned char *data;
	unsigned int size;
} MonoBundledSymfile;

typedef struct _MonoBundledAssemblyResource {
	MonoBundledResource resource;
	MonoBundledAssembly assembly;
    MonoBundledSymfile symfile;
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
    MonoBundledSymfile symfile;
} MonoBundledSatelliteAssemblyResource;

void
mono_free_bundled_resources (void);

void
mono_add_bundled_resource (MonoBundledResource **resources_to_bundle, uint32_t len);

MonoBundledResource *
mono_get_bundled_resource_data (const char *name);

#endif /* __MONO_BUNDLED_SOURCE_H__ */