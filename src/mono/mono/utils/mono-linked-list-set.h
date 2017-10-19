/**
 * \file
 * A lock-free split ordered list.
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2011 Novell, Inc
 */

#ifndef __MONO_SPLIT_ORDERED_LIST_H__
#define __MONO_SPLIT_ORDERED_LIST_H__

#include <mono/utils/hazard-pointer.h>
#include <mono/utils/mono-membar.h>

typedef struct _MonoLinkedListSetNode MonoLinkedListSetNode;

struct _MonoLinkedListSetNode {
	/* next must be the first element in this struct! */
	MonoLinkedListSetNode *next;
	uintptr_t key;
};

typedef struct {
	MonoLinkedListSetNode *head;
	void (*free_node_func)(void *);
} MonoLinkedListSet;


static inline gpointer
mono_lls_pointer_unmask (gpointer p)
{
	return (gpointer)((uintptr_t)p & ~(uintptr_t)0x3);
}

static inline uintptr_t
mono_lls_pointer_get_mark (gpointer n)
{
	return (uintptr_t)n & 0x1;
}

/*
Those are low level operations. prev, cur, next are returned in the hazard pointer table.
You must manually clean the hazard pointer table after using them.
*/

MONO_API void
mono_lls_init (MonoLinkedListSet *list, void (*free_node_func)(void *));

MONO_API gboolean
mono_lls_find (MonoLinkedListSet *list, MonoThreadHazardPointers *hp, uintptr_t key);

MONO_API gboolean
mono_lls_insert (MonoLinkedListSet *list, MonoThreadHazardPointers *hp, MonoLinkedListSetNode *value);

MONO_API gboolean
mono_lls_remove (MonoLinkedListSet *list, MonoThreadHazardPointers *hp, MonoLinkedListSetNode *value);

MONO_API gpointer
mono_lls_get_hazardous_pointer_with_mask (gpointer volatile *pp, MonoThreadHazardPointers *hp, int hazard_index);

static inline gboolean
mono_lls_filter_accept_all (gpointer elem)
{
	return TRUE;
}

/*
 * These macros assume that no other threads are actively modifying the list.
 */

#define MONO_LLS_FOREACH_FILTERED(list, type, elem, filter) \
	do { \
		MonoLinkedListSet *list__ = (list); \
		for (MonoLinkedListSetNode *cur__ = list__->head; cur__; cur__ = (MonoLinkedListSetNode *) mono_lls_pointer_unmask (cur__->next)) { \
			if (!mono_lls_pointer_get_mark (cur__->next)) { \
				type *elem = (type *) cur__; \
				if (filter (elem)) {

#define MONO_LLS_FOREACH_END \
				} \
			} \
		} \
	} while (0);

#define MONO_LLS_FOREACH(list, type, elem) \
	MONO_LLS_FOREACH_FILTERED ((list), type, elem, mono_lls_filter_accept_all)

/*
 * These macros can be used while other threads are potentially modifying the
 * list, but they only provide a snapshot of the list as a result.
 *
 * NOTE: Do NOT break out of the loop through any other means than a break
 * statement, as other ways of breaking the loop will skip past important
 * cleanup work.
 */

#define MONO_LLS_FOREACH_FILTERED_SAFE(list, type, elem, filter) \
	do { \
		/* NOTE: Keep this macro's code in sync with the mono_lls_find () logic. */ \
		MonoLinkedListSet *list__ = (list); \
		MonoThreadHazardPointers *hp__ = mono_hazard_pointer_get (); \
		gboolean progress__ = FALSE; \
		uintptr_t hkey__; \
		gboolean restart__; \
		do { \
			restart__ = FALSE; \
			MonoLinkedListSetNode **prev__ = &list__->head; \
			mono_hazard_pointer_set (hp__, 2, prev__); \
			MonoLinkedListSetNode *cur__ = (MonoLinkedListSetNode *) mono_lls_get_hazardous_pointer_with_mask ((gpointer *) prev__, hp__, 1); \
			while (1) { \
				if (!cur__) { \
					break; \
				} \
				MonoLinkedListSetNode *next__ = (MonoLinkedListSetNode *) mono_lls_get_hazardous_pointer_with_mask ((gpointer *) &cur__->next, hp__, 0); \
				uintptr_t ckey__ = cur__->key; \
				mono_memory_read_barrier (); \
				if (*prev__ != cur__) { \
					restart__ = TRUE; \
					break; \
				} \
				if (!mono_lls_pointer_get_mark (next__)) { \
					if (!progress__ || ckey__ > hkey__) { \
						progress__ = TRUE; \
						hkey__ = ckey__; \
						type *elem = (type *) cur__; \
						if (filter (elem)) { \
							gboolean broke__ = TRUE; \
							gboolean done__ = FALSE; \
							do { \
								if (done__) { \
									broke__ = FALSE; \
									break; \
								} \
								done__ = TRUE;

#define MONO_LLS_FOREACH_SAFE_END \
								broke__ = FALSE; \
								break; \
							} while (1); \
							if (broke__) { \
								break; \
							} \
						} \
					} \
					prev__ = &cur__->next; \
					mono_hazard_pointer_set (hp__, 2, cur__); \
				} else { \
					next__ = (MonoLinkedListSetNode *) mono_lls_pointer_unmask (next__); \
					if (mono_atomic_cas_ptr ((volatile gpointer *) prev__, next__, cur__) == cur__) { \
						mono_memory_write_barrier (); \
						mono_hazard_pointer_clear (hp__, 1); \
						if (list__->free_node_func) { \
							mono_thread_hazardous_queue_free (cur__, list__->free_node_func); \
						} \
					} else { \
						restart__ = TRUE; \
						break; \
					} \
				} \
				cur__ = (MonoLinkedListSetNode *) mono_lls_pointer_unmask (next__); \
				mono_hazard_pointer_set (hp__, 1, cur__); \
			} \
		} while (restart__); \
		mono_hazard_pointer_clear (hp__, 0); \
		mono_hazard_pointer_clear (hp__, 1); \
		mono_hazard_pointer_clear (hp__, 2); \
	} while (0);

#define MONO_LLS_FOREACH_SAFE(list, type, elem) \
	MONO_LLS_FOREACH_FILTERED_SAFE ((list), type, elem, mono_lls_filter_accept_all)

#endif /* __MONO_SPLIT_ORDERED_LIST_H__ */
