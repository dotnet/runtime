#ifndef _MONO_CQ_H
#define _MONO_CQ_H

#include <config.h>
#include <glib.h>
#include <mono/metadata/object.h>
#include <mono/metadata/gc-internal.h>

G_BEGIN_DECLS

typedef struct _MonoCQ MonoCQ;

MonoCQ *mono_cq_create (void) MONO_INTERNAL;
void mono_cq_destroy (MonoCQ *cq) MONO_INTERNAL;
gint mono_cq_count (MonoCQ *cq) MONO_INTERNAL;
void mono_cq_enqueue (MonoCQ *cq, MonoObject *obj) MONO_INTERNAL;
gboolean mono_cq_dequeue (MonoCQ *cq, MonoObject **result) MONO_INTERNAL;

G_END_DECLS

#endif

