/**
 * \file
 */

#ifndef __MONO_CALLSPEC_H__
#define __MONO_CALLSPEC_H__
#include <glib.h>
#include <mono/utils/mono-compiler.h>

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
	int exclude;
	void *data, *data2;
} MonoTraceOperation;

typedef struct {
	int len;
	gboolean enabled;
	MonoTraceOperation *ops;
} MonoCallSpec;

MONO_PROFILER_API gboolean mono_callspec_parse		(const char *options,
							 MonoCallSpec *spec,
							 char **errstr);
MONO_PROFILER_API void     mono_callspec_cleanup	(MonoCallSpec *spec);
MONO_PROFILER_API gboolean mono_callspec_eval_exception	(MonoClass *klass,
							 MonoCallSpec *spec);
MONO_PROFILER_API gboolean mono_callspec_eval		(MonoMethod *method,
							 const MonoCallSpec *spec);
void			   mono_callspec_set_assembly	(MonoAssembly *assembly);

#endif /* __MONO_CALLSPEC_H__ */
