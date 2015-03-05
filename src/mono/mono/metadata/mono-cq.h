#ifndef _MONO_CQ_H
#define _MONO_CQ_H

#include <config.h>
#include <glib.h>
#include <mono/metadata/object.h>
#include <mono/metadata/gc-internal.h>

G_BEGIN_DECLS

typedef struct _MonoCQ MonoCQ;

MonoCQ *mono_cq_create (void);
void mono_cq_destroy (MonoCQ *cq);
gint mono_cq_count (MonoCQ *cq);
void mono_cq_enqueue (MonoCQ *cq, MonoObject *obj);
gboolean mono_cq_dequeue (MonoCQ *cq, MonoObject **result);

G_END_DECLS

#endif

