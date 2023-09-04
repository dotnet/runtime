/**
 * \file
 * Copyright 2015 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_IMAGE_INTERNALS_H__
#define __MONO_METADATA_IMAGE_INTERNALS_H__

#include <mono/metadata/image.h>
#include <mono/metadata/loader-internals.h>

typedef struct {
	guint dont_care_about_cli : 1;
	guint dont_care_about_pecoff : 1;
} MonoImageLoadOptions;

typedef struct {
	MonoImageLoadOptions load_options;
	guint not_executable : 1;
	guint metadata_only : 1;
} MonoImageOpenOptions;

MonoImage*
mono_image_loaded_internal (MonoAssemblyLoadContext *alc, const char *name);

MonoImage*
mono_image_load_file_for_image_checked (MonoImage *image, uint32_t fileidx, MonoError *error);

MonoImage*
mono_image_load_module_checked (MonoImage *image, uint32_t idx, MonoError *error);

MonoImage *
mono_image_open_a_lot (MonoAssemblyLoadContext *alc, const char *fname, MonoImageOpenStatus *status, const MonoImageOpenOptions *options);

#endif /* __MONO_METADATA_IMAGE_INTERNALS_H__ */
