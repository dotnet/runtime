// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// This file contains icalls used in jitted interpreter traces and wrappers,
//  along with infrastructure to support code generration

#ifndef __USE_ISOC99
#define __USE_ISOC99
#endif
#include "config.h"

#if 0
#define jiterp_assert(b) g_assert(b)
#else
#define jiterp_assert(b)
#endif

void jiterp_preserve_module (void);

#if HOST_BROWSER

#include <emscripten.h>

#include <string.h>
#include <stdlib.h>
#include <math.h>

#include <mono/metadata/mono-config.h>
#include <mono/utils/mono-threads.h>
#include <mono/metadata/gc-internals.h>

#include "interp.h"
#include "interp-internals.h"
#include "mintops.h"
#include "transform.h"
#include "interp-intrins.h"
#include "tiering.h"

#include <mono/mini/mini.h>
#include <mono/mini/mini-runtime.h>
#include <mono/mini/aot-runtime.h>
#include <mono/mini/llvm-runtime.h>
#include <mono/mini/llvmonly-runtime.h>
#include <mono/utils/options.h>

#include "jiterpreter.h"

static gint32 jiterpreter_abort_counts[MINT_LASTOP + 1] = { 0 };
static int64_t jiterp_trace_bailout_counts[256] = { 0 };

// This function pointer is used by interp.c to invoke jit_call_cb for exception handling purposes
// See jiterpreter-jit-call.ts mono_jiterp_do_jit_call_indirect
WasmDoJitCall jiterpreter_do_jit_call = mono_jiterp_do_jit_call_indirect;

