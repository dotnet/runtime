/*
 * sgen-protocol.h: Binary protocol of internal activity, to aid
 * debugging.
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright (C) 2012 Xamarin Inc
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Library General Public
 * License 2.0 as published by the Free Software Foundation;
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Library General Public License for more details.
 *
 * You should have received a copy of the GNU Library General Public
 * License 2.0 along with this library; if not, write to the Free
 * Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
 */

#include "sgen-gc.h"

#ifdef SGEN_BINARY_PROTOCOL

enum {
	SGEN_PROTOCOL_COLLECTION_FORCE,
	SGEN_PROTOCOL_COLLECTION_BEGIN,
	SGEN_PROTOCOL_COLLECTION_END,
	SGEN_PROTOCOL_ALLOC,
	SGEN_PROTOCOL_COPY,
	SGEN_PROTOCOL_PIN,
	SGEN_PROTOCOL_MARK,
	SGEN_PROTOCOL_SCAN_BEGIN,
	SGEN_PROTOCOL_SCAN_VTYPE_BEGIN,
	SGEN_PROTOCOL_WBARRIER,
	SGEN_PROTOCOL_GLOBAL_REMSET,
	SGEN_PROTOCOL_PTR_UPDATE,
	SGEN_PROTOCOL_CLEANUP,
	SGEN_PROTOCOL_EMPTY,
	SGEN_PROTOCOL_THREAD_SUSPEND,
	SGEN_PROTOCOL_THREAD_RESTART,
	SGEN_PROTOCOL_THREAD_REGISTER,
	SGEN_PROTOCOL_THREAD_UNREGISTER,
	SGEN_PROTOCOL_MISSING_REMSET,
	SGEN_PROTOCOL_ALLOC_PINNED,
	SGEN_PROTOCOL_ALLOC_DEGRADED,
	SGEN_PROTOCOL_CARD_SCAN,
	SGEN_PROTOCOL_CEMENT,
	SGEN_PROTOCOL_CEMENT_RESET,
	SGEN_PROTOCOL_DISLINK_UPDATE,
	SGEN_PROTOCOL_DISLINK_UPDATE_STAGED,
	SGEN_PROTOCOL_DISLINK_PROCESS_STAGED,
	SGEN_PROTOCOL_DOMAIN_UNLOAD_BEGIN,
	SGEN_PROTOCOL_DOMAIN_UNLOAD_END
};

typedef struct {
	int generation;
} SGenProtocolCollectionForce;

typedef struct {
	int index, generation;
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
	gpointer obj;
	gpointer vtable;
	int size;
} SGenProtocolScanBegin;

typedef struct {
	gpointer obj;
	int size;
} SGenProtocolScanVTypeBegin;

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
	gpointer thread, stopped_ip;
} SGenProtocolThreadSuspend;

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

typedef struct {
	gpointer start;
	int size;
} SGenProtocolCardScan;

typedef struct {
	gpointer obj;
	gpointer vtable;
	int size;
} SGenProtocolCement;

typedef struct {
	gpointer link;
	gpointer obj;
	int track;
	int staged;
} SGenProtocolDislinkUpdate;

typedef struct {
	gpointer link;
	gpointer obj;
	int track;
	int index;
} SGenProtocolDislinkUpdateStaged;

typedef struct {
	gpointer link;
	gpointer obj;
	int index;
} SGenProtocolDislinkProcessStaged;

typedef struct {
	gpointer domain;
} SGenProtocolDomainUnload;

/* missing: finalizers, dislinks, roots, non-store wbarriers */

void binary_protocol_init (const char *filename) MONO_INTERNAL;
gboolean binary_protocol_is_enabled (void) MONO_INTERNAL;

void binary_protocol_flush_buffers (gboolean force) MONO_INTERNAL;

