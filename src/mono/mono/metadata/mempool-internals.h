#ifndef _MONO_MEMPOOL_INTERNALS_H_
#define _MONO_MEMPOOL_INTERNALS_H_

#include <glib.h>

#include "mono/utils/mono-compiler.h"

GSList*
g_slist_prepend_mempool (MonoMemPool *mp, GSList *list, gpointer data) MONO_INTERNAL;
GSList*
g_slist_append_mempool (MonoMemPool *mp, GSList *list, gpointer data) MONO_INTERNAL;

GList*
g_list_prepend_mempool (MonoMemPool *mp, GList *list, gpointer data) MONO_INTERNAL;

#endif
