/**
 * \file
 * Copyright 2015 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_IMAGE_INTERNALS_H__
#define __MONO_METADATA_IMAGE_INTERNALS_H__

#include <mono/metadata/image.h>
#include <mono/metadata/loader-internals.h>

MonoImage*
mono_image_loaded_internal (MonoAssemblyLoadContext *alc, const char *name, mono_bool refonly);

MonoImage*
mono_image_load_file_for_image_checked (MonoImage *image, int fileidx, MonoError *error);

MonoImage*
mono_image_load_module_checked (MonoImage *image, int idx, MonoError *error);

MonoImage *
mono_image_open_a_lot (MonoAssemblyLoadContext *alc, const char *fname, MonoImageOpenStatus *status, gboolean refonly, gboolean load_from_context);

gboolean
mono_is_problematic_image (MonoImage *image);

gboolean
mono_is_problematic_file (const char *fname);

#endif /* __MONO_METADATA_IMAGE_INTERNALS_H__ */
