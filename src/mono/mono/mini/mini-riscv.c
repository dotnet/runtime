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

static guint8*
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
	MonoJitMemoryManager* jit_mm;

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
			g_print ("thunk failed %p->%p, thunk space=%d method %s", code, target, thunks_size, mono_method_full_name (cfg->method, TRUE));
			g_assert_not_reached ();
		}

		g_assert (*(guint32*)thunks == 0);
		emit_thunk (thunks, target);

		cfg->arch.thunks += THUNK_SIZE;
		cfg->arch.thunks_size -= THUNK_SIZE;

		return thunks;
	}
	else {
		ji = mini_jit_info_table_find (code);
		g_assert (ji);
		info = mono_jit_info_get_thunk_info (ji);
		g_assert (info);

		thunks = (guint8*)ji->code_start + info->thunks_offset;
		thunks_size = info->thunks_size;

		orig_target = mono_arch_get_call_target (code + 4);

		/* Arbitrary lock */
		jit_mm = get_default_jit_mm ();

		jit_mm_lock (jit_mm);

		target_thunk = NULL;
		if (orig_target >= thunks && orig_target < thunks + thunks_size){
			/* The call already points to a thunk, because of trampolines etc. */
			target_thunk = orig_target;
		}
		else {
			for (p = thunks; p < thunks + thunks_size; p += THUNK_SIZE) {
				if (((guint32*)p) [0] == 0) {
					/* Free entry */
					target_thunk = p;
					break;
				} else if (*(guint64*)(p + 4) == (guint64)target) {
					/* Thunk already points to target */
					target_thunk = p;
					break;
				}
			}
		}

		if (!target_thunk) {
			jit_mm_unlock (jit_mm);
			g_print ("thunk failed %p->%p, thunk space=%d method %s", code, target, thunks_size, cfg ? mono_method_full_name (cfg->method, TRUE) : mono_method_full_name (jinfo_get_method (ji), TRUE));
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
	switch (relocation){
	case MONO_R_RISCV_IMM:
		*(guint64 *)(code + 4) = (guint64)target;
		break;
	case MONO_R_RISCV_JAL:{
		gint32 inst = *(gint32 *) code;
		gint32 rd = RISCV_BITS (inst, 7, 5);
		target = MINI_FTNPTR_TO_ADDR (target);
		if(riscv_is_jal_disp (code, target))
			riscv_jal(code, rd, riscv_get_jal_disp (code, target));
		else{
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
	case MONO_R_RISCV_BLTU:{
		int offset = target - code;

		gint32 inst = *(gint32 *) code;
		gint32 rs1 = RISCV_BITS (inst, 15, 5);
		gint32 rs2 = RISCV_BITS (inst, 20, 5);

		// if the offset too large to encode as B_IMM
		// try to use jal to branch
		if(!RISCV_VALID_B_IMM ((gint32) (gssize)(offset))){
			// branch inst should followed by a nop inst
			g_assert (*(gint32 *)(code + 4) == 0x13);
			if(riscv_is_jal_disp (code, target)){
				if(relocation == MONO_R_RISCV_BEQ)
					riscv_bne (code, rs1, rs2, 8);
				else if(relocation == MONO_R_RISCV_BNE)
					riscv_beq (code, rs1, rs2, 8);
				else if(relocation == MONO_R_RISCV_BGE)
					riscv_blt (code, rs1, rs2, 8);
				else if(relocation == MONO_R_RISCV_BLT)
					riscv_bge (code, rs1, rs2, 8);
				else if(relocation == MONO_R_RISCV_BGEU)
					riscv_bltu (code, rs1, rs2, 8);
				else if(relocation == MONO_R_RISCV_BLTU)
					riscv_bgeu (code, rs1, rs2, 8);
				else
					g_assert_not_reached ();
				break;

				riscv_jal (code, RISCV_ZERO, riscv_get_jal_disp(code, target));
			}
			else
				g_assert_not_reached ();
		}

		if(relocation == MONO_R_RISCV_BEQ)
			riscv_beq (code, rs1, rs2, offset);
		else if(relocation == MONO_R_RISCV_BNE)
			riscv_bne (code, rs1, rs2, offset);
		else if(relocation == MONO_R_RISCV_BGE)
			riscv_bge (code, rs1, rs2, offset);
		else if(relocation == MONO_R_RISCV_BLT)
			riscv_blt (code, rs1, rs2, offset);
		else if(relocation == MONO_R_RISCV_BGEU)
			riscv_bgeu (code, rs1, rs2, offset);
		else if(relocation == MONO_R_RISCV_BLTU)
			riscv_bltu (code, rs1, rs2, offset);
		else
			g_assert_not_reached ();
		break;
	}
	case MONO_R_RISCV_JALR:
		*(guint64 *) code = (guint64)target;
		break;
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

void
mono_arch_patch_code_new (MonoCompile *cfg, guint8 *code,
                          MonoJumpInfo *ji, gpointer target)
{
	guint8 *ip;

	ip = ji->ip.i + code;
	switch (ji->type){
		case MONO_PATCH_INFO_METHOD_JUMP:
			/* ji->relocation is not set by the caller */
			riscv_patch_full (cfg, ip, (guint8*)target, MONO_R_RISCV_JAL);
			mono_arch_flush_icache (ip, 8);
			break;
		case MONO_PATCH_INFO_NONE:
			break;
		default:
			riscv_patch_full (cfg, ip, (guint8*)target, ji->relocation);
			break;
	}
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
	NOT_IMPLEMENTED;
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


static guint8 *
mono_riscv_emit_call (MonoCompile *cfg, guint8* code, MonoJumpInfoType patch_type, gconstpointer data)
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
	// guint8 *p;

	// riscv_auipc(code, RISCV_T0, 0);
	// // load imm
	// riscv_jal (code, RISCV_T1, sizeof (guint64) + 4);
	// p = code;
	// code += sizeof (guint64);
	// riscv_ld (code, RISCV_T1, RISCV_T1, 0);
	// // pc + imm
	// riscv_add(code, RISCV_T0, RISCV_T0, RISCV_T1);

	// *(guint64 *)p = (gsize)code;

	switch(opcode){
	case OP_RISCV_EXC_BEQ:
		riscv_bne (code, sreg1, sreg2, 16 + sizeof (guint64));
		break;
	case OP_RISCV_EXC_BNE:
		riscv_beq (code, sreg1, sreg2, 16 + sizeof (guint64));
		break;
	case OP_RISCV_EXC_BLT:
		riscv_bge (code, sreg1, sreg2, 16 + sizeof (guint64));
		break;
	case OP_RISCV_EXC_BLTU:
		riscv_bgeu (code, sreg1, sreg2, 16 + sizeof (guint64));
		break;
	case OP_RISCV_EXC_BGEU:
		riscv_bltu (code, sreg1, sreg2, 16 + sizeof (guint64));
		break;
	default:
		g_print ("can't emit exc branch %d\n", opcode);
		NOT_IMPLEMENTED;
	}
	riscv_jal (code, RISCV_T6, sizeof (guint64) + 4);

	mono_add_patch_info_rel (cfg, code - cfg->native_code, MONO_PATCH_INFO_EXC, exc_name, MONO_R_RISCV_JALR);
	code += sizeof (guint64);

	code = mono_riscv_emit_load (code, RISCV_T6, RISCV_T6, 0, 0);
	riscv_jalr (code, RISCV_ZERO, RISCV_T6, 0);
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
	MonoJumpInfo *ji;
	MonoClass *exc_class;
	guint8 *code, *ip;
	guint8* exc_throw_pos [MONO_EXC_INTRINS_NUM] = {NULL};
	guint8 exc_throw_found [MONO_EXC_INTRINS_NUM] = {0};
	int exc_id, max_epilog_size = 0;

	for (ji = cfg->patch_info; ji; ji = ji->next) {
		if (ji->type == MONO_PATCH_INFO_EXC) {
			exc_id = mini_exception_id_by_name ((const char*)ji->data.target);
			g_assert (exc_id < MONO_EXC_INTRINS_NUM);
			if (!exc_throw_found [exc_id]) {
				max_epilog_size += 40; // 8 Inst for exception
				exc_throw_found [exc_id] = TRUE;
			}
		}
	}

	code = realloc_code (cfg, max_epilog_size);

	/* Emit code to raise corlib exceptions */
	for (ji = cfg->patch_info; ji; ji = ji->next) {
		if (ji->type != MONO_PATCH_INFO_EXC)
			continue;

		ip = cfg->native_code + ji->ip.i;

		exc_id = mini_exception_id_by_name ((const char*)ji->data.target);

		if (exc_throw_pos [exc_id]){
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
		riscv_addi (code, RISCV_A1, RISCV_T0, 0);
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
