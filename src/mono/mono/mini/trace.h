/**
 * \file
 */

#ifndef __MONO_TRACE_H__
#define __MONO_TRACE_H__
#include <glib.h>
#include "mono/utils/mono-compiler.h"

void
mono_trace_enter_method (MonoMethod *method, MonoProfilerCallContext *ctx);

void 
mono_trace_leave_method (MonoMethod *method, MonoProfilerCallContext *ctx);

void mono_trace_enable (gboolean enable);
gboolean mono_trace_is_enabled (void);
gboolean mono_trace_eval_exception (MonoClass *klass);

#endif /* __MONO_TRACE_H__ */
