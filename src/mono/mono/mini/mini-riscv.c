/*
 * Licensed to the .NET Foundation under one or more agreements.
 * The .NET Foundation licenses this file to you under the MIT license.
 */

#include <mono/utils/mono-hwcap.h>

#include "mini-runtime.h"
#include "ir-emit.h"

#include <mono/metadata/tokentype.h>
#include <mono/metadata/marshal-shared.h>

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

// Reference to mini-arm64, should be the namber of Parameter Regs
#define MAX_ARCH_DELEGATE_PARAMS 8

static gpointer
get_delegate_invoke_impl (gboolean has_target, gboolean param_count, guint32 *code_size)
{
	guint8 *code, *start;

	MINI_BEGIN_CODEGEN ();

	if (has_target) {
		start = code = mono_global_codeman_reserve (4 * 3);
		code = mono_riscv_emit_load (code, RISCV_T1, RISCV_A0, MONO_STRUCT_OFFSET (MonoDelegate, method_ptr), 0);
		code = mono_riscv_emit_load (code, RISCV_A0, RISCV_A0, MONO_STRUCT_OFFSET (MonoDelegate, target), 0);
		riscv_jalr (code, RISCV_ZERO, RISCV_T1, 0);

		g_assert ((code - start) <= 4 * 3);
	} else {
		int size, i;

		size = 8 + param_count * 4;
		start = code = mono_global_codeman_reserve (size);

		code = mono_riscv_emit_load (code, RISCV_T1, RISCV_A0, MONO_STRUCT_OFFSET (MonoDelegate, method_ptr), 0);
		/* slide down the arguments */
		for (i = 0; i < param_count; ++i)
			riscv_addi (code, RISCV_A0 + i, RISCV_A0 + i + 1, 0);

		riscv_jalr (code, RISCV_ZERO, RISCV_T1, 0);
		g_assert ((code - start) <= size);
	}

	MINI_END_CODEGEN (start, code - start, MONO_PROFILER_CODE_BUFFER_DELEGATE_INVOKE, NULL);

	if (code_size)
		*code_size = code - start;

	return MINI_ADDR_TO_FTNPTR (start);
}

#define MAX_VIRTUAL_DELEGATE_OFFSET 32

static gpointer
get_delegate_virtual_invoke_impl (MonoTrampInfo **info, gboolean load_imt_reg, int offset)
{
	guint8 *code, *start;
	const int size = 20;
	char *tramp_name;
	GSList *unwind_ops;

	if (offset / (int)sizeof (target_mgreg_t) > MAX_VIRTUAL_DELEGATE_OFFSET)
		return NULL;

	MINI_BEGIN_CODEGEN ();

	start = code = mono_global_codeman_reserve (size);

	unwind_ops = mono_arch_get_cie_program ();

	/* Replace the this argument with the target */
	/* this argument will be RISCV_A0 forever */
	code = mono_riscv_emit_load (code, RISCV_A0, RISCV_A0, MONO_STRUCT_OFFSET (MonoDelegate, target), 0);

	if (load_imt_reg)
		// FIXME:
		g_assert_not_reached ();

	/* Load this->vtable [offset] */
	code = mono_riscv_emit_load (code, RISCV_T1, RISCV_A0, MONO_STRUCT_OFFSET (MonoObject, vtable), 0);
	code = mono_riscv_emit_load (code, RISCV_T1, RISCV_T1, offset, 0);

	riscv_jalr (code, RISCV_ZERO, RISCV_T1, 0);

	g_assert ((code - start) <= size);

	MINI_END_CODEGEN (start, code - start, MONO_PROFILER_CODE_BUFFER_DELEGATE_INVOKE, NULL);

	tramp_name = mono_get_delegate_virtual_invoke_impl_name (load_imt_reg, offset);
	*info = mono_tramp_info_create (tramp_name, start, GPTRDIFF_TO_UINT32 (code - start), NULL, unwind_ops);
	g_free (tramp_name);

	return start;
}

/*
 * mono_arch_get_delegate_invoke_impls:
 *
 *   Return a list of MonoAotTrampInfo structures for the delegate invoke impl
 * trampolines.
 */
GSList *
mono_arch_get_delegate_invoke_impls (void)
{
	GSList *res = NULL;
	guint8 *code;
	guint32 code_len;
	char *tramp_name;

	code = (guint8 *)get_delegate_invoke_impl (TRUE, 0, &code_len);
	res = g_slist_prepend (res, mono_tramp_info_create ("delegate_invoke_impl_has_target", code, code_len, NULL, NULL));

	for (int i = 0; i <= MAX_ARCH_DELEGATE_PARAMS; ++i) {
		code = (guint8 *)get_delegate_invoke_impl (FALSE, i, &code_len);
		tramp_name = g_strdup_printf ("delegate_invoke_impl_target_%d", i);
		res = g_slist_prepend (res, mono_tramp_info_create (tramp_name, code, code_len, NULL, NULL));
		g_free (tramp_name);
	}

	return res;
}

gpointer
mono_arch_get_delegate_invoke_impl (MonoMethodSignature *sig, gboolean has_target)
{
	guint8 *code, *start;
	/*
	 * vtypes are returned in registers, so
	 * they can be supported by delegate invokes.
	 */

	if (has_target) {
		static guint8 *cached = NULL;

		if (cached)
			return cached;

		if (mono_ee_features.use_aot_trampolines)
			NOT_IMPLEMENTED;
		else
			start = (guint8 *)get_delegate_invoke_impl (TRUE, 0, NULL);
		mono_memory_barrier ();
		cached = start;
		return cached;
	} else {
		static guint8 *cache [MAX_ARCH_DELEGATE_PARAMS + 1] = {NULL};

		if (sig->param_count > MAX_ARCH_DELEGATE_PARAMS)
			return NULL;
		for (int i = 0; i < sig->param_count; ++i)
			if (!mono_is_regsize_var (sig->params [i]))
				return NULL;

		code = cache [sig->param_count];
		if (code)
			return code;
		if (mono_ee_features.use_aot_trampolines) {
			NOT_IMPLEMENTED;
		} else {
			start = (guint8 *)get_delegate_invoke_impl (FALSE, sig->param_count, NULL);
		}
		mono_memory_barrier ();
		cache [sig->param_count] = start;
		return start;
	}

	return NULL;
}

gpointer
mono_arch_get_delegate_virtual_invoke_impl (MonoMethodSignature *sig,
                                            MonoMethod *method,
                                            int offset,
                                            gboolean load_imt_reg)
{
	MonoTrampInfo *info;
	gpointer code;

	if (load_imt_reg)
		// FIXME:
		return FALSE;

	code = get_delegate_virtual_invoke_impl (&info, load_imt_reg, offset);
	if (code)
		mono_tramp_info_register (info, NULL);

	return code;
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
	MonoObject *this = (MonoObject *)regs [RISCV_A0];
	return (gpointer)this;
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

static guint8 *
emit_thunk (guint8 *code, gconstpointer target)
{
	guint8 *p = code;
	code = mono_riscv_emit_imm (code, RISCV_T0, (gsize)target);
	riscv_jalr (code, RISCV_ZERO, RISCV_T0, 0);

	g_assert ((p - code) < THUNK_SIZE);
	mono_arch_flush_icache (p, code - p);
	return code;
}

static gpointer
create_thunk (MonoCompile *cfg, guchar *code, const guchar *target)
{
	MonoJitInfo *ji;
	MonoThunkJitInfo *info;
	guint8 *thunks, *p;
	int thunks_size;
	guint8 *orig_target;
	guint8 *target_thunk;
	MonoJitMemoryManager *jit_mm;

	if (cfg) {
		/*
		 * This can be called multiple times during JITting,
		 * save the current position in cfg->arch to avoid
		 * doing a O(n^2) search.
		 */
		if (!cfg->arch.thunks) {
			cfg->arch.thunks = cfg->thunks;
			cfg->arch.thunks_size = cfg->thunk_area;
		}

		thunks = cfg->arch.thunks;
		thunks_size = cfg->arch.thunks_size;
		if (!thunks_size) {
			g_print ("thunk failed %p->%p, thunk space=%d method %s", code, target, thunks_size,
			         mono_method_full_name (cfg->method, TRUE));
			g_assert_not_reached ();
		}

		g_assert (*(guint32 *)thunks == 0);
		emit_thunk (thunks, target);

		cfg->arch.thunks += THUNK_SIZE;
		cfg->arch.thunks_size -= THUNK_SIZE;

		return thunks;
	} else {
		ji = mini_jit_info_table_find (code);
		g_assert (ji);
		info = mono_jit_info_get_thunk_info (ji);
		g_assert (info);

		thunks = (guint8 *)ji->code_start + info->thunks_offset;
		thunks_size = info->thunks_size;

		orig_target = mono_arch_get_call_target (code + 4);

		/* Arbitrary lock */
		jit_mm = get_default_jit_mm ();

		jit_mm_lock (jit_mm);

		target_thunk = NULL;
		if (orig_target >= thunks && orig_target < thunks + thunks_size) {
			/* The call already points to a thunk, because of trampolines etc. */
			target_thunk = orig_target;
		} else {
			for (p = thunks; p < thunks + thunks_size; p += THUNK_SIZE) {
				if (((guint32 *)p) [0] == 0) {
					/* Free entry */
					target_thunk = p;
					break;
				} else if (*(guint64 *)(p + 4) == (guint64)target) {
					/* Thunk already points to target */
					target_thunk = p;
					break;
				}
			}
		}

		if (!target_thunk) {
			jit_mm_unlock (jit_mm);
			g_print ("thunk failed %p->%p, thunk space=%d method %s", code, target, thunks_size,
			         cfg ? mono_method_full_name (cfg->method, TRUE)
			             : mono_method_full_name (jinfo_get_method (ji), TRUE));
			g_assert_not_reached ();
		}

		emit_thunk (target_thunk, target);

		jit_mm_unlock (jit_mm);

		return target_thunk;
	}
}

static void
riscv_patch_full (MonoCompile *cfg, guint8 *code, guint8 *target, int relocation)
{
	switch (relocation) {
	case MONO_R_RISCV_IMM:
		*(guint64 *)(code + 4) = (guint64)target;
		break;
	case MONO_R_RISCV_JAL: {
		gint32 inst = *(gint32 *)code;
		gint32 rd = RISCV_BITS (inst, 7, 5);
		target = MINI_FTNPTR_TO_ADDR (target);
		if (riscv_is_jal_disp (code, target))
			riscv_jal (code, rd, riscv_get_jal_disp (code, target));
		else {
			gpointer thunk;
			thunk = create_thunk (cfg, code, target);
			g_assert (riscv_is_jal_disp (code, thunk));
			riscv_jal (code, rd, riscv_get_jal_disp (code, thunk));
		}
		break;
	}
	case MONO_R_RISCV_BEQ:
	case MONO_R_RISCV_BNE:
	case MONO_R_RISCV_BGE:
	case MONO_R_RISCV_BLT:
	case MONO_R_RISCV_BGEU:
	case MONO_R_RISCV_BLTU: {
		int offset = target - code;

		gint32 inst = *(gint32 *)code;
		gint32 rs1 = RISCV_BITS (inst, 15, 5);
		gint32 rs2 = RISCV_BITS (inst, 20, 5);

		// if the offset too large to encode as B_IMM
		// try to use jal to branch
		if (!RISCV_VALID_B_IMM (offset)) {
			// branch inst should followed by a nop inst
			g_assert (*(gint32 *)(code + 4) == 0x13);
			if (riscv_is_jal_disp (code, target)) {
				if (relocation == MONO_R_RISCV_BEQ)
					riscv_bne (code, rs1, rs2, 8);
				else if (relocation == MONO_R_RISCV_BNE)
					riscv_beq (code, rs1, rs2, 8);
				else if (relocation == MONO_R_RISCV_BGE)
					riscv_blt (code, rs1, rs2, 8);
				else if (relocation == MONO_R_RISCV_BLT)
					riscv_bge (code, rs1, rs2, 8);
				else if (relocation == MONO_R_RISCV_BGEU)
					riscv_bltu (code, rs1, rs2, 8);
				else if (relocation == MONO_R_RISCV_BLTU)
					riscv_bgeu (code, rs1, rs2, 8);
				else
					g_assert_not_reached ();

				riscv_jal (code, RISCV_ZERO, riscv_get_jal_disp (code, target));
				break;
			} else
				g_assert_not_reached ();
		} else {
			if (relocation == MONO_R_RISCV_BEQ)
				riscv_beq (code, rs1, rs2, offset);
			else if (relocation == MONO_R_RISCV_BNE)
				riscv_bne (code, rs1, rs2, offset);
			else if (relocation == MONO_R_RISCV_BGE)
				riscv_bge (code, rs1, rs2, offset);
			else if (relocation == MONO_R_RISCV_BLT)
				riscv_blt (code, rs1, rs2, offset);
			else if (relocation == MONO_R_RISCV_BGEU)
				riscv_bgeu (code, rs1, rs2, offset);
			else if (relocation == MONO_R_RISCV_BLTU)
				riscv_bltu (code, rs1, rs2, offset);
			else
				g_assert_not_reached ();
		}
		break;
	}
	default:
		NOT_IMPLEMENTED;
	}
}

static void
riscv_patch_rel (guint8 *code, guint8 *target, int relocation)
{
	riscv_patch_full (NULL, code, target, relocation);
}

void
mono_riscv_patch (guint8 *code, guint8 *target, int relocation)
{
	riscv_patch_rel (code, target, relocation);
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
	guint8 *ip;

	ip = ji->ip.i + code;
	switch (ji->type) {
	case MONO_PATCH_INFO_METHOD_JUMP:
		/* ji->relocation is not set by the caller */
		riscv_patch_full (cfg, ip, (guint8 *)target, MONO_R_RISCV_JAL);
		mono_arch_flush_icache (ip, 8);
		break;
	case MONO_PATCH_INFO_NONE:
		break;
	default:
		riscv_patch_full (cfg, ip, (guint8 *)target, ji->relocation);
		break;
	}
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
		ainfo->storage = ArgOnStack;
		ainfo->slot_size = sizeof (host_mgreg_t);
		ainfo->is_signed = sign;
		ainfo->offset = cinfo->stack_usage;
		cinfo->stack_usage += sizeof (host_mgreg_t);
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
		// As ABI specifed, if there is ireg avaliable, store it into ireg
		if (cinfo->next_arg <= RISCV_A7) {
			ainfo->storage = single ? ArgR4InIReg : ArgR8InIReg;
			ainfo->reg = cinfo->next_arg;
			cinfo->next_arg++;
		} else {
			ainfo->storage = single ? ArgOnStackR4 : ArgOnStackR8;
			ainfo->slot_size = size;
			ainfo->offset = cinfo->stack_usage;
			cinfo->stack_usage += size;
		}
	}
}

static gboolean
is_hfa (MonoType *t, int *out_nfields, int *out_esize, int *field_offsets)
{
	MonoClass *klass;
	gpointer iter;
	MonoClassField *field;
	MonoType *ftype, *prev_ftype = NULL;
	int nfields = 0;

	klass = mono_class_from_mono_type_internal (t);
	iter = NULL;
	while ((field = mono_class_get_fields_internal (klass, &iter))) {
		if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;
		ftype = mono_field_get_type_internal (field);
		ftype = mini_get_underlying_type (ftype);

		if (MONO_TYPE_ISSTRUCT (ftype)) {
			int nested_nfields, nested_esize;
			int nested_field_offsets [16];

			MonoType *fixed_etype;
			int fixed_len;
			if (mono_marshal_shared_get_fixed_buffer_attr (field, &fixed_etype, &fixed_len)) {
				if (fixed_etype->type != MONO_TYPE_R4 && fixed_etype->type != MONO_TYPE_R8)
					return FALSE;
				if (fixed_len > 16)
					return FALSE;
				nested_nfields = fixed_len;
				nested_esize = fixed_etype->type == MONO_TYPE_R4 ? 4 : 8;
				for (int i = 0; i < nested_nfields; ++i)
					nested_field_offsets [i] = i * nested_esize;
			} else {
				if (!is_hfa (ftype, &nested_nfields, &nested_esize, nested_field_offsets))
					return FALSE;
			}

			if (nested_esize == 4)
				ftype = m_class_get_byval_arg (mono_defaults.single_class);
			else
				ftype = m_class_get_byval_arg (mono_defaults.double_class);

			if (prev_ftype && prev_ftype->type != ftype->type)
				return FALSE;
			prev_ftype = ftype;
			for (int i = 0; i < nested_nfields; ++i) {
				if (nfields + i < 4)
					field_offsets [nfields + i] =
					    field->offset - MONO_ABI_SIZEOF (MonoObject) + nested_field_offsets [i];
			}
			nfields += nested_nfields;
		} else {
			if (!(!m_type_is_byref (ftype) && (ftype->type == MONO_TYPE_R4 || ftype->type == MONO_TYPE_R8)))
				return FALSE;
			if (prev_ftype && prev_ftype->type != ftype->type)
				return FALSE;
			prev_ftype = ftype;
			if (nfields < 4)
				field_offsets [nfields] = field->offset - MONO_ABI_SIZEOF (MonoObject);
			nfields++;
		}
	}
	if (nfields == 0 || nfields > 2)
		return FALSE;
	*out_nfields = nfields;
	*out_esize = prev_ftype->type == MONO_TYPE_R4 ? 4 : 8;
	return TRUE;
}

static void
add_valuetype (CallInfo *cinfo, ArgInfo *ainfo, MonoType *t)
{
	int size, aligned_size, nfields, esize;
	guint32 align;
	int field_offsets [16];

	size = mini_type_stack_size_full (t, &align, cinfo->pinvoke);
	aligned_size = ALIGN_TO (size, align);

	if (is_hfa (t, &nfields, &esize, field_offsets)) {
		/*
		 * The struct might include nested float structs aligned at 8,
		 * so need to keep track of the offsets of the individual fields.
		 */
		if (cinfo->next_farg + nfields - 1 <= RISCV_FA7) {
			ainfo->storage = ArgHFA;
			ainfo->reg = cinfo->next_farg;
			ainfo->nregs = nfields;
			ainfo->size = size;
			ainfo->esize = esize;
			for (int i = 0; i < nfields; ++i)
				ainfo->foffsets [i] = GINT_TO_UINT8 (field_offsets [i]);
			cinfo->next_farg += ainfo->nregs;
		} else {
			ainfo->nfregs_to_skip = cinfo->next_farg <= RISCV_FA7 ? RISCV_FA7 - cinfo->next_farg + 1 : 0;
			cinfo->next_farg = RISCV_FA7 + 1;

			ainfo->offset = cinfo->stack_usage;
			ainfo->storage = ArgVtypeOnStack;
			cinfo->stack_usage += aligned_size;
			ainfo->slot_size = aligned_size;

			ainfo->hfa = TRUE;
			ainfo->nregs = nfields;
			ainfo->esize = esize;
		}
		return;
	}

	// Scalars wider than 2×XLEN bits are passed by reference
	if (aligned_size > sizeof (host_mgreg_t) * 2) {
		if (cinfo->next_arg > RISCV_A7) {
			ainfo->offset = cinfo->stack_usage;
			ainfo->storage = ArgVtypeByRefOnStack;
			cinfo->stack_usage += aligned_size;
			ainfo->slot_size = aligned_size;
		} else {
			ainfo->storage = ArgVtypeByRef;
			ainfo->reg = cinfo->next_arg;
			ainfo->size = sizeof (host_mgreg_t);
			ainfo->nregs = 1;
			cinfo->next_arg += ainfo->nregs;
		}
	}
	// Scalars that are 2×XLEN bits wide are passed in a pair of argument registers
	else if (aligned_size == sizeof (host_mgreg_t) * 2) {
		// If no argument registers are available, the scalar is passed on the stack by value
		if (cinfo->next_arg > RISCV_A7) {
			ainfo->offset = cinfo->stack_usage;
			ainfo->storage = ArgVtypeOnStack;
			cinfo->stack_usage += aligned_size;
			ainfo->slot_size = aligned_size;
		}
		// If exactly one register is available, the low-order XLEN bits are
		// passed in the register and the high-order XLEN bits are passed on the stack
		else if (cinfo->next_arg == RISCV_A7) {
			ainfo->offset = cinfo->stack_usage;
			ainfo->storage = ArgVtypeInMixed;
			cinfo->stack_usage += sizeof (host_mgreg_t);
			ainfo->slot_size = sizeof (host_mgreg_t);

			ainfo->reg = cinfo->next_arg;
			ainfo->size = sizeof (host_mgreg_t);
			ainfo->nregs = 1;
			cinfo->next_arg += ainfo->nregs;
		}
		// Scalars that are 2×XLEN bits wide are passed in a pair of argument
		// registers, with the low-order XLEN bits in the lower-numbered register
		// and the high-order XLEN bits in the higher-numbered register
		else {
			ainfo->storage = ArgVtypeInIReg;
			ainfo->reg = cinfo->next_arg;
			ainfo->size = sizeof (host_mgreg_t) * 2;
			ainfo->nregs = 2;
			cinfo->next_arg += ainfo->nregs;
		}
	}
	// Scalars that are at most XLEN bits wide are passed in a single argument register
	else {
		if (cinfo->next_arg > RISCV_A7) {
			ainfo->offset = cinfo->stack_usage;
			ainfo->storage = ArgVtypeOnStack;
			cinfo->stack_usage += aligned_size;
			ainfo->slot_size = aligned_size;
		} else {
			ainfo->storage = ArgVtypeInIReg;
			ainfo->reg = cinfo->next_arg;
			ainfo->size = sizeof (host_mgreg_t);
			ainfo->nregs = 1;
			cinfo->next_arg += ainfo->nregs;
		}
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
#ifdef TARGET_RISCV32
	case MONO_TYPE_I:
#endif
	case MONO_TYPE_I4:
		add_arg (cinfo, ainfo, 4, TRUE);
		break;
	case MONO_TYPE_U4:
#ifdef TARGET_RISCV32
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_OBJECT:
#endif
		add_arg (cinfo, ainfo, 4, FALSE);
		break;
#ifdef TARGET_RISCV64
	case MONO_TYPE_I:
#endif
	case MONO_TYPE_I8:
		add_arg (cinfo, ainfo, 8, TRUE);
		break;
	case MONO_TYPE_U8:
#ifdef TARGET_RISCV64
	case MONO_TYPE_U:
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
	case MONO_TYPE_TYPEDBYREF:
		add_valuetype (cinfo, ainfo, ptype);
		break;

	default:
		g_print ("Can't handle as return value 0x%x\n", ptype->type);
		g_assert_not_reached ();
		break;
	}
}

