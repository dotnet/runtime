// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <glib.h>
#include <stdbool.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/bundled-resources-internals.h>
#include <mono/metadata/webcil-loader.h>

static GHashTable *bundled_resources = NULL;
static bool bundle_contains_assemblies = false;
static bool bundle_contains_satellite_assemblies = false;

//---------------------------------------------------------------------------------------
//
// mono_bundled_resources_free frees all memory allocated for bundled resources.
// It should only be called when the runtime no longer needs access to the data,
// most likely to happen during runtime shutdown.
//

void
mono_bundled_resources_free (void)
{
	g_assert (mono_runtime_is_shutting_down ());

	g_hash_table_destroy (bundled_resources);
	bundled_resources = NULL;

	bundle_contains_assemblies = false;
	bundle_contains_satellite_assemblies = false;
}

void
mono_wasm_free_bundled_assembly_resource_func (void *resource)
{
	MonoBundledResource *bundled_resource = (MonoBundledResource *)resource;
	g_free ((void *)bundled_resource->id);
	g_free (resource);
}

void
mono_wasm_free_bundled_satellite_assembly_resource_func (void *resource)
{
	MonoBundledResource *bundled_resource = (MonoBundledResource *)resource;
	g_free ((void *)bundled_resource->id);
	MonoBundledSatelliteAssemblyResource *satellite_assembly_resource = (MonoBundledSatelliteAssemblyResource *)resource;
	g_free ((void *)satellite_assembly_resource->satellite_assembly.name);
	g_free ((void *)satellite_assembly_resource->satellite_assembly.culture);

	g_free (resource);
}

//---------------------------------------------------------------------------------------
//
// mono_bundled_resources_value_destroy_func frees the memory allocated by the hashtable's
// MonoBundled*Resource by invoking its underlying free_bundled_resource_func when possible.
//

static void
mono_bundled_resources_value_destroy_func (void *resource)
{
	MonoBundledResource *value = (MonoBundledResource *)resource;
	if (value->free_data) {
		MonoBundledResource *free_data = (MonoBundledResource *)value->free_data;
		if (free_data->free_bundled_resource_func) {
			free_data->free_bundled_resource_func (free_data);
		}
	}
	if (value->free_bundled_resource_func)
		value->free_bundled_resource_func (resource);
}

static bool
is_known_assembly_extension (const char *ext)
{
#ifdef ENABLE_WEBCIL
	return !strcmp (ext, ".dll") || !strcmp (ext, ".webcil") || !strcmp (ext, MONO_WEBCIL_IN_WASM_EXTENSION);
#else
	return !strcmp (ext, ".dll") || !strcmp (ext, MONO_WEBCIL_IN_WASM_EXTENSION);
#endif
}

static gboolean
resource_id_equal (const char *id_one, const char *id_two)
{
	const char *extension_one = strrchr (id_one, '.');
	const char *extension_two = strrchr (id_two, '.');
	if (extension_one && extension_two && is_known_assembly_extension (extension_one) && is_known_assembly_extension (extension_two)) {
		size_t len_one = extension_one - id_one;
		size_t len_two = extension_two - id_two;
		return (len_one == len_two) && !strncmp (id_one, id_two, len_one);
	}

	return !strcmp (id_one, id_two);
}

static guint
resource_id_hash (const char *id)
{
	const char *current = id;
	const char *extension = NULL;
	guint previous_hash = 0;
	guint hash = 0;

	while (*current) {
		hash = (hash << 5) - (hash + *current);
		if (*current == '.') {
			extension = current;
			previous_hash = hash;
		}
		current++;
	}

	// alias all extensions to .dll
	if (extension && is_known_assembly_extension (extension)) {
		hash = previous_hash;
		hash = (hash << 5) - (hash + 'd');
		hash = (hash << 5) - (hash + 'l');
		hash = (hash << 5) - (hash + 'l');
	}

	return hash;
}

