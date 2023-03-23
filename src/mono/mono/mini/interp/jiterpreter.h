#ifndef __MONO_MINI_JITERPRETER_H__
#define __MONO_MINI_JITERPRETER_H__

#ifdef HOST_BROWSER

#ifdef DISABLE_THREADS
#define JITERPRETER_ENABLE_JIT_CALL_TRAMPOLINES 1
// enables specialized mono_llvm_cpp_catch_exception replacement (see jiterpreter-jit-call.ts)
// works even if the jiterpreter is otherwise disabled.
#define JITERPRETER_ENABLE_SPECIALIZED_JIT_CALL 1
#else
#define JITERPRETER_ENABLE_JIT_CALL_TRAMPOLINES 0
#define JITERPRETER_ENABLE_SPECIALIZED_JIT_CALL 0
#endif // DISABLE_THREADS

// mono_interp_tier_prepare_jiterpreter will return these special values if it doesn't
//  have a function pointer for a specific entry point.
// TRAINING indicates that the hit count is not high enough yet
#define JITERPRETER_TRAINING 0
// NOT_JITTED indicates that the trace was not jitted and it should be turned into a NOP
#define JITERPRETER_NOT_JITTED 1

typedef struct {
	gint32 backward_branch_taken;
	gint32 bailout_opcode_count;
} JiterpreterCallInfo;

typedef const ptrdiff_t (*JiterpreterThunk) (void *frame, void *pLocals, JiterpreterCallInfo *cinfo);
typedef void (*WasmJitCallThunk) (void *ret_sp, void *sp, void *ftndesc, gboolean *thrown);
typedef void (*WasmDoJitCall) (gpointer cb, gpointer arg, gboolean *out_thrown);

// Parses a single jiterpreter runtime option. This is used both by driver.c and our typescript
gboolean
mono_jiterp_parse_option (const char *option);

// HACK: Prevent jiterpreter.c from being entirely linked out, because the typescript relies on it
void
jiterp_preserve_module ();

// HACK: Pass void* so that this header can include safely in files without definition for TransformData
void
jiterp_insert_entry_points (void *imethod, void *td);

// used by the typescript JIT implementation to notify the runtime that it has finished jitting a thunk
//  for a specific callsite, since it can take a while before it happens
void
mono_jiterp_register_jit_call_thunk (void *cinfo, WasmJitCallThunk thunk);

extern void
mono_interp_record_interp_entry (void *fn_ptr);

// jiterpreter-interp-entry.ts
// HACK: Pass void* so that this header can include safely in files without definition for InterpMethod
extern gpointer
mono_interp_jit_wasm_entry_trampoline (
	void *imethod, MonoMethod *method, int argument_count, MonoType *param_types,
	int unbox, int has_this, int has_return, const char *name, void *default_implementation
);

// Fast-path implemented in C
JiterpreterThunk
mono_interp_tier_prepare_jiterpreter_fast (
	void *frame, MonoMethod *method, const guint16 *ip,
	const guint16 *start_of_body, int size_of_body
);

// HACK: Pass void* so that this header can include safely in files without definition for InterpFrame
// Slow-path implemented in TypeScript, actually performs JIT
extern JiterpreterThunk
mono_interp_tier_prepare_jiterpreter (
	void *frame, MonoMethod *method, const guint16 *ip, gint32 trace_index,
	const guint16 *start_of_body, int size_of_body
);

// HACK: Pass void* so that this header can include safely in files without definition for InterpMethod,
//  or JitCallInfo
extern void
mono_interp_jit_wasm_jit_call_trampoline (
	MonoMethod *method, void *rmethod, void *cinfo,
	guint32 *arg_offsets, gboolean catch_exceptions
);

// synchronously jits everything waiting in the do_jit_call jit queue
extern void
mono_interp_flush_jitcall_queue ();

// invokes a specific jit call trampoline with JS exception handling. this is only used if
//  we were unable to use WASM EH to perform exception handling, either because it was
//  disabled or because the current runtime environment does not support it
extern void
mono_interp_invoke_wasm_jit_call_trampoline (
	WasmJitCallThunk thunk,
	void *ret_sp, void *sp,
	void *ftndesc, gboolean *thrown
);

extern void
mono_jiterp_do_jit_call_indirect (
	gpointer cb, gpointer arg, gboolean *out_thrown
);

#ifdef __MONO_MINI_INTERPRETER_INTERNALS_H__

typedef struct {
	InterpMethod *rmethod;
	ThreadContext *context;
	gpointer orig_domain;
	gpointer attach_cookie;
} JiterpEntryDataHeader;

// we optimize delegate calls by attempting to cache the delegate invoke
//  target - this will improve performance when the same delegate is invoked
//  repeatedly inside a loop
typedef struct {
	MonoDelegate *delegate_invoke_is_for;
	MonoMethod *delegate_invoke;
	InterpMethod *delegate_invoke_rmethod;
} JiterpEntryDataCache;

// jitted interp_entry wrappers use custom tracking data structures
//  that are allocated in the heap, one per wrapper
// FIXME: For thread safety we need to make these thread-local or stack-allocated
// Note that if we stack allocate these the cache will need to move somewhere else
typedef struct {
	// We split the cache out from the important data so that when
	//  jiterp_interp_entry copies the important data it doesn't have
	//  to also copy the cache. This reduces overhead slightly
	JiterpEntryDataHeader header;
	JiterpEntryDataCache cache;
} JiterpEntryData;

volatile size_t *
mono_jiterp_get_polling_required_address (void);

void
mono_jiterp_do_safepoint (InterpFrame *frame, guint16 *ip);

void
mono_jiterp_interp_entry (JiterpEntryData *_data, stackval *sp_args, void *res);

gpointer
mono_jiterp_imethod_to_ftnptr (InterpMethod *imethod);

void
mono_jiterp_enum_hasflag (MonoClass *klass, gint32 *dest, stackval *sp1, stackval *sp2);

ptrdiff_t
mono_jiterp_monitor_trace (const guint16 *ip, void *frame, void *locals);

#endif // __MONO_MINI_INTERPRETER_INTERNALS_H__

extern WasmDoJitCall jiterpreter_do_jit_call;

#endif // HOST_BROWSER

#endif // __MONO_MINI_JITERPRETER_H__
