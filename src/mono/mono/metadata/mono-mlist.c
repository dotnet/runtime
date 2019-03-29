/**
 * \file
 * Managed object list implementation
 *
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * Copyright 2006-2009 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include "mono/metadata/mono-mlist.h"
#include "mono/metadata/appdomain.h"
#include "mono/metadata/class-internals.h"
#include "mono/metadata/object-internals.h"


static
MonoMList*  mono_mlist_alloc_checked       (MonoObject *data, MonoError *error);


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
 * \param data object to use as data
 * Allocates a new managed list node with \p data as the contents.
 * A managed list node also represents a singly-linked list.
 * Managed lists are garbage collected, so there is no free routine
 * and the user is required to keep references to the managed list
 * to prevent it from being garbage collected.
 */
MonoMList*
mono_mlist_alloc (MonoObject *data)
{
	ERROR_DECL (error);
	MonoMList *result = mono_mlist_alloc_checked (data, error);
	mono_error_cleanup (error);
	return result;
}

/**
 * mono_mlist_alloc_checked:
 * \param data object to use as data
 * \param error set on error
 * Allocates a new managed list node with \p data as the contents.  A
 * managed list node also represents a singly-linked list.  Managed
 * lists are garbage collected, so there is no free routine and the
 * user is required to keep references to the managed list to prevent
 * it from being garbage collected. On failure returns NULL and sets
 * \p error.
 */
MonoMList*
mono_mlist_alloc_checked (MonoObject *data, MonoError *error)
{
	error_init (error);
	MonoMList* res;
	if (!monolist_item_vtable) {
#ifdef ENABLE_NETCORE
		MonoClass *klass = mono_class_load_from_name (mono_defaults.corlib, "Mono", "MonoListItem");
#else
		MonoClass *klass = mono_class_load_from_name (mono_defaults.corlib, "System", "MonoListItem");
#endif
		monolist_item_vtable = mono_class_vtable_checked (mono_get_root_domain (), klass, error);
		mono_error_assert_ok  (error);
	}
	res = (MonoMList*)mono_object_new_specific_checked (monolist_item_vtable, error);
	return_val_if_nok (error, NULL);
	MONO_OBJECT_SETREF_INTERNAL (res, data, data);
	return res;
}

/**
 * mono_mlist_get_data:
 * \param list the managed list node
 * Get the object stored in the list node \p list.
 */
MonoObject*
mono_mlist_get_data (MonoMList* list)
{
	return list->data;
}

/**
 * mono_mlist_set_data:
 * \param list the managed list node
 * Set the object content in the list node \p list.
 */
void
mono_mlist_set_data (MonoMList* list, MonoObject *data)
{
	MONO_OBJECT_SETREF_INTERNAL (list, data, data);
}

/**
 * mono_mlist_set_next:
 * \param list a managed list node
 * \param next list node that will be next for the \p list node.
 * Set next node for \p list to \p next.
 */
MonoMList *
mono_mlist_set_next (MonoMList* list, MonoMList *next)
{
	if (!list)
		return next;

	MONO_OBJECT_SETREF_INTERNAL (list, next, next);
	return list;
}

/**
 * mono_mlist_length:
 * \param list the managed list
 * Get the number of items in the list \p list.
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
 * \param list the managed list node
 * Returns the next managed list node starting from \p list.
 */
MonoMList*
mono_mlist_next (MonoMList* list)
{
	return list->next;
}

/**
 * mono_mlist_last:
 * \param list the managed list node
 * Returns the last managed list node in list \p list.
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
 * \param list the managed list
 * \param data the object to add to the list
 * Allocate a new list node with \p data as content and prepend it
 * to the list \p list. \p list can be NULL.
 */
MonoMList*
mono_mlist_prepend (MonoMList* list, MonoObject *data)
{
	ERROR_DECL (error);
	MonoMList *result = mono_mlist_prepend_checked (list, data, error);
	mono_error_cleanup (error);
	return result;
}

/**
 * mono_mlist_prepend_checked:
 * \param list the managed list
 * \param data the object to add to the list
 * \param error set on error
 * Allocate a new list node with \p data as content and prepend it to
 * the list \p list. \p list can be NULL. On failure returns NULL and sets
 * \p error.
 */
MonoMList*
mono_mlist_prepend_checked (MonoMList* list, MonoObject *data, MonoError *error)
{
	error_init (error);
	MonoMList* res = mono_mlist_alloc_checked (data, error);
	return_val_if_nok (error, NULL);

	if (list)
		MONO_OBJECT_SETREF_INTERNAL (res, next, list);
	return res;
}

/**
 * mono_mlist_append:
 * \param list the managed list
 * \param data the object to add to the list
 * Allocate a new list node with \p data as content and append it
 * to the list \p list. \p list can be NULL.
 * Since managed lists are singly-linked, this operation takes O(n) time.
 */
MonoMList*
mono_mlist_append (MonoMList* list, MonoObject *data)
{
	ERROR_DECL (error);
	MonoMList *result = mono_mlist_append_checked (list, data, error);
	mono_error_cleanup (error);
	return result;
}

/**
 * mono_mlist_append_checked:
 * \param list the managed list
 * \param data the object to add to the list
 * \param error set on error
 * Allocate a new list node with \p data as content and append it
 * to the list \p list. \p list can be NULL.
 * Since managed lists are singly-linked, this operation takes O(n) time.
 * On failure returns NULL and sets \p error.
 */
MonoMList*
mono_mlist_append_checked (MonoMList* list, MonoObject *data, MonoError *error)
{
	error_init (error);
	MonoMList* res = mono_mlist_alloc_checked (data, error);
	return_val_if_nok (error, NULL);

	if (list) {
		MonoMList* last = mono_mlist_last (list);
		MONO_OBJECT_SETREF_INTERNAL (last, next, res);
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
 * \param list the managed list
 * \param data the object to remove from the list
 * Remove the list node \p item from the managed list \p list.
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
		MONO_OBJECT_SETREF_INTERNAL (prev, next, item->next);
		item->next = NULL;
		return list;
	} else {
		/* not found */
		return list;
	}
}

