#ifndef __MONO_MINI_GC_H__
#define __MONO_MINI_GC_H__

#include "mini.h"

/*
 * The GC type of a stack slot or register.
 * This can change through the method as follows:
 * - a SLOT_REF can become SLOT_NOREF and vice-versa when it becomes live/dead.
 * - a SLOT_PIN can become SLOT_REF after it has been definitely assigned.
 */
typedef enum {
	/* Stack slot doesn't contain a reference */
	SLOT_NOREF = 0,
	/* Stack slot contains a reference */
	SLOT_REF = 1,
	/* No info or managed pointer, slot needs to be scanned conservatively */
	SLOT_PIN = 2
} GCSlotType;

void mini_gc_init (void) MONO_INTERNAL;

void mini_gc_init_cfg (MonoCompile *cfg) MONO_INTERNAL;

void mini_gc_create_gc_map (MonoCompile *cfg) MONO_INTERNAL;

void mini_gc_set_slot_type_from_fp (MonoCompile *cfg, int slot_offset, GCSlotType type) MONO_INTERNAL;

void mini_gc_set_slot_type_from_cfa (MonoCompile *cfg, int slot_offset, GCSlotType type) MONO_INTERNAL;

#endif
