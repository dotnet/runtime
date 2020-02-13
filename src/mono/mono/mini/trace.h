/**
 * \file
 */

#ifndef __MONO_TRACE_H__
#define __MONO_TRACE_H__
#include <glib.h>
#include "mono/utils/mono-compiler.h"
#include "mono/metadata/icalls.h"

ICALL_EXTERN_C
void
mono_trace_enter_method (MonoMethod *method, MonoJitInfo *ji, MonoProfilerCallContext *ctx);

ICALL_EXTERN_C
void 
mono_trace_leave_method (MonoMethod *method, MonoJitInfo *ji, MonoProfilerCallContext *ctx);

void mono_trace_enable (gboolean enable);
gboolean mono_trace_is_enabled (void);
gboolean mono_trace_eval_exception (MonoClass *klass);

#endif /* __MONO_TRACE_H__ */
