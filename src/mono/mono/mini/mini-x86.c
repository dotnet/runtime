/**
 * \file
 * x86 backend for the Mono code generator
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Patrik Torstensson
 *
 * Copyright 2003 Ximian, Inc.
 * Copyright 2003-2011 Novell Inc.
 * Copyright 2011 Xamarin Inc.
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include "mini.h"
#include <string.h>
#include <math.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

#include <mono/metadata/abi-details.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/tokentype.h>
#include <mono/utils/mono-math.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/utils/mono-hwcap.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/unlocked.h>

#include "mini-x86.h"
#include "cpu-x86.h"
#include "ir-emit.h"
#include "mini-gc.h"
#include "aot-runtime.h"
#include "mini-runtime.h"

MONO_DISABLE_WARNING(4127) /* conditional expression is constant */

static GENERATE_TRY_GET_CLASS_WITH_CACHE (math, "System", "Math")


/* The single step trampoline */
static gpointer ss_trampoline;

/* The breakpoint trampoline */
static gpointer bp_trampoline;

#define ARGS_OFFSET 8

#ifdef TARGET_WIN32
/* Under windows, the default pinvoke calling convention is stdcall */
#define CALLCONV_IS_STDCALL(sig) ((sig)->pinvoke && ((sig)->call_convention == MONO_CALL_STDCALL || (sig)->call_convention == MONO_CALL_DEFAULT || (sig)->call_convention == MONO_CALL_THISCALL))
#else
#define CALLCONV_IS_STDCALL(sig) ((sig)->pinvoke && ((sig)->call_convention == MONO_CALL_STDCALL || (sig)->call_convention == MONO_CALL_THISCALL))
#endif

#define X86_IS_CALLEE_SAVED_REG(reg) (((reg) == X86_EBX) || ((reg) == X86_EDI) || ((reg) == X86_ESI))

#define OP_SEQ_POINT_BP_OFFSET 7

const char*
mono_arch_regname (int reg)
{
	switch (reg) {
	case X86_EAX: return "%eax";
	case X86_EBX: return "%ebx";
	case X86_ECX: return "%ecx";
	case X86_EDX: return "%edx";
	case X86_ESP: return "%esp";
	case X86_EBP: return "%ebp";
	case X86_EDI: return "%edi";
	case X86_ESI: return "%esi";
	}
	return "unknown";
}

const char*
mono_arch_fregname (int reg)
{
	switch (reg) {
	case 0:
		return "%fr0";
	case 1:
		return "%fr1";
	case 2:
		return "%fr2";
	case 3:
		return "%fr3";
	case 4:
		return "%fr4";
	case 5:
		return "%fr5";
	case 6:
		return "%fr6";
	case 7:
		return "%fr7";
	default:
		return "unknown";
	}
}

const char *
mono_arch_xregname (int reg)
{
	switch (reg) {
	case 0:
		return "%xmm0";
	case 1:
		return "%xmm1";
	case 2:
		return "%xmm2";
	case 3:
		return "%xmm3";
	case 4:
		return "%xmm4";
	case 5:
		return "%xmm5";
	case 6:
		return "%xmm6";
	case 7:
		return "%xmm7";
	default:
		return "unknown";
	}
}

void
mono_x86_patch (unsigned char* code, gpointer target)
{
	mono_x86_patch_inline (code, target);
}

#define FLOAT_PARAM_REGS 0

static const guint32 thiscall_param_regs [] = { X86_ECX, X86_NREG };

static const guint32 *callconv_param_regs(MonoMethodSignature *sig)
{
	if (!sig->pinvoke)
		return NULL;

	switch (sig->call_convention) {
	case MONO_CALL_THISCALL:
		 return thiscall_param_regs;
	default:
		 return NULL;
	}
}

#if defined(TARGET_WIN32) || defined(__APPLE__) || defined(__FreeBSD__)
#define SMALL_STRUCTS_IN_REGS
static X86_Reg_No return_regs [] = { X86_EAX, X86_EDX };
#endif

static void inline
add_general (guint32 *gr, const guint32 *param_regs, guint32 *stack_size, ArgInfo *ainfo)
{
    ainfo->offset = *stack_size;

    if (!param_regs || param_regs [*gr] == X86_NREG) {
		ainfo->storage = ArgOnStack;
		ainfo->nslots = 1;
		(*stack_size) += sizeof (target_mgreg_t);
    }
    else {
		ainfo->storage = ArgInIReg;
		ainfo->reg = param_regs [*gr];
		(*gr) ++;
    }
}

static void inline
add_general_pair (guint32 *gr, const guint32 *param_regs , guint32 *stack_size, ArgInfo *ainfo)
{
	ainfo->offset = *stack_size;

	g_assert(!param_regs || param_regs[*gr] == X86_NREG);

	ainfo->storage = ArgOnStack;
	(*stack_size) += sizeof (target_mgreg_t) * 2;
	ainfo->nslots = 2;
}

static void inline
add_float (guint32 *gr, guint32 *stack_size, ArgInfo *ainfo, gboolean is_double)
{
    ainfo->offset = *stack_size;

    if (*gr >= FLOAT_PARAM_REGS) {
		ainfo->storage = ArgOnStack;
		(*stack_size) += is_double ? 8 : 4;
		ainfo->nslots = is_double ? 2 : 1;
    }
    else {
		/* A double register */
		if (is_double)
			ainfo->storage = ArgInDoubleSSEReg;
		else
			ainfo->storage = ArgInFloatSSEReg;
		ainfo->reg = *gr;
		(*gr) += 1;
    }
}


static void
add_valuetype (MonoMethodSignature *sig, ArgInfo *ainfo, MonoType *type,
	       gboolean is_return,
	       guint32 *gr, const guint32 *param_regs, guint32 *fr, guint32 *stack_size)
{
	guint32 size;
	MonoClass *klass;

	klass = mono_class_from_mono_type_internal (type);
	size = mini_type_stack_size_full (m_class_get_byval_arg (klass), NULL, sig->pinvoke && !sig->marshalling_disabled);

#if defined(TARGET_WIN32)
	/*
	* Standard C and C++ doesn't allow empty structs, empty structs will always have a size of 1 byte.
	* GCC have an extension to allow empty structs, https://gcc.gnu.org/onlinedocs/gcc/Empty-Structures.html.
	* This cause a little dilemma since runtime build using none GCC compiler will not be compatible with
	* GCC build C libraries and the other way around. On platforms where empty structs has size of 1 byte
	* it must be represented in call and cannot be dropped.
	*/
	if (size == 0 && MONO_TYPE_ISSTRUCT (type) && sig->pinvoke) {
		/* Empty structs (1 byte size) needs to be represented in a stack slot */
		ainfo->pass_empty_struct = TRUE;
		size = 1;
	}
#endif

#ifdef SMALL_STRUCTS_IN_REGS
	if (sig->pinvoke && is_return) {
		MonoMarshalType *info;

		info = mono_marshal_load_type_info (klass);
		g_assert (info);

		ainfo->pair_storage [0] = ainfo->pair_storage [1] = ArgNone;

		/* Ignore empty struct return value, if used. */
		if (info->num_fields == 0 && ainfo->pass_empty_struct) {
			ainfo->storage = ArgValuetypeInReg;
			return;
		}

		/*
		* Windows x86 ABI for returning structs of size 4 or 8 bytes (regardless of type) dictates that
		* values are passed in EDX:EAX register pairs, https://msdn.microsoft.com/en-us/library/984x0h58.aspx.
		* This is different compared to for example float or double return types (not in struct) that will be returned
		* in ST(0), https://msdn.microsoft.com/en-us/library/ha59cbfz.aspx.
		*
		* Apples OSX x86 ABI for returning structs of size 4 or 8 bytes uses a slightly different approach.
		* If a struct includes only one scalar value, it will be handled with the same rules as scalar values.
		* This means that structs with one float or double will be returned in ST(0). For more details,
		* https://developer.apple.com/library/mac/documentation/DeveloperTools/Conceptual/LowLevelABI/130-IA-32_Function_Calling_Conventions/IA32.html.
		*/
#if !defined(TARGET_WIN32)

		/* Special case structs with only a float member */
		if (info->num_fields == 1) {
			int ftype = mini_get_underlying_type (info->fields [0].field->type)->type;
			if ((info->native_size == 8) && (ftype == MONO_TYPE_R8)) {
				ainfo->storage = ArgValuetypeInReg;
				ainfo->pair_storage [0] = ArgOnDoubleFpStack;
				return;
			}
			if ((info->native_size == 4) && (ftype == MONO_TYPE_R4)) {
				ainfo->storage = ArgValuetypeInReg;
				ainfo->pair_storage [0] = ArgOnFloatFpStack;
				return;
			}
		}
#endif

		if ((info->native_size == 1) || (info->native_size == 2) || (info->native_size == 4) || (info->native_size == 8)) {
			ainfo->storage = ArgValuetypeInReg;
			ainfo->pair_storage [0] = ArgInIReg;
			ainfo->pair_regs [0] = return_regs [0];
			if (info->native_size > 4) {
				ainfo->pair_storage [1] = ArgInIReg;
				ainfo->pair_regs [1] = return_regs [1];
			}
			return;
		}
	}
#endif

	if (param_regs && param_regs [*gr] != X86_NREG && !is_return) {
		g_assert (size <= 4);
		ainfo->storage = ArgValuetypeInReg;
		ainfo->reg = param_regs [*gr];
		(*gr)++;
		return;
	}

	ainfo->offset = *stack_size;
	ainfo->storage = ArgOnStack;
	*stack_size += ALIGN_TO (size, sizeof (target_mgreg_t));
	ainfo->nslots = ALIGN_TO (size, sizeof (target_mgreg_t)) / sizeof (target_mgreg_t);
}

/*
 * get_call_info:
 *
 *  Obtain information about a call according to the calling convention.
 * For x86 ELF, see the "System V Application Binary Interface Intel386
 * Architecture Processor Supplment, Fourth Edition" document for more
 * information.
 * For x86 win32, see https://msdn.microsoft.com/en-us/library/984x0h58.aspx.
 */
static CallInfo*
get_call_info_internal (CallInfo *cinfo, MonoMethodSignature *sig)
{
	guint32 i, gr, fr, pstart;
	const guint32 *param_regs;
	MonoType *ret_type;
	int n = sig->hasthis + sig->param_count;
	guint32 stack_size = 0;
	gboolean is_pinvoke = sig->pinvoke;

	gr = 0;
	fr = 0;
	cinfo->nargs = n;

	param_regs = callconv_param_regs(sig);

	/* return value */
	{
		ret_type = mini_get_underlying_type (sig->ret);
		switch (ret_type->type) {
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_FNPTR:
		case MONO_TYPE_OBJECT:
			cinfo->ret.storage = ArgInIReg;
			cinfo->ret.reg = X86_EAX;
			break;
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
			cinfo->ret.storage = ArgInIReg;
			cinfo->ret.reg = X86_EAX;
			cinfo->ret.is_pair = TRUE;
			break;
		case MONO_TYPE_R4:
			cinfo->ret.storage = ArgOnFloatFpStack;
			break;
		case MONO_TYPE_R8:
			cinfo->ret.storage = ArgOnDoubleFpStack;
			break;
		case MONO_TYPE_GENERICINST:
			if (!mono_type_generic_inst_is_valuetype (ret_type)) {
				cinfo->ret.storage = ArgInIReg;
				cinfo->ret.reg = X86_EAX;
				break;
			}
			if (mini_is_gsharedvt_type (ret_type)) {
				cinfo->ret.storage = ArgOnStack;
				cinfo->vtype_retaddr = TRUE;
				break;
			}
			/* Fall through */
		case MONO_TYPE_VALUETYPE:
		case MONO_TYPE_TYPEDBYREF: {
			guint32 tmp_gr = 0, tmp_fr = 0, tmp_stacksize = 0;

			add_valuetype (sig, &cinfo->ret, ret_type, TRUE, &tmp_gr, NULL, &tmp_fr, &tmp_stacksize);
			if (cinfo->ret.storage == ArgOnStack) {
				cinfo->vtype_retaddr = TRUE;
				/* The caller passes the address where the value is stored */
			}
			break;
		}
		case MONO_TYPE_VAR:
		case MONO_TYPE_MVAR:
			g_assert (mini_is_gsharedvt_type (ret_type));
			cinfo->ret.storage = ArgOnStack;
			cinfo->vtype_retaddr = TRUE;
			break;
		case MONO_TYPE_VOID:
			cinfo->ret.storage = ArgNone;
			break;
		default:
			g_error ("Can't handle as return value 0x%x", ret_type->type);
		}
	}

	pstart = 0;
	/*
	 * To simplify get_this_arg_reg () and LLVM integration, emit the vret arg after
	 * the first argument, allowing 'this' to be always passed in the first arg reg.
	 * Also do this if the first argument is a reference type, since virtual calls
	 * are sometimes made using calli without sig->hasthis set, like in the delegate
	 * invoke wrappers.
	 */
	if (cinfo->vtype_retaddr && !is_pinvoke && (sig->hasthis || (sig->param_count > 0 && MONO_TYPE_IS_REFERENCE (mini_get_underlying_type (sig->params [0]))))) {
		if (sig->hasthis) {
			add_general (&gr, param_regs, &stack_size, cinfo->args + 0);
		} else {
			add_general (&gr, param_regs, &stack_size, &cinfo->args [sig->hasthis + 0]);
			pstart = 1;
		}
		cinfo->vret_arg_offset = stack_size;
		add_general (&gr, NULL, &stack_size, &cinfo->ret);
		cinfo->vret_arg_index = 1;
	} else {
		/* this */
		if (sig->hasthis)
			add_general (&gr, param_regs, &stack_size, cinfo->args + 0);

		if (cinfo->vtype_retaddr)
			add_general (&gr, NULL, &stack_size, &cinfo->ret);
	}

	if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) && (n == 0)) {
		fr = FLOAT_PARAM_REGS;

		/* Emit the signature cookie just before the implicit arguments */
		add_general (&gr, param_regs, &stack_size, &cinfo->sig_cookie);
	}

	for (i = pstart; i < sig->param_count; ++i) {
		ArgInfo *ainfo = &cinfo->args [sig->hasthis + i];
		MonoType *ptype;

		if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) && (i == sig->sentinelpos)) {
			/* We allways pass the sig cookie on the stack for simplicity */
			/*
			 * Prevent implicit arguments + the sig cookie from being passed
			 * in registers.
			 */
			fr = FLOAT_PARAM_REGS;

			/* Emit the signature cookie just before the implicit arguments */
			add_general (&gr, param_regs, &stack_size, &cinfo->sig_cookie);
		}

		if (m_type_is_byref (sig->params [i])) {
			add_general (&gr, param_regs, &stack_size, ainfo);
			continue;
		}
		ptype = mini_get_underlying_type (sig->params [i]);
		switch (ptype->type) {
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
			add_general (&gr, param_regs, &stack_size, ainfo);
			break;
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
			add_general (&gr, param_regs, &stack_size, ainfo);
			break;
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
			add_general (&gr, param_regs, &stack_size, ainfo);
			break;
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_FNPTR:
		case MONO_TYPE_OBJECT:
			add_general (&gr, param_regs, &stack_size, ainfo);
			break;
		case MONO_TYPE_GENERICINST:
			if (!mono_type_generic_inst_is_valuetype (ptype)) {
				add_general (&gr, param_regs, &stack_size, ainfo);
				break;
			}
			if (mini_is_gsharedvt_type (ptype)) {
				/* gsharedvt arguments are passed by ref */
				add_general (&gr, param_regs, &stack_size, ainfo);
				g_assert (ainfo->storage == ArgOnStack);
				ainfo->storage = ArgGSharedVt;
				break;
			}
			/* Fall through */
		case MONO_TYPE_VALUETYPE:
		case MONO_TYPE_TYPEDBYREF:
			add_valuetype (sig, ainfo, ptype, FALSE, &gr, param_regs, &fr, &stack_size);
			break;
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
			add_general_pair (&gr, param_regs, &stack_size, ainfo);
			break;
		case MONO_TYPE_R4:
			add_float (&fr, &stack_size, ainfo, FALSE);
			break;
		case MONO_TYPE_R8:
			add_float (&fr, &stack_size, ainfo, TRUE);
			break;
		case MONO_TYPE_VAR:
		case MONO_TYPE_MVAR:
			/* gsharedvt arguments are passed by ref */
			g_assert (mini_is_gsharedvt_type (ptype));
			add_general (&gr, param_regs, &stack_size, ainfo);
			g_assert (ainfo->storage == ArgOnStack);
			ainfo->storage = ArgGSharedVt;
			break;
		default:
			g_error ("unexpected type 0x%x", ptype->type);
			g_assert_not_reached ();
		}
	}

	if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) && (n > 0) && (sig->sentinelpos == sig->param_count)) {
		fr = FLOAT_PARAM_REGS;

		/* Emit the signature cookie just before the implicit arguments */
		add_general (&gr, param_regs, &stack_size, &cinfo->sig_cookie);
	}

	if (cinfo->vtype_retaddr) {
		/* if the function returns a struct on stack, the called method already does a ret $0x4 */
		cinfo->callee_stack_pop = 4;
	} else if (CALLCONV_IS_STDCALL (sig)) {
		/* Have to compensate for the stack space popped by the native callee */
		cinfo->callee_stack_pop = stack_size;
	}

	if (mono_do_x86_stack_align && (stack_size % MONO_ARCH_FRAME_ALIGNMENT) != 0) {
		cinfo->need_stack_align = TRUE;
		cinfo->stack_align_amount = MONO_ARCH_FRAME_ALIGNMENT - (stack_size % MONO_ARCH_FRAME_ALIGNMENT);
		stack_size += cinfo->stack_align_amount;
	}

	cinfo->stack_usage = stack_size;
	cinfo->reg_usage = gr;
	cinfo->freg_usage = fr;
	return cinfo;
}

static CallInfo*
get_call_info (MonoMemPool *mp, MonoMethodSignature *sig)
{
	int n = sig->hasthis + sig->param_count;
	CallInfo *cinfo;

	if (mp)
		cinfo = mono_mempool_alloc0 (mp, sizeof (CallInfo) + (sizeof (ArgInfo) * n));
	else
		cinfo = g_malloc0 (sizeof (CallInfo) + (sizeof (ArgInfo) * n));

	return get_call_info_internal (cinfo, sig);
}

static gboolean storage_in_ireg (ArgStorage storage)
{
	return (storage == ArgInIReg || storage == ArgValuetypeInReg);
}

static int
arg_need_temp (ArgInfo *ainfo)
{
	/*
	 * We always fetch the double value from the fpstack. In that case, we
	 * need to have a separate tmp that is the double value casted to float
	 */
	if (ainfo->storage == ArgOnFloatFpStack)
		return sizeof (float);
	return 0;
}

static gpointer
arg_get_storage (CallContext *ccontext, ArgInfo *ainfo)
{
	switch (ainfo->storage) {
		case ArgOnStack:
			return ccontext->stack + ainfo->offset;
		case ArgOnDoubleFpStack:
			return &ccontext->fret;
		case ArgInIReg:
			/* If pair, the storage is for EDX:EAX */
			return &ccontext->eax;
		default:
			g_error ("Arg storage type not yet supported");
        }
}

static void
arg_get_val (CallContext *ccontext, ArgInfo *ainfo, gpointer dest)
{
	g_assert (ainfo->storage == ArgOnFloatFpStack);

	*(float*) dest = (float)ccontext->fret;
}

void
mono_arch_set_native_call_context_args (CallContext *ccontext, gpointer frame, MonoMethodSignature *sig)
{
	CallInfo *cinfo = get_call_info (NULL, sig);
	const MonoEECallbacks *interp_cb = mini_get_interp_callbacks ();
	gpointer storage;
	ArgInfo *ainfo;

	memset (ccontext, 0, sizeof (CallContext));

	ccontext->stack_size = ALIGN_TO (cinfo->stack_usage, MONO_ARCH_FRAME_ALIGNMENT);
	if (ccontext->stack_size)
		ccontext->stack = (guint8*)g_calloc (1, ccontext->stack_size);

	if (sig->ret->type != MONO_TYPE_VOID) {
		ainfo = &cinfo->ret;
		if (ainfo->storage == ArgOnStack) {
			/* This is a value type return. The pointer to vt storage is pushed as first argument */
			g_assert (ainfo->offset == 0);
			g_assert (ainfo->nslots == 1);
			storage = interp_cb->frame_arg_to_storage ((MonoInterpFrameHandle)frame, sig, -1);
			*(host_mgreg_t*)ccontext->stack = (host_mgreg_t)storage;
		}
	}

	g_assert (!sig->hasthis);

	for (int i = 0; i < sig->param_count; i++) {
		ainfo = &cinfo->args [i];

		storage = arg_get_storage (ccontext, ainfo);

		interp_cb->frame_arg_to_data ((MonoInterpFrameHandle)frame, sig, i, storage);
	}

	g_free (cinfo);
}

void
mono_arch_get_native_call_context_ret (CallContext *ccontext, gpointer frame, MonoMethodSignature *sig)
{
	const MonoEECallbacks *interp_cb;
	CallInfo *cinfo;
	ArgInfo *ainfo;
	gpointer storage;

	/* No return value */
	if (sig->ret->type == MONO_TYPE_VOID)
		return;

	interp_cb = mini_get_interp_callbacks ();
	cinfo = get_call_info (NULL, sig);
	ainfo = &cinfo->ret;

	/* Check if return value was stored directly at address passed in reg */
	if (cinfo->ret.storage != ArgOnStack) {
		int temp_size = arg_need_temp (ainfo);

		if (temp_size) {
			storage = alloca (temp_size);
			arg_get_val (ccontext, ainfo, storage);
		} else {
			storage = arg_get_storage (ccontext, ainfo);
		}
		interp_cb->data_to_frame_arg ((MonoInterpFrameHandle)frame, sig, -1, storage);
	}

	g_free (cinfo);
}

/*
 * mono_arch_get_argument_info:
 * @csig:  a method signature
 * @param_count: the number of parameters to consider
 * @arg_info: an array to store the result infos
 *
 * Gathers information on parameters such as size, alignment and
 * padding. arg_info should be large enought to hold param_count + 1 entries.
 *
 * Returns the size of the argument area on the stack.
 * This should be signal safe, since it is called from
 * mono_arch_unwind_frame ().
 * FIXME: The metadata calls might not be signal safe.
 */
int
mono_arch_get_argument_info (MonoMethodSignature *csig, int param_count, MonoJitArgumentInfo *arg_info)
{
	int len, k, args_size = 0;
	int size, pad;
	guint32 align;
	int offset = 8;
	CallInfo *cinfo;
	int prev_stackarg;
	int num_regs;

	/* Avoid g_malloc as it is not signal safe */
	len = sizeof (CallInfo) + (sizeof (ArgInfo) * (csig->param_count + 1));
	cinfo = (CallInfo*)g_alloca (len);
	memset (cinfo, 0, len);

	cinfo = get_call_info_internal (cinfo, csig);

	arg_info [0].offset = offset;

	if (cinfo->vtype_retaddr && cinfo->vret_arg_index == 0) {
		args_size += sizeof (target_mgreg_t);
		offset += 4;
	}

	if (csig->hasthis && !storage_in_ireg (cinfo->args [0].storage)) {
		args_size += sizeof (target_mgreg_t);
		offset += 4;
	}

	if (cinfo->vtype_retaddr && cinfo->vret_arg_index == 1 && csig->hasthis) {
		/* Emitted after this */
		args_size += sizeof (target_mgreg_t);
		offset += 4;
	}

	arg_info [0].size = args_size;
	prev_stackarg = 0;

	for (k = 0; k < param_count; k++) {
		size = mini_type_stack_size_full (csig->params [k], &align, csig->pinvoke && !csig->marshalling_disabled);

		if (storage_in_ireg (cinfo->args [csig->hasthis + k].storage)) {
			/* not in stack, we'll give it an offset at the end */
			arg_info [k + 1].pad = 0;
			arg_info [k + 1].size = size;
		} else {
			/* ignore alignment for now */
			align = 1;

			args_size += pad = (align - (args_size & (align - 1))) & (align - 1);
			arg_info [prev_stackarg].pad = pad;
			args_size += size;
			arg_info [k + 1].pad = 0;
			arg_info [k + 1].size = size;
			offset += pad;
			arg_info [k + 1].offset = offset;
			offset += size;
			prev_stackarg = k + 1;
		}

		if (k == 0 && cinfo->vtype_retaddr && cinfo->vret_arg_index == 1 && !csig->hasthis) {
			/* Emitted after the first arg */
			args_size += sizeof (target_mgreg_t);
			offset += 4;
		}
	}

	if (mono_do_x86_stack_align && !CALLCONV_IS_STDCALL (csig))
		align = MONO_ARCH_FRAME_ALIGNMENT;
	else
		align = 4;
	args_size += pad = (align - (args_size & (align - 1))) & (align - 1);
	arg_info [k].pad = pad;

	/* Add offsets for any reg parameters */
	num_regs = 0;
	if (csig->hasthis && storage_in_ireg (cinfo->args [0].storage))
		arg_info [0].offset = args_size + 4 * num_regs++;
	for (k=0; k < param_count; k++) {
		if (storage_in_ireg (cinfo->args[csig->hasthis + k].storage)) {
			arg_info [k + 1].offset = args_size + 4 * num_regs++;
		}
	}

	return args_size;
}

#ifndef DISABLE_JIT

gboolean
mono_arch_tailcall_supported (MonoCompile *cfg, MonoMethodSignature *caller_sig, MonoMethodSignature *callee_sig, gboolean virtual_)
{
	g_assert (caller_sig);
	g_assert (callee_sig);

	// Direct AOT calls usually go through the PLT/GOT.
	//   Unless we can determine here if is_direct_callable will return TRUE?
	// But the PLT/GOT is addressed with nonvolatile ebx, which
	// gets restored before the jump.
	// See https://github.com/mono/mono/commit/f5373adc8a89d4b0d1d549fdd6d9adc3ded4b400
	// See https://github.com/mono/mono/issues/11265
	if (!virtual_ && cfg->compile_aot && !cfg->full_aot)
		return FALSE;

	CallInfo *caller_info = get_call_info (NULL, caller_sig);
	CallInfo *callee_info = get_call_info (NULL, callee_sig);

	/*
	 * Tailcalls with more callee stack usage than the caller cannot be supported, since
	 * the extra stack space would be left on the stack after the tailcall.
	 */
	gboolean res = IS_SUPPORTED_TAILCALL (callee_info->stack_usage <= caller_info->stack_usage)
				&& IS_SUPPORTED_TAILCALL (caller_info->ret.storage == callee_info->ret.storage);
	if (!res && !mono_tailcall_print_enabled ())
		goto exit;

	// Limit stack_usage to 1G.
	res &= IS_SUPPORTED_TAILCALL (callee_info->stack_usage < (1 << 30));
	res &= IS_SUPPORTED_TAILCALL (caller_info->stack_usage < (1 << 30));

exit:
	g_free (caller_info);
	g_free (callee_info);

	return res;
}

#endif

/*
 * Initialize the cpu to execute managed code.
 */
void
mono_arch_cpu_init (void)
{
	/* spec compliance requires running with double precision */
#ifndef _MSC_VER
	guint16 fpcw;

	__asm__  __volatile__ ("fnstcw %0\n": "=m" (fpcw));
	fpcw &= ~X86_FPCW_PRECC_MASK;
	fpcw |= X86_FPCW_PREC_DOUBLE;
	__asm__  __volatile__ ("fldcw %0\n": : "m" (fpcw));
	__asm__  __volatile__ ("fnstcw %0\n": "=m" (fpcw));
#else
	_control87 (_PC_53, MCW_PC);
#endif
}

/*
 * Initialize architecture specific code.
 */
void
mono_arch_init (void)
{
	if (!mono_aot_only)
		bp_trampoline = mini_get_breakpoint_trampoline ();
}

/*
 * Cleanup architecture specific code.
 */
void
mono_arch_cleanup (void)
{
}

/*
 * This function returns the optimizations supported on this cpu.
 */
guint32
mono_arch_cpu_optimizations (guint32 *exclude_mask)
{
	guint32 opts = 0;

	*exclude_mask = 0;

	if (mono_hwcap_x86_has_cmov) {
		opts |= MONO_OPT_CMOV;

		if (mono_hwcap_x86_has_fcmov)
			opts |= MONO_OPT_FCMOV;
		else
			*exclude_mask |= MONO_OPT_FCMOV;
	} else {
		*exclude_mask |= MONO_OPT_CMOV;
	}

	if (mono_hwcap_x86_has_sse2)
		opts |= MONO_OPT_SSE2;
	else
		*exclude_mask |= MONO_OPT_SSE2;

#ifdef MONO_ARCH_SIMD_INTRINSICS
		/*SIMD intrinsics require at least SSE2.*/
		if (!mono_hwcap_x86_has_sse2)
			*exclude_mask |= MONO_OPT_SIMD;
#endif

	return opts;
}

MonoCPUFeatures
mono_arch_get_cpu_features (void)
{
	guint64 features = MONO_CPU_INITED;

	if (mono_hwcap_x86_has_sse1)
		features |= MONO_CPU_X86_SSE;

	if (mono_hwcap_x86_has_sse2)
		features |= MONO_CPU_X86_SSE2;

	if (mono_hwcap_x86_has_sse3)
		features |= MONO_CPU_X86_SSE3;

	if (mono_hwcap_x86_has_ssse3)
		features |= MONO_CPU_X86_SSSE3;

	if (mono_hwcap_x86_has_sse41)
		features |= MONO_CPU_X86_SSE41;

	if (mono_hwcap_x86_has_sse42)
		features |= MONO_CPU_X86_SSE42;

	return (MonoCPUFeatures)features;
}

/*
 * Determine whenever the trap whose info is in SIGINFO is caused by
 * integer overflow.
 */
gboolean
mono_arch_is_int_overflow (void *sigctx, void *info)
{
	MonoContext ctx;
	guint8* ip;

	mono_sigctx_to_monoctx (sigctx, &ctx);

	ip = (guint8*)ctx.eip;

	if ((ip [0] == 0xf7) && (x86_modrm_mod (ip [1]) == 0x3) && (x86_modrm_reg (ip [1]) == 0x7)) {
		gint32 reg;

		/* idiv REG */
		switch (x86_modrm_rm (ip [1])) {
		case X86_EAX:
			reg = ctx.eax;
			break;
		case X86_ECX:
			reg = ctx.ecx;
			break;
		case X86_EDX:
			reg = ctx.edx;
			break;
		case X86_EBX:
			reg = ctx.ebx;
			break;
		case X86_ESI:
			reg = ctx.esi;
			break;
		case X86_EDI:
			reg = ctx.edi;
			break;
		default:
			g_assert_not_reached ();
			reg = -1;
		}

		if (reg == -1)
			return TRUE;
	}

	return FALSE;
}

GList *
mono_arch_get_allocatable_int_vars (MonoCompile *cfg)
{
	GList *vars = NULL;
	int i;

	for (i = 0; i < cfg->num_varinfo; i++) {
		MonoInst *ins = cfg->varinfo [i];
		MonoMethodVar *vmv = MONO_VARINFO (cfg, i);

		/* unused vars */
		if (vmv->range.first_use.abs_pos >= vmv->range.last_use.abs_pos)
			continue;

		if ((ins->flags & (MONO_INST_IS_DEAD|MONO_INST_VOLATILE|MONO_INST_INDIRECT)) ||
		    (ins->opcode != OP_LOCAL && ins->opcode != OP_ARG))
			continue;

		/* we dont allocate I1 to registers because there is no simply way to sign extend
		 * 8bit quantities in caller saved registers on x86 */
		if (mono_is_regsize_var (ins->inst_vtype) && (ins->inst_vtype->type != MONO_TYPE_I1)) {
			g_assert (MONO_VARINFO (cfg, i)->reg == -1);
			g_assert (i == vmv->idx);
			vars = g_list_prepend (vars, vmv);
		}
	}

	vars = mono_varlist_sort (cfg, vars, 0);

	return vars;
}

