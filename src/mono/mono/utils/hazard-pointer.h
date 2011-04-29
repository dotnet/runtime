/*
 * hazard-pointer.h: Hazard pointer related code.
 *
 * (C) Copyright 2011 Novell, Inc
 */
#ifndef __MONO_HAZARD_POINTER_H__
#define __MONO_HAZARD_POINTER_H__

#include <glib.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-membar.h>

#define HAZARD_POINTER_COUNT 3

typedef struct {
	gpointer hazard_pointers [HAZARD_POINTER_COUNT];
} MonoThreadHazardPointers;

typedef void (*MonoHazardousFreeFunc) (gpointer p);

void mono_thread_hazardous_free_or_queue (gpointer p, MonoHazardousFreeFunc free_func) MONO_INTERNAL;
void mono_thread_hazardous_try_free_all (void) MONO_INTERNAL;
MonoThreadHazardPointers* mono_hazard_pointer_get (void) MONO_INTERNAL;
gpointer get_hazardous_pointer (gpointer volatile *pp, MonoThreadHazardPointers *hp, int hazard_index) MONO_INTERNAL;

#define mono_hazard_pointer_set(hp,i,v)	\
	do { g_assert ((i) >= 0 && (i) < HAZARD_POINTER_COUNT); \
		(hp)->hazard_pointers [(i)] = (v); \
		mono_memory_write_barrier (); \
	} while (0)

#define mono_hazard_pointer_get_val(hp,i)	\
	((hp)->hazard_pointers [(i)])

#define mono_hazard_pointer_clear(hp,i)	\
	do { g_assert ((i) >= 0 && (i) < HAZARD_POINTER_COUNT); \
		(hp)->hazard_pointers [(i)] = NULL; \
	} while (0)


void mono_thread_small_id_free (int id) MONO_INTERNAL;
int mono_thread_small_id_alloc (void) MONO_INTERNAL;

void mono_thread_smr_init (void) MONO_INTERNAL;
void mono_thread_smr_cleanup (void) MONO_INTERNAL;
#endif /*__MONO_HAZARD_POINTER_H__*/
