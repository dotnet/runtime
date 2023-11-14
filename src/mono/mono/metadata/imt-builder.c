// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include "config.h"
#include <glib.h>
#include <mono/metadata/class-init.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/object-internals.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/unlocked.h>

static gboolean always_build_imt_trampolines;

#if (MONO_IMT_SIZE > 32)
#error "MONO_IMT_SIZE cannot be larger than 32"
#endif

void
mono_set_always_build_imt_trampolines (gboolean value)
{
	always_build_imt_trampolines = value;
}

#define rot(x,k) (((x)<<(k)) | ((x)>>(32-(k))))
#define mix(a,b,c) { \
	a -= c;  a ^= rot(c, 4);  c += b; \
	b -= a;  b ^= rot(a, 6);  a += c; \
	c -= b;  c ^= rot(b, 8);  b += a; \
	a -= c;  a ^= rot(c,16);  c += b; \
	b -= a;  b ^= rot(a,19);  a += c; \
	c -= b;  c ^= rot(b, 4);  b += a; \
}
#define mono_final(a,b,c) { \
	c ^= b; c -= rot(b,14); \
	a ^= c; a -= rot(c,11); \
	b ^= a; b -= rot(a,25); \
	c ^= b; c -= rot(b,16); \
	a ^= c; a -= rot(c,4);  \
	b ^= a; b -= rot(a,14); \
	c ^= b; c -= rot(b,24); \
}

/*
 * mono_method_get_imt_slot:
 *
 *   The IMT slot is embedded into AOTed code, so this must return the same value
 * for the same method across all executions. This means:
 * - pointers shouldn't be used as hash values.
 * - mono_metadata_str_hash () should be used for hashing strings.
 */
guint32
mono_method_get_imt_slot (MonoMethod *method)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	MonoMethodSignature *sig;
	int hashes_count;
	guint32 *hashes_start, *hashes;
	guint32 a, b, c;
	int i;

	/* This can be used to stress tests the collision code */
	//return 0;

	/*
	 * We do this to simplify generic sharing.  It will hurt
	 * performance in cases where a class implements two different
	 * instantiations of the same generic interface.
	 * The code in build_imt_slots () depends on this.
	 */
	if (method->is_inflated)
		method = ((MonoMethodInflated*)method)->declaring;

	sig = mono_method_signature_internal (method);
	hashes_count = sig->param_count + 4;
	hashes_start = (guint32 *)g_malloc (hashes_count * sizeof (guint32));
	hashes = hashes_start;

	if (! MONO_CLASS_IS_INTERFACE_INTERNAL (method->klass)) {
		g_error ("mono_method_get_imt_slot: %s.%s.%s is not an interface MonoMethod",
				m_class_get_name_space (method->klass), m_class_get_name (method->klass), method->name);
	}

	/* Initialize hashes */
	hashes [0] = mono_metadata_str_hash (m_class_get_name (method->klass));
	hashes [1] = mono_metadata_str_hash (m_class_get_name_space (method->klass));
	hashes [2] = mono_metadata_str_hash (method->name);
	hashes [3] = mono_metadata_type_hash (sig->ret);
	for (i = 0; i < sig->param_count; i++) {
		hashes [4 + i] = mono_metadata_type_hash (sig->params [i]);
	}

	/* Setup internal state */
	a = b = c = 0xdeadbeef + (((guint32)hashes_count)<<2);

	/* Handle most of the hashes */
	while (hashes_count > 3) {
		a += hashes [0];
		b += hashes [1];
		c += hashes [2];
		mix (a,b,c);
		hashes_count -= 3;
		hashes += 3;
	}

	/* Handle the last 3 hashes (all the case statements fall through) */
	switch (hashes_count) {
	case 3 : c += hashes [2];
	case 2 : b += hashes [1];
	case 1 : a += hashes [0];
		mono_final (a,b,c);
	case 0: /* nothing left to add */
		break;
	}

	g_free (hashes_start);
	/* Report the result */
	return c % MONO_IMT_SIZE;
}
#undef rot
#undef mix
#undef mono_final

#define DEBUG_IMT 0