GList *
mono_arch_get_global_int_regs (MonoCompile *cfg)
{
	GList *regs = NULL;

	/* we can use 3 registers for global allocation */
	regs = g_list_prepend (regs, (gpointer)X86_EBX);
	regs = g_list_prepend (regs, (gpointer)X86_ESI);
	regs = g_list_prepend (regs, (gpointer)X86_EDI);

	return regs;
}

/*
 * mono_arch_regalloc_cost:
 *
 *  Return the cost, in number of memory references, of the action of
 * allocating the variable VMV into a register during global register
 * allocation.
 */
guint32
mono_arch_regalloc_cost (MonoCompile *cfg, MonoMethodVar *vmv)
{
	MonoInst *ins = cfg->varinfo [vmv->idx];

	if (cfg->method->save_lmf)
		/* The register is already saved */
		return (ins->opcode == OP_ARG) ? 1 : 0;
	else
		/* push+pop+possible load if it is an argument */
		return (ins->opcode == OP_ARG) ? 3 : 2;
}

static void
set_needs_stack_frame (MonoCompile *cfg, gboolean flag)
{
	static int inited = FALSE;
	static int count = 0;

	if (cfg->arch.need_stack_frame_inited) {
		g_assert (cfg->arch.need_stack_frame == flag);
		return;
	}

	cfg->arch.need_stack_frame = flag;
	cfg->arch.need_stack_frame_inited = TRUE;

	if (flag)
		return;

	if (!inited) {
		mono_counters_register ("Could eliminate stack frame", MONO_COUNTER_INT|MONO_COUNTER_JIT, &count);
		inited = TRUE;
	}
	++count;

	//g_print ("will eliminate %s.%s.%s\n", cfg->method->klass->name_space, cfg->method->klass->name, cfg->method->name);
}

static gboolean
needs_stack_frame (MonoCompile *cfg)
{
	MonoMethodSignature *sig;
	MonoMethodHeader *header;
	gboolean result = FALSE;

#if defined (__APPLE__)
	/*OSX requires stack frame code to have the correct alignment. */
	return TRUE;
#endif

	if (cfg->arch.need_stack_frame_inited)
		return cfg->arch.need_stack_frame;

	header = cfg->header;
	sig = mono_method_signature_internal (cfg->method);

	if (cfg->disable_omit_fp)
		result = TRUE;
	else if (cfg->flags & MONO_CFG_HAS_ALLOCA)
		result = TRUE;
	else if (cfg->method->save_lmf)
		result = TRUE;
	else if (cfg->stack_offset)
		result = TRUE;
	else if (cfg->param_area)
		result = TRUE;
	else if (cfg->flags & (MONO_CFG_HAS_CALLS | MONO_CFG_HAS_ALLOCA | MONO_CFG_HAS_TAILCALL))
		result = TRUE;
	else if (header->num_clauses)
		result = TRUE;
	else if (sig->param_count + sig->hasthis)
		result = TRUE;
	else if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG))
		result = TRUE;

	set_needs_stack_frame (cfg, result);

	return cfg->arch.need_stack_frame;
}

/*
 * Set var information according to the calling convention. X86 version.
 * The locals var stuff should most likely be split in another method.
 */
void
mono_arch_allocate_vars (MonoCompile *cfg)
{
	MonoMethodSignature *sig;
	MonoInst *inst;
	guint32 locals_stack_size, locals_stack_align;
	int i, offset;
	gint32 *offsets;
	CallInfo *cinfo;

	sig = mono_method_signature_internal (cfg->method);

	if (!cfg->arch.cinfo)
		cfg->arch.cinfo = get_call_info (cfg->mempool, sig);
	cinfo = cfg->arch.cinfo;

	cfg->frame_reg = X86_EBP;
	offset = 0;

	if (cfg->has_atomic_add_i4 || cfg->has_atomic_exchange_i4) {
		/* The opcode implementations use callee-saved regs as scratch regs by pushing and pop-ing them, but that is not async safe */
		cfg->used_int_regs |= (1 << X86_EBX) | (1 << X86_EDI) | (1 << X86_ESI);
	}

	/* Reserve space to save LMF and caller saved registers */

	if (cfg->method->save_lmf) {
		/* The LMF var is allocated normally */
	} else {
		if (cfg->used_int_regs & (1 << X86_EBX)) {
			offset += 4;
		}

		if (cfg->used_int_regs & (1 << X86_EDI)) {
			offset += 4;
		}

		if (cfg->used_int_regs & (1 << X86_ESI)) {
			offset += 4;
		}
	}

	switch (cinfo->ret.storage) {
	case ArgValuetypeInReg:
		/* Allocate a local to hold the result, the epilog will copy it to the correct place */
		offset += 8;
		cfg->ret->opcode = OP_REGOFFSET;
		cfg->ret->inst_basereg = X86_EBP;
		cfg->ret->inst_offset = - offset;
		break;
	default:
		break;
	}

	/* Allocate a local for any register arguments that need them. */
	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ArgInfo *ainfo = &cinfo->args [i];
		inst = cfg->args [i];
		if (inst->opcode != OP_REGVAR && storage_in_ireg (ainfo->storage)) {
			offset += 4;
			cfg->args[i]->opcode = OP_REGOFFSET;
			cfg->args[i]->inst_basereg = X86_EBP;
			cfg->args[i]->inst_offset = - offset;
		}
	}

	/* Allocate locals */
	offsets = mono_allocate_stack_slots (cfg, TRUE, &locals_stack_size, &locals_stack_align);
	if (locals_stack_size > MONO_ARCH_MAX_FRAME_SIZE) {
		char *mname = mono_method_full_name (cfg->method, TRUE);
		mono_cfg_set_exception_invalid_program (cfg, g_strdup_printf ("Method %s stack is too big.", mname));
		g_free (mname);
		return;
	}
	if (locals_stack_align) {
		int prev_offset = offset;

		offset += (locals_stack_align - 1);
		offset &= ~(locals_stack_align - 1);

		while (prev_offset < offset) {
			prev_offset += 4;
			mini_gc_set_slot_type_from_fp (cfg, - prev_offset, SLOT_NOREF);
		}
	}
	cfg->locals_min_stack_offset = - (offset + (gint)locals_stack_size);
	cfg->locals_max_stack_offset = - offset;
	/*
	 * EBP is at alignment 8 % MONO_ARCH_FRAME_ALIGNMENT, so if we
	 * have locals larger than 8 bytes we need to make sure that
	 * they have the appropriate offset.
	 */
	if (MONO_ARCH_FRAME_ALIGNMENT > 8 && locals_stack_align > 8) {
		int extra_size = MONO_ARCH_FRAME_ALIGNMENT - sizeof (target_mgreg_t) * 2;
		offset += extra_size;
		locals_stack_size += extra_size;
	}
	for (i = cfg->locals_start; i < cfg->num_varinfo; i++) {
		if (offsets [i] != -1) {
			inst = cfg->varinfo [i];
			inst->opcode = OP_REGOFFSET;
			inst->inst_basereg = X86_EBP;
			inst->inst_offset = - (offset + offsets [i]);
			//printf ("allocated local %d to ", i); mono_print_tree_nl (inst);
		}
	}
	offset += locals_stack_size;


	/*
	 * Allocate arguments+return value
	 */

	switch (cinfo->ret.storage) {
	case ArgOnStack:
		if (cfg->vret_addr) {
			/*
			 * In the new IR, the cfg->vret_addr variable represents the
			 * vtype return value.
			 */
			cfg->vret_addr->opcode = OP_REGOFFSET;
			cfg->vret_addr->inst_basereg = cfg->frame_reg;
			cfg->vret_addr->inst_offset = cinfo->ret.offset + ARGS_OFFSET;
			if (G_UNLIKELY (cfg->verbose_level > 1)) {
				printf ("vret_addr =");
				mono_print_ins (cfg->vret_addr);
			}
		} else {
			cfg->ret->opcode = OP_REGOFFSET;
			cfg->ret->inst_basereg = X86_EBP;
			cfg->ret->inst_offset = cinfo->ret.offset + ARGS_OFFSET;
		}
		break;
	case ArgValuetypeInReg:
		break;
	case ArgInIReg:
		cfg->ret->opcode = OP_REGVAR;
		cfg->ret->inst_c0 = cinfo->ret.reg;
		cfg->ret->dreg = cinfo->ret.reg;
		break;
	case ArgNone:
	case ArgOnFloatFpStack:
	case ArgOnDoubleFpStack:
		break;
	default:
		g_assert_not_reached ();
	}

	if (sig->call_convention == MONO_CALL_VARARG) {
		g_assert (cinfo->sig_cookie.storage == ArgOnStack);
		cfg->sig_cookie = cinfo->sig_cookie.offset + ARGS_OFFSET;
	}

	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ArgInfo *ainfo = &cinfo->args [i];
		inst = cfg->args [i];
		if (inst->opcode != OP_REGVAR) {
			if (storage_in_ireg (ainfo->storage)) {
				/* We already allocated locals for register arguments. */
			} else {
				inst->opcode = OP_REGOFFSET;
				inst->inst_basereg = X86_EBP;
				inst->inst_offset = ainfo->offset + ARGS_OFFSET;
			}
		}
	}

	cfg->stack_offset = offset;
}

void
mono_arch_create_vars (MonoCompile *cfg)
{
	MonoType *sig_ret;
	MonoMethodSignature *sig;
	CallInfo *cinfo;

	sig = mono_method_signature_internal (cfg->method);

	if (!cfg->arch.cinfo)
		cfg->arch.cinfo = get_call_info (cfg->mempool, sig);
	cinfo = cfg->arch.cinfo;

	sig_ret = mini_get_underlying_type (sig->ret);

	if (cinfo->ret.storage == ArgValuetypeInReg)
		cfg->ret_var_is_local = TRUE;
	if ((cinfo->ret.storage != ArgValuetypeInReg) && (MONO_TYPE_ISSTRUCT (sig_ret) || mini_is_gsharedvt_variable_type (sig_ret))) {
		cfg->vret_addr = mono_compile_create_var (cfg, mono_get_int_type (), OP_ARG);
	}

	if (cfg->gen_sdb_seq_points) {
		MonoInst *ins;

		ins = mono_compile_create_var (cfg, mono_get_int_type (), OP_LOCAL);
		ins->flags |= MONO_INST_VOLATILE;
		cfg->arch.ss_tramp_var = ins;

		ins = mono_compile_create_var (cfg, mono_get_int_type (), OP_LOCAL);
		ins->flags |= MONO_INST_VOLATILE;
		cfg->arch.bp_tramp_var = ins;
	}

	if (cfg->method->save_lmf) {
		cfg->create_lmf_var = TRUE;
		cfg->lmf_ir = TRUE;
	}

	cfg->arch_eh_jit_info = 1;
}

/*
 * It is expensive to adjust esp for each individual fp argument pushed on the stack
 * so we try to do it just once when we have multiple fp arguments in a row.
 * We don't use this mechanism generally because for int arguments the generated code
 * is slightly bigger and new generation cpus optimize away the dependency chains
 * created by push instructions on the esp value.
 * fp_arg_setup is the first argument in the execution sequence where the esp register
 * is modified.
 */
static G_GNUC_UNUSED int
collect_fp_stack_space (MonoMethodSignature *sig, int start_arg, int *fp_arg_setup)
{
	int fp_space = 0;
	MonoType *t;

	for (; start_arg < sig->param_count; ++start_arg) {
		t = mini_get_underlying_type (sig->params [start_arg]);
		if (!m_type_is_byref (t) && t->type == MONO_TYPE_R8) {
			fp_space += sizeof (double);
			*fp_arg_setup = start_arg;
		} else {
			break;
		}
	}
	return fp_space;
}

static void
emit_sig_cookie (MonoCompile *cfg, MonoCallInst *call, CallInfo *cinfo)
{
	MonoMethodSignature *tmp_sig;
	int sig_reg;

	/*
	 * mono_ArgIterator_Setup assumes the signature cookie is
	 * passed first and all the arguments which were before it are
	 * passed on the stack after the signature. So compensate by
	 * passing a different signature.
	 */
	tmp_sig = mono_metadata_signature_dup (call->signature);
	tmp_sig->param_count -= call->signature->sentinelpos;
	tmp_sig->sentinelpos = 0;
	memcpy (tmp_sig->params, call->signature->params + call->signature->sentinelpos, tmp_sig->param_count * sizeof (MonoType*));

	if (cfg->compile_aot) {
		sig_reg = mono_alloc_ireg (cfg);
		MONO_EMIT_NEW_SIGNATURECONST (cfg, sig_reg, tmp_sig);
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, X86_ESP, cinfo->sig_cookie.offset, sig_reg);
	} else {
		MONO_EMIT_NEW_STORE_MEMBASE_IMM (cfg, OP_STORE_MEMBASE_IMM, X86_ESP, cinfo->sig_cookie.offset, (gsize)tmp_sig);
	}
}

#ifdef ENABLE_LLVM
LLVMCallInfo*
mono_arch_get_llvm_call_info (MonoCompile *cfg, MonoMethodSignature *sig)
{
	int i, n;
	CallInfo *cinfo;
	ArgInfo *ainfo;
	LLVMCallInfo *linfo;
	MonoType *t, *sig_ret;

	n = sig->param_count + sig->hasthis;

	cinfo = get_call_info (cfg->mempool, sig);
	sig_ret = sig->ret;

	linfo = mono_mempool_alloc0 (cfg->mempool, sizeof (LLVMCallInfo) + (sizeof (LLVMArgInfo) * n));

	/*
	 * LLVM always uses the native ABI while we use our own ABI, the
	 * only difference is the handling of vtypes:
	 * - we only pass/receive them in registers in some cases, and only
	 *   in 1 or 2 integer registers.
	 */
	if (cinfo->ret.storage == ArgValuetypeInReg) {
		if (sig->pinvoke) {
			cfg->exception_message = g_strdup ("pinvoke + vtypes");
			cfg->disable_llvm = TRUE;
			return linfo;
		}

		cfg->exception_message = g_strdup ("vtype ret in call");
		cfg->disable_llvm = TRUE;
		/*
		linfo->ret.storage = LLVMArgVtypeInReg;
		for (j = 0; j < 2; ++j)
			linfo->ret.pair_storage [j] = arg_storage_to_llvm_arg_storage (cfg, cinfo->ret.pair_storage [j]);
		*/
	}

	if (mini_type_is_vtype (sig_ret) && cinfo->ret.storage == ArgInIReg) {
		/* Vtype returned using a hidden argument */
		linfo->ret.storage = LLVMArgVtypeRetAddr;
		linfo->vret_arg_index = cinfo->vret_arg_index;
	}

	if (mini_type_is_vtype (sig_ret) && cinfo->ret.storage != ArgInIReg) {
		// FIXME:
		cfg->exception_message = g_strdup ("vtype ret in call");
		cfg->disable_llvm = TRUE;
	}

	for (i = 0; i < n; ++i) {
		ainfo = cinfo->args + i;

		if (i >= sig->hasthis)
			t = sig->params [i - sig->hasthis];
		else
			t = mono_get_int_type ();

		linfo->args [i].storage = LLVMArgNone;

		switch (ainfo->storage) {
		case ArgInIReg:
			linfo->args [i].storage = LLVMArgNormal;
			break;
		case ArgInDoubleSSEReg:
		case ArgInFloatSSEReg:
			linfo->args [i].storage = LLVMArgNormal;
			break;
		case ArgOnStack:
			if (mini_type_is_vtype (t)) {
				if (mono_class_value_size (mono_class_from_mono_type_internal (t), NULL) == 0)
				/* LLVM seems to allocate argument space for empty structures too */
					linfo->args [i].storage = LLVMArgNone;
				else
					linfo->args [i].storage = LLVMArgVtypeByVal;
			} else {
				linfo->args [i].storage = LLVMArgNormal;
			}
			break;
		case ArgValuetypeInReg:
			if (sig->pinvoke) {
				cfg->exception_message = g_strdup ("pinvoke + vtypes");
				cfg->disable_llvm = TRUE;
				return linfo;
			}

			cfg->exception_message = g_strdup ("vtype arg");
			cfg->disable_llvm = TRUE;
			/*
			linfo->args [i].storage = LLVMArgVtypeInReg;
			for (j = 0; j < 2; ++j)
				linfo->args [i].pair_storage [j] = arg_storage_to_llvm_arg_storage (cfg, ainfo->pair_storage [j]);
			*/
			break;
		case ArgGSharedVt:
			linfo->args [i].storage = LLVMArgGSharedVt;
			break;
		default:
			cfg->exception_message = g_strdup ("ainfo->storage");
			cfg->disable_llvm = TRUE;
			break;
		}
	}

	return linfo;
}
#endif

static void
emit_gc_param_slot_def (MonoCompile *cfg, int sp_offset, MonoType *t)
{
	if (cfg->compute_gc_maps) {
		MonoInst *def;

		/* Needs checking if the feature will be enabled again */
		g_assert_not_reached ();

		/* On x86, the offsets are from the sp value before the start of the call sequence */
		if (t == NULL)
			t = mono_get_int_type ();
		EMIT_NEW_GC_PARAM_SLOT_LIVENESS_DEF (cfg, def, sp_offset, t);
	}
}

void
mono_arch_emit_call (MonoCompile *cfg, MonoCallInst *call)
{
	MonoType *sig_ret;
	MonoInst *arg, *in;
	MonoMethodSignature *sig;
	int i, j, n;
	CallInfo *cinfo;
	int sentinelpos = 0, sp_offset = 0;

	sig = call->signature;
	n = sig->param_count + sig->hasthis;
	sig_ret = mini_get_underlying_type (sig->ret);

	cinfo = get_call_info (cfg->mempool, sig);
	call->call_info = cinfo;

	if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG))
		sentinelpos = sig->sentinelpos + (sig->hasthis ? 1 : 0);

	if (sig_ret && MONO_TYPE_ISSTRUCT (sig_ret)) {
		if (cinfo->ret.storage == ArgValuetypeInReg && cinfo->ret.pair_storage[0] != ArgNone ) {
			/*
			 * Tell the JIT to use a more efficient calling convention: call using
			 * OP_CALL, compute the result location after the call, and save the
			 * result there.
			 */
			call->vret_in_reg = TRUE;
#if defined (__APPLE__)
			if (cinfo->ret.pair_storage [0] == ArgOnDoubleFpStack || cinfo->ret.pair_storage [0] == ArgOnFloatFpStack)
				call->vret_in_reg_fp = TRUE;
#endif
			if (call->vret_var)
				NULLIFY_INS (call->vret_var);
		}
	}

	// FIXME: Emit EMIT_NEW_GC_PARAM_SLOT_LIVENESS_DEF everywhere

	/* Handle the case where there are no implicit arguments */
	if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) && (n == sentinelpos)) {
		emit_sig_cookie (cfg, call, cinfo);
		sp_offset = cinfo->sig_cookie.offset;
		emit_gc_param_slot_def (cfg, sp_offset, NULL);
	}

	/* Arguments are pushed in the reverse order */
	for (i = n - 1; i >= 0; i --) {
		ArgInfo *ainfo = cinfo->args + i;
		MonoType *orig_type, *t;
		int argsize;

		if (cinfo->vtype_retaddr && cinfo->vret_arg_index == 1 && i == 0) {
			MonoInst *vtarg;

			/* Push the vret arg before the first argument */
			MONO_INST_NEW (cfg, vtarg, OP_STORE_MEMBASE_REG);
			vtarg->type = STACK_MP;
			vtarg->inst_destbasereg = X86_ESP;
			vtarg->sreg1 = call->vret_var->dreg;
			vtarg->inst_offset = cinfo->ret.offset;
			MONO_ADD_INS (cfg->cbb, vtarg);
			emit_gc_param_slot_def (cfg, cinfo->ret.offset, NULL);
		}

		if (i >= sig->hasthis)
			t = sig->params [i - sig->hasthis];
		else
			t = mono_get_int_type ();
		orig_type = t;
		t = mini_get_underlying_type (t);

		MONO_INST_NEW (cfg, arg, OP_X86_PUSH);

		in = call->args [i];
		arg->cil_code = in->cil_code;
		arg->sreg1 = in->dreg;
		arg->type = in->type;

		g_assert (in->dreg != -1);

		if (ainfo->storage == ArgGSharedVt) {
			arg->opcode = OP_OUTARG_VT;
			arg->sreg1 = in->dreg;
			arg->klass = in->klass;
			arg->inst_p1 = mono_mempool_alloc (cfg->mempool, sizeof (ArgInfo));
			memcpy (arg->inst_p1, ainfo, sizeof (ArgInfo));
			sp_offset += 4;
			MONO_ADD_INS (cfg->cbb, arg);
		} else if ((i >= sig->hasthis) && (MONO_TYPE_ISSTRUCT(t))) {
			guint32 align;
			guint32 size;

			g_assert (in->klass);

			if (t->type == MONO_TYPE_TYPEDBYREF) {
				size = MONO_ABI_SIZEOF (MonoTypedRef);
				align = sizeof (target_mgreg_t);
			}
			else {
				size = mini_type_stack_size_full (m_class_get_byval_arg (in->klass), &align, sig->pinvoke && !sig->marshalling_disabled);
			}

			if (size > 0 || ainfo->pass_empty_struct) {
				arg->opcode = OP_OUTARG_VT;
				arg->sreg1 = in->dreg;
				arg->klass = in->klass;
				arg->backend.size = size;
				arg->inst_p0 = call;
				arg->inst_p1 = mono_mempool_alloc (cfg->mempool, sizeof (ArgInfo));
				memcpy (arg->inst_p1, ainfo, sizeof (ArgInfo));

				MONO_ADD_INS (cfg->cbb, arg);
				if (ainfo->storage != ArgValuetypeInReg) {
					emit_gc_param_slot_def (cfg, ainfo->offset, orig_type);
				}
			}
		} else {
			switch (ainfo->storage) {
			case ArgOnStack:
				if (!m_type_is_byref (t)) {
					if (t->type == MONO_TYPE_R4) {
						MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER4_MEMBASE_REG, X86_ESP, ainfo->offset, in->dreg);
						argsize = 4;
					} else if (t->type == MONO_TYPE_R8) {
						MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER8_MEMBASE_REG, X86_ESP, ainfo->offset, in->dreg);
						argsize = 8;
					} else if (t->type == MONO_TYPE_I8 || t->type == MONO_TYPE_U8) {
						MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, X86_ESP, ainfo->offset + 4, MONO_LVREG_MS (in->dreg));
						MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, X86_ESP, ainfo->offset, MONO_LVREG_LS (in->dreg));
						argsize = 4;
					} else {
						MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, X86_ESP, ainfo->offset, in->dreg);
						argsize = 4;
					}
				} else {
					MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, X86_ESP, ainfo->offset, in->dreg);
					argsize = 4;
				}
				break;
			case ArgInIReg:
				arg->opcode = OP_MOVE;
				arg->dreg = ainfo->reg;
				MONO_ADD_INS (cfg->cbb, arg);
				argsize = 0;
				break;
			default:
				g_assert_not_reached ();
			}

			if (cfg->compute_gc_maps) {
				if (argsize == 4) {
					/* FIXME: The == STACK_OBJ check might be fragile ? */
					if (sig->hasthis && i == 0 && call->args [i]->type == STACK_OBJ) {
						/* this */
						if (call->need_unbox_trampoline)
							/* The unbox trampoline transforms this into a managed pointer */
							emit_gc_param_slot_def (cfg, ainfo->offset, mono_class_get_byref_type (mono_defaults.int_class));
						else
							emit_gc_param_slot_def (cfg, ainfo->offset, mono_get_object_type ());
					} else {
						emit_gc_param_slot_def (cfg, ainfo->offset, orig_type);
					}
				} else {
					/* i8/r8 */
					for (j = 0; j < argsize; j += 4)
						emit_gc_param_slot_def (cfg, ainfo->offset + j, NULL);
				}
			}
		}

		if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) && (i == sentinelpos)) {
			/* Emit the signature cookie just before the implicit arguments */
			emit_sig_cookie (cfg, call, cinfo);
			emit_gc_param_slot_def (cfg, cinfo->sig_cookie.offset, NULL);
		}
	}

	if (sig_ret && (MONO_TYPE_ISSTRUCT (sig_ret) || cinfo->vtype_retaddr)) {
		MonoInst *vtarg;

		if (cinfo->ret.storage == ArgValuetypeInReg) {
			/* Already done */
		}
		else if (cinfo->ret.storage == ArgInIReg) {
			NOT_IMPLEMENTED;
			/* The return address is passed in a register */
			MONO_INST_NEW (cfg, vtarg, OP_MOVE);
			vtarg->sreg1 = call->inst.dreg;
			vtarg->dreg = mono_alloc_ireg (cfg);
			MONO_ADD_INS (cfg->cbb, vtarg);

			mono_call_inst_add_outarg_reg (cfg, call, vtarg->dreg, cinfo->ret.reg, FALSE);
		} else if (cinfo->vtype_retaddr && cinfo->vret_arg_index == 0) {
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, X86_ESP, cinfo->ret.offset, call->vret_var->dreg);
			emit_gc_param_slot_def (cfg, cinfo->ret.offset, NULL);
		}
	}

	call->stack_usage = cinfo->stack_usage;
	call->stack_align_amount = cinfo->stack_align_amount;
}

void
mono_arch_emit_outarg_vt (MonoCompile *cfg, MonoInst *ins, MonoInst *src)
{
	MonoCallInst *call = (MonoCallInst*)ins->inst_p0;
	ArgInfo *ainfo = (ArgInfo*)ins->inst_p1;
	int size = ins->backend.size;

	if (ainfo->storage == ArgValuetypeInReg) {
		int dreg = mono_alloc_ireg (cfg);
		switch (size) {
		case 1:
			MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU1_MEMBASE, dreg, src->dreg, 0);
			break;
		case 2:
			MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU2_MEMBASE, dreg, src->dreg, 0);
			break;
		case 4:
			MONO_EMIT_NEW_LOAD_MEMBASE (cfg, dreg, src->dreg, 0);
			break;
		case 3: /* FIXME */
		default:
			g_assert_not_reached ();
		}
		mono_call_inst_add_outarg_reg (cfg, call, dreg, ainfo->reg, FALSE);
	}
	else {
		if (cfg->gsharedvt && mini_is_gsharedvt_klass (ins->klass)) {
			/* Pass by addr */
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, X86_ESP, ainfo->offset, src->dreg);
		} else if (size <= 4) {
			int dreg = mono_alloc_ireg (cfg);
			if (ainfo->pass_empty_struct) {
				//Pass empty struct value as 0 on platforms representing empty structs as 1 byte.
				MONO_EMIT_NEW_ICONST (cfg, dreg, 0);
			} else {
				MONO_EMIT_NEW_LOAD_MEMBASE (cfg, dreg, src->dreg, 0);
			}
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, X86_ESP, ainfo->offset, dreg);
		} else if (size <= 20) {
			mini_emit_memcpy (cfg, X86_ESP, ainfo->offset, src->dreg, 0, size, 4);
		} else {
			// FIXME: Code growth
			mini_emit_memcpy (cfg, X86_ESP, ainfo->offset, src->dreg, 0, size, 4);
		}
	}
}

void
mono_arch_emit_setret (MonoCompile *cfg, MonoMethod *method, MonoInst *val)
{
	MonoType *ret = mini_get_underlying_type (mono_method_signature_internal (method)->ret);

	if (!m_type_is_byref (ret)) {
		if (ret->type == MONO_TYPE_R4) {
			if (COMPILE_LLVM (cfg))
				MONO_EMIT_NEW_UNALU (cfg, OP_FMOVE, cfg->ret->dreg, val->dreg);
			/* Nothing to do */
			return;
		} else if (ret->type == MONO_TYPE_R8) {
			if (COMPILE_LLVM (cfg))
				MONO_EMIT_NEW_UNALU (cfg, OP_FMOVE, cfg->ret->dreg, val->dreg);
			/* Nothing to do */
			return;
		} else if (ret->type == MONO_TYPE_I8 || ret->type == MONO_TYPE_U8) {
			if (COMPILE_LLVM (cfg))
				MONO_EMIT_NEW_UNALU (cfg, OP_LMOVE, cfg->ret->dreg, val->dreg);
			else {
				MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, X86_EAX, MONO_LVREG_LS (val->dreg));
				MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, X86_EDX, MONO_LVREG_MS (val->dreg));
			}
			return;
		}
	}

	MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, cfg->ret->dreg, val->dreg);
}

#define EMIT_COND_BRANCH(ins,cond,sign) \
if (ins->inst_true_bb->native_offset) { \
	x86_branch (code, cond, cfg->native_code + ins->inst_true_bb->native_offset, sign); \
} else { \
	mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_BB, ins->inst_true_bb); \
	if ((cfg->opt & MONO_OPT_BRANCH) && x86_is_imm8 (ins->inst_true_bb->max_offset - cpos)) \
		x86_branch8 (code, cond, 0, sign); \
	else \
		x86_branch32 (code, cond, 0, sign); \
}

/*
 *	Emit an exception if condition is fail and
 *  if possible do a directly branch to target
 */
#define EMIT_COND_SYSTEM_EXCEPTION(cond,signed,exc_name)            \
	do {                                                        \
		MonoInst *tins = mono_branch_optimize_exception_target (cfg, bb, exc_name); \
		if (tins == NULL) {										\
			mono_add_patch_info (cfg, code - cfg->native_code,   \
					MONO_PATCH_INFO_EXC, exc_name);  \
			x86_branch32 (code, cond, 0, signed);               \
		} else {	\
			EMIT_COND_BRANCH (tins, cond, signed);	\
		}			\
	} while (0);

#define EMIT_FPCOMPARE(code) do { \
	x86_fcompp (code); \
	x86_fnstsw (code); \
} while (0);

static guint8*
x86_align_and_patch (MonoCompile *cfg, guint8 *code, guint32 patch_type, gconstpointer data)
{
	gboolean needs_paddings = TRUE;
	guint32 pad_size;
	MonoJumpInfo *jinfo = NULL;

	if (cfg->abs_patches) {
		jinfo = (MonoJumpInfo*)g_hash_table_lookup (cfg->abs_patches, data);
		if (jinfo && (jinfo->type == MONO_PATCH_INFO_JIT_ICALL_ADDR
				|| jinfo->type == MONO_PATCH_INFO_SPECIFIC_TRAMPOLINE_LAZY_FETCH_ADDR))
			needs_paddings = FALSE;
	}

	if (cfg->compile_aot)
		needs_paddings = FALSE;
	/*The address must be 4 bytes aligned to avoid spanning multiple cache lines.
	This is required for code patching to be safe on SMP machines.
	*/
	pad_size = (guint32)(code + 1 - cfg->native_code) & 0x3;
	if (needs_paddings && pad_size)
		x86_padding (code, 4 - pad_size);

	mono_add_patch_info (cfg, code - cfg->native_code, (MonoJumpInfoType)patch_type, data);

	return code;
}

static guint8*
emit_call (MonoCompile *cfg, guint8 *code, guint32 patch_type, gconstpointer data)
{
	code = x86_align_and_patch (cfg, code, patch_type, data);

	x86_call_code (code, 0);

	return code;
}

#define INST_IGNORES_CFLAGS(opcode) (!(((opcode) == OP_ADC) || ((opcode) == OP_IADC) || ((opcode) == OP_ADC_IMM) || ((opcode) == OP_IADC_IMM) || ((opcode) == OP_SBB) || ((opcode) == OP_ISBB) || ((opcode) == OP_SBB_IMM) || ((opcode) == OP_ISBB_IMM)))

/*
 * mono_peephole_pass_1:
 *
 *   Perform peephole opts which should/can be performed before local regalloc
 */
