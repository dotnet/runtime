/*
 * Licensed to the .NET Foundation under one or more agreements.
 * The .NET Foundation licenses this file to you under the MIT license.
 */

#include <mono/utils/mono-hwcap.h>

#include "mini-runtime.h"
#include "ir-emit.h"

#include <mono/metadata/tokentype.h>

#ifdef TARGET_RISCV64
#include "cpu-riscv64.h"
#else
#include "cpu-riscv32.h"
#endif

/* The single step trampoline */
static gpointer ss_trampoline;

/* The breakpoint trampoline */
static gpointer bp_trampoline;

gboolean riscv_stdext_a, riscv_stdext_b, riscv_stdext_c, riscv_stdext_d, riscv_stdext_f, riscv_stdext_j, riscv_stdext_l,
    riscv_stdext_m, riscv_stdext_n, riscv_stdext_p, riscv_stdext_q, riscv_stdext_t, riscv_stdext_v;

void
mono_arch_cpu_init (void)
{
}

void
mono_arch_init (void)
{
	riscv_stdext_a = mono_hwcap_riscv_has_stdext_a;
	// TODO: skip compress inst for now
	riscv_stdext_c = FALSE;
	riscv_stdext_d = mono_hwcap_riscv_has_stdext_d;
	riscv_stdext_f = mono_hwcap_riscv_has_stdext_f;
	riscv_stdext_m = mono_hwcap_riscv_has_stdext_m;

	if (!mono_aot_only)
		bp_trampoline = mini_get_breakpoint_trampoline ();
}

void
mono_arch_finish_init (void)
{
}

void
mono_arch_register_lowlevel_calls (void)
{
}

void
mono_arch_cleanup (void)
{
}

void
mono_arch_set_target (char *mtriple)
{
	// riscv{32,64}[extensions]-[<vendor>-]<system>-<abi>

	size_t len = strlen (MONO_RISCV_ARCHITECTURE);

	if (!strncmp (mtriple, MONO_RISCV_ARCHITECTURE, len)) {
		mtriple += len;

		for (;;) {
			char c = *mtriple;

			if (!c || c == '-')
				break;

			// ISA manual says upper and lower case are both OK.
			switch (c) {
			case 'A':
			case 'a':
				riscv_stdext_a = TRUE;
				break;
			case 'B':
			case 'b':
				riscv_stdext_b = TRUE;
				break;
			case 'C':
			case 'c':
				riscv_stdext_c = TRUE;
				break;
			case 'D':
			case 'd':
				riscv_stdext_d = TRUE;
				break;
			case 'F':
			case 'f':
				riscv_stdext_f = TRUE;
				break;
			case 'J':
			case 'j':
				riscv_stdext_j = TRUE;
				break;
			case 'L':
			case 'l':
				riscv_stdext_l = TRUE;
				break;
			case 'M':
			case 'm':
				riscv_stdext_m = TRUE;
				break;
			case 'N':
			case 'n':
				riscv_stdext_n = TRUE;
				break;
			case 'P':
			case 'p':
				riscv_stdext_p = TRUE;
				break;
			case 'Q':
			case 'q':
				riscv_stdext_q = TRUE;
				break;
			case 'T':
			case 't':
				riscv_stdext_t = TRUE;
				break;
			case 'V':
			case 'v':
				riscv_stdext_v = TRUE;
				break;
			default:
				break;
			}

			mtriple++;
		}
	}
}

guint32
mono_arch_cpu_optimizations (guint32 *exclude_mask)
{
	*exclude_mask = 0;
	return 0;
}

gboolean
mono_arch_have_fast_tls (void)
{
	return TRUE;
}

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
	case OP_ATOMIC_STORE_I1:
	case OP_ATOMIC_STORE_I2:
	case OP_ATOMIC_STORE_I4:
	case OP_ATOMIC_STORE_U1:
	case OP_ATOMIC_STORE_U2:
	case OP_ATOMIC_STORE_U4:
#ifdef TARGET_RISCV64
	case OP_ATOMIC_ADD_I8:
	case OP_ATOMIC_EXCHANGE_I8:
	case OP_ATOMIC_CAS_I8:
	case OP_ATOMIC_LOAD_I8:
	case OP_ATOMIC_LOAD_U8:
	case OP_ATOMIC_STORE_I8:
	case OP_ATOMIC_STORE_U8:
#endif
		return riscv_stdext_a;
	case OP_ATOMIC_LOAD_R4:
	case OP_ATOMIC_STORE_R4:
		return riscv_stdext_a && riscv_stdext_f;
	case OP_ATOMIC_LOAD_R8:
	case OP_ATOMIC_STORE_R8:
		return riscv_stdext_a && riscv_stdext_d;
	default:
		return FALSE;
	}
}

const char *
mono_arch_regname (int reg)
{
    static const char *names [RISCV_N_GREGS] = {
		"zero", "ra", "sp",  "gp",  "tp", "t0", "t1", "t2",
		"s0",   "s1", "a0",  "a1",  "a2", "a3", "a4", "a5",
		"a6",   "a7", "s2",  "s3",  "s4", "s5", "s6", "s7",
		"s8",   "s9", "s10", "s11", "t3", "t4", "t5", "t6",
    };

    if (reg >= 0 && reg < G_N_ELEMENTS (names))
        return names [reg];

    return "x?";
}

const char*
mono_arch_fregname (int reg)
{
    static const char *names [RISCV_N_FREGS] = {
		"ft0", "ft1", "ft2",  "ft3",  "ft4", "ft5", "ft6",  "ft7",
		"fs0", "fs1", "fa0",  "fa1",  "fa2", "fa3", "fa4",  "fa5",
		"fa6", "fa7", "fs2",  "fs3",  "fs4", "fs5", "fs6",  "fs7",
		"fs8", "fs9", "fs10", "fs11", "ft8", "ft9", "ft10", "ft11",
    };

    if (reg >= 0 && reg < G_N_ELEMENTS (names))
        return names [reg];

    return "f?";
}

gpointer
mono_arch_get_this_arg_from_call (host_mgreg_t *regs, guint8 *code)
{
	return (gpointer) regs [RISCV_A0];
}

MonoMethod *
mono_arch_find_imt_method (host_mgreg_t *regs, guint8 *code)
{
	return (MonoMethod *) regs [MONO_ARCH_IMT_REG];
}

MonoVTable *
mono_arch_find_static_call_vtable (host_mgreg_t *regs, guint8 *code)
{
	return (MonoVTable *)regs [MONO_ARCH_RGCTX_REG];
}

GSList*
mono_arch_get_cie_program (void)
{
	GSList *l = NULL;

	mono_add_unwind_op_def_cfa (l, (guint8*)NULL, (guint8*)NULL, RISCV_SP, 0);

	return l;
}

host_mgreg_t
mono_arch_context_get_int_reg (MonoContext *ctx, int reg)
{
	return ctx->gregs [reg];
}

host_mgreg_t*
mono_arch_context_get_int_reg_address (MonoContext *ctx, int reg)
{
	return &ctx->gregs [reg];
}

void
mono_arch_context_set_int_reg (MonoContext *ctx, int reg, host_mgreg_t val)
{
	ctx->gregs [reg] = val;
}

void
mono_arch_flush_register_windows (void)
{
}

void
mono_arch_flush_icache (guint8 *code, gint size)
{
#ifndef MONO_CROSS_COMPILE
	__builtin___clear_cache ((char *)code, (char *)code + size);
#endif
}

MonoDynCallInfo *
mono_arch_dyn_call_prepare (MonoMethodSignature *sig)
{
	NOT_IMPLEMENTED;
	return NULL;
}

void
mono_arch_dyn_call_free (MonoDynCallInfo *info)
{
	NOT_IMPLEMENTED;
}

int
mono_arch_dyn_call_get_buf_size (MonoDynCallInfo *info)
{
	NOT_IMPLEMENTED;
	return 0;
}

void
mono_arch_start_dyn_call (MonoDynCallInfo *info, gpointer **args, guint8 *ret,
                          guint8 *buf)
{
	NOT_IMPLEMENTED;
}

void
mono_arch_finish_dyn_call (MonoDynCallInfo *info, guint8 *buf)
{
	NOT_IMPLEMENTED;
}

int
mono_arch_get_argument_info (MonoMethodSignature *csig, int param_count,
                             MonoJitArgumentInfo *arg_info)
{
    NOT_IMPLEMENTED;
    return 0;
}

/**
 * [NFC] Because there is OP_LOCALLOC stuff,
 * the stack size will increase dynamicly,
 * so the stack size stored in cfg->stack_offset
 * can't guide us to destroy the frame.
 *
 * Emits:
 * 	 addi sp,fp, 0
 *   ld ra, -8(fp) # 8-byte Folded Reload
 *   ld s0, -16(fp) # 8-byte Folded Reload
 */
