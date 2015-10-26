/*
 * Copyright 2011 Novell, Inc.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

/*
 * The bridge is a mechanism for SGen to let clients override the death of some
 * unreachable objects.  We use it in monodroid to do garbage collection across
 * the Mono and Java heaps.
 *
 * The client can designate some objects as "bridged", which means that they
 * participate in the bridge processing step once SGen considers them
 * unreachable, i.e., dead.  Bridged objects must be registered for
 * finalization.
 *
 * When SGen is done marking, it puts together a list of all dead bridged
 * objects and then does a strongly connected component analysis over their
 * object graph.  That graph will usually contain non-bridged objects, too.
 *
 * The output of the SCC analysis is passed to the `cross_references()`
 * callback.  It is expected to set the `is_alive` flag on those strongly
 * connected components that it wishes to be kept alive.  Only bridged objects
 * will be reported to the callback, i.e., non-bridged objects are removed from
 * the callback graph.
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
 */

#ifndef _MONO_SGEN_BRIDGE_H_
#define _MONO_SGEN_BRIDGE_H_

#include <mono/utils/mono-publib.h>

MONO_BEGIN_DECLS

enum {
	SGEN_BRIDGE_VERSION = 4
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

MONO_API void mono_gc_register_bridge_callbacks (MonoGCBridgeCallbacks *callbacks);

MONO_API void mono_gc_wait_for_bridge_processing (void);

MONO_END_DECLS

#endif
