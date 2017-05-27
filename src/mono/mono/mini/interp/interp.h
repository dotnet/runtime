/**
 * \file
 */

#ifndef __MONO_MINI_INTERPRETER_H__
#define __MONO_MINI_INTERPRETER_H__
#include <mono/mini/mini.h>

#define INTERP_ICALL_TRAMP_IARGS 12
#define INTERP_ICALL_TRAMP_FARGS 4

struct _InterpMethodArguments {
	size_t ilen;
	gpointer *iargs;
	size_t flen;
	double *fargs;
	gpointer *retval;
	size_t is_float_ret;
};

typedef struct _InterpMethodArguments InterpMethodArguments;


typedef struct _MonoInterpStackIter MonoInterpStackIter;

/* Needed for stack allocation */
struct _MonoInterpStackIter {
	gpointer dummy [8];
};

typedef gpointer MonoInterpFrameHandle;

int
mono_interp_regression_list (int verbose, int count, char *images []);

void
mono_interp_init (void);

gpointer
mono_interp_create_method_pointer (MonoMethod *method, MonoError *error);

MonoObject*
mono_interp_runtime_invoke (MonoMethod *method, void *obj, void **params, MonoObject **exc, MonoError *error);

void
mono_interp_init_delegate (MonoDelegate *del);

gpointer
mono_interp_create_trampoline (MonoDomain *domain, MonoMethod *method, MonoError *error);

void
mono_interp_parse_options (const char *options);

void
interp_walk_stack_with_ctx (MonoInternalStackWalk func, MonoContext *ctx, MonoUnwindOptions options, void *user_data);

void
mono_interp_set_resume_state (MonoJitTlsData *jit_tls, MonoException *ex, MonoInterpFrameHandle interp_frame, gpointer handler_ip);

void
mono_interp_run_finally (StackFrameInfo *frame, int clause_index, gpointer handler_ip);

void
mono_interp_frame_iter_init (MonoInterpStackIter *iter, gpointer interp_exit_data);

gboolean
mono_interp_frame_iter_next (MonoInterpStackIter *iter, StackFrameInfo *frame);

MonoJitInfo*
mono_interp_find_jit_info (MonoDomain *domain, MonoMethod *method);

void
mono_interp_set_breakpoint (MonoJitInfo *jinfo, gpointer ip);

void
mono_interp_clear_breakpoint (MonoJitInfo *jinfo, gpointer ip);

MonoJitInfo*
mono_interp_frame_get_jit_info (MonoInterpFrameHandle frame);

gpointer
mono_interp_frame_get_ip (MonoInterpFrameHandle frame);

gpointer
mono_interp_frame_get_arg (MonoInterpFrameHandle frame, int pos);

gpointer
mono_interp_frame_get_local (MonoInterpFrameHandle frame, int pos);

gpointer
mono_interp_frame_get_this (MonoInterpFrameHandle frame);

void
mono_interp_start_single_stepping (void);

void
mono_interp_stop_single_stepping (void);

#endif /* __MONO_MINI_INTERPRETER_H__ */