void
mono_arch_peephole_pass_1 (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *n;

	MONO_BB_FOR_EACH_INS_SAFE (bb, n, ins) {
		MonoInst *last_ins = mono_inst_prev (ins, FILTER_IL_SEQ_POINT);

		switch (ins->opcode) {
		case OP_IADD_IMM:
		case OP_ADD_IMM:
			if ((ins->sreg1 < MONO_MAX_IREGS) && (ins->dreg >= MONO_MAX_IREGS)) {
				/*
				 * X86_LEA is like ADD, but doesn't have the
				 * sreg1==dreg restriction.
				 */
				ins->opcode = OP_X86_LEA_MEMBASE;
				ins->inst_basereg = ins->sreg1;
			} else if ((ins->inst_imm == 1) && (ins->dreg == ins->sreg1))
				ins->opcode = OP_X86_INC_REG;
			break;
		case OP_SUB_IMM:
		case OP_ISUB_IMM:
			if ((ins->sreg1 < MONO_MAX_IREGS) && (ins->dreg >= MONO_MAX_IREGS)) {
				ins->opcode = OP_X86_LEA_MEMBASE;
				ins->inst_basereg = ins->sreg1;
				ins->inst_imm = -ins->inst_imm;
			} else if ((ins->inst_imm == 1) && (ins->dreg == ins->sreg1))
				ins->opcode = OP_X86_DEC_REG;
			break;
		case OP_COMPARE_IMM:
		case OP_ICOMPARE_IMM:
			/* OP_COMPARE_IMM (reg, 0)
			 * -->
			 * OP_X86_TEST_NULL (reg)
			 */
			if (!ins->inst_imm)
				ins->opcode = OP_X86_TEST_NULL;
			break;
		case OP_X86_COMPARE_MEMBASE_IMM:
			/*
			 * OP_STORE_MEMBASE_REG reg, offset(basereg)
			 * OP_X86_COMPARE_MEMBASE_IMM offset(basereg), imm
			 * -->
			 * OP_STORE_MEMBASE_REG reg, offset(basereg)
			 * OP_COMPARE_IMM reg, imm
			 *
			 * Note: if imm = 0 then OP_COMPARE_IMM replaced with OP_X86_TEST_NULL
			 */
			if (last_ins && (last_ins->opcode == OP_STOREI4_MEMBASE_REG) &&
			    ins->inst_basereg == last_ins->inst_destbasereg &&
			    ins->inst_offset == last_ins->inst_offset) {
					ins->opcode = OP_COMPARE_IMM;
					ins->sreg1 = last_ins->sreg1;

					/* check if we can remove cmp reg,0 with test null */
					if (!ins->inst_imm)
						ins->opcode = OP_X86_TEST_NULL;
				}

			break;
		case OP_X86_PUSH_MEMBASE:
			if (last_ins && (last_ins->opcode == OP_STOREI4_MEMBASE_REG ||
				         last_ins->opcode == OP_STORE_MEMBASE_REG) &&
			    ins->inst_basereg == last_ins->inst_destbasereg &&
			    ins->inst_offset == last_ins->inst_offset) {
				    ins->opcode = OP_X86_PUSH;
				    ins->sreg1 = last_ins->sreg1;
			}
			break;
		}

		mono_peephole_ins (bb, ins);
	}
}

void
mono_arch_peephole_pass_2 (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *n;

	MONO_BB_FOR_EACH_INS_SAFE (bb, n, ins) {
		switch (ins->opcode) {
		case OP_ICONST:
			/* reg = 0 -> XOR (reg, reg) */
			/* XOR sets cflags on x86, so we cant do it always */
			if (ins->inst_c0 == 0 && (!ins->next || (ins->next && INST_IGNORES_CFLAGS (ins->next->opcode)))) {
				MonoInst *ins2;

				ins->opcode = OP_IXOR;
				ins->sreg1 = ins->dreg;
				ins->sreg2 = ins->dreg;

				/*
				 * Convert succeeding STORE_MEMBASE_IMM 0 ins to STORE_MEMBASE_REG
				 * since it takes 3 bytes instead of 7.
				 */
				for (ins2 = mono_inst_next (ins, FILTER_IL_SEQ_POINT); ins2; ins2 = ins2->next) {
					if ((ins2->opcode == OP_STORE_MEMBASE_IMM) && (ins2->inst_imm == 0)) {
						ins2->opcode = OP_STORE_MEMBASE_REG;
						ins2->sreg1 = ins->dreg;
					}
					else if ((ins2->opcode == OP_STOREI4_MEMBASE_IMM) && (ins2->inst_imm == 0)) {
						ins2->opcode = OP_STOREI4_MEMBASE_REG;
						ins2->sreg1 = ins->dreg;
					}
					else if ((ins2->opcode == OP_STOREI1_MEMBASE_IMM) || (ins2->opcode == OP_STOREI2_MEMBASE_IMM)) {
						/* Continue iteration */
					}
					else
						break;
				}
			}
			break;
		case OP_IADD_IMM:
		case OP_ADD_IMM:
			if ((ins->inst_imm == 1) && (ins->dreg == ins->sreg1))
				ins->opcode = OP_X86_INC_REG;
			break;
		case OP_ISUB_IMM:
		case OP_SUB_IMM:
			if ((ins->inst_imm == 1) && (ins->dreg == ins->sreg1))
				ins->opcode = OP_X86_DEC_REG;
			break;
		}

		mono_peephole_ins (bb, ins);
	}
}

#define NEW_INS(cfg,ins,dest,op) do {	\
		MONO_INST_NEW ((cfg), (dest), (op)); \
		(dest)->cil_code = (ins)->cil_code;				 \
		mono_bblock_insert_before_ins (bb, ins, (dest)); \
	} while (0)

/*
 * mono_arch_lowering_pass:
 *
 *  Converts complex opcodes into simpler ones so that each IR instruction
 * corresponds to one machine instruction.
 */
void
mono_arch_lowering_pass (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *next;

	/*
	 * FIXME: Need to add more instructions, but the current machine
	 * description can't model some parts of the composite instructions like
	 * cdq.
	 */
	MONO_BB_FOR_EACH_INS_SAFE (bb, next, ins) {
		switch (ins->opcode) {
		case OP_IREM_IMM:
		case OP_IDIV_IMM:
		case OP_IDIV_UN_IMM:
		case OP_IREM_UN_IMM:
			/*
			 * Keep the cases where we could generated optimized code, otherwise convert
			 * to the non-imm variant.
			 */
			if ((ins->opcode == OP_IREM_IMM) && mono_is_power_of_two (ins->inst_imm) >= 0)
				break;
			mono_decompose_op_imm (cfg, bb, ins);
			break;
#ifdef MONO_ARCH_SIMD_INTRINSICS
		case OP_EXPAND_I1: {
			MonoInst *temp;
			int temp_reg1 = mono_alloc_ireg (cfg);
			int temp_reg2 = mono_alloc_ireg (cfg);
			int original_reg = ins->sreg1;

			NEW_INS (cfg, ins, temp, OP_ICONV_TO_U1);
			temp->sreg1 = original_reg;
			temp->dreg = temp_reg1;

			NEW_INS (cfg, ins, temp, OP_SHL_IMM);
			temp->sreg1 = temp_reg1;
			temp->dreg = temp_reg2;
			temp->inst_imm = 8;

			NEW_INS (cfg, ins, temp, OP_IOR);
			temp->sreg1 = temp->dreg = temp_reg2;
			temp->sreg2 = temp_reg1;

			ins->opcode = OP_EXPAND_I2;
			ins->sreg1 = temp_reg2;
		}
			break;
#endif
		default:
			break;
		}
	}

	bb->max_vreg = cfg->next_vreg;
}

static const int
branch_cc_table [] = {
	X86_CC_EQ, X86_CC_GE, X86_CC_GT, X86_CC_LE, X86_CC_LT,
	X86_CC_NE, X86_CC_GE, X86_CC_GT, X86_CC_LE, X86_CC_LT,
	X86_CC_O, X86_CC_NO, X86_CC_C, X86_CC_NC
};

/* Maps CMP_... constants to X86_CC_... constants */
static const int
cc_table [] = {
	X86_CC_EQ, X86_CC_NE, X86_CC_LE, X86_CC_GE, X86_CC_LT, X86_CC_GT,
	X86_CC_LE, X86_CC_GE, X86_CC_LT, X86_CC_GT
};

static const int
cc_signed_table [] = {
	TRUE, TRUE, TRUE, TRUE, TRUE, TRUE,
	FALSE, FALSE, FALSE, FALSE
};

static unsigned char*
emit_float_to_int (MonoCompile *cfg, guchar *code, int dreg, int size, gboolean is_signed)
{
#define XMM_TEMP_REG 0
	/*This SSE2 optimization must not be done which OPT_SIMD in place as it clobbers xmm0.*/
	/*The xmm pass decomposes OP_FCONV_ ops anyway anyway.*/
	if (cfg->opt & MONO_OPT_SSE2 && size < 8 && !(cfg->opt & MONO_OPT_SIMD)) {
		/* optimize by assigning a local var for this use so we avoid
		 * the stack manipulations */
		x86_alu_reg_imm (code, X86_SUB, X86_ESP, 8);
		x86_fst_membase (code, X86_ESP, 0, TRUE, TRUE);
		x86_movsd_reg_membase (code, XMM_TEMP_REG, X86_ESP, 0);
		x86_cvttsd2si (code, dreg, XMM_TEMP_REG);
		x86_alu_reg_imm (code, X86_ADD, X86_ESP, 8);
		if (size == 1)
			x86_widen_reg (code, dreg, dreg, is_signed, FALSE);
		else if (size == 2)
			x86_widen_reg (code, dreg, dreg, is_signed, TRUE);
		return code;
	}
	x86_alu_reg_imm (code, X86_SUB, X86_ESP, 4);
	x86_fnstcw_membase(code, X86_ESP, 0);
	x86_mov_reg_membase (code, dreg, X86_ESP, 0, 2);
	x86_alu_reg_imm (code, X86_OR, dreg, 0xc00);
	x86_mov_membase_reg (code, X86_ESP, 2, dreg, 2);
	x86_fldcw_membase (code, X86_ESP, 2);
	if (size == 8) {
		x86_alu_reg_imm (code, X86_SUB, X86_ESP, 8);
		x86_fist_pop_membase (code, X86_ESP, 0, TRUE);
		x86_pop_reg (code, dreg);
		/* FIXME: need the high register
		 * x86_pop_reg (code, dreg_high);
		 */
	} else {
		x86_push_reg (code, X86_EAX); // SP = SP - 4
		x86_fist_pop_membase (code, X86_ESP, 0, FALSE);
		x86_pop_reg (code, dreg);
	}
	x86_fldcw_membase (code, X86_ESP, 0);
	x86_alu_reg_imm (code, X86_ADD, X86_ESP, 4);

	if (size == 1)
		x86_widen_reg (code, dreg, dreg, is_signed, FALSE);
	else if (size == 2)
		x86_widen_reg (code, dreg, dreg, is_signed, TRUE);
	return code;
}

static unsigned char*
mono_emit_stack_alloc (MonoCompile *cfg, guchar *code, MonoInst* tree)
{
	int sreg = tree->sreg1;
	int need_touch = FALSE;

#if defined (TARGET_WIN32) || defined (MONO_ARCH_SIGSEGV_ON_ALTSTACK)
	need_touch = TRUE;
#endif

	if (need_touch) {
		guint8* br[5];

		/*
		 * Under Windows:
		 * If requested stack size is larger than one page,
		 * perform stack-touch operation
		 */
		/*
		 * Generate stack probe code.
		 * Under Windows, it is necessary to allocate one page at a time,
		 * "touching" stack after each successful sub-allocation. This is
		 * because of the way stack growth is implemented - there is a
		 * guard page before the lowest stack page that is currently commited.
		 * Stack normally grows sequentially so OS traps access to the
		 * guard page and commits more pages when needed.
		 */
		x86_test_reg_imm (code, sreg, ~0xFFF);
		br[0] = code; x86_branch8 (code, X86_CC_Z, 0, FALSE);

		br[2] = code; /* loop */
		x86_alu_reg_imm (code, X86_SUB, X86_ESP, 0x1000);
		x86_test_membase_reg (code, X86_ESP, 0, X86_ESP);

		/*
		 * By the end of the loop, sreg2 is smaller than 0x1000, so the init routine
		 * that follows only initializes the last part of the area.
		 */
		/* Same as the init code below with size==0x1000 */
		if (tree->flags & MONO_INST_INIT) {
			x86_push_reg (code, X86_EAX);
			x86_push_reg (code, X86_ECX);
			x86_push_reg (code, X86_EDI);
			x86_mov_reg_imm (code, X86_ECX, (0x1000 >> 2));
			x86_alu_reg_reg (code, X86_XOR, X86_EAX, X86_EAX);
			if (cfg->param_area)
				x86_lea_membase (code, X86_EDI, X86_ESP, 12 + ALIGN_TO (cfg->param_area, MONO_ARCH_FRAME_ALIGNMENT));
			else
				x86_lea_membase (code, X86_EDI, X86_ESP, 12);
			x86_cld (code);
			x86_prefix (code, X86_REP_PREFIX);
			x86_stosl (code);
			x86_pop_reg (code, X86_EDI);
			x86_pop_reg (code, X86_ECX);
			x86_pop_reg (code, X86_EAX);
		}

		x86_alu_reg_imm (code, X86_SUB, sreg, 0x1000);
		x86_alu_reg_imm (code, X86_CMP, sreg, 0x1000);
		br[3] = code; x86_branch8 (code, X86_CC_AE, 0, FALSE);
		x86_patch (br[3], br[2]);
		x86_test_reg_reg (code, sreg, sreg);
		br[4] = code; x86_branch8 (code, X86_CC_Z, 0, FALSE);
		x86_alu_reg_reg (code, X86_SUB, X86_ESP, sreg);

		br[1] = code; x86_jump8 (code, 0);

		x86_patch (br[0], code);
		x86_alu_reg_reg (code, X86_SUB, X86_ESP, sreg);
		x86_patch (br[1], code);
		x86_patch (br[4], code);
	}
	else
		x86_alu_reg_reg (code, X86_SUB, X86_ESP, tree->sreg1);

	if (tree->flags & MONO_INST_INIT) {
		int offset = 0;
		if (tree->dreg != X86_EAX && sreg != X86_EAX) {
			x86_push_reg (code, X86_EAX);
			offset += 4;
		}
		if (tree->dreg != X86_ECX && sreg != X86_ECX) {
			x86_push_reg (code, X86_ECX);
			offset += 4;
		}
		if (tree->dreg != X86_EDI && sreg != X86_EDI) {
			x86_push_reg (code, X86_EDI);
			offset += 4;
		}

		x86_shift_reg_imm (code, X86_SHR, sreg, 2);
		x86_mov_reg_reg (code, X86_ECX, sreg);
		x86_alu_reg_reg (code, X86_XOR, X86_EAX, X86_EAX);

		if (cfg->param_area)
			x86_lea_membase (code, X86_EDI, X86_ESP, offset + ALIGN_TO (cfg->param_area, MONO_ARCH_FRAME_ALIGNMENT));
		else
			x86_lea_membase (code, X86_EDI, X86_ESP, offset);
		x86_cld (code);
		x86_prefix (code, X86_REP_PREFIX);
		x86_stosl (code);

		if (tree->dreg != X86_EDI && sreg != X86_EDI)
			x86_pop_reg (code, X86_EDI);
		if (tree->dreg != X86_ECX && sreg != X86_ECX)
			x86_pop_reg (code, X86_ECX);
		if (tree->dreg != X86_EAX && sreg != X86_EAX)
			x86_pop_reg (code, X86_EAX);
	}
	return code;
}


static guint8*
emit_move_return_value (MonoCompile *cfg, MonoInst *ins, guint8 *code)
{
	/* Move return value to the target register */
	switch (ins->opcode) {
	case OP_CALL:
	case OP_CALL_REG:
	case OP_CALL_MEMBASE:
		x86_mov_reg_reg (code, ins->dreg, X86_EAX);
		break;
	default:
		break;
	}

	return code;
}

#ifdef TARGET_MACH
static int tls_gs_offset;
#endif

gboolean
mono_arch_have_fast_tls (void)
{
#ifdef TARGET_MACH
	static gboolean have_fast_tls = FALSE;
	static gboolean inited = FALSE;
	guint32 *ins;

	if (mini_debug_options.use_fallback_tls)
		return FALSE;
	if (inited)
		return have_fast_tls;

	ins = (guint32*)pthread_getspecific;
	/*
	 * We're looking for these two instructions:
	 *
	 * mov    0x4(%esp),%eax
	 * mov    %gs:[offset](,%eax,4),%eax
	 */
	have_fast_tls = ins [0] == 0x0424448b && ins [1] == 0x85048b65;
	tls_gs_offset = ins [2];
	inited = TRUE;

	return have_fast_tls;
#elif defined(TARGET_ANDROID)
	return FALSE;
#else
	if (mini_debug_options.use_fallback_tls)
		return FALSE;
	return TRUE;
#endif
}

static guint8*
mono_x86_emit_tls_get (guint8* code, int dreg, int tls_offset)
{
#if defined (TARGET_MACH)
	x86_prefix (code, X86_GS_PREFIX);
	x86_mov_reg_mem (code, dreg, tls_gs_offset + (tls_offset * 4), 4);
#elif defined (TARGET_WIN32)
	/*
	 * See the Under the Hood article in the May 1996 issue of Microsoft Systems
	 * Journal and/or a disassembly of the TlsGet () function.
	 */
	x86_prefix (code, X86_FS_PREFIX);
	x86_mov_reg_mem (code, dreg, 0x18, 4);
	if (tls_offset < 64) {
		x86_mov_reg_membase (code, dreg, dreg, 3600 + (tls_offset * 4), 4);
	} else {
		guint8 *buf [16];

		g_assert (tls_offset < 0x440);
		/* Load TEB->TlsExpansionSlots */
		x86_mov_reg_membase (code, dreg, dreg, 0xf94, 4);
		x86_test_reg_reg (code, dreg, dreg);
		buf [0] = code;
		x86_branch (code, X86_CC_EQ, code, TRUE);
		x86_mov_reg_membase (code, dreg, dreg, (tls_offset * 4) - 0x100, 4);
		x86_patch (buf [0], code);
	}
#else
	x86_prefix (code, X86_GS_PREFIX);
	x86_mov_reg_mem (code, dreg, tls_offset, 4);
#endif
	return code;
}

static guint8*
mono_x86_emit_tls_set (guint8* code, int sreg, int tls_offset)
{
#if defined (TARGET_MACH)
	x86_prefix (code, X86_GS_PREFIX);
	x86_mov_mem_reg (code, tls_gs_offset + (tls_offset * 4), sreg, 4);
#elif defined (TARGET_WIN32)
	g_assert_not_reached ();
#else
	x86_prefix (code, X86_GS_PREFIX);
	x86_mov_mem_reg (code, tls_offset, sreg, 4);
#endif
	return code;
}

/*
 * emit_setup_lmf:
 *
 *   Emit code to initialize an LMF structure at LMF_OFFSET.
 */
static guint8*
emit_setup_lmf (MonoCompile *cfg, guint8 *code, gint32 lmf_offset, int cfa_offset)
{
	/* save all caller saved regs */
	x86_mov_membase_reg (code, cfg->frame_reg, lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, ebx), X86_EBX, sizeof (target_mgreg_t));
	mono_emit_unwind_op_offset (cfg, code, X86_EBX, - cfa_offset + lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, ebx));
	x86_mov_membase_reg (code, cfg->frame_reg, lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, edi), X86_EDI, sizeof (target_mgreg_t));
	mono_emit_unwind_op_offset (cfg, code, X86_EDI, - cfa_offset + lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, edi));
	x86_mov_membase_reg (code, cfg->frame_reg, lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, esi), X86_ESI, sizeof (target_mgreg_t));
	mono_emit_unwind_op_offset (cfg, code, X86_ESI, - cfa_offset + lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, esi));
	x86_mov_membase_reg (code, cfg->frame_reg, lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, ebp), X86_EBP, sizeof (target_mgreg_t));

	/* save the current IP */
	if (cfg->compile_aot) {
		/* This pushes the current ip */
		x86_call_imm (code, 0);
		x86_pop_reg (code, X86_EAX);
	} else {
		mono_add_patch_info (cfg, code + 1 - cfg->native_code, MONO_PATCH_INFO_IP, NULL);
		x86_mov_reg_imm (code, X86_EAX, 0);
	}
	x86_mov_membase_reg (code, cfg->frame_reg, lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, eip), X86_EAX, sizeof (target_mgreg_t));

	mini_gc_set_slot_type_from_cfa (cfg, -cfa_offset + lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, eip), SLOT_NOREF);
	mini_gc_set_slot_type_from_cfa (cfg, -cfa_offset + lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, ebp), SLOT_NOREF);
	mini_gc_set_slot_type_from_cfa (cfg, -cfa_offset + lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, esi), SLOT_NOREF);
	mini_gc_set_slot_type_from_cfa (cfg, -cfa_offset + lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, edi), SLOT_NOREF);
	mini_gc_set_slot_type_from_cfa (cfg, -cfa_offset + lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, ebx), SLOT_NOREF);
	mini_gc_set_slot_type_from_cfa (cfg, -cfa_offset + lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, esp), SLOT_NOREF);
	mini_gc_set_slot_type_from_cfa (cfg, -cfa_offset + lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, method), SLOT_NOREF);
	mini_gc_set_slot_type_from_cfa (cfg, -cfa_offset + lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, lmf_addr), SLOT_NOREF);
	mini_gc_set_slot_type_from_cfa (cfg, -cfa_offset + lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, previous_lmf), SLOT_NOREF);

	return code;
}

#ifdef TARGET_WIN32

#define TEB_LAST_ERROR_OFFSET 0x34

static guint8*
emit_get_last_error (guint8* code, int dreg)
{
	/* Threads last error value is located in TEB_LAST_ERROR_OFFSET. */
	x86_prefix (code, X86_FS_PREFIX);
	x86_mov_reg_mem (code, dreg, TEB_LAST_ERROR_OFFSET, sizeof (guint32));
	return code;
}

#else

static guint8*
emit_get_last_error (guint8* code, int dreg)
{
	g_assert_not_reached ();
}

#endif

/* benchmark and set based on cpu */
#define LOOP_ALIGNMENT 8
#define bb_is_loop_start(bb) ((bb)->loop_body_start && (bb)->nesting)