void binary_protocol_collection_force (int generation) MONO_INTERNAL;
void binary_protocol_collection_begin (int index, int generation) MONO_INTERNAL;
void binary_protocol_collection_end (int index, int generation) MONO_INTERNAL;
void binary_protocol_alloc (gpointer obj, gpointer vtable, int size) MONO_INTERNAL;
void binary_protocol_alloc_pinned (gpointer obj, gpointer vtable, int size) MONO_INTERNAL;
void binary_protocol_alloc_degraded (gpointer obj, gpointer vtable, int size) MONO_INTERNAL;
void binary_protocol_copy (gpointer from, gpointer to, gpointer vtable, int size) MONO_INTERNAL;
void binary_protocol_pin (gpointer obj, gpointer vtable, int size) MONO_INTERNAL;
void binary_protocol_mark (gpointer obj, gpointer vtable, int size) MONO_INTERNAL;
void binary_protocol_scan_begin (gpointer obj, gpointer vtable, int size) MONO_INTERNAL;
void binary_protocol_scan_vtype_begin (gpointer start, int size) MONO_INTERNAL;
void binary_protocol_wbarrier (gpointer ptr, gpointer value, gpointer value_vtable) MONO_INTERNAL;
void binary_protocol_global_remset (gpointer ptr, gpointer value, gpointer value_vtable) MONO_INTERNAL;
void binary_protocol_ptr_update (gpointer ptr, gpointer old_value, gpointer new_value, gpointer vtable, int size) MONO_INTERNAL;
void binary_protocol_cleanup (gpointer ptr, gpointer vtable, int size) MONO_INTERNAL;
void binary_protocol_empty (gpointer start, int size) MONO_INTERNAL;
void binary_protocol_thread_suspend (gpointer thread, gpointer stopped_ip) MONO_INTERNAL;
void binary_protocol_thread_restart (gpointer thread) MONO_INTERNAL;
void binary_protocol_thread_register (gpointer thread) MONO_INTERNAL;
void binary_protocol_thread_unregister (gpointer thread) MONO_INTERNAL;
void binary_protocol_missing_remset (gpointer obj, gpointer obj_vtable, int offset,
		gpointer value, gpointer value_vtable, int value_pinned) MONO_INTERNAL;
void binary_protocol_card_scan (gpointer start, int size) MONO_INTERNAL;
void binary_protocol_cement (gpointer ptr, gpointer vtable, int size) MONO_INTERNAL;
void binary_protocol_cement_reset (void) MONO_INTERNAL;
void binary_protocol_dislink_update (gpointer link, gpointer obj, int track, int staged) MONO_INTERNAL;
void binary_protocol_dislink_update_staged (gpointer link, gpointer obj, int track, int index) MONO_INTERNAL;
void binary_protocol_dislink_process_staged (gpointer link, gpointer obj, int index) MONO_INTERNAL;
void binary_protocol_domain_unload_begin (gpointer domain) MONO_INTERNAL;
void binary_protocol_domain_unload_end (gpointer domain) MONO_INTERNAL;

#else

#define binary_protocol_is_enabled()	FALSE

#define binary_protocol_flush_buffers(force)
#define binary_protocol_collection_force(generation)
#define binary_protocol_collection_begin(index, generation)
#define binary_protocol_collection_end(index, generation)
#define binary_protocol_alloc(obj, vtable, size)
#define binary_protocol_alloc_pinned(obj, vtable, size)
#define binary_protocol_alloc_degraded(obj, vtable, size)
#define binary_protocol_copy(from, to, vtable, size)
#define binary_protocol_pin(obj, vtable, size)
#define binary_protocol_mark(obj, vtable, size)
#define binary_protocol_scan_begin(obj, vtable, size)
#define binary_protocol_scan_vtype_begin(obj, size)
#define binary_protocol_wbarrier(ptr, value, value_vtable)
#define binary_protocol_global_remset(ptr, value, value_vtable)
#define binary_protocol_ptr_update(ptr, old_value, new_value, vtable, size)
#define binary_protocol_cleanup(ptr, vtable, size)
#define binary_protocol_empty(start, size)
#define binary_protocol_thread_suspend(thread, ip)
#define binary_protocol_thread_restart(thread)
#define binary_protocol_thread_register(thread)
#define binary_protocol_thread_unregister(thread)
#define binary_protocol_missing_remset(obj, obj_vtable, offset, value, value_vtable, value_pinned)
#define binary_protocol_card_scan(start, size)
#define binary_protocol_cement(ptr, vtable, size)
#define binary_protocol_cement_reset()
#define binary_protocol_dislink_update(link,obj,track,staged)
#define binary_protocol_dislink_update_staged(link,obj,track,index)
#define binary_protocol_dislink_process_staged(link,obj,index)
#define binary_protocol_domain_unload_begin(domain)
#define binary_protocol_domain_unload_end(domain)

#endif