static void
add_imt_builder_entry (MonoImtBuilderEntry **imt_builder, MonoMethod *method, guint32 *imt_collisions_bitmap, int vtable_slot, int slot_num) {
	MONO_REQ_GC_NEUTRAL_MODE;

	g_assert (slot_num >= 0 && slot_num < MONO_IMT_SIZE);

	guint32 imt_slot = mono_method_get_imt_slot (method);
	MonoImtBuilderEntry *entry;

	if (imt_slot != slot_num) {
		/* we build just a single imt slot and this is not it */
		return;
	}

	entry = (MonoImtBuilderEntry *)g_malloc0 (sizeof (MonoImtBuilderEntry));
	entry->key = method;
	entry->value.vtable_slot = vtable_slot;
	entry->next = imt_builder [imt_slot];
	if (imt_builder [imt_slot] != NULL) {
		entry->children = imt_builder [imt_slot]->children + 1;
		if (entry->children == 1) {
			UnlockedIncrement (&mono_stats.imt_slots_with_collisions);
			*imt_collisions_bitmap |= (1 << imt_slot);
		}
	} else {
		entry->children = 0;
		UnlockedIncrement (&mono_stats.imt_used_slots);
	}
	imt_builder [imt_slot] = entry;
#if DEBUG_IMT
	{
	char *method_name = mono_method_full_name (method, TRUE);
	printf ("Added IMT slot for method (%p) %s: imt_slot = %d, vtable_slot = %d, colliding with other %d entries\n",
			method, method_name, imt_slot, vtable_slot, entry->children);
	g_free (method_name);
	}
#endif
}

#if DEBUG_IMT
static void
print_imt_entry (const char* message, MonoImtBuilderEntry *e, int num) {
	if (e != NULL) {
		MonoMethod *method = e->key;
		printf ("  * %s [%d]: (%p) '%s.%s.%s'\n",
				message,
				num,
				method,
				m_class_get_name_space (method->klass),
				m_class_get_name (method->klass),
				method->name);
	} else {
		printf ("  * %s: NULL\n", message);
	}
}
#endif

static int
compare_imt_builder_entries (const void *p1, const void *p2) {
	MonoImtBuilderEntry *e1 = *(MonoImtBuilderEntry**) p1;
	MonoImtBuilderEntry *e2 = *(MonoImtBuilderEntry**) p2;

	return (e1->key < e2->key) ? -1 : ((e1->key > e2->key) ? 1 : 0);
}

static int
imt_emit_ir (MonoImtBuilderEntry **sorted_array, int start, int end, GPtrArray *out_array)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	int count = end - start;
	int chunk_start = out_array->len;
	if (count < 4) {
		int i;
		for (i = start; i < end; ++i) {
			MonoIMTCheckItem *item = g_new0 (MonoIMTCheckItem, 1);
			item->key = sorted_array [i]->key;
			item->value = sorted_array [i]->value;
			item->has_target_code = sorted_array [i]->has_target_code;
			item->is_equals = TRUE;
			if (i < end - 1)
				item->check_target_idx = out_array->len + 1;
			else
				item->check_target_idx = 0;
			g_ptr_array_add (out_array, item);
		}
	} else {
		int middle = start + count / 2;
		MonoIMTCheckItem *item = g_new0 (MonoIMTCheckItem, 1);

		item->key = sorted_array [middle]->key;
		item->is_equals = FALSE;
		g_ptr_array_add (out_array, item);
		imt_emit_ir (sorted_array, start, middle, out_array);
		item->check_target_idx = imt_emit_ir (sorted_array, middle, end, out_array);
	}
	return chunk_start;
}

static GPtrArray*
imt_sort_slot_entries (MonoImtBuilderEntry *entries) {
	MONO_REQ_GC_NEUTRAL_MODE;

	int number_of_entries = entries->children + 1;
	MonoImtBuilderEntry **sorted_array = (MonoImtBuilderEntry **)g_malloc (sizeof (MonoImtBuilderEntry*) * number_of_entries);
	GPtrArray *result = g_ptr_array_new ();
	MonoImtBuilderEntry *current_entry;
	int i;

	for (current_entry = entries, i = 0; current_entry != NULL; current_entry = current_entry->next, i++) {
		sorted_array [i] = current_entry;
	}
	mono_qsort (sorted_array, number_of_entries, sizeof (MonoImtBuilderEntry*), compare_imt_builder_entries);

	/*for (i = 0; i < number_of_entries; i++) {
		print_imt_entry (" sorted array:", sorted_array [i], i);
	}*/

	imt_emit_ir (sorted_array, 0, number_of_entries, result);

	g_free (sorted_array);
	return result;
}

