// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef _MONO_COMPONENT_HOT_RELOAD_INTERNALS_H
#define _MONO_COMPONENT_HOT_RELOAD_INTERNALS_H

#include <glib.h>
#include "mono/metadata/object-forward.h"
#include "mono/metadata/metadata-internals.h"

/* Class-specific metadata update info.  See
 * mono_class_get_metadata_update_info() Note that this info is associated with
 * class _definitions_ that can be edited, so primitives, generic instances,
 * arrays, pointers, etc do not have this info.
 */
struct _MonoClassMetadataUpdateInfo {
	GArray *added_methods; /* a set of Method table tokens of any methods added to this class */
	GArray *added_fields; /* a set of Field table tokens of any methods added to this class */
};

#endif/*_MONO_COMPONENT_HOT_RELOAD_INTERNALS_H*/