#ifndef DISABLE_JIT
void
mono_arch_output_basic_block (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins;
	MonoCallInst *call;
	guint8 *code = cfg->native_code + cfg->code_len;

	if (cfg->opt & MONO_OPT_LOOP) {
		int pad, align = LOOP_ALIGNMENT;
		/* set alignment depending on cpu */
		if (bb_is_loop_start (bb) && (pad = (cfg->code_len & (align - 1)))) {
			pad = align - pad;
			/*g_print ("adding %d pad at %x to loop in %s\n", pad, cfg->code_len, cfg->method->name);*/
			x86_padding (code, pad);
			cfg->code_len += pad;
			bb->native_offset = cfg->code_len;
		}
	}

	if (cfg->verbose_level > 2)
		g_print ("Basic block %d starting at offset 0x%x\n", bb->block_num, bb->native_offset);

	int cpos = bb->max_offset;

	set_code_cursor (cfg, code);

	mono_debug_open_block (cfg, bb, code - cfg->native_code);

    if (mono_break_at_bb_method && mono_method_desc_full_match (mono_break_at_bb_method, cfg->method) && bb->block_num == mono_break_at_bb_bb_num)
		x86_breakpoint (code);

	MONO_BB_FOR_EACH_INS (bb, ins) {
		const guint offset = code - cfg->native_code;
		set_code_cursor (cfg, code);
		int max_len = ins_get_size (ins->opcode);
		code = realloc_code (cfg, max_len);

		if (cfg->debug_info)
			mono_debug_record_line_number (cfg, ins, offset);

		switch (ins->opcode) {
		case OP_BIGMUL:
			x86_mul_reg (code, ins->sreg2, TRUE);
			break;
		case OP_BIGMUL_UN:
			x86_mul_reg (code, ins->sreg2, FALSE);
			break;
		case OP_X86_SETEQ_MEMBASE:
		case OP_X86_SETNE_MEMBASE:
			x86_set_membase (code, ins->opcode == OP_X86_SETEQ_MEMBASE ? X86_CC_EQ : X86_CC_NE,
		                         ins->inst_basereg, ins->inst_offset, TRUE);
			break;
		case OP_STOREI1_MEMBASE_IMM:
			x86_mov_membase_imm (code, ins->inst_destbasereg, ins->inst_offset, ins->inst_imm, 1);
			break;
		case OP_STOREI2_MEMBASE_IMM:
			x86_mov_membase_imm (code, ins->inst_destbasereg, ins->inst_offset, ins->inst_imm, 2);
			break;
		case OP_STORE_MEMBASE_IMM:
		case OP_STOREI4_MEMBASE_IMM:
			x86_mov_membase_imm (code, ins->inst_destbasereg, ins->inst_offset, ins->inst_imm, 4);
			break;
		case OP_STOREI1_MEMBASE_REG:
			x86_mov_membase_reg (code, ins->inst_destbasereg, ins->inst_offset, ins->sreg1, 1);
			break;
		case OP_STOREI2_MEMBASE_REG:
			x86_mov_membase_reg (code, ins->inst_destbasereg, ins->inst_offset, ins->sreg1, 2);
			break;
		case OP_STORE_MEMBASE_REG:
		case OP_STOREI4_MEMBASE_REG:
			x86_mov_membase_reg (code, ins->inst_destbasereg, ins->inst_offset, ins->sreg1, 4);
			break;
		case OP_LOADU4_MEM:
			x86_mov_reg_mem (code, ins->dreg, ins->inst_imm, 4);
			break;
		case OP_LOAD_MEM:
		case OP_LOADI4_MEM:
			/* These are created by the cprop pass so they use inst_imm as the source */
			x86_mov_reg_mem (code, ins->dreg, ins->inst_imm, 4);
			break;
		case OP_LOADU1_MEM:
			x86_widen_mem (code, ins->dreg, ins->inst_imm, FALSE, FALSE);
			break;
		case OP_LOADU2_MEM:
			x86_widen_mem (code, ins->dreg, ins->inst_imm, FALSE, TRUE);
			break;
		case OP_LOAD_MEMBASE:
		case OP_LOADI4_MEMBASE:
		case OP_LOADU4_MEMBASE:
			x86_mov_reg_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset, 4);
			break;
		case OP_LOADU1_MEMBASE:
			x86_widen_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset, FALSE, FALSE);
			break;
		case OP_LOADI1_MEMBASE:
			x86_widen_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset, TRUE, FALSE);
			break;
		case OP_LOADU2_MEMBASE:
			x86_widen_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset, FALSE, TRUE);
			break;
		case OP_LOADI2_MEMBASE:
			x86_widen_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset, TRUE, TRUE);
			break;
		case OP_ICONV_TO_I1:
		case OP_SEXT_I1:
			x86_widen_reg (code, ins->dreg, ins->sreg1, TRUE, FALSE);
			break;
		case OP_ICONV_TO_I2:
		case OP_SEXT_I2:
			x86_widen_reg (code, ins->dreg, ins->sreg1, TRUE, TRUE);
			break;
		case OP_ICONV_TO_U1:
			x86_widen_reg (code, ins->dreg, ins->sreg1, FALSE, FALSE);
			break;
		case OP_ICONV_TO_U2:
			x86_widen_reg (code, ins->dreg, ins->sreg1, FALSE, TRUE);
			break;
		case OP_COMPARE:
		case OP_ICOMPARE:
			x86_alu_reg_reg (code, X86_CMP, ins->sreg1, ins->sreg2);
			break;
		case OP_COMPARE_IMM:
		case OP_ICOMPARE_IMM:
			x86_alu_reg_imm (code, X86_CMP, ins->sreg1, ins->inst_imm);
			break;
		case OP_X86_COMPARE_MEMBASE_REG:
			x86_alu_membase_reg (code, X86_CMP, ins->inst_basereg, ins->inst_offset, ins->sreg2);
			break;
		case OP_X86_COMPARE_MEMBASE_IMM:
			x86_alu_membase_imm (code, X86_CMP, ins->inst_basereg, ins->inst_offset, ins->inst_imm);
			break;
		case OP_X86_COMPARE_MEMBASE8_IMM:
			x86_alu_membase8_imm (code, X86_CMP, ins->inst_basereg, ins->inst_offset, ins->inst_imm);
			break;
		case OP_X86_COMPARE_REG_MEMBASE:
			x86_alu_reg_membase (code, X86_CMP, ins->sreg1, ins->sreg2, ins->inst_offset);
			break;
		case OP_X86_COMPARE_MEM_IMM:
			x86_alu_mem_imm (code, X86_CMP, ins->inst_offset, ins->inst_imm);
			break;
		case OP_X86_TEST_NULL:
			x86_test_reg_reg (code, ins->sreg1, ins->sreg1);
			break;
		case OP_X86_ADD_MEMBASE_IMM:
			x86_alu_membase_imm (code, X86_ADD, ins->inst_basereg, ins->inst_offset, ins->inst_imm);
			break;
		case OP_X86_ADD_REG_MEMBASE:
			x86_alu_reg_membase (code, X86_ADD, ins->sreg1, ins->sreg2, ins->inst_offset);
			break;
		case OP_X86_SUB_MEMBASE_IMM:
			x86_alu_membase_imm (code, X86_SUB, ins->inst_basereg, ins->inst_offset, ins->inst_imm);
			break;
		case OP_X86_SUB_REG_MEMBASE:
			x86_alu_reg_membase (code, X86_SUB, ins->sreg1, ins->sreg2, ins->inst_offset);
			break;
		case OP_X86_AND_MEMBASE_IMM:
			x86_alu_membase_imm (code, X86_AND, ins->inst_basereg, ins->inst_offset, ins->inst_imm);
			break;
		case OP_X86_OR_MEMBASE_IMM:
			x86_alu_membase_imm (code, X86_OR, ins->inst_basereg, ins->inst_offset, ins->inst_imm);
			break;
		case OP_X86_XOR_MEMBASE_IMM:
			x86_alu_membase_imm (code, X86_XOR, ins->inst_basereg, ins->inst_offset, ins->inst_imm);
			break;
		case OP_X86_ADD_MEMBASE_REG:
			x86_alu_membase_reg (code, X86_ADD, ins->inst_basereg, ins->inst_offset, ins->sreg2);
			break;
		case OP_X86_SUB_MEMBASE_REG:
			x86_alu_membase_reg (code, X86_SUB, ins->inst_basereg, ins->inst_offset, ins->sreg2);
			break;
		case OP_X86_AND_MEMBASE_REG:
			x86_alu_membase_reg (code, X86_AND, ins->inst_basereg, ins->inst_offset, ins->sreg2);
			break;
		case OP_X86_OR_MEMBASE_REG:
			x86_alu_membase_reg (code, X86_OR, ins->inst_basereg, ins->inst_offset, ins->sreg2);
			break;
		case OP_X86_XOR_MEMBASE_REG:
			x86_alu_membase_reg (code, X86_XOR, ins->inst_basereg, ins->inst_offset, ins->sreg2);
			break;
		case OP_X86_INC_MEMBASE:
			x86_inc_membase (code, ins->inst_basereg, ins->inst_offset);
			break;
		case OP_X86_INC_REG:
			x86_inc_reg (code, ins->dreg);
			break;
		case OP_X86_DEC_MEMBASE:
			x86_dec_membase (code, ins->inst_basereg, ins->inst_offset);
			break;
		case OP_X86_DEC_REG:
			x86_dec_reg (code, ins->dreg);
			break;
		case OP_X86_MUL_REG_MEMBASE:
			x86_imul_reg_membase (code, ins->sreg1, ins->sreg2, ins->inst_offset);
			break;
		case OP_X86_AND_REG_MEMBASE:
			x86_alu_reg_membase (code, X86_AND, ins->sreg1, ins->sreg2, ins->inst_offset);
			break;
		case OP_X86_OR_REG_MEMBASE:
			x86_alu_reg_membase (code, X86_OR, ins->sreg1, ins->sreg2, ins->inst_offset);
			break;
		case OP_X86_XOR_REG_MEMBASE:
			x86_alu_reg_membase (code, X86_XOR, ins->sreg1, ins->sreg2, ins->inst_offset);
			break;
		case OP_BREAK:
			x86_breakpoint (code);
			break;
 		case OP_RELAXED_NOP:
			x86_prefix (code, X86_REP_PREFIX);
			x86_nop (code);
			break;
 		case OP_HARD_NOP:
			x86_nop (code);
			break;
 		case OP_NOP:
 		case OP_DUMMY_USE:
		case OP_DUMMY_ICONST:
		case OP_DUMMY_R8CONST:
		case OP_DUMMY_R4CONST:
 		case OP_NOT_REACHED:
 		case OP_NOT_NULL:
 			break;
		case OP_IL_SEQ_POINT:
			mono_add_seq_point (cfg, bb, ins, code - cfg->native_code);
			break;
		case OP_SEQ_POINT: {
			int i;

			if (cfg->compile_aot)
				NOT_IMPLEMENTED;

			/* Have to use ecx as a temp reg since this can occur after OP_SETRET */

			/*
			 * We do this _before_ the breakpoint, so single stepping after
			 * a breakpoint is hit will step to the next IL offset.
			 */
			if (ins->flags & MONO_INST_SINGLE_STEP_LOC) {
				MonoInst *var = cfg->arch.ss_tramp_var;
				guint8 *br [1];

				g_assert (var);
				g_assert (var->opcode == OP_REGOFFSET);
				/* Load ss_tramp_var */
				/* This is equal to &ss_trampoline */
				x86_mov_reg_membase (code, X86_ECX, var->inst_basereg, var->inst_offset, sizeof (target_mgreg_t));
				x86_mov_reg_membase (code, X86_ECX, X86_ECX, 0, sizeof (target_mgreg_t));
				x86_alu_reg_imm (code, X86_CMP, X86_ECX, 0);
				br[0] = code; x86_branch8 (code, X86_CC_EQ, 0, FALSE);
				x86_call_reg (code, X86_ECX);
				x86_patch (br [0], code);
			}

			/*
			 * Many parts of sdb depend on the ip after the single step trampoline call to be equal to the seq point offset.
			 * This means we have to put the loading of bp_tramp_var after the offset.
			 */

			mono_add_seq_point (cfg, bb, ins, code - cfg->native_code);

			MonoInst *var = cfg->arch.bp_tramp_var;

			g_assert (var);
			g_assert (var->opcode == OP_REGOFFSET);
			/* Load the address of the bp trampoline */
			/* This needs to be constant size */
			guint8 *start = code;
			x86_mov_reg_membase (code, X86_ECX, var->inst_basereg, var->inst_offset, 4);
			if (code < start + OP_SEQ_POINT_BP_OFFSET) {
				int size = start + OP_SEQ_POINT_BP_OFFSET - code;
				x86_padding (code, size);
			}
			/*
			 * A placeholder for a possible breakpoint inserted by
			 * mono_arch_set_breakpoint ().
			 */
			for (i = 0; i < 2; ++i)
				x86_nop (code);
			/*
			 * Add an additional nop so skipping the bp doesn't cause the ip to point
			 * to another IL offset.
			 */
			x86_nop (code);
			break;
		}
		case OP_ADDCC:
		case OP_IADDCC:
		case OP_IADD:
			x86_alu_reg_reg (code, X86_ADD, ins->sreg1, ins->sreg2);
			break;
		case OP_ADC:
		case OP_IADC:
			x86_alu_reg_reg (code, X86_ADC, ins->sreg1, ins->sreg2);
			break;
		case OP_ADDCC_IMM:
		case OP_ADD_IMM:
		case OP_IADD_IMM:
			x86_alu_reg_imm (code, X86_ADD, ins->dreg, ins->inst_imm);
			break;
		case OP_ADC_IMM:
		case OP_IADC_IMM:
			x86_alu_reg_imm (code, X86_ADC, ins->dreg, ins->inst_imm);
			break;
		case OP_SUBCC:
		case OP_ISUBCC:
		case OP_ISUB:
			x86_alu_reg_reg (code, X86_SUB, ins->sreg1, ins->sreg2);
			break;
		case OP_SBB:
		case OP_ISBB:
			x86_alu_reg_reg (code, X86_SBB, ins->sreg1, ins->sreg2);
			break;
		case OP_SUBCC_IMM:
		case OP_SUB_IMM:
		case OP_ISUB_IMM:
			x86_alu_reg_imm (code, X86_SUB, ins->dreg, ins->inst_imm);
			break;
		case OP_SBB_IMM:
		case OP_ISBB_IMM:
			x86_alu_reg_imm (code, X86_SBB, ins->dreg, ins->inst_imm);
			break;
		case OP_IAND:
			x86_alu_reg_reg (code, X86_AND, ins->sreg1, ins->sreg2);
			break;
		case OP_AND_IMM:
		case OP_IAND_IMM:
			x86_alu_reg_imm (code, X86_AND, ins->sreg1, ins->inst_imm);
			break;
		case OP_IDIV:
		case OP_IREM:
			/*
			 * The code is the same for div/rem, the allocator will allocate dreg
			 * to RAX/RDX as appropriate.
			 */
			if (ins->sreg2 == X86_EDX) {
				/* cdq clobbers this */
				x86_push_reg (code, ins->sreg2);
				x86_cdq (code);
				x86_div_membase (code, X86_ESP, 0, TRUE);
				x86_alu_reg_imm (code, X86_ADD, X86_ESP, 4);
			} else {
				x86_cdq (code);
				x86_div_reg (code, ins->sreg2, TRUE);
			}
			break;
		case OP_IDIV_UN:
		case OP_IREM_UN:
			if (ins->sreg2 == X86_EDX) {
				x86_push_reg (code, ins->sreg2);
				x86_alu_reg_reg (code, X86_XOR, X86_EDX, X86_EDX);
				x86_div_membase (code, X86_ESP, 0, FALSE);
				x86_alu_reg_imm (code, X86_ADD, X86_ESP, 4);
			} else {
				x86_alu_reg_reg (code, X86_XOR, X86_EDX, X86_EDX);
				x86_div_reg (code, ins->sreg2, FALSE);
			}
			break;
		case OP_DIV_IMM:
			x86_mov_reg_imm (code, ins->sreg2, ins->inst_imm);
			x86_cdq (code);
			x86_div_reg (code, ins->sreg2, TRUE);
			break;
		case OP_IREM_IMM: {
			int power = mono_is_power_of_two (ins->inst_imm);

			g_assert (ins->sreg1 == X86_EAX);
			g_assert (ins->dreg == X86_EAX);
			g_assert (power >= 0);

			if (power == 1) {
				/* Based on http://compilers.iecc.com/comparch/article/93-04-079 */
				x86_cdq (code);
				x86_alu_reg_imm (code, X86_AND, X86_EAX, 1);
				/*
				 * If the divident is >= 0, this does not nothing. If it is positive, it
				 * it transforms %eax=0 into %eax=0, and %eax=1 into %eax=-1.
				 */
				x86_alu_reg_reg (code, X86_XOR, X86_EAX, X86_EDX);
				x86_alu_reg_reg (code, X86_SUB, X86_EAX, X86_EDX);
			} else if (power == 0) {
				x86_alu_reg_reg (code, X86_XOR, ins->dreg, ins->dreg);
			} else {
				/* Based on gcc code */

				/* Add compensation for negative dividents */
				x86_cdq (code);
				x86_shift_reg_imm (code, X86_SHR, X86_EDX, 32 - power);
				x86_alu_reg_reg (code, X86_ADD, X86_EAX, X86_EDX);
				/* Compute remainder */
				x86_alu_reg_imm (code, X86_AND, X86_EAX, (1 << power) - 1);
				/* Remove compensation */
				x86_alu_reg_reg (code, X86_SUB, X86_EAX, X86_EDX);
			}
			break;
		}
		case OP_IOR:
			x86_alu_reg_reg (code, X86_OR, ins->sreg1, ins->sreg2);
			break;
		case OP_OR_IMM:
		case OP_IOR_IMM:
			x86_alu_reg_imm (code, X86_OR, ins->sreg1, ins->inst_imm);
			break;
		case OP_IXOR:
			x86_alu_reg_reg (code, X86_XOR, ins->sreg1, ins->sreg2);
			break;
		case OP_XOR_IMM:
		case OP_IXOR_IMM:
			x86_alu_reg_imm (code, X86_XOR, ins->sreg1, ins->inst_imm);
			break;
		case OP_ISHL:
			g_assert (ins->sreg2 == X86_ECX);
			x86_shift_reg (code, X86_SHL, ins->dreg);
			break;
		case OP_ISHR:
			g_assert (ins->sreg2 == X86_ECX);
			x86_shift_reg (code, X86_SAR, ins->dreg);
			break;
		case OP_SHR_IMM:
		case OP_ISHR_IMM:
			x86_shift_reg_imm (code, X86_SAR, ins->dreg, ins->inst_imm);
			break;
		case OP_SHR_UN_IMM:
		case OP_ISHR_UN_IMM:
			x86_shift_reg_imm (code, X86_SHR, ins->dreg, ins->inst_imm);
			break;
		case OP_ISHR_UN:
			g_assert (ins->sreg2 == X86_ECX);
			x86_shift_reg (code, X86_SHR, ins->dreg);
			break;
		case OP_SHL_IMM:
		case OP_ISHL_IMM:
			x86_shift_reg_imm (code, X86_SHL, ins->dreg, ins->inst_imm);
			break;
		case OP_LSHL: {
			guint8 *jump_to_end;

			/* handle shifts below 32 bits */
			x86_shld_reg (code, ins->backend.reg3, ins->sreg1);
			x86_shift_reg (code, X86_SHL, ins->sreg1);

			x86_test_reg_imm (code, X86_ECX, 32);
			jump_to_end = code; x86_branch8 (code, X86_CC_EQ, 0, TRUE);

			/* handle shift over 32 bit */
			x86_mov_reg_reg (code, ins->backend.reg3, ins->sreg1);
			x86_clear_reg (code, ins->sreg1);

			x86_patch (jump_to_end, code);
			}
			break;
		case OP_LSHR: {
			guint8 *jump_to_end;

			/* handle shifts below 32 bits */
			x86_shrd_reg (code, ins->sreg1, ins->backend.reg3);
			x86_shift_reg (code, X86_SAR, ins->backend.reg3);

			x86_test_reg_imm (code, X86_ECX, 32);
			jump_to_end = code; x86_branch8 (code, X86_CC_EQ, 0, FALSE);

			/* handle shifts over 31 bits */
			x86_mov_reg_reg (code, ins->sreg1, ins->backend.reg3);
			x86_shift_reg_imm (code, X86_SAR, ins->backend.reg3, 31);

			x86_patch (jump_to_end, code);
			}
			break;
		case OP_LSHR_UN: {
			guint8 *jump_to_end;

			/* handle shifts below 32 bits */
			x86_shrd_reg (code, ins->sreg1, ins->backend.reg3);
			x86_shift_reg (code, X86_SHR, ins->backend.reg3);

			x86_test_reg_imm (code, X86_ECX, 32);
			jump_to_end = code; x86_branch8 (code, X86_CC_EQ, 0, FALSE);

			/* handle shifts over 31 bits */
			x86_mov_reg_reg (code, ins->sreg1, ins->backend.reg3);
			x86_clear_reg (code, ins->backend.reg3);

			x86_patch (jump_to_end, code);
			}
			break;
		case OP_LSHL_IMM:
			if (ins->inst_imm >= 32) {
				x86_mov_reg_reg (code, ins->backend.reg3, ins->sreg1);
				x86_clear_reg (code, ins->sreg1);
				x86_shift_reg_imm (code, X86_SHL, ins->backend.reg3, ins->inst_imm - 32);
			} else {
				x86_shld_reg_imm (code, ins->backend.reg3, ins->sreg1, ins->inst_imm);
				x86_shift_reg_imm (code, X86_SHL, ins->sreg1, ins->inst_imm);
			}
			break;
		case OP_LSHR_IMM:
			if (ins->inst_imm >= 32) {
				x86_mov_reg_reg (code, ins->sreg1, ins->backend.reg3);
				x86_shift_reg_imm (code, X86_SAR, ins->backend.reg3, 0x1f);
				x86_shift_reg_imm (code, X86_SAR, ins->sreg1, ins->inst_imm - 32);
			} else {
				x86_shrd_reg_imm (code, ins->sreg1, ins->backend.reg3, ins->inst_imm);
				x86_shift_reg_imm (code, X86_SAR, ins->backend.reg3, ins->inst_imm);
			}
			break;
		case OP_LSHR_UN_IMM:
			if (ins->inst_imm >= 32) {
				x86_mov_reg_reg (code, ins->sreg1, ins->backend.reg3);
				x86_clear_reg (code, ins->backend.reg3);
				x86_shift_reg_imm (code, X86_SHR, ins->sreg1, ins->inst_imm - 32);
			} else {
				x86_shrd_reg_imm (code, ins->sreg1, ins->backend.reg3, ins->inst_imm);
				x86_shift_reg_imm (code, X86_SHR, ins->backend.reg3, ins->inst_imm);
			}
			break;
		case OP_INOT:
			x86_not_reg (code, ins->sreg1);
			break;
		case OP_INEG:
			x86_neg_reg (code, ins->sreg1);
			break;

		case OP_IMUL:
			x86_imul_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_MUL_IMM:
		case OP_IMUL_IMM:
			switch (ins->inst_imm) {
			case 2:
				/* MOV r1, r2 */
				/* ADD r1, r1 */
				x86_mov_reg_reg (code, ins->dreg, ins->sreg1);
				x86_alu_reg_reg (code, X86_ADD, ins->dreg, ins->dreg);
				break;
			case 3:
				/* LEA r1, [r2 + r2*2] */
				x86_lea_memindex (code, ins->dreg, ins->sreg1, 0, ins->sreg1, 1);
				break;
			case 5:
				/* LEA r1, [r2 + r2*4] */
				x86_lea_memindex (code, ins->dreg, ins->sreg1, 0, ins->sreg1, 2);
				break;
			case 6:
				/* LEA r1, [r2 + r2*2] */
				/* ADD r1, r1          */
				x86_lea_memindex (code, ins->dreg, ins->sreg1, 0, ins->sreg1, 1);
				x86_alu_reg_reg (code, X86_ADD, ins->dreg, ins->dreg);
				break;
			case 9:
				/* LEA r1, [r2 + r2*8] */
				x86_lea_memindex (code, ins->dreg, ins->sreg1, 0, ins->sreg1, 3);
				break;
			case 10:
				/* LEA r1, [r2 + r2*4] */
				/* ADD r1, r1          */
				x86_lea_memindex (code, ins->dreg, ins->sreg1, 0, ins->sreg1, 2);
				x86_alu_reg_reg (code, X86_ADD, ins->dreg, ins->dreg);
				break;
			case 12:
				/* LEA r1, [r2 + r2*2] */
				/* SHL r1, 2           */
				x86_lea_memindex (code, ins->dreg, ins->sreg1, 0, ins->sreg1, 1);
				x86_shift_reg_imm (code, X86_SHL, ins->dreg, 2);
				break;
			case 25:
				/* LEA r1, [r2 + r2*4] */
				/* LEA r1, [r1 + r1*4] */
				x86_lea_memindex (code, ins->dreg, ins->sreg1, 0, ins->sreg1, 2);
				x86_lea_memindex (code, ins->dreg, ins->dreg, 0, ins->dreg, 2);
				break;
			case 100:
				/* LEA r1, [r2 + r2*4] */
				/* SHL r1, 2           */
				/* LEA r1, [r1 + r1*4] */
				x86_lea_memindex (code, ins->dreg, ins->sreg1, 0, ins->sreg1, 2);
				x86_shift_reg_imm (code, X86_SHL, ins->dreg, 2);
				x86_lea_memindex (code, ins->dreg, ins->dreg, 0, ins->dreg, 2);
				break;
			default:
				x86_imul_reg_reg_imm (code, ins->dreg, ins->sreg1, ins->inst_imm);
				break;
			}
			break;
		case OP_IMUL_OVF:
			x86_imul_reg_reg (code, ins->sreg1, ins->sreg2);
			EMIT_COND_SYSTEM_EXCEPTION (X86_CC_O, FALSE, ins->inst_exc_name);
			break;
		case OP_IMUL_OVF_UN: {
			/* the mul operation and the exception check should most likely be split */
			int non_eax_reg, saved_eax = FALSE, saved_edx = FALSE;
			/*g_assert (ins->sreg2 == X86_EAX);
			g_assert (ins->dreg == X86_EAX);*/
			if (ins->sreg2 == X86_EAX) {
				non_eax_reg = ins->sreg1;
			} else if (ins->sreg1 == X86_EAX) {
				non_eax_reg = ins->sreg2;
			} else {
				/* no need to save since we're going to store to it anyway */
				if (ins->dreg != X86_EAX) {
					saved_eax = TRUE;
					x86_push_reg (code, X86_EAX);
				}
				x86_mov_reg_reg (code, X86_EAX, ins->sreg1);
				non_eax_reg = ins->sreg2;
			}
			if (ins->dreg == X86_EDX) {
				if (!saved_eax) {
					saved_eax = TRUE;
					x86_push_reg (code, X86_EAX);
				}
			} else {
				saved_edx = TRUE;
				x86_push_reg (code, X86_EDX);
			}
			x86_mul_reg (code, non_eax_reg, FALSE);
			/* save before the check since pop and mov don't change the flags */
			x86_mov_reg_reg (code, ins->dreg, X86_EAX);
			if (saved_edx)
				x86_pop_reg (code, X86_EDX);
			if (saved_eax)
				x86_pop_reg (code, X86_EAX);
			EMIT_COND_SYSTEM_EXCEPTION (X86_CC_O, FALSE, ins->inst_exc_name);
			break;
		}
		case OP_ICONST:
			x86_mov_reg_imm (code, ins->dreg, ins->inst_c0);
			break;
		case OP_AOTCONST:
			g_assert_not_reached ();
			mono_add_patch_info (cfg, offset, (MonoJumpInfoType)(gsize)ins->inst_i1, ins->inst_p0);
			x86_mov_reg_imm (code, ins->dreg, 0);
			break;
		case OP_JUMP_TABLE:
			mono_add_patch_info (cfg, offset, (MonoJumpInfoType)(gsize)ins->inst_i1, ins->inst_p0);
			x86_mov_reg_imm (code, ins->dreg, 0);
			break;
		case OP_LOAD_GOTADDR:
			g_assert (ins->dreg == MONO_ARCH_GOT_REG);
			code = mono_arch_emit_load_got_addr (cfg->native_code, code, cfg, NULL);
			break;
		case OP_GOT_ENTRY:
			mono_add_patch_info (cfg, offset, (MonoJumpInfoType)(gsize)ins->inst_right->inst_i1, ins->inst_right->inst_p0);
			x86_mov_reg_membase (code, ins->dreg, ins->inst_basereg, 0xf0f0f0f0, 4);
			break;
		case OP_X86_PUSH_GOT_ENTRY:
			mono_add_patch_info (cfg, offset, (MonoJumpInfoType)(gsize)ins->inst_right->inst_i1, ins->inst_right->inst_p0);
			x86_push_membase (code, ins->inst_basereg, 0xf0f0f0f0);
			break;
		case OP_MOVE:
			x86_mov_reg_reg (code, ins->dreg, ins->sreg1);
			break;

		case OP_TAILCALL_PARAMETER:
			// This opcode helps compute sizes, i.e.
			// of the subsequent OP_TAILCALL, but contributes no code.
			g_assert (ins->next);
			break;

		case OP_TAILCALL:
		case OP_TAILCALL_MEMBASE:
		case OP_TAILCALL_REG: {
			call = (MonoCallInst*)ins;
			int pos = 0, i;
			gboolean const tailcall_membase = ins->opcode == OP_TAILCALL_MEMBASE;
			gboolean const tailcall_reg = (ins->opcode == OP_TAILCALL_REG);
			int const sreg1 = ins->sreg1;
			gboolean const sreg1_ecx = sreg1 == X86_ECX;
			gboolean const tailcall_membase_ecx = tailcall_membase && sreg1_ecx;
			gboolean const tailcall_membase_not_ecx = tailcall_membase && !sreg1_ecx;

			max_len += (call->stack_usage - call->stack_align_amount) / sizeof (target_mgreg_t) * ins_get_size (OP_TAILCALL_PARAMETER);
			code = realloc_code (cfg, max_len);

			ins->flags |= MONO_INST_GC_CALLSITE;
			ins->backend.pc_offset = code - cfg->native_code;

			g_assert (!cfg->method->save_lmf);

			// Ecx is volatile, not used for parameters, or rgctx/imt (edx).
			// It is also not used for return value, though that does not matter.
			// Ecx is preserved across the tailcall formation.
			//
			// Eax could also be used here at the cost of a push/pop moving the parameters.
			// Edx must be preserved as it is rgctx/imt.
			//
			// If ecx happens to be the base of the tailcall_membase, then
			// just end with jmp [ecx+offset] -- one instruction.
			// if ecx is not the base, then move ecx, [reg+offset] and later jmp [ecx] -- two instructions.

			if (tailcall_reg) {
				g_assert (sreg1 > -1);
				x86_mov_reg_reg (code, X86_ECX, sreg1);
			} else if (tailcall_membase_not_ecx) {
				g_assert (sreg1 > -1);
				x86_mov_reg_membase (code, X86_ECX, sreg1, ins->inst_offset, 4);
			}

			/* restore callee saved registers */
			for (i = 0; i < X86_NREG; ++i)
				if (X86_IS_CALLEE_SAVED_REG (i) && cfg->used_int_regs & ((regmask_t)1 << i))
					pos -= 4;
			if (cfg->used_int_regs & (1 << X86_ESI)) {
				x86_mov_reg_membase (code, X86_ESI, X86_EBP, pos, 4);
				pos += 4;
			}
			if (cfg->used_int_regs & (1 << X86_EDI)) {
				x86_mov_reg_membase (code, X86_EDI, X86_EBP, pos, 4);
				pos += 4;
			}
			if (cfg->used_int_regs & (1 << X86_EBX)) {
				x86_mov_reg_membase (code, X86_EBX, X86_EBP, pos, 4);
				pos += 4;
			}

			/* Copy arguments on the stack to our argument area */
			// FIXME use rep mov for constant code size, before nonvolatiles
			// restored, first saving esi, edi into volatiles
			for (i = 0; i < call->stack_usage - call->stack_align_amount; i += 4) {
				x86_mov_reg_membase (code, X86_EAX, X86_ESP, i, 4);
				x86_mov_membase_reg (code, X86_EBP, 8 + i, X86_EAX, 4);
			}

			/* restore ESP/EBP */
			x86_leave (code);

			if (tailcall_membase_ecx) {
				x86_jump_membase (code, X86_ECX, ins->inst_offset);
			} else if (tailcall_reg || tailcall_membase_not_ecx) {
				x86_jump_reg (code, X86_ECX);
			} else {
				// FIXME Patch data instead of code.
				code = x86_align_and_patch (cfg, code, MONO_PATCH_INFO_METHOD_JUMP, call->method);
				x86_jump32 (code, 0);
			}

			ins->flags |= MONO_INST_GC_CALLSITE;
			break;
		}
		case OP_CHECK_THIS:
			/* ensure ins->sreg1 is not NULL
			 * note that cmp DWORD PTR [eax], eax is one byte shorter than
			 * cmp DWORD PTR [eax], 0
		         */
			x86_alu_membase_reg (code, X86_CMP, ins->sreg1, 0, ins->sreg1);
			break;
		case OP_ARGLIST: {
			int hreg = ins->sreg1 == X86_EAX? X86_ECX: X86_EAX;
			x86_push_reg (code, hreg);
			x86_lea_membase (code, hreg, X86_EBP, cfg->sig_cookie);
			x86_mov_membase_reg (code, ins->sreg1, 0, hreg, 4);
			x86_pop_reg (code, hreg);
			break;
		}
		case OP_FCALL:
		case OP_LCALL:
		case OP_VCALL:
		case OP_VCALL2:
		case OP_VOIDCALL:
		case OP_CALL:
		case OP_FCALL_REG:
		case OP_LCALL_REG:
		case OP_VCALL_REG:
		case OP_VCALL2_REG:
		case OP_VOIDCALL_REG:
		case OP_CALL_REG:
		case OP_FCALL_MEMBASE:
		case OP_LCALL_MEMBASE:
		case OP_VCALL_MEMBASE:
		case OP_VCALL2_MEMBASE:
		case OP_VOIDCALL_MEMBASE:
		case OP_CALL_MEMBASE: {
			CallInfo *cinfo;

			call = (MonoCallInst*)ins;
			cinfo = call->call_info;

			switch (ins->opcode) {
			case OP_FCALL:
			case OP_LCALL:
			case OP_VCALL:
			case OP_VCALL2:
			case OP_VOIDCALL:
			case OP_CALL: {
				const MonoJumpInfoTarget patch = mono_call_to_patch (call);
				code = emit_call (cfg, code, patch.type, patch.target);
				break;
			}
			case OP_FCALL_REG:
			case OP_LCALL_REG:
			case OP_VCALL_REG:
			case OP_VCALL2_REG:
			case OP_VOIDCALL_REG:
			case OP_CALL_REG:
				x86_call_reg (code, ins->sreg1);
				break;
			case OP_FCALL_MEMBASE:
			case OP_LCALL_MEMBASE:
			case OP_VCALL_MEMBASE:
			case OP_VCALL2_MEMBASE:
			case OP_VOIDCALL_MEMBASE:
			case OP_CALL_MEMBASE:
				x86_call_membase (code, ins->sreg1, ins->inst_offset);
				break;
			default:
				g_assert_not_reached ();
				break;
			}
			ins->flags |= MONO_INST_GC_CALLSITE;
			ins->backend.pc_offset = code - cfg->native_code;
			if (cinfo->callee_stack_pop) {
				/* Have to compensate for the stack space popped by the callee */
				x86_alu_reg_imm (code, X86_SUB, X86_ESP, cinfo->callee_stack_pop);
			}
			code = emit_move_return_value (cfg, ins, code);
			break;
		}
		case OP_X86_LEA:
			x86_lea_memindex (code, ins->dreg, ins->sreg1, ins->inst_imm, ins->sreg2, ins->backend.shift_amount);
			break;
		case OP_X86_LEA_MEMBASE:
			x86_lea_membase (code, ins->dreg, ins->sreg1, ins->inst_imm);
			break;
		case OP_X86_XCHG:
			x86_xchg_reg_reg (code, ins->sreg1, ins->sreg2, 4);
			break;
		case OP_LOCALLOC:
			/* keep alignment */
			x86_alu_reg_imm (code, X86_ADD, ins->sreg1, MONO_ARCH_LOCALLOC_ALIGNMENT - 1);
			x86_alu_reg_imm (code, X86_AND, ins->sreg1, ~(MONO_ARCH_LOCALLOC_ALIGNMENT - 1));
			code = mono_emit_stack_alloc (cfg, code, ins);
			x86_mov_reg_reg (code, ins->dreg, X86_ESP);
			if (cfg->param_area)
				x86_alu_reg_imm (code, X86_ADD, ins->dreg, ALIGN_TO (cfg->param_area, MONO_ARCH_FRAME_ALIGNMENT));
			break;
		case OP_LOCALLOC_IMM: {
			guint32 size = ins->inst_imm;
			size = (size + (MONO_ARCH_FRAME_ALIGNMENT - 1)) & ~ (MONO_ARCH_FRAME_ALIGNMENT - 1);

			if (ins->flags & MONO_INST_INIT) {
				/* FIXME: Optimize this */
				x86_mov_reg_imm (code, ins->dreg, size);
				ins->sreg1 = ins->dreg;

				code = mono_emit_stack_alloc (cfg, code, ins);
				x86_mov_reg_reg (code, ins->dreg, X86_ESP);
			} else {
				x86_alu_reg_imm (code, X86_SUB, X86_ESP, size);
				x86_mov_reg_reg (code, ins->dreg, X86_ESP);
			}
			if (cfg->param_area)
				x86_alu_reg_imm (code, X86_ADD, ins->dreg, ALIGN_TO (cfg->param_area, MONO_ARCH_FRAME_ALIGNMENT));
			break;
		}
		case OP_THROW: {
			x86_alu_reg_imm (code, X86_SUB, X86_ESP, MONO_ARCH_FRAME_ALIGNMENT - 4);
			x86_push_reg (code, ins->sreg1);
			code = emit_call (cfg, code, MONO_PATCH_INFO_JIT_ICALL_ID,
							  GUINT_TO_POINTER (MONO_JIT_ICALL_mono_arch_throw_exception));
			ins->flags |= MONO_INST_GC_CALLSITE;
			ins->backend.pc_offset = code - cfg->native_code;
			break;
		}
		case OP_RETHROW: {
			x86_alu_reg_imm (code, X86_SUB, X86_ESP, MONO_ARCH_FRAME_ALIGNMENT - 4);
			x86_push_reg (code, ins->sreg1);
			code = emit_call (cfg, code, MONO_PATCH_INFO_JIT_ICALL_ID,
							  GUINT_TO_POINTER (MONO_JIT_ICALL_mono_arch_rethrow_exception));
			ins->flags |= MONO_INST_GC_CALLSITE;
			ins->backend.pc_offset = code - cfg->native_code;
			break;
		}
		case OP_CALL_HANDLER:
			x86_alu_reg_imm (code, X86_SUB, X86_ESP, MONO_ARCH_FRAME_ALIGNMENT - 4);
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_BB, ins->inst_target_bb);
			x86_call_imm (code, 0);
			for (GList *tmp = ins->inst_eh_blocks; tmp != bb->clause_holes; tmp = tmp->prev)
				mono_cfg_add_try_hole (cfg, ((MonoLeaveClause *) tmp->data)->clause, code, bb);
			x86_alu_reg_imm (code, X86_ADD, X86_ESP, MONO_ARCH_FRAME_ALIGNMENT - 4);
			break;
		case OP_START_HANDLER: {
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);
			x86_mov_membase_reg (code, spvar->inst_basereg, spvar->inst_offset, X86_ESP, 4);
			if (cfg->param_area)
				x86_alu_reg_imm (code, X86_SUB, X86_ESP, ALIGN_TO (cfg->param_area, MONO_ARCH_FRAME_ALIGNMENT));
			break;
		}
		case OP_ENDFINALLY: {
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);
			x86_mov_reg_membase (code, X86_ESP, spvar->inst_basereg, spvar->inst_offset, 4);
			x86_ret (code);
			break;
		}
		case OP_ENDFILTER: {
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);
			x86_mov_reg_membase (code, X86_ESP, spvar->inst_basereg, spvar->inst_offset, 4);
			/* The local allocator will put the result into EAX */
			x86_ret (code);
			break;
		}
		case OP_GET_EX_OBJ:
			x86_mov_reg_reg (code, ins->dreg, X86_EAX);
			break;

		case OP_LABEL:
			ins->inst_c0 = code - cfg->native_code;
			break;
		case OP_BR:
			if (ins->inst_target_bb->native_offset) {
				x86_jump_code (code, cfg->native_code + ins->inst_target_bb->native_offset);
			} else {
				mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_target_bb);
				if ((cfg->opt & MONO_OPT_BRANCH) &&
				    x86_is_imm8 (ins->inst_target_bb->max_offset - cpos))
					x86_jump8 (code, 0);
				else
					x86_jump32 (code, 0);
			}
			break;
		case OP_BR_REG:
			x86_jump_reg (code, ins->sreg1);
			break;
		case OP_ICNEQ:
		case OP_ICGE:
		case OP_ICLE:
		case OP_ICGE_UN:
		case OP_ICLE_UN:

		case OP_CEQ:
		case OP_CLT:
		case OP_CLT_UN:
		case OP_CGT:
		case OP_CGT_UN:
		case OP_CNE:
		case OP_ICEQ:
		case OP_ICLT:
		case OP_ICLT_UN:
		case OP_ICGT:
		case OP_ICGT_UN:
			x86_set_reg (code, cc_table [mono_opcode_to_cond (ins->opcode)], ins->dreg, cc_signed_table [mono_opcode_to_cond (ins->opcode)]);
			x86_widen_reg (code, ins->dreg, ins->dreg, FALSE, FALSE);
			break;
		case OP_COND_EXC_EQ:
		case OP_COND_EXC_NE_UN:
		case OP_COND_EXC_LT:
		case OP_COND_EXC_LT_UN:
		case OP_COND_EXC_GT:
		case OP_COND_EXC_GT_UN:
		case OP_COND_EXC_GE:
		case OP_COND_EXC_GE_UN:
		case OP_COND_EXC_LE:
		case OP_COND_EXC_LE_UN:
		case OP_COND_EXC_IEQ:
		case OP_COND_EXC_INE_UN:
		case OP_COND_EXC_ILT:
		case OP_COND_EXC_ILT_UN:
		case OP_COND_EXC_IGT:
		case OP_COND_EXC_IGT_UN:
		case OP_COND_EXC_IGE:
		case OP_COND_EXC_IGE_UN:
		case OP_COND_EXC_ILE:
		case OP_COND_EXC_ILE_UN:
			EMIT_COND_SYSTEM_EXCEPTION (cc_table [mono_opcode_to_cond (ins->opcode)], cc_signed_table [mono_opcode_to_cond (ins->opcode)], (const char*)ins->inst_p1);
			break;
		case OP_COND_EXC_OV:
		case OP_COND_EXC_NO:
		case OP_COND_EXC_C:
		case OP_COND_EXC_NC:
			EMIT_COND_SYSTEM_EXCEPTION (branch_cc_table [ins->opcode - OP_COND_EXC_EQ], (ins->opcode < OP_COND_EXC_NE_UN), (const char*)ins->inst_p1);
			break;
		case OP_COND_EXC_IOV:
		case OP_COND_EXC_INO:
		case OP_COND_EXC_IC:
		case OP_COND_EXC_INC:
			EMIT_COND_SYSTEM_EXCEPTION (branch_cc_table [ins->opcode - OP_COND_EXC_IEQ], (ins->opcode < OP_COND_EXC_INE_UN), (const char*)ins->inst_p1);
			break;
		case OP_IBEQ:
		case OP_IBNE_UN:
		case OP_IBLT:
		case OP_IBLT_UN:
		case OP_IBGT:
		case OP_IBGT_UN:
		case OP_IBGE:
		case OP_IBGE_UN:
		case OP_IBLE:
		case OP_IBLE_UN:
			EMIT_COND_BRANCH (ins, cc_table [mono_opcode_to_cond (ins->opcode)], cc_signed_table [mono_opcode_to_cond (ins->opcode)]);
			break;

		case OP_CMOV_IEQ:
		case OP_CMOV_IGE:
		case OP_CMOV_IGT:
		case OP_CMOV_ILE:
		case OP_CMOV_ILT:
		case OP_CMOV_INE_UN:
		case OP_CMOV_IGE_UN:
		case OP_CMOV_IGT_UN:
		case OP_CMOV_ILE_UN:
		case OP_CMOV_ILT_UN:
			g_assert (ins->dreg == ins->sreg1);
			x86_cmov_reg (code, cc_table [mono_opcode_to_cond (ins->opcode)], cc_signed_table [mono_opcode_to_cond (ins->opcode)], ins->dreg, ins->sreg2);
			break;

		/* floating point opcodes */
		case OP_R8CONST: {
			double d = *(double *)ins->inst_p0;

			if ((d == 0.0) && (mono_signbit (d) == 0)) {
				x86_fldz (code);
			} else if (d == 1.0) {
				x86_fld1 (code);
			} else {
				if (cfg->compile_aot) {
					guint32 *val = (guint32*)&d;
					x86_push_imm (code, val [1]);
					x86_push_imm (code, val [0]);
					x86_fld_membase (code, X86_ESP, 0, TRUE);
					x86_alu_reg_imm (code, X86_ADD, X86_ESP, 8);
				}
				else {
					mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_R8, ins->inst_p0);
					x86_fld (code, (gsize)NULL, TRUE);
				}
			}
			break;
		}
		case OP_R4CONST: {
			float f = *(float *)ins->inst_p0;

			if ((f == 0.0) && (mono_signbit (f) == 0)) {
				x86_fldz (code);
			} else if (f == 1.0) {
				x86_fld1 (code);
			} else {
				if (cfg->compile_aot) {
					guint32 val = *(guint32*)&f;
					x86_push_imm (code, val);
					x86_fld_membase (code, X86_ESP, 0, FALSE);
					x86_alu_reg_imm (code, X86_ADD, X86_ESP, 4);
				}
				else {
					mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_R4, ins->inst_p0);
					x86_fld (code, (gsize)NULL, FALSE);
				}
			}
			break;
		}
		case OP_STORER8_MEMBASE_REG:
			x86_fst_membase (code, ins->inst_destbasereg, ins->inst_offset, TRUE, TRUE);
			break;
		case OP_LOADR8_MEMBASE:
			x86_fld_membase (code, ins->inst_basereg, ins->inst_offset, TRUE);
			break;
		case OP_STORER4_MEMBASE_REG:
			x86_fst_membase (code, ins->inst_destbasereg, ins->inst_offset, FALSE, TRUE);
			break;
		case OP_LOADR4_MEMBASE:
			x86_fld_membase (code, ins->inst_basereg, ins->inst_offset, FALSE);
			break;
		case OP_ICONV_TO_R4:
			x86_push_reg (code, ins->sreg1);
			x86_fild_membase (code, X86_ESP, 0, FALSE);
			/* Change precision */
			x86_fst_membase (code, X86_ESP, 0, FALSE, TRUE);
			x86_fld_membase (code, X86_ESP, 0, FALSE);
			x86_alu_reg_imm (code, X86_ADD, X86_ESP, 4);
			break;
		case OP_ICONV_TO_R8:
			x86_push_reg (code, ins->sreg1);
			x86_fild_membase (code, X86_ESP, 0, FALSE);
			x86_alu_reg_imm (code, X86_ADD, X86_ESP, 4);
			break;
		case OP_ICONV_TO_R_UN:
			x86_push_imm (code, 0);
			x86_push_reg (code, ins->sreg1);
			x86_fild_membase (code, X86_ESP, 0, TRUE);
			x86_alu_reg_imm (code, X86_ADD, X86_ESP, 8);
			break;
		case OP_X86_FP_LOAD_I8:
			x86_fild_membase (code, ins->inst_basereg, ins->inst_offset, TRUE);
			break;
		case OP_X86_FP_LOAD_I4:
			x86_fild_membase (code, ins->inst_basereg, ins->inst_offset, FALSE);
			break;
		case OP_FCONV_TO_R4:
			/* Change precision */
			x86_alu_reg_imm (code, X86_SUB, X86_ESP, 4);
			x86_fst_membase (code, X86_ESP, 0, FALSE, TRUE);
			x86_fld_membase (code, X86_ESP, 0, FALSE);
			x86_alu_reg_imm (code, X86_ADD, X86_ESP, 4);
			break;
		case OP_FCONV_TO_I1:
			code = emit_float_to_int (cfg, code, ins->dreg, 1, TRUE);
			break;
		case OP_FCONV_TO_U1:
			code = emit_float_to_int (cfg, code, ins->dreg, 1, FALSE);
			break;
		case OP_FCONV_TO_I2:
			code = emit_float_to_int (cfg, code, ins->dreg, 2, TRUE);
			break;
		case OP_FCONV_TO_U2:
			code = emit_float_to_int (cfg, code, ins->dreg, 2, FALSE);
			break;
		case OP_FCONV_TO_I4:
			code = emit_float_to_int (cfg, code, ins->dreg, 4, TRUE);
			break;
		case OP_FCONV_TO_I8:
			x86_alu_reg_imm (code, X86_SUB, X86_ESP, 4);
			x86_fnstcw_membase(code, X86_ESP, 0);
			x86_mov_reg_membase (code, ins->dreg, X86_ESP, 0, 2);
			x86_alu_reg_imm (code, X86_OR, ins->dreg, 0xc00);
			x86_mov_membase_reg (code, X86_ESP, 2, ins->dreg, 2);
			x86_fldcw_membase (code, X86_ESP, 2);
			x86_alu_reg_imm (code, X86_SUB, X86_ESP, 8);
			x86_fist_pop_membase (code, X86_ESP, 0, TRUE);
			x86_pop_reg (code, ins->dreg);
			x86_pop_reg (code, ins->backend.reg3);
			x86_fldcw_membase (code, X86_ESP, 0);
			x86_alu_reg_imm (code, X86_ADD, X86_ESP, 4);
			break;
		case OP_LCONV_TO_R8_2:
			x86_push_reg (code, ins->sreg2);
			x86_push_reg (code, ins->sreg1);
			x86_fild_membase (code, X86_ESP, 0, TRUE);
			/* Change precision */
			x86_fst_membase (code, X86_ESP, 0, TRUE, TRUE);
			x86_fld_membase (code, X86_ESP, 0, TRUE);
			x86_alu_reg_imm (code, X86_ADD, X86_ESP, 8);
			break;
		case OP_LCONV_TO_R4_2:
			x86_push_reg (code, ins->sreg2);
			x86_push_reg (code, ins->sreg1);
			x86_fild_membase (code, X86_ESP, 0, TRUE);
			/* Change precision */
			x86_fst_membase (code, X86_ESP, 0, FALSE, TRUE);
			x86_fld_membase (code, X86_ESP, 0, FALSE);
			x86_alu_reg_imm (code, X86_ADD, X86_ESP, 8);
			break;
		case OP_LCONV_TO_R_UN_2: {
			static guint8 mn[] = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x3f, 0x40 };
			guint8 *br;

			/* load 64bit integer to FP stack */
			x86_push_reg (code, ins->sreg2);
			x86_push_reg (code, ins->sreg1);
			x86_fild_membase (code, X86_ESP, 0, TRUE);

			/* test if lreg is negative */
			x86_test_reg_reg (code, ins->sreg2, ins->sreg2);
			br = code; x86_branch8 (code, X86_CC_GEZ, 0, TRUE);

			/* add correction constant mn */
			if (cfg->compile_aot) {
				x86_push_imm (code, (((guint32)mn [9]) << 24) | ((guint32)mn [8] << 16) | ((guint32)mn [7] << 8) | ((guint32)mn [6]));
				x86_push_imm (code, (((guint32)mn [5]) << 24) | ((guint32)mn [4] << 16) | ((guint32)mn [3] << 8) | ((guint32)mn [2]));
				x86_push_imm (code, (((guint32)mn [1]) << 24) | ((guint32)mn [0] << 16));
				x86_fld80_membase (code, X86_ESP, 2);
				x86_alu_reg_imm (code, X86_ADD, X86_ESP, 12);
			} else {
				x86_fld80_mem (code, (gsize)&mn);
			}
			x86_fp_op_reg (code, X86_FADD, 1, TRUE);

			x86_patch (br, code);

			/* Change precision */
			x86_fst_membase (code, X86_ESP, 0, TRUE, TRUE);
			x86_fld_membase (code, X86_ESP, 0, TRUE);

			x86_alu_reg_imm (code, X86_ADD, X86_ESP, 8);

			break;
		}
		case OP_LCONV_TO_OVF_I:
		case OP_LCONV_TO_OVF_I4_2: {
			guint8 *br [3], *label [1];
			MonoInst *tins;

			/*
			 * Valid ints: 0xffffffff:8000000 to 00000000:0x7f000000
			 */
			x86_test_reg_reg (code, ins->sreg1, ins->sreg1);

			/* If the low word top bit is set, see if we are negative */
			br [0] = code; x86_branch8 (code, X86_CC_LT, 0, TRUE);
			/* We are not negative (no top bit set, check for our top word to be zero */
			x86_test_reg_reg (code, ins->sreg2, ins->sreg2);
			br [1] = code; x86_branch8 (code, X86_CC_EQ, 0, TRUE);
			label [0] = code;

			/* throw exception */
			tins = mono_branch_optimize_exception_target (cfg, bb, "OverflowException");
			if (tins) {
				mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_BB, tins->inst_true_bb);
				if ((cfg->opt & MONO_OPT_BRANCH) && x86_is_imm8 (tins->inst_true_bb->max_offset - cpos))
					x86_jump8 (code, 0);
				else
					x86_jump32 (code, 0);
			} else {
				mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_EXC, "OverflowException");
				x86_jump32 (code, 0);
			}


			x86_patch (br [0], code);
			/* our top bit is set, check that top word is 0xfffffff */
			x86_alu_reg_imm (code, X86_CMP, ins->sreg2, 0xffffffff);

			x86_patch (br [1], code);
			/* nope, emit exception */
			br [2] = code; x86_branch8 (code, X86_CC_NE, 0, TRUE);
			x86_patch (br [2], label [0]);

			x86_mov_reg_reg (code, ins->dreg, ins->sreg1);
			break;
		}
		case OP_FMOVE:
			/* Not needed on the fp stack */
			break;
		case OP_MOVE_F_TO_I4:
			x86_fst_membase (code, ins->backend.spill_var->inst_basereg, ins->backend.spill_var->inst_offset, FALSE, TRUE);
			x86_mov_reg_membase (code, ins->dreg, ins->backend.spill_var->inst_basereg, ins->backend.spill_var->inst_offset, 4);
			break;
		case OP_MOVE_I4_TO_F:
			x86_mov_membase_reg (code, ins->backend.spill_var->inst_basereg, ins->backend.spill_var->inst_offset, ins->sreg1, 4);
			x86_fld_membase (code, ins->backend.spill_var->inst_basereg, ins->backend.spill_var->inst_offset, FALSE);
			break;
		case OP_FADD:
			x86_fp_op_reg (code, X86_FADD, 1, TRUE);
			break;
		case OP_FSUB:
			x86_fp_op_reg (code, X86_FSUB, 1, TRUE);
			break;
		case OP_FMUL:
			x86_fp_op_reg (code, X86_FMUL, 1, TRUE);
			break;
		case OP_FDIV:
			x86_fp_op_reg (code, X86_FDIV, 1, TRUE);
			break;
		case OP_FNEG:
			x86_fchs (code);
			break;
		case OP_ABS:
			x86_fabs (code);
			break;
		case OP_TAN: {
			/*
			 * it really doesn't make sense to inline all this code,
			 * it's here just to show that things may not be as simple
			 * as they appear.
			 */
			guchar *check_pos, *end_tan, *pop_jump;
			x86_push_reg (code, X86_EAX);
			x86_fptan (code);
			x86_fnstsw (code);
			x86_test_reg_imm (code, X86_EAX, X86_FP_C2);
			check_pos = code;
			x86_branch8 (code, X86_CC_NE, 0, FALSE);
			x86_fstp (code, 0); /* pop the 1.0 */
			end_tan = code;
			x86_jump8 (code, 0);
			x86_fldpi (code);
			x86_fp_op (code, X86_FADD, 0);
			x86_fxch (code, 1);
			x86_fprem1 (code);
			x86_fstsw (code);
			x86_test_reg_imm (code, X86_EAX, X86_FP_C2);
			pop_jump = code;
			x86_branch8 (code, X86_CC_NE, 0, FALSE);
			x86_fstp (code, 1);
			x86_fptan (code);
			x86_patch (pop_jump, code);
			x86_fstp (code, 0); /* pop the 1.0 */
			x86_patch (check_pos, code);
			x86_patch (end_tan, code);
			x86_fldz (code);
			x86_fp_op_reg (code, X86_FADD, 1, TRUE);
			x86_pop_reg (code, X86_EAX);
			break;
		}
		case OP_ATAN:
			x86_fld1 (code);
			x86_fpatan (code);
			x86_fldz (code);
			x86_fp_op_reg (code, X86_FADD, 1, TRUE);
			break;
		case OP_SQRT:
			x86_fsqrt (code);
			break;
		case OP_ROUND:
			x86_frndint (code);
			break;
		case OP_IMIN:
			g_assert (cfg->opt & MONO_OPT_CMOV);
			g_assert (ins->dreg == ins->sreg1);
			x86_alu_reg_reg (code, X86_CMP, ins->sreg1, ins->sreg2);
			x86_cmov_reg (code, X86_CC_GT, TRUE, ins->dreg, ins->sreg2);
			break;
		case OP_IMIN_UN:
			g_assert (cfg->opt & MONO_OPT_CMOV);
			g_assert (ins->dreg == ins->sreg1);
			x86_alu_reg_reg (code, X86_CMP, ins->sreg1, ins->sreg2);
			x86_cmov_reg (code, X86_CC_GT, FALSE, ins->dreg, ins->sreg2);
			break;
		case OP_IMAX:
			g_assert (cfg->opt & MONO_OPT_CMOV);
			g_assert (ins->dreg == ins->sreg1);
			x86_alu_reg_reg (code, X86_CMP, ins->sreg1, ins->sreg2);
			x86_cmov_reg (code, X86_CC_LT, TRUE, ins->dreg, ins->sreg2);
			break;
		case OP_IMAX_UN:
			g_assert (cfg->opt & MONO_OPT_CMOV);
			g_assert (ins->dreg == ins->sreg1);
			x86_alu_reg_reg (code, X86_CMP, ins->sreg1, ins->sreg2);
			x86_cmov_reg (code, X86_CC_LT, FALSE, ins->dreg, ins->sreg2);
			break;
		case OP_X86_FPOP:
			x86_fstp (code, 0);
			break;
		case OP_X86_FXCH:
			x86_fxch (code, ins->inst_imm);
			break;
		case OP_FREM: {
			guint8 *l1, *l2;

			x86_push_reg (code, X86_EAX);
			/* we need to exchange ST(0) with ST(1) */
			x86_fxch (code, 1);

			/* this requires a loop, because fprem somtimes
			 * returns a partial remainder */
			l1 = code;
			/* looks like MS is using fprem instead of the IEEE compatible fprem1 */
			/* x86_fprem1 (code); */
			x86_fprem (code);
			x86_fnstsw (code);
			x86_alu_reg_imm (code, X86_AND, X86_EAX, X86_FP_C2);
			l2 = code;
			x86_branch8 (code, X86_CC_NE, 0, FALSE);
			x86_patch (l2, l1);

			/* pop result */
			x86_fstp (code, 1);

			x86_pop_reg (code, X86_EAX);
			break;
		}
		case OP_FCOMPARE:
			if (cfg->opt & MONO_OPT_FCMOV) {
				x86_fcomip (code, 1);
				x86_fstp (code, 0);
				break;
			}
			/* this overwrites EAX */
			EMIT_FPCOMPARE(code);
			x86_alu_reg_imm (code, X86_AND, X86_EAX, X86_FP_CC_MASK);
			break;
		case OP_FCEQ:
		case OP_FCNEQ:
			if (cfg->opt & MONO_OPT_FCMOV) {
				/* zeroing the register at the start results in
				 * shorter and faster code (we can also remove the widening op)
				 */
				guchar *unordered_check;
				x86_alu_reg_reg (code, X86_XOR, ins->dreg, ins->dreg);
				x86_fcomip (code, 1);
				x86_fstp (code, 0);
				unordered_check = code;
				x86_branch8 (code, X86_CC_P, 0, FALSE);
				if (ins->opcode == OP_FCEQ) {
					x86_set_reg (code, X86_CC_EQ, ins->dreg, FALSE);
					x86_patch (unordered_check, code);
				} else {
					guchar *jump_to_end;
					x86_set_reg (code, X86_CC_NE, ins->dreg, FALSE);
					jump_to_end = code;
					x86_jump8 (code, 0);
					x86_patch (unordered_check, code);
					x86_inc_reg (code, ins->dreg);
					x86_patch (jump_to_end, code);
				}

				break;
			}
			if (ins->dreg != X86_EAX)
				x86_push_reg (code, X86_EAX);

			EMIT_FPCOMPARE(code);
			x86_alu_reg_imm (code, X86_AND, X86_EAX, X86_FP_CC_MASK);
			x86_alu_reg_imm (code, X86_CMP, X86_EAX, 0x4000);
			x86_set_reg (code, ins->opcode == OP_FCEQ ? X86_CC_EQ : X86_CC_NE, ins->dreg, TRUE);
			x86_widen_reg (code, ins->dreg, ins->dreg, FALSE, FALSE);

			if (ins->dreg != X86_EAX)
				x86_pop_reg (code, X86_EAX);
			break;
		case OP_FCLT:
		case OP_FCLT_UN:
			if (cfg->opt & MONO_OPT_FCMOV) {
				/* zeroing the register at the start results in
				 * shorter and faster code (we can also remove the widening op)
				 */
				x86_alu_reg_reg (code, X86_XOR, ins->dreg, ins->dreg);
				x86_fcomip (code, 1);
				x86_fstp (code, 0);
				if (ins->opcode == OP_FCLT_UN) {
					guchar *unordered_check = code;
					guchar *jump_to_end;
					x86_branch8 (code, X86_CC_P, 0, FALSE);
					x86_set_reg (code, X86_CC_GT, ins->dreg, FALSE);
					jump_to_end = code;
					x86_jump8 (code, 0);
					x86_patch (unordered_check, code);
					x86_inc_reg (code, ins->dreg);
					x86_patch (jump_to_end, code);
				} else {
					x86_set_reg (code, X86_CC_GT, ins->dreg, FALSE);
				}
				break;
			}
			if (ins->dreg != X86_EAX)
				x86_push_reg (code, X86_EAX);

			EMIT_FPCOMPARE(code);
			x86_alu_reg_imm (code, X86_AND, X86_EAX, X86_FP_CC_MASK);
			if (ins->opcode == OP_FCLT_UN) {
				guchar *is_not_zero_check, *end_jump;
				is_not_zero_check = code;
				x86_branch8 (code, X86_CC_NZ, 0, TRUE);
				end_jump = code;
				x86_jump8 (code, 0);
				x86_patch (is_not_zero_check, code);
				x86_alu_reg_imm (code, X86_CMP, X86_EAX, X86_FP_CC_MASK);

				x86_patch (end_jump, code);
			}
			x86_set_reg (code, X86_CC_EQ, ins->dreg, TRUE);
			x86_widen_reg (code, ins->dreg, ins->dreg, FALSE, FALSE);

			if (ins->dreg != X86_EAX)
				x86_pop_reg (code, X86_EAX);
			break;
		case OP_FCLE: {
			guchar *unordered_check;
			guchar *jump_to_end;
			if (cfg->opt & MONO_OPT_FCMOV) {
				/* zeroing the register at the start results in
				 * shorter and faster code (we can also remove the widening op)
				 */
				x86_alu_reg_reg (code, X86_XOR, ins->dreg, ins->dreg);
				x86_fcomip (code, 1);
				x86_fstp (code, 0);
				unordered_check = code;
				x86_branch8 (code, X86_CC_P, 0, FALSE);
				x86_set_reg (code, X86_CC_NB, ins->dreg, FALSE);
				x86_patch (unordered_check, code);
				break;
			}
			if (ins->dreg != X86_EAX)
				x86_push_reg (code, X86_EAX);

			EMIT_FPCOMPARE(code);
			x86_alu_reg_imm (code, X86_AND, X86_EAX, X86_FP_CC_MASK);
			x86_alu_reg_imm (code, X86_CMP, X86_EAX, 0x4500);
			unordered_check = code;
			x86_branch8 (code, X86_CC_EQ, 0, FALSE);

			x86_alu_reg_imm (code, X86_CMP, X86_EAX, X86_FP_C0);
			x86_set_reg (code, X86_CC_NE, ins->dreg, TRUE);
			x86_widen_reg (code, ins->dreg, ins->dreg, FALSE, FALSE);
			jump_to_end = code;
			x86_jump8 (code, 0);
			x86_patch (unordered_check, code);
			x86_alu_reg_reg (code, X86_XOR, ins->dreg, ins->dreg);
			x86_patch (jump_to_end, code);

			if (ins->dreg != X86_EAX)
				x86_pop_reg (code, X86_EAX);
			break;
		}
		case OP_FCGT:
		case OP_FCGT_UN:
			if (cfg->opt & MONO_OPT_FCMOV) {
				/* zeroing the register at the start results in
				 * shorter and faster code (we can also remove the widening op)
				 */
				guchar *unordered_check;
				x86_alu_reg_reg (code, X86_XOR, ins->dreg, ins->dreg);
				x86_fcomip (code, 1);
				x86_fstp (code, 0);
				if (ins->opcode == OP_FCGT) {
					unordered_check = code;
					x86_branch8 (code, X86_CC_P, 0, FALSE);
					x86_set_reg (code, X86_CC_LT, ins->dreg, FALSE);
					x86_patch (unordered_check, code);
				} else {
					x86_set_reg (code, X86_CC_LT, ins->dreg, FALSE);
				}
				break;
			}
			if (ins->dreg != X86_EAX)
				x86_push_reg (code, X86_EAX);

			EMIT_FPCOMPARE(code);
			x86_alu_reg_imm (code, X86_AND, X86_EAX, X86_FP_CC_MASK);
			x86_alu_reg_imm (code, X86_CMP, X86_EAX, X86_FP_C0);
			if (ins->opcode == OP_FCGT_UN) {
				guchar *is_not_zero_check, *end_jump;
				is_not_zero_check = code;
				x86_branch8 (code, X86_CC_NZ, 0, TRUE);
				end_jump = code;
				x86_jump8 (code, 0);
				x86_patch (is_not_zero_check, code);
				x86_alu_reg_imm (code, X86_CMP, X86_EAX, X86_FP_CC_MASK);

				x86_patch (end_jump, code);
			}
			x86_set_reg (code, X86_CC_EQ, ins->dreg, TRUE);
			x86_widen_reg (code, ins->dreg, ins->dreg, FALSE, FALSE);

			if (ins->dreg != X86_EAX)
				x86_pop_reg (code, X86_EAX);
			break;
		case OP_FCGE: {
			guchar *unordered_check;
			guchar *jump_to_end;
			if (cfg->opt & MONO_OPT_FCMOV) {
				/* zeroing the register at the start results in
				 * shorter and faster code (we can also remove the widening op)
				 */
				x86_alu_reg_reg (code, X86_XOR, ins->dreg, ins->dreg);
				x86_fcomip (code, 1);
				x86_fstp (code, 0);
				unordered_check = code;
				x86_branch8 (code, X86_CC_P, 0, FALSE);
				x86_set_reg (code, X86_CC_NA, ins->dreg, FALSE);
				x86_patch (unordered_check, code);
				break;
			}
			if (ins->dreg != X86_EAX)
				x86_push_reg (code, X86_EAX);

			EMIT_FPCOMPARE(code);
			x86_alu_reg_imm (code, X86_AND, X86_EAX, X86_FP_CC_MASK);
			x86_alu_reg_imm (code, X86_CMP, X86_EAX, 0x4500);
			unordered_check = code;
			x86_branch8 (code, X86_CC_EQ, 0, FALSE);

			x86_alu_reg_imm (code, X86_CMP, X86_EAX, X86_FP_C0);
			x86_set_reg (code, X86_CC_GE, ins->dreg, TRUE);
			x86_widen_reg (code, ins->dreg, ins->dreg, FALSE, FALSE);
			jump_to_end = code;
			x86_jump8 (code, 0);
			x86_patch (unordered_check, code);
			x86_alu_reg_reg (code, X86_XOR, ins->dreg, ins->dreg);
			x86_patch (jump_to_end, code);

			if (ins->dreg != X86_EAX)
				x86_pop_reg (code, X86_EAX);
			break;
		}
		case OP_FBEQ:
			if (cfg->opt & MONO_OPT_FCMOV) {
				guchar *jump = code;
				x86_branch8 (code, X86_CC_P, 0, TRUE);
				EMIT_COND_BRANCH (ins, X86_CC_EQ, FALSE);
				x86_patch (jump, code);
				break;
			}
			x86_alu_reg_imm (code, X86_CMP, X86_EAX, 0x4000);
			EMIT_COND_BRANCH (ins, X86_CC_EQ, TRUE);
			break;
		case OP_FBNE_UN:
			/* Branch if C013 != 100 */
			if (cfg->opt & MONO_OPT_FCMOV) {
				/* branch if !ZF or (PF|CF) */
				EMIT_COND_BRANCH (ins, X86_CC_NE, FALSE);
				EMIT_COND_BRANCH (ins, X86_CC_P, FALSE);
				EMIT_COND_BRANCH (ins, X86_CC_B, FALSE);
				break;
			}
			x86_alu_reg_imm (code, X86_CMP, X86_EAX, X86_FP_C3);
			EMIT_COND_BRANCH (ins, X86_CC_NE, FALSE);
			break;
		case OP_FBLT:
			if (cfg->opt & MONO_OPT_FCMOV) {
				EMIT_COND_BRANCH (ins, X86_CC_GT, FALSE);
				break;
			}
			EMIT_COND_BRANCH (ins, X86_CC_EQ, FALSE);
			break;
		case OP_FBLT_UN:
			if (cfg->opt & MONO_OPT_FCMOV) {
				EMIT_COND_BRANCH (ins, X86_CC_P, FALSE);
				EMIT_COND_BRANCH (ins, X86_CC_GT, FALSE);
				break;
			}
			if (ins->opcode == OP_FBLT_UN) {
				guchar *is_not_zero_check, *end_jump;
				is_not_zero_check = code;
				x86_branch8 (code, X86_CC_NZ, 0, TRUE);
				end_jump = code;
				x86_jump8 (code, 0);
				x86_patch (is_not_zero_check, code);
				x86_alu_reg_imm (code, X86_CMP, X86_EAX, X86_FP_CC_MASK);

				x86_patch (end_jump, code);
			}
			EMIT_COND_BRANCH (ins, X86_CC_EQ, FALSE);
			break;
		case OP_FBGT:
		case OP_FBGT_UN:
			if (cfg->opt & MONO_OPT_FCMOV) {
				if (ins->opcode == OP_FBGT) {
					guchar *br1;

					/* skip branch if C1=1 */
					br1 = code;
					x86_branch8 (code, X86_CC_P, 0, FALSE);
					/* branch if (C0 | C3) = 1 */
					EMIT_COND_BRANCH (ins, X86_CC_LT, FALSE);
					x86_patch (br1, code);
				} else {
					EMIT_COND_BRANCH (ins, X86_CC_LT, FALSE);
				}
				break;
			}
			x86_alu_reg_imm (code, X86_CMP, X86_EAX, X86_FP_C0);
			if (ins->opcode == OP_FBGT_UN) {
				guchar *is_not_zero_check, *end_jump;
				is_not_zero_check = code;
				x86_branch8 (code, X86_CC_NZ, 0, TRUE);
				end_jump = code;
				x86_jump8 (code, 0);
				x86_patch (is_not_zero_check, code);
				x86_alu_reg_imm (code, X86_CMP, X86_EAX, X86_FP_CC_MASK);

				x86_patch (end_jump, code);
			}
			EMIT_COND_BRANCH (ins, X86_CC_EQ, FALSE);
			break;
		case OP_FBGE:
			/* Branch if C013 == 100 or 001 */
			if (cfg->opt & MONO_OPT_FCMOV) {
				guchar *br1;

				/* skip branch if C1=1 */
				br1 = code;
				x86_branch8 (code, X86_CC_P, 0, FALSE);
				/* branch if (C0 | C3) = 1 */
				EMIT_COND_BRANCH (ins, X86_CC_BE, FALSE);
				x86_patch (br1, code);
				break;
			}
			x86_alu_reg_imm (code, X86_CMP, X86_EAX, X86_FP_C0);
			EMIT_COND_BRANCH (ins, X86_CC_EQ, FALSE);
			x86_alu_reg_imm (code, X86_CMP, X86_EAX, X86_FP_C3);
			EMIT_COND_BRANCH (ins, X86_CC_EQ, FALSE);
			break;
		case OP_FBGE_UN:
			/* Branch if C013 == 000 */
			if (cfg->opt & MONO_OPT_FCMOV) {
				EMIT_COND_BRANCH (ins, X86_CC_LE, FALSE);
				break;
			}
			EMIT_COND_BRANCH (ins, X86_CC_NE, FALSE);
			break;
		case OP_FBLE:
			/* Branch if C013=000 or 100 */
			if (cfg->opt & MONO_OPT_FCMOV) {
				guchar *br1;

				/* skip branch if C1=1 */
				br1 = code;
				x86_branch8 (code, X86_CC_P, 0, FALSE);
				/* branch if C0=0 */
				EMIT_COND_BRANCH (ins, X86_CC_NB, FALSE);
				x86_patch (br1, code);
				break;
			}
			x86_alu_reg_imm (code, X86_AND, X86_EAX, (X86_FP_C0|X86_FP_C1));
			x86_alu_reg_imm (code, X86_CMP, X86_EAX, 0);
			EMIT_COND_BRANCH (ins, X86_CC_EQ, FALSE);
			break;
		case OP_FBLE_UN:
			/* Branch if C013 != 001 */
			if (cfg->opt & MONO_OPT_FCMOV) {
				EMIT_COND_BRANCH (ins, X86_CC_P, FALSE);
				EMIT_COND_BRANCH (ins, X86_CC_GE, FALSE);
				break;
			}
			x86_alu_reg_imm (code, X86_CMP, X86_EAX, X86_FP_C0);
			EMIT_COND_BRANCH (ins, X86_CC_NE, FALSE);
			break;
		case OP_CKFINITE: {
			guchar *br1;
			x86_push_reg (code, X86_EAX);
			x86_fxam (code);
			x86_fnstsw (code);
			x86_alu_reg_imm (code, X86_AND, X86_EAX, 0x4100);
			x86_alu_reg_imm (code, X86_CMP, X86_EAX, X86_FP_C0);
			x86_pop_reg (code, X86_EAX);

			/* Have to clean up the fp stack before throwing the exception */
			br1 = code;
			x86_branch8 (code, X86_CC_NE, 0, FALSE);

			x86_fstp (code, 0);
			EMIT_COND_SYSTEM_EXCEPTION (X86_CC_EQ, FALSE, "OverflowException");

			x86_patch (br1, code);
			break;
		}
		case OP_TLS_GET: {
			code = mono_x86_emit_tls_get (code, ins->dreg, ins->inst_offset);
			break;
		}
		case OP_TLS_SET: {
			code = mono_x86_emit_tls_set (code, ins->sreg1, ins->inst_offset);
			break;
		}
		case OP_MEMORY_BARRIER: {
			if (ins->backend.memory_barrier_kind == MONO_MEMORY_BARRIER_SEQ) {
				x86_prefix (code, X86_LOCK_PREFIX);
				x86_alu_membase_imm (code, X86_ADD, X86_ESP, 0, 0);
			}
			break;
		}
		case OP_ATOMIC_ADD_I4: {
			int dreg = ins->dreg;

			g_assert (cfg->has_atomic_add_i4);

			/* hack: limit in regalloc, dreg != sreg1 && dreg != sreg2 */
			if (ins->sreg2 == dreg) {
				if (dreg == X86_EBX) {
					dreg = X86_EDI;
					if (ins->inst_basereg == X86_EDI)
						dreg = X86_ESI;
				} else {
					dreg = X86_EBX;
					if (ins->inst_basereg == X86_EBX)
						dreg = X86_EDI;
				}
			} else if (ins->inst_basereg == dreg) {
				if (dreg == X86_EBX) {
					dreg = X86_EDI;
					if (ins->sreg2 == X86_EDI)
						dreg = X86_ESI;
				} else {
					dreg = X86_EBX;
					if (ins->sreg2 == X86_EBX)
						dreg = X86_EDI;
				}
			}

			if (dreg != ins->dreg) {
				x86_push_reg (code, dreg);
			}

			x86_mov_reg_reg (code, dreg, ins->sreg2);
			x86_prefix (code, X86_LOCK_PREFIX);
			x86_xadd_membase_reg (code, ins->inst_basereg, ins->inst_offset, dreg, 4);
			/* dreg contains the old value, add with sreg2 value */
			x86_alu_reg_reg (code, X86_ADD, dreg, ins->sreg2);

			if (ins->dreg != dreg) {
				x86_mov_reg_reg (code, ins->dreg, dreg);
				x86_pop_reg (code, dreg);
			}

			break;
		}
		case OP_ATOMIC_EXCHANGE_I4: {
			guchar *br[2];
			int sreg2 = ins->sreg2;
			int breg = ins->inst_basereg;

			g_assert (cfg->has_atomic_exchange_i4);

			/* cmpxchg uses eax as comperand, need to make sure we can use it
			 * hack to overcome limits in x86 reg allocator
			 * (req: dreg == eax and sreg2 != eax and breg != eax)
			 */
			g_assert (ins->dreg == X86_EAX);

			/* We need the EAX reg for the cmpxchg */
			if (ins->sreg2 == X86_EAX) {
				sreg2 = (breg == X86_EDX) ? X86_EBX : X86_EDX;
				x86_push_reg (code, sreg2);
				x86_mov_reg_reg (code, sreg2, X86_EAX);
			}

			if (breg == X86_EAX) {
				breg = (sreg2 == X86_ESI) ? X86_EDI : X86_ESI;
				x86_push_reg (code, breg);
				x86_mov_reg_reg (code, breg, X86_EAX);
			}

			x86_mov_reg_membase (code, X86_EAX, breg, ins->inst_offset, 4);

			br [0] = code; x86_prefix (code, X86_LOCK_PREFIX);
			x86_cmpxchg_membase_reg (code, breg, ins->inst_offset, sreg2);
			br [1] = code; x86_branch8 (code, X86_CC_NE, -1, FALSE);
			x86_patch (br [1], br [0]);

			if (breg != ins->inst_basereg)
				x86_pop_reg (code, breg);

			if (ins->sreg2 != sreg2)
				x86_pop_reg (code, sreg2);

			break;
		}
		case OP_ATOMIC_CAS_I4: {
			g_assert (ins->dreg == X86_EAX);
			g_assert (ins->sreg3 == X86_EAX);
			g_assert (ins->sreg1 != X86_EAX);
			g_assert (ins->sreg1 != ins->sreg2);

			x86_prefix (code, X86_LOCK_PREFIX);
			x86_cmpxchg_membase_reg (code, ins->sreg1, ins->inst_offset, ins->sreg2);
			break;
		}
		case OP_ATOMIC_LOAD_I1: {
			x86_widen_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset, TRUE, FALSE);
			break;
		}
		case OP_ATOMIC_LOAD_U1: {
			x86_widen_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset, FALSE, FALSE);
			break;
		}
		case OP_ATOMIC_LOAD_I2: {
			x86_widen_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset, TRUE, TRUE);
			break;
		}
		case OP_ATOMIC_LOAD_U2: {
			x86_widen_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset, FALSE, TRUE);
			break;
		}
		case OP_ATOMIC_LOAD_I4:
		case OP_ATOMIC_LOAD_U4: {
			x86_mov_reg_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset, 4);
			break;
		}
		case OP_ATOMIC_LOAD_R4:
		case OP_ATOMIC_LOAD_R8: {
			x86_fld_membase (code, ins->inst_basereg, ins->inst_offset, ins->opcode == OP_ATOMIC_LOAD_R8);
			break;
		}
		case OP_ATOMIC_STORE_I1:
		case OP_ATOMIC_STORE_U1:
		case OP_ATOMIC_STORE_I2:
		case OP_ATOMIC_STORE_U2:
		case OP_ATOMIC_STORE_I4:
		case OP_ATOMIC_STORE_U4: {
			int size;

			switch (ins->opcode) {
			case OP_ATOMIC_STORE_I1:
			case OP_ATOMIC_STORE_U1:
				size = 1;
				break;
			case OP_ATOMIC_STORE_I2:
			case OP_ATOMIC_STORE_U2:
				size = 2;
				break;
			case OP_ATOMIC_STORE_I4:
			case OP_ATOMIC_STORE_U4:
				size = 4;
				break;
			default:
				size = 0;
				g_assert_not_reached ();
			}

			x86_mov_membase_reg (code, ins->inst_destbasereg, ins->inst_offset, ins->sreg1, size);

			if (ins->backend.memory_barrier_kind == MONO_MEMORY_BARRIER_SEQ)
				x86_mfence (code);
			break;
		}
		case OP_ATOMIC_STORE_R4:
		case OP_ATOMIC_STORE_R8: {
			x86_fst_membase (code, ins->inst_destbasereg, ins->inst_offset, ins->opcode == OP_ATOMIC_STORE_R8, TRUE);

			if (ins->backend.memory_barrier_kind == MONO_MEMORY_BARRIER_SEQ)
				x86_mfence (code);
			break;
		}
		case OP_CARD_TABLE_WBARRIER: {
			int ptr = ins->sreg1;
			int value = ins->sreg2;
			guchar *br = NULL;
			int nursery_shift, card_table_shift;
			gpointer card_table_mask;
			size_t nursery_size;
			gulong card_table = (gulong)(gsize)mono_gc_get_card_table (&card_table_shift, &card_table_mask);
			gulong nursery_start = (gulong)(gsize)mono_gc_get_nursery (&nursery_shift, &nursery_size);
			gboolean card_table_nursery_check = mono_gc_card_table_nursery_check ();

			/*
			 * We need one register we can clobber, we choose EDX and make sreg1
			 * fixed EAX to work around limitations in the local register allocator.
			 * sreg2 might get allocated to EDX, but that is not a problem since
			 * we use it before clobbering EDX.
			 */
			g_assert (ins->sreg1 == X86_EAX);

			/*
			 * This is the code we produce:
			 *
			 *   edx = value
			 *   edx >>= nursery_shift
			 *   cmp edx, (nursery_start >> nursery_shift)
			 *   jne done
			 *   edx = ptr
			 *   edx >>= card_table_shift
			 *   card_table[edx] = 1
			 * done:
			 */

			if (card_table_nursery_check) {
				if (value != X86_EDX)
					x86_mov_reg_reg (code, X86_EDX, value);
				x86_shift_reg_imm (code, X86_SHR, X86_EDX, nursery_shift);
				x86_alu_reg_imm (code, X86_CMP, X86_EDX, nursery_start >> nursery_shift);
				br = code; x86_branch8 (code, X86_CC_NE, -1, FALSE);
			}
			x86_mov_reg_reg (code, X86_EDX, ptr);
			x86_shift_reg_imm (code, X86_SHR, X86_EDX, card_table_shift);
			if (card_table_mask)
				x86_alu_reg_imm (code, X86_AND, X86_EDX, (gsize)card_table_mask);
			x86_mov_membase_imm (code, X86_EDX, card_table, 1, 1);
			if (card_table_nursery_check)
				x86_patch (br, code);
			break;
		}