//---------------------------------------------------------------------------------------
//
// mono_bundled_resources_add handles bundling of many types of resources to circumvent
// needing to find or have those resources on disk. The MonoBundledResource struct models
// the union of information carried by all supported types of resources which are
// enumerated in MonoBundledResourceType.
//
// bundled_resources:
// A single hash table will hold all resources being bundled with the understanding
// that all resources being bundled are uniquely named. The bundled resource is tagged
// with the type of resource it represents, and the pointer added to this hash table
// should fully represent a MonoBundled*Resource struct defined in `bundled-resources-internals.h
//
// Arguments:
// ** resources_to_bundle - An array of pointers to `MonoBundledResource`, which details
//     the type of MonoBundled*Resource information follows this pointer in memory.
// len - The number of resources being added to the hash table
//

void
mono_bundled_resources_add (MonoBundledResource **resources_to_bundle, uint32_t len)
{
	MonoDomain *domain = mono_get_root_domain ();
	g_assert (!domain);

	if (!bundled_resources)
		bundled_resources = g_hash_table_new_full ((GHashFunc)resource_id_hash, (GEqualFunc)resource_id_equal, NULL, mono_bundled_resources_value_destroy_func);

	bool assemblyAdded = false;
	bool satelliteAssemblyAdded = false;

	for (uint32_t i = 0; i < len; ++i) {
		MonoBundledResource *resource_to_bundle = (MonoBundledResource *)resources_to_bundle[i];
		if (resource_to_bundle->type == MONO_BUNDLED_ASSEMBLY)
			assemblyAdded = true;

		if (resource_to_bundle->type == MONO_BUNDLED_SATELLITE_ASSEMBLY)
			satelliteAssemblyAdded = true;

		g_hash_table_insert (bundled_resources, (gpointer) resource_to_bundle->id, resource_to_bundle);
	}

	if (assemblyAdded)
		bundle_contains_assemblies = true;

	if (satelliteAssemblyAdded)
		bundle_contains_satellite_assemblies = true;
}


//---------------------------------------------------------------------------------------
//
// mono_bundled_resources_get retrieves the pointer of the MonoBundledResource associated
// with a key equivalent to the requested resource id.
//
// Arguments:
//  * id - Unique name of the resource
//
// Returns:
//  MonoBundledResource * - Pointer to the resource in the hashmap with the key `id`
//

static MonoBundledResource *
mono_bundled_resources_get (const char *id)
{
	if (!bundled_resources)
		return NULL;

	return g_hash_table_lookup (bundled_resources, id);
}

//---------------------------------------------------------------------------------------
//
// mono_bundled_resources_get_assembly_resource retrieves MonoBundledAssemblyResource* associated
// with a key equivalent to the requested resource id if found.
//
// Arguments:
//  * id - Unique name of the resource
//
// Returns:
//  MonoBundledAssemblyResource * - Pointer to the bundled assembly resource in the hashmap with the key `id`
//
// Note: As MonoBundled*Resource types are not public, prefer `mono_bundled_resources_get_assembly_resource_values`
// in external contexts to grab assembly and symbol data.
//

static MonoBundledAssemblyResource *
mono_bundled_resources_get_assembly_resource (const char *id)
{
	MonoBundledAssemblyResource *assembly =
		(MonoBundledAssemblyResource*)mono_bundled_resources_get (id);
	if (!assembly)
		return NULL;
	g_assert (assembly->resource.type == MONO_BUNDLED_ASSEMBLY);
	return assembly;
}

//---------------------------------------------------------------------------------------
//
// mono_bundled_resources_get_satellite_assembly_resource retrieves MonoBundledSatelliteAssemblyResource* associated
// with a key equivalent to the requested resource id if found.
//
// Arguments:
//  * id - Unique name of the resource
//
// Returns:
//  MonoBundledSatelliteAssemblyResource * - Pointer to the bundled assembly resource in the hashmap with the key `id`
//
// Note: As MonoBundled*Resource types are not public, prefer `mono_bundled_resources_get_satellite_assembly_resource_values`
// in external contexts to grab satellite assembly data.
//

