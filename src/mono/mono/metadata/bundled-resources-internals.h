// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef __MONO_METADATA_BUNDLED_RESOURCES_INTERNALS_H__
#define __MONO_METADATA_BUNDLED_RESOURCES_INTERNALS_H__

typedef enum {
	MONO_BUNDLED_DATA,
	MONO_BUNDLED_ASSEMBLY,
	MONO_BUNDLED_SATELLITE_ASSEMBLY,
	MONO_BUNDLED_PDB,
	MONO_BUNDLED_RESOURCE_COUNT,
} MonoBundledResourceType;

typedef struct MonoBundledResource {
	const char *culture; // Satellite assemblies
	const unsigned char *data;
	unsigned int size;
	MonoBundledResourceType type;
} MonoBundledResource;

void
mono_free_bundled_resources (void);

void
mono_add_bundled_resource (const char *name, const char *culture, const unsigned char *data, unsigned int size, MonoBundledResourceType type);

void
mono_get_bundled_resource_data (const char *name, const unsigned char **out_data, unsigned int *out_size);

void
mono_register_bundled_resources (void);

#endif /* __MONO_METADATA_BUNDLED_RESOURCES_INTERNALS_H__ */