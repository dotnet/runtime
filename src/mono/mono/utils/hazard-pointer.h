/**
 * \file
 * Hazard pointer related code.
 *
 * (C) Copyright 2011 Novell, Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_HAZARD_POINTER_H__
#define __MONO_HAZARD_POINTER_H__

#include <glib.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-membar.h>
#include <mono/utils/mono-publib.h>

#define HAZARD_POINTER_COUNT 3

typedef struct {
	gpointer hazard_pointers [HAZARD_POINTER_COUNT];
} MonoThreadHazardPointers;

typedef void (*MonoHazardousFreeFunc) (gpointer p);

MONO_API gboolean mono_thread_hazardous_try_free (gpointer p, MonoHazardousFreeFunc free_func);
MONO_API void mono_thread_hazardous_queue_free (gpointer p, MonoHazardousFreeFunc free_func);

MONO_API void mono_thread_hazardous_try_free_all (void);
void mono_thread_hazardous_try_free_some (void);
MONO_API MonoThreadHazardPointers* mono_hazard_pointer_get (void);
gpointer mono_get_hazardous_pointer (gpointer volatile *pp, MonoThreadHazardPointers *hp, int hazard_index);

#define mono_hazard_pointer_set(hp,i,v)	\
	do { g_assert ((i) >= 0 && (i) < HAZARD_POINTER_COUNT); \
		(hp)->hazard_pointers [(i)] = (v); \
		mono_memory_write_barrier (); \
	} while (0)

#define mono_hazard_pointer_get_val(hp,i)	\
	((hp)->hazard_pointers [(i)])

#define mono_hazard_pointer_clear(hp,i)	\
	do { g_assert ((i) >= 0 && (i) < HAZARD_POINTER_COUNT); \
		mono_memory_write_barrier (); \
		(hp)->hazard_pointers [(i)] = NULL; \
	} while (0)


void mono_thread_small_id_free (int id);
int mono_thread_small_id_alloc (void);

int mono_hazard_pointer_save_for_signal_handler (void);
void mono_hazard_pointer_restore_for_signal_handler (int small_id);

typedef void (*MonoHazardFreeQueueSizeCallback)(size_t size);
void mono_hazard_pointer_install_free_queue_size_callback (MonoHazardFreeQueueSizeCallback cb);

void mono_thread_smr_init (void);
void mono_thread_smr_cleanup (void);
#endif /*__MONO_HAZARD_POINTER_H__*/
