#ifndef _MONO_WSQ_H
#define _MONO_WSQ_H

#include <config.h>
#include <glib.h>
#include <mono/metadata/object.h>
#include <mono/metadata/gc-internal.h>
#include <mono/io-layer/io-layer.h>

G_BEGIN_DECLS

typedef struct _MonoWSQ MonoWSQ;

void mono_wsq_init (void) MONO_INTERNAL;
void mono_wsq_cleanup (void) MONO_INTERNAL;

MonoWSQ *mono_wsq_create (void) MONO_INTERNAL;
void mono_wsq_destroy (MonoWSQ *wsq) MONO_INTERNAL;
gboolean mono_wsq_local_push (void *obj) MONO_INTERNAL;
gboolean mono_wsq_local_pop (void **ptr) MONO_INTERNAL;
void mono_wsq_try_steal (MonoWSQ *wsq, void **ptr, guint32 ms_timeout) MONO_INTERNAL;
gint mono_wsq_count (MonoWSQ *wsq) MONO_INTERNAL;

G_END_DECLS

#endif
