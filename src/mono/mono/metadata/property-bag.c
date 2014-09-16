
/*
 * property-bag.c: Linearizable property bag.
 *
 * Authors:
 *   Rodrigo Kumpera (kumpera@gmail.com)
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <mono/metadata/property-bag.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-membar.h>

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

void*
mono_property_bag_add (MonoPropertyBag *bag, void *value)
{
	MonoPropertyBagItem *cur, **prev, *item = value;
	int tag = item->tag;
	mono_memory_barrier (); //publish the values in value

retry:
	prev = &bag->head;
	while (1) {
		cur = *prev;
		if (!cur || cur->tag > tag) {
			item->next = cur;
			if (InterlockedCompareExchangePointer ((void*)prev, item, cur) == cur)
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
