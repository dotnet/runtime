/**
 * \file
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

#include <mono/metadata/details/sgen-bridge-types.h>

MONO_BEGIN_DECLS

#define MONO_API_FUNCTION(ret,name,args) MONO_API ret name args;
#include <mono/metadata/details/sgen-bridge-functions.h>
#undef MONO_API_FUNCTION

MONO_END_DECLS

#endif