guint8 *
mono_riscv_emit_destroy_frame (guint8 *code)
{
	riscv_addi (code, RISCV_SP, RISCV_FP, 0);
	code = mono_riscv_emit_load (code, RISCV_RA, RISCV_FP, -(gint32)sizeof (host_mgreg_t), 0);
	code = mono_riscv_emit_load (code, RISCV_S0, RISCV_FP, -(gint32)sizeof (host_mgreg_t) * 2, 0);

	return code;
}
void
mono_arch_patch_code_new (MonoCompile *cfg, guint8 *code,
                          MonoJumpInfo *ji, gpointer target)
{
	NOT_IMPLEMENTED;
}

/**
 * add_arg:
 * 	Add Arguments into a0-a7 reg.
 * 	if there is no available regs, store it into stack.
 */
static void
add_arg (CallInfo *cinfo, ArgInfo *ainfo, int size, gboolean sign)
{
	g_assert (size <= 8);
	g_assert (cinfo->next_arg >= RISCV_A0);

	if (cinfo->vararg) {
		NOT_IMPLEMENTED;
	}

	// for RV64, length of all arg here will not wider then XLEN
	// store it normally.
	if (cinfo->next_arg <= RISCV_A7) {
		// there is at least 1 avaliable reg
#ifdef TARGET_RISCV32
		// Scalars that are 2×XLEN bits wide are passed in a pair of argument registers
		if (size == 8) {
			NOT_IMPLEMENTED;
			// If exactly one register is available, the low-order XLEN bits are
			// passed in the register and the high-order XLEN bits are passed on the stack
			if (cinfo->next_arg == RISCV_A7) {
				NOT_IMPLEMENTED;
			}
			return;
		}
#endif
		ainfo->storage = ArgInIReg;
		ainfo->reg = cinfo->next_arg;
		cinfo->next_arg++;
	}
	// If no argument registers are available, the scalar is passed on the stack by value
	else {
		// 	cinfo->stack_usage & ainfo->offset
		// will be calculated in get_call_info()
		ainfo->storage = ArgOnStack;
		ainfo->slot_size = size;
		ainfo->is_signed = sign;
	}
}

static void
add_farg (CallInfo *cinfo, ArgInfo *ainfo, gboolean single)
{
	int size = single ? 4 : 8;

	g_assert (mono_arch_is_soft_float () == FALSE);
	if (cinfo->next_farg <= RISCV_FA7) {
#ifdef TARGET_RISCV64
		ainfo->storage = single ? ArgInFRegR4 : ArgInFReg;
		ainfo->reg = cinfo->next_farg;
		cinfo->next_farg++;
#else
		NOT_IMPLEMENTED;
#endif
	} else {
		ainfo->storage = single ? ArgOnStackR4 : ArgOnStackR8;
		ainfo->slot_size = size;
	}
}

static void
add_valuetype (CallInfo *cinfo, ArgInfo *ainfo, MonoType *t)
{
	int size, aligned_size;
	guint32 align;

	size = mini_type_stack_size_full (t, &align, cinfo->pinvoke);
	aligned_size = ALIGN_TO (size, align);

	// Scalars wider than 2×XLEN bits are passed by reference
	if (aligned_size > sizeof (host_mgreg_t) * 2) {
		if (cinfo->next_arg > RISCV_A7) {
			ainfo->storage = ArgVtypeByRefOnStack;
			cinfo->stack_usage += aligned_size;
			ainfo->slot_size = aligned_size;
			ainfo->offset = cinfo->stack_usage;
		} else {
			ainfo->storage = ArgVtypeByRef;
			ainfo->reg = cinfo->next_arg;
			ainfo->size = sizeof (host_mgreg_t);
			ainfo->is_regpair = FALSE;
			cinfo->next_arg += 1;
		}
	}
	// Scalars that are 2×XLEN bits wide are passed in a pair of argument registers
	else if (aligned_size == sizeof (host_mgreg_t) * 2) {
		// If no argument registers are available, the scalar is passed on the stack by value
		if (cinfo->next_arg > RISCV_A7) {
			ainfo->storage = ArgVtypeOnStack;
			cinfo->stack_usage += sizeof (host_mgreg_t) * 2;
			ainfo->slot_size = sizeof (host_mgreg_t) * 2;
			ainfo->offset = cinfo->stack_usage;
		}
		// If exactly one register is available, the low-order XLEN bits are
		// passed in the register and the high-order XLEN bits are passed on the stack
		else if (cinfo->next_arg == RISCV_A7) {
			ainfo->storage = ArgVtypeInMixed;
			cinfo->stack_usage += sizeof (host_mgreg_t);
			ainfo->slot_size = sizeof (host_mgreg_t);
			ainfo->offset = cinfo->stack_usage;

			ainfo->reg = cinfo->next_arg;
			ainfo->size = sizeof (host_mgreg_t);
			ainfo->is_regpair = FALSE;

			cinfo->next_arg += 1;
		}
		// Scalars that are 2×XLEN bits wide are passed in a pair of argument
		// registers, with the low-order XLEN bits in the lower-numbered register
		// and the high-order XLEN bits in the higher-numbered register
		else {
			ainfo->storage = ArgVtypeInIReg;
			ainfo->reg = cinfo->next_arg;
			ainfo->size = sizeof (host_mgreg_t) * 2;
			ainfo->is_regpair = TRUE;

			cinfo->next_arg += 2;
		}
	}
	// Scalars that are at most XLEN bits wide are passed in a single argument register
	else {
		ainfo->storage = ArgVtypeInIReg;
		ainfo->reg = cinfo->next_arg;
		ainfo->size = sizeof (host_mgreg_t);
		ainfo->is_regpair = FALSE;

		cinfo->next_arg += 1;
	}
}

static void
add_param (CallInfo *cinfo, ArgInfo *ainfo, MonoType *t)
{
	MonoType *ptype;

	ptype = mini_get_underlying_type (t);
	// FIXME: May break some ABI rules
	switch (ptype->type) {
	case MONO_TYPE_VOID:
		ainfo->storage = ArgNone;
		break;
	case MONO_TYPE_I1:
		add_arg (cinfo, ainfo, 1, TRUE);
		break;
	case MONO_TYPE_U1:
		add_arg (cinfo, ainfo, 1, FALSE);
		break;
	case MONO_TYPE_I2:
		add_arg (cinfo, ainfo, 2, TRUE);
		break;
	case MONO_TYPE_U2:
		add_arg (cinfo, ainfo, 2, FALSE);
		break;
	case MONO_TYPE_I:
	case MONO_TYPE_I4:
		add_arg (cinfo, ainfo, 4, TRUE);
		break;
	case MONO_TYPE_U:
	case MONO_TYPE_U4:
#ifdef TARGET_RISCV32
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_OBJECT:
#endif
		add_arg (cinfo, ainfo, 4, FALSE);
		break;
	case MONO_TYPE_I8:
		add_arg (cinfo, ainfo, 8, TRUE);
		break;
	case MONO_TYPE_U8:
#ifdef TARGET_RISCV64
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_OBJECT:
#endif
		add_arg (cinfo, ainfo, 8, FALSE);
		break;
	case MONO_TYPE_R4:
		if (mono_arch_is_soft_float ())
			add_arg (cinfo, ainfo, 4, TRUE);
		else
			add_farg (cinfo, ainfo, TRUE);
		break;
	case MONO_TYPE_R8:
		if (mono_arch_is_soft_float ())
			add_arg (cinfo, ainfo, 8, FALSE);
		else
			add_farg (cinfo, ainfo, FALSE);
		break;

	case MONO_TYPE_GENERICINST:
		if (!mono_type_generic_inst_is_valuetype (ptype))
			add_arg (cinfo, ainfo, sizeof (host_mgreg_t), FALSE);
		else if (mini_is_gsharedvt_variable_type (ptype))
			NOT_IMPLEMENTED;
		else
			add_valuetype (cinfo, ainfo, ptype);
		break;
	case MONO_TYPE_VALUETYPE:
		add_valuetype (cinfo, ainfo, ptype);
		break;

	default:
		g_print ("Can't handle as return value 0x%x\n", ptype->type);
		g_assert_not_reached ();
		break;
	}
}

/**
 * get_call_info:
 * 	create call info here.
 *  allocate memory for *cinfo, and assign Regs for Arguments.
 *  if thsere is no available regs, store it into stack top of caller
 *  in increase order.
 *  eg. 8th arg is stored at sp+8, 9th arg is stored at sp+0, etc.
 */
