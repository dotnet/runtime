/*
 * mono-mlist.c: Managed object list implementation
 *
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * Copyright 2006-2009 Novell, Inc (http://www.novell.com)
 */

#include "mono/metadata/mono-mlist.h"
#include "mono/metadata/appdomain.h"
#include "mono/metadata/class-internals.h"

/* matches the System.MonoListItem object*/
struct _MonoMList {
	MonoObject object;
	MonoMList *next;
	MonoObject *data;
};

/* 
 * note: we only allocate in the root domain: this lists are
 * not exposed to managed code
 */
static MonoVTable *monolist_item_vtable = NULL;

/**
 * mono_mlist_alloc:
 * @data: object to use as data
 *
 * Allocates a new managed list node with @data as the contents.
 * A managed list node also represents a singly-linked list.
 * Managed lists are garbage collected, so there is no free routine
 * and the user is required to keep references to the managed list
 * to prevent it from being garbage collected.
 */
MonoMList*
mono_mlist_alloc (MonoObject *data)
{
	MonoMList* res;
	if (!monolist_item_vtable) {
		MonoClass *klass = mono_class_from_name (mono_defaults.corlib, "System", "MonoListItem");
		monolist_item_vtable = mono_class_vtable (mono_get_root_domain (), klass);
		g_assert (monolist_item_vtable);
	}
	res = (MonoMList*)mono_object_new_fast (monolist_item_vtable);
	MONO_OBJECT_SETREF (res, data, data);
	return res;
}

/**
 * mono_mlist_get_data:
 * @list: the managed list node
 *
 * Get the object stored in the list node @list.
 */
MonoObject*
mono_mlist_get_data (MonoMList* list)
{
	return list->data;
}

/**
 * mono_mlist_set_data:
 * @list: the managed list node
 *
 * Set the object content in the list node @list.
 */
void
mono_mlist_set_data (MonoMList* list, MonoObject *data)
{
	MONO_OBJECT_SETREF (list, data, data);
}

/**
 * mono_mlist_set_next:
 * @list: a managed list node
 * @next: list node that will be next for the @list node.
 *
 * Set next node for @list to @next.
 */
MonoMList *
mono_mlist_set_next (MonoMList* list, MonoMList *next)
{
	if (!list)
		return next;

	MONO_OBJECT_SETREF (list, next, next);
	return list;
}

/**
 * mono_mlist_length:
 * @list: the managed list
 *
 * Get the number of items in the list @list.
 * Since managed lists are singly-linked, this operation takes O(n) time.
 */
int
mono_mlist_length (MonoMList* list)
{
	int len = 0;
	while (list) {
		list = list->next;
		++len;
	}
	return len;
}

/**
 * mono_mlist_next:
 * @list: the managed list node
 *
 * Returns the next managed list node starting from @list.
 */
MonoMList*
mono_mlist_next (MonoMList* list)
{
	return list->next;
}

/**
 * mono_mlist_last:
 * @list: the managed list node
 *
 * Returns the last managed list node in list @list.
 * Since managed lists are singly-linked, this operation takes O(n) time.
 */
MonoMList*
mono_mlist_last (MonoMList* list)
{
	if (list) {
		while (list->next)
			list = list->next;
		return list;
	}
	return NULL;
}

/**
 * mono_mlist_prepend:
 * @list: the managed list
 * @data: the object to add to the list
 *
 * Allocate a new list node with @data as content and prepend it
 * to the list @list. @list can be NULL.
 */
MonoMList*
mono_mlist_prepend (MonoMList* list, MonoObject *data)
{
	MonoMList* res = mono_mlist_alloc (data);
	if (list)
		MONO_OBJECT_SETREF (res, next, list);
	return res;
}

/**
 * mono_mlist_append:
 * @list: the managed list
 * @data: the object to add to the list
 *
 * Allocate a new list node with @data as content and append it
 * to the list @list. @list can be NULL.
 * Since managed lists are singly-linked, this operation takes O(n) time.
 */
MonoMList*
mono_mlist_append (MonoMList* list, MonoObject *data)
{
	MonoMList* res = mono_mlist_alloc (data);
	if (list) {
		MonoMList* last = mono_mlist_last (list);
		MONO_OBJECT_SETREF (last, next, res);
		return list;
	} else {
		return res;
	}
}

static MonoMList*
find_prev (MonoMList* list, MonoMList *item)
{
	MonoMList* prev = NULL;
	while (list) {
		if (list == item)
			break;
		prev = list;
		list = list->next;
	}
	return prev;
}

/**
 * mono_mlist_remove_item:
 * @list: the managed list
 * @data: the object to remove from the list
 *
 * Remove the list node @item from the managed list @list.
 * Since managed lists are singly-linked, this operation can take O(n) time.
 */
MonoMList*
mono_mlist_remove_item (MonoMList* list, MonoMList *item)
{
	MonoMList* prev;
	if (list == item) {
		list = item->next;
		item->next = NULL;
		return list;
	}
	prev = find_prev (list, item);
	if (prev) {
		MONO_OBJECT_SETREF (prev, next, item->next);
		item->next = NULL;
		return list;
	} else {
		/* not found */
		return list;
	}
}

