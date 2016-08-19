/* 
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_DYNAMIC_IMAGE_INTERNALS_H__
#define __MONO_METADATA_DYNAMIC_IMAGE_INTERNALS_H__

#include <mono/metadata/object.h>
#include <mono/metadata/metadata-internals.h>

typedef struct {
	guint32 import_lookup_table;
	guint32 timestamp;
	guint32 forwarder;
	guint32 name_rva;
	guint32 import_address_table_rva;
} MonoIDT;

typedef struct {
	guint32 name_rva;
	guint32 flags;
} MonoILT;


void
mono_dynamic_images_init (void);

void
mono_dynamic_image_register_token (MonoDynamicImage *assembly, guint32 token, MonoObject *obj);

gboolean
mono_dynamic_image_is_valid_token (MonoDynamicImage *image, guint32 token);

MonoDynamicImage*
mono_dynamic_image_create (MonoDynamicAssembly *assembly, char *assembly_name, char *module_name);

guint32
mono_dynamic_image_add_to_blob_cached (MonoDynamicImage *assembly, char *b1, int s1, char *b2, int s2);

void
mono_dynimage_alloc_table (MonoDynamicTable *table, guint nrows);

#endif  /* __MONO_METADATA_DYNAMIC_IMAGE_INTERNALS_H__ */

