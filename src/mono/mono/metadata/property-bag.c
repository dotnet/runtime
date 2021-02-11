/**
 * \file
 * Linearizable property bag.
 *
 * Authors:
 *   Rodrigo Kumpera (kumpera@gmail.com)
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <mono/metadata/property-bag.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-membar.h>

/*
 * mono_property_bag_get:
 *
 *   Return the value of the property with TAG or NULL.
 * This doesn't take any locks.
 */
void*
mono_property_bag_get (MonoPropertyBag *bag, int tag)
{
	MonoPropertyBagItem *item;
	
	for (item = bag->head; item && item->tag <= tag; item = item->next) {
		if (item->tag == tag)
			return item;
	}
	return NULL;
}

/*
 * mono_property_bag_add:
 *
 *   Store VALUE in the property bag. Return the previous value
 * with the same tag, or NULL. VALUE should point to a structure
 * extending the MonoPropertyBagItem structure with the 'tag'
 * field set.
 * This doesn't take any locks.
 */
void*
mono_property_bag_add (MonoPropertyBag *bag, void *value)
{
	MonoPropertyBagItem *cur, **prev, *item = (MonoPropertyBagItem*)value;
	int tag = item->tag;
	mono_memory_barrier (); //publish the values in value

retry:
	prev = &bag->head;
	while (1) {
		cur = *prev;
		if (!cur || cur->tag > tag) {
			item->next = cur;
			if (mono_atomic_cas_ptr ((void**)prev, item, cur) == cur)
				return item;
			goto retry;
		} else if (cur->tag == tag) {
			return cur;
		} else {
			prev = &cur->next;
		}
	}
	return value;
}
