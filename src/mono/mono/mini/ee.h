/*
 * Licensed to the .NET Foundation under one or more agreements.
 * The .NET Foundation licenses this file to you under the MIT license.
 */

#include <config.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/object.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-error.h>
#include <mono/utils/mono-publib.h>
#include <mono/eglib/glib.h>

#ifndef __MONO_EE_H__
#define __MONO_EE_H__

#define MONO_EE_API_VERSION 0x10

typedef struct _MonoInterpStackIter MonoInterpStackIter;

/* Needed for stack allocation */
struct _MonoInterpStackIter {
	gpointer dummy [8];
};

typedef gpointer MonoInterpFrameHandle;

#define MONO_EE_CALLBACKS \
	MONO_EE_CALLBACK (void, entry_from_trampoline, (gpointer ccontext, gpointer imethod)) \
	MONO_EE_CALLBACK (void, to_native_trampoline, (gpointer addr, gpointer ccontext)) \
	MONO_EE_CALLBACK (gpointer, create_method_pointer, (MonoMethod *method, gboolean compile, MonoError *error)) \
	MONO_EE_CALLBACK (MonoFtnDesc*, create_method_pointer_llvmonly, (MonoMethod *method, gboolean unbox, MonoError *error)) \
	MONO_EE_CALLBACK (void, free_method, (MonoDomain *domain, MonoMethod *method)) \
	MONO_EE_CALLBACK (MonoObject*, runtime_invoke, (MonoMethod *method, void *obj, void **params, MonoObject **exc, MonoError *error)) \
	MONO_EE_CALLBACK (void, init_delegate, (MonoDelegate *del, MonoError *error)) \
	MONO_EE_CALLBACK (void, delegate_ctor, (MonoObjectHandle this_obj, MonoObjectHandle target, gpointer addr, MonoError *error)) \
	MONO_EE_CALLBACK (gpointer, get_remoting_invoke, (MonoMethod *method, gpointer imethod, MonoError *error)) \
	MONO_EE_CALLBACK (void, set_resume_state, (MonoJitTlsData *jit_tls, MonoObject *ex, MonoJitExceptionInfo *ei, MonoInterpFrameHandle interp_frame, gpointer handler_ip)) \
	MONO_EE_CALLBACK (void, get_resume_state, (const MonoJitTlsData *jit_tls, gboolean *has_resume_state, MonoInterpFrameHandle *interp_frame, gpointer *handler_ip)) \
	MONO_EE_CALLBACK (gboolean, run_finally, (StackFrameInfo *frame, int clause_index, gpointer handler_ip, gpointer handler_ip_end)) \
	MONO_EE_CALLBACK (gboolean, run_filter, (StackFrameInfo *frame, MonoException *ex, int clause_index, gpointer handler_ip, gpointer handler_ip_end)) \
	MONO_EE_CALLBACK (void, frame_iter_init, (MonoInterpStackIter *iter, gpointer interp_exit_data)) \
	MONO_EE_CALLBACK (gboolean, frame_iter_next, (MonoInterpStackIter *iter, StackFrameInfo *frame)) \
	MONO_EE_CALLBACK (MonoJitInfo*, find_jit_info, (MonoDomain *domain, MonoMethod *method)) \
	MONO_EE_CALLBACK (void, set_breakpoint, (MonoJitInfo *jinfo, gpointer ip)) \
	MONO_EE_CALLBACK (void, clear_breakpoint, (MonoJitInfo *jinfo, gpointer ip)) \
	MONO_EE_CALLBACK (MonoJitInfo*, frame_get_jit_info, (MonoInterpFrameHandle frame)) \
	MONO_EE_CALLBACK (gpointer, frame_get_ip, (MonoInterpFrameHandle frame)) \
	MONO_EE_CALLBACK (gpointer, frame_get_arg, (MonoInterpFrameHandle frame, int pos)) \
	MONO_EE_CALLBACK (gpointer, frame_get_local, (MonoInterpFrameHandle frame, int pos)) \
	MONO_EE_CALLBACK (gpointer, frame_get_this, (MonoInterpFrameHandle frame)) \
	MONO_EE_CALLBACK (void, frame_arg_to_data, (MonoInterpFrameHandle frame, MonoMethodSignature *sig, int index, gpointer data)) \
	MONO_EE_CALLBACK (void, data_to_frame_arg, (MonoInterpFrameHandle frame, MonoMethodSignature *sig, int index, gconstpointer data)) \
	MONO_EE_CALLBACK (gpointer, frame_arg_to_storage, (MonoInterpFrameHandle frame, MonoMethodSignature *sig, int index)) \
	MONO_EE_CALLBACK (MonoInterpFrameHandle, frame_get_parent, (MonoInterpFrameHandle frame)) \
	MONO_EE_CALLBACK (void, start_single_stepping, (void)) \
	MONO_EE_CALLBACK (void, stop_single_stepping, (void)) \
	MONO_EE_CALLBACK (void, free_context, (gpointer)) \
	MONO_EE_CALLBACK (void, set_optimizations, (guint32)) \
	MONO_EE_CALLBACK (void, metadata_update_init, (MonoError *error)) \
	MONO_EE_CALLBACK (void, invalidate_transformed, (MonoDomain *domain)) \
	MONO_EE_CALLBACK (void, cleanup, (void)) \
	MONO_EE_CALLBACK (void, mark_stack, (gpointer thread_info, GcScanFunc func, gpointer gc_data, gboolean precise)) \

typedef struct _MonoEECallbacks {

#undef MONO_EE_CALLBACK
#define MONO_EE_CALLBACK(ret, name, sig) ret (*name) sig;

	MONO_EE_CALLBACKS

} MonoEECallbacks;

#endif /* __MONO_EE_H__ */