#ifdef MONO_ARCH_SIMD_INTRINSICS
		case OP_ADDPS:
			x86_sse_alu_ps_reg_reg (code, X86_SSE_ADD, ins->sreg1, ins->sreg2);
			break;
		case OP_DIVPS:
			x86_sse_alu_ps_reg_reg (code, X86_SSE_DIV, ins->sreg1, ins->sreg2);
			break;
		case OP_MULPS:
			x86_sse_alu_ps_reg_reg (code, X86_SSE_MUL, ins->sreg1, ins->sreg2);
			break;
		case OP_SUBPS:
			x86_sse_alu_ps_reg_reg (code, X86_SSE_SUB, ins->sreg1, ins->sreg2);
			break;
		case OP_MAXPS:
			x86_sse_alu_ps_reg_reg (code, X86_SSE_MAX, ins->sreg1, ins->sreg2);
			break;
		case OP_MINPS:
			x86_sse_alu_ps_reg_reg (code, X86_SSE_MIN, ins->sreg1, ins->sreg2);
			break;
		case OP_COMPPS:
			g_assert (ins->inst_c0 >= 0 && ins->inst_c0 <= 7);
			x86_sse_alu_ps_reg_reg_imm (code, X86_SSE_COMP, ins->sreg1, ins->sreg2, ins->inst_c0);
			break;
		case OP_ANDPS:
			x86_sse_alu_ps_reg_reg (code, X86_SSE_AND, ins->sreg1, ins->sreg2);
			break;
		case OP_ANDNPS:
			x86_sse_alu_ps_reg_reg (code, X86_SSE_ANDN, ins->sreg1, ins->sreg2);
			break;
		case OP_ORPS:
			x86_sse_alu_ps_reg_reg (code, X86_SSE_OR, ins->sreg1, ins->sreg2);
			break;
		case OP_XORPS:
			x86_sse_alu_ps_reg_reg (code, X86_SSE_XOR, ins->sreg1, ins->sreg2);
			break;
		case OP_SQRTPS:
			x86_sse_alu_ps_reg_reg (code, X86_SSE_SQRT, ins->dreg, ins->sreg1);
			break;
		case OP_RSQRTPS:
			x86_sse_alu_ps_reg_reg (code, X86_SSE_RSQRT, ins->dreg, ins->sreg1);
			break;
		case OP_RCPPS:
			x86_sse_alu_ps_reg_reg (code, X86_SSE_RCP, ins->dreg, ins->sreg1);
			break;
		case OP_ADDSUBPS:
			x86_sse_alu_sd_reg_reg (code, X86_SSE_ADDSUB, ins->sreg1, ins->sreg2);
			break;
		case OP_HADDPS:
			x86_sse_alu_sd_reg_reg (code, X86_SSE_HADD, ins->sreg1, ins->sreg2);
			break;
		case OP_HSUBPS:
			x86_sse_alu_sd_reg_reg (code, X86_SSE_HSUB, ins->sreg1, ins->sreg2);
			break;
		case OP_DUPPS_HIGH:
			x86_sse_alu_ss_reg_reg (code, X86_SSE_MOVSHDUP, ins->dreg, ins->sreg1);
			break;
		case OP_DUPPS_LOW:
			x86_sse_alu_ss_reg_reg (code, X86_SSE_MOVSLDUP, ins->dreg, ins->sreg1);
			break;

		case OP_PSHUFLEW_HIGH:
			g_assert (ins->inst_c0 >= 0 && ins->inst_c0 <= 0xFF);
			x86_pshufw_reg_reg (code, ins->dreg, ins->sreg1, ins->inst_c0, 1);
			break;
		case OP_PSHUFLEW_LOW:
			g_assert (ins->inst_c0 >= 0 && ins->inst_c0 <= 0xFF);
			x86_pshufw_reg_reg (code, ins->dreg, ins->sreg1, ins->inst_c0, 0);
			break;
		case OP_PSHUFLED:
			g_assert (ins->inst_c0 >= 0 && ins->inst_c0 <= 0xFF);
			x86_sse_shift_reg_imm (code, X86_SSE_PSHUFD, ins->dreg, ins->sreg1, ins->inst_c0);
			break;
		case OP_SHUFPS:
			g_assert (ins->inst_c0 >= 0 && ins->inst_c0 <= 0xFF);
			x86_sse_alu_reg_reg_imm8 (code, X86_SSE_SHUFP, ins->sreg1, ins->sreg2, ins->inst_c0);
			break;
		case OP_SHUFPD:
			g_assert (ins->inst_c0 >= 0 && ins->inst_c0 <= 0x3);
			x86_sse_alu_pd_reg_reg_imm8 (code, X86_SSE_SHUFP, ins->sreg1, ins->sreg2, ins->inst_c0);
			break;

		case OP_ADDPD:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_ADD, ins->sreg1, ins->sreg2);
			break;
		case OP_DIVPD:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_DIV, ins->sreg1, ins->sreg2);
			break;
		case OP_MULPD:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_MUL, ins->sreg1, ins->sreg2);
			break;
		case OP_SUBPD:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_SUB, ins->sreg1, ins->sreg2);
			break;
		case OP_MAXPD:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_MAX, ins->sreg1, ins->sreg2);
			break;
		case OP_MINPD:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_MIN, ins->sreg1, ins->sreg2);
			break;
		case OP_COMPPD:
			g_assert (ins->inst_c0 >= 0 && ins->inst_c0 <= 7);
			x86_sse_alu_pd_reg_reg_imm (code, X86_SSE_COMP, ins->sreg1, ins->sreg2, ins->inst_c0);
			break;
		case OP_ANDPD:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_AND, ins->sreg1, ins->sreg2);
			break;
		case OP_ANDNPD:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_ANDN, ins->sreg1, ins->sreg2);
			break;
		case OP_ORPD:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_OR, ins->sreg1, ins->sreg2);
			break;
		case OP_XORPD:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_XOR, ins->sreg1, ins->sreg2);
			break;
		case OP_SQRTPD:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_SQRT, ins->dreg, ins->sreg1);
			break;
		case OP_ADDSUBPD:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_ADDSUB, ins->sreg1, ins->sreg2);
			break;
		case OP_HADDPD:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_HADD, ins->sreg1, ins->sreg2);
			break;
		case OP_HSUBPD:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_HSUB, ins->sreg1, ins->sreg2);
			break;
		case OP_DUPPD:
			x86_sse_alu_sd_reg_reg (code, X86_SSE_MOVDDUP, ins->dreg, ins->sreg1);
			break;

		case OP_EXTRACT_MASK:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PMOVMSKB, ins->dreg, ins->sreg1);
			break;

		case OP_PAND:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PAND, ins->sreg1, ins->sreg2);
			break;
		case OP_POR:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_POR, ins->sreg1, ins->sreg2);
			break;
		case OP_PXOR:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PXOR, ins->sreg1, ins->sreg2);
			break;

		case OP_PADDB:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PADDB, ins->sreg1, ins->sreg2);
			break;
		case OP_PADDW:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PADDW, ins->sreg1, ins->sreg2);
			break;
		case OP_PADDD:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PADDD, ins->sreg1, ins->sreg2);
			break;
		case OP_PADDQ:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PADDQ, ins->sreg1, ins->sreg2);
			break;

		case OP_PSUBB:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PSUBB, ins->sreg1, ins->sreg2);
			break;
		case OP_PSUBW:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PSUBW, ins->sreg1, ins->sreg2);
			break;
		case OP_PSUBD:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PSUBD, ins->sreg1, ins->sreg2);
			break;
		case OP_PSUBQ:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PSUBQ, ins->sreg1, ins->sreg2);
			break;

		case OP_PMAXB_UN:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PMAXUB, ins->sreg1, ins->sreg2);
			break;
		case OP_PMAXW_UN:
			x86_sse_alu_sse41_reg_reg (code, X86_SSE_PMAXUW, ins->sreg1, ins->sreg2);
			break;
		case OP_PMAXD_UN:
			x86_sse_alu_sse41_reg_reg (code, X86_SSE_PMAXUD, ins->sreg1, ins->sreg2);
			break;

		case OP_PMAXB:
			x86_sse_alu_sse41_reg_reg (code, X86_SSE_PMAXSB, ins->sreg1, ins->sreg2);
			break;
		case OP_PMAXW:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PMAXSW, ins->sreg1, ins->sreg2);
			break;
		case OP_PMAXD:
			x86_sse_alu_sse41_reg_reg (code, X86_SSE_PMAXSD, ins->sreg1, ins->sreg2);
			break;

		case OP_PAVGB_UN:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PAVGB, ins->sreg1, ins->sreg2);
			break;
		case OP_PAVGW_UN:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PAVGW, ins->sreg1, ins->sreg2);
			break;

		case OP_PMINB_UN:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PMINUB, ins->sreg1, ins->sreg2);
			break;
		case OP_PMINW_UN:
			x86_sse_alu_sse41_reg_reg (code, X86_SSE_PMINUW, ins->sreg1, ins->sreg2);
			break;
		case OP_PMIND_UN:
			x86_sse_alu_sse41_reg_reg (code, X86_SSE_PMINUD, ins->sreg1, ins->sreg2);
			break;

		case OP_PMINB:
			x86_sse_alu_sse41_reg_reg (code, X86_SSE_PMINSB, ins->sreg1, ins->sreg2);
			break;
		case OP_PMINW:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PMINSW, ins->sreg1, ins->sreg2);
			break;
		case OP_PMIND:
			x86_sse_alu_sse41_reg_reg (code, X86_SSE_PMINSD, ins->sreg1, ins->sreg2);
			break;

		case OP_PCMPEQB:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PCMPEQB, ins->sreg1, ins->sreg2);
			break;
		case OP_PCMPEQW:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PCMPEQW, ins->sreg1, ins->sreg2);
			break;
		case OP_PCMPEQD:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PCMPEQD, ins->sreg1, ins->sreg2);
			break;
		case OP_PCMPEQQ:
			x86_sse_alu_sse41_reg_reg (code, X86_SSE_PCMPEQQ, ins->sreg1, ins->sreg2);
			break;

		case OP_PCMPGTB:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PCMPGTB, ins->sreg1, ins->sreg2);
			break;
		case OP_PCMPGTW:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PCMPGTW, ins->sreg1, ins->sreg2);
			break;
		case OP_PCMPGTD:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PCMPGTD, ins->sreg1, ins->sreg2);
			break;
		case OP_PCMPGTQ:
			x86_sse_alu_sse41_reg_reg (code, X86_SSE_PCMPGTQ, ins->sreg1, ins->sreg2);
			break;

		case OP_PSUM_ABS_DIFF:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PSADBW, ins->sreg1, ins->sreg2);
			break;

		case OP_UNPACK_LOWB:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PUNPCKLBW, ins->sreg1, ins->sreg2);
			break;
		case OP_UNPACK_LOWW:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PUNPCKLWD, ins->sreg1, ins->sreg2);
			break;
		case OP_UNPACK_LOWD:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PUNPCKLDQ, ins->sreg1, ins->sreg2);
			break;
		case OP_UNPACK_LOWQ:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PUNPCKLQDQ, ins->sreg1, ins->sreg2);
			break;
		case OP_UNPACK_LOWPS:
			x86_sse_alu_ps_reg_reg (code, X86_SSE_UNPCKL, ins->sreg1, ins->sreg2);
			break;
		case OP_UNPACK_LOWPD:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_UNPCKL, ins->sreg1, ins->sreg2);
			break;

		case OP_UNPACK_HIGHB:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PUNPCKHBW, ins->sreg1, ins->sreg2);
			break;
		case OP_UNPACK_HIGHW:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PUNPCKHWD, ins->sreg1, ins->sreg2);
			break;
		case OP_UNPACK_HIGHD:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PUNPCKHDQ, ins->sreg1, ins->sreg2);
			break;
		case OP_UNPACK_HIGHQ:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PUNPCKHQDQ, ins->sreg1, ins->sreg2);
			break;
		case OP_UNPACK_HIGHPS:
			x86_sse_alu_ps_reg_reg (code, X86_SSE_UNPCKH, ins->sreg1, ins->sreg2);
			break;
		case OP_UNPACK_HIGHPD:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_UNPCKH, ins->sreg1, ins->sreg2);
			break;

		case OP_PACKW:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PACKSSWB, ins->sreg1, ins->sreg2);
			break;
		case OP_PACKD:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PACKSSDW, ins->sreg1, ins->sreg2);
			break;
		case OP_PACKW_UN:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PACKUSWB, ins->sreg1, ins->sreg2);
			break;
		case OP_PACKD_UN:
			x86_sse_alu_sse41_reg_reg (code, X86_SSE_PACKUSDW, ins->sreg1, ins->sreg2);
			break;

		case OP_PADDB_SAT_UN:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PADDUSB, ins->sreg1, ins->sreg2);
			break;
		case OP_PSUBB_SAT_UN:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PSUBUSB, ins->sreg1, ins->sreg2);
			break;
		case OP_PADDW_SAT_UN:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PADDUSW, ins->sreg1, ins->sreg2);
			break;
		case OP_PSUBW_SAT_UN:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PSUBUSW, ins->sreg1, ins->sreg2);
			break;

		case OP_PADDB_SAT:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PADDSB, ins->sreg1, ins->sreg2);
			break;
		case OP_PSUBB_SAT:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PSUBSB, ins->sreg1, ins->sreg2);
			break;
		case OP_PADDW_SAT:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PADDSW, ins->sreg1, ins->sreg2);
			break;
		case OP_PSUBW_SAT:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PSUBSW, ins->sreg1, ins->sreg2);
			break;

		case OP_PMULW:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PMULLW, ins->sreg1, ins->sreg2);
			break;
		case OP_PMULD:
			x86_sse_alu_sse41_reg_reg (code, X86_SSE_PMULLD, ins->sreg1, ins->sreg2);
			break;
		case OP_PMULQ:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PMULUDQ, ins->sreg1, ins->sreg2);
			break;
		case OP_PMULW_HIGH_UN:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PMULHUW, ins->sreg1, ins->sreg2);
			break;
		case OP_PMULW_HIGH:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PMULHW, ins->sreg1, ins->sreg2);
			break;

		case OP_PSHRW:
			x86_sse_shift_reg_imm (code, X86_SSE_PSHIFTW, X86_SSE_SHR, ins->dreg, ins->inst_imm);
			break;
		case OP_PSHRW_REG:
			x86_sse_shift_reg_reg (code, X86_SSE_PSRLW_REG, ins->dreg, ins->sreg2);
			break;

		case OP_PSARW:
			x86_sse_shift_reg_imm (code, X86_SSE_PSHIFTW, X86_SSE_SAR, ins->dreg, ins->inst_imm);
			break;
		case OP_PSARW_REG:
			x86_sse_shift_reg_reg (code, X86_SSE_PSRAW_REG, ins->dreg, ins->sreg2);
			break;

		case OP_PSHLW:
			x86_sse_shift_reg_imm (code, X86_SSE_PSHIFTW, X86_SSE_SHL, ins->dreg, ins->inst_imm);
			break;
		case OP_PSHLW_REG:
			x86_sse_shift_reg_reg (code, X86_SSE_PSLLW_REG, ins->dreg, ins->sreg2);
			break;

		case OP_PSHRD:
			x86_sse_shift_reg_imm (code, X86_SSE_PSHIFTD, X86_SSE_SHR, ins->dreg, ins->inst_imm);
			break;
		case OP_PSHRD_REG:
			x86_sse_shift_reg_reg (code, X86_SSE_PSRLD_REG, ins->dreg, ins->sreg2);
			break;

		case OP_PSARD:
			x86_sse_shift_reg_imm (code, X86_SSE_PSHIFTD, X86_SSE_SAR, ins->dreg, ins->inst_imm);
			break;
		case OP_PSARD_REG:
			x86_sse_shift_reg_reg (code, X86_SSE_PSRAD_REG, ins->dreg, ins->sreg2);
			break;

		case OP_PSHLD:
			x86_sse_shift_reg_imm (code, X86_SSE_PSHIFTD, X86_SSE_SHL, ins->dreg, ins->inst_imm);
			break;
		case OP_PSHLD_REG:
			x86_sse_shift_reg_reg (code, X86_SSE_PSLLD_REG, ins->dreg, ins->sreg2);
			break;

		case OP_PSHRQ:
			x86_sse_shift_reg_imm (code, X86_SSE_PSHIFTQ, X86_SSE_SHR, ins->dreg, ins->inst_imm);
			break;
		case OP_PSHRQ_REG:
			x86_sse_shift_reg_reg (code, X86_SSE_PSRLQ_REG, ins->dreg, ins->sreg2);
			break;

		case OP_PSHLQ:
			x86_sse_shift_reg_imm (code, X86_SSE_PSHIFTQ, X86_SSE_SHL, ins->dreg, ins->inst_imm);
			break;
		case OP_PSHLQ_REG:
			x86_sse_shift_reg_reg (code, X86_SSE_PSLLQ_REG, ins->dreg, ins->sreg2);
			break;

		case OP_ICONV_TO_X:
			x86_movd_xreg_reg (code, ins->dreg, ins->sreg1);
			break;
		case OP_EXTRACT_I4:
			x86_movd_reg_xreg (code, ins->dreg, ins->sreg1);
			break;
		case OP_EXTRACT_I1:
			x86_movd_reg_xreg (code, ins->dreg, ins->sreg1);
			if (ins->inst_c0)
				x86_shift_reg_imm (code, X86_SHR, ins->dreg, ins->inst_c0 * 8);
			x86_widen_reg (code, ins->dreg, ins->dreg, ins->inst_c1 == MONO_TYPE_I1, FALSE);
			break;
		case OP_EXTRACT_I2:
			x86_movd_reg_xreg (code, ins->dreg, ins->sreg1);
			if (ins->inst_c0)
				x86_shift_reg_imm (code, X86_SHR, ins->dreg, 16);
			x86_widen_reg (code, ins->dreg, ins->dreg, ins->inst_c1 == MONO_TYPE_I2, TRUE);
			break;
		case OP_EXTRACT_R8:
			if (ins->inst_c0)
				x86_sse_alu_pd_membase_reg (code, X86_SSE_MOVHPD_MEMBASE_REG, ins->backend.spill_var->inst_basereg, ins->backend.spill_var->inst_offset, ins->sreg1);
			else
				x86_sse_alu_sd_membase_reg (code, X86_SSE_MOVSD_MEMBASE_REG, ins->backend.spill_var->inst_basereg, ins->backend.spill_var->inst_offset, ins->sreg1);
			x86_fld_membase (code, ins->backend.spill_var->inst_basereg, ins->backend.spill_var->inst_offset, TRUE);
			break;

		case OP_INSERT_I2:
			x86_sse_alu_pd_reg_reg_imm (code, X86_SSE_PINSRW, ins->sreg1, ins->sreg2, ins->inst_c0);
			break;
		case OP_EXTRACTX_U2:
			x86_sse_alu_pd_reg_reg_imm (code, X86_SSE_PEXTRW, ins->dreg, ins->sreg1, ins->inst_c0);
			break;
		case OP_INSERTX_U1_SLOW:
			/*sreg1 is the extracted ireg (scratch)
			/sreg2 is the to be inserted ireg (scratch)
			/dreg is the xreg to receive the value*/

			/*clear the bits from the extracted word*/
			x86_alu_reg_imm (code, X86_AND, ins->sreg1, ins->inst_c0 & 1 ? 0x00FF : 0xFF00);
			/*shift the value to insert if needed*/
			if (ins->inst_c0 & 1)
				x86_shift_reg_imm (code, X86_SHL, ins->sreg2, 8);
			/*join them together*/
			x86_alu_reg_reg (code, X86_OR, ins->sreg1, ins->sreg2);
			x86_sse_alu_pd_reg_reg_imm (code, X86_SSE_PINSRW, ins->dreg, ins->sreg1, ins->inst_c0 / 2);
			break;
		case OP_INSERTX_I4_SLOW:
			x86_sse_alu_pd_reg_reg_imm (code, X86_SSE_PINSRW, ins->dreg, ins->sreg2, ins->inst_c0 * 2);
			x86_shift_reg_imm (code, X86_SHR, ins->sreg2, 16);
			x86_sse_alu_pd_reg_reg_imm (code, X86_SSE_PINSRW, ins->dreg, ins->sreg2, ins->inst_c0 * 2 + 1);
			break;

		case OP_INSERTX_R4_SLOW:
			x86_fst_membase (code, ins->backend.spill_var->inst_basereg, ins->backend.spill_var->inst_offset, FALSE, TRUE);
			/*TODO if inst_c0 == 0 use movss*/
			x86_sse_alu_pd_reg_membase_imm (code, X86_SSE_PINSRW, ins->dreg, ins->backend.spill_var->inst_basereg, ins->backend.spill_var->inst_offset + 0, ins->inst_c0 * 2);
			x86_sse_alu_pd_reg_membase_imm (code, X86_SSE_PINSRW, ins->dreg, ins->backend.spill_var->inst_basereg, ins->backend.spill_var->inst_offset + 2, ins->inst_c0 * 2 + 1);
			break;
		case OP_INSERTX_R8_SLOW:
			x86_fst_membase (code, ins->backend.spill_var->inst_basereg, ins->backend.spill_var->inst_offset, TRUE, TRUE);
			if (cfg->verbose_level)
				printf ("CONVERTING a OP_INSERTX_R8_SLOW %d offset %x\n", ins->inst_c0, offset);
			if (ins->inst_c0)
				x86_sse_alu_pd_reg_membase (code, X86_SSE_MOVHPD_REG_MEMBASE, ins->dreg, ins->backend.spill_var->inst_basereg, ins->backend.spill_var->inst_offset);
			else
				x86_movsd_reg_membase (code, ins->dreg, ins->backend.spill_var->inst_basereg, ins->backend.spill_var->inst_offset);
			break;

		case OP_STOREX_MEMBASE_REG:
		case OP_STOREX_MEMBASE:
			x86_movups_membase_reg (code, ins->dreg, ins->inst_offset, ins->sreg1);
			break;
		case OP_LOADX_MEMBASE:
			x86_movups_reg_membase (code, ins->dreg, ins->sreg1, ins->inst_offset);
			break;
		case OP_LOADX_ALIGNED_MEMBASE:
			x86_movaps_reg_membase (code, ins->dreg, ins->sreg1, ins->inst_offset);
			break;
		case OP_STOREX_ALIGNED_MEMBASE_REG:
			x86_movaps_membase_reg (code, ins->dreg, ins->inst_offset, ins->sreg1);
			break;
		case OP_STOREX_NTA_MEMBASE_REG:
			x86_sse_alu_reg_membase (code, X86_SSE_MOVNTPS, ins->dreg, ins->sreg1, ins->inst_offset);
			break;
		case OP_PREFETCH_MEMBASE:
			x86_sse_alu_reg_membase (code, X86_SSE_PREFETCH, ins->backend.arg_info, ins->sreg1, ins->inst_offset);

			break;
		case OP_XMOVE:
			/*FIXME the peephole pass should have killed this*/
			if (ins->dreg != ins->sreg1)
				x86_movaps_reg_reg (code, ins->dreg, ins->sreg1);
			break;
		case OP_XZERO:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PXOR, ins->dreg, ins->dreg);
			break;
		case OP_XONES:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_PCMPEQB, ins->dreg, ins->dreg);
			break;

		case OP_FCONV_TO_R8_X:
			x86_fst_membase (code, ins->backend.spill_var->inst_basereg, ins->backend.spill_var->inst_offset, TRUE, TRUE);
			x86_movsd_reg_membase (code, ins->dreg, ins->backend.spill_var->inst_basereg, ins->backend.spill_var->inst_offset);
			break;

		case OP_XCONV_R8_TO_I4:
			x86_cvttsd2si (code, ins->dreg, ins->sreg1);
			switch (ins->backend.source_opcode) {
			case OP_FCONV_TO_I1:
				x86_widen_reg (code, ins->dreg, ins->dreg, TRUE, FALSE);
				break;
			case OP_FCONV_TO_U1:
				x86_widen_reg (code, ins->dreg, ins->dreg, FALSE, FALSE);
				break;
			case OP_FCONV_TO_I2:
				x86_widen_reg (code, ins->dreg, ins->dreg, TRUE, TRUE);
				break;
			case OP_FCONV_TO_U2:
				x86_widen_reg (code, ins->dreg, ins->dreg, FALSE, TRUE);
				break;
			}
			break;

		case OP_EXPAND_I2:
			x86_sse_alu_pd_reg_reg_imm (code, X86_SSE_PINSRW, ins->dreg, ins->sreg1, 0);
			x86_sse_alu_pd_reg_reg_imm (code, X86_SSE_PINSRW, ins->dreg, ins->sreg1, 1);
			x86_sse_shift_reg_imm (code, X86_SSE_PSHUFD, ins->dreg, ins->dreg, 0);
			break;
		case OP_EXPAND_I4:
			x86_movd_xreg_reg (code, ins->dreg, ins->sreg1);
			x86_sse_shift_reg_imm (code, X86_SSE_PSHUFD, ins->dreg, ins->dreg, 0);
			break;
		case OP_EXPAND_R4:
			x86_fst_membase (code, ins->backend.spill_var->inst_basereg, ins->backend.spill_var->inst_offset, FALSE, TRUE);
			x86_movd_xreg_membase (code, ins->dreg, ins->backend.spill_var->inst_basereg, ins->backend.spill_var->inst_offset);
			x86_sse_shift_reg_imm (code, X86_SSE_PSHUFD, ins->dreg, ins->dreg, 0);
			break;
		case OP_EXPAND_R8:
			x86_fst_membase (code, ins->backend.spill_var->inst_basereg, ins->backend.spill_var->inst_offset, TRUE, TRUE);
			x86_movsd_reg_membase (code, ins->dreg, ins->backend.spill_var->inst_basereg, ins->backend.spill_var->inst_offset);
			x86_sse_shift_reg_imm (code, X86_SSE_PSHUFD, ins->dreg, ins->dreg, 0x44);
			break;

		case OP_CVTDQ2PD:
			x86_sse_alu_ss_reg_reg (code, X86_SSE_CVTDQ2PD, ins->dreg, ins->sreg1);
			break;
		case OP_CVTDQ2PS:
			x86_sse_alu_ps_reg_reg (code, X86_SSE_CVTDQ2PS, ins->dreg, ins->sreg1);
			break;
		case OP_CVTPD2DQ:
			x86_sse_alu_sd_reg_reg (code, X86_SSE_CVTPD2DQ, ins->dreg, ins->sreg1);
			break;
		case OP_CVTPD2PS:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_CVTPD2PS, ins->dreg, ins->sreg1);
			break;
		case OP_CVTPS2DQ:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_CVTPS2DQ, ins->dreg, ins->sreg1);
			break;
		case OP_CVTPS2PD:
			x86_sse_alu_ps_reg_reg (code, X86_SSE_CVTPS2PD, ins->dreg, ins->sreg1);
			break;
		case OP_CVTTPD2DQ:
			x86_sse_alu_pd_reg_reg (code, X86_SSE_CVTTPD2DQ, ins->dreg, ins->sreg1);
			break;
		case OP_CVTTPS2DQ:
			x86_sse_alu_ss_reg_reg (code, X86_SSE_CVTTPS2DQ, ins->dreg, ins->sreg1);
			break;