static int
call_info_size (MonoMethodSignature *sig)
{
	int n = sig->hasthis + sig->param_count;

	return sizeof (CallInfo) + (sizeof (ArgInfo) * n);
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
	gboolean is_pinvoke = sig->pinvoke;
	int paramNum = sig->hasthis + sig->param_count;
	int pindex;
	int size = call_info_size (sig);
	if (mp)
		cinfo = mono_mempool_alloc0 (mp, size);
	else
		cinfo = g_malloc0 (size);

	cinfo->nargs = paramNum;

	// return value
	cinfo->next_arg = RISCV_A0;
	cinfo->next_farg = RISCV_FA0;
	add_param (cinfo, &cinfo->ret, sig->ret);

	cinfo->next_arg = RISCV_A0;
	cinfo->next_farg = RISCV_FA0;
	// reset status
	cinfo->stack_usage = 0;

	guint32 paramStart = 0;
	if (cinfo->ret.storage == ArgVtypeByRef && !is_pinvoke && (sig->hasthis || (sig->param_count > 0 && MONO_TYPE_IS_REFERENCE (mini_get_underlying_type (sig->params [0]))))) {
		add_arg (cinfo, cinfo->args + 0, sizeof (host_mgreg_t), FALSE);
		if (!sig->hasthis)
			paramStart = 1;
		add_param (cinfo, &cinfo->ret, sig->ret);
	}
	else{
		// add this pointer as first argument if hasthis == true
		if (sig->hasthis)
			add_arg (cinfo, cinfo->args + 0, sizeof (host_mgreg_t), FALSE);

		if (cinfo->ret.storage == ArgVtypeByRef)
			add_param (cinfo, &cinfo->ret, sig->ret);
	}

	// other general Arguments
	for (pindex = paramStart; pindex < sig->param_count; ++pindex) {
		ArgInfo *ainfo = cinfo->args + sig->hasthis + pindex;

		// process the variable parameter sig->sentinelpos mark the first VARARG
		if ((sig->call_convention == MONO_CALL_VARARG) && (pindex == sig->sentinelpos)) {
			cinfo->next_arg = RISCV_A7 + 1;
			cinfo->next_farg = RISCV_FA7 + 1;
			/* Emit the signature cookie just before the implicit arguments */
			add_param (cinfo, &cinfo->sig_cookie, mono_get_int_type ());
		}

		add_param (cinfo, ainfo, sig->params [pindex]);
	}

	/* Handle the case where there are no implicit arguments */
	if ((sig->call_convention == MONO_CALL_VARARG) && (pindex == sig->sentinelpos)) {
		cinfo->next_arg = RISCV_A7 + 1;
		cinfo->next_farg = RISCV_FA7 + 1;
		/* Emit the signature cookie just before the implicit arguments */
		add_param (cinfo, &cinfo->sig_cookie, mono_get_int_type ());
	}

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
	case ArgHFA:
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

gpointer
mono_arch_get_interp_native_call_info (MonoMemoryManager *mem_manager, MonoMethodSignature *sig)
{
    CallInfo *cinfo = get_call_info (NULL, sig);
	if (mem_manager) {
		int size = call_info_size (sig);
		gpointer res = mono_mem_manager_alloc0 (mem_manager, size);
		memcpy (res, cinfo, size);
		g_free (cinfo);
		return res;
	} else {
		return cinfo;
	}
}

void
mono_arch_free_interp_native_call_info (gpointer call_info)
{
	/* Allocated by get_call_info () */
	g_free (call_info);
}

/* Set arguments in the ccontext (for i2n entry) */
void
mono_arch_set_native_call_context_args (CallContext *ccontext, gpointer frame, MonoMethodSignature *sig, gpointer call_info)
{
	const MonoEECallbacks *interp_cb = mini_get_interp_callbacks ();
	CallInfo *cinfo = (CallInfo*)call_info;
	gpointer storage;
	ArgInfo *ainfo;

	memset (ccontext, 0, sizeof (CallContext));

	ccontext->stack_size = ALIGN_TO (cinfo->stack_usage, MONO_ARCH_FRAME_ALIGNMENT);
	if (ccontext->stack_size)
		ccontext->stack = (guint8 *)g_calloc (1, ccontext->stack_size);

	if (sig->ret->type != MONO_TYPE_VOID) {
		ainfo = &cinfo->ret;
		if (ainfo->storage == ArgVtypeByRef) {
			storage = interp_cb->frame_arg_to_storage ((MonoInterpFrameHandle)frame, sig, -1);
			ccontext->gregs [cinfo->ret.reg] = (gsize)storage;
		}
	}

	g_assert (!sig->hasthis);

	for (int i = 0; i < sig->param_count; i++) {
		ainfo = &cinfo->args [i];

		if (ainfo->storage == ArgVtypeByRef) {
			storage = arg_get_storage (ccontext, ainfo);
			*(gpointer *)storage = interp_cb->frame_arg_to_storage ((MonoInterpFrameHandle)frame, sig, i);
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
}

/* Set return value in the ccontext (for n2i return) */
void
mono_arch_set_native_call_context_ret (CallContext *ccontext, gpointer frame, MonoMethodSignature *sig, gpointer call_info, gpointer retp)
{
	NOT_IMPLEMENTED;
}

/* Gets the arguments from ccontext (for n2i entry) */
gpointer
mono_arch_get_native_call_context_args (CallContext *ccontext, gpointer frame, MonoMethodSignature *sig, gpointer call_info)
{
	NOT_IMPLEMENTED;
}

/* Gets the return value from ccontext (for i2n exit) */
void
mono_arch_get_native_call_context_ret (CallContext *ccontext, gpointer frame, MonoMethodSignature *sig, gpointer call_info)
{
	const MonoEECallbacks *interp_cb;
	CallInfo *cinfo = (CallInfo*)call_info;
	ArgInfo *ainfo;
	gpointer storage;

	if (sig->ret->type == MONO_TYPE_VOID)
		return;

	interp_cb = mini_get_interp_callbacks ();
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
	case OP_IMUL:
	case OP_MUL_IMM:
	case OP_IREM_UN_IMM:
#ifdef TARGET_RISCV64
	case OP_LMUL_IMM:
	case OP_LDIV:
	case OP_LDIV_UN:
	case OP_LREM:
	case OP_LREM_UN:
	case OP_LREM_UN_IMM:
#endif
		return !riscv_stdext_m;

	case OP_FADD:
	case OP_RADD:
	case OP_FSUB:
	case OP_RSUB:
	case OP_FDIV:
	case OP_RDIV:
	case OP_FMUL:
	case OP_RMUL:
	case OP_RNEG:
	case OP_FNEG:
	case OP_FMOVE:
	case OP_RMOVE:
	case OP_RCALL:
	case OP_FCALL:
	case OP_FCEQ:
	case OP_FCLT:
	case OP_FCLT_UN:
	case OP_RCLT:
	case OP_RCLT_UN:
	case OP_FCGT:
	case OP_FCGT_UN:
	case OP_RCOMPARE:
	case OP_FCOMPARE:
	case OP_FBEQ:
	case OP_FBLT:
	case OP_FBLE:
	case OP_FBGT:
	case OP_FBGE:
	case OP_FBGE_UN:
	case OP_FBLT_UN:
	case OP_FBLE_UN:
	case OP_FBGT_UN:
	case OP_RCONV_TO_I2:
	case OP_RCONV_TO_I4:
	case OP_RCONV_TO_U4:
	case OP_ICONV_TO_R4:
	case OP_FCONV_TO_I1:
	case OP_FCONV_TO_U1:
	case OP_FCONV_TO_I2:
	case OP_FCONV_TO_U2:
	case OP_FCONV_TO_I4:
	case OP_FCONV_TO_R4:
	case OP_FCONV_TO_U4:
#ifdef TARGET_RISCV64
	case OP_R8CONST:
	case OP_ICONV_TO_R8:
	case OP_LCONV_TO_R8:
	case OP_FCONV_TO_R8:
	case OP_FCONV_TO_I8:
	case OP_FCONV_TO_U8:
	case OP_FCONV_TO_OVF_U8:
	case OP_RCONV_TO_R8:
#endif
		return !mono_arch_is_soft_float ();
	default:
		return TRUE;
	}
}

gboolean
mono_arch_tailcall_supported (MonoCompile *cfg, MonoMethodSignature *caller_sig, MonoMethodSignature *callee_sig, gboolean virtual_)
{
	g_assert (caller_sig);
	g_assert (callee_sig);

	CallInfo *caller_info = get_call_info (NULL, caller_sig);
	CallInfo *callee_info = get_call_info (NULL, callee_sig);

	// Do not tail call optimize functions with varargs passed by stack.
	gboolean res = IS_SUPPORTED_TAILCALL (callee_sig->call_convention != MONO_CALL_VARARG);

	//	Byval parameters hand the function a pointer directly into the stack area
	//	we want to reuse during a tail call. Do not tail call optimize functions with
	//	byval parameters.
	//
	// Do not tail call optimize if stack is used to pass parameters.
	const ArgInfo *ainfo;
	ainfo = callee_info->args + callee_sig->hasthis;
	for (int i = 0; res && i < callee_sig->param_count; ++i) {
		res == IS_SUPPORTED_TAILCALL (ainfo [i].storage < ArgOnStack);
	}

	// Do not tail call optimize if callee uses structret semantics.
	res &= IS_SUPPORTED_TAILCALL (callee_info->ret.storage < ArgVtypeByRef);

	// Do not tail call optimize if caller uses structret semantics.
	res &= IS_SUPPORTED_TAILCALL (caller_info->ret.storage < ArgVtypeByRef);

	g_free (caller_info);
	g_free (callee_info);

	return res;
}

gboolean
mono_arch_is_inst_imm (int opcode, int imm_opcode, gint64 imm)
{
	// TODO: Make a proper decision based on opcode.
	return TRUE;
}

gint static mono_arch_get_memory_ordering (int memory_barrier_kind)
{
	switch (memory_barrier_kind) {
	case MONO_MEMORY_BARRIER_ACQ:
		return RISCV_ORDER_AQ;
		break;
	case MONO_MEMORY_BARRIER_REL:
		return RISCV_ORDER_RL;
		break;
	case MONO_MEMORY_BARRIER_SEQ:
		return RISCV_ORDER_ALL;
		break;
	default:
		return RISCV_ORDER_NONE;
		break;
	}
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
		MONO_INST_NEW (cfg, ins, OP_RMOVE);
		ins->dreg = mono_alloc_freg (cfg);
		ins->sreg1 = arg->dreg;
		MONO_ADD_INS (cfg->cbb, ins);
		mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, reg, TRUE);
		break;
	case ArgR4InIReg:
		MONO_INST_NEW (cfg, ins, OP_MOVE_F_TO_I4);
		ins->dreg = mono_alloc_ireg (cfg);
		ins->sreg1 = arg->dreg;
		MONO_ADD_INS (cfg->cbb, ins);
		mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, reg, FALSE);
		break;
	case ArgR8InIReg:
#ifdef TARGET_RISCV64
		MONO_INST_NEW (cfg, ins, OP_MOVE_F_TO_I8);
		ins->dreg = mono_alloc_ireg (cfg);
		ins->sreg1 = arg->dreg;
		MONO_ADD_INS (cfg->cbb, ins);
		mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, reg, FALSE);
#else
		NOT_IMPLEMENTED;
#endif
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
	MonoMethodSignature *tmp_sig;
	int sig_reg;

	if (MONO_IS_TAILCALL_OPCODE (call))
		NOT_IMPLEMENTED;

	g_assert (cinfo->sig_cookie.storage == ArgOnStack);

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

	sig_reg = mono_alloc_ireg (cfg);
	MONO_EMIT_NEW_SIGNATURECONST (cfg, sig_reg, tmp_sig);

	MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, RISCV_SP, cinfo->sig_cookie.offset, sig_reg);
}

/**
 * mono_arch_emit_call:
 * 	move all Args to corresponding reg/stack in Caller
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
	case ArgHFA:
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
		case ArgR4InIReg:
		case ArgR8InIReg:
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
		case ArgOnStackR4:
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER4_MEMBASE_REG, RISCV_SP, ainfo->offset, arg->dreg);
			break;
		case ArgOnStackR8:
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER8_MEMBASE_REG, RISCV_SP, ainfo->offset, arg->dreg);
			break;
		case ArgVtypeInIReg:
		case ArgVtypeByRef:
		case ArgVtypeOnStack:
		case ArgVtypeInMixed:
		case ArgVtypeByRefOnStack:
		case ArgHFA: {
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

	if (ins->backend.size == 0)
		return;
	switch (ainfo->storage) {
	case ArgHFA:
		for (int i = 0; i < ainfo->nregs; ++i) {
			if (ainfo->esize == 4)
				MONO_INST_NEW (cfg, load, OP_LOADR4_MEMBASE);
			else
				MONO_INST_NEW (cfg, load, OP_LOADR8_MEMBASE);
			load->dreg = mono_alloc_freg (cfg);
			load->inst_basereg = src->dreg;
			load->inst_offset = ainfo->foffsets [i];
			MONO_ADD_INS (cfg->cbb, load);
			add_outarg_reg (cfg, call, ainfo->esize == 4 ? ArgInFRegR4 : ArgInFReg, ainfo->reg + i, load);
		}
		break;
	case ArgVtypeInIReg:
		MONO_INST_NEW (cfg, load, OP_LOAD_MEMBASE);
		load->dreg = mono_alloc_ireg (cfg);
		load->inst_basereg = src->dreg;
		load->inst_offset = 0;
		MONO_ADD_INS (cfg->cbb, load);
		add_outarg_reg (cfg, call, ArgInIReg, ainfo->reg, load);

		if (ainfo->size > sizeof (host_mgreg_t)) {
			MONO_INST_NEW (cfg, load, OP_LOAD_MEMBASE);
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
			MONO_INST_NEW (cfg, load, OP_LOAD_MEMBASE);
			load->dreg = mono_alloc_ireg (cfg);
			load->inst_basereg = src->dreg;
			load->inst_offset = i;
			MONO_ADD_INS (cfg->cbb, load);
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, RISCV_SP, ainfo->offset + i, load->dreg);
		}
		break;
	case ArgVtypeInMixed: {
		g_assert (ainfo->slot_size == sizeof (host_mgreg_t));

		MONO_INST_NEW (cfg, load, OP_LOAD_MEMBASE);
		load->dreg = ainfo->reg;
		load->inst_basereg = src->dreg;
		load->inst_offset = 0;
		MONO_ADD_INS (cfg->cbb, load);
		add_outarg_reg (cfg, call, ArgInIReg, ainfo->reg, load);

		MONO_INST_NEW (cfg, load, OP_LOAD_MEMBASE);
		load->dreg = mono_alloc_ireg (cfg);
		load->inst_basereg = src->dreg;
		load->inst_offset = sizeof (host_mgreg_t);
		MONO_ADD_INS (cfg->cbb, load);
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, RISCV_SP, ainfo->offset, load->dreg);
		break;
	}
	case ArgVtypeByRef:
	case ArgVtypeByRefOnStack: {
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
		mini_emit_memcpy (cfg, load->dreg, 0, src->dreg, 0, ins->backend.size, TARGET_SIZEOF_VOID_P);
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
#ifdef TARGET_RISCV64
	case ArgInFRegR4:
		if (COMPILE_LLVM (cfg))
			NOT_IMPLEMENTED;
		else
			MONO_EMIT_NEW_UNALU (cfg, OP_RMOVE, cfg->ret->dreg, val->dreg);
		break;
#endif
	default:
		g_print ("can't process Storage type %d\n", cinfo->ret.storage);
		NOT_IMPLEMENTED;
	}
}

void
mono_arch_decompose_opts (MonoCompile *cfg, MonoInst *ins)
{
	switch (ins->opcode) {
	case OP_CKFINITE:
	case OP_FMOVE:
	case OP_RMOVE:
	case OP_IADD:
	case OP_LADD:
	case OP_RADD:
	case OP_FADD:
	case OP_IADD_IMM:
	case OP_LADD_IMM:
	case OP_IADD_OVF:
	case OP_LADD_OVF:
	case OP_LADD_OVF_UN:
	case OP_IADD_OVF_UN:
	case OP_ISUB:
	case OP_LSUB:
	case OP_FSUB:
	case OP_RSUB:
	case OP_ISUB_IMM:
	case OP_LSUB_IMM:
	case OP_ISUB_OVF:
	case OP_LSUB_OVF:
	case OP_ISUB_OVF_UN:
	case OP_LSUB_OVF_UN:

	case OP_INEG:
	case OP_LAND:
	case OP_LOR:
	case OP_IXOR:
	case OP_IXOR_IMM:
	case OP_ICONV_TO_I:
	case OP_ICONV_TO_U:
	case OP_ICONV_TO_I1:
	case OP_ICONV_TO_U1:
	case OP_ICONV_TO_I2:
	case OP_ICONV_TO_U2:
	case OP_RCONV_TO_I1:
	case OP_RCONV_TO_U1:
	case OP_RCONV_TO_I2:
	case OP_RCONV_TO_U2:
	case OP_RCONV_TO_I4:
	case OP_RCONV_TO_U4:
	case OP_FCONV_TO_I1:
	case OP_FCONV_TO_U1:
	case OP_FCONV_TO_I2:
	case OP_FCONV_TO_U2:
	case OP_FCONV_TO_I4:
	case OP_FCONV_TO_U4:
	case OP_ICONV_TO_R_UN:
	case OP_LCONV_TO_R_UN:
	case OP_ICONV_TO_R4:
	case OP_LCONV_TO_R4:
	case OP_RCONV_TO_R4:
	case OP_RCONV_TO_R8:
#ifdef TARGET_RISCV64
	case OP_LXOR_IMM:
	case OP_LNEG:

	case OP_ICONV_TO_I4:
	case OP_ICONV_TO_OVF_I4:
	case OP_ICONV_TO_OVF_I4_UN:
	case OP_ICONV_TO_I8:
	case OP_ICONV_TO_U8:
	case OP_LCONV_TO_U:
	case OP_LCONV_TO_I:
	case OP_LCONV_TO_I1:
	case OP_LCONV_TO_U1:
	case OP_LCONV_TO_I2:
	case OP_LCONV_TO_U2:
	case OP_LCONV_TO_I4:
	case OP_LCONV_TO_U4:
	case OP_LCONV_TO_I8:
	case OP_LCONV_TO_U8:

	case OP_LCONV_TO_OVF_I:
	case OP_LCONV_TO_OVF_I_UN:
	case OP_LCONV_TO_OVF_U:
	case OP_LCONV_TO_OVF_U_UN:
	case OP_LCONV_TO_OVF_I1:
	case OP_LCONV_TO_OVF_I1_UN:
	case OP_LCONV_TO_OVF_U1:
	case OP_LCONV_TO_OVF_U1_UN:
	case OP_LCONV_TO_OVF_I2:
	case OP_LCONV_TO_OVF_I2_UN:
	case OP_LCONV_TO_OVF_U2:
	case OP_LCONV_TO_OVF_U2_UN:
	case OP_LCONV_TO_OVF_I4:
	case OP_LCONV_TO_OVF_I4_UN:
	case OP_LCONV_TO_OVF_U4:
	case OP_LCONV_TO_OVF_U4_UN:
	case OP_LCONV_TO_OVF_I8:
	case OP_LCONV_TO_OVF_I8_UN:
	case OP_LCONV_TO_OVF_U8:
	case OP_LCONV_TO_OVF_U8_UN:

	case OP_ICONV_TO_R8:
	case OP_LCONV_TO_R8:
	case OP_FCONV_TO_R4:
	case OP_FCONV_TO_R8:
	case OP_FCONV_TO_I8:
	case OP_FCONV_TO_U8:
	case OP_FCONV_TO_OVF_I8:
	case OP_FCONV_TO_OVF_U8:
	case OP_RCONV_TO_I8:
	case OP_RCONV_TO_U8:
	case OP_RCONV_TO_OVF_I8:
	case OP_RCONV_TO_OVF_U8:
#endif
	case OP_RNEG:
	case OP_FNEG:
	case OP_IAND:
	case OP_IAND_IMM:
	case OP_LAND_IMM:
	case OP_INOT:
	case OP_LNOT:
	case OP_IOR:
	case OP_IOR_IMM:
	case OP_LOR_IMM:
	case OP_LXOR:
	case OP_ISHL:
	case OP_LSHL:
	case OP_ISHL_IMM:
	case OP_LSHL_IMM:
	case OP_ISHR:
	case OP_LSHR:
	case OP_ISHR_UN:
	case OP_LSHR_UN:
	case OP_ISHR_IMM:
	case OP_LSHR_IMM:
	case OP_ISHR_UN_IMM:
	case OP_LSHR_UN_IMM:

	case OP_IMUL:
	case OP_LMUL:
	case OP_RMUL:
	case OP_FMUL:
	case OP_IMUL_IMM:
	case OP_LMUL_IMM:
	case OP_IMUL_OVF:
	case OP_LMUL_OVF:
	case OP_IMUL_OVF_UN:
	case OP_LMUL_OVF_UN:
	case OP_IMUL_OVF_UN_OOM:
	case OP_LMUL_OVF_UN_OOM:
	case OP_IDIV:
	case OP_LDIV:
	case OP_FDIV:
	case OP_IDIV_UN:
	case OP_LDIV_UN:
	case OP_IDIV_IMM:
	case OP_IDIV_UN_IMM:
	case OP_RDIV:
	case OP_IREM:
	case OP_LREM:
	case OP_FREM:
	case OP_RREM:
	case OP_IREM_IMM:
	case OP_LREM_IMM:
	case OP_IREM_UN:
	case OP_LREM_UN:
	case OP_IREM_UN_IMM:

	case OP_ICONV_TO_OVF_I:
	case OP_ICONV_TO_OVF_U:
	case OP_ICONV_TO_OVF_I1:
	case OP_ICONV_TO_OVF_I1_UN:
	case OP_ICONV_TO_OVF_U1:
	case OP_ICONV_TO_OVF_U1_UN:
	case OP_ICONV_TO_OVF_I2:
	case OP_ICONV_TO_OVF_I2_UN:
	case OP_ICONV_TO_OVF_U2:
	case OP_ICONV_TO_OVF_U2_UN:
	case OP_ICONV_TO_OVF_I8:
	case OP_ICONV_TO_OVF_I8_UN:
	case OP_ICONV_TO_OVF_U4_UN:
	case OP_ICONV_TO_OVF_U8:
	case OP_ICONV_TO_OVF_U8_UN:
	case OP_ICONV_TO_OVF_I_UN:
	case OP_ICONV_TO_OVF_U_UN:

		break;

	case OP_ICONV_TO_U4:
		ins->opcode = OP_ZEXT_I4;
		break;
	case OP_ICONV_TO_OVF_U4:
		MONO_EMIT_NEW_UNALU (cfg, OP_SEXT_I4, ins->dreg, ins->sreg1);
		MONO_EMIT_NEW_ICOMPARE_IMM (cfg, ins->dreg, 0);
		MONO_EMIT_NEW_COND_EXC (cfg, ILT, "OverflowException");
		MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, ins->dreg, ins->sreg1);
		NULLIFY_INS (ins);
		break;
	default:
		g_print ("Can't decompose the OP %s\n", mono_inst_name (ins->opcode));
		NOT_IMPLEMENTED;
	}
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
#ifdef TARGET_RISCV64
		case ArgInFRegR4:
#endif
			cfg->ret->opcode = OP_REGVAR;
			cfg->ret->inst_c0 = cinfo->ret.reg;
			cfg->ret->dreg = cinfo->ret.reg;
			break;
		case ArgHFA:
		case ArgVtypeInIReg:
			/* Allocate a local to hold the result, the epilog will copy it to the correct place */
			cfg->ret->opcode = OP_REGOFFSET;
			cfg->ret->inst_basereg = cfg->frame_reg;
			g_assert (cinfo->ret.nregs > 0);
			offset += cinfo->ret.nregs * sizeof (host_mgreg_t);
			cfg->ret->inst_offset = -offset;
			break;
		case ArgVtypeByRef:
			/**
			 * Caller pass the address of return value as an implicit param.
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
		case ArgR4InIReg:
		case ArgR8InIReg:
			offset += sizeof (host_mgreg_t);
			ins->inst_offset = -offset;
			break;
		case ArgOnStack:
		case ArgOnStackR4:
		case ArgOnStackR8:
		case ArgVtypeOnStack:
			/* These are in the parent frame */
			g_assert (ainfo->offset >= 0);
			ins->inst_basereg = RISCV_FP;
			ins->inst_offset = ainfo->offset;
			break;
		case ArgHFA:
		case ArgVtypeInIReg:
			ins->opcode = OP_REGOFFSET;
			ins->inst_basereg = cfg->frame_reg;
			/* These arguments are saved to the stack in the prolog */
			g_assert (ainfo->nregs > 0);
			offset += ainfo->nregs * sizeof (host_mgreg_t);
			ins->inst_offset = -offset;
			break;
		case ArgVtypeInMixed:
			ins->opcode = OP_REGOFFSET;
			ins->inst_basereg = cfg->frame_reg;
			offset += sizeof (host_mgreg_t) * 2;
			ins->inst_offset = -offset;
			break;
		case ArgVtypeByRefOnStack: {
			MonoInst *vtaddr;

			/* The vtype address is in the parent frame */
			g_assert (ainfo->offset >= 0);
			MONO_INST_NEW (cfg, vtaddr, 0);
			vtaddr->opcode = OP_REGOFFSET;
			vtaddr->inst_basereg = RISCV_FP;
			vtaddr->inst_offset = ainfo->offset;

			/* Need an indirection */
			ins->opcode = OP_VTARG_ADDR;
			ins->inst_left = vtaddr;
			break;
		}
		case ArgVtypeByRef: {
			MonoInst *vtaddr;

			// if (ainfo->gsharedvt) {
			// 	ins->opcode = OP_REGOFFSET;
			// 	ins->inst_basereg = cfg->frame_reg;
			// 	ins->inst_offset = offset;
			// 	offset += 8;
			// 	break;
			// }

			/* The vtype address is in a register, will be copied to the stack in the prolog */
			MONO_INST_NEW (cfg, vtaddr, 0);
			vtaddr->opcode = OP_REGOFFSET;
			vtaddr->inst_basereg = cfg->frame_reg;
			offset += sizeof (host_mgreg_t);
			vtaddr->inst_offset = -offset;

			/* Need an indirection */
			ins->opcode = OP_VTARG_ADDR;
			ins->inst_left = vtaddr;
			break;
		}
		default:
			g_print ("unable allocate var with type %d.\n", ainfo->storage);
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