static CallInfo *
get_call_info (MonoMemPool *mp, MonoMethodSignature *sig)
{
	CallInfo *cinfo;
	int paramNum = sig->hasthis + sig->param_count;
	int pindex;
	if (mp)
		cinfo = mono_mempool_alloc0 (mp, sizeof (CallInfo) + (sizeof (ArgInfo) * paramNum));
	else
		cinfo = g_malloc0 (sizeof (CallInfo) + (sizeof (ArgInfo) * paramNum));

	cinfo->nargs = paramNum;

	// return value
	cinfo->next_arg = RISCV_A0;
	cinfo->next_farg = RISCV_FA0;
	add_param (cinfo, &cinfo->ret, sig->ret);

	//  If the reture value would have been passed by reference,
	// the caller allocates memory for the return value, and
	// passes the address as an implicit first parameter.
	if (cinfo->ret.storage == ArgVtypeByRef) {
		g_assert (cinfo->ret.reg == RISCV_A0);
		cinfo->next_arg = RISCV_A1;
	} else
		cinfo->next_arg = RISCV_A0;

	cinfo->next_farg = RISCV_FA0;
	// reset status
	cinfo->stack_usage = 0;

	// add this pointer as first argument if hasthis == true
	if (sig->hasthis)
		add_arg (cinfo, cinfo->args + 0, 8, FALSE);

	// other general Arguments
	guint32 paramStart = 0;
	guint32 argStack = 0;
	for (pindex = paramStart; pindex < sig->param_count; ++pindex) {
		ArgInfo *ainfo = cinfo->args + sig->hasthis + pindex;

		// process the variable parameter sig->sentinelpos mark the first VARARG
		if ((sig->call_convention == MONO_CALL_VARARG) && (pindex == sig->sentinelpos))
			NOT_IMPLEMENTED;

		add_param (cinfo, ainfo, sig->params [pindex]);

		if (ainfo->storage == ArgOnStack || ainfo->storage == ArgOnStackR4 || ainfo->storage == ArgOnStackR8)
			argStack += ainfo->slot_size;
	}

	// reserve the regs stored at the srack
	if (argStack > 0) {
		cinfo->stack_usage += argStack;

		for (pindex = paramStart; pindex < sig->param_count; ++pindex) {
			ArgInfo *ainfo = cinfo->args + sig->hasthis + pindex;
			if (ainfo->storage == ArgOnStack || ainfo->storage == ArgOnStackR4 || ainfo->storage == ArgOnStackR8) {
				g_assert (argStack >= ainfo->slot_size);
				argStack -= ainfo->slot_size;
				ainfo->offset = argStack;
			}
		}
	}

	/* Handle the case where there are no implicit arguments */
	if ((sig->call_convention == MONO_CALL_VARARG) && (pindex == sig->sentinelpos))
		NOT_IMPLEMENTED;

	cinfo->stack_usage = ALIGN_TO (cinfo->stack_usage, MONO_ARCH_FRAME_ALIGNMENT);

	return cinfo;
}

static int
arg_need_temp (ArgInfo *ainfo)
{
	if (ainfo->storage == ArgVtypeInMixed)
		return sizeof (host_mgreg_t) * 2;
	return 0;
}

static gpointer
arg_get_storage (CallContext *ccontext, ArgInfo *ainfo)
{
	switch (ainfo->storage) {
	case ArgInIReg:
	case ArgVtypeInIReg:
		return &ccontext->gregs [ainfo->reg];
	case ArgInFReg:
		return &ccontext->fregs [ainfo->reg];
	case ArgOnStack:
	case ArgVtypeOnStack:
		return ccontext->stack + ainfo->offset;
	case ArgVtypeByRef:
		return (gpointer)ccontext->gregs [ainfo->reg];
	default:
		g_print ("Can't process storage type %d\n", ainfo->storage);
		NOT_IMPLEMENTED;
	}
}

static void
arg_set_val (CallContext *ccontext, ArgInfo *ainfo, gpointer src)
{
	g_assert (arg_need_temp (ainfo));
	NOT_IMPLEMENTED;
}

static void
arg_get_val (CallContext *ccontext, ArgInfo *ainfo, gpointer dest)
{
	g_assert (arg_need_temp (ainfo));
	NOT_IMPLEMENTED;
}

/* Set arguments in the ccontext (for i2n entry) */
void
mono_arch_set_native_call_context_args (CallContext *ccontext, gpointer frame, MonoMethodSignature *sig)
{
	const MonoEECallbacks *interp_cb = mini_get_interp_callbacks ();
	CallInfo *cinfo = get_call_info (NULL, sig);
	gpointer storage;
	ArgInfo *ainfo;

	memset (ccontext, 0, sizeof (CallContext));

	ccontext->stack_size = ALIGN_TO (cinfo->stack_usage, MONO_ARCH_FRAME_ALIGNMENT);
	if (ccontext->stack_size)
		ccontext->stack = (guint8 *)g_calloc (1, ccontext->stack_size);

	if (sig->ret->type != MONO_TYPE_VOID) {
		ainfo = &cinfo->ret;
		if (ainfo->storage == ArgVtypeByRef) {
			g_assert (cinfo->ret.reg == RISCV_A0);
			storage = interp_cb->frame_arg_to_storage ((MonoInterpFrameHandle)frame, sig, -1);
			ccontext->gregs [cinfo->ret.reg] = (gsize)storage;
		}
	}

	g_assert (!sig->hasthis);

	for (int i = 0; i < sig->param_count; i++) {
		ainfo = &cinfo->args [i];

		if (ainfo->storage == ArgVtypeByRef) {
			ccontext->gregs [ainfo->reg] =
			    (host_mgreg_t)interp_cb->frame_arg_to_storage ((MonoInterpFrameHandle)frame, sig, i);
			continue;
		}

		int temp_size = arg_need_temp (ainfo);

		if (temp_size)
			storage = alloca (temp_size); // FIXME? alloca in a loop
		else
			storage = arg_get_storage (ccontext, ainfo);

		interp_cb->frame_arg_to_data ((MonoInterpFrameHandle)frame, sig, i, storage);
		if (temp_size)
			arg_set_val (ccontext, ainfo, storage);
	}

	g_free (cinfo);
}

/* Set return value in the ccontext (for n2i return) */
void
mono_arch_set_native_call_context_ret (CallContext *ccontext, gpointer frame, MonoMethodSignature *sig, gpointer retp)
{
	NOT_IMPLEMENTED;
}

/* Gets the arguments from ccontext (for n2i entry) */
gpointer
mono_arch_get_native_call_context_args (CallContext *ccontext, gpointer frame, MonoMethodSignature *sig)
{
	NOT_IMPLEMENTED;
}

/* Gets the return value from ccontext (for i2n exit) */
void
mono_arch_get_native_call_context_ret (CallContext *ccontext, gpointer frame, MonoMethodSignature *sig)
{
	const MonoEECallbacks *interp_cb;
	CallInfo *cinfo;
	ArgInfo *ainfo;
	gpointer storage;

	if (sig->ret->type == MONO_TYPE_VOID)
		return;

	interp_cb = mini_get_interp_callbacks ();
	cinfo = get_call_info (NULL, sig);
	ainfo = &cinfo->ret;

	if (ainfo->storage != ArgVtypeByRef) {
		int temp_size = arg_need_temp (ainfo);

		if (temp_size) {
			storage = alloca (temp_size);
			arg_get_val (ccontext, ainfo, storage);
		} else
			storage = arg_get_storage (ccontext, ainfo);
		interp_cb->data_to_frame_arg ((MonoInterpFrameHandle)frame, sig, -1, storage);
	}

	g_free (cinfo);
}

#ifndef DISABLE_JIT

#ifdef MONO_ARCH_SOFT_FLOAT_FALLBACK

gboolean
mono_arch_is_soft_float (void)
{
	return !riscv_stdext_d;
}

#endif

gboolean
mono_arch_opcode_needs_emulation (MonoCompile *cfg, int opcode)
{
	switch (opcode) {
	case OP_IDIV:
	case OP_IDIV_UN:
	case OP_IREM:
	case OP_IREM_UN:
#ifdef TARGET_RISCV64
	case OP_LDIV:
	case OP_LDIV_UN:
	case OP_LREM:
	case OP_LREM_UN:
#endif
		return !riscv_stdext_m;
	default:
		return TRUE;
	}
}

gboolean
mono_arch_tailcall_supported (MonoCompile *cfg, MonoMethodSignature *caller_sig, MonoMethodSignature *callee_sig, gboolean virtual_)
{
	NOT_IMPLEMENTED;
}

gboolean
mono_arch_is_inst_imm (int opcode, int imm_opcode, gint64 imm)
{
	// TODO: Make a proper decision based on opcode.
	return TRUE;
}

