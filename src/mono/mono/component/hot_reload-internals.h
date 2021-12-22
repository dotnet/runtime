// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef _MONO_COMPONENT_HOT_RELOAD_INTERNALS_H
#define _MONO_COMPONENT_HOT_RELOAD_INTERNALS_H

#include <glib.h>
#include "mono/metadata/object-forward.h"
#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/class-internals.h"

/* Class-specific metadata update info.  See
 * mono_class_get_metadata_update_info() Note that this info is associated with
 * class _definitions_ that can be edited, so primitives, generic instances,
 * arrays, pointers, etc do not have this info.
 */
struct _MonoClassMetadataUpdateInfo {
	/* FIXME: use a struct that allocates out of the MonoClass mempool! or maybe add the GArray
	 * to the BaselineInfo for the image and cleanup from there. */
	GArray *added_members; /* a set of Method or Field table tokens of any methods or fields added to this class */

	GPtrArray *added_fields; /* a set of MonoClassMetadataUpdateField* values for every added field. */
};

typedef struct _MonoClassMetadataUpdateField {
	MonoClassField field;
	uint32_t generation; /* when this field was added */
	uint32_t token; /* the Field table token where this field was defined. (this won't make
			 * sense for generic instances, once EnC is supported there) */
} MonoClassMetadataUpdateField;

#endif/*_MONO_COMPONENT_HOT_RELOAD_INTERNALS_H*/
