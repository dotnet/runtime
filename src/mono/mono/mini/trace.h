#ifndef __MONO_TRACE_H__
#define __MONO_TRACE_H__

typedef enum {
	MONO_TRACEOP_ALL,
	MONO_TRACEOP_PROGRAM,
	MONO_TRACEOP_METHOD,
	MONO_TRACEOP_ASSEMBLY,
	MONO_TRACEOP_CLASS
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

#endif /* __MONO_TRACE_H__ */