// We disable this diagnostic because EMSCRIPTEN_KEEPALIVE makes it a false alarm, the keepalive
//  functions are being used externally. Having a bunch of prototypes is pointless since these
//  functions are not consumed by C anywhere else
#pragma clang diagnostic ignored "-Wmissing-prototypes"

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_encode_leb64_ref (unsigned char * destination, void * source, int valueIsSigned) {
	if (!destination || !source)
		return 0;

	unsigned char b;
	unsigned char * originalDestination = destination;
	if (valueIsSigned) {
		int64_t value = *((int64_t*)source);
		int more = 1, signBit;

		while (more) {
			b = (unsigned char)(value & 0x7FL);
			value >>= 7;

			signBit = (b & 0x40u) != 0;
			if (
				((value == 0) && !signBit) ||
				((value == -1) && signBit)
			)
				more = 0;
			else
				b |= 0x80;

			*destination++ = b;
		}
	} else {
		uint64_t value = *((uint64_t*)source);

		do {
			b = (unsigned char)(value & 0x7Ful);
			value >>= 7;

			if (value != 0)
				b |= 0x80;

			*destination++ = b;
		} while (value != 0);
	}

	return (int)(destination - originalDestination);
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_encode_leb52 (unsigned char * destination, double doubleValue, int valueIsSigned) {
	if (!destination)
		return 0;

	if (valueIsSigned) {
		int64_t value = (int64_t)doubleValue;
		if (((double)value) != doubleValue)
			return 0;

		return mono_jiterp_encode_leb64_ref(destination, &value, valueIsSigned);
	} else {
		uint64_t value = (uint64_t)doubleValue;
		if (((double)value) != doubleValue)
			return 0;

		return mono_jiterp_encode_leb64_ref(destination, &value, valueIsSigned);
	}
}

// Many of the following functions implement various opcodes or provide support for opcodes
//  so that jiterpreter traces don't have to inline dozens of wasm instructions worth of
//  complex logic - these are designed to match interp.c

EMSCRIPTEN_KEEPALIVE double
mono_jiterp_fmod (double lhs, double rhs) {
	return fmod(lhs, rhs);
}

EMSCRIPTEN_KEEPALIVE double
mono_jiterp_atan2 (double lhs, double rhs) {
	return atan2(lhs, rhs);
}

// If a trace is jitted for a method that hasn't been tiered yet, we need to
//  update the interpreter entry count for the method.
EMSCRIPTEN_KEEPALIVE int
mono_jiterp_increase_entry_count (void *_imethod) {
	InterpMethod *imethod = (InterpMethod*)_imethod;
	imethod->entry_count++;
	// Return whether the trace should bail out because the method needs to be tiered
	return imethod->entry_count >= INTERP_TIER_ENTRY_LIMIT;
}

EMSCRIPTEN_KEEPALIVE void*
mono_jiterp_object_unbox (MonoObject *obj) {
	return mono_object_unbox_internal(obj);
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_try_unbox_ref (
	MonoClass *klass, void **dest, MonoObject **src
) {
	if (!klass)
		return 0;

	MonoObject *o = *src;
	if (!o)
		return 0;

	if (
		!(
			(m_class_get_rank (o->vtable->klass) == 0) &&
			(m_class_get_element_class (o->vtable->klass) == m_class_get_element_class (klass))
		)
	)
		return 0;

	*dest = mono_object_unbox_internal(o);
	return 1;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_type_is_byref (MonoType *type) {
	if (!type)
		return 0;
	return m_type_is_byref(type);
}

EMSCRIPTEN_KEEPALIVE void
mono_jiterp_value_copy (void *dest, void *src, MonoClass *klass) {
	mono_value_copy_internal(dest, src, klass);
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_strlen_ref (MonoString **ppString, int *result) {
	MonoString *pString = *ppString;
	if (!pString)
		return 0;

	*result = mono_string_length_internal(pString);
	return 1;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_getchr_ref (MonoString **ppString, int *pIndex, int *result) {
	int index = *pIndex;
	MonoString *pString = *ppString;
	if (!pString)
		return 0;
	if ((index < 0) || (index >= mono_string_length_internal(pString)))
		return 0;

	*result = mono_string_chars_internal(pString)[index];
	return 1;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_try_newobj_inlined (MonoObject **destination, MonoVTable *vtable) {
	*destination = 0;
	if (!vtable->initialized)
		return 0;

	*destination = mono_gc_alloc_obj(vtable, m_class_get_instance_size(vtable->klass));
	if (!destination)
		return 0;

	return 1;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_getitem_span (
	void **destination, MonoSpanOfVoid *span, int index, size_t element_size
) {
	if (!span)
		return 0;

	const gint32 length = span->_length;
	if ((index < 0) || (index >= length))
		return 0;

	unsigned char * pointer = (unsigned char *)span->_reference;
	*destination = pointer + (index * element_size);
	return 1;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_gettype_ref (
	MonoObject **destination, MonoObject **source
) {
	MonoObject *obj = *source;
	if (obj) {
		*destination = (obj)->vtable->type;
		return 1;
	} else
		return 0;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_cast_ref (
	MonoObject **destination, MonoObject **source,
	MonoClass *klass, MintOpcode opcode
) {
	if (!klass)
		return 0;

	MonoObject *obj = *source;
	if (!obj) {
		*destination = 0;
		return 1;
	}

	switch (opcode) {
		case MINT_CASTCLASS:
		case MINT_ISINST: {
			if (obj) {
				// FIXME push/pop LMF
				if (!mono_jiterp_isinst (obj, klass)) { // FIXME: do not swallow the error
					if (opcode == MINT_ISINST)
						*destination = NULL;
					else
						return 0; // bailout
				} else {
					*destination = obj;
				}
			} else {
				*destination = NULL;
			}
			return 1;
		}
		case MINT_CASTCLASS_INTERFACE:
		case MINT_ISINST_INTERFACE: {
			gboolean isinst;
			// FIXME: Perform some of this work at JIT time
			if (MONO_VTABLE_IMPLEMENTS_INTERFACE (obj->vtable, m_class_get_interface_id (klass))) {
				isinst = TRUE;
			} else if (m_class_is_array_special_interface (klass)) {
				/* slow path */
				// FIXME push/pop LMF
				isinst = mono_jiterp_isinst (obj, klass); // FIXME: do not swallow the error
			} else {
				isinst = FALSE;
			}

			if (!isinst) {
				if (opcode == MINT_ISINST_INTERFACE)
					*destination = NULL;
				else
					return 0; // bailout
			} else {
				*destination = obj;
			}
			return 1;
		}
		case MINT_CASTCLASS_COMMON:
		case MINT_ISINST_COMMON: {
			if (obj) {
				gboolean isinst = mono_class_has_parent_fast (obj->vtable->klass, klass);

				if (!isinst) {
					if (opcode == MINT_ISINST_COMMON)
						*destination = NULL;
					else
						return 0; // bailout
				} else {
					*destination = obj;
				}
			} else {
				*destination = NULL;
			}
			return 1;
		}
	}

	return 0;
}

EMSCRIPTEN_KEEPALIVE void*
mono_jiterp_array_get_element_address_with_size_ref (MonoArray **array, int size, int index)
{
	// HACK: This does not need to be volatile because we know array is visible to
	//  the GC and this is called from interp traces in gc unsafe mode
	MonoArray* _array = *array;
	if (!_array)
		return NULL;
	if (index >= mono_array_length_internal(_array))
		return NULL;
	return mono_array_addr_with_size_fast (_array, size, index);
}

EMSCRIPTEN_KEEPALIVE void
mono_jiterp_localloc (gpointer *destination, gint32 len, InterpFrame *frame)
{
	ThreadContext *context = mono_jiterp_get_context();
	gpointer mem;
	if (len > 0) {
		mem = mono_jiterp_frame_data_allocator_alloc (&context->data_stack, frame, ALIGN_TO (len, MINT_VT_ALIGNMENT));

		if (frame->imethod->init_locals)
			memset (mem, 0, len);
	} else {
		mem = NULL;
	}
	*destination = mem;
}

EMSCRIPTEN_KEEPALIVE void
mono_jiterp_ldtsflda (gpointer *destination, guint32 offset) {
	MonoInternalThread *thread = mono_thread_internal_current ();
	*destination = ((char*)thread->static_data [offset & 0x3f]) + (offset >> 6);
}

EMSCRIPTEN_KEEPALIVE void
mono_jiterp_box_ref (MonoVTable *vtable, MonoObject **dest, void *src, gboolean vt) {
	HANDLE_FUNCTION_ENTER ();

	MonoObjectHandle tmp_handle = MONO_HANDLE_NEW (MonoObject, NULL);

	// FIXME push/pop LMF
	MonoObject *o = mono_gc_alloc_obj (vtable, m_class_get_instance_size (vtable->klass));
	MONO_HANDLE_ASSIGN_RAW (tmp_handle, o);
	if (vt)
		mono_value_copy_internal (mono_object_get_data (o), src, vtable->klass);
	else
		mono_jiterp_stackval_to_data (m_class_get_byval_arg (vtable->klass), (stackval*)(src), mono_object_get_data (o));
	MONO_HANDLE_ASSIGN_RAW (tmp_handle, NULL);

	*dest = o;

	HANDLE_FUNCTION_RETURN ();
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_conv_ovf (void *dest, void *src, int opcode) {
	switch (opcode) {
		case MINT_CONV_OVF_I4_I8: {
			gint64 val = *(gint64*)src;
			if (val < G_MININT32 || val > G_MAXINT32)
				return 0;
			*(gint32*)dest = (gint32) val;
			return 1;
		}

		case MINT_CONV_OVF_U4_I8: {
			gint64 val = *(gint64*)src;
			if (val < 0 || val > G_MAXUINT32)
				return 0;
			*(guint32*)dest = (guint32) val;
			return 1;
		}

		case MINT_CONV_OVF_I4_U8: {
			guint64 val = *(guint64*)src;
			if (val > G_MAXINT32)
				return 0;
			*(gint32*)dest = (gint32) val;
			return 1;
		}

		case MINT_CONV_OVF_U4_I4: {
			gint32 val = *(gint32*)src;
			if (val < 0)
				return 0;
			*(guint32*)dest = (guint32) val;
			return 1;
		}

		case MINT_CONV_OVF_I4_R8:
		case MINT_CONV_OVF_I4_R4: {
			double val;
			if (opcode == MINT_CONV_OVF_I4_R4)
				val = *(float*)src;
			else
				val = *(double*)src;

			if (val > ((double)G_MININT32 - 1) && val < ((double)G_MAXINT32 + 1)) {
				*(gint32*)dest = (gint32) val;
				return 1;
			}
			return 0;
		}
	}

	// TODO: return 0 on success and a unique bailout code on failure?
	// Probably not necessary right now and would bloat traces slightly
	return 0;
}

// we use these helpers at JIT time to figure out where to do memory loads and stores
EMSCRIPTEN_KEEPALIVE size_t
mono_jiterp_get_offset_of_vtable_initialized_flag () {
	return offsetof(MonoVTable, initialized);
}

EMSCRIPTEN_KEEPALIVE size_t
mono_jiterp_get_offset_of_array_data () {
	return MONO_STRUCT_OFFSET (MonoArray, vector);
}

EMSCRIPTEN_KEEPALIVE size_t
mono_jiterp_get_size_of_stackval () {
	return sizeof(stackval);
}

// jiterpreter-interp-entry.ts uses this information to decide whether to call
//  stackval_from_data for a given type or just do a raw value copy of N bytes
EMSCRIPTEN_KEEPALIVE int
mono_jiterp_type_get_raw_value_size (MonoType *type) {
	// We use a NULL type to indicate that we want a raw ptr copy
	if ((type == NULL) || m_type_is_byref (type))
		return 256;

	switch (type->type) {
		// for unsigned types we return a negative size to communicate to
		//  the jiterpreter implementation that it should use the _u version
		//  of the wasm load opcodes instead of the _s version
		case MONO_TYPE_U1:
			return -1;
		case MONO_TYPE_U2:
		case MONO_TYPE_CHAR:
			return -2;

		case MONO_TYPE_I1:
			return 1;
		case MONO_TYPE_I2:
			return 2;
		case MONO_TYPE_I:
		case MONO_TYPE_I4:
		case MONO_TYPE_U:
		case MONO_TYPE_U4:
		case MONO_TYPE_PTR:
		case MONO_TYPE_FNPTR:
			return 4;

		default:
			return 0;
	}
}

// we use these helpers to record when a trace bails out (in countBailouts mode)
EMSCRIPTEN_KEEPALIVE void*
mono_jiterp_trace_bailout (void* rip, int reason)
{
	if (reason < 256)
		jiterp_trace_bailout_counts[reason]++;
	return rip;
}

EMSCRIPTEN_KEEPALIVE double
mono_jiterp_get_trace_bailout_count (int reason)
{
	if (reason > 255)
		return -1;

	int64_t result = jiterp_trace_bailout_counts[reason];
	return (double)result;
}

// we use this to record how many times a trace has aborted due to a given opcode.
// this is done in C because the heuristic updates it along with typescript updating it
EMSCRIPTEN_KEEPALIVE gint32
mono_jiterp_adjust_abort_count (MintOpcode opcode, gint32 delta) {
	if ((opcode < 0) || (opcode >= MINT_LASTOP))
		return 0;
	if (delta != 0)
		jiterpreter_abort_counts[opcode] += delta;
	return jiterpreter_abort_counts[opcode];
}

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

// at the start of a jitted interp_entry wrapper, this is called to perform initial setup
//  like resolving the target for delegates and setting up the thread context
// inlining this into the wrappers would make them unnecessarily big and complex
EMSCRIPTEN_KEEPALIVE stackval *
mono_jiterp_interp_entry_prologue (JiterpEntryData *data, void *this_arg)
{
	stackval *sp_args;
	MonoMethod *method;
	InterpMethod *rmethod;
	ThreadContext *context;

	// unbox implemented by jit

	jiterp_assert(data);
	rmethod = data->header.rmethod;
	jiterp_assert(rmethod);
	method = rmethod->method;
	jiterp_assert(method);

	if (mono_interp_is_method_multicastdelegate_invoke(method)) {
		// Copy the current state of the cache before using it
		JiterpEntryDataCache cache = data->cache;
		if (this_arg && (cache.delegate_invoke_is_for == (MonoDelegate*)this_arg)) {
			// We previously cached the invoke for this delegate
			method = cache.delegate_invoke;
			data->header.rmethod = rmethod = cache.delegate_invoke_rmethod;
		} else {
			/*
			* This happens when AOT code for the invoke wrapper is not found.
			* Have to replace the method with the wrapper here, since the wrapper depends on the delegate.
			*/
			MonoDelegate *del = (MonoDelegate*)this_arg;
			method = mono_marshal_get_delegate_invoke (method, del);
			data->header.rmethod = rmethod = mono_interp_get_imethod (method);

			// Cache the delegate invoke. This works because data was allocated statically
			//  when the jitted trampoline was created, so it will stick around.
			// FIXME: Thread safety
			data->cache.delegate_invoke_is_for = NULL;
			data->cache.delegate_invoke = method;
			data->cache.delegate_invoke_rmethod = rmethod;
			data->cache.delegate_invoke_is_for = del;
		}
	}

	// FIXME: Thread safety

	if (rmethod->needs_thread_attach)
		data->header.orig_domain = mono_threads_attach_coop (mono_domain_get (), &data->header.attach_cookie);
	else
		data->header.orig_domain = data->header.attach_cookie = NULL;

	data->header.context = context = mono_jiterp_get_context ();
	sp_args = (stackval*)context->stack_pointer;

	return sp_args;
}

// after interp_entry_prologue the wrapper will set up all the argument values
//  in the correct place and compute the stack offset, then it passes that in to this
//  function in order to actually enter the interpreter and process the return value
EMSCRIPTEN_KEEPALIVE void
mono_jiterp_interp_entry (JiterpEntryData *_data, stackval *sp_args, void *res)
{
	JiterpEntryDataHeader header;
	MonoType *type;

	// Copy the scratch buffer into a local variable. This is necessary for us to be
	//  reentrant-safe because mono_interp_exec_method could end up hitting the trampoline
	//  again
	jiterp_assert(_data);
	header = _data->header;

	jiterp_assert(header.rmethod);
	jiterp_assert(header.rmethod->method);
	jiterp_assert(sp_args);

	stackval *sp = (stackval*)header.context->stack_pointer;

	InterpFrame frame = {0};
	frame.imethod = header.rmethod;
	frame.stack = sp;
	frame.retval = sp;

	header.context->stack_pointer = (guchar*)sp_args;
	g_assert ((guchar*)sp_args < header.context->stack_end);

	MONO_ENTER_GC_UNSAFE;
	mono_interp_exec_method (&frame, header.context, NULL);
	MONO_EXIT_GC_UNSAFE;

	header.context->stack_pointer = (guchar*)sp;

	if (header.rmethod->needs_thread_attach)
		mono_threads_detach_coop (header.orig_domain, &header.attach_cookie);

	mono_jiterp_check_pending_unwind (header.context);

	if (mono_llvm_only) {
		if (header.context->has_resume_state)
			/* The exception will be handled in a frame above us */
			mono_llvm_cpp_throw_exception ();
	} else {
		g_assert (!header.context->has_resume_state);
	}

	// The return value is at the bottom of the stack, after the locals space
	type = header.rmethod->rtype;
	if (type->type != MONO_TYPE_VOID)
		mono_jiterp_stackval_to_data (type, frame.stack, res);
}

// should_abort_trace returns one of these codes depending on the opcode and current state
#define TRACE_IGNORE -1
#define TRACE_CONTINUE 0
#define TRACE_ABORT 1

/*
 * This function provides an approximate answer for "will this instruction cause the jiterpreter
 *  to abort trace compilation here?" so that we can decide whether it's worthwhile to have
 *  a trace entry instruction at various points in a method. It doesn't need to be exact, it just
 *  needs to provide correct answers often enough so that we avoid generating lots of expensive
 *  trace nops while still ensuring we put entry points where we need them.
 * At present this is around 94-97% accurate, which is more than good enough
 */
static int
jiterp_should_abort_trace (InterpInst *ins, gboolean *inside_branch_block)
{
	guint16 opcode = ins->opcode;
	switch (opcode) {
		// Individual instructions that never abort traces.
		// Please keep this in sync with jiterpreter.ts:generate_wasm_body
		case MINT_TIER_ENTER_METHOD:
		case MINT_TIER_PATCHPOINT:
		case MINT_TIER_PREPARE_JITERPRETER:
		case MINT_TIER_NOP_JITERPRETER:
		case MINT_TIER_ENTER_JITERPRETER:
		case MINT_NOP:
		case MINT_DEF:
		case MINT_DUMMY_USE:
		case MINT_IL_SEQ_POINT:
		case MINT_TIER_PATCHPOINT_DATA:
		case MINT_MONO_MEMORY_BARRIER:
		case MINT_SDB_BREAKPOINT:
		case MINT_SDB_INTR_LOC:
		case MINT_SDB_SEQ_POINT:
			return TRACE_IGNORE;

		case MINT_INITLOCAL:
		case MINT_INITLOCALS:
		case MINT_LOCALLOC:
		case MINT_INITOBJ:
		case MINT_CKNULL:
		case MINT_LDLOCA_S:
		case MINT_LDTOKEN:
		case MINT_LDSTR:
		case MINT_LDFTN_ADDR:
		case MINT_MONO_LDPTR:
		case MINT_CPOBJ_VT:
		case MINT_LDOBJ_VT:
		case MINT_STOBJ_VT:
		case MINT_STRLEN:
		case MINT_GETCHR:
		case MINT_GETITEM_SPAN:
		case MINT_INTRINS_SPAN_CTOR:
		case MINT_INTRINS_UNSAFE_BYTE_OFFSET:
		case MINT_INTRINS_GET_TYPE:
		case MINT_INTRINS_MEMORYMARSHAL_GETARRAYDATAREF:
		case MINT_CASTCLASS:
		case MINT_CASTCLASS_COMMON:
		case MINT_CASTCLASS_INTERFACE:
		case MINT_ISINST:
		case MINT_ISINST_COMMON:
		case MINT_ISINST_INTERFACE:
		case MINT_BOX:
		case MINT_BOX_VT:
		case MINT_UNBOX:
		case MINT_NEWOBJ_INLINED:
		case MINT_NEWOBJ_VT_INLINED:
		case MINT_LD_DELEGATE_METHOD_PTR:
		case MINT_LDTSFLDA:
			return TRACE_CONTINUE;

		case MINT_BR:
		case MINT_BR_S:
			if (*inside_branch_block)
				return TRACE_CONTINUE;

			return TRACE_ABORT;

		case MINT_THROW:
		case MINT_LEAVE:
		case MINT_LEAVE_S:
			if (*inside_branch_block)
				return TRACE_CONTINUE;

			return TRACE_ABORT;

		case MINT_LEAVE_CHECK:
		case MINT_LEAVE_S_CHECK:
			return TRACE_ABORT;

		case MINT_CALL_HANDLER:
		case MINT_CALL_HANDLER_S:
		case MINT_ENDFINALLY:
		case MINT_RETHROW:
		case MINT_MONO_RETHROW:
		case MINT_PROF_EXIT:
		case MINT_PROF_EXIT_VOID:
		case MINT_SAFEPOINT:
			return TRACE_ABORT;

		default:
		if (
			// branches
			// FIXME: some of these abort traces because the trace compiler doesn't
			//  implement them, but they are rare
			(opcode >= MINT_BRFALSE_I4) &&
			(opcode <= MINT_BLT_UN_I8_IMM_SP)
		) {
			*inside_branch_block = TRUE;
			return TRACE_CONTINUE;
		}
		else if (
			// calls
			// FIXME: many of these abort traces unconditionally because the trace
			//  compiler doesn't implement them, but that's fixable
			(opcode >= MINT_CALL) &&
			(opcode <= MINT_CALLI_NAT_FAST)
			// (opcode <= MINT_JIT_CALL2)
		)
			return *inside_branch_block ? TRACE_CONTINUE : TRACE_ABORT;
		else if (
			// returns
			(opcode >= MINT_RET) &&
			(opcode <= MINT_RET_U2)
		)
			return *inside_branch_block ? TRACE_CONTINUE : TRACE_ABORT;
		else if (
			(opcode >= MINT_LDC_I4_M1) &&
			(opcode <= MINT_LDC_R8)
		)
			return TRACE_CONTINUE;
		else if (
			(opcode >= MINT_MOV_SRC_OFF) &&
			(opcode <= MINT_MOV_8_4)
		)
			return TRACE_CONTINUE;
		else if (
			// binops
			(opcode >= MINT_ADD_I4) &&
			(opcode <= MINT_CLT_UN_R8)
		)
			return TRACE_CONTINUE;
		else if (
			// unops and some superinsns
			// fixme: a lot of these aren't actually implemented. but they're also uncommon
			(opcode >= MINT_ADD1_I4) &&
			(opcode <= MINT_SHR_I8_IMM)
		)
			return TRACE_CONTINUE;
		else if (
			// math intrinsics
			(opcode >= MINT_ASIN) &&
			(opcode <= MINT_MAXF)
		)
			return TRACE_CONTINUE;
		else if (
			// field operations
			// the trace compiler currently implements most, but not all of these
			(opcode >= MINT_LDFLD_I1) &&
			(opcode <= MINT_LDTSFLDA)
		)
			return TRACE_CONTINUE;
		else if (
			// indirect operations
			// there are also a few of these not implemented by the trace compiler yet
			(opcode >= MINT_LDLOCA_S) &&
			(opcode <= MINT_STIND_OFFSET_IMM_I8)
		)
			return TRACE_CONTINUE;
		else if (
			// array operations
			// some of these like the _I ones aren't implemented yet but are rare
			(opcode >= MINT_LDELEM_I) &&
			(opcode <= MINT_GETITEM_LOCALSPAN)
		)
			return TRACE_CONTINUE;
		else
			return TRACE_ABORT;
	}
}

static gboolean
should_generate_trace_here (InterpBasicBlock *bb, InterpInst *last_ins) {
	int current_trace_length = 0;
	// A preceding trace may have been in a branch block, but we only care whether the current
	//  trace will have a branch block opened, because that determines whether calls and branches
	//  will unconditionally abort the trace or not.
	gboolean inside_branch_block = FALSE;

	// We scan forward through the entire method body starting from the current block, not just
	//  the current block (since the actual trace compiler doesn't know about block boundaries).
	for (InterpInst *ins = bb->first_ins; (ins != NULL) && (ins != last_ins); ins = ins->next) {
		int category = jiterp_should_abort_trace(ins, &inside_branch_block);
		switch (category) {
			case TRACE_ABORT: {
				jiterpreter_abort_counts[ins->opcode]++;
				return current_trace_length >= mono_opt_jiterpreter_minimum_trace_length;
			}
			case TRACE_IGNORE:
				break;
			default:
				current_trace_length++;
				break;
		}

		// Once we know the trace is long enough we can stop scanning.
		if (current_trace_length >= mono_opt_jiterpreter_minimum_trace_length)
			return TRUE;
	}

	return FALSE;
}

/*
 * Insert jiterpreter entry points at the correct candidate locations:
 * The first basic block of the function,
 * Backward branch targets (if enabled),
 * The next basic block after a call instruction (if enabled)
 * To determine whether it is appropriate to insert an entry point at a given candidate location
 *  we have to scan through all the instructions to estimate whether it is possible to generate
 *  a suitably large trace. If it's not, we should avoid the overhead of the jiterpreter nop
 *  instruction that would end up there instead and not waste any resources trying to compile it.
 */
void
jiterp_insert_entry_points (void *_td)
{
	if (!mono_opt_jiterpreter_traces_enabled)
		return;
	TransformData *td = (TransformData *)_td;

	// Insert an entry opcode for the next basic block (call resume and first bb)
	// FIXME: Should we do this based on relationships between BBs instead of insn sequence?
	gboolean enter_at_next = TRUE;

	for (InterpBasicBlock *bb = td->entry_bb; bb != NULL; bb = bb->next_bb) {
		// Enter trace at top of functions
		gboolean is_backwards_branch = FALSE,
			is_resume_or_first = enter_at_next;

		// If backwards branches target a block, enter a trace there so that
		//  after the backward branch we can re-enter jitted code
		if (mono_opt_jiterpreter_backward_branch_entries_enabled && bb->backwards_branch_target)
			is_backwards_branch = TRUE;

		gboolean enabled = (is_backwards_branch || is_resume_or_first);
		// FIXME: This scan will likely proceed forward all the way out of the current block,
		//  which means that for large methods we will sometimes scan the same instruction
		//  multiple times and waste some work. At present this is unavoidable because
		//  control flow means we can end up with two traces covering different subsets
		//  of the same method in order to handle loops and resuming
		gboolean should_generate = enabled && should_generate_trace_here(bb, td->last_ins);

		if (mono_opt_jiterpreter_call_resume_enabled && bb->contains_call_instruction)
			enter_at_next = TRUE;

		if (mono_opt_jiterpreter_always_generate)
			should_generate = TRUE;

		if (enabled && should_generate) {
			td->cbb = bb;
			mono_jiterp_insert_ins (td, NULL, MINT_TIER_PREPARE_JITERPRETER);
			// Note that we only clear enter_at_next here, after generating a trace.
			// This means that the flag will stay set intentionally if we keep failing
			//  to generate traces, perhaps due to a string of small basic blocks
			//  or multiple call instructions.
			enter_at_next = bb->contains_call_instruction;
		}
	}
}

// Used to parse runtime options that control the jiterpreter. This is *also* used at runtime
//  by the jiterpreter typescript to reconfigure the jiterpreter, for example if WASM EH is not
//  actually available even though it was enabled (to turn it off).
EMSCRIPTEN_KEEPALIVE gboolean
mono_jiterp_parse_option (const char *option)
{
	if (!option || (*option == 0))
		return FALSE;

	const char *arr[2] = { option, NULL };
	int temp;
	mono_options_parse_options (arr, 1, &temp, NULL);
	return TRUE;
}

// When jiterpreter options change we increment this version so that the typescript knows
//  it will have to re-query all the option values
EMSCRIPTEN_KEEPALIVE gint32
mono_jiterp_get_options_version ()
{
	return mono_options_version;
}

EMSCRIPTEN_KEEPALIVE char *
mono_jiterp_get_options_as_json ()
{
	return mono_options_get_as_json ();
}

EMSCRIPTEN_KEEPALIVE void
mono_jiterp_update_jit_call_dispatcher (WasmDoJitCall dispatcher)
{
	// If we received a 0 dispatcher that means the TS side failed to compile
	//  any kind of dispatcher - this likely indicates that content security policy
	//  blocked the use of Module.addFunction
	if (!dispatcher)
		dispatcher = (WasmDoJitCall)mono_llvm_cpp_catch_exception;
	jiterpreter_do_jit_call = dispatcher;
}

// HACK: fix C4206
EMSCRIPTEN_KEEPALIVE
#endif

void jiterp_preserve_module () {
}
