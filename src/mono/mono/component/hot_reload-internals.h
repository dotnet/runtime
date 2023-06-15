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

/* Class-specific metadata update info for an existing class.  See
 * mono_class_get_metadata_update_info() Note that this info is associated with
 * class _definitions_ that can be edited, so primitives, generic instances,
 * arrays, pointers, etc do not have this info.
 */
struct _MonoClassMetadataUpdateInfo {
	/* FIXME: use a struct that allocates out of the MonoClass mempool! or maybe add the GArray
	 * to the BaselineInfo for the image and cleanup from there. */
	GSList *added_members; /* a set of Method or Field table tokens of any methods or fields added to this class, allocated from the MonoClass mempool */

	GSList *added_fields; /* a set of MonoClassMetadataUpdateField* values for every added field. */
	GSList *added_props; /* a set of MonoClassMetadataUpdateProperty* values */
	GSList *added_events; /* a set of MonoClassMetadataUpdateEvent* values */

	MonoClassRuntimeMetadataUpdateInfo runtime;

	uint32_t generation; /* must be updated when a GTD gets added props, events or fields; must be updated when a GINST copies updated info from the parent */
};

/*
 * Added type skeleton.
 *
 * When a hot reload delta is adding brand new class, the runtime allows a lot more leeway than when
 * new members are added to existing classes.  Anything that is possible to write in a baseline
 * assembly is possible with an added class.  One complication is that the EnCLog first contains a
 * row with a new TypeDef table token, but that table row has zeros for the field and method token
 * ids.  Instead, each method and field is added by an EnCLog entry with a ENC_FUNC_ADD_METHOD or
 * ENC_FUNC_ADD_FIELD function code.  We don't want to materialzie the MonoClass for the new type
 * definition until we've see all the added methods and fields.  Instead when we process the log we
 * collect a skeleton for the new class and then use it to create the MonoClass.
 *
 * We assume that the new methods and fields for a given class form a contiguous run (ie first and
 * count are sufficient to identify all the rows belonging to the new class).
 */
typedef struct _MonoAddedDefSkeleton {
	uint32_t typedef_token; /* which type is it */
	uint32_t first_method_idx, first_field_idx;
	uint32_t method_count;
	uint32_t field_count;
	uint32_t first_prop_idx;
	uint32_t prop_count;
	uint32_t first_event_idx;
	uint32_t event_count;
} MonoAddedDefSkeleton;


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
} MonoClassMetadataUpdateField;

typedef struct _MonoClassMetadataUpdateProperty {
	MonoProperty prop;
	uint32_t generation; /* when this prop was added */
	uint32_t token; /* the Property table token where this prop was defined. */
} MonoClassMetadataUpdateProperty;

typedef struct _MonoClassMetadataUpdateEvent {
	MonoEvent evt;
	uint32_t generation; /* when this event was added */
	uint32_t token; /* the Event table token where this event was defined. */
} MonoClassMetadataUpdateEvent;

typedef struct _MonoMethodMetadataUpdateParamInfo {
	uint32_t first_param_token; /* a Param token */
	uint32_t param_count;
} MonoMethodMetadataUpdateParamInfo;

#endif/*_MONO_COMPONENT_HOT_RELOAD_INTERNALS_H*/
