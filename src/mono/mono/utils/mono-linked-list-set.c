/*
 * mono-split-ordered-list.c: A lock-free split ordered list.
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2011 Novell, Inc
 *
 * This is an implementation of Maged Michael's lock-free linked-list set.
 * For more details see:
 *	"High Performance Dynamic Lock-Free Hash Tables and List-Based Sets"
 *  Maged M. Michael 2002
 *
 *  http://www.research.ibm.com/people/m/michael/spaa-2002.pdf
 */

#include <mono/utils/mono-linked-list-set.h>

/*atomics.*/
#include <mono/io-layer/io-layer.h>

static inline gpointer
mask (gpointer n, uintptr_t bit)
{
	return (gpointer)(((uintptr_t)n) | bit);
}

gpointer
get_hazardous_pointer_with_mask (gpointer volatile *pp, MonoThreadHazardPointers *hp, int hazard_index)
{
	gpointer p;

	for (;;) {
		/* Get the pointer */
		p = *pp;
		/* If we don't have hazard pointers just return the
		   pointer. */
		if (!hp)
			return p;
		/* Make it hazardous */
		mono_hazard_pointer_set (hp, hazard_index, mono_lls_pointer_unmask (p));

		mono_memory_barrier ();

		/* Check that it's still the same.  If not, try
		   again. */
		if (*pp != p) {
			mono_hazard_pointer_clear (hp, hazard_index);
			continue;
		}
		break;
	}

	return p;
}

/*
Initialize @list and will use @free_node_func to release memory.
If @free_node_func is null the caller is responsible for releasing node memory.
@free_node_func must be lock-free.  That implies that it cannot use malloc/free.
*/
void
mono_lls_init (MonoLinkedListSet *list, void (*free_node_func)(void *))
{
	list->head = NULL;
	list->free_node_func = free_node_func;
}

/*
Search @list for element with key @key.
The nodes next, cur and prev are returned in @hp.
Returns true if a node with key @key was found.
This function cannot be called from a signal nor within interrupt context*.
XXX A variant that works within interrupted is possible if needed.

* interrupt context is when the current thread is reposible for another thread
been suspended at an arbritary point. This is a limitation of our SMR implementation.
*/
gboolean
mono_lls_find (MonoLinkedListSet *list, MonoThreadHazardPointers *hp, uintptr_t key)
{
	MonoLinkedListSetNode *cur, *next;
	MonoLinkedListSetNode **prev;
	uintptr_t cur_key;

try_again:
	prev = &list->head;

	/*
	 * prev is not really a hazardous pointer, but we return prev
	 * in hazard pointer 2, so we set it here.  Note also that
	 * prev is not a pointer to a node.  We use here the fact that
	 * the first element in a node is the next pointer, so it
	 * works, but it's not pretty.
	 */
	mono_hazard_pointer_set (hp, 2, prev);

	cur = get_hazardous_pointer_with_mask ((gpointer*)prev, hp, 1);

	while (1) {
		if (cur == NULL)
			return FALSE;
		next = get_hazardous_pointer_with_mask ((gpointer*)&cur->next, hp, 0);
		cur_key = cur->key;

		/*
		 * We need to make sure that we dereference prev below
		 * after reading cur->next above, so we need a read
		 * barrier.
		 */
		mono_memory_read_barrier ();

		if (*prev != cur)
			goto try_again;

		if (!mono_lls_pointer_get_mark (next)) {
			if (cur_key >= key)
				return cur_key == key;

			prev = &cur->next;
			mono_hazard_pointer_set (hp, 2, cur);
		} else {
			next = mono_lls_pointer_unmask (next);
			if (InterlockedCompareExchangePointer ((volatile gpointer*)prev, next, cur) == cur) {
				/* The hazard pointer must be cleared after the CAS. */
				mono_memory_write_barrier ();
				mono_hazard_pointer_clear (hp, 1);
				if (list->free_node_func)
					mono_thread_hazardous_free_or_queue (cur, list->free_node_func, FALSE, TRUE);
			} else
				goto try_again;
		}
		cur = mono_lls_pointer_unmask (next);
		mono_hazard_pointer_set (hp, 1, cur);
	}
}

/*
Insert @value into @list.
The nodes value, cur and prev are returned in @hp.
Return true if @value was inserted by this call. If it returns FALSE, it's the caller
resposibility to release memory.
This function cannot be called from a signal nor with the world stopped.
*/
gboolean
mono_lls_insert (MonoLinkedListSet *list, MonoThreadHazardPointers *hp, MonoLinkedListSetNode *value)
{
	MonoLinkedListSetNode *cur, **prev;
	/*We must do a store barrier before inserting 
	to make sure all values in @node are globally visible.*/
	mono_memory_barrier ();

	while (1) {
		if (mono_lls_find (list, hp, value->key))
			return FALSE;
		cur = mono_hazard_pointer_get_val (hp, 1);
		prev = mono_hazard_pointer_get_val (hp, 2);

		value->next = cur;
		mono_hazard_pointer_set (hp, 0, value);
		/* The CAS must happen after setting the hazard pointer. */
		mono_memory_write_barrier ();
		if (InterlockedCompareExchangePointer ((volatile gpointer*)prev, value, cur) == cur)
			return TRUE;
	}
}

/*
Search @list for element with key @key.
The nodes next, cur and prev are returned in @hp
Returns true if @value was removed by this call.
This function cannot be called from a signal nor with the world stopped.
*/
gboolean
mono_lls_remove (MonoLinkedListSet *list, MonoThreadHazardPointers *hp, MonoLinkedListSetNode *value)
{
	MonoLinkedListSetNode *cur, **prev, *next;
	while (1) {
		if (!mono_lls_find (list, hp, value->key))
			return FALSE;

		next = mono_hazard_pointer_get_val (hp, 0);
		cur = mono_hazard_pointer_get_val (hp, 1);
		prev = mono_hazard_pointer_get_val (hp, 2);

		g_assert (cur == value);

		if (InterlockedCompareExchangePointer ((volatile gpointer*)&cur->next, mask (next, 1), next) != next)
			continue;
		/* The second CAS must happen before the first. */
		mono_memory_write_barrier ();
		if (InterlockedCompareExchangePointer ((volatile gpointer*)prev, next, cur) == cur) {
			/* The CAS must happen before the hazard pointer clear. */
			mono_memory_write_barrier ();
			mono_hazard_pointer_clear (hp, 1);
			if (list->free_node_func)
				mono_thread_hazardous_free_or_queue (value, list->free_node_func, FALSE, TRUE);
		} else
			mono_lls_find (list, hp, value->key);
		return TRUE;
	}
}
