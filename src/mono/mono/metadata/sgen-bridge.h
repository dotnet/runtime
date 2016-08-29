/*
 * Copyright 2011 Novell, Inc.
 * 
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

/*
 * The bridge is a mechanism for SGen to let clients override the death of some
 * unreachable objects.  We use it in monodroid to do garbage collection across
 * the Mono and Java heaps.
 *
 * The client (Monodroid) can designate some objects as "bridged", which means
 * that they participate in the bridge processing step once SGen considers them
 * unreachable, i.e., dead.  Bridged objects must be registered for
 * finalization.
 *
 * When SGen is done marking, it puts together a list of all dead bridged
 * objects.  This is passed to the bridge processor, which does an analysis to
 * simplify the graph: It replaces strongly-connected components with single
 * nodes, and may remove nodes corresponding to components which do not contain
 * bridged objects.
 *
 * The output of the SCC analysis is passed to the client's `cross_references()`
 * callback.  This consists of 2 arrays, an array of SCCs (MonoGCBridgeSCC),
 * and an array of "xrefs" (edges between SCCs, MonoGCBridgeXRef).  Edges are
 * encoded as pairs of "API indices", ie indexes in the SCC array.  The client
 * is expected to set the `is_alive` flag on those strongly connected components
 * that it wishes to be kept alive.
 *
 * In monodroid each bridged object has a corresponding Java mirror object.  In
 * the bridge callback it reifies the Mono object graph in the Java heap so that
 * the full, combined object graph is now instantiated on the Java side.  Then
 * it triggers a Java GC, waits for it to finish, and checks which of the Java
 * mirror objects are still alive.  For those it sets the `is_alive` flag and
 * returns from the callback.
 *
 * The SCC analysis is done while the world is stopped, but the callback is made
 * with the world running again.  Weak links to bridged objects and other
 * objects reachable from them are kept until the callback returns, at which
 * point all links to bridged objects that don't have `is_alive` set are nulled.
 * Note that weak links to non-bridged objects reachable from bridged objects
 * are not nulled.  This might be considered a bug.
 *
 * There are three different implementations of the bridge processor, each of
 * which implements 8 callbacks (see SgenBridgeProcessor).  The implementations
 * differ in the algorithm they use to compute the "simplified" SCC graph.
 */

#ifndef _MONO_SGEN_BRIDGE_H_
#define _MONO_SGEN_BRIDGE_H_

#include <mono/utils/mono-publib.h>

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

/*
 * Note: This may be called at any time, but cannot be called concurrently
 * with (during and on a separate thread from) sgen init. Callers are
 * responsible for enforcing this.
 */
MONO_API void mono_gc_register_bridge_callbacks (MonoGCBridgeCallbacks *callbacks);

MONO_API void mono_gc_wait_for_bridge_processing (void);

MONO_END_DECLS

#endif
