#ifndef __MONO_TRACE_H__
#define __MONO_TRACE_H__

typedef enum {
	MONO_TRACEOP_ALL,
	MONO_TRACEOP_PROGRAM,
	MONO_TRACEOP_METHOD,
	MONO_TRACEOP_ASSEMBLY,
	MONO_TRACEOP_CLASS,
	MONO_TRACEOP_NAMESPACE
} MonoTraceOpcode;

typedef struct {
	MonoTraceOpcode op;
	int   exclude;
	void *data, *data2;
} MonoTraceOperation;

struct MonoTraceSpec {
	int len;
	MonoTraceOperation *ops;

	MonoAssembly *assembly;
};

void
mono_trace_enter_method (MonoMethod *method, char *ebp);

void 
mono_trace_leave_method (MonoMethod *method, ...);

#endif /* __MONO_TRACE_H__ */
