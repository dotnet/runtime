/**
 * \file
 * Copyright 2008-2010 Novell, Inc.
 * Copyright 2011 Xamarin Inc.
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_MONO_STACK_UNWINDING_H__
#define __MONO_MONO_STACK_UNWINDING_H__

#include <mono/metadata/appdomain.h>
#include <mono/metadata/metadata.h>
#include <mono/utils/mono-context.h>

/*
 * Possible frame types returned by the stack walker.
 */
typedef enum {
	/* Normal managed frames */
	FRAME_TYPE_MANAGED = 0,
	/* Pseudo frame marking the start of a method invocation done by the soft debugger */
	FRAME_TYPE_DEBUGGER_INVOKE = 1,
	/* Frame for transitioning to native code */
	FRAME_TYPE_MANAGED_TO_NATIVE = 2,
	FRAME_TYPE_TRAMPOLINE = 3,
	/* Interpreter frame */
	FRAME_TYPE_INTERP = 4,
	/* Frame for transitioning from interpreter to managed code */
	FRAME_TYPE_INTERP_TO_MANAGED = 5,
	FRAME_TYPE_NUM = 6
} MonoStackFrameType;

typedef enum {
	MONO_UNWIND_NONE = 0x0,
	MONO_UNWIND_LOOKUP_IL_OFFSET = 0x1,
	/* NOT signal safe */
	MONO_UNWIND_LOOKUP_ACTUAL_METHOD = 0x2,
	/*
	 * Store the locations where caller-saved registers are saved on the stack in
	 * frame->reg_locations. The pointer is only valid during the call to the unwind
	 * callback.
	 */
	MONO_UNWIND_REG_LOCATIONS = 0x4,
	MONO_UNWIND_DEFAULT = MONO_UNWIND_LOOKUP_ACTUAL_METHOD,
	MONO_UNWIND_SIGNAL_SAFE = MONO_UNWIND_NONE,
	MONO_UNWIND_LOOKUP_ALL = MONO_UNWIND_LOOKUP_IL_OFFSET | MONO_UNWIND_LOOKUP_ACTUAL_METHOD,
} MonoUnwindOptions;

typedef struct {
	MonoStackFrameType type;
	/* 
	 * For FRAME_TYPE_MANAGED, otherwise NULL.
	 */
	MonoJitInfo *ji;
	/*
	 * Same as ji->method.
	 * Not valid if ASYNC_CONTEXT is true.
	 */
	MonoMethod *method;
	/*
	 * If ji->method is a gshared method, this is the actual method instance.
	 * This is only filled if lookup for actual method was requested (MONO_UNWIND_LOOKUP_ACTUAL_METHOD)
	 * Not valid if ASYNC_CONTEXT is true.
	 */
	MonoMethod *actual_method;
	/* The domain containing the code executed by this frame */
	MonoDomain *domain;
	/* Whenever method is a user level method */
	gboolean managed;
	/*
	 * Whenever this frame was loaded in async context.
	 */
	gboolean async_context;
	int native_offset;
	/*
	 * IL offset of this frame.
	 * Only available if the runtime have debugging enabled (--debug switch) and 
	 *  il offset resultion was requested (MONO_UNWIND_LOOKUP_IL_OFFSET)
	 */
	int il_offset;

	/* For FRAME_TYPE_INTERP_EXIT */
	gpointer interp_exit_data;

	/* For FRAME_TYPE_INTERP */
	gpointer interp_frame;

	/* The next fields are only useful for the jit */
	gpointer lmf;
	guint32 unwind_info_len;
	guint8 *unwind_info;

	mgreg_t **reg_locations;
} MonoStackFrameInfo;

/*Index into MonoThreadState::unwind_data. */
enum {
	MONO_UNWIND_DATA_DOMAIN,
	MONO_UNWIND_DATA_LMF,
	MONO_UNWIND_DATA_JIT_TLS,	
};

/*
 * This structs holds all information needed to unwind the stack
 * of a thread.
 */
typedef struct {
	MonoContext ctx;
	gpointer unwind_data [3]; /*right now: domain, lmf and jit_tls*/
	gboolean valid;
	void *gc_stackdata;
	int gc_stackdata_size;
} MonoThreadUnwindState;


#endif