#define NEW_INS_BEFORE(cfg, ins, dest, op)                                                                             \
	do {                                                                                                               \
		MONO_INST_NEW ((cfg), (dest), (op));                                                                           \
		(dest)->cil_code = (ins)->cil_code;                                                                            \
		mono_bblock_insert_before_ins (bb, ins, (dest));                                                               \
	} while (0)

#define NEW_INS_AFTER(cfg, ins, next_ins, dest, op)                                                                    \
	do {                                                                                                               \
		MONO_INST_NEW ((cfg), (dest), (op));                                                                           \
		(dest)->cil_code = (ins)->cil_code;                                                                            \
		mono_bblock_insert_after_ins (bb, ins, (dest));                                                                \
		next_ins = dest;                                                                                               \
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
	MonoInst *ins, *last_ins, *next_ins, *temp;
	if (cfg->verbose_level > 2) {
		g_print ("BASIC BLOCK %d (before lowering)\n", bb->block_num);
		MONO_BB_FOR_EACH_INS (bb, ins)
		{
			mono_print_ins (ins);
		}
	}

	MONO_BB_FOR_EACH_INS_SAFE (bb, next_ins, ins)
	{
	loop_start:
		switch (ins->opcode) {
		case OP_ARGLIST:
		case OP_CKFINITE:
		case OP_BREAK:
		case OP_IL_SEQ_POINT:
		case OP_SEQ_POINT:
		case OP_GC_SAFE_POINT:
		case OP_BR:
		case OP_BR_REG:
		case OP_JUMP_TABLE:
		case OP_TAILCALL_PARAMETER:
		case OP_TAILCALL:
		case OP_TAILCALL_REG:
		case OP_TAILCALL_MEMBASE:
		case OP_CALL:
		case OP_RCALL:
		case OP_FCALL:
		case OP_LCALL:
		case OP_VCALL2:
		case OP_VOIDCALL:
		case OP_START_HANDLER:
		case OP_CALL_HANDLER:
		case OP_ENDFINALLY:
		case OP_ENDFILTER:
		case OP_GET_EX_OBJ:
		case OP_GENERIC_CLASS_INIT:
		case OP_I8CONST:
		case OP_ICONST:
		case OP_MOVE:
		case OP_RMOVE:
		case OP_FMOVE:
		case OP_MOVE_F_TO_I4:
		case OP_MOVE_I4_TO_F:
#ifdef TARGET_RISCV64
		case OP_MOVE_F_TO_I8:
		case OP_MOVE_I8_TO_F:
#endif
		case OP_LMOVE:
		case OP_ISUB:
		case OP_LSUB:
		case OP_FSUB:
		case OP_RSUB:
		case OP_IADD:
		case OP_LADD:
		case OP_IMUL:
		case OP_LMUL:
		case OP_RMUL:
		case OP_FMUL:
		case OP_FDIV:
		case OP_IDIV:
		case OP_LDIV:
		case OP_IDIV_UN:
		case OP_LDIV_UN:
		case OP_RDIV:
		case OP_IREM:
		case OP_LREM:
		case OP_IREM_UN:
		case OP_LREM_UN:
		case OP_CHECK_THIS:
		case OP_IAND:
		case OP_LAND:
		case OP_IXOR:
		case OP_LXOR:
		case OP_IOR:
		case OP_LOR:
		case OP_ISHL:
		case OP_LSHL:
		case OP_ISHR:
		case OP_ISHR_UN:
		case OP_LSHR:
		case OP_LSHR_UN:
		case OP_LOCALLOC:

		/* skip dummy IL */
		case OP_NOT_REACHED:
		case OP_NOT_NULL:
		case OP_DUMMY_USE:
		case OP_NOP:
		case OP_RELAXED_NOP:

		/* skip custom OP code*/
		case OP_RISCV_BEQ:
		case OP_RISCV_BNE:
		case OP_RISCV_BGE:
		case OP_RISCV_BGEU:
		case OP_RISCV_BLT:
		case OP_RISCV_BLTU:
		case OP_RISCV_EXC_BEQ:
		case OP_RISCV_EXC_BNE:
		case OP_RISCV_EXC_BGEU:
		case OP_RISCV_EXC_BLT:
		case OP_RISCV_EXC_BLTU:
		case OP_RISCV_SLT:
		case OP_RISCV_SLTU:
		case OP_RISCV_SLTIU:
			break;

		/* Atomic Ext */
		case OP_MEMORY_BARRIER:
		case OP_ATOMIC_ADD_I4:
		case OP_ATOMIC_STORE_I1:
		case OP_ATOMIC_STORE_U1:
		case OP_ATOMIC_STORE_I2:
		case OP_ATOMIC_STORE_U2:
		case OP_ATOMIC_STORE_I4:
		case OP_ATOMIC_STORE_U4:
		case OP_ATOMIC_STORE_I8:
		case OP_ATOMIC_STORE_U8:
		case OP_ATOMIC_LOAD_I1:
		case OP_ATOMIC_LOAD_U1:
		case OP_ATOMIC_LOAD_I2:
		case OP_ATOMIC_LOAD_U2:
		case OP_ATOMIC_LOAD_I4:
		case OP_ATOMIC_LOAD_U4:
		case OP_ATOMIC_LOAD_I8:
		case OP_ATOMIC_LOAD_U8:
		case OP_ATOMIC_CAS_I4:
		case OP_ATOMIC_CAS_I8:
		case OP_ATOMIC_EXCHANGE_I4:
#ifdef TARGET_RISCV64
		case OP_ATOMIC_ADD_I8:
		case OP_ATOMIC_EXCHANGE_I8:
#endif

		/* Float Ext */
		case OP_R8CONST:
		case OP_RADD:
		case OP_FADD:
		case OP_RNEG:
		case OP_FNEG:
		case OP_ICONV_TO_R8:
		case OP_RCONV_TO_R8:
		case OP_RCONV_TO_I8:
		case OP_RCONV_TO_I4:
		case OP_RCONV_TO_U4:
		case OP_FCONV_TO_I4:
		case OP_FCONV_TO_U4:
		case OP_FCONV_TO_R4:
		case OP_FCONV_TO_I8:
		case OP_FCEQ:
		case OP_FCLE:
		case OP_FCLT:
		case OP_FCLT_UN:
		case OP_RCEQ:
		case OP_RCLE:
		case OP_RCLT:
		case OP_RCLT_UN:
		case OP_RISCV_SETFREG_R4:
		case OP_R4CONST:
		case OP_ICONV_TO_R4:
			break;
		case OP_RCGT:
		case OP_RCGT_UN: {
			// rcgt rd, rs1, rs2 -> rlt rd, rs2, rs1
			ins->opcode = OP_RCLT;
			int tmp_reg = ins->sreg1;
			ins->sreg1 = ins->sreg2;
			ins->sreg2 = tmp_reg;
			break;
		}
		case OP_RCGE:
		case OP_FCGE: {
			// rcge rd, rs1, rs2 -> rcle rd, rs2, rs1
			if (ins->opcode == OP_FCGE)
				ins->opcode = OP_FCLE;
			else
				ins->opcode = OP_RCLE;
			int tmp_reg = ins->sreg1;
			ins->sreg1 = ins->sreg2;
			ins->sreg2 = tmp_reg;
			break;
		}
		case OP_RCNEQ:
		case OP_FCNEQ: {
			// fcneq rd, rs1, rs2 -> fceq rd, rs1, rs2, ceq rd, rd, zero
			if (ins->opcode == OP_FCNEQ)
				ins->opcode = OP_FCEQ;
			else
				ins->opcode = OP_RCEQ;
			NEW_INS_AFTER (cfg, ins, next_ins, temp, OP_CEQ);
			temp->dreg = ins->dreg;
			temp->sreg1 = temp->dreg;
			temp->sreg2 = RISCV_ZERO;
			break;
		}
		case OP_FCGT:
		case OP_FCGT_UN: {
			// fcgt rd, rs1, rs2 -> flt rd, rs2, rs1
			ins->opcode = OP_FCLT;
			int tmp_reg = ins->sreg1;
			ins->sreg1 = ins->sreg2;
			ins->sreg2 = tmp_reg;
			break;
		}
		case OP_FCONV_TO_I1:
		case OP_RCONV_TO_I1: {
			// fconv_to_i1 rd, fs1 => fconv_to_i{4|8} rs1, fs1; {i|l}conv_to_i1 rd, rs1
			int rd = ins->dreg;
#ifdef TARGET_RISCV64
			NEW_INS_AFTER (cfg, ins, next_ins, temp, OP_LCONV_TO_I1);
			if (ins->opcode == OP_FCONV_TO_I1)
				ins->opcode = OP_FCONV_TO_I8;
			else
				ins->opcode = OP_RCONV_TO_I8;
#else
			NEW_INS_AFTER (cfg, ins, next_ins, temp, OP_ICONV_TO_I1);
			if (ins->opcode == OP_FCONV_TO_I1)
				ins->opcode = OP_FCONV_TO_I4;
			else
				ins->opcode = OP_RCONV_TO_I4;
#endif
			ins->dreg = mono_alloc_ireg (cfg);

			temp->dreg = rd;
			temp->sreg1 = ins->dreg;
			break;
		}
		case OP_FCONV_TO_U1:
		case OP_RCONV_TO_U1: {
			// fconv_to_u1 rd, fs1 => fconv_to_u{4|8} rs1, fs1; {i|l}conv_to_u1 rd, rs1
			int rd = ins->dreg;
#ifdef TARGET_RISCV64
			NEW_INS_AFTER (cfg, ins, next_ins, temp, OP_LCONV_TO_U1);
			if (ins->opcode == OP_FCONV_TO_U1)
				ins->opcode = OP_FCONV_TO_U8;
			else
				ins->opcode = OP_RCONV_TO_U8;
#else
			NEW_INS_AFTER (cfg, ins, next_ins, temp, OP_ICONV_TO_U1);
			if (ins->opcode == OP_FCONV_TO_U1)
				ins->opcode = OP_FCONV_TO_U4;
			else
				ins->opcode = OP_RCONV_TO_U4;
#endif
			ins->dreg = mono_alloc_ireg (cfg);

			temp->dreg = rd;
			temp->sreg1 = ins->dreg;
			break;
		}
		case OP_FCONV_TO_I2:
		case OP_RCONV_TO_I2: {
			// fconv_to_i2 rd, fs1 => fconv_to_i{4|8} rs1, fs1; {i|l}conv_to_i2 rd, rs1
			int rd = ins->dreg;
#ifdef TARGET_RISCV64
			NEW_INS_AFTER (cfg, ins, next_ins, temp, OP_LCONV_TO_I2);
			if (ins->opcode == OP_FCONV_TO_I2)
				ins->opcode = OP_FCONV_TO_I8;
			else
				ins->opcode = OP_RCONV_TO_I8;
#else
			NEW_INS_AFTER (cfg, ins, next_ins, temp, OP_ICONV_TO_I2);
			if (ins->opcode == OP_FCONV_TO_I2)
				ins->opcode = OP_FCONV_TO_I4;
			else
				ins->opcode = OP_RCONV_TO_I4;
#endif
			ins->dreg = mono_alloc_ireg (cfg);

			temp->dreg = rd;
			temp->sreg1 = ins->dreg;
			break;
		}
		case OP_FCONV_TO_U2:
		case OP_RCONV_TO_U2: {
			// fconv_to_u2 rd, fs1 => fconv_to_u{4|8} rs1, fs1; {i|l}conv_to_u2 rd, rs1
			int rd = ins->dreg;
#ifdef TARGET_RISCV64
			NEW_INS_AFTER (cfg, ins, next_ins, temp, OP_LCONV_TO_U2);
			if (ins->opcode == OP_FCONV_TO_U2)
				ins->opcode = OP_FCONV_TO_U8;
			else
				ins->opcode = OP_RCONV_TO_U8;
#else
			NEW_INS_AFTER (cfg, ins, next_ins, temp, OP_ICONV_TO_U2);
			if (ins->opcode == OP_FCONV_TO_U2)
				ins->opcode = OP_FCONV_TO_U4;
			else
				ins->opcode = OP_RCONV_TO_U4;
#endif
			ins->dreg = mono_alloc_ireg (cfg);

			temp->dreg = rd;
			temp->sreg1 = ins->dreg;
			break;
		}
		case OP_RCONV_TO_R4: {
			if (ins->dreg != ins->sreg1)
				ins->opcode = OP_RMOVE;
			else
				NULLIFY_INS (ins);
			break;
		}
		case OP_RCOMPARE: {
			if (next_ins) {
				// insert two OP_RISCV_FBNAN in case unordered compare
				if (next_ins->opcode == OP_FBLT_UN || next_ins->opcode == OP_FBGT_UN ||
				    next_ins->opcode == OP_FBGE_UN || next_ins->opcode == OP_FBLE_UN ||
				    next_ins->opcode == OP_FBNE_UN) {
					NEW_INS_BEFORE (cfg, ins, temp, OP_RISCV_RBNAN);
					temp->sreg1 = ins->sreg1;
					temp->inst_many_bb = mono_mempool_alloc (cfg->mempool, sizeof (gpointer) * 2);
					temp->inst_true_bb = next_ins->inst_true_bb;
					NEW_INS_BEFORE (cfg, ins, temp, OP_RISCV_RBNAN);
					temp->sreg1 = ins->sreg2;
					temp->inst_many_bb = mono_mempool_alloc (cfg->mempool, sizeof (gpointer) * 2);
					temp->inst_true_bb = next_ins->inst_true_bb;
				}

				if (next_ins->opcode == OP_FBLT || next_ins->opcode == OP_FBLT_UN) {
					ins->opcode = OP_RCLT;
					ins->dreg = mono_alloc_ireg (cfg);

					next_ins->opcode = OP_RISCV_BNE;
					next_ins->sreg1 = ins->dreg;
					next_ins->sreg2 = RISCV_ZERO;
				} else if (next_ins->opcode == OP_FBGT || next_ins->opcode == OP_FBGT_UN) {
					// rcmp rd, rs1, rs2; fbgt rd -> rcgt rd, rs1, rs2; bne rd, X0
					// rcgt rd, rs1, rs2 -> flt.s rd, rs2, rs1
					ins->opcode = OP_RCLT;
					ins->dreg = mono_alloc_ireg (cfg);
					int tmp_reg = ins->sreg1;
					ins->sreg1 = ins->sreg2;
					ins->sreg2 = tmp_reg;

					next_ins->opcode = OP_RISCV_BNE;
					next_ins->sreg1 = ins->dreg;
					next_ins->sreg2 = RISCV_ZERO;
				} else if (next_ins->opcode == OP_FBGE || next_ins->opcode == OP_FBGE_UN) {
					// rcmp rd, rs1, rs2; fbge rd -> rcle rd, rs2, rs1; bne rd, X0
					ins->opcode = OP_RCLE;
					ins->dreg = mono_alloc_ireg (cfg);
					int tmp_reg = ins->sreg1;
					ins->sreg1 = ins->sreg2;
					ins->sreg2 = tmp_reg;

					next_ins->opcode = OP_RISCV_BNE;
					next_ins->sreg1 = ins->dreg;
					next_ins->sreg2 = RISCV_ZERO;
				} else if (next_ins->opcode == OP_FBLE || next_ins->opcode == OP_FBLE_UN) {
					ins->opcode = OP_RCLE;
					ins->dreg = mono_alloc_ireg (cfg);

					next_ins->opcode = OP_RISCV_BNE;
					next_ins->sreg1 = ins->dreg;
					next_ins->sreg2 = RISCV_ZERO;
				} else if (next_ins->opcode == OP_FBEQ) {
					// rcmp rs1, rs2; fbeq rd -> rceq rd, rs1, rs2; bne rd, X0
					ins->opcode = OP_RCEQ;
					ins->dreg = mono_alloc_ireg (cfg);

					next_ins->opcode = OP_RISCV_BNE;
					next_ins->sreg1 = ins->dreg;
					next_ins->sreg2 = RISCV_ZERO;
				} else if (next_ins->opcode == OP_FBNE_UN) {
					// rcmp rs1, rs2; fbne rd -> rceq rd, rs1, rs2; beq rd, X0
					ins->opcode = OP_RCEQ;
					ins->dreg = mono_alloc_ireg (cfg);

					next_ins->opcode = OP_RISCV_BEQ;
					next_ins->sreg1 = ins->dreg;
					next_ins->sreg2 = RISCV_ZERO;
				} else {
					if (cfg->verbose_level > 1) {
						g_print ("Unhandaled op %s following after OP_RCOMPARE\n", mono_inst_name (next_ins->opcode));
					}
					NULLIFY_INS (ins);
				}
			} else {
				NULLIFY_INS (ins);
				// g_assert_not_reached ();
			}
			break;
		}
		case OP_FCOMPARE: {
			if (next_ins) {
				if (next_ins->opcode == OP_FBLT_UN || next_ins->opcode == OP_FBGT_UN ||
				    next_ins->opcode == OP_FBGE_UN || next_ins->opcode == OP_FBLE_UN ||
				    next_ins->opcode == OP_FBNE_UN) {
					// insert two OP_RISCV_FBNAN in case unordered compare
					NEW_INS_BEFORE (cfg, ins, temp, OP_RISCV_FBNAN);
					temp->sreg1 = ins->sreg1;
					temp->inst_many_bb = mono_mempool_alloc (cfg->mempool, sizeof (gpointer) * 2);
					temp->inst_true_bb = next_ins->inst_true_bb;
					NEW_INS_BEFORE (cfg, ins, temp, OP_RISCV_FBNAN);
					temp->sreg1 = ins->sreg2;
					temp->inst_many_bb = mono_mempool_alloc (cfg->mempool, sizeof (gpointer) * 2);
					temp->inst_true_bb = next_ins->inst_true_bb;
				}

				if (next_ins->opcode == OP_FBLT || next_ins->opcode == OP_FBLT_UN) {
					ins->opcode = OP_FCLT;
					ins->dreg = mono_alloc_ireg (cfg);

					next_ins->opcode = OP_RISCV_BNE;
					next_ins->sreg1 = ins->dreg;
					next_ins->sreg2 = RISCV_ZERO;
				} else if (next_ins->opcode == OP_FBGT || next_ins->opcode == OP_FBGT_UN) {
					// fcmp rd, rs1, rs2; fbgt rd -> fclt rd, rs2, rs1; bne rd, X0
					ins->opcode = OP_FCLT;
					ins->dreg = mono_alloc_ireg (cfg);
					int tmp_reg = ins->sreg1;
					ins->sreg1 = ins->sreg2;
					ins->sreg2 = tmp_reg;

					next_ins->opcode = OP_RISCV_BNE;
					next_ins->sreg1 = ins->dreg;
					next_ins->sreg2 = RISCV_ZERO;
				} else if (next_ins->opcode == OP_FBGE || next_ins->opcode == OP_FBGE_UN) {
					// fcmp rd, rs1, rs2; fbge rd -> fcle rd, rs2, rs1; bne rd, X0
					ins->opcode = OP_FCLE;
					ins->dreg = mono_alloc_ireg (cfg);
					int tmp_reg = ins->sreg1;
					ins->sreg1 = ins->sreg2;
					ins->sreg2 = tmp_reg;

					next_ins->opcode = OP_RISCV_BNE;
					next_ins->sreg1 = ins->dreg;
					next_ins->sreg2 = RISCV_ZERO;
				} else if (next_ins->opcode == OP_FBLE || next_ins->opcode == OP_FBLE_UN) {
					ins->opcode = OP_FCLE;
					ins->dreg = mono_alloc_ireg (cfg);

					next_ins->opcode = OP_RISCV_BNE;
					next_ins->sreg1 = ins->dreg;
					next_ins->sreg2 = RISCV_ZERO;
				} else if (next_ins->opcode == OP_FBNE_UN) {
					// fcmp rd, rs1, rs2; fbne rd -> fceq rd, rs1, rs2; beq rd, X0
					ins->opcode = OP_FCEQ;
					ins->dreg = mono_alloc_ireg (cfg);
					ins->sreg1 = ins->sreg1;
					ins->sreg2 = ins->sreg2;

					next_ins->opcode = OP_RISCV_BEQ;
					next_ins->sreg1 = ins->dreg;
					next_ins->sreg2 = RISCV_ZERO;
				} else if (next_ins->opcode == OP_FBEQ) {
					// fcmp rd, rs1, rs2; fbeq rd -> fceq rd, rs1, rs2; bne rd, X0
					ins->opcode = OP_FCEQ;
					ins->dreg = mono_alloc_ireg (cfg);
					ins->sreg1 = ins->sreg1;
					ins->sreg2 = ins->sreg2;

					next_ins->opcode = OP_RISCV_BNE;
					next_ins->sreg1 = ins->dreg;
					next_ins->sreg2 = RISCV_ZERO;
				} else {
					if (cfg->verbose_level > 1) {
						g_print ("Unhandaled op %s following after OP_FCOMPARE\n", mono_inst_name (next_ins->opcode));
					}
					NULLIFY_INS (ins);
				}
			} else {
				NULLIFY_INS (ins);
				// g_assert_not_reached ();
			}
			break;
		}

		case OP_LOCALLOC_IMM:
			if (ins->inst_imm > 32) {
				NEW_INS_BEFORE (cfg, ins, temp, OP_I8CONST);
				temp->inst_l = ins->inst_imm;
				temp->dreg = mono_alloc_ireg (cfg);

				g_assert (mono_op_imm_to_op (ins->opcode) != -1);
				ins->opcode = GINT_TO_OPCODE (mono_op_imm_to_op (ins->opcode));
				ins->sreg1 = temp->dreg;
			}
			break;

		case OP_CALL_MEMBASE:
		case OP_RCALL_MEMBASE:
		case OP_LCALL_MEMBASE:
		case OP_FCALL_MEMBASE:
		case OP_VCALL2_MEMBASE:
		case OP_VOIDCALL_MEMBASE:
			if (!RISCV_VALID_J_IMM (ins->inst_offset)) {
				NOT_IMPLEMENTED;
				NEW_INS_BEFORE (cfg, ins, temp, OP_I8CONST);
				temp->dreg = mono_alloc_ireg (cfg);
				temp->inst_l = ins->inst_offset;

				ins->sreg1 = temp->dreg;

				NEW_INS_BEFORE (cfg, ins, temp, OP_LADD);
				temp->dreg = ins->sreg1;
				temp->sreg1 = ins->sreg1;
				temp->sreg2 = ins->inst_basereg;

				ins->inst_basereg = -1;
				ins->inst_offset = 0;
				// convert OP_.*CALL_MEMBASE into OP_.*CALL_REG
				ins->opcode -= 1;
			}
			break;
		case OP_CALL_REG:
		case OP_LCALL_REG:
		case OP_RCALL_REG:
		case OP_FCALL_REG:
		case OP_VOIDCALL_REG:
		case OP_VCALL2_REG:
			break;

		/* Throw */
		case OP_THROW:
		case OP_RETHROW:
			if (ins->sreg1 != RISCV_A0) {
				NEW_INS_BEFORE (cfg, ins, temp, OP_MOVE);
				temp->dreg = RISCV_A0;
				temp->sreg1 = ins->sreg1;
				ins->sreg1 = RISCV_A0;
			}
			break;
		// RISC-V dosn't support store Imm to Memory directly
		// store Imm into Reg firstly.
		case OP_STOREI1_MEMBASE_IMM:
		case OP_STOREI2_MEMBASE_IMM:
		case OP_STOREI4_MEMBASE_IMM:
#ifdef TARGET_RISCV64
		case OP_STOREI8_MEMBASE_IMM:
#endif
		case OP_STORE_MEMBASE_IMM: {
			if (ins->inst_imm != 0) {
				NEW_INS_BEFORE (cfg, ins, temp, OP_I8CONST);
				temp->inst_l = ins->inst_imm;
				temp->dreg = mono_alloc_ireg (cfg);

				ins->sreg1 = temp->dreg;
			} else {
				ins->sreg1 = RISCV_ZERO;
			}

			switch (ins->opcode) {
			case OP_STORE_MEMBASE_IMM:
				ins->opcode = OP_STORE_MEMBASE_REG;
				break;
			case OP_STOREI1_MEMBASE_IMM:
				ins->opcode = OP_STOREI1_MEMBASE_REG;
				break;
			case OP_STOREI2_MEMBASE_IMM:
				ins->opcode = OP_STOREI2_MEMBASE_REG;
				break;
			case OP_STOREI4_MEMBASE_IMM:
				ins->opcode = OP_STOREI4_MEMBASE_REG;
				break;
#ifdef TARGET_RISCV64
			case OP_STOREI8_MEMBASE_IMM:
				ins->opcode = OP_STOREI8_MEMBASE_REG;
				break;
#endif
			default:
				g_assert_not_reached ();
				break;
			}
			goto loop_start; /* make it handle the possibly big ins->inst_offset */
		}
		// Inst S{B|H|W|D} use I-type Imm
		case OP_STOREI1_MEMBASE_REG:
		case OP_STOREI2_MEMBASE_REG:
		case OP_STOREI4_MEMBASE_REG:
		case OP_STORER4_MEMBASE_REG:
#ifdef TARGET_RISCV64
		case OP_STOREI8_MEMBASE_REG:
#endif
		case OP_STORER8_MEMBASE_REG:
		case OP_STORE_MEMBASE_REG: {
			if (ins->opcode == OP_STORER4_MEMBASE_REG && !cfg->r4fp) {
				NEW_INS_BEFORE (cfg, ins, temp, OP_FCONV_TO_R4);
				temp->dreg = mono_alloc_freg (cfg);
				temp->sreg1 = ins->sreg1;
				ins->sreg1 = temp->dreg;
			}
			// check if offset is valid I-type Imm
			if (!RISCV_VALID_I_IMM (ins->inst_offset)) {
				/**
				 * iconst t0, offset
				 * add t0, rd, t0
				 * store rs1, 0(t0)
				 */
				int offset_reg = mono_alloc_ireg (cfg);
				NEW_INS_BEFORE (cfg, ins, temp, OP_I8CONST);
				temp->dreg = offset_reg;
				temp->inst_l = ins->inst_offset;

#ifdef TARGET_RISCV64
				NEW_INS_BEFORE (cfg, ins, temp, OP_LADD);
#else
				NEW_INS_BEFORE (cfg, ins, temp, OP_IADD);
#endif
				temp->dreg = offset_reg;
				temp->sreg1 = ins->inst_destbasereg;
				temp->sreg2 = offset_reg;

				ins->inst_offset = 0;
				ins->inst_destbasereg = offset_reg;
			}
			break;
		}

		// Inst L{B|H|W|D} use I-type Imm
		case OP_LOADI1_MEMBASE:
		case OP_LOADU1_MEMBASE:
		case OP_LOADI2_MEMBASE:
		case OP_LOADU2_MEMBASE:
		case OP_LOADI4_MEMBASE:
		case OP_LOADU4_MEMBASE:
#ifdef TARGET_RISCV64
		case OP_LOADI8_MEMBASE:
#endif
		case OP_LOADR4_MEMBASE:
		case OP_LOADR8_MEMBASE:
		case OP_LOAD_MEMBASE:
			if (!RISCV_VALID_I_IMM (ins->inst_imm)) {
				NEW_INS_BEFORE (cfg, ins, temp, OP_I8CONST);
				temp->inst_l = ins->inst_imm;
				temp->dreg = mono_alloc_ireg (cfg);
				ins->sreg1 = temp->dreg;
				ins->inst_imm = 0;
			}
			break;

		case OP_COMPARE_IMM:
		case OP_ICOMPARE_IMM:
		case OP_LCOMPARE_IMM: {
			if (next_ins) {
				if (next_ins->opcode == OP_LCEQ || next_ins->opcode == OP_ICEQ) {
					if (RISCV_VALID_I_IMM (ins->inst_imm)) {
						// compare rs1, imm; lceq rd => addi rs2, rs1, -imm; sltiu rd, rs2, 1
						if (next_ins->opcode == OP_ICEQ)
							ins->opcode = OP_IADD_IMM;
						else
							ins->opcode = OP_LADD_IMM;
						ins->dreg = mono_alloc_ireg (cfg);
						ins->inst_imm = -ins->inst_imm;

						next_ins->opcode = OP_RISCV_SLTIU;
						next_ins->sreg1 = ins->dreg;
						next_ins->inst_imm = 1;
						break;
					}
				}
				if (next_ins->opcode == OP_ICNEQ) {
					if (RISCV_VALID_I_IMM (ins->inst_imm)) {
						// compare rs1, imm; lcneq rd => addi rs2, rs1, -imm; sltu rd, X0, rs2
						ins->opcode = OP_IADD_IMM;
						ins->dreg = mono_alloc_ireg (cfg);
						ins->inst_imm = -ins->inst_imm;

						next_ins->opcode = OP_RISCV_SLTU;
						next_ins->sreg1 = RISCV_ZERO;
						next_ins->sreg2 = ins->dreg;
						break;
					}
				} else if (next_ins->opcode == OP_LCGT_UN || next_ins->opcode == OP_ICGT_UN) {
					if ((ins->inst_imm != -1) && RISCV_VALID_I_IMM (ins->inst_imm + 1)) {
						// compare rs1, imm; lcgt_un rd => sltiu rd, rs1, imm; xori rd, rd, 1
						ins->opcode = OP_RISCV_SLTIU;
						ins->dreg = next_ins->dreg;
						ins->sreg1 = ins->sreg1;
						ins->inst_imm = ins->inst_imm + 1;

						next_ins->opcode = OP_XOR_IMM;
						next_ins->dreg = ins->dreg;
						next_ins->sreg1 = ins->dreg;
						next_ins->inst_imm = 1;
						break;
					} else if ((ins->inst_imm == -1)) {
						// rs1 will never greater than -1
						next_ins->opcode = OP_ADD_IMM;
						next_ins->sreg1 = RISCV_ZERO;
						next_ins->inst_imm = 0;

						NULLIFY_INS (ins);
						break;
					}
				} else if (next_ins->opcode == OP_LCGT || next_ins->opcode == OP_ICGT) {
					if (RISCV_VALID_I_IMM (ins->inst_imm + 1)) {
						ins->opcode = OP_RISCV_SLTI;
						ins->dreg = next_ins->dreg;
						ins->sreg1 = ins->sreg1;
						ins->inst_imm = ins->inst_imm + 1;

						next_ins->opcode = OP_XOR_IMM;
						next_ins->dreg = ins->dreg;
						next_ins->sreg1 = ins->dreg;
						next_ins->inst_imm = 1;
						break;
					}
				}
			} else {
				// why it will reach this?
				NULLIFY_INS (ins);
				break;
			}

			if (ins->inst_imm == 0) {
				ins->sreg2 = RISCV_ZERO;
			} else {
				NEW_INS_BEFORE (cfg, ins, temp, OP_I8CONST);
				temp->inst_l = ins->inst_imm;
				temp->dreg = mono_alloc_ireg (cfg);
				ins->sreg2 = temp->dreg;
				ins->inst_imm = 0;
			}
			ins->opcode = OP_ICOMPARE;
			goto loop_start;
		}
		case OP_COMPARE:
		case OP_ICOMPARE:
		case OP_LCOMPARE: {
			if (next_ins) {
				if (next_ins->opcode == OP_COND_EXC_EQ || next_ins->opcode == OP_COND_EXC_IEQ) {
					next_ins->opcode = OP_RISCV_EXC_BEQ;
					next_ins->sreg1 = ins->sreg1;
					next_ins->sreg2 = ins->sreg2;
					NULLIFY_INS (ins);
				} else if (next_ins->opcode == OP_COND_EXC_NE_UN) {
					next_ins->opcode = OP_RISCV_EXC_BNE;
					next_ins->sreg1 = ins->sreg1;
					next_ins->sreg2 = ins->sreg2;
					NULLIFY_INS (ins);
				} else if (next_ins->opcode == OP_COND_EXC_LT || next_ins->opcode == OP_COND_EXC_ILT) {
					next_ins->opcode = OP_RISCV_EXC_BLT;
					next_ins->sreg1 = ins->sreg1;
					next_ins->sreg2 = ins->sreg2;
					NULLIFY_INS (ins);
				} else if (next_ins->opcode == OP_COND_EXC_LT_UN || next_ins->opcode == OP_COND_EXC_ILT_UN) {
					next_ins->opcode = OP_RISCV_EXC_BLTU;
					next_ins->sreg1 = ins->sreg1;
					next_ins->sreg2 = ins->sreg2;
					NULLIFY_INS (ins);
				} else if (next_ins->opcode == OP_COND_EXC_LE_UN) {
					next_ins->opcode = OP_RISCV_EXC_BGEU;
					next_ins->sreg1 = ins->sreg2;
					next_ins->sreg2 = ins->sreg1;
					NULLIFY_INS (ins);
				} else if (next_ins->opcode == OP_COND_EXC_IGT || next_ins->opcode == OP_COND_EXC_GT) {
					next_ins->opcode = OP_RISCV_EXC_BLT;
					next_ins->sreg1 = ins->sreg2;
					next_ins->sreg2 = ins->sreg1;
					NULLIFY_INS (ins);
				} else if (next_ins->opcode == OP_COND_EXC_IGT_UN || next_ins->opcode == OP_COND_EXC_GT_UN) {
					next_ins->opcode = OP_RISCV_EXC_BLTU;
					next_ins->sreg1 = ins->sreg2;
					next_ins->sreg2 = ins->sreg1;
					NULLIFY_INS (ins);
				} else if (next_ins->opcode == OP_LBEQ || next_ins->opcode == OP_IBEQ) {
					next_ins->opcode = OP_RISCV_BEQ;
					next_ins->sreg1 = ins->sreg1;
					next_ins->sreg2 = ins->sreg2;
					NULLIFY_INS (ins);
				} else if (next_ins->opcode == OP_LBNE_UN || next_ins->opcode == OP_IBNE_UN) {
					next_ins->opcode = OP_RISCV_BNE;
					next_ins->sreg1 = ins->sreg1;
					next_ins->sreg2 = ins->sreg2;
					NULLIFY_INS (ins);
				} else if (next_ins->opcode == OP_LBGE_UN || next_ins->opcode == OP_IBGE_UN) {
					next_ins->opcode = OP_RISCV_BGEU;
					next_ins->sreg1 = ins->sreg1;
					next_ins->sreg2 = ins->sreg2;
					NULLIFY_INS (ins);
				} else if (next_ins->opcode == OP_LBGE || next_ins->opcode == OP_IBGE) {
					next_ins->opcode = OP_RISCV_BGE;
					next_ins->sreg1 = ins->sreg1;
					next_ins->sreg2 = ins->sreg2;
					NULLIFY_INS (ins);
				} else if (next_ins->opcode == OP_LBGT_UN || next_ins->opcode == OP_IBGT_UN) {
					next_ins->opcode = OP_RISCV_BLTU;
					next_ins->sreg1 = ins->sreg2;
					next_ins->sreg2 = ins->sreg1;
					NULLIFY_INS (ins);
				} else if (next_ins->opcode == OP_LBGT || next_ins->opcode == OP_IBGT) {
					next_ins->opcode = OP_RISCV_BLT;
					next_ins->sreg1 = ins->sreg2;
					next_ins->sreg2 = ins->sreg1;
					NULLIFY_INS (ins);
				} else if (next_ins->opcode == OP_LBLE || next_ins->opcode == OP_IBLE) {
					// ble rs1, rs2  -> bge rs2, rs1
					next_ins->opcode = OP_RISCV_BGE;
					next_ins->sreg1 = ins->sreg2;
					next_ins->sreg2 = ins->sreg1;
					NULLIFY_INS (ins);
				} else if (next_ins->opcode == OP_LBLE_UN || next_ins->opcode == OP_IBLE_UN) {
					// ble rs1, rs2  -> bge rs2, rs1
					next_ins->opcode = OP_RISCV_BGEU;
					next_ins->sreg1 = ins->sreg2;
					next_ins->sreg2 = ins->sreg1;
					NULLIFY_INS (ins);
				} else if (next_ins->opcode == OP_LBLT_UN || next_ins->opcode == OP_IBLT_UN) {
					next_ins->opcode = OP_RISCV_BLTU;
					next_ins->sreg1 = ins->sreg1;
					next_ins->sreg2 = ins->sreg2;
					NULLIFY_INS (ins);
				} else if (next_ins->opcode == OP_LBLT || next_ins->opcode == OP_IBLT) {
					next_ins->opcode = OP_RISCV_BLT;
					next_ins->sreg1 = ins->sreg1;
					next_ins->sreg2 = ins->sreg2;
					NULLIFY_INS (ins);
				} else if (next_ins->opcode == OP_LCLT || next_ins->opcode == OP_ICLT) {
					next_ins->opcode = OP_RISCV_SLT;
					next_ins->sreg1 = ins->sreg1;
					next_ins->sreg2 = ins->sreg2;
					NULLIFY_INS (ins);
				} else if (next_ins->opcode == OP_LCLT_UN || next_ins->opcode == OP_ICLT_UN) {
					next_ins->opcode = OP_RISCV_SLTU;
					next_ins->sreg1 = ins->sreg1;
					next_ins->sreg2 = ins->sreg2;
					NULLIFY_INS (ins);
				} else if (next_ins->opcode == OP_LCEQ) {
					// compare rs1, rs2; lceq rd => xor rd, rs1, rs2; sltiu rd, rd, 1
					ins->opcode = OP_IXOR;
					ins->dreg = next_ins->dreg;

					next_ins->opcode = OP_RISCV_SLTIU;
					next_ins->sreg1 = ins->dreg;
					next_ins->inst_imm = 1;
				} else if (next_ins->opcode == OP_ICEQ) {
					// compare rs1, rs2; lceq rd => xor rd, rs1, rs2; sltiu rd, rd, 1
					ins->opcode = OP_IXOR;
					ins->dreg = next_ins->dreg;

					next_ins->opcode = OP_RISCV_SLTIU;
					next_ins->sreg1 = ins->dreg;
					next_ins->inst_imm = 1;

					// insert a sext.i4 between XOR and SLTIU
					// will change next_ins pointer
					NEW_INS_AFTER (cfg, ins, next_ins, temp, OP_SEXT_I4);
					temp->dreg = ins->dreg;
					temp->sreg1 = ins->dreg;
				} else if (next_ins->opcode == OP_ICNEQ) {
					// compare rs1, rs2; lcneq rd => xor rd, rs1, rs2; sltu rd, X0, rd
					ins->opcode = OP_IXOR;
					ins->dreg = next_ins->dreg;

					next_ins->opcode = OP_RISCV_SLTU;
					next_ins->sreg1 = RISCV_ZERO;
					next_ins->sreg2 = ins->dreg;

					// insert a sext.i4 between XOR and SLTU
					// will change next_ins pointer
					NEW_INS_AFTER (cfg, ins, next_ins, temp, OP_SEXT_I4);
					temp->dreg = ins->dreg;
					temp->sreg1 = ins->dreg;
				} else if (next_ins->opcode == OP_LCGT || next_ins->opcode == OP_ICGT) {
					next_ins->opcode = OP_RISCV_SLT;
					next_ins->sreg1 = ins->sreg2;
					next_ins->sreg2 = ins->sreg1;
					NULLIFY_INS (ins);
				} else if (next_ins->opcode == OP_LCGT_UN || next_ins->opcode == OP_ICGT_UN) {
					next_ins->opcode = OP_RISCV_SLTU;
					next_ins->sreg1 = ins->sreg2;
					next_ins->sreg2 = ins->sreg1;
					NULLIFY_INS (ins);
				} else {
					/**
					 * there is compare without branch OP followed
					 *
					 *  icompare_imm R226
					 *  call
					 *
					 * what should I do?
					 */
					if (cfg->verbose_level > 1) {
						g_print ("Unhandaled op %s following after OP_{I|L}COMPARE{|_IMM}\n",
						         mono_inst_name (next_ins->opcode));
					}
					NULLIFY_INS (ins);
					break;
				}
			} else
				NULLIFY_INS (ins);
			break;
		}

		/* Math */
		case OP_INEG:
		case OP_LNEG:
			if (ins->opcode == OP_INEG)
				ins->opcode = OP_ISUB;
			else
				ins->opcode = OP_LSUB;
			ins->sreg2 = ins->sreg1;
			ins->sreg1 = RISCV_ZERO;
			break;
		case OP_SUB_IMM:
		case OP_ISUB_IMM:
		case OP_LSUB_IMM:
			ins->inst_imm = -ins->inst_imm;
			// convert OP_{I|L}SUB_IMM to their corresponding ADD_IMM
			ins->opcode -= 1;
			goto loop_start;
		case OP_SUBCC:
		case OP_ISUBCC:
		case OP_LSUBCC:
			/**
			 * sub rd, rs1, rs2
			 * slti t1, x0, rs2
			 * slt t2, rd, rs1
			 * bne t1, t2, overflow
			*/
			if (ins->opcode == OP_SUBCC) {
#ifdef TARGET_RISCV64
				ins->opcode = OP_LSUB;
#else
				ins->opcode = OP_ISUB;
#endif
			} else if (ins->opcode == OP_LSUBCC)
				ins->opcode = OP_LSUB;
			else
				ins->opcode = OP_ISUB;
			MonoInst *branch_ins = next_ins;
			if (branch_ins) {
				if (branch_ins->opcode == OP_COND_EXC_NC || branch_ins->opcode == OP_COND_EXC_C ||
				    branch_ins->opcode == OP_COND_EXC_IC) {
					// bltu rs1, rd, overflow
					branch_ins->opcode = OP_RISCV_EXC_BLTU;
					branch_ins->sreg1 = ins->sreg1;
					branch_ins->sreg2 = ins->dreg;
				} else if (branch_ins->opcode == OP_COND_EXC_OV || next_ins->opcode == OP_COND_EXC_IOV) {
					// bne t1, t2, overflow
					branch_ins->opcode = OP_RISCV_EXC_BNE;
					branch_ins->sreg1 = mono_alloc_ireg (cfg);
					branch_ins->sreg2 = mono_alloc_ireg (cfg);

					// slt t1, x0, rs2
					NEW_INS_BEFORE (cfg, branch_ins, temp, OP_RISCV_SLT);
					temp->dreg = branch_ins->sreg1;
					temp->sreg1 = RISCV_ZERO;
					temp->sreg2 = ins->sreg2;

					// slt t2, rd, rs1
					NEW_INS_BEFORE (cfg, branch_ins, temp, OP_RISCV_SLT);
					temp->dreg = branch_ins->sreg2;
					temp->sreg1 = ins->dreg;
					temp->sreg2 = ins->sreg1;
				} else {
					mono_print_ins (branch_ins);
					g_assert_not_reached ();
				}
			}
			break;
		// Inst ADDI use I-type Imm
		case OP_ADD_IMM:
		case OP_IADD_IMM:
		case OP_LADD_IMM:
			if (!RISCV_VALID_I_IMM (ins->inst_imm)) {
				NEW_INS_BEFORE (cfg, ins, temp, OP_I8CONST);
				temp->inst_l = ins->inst_imm;
				temp->dreg = mono_alloc_ireg (cfg);

				g_assert (mono_op_imm_to_op (ins->opcode) != -1);
				ins->opcode = GINT_TO_OPCODE (mono_op_imm_to_op (ins->opcode));
				ins->sreg2 = temp->dreg;
			}
			break;
		case OP_ADDCC:
		case OP_IADDCC: {
			/**
			 * add t0, t1, t2
			 * slti t3, t2, 0
			 * slt t4,t0,t1
			 * bne t3, t4, overflow
			 */
#ifdef TARGET_RISCV64
			if (ins->opcode == OP_ADDCC)
				ins->opcode = OP_LADD;
			else
#endif
				ins->opcode = OP_IADD;

			MonoInst *branch_ins = next_ins;
			if (branch_ins) {
				if (branch_ins->opcode == OP_COND_EXC_C || branch_ins->opcode == OP_COND_EXC_IC) {
					//  bltu rd, rs1, overflow

					branch_ins->opcode = OP_RISCV_EXC_BLTU;
					branch_ins->sreg1 = ins->dreg;
					branch_ins->sreg2 = ins->sreg1;
				} else if (branch_ins->opcode == OP_COND_EXC_OV || branch_ins->opcode == OP_COND_EXC_IOV) {
					// bne t3, t4, overflow
					branch_ins->opcode = OP_RISCV_EXC_BNE;
					branch_ins->sreg1 = mono_alloc_ireg (cfg);
					branch_ins->sreg2 = mono_alloc_ireg (cfg);

					// slti t3, t2, 0
					NEW_INS_BEFORE (cfg, branch_ins, temp, OP_RISCV_SLTI);
					temp->dreg = branch_ins->sreg1;
					temp->sreg1 = ins->sreg2;
					temp->inst_imm = 0;

					// slt t4,t0,t1
					NEW_INS_BEFORE (cfg, branch_ins, temp, OP_RISCV_SLT);
					temp->dreg = branch_ins->sreg2;
					temp->sreg1 = ins->dreg;
					temp->sreg2 = ins->sreg1;
				} else {
					mono_print_ins (branch_ins);
					g_assert_not_reached ();
				}
			}
			break;
		}
		case OP_MUL_IMM:
			g_assert (riscv_stdext_m);
			NEW_INS_BEFORE (cfg, ins, temp, OP_I8CONST);
			temp->inst_l = ins->inst_imm;
			temp->dreg = mono_alloc_ireg (cfg);
			ins->sreg2 = temp->dreg;
			ins->inst_imm = 0;
#ifdef TARGET_RISCV64
				ins->opcode = OP_LMUL;
#else
				ins->opcode = OP_IMUL;
#endif
			    break;
		case OP_DIV_IMM:
			NEW_INS_BEFORE (cfg, ins, temp, OP_I8CONST);
			temp->inst_l = ins->inst_imm;
			temp->dreg = mono_alloc_ireg (cfg);

#ifdef TARGET_RISCV64
			g_assert (mono_op_imm_to_op (ins->opcode) == OP_LDIV);
#else
			g_assert (mono_op_imm_to_op (ins->opcode) == OP_IDIV);
#endif
			ins->opcode = mono_op_imm_to_op (ins->opcode);
			ins->sreg2 = temp->dreg;
			break;
		case OP_DIV_UN_IMM:
			NEW_INS_BEFORE (cfg, ins, temp, OP_I8CONST);
			temp->inst_l = ins->inst_imm;
			temp->dreg = mono_alloc_ireg (cfg);

#ifdef TARGET_RISCV64
			g_assert (mono_op_imm_to_op (ins->opcode) == OP_LDIV_UN);
#else
			g_assert (mono_op_imm_to_op (ins->opcode) == OP_IDIV_UN);
#endif
			ins->opcode = mono_op_imm_to_op (ins->opcode);
			ins->sreg2 = temp->dreg;
			break;
		case OP_IMUL_IMM:
		case OP_LMUL_IMM:
		case OP_IDIV_IMM:
		case OP_IDIV_UN_IMM:
		case OP_IREM_IMM:
		case OP_LREM_IMM:
		case OP_IREM_UN_IMM:
		case OP_LREM_UN_IMM:
			NEW_INS_BEFORE (cfg, ins, temp, OP_I8CONST);
			temp->inst_l = ins->inst_imm;
			temp->dreg = mono_alloc_ireg (cfg);

			g_assert (mono_op_imm_to_op (ins->opcode) != -1);
			ins->opcode = GINT_TO_OPCODE (mono_op_imm_to_op (ins->opcode));
			ins->sreg2 = temp->dreg;
			break;

		// Bit OP
		case OP_AND_IMM:
		case OP_IAND_IMM:
		case OP_LAND_IMM:
		case OP_IOR_IMM:
		case OP_LOR_IMM:
		case OP_XOR_IMM:
		case OP_IXOR_IMM:
		case OP_LXOR_IMM:
		case OP_SHL_IMM:
		case OP_ISHL_IMM:
		case OP_LSHL_IMM:
		case OP_LSHR_IMM:
		case OP_LSHR_UN_IMM:
		case OP_SHR_IMM:
		case OP_ISHR_IMM:
		case OP_SHR_UN_IMM:
		case OP_ISHR_UN_IMM:
			if (!RISCV_VALID_LS_AMOUNT (ins->inst_imm)) {
				NEW_INS_BEFORE (cfg, ins, temp, OP_I8CONST);
				temp->inst_l = ins->inst_imm;
				temp->dreg = mono_alloc_ireg (cfg);

				g_assert (mono_op_imm_to_op (ins->opcode) != -1);
				ins->opcode = GINT_TO_OPCODE (mono_op_imm_to_op (ins->opcode));
				ins->sreg2 = temp->dreg;
			}
			break;
		case OP_INOT:
		case OP_LNOT:
			ins->opcode = OP_XOR_IMM;
			ins->inst_imm = -1;
			break;
		case OP_ICONV_TO_I1:
		case OP_LCONV_TO_I1:
			// slli    a0, a0, 56
			// srai    a0, a0, 56
			NEW_INS_BEFORE (cfg, ins, temp, OP_SHL_IMM);
			temp->dreg = ins->dreg;
			temp->sreg1 = ins->sreg1;
			temp->inst_imm = 56;

			ins->opcode = OP_SHR_IMM;
			ins->dreg = ins->dreg;
			ins->sreg1 = temp->dreg;
			ins->inst_imm = 56;
			break;
		case OP_ICONV_TO_U1:
		case OP_LCONV_TO_U1:
			// andi rd, rs1, 255
			ins->opcode = OP_AND_IMM;
			ins->inst_imm = 255;
			break;
		case OP_ICONV_TO_U2:
		case OP_LCONV_TO_U2:
			// slli    a0, a0, 48
			// srli    a0, a0, 48
			NEW_INS_BEFORE (cfg, ins, temp, OP_SHL_IMM);
			temp->dreg = ins->dreg;
			temp->sreg1 = ins->sreg1;
			temp->inst_imm = 48;

			ins->opcode = OP_SHR_UN_IMM;
			ins->dreg = ins->dreg;
			ins->sreg1 = temp->dreg;
			ins->inst_imm = 48;
			break;
		case OP_ICONV_TO_I2:
		case OP_LCONV_TO_I2:
			// slli    a0, a0, 48
			// srai    a0, a0, 48
			NEW_INS_BEFORE (cfg, ins, temp, OP_SHL_IMM);
			temp->dreg = ins->dreg;
			temp->sreg1 = ins->sreg1;
			temp->inst_imm = 48;

			ins->opcode = OP_SHR_IMM;
			ins->dreg = ins->dreg;
			ins->sreg1 = temp->dreg;
			ins->inst_imm = 48;
			break;
#ifdef TARGET_RISCV64
		case OP_SEXT_I4:
			ins->opcode = OP_RISCV_ADDIW;
			ins->inst_imm = 0;
			break;
		case OP_ZEXT_I4: {
			// TODO: Add inst riscv_adduw in riscv-codegen.h
			// if(riscv_stdext_b){
			if (FALSE) {
				NOT_IMPLEMENTED;
				ins->opcode = OP_RISCV_ADDUW;
				ins->sreg2 = RISCV_ZERO;
			} else {
				// slli a0, a1, 32
				// srli a0, a0, 32
				NEW_INS_BEFORE (cfg, ins, temp, OP_SHL_IMM);
				temp->dreg = ins->dreg;
				temp->sreg1 = ins->sreg1;
				temp->inst_imm = 32;

				ins->opcode = OP_SHR_UN_IMM;
				ins->sreg1 = temp->dreg;
				ins->inst_imm = 32;
			}
			break;
		}
#endif
		default:
			printf ("unable to lower the following IR:");
			mono_print_ins (ins);
			NOT_IMPLEMENTED;
			break;
		}
		last_ins = ins;
	}

	bb->last_ins = last_ins;
	bb->max_vreg = cfg->next_vreg;

	if (cfg->verbose_level > 2) {
		g_print ("BASIC BLOCK %d (after lowering)\n", bb->block_num);
		MONO_BB_FOR_EACH_INS (bb, ins)
		{
			mono_print_ins (ins);
		}
	}
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
	if (RISCV_VALID_IMM32 (imm)) {
		gint32 Hi = RISCV_BITS (imm, 12, 20);
		gint32 Lo = RISCV_BITS (imm, 0, 12);

		// Lo is in signed num
		// if Lo >= 0x800
		// convert into ((Hi + 1) << 20) -  (0x1000 - Lo)
		if (Lo >= 0x800) {
			if (imm > 0)
				Hi += 1;
			Lo = Lo - 0x1000;
		}

		g_assert (Hi <= 0xfffff);
		riscv_lui (code, rd, Hi);
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
mono_riscv_emit_load (guint8 *code, int rd, int rs1, target_mgreg_t imm, int length)
{
	if (!RISCV_VALID_I_IMM (imm)) {
		g_assert (rs1 != RISCV_T0);
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

// Uses at most 16 bytes on RV32I and 24 bytes on RV64I.
guint8 *
mono_riscv_emit_loadu (guint8 *code, int rd, int rs1, target_mgreg_t imm, int length)
{
	if (!RISCV_VALID_I_IMM (imm)) {
		g_assert (rs1 != RISCV_T0);
		code = mono_riscv_emit_imm (code, RISCV_T0, imm);
		riscv_add (code, RISCV_T0, rs1, RISCV_T0);
		rs1 = RISCV_T0;
		imm = 0;
	}

	switch (length) {
	case 1:
		riscv_lbu (code, rd, rs1, imm);
		break;
	case 2:
		riscv_lhu (code, rd, rs1, imm);
		break;
#ifdef TARGET_RISCV64
	case 4:
		riscv_lwu (code, rd, rs1, imm);
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
mono_riscv_emit_fload (guint8 *code, int rd, int rs1, target_mgreg_t imm, gboolean isSingle)
{
	g_assert (riscv_stdext_d || (isSingle && riscv_stdext_f));
	if (!RISCV_VALID_I_IMM (imm)) {
		g_assert (rs1 != RISCV_T0);
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
mono_riscv_emit_store (guint8 *code, int rs2, int rs1, target_mgreg_t imm, int length)
{
	if (!RISCV_VALID_S_IMM (imm)) {
		g_assert (rs1 != RISCV_T0 && rs2 != RISCV_T0);
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
mono_riscv_emit_fstore (guint8 *code, int rs2, int rs1, target_mgreg_t imm, gboolean isSingle)
{
	g_assert (riscv_stdext_d || (isSingle && riscv_stdext_f));
	if (!RISCV_VALID_I_IMM (imm)) {
		g_assert (rs1 != RISCV_T0);
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
		code = mono_riscv_emit_imm (code, RISCV_T0, offset);
		riscv_add (code, RISCV_T0, basereg, RISCV_T0);
		basereg = RISCV_T0;
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
		code = mono_riscv_emit_imm (code, RISCV_T0, offset);
		riscv_add (code, RISCV_T0, basereg, RISCV_T0);
		basereg = RISCV_T0;
		offset = 0;
	}

	for (int i = 0; i < 32; ++i) {
		if (regs & (1 << i)) {
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
		code = mono_riscv_emit_imm (code, RISCV_T0, offset);
		riscv_add (code, RISCV_T0, basereg, RISCV_T0);
		basereg = RISCV_T0;
		offset = 0;
	}

	for (int i = 0; i < 32; ++i) {
		if (regs & (1 << i)) {
			if (!isFloat && i == RISCV_SP)
				g_assert_not_reached ();
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

static __attribute__ ((__warn_unused_result__)) guint8 *
emit_store_stack_cfa (MonoCompile *cfg, guint8 *code, guint64 regs, int basereg, int offset, guint64 no_cfa_regset)
{
	guint32 cfa_regset = regs & ~no_cfa_regset;
	g_assert (basereg == RISCV_FP);
	g_assert (offset <= 0);

	code = mono_riscv_emit_store_stack(code, regs, basereg, offset, FALSE);

	int pos = 0;
	for (int i = 0; i < 32; ++i){
		if (regs & (1 << i)) {
			if (cfa_regset & (1 << i))
				mono_emit_unwind_op_offset (cfg, code, i, offset + (pos * sizeof (host_mgreg_t)));
			pos++;
		}
	}
	return code;
}

/* Same as mono_riscv_emitstore_regarray, but emit unwind info */
/* CFA_OFFSET is the offset between the CFA and basereg */
static __attribute__ ((__warn_unused_result__)) guint8 *
emit_store_regarray_cfa (MonoCompile *cfg, guint8 *code, guint64 regs, int basereg, int offset, guint64 no_cfa_regset)
{
	guint32 cfa_regset = regs & ~no_cfa_regset;
	g_assert (basereg == RISCV_FP);
	g_assert (offset <= 0);

	for (int i = 0; i < 32; ++i) {
		if (regs & (1 << i)) {
			code = mono_riscv_emit_store (code, i, basereg, offset + (i * sizeof (host_mgreg_t)), 0);

			if (cfa_regset & (1 << i)) {
				mono_emit_unwind_op_offset (cfg, code, i, offset + (i * sizeof (host_mgreg_t)));
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
emit_setup_lmf (MonoCompile *cfg, guint8 *code, gint32 lmf_offset)
{
	/*
	 * The LMF should contain all the state required to be able to reconstruct the machine state
	 * at the current point of execution. Since the LMF is only read during EH, only callee
	 * saved etc. registers need to be saved.
	 * FIXME: Save callee saved fp regs, JITted code doesn't use them, but native code does, and they
	 * need to be restored during EH.
	 */
	g_assert (lmf_offset <= 0);
	/* callee saved gregs + sp */
	code = emit_store_regarray_cfa (cfg, code, MONO_ARCH_LMF_REGS, RISCV_FP,
	                                lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, gregs), (1 << RISCV_SP | 1 << RISCV_FP));

	/* pc */
	riscv_auipc (code, RISCV_RA, 0);
	code = mono_riscv_emit_store (code, RISCV_RA, RISCV_FP, lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, pc), 0);

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
				if (ins->dreg != ainfo->reg)
					riscv_addi (code, ins->dreg, ainfo->reg, 0);
				if (i == 0 && sig->hasthis) {
					mono_add_var_location (cfg, ins, TRUE, ainfo->reg, 0, 0, code - cfg->native_code);
					mono_add_var_location (cfg, ins, TRUE, ins->dreg, 0, code - cfg->native_code, 0);
				}
				break;
			case ArgOnStack:
				code = mono_riscv_emit_load (code, ins->dreg, RISCV_FP, ainfo->offset, ainfo->slot_size);
				break;
			default:
				g_print ("Can't handle arg type %d\n", ainfo->storage);
				NOT_IMPLEMENTED;
			}
		} else {
			if (ainfo->storage != ArgVtypeByRef && ainfo->storage != ArgVtypeByRefOnStack)
				g_assert (ins->opcode == OP_REGOFFSET);

			switch (ainfo->storage) {
			case ArgInIReg:
			case ArgR4InIReg:
			case ArgR8InIReg:
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
				g_assert (ainfo->nregs <= 2);
				if (ainfo->nregs == 2)
					code = mono_riscv_emit_store (code, ainfo->reg + 1, ins->inst_basereg,
					                              ins->inst_offset + sizeof (host_mgreg_t), 0);
				code = mono_riscv_emit_store (code, ainfo->reg, ins->inst_basereg, ins->inst_offset, 0);
				break;
			case ArgVtypeInMixed:
				code = mono_riscv_emit_load (code, RISCV_T1, RISCV_S0, 0, 0);
				code = mono_riscv_emit_store (code, RISCV_T1, ins->inst_basereg,
				                              ins->inst_offset + sizeof (host_mgreg_t), 0);
				code = mono_riscv_emit_store (code, ainfo->reg, ins->inst_basereg, ins->inst_offset, 0);
				break;
			case ArgVtypeByRef:
				// if (ainfo->gsharedvt) {
				// 	g_assert (ins->opcode == OP_GSHAREDVT_ARG_REGOFFSET);
				// 	arm_strx (code, ainfo->reg, ins->inst_basereg, ins->inst_offset);
				// } else {
				g_assert (ins->opcode == OP_VTARG_ADDR);
				g_assert (ins->inst_left->opcode == OP_REGOFFSET);
				code = mono_riscv_emit_store (code, ainfo->reg, ins->inst_left->inst_basereg,
				                              ins->inst_left->inst_offset, 0);
				// }
				break;
			case ArgHFA:
				for (int part = 0; part < ainfo->nregs; part++) {
					if (ainfo->esize == 4)
						code = mono_riscv_emit_fstore (code, ainfo->reg + part, ins->inst_basereg,
						                               GTMREG_TO_INT (ins->inst_offset + ainfo->foffsets [part]), TRUE);
					else
						code =
						    mono_riscv_emit_fstore (code, ainfo->reg + part, ins->inst_basereg,
						                            GTMREG_TO_INT (ins->inst_offset + ainfo->foffsets [part]), FALSE);
				}
				break;
			case ArgOnStack:
			case ArgOnStackR4:
			case ArgOnStackR8:
			case ArgVtypeOnStack:
			case ArgVtypeByRefOnStack:
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
				riscv_fsgnj_s (code, call->inst.dreg, cinfo->ret.reg, cinfo->ret.reg);
		}
		break;
	case ArgInFRegR4:
		g_assert (riscv_stdext_d || riscv_stdext_f);
		if (call->inst.dreg != cinfo->ret.reg) {
			riscv_fsgnj_s (code, call->inst.dreg, cinfo->ret.reg, cinfo->ret.reg);
		}
		break;
	case ArgVtypeInIReg: {
		MonoInst *loc = cfg->arch.vret_addr_loc;

		/* Load the destination address */
		g_assert (loc && loc->opcode == OP_REGOFFSET);
		code = mono_riscv_emit_load (code, RISCV_T1, loc->inst_basereg, loc->inst_offset, 0);
		code = mono_riscv_emit_store (code, cinfo->ret.reg, RISCV_T1, 0, 0);
		g_assert (cinfo->ret.nregs <= 2);
		if (cinfo->ret.nregs == 2) {
			code = mono_riscv_emit_store (code, cinfo->ret.reg + 1, RISCV_T1, sizeof (host_mgreg_t), 0);
		}
		break;
	}
	case ArgHFA: {
		MonoInst *loc = cfg->arch.vret_addr_loc;
		int i;

		/* Load the destination address */
		g_assert (loc && loc->opcode == OP_REGOFFSET);
		code = mono_riscv_emit_load (code, RISCV_T1, loc->inst_basereg, GTMREG_TO_INT (loc->inst_offset), 0);
		for (i = 0; i < cinfo->ret.nregs; ++i) {
			if (cinfo->ret.esize == 4)
				code = mono_riscv_emit_fstore (code, cinfo->ret.reg + i, RISCV_T1, cinfo->ret.foffsets [i], TRUE);
			else
				code = mono_riscv_emit_fstore (code, cinfo->ret.reg + i, RISCV_T1, cinfo->ret.foffsets [i], FALSE);
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

static guint8 *
mono_riscv_emit_call (MonoCompile *cfg, guint8 *code, MonoJumpInfoType patch_type, gconstpointer data)
{
	mono_add_patch_info_rel (cfg, code - cfg->native_code, patch_type, data, MONO_R_RISCV_JAL);
	// only used as a placeholder
	riscv_jal (code, RISCV_RA, 0);
	cfg->thunk_area += THUNK_SIZE;
	return code;
}

/* This clobbers RA */
static guint8 *
mono_riscv_emit_branch_exc (MonoCompile *cfg, guint8 *code, int opcode, int sreg1, int sreg2, const char *exc_name)
{
	riscv_auipc (code, MONO_ARCH_EXC_ADDR_REG, 0);
	riscv_addi (code, MONO_ARCH_EXC_ADDR_REG, MONO_ARCH_EXC_ADDR_REG, 8);
	switch (opcode) {
	case OP_RISCV_EXC_BEQ:
		riscv_bne (code, sreg1, sreg2, 8);
		break;
	case OP_RISCV_EXC_BNE:
		riscv_beq (code, sreg1, sreg2, 8);
		break;
	case OP_RISCV_EXC_BLT:
		riscv_bge (code, sreg1, sreg2, 8);
		break;
	case OP_RISCV_EXC_BLTU:
		riscv_bgeu (code, sreg1, sreg2, 8);
		break;
	case OP_RISCV_EXC_BGEU:
		riscv_bltu (code, sreg1, sreg2, 8);
		break;
	default:
		g_print ("can't emit exc branch %d\n", opcode);
		NOT_IMPLEMENTED;
	}
	mono_add_patch_info_rel (cfg, code - cfg->native_code, MONO_PATCH_INFO_EXC, exc_name, MONO_R_RISCV_JAL);
	cfg->thunk_area += THUNK_SIZE;
	riscv_jal (code, RISCV_ZERO, 0);
	return code;
}

/*
 * Stack frame layout:
 *  |--------------------------| -- <-- sp + stack_size (FP)
 *  | saved return value	   |
 *  |--------------------------|
 * 	| saved FP reg			   |
 *  |--------------------------|
 *  | callee saved regs		   |
 *  |--------------------------|
 *  | param area			   |
 *  |--------------------------|
 * 	| MonoLMF structure		   |
 *  |--------------------------|
 *  | realignment			   |
 *  |--------------------------| -- <-- sp
 */
guint8 *
mono_arch_emit_prolog (MonoCompile *cfg)
{
	guint8 *code;

	cfg->code_size = MAX (cfg->header->code_size * 4, 1024);
	code = cfg->native_code = g_malloc (cfg->code_size);

	/* realigned */
	cfg->stack_offset = ALIGN_TO (cfg->stack_offset, MONO_ARCH_FRAME_ALIGNMENT);

	/*
	 * - Setup frame
	 */
	int stack_size = 0;
	mono_emit_unwind_op_def_cfa (cfg, code, RISCV_SP, 0);

	/* Setup frame */
	if (RISCV_VALID_I_IMM (-cfg->stack_offset)) {
		riscv_addi (code, RISCV_SP, RISCV_SP, -cfg->stack_offset);
		mono_emit_unwind_op_def_cfa_offset (cfg, code, cfg->stack_offset);
		// save return value
		stack_size += sizeof (target_mgreg_t);
		code = mono_riscv_emit_store (code, RISCV_RA, RISCV_SP, cfg->stack_offset - stack_size, 0);
		// save s0(fp) value
		stack_size += sizeof (target_mgreg_t);
		code = mono_riscv_emit_store (code, RISCV_FP, RISCV_SP, cfg->stack_offset - stack_size, 0);

		mono_emit_unwind_op_offset (cfg, code, RISCV_RA, -(int)sizeof (target_mgreg_t));
		mono_emit_unwind_op_offset (cfg, code, RISCV_FP, -(int)(sizeof (target_mgreg_t) * 2));

		// set s0(fp) value
		riscv_addi (code, RISCV_FP, RISCV_SP, cfg->stack_offset);
	} else {
		// save stack size into T0
		code = mono_riscv_emit_imm (code, RISCV_T0, cfg->stack_offset);
		// calculate SP
		riscv_sub (code, RISCV_SP, RISCV_SP, RISCV_T0);
		mono_emit_unwind_op_def_cfa_offset (cfg, code, cfg->stack_offset);

		// save return value
		stack_size += sizeof (target_mgreg_t);
		code = mono_riscv_emit_store (code, RISCV_RA, RISCV_SP, cfg->stack_offset - stack_size, 0);
		// save s0(fp) value
		stack_size += sizeof (target_mgreg_t);
		code = mono_riscv_emit_store (code, RISCV_FP, RISCV_SP, cfg->stack_offset - stack_size, 0);

		mono_emit_unwind_op_offset (cfg, code, RISCV_RA, -(int)sizeof (target_mgreg_t));
		mono_emit_unwind_op_offset (cfg, code, RISCV_FP, -(int)(sizeof (target_mgreg_t) * 2));

		// set s0(fp) value
		code = mono_riscv_emit_imm (code, RISCV_T0, cfg->stack_offset);
		riscv_add (code, RISCV_FP, RISCV_SP, RISCV_T0);
	}
	mono_emit_unwind_op_def_cfa (cfg, code, RISCV_FP, 0);

	// save other registers
	if (cfg->param_area)
		/* The param area is below the stack pointer */
		riscv_addi (code, RISCV_SP, RISCV_SP, -cfg->param_area);

	if (cfg->method->save_lmf) {
		g_assert (cfg->lmf_var->inst_offset <= 0);
		code = emit_setup_lmf (cfg, code, cfg->lmf_var->inst_offset);
	} else
		/* Save gregs */
		code = emit_store_stack_cfa (cfg, code, MONO_ARCH_CALLEE_SAVED_REGS & cfg->used_int_regs, RISCV_FP,
		                                    -cfg->arch.saved_gregs_offset, FALSE);

	/* Save address of return value received in A0*/
	if (cfg->vret_addr) {
		MonoInst *ins = cfg->vret_addr;

		g_assert (ins->opcode == OP_REGOFFSET);
		code = mono_riscv_emit_store (code, cfg->arch.cinfo->ret.reg, ins->inst_basereg, ins->inst_offset, 0);
	}

	/* Save mrgctx received in MONO_ARCH_RGCTX_REG */
	if (cfg->rgctx_var) {
		MonoInst *ins = cfg->rgctx_var;

		g_assert (ins->opcode == OP_REGOFFSET);

		code = mono_riscv_emit_store (code, MONO_ARCH_RGCTX_REG, ins->inst_basereg, ins->inst_offset, 0);

		mono_add_var_location (cfg, cfg->rgctx_var, TRUE, MONO_ARCH_RGCTX_REG, 0, 0, code - cfg->native_code);
		mono_add_var_location (cfg, cfg->rgctx_var, FALSE, ins->inst_basereg, ins->inst_offset, code - cfg->native_code,
		                       0);
	}

	/*
	 * Move arguments to their registers/stack locations.
	 */
	code = emit_move_args (cfg, code);

	/* Initialize seq_point_info_var */
	if (cfg->arch.seq_point_info_var)
		NOT_IMPLEMENTED;
	else {
		MonoInst *ins;
		if (cfg->arch.ss_tramp_var) {
			/* Initialize ss_tramp_var */
			ins = cfg->arch.ss_tramp_var;
			g_assert (ins->opcode == OP_REGOFFSET);

			code = mono_riscv_emit_imm (code, RISCV_T1, (guint64)&ss_trampoline);
			code = mono_riscv_emit_store (code, RISCV_T1, ins->inst_basereg, ins->inst_offset, 0);
		}
		if (cfg->arch.bp_tramp_var) {
			/* Initialize bp_tramp_var */
			ins = cfg->arch.bp_tramp_var;
			g_assert (ins->opcode == OP_REGOFFSET);
			code = mono_riscv_emit_imm (code, RISCV_T1, (guint64)bp_trampoline);
			code = mono_riscv_emit_store (code, RISCV_T1, ins->inst_basereg, ins->inst_offset, 0);
		}
	}

	return code;
}

void
mono_arch_emit_epilog (MonoCompile *cfg)
{
	guint8 *code = NULL;
	CallInfo *cinfo;
	// MonoMethod *method = cfg->method;
	int max_epilog_size = 16 + 20 * 4;
	// int alloc2_size = 0;

	code = realloc_code (cfg, max_epilog_size);

	if (cfg->method->save_lmf) {
		code = mono_riscv_emit_load_regarray (code, MONO_ARCH_CALLEE_SAVED_REGS & cfg->used_int_regs, RISCV_FP,
		                                      cfg->lmf_var->inst_offset + MONO_STRUCT_OFFSET (MonoLMF, gregs), FALSE);
	} else {
		/* Restore gregs */
		code = mono_riscv_emit_load_stack (code, MONO_ARCH_CALLEE_SAVED_REGS & cfg->used_int_regs, RISCV_FP,
		                                   -cfg->arch.saved_gregs_offset, FALSE);
	}

	/* Load returned vtypes into registers if needed */
	cinfo = cfg->arch.cinfo;
	switch (cinfo->ret.storage) {
	case ArgNone:
	case ArgInIReg:
	case ArgInFReg:
	case ArgInFRegR4:
	case ArgVtypeByRef:
		break;
	case ArgVtypeInIReg: {
		MonoInst *ins = cfg->ret;

		g_assert (cinfo->ret.nregs <= 2);
		if (cinfo->ret.nregs == 2)
			code = mono_riscv_emit_load (code, cinfo->ret.reg + 1, ins->inst_basereg,
			                             ins->inst_offset + sizeof (host_mgreg_t), 0);
		code = mono_riscv_emit_load (code, cinfo->ret.reg, ins->inst_basereg, ins->inst_offset, 0);
		break;
	}
	case ArgHFA: {
		MonoInst *ins = cfg->ret;

		for (int i = 0; i < cinfo->ret.nregs; ++i) {
			if (cinfo->ret.esize == 4)
				code = mono_riscv_emit_fload (code, cinfo->ret.reg + i, ins->inst_basereg,
				                              GTMREG_TO_INT (ins->inst_offset + cinfo->ret.foffsets [i]), TRUE);
			else
				code = mono_riscv_emit_fload (code, cinfo->ret.reg + i, ins->inst_basereg,
				                              GTMREG_TO_INT (ins->inst_offset + cinfo->ret.foffsets [i]), FALSE);
		}
		break;
	}
	default:
		g_print ("Unable process returned storage %d(0x%x)\n", cinfo->ret.storage, cinfo->ret.storage);
		NOT_IMPLEMENTED;
	}

	/* Destroy frame */
	code = mono_riscv_emit_destroy_frame (code);

	riscv_jalr (code, RISCV_X0, RISCV_RA, 0);

	g_assert (code - (cfg->native_code + cfg->code_len) < max_epilog_size);
	set_code_cursor (cfg, code);
}

void
mono_arch_output_basic_block (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins;
	MonoCallInst *call;
	guint8 *code = cfg->native_code + cfg->code_len;
	int start_offset, max_len;

	start_offset = code - cfg->native_code;
	g_assert (start_offset <= cfg->code_size);

	if (cfg->verbose_level > 2) {
		g_print ("Basic block %d starting at offset 0x%x\n", bb->block_num, bb->native_offset);
		MONO_BB_FOR_EACH_INS (bb, ins)
		{
			mono_print_ins (ins);
		}
	}

	MONO_BB_FOR_EACH_INS (bb, ins)
	{
		guint offset = code - cfg->native_code;
		set_code_cursor (cfg, code);
		max_len = ins_get_size (ins->opcode);
		code = realloc_code (cfg, max_len);

		mono_debug_record_line_number (cfg, ins, offset);

		/* Check for virtual regs that snuck by */
		g_assert ((ins->dreg >= -1) && (ins->dreg < 32));

		switch (ins->opcode) {
		case OP_NOT_REACHED:
		case OP_NOT_NULL:
		case OP_DUMMY_USE:
			break;

		/* VM Related OP */
		case OP_CHECK_THIS:
			//  try to load 1 byte from ins->sreg to check it is null pionter
			code = mono_riscv_emit_load (code, RISCV_RA, ins->sreg1, 0, 1);
			break;
		case OP_GET_EX_OBJ:
			if (ins->dreg != RISCV_A0)
				// mv dreg, RISCV_A0
				riscv_addi (code, ins->dreg, RISCV_A0, 0);
			break;
		case OP_IL_SEQ_POINT:
			mono_add_seq_point (cfg, bb, ins, code - cfg->native_code);
			break;
		case OP_SEQ_POINT: {
			if (ins->flags & MONO_INST_SINGLE_STEP_LOC) {
				MonoInst *var = cfg->arch.ss_tramp_var;

				g_assert (var);
				g_assert (var->opcode == OP_REGOFFSET);
				/* Load ss_tramp_var */
				/* This is equal to &ss_trampoline */
				code = mono_riscv_emit_load (code, RISCV_T1, var->inst_basereg, var->inst_offset, 0);
				/* Load the trampoline address */
				code = mono_riscv_emit_load (code, RISCV_T1, RISCV_T1, 0, 0);
				/* Call it if it is non-null */
				// In riscv, we use jalr to jump
				riscv_beq (code, RISCV_ZERO, RISCV_T1, 8);
				riscv_jalr (code, RISCV_ZERO, RISCV_T1, 0);
			}
			mono_add_seq_point (cfg, bb, ins, code - cfg->native_code);

			if (cfg->compile_aot)
				NOT_IMPLEMENTED;
			else {
				MonoInst *var = cfg->arch.bp_tramp_var;
				g_assert (var);
				g_assert (var->opcode == OP_REGOFFSET);
				/* Load the address of the bp trampoline into T1 */
				code = mono_riscv_emit_load (code, RISCV_T1, var->inst_basereg, var->inst_offset, 0);
				/*
				 * A placeholder for a possible breakpoint inserted by
				 * mono_arch_set_breakpoint ().
				 */
				code = mono_riscv_emit_nop (code);
			}
			break;
		}
		case OP_LOCALLOC: {
			g_assert (MONO_ARCH_FRAME_ALIGNMENT == 16);
			guint8 *loop_start;
			// ins->sreg1 stores the size of new object.
			// 1. Align the object size to MONO_ARCH_FRAME_ALIGNMENT
			// RISCV_T0 = ALIGN_TO(ins->sreg1, MONO_ARCH_FRAME_ALIGNMENT)
			riscv_addi (code, RISCV_T0, ins->sreg1, (MONO_ARCH_FRAME_ALIGNMENT - 1));
			code = mono_riscv_emit_imm (code, RISCV_T1, -MONO_ARCH_FRAME_ALIGNMENT);
			riscv_and (code, RISCV_T0, RISCV_T0, RISCV_T1);

			// 2. extend sp for Object
			riscv_addi (code, RISCV_T1, RISCV_SP, 0);
			riscv_sub (code, RISCV_SP, RISCV_T1, RISCV_T0);

			// 3.
			/* Init */
			/* T0 = pointer, T1 = end */
			riscv_addi (code, RISCV_T0, RISCV_SP, 0);
			loop_start = code;
			riscv_beq (code, RISCV_T0, RISCV_T1, 0);
			riscv_sd (code, RISCV_ZERO, RISCV_T0, 0);
			riscv_sd (code, RISCV_ZERO, RISCV_T0, sizeof (host_mgreg_t));
#ifdef TARGET_RISCV32
			NOT_IMPLEMENTED;
			code = mono_riscv_emit_store (code, RISCV_ZERO, RISCV_T0, sizeof (host_mgreg_t) * 2, 0);
			code = mono_riscv_emit_store (code, RISCV_ZERO, RISCV_T0, sizeof (host_mgreg_t) * 3, 0);
#endif
			riscv_addi (code, RISCV_T0, RISCV_T0, MONO_ARCH_FRAME_ALIGNMENT);
			riscv_jal (code, RISCV_ZERO, riscv_get_jal_disp (code, loop_start));
			riscv_patch_rel (loop_start, code, MONO_R_RISCV_BEQ);

			riscv_addi (code, ins->dreg, RISCV_SP, 0);
			if (cfg->param_area) {
				g_assert (cfg->param_area > 0);
				riscv_addi (code, RISCV_SP, RISCV_SP, -cfg->param_area);
			}
			break;
		}
		case OP_LOCALLOC_IMM: {
			int aligned_imm, aligned_imm_offset;
			aligned_imm = ALIGN_TO (ins->inst_imm, MONO_ARCH_FRAME_ALIGNMENT);
			g_assert (RISCV_VALID_I_IMM (aligned_imm));
			riscv_addi (code, RISCV_SP, RISCV_SP, -aligned_imm);

			/* Init */
			g_assert (MONO_ARCH_FRAME_ALIGNMENT == 16);
			aligned_imm_offset = 0;
			while (aligned_imm_offset < aligned_imm) {
				code = mono_riscv_emit_store (code, RISCV_ZERO, RISCV_SP, aligned_imm_offset + 0, 0);
				code =
				    mono_riscv_emit_store (code, RISCV_ZERO, RISCV_SP, aligned_imm_offset + sizeof (host_mgreg_t), 0),
				0;
#ifdef TARGET_RISCV32
				code = mono_riscv_emit_store (code, RISCV_ZERO, RISCV_SP,
				                              aligned_imm_offset + sizeof (host_mgreg_t) * 2, 0);
				code = mono_riscv_emit_store (code, RISCV_ZERO, RISCV_SP,
				                              aligned_imm_offset + sizeof (host_mgreg_t) * 3, 0);
#endif
				aligned_imm_offset += 16;
			}

			riscv_addi (code, ins->dreg, RISCV_SP, 0);
			if (cfg->param_area) {
				g_assert (cfg->param_area > 0);
				riscv_addi (code, RISCV_SP, RISCV_SP, -cfg->param_area);
			}
			break;
		}
		case OP_GENERIC_CLASS_INIT: {
			int byte_offset;
			guint8 *branch_label;

			byte_offset = MONO_STRUCT_OFFSET (MonoVTable, initialized);

			/* Load vtable->initialized */
			code = mono_riscv_emit_load (code, RISCV_T1, ins->sreg1, byte_offset, 1);
			branch_label = code;
			riscv_bne (code, RISCV_ZERO, RISCV_T1, 0);

			/* Slowpath */
			g_assert (ins->sreg1 == RISCV_A0);
			code = mono_riscv_emit_call (cfg, code, MONO_PATCH_INFO_JIT_ICALL_ID,
			                             GUINT_TO_POINTER (MONO_JIT_ICALL_mono_generic_class_init));

			mono_riscv_patch (branch_label, code, MONO_R_RISCV_BNE);
			break;
		}
		case OP_CKFINITE: {
			g_assert (riscv_stdext_d || riscv_stdext_f);
			/* Check for infinity and nans */
			if (riscv_stdext_d)
				riscv_fclass_d (code, RISCV_T0, ins->sreg1);
			else
				riscv_fclass_s (code, RISCV_T0, ins->sreg1);

			riscv_andi (code, RISCV_T0, RISCV_T0, ~(RISCV_FCLASS_INF | RISCV_FCLASS_NAN));
			code =
			    mono_riscv_emit_branch_exc (cfg, code, OP_RISCV_EXC_BEQ, RISCV_T0, RISCV_ZERO, "ArithmeticException");
			if (ins->dreg != ins->sreg1) {
				if (riscv_stdext_d)
					riscv_fsgnj_d (code, ins->dreg, ins->sreg1, ins->sreg1);
				else
					riscv_fsgnj_s (code, ins->dreg, ins->sreg1, ins->sreg1);
			}
		}
		case OP_BREAK:
			/*
			 * gdb does not like encountering the hw breakpoint ins in the debugged code.
			 * So instead of emitting a trap, we emit a call a C function and place a
			 * breakpoint there.
			 */
			code = mono_riscv_emit_call (cfg, code, MONO_PATCH_INFO_JIT_ICALL_ID,
			                             GUINT_TO_POINTER (MONO_JIT_ICALL_mono_break));
			break;
		case OP_ARGLIST:
			g_assert (cfg->arch.cinfo);
			riscv_addi (code, RISCV_T1, RISCV_FP, cfg->arch.cinfo->sig_cookie.offset);
			code = mono_riscv_emit_store (code, RISCV_T1, ins->sreg1, 0, 0);
			break;

		case OP_NOP:
		case OP_RELAXED_NOP:
			// code = mono_riscv_emit_nop (code);
			break;
		case OP_MOVE:
#ifdef TARGET_RISCV64
		case OP_LMOVE:
#endif
			// mv ra, a1 -> addi ra, a1, 0
			riscv_addi (code, ins->dreg, ins->sreg1, 0);
			break;
		// r4 move
		case OP_RMOVE:
			g_assert (riscv_stdext_f);
			riscv_fsgnj_s (code, ins->dreg, ins->sreg1, ins->sreg1);
			break;
		case OP_FMOVE:
			// fmv.{s|d} rd, rs1 -> fsgnj.{s|d} rd, rs1, rs1
			g_assert (riscv_stdext_d || riscv_stdext_f);
			if (riscv_stdext_d)
				riscv_fsgnj_d (code, ins->dreg, ins->sreg1, ins->sreg1);
			else
				riscv_fsgnj_s (code, ins->dreg, ins->sreg1, ins->sreg1);
			break;
		case OP_MOVE_F_TO_I4:
			g_assert (riscv_stdext_f);
			riscv_fmv_x_w (code, ins->dreg, ins->sreg1);
			break;
		case OP_MOVE_I4_TO_F:
			g_assert (riscv_stdext_f);
			riscv_fmv_w_x (code, ins->dreg, ins->sreg1);
			break;
#ifdef TARGET_RISCV64
		case OP_MOVE_F_TO_I8:
			g_assert (riscv_stdext_d);
			riscv_fmv_x_d (code, ins->dreg, ins->sreg1);
			break;
		case OP_MOVE_I8_TO_F:
			g_assert (riscv_stdext_d);
			riscv_fmv_d_x (code, ins->dreg, ins->sreg1);
			break;
#endif
		case OP_LOAD_MEMBASE:
			code = mono_riscv_emit_load (code, ins->dreg, ins->sreg1, ins->inst_offset, 0);
			break;
		case OP_LOADI1_MEMBASE:
			code = mono_riscv_emit_load (code, ins->dreg, ins->sreg1, ins->inst_offset, 1);
			break;
		case OP_LOADU1_MEMBASE:
			code = mono_riscv_emit_loadu (code, ins->dreg, ins->sreg1, ins->inst_offset, 1);
			break;
		case OP_LOADI2_MEMBASE:
			code = mono_riscv_emit_load (code, ins->dreg, ins->sreg1, ins->inst_offset, 2);
			break;
		case OP_LOADU2_MEMBASE:
			code = mono_riscv_emit_loadu (code, ins->dreg, ins->sreg1, ins->inst_offset, 2);
			break;
		case OP_LOADI4_MEMBASE:
			code = mono_riscv_emit_load (code, ins->dreg, ins->sreg1, ins->inst_offset, 4);
			break;
#ifdef TARGET_RISCV64
		case OP_LOADU4_MEMBASE:
			code = mono_riscv_emit_loadu (code, ins->dreg, ins->sreg1, ins->inst_offset, 4);
			break;
		case OP_LOADI8_MEMBASE:
			code = mono_riscv_emit_load (code, ins->dreg, ins->sreg1, ins->inst_offset, 8);
			break;
#endif
		case OP_STORE_MEMBASE_REG:
			code = mono_riscv_emit_store (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset, 0);
			break;
		case OP_STOREI1_MEMBASE_REG:
			code = mono_riscv_emit_store (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset, 1);
			break;
		case OP_STOREI2_MEMBASE_REG:
			code = mono_riscv_emit_store (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset, 2);
			break;
		case OP_STOREI4_MEMBASE_REG:
			code = mono_riscv_emit_store (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset, 4);
			break;
		case OP_STOREI8_MEMBASE_REG:
			code = mono_riscv_emit_store (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset, 8);
			break;
		case OP_ICONST:
			code = mono_riscv_emit_imm (code, ins->dreg, (int)ins->inst_c0);
			break;
		case OP_I8CONST:
			code = mono_riscv_emit_imm (code, ins->dreg, ins->inst_l);
			break;
		case OP_IADD:
#ifdef TARGET_RISCV64
			riscv_addw (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
#endif
		case OP_LADD:
			riscv_add (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_IADD_IMM:
#ifdef TARGET_RISCV64
			riscv_addiw (code, ins->dreg, ins->sreg1, ins->inst_imm);
			break;
#endif
		case OP_ADD_IMM:
		case OP_LADD_IMM:
			riscv_addi (code, ins->dreg, ins->sreg1, ins->inst_imm);
			break;
		case OP_RADD:
			g_assert (riscv_stdext_f);
			riscv_fadd_s (code, RISCV_ROUND_DY, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_FADD:
			g_assert (riscv_stdext_f || riscv_stdext_d);
			if (riscv_stdext_d)
				riscv_fadd_d (code, RISCV_ROUND_DY, ins->dreg, ins->sreg1, ins->sreg2);
			else {
				NOT_IMPLEMENTED;
				riscv_fadd_s (code, RISCV_ROUND_DY, ins->dreg, ins->sreg1, ins->sreg2);
			}
			break;
		case OP_ISUB:
#ifdef TARGET_RISCV64
			riscv_subw (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
#endif
		case OP_LSUB:
			riscv_sub (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_FSUB:
			g_assert (riscv_stdext_f || riscv_stdext_d);
			if (riscv_stdext_d)
				riscv_fsub_d (code, RISCV_ROUND_DY, ins->dreg, ins->sreg1, ins->sreg2);
			else {
				NOT_IMPLEMENTED;
				riscv_fsub_s (code, RISCV_ROUND_DY, ins->dreg, ins->sreg1, ins->sreg2);
			}
			break;
		case OP_RSUB:
			g_assert (riscv_stdext_f);
			riscv_fsub_s (code, RISCV_ROUND_DY, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_RNEG:
			g_assert (riscv_stdext_f);
			riscv_fsgnjn_s (code, ins->dreg, ins->sreg1, ins->sreg1);
			break;
		case OP_FNEG:
			g_assert (riscv_stdext_f || riscv_stdext_d);
			if (riscv_stdext_d)
				riscv_fsgnjn_d (code, ins->dreg, ins->sreg1, ins->sreg1);
			else {
				NOT_IMPLEMENTED;
				riscv_fsgnjn_s (code, ins->dreg, ins->sreg1, ins->sreg1);
			}
			break;
			break;
		case OP_IMUL:
#ifdef TARGET_RISCV64
			g_assert (riscv_stdext_m);
			riscv_mulw (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_LMUL:
#endif
			g_assert (riscv_stdext_m);
			riscv_mul (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_RMUL:
			g_assert (riscv_stdext_f);
			riscv_fmul_s (code, RISCV_ROUND_DY, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_FMUL:
			g_assert (riscv_stdext_f || riscv_stdext_d);
			if (riscv_stdext_d)
				riscv_fmul_d (code, RISCV_ROUND_DY, ins->dreg, ins->sreg1, ins->sreg2);
			else
				NOT_IMPLEMENTED;
			break;
		case OP_FDIV:
			g_assert (riscv_stdext_f || riscv_stdext_d);
			if (riscv_stdext_d) {
				riscv_fdiv_d (code, RISCV_ROUND_DY, ins->dreg, ins->sreg1, ins->sreg2);
			} else {
				NOT_IMPLEMENTED;
				riscv_fdiv_s (code, RISCV_ROUND_DY, ins->dreg, ins->sreg1, ins->sreg2);
			}
			break;
		case OP_IREM:
		case OP_IDIV: {
			g_assert (riscv_stdext_m);
			/* Check for zero */
			code = mono_riscv_emit_branch_exc (cfg, code, OP_RISCV_EXC_BEQ, ins->sreg2, RISCV_ZERO,
			                                   "DivideByZeroException");
			/* Check for INT64_MIN/-1 */
#ifdef TARGET_RISCV64
			code = mono_riscv_emit_imm (code, RISCV_T0, 0xffffffff80000000);
#else
			code = mono_riscv_emit_imm (code, RISCV_T0, 0x80000000);
#endif
			// compare t0, rs1; ceq rd => xor t0, t0, rs1; sltiu t0, t0, 1
			riscv_xor (code, RISCV_T0, RISCV_T0, ins->sreg1);
			riscv_sltiu (code, RISCV_T1, RISCV_T0, 1);
#ifdef TARGET_RISCV64
			code = mono_riscv_emit_imm (code, RISCV_T0, 0xffffffffffffffff);
#else
			code = mono_riscv_emit_imm (code, RISCV_T0, 0xffffffff);
#endif
			riscv_xor (code, RISCV_T0, RISCV_T0, ins->sreg2);
			riscv_sltiu (code, RISCV_T0, RISCV_T0, 1);
			riscv_and (code, RISCV_T0, RISCV_T0, RISCV_T1);
			riscv_addi (code, RISCV_T0, RISCV_T0, -1);
			code = mono_riscv_emit_branch_exc (cfg, code, OP_RISCV_EXC_BEQ, RISCV_T0, RISCV_ZERO, "OverflowException");
			if (ins->opcode == OP_IREM)
#ifdef TARGET_RISCV64
				riscv_remw (code, ins->dreg, ins->sreg1, ins->sreg2);
#else
				riscv_rem (code, ins->dreg, ins->sreg1, ins->sreg2);
#endif
			else
				riscv_div (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		}
		case OP_LREM:
		case OP_LDIV: {
			g_assert (riscv_stdext_m);
			/* Check for zero */
			code = mono_riscv_emit_branch_exc (cfg, code, OP_RISCV_EXC_BEQ, ins->sreg2, RISCV_ZERO,
			                                   "DivideByZeroException");
			/* Check for INT64_MIN/-1 */
			code = mono_riscv_emit_imm (code, RISCV_T0, 0x8000000000000000);
			// compare t0, rs1; ceq rd => xor t0, t0, rs1; sltiu t0, t0, 1
			riscv_xor (code, RISCV_T0, RISCV_T0, ins->sreg1);
			riscv_sltiu (code, RISCV_T1, RISCV_T0, 1);
			code = mono_riscv_emit_imm (code, RISCV_T0, 0xffffffffffffffff);
			riscv_xor (code, RISCV_T0, RISCV_T0, ins->sreg2);
			riscv_sltiu (code, RISCV_T0, RISCV_T0, 1);
			riscv_and (code, RISCV_T0, RISCV_T0, RISCV_T1);
			riscv_addi (code, RISCV_T0, RISCV_T0, -1);
			code = mono_riscv_emit_branch_exc (cfg, code, OP_RISCV_EXC_BEQ, RISCV_T0, RISCV_ZERO, "OverflowException");
			if (ins->opcode == OP_LREM)
				riscv_rem (code, ins->dreg, ins->sreg1, ins->sreg2);
			else
				riscv_div (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		}
		case OP_IDIV_UN:
#ifdef TARGET_RISCV64
			g_assert (riscv_stdext_m);
			code = mono_riscv_emit_branch_exc (cfg, code, OP_RISCV_EXC_BEQ, ins->sreg2, RISCV_ZERO,
			                                   "DivideByZeroException");
			riscv_divuw (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
#endif
		case OP_LDIV_UN:
			g_assert (riscv_stdext_m);
			code = mono_riscv_emit_branch_exc (cfg, code, OP_RISCV_EXC_BEQ, ins->sreg2, RISCV_ZERO,
			                                   "DivideByZeroException");
			riscv_divu (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_IREM_UN:
#ifdef TARGET_RISCV64
			g_assert (riscv_stdext_m);
			code = mono_riscv_emit_branch_exc (cfg, code, OP_RISCV_EXC_BEQ, ins->sreg2, RISCV_ZERO,
			                                   "DivideByZeroException");
			riscv_remuw (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
#endif
		case OP_LREM_UN:
			g_assert (riscv_stdext_m);
			code = mono_riscv_emit_branch_exc (cfg, code, OP_RISCV_EXC_BEQ, ins->sreg2, RISCV_ZERO,
			                                   "DivideByZeroException");
			riscv_remu (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;

		/* Bit/logic */
		case OP_IAND:
		case OP_LAND:
			riscv_and (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_AND_IMM:
		case OP_IAND_IMM:
		case OP_LAND_IMM:
			riscv_andi (code, ins->dreg, ins->sreg1, ins->inst_imm);
			break;
		case OP_IXOR:
		case OP_LXOR:
			riscv_xor (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_XOR_IMM:
		case OP_IXOR_IMM:
		case OP_LXOR_IMM:
			riscv_xori (code, ins->dreg, ins->sreg1, ins->inst_imm);
			break;
		case OP_IOR:
		case OP_LOR:
			riscv_or (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_IOR_IMM:
		case OP_LOR_IMM:
			riscv_ori (code, ins->dreg, ins->sreg1, ins->inst_imm);
			break;
		case OP_RISCV_SLT:
			riscv_slt (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_RISCV_SLTU:
			riscv_sltu (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_RISCV_SLTI:
			riscv_slti (code, ins->dreg, ins->sreg1, ins->inst_imm);
			break;
		case OP_RISCV_SLTIU:
			riscv_sltiu (code, ins->dreg, ins->sreg1, ins->inst_imm);
			break;
		case OP_ISHR:
#ifdef TARGET_RISCV64
			riscv_sraw (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_LSHR:
#endif
			riscv_sra (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_ISHR_UN:
#ifdef TARGET_RISCV64
			riscv_srlw (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_LSHR_UN:
#endif
			riscv_srl (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_ISHR_UN_IMM:
#ifdef TARGET_RISCV64
			riscv_srliw (code, ins->dreg, ins->sreg1, ins->inst_imm);
			break;
		case OP_LSHR_UN_IMM:
#endif
		case OP_SHR_UN_IMM:
			riscv_srli (code, ins->dreg, ins->sreg1, ins->inst_imm);
			break;
		case OP_ISHR_IMM:
#ifdef TARGET_RISCV64
			riscv_sraiw (code, ins->dreg, ins->sreg1, ins->inst_imm);
			break;
		case OP_LSHR_IMM:
#endif
		case OP_SHR_IMM:
			riscv_srai (code, ins->dreg, ins->sreg1, ins->inst_imm);
			break;
		case OP_ISHL:
#ifdef TARGET_RISCV64
			riscv_sllw (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_LSHL:
#endif
			riscv_sll (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_ISHL_IMM:
#ifdef TARGET_RISCV64
			riscv_slliw (code, ins->dreg, ins->sreg1, ins->inst_imm);
			break;
		case OP_LSHL_IMM:
#endif
		case OP_SHL_IMM:
			riscv_slli (code, ins->dreg, ins->sreg1, ins->inst_imm);
			break;
#if defined(TARGET_RISCV64)
		case OP_RISCV_ADDIW:
			riscv_addiw (code, ins->dreg, ins->sreg1, 0);
			break;
		case OP_RISCV_ADDUW:
			// TODO: Add inst riscv_adduw in riscv-codegen.h
			NOT_IMPLEMENTED;
			break;
#endif

		/* Atomic */
		case OP_MEMORY_BARRIER:
			riscv_fence (code, mono_arch_get_memory_ordering (ins->backend.memory_barrier_kind),
			             mono_arch_get_memory_ordering (ins->backend.memory_barrier_kind));
			break;
		/**
		 * OP_ATOMIC_ADD_I4 rd, rs1, rs2
		 * this instriuction increase the value of address rs1 by rs2
		 * and store the **new** value to rd.
		 * But in RISC-V amoadd rd, rs2(rs1) increase the value of address rs1 by rs2
		 * and store the **old** value to rd,  store the result value to address rs1.
		 * So we need more add rd, rd, rs2 to fix the rd as the **new** value.
		 */
		case OP_ATOMIC_ADD_I4:
			riscv_amoadd_w (code, RISCV_ORDER_ALL, RISCV_T0, ins->sreg2, ins->sreg1);
			riscv_addw (code, ins->dreg, RISCV_T0, ins->sreg2);
			break;
		case OP_ATOMIC_ADD_I8:
			riscv_amoadd_d (code, RISCV_ORDER_ALL, RISCV_T0, ins->sreg2, ins->sreg1);
			riscv_add (code, ins->dreg, RISCV_T0, ins->sreg2);
			break;
		case OP_ATOMIC_LOAD_I1: {
			riscv_fence (code, RISCV_FENCE_MEM, RISCV_FENCE_MEM);
			code = mono_riscv_emit_load (code, ins->dreg, ins->sreg1, ins->inst_offset, 1);
			riscv_fence (code, RISCV_FENCE_R, RISCV_FENCE_MEM);
			break;
		}
		case OP_ATOMIC_LOAD_U1: {
			riscv_fence (code, RISCV_FENCE_MEM, RISCV_FENCE_MEM);
			code = mono_riscv_emit_loadu (code, ins->dreg, ins->sreg1, ins->inst_offset, 1);
			riscv_fence (code, RISCV_FENCE_R, RISCV_FENCE_MEM);
			break;
		}
		case OP_ATOMIC_LOAD_U2: {
			riscv_fence (code, RISCV_FENCE_MEM, RISCV_FENCE_MEM);
			code = mono_riscv_emit_loadu (code, ins->dreg, ins->sreg1, ins->inst_offset, 2);
			riscv_fence (code, RISCV_FENCE_R, RISCV_FENCE_MEM);
			break;
		}
		case OP_ATOMIC_LOAD_I2: {
			riscv_fence (code, RISCV_FENCE_MEM, RISCV_FENCE_MEM);
			code = mono_riscv_emit_load (code, ins->dreg, ins->sreg1, ins->inst_offset, 2);
			riscv_fence (code, RISCV_FENCE_R, RISCV_FENCE_MEM);
			break;
		}
		case OP_ATOMIC_LOAD_I4: {
			riscv_fence (code, RISCV_FENCE_MEM, RISCV_FENCE_MEM);
			code = mono_riscv_emit_load (code, ins->dreg, ins->sreg1, ins->inst_offset, 4);
			riscv_fence (code, RISCV_FENCE_R, RISCV_FENCE_MEM);
			break;
		}
		case OP_ATOMIC_LOAD_U4: {
			riscv_fence (code, RISCV_FENCE_MEM, RISCV_FENCE_MEM);
			code = mono_riscv_emit_loadu (code, ins->dreg, ins->sreg1, ins->inst_offset, 4);
			riscv_fence (code, RISCV_FENCE_R, RISCV_FENCE_MEM);
			break;
		}
		case OP_ATOMIC_STORE_I1:
		case OP_ATOMIC_STORE_U1: {
			riscv_fence (code, RISCV_FENCE_MEM, RISCV_FENCE_W);
			code = mono_riscv_emit_store (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset, 1);
			if (ins->backend.memory_barrier_kind == MONO_MEMORY_BARRIER_SEQ)
				riscv_fence (code, RISCV_FENCE_ALL, RISCV_FENCE_ALL);
			break;
		}
		case OP_ATOMIC_STORE_I2:
		case OP_ATOMIC_STORE_U2: {
			riscv_fence (code, RISCV_FENCE_MEM, RISCV_FENCE_W);
			code = mono_riscv_emit_store (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset, 2);
			if (ins->backend.memory_barrier_kind == MONO_MEMORY_BARRIER_SEQ)
				riscv_fence (code, RISCV_FENCE_ALL, RISCV_FENCE_ALL);
			break;
		}
		case OP_ATOMIC_STORE_I4:
		case OP_ATOMIC_STORE_U4: {
			riscv_fence (code, RISCV_FENCE_MEM, RISCV_FENCE_W);
			code = mono_riscv_emit_store (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset, 4);
			if (ins->backend.memory_barrier_kind == MONO_MEMORY_BARRIER_SEQ)
				riscv_fence (code, RISCV_FENCE_ALL, RISCV_FENCE_ALL);
			break;
		}
		case OP_ATOMIC_CAS_I4: {
			g_assert (riscv_stdext_a);
			/**
			 * loop_start:
			 * 	lr.w.aqrl	t0, rs1
			 * 	bne		t0, rs3, loop_end
			 * 	sc.w.rl t1, rs2, rs1
			 * 	bnez t1, loop_start
			 * loop_end:
			 * 	fence rw, rw
			 * 	mv rd, t0
			 *
			 */
			guint8 *loop_start, *branch_label;
			/* sreg2 is the value, sreg3 is the comparand */
			loop_start = code;
			riscv_lr_w (code, RISCV_ORDER_ALL, RISCV_T0, ins->sreg1);
			branch_label = code;
			riscv_bne (code, RISCV_T0, ins->sreg3, 0);
			riscv_sc_w (code, RISCV_ORDER_RL, RISCV_T1, ins->sreg2, ins->sreg1);
			riscv_bne (code, RISCV_ZERO, RISCV_T1, loop_start - code);
			riscv_patch_rel (branch_label, code, MONO_R_RISCV_BNE);

			riscv_fence (code, RISCV_FENCE_MEM, RISCV_FENCE_MEM);
			riscv_addi (code, ins->dreg, RISCV_T0, 0);
			break;
		}
		case OP_ATOMIC_EXCHANGE_I4: {
			g_assert (riscv_stdext_a);
			riscv_amoswap_w (code, RISCV_ORDER_ALL, ins->dreg, ins->sreg2, ins->sreg1);
			break;
		}
#ifdef TARGET_RISCV64
		case OP_ATOMIC_STORE_I8:
		case OP_ATOMIC_STORE_U8: {
			// TODO: Check This
			riscv_fence (code, RISCV_FENCE_MEM, RISCV_FENCE_W);
			code = mono_riscv_emit_store (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset, 8);
			if (ins->backend.memory_barrier_kind == MONO_MEMORY_BARRIER_SEQ)
				riscv_fence (code, RISCV_FENCE_ALL, RISCV_FENCE_ALL);
			break;
		}
		case OP_ATOMIC_LOAD_I8:
		case OP_ATOMIC_LOAD_U8: {
			// TODO: Check This
			riscv_fence (code, RISCV_FENCE_MEM, RISCV_FENCE_MEM);
			code = mono_riscv_emit_load (code, ins->dreg, ins->sreg1, ins->inst_offset, 8);
			riscv_fence (code, RISCV_FENCE_R, RISCV_FENCE_MEM);
			break;
		}
		case OP_ATOMIC_CAS_I8: {
			g_assert (riscv_stdext_a);
			guint8 *loop_start, *branch_label;
			/* sreg2 is the value, sreg3 is the comparand */
			loop_start = code;
			riscv_lr_d (code, RISCV_ORDER_ALL, RISCV_T0, ins->sreg1);
			branch_label = code;
			riscv_bne (code, RISCV_T0, ins->sreg3, 0);
			riscv_sc_d (code, RISCV_ORDER_RL, RISCV_T1, ins->sreg2, ins->sreg1);
			riscv_bne (code, RISCV_ZERO, RISCV_T1, loop_start - code);
			riscv_patch_rel (branch_label, code, MONO_R_RISCV_BNE);

			riscv_fence (code, RISCV_FENCE_MEM, RISCV_FENCE_MEM);
			riscv_addi (code, ins->dreg, RISCV_T0, 0);
			break;
		}
		case OP_ATOMIC_EXCHANGE_I8: {
			g_assert (riscv_stdext_a);
			riscv_amoswap_d (code, RISCV_ORDER_ALL, ins->dreg, ins->sreg2, ins->sreg1);
			break;
		}
#endif

		/* Float */
		case OP_R4CONST:
		case OP_R8CONST: {
			code = mono_riscv_emit_float_imm (code, ins->dreg, *(guint64 *)ins->inst_p0, ins->opcode == OP_R4CONST);
			break;
		}
		case OP_ICONV_TO_R4: {
			g_assert (riscv_stdext_f);
			riscv_fcvt_s_w (code, RISCV_ROUND_DY, ins->dreg, ins->sreg1);
			break;
		}
		case OP_ICONV_TO_R8: {
			g_assert (riscv_stdext_d);
			riscv_fcvt_d_w (code, ins->dreg, ins->sreg1);
			break;
		}
		case OP_RCONV_TO_R8: {
			g_assert (riscv_stdext_d);
			riscv_fcvt_d_s (code, ins->dreg, ins->sreg1);
			break;
		}
		case OP_RCONV_TO_I8: {
			g_assert (riscv_stdext_f);
			riscv_fcvt_l_s (code, RISCV_ROUND_TZ, ins->dreg, ins->sreg1);
			break;
		}
		case OP_FCONV_TO_I8: {
			g_assert (riscv_stdext_f || riscv_stdext_d);
			if (riscv_stdext_d)
				riscv_fcvt_l_d (code, RISCV_ROUND_TZ, ins->dreg, ins->sreg1);
			else
				riscv_fcvt_l_s (code, RISCV_ROUND_TZ, ins->dreg, ins->sreg1);
			break;
		}
		case OP_FCONV_TO_U8: {
			g_assert (riscv_stdext_f || riscv_stdext_d);
			if (riscv_stdext_d)
				riscv_fcvt_lu_d (code, RISCV_ROUND_TZ, ins->dreg, ins->sreg1);
			else
				riscv_fcvt_lu_s (code, RISCV_ROUND_TZ, ins->dreg, ins->sreg1);
			break;
		}
		case OP_RCONV_TO_U8: {
			g_assert (riscv_stdext_f);
			riscv_fcvt_lu_s (code, RISCV_ROUND_TZ, ins->dreg, ins->sreg1);
			break;
		}
		case OP_RCONV_TO_I4: {
			g_assert (riscv_stdext_f);
			riscv_fcvt_w_s (code, RISCV_ROUND_TZ, ins->dreg, ins->sreg1);
			break;
		}
		case OP_RCONV_TO_U4: {
			g_assert (riscv_stdext_f);
			riscv_fcvt_wu_s (code, RISCV_ROUND_TZ, ins->dreg, ins->sreg1);
			break;
		}
		case OP_FCONV_TO_I4: {
			g_assert (riscv_stdext_f || riscv_stdext_d);
			if (riscv_stdext_d)
				riscv_fcvt_w_d (code, RISCV_ROUND_TZ, ins->dreg, ins->sreg1);
			else
				riscv_fcvt_w_s (code, RISCV_ROUND_TZ, ins->dreg, ins->sreg1);
			break;
		}
		case OP_FCONV_TO_U4: {
			g_assert (riscv_stdext_f || riscv_stdext_d);
			if (riscv_stdext_d)
				riscv_fcvt_wu_d (code, RISCV_ROUND_TZ, ins->dreg, ins->sreg1);
			else
				riscv_fcvt_wu_s (code, RISCV_ROUND_TZ, ins->dreg, ins->sreg1);
			break;
		}
		case OP_FCONV_TO_R4:
		case OP_RISCV_SETFREG_R4: {
			g_assert (riscv_stdext_d);
			riscv_fcvt_s_d (code, RISCV_ROUND_DY, ins->dreg, ins->sreg1);
			break;
		}
		case OP_RDIV: {
			g_assert (riscv_stdext_f);
			riscv_fdiv_s (code, RISCV_ROUND_DY, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		}
		case OP_FCEQ: {
			g_assert (riscv_stdext_f || riscv_stdext_d);
			if (riscv_stdext_d)
				riscv_feq_d (code, ins->dreg, ins->sreg1, ins->sreg2);
			else
				riscv_feq_s (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		}
		case OP_RCEQ: {
			g_assert (riscv_stdext_f);
			riscv_feq_s (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		}
		case OP_RCLT:
		case OP_RCLT_UN: {
			g_assert (riscv_stdext_f);
			riscv_flt_s (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		}
		case OP_FCLT:
		case OP_FCLT_UN: {
			g_assert (riscv_stdext_f || riscv_stdext_d);
			if (riscv_stdext_d)
				riscv_flt_d (code, ins->dreg, ins->sreg1, ins->sreg2);
			else
				NOT_IMPLEMENTED;
			break;
		}
		case OP_RCLE: {
			g_assert (riscv_stdext_f);
			riscv_fle_s (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		}
		case OP_FCLE: {
			g_assert (riscv_stdext_f || riscv_stdext_d);
			if (riscv_stdext_d)
				riscv_fle_d (code, ins->dreg, ins->sreg1, ins->sreg2);
			else
				NOT_IMPLEMENTED;
			break;
		}
		case OP_STORER4_MEMBASE_REG: {
			if (mono_arch_is_soft_float ())
				code = mono_riscv_emit_store (code, ins->sreg1, ins->dreg, ins->inst_offset, 4);
			else
				code = mono_riscv_emit_fstore (code, ins->sreg1, ins->dreg, ins->inst_offset, TRUE);
			break;
		}
		case OP_STORER8_MEMBASE_REG: {
			if (mono_arch_is_soft_float ())
				code = mono_riscv_emit_store (code, ins->sreg1, ins->dreg, ins->inst_offset, 8);
			else
				code = mono_riscv_emit_fstore (code, ins->sreg1, ins->dreg, ins->inst_offset, FALSE);
			break;
		}
		case OP_LOADR4_MEMBASE: {
			if (mono_arch_is_soft_float ())
				code = mono_riscv_emit_load (code, ins->dreg, ins->sreg1, ins->inst_offset, 4);
			else
				code = mono_riscv_emit_fload (code, ins->dreg, ins->sreg1, ins->inst_offset, TRUE);
			break;
		}
		case OP_LOADR8_MEMBASE: {
			if (mono_arch_is_soft_float ())
				code = mono_riscv_emit_load (code, ins->dreg, ins->sreg1, ins->inst_offset, 8);
			else
				code = mono_riscv_emit_fload (code, ins->dreg, ins->sreg1, ins->inst_offset, FALSE);
			break;
		}

		/* Calls */
		case OP_TAILCALL_PARAMETER:
			// This opcode helps compute sizes, i.e.
			// of the subsequent OP_TAILCALL, but contributes no code.
			g_assert (ins->next);
			break;
		case OP_TAILCALL:
		case OP_TAILCALL_REG:
		case OP_TAILCALL_MEMBASE: {
			int target_reg = RISCV_T1;
			call = (MonoCallInst *)ins;

			g_assert (!cfg->method->save_lmf);

			switch (ins->opcode) {
			case OP_TAILCALL:
				break;
			case OP_TAILCALL_REG:
				g_assert (ins->sreg1 != -1);
				g_assert (ins->sreg1 != RISCV_T0);
				g_assert (ins->sreg1 != RISCV_T1);
				g_assert (ins->sreg1 != RISCV_RA);
				g_assert (ins->sreg1 != RISCV_SP);
				g_assert (ins->sreg1 != RISCV_FP);
				riscv_addi (code, target_reg, ins->sreg1, 0);
				break;
			case OP_TAILCALL_MEMBASE:
				g_assert (ins->sreg1 != -1);
				g_assert (ins->sreg1 != RISCV_T0);
				g_assert (ins->sreg1 != RISCV_T1);
				g_assert (ins->sreg1 != RISCV_RA);
				g_assert (ins->sreg1 != RISCV_SP);
				g_assert (ins->sreg1 != RISCV_FP);
				code = mono_riscv_emit_load (code, target_reg, ins->inst_basereg, ins->inst_offset, 0);
				break;
			default:
				g_assert_not_reached ();
			}

			/* Restore registers */
			code = mono_riscv_emit_load_stack (code, MONO_ARCH_CALLEE_SAVED_REGS & cfg->used_int_regs, RISCV_FP,
			                                   -cfg->arch.saved_gregs_offset, FALSE);

			/* Destroy frame */
			code = mono_riscv_emit_destroy_frame (code);

			switch (ins->opcode) {
			case OP_TAILCALL:
				if (cfg->compile_aot) {
					NOT_IMPLEMENTED;
				} else {
					mono_add_patch_info_rel (cfg, GPTRDIFF_TO_INT (code - cfg->native_code),
					                         MONO_PATCH_INFO_METHOD_JUMP, call->method, MONO_R_RISCV_JAL);
					cfg->thunk_area += THUNK_SIZE;
					riscv_jal (code, RISCV_ZERO, 0);
				}
				break;
			case OP_TAILCALL_REG:
			case OP_TAILCALL_MEMBASE:
				// code = mono_arm_emit_brx (code, branch_reg);
				riscv_jalr (code, RISCV_ZERO, target_reg, 0);
				break;

			default:
				g_assert_not_reached ();
			}

			ins->flags |= MONO_INST_GC_CALLSITE;
			ins->backend.pc_offset = GPTRDIFF_TO_INT (code - cfg->native_code);

			break;
		}
		case OP_VOIDCALL:
		case OP_CALL:
		case OP_FCALL:
		case OP_RCALL:
		case OP_LCALL:
		case OP_VCALL2: {
			call = (MonoCallInst *)ins;
			const MonoJumpInfoTarget patch = mono_call_to_patch (call);
			code = mono_riscv_emit_call (cfg, code, patch.type, patch.target);
			ins->flags |= MONO_INST_GC_CALLSITE;
			ins->backend.pc_offset = GPTRDIFF_TO_INT (code - cfg->native_code);
			code = emit_move_return_value (cfg, code, ins);
			break;
		}
		case OP_CALL_REG:
		case OP_LCALL_REG:
		case OP_RCALL_REG:
		case OP_FCALL_REG:
		case OP_VOIDCALL_REG:
		case OP_VCALL2_REG:
			// JALR x1, 0(src1)
			riscv_jalr (code, RISCV_RA, ins->sreg1, 0);
			code = emit_move_return_value (cfg, code, ins);
			break;
		case OP_CALL_MEMBASE:
		case OP_LCALL_MEMBASE:
		case OP_RCALL_MEMBASE:
		case OP_FCALL_MEMBASE:
		case OP_VCALL2_MEMBASE:
		case OP_VOIDCALL_MEMBASE:
			code = mono_riscv_emit_load (code, RISCV_T1, ins->inst_basereg, ins->inst_offset, 0);
			riscv_jalr (code, RISCV_RA, RISCV_T1, 0);
			code = emit_move_return_value (cfg, code, ins);
			break;

		/* Branch */
		// incase of target is far from the brunch inst
		// reverse a nop inst for patch use
		case OP_RISCV_BNE:
			mono_add_patch_info_rel (cfg, (code - cfg->native_code), MONO_PATCH_INFO_BB, ins->inst_true_bb,
			                         MONO_R_RISCV_BNE);
			riscv_bne (code, ins->sreg1, ins->sreg2, 0);
			code = mono_riscv_emit_nop (code);
			break;
		case OP_RISCV_BEQ:
			mono_add_patch_info_rel (cfg, (code - cfg->native_code), MONO_PATCH_INFO_BB, ins->inst_true_bb,
			                         MONO_R_RISCV_BEQ);
			riscv_beq (code, ins->sreg1, ins->sreg2, 0);
			code = mono_riscv_emit_nop (code);
			break;
		case OP_RISCV_BGE:
			mono_add_patch_info_rel (cfg, (code - cfg->native_code), MONO_PATCH_INFO_BB, ins->inst_true_bb,
			                         MONO_R_RISCV_BGE);
			riscv_bge (code, ins->sreg1, ins->sreg2, 0);
			code = mono_riscv_emit_nop (code);
			break;
		case OP_RISCV_BGEU:
			mono_add_patch_info_rel (cfg, (code - cfg->native_code), MONO_PATCH_INFO_BB, ins->inst_true_bb,
			                         MONO_R_RISCV_BGEU);
			riscv_bgeu (code, ins->sreg1, ins->sreg2, 0);
			code = mono_riscv_emit_nop (code);
			break;
		case OP_RISCV_BLT:
			mono_add_patch_info_rel (cfg, (code - cfg->native_code), MONO_PATCH_INFO_BB, ins->inst_true_bb,
			                         MONO_R_RISCV_BLT);
			riscv_blt (code, ins->sreg1, ins->sreg2, 0);
			code = mono_riscv_emit_nop (code);
			break;
		case OP_RISCV_BLTU:
			mono_add_patch_info_rel (cfg, (code - cfg->native_code), MONO_PATCH_INFO_BB, ins->inst_true_bb,
			                         MONO_R_RISCV_BLTU);
			riscv_bltu (code, ins->sreg1, ins->sreg2, 0);
			code = mono_riscv_emit_nop (code);
			break;
		case OP_BR:
			mono_add_patch_info_rel (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_target_bb, MONO_R_RISCV_JAL);
			riscv_jal (code, RISCV_ZERO, 0);
			break;
		case OP_BR_REG:
			riscv_jalr (code, RISCV_ZERO, ins->sreg1, 0);
			break;
		case OP_JUMP_TABLE:
			mono_add_patch_info_rel (cfg, offset, (MonoJumpInfoType)(gsize)ins->inst_i1, ins->inst_p0,
			                         MONO_R_RISCV_IMM);
			code = mono_riscv_emit_imm (code, ins->dreg, 0xffffffff);
			break;
		case OP_RISCV_FBNAN:
			// fclass.d T0, rs1; andi T0, T0, RISCV_FCLASS_NAN; bne T0, zero, target
			riscv_fclass_d (code, RISCV_T0, ins->sreg1);
			riscv_andi (code, RISCV_T0, RISCV_T0, RISCV_FCLASS_NAN);
			mono_add_patch_info_rel (cfg, (code - cfg->native_code), MONO_PATCH_INFO_BB, ins->inst_true_bb,
			                         MONO_R_RISCV_BNE);
			riscv_bne (code, RISCV_T0, RISCV_ZERO, 0);
			code = mono_riscv_emit_nop (code);
			break;
		case OP_RISCV_RBNAN:
			// fclass.s T0, rs1; andi T0, T0, RISCV_FCLASS_NAN; bne T0, zero, target
			riscv_fclass_s (code, RISCV_T0, ins->sreg1);
			riscv_andi (code, RISCV_T0, RISCV_T0, RISCV_FCLASS_NAN);
			mono_add_patch_info_rel (cfg, (code - cfg->native_code), MONO_PATCH_INFO_BB, ins->inst_true_bb,
			                         MONO_R_RISCV_BNE);
			riscv_bne (code, RISCV_T0, RISCV_ZERO, 0);
			code = mono_riscv_emit_nop (code);
			break;
		case OP_RISCV_EXC_BNE:
		case OP_RISCV_EXC_BEQ:
		case OP_RISCV_EXC_BGEU:
		case OP_RISCV_EXC_BLT:
		case OP_RISCV_EXC_BLTU: {
			code =
			    mono_riscv_emit_branch_exc (cfg, code, ins->opcode, ins->sreg1, ins->sreg2, (const char *)ins->inst_p1);
			break;
		}

		/* Throw */
		case OP_THROW:
			code = mono_riscv_emit_call (cfg, code, MONO_PATCH_INFO_JIT_ICALL_ID,
			                             GUINT_TO_POINTER (MONO_JIT_ICALL_mono_arch_throw_exception));
			break;
		case OP_RETHROW:
			code = mono_riscv_emit_call (cfg, code, MONO_PATCH_INFO_JIT_ICALL_ID,
			                             GUINT_TO_POINTER (MONO_JIT_ICALL_mono_arch_rethrow_exception));
			break;

		case OP_GC_SAFE_POINT: {
			guint8 *src_inst_pointer [1];

			riscv_ld (code, RISCV_T1, ins->sreg1, 0);
			/* Call it if it is non-null */
			src_inst_pointer [0] = code;
			riscv_beq (code, RISCV_ZERO, RISCV_T1, 0);
			code = mono_riscv_emit_call (cfg, code, MONO_PATCH_INFO_JIT_ICALL_ID,
			                             GUINT_TO_POINTER (MONO_JIT_ICALL_mono_threads_state_poll));
			mono_riscv_patch (src_inst_pointer [0], code, MONO_R_RISCV_BEQ);
			break;
		}
		case OP_START_HANDLER: {
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);
			/* Save caller address */
			code = mono_riscv_emit_store (code, RISCV_RA, spvar->inst_basereg, spvar->inst_offset, 0);

			/*
			 * Reserve a param area, see test_0_finally_param_area ().
			 * This is needed because the param area is not set up when
			 * we are called from EH code.
			 */
			if (cfg->param_area)
				riscv_addi (code, RISCV_SP, RISCV_SP, -ALIGN_TO (cfg->param_area, MONO_ARCH_FRAME_ALIGNMENT));
			break;
		}
		case OP_CALL_HANDLER:
			mono_add_patch_info_rel (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_target_bb, MONO_R_RISCV_JAL);
			riscv_jal (code, RISCV_RA, 0);
			cfg->thunk_area += THUNK_SIZE;
			for (GList *tmp = ins->inst_eh_blocks; tmp != bb->clause_holes; tmp = tmp->prev)
				mono_cfg_add_try_hole (cfg, ((MonoLeaveClause *)tmp->data)->clause, code, bb);
			break;
		case OP_ENDFINALLY:
		case OP_ENDFILTER: {
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);
			if (cfg->param_area)
				riscv_addi (code, RISCV_SP, RISCV_SP, cfg->param_area);
			if (ins->opcode == OP_ENDFILTER && ins->sreg1 != RISCV_A0)
				riscv_addi (code, RISCV_A0, ins->sreg1, 0);

			/* Return to either after the branch in OP_CALL_HANDLER, or to the EH code */
			code = mono_riscv_emit_load (code, RISCV_RA, spvar->inst_basereg, spvar->inst_offset, 0);
			riscv_jalr (code, RISCV_ZERO, RISCV_RA, 0);
			break;
		}

		default:
			printf ("unable to output following IR:");
			mono_print_ins (ins);
			NOT_IMPLEMENTED;
			break;
		}

		g_assertf ((code - cfg->native_code - offset) <= max_len,
		           "wrong maximal instruction length of instruction %s (expected %d, got %d)",
		           mono_inst_name (ins->opcode), max_len, (int)(code - cfg->native_code - offset));
	}
	set_code_cursor (cfg, code);
}

void
mono_arch_emit_exceptions (MonoCompile *cfg)
{
	MonoJumpInfo *ji;
	MonoClass *exc_class;
	guint8 *code, *ip;
	guint8 *exc_throw_pos [MONO_EXC_INTRINS_NUM] = {NULL};
	guint8 exc_throw_found [MONO_EXC_INTRINS_NUM] = {0};
	int exc_id, size = 0;

	for (ji = cfg->patch_info; ji; ji = ji->next) {
		if (ji->type == MONO_PATCH_INFO_EXC) {
			exc_id = mini_exception_id_by_name ((const char *)ji->data.target);
			g_assert (exc_id < MONO_EXC_INTRINS_NUM);
			if (!exc_throw_found [exc_id]) {
				size += 40; // 8 Inst for exception
				exc_throw_found [exc_id] = TRUE;
			}
		}
	}

	code = realloc_code (cfg, size);

	/* Emit code to raise corlib exceptions */
	for (ji = cfg->patch_info; ji; ji = ji->next) {
		if (ji->type != MONO_PATCH_INFO_EXC)
			continue;

		ip = cfg->native_code + ji->ip.i;

		exc_id = mini_exception_id_by_name ((const char *)ji->data.target);

		if (exc_throw_pos [exc_id]) {
			/* ip should points to the branch inst in OP_COND_EXC_... */
			riscv_patch_rel (ip, exc_throw_pos [exc_id], ji->relocation);
			ji->type = MONO_PATCH_INFO_NONE;
			continue;
		}

		exc_throw_pos [exc_id] = code;
		riscv_patch_rel (ip, code, ji->relocation);

		/* A0 = type token */
		exc_class = mono_class_load_from_name (mono_defaults.corlib, "System", ji->data.name);
		code = mono_riscv_emit_imm (code, RISCV_A0, m_class_get_type_token (exc_class) - MONO_TOKEN_TYPE_DEF);
		/* A1 = throw ip */
		riscv_addi (code, RISCV_A1, MONO_ARCH_EXC_ADDR_REG, 0);
		/* Branch to the corlib exception throwing trampoline */
		ji->ip.i = code - cfg->native_code;
		ji->type = MONO_PATCH_INFO_JIT_ICALL_ID;
		ji->data.jit_icall_id = MONO_JIT_ICALL_mono_arch_throw_corlib_exception;
		ji->relocation = MONO_R_RISCV_JAL;
		riscv_jal (code, RISCV_RA, 0);

		cfg->thunk_area += THUNK_SIZE;
		set_code_cursor (cfg, code);
	}

	set_code_cursor (cfg, code);
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
