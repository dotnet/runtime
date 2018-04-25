/*
 * Licensed to the .NET Foundation under one or more agreements.
 * The .NET Foundation licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
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

#define MONO_EE_API_VERSION 0x3

typedef struct _MonoInterpStackIter MonoInterpStackIter;

/* Needed for stack allocation */
struct _MonoInterpStackIter {
	gpointer dummy [8];
};

typedef gpointer MonoInterpFrameHandle;

struct _MonoEECallbacks {
	void (*entry_from_trampoline) (gpointer ccontext, gpointer imethod);
	gpointer (*create_method_pointer) (MonoMethod *method, MonoError *error);
	MonoObject* (*runtime_invoke) (MonoMethod *method, void *obj, void **params, MonoObject **exc, MonoError *error);
	void (*init_delegate) (MonoDelegate *del);
	gpointer (*get_remoting_invoke) (gpointer imethod, MonoError *error);
	gpointer (*create_trampoline) (MonoDomain *domain, MonoMethod *method, MonoError *error);
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
	void (*frame_arg_to_data) (MonoInterpFrameHandle frame, MonoMethodSignature *sig, int index, gpointer data);
	void (*data_to_frame_arg) (MonoInterpFrameHandle frame, MonoMethodSignature *sig, int index, gpointer data);
	gpointer (*frame_arg_to_storage) (MonoInterpFrameHandle frame, MonoMethodSignature *sig, int index);
	void (*frame_arg_set_storage) (MonoInterpFrameHandle frame, MonoMethodSignature *sig, int index, gpointer storage);
	MonoInterpFrameHandle (*frame_get_parent) (MonoInterpFrameHandle frame);
	void (*start_single_stepping) (void);
	void (*stop_single_stepping) (void);
};

typedef struct _MonoEECallbacks MonoEECallbacks;

#endif /* __MONO_EE_H__ */
