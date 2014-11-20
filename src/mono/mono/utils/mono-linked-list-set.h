/*
 * mono-linked-list-set.h: A lock-free split ordered list.
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

void
mono_lls_init (MonoLinkedListSet *list, void (*free_node_func)(void *));

gboolean
mono_lls_find (MonoLinkedListSet *list, MonoThreadHazardPointers *hp, uintptr_t key) MONO_INTERNAL;

gboolean
mono_lls_insert (MonoLinkedListSet *list, MonoThreadHazardPointers *hp, MonoLinkedListSetNode *value) MONO_INTERNAL;

gboolean
mono_lls_remove (MonoLinkedListSet *list, MonoThreadHazardPointers *hp, MonoLinkedListSetNode *value) MONO_INTERNAL;

gpointer
get_hazardous_pointer_with_mask (gpointer volatile *pp, MonoThreadHazardPointers *hp, int hazard_index) MONO_INTERNAL;

/*
Requires the world to be stoped
*/
#define MONO_LLS_FOREACH(list, element, type) {\
	MonoLinkedListSetNode *__cur;	\
	for (__cur = (list)->head; __cur; __cur = mono_lls_pointer_unmask (__cur->next)) \
		if (!mono_lls_pointer_get_mark (__cur->next)) {	\
			(element) = (type)__cur;


#define MONO_LLS_FOREACH_FILTERED(list, element, filter_func, type) {\
	MonoLinkedListSetNode *__cur;	\
	for (__cur = (list)->head; __cur; __cur = mono_lls_pointer_unmask (__cur->next)) \
		if (!mono_lls_pointer_get_mark (__cur->next)) {	\
			(element) = (type)__cur;			\
			if (!filter_func (element)) continue;

#define MONO_LLS_END_FOREACH }}

static inline MonoLinkedListSetNode*
mono_lls_info_step (MonoLinkedListSetNode *val, MonoThreadHazardPointers *hp)
{
	val = mono_lls_pointer_unmask (val);
	mono_hazard_pointer_set (hp, 1, val);
	return val;
}

/*
Provides snapshot iteration
*/
#define MONO_LLS_FOREACH_SAFE(list, element, type) {\
	MonoThreadHazardPointers *__hp = mono_hazard_pointer_get ();	\
	MonoLinkedListSetNode *__cur, *__next;	\
	for (__cur = mono_lls_pointer_unmask (get_hazardous_pointer ((gpointer volatile*)&(list)->head, __hp, 1)); \
		__cur;	\
		__cur = mono_lls_info_step (__next, __hp)) {	\
		__next = get_hazardous_pointer_with_mask ((gpointer volatile*)&__cur->next, __hp, 0);	\
		if (!mono_lls_pointer_get_mark (__next)) {	\
			(element) = (type)__cur;

#define MONO_LLS_FOREACH_FILTERED_SAFE(list, element, filter_func, type) {\
	MonoThreadHazardPointers *__hp = mono_hazard_pointer_get ();	\
	MonoLinkedListSetNode *__cur, *__next;	\
	for (__cur = mono_lls_pointer_unmask (get_hazardous_pointer ((gpointer volatile*)&(list)->head, __hp, 1)); \
		__cur;	\
		__cur = mono_lls_info_step (__next, __hp)) {	\
		__next = get_hazardous_pointer_with_mask ((gpointer volatile*)&__cur->next, __hp, 0);	\
		if (!mono_lls_pointer_get_mark (__next)) {	\
			(element) = (type)__cur;	\
			if (!filter_func (element)) continue;


#define MONO_LLS_END_FOREACH_SAFE \
		} \
	}	\
	mono_hazard_pointer_clear (__hp, 0); \
	mono_hazard_pointer_clear (__hp, 1); \
}

#endif /* __MONO_SPLIT_ORDERED_LIST_H__ */
