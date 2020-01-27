/**
 * \file
 */

#ifndef _MONO_METADATA_W32EVENT_H_
#define _MONO_METADATA_W32EVENT_H_

#include <config.h>
#include <glib.h>

#include "object.h"
#include "object-internals.h"
#include "w32handle-namespace.h"
#include <mono/metadata/icalls.h>

void
mono_w32event_init (void);

gpointer
mono_w32event_create (gboolean manual, gboolean initial);

gboolean
mono_w32event_close (gpointer handle);

void
mono_w32event_set (gpointer handle);

void
mono_w32event_reset (gpointer handle);

ICALL_EXPORT
gboolean
ves_icall_System_Threading_Events_SetEvent_internal (gpointer handle);

ICALL_EXPORT
gboolean
ves_icall_System_Threading_Events_ResetEvent_internal (gpointer handle);

ICALL_EXPORT
void
ves_icall_System_Threading_Events_CloseEvent_internal (gpointer handle);

typedef struct MonoW32HandleNamedEvent MonoW32HandleNamedEvent;

MonoW32HandleNamespace*
mono_w32event_get_namespace (MonoW32HandleNamedEvent *event);

#endif /* _MONO_METADATA_W32EVENT_H_ */
