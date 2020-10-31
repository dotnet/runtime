/**
 * \file
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
 * 
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"
#ifdef HAVE_SGEN_GC

#ifndef DISABLE_SGEN_DEBUG_HELPERS

#include <string.h>

#include "mono/sgen/sgen-gc.h"
#include "mono/sgen/sgen-pinning.h"
#include "mono/sgen/sgen-hash-table.h"
#include "mono/sgen/sgen-client.h"

typedef struct _PinStatAddress PinStatAddress;
struct _PinStatAddress {
	char *addr;
	int pin_types;
	PinStatAddress *left;
	PinStatAddress *right;
};

typedef struct {
	size_t num_pins [PIN_TYPE_MAX];
} PinnedClassEntry;

typedef struct {
	gulong num_remsets;
} GlobalRemsetClassEntry;

static gboolean do_pin_stats = FALSE;

static PinStatAddress *pin_stat_addresses = NULL;
static size_t pinned_byte_counts [PIN_TYPE_MAX];

static size_t pinned_bytes_in_generation [GENERATION_MAX];
static int pinned_objects_in_generation [GENERATION_MAX];

static SgenPointerQueue pinned_objects = SGEN_POINTER_QUEUE_INIT (INTERNAL_MEM_STATISTICS);

static SgenHashTable pinned_class_hash_table = SGEN_HASH_TABLE_INIT (INTERNAL_MEM_STATISTICS, INTERNAL_MEM_STAT_PINNED_CLASS, sizeof (PinnedClassEntry), g_str_hash, g_str_equal);
static SgenHashTable global_remset_class_hash_table = SGEN_HASH_TABLE_INIT (INTERNAL_MEM_STATISTICS, INTERNAL_MEM_STAT_REMSET_CLASS, sizeof (GlobalRemsetClassEntry), g_str_hash, g_str_equal);

void
sgen_pin_stats_enable (void)
{
	do_pin_stats = TRUE;
}

static void
pin_stats_tree_free (PinStatAddress *node)
{
	if (!node)
		return;
	pin_stats_tree_free (node->left);
	pin_stats_tree_free (node->right);
	sgen_free_internal_dynamic (node, sizeof (PinStatAddress), INTERNAL_MEM_STATISTICS);
}

void
sgen_pin_stats_reset (void)
{
	int i;
	pin_stats_tree_free (pin_stat_addresses);
	pin_stat_addresses = NULL;
	for (i = 0; i < PIN_TYPE_MAX; ++i)
		pinned_byte_counts [i] = 0;
	for (i = 0; i < GENERATION_MAX; ++i) {
		pinned_bytes_in_generation [i] = 0;
		pinned_objects_in_generation [i] = 0;
	}
	sgen_pointer_queue_clear (&pinned_objects);
	sgen_hash_table_clean (&pinned_class_hash_table);
	sgen_hash_table_clean (&global_remset_class_hash_table);
}

void
sgen_pin_stats_register_address (char *addr, int pin_type)
{
	PinStatAddress **node_ptr = &pin_stat_addresses;
	PinStatAddress *node;
	int pin_type_bit = 1 << pin_type;

	if (!do_pin_stats)
		return;
	while (*node_ptr) {
		node = *node_ptr;
		if (addr == node->addr) {
			node->pin_types |= pin_type_bit;
			return;
		}
		if (addr < node->addr)
			node_ptr = &node->left;
		else
			node_ptr = &node->right;
	}

	node = (PinStatAddress *)sgen_alloc_internal_dynamic (sizeof (PinStatAddress), INTERNAL_MEM_STATISTICS, TRUE);
	node->addr = addr;
	node->pin_types = pin_type_bit;
	node->left = node->right = NULL;

	*node_ptr = node;
}

static void
pin_stats_count_object_from_tree (GCObject *object, size_t size, PinStatAddress *node, int *pin_types)
{
	char *obj = (char*)object;
	if (!node)
		return;
	if (node->addr >= obj && node->addr < obj + size) {
		int i;
		for (i = 0; i < PIN_TYPE_MAX; ++i) {
			int pin_bit = 1 << i;
			if (!(*pin_types & pin_bit) && (node->pin_types & pin_bit)) {
				pinned_byte_counts [i] += size;
				*pin_types |= pin_bit;
			}
		}
	}
	if (obj < node->addr)
		pin_stats_count_object_from_tree (object, size, node->left, pin_types);
	if (obj + size - 1 > node->addr)
		pin_stats_count_object_from_tree (object, size, node->right, pin_types);
}

static gpointer
lookup_vtable_entry (SgenHashTable *hash_table, GCVTable vtable, gpointer empty_entry)
{
	char *name = g_strdup_printf ("%s.%s", sgen_client_vtable_get_namespace (vtable), sgen_client_vtable_get_name (vtable));
	gpointer entry = sgen_hash_table_lookup (hash_table, name);

	if (entry) {
		g_free (name);
	} else {
		sgen_hash_table_replace (hash_table, name, empty_entry, NULL);
		entry = sgen_hash_table_lookup (hash_table, name);
	}

	return entry;
}

static void
register_vtable (GCVTable vtable, int pin_types)
{
	PinnedClassEntry empty_entry;
	PinnedClassEntry *entry;
	int i;

	memset (&empty_entry, 0, sizeof (PinnedClassEntry));
	entry = (PinnedClassEntry *)lookup_vtable_entry (&pinned_class_hash_table, vtable, &empty_entry);

	for (i = 0; i < PIN_TYPE_MAX; ++i) {
		if (pin_types & (1 << i))
			++entry->num_pins [i];
	}
}

void
sgen_pin_stats_register_object (GCObject *obj, int generation)
{
	int pin_types = 0;
	size_t size = 0;

	if (sgen_binary_protocol_is_enabled ()) {
		size = sgen_safe_object_get_size (obj);
		pinned_bytes_in_generation [generation] += size;
		++pinned_objects_in_generation [generation];
	}

	if (!do_pin_stats)
		return;

	if (!size)
		size = sgen_safe_object_get_size (obj);

	pin_stats_count_object_from_tree (obj, size, pin_stat_addresses, &pin_types);
	sgen_pointer_queue_add (&pinned_objects, obj);

	if (pin_types)
		register_vtable (SGEN_LOAD_VTABLE (obj), pin_types);
}

void
sgen_pin_stats_register_global_remset (GCObject *obj)
{
	GlobalRemsetClassEntry empty_entry;
	GlobalRemsetClassEntry *entry;

	if (!do_pin_stats)
		return;

	memset (&empty_entry, 0, sizeof (GlobalRemsetClassEntry));
	entry = (GlobalRemsetClassEntry *)lookup_vtable_entry (&global_remset_class_hash_table, SGEN_LOAD_VTABLE (obj), &empty_entry);

	++entry->num_remsets;
}

void
sgen_pin_stats_report (void)
{
	char *name;
	PinnedClassEntry *pinned_entry;
	GlobalRemsetClassEntry *remset_entry;

	sgen_binary_protocol_pin_stats (pinned_objects_in_generation [GENERATION_NURSERY], pinned_bytes_in_generation [GENERATION_NURSERY],
			pinned_objects_in_generation [GENERATION_OLD], pinned_bytes_in_generation [GENERATION_OLD]);

	if (!do_pin_stats)
		return;

	mono_gc_printf (sgen_gc_debug_file, "\n%-50s  %10s  %10s  %10s\n", "Class", "Stack", "Static", "Other");
	SGEN_HASH_TABLE_FOREACH (&pinned_class_hash_table, char *, name, PinnedClassEntry *, pinned_entry) {
		int i;
		mono_gc_printf (sgen_gc_debug_file, "%-50s", name);
		for (i = 0; i < PIN_TYPE_MAX; ++i)
			mono_gc_printf (sgen_gc_debug_file, "  %10ld", (long)pinned_entry->num_pins [i]);
		mono_gc_printf (sgen_gc_debug_file, "\n");
	} SGEN_HASH_TABLE_FOREACH_END;

	mono_gc_printf (sgen_gc_debug_file, "\n%-50s  %10s\n", "Class", "#Remsets");
	SGEN_HASH_TABLE_FOREACH (&global_remset_class_hash_table, char *, name, GlobalRemsetClassEntry *, remset_entry) {
		mono_gc_printf (sgen_gc_debug_file, "%-50s  %10ld\n", name, remset_entry->num_remsets);
	} SGEN_HASH_TABLE_FOREACH_END;

	mono_gc_printf (sgen_gc_debug_file, "\nTotal bytes pinned from stack: %ld  static: %ld  other: %ld\n",
			(long)pinned_byte_counts [PIN_TYPE_STACK],
			(long)pinned_byte_counts [PIN_TYPE_STATIC_DATA],
			(long)pinned_byte_counts [PIN_TYPE_OTHER]);
}

size_t
sgen_pin_stats_get_pinned_byte_count (int pin_type)
{
	return pinned_byte_counts [pin_type];
}

SgenPointerQueue*
sgen_pin_stats_get_object_list (void)
{
	return &pinned_objects;
}

#endif

#endif /* HAVE_SGEN_GC */