static gpointer
initialize_imt_slot (MonoVTable *vtable, MonoImtBuilderEntry *imt_builder_entry, gpointer fail_tramp)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	if (imt_builder_entry != NULL) {
		if (imt_builder_entry->children == 0 && !fail_tramp && !always_build_imt_trampolines) {
			/* No collision, return the vtable slot contents */
			return vtable->vtable [imt_builder_entry->value.vtable_slot];
		} else {
			/* Collision, build the trampoline */
			GPtrArray *imt_ir = imt_sort_slot_entries (imt_builder_entry);
			gpointer result;
			result = mono_get_runtime_callbacks ()->build_imt_trampoline (vtable,
				(MonoIMTCheckItem**)imt_ir->pdata, imt_ir->len, fail_tramp);
			for (guint i = 0; i < imt_ir->len; ++i)
				g_free (g_ptr_array_index (imt_ir, i));
			g_ptr_array_free (imt_ir, TRUE);
			return result;
		}
	} else {
		if (fail_tramp)
			return fail_tramp;
		else
			/* Empty slot */
			return NULL;
	}
}

static MonoImtBuilderEntry*
get_generic_virtual_entries (MonoMemoryManager *mem_manager, gpointer *vtable_slot);

/*
 * LOCKING: assume the loader lock is held
 *
*/
static void
build_imt_slots (MonoClass *klass, MonoVTable *vt, gpointer* imt, int slot_num)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	int i;
	guint32 imt_collisions_bitmap = 0;
	MonoImtBuilderEntry **imt_builder = (MonoImtBuilderEntry **)g_calloc (MONO_IMT_SIZE, sizeof (MonoImtBuilderEntry*));
	int method_count = 0;
	gboolean record_method_count_for_max_collisions = FALSE;
	gboolean has_generic_virtual = FALSE, has_variant_iface = FALSE;
	MonoMemoryManager *mem_manager = m_class_get_mem_manager (klass);

	g_assert (slot_num >= 0 && slot_num < MONO_IMT_SIZE);

#if DEBUG_IMT
	printf ("Building IMT for class %s.%s slot %d\n", m_class_get_name_space (klass), m_class_get_name (klass), slot_num);
#endif
	int klass_interface_offsets_count = m_class_get_interface_offsets_count (klass);
	MonoClass **klass_interfaces_packed = m_class_get_interfaces_packed (klass);
	guint16 *klass_interface_offsets_packed = m_class_get_interface_offsets_packed (klass);
	for (i = 0; i < klass_interface_offsets_count; ++i) {
		MonoClass *iface = klass_interfaces_packed [i];
		int interface_offset = klass_interface_offsets_packed [i];
		int method_slot_in_interface, vt_slot;

		if (mono_class_has_variant_generic_params (iface))
			has_variant_iface = TRUE;

		mono_class_setup_methods (iface);
		vt_slot = interface_offset;
		int mcount = mono_class_get_method_count (iface);
		for (method_slot_in_interface = 0; method_slot_in_interface < mcount; method_slot_in_interface++) {
			MonoMethod *method;

			if (mono_class_is_ginst (iface)) {
				/*
				 * The imt slot of the method is the same as for its declaring method,
				 * see the comment in mono_method_get_imt_slot (), so we can
				 * avoid inflating methods which will be discarded by
				 * add_imt_builder_entry anyway.
				 */
				method = mono_class_get_method_by_index (mono_class_get_generic_class (iface)->container_class, method_slot_in_interface);
				if (m_method_is_static (method)) {
					if (m_method_is_virtual (method))
						vt_slot ++;
					continue;
				}
				if (mono_method_get_imt_slot (method) != slot_num) {
					vt_slot ++;
					continue;
				}
			}
			method = mono_class_get_method_by_index (iface, method_slot_in_interface);
			if (method->is_generic) {
				if (m_method_is_virtual (method)) {
					has_generic_virtual = TRUE;
					vt_slot ++;
				}
				continue;
			}

			if (m_method_is_static (method)) {
				if (m_method_is_virtual (method))
					vt_slot ++;
				continue;
			}

			if (method->flags & METHOD_ATTRIBUTE_VIRTUAL) {
				int vt_slot_const = method->slot + interface_offset;
				if (G_UNLIKELY (vt_slot != vt_slot_const)) {
					char *name = mono_method_full_name (method, TRUE);
					g_assertf (vt_slot == vt_slot_const, "method %s computed slot %d != pre-assigned slot %d (interface %d, slot %d)", name, vt_slot, vt_slot_const, interface_offset, method->slot);
					g_free (name);
				}
				add_imt_builder_entry (imt_builder, method, &imt_collisions_bitmap, vt_slot, slot_num);
				vt_slot ++;
			}
		}
	}
	for (i = 0; i < MONO_IMT_SIZE; ++i) {
		/* overwrite the imt slot only if we're building all the entries or if
		 * we're building this specific one
		 */
		if (i == slot_num) {
			MonoImtBuilderEntry *entries = get_generic_virtual_entries (mem_manager, &imt [i]);

			if (entries) {
				if (imt_builder [i]) {
					MonoImtBuilderEntry *entry;

					/* Link entries with imt_builder [i] */
					for (entry = entries; entry->next; entry = entry->next) {
#if DEBUG_IMT
						MonoMethod *method = (MonoMethod*)entry->key;
						char *method_name = mono_method_full_name (method, TRUE);
						printf ("Added extra entry for method (%p) %s: imt_slot = %d\n", method, method_name, i);
						g_free (method_name);
#endif
					}
					entry->next = imt_builder [i];
					entries->children += imt_builder [i]->children + 1;
				}
				imt_builder [i] = entries;
			}

			if (has_generic_virtual || has_variant_iface) {
				/*
				 * There might be collisions later when the trampoline is expanded.
				 */
				imt_collisions_bitmap |= (1 << i);

				/*
				 * The IMT trampoline might be called with an instance of one of the
				 * generic virtual methods, so has to fallback to the IMT trampoline.
				 */
				imt [i] = initialize_imt_slot (vt, imt_builder [i], mono_get_runtime_callbacks ()->get_imt_trampoline (vt, i));
			} else {
				imt [i] = initialize_imt_slot (vt, imt_builder [i], NULL);
			}
#if DEBUG_IMT
			printf ("initialize_imt_slot[%d]: %p methods %d\n", i, imt [i], imt_builder [i]->children + 1);
#endif
		}

		if (imt_builder [i] != NULL) {
			int methods_in_slot = imt_builder [i]->children + 1;
			if (methods_in_slot > UnlockedRead (&mono_stats.imt_max_collisions_in_slot)) {
				UnlockedWrite (&mono_stats.imt_max_collisions_in_slot, methods_in_slot);
				record_method_count_for_max_collisions = TRUE;
			}
			method_count += methods_in_slot;
		}
	}

	UnlockedAdd (&mono_stats.imt_number_of_methods, method_count);
	if (record_method_count_for_max_collisions) {
		UnlockedWrite (&mono_stats.imt_method_count_when_max_collisions, method_count);
	}

	for (i = 0; i < MONO_IMT_SIZE; i++) {
		MonoImtBuilderEntry* entry = imt_builder [i];
		while (entry != NULL) {
			MonoImtBuilderEntry* next = entry->next;
			g_free (entry);
			entry = next;
		}
	}
	g_free (imt_builder);
	/* we OR the bitmap since we may build just a single imt slot at a time */
	vt->imt_collisions_bitmap |= imt_collisions_bitmap;
}

