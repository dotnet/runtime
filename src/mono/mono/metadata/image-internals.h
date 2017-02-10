/**
 * \file
 * Copyright 2015 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_IMAGE_INTERNALS_H__
#define __MONO_METADATA_IMAGE_INTERNALS_H__

#include <mono/metadata/image.h>

MonoImage *
mono_find_image_owner (void *ptr);

MonoImage*
mono_image_load_file_for_image_checked (MonoImage *image, int fileidx, MonoError *error);

MonoImage*
mono_image_load_module_checked (MonoImage *image, int idx, MonoError *error);

MonoImage *
mono_image_open_a_lot (const char *fname, MonoImageOpenStatus *status, gboolean refonly, gboolean load_from_context);

#endif /* __MONO_METADATA_IMAGE_INTERNALS_H__ */