static MonoBundledSatelliteAssemblyResource *
mono_bundled_resources_get_satellite_assembly_resource (const char *id)
{
	MonoBundledSatelliteAssemblyResource *satellite_assembly =
		(MonoBundledSatelliteAssemblyResource*)mono_bundled_resources_get (id);
	if (!satellite_assembly)
		return NULL;
	g_assert (satellite_assembly->resource.type == MONO_BUNDLED_SATELLITE_ASSEMBLY);
	return satellite_assembly;
}

//---------------------------------------------------------------------------------------
//
// mono_bundled_resources_get_data_resource retrieves MonoBundledDataResource* associated
// with a key equivalent to the requested resource id if found.
//
// Arguments:
//  * id - Unique name of the resource
//
// Returns:
//  MonoBundledDataResource * - Pointer to the bundled assembly resource in the hashmap with the key `id`
//
// Note: As MonoBundled*Resource types are not public, prefer `mono_bundled_resources_get_data_resource_values`
// in external contexts to grab data.
//

static MonoBundledDataResource *
mono_bundled_resources_get_data_resource (const char *id)
{
	MonoBundledDataResource *data =
		(MonoBundledDataResource*)mono_bundled_resources_get (id);
	if (!data)
		return NULL;
	g_assert (data->resource.type == MONO_BUNDLED_DATA);
	return data;
}

//---------------------------------------------------------------------------------------
//
// mono_bundled_resources_get_assembly_resource_values retrieves assembly data associated
// with a key equivalent to the requested resource id if found.
//
// Arguments:
//  * id - Unique name of the resource
//  ** data_out - address to point at assembly byte data
//  ** size_out - address to point at assembly byte data size
//  ** symbol_data_out - address to point at assembly symbol byte data
//  ** symbol_size_out - address to point at assembly symbol byte data size
//
// Returns:
//  bool - whether or not a valid MonoBundledAssemblyResource was found with key 'id'
//

bool
mono_bundled_resources_get_assembly_resource_values (const char *id, const uint8_t **data_out, uint32_t *size_out, const uint8_t **symbol_data_out, uint32_t *symbol_size_out)
{
	*data_out = NULL;
	*size_out = 0;
	*symbol_data_out = NULL;
	*size_out = 0;
	MonoBundledAssemblyResource *bundled_assembly_resource = mono_bundled_resources_get_assembly_resource (id);
	if (!bundled_assembly_resource ||
		!bundled_assembly_resource->assembly.data ||
		bundled_assembly_resource->assembly.size == 0)
		return false;

	*data_out = bundled_assembly_resource->assembly.data;
	*size_out = bundled_assembly_resource->assembly.size;
	*symbol_data_out = bundled_assembly_resource->symbol_data.data;
	*symbol_size_out = bundled_assembly_resource->symbol_data.size;

	return true;
}

//---------------------------------------------------------------------------------------
//
// mono_bundled_resources_get_satellite_assembly_resource_values retrieves satellite assembly data associated
// with a key equivalent to the requested resource id if found.
//
// Arguments:
//  * id - Unique name of the resource
//  ** data_out - address to point at satellite assembly byte data
//  ** size_out - address to point at satellite assembly byte data size
//
// Returns:
//  bool - whether or not a valid MonoBundledSatelliteAssemblyResource was found with key 'id'
//

bool
mono_bundled_resources_get_satellite_assembly_resource_values (const char *id, const uint8_t **data_out, uint32_t *size_out)
{
	*data_out = NULL;
	*size_out = 0;

	MonoBundledSatelliteAssemblyResource *bundled_satellite_assembly_resource = mono_bundled_resources_get_satellite_assembly_resource (id);
	if (!bundled_satellite_assembly_resource ||
		!bundled_satellite_assembly_resource->satellite_assembly.data ||
		bundled_satellite_assembly_resource->satellite_assembly.size == 0)
		return false;

	*data_out = bundled_satellite_assembly_resource->satellite_assembly.data;
	*size_out = bundled_satellite_assembly_resource->satellite_assembly.size;

	return true;
}

