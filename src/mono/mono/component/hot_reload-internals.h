// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef _MONO_COMPONENT_HOT_RELOAD_INTERNALS_H
#define _MONO_COMPONENT_HOT_RELOAD_INTERNALS_H

#include <glib.h>
#include "mono/metadata/object-forward.h"
#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/class-internals.h"

/* Execution-time info for an updated class.  */
typedef struct _MonoClassRuntimeMetadataUpdateInfo {
	MonoCoopMutex static_fields_lock; /* protects the static_fields hashtable.  Values can be used outside the lock (since they're allocated pinned).  */
	MonoGHashTable *static_fields; /* key is field token, value is a pinned managed object: either a boxed valuetype (the static field address is the value address) or a Mono.HotReload.FieldStore object (in which case the static field address is the address of the _loc field in the object.) */
	gboolean inited;
} MonoClassRuntimeMetadataUpdateInfo;

/* Class-specific metadata update info.  See
 * mono_class_get_metadata_update_info() Note that this info is associated with
 * class _definitions_ that can be edited, so primitives, generic instances,
 * arrays, pointers, etc do not have this info.
 */
struct _MonoClassMetadataUpdateInfo {
	/* FIXME: use a struct that allocates out of the MonoClass mempool! or maybe add the GArray
	 * to the BaselineInfo for the image and cleanup from there. */
	GSList *added_members; /* a set of Method or Field table tokens of any methods or fields added to this class, allocated from the MonoClass mempool */

	GPtrArray *added_fields; /* a set of MonoClassMetadataUpdateField* values for every added field. */

	MonoClassRuntimeMetadataUpdateInfo runtime;
};


/* Keep in sync with Mono.HotReload.FieldStore in managed */
typedef struct _MonoHotReloadFieldStoreObject {
	MonoObject object;
	MonoObject *_loc;
} MonoHotReloadFieldStoreObject;

typedef struct _MonoClassMetadataUpdateField {
	MonoClassField field;
	uint32_t generation; /* when this field was added */
	uint32_t token; /* the Field table token where this field was defined. (this won't make
			 * sense for generic instances, once EnC is supported there) */
	/* if non-zero the EnC update came before the parent class was initialized.  The field is
	 * stored in the instance at this offset.  MonoClassField:offset is -1.  Not used for static
	 * fields. */
	int before_init_instance_offset;
} MonoClassMetadataUpdateField;

#endif/*_MONO_COMPONENT_HOT_RELOAD_INTERNALS_H*/
