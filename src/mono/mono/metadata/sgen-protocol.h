/*
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
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

#include "sgen-gc.h"

#ifdef SGEN_BINARY_PROTOCOL

enum {
	SGEN_PROTOCOL_COLLECTION,
	SGEN_PROTOCOL_ALLOC,
	SGEN_PROTOCOL_COPY,
	SGEN_PROTOCOL_PIN,
	SGEN_PROTOCOL_MARK,
	SGEN_PROTOCOL_WBARRIER,
	SGEN_PROTOCOL_GLOBAL_REMSET,
	SGEN_PROTOCOL_PTR_UPDATE,
	SGEN_PROTOCOL_CLEANUP,
	SGEN_PROTOCOL_EMPTY,
	SGEN_PROTOCOL_THREAD_RESTART,
	SGEN_PROTOCOL_THREAD_REGISTER,
	SGEN_PROTOCOL_THREAD_UNREGISTER,
	SGEN_PROTOCOL_MISSING_REMSET,
	SGEN_PROTOCOL_ALLOC_PINNED,
	SGEN_PROTOCOL_ALLOC_DEGRADED
};

typedef struct {
	int generation;
} SGenProtocolCollection;

typedef struct {
	gpointer obj;
	gpointer vtable;
	int size;
} SGenProtocolAlloc;

typedef struct {
	gpointer from;
	gpointer to;
	gpointer vtable;
	int size;
} SGenProtocolCopy;

typedef struct {
	gpointer obj;
	gpointer vtable;
	int size;
} SGenProtocolPin;

typedef struct {
	gpointer obj;
	gpointer vtable;
	int size;
} SGenProtocolMark;

typedef struct {
	gpointer ptr;
	gpointer value;
	gpointer value_vtable;
} SGenProtocolWBarrier;

typedef struct {
	gpointer ptr;
	gpointer value;
	gpointer value_vtable;
} SGenProtocolGlobalRemset;

typedef struct {
	gpointer ptr;
	gpointer old_value;
	gpointer new_value;
	gpointer vtable;
	int size;
} SGenProtocolPtrUpdate;

typedef struct {
	gpointer ptr;
	gpointer vtable;
	int size;
} SGenProtocolCleanup;

typedef struct {
	gpointer start;
	int size;
} SGenProtocolEmpty;

typedef struct {
	gpointer thread;
} SGenProtocolThreadRestart;

typedef struct {
	gpointer thread;
} SGenProtocolThreadRegister;

typedef struct {
	gpointer thread;
} SGenProtocolThreadUnregister;

typedef struct {
	gpointer obj;
	gpointer obj_vtable;
	int offset;
	gpointer value;
	gpointer value_vtable;
	int value_pinned;
} SGenProtocolMissingRemset;

/* missing: finalizers, dislinks, roots, non-store wbarriers */

void binary_protocol_init (const char *filename) MONO_INTERNAL;
gboolean binary_protocol_is_enabled (void) MONO_INTERNAL;

void binary_protocol_flush_buffers (gboolean force) MONO_INTERNAL;

void binary_protocol_collection (int generation) MONO_INTERNAL;
void binary_protocol_alloc (gpointer obj, gpointer vtable, int size) MONO_INTERNAL;
void binary_protocol_alloc_pinned (gpointer obj, gpointer vtable, int size) MONO_INTERNAL;
void binary_protocol_alloc_degraded (gpointer obj, gpointer vtable, int size) MONO_INTERNAL;
void binary_protocol_copy (gpointer from, gpointer to, gpointer vtable, int size) MONO_INTERNAL;
void binary_protocol_pin (gpointer obj, gpointer vtable, int size) MONO_INTERNAL;
void binary_protocol_mark (gpointer obj, gpointer vtable, int size) MONO_INTERNAL;
void binary_protocol_wbarrier (gpointer ptr, gpointer value, gpointer value_vtable) MONO_INTERNAL;
void binary_protocol_global_remset (gpointer ptr, gpointer value, gpointer value_vtable) MONO_INTERNAL;
void binary_protocol_ptr_update (gpointer ptr, gpointer old_value, gpointer new_value, gpointer vtable, int size) MONO_INTERNAL;
void binary_protocol_cleanup (gpointer ptr, gpointer vtable, int size) MONO_INTERNAL;
void binary_protocol_empty (gpointer start, int size) MONO_INTERNAL;
void binary_protocol_thread_restart (gpointer thread) MONO_INTERNAL;
void binary_protocol_thread_register (gpointer thread) MONO_INTERNAL;
void binary_protocol_thread_unregister (gpointer thread) MONO_INTERNAL;
void binary_protocol_missing_remset (gpointer obj, gpointer obj_vtable, int offset,
		gpointer value, gpointer value_vtable, int value_pinned) MONO_INTERNAL;

#else

#define binary_protocol_is_enabled()	FALSE

#define binary_protocol_flush_buffers(force)
#define binary_protocol_collection(generation)
#define binary_protocol_alloc(obj, vtable, size)
#define binary_protocol_alloc_pinned(obj, vtable, size)
#define binary_protocol_alloc_degraded(obj, vtable, size)
#define binary_protocol_copy(from, to, vtable, size)
#define binary_protocol_pin(obj, vtable, size)
#define binary_protocol_mark(obj, vtable, size)
#define binary_protocol_wbarrier(ptr, value, value_vtable)
#define binary_protocol_global_remset(ptr, value, value_vtable)
#define binary_protocol_ptr_update(ptr, old_value, new_value, vtable, size)
#define binary_protocol_cleanup(ptr, vtable, size)
#define binary_protocol_empty(start, size)
#define binary_protocol_thread_restart(thread)
#define binary_protocol_thread_register(thread)
#define binary_protocol_thread_unregister(thread)
#define binary_protocol_missing_remset(obj, obj_vtable, offset, value, value_vtable, value_pinned)

#endif
