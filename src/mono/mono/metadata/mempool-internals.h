#ifndef _MONO_MEMPOOL_INTERNALS_H_
#define _MONO_MEMPOOL_INTERNALS_H_

#include <glib.h>

#include "mono/utils/mono-compiler.h"
#include "mono/metadata/mempool.h"

static inline GList*
g_list_prepend_mempool (MonoMemPool *mp, GList *list, gpointer data)
{
	GList *new_list;
	
	new_list = (GList *) mono_mempool_alloc (mp, sizeof (GList));
	new_list->data = data;
	new_list->prev = list ? list->prev : NULL;
    new_list->next = list;

    if (new_list->prev)
            new_list->prev->next = new_list;
    if (list)
            list->prev = new_list;

	return new_list;
}

static inline GSList*
g_slist_prepend_mempool (MonoMemPool *mp, GSList *list, gpointer  data)
{
	GSList *new_list;
	
	new_list = (GSList *) mono_mempool_alloc (mp, sizeof (GSList));
	new_list->data = data;
	new_list->next = list;

	return new_list;
}

static inline GSList*
g_slist_append_mempool (MonoMemPool *mp, GSList *list, gpointer data)
{
	GSList *new_list;
	GSList *last;

	new_list = (GSList *) mono_mempool_alloc (mp, sizeof (GSList));
	new_list->data = data;
	new_list->next = NULL;

	if (list) {
		last = list;
		while (last->next)
			last = last->next;
		last->next = new_list;

		return list;
	} else
		return new_list;
}

long
mono_mempool_get_bytes_allocated (void);

#endif