//---------------------------------------------------------------------------------------
//
// mono_bundled_resources_get_data_resource_values retrieves data associated
// with a key equivalent to the requested resource id if found.
//
// Arguments:
//  * id - Unique name of the resource
//  ** data_out - address to point at resource byte data
//  ** size_out - address to point at resource byte data size
//
// Returns:
//  bool - whether or not a valid MonoBundledDataResource was found with key 'id'
//

bool
mono_bundled_resources_get_data_resource_values (const char *id, const uint8_t **data_out, uint32_t *size_out)
{
	*data_out = NULL;
	*size_out = 0;

	MonoBundledDataResource *bundled_data_resource = mono_bundled_resources_get_data_resource (id);
	if (!bundled_data_resource ||
		!bundled_data_resource->data.data ||
		bundled_data_resource->data.size == 0)
		return false;

	*data_out = bundled_data_resource->data.data;
	*size_out = bundled_data_resource->data.size;

	return true;
}

void
mono_bundled_resources_add_assembly_resource (const char *id, const char *name, const uint8_t *data, uint32_t size, void (*free_bundled_resource_func)(void *), void *free_data)
{
	// Check if assembly pdb counterpart had been added via mono_register_symfile_for_assembly
	MonoBundledAssemblyResource *assembly_resource = mono_bundled_resources_get_assembly_resource (name);
	if (!assembly_resource) {
		assembly_resource = g_new0 (MonoBundledAssemblyResource, 1);
		assembly_resource->resource.type = MONO_BUNDLED_ASSEMBLY;
		assembly_resource->resource.id = id;
		assembly_resource->resource.free_bundled_resource_func = free_bundled_resource_func;
		assembly_resource->resource.free_data = free_data;
		mono_bundled_resources_add ((MonoBundledResource **)&assembly_resource, 1);
	} else {
		// Ensure the MonoBundledAssemblyData has not been initialized
		g_assert (!assembly_resource->assembly.name && !assembly_resource->assembly.data && assembly_resource->assembly.size == 0);

		MonoBundledAssemblyResource *assembly_resource_symbol_owned_data = g_new0 (MonoBundledAssemblyResource, 1);
		assembly_resource_symbol_owned_data->resource.id = assembly_resource->resource.id;
		assembly_resource_symbol_owned_data->resource.free_bundled_resource_func = assembly_resource->resource.free_bundled_resource_func;
		assembly_resource_symbol_owned_data->resource.free_data = assembly_resource->resource.free_data;
		assembly_resource_symbol_owned_data->symbol_data.data = assembly_resource->symbol_data.data;
		assembly_resource_symbol_owned_data->symbol_data.size = assembly_resource->symbol_data.size;

		assembly_resource->resource.free_bundled_resource_func = free_bundled_resource_func;
		assembly_resource->resource.free_data = assembly_resource_symbol_owned_data;
	}
	assembly_resource->assembly.name = name;
	assembly_resource->assembly.data = data;
	assembly_resource->assembly.size = size;
}