GList *
mono_arch_get_allocatable_int_vars (MonoCompile *cfg)
{
	GList *vars = NULL;

	for (guint i = 0; i < cfg->num_varinfo; i++) {
		MonoInst *ins = cfg->varinfo [i];
		MonoMethodVar *vmv = MONO_VARINFO (cfg, i);

		if (vmv->range.first_use.abs_pos >= vmv->range.last_use.abs_pos)
			continue;

		if ((ins->flags & (MONO_INST_IS_DEAD | MONO_INST_VOLATILE | MONO_INST_INDIRECT)) ||
		    (ins->opcode != OP_LOCAL && ins->opcode != OP_ARG))
			continue;

		if (!mono_is_regsize_var (ins->inst_vtype))
			continue;

		vars = g_list_prepend (vars, vmv);
	}

	vars = mono_varlist_sort (cfg, vars, 0);

	return vars;
}

GList *
mono_arch_get_global_int_regs (MonoCompile *cfg)
{
	GList *regs = NULL;

	// RISCV_FP aka RISCV_S0 is reserved
	regs = g_list_prepend (regs, GUINT_TO_POINTER (RISCV_S1));
	for (int i = RISCV_S2; i <= RISCV_S11; i++)
		regs = g_list_prepend (regs, GUINT_TO_POINTER (i));

	return regs;
}

guint32
mono_arch_regalloc_cost (MonoCompile *cfg, MonoMethodVar *vmv)
{
	return cfg->varinfo [vmv->idx]->opcode == OP_ARG ? 1 : 2;
}

#ifdef ENABLE_LLVM

LLVMCallInfo*
mono_arch_get_llvm_call_info (MonoCompile *cfg, MonoMethodSignature *sig)
{
	NOT_IMPLEMENTED;
}

#endif

/**
 * mono_arch_create_vars:
 *	before this function, mono_compile_create_vars() in mini.c
 *	has process vars in a genetic ways. So just do some Arch
 *	related process specified in ABI.
 */

void
mono_arch_create_vars (MonoCompile *cfg)
{
	MonoMethodSignature *sig;
	CallInfo *cinfo;

	sig = mono_method_signature_internal (cfg->method);
	if (!cfg->arch.cinfo)
		cfg->arch.cinfo = get_call_info (cfg->mempool, sig);
	cinfo = cfg->arch.cinfo;

	if (cinfo->ret.storage == ArgVtypeByRef) {
		cfg->vret_addr = mono_compile_create_var (cfg, mono_get_int_type (), OP_LOCAL);
		cfg->vret_addr->flags |= MONO_INST_VOLATILE;
	}

	if (cfg->gen_sdb_seq_points) {
		MonoInst *ins;

		if (cfg->compile_aot) {
			ins = mono_compile_create_var (cfg, mono_get_int_type (), OP_LOCAL);
			ins->flags |= MONO_INST_VOLATILE;
			cfg->arch.seq_point_info_var = ins;
		}

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
}

MonoInst *
mono_arch_emit_inst_for_method (MonoCompile *cfg, MonoMethod *cmethod,
                                MonoMethodSignature *fsig, MonoInst **args)
{
	return NULL;
}

static void
add_outarg_reg (MonoCompile *cfg, MonoCallInst *call, ArgStorage storage, int reg, MonoInst *arg)
{
	MonoInst *ins;

	switch (storage) {
	default:
		g_print ("unable process storage type %d\n", storage);
		NOT_IMPLEMENTED;
		break;
	case ArgInIReg:
		MONO_INST_NEW (cfg, ins, OP_MOVE);
		ins->dreg = mono_alloc_ireg_copy (cfg, arg->dreg);
		ins->sreg1 = arg->dreg;
		MONO_ADD_INS (cfg->cbb, ins);
		mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, reg, FALSE);
		break;
	case ArgInFReg:
		MONO_INST_NEW (cfg, ins, OP_FMOVE);
		ins->dreg = mono_alloc_freg (cfg);
		ins->sreg1 = arg->dreg;
		MONO_ADD_INS (cfg->cbb, ins);
		mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, reg, TRUE);
		break;
	case ArgInFRegR4:
		if (cfg->r4fp)
			MONO_INST_NEW (cfg, ins, OP_RMOVE);
		else
			MONO_INST_NEW (cfg, ins, OP_RISCV_SETFREG_R4);
		ins->dreg = mono_alloc_freg (cfg);
		ins->sreg1 = arg->dreg;
		MONO_ADD_INS (cfg->cbb, ins);
		mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, reg, TRUE);
		break;
	}
}

/*
 * take the arguments and generate the arch-specific
 * instructions to properly call the function in call.
 * This includes pushing, moving arguments to the right register
 * etc.
 */
static void
emit_sig_cookie (MonoCompile *cfg, MonoCallInst *call, CallInfo *cinfo)
{
	NOT_IMPLEMENTED;
	MonoMethodSignature *tmp_sig;
	MonoInst *sig_arg;

	if (MONO_IS_TAILCALL_OPCODE (call))
		NOT_IMPLEMENTED;

	/*
	 * mono_ArgIterator_Setup assumes the signature cookie is
	 * passed first and all the arguments which were before it are
	 * passed on the stack after the signature. So compensate by
	 * passing a different signature.
	 */
	tmp_sig = mono_metadata_signature_dup (call->signature);
	tmp_sig->param_count -= call->signature->sentinelpos;
	tmp_sig->sentinelpos = 0;
	memcpy (tmp_sig->params, call->signature->params + call->signature->sentinelpos,
	        tmp_sig->param_count * sizeof (MonoType *));

	MONO_INST_NEW (cfg, sig_arg, OP_ICONST);
	sig_arg->dreg = mono_alloc_ireg (cfg);
	sig_arg->inst_p0 = tmp_sig;
	MONO_ADD_INS (cfg->cbb, sig_arg);

	MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, RISCV_SP, cinfo->sig_cookie.offset, sig_arg->dreg);
}

/**
 * mono_arch_emit_call:
 * 	we process all Args of a function call
 *  (return, parameters)
 */
void
mono_arch_emit_call (MonoCompile *cfg, MonoCallInst *call)
{
	MonoInst *arg, *vtarg;
	MonoMethodSignature *sig;
	ArgInfo *ainfo;
	CallInfo *cinfo;

	sig = call->signature;
	int paramNum = sig->param_count + sig->hasthis;

	cinfo = get_call_info (cfg->mempool, sig);

	/* Emit the inst of return at mono_arch_emit_setret() */
	switch (cinfo->ret.storage) {
	case ArgVtypeInIReg:
		if (MONO_IS_TAILCALL_OPCODE (call))
			break;
		/*
		 * The vtype is returned in registers, save the return area address in a local, and save the vtype into
		 * the location pointed to by it after call in mono_riscv_emitmove_return_value ().
		 */
		if (!cfg->arch.vret_addr_loc) {
			cfg->arch.vret_addr_loc = mono_compile_create_var (cfg, mono_get_int_type (), OP_LOCAL);
			/* Prevent it from being register allocated or optimized away */
			cfg->arch.vret_addr_loc->flags |= MONO_INST_VOLATILE;
		}
		MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, cfg->arch.vret_addr_loc->dreg, call->vret_var->dreg);
		break;
	case ArgVtypeByRef:
		/* Pass the vtype return address in A0 */
		g_assert (cinfo->ret.reg == RISCV_A0);
		g_assert (!MONO_IS_TAILCALL_OPCODE (call) || call->vret_var == cfg->vret_addr);
		MONO_INST_NEW (cfg, vtarg, OP_MOVE);
		vtarg->sreg1 = call->vret_var->dreg;
		vtarg->dreg = mono_alloc_preg (cfg);
		MONO_ADD_INS (cfg->cbb, vtarg);
		mono_call_inst_add_outarg_reg (cfg, call, vtarg->dreg, cinfo->ret.reg, FALSE);
		break;
	case ArgVtypeByRefOnStack:
		NOT_IMPLEMENTED;
		break;
	case ArgVtypeOnStack:
		NOT_IMPLEMENTED;
		break;
	case ArgVtypeInMixed:
		NOT_IMPLEMENTED;
		break;
	default:
		break;
	}

	// if (cinfo->struct_ret)
	// 	// call->used_iregs |= 1 << cinfo->struct_ret;
	// 	NOT_IMPLEMENTED;

	if (COMPILE_LLVM (cfg)) {
		/* We shouldn't be called in the llvm case */
		cfg->disable_llvm = TRUE;
		return;
	}

	for (int i = 0; i < paramNum; i++) {
		ainfo = cinfo->args + i;
		MonoType *t;

		if (sig->hasthis && i == 0)
			t = mono_get_object_type ();
		else
			t = sig->params [i - sig->hasthis];
		t = mini_get_underlying_type (t);

		/* Emit the signature cookie just before the implicit arguments */
		if ((sig->call_convention == MONO_CALL_VARARG) && (i == sig->sentinelpos))
			emit_sig_cookie (cfg, call, cinfo);

		arg = call->args [i];
		switch (ainfo->storage) {
		case ArgInIReg:
		case ArgInFReg:
		case ArgInFRegR4:
			add_outarg_reg (cfg, call, ainfo->storage, ainfo->reg, arg);
			break;
		case ArgOnStack: {
			switch (ainfo->slot_size) {
			case 1:
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI1_MEMBASE_REG, RISCV_SP, ainfo->offset, arg->dreg);
				break;
			case 2:
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI2_MEMBASE_REG, RISCV_SP, ainfo->offset, arg->dreg);
				break;
			case 4:
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, RISCV_SP, ainfo->offset, arg->dreg);
				break;
			case 8:
#ifdef TARGET_RISCV32
				NOT_IMPLEMENTED;
				break;
#else // TARGET_RISCV64
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI8_MEMBASE_REG, RISCV_SP, ainfo->offset, arg->dreg);
				break;
			// RV64 Only, XLEN*2 == 16
			case 16:
				NOT_IMPLEMENTED;
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI8_MEMBASE_REG, RISCV_SP, ainfo->offset, arg->dreg);
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI8_MEMBASE_REG, RISCV_SP, ainfo->offset + 8, arg->dreg + 1);
				break;
