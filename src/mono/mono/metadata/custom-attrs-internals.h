#ifndef __MONO_METADATA_CUSTOM_ATTRS_INTERNALS_H__
#define __MONO_METADATA_CUSTOM_ATTRS_INTERNALS_H__

#include <mono/metadata/object.h>
#include <mono/metadata/reflection.h>

MonoCustomAttrInfo*
mono_custom_attrs_from_builders (MonoImage *alloc_img, MonoImage *image, MonoArray *cattrs);

#endif  /* __MONO_METADATA_REFLECTION_CUSTOM_ATTRS_INTERNALS_H__ */
