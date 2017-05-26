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

gpointer
ves_icall_System_Threading_Events_CreateEvent_internal (MonoBoolean manual, MonoBoolean initial, MonoStringHandle name, gint32 *err, MonoError *error);

gboolean
ves_icall_System_Threading_Events_SetEvent_internal (gpointer handle);

gboolean
ves_icall_System_Threading_Events_ResetEvent_internal (gpointer handle);

void
ves_icall_System_Threading_Events_CloseEvent_internal (gpointer handle);

gpointer
ves_icall_System_Threading_Events_OpenEvent_internal (MonoStringHandle name, gint32 rights, gint32 *err, MonoError *error);

typedef struct MonoW32HandleNamedEvent MonoW32HandleNamedEvent;

MonoW32HandleNamespace*
mono_w32event_get_namespace (MonoW32HandleNamedEvent *event);

#endif /* _MONO_METADATA_W32EVENT_H_ */