#endif
			default:
				g_assert_not_reached ();
			}
			break;
		}
		case ArgVtypeInIReg:
		case ArgVtypeByRef: {
			MonoInst *ins;
			guint32 align;
			guint32 size;
			size = mono_class_value_size (arg->klass, &align);

			MONO_INST_NEW (cfg, ins, OP_OUTARG_VT);
			ins->sreg1 = arg->dreg;
			ins->klass = arg->klass;
			ins->backend.size = size;
			ins->inst_p0 = call;
			ins->inst_p1 = mono_mempool_alloc (cfg->mempool, sizeof (ArgInfo));
			memcpy (ins->inst_p1, ainfo, sizeof (ArgInfo));
			MONO_ADD_INS (cfg->cbb, ins);

			break;
		}
		default:
			g_print ("can't process Storage type %d\n", ainfo->storage);
			NOT_IMPLEMENTED;
		}
	}

	/* Handle the case where there are no implicit arguments */
	if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) && (paramNum == sig->sentinelpos))
		emit_sig_cookie (cfg, call, cinfo);

	call->call_info = cinfo;
	call->stack_usage = cinfo->stack_usage;
}

void
mono_arch_emit_outarg_vt (MonoCompile *cfg, MonoInst *ins, MonoInst *src)
{
	MonoCallInst *call = (MonoCallInst *)ins->inst_p0;
	ArgInfo *ainfo = (ArgInfo *)ins->inst_p1;
	MonoInst *load;
	int op_load = 0;

#ifdef TARGET_RISCV64
	op_load = OP_LOADI8_MEMBASE;
#else // TARGET_RISCV32
	op_load = OP_LOADI4_MEMBASE;
#endif

	if (ins->backend.size == 0)
		return;

	switch (ainfo->storage) {
	case ArgVtypeInIReg:
		MONO_INST_NEW (cfg, load, op_load);
		load->dreg = mono_alloc_ireg (cfg);
		load->inst_basereg = src->dreg;
		load->inst_offset = 0;
		MONO_ADD_INS (cfg->cbb, load);
		add_outarg_reg (cfg, call, ArgInIReg, ainfo->reg, load);

		if (ainfo->size > sizeof (host_mgreg_t)) {
			MONO_INST_NEW (cfg, load, op_load);
			load->dreg = mono_alloc_ireg (cfg);
			load->inst_basereg = src->dreg;
			load->inst_offset = sizeof (target_mgreg_t);
			MONO_ADD_INS (cfg->cbb, load);
			add_outarg_reg (cfg, call, ArgInIReg, ainfo->reg + 1, load);
		}
		break;
	case ArgVtypeOnStack:
		g_assert (ainfo->offset >= 0);
		for (int i = 0; i < ainfo->slot_size; i += sizeof (target_mgreg_t)) {
			MONO_INST_NEW (cfg, load, op_load);
			load->dreg = mono_alloc_ireg (cfg);
			load->inst_basereg = src->dreg;
			load->inst_offset = i;
			MONO_ADD_INS (cfg->cbb, load);
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, op_load, RISCV_FP, -ainfo->offset + i, load->dreg);
		}
		break;
	case ArgVtypeByRef: {
		MonoInst *vtaddr, *arg;
		/* Pass the vtype address in a reg/on the stack */
		// if (ainfo->gsharedvt) {
		// 	load = src;
		// } else {
		/* Make a copy of the argument */
		vtaddr = mono_compile_create_var (cfg, m_class_get_byval_arg (ins->klass), OP_LOCAL);

		MONO_INST_NEW (cfg, load, OP_LDADDR);
		load->inst_p0 = vtaddr;
		vtaddr->flags |= MONO_INST_INDIRECT;
		load->type = STACK_MP;
		load->klass = vtaddr->klass;
		load->dreg = mono_alloc_ireg (cfg);
		MONO_ADD_INS (cfg->cbb, load);
		mini_emit_memcpy (cfg, load->dreg, 0, src->dreg, 0, ainfo->size, 8);
		// }

		if (ainfo->storage == ArgVtypeByRef) {
			MONO_INST_NEW (cfg, arg, OP_MOVE);
			arg->dreg = mono_alloc_preg (cfg);
			arg->sreg1 = load->dreg;
			MONO_ADD_INS (cfg->cbb, arg);
			add_outarg_reg (cfg, call, ArgInIReg, ainfo->reg, arg);
		} else {
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, RISCV_SP, ainfo->offset, load->dreg);
		}
		break;
	}
	default:
		NOT_IMPLEMENTED;
		break;
	}
}

void
mono_arch_emit_setret (MonoCompile *cfg, MonoMethod *method, MonoInst *val)
{
	MonoMethodSignature *sig;
	CallInfo *cinfo;

	sig = mono_method_signature_internal (cfg->method);
	if (!cfg->arch.cinfo)
		cfg->arch.cinfo = get_call_info (cfg->mempool, sig);
	cinfo = cfg->arch.cinfo;

	switch (cinfo->ret.storage) {
	case ArgNone:
		break;
	case ArgInIReg:
		MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, cfg->ret->dreg, val->dreg);
		break;
	case ArgInFReg:
		MONO_EMIT_NEW_UNALU (cfg, OP_FMOVE, cfg->ret->dreg, val->dreg);
		break;
	default:
		g_print ("can't process Storage type %d\n", cinfo->ret.storage);
		NOT_IMPLEMENTED;
	}
}

void
mono_arch_decompose_opts (MonoCompile *cfg, MonoInst *ins)
{
	NOT_IMPLEMENTED;
}

void
mono_arch_decompose_long_opts (MonoCompile *cfg, MonoInst *long_ins)
{
#ifdef TARGET_RISCV32
	NOT_IMPLEMENTED;
#endif
}

/*
 * Set var information according to the calling convention. RISCV version.
 */