#endif
		case OP_LIVERANGE_START: {
			if (cfg->verbose_level > 1)
				printf ("R%d START=0x%x\n", MONO_VARINFO (cfg, ins->inst_c0)->vreg, (int)(code - cfg->native_code));
			MONO_VARINFO (cfg, ins->inst_c0)->live_range_start = code - cfg->native_code;
			break;
		}
		case OP_LIVERANGE_END: {
			if (cfg->verbose_level > 1)
				printf ("R%d END=0x%x\n", MONO_VARINFO (cfg, ins->inst_c0)->vreg, (int)(code - cfg->native_code));
			MONO_VARINFO (cfg, ins->inst_c0)->live_range_end = code - cfg->native_code;
			break;
		}
		case OP_GC_SAFE_POINT: {
			guint8 *br [1];

			x86_test_membase_imm (code, ins->sreg1, 0, 1);
			br[0] = code; x86_branch8 (code, X86_CC_EQ, 0, FALSE);
			code = emit_call (cfg, code, MONO_PATCH_INFO_JIT_ICALL_ID, GUINT_TO_POINTER (MONO_JIT_ICALL_mono_threads_state_poll));
			x86_patch (br [0], code);

			break;
		}
		case OP_GC_LIVENESS_DEF:
		case OP_GC_LIVENESS_USE:
		case OP_GC_PARAM_SLOT_LIVENESS_DEF:
			ins->backend.pc_offset = code - cfg->native_code;
			break;
		case OP_GC_SPILL_SLOT_LIVENESS_DEF:
			ins->backend.pc_offset = code - cfg->native_code;
			bb->spill_slot_defs = g_slist_prepend_mempool (cfg->mempool, bb->spill_slot_defs, ins);
			break;
		case OP_GET_SP:
			x86_mov_reg_reg (code, ins->dreg, X86_ESP);
			break;
		case OP_SET_SP:
			x86_mov_reg_reg (code, X86_ESP, ins->sreg1);
			break;
		case OP_FILL_PROF_CALL_CTX:
			x86_mov_membase_reg (code, ins->sreg1, MONO_STRUCT_OFFSET (MonoContext, esp), X86_ESP, sizeof (target_mgreg_t));
			x86_mov_membase_reg (code, ins->sreg1, MONO_STRUCT_OFFSET (MonoContext, ebp), X86_EBP, sizeof (target_mgreg_t));
			x86_mov_membase_reg (code, ins->sreg1, MONO_STRUCT_OFFSET (MonoContext, ebx), X86_EBX, sizeof (target_mgreg_t));
			x86_mov_membase_reg (code, ins->sreg1, MONO_STRUCT_OFFSET (MonoContext, esi), X86_ESI, sizeof (target_mgreg_t));
			x86_mov_membase_reg (code, ins->sreg1, MONO_STRUCT_OFFSET (MonoContext, edi), X86_EDI, sizeof (target_mgreg_t));
			break;
		case OP_GET_LAST_ERROR:
			code = emit_get_last_error (code, ins->dreg);
			break;
		default:
			g_warning ("unknown opcode %s\n", mono_inst_name (ins->opcode));
			g_assert_not_reached ();
		}

		if (G_UNLIKELY ((code - cfg->native_code - offset) > max_len)) {
			g_warning ("wrong maximal instruction length of instruction %s (expected %d, got %d)",
					   mono_inst_name (ins->opcode), max_len, code - cfg->native_code - offset);
			g_assert_not_reached ();
		}

		cpos += max_len;
	}

	set_code_cursor (cfg, code);
}

