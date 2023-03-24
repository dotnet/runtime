// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// This file contains icalls used in jitted interpreter traces and wrappers,
//  along with infrastructure to support code generration

#ifndef __USE_ISOC99
#define __USE_ISOC99
#endif
#include "config.h"

void jiterp_preserve_module (void);

// NOTE: All code in this file needs to be guarded with HOST_BROWSER, since
//  we don't run non-wasm tests for changes to this file!

#if HOST_BROWSER

#if 0
#define jiterp_assert(b) g_assert(b)
#else
#define jiterp_assert(b)
#endif

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

#include <mono/utils/mono-math.h>
#include <mono/mini/mini.h>
#include <mono/mini/mini-runtime.h>
#include <mono/mini/aot-runtime.h>
#include <mono/mini/llvm-runtime.h>
#include <mono/mini/llvmonly-runtime.h>
#include <mono/utils/options.h>
#include <mono/utils/atomic.h>

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

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_encode_leb_signed_boundary (unsigned char * destination, int bits, int sign) {
	if (!destination)
		return 0;

	int64_t value;
	switch (bits) {
		case 32:
			value = sign >= 0 ? INT_MAX : INT_MIN;
			break;
		case 64:
			value = sign >= 0 ? INT64_MAX : INT64_MIN;
			break;
		default:
			return 0;
	}

	return mono_jiterp_encode_leb64_ref(destination, &value, TRUE);
}