void
mono_arch_allocate_vars (MonoCompile *cfg)
{
	MonoMethodSignature *sig;
	MonoInst *ins;
	CallInfo *cinfo;
	ArgInfo *ainfo;
	int offset, size, align;
	guint32 locals_stack_size, locals_stack_align;
	gint32 *local_stack;

	/*
	 * Allocate arguments and locals to either register (OP_REGVAR) or to a stack slot (OP_REGOFFSET).
	 * Compute cfg->stack_offset and update cfg->used_int_regs.
	 */
	sig = mono_method_signature_internal (cfg->method);

	if (!cfg->arch.cinfo)
		cfg->arch.cinfo = get_call_info (cfg->mempool, sig);
	cinfo = cfg->arch.cinfo;

	offset = 0;
	// save RA & FP reg to stack
	cfg->frame_reg = RISCV_FP;
	offset += sizeof (host_mgreg_t) * 2;

	if (cfg->method->save_lmf) {
		/* The LMF var is allocated normally */
	} else {
		/* Callee saved regs */
		// saved_gregs_offset store the addr of firs reg
		for (guint i = 0; i < 32; ++i)
			if ((MONO_ARCH_CALLEE_SAVED_REGS & (1 << i)) && (cfg->used_int_regs & (1 << i))) {
				offset += sizeof (host_mgreg_t);
			}
		cfg->arch.saved_gregs_offset = offset;
	}

	/* Return value */
	if (sig->ret->type != MONO_TYPE_VOID) {
		switch (cinfo->ret.storage) {
		case ArgNone:
			break;
		case ArgInIReg:
		case ArgInFReg:
			cfg->ret->opcode = OP_REGVAR;
			cfg->ret->inst_c0 = cinfo->ret.reg;
			cfg->ret->dreg = cinfo->ret.reg;
			break;
		case ArgVtypeInIReg:
			/* Allocate a local to hold the result, the epilog will copy it to the correct place */
			cfg->ret->opcode = OP_REGOFFSET;
			cfg->ret->inst_basereg = cfg->frame_reg;
			if (cinfo->ret.is_regpair)
				offset += sizeof (host_mgreg_t);
			offset += sizeof (host_mgreg_t);
			cfg->ret->inst_offset = -offset;
			break;
		case ArgVtypeByRef:
			/**
			 * Caller pass the address of return value by A0 as an implicit param.
			 * It will be saved in the prolog
			 */
			cfg->vret_addr->opcode = OP_REGOFFSET;
			cfg->vret_addr->inst_basereg = cfg->frame_reg;
			offset += sizeof (host_mgreg_t);
			cfg->vret_addr->inst_offset = -offset;
			if (G_UNLIKELY (cfg->verbose_level > 1)) {
				printf ("vret_addr =");
				mono_print_ins (cfg->vret_addr);
			}
			break;
		default:
			g_print ("Can't handle storage type %d\n", cinfo->ret.storage);
			NOT_IMPLEMENTED;
			break;
		}
	}

	/* Arguments */
	for (guint i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ainfo = cinfo->args + i;
		ins = cfg->args [i];

		if (ins->opcode == OP_REGVAR)
			continue;
		ins->opcode = OP_REGOFFSET;
		ins->inst_basereg = cfg->frame_reg;

		switch (ainfo->storage) {
		case ArgInIReg:
		case ArgInFReg:
		case ArgInFRegR4:
			offset += sizeof (host_mgreg_t);
			ins->inst_offset = -offset;
			break;
		case ArgOnStack:
		case ArgVtypeOnStack:
			/* These are in the parent frame */
			g_assert (ainfo->offset >= 0);
			ins->inst_basereg = RISCV_FP;
			ins->inst_offset = ainfo->offset;
			break;
		case ArgVtypeInIReg:
			ins->opcode = OP_REGOFFSET;
			ins->inst_basereg = cfg->frame_reg;
			/* These arguments are saved to the stack in the prolog */
			if (ainfo->is_regpair)
				offset += sizeof (host_mgreg_t);
			offset += sizeof (host_mgreg_t);
			ins->inst_offset = -offset;
			break;
		default:
			NOT_IMPLEMENTED;
			break;
		}
		if (cfg->verbose_level >= 2)
			g_print ("arg %d allocated to %ld(%s).\n", i, ins->inst_offset, mono_arch_regname (ins->inst_basereg));
	}

	/* OP_SEQ_POINT depends on these */
	// FIXME: Allocate these to registers
	ins = cfg->arch.seq_point_info_var;
	if (ins) {
		size = sizeof (host_mgreg_t);
		align = sizeof (host_mgreg_t);
		offset += align - 1;
		offset &= ~(align - 1);
		ins->opcode = OP_REGOFFSET;
		ins->inst_basereg = cfg->frame_reg;
		offset += size;
		ins->inst_offset = -offset;
		g_print ("alloc seq_point_info_var to %ld(%s).\n", ins->inst_offset, mono_arch_regname (ins->inst_basereg));
	}
	ins = cfg->arch.ss_tramp_var;
	if (ins) {
		size = sizeof (host_mgreg_t);
		align = sizeof (host_mgreg_t);
		offset += align - 1;
		offset &= ~(align - 1);
		ins->opcode = OP_REGOFFSET;
		ins->inst_basereg = cfg->frame_reg;
		offset += size;
		ins->inst_offset = -offset;
		g_print ("alloc ss_tramp_var to %ld(%s).\n", ins->inst_offset, mono_arch_regname (ins->inst_basereg));
	}
	ins = cfg->arch.bp_tramp_var;
	if (ins) {
		size = sizeof (host_mgreg_t);
		align = sizeof (host_mgreg_t);
		offset += align - 1;
		offset &= ~(align - 1);
		ins->opcode = OP_REGOFFSET;
		ins->inst_basereg = cfg->frame_reg;
		offset += size;
		ins->inst_offset = -offset;
		g_print ("alloc bp_tramp_var to %ld(%s).\n", ins->inst_offset, mono_arch_regname (ins->inst_basereg));
	}

	/* Allocate locals */
	local_stack = mono_allocate_stack_slots (cfg, FALSE, &locals_stack_size, &locals_stack_align);
	if (locals_stack_align)
		offset = ALIGN_TO (offset, locals_stack_align);

	offset += locals_stack_size;
	for (guint i = cfg->locals_start; i < cfg->num_varinfo; i++) {
		if (local_stack [i] != -1) {
			ins = cfg->varinfo [i];
			ins->opcode = OP_REGOFFSET;
			ins->inst_basereg = cfg->frame_reg;
			ins->inst_offset = -offset + local_stack [i];
		}
	}
	offset = ALIGN_TO (offset, MONO_ARCH_FRAME_ALIGNMENT);

	cfg->stack_offset = offset;
}

void
mono_arch_lowering_pass (MonoCompile *cfg, MonoBasicBlock *bb)
{
	NOT_IMPLEMENTED;
}

void
mono_arch_peephole_pass_1 (MonoCompile *cfg, MonoBasicBlock *bb)
{
}

void
mono_arch_peephole_pass_2 (MonoCompile *cfg, MonoBasicBlock *bb)
{
}

// Uses at most 8 bytes on RV32I and 16 bytes on RV64I.
guint8 *
mono_riscv_emit_imm (guint8 *code, int rd, gsize imm)
{
#ifdef TARGET_RISCV64
	if (RISCV_VALID_I_IMM (imm)) {
		riscv_addi (code, rd, RISCV_ZERO, imm);
		return code;
	}

	/**
	 * use LUI & ADDIW load 32 bit Imm
	 * LUI: High 20 bit of imm
	 * ADDIW: Low 12 bit of imm
	 */
	if (RISCV_VALID_IMM (imm)) {
		gint32 Hi = RISCV_BITS (imm, 12, 20);
		gint32 Lo = RISCV_BITS (imm, 0, 12);

		// Lo is in signed num
		// if Lo > 0x800
		// convert into ((Hi + 1) << 20) -  (0x1000 - Lo)
		if (Lo >= 0x800) {
			Hi += 1;
			Lo = Lo - 0x1000;
		}

		// if Hi is 0 or overflow, skip
		if (Hi < 0xfffff) {
			riscv_lui (code, rd, Hi);
		}
		riscv_addiw (code, rd, rd, Lo);
		return code;
	}

	/*
	 * This is not pretty, but RV64I doesn't make it easy to load constants.
	 * Need to figure out something better.
	 */
	riscv_jal (code, rd, sizeof (guint64) + 4);
	*(guint64 *) code = imm;
	code += sizeof (guint64);
	riscv_ld (code, rd, rd, 0);
#else
	if (RISCV_VALID_I_IMM (imm)) {
		riscv_addi (code, rd, RISCV_ZERO, imm);
		return code;
	}

	riscv_lui (code, rd, RISCV_BITS (imm, 12, 20));

	if (!RISCV_VALID_U_IMM (imm))
		riscv_ori (code, rd, rd, RISCV_BITS (imm, 0, 12));
#endif

	return code;
}

guint8 *
mono_riscv_emit_float_imm (guint8 *code, int rd, gsize f_imm, gboolean isSingle)
{
	if (f_imm == 0) {
		if (mono_arch_is_soft_float ())
			riscv_addi (code, rd, RISCV_ZERO, 0);
		else if (isSingle)
			riscv_fmv_w_x (code, rd, RISCV_ZERO);
		else
			riscv_fmv_d_x (code, rd, RISCV_ZERO);
		return code;
	}

	riscv_jal (code, RISCV_T0, sizeof (guint64) + 4);
	*(guint64 *)code = f_imm;
	code += sizeof (guint64);

	if (mono_arch_is_soft_float ())
#ifdef TARGET_RISCV64
		riscv_ld (code, rd, RISCV_T0, 0);
#else
		riscv_lw (code, rd, RISCV_T0, 0);
#endif
	else if (isSingle)
		riscv_flw (code, rd, RISCV_T0, 0);
	else
		riscv_fld (code, rd, RISCV_T0, 0);

	return code;
}

guint8 *
mono_riscv_emit_nop (guint8 *code)
{
	// if(riscv_stdext_c){
	// }
	riscv_addi (code, RISCV_ZERO, RISCV_ZERO, 0);
	return code;
}