#endif /* DISABLE_JIT */

void
mono_arch_register_lowlevel_calls (void)
{
}

void
mono_arch_patch_code_new (MonoCompile *cfg, guint8 *code, MonoJumpInfo *ji, gpointer target)
{
	unsigned char *ip = ji->ip.i + code;

	switch (ji->type) {
	case MONO_PATCH_INFO_IP:
		*((gconstpointer *)(ip)) = target;
		break;
	case MONO_PATCH_INFO_ABS:
	case MONO_PATCH_INFO_METHOD:
	case MONO_PATCH_INFO_METHOD_JUMP:
	case MONO_PATCH_INFO_JIT_ICALL_ID:
	case MONO_PATCH_INFO_BB:
	case MONO_PATCH_INFO_LABEL:
	case MONO_PATCH_INFO_RGCTX_FETCH:
	case MONO_PATCH_INFO_JIT_ICALL_ADDR:
	case MONO_PATCH_INFO_SPECIFIC_TRAMPOLINE_LAZY_FETCH_ADDR:
		x86_patch (ip, (unsigned char*)target);
		break;
	case MONO_PATCH_INFO_NONE:
		break;
	case MONO_PATCH_INFO_R4:
	case MONO_PATCH_INFO_R8: {
		guint32 offset = mono_arch_get_patch_offset (ip);
		*((gconstpointer *)(ip + offset)) = target;
		break;
	}
	default: {
		guint32 offset = mono_arch_get_patch_offset (ip);
		*((gconstpointer *)(ip + offset)) = target;
		break;
	}
	}
}

static G_GNUC_UNUSED void
stack_unaligned (MonoMethod *m, gpointer caller)
{
	printf ("%s\n", mono_method_full_name (m, TRUE));
	g_assert_not_reached ();
}

#ifndef DISABLE_JIT

guint8 *
mono_arch_emit_prolog (MonoCompile *cfg)
{
	MonoMethod *method = cfg->method;
	MonoBasicBlock *bb;
	MonoMethodSignature *sig;
	MonoInst *inst;
	CallInfo *cinfo;
	ArgInfo *ainfo;
	int alloc_size, pos, max_offset, i, cfa_offset;
	guint8 *code;
	gboolean need_stack_frame;

	cfg->code_size = MAX (cfg->header->code_size * 4, 10240);

	code = cfg->native_code = g_malloc (cfg->code_size);

#if 0
	{
		guint8 *br [16];

	/* Check that the stack is aligned on osx */
	x86_mov_reg_reg (code, X86_EAX, X86_ESP);
	x86_alu_reg_imm (code, X86_AND, X86_EAX, 15);
	x86_alu_reg_imm (code, X86_CMP, X86_EAX, 0xc);
	br [0] = code;
	x86_branch_disp (code, X86_CC_Z, 0, FALSE);
	x86_push_membase (code, X86_ESP, 0);
	x86_push_imm (code, cfg->method);
	x86_mov_reg_imm (code, X86_EAX, stack_unaligned);
	x86_call_reg (code, X86_EAX);
	x86_patch (br [0], code);
	}
#endif

	/* Offset between RSP and the CFA */
	cfa_offset = 0;

	// CFA = sp + 4
	cfa_offset = 4;
	mono_emit_unwind_op_def_cfa (cfg, code, X86_ESP, cfa_offset);
	// IP saved at CFA - 4
	/* There is no IP reg on x86 */
	mono_emit_unwind_op_offset (cfg, code, X86_NREG, -cfa_offset);
	mini_gc_set_slot_type_from_cfa (cfg, -cfa_offset, SLOT_NOREF);

	need_stack_frame = needs_stack_frame (cfg);

	if (need_stack_frame) {
		x86_push_reg (code, X86_EBP);
		cfa_offset += 4;
		mono_emit_unwind_op_def_cfa_offset (cfg, code, cfa_offset);
		mono_emit_unwind_op_offset (cfg, code, X86_EBP, - cfa_offset);
		x86_mov_reg_reg (code, X86_EBP, X86_ESP);
		mono_emit_unwind_op_def_cfa_reg (cfg, code, X86_EBP);
		/* These are handled automatically by the stack marking code */
		mini_gc_set_slot_type_from_cfa (cfg, -cfa_offset, SLOT_NOREF);
	} else {
		cfg->frame_reg = X86_ESP;
	}

	cfg->stack_offset += cfg->param_area;
	cfg->stack_offset = ALIGN_TO (cfg->stack_offset, MONO_ARCH_FRAME_ALIGNMENT);

	alloc_size = cfg->stack_offset;
	pos = 0;

	if (!method->save_lmf) {
		if (cfg->used_int_regs & (1 << X86_EBX)) {
			x86_push_reg (code, X86_EBX);
			pos += 4;
			cfa_offset += 4;
			mono_emit_unwind_op_offset (cfg, code, X86_EBX, - cfa_offset);
			/* These are handled automatically by the stack marking code */
			mini_gc_set_slot_type_from_cfa (cfg, - cfa_offset, SLOT_NOREF);
		}

		if (cfg->used_int_regs & (1 << X86_EDI)) {
			x86_push_reg (code, X86_EDI);
			pos += 4;
			cfa_offset += 4;
			mono_emit_unwind_op_offset (cfg, code, X86_EDI, - cfa_offset);
			mini_gc_set_slot_type_from_cfa (cfg, - cfa_offset, SLOT_NOREF);
		}

		if (cfg->used_int_regs & (1 << X86_ESI)) {
			x86_push_reg (code, X86_ESI);
			pos += 4;
			cfa_offset += 4;
			mono_emit_unwind_op_offset (cfg, code, X86_ESI, - cfa_offset);
			mini_gc_set_slot_type_from_cfa (cfg, - cfa_offset, SLOT_NOREF);
		}
	}

	alloc_size -= pos;

	/* the original alloc_size is already aligned: there is %ebp and retip pushed, so realign */
	if (mono_do_x86_stack_align && need_stack_frame) {
		int tot = alloc_size + pos + 4; /* ret ip */
		if (need_stack_frame)
			tot += 4; /* ebp */
		tot &= MONO_ARCH_FRAME_ALIGNMENT - 1;
		if (tot) {
			alloc_size += MONO_ARCH_FRAME_ALIGNMENT - tot;
			for (i = 0; i < MONO_ARCH_FRAME_ALIGNMENT - tot; i += sizeof (target_mgreg_t))
				mini_gc_set_slot_type_from_fp (cfg, - (alloc_size + pos - i), SLOT_NOREF);
		}
	}

	cfg->arch.sp_fp_offset = alloc_size + pos;

	if (alloc_size) {
		/* See mono_emit_stack_alloc */
#if defined (TARGET_WIN32) || defined (MONO_ARCH_SIGSEGV_ON_ALTSTACK)
		guint32 remaining_size = alloc_size;
		/*FIXME handle unbounded code expansion, we should use a loop in case of more than X interactions*/
		guint32 required_code_size = ((remaining_size / 0x1000) + 1) * 8; /*8 is the max size of x86_alu_reg_imm + x86_test_membase_reg*/
		set_code_cursor (cfg, code);
		code = realloc_code (cfg, required_code_size);
		while (remaining_size >= 0x1000) {
			x86_alu_reg_imm (code, X86_SUB, X86_ESP, 0x1000);
			x86_test_membase_reg (code, X86_ESP, 0, X86_ESP);
			remaining_size -= 0x1000;
		}
		if (remaining_size)
			x86_alu_reg_imm (code, X86_SUB, X86_ESP, remaining_size);
#else
		x86_alu_reg_imm (code, X86_SUB, X86_ESP, alloc_size);
#endif

		g_assert (need_stack_frame);
	}

	if (cfg->method->wrapper_type == MONO_WRAPPER_NATIVE_TO_MANAGED ||
			cfg->method->wrapper_type == MONO_WRAPPER_RUNTIME_INVOKE) {
		x86_alu_reg_imm (code, X86_AND, X86_ESP, -MONO_ARCH_FRAME_ALIGNMENT);
	}

#if DEBUG_STACK_ALIGNMENT
	/* check the stack is aligned */
	if (need_stack_frame && method->wrapper_type == MONO_WRAPPER_NONE) {
		x86_mov_reg_reg (code, X86_ECX, X86_ESP);
		x86_alu_reg_imm (code, X86_AND, X86_ECX, MONO_ARCH_FRAME_ALIGNMENT - 1);
		x86_alu_reg_imm (code, X86_CMP, X86_ECX, 0);
		x86_branch_disp (code, X86_CC_EQ, 3, FALSE);
		x86_breakpoint (code);
	}
#endif

	/* compute max_offset in order to use short forward jumps */
	max_offset = 0;
	if (cfg->opt & MONO_OPT_BRANCH) {
		for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
			MonoInst *ins;
			bb->max_offset = max_offset;

			/* max alignment for loops */
			if ((cfg->opt & MONO_OPT_LOOP) && bb_is_loop_start (bb))
				max_offset += LOOP_ALIGNMENT;
			MONO_BB_FOR_EACH_INS (bb, ins) {
				if (ins->opcode == OP_LABEL)
					ins->inst_c1 = max_offset;
				max_offset += ins_get_size (ins->opcode);
			}
		}
	}

	/* store runtime generic context */
	if (cfg->rgctx_var) {
		g_assert (cfg->rgctx_var->opcode == OP_REGOFFSET && cfg->rgctx_var->inst_basereg == X86_EBP);

		x86_mov_membase_reg (code, X86_EBP, cfg->rgctx_var->inst_offset, MONO_ARCH_RGCTX_REG, 4);
	}

	if (method->save_lmf)
		code = emit_setup_lmf (cfg, code, cfg->lmf_var->inst_offset, cfa_offset);

	{
		MonoInst *ins;

		if (cfg->arch.ss_tramp_var) {
			/* Initialize ss_tramp_var */
			ins = cfg->arch.ss_tramp_var;
			g_assert (ins->opcode == OP_REGOFFSET);

			g_assert (!cfg->compile_aot);
			x86_mov_membase_imm (code, ins->inst_basereg, ins->inst_offset, (gsize)&ss_trampoline, 4);
		}

		if (cfg->arch.bp_tramp_var) {
			/* Initialize bp_tramp_var */
			ins = cfg->arch.bp_tramp_var;
			g_assert (ins->opcode == OP_REGOFFSET);

			g_assert (!cfg->compile_aot);
			x86_mov_membase_imm (code, ins->inst_basereg, ins->inst_offset, (gsize)&bp_trampoline, 4);
		}
	}

	/* load arguments allocated to register from the stack */
	sig = mono_method_signature_internal (method);
	pos = 0;

	cinfo = cfg->arch.cinfo;

	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		inst = cfg->args [pos];
		ainfo = &cinfo->args [pos];
		if (inst->opcode == OP_REGVAR) {
			if (storage_in_ireg (ainfo->storage)) {
				x86_mov_reg_reg (code, inst->dreg, ainfo->reg);
			} else {
				g_assert (need_stack_frame);
				x86_mov_reg_membase (code, inst->dreg, X86_EBP, ainfo->offset + ARGS_OFFSET, 4);
			}
			if (cfg->verbose_level > 2)
				g_print ("Argument %d assigned to register %s\n", pos, mono_arch_regname (inst->dreg));
		} else {
			if (storage_in_ireg (ainfo->storage)) {
				x86_mov_membase_reg (code, inst->inst_basereg, inst->inst_offset, ainfo->reg, 4);
			}
		}
		pos++;
	}

	set_code_cursor (cfg, code);

	return code;
}

#endif

void
mono_arch_emit_epilog (MonoCompile *cfg)
{
	MonoMethod *method = cfg->method;
	MonoMethodSignature *sig = mono_method_signature_internal (method);
	int i, quad, pos;
	guint32 stack_to_pop;
	guint8 *code;
	int max_epilog_size = 16;
	CallInfo *cinfo;
	gboolean need_stack_frame = needs_stack_frame (cfg);

	if (cfg->method->save_lmf)
		max_epilog_size += 128;

	code = realloc_code (cfg, max_epilog_size);

	/* the code restoring the registers must be kept in sync with OP_TAILCALL */
	pos = 0;

	if (method->save_lmf) {
		gint32 lmf_offset = cfg->lmf_var->inst_offset;

		/* restore caller saved regs */
		if (cfg->used_int_regs & (1 << X86_EBX)) {
			x86_mov_reg_membase (code, X86_EBX, cfg->frame_reg, lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, ebx), 4);
		}

		if (cfg->used_int_regs & (1 << X86_EDI)) {
			x86_mov_reg_membase (code, X86_EDI, cfg->frame_reg, lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, edi), 4);
		}
		if (cfg->used_int_regs & (1 << X86_ESI)) {
			x86_mov_reg_membase (code, X86_ESI, cfg->frame_reg, lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, esi), 4);
		}

		/* EBP is restored by LEAVE */
	} else {
		for (i = 0; i < X86_NREG; ++i) {
			if ((cfg->used_int_regs & X86_CALLER_REGS & ((regmask_t)1 << i)) && (i != X86_EBP)) {
				pos -= 4;
			}
		}

		g_assert (!pos || need_stack_frame);
		if (pos) {
			x86_lea_membase (code, X86_ESP, X86_EBP, pos);
		}

		if (cfg->used_int_regs & (1 << X86_ESI)) {
			x86_pop_reg (code, X86_ESI);
		}
		if (cfg->used_int_regs & (1 << X86_EDI)) {
			x86_pop_reg (code, X86_EDI);
		}
		if (cfg->used_int_regs & (1 << X86_EBX)) {
			x86_pop_reg (code, X86_EBX);
		}
	}

	/* Load returned vtypes into registers if needed */
	cinfo = cfg->arch.cinfo;
	if (cinfo->ret.storage == ArgValuetypeInReg) {
		for (quad = 0; quad < 2; quad ++) {
			switch (cinfo->ret.pair_storage [quad]) {
			case ArgInIReg:
				x86_mov_reg_membase (code, cinfo->ret.pair_regs [quad], cfg->ret->inst_basereg, cfg->ret->inst_offset + (quad * sizeof (target_mgreg_t)), 4);
				break;
			case ArgOnFloatFpStack:
				x86_fld_membase (code, cfg->ret->inst_basereg, cfg->ret->inst_offset + (quad * sizeof (target_mgreg_t)), FALSE);
				break;
			case ArgOnDoubleFpStack:
				x86_fld_membase (code, cfg->ret->inst_basereg, cfg->ret->inst_offset + (quad * sizeof (target_mgreg_t)), TRUE);
				break;
			case ArgNone:
				break;
			default:
				g_assert_not_reached ();
			}
		}
	}

	if (need_stack_frame)
		x86_leave (code);

	if (CALLCONV_IS_STDCALL (sig)) {
		MonoJitArgumentInfo *arg_info = g_newa (MonoJitArgumentInfo, sig->param_count + 1);

		stack_to_pop = mono_arch_get_argument_info (sig, sig->param_count, arg_info);
	} else if (cinfo->callee_stack_pop)
		stack_to_pop = cinfo->callee_stack_pop;
	else
		stack_to_pop = 0;

	if (stack_to_pop) {
		g_assert (need_stack_frame);
		x86_ret_imm (code, stack_to_pop);
	} else {
		x86_ret (code);
	}

	set_code_cursor (cfg, code);
}

void
mono_arch_emit_exceptions (MonoCompile *cfg)
{
	MonoJumpInfo *patch_info;
	int nthrows, i;
	guint8 *code;
	MonoClass *exc_classes [16];
	guint8 *exc_throw_start [16], *exc_throw_end [16];
	guint32 code_size;
	int exc_count = 0;

	/* Compute needed space */
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		if (patch_info->type == MONO_PATCH_INFO_EXC)
			exc_count++;
	}

	/*
	 * make sure we have enough space for exceptions
	 * 16 is the size of two push_imm instructions and a call
	 */
	if (cfg->compile_aot)
		code_size = exc_count * 32;
	else
		code_size = exc_count * 16;

	code = realloc_code (cfg, code_size);

	nthrows = 0;
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		switch (patch_info->type) {
		case MONO_PATCH_INFO_EXC: {
			MonoClass *exc_class;
			guint8 *buf, *buf2;
			guint32 throw_ip;

			x86_patch (patch_info->ip.i + cfg->native_code, code);

			exc_class = mono_class_load_from_name (mono_defaults.corlib, "System", patch_info->data.name);
			throw_ip = patch_info->ip.i;

			/* Find a throw sequence for the same exception class */
			for (i = 0; i < nthrows; ++i)
				if (exc_classes [i] == exc_class)
					break;
			if (i < nthrows) {
				x86_push_imm (code, (exc_throw_end [i] - cfg->native_code) - throw_ip);
				x86_jump_code (code, exc_throw_start [i]);
				patch_info->type = MONO_PATCH_INFO_NONE;
			}
			else {
				guint32 size;

				/* Compute size of code following the push <OFFSET> */
				size = 5 + 5;

				/*This is aligned to 16 bytes by the callee. This way we save a few bytes here.*/

				if ((code - cfg->native_code) - throw_ip < 126 - size) {
					/* Use the shorter form */
					buf = buf2 = code;
					x86_push_imm (code, 0);
				}
				else {
					buf = code;
					x86_push_imm (code, 0xf0f0f0f0);
					buf2 = code;
				}

				if (nthrows < 16) {
					exc_classes [nthrows] = exc_class;
					exc_throw_start [nthrows] = code;
				}

				x86_push_imm (code, m_class_get_type_token (exc_class) - MONO_TOKEN_TYPE_DEF);
				patch_info->data.jit_icall_id = MONO_JIT_ICALL_mono_arch_throw_corlib_exception;
				patch_info->type = MONO_PATCH_INFO_JIT_ICALL_ID;
				patch_info->ip.i = code - cfg->native_code;
				x86_call_code (code, 0);
				x86_push_imm (buf, (code - cfg->native_code) - throw_ip);
				while (buf < buf2)
					x86_nop (buf);

				if (nthrows < 16) {
					exc_throw_end [nthrows] = code;
					nthrows ++;
				}
			}
			break;
		}
		default:
			/* do nothing */
			break;
		}
		set_code_cursor (cfg, code);
	}
	set_code_cursor (cfg, code);
}

MONO_NEVER_INLINE
void
mono_arch_flush_icache (guint8 *code, gint size)
{
	/* call/ret required (or likely other control transfer) */
}

void
mono_arch_flush_register_windows (void)
{
}

gboolean
mono_arch_is_inst_imm (int opcode, int imm_opcode, gint64 imm)
{
	return TRUE;
}

void
mono_arch_finish_init (void)
{
}

// Linear handler, the bsearch head compare is shorter
//[2 + 4] x86_alu_reg_imm (code, X86_CMP, ins->sreg1, ins->inst_imm);
//[1 + 1] x86_branch8(inst,cond,imm,is_signed)
//        x86_patch(ins,target)
//[1 + 5] x86_jump_mem(inst,mem)

#define CMP_SIZE 6
#define BR_SMALL_SIZE 2
#define BR_LARGE_SIZE 5
#define JUMP_IMM_SIZE 6
#define ENABLE_WRONG_METHOD_CHECK 0
#define DEBUG_IMT 0

static int
imt_branch_distance (MonoIMTCheckItem **imt_entries, int start, int target)
{
	int i, distance = 0;
	for (i = start; i < target; ++i)
		distance += imt_entries [i]->chunk_size;
	return distance;
}

/*
 * LOCKING: called with the domain lock held
 */
gpointer
mono_arch_build_imt_trampoline (MonoVTable *vtable, MonoIMTCheckItem **imt_entries, int count,
	gpointer fail_tramp)
{
	int i;
	int size = 0;
	guint8 *code, *start;
	GSList *unwind_ops;
	MonoMemoryManager *mem_manager = m_class_get_mem_manager (vtable->klass);

	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		if (item->is_equals) {
			if (item->check_target_idx) {
				if (!item->compare_done)
					item->chunk_size += CMP_SIZE;
				item->chunk_size += BR_SMALL_SIZE + JUMP_IMM_SIZE;
			} else {
				if (fail_tramp) {
					item->chunk_size += CMP_SIZE + BR_SMALL_SIZE + JUMP_IMM_SIZE * 2;
				} else {
					item->chunk_size += JUMP_IMM_SIZE;
#if ENABLE_WRONG_METHOD_CHECK
					item->chunk_size += CMP_SIZE + BR_SMALL_SIZE + 1;
#endif
				}
			}
		} else {
			item->chunk_size += CMP_SIZE + BR_LARGE_SIZE;
			imt_entries [item->check_target_idx]->compare_done = TRUE;
		}
		size += item->chunk_size;
	}
	if (fail_tramp) {
		code = (guint8 *)mini_alloc_generic_virtual_trampoline (vtable, size);
	} else {
		code = mono_mem_manager_code_reserve (mem_manager, size);
	}
	start = code;

	unwind_ops = mono_arch_get_cie_program ();

	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		item->code_target = code;
		if (item->is_equals) {
			if (item->check_target_idx) {
				if (!item->compare_done)
					x86_alu_reg_imm (code, X86_CMP, MONO_ARCH_IMT_REG, (guint32)(gsize)item->key);
				item->jmp_code = code;
				x86_branch8 (code, X86_CC_NE, 0, FALSE);
				if (item->has_target_code)
					x86_jump_code (code, item->value.target_code);
				else
					x86_jump_mem (code, (gsize)&vtable->vtable [item->value.vtable_slot]);
			} else {
				if (fail_tramp) {
					x86_alu_reg_imm (code, X86_CMP, MONO_ARCH_IMT_REG, (guint32)(gsize)item->key);
					item->jmp_code = code;
					x86_branch8 (code, X86_CC_NE, 0, FALSE);
					if (item->has_target_code)
						x86_jump_code (code, item->value.target_code);
					else
						x86_jump_mem (code, (gsize)&vtable->vtable [item->value.vtable_slot]);
					x86_patch (item->jmp_code, code);
					x86_jump_code (code, fail_tramp);
					item->jmp_code = NULL;
				} else {
					/* enable the commented code to assert on wrong method */
#if ENABLE_WRONG_METHOD_CHECK
					x86_alu_reg_imm (code, X86_CMP, MONO_ARCH_IMT_REG, (guint32)(gsize)item->key);
					item->jmp_code = code;
					x86_branch8 (code, X86_CC_NE, 0, FALSE);
#endif
					if (item->has_target_code)
						x86_jump_code (code, item->value.target_code);
					else
						x86_jump_mem (code, (gsize)&vtable->vtable [item->value.vtable_slot]);
#if ENABLE_WRONG_METHOD_CHECK
					x86_patch (item->jmp_code, code);
					x86_breakpoint (code);
					item->jmp_code = NULL;
#endif
				}
			}
		} else {
			x86_alu_reg_imm (code, X86_CMP, MONO_ARCH_IMT_REG, (guint32)(gsize)item->key);
			item->jmp_code = code;
			if (x86_is_imm8 (imt_branch_distance (imt_entries, i, item->check_target_idx)))
				x86_branch8 (code, X86_CC_GE, 0, FALSE);
			else
				x86_branch32 (code, X86_CC_GE, 0, FALSE);
		}
	}
	/* patch the branches to get to the target items */
	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		if (item->jmp_code) {
			if (item->check_target_idx) {
				x86_patch (item->jmp_code, imt_entries [item->check_target_idx]->code_target);
			}
		}
	}

	if (!fail_tramp)
		UnlockedAdd (&mono_stats.imt_trampolines_size, code - start);
	g_assertf (code - start <= size, "%d %d", (int)(code - start), size);

