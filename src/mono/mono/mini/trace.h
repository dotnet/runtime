#ifndef __MONO_TRACE_H__
#define __MONO_TRACE_H__
#include <glib.h>
#include "mono/utils/mono-compiler.h"

typedef enum {
	MONO_TRACEOP_ALL,
	MONO_TRACEOP_PROGRAM,
	MONO_TRACEOP_METHOD,
	MONO_TRACEOP_ASSEMBLY,
	MONO_TRACEOP_CLASS,
	MONO_TRACEOP_NAMESPACE,
	MONO_TRACEOP_EXCEPTION,
	MONO_TRACEOP_WRAPPER,
} MonoTraceOpcode;

typedef struct {
	MonoTraceOpcode op;
	int   exclude;
	void *data, *data2;
} MonoTraceOperation;

struct MonoTraceSpec {
	int len;
	gboolean enabled;
	MonoTraceOperation *ops;

	MonoAssembly *assembly;
};

G_BEGIN_DECLS

void
mono_trace_enter_method (MonoMethod *method, char *ebp) MONO_INTERNAL;

void 
mono_trace_leave_method (MonoMethod *method, ...) MONO_INTERNAL;

void mono_trace_enable (gboolean enable) MONO_INTERNAL;
gboolean mono_trace_is_enabled (void) MONO_INTERNAL;
gboolean mono_trace_eval_exception (MonoClass *klass) MONO_INTERNAL;

G_END_DECLS

#endif /* __MONO_TRACE_H__ */
