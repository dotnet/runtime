/**
 * \file
 */

#ifndef __MONO_MINI_INTERPRETER_H__
#define __MONO_MINI_INTERPRETER_H__
#include <mono/mini/mini-runtime.h>

#ifdef TARGET_WASM
#define INTERP_ICALL_TRAMP_IARGS 12
#define INTERP_ICALL_TRAMP_FARGS 12
#else
#define INTERP_ICALL_TRAMP_IARGS 12
#define INTERP_ICALL_TRAMP_FARGS 4
#endif

struct _InterpMethodArguments {
	size_t ilen;
	gpointer *iargs;
	size_t flen;
	double *fargs;
	gpointer *retval;
	size_t is_float_ret;
#ifdef TARGET_WASM
	MonoMethodSignature *sig;
#endif
};

typedef struct _InterpMethodArguments InterpMethodArguments;

typedef struct _MonoInterpStackIter MonoInterpStackIter;

/* Needed for stack allocation */
struct _MonoInterpStackIter {
	gpointer dummy [8];
};

typedef gpointer MonoInterpFrameHandle;


struct _MonoInterpCallbacks {
	gpointer (*create_method_pointer) (MonoMethod *method, MonoError *error);
	MonoObject* (*runtime_invoke) (MonoMethod *method, void *obj, void **params, MonoObject **exc, MonoError *error);
	void (*init_delegate) (MonoDelegate *del);
#ifndef DISABLE_REMOTING
	gpointer (*get_remoting_invoke) (gpointer imethod, MonoError *error);
#endif
	gpointer (*create_trampoline) (MonoDomain *domain, MonoMethod *method, MonoError *error);
	void (*walk_stack_with_ctx) (MonoInternalStackWalk func, MonoContext *ctx, MonoUnwindOptions options, void *user_data);
	void (*set_resume_state) (MonoJitTlsData *jit_tls, MonoException *ex, MonoJitExceptionInfo *ei, MonoInterpFrameHandle interp_frame, gpointer handler_ip);
	gboolean (*run_finally) (StackFrameInfo *frame, int clause_index, gpointer handler_ip);
	gboolean (*run_filter) (StackFrameInfo *frame, MonoException *ex, int clause_index, gpointer handler_ip);
	void (*frame_iter_init) (MonoInterpStackIter *iter, gpointer interp_exit_data);
	gboolean (*frame_iter_next) (MonoInterpStackIter *iter, StackFrameInfo *frame);
	MonoJitInfo* (*find_jit_info) (MonoDomain *domain, MonoMethod *method);
	void (*set_breakpoint) (MonoJitInfo *jinfo, gpointer ip);
	void (*clear_breakpoint) (MonoJitInfo *jinfo, gpointer ip);
	MonoJitInfo* (*frame_get_jit_info) (MonoInterpFrameHandle frame);
	gpointer (*frame_get_ip) (MonoInterpFrameHandle frame);
	gpointer (*frame_get_arg) (MonoInterpFrameHandle frame, int pos);
	gpointer (*frame_get_local) (MonoInterpFrameHandle frame, int pos);
	gpointer (*frame_get_this) (MonoInterpFrameHandle frame);
	MonoInterpFrameHandle (*frame_get_parent) (MonoInterpFrameHandle frame);
	void (*start_single_stepping) (void);
	void (*stop_single_stepping) (void);
};

void mono_interp_parse_options (const char *options);

void mono_interp_init (void);

#endif /* __MONO_MINI_INTERPRETER_H__ */