/**
 * mono_vtable_build_imt_slot:
 * \param vtable virtual object table struct
 * \param imt_slot slot in the IMT table
 * Fill the given \p imt_slot in the IMT table of \p vtable with
 * a trampoline or a trampoline for the case of collisions.
 * This is part of the internal mono API.
 * LOCKING: Take the loader lock.
 */
void
mono_vtable_build_imt_slot (MonoVTable* vtable, int imt_slot)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	gpointer *imt = (gpointer*)vtable;
	imt -= MONO_IMT_SIZE;
	g_assert (imt_slot >= 0 && imt_slot < MONO_IMT_SIZE);

	/* no support for extra interfaces: the proxy objects will need
	 * to build the complete IMT
	 * Update and heck needs to ahppen inside the proper domain lock, as all
	 * the changes made to a MonoVTable.
	 */
	mono_loader_lock ();
	/* we change the slot only if it wasn't changed from the generic imt trampoline already */
	if (!mono_get_runtime_callbacks ()->imt_entry_inited (vtable, imt_slot))
		build_imt_slots (vtable->klass, vtable, imt, imt_slot);
	mono_loader_unlock ();
}

#define THUNK_THRESHOLD		10

typedef struct _GenericVirtualCase {
	MonoMethod *method;
	gpointer code;
	int count;
	struct _GenericVirtualCase *next;
} GenericVirtualCase;

/*
 * get_generic_virtual_entries:
 *
 *   Return IMT entries for the generic virtual method instances and
 *   variant interface methods for vtable slot
 * VTABLE_SLOT.
 */
