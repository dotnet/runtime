/*
 * Licensed to the .NET Foundation under one or more agreements.
 * The .NET Foundation licenses this file to you under the MIT license.
 */

#include <mono/utils/mono-hwcap.h>

#include "mini-runtime.h"

#ifdef TARGET_RISCV64
#include "cpu-riscv64.h"
#else
#include "cpu-riscv32.h"
#endif

static gboolean riscv_stdext_a, riscv_stdext_b, riscv_stdext_c,
                riscv_stdext_d, riscv_stdext_f, riscv_stdext_j,
                riscv_stdext_l, riscv_stdext_m, riscv_stdext_n,
                riscv_stdext_p, riscv_stdext_q, riscv_stdext_t,
                riscv_stdext_v;

void
mono_arch_cpu_init (void)
{
}

void
mono_arch_init (void)
{
	riscv_stdext_a = mono_hwcap_riscv_has_stdext_a;
	riscv_stdext_c = mono_hwcap_riscv_has_stdext_c;
	riscv_stdext_d = mono_hwcap_riscv_has_stdext_d;
	riscv_stdext_f = mono_hwcap_riscv_has_stdext_f;
	riscv_stdext_m = mono_hwcap_riscv_has_stdext_m;
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
#define MAX_ARCH_DELEGATE_PARAMS 7

static gpointer
get_delegate_invoke_impl (gboolean has_target, gboolean param_count, guint32 *code_size)
{
	guint8 *code, *start;

	MINI_BEGIN_CODEGEN ();

	if (has_target) {
		start = code = mono_global_codeman_reserve (4 * 3);
		code = mono_riscv_emit_load (code, RISCV_T0, RISCV_A0, MONO_STRUCT_OFFSET (MonoDelegate, method_ptr), 0);
		code = mono_riscv_emit_load (code, RISCV_A0, RISCV_A0, MONO_STRUCT_OFFSET (MonoDelegate, target), 0);
		riscv_jalr (code, RISCV_ZERO, RISCV_T0, 0);

		g_assert ((code - start) <= 4 * 3);
	}
	else{
		int size, i;

		size = 8 + param_count * 4;
		start = code = mono_global_codeman_reserve (size);

		code = mono_riscv_emit_load (code, RISCV_T0, RISCV_A0, MONO_STRUCT_OFFSET (MonoDelegate, method_ptr), 0);
		/* slide down the arguments */
		for (i = 0; i < param_count; ++i)
			riscv_addi (code, RISCV_A0 + i, RISCV_A0 + i + 1, 0);
		
		riscv_jalr (code, RISCV_ZERO, RISCV_T0, 0);
		g_assert ((code - start) <= size);
	}

	MINI_END_CODEGEN (start, code - start, MONO_PROFILER_CODE_BUFFER_DELEGATE_INVOKE, NULL);

	if (code_size)
		*code_size = code - start;

	return MINI_ADDR_TO_FTNPTR (start);
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

	code = (guint8*)get_delegate_invoke_impl (TRUE, 0, &code_len);
	res = g_slist_prepend (res, mono_tramp_info_create ("delegate_invoke_impl_has_target", code, code_len, NULL, NULL));

	for (int i = 0; i <= MAX_ARCH_DELEGATE_PARAMS; ++i) {
		code = (guint8*)get_delegate_invoke_impl (FALSE, i, &code_len);
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
		static guint8* cached = NULL;

		if (cached)
			return cached;

		if (mono_ee_features.use_aot_trampolines)
			NOT_IMPLEMENTED;
		else
			start = (guint8*)get_delegate_invoke_impl (TRUE, 0, NULL);
		mono_memory_barrier ();
		cached = start;
		return cached;
	}
	else{
		static guint8* cache [MAX_ARCH_DELEGATE_PARAMS + 1] = {NULL};

		if (sig->param_count > MAX_ARCH_DELEGATE_PARAMS)
			NOT_IMPLEMENTED;
		for (int i = 0; i < sig->param_count; ++i)
			if (!mono_is_regsize_var (sig->params [i]))
				NOT_IMPLEMENTED;

		code = cache [sig->param_count];
		if (code)
			return code;
		if (mono_ee_features.use_aot_trampolines) {
			NOT_IMPLEMENTED;
		}
		else {
			start = (guint8*)get_delegate_invoke_impl (FALSE, sig->param_count, NULL);
		}
		mono_memory_barrier ();
		cache [sig->param_count] = start;
		return start;
	}

	return NULL;
}

gpointer
mono_arch_get_delegate_virtual_invoke_impl (MonoMethodSignature *sig,
                                            MonoMethod *method, int offset,
                                            gboolean load_imt_reg)
{
	NOT_IMPLEMENTED;
	return NULL;
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
#ifdef TARGET_RISCV64
	case OP_ATOMIC_LOAD_R8:
	case OP_ATOMIC_STORE_R8:
#endif
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
	return (MonoVTable *) regs [MONO_ARCH_VTABLE_REG];
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

void
mono_arch_patch_code_new (MonoCompile *cfg, guint8 *code,
                          MonoJumpInfo *ji, gpointer target)
{
	NOT_IMPLEMENTED;
}

/* Set arguments in the ccontext (for i2n entry) */
void
mono_arch_set_native_call_context_args (CallContext *ccontext, gpointer frame, MonoMethodSignature *sig)
{
	NOT_IMPLEMENTED;
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
	NOT_IMPLEMENTED;
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
#ifdef TARGET_RISCV64
	case OP_LMUL_IMM:
	case OP_LDIV:
	case OP_LDIV_UN:
	case OP_LREM:
	case OP_LREM_UN:
	case OP_LREM_UN_IMM:
#endif
		return !riscv_stdext_m;

	case OP_FDIV:
	case OP_FMUL:
	case OP_FCONV_TO_I4:
	case OP_ICONV_TO_R4:
#ifdef TARGET_RISCV64
	case OP_ICONV_TO_R8:
	case OP_LCONV_TO_R8:
	case OP_FCONV_TO_R8:
#endif
		return !mono_arch_is_soft_float ();
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

gint
static mono_arch_get_memory_ordering(int memory_barrier_kind)
{
	gint ordering;
	switch (memory_barrier_kind){
	case MONO_MEMORY_BARRIER_ACQ:
		ordering = RISCV_ORDER_AQ;
		break;
	case MONO_MEMORY_BARRIER_REL:
		ordering = RISCV_ORDER_RL;
		break;
	case MONO_MEMORY_BARRIER_SEQ:
		ordering = RISCV_ORDER_ALL;
	default:
		ordering = RISCV_ORDER_NONE;
		break;
	}
	return ordering;
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

	for (int i = RISCV_S0; i <= RISCV_S11; i++)
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

void
mono_arch_create_vars (MonoCompile *cfg)
{
	NOT_IMPLEMENTED;
}

MonoInst *
mono_arch_emit_inst_for_method (MonoCompile *cfg, MonoMethod *cmethod,
                                MonoMethodSignature *fsig, MonoInst **args)
{
	return NULL;
}

void
mono_arch_emit_call (MonoCompile *cfg, MonoCallInst *call)
{
	NOT_IMPLEMENTED;
}

void
mono_arch_emit_outarg_vt (MonoCompile *cfg, MonoInst *ins, MonoInst *src)
{
	NOT_IMPLEMENTED;
}

void
mono_arch_emit_setret (MonoCompile *cfg, MonoMethod *method, MonoInst *val)
{
	NOT_IMPLEMENTED;
}

void
mono_arch_decompose_opts (MonoCompile *cfg, MonoInst *ins)
{
	switch (ins->opcode) {
	case OP_LADD:
	case OP_LADD_IMM:
	case OP_IADD:
	case OP_IADD_IMM:
	case OP_IADD_OVF:
	case OP_ISUB:
	case OP_LSUB:
	case OP_ISUB_IMM:
	case OP_LSUB_IMM:
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
	case OP_FCONV_TO_I4:
	case OP_ICONV_TO_R4:
#ifdef TARGET_RISCV64
	case OP_LNEG:

	case OP_ICONV_TO_I4:
	case OP_ICONV_TO_U4:
	case OP_ICONV_TO_I8:
	case OP_ICONV_TO_U8:
	case OP_LCONV_TO_U:
	case OP_LCONV_TO_I:
	case OP_LCONV_TO_U1:
	case OP_LCONV_TO_U2:
	case OP_LCONV_TO_I4:
	case OP_LCONV_TO_U4:
	case OP_LCONV_TO_I8:
	case OP_LCONV_TO_U8:

	case OP_ICONV_TO_R8:
	case OP_LCONV_TO_R8:
	case OP_FCONV_TO_R8:
#endif
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
	case OP_ISHL_IMM:
	case OP_LSHL_IMM:
	case OP_ISHR_UN:
	case OP_LSHR_UN:
	case OP_ISHR_IMM:
	case OP_LSHR_IMM:
	case OP_ISHR_UN_IMM:
	case OP_LSHR_UN_IMM:

	case OP_IMUL:
	case OP_LMUL:
	case OP_FMUL:
	case OP_LMUL_IMM:
	case OP_IDIV:
	case OP_LDIV:
	case OP_LDIV_UN:
	case OP_IDIV_UN:
	case OP_IREM:
	case OP_LREM:
	case OP_IREM_UN:
	case OP_LREM_UN:

	case OP_ICONV_TO_OVF_U2:
	case OP_LCONV_TO_OVF_U:
	case OP_LCONV_TO_OVF_I4_UN:
	case OP_LCONV_TO_OVF_U4_UN:

	case OP_LADD_OVF_UN:
	case OP_IMUL_OVF:
	case OP_LMUL_OVF_UN_OOM:

	case OP_FDIV:
		break;
	default:
		g_print("Can't decompose the OP %s\n",mono_inst_name (ins->opcode));
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

void
mono_arch_allocate_vars (MonoCompile *cfg)
{
	NOT_IMPLEMENTED;
}

#define NEW_INS_BEFORE(cfg,ins,dest,op) do {	\
		MONO_INST_NEW ((cfg), (dest), (op)); 	\
		(dest)->cil_code = (ins)->cil_code; 	\
		mono_bblock_insert_before_ins (bb, ins, (dest)); \
	} while (0)

#define NEW_INS_AFTER(cfg,ins,dest,op) do {		\
		MONO_INST_NEW ((cfg), (dest), (op)); 	\
		(dest)->cil_code = (ins)->cil_code; 	\
		mono_bblock_insert_after_ins (bb, ins, (dest)); \
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
	MonoInst *ins, *last_ins,*n, *temp;
	if (cfg->verbose_level > 2) {
		g_print ("BASIC BLOCK %d (before lowering)\n", bb->block_num);
		MONO_BB_FOR_EACH_INS (bb, ins) {
			mono_print_ins (ins);
		}
		
	}

	MONO_BB_FOR_EACH_INS_SAFE (bb, n, ins){
loop_start:
		switch (ins->opcode){
		case OP_IL_SEQ_POINT:
		case OP_SEQ_POINT:
		case OP_GC_SAFE_POINT:
		case OP_BR:
		case OP_BR_REG:
		case OP_JUMP_TABLE:
		case OP_CALL:
		case OP_FCALL:
		case OP_LCALL:
		case OP_VCALL2:
		case OP_VOIDCALL:
		case OP_START_HANDLER:
		case OP_CALL_HANDLER:
		case OP_ENDFINALLY:
		case OP_GET_EX_OBJ:
		case OP_GENERIC_CLASS_INIT:
		case OP_I8CONST:
		case OP_ICONST:
		case OP_MOVE:
		case OP_RMOVE:
		case OP_FMOVE:
		case OP_LMOVE:
		case OP_ISUB:
		case OP_LSUB:
		case OP_IADD:
		case OP_LADD:
		case OP_FMUL:
		case OP_LDIV:
		case OP_LDIV_UN:
		case OP_IREM:
		case OP_LREM:
		case OP_IREM_UN:
		case OP_LREM_UN:
		case OP_CHECK_THIS:
		case OP_IAND:
		case OP_LAND:
		case OP_XOR_IMM:
		case OP_IXOR_IMM:
		case OP_IXOR:
		case OP_LXOR:
		case OP_IOR:
		case OP_LOR:
		case OP_ISHL:
		case OP_SHL_IMM:
		case OP_LSHL_IMM:
		case OP_SHR_IMM:
		case OP_ISHR_IMM:
		case OP_SHR_UN_IMM:
		case OP_ISHR_UN:
		case OP_LSHR_UN:
		case OP_ISHR_UN_IMM:
		case OP_ISHL_IMM:
		case OP_LSHR_IMM:
		case OP_LSHR_UN_IMM:
		case OP_LOCALLOC:
		
		/* skip dummy IL */
		case OP_NOT_REACHED:
		case OP_NOT_NULL:
		case OP_DUMMY_USE:
		case OP_NOP:

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
		case OP_ATOMIC_STORE_U1:
		case OP_ATOMIC_STORE_I4:
		case OP_ATOMIC_STORE_U8:
		case OP_ATOMIC_LOAD_I4:
		case OP_ATOMIC_LOAD_I8:
		case OP_ATOMIC_LOAD_U8:
		case OP_ATOMIC_CAS_I4:
		case OP_ATOMIC_CAS_I8:
		case OP_ATOMIC_EXCHANGE_I4:
#ifdef TARGET_RISCV64
		case OP_ATOMIC_EXCHANGE_I8:
#endif

		/* Float Ext */
		case OP_R8CONST:
		case OP_ICONV_TO_R8:
		case OP_RCONV_TO_R8:
		case OP_FCONV_TO_I4:
		case OP_FCEQ:
		case OP_FCLT:
		case OP_FCLT_UN:
		case OP_RISCV_SETFREG_R4:
			break;
		case OP_R4CONST:
		case OP_ICONV_TO_R4:
			if(!cfg->r4fp){
				NEW_INS_AFTER (cfg, ins, temp, OP_RCONV_TO_R8);
				temp->dreg = ins->dreg;
				temp->sreg1 = mono_alloc_freg(cfg);

				ins->dreg = temp->sreg1;
			}
			break;
		case OP_FCGT:
		case OP_FCGT_UN:{
			// fcgt rd, rs1, rs2 -> flt rd, rs2, rs1
			ins->opcode = OP_FCLT;
			int tmp_reg = ins->sreg1;
			ins->sreg1 = ins->sreg2;
			ins->sreg2 = tmp_reg;
			break;
		}
		case OP_FCOMPARE:{
			if (ins->next){
				if(ins->next->opcode == OP_FBLT || ins->next->opcode == OP_FBLT_UN){
					ins->opcode = OP_FCLT;
					ins->dreg = mono_alloc_ireg (cfg);

					ins->next->opcode = OP_RISCV_BNE;
					ins->next->sreg1 = ins->dreg;
					ins->next->sreg2 = RISCV_ZERO;
				}
				else if(ins->next->opcode == OP_FBGT || ins->next->opcode == OP_FBGT_UN){
					// fcmp rd, rs1, rs2; fbgt rd -> fcgt rd, rs1, rs2; bne rd, X0
					// fcgt rd, rs1, rs2 -> flt rd, rs2, rs1
					ins->opcode = OP_FCLT;
					ins->dreg = mono_alloc_ireg (cfg);
					int tmp_reg = ins->sreg1;
					ins->sreg1 = ins->sreg2;
					ins->sreg2 = tmp_reg;

					ins->next->opcode = OP_RISCV_BNE;
					ins->next->sreg1 = ins->dreg;
					ins->next->sreg2 = RISCV_ZERO;
				}
				else {
					g_print("Unhandaled op %s following after OP_FCOMPARE\n",mono_inst_name (ins->next->opcode));
					NOT_IMPLEMENTED;
				}
			}
			else{
				g_assert_not_reached();
			}
			break;
		}

		case OP_LOCALLOC_IMM:
			if (ins->inst_imm > 32)
				mono_decompose_op_imm (cfg, bb, ins);
			break;

		case OP_CALL_MEMBASE:
		case OP_LCALL_MEMBASE:
		case OP_VCALL2_MEMBASE:
		case OP_VOIDCALL_MEMBASE:
			if(!RISCV_VALID_J_IMM(ins->inst_offset)){
				NOT_IMPLEMENTED;
				NEW_INS_BEFORE (cfg, ins, temp, OP_ICONST);
				temp->dreg = mono_alloc_ireg (cfg);
				temp->inst_c0 = ins->inst_offset;

				ins->sreg1 = temp->dreg;

				NEW_INS_BEFORE (cfg, ins, temp, OP_LADD);
				temp->dreg = ins->sreg1;
				temp->sreg1 = ins->sreg1;
				temp->sreg2 = ins->inst_basereg;

				ins->inst_basereg = -1;
				ins->inst_offset = 0;
				ins->opcode = OP_VOIDCALL_REG;
			}
			break;
		case OP_CALL_REG:
		case OP_VOIDCALL_REG:
			break;
		
		/* Throw */
		case OP_THROW:
		case OP_RETHROW:
			if (ins->sreg1 != RISCV_A0){
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
		case OP_STORE_MEMBASE_IMM:{
			if(ins->inst_imm != 0){
				NEW_INS_BEFORE (cfg, ins, temp, OP_ICONST);
				temp->inst_c0 = ins->inst_imm;
				temp->dreg = mono_alloc_ireg (cfg);
				
				ins->sreg1 = temp->dreg;
			}
			else{
				ins->sreg1 = RISCV_ZERO;
			}
			
			switch (ins->opcode){
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
					g_assert_not_reached();
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
		case OP_STORE_MEMBASE_REG:
			if(ins->opcode == OP_STORER4_MEMBASE_REG && !cfg->r4fp){
				NEW_INS_BEFORE (cfg, ins, temp, OP_FCONV_TO_R4);
				temp->dreg = mono_alloc_freg(cfg);
				temp->sreg1 = ins->sreg1;
			
				ins->sreg1 = temp->dreg;
			}
			// check if offset is valid I-type Imm
			if(! RISCV_VALID_I_IMM ((gint32) (gssize) (ins->inst_offset)))
				NOT_IMPLEMENTED;
			break;

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
			if(ins->opcode == OP_LOADR4_MEMBASE && !cfg->r4fp){
				NEW_INS_AFTER (cfg, ins, temp, OP_RCONV_TO_R8);
				temp->dreg = ins->dreg;
				temp->sreg1 = mono_alloc_freg(cfg);

				ins->dreg = temp->sreg1;
			}
			if(! RISCV_VALID_I_IMM ((gint32) (gssize) (ins->inst_imm))){
				NEW_INS_BEFORE (cfg, ins, temp, OP_ICONST);
				temp->inst_c0 = ins->inst_imm;
				temp->dreg = mono_alloc_ireg (cfg);
				ins->sreg1 = temp->dreg;
				ins->inst_imm = 0;
			}
			break;

		case OP_COMPARE_IMM:
		case OP_ICOMPARE_IMM:
		case OP_LCOMPARE_IMM:{
			if (ins->next){
				if(ins->next->opcode == OP_LCEQ || ins->next->opcode == OP_ICEQ){
					if(RISCV_VALID_I_IMM(ins->inst_imm)){
						// compare rs1, imm; lceq rd => addi rs2, rs1, -imm; sltiu rd, rs2, 1
						ins->opcode = OP_ADD_IMM;
						ins->dreg = mono_alloc_ireg (cfg);
						ins->inst_imm = -ins->inst_imm;

						ins->next->opcode = OP_RISCV_SLTIU;
						ins->next->sreg1 = ins->dreg;
						ins->next->inst_imm = 1;
						break;
					}
				}
				if(ins->next->opcode == OP_ICNEQ){
					if(RISCV_VALID_I_IMM(ins->inst_imm)){
						// compare rs1, imm; lcneq rd => addi rs2, rs1, -imm; sltu rd, X0, rs2
						ins->opcode = OP_ADD_IMM;
						ins->dreg = mono_alloc_ireg (cfg);
						ins->inst_imm = -ins->inst_imm;

						ins->next->opcode = OP_RISCV_SLTU;
						ins->next->sreg1 = RISCV_ZERO;
						ins->next->sreg2 = ins->dreg;
						break;
					}
				}
				else if(ins->next->opcode == OP_LCGT_UN || ins->next->opcode == OP_ICGT_UN){
					if(RISCV_VALID_I_IMM(ins->inst_imm + 1)){
						// compare rs1, imm; lcgt_un rd => sltiu rd, rs1, imm; xori rd, rd, 1
						ins->opcode = OP_RISCV_SLTIU;
						ins->dreg = ins->next->dreg;
						ins->sreg1 = ins->sreg1;
						ins->inst_imm = ins->inst_imm + 1;

						ins->next->opcode = OP_XOR_IMM;
						ins->next->dreg = ins->dreg;
						ins->next->sreg1 = ins->dreg;
						ins->next->inst_imm = 1;
						break;
					}			
				}
				else if(ins->next->opcode == OP_LCGT || ins->next->opcode == OP_ICGT){
					if(RISCV_VALID_I_IMM(ins->inst_imm + 1)){
						ins->opcode = OP_RISCV_SLTI;
						ins->dreg = ins->next->dreg;
						ins->sreg1 = ins->sreg1;
						ins->inst_imm = ins->inst_imm + 1;

						ins->next->opcode = OP_XOR_IMM;
						ins->next->dreg = ins->dreg;
						ins->next->sreg1 = ins->dreg;
						ins->next->inst_imm = 1;
						break;
					}
				}
			}
			else{
				// why it will reach this?
				NULLIFY_INS (ins);
				break;
			}


			if(ins->inst_imm == 0){
				ins->sreg2 = RISCV_ZERO;
			}
			else{
				NEW_INS_BEFORE (cfg, ins, temp, OP_ICONST);
				temp->inst_c0 = ins->inst_imm;
				temp->dreg = mono_alloc_ireg (cfg);
				ins->sreg2 = temp->dreg;
				ins->inst_imm = 0;
			}
			ins->opcode = OP_ICOMPARE;
			goto loop_start;
		}
		case OP_COMPARE:
		case OP_ICOMPARE:
		case OP_LCOMPARE:{
			if (ins->next){
				if(ins->next->opcode == OP_COND_EXC_EQ || ins->next->opcode == OP_COND_EXC_IEQ){
					ins->next->opcode = OP_RISCV_EXC_BEQ;
					ins->next->sreg1 = ins->sreg1;
					ins->next->sreg2 = ins->sreg2;
					NULLIFY_INS (ins);
				}
				else if(ins->next->opcode == OP_COND_EXC_NE_UN){
					ins->next->opcode = OP_RISCV_EXC_BNE;
					ins->next->sreg1 = ins->sreg1;
					ins->next->sreg2 = ins->sreg2;
					NULLIFY_INS (ins);
				}
				else if(ins->next->opcode == OP_COND_EXC_LT){
					ins->next->opcode = OP_RISCV_EXC_BLT;
					ins->next->sreg1 = ins->sreg1;
					ins->next->sreg2 = ins->sreg2;
					NULLIFY_INS (ins);
				}
				else if(ins->next->opcode == OP_COND_EXC_LT_UN){
					ins->next->opcode = OP_RISCV_EXC_BLTU;
					ins->next->sreg1 = ins->sreg1;
					ins->next->sreg2 = ins->sreg2;
					NULLIFY_INS (ins);
				}
				else if(ins->next->opcode == OP_COND_EXC_LE_UN){
					ins->next->opcode = OP_RISCV_EXC_BGEU;
					ins->next->sreg1 = ins->sreg2;
					ins->next->sreg2 = ins->sreg1;
					NULLIFY_INS (ins);
				}
				else if(ins->next->opcode == OP_COND_EXC_IGT_UN || ins->next->opcode == OP_COND_EXC_GT_UN){
					ins->next->opcode = OP_RISCV_EXC_BLTU;
					ins->next->sreg1 = ins->sreg2;
					ins->next->sreg2 = ins->sreg1;
					NULLIFY_INS (ins);
				}
				else if(ins->next->opcode == OP_LBEQ || ins->next->opcode == OP_IBEQ){
					ins->next->opcode = OP_RISCV_BEQ;
					ins->next->sreg1 = ins->sreg1;
					ins->next->sreg2 = ins->sreg2;
					NULLIFY_INS (ins);
				}
				else if(ins->next->opcode == OP_LBNE_UN || ins->next->opcode == OP_IBNE_UN){
					ins->next->opcode = OP_RISCV_BNE;
					ins->next->sreg1 = ins->sreg1;
					ins->next->sreg2 = ins->sreg2;
					NULLIFY_INS (ins);
				}
				else if(ins->next->opcode == OP_LBGE_UN || ins->next->opcode == OP_IBGE_UN){
					ins->next->opcode = OP_RISCV_BGEU;
					ins->next->sreg1 = ins->sreg1;
					ins->next->sreg2 = ins->sreg2;
					NULLIFY_INS (ins);
				}
				else if(ins->next->opcode == OP_LBGE || ins->next->opcode == OP_IBGE){
					ins->next->opcode = OP_RISCV_BGE;
					ins->next->sreg1 = ins->sreg1;
					ins->next->sreg2 = ins->sreg2;
					NULLIFY_INS (ins);
				}
				else if(ins->next->opcode == OP_LBGT_UN || ins->next->opcode == OP_IBGT_UN){
					ins->next->opcode = OP_RISCV_BLTU;
					ins->next->sreg1 = ins->sreg2;
					ins->next->sreg2 = ins->sreg1;
					NULLIFY_INS (ins);
				}
				else if(ins->next->opcode == OP_LBGT || ins->next->opcode == OP_IBGT){
					ins->next->opcode = OP_RISCV_BLT;
					ins->next->sreg1 = ins->sreg2;
					ins->next->sreg2 = ins->sreg1;
					NULLIFY_INS (ins);
				}
				else if(ins->next->opcode == OP_LBLE || ins->next->opcode == OP_IBLE){
					// ble rs1, rs2  -> bge rs2, rs1
					ins->next->opcode = OP_RISCV_BGE;
					ins->next->sreg1 = ins->sreg2;
					ins->next->sreg2 = ins->sreg1;
					NULLIFY_INS (ins);
				}
				else if(ins->next->opcode == OP_LBLE_UN || ins->next->opcode == OP_IBLE_UN){
					// ble rs1, rs2  -> bge rs2, rs1
					ins->next->opcode = OP_RISCV_BGEU;
					ins->next->sreg1 = ins->sreg2;
					ins->next->sreg2 = ins->sreg1;
					NULLIFY_INS (ins);
				}
				else if(ins->next->opcode == OP_LBLT_UN || ins->next->opcode == OP_IBLT_UN){
					ins->next->opcode = OP_RISCV_BLTU;
					ins->next->sreg1 = ins->sreg1;
					ins->next->sreg2 = ins->sreg2;
					NULLIFY_INS (ins);
				}
				else if(ins->next->opcode == OP_LBLT || ins->next->opcode == OP_IBLT){
					ins->next->opcode = OP_RISCV_BLT;
					ins->next->sreg1 = ins->sreg1;
					ins->next->sreg2 = ins->sreg2;
					NULLIFY_INS (ins);
				}
				else if(ins->next->opcode == OP_LCLT || ins->next->opcode == OP_ICLT){
					ins->next->opcode = OP_RISCV_SLT;
					ins->next->sreg1 = ins->sreg1;
					ins->next->sreg2 = ins->sreg2;
					NULLIFY_INS (ins);
				}
				else if(ins->next->opcode == OP_LCLT_UN || ins->next->opcode == OP_ICLT_UN){
					ins->next->opcode = OP_RISCV_SLTU;
					ins->next->sreg1 = ins->sreg1;
					ins->next->sreg2 = ins->sreg2;
					NULLIFY_INS (ins);
				}
				else if(ins->next->opcode == OP_LCEQ || ins->next->opcode == OP_ICEQ){
					// compare rs1, rs2; lceq rd => xor rd, rs1, rs2; sltiu rd, rd, 1
					ins->opcode = OP_IXOR;
					ins->dreg = ins->next->dreg;

					ins->next->opcode = OP_RISCV_SLTIU;
					ins->next->sreg1 = ins->dreg;
					ins->next->inst_imm = 1;
				}
				else if(ins->next->opcode == OP_ICNEQ){
					// compare rs1, rs2; lcneq rd => xor rd, rs1, rs2; sltu rd, X0, rd
					ins->opcode = OP_IXOR;
					ins->dreg = ins->next->dreg;

					ins->next->opcode = OP_RISCV_SLTU;
					ins->next->sreg1 = RISCV_ZERO;
					ins->next->sreg2 = ins->dreg;
				}
				else if(ins->next->opcode == OP_LCGT || ins->next->opcode == OP_ICGT){
					ins->next->opcode = OP_RISCV_SLT;
					ins->next->sreg1 = ins->sreg2;
					ins->next->sreg2 = ins->sreg1;
					NULLIFY_INS (ins);
				}
				else if(ins->next->opcode == OP_LCGT_UN || ins->next->opcode == OP_ICGT_UN){
					ins->next->opcode = OP_RISCV_SLTU;
					ins->next->sreg1 = ins->sreg2;
					ins->next->sreg2 = ins->sreg1;
					NULLIFY_INS (ins);
				}
				else if(ins->next->opcode == OP_IL_SEQ_POINT ||
						ins->next->opcode == OP_MOVE ||
						ins->next->opcode == OP_LOAD_MEMBASE ||
						ins->next->opcode == OP_NOP ||
						ins->next->opcode == OP_LOADI4_MEMBASE){
					/**
					 * there is compare without branch OP followed
					 * 
					 *  icompare_imm R226
					 *  il_seq_point il: 0xc6
					 * 
					 * what should I do?
					*/
					NULLIFY_INS (ins);
					break;
				}
				else {
					g_print("Unhandaled op %s following after OP_{I|L}COMPARE{|_IMM}\n",mono_inst_name (ins->next->opcode));
					NOT_IMPLEMENTED;
				}
			}
			else
				g_assert_not_reached();
			break;
		}

		/* Math */
		case OP_INEG:
		case OP_LNEG:
			ins->opcode = OP_ISUB;
			ins->sreg2 = ins->sreg1;
			ins->sreg1 = RISCV_ZERO;
			break;
		case OP_SUB_IMM:
		case OP_ISUB_IMM:
		case OP_LSUB_IMM:
			ins->inst_imm = -ins->inst_imm;
			ins->opcode = OP_ADD_IMM;
			goto loop_start;
		// Inst ADDI use I-type Imm
		case OP_ADD_IMM:
		case OP_IADD_IMM:
		case OP_LADD_IMM:
			if(! RISCV_VALID_I_IMM ((gint32) (gssize) (ins->inst_imm)))
				mono_decompose_op_imm (cfg, bb, ins);
			break;
		case OP_ADDCC:
		case OP_IADDCC:{
			/**
			 * add t0, t1, t2
			 * slti t3, t2, 0
			 * slt t4,t0,t1
			 * bne t3, t4, overflow
			*/
			ins->opcode = OP_IADD;
			MonoInst *branch_ins = ins->next;
			if(branch_ins){
				if(branch_ins->opcode == OP_COND_EXC_C
					|| branch_ins->opcode == OP_COND_EXC_IOV ){
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
				}
				else{
					mono_print_ins(branch_ins);
					g_assert_not_reached();
				}
			}
			break;
		}
		case OP_MUL_IMM:
			g_assert(riscv_stdext_m);
			NEW_INS_BEFORE (cfg, ins, temp, OP_ICONST);
			temp->inst_c0 = ins->inst_imm;
			temp->dreg = mono_alloc_ireg (cfg);
			ins->sreg2 = temp->dreg;
			ins->inst_imm = 0;
			ins->opcode = OP_IMUL;
			break;
		case OP_IREM_IMM:
		case OP_LREM_UN_IMM:
			mono_decompose_op_imm (cfg, bb, ins);
			break;

		// Bit OP
		case OP_AND_IMM:
		case OP_IAND_IMM:
		case OP_LAND_IMM:
		case OP_IOR_IMM:
		case OP_LOR_IMM:
			if(! RISCV_VALID_I_IMM ((gint32) (gssize) (ins->inst_imm)))
				mono_decompose_op_imm (cfg, bb, ins);
			break;
		case OP_INOT:
		case OP_LNOT:
			ins->opcode = OP_XOR_IMM;
			ins->inst_imm = -1;
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
			NEW_INS_BEFORE (cfg, ins, temp, OP_ICONST);
			temp->opcode = OP_SHL_IMM;
			temp->dreg = ins->dreg;
			temp->sreg1 = ins->sreg1;
			temp->inst_imm = 48;

			ins->opcode = OP_SHR_UN_IMM;
			ins->dreg = ins->dreg;
			ins->sreg1 = temp->dreg;
			ins->inst_imm = 48;
			break;
		case OP_ICONV_TO_I2:
			// slli    a0, a0, 48
			// srai    a0, a0, 48
			NEW_INS_BEFORE (cfg, ins, temp, OP_ICONST);
			temp->opcode = OP_SHL_IMM;
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
		case OP_ZEXT_I4:{
			// TODO: Add inst riscv_adduw in riscv-codegen.h
			// if(riscv_stdext_b){
			if(FALSE){
				NOT_IMPLEMENTED;
				ins->opcode = OP_RISCV_ADDUW;
				ins->sreg2 = RISCV_ZERO;
			}
			else{
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
			printf ("unable to lowering following IR:"); mono_print_ins (ins);
			NOT_IMPLEMENTED;
			break;
		}
		last_ins = ins;
	}

	bb->last_ins = last_ins;
	bb->max_vreg = cfg->next_vreg;

	if (cfg->verbose_level > 2) {
		g_print ("BASIC BLOCK %d (after lowering)\n", bb->block_num);
		MONO_BB_FOR_EACH_INS (bb, ins) {
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

	/*
	 * This is not pretty, but RV64I doesn't make it easy to load constants.
	 * Need to figure out something better.
	 */
	riscv_jal (code, rd, sizeof (guint64));
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

// Uses at most 16 bytes on RV32I and 24 bytes on RV64I.
guint8 *
mono_riscv_emit_load (guint8 *code, int rd, int rs1, gint32 imm)
{
	if (RISCV_VALID_I_IMM (imm)) {
#ifdef TARGET_RISCV64
		riscv_ld (code, rd, rs1, imm);
#else
		riscv_lw (code, rd, rs1, imm);
#endif
	} else {
		code = mono_riscv_emit_imm (code, rd, imm);
		riscv_add (code, rd, rs1, rd);
#ifdef TARGET_RISCV64
		riscv_ld (code, rd, rd, 0);
#else
		riscv_lw (code, rd, rd, 0);
#endif
	}

	return code;
}

// May clobber t1. Uses at most 16 bytes on RV32I and 24 bytes on RV64I.
guint8 *
mono_riscv_emit_store (guint8 *code, int rs1, int rs2, gint32 imm)
{
	if (RISCV_VALID_S_IMM (imm)) {
#ifdef TARGET_RISCV64
		riscv_sd (code, rs1, rs2, imm);
#else
		riscv_sw (code, rs1, rs2, imm);
#endif
	} else {
		code = mono_riscv_emit_imm (code, RISCV_T1, imm);
		riscv_add (code, RISCV_T1, rs2, RISCV_T1);
#ifdef TARGET_RISCV64
		riscv_sd (code, rs1, RISCV_T1, 0);
#else
		riscv_sw (code, rs1, RISCV_T1, 0);
#endif
	}

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
	int cfa_offset;

	cfg->code_size = MAX (cfg->header->code_size * 4, 1024);
	code = cfg->native_code = g_malloc (cfg->code_size);

	/* realigned */
	cfg->stack_offset = ALIGN_TO (cfg->stack_offset, MONO_ARCH_FRAME_ALIGNMENT);

	/*
	 * - Setup frame
	 */
	cfa_offset = 0;
	int stack_size = 0;
	mono_emit_unwind_op_def_cfa (cfg, code, RISCV_SP, 0);

	/* Setup frame */
	if (RISCV_VALID_I_IMM (-cfg->stack_offset)){
		riscv_addi (code,RISCV_SP,RISCV_SP,-cfg->stack_offset);
		// save return value
		stack_size += sizeof(target_mgreg_t);
		code = mono_riscv_emit_store (code, RISCV_RA, RISCV_SP, cfg->stack_offset - stack_size, 0);
		// save s0(fp) value
		stack_size += sizeof(target_mgreg_t);
		code = mono_riscv_emit_store (code, RISCV_FP, RISCV_SP, cfg->stack_offset - stack_size, 0);
	}
	else
		NOT_IMPLEMENTED;

	cfa_offset += cfg->stack_offset;
	mono_emit_unwind_op_def_cfa_offset (cfg, code, cfa_offset);
	mono_emit_unwind_op_offset (cfg, code, RISCV_RA, cfa_offset - sizeof(target_mgreg_t));
	mono_emit_unwind_op_offset (cfg, code, RISCV_FP, cfa_offset - (sizeof(target_mgreg_t) * 2));

	// set s0(fp) value
	riscv_addi (code,RISCV_FP,RISCV_SP,cfg->stack_offset);

	// save other registers
	if (cfg->param_area)
		/* The param area is below the stack pointer */
		riscv_addi (code, RISCV_SP, RISCV_SP, -cfg->param_area);


	if (cfg->method->save_lmf){
		g_assert(cfg->lmf_var->inst_offset <= 0);
		code = emit_setup_lmf (cfg, code, cfg->lmf_var->inst_offset, cfa_offset);
	}
	else
		/* Save gregs */
		code = mono_riscv_emit_store_stack (code, MONO_ARCH_CALLEE_SAVED_REGS & cfg->used_int_regs, RISCV_FP, -cfg->arch.saved_gregs_offset, FALSE);
	
	/* Save address of return value received in A0*/
	if (cfg->vret_addr) {
		MonoInst *ins = cfg->vret_addr;

		g_assert (ins->opcode == OP_REGOFFSET);
		code = mono_riscv_emit_store (code, RISCV_A0, ins->inst_basereg, ins->inst_offset, 0);
	}

	/* Save mrgctx received in MONO_ARCH_RGCTX_REG */
	if (cfg->rgctx_var) {
		MonoInst *ins = cfg->rgctx_var;

		g_assert (ins->opcode == OP_REGOFFSET);

		code = mono_riscv_emit_store (code, MONO_ARCH_RGCTX_REG, ins->inst_basereg, ins->inst_offset, 0);

		mono_add_var_location (cfg, cfg->rgctx_var, TRUE, MONO_ARCH_RGCTX_REG, 0, 0, code - cfg->native_code);
		mono_add_var_location (cfg, cfg->rgctx_var, FALSE, ins->inst_basereg, ins->inst_offset, code - cfg->native_code, 0);
	}

	/*
	 * Move arguments to their registers/stack locations.
	 */
	code = emit_move_args (cfg, code);

	/* Initialize seq_point_info_var */
	if (cfg->arch.seq_point_info_var)
		NOT_IMPLEMENTED;
	else{
		MonoInst *ins;
		if (cfg->arch.ss_tramp_var) {
			/* Initialize ss_tramp_var */
			ins = cfg->arch.ss_tramp_var;
			g_assert (ins->opcode == OP_REGOFFSET);

			code = mono_riscv_emit_imm (code, RISCV_T0, (guint64)&ss_trampoline);
			code = mono_riscv_emit_store (code, RISCV_T0, ins->inst_basereg, ins->inst_offset, 0);
		}
		if (cfg->arch.bp_tramp_var){
			/* Initialize bp_tramp_var */
			ins = cfg->arch.bp_tramp_var;
			g_assert (ins->opcode == OP_REGOFFSET);
			code = mono_riscv_emit_imm (code, RISCV_T0, (guint64)bp_trampoline);
			code = mono_riscv_emit_store (code, RISCV_T0, ins->inst_basereg, ins->inst_offset, 0);
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
		code = mono_riscv_emit_load_regarray (code, MONO_ARCH_CALLEE_SAVED_REGS & cfg->used_int_regs, RISCV_FP, cfg->lmf_var->inst_offset + MONO_STRUCT_OFFSET (MonoLMF, gregs), FALSE);
	} else {
		/* Restore gregs */
		code = mono_riscv_emit_load_stack (code, MONO_ARCH_CALLEE_SAVED_REGS & cfg->used_int_regs, RISCV_FP, -cfg->arch.saved_gregs_offset, FALSE);
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
	case ArgVtypeInIReg:{
		MonoInst *ins = cfg->ret;

		if(cinfo->ret.is_regpair)
			code = mono_riscv_emit_load (code, cinfo->ret.reg + 1, ins->inst_basereg, ins->inst_offset + sizeof(host_mgreg_t), 0);
		code = mono_riscv_emit_load (code, cinfo->ret.reg, ins->inst_basereg, ins->inst_offset, 0);
		break;
	}
	default:
		g_print("Unable process returned storage %d(0x%x)\n",cinfo->ret.storage,cinfo->ret.storage);
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

	if (cfg->verbose_level > 2){
		g_print ("Basic block %d starting at offset 0x%x\n", bb->block_num, bb->native_offset);
		MONO_BB_FOR_EACH_INS (bb, ins) {
			mono_print_ins (ins);
		}
	}

	MONO_BB_FOR_EACH_INS (bb, ins) {
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
		case OP_SEQ_POINT:{
			if (ins->flags & MONO_INST_SINGLE_STEP_LOC){
				MonoInst *var = cfg->arch.ss_tramp_var;

				g_assert (var);
				g_assert (var->opcode == OP_REGOFFSET);
				/* Load ss_tramp_var */
				/* This is equal to &ss_trampoline */
				code = mono_riscv_emit_load (code, RISCV_T0, var->inst_basereg, var->inst_offset, 0);
				/* Load the trampoline address */
				code = mono_riscv_emit_load (code, RISCV_T0, RISCV_T0, 0, 0);
				/* Call it if it is non-null */
				// In riscv, we use jalr to jump
				riscv_beq (code, RISCV_ZERO, RISCV_T0, 8);
				riscv_jalr (code, RISCV_ZERO, RISCV_T0, 0);
			}
			mono_add_seq_point (cfg, bb, ins, code - cfg->native_code);

			if (cfg->compile_aot)
				NOT_IMPLEMENTED;
			else{
				MonoInst *var = cfg->arch.bp_tramp_var;
				g_assert (var);
				g_assert (var->opcode == OP_REGOFFSET);
				/* Load the address of the bp trampoline into IP0 */
				code = mono_riscv_emit_load (code, RISCV_T0, var->inst_basereg, var->inst_offset, 0);
				/* 
				* A placeholder for a possible breakpoint inserted by
				* mono_arch_set_breakpoint ().
				*/
				code = mono_riscv_emit_nop (code);
			}
			break;
		}
		case OP_LOCALLOC:{
			g_assert(MONO_ARCH_FRAME_ALIGNMENT == 16);
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
			code = mono_riscv_emit_store (code, RISCV_ZERO, RISCV_T0, 0, 0);
			code = mono_riscv_emit_store (code, RISCV_ZERO, RISCV_T0, sizeof(host_mgreg_t), 0);
#ifdef TARGET_RISCV32
			code = mono_riscv_emit_store (code, RISCV_ZERO, RISCV_T0, sizeof(host_mgreg_t) * 2, 0);
			code = mono_riscv_emit_store (code, RISCV_ZERO, RISCV_T0, sizeof(host_mgreg_t) * 3, 0);
#endif
			riscv_addi (code, RISCV_T0, RISCV_T0, MONO_ARCH_FRAME_ALIGNMENT);
			riscv_jal (code, RISCV_ZERO, riscv_get_jal_disp (code, loop_start));
			riscv_patch_rel (loop_start, code, MONO_R_RISCV_BEQ);

			riscv_addi (code, ins->dreg, RISCV_SP, 0);
			if (cfg->param_area){
				g_assert (cfg->param_area > 0);
				riscv_addi (code, RISCV_SP, RISCV_SP, -cfg->param_area);
			}
			break;
		}
		case OP_LOCALLOC_IMM:{
			int aligned_imm, aligned_imm_offset;
			aligned_imm = ALIGN_TO (ins->inst_imm, MONO_ARCH_FRAME_ALIGNMENT);
			g_assert (RISCV_VALID_I_IMM (aligned_imm));
			riscv_addi (code, RISCV_SP, RISCV_SP, -aligned_imm);

			/* Init */
			g_assert (MONO_ARCH_FRAME_ALIGNMENT == 16);
			aligned_imm_offset = 0;
			while (aligned_imm_offset < aligned_imm){
				code = mono_riscv_emit_store (code, RISCV_ZERO, RISCV_SP, aligned_imm_offset + 0, 0);
				code = mono_riscv_emit_store (code, RISCV_ZERO, RISCV_SP, aligned_imm_offset + sizeof(host_mgreg_t), 0), 0;
#ifdef TARGET_RISCV32
				code = mono_riscv_emit_store (code, RISCV_ZERO, RISCV_SP, aligned_imm_offset + sizeof(host_mgreg_t) * 2, 0);
				code = mono_riscv_emit_store (code, RISCV_ZERO, RISCV_SP, aligned_imm_offset + sizeof(host_mgreg_t) * 3, 0);
#endif
				aligned_imm_offset += 16;
			}

			riscv_addi (code, ins->dreg, RISCV_SP, 0);
			if (cfg->param_area){
				g_assert (cfg->param_area > 0);
				riscv_addi (code, RISCV_SP, RISCV_SP, -cfg->param_area);
			}
			break;
		}
		case OP_GENERIC_CLASS_INIT:{
			int byte_offset;
			guint8 *branch_label;

			byte_offset = MONO_STRUCT_OFFSET (MonoVTable, initialized);
			
			/* Load vtable->initialized */
			code = mono_riscv_emit_load (code, RISCV_T0, ins->sreg1, byte_offset, 1);
			branch_label = code;
			riscv_bne (code, RISCV_ZERO, RISCV_T0, 0);

			/* Slowpath */
			g_assert (ins->sreg1 == RISCV_A0);
			code = mono_riscv_emit_call (cfg, code, MONO_PATCH_INFO_JIT_ICALL_ID,
								GUINT_TO_POINTER (MONO_JIT_ICALL_mono_generic_class_init));

			mono_riscv_patch (branch_label, code, MONO_R_RISCV_BNE);
			break;
		}

		case OP_NOP:
			code = mono_riscv_emit_nop (code);
			break;
		case OP_MOVE:
#ifdef TARGET_RISCV64
		case OP_LMOVE:
#endif
			// mv ra, a1 -> addi ra, a1, 0
			riscv_addi(code, ins->dreg, ins->sreg1, 0);
			break;
		// r4 move
		case OP_RMOVE:
			g_assert(riscv_stdext_f);
			riscv_fsgnj_s (code, ins->dreg, ins->sreg1, ins->sreg1);
			break;
		case OP_FMOVE:
			// fmv.{s|d} rd, rs1 -> fsgnj.{s|d} rd, rs1, rs1
			g_assert(riscv_stdext_d || riscv_stdext_f);
			if(riscv_stdext_d)
				riscv_fsgnj_d (code, ins->dreg, ins->sreg1, ins->sreg1);
			else
				riscv_fsgnj_s (code, ins->dreg, ins->sreg1, ins->sreg1);
			break;
		case OP_LOAD_MEMBASE:
			code = mono_riscv_emit_load (code, ins->dreg, ins->sreg1, ins->inst_offset, 0);
			break;
		case OP_LOADI1_MEMBASE:
			code = mono_riscv_emit_load (code, ins->dreg, ins->sreg1, ins->inst_offset, 1);
			break;
		case OP_LOADU1_MEMBASE:
			riscv_lbu (code, ins->dreg, ins->sreg1, ins->inst_offset);
			break;
		case OP_LOADI2_MEMBASE:
			code = mono_riscv_emit_load (code, ins->dreg, ins->sreg1, ins->inst_offset, 2);
			break;
		case OP_LOADU2_MEMBASE:
			riscv_lhu (code, ins->dreg, ins->sreg1, ins->inst_offset);
			break;
		case OP_LOADI4_MEMBASE:
			code = mono_riscv_emit_load (code, ins->dreg, ins->sreg1, ins->inst_offset, 4);
			break;
#ifdef TARGET_RISCV64
		case OP_LOADU4_MEMBASE:
			riscv_lwu (code, ins->dreg, ins->sreg1, ins->inst_offset);
			break;
		case OP_LOADI8_MEMBASE:
			code = mono_riscv_emit_load (code, ins->dreg, ins->sreg1, ins->inst_offset, 8);
			break;
#endif
		case OP_STORE_MEMBASE_REG:
			code = mono_riscv_emit_store (code, ins->sreg1, ins->dreg, ins->inst_offset, 0);
			break;
		case OP_STOREI1_MEMBASE_REG:
			code = mono_riscv_emit_store (code, ins->sreg1, ins->dreg, ins->inst_offset, 1);
			break;
		case OP_STOREI2_MEMBASE_REG:
			code = mono_riscv_emit_store (code, ins->sreg1, ins->dreg, ins->inst_offset, 2);
			break;
		case OP_STOREI4_MEMBASE_REG:
			code = mono_riscv_emit_store (code, ins->sreg1, ins->dreg, ins->inst_offset, 4);
			break;
		case OP_STOREI8_MEMBASE_REG:
			code = mono_riscv_emit_store (code, ins->sreg1, ins->dreg, ins->inst_offset, 8);
			break;
		case OP_ICONST:
		case OP_I8CONST:
			code = mono_riscv_emit_imm (code, ins->dreg, ins->inst_c0);
			break;
		case OP_IADD:
		case OP_LADD:
			riscv_add (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_ADD_IMM:
		case OP_IADD_IMM:
		case OP_LADD_IMM:
			riscv_addi (code, ins->dreg, ins->sreg1, ins->inst_imm);
			break;
		case OP_ISUB:
		case OP_LSUB:
			riscv_sub (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_IMUL:
			g_assert (riscv_stdext_m);
			riscv_mul (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_FMUL:
			g_assert (riscv_stdext_f || riscv_stdext_d);
			if (riscv_stdext_d)
				riscv_fmul_d (code, RISCV_ROUND_DY, ins->dreg, ins->sreg1, ins->sreg2);
			else
				riscv_fmul_s (code, RISCV_ROUND_DY, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_LDIV:
			g_assert (riscv_stdext_m);
			code = mono_riscv_emit_branch_exc (cfg, code, OP_RISCV_EXC_BEQ, ins->sreg2, RISCV_ZERO, "DivideByZeroException");
			riscv_div (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_LDIV_UN:
			g_assert(riscv_stdext_m);
			code = mono_riscv_emit_branch_exc (cfg, code, OP_RISCV_EXC_BEQ, ins->sreg2, RISCV_ZERO, "DivideByZeroException");
			riscv_divu (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_IREM:
		case OP_LREM:
			g_assert(riscv_stdext_m);
			code = mono_riscv_emit_branch_exc (cfg, code, OP_RISCV_EXC_BEQ, ins->sreg2, RISCV_ZERO, "DivideByZeroException");
			riscv_rem (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_IREM_UN:
		case OP_LREM_UN:
			g_assert(riscv_stdext_m);
			code = mono_riscv_emit_branch_exc (cfg, code, OP_RISCV_EXC_BEQ, ins->sreg2, RISCV_ZERO, "DivideByZeroException");
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
			riscv_fence (code, RISCV_FENCE_MEM, RISCV_FENCE_MEM);
			break;
		case OP_ATOMIC_ADD_I4:
			riscv_amoadd_w (code, RISCV_ORDER_ALL, ins->dreg, ins->sreg2, ins->sreg1);
			break;
		case OP_ATOMIC_LOAD_I4:{
			riscv_fence (code, RISCV_FENCE_MEM, RISCV_FENCE_MEM);
			code = mono_riscv_emit_load (code, ins->dreg, ins->sreg1, ins->inst_offset, 4);
			riscv_fence (code, RISCV_FENCE_R, RISCV_FENCE_MEM);
			break;
		}
		case OP_ATOMIC_STORE_U1:{
			riscv_fence (code, RISCV_FENCE_MEM, RISCV_FENCE_W);
			code = mono_riscv_emit_store (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset, 1);
			if (ins->backend.memory_barrier_kind == MONO_MEMORY_BARRIER_SEQ)
				riscv_fence (code, RISCV_FENCE_MEM, RISCV_FENCE_MEM);
			break;
		}
		case OP_ATOMIC_STORE_I4:{
			riscv_fence (code, RISCV_FENCE_MEM, RISCV_FENCE_W);
			code = mono_riscv_emit_store (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset, 4);
			if (ins->backend.memory_barrier_kind == MONO_MEMORY_BARRIER_SEQ)
				riscv_fence (code, RISCV_FENCE_MEM, RISCV_FENCE_MEM);
			break;
		}
		case OP_ATOMIC_CAS_I4:{
			g_assert(riscv_stdext_a);
			/**
			 * loop_start:
			 * 	lr.w	t0, rs1
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
			riscv_lr_w (code, RISCV_ORDER_NONE, RISCV_T0, ins->sreg1);
			branch_label = code;
			riscv_bne (code, RISCV_T0, ins->sreg3, 0);
			riscv_sc_w (code, RISCV_ORDER_RL, RISCV_T1, ins->sreg2, ins->sreg1);
			riscv_bne (code, RISCV_ZERO, RISCV_T1, loop_start - code);
			riscv_patch_rel (branch_label, code, MONO_R_RISCV_BNE);
			
			riscv_fence (code, RISCV_FENCE_MEM, RISCV_FENCE_MEM);
			riscv_addi (code, ins->dreg, RISCV_T0, 0);
			break;
		}
		case OP_ATOMIC_EXCHANGE_I4:{
			g_assert (riscv_stdext_a);
			riscv_amoswap_w (code, RISCV_ORDER_ALL, ins->dreg, ins->sreg2, ins->sreg1);
			break;
		}
#ifdef TARGET_RISCV64
		case OP_ATOMIC_STORE_U8:{
			// TODO: Check This
			riscv_fence (code, RISCV_FENCE_MEM, RISCV_FENCE_W);
			code = mono_riscv_emit_store (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset, 8);
			if (ins->backend.memory_barrier_kind == MONO_MEMORY_BARRIER_SEQ)
				riscv_fence (code, RISCV_FENCE_MEM, RISCV_FENCE_MEM);
			break;
		}
		case OP_ATOMIC_LOAD_I8:
		case OP_ATOMIC_LOAD_U8:{
			// TODO: Check This
			riscv_fence (code, RISCV_FENCE_MEM, RISCV_FENCE_MEM);
			code = mono_riscv_emit_load (code, ins->dreg, ins->sreg1, ins->inst_offset, 8);
			riscv_fence (code, RISCV_FENCE_R, RISCV_FENCE_MEM);
			break;
		}
		case OP_ATOMIC_CAS_I8:{
			g_assert (riscv_stdext_a);
			guint8 *loop_start, *branch_label;
			/* sreg2 is the value, sreg3 is the comparand */
			loop_start = code;
			riscv_lr_d (code, RISCV_ORDER_NONE, RISCV_T0, ins->sreg1);
			branch_label = code;
			riscv_bne (code, RISCV_T0, ins->sreg3, 0);
			riscv_sc_d (code, RISCV_ORDER_RL, RISCV_T1, ins->sreg2, ins->sreg1);
			riscv_bne (code, RISCV_ZERO, RISCV_T1, loop_start - code);
			riscv_patch_rel (branch_label, code, MONO_R_RISCV_BNE);
			
			riscv_fence (code, RISCV_FENCE_MEM, RISCV_FENCE_MEM);
			riscv_addi (code, ins->dreg, RISCV_T0, 0);
			break;
		}
		case OP_ATOMIC_EXCHANGE_I8:{
			g_assert (riscv_stdext_a);
			riscv_amoswap_d (code, RISCV_ORDER_ALL, ins->dreg, ins->sreg2, ins->sreg1);
			break;
		}
#endif

		/* Float */
		case OP_R4CONST:
		case OP_R8CONST:{
			code = mono_riscv_emit_float_imm (code, ins->dreg, *(guint64*)ins->inst_p0, ins->opcode == OP_R4CONST);
			break;
		}
		case OP_ICONV_TO_R4:{
			g_assert (riscv_stdext_f);
			riscv_fcvt_s_w (code, RISCV_ROUND_DY, ins->dreg, ins->sreg1);
			break;
		}
		case OP_ICONV_TO_R8:{
			g_assert (riscv_stdext_d);
			riscv_fcvt_d_w (code, ins->dreg, ins->sreg1);
			break;
		}
		case OP_RCONV_TO_R8:{
			g_assert (riscv_stdext_d);
			riscv_fcvt_d_s (code, ins->dreg, ins->sreg1);
			break;
		}
		case OP_FCONV_TO_I4:{
			g_assert (riscv_stdext_f || riscv_stdext_d);
			if(riscv_stdext_d)
				riscv_fcvt_w_d (code, RISCV_ROUND_DY, ins->dreg, ins->sreg1);
			else
				riscv_fcvt_w_s (code, RISCV_ROUND_DY, ins->dreg, ins->sreg1);
			break;
		}
		case OP_FCONV_TO_R4:
		case OP_RISCV_SETFREG_R4:{
			g_assert (riscv_stdext_d);
			riscv_fcvt_s_d (code, RISCV_ROUND_DY, ins->dreg, ins->sreg1);
			break;
		}
		case OP_FCEQ:{
			g_assert (riscv_stdext_f || riscv_stdext_d);
			if (riscv_stdext_d)
				riscv_feq_d (code, ins->dreg, ins->sreg1, ins->sreg2);
			else
				riscv_feq_s (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		}
		case OP_FCLT:
		case OP_FCLT_UN:{
			g_assert (riscv_stdext_f || riscv_stdext_d);
			if (riscv_stdext_d)
				riscv_flt_d (code, ins->dreg, ins->sreg1, ins->sreg2);
			else
				riscv_flt_s (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		}
		case OP_STORER4_MEMBASE_REG:{
			if (mono_arch_is_soft_float ())
				code = mono_riscv_emit_store (code, ins->sreg1, ins->dreg, ins->inst_offset, 4);
			else
				code = mono_riscv_emit_fstore (code, ins->sreg1, ins->dreg, ins->inst_offset, TRUE);
			break;
		}
		case OP_STORER8_MEMBASE_REG:{
			if (mono_arch_is_soft_float ())
				code = mono_riscv_emit_store (code, ins->sreg1, ins->dreg, ins->inst_offset, 8);
			else
				code = mono_riscv_emit_fstore (code, ins->sreg1, ins->dreg, ins->inst_offset, FALSE);
			break;
		}
		case OP_LOADR4_MEMBASE:{
			if( mono_arch_is_soft_float())
				code = mono_riscv_emit_load (code, ins->dreg, ins->sreg1, ins->inst_offset, 4);
			else
				code = mono_riscv_emit_fload (code, ins->dreg, ins->sreg1, ins->inst_offset, TRUE);
			break;
		}
		case OP_LOADR8_MEMBASE:{
			if( mono_arch_is_soft_float ())
				code = mono_riscv_emit_load (code, ins->dreg, ins->sreg1, ins->inst_offset, 8);
			else
				code = mono_riscv_emit_fload (code, ins->dreg, ins->sreg1, ins->inst_offset, FALSE);
			break;
		}

		/* Calls */
		case OP_VOIDCALL:
		case OP_CALL:
		case OP_FCALL:
		case OP_LCALL:
		case OP_VCALL2: {
			call = (MonoCallInst*)ins;
			const MonoJumpInfoTarget patch = mono_call_to_patch (call);
			code = mono_riscv_emit_call (cfg, code, patch.type, patch.target);
			code = emit_move_return_value (cfg, code, ins);
			break;
		}
		case OP_CALL_REG:
		case OP_VOIDCALL_REG:
			// JALR x1, 0(src1)
			riscv_jalr (code, RISCV_RA, ins->sreg1, 0);
			code = emit_move_return_value (cfg, code, ins);
			break;
		case OP_CALL_MEMBASE:
		case OP_LCALL_MEMBASE:
		case OP_VCALL2_MEMBASE:
		case OP_VOIDCALL_MEMBASE:
			code = mono_riscv_emit_load (code, RISCV_T0, ins->inst_basereg, ins->inst_offset, 0);
			riscv_jalr (code, RISCV_RA, RISCV_T0, 0);
			code = emit_move_return_value (cfg, code, ins);
			break;

		/* Branch */
		// incase of target is far from the brunch inst
		// reverse a nop inst for patch use
		case OP_RISCV_BNE:
			mono_add_patch_info_rel (cfg, (code - cfg->native_code), MONO_PATCH_INFO_BB, ins->inst_true_bb, MONO_R_RISCV_BNE);
			riscv_bne (code, ins->sreg1, ins->sreg2, 0);
			code = mono_riscv_emit_nop (code);
			break;
		case OP_RISCV_BEQ:
			mono_add_patch_info_rel (cfg, (code - cfg->native_code), MONO_PATCH_INFO_BB, ins->inst_true_bb, MONO_R_RISCV_BEQ);
			riscv_beq (code, ins->sreg1, ins->sreg2, 0);
			code = mono_riscv_emit_nop (code);
			break;
		case OP_RISCV_BGE:
			mono_add_patch_info_rel (cfg, (code - cfg->native_code), MONO_PATCH_INFO_BB, ins->inst_true_bb, MONO_R_RISCV_BGE);
			riscv_bge (code, ins->sreg1, ins->sreg2, 0);
			code = mono_riscv_emit_nop (code);
			break;
		case OP_RISCV_BGEU:
			mono_add_patch_info_rel (cfg, (code - cfg->native_code), MONO_PATCH_INFO_BB, ins->inst_true_bb, MONO_R_RISCV_BGEU);
			riscv_bgeu (code, ins->sreg1, ins->sreg2, 0);
			code = mono_riscv_emit_nop (code);
			break;
		case OP_RISCV_BLT:
			mono_add_patch_info_rel (cfg, (code - cfg->native_code), MONO_PATCH_INFO_BB, ins->inst_true_bb, MONO_R_RISCV_BLT);
			riscv_blt (code, ins->sreg1, ins->sreg2, 0);
			code = mono_riscv_emit_nop (code);
			break;
		case OP_RISCV_BLTU:
			mono_add_patch_info_rel (cfg, (code - cfg->native_code), MONO_PATCH_INFO_BB, ins->inst_true_bb, MONO_R_RISCV_BLTU);
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
			mono_add_patch_info_rel (cfg, offset, (MonoJumpInfoType)(gsize)ins->inst_i1, ins->inst_p0, MONO_R_RISCV_IMM);
			code = mono_riscv_emit_imm (code, ins->dreg, 0xffffffff);
			break;
		case OP_RISCV_EXC_BNE:
		case OP_RISCV_EXC_BEQ:
		case OP_RISCV_EXC_BGEU:
		case OP_RISCV_EXC_BLT:
		case OP_RISCV_EXC_BLTU:{
			code = mono_riscv_emit_branch_exc (cfg, code, ins->opcode, ins->sreg1, ins->sreg2, (const char*)ins->inst_p1);
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

		case OP_GC_SAFE_POINT:{
			guint8 *src_inst_pointer [1];

			riscv_ld (code, RISCV_T1, ins->sreg1, 0);
			/* Call it if it is non-null */
			src_inst_pointer [0] = code;
			riscv_beq (code, RISCV_ZERO, RISCV_T1, 0);
			code = mono_riscv_emit_call (cfg, code, MONO_PATCH_INFO_JIT_ICALL_ID, GUINT_TO_POINTER (MONO_JIT_ICALL_mono_threads_state_poll));
			mono_riscv_patch (src_inst_pointer [0], code, MONO_R_RISCV_BEQ);
			break;
		}
		case OP_START_HANDLER:{
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);
			/* Save caller address */
			code = mono_riscv_emit_store (code, RISCV_RA, spvar->inst_basereg, spvar->inst_offset, 0);

			/*
			* Reserve a param area, see test_0_finally_param_area ().
			* This is needed because the param area is not set up when
			* we are called from EH code.
			*/
			if (cfg->param_area)
				riscv_addi (code, RISCV_SP, RISCV_SP, -cfg->param_area);
			break;
		}
		case OP_CALL_HANDLER:
			mono_add_patch_info_rel (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_target_bb, MONO_R_RISCV_JAL);
			riscv_jal (code, RISCV_RA, 0);
			cfg->thunk_area += THUNK_SIZE;
			for (GList *tmp = ins->inst_eh_blocks; tmp != bb->clause_holes; tmp = tmp->prev)
				mono_cfg_add_try_hole (cfg, ((MonoLeaveClause *) tmp->data)->clause, code, bb);
			break;
		case OP_ENDFINALLY:{
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);
			if (cfg->param_area)
				riscv_addi (code, RISCV_SP, RISCV_SP, -cfg->param_area);
			if (ins->opcode == OP_ENDFILTER && ins->sreg1 != RISCV_A0)
				riscv_addi (code, RISCV_A0, ins->sreg1, 0);
			
			/* Return to either after the branch in OP_CALL_HANDLER, or to the EH code */
			code = mono_riscv_emit_load (code, RISCV_RA, spvar->inst_basereg, spvar->inst_offset, 0);
			riscv_jalr (code, RISCV_ZERO, RISCV_RA, 0);
			break;
		}

		default:
			printf ("unable to output following IR:"); mono_print_ins (ins);
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
	NOT_IMPLEMENTED;
}

void
mono_arch_stop_single_stepping (void)
{
	NOT_IMPLEMENTED;
}

gboolean
mono_arch_is_single_step_event (void *info, void *sigctx)
{
	NOT_IMPLEMENTED;
	return FALSE;
}

gboolean
mono_arch_is_breakpoint_event (void *info, void *sigctx)
{
	NOT_IMPLEMENTED;
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
	NOT_IMPLEMENTED;
	return NULL;
}
#endif /* MONO_ARCH_SOFT_DEBUG_SUPPORTED */

gpointer
mono_arch_load_function (MonoJitICallId jit_icall_id)
{
	return NULL;
}
