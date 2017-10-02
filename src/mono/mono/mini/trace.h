/**
 * \file
 */

#ifndef __MONO_TRACE_H__
#define __MONO_TRACE_H__
#include <glib.h>
#include "mono/utils/mono-compiler.h"

G_BEGIN_DECLS

void
mono_trace_enter_method (MonoMethod *method, char *ebp);

void 
mono_trace_leave_method (MonoMethod *method, ...);

void mono_trace_enable (gboolean enable);
gboolean mono_trace_is_enabled (void);
gboolean mono_trace_eval_exception (MonoClass *klass);

G_END_DECLS

#endif /* __MONO_TRACE_H__ */