static MonoImtBuilderEntry*
get_generic_virtual_entries (MonoMemoryManager *mem_manager, gpointer *vtable_slot)
{
	MONO_REQ_GC_NEUTRAL_MODE;

  	GenericVirtualCase *list;
 	MonoImtBuilderEntry *entries;

	mono_mem_manager_lock (mem_manager);
 	if (!mem_manager->generic_virtual_cases)
 		mem_manager->generic_virtual_cases = g_hash_table_new (mono_aligned_addr_hash, NULL);

	list = (GenericVirtualCase *)g_hash_table_lookup (mem_manager->generic_virtual_cases, vtable_slot);

 	entries = NULL;
 	for (; list; list = list->next) {
 		MonoImtBuilderEntry *entry;

 		if (list->count < THUNK_THRESHOLD)
 			continue;

 		entry = g_new0 (MonoImtBuilderEntry, 1);
 		entry->key = list->method;
 		entry->value.target_code = mono_get_addr_from_ftnptr (list->code);
 		entry->has_target_code = 1;
 		if (entries)
 			entry->children = entries->children + 1;
 		entry->next = entries;
 		entries = entry;
 	}

	mono_mem_manager_unlock (mem_manager);

 	/* FIXME: Leaking memory ? */
 	return entries;
}

/**
 * \param domain a domain
 * \param vtable_slot pointer to the vtable slot
 * \param method the inflated generic virtual method
 * \param code the method's code
 *
 * Registers a call via unmanaged code to a generic virtual method
 * instantiation or variant interface method.  If the number of calls reaches a threshold
 * (THUNK_THRESHOLD), the method is added to the vtable slot's generic
 * virtual method trampoline.
 */
void
mono_method_add_generic_virtual_invocation (MonoVTable *vtable,
											gpointer *vtable_slot,
											MonoMethod *method, gpointer code)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	static gboolean inited = FALSE;
	static int num_added = 0;
	static int num_freed = 0;

	GenericVirtualCase *gvc, *list;
	MonoImtBuilderEntry *entries;
	GPtrArray *sorted;
	MonoMemoryManager *mem_manager = m_class_get_mem_manager (vtable->klass);

	mono_loader_lock ();

	mono_mem_manager_lock (mem_manager);
	if (!mem_manager->generic_virtual_cases)
		mem_manager->generic_virtual_cases = g_hash_table_new (mono_aligned_addr_hash, NULL);

	if (!inited) {
		mono_counters_register ("Generic virtual cases", MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &num_added);
		mono_counters_register ("Freed IMT trampolines", MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &num_freed);
		inited = TRUE;
	}

	/* Check whether the case was already added */
	list = (GenericVirtualCase *)g_hash_table_lookup (mem_manager->generic_virtual_cases, vtable_slot);
	gvc = list;
	while (gvc) {
		if (gvc->method == method)
			break;
		gvc = gvc->next;
	}

	/* If not found, make a new one */
	if (!gvc) {
		gvc = (GenericVirtualCase *)m_class_alloc (vtable->klass, sizeof (GenericVirtualCase));
		gvc->method = method;
		gvc->code = code;
		gvc->count = 0;
		gvc->next = (GenericVirtualCase *)g_hash_table_lookup (mem_manager->generic_virtual_cases, vtable_slot);

		g_hash_table_insert (mem_manager->generic_virtual_cases, vtable_slot, gvc);

		num_added++;
	}

	mono_mem_manager_unlock (mem_manager);

	if (++gvc->count == THUNK_THRESHOLD) {
		gpointer *old_thunk = (void **)*vtable_slot;
		gpointer vtable_trampoline = NULL;
		gpointer imt_trampoline = NULL;

		if ((gpointer)vtable_slot < (gpointer)vtable) {
			size_t displacement = (gpointer*)vtable_slot - (gpointer*)vtable;
			size_t imt_slot = MONO_IMT_SIZE + displacement;

			/* Force the rebuild of the trampoline at the next call */
			imt_trampoline = mono_get_runtime_callbacks ()->get_imt_trampoline (vtable, (int)imt_slot);
			*vtable_slot = imt_trampoline;
		} else {
			vtable_trampoline = mono_get_runtime_callbacks ()->get_vtable_trampoline ? mono_get_runtime_callbacks ()->get_vtable_trampoline (vtable, (int)((gpointer*)vtable_slot - (gpointer*)vtable->vtable)) : NULL;

			entries = get_generic_virtual_entries (mem_manager, vtable_slot);

			sorted = imt_sort_slot_entries (entries);

			*vtable_slot = mono_get_runtime_callbacks ()->build_imt_trampoline (vtable, (MonoIMTCheckItem**)sorted->pdata, sorted->len,
																				vtable_trampoline);

			while (entries) {
				MonoImtBuilderEntry *next = entries->next;
				g_free (entries);
				entries = next;
			}

			for (guint i = 0; i < sorted->len; ++i)
				g_free (g_ptr_array_index (sorted, i));
			g_ptr_array_free (sorted, TRUE);

			if (old_thunk != vtable_trampoline && old_thunk != imt_trampoline)
				num_freed ++;
		}
	}

	mono_loader_unlock ();
}