void
mono_bundled_resources_add_assembly_symbol_resource (const char *id, const uint8_t *data, uint32_t size, void (*free_bundled_resource_func)(void *), void *free_data)
{
	// Check if assembly dll counterpart had been added via mono_register_bundled_assemblies
	MonoBundledAssemblyResource *assembly_resource = mono_bundled_resources_get_assembly_resource (id);
	if (!assembly_resource) {
		assembly_resource = g_new0 (MonoBundledAssemblyResource, 1);
		assembly_resource->resource.type = MONO_BUNDLED_ASSEMBLY;
		assembly_resource->resource.id = id;
		assembly_resource->resource.free_bundled_resource_func = free_bundled_resource_func;
		assembly_resource->resource.free_data = free_data;
		mono_bundled_resources_add ((MonoBundledResource **)&assembly_resource, 1);
	} else {
		// Ensure the MonoBundledSymbolData has not been initialized
		g_assert (!assembly_resource->symbol_data.data && assembly_resource->symbol_data.size == 0);

		MonoBundledAssemblyResource *assembly_resource_assembly_owned_data = g_new0 (MonoBundledAssemblyResource, 1);
		assembly_resource_assembly_owned_data->resource.id = assembly_resource->resource.id;
		assembly_resource_assembly_owned_data->resource.free_bundled_resource_func = assembly_resource->resource.free_bundled_resource_func;
		assembly_resource_assembly_owned_data->resource.free_data = assembly_resource->resource.free_data;
		assembly_resource_assembly_owned_data->assembly.name = assembly_resource->assembly.name;
		assembly_resource_assembly_owned_data->assembly.data = assembly_resource->assembly.data;
		assembly_resource_assembly_owned_data->assembly.size = assembly_resource->assembly.size;

		assembly_resource->resource.free_bundled_resource_func = free_bundled_resource_func;
		assembly_resource->resource.free_data = assembly_resource_assembly_owned_data;
	}
	assembly_resource->symbol_data.data = (const uint8_t *)data;
	assembly_resource->symbol_data.size = (uint32_t)size;
}

void
mono_bundled_resources_add_satellite_assembly_resource (const char *id, const char *name, const char *culture, const uint8_t *data, uint32_t size, void (*free_bundled_resource_func)(void *), void *free_data)
{
	MonoBundledSatelliteAssemblyResource *satellite_assembly_resource = mono_bundled_resources_get_satellite_assembly_resource (id);
	g_assert (!satellite_assembly_resource);
	satellite_assembly_resource = g_new0 (MonoBundledSatelliteAssemblyResource, 1);
	satellite_assembly_resource->resource.type = MONO_BUNDLED_SATELLITE_ASSEMBLY;
	satellite_assembly_resource->resource.id = id;
	satellite_assembly_resource->resource.free_bundled_resource_func = free_bundled_resource_func;
	satellite_assembly_resource->resource.free_data = free_data;
	satellite_assembly_resource->satellite_assembly.name = name;
	satellite_assembly_resource->satellite_assembly.culture = culture;
	satellite_assembly_resource->satellite_assembly.data = data;
	satellite_assembly_resource->satellite_assembly.size = size;
	mono_bundled_resources_add ((MonoBundledResource **)&satellite_assembly_resource, 1);
}

void
mono_bundled_resources_add_data_resource (const char *id, const char *name, const uint8_t *data, uint32_t size, void (*free_bundled_resource_func)(void *), void *free_data)
{
	MonoBundledDataResource *data_resource = mono_bundled_resources_get_data_resource (id);
	g_assert (!data_resource);
	data_resource = g_new0 (MonoBundledDataResource, 1);
	data_resource->resource.type = MONO_BUNDLED_DATA;
	data_resource->resource.id = id;
	data_resource->resource.free_bundled_resource_func = free_bundled_resource_func;
	data_resource->resource.free_data = free_data;
	data_resource->data.name = name;
	data_resource->data.data = data;
	data_resource->data.size = size;
	mono_bundled_resources_add ((MonoBundledResource **)&data_resource, 1);
}

//---------------------------------------------------------------------------------------
//
// mono_bundled_resources_contains_assemblies returns whether or not assemblies
// have been added to the bundled resource hash table via mono_bundled_resources_add.
//
// Returns:
//  bool - bool value indicating whether or not a bundled assembly resource had been added.
//

bool
mono_bundled_resources_contains_assemblies (void)
{
	return bundle_contains_assemblies;
}

//---------------------------------------------------------------------------------------
//
// mono_bundled_resources_contains_satellite_assemblies returns whether or not satellite assemblies
// have been added to the bundled resource hash table via mono_bundled_resources_add.
//
// Returns:
//  bool - bool value indicating whether or not a bundled satellite assembly resource had been added.
//

bool
mono_bundled_resources_contains_satellite_assemblies (void)
{
	return bundle_contains_satellite_assemblies;
}