// Uses at most 16 bytes on RV32I and 24 bytes on RV64I.
// length == 0 means the data size follows the XLEN
guint8 *
mono_riscv_emit_load (guint8 *code, int rd, int rs1, gint32 imm, int length)
{
	if (!RISCV_VALID_I_IMM (imm)) {
		code = mono_riscv_emit_imm (code, RISCV_T0, imm);
		riscv_add (code, RISCV_T0, rs1, RISCV_T0);
		rs1 = RISCV_T0;
		imm = 0;
	}

	switch (length) {
	case 0:
#ifdef TARGET_RISCV64
		riscv_ld (code, rd, rs1, imm);
#else
		riscv_lw (code, rd, rs1, imm);
#endif
		break;
	case 1:
		riscv_lb (code, rd, rs1, imm);
		break;
	case 2:
		riscv_lh (code, rd, rs1, imm);
		break;
	case 4:
		riscv_lw (code, rd, rs1, imm);
		break;
#ifdef TARGET_RISCV64
	case 8:
		riscv_ld (code, rd, rs1, imm);
		break;
#endif
	default:
		g_assert_not_reached ();
		break;
	}
	return code;
}

// Uses at most 16 bytes on RV32D and 24 bytes on RV64D.
guint8 *
mono_riscv_emit_fload (guint8 *code, int rd, int rs1, gint32 imm, gboolean isSingle)
{
	g_assert (riscv_stdext_d || (isSingle && riscv_stdext_f));
	if (!RISCV_VALID_I_IMM (imm)) {
		code = mono_riscv_emit_imm (code, RISCV_T0, imm);
		riscv_add (code, RISCV_T0, rs1, RISCV_T0);
		rs1 = RISCV_T0;
		imm = 0;
	}

	if (isSingle)
		riscv_flw (code, rd, rs1, imm);
	else
		riscv_fld (code, rd, rs1, imm);

	return code;
}

// May clobber t0. Uses at most 16 bytes on RV32I and 24 bytes on RV64I.
// length == 0 means the data size follows the XLEN
guint8 *
mono_riscv_emit_store (guint8 *code, int rs2, int rs1, gint32 imm, int length)
{
	if (!RISCV_VALID_S_IMM (imm)) {
		code = mono_riscv_emit_imm (code, RISCV_T0, imm);
		riscv_add (code, RISCV_T0, rs1, RISCV_T0);
		rs1 = RISCV_T0;
		imm = 0;
	}

	switch (length) {
	case 0:
#ifdef TARGET_RISCV64
		riscv_sd (code, rs2, rs1, imm);
#else
		riscv_sd (code, rs2, rs1, imm);
#endif
		break;
	case 1:
		riscv_sb (code, rs2, rs1, imm);
		break;
	case 2:
		riscv_sh (code, rs2, rs1, imm);
		break;
	case 4:
		riscv_sw (code, rs2, rs1, imm);
		break;
#ifdef TARGET_RISCV64
	case 8:
		riscv_sd (code, rs2, rs1, imm);
		break;
#endif
	default:
		g_assert_not_reached ();
		break;
	}
	return code;
}

// May clobber t0. Uses at most 16 bytes on RV32I and 24 bytes on RV64I.
guint8 *
mono_riscv_emit_fstore (guint8 *code, int rs2, int rs1, gint32 imm, gboolean isSingle)
{
	g_assert (riscv_stdext_d || (isSingle && riscv_stdext_f));
	if (!RISCV_VALID_I_IMM (imm)) {
		code = mono_riscv_emit_imm (code, RISCV_T0, imm);
		riscv_add (code, RISCV_T0, rs1, RISCV_T0);
		rs1 = RISCV_T0;
		imm = 0;
	}

	if (isSingle)
		riscv_fsw (code, rs2, rs1, imm);
	else
		riscv_fsd (code, rs2, rs1, imm);

	return code;

	return code;
}

/*
 * mono_riscv_emit_load_regarray:
 *
 *   Emit code to load the registers in REGS from the appropriate elements of
 * a register array at BASEREG+OFFSET.
 */
guint8 *
mono_riscv_emit_load_regarray (guint8 *code, guint64 regs, int basereg, int offset, gboolean isFloat)
{
	g_assert (basereg != RISCV_SP);

	if (!RISCV_VALID_S_IMM (offset)) {
		code = mono_riscv_emit_imm (code, RISCV_T6, offset);
		riscv_add (code, RISCV_T6, basereg, RISCV_T6);
		basereg = RISCV_T6;
		offset = 0;
	}

	for (int i = 0; i < 32; ++i) {
		if (regs & (1 << i)) {
			if (!isFloat && i == RISCV_SP)
				g_assert_not_reached ();
			if (isFloat)
				code = mono_riscv_emit_fload (code, i, basereg, offset + (i * sizeof (host_mgreg_t)), FALSE);
			else
				code = mono_riscv_emit_load (code, i, basereg, offset + (i * sizeof (host_mgreg_t)), 0);
		}
	}

	return code;
}

/*
 * mono_riscv_emit_store_regarray:
 *
 *   Emit code to store the registers in REGS from the appropriate elements of
 * a register array at BASEREG+OFFSET.
 */
guint8 *
mono_riscv_emit_store_regarray (guint8 *code, guint64 regs, int basereg, int offset, gboolean isFloat)
{
	g_assert (basereg != RISCV_SP);

	if (!RISCV_VALID_S_IMM (offset)) {
		code = mono_riscv_emit_imm (code, RISCV_T6, offset);
		riscv_add (code, RISCV_T6, basereg, RISCV_T6);
		basereg = RISCV_T6;
		offset = 0;
	}

	for (int i = 0; i < 32; ++i) {
		if (regs & (1 << i)) {
			if (!isFloat && i == RISCV_SP)
				g_assert_not_reached ();
			if (isFloat)
				code = mono_riscv_emit_fstore (code, i, basereg, offset + (i * sizeof (host_mgreg_t)), FALSE);
			else
				code = mono_riscv_emit_store (code, i, basereg, offset + (i * sizeof (host_mgreg_t)), 0);
		}
	}

	return code;
}

/*
 * mono_riscv_emit_load_stack:
 *
 *   Emit code to load the registers in REGS from stack or consecutive memory locations starting
 * at BASEREG+OFFSET.
 */
guint8 *
mono_riscv_emit_load_stack (guint8 *code, guint64 regs, int basereg, int offset, gboolean isFloat)
{
	int pos = 0;
	g_assert (basereg != RISCV_SP);

	if (!RISCV_VALID_S_IMM (offset)) {
		code = mono_riscv_emit_imm (code, RISCV_T6, offset);
		riscv_add (code, RISCV_T6, basereg, RISCV_T6);
		basereg = RISCV_T6;
		offset = 0;
	}

	for (int i = 0; i < 32; ++i) {
		if (regs & (1 << i)) {
			if (isFloat)
				code = mono_riscv_emit_fload (code, i, basereg, (offset + (pos * sizeof (host_mgreg_t))), FALSE);
			else
				code = mono_riscv_emit_load (code, i, basereg, (offset + (pos * sizeof (host_mgreg_t))), 0);
			pos++;
		}
	}

	return code;
}

/*
 * mono_riscv_emit_store_stack:
 *
 *   Emit code to store the registers in REGS into consecutive memory locations starting
 * at BASEREG+OFFSET.
 */
guint8 *
mono_riscv_emit_store_stack (guint8 *code, guint64 regs, int basereg, int offset, gboolean isFloat)
{
	int pos = 0;
	g_assert (basereg != RISCV_SP);

	for (int i = 0; i < 32; ++i) {
		if (regs & (1 << i)) {
			if (isFloat)
				code = mono_riscv_emit_fstore (code, i, basereg, (offset + (pos * sizeof (host_mgreg_t))), FALSE);
			else
				code = mono_riscv_emit_store (code, i, basereg, (offset + (pos * sizeof (host_mgreg_t))), 0);
			pos++;
		}
	}
	return code;
}

/* Same as mono_riscv_emitstore_regarray, but emit unwind info */
/* CFA_OFFSET is the offset between the CFA and basereg */
static __attribute__ ((__warn_unused_result__)) guint8 *
emit_store_regarray_cfa (
    MonoCompile *cfg, guint8 *code, guint64 regs, int basereg, int offset, int cfa_offset, guint64 no_cfa_regset)
{
	guint32 cfa_regset = regs & ~no_cfa_regset;

	for (int i = 0; i < 32; ++i) {
		if (regs & (1 << i)) {
			code = mono_riscv_emit_store (code, i, basereg, offset + (i * sizeof (host_mgreg_t)), 0);

			if (cfa_regset & (1 << i)) {
				g_assert (cfa_offset >= 0);
				mono_emit_unwind_op_offset (cfg, code, i, (-cfa_offset) + offset + (i * sizeof (host_mgreg_t)));
			}
		}
	}
	return code;
}

