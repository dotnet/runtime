#ifndef _MONO_WSQ_H
#define _MONO_WSQ_H

#include <config.h>
#include <glib.h>
#include <mono/metadata/object.h>
#include <mono/metadata/gc-internal.h>
#include <mono/io-layer/io-layer.h>

G_BEGIN_DECLS

typedef struct _MonoWSQ MonoWSQ;

void mono_wsq_init (void);
void mono_wsq_cleanup (void);

MonoWSQ *mono_wsq_create (void);
void mono_wsq_destroy (MonoWSQ *wsq);
gboolean mono_wsq_local_push (void *obj);
gboolean mono_wsq_local_pop (void **ptr);
void mono_wsq_try_steal (MonoWSQ *wsq, void **ptr, guint32 ms_timeout);
gint mono_wsq_count (MonoWSQ *wsq);
gboolean mono_wsq_suspend (MonoWSQ *wsq);

G_END_DECLS

#endif
