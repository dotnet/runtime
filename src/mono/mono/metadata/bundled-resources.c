// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <glib.h>

#include "bundled-resources-internals.h"
#include "assembly-internals.h"

#include <mono/metadata/assembly.h>
#include <mono/metadata/mono-private-unstable.h>

static GHashTable *bundled_resources = NULL;

static int *resources_bundled_counts = NULL;

//---------------------------------------------------------------------------------------
//
// mono_free_bundled_resources frees all memory allocated for bundled resources.
// It should only be called when the runtime no longer needs access to the data,
// most likely to happen during runtime shutdown.
//

void
mono_free_bundled_resources (void)
{
	if (bundled_resources)
		g_hash_table_destroy (bundled_resources);
	bundled_resources = NULL;

	g_free (resources_bundled_counts);
	resources_bundled_counts = NULL;
}

//---------------------------------------------------------------------------------------
//
// mono_add_bundled_resource handles bundling of many types of resources to circumvent
// needing to find or have those resources on disk. The MonoBundledResource struct models
// the union of information carried by all supported types of resources which are
// enumerated in MonoBundledResourceType.
//
// bundled_resources:
// A single hash table will hold all resources being bundled with the understanding
// that all resources being bundled are uniquely named. The bundled resource is tagged
// with the type of resource it represents for later processing of specific types of
// resources as done in mono_register_bundled_resources.
//
// resources_bundled_counts:
// An integer array tracks the number of each resource type added to the bundle
// to facilitate appropriate memory allocation when processing the bundle as done in
// mono_register_bundled_resources
//
// Arguments:
//  * name - Unique name of the resource
//  * culture - Culture of satellite assemblies
//  * data - Byte data array of the bundled resource
//  * size - Size of byte data array of the bundled resource
//  type - Type of bundled resource
//

void
mono_add_bundled_resource (const char *name, const char *culture, const unsigned char *data, unsigned int size, MonoBundledResourceType type)
{
	if (!bundled_resources)
		bundled_resources = g_hash_table_new (g_str_hash, g_str_equal);

	if (!resources_bundled_counts)
		resources_bundled_counts = g_malloc0 (MONO_BUNDLED_RESOURCE_COUNT * sizeof (int));

	g_assert (!g_hash_table_contains (bundled_resources, name));

	MonoBundledResource *bundled_resource = g_new0 (MonoBundledResource, 1);
	bundled_resource->culture = culture;
	bundled_resource->data = data;
	bundled_resource->size = size;
	bundled_resource->type = type;
	++resources_bundled_counts[type];
	g_hash_table_insert (bundled_resources, (gpointer) name, bundled_resource);
}

//---------------------------------------------------------------------------------------
//
// mono_get_bundled_resource_data retrieves the data of the bundled resource. If the
// requested bundled resource's name has been added via mono_add_bundled_resource,
// the byte array data and data size are populated.
//
// Arguments:
//  * name - Unique name of the resource
//  ** out_data - the pointer to the byte array data
//  ** out_size - the pointer to the data size

void
mono_get_bundled_resource_data (const char *name, const unsigned char **out_data, unsigned int *out_size)
{
	*out_data = NULL;
	*out_size = 0;

	MonoBundledResource *bundled_resource;

	if (g_hash_table_lookup_extended (bundled_resources, name, NULL, (gpointer *)&bundled_resource)) {
		*out_data = bundled_resource->data;
		*out_size = bundled_resource->size;
	}
}

static int num_bundled_resources_to_register = 0;

//---------------------------------------------------------------------------------------
//
// populate_bundled_assemblies assists in populating the MonoBundledAssembly
// to register in mono_register_bundled_resources
//

static void
populate_bundled_assemblies (gpointer key, gpointer value, gpointer user_data)
{
	MonoBundledAssembly **bundle = (MonoBundledAssembly **)user_data;
	MonoBundledResource *bundled_resource = (MonoBundledResource *)value;
	if (!bundle || !bundled_resource || bundled_resource->type != MONO_BUNDLED_ASSEMBLY)
		return;

	MonoBundledAssembly *bundled_assembly = mono_create_new_bundled_assembly ((const char *)key, bundled_resource->data, bundled_resource->size);

	g_assert (num_bundled_resources_to_register < resources_bundled_counts[MONO_BUNDLED_ASSEMBLY]);
	bundle [num_bundled_resources_to_register++] = bundled_assembly;
}

//---------------------------------------------------------------------------------------
//
// populate_bundled_satellite_assemblies assists in populating the MonoBundledSatelliteAssembly
// to register in mono_register_bundled_resources
//

static void
populate_bundled_satellite_assemblies (gpointer key, gpointer value, gpointer user_data)
{
	MonoBundledSatelliteAssembly **bundle = (MonoBundledSatelliteAssembly **)user_data;
	MonoBundledResource *bundled_resource = (MonoBundledResource *)value;
	if (!bundle || !bundled_resource || bundled_resource->type != MONO_BUNDLED_SATELLITE_ASSEMBLY)
		return;

	MonoBundledSatelliteAssembly *bundled_satellite_assembly = mono_create_new_bundled_satellite_assembly ((const char *)key, bundled_resource->culture, bundled_resource->data, bundled_resource->size);

	g_assert (num_bundled_resources_to_register < resources_bundled_counts[MONO_BUNDLED_SATELLITE_ASSEMBLY]);
	bundle [num_bundled_resources_to_register++] = bundled_satellite_assembly;
}

//---------------------------------------------------------------------------------------
//
// mono_register_bundled_resources registers resources added by mono_add_bundled_resource.
// Out of the supported resource types in MonoBundledResourceType, assemblies and satellite
// assemblies are registered.
//
// To appropriately allocate memory for the MonoBundledAssembly array and
// MonoBundledSatelliteAssembly array to register, resources_bundled_counts is leveraged,
// and therefore, mono_add_bundled_resource must be previously invoked for any successful
// registering of assemblies and satellite assemblies.
//

void
mono_register_bundled_resources (void)
{
	if (!bundled_resources)
		return;

	// register assemblies
	if (resources_bundled_counts [MONO_BUNDLED_ASSEMBLY] != 0)
	{
		MonoBundledAssembly **assembly_bundle = g_new0 (MonoBundledAssembly*, resources_bundled_counts[MONO_BUNDLED_ASSEMBLY] + 1);
		num_bundled_resources_to_register = 0;
		g_hash_table_foreach (bundled_resources, populate_bundled_assemblies, assembly_bundle);
		g_assert (num_bundled_resources_to_register == resources_bundled_counts[MONO_BUNDLED_ASSEMBLY]);

		mono_register_bundled_assemblies ((const MonoBundledAssembly **)assembly_bundle);
	}

	// register satellite assemblies
	if (resources_bundled_counts [MONO_BUNDLED_SATELLITE_ASSEMBLY] != 0)
	{
		MonoBundledSatelliteAssembly **satellite_assembly_bundle = g_new0 (MonoBundledSatelliteAssembly *, resources_bundled_counts[MONO_BUNDLED_SATELLITE_ASSEMBLY] + 1);
		num_bundled_resources_to_register = 0;
		g_hash_table_foreach (bundled_resources, populate_bundled_satellite_assemblies, satellite_assembly_bundle);
		g_assert (num_bundled_resources_to_register == resources_bundled_counts[MONO_BUNDLED_SATELLITE_ASSEMBLY]);

		mono_register_bundled_satellite_assemblies ((const MonoBundledSatelliteAssembly **)satellite_assembly_bundle);
	}
}
