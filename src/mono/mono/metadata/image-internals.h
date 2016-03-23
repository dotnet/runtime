/* 
 * Copyright 2015 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_IMAGE_INTERNALS_H__
#define __MONO_METADATA_IMAGE_INTERNALS_H__

#include <mono/metadata/image.h>

MonoImage *
mono_find_image_owner (void *ptr);

#endif /* __MONO_METADATA_IMAGE_INTERNALS_H__ */