#if DEBUG_IMT
	{
		char *buff = g_strdup_printf ("thunk_for_class_%s_%s_entries_%d", m_class_get_name_space (vtable->klass), m_class_get_name (vtable->klass), count);
		mono_disassemble_code (NULL, (guint8*)start, code - start, buff);
		g_free (buff);
	}
#endif
	if (mono_jit_map_is_enabled ()) {
		char *buff;
		if (vtable)
			buff = g_strdup_printf ("imt_%s_%s_entries_%d", m_class_get_name_space (vtable->klass), m_class_get_name (vtable->klass), count);
		else
			buff = g_strdup_printf ("imt_trampoline_entries_%d", count);
		mono_emit_jit_tramp (start, code - start, buff);
		g_free (buff);
	}

	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_IMT_TRAMPOLINE, NULL));

	mono_tramp_info_register (mono_tramp_info_create (NULL, start, code - start, NULL, unwind_ops), mem_manager);

	return start;
}

MonoMethod*
mono_arch_find_imt_method (host_mgreg_t *regs, guint8 *code)
{
	return (MonoMethod*) regs [MONO_ARCH_IMT_REG];
}

MonoVTable*
mono_arch_find_static_call_vtable (host_mgreg_t *regs, guint8 *code)
{
	return (MonoVTable*) regs [MONO_ARCH_RGCTX_REG];
}

GSList*
mono_arch_get_cie_program (void)
{
	GSList *l = NULL;

	mono_add_unwind_op_def_cfa (l, (guint8*)NULL, (guint8*)NULL, X86_ESP, 4);
	mono_add_unwind_op_offset (l, (guint8*)NULL, (guint8*)NULL, X86_NREG, -4);

	return l;
}

MonoInst*
mono_arch_emit_inst_for_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	MonoInst *ins = NULL;
	int opcode = 0;

	if (cmethod->klass == mono_class_try_get_math_class ()) {
		if (strcmp (cmethod->name, "Tan") == 0) {
			opcode = OP_TAN;
		} else if (strcmp (cmethod->name, "Atan") == 0) {
			opcode = OP_ATAN;
		} else if (strcmp (cmethod->name, "Sqrt") == 0) {
			opcode = OP_SQRT;
		} else if (strcmp (cmethod->name, "Abs") == 0 && fsig->params [0]->type == MONO_TYPE_R8) {
			opcode = OP_ABS;
		} else if (strcmp (cmethod->name, "Round") == 0 && fsig->param_count == 1 && fsig->params [0]->type == MONO_TYPE_R8) {
			opcode = OP_ROUND;
		}

		if (opcode && fsig->param_count == 1) {
			MONO_INST_NEW (cfg, ins, opcode);
			ins->type = STACK_R8;
			ins->dreg = mono_alloc_freg (cfg);
			ins->sreg1 = args [0]->dreg;
			MONO_ADD_INS (cfg->cbb, ins);
		}

		if (cfg->opt & MONO_OPT_CMOV) {
			opcode = 0;

			if (strcmp (cmethod->name, "Min") == 0) {
				if (fsig->params [0]->type == MONO_TYPE_I4)
					opcode = OP_IMIN;
			} else if (strcmp (cmethod->name, "Max") == 0) {
				if (fsig->params [0]->type == MONO_TYPE_I4)
					opcode = OP_IMAX;
			}

			if (opcode && fsig->param_count == 2) {
				MONO_INST_NEW (cfg, ins, opcode);
				ins->type = STACK_I4;
				ins->dreg = mono_alloc_ireg (cfg);
				ins->sreg1 = args [0]->dreg;
				ins->sreg2 = args [1]->dreg;
				MONO_ADD_INS (cfg->cbb, ins);
			}
		}

#if 0
		/* OP_FREM is not IEEE compatible */
		else if (strcmp (cmethod->name, "IEEERemainder") == 0 && fsig->param_count == 2) {
			MONO_INST_NEW (cfg, ins, OP_FREM);
			ins->inst_i0 = args [0];
			ins->inst_i1 = args [1];
		}
#endif
	}

	return ins;
}

guint32
mono_arch_get_patch_offset (guint8 *code)
{
	if ((code [0] == 0x8b) && (x86_modrm_mod (code [1]) == 0x2))
		return 2;
	else if (code [0] == 0xba)
		return 1;
	else if (code [0] == 0x68)
		/* push IMM */
		return 1;
	else if ((code [0] == 0xff) && (x86_modrm_reg (code [1]) == 0x6))
		/* push <OFFSET>(<REG>) */
		return 2;
	else if ((code [0] == 0xff) && (x86_modrm_reg (code [1]) == 0x2))
		/* call *<OFFSET>(<REG>) */
		return 2;
	else if ((code [0] == 0xdd) || (code [0] == 0xd9))
		/* fldl <ADDR> */
		return 2;
	else if ((code [0] == 0x58) && (code [1] == 0x05))
		/* pop %eax; add <OFFSET>, %eax */
		return 2;
	else if ((code [0] >= 0x58) && (code [0] <= 0x58 + X86_NREG) && (code [1] == 0x81))
		/* pop <REG>; add <OFFSET>, <REG> */
		return 3;
	else if ((code [0] >= 0xb8) && (code [0] < 0xb8 + 8))
		/* mov <REG>, imm */
		return 1;
	else if (code [0] == 0xE9)
		/* jmp eip+32b */
		return 1;
	g_assert_not_reached ();
	return -1;
}

/**
 * \return TRUE if no sw breakpoint was present (always).
 *
 * Copy \p size bytes from \p code - \p offset to the buffer \p buf. If the debugger inserted software
 * breakpoints in the original code, they are removed in the copy.
 */
gboolean
mono_breakpoint_clean_code (guint8 *method_start, guint8 *code, int offset, guint8 *buf, int size)
{
	/*
	 * If method_start is non-NULL we need to perform bound checks, since we access memory
	 * at code - offset we could go before the start of the method and end up in a different
	 * page of memory that is not mapped or read incorrect data anyway. We zero-fill the bytes
	 * instead.
	 */
	if (!method_start || code - offset >= method_start) {
		memcpy (buf, code - offset, size);
	} else {
		int diff = code - method_start;
		memset (buf, 0, size);
		memcpy (buf + offset - diff, method_start, diff + size - offset);
	}
	return TRUE;
}

/*
 * mono_x86_get_this_arg_offset:
 *
 *   Return the offset of the stack location where this is passed during a virtual
 * call.
 */
guint32
mono_x86_get_this_arg_offset (MonoMethodSignature *sig)
{
	return 0;
}

gpointer
mono_arch_get_this_arg_from_call (host_mgreg_t *regs, guint8 *code)
{
	host_mgreg_t esp = regs [X86_ESP];
	gpointer res;
	int offset;

	offset = 0;

	/*
	 * The stack looks like:
	 * <other args>
	 * <this=delegate>
	 */
	res = ((MonoObject**)esp) [0];
	return res;
}

#define MAX_ARCH_DELEGATE_PARAMS 10

static gpointer
get_delegate_invoke_impl (MonoTrampInfo **info, gboolean has_target, guint32 param_count)
{
	guint8 *code, *start;
	int code_reserve = 64;
	GSList *unwind_ops;

	unwind_ops = mono_arch_get_cie_program ();

	/*
	 * The stack contains:
	 * <delegate>
	 * <return addr>
	 */

	if (has_target) {
		start = code = mono_global_codeman_reserve (code_reserve);

		/* Replace the this argument with the target */
		x86_mov_reg_membase (code, X86_EAX, X86_ESP, 4, 4);
		x86_mov_reg_membase (code, X86_ECX, X86_EAX, MONO_STRUCT_OFFSET (MonoDelegate, target), 4);
		x86_mov_membase_reg (code, X86_ESP, 4, X86_ECX, 4);
		x86_jump_membase (code, X86_EAX, MONO_STRUCT_OFFSET (MonoDelegate, method_ptr));
	} else {
		int i = 0;
		/* 8 for mov_reg and jump, plus 8 for each parameter */
		code_reserve = 8 + (param_count * 8);
		/*
		 * The stack contains:
		 * <args in reverse order>
		 * <delegate>
		 * <return addr>
		 *
		 * and we need:
		 * <args in reverse order>
		 * <return addr>
		 *
		 * without unbalancing the stack.
		 * So move each arg up a spot in the stack (overwriting un-needed 'this' arg)
		 * and leaving original spot of first arg as placeholder in stack so
		 * when callee pops stack everything works.
		 */

		start = code = mono_global_codeman_reserve (code_reserve);

		/* store delegate for access to method_ptr */
		x86_mov_reg_membase (code, X86_ECX, X86_ESP, 4, 4);

		/* move args up */
		for (i = 0; i < param_count; ++i) {
			x86_mov_reg_membase (code, X86_EAX, X86_ESP, (i+2)*4, 4);
			x86_mov_membase_reg (code, X86_ESP, (i+1)*4, X86_EAX, 4);
		}

		x86_jump_membase (code, X86_ECX, MONO_STRUCT_OFFSET (MonoDelegate, method_ptr));
	}

	g_assertf ((code - start) <= code_reserve, "%d %d", (int)(code - start), code_reserve);

	if (has_target) {
		*info = mono_tramp_info_create ("delegate_invoke_impl_has_target", start, code - start, NULL, unwind_ops);
	} else {
		char *name = g_strdup_printf ("delegate_invoke_impl_target_%d", param_count);
		*info = mono_tramp_info_create (name, start, code - start, NULL, unwind_ops);
		g_free (name);
	}

	if (mono_jit_map_is_enabled ()) {
		char *buff;
		if (has_target)
			buff = (char*)"delegate_invoke_has_target";
		else
			buff = g_strdup_printf ("delegate_invoke_no_target_%d", param_count);
		mono_emit_jit_tramp (start, code - start, buff);
		if (!has_target)
			g_free (buff);
	}
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_DELEGATE_INVOKE, NULL));

	return start;
}

#define MAX_VIRTUAL_DELEGATE_OFFSET 32

static gpointer
get_delegate_virtual_invoke_impl (MonoTrampInfo **info, gboolean load_imt_reg, int offset)
{
	guint8 *code, *start;
	int size = 24;
	char *tramp_name;
	GSList *unwind_ops;

	if (offset / (int)sizeof (target_mgreg_t) > MAX_VIRTUAL_DELEGATE_OFFSET)
		return NULL;

	/*
	 * The stack contains:
	 * <delegate>
	 * <return addr>
	 */
	start = code = mono_global_codeman_reserve (size);

	unwind_ops = mono_arch_get_cie_program ();

	/* Replace the this argument with the target */
	x86_mov_reg_membase (code, X86_EAX, X86_ESP, 4, 4);
	x86_mov_reg_membase (code, X86_ECX, X86_EAX, MONO_STRUCT_OFFSET (MonoDelegate, target), 4);
	x86_mov_membase_reg (code, X86_ESP, 4, X86_ECX, 4);

	if (load_imt_reg) {
		/* Load the IMT reg */
		x86_mov_reg_membase (code, MONO_ARCH_IMT_REG, X86_EAX, MONO_STRUCT_OFFSET (MonoDelegate, method), 4);
	}

	/* Load the vtable */
	x86_mov_reg_membase (code, X86_EAX, X86_ECX, MONO_STRUCT_OFFSET (MonoObject, vtable), 4);
	x86_jump_membase (code, X86_EAX, offset);

	g_assertf ((code - start) <= size, "%d %d", (int)(code - start), size);

	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_DELEGATE_INVOKE, NULL));

	tramp_name = mono_get_delegate_virtual_invoke_impl_name (load_imt_reg, offset);
	*info = mono_tramp_info_create (tramp_name, start, code - start, NULL, unwind_ops);
	g_free (tramp_name);


	return start;
}

GSList*
mono_arch_get_delegate_invoke_impls (void)
{
	GSList *res = NULL;
	MonoTrampInfo *info;
	int i;

	get_delegate_invoke_impl (&info, TRUE, 0);
	res = g_slist_prepend (res, info);

	for (i = 0; i <= MAX_ARCH_DELEGATE_PARAMS; ++i) {
		get_delegate_invoke_impl (&info, FALSE, i);
		res = g_slist_prepend (res, info);
	}

	for (i = 0; i <= MAX_VIRTUAL_DELEGATE_OFFSET; ++i) {
		get_delegate_virtual_invoke_impl (&info, TRUE, - i * TARGET_SIZEOF_VOID_P);
		res = g_slist_prepend (res, info);

		get_delegate_virtual_invoke_impl (&info, FALSE, i * TARGET_SIZEOF_VOID_P);
		res = g_slist_prepend (res, info);
	}

	return res;
}

gpointer
mono_arch_get_delegate_invoke_impl (MonoMethodSignature *sig, gboolean has_target)
{
	guint8 *code, *start;

	if (sig->param_count > MAX_ARCH_DELEGATE_PARAMS)
		return NULL;

	/* FIXME: Support more cases */
	if (MONO_TYPE_ISSTRUCT (sig->ret))
		return NULL;

	/*
	 * The stack contains:
	 * <delegate>
	 * <return addr>
	 */

	if (has_target) {
		static guint8* cached = NULL;
		if (cached)
			return cached;

		if (mono_ee_features.use_aot_trampolines) {
			start = (guint8*)mono_aot_get_trampoline ("delegate_invoke_impl_has_target");
		} else {
			MonoTrampInfo *info;
			start = (guint8*)get_delegate_invoke_impl (&info, TRUE, 0);
			mono_tramp_info_register (info, NULL);
		}

		mono_memory_barrier ();

		cached = start;
	} else {
		static guint8* cache [MAX_ARCH_DELEGATE_PARAMS + 1] = {NULL};
		int i = 0;

		for (i = 0; i < sig->param_count; ++i)
			if (!mono_is_regsize_var (sig->params [i]))
				return NULL;

		code = cache [sig->param_count];
		if (code)
			return code;

		if (mono_ee_features.use_aot_trampolines) {
			char *name = g_strdup_printf ("delegate_invoke_impl_target_%d", sig->param_count);
			start = (guint8*)mono_aot_get_trampoline (name);
			g_free (name);
		} else {
			MonoTrampInfo *info;
			start = (guint8*)get_delegate_invoke_impl (&info, FALSE, sig->param_count);
			mono_tramp_info_register (info, NULL);
		}

		mono_memory_barrier ();

		cache [sig->param_count] = start;
	}

	return start;
}

gpointer
mono_arch_get_delegate_virtual_invoke_impl (MonoMethodSignature *sig, MonoMethod *method, int offset, gboolean load_imt_reg)
{
	MonoTrampInfo *info;
	gpointer code;

	code = get_delegate_virtual_invoke_impl (&info, load_imt_reg, offset);
	if (code)
		mono_tramp_info_register (info, NULL);
	return code;
}

host_mgreg_t
mono_arch_context_get_int_reg (MonoContext *ctx, int reg)
{
	switch (reg) {
	case X86_EAX: return ctx->eax;
	case X86_EBX: return ctx->ebx;
	case X86_ECX: return ctx->ecx;
	case X86_EDX: return ctx->edx;
	case X86_ESP: return ctx->esp;
	case X86_EBP: return ctx->ebp;
	case X86_ESI: return ctx->esi;
	case X86_EDI: return ctx->edi;
	default:
		g_assert_not_reached ();
		return 0;
	}
}

host_mgreg_t*
mono_arch_context_get_int_reg_address (MonoContext *ctx, int reg)
{
	switch (reg) {
	case X86_EAX: return &ctx->eax;
	case X86_EBX: return &ctx->ebx;
	case X86_ECX: return &ctx->ecx;
	case X86_EDX: return &ctx->edx;
	case X86_ESP: return &ctx->esp;
	case X86_EBP: return &ctx->ebp;
	case X86_ESI: return &ctx->esi;
	case X86_EDI: return &ctx->edi;
	default:
		g_assert_not_reached ();
		return 0;
	}
}

void
mono_arch_context_set_int_reg (MonoContext *ctx, int reg, host_mgreg_t val)
{
	switch (reg) {
	case X86_EAX:
		ctx->eax = val;
		break;
	case X86_EBX:
		ctx->ebx = val;
		break;
	case X86_ECX:
		ctx->ecx = val;
		break;
	case X86_EDX:
		ctx->edx = val;
		break;
	case X86_ESP:
		ctx->esp = val;
		break;
	case X86_EBP:
		ctx->ebp = val;
		break;
	case X86_ESI:
		ctx->esi = val;
		break;
	case X86_EDI:
		ctx->edi = val;
		break;
	default:
		g_assert_not_reached ();
	}
}

#ifdef MONO_ARCH_SIMD_INTRINSICS

static MonoInst*
get_float_to_x_spill_area (MonoCompile *cfg)
{
	if (!cfg->fconv_to_r8_x_var) {
		cfg->fconv_to_r8_x_var = mono_compile_create_var (cfg, m_class_get_byval_arg (mono_defaults.double_class), OP_LOCAL);
		cfg->fconv_to_r8_x_var->flags |= MONO_INST_VOLATILE; /*FIXME, use the don't regalloc flag*/
	}
	return cfg->fconv_to_r8_x_var;
}

/*
 * Convert all fconv opts that MONO_OPT_SSE2 would get wrong.
 */
void
mono_arch_decompose_opts (MonoCompile *cfg, MonoInst *ins)
{
	MonoInst *fconv;
	int dreg, src_opcode;

	if (!(cfg->opt & MONO_OPT_SSE2) || !(cfg->opt & MONO_OPT_SIMD) || COMPILE_LLVM (cfg))
		return;

	switch (src_opcode = ins->opcode) {
	case OP_FCONV_TO_I1:
	case OP_FCONV_TO_U1:
	case OP_FCONV_TO_I2:
	case OP_FCONV_TO_U2:
	case OP_FCONV_TO_I4:
		break;
	default:
		return;
	}

	/* dreg is the IREG and sreg1 is the FREG */
	MONO_INST_NEW (cfg, fconv, OP_FCONV_TO_R8_X);
	fconv->klass = NULL; /*FIXME, what can I use here as the Mono.Simd lib might not be loaded yet*/
	fconv->sreg1 = ins->sreg1;
	fconv->dreg = mono_alloc_ireg (cfg);
	fconv->type = STACK_VTYPE;
	fconv->backend.spill_var = get_float_to_x_spill_area (cfg);

	mono_bblock_insert_before_ins (cfg->cbb, ins, fconv);

	dreg = ins->dreg;
	NULLIFY_INS (ins);
	ins->opcode = OP_XCONV_R8_TO_I4;

	ins->klass = mono_defaults.int32_class;
	ins->sreg1 = fconv->dreg;
	ins->dreg = dreg;
	ins->type = STACK_I4;
	ins->backend.source_opcode = src_opcode;
}

#endif /* #ifdef MONO_ARCH_SIMD_INTRINSICS */

void
mono_arch_decompose_long_opts (MonoCompile *cfg, MonoInst *long_ins)
{
	MonoInst *ins;

	if (long_ins->opcode == OP_LNEG) {
		ins = long_ins;
		MONO_EMIT_NEW_UNALU (cfg, OP_INEG, MONO_LVREG_LS (ins->dreg), MONO_LVREG_LS (ins->sreg1));
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ADC_IMM, MONO_LVREG_MS (ins->dreg), MONO_LVREG_MS (ins->sreg1), 0);
		MONO_EMIT_NEW_UNALU (cfg, OP_INEG, MONO_LVREG_MS (ins->dreg), MONO_LVREG_MS (ins->dreg));
		NULLIFY_INS (ins);
		return;
	}

#ifdef MONO_ARCH_SIMD_INTRINSICS
	int vreg;
	if (!(cfg->opt & MONO_OPT_SIMD))
		return;

	/*TODO move this to simd-intrinsic.c once we support sse 4.1 dword extractors since we need the runtime caps info */
	switch (long_ins->opcode) {
	case OP_EXTRACT_I8:
		vreg = long_ins->sreg1;

		if (long_ins->inst_c0) {
			MONO_INST_NEW (cfg, ins, OP_PSHUFLED);
			ins->klass = long_ins->klass;
			ins->sreg1 = long_ins->sreg1;
			ins->inst_c0 = 2;
			ins->type = STACK_VTYPE;
			ins->dreg = vreg = alloc_ireg (cfg);
			MONO_ADD_INS (cfg->cbb, ins);
		}

		MONO_INST_NEW (cfg, ins, OP_EXTRACT_I4);
		ins->klass = mono_defaults.int32_class;
		ins->sreg1 = vreg;
		ins->type = STACK_I4;
		ins->dreg = MONO_LVREG_LS (long_ins->dreg);
		MONO_ADD_INS (cfg->cbb, ins);

		MONO_INST_NEW (cfg, ins, OP_PSHUFLED);
		ins->klass = long_ins->klass;
		ins->sreg1 = long_ins->sreg1;
		ins->inst_c0 = long_ins->inst_c0 ? 3 : 1;
		ins->type = STACK_VTYPE;
		ins->dreg = vreg = alloc_ireg (cfg);
		MONO_ADD_INS (cfg->cbb, ins);

		MONO_INST_NEW (cfg, ins, OP_EXTRACT_I4);
		ins->klass = mono_defaults.int32_class;
		ins->sreg1 = vreg;
		ins->type = STACK_I4;
		ins->dreg = MONO_LVREG_MS (long_ins->dreg);
		MONO_ADD_INS (cfg->cbb, ins);

		long_ins->opcode = OP_NOP;
		break;
	case OP_INSERTX_I8_SLOW:
		MONO_INST_NEW (cfg, ins, OP_INSERTX_I4_SLOW);
		ins->dreg = long_ins->dreg;
		ins->sreg1 = long_ins->dreg;
		ins->sreg2 = MONO_LVREG_LS (long_ins->sreg2);
		ins->inst_c0 = long_ins->inst_c0 * 2;
		MONO_ADD_INS (cfg->cbb, ins);

		MONO_INST_NEW (cfg, ins, OP_INSERTX_I4_SLOW);
		ins->dreg = long_ins->dreg;
		ins->sreg1 = long_ins->dreg;
		ins->sreg2 = MONO_LVREG_MS (long_ins->sreg2);
		ins->inst_c0 = long_ins->inst_c0 * 2 + 1;
		MONO_ADD_INS (cfg->cbb, ins);

		long_ins->opcode = OP_NOP;
		break;
	case OP_EXPAND_I8:
		MONO_INST_NEW (cfg, ins, OP_ICONV_TO_X);
		ins->dreg = long_ins->dreg;
		ins->sreg1 = MONO_LVREG_LS (long_ins->sreg1);
		ins->klass = long_ins->klass;
		ins->type = STACK_VTYPE;
		MONO_ADD_INS (cfg->cbb, ins);

		MONO_INST_NEW (cfg, ins, OP_INSERTX_I4_SLOW);
		ins->dreg = long_ins->dreg;
		ins->sreg1 = long_ins->dreg;
		ins->sreg2 = MONO_LVREG_MS (long_ins->sreg1);
		ins->inst_c0 = 1;
		ins->klass = long_ins->klass;
		ins->type = STACK_VTYPE;
		MONO_ADD_INS (cfg->cbb, ins);

		MONO_INST_NEW (cfg, ins, OP_PSHUFLED);
		ins->dreg = long_ins->dreg;
		ins->sreg1 = long_ins->dreg;
		ins->inst_c0 = 0x44; /*Magic number for swizzling (X,Y,X,Y)*/
		ins->klass = long_ins->klass;
		ins->type = STACK_VTYPE;
		MONO_ADD_INS (cfg->cbb, ins);

		long_ins->opcode = OP_NOP;
		break;
	}
#endif /* MONO_ARCH_SIMD_INTRINSICS */
}

/*
 * mono_aot_emit_load_got_addr:
 *
 *   Emit code to load the got address.
 * On x86, the result is placed into EBX.
 */
guint8*
mono_arch_emit_load_got_addr (guint8 *start, guint8 *code, MonoCompile *cfg, MonoJumpInfo **ji)
{
	x86_call_imm (code, 0);
	/*
	 * The patch needs to point to the pop, since the GOT offset needs
	 * to be added to that address.
	 */
	if (cfg)
		mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_GOT_OFFSET, NULL);
	else
		*ji = mono_patch_info_list_prepend (*ji, code - start, MONO_PATCH_INFO_GOT_OFFSET, NULL);
	x86_pop_reg (code, MONO_ARCH_GOT_REG);
	x86_alu_reg_imm (code, X86_ADD, MONO_ARCH_GOT_REG, 0xf0f0f0f0);
	if (cfg)
		set_code_cursor (cfg, code);
	return code;
}

/*
 * mono_arch_emit_load_aotconst:
 *
 *   Emit code to load the contents of the GOT slot identified by TRAMP_TYPE and
 * TARGET from the mscorlib GOT in full-aot code.
 * On x86, the GOT address is assumed to be in EBX, and the result is placed into
 * EAX.
 */
guint8*
mono_arch_emit_load_aotconst (guint8 *start, guint8 *code, MonoJumpInfo **ji, MonoJumpInfoType tramp_type, gconstpointer target)
{
	/* Load the mscorlib got address */
	x86_mov_reg_membase (code, X86_EAX, MONO_ARCH_GOT_REG, sizeof (target_mgreg_t), 4);
	*ji = mono_patch_info_list_prepend (*ji, code - start, tramp_type, target);
	/* arch_emit_got_access () patches this */
	x86_mov_reg_membase (code, X86_EAX, X86_EAX, 0xf0f0f0f0, 4);

	return code;
}

/* Can't put this into mini-x86.h */
gpointer
mono_x86_get_signal_exception_trampoline (MonoTrampInfo **info, gboolean aot);

GSList *
mono_arch_get_trampolines (gboolean aot)
{
	MonoTrampInfo *info;
	GSList *tramps = NULL;

	mono_x86_get_signal_exception_trampoline (&info, aot);

	tramps = g_slist_append (tramps, info);

	return tramps;
}

/* Soft Debug support */
#ifdef MONO_ARCH_SOFT_DEBUG_SUPPORTED

/*
 * mono_arch_set_breakpoint:
 *
 *   Set a breakpoint at the native code corresponding to JI at NATIVE_OFFSET.
 * The location should contain code emitted by OP_SEQ_POINT.
 */
void
mono_arch_set_breakpoint (MonoJitInfo *ji, guint8 *ip)
{
	guint8 *code = ip + OP_SEQ_POINT_BP_OFFSET;

	g_assert (code [0] == 0x90);
	x86_call_membase (code, X86_ECX, 0);
}

/*
 * mono_arch_clear_breakpoint:
 *
 *   Clear the breakpoint at IP.
 */
void
mono_arch_clear_breakpoint (MonoJitInfo *ji, guint8 *ip)
{
	guint8 *code = ip + OP_SEQ_POINT_BP_OFFSET;
	int i;

	for (i = 0; i < 2; ++i)
		x86_nop (code);
}

/*
 * mono_arch_start_single_stepping:
 *
 *   Start single stepping.
 */
void
mono_arch_start_single_stepping (void)
{
	ss_trampoline = mini_get_single_step_trampoline ();
}

/*
 * mono_arch_stop_single_stepping:
 *
 *   Stop single stepping.
 */
void
mono_arch_stop_single_stepping (void)
{
	ss_trampoline = NULL;
}

/*
 * mono_arch_is_single_step_event:
 *
 *   Return whenever the machine state in SIGCTX corresponds to a single
 * step event.
 */
gboolean
mono_arch_is_single_step_event (void *info, void *sigctx)
{
	/* We use soft breakpoints */
	return FALSE;
}

gboolean
mono_arch_is_breakpoint_event (void *info, void *sigctx)
{
	/* We use soft breakpoints */
	return FALSE;
}

#define BREAKPOINT_SIZE 2

/*
 * mono_arch_skip_breakpoint:
 *
 *   See mini-amd64.c for docs.
 */
void
mono_arch_skip_breakpoint (MonoContext *ctx, MonoJitInfo *ji)
{
	g_assert_not_reached ();
}

/*
 * mono_arch_skip_single_step:
 *
 *   See mini-amd64.c for docs.
 */
void
mono_arch_skip_single_step (MonoContext *ctx)
{
	g_assert_not_reached ();
}

/*
 * mono_arch_get_seq_point_info:
 *
 *   See mini-amd64.c for docs.
 */
SeqPointInfo*
mono_arch_get_seq_point_info (guint8 *code)
{
	NOT_IMPLEMENTED;
	return NULL;
}

#endif

gboolean
mono_arch_opcode_supported (int opcode)
{
	switch (opcode) {
	case OP_ATOMIC_ADD_I4:
	case OP_ATOMIC_EXCHANGE_I4:
	case OP_ATOMIC_CAS_I4:
	case OP_ATOMIC_LOAD_I1:
	case OP_ATOMIC_LOAD_I2:
	case OP_ATOMIC_LOAD_I4:
	case OP_ATOMIC_LOAD_U1:
	case OP_ATOMIC_LOAD_U2:
	case OP_ATOMIC_LOAD_U4:
	case OP_ATOMIC_LOAD_R4:
	case OP_ATOMIC_LOAD_R8:
	case OP_ATOMIC_STORE_I1:
	case OP_ATOMIC_STORE_I2:
	case OP_ATOMIC_STORE_I4:
	case OP_ATOMIC_STORE_U1:
	case OP_ATOMIC_STORE_U2:
	case OP_ATOMIC_STORE_U4:
	case OP_ATOMIC_STORE_R4:
	case OP_ATOMIC_STORE_R8:
		return TRUE;
	default:
		return FALSE;
	}
}

CallInfo*
mono_arch_get_call_info (MonoMemPool *mp, MonoMethodSignature *sig)
{
	return get_call_info (mp, sig);
}

gpointer
mono_arch_load_function (MonoJitICallId jit_icall_id)
{
	gpointer target = NULL;
	switch (jit_icall_id) {
#undef MONO_AOT_ICALL
#define MONO_AOT_ICALL(x) case MONO_JIT_ICALL_ ## x: target = (gpointer)x; break;
	MONO_AOT_ICALL (mono_x86_start_gsharedvt_call)
	MONO_AOT_ICALL (mono_x86_throw_corlib_exception)
	MONO_AOT_ICALL (mono_x86_throw_exception)
	}
	return target;
}