// Many of the following functions implement various opcodes or provide support for opcodes
//  so that jiterpreter traces don't have to inline dozens of wasm instructions worth of
//  complex logic - these are designed to match interp.c

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
mono_jiterp_try_newstr (MonoString **destination, int length) {
	ERROR_DECL(error);
	*destination = mono_string_new_size_checked(length, error);
	if (!is_ok (error))
		*destination = 0;
	mono_error_cleanup (error); // FIXME: do not swallow the error
	return *destination != 0;
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

EMSCRIPTEN_KEEPALIVE void
mono_jiterp_localloc (gpointer *destination, gint32 len, InterpFrame *frame)
{
	ThreadContext *context = mono_jiterp_get_context();
	gpointer mem;
	if (len > 0) {
		mem = mono_jiterp_frame_data_allocator_alloc (&context->data_stack, frame, ALIGN_TO (len, sizeof (gint64)));

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
mono_jiterp_conv (void *dest, void *src, int opcode) {
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

		case MINT_CONV_OVF_I8_R8:
		case MINT_CONV_OVF_I8_R4: {
			double val;
			if (opcode == MINT_CONV_OVF_I8_R4)
				val = *(float*)src;
			else
				val = *(double*)src;

			return mono_try_trunc_i64(val, dest);
		}
	}

	// TODO: return 0 on success and a unique bailout code on failure?
	// Probably not necessary right now and would bloat traces slightly
	return 0;
}

#define JITERP_CNE_UN_R4 (0xFFFF + 0)
#define JITERP_CGE_UN_R4 (0xFFFF + 1)
#define JITERP_CLE_UN_R4 (0xFFFF + 2)
#define JITERP_CNE_UN_R8 (0xFFFF + 3)
#define JITERP_CGE_UN_R8 (0xFFFF + 4)
#define JITERP_CLE_UN_R8 (0xFFFF + 5)


#define JITERP_RELOP(opcode, type, op, noorder) \
	case opcode: \
		{ \
			if (is_unordered) \
				return noorder; \
			else \
				return ((type)lhs op (type)rhs); \
		}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_relop_fp (double lhs, double rhs, int opcode) {
	gboolean is_unordered = mono_isunordered (lhs, rhs);
	switch (opcode) {
		JITERP_RELOP(MINT_CEQ_R4, float, ==, 0);
		JITERP_RELOP(MINT_CEQ_R8, double, ==, 0);
		JITERP_RELOP(MINT_CNE_R4, float, !=, 1);
		JITERP_RELOP(MINT_CNE_R8, double, !=, 1);
		JITERP_RELOP(JITERP_CNE_UN_R4, float, !=, 1);
		JITERP_RELOP(JITERP_CNE_UN_R8, double, !=, 1);
		JITERP_RELOP(MINT_CGT_R4, float, >, 0);
		JITERP_RELOP(MINT_CGT_R8, double, >, 0);
		JITERP_RELOP(MINT_CGE_R4, float, >=, 0);
		JITERP_RELOP(MINT_CGE_R8, double, >=, 0);
		JITERP_RELOP(JITERP_CGE_UN_R4, float, >=, 1);
		JITERP_RELOP(JITERP_CGE_UN_R8, double, >=, 1);
		JITERP_RELOP(MINT_CGT_UN_R4, float, >, 1);
		JITERP_RELOP(MINT_CGT_UN_R8, double, >, 1);
		JITERP_RELOP(MINT_CLT_R4, float, <, 0);
		JITERP_RELOP(MINT_CLT_R8, double, <, 0);
		JITERP_RELOP(MINT_CLT_UN_R4, float, <, 1);
		JITERP_RELOP(MINT_CLT_UN_R8, double, <, 1);
		JITERP_RELOP(MINT_CLE_R4, float, <=, 0);
		JITERP_RELOP(MINT_CLE_R8, double, <=, 0);
		JITERP_RELOP(JITERP_CLE_UN_R4, float, <=, 1);
		JITERP_RELOP(JITERP_CLE_UN_R8, double, <=, 1);

		default:
			g_assert_not_reached();
	}
}

#undef JITERP_RELOP

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
EMSCRIPTEN_KEEPALIVE void
mono_jiterp_trace_bailout (int reason)
{
	if (reason < 256)
		jiterp_trace_bailout_counts[reason]++;
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

// at the start of a jitted interp_entry wrapper, this is called to perform initial setup
//  like resolving the target for delegates and setting up the thread context
// inlining this into the wrappers would make them unnecessarily big and complex
EMSCRIPTEN_KEEPALIVE stackval *
mono_jiterp_interp_entry_prologue (JiterpEntryData *data, void *this_arg)
{
	stackval *sp_args;
	InterpMethod *rmethod;
	ThreadContext *context;

	// unbox implemented by jit

	jiterp_assert(data);
	rmethod = data->header.rmethod;
	jiterp_assert(rmethod);

	// Is this method MulticastDelegate.Invoke?
	if (rmethod->is_invoke) {
		// Copy the current state of the cache before using it
		JiterpEntryDataCache cache = data->cache;
		if (this_arg && (cache.delegate_invoke_is_for == (MonoDelegate*)this_arg)) {
			// We previously cached the invoke for this delegate
			data->header.rmethod = rmethod = cache.delegate_invoke_rmethod;
		} else {
			/*
			* This happens when AOT code for the invoke wrapper is not found.
			* Have to replace the method with the wrapper here, since the wrapper depends on the delegate.
			*/
			MonoDelegate *del = (MonoDelegate*)this_arg;
			MonoMethod *method = mono_marshal_get_delegate_invoke (rmethod->method, del);
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

EMSCRIPTEN_KEEPALIVE int32_t
mono_jiterp_cas_i32 (volatile int32_t *addr, int32_t newVal, int32_t expected)
{
	return mono_atomic_cas_i32 (addr, newVal, expected);
}

EMSCRIPTEN_KEEPALIVE void
mono_jiterp_cas_i64 (volatile int64_t *addr, int64_t *newVal, int64_t *expected, int64_t *oldVal)
{
	*oldVal= mono_atomic_cas_i64 (addr, *newVal, *expected);
}

// should_abort_trace returns one of these codes depending on the opcode and current state
#define TRACE_IGNORE -1
#define TRACE_CONTINUE 0
#define TRACE_ABORT 1
#define TRACE_CONDITIONAL_ABORT 2

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
		case MINT_LDSTR:
		case MINT_LDFTN:
		case MINT_LDFTN_ADDR:
		case MINT_LDPTR:
		case MINT_CPOBJ_VT:
		case MINT_LDOBJ_VT:
		case MINT_STOBJ_VT:
		case MINT_STOBJ_VT_NOREF:
		case MINT_CPOBJ_VT_NOREF:
		case MINT_STRLEN:
		case MINT_GETCHR:
		case MINT_GETITEM_SPAN:
		case MINT_GETITEM_LOCALSPAN:
		case MINT_INTRINS_SPAN_CTOR:
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
		case MINT_NEWSTR:
		case MINT_NEWOBJ_INLINED:
		case MINT_NEWOBJ_VT_INLINED:
		case MINT_LD_DELEGATE_METHOD_PTR:
		case MINT_LDTSFLDA:
		case MINT_SAFEPOINT:
		case MINT_INTRINS_GET_HASHCODE:
		case MINT_INTRINS_TRY_GET_HASHCODE:
		case MINT_INTRINS_RUNTIMEHELPERS_OBJECT_HAS_COMPONENT_SIZE:
		case MINT_INTRINS_ENUM_HASFLAG:
		case MINT_INTRINS_ORDINAL_IGNORE_CASE_ASCII:
		case MINT_ADD_MUL_I4_IMM:
		case MINT_ADD_MUL_I8_IMM:
		case MINT_ARRAY_RANK:
		case MINT_ARRAY_ELEMENT_SIZE:
		case MINT_MONO_CMPXCHG_I4:
		case MINT_MONO_CMPXCHG_I8:
		case MINT_CPBLK:
		case MINT_INITBLK:
		case MINT_ROL_I4_IMM:
		case MINT_ROL_I8_IMM:
		case MINT_ROR_I4_IMM:
		case MINT_ROR_I8_IMM:
		case MINT_CLZ_I4:
		case MINT_CTZ_I4:
		case MINT_POPCNT_I4:
		case MINT_CLZ_I8:
		case MINT_CTZ_I8:
		case MINT_POPCNT_I8:
			return TRACE_CONTINUE;

		case MINT_BR:
		case MINT_BR_S:
		case MINT_LEAVE:
		case MINT_LEAVE_S:
		case MINT_CALL_HANDLER:
		case MINT_CALL_HANDLER_S:
			// Detect backwards branches
			if (ins->info.target_bb->il_offset <= ins->il_offset) {
				if (*inside_branch_block)
					return TRACE_CONDITIONAL_ABORT;
				else
					return mono_opt_jiterpreter_backward_branches_enabled ? TRACE_CONTINUE : TRACE_ABORT;
			}

			*inside_branch_block = TRUE;
			return TRACE_CONTINUE;

		case MINT_ICALL_V_P:
		case MINT_ICALL_V_V:
		case MINT_ICALL_P_P:
		case MINT_ICALL_P_V:
		case MINT_ICALL_PP_V:
		case MINT_ICALL_PP_P:
		case MINT_MONO_RETHROW:
		case MINT_THROW:
			if (*inside_branch_block)
				return TRACE_CONDITIONAL_ABORT;

			return TRACE_ABORT;

		case MINT_LEAVE_CHECK:
		case MINT_LEAVE_S_CHECK:
			return TRACE_ABORT;

		case MINT_ENDFINALLY:
		case MINT_RETHROW:
		case MINT_PROF_EXIT:
		case MINT_PROF_EXIT_VOID:
			return TRACE_ABORT;

		case MINT_MOV_SRC_OFF:
		case MINT_MOV_DST_OFF:
			// These opcodes will turn into supported MOVs later
			return TRACE_CONTINUE;

		default:
		if (
			// branches
			// FIXME: some of these abort traces because the trace compiler doesn't
			//  implement them, but they are rare
			(opcode >= MINT_BRFALSE_I4) &&
			(opcode <= MINT_BLT_UN_I8_IMM_SP)
		) {
			// FIXME: Detect negative displacement and abort appropriately
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
			return *inside_branch_block ? TRACE_CONDITIONAL_ABORT : TRACE_ABORT;
		else if (
			// returns
			(opcode >= MINT_RET) &&
			(opcode <= MINT_RET_U2)
		)
			return *inside_branch_block ? TRACE_CONDITIONAL_ABORT : TRACE_ABORT;
		else if (
			(opcode >= MINT_LDC_I4_M1) &&
			(opcode <= MINT_LDC_R8)
		)
			return TRACE_CONTINUE;
		else if (
			(opcode >= MINT_MOV_I4_I1) &&
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
			// math intrinsics - we implement most but not all of these
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
			(opcode >= MINT_LDELEM_I1) &&
			(opcode <= MINT_GETITEM_LOCALSPAN)
		)
			return TRACE_CONTINUE;
		else
			return TRACE_ABORT;
	}
}

static gboolean
should_generate_trace_here (InterpBasicBlock *bb) {
	int current_trace_length = 0;
	// A preceding trace may have been in a branch block, but we only care whether the current
	//  trace will have a branch block opened, because that determines whether calls and branches
	//  will unconditionally abort the trace or not.
	gboolean inside_branch_block = FALSE;

	while (bb) {
		// We scan forward through the entire method body starting from the current block, not just
		//  the current block (since the actual trace compiler doesn't know about block boundaries).
		for (InterpInst *ins = bb->first_ins; ins != NULL; ins = ins->next) {
			int category = jiterp_should_abort_trace(ins, &inside_branch_block);
			switch (category) {
				case TRACE_ABORT:
					jiterpreter_abort_counts[ins->opcode]++;
					return current_trace_length >= mono_opt_jiterpreter_minimum_trace_length;
				case TRACE_CONDITIONAL_ABORT:
					// FIXME: Stop traces that contain these early on, as long as we are relatively certain
					//  that these instructions will be hit (i.e. they are not unlikely branches)
					break;
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

		bb = bb->next_bb;
	}

	return FALSE;
}

typedef struct {
	// 64-bits because it can get very high if estimate heat is turned on
	gint64 hit_count;
	JiterpreterThunk thunk;
	int penalty_total;
} TraceInfo;

// The maximum number of trace segments used to store TraceInfo. This limits
//  the maximum total number of traces to MAX_TRACE_SEGMENTS * TRACE_SEGMENT_SIZE
#define MAX_TRACE_SEGMENTS 1024
#define TRACE_SEGMENT_SIZE 1024

static volatile gint32 trace_count = 0;
static TraceInfo *trace_segments[MAX_TRACE_SEGMENTS] = { NULL };
static gint32 traces_rejected = 0;

static TraceInfo *
trace_info_allocate_segment (gint32 index) {
	g_assert (index < MAX_TRACE_SEGMENTS);

	volatile gpointer *slot = (volatile gpointer *)&trace_segments[index];
	gpointer segment = g_malloc0 (sizeof(TraceInfo) * TRACE_SEGMENT_SIZE);
	gpointer result = mono_atomic_cas_ptr (slot, segment, NULL);
	if (result != NULL) {
		g_free (segment);
		return (TraceInfo *)result;
	} else {
		return (TraceInfo *)segment;
	}
}

static TraceInfo *
trace_info_get (gint32 index) {
	g_assert (index >= 0);
	int segment_index = index / TRACE_SEGMENT_SIZE,
		element_index = index % TRACE_SEGMENT_SIZE;

	g_assert (segment_index < MAX_TRACE_SEGMENTS);

	TraceInfo *segment = trace_segments[segment_index];
	if (!segment)
		segment = trace_info_allocate_segment (segment_index);

	return &segment[element_index];
}

static gint32
trace_info_alloc () {
	gint32 index = trace_count++,
		limit = (MAX_TRACE_SEGMENTS * TRACE_SEGMENT_SIZE);
	// Make sure we're not out of space in the trace info table.
	if (index == limit)
		g_print ("MONO_WASM: Reached maximum number of jiterpreter trace entry points (%d).\n", limit);
	if (index >= limit)
		return -1;

	TraceInfo *info = trace_info_get (index);
	info->hit_count = 0;
	info->thunk = NULL;
	return index;
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
jiterp_insert_entry_points (void *_imethod, void *_td)
{
	InterpMethod *imethod = (InterpMethod *)_imethod;
	TransformData *td = (TransformData *)_td;
	// Insert an entry opcode for the next basic block (call resume and first bb)
	// FIXME: Should we do this based on relationships between BBs instead of insn sequence?
	gboolean enter_at_next = TRUE;

	if (!mono_opt_jiterpreter_traces_enabled)
		return;

	// Start with a high instruction counter so the distance check will pass
	int instruction_count = mono_opt_jiterpreter_minimum_distance_between_traces;

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
		gboolean should_generate = enabled &&
		// Only insert a trace if the heuristic says this location will likely produce a long
		//  enough one to be worth it
			should_generate_trace_here(bb) &&
		// And don't insert another trace if we inserted one too recently, unless this
		//  is a backwards branch target
			(
				(instruction_count >= mono_opt_jiterpreter_minimum_distance_between_traces) ||
				is_backwards_branch
			);

		if (mono_opt_jiterpreter_call_resume_enabled && bb->contains_call_instruction)
			enter_at_next = TRUE;

		if (mono_opt_jiterpreter_disable_heuristic)
			should_generate = TRUE;

		if (enabled && should_generate) {
			gint32 trace_index = trace_info_alloc ();
			if (trace_index < 0) {
				// We're out of space in the TraceInfo table.
				return;
			} else {
				td->cbb = bb;
				imethod->contains_traces = TRUE;
				InterpInst *ins = mono_jiterp_insert_ins (td, NULL, MINT_TIER_PREPARE_JITERPRETER);
				memcpy(ins->data, &trace_index, sizeof (trace_index));

				// Clear the instruction counter
				instruction_count = 0;

				// Note that we only clear enter_at_next here, after generating a trace.
				// This means that the flag will stay set intentionally if we keep failing
				//  to generate traces, perhaps due to a string of small basic blocks
				//  or multiple call instructions.
				enter_at_next = bb->contains_call_instruction;
			}
		} else if (is_backwards_branch && enabled && !should_generate) {
			// We failed to start a trace at a backwards branch target, but that might just mean
			//  that the loop body starts with one or two unsupported opcodes, so it may be
			//  worthwhile to try again later
			// FIXME: This caused a bunch of regressions
			// enter_at_next = TRUE;
		}

		// Increase the instruction counter. If we inserted an entry point at the top of this bb,
		//  the new instruction counter will be the number of instructions in the block, so if
		//  it's big enough we'll be able to insert another entry point right away.
		instruction_count += bb->in_count;
	}
}

EMSCRIPTEN_KEEPALIVE double
mono_jiterp_get_trace_hit_count (gint32 trace_index) {
	return trace_info_get (trace_index)->hit_count;
}

JiterpreterThunk
mono_interp_tier_prepare_jiterpreter_fast (
	void *frame, MonoMethod *method, const guint16 *ip,
	const guint16 *start_of_body, int size_of_body
) {
	if (!mono_opt_jiterpreter_traces_enabled)
		return (JiterpreterThunk)(void*)JITERPRETER_NOT_JITTED;

	guint32 trace_index = READ32 (ip + 1);
	TraceInfo *trace_info = trace_info_get (trace_index);
	g_assert (trace_info);

	if (trace_info->thunk)
		return trace_info->thunk;

#ifdef DISABLE_THREADS
	gint64 count = trace_info->hit_count++;
#else
	gint64 count = mono_atomic_inc_i64(&trace_info->hit_count);
#endif

	if (count == mono_opt_jiterpreter_minimum_trace_hit_count) {
		JiterpreterThunk result = mono_interp_tier_prepare_jiterpreter(
			frame, method, ip, (gint32)trace_index,
			start_of_body, size_of_body
		);
		trace_info->thunk = result;
		return result;
	} else {
		// Hit count not reached, or already reached but compilation is not done yet
		return (JiterpreterThunk)(void*)JITERPRETER_TRAINING;
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
	else if (((int)(void*)dispatcher)==-1)
		dispatcher = mono_jiterp_do_jit_call_indirect;

	jiterpreter_do_jit_call = dispatcher;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_object_has_component_size (MonoObject ** ppObj)
{
	MonoObject *obj = *ppObj;
	if (!obj)
		return 0;
	return (obj->vtable->flags & MONO_VT_FLAG_ARRAY_OR_STRING) != 0;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_get_hashcode (MonoObject ** ppObj)
{
	MonoObject *obj = *ppObj;
	return mono_object_hash_internal (obj);
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_try_get_hashcode (MonoObject ** ppObj)
{
	MonoObject *obj = *ppObj;
	return mono_object_try_get_hash_internal (obj);
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_get_signature_has_this (MonoMethodSignature *sig)
{
	return sig->hasthis;
}

EMSCRIPTEN_KEEPALIVE MonoType *
mono_jiterp_get_signature_return_type (MonoMethodSignature *sig)
{
	return sig->ret;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_get_signature_param_count (MonoMethodSignature *sig)
{
	return sig->param_count;
}

EMSCRIPTEN_KEEPALIVE MonoType **
mono_jiterp_get_signature_params (MonoMethodSignature *sig)
{
	return sig->params;
}

#define DUMMY_BYREF 0xFFFF

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_type_to_ldind (MonoType *type)
{
	if (!type)
		return 0;
	if (m_type_is_byref(type))
		return DUMMY_BYREF;
	return mono_type_to_ldind (type);
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_type_to_stind (MonoType *type)
{
	if (!type)
		return 0;
	if (m_type_is_byref(type))
		return 0;
	return mono_type_to_stind (type);
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_get_array_rank (gint32 *dest, MonoObject **src)
{
	if (!src || !*src) {
		*dest = 0;
		return 0;
	}

	*dest = m_class_get_rank (mono_object_class (*src));
	return 1;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_get_array_element_size (gint32 *dest, MonoObject **src)
{
	if (!src || !*src) {
		*dest = 0;
		return 0;
	}

	*dest = mono_array_element_size (mono_object_class (*src));
	return 1;
}

// Returns 1 on success so that the trace can do br_if to bypass its bailout
EMSCRIPTEN_KEEPALIVE int
mono_jiterp_set_object_field (
	uint8_t *locals, guint32 fieldOffsetBytes,
	guint32 targetLocalOffsetBytes, guint32 sourceLocalOffsetBytes
) {
	MonoObject * targetObject = *(MonoObject **)(locals + targetLocalOffsetBytes);
	if (!targetObject)
		return 0;
	MonoObject ** target = (MonoObject **)(((uint8_t *)targetObject) + fieldOffsetBytes);
	mono_gc_wbarrier_set_field_internal (
		targetObject, target,
		*(MonoObject **)(locals + sourceLocalOffsetBytes)
	);
	return 1;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_debug_count ()
{
	return mono_debug_count();
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_stelem_ref (
	MonoArray *o, gint32 aindex, MonoObject *ref
) {
	if (!o)
		return 0;
	if (aindex >= mono_array_length_internal (o))
		return 0;

	if (ref) {
		// FIXME push/pop LMF
		gboolean isinst = mono_jiterp_isinst (ref, m_class_get_element_class (mono_object_class (o)));
		if (!isinst)
			return 0;
	}

	mono_array_setref_fast ((MonoArray *) o, aindex, ref);
	return 1;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_trace_transfer (
	int displacement, JiterpreterThunk trace, void *frame, void *pLocals, JiterpreterCallInfo *cinfo
) {
	// This indicates that we lost a race condition, so there's no trace to call. Just bail out.
	// FIXME: Detect this at trace generation time and spin until the trace is available
	if (!trace)
		return displacement;

	// When we transfer control to a trace that represents a loop body, at the end of the loop
	//  body it may branch back to itself. In that case, we can just call it again - the
	//  safepoint was already performed by the trace.
	int relative_displacement = 0;
	while (relative_displacement == 0)
		relative_displacement = trace(frame, pLocals, cinfo);

	// We got a relative displacement other than 0, so the trace bailed out somewhere or
	//  branched to another branch target. Time to return (and our caller will return too.)
	return displacement + relative_displacement;
}

#define JITERP_MEMBER_VT_INITIALIZED 0
#define JITERP_MEMBER_ARRAY_DATA 1
#define JITERP_MEMBER_STRING_LENGTH 2
#define JITERP_MEMBER_STRING_DATA 3
#define JITERP_MEMBER_IMETHOD 4
#define JITERP_MEMBER_DATA_ITEMS 5
#define JITERP_MEMBER_RMETHOD 6
#define JITERP_MEMBER_SPAN_LENGTH 7
#define JITERP_MEMBER_SPAN_DATA 8
#define JITERP_MEMBER_ARRAY_LENGTH 9
#define JITERP_MEMBER_BACKWARD_BRANCH_OFFSETS 10
#define JITERP_MEMBER_BACKWARD_BRANCH_OFFSETS_COUNT 11
#define JITERP_MEMBER_CLAUSE_DATA_OFFSETS 12

// we use these helpers at JIT time to figure out where to do memory loads and stores
EMSCRIPTEN_KEEPALIVE size_t
mono_jiterp_get_member_offset (int member) {
	switch (member) {
		case JITERP_MEMBER_VT_INITIALIZED:
			return MONO_STRUCT_OFFSET (MonoVTable, initialized);
		case JITERP_MEMBER_ARRAY_DATA:
			return MONO_STRUCT_OFFSET (MonoArray, vector);
		case JITERP_MEMBER_ARRAY_LENGTH:
			return MONO_STRUCT_OFFSET (MonoArray, max_length);
		case JITERP_MEMBER_STRING_LENGTH:
			return MONO_STRUCT_OFFSET (MonoString, length);
		case JITERP_MEMBER_STRING_DATA:
			return MONO_STRUCT_OFFSET (MonoString, chars);
		case JITERP_MEMBER_IMETHOD:
			return offsetof (InterpFrame, imethod);
		case JITERP_MEMBER_DATA_ITEMS:
			return offsetof (InterpMethod, data_items);
		case JITERP_MEMBER_BACKWARD_BRANCH_OFFSETS:
			return offsetof (InterpMethod, backward_branch_offsets);
		case JITERP_MEMBER_BACKWARD_BRANCH_OFFSETS_COUNT:
			return offsetof (InterpMethod, backward_branch_offsets_count);
		case JITERP_MEMBER_CLAUSE_DATA_OFFSETS:
			return offsetof (InterpMethod, clause_data_offsets);
		case JITERP_MEMBER_RMETHOD:
			return offsetof (JiterpEntryDataHeader, rmethod);
		case JITERP_MEMBER_SPAN_LENGTH:
			return offsetof (MonoSpanOfVoid, _length);
		case JITERP_MEMBER_SPAN_DATA:
			return offsetof (MonoSpanOfVoid, _reference);
		default:
			g_assert_not_reached();
	}
}

#define JITERP_NUMBER_MODE_U32 0
#define JITERP_NUMBER_MODE_I32 1
#define JITERP_NUMBER_MODE_F32 2
#define JITERP_NUMBER_MODE_F64 3

EMSCRIPTEN_KEEPALIVE void
mono_jiterp_write_number_unaligned (void *dest, double value, int mode) {
	switch (mode) {
		case JITERP_NUMBER_MODE_U32:
			*((uint32_t *)dest) = (uint32_t)value;
			return;
		case JITERP_NUMBER_MODE_I32:
			*((int32_t *)dest) = (int32_t)value;
			return;
		case JITERP_NUMBER_MODE_F32:
			*((float *)dest) = (float)value;
			return;
		case JITERP_NUMBER_MODE_F64:
			*((double *)dest) = value;
			return;
		default:
			g_assert_not_reached();
	}
}

#define TRACE_PENALTY_LIMIT 200

ptrdiff_t
mono_jiterp_monitor_trace (const guint16 *ip, void *_frame, void *locals)
{
	gint32 index = READ32 (ip + 1);
	TraceInfo *info = trace_info_get (index);
	g_assert (info);

	JiterpreterThunk thunk = info->thunk;
	// FIXME: This shouldn't be possible
	g_assert (((guint32)(void *)thunk) > JITERPRETER_NOT_JITTED);

	JiterpreterCallInfo cinfo;
	cinfo.backward_branch_taken = 0;
	cinfo.bailout_opcode_count = -1;

	InterpFrame *frame = _frame;

	ptrdiff_t result = thunk (frame, locals, &cinfo);
	// If a backward branch was taken, we can treat the trace as if it successfully
	//  executed at least one time. We don't know how long it actually ran, but back
	//  branches are almost always going to be loops. It's fine if a bailout happens
	//  after multiple loop iterations.
	if (
		(cinfo.bailout_opcode_count >= 0) &&
		!cinfo.backward_branch_taken &&
		(cinfo.bailout_opcode_count < mono_opt_jiterpreter_trace_monitoring_long_distance)
	) {
		// Start with a penalty of 2 and lerp all the way down to 0
		float scaled = (float)(cinfo.bailout_opcode_count - mono_opt_jiterpreter_trace_monitoring_short_distance)
			/ (mono_opt_jiterpreter_trace_monitoring_long_distance - mono_opt_jiterpreter_trace_monitoring_short_distance);
		int penalty = MIN ((int)((1.0f - scaled) * TRACE_PENALTY_LIMIT), TRACE_PENALTY_LIMIT);
		info->penalty_total += penalty;

		if (mono_opt_jiterpreter_trace_monitoring_log > 2)
			g_print ("trace #%d @%d '%s' bailout recorded at opcode #%d, penalty=%d\n", index, ip, frame->imethod->method->name, cinfo.bailout_opcode_count, penalty);
	}

	gint64 hit_count = info->hit_count++ - mono_opt_jiterpreter_minimum_trace_hit_count;
	if (hit_count == mono_opt_jiterpreter_trace_monitoring_period) {
		// Prepare to enable the trace
		volatile guint16 *mutable_ip = (volatile guint16*)ip;
		*mutable_ip = MINT_TIER_NOP_JITERPRETER;

		mono_memory_barrier ();
		float average_penalty = info->penalty_total / (float)hit_count / 100.0f,
			threshold = (mono_opt_jiterpreter_trace_monitoring_max_average_penalty / 100.0f);

		if (average_penalty <= threshold) {
			*(volatile JiterpreterThunk*)(ip + 1) = thunk;
			mono_memory_barrier ();
			*mutable_ip = MINT_TIER_ENTER_JITERPRETER;
			if (mono_opt_jiterpreter_trace_monitoring_log > 1)
				g_print ("trace #%d @%d '%s' accepted; average_penalty %f <= %f\n", index, ip, frame->imethod->method->name, average_penalty, threshold);
		} else {
			traces_rejected++;
			if (mono_opt_jiterpreter_trace_monitoring_log > 0) {
				char * full_name = mono_method_get_full_name (frame->imethod->method);
				g_print ("trace #%d @%d '%s' rejected; average_penalty %f > %f\n", index, ip, full_name, average_penalty, threshold);
				g_free (full_name);
			}
		}
	}

	return result;
}

EMSCRIPTEN_KEEPALIVE gint32
mono_jiterp_get_rejected_trace_count ()
{
	return traces_rejected;
}

// HACK: fix C4206
EMSCRIPTEN_KEEPALIVE
#endif // HOST_BROWSER

void jiterp_preserve_module () {
}
