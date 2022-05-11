// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
#ifndef _MONO_SGEN_BRIDGE_TYPES_H
#define _MONO_SGEN_BRIDGE_TYPES_H

#include <mono/utils/details/mono-publib-types.h>

MONO_BEGIN_DECLS

enum {
	SGEN_BRIDGE_VERSION = 5
};

typedef enum {
	/* Instances of this class should be scanned when computing the transitive dependency among bridges. E.g. List<object>*/
	GC_BRIDGE_TRANSPARENT_CLASS,
	/* Instances of this class should not be scanned when computing the transitive dependency among bridges. E.g. String*/
	GC_BRIDGE_OPAQUE_CLASS,
	/* Instances of this class should be bridged and have their dependency computed. */
	GC_BRIDGE_TRANSPARENT_BRIDGE_CLASS,
	/* Instances of this class should be bridged but no dependencies should not be calculated. */
	GC_BRIDGE_OPAQUE_BRIDGE_CLASS,
} MonoGCBridgeObjectKind;

typedef struct {
	mono_bool is_alive;	/* to be set by the cross reference callback */
	int num_objs;
	MonoObject *objs [MONO_ZERO_LEN_ARRAY];
} MonoGCBridgeSCC;

typedef struct {
	int src_scc_index;
	int dst_scc_index;
} MonoGCBridgeXRef;

typedef struct {
	int bridge_version;
	/*
	 * Tells the runtime which classes to even consider when looking for
	 * bridged objects.  If subclasses are to be considered as well, the
	 * subclass check must be done in the callback.
	 */
	MonoGCBridgeObjectKind (*bridge_class_kind) (MonoClass *klass);
	/*
	 * This is only called on objects for whose classes
	 * `bridge_class_kind()` returned `XXX_BRIDGE_CLASS`.
	 */
	mono_bool (*is_bridge_object) (MonoObject *object);
	void (*cross_references) (int num_sccs, MonoGCBridgeSCC **sccs, int num_xrefs, MonoGCBridgeXRef *xrefs);
} MonoGCBridgeCallbacks;

MONO_END_DECLS

#endif /* _MONO_SGEN_BRIDGE_TYPES_H */
