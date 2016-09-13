
#ifndef _MONO_METADATA_W32HANDLE_NAMESPACE_H_
#define _MONO_METADATA_W32HANDLE_NAMESPACE_H_

#include <config.h>
#include <glib.h>

#include "mono/utils/w32handle.h"

#define MONO_W32HANDLE_NAMESPACE_MAX_PATH 260

typedef struct {
	gchar name [MONO_W32HANDLE_NAMESPACE_MAX_PATH + 1];
} MonoW32HandleNamespace;

void
mono_w32handle_namespace_init (void);

void
mono_w32handle_namespace_lock (void);

void
mono_w32handle_namespace_unlock (void);

gpointer
mono_w32handle_namespace_search_handle (MonoW32HandleType type, gchar *name);

#endif /* _MONO_METADATA_W32HANDLE_NAMESPACE_H_ */