/*
 * emit_setup_lmf:
 *
 *   Emit code to initialize an LMF structure at LMF_OFFSET.
 * Clobbers T6.
 */
static guint8 *
emit_setup_lmf (MonoCompile *cfg, guint8 *code, gint32 lmf_offset, int cfa_offset)
{
	/*
	 * The LMF should contain all the state required to be able to reconstruct the machine state
	 * at the current point of execution. Since the LMF is only read during EH, only callee
	 * saved etc. registers need to be saved.
	 * FIXME: Save callee saved fp regs, JITted code doesn't use them, but native code does, and they
	 * need to be restored during EH.
	 */
	g_assert (lmf_offset <= 0);
	/* pc */
	code = mono_riscv_emit_imm (code, RISCV_T6, (gsize)code);
	code = mono_riscv_emit_store (code, RISCV_T6, RISCV_FP, lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, pc), 0);
	/* callee saved gregs + sp */
	code = emit_store_regarray_cfa (cfg, code, MONO_ARCH_LMF_REGS, RISCV_FP,
	                                lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, gregs), cfa_offset, (1 << RISCV_SP));

	return code;
}

static guint8 *
emit_move_args (MonoCompile *cfg, guint8 *code)
{
	MonoInst *ins;
	CallInfo *cinfo;
	ArgInfo *ainfo;
	MonoMethodSignature *sig = mono_method_signature_internal (cfg->method);

	cinfo = cfg->arch.cinfo;
	g_assert (cinfo);

	for (int i = 0; i < cinfo->nargs; ++i) {
		ainfo = cinfo->args + i;
		ins = cfg->args [i];

		if (ins->opcode == OP_REGVAR) {
			switch (ainfo->storage) {
			case ArgInIReg:
				riscv_addi (code, ins->dreg, ainfo->reg, 0);
				if (i == 0 && sig->hasthis) {
					mono_add_var_location (cfg, ins, TRUE, ainfo->reg, 0, 0, code - cfg->native_code);
					mono_add_var_location (cfg, ins, TRUE, ins->dreg, 0, code - cfg->native_code, 0);
				}
				break;

			default:
				NOT_IMPLEMENTED;
			}
		} else {
			if (ainfo->storage != ArgVtypeByRef && ainfo->storage != ArgVtypeByRefOnStack)
				g_assert (ins->opcode == OP_REGOFFSET);

			switch (ainfo->storage) {
			case ArgInIReg:
				g_assert (ainfo->is_regpair == FALSE);
				code = mono_riscv_emit_store (code, ainfo->reg, ins->inst_basereg, ins->inst_offset, 0);
				if (i == 0 && sig->hasthis) {
					mono_add_var_location (cfg, ins, TRUE, ainfo->reg, 0, 0, code - cfg->native_code);
					mono_add_var_location (cfg, ins, FALSE, ins->inst_basereg, ins->inst_offset,
					                       code - cfg->native_code, 0);
				}
				break;
			case ArgInFReg:
			case ArgInFRegR4:
				code = mono_riscv_emit_fstore (code, ainfo->reg, ins->inst_basereg, ins->inst_offset,
				                               ainfo->storage == ArgInFRegR4);
				break;
			case ArgVtypeInIReg:
				if (ainfo->is_regpair)
					code = mono_riscv_emit_store (code, ainfo->reg + 1, ins->inst_basereg,
					                              ins->inst_offset + sizeof (host_mgreg_t), 0);
				code = mono_riscv_emit_store (code, ainfo->reg, ins->inst_basereg, ins->inst_offset, 0);
				break;
			case ArgOnStack:
				break;
			default:
				g_print ("can't process Storage type %d\n", ainfo->storage);
				NOT_IMPLEMENTED;
			}
		}
	}

	return code;
}

static guint8 *
emit_move_return_value (MonoCompile *cfg, guint8 *code, MonoInst *ins)
{
	CallInfo *cinfo;
	MonoCallInst *call;

	call = (MonoCallInst *)ins;
	cinfo = call->call_info;
	g_assert (cinfo);
	switch (cinfo->ret.storage) {
	case ArgNone:
		break;
	case ArgInIReg:
		if (call->inst.dreg != cinfo->ret.reg) {
			riscv_addi (code, call->inst.dreg, cinfo->ret.reg, 0);
		}
		break;
	case ArgInFReg:
		g_assert (riscv_stdext_d || riscv_stdext_f);
		if (call->inst.dreg != cinfo->ret.reg) {
			if (riscv_stdext_d)
				riscv_fsgnj_d (code, call->inst.dreg, cinfo->ret.reg, cinfo->ret.reg);
			else
				riscv_fsgnj_s (code, ins->dreg, cinfo->ret.reg, cinfo->ret.reg);
		}
		break;
	case ArgVtypeInIReg: {
		MonoInst *loc = cfg->arch.vret_addr_loc;

		/* Load the destination address */
		g_assert (loc && loc->opcode == OP_REGOFFSET);
		code = mono_riscv_emit_load (code, RISCV_T0, loc->inst_basereg, loc->inst_offset, 0);
		code = mono_riscv_emit_store (code, cinfo->ret.reg, RISCV_T0, 0, 0);
		if (cinfo->ret.is_regpair) {
			code = mono_riscv_emit_store (code, cinfo->ret.reg + 1, RISCV_T0, sizeof (host_mgreg_t), 0);
		}
		break;
	}
	case ArgVtypeByRef:
		break;
	default:
		g_print ("Unable process returned storage %d(0x%x)\n", cinfo->ret.storage, cinfo->ret.storage);
		NOT_IMPLEMENTED;
		break;
	}
	return code;
}

guint8 *
mono_arch_emit_prolog (MonoCompile *cfg)
{
	NOT_IMPLEMENTED;
}

void
mono_arch_emit_epilog (MonoCompile *cfg)
{
	NOT_IMPLEMENTED;
}

void
mono_arch_output_basic_block (MonoCompile *cfg, MonoBasicBlock *bb)
{
	NOT_IMPLEMENTED;
}

void
mono_arch_emit_exceptions (MonoCompile *cfg)
{
	NOT_IMPLEMENTED;
}

guint32
mono_arch_get_patch_offset (guint8 *code)
{
	NOT_IMPLEMENTED;
	return 0;
}

GSList *
mono_arch_get_trampolines (gboolean aot)
{
	NOT_IMPLEMENTED;
	return NULL;
}

#endif

#if defined(MONO_ARCH_SOFT_DEBUG_SUPPORTED)
void
mono_arch_set_breakpoint (MonoJitInfo *ji, guint8 *ip)
{
	NOT_IMPLEMENTED;
}

void
mono_arch_clear_breakpoint (MonoJitInfo *ji, guint8 *ip)
{
	NOT_IMPLEMENTED;
}

void
mono_arch_start_single_stepping (void)
{
	ss_trampoline = mini_get_single_step_trampoline ();
}

void
mono_arch_stop_single_stepping (void)
{
	ss_trampoline = NULL;
}

gboolean
mono_arch_is_single_step_event (void *info, void *sigctx)
{
	// NOT_IMPLEMENTED;
	/* No reference Information, Don't know how to implement */
	return FALSE;
}

gboolean
mono_arch_is_breakpoint_event (void *info, void *sigctx)
{
	// NOT_IMPLEMENTED;
	/* No reference Information, Don't know how to implement */
	return FALSE;
}

void
mono_arch_skip_breakpoint (MonoContext *ctx, MonoJitInfo *ji)
{
	NOT_IMPLEMENTED;
}

void
mono_arch_skip_single_step (MonoContext *ctx)
{
	NOT_IMPLEMENTED;
}

SeqPointInfo*
mono_arch_get_seq_point_info (guint8 *code)
{
	SeqPointInfo *info;
	MonoJitInfo *ji;
	MonoJitMemoryManager *jit_mm;

	jit_mm = get_default_jit_mm ();

	// FIXME: Add a free function

	jit_mm_lock (jit_mm);
	info = (SeqPointInfo *)g_hash_table_lookup (jit_mm->arch_seq_points, code);
	jit_mm_unlock (jit_mm);

	if (!info) {
		ji = mini_jit_info_table_find (code);
		g_assert (ji);

		info = g_malloc0 (sizeof (SeqPointInfo) + (ji->code_size / 4) * sizeof (guint8 *));

		info->ss_tramp_addr = &ss_trampoline;

		jit_mm_lock (jit_mm);
		g_hash_table_insert (jit_mm->arch_seq_points, code, info);
		jit_mm_unlock (jit_mm);
	}

	return info;
}
#endif /* MONO_ARCH_SOFT_DEBUG_SUPPORTED */

gpointer
mono_arch_load_function (MonoJitICallId jit_icall_id)
{
	return NULL;
}